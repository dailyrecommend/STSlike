using System;
using STSlike.Domain.Combat;
using STSlike.Domain.Statuses;

namespace STSlike.Domain.Effects
{
    /// <summary>
    /// Weakens the target for a number of turns.
    ///
    /// One effect class per condition rather than a single "apply status" that
    /// picks one: a condition is code, so naming it in code costs nothing, and
    /// it keeps the inspector to a flat dropdown instead of a dropdown inside a
    /// dropdown. Adding a condition stays a matter of new files only.
    /// </summary>
    [Serializable]
    public sealed class ApplyWeakEffect : ICardEffect, IDescribableEffect
    {
        public int turns = 2;

        public int DisplayValue => turns;

        public ApplyWeakEffect() { }

        public ApplyWeakEffect(int turns)
        {
            this.turns = turns;
        }

        public void Execute(EffectContext context)
        {
            if (context.Target == null || turns <= 0)
                return;

            context.Target.Statuses.Apply(WeakStatus.Instance, turns);
            context.Bus?.Publish(new StatusChangedEvent(context.Target, WeakStatus.StatusId));
        }
    }
}
