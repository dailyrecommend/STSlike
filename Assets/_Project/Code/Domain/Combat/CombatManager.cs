using System;
using System.Collections.Generic;
using System.Linq;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Commands;
using STSlike.Domain.Effects;
using STSlike.Domain.Events;
using STSlike.Domain.Randomness;

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
        private readonly IRng _rng;
        private readonly CommandInvoker _invoker = new CommandInvoker();

        // Reused so that playing a card allocates nothing.
        private readonly List<Combatant> _targets = new List<Combatant>();
        private readonly List<Combatant> _living = new List<Combatant>();

        public CombatContext Context => _context;

        /// <summary>Commands executed so far this combat (for replay/debugging).</summary>
        public IReadOnlyList<ICommand> CommandHistory => _invoker.History;

        /// <summary>
        /// The random source is optional: only cards that scatter their target
        /// need one, and a fight built without one simply resolves those on the
        /// first enemy standing rather than failing to start.
        /// </summary>
        public CombatManager(CombatContext context, IEventBus bus, int cardsPerTurn = 5,
            IRng rng = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            if (cardsPerTurn < 0)
                throw new ArgumentOutOfRangeException(nameof(cardsPerTurn));
            _cardsPerTurn = cardsPerTurn;
            _rng = rng;
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

            // One pass per recipient. A card that sweeps the room runs its
            // effects once per enemy, so block is spent and events are raised
            // per victim exactly as they would be for separate cards.
            foreach (Combatant victim in ResolveTargets(card, target))
            {
                var effectContext = new EffectContext(player, victim, _bus);
                foreach (ICardEffect effect in card.Effects)
                    _invoker.Enqueue(new EffectCommand(effect, effectContext));
            }
            _invoker.ExecuteAll();

            player.Deck.DiscardFromHand(card);

            CheckCombatEnd();
            _bus.Publish(new CardResolvedEvent(card));
            PublishEndedIfOver();
            return true;
        }

        /// <summary>
        /// Everyone this card lands on.
        ///
        /// The mode is authored on the card rather than inferred from its type:
        /// a sweeping attack asks the player for nothing, and a skill that lands
        /// a debuff asks for exactly as much as a strike does. An empty result
        /// is legitimate — a room sweep with nobody left standing fizzles, and
        /// the card is still spent and discarded.
        /// </summary>
        private List<Combatant> ResolveTargets(Card card, Combatant picked)
        {
            _targets.Clear();

            switch (card.Definition.Target)
            {
                case CardTarget.Self:
                    _targets.Add(_context.Player);
                    break;

                case CardTarget.AllEnemies:
                    foreach (EnemyState enemy in _context.Enemies)
                    {
                        if (!enemy.IsDead)
                            _targets.Add(enemy);
                    }
                    break;

                case CardTarget.RandomEnemy:
                    Combatant rolled = RandomLivingEnemy();
                    if (rolled != null)
                        _targets.Add(rolled);
                    break;

                default:
                    // The pick, or the first one standing when a view played
                    // this without naming anybody.
                    Combatant single = picked ?? _context.Enemy;
                    if (single != null)
                        _targets.Add(single);
                    break;
            }

            return _targets;
        }

        /// <summary>
        /// Drawn from the run's seeded stream, so a seed reproduces which enemy
        /// a scattering card happened to hit. Without a source it falls to the
        /// first one standing rather than reaching for a global RNG, which would
        /// quietly break that guarantee.
        /// </summary>
        private Combatant RandomLivingEnemy()
        {
            _living.Clear();
            foreach (EnemyState enemy in _context.Enemies)
            {
                if (!enemy.IsDead)
                    _living.Add(enemy);
            }

            if (_living.Count == 0)
                return null;

            return _rng != null ? _living[_rng.Next(_living.Count)] : _living[0];
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
