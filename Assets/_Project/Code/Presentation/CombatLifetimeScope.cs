using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using STSlike.Data;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Characters;
using STSlike.Domain.Content;
using STSlike.Domain.Events;
using STSlike.Domain.Map;
using STSlike.Domain.Progression;
using STSlike.Domain.Randomness;
using STSlike.Domain.Run;
using STSlike.Domain.Run.Rewards;
using STSlike.Infrastructure.Persistence;
using STSlike.Presentation.UI;
using STSlike.Presentation.View;
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
        [Header("Content")]
        [Tooltip("Everything the game can make. One reference instead of a list " +
                 "per kind: adding a card is then a matter of creating it and " +
                 "rescanning, not of remembering to drag it in here.")]
        [SerializeField] private ContentCatalog catalog;

        [Tooltip("Which character this run plays, by id. A name rather than a " +
                 "reference, so a save can record it and content the scene has " +
                 "never heard of can supply it.")]
        [SerializeField] private string characterId = "albino";

        [Header("Run")]
        [SerializeField] private int seed = 0;
        [SerializeField] private int cardsPerTurn = 5;

        [Header("Map")]
        [SerializeField] private int mapFloors = 12;
        [SerializeField] private int mapColumns = 6;
        [SerializeField] private int mapPaths = 5;
        [SerializeField] private int firstEliteFloor = 4;
        [SerializeField] private int treasureFloor = 5;

        [Header("Fallback Enemy (used when the catalogue holds none)")]
        [SerializeField] private int enemyHp = 45;
        [SerializeField] private int enemyAttack = 12;

        [Header("Rewards")]
        [SerializeField] private int rewardCardChoices = 3;
        [SerializeField] private int rewardHealAmount = 8;

        // Resolved once in Configure and reused by the registrations below.
        private IContentRegistry _content;
        private ICharacterDefinition _character;

        protected override void Configure(IContainerBuilder builder)
        {
            // Content is resolved before anything else: the run, the fights and
            // the rewards are all built out of it.
            _content = BuildRegistry();
            _character = ResolveCharacter();
            builder.RegisterInstance(_content);

            builder.Register<IEventBus, EventBus>(Lifetime.Singleton);
            builder.Register<CombatSequencer>(Lifetime.Singleton);

            // One RNG for the whole run: shuffles and reward offers draw from the
            // same seeded stream, so a seed reproduces the entire run.
            builder.Register<IRng>(_ => new SystemRng(seed), Lifetime.Singleton);

            // The player and the deck live for the whole run, so health and cards
            // carry across encounters.
            builder.Register(resolver =>
                {
                    IRng rng = resolver.Resolve<IRng>();

                    // The map is drawn from the same seeded stream, so a seed
                    // reproduces the route as well as the shuffles.
                    RunMap map = new MapGenerator().Generate(rng, new MapGenerationSettings
                    {
                        RowCount = mapFloors,
                        Columns = mapColumns,
                        PathCount = mapPaths,
                        FirstEliteRow = firstEliteFloor,
                        TreasureRow = treasureFloor,
                    });

                    var deck = new Deck(rng);
                    var player = new PlayerState(ResolvedName, ResolvedMaxHp, ResolvedMaxEnergy,
                        deck, BuildWeapon());
                    return new RunContext(map, player, BuildStarterDeck(),
                        seed, ResolvedStartingCredits, ResolvedCharacterId);
                },
                Lifetime.Singleton);

            // Art for the HUD. Always registered — with empty sprites when the
            // character did not come from an asset — so binders never fail to
            // resolve.
            var art = _character as CharacterData;
            builder.RegisterInstance(new CharacterPresentation(
                ResolvedCharacterId,
                art != null ? art.Icon : null,
                art != null ? art.EnergyIcon : null,
                art != null ? art.ModelPrefab : null));

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
                    new CombatRunner.Config(cardsPerTurn, enemyHp, enemyAttack),
                    resolver.Resolve<RunContext>(),
                    resolver.Resolve<IRng>(),
                    _content),
                Lifetime.Singleton);

            // The flow depends on the launcher abstraction, not on CombatRunner.
            builder.Register<ICombatLauncher>(resolver => resolver.Resolve<CombatRunner>(), Lifetime.Singleton);
            builder.Register<RunFlow>(Lifetime.Singleton);

            builder.RegisterEntryPoint<CombatBootEntryPoint>();

            // The one seam the visuals talk to.
            builder.Register<CombatPresenter>(Lifetime.Singleton);
            builder.Register<CardHoverChannel>(Lifetime.Singleton);

            // Which card is picked up but not yet thrown. Shared by the hand,
            // which raises it, and the stage, which offers targets for it.
            builder.Register<CardSelection>(Lifetime.Singleton);

            // Which figure the pointer is on. Drives the name that appears over
            // it, and the target highlight while a card is being aimed.
            builder.Register<ActorHoverChannel>(Lifetime.Singleton);

            // Keeps the HUD from running ahead of the animation: the stage parks
            // each blow here and releases it as it plays.
            builder.Register<CombatDisplayState>(Lifetime.Singleton);

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
            RegisterIfPresent<MapViewBinder>(builder);
            RegisterIfPresent<CylinderBinder>(builder);

            if (FindFirstObjectByType<CombatStageBinder>(FindObjectsInactive.Include) != null)
            {
                // Also as IActorStage: the panels ask it where each figure is,
                // and the picker asks it who a clicked collider belongs to.
                builder.RegisterComponentInHierarchy<CombatStageBinder>()
                    .AsSelf()
                    .As<IActorStage>();

                // Only wired up alongside the stage — neither has anything to
                // point at without models.
                RegisterIfPresent<ActorOverlayBinder>(builder);
                RegisterIfPresent<DamageNumberBinder>(builder);
                RegisterIfPresent<ActorTargetingInput>(builder);
            }
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

        /// <summary>
        /// Every source the game should load, in the order they take effect. One
        /// today; a mod source appended here later would override rather than
        /// merely add, which is what patching content means.
        /// </summary>
        private IContentRegistry BuildRegistry()
        {
            var sources = new List<IContentSource>();
            if (catalog != null)
                sources.Add(catalog);
            else
                Debug.LogError("[CombatLifetimeScope] No content catalogue assigned — " +
                               "there is nothing to build a run out of.", this);

            var registry = new ContentRegistry(sources);

            // Two assets claiming one id is an authoring mistake while there is
            // only one source, and silence is what makes it hard to find.
            foreach (string clash in registry.Overrides)
                Debug.LogWarning($"[Content] {clash}", this);

            return registry;
        }

        /// <summary>
        /// The character this run plays. Falls back to whoever the catalogue
        /// holds first rather than to invented numbers: a run with the wrong
        /// character is recoverable, a run with a made-up one is confusing.
        /// </summary>
        private ICharacterDefinition ResolveCharacter()
        {
            ICharacterDefinition found = _content.Character(characterId);
            if (found != null)
                return found;

            if (_content.Characters.Count > 0)
            {
                Debug.LogError($"[CombatLifetimeScope] No character with id '{characterId}'. " +
                               $"Using '{_content.Characters[0].Id}' instead.", this);
                return _content.Characters[0];
            }

            Debug.LogError("[CombatLifetimeScope] The catalogue holds no characters.", this);
            return null;
        }

        private string ResolvedCharacterId => _character != null ? _character.Id : characterId;
        private int ResolvedMaxHp => _character != null ? _character.MaxHp : 70;
        private int ResolvedMaxEnergy => _character != null ? _character.MaxEnergy : 3;
        private int ResolvedStartingCredits => _character != null ? _character.StartingCredits : 0;
        /// <summary>
        /// The weapon this character carries, or null when they carry none —
        /// which is what a capacity of zero on the definition means.
        /// </summary>
        private WeaponState BuildWeapon()
        {
            if (_character == null || _character.MaxAmmo <= 0)
                return null;

            return new WeaponState(_character.MaxAmmo, _character.ShotDamage);
        }

        private string ResolvedName =>
            _character is CharacterData data ? data.DisplayName : ResolvedCharacterId;

        /// <summary>
        /// Everything that may be won. Which cards those are is authored on the
        /// cards themselves, so adding one never means remembering to add it to
        /// a second list somewhere.
        /// </summary>
        private List<ICardDefinition> BuildRewardPool()
        {
            var pool = new List<ICardDefinition>();
            foreach (ICardDefinition card in _content.Cards)
            {
                if (card.Collectable)
                    pool.Add(card);
            }
            return pool;
        }

        private List<Card> BuildStarterDeck()
        {
            var cards = new List<Card>();
            if (_character == null)
                return cards;

            foreach (ICardDefinition definition in _character.StartingDeck)
                cards.Add(new Card(definition));

            return cards;
        }
    }
}
