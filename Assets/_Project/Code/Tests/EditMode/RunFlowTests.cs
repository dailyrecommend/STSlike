using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Combat;
using STSlike.Domain.Effects;
using STSlike.Domain.Events;
using STSlike.Domain.Randomness;
using STSlike.Domain.Run;
using STSlike.Domain.Run.Rewards;
using STSlike.Domain.Run.States;

namespace STSlike.Tests.EditMode
{
    public class RunFlowTests
    {
        // Records which encounters were launched instead of building real combat.
        private sealed class FakeLauncher : ICombatLauncher
        {
            public readonly List<int> Launched = new List<int>();
            public void StartCombat(int encounterNumber) => Launched.Add(encounterNumber);
        }

        private static Card MakeCard(string id) =>
            new Card(new CardDefinition(id, 1, new List<ICardEffect>()));

        private static RunContext NewRunContext(int encounters = 3, int playerHp = 70)
        {
            var player = new PlayerState("Player", playerHp, 3, new Deck(new SystemRng(0)));
            var starter = Enumerable.Range(0, 6).Select(i => MakeCard($"c{i}")).ToList();
            return new RunContext(encounters, player, starter);
        }

        private static RewardGenerator NewGenerator(int cardChoices = 2, int healAmount = 8)
        {
            var pool = new List<ICardDefinition>
            {
                new CardDefinition("bash", 2, new List<ICardEffect>()),
                new CardDefinition("cleave", 1, new List<ICardEffect>()),
                new CardDefinition("guard", 1, new List<ICardEffect>()),
            };
            return new RewardGenerator(new SystemRng(1), pool, cardChoices, healAmount);
        }

        private static (RunFlow flow, FakeLauncher launcher, EventBus bus) NewRun(int encounters = 3)
        {
            var bus = new EventBus();
            var launcher = new FakeLauncher();
            var flow = new RunFlow(bus, launcher, NewRunContext(encounters), NewGenerator());
            return (flow, launcher, bus);
        }

        [Test]
        public void Start_EntersCombat_AndLaunchesFirstEncounter()
        {
            var (flow, launcher, bus) = NewRun();
            EncounterStartedEvent started = default;
            bus.Subscribe<EncounterStartedEvent>(e => started = e);

            flow.Start();

            Assert.IsInstanceOf<CombatFlowState>(flow.CurrentState);
            CollectionAssert.AreEqual(new[] { 1 }, launcher.Launched);
            Assert.AreEqual(1, started.EncounterNumber);
            Assert.AreEqual(3, started.TotalEncounters);
        }

        [Test]
        public void WinningNonFinalEncounter_MovesToReward()
        {
            var (flow, _, bus) = NewRun();
            int rewardOffered = 0;
            bus.Subscribe<RewardOfferedEvent>(_ => rewardOffered++);
            flow.Start();

            bus.Publish(new CombatEndedEvent(true));

            Assert.IsInstanceOf<RewardFlowState>(flow.CurrentState);
            Assert.IsTrue(flow.IsAwaitingReward);
            Assert.AreEqual(1, rewardOffered);
        }

        [Test]
        public void SkipReward_LaunchesNextEncounter()
        {
            var (flow, launcher, bus) = NewRun();
            flow.Start();
            bus.Publish(new CombatEndedEvent(true));

            flow.SkipReward();

            Assert.IsInstanceOf<CombatFlowState>(flow.CurrentState);
            Assert.AreEqual(2, flow.Run.CurrentEncounter);
            CollectionAssert.AreEqual(new[] { 1, 2 }, launcher.Launched);
        }

        [Test]
        public void LosingAnyEncounter_EndsRunAsFailure()
        {
            var (flow, _, bus) = NewRun();
            RunEndedEvent ended = default;
            int count = 0;
            bus.Subscribe<RunEndedEvent>(e => { ended = e; count++; });
            flow.Start();

            bus.Publish(new CombatEndedEvent(false));

            Assert.IsInstanceOf<RunEndedFlowState>(flow.CurrentState);
            Assert.AreEqual(1, count);
            Assert.IsFalse(ended.PlayerWon);
            Assert.AreEqual(0, ended.EncountersCleared); // died on encounter 1
            Assert.IsTrue(flow.Run.IsFinished);
        }

        [Test]
        public void ClearingFinalEncounter_EndsRunAsVictory()
        {
            var (flow, launcher, bus) = NewRun(encounters: 2);
            RunEndedEvent ended = default;
            bus.Subscribe<RunEndedEvent>(e => ended = e);
            flow.Start();

            bus.Publish(new CombatEndedEvent(true)); // clear 1 -> reward
            flow.SkipReward();               // encounter 2 (final)
            bus.Publish(new CombatEndedEvent(true)); // clear final

            Assert.IsInstanceOf<RunEndedFlowState>(flow.CurrentState);
            Assert.IsTrue(ended.PlayerWon);
            Assert.AreEqual(2, ended.EncountersCleared);
            CollectionAssert.AreEqual(new[] { 1, 2 }, launcher.Launched);
        }

