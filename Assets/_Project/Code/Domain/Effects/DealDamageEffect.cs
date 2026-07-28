using System;
using STSlike.Domain.Combat;

namespace STSlike.Domain.Effects
{
    /// <summary>Deals damage to the target, then announces it on the bus.</summary>
    [Serializable]
    public sealed class DealDamageEffect : ICardEffect
    {
        public int amount;

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
            context.Bus?.Publish(new DamageDealtEvent(context.Target, amount));
        }
    }
}
