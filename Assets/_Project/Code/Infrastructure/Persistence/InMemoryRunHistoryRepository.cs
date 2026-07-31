using System;
using System.Collections.Generic;
using System.Linq;
using STSlike.Domain.Progression;

namespace STSlike.Infrastructure.Persistence
{
    /// <summary>
    /// Volatile <see cref="IRunHistoryRepository"/>. Used in tests and as the
    /// fallback when no database is available; the SQLite implementation slots
    /// in behind the same interface without any caller changing.
    /// </summary>
    public sealed class InMemoryRunHistoryRepository : IRunHistoryRepository
    {
        private readonly List<RunRecord> _records = new List<RunRecord>();

        public int TotalRuns => _records.Count;
        public int TotalWins => _records.Count(r => r.PlayerWon);

        public void Add(RunRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            _records.Add(record);
        }

        public IReadOnlyList<RunRecord> GetRecent(int count)
        {
            if (count <= 0)
                return Array.Empty<RunRecord>();

            return _records
                .OrderByDescending(r => r.CompletedAtUtc)
                .Take(count)
                .ToList();
        }
    }
}
