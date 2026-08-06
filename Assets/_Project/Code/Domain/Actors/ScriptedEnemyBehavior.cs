using System;
using System.Collections.Generic;

namespace STSlike.Domain.Actors
{
    /// <summary>
    /// Repeats an authored list of moves in order.
    ///
    /// A fixed rotation is deliberately readable: the player is meant to learn
    /// "this one hits twice then guards" and plan around it. Randomised enemies
    /// are a different behavior class, used sparingly, because a fight nobody
    /// can predict is a fight nobody can play well.
    /// </summary>
    public sealed class ScriptedEnemyBehavior : IEnemyBehavior
    {
        private readonly List<EnemyMove> _moves;

        public ScriptedEnemyBehavior(IReadOnlyList<EnemyMove> moves)
        {
            if (moves == null || moves.Count == 0)
                throw new ArgumentException("An enemy needs at least one move.", nameof(moves));

            _moves = new List<EnemyMove>(moves);
        }

        public EnemyIntent PlanNext(EnemyState self, int turnNumber)
        {
            // Turn numbers start at 1, so the first turn shows the first move.
            int index = Math.Max(0, turnNumber - 1) % _moves.Count;
            return EnemyIntent.From(_moves[index], index);
        }
    }
}
