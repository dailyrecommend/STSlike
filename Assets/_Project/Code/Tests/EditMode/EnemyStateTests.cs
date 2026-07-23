using System;
using NUnit.Framework;
using STSlike.Domain.Actors;

namespace STSlike.Tests.EditMode
{
    public class EnemyStateTests
    {
        [Test]
        public void NewEnemy_AdvertisesAttackIntent()
        {
            var enemy = new EnemyState("Cultist", 48, attackDamage: 12);

            Assert.AreEqual(IntentType.Attack, enemy.Intent.Type);
            Assert.AreEqual(12, enemy.Intent.Amount);
        }

        [Test]
        public void Enemy_IsACombatant_TakesDamage()
        {
            var enemy = new EnemyState("Cultist", 48, attackDamage: 12);

            enemy.TakeDamage(20);

            Assert.AreEqual(28, enemy.CurrentHp);
            Assert.IsFalse(enemy.IsDead);
        }

        [Test]
        public void PlanNextTurn_RefreshesIntent()
        {
            var enemy = new EnemyState("Cultist", 48, attackDamage: 12);

            enemy.PlanNextTurn();

            Assert.AreEqual(IntentType.Attack, enemy.Intent.Type);
            Assert.AreEqual(12, enemy.Intent.Amount);
        }

        [Test]
        public void EnemyIntent_Attack_RejectsNegativeDamage()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyIntent.Attack(-1));
        }
    }
}
