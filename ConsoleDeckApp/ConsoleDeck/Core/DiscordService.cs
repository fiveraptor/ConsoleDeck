using System.IO;
using System.IO.Pipes;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using ConsoleDeck.Models;

namespace ConsoleDeck.Core;

public enum DiscordStatus { Disconnected, Connecting, Connected }

public class DiscordService
{
    public DiscordStatus Status { get; private set; } = DiscordStatus.Disconnected;
    public event Action<DiscordStatus>? StatusChanged;

    private DiscordIpcClient? _client;
    private CancellationTokenSource? _watchdogCts;

    public async Task<bool> ConnectAsync(DiscordAuth auth)
    {
        if (string.IsNullOrEmpty(auth.ClientId)) return false;

        SetStatus(DiscordStatus.Connecting);
        try
        {
            var client = new DiscordIpcClient();
            if (!await client.ConnectAsync(auth.ClientId))
            {
                SetStatus(DiscordStatus.Disconnected);
                return false;
            }

            if (!string.IsNullOrEmpty(auth.AccessToken))
            {
                if (await client.AuthenticateAsync(auth.AccessToken))
                {
                    _client = client;
                    SetStatus(DiscordStatus.Connected);
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(auth.RefreshToken) && !string.IsNullOrEmpty(auth.ClientSecret))
            {
                var token = await RefreshTokenAsync(auth);
                if (token != null)
                {
                    if (await client.AuthenticateAsync(token))
                    {
                        _client = client;
                        SetStatus(DiscordStatus.Connected);
                        return true;
                    }
                }
            }

            client.Dispose();
            SetStatus(DiscordStatus.Disconnected);
            return false;
        }
        catch
        {
            SetStatus(DiscordStatus.Disconnected);
            return false;
        }
    }

    public void Disconnect()
    {
        _watchdogCts?.Cancel();
        _client?.Dispose();
        _client = null;
        SetStatus(DiscordStatus.Disconnected);
    }

    public void StartWatchdog(DiscordAuth auth)
    {
        _watchdogCts?.Cancel();
        _watchdogCts = new CancellationTokenSource();
        Task.Run(() => WatchdogLoop(auth, _watchdogCts.Token));
    }

    private async Task WatchdogLoop(DiscordAuth auth, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(10_000, ct).ContinueWith(_ => { });
            if (_client == null && Status == DiscordStatus.Disconnected)
                await ConnectAsync(auth);
        }
    }

    public void ToggleMute() => RunAction(async c =>
    {
        var settings = await c.GetVoiceSettingsAsync();
        var muted = settings.GetProperty("data").GetProperty("mute").GetBoolean();
        await c.SetVoiceSettingsAsync(mute: !muted);
    });

    public void ToggleDeafen() => RunAction(async c =>
    {
        var settings = await c.GetVoiceSettingsAsync();
        var deaf = settings.GetProperty("data").GetProperty("deaf").GetBoolean();
        await c.SetVoiceSettingsAsync(deaf: !deaf);
    });

    public void LeaveChannel() => RunAction(c => c.LeaveVoiceChannelAsync());

    private void RunAction(Func<DiscordIpcClient, Task> action)
    {
        if (_client == null) return;
        Task.Run(async () =>
        {
            try { await action(_client); }
            catch
            {
                _client?.Dispose();
                _client = null;
                SetStatus(DiscordStatus.Disconnected);
            }
        });
    }

    private static async Task<string?> RefreshTokenAsync(DiscordAuth auth)
    {
        try
        {
            using var http = new HttpClient();
            var resp = await http.PostAsync("https://discord.com/api/oauth2/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = auth.ClientId,
                    ["client_secret"] = auth.ClientSecret,
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = auth.RefreshToken!,
                }));
            var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("access_token", out var tok))
            {
                auth.AccessToken = tok.GetString();
                if (doc.RootElement.TryGetProperty("refresh_token", out var rt))
                    auth.RefreshToken = rt.GetString();
                return auth.AccessToken;
            }
        }
        catch { }
        return null;
    }

    private void SetStatus(DiscordStatus s)
    {
        Status = s;
        StatusChanged?.Invoke(s);
    }
}

internal class DiscordIpcClient : IDisposable
{
    private NamedPipeClientStream? _pipe;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<bool> ConnectAsync(string clientId)
    {
        for (int i = 0; i <= 9; i++)
        {
            try
            {
                var pipe = new NamedPipeClientStream(".", $"discord-ipc-{i}", PipeDirection.InOut, PipeOptions.Asynchronous);
                await pipe.ConnectAsync(2000);
                _pipe = pipe;
                await WriteFrameAsync(0, new { v = 1, client_id = clientId });
                var resp = await ReadFrameAsync();
                if (resp.TryGetProperty("evt", out var evt) && evt.GetString() == "READY")
                    return true;
                pipe.Dispose();
                _pipe = null;
            }
            catch { }
        }
        return false;
    }

    public async Task<bool> AuthenticateAsync(string token)
    {
        try
        {
            var resp = await SendCommandAsync("AUTHENTICATE", new { access_token = token });
            return resp.TryGetProperty("evt", out var evt) && evt.GetString() != "ERROR";
        }
        catch { return false; }
    }

    public async Task<JsonElement> GetVoiceSettingsAsync()
        => await SendCommandAsync("GET_VOICE_SETTINGS", new { });

    public async Task<JsonElement> SetVoiceSettingsAsync(bool? mute = null, bool? deaf = null)
    {
        var args = new Dictionary<string, object>();
        if (mute.HasValue) args["mute"] = mute.Value;
        if (deaf.HasValue) args["deaf"] = deaf.Value;
        return await SendCommandAsync("SET_VOICE_SETTINGS", args);
    }

    public Task<JsonElement> LeaveVoiceChannelAsync()
        => SendCommandAsync("SELECT_VOICE_CHANNEL", new Dictionary<string, object?> { ["channel_id"] = null });

    private async Task<JsonElement> SendCommandAsync(string cmd, object args)
    {
        await _lock.WaitAsync();
        try
        {
            var nonce = Guid.NewGuid().ToString("N");
            await WriteFrameAsync(1, new { cmd, args, nonce });
            while (true)
            {
                var resp = await ReadFrameAsync();
                if (resp.TryGetProperty("nonce", out var n) && n.GetString() == nonce)
                    return resp;
            }
        }
        finally { _lock.Release(); }
    }

    private async Task WriteFrameAsync(int opcode, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var payload = Encoding.UTF8.GetBytes(json);
        var header = new byte[8];
        BitConverter.TryWriteBytes(header.AsSpan(0), opcode);
        BitConverter.TryWriteBytes(header.AsSpan(4), payload.Length);
        await _pipe!.WriteAsync(header);
        await _pipe.WriteAsync(payload);
        await _pipe.FlushAsync();
    }

    private async Task<JsonElement> ReadFrameAsync()
    {
        var header = new byte[8];
        await _pipe!.ReadExactlyAsync(header);
        var length = BitConverter.ToInt32(header, 4);
        var data = new byte[length];
        await _pipe.ReadExactlyAsync(data);
        return JsonDocument.Parse(data).RootElement.Clone();
    }

    public void Dispose()
    {
        _pipe?.Dispose();
        _pipe = null;
    }
}
