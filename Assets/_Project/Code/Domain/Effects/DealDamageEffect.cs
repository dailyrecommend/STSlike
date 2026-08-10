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

            // A blow that got through breaks anything that took concentration to
            // hold. Blocked damage does not: the guard held, so the posture did.
            if (lost > 0 && context.Target.Statuses.DropOnHurt())
                context.Bus?.Publish(new StatusChangedEvent(context.Target, null));
        }
    }
}
