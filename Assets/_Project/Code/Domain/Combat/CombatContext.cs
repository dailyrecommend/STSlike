using System;
using STSlike.Domain.Actors;

namespace STSlike.Domain.Combat
{
    /// <summary>
    /// The single owner of a combat's state: the player, the enemy, and the
    /// current phase. The phase is settable only from within the domain
    /// (internal), so the manager drives transitions while views read them.
    /// Stage 1 has one enemy; a list can replace it without touching callers.
    /// </summary>
    public sealed class CombatContext
    {
        public PlayerState Player { get; }
        public EnemyState Enemy { get; }
        public CombatPhase Phase { get; internal set; }

        /// <summary>
        /// Which player turn this is, counting from 1. Zero means the fight has
        /// been set up but the first turn has not begun. Resets every combat —
        /// it is a fight-scoped number, not a run-scoped one.
        /// </summary>
        public int TurnNumber { get; internal set; }

        public bool IsOver => Phase == CombatPhase.Won || Phase == CombatPhase.Lost;

        public CombatContext(PlayerState player, EnemyState enemy)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            Phase = CombatPhase.PlayerTurn;
        }
    }
}
