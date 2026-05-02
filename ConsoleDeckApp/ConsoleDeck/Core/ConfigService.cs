using System.IO;
using System.Text.Json;
using ConsoleDeck.Models;

namespace ConsoleDeck.Core;

public class ConfigService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public string DataDir { get; }
    public string ConfigFile { get; }
    public string DiscordAuthFile { get; }
    public string HaAuthFile { get; }

    public event Action? ConfigChanged;

    private FileSystemWatcher? _watcher;
    private DateTime _lastWrite;

    public ConfigService()
    {
        DataDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        ConfigFile = Path.Combine(DataDir, "config.json");
        DiscordAuthFile = Path.Combine(DataDir, "discord_auth.json");
        HaAuthFile = Path.Combine(DataDir, "ha_auth.json");
    }

    public Dictionary<string, ButtonAction> LoadConfig()
    {
        if (!File.Exists(ConfigFile))
        {
            var def = Enumerable.Range(1, 9)
                .ToDictionary(i => $"BUTTON_{i}", _ => new ButtonAction());
            SaveConfig(def);
            return def;
        }
        var json = File.ReadAllText(ConfigFile);
        return JsonSerializer.Deserialize<Dictionary<string, ButtonAction>>(json) ?? new();
    }

    public void SaveConfig(Dictionary<string, ButtonAction> config)
    {
        File.WriteAllText(ConfigFile, JsonSerializer.Serialize(config, JsonOpts));
    }

    public DiscordAuth LoadDiscordAuth()
    {
        if (!File.Exists(DiscordAuthFile)) return new();
        return JsonSerializer.Deserialize<DiscordAuth>(File.ReadAllText(DiscordAuthFile)) ?? new();
    }

    public void SaveDiscordAuth(DiscordAuth auth)
    {
        File.WriteAllText(DiscordAuthFile, JsonSerializer.Serialize(auth, JsonOpts));
    }

    public HaAuth LoadHaAuth()
    {
        if (!File.Exists(HaAuthFile)) return new();
        return JsonSerializer.Deserialize<HaAuth>(File.ReadAllText(HaAuthFile)) ?? new();
    }

    public void SaveHaAuth(HaAuth auth)
    {
        File.WriteAllText(HaAuthFile, JsonSerializer.Serialize(auth, JsonOpts));
    }

    public void StartWatching()
    {
        _lastWrite = File.Exists(ConfigFile) ? File.GetLastWriteTime(ConfigFile) : DateTime.MinValue;
        _watcher = new FileSystemWatcher(DataDir, "config.json")
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true,
        };
        _watcher.Changed += OnFileChanged;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var mtime = File.GetLastWriteTime(ConfigFile);
        if (mtime == _lastWrite) return;
        _lastWrite = mtime;
        ConfigChanged?.Invoke();
    }

    public void StopWatching()
    {
        _watcher?.Dispose();
        _watcher = null;
    }
}
