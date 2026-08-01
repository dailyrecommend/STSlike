using System;
using System.Collections.Generic;
using UnityEngine;
using STSlike.Domain.Cards;
using STSlike.Domain.Characters;

namespace STSlike.Data
{
    /// <summary>
    /// Authoring asset for a playable character. Implements the domain's
    /// <see cref="ICharacterDefinition"/>, so the rules consume it without
    /// knowing it is a Unity asset, while the UI reads the icons directly.
    ///
    /// Adding a character is now data: create one of these, fill in the deck,
    /// and assign it in the scene. No code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "Character_New", menuName = "STSlike/Character", order = 1)]
    public sealed class CharacterData : ScriptableObject, ICharacterDefinition
    {
        [Serializable]
        private struct DeckEntry
        {
            public CardData card;
            [Min(1)] public int count;
        }

        [Header("Identity")]
        [SerializeField] private string id = "albino";

        [Header("Presentation (read by the UI, not by combat)")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite energyIcon;

        [Header("Starting Resources")]
        [SerializeField, Min(1)] private int maxHp = 70;
        [SerializeField, Min(0)] private int maxEnergy = 3;
        [SerializeField, Min(0)] private int startingCredits = 100;

        [Header("Starting Deck")]
        [SerializeField] private List<DeckEntry> startingDeck = new List<DeckEntry>();

        public string Id => id;
        public int MaxHp => maxHp;
        public int MaxEnergy => maxEnergy;
        public int StartingCredits => startingCredits;

        /// <summary>Portrait shown in the top-left of the HUD.</summary>
        public Sprite Icon => icon;

        /// <summary>Energy orb art; each character has its own.</summary>
        public Sprite EnergyIcon => energyIcon;

        public IReadOnlyList<ICardDefinition> StartingDeck => BuildStartingDeck();

        // Expanded on read so the inspector can stay in the compact
        // (card, count) form that is actually pleasant to edit.
        private List<ICardDefinition> BuildStartingDeck()
        {
            var cards = new List<ICardDefinition>();
            foreach (DeckEntry entry in startingDeck)
            {
                if (entry.card == null)
                    continue;
                for (int i = 0; i < Mathf.Max(1, entry.count); i++)
                    cards.Add(entry.card);
            }
            return cards;
        }
    }
}
