namespace STSlike.Domain.Cards
{
    /// <summary>
    /// Who a card applies to, and therefore whether the player is asked to
    /// choose.
    ///
    /// Deliberately separate from <see cref="CardType"/>. The two look like they
    /// line up — attacks hit enemies, skills do not — but they come apart almost
    /// immediately: an attack can sweep the whole room or strike at random, and
    /// a skill can need a single enemy picked to put a debuff on. Deriving one
    /// from the other would mean every such card had to fight the rule instead
    /// of stating what it does.
    /// </summary>
    public enum CardTarget
    {
        /// <summary>One enemy, chosen by the player. The only mode that asks.</summary>
        SingleEnemy = 0,

        /// <summary>Everyone still standing in the room.</summary>
        AllEnemies = 1,

        /// <summary>One enemy, chosen by the run's seeded random source.</summary>
        RandomEnemy = 2,

        /// <summary>The player. Blocks, heals, draws.</summary>
        Self = 3,
    }
}
