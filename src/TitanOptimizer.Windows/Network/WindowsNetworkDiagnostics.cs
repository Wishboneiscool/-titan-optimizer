using System.Net.NetworkInformation;
using TitanOptimizer.Core.Diagnostics;

namespace TitanOptimizer.Windows.Network;

public sealed class WindowsNetworkDiagnostics
{
    public IReadOnlyList<NetworkAdapterSnapshot> Scan()
    {
        EnsureWindows();

        return NetworkInterface.GetAllNetworkInterfaces()
            .Select(adapter =>
            {
                var properties = adapter.GetIPProperties();
                return new NetworkAdapterSnapshot(
                    adapter.Name,
                    adapter.Description,
                    adapter.NetworkInterfaceType.ToString(),
                    adapter.OperationalStatus.ToString(),
                    adapter.Speed,
                    properties.UnicastAddresses.Select(address => address.Address.ToString()).ToArray(),
                    properties.GatewayAddresses.Select(gateway => gateway.Address.ToString()).ToArray());
            })
            .OrderBy(adapter => adapter.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Network diagnostics are supported only on Windows.");
        }
    }
}
