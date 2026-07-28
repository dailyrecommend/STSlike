using System;
using System.Collections.Generic;
using System.Linq;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Commands;
using STSlike.Domain.Effects;
using STSlike.Domain.Events;

namespace STSlike.Domain.Combat
{
    /// <summary>
    /// Orchestrates the combat loop: begin turn -> play cards -> end turn ->
    /// enemy acts -> check win/lose. Holds no rendering. Instead of a single
    /// "something changed" signal, it publishes typed events on an injected
    /// <see cref="IEventBus"/>, which the UI and character mechanics subscribe to.
    /// </summary>
    public sealed class CombatManager
    {
        private readonly CombatContext _context;
        private readonly IEventBus _bus;
        private readonly int _cardsPerTurn;
        private readonly CommandInvoker _invoker = new CommandInvoker();

        public CombatContext Context => _context;

        /// <summary>Commands executed so far this combat (for replay/debugging).</summary>
        public IReadOnlyList<ICommand> CommandHistory => _invoker.History;

        public CombatManager(CombatContext context, IEventBus bus, int cardsPerTurn = 5)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            if (cardsPerTurn < 0)
                throw new ArgumentOutOfRangeException(nameof(cardsPerTurn));
            _cardsPerTurn = cardsPerTurn;
        }

        public void StartCombat(IEnumerable<Card> startingDeck)
        {
            if (startingDeck == null)
                throw new ArgumentNullException(nameof(startingDeck));

            _context.Player.Deck.Prepare(startingDeck);
            _bus.Publish(new CombatStartedEvent());
            BeginPlayerTurn();
        }

        /// <summary>
        /// Attempts to play a card from hand. Returns false (without changing
        /// state) if it is not the player's turn, the card is not in hand, or
        /// the player cannot afford it.
        /// </summary>
        public bool PlayCard(Card card, Combatant target = null)
        {
            if (card == null)
                throw new ArgumentNullException(nameof(card));
            if (_context.Phase != CombatPhase.PlayerTurn)
                return false;

            PlayerState player = _context.Player;
            if (!player.Deck.Hand.Cards.Contains(card))
                return false;
            if (!player.CanAfford(card.Cost))
                return false;

            player.SpendEnergy(card.Cost);
            _bus.Publish(new CardPlayedEvent(card));

            Combatant effectTarget = target ?? _context.Enemy;
            var effectContext = new EffectContext(player, effectTarget, _bus);
            foreach (ICardEffect effect in card.Effects)
                _invoker.Enqueue(new EffectCommand(effect, effectContext));
            _invoker.ExecuteAll();

            player.Deck.DiscardFromHand(card);

            CheckCombatEnd();
            PublishEndedIfOver();
            return true;
        }

        public void EndTurn()
        {
            if (_context.Phase != CombatPhase.PlayerTurn)
                return;

            _bus.Publish(new PlayerTurnEndedEvent());
            _context.Player.Deck.DiscardHand();
            RunEnemyTurn();

            if (_context.IsOver)
            {
                PublishEndedIfOver();
                return;
            }

            BeginPlayerTurn();
        }

        private void BeginPlayerTurn()
        {
            PlayerState player = _context.Player;
            player.ResetBlock();          // block does not carry over turns
            player.RefillEnergy();
            player.Deck.DrawCards(_cardsPerTurn);

            _context.Phase = CombatPhase.PlayerTurn;
            _bus.Publish(new PlayerTurnStartedEvent());
        }

        private void RunEnemyTurn()
        {
            _context.Phase = CombatPhase.EnemyTurn;
            EnemyState enemy = _context.Enemy;

            // Enemy actions reuse the same effect system as cards.
            if (enemy.Intent.Type == IntentType.Attack)
            {
                _invoker.Enqueue(new EffectCommand(
                    new DealDamageEffect(enemy.Intent.Amount),
                    new EffectContext(enemy, _context.Player, _bus)));
                _invoker.ExecuteAll();
            }

            enemy.PlanNextTurn();
            CheckCombatEnd();
        }

        private void CheckCombatEnd()
        {
            if (_context.Enemy.IsDead)
                _context.Phase = CombatPhase.Won;
            else if (_context.Player.IsDead)
                _context.Phase = CombatPhase.Lost;
        }

        private void PublishEndedIfOver()
        {
            if (_context.Phase == CombatPhase.Won)
                _bus.Publish(new CombatEndedEvent(true));
            else if (_context.Phase == CombatPhase.Lost)
                _bus.Publish(new CombatEndedEvent(false));
        }
    }
}
