namespace TitanOptimizer.Core.Benchmarking;

public enum BenchmarkOutcome
{
    InsufficientData,
    Improvement,
    Regression,
    Inconclusive
}

public sealed record BenchmarkComparison(
    double BaselineMean,
    double AfterMean,
    double RelativeChange,
    double BaselineStandardDeviation,
    double AfterStandardDeviation,
    BenchmarkOutcome Outcome,
    string Explanation);

public static class BenchmarkAnalyzer
{
    public static BenchmarkComparison Compare(
        IReadOnlyList<double> baseline,
        IReadOnlyList<double> after,
        bool higherIsBetter = true,
        double minimumRelativeChange = 0.05)
    {
        if (baseline.Count < 2 || after.Count < 2)
        {
            return new BenchmarkComparison(
                0,
                0,
                0,
                0,
                0,
                BenchmarkOutcome.InsufficientData,
                "At least two baseline and two after samples are required.");
        }

        var baselineMean = baseline.Average();
        var afterMean = after.Average();
        if (Math.Abs(baselineMean) < double.Epsilon)
        {
            return new BenchmarkComparison(
                baselineMean,
                afterMean,
                0,
                StandardDeviation(baseline, baselineMean),
                StandardDeviation(after, afterMean),
                BenchmarkOutcome.Inconclusive,
                "The baseline mean is too close to zero for a relative comparison.");
        }

        var relativeChange = (afterMean - baselineMean) / Math.Abs(baselineMean);
        var baselineDeviation = StandardDeviation(baseline, baselineMean);
        var afterDeviation = StandardDeviation(after, afterMean);
        var noiseFloor = Math.Max(minimumRelativeChange, Math.Max(baselineDeviation, afterDeviation) / Math.Abs(baselineMean));
        var direction = higherIsBetter ? relativeChange : -relativeChange;

        if (Math.Abs(direction) < noiseFloor)
        {
            return new BenchmarkComparison(
                baselineMean,
                afterMean,
                relativeChange,
                baselineDeviation,
                afterDeviation,
                BenchmarkOutcome.Inconclusive,
                "The observed change is not larger than the configured noise floor.");
        }

        var outcome = direction > 0 ? BenchmarkOutcome.Improvement : BenchmarkOutcome.Regression;
        return new BenchmarkComparison(
            baselineMean,
            afterMean,
            relativeChange,
            baselineDeviation,
            afterDeviation,
            outcome,
            outcome == BenchmarkOutcome.Improvement
                ? "The measured improvement exceeded the noise floor."
                : "The measured regression exceeded the noise floor.");
    }

    private static double StandardDeviation(IReadOnlyList<double> values, double mean)
    {
        var variance = values.Sum(value => Math.Pow(value - mean, 2)) / (values.Count - 1);
        return Math.Sqrt(variance);
    }
}
