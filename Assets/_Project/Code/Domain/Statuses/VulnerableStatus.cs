using STSlike.Domain.Cards;
using STSlike.Domain.Combat;

namespace STSlike.Domain.Statuses
{
    /// <summary>
    /// Broken: this combatant takes 50% more damage — from anything but weapon
    /// fire, which is settled by the weapon alone.
    ///
    /// As with Weakness, stacks are turns rather than severity: the multiplier
    /// is the same whether it was applied once or three times over.
    /// </summary>
    public sealed class VulnerableStatus : IStatusEffect
    {
        public const string StatusId = "Broken";

        /// <summary>Shared: the status carries no per-figure state.</summary>
        public static readonly VulnerableStatus Instance = new VulnerableStatus();

        public string Id => StatusId;

        public StatusDuration Duration => StatusDuration.OwnerTurns;

        public bool IsStance => false;
        public bool SurvivesBeingHurt => true;

        public int ModifyOutgoingDamage(int amount, int stacks, string tag) => amount;

        public bool AllowsPlaying(ICardDefinition card, int stacks) => true;

        public int ModifyIncomingDamage(int amount, int stacks, string tag)
        {
            if (stacks <= 0 || amount <= 0)
                return amount;

            // Weapon fire is settled by the weapon and nothing else. A round
            // does what a round does whether the target is reeling or braced.
            if (tag == DamageTags.Shot)
                return amount;

            return amount * 3 / 2;
        }

        private VulnerableStatus() { }
    }
}
