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
        [SerializeField] private string id;
        [SerializeField, Min(0)] private int cost;
        [SerializeReference, SerializeReferenceDropdown]
        private List<ICardEffect> effects = new List<ICardEffect>();

        public string Id => id;
        public int Cost => cost;
        public IReadOnlyList<ICardEffect> Effects => effects;

        /// <summary>
        /// Programmatic factory for tests and editor tooling. Authored assets
        /// are normally created via the Create menu and filled in the inspector.
        /// </summary>
        public static CardData Create(string id, int cost, List<ICardEffect> effects)
        {
            var data = CreateInstance<CardData>();
            data.id = id;
            data.cost = cost;
            data.effects = effects ?? new List<ICardEffect>();
            return data;
        }
    }
}
