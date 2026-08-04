using STSlike.Domain.Events;
using STSlike.Domain.Map;
using STSlike.Domain.Run.Rewards;
using STSlike.Domain.StateMachines;

namespace STSlike.Domain.Run.States
{
    /// <summary>
    /// A node whose content has not been built yet — shops and random events.
    ///
    /// It exists so the map is fully walkable today: the node announces itself,
    /// the player acknowledges it, and the run carries on. When shops and events
    /// are implemented they become their own states and the map router points at
    /// them instead; nothing else has to change.
    /// </summary>
    public sealed class NodeVisitFlowState : IState
    {
        private readonly StateMachine _machine;
        private readonly IEventBus _bus;
        private readonly ICombatLauncher _launcher;
        private readonly RunContext _run;
        private readonly IRewardGenerator _generator;

        public MapNodeType NodeType { get; }

        public NodeVisitFlowState(StateMachine machine, IEventBus bus, ICombatLauncher launcher,
            RunContext run, IRewardGenerator generator, MapNodeType nodeType)
        {
            _machine = machine;
            _bus = bus;
            _launcher = launcher;
            _run = run;
            _generator = generator;
            NodeType = nodeType;
        }

        public void Enter() => _bus.Publish(new NodeVisitStartedEvent(NodeType));

        public void Exit() { }

        public void Continue()
        {
            _machine.ChangeState(new MapSelectionFlowState(_machine, _bus, _launcher, _run, _generator));
        }
    }
}
