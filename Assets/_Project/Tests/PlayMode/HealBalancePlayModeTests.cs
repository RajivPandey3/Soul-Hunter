using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using SoulHunter.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Heal balance pass (owner, 2026-09-29; vampire.survivors.wiki PowerUps and Recovery pages): the Max Health and
    /// Recovery PowerUps can be bought in the shop up to their VS ranks, and a run starts with them: more max health at
    /// full health, health coming back each second, and chickens healing more.
    /// </summary>
    public class HealBalancePlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator ARun_StartsWithTheHealingPowerUps()
        {
            yield return BootIntoGameplay(save =>
            {
                save.PowerUpRanks.Clear();
                PowerUps.SetRank(save, PowerUpId.MaxHealth, 3);
                PowerUps.SetRank(save, PowerUpId.Recovery, 5);
                save.SoulEggs?.Clear();
            });
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList()) if (stray != null) stray.gameObject.SetActive(false);
            yield return null;
            var player = PlayerController.Instance;
            var health = player.GetComponent<HealthController>();
            Assert.That(health.MaxHealth, Is.EqualTo(133), "Kael's 100 x 1.331 (VS Max Health rank 3).");
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth), "The run starts at full health.");
            Assert.That(player.Stats.Regen, Is.GreaterThanOrEqualTo(0.5f - 1e-4f), "VS Recovery rank 5: +0.5 HP per second.");

            health.TakeDamage(new DamagePacket(60, player.transform.position, Vector3.zero, DamageType.Normal));
            yield return null;
            int hurt = health.CurrentHealth;
            yield return new WaitForSeconds(4.2f);
            Assert.That(health.CurrentHealth, Is.GreaterThanOrEqualTo(hurt + 2), "Health comes back each second.");

            int before = health.CurrentHealth;
            var chicken = PickupPoolManager.Instance.SpawnChicken(player.transform.position + Vector3.forward);
            yield return WaitUntilRealtime(() => !chicken.activeInHierarchy, "the chicken to be picked up");
            Assert.That(health.CurrentHealth - before, Is.GreaterThanOrEqualTo(HealthPickup.HealWithRecovery(30, player.Stats.Regen) - 1),
                "A chicken heals 30 x (1 + Recovery).");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
