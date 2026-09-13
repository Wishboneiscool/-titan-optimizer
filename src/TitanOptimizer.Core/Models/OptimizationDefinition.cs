namespace TitanOptimizer.Core.Models;

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum QualityTier
{
    StronglySupported = 1,
    ContextDependent = 2,
    Experimental = 3,
    Deprecated = 4,
    Rejected = 5
}

public sealed record OptimizationDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required string Subsystem { get; init; }
    public required string Description { get; init; }
    public required RiskLevel Risk { get; init; }
    public required QualityTier Tier { get; init; }
    public required bool RequiresAdministrator { get; init; }
    public required bool Reversible { get; init; }
    public required bool BackupRequired { get; init; }
    public required string EvidenceSummary { get; init; }
    public string[] Prerequisites { get; init; } = [];
    public string[] Dependencies { get; init; } = [];
    public string[] Conflicts { get; init; } = [];

    public bool IsEligibleForAutomaticApplication =>
        Tier is QualityTier.StronglySupported or QualityTier.ContextDependent
        && Risk is not (RiskLevel.High or RiskLevel.Critical)
        && Reversible;
}
