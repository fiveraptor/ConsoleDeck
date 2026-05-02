using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ConsoleDeck.Models;

namespace ConsoleDeck.Views;

public partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
        App.Discord.StatusChanged += OnDiscordStatusChanged;
        UpdateDiscordBadge(App.Discord.Status);
    }

    private void LoadSettings()
    {
        var discord = App.Config.LoadDiscordAuth();
        TbDiscordId.Text = discord.ClientId;
        PbDiscordSecret.Password = discord.ClientSecret;

        var ha = App.Config.LoadHaAuth();
        TbHaUrl.Text = ha.Url;
        PbHaToken.Password = ha.Token;
    }

    private void OnDiscordStatusChanged(Core.DiscordStatus status)
        => Dispatcher.Invoke(() => UpdateDiscordBadge(status));

    private void UpdateDiscordBadge(Core.DiscordStatus status)
    {
        (DiscordStatusText.Text, DiscordStatusBadge.Background) = status switch
        {
            Core.DiscordStatus.Connected    => ("Verbunden",    new SolidColorBrush(Color.FromRgb(30, 70, 40))),
            Core.DiscordStatus.Connecting   => ("Verbindet...", new SolidColorBrush(Color.FromRgb(60, 50, 20))),
            _                               => ("Nicht verbunden", new SolidColorBrush(Color.FromRgb(50, 30, 30))),
        };
        DiscordStatusText.Foreground = status switch
        {
            Core.DiscordStatus.Connected  => new SolidColorBrush(Color.FromRgb(100, 220, 120)),
            Core.DiscordStatus.Connecting => new SolidColorBrush(Colors.Gold),
            _ => new SolidColorBrush(Color.FromRgb(200, 100, 100)),
        };
    }

    private void SaveDiscord_Click(object sender, RoutedEventArgs e)
    {
        var existing = App.Config.LoadDiscordAuth();
        var newId = TbDiscordId.Text.Trim();
        var newSecret = PbDiscordSecret.Password;
        var changed = existing.ClientId != newId || existing.ClientSecret != newSecret;
        existing.ClientId = newId;
        existing.ClientSecret = newSecret;
        if (changed)
        {
            existing.AccessToken = null;
            existing.RefreshToken = null;
        }
        App.Config.SaveDiscordAuth(existing);
        ShowToast("Discord-Einstellungen gespeichert.");
    }

    private void SaveHa_Click(object sender, RoutedEventArgs e)
    {
        App.Config.SaveHaAuth(new HaAuth
        {
            Url = TbHaUrl.Text.Trim().TrimEnd('/'),
            Token = PbHaToken.Password,
        });
        HaTestResult.Text = "";
        ShowToast("Home Assistant Einstellungen gespeichert.");
    }

    private async void TestHa_Click(object sender, RoutedEventArgs e)
    {
        HaTestResult.Text = "Teste...";
        HaTestResult.Foreground = new SolidColorBrush(Colors.Gray);
        var ok = await App.HomeAssistant.TestConnectionAsync(
            TbHaUrl.Text.Trim().TrimEnd('/'),
            PbHaToken.Password);
        HaTestResult.Text = ok ? "✓ Verbunden" : "✗ Fehler";
        HaTestResult.Foreground = ok
            ? new SolidColorBrush(Color.FromRgb(100, 220, 120))
            : new SolidColorBrush(Color.FromRgb(220, 80, 80));
    }

    private void ShowToast(string msg)
    {
        if (Window.GetWindow(this) is MainWindow mw)
            mw.ShowToast(msg);
    }
}
