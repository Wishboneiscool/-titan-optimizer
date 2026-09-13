using System.Diagnostics;
using System.Security.Cryptography;

namespace TitanOptimizer.Core.Benchmarking;

/// <summary>
/// A deterministic local workload used to validate the measurement pipeline.
/// It is not a gaming benchmark and must not be presented as an FPS predictor.
/// </summary>
public sealed class SyntheticCpuBenchmark : IBenchmark
{
    private readonly int _iterations;

    public SyntheticCpuBenchmark(int iterations = 100_000)
    {
        if (iterations is < 1_000 or > 10_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations));
        }

        _iterations = iterations;
    }

    public BenchmarkSample Run(CancellationToken cancellationToken = default)
    {
        var input = new byte[128];
        var output = new byte[32];
        var stopwatch = Stopwatch.StartNew();

        for (var iteration = 0; iteration < _iterations; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BitConverter.TryWriteBytes(input.AsSpan(0, sizeof(int)), iteration);
            SHA256.HashData(input, output);
        }

        stopwatch.Stop();
        var seconds = Math.Max(stopwatch.Elapsed.TotalSeconds, double.Epsilon);
        return new BenchmarkSample(
            "synthetic-cpu-sha256",
            DateTimeOffset.UtcNow,
            stopwatch.Elapsed,
            _iterations,
            _iterations / seconds);
    }
}
