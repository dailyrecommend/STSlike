#if STSLIKE_SQLITE
using System;
using System.Collections.Generic;
using System.Linq;
using STSlike.Domain.Progression;

namespace STSlike.Infrastructure.Persistence.Sqlite
{
    /// <summary>SQLite-backed <see cref="ICardStatsRepository"/>.</summary>
    public sealed class SqliteCardStatsRepository : ICardStatsRepository
    {
        private readonly SqliteDatabase _database;

        public SqliteCardStatsRepository(SqliteDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public void RecordPick(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
                throw new ArgumentException("CardId must be provided.", nameof(cardId));

            CardStatsRow row = LoadOrCreate(cardId);
            row.PickCount++;
            _database.Connection.InsertOrReplace(row);
        }

        public void RecordRunOutcome(IEnumerable<string> deckCardIds, bool playerWon)
        {
            if (deckCardIds == null)
                throw new ArgumentNullException(nameof(deckCardIds));

            // Materialize first: the transaction body must not depend on a
            // sequence that is still being enumerated from the same connection.
            List<string> ids = deckCardIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList();

            if (ids.Count == 0)
                return;

            // One transaction for the whole deck: a run either counts fully or
            // not at all, and it is far faster than a write per card.
            _database.Connection.RunInTransaction(() =>
            {
                foreach (string cardId in ids)
                {
                    CardStatsRow row = LoadOrCreate(cardId);
                    row.RunsWithCard++;
                    if (playerWon)
                        row.WinsWithCard++;
                    _database.Connection.InsertOrReplace(row);
                }
            });
        }

        public IReadOnlyList<CardStats> GetAll()
        {
            return _database.Connection.Table<CardStatsRow>()
                .OrderBy(r => r.CardId)
                .ToList()
                .Select(r => new CardStats(r.CardId, r.PickCount, r.RunsWithCard, r.WinsWithCard))
                .ToList();
        }

        private CardStatsRow LoadOrCreate(string cardId)
        {
            return _database.Connection.Find<CardStatsRow>(cardId)
                   ?? new CardStatsRow { CardId = cardId };
        }
    }
}
#endif
