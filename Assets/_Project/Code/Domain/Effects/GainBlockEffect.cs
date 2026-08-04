using System;
using STSlike.Domain.Combat;

namespace STSlike.Domain.Effects
{
    /// <summary>Grants block to the caster, then announces it on the bus.</summary>
    [Serializable]
    public sealed class GainBlockEffect : ICardEffect, IDescribableEffect
    {
        public int amount;

        public int DisplayValue => amount;

        public GainBlockEffect() { }

        public GainBlockEffect(int amount)
        {
            this.amount = amount;
        }

        public void Execute(EffectContext context)
        {
            context.Source.GainBlock(amount);
            context.Bus?.Publish(new BlockGainedEvent(context.Source, amount));
        }
    }
}
