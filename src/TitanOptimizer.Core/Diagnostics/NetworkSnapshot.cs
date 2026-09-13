namespace TitanOptimizer.Core.Diagnostics;

public sealed record NetworkAdapterSnapshot(
    string Name,
    string Description,
    string Type,
    string Status,
    long LinkSpeedBitsPerSecond,
    IReadOnlyList<string> Addresses,
    IReadOnlyList<string> Gateways);
