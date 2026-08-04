using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using STSlike.Data;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Events;
using STSlike.Domain.Progression;
using STSlike.Domain.Randomness;
using STSlike.Domain.Run;
using STSlike.Domain.Run.Rewards;
using STSlike.Infrastructure.Persistence;
using STSlike.Presentation.UI;
#if STSLIKE_SQLITE
using STSlike.Infrastructure.Persistence.Sqlite;
#endif

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

        [Header("Character (leave empty to use the fallback numbers below)")]
        [SerializeField] private CharacterData character;

        [Header("Run")]
        [SerializeField] private int seed = 0;
        [SerializeField] private int totalEncounters = 3;
        [SerializeField] private int cardsPerTurn = 5;

        [Header("Fallback Player Settings (ignored when a Character is assigned)")]
        [SerializeField] private string characterId = "albino";
        [SerializeField] private int playerHp = 70;
        [SerializeField] private int maxEnergy = 3;
        [SerializeField] private int startingCredits = 100;

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
                    var player = new PlayerState("Player", ResolvedMaxHp, ResolvedMaxEnergy, deck);
                    return new RunContext(totalEncounters, player, BuildStarterDeck(),
                        seed, ResolvedStartingCredits, ResolvedCharacterId);
                },
                Lifetime.Singleton);

            // Art for the HUD. Always registered — with empty sprites when no
            // character asset is assigned yet — so binders never fail to resolve.
            builder.RegisterInstance(new CharacterPresentation(
                ResolvedCharacterId,
                character != null ? character.Icon : null,
                character != null ? character.EnergyIcon : null));

            // Meta progression. The repositories are interfaces, so the storage
            // swap lives here and nowhere else — no game code knows which one
            // it is talking to. Define STSLIKE_SQLITE once sqlite-net-pcl is
            // installed; without it the game still runs, just without a file.
#if STSLIKE_SQLITE
            builder.Register(_ => SqliteDatabase.OpenDefault(), Lifetime.Singleton);
            builder.Register<IRunHistoryRepository, SqliteRunHistoryRepository>(Lifetime.Singleton);
            builder.Register<ICardStatsRepository, SqliteCardStatsRepository>(Lifetime.Singleton);
            builder.Register<IRunSaveRepository, SqliteRunSaveRepository>(Lifetime.Singleton);
#else
            builder.Register<IRunHistoryRepository, InMemoryRunHistoryRepository>(Lifetime.Singleton);
            builder.Register<ICardStatsRepository, InMemoryCardStatsRepository>(Lifetime.Singleton);
            builder.Register<IRunSaveRepository, InMemoryRunSaveRepository>(Lifetime.Singleton);
#endif
            builder.Register(resolver => new ProgressionRecorder(
                    resolver.Resolve<IEventBus>(),
                    resolver.Resolve<RunContext>(),
                    resolver.Resolve<IRunHistoryRepository>(),
                    resolver.Resolve<ICardStatsRepository>(),
                    characterId),
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

            // The one seam the visuals talk to.
            builder.Register<CombatPresenter>(Lifetime.Singleton);
            builder.Register<CardHoverChannel>(Lifetime.Singleton);

            // Scene components. RegisterComponentInHierarchy throws when the
            // component is absent, so each is registered only if it is actually
            // placed — that way the UI can be built up one binder at a time.
            RegisterIfPresent<CombatDebugView>(builder);
            RegisterIfPresent<CombatHudBinder>(builder);
            RegisterIfPresent<HandViewBinder>(builder);
            RegisterIfPresent<RewardViewBinder>(builder);
            RegisterIfPresent<CardTooltipBinder>(builder);
            RegisterIfPresent<HandCardLayout>(builder);
            RegisterIfPresent<CardListPopupBinder>(builder);
        }

        /// <summary>
        /// Registers a scene component only when it exists. Inactive objects are
        /// included on purpose: panels such as the reward screen normally start
        /// switched off, and they still need to be injected.
        /// </summary>
        private static void RegisterIfPresent<T>(IContainerBuilder builder) where T : MonoBehaviour
        {
            if (FindFirstObjectByType<T>(FindObjectsInactive.Include) != null)
                builder.RegisterComponentInHierarchy<T>();
        }

        // A character asset wins when assigned; the loose fields stay so the
        // scene still runs before one has been authored.
        private string ResolvedCharacterId => character != null ? character.Id : characterId;
        private int ResolvedMaxHp => character != null ? character.MaxHp : playerHp;
        private int ResolvedMaxEnergy => character != null ? character.MaxEnergy : maxEnergy;
        private int ResolvedStartingCredits => character != null ? character.StartingCredits : startingCredits;

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

            if (character != null)
            {
                foreach (ICardDefinition definition in character.StartingDeck)
                    cards.Add(new Card(definition));
                return cards;
            }

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
