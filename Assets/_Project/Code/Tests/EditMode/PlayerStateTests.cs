using System;
using NUnit.Framework;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Randomness;

namespace STSlike.Tests.EditMode
{
    public class PlayerStateTests
    {
        private static PlayerState MakePlayer(int maxEnergy = 3) =>
            new PlayerState("Ironclad", 80, maxEnergy, new Deck(new SystemRng(0)));

        [Test]
        public void NewPlayer_HasDeck_AndStartsWithZeroEnergy()
        {
            var player = MakePlayer();

            Assert.IsNotNull(player.Deck);
            Assert.AreEqual(0, player.Energy);
            Assert.AreEqual(3, player.MaxEnergy);
            Assert.AreEqual(80, player.CurrentHp); // inherited from Combatant
        }

        [Test]
        public void RefillEnergy_SetsEnergyToMax()
        {
            var player = MakePlayer();

            player.RefillEnergy();

            Assert.AreEqual(3, player.Energy);
        }

        [Test]
        public void SpendEnergy_ReducesEnergy()
        {
            var player = MakePlayer();
            player.RefillEnergy();

            player.SpendEnergy(2);

            Assert.AreEqual(1, player.Energy);
        }

        [Test]
        public void CanAfford_ReflectsAvailableEnergy()
        {
            var player = MakePlayer();
            player.RefillEnergy(); // 3

            Assert.IsTrue(player.CanAfford(3));
            Assert.IsFalse(player.CanAfford(4));
        }

        [Test]
        public void SpendEnergy_WhenNotEnough_Throws()
        {
            var player = MakePlayer();
            player.RefillEnergy(); // 3

            Assert.Throws<InvalidOperationException>(() => player.SpendEnergy(4));
        }

        [Test]
        public void Player_IsACombatant_TakesDamageThroughBlock()
        {
            var player = MakePlayer();
            player.GainBlock(5);

            player.TakeDamage(8);

            Assert.AreEqual(77, player.CurrentHp); // 8 - 5 block = 3 to hp
        }
    }
}
