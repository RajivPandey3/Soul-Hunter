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
    }
}
