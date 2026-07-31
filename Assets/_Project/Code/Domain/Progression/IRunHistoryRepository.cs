using System.Collections.Generic;

namespace STSlike.Domain.Progression
{
    /// <summary>
    /// Repository boundary for run history. The domain talks to this contract,
    /// never to a database, so the storage behind it (in-memory today, SQLite
    /// next, something else later) can be swapped without touching game rules.
    /// </summary>
    public interface IRunHistoryRepository
    {
        void Add(RunRecord record);

        /// <summary>Most recently completed runs first.</summary>
        IReadOnlyList<RunRecord> GetRecent(int count);

        int TotalRuns { get; }
        int TotalWins { get; }
    }
}
