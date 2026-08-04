using System;
using System.Collections.Generic;
using STSlike.Domain.Events;
using STSlike.Domain.Map;
using STSlike.Domain.Run.Rewards;
using STSlike.Domain.Run.States;
using STSlike.Domain.StateMachines;

namespace STSlike.Domain.Run
{
    /// <summary>
    /// Drives a run up the act map with a <see cref="StateMachine"/>: choose a
    /// node -> resolve it -> back to the map, until the boss falls or the player
    /// does.
    ///
    /// Each state owns its own transitions, so new stops on the route are added
    /// as new states rather than by editing a central switch. This class is just
    /// the thin surface the game talks to.
    /// </summary>
    public sealed class RunFlow
    {
        private readonly IEventBus _bus;
        private readonly ICombatLauncher _launcher;
        private readonly RunContext _run;
        private readonly IRewardGenerator _generator;

        private StateMachine _machine = new StateMachine();

        public RunContext Run => _run;
        public IState CurrentState => _machine.Current;

        public bool IsChoosingNode => _machine.Current is MapSelectionFlowState;
        public bool IsAwaitingReward => _machine.Current is RewardFlowState;

        /// <summary>True at a node that is just waiting to be acknowledged.</summary>
        public bool IsVisitingNode =>
            _machine.Current is RestFlowState ||
            _machine.Current is TreasureFlowState ||
            _machine.Current is NodeVisitFlowState;

        /// <summary>The nodes that may be travelled to, or empty when not on the map.</summary>
        public IReadOnlyList<MapNode> NodeChoices =>
            _machine.Current is MapSelectionFlowState map ? map.Choices : Array.Empty<MapNode>();

        /// <summary>The offers on the table, or empty when not in a reward step.</summary>
        public IReadOnlyList<IRunReward> CurrentRewards =>
            _machine.Current is RewardFlowState reward ? reward.Options : Array.Empty<IRunReward>();

        public RunFlow(IEventBus bus, ICombatLauncher launcher, RunContext run, IRewardGenerator generator)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        }

        public void Start()
        {
            _bus.Publish(new RunStartedEvent());
            _machine.Start(new MapSelectionFlowState(_machine, _bus, _launcher, _run, _generator));
        }

        /// <summary>Abandons the current run and begins a fresh one.</summary>
        public void Restart()
        {
            _machine.Current?.Exit();   // release the old state's subscriptions
            _machine = new StateMachine();
            _run.Reset();
            Start();
        }

        /// <summary>Travels to one of the offered nodes. No-op unless on the map.</summary>
        public void ChooseNode(int nodeId)
        {
            if (_machine.Current is MapSelectionFlowState map)
                map.Choose(nodeId);
        }

        /// <summary>Acknowledges a rest, treasure or placeholder node. No-op elsewhere.</summary>
        public void ContinueFromNode()
        {
            switch (_machine.Current)
            {
                case RestFlowState rest:
                    rest.Continue();
                    break;
                case TreasureFlowState treasure:
                    treasure.Continue();
                    break;
                case NodeVisitFlowState visit:
                    visit.Continue();
                    break;
            }
        }

        /// <summary>Takes an offered reward. No-op unless a reward is pending.</summary>
        public void ChooseReward(int index)
        {
            if (_machine.Current is RewardFlowState reward)
                reward.Choose(index);
        }

        /// <summary>Declines the offer. No-op unless a reward is pending.</summary>
        public void SkipReward()
        {
            if (_machine.Current is RewardFlowState reward)
                reward.Skip();
        }
    }
}
