using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleDeck.Models;

namespace ConsoleDeck.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    public ObservableCollection<ButtonViewModel> Buttons { get; } = new();

    public DashboardViewModel()
    {
        Reload();
        App.Config.ConfigChanged += () => System.Windows.Application.Current.Dispatcher.Invoke(Reload);
    }

    public void Reload()
    {
        var config = App.Config.LoadConfig();
        Buttons.Clear();
        for (int i = 1; i <= 9; i++)
        {
            var key = $"BUTTON_{i}";
            var action = config.TryGetValue(key, out var a) ? a : new ButtonAction();
            Buttons.Add(new ButtonViewModel(i, action));
        }
    }
}

public partial class ButtonViewModel : ObservableObject
{
    public int Number { get; }

    [ObservableProperty] private ButtonAction _action;
    [ObservableProperty] private string _typeLabel = "NONE";
    [ObservableProperty] private Brush _typeBrush = Brushes.Gray;
    [ObservableProperty] private string _preview = "";

    public ButtonViewModel(int number, ButtonAction action)
    {
        Number = number;
        _action = action;
        Refresh();
    }

    public void SetAction(ButtonAction action)
    {
        Action = action;
        Refresh();
    }

    private void Refresh()
    {
        TypeLabel = TypeLabels.TryGetValue(Action.Type, out var l) ? l : "NONE";
        TypeBrush = TypeColors.TryGetValue(Action.Type, out var c)
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(c))
            : Brushes.Gray;
        Preview = BuildPreview(Action);
    }

    private static string BuildPreview(ButtonAction a) => a.Type switch
    {
        "exe"            => Shorten(a.Focus?.Length > 0 ? a.Focus : a.Value),
        "homeassistant"  => $"{Shorten(a.Value, 12)} {a.HaAction ?? "toggle"}",
        "discord_join"   => a.Value.Length > 0 ? $"ID …{a.Value[^Math.Min(6, a.Value.Length)..]}" : "",
        _                => Shorten(a.Value),
    };

    private static string Shorten(string? s, int max = 18)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Replace("https://", "").Replace("http://", "");
        return s.Length <= max ? s : s[..(max - 1)] + "…";
    }

    private static readonly Dictionary<string, string> TypeLabels = new()
    {
        ["link"] = "LINK", ["exe"] = "FILE",
        ["play_pause"] = "PLAY", ["next_track"] = "NEXT", ["prev_track"] = "PREV", ["stop"] = "STOP",
        ["mute"] = "MUTE", ["hotkey"] = "KEY",
        ["discord_mute"] = "MUTE", ["discord_deafen"] = "DEAF", ["discord_leave"] = "LEAVE",
        ["discord_video"] = "CAM", ["discord_screenshare"] = "STREAM", ["discord_join"] = "JOIN",
        ["homeassistant"] = "HA",
        ["audio_device"] = "AUDIO", ["audio_toggle"] = "AUDIO",
        ["none"] = "NONE",
    };

    private static readonly Dictionary<string, string> TypeColors = new()
    {
        ["link"] = "#00AACC", ["exe"] = "#22AA44",
        ["play_pause"] = "#3366CC", ["next_track"] = "#3366CC", ["prev_track"] = "#3366CC", ["stop"] = "#3366CC",
        ["mute"] = "#9933CC", ["hotkey"] = "#CCAA00",
        ["discord_mute"] = "#7B68EE", ["discord_deafen"] = "#7B68EE", ["discord_leave"] = "#CC3333",
        ["discord_video"] = "#5B9BD5", ["discord_screenshare"] = "#9B59B6", ["discord_join"] = "#27AE60",
        ["homeassistant"] = "#FF9800",
        ["audio_device"] = "#00AABB", ["audio_toggle"] = "#00AABB",
        ["none"] = "#555555",
    };
}
