using System;
using System.Collections.Generic;
using STSlike.Domain.Actors;

namespace STSlike.Domain.Combat
{
    /// <summary>
    /// The single owner of a combat's state: the player, the enemies, and the
    /// current phase. The phase is settable only from within the domain
    /// (internal), so the manager drives transitions while views read them.
    /// </summary>
    public sealed class CombatContext
    {
        private readonly List<EnemyState> _enemies;

        public PlayerState Player { get; }

        /// <summary>Everyone the player is fighting, in display order.</summary>
        public IReadOnlyList<EnemyState> Enemies => _enemies;

        public CombatPhase Phase { get; internal set; }

        /// <summary>
        /// Which player turn this is, counting from 1. Zero means the fight has
        /// been set up but the first turn has not begun. Resets every combat.
        /// </summary>
        public int TurnNumber { get; internal set; }

        public bool IsOver => Phase == CombatPhase.Won || Phase == CombatPhase.Lost;

        /// <summary>True once nothing is left standing.</summary>
        public bool AllEnemiesDead
        {
            get
            {
                foreach (EnemyState enemy in _enemies)
                {
                    if (!enemy.IsDead)
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// The default target: the first enemy still alive. Untargeted cards and
        /// single-enemy UI use this, so neither has to deal with a list.
        /// </summary>
        public EnemyState Enemy
        {
            get
            {
                foreach (EnemyState enemy in _enemies)
                {
                    if (!enemy.IsDead)
                        return enemy;
                }
                return _enemies[0];
            }
        }

        public CombatContext(PlayerState player, params EnemyState[] enemies)
            : this(player, (IReadOnlyList<EnemyState>)enemies)
        {
        }

        public CombatContext(PlayerState player, IReadOnlyList<EnemyState> enemies)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));

            if (enemies == null || enemies.Count == 0)
                throw new ArgumentException("A combat needs at least one enemy.", nameof(enemies));

            _enemies = new List<EnemyState>(enemies);
            foreach (EnemyState enemy in _enemies)
            {
                if (enemy == null)
                    throw new ArgumentException("Enemy list contains a null.", nameof(enemies));
            }

            Phase = CombatPhase.PlayerTurn;
        }
    }
}
