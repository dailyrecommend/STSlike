using System.Collections.Generic;
using NUnit.Framework;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Combat;
using STSlike.Domain.Effects;
using STSlike.Domain.Events;
using STSlike.Domain.Randomness;
using STSlike.Domain.Statuses;

namespace STSlike.Tests.EditMode
{
    /// <summary>
    /// Weak and Vulnerable, and the machinery they are the first users of.
    ///
    /// The point of the design is that the damage formula never names them, so
    /// these cover both the numbers and the arrangement: conditions stack as
    /// duration, run down at the end of their owner's turn, and do not survive
    /// a fight.
    /// </summary>
    public sealed class StatusEffectTests
    {
        private static Combatant Fighter(int hp = 50) => new Combatant("Fighter", hp);

        // ---- the numbers ----

        [Test]
        public void WeakCutsOutgoingDamageByAQuarter()
        {
            Combatant attacker = Fighter();
            attacker.Statuses.Apply(WeakStatus.Instance, 2);

            Assert.AreEqual(9, attacker.Statuses.ModifyOutgoingDamage(12));
        }

        [Test]
        public void VulnerableRaisesIncomingDamageByAHalf()
        {
            Combatant victim = Fighter();
            victim.Statuses.Apply(VulnerableStatus.Instance, 2);

            Assert.AreEqual(15, victim.Statuses.ModifyIncomingDamage(10));
        }

        [Test]
        public void WeakDoesNotTouchIncomingAndVulnerableDoesNotTouchOutgoing()
        {
            Combatant fighter = Fighter();
            fighter.Statuses.Apply(WeakStatus.Instance, 2);
            fighter.Statuses.Apply(VulnerableStatus.Instance, 2);

            Assert.AreEqual(15, fighter.Statuses.ModifyIncomingDamage(10),
                "only vulnerable should raise what arrives");
            Assert.AreEqual(9, fighter.Statuses.ModifyOutgoingDamage(12),
                "only weak should cut what is thrown");
        }

        [Test]
        public void MoreStacksMeansLongerNotStronger()
        {
            Combatant attacker = Fighter();
            attacker.Statuses.Apply(WeakStatus.Instance, 1);
            int once = attacker.Statuses.ModifyOutgoingDamage(12);

            attacker.Statuses.Apply(WeakStatus.Instance, 5);

            Assert.AreEqual(once, attacker.Statuses.ModifyOutgoingDamage(12));
            Assert.AreEqual(6, attacker.Statuses.StacksOf(WeakStatus.StatusId));
        }

        // ---- through a real attack ----

        [Test]
        public void BothSidesGetASayOnOneBlow()
        {
            Combatant attacker = Fighter();
            Combatant victim = Fighter();
            attacker.Statuses.Apply(WeakStatus.Instance, 2);
            victim.Statuses.Apply(VulnerableStatus.Instance, 2);

            new DealDamageEffect(10).Execute(new EffectContext(attacker, victim));

            // Weakened first: 10 -> 7. Then vulnerable: 7 -> 10.
            Assert.AreEqual(40, victim.CurrentHp);
        }

        [Test]
        public void TheAnnouncedAmountIsWhatWasActuallyThrown()
        {
            Combatant attacker = Fighter();
            Combatant victim = Fighter();
            victim.Statuses.Apply(VulnerableStatus.Instance, 2);

            var bus = new EventBus();
            int announced = 0;
            bus.Subscribe<DamageDealtEvent>(e => announced = e.Amount);

            new DealDamageEffect(10).Execute(new EffectContext(attacker, victim, bus));

            // Not the card's printed 10 — a floating number saying 10 over a
            // 15-point hit is worse than no number at all.
            Assert.AreEqual(15, announced);
        }

        [Test]
        public void BlockIsSpentAgainstTheModifiedAmount()
        {
            Combatant attacker = Fighter();
            Combatant victim = Fighter();
            victim.GainBlock(12);
            victim.Statuses.Apply(VulnerableStatus.Instance, 2);

            new DealDamageEffect(10).Execute(new EffectContext(attacker, victim));

            // 15 arrives, 12 absorbed, 3 through.
            Assert.AreEqual(47, victim.CurrentHp);
            Assert.AreEqual(0, victim.Block);
        }

        // ---- the clock ----

        [Test]
        public void TickingRunsTheClockDownAndDropsWhatExpires()
        {
            Combatant fighter = Fighter();
            fighter.Statuses.Apply(WeakStatus.Instance, 2);

            fighter.Statuses.TickDown();
            Assert.AreEqual(1, fighter.Statuses.StacksOf(WeakStatus.StatusId));

            fighter.Statuses.TickDown();
            Assert.AreEqual(0, fighter.Statuses.StacksOf(WeakStatus.StatusId));
            Assert.AreEqual(0, fighter.Statuses.Active.Count,
                "an expired condition should leave the panel, not sit there at zero");
        }

