using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.UI;
using SoulHunter.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS-style accessibility in the real game: Options from the main menu, a working pause menu, controller
    /// pause, keyboard/controller focus on every screen, and the Options switches taking effect.
    /// Options are changed in memory only and the screen is destroyed without Close(), so nothing is saved.
    /// </summary>
    public class AccessibilityPlayModeTests : GameplayPlayModeTestBase
    {
        private static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest]
        public IEnumerator MainMenu_OptionsOpens_WithEveryOption_AndKeepsFocusInside()
        {
            var load = SceneManager.LoadSceneAsync("Bootstrap");
            Assert.IsNotNull(load);
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            yield return Frames(2);
            var nav = Object.FindFirstObjectByType<MainMenuNavigation>();
            Assert.IsNotNull(nav.OptionsButton, "Main menu has an Options button. " + Errors());
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(nav.StartButton.gameObject), "Start is focused for keyboard/controller.");

            Click(nav.OptionsButton, "Options");
            yield return null;
            var options = OptionsMenuUI.Current;
            Assert.IsNotNull(options, "Options screen opened. " + Errors());
            var names = options.GetComponentsInChildren<Selectable>().Select(s => s.name).ToList();
            foreach (var option in new[] { "Flashing VFX", "Damage Numbers", "Screen Shake", "Fullscreen", "Pause When Unfocused" })
                Assert.That(names, Does.Contain(option), $"Options has {option}.");
            Assert.That(options.GetComponentsInChildren<Slider>().Length, Is.EqualTo(2), "Sounds and Music sliders.");
            foreach (var binding in new[] { "Dash_<Keyboard>", "Dash_<Gamepad>", "Ultimate_<Gamepad>", "Pause_<Gamepad>", "Move_<Keyboard>" })
                Assert.That(names.Any(n => n.StartsWith(binding)), Is.True, $"Options can remap {binding}.");
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(options.transform), Is.True, "Focus is inside Options.");

            // Toggling a switch changes the option at once (in memory; the screen is not closed, so nothing is saved).
            var data = GameSettings.Data;
            bool before = data.DamageNumbers;
            try
            {
                var toggle = options.GetComponentsInChildren<Button>().First(b => b.name == "Damage Numbers");
                Click(toggle, "Damage Numbers");
                Assert.That(data.DamageNumbers, Is.EqualTo(!before));
                Assert.That(toggle.GetComponentInChildren<TMPro.TextMeshProUGUI>().text, Is.EqualTo(!before ? "ON" : "OFF"),
                    "State shown as text, not only colour.");
            }
            finally
            {
                data.DamageNumbers = before;
                Object.Destroy(options.gameObject);
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Gameplay_PauseMenu_ControllerPause_FocusLoss_AndMerchantShop()
        {
            yield return BootIntoGameplay();
            var system = GameSystemManager.Instance;
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner != null) spawner.enabled = false;

            // Pause menu: Resume / Options / Quit, Resume focused.
            system.TogglePause();
            yield return null;
            var pause = system.PauseMenuPanel.GetComponent<PauseMenuUI>();
            Assert.IsNotNull(pause, "The HUD attached the VS pause menu. " + Errors());
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(pause.ResumeButton.gameObject), "Resume is focused.");
            Assert.That(pause.OptionsButton.gameObject.activeInHierarchy && pause.QuitButton.gameObject.activeInHierarchy, Is.True);
            Click(pause.ResumeButton, "Resume");
            yield return null;
            Assert.That(system.IsPaused, Is.False, "Resume un-pauses. " + Errors());
            Assert.That(GameTime.IsPaused, Is.False);

            // A controller's Start button pauses.
            var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.Start));
                InputSystem.Update();
                yield return null;
                InputSystem.QueueStateEvent(pad, new GamepadState());
                InputSystem.Update();
                yield return null;
                Assert.That(system.IsPaused, Is.True, "Start on a controller pauses. " + Errors());
            }
            finally { InputSystem.RemoveDevice(pad); }
            system.Resume();
            yield return null;

            // Losing window focus pauses (option on by default).
            system.SendMessage("OnApplicationFocus", false);
            Assert.That(system.IsPaused, Is.True, "Alt-tab pauses the run.");
            system.Resume();
            yield return null;

            // The merchant shop is a focused uGUI screen now, not mouse-only IMGUI.
            var merchant = new GameObject("TestMerchant", typeof(SphereCollider)).AddComponent<WanderingMerchant>();
            merchant.OpenShop();
            yield return null;
            Assert.IsNotNull(merchant.ShopUI, "Shop screen exists. " + Errors());
            Assert.That(merchant.ShopUI.GetComponentsInChildren<Button>().Length, Is.EqualTo(5), "Four offers and Leave.");
            Assert.That(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(merchant.ShopUI.transform), Is.True,
                "A shop button is focused.");
            merchant.CloseShop();
            yield return null;
            Assert.IsNull(merchant.ShopUI);
            Assert.That(GameTime.IsPaused, Is.False);
            Object.Destroy(merchant.gameObject);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator OptionsSwitches_TurnOffDamageNumbersShakeAndFlashes()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList()) if (stray != null) Object.Destroy(stray.gameObject);
            yield return null;
            var data = GameSettings.Data;
            bool numbers = data.DamageNumbers, shake = data.ScreenShake, flashes = data.FlashingVfx;
            data.DamageNumbers = false; data.ScreenShake = false; data.FlashingVfx = false; // in memory only
            try
            {
                var prefabs = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs");
                var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
                var enemy = Object.Instantiate(prefabs[0], player.transform.position + new Vector3(0f, 0f, 30f), Quaternion.identity);
                yield return null;
                int popupsBefore = Object.FindObjectsByType<DamagePopup>(FindObjectsSortMode.None).Count(p => p.gameObject.activeInHierarchy);
                enemy.GetComponent<HealthController>().TakeDamage(new DamagePacket(1, enemy.transform.position, Vector3.zero));
                yield return null;
                int popupsAfter = Object.FindObjectsByType<DamagePopup>(FindObjectsSortMode.None).Count(p => p.gameObject.activeInHierarchy);
                Assert.That(popupsAfter, Is.EqualTo(popupsBefore), "Damage Numbers OFF shows no number.");
                var flash = enemy.GetComponent<SoulHunter.Gameplay.VFX.DamageFlash>();
                Assert.IsNotNull(flash, "Enemy has a hit flash to check.");
                var isFlashing = typeof(SoulHunter.Gameplay.VFX.DamageFlash).GetField("_isFlashing", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.That((bool)isFlashing.GetValue(flash), Is.False, "Flashing VFX OFF: no hit flash.");

                var shaker = Object.FindFirstObjectByType<SoulHunter.Gameplay.VFX.CameraShake>();
                Assert.IsNotNull(shaker, "Gameplay camera has CameraShake.");
                shaker.TriggerShake(0.5f, 1f);
                var shakeTime = typeof(SoulHunter.Gameplay.VFX.CameraShake).GetField("_shakeTime", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.That((float)shakeTime.GetValue(shaker), Is.LessThanOrEqualTo(0f), "Screen Shake OFF: no shake.");
                Object.Destroy(enemy);
            }
            finally { data.DamageNumbers = numbers; data.ScreenShake = shake; data.FlashingVfx = flashes; }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
