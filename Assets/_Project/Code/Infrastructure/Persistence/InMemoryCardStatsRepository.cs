using System;
using System.Collections.Generic;
using System.Linq;
using STSlike.Domain.Progression;

namespace STSlike.Infrastructure.Persistence
{
    /// <summary>Volatile <see cref="ICardStatsRepository"/> counterpart.</summary>
    public sealed class InMemoryCardStatsRepository : ICardStatsRepository
    {
        private sealed class Totals
        {
            public int PickCount;
            public int RunsWithCard;
            public int WinsWithCard;
        }

        private readonly Dictionary<string, Totals> _byCardId = new Dictionary<string, Totals>();

        public void RecordPick(string cardId)
        {
            Get(cardId).PickCount++;
        }

        public void RecordRunOutcome(IEnumerable<string> deckCardIds, bool playerWon)
        {
            if (deckCardIds == null)
                throw new ArgumentNullException(nameof(deckCardIds));

            foreach (string cardId in deckCardIds)
            {
                if (string.IsNullOrWhiteSpace(cardId))
                    continue;

                Totals totals = Get(cardId);
                totals.RunsWithCard++;
                if (playerWon)
                    totals.WinsWithCard++;
            }
        }

        public IReadOnlyList<CardStats> GetAll()
        {
            return _byCardId
                .Select(pair => new CardStats(pair.Key, pair.Value.PickCount,
                    pair.Value.RunsWithCard, pair.Value.WinsWithCard))
                .OrderBy(stats => stats.CardId)
                .ToList();
        }

        private Totals Get(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
                throw new ArgumentException("CardId must be provided.", nameof(cardId));

            if (!_byCardId.TryGetValue(cardId, out Totals totals))
            {
                totals = new Totals();
                _byCardId[cardId] = totals;
            }
            return totals;
        }
    }
}
