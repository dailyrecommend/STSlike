using System;
using System.Collections.Generic;
using STSlike.Domain.Randomness;

namespace STSlike.Domain.Cards
{
    /// <summary>
    /// An ordered collection of cards (draw / hand / discard are each one of
    /// these). Knows nothing about which role it plays; cross-pile rules
    /// (e.g. reshuffling discard into draw) live with the owner of the piles.
    /// The top of the pile is the last card added.
    /// </summary>
    public sealed class CardPile
    {
        private readonly List<Card> _cards = new List<Card>();

        public int Count => _cards.Count;
        public bool IsEmpty => _cards.Count == 0;

        /// <summary>Read-only view for UI/inspection. Bottom first, top last.</summary>
        public IReadOnlyList<Card> Cards => _cards;

        public void Add(Card card)
        {
            if (card == null)
                throw new ArgumentNullException(nameof(card));
            _cards.Add(card);
        }

        public void AddRange(IEnumerable<Card> cards)
        {
            if (cards == null)
                throw new ArgumentNullException(nameof(cards));
            foreach (var card in cards)
                Add(card);
        }

        /// <summary>Removes and returns the top card (last added).</summary>
        public Card Draw()
        {
            if (IsEmpty)
                throw new InvalidOperationException("Cannot draw from an empty pile.");

            int top = _cards.Count - 1;
            Card card = _cards[top];
            _cards.RemoveAt(top);
            return card;
        }

        /// <summary>Fisher–Yates shuffle; deterministic for a given seeded RNG.</summary>
        public void Shuffle(IRng rng)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }

        public void Clear() => _cards.Clear();
    }
}
