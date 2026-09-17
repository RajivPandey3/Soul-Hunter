using System;
using System.Collections.Generic;
using SoulHunter.Core.Events;

namespace SoulHunter.Core.Events.Infrastructure
{
    /// <summary>
    /// Executes callbacks for published events.
    /// Responsible only for dispatching.
    /// </summary>
    internal static class EventDispatcher
    {
        public static void Dispatch<T>(T gameEvent)
            where T : IGameEvent
        {
            Type eventType = typeof(T);

            if (!EventRegistry.TryGet(eventType, out List<EventSubscriptionEntry> listeners))
                return;

            // Iterate backwards so listeners can safely unsubscribe during dispatch.
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                EventSubscriptionEntry entry = listeners[i];

                if (!entry.Enabled)
                    continue;

                if (entry.Callback is Action<T> callback)
                {
                    callback.Invoke(gameEvent);
                }
            }
        }
    }
}
