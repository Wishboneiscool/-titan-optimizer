namespace TitanOptimizer.Core.Profiles;

public sealed record ProfileSelection
{
    public required bool Enabled { get; init; }
    public Dictionary<string, string> Parameters { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed record OptimizationProfile
{
    public int SchemaVersion { get; init; } = 1;
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public DateTimeOffset UpdatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public Dictionary<string, ProfileSelection> Optimizations { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
