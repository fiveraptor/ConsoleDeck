using System.Diagnostics;
using System.Runtime.InteropServices;
using ConsoleDeck.Models;

namespace ConsoleDeck.Core;

public class ActionExecutor
{
    private readonly DiscordService _discord;
    private readonly HomeAssistantService _ha;

    private int _lastVolumeValue;

    public ActionExecutor(DiscordService discord, HomeAssistantService ha)
    {
        _discord = discord;
        _ha = ha;
    }

    public void Execute(ButtonAction action)
    {
        switch (action.Type)
        {
            case "link" when action.Value.Length > 0:
                Process.Start(new ProcessStartInfo(action.Value) { UseShellExecute = true });
                break;
            case "exe" when action.Value.Length > 0:
                if (!string.IsNullOrEmpty(action.Focus) && FocusWindow(action.Focus))
                    break;
                Process.Start(new ProcessStartInfo(action.Value) { UseShellExecute = true });
                break;
            case "play_pause":  SimulateKey(0xB3); break;
            case "next_track":  SimulateKey(0xB0); break;
            case "prev_track":  SimulateKey(0xB1); break;
            case "stop":        SimulateKey(0xB2); break;
            case "mute":        SimulateKey(0xAD); break;
            case "hotkey" when action.Value.Length > 0:
                SimulateHotkey(action.Value);
                break;
            case "discord_mute":        _discord.ToggleMute(); break;
            case "discord_deafen":      _discord.ToggleDeafen(); break;
            case "discord_leave":       _discord.LeaveChannel(); break;

            case "discord_join" when action.Value.Length > 0:
                _discord.JoinChannel(action.Value);
                break;
            case "homeassistant" when action.Value.Length > 0:
                _ha.CallService(action.Value, action.HaAction ?? "toggle");
                break;
            case "audio_device" when action.Value.Length > 0:
                AudioService.SetDefaultDevice(action.Value);
                break;
            case "audio_toggle" when action.Value.Contains('|'):
                var pts = action.Value.Split('|', 2);
                AudioService.ToggleDevice(pts[0], pts[1]);
                break;
        }
    }

    public void HandleVolume(string raw)
    {
        if (!int.TryParse(raw, out var val)) return;
        var delta = val - _lastVolumeValue;
        _lastVolumeValue = val;
        if (delta == 0) return;
        var vk = delta > 0 ? 0xAF : 0xAE;
        for (int i = 0; i < Math.Abs(delta); i++)
            SimulateKey(vk);
    }

    public void HandleMute()  => SimulateKey(0xAD);
    public void HandleMedia() => SimulateKey(0xB3);

    private static void SimulateKey(int vk)
    {
        const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        const uint KEYEVENTF_KEYUP = 0x0002;
        keybd_event((byte)vk, 0, KEYEVENTF_EXTENDEDKEY, 0);
        keybd_event((byte)vk, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
    }

    private static readonly Dictionary<string, int> VkMap = new()
    {
        ["ctrl"] = 0x11, ["control"] = 0x11,
        ["shift"] = 0x10, ["alt"] = 0x12, ["win"] = 0x5B,
        ["space"] = 0x20, ["enter"] = 0x0D, ["tab"] = 0x09,
        ["esc"] = 0x1B, ["escape"] = 0x1B, ["backspace"] = 0x08,
        ["del"] = 0x2E, ["delete"] = 0x2E,
        ["up"] = 0x26, ["down"] = 0x28, ["left"] = 0x25, ["right"] = 0x27,
        ["home"] = 0x24, ["end"] = 0x23, ["pageup"] = 0x21, ["pagedown"] = 0x22,
        ["f1"] = 0x70, ["f2"] = 0x71, ["f3"] = 0x72, ["f4"] = 0x73,
        ["f5"] = 0x74, ["f6"] = 0x75, ["f7"] = 0x76, ["f8"] = 0x77,
        ["f9"] = 0x78, ["f10"] = 0x79, ["f11"] = 0x7A, ["f12"] = 0x7B,
    };

    private static void SimulateHotkey(string combo)
    {
        var keys = combo.Split('+').Select(k => k.Trim().ToLower()).ToArray();
        var vks = new List<int>();
        foreach (var key in keys)
        {
            if (VkMap.TryGetValue(key, out var vk))
                vks.Add(vk);
            else if (key.Length == 1)
                vks.Add(char.ToUpper(key[0]));
        }
        if (vks.Count == 0) return;

        var inputs = new INPUT[vks.Count * 2];
        for (int i = 0; i < vks.Count; i++)
            inputs[i] = MakeKeyInput((ushort)vks[i], false);
        for (int i = 0; i < vks.Count; i++)
            inputs[vks.Count + i] = MakeKeyInput((ushort)vks[vks.Count - 1 - i], true);

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT MakeKeyInput(ushort vk, bool keyUp)
    {
        var inp = new INPUT { type = 1 };
        inp.u.ki.wVk = vk;
        inp.u.ki.dwFlags = (uint)(keyUp ? 0x0002 : 0x0001);
        return inp;
    }

    private static bool FocusWindow(string titleHint)
    {
        var hint = titleHint.ToLower();
        var found = IntPtr.Zero;
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd)) return true;
            var len = GetWindowTextLength(hwnd);
            if (len == 0) return true;
            var sb = new System.Text.StringBuilder(len + 1);
            GetWindowText(hwnd, sb, sb.Capacity);
            if (sb.ToString().ToLower().Contains(hint))
            {
                found = hwnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);

        if (found == IntPtr.Zero) return false;
        ShowWindow(found, 9);
        SetForegroundWindow(found);
        return true;
    }

    // P/Invoke declarations
    [DllImport("user32.dll")] static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
    [DllImport("user32.dll")] static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int GetWindowTextLength(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int GetWindowText(IntPtr hwnd, System.Text.StringBuilder sb, int maxCount);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hwnd);

    delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Explicit)]
    struct INPUTUNION { [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)]
    struct INPUT { public uint type; public INPUTUNION u; }
}
