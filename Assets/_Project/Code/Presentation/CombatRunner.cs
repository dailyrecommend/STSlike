using STSlike.Domain.Actors;
using STSlike.Domain.Combat;
using STSlike.Domain.Events;
using STSlike.Domain.Run;

namespace STSlike.Presentation
{
    /// <summary>
    /// Builds and starts individual combats on request. Implements the domain's
    /// <see cref="ICombatLauncher"/> so the run flow can drive fights without
    /// knowing anything about Unity.
    ///
    /// The player is NOT rebuilt per fight — it is the run's persistent instance,
    /// so health carries across the map. Only the enemy and the combat itself
    /// are new each time.
    /// </summary>
    public sealed class CombatRunner : ICombatLauncher
    {
        public readonly struct Config
        {
            public readonly int CardsPerTurn;
            public readonly int EnemyHp;
            public readonly int EnemyAttack;
            public readonly int EnemyHpPerFloor;
            public readonly int EnemyAttackPerFloor;
            public readonly float EliteMultiplier;
            public readonly float BossMultiplier;

            public Config(int cardsPerTurn, int enemyHp, int enemyAttack,
                int enemyHpPerFloor, int enemyAttackPerFloor,
                float eliteMultiplier, float bossMultiplier)
            {
                CardsPerTurn = cardsPerTurn;
                EnemyHp = enemyHp;
                EnemyAttack = enemyAttack;
                EnemyHpPerFloor = enemyHpPerFloor;
                EnemyAttackPerFloor = enemyAttackPerFloor;
                EliteMultiplier = eliteMultiplier;
                BossMultiplier = bossMultiplier;
            }
        }

        private readonly IEventBus _bus;
        private readonly Config _config;
        private readonly RunContext _run;

        public CombatManager Current { get; private set; }

        public CombatRunner(IEventBus bus, Config config, RunContext run)
        {
            _bus = bus;
            _config = config;
            _run = run;
        }

        public void StartCombat(CombatRequest request)
        {
            int step = request.Floor - 1;
            float multiplier = request.IsBoss
                ? _config.BossMultiplier
                : request.IsElite ? _config.EliteMultiplier : 1f;

            int hp = (int)((_config.EnemyHp + step * _config.EnemyHpPerFloor) * multiplier);
            int attack = (int)((_config.EnemyAttack + step * _config.EnemyAttackPerFloor) * multiplier);

            var enemy = new EnemyState(NameFor(request), hp, attack);

            // Same player, fresh enemy. The deck is re-prepared (and reshuffled)
            // from the run's current cards, so rewards taken earlier are in play.
            var context = new CombatContext(_run.Player, enemy);
            Current = new CombatManager(context, _bus, _config.CardsPerTurn);
            Current.StartCombat(_run.Cards);
        }

        private static string NameFor(CombatRequest request)
        {
            if (request.IsBoss)
                return "Boss";
            return request.IsElite ? $"Elite {request.Floor}" : $"Enemy {request.Floor}";
        }
    }
}
