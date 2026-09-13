using TitanOptimizer.Core.Benchmarking;

namespace TitanOptimizer.Core.Journal;

public interface IBenchmarkJournal
{
    void Append(BenchmarkRecord record);

    IReadOnlyList<BenchmarkRecord> GetRecent(int limit = 50);
}
