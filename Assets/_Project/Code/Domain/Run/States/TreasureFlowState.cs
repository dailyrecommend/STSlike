using STSlike.Domain.Events;
using STSlike.Domain.Map;
using STSlike.Domain.Run.Rewards;
using STSlike.Domain.StateMachines;

namespace STSlike.Domain.Run.States
{
    /// <summary>
    /// A treasure node: hands over credits, then waits for the player before
    /// returning to the map.
    /// </summary>
    public sealed class TreasureFlowState : IState
    {
        /// <summary>Credits awarded, before floor scaling.</summary>
        public const int BaseCredits = 50;

        /// <summary>Extra credits per floor, so later treasure keeps up with shop prices.</summary>
        public const int CreditsPerFloor = 5;

        private readonly StateMachine _machine;
        private readonly IEventBus _bus;
        private readonly ICombatLauncher _launcher;
        private readonly RunContext _run;
        private readonly IRewardGenerator _generator;

        public int AwardedCredits { get; private set; }

        public TreasureFlowState(StateMachine machine, IEventBus bus, ICombatLauncher launcher,
            RunContext run, IRewardGenerator generator)
        {
            _machine = machine;
            _bus = bus;
            _launcher = launcher;
            _run = run;
            _generator = generator;
        }

        public void Enter()
        {
            AwardedCredits = BaseCredits + CreditsPerFloor * (_run.Floor - 1);
            _run.AddCredits(AwardedCredits);

            _bus.Publish(new NodeVisitStartedEvent(MapNodeType.Treasure));
            _bus.Publish(new TreasureFoundEvent(AwardedCredits));
        }

        public void Exit() { }

        public void Continue()
        {
            _machine.ChangeState(new MapSelectionFlowState(_machine, _bus, _launcher, _run, _generator));
        }
    }
}
