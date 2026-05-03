using System.Threading;
using System.Windows;
using ConsoleDeck.Core;
using H.NotifyIcon;

namespace ConsoleDeck;

public partial class App : Application
{
    private static readonly Mutex _mutex = new(false, "ConsoleDeck-SingleInstance");
    private static readonly EventWaitHandle _showEvent = new(false, EventResetMode.AutoReset, "ConsoleDeck-ShowWindow");
    private bool _mutexOwned;

    public static ConfigService Config { get; } = new();
    public static SerialService Serial { get; } = new();
    public static DiscordService Discord { get; } = new();
    public static HomeAssistantService HomeAssistant { get; } = new();

    private TaskbarIcon? _trayIcon;

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            _mutexOwned = _mutex.WaitOne(TimeSpan.Zero, false);
        }
        catch (AbandonedMutexException)
        {
            _mutexOwned = true;
        }

        if (!_mutexOwned)
        {
            _showEvent.Set(); // Signal the running instance to bring its window to front
            Current.Shutdown();
            return;
        }

        bool silent = e.Args.Contains("--silent");

        base.OnStartup(e);
        ThemeService.Init();

        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        _trayIcon.ForceCreate(false);

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;

        if (!silent)
        {
            mainWindow.Show();
            mainWindow.Activate();
        }

        _ = Task.Run(() =>
        {
            try { while (true) { _showEvent.WaitOne(); Dispatcher.Invoke(ShowMainWindow); } }
            catch (ObjectDisposedException) { }
        });

        Config.StartWatching();
        Serial.Start();
        Serial.MessageReceived += OnSerialMessage;

        Discord.SetSaveCallback(a => Config.SaveDiscordAuth(a));
        var auth = Config.LoadDiscordAuth();
        if (!string.IsNullOrEmpty(auth.ClientId))
        {
            await Discord.ConnectAsync(auth);
            Discord.StartWatchdog(auth);
        }
    }

    private void OnSerialMessage(string line)
    {
        var config = Config.LoadConfig();
        if (line.StartsWith("VOLUME_"))
            ActionExecutorInstance.HandleVolume(line["VOLUME_".Length..]);
        else if (line == "MUTE")
            ActionExecutorInstance.HandleMute();
        else if (line == "MEDIA")
            ActionExecutorInstance.HandleMedia();
        else if (config.TryGetValue(line, out var action))
            ActionExecutorInstance.Execute(action);
    }

    private static readonly ActionExecutor ActionExecutorInstance = new(Discord, HomeAssistant);

    protected override void OnExit(ExitEventArgs e)
    {
        Config.StopWatching();
        Serial.Stop();
        Discord.Disconnect();
        _trayIcon?.Dispose();
        _showEvent.Dispose();
        if (_mutexOwned) _mutex.ReleaseMutex();
        _mutex.Dispose();
        base.OnExit(e);
    }

    private void TrayIcon_DoubleClick(object sender, RoutedEventArgs e) => ShowMainWindow();
    private void TrayOpen_Click(object sender, RoutedEventArgs e) => ShowMainWindow();
    private void TrayQuit_Click(object sender, RoutedEventArgs e) => Current.Shutdown();

    private void ShowMainWindow()
    {
        if (MainWindow is MainWindow mw)
        {
            mw.Show();
            mw.WindowState = WindowState.Normal;
            mw.Activate();
        }
    }
}
