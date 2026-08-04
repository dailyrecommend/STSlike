using System;
using System.Collections.Generic;
using System.Linq;
using STSlike.Domain.Events;
using STSlike.Domain.Run;
using STSlike.Domain.Run.Rewards;

namespace STSlike.Domain.Progression
{
    /// <summary>
    /// Turns run events into persisted records. It listens rather than being
    /// called, so nothing in the combat or run code knows a database exists —
    /// adding analytics later means another listener, not edits to game rules.
    /// </summary>
    public sealed class ProgressionRecorder : IDisposable
    {
        private readonly IEventBus _bus;
        private readonly RunContext _run;
        private readonly IRunHistoryRepository _history;
        private readonly ICardStatsRepository _cardStats;
        private readonly string _characterId;
        private readonly Func<DateTime> _clock;

        private bool _listening;

        public ProgressionRecorder(IEventBus bus, RunContext run,
            IRunHistoryRepository history, ICardStatsRepository cardStats,
            string characterId, Func<DateTime> clock = null)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _cardStats = cardStats ?? throw new ArgumentNullException(nameof(cardStats));

            if (string.IsNullOrWhiteSpace(characterId))
                throw new ArgumentException("CharacterId must be provided.", nameof(characterId));
            _characterId = characterId;

            _clock = clock ?? (() => DateTime.UtcNow);
        }

        public void Start()
        {
            if (_listening)
                return;

            _bus.Subscribe<RewardTakenEvent>(OnRewardTaken);
            _bus.Subscribe<RunEndedEvent>(OnRunEnded);
            _listening = true;
        }

        public void Dispose()
        {
            if (!_listening)
                return;

            _bus.Unsubscribe<RewardTakenEvent>(OnRewardTaken);
            _bus.Unsubscribe<RunEndedEvent>(OnRunEnded);
            _listening = false;
        }

        private void OnRewardTaken(RewardTakenEvent e)
        {
            if (e.Reward is AddCardReward card)
                _cardStats.RecordPick(card.Card.Id);
        }

        private void OnRunEnded(RunEndedEvent e)
        {
            _history.Add(new RunRecord(
                runId: Guid.NewGuid(),
                characterId: _characterId,
                seed: _run.Seed,
                playerWon: e.PlayerWon,
                encountersCleared: e.FloorsCleared,
                totalEncounters: _run.TotalFloors,
                deckSize: _run.Cards.Count,
                completedAtUtc: _clock()));

            // Distinct: a deck with three Strikes should count as one run for
            // Strike, otherwise duplicates would skew the win rate.
            IEnumerable<string> deckIds = _run.Cards.Select(c => c.Id).Distinct();
            _cardStats.RecordRunOutcome(deckIds, e.PlayerWon);
        }
    }
}
