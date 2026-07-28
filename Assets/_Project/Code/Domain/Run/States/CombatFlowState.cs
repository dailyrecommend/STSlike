using STSlike.Domain.Combat;
using STSlike.Domain.Events;
using STSlike.Domain.Run.Rewards;
using STSlike.Domain.StateMachines;

namespace STSlike.Domain.Run.States
{
    /// <summary>
    /// Fighting an encounter. Listens for the combat result only while it is the
    /// active state (subscribe on Enter, unsubscribe on Exit), then routes to
    /// reward, run-cleared, or game-over.
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
            _bus.Publish(new EncounterStartedEvent(_run.CurrentEncounter, _run.TotalEncounters));
            _launcher.StartCombat(_run.CurrentEncounter);
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

            if (_run.IsFinalEncounter)
            {
                _machine.ChangeState(new RunEndedFlowState(_bus, _run, playerWon: true));
                return;
            }

            _machine.ChangeState(new RewardFlowState(_machine, _bus, _launcher, _run, _generator));
        }
    }
}
