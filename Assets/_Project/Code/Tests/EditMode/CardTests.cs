using System;
using System.Collections.Generic;
using NUnit.Framework;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Effects;

namespace STSlike.Tests.EditMode
{
    public class CardTests
    {
        private static CardDefinition BashDefinition() => new CardDefinition(
            id: "bash",
            cost: 2,
            effects: new List<ICardEffect>
            {
                new DealDamageEffect(8),
                new GainBlockEffect(5),
            });

        [Test]
        public void CardDefinition_ExposesIdCostEffects()
        {
            var def = BashDefinition();

            Assert.AreEqual("bash", def.Id);
            Assert.AreEqual(2, def.Cost);
            Assert.AreEqual(2, def.Effects.Count);
        }

        [Test]
        public void Card_ForwardsDefinitionData()
        {
            var card = new Card(BashDefinition());

            Assert.AreEqual("bash", card.Id);
            Assert.AreEqual(2, card.Cost);
            Assert.AreEqual(2, card.Effects.Count);
        }

        // Integration: a Card's effects run against combatants as expected.
        [Test]
        public void Card_EffectsApplyToCombatants()
        {
            var player = new Combatant("Player", 70);
            var enemy = new Combatant("Enemy", 40);
            var card = new Card(BashDefinition());

            var context = new EffectContext(player, enemy);
            foreach (var effect in card.Effects)
                effect.Execute(context);

            Assert.AreEqual(32, enemy.CurrentHp); // 8 damage
            Assert.AreEqual(5, player.Block);      // 5 block to caster
        }

        [Test]
        public void Card_RejectsNullDefinition()
        {
            Assert.Throws<ArgumentNullException>(() => new Card(null));
        }

        [Test]
        public void CardDefinition_RejectsEmptyId()
        {
            Assert.Throws<ArgumentException>(
                () => new CardDefinition(" ", 1, new List<ICardEffect>()));
        }

        [Test]
        public void CardDefinition_RejectsNegativeCost()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CardDefinition("x", -1, new List<ICardEffect>()));
        }
    }
}
