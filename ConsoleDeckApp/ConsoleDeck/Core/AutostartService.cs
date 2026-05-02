using Microsoft.Win32;

namespace ConsoleDeck.Core;

public static class AutostartService
{
    private const string RunKey  = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "ConsoleDeck";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue(AppName) != null;
    }

    public static void SetEnabled(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key == null) return;

        if (enable)
        {
            var exe = Environment.ProcessPath ?? AppContext.BaseDirectory + "ConsoleDeck.exe";
            key.SetValue(AppName, $"\"{exe}\" --silent");
        }
        else
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
        }
    }
}
