using NUnit.Framework;
using UnityEngine.InputSystem;
using SoulHunter.Core.Input;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>Bug hunt 2026-10-29: two modal screens over each other share the Pause key; the first to close kept giving it back.</summary>
    public class PauseKeyHoldsTests
    {
        [Test]
        public void ThePauseKey_StaysOff_UntilTheLastScreenLetsGo()
        {
            var pause = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            pause.Enable();
            object chest = new object(), levelUp = new object();
            try
            {
                PauseKeyHolds.Hold(levelUp, pause);
                PauseKeyHolds.Hold(chest, pause);
                Assert.That(pause.enabled, Is.False);
                PauseKeyHolds.Release(chest, pause); // the chest on top closes first
                Assert.That(pause.enabled, Is.False, "the level-up under it still holds the key");
                PauseKeyHolds.Release(levelUp, pause);
                Assert.That(pause.enabled, Is.True, "back as it was once the last one lets go");
                PauseKeyHolds.Release(levelUp, pause); // a second release changes nothing
                Assert.That(pause.enabled, Is.True);
            }
            finally { PauseKeyHolds.Release(chest, pause); PauseKeyHolds.Release(levelUp, pause); pause.Dispose(); }
        }
    }
}
