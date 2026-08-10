using STSlike.Domain.Cards;
using STSlike.Domain.Combat;

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

        public StatusDuration Duration => StatusDuration.OwnerTurns;

        public bool IsStance => false;
        public bool SurvivesBeingHurt => true;

        public int ModifyOutgoingDamage(int amount, int stacks, string tag)
        {
            if (stacks <= 0 || amount <= 0)
                return amount;

            // A weapon's output is the weapon's, not its owner's. Weakening the
            // hand on the grip does nothing to what the barrel puts out.
            if (tag == DamageTags.Shot)
                return amount;

            // Rounded down, so a 1-damage jab weakened still lands for nothing
            // rather than being quietly rounded back up to full.
            return amount * 3 / 4;
        }

        public int ModifyIncomingDamage(int amount, int stacks, string tag) => amount;

        /// <summary>Weakness blunts what you do, it does not stop you doing it.</summary>
        public bool AllowsPlaying(ICardDefinition card, int stacks) => true;

        private WeakStatus() { }
    }
}
