using STSlike.Domain.Cards;

namespace STSlike.Domain.Statuses
{
    /// <summary>
    /// A condition a combatant can be under.
    ///
    /// Stateless and shared, the same way a card definition is: the combatant
    /// holds how many stacks it has, the status itself only says what stacks
    /// mean. So one instance serves every figure in the fight.
    ///
    /// The hooks are how a status reaches into a calculation without that
    /// calculation knowing it exists. Adding a new condition is a new file —
    /// the damage formula never grows a case for it.
    /// </summary>
    public interface IStatusEffect
    {
        /// <summary>Stable key, used by saves and by icon lookups.</summary>
        string Id { get; }

        /// <summary>When this condition's clock runs out.</summary>
        StatusDuration Duration { get; }

        /// <summary>
        /// Damage this combatant is about to deal. The tag says what kind of
        /// blow it is, so a condition can decline to touch some of them — a
        /// weakened gunhand still fires a full-strength round.
        /// </summary>
        int ModifyOutgoingDamage(int amount, int stacks, string tag);

        /// <summary>Damage this combatant is about to take.</summary>
        int ModifyIncomingDamage(int amount, int stacks, string tag);

        /// <summary>
        /// Whether this condition permits playing that card at all.
        ///
        /// A veto rather than a filter: any condition saying no is enough. This
        /// is how a stance narrows a hand to what it allows without the play
        /// rules having to know stances exist — they ask the conditions and take
        /// the answer.
        /// </summary>
        bool AllowsPlaying(ICardDefinition card, int stacks);
    }
}
