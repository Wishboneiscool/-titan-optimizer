using System.Runtime.Versioning;
using Microsoft.Win32;
using TitanOptimizer.Core.Diagnostics;

namespace TitanOptimizer.Windows.Startup;

[SupportedOSPlatform("windows")]
public sealed class WindowsStartupInventory
{
    private const string RunPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string RunOncePath = "Software\\Microsoft\\Windows\\CurrentVersion\\RunOnce";

    public IReadOnlyList<StartupItem> Scan()
    {
        EnsureWindows();
        var items = new List<StartupItem>();
        ReadRunKey(Registry.CurrentUser, RunPath, "Current user Run", items);
        ReadRunKey(Registry.CurrentUser, RunOncePath, "Current user RunOnce", items);

        using (var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
        {
            ReadRunKey(localMachine, RunPath, "Local machine Run", items);
            ReadRunKey(localMachine, RunOncePath, "Local machine RunOnce", items);
        }

        var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        if (Directory.Exists(startupFolder))
        {
            foreach (var path in Directory.EnumerateFileSystemEntries(startupFolder).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                items.Add(new StartupItem(
                    Path.GetFileName(path),
                    "Current user Startup folder",
                    path,
                    true,
                    null,
                    "Not measured"));
            }
        }

        return items
            .GroupBy(item => $"{item.Source}|{item.Name}|{item.Command}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void ReadRunKey(RegistryKey root, string path, string source, ICollection<StartupItem> items)
    {
        using var key = root.OpenSubKey(path, writable: false);
        if (key is null)
        {
            return;
        }

        foreach (var name in key.GetValueNames())
        {
            var command = key.GetValue(name)?.ToString();
            if (string.IsNullOrWhiteSpace(command))
            {
                continue;
            }

            items.Add(new StartupItem(name, source, command, true, null, "Not measured"));
        }
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Startup inventory is supported only on Windows.");
        }
    }
}
