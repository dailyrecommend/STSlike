using System;
using System.Collections.Generic;
using NUnit.Framework;
using STSlike.Domain.StateMachines;

namespace STSlike.Tests.EditMode
{
    public class StateMachineTests
    {
        // Records enter/exit calls into a shared log for order assertions.
        private sealed class LoggingState : IState
        {
            private readonly List<string> _log;
            private readonly string _id;
            public LoggingState(List<string> log, string id) { _log = log; _id = id; }
            public void Enter() => _log.Add($"enter {_id}");
            public void Exit() => _log.Add($"exit {_id}");
        }

        [Test]
        public void Start_EntersInitialState()
        {
            var log = new List<string>();
            var machine = new StateMachine();
            var a = new LoggingState(log, "A");

            machine.Start(a);

            Assert.AreSame(a, machine.Current);
            Assert.IsTrue(machine.IsRunning);
            CollectionAssert.AreEqual(new[] { "enter A" }, log);
        }

        [Test]
        public void ChangeState_ExitsOldThenEntersNew_InOrder()
        {
            var log = new List<string>();
            var machine = new StateMachine();
            var a = new LoggingState(log, "A");
            var b = new LoggingState(log, "B");
            machine.Start(a);
            log.Clear();

            machine.ChangeState(b);

            Assert.AreSame(b, machine.Current);
            CollectionAssert.AreEqual(new[] { "exit A", "enter B" }, log);
        }

        [Test]
        public void Start_Twice_Throws()
        {
            var machine = new StateMachine();
            machine.Start(new LoggingState(new List<string>(), "A"));

            Assert.Throws<InvalidOperationException>(
                () => machine.Start(new LoggingState(new List<string>(), "B")));
        }

        [Test]
        public void ChangeState_Null_Throws()
        {
            var machine = new StateMachine();
            machine.Start(new LoggingState(new List<string>(), "A"));

            Assert.Throws<ArgumentNullException>(() => machine.ChangeState(null));
        }

        // A state that transitions immediately on Enter (e.g. a pass-through
        // reward screen) must leave the machine in the final state.
        [Test]
        public void TransitionDuringEnter_LandsOnFinalState()
        {
            var log = new List<string>();
            var machine = new StateMachine();
            var final = new LoggingState(log, "Final");

            var passThrough = new PassThroughState(machine, final);
            machine.Start(passThrough);

            Assert.AreSame(final, machine.Current);
        }

        private sealed class PassThroughState : IState
        {
            private readonly StateMachine _machine;
            private readonly IState _next;
            public PassThroughState(StateMachine machine, IState next) { _machine = machine; _next = next; }
            public void Enter() => _machine.ChangeState(_next);
            public void Exit() { }
        }
    }
}
