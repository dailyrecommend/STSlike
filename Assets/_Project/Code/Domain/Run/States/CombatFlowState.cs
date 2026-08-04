using STSlike.Domain.Combat;
using STSlike.Domain.Events;
using STSlike.Domain.Map;
using STSlike.Domain.Run.Rewards;
using STSlike.Domain.StateMachines;

namespace STSlike.Domain.Run.States
{
    /// <summary>
    /// Fighting whatever stands on the current node. Listens for the result only
    /// while it is the active state (subscribe on Enter, unsubscribe on Exit),
    /// then routes to reward, the cleared run, or game over.
    /// </summary>
    public sealed class CombatFlowState : IState
    {
        private readonly StateMachine _machine;
        private readonly IEventBus _bus;
        private readonly ICombatLauncher _launcher;
        private readonly RunContext _run;
        private readonly IRewardGenerator _generator;

        public CombatFlowState(StateMachine machine, IEventBus bus, ICombatLauncher launcher,
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
            _bus.Subscribe<CombatEndedEvent>(OnCombatEnded);

            MapNodeType type = _run.CurrentNode != null ? _run.CurrentNode.Type : MapNodeType.Combat;
            _bus.Publish(new EncounterStartedEvent(_run.Floor, _run.TotalFloors, type));
            _launcher.StartCombat(new CombatRequest(_run.Floor, type));
        }

        public void Exit()
        {
            _bus.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
        }

        private void OnCombatEnded(CombatEndedEvent e)
        {
            if (!e.PlayerWon)
            {
                _machine.ChangeState(new RunEndedFlowState(_bus, _run, playerWon: false));
                return;
            }

            // Beating the boss is the top of the act: there is nothing above it
            // to award a reward for.
            if (_run.IsOnBoss)
            {
                _machine.ChangeState(new RunEndedFlowState(_bus, _run, playerWon: true));
                return;
            }

            _machine.ChangeState(new RewardFlowState(_machine, _bus, _launcher, _run, _generator));
        }
    }
}
