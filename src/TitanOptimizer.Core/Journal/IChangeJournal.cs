using TitanOptimizer.Core.Models;

namespace TitanOptimizer.Core.Journal;

public interface IChangeJournal
{
    void Append(ChangeRecord record);

    IReadOnlyList<ChangeRecord> GetRecent(int limit = 50);
}
