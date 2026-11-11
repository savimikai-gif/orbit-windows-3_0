using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;

namespace ORBIT;

internal static class WindowsControl
{
    internal static int SetVolume(int pct)
    {
        using var devices = new MMDeviceEnumerator();
        using var output = devices.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        output.AudioEndpointVolume.MasterVolumeLevelScalar = Math.Clamp(pct, 0, 100) / 100f;
        return (int)Math.Round(output.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
    }
    internal static bool Mute(bool on)
    {
        using var devices = new MMDeviceEnumerator();
        using var output = devices.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        output.AudioEndpointVolume.Mute = on;
        return output.AudioEndpointVolume.Mute;
    }
    internal static object SetBrightness(int pct)
    {
        using var search = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM WmiMonitorBrightnessMethods WHERE Active = True");
        int changed = 0;
        foreach (ManagementObject screen in search.Get())
        {
            using (screen) { screen.InvokeMethod("WmiSetBrightness", new object[] { (uint)1, (byte)Math.Clamp(pct, 0, 100) }); changed++; }
        }
        if (changed == 0) throw new InvalidOperationException("This display does not support Windows software brightness. External monitors may require their own controls.");
        return new { brightness = Math.Clamp(pct, 0, 100), displays = changed };
    }
    internal static void LaunchKnown(string app)
    {
        // Fixed allowlist, no shell, arguments or paths supplied by microphone.
        string path = app.Trim().ToLowerInvariant() switch
        {
            "calculator" or "calc" => "calc.exe",
            "notepad" => "notepad.exe",
            "explorer" or "files" => "explorer.exe",
            "settings" => "ms-settings:",
            _ => throw new InvalidOperationException("This voice app shortcut is not supported.")
        };
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Size;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GlobalMemoryStatusEx(ref MemoryStatus data);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetSystemTimes(out long idle, out long kernel, out long user);
    private static long prevIdle, prevKernel, prevUser;
    private static bool sampled;
    internal static object Snapshot()
    {
        var mem = new MemoryStatus { Size = (uint)Marshal.SizeOf<MemoryStatus>() };
        if (!GlobalMemoryStatusEx(ref mem)) throw new InvalidOperationException("Windows memory metrics unavailable.");
        double? cpu = null;
        if (GetSystemTimes(out var idle, out var kernel, out var user))
        {
            if (sampled)
            {
                long totalDelta = kernel - prevKernel + user - prevUser;
                long idleDelta = idle - prevIdle;
                if (totalDelta > 0) cpu = Math.Round(100.0 * Math.Clamp(1.0 - (double)idleDelta / totalDelta, 0, 1), 1);
            }
            (prevIdle, prevKernel, prevUser) = (idle, kernel, user); sampled = true;
        }
        double totalGb = Math.Round(mem.TotalPhysical / (1024.0*1024*1024), 2);
        double usedGb = Math.Round((mem.TotalPhysical - mem.AvailablePhysical)/(1024.0*1024*1024), 2);
        return new { cpu, ram = Math.Round(mem.MemoryLoad * 1.0, 1), gpu = (double?)null, memoryTotalGb = totalGb, memoryUsedGb = usedGb };
    }
}
