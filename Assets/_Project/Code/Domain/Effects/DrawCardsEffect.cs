using System;
using STSlike.Domain.Actors;
using STSlike.Domain.Combat;

namespace STSlike.Domain.Effects
{
    /// <summary>
    /// Draws into the hand of whoever played the card.
    ///
    /// The deck reshuffles the discard pile when the draw pile runs out, so this
    /// only comes up short when the whole deck is already in hand. The event
    /// carries what was really drawn rather than what was asked for — a card
    /// reading "draw 2" that managed one should not claim otherwise.
    /// </summary>
    [Serializable]
    public sealed class DrawCardsEffect : ICardEffect, IDescribableEffect
    {
        public int amount;

        public int DisplayValue => amount;

        public DrawCardsEffect() { }

        public DrawCardsEffect(int amount)
        {
            this.amount = amount;
        }

        public void Execute(EffectContext context)
        {
            // Enemies have no deck, so an enemy running this does nothing
            // rather than throwing.
            PlayerState player = context.Player;
            if (player == null || amount <= 0)
                return;

            int drawn = player.Deck.DrawCards(amount);
            if (drawn > 0)
                context.Bus?.Publish(new CardsDrawnEvent(player, drawn));
        }
    }
}
