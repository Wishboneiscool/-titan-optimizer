using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using TitanOptimizer.Core.Diagnostics;

namespace TitanOptimizer.Windows.System;

[SupportedOSPlatform("windows")]
public sealed class WindowsSystemProfiler
{
    public SystemSnapshot Capture()
    {
        EnsureWindows();

        var memory = new MemoryStatusEx
        {
            Length = (uint)Marshal.SizeOf<MemoryStatusEx>()
        };
        if (!GlobalMemoryStatusEx(ref memory))
        {
            throw new InvalidOperationException("Windows memory information could not be read.");
        }

        var volumes = DriveInfo.GetDrives()
            .Where(drive => drive.IsReady)
            .Select(drive => new StorageVolumeSnapshot(
                drive.Name,
                drive.TotalSize,
                drive.AvailableFreeSpace))
            .ToArray();

        return new SystemSnapshot(
            DateTimeOffset.UtcNow,
            Environment.OSVersion.VersionString,
            Environment.ProcessorCount,
            memory.TotalPhysicalMemory,
            memory.AvailablePhysicalMemory,
            volumes);
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("System profiling is supported only on Windows.");
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx status);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysicalMemory;
        public ulong AvailablePhysicalMemory;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }
}
