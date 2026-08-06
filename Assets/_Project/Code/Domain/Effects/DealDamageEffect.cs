using System;
using STSlike.Domain.Combat;

namespace STSlike.Domain.Effects
{
    /// <summary>Deals damage to the target, then announces it on the bus.</summary>
    [Serializable]
    public sealed class DealDamageEffect : ICardEffect, IDescribableEffect
    {
        public int amount;

        public int DisplayValue => amount;

        public DealDamageEffect() { }

        public DealDamageEffect(int amount)
        {
            this.amount = amount;
        }

        public void Execute(EffectContext context)
        {
            if (context.Target == null)
                return;

            context.Target.TakeDamage(amount);
            context.Bus?.Publish(new DamageDealtEvent(context.Source, context.Target, amount));
        }
    }
}
