using System;

namespace SoulHunter.Core.Events.Infrastructure
{
    /// <summary>
    /// Internal record representing a single event subscription.
    /// Used only by the Event Framework.
    /// </summary>
    public sealed class EventSubscriptionEntry
    {
        public int Id { get; }
        public Delegate Callback { get; }
        public bool Enabled { get; set; }
        public int Priority { get; }

        public EventSubscriptionEntry(
            int id,
            Delegate callback,
            int priority = 0)
        {
            Id = id;
            Callback = callback;
            Priority = priority;
            Enabled = true;
        }
    }
}
