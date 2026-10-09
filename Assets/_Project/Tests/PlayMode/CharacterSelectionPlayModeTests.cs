using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;
using SoulHunter.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS-style character select: the main menu shows a card for every character, clicking one shows
    /// its full stat block, and the chosen character's stats and abilities are live when the run starts.
    /// Nothing is written to the save file (Confirm is not pressed; selection is set in memory).
    /// </summary>
    public class CharacterSelectionPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator SelectScreen_HasACardPerCharacter_AndClickingShowsItsStats()
        {
            var load = SceneManager.LoadSceneAsync("Bootstrap");
            Assert.IsNotNull(load, "Bootstrap is not in Build Settings.");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            yield return null;

            var navigation = Object.FindFirstObjectByType<MainMenuNavigation>();
            Assert.IsNotNull(navigation, "MainMenuNavigation missing. " + Errors());
            navigation.ShowCharacters();
            yield return null;

            var ui = Object.FindFirstObjectByType<CharacterSelectionUI>();
            Assert.IsNotNull(ui, "CharacterSelectionUI missing. " + Errors());
            int expected = GameContentCatalog.Load().Characters.Count(c => c != null);
            Assert.That(ui.Cards.Count, Is.EqualTo(expected), "One card per character. " + Errors());

            int isolde = GameContentCatalog.Load().Characters.Where(c => c != null).ToList().FindIndex(c => c.CharacterName == "Isolde");
            Assert.That(isolde, Is.GreaterThanOrEqualTo(0));
            Click(ui.Cards[isolde], "Isolde card");
            yield return null;

            Assert.That(ui.SelectedCharacter.CharacterName, Is.EqualTo("Isolde"));
            string stats = ui.StatsText.text;
            foreach (var label in new[] { "Max Health", "Recovery", "Armor", "Move Speed", "Might", "Speed", "Duration", "Area",
                                          "Cooldown", "Amount", "Revival", "Magnet", "Luck", "Growth", "Greed", "Curse" })
                StringAssert.Contains(label, stats, $"Stats panel is missing {label}.");
            StringAssert.Contains("-15%", stats, "Isolde's -15% cooldown should show.");
            Assert.That(ui.Cards[isolde].gameObject.activeInHierarchy, Is.True);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// Owner report (2026-09-30): the character select screen was "bekar" outside 1920x1080. The canvas used Constant
        /// Pixel Size, so fixed 190x132-pixel cards overflowed the grid, and the mode row pushed the last row under the info
        /// box. The test window is not 1080p, which is the case that broke.
        /// </summary>
        [UnityTest]
        public IEnumerator SelectScreen_FitsTheWindow_EveryCardVisible_NothingOverlaps()
        {
            var load = SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            yield return null;
            Object.FindFirstObjectByType<MainMenuNavigation>().ShowCharacters();
            yield return null;
            yield return null;
            yield return null;

            var ui = Object.FindFirstObjectByType<CharacterSelectionUI>();
            var scaler = ui.GetComponentInParent<UnityEngine.UI.CanvasScaler>() ?? Object.FindFirstObjectByType<UnityEngine.UI.CanvasScaler>();
            Assert.That(scaler.uiScaleMode, Is.EqualTo(UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize), "The menu scales with the screen.");
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));

            var viewport = (RectTransform)ui.Cards[0].transform.parent.parent;
            Rect view = ScreenRect(viewport);
            foreach (var card in ui.Cards)
            {
                Rect r = ScreenRect((RectTransform)card.transform);
                Assert.That(r.xMin >= view.xMin - 1f && r.xMax <= view.xMax + 1f && r.yMin >= view.yMin - 1f && r.yMax <= view.yMax + 1f, Is.True,
                    $"{card.name} is fully visible in the grid (card {r}, grid {view}, screen {Screen.width}x{Screen.height}).");
            }

            var root = viewport.parent.parent;
            Rect grid = ScreenRect((RectTransform)root.Find("CharacterGrid"));
            Assert.That(grid.Overlaps(ScreenRect((RectTransform)root.Find("SelectedInfo"))), Is.False, "The grid is clear of the info box.");
            Assert.That(grid.Overlaps(ScreenRect((RectTransform)root.Find("RunModes"))), Is.False, "The grid is clear of the mode row.");

            var modeLabels = root.Find("RunModes").GetComponentsInChildren<TMPro.TextMeshProUGUI>().Where(t => t.name == "Label").ToList();
            Assert.That(modeLabels.Count, Is.EqualTo(8)); // six modes and the two Randomizers (P3-05)
            Assert.That(modeLabels.Select(l => l.fontSize).Distinct().Count(), Is.EqualTo(1),
                "All eight mode labels share one size: " + string.Join(", ", modeLabels.Select(l => $"{l.text} {l.fontSize:0.0}")));

            // TextMeshPro's defaults (auto-size up to 72) used to replace these limits, because the panel is built inactive.
            var texts = root.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
            foreach (var (name, max) in new[] { ("Gold_Text", 32f), ("StartingWeapon_Text", 18f) })
            {
                var text = texts.First(t => t.name == name);
                Assert.That(text.fontSizeMax, Is.EqualTo(max), $"{name} keeps its size limit.");
                Assert.That(text.fontSize, Is.LessThanOrEqualTo(max + 0.01f), $"{name} is at most {max}.");
            }
            var filter = texts.First(t => t.text.StartsWith("FILTER"));
            Assert.That(filter.fontSize, Is.LessThanOrEqualTo(20.01f), "FILTER is at most 20.");
            foreach (var badge in texts.Where(t => t.name == "Badge" && !string.IsNullOrEmpty(t.text)))
            {
                badge.ForceMeshUpdate();
                Assert.That(badge.textInfo.lineCount, Is.EqualTo(1), $"'{badge.text}' stays on one line.");
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static Rect ScreenRect(RectTransform t)
        {
            var corners = new Vector3[4];
            t.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [UnityTest]
        public IEnumerator ChosenCharacter_StatBlockAndAbilities_AreLiveInTheRun()
        {
            var load = SceneManager.LoadSceneAsync("Bootstrap");
            Assert.IsNotNull(load, "Bootstrap is not in Build Settings.");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            var save = GameServices.Instance.Get<SaveService>();
            string previousSelection = save.CurrentData.SelectedCharacterName;
            save.CurrentData.SelectedCharacterName = "Sylas"; // in memory only, never saved
            try
            {
                GameServices.Instance.Get<SoulHunter.Core.Scenes.SceneService>().LoadSceneAsync("Main_Gameplay");
                yield return WaitForFreshGameplay(previousPlayer: null);

                var sylas = GameContentCatalog.FindCharacterExact("Sylas");
                var player = PlayerController.Instance;
                var stats = player.Stats;
                var health = player.GetComponent<HealthController>();

                Assert.That(stats.MoveSpeedMultiplier, Is.EqualTo(sylas.BaseMoveSpeed).Within(0.001f), "Move speed multiplier applied. " + Errors());
                Assert.That(health.IsShielded, Is.True, "Sylas starts shielded. " + Errors());
                int before = health.CurrentHealth;
                health.TakeDamage(new DamagePacket(20, player.transform.position, Vector3.zero) { IgnoresHitInvulnerability = true });
                Assert.That(health.CurrentHealth, Is.EqualTo(before), "Shield blocks the hit. " + Errors());

                var visual = player.GetComponentInChildren<StartingShieldVisual>();
                Assert.IsNotNull(visual, "Sylas's shield is visible while it lasts. " + Errors());
                Assert.IsNotNull(visual.Visual, "Shield visual model spawned. " + Errors());
                health.ShieldedUntil = Time.time - 1f; // end the shield now
                yield return null;
                yield return null;
                Assert.IsNull(player.GetComponentInChildren<StartingShieldVisual>(), "Shield visual is removed when the shield ends.");
                Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
            }
            finally
            {
                save.CurrentData.SelectedCharacterName = previousSelection;
            }
        }

        [UnityTest]
        public IEnumerator SelectScreen_ShowsLockedBuyableAndOwnedCharacters_AndFiltersByStage()
        {
            var load = SceneManager.LoadSceneAsync("Bootstrap");
            Assert.IsNotNull(load, "Bootstrap is not in Build Settings.");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            Object.FindFirstObjectByType<MainMenuNavigation>().ShowCharacters();
            yield return null;
            var ui = Object.FindFirstObjectByType<CharacterSelectionUI>();
            var data = GameServices.Instance.Get<SaveService>().CurrentData;

            // In memory only, never saved: Gideon unlocked but not bought, no gold.
            var unlockedBefore = data.UnlockedCharacters; var ownedBefore = data.OwnedCharacters; int goldBefore = data.Currency;
            data.UnlockedCharacters = new System.Collections.Generic.List<string> { "Gideon" };
            data.OwnedCharacters = new System.Collections.Generic.List<string>();
            data.Currency = 0;
            try
            {
                ui.Refresh();
                var names = GameContentCatalog.Load().Characters.Where(c => c != null).Select(c => c.CharacterName).ToList();
                Assert.That(ui.BadgeText(names.IndexOf("Kael")), Is.Empty, "Kael is owned.");
                Assert.That(ui.BadgeText(names.IndexOf("Gideon")), Is.EqualTo($"{GameContentCatalog.FindCharacterExact("Gideon").GoldPrice} G"));
                Assert.That(ui.BadgeText(names.IndexOf("Draven")), Is.EqualTo("LOCKED"));
                Assert.That(ui.GoldText, Is.EqualTo("Gold: 0"));

                ui.SelectCharacter(names.IndexOf("Draven"));
                Assert.That(ui.ConfirmText, Is.EqualTo("Locked"));
                Assert.That(ui.ConfirmInteractable, Is.False, "A locked character cannot be picked.");
                ui.SelectCharacter(names.IndexOf("Gideon"));
                Assert.That(ui.ConfirmText, Does.StartWith("Buy"));
                Assert.That(ui.ConfirmInteractable, Is.False, "Not enough gold to buy.");
                ui.SelectCharacter(names.IndexOf("Kael"));
                Assert.That(ui.ConfirmText, Is.EqualTo("Confirm"));
                Assert.That(ui.ConfirmInteractable, Is.True);

                // Filter: hiding stage 2 hides the characters that come from it, only while the filter is on.
                ui.SetStageShown(2, false);
                Assert.That(ui.Cards.All(c => c.gameObject.activeSelf), Is.True, "Filter off shows everyone.");
                ui.SetFilter(true);
                Assert.That(ui.FilterCountText, Is.EqualTo("9/10"));
                for (int i = 0; i < names.Count; i++)
                {
                    bool fromStage2 = GameContentCatalog.FindCharacterExact(names[i]).OriginStage == 2;
                    Assert.That(ui.Cards[i].gameObject.activeSelf, Is.EqualTo(!fromStage2), $"{names[i]} filter visibility.");
                }
                Assert.That(ui.StageToggles[1].isOn, Is.False);
                ui.SetFilter(false);
                Assert.That(ui.Cards.All(c => c.gameObject.activeSelf), Is.True);
                Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
            }
            finally
            {
                data.UnlockedCharacters = unlockedBefore; data.OwnedCharacters = ownedBefore; data.Currency = goldBefore;
            }
        }

        [UnityTest]
        public IEnumerator Run_HasAnUnlockTracker_ThatFollowsStageProgress()
        {
            yield return BootIntoGameplay();
            var tracker = SoulHunter.Gameplay.Core.GameSessionManager.Instance.GetComponent<SoulHunter.Gameplay.Core.CharacterUnlockTracker>();
            Assert.IsNotNull(tracker, "The session owns a character unlock tracker. " + Errors());
            var progress = tracker.CurrentProgress();
            Assert.That(progress.Stage, Is.EqualTo(1));
            Assert.That(progress.PlayerLevel, Is.GreaterThanOrEqualTo(1));
            Assert.That(progress.WeaponLevel, Is.Not.Null);

            // Clearing stage 1 unlocks nobody, so nothing is written to the save file.
            var progression = SoulHunter.Gameplay.Core.LevelProgressionManager.Instance;
            typeof(SoulHunter.Gameplay.Core.LevelProgressionManager)
                .GetMethod("CompleteStage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(progression, new object[] { "test" });
            yield return null;
            Assert.That(tracker.HighestClearedStage, Is.EqualTo(1), "Stage clears are tracked. " + Errors());
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Seraphina_ProjectilesBounceThreeExtraTimes()
        {
            var load = SceneManager.LoadSceneAsync("Bootstrap");
            Assert.IsNotNull(load, "Bootstrap is not in Build Settings.");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            var save = GameServices.Instance.Get<SaveService>();
            string previousSelection = save.CurrentData.SelectedCharacterName;
            save.CurrentData.SelectedCharacterName = "Seraphina"; // in memory only, never saved
            try
            {
                GameServices.Instance.Get<SoulHunter.Core.Scenes.SceneService>().LoadSceneAsync("Main_Gameplay");
                yield return WaitForFreshGameplay(previousPlayer: null);

                // Her starting weapon fires at the horde; every live shot must carry her 3 extra bounces.
                var field = typeof(Projectile).GetField("_maxBounces", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Projectile shot = null;
                yield return WaitUntilRealtime(() =>
                    (shot = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).FirstOrDefault(p => p.isActiveAndEnabled)) != null,
                    "Seraphina's first projectile");
                Assert.That((int)field.GetValue(shot), Is.GreaterThanOrEqualTo(3), "Seraphina's projectile has her 3 extra bounces. " + Errors());
            }
            finally
            {
                save.CurrentData.SelectedCharacterName = previousSelection;
            }
        }
    }
}
