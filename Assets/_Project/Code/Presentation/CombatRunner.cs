using System.Collections.Generic;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Combat;
using STSlike.Domain.Events;
using STSlike.Domain.Randomness;

namespace STSlike.Presentation
{
    /// <summary>
    /// Builds and starts a combat from configuration, and can restart it. Its
    /// only injected dependency is the shared <see cref="IEventBus"/>; the rest
    /// is per-combat state it constructs. This is the wiring that used to live
    /// in CombatBootstrap, now a plain service the DI container owns.
    /// </summary>
    public sealed class CombatRunner
    {
        public readonly struct Config
        {
            public readonly int Seed;
            public readonly int CardsPerTurn;
            public readonly int PlayerHp;
            public readonly int MaxEnergy;
            public readonly int EnemyHp;
            public readonly int EnemyAttack;

            public Config(int seed, int cardsPerTurn, int playerHp, int maxEnergy, int enemyHp, int enemyAttack)
            {
                Seed = seed;
                CardsPerTurn = cardsPerTurn;
                PlayerHp = playerHp;
                MaxEnergy = maxEnergy;
                EnemyHp = enemyHp;
                EnemyAttack = enemyAttack;
            }
        }

        private readonly IEventBus _bus;
        private readonly Config _config;
        private readonly List<Card> _starterDeck;

        public CombatManager Current { get; private set; }

        public CombatRunner(IEventBus bus, Config config, List<Card> starterDeck)
        {
            _bus = bus;
            _config = config;
            _starterDeck = starterDeck;
        }

        public void StartNew()
        {
            var deck = new Deck(new SystemRng(_config.Seed));
            var player = new PlayerState("Player", _config.PlayerHp, _config.MaxEnergy, deck);
            var enemy = new EnemyState("Enemy", _config.EnemyHp, _config.EnemyAttack);
            var context = new CombatContext(player, enemy);

            Current = new CombatManager(context, _bus, _config.CardsPerTurn);
            Current.StartCombat(_starterDeck);
        }
    }
}
