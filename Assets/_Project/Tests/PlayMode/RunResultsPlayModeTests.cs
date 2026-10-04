using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;
using SoulHunter.Gameplay.Weapons;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// The VS Results screen's data in the real game (vampire.survivors.wiki/w/Results): per-weapon damage, time used and
    /// DPS; pickups counted; every section shown with its yellow rules; Shadow Kael's mirrored weapons left out.
    /// </summary>
    public class RunResultsPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator EachWeapon_GetsItsOwnHits_TimeUsed_AndDps()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            var stats = RunStatsTracker.Instance;
            var player = PlayerController.Instance;

            var whip = Record(stats, typeof(WhipWeapon));
            Assert.IsNotNull(whip, "Kael's starting Whip has a Results row from the start. " + Errors());
            Assert.AreEqual("Whip", whip.Name);
            Assert.IsNotNull(whip.Icon, "The Whip row has its icon.");
            Assert.AreEqual(8, whip.MaxLevel, "Whip max level (wiki: 8).");

            // Real hits: enemies next to the hero; the Whip's own damage is credited to it.
            var prefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            var enemies = new List<GameObject>();
            for (int i = 0; i < 6; i++)
                enemies.Add(Object.Instantiate(prefab, player.transform.position + Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward * 1.6f, Quaternion.identity));
            foreach (var enemy in enemies) { var touch = enemy.GetComponent<TouchDamage>(); if (touch != null) touch.enabled = false; }
            yield return WaitUntilRealtime(() => whip.Damage > 0, "the Whip's hits to be credited to it");

            float whipSeconds = whip.SecondsUsed;
            Assert.Greater(whipSeconds, 0f, "Time used counts while the Whip is held.");
            Assert.AreEqual(whip.Damage / whip.SecondsUsed, whip.Dps, 1e-6, "DPS = damage / time used.");

            // A weapon gained later starts its own row and its own clock.
            WeaponManager.Instance.GiveWeapon(UpgradeData.UpgradeType.Knife, 1);
            yield return null;
            var knife = Record(stats, typeof(KnifeWeapon));
            Assert.IsNotNull(knife, "A new weapon gets a Results row.");
            Assert.AreEqual("Knife", knife.Name);
            yield return new WaitForSeconds(0.3f);
            Assert.Less(knife.SecondsUsed, whip.SecondsUsed, "The Knife's time used counts from when it was gained.");

            // Shadow Kael's mirrored copy is not the player's: its hits are left out.
            var mirror = Object.Instantiate(WeaponManager.Instance.GetWeaponObject(UpgradeData.UpgradeType.Whip), enemies[0].transform);
            mirror.SetActive(false);
            var mirrorWeapon = mirror.GetComponent<AutoAttackWeapon>();
            Assert.IsTrue(mirrorWeapon.IsHeldByEnemy);
            int rows = stats.Weapons.Count;
            stats.RecordHit(mirrorWeapon, null, 999);
            Assert.AreEqual(rows, stats.Weapons.Count, "A boss-held weapon gets no Results row.");

            // A hit dealt through a damage scope with no weapon is credited to its named source (Soul Burst, rail carts).
            using (DamageSource.From(null, "Soul Burst"))
                enemies.First(e => e != null && e.activeInHierarchy).GetComponent<HealthController>()
                    .TakeDamage(new DamagePacket(7, Vector3.zero, Vector3.zero, DamageType.Spectral));
            var burst = stats.Weapons.FirstOrDefault(r => r.Weapon == null && r.Name == "Soul Burst");
            Assert.IsNotNull(burst, "Soul Burst has its own row.");
            Assert.AreEqual(7d, burst.Damage, 1e-6);
            foreach (var enemy in enemies) if (enemy != null) Object.Destroy(enemy);
        }

        [UnityTest]
        public IEnumerator PentagramErase_CreditsEachEnemysMaxHealth()
        {
            // vampire.survivors.wiki/w/Pentagram: it "always deals damage equal to the max health of the enemy".
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            // The opening horde is already on screen; take it away so only these three are erased.
            foreach (var other in EnemyController.ActiveEnemies.ToList()) if (other != null) other.gameObject.SetActive(false);
            yield return null;
            var prefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            var enemies = new List<HealthController>();
            for (int i = 0; i < 3; i++)
            {
                var enemy = Object.Instantiate(prefab, player.transform.position + Quaternion.Euler(0f, i * 120f, 0f) * Vector3.forward * 4f, Quaternion.identity);
                var touch = enemy.GetComponent<TouchDamage>();
                if (touch != null) touch.enabled = false;
                enemies.Add(enemy.GetComponent<HealthController>());
            }
            yield return null;
            Assert.AreEqual(3, EnemyController.ActiveEnemies.Count, "Only the three test enemies are alive.");
            int expected = enemies.Sum(h => h.MaxHealth);
            WeaponManager.Instance.GiveWeapon(UpgradeData.UpgradeType.Pentagram, 1);
            var stats = RunStatsTracker.Instance;
            RunStatsTracker.WeaponRecord pentagram = null;
            yield return WaitUntilRealtime(() => (pentagram = Record(stats, typeof(PentagramWeapon))) != null && pentagram.Damage > 0,
                "the Pentagram's erase to be credited to it");
            Assert.IsTrue(enemies.All(h => h == null || !h.gameObject.activeInHierarchy || h.CurrentHealth <= 0), "Every enemy on screen was erased.");
            Assert.AreEqual(expected, pentagram.Damage, 1e-6, "Each erased enemy counts its max health.");
        }

        [UnityTest]
        public IEnumerator Pickups_AreCountedByName()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var stats = RunStatsTracker.Instance;
            var at = PlayerController.Instance.transform.position;
            PickupPoolManager.Instance.SpawnGold(at, 1);
            PickupPoolManager.Instance.SpawnGold(at, 1);
            PickupPoolManager.Instance.SpawnGold(at, 10);
            PickupPoolManager.Instance.SpawnChicken(at);
            yield return WaitUntilRealtime(() => stats.PickupCount(PickupNames.Coin) == 2 && stats.PickupCount(PickupNames.CoinBag) == 1
                && stats.PickupCount(PickupNames.Chicken) == 1, "the coins, the bag and the chicken to be counted",
                () => string.Join(", ", stats.Pickups.Select(p => p.Key + " x" + p.Value)));
        }

        [UnityTest]
        public IEnumerator TheScreen_ShowsEverySection_WithTheYellowRules()
        {
            yield return BootIntoGameplay(save =>
            {
                save.PowerUpRanks.Clear();
                PowerUps.SetRank(save, PowerUpId.Might, 2);
            });
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            var stats = RunStatsTracker.Instance;
            var manager = WeaponManager.Instance;
            manager.GiveWeapon(UpgradeData.UpgradeType.Whip, 8);
            manager.GiveWeapon(UpgradeData.UpgradeType.Knife, 2);
            yield return null;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            var whip = Record(stats, typeof(WhipWeapon));
            var knife = Record(stats, typeof(KnifeWeapon));
            stats.RecordHit(whip.Weapon, null, 5000); // most damage
            whip.SecondsUsed = 100f; knife.SecondsUsed = 1f;
            stats.RecordHit(knife.Weapon, null, 300); // most DPS (300/s against 50/s)
            stats.RecordUnlock("Character: Corvan");
            stats.RecordPickup(PickupNames.Chicken);

            var health = PlayerController.Instance.GetComponent<HealthController>();
            for (int i = 0; i < 10 && !GameSessionManager.Instance.IsRunFinished; i++) { health.IsInvincible = false; health.KillSilently(); yield return null; }
            yield return null;
            var screen = Object.FindFirstObjectByType<GameOverUI>().Screen;
            Assert.IsNotNull(screen, "Results screen after death. " + Errors());
            var results = screen.Results;

            StringAssert.StartsWith("Level 1:", results.StageLine);
            Assert.AreEqual("Normal", results.Mode);
            Assert.AreEqual("Kael", results.Character);
            Assert.That(results.PowerUps, Has.Member(new KeyValuePair<string, int>("Might", 2)), "Active PowerUps with their rank.");
            Assert.That(results.Pickups, Has.Member(new KeyValuePair<string, int>(PickupNames.Chicken, 1)));
            StringAssert.Contains("Might", screen.ListsText.text);
            StringAssert.Contains(PickupNames.Chicken, screen.ListsText.text);

            int whipRow = results.Weapons.FindIndex(r => r.Name == "Whip");
            int knifeRow = results.Weapons.FindIndex(r => r.Name == "Knife");
            Assert.AreEqual(whipRow, results.TopDamageRow, "The most damage is the Whip.");
            Assert.AreEqual(knifeRow, results.TopDpsRow, "The most DPS is the Knife.");
            Assert.AreEqual(RunResultsScreen.Yellow, screen.WeaponRows[whipRow].Damage.color, "Top damage in yellow.");
            Assert.AreEqual(RunResultsScreen.Yellow, screen.WeaponRows[knifeRow].Dps.color, "Top DPS in yellow.");
            Assert.AreNotEqual(RunResultsScreen.Yellow, screen.WeaponRows[knifeRow].Damage.color);
            Assert.AreEqual(RunResultsScreen.Yellow, screen.WeaponRows[whipRow].Level.color, "Whip 8 is max: yellow level.");
            Assert.AreEqual("5.00k", screen.WeaponRows[whipRow].Damage.text, "Damage with SI prefixes.");

            int whipItem = results.Items.FindIndex(i => i.Name == "Whip");
            int knifeItem = results.Items.FindIndex(i => i.Name == "Knife");
            Assert.GreaterOrEqual(whipItem, 0, "The Whip is in the items list.");
            Assert.AreEqual(RunResultsScreen.Yellow, screen.ItemLevelTexts[whipItem].color, "A max item level is yellow.");
            Assert.AreEqual(SoulHunter.Gameplay.UI.UITheme.Text, screen.ItemLevelTexts[knifeItem].color, "A non-max item level is in the theme's text colour.");

            Assert.IsNotNull(screen.UnlocksButton, "A run with an unlock has the unlocks button.");
            Assert.IsFalse(screen.UnlocksPanel.activeSelf);
            Click(screen.UnlocksButton, "SHOW UNLOCKS");
            Assert.IsTrue(screen.UnlocksPanel.activeSelf, "The button shows the unlocks.");
            Click(screen.UnlocksButton, "HIDE UNLOCKS");
            Assert.IsFalse(screen.UnlocksPanel.activeSelf, "and hides them.");
        }

        private static RunStatsTracker.WeaponRecord Record(RunStatsTracker stats, System.Type weaponType) =>
            stats.Weapons.FirstOrDefault(r => r.Weapon != null && r.Weapon.GetType() == weaponType);
    }
}
