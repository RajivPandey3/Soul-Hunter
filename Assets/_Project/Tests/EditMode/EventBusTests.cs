using NUnit.Framework;
using SoulHunter.Core.Events;

namespace SoulHunter.Tests.EditMode
{
    public class EventBusTests
    {
        private sealed class TestEvent : IGameEvent { }

        [Test]
        public void Raise_InvokesActiveSubscriberWithRaisedEvent()
        {
            using (var bus = new EventBus())
            {
                var raisedEvent = new TestEvent();
                TestEvent receivedEvent = null;
                var callCount = 0;
                using (bus.Subscribe<TestEvent>(eventData =>
                {
                    receivedEvent = eventData;
                    callCount++;
                }))
                {
                    bus.Raise(raisedEvent);

                    Assert.That(callCount, Is.EqualTo(1));
                    Assert.That(receivedEvent, Is.SameAs(raisedEvent));
                }
            }
        }

        [Test]
        public void Dispose_SubscriptionPreventsSubsequentDelivery()
        {
            using (var bus = new EventBus())
            {
                var callCount = 0;
                using (EventSubscription subscription = bus.Subscribe<TestEvent>(_ => callCount++))
                {
                    bus.Raise(new TestEvent());
                    Assert.That(callCount, Is.EqualTo(1));

                    subscription.Dispose();
                    bus.Raise(new TestEvent());
                    bus.Raise(new TestEvent());

                    Assert.That(callCount, Is.EqualTo(1),
                        "Disposed subscriptions must no longer receive events.");
                }
            }
        }
    }
}
