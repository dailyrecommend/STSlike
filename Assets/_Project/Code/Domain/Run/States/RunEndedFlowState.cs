using STSlike.Domain.Events;
using STSlike.Domain.StateMachines;

namespace STSlike.Domain.Run.States
{
    /// <summary>
    /// Terminal state: the run was cleared or failed. Records the outcome on the
    /// run context and announces it — the hook where run history will later be
    /// written to the database.
    /// </summary>
    public sealed class RunEndedFlowState : IState
    {
        private readonly IEventBus _bus;
        private readonly RunContext _run;
        private readonly bool _playerWon;

        public bool PlayerWon => _playerWon;

        public RunEndedFlowState(IEventBus bus, RunContext run, bool playerWon)
        {
            _bus = bus;
            _run = run;
            _playerWon = playerWon;
        }

        public void Enter()
        {
            // A cleared run finished its final encounter; a failed one cleared
            // every encounter before the current.
            int cleared = _playerWon ? _run.TotalEncounters : _run.CurrentEncounter - 1;

            _run.Finish(_playerWon);
            _bus.Publish(new RunEndedEvent(_playerWon, cleared));
        }

        public void Exit() { }
    }
}
