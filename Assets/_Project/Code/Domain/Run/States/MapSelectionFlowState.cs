using System;
using System.Collections.Generic;
using STSlike.Domain.Events;
using STSlike.Domain.Map;
using STSlike.Domain.Run.Rewards;
using STSlike.Domain.StateMachines;

namespace STSlike.Domain.Run.States
{
    /// <summary>
    /// The map screen: the player picks where to go next.
    ///
    /// This is the hub every other node returns to, and the only place the run
    /// advances a floor. Routing by node type happens here, so adding a new kind
    /// of stop means adding a state and one case — the states themselves never
    /// need to know what comes after them.
    /// </summary>
    public sealed class MapSelectionFlowState : IState
    {
        private readonly StateMachine _machine;
        private readonly IEventBus _bus;
        private readonly ICombatLauncher _launcher;
        private readonly RunContext _run;
        private readonly IRewardGenerator _generator;

        public MapSelectionFlowState(StateMachine machine, IEventBus bus, ICombatLauncher launcher,
            RunContext run, IRewardGenerator generator)
        {
            _machine = machine;
            _bus = bus;
            _launcher = launcher;
            _run = run;
            _generator = generator;
        }

        /// <summary>The nodes the player may pick right now.</summary>
        public IReadOnlyList<MapNode> Choices => _run.AvailableNodes;

        public void Enter() => _bus.Publish(new MapChoiceOfferedEvent());

        public void Exit() { }

        /// <summary>Travels to a node. Unreachable ids are rejected by the run context.</summary>
        public void Choose(int nodeId)
        {
            MapNode node = null;
            foreach (MapNode candidate in Choices)
            {
                if (candidate.Id == nodeId)
                {
                    node = candidate;
                    break;
                }
            }

            if (node == null)
                throw new InvalidOperationException($"Node {nodeId} is not one of the current choices.");

            _run.MoveTo(node);
            _bus.Publish(new NodeEnteredEvent(node));
            _machine.ChangeState(StateFor(node));
        }

        private IState StateFor(MapNode node)
        {
            switch (node.Type)
            {
                case MapNodeType.Combat:
                case MapNodeType.Elite:
                case MapNodeType.Boss:
                    return new CombatFlowState(_machine, _bus, _launcher, _run, _generator);

                case MapNodeType.Rest:
                    return new RestFlowState(_machine, _bus, _launcher, _run, _generator);

                case MapNodeType.Treasure:
                    return new TreasureFlowState(_machine, _bus, _launcher, _run, _generator);

                default:
                    // Shop and Event have no content yet; they announce
                    // themselves and wait for the player to move on.
                    return new NodeVisitFlowState(_machine, _bus, _launcher, _run, _generator, node.Type);
            }
        }
    }
}
