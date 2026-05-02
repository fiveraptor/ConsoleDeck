using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
        ThemeService.ThemeChanged += RefreshStatusColors;
        OnArduinoStatus(App.Serial.Status);
        OnDiscordStatus(App.Discord.Status);

        ContentFrame.Navigate(_dashPage);
        NavList.SelectedIndex = 0;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
        ApplyTitleBarTheme(hwnd);
    }

    private void ApplyTitleBarTheme(IntPtr hwnd = default)
    {
        if (hwnd == IntPtr.Zero) hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;
        int dark = ThemeService.IsDark ? 1 : 0;
        DwmSetWindowAttribute(hwnd, 20, ref dark, sizeof(int));
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0x001A) // WM_SETTINGCHANGE
        {
            var setting = Marshal.PtrToStringUni(lParam);
            if (setting == "ImmersiveColorSet")
                ThemeService.Apply();
        }
        return IntPtr.Zero;
    }

    private void RefreshStatusColors()
    {
        Dispatcher.Invoke(ApplyTitleBarTheme);
        OnArduinoStatus(App.Serial.Status);
        OnDiscordStatus(App.Discord.Status);
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
        if (page == null || ContentFrame.Content == page) return;

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(100));
        fadeOut.Completed += (_, _) =>
        {
            ContentFrame.Navigate(page);
            var tf = (TranslateTransform)ContentFrame.RenderTransform;
            tf.Y = 10;
            tf.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(10, 0, TimeSpan.FromMilliseconds(220)) { DecelerationRatio = 1.0 });
            ContentFrame.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
        };
        ContentFrame.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }

    private void OnArduinoStatus(ConnectionState state)
    {
        Dispatcher.Invoke(() =>
        {
            bool dark = ThemeService.IsDark;
            (ArduinoLed.Fill, ArduinoStatus.Text, ArduinoStatus.Foreground) = state switch
            {
                ConnectionState.Connected =>
                    (Led(dark, 100, 210, 120, 40, 160, 60),
                     "Arduino: Verbunden",
                     Led(dark, 100, 210, 120, 40, 160, 60)),
                ConnectionState.Searching =>
                    (Led(dark, 210, 170, 60, 160, 120, 0),
                     "Arduino: Suche...",
                     Led(dark, 210, 170, 60, 130, 95, 0)),
                _ =>
                    (Led(dark, 140, 140, 140, 100, 100, 100),
                     "Arduino: Getrennt",
                     Led(dark, 140, 140, 140, 100, 100, 100)),
            };
        });
    }

    private void OnDiscordStatus(DiscordStatus state)
    {
        Dispatcher.Invoke(() =>
        {
            bool dark = ThemeService.IsDark;
            (DiscordLed.Fill, DiscordStatusBar.Text, DiscordStatusBar.Foreground) = state switch
            {
                DiscordStatus.Connected =>
                    (Led(dark, 100, 210, 120, 40, 160, 60),
                     "Discord: Verbunden",
                     Led(dark, 100, 210, 120, 40, 160, 60)),
                DiscordStatus.Connecting =>
                    (Led(dark, 210, 170, 60, 160, 120, 0),
                     "Discord: Verbindet...",
                     Led(dark, 210, 170, 60, 130, 95, 0)),
                _ =>
                    (Led(dark, 140, 140, 140, 100, 100, 100),
                     "Discord: Nicht verbunden",
                     Led(dark, 140, 140, 140, 100, 100, 100)),
            };
        });
    }

    // Returns dark-mode or light-mode brush
    private static SolidColorBrush Led(bool dark,
        byte rd, byte gd, byte bd,
        byte rl, byte gl, byte bl)
        => dark
            ? new SolidColorBrush(Color.FromRgb(rd, gd, bd))
            : new SolidColorBrush(Color.FromRgb(rl, gl, bl));

    public void ShowToast(string message, int durationMs = 2500)
    {
        Dispatcher.Invoke(() =>
        {
            ToastText.Text = message;
            _toastTimer?.Stop();

            ToastBorder.BeginAnimation(UIElement.OpacityProperty, null);
            ToastBorder.Visibility = Visibility.Visible;

            ToastBorder.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));

            _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
            _toastTimer.Tick += (_, _) =>
            {
                _toastTimer!.Stop();
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
                fadeOut.Completed += (_, _) => ToastBorder.Visibility = Visibility.Collapsed;
                ToastBorder.BeginAnimation(UIElement.OpacityProperty, fadeOut);
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
