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
            _bus.Publish(new CardResolvedEvent(card));
            PublishEndedIfOver();
            return true;
        }

        /// <summary>
        /// Ends the fight immediately with the given result.
        ///
        /// A development aid for skipping past content while testing the run
        /// flow. It goes through the same phase change and event as a real
        /// finish, so everything downstream — rewards, the map, run history —
        /// behaves exactly as it would have.
        /// </summary>
        public void ForceResolve(bool playerWon)
        {
            if (_context.IsOver)
                return;

            _context.Phase = playerWon ? CombatPhase.Won : CombatPhase.Lost;
            PublishEndedIfOver();
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
            _context.TurnNumber++;
            player.ResetBlock();          // block does not carry over turns
            player.RefillEnergy();
            player.Deck.DrawCards(_cardsPerTurn);

            _context.Phase = CombatPhase.PlayerTurn;
            _bus.Publish(new PlayerTurnStartedEvent());
        }

        private void RunEnemyTurn()
        {
            _context.Phase = CombatPhase.EnemyTurn;

            foreach (EnemyState enemy in _context.Enemies)
            {
                if (enemy.IsDead)
                    continue;

                // Block an enemy raised last turn protected it through the
                // player's turn; it expires as its own turn comes round.
                enemy.ResetBlock();
                Act(enemy);

                // Killing the player mid-way stops the rest of the room from
                // piling on after the fight is already decided.
                if (_context.Player.IsDead)
                    break;
            }

            foreach (EnemyState enemy in _context.Enemies)
            {
                if (!enemy.IsDead)
                    enemy.PlanNextTurn(_context.TurnNumber + 1);
            }

            CheckCombatEnd();
        }

        private void Act(EnemyState enemy)
        {
            // Announced once for the whole move, however many blows it contains.
            _bus.Publish(new EnemyActionStartedEvent(enemy, enemy.Intent));

            // Enemy actions reuse the same effect system as cards, so block and
            // damage behave identically no matter who caused them.
            switch (enemy.Intent.Type)
            {
                case IntentType.Attack:
                    // Queued as one command per hit rather than one for the
                    // total: block is spent per hit, and each lands as its own
                    // event so the view can play three impacts, not one.
                    for (int hit = 0; hit < enemy.Intent.Hits; hit++)
                    {
                        _invoker.Enqueue(new EffectCommand(
                            new DealDamageEffect(enemy.Intent.Amount),
                            new EffectContext(enemy, _context.Player, _bus)));
                    }
                    _invoker.ExecuteAll();
                    break;

                case IntentType.Defend:
                    _invoker.Enqueue(new EffectCommand(
                        new GainBlockEffect(enemy.Intent.Amount),
                        new EffectContext(enemy, _context.Player, _bus)));
                    _invoker.ExecuteAll();
                    break;

                // Buff has no mechanic behind it until status effects exist;
                // it is telegraphed and then does nothing rather than silently
                // being treated as an attack.
            }
        }

        private void CheckCombatEnd()
        {
            if (_context.AllEnemiesDead)
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
