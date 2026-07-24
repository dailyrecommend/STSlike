using System.Collections.Generic;
using NUnit.Framework;
using STSlike.Data;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Effects;

namespace STSlike.Tests.EditMode
{
    public class CardDataTests
    {
        [Test]
        public void CardData_SatisfiesICardDefinition()
        {
            ICardDefinition def = CardData.Create("strike", 1,
                new List<ICardEffect> { new DealDamageEffect(6) });

            Assert.AreEqual("strike", def.Id);
            Assert.AreEqual(1, def.Cost);
            Assert.AreEqual(1, def.Effects.Count);
        }

        // The whole chain: SO asset -> ICardDefinition -> Card -> effects run.
        [Test]
        public void CardData_WrappedInCard_ExecutesEffects()
        {
            var data = CardData.Create("bash", 2, new List<ICardEffect>
            {
                new DealDamageEffect(8),
                new GainBlockEffect(5),
            });
            var card = new Card(data);

            var player = new Combatant("Player", 70);
            var enemy = new Combatant("Enemy", 40);
            var context = new EffectContext(player, enemy);
            foreach (ICardEffect effect in card.Effects)
                effect.Execute(context);

            Assert.AreEqual(32, enemy.CurrentHp);
            Assert.AreEqual(5, player.Block);
        }
    }
}
