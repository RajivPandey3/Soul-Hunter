using NUnit.Framework;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Core;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: GameTimeTests verifies that one screen closing (or the pause menu toggling,
    /// Soul Burst's slow motion ending, or the next stage starting) never resumes the game while
    /// another screen still holds a pause. Before GameTime, each of them wrote Time.timeScale = 1.
    /// </summary>
    public class GameTimeTests
    {
        private GameObject _object;

        [SetUp]
        public void SetUp()
        {
            GameTime.ResetAll();
            _object = new GameObject("GameTimeTest");
            // WanderingMerchant requires a concrete Collider in EditMode.
            _object.AddComponent<BoxCollider>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_object);
            GameTime.ResetAll();
        }

        [Test]
        public void Pause_HoldsUntilEveryOwnerResumes()
        {
            var levelUp = new object();
            var chest = new object();

            GameTime.Pause(levelUp);
            GameTime.Pause(chest);
            GameTime.Resume(chest);
            Assert.That(Time.timeScale, Is.EqualTo(0f), "Level-up is still open.");

            GameTime.Resume(levelUp);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void SlowMotionEnding_DoesNotResumeAPausedGame()
        {
            var levelUp = new object();
            GameTime.SetSlowMotion(0.1f);
            Assert.That(Time.timeScale, Is.EqualTo(0.1f));

            GameTime.Pause(levelUp);
            GameTime.SetSlowMotion(1f);
            Assert.That(Time.timeScale, Is.EqualTo(0f), "Soul Burst ending must not unpause the level-up screen.");

            GameTime.Resume(levelUp);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void DestroyedOwner_NoLongerHoldsThePause()
        {
            var owner = new GameObject("DestroyedScreen");
            GameTime.Pause(owner);
            Object.DestroyImmediate(owner);

            Assert.That(GameTime.IsPaused, Is.False);
        }

        [Test]
        public void PauseMenuToggledTwice_DuringMerchantShop_KeepsGamePaused()
        {
            var merchant = _object.AddComponent<WanderingMerchant>();
            var system = _object.AddComponent<GameSystemManager>();
            merchant.OpenShop();

            system.TogglePause();
            system.TogglePause();

            Assert.That(Time.timeScale, Is.EqualTo(0f), "Closing the pause menu must not resume the game under the shop.");

            merchant.CloseShop();
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void NextStageStarting_DuringMerchantShop_KeepsGamePaused()
        {
            var merchant = _object.AddComponent<WanderingMerchant>();
            var progression = _object.AddComponent<LevelProgressionManager>();
            merchant.OpenShop();

            progression.StartStage(2);

            Assert.That(Time.timeScale, Is.EqualTo(0f), "A stage starting must not resume the game under the shop.");
        }
    }
}
