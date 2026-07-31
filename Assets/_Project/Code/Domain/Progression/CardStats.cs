using System;

namespace STSlike.Domain.Progression
{
    /// <summary>
    /// Aggregated pick/win data for one card. Kept as totals rather than raw
    /// events so balancing questions ("is this card ever taken, and does taking
    /// it correlate with clearing?") stay cheap to answer.
    /// </summary>
    public sealed class CardStats
    {
        public string CardId { get; }
        public int PickCount { get; }
        public int RunsWithCard { get; }
        public int WinsWithCard { get; }

        /// <summary>Share of finished runs containing this card that were cleared.</summary>
        public float WinRate => RunsWithCard == 0 ? 0f : (float)WinsWithCard / RunsWithCard;

        public CardStats(string cardId, int pickCount, int runsWithCard, int winsWithCard)
        {
            if (string.IsNullOrWhiteSpace(cardId))
                throw new ArgumentException("CardId must be provided.", nameof(cardId));

            CardId = cardId;
            PickCount = pickCount;
            RunsWithCard = runsWithCard;
            WinsWithCard = winsWithCard;
        }
    }
}
