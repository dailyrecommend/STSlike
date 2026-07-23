namespace STSlike.Domain.Combat
{
    /// <summary>
    /// Where a combat currently is. Stage 1 drives these transitions with
    /// simple manager methods; this enum is the seam a proper state machine
    /// will formalize later (combat / event / shop / map flow).
    /// </summary>
    public enum CombatPhase
    {
        PlayerTurn,
        EnemyTurn,
        Won,
        Lost,
    }
}
