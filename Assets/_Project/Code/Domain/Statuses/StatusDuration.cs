namespace STSlike.Domain.Statuses
{
    /// <summary>
    /// How a condition's clock runs.
    ///
    /// Most conditions count down on their owner, but not all: a mark that only
    /// exists to be exploited within a single turn has to go when that turn
    /// does, wherever it happens to be sitting. Saying so here keeps the turn
    /// loop from growing a list of special cases.
    /// </summary>
    public enum StatusDuration
    {
        /// <summary>Loses a step at the end of its owner's own turn.</summary>
        OwnerTurns = 0,

        /// <summary>
        /// Wiped entirely when the player's turn ends, whoever is carrying it.
        /// For marks built up and cashed in within one turn.
        /// </summary>
        PlayerTurn = 1,

        /// <summary>Never runs out on its own; something has to remove it.</summary>
        Persistent = 2,
    }
}
