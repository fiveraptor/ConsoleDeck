using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleDeck.Models;

namespace ConsoleDeck.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private string _discordClientId = "";
    [ObservableProperty] private string _discordClientSecret = "";
    [ObservableProperty] private bool _discordHasToken;

    [ObservableProperty] private string _haUrl = "";
    [ObservableProperty] private string _haToken = "";
    [ObservableProperty] private string _haTestResult = "";
    [ObservableProperty] private bool _haTesting;

    public SettingsViewModel()
    {
        var discord = App.Config.LoadDiscordAuth();
        DiscordClientId = discord.ClientId;
        DiscordClientSecret = discord.ClientSecret;
        DiscordHasToken = !string.IsNullOrEmpty(discord.AccessToken);

        var ha = App.Config.LoadHaAuth();
        HaUrl = ha.Url;
        HaToken = ha.Token;
    }

    [RelayCommand]
    private void SaveDiscord()
    {
        var existing = App.Config.LoadDiscordAuth();
        var changed = existing.ClientId != DiscordClientId || existing.ClientSecret != DiscordClientSecret;
        existing.ClientId = DiscordClientId;
        existing.ClientSecret = DiscordClientSecret;
        if (changed)
        {
            existing.AccessToken = null;
            existing.RefreshToken = null;
            DiscordHasToken = false;
        }
        App.Config.SaveDiscordAuth(existing);
    }

    [RelayCommand]
    private void SaveHa()
    {
        App.Config.SaveHaAuth(new HaAuth { Url = HaUrl, Token = HaToken });
        HaTestResult = "";
    }

    [RelayCommand]
    private async Task TestHa()
    {
        HaTesting = true;
        HaTestResult = "";
        var ok = await App.HomeAssistant.TestConnectionAsync(HaUrl, HaToken);
        HaTestResult = ok ? "✓ Verbunden" : "✗ Fehler – URL oder Token prüfen";
        HaTesting = false;
    }
}
