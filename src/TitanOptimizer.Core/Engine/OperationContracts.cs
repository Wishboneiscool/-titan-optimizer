namespace TitanOptimizer.Core.Engine;

public enum OperationMode
{
    Preview,
    DryRun,
    Apply
}

public sealed record ValidationResult(bool IsValid, string Reason)
{
    public static ValidationResult Valid() => new(true, string.Empty);
    public static ValidationResult Invalid(string reason) => new(false, reason);
}

public sealed record OperationResult(
    bool Succeeded,
    bool WasDryRun,
    string Message,
    string? Before,
    string? After);
