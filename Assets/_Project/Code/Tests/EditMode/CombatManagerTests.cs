using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Combat;
using STSlike.Domain.Effects;
using STSlike.Domain.Events;
using STSlike.Domain.Randomness;

namespace STSlike.Tests.EditMode
{
    public class CombatManagerTests
    {
        private static Card Strike() =>
            new Card(new CardDefinition("strike", 1,
                new List<ICardEffect> { new DealDamageEffect(6) }));

        private static Card Defend() =>
            new Card(new CardDefinition("defend", 1,
                new List<ICardEffect> { new GainBlockEffect(5) }));

        private static Card ExpensiveHit() =>
            new Card(new CardDefinition("bighit", 4,
                new List<ICardEffect> { new DealDamageEffect(20) }));

        private static List<Card> DeckOf(int count, System.Func<Card> factory) =>
            Enumerable.Range(0, count).Select(_ => factory()).ToList();

        private static (CombatManager mgr, CombatContext ctx) NewCombat(
            IEnumerable<Card> deck,
            int enemyHp = 50, int enemyAttack = 10,
            int playerHp = 80, int maxEnergy = 3, int cardsPerTurn = 5, int seed = 0)
        {
            var player = new PlayerState("Player", playerHp, maxEnergy, new Deck(new SystemRng(seed)));
            var enemy = new EnemyState("Enemy", enemyHp, enemyAttack);
            var ctx = new CombatContext(player, enemy);
            var mgr = new CombatManager(ctx, new EventBus(), cardsPerTurn);
            mgr.StartCombat(deck);
            return (mgr, ctx);
        }

        private static Card FirstInHand(CombatContext ctx) => ctx.Player.Deck.Hand.Cards[0];

        [Test]
        public void StartCombat_DealsOpeningHand_RefillsEnergy_PlayerTurn()
        {
            var (mgr, ctx) = NewCombat(DeckOf(10, Strike), cardsPerTurn: 5);

            Assert.AreEqual(5, ctx.Player.Deck.Hand.Count);
            Assert.AreEqual(3, ctx.Player.Energy);
            Assert.AreEqual(CombatPhase.PlayerTurn, ctx.Phase);
        }

        [Test]
        public void PlayCard_DealsDamage_SpendsEnergy_DiscardsCard()
        {
            var (mgr, ctx) = NewCombat(DeckOf(10, Strike), enemyHp: 50);
            Card card = FirstInHand(ctx);

            bool played = mgr.PlayCard(card, ctx.Enemy);

            Assert.IsTrue(played);
            Assert.AreEqual(44, ctx.Enemy.CurrentHp); // 6 damage
            Assert.AreEqual(2, ctx.Player.Energy);     // 3 - 1
            Assert.AreEqual(4, ctx.Player.Deck.Hand.Count);
            Assert.AreEqual(1, ctx.Player.Deck.DiscardPile.Count);
        }

        [Test]
        public void PlayCard_WhenUnaffordable_ReturnsFalse_NoChange()
        {
            var (mgr, ctx) = NewCombat(DeckOf(5, ExpensiveHit), enemyHp: 50, maxEnergy: 3);
            Card card = FirstInHand(ctx);

            bool played = mgr.PlayCard(card, ctx.Enemy);

            Assert.IsFalse(played);
            Assert.AreEqual(50, ctx.Enemy.CurrentHp);
            Assert.AreEqual(3, ctx.Player.Energy);
        }

        [Test]
        public void PlayCard_NotInHand_ReturnsFalse()
        {
            var (mgr, ctx) = NewCombat(DeckOf(10, Strike));
            Card stray = Strike(); // never added to the deck

            bool played = mgr.PlayCard(stray, ctx.Enemy);

            Assert.IsFalse(played);
        }

        [Test]
        public void PlayCard_LethalDamage_SetsPhaseWon()
        {
            var (mgr, ctx) = NewCombat(DeckOf(10, Strike), enemyHp: 6);
            Card card = FirstInHand(ctx);

            mgr.PlayCard(card, ctx.Enemy);

            Assert.IsTrue(ctx.Enemy.IsDead);
            Assert.AreEqual(CombatPhase.Won, ctx.Phase);
        }

