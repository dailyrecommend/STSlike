using System;
using System.Collections.Generic;
using STSlike.Domain.Cards;
using STSlike.Domain.Randomness;

namespace STSlike.Domain.Run.Rewards
{
    /// <summary>
    /// Offers a few distinct cards drawn from the pool, plus a heal when the
    /// player is actually hurt. Uses the run's seeded RNG, so the same seed
    /// produces the same offers — rewards stay part of a reproducible run.
    /// </summary>
    public sealed class RewardGenerator : IRewardGenerator
    {
        private readonly IRng _rng;
        private readonly IReadOnlyList<ICardDefinition> _cardPool;
        private readonly int _cardChoices;
        private readonly int _healAmount;

        public RewardGenerator(IRng rng, IReadOnlyList<ICardDefinition> cardPool,
            int cardChoices = 3, int healAmount = 8)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _cardPool = cardPool ?? throw new ArgumentNullException(nameof(cardPool));
            if (cardChoices < 0)
                throw new ArgumentOutOfRangeException(nameof(cardChoices));
            _cardChoices = cardChoices;
            _healAmount = healAmount;
        }

        public IReadOnlyList<IRunReward> Generate(RunContext run)
        {
            if (run == null)
                throw new ArgumentNullException(nameof(run));

            var options = new List<IRunReward>();

            // Draw distinct cards: pick from a shrinking index list so the same
            // card is never offered twice in one reward screen.
            var available = new List<int>(_cardPool.Count);
            for (int i = 0; i < _cardPool.Count; i++)
                available.Add(i);

            int picks = Math.Min(_cardChoices, available.Count);
            for (int i = 0; i < picks; i++)
            {
                int slot = _rng.Next(available.Count);
                options.Add(new AddCardReward(_cardPool[available[slot]]));
                available.RemoveAt(slot);
            }

            // Only worth offering when it would actually do something.
            if (_healAmount > 0 && run.Player.CurrentHp < run.Player.MaxHp)
                options.Add(new HealReward(_healAmount));

            return options;
        }
    }
}
