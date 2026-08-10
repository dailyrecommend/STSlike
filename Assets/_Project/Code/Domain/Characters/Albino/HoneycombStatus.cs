using STSlike.Domain.Cards;
using STSlike.Domain.Statuses;

namespace STSlike.Domain.Characters.Albino
{
    /// <summary>
    /// Honeycomb: a wound that feeds on itself.
    ///
    /// Every round that lands leaves half its damage behind as a stack, and the
    /// next round carries all of those stacks as extra damage. So each shot is
    /// worth about half again as much as the one before it — barely noticeable
    /// at first, brutal by the sixth. A full cylinder into one target is worth
    /// roughly four times what the same rounds spread around would be.
    ///
    /// Feeding it half rather than all of the damage is what keeps the curve a
    /// curve. Feeding it everything doubles each shot instead, and one card
    /// ends up deciding every fight.
    ///
    /// Unlike Weakness or Broken, stacks here are damage rather than turns —
    /// the number over the enemy is how much the next round gains.
    ///
    /// Wiped at the end of the player's turn, so the chain has to be built and
    /// cashed within it. That is what forces the cylinder to be spent in one
    /// burst rather than trickled out.
    ///
    /// Carries no damage hook of its own: the bonus is folded into the round
    /// before the shooter's conditions apply, so a firing stance amplifies the
    /// mark along with the shot. Only gunfire touches it, so the arithmetic
    /// lives with the effect that fires — the rule for how fast it grows lives
    /// here.
    /// </summary>
    public sealed class HoneycombStatus : IStatusEffect
    {
        public const string StatusId = "Honeycomb";

        public static readonly HoneycombStatus Instance = new HoneycombStatus();

        public string Id => StatusId;

        public StatusDuration Duration => StatusDuration.PlayerTurn;

        public bool IsStance => false;
        public bool SurvivesBeingHurt => true;

        /// <summary>
        /// How much a round of this size deepens the wound. Half, rounded down,
        /// so a glancing shot leaves nothing and the chain has to be started
        /// with something real.
        /// </summary>
        public static int StacksFrom(int damage) => damage > 0 ? damage / 2 : 0;

        // The stacks are the bonus, so nothing here needs to modify damage —
        // the shot reads the count directly.
        public int ModifyOutgoingDamage(int amount, int stacks, string tag) => amount;
        public int ModifyIncomingDamage(int amount, int stacks, string tag) => amount;
        public bool AllowsPlaying(ICardDefinition card, int stacks) => true;

        private HoneycombStatus() { }
    }
}
