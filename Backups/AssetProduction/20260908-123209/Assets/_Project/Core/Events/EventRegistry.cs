using System;
using System.Collections.Generic;

namespace SoulHunter.Core.Events.Infrastructure
{
    /// <summary>
    /// Stores all event subscriptions.
    /// Responsible only for listener storage.
    /// </summary>
    internal static class EventRegistry
    {
        private static readonly Dictionary<Type, List<EventSubscriptionEntry>>
            registry = new();

        public static List<EventSubscriptionEntry> GetOrCreate(Type eventType)
        {
            if (!registry.TryGetValue(eventType, out List<EventSubscriptionEntry> list))
            {
                list = new List<EventSubscriptionEntry>();
                registry.Add(eventType, list);
            }

            return list;
        }

        public static bool TryGet(
            Type eventType,
            out List<EventSubscriptionEntry> listeners)
        {
            return registry.TryGetValue(eventType, out listeners);
        }

        public static void Remove(Type eventType)
        {
            registry.Remove(eventType);
        }

        public static void Clear()
        {
            registry.Clear();
        }
    }
}
