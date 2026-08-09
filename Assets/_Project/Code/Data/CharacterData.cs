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
        [Tooltip("Key used by saves and statistics. Never shown, so it can stay " +
                 "stable while the name on screen changes.")]
        [SerializeField] private string id = "albino";

        [Header("Presentation (read by the UI, not by combat)")]
        [Tooltip("Name shown above the figure. Falls back to the id when empty.")]
        [SerializeField] private string displayName;

        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite energyIcon;

        [Tooltip("Spawned in the arena when a run begins.")]
        [SerializeField] private GameObject modelPrefab;

        [Header("Starting Resources")]
        [SerializeField, Min(1)] private int maxHp = 70;
        [SerializeField, Min(0)] private int maxEnergy = 3;
        [SerializeField, Min(0)] private int startingCredits = 100;

        [Header("Weapon (0 rounds = carries no such weapon)")]
        [Tooltip("Rounds the cylinder holds. Refilled when a fight begins, and " +
                 "never by the turn — reloading is a card.")]
        [SerializeField, Min(0)] private int maxAmmo = 6;

        [Tooltip("Base damage of one round. Fixed on purpose: the weapon's " +
                 "output does not care how its owner is feeling.")]
        [SerializeField, Min(0)] private int shotDamage = 6;

        [Header("Starting Deck")]
        [SerializeField] private List<DeckEntry> startingDeck = new List<DeckEntry>();

        public string Id => id;
        public int MaxHp => maxHp;
        public int MaxEnergy => maxEnergy;
        public int StartingCredits => startingCredits;
        public int MaxAmmo => maxAmmo;
        public int ShotDamage => shotDamage;

        /// <summary>Falls back to the id so an unnamed character still reads.</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? id : displayName;

        /// <summary>Portrait shown in the top-left of the HUD.</summary>
        public Sprite Icon => icon;

        /// <summary>Energy orb art; each character has its own.</summary>
        public Sprite EnergyIcon => energyIcon;

        /// <summary>The 3D figure that stands in the arena.</summary>
        public GameObject ModelPrefab => modelPrefab;

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
