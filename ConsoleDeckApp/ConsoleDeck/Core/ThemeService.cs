using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;
using Wpf.Ui.Appearance;

namespace ConsoleDeck.Core;

public static class ThemeService
{
    private const string RegPath  = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string RegValue = "AppsUseLightTheme";

    public static bool IsDark { get; private set; }
    public static event Action? ThemeChanged;

    public static void Init() => Apply();

    public static void Apply()
    {
        bool dark = !IsWindowsLight();
        IsDark = dark;

        ApplicationThemeManager.Apply(dark ? ApplicationTheme.Dark : ApplicationTheme.Light);

        var dicts = Application.Current.Resources.MergedDictionaries;
        for (int i = dicts.Count - 1; i >= 0; i--)
        {
            var src = dicts[i].Source?.OriginalString ?? "";
            if (src.Contains("Themes/") && (src.EndsWith("Light.xaml") || src.EndsWith("Dark.xaml")))
            {
                dicts.RemoveAt(i);
                break;
            }
        }
        dicts.Add(new ResourceDictionary
        {
            Source = new Uri($"Themes/{(dark ? "Dark" : "Light")}.xaml", UriKind.Relative)
        });

        ThemeChanged?.Invoke();
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    public static void ApplyTitleBar(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return;
        int dark = IsDark ? 1 : 0;
        DwmSetWindowAttribute(hwnd, 20, ref dark, sizeof(int));
    }

    public static void ApplyTitleBar(Window window)
        => ApplyTitleBar(new WindowInteropHelper(window).Handle);

    private static bool IsWindowsLight()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegPath);
            return key?.GetValue(RegValue) is 1;
        }
        catch { return true; }
    }
}
