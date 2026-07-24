using System;

namespace STSlike.Domain.Events
{
    /// <summary>
    /// A typed publish/subscribe hub (Observer). Publishers raise event
    /// instances; any number of subscribers react to a given event type
    /// without the publisher knowing they exist. This is the decoupling seam
    /// character-unique mechanics and the UI hook into.
    /// </summary>
    public interface IEventBus
    {
        void Subscribe<T>(Action<T> handler);
        void Unsubscribe<T>(Action<T> handler);
        void Publish<T>(T @event);
    }
}
