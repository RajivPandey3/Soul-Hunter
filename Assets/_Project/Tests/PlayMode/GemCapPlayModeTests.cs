using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS wiki (Experience_Gem): with more than 400 gems on the ground, new XP is merged into one red gem. Found
    /// 2026-09-28: without it, thousands of uncollected gems dragged a 30-minute editor run down to ~20 FPS.
    /// </summary>
    public class GemCapPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator PastFourHundredGems_NewXpGoesIntoOneGem_AndNoXpIsLost()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var pool = PickupPoolManager.Instance;
            var far = PlayerController.Instance.transform.position + new Vector3(200f, 0f, 200f); // out of pickup range

            int before = XPGem.ActiveGems.Count;
            long xpBefore = XPGem.ActiveGems.Sum(g => (long)g.XPValue);
            for (int i = 0; i < 600; i++) pool.SpawnGem(far + new Vector3(i % 30, 0f, i / 30), 10);
            yield return null;

            Assert.That(XPGem.ActiveGems.Count, Is.LessThanOrEqualTo(PickupPoolManager.MaxGemsOnGround + 1),
                "No more than 400 gems (plus the merged one) lie on the ground.");
            long xpAfter = XPGem.ActiveGems.Sum(g => (long)g.XPValue);
            Assert.That(xpAfter - xpBefore, Is.EqualTo(6000), "Every gem's XP is still on the ground, merged or not.");
            Assert.That(XPGem.ActiveGems.Max(g => g.XPValue), Is.GreaterThan(100), "One gem holds the merged XP.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// Bug MIS O1 (2026-09-30: the bot's level stayed at 123 for 15 minutes with 400 gems on the ground): the merged
        /// gem must be visible as a red gem (wiki: "accumulated into a single red Experience Gem") and give a player who
        /// reaches it all of its XP.
        /// </summary>
        [UnityTest]
        public IEnumerator TheMergedGem_IsARedGem_AndReachingItGivesAllItsXp()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<SoulHunter.Gameplay.Combat.AutoAttackWeapon>()) weapon.enabled = false;
            var pool = PickupPoolManager.Instance;
            var player = PlayerController.Instance;
            var far = player.transform.position + new Vector3(200f, 0f, 200f);
            for (int i = 0; i < 600; i++) pool.SpawnGem(far + new Vector3(i % 30, 0f, i / 30), 10);
            yield return null;

            var merged = XPGem.ActiveGems.OrderByDescending(g => g.XPValue).First();
            Assert.That(merged.XPValue, Is.GreaterThanOrEqualTo(2000), "About 200 gems' XP went into the merged gem.");
            var tier = merged.GetComponent<TieredVisual>();
            Assert.That(tier, Is.Not.Null);
            Assert.That(tier.CurrentTier, Is.EqualTo(2), "It shows the red (top) tier.");
            Assert.That(tier.Tiers[2] != null && tier.Tiers[2].activeInHierarchy, Is.True, "The red model is the one drawn.");
            Assert.That(merged.GetComponentsInChildren<Renderer>().Any(r => r.enabled && r.gameObject.activeInHierarchy), Is.True, "It is drawn.");

            // A player who walks to it collects it and gains its XP (levels).
            var experience = player.GetComponent<PlayerExperience>();
            int levelBefore = experience.CurrentLevel;
            player.transform.position = merged.transform.position + new Vector3(0.3f, 0f, 0f);
            var body = player.GetComponent<Rigidbody>();
            if (body != null) body.position = player.transform.position;
            yield return WaitUntilRealtime(() => !merged.isActiveAndEnabled, "the merged gem to be collected");
            Assert.That(experience.CurrentLevel, Is.GreaterThan(levelBefore + 3), "Its XP reached the player (several levels at once).");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
