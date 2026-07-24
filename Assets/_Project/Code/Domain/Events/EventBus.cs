using System;
using System.Collections.Generic;

namespace STSlike.Domain.Events
{
    /// <summary>
    /// Default <see cref="IEventBus"/>: keeps one multicast delegate per event
    /// type. Publishing invokes a snapshot of the delegate, so handlers may
    /// safely subscribe/unsubscribe while an event is being delivered.
    /// </summary>
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

        public void Subscribe<T>(Action<T> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Type type = typeof(T);
            _handlers.TryGetValue(type, out Delegate existing);
            _handlers[type] = Delegate.Combine(existing, handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null)
                return;

            Type type = typeof(T);
            if (!_handlers.TryGetValue(type, out Delegate existing))
                return;

            Delegate remaining = Delegate.Remove(existing, handler);
            if (remaining == null)
                _handlers.Remove(type);
            else
                _handlers[type] = remaining;
        }

        public void Publish<T>(T @event)
        {
            if (_handlers.TryGetValue(typeof(T), out Delegate existing) && existing is Action<T> action)
                action(@event);
        }
    }
}
