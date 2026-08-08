using System;
using System.Collections.Generic;
using UnityEngine;
using STSlike.Data;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Effects;
using STSlike.Domain.Combat;
using STSlike.Domain.Events;
using STSlike.Domain.Map;
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
        private readonly CombatSequencer _sequencer;

        private readonly List<Card> _handCards = new List<Card>();
        private readonly List<CardView> _hand = new List<CardView>();
        private readonly List<RewardView> _rewards = new List<RewardView>();
        private readonly List<ActorHudView> _actors = new List<ActorHudView>();

        // Set when the turn is handed over, cleared once the screen has finished
        // showing what the enemy did with it.
        private bool _resolvingEnemyTurn;

        /// <summary>Any visible state may have changed; rebind the HUD.</summary>
        public event Action Changed;

        /// <summary>A card was played — carries its art and motion for the stage.</summary>
        public event Action<CardView> CardPlayed;

        /// <summary>A fight has been built and the figures can be spawned.</summary>
        public event Action CombatStarted;

        /// <summary>An enemy is starting its move — play its motion once here.</summary>
        public event Action<ActorActionView> EnemyActionStarted;

        /// <summary>Damage landed — drive floating numbers and hit reactions from this.</summary>
        public event Action<HitView> Hit;

        /// <summary>Block was gained.</summary>
        public event Action<HitView> BlockGained;

        public event Action PlayerTurnStarted;

        /// <summary>The run has entered a fight, before it has been built.</summary>
        public event Action EncounterStarted;

        public event Action RewardOffered;

        /// <summary>True when the run is over; the bool is whether it was cleared.</summary>
        public event Action<bool> RunEnded;

        public CombatPresenter(IEventBus bus, RunFlow flow, CombatRunner runner,
            CombatSequencer sequencer)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _sequencer = sequencer ?? throw new ArgumentNullException(nameof(sequencer));

            // Whether the screen has caught up is not something the rules raise
            // an event about, but it decides whether the player may act.
            _sequencer.BusyChanged += OnSequencerBusyChanged;

            _bus.Subscribe<EncounterStartedEvent>(OnEncounterStarted);
            _bus.Subscribe<CombatStartedEvent>(OnCombatStarted);
            _bus.Subscribe<EnemyActionStartedEvent>(OnEnemyActionStarted);
            _bus.Subscribe<PlayerTurnStartedEvent>(OnPlayerTurnStarted);
            _bus.Subscribe<CardPlayedEvent>(OnCardPlayed);
            _bus.Subscribe<CardResolvedEvent>(OnAnyChange);
            _bus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            _bus.Subscribe<BlockGainedEvent>(OnBlockGained);
            _bus.Subscribe<EnergyGainedEvent>(OnAnyChange);
            _bus.Subscribe<CardsDrawnEvent>(OnAnyChange);
            _bus.Subscribe<PlayerTurnEndedEvent>(OnPlayerTurnEnded);
            _bus.Subscribe<CombatEndedEvent>(OnAnyChange);
            _bus.Subscribe<MapChoiceOfferedEvent>(OnAnyChange);
            _bus.Subscribe<NodeEnteredEvent>(OnAnyChange);
            _bus.Subscribe<NodeVisitStartedEvent>(OnAnyChange);
            _bus.Subscribe<RewardOfferedEvent>(OnRewardOffered);
            _bus.Subscribe<RewardTakenEvent>(OnAnyChange);
            _bus.Subscribe<RunEndedEvent>(OnRunEnded);
        }

        public void Dispose()
        {
            _sequencer.BusyChanged -= OnSequencerBusyChanged;
            _bus.Unsubscribe<EncounterStartedEvent>(OnEncounterStarted);
            _bus.Unsubscribe<CombatStartedEvent>(OnCombatStarted);
            _bus.Unsubscribe<EnemyActionStartedEvent>(OnEnemyActionStarted);
            _bus.Unsubscribe<PlayerTurnStartedEvent>(OnPlayerTurnStarted);
            _bus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
            _bus.Unsubscribe<CardResolvedEvent>(OnAnyChange);
            _bus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            _bus.Unsubscribe<BlockGainedEvent>(OnBlockGained);
            _bus.Unsubscribe<EnergyGainedEvent>(OnAnyChange);
            _bus.Unsubscribe<CardsDrawnEvent>(OnAnyChange);
            _bus.Unsubscribe<PlayerTurnEndedEvent>(OnAnyChange);
            _bus.Unsubscribe<CombatEndedEvent>(OnAnyChange);
            _bus.Unsubscribe<MapChoiceOfferedEvent>(OnAnyChange);
            _bus.Unsubscribe<NodeEnteredEvent>(OnAnyChange);
            _bus.Unsubscribe<NodeVisitStartedEvent>(OnAnyChange);
            _bus.Unsubscribe<RewardOfferedEvent>(OnRewardOffered);
            _bus.Unsubscribe<RewardTakenEvent>(OnAnyChange);
            _bus.Unsubscribe<RunEndedEvent>(OnRunEnded);
        }

        // ---- state the UI reads ----

        public bool IsAwaitingReward => _flow.IsAwaitingReward;
        public bool IsRunOver => _flow.Run.IsFinished;

        /// <summary>True while the map screen is asking where to go next.</summary>
        public bool IsChoosingNode => _flow.IsChoosingNode;

        /// <summary>True at a rest, treasure or shop node waiting to be acknowledged.</summary>
        public bool IsVisitingNode => _flow.IsVisitingNode;

        /// <summary>
        /// True while the enemy's turn is playing out on screen.
        ///
        /// The rules have no such state to ask about: ending a turn settles the
        /// enemy's whole reply on the spot and hands the turn straight back. The
        /// stretch the player experiences as the enemy's turn is the screen
        /// catching up afterwards, which is this — the turn was ended, and the
        /// queue has not drained yet.
        ///
        /// Guarded by the queue as well as the flag so a fight with no stage to
        /// animate never locks: with nothing enqueued, it is over at once.
        /// </summary>
        public bool IsEnemyTurn => _resolvingEnemyTurn && _sequencer.IsBusy;

        /// <summary>
        /// Whether the player may play a card or end the turn right now.
        ///
        /// Their own cards do not block them: playing two in a row is allowed
        /// and the animations simply queue behind each other, which is how a
        /// deck that chains cards is meant to feel. It is the enemy's reply
        /// that has to be watched before the board can be acted on again.
        /// </summary>
        public bool CanAct => IsInCombat && !IsEnemyTurn;

        /// <summary>False while any other screen is up or the run has ended.</summary>
        public bool IsInCombat =>
            !IsAwaitingReward && !IsChoosingNode && !IsVisitingNode && !IsRunOver
            && _runner.Current != null && !_runner.Current.Context.IsOver;

        /// <summary>The nodes that can be travelled to right now.</summary>
        public IReadOnlyList<MapNodeView> NodeChoices
        {
            get
            {
                var views = new List<MapNodeView>();
                foreach (MapNode node in _flow.NodeChoices)
                    views.Add(new MapNodeView(node.Id, node.Row, node.Column, node.Type,
                        true, false, false, node.Next));
                return views;
            }
        }

        /// <summary>Every node on the act map, for drawing the map screen.</summary>
        public IReadOnlyList<MapNodeView> MapNodes
        {
            get
            {
                RunContext run = _flow.Run;
                var reachable = new HashSet<int>();
                foreach (MapNode node in _flow.NodeChoices)
                    reachable.Add(node.Id);

                var views = new List<MapNodeView>(run.Map.Nodes.Count);
                foreach (MapNode node in run.Map.Nodes)
                {
                    views.Add(new MapNodeView(node.Id, node.Row, node.Column, node.Type,
                        reachable.Contains(node.Id),
                        run.CurrentNode != null && run.CurrentNode.Id == node.Id,
                        run.HasVisited(node.Id),
                        node.Next));
                }
                return views;
            }
        }

        /// <summary>Travels to one of the offered nodes.</summary>
        public void ChooseNode(int nodeId) => _flow.ChooseNode(nodeId);

        /// <summary>Acknowledges a rest/treasure/shop node and returns to the map.</summary>
        public void ContinueFromNode() => _flow.ContinueFromNode();

        /// <summary>Current turn of the ongoing fight, counting from 1. Zero between fights.</summary>
        public int Turn => _runner.Current?.Context.TurnNumber ?? 0;

        public RunView Run
        {
            get
            {
                RunContext run = _flow.Run;
                MapNode node = run.CurrentNode;

                return new RunView(run.CharacterId, run.Floor, run.TotalFloors,
                    run.Credits, run.Cards.Count, run.Seed, run.IsFinished, run.PlayerWon,
                    node != null ? node.Type : MapNodeType.Combat, node != null);
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

        /// <summary>
        /// Everyone on stage, the player first and then the room in slot order.
        ///
        /// One shape for both sides is what lets a single widget float over any
        /// figure — a fight with four opponents needs no new UI, only more of
        /// the same. The list is reused between calls, so the overlay can ask
        /// for it every frame without generating garbage.
        /// </summary>
        public IReadOnlyList<ActorHudView> Actors
        {
            get
            {
                _actors.Clear();
                _actors.Add(DescribePlayer());

                CombatManager manager = _runner.Current;
                if (manager == null)
                    return _actors;

                IReadOnlyList<Domain.Actors.EnemyState> enemies = manager.Context.Enemies;
                for (int i = 0; i < enemies.Count; i++)
                    _actors.Add(DescribeEnemy(enemies[i], i));

                return _actors;
            }
        }

        /// <summary>
        /// One figure by reference. False when nobody holds that place — a slot
        /// the room never filled, or a view a frame out of date.
        ///
        /// Built straight from the rules rather than by searching
        /// <see cref="Actors"/>. That list is reused between calls, so going
        /// through it here would let a caller iterating it trigger the rebuild
        /// that empties the very list they are walking.
        /// </summary>
        public bool TryGetActor(ActorRef actor, out ActorHudView found)
        {
            found = default;
            if (!actor.IsValid)
                return false;

            if (actor.IsPlayer)
            {
                found = DescribePlayer();
                return true;
            }

            CombatManager manager = _runner.Current;
            if (manager == null)
                return false;

            IReadOnlyList<Domain.Actors.EnemyState> enemies = manager.Context.Enemies;
            int slot = actor.EnemyIndex;
            if (slot < 0 || slot >= enemies.Count)
                return false;

            found = DescribeEnemy(enemies[slot], slot);
            return true;
        }

        private ActorHudView DescribePlayer()
        {
            PlayerView player = Player;
            return new ActorHudView(ActorRef.Player, player.Name,
                player.Hp, player.MaxHp, player.Block, isEnemy: false);
        }

        private static ActorHudView DescribeEnemy(Domain.Actors.EnemyState enemy, int slot)
        {
            Domain.Actors.EnemyIntent intent = enemy.Intent;

            return new ActorHudView(ActorRef.Enemy(slot), enemy.Name,
                enemy.CurrentHp, enemy.MaxHp, enemy.Block, isEnemy: true,
                intent != null ? intent.Type : IntentType.Unknown,
                intent != null ? intent.Amount : 0,
                intent != null ? intent.Hits : 1);
        }

        /// <summary>Looks up the motion authored beside this step of the enemy's routine.</summary>
        private string MotionFor(int slot, int moveIndex)
        {
            EnemyData data = DataFor(slot);
            return data != null ? data.AnimationFor(moveIndex) : null;
        }

        private float ImpactDelayFor(int slot, int moveIndex)
        {
            EnemyData data = DataFor(slot);
            return data != null ? data.ImpactDelayFor(moveIndex) : 0f;
        }

        private float HitIntervalFor(int slot, int moveIndex)
        {
            EnemyData data = DataFor(slot);
            return data != null ? data.HitIntervalFor(moveIndex) : 0f;
        }

        // Motion and timing are authored beside the asset, so content from
        // anywhere else simply has none and the stage uses its fallbacks.
        private EnemyData DataFor(int slot)
        {
            IReadOnlyList<Domain.Actors.IEnemyDefinition> data = _runner.CurrentEnemies;
            return slot >= 0 && slot < data.Count ? data[slot] as EnemyData : null;
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

        /// <summary>
        /// Plays the card at that hand slot, at the enemy in that slot of the
        /// room. Pass -1 to let it fall to the first opponent still standing,
        /// which is what an unaimed card wants.
        ///
        /// Out-of-range and unaffordable calls are ignored rather than throwing:
        /// a view is allowed to be a frame out of date.
        /// </summary>
        public void PlayCard(int handIndex, int enemySlot = -1)
        {
            CombatManager manager = _runner.Current;
            if (manager == null || !CanAct)
                return;

            RebuildHandCards();
            if (handIndex < 0 || handIndex >= _handCards.Count)
                return;

            manager.PlayCard(_handCards[handIndex], EnemyAt(enemySlot, manager));
        }

        private static Domain.Actors.Combatant EnemyAt(int slot, CombatManager manager)
        {
            IReadOnlyList<Domain.Actors.EnemyState> enemies = manager.Context.Enemies;

            // A slot that has emptied since the click — the last blow of a
            // flurry killed it — falls back rather than fizzling the card.
            if (slot >= 0 && slot < enemies.Count && !enemies[slot].IsDead)
                return enemies[slot];

            return manager.Context.Enemy;
        }

        public void EndTurn()
        {
            if (CanAct)
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

        /// <summary>
        /// Development aid: skips whatever is currently on screen — wins the
        /// fight, declines the reward, takes the first route, acknowledges a
        /// node. Everything goes through the normal commands, so the run stays
        /// in a state it could have reached by playing.
        /// </summary>
        public void DebugSkip()
        {
            if (IsRunOver)
            {
                StartNewRun();
                return;
            }

            if (IsAwaitingReward)
            {
                SkipReward();
                return;
            }

            if (IsVisitingNode)
            {
                ContinueFromNode();
                return;
            }

            if (IsChoosingNode)
            {
                IReadOnlyList<MapNode> choices = _flow.NodeChoices;
                if (choices.Count > 0)
                    ChooseNode(choices[0].Id);
                return;
            }

            _runner.Current?.ForceResolve(playerWon: true);
        }

        // ---- event plumbing ----

        private void OnEncounterStarted(EncounterStartedEvent e)
        {
            EncounterStarted?.Invoke();
            RaiseChanged();
        }

        // Raised before the enemy acts, and the enemy's whole turn follows in the
        // same breath — so this is the last moment the player is known to have
        // handed control over.
        private void OnPlayerTurnEnded(PlayerTurnEndedEvent e)
        {
            _resolvingEnemyTurn = true;
            RaiseChanged();
        }

        private void OnSequencerBusyChanged()
        {
            if (!_sequencer.IsBusy)
                _resolvingEnemyTurn = false;

            RaiseChanged();
        }

        private void OnPlayerTurnStarted(PlayerTurnStartedEvent e)
        {
            PlayerTurnStarted?.Invoke();
            RaiseChanged();
        }

        private void OnCardPlayed(CardPlayedEvent e)
        {
            CardPlayed?.Invoke(BuildCardView(e.Card, -1, playable: false));
            RaiseChanged();
        }

        private void OnCombatStarted(CombatStartedEvent e)
        {
            CombatStarted?.Invoke();
            RaiseChanged();
        }

        private void OnEnemyActionStarted(EnemyActionStartedEvent e)
        {
            ActorRef actor = Identify(e.Enemy);
            int move = e.Intent != null ? e.Intent.MoveIndex : -1;

            EnemyActionStarted?.Invoke(new ActorActionView(
                actor,
                MotionFor(actor.EnemyIndex, move),
                ImpactDelayFor(actor.EnemyIndex, move),
                HitIntervalFor(actor.EnemyIndex, move)));
        }

        private void OnDamageDealt(DamageDealtEvent e)
        {
            Hit?.Invoke(new HitView(Identify(e.Source), Identify(e.Target),
                e.Target.Name, e.Amount, e.HpLost));
            RaiseChanged();
        }

        private void OnBlockGained(BlockGainedEvent e)
        {
            ActorRef actor = Identify(e.Actor);
            BlockGained?.Invoke(new HitView(actor, actor, e.Actor.Name, e.Amount));
            RaiseChanged();
        }

        /// <summary>
        /// Turns a combatant into a stage reference. Enemies are identified by
        /// their slot, which is what the stage uses to find the right model.
        /// </summary>
        private ActorRef Identify(Domain.Actors.Combatant combatant)
        {
            if (combatant == null)
                return ActorRef.None;
            if (ReferenceEquals(combatant, _flow.Run.Player))
                return ActorRef.Player;

            CombatManager manager = _runner.Current;
            if (manager == null)
                return ActorRef.None;

            IReadOnlyList<Domain.Actors.EnemyState> enemies = manager.Context.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (ReferenceEquals(enemies[i], combatant))
                    return ActorRef.Enemy(i);
            }

            return ActorRef.None;
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

        // Inspection lists carry HandIndex -1, so a view built from them can
        // never be wired to PlayCard by accident. They are still marked playable
        // because that flag drives the "cannot afford" dimming — a card being
        // browsed in a pile should look normal, not greyed out.
        private static List<CardView> Describe(IReadOnlyList<Card> cards, bool sorted)
        {
            var views = new List<CardView>(cards.Count);
            foreach (Card card in cards)
                views.Add(BuildCardView(card, -1, playable: true));

            if (sorted)
                views.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

            return views;
        }

        private static CardView BuildCardView(Card card, int handIndex, bool playable)
        {
            // The definition at runtime is the authoring asset, so the display
            // fields can be read straight off it. Definitions built in code
            // (tests, generated cards) simply fall back to the id.
            var data = card.Definition as CardData;

            string name = data != null ? data.DisplayName : card.Id;
            Sprite art = data != null ? data.Artwork : null;
            string description = data != null
                ? FormatDescription(data.DescriptionTemplate, card.Effects)
                : string.Empty;

            return new CardView(handIndex, card.Id, card.Cost, playable,
                name, description, art, card.Definition.Type, card.Definition.Rarity,
                data != null ? data.AnimationTrigger : null,
                data != null ? data.ImpactDelay : 0f,
                data != null ? data.HitInterval : 0f,
                card.Definition.Target);
        }

        /// <summary>
        /// Fills "{0}" style placeholders with the effects' own numbers, in the
        /// order the effects are listed. Keeping the text tied to the effects is
        /// what stops a rebalanced or upgraded card from lying about itself.
        /// </summary>
        private static string FormatDescription(string template, IReadOnlyList<ICardEffect> effects)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            var values = new List<object>();
            foreach (ICardEffect effect in effects)
            {
                if (effect is IDescribableEffect describable)
                    values.Add(describable.DisplayValue);
            }

            if (values.Count == 0)
                return template;

            try
            {
                return string.Format(template, values.ToArray());
            }
            catch (FormatException)
            {
                // A typo in the template must not take the card off the screen.
                return template;
            }
        }

        /// <summary>
        /// The hand as the rules hold it, for resolving a hand slot to a card.
        ///
        /// Kept separate from building the views so that playing a card does not
        /// have to touch the list <see cref="Hand"/> hands out: a view iterating
        /// the hand and playing from it would otherwise empty the collection it
        /// was walking.
        /// </summary>
        private void RebuildHandCards()
        {
            _handCards.Clear();

            CombatManager manager = _runner.Current;
            if (manager == null)
                return;

            _handCards.AddRange(manager.Context.Player.Deck.Hand.Cards);
        }

        private void RebuildHand()
        {
            RebuildHandCards();
            _hand.Clear();

            CombatManager manager = _runner.Current;
            if (manager == null)
                return;

            // Playable means "clicking this would do something", which is not
            // only about energy: the whole hand dims while the enemy is taking
            // its turn. Cards the player is chaining themselves stay lit.
            bool canAct = CanAct;
            PlayerState player = manager.Context.Player;

            for (int i = 0; i < _handCards.Count; i++)
            {
                Card card = _handCards[i];
                _hand.Add(BuildCardView(card, i, canAct && player.CanAfford(card.Cost)));
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
