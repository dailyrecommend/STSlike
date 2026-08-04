using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Combat;
using STSlike.Domain.Effects;
using STSlike.Domain.Events;
using STSlike.Domain.Map;
using STSlike.Domain.Randomness;
using STSlike.Domain.Run;
using STSlike.Domain.Run.Rewards;
using STSlike.Domain.Run.States;

namespace STSlike.Tests.EditMode
{
    public class RunFlowTests
    {
        // Records what the flow asked for instead of building real combat.
        private sealed class FakeLauncher : ICombatLauncher
        {
            public readonly List<CombatRequest> Launched = new List<CombatRequest>();
            public void StartCombat(CombatRequest request) => Launched.Add(request);
        }

        private static Card MakeCard(string id) =>
            new Card(new CardDefinition(id, 1, new List<ICardEffect>()));

        private static RunMap MakeMap(int floors = 5, int seed = 1) =>
            new MapGenerator().Generate(new SystemRng(seed), new MapGenerationSettings
            {
                RowCount = floors,
                Columns = 4,
                PathCount = 3,
                TreasureRow = -1,        // keep the map to plain fights unless a test asks otherwise
                RestBeforeBoss = false,
                CombatWeight = 100,
                EventWeight = 0,
                EliteWeight = 0,
                ShopWeight = 0,
                RestWeight = 0,
            });

