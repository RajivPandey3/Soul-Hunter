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
            // VS sections in the wiki's order (Favorites, Display, Sound, Gameplay), then Soul Hunter's Controls.
            Assert.That(options.TabButtons.Select(t => t.name), Is.EqualTo(new[] { "Tab_Favorites", "Tab_Display", "Tab_Sound", "Tab_Gameplay", "Tab_Controls", "Tab_Save Data" }));
            Assert.That(options.CurrentTab, Is.EqualTo(0), "Favorites opens first.");
            var names = options.GetComponentsInChildren<Selectable>(true).Select(s => s.name).ToList();
            foreach (var option in new[] { "Window Mode", "Resolution", "Screen Refresh Rate", "V-Sync", "Flashing VFX", "Disable Blood", "Screen Shake",
                                           "Damage Numbers", "Show Stage Progression", "Visible Joystick", "Joystick Type", "Pause When Unfocused", "Reset Controls" })
                Assert.That(names, Does.Contain(option), $"Options has {option}.");
            Assert.That(options.GetComponentsInChildren<Slider>(true).Length, Is.EqualTo(4), "Sounds and Music sliders, in Sound and their Favorites copies.");
            options.ShowTab(3); // Gameplay
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Damage Numbers"), "Opening a section focuses its first option.");
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
            Assert.That(merchant.ShopUI.GetComponentsInChildren<Button>().Length, Is.EqualTo(4), "Two weapons, the Soul Egg and Leave.");
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
        public IEnumerator Hud_StageProgression_ReadableLabels_DisableBlood_AndTouchControls()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner != null) spawner.enabled = false;
            var data = GameSettings.Data;
            bool progression = data.ShowStageProgression, blood = data.DisableBlood, joystick = data.VisibleJoystick;
            try
            {
                // Stage progression label (VS "Show stage progression").
                var hud = Object.FindFirstObjectByType<HudReadability>();
                Assert.IsNotNull(hud, "HUD readability helper attached. " + Errors());
                Assert.That(hud.StageLabel.text, Does.StartWith("Stage 1/10"));
                Assert.That(hud.StageLabel.gameObject.activeInHierarchy, Is.True);
                data.ShowStageProgression = false; GameSettings.NotifyChanged();
                Assert.That(hud.StageLabel.gameObject.activeSelf, Is.False, "Option off hides it.");
                data.ShowStageProgression = true; GameSettings.NotifyChanged();

                // HUD labels are white (they were dark blue on the dark ground).
                var hudController = Object.FindFirstObjectByType<GameHUDController>();
                foreach (var name in new[] { "_goldText", "_killText" })
                {
                    var label = GetPrivateField<TMPro.TextMeshProUGUI>(hudController, name);
                    Assert.That(label.color, Is.EqualTo(Color.white), $"{name} is readable.");
                }

                // Disable Blood: Blood Arenas floor stains use the plain floor material.
                LevelProgressionManager.Instance.StartStage(8);
                yield return null;
                var map = Object.FindFirstObjectByType<SoulHunter.Gameplay.Environment.InfiniteMap>();
                bool HasBlood() => map.ChunkAt(1, 1).GetComponentsInChildren<Renderer>()
                    .SelectMany(r => r.sharedMaterials).Any(m => m != null && m.name.EndsWith("_Blood"));
                Assert.That(HasBlood(), Is.True, "Blood Arenas floor shows stains by default.");
                data.DisableBlood = true; GameSettings.NotifyChanged();
                Assert.That(HasBlood(), Is.False, "Disable Blood removes them.");
                data.DisableBlood = false; GameSettings.NotifyChanged();
                Assert.That(HasBlood(), Is.True, "and turning it back on restores them.");

                // Visible Joystick: on-screen stick and buttons drive the virtual gamepad.
                var touch = Object.FindFirstObjectByType<TouchControlsUI>();
                Assert.IsNotNull(touch, "Touch controls attached. " + Errors());
                Assert.That(touch.Root.activeSelf, Is.EqualTo(Application.isMobilePlatform), "Hidden on desktop by default.");
                data.VisibleJoystick = true; GameSettings.NotifyChanged();
                yield return null;
                Assert.That(touch.Root.activeSelf, Is.True);
                Assert.IsNotNull(touch.Root.GetComponentInChildren<UnityEngine.InputSystem.OnScreen.OnScreenStick>());
                Assert.That(touch.Root.GetComponentsInChildren<UnityEngine.InputSystem.OnScreen.OnScreenButton>().Length, Is.EqualTo(2), "Dash and Pause.");
            }
            finally
            {
                data.ShowStageProgression = progression; data.DisableBlood = blood; data.VisibleJoystick = joystick;
                GameSettings.NotifyChanged();
            }
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

        // ---------------------------------------------------------------- VS Options: Favorites, refresh rate, joystick type

        private IEnumerator OpenOptionsFromTheMainMenu()
        {
            var load = SceneManager.LoadSceneAsync("Bootstrap");
            Assert.IsNotNull(load);
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            yield return Frames(2);
            var nav = Object.FindFirstObjectByType<MainMenuNavigation>();
            Click(nav.OptionsButton, "Options");
            yield return null;
            Assert.IsNotNull(OptionsMenuUI.Current, "Options screen opened. " + Errors());
        }

        /// <summary>A section (tab page) of the Options screen by name.</summary>
        private static Transform Section(OptionsMenuUI options, string name) =>
            options.GetComponentsInChildren<RectTransform>(true).First(r => r.name == name && r.parent != null && r.parent.name == "Panel");

        private static Button RowButton(Transform section, string row) =>
            section.GetComponentsInChildren<Button>(true).First(b => b.name == row);

        private static string State(Button button) => button.GetComponentInChildren<TMPro.TextMeshProUGUI>().text;

        [UnityTest]
        public IEnumerator Favorites_IsTheFirstSection_AndItsCopiesShareTheSettings()
        {
            yield return OpenOptionsFromTheMainMenu();
            var options = OptionsMenuUI.Current;
            var data = GameSettings.Data;
            bool numbers = data.DamageNumbers, flashes = data.FlashingVfx;
            float sfx = data.SfxVolume;
            try
            {
                // VS: "The Favorites section contains a copy of some commonly changed options from other sections."
                var favorites = Section(options, "Favorites");
                Assert.That(favorites.gameObject.activeInHierarchy, Is.True, "Favorites is the open section.");
                var rows = favorites.GetComponentsInChildren<Selectable>(true).Select(s => s.transform.parent.name).ToList();
                Assert.That(rows, Is.EqualTo(new[] { "Sounds", "Music", "Damage Numbers", "Flashing VFX" }),
                    "The VS Favorites Soul Hunter has (Language and Classic Sound Effects have no counterpart).");
                yield return null;
                Assert.That(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(favorites), Is.True, "Its first option is focused.");

                // A switch in Favorites changes the one setting, and the Gameplay copy shows it.
                Click(RowButton(favorites, "Damage Numbers"), "Favorites Damage Numbers");
                Assert.That(data.DamageNumbers, Is.EqualTo(!numbers));
                options.ShowTab(3); // Gameplay
                yield return null;
                Assert.That(State(RowButton(Section(options, "Gameplay"), "Damage Numbers")), Is.EqualTo(!numbers ? "ON" : "OFF"),
                    "Gameplay's Damage Numbers shows the change made in Favorites.");

                // The other way: Flashing VFX in Display, seen in Favorites.
                options.ShowTab(1);
                yield return null;
                Click(RowButton(Section(options, "Display"), "Flashing VFX"), "Display Flashing VFX");
                Assert.That(data.FlashingVfx, Is.EqualTo(!flashes));
                options.ShowTab(0);
                yield return null;
                Assert.That(State(RowButton(favorites, "Flashing VFX")), Is.EqualTo(!flashes ? "ON" : "OFF"),
                    "Favorites' Flashing VFX shows the change made in Display.");

                // Sliders: the Sounds copy in Favorites moves the Sound section's slider.
                float target = sfx > 0.5f ? 0.2f : 0.8f;
                favorites.GetComponentsInChildren<Slider>(true).First(sl => sl.transform.parent.name == "Sounds").value = target;
                Assert.That(data.SfxVolume, Is.EqualTo(target).Within(0.001f));
                options.ShowTab(2); // Sound
                yield return null;
                var soundSlider = Section(options, "Sound").GetComponentsInChildren<Slider>(true).First(sl => sl.transform.parent.name == "Sounds");
                Assert.That(soundSlider.value, Is.EqualTo(target).Within(0.001f), "The Sound section's slider shows the Favorites change.");
            }
            finally
            {
                data.DamageNumbers = numbers; data.FlashingVfx = flashes; data.SfxVolume = sfx;
                GameSettings.NotifyChanged();
                Object.Destroy(options.gameObject); // not Close(): nothing is saved
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator ScreenRefreshRate_ShowsOnlyInExclusiveFullscreen_AndGoesIntoTheSave()
        {
            yield return OpenOptionsFromTheMainMenu();
            var options = OptionsMenuUI.Current;
            var data = GameSettings.Data;
            int mode = data.WindowMode, hz = data.RefreshRateHz;
            try
            {
                data.WindowMode = 0; // Fullscreen Window
                options.ShowTab(1); // Display (ShowTab refreshes every row)
                yield return null;
                var display = Section(options, "Display");
                var refresh = RowButton(display, "Screen Refresh Rate");
                var windowMode = RowButton(display, "Window Mode");
                Assert.That(refresh.gameObject.activeInHierarchy, Is.False, "Hidden in Fullscreen Window (VS: Exclusive Fullscreen only).");
                Assert.That(Selectable.allSelectablesArray, Has.No.Member(refresh), "A hidden row cannot be focused.");
                float resolutionY = RowButton(display, "Resolution").transform.parent.position.y;
                float vsyncHidden = RowButton(display, "V-Sync").transform.parent.position.y;

                // Cycle Window Mode to Exclusive Fullscreen (1), as a player would.
                Click(windowMode, "Window Mode");
                yield return null;
                Assert.That(data.WindowMode, Is.EqualTo(1));
                bool shown = !Application.isMobilePlatform;
                Assert.That(refresh.gameObject.activeInHierarchy, Is.EqualTo(shown), "Shown in Exclusive Fullscreen on PC.");
                if (shown)
                {
                    Canvas.ForceUpdateCanvases();
                    Assert.That(refresh.transform.parent.position.y, Is.LessThan(resolutionY), "Right under Resolution.");
                    Assert.That(RowButton(display, "V-Sync").transform.parent.position.y, Is.LessThan(vsyncHidden - 1f), "The rows below make room for it.");
                    Assert.That(Selectable.allSelectablesArray, Does.Contain(refresh), "It can be focused now.");

                    // Its choices: Default, then the display's rates for the chosen size; the choice goes to GameData.
                    var rates = DisplaySettingsApplier.RefreshRates(DisplaySettingsApplier.ChosenSize());
                    data.RefreshRateHz = 0;
                    options.ShowTab(1);
                    Assert.That(State(refresh), Is.EqualTo("Default"));
                    Click(refresh, "Screen Refresh Rate");
                    int expected = rates.Count > 0 ? rates[0] : 0;
                    Assert.That(data.RefreshRateHz, Is.EqualTo(expected), "The first rate after Default (or Default when the display lists none).");
                    Assert.That(State(refresh), Is.EqualTo(DisplaySettingsApplier.RefreshRateLabel(expected)));
                }

                // Leaving Exclusive Fullscreen hides it again and keeps the stored rate.
                int kept = data.RefreshRateHz;
                Click(windowMode, "Window Mode");
                yield return null;
                Assert.That(data.WindowMode, Is.EqualTo(2));
                Assert.That(refresh.gameObject.activeInHierarchy, Is.False, "Hidden again in Maximised Window.");
                Assert.That(data.RefreshRateHz, Is.EqualTo(kept), "The rate is kept for the next time Exclusive Fullscreen is chosen.");
            }
            finally
            {
                data.WindowMode = mode; data.RefreshRateHz = hz;
                Object.Destroy(options.gameObject); // not Close(): nothing is saved
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator JoystickType_LegacyIsTheFixedStick_DefaultFloatsToTheThumb()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner != null) spawner.enabled = false;
            var data = GameSettings.Data;
            bool joystick = data.VisibleJoystick;
            int type = data.JoystickType;
            try
            {
                data.VisibleJoystick = true;
                data.JoystickType = GameSettings.JoystickDefault;
                GameSettings.NotifyChanged();
                yield return Frames(2);
                var touch = Object.FindFirstObjectByType<TouchControlsUI>();
                Assert.IsNotNull(touch, "Touch controls attached. " + Errors());
                Assert.That(touch.IsFloating, Is.True);
                Assert.That(touch.Zone.gameObject.activeInHierarchy, Is.True, "Default: the floating stick's area takes presses.");
                var knob = touch.StickBase.GetComponentInChildren<UnityEngine.InputSystem.OnScreen.OnScreenStick>();
                Assert.That(knob.GetComponent<Image>().raycastTarget, Is.False, "and the stick itself does not.");
                Vector2 home = touch.StickBase.anchoredPosition;
                Vector2 homeScreen = RectTransformUtility.WorldToScreenPoint(null, touch.StickBase.position);

                // A thumb lands in the left part of the screen, away from the stick's resting place.
                var press = new Vector2(Screen.width * 0.3f, Screen.height * 0.5f);
                var e = new PointerEventData(EventSystem.current) { position = press, pressPosition = press };
                touch.Zone.OnPointerDown(e);
                Vector2 baseScreen = RectTransformUtility.WorldToScreenPoint(null, touch.StickBase.position);
                Assert.That(Vector2.Distance(baseScreen, press), Is.LessThan(2f), $"Default: the stick jumps to the thumb ({baseScreen} vs {press}).");
                Assert.That(Vector2.Distance(baseScreen, homeScreen), Is.GreaterThan(20f), "It left its resting place.");
                var knobRect = (RectTransform)knob.transform;
                Vector2 knobBefore = knobRect.anchoredPosition;
                e.position = press + new Vector2(40f, 0f);
                touch.Zone.OnDrag(e);
                Assert.That(knobRect.anchoredPosition.x, Is.GreaterThan(knobBefore.x + 1f), "Dragging right moves the knob right.");
                touch.Zone.OnPointerUp(e);
                Assert.That(Vector2.Distance(touch.StickBase.anchoredPosition, home), Is.LessThan(0.01f), "Released: back to its resting place.");

                // Legacy: the fixed stick of before; nothing floats.
                data.JoystickType = GameSettings.JoystickLegacy;
                GameSettings.NotifyChanged();
                yield return null;
                Assert.That(touch.IsFloating, Is.False);
                Assert.That(touch.Zone.gameObject.activeInHierarchy, Is.False, "Legacy: no floating area.");
                Assert.That(knob.GetComponent<Image>().raycastTarget, Is.True, "Legacy: the stick takes the press where it is.");
                Assert.That(Vector2.Distance(touch.StickBase.anchoredPosition, home), Is.LessThan(0.01f), "Legacy: the stick stays put.");

                // The Gameplay tab's Joystick Type cycles Legacy / Default into GameData, and the stick follows at once.
                var options = OptionsMenuUI.Open();
                try
                {
                    options.ShowTab(3);
                    yield return null;
                    var cycle = RowButton(Section(options, "Gameplay"), "Joystick Type");
                    Assert.That(State(cycle), Is.EqualTo("Legacy"));
                    Click(cycle, "Joystick Type");
                    Assert.That(data.JoystickType, Is.EqualTo(GameSettings.JoystickDefault));
                    Assert.That(State(cycle), Is.EqualTo("Default"));
                    Assert.That(touch.Zone.gameObject.activeInHierarchy, Is.True, "The stick follows the option at once.");
                }
                finally { Object.Destroy(options.gameObject); } // not Close(): nothing is saved
            }
            finally
            {
                data.VisibleJoystick = joystick; data.JoystickType = type;
                GameSettings.NotifyChanged();
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
