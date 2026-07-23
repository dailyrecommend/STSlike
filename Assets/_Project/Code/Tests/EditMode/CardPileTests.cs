using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using STSlike.Domain.Cards;
using STSlike.Domain.Effects;
using STSlike.Domain.Randomness;

namespace STSlike.Tests.EditMode
{
    public class CardPileTests
    {
        // Feeds a fixed sequence of "random" values so shuffle output is exact.
        private sealed class ScriptedRng : IRng
        {
            private readonly Queue<int> _values;
            public ScriptedRng(params int[] values) => _values = new Queue<int>(values);
            public int Next(int maxExclusive) => _values.Dequeue();
        }

        private static Card MakeCard(string id) =>
            new Card(new CardDefinition(id, 1, new List<ICardEffect>()));

        private static CardPile PileOf(params string[] ids)
        {
            var pile = new CardPile();
            pile.AddRange(ids.Select(MakeCard));
            return pile;
        }

        [Test]
        public void NewPile_IsEmpty()
        {
            var pile = new CardPile();

            Assert.AreEqual(0, pile.Count);
            Assert.IsTrue(pile.IsEmpty);
        }

        [Test]
        public void Draw_ReturnsTopCard_AndRemovesIt()
        {
            var pile = PileOf("A", "B", "C"); // top is C (last added)

            Card drawn = pile.Draw();

            Assert.AreEqual("C", drawn.Id);
            Assert.AreEqual(2, pile.Count);
        }

        [Test]
        public void Draw_OnEmptyPile_Throws()
        {
            var pile = new CardPile();

            Assert.Throws<InvalidOperationException>(() => pile.Draw());
        }

        [Test]
        public void Shuffle_ReordersDeterministically_WithScriptedRng()
        {
            var pile = PileOf("A", "B", "C", "D"); // indices 0..3

            // Fisher-Yates: i=3 j=1, i=2 j=0, i=1 j=1  ->  [C, D, A, B]
            pile.Shuffle(new ScriptedRng(1, 0, 1));

            CollectionAssert.AreEqual(
                new[] { "C", "D", "A", "B" },
                pile.Cards.Select(c => c.Id).ToArray());
        }

        [Test]
        public void Shuffle_PreservesAllCards_NoLossNoDuplication()
        {
            var pile = PileOf("A", "B", "C", "D", "E");

            pile.Shuffle(new ScriptedRng(2, 1, 0, 1));

            CollectionAssert.AreEquivalent(
                new[] { "A", "B", "C", "D", "E" },
                pile.Cards.Select(c => c.Id).ToArray());
        }

        // Determinism property: the same seed yields the same shuffle.
        [Test]
        public void Shuffle_SameSeed_ProducesSameOrder()
        {
            var pile1 = PileOf("A", "B", "C", "D", "E", "F");
            var pile2 = PileOf("A", "B", "C", "D", "E", "F");

            pile1.Shuffle(new SystemRng(1234));
            pile2.Shuffle(new SystemRng(1234));

            CollectionAssert.AreEqual(
                pile1.Cards.Select(c => c.Id).ToArray(),
                pile2.Cards.Select(c => c.Id).ToArray());
        }

        [Test]
        public void Clear_EmptiesThePile()
        {
            var pile = PileOf("A", "B");

            pile.Clear();

            Assert.IsTrue(pile.IsEmpty);
        }
    }
}
