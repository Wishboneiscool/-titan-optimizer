namespace TitanOptimizer.Core.Configuration;

public sealed record AppSettings
{
    public int SchemaVersion { get; init; } = 1;
    public string ActiveProfileId { get; init; } = "safe";
    public bool AutomaticRecommendations { get; init; } = true;
    public bool CreateSnapshotsBeforeApply { get; init; } = true;
    public bool RestorePointBeforeHighRisk { get; init; } = true;
    public bool ShowExperimentalDefinitions { get; init; }
    public bool ReducedMotion { get; init; }
    public bool StartWithWindows { get; init; }
}
