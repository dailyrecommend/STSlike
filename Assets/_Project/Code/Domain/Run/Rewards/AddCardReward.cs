using System;
using STSlike.Domain.Cards;

namespace STSlike.Domain.Run.Rewards
{
    /// <summary>Adds a copy of a card to the run deck — the deckbuilding reward.</summary>
    public sealed class AddCardReward : IRunReward
    {
        public ICardDefinition Card { get; }

        public AddCardReward(ICardDefinition card)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
        }

        public void Apply(RunContext run)
        {
            run.AddCard(new Card(Card));
        }
    }
}
