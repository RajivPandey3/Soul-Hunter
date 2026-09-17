using System;

namespace SoulHunter.Core.Events
{
    /// <summary>
    /// Represents an active event subscription.
    /// Disposing the subscription automatically removes the listener.
    /// </summary>
    public sealed class EventSubscription : IDisposable
    {
        private readonly int id;
        private readonly Type eventType;
        private bool disposed;
        private Action unsubscribe;

        internal EventSubscription(Action unsubscribe)
        {
            this.unsubscribe = unsubscribe;
        }

        internal EventSubscription(int id, Type eventType)
        {
            this.id = id;
            this.eventType = eventType;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            if (unsubscribe != null)
            {
                unsubscribe();
                unsubscribe = null;
            }
            else if (Infrastructure.EventRegistry.TryGet(eventType, out var entries))
            {
                // Retain compatibility with the legacy registry without a no-op Dispose.
                entries.RemoveAll(entry => entry.Id == id);
                if (entries.Count == 0) Infrastructure.EventRegistry.Remove(eventType);
            }
        }
    }
}
