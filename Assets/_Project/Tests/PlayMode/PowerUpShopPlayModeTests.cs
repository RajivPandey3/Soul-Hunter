using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
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
    /// The VS PowerUp shop in the real game (owner, 2026-09-30: "everwhere follow DOT VS ONLY"; wiki PowerUps and each
    /// PowerUp's page): the MainMenu screen sells every PowerUp at the VS price up to its VS rank and refunds them, and a
    /// run gets every effect.
    /// </summary>
    public class PowerUpShopPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator TheShop_ListsEveryPowerUp_SellsAtTheVsPrice_UpToTheVsRank_AndRefunds()
        {
            SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            var save = GameServices.Instance.Get<SaveService>();
            save.UseFile(System.IO.Path.Combine(Application.temporaryCachePath, "playmode_test_save.json"));
            save.CurrentData.Currency = 50000;
            save.CurrentData.PowerUpRanks.Clear();
            Object.FindFirstObjectByType<MainMenuNavigation>().ShowPowerUps();
            yield return null;

            var shop = Object.FindFirstObjectByType<PowerUpMenuUI>();
            Assert.That(shop, Is.Not.Null);
            foreach (var info in PowerUps.All)
                Assert.That(shop.CellFor(info.Id) != null && shop.CellFor(info.Id).gameObject.activeInHierarchy, Is.True, $"A cell for {info.Id}. " + Errors());

            shop.Select(PowerUpId.Recovery);
            StringAssert.Contains("Recovers 0.1 HP per rank", shop.InfoText);
            StringAssert.Contains("Price: 200", shop.InfoText, "Nothing bought: the initial price.");
            int gold = save.CurrentData.Currency;
            for (int i = 0; i < 7; i++) shop.BuyButton.onClick.Invoke();
            Assert.That(PowerUps.Rank(save.CurrentData, PowerUpId.Recovery), Is.EqualTo(5), "Stops at the VS max rank.");
            int paid = 200 + (400 + 22) + (600 + 24) + (800 + 26) + (1000 + 29);
            Assert.That(save.CurrentData.Currency, Is.EqualTo(gold - paid), "Each rank at InitialPrice x (1 + Bought) + fee.");
            StringAssert.Contains("MAX", shop.InfoText);
            Assert.That(shop.BuyButton.interactable, Is.False);

            shop.Select(PowerUpId.Might);
            int fee = (int)System.Math.Floor(20 * System.Math.Pow(1.1, 5));
            StringAssert.Contains($"Price: {200 + fee}", shop.InfoText, "Another PowerUp now carries the fee of 5 ranks bought.");

            shop.RefundButton.onClick.Invoke();
            yield return null;
            Assert.That(PowerUps.TotalBought(save.CurrentData), Is.EqualTo(0));
            Assert.That(save.CurrentData.Currency, Is.EqualTo(gold), "Refund gives every rank's gold back.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator ARun_GetsEveryPowerUp()
        {
            yield return BootIntoGameplay(save =>
            {
                save.PowerUpRanks.Clear();
                save.SoulEggs?.Clear();
                PowerUpUnlocks.UnlockEverything(save); // VS locked PowerUps (2026-10-01)
                foreach (var info in PowerUps.All) PowerUps.SetRank(save, info.Id, info.MaxRank);
            });
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var stats = PlayerController.Instance.Stats;
            var kael = GameContentCatalogKael();
            // Kael's base plus every PowerUp at its max (Omni adds 10% to Might, Area, Speed and Duration).
            Assert.That(stats.Might, Is.EqualTo(kael.StartingMight + 0.25f + 0.10f).Within(0.001f), "Might");
            Assert.That(stats.Armor, Is.EqualTo(kael.StartingArmor + 3), "Armor");
            Assert.That(stats.Regen, Is.EqualTo(kael.StartingRecovery + 0.5f).Within(0.001f), "Recovery");
            Assert.That(stats.Cooldown, Is.EqualTo(kael.StartingCooldown - 0.05f).Within(0.001f), "Cooldown");
            Assert.That(stats.Area, Is.EqualTo(kael.StartingArea + 0.10f + 0.10f).Within(0.001f), "Area");
            Assert.That(stats.Amount, Is.EqualTo(kael.StartingAmount + 1), "Amount");
            Assert.That(stats.Luck, Is.EqualTo(kael.StartingLuck + 0.30f).Within(0.001f), "Luck");
            Assert.That(stats.Greed, Is.EqualTo(kael.StartingGreed + 0.50f).Within(0.001f), "Greed");
            Assert.That(stats.Curse, Is.EqualTo(kael.StartingCurse + 0.50f).Within(0.001f), "Curse");
            Assert.That(stats.Revivals, Is.EqualTo(kael.StartingRevivals + 1), "Revival");
            Assert.That(stats.Charm, Is.EqualTo(100), "Charm");
            Assert.That(stats.Defang, Is.EqualTo(0.15f).Within(0.001f), "Defang");
            Assert.That(stats.Preserve, Is.EqualTo(0.5f).Within(0.001f), "Preserve");
            Assert.That(PlayerController.Instance.GetComponent<HealthController>().MaxHealth, Is.EqualTo(Mathf.RoundToInt(kael.BaseMaxHealth * 1.331f)).Within(1), "Max Health");
            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            Assert.That(levelUp.RerollsLeft, Is.EqualTo(10), "Reroll: 2 per rank, from none (VS).");
            Assert.That(levelUp.SkipsLeft, Is.EqualTo(10));
            Assert.That(levelUp.BanishesLeft, Is.EqualTo(10));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator WithoutPowerUps_ARunHasNoRerollSkipOrBanish_LikeVs()
        {
            yield return BootIntoGameplay(save => { save.PowerUpRanks.Clear(); save.SoulEggs?.Clear(); });
            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            Assert.That(levelUp.RerollsLeft + levelUp.SkipsLeft + levelUp.BanishesLeft, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator DefangedEnemies_DealNoDamage_AndLookGreen()
        {
            yield return BootIntoGameplay(save => save.PowerUpRanks.Clear());
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList()) if (stray != null) stray.gameObject.SetActive(false);
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            var player = PlayerController.Instance;
            var health = player.GetComponent<HealthController>();
            var prefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            var enemy = Object.Instantiate(prefab, player.transform.position + Vector3.forward * 0.3f, Quaternion.identity);
            var controller = enemy.GetComponent<EnemyController>();
            controller.SetDefanged(true);
            int before = health.CurrentHealth;
            yield return new WaitForSeconds(1.5f);
            Assert.That(health.CurrentHealth, Is.EqualTo(before), "A defanged enemy touching the hero deals no damage.");
            var block = new MaterialPropertyBlock();
            var rendererWithTint = enemy.GetComponentsInChildren<Renderer>().FirstOrDefault(r => { r.GetPropertyBlock(block); return block.GetColor("_BaseColor") == EnemyController.DefangTint; });
            Assert.That(rendererWithTint, Is.Not.Null, "It looks green (Show Defanged Enemies is on by default).");

            controller.SetDefanged(false);
            yield return new WaitForSeconds(1.5f);
            Assert.That(health.CurrentHealth, Is.LessThan(before), "Without Defang the same enemy hurts.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Skip_GivesExperience_AndPreserveCanKeepACharge()
        {
            yield return BootIntoGameplay(save =>
            {
                save.PowerUpRanks.Clear();
                save.SoulEggs?.Clear();
                PowerUpUnlocks.UnlockEverything(save);
                PowerUps.SetRank(save, PowerUpId.Skip, 5);
                PowerUps.SetRank(save, PowerUpId.Reroll, 5);
                PowerUps.SetRank(save, PowerUpId.Preserve, 5);
            });
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            var experience = PlayerController.Instance.GetComponent<PlayerExperience>();
            int level = experience.CurrentLevel;
            experience.AddXP(experience.XPToNextLevel - experience.CurrentXP);
            yield return WaitUntilRealtime(() => levelUp.IsLevelUpActive, "a level-up");
            int xpBefore = experience.CurrentXP;
            int toNext = experience.XPToNextLevel;
            Assert.That(levelUp.Skip(), Is.True);
            Assert.That(experience.CurrentXP - xpBefore, Is.GreaterThanOrEqualTo(Mathf.RoundToInt(toNext * LevelUpManager.SkipExperienceShare) - 1),
                "VS Skip: experience instead (20% of the next level).");

            // Preserve 50%: over many rerolls some charges are kept, some used.
            int used = 0, kept = 0;
            for (int i = 0; i < 40 && levelUp.RerollsLeft > 0; i++)
            {
                experience.AddXP(experience.XPToNextLevel - experience.CurrentXP);
                yield return WaitUntilRealtime(() => levelUp.IsLevelUpActive, "a level-up");
                int left = levelUp.RerollsLeft;
                levelUp.Reroll();
                if (levelUp.RerollsLeft == left) kept++; else used++;
                levelUp.Skip();
                yield return null;
            }
            Assert.That(kept, Is.GreaterThan(0), "Preserve kept some rerolls.");
            Assert.That(used, Is.GreaterThan(0), "And used some.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static SoulHunter.Gameplay.Data.CharacterData GameContentCatalogKael() =>
            SoulHunter.Gameplay.Data.GameContentCatalog.FindCharacterExact("Kael");
    }
}
