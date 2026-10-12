using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner (2026-10-12, from the phone: "play ke dauran ... sab samne dikh raha hai"; chose the thin strip). On a touch
    /// screen the run HUD is one thin strip under the XP bar: the item slots at the left, the clock in the middle, kills,
    /// gold and Pause at the right, and the stage name only for the first seconds of a stage. Before, the PC arrangement
    /// at phone text sizes ran down the top third of an upright phone. A PC keeps its approved arrangement.
    /// </summary>
    public class PhoneHudStripPlayModeTests : GameplayPlayModeTestBase
    {
        private static Rect Drawn(TMP_Text text)
        {
            text.ForceMeshUpdate();
            var root = (RectTransform)text.GetComponentInParent<Canvas>().rootCanvas.transform;
            Vector2 a = root.InverseTransformPoint(text.transform.TransformPoint(text.textBounds.min));
            Vector2 b = root.InverseTransformPoint(text.transform.TransformPoint(text.textBounds.max));
            Vector2 origin = root.rect.min;
            return Rect.MinMaxRect(a.x - origin.x, a.y - origin.y, b.x - origin.x, b.y - origin.y);
        }

        [UnityTest]
        public IEnumerator OnAPhone_TheHudIsOneThinStrip_AndAPcKeepsItsOwn()
        {
            yield return BootIntoGameplay(save => { save.VisibleJoystick = true; save.ShowStageProgression = true; });
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            yield return CloseOpenLevelUps();

            var hud = Object.FindFirstObjectByType<GameHUDController>();
            var canvas = hud.GetComponentInParent<Canvas>().rootCanvas;
            var timer = Object.FindFirstObjectByType<TimerUI>();
            var slots = canvas.GetComponentInChildren<InventoryHud>();
            var touch = Object.FindFirstObjectByType<TouchControlsUI>();
            var stage = hud.GetComponent<HudReadability>().StageLabel;
            var xpBar = Object.FindFirstObjectByType<XPBarUI>();
            yield return WaitUntilRealtime(() => xpBar != null && xpBar.LevelLabel != null, "the XP bar has its level label");
            var level = xpBar.LevelLabel;
            Assert.That(hud.KillText, Is.Not.Null, "The HUD has its kills label.");
            Assert.That(hud.GoldText, Is.Not.Null, "The HUD has its gold label.");
            Assert.That(timer != null && timer.TimeText != null, Is.True, "The HUD has its clock.");
            var problems = new List<string>();
            try
            {
                foreach (var d in ScreenSimulation.Devices.Where(x => x.Touch))
                {
                    yield return ScreenSimulation.At(d.Pixels, d.Dpi, d.Touch, canvas);
                    yield return null;
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    float dp = UiScreen.MinTouchUnits / 48f;
                    Vector2 size = ((RectTransform)canvas.transform).rect.size;
                    if (!hud.PhoneStrip) problems.Add($"{d.Name}: the HUD is not laid out as the phone strip");

                    var parts = new List<(string Name, Rect Box)>
                    {
                        ("kills", Drawn(hud.KillText)), ("gold", Drawn(hud.GoldText)), ("clock", Drawn(timer.TimeText)), ("level", Drawn(level)),
                        ("slots", ScreenSimulation.UnitsRect((RectTransform)slots.transform)), ("Pause", ScreenSimulation.UnitsRect(touch.PauseButton)),
                    };
                    foreach (var (name, box) in parts)
                    {
                        if (box.xMin < -1f || box.xMax > size.x + 1f || box.yMax > size.y + 1f) problems.Add($"{d.Name}: {name} {box} is off the {size} canvas");
                        // The strip: everything within 120 dp of the top (the old arrangement reached 280 dp down).
                        if (box.yMin < size.y - 120f * dp) problems.Add($"{d.Name}: {name} {box} reaches {(size.y - box.yMin) / dp:0} dp down the screen, past the strip");
                    }
                    for (int i = 0; i < parts.Count; i++)
                        for (int j = i + 1; j < parts.Count; j++)
                            if (parts[i].Box.Overlaps(parts[j].Box)) problems.Add($"{d.Name}: {parts[i].Name} {parts[i].Box} and {parts[j].Name} {parts[j].Box} overlap");

                    Rect slotBox = parts[4].Box, clock = parts[2].Box, pause = parts[5].Box, kills = parts[0].Box;
                    if (!(slotBox.xMax <= clock.xMin && clock.xMax <= kills.xMin && kills.xMax <= pause.xMin))
                        problems.Add($"{d.Name}: not slots, clock, kills, Pause from left to right ({slotBox}, {clock}, {kills}, {pause})");
                    if (Mathf.Abs(clock.center.x - size.x * 0.5f) > 2f) problems.Add($"{d.Name}: the clock {clock} is not in the middle of the {size.x:0} wide canvas");
                    if (Mathf.Min(pause.width, pause.height) < UiScreen.MinTouchUnits - 0.5f) problems.Add($"{d.Name}: Pause {pause.size} is under 48 dp");
                    // Pause is the two-bar symbol, not the word; the floating stick's area is the whole width when the
                    // phone is upright, the left part when it is held sideways.
                    if (touch.PauseBars.Count != 2 || touch.PauseBars.Any(b => !b.isActiveAndEnabled)) problems.Add($"{d.Name}: Pause does not show its two bars");
                    if (touch.PauseButton.GetComponentsInChildren<TMP_Text>().Any(t => t.isActiveAndEnabled)) problems.Add($"{d.Name}: Pause still shows a word");
                    foreach (var bar in touch.PauseBars)
                    {
                        Rect b = ScreenSimulation.UnitsRect(bar.rectTransform);
                        if (b.xMin < pause.xMin - 0.5f || b.xMax > pause.xMax + 0.5f || b.yMin < pause.yMin - 0.5f || b.yMax > pause.yMax + 0.5f) problems.Add($"{d.Name}: a Pause bar {b} is outside the pad {pause}");
                    }
                    Rect zone = ScreenSimulation.UnitsRect(touch.ZoneRect);
                    bool upright = d.Pixels.y > d.Pixels.x;
                    if (upright && (zone.xMin > size.x * 0.05f || zone.xMax < size.x * 0.95f)) problems.Add($"{d.Name}: the stick's touch area {zone} is not the whole width");
                    if (!upright && zone.xMax > size.x * 0.5f) problems.Add($"{d.Name}: the stick's touch area {zone} reaches past the left half");
                    if (zone.yMax > slotBox.yMin) problems.Add($"{d.Name}: the stick's touch area {zone} reaches into the HUD strip (slots {slotBox})");
                    if (timer.TimeText.text.Contains("Time")) problems.Add($"{d.Name}: the clock reads '{timer.TimeText.text}'");
                    if (!hud.KillText.text.StartsWith("Kills: ")) problems.Add($"{d.Name}: kills reads '{hud.KillText.text}'");
                    foreach (var text in new TMP_Text[] { hud.KillText, hud.GoldText, timer.TimeText })
                        if (text.fontSize < UiScreen.MinTextUnits - 0.01f) problems.Add($"{d.Name}: '{text.text}' is {text.fontSize:0.#} units, under the 12 sp floor");
                }

                // An unlock notice shown as a stage opens hangs under the strip and the stage name (on the phone it was drawn
                // over the stage name), and the item slots hide under the pause menu, whose title sits in their corner.
                {
                    var unlocks = Object.FindFirstObjectByType<SoulHunter.Gameplay.Core.CharacterUnlockTracker>();
                    unlocks.AnnounceUnlock("Unlocked: the Bestiary (main menu)");
                    yield return null;
                    var last = ScreenSimulation.Devices.Last(x => x.Touch);
                    yield return ScreenSimulation.At(last.Pixels, last.Dpi, last.Touch, canvas, unlocks.Notice.GetComponent<Canvas>());
                    yield return null;
                    yield return null;
                    Rect notice = ScreenSimulation.UnitsRect(unlocks.NoticeBox);
                    Rect slotBox = ScreenSimulation.UnitsRect((RectTransform)slots.transform);
                    if (notice.Overlaps(slotBox)) problems.Add($"The unlock notice {notice} is drawn over the item slots {slotBox}");
                    foreach (var text in new TMP_Text[] { hud.KillText, hud.GoldText, timer.TimeText })
                        if (notice.Overlaps(Drawn(text))) problems.Add($"The unlock notice {notice} is drawn over '{text.text}'");
                    if (stage.gameObject.activeInHierarchy && notice.Overlaps(Drawn(stage))) problems.Add($"The unlock notice {notice} is drawn over the stage name {Drawn(stage)}");

                    if (slots.HiddenUnderMenu) problems.Add("The item slots are hidden during play");
                    var system = SoulHunter.Gameplay.Core.GameSystemManager.Instance;
                    system.TogglePause();
                    yield return null;
                    yield return null;
                    if (!slots.HiddenUnderMenu) problems.Add("The item slots still show under the pause menu on a phone");
                    system.Resume();
                    yield return null;
                    yield return null;
                    if (slots.HiddenUnderMenu) problems.Add("The item slots stay hidden after the pause menu closes");
                }

                // The stage name: one line under the strip, gone after its seconds of play.
                if (stage.gameObject.activeInHierarchy)
                {
                    Rect name = Drawn(stage);
                    Vector2 size = ((RectTransform)canvas.transform).rect.size;
                    if (name.xMin < -1f || name.xMax > size.x + 1f) problems.Add($"The stage name {name} is off the canvas");
                    if (name.Overlaps(ScreenSimulation.UnitsRect((RectTransform)slots.transform))) problems.Add("The stage name is drawn over the item slots");
                }
                yield return WaitUntilRealtime(() => !stage.gameObject.activeInHierarchy, "the stage name hides after its first seconds on a phone");
            }
            finally { ScreenSimulation.Restore(); }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));

            // A PC: the approved arrangement and words are back.
            yield return null;
            yield return null;
            Assert.That(hud.PhoneStrip, Is.False, "A PC does not use the strip.");
            Assert.That(hud.KillText.text, Does.StartWith("Total Kills: "));
            Assert.That(timer.TimeText.text, Does.StartWith("Time: "));
            Assert.That(stage.gameObject.activeInHierarchy, Is.True, "A PC always shows the stage name.");
            Assert.That(((RectTransform)slots.transform).localScale.x, Is.EqualTo(1f).Within(0.001f), "The item slots are full size on a PC.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// Owner (2026-10-12: "portrait mode mein joystick ko jahan par ungli rakhe wahin par aa jaye"). On the real screen,
        /// with the phone held upright, a press on the right side of the screen is taken by the floating stick's area and
        /// the stick jumps under it; Dash still takes its own press. Held sideways, the right side is not the stick's.
        /// </summary>
        [UnityTest]
        public IEnumerator Upright_TheStickComesToTheThumb_AnywhereAcrossTheScreen()
        {
            yield return BootIntoGameplay(save => { save.VisibleJoystick = true; save.JoystickType = SoulHunter.Core.Persistence.GameSettings.JoystickDefault; });
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            yield return CloseOpenLevelUps();
            var touch = Object.FindFirstObjectByType<TouchControlsUI>();
            var events = UnityEngine.EventSystems.EventSystem.current;
            Assert.IsNotNull(events, "The scene has an event system.");
            System.Func<Vector2, string> topAt = point =>
            {
                var hits = new List<UnityEngine.EventSystems.RaycastResult>();
                events.RaycastAll(new UnityEngine.EventSystems.PointerEventData(events) { position = point }, hits);
                if (hits.Count == 0) return "(nothing)";
                // The press goes to the pad the hit object belongs to (a pad's own picture or label).
                for (Transform t = hits[0].gameObject.transform; t != null; t = t.parent)
                    if (t.name == "StickZone" || t.name == "DashButton" || t.name == "PauseButton") return t.name;
                return hits[0].gameObject.name;
            };
            try
            {
                // Upright (the window keeps its shape; the HUD is told the phone is upright).
                UiScreen.TestPixels = new Vector2(Screen.height, Screen.width * 2f);
                UiScreen.RaiseChanged();
                yield return null;
                yield return null;
                // Owner (2026-10-12): the stick's square pad is not drawn; a round see-through plate and a small knob are.
                Assert.That(touch.StickBase.GetComponent<UnityEngine.UI.Image>().color.a, Is.EqualTo(0f), "The stick's square pad is not drawn.");
                Assert.IsNotNull(touch.Plate.sprite, "The plate uses the theme's round picture.");
                Assert.That(touch.Plate.color.a, Is.GreaterThan(0.1f).And.LessThan(0.8f), "The plate is see-through.");
                Assert.That(touch.Plate.rectTransform.rect.width, Is.EqualTo(touch.StickBase.rect.width * TouchControlsUI.PlateShare).Within(0.5f), "The plate is the size the knob used to be.");
                Assert.That(touch.Dot.rectTransform.rect.width, Is.LessThan(touch.Plate.rectTransform.rect.width * 0.4f), "The knob is much smaller than the plate.");
                Assert.That(touch.Dot.color.a, Is.GreaterThan(0.5f), "The knob is drawn.");
                Assert.That(touch.RoundPads.Count, Is.EqualTo(2), "Dash and Pause.");
                foreach (var pad in touch.RoundPads)
                {
                    Assert.That(pad.sprite, Is.EqualTo(touch.Plate.sprite), pad.name + " is the same round picture as the stick's plate.");
                    Assert.That(pad.color.a, Is.GreaterThan(0.1f).And.LessThan(0.8f), pad.name + " is see-through.");
                }
                foreach (var spot in new[] { new Vector2(0.85f, 0.5f), new Vector2(0.5f, 0.3f), new Vector2(0.1f, 0.6f), new Vector2(0.7f, 0.75f) })
                {
                    var press = new Vector2(Screen.width * spot.x, Screen.height * spot.y);
                    Assert.That(topAt(press), Is.EqualTo("StickZone"), $"Upright, a press at {spot} of the screen is the stick's.");
                    var e = new UnityEngine.EventSystems.PointerEventData(events) { position = press, pressPosition = press };
                    touch.Zone.OnPointerDown(e);
                    Vector2 stick = RectTransformUtility.WorldToScreenPoint(null, touch.StickBase.position);
                    Vector2 half = RectTransformUtility.WorldToScreenPoint(null, touch.StickBase.TransformPoint(touch.StickBase.rect.max)) - stick;
                    // Under the thumb, or as near as keeps the whole stick on the screen.
                    var want = new Vector2(Mathf.Clamp(press.x, half.x, Screen.width - half.x), Mathf.Clamp(press.y, half.y, Screen.height - half.y));
                    Assert.That(Vector2.Distance(stick, want), Is.LessThan(Mathf.Max(half.x, 4f) * 0.5f), $"The stick came to the thumb at {spot} (stick {stick}, thumb {press}).");
                    touch.Zone.OnPointerUp(e);
                }
                var dash = touch.Root.transform.Find("DashButton") as RectTransform;
                Vector2 dashAt = RectTransformUtility.WorldToScreenPoint(null, dash.TransformPoint(dash.rect.center));
                Assert.That(topAt(dashAt), Is.EqualTo("DashButton"), "Dash still takes its own press.");
                Vector2 pauseAt = RectTransformUtility.WorldToScreenPoint(null, touch.PauseButton.TransformPoint(touch.PauseButton.rect.center));
                Assert.That(topAt(pauseAt), Is.EqualTo("PauseButton"), "Pause still takes its own press.");

                // Sideways: the right side of the screen is not the stick's.
                UiScreen.TestPixels = new Vector2(Screen.width * 2f, Screen.height);
                UiScreen.RaiseChanged();
                yield return null;
                yield return null;
                Assert.That(topAt(new Vector2(Screen.width * 0.7f, Screen.height * 0.5f)), Is.Not.EqualTo("StickZone"), "Sideways, the right side is not the stick's area.");
                Assert.That(topAt(new Vector2(Screen.width * 0.2f, Screen.height * 0.4f)), Is.EqualTo("StickZone"), "Sideways, the left part is.");
            }
            finally
            {
                UiScreen.TestPixels = null;
                UiScreen.RaiseChanged();
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
