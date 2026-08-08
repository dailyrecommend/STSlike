namespace STSlike.Domain.Statuses
{
    /// <summary>
    /// Weak: this combatant deals 25% less attack damage.
    ///
    /// Stacks are turns, not strength — being weakened twice over lasts longer,
    /// it does not hit softer. That is why the reduction ignores the count.
    /// </summary>
    public sealed class WeakStatus : IStatusEffect
    {
        public const string StatusId = "Weakness";

        /// <summary>Shared: the status carries no per-figure state.</summary>
        public static readonly WeakStatus Instance = new WeakStatus();

        public string Id => StatusId;

        public int ModifyOutgoingDamage(int amount, int stacks)
        {
            if (stacks <= 0 || amount <= 0)
                return amount;

            // Rounded down, so a 1-damage jab weakened still lands for nothing
            // rather than being quietly rounded back up to full.
            return amount * 3 / 4;
        }

        public int ModifyIncomingDamage(int amount, int stacks) => amount;

        private WeakStatus() { }
    }
}