        [Test]
        public void EndTurn_EnemyAttacks_ThenNewPlayerTurnBegins()
        {
            var (mgr, ctx) = NewCombat(DeckOf(10, Strike), enemyAttack: 10, playerHp: 80);

            mgr.EndTurn();

            Assert.AreEqual(70, ctx.Player.CurrentHp);          // took 10
            Assert.AreEqual(CombatPhase.PlayerTurn, ctx.Phase); // back to player
            Assert.AreEqual(3, ctx.Player.Energy);              // refilled
            Assert.AreEqual(5, ctx.Player.Deck.Hand.Count);     // new hand
        }

        [Test]
        public void Block_PersistsThroughEnemyTurn_AbsorbsAttack()
        {
            var (mgr, ctx) = NewCombat(DeckOf(5, Defend), enemyAttack: 10, playerHp: 80);
            Card defend = FirstInHand(ctx);

            mgr.PlayCard(defend, ctx.Enemy); // gain 5 block
            mgr.EndTurn();                   // enemy hits for 10

            Assert.AreEqual(75, ctx.Player.CurrentHp); // 10 - 5 block = 5 to hp
        }

        [Test]
        public void EndTurn_WhenEnemyKillsPlayer_SetsPhaseLost()
        {
            var (mgr, ctx) = NewCombat(DeckOf(10, Strike), enemyAttack: 10, playerHp: 8);

            mgr.EndTurn();

            Assert.IsTrue(ctx.Player.IsDead);
            Assert.AreEqual(CombatPhase.Lost, ctx.Phase);
        }

        [Test]
        public void AfterVictory_FurtherActionsAreNoOps()
        {
            var (mgr, ctx) = NewCombat(DeckOf(10, Strike), enemyHp: 6);
            mgr.PlayCard(FirstInHand(ctx), ctx.Enemy); // win

            Assert.AreEqual(CombatPhase.Won, ctx.Phase);
            Assert.DoesNotThrow(() => mgr.EndTurn());   // ignored
            Assert.IsFalse(mgr.PlayCard(Strike(), ctx.Enemy));
            Assert.AreEqual(CombatPhase.Won, ctx.Phase);
        }

        // Full loop across a turn boundary, played to victory.
        [Test]
        public void FullLoop_PlayedToVictoryOverTwoTurns()
        {
            var (mgr, ctx) = NewCombat(DeckOf(12, Strike),
                enemyHp: 30, enemyAttack: 4, playerHp: 80, maxEnergy: 3, cardsPerTurn: 5);

            // Turn 1: 3 strikes = 18 damage -> enemy at 12.
            for (int i = 0; i < 3; i++)
                mgr.PlayCard(FirstInHand(ctx), ctx.Enemy);
            Assert.AreEqual(12, ctx.Enemy.CurrentHp);

            mgr.EndTurn(); // enemy hits for 4
            Assert.AreEqual(76, ctx.Player.CurrentHp);
            Assert.AreEqual(CombatPhase.PlayerTurn, ctx.Phase);

            // Turn 2: 2 strikes = 12 damage -> enemy dead.
            for (int i = 0; i < 2; i++)
                mgr.PlayCard(FirstInHand(ctx), ctx.Enemy);

            Assert.AreEqual(CombatPhase.Won, ctx.Phase);
            Assert.AreEqual(76, ctx.Player.CurrentHp); // never took more damage
        }

        [Test]
        public void Events_ArePublished_ForCardPlayAndCombatEnd()
        {
            var player = new PlayerState("Player", 80, 3, new Deck(new SystemRng(0)));
            var enemy = new EnemyState("Enemy", 6, 10); // dies to one strike
            var ctx = new CombatContext(player, enemy);
            var bus = new EventBus();

            int cardPlayed = 0, turnStarted = 0;
            bool endedWon = false;
            bus.Subscribe<CardPlayedEvent>(_ => cardPlayed++);
            bus.Subscribe<PlayerTurnStartedEvent>(_ => turnStarted++);
            bus.Subscribe<CombatEndedEvent>(e => endedWon = e.PlayerWon);

            var mgr = new CombatManager(ctx, bus, 5);
            mgr.StartCombat(DeckOf(10, Strike)); // publishes PlayerTurnStarted

            mgr.PlayCard(FirstInHand(ctx), ctx.Enemy); // lethal: CardPlayed + CombatEnded(won)

            Assert.AreEqual(1, cardPlayed);
            Assert.GreaterOrEqual(turnStarted, 1);
            Assert.IsTrue(endedWon);
        }
    }
}