        // Proves Exit() released the subscription: a stale combat result must not
        // drive the flow once it has left the combat state.
        [Test]
        public void AfterRunEnds_FurtherCombatResultsAreIgnored()
        {
            var (flow, _, bus) = NewRun();
            int endedCount = 0;
            bus.Subscribe<RunEndedEvent>(_ => endedCount++);
            flow.Start();
            bus.Publish(new CombatEndedEvent(false)); // run over

            bus.Publish(new CombatEndedEvent(true));  // stale
            bus.Publish(new CombatEndedEvent(false)); // stale

            Assert.AreEqual(1, endedCount);
            Assert.IsInstanceOf<RunEndedFlowState>(flow.CurrentState);
        }

        [Test]
        public void SkipReward_OutsideReward_IsNoOp()
        {
            var (flow, launcher, _) = NewRun();
            flow.Start();

            flow.SkipReward(); // still in combat

            Assert.IsInstanceOf<CombatFlowState>(flow.CurrentState);
            CollectionAssert.AreEqual(new[] { 1 }, launcher.Launched);
        }

        [Test]
        public void Restart_BeginsAFreshRunFromEncounterOne()
        {
            var (flow, launcher, bus) = NewRun();
            flow.Start();
            bus.Publish(new CombatEndedEvent(true));
            flow.SkipReward(); // now on encounter 2

            flow.Restart();

            Assert.IsInstanceOf<CombatFlowState>(flow.CurrentState);
            Assert.AreEqual(1, flow.Run.CurrentEncounter);
            Assert.IsFalse(flow.Run.IsFinished);
            CollectionAssert.AreEqual(new[] { 1, 2, 1 }, launcher.Launched);
        }

        // After a restart the old combat state must no longer be listening,
        // otherwise one combat result would be handled twice.
        [Test]
        public void Restart_DoesNotLeaveOldStateSubscribed()
        {
            var (flow, launcher, bus) = NewRun();
            flow.Start();
            flow.Restart();

            bus.Publish(new CombatEndedEvent(true)); // should advance exactly once

            Assert.IsInstanceOf<RewardFlowState>(flow.CurrentState);
            CollectionAssert.AreEqual(new[] { 1, 1 }, launcher.Launched);
        }

        [Test]
        public void RunContext_RejectsZeroEncounters()
        {
            var player = new PlayerState("Player", 70, 3, new Deck(new SystemRng(0)));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new RunContext(0, player, new List<Card>()));
        }

        // ---- run-persistent player state ----

        [Test]
        public void PlayerDamage_CarriesAcrossEncounters()
        {
            var bus = new EventBus();
            var launcher = new FakeLauncher();
            RunContext run = NewRunContext(encounters: 3, playerHp: 70);
            var flow = new RunFlow(bus, launcher, run, NewGenerator());
            flow.Start();

            run.Player.TakeDamage(25);       // hurt during encounter 1
            bus.Publish(new CombatEndedEvent(true));
            flow.SkipReward();       // into encounter 2

            Assert.AreEqual(2, run.CurrentEncounter);
            Assert.AreEqual(45, run.Player.CurrentHp); // NOT reset to 70
        }

        [Test]
        public void Restart_RestoresFullHealthAndStartingDeck()
        {
            var bus = new EventBus();
            var launcher = new FakeLauncher();
            RunContext run = NewRunContext(encounters: 3, playerHp: 70);
            var flow = new RunFlow(bus, launcher, run, NewGenerator());
            flow.Start();

            run.Player.TakeDamage(30);
            run.Player.GainBlock(9);
            run.AddCard(MakeCard("reward"));
            Assert.AreEqual(7, run.Cards.Count);

            flow.Restart();

            Assert.AreEqual(70, run.Player.CurrentHp);
            Assert.AreEqual(0, run.Player.Block);
            Assert.AreEqual(6, run.Cards.Count); // back to the starting deck
        }

        [Test]
        public void AddCard_GrowsTheRunDeck()
        {
            RunContext run = NewRunContext();

            run.AddCard(MakeCard("bash"));

            Assert.AreEqual(7, run.Cards.Count);
            Assert.AreEqual("bash", run.Cards[6].Id);
        }

        // ---- rewards ----

        [Test]
        public void ClearingEncounter_OffersCardChoices()
        {
            var (flow, _, bus) = NewRun();
            flow.Start();

            bus.Publish(new CombatEndedEvent(true));

            // 2 card choices; no heal because the player is at full health.
            Assert.AreEqual(2, flow.CurrentRewards.Count);
            CollectionAssert.AllItemsAreInstancesOfType(
                (System.Collections.ICollection)flow.CurrentRewards, typeof(AddCardReward));
        }

