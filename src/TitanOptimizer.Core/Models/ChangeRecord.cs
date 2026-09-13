namespace TitanOptimizer.Core.Models;

public sealed record ChangeRecord
{
    public required Guid SessionId { get; init; }
    public required string OptimizationId { get; init; }
    public required DateTimeOffset TimestampUtc { get; init; }
    public required string Before { get; init; }
    public required string Requested { get; init; }
    public required string? After { get; init; }
    public required string Result { get; init; }
    public required bool RollbackAvailable { get; init; }
    public string? Error { get; init; }
}
