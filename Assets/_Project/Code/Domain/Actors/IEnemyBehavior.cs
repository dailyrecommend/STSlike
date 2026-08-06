namespace STSlike.Domain.Actors
{
    /// <summary>
    /// Decides what an enemy announces next (Strategy).
    ///
    /// Kept separate from <see cref="EnemyState"/> so an enemy's stats and its
    /// AI can vary independently: the same routine can be reused by a tougher
    /// version, and a smarter routine (react to the player's block, enrage at
    /// low health) is a new class rather than a change to the enemy itself.
    /// </summary>
    public interface IEnemyBehavior
    {
        /// <summary>Chooses the move to telegraph for the coming turn.</summary>
        EnemyIntent PlanNext(EnemyState self, int turnNumber);
    }
}
