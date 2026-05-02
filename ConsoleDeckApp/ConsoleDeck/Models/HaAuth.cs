using System.Text.Json.Serialization;

namespace ConsoleDeck.Models;

public class HaAuth
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("token")]
    public string Token { get; set; } = "";
}
