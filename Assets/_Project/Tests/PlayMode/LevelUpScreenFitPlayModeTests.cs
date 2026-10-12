using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner, 2026-10-01 ("level up screen bhi chest jaisa kar do"): the level-up screen lived on the HUD canvas, which
    /// scales by width only, so a wide, short window cut off its lower cards and buttons. It now has its own canvas that
    /// expands to fit (like the chest screen), above the HUD and below the chest screen.
    /// </summary>
    public class LevelUpScreenFitPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator TheLevelUpScreen_HasItsOwnExpandingCanvas_AndEveryButtonIsOnScreen()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.enabled = false;
            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            levelUp.AddCharges(1, 1, 1); // so Reroll / Skip / Banish show too
            var experience = player.GetComponent<PlayerExperience>();

            for (int round = 0; round < 2; round++)
            {
                experience.AddXP(experience.XPToNextLevel - experience.CurrentXP);
                yield return WaitUntilRealtime(() => levelUp.IsLevelUpActive, "a level-up");
                yield return null;
                yield return null;
                var ui = Object.FindFirstObjectByType<LevelUpUI>();
                var panel = (GameObject)typeof(LevelUpUI).GetField("_levelUpPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(ui);
                var canvas = panel.GetComponentInParent<Canvas>().rootCanvas;
                Assert.That(canvas.name, Is.EqualTo(LevelUpUI.ScreenCanvasName), "The level-up screen has its own canvas.");
                var scaler = canvas.GetComponent<CanvasScaler>();
                Assert.That(scaler.GetComponent<SoulHunter.Gameplay.UI.ResponsiveCanvas>(), Is.Not.Null, "It takes the game's one UI scale (ResponsiveCanvas, landscape and portrait).");
                Assert.That(canvas.sortingOrder, Is.EqualTo(LevelUpUI.ScreenSortingOrder));
                Assert.That(canvas.sortingOrder, Is.LessThan(ChestUI.ScreenSortingOrder), "Below the chest screen.");

                var corners = new Vector3[4];
                foreach (var button in panel.GetComponentsInChildren<Button>().Where(b => b.gameObject.activeInHierarchy))
                {
                    ((RectTransform)button.transform).GetWorldCorners(corners);
                    foreach (var c in corners)
                        Assert.That(c.x >= -1f && c.x <= Screen.width + 1f && c.y >= -1f && c.y <= Screen.height + 1f, Is.True,
                            $"{button.name} corner {c} is on the {Screen.width}x{Screen.height} screen.");
                }
                Assert.That(panel.GetComponentsInChildren<Button>().Count(b => b.gameObject.activeInHierarchy), Is.GreaterThanOrEqualTo(4), "Cards and action buttons were checked.");
                // Responsive (owner, 2026-10-10): the same screen laid out on a PC, a wide PC, a 5:4 PC and a phone held
                // either way: every card and button on the canvas, none overlapping, none under 48 dp on the phone.
                if (round == 0) yield return ScreenSimulation.AssertFitsEveryDevice(() => panel.GetComponentsInChildren<Selectable>(), canvas);
                // Every stats row is drawn on the stats panel (owner's phone, 2026-10-12: upright, "Cooldown" was drawn
                // under the panel, on the first card).
                if (round == 0)
                {
                    var rowProblems = new System.Collections.Generic.List<string>();
                    try
                    {
                        foreach (var d in ScreenSimulation.Devices)
                        {
                            yield return ScreenSimulation.At(d.Pixels, d.Dpi, d.Touch, canvas);
                            for (int i = 0; i < 3; i++) { yield return null; Canvas.ForceUpdateCanvases(); }
                            var stats = panel.GetComponentsInChildren<StatsPanel>().FirstOrDefault(s => s.gameObject.activeInHierarchy);
                            if (stats == null) continue;
                            stats.Refresh();
                            yield return null;
                            Rect box = ScreenSimulation.UnitsRect((RectTransform)stats.transform);
                            foreach (var text in stats.GetComponentsInChildren<TMPro.TMP_Text>().Where(t => !string.IsNullOrWhiteSpace(t.text)))
                            {
                                text.ForceMeshUpdate();
                                float low = ((RectTransform)canvas.transform).InverseTransformPoint(text.transform.TransformPoint(text.textBounds.min)).y - ((RectTransform)canvas.transform).rect.yMin;
                                if (low < box.yMin - 2f) rowProblems.Add($"{d.Name}: stats text '{text.name}' is drawn {box.yMin - low:0.#} units under its panel {box}");
                            }
                        }
                    }
                    finally { ScreenSimulation.Restore(); }
                    Assert.That(rowProblems, Is.Empty, string.Join("\n", rowProblems));
                }
                levelUp.SelectUpgrade(levelUp.GetRandomValidUpgrades(1)[0]);
                yield return null;
                yield return null;
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
