using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// End-to-end chest flow: a chest spawned by the pickup pool is collected by the
    /// player, opens the chest screen with a reward, and Claim closes it and resumes play.
    /// </summary>
    public class ChestFlowPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator CollectingChest_OpensRewardScreen_AndClaimResumesGame()
        {
            yield return BootIntoGameplay();

            var pool = PickupPoolManager.Instance;
            Assert.IsNotNull(pool, "No PickupPoolManager in Main_Gameplay.");
            var chestUI = LiveChestUI();
            var panel = GetPrivateField<GameObject>(chestUI, "_chestPanel");
            Assert.IsNotNull(panel, "ChestUI has no chest panel assigned.");
            Assert.IsFalse(panel.activeInHierarchy, "Chest panel should be hidden before any chest is collected.");

            // Spawn a chest on top of the player, exactly as boss drops do.
            pool.SpawnChest(PlayerController.Instance.transform.position);
            var chest = Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None)
                .FirstOrDefault(c => c.gameObject.activeInHierarchy);
            Assert.IsNotNull(chest, "SpawnChest did not produce an active chest.");

            yield return WaitUntilRealtime(() => !chest.gameObject.activeInHierarchy, "the player to collect the chest");
            yield return null;

            Assert.IsTrue(panel.activeInHierarchy,
                "Chest was collected but the chest screen did not open " +
                $"(pool _chestUI={(GetPrivateField<ChestUI>(pool, "_chestUI") == null ? "NULL" : "set")}). " + Errors());
            Assert.AreEqual(0f, Time.timeScale, "Game should pause while the chest screen is open.");

            var rewardText = GetPrivateField<TextMeshProUGUI>(chestUI, "_rewardText");
            Assert.IsNotNull(rewardText, "ChestUI has no reward text assigned.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(rewardText.text) || rewardText.text == "Chest Empty!",
                $"Chest screen shows no reward (text: '{rewardText.text}'). " + Errors());

            Click(GetPrivateField<Button>(chestUI, "_claimButton"), "Claim");
            yield return null;

            Assert.IsFalse(panel.activeInHierarchy, "Chest screen should close after Claim.");
            Assert.AreEqual(1f, Time.timeScale, "Game should resume after Claim. " + Errors());
        }

        private static ChestUI LiveChestUI()
        {
            var live = Object.FindObjectsByType<ChestUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(ui => ui.isActiveAndEnabled && GetPrivateField<GameObject>(ui, "_chestPanel") != null)
                .ToArray();
            Assert.AreEqual(1, live.Length, "Expected exactly one active, wired ChestUI.");
            return live[0];
        }
    }
}
