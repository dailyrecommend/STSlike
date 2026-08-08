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

        /// <summary>Damage this combatant is about to deal.</summary>
        int ModifyOutgoingDamage(int amount, int stacks);

        /// <summary>Damage this combatant is about to take.</summary>
        int ModifyIncomingDamage(int amount, int stacks);
    }
}
