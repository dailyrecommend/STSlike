#if STSLIKE_SQLITE
using System;
using System.Collections.Generic;
using System.Linq;
using STSlike.Domain.Progression;

namespace STSlike.Infrastructure.Persistence.Sqlite
{
    /// <summary>
    /// SQLite-backed <see cref="IRunHistoryRepository"/>. Same contract as the
    /// in-memory version, so switching between them is one registration line in
    /// the composition root and nothing else in the project changes.
    /// </summary>
    public sealed class SqliteRunHistoryRepository : IRunHistoryRepository
    {
        private readonly SqliteDatabase _database;

        public SqliteRunHistoryRepository(SqliteDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        // Counted in the database rather than in memory: the table outlives the
        // session, so totals must come from the file, not from this run.
        public int TotalRuns =>
            _database.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM RunHistory");

        public int TotalWins =>
            _database.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM RunHistory WHERE PlayerWon = 1");

        public void Add(RunRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            _database.Connection.Insert(new RunHistoryRow
            {
                RunId = record.RunId.ToString(),
                CharacterId = record.CharacterId,
                Seed = record.Seed,
                PlayerWon = record.PlayerWon,
                EncountersCleared = record.EncountersCleared,
                TotalEncounters = record.TotalEncounters,
                DeckSize = record.DeckSize,
                CompletedAtUtcTicks = record.CompletedAtUtc.Ticks,
            });
        }

        public IReadOnlyList<RunRecord> GetRecent(int count)
        {
            if (count <= 0)
                return Array.Empty<RunRecord>();

            return _database.Connection.Table<RunHistoryRow>()
                .OrderByDescending(r => r.CompletedAtUtcTicks)
                .Take(count)
                .ToList()
                .Select(ToRecord)
                .ToList();
        }

        private static RunRecord ToRecord(RunHistoryRow row)
        {
            return new RunRecord(
                runId: Guid.TryParse(row.RunId, out Guid id) ? id : Guid.Empty,
                characterId: row.CharacterId,
                seed: row.Seed,
                playerWon: row.PlayerWon,
                encountersCleared: row.EncountersCleared,
                totalEncounters: row.TotalEncounters,
                deckSize: row.DeckSize,
                completedAtUtc: new DateTime(row.CompletedAtUtcTicks, DateTimeKind.Utc));
        }
    }
}
#endif
