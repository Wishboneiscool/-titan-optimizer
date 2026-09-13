using TitanOptimizer.Core.Benchmarking;
using Xunit;

namespace TitanOptimizer.Core.Tests;

public sealed class BenchmarkAnalyzerTests
{
    [Fact]
    public void SmallChangesWithNoiseAreInconclusive()
    {
        var result = BenchmarkAnalyzer.Compare(
            [100.0, 101.0, 99.0],
            [101.0, 100.0, 102.0]);

        Assert.Equal(BenchmarkOutcome.Inconclusive, result.Outcome);
    }

    [Fact]
    public void ConsistentHigherThroughputIsAnImprovement()
    {
        var result = BenchmarkAnalyzer.Compare(
            [100.0, 101.0, 99.0],
            [120.0, 121.0, 119.0]);

        Assert.Equal(BenchmarkOutcome.Improvement, result.Outcome);
    }

    [Fact]
    public void FewerThanTwoSamplesCannotProduceAClaim()
    {
        var result = BenchmarkAnalyzer.Compare([100.0], [110.0]);

        Assert.Equal(BenchmarkOutcome.InsufficientData, result.Outcome);
    }
}
