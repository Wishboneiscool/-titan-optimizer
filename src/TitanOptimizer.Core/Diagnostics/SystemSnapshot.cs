namespace TitanOptimizer.Core.Diagnostics;

public sealed record StorageVolumeSnapshot(
    string Volume,
    long TotalBytes,
    long AvailableBytes);

public sealed record SystemSnapshot(
    DateTimeOffset CapturedUtc,
    string OperatingSystem,
    int CpuLogicalProcessors,
    ulong TotalMemoryBytes,
    ulong AvailableMemoryBytes,
    IReadOnlyList<StorageVolumeSnapshot> Volumes);
