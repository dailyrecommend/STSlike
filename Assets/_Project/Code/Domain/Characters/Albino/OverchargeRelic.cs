using STSlike.Domain.Cards;
using STSlike.Domain.Relics;

namespace STSlike.Domain.Characters.Albino
{
    /// <summary>
    /// Overcharge: every tenth attack card leaves the next one doubled.
    ///
    /// The relic that needs its own memory, and the reason relics are objects
    /// rather than shared definitions — it has to count, and the count belongs
    /// to this run.
    ///
    /// Counting cards rather than blows is deliberate: a six-round burst is one
    /// card, so the payoff cannot be farmed by firing more. And the doubling
    /// applies to a whole card once armed, so a burst gets it on every round —
    /// which is what makes saving the charge for a full cylinder the right play.
    /// </summary>
    [System.Serializable]
    public sealed class OverchargeRelic : Relic
    {
        public const string RelicId = "Overcharge";

        /// <summary>Attack cards between payoffs. Tuned on the asset.</summary>
        public int threshold = 10;

        /// <summary>What the payoff multiplies by, as a percentage.</summary>
        public int payoffPercent = 200;

        // Not serialized, so a fresh relic starts fresh: this is the run's
        // progress, not something the content author decides.
        private int _played;
        private bool _armed;
        private bool _spending;

        public override string Id => RelicId;

        /// <summary>For a view that wants to show how close it is.</summary>
        public int Progress => _played;
        public bool IsArmed => _armed;

        public override void OnCardPlayed(Card card, RelicContext context)
        {
            if (card.Definition.Type != CardType.Attack)
                return;

            // Armed already: this is the card that cashes it in, and it does not
            // also count towards the next one.
            if (_armed)
            {
                _armed = false;
                _spending = true;
                return;
            }

            if (++_played < threshold)
                return;

            _played = 0;
            _armed = true;
        }

        // Held for the whole card rather than the first blow, so a burst is
        // doubled end to end. Cleared once the card is done with.
        public override void OnCardResolved(Card card, RelicContext context) => _spending = false;

        public override int ModifyOutgoingDamage(int amount, string tag) =>
            _spending ? amount * payoffPercent / 100 : amount;

        /// <summary>
        /// A charge is not carried between fights. Letting it cross would mean
        /// walking into a boss with a free doubled cylinder, decided several
        /// rooms earlier.
        /// </summary>
        public override void OnCombatStarted(RelicContext context)
        {
            _armed = false;
            _spending = false;
        }
    }
}
