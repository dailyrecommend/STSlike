using System;

namespace STSlike.Domain.Actors
{
    /// <summary>
    /// An enemy during combat: a <see cref="Combatant"/> that also advertises
    /// what it will do next. The decision itself belongs to an injected
    /// <see cref="IEnemyBehavior"/>, so stats and AI can be mixed freely.
    /// </summary>
    public sealed class EnemyState : Combatant
    {
        private readonly IEnemyBehavior _behavior;

        /// <summary>Identifier of the definition this was built from, for art lookups.</summary>
        public string DefinitionId { get; }

        public EnemyIntent Intent { get; private set; }

        public EnemyState(string name, int maxHp, IEnemyBehavior behavior, string definitionId = null)
            : base(name, maxHp)
        {
            _behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? name : definitionId;

            PlanNextTurn(turnNumber: 1);
        }

        /// <summary>Convenience for a plain attacker; mostly used by tests.</summary>
        public EnemyState(string name, int maxHp, int attackDamage)
            : this(name, maxHp, new ScriptedEnemyBehavior(new[]
            {
                new EnemyMove(IntentType.Attack, attackDamage),
            }))
        {
        }

        /// <summary>Decides what to telegraph for the coming turn.</summary>
        public void PlanNextTurn(int turnNumber)
        {
            Intent = _behavior.PlanNext(this, turnNumber) ?? EnemyIntent.Unknown();
        }
    }
}
