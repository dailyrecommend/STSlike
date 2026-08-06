using System;
using System.Collections.Generic;
using NUnit.Framework;
using STSlike.Domain.Actors;

namespace STSlike.Tests.EditMode
{
    public class EnemyStateTests
    {
        private static EnemyState Scripted(params EnemyMove[] moves) =>
            new EnemyState("Cultist", 48, new ScriptedEnemyBehavior(moves));

        [Test]
        public void NewEnemy_AdvertisesItsFirstMove()
        {
            EnemyState enemy = Scripted(new EnemyMove(IntentType.Attack, 12));

            Assert.AreEqual(IntentType.Attack, enemy.Intent.Type);
            Assert.AreEqual(12, enemy.Intent.Amount);
        }

        [Test]
        public void Enemy_IsACombatant_TakesDamage()
        {
            EnemyState enemy = Scripted(new EnemyMove(IntentType.Attack, 12));

            enemy.TakeDamage(20);

            Assert.AreEqual(28, enemy.CurrentHp);
            Assert.IsFalse(enemy.IsDead);
        }

        // The readable rhythm players are meant to learn: it must repeat in order.
        [Test]
        public void ScriptedBehavior_CyclesThroughItsMoves()
        {
            EnemyState enemy = Scripted(
                new EnemyMove(IntentType.Attack, 8),
                new EnemyMove(IntentType.Attack, 8),
                new EnemyMove(IntentType.Defend, 6));

            Assert.AreEqual(IntentType.Attack, enemy.Intent.Type);

            enemy.PlanNextTurn(2);
            Assert.AreEqual(IntentType.Attack, enemy.Intent.Type);

            enemy.PlanNextTurn(3);
            Assert.AreEqual(IntentType.Defend, enemy.Intent.Type);
            Assert.AreEqual(6, enemy.Intent.Amount);

            enemy.PlanNextTurn(4);   // wraps back to the start
            Assert.AreEqual(IntentType.Attack, enemy.Intent.Type);
        }

        [Test]
        public void SimpleConstructor_MakesAPlainAttacker()
        {
            var enemy = new EnemyState("Grunt", 30, attackDamage: 7);

            Assert.AreEqual(IntentType.Attack, enemy.Intent.Type);
            Assert.AreEqual(7, enemy.Intent.Amount);

            enemy.PlanNextTurn(5);
            Assert.AreEqual(7, enemy.Intent.Amount);   // always the same move
        }

        [Test]
        public void EnemyIntent_RejectsNegativeAmounts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyIntent.Attack(-1));
        }

        [Test]
        public void ScriptedBehavior_NeedsAtLeastOneMove()
        {
            Assert.Throws<ArgumentException>(
                () => new ScriptedEnemyBehavior(new List<EnemyMove>()));
        }

        [Test]
        public void DefinitionId_IsKeptForArtLookups()
        {
            var enemy = new EnemyState("Sentry Bot", 20,
                new ScriptedEnemyBehavior(new[] { new EnemyMove(IntentType.Attack, 5) }),
                definitionId: "sentry");

            Assert.AreEqual("sentry", enemy.DefinitionId);
        }
    }
}