        [Test]
        public void AConditionOnThePlayerStillBitesDuringTheEnemyReply()
        {
            PlayerState player = Player(out CombatManager manager, out EnemyState _);
            int before = player.CurrentHp;
            player.Statuses.Apply(VulnerableStatus.Instance, 2);

            manager.EndTurn();

            // The clock runs down as the player's turn closes, but a step is
            // left — so the enemy's 5-point swing lands as 7. Ticking after the
            // reply instead would make the last turn of a debuff free.
            Assert.AreEqual(before - 7, player.CurrentHp);
            Assert.AreEqual(1, player.Statuses.StacksOf(VulnerableStatus.StatusId));
        }

        [Test]
        public void ItsLastStepSpentTheConditionIsGoneBeforeTheReply()
        {
            PlayerState player = Player(out CombatManager manager, out EnemyState _);
            int before = player.CurrentHp;
            player.Statuses.Apply(VulnerableStatus.Instance, 1);

            manager.EndTurn();

            Assert.AreEqual(before - 5, player.CurrentHp, "it had run out, so the swing is plain");
        }

        [Test]
        public void AnEnemysConditionCoversThePlayersNextTurn()
        {
            PlayerState _ = Player(out CombatManager manager, out EnemyState enemy);
            enemy.Statuses.Apply(VulnerableStatus.Instance, 2);

            manager.EndTurn();

            // One step spent on the enemy's own turn; the rest is still there
            // for the player to exploit.
            Assert.AreEqual(1, enemy.Statuses.StacksOf(VulnerableStatus.StatusId));
        }

        [Test]
        public void ConditionsDoNotSurviveAFight()
        {
            PlayerState player = Player(out CombatManager manager, out EnemyState _);
            player.Statuses.Apply(WeakStatus.Instance, 3);

            // The player is the run's own instance, carried from room to room.
            manager.StartCombat(new List<Card> { Card("a"), Card("b") });

            Assert.AreEqual(0, player.Statuses.StacksOf(WeakStatus.StatusId));
        }

        // ---- what the screen is told ----

        [Test]
        public void ThePreviewMatchesWhatTheBlowActuallyDoes()
        {
            Combatant attacker = Fighter();
            Combatant victim = Fighter();
            attacker.Statuses.Apply(WeakStatus.Instance, 2);
            victim.Statuses.Apply(VulnerableStatus.Instance, 2);

            var attack = new DealDamageEffect(2);
            int promised = attack.Preview(attacker, victim);

            attack.Execute(new EffectContext(attacker, victim));
            int actual = 50 - victim.CurrentHp;

            // The whole point: a card that says 2 while dealing 1 is worse than
            // a card that says nothing.
            Assert.AreEqual(promised, actual);
        }

        [Test]
        public void WeakTurnsATwoDamageJabIntoOne()
        {
            Combatant attacker = Fighter();
            attacker.Statuses.Apply(WeakStatus.Instance, 2);

            Assert.AreEqual(1, new DealDamageEffect(2).Preview(attacker, Fighter()));
        }

        [Test]
        public void APreviewWithNobodyInvolvedIsTheAuthoredNumber()
        {
            // A card browsed on the map has no fight to be measured against.
            Assert.AreEqual(6, new DealDamageEffect(6).Preview(null, null));
        }

        // ---- as a card ----

        [Test]
        public void ACardCanApplyAConditionToItsTarget()
        {
            Combatant player = Fighter();
            Combatant victim = Fighter();
            var bus = new EventBus();
            string announced = null;
            bus.Subscribe<StatusChangedEvent>(e => announced = e.StatusId);

            new ApplyVulnerableEffect(2).Execute(new EffectContext(player, victim, bus));

            Assert.AreEqual(2, victim.Statuses.StacksOf(VulnerableStatus.StatusId));
            Assert.AreEqual(VulnerableStatus.StatusId, announced);
        }

        [Test]
        public void AnUntargetedConditionCardDoesNothing()
        {
            Combatant player = Fighter();

            Assert.DoesNotThrow(() =>
                new ApplyWeakEffect(2).Execute(new EffectContext(player, null)));
        }

        // ---- helpers ----

        private static Card Card(string id) =>
            new Card(new CardDefinition(id, 1, new List<ICardEffect>()));

        private static PlayerState Player(out CombatManager manager, out EnemyState enemy)
        {
            var player = new PlayerState("Player", 70, 3, new Deck(new SystemRng(1)));
            enemy = new EnemyState("Grunt", 30, 5);

            manager = new CombatManager(
                new CombatContext(player, new List<EnemyState> { enemy }),
                new EventBus(), cardsPerTurn: 1);

            manager.StartCombat(new List<Card> { Card("a"), Card("b"), Card("c") });
            return player;
        }
    }
}
