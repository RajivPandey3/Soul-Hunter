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
            yield return BootIntoGameplay(save => { save.MetaMaxHealthLevel = 3; save.MetaRecoveryLevel = 5; save.SoulEggs?.Clear(); });
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

        [UnityTest]
        public IEnumerator TheShop_SellsMaxHealthAndRecovery_UpToTheirVsRanks()
        {
            var load = SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            var save = GameServices.Instance.Get<SaveService>();
            save.UseFile(System.IO.Path.Combine(Application.temporaryCachePath, "playmode_test_save.json"));
            save.CurrentData.Currency = 5000;
            save.CurrentData.MetaMaxHealthLevel = 0;
            save.CurrentData.MetaRecoveryLevel = 0;
            Object.FindFirstObjectByType<MainMenuNavigation>().ShowPowerUps();
            yield return null;

            var shop = Object.FindFirstObjectByType<PowerUpMenuUI>();
            Assert.That(shop, Is.Not.Null);
            Assert.That(shop.MaxHealthButton, Is.Not.Null, "The shop has a Max Health button. " + Errors());
            Assert.That(shop.RecoveryButton, Is.Not.Null, "The shop has a Recovery button.");
            Assert.That(shop.MaxHealthButton.gameObject.activeInHierarchy && shop.RecoveryButton.gameObject.activeInHierarchy, Is.True);

            for (int i = 0; i < 5; i++) shop.MaxHealthButton.onClick.Invoke();
            for (int i = 0; i < 7; i++) shop.RecoveryButton.onClick.Invoke();
            Assert.That(save.CurrentData.MetaMaxHealthLevel, Is.EqualTo(MetaPowerUps.MaxHealthRanks), "Max Health stops at rank 3.");
            Assert.That(save.CurrentData.MetaRecoveryLevel, Is.EqualTo(MetaPowerUps.RecoveryRanks), "Recovery stops at rank 5.");
            Assert.That(save.CurrentData.Currency, Is.EqualTo(5000 - 3 * MetaPowerUps.MaxHealthPrice - 5 * MetaPowerUps.RecoveryPrice), "Only real purchases cost gold.");
            Assert.That(shop.MaxHealthButton.interactable || shop.RecoveryButton.interactable, Is.False, "Maxed buttons are switched off.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
