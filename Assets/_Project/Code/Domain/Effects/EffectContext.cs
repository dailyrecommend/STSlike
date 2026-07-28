using System;
using STSlike.Domain.Actors;
using STSlike.Domain.Events;

namespace STSlike.Domain.Effects
{
    /// <summary>
    /// The information an effect needs when it runs: who played the card
    /// (source), who it is aimed at (target), and an optional event bus the
    /// effect can announce its outcome on (damage dealt, block gained). The bus
    /// is optional so pure unit tests can run effects without one.
    /// </summary>
    public sealed class EffectContext
    {
        public Combatant Source { get; }
        public Combatant Target { get; }
        public IEventBus Bus { get; }

        public EffectContext(Combatant source, Combatant target, IEventBus bus = null)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Target = target; // may be null for effects that don't need a target
            Bus = bus;
        }
    }
}
