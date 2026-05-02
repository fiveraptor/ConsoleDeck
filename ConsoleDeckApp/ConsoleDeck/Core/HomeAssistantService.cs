using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ConsoleDeck.Core;

public class HomeAssistantService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };

    public async Task<bool> TestConnectionAsync(string url, string token)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"{url.TrimEnd('/')}/api/");
            req.Headers.Add("Authorization", $"Bearer {token}");
            var resp = await Http.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public void CallService(string entityId, string action)
    {
        var auth = App.Config.LoadHaAuth();
        if (string.IsNullOrEmpty(auth.Url) || string.IsNullOrEmpty(auth.Token)) return;
        Task.Run(() => CallServiceAsync(entityId, action, auth.Url, auth.Token));
    }

    private static async Task CallServiceAsync(string entityId, string action, string url, string token)
    {
        try
        {
            var domain = entityId.Split('.')[0];
            var req = new HttpRequestMessage(HttpMethod.Post, $"{url.TrimEnd('/')}/api/services/{domain}/{action}");
            req.Headers.Add("Authorization", $"Bearer {token}");
            req.Content = new StringContent(
                JsonSerializer.Serialize(new { entity_id = entityId }),
                Encoding.UTF8, "application/json");
            await Http.SendAsync(req);
        }
        catch { }
    }
}
