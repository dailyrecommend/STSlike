using STSlike.Domain.Actors;

namespace STSlike.Domain.Effects
{
    /// <summary>
    /// An effect whose printed number depends on who is involved.
    ///
    /// Most do not: "gain 5 block" is 5 whoever reads it. Damage is the odd one
    /// out, because conditions on the attacker and the defender change it before
    /// it lands — and a card that says 2 while dealing 1 is worse than a card
    /// that says nothing.
    ///
    /// Opt-in rather than part of <see cref="IDescribableEffect"/> so effects
    /// that print a constant are not made to implement a method that would only
    /// return that constant back.
    /// </summary>
    public interface IPreviewableEffect
    {
        /// <summary>
        /// What this would show right now. Either party may be null — a card
        /// inspected outside a fight has nobody to aim at, and then the authored
        /// number is the honest answer.
        /// </summary>
        int Preview(Combatant source, Combatant target);
    }
}
