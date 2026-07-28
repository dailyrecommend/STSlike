using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using STSlike.Data;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Events;
using STSlike.Domain.Randomness;
using STSlike.Domain.Run;
using STSlike.Domain.Run.Rewards;

namespace STSlike.Presentation
{
    /// <summary>
    /// Composition root. Registers the shared services (event bus, sequencer),
    /// the combat launcher, the run flow that drives encounters, an entry point
    /// that starts the run, and the dev harness that should receive injections.
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
        [SerializeField] private int totalEncounters = 3;
        [SerializeField] private int cardsPerTurn = 5;

        [Header("Player")]
        [SerializeField] private int playerHp = 70;
        [SerializeField] private int maxEnergy = 3;

        [Header("Enemy")]
        [SerializeField] private int enemyHp = 45;
        [SerializeField] private int enemyAttack = 12;

        [Header("Per-Encounter Scaling")]
        [SerializeField] private int enemyHpPerEncounter = 15;
        [SerializeField] private int enemyAttackPerEncounter = 2;

        [Header("Starter Deck (assign CardData assets + counts)")]
        [SerializeField] private List<DeckEntry> starterDeck = new List<DeckEntry>();

        [Header("Rewards")]
        [SerializeField] private List<CardData> rewardCardPool = new List<CardData>();
        [SerializeField] private int rewardCardChoices = 3;
        [SerializeField] private int rewardHealAmount = 8;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<IEventBus, EventBus>(Lifetime.Singleton);
            builder.Register<CombatSequencer>(Lifetime.Singleton);

            // One RNG for the whole run: shuffles and reward offers draw from the
            // same seeded stream, so a seed reproduces the entire run.
            builder.Register<IRng>(_ => new SystemRng(seed), Lifetime.Singleton);

            // The player and the deck live for the whole run, so health and cards
            // carry across encounters.
            builder.Register(resolver =>
                {
                    var deck = new Deck(resolver.Resolve<IRng>());
                    var player = new PlayerState("Player", playerHp, maxEnergy, deck);
                    return new RunContext(totalEncounters, player, BuildStarterDeck());
                },
                Lifetime.Singleton);

            builder.Register<IRewardGenerator>(resolver => new RewardGenerator(
                    resolver.Resolve<IRng>(),
                    BuildRewardPool(),
                    rewardCardChoices,
                    rewardHealAmount),
                Lifetime.Singleton);

            builder.Register(resolver => new CombatRunner(
                    resolver.Resolve<IEventBus>(),
                    new CombatRunner.Config(cardsPerTurn, enemyHp, enemyAttack,
                        enemyHpPerEncounter, enemyAttackPerEncounter),
                    resolver.Resolve<RunContext>()),
                Lifetime.Singleton);

            // The flow depends on the launcher abstraction, not on CombatRunner.
            builder.Register<ICombatLauncher>(resolver => resolver.Resolve<CombatRunner>(), Lifetime.Singleton);
            builder.Register<RunFlow>(Lifetime.Singleton);

            builder.RegisterEntryPoint<CombatBootEntryPoint>();

            // Inject the harness that is already placed in the scene, so its
            // inspector values (font size, panel width, log pacing) take effect.
            builder.RegisterComponentInHierarchy<CombatDebugView>();
        }

        private List<ICardDefinition> BuildRewardPool()
        {
            var pool = new List<ICardDefinition>();
            foreach (CardData card in rewardCardPool)
            {
                if (card != null)
                    pool.Add(card);
            }
            return pool;
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
