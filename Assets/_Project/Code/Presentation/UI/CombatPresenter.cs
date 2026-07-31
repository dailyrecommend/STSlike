using System;
using System.Collections.Generic;
using STSlike.Domain.Cards;
using STSlike.Domain.Combat;
using STSlike.Domain.Events;
using STSlike.Domain.Run;
using STSlike.Domain.Run.Rewards;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// The single seam between the game and its visuals.
    ///
    /// Views ask this for display-ready snapshots and call these commands; they
    /// never touch the domain. That keeps prefab wiring stable while the rules
    /// evolve, and it means a view cannot put the game into an illegal state —
    /// every command goes through the same validation the domain already does.
    ///
    /// Events are for things that should *animate* (a hit, a card leaving the
    /// hand); <see cref="Changed"/> is the catch-all "redraw yourself" signal.
    /// </summary>
    public sealed class CombatPresenter : IDisposable
    {
        private readonly IEventBus _bus;
        private readonly RunFlow _flow;
        private readonly CombatRunner _runner;

        private readonly List<Card> _handCards = new List<Card>();
        private readonly List<CardView> _hand = new List<CardView>();
        private readonly List<RewardView> _rewards = new List<RewardView>();

        /// <summary>Any visible state may have changed; rebind the HUD.</summary>
        public event Action Changed;

        /// <summary>A card finished resolving (id).</summary>
        public event Action<string> CardPlayed;

        /// <summary>Damage landed — drive floating numbers and hit reactions from this.</summary>
        public event Action<HitView> Hit;

        /// <summary>Block was gained.</summary>
        public event Action<HitView> BlockGained;

        public event Action PlayerTurnStarted;
        public event Action EncounterStarted;
        public event Action RewardOffered;

        /// <summary>True when the run is over; the bool is whether it was cleared.</summary>
        public event Action<bool> RunEnded;

        public CombatPresenter(IEventBus bus, RunFlow flow, CombatRunner runner)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));

            _bus.Subscribe<EncounterStartedEvent>(OnEncounterStarted);
            _bus.Subscribe<PlayerTurnStartedEvent>(OnPlayerTurnStarted);
            _bus.Subscribe<CardPlayedEvent>(OnCardPlayed);
            _bus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            _bus.Subscribe<BlockGainedEvent>(OnBlockGained);
            _bus.Subscribe<PlayerTurnEndedEvent>(OnAnyChange);
            _bus.Subscribe<CombatEndedEvent>(OnAnyChange);
            _bus.Subscribe<RewardOfferedEvent>(OnRewardOffered);
            _bus.Subscribe<RewardTakenEvent>(OnAnyChange);
            _bus.Subscribe<RunEndedEvent>(OnRunEnded);
        }

        public void Dispose()
        {
            _bus.Unsubscribe<EncounterStartedEvent>(OnEncounterStarted);
            _bus.Unsubscribe<PlayerTurnStartedEvent>(OnPlayerTurnStarted);
            _bus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
            _bus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            _bus.Unsubscribe<BlockGainedEvent>(OnBlockGained);
            _bus.Unsubscribe<PlayerTurnEndedEvent>(OnAnyChange);
            _bus.Unsubscribe<CombatEndedEvent>(OnAnyChange);
            _bus.Unsubscribe<RewardOfferedEvent>(OnRewardOffered);
            _bus.Unsubscribe<RewardTakenEvent>(OnAnyChange);
            _bus.Unsubscribe<RunEndedEvent>(OnRunEnded);
        }

        // ---- state the UI reads ----

        public bool IsAwaitingReward => _flow.IsAwaitingReward;
        public bool IsRunOver => _flow.Run.IsFinished;

        /// <summary>False while a reward screen is up or the run has ended.</summary>
        public bool IsInCombat =>
            !IsAwaitingReward && !IsRunOver && _runner.Current != null && !_runner.Current.Context.IsOver;

        public RunView Run
        {
            get
            {
                RunContext run = _flow.Run;
                return new RunView(run.CharacterId, run.CurrentEncounter, run.TotalEncounters,
                    run.Credits, run.Cards.Count, run.Seed, run.IsFinished, run.PlayerWon);
            }
        }

        public PlayerView Player
        {
            get
            {
                var player = _flow.Run.Player;
                Deck deck = player.Deck;
                return new PlayerView(player.Name, player.CurrentHp, player.MaxHp,
                    player.Block, player.Energy, player.MaxEnergy,
                    deck.DrawPile.Count, deck.DiscardPile.Count, deck.Hand.Count);
            }
        }

        /// <summary>
        /// Contents of the draw pile, for the "click the pile to inspect it"
        /// popup. Deliberately sorted by id: showing the real shuffled order
        /// would leak the next draws.
        /// </summary>
        public IReadOnlyList<CardView> DrawPileContents => Describe(_flow.Run.Player.Deck.DrawPile.Cards, sorted: true);

        /// <summary>Contents of the discard pile, in the order they were discarded.</summary>
        public IReadOnlyList<CardView> DiscardPileContents => Describe(_flow.Run.Player.Deck.DiscardPile.Cards, sorted: false);

        /// <summary>Every card owned this run, for the deck viewer in the top bar.</summary>
        public IReadOnlyList<CardView> RunDeckContents => Describe(_flow.Run.Cards, sorted: true);

        public EnemyView Enemy
        {
            get
            {
                CombatManager manager = _runner.Current;
                if (manager == null)
                    return default;

                var enemy = manager.Context.Enemy;
                return new EnemyView(enemy.Name, enemy.CurrentHp, enemy.MaxHp, enemy.Block,
                    enemy.Intent.Type.ToString(), enemy.Intent.Amount);
            }
        }

        public IReadOnlyList<CardView> Hand
        {
            get
            {
                RebuildHand();
                return _hand;
            }
        }

        public IReadOnlyList<RewardView> Rewards
        {
            get
            {
                RebuildRewards();
                return _rewards;
            }
        }

        // ---- commands the UI calls ----

        /// <summary>Plays the card at that hand slot. Out-of-range and unaffordable calls are ignored.</summary>
        public void PlayCard(int handIndex)
        {
            CombatManager manager = _runner.Current;
            if (manager == null || !IsInCombat)
                return;

            RebuildHand();
            if (handIndex < 0 || handIndex >= _handCards.Count)
                return;

            manager.PlayCard(_handCards[handIndex], manager.Context.Enemy);
        }

        public void EndTurn()
        {
            if (IsInCombat)
                _runner.Current.EndTurn();
        }

        public void ChooseReward(int optionIndex)
        {
            if (!IsAwaitingReward)
                return;
            if (optionIndex < 0 || optionIndex >= _flow.CurrentRewards.Count)
                return;

            _flow.ChooseReward(optionIndex);
        }

        public void SkipReward() => _flow.SkipReward();

        public void StartNewRun() => _flow.Restart();

        // ---- event plumbing ----

        private void OnEncounterStarted(EncounterStartedEvent e)
        {
            EncounterStarted?.Invoke();
            RaiseChanged();
        }

        private void OnPlayerTurnStarted(PlayerTurnStartedEvent e)
        {
            PlayerTurnStarted?.Invoke();
            RaiseChanged();
        }

        private void OnCardPlayed(CardPlayedEvent e)
        {
            CardPlayed?.Invoke(e.Card.Id);
            RaiseChanged();
        }

        private void OnDamageDealt(DamageDealtEvent e)
        {
            bool onPlayer = ReferenceEquals(e.Target, _flow.Run.Player);
            Hit?.Invoke(new HitView(onPlayer, e.Target.Name, e.Amount));
            RaiseChanged();
        }

        private void OnBlockGained(BlockGainedEvent e)
        {
            bool onPlayer = ReferenceEquals(e.Actor, _flow.Run.Player);
            BlockGained?.Invoke(new HitView(onPlayer, e.Actor.Name, e.Amount));
            RaiseChanged();
        }

        private void OnRewardOffered(RewardOfferedEvent e)
        {
            RewardOffered?.Invoke();
            RaiseChanged();
        }

        private void OnRunEnded(RunEndedEvent e)
        {
            RunEnded?.Invoke(e.PlayerWon);
            RaiseChanged();
        }

        private void OnAnyChange<T>(T _) => RaiseChanged();

        private void RaiseChanged() => Changed?.Invoke();

        // Inspection lists are not playable, so HandIndex is -1: a view built
        // from these can never be wired to PlayCard by accident.
        private static List<CardView> Describe(IReadOnlyList<Card> cards, bool sorted)
        {
            var views = new List<CardView>(cards.Count);
            foreach (Card card in cards)
                views.Add(new CardView(-1, card.Id, card.Cost, false));

            if (sorted)
                views.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

            return views;
        }

        private void RebuildHand()
        {
            _handCards.Clear();
            _hand.Clear();

            CombatManager manager = _runner.Current;
            if (manager == null)
                return;

            var player = manager.Context.Player;
            IReadOnlyList<Card> cards = player.Deck.Hand.Cards;
            for (int i = 0; i < cards.Count; i++)
            {
                Card card = cards[i];
                _handCards.Add(card);
                _hand.Add(new CardView(i, card.Id, card.Cost, player.CanAfford(card.Cost)));
            }
        }

        private void RebuildRewards()
        {
            _rewards.Clear();

            IReadOnlyList<IRunReward> options = _flow.CurrentRewards;
            for (int i = 0; i < options.Count; i++)
            {
                switch (options[i])
                {
                    case AddCardReward card:
                        _rewards.Add(new RewardView(i, RewardKind.AddCard, card.Card.Id, card.Card.Cost, 0));
                        break;
                    case HealReward heal:
                        _rewards.Add(new RewardView(i, RewardKind.Heal, null, 0, heal.Amount));
                        break;
                    default:
                        _rewards.Add(new RewardView(i, RewardKind.Other, null, 0, 0));
                        break;
                }
            }
        }
    }
}
