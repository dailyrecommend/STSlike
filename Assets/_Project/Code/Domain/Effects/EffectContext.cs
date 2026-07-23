using System;
using STSlike.Domain.Actors;

namespace STSlike.Domain.Effects
{
    /// <summary>
    /// The information an effect needs when it runs: who played the card
    /// (source) and who it is aimed at (target). More fields (e.g. the full
    /// combat context for draw/energy effects) can be added later without
    /// touching existing effects.
    /// </summary>
    public sealed class EffectContext
    {
        public Combatant Source { get; }
        public Combatant Target { get; }

        public EffectContext(Combatant source, Combatant target)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Target = target; // may be null for effects that don't need a target
        }
    }
}
