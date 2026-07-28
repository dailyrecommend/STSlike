using System;
using System.Collections.Generic;

namespace STSlike.Domain.Commands
{
    /// <summary>
    /// Queues commands and executes them in order, recording a history.
    /// ExecuteAll is today's synchronous behavior; ExecuteNext exists so a
    /// later animation layer can pace commands one at a time. History (with the
    /// run's seeded RNG) is the basis for replay and debugging.
    /// </summary>
    public sealed class CommandInvoker
    {
        private readonly Queue<ICommand> _pending = new Queue<ICommand>();
        private readonly List<ICommand> _history = new List<ICommand>();

        public bool HasPending => _pending.Count > 0;
        public IReadOnlyList<ICommand> History => _history;

        public void Enqueue(ICommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            _pending.Enqueue(command);
        }

        /// <summary>Executes the next pending command; returns false if none remain.</summary>
        public bool ExecuteNext()
        {
            if (_pending.Count == 0)
                return false;

            ICommand command = _pending.Dequeue();
            command.Execute();
            _history.Add(command);
            return true;
        }

        /// <summary>Executes all pending commands in FIFO order.</summary>
        public void ExecuteAll()
        {
            while (ExecuteNext()) { }
        }
    }
}
