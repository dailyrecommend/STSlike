using System;

namespace STSlike.Domain.Effects
{
    /// <summary>Deals damage to the target.</summary>
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
            context.Target?.TakeDamage(amount);
        }
    }
}
