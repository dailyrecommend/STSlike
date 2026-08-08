using System.Collections.Generic;
using UnityEngine;
using STSlike.Domain.Cards;
using STSlike.Domain.Effects;

namespace STSlike.Data
{
    /// <summary>
    /// Authoring asset for a card. It implements the domain's
    /// <see cref="ICardDefinition"/>, so combat consumes it without ever
    /// knowing it is a Unity asset. Effects are composed in the inspector via
    /// [SerializeReference], so adding a new card is pure data (no code).
    /// </summary>
    [CreateAssetMenu(fileName = "Card_New", menuName = "STSlike/Card", order = 0)]
    public sealed class CardData : ScriptableObject, ICardDefinition
    {
        [Header("Rules")]
        [SerializeField] private string id;
        [SerializeField, Min(0)] private int cost;
        [SerializeField] private CardType type = CardType.Attack;

        [Tooltip("Who this applies to. Only Single Enemy asks the player to pick " +
                 "one — a sweeping attack, a random one, and anything you do to " +
                 "yourself all resolve on the spot. Set this from what the card " +
                 "does, not from its type: a skill that lands a debuff needs a " +
                 "target just as much as a strike does.")]
        [SerializeField] private CardTarget target = CardTarget.SingleEnemy;

        [SerializeField] private CardRarity rarity = CardRarity.Common;

        [Tooltip("Can be offered as a reward. Turn this off for starter cards — " +
                 "they belong in the opening deck, not in the prize pool.")]
        [SerializeField] private bool collectable = true;

        [SerializeReference, SerializeReferenceDropdown]
        private List<ICardEffect> effects = new List<ICardEffect>();

        [Header("Presentation")]
        [SerializeField] private string displayName;

        [SerializeField, TextArea(2, 4)]
        [Tooltip("Use {0}, {1}... for the effect values, in the order the effects are listed. " +
                 "Example: \"Deal {0} damage.\" The numbers then follow the card automatically " +
                 "when it is rebalanced or upgraded.")]
        private string description;

        [SerializeField] private Sprite artwork;

        [Tooltip("Animator trigger the character plays when this card is used. " +
                 "Cards that share a motion can share a trigger.")]
        [SerializeField] private string animationTrigger = "Attack";

        [Tooltip("Seconds from the start of the motion until the blow lands. " +
                 "The damage is already decided by then — this only holds back " +
                 "the flinch and the health bar so they line up with the swing.")]
        [SerializeField, Min(0f)] private float impactDelay = 0.25f;

        [Tooltip("Seconds between blows when this card hits more than once. " +
                 "Match it to the rhythm of the animation's swings.")]
        [SerializeField, Min(0f)] private float hitInterval = 0.15f;

        public string Id => id;
        public int Cost => cost;
        public CardType Type => type;
        public CardTarget Target => target;
        public CardRarity Rarity => rarity;
        public bool Collectable => collectable;
        public IReadOnlyList<ICardEffect> Effects => effects;

        /// <summary>Falls back to the id so an unfinished card is still readable.</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? id : displayName;

        /// <summary>Raw template; the presenter fills in the numbers.</summary>
        public string DescriptionTemplate => description;

        public Sprite Artwork => artwork;

        /// <summary>Motion the character plays when this card resolves.</summary>
        public string AnimationTrigger => animationTrigger;

        /// <summary>Seconds into the motion at which the hit should read as landing.</summary>
        public float ImpactDelay => impactDelay;

        /// <summary>Gap between blows when this card hits more than once.</summary>
        public float HitInterval => hitInterval;

        /// <summary>
        /// Programmatic factory for tests and editor tooling. Authored assets
        /// are normally created via the Create menu and filled in the inspector.
        /// </summary>
        public static CardData Create(string id, int cost, List<ICardEffect> effects,
            CardType type = CardType.Attack, CardRarity rarity = CardRarity.Common,
            CardTarget target = CardTarget.SingleEnemy)
        {
            var data = CreateInstance<CardData>();
            data.id = id;
            data.cost = cost;
            data.type = type;
            data.target = target;
            data.rarity = rarity;
            data.effects = effects ?? new List<ICardEffect>();
            return data;
        }
    }
}