        [Test]
        public void ChoosingACardReward_AddsItToTheRunDeck()
        {
            var bus = new EventBus();
            var launcher = new FakeLauncher();
            RunContext run = NewRunContext();
            var flow = new RunFlow(bus, launcher, run, NewGenerator());
            flow.Start();
            bus.Publish(new CombatEndedEvent(true));

            var offered = (AddCardReward)flow.CurrentRewards[0];
            flow.ChooseReward(0);

            Assert.AreEqual(7, run.Cards.Count);
            Assert.AreEqual(offered.Card.Id, run.Cards[6].Id);
            Assert.AreEqual(2, run.CurrentEncounter); // advanced
        }

        [Test]
        public void HealRewardIsOffered_OnlyWhenHurt_AndRestoresHp()
        {
            var bus = new EventBus();
            var launcher = new FakeLauncher();
            RunContext run = NewRunContext(playerHp: 70);
            var flow = new RunFlow(bus, launcher, run, NewGenerator(cardChoices: 1, healAmount: 8));
            flow.Start();
            run.Player.TakeDamage(20); // now 50/70
            bus.Publish(new CombatEndedEvent(true));

            Assert.AreEqual(2, flow.CurrentRewards.Count); // 1 card + heal
            int healIndex = flow.CurrentRewards[0] is HealReward ? 0 : 1;
            Assert.IsInstanceOf<HealReward>(flow.CurrentRewards[healIndex]);

            flow.ChooseReward(healIndex);

            Assert.AreEqual(58, run.Player.CurrentHp);
        }

        [Test]
        public void SkippingReward_LeavesDeckAndHpUnchanged()
        {
            var bus = new EventBus();
            var launcher = new FakeLauncher();
            RunContext run = NewRunContext();
            var flow = new RunFlow(bus, launcher, run, NewGenerator());
            flow.Start();
            run.Player.TakeDamage(10);
            bus.Publish(new CombatEndedEvent(true));

            flow.SkipReward();

            Assert.AreEqual(6, run.Cards.Count);
            Assert.AreEqual(60, run.Player.CurrentHp);
            Assert.AreEqual(2, run.CurrentEncounter);
        }

        [Test]
        public void RewardGenerator_NeverOffersTheSameCardTwice()
        {
            RunContext run = NewRunContext();
            var generator = new RewardGenerator(new SystemRng(7), new List<ICardDefinition>
            {
                new CardDefinition("a", 1, new List<ICardEffect>()),
                new CardDefinition("b", 1, new List<ICardEffect>()),
                new CardDefinition("c", 1, new List<ICardEffect>()),
            }, cardChoices: 3, healAmount: 0);

            var options = generator.Generate(run);

            var ids = options.OfType<AddCardReward>().Select(o => o.Card.Id).ToList();
            CollectionAssert.AllItemsAreUnique(ids);
            Assert.AreEqual(3, ids.Count);
        }

        [Test]
        public void RewardGenerator_HandlesPoolSmallerThanChoices()
        {
            RunContext run = NewRunContext();
            var generator = new RewardGenerator(new SystemRng(0), new List<ICardDefinition>
            {
                new CardDefinition("only", 1, new List<ICardEffect>()),
            }, cardChoices: 3, healAmount: 0);

            var options = generator.Generate(run);

            Assert.AreEqual(1, options.Count);
        }

        [Test]
        public void ChooseReward_WithInvalidIndex_Throws()
        {
            var (flow, _, bus) = NewRun();
            flow.Start();
            bus.Publish(new CombatEndedEvent(true));

            Assert.Throws<System.ArgumentOutOfRangeException>(() => flow.ChooseReward(99));
        }

        // Same seed, same offers — rewards stay part of a reproducible run.
        [Test]
        public void RewardGenerator_IsDeterministicForASeed()
        {
            RunContext run = NewRunContext();
            var pool = new List<ICardDefinition>
            {
                new CardDefinition("a", 1, new List<ICardEffect>()),
                new CardDefinition("b", 1, new List<ICardEffect>()),
                new CardDefinition("c", 1, new List<ICardEffect>()),
                new CardDefinition("d", 1, new List<ICardEffect>()),
            };

            var first = new RewardGenerator(new SystemRng(42), pool, 2, 0).Generate(run);
            var second = new RewardGenerator(new SystemRng(42), pool, 2, 0).Generate(run);

            CollectionAssert.AreEqual(
                first.OfType<AddCardReward>().Select(o => o.Card.Id).ToArray(),
                second.OfType<AddCardReward>().Select(o => o.Card.Id).ToArray());
        }
    }
}
