using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner (2026-10-10): the game plays in landscape and portrait from the same screens. The Omens wheel and Blind Fate's
    /// pick are laid out on a PC (16:9, 21:9, 5:4) and on the owner's phone held either way (1080x2400 at 480 dpi, touch)
    /// with the touch controls shown: each stays on the canvas and clear of the stick, Dash and Pause pads, and its text
    /// never goes under the touch floor (12 sp). Before, the wheel was lifted to 30% of the height, which still covered
    /// Dash on a phone held sideways.
    /// </summary>
    public class HudOverlaysResponsivePlayModeTests : GameplayPlayModeTestBase
    {
        private IEnumerator Boot(System.Action<SoulHunter.Core.Persistence.GameData> configure)
        {
            yield return BootIntoGameplay(save => { save.VisibleJoystick = true; configure?.Invoke(save); });
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            // No kills: no level-up screen pauses the game (and hides the pads) mid-test.
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            yield return CloseOpenLevelUps();
            Assert.That(TouchControlsUI.ShouldShow, Is.True, "Visible Joystick shows the touch controls.");
        }

        private static List<(string Name, Rect Box)> Pads()
        {
            var touch = Object.FindFirstObjectByType<TouchControlsUI>();
            Assert.IsNotNull(touch, "The HUD has touch controls.");
            var pads = new List<(string, Rect)>();
            foreach (Transform child in touch.Root.transform)
                if (child.name != "StickZone") pads.Add((child.name, ScreenSimulation.UnitsRect((RectTransform)child)));
            Assert.That(pads.Select(p => p.Item1), Is.SupersetOf(new[] { "StickBase", "DashButton", "PauseButton" }), "The stick, Dash and Pause pads.");
            return pads;
        }

        private static void Check(string what, RectTransform overlay, IEnumerable<TMP_Text> texts, List<string> problems)
        {
            Rect box = ScreenSimulation.UnitsRect(overlay);
            Vector2 canvas = ((RectTransform)overlay.GetComponentInParent<Canvas>().rootCanvas.transform).rect.size;
            if (box.xMin < -0.5f || box.yMin < -0.5f || box.xMax > canvas.x + 0.5f || box.yMax > canvas.y + 0.5f)
                problems.Add($"{what}: {box} is off the {canvas} canvas.");
            foreach (var (name, pad) in Pads())
                if (box.Overlaps(pad)) problems.Add($"{what}: {box} covers the {name} pad {pad}.");
            foreach (var text in texts)
            {
                if (text == null || !text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text)) continue;
                text.ForceMeshUpdate();
                if (text.fontSize < UiScreen.MinTextUnits - 0.01f)
                    problems.Add($"{what}: '{text.text}' is {text.fontSize:0.#} units, under the {UiScreen.MinTextUnits:0.#} floor.");
            }
        }

        [UnityTest]
        public IEnumerator TheOmensWheel_StaysClearOfTheTouchPads_OnEveryScreen()
        {
            yield return Boot(save => save.ModeRandomEvents = true);
            var wheel = Object.FindFirstObjectByType<OmenRouletteUI>();
            Assert.IsNotNull(wheel, "The HUD has the Omens wheel. " + Errors());
            yield return WaitUntilRealtime(() => wheel.IsShowing, "the wheel shows while Omens is on");
            var canvas = wheel.Root.GetComponentInParent<Canvas>().rootCanvas;
            var problems = new List<string>();
            var places = new Dictionary<string, Rect>();
            try
            {
                foreach (var d in ScreenSimulation.Devices)
                {
                    yield return ScreenSimulation.At(d.Pixels, d.Dpi, d.Touch, canvas);
                    yield return null; // the wheel places itself every frame
                    Canvas.ForceUpdateCanvases();
                    Check($"Omens wheel on {d.Name}", wheel.Root, wheel.Texts, problems);
                    places[d.Name] = ScreenSimulation.UnitsRect(wheel.Root);
                }
            }
            finally { ScreenSimulation.Restore(); }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            Assert.That(places["PC 1920x1080"].size.x, Is.EqualTo(OmenRouletteUI.Size.x).Within(0.5f), "It keeps its size.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator BlindFatesPick_StaysClearOfTheTouchPads_OnEveryScreen()
        {
            yield return Boot(save => save.ModeRandomLevelUp = true);
            var popup = Object.FindFirstObjectByType<BlindFatePopup>();
            Assert.IsNotNull(popup, "The HUD shows Blind Fate's pick. " + Errors());
            var pick = WeaponDefinitionLookup.Find(UpgradeData.UpgradeType.Whip).GetLevel(2);
            Assert.IsNotNull(pick);
            var canvas = popup.Root.GetComponentInParent<Canvas>().rootCanvas;
            var root = (RectTransform)canvas.transform;
            var label = popup.Root.GetComponentInChildren<TMP_Text>(true);
            var problems = new List<string>();
            try
            {
                foreach (var d in ScreenSimulation.Devices)
                {
                    yield return ScreenSimulation.At(d.Pixels, d.Dpi, d.Touch, canvas);
                    popup.Show(pick);
                    yield return null; // over the hero
                    Canvas.ForceUpdateCanvases();
                    Assert.That(popup.IsShowing, Is.True, $"{d.Name}: the pick shows.");
                    Check($"Blind Fate over the hero on {d.Name}", popup.Root, new[] { label }, problems);
                    // Asked to sit on each pad (a hero walking into a corner): it moves to the nearest free spot.
                    foreach (var (name, pad) in Pads())
                    {
                        Vector2 centre = pad.center - root.rect.size * 0.5f; // from the canvas centre, in units
                        popup.PlaceAt(centre);
                        Check($"Blind Fate at the {name} pad on {d.Name}", popup.Root, new[] { label }, problems);
                    }
                }
            }
            finally { ScreenSimulation.Restore(); }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
