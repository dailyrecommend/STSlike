using System;
using System.Collections.Generic;
using STSlike.Domain.Effects;

namespace STSlike.Domain.Cards
{
    /// <summary>
    /// Immutable, in-memory implementation of <see cref="ICardDefinition"/>.
    /// Used for code-defined cards and for tests; proves the combat rules do
    /// not depend on ScriptableObjects.
    /// </summary>
    public sealed class CardDefinition : ICardDefinition
    {
        public string Id { get; }
        public int Cost { get; }
        public CardType Type { get; }
        public CardTarget Target { get; }
        public CardRarity Rarity { get; }
        public bool Collectable { get; }
        public IReadOnlyList<ICardEffect> Effects { get; }

        public CardDefinition(string id, int cost, IReadOnlyList<ICardEffect> effects,
            CardType type = CardType.Attack, CardRarity rarity = CardRarity.Common,
            CardTarget target = CardTarget.SingleEnemy, bool collectable = true)
        {
            Type = type;
            Target = target;
            Rarity = rarity;
            Collectable = collectable;
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id must be provided.", nameof(id));
            if (cost < 0)
                throw new ArgumentOutOfRangeException(nameof(cost), "Cost cannot be negative.");

            Id = id;
            Cost = cost;
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
        }
    }
}
