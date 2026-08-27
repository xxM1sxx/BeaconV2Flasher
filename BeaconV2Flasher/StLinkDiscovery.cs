using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace BeaconV2Flasher;

public sealed record StLinkProbe(string Name, string DeviceId, string? AdapterSerial)
{
    public string DisplayName =>
        $"{Name} — {(AdapterSerial is null ? "tanpa serial" : AdapterSerial)}";
}

public static class StLinkDiscovery
{
    private const string UsbRegistryPath = @"SYSTEM\CurrentControlSet\Enum\USB";

    public static IReadOnlyList<StLinkProbe> GetConnected()
    {
        var probes = new Dictionary<string, StLinkProbe>(StringComparer.OrdinalIgnoreCase);

        using var usb = Registry.LocalMachine.OpenSubKey(UsbRegistryPath);
        if (usb is null)
            return [];

        foreach (var deviceKeyName in usb.GetSubKeyNames())
        {
            if (!deviceKeyName.StartsWith("VID_0483&PID_", StringComparison.OrdinalIgnoreCase) ||
                deviceKeyName.Contains("&MI_", StringComparison.OrdinalIgnoreCase))
                continue;

            using var device = usb.OpenSubKey(deviceKeyName);
            if (device is null)
                continue;

            foreach (var instanceName in device.GetSubKeyNames())
            {
                var deviceId = $@"USB\{deviceKeyName}\{instanceName}";
                if (!IsPresent(deviceId))
                    continue;

                using var instance = device.OpenSubKey(instanceName);
                var name = ReadDeviceName(instance);
                if (!IsStLink(name))
                    continue;

                // Windows-generated IDs contain '&'. OpenOCD can still use the
                // device automatically, but cannot select it by adapter serial.
                var adapterSerial = instanceName.Contains('&') ? null : instanceName;
                probes.TryAdd(
                    deviceId,
                    new StLinkProbe(name, deviceId, adapterSerial));
            }
        }

        return probes.Values.OrderBy(probe => probe.DisplayName).ToArray();
    }

    private static string ReadDeviceName(RegistryKey? instance)
    {
        var value = instance?.GetValue("FriendlyName") as string
                    ?? instance?.GetValue("DeviceDesc") as string
                    ?? "ST-Link";

        var separator = value.LastIndexOf(';');
        return separator >= 0 ? value[(separator + 1)..] : value;
    }

    private static bool IsStLink(string name) =>
        name.Contains("ST-Link", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("STLink", StringComparison.OrdinalIgnoreCase);

    private static bool IsPresent(string deviceId) =>
        CM_Locate_DevNode(out _, deviceId, 0) == 0;

    [DllImport("cfgmgr32.dll", EntryPoint = "CM_Locate_DevNodeW", CharSet = CharSet.Unicode)]
    private static extern int CM_Locate_DevNode(
        out uint deviceInstance,
        string deviceId,
        uint flags);
}
