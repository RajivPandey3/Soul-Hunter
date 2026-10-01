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
                Assert.That(scaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.Expand), "It expands to fit any window, like the chest screen.");
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
                levelUp.SelectUpgrade(levelUp.GetRandomValidUpgrades(1)[0]);
                yield return null;
                yield return null;
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
