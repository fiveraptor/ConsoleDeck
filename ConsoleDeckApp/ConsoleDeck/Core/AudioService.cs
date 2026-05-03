using System.Runtime.InteropServices;

namespace ConsoleDeck.Core;

public record AudioDevice(string Id, string Name);

public static class AudioService
{
    private const uint DEVICE_STATE_ACTIVE = 1;
    private const uint STGM_READ = 0;

    private static readonly PROPERTYKEY FriendlyNameKey = new()
    {
        fmtid = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"),
        pid = 14
    };

    public static List<AudioDevice> GetOutputDevices()
    {
        try
        {
            var result = new List<AudioDevice>();
            var enumerator = (IMMDeviceEnumerator)new MmDeviceEnumeratorCoClass();
            enumerator.EnumAudioEndpoints(0 /* eRender */, DEVICE_STATE_ACTIVE, out var collection);
            collection.GetCount(out uint count);
            for (uint i = 0; i < count; i++)
            {
                collection.Item(i, out var device);
                device.GetId(out string id);
                device.OpenPropertyStore(STGM_READ, out var store);
                store.GetValue(in FriendlyNameKey, out var pv);
                string name = pv.vt == 31 ? (Marshal.PtrToStringUni(pv.pwszVal) ?? id) : id;
                PropVariantClear(ref pv);
                result.Add(new AudioDevice(id, name));
                Marshal.ReleaseComObject(store);
                Marshal.ReleaseComObject(device);
            }
            Marshal.ReleaseComObject(collection);
            Marshal.ReleaseComObject(enumerator);
            return result;
        }
        catch { return []; }
    }

