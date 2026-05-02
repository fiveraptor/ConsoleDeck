using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ConsoleDeck.Core;
using ConsoleDeck.Models;

namespace ConsoleDeck.Views;

public partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
        App.Discord.StatusChanged += OnDiscordStatusChanged;
        ThemeService.ThemeChanged += OnThemeChanged;
        UpdateDiscordBadge(App.Discord.Status);
    }

    private void LoadSettings()
    {
        AutostartToggle.IsChecked = AutostartService.IsEnabled();

        var discord = App.Config.LoadDiscordAuth();
        TbDiscordId.Text = discord.ClientId;
        PbDiscordSecret.Password = discord.ClientSecret;

        var ha = App.Config.LoadHaAuth();
        TbHaUrl.Text = ha.Url;
        PbHaToken.Password = ha.Token;
    }

    private void AutostartToggle_Changed(object sender, RoutedEventArgs e)
    {
        var enabled = AutostartToggle.IsChecked == true;
        AutostartService.SetEnabled(enabled);
        ShowToast(enabled ? "Autostart aktiviert." : "Autostart deaktiviert.");
    }

    private void OnDiscordStatusChanged(DiscordStatus status)
        => Dispatcher.Invoke(() => UpdateDiscordBadge(status));

    private void OnThemeChanged()
        => Dispatcher.Invoke(() => UpdateDiscordBadge(App.Discord.Status));

    private void UpdateDiscordBadge(DiscordStatus status)
    {
        bool dark = ThemeService.IsDark;
        (DiscordStatusText.Text, DiscordStatusBadge.Background, DiscordStatusText.Foreground) = status switch
        {
            DiscordStatus.Connected  => ("Verbunden",
                Brush(dark ? (30, 70, 40)   : (223, 246, 221)),
                Brush(dark ? (100, 210, 120) : (16, 124, 16))),
            DiscordStatus.Connecting => ("Verbindet…",
                Brush(dark ? (60, 50, 20)   : (255, 244, 206)),
                Brush(dark ? (210, 170, 60)  : (131, 92, 0))),
            _                        => ("Nicht verbunden",
                Brush(dark ? (50, 50, 50)   : (243, 243, 243)),
                Brush(dark ? (140, 140, 140) : (92, 92, 92))),
        };
    }

    private static SolidColorBrush Brush((int r, int g, int b) c)
        => new(Color.FromRgb((byte)c.r, (byte)c.g, (byte)c.b));

    private void SaveDiscord_Click(object sender, RoutedEventArgs e)
    {
        var existing  = App.Config.LoadDiscordAuth();
        var newId     = TbDiscordId.Text.Trim();
        var newSecret = PbDiscordSecret.Password;
        var changed   = existing.ClientId != newId || existing.ClientSecret != newSecret;
        existing.ClientId     = newId;
        existing.ClientSecret = newSecret;
        if (changed)
        {
            existing.AccessToken  = null;
            existing.RefreshToken = null;
        }
        App.Config.SaveDiscordAuth(existing);
        ShowToast("Discord-Einstellungen gespeichert. Discord-Popup bestätigen…");

        if (!string.IsNullOrEmpty(newId))
        {
            App.Discord.Disconnect();
            var auth = App.Config.LoadDiscordAuth();
            _ = App.Discord.ConnectAsync(auth, allowOAuthFlow: true);
            App.Discord.StartWatchdog(auth);
        }
    }

    private void SaveHa_Click(object sender, RoutedEventArgs e)
    {
        App.Config.SaveHaAuth(new HaAuth
        {
            Url   = TbHaUrl.Text.Trim().TrimEnd('/'),
            Token = PbHaToken.Password,
        });
        HaTestResult.Text = "";
        ShowToast("Home Assistant Einstellungen gespeichert.");
    }

    private async void TestHa_Click(object sender, RoutedEventArgs e)
    {
        bool dark = ThemeService.IsDark;
        HaTestResult.Text       = "Teste…";
        HaTestResult.Foreground = Brush(dark ? (140, 140, 140) : (92, 92, 92));

        var ok = await App.HomeAssistant.TestConnectionAsync(
            TbHaUrl.Text.Trim().TrimEnd('/'),
            PbHaToken.Password);

        HaTestResult.Text       = ok ? "✓ Verbunden" : "✗ Fehler";
        HaTestResult.Foreground = ok
            ? Brush(dark ? (100, 210, 120) : (16, 124, 16))
            : Brush(dark ? (210, 90, 90)   : (168, 0, 0));
    }

    private void ShowToast(string msg)
    {
        if (Window.GetWindow(this) is MainWindow mw) mw.ShowToast(msg);
    }
}
