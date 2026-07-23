using System;
using System.Collections.Generic;
using STSlike.Domain.Randomness;

namespace STSlike.Domain.Cards
{
    /// <summary>
    /// Owns the three combat piles (draw / hand / discard) and the rules for
    /// moving cards between them, including reshuffling the discard pile back
    /// into the draw pile when the draw pile runs out.
    /// </summary>
    public sealed class Deck
    {
        private readonly IRng _rng;

        public CardPile DrawPile { get; } = new CardPile();
        public CardPile Hand { get; } = new CardPile();
        public CardPile DiscardPile { get; } = new CardPile();

        public Deck(IRng rng)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        /// <summary>Loads a starting deck into the draw pile and shuffles it.</summary>
        public void Prepare(IEnumerable<Card> startingDeck)
        {
            if (startingDeck == null)
                throw new ArgumentNullException(nameof(startingDeck));

            DrawPile.Clear();
            Hand.Clear();
            DiscardPile.Clear();

            DrawPile.AddRange(startingDeck);
            DrawPile.Shuffle(_rng);
        }

        /// <summary>
        /// Draws one card into hand. If the draw pile is empty, the discard
        /// pile is shuffled back into it first. Returns the drawn card, or
        /// null when no cards are available anywhere.
        /// </summary>
        public Card DrawOne()
        {
            if (DrawPile.IsEmpty)
                ReshuffleDiscardIntoDraw();

            if (DrawPile.IsEmpty)
                return null;

            Card card = DrawPile.Draw();
            Hand.Add(card);
            return card;
        }

        /// <summary>Draws up to <paramref name="count"/> cards; returns how many were actually drawn.</summary>
        public int DrawCards(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            int drawn = 0;
            for (int i = 0; i < count; i++)
            {
                if (DrawOne() == null)
                    break;
                drawn++;
            }
            return drawn;
        }

        /// <summary>Moves a specific card from hand to the discard pile.</summary>
        public void DiscardFromHand(Card card)
        {
            if (card == null)
                throw new ArgumentNullException(nameof(card));
            if (!Hand.Remove(card))
                throw new InvalidOperationException("Card is not in hand.");

            DiscardPile.Add(card);
        }

        /// <summary>Moves the entire hand to the discard pile (end of turn).</summary>
        public void DiscardHand()
        {
            while (!Hand.IsEmpty)
                DiscardPile.Add(Hand.Draw());
        }

        private void ReshuffleDiscardIntoDraw()
        {
            DrawPile.AddRange(DiscardPile.Cards);
            DiscardPile.Clear();
            DrawPile.Shuffle(_rng);
        }
    }
}
