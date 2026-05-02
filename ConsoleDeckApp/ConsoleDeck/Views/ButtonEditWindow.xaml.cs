using System.Windows;
using System.Windows.Controls;
using ConsoleDeck.Models;
using Microsoft.Win32;

namespace ConsoleDeck.Views;

public partial class ButtonEditWindow : Window
{
    private readonly int _buttonNumber;
    private readonly string _buttonKey;

    public ButtonEditWindow(int buttonNumber)
    {
        _buttonNumber = buttonNumber;
        _buttonKey = $"BUTTON_{buttonNumber}";
        InitializeComponent();
        TitleText.Text = $"Button {buttonNumber} konfigurieren";
        LoadCurrentAction();
    }

    private void LoadCurrentAction()
    {
        var config = App.Config.LoadConfig();
        if (!config.TryGetValue(_buttonKey, out var action))
            action = new ButtonAction();

        SelectType(action.Type);

        TbUrl.Text = action.Value;
        TbExePath.Text = action.Value;
        TbFocus.Text = action.Focus ?? "";
        TbHotkey.Text = action.Value;
        TbEntityId.Text = action.Value;

        var haAction = action.HaAction ?? "toggle";
        foreach (ComboBoxItem item in CbHaAction.Items)
            if (item.Content?.ToString() == haAction)
                item.IsSelected = true;
    }

    private void SelectType(string type)
    {
        var radios = new Dictionary<string, RadioButton>
        {
            ["link"] = RbLink, ["exe"] = RbExe,
            ["play_pause"] = RbPlay, ["next_track"] = RbNext, ["prev_track"] = RbPrev, ["stop"] = RbStop,
            ["mute"] = RbMute, ["hotkey"] = RbHotkey,
            ["discord_mute"] = RbDMute, ["discord_deafen"] = RbDDeaf, ["discord_leave"] = RbDLeave,
            ["homeassistant"] = RbHa, ["none"] = RbNone,
        };
        if (radios.TryGetValue(type, out var rb))
            rb.IsChecked = true;
        else
            RbNone.IsChecked = true;
    }

    private string GetSelectedType()
    {
        var radios = new[] { RbLink, RbExe, RbPlay, RbNext, RbPrev, RbStop,
                             RbMute, RbHotkey, RbDMute, RbDDeaf, RbDLeave, RbHa, RbNone };
        foreach (var rb in radios)
            if (rb.IsChecked == true)
                return rb.Tag?.ToString() ?? "none";
        return "none";
    }

    private void TypeChanged(object sender, RoutedEventArgs e)
    {
        if (PanelLink == null) return; // guard: panels not yet initialized during XAML load
        if (sender is not RadioButton rb) return;
        ShowPanel(rb.Tag?.ToString() ?? "none");
    }

    private void ShowPanel(string type)
    {
        PanelLink.Visibility    = Visibility.Collapsed;
        PanelExe.Visibility     = Visibility.Collapsed;
        PanelHotkey.Visibility  = Visibility.Collapsed;
        PanelHa.Visibility      = Visibility.Collapsed;
        PanelNoInput.Visibility = Visibility.Collapsed;

        switch (type)
        {
            case "link":         PanelLink.Visibility    = Visibility.Visible; break;
            case "exe":          PanelExe.Visibility     = Visibility.Visible; break;
            case "hotkey":       PanelHotkey.Visibility  = Visibility.Visible; break;
            case "homeassistant": PanelHa.Visibility     = Visibility.Visible; break;
            default:
                PanelNoInput.Visibility = Visibility.Visible;
                TbNoInputHint.Text = GetNoInputHint(type);
                break;
        }
    }

    private static string GetNoInputHint(string type) => type switch
    {
        "play_pause"     => "Sendet den Play/Pause-Medienkey.",
        "next_track"     => "Sendet den 'Nächster Titel'-Medienkey.",
        "prev_track"     => "Sendet den 'Vorheriger Titel'-Medienkey.",
        "stop"           => "Sendet den Stop-Medienkey.",
        "mute"           => "Schaltet die System-Stummschaltung ein/aus.",
        "discord_mute"   => "Schaltet das Mikrofon in Discord stumm oder ein.",
        "discord_deafen" => "Aktiviert / deaktiviert das Deafen in Discord.",
        "discord_leave"  => "Verlässt den aktuellen Discord-Sprachkanal.",
        "none"           => "Dieser Button hat keine Aktion.",
        _ => "",
    };

    private void BrowseExe_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Ausführbare Dateien|*.exe;*.lnk;*.bat;*.cmd|Alle Dateien|*.*",
            Title = "Datei auswählen",
        };
        if (dlg.ShowDialog(this) == true)
            TbExePath.Text = dlg.FileName;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var type = GetSelectedType();
        var action = new ButtonAction { Type = type };

        switch (type)
        {
            case "link":
                action.Value = TbUrl.Text.Trim();
                break;
            case "exe":
                action.Value = TbExePath.Text.Trim();
                var focus = TbFocus.Text.Trim();
                if (focus.Length > 0) action.Focus = focus;
                break;
            case "hotkey":
                action.Value = TbHotkey.Text.Trim().ToLower();
                break;
            case "homeassistant":
                action.Value = TbEntityId.Text.Trim();
                action.HaAction = (CbHaAction.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "toggle";
                break;
        }

        var config = App.Config.LoadConfig();
        config[_buttonKey] = action;
        App.Config.SaveConfig(config);

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
