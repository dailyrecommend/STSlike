using STSlike.Domain.Cards;
using STSlike.Domain.Statuses;
using STSlike.Domain.Combat;

namespace STSlike.Domain.Characters.Albino
{
    /// <summary>
    /// Focus: everything goes into the weapon, and nothing is left over.
    ///
    /// Rounds hit far harder — and because the bonus from a wound is folded into
    /// the round before this multiplier runs, the whole Honeycomb chain is
    /// amplified with it. That is where the character's ceiling comes from.
    ///
    /// The price is total commitment. Nothing but gunfire can be played, so
    /// there is no guarding and no reloading while held, and a single blow that
    /// gets through breaks it outright. Standing in it is a bet that the target
    /// dies first.
    ///
    /// Persistent, so it stays until dropped rather than counting down. Stacks
    /// are meaningless here — it is on or it is not.
    /// </summary>
    public sealed class FocusStatus : IStatusEffect
    {
        public const string StatusId = "Focus";

        /// <summary>Damage multiplier on weapon fire, as a percentage.</summary>
        public const int DamagePercent = 200;

        public static readonly FocusStatus Instance = new FocusStatus();

        public string Id => StatusId;

        public StatusDuration Duration => StatusDuration.Persistent;

        public bool IsStance => true;

        /// <summary>Concentration does not survive being hit.</summary>
        public bool SurvivesBeingHurt => false;

        /// <summary>
        /// The one condition that deliberately reaches into weapon fire. Every
        /// other one leaves it alone precisely so that this can mean something.
        /// </summary>
        public int ModifyOutgoingDamage(int amount, int stacks, string tag)
        {
            if (stacks <= 0 || amount <= 0 || tag != DamageTags.Shot)
                return amount;

            return amount * DamagePercent / 100;
        }

        public int ModifyIncomingDamage(int amount, int stacks, string tag) => amount;

        /// <summary>
        /// Gunfire, and the card that drops the stance. Nothing else — that gap
        /// is the whole cost, and letting a guard slip through would remove it.
        /// </summary>
        public bool AllowsPlaying(ICardDefinition card, int stacks)
        {
            foreach (string tag in card.Tags)
            {
                if (tag == CardTags.Shot || tag == CardTags.Stance)
                    return true;
            }
            return false;
        }

        private FocusStatus() { }
    }
}