    public static string? GetDefaultOutputDeviceId()
    {
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MmDeviceEnumeratorCoClass();
            enumerator.GetDefaultAudioEndpoint(0 /* eRender */, 1 /* eMultimedia */, out var device);
            device.GetId(out string id);
            Marshal.ReleaseComObject(device);
            Marshal.ReleaseComObject(enumerator);
            return id;
        }
        catch { return null; }
    }

    public static void SetDefaultDevice(string deviceId)
    {
        // Try primary IID (CLSID == IID pattern, works on most systems)
        try
        {
            var policy = (IPolicyConfig)new PolicyConfigClientCoClass();
            for (uint role = 0; role < 3; role++) policy.SetDefaultEndpoint(deviceId, role);
            Marshal.ReleaseComObject(policy);
            return;
        }
        catch { }

        // Fallback: alternate IID used on some Windows 7+ systems
        try
        {
            var policy = (IPolicyConfigAlt)new PolicyConfigClientCoClass();
            for (uint role = 0; role < 3; role++) policy.SetDefaultEndpoint(deviceId, role);
            Marshal.ReleaseComObject(policy);
            return;
        }
        catch { }

        // Last resort: probe via raw QueryInterface + unsafe vtable call
        SetDefaultDeviceUnsafe(deviceId);
    }

    private static unsafe void SetDefaultDeviceUnsafe(string deviceId)
    {
        try
        {
            var comObj = new PolicyConfigClientCoClass();
            IntPtr punk = Marshal.GetIUnknownForObject(comObj);

            // Try all known IIDs for IPolicyConfig
            Guid[] candidates = [
                new Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9"),
                new Guid("F8679F50-850A-41CF-9C72-430F290290C8"),
                new Guid("568B9108-44BF-40B4-9006-86AFE5B5A620"),
            ];

            IntPtr ppv = IntPtr.Zero;
            foreach (var g in candidates)
            {
                if (Marshal.QueryInterface(punk, in g, out ppv) == 0)
                    break;
                ppv = IntPtr.Zero;
            }
            Marshal.Release(punk);

            if (ppv == IntPtr.Zero)
                return;

            try
            {
                // SetDefaultEndpoint is at vtable index 13 (3 IUnknown + 10 preceding methods)
                void** vtbl = *(void***)ppv;
                var fn = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint, int>)vtbl[13];
                IntPtr pId = Marshal.StringToHGlobalUni(deviceId);
                try
                {
                    fn(ppv, pId, 0);
                    fn(ppv, pId, 1);
                    fn(ppv, pId, 2);
                }
                finally { Marshal.FreeHGlobal(pId); }
            }
            finally { Marshal.Release(ppv); }

            Marshal.ReleaseComObject(comObj);
        }
        catch { }
    }

    public static void ToggleDevice(string deviceId1, string deviceId2)
    {
        var current = GetDefaultOutputDeviceId();
        SetDefaultDevice(current == deviceId1 ? deviceId2 : deviceId1);
    }

    // ── COM CoClasses ────────────────────────────────────────────────────────

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MmDeviceEnumeratorCoClass { }

    [ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
    private class PolicyConfigClientCoClass { }

    // ── COM Interfaces ───────────────────────────────────────────────────────

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int dataFlow, uint dwStateMask,
            [MarshalAs(UnmanagedType.Interface)] out IMMDeviceCollection ppDevices);
        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role,
            [MarshalAs(UnmanagedType.Interface)] out IMMDevice ppEndpoint);
        [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId,
            [MarshalAs(UnmanagedType.Interface)] out IMMDevice ppDevice);
        [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr pClient);
        [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr pClient);
    }

    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
        [PreserveSig] int GetCount(out uint pcDevices);
        [PreserveSig] int Item(uint nDevice, [MarshalAs(UnmanagedType.Interface)] out IMMDevice ppDevice);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, uint dwClsCtx, IntPtr pActivationParams,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        [PreserveSig] int OpenPropertyStore(uint stgmAccess,
            [MarshalAs(UnmanagedType.Interface)] out IPropertyStore ppProperties);
        [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
        [PreserveSig] int GetState(out uint pdwState);
    }

    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        [PreserveSig] int GetCount(out uint cProps);
        [PreserveSig] int GetAt(uint iProp, out PROPERTYKEY pkey);
        [PreserveSig] int GetValue(in PROPERTYKEY key, out PROPVARIANT pv);
        [PreserveSig] int SetValue(ref PROPERTYKEY key, ref PROPVARIANT pv);
        [PreserveSig] int Commit();
    }

    // Primary IID: CLSID == IID (supported by CPolicyConfigClient on most Windows versions)
    [ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        [PreserveSig] int GetMixFormat([MarshalAs(UnmanagedType.LPWStr)] string p, IntPtr pp);
        [PreserveSig] int GetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string p, [MarshalAs(UnmanagedType.Bool)] bool b, IntPtr pp);
        [PreserveSig] int ResetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string p);
        [PreserveSig] int SetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string p, IntPtr a, IntPtr b);
        [PreserveSig] int GetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string p, [MarshalAs(UnmanagedType.Bool)] bool b, IntPtr a, IntPtr c);
        [PreserveSig] int SetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string p, IntPtr a);
        [PreserveSig] int GetShareMode([MarshalAs(UnmanagedType.LPWStr)] string p, IntPtr a);
        [PreserveSig] int SetShareMode([MarshalAs(UnmanagedType.LPWStr)] string p, IntPtr a);
        [PreserveSig] int GetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string p, [MarshalAs(UnmanagedType.Bool)] bool b, IntPtr k, IntPtr v);
        [PreserveSig] int SetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string p, [MarshalAs(UnmanagedType.Bool)] bool b, IntPtr k, IntPtr v);
        [PreserveSig] int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, uint role);
        [PreserveSig] int SetEndpointVisibility([MarshalAs(UnmanagedType.LPWStr)] string p, [MarshalAs(UnmanagedType.Bool)] bool b);
    }

    // Alternate IID used on Windows 7+ builds where CLSID != IID
    [ComImport, Guid("F8679F50-850A-41CF-9C72-430F290290C8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfigAlt
    {
        [PreserveSig] int GetMixFormat([MarshalAs(UnmanagedType.LPWStr)] string p, IntPtr pp);
        [PreserveSig] int GetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string p, [MarshalAs(UnmanagedType.Bool)] bool b, IntPtr pp);
        [PreserveSig] int ResetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string p);
        [PreserveSig] int SetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string p, IntPtr a, IntPtr b);
        [PreserveSig] int GetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string p, [MarshalAs(UnmanagedType.Bool)] bool b, IntPtr a, IntPtr c);
        [PreserveSig] int SetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string p, IntPtr a);
        [PreserveSig] int GetShareMode([MarshalAs(UnmanagedType.LPWStr)] string p, IntPtr a);
        [PreserveSig] int SetShareMode([MarshalAs(UnmanagedType.LPWStr)] string p, IntPtr a);
        [PreserveSig] int GetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string p, [MarshalAs(UnmanagedType.Bool)] bool b, IntPtr k, IntPtr v);
        [PreserveSig] int SetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string p, [MarshalAs(UnmanagedType.Bool)] bool b, IntPtr k, IntPtr v);
        [PreserveSig] int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, uint role);
        [PreserveSig] int SetEndpointVisibility([MarshalAs(UnmanagedType.LPWStr)] string p, [MarshalAs(UnmanagedType.Bool)] bool b);
    }

    // ── Structs ──────────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct PROPERTYKEY
    {
        public Guid fmtid;
        public uint pid;
    }

    // Size=24 matches sizeof(PROPVARIANT) on both x86 and x64
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct PROPVARIANT
    {
        [FieldOffset(0)] public ushort vt;
        [FieldOffset(8)] public IntPtr pwszVal;
    }

    // PropVariantClear writes to the struct (clears it), so ref is correct despite CS9191
    [DllImport("ole32.dll"), SuppressGCTransition]
    private static extern int PropVariantClear(ref PROPVARIANT pvar);
}
