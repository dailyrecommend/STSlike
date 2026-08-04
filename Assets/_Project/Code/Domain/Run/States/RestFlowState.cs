using STSlike.Domain.Events;
using STSlike.Domain.Run.Rewards;
using STSlike.Domain.StateMachines;

namespace STSlike.Domain.Run.States
{
    /// <summary>
    /// A campfire: restores a share of maximum health, then waits for the player
    /// before returning to the map.
    ///
    /// Healing a percentage rather than a flat amount keeps rest stops worth the
    /// same at every point in a run, and it is the counterweight that makes
    /// taking a risky elite route a real decision.
    /// </summary>
    public sealed class RestFlowState : IState
    {
        /// <summary>Share of max HP restored.</summary>
        public const float HealFraction = 0.3f;

        private readonly StateMachine _machine;
        private readonly IEventBus _bus;
        private readonly ICombatLauncher _launcher;
        private readonly RunContext _run;
        private readonly IRewardGenerator _generator;

        public int HealedAmount { get; private set; }

        public RestFlowState(StateMachine machine, IEventBus bus, ICombatLauncher launcher,
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
            int before = _run.Player.CurrentHp;
            _run.Player.Heal(Mathf_CeilToInt(_run.Player.MaxHp * HealFraction));
            HealedAmount = _run.Player.CurrentHp - before;

            _bus.Publish(new NodeVisitStartedEvent(Map.MapNodeType.Rest));
            _bus.Publish(new RestedEvent(HealedAmount));
        }

        public void Exit() { }

        public void Continue()
        {
            _machine.ChangeState(new MapSelectionFlowState(_machine, _bus, _launcher, _run, _generator));
        }

        // The domain has no engine reference, so no Mathf here.
        private static int Mathf_CeilToInt(float value) => (int)System.Math.Ceiling(value);
    }
}
