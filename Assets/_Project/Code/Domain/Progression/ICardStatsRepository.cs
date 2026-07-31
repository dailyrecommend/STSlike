using System.Collections.Generic;

namespace STSlike.Domain.Progression
{
    /// <summary>Repository boundary for per-card balancing data.</summary>
    public interface ICardStatsRepository
    {
        /// <summary>The player chose this card as a reward.</summary>
        void RecordPick(string cardId);

        /// <summary>A run finished; credit every distinct card in the final deck.</summary>
        void RecordRunOutcome(IEnumerable<string> deckCardIds, bool playerWon);

        IReadOnlyList<CardStats> GetAll();
    }
}
