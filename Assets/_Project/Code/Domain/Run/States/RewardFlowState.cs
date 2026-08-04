using System;
using System.Collections.Generic;
using STSlike.Domain.Events;
using STSlike.Domain.Run.Rewards;
using STSlike.Domain.StateMachines;

namespace STSlike.Domain.Run.States
{
    /// <summary>
    /// After a fight: offers rewards and waits for the player to take one (or
    /// skip) before returning to the map. The options are generated once on
    /// Enter so the offer cannot be rerolled by looking at it again.
    /// </summary>
    public sealed class RewardFlowState : IState
    {
        private readonly StateMachine _machine;
        private readonly IEventBus _bus;
        private readonly ICombatLauncher _launcher;
        private readonly RunContext _run;
        private readonly IRewardGenerator _generator;

        public IReadOnlyList<IRunReward> Options { get; private set; } = Array.Empty<IRunReward>();

        public RewardFlowState(StateMachine machine, IEventBus bus, ICombatLauncher launcher,
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
            Options = _generator.Generate(_run);
            _bus.Publish(new RewardOfferedEvent(_run.Floor));
        }

        public void Exit() { }

        /// <summary>Takes the reward at <paramref name="index"/> and returns to the map.</summary>
        public void Choose(int index)
        {
            if (index < 0 || index >= Options.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            IRunReward chosen = Options[index];
            chosen.Apply(_run);
            _bus.Publish(new RewardTakenEvent(_run.Floor, chosen));
            BackToMap();
        }

        /// <summary>Declines every option and returns to the map.</summary>
        public void Skip() => BackToMap();

        private void BackToMap()
        {
            _machine.ChangeState(new MapSelectionFlowState(_machine, _bus, _launcher, _run, _generator));
        }
    }
}
