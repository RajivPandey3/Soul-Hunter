using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>The owner's story is told in play: each stage opens with its story card, without pausing the game.</summary>
    public class StoryCardPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator EachStage_OpensWithItsStory_WithoutPausing()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            // No kills, so no level-up screen pauses the game while the card counts down (flaky before 2026-09-28).
            foreach (var weapon in SoulHunter.Gameplay.Player.PlayerController.Instance.GetComponentsInChildren<SoulHunter.Gameplay.Combat.AutoAttackWeapon>())
                weapon.gameObject.SetActive(false);
            foreach (var stray in EnemyController.ActiveEnemies.ToArray())
                if (stray != null) stray.gameObject.SetActive(false);
            var card = Object.FindFirstObjectByType<StoryCardUI>();
            Assert.IsNotNull(card, "The HUD has no story card. " + Errors());
            Assert.That(card.IsShowing, Is.True, "Level 1 opens with the story.");
            Assert.That(card.Title, Is.EqualTo("Level 1: The Cursed Graveyard"));
            StringAssert.StartsWith(CampaignStory.Premise, card.Body);
            Assert.That(GameTime.IsPaused, Is.False, "The story card does not pause the game.");

            LevelProgressionManager.Instance.StartStage(4);
            yield return null;
            Assert.That(card.IsShowing, Is.True);
            Assert.That(card.Title, Is.EqualTo("Level 4: The Ruined Castle"));
            Assert.That(card.Body, Is.EqualTo("The gateway to the Underworld. The Gatekeeper warns Kael that he is being deceived."));

            // A menu (level-up, chest, pause) hides the card; it comes back when the menu closes.
            yield return new WaitForSecondsRealtime(1f);
            Assert.That(card.Alpha, Is.GreaterThan(0.5f));
            var menu = new object();
            GameTime.Pause(menu);
            yield return null;
            Assert.That(card.IsShowing && card.Alpha == 0f, Is.True, "Hidden, not closed, while a menu is open.");
            GameTime.Resume(menu);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(card.Alpha, Is.GreaterThan(0.5f), "Back once the menu closes.");

            float limit = Time.realtimeSinceStartup + StoryCardUI.DurationFor(card.Body) + 3f;
            while (card.IsShowing && Time.realtimeSinceStartup < limit) yield return null;
            Assert.That(card.IsShowing, Is.False, "The card goes away on its own.");

            LevelProgressionManager.Instance.StartStage(10);
            yield return null;
            Assert.That(card.Body, Is.EqualTo(CampaignStory.Twist), "Level 10 reveals the twist.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// Owner (2026-10-10): landscape and portrait from the same screens. On a PC (16:9, 21:9, 5:4) and on the owner's
        /// phone held either way, with the touch controls shown, the story card stays inside the canvas, is never wider
        /// than the screen minus its margins (nor than its 1000-unit design width), stays clear of the HUD's item slots,
        /// labels and touch pads, and its text never goes under the touch floor (12 sp).
        /// </summary>
        [UnityTest]
        public IEnumerator TheStoryCard_FitsEveryScreen_ClearOfTheHud()
        {
            yield return BootIntoGameplay(save => save.VisibleJoystick = true);
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var weapon in SoulHunter.Gameplay.Player.PlayerController.Instance.GetComponentsInChildren<SoulHunter.Gameplay.Combat.AutoAttackWeapon>())
                weapon.gameObject.SetActive(false);
            yield return CloseOpenLevelUps();
            var card = Object.FindFirstObjectByType<StoryCardUI>();
            Assert.IsNotNull(card, "The HUD has a story card. " + Errors());
            var canvas = card.Card.GetComponentInParent<Canvas>().rootCanvas;
            var root = (RectTransform)canvas.transform;
            var problems = new System.Collections.Generic.List<string>();
            try
            {
                foreach (var d in ScreenSimulation.Devices)
                {
                    yield return ScreenSimulation.At(d.Pixels, d.Dpi, d.Touch, canvas);
                    card.Show(1); // the premise: the longest card
                    for (int i = 0; i < 3; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
                    card.Layout(); // after the HUD's labels and slots have settled on this screen
                    Canvas.ForceUpdateCanvases();
                    string what = $"story card on {d.Name}";
                    Assert.That(card.IsShowing, Is.True, $"{what}: shown.");
                    Rect box = ScreenSimulation.UnitsRect(card.Card);
                    Vector2 size = root.rect.size;
                    if (box.xMin < -0.5f || box.yMin < -0.5f || box.xMax > size.x + 0.5f || box.yMax > size.y + 0.5f)
                        problems.Add($"{what}: {box} is off the {size} canvas.");
                    if (box.width > Mathf.Min(StoryCardUI.MaxWidth, size.x - 2f * StoryCardUI.Margin) + 0.5f)
                        problems.Add($"{what}: {box.width:0} wide, more than the screen minus its margins or the design width.");
                    if (box.height < 100f) problems.Add($"{what}: only {box.height:0} units tall.");
                    foreach (var slot in root.GetComponentsInChildren<InventoryHud>().SelectMany(h => h.GetComponentsInChildren<UnityEngine.UI.Image>()))
                        if (slot.isActiveAndEnabled && box.Overlaps(ScreenSimulation.UnitsRect(slot.rectTransform)))
                        { problems.Add($"{what}: {box} covers the item slots."); break; }
                    foreach (var backing in root.GetComponentsInChildren<HudBacking>())
                    {
                        var image = backing.GetComponent<UnityEngine.UI.Image>();
                        if (image != null && image.enabled && box.Overlaps(ScreenSimulation.UnitsRect((RectTransform)backing.transform)))
                            problems.Add($"{what}: {box} covers the HUD label {backing.name}.");
                    }
                    var touch = Object.FindFirstObjectByType<TouchControlsUI>();
                    foreach (Transform pad in touch.Root.transform)
                        if (pad.name != "StickZone" && box.Overlaps(ScreenSimulation.UnitsRect((RectTransform)pad)))
                            problems.Add($"{what}: {box} covers the {pad.name} pad.");
                    foreach (var text in new[] { card.TitleText, card.BodyText })
                    {
                        text.ForceMeshUpdate();
                        if (text.fontSize < UiScreen.MinTextUnits - 0.01f)
                            problems.Add($"{what}: {text.name} at {text.fontSize:0.#} units, under the {UiScreen.MinTextUnits:0.#} floor.");
                    }
                }
            }
            finally { ScreenSimulation.Restore(); }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
