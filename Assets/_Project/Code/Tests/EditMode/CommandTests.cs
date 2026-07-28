using System;
using System.Collections.Generic;
using NUnit.Framework;
using STSlike.Domain.Actors;
using STSlike.Domain.Commands;
using STSlike.Domain.Effects;

namespace STSlike.Tests.EditMode
{
    public class CommandTests
    {
        // Records the order in which it runs, for asserting queue behavior.
        private sealed class RecordingCommand : ICommand
        {
            private readonly List<string> _log;
            private readonly string _id;
            public RecordingCommand(List<string> log, string id) { _log = log; _id = id; }
            public void Execute() => _log.Add(_id);
        }

        [Test]
        public void EffectCommand_Execute_RunsTheEffect()
        {
            var source = new Combatant("Player", 70);
            var target = new Combatant("Enemy", 40);
            var command = new EffectCommand(new DealDamageEffect(9), new EffectContext(source, target));

            command.Execute();

            Assert.AreEqual(31, target.CurrentHp);
        }

        [Test]
        public void EffectCommand_RejectsNulls()
        {
            Assert.Throws<ArgumentNullException>(() => new EffectCommand(null, null));
        }

        [Test]
        public void Invoker_ExecuteAll_RunsInFifoOrder()
        {
            var log = new List<string>();
            var invoker = new CommandInvoker();
            invoker.Enqueue(new RecordingCommand(log, "a"));
            invoker.Enqueue(new RecordingCommand(log, "b"));
            invoker.Enqueue(new RecordingCommand(log, "c"));

            invoker.ExecuteAll();

            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, log);
        }

        [Test]
        public void Invoker_RecordsExecutedCommandsInHistory()
        {
            var invoker = new CommandInvoker();
            invoker.Enqueue(new RecordingCommand(new List<string>(), "x"));
            invoker.Enqueue(new RecordingCommand(new List<string>(), "y"));

            invoker.ExecuteAll();

            Assert.AreEqual(2, invoker.History.Count);
        }

        [Test]
        public void Invoker_ExecuteNext_RunsOneAtATime()
        {
            var log = new List<string>();
            var invoker = new CommandInvoker();
            invoker.Enqueue(new RecordingCommand(log, "a"));
            invoker.Enqueue(new RecordingCommand(log, "b"));

            Assert.IsTrue(invoker.HasPending);
            Assert.IsTrue(invoker.ExecuteNext());
            Assert.AreEqual(1, log.Count);      // only one ran
            Assert.IsTrue(invoker.ExecuteNext());
            Assert.IsFalse(invoker.ExecuteNext()); // queue empty
            Assert.IsFalse(invoker.HasPending);
        }
    }
}
