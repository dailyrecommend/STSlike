using STSlike.Domain.Actors;
using STSlike.Domain.Combat;
using STSlike.Domain.Events;
using STSlike.Domain.Run;

namespace STSlike.Presentation
{
    /// <summary>
    /// Builds and starts individual combats on request. Implements the domain's
    /// <see cref="ICombatLauncher"/> so the run flow can drive encounters
    /// without knowing anything about Unity.
    ///
    /// The player is NOT rebuilt per encounter — it is the run's persistent
    /// instance, so health carries across fights. Only the enemy and the combat
    /// itself are new each time.
    /// </summary>
    public sealed class CombatRunner : ICombatLauncher
    {
        public readonly struct Config
        {
            public readonly int CardsPerTurn;
            public readonly int EnemyHp;
            public readonly int EnemyAttack;
            public readonly int EnemyHpPerEncounter;
            public readonly int EnemyAttackPerEncounter;

            public Config(int cardsPerTurn, int enemyHp, int enemyAttack,
                int enemyHpPerEncounter, int enemyAttackPerEncounter)
            {
                CardsPerTurn = cardsPerTurn;
                EnemyHp = enemyHp;
                EnemyAttack = enemyAttack;
                EnemyHpPerEncounter = enemyHpPerEncounter;
                EnemyAttackPerEncounter = enemyAttackPerEncounter;
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

        public void StartCombat(int encounterNumber)
        {
            int step = encounterNumber - 1;

            var enemy = new EnemyState(
                $"Enemy {encounterNumber}",
                _config.EnemyHp + step * _config.EnemyHpPerEncounter,
                _config.EnemyAttack + step * _config.EnemyAttackPerEncounter);

            // Same player, fresh enemy. The deck is re-prepared (and reshuffled)
            // from the run's current card collection, so cards gained as rewards
            // are included from the next encounter onward.
            var context = new CombatContext(_run.Player, enemy);
            Current = new CombatManager(context, _bus, _config.CardsPerTurn);
            Current.StartCombat(_run.Cards);
        }
    }
}