        private static RunContext NewRunContext(int floors = 5, int playerHp = 70,
            int seed = 1234, RunMap map = null, params string[] deck)
        {
            var player = new PlayerState("Player", playerHp, 3, new Deck(new SystemRng(seed)));
            IReadOnlyList<Card> starter = (deck.Length > 0 ? deck : new[] { "strike", "defend" })
                .Select(MakeCard).ToList();
            return new RunContext(map ?? MakeMap(floors), player, starter, seed);
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

        private static (RunFlow flow, FakeLauncher launcher, EventBus bus, RunContext run) NewRun(
            int floors = 5, RunMap map = null)
        {
            var bus = new EventBus();
            var launcher = new FakeLauncher();
            RunContext run = NewRunContext(floors, map: map);
            var flow = new RunFlow(bus, launcher, run, NewGenerator());
            return (flow, launcher, bus, run);
        }

        /// <summary>Walks onto the first available node.</summary>
        private static void TakeFirstNode(RunFlow flow) => flow.ChooseNode(flow.NodeChoices[0].Id);

        // ---- map traversal ----

        [Test]
        public void Start_OffersTheBottomFloor()
        {
            var (flow, launcher, bus, run) = NewRun();
            int offered = 0;
            bus.Subscribe<MapChoiceOfferedEvent>(_ => offered++);

            flow.Start();

            Assert.IsTrue(flow.IsChoosingNode);
            Assert.IsNotEmpty(flow.NodeChoices);
            Assert.AreEqual(1, offered);
            Assert.IsEmpty(launcher.Launched, "Nothing should start before a node is chosen.");
            foreach (var node in flow.NodeChoices)
                Assert.AreEqual(0, node.Row);
        }

        [Test]
        public void ChoosingACombatNode_StartsThatFight()
        {
            var (flow, launcher, bus, run) = NewRun();
            flow.Start();

            TakeFirstNode(flow);

            Assert.IsInstanceOf<CombatFlowState>(flow.CurrentState);
            Assert.AreEqual(1, launcher.Launched.Count);
            Assert.AreEqual(1, launcher.Launched[0].Floor);
            Assert.AreEqual(1, run.Floor);
        }

        [Test]
        public void WinningAFight_OffersARewardThenReturnsToTheMap()
        {
            var (flow, _, bus, _) = NewRun();
            flow.Start();
            TakeFirstNode(flow);

            bus.Publish(new CombatEndedEvent(true));
            Assert.IsTrue(flow.IsAwaitingReward);

            flow.SkipReward();
            Assert.IsTrue(flow.IsChoosingNode);
        }

        [Test]
        public void TheMapOnlyOffersConnectedNodes()
        {
            var (flow, _, bus, run) = NewRun();
            flow.Start();
            TakeFirstNode(flow);
            bus.Publish(new CombatEndedEvent(true));
            flow.SkipReward();

            IReadOnlyList<int> expected = run.CurrentNode.Next;
            CollectionAssert.AreEquivalent(expected, flow.NodeChoices.Select(n => n.Id).ToList());
        }

        [Test]
        public void ClimbingAdvancesTheFloor()
        {
            var (flow, launcher, bus, run) = NewRun();
            flow.Start();
            TakeFirstNode(flow);
            bus.Publish(new CombatEndedEvent(true));
            flow.SkipReward();

            TakeFirstNode(flow);

            Assert.AreEqual(2, run.Floor);
            Assert.AreEqual(2, launcher.Launched[1].Floor);
        }

        [Test]
        public void ReachingAnUnconnectedNode_IsRejected()
        {
            var (flow, _, _, run) = NewRun();
            flow.Start();

            // A node on the top floor is never reachable from the start.
            Assert.Throws<System.InvalidOperationException>(() => run.MoveTo(run.Map.Boss));
        }

        // ---- outcomes ----

        [Test]
        public void LosingAFight_EndsTheRun()
        {
            var (flow, _, bus, run) = NewRun();
            RunEndedEvent ended = default;
            int count = 0;
            bus.Subscribe<RunEndedEvent>(e => { ended = e; count++; });
            flow.Start();
            TakeFirstNode(flow);

            bus.Publish(new CombatEndedEvent(false));

            Assert.IsInstanceOf<RunEndedFlowState>(flow.CurrentState);
            Assert.AreEqual(1, count);
            Assert.IsFalse(ended.PlayerWon);
            Assert.AreEqual(0, ended.FloorsCleared);   // died on the first floor
            Assert.IsTrue(run.IsFinished);
        }

        [Test]
        public void BeatingTheBoss_ClearsTheRun()
        {
            var (flow, _, bus, run) = NewRun(floors: 3);
            RunEndedEvent ended = default;
            bus.Subscribe<RunEndedEvent>(e => ended = e);
            flow.Start();

            // Climb every floor, taking the first route each time.
            while (!run.IsFinished)
            {
                TakeFirstNode(flow);
                bus.Publish(new CombatEndedEvent(true));
                if (flow.IsAwaitingReward)
                    flow.SkipReward();
            }

            Assert.IsTrue(ended.PlayerWon);
            Assert.AreEqual(run.TotalFloors, ended.FloorsCleared);
            Assert.IsTrue(run.IsOnBoss);
        }

        // Proves Exit() released the subscription: a stale combat result must not
        // drive the flow once it has left the combat state.
        [Test]
        public void AfterTheRunEnds_FurtherCombatResultsAreIgnored()
        {
            var (flow, _, bus, _) = NewRun();
            int endedCount = 0;
            bus.Subscribe<RunEndedEvent>(_ => endedCount++);
            flow.Start();
            TakeFirstNode(flow);
            bus.Publish(new CombatEndedEvent(false));   // run over

            bus.Publish(new CombatEndedEvent(true));    // stale
            bus.Publish(new CombatEndedEvent(false));   // stale

            Assert.AreEqual(1, endedCount);
        }

        // ---- non-combat nodes ----

        [Test]
        public void ARestNode_HealsAndWaitsForThePlayer()
        {
            RunMap map = new MapGenerator().Generate(new SystemRng(3), new MapGenerationSettings
            {
                RowCount = 4, Columns = 3, PathCount = 2,
                TreasureRow = -1, RestBeforeBoss = false,
                CombatWeight = 0, EventWeight = 0, EliteWeight = 0, ShopWeight = 0, RestWeight = 100,
            });

            var bus = new EventBus();
            RunContext run = NewRunContext(map: map);
            var flow = new RunFlow(bus, new FakeLauncher(), run, NewGenerator());
            flow.Start();
            TakeFirstNode(flow);                       // floor 1 is always combat
            bus.Publish(new CombatEndedEvent(true));
            flow.SkipReward();

            run.Player.TakeDamage(40);
            int before = run.Player.CurrentHp;

            TakeFirstNode(flow);                       // floor 2 is a rest

            Assert.IsTrue(flow.IsVisitingNode);
            Assert.Greater(run.Player.CurrentHp, before);

            flow.ContinueFromNode();
            Assert.IsTrue(flow.IsChoosingNode);
        }

        [Test]
        public void ATreasureNode_AwardsCredits()
        {
            RunMap map = new MapGenerator().Generate(new SystemRng(3), new MapGenerationSettings
            {
                RowCount = 4, Columns = 3, PathCount = 2,
                TreasureRow = 1, RestBeforeBoss = false,
                CombatWeight = 100, EventWeight = 0, EliteWeight = 0, ShopWeight = 0, RestWeight = 0,
            });

            var bus = new EventBus();
            RunContext run = NewRunContext(map: map);
            var flow = new RunFlow(bus, new FakeLauncher(), run, NewGenerator());
            flow.Start();
            TakeFirstNode(flow);
            bus.Publish(new CombatEndedEvent(true));
            flow.SkipReward();

            int before = run.Credits;
            TakeFirstNode(flow);                       // floor 2 is treasure

            Assert.Greater(run.Credits, before);
            flow.ContinueFromNode();
            Assert.IsTrue(flow.IsChoosingNode);
        }

        // ---- rewards ----

        [Test]
        public void ChoosingACardReward_AddsItToTheRunDeck()
        {
            var (flow, _, bus, run) = NewRun();
            flow.Start();
            TakeFirstNode(flow);
            bus.Publish(new CombatEndedEvent(true));

            int before = run.Cards.Count;
            var offered = (AddCardReward)flow.CurrentRewards[0];
            flow.ChooseReward(0);

            Assert.AreEqual(before + 1, run.Cards.Count);
            Assert.AreEqual(offered.Card.Id, run.Cards[run.Cards.Count - 1].Id);
            Assert.IsTrue(flow.IsChoosingNode);
        }

        [Test]
        public void HealRewardIsOffered_OnlyWhenHurt()
        {
            var (flow, _, bus, run) = NewRun();
            flow.Start();
            TakeFirstNode(flow);
            run.Player.TakeDamage(20);
            bus.Publish(new CombatEndedEvent(true));

            Assert.IsTrue(flow.CurrentRewards.Any(r => r is HealReward));
        }

        [Test]
        public void CommandsOutsideTheirStep_AreNoOps()
        {
            var (flow, launcher, _, _) = NewRun();
            flow.Start();

            flow.SkipReward();        // not in a reward step
            flow.ContinueFromNode();  // not at a visit node

            Assert.IsTrue(flow.IsChoosingNode);
            Assert.IsEmpty(launcher.Launched);
        }

        // ---- run-persistent state ----

        [Test]
        public void PlayerDamage_CarriesBetweenFloors()
        {
            var (flow, _, bus, run) = NewRun();
            flow.Start();
            TakeFirstNode(flow);

            run.Player.TakeDamage(25);
            bus.Publish(new CombatEndedEvent(true));
            flow.SkipReward();
            TakeFirstNode(flow);

            Assert.AreEqual(45, run.Player.CurrentHp);   // NOT reset to 70
        }

        [Test]
        public void Restart_ReturnsToTheFootOfTheMapWithAFreshRun()
        {
            var (flow, _, bus, run) = NewRun();
            flow.Start();
            TakeFirstNode(flow);
            run.Player.TakeDamage(30);
            run.AddCard(MakeCard("reward"));
            bus.Publish(new CombatEndedEvent(true));
            flow.SkipReward();

            flow.Restart();

            Assert.IsTrue(flow.IsChoosingNode);
            Assert.IsNull(run.CurrentNode);
            Assert.AreEqual(0, run.Floor);
            Assert.AreEqual(70, run.Player.CurrentHp);
            Assert.AreEqual(2, run.Cards.Count);        // back to the starting deck
            Assert.IsFalse(run.IsFinished);
        }

        [Test]
        public void Restart_DoesNotLeaveTheOldStateSubscribed()
        {
            var (flow, launcher, bus, _) = NewRun();
            flow.Start();
            TakeFirstNode(flow);
            flow.Restart();
            TakeFirstNode(flow);

            bus.Publish(new CombatEndedEvent(true));   // should advance exactly once

            Assert.IsTrue(flow.IsAwaitingReward);
            Assert.AreEqual(2, launcher.Launched.Count);   // one fight before the restart, one after
        }
    }
}
