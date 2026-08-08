using System.Collections.Generic;
using NUnit.Framework;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Combat;
using STSlike.Domain.Effects;
using STSlike.Domain.Events;
using STSlike.Domain.Randomness;

namespace STSlike.Tests.EditMode
{
    /// <summary>
    /// Effects that touch the player's own resources rather than a body.
    ///
    /// These are the first effects to need more than <see cref="Combatant"/>, so
    /// the tests are as much about the context reaching a deck and an energy bar
    /// as about the two effects themselves.
    /// </summary>
    public sealed class ResourceEffectTests
    {
        private static Card Card(string id) =>
            new Card(new CardDefinition(id, 1, new List<ICardEffect>()));

        private static PlayerState Player(int maxEnergy = 3, params string[] deck)
        {
            var cards = new List<Card>();
            foreach (string id in deck)
                cards.Add(Card(id));

            var pile = new Deck(new SystemRng(1));
            pile.Prepare(cards);

            return new PlayerState("Player", 70, maxEnergy, pile);
        }

        // ---- energy ----

        [Test]
        public void GainEnergyAddsToTheBar()
        {
            PlayerState player = Player();
            player.RefillEnergy();
            player.SpendEnergy(2);

            new GainEnergyEffect(1).Execute(new EffectContext(player, null));

            Assert.AreEqual(2, player.Energy);
        }

        [Test]
        public void GainEnergyMayExceedTheTurnMaximum()
        {
            PlayerState player = Player(maxEnergy: 3);
            player.RefillEnergy();

            new GainEnergyEffect(2).Execute(new EffectContext(player, null));

            // The maximum is what a turn starts with, not a ceiling — clamping
            // would make this card do nothing on the turn you would play it.
            Assert.AreEqual(5, player.Energy);
        }

        [Test]
        public void GainEnergyAnnouncesItself()
        {
            PlayerState player = Player();
            var bus = new EventBus();
            int announced = 0;
            bus.Subscribe<EnergyGainedEvent>(e => announced = e.Amount);

            new GainEnergyEffect(2).Execute(new EffectContext(player, null, bus));

            Assert.AreEqual(2, announced);
        }

        [Test]
        public void AnEnemyRunningAnEnergyEffectDoesNothing()
        {
            var enemy = new EnemyState("Grunt", 20, 5);

            // Enemies have no energy bar; the effect declines rather than throws.
            Assert.DoesNotThrow(() =>
                new GainEnergyEffect(2).Execute(new EffectContext(enemy, null)));
        }

        // ---- draw ----

        [Test]
        public void DrawMovesCardsIntoHand()
        {
            PlayerState player = Player(3, "a", "b", "c");
            int before = player.Deck.Hand.Cards.Count;

            new DrawCardsEffect(2).Execute(new EffectContext(player, null));

            Assert.AreEqual(before + 2, player.Deck.Hand.Cards.Count);
        }

        [Test]
        public void DrawReportsWhatWasActuallyDrawn()
        {
            // Only one card exists, so "draw 2" can only manage one.
            PlayerState player = Player(3, "a");
            var bus = new EventBus();
            int reported = -1;
            bus.Subscribe<CardsDrawnEvent>(e => reported = e.Count);

            new DrawCardsEffect(2).Execute(new EffectContext(player, null, bus));

            Assert.AreEqual(1, reported);
            Assert.AreEqual(1, player.Deck.Hand.Cards.Count);
        }

        [Test]
        public void DrawingNothingSaysNothing()
        {
            PlayerState player = Player();
            var bus = new EventBus();
            bool announced = false;
            bus.Subscribe<CardsDrawnEvent>(_ => announced = true);

            new DrawCardsEffect(2).Execute(new EffectContext(player, null, bus));

            Assert.IsFalse(announced, "an empty deck drew nothing, so there is nothing to announce");
        }

        [Test]
        public void AnEnemyRunningADrawEffectDoesNothing()
        {
            var enemy = new EnemyState("Grunt", 20, 5);

            Assert.DoesNotThrow(() =>
                new DrawCardsEffect(2).Execute(new EffectContext(enemy, null)));
        }

        // ---- through a real card ----

        [Test]
        public void ACardCanBothDrawAndRefundEnergyWhenPlayedForReal()
        {
            var definition = new CardDefinition("draft", 1,
                new List<ICardEffect> { new DrawCardsEffect(2), new GainEnergyEffect(1) },
                CardType.Skill, CardRarity.Common, CardTarget.Self);

            // Every card in the deck is the one under test, so the shuffle
            // cannot decide whether this test gets to run.
            var deck = new List<Card>();
            for (int i = 0; i < 6; i++)
                deck.Add(new Card(definition));

            var player = new PlayerState("Player", 70, 3, new Deck(new SystemRng(1)));
            var enemy = new EnemyState("Grunt", 20, 5);
            var manager = new CombatManager(
                new CombatContext(player, new List<EnemyState> { enemy }),
                new EventBus(), cardsPerTurn: 2);

            manager.StartCombat(deck);

            int energyBefore = player.Energy;
            int handBefore = player.Deck.Hand.Cards.Count;

            manager.PlayCard(player.Deck.Hand.Cards[0], enemy);

            // Cost 1 paid and 1 refunded is a net zero; two drawn and the played
            // card discarded is a net one.
            Assert.AreEqual(energyBefore, player.Energy);
            Assert.AreEqual(handBefore + 1, player.Deck.Hand.Cards.Count);
        }
    }
}
