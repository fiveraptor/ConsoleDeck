using System.Text.Json.Serialization;

namespace ConsoleDeck.Models;

public class ButtonAction
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "none";

    [JsonPropertyName("value")]
    public string Value { get; set; } = "";

    [JsonPropertyName("focus")]
    public string? Focus { get; set; }

    [JsonPropertyName("ha_action")]
    public string? HaAction { get; set; }

    public ButtonAction Clone() => new()
    {
        Type = Type,
        Value = Value,
        Focus = Focus,
        HaAction = HaAction,
    };
}
