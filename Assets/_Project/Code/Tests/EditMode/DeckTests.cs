using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using STSlike.Domain.Cards;
using STSlike.Domain.Effects;
using STSlike.Domain.Randomness;

namespace STSlike.Tests.EditMode
{
    public class DeckTests
    {
        private static Card MakeCard(string id) =>
            new Card(new CardDefinition(id, 1, new List<ICardEffect>()));

        private static Deck DeckWith(int seed, params string[] ids)
        {
            var deck = new Deck(new SystemRng(seed));
            deck.Prepare(ids.Select(MakeCard));
            return deck;
        }

        [Test]
        public void Prepare_PutsWholeDeckInDrawPile_HandAndDiscardEmpty()
        {
            var deck = DeckWith(0, "A", "B", "C", "D", "E");

            Assert.AreEqual(5, deck.DrawPile.Count);
            Assert.AreEqual(0, deck.Hand.Count);
            Assert.AreEqual(0, deck.DiscardPile.Count);
        }

        [Test]
        public void DrawCards_MovesCardsFromDrawToHand()
        {
            var deck = DeckWith(0, "A", "B", "C", "D", "E");

            int drawn = deck.DrawCards(3);

            Assert.AreEqual(3, drawn);
            Assert.AreEqual(3, deck.Hand.Count);
            Assert.AreEqual(2, deck.DrawPile.Count);
        }

        [Test]
        public void DiscardFromHand_MovesSpecificCard_HandToDiscard()
        {
            var deck = DeckWith(0, "A", "B", "C");
            deck.DrawCards(3);
            Card toDiscard = deck.Hand.Cards[0];

            deck.DiscardFromHand(toDiscard);

            Assert.AreEqual(2, deck.Hand.Count);
            Assert.AreEqual(1, deck.DiscardPile.Count);
            Assert.AreSame(toDiscard, deck.DiscardPile.Cards[0]);
        }

        [Test]
        public void DiscardHand_MovesEntireHandToDiscard()
        {
            var deck = DeckWith(0, "A", "B", "C", "D");
            deck.DrawCards(4);

            deck.DiscardHand();

            Assert.AreEqual(0, deck.Hand.Count);
            Assert.AreEqual(4, deck.DiscardPile.Count);
        }

        // The core rule: an empty draw pile reshuffles the discard pile back in.
        [Test]
        public void DrawOne_WhenDrawEmpty_ReshufflesDiscardIntoDraw()
        {
            var deck = DeckWith(0, "A", "B", "C");
            deck.DrawCards(3);   // hand: 3, draw: 0, discard: 0
            deck.DiscardHand();  // hand: 0, draw: 0, discard: 3

            Card drawn = deck.DrawOne(); // must reshuffle discard -> draw, then draw

            Assert.IsNotNull(drawn);
            Assert.AreEqual(1, deck.Hand.Count);
            Assert.AreEqual(2, deck.DrawPile.Count);
            Assert.AreEqual(0, deck.DiscardPile.Count);
        }

        [Test]
        public void DrawCards_MoreThanAvailable_DrawsWhatItCan()
        {
            var deck = DeckWith(0, "A", "B"); // only 2 cards exist anywhere

            int drawn = deck.DrawCards(5);

            Assert.AreEqual(2, drawn);
            Assert.AreEqual(2, deck.Hand.Count);
        }

        [Test]
        public void DiscardFromHand_CardNotInHand_Throws()
        {
            var deck = DeckWith(0, "A", "B");
            deck.DrawCards(2);

            Assert.Throws<System.InvalidOperationException>(
                () => deck.DiscardFromHand(MakeCard("Z")));
        }
    }
}
