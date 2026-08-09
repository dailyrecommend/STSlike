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
    /// Firing the weapon.
    ///
    /// Two things make this different from ordinary damage, and both are the
    /// character's identity rather than incidental: the number comes from the
    /// weapon so debuffs on its owner do not touch it, and every round is spent
    /// whether or not there was one to spend.
    /// </summary>
    public sealed class ShotEffectTests
    {
        private static PlayerState Gunhand(int maxAmmo = 6, int shotDamage = 6) =>
            new PlayerState("Albino", 70, 3, new Deck(new SystemRng(1)), maxAmmo, shotDamage);

        private static Combatant Target(int hp = 100) => new Combatant("Grunt", hp);

        [Test]
        public void ARoundDealsTheWeaponsDamage()
        {
            PlayerState player = Gunhand();
            Combatant target = Target();

            new ShotEffect(1).Execute(new EffectContext(player, target));

            // Just the weapon: a clean target has nothing to build on yet.
            Assert.AreEqual(94, target.CurrentHp);
            Assert.AreEqual(5, player.Ammo);
        }

        [Test]
        public void EachShotSpendsOneRound()
        {
            PlayerState player = Gunhand();
            Combatant target = Target();

            new ShotEffect(3).Execute(new EffectContext(player, target));

            // 6, 9, 13 — the wound is deepening under the fire.
            Assert.AreEqual(3, player.Ammo);
            Assert.AreEqual(100 - 28, target.CurrentHp);
        }

        [Test]
        public void ABurstStopsWhenTheCylinderRunsDry()
        {
            PlayerState player = Gunhand(maxAmmo: 2);
            Combatant target = Target();

            // Asked for four, had two. The card was still played.
            new ShotEffect(4).Execute(new EffectContext(player, target));

            Assert.AreEqual(0, player.Ammo);
            Assert.AreEqual(100 - 15, target.CurrentHp);
        }

        [Test]
        public void AnEmptyCylinderFiresNothingAndSaysSo()
        {
            PlayerState player = Gunhand(maxAmmo: 0);
            Combatant target = Target();
            var bus = new EventBus();
            bool clicked = false;
            bus.Subscribe<ShotFizzledEvent>(_ => clicked = true);

            new ShotEffect(2).Execute(new EffectContext(player, target, bus));

            Assert.AreEqual(100, target.CurrentHp);
            Assert.IsTrue(clicked, "the player has to hear why nothing happened");
        }

        [Test]
        public void ABurstStopsOnceTheTargetIsDown()
        {
            PlayerState player = Gunhand();
            Combatant target = Target(hp: 12);

            new ShotEffect(6).Execute(new EffectContext(player, target));

            // Two rounds finished it; the other four stay in the cylinder rather
            // than being emptied into a corpse.
            Assert.IsTrue(target.IsDead);
            Assert.AreEqual(4, player.Ammo);
        }

        // ---- the point of the tag ----

        [Test]
        public void AWeakenedGunhandStillFiresFullStrength()
        {
            PlayerState player = Gunhand();
            player.Statuses.Apply(WeakStatus.Instance, 2);
            Combatant target = Target();

            new ShotEffect(1).Execute(new EffectContext(player, target));

            // The whole reason the weapon exists: a full 6, not the 4 a weakened
            // swing would have managed.
            Assert.AreEqual(94, target.CurrentHp);
        }

        [Test]
        public void AnOpenTargetStillTakesMoreFromGunfire()
        {
            PlayerState player = Gunhand();
            Combatant target = Target();
            target.Statuses.Apply(VulnerableStatus.Instance, 2);

            new ShotEffect(1).Execute(new EffectContext(player, target));

            // 6 leaves the barrel, 9 arrives.
            Assert.AreEqual(91, target.CurrentHp);
        }

        [Test]
        public void EveryRoundIsAnnouncedSeparately()
        {
            PlayerState player = Gunhand();
            var bus = new EventBus();
            int blows = 0;
            bus.Subscribe<DamageDealtEvent>(_ => blows++);

            new ShotEffect(3).Execute(new EffectContext(player, Target(), bus));

            // One event per round, so the stage can play three impacts rather
            // than one lump.
            Assert.AreEqual(3, blows);
        }

        [Test]
        public void AnEnemyRunningAShotEffectDoesNothing()
        {
            var enemy = new EnemyState("Grunt", 20, 5);

            Assert.DoesNotThrow(() =>
                new ShotEffect(2).Execute(new EffectContext(enemy, Target())));
        }
    }
}
