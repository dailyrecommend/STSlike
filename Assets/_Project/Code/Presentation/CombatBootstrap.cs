using System.Collections.Generic;
using UnityEngine;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Combat;
using STSlike.Domain.Effects;
using STSlike.Domain.Randomness;

namespace STSlike.Presentation
{
    /// <summary>
    /// Stage 1 composition root: builds the domain objects by hand and starts
    /// a combat. This manual wiring is exactly what a DI container (VContainer)
    /// replaces in Stage 2, and the hard-coded starter deck will move to
    /// ScriptableObject data later.
    /// </summary>
    public sealed class CombatBootstrap : MonoBehaviour
    {
        [Header("Run")]
        [SerializeField] private int seed = 0;
        [SerializeField] private int cardsPerTurn = 5;

        [Header("Player")]
        [SerializeField] private int playerHp = 70;
        [SerializeField] private int maxEnergy = 3;

        [Header("Enemy")]
        [SerializeField] private int enemyHp = 45;
        [SerializeField] private int enemyAttack = 12;

        public CombatManager Manager { get; private set; }

        private void Awake() => StartNewCombat();

        public void StartNewCombat()
        {
            var deck = new Deck(new SystemRng(seed));
            var player = new PlayerState("Player", playerHp, maxEnergy, deck);
            var enemy = new EnemyState("Enemy", enemyHp, enemyAttack);
            var context = new CombatContext(player, enemy);

            Manager = new CombatManager(context, cardsPerTurn);
            Manager.StartCombat(BuildStarterDeck());
        }

        // Definitions are shared across card copies (flyweight); only the Card
        // instances differ. This is what ScriptableObjects will provide later.
        private static List<Card> BuildStarterDeck()
        {
            var strike = new CardDefinition("strike", 1,
                new List<ICardEffect> { new DealDamageEffect(6) });
            var defend = new CardDefinition("defend", 1,
                new List<ICardEffect> { new GainBlockEffect(5) });

            var cards = new List<Card>();
            for (int i = 0; i < 5; i++) cards.Add(new Card(strike));
            for (int i = 0; i < 5; i++) cards.Add(new Card(defend));
            return cards;
        }
    }
}
