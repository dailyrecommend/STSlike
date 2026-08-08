using System;
using STSlike.Domain.Combat;
using STSlike.Domain.Statuses;

namespace STSlike.Domain.Effects
{
    /// <summary>Leaves the target open, taking more damage for a few turns.</summary>
    [Serializable]
    public sealed class ApplyVulnerableEffect : ICardEffect, IDescribableEffect
    {
        public int turns = 2;

        public int DisplayValue => turns;

        public ApplyVulnerableEffect() { }

        public ApplyVulnerableEffect(int turns)
        {
            this.turns = turns;
        }

        public void Execute(EffectContext context)
        {
            if (context.Target == null || turns <= 0)
                return;

            context.Target.Statuses.Apply(VulnerableStatus.Instance, turns);
            context.Bus?.Publish(new StatusChangedEvent(context.Target, VulnerableStatus.StatusId));
        }
    }
}
