using UnityEngine;
using STSlike.Domain.Relics;

namespace STSlike.Data
{
    /// <summary>
    /// Authoring asset for a relic: which behaviour it is, how it is tuned, and
    /// how it presents itself.
    ///
    /// The same shape as a card — behaviour picked from a dropdown, numbers
    /// tuned on the asset, art and wording alongside. Adding a relic is a class
    /// and an asset; rebalancing one is just the asset.
    /// </summary>
    [CreateAssetMenu(fileName = "Relic_New", menuName = "STSlike/Relic", order = 4)]
    public sealed class RelicData : ScriptableObject, IRelicDefinition
    {
        [Tooltip("What it does. Pick the behaviour, then tune its numbers here " +
                 "rather than in code.")]
        [SerializeReference, SerializeReferenceDropdown]
        private Relic behaviour;

        [Header("Rules")]
        [SerializeField] private RelicRarity rarity = RelicRarity.Common;

        [Tooltip("Can be found during a run. Turn this off for one handed out " +
                 "by a starting kit, or one still being worked on.")]
        [SerializeField] private bool obtainable = true;

        [Tooltip("Leave empty for a relic anyone may find. A character id keeps " +
                 "it to that character — a revolver's relic means nothing to " +
                 "somebody carrying a sword.")]
        [SerializeField] private string characterId;

        [Header("Presentation")]
        [SerializeField] private string displayName;

        [SerializeField, TextArea(2, 4)] private string description;
        [SerializeField] private Sprite icon;

        /// <summary>
        /// Taken from the behaviour rather than typed here, so the asset and the
        /// class can never disagree about which relic this is.
        /// </summary>
        public string Id => behaviour != null ? behaviour.Id : null;

        public RelicRarity Rarity => rarity;
        public bool Obtainable => obtainable;
        public string CharacterId => characterId;

        /// <summary>Falls back to the id so an unnamed relic still reads.</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? Id : displayName;

        public string Description => description;
        public Sprite Icon => icon;

        /// <summary>
        /// A fresh relic carrying this asset's tuning.
        ///
        /// Copied rather than handed out, because a relic keeps its own state:
        /// the run that counts to ten must not be counting on the asset, or the
        /// next run would start halfway there. The round trip through JSON is
        /// the shortest deep copy Unity offers for a [SerializeReference] object,
        /// and it carries the tuned fields across while leaving the run's own
        /// progress — which is deliberately not serialized — at its default.
        /// </summary>
        public Relic Create()
        {
            if (behaviour == null)
                return null;

            string tuning = JsonUtility.ToJson(behaviour);
            return JsonUtility.FromJson(tuning, behaviour.GetType()) as Relic;
        }
    }
}
