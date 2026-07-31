#if STSLIKE_SQLITE
using System;
using System.Collections.Generic;
using System.Linq;
using STSlike.Domain.Progression;

namespace STSlike.Infrastructure.Persistence.Sqlite
{
    /// <summary>
    /// SQLite-backed <see cref="IRunSaveRepository"/>: a single row, so the file
    /// can never end up holding two half-written runs.
    /// </summary>
    public sealed class SqliteRunSaveRepository : IRunSaveRepository
    {
        private const int SlotId = 1;
        private const char Separator = ',';

        private readonly SqliteDatabase _database;

        public SqliteRunSaveRepository(SqliteDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public bool HasSave =>
            _database.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM RunSave WHERE Id = ?", SlotId) > 0;

        public void Save(RunSaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            _database.Connection.InsertOrReplace(new RunSaveRow
            {
                Id = SlotId,
                CharacterId = data.CharacterId,
                Seed = data.Seed,
                RngCallCount = data.RngCallCount,
                CurrentEncounter = data.CurrentEncounter,
                TotalEncounters = data.TotalEncounters,
                PlayerHp = data.PlayerHp,
                PlayerMaxHp = data.PlayerMaxHp,
                Stage = (int)data.Stage,
                DeckCardIds = string.Join(Separator.ToString(), data.DeckCardIds),
                SavedAtUtcTicks = data.SavedAtUtc.Ticks,
            });
        }

        public RunSaveData Load()
        {
            RunSaveRow row = _database.Connection.Find<RunSaveRow>(SlotId);
            if (row == null)
                return null;

            List<string> cardIds = string.IsNullOrEmpty(row.DeckCardIds)
                ? new List<string>()
                : row.DeckCardIds.Split(Separator)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .ToList();

            return new RunSaveData(
                characterId: row.CharacterId,
                seed: row.Seed,
                rngCallCount: row.RngCallCount,
                currentEncounter: row.CurrentEncounter,
                totalEncounters: row.TotalEncounters,
                playerHp: row.PlayerHp,
                playerMaxHp: row.PlayerMaxHp,
                stage: (RunSaveStage)row.Stage,
                deckCardIds: cardIds,
                savedAtUtc: new DateTime(row.SavedAtUtcTicks, DateTimeKind.Utc));
        }

        public void Clear()
        {
            _database.Connection.Delete<RunSaveRow>(SlotId);
        }
    }
}
#endif
