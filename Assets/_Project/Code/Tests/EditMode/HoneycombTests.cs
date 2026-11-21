using System.Collections.Generic;
using NUnit.Framework;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Combat;
using STSlike.Domain.Effects;
using STSlike.Domain.Events;
using STSlike.Domain.Randomness;
using STSlike.Domain.Characters.Albino;
using STSlike.Domain.Statuses;

namespace STSlike.Tests.EditMode
{
    /// <summary>
    /// The wound that feeds on itself.
    ///
    /// Each round leaves half its damage behind, and the next round carries all
    /// of it — so the curve is the character. These pin the shape of that curve,
    /// the ordering that makes a stance amplify the mark too, and the rule that
    /// makes it a single-turn commitment.
    /// </summary>
    public sealed class HoneycombTests
    {
        /// <summary>Loaded, since these are about the chain rather than about loading.</summary>
        private static PlayerState Gunhand(int maxAmmo = 6)
        {
            var weapon = new WeaponState(maxAmmo, 6);
            weapon.ReloadFully();

            return new PlayerState("Albino", 70, 3, new Deck(new SystemRng(1)), weapon);
        }

        private static Combatant Target(int hp = 400) => new Combatant("Grunt", hp);

        private static int Marks(Combatant target) =>
            target.Statuses.StacksOf(HoneycombStatus.StatusId);

        [Test]
        public void ARoundLeavesHalfOfItselfBehind()
        {
            Assert.AreEqual(3, HoneycombStatus.StacksFrom(6));
            Assert.AreEqual(4, HoneycombStatus.StacksFrom(9), "rounded down");
            Assert.AreEqual(0, HoneycombStatus.StacksFrom(1), "a graze leaves nothing");
            Assert.AreEqual(0, HoneycombStatus.StacksFrom(0));
        }

        [Test]
        public void TheFirstRoundIntoACleanTargetIsJustTheWeapon()
        {
            PlayerState player = Gunhand();
            Combatant target = Target();

            new ShotEffect(1).Execute(new EffectContext(player, target));

            // Nothing to build on yet — the chain has to be started.
            Assert.AreEqual(400 - 6, target.CurrentHp);
            Assert.AreEqual(3, Marks(target));
        }

        [Test]
        public void EachRoundCarriesEverythingTheWoundHasCollected()
        {
            PlayerState player = Gunhand();
            Combatant target = Target();

            new ShotEffect(3).Execute(new EffectContext(player, target));

            // 6 (marks 3), then 6+3 = 9 (marks 7), then 6+7 = 13 (marks 13).
            Assert.AreEqual(400 - 28, target.CurrentHp);
            Assert.AreEqual(13, Marks(target));
        }

        [Test]
        public void AFullCylinderIsWorthFarMoreThanHalfOfOne()
        {
            PlayerState three = Gunhand();
            Combatant a = Target();
            new ShotEffect(3).Execute(new EffectContext(three, a));

            PlayerState six = Gunhand();
            Combatant b = Target();
            new ShotEffect(6).Execute(new EffectContext(six, b));

            int halfCylinder = 400 - a.CurrentHp;   // 6 + 9 + 13
            int fullCylinder = 400 - b.CurrentHp;   // + 19 + 28 + 42

            Assert.AreEqual(28, halfCylinder);
            Assert.AreEqual(117, fullCylinder);

            // Twice the rounds for four times the damage. That gap is the reason
            // to commit to one target rather than spread fire around.
            Assert.Greater(fullCylinder, halfCylinder * 3);
        }

        [Test]
        public void OrdinaryConditionsCannotSpeedTheChainUp()
        {
            PlayerState player = Gunhand();
            Combatant target = Target();
            target.Statuses.Apply(VulnerableStatus.Instance, 9);

            new ShotEffect(2).Execute(new EffectContext(player, target));

            // The chain is fed by what the round was worth, and a round is worth
            // what the weapon says. So the only things that can accelerate it
            // are the character's own — which is what keeps the curve hers.
            Assert.AreEqual(400 - 15, target.CurrentHp);
            Assert.AreEqual(7, Marks(target));
        }

        [Test]
        public void TheMarkRidesTheShootersOwnModifiers()
        {
            PlayerState player = Gunhand();
            player.Statuses.Apply(WeakStatus.Instance, 9);
            Combatant target = Target();

            new ShotEffect(2).Execute(new EffectContext(player, target));

            // Weakness does not touch gunfire, so this proves only that the
            // bonus is folded in before the shooter's conditions are consulted.
            // A stance multiplier rides the same seam and must catch the mark.
            Assert.AreEqual(400 - 15, target.CurrentHp);
        }

        // ---- the clock ----

        [Test]
        public void TheMarkIsWipedWhenThePlayersTurnEnds()
        {
            PlayerState player = Gunhand();
            var enemy = new EnemyState("Grunt", 400, 5);
            CombatManager manager = Fight(player, enemy);

            new ShotEffect(4).Execute(new EffectContext(player, enemy));
            Assert.Greater(Marks(enemy), 0);

            manager.EndTurn();

            // Wiped outright, not counted down: a half-built chain is worth
            // nothing next turn, and carrying it would remove the reason to
            // empty the cylinder now.
            Assert.AreEqual(0, Marks(enemy));
        }

        [Test]
        public void WipingTheMarkLeavesOrdinaryConditionsAlone()
        {
            PlayerState player = Gunhand();
            var enemy = new EnemyState("Grunt", 400, 5);
            CombatManager manager = Fight(player, enemy);

            enemy.Statuses.Apply(VulnerableStatus.Instance, 3);
            new ShotEffect(2).Execute(new EffectContext(player, enemy));

            manager.EndTurn();

            Assert.AreEqual(0, Marks(enemy));
            Assert.AreEqual(2, enemy.Statuses.StacksOf(VulnerableStatus.StatusId),
                "the enemy's own clock ran down by one, not to nothing");
        }

        private static CombatManager Fight(PlayerState player, EnemyState enemy)
        {
            var manager = new CombatManager(
                new CombatContext(player, new List<EnemyState> { enemy }),
                new EventBus(), cardsPerTurn: 1);

            manager.StartCombat(new List<Card>
            {
                new Card(new CardDefinition("a", 1, new List<ICardEffect>())),
            });

            // A fight begins with an empty weapon; these tests are about what
            // happens once it is loaded.
            player.Weapon.ReloadFully();

            return manager;
        }
    }
}
