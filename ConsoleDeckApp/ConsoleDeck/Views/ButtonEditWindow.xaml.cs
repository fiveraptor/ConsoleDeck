using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using ConsoleDeck.Core;
using ConsoleDeck.Models;
using Microsoft.Win32;
using AudioDeviceItem = System.Windows.Controls.ComboBoxItem;

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
        LoadAudioDevices();
        LoadCurrentAction();
    }

    private void LoadAudioDevices()
    {
        var devices = AudioService.GetOutputDevices();
        foreach (var d in devices)
        {
            CbAudioDevice.Items.Add(new AudioDeviceItem { Content = d.Name, Tag = d.Id });
            CbAudioToggle1.Items.Add(new AudioDeviceItem { Content = d.Name, Tag = d.Id });
            CbAudioToggle2.Items.Add(new AudioDeviceItem { Content = d.Name, Tag = d.Id });
        }
        if (devices.Count > 0)
        {
            CbAudioDevice.SelectedIndex = 0;
            CbAudioToggle1.SelectedIndex = 0;
            CbAudioToggle2.SelectedIndex = devices.Count > 1 ? 1 : 0;
        }
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
        TbDiscordChannelId.Text = action.Value;

        var haAction = action.HaAction ?? "toggle";
        foreach (ComboBoxItem item in CbHaAction.Items)
            if (item.Content?.ToString() == haAction)
                item.IsSelected = true;

        switch (action.Type)
        {
            case "audio_device":
                SelectById(CbAudioDevice, action.Value);
                break;
            case "audio_toggle":
                var parts = action.Value.Split('|', 2);
                if (parts.Length >= 1) SelectById(CbAudioToggle1, parts[0]);
                if (parts.Length >= 2) SelectById(CbAudioToggle2, parts[1]);
                break;
        }
    }

    private static void SelectById(ComboBox cb, string id)
    {
        foreach (AudioDeviceItem item in cb.Items)
            if (item.Tag?.ToString() == id)
            {
                cb.SelectedItem = item;
                return;
            }
    }

    private static string SelectedId(ComboBox cb) =>
        (cb.SelectedItem as AudioDeviceItem)?.Tag?.ToString() ?? "";

    private void SelectType(string type)
    {
        var radios = new Dictionary<string, RadioButton>
        {
            ["link"] = RbLink, ["exe"] = RbExe,
            ["play_pause"] = RbPlay, ["next_track"] = RbNext, ["prev_track"] = RbPrev, ["stop"] = RbStop,
            ["mute"] = RbMute, ["hotkey"] = RbHotkey,
            ["discord_mute"] = RbDMute, ["discord_deafen"] = RbDDeaf, ["discord_leave"] = RbDLeave,
            ["discord_video"] = RbDVideo, ["discord_screenshare"] = RbDStream, ["discord_join"] = RbDJoin,
            ["homeassistant"] = RbHa,
            ["audio_device"] = RbAudioDev, ["audio_toggle"] = RbAudioToggle,
            ["none"] = RbNone,
        };
        if (radios.TryGetValue(type, out var rb))
            rb.IsChecked = true;
        else
            RbNone.IsChecked = true;
    }

    private string GetSelectedType()
    {
        var radios = new[] { RbLink, RbExe, RbPlay, RbNext, RbPrev, RbStop,
                             RbMute, RbHotkey, RbDMute, RbDDeaf, RbDLeave,
                             RbDVideo, RbDStream, RbDJoin,
                             RbHa, RbAudioDev, RbAudioToggle, RbNone };
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
        PanelLink.Visibility         = Visibility.Collapsed;
        PanelExe.Visibility          = Visibility.Collapsed;
        PanelHotkey.Visibility       = Visibility.Collapsed;
        PanelHa.Visibility           = Visibility.Collapsed;
        PanelAudioDevice.Visibility  = Visibility.Collapsed;
        PanelAudioToggle.Visibility  = Visibility.Collapsed;
        PanelDiscordJoin.Visibility  = Visibility.Collapsed;
        PanelNoInput.Visibility      = Visibility.Collapsed;

        switch (type)
        {
            case "link":              PanelLink.Visibility        = Visibility.Visible; break;
            case "exe":               PanelExe.Visibility         = Visibility.Visible; break;
            case "hotkey":            PanelHotkey.Visibility      = Visibility.Visible; break;
            case "homeassistant":     PanelHa.Visibility          = Visibility.Visible; break;
            case "audio_device":      PanelAudioDevice.Visibility = Visibility.Visible; break;
            case "audio_toggle":      PanelAudioToggle.Visibility = Visibility.Visible; break;
            case "discord_join":      PanelDiscordJoin.Visibility = Visibility.Visible; break;
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
        "discord_mute"        => "Schaltet das Mikrofon in Discord stumm oder ein.",
        "discord_deafen"      => "Aktiviert / deaktiviert das Deafen in Discord.",
        "discord_leave"       => "Verlässt den aktuellen Discord-Sprachkanal.",
        "discord_video"       => "Schaltet die Kamera in Discord ein oder aus.\nHinweis: Funktioniert nur im aktiven Voice-Call.",
        "discord_screenshare" => "Startet oder beendet den Screen-Share in Discord.\nHinweis: Funktioniert nur im aktiven Voice-Call.",
        "none"                => "Dieser Button hat keine Aktion.",
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
            case "audio_device":
                action.Value = SelectedId(CbAudioDevice);
                break;
            case "audio_toggle":
                var id1 = SelectedId(CbAudioToggle1);
                var id2 = SelectedId(CbAudioToggle2);
                if (id1.Length > 0 && id2.Length > 0)
                    action.Value = $"{id1}|{id2}";
                break;
            case "discord_join":
                action.Value = TbDiscordChannelId.Text.Trim();
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

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ThemeService.ApplyTitleBar(this);
        ThemeService.ThemeChanged += RefreshTitleBar;
    }

    protected override void OnClosed(EventArgs e)
    {
        ThemeService.ThemeChanged -= RefreshTitleBar;
        base.OnClosed(e);
    }

    private void RefreshTitleBar() => Dispatcher.Invoke(() => ThemeService.ApplyTitleBar(this));
}
