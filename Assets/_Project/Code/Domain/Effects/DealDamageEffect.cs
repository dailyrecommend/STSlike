using System;
using STSlike.Domain.Actors;
using STSlike.Domain.Combat;

namespace STSlike.Domain.Effects
{
    /// <summary>Deals damage to the target, then announces it on the bus.</summary>
    [Serializable]
    public sealed class DealDamageEffect : ICardEffect, IDescribableEffect, IPreviewableEffect
    {
        public int amount;

        public int DisplayValue => amount;

        public int Preview(Combatant source, Combatant target) =>
            DamageCalculation.Resolve(source, target, amount);

        public DealDamageEffect() { }

        public DealDamageEffect(int amount)
        {
            this.amount = amount;
        }

        public void Execute(EffectContext context)
        {
            if (context.Target == null)
                return;

            // The same call the card face and the enemy's intent make to decide
            // what to print, so what was promised is what happens.
            int landing = DamageCalculation.Resolve(context.Source, context.Target, amount);

            int lost = context.Target.TakeDamage(landing);
            context.Bus?.Publish(
                new DamageDealtEvent(context.Source, context.Target, landing, lost));
        }
    }
}
