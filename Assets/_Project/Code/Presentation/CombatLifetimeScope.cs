using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using STSlike.Data;
using STSlike.Domain.Cards;
using STSlike.Domain.Events;

namespace STSlike.Presentation
{
    /// <summary>
    /// Composition root for combat. Instead of newing everything by hand, it
    /// registers services with the container: the shared event bus, the combat
    /// runner (with its config + starter deck), an entry point that starts the
    /// fight, and the scene view that should receive injections.
    /// </summary>
    public class CombatLifetimeScope : LifetimeScope
    {
        [Serializable]
        private struct DeckEntry
        {
            public CardData card;
            public int count;
        }

        [Header("Run")]
        [SerializeField] private int seed = 0;
        [SerializeField] private int cardsPerTurn = 5;

        [Header("Player")]
        [SerializeField] private int playerHp = 70;
        [SerializeField] private int maxEnergy = 3;

        [Header("Enemy")]
        [SerializeField] private int enemyHp = 45;
        [SerializeField] private int enemyAttack = 12;

        [Header("Starter Deck (assign CardData assets + counts)")]
        [SerializeField] private List<DeckEntry> starterDeck = new List<DeckEntry>();

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<IEventBus, EventBus>(Lifetime.Singleton);

            builder.Register(resolver => new CombatRunner(
                    resolver.Resolve<IEventBus>(),
                    new CombatRunner.Config(seed, cardsPerTurn, playerHp, maxEnergy, enemyHp, enemyAttack),
                    BuildStarterDeck()),
                Lifetime.Singleton);

            builder.RegisterEntryPoint<CombatBootEntryPoint>();

            builder.RegisterComponentInHierarchy<CombatDebugView>();
        }

        private List<Card> BuildStarterDeck()
        {
            var cards = new List<Card>();
            foreach (DeckEntry entry in starterDeck)
            {
                if (entry.card == null)
                    continue;
                for (int i = 0; i < entry.count; i++)
                    cards.Add(new Card(entry.card));
            }
            return cards;
        }
    }
}
