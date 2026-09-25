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
            Assert.That(data.FlashingVfx && data.DamageNumbers && data.ScreenShake && data.Fullscreen && data.PauseOnFocusLoss, Is.True);
            Assert.That(data.InputBindingOverrides, Is.Empty);
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
