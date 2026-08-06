using System.Collections.Generic;
using STSlike.Data;
using STSlike.Domain.Actors;
using STSlike.Domain.Combat;
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
        private readonly IReadOnlyList<EnemyData> _pool;

        public CombatManager Current { get; private set; }

        /// <summary>The assets behind the current fight, for spawning models.</summary>
        public IReadOnlyList<EnemyData> CurrentEnemyData { get; private set; } = new List<EnemyData>();

        public CombatRunner(IEventBus bus, Config config, RunContext run, IRng rng,
            IReadOnlyList<EnemyData> pool)
        {
            _bus = bus;
            _config = config;
            _run = run;
            _rng = rng;
            _pool = pool ?? new List<EnemyData>();
        }

        public void StartCombat(CombatRequest request)
        {
            List<EnemyData> chosen = PickEnemies(request);
            var enemies = new List<EnemyState>(chosen.Count);

            foreach (EnemyData data in chosen)
                enemies.Add(Build(data));

            if (enemies.Count == 0)
                enemies.Add(BuildFallback(request));

            CurrentEnemyData = chosen;

            var context = new CombatContext(_run.Player, enemies);
            Current = new CombatManager(context, _bus, _config.CardsPerTurn);
            Current.StartCombat(_run.Cards);
        }

        /// <summary>
        /// Picks the opposition for this node. One enemy for now; the shape is
        /// a list so an encounter table can hand back a whole room later without
        /// anything downstream changing.
        /// </summary>
        private List<EnemyData> PickEnemies(CombatRequest request)
        {
            EnemyTier wanted = request.IsBoss ? EnemyTier.Boss
                : request.IsElite ? EnemyTier.Elite
                : EnemyTier.Normal;

            var candidates = new List<EnemyData>();
            foreach (EnemyData data in _pool)
            {
                if (data != null && data.Tier == wanted)
                    candidates.Add(data);
            }

            // Fall back to whatever exists rather than showing an empty room.
            if (candidates.Count == 0)
            {
                foreach (EnemyData data in _pool)
                {
                    if (data != null)
                        candidates.Add(data);
                }
            }

            var picked = new List<EnemyData>();
            if (candidates.Count > 0)
                picked.Add(candidates[_rng.Next(candidates.Count)]);

            return picked;
        }

        // The asset is taken at face value: no scaling is applied on top, so what
        // is authored is exactly what the player faces. Difficulty is a matter of
        // which enemies a floor may draw.
        private static EnemyState Build(EnemyData data)
        {
            return new EnemyState(data.DisplayName, data.MaxHp,
                new ScriptedEnemyBehavior(data.Pattern), data.Id);
        }

        private EnemyState BuildFallback(CombatRequest request)
        {
            return new EnemyState($"Enemy {request.Floor}",
                _config.FallbackEnemyHp, _config.FallbackEnemyAttack);
        }
    }
}
