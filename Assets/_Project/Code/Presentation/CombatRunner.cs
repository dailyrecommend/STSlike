using System;
using System.Collections.Generic;
using STSlike.Data;
using STSlike.Domain.Actors;
using STSlike.Domain.Combat;
using STSlike.Domain.Content;
using STSlike.Domain.Events;
using STSlike.Domain.Randomness;
using STSlike.Domain.Run;

namespace STSlike.Presentation
{
    /// <summary>
    /// Builds and starts individual combats on request. Implements the domain's
    /// <see cref="ICombatLauncher"/> so the run flow can drive fights without
    /// knowing anything about Unity.
    ///
    /// The player is NOT rebuilt per fight — it is the run's persistent instance,
    /// so health carries across the map. Only the opposition is new each time.
    /// </summary>
    public sealed class CombatRunner : ICombatLauncher
    {
        public readonly struct Config
        {
            public readonly int CardsPerTurn;

            /// <summary>Used only when no enemy assets are assigned yet.</summary>
            public readonly int FallbackEnemyHp;
            public readonly int FallbackEnemyAttack;

            public Config(int cardsPerTurn, int fallbackEnemyHp, int fallbackEnemyAttack)
            {
                CardsPerTurn = cardsPerTurn;
                FallbackEnemyHp = fallbackEnemyHp;
                FallbackEnemyAttack = fallbackEnemyAttack;
            }
        }

        private readonly IEventBus _bus;
        private readonly Config _config;
        private readonly RunContext _run;
        private readonly IRng _rng;
        private readonly IContentRegistry _content;

        public CombatManager Current { get; private set; }

        /// <summary>
        /// The definitions behind the current fight, for spawning models and
        /// reading motions. Definitions rather than assets: content that did not
        /// come from a ScriptableObject still fights, it just has no art, and the
        /// view is the right place to notice that.
        /// </summary>
        public IReadOnlyList<IEnemyDefinition> CurrentEnemies { get; private set; }
            = new List<IEnemyDefinition>();

        public CombatRunner(IEventBus bus, Config config, RunContext run, IRng rng,
            IContentRegistry content)
        {
            _bus = bus;
            _config = config;
            _run = run;
            _rng = rng;
            _content = content;
        }

        public void StartCombat(CombatRequest request)
        {
            List<IEnemyDefinition> chosen = PickEnemies(request);
            var enemies = new List<EnemyState>(chosen.Count);

            foreach (IEnemyDefinition definition in chosen)
                enemies.Add(Build(definition));

            if (enemies.Count == 0)
                enemies.Add(BuildFallback(request));

            CurrentEnemies = chosen;

            var context = new CombatContext(_run.Player, enemies);
            Current = new CombatManager(context, _bus, _config.CardsPerTurn, _rng);
            Current.StartCombat(_run.Cards);
        }

        /// <summary>
        /// Picks the opposition for this node — a whole authored room where one
        /// fits, and otherwise a single enemy of the right tier.
        ///
        /// Which tier suits which node is a rule, so the filtering lives here
        /// rather than in the registry: the registry only knows what exists.
        /// </summary>
        private List<IEnemyDefinition> PickEnemies(CombatRequest request)
        {
            EnemyTier wanted = request.IsBoss ? EnemyTier.Boss
                : request.IsElite ? EnemyTier.Elite
                : EnemyTier.Normal;

            List<IEnemyDefinition> room = PickRoom(wanted, request.Floor);
            return room.Count > 0 ? room : PickLoneEnemy(wanted);
        }

        private List<IEnemyDefinition> PickRoom(EnemyTier wanted, int floor)
        {
            var candidates = new List<IEncounterDefinition>();

            IReadOnlyList<IEncounterDefinition> all = _content != null
                ? _content.Encounters
                : Array.Empty<IEncounterDefinition>();

            foreach (IEncounterDefinition room in all)
            {
                if (room != null && room.Tier == wanted && Suits(room, floor))
                    candidates.Add(room);
            }

            var enemies = new List<IEnemyDefinition>();
            if (candidates.Count == 0)
                return enemies;

            foreach (string id in candidates[_rng.Next(candidates.Count)].EnemyIds)
            {
                // A room naming an enemy that is not installed loses that enemy
                // rather than the whole fight.
                IEnemyDefinition enemy = _content.Enemy(id);
                if (enemy != null)
                    enemies.Add(enemy);
            }

            return enemies;
        }

        // A maximum of zero means the room never stops being appropriate.
        private static bool Suits(IEncounterDefinition room, int floor) =>
            floor >= room.MinFloor && (room.MaxFloor <= 0 || floor <= room.MaxFloor);

        /// <summary>
        /// What a fight looks like before any rooms have been authored: one
        /// enemy of roughly the right sort. Keeps a project with enemies but no
        /// encounters playable.
        /// </summary>
        private List<IEnemyDefinition> PickLoneEnemy(EnemyTier wanted)
        {
            IReadOnlyList<IEnemyDefinition> all = _content != null
                ? _content.Enemies
                : Array.Empty<IEnemyDefinition>();

            var candidates = new List<IEnemyDefinition>();
            foreach (IEnemyDefinition definition in all)
            {
                if (definition != null && definition.Tier == wanted)
                    candidates.Add(definition);
            }

            // Rather than show an empty room, take whatever exists.
            if (candidates.Count == 0)
            {
                foreach (IEnemyDefinition definition in all)
                {
                    if (definition != null)
                        candidates.Add(definition);
                }
            }

            var picked = new List<IEnemyDefinition>();
            if (candidates.Count > 0)
                picked.Add(candidates[_rng.Next(candidates.Count)]);

            return picked;
        }

        // The definition is taken at face value: no scaling is applied on top, so
        // what is authored is exactly what the player faces. Difficulty is a
        // matter of which enemies a floor may draw.
        //
        // The name is presentation and so is not on the contract; an asset can
        // offer a nicer one, and anything else is known by its id.
        private static EnemyState Build(IEnemyDefinition definition)
        {
            string name = definition is EnemyData data ? data.DisplayName : definition.Id;

            return new EnemyState(name, definition.MaxHp,
                new ScriptedEnemyBehavior(definition.Pattern), definition.Id);
        }

        private EnemyState BuildFallback(CombatRequest request)
        {
            return new EnemyState($"Enemy {request.Floor}",
                _config.FallbackEnemyHp, _config.FallbackEnemyAttack);
        }
    }
}
