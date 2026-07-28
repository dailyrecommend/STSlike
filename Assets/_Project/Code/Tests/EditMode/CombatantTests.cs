using System;
using NUnit.Framework;
using STSlike.Domain.Actors;

namespace STSlike.Tests.EditMode
{
    public class CombatantTests
    {
        [Test]
        public void NewCombatant_StartsAtFullHp_NoBlock_Alive()
        {
            var c = new Combatant("Ironclad", 80);

            Assert.AreEqual(80, c.MaxHp);
            Assert.AreEqual(80, c.CurrentHp);
            Assert.AreEqual(0, c.Block);
            Assert.IsFalse(c.IsDead);
        }

        [Test]
        public void TakeDamage_WithNoBlock_ReducesHp()
        {
            var c = new Combatant("Dummy", 50);

            c.TakeDamage(12);

            Assert.AreEqual(38, c.CurrentHp);
            Assert.AreEqual(0, c.Block);
        }

        [Test]
        public void TakeDamage_FullyAbsorbedByBlock_LeavesHpUntouched()
        {
            var c = new Combatant("Dummy", 50);
            c.GainBlock(8);

            c.TakeDamage(6);

            Assert.AreEqual(50, c.CurrentHp);
            Assert.AreEqual(2, c.Block);
        }

        [Test]
        public void TakeDamage_PartiallyAbsorbedByBlock_SpillsToHp()
        {
            var c = new Combatant("Dummy", 50);
            c.GainBlock(3);

            c.TakeDamage(10);

            Assert.AreEqual(43, c.CurrentHp);
            Assert.AreEqual(0, c.Block);
        }

        [Test]
        public void TakeDamage_CannotDropHpBelowZero()
        {
            var c = new Combatant("Dummy", 10);

            c.TakeDamage(999);

            Assert.AreEqual(0, c.CurrentHp);
            Assert.IsTrue(c.IsDead);
        }

        [Test]
        public void ResetBlock_ClearsBlock()
        {
            var c = new Combatant("Dummy", 50);
            c.GainBlock(15);

            c.ResetBlock();

            Assert.AreEqual(0, c.Block);
        }

        [Test]
        public void GainBlock_Accumulates()
        {
            var c = new Combatant("Dummy", 50);

            c.GainBlock(5);
            c.GainBlock(4);

            Assert.AreEqual(9, c.Block);
        }

        [Test]
        public void Heal_RestoresHp_ClampedToMax()
        {
            var c = new Combatant("Dummy", 50);
            c.TakeDamage(30); // at 20

            c.Heal(8);
            Assert.AreEqual(28, c.CurrentHp);

            c.Heal(999);
            Assert.AreEqual(50, c.CurrentHp); // never above max
        }

        [Test]
        public void Heal_DoesNotReviveTheDead()
        {
            var c = new Combatant("Dummy", 10);
            c.TakeDamage(10);

            c.Heal(5);

            Assert.AreEqual(0, c.CurrentHp);
            Assert.IsTrue(c.IsDead);
        }

        [Test]
        public void RestoreToFull_SetsHpToMax()
        {
            var c = new Combatant("Dummy", 50);
            c.TakeDamage(40);

            c.RestoreToFull();

            Assert.AreEqual(50, c.CurrentHp);
        }

        [Test]
        public void Constructor_RejectsInvalidMaxHp()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Combatant("X", 0));
        }
    }
}
