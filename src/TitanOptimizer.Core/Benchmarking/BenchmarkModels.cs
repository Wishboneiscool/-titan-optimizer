namespace TitanOptimizer.Core.Benchmarking;

public sealed record BenchmarkSample(
    string Name,
    DateTimeOffset CapturedUtc,
    TimeSpan Duration,
    long WorkUnits,
    double WorkUnitsPerSecond);

public interface IBenchmark
{
    BenchmarkSample Run(CancellationToken cancellationToken = default);
}
