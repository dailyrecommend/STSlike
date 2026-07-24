using NUnit.Framework;
using STSlike.Domain.Events;

namespace STSlike.Tests.EditMode
{
    public class EventBusTests
    {
        private readonly struct Ping
        {
            public readonly int Value;
            public Ping(int value) => Value = value;
        }

        private readonly struct Pong { }

        [Test]
        public void Publish_DeliversToSubscriber_WithPayload()
        {
            var bus = new EventBus();
            int received = 0;
            bus.Subscribe<Ping>(e => received = e.Value);

            bus.Publish(new Ping(42));

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNothing()
        {
            var bus = new EventBus();

            Assert.DoesNotThrow(() => bus.Publish(new Ping(1)));
        }

        [Test]
        public void Publish_ReachesAllSubscribers()
        {
            var bus = new EventBus();
            int a = 0, b = 0;
            bus.Subscribe<Ping>(e => a += e.Value);
            bus.Subscribe<Ping>(e => b += e.Value);

            bus.Publish(new Ping(5));

            Assert.AreEqual(5, a);
            Assert.AreEqual(5, b);
        }

        [Test]
        public void Publish_OnlyReachesMatchingEventType()
        {
            var bus = new EventBus();
            int pings = 0;
            bus.Subscribe<Ping>(_ => pings++);

            bus.Publish(new Pong()); // different type

            Assert.AreEqual(0, pings);
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            var bus = new EventBus();
            int count = 0;
            void Handler(Ping _) => count++;

            bus.Subscribe<Ping>(Handler);
            bus.Publish(new Ping(1));
            bus.Unsubscribe<Ping>(Handler);
            bus.Publish(new Ping(1));

            Assert.AreEqual(1, count); // only the first delivery counted
        }

        [Test]
        public void Unsubscribe_OneOfMany_LeavesOthers()
        {
            var bus = new EventBus();
            int a = 0, b = 0;
            void HandlerA(Ping _) => a++;
            void HandlerB(Ping _) => b++;

            bus.Subscribe<Ping>(HandlerA);
            bus.Subscribe<Ping>(HandlerB);
            bus.Unsubscribe<Ping>(HandlerA);
            bus.Publish(new Ping(1));

            Assert.AreEqual(0, a);
            Assert.AreEqual(1, b);
        }
    }
}
