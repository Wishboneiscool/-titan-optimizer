namespace TitanOptimizer.Core.Diagnostics;

public sealed record StartupItem(
    string Name,
    string Source,
    string Command,
    bool Enabled,
    string? Publisher,
    string EstimatedImpact);
