using System;

namespace STSlike.Domain.Actors
{
    /// <summary>
    /// An enemy during combat: a <see cref="Combatant"/> that also advertises
    /// its next-turn <see cref="Intent"/>. Stage 1 uses a fixed attack; richer
    /// behavior will move behind an injected strategy (IEnemyBehavior) later.
    /// </summary>
    public sealed class EnemyState : Combatant
    {
        private readonly int _attackDamage;

        public EnemyIntent Intent { get; private set; }

        public EnemyState(string name, int maxHp, int attackDamage)
            : base(name, maxHp)
        {
            if (attackDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(attackDamage), "Damage cannot be negative.");

            _attackDamage = attackDamage;
            PlanNextTurn();
        }

        /// <summary>Decides the intent for the upcoming turn.</summary>
        public void PlanNextTurn()
        {
            Intent = EnemyIntent.Attack(_attackDamage);
        }
    }
}
