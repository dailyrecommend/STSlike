using System;
using System.Collections.Generic;
using STSlike.Domain.Events;
using STSlike.Domain.Run.Rewards;
using STSlike.Domain.Run.States;
using STSlike.Domain.StateMachines;

namespace STSlike.Domain.Run
{
    /// <summary>
    /// Drives a run through its states (combat -> reward -> combat -> ... ->
    /// cleared / game over) with a <see cref="StateMachine"/>. Each state owns
    /// its own transition rules, so new stops on the route (shop, event, boss)
    /// are added as new states rather than by editing a central switch.
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
        public bool IsAwaitingReward => _machine.Current is RewardFlowState;

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
            _machine.Start(new CombatFlowState(_machine, _bus, _launcher, _run, _generator));
        }

        /// <summary>Abandons the current run and begins a fresh one.</summary>
        public void Restart()
        {
            _machine.Current?.Exit();   // release the old state's subscriptions
            _machine = new StateMachine();
            _run.Reset();
            Start();
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
