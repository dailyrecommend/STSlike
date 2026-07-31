using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Combat;
using STSlike.Domain.Effects;
using STSlike.Domain.Events;
using STSlike.Domain.Progression;
using STSlike.Domain.Randomness;
using STSlike.Domain.Run;
using STSlike.Domain.Run.Rewards;
using STSlike.Infrastructure.Persistence;

namespace STSlike.Tests.EditMode
{
    public class ProgressionTests
    {
        private sealed class FakeLauncher : ICombatLauncher
        {
            public void StartCombat(int encounterNumber) { }
        }

        private static Card MakeCard(string id) =>
            new Card(new CardDefinition(id, 1, new List<ICardEffect>()));

        private static CardDefinition Definition(string id) =>
            new CardDefinition(id, 1, new List<ICardEffect>());

        private static RunContext NewRunContext(int encounters = 2, int seed = 1234, params string[] deck)
        {
            var player = new PlayerState("Player", 70, 3, new Deck(new SystemRng(seed)));
            IReadOnlyList<Card> starter = (deck.Length > 0 ? deck : new[] { "strike", "defend" })
                .Select(MakeCard).ToList();
            return new RunContext(encounters, player, starter, seed);
        }

        // ---- repositories ----

        [Test]
        public void RunHistory_StoresRecords_AndSummarizes()
        {
            var repo = new InMemoryRunHistoryRepository();
            var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            repo.Add(new RunRecord(Guid.NewGuid(), "albino", 1, true, 3, 3, 12, now));
            repo.Add(new RunRecord(Guid.NewGuid(), "albino", 2, false, 1, 3, 10, now.AddMinutes(5)));

            Assert.AreEqual(2, repo.TotalRuns);
            Assert.AreEqual(1, repo.TotalWins);
        }

        [Test]
        public void RunHistory_GetRecent_ReturnsNewestFirst()
        {
            var repo = new InMemoryRunHistoryRepository();
            var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            repo.Add(new RunRecord(Guid.NewGuid(), "albino", 1, true, 3, 3, 12, now));
            repo.Add(new RunRecord(Guid.NewGuid(), "albino", 2, false, 1, 3, 10, now.AddMinutes(5)));

            IReadOnlyList<RunRecord> recent = repo.GetRecent(1);

            Assert.AreEqual(1, recent.Count);
            Assert.AreEqual(2, recent[0].Seed); // the later run
        }

        [Test]
        public void CardStats_CountsPicksAndWinRate()
        {
            var repo = new InMemoryCardStatsRepository();

            repo.RecordPick("bash");
            repo.RecordPick("bash");
            repo.RecordRunOutcome(new[] { "bash", "strike" }, playerWon: true);
            repo.RecordRunOutcome(new[] { "bash" }, playerWon: false);

            CardStats bash = repo.GetAll().Single(s => s.CardId == "bash");
            Assert.AreEqual(2, bash.PickCount);
            Assert.AreEqual(2, bash.RunsWithCard);
            Assert.AreEqual(1, bash.WinsWithCard);
            Assert.AreEqual(0.5f, bash.WinRate, 0.0001f);

            CardStats strike = repo.GetAll().Single(s => s.CardId == "strike");
            Assert.AreEqual(0, strike.PickCount); // never picked, only present
            Assert.AreEqual(1f, strike.WinRate, 0.0001f);
        }

        // ---- recorder wired to the run ----

        [Test]
        public void FinishingARun_WritesAHistoryRecord()
        {
            var bus = new EventBus();
            RunContext run = NewRunContext(encounters: 1, seed: 4242);
            var history = new InMemoryRunHistoryRepository();
            var stats = new InMemoryCardStatsRepository();
            var fixedTime = new DateTime(2026, 7, 29, 9, 0, 0, DateTimeKind.Utc);

            using (var recorder = new ProgressionRecorder(bus, run, history, stats, "albino", () => fixedTime))
            {
                recorder.Start();
                var flow = new RunFlow(bus, new FakeLauncher(), run, NewGenerator());
                flow.Start();

                bus.Publish(new CombatEndedEvent(true)); // final encounter -> run cleared
            }

            Assert.AreEqual(1, history.TotalRuns);
            RunRecord record = history.GetRecent(1)[0];
            Assert.AreEqual("albino", record.CharacterId);
            Assert.AreEqual(4242, record.Seed);       // seed persisted for replay
            Assert.IsTrue(record.PlayerWon);
            Assert.AreEqual(1, record.EncountersCleared);
            Assert.AreEqual(fixedTime, record.CompletedAtUtc);
        }

        [Test]
        public void TakingACardReward_CountsAsAPick()
        {
            var bus = new EventBus();
            RunContext run = NewRunContext();
            var history = new InMemoryRunHistoryRepository();
            var stats = new InMemoryCardStatsRepository();

            using (var recorder = new ProgressionRecorder(bus, run, history, stats, "albino"))
            {
                recorder.Start();
                bus.Publish(new RewardTakenEvent(1, new AddCardReward(Definition("bash"))));
            }

            Assert.AreEqual(1, stats.GetAll().Single(s => s.CardId == "bash").PickCount);
        }

        [Test]
        public void TakingANonCardReward_DoesNotTouchCardStats()
        {
            var bus = new EventBus();
            var stats = new InMemoryCardStatsRepository();

            using (var recorder = new ProgressionRecorder(bus, NewRunContext(),
                       new InMemoryRunHistoryRepository(), stats, "albino"))
            {
                recorder.Start();
                bus.Publish(new RewardTakenEvent(1, new HealReward(8)));
            }

            Assert.AreEqual(0, stats.GetAll().Count);
        }

        [Test]
        public void RunOutcome_CreditsEachDistinctCardOnce()
        {
            var bus = new EventBus();
            // Three copies of the same card must count as one run for it.
            RunContext run = NewRunContext(encounters: 1, seed: 7, deck: new[] { "strike", "strike", "strike" });
            var history = new InMemoryRunHistoryRepository();
            var stats = new InMemoryCardStatsRepository();

            using (var recorder = new ProgressionRecorder(bus, run, history, stats, "albino"))
            {
                recorder.Start();
                bus.Publish(new RunEndedEvent(true, 1));
            }

            CardStats strike = stats.GetAll().Single(s => s.CardId == "strike");
            Assert.AreEqual(1, strike.RunsWithCard);
            Assert.AreEqual(1, strike.WinsWithCard);
        }

        [Test]
        public void DisposedRecorder_StopsListening()
        {
            var bus = new EventBus();
            var history = new InMemoryRunHistoryRepository();

            var recorder = new ProgressionRecorder(bus, NewRunContext(),
                history, new InMemoryCardStatsRepository(), "albino");
            recorder.Start();
            recorder.Dispose();

            bus.Publish(new RunEndedEvent(true, 1));

            Assert.AreEqual(0, history.TotalRuns);
        }

        private static RewardGenerator NewGenerator()
        {
            var pool = new List<ICardDefinition> { Definition("bash"), Definition("cleave") };
            return new RewardGenerator(new SystemRng(1), pool, cardChoices: 1, healAmount: 0);
        }
    }
}
