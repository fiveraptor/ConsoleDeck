using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ConsoleDeck.Core;
using ConsoleDeck.Views;

namespace ConsoleDeck;

public partial class MainWindow : Window
{
    private readonly DashboardPage _dashPage = new();
    private readonly SettingsPage _settingsPage = new();
    private DispatcherTimer? _toastTimer;

    public MainWindow()
    {
        InitializeComponent();

        App.Serial.StatusChanged += OnArduinoStatus;
        App.Discord.StatusChanged += OnDiscordStatus;
        OnArduinoStatus(App.Serial.Status);
        OnDiscordStatus(App.Discord.Status);

        ContentFrame.Navigate(_dashPage);
        NavList.SelectedIndex = 0;
    }

    private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ContentFrame == null) return;
        if (NavList.SelectedItem is not ListBoxItem item) return;
        Page? page = item.Tag?.ToString() switch
        {
            "dashboard" => _dashPage,
            "settings"  => _settingsPage,
            _ => null,
        };
        if (page != null && ContentFrame.Content != page)
            ContentFrame.Navigate(page);
    }

    private void OnArduinoStatus(ConnectionState state)
    {
        Dispatcher.Invoke(() =>
        {
            (ArduinoLed.Fill, ArduinoStatus.Text, ArduinoStatus.Foreground) = state switch
            {
                ConnectionState.Connected => (Brush(80, 200, 100), "Arduino: Verbunden",   Brush(80, 200, 100)),
                ConnectionState.Searching => (new SolidColorBrush(Colors.Gold), "Arduino: Suche...", new SolidColorBrush(Colors.Gold)),
                _                         => (Brush(180, 80, 80), "Arduino: Getrennt",    Brush(180, 80, 80)),
            };
        });
    }

    private void OnDiscordStatus(DiscordStatus state)
    {
        Dispatcher.Invoke(() =>
        {
            (DiscordLed.Fill, DiscordStatusBar.Text, DiscordStatusBar.Foreground) = state switch
            {
                DiscordStatus.Connected  => (Brush(80, 200, 100), "Discord: Verbunden",     Brush(80, 200, 100)),
                DiscordStatus.Connecting => (new SolidColorBrush(Colors.Gold), "Discord: Verbindet...", new SolidColorBrush(Colors.Gold)),
                _                        => (Brush(100, 100, 100), "Discord: Nicht verbunden", Brush(100, 100, 100)),
            };
        });
    }

    private static SolidColorBrush Brush(byte r, byte g, byte b)
        => new(Color.FromRgb(r, g, b));

    public void ShowToast(string message, int durationMs = 2500)
    {
        Dispatcher.Invoke(() =>
        {
            ToastText.Text = message;
            ToastBorder.Visibility = Visibility.Visible;
            _toastTimer?.Stop();
            _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
            _toastTimer.Tick += (_, _) =>
            {
                ToastBorder.Visibility = Visibility.Collapsed;
                _toastTimer.Stop();
            };
            _toastTimer.Start();
        });
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
