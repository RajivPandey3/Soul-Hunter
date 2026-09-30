using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
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
    /// VS stats panel (vampire.survivors.wiki/w/Player_stats: "in-game when paused, or when leveling up"). Owner request
    /// (2026-09-30) after Hollow Heart looked like it did nothing.
    /// </summary>
    public class StatsPanelPlayModeTests : GameplayPlayModeTestBase
    {
        private static readonly string[] WikiOrder =
        {
            "Max Health", "Recovery", "Armor", "Move Speed", "Might", "Area", "Speed", "Duration", "Amount", "Cooldown",
            "Luck", "Growth", "Greed", "Curse", "Magnet", "Revival", "Reroll", "Skip", "Banish",
        };

        [UnityTest]
        public IEnumerator ThePauseMenu_ShowsTheLiveStats_BesideItsButtons()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var system = GameSystemManager.Instance;
            system.TogglePause();
            yield return null;
            var panel = system.PauseMenuPanel.GetComponentInChildren<StatsPanel>(true);
            Assert.IsNotNull(panel, "The pause menu has a stats panel. " + Errors());
            Assert.That(panel.gameObject.activeInHierarchy, Is.True);
            Assert.That(panel.Rows.Select(r => r.Label), Is.EqualTo(WikiOrder), "The wiki's order.");
            int maxHealth = PlayerController.Instance.GetComponent<HealthController>().MaxHealth;
            Assert.That(panel.Rows[0].Value, Is.EqualTo(maxHealth.ToString()), "Max Health is the live value.");
            Assert.That(panel.GetComponentsInChildren<TMP_Text>().Any(t => t.text.Contains("Max Health")), Is.True, "The rows are drawn.");

            var pause = system.PauseMenuPanel.GetComponent<PauseMenuUI>();
            foreach (var button in new[] { pause.ResumeButton, pause.OptionsButton, pause.QuitButton })
                Assert.That(Overlaps((RectTransform)panel.transform, (RectTransform)button.transform), Is.False, $"The panel is clear of {button.name}.");

            system.Resume();
            yield return null;
            Assert.That(panel.gameObject.activeInHierarchy, Is.False, "It hides with the pause menu.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator TheLevelUpScreen_ShowsTheStats_AndHollowHeartRaisesMaxHealth()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.enabled = false;
            var health = player.GetComponent<HealthController>();
            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            var experience = player.GetComponent<PlayerExperience>();
            experience.AddXP(experience.XPToNextLevel - experience.CurrentXP);
            yield return WaitUntilRealtime(() => levelUp.IsLevelUpActive, "the level-up screen");
            yield return null;

            var panel = Object.FindObjectsByType<StatsPanel>(FindObjectsSortMode.None).FirstOrDefault(p => p.gameObject.activeInHierarchy);
            Assert.IsNotNull(panel, "The level-up screen has a stats panel. " + Errors());
            Assert.That(panel.Rows.Select(r => r.Label), Is.EqualTo(WikiOrder));
            int before = health.MaxHealth;
            Assert.That(panel.Rows[0].Value, Is.EqualTo(before.ToString()));

            // The panel sits between the heading and the cards.
            var panelRect = (RectTransform)panel.transform;
            foreach (var button in panel.transform.parent.GetComponentsInChildren<Button>().Where(b => b.gameObject.activeInHierarchy))
                Assert.That(Overlaps(panelRect, (RectTransform)button.transform), Is.False, $"The panel is clear of {button.name}.");
            var heading = panel.transform.parent.GetComponentsInChildren<TMP_Text>().FirstOrDefault(t => t.text.Contains("LEVEL UP"));
            if (heading != null)
            {
                // The heading's rect is tall; what must stay clear is its drawn text.
                float textBottom = heading.rectTransform.TransformPoint(new Vector3(0f, heading.textBounds.min.y, 0f)).y;
                Assert.That(ScreenRect(panelRect).yMax, Is.LessThanOrEqualTo(textBottom + 0.5f), "The panel is below the LEVEL UP! text.");
                Assert.That(ScreenRect(panelRect).height, Is.GreaterThan(40f), "The panel has room to show its rows.");
            }

            // Hollow Heart while the screen is open: Max Health x1.2 shows in the panel (VS: it does not fill the new health).
            var heart = ScriptableObject.CreateInstance<UpgradeData>();
            heart.Type = UpgradeData.UpgradeType.MaxHealth;
            heart.Level = 1;
            int current = health.CurrentHealth;
            try { WeaponManager.Instance.ApplyUpgrade(heart); }
            finally { Object.Destroy(heart); }
            yield return new WaitForSecondsRealtime(0.4f);
            int after = health.MaxHealth;
            Assert.That(after, Is.EqualTo(Mathf.RoundToInt(before * 1.2f)).Within(1), "Hollow Heart: Max Health x1.2.");
            Assert.That(health.CurrentHealth, Is.EqualTo(current), "VS: it does not heal to fill the new Max Health.");
            Assert.That(panel.Rows[0].Value, Is.EqualTo(after.ToString()), "The panel shows the new Max Health.");
            Assert.That(panel.Rows[0].Changed, Is.True, "Highlighted as changed.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static bool Overlaps(RectTransform a, RectTransform b) => ScreenRect(a).Overlaps(ScreenRect(b));

        private static Rect ScreenRect(RectTransform t)
        {
            var corners = new Vector3[4];
            t.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
