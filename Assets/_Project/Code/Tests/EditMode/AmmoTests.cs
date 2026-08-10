using System.Collections.Generic;
using NUnit.Framework;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Combat;
using STSlike.Domain.Characters.Albino;
using STSlike.Domain.Effects;
using STSlike.Domain.Events;
using STSlike.Domain.Randomness;

namespace STSlike.Tests.EditMode
{
    /// <summary>
    /// The cylinder: a resource that does not come back on its own.
    ///
    /// Energy refills every turn, which is what makes it a rhythm rather than a
    /// decision. Ammo is the opposite, so these pin down the parts that make it
    /// a decision — it survives the turn, it is capped, and running dry is a
    /// state the player can reach and has to play out of.
    /// </summary>
    public sealed class AmmoTests
    {
        private static PlayerState Gunhand(int maxAmmo = 6) =>
            new PlayerState("Albino", 70, 3, new Deck(new SystemRng(1)), new WeaponState(maxAmmo, 6));

        [Test]
        public void ACylinderStartsFull()
        {
            PlayerState player = Gunhand();

            Assert.AreEqual(6, player.Weapon.Ammo);
            Assert.AreEqual(6, player.Weapon.MaxAmmo);
        }

        [Test]
        public void SpendingTakesOneRound()
        {
            PlayerState player = Gunhand();

            Assert.IsTrue(player.Weapon.TrySpend());
            Assert.AreEqual(5, player.Weapon.Ammo);
        }

        [Test]
        public void AnEmptyCylinderRefusesRatherThanGoingNegative()
        {
            PlayerState player = Gunhand(maxAmmo: 1);
            player.Weapon.TrySpend();

            // The card is still played and still spent — it simply does nothing.
            Assert.IsFalse(player.Weapon.TrySpend());
            Assert.AreEqual(0, player.Weapon.Ammo);
        }

        [Test]
        public void ReloadingNeverOverfills()
        {
            PlayerState player = Gunhand();
            player.Weapon.TrySpend();

            player.Weapon.Reload(6);

            Assert.AreEqual(6, player.Weapon.Ammo);
        }

        [Test]
        public void APartialReloadPutsBackJustThatMany()
        {
            PlayerState player = Gunhand();
            for (int i = 0; i < 5; i++)
                player.Weapon.TrySpend();

            player.Weapon.Reload(2);

            Assert.AreEqual(3, player.Weapon.Ammo);
        }

        [Test]
        public void ACharacterCanCarryNoWeaponAtAll()
        {
            var unarmed = new PlayerState("Unarmed", 70, 3, new Deck(new SystemRng(1)));

            // Null rather than a capacity of zero: "carries none" and "carries
            // an empty one" are different statements, and only one is true of
            // somebody holding a sword.
            Assert.IsNull(unarmed.Weapon);
            Assert.IsFalse(unarmed.IsArmed);
        }

        [Test]
        public void AWeaponThatHoldsNothingIsRefusedOutright()
        {
            // The way to have no weapon is to have no weapon.
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new WeaponState(0, 6));
        }

        [Test]
        public void AmmoSurvivesTheTurnUnlikeEnergy()
        {
            PlayerState player = Gunhand();
            var enemy = new EnemyState("Grunt", 30, 5);
            var manager = new CombatManager(
                new CombatContext(player, new List<EnemyState> { enemy }),
                new EventBus(), cardsPerTurn: 1);

            manager.StartCombat(new List<Card>
            {
                new Card(new CardDefinition("a", 1, new List<ICardEffect>())),
            });

            player.Weapon.TrySpend();
            player.Weapon.TrySpend();
            manager.EndTurn();

            // Energy came back; the cylinder did not. That gap is the character.
            Assert.AreEqual(3, player.Energy);
            Assert.AreEqual(4, player.Weapon.Ammo);
        }

        [Test]
        public void EveryFightStartsWithAFullCylinder()
        {
            PlayerState player = Gunhand();
            var enemy = new EnemyState("Grunt", 30, 5);
            var manager = new CombatManager(
                new CombatContext(player, new List<EnemyState> { enemy }),
                new EventBus(), cardsPerTurn: 1);

            for (int i = 0; i < 5; i++)
                player.Weapon.TrySpend();

            // Walking into the next room with an empty gun would make the reload
            // card mandatory rather than a choice.
            manager.StartCombat(new List<Card>
            {
                new Card(new CardDefinition("a", 1, new List<ICardEffect>())),
            });

            Assert.AreEqual(6, player.Weapon.Ammo);
        }

        // ---- as a card ----

        [Test]
        public void AReloadCardPutsRoundsBackAndSaysSo()
        {
            PlayerState player = Gunhand();
            player.Weapon.TrySpend();
            player.Weapon.TrySpend();

            var bus = new EventBus();
            int reported = -1;
            bus.Subscribe<AmmoChangedEvent>(e => reported = e.Ammo);

            new ReloadEffect(6).Execute(new EffectContext(player, null, bus));

            Assert.AreEqual(6, player.Weapon.Ammo);
            Assert.AreEqual(6, reported);
        }

        [Test]
        public void ReloadingAFullCylinderSaysNothing()
        {
            PlayerState player = Gunhand();
            var bus = new EventBus();
            bool announced = false;
            bus.Subscribe<AmmoChangedEvent>(_ => announced = true);

            new ReloadEffect(6).Execute(new EffectContext(player, null, bus));

            Assert.IsFalse(announced, "nothing moved, so there is nothing to redraw");
        }
    }
}
