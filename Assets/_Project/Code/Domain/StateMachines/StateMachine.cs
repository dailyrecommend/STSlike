using System;

namespace STSlike.Domain.StateMachines
{
    /// <summary>
    /// A minimal finite state machine: holds the current state and, on a
    /// transition, exits the old state before entering the new one. Reusable
    /// for game flow (menu / map / combat / reward), enemy AI, and so on.
    /// </summary>
    public sealed class StateMachine
    {
        public IState Current { get; private set; }

        public bool IsRunning => Current != null;

        /// <summary>Enters the initial state.</summary>
        public void Start(IState initial)
        {
            if (initial == null)
                throw new ArgumentNullException(nameof(initial));
            if (Current != null)
                throw new InvalidOperationException("State machine already started.");

            Current = initial;
            Current.Enter();
        }

        /// <summary>Exits the current state (if any) and enters the next one.</summary>
        public void ChangeState(IState next)
        {
            if (next == null)
                throw new ArgumentNullException(nameof(next));

            Current?.Exit();
            Current = next;
            Current.Enter();
        }
    }
}
