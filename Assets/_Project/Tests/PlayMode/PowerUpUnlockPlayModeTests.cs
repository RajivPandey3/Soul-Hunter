using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using SoulHunter.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS locked PowerUps in the real game (owner, 2026-10-01: "adopt vs rule"): the shop shows them LOCKED with their
    /// condition and will not sell them, and surviving in Level 6 unlocks a Skip rank during the run.
    /// </summary>
    public class PowerUpUnlockPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator TheShop_ShowsLockedPowerUps_WithTheirCondition_AndWillNotSellThem()
        {
            SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            var save = GameServices.Instance.Get<SaveService>();
            save.UseFile(System.IO.Path.Combine(Application.temporaryCachePath, "playmode_test_save.json"));
            save.CurrentData.Currency = 50000;
            save.CurrentData.PowerUpRanks.Clear();
            save.CurrentData.UnlockedAchievements.Clear();
            Object.FindFirstObjectByType<MainMenuNavigation>().ShowPowerUps();
            yield return null;

            var shop = Object.FindFirstObjectByType<PowerUpMenuUI>();
            foreach (var id in new[] { PowerUpId.Reroll, PowerUpId.Skip, PowerUpId.Banish, PowerUpId.Omni, PowerUpId.Charm, PowerUpId.Defang, PowerUpId.Preserve })
            {
                StringAssert.Contains("LOCKED", shop.CellFor(id).GetComponentInChildren<TextMeshProUGUI>().text, $"{id} cell");
                shop.Select(id);
                StringAssert.Contains("Unlock", shop.InfoText, $"{id} shows what unlocks it.");
                Assert.That(shop.BuyButton.interactable, Is.False, $"{id} cannot be bought.");
                int gold = save.CurrentData.Currency;
                shop.BuyButton.onClick.Invoke();
                Assert.That(PowerUps.Rank(save.CurrentData, id), Is.EqualTo(0), $"{id} not bought.");
                Assert.That(save.CurrentData.Currency, Is.EqualTo(gold), $"No gold spent on {id}.");
            }
            shop.Select(PowerUpId.Reroll);
            StringAssert.Contains("Reach level 80 with Corvan.", shop.InfoText);

            // Once unlocked, a rank can be bought, and the next one stays locked.
            PowerUpUnlocks.Record(save.CurrentData, "Corvan", 80, 1, 0f);
            shop.Select(PowerUpId.Reroll);
            Assert.That(shop.BuyButton.interactable, Is.True, "Reroll rank 1 unlocked.");
            shop.BuyButton.onClick.Invoke();
            Assert.That(PowerUps.Rank(save.CurrentData, PowerUpId.Reroll), Is.EqualTo(1));
            Assert.That(shop.BuyButton.interactable, Is.False, "Rank 2 locked.");
            StringAssert.Contains("Reach level 80 with Aria.", shop.InfoText);

            // An open PowerUp is unchanged.
            shop.Select(PowerUpId.Might);
            Assert.That(shop.BuyButton.interactable, Is.True);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Surviving15MinutesInLevel6_UnlocksASkipRank_WithANotice()
        {
            yield return BootIntoGameplay(save => save.UnlockedAchievements.Clear());
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>()) weapon.enabled = false;
            PlayerController.Instance.GetComponent<HealthController>().GrantInvulnerability(9999f);
            var save = GameServices.Instance.Get<SaveService>();
            var progression = LevelProgressionManager.Instance;
            progression.StartStage(6);
            yield return null;
            Assert.That(PowerUpUnlocks.UnlockedRanks(save.CurrentData, PowerUpId.Skip), Is.EqualTo(0));
            progression.SkipTo(15f * 60f);
            var tracker = Object.FindFirstObjectByType<CharacterUnlockTracker>();
            yield return WaitUntilRealtime(() => PowerUpUnlocks.UnlockedRanks(save.CurrentData, PowerUpId.Skip) == 1, "the Skip unlock");
            // 15:00 also unlocks items (Peachone at 10:00): the notices follow one another, none is lost.
            yield return WaitUntilRealtime(() => tracker.Notice != null && tracker.Notice.activeInHierarchy &&
                tracker.NoticeText != null && tracker.NoticeText.text.Contains("Skip rank 1"), "the Skip unlock notice");
            Assert.That(save.CurrentData.UnlockedAchievements, Does.Contain(PowerUpUnlocks.SkipId(6, 900)));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
