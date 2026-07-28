using System.Collections.Generic;
using NUnit.Framework;
using STSlike.Domain.Actors;
using STSlike.Domain.Combat;
using STSlike.Domain.Effects;
using STSlike.Domain.Events;

namespace STSlike.Tests.EditMode
{
    public class CardEffectTests
    {
        [Test]
        public void DealDamageEffect_ReducesTargetHp()
        {
            var source = new Combatant("Player", 70);
            var target = new Combatant("Enemy", 40);
            var effect = new DealDamageEffect(9);

            effect.Execute(new EffectContext(source, target));

            Assert.AreEqual(31, target.CurrentHp);
            Assert.AreEqual(70, source.CurrentHp); // source untouched
        }

        [Test]
        public void GainBlockEffect_AddsBlockToSource_NotTarget()
        {
            var source = new Combatant("Player", 70);
            var target = new Combatant("Enemy", 40);
            var effect = new GainBlockEffect(5);

            effect.Execute(new EffectContext(source, target));

            Assert.AreEqual(5, source.Block);
            Assert.AreEqual(0, target.Block);
        }

        [Test]
        public void DealDamageEffect_RespectsTargetBlock()
        {
            var source = new Combatant("Player", 70);
            var target = new Combatant("Enemy", 40);
            target.GainBlock(4);

            new DealDamageEffect(10).Execute(new EffectContext(source, target));

            Assert.AreEqual(34, target.CurrentHp); // 10 - 4 block = 6 to hp
            Assert.AreEqual(0, target.Block);
        }

        [Test]
        public void DealDamageEffect_PublishesDamageDealtEvent()
        {
            var bus = new EventBus();
            DamageDealtEvent captured = default;
            int count = 0;
            bus.Subscribe<DamageDealtEvent>(e => { captured = e; count++; });

            var source = new Combatant("Player", 70);
            var target = new Combatant("Enemy", 40);
            new DealDamageEffect(7).Execute(new EffectContext(source, target, bus));

            Assert.AreEqual(1, count);
            Assert.AreEqual(7, captured.Amount);
            Assert.AreSame(target, captured.Target);
        }

        [Test]
        public void GainBlockEffect_PublishesBlockGainedEvent()
        {
            var bus = new EventBus();
            BlockGainedEvent captured = default;
            int count = 0;
            bus.Subscribe<BlockGainedEvent>(e => { captured = e; count++; });

            var source = new Combatant("Player", 70);
            var target = new Combatant("Enemy", 40);
            new GainBlockEffect(5).Execute(new EffectContext(source, target, bus));

            Assert.AreEqual(1, count);
            Assert.AreEqual(5, captured.Amount);
            Assert.AreSame(source, captured.Actor);
        }

        [Test]
        public void Effects_WithoutBus_DoNotThrow()
        {
            var source = new Combatant("Player", 70);
            var target = new Combatant("Enemy", 40);
            var context = new EffectContext(source, target); // no bus

            Assert.DoesNotThrow(() => new DealDamageEffect(6).Execute(context));
            Assert.DoesNotThrow(() => new GainBlockEffect(4).Execute(context));
        }

        // The payoff of Strategy: a card is just a list of ICardEffect, and the
        // caller runs them uniformly without knowing what each one does.
        [Test]
        public void MultipleEffects_ExecuteUniformlyAsAList()
        {
            var player = new Combatant("Player", 70);
            var enemy = new Combatant("Enemy", 40);

            // e.g. a "Bash": deal 8, then gain 5 block.
            var effects = new List<ICardEffect>
            {
                new DealDamageEffect(8),
                new GainBlockEffect(5),
            };

            var context = new EffectContext(player, enemy);
            foreach (var effect in effects)
                effect.Execute(context);

            Assert.AreEqual(32, enemy.CurrentHp);
            Assert.AreEqual(5, player.Block);
        }
    }
}
