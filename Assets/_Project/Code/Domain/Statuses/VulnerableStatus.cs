using STSlike.Domain.Cards;

namespace STSlike.Domain.Statuses
{
    /// <summary>
    /// Vulnerable: this combatant takes 50% more attack damage.
    ///
    /// As with Weak, stacks are turns rather than severity — the multiplier is
    /// the same whether it was applied once or three times over.
    /// </summary>
    public sealed class VulnerableStatus : IStatusEffect
    {
        public const string StatusId = "Broken";

        /// <summary>Shared: the status carries no per-figure state.</summary>
        public static readonly VulnerableStatus Instance = new VulnerableStatus();

        public string Id => StatusId;

        public StatusDuration Duration => StatusDuration.OwnerTurns;

        public int ModifyOutgoingDamage(int amount, int stacks, string tag) => amount;

        public bool AllowsPlaying(ICardDefinition card, int stacks) => true;

        /// <summary>
        /// Every kind of blow, gunfire included. This is a hole in the defender's
        /// guard rather than a fault in the attacker, so nothing about how the
        /// damage was produced makes it land any softer.
        /// </summary>
        public int ModifyIncomingDamage(int amount, int stacks, string tag)
        {
            if (stacks <= 0 || amount <= 0)
                return amount;

            return amount * 3 / 2;
        }

        private VulnerableStatus() { }
    }
}
