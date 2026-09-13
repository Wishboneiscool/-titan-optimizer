namespace TitanOptimizer.Core.Benchmarking;

public sealed record BenchmarkRecord
{
    public required Guid SessionId { get; init; }
    public required string Name { get; init; }
    public required DateTimeOffset CapturedUtc { get; init; }
    public required TimeSpan Duration { get; init; }
    public required long WorkUnits { get; init; }
    public required double WorkUnitsPerSecond { get; init; }
    public string? RelatedOptimizationId { get; init; }
}
