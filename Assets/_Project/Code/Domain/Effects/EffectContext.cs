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

        /// <summary>
        /// Whoever played this, as a player — null when it was not one.
        ///
        /// <see cref="Combatant"/> is only health and block, which is all an
        /// attack needs, so effects that reach for a deck or for energy had no
        /// way to ask. Those are player-only concepts here: enemies have neither,
        /// and null is the honest answer rather than an error, so an effect
        /// written for a card simply does nothing if an enemy ever runs it.
        /// </summary>
        public PlayerState Player => Source as PlayerState;

        public EffectContext(Combatant source, Combatant target, IEventBus bus = null)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Target = target; // may be null for effects that don't need a target
            Bus = bus;
        }
    }
}
