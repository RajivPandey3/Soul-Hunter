using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using SoulHunter.Core.Events;
using SoulHunter.Core.Input;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: AccessibilityTests covers the VS-style options and controls: safe defaults (also for
    /// old saves), controller bindings for every action, and remapped controls saving and loading.
    /// </summary>
    public class AccessibilityTests
    {
        private InputService _input;

        [TearDown]
        public void TearDown()
        {
            _input?.Cleanup();
            _input = null;
        }

        private InputService NewInput()
        {
            _input = new InputService(new EventBus());
            _input.Initialize();
            return _input;
        }

        [Test]
        public void OldSave_WithoutOptions_KeepsSafeDefaults()
        {
            // Learning Comment: Purani save mein options na hon to sab kuch ON aur poori volume par rahe.
            var data = new GameData();
            JsonUtility.FromJsonOverwrite("{\"Currency\":5,\"MasterVolume\":0.7}", data);
            Assert.That(data.Currency, Is.EqualTo(5));
            Assert.That(data.MusicVolume, Is.EqualTo(1f));
            Assert.That(data.SfxVolume, Is.EqualTo(1f));
            Assert.That(data.FlashingVfx && data.DamageNumbers && data.ScreenShake && data.PauseOnFocusLoss && data.VSync && data.ShowStageProgression, Is.True);
            Assert.That(data.InputBindingOverrides, Is.Empty);
            Assert.That(data.WindowMode, Is.EqualTo(0), "Fullscreen Window by default.");
            Assert.That(data.ResolutionIndex, Is.EqualTo(-1), "Native resolution by default.");
            Assert.That(data.DisableBlood || data.VisibleJoystick, Is.False);
        }

        [Test]
        public void RefreshRateAndJoystickType_Default_AndAnOldSaveLoadsTheDefaults()
        {
            // VS Options: Screen Refresh Rate (Display) starts at Default; Joystick Type (Gameplay) starts at Default.
            var fresh = new GameData();
            Assert.That(fresh.RefreshRateHz, Is.EqualTo(0), "Default refresh rate (the display picks).");
            Assert.That(fresh.JoystickType, Is.EqualTo(GameSettings.JoystickDefault), "Joystick Type Default.");
            var old = new GameData();
            JsonUtility.FromJsonOverwrite("{\"Currency\":5,\"WindowMode\":1}", old);
            Assert.That(old.RefreshRateHz, Is.EqualTo(0));
            Assert.That(old.JoystickType, Is.EqualTo(GameSettings.JoystickDefault));
            Assert.That(GameSettings.JoystickTypeNames, Is.EqualTo(new[] { "Legacy", "Default" }), "The wiki's two values, in its order.");
        }

        [Test]
        public void RefreshRateAndJoystickType_AreSaved_AndRepaired()
        {
            var data = new GameData { SaveVersion = GameData.CurrentVersion, RefreshRateHz = 144, JoystickType = GameSettings.JoystickLegacy };
            var loaded = new GameData();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(data), loaded);
            SaveService.Migrate(loaded);
            Assert.That(loaded.RefreshRateHz, Is.EqualTo(144), "A chosen rate survives a save and the repair.");
            Assert.That(loaded.JoystickType, Is.EqualTo(GameSettings.JoystickLegacy));

            var broken = new GameData { SaveVersion = GameData.CurrentVersion, RefreshRateHz = -5, JoystickType = 7 };
            SaveService.Migrate(broken);
            Assert.That(broken.RefreshRateHz, Is.EqualTo(0), "An impossible rate goes back to Default.");
            Assert.That(broken.JoystickType, Is.EqualTo(GameSettings.JoystickDefault), "An unknown type goes back to Default.");
        }

        [Test]
        public void RefreshRates_AreTheDisplaysRatesForTheSize_InWholeHertz_LowestFirst()
        {
            var size = new Vector2Int(1920, 1080);
            var modes = new[]
            {
                (size, 144.0), (size, 59.94), (size, 60.0), (new Vector2Int(1280, 720), 75.0), (size, 120.0), (size, 0.0)
            };
            Assert.That(SoulHunter.Gameplay.Core.DisplaySettingsApplier.RefreshRates(modes, size), Is.EqualTo(new[] { 60, 120, 144 }),
                "59.94 reads as 60; other sizes and empty rates are left out.");
            Assert.That(SoulHunter.Gameplay.Core.DisplaySettingsApplier.RefreshRateLabel(0), Is.EqualTo("Default"));
            Assert.That(SoulHunter.Gameplay.Core.DisplaySettingsApplier.RefreshRateLabel(144), Is.EqualTo("144 Hz"));
            // VS: "Only visible when using Exclusive Fullscreen Window Mode" (Window Mode 1), and PC-exclusive.
            for (int mode = 0; mode < 4; mode++)
                Assert.That(SoulHunter.Gameplay.Core.DisplaySettingsApplier.ShowsRefreshRate(mode, false), Is.EqualTo(mode == 1), $"Window Mode {mode}");
            Assert.That(SoulHunter.Gameplay.Core.DisplaySettingsApplier.ShowsRefreshRate(1, true), Is.False, "Not on mobile.");
        }

        [Test]
        public void EveryAction_HasKeyboardAndControllerBindings()
        {
            // Learning Comment: VS ki tarah poora game controller se khelne layak ho (pehle Dash/Ultimate/Pause ka koi button nahi tha).
            var map = NewInput().Actions;
            string Paths(string action) => string.Join(",", map.FindAction(action).bindings.Select(b => b.path));
            StringAssert.Contains("<Gamepad>/leftStick", Paths(InputService.MoveAction));
            StringAssert.Contains("<Gamepad>/dpad", Paths(InputService.MoveAction));
            StringAssert.Contains("<Gamepad>/buttonSouth", Paths(InputService.DashAction));
            StringAssert.Contains("<Gamepad>/buttonNorth", Paths(InputService.UltimateAction));
            StringAssert.Contains("<Gamepad>/start", Paths(InputService.PauseAction));
            foreach (var name in new[] { InputService.DashAction, InputService.UltimateAction, InputService.PauseAction })
            {
                Assert.That(OptionsMenuUI.BindingIndex(map.FindAction(name), "<Keyboard>"), Is.GreaterThanOrEqualTo(0), name);
                Assert.That(OptionsMenuUI.BindingIndex(map.FindAction(name), "<Gamepad>"), Is.GreaterThanOrEqualTo(0), name);
            }
            var move = map.FindAction(InputService.MoveAction);
            foreach (var part in new[] { "up", "down", "left", "right" })
                Assert.That(move.bindings[OptionsMenuUI.CompositePart(move, part)].path, Does.StartWith("<Keyboard>/"), part);
            Assert.That(move.bindings[OptionsMenuUI.CompositePart(move, "up")].path, Is.EqualTo("<Keyboard>/w"), "Remapping targets the WASD set.");
        }

        [Test]
        public void RemappedControls_SaveAndLoad_AndReset()
        {
            // Learning Comment: Badli hui keys save hon, agli baar wapas lagen, aur Reset se default ho jayen.
            var dash = NewInput().Actions.FindAction(InputService.DashAction);
            int keyboard = OptionsMenuUI.BindingIndex(dash, "<Keyboard>");
            dash.ApplyBindingOverride(keyboard, "<Keyboard>/k");
            string json = _input.SaveBindingOverrides();
            Assert.That(json, Does.Contain("<Keyboard>/k"));
            _input.Cleanup();

            var reloaded = NewInput().Actions.FindAction(InputService.DashAction);
            Assert.That(reloaded.bindings[keyboard].effectivePath, Is.EqualTo("<Keyboard>/space"), "A fresh service starts with defaults.");
            _input.LoadBindingOverrides(json);
            Assert.That(reloaded.bindings[keyboard].effectivePath, Is.EqualTo("<Keyboard>/k"));
            _input.ResetBindings();
            Assert.That(reloaded.bindings[keyboard].effectivePath, Is.EqualTo("<Keyboard>/space"));
            _input.LoadBindingOverrides("not json");
            Assert.That(reloaded.bindings[keyboard].effectivePath, Is.EqualTo("<Keyboard>/space"), "A broken saved value keeps the defaults.");
        }
    }
}
