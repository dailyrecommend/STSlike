using System;
using System.Collections.Generic;
using STSlike.Domain.Effects;

namespace STSlike.Domain.Cards
{
    /// <summary>
    /// A runtime card instance held in a deck/hand. Wraps its shared
    /// <see cref="ICardDefinition"/> (design data) and is the place where
    /// per-copy state (upgrades, temporary cost changes) will live later.
    /// </summary>
    public sealed class Card
    {
        public ICardDefinition Definition { get; }

        public string Id => Definition.Id;
        public int Cost => Definition.Cost;
        public IReadOnlyList<ICardEffect> Effects => Definition.Effects;

        public Card(ICardDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }
    }
}
