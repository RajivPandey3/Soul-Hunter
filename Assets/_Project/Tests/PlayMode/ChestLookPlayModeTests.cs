using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Level 1 plan step 3 (2026-10-01): VS chest types (wiki Treasure_Chest): a chest from before 10:00 is bronze and
    /// cannot evolve, one from 10:00 on is silver and can, an Arcana chest has its own look. Soul Hunter recolours the
    /// approved Soul Reliquary (ChestLook); the opening screen shows the same look.
    /// </summary>
    public class ChestLookPlayModeTests : GameplayPlayModeTestBase
    {
        private static void AssertColour(Color actual, Color expected, string what) =>
            Assert.That(Vector4.Distance(actual, expected), Is.LessThan(0.02f), $"{what}: {actual} (expected {expected}).");

        private static ChestPickup KillChestCarrier(GameObject enemyPrefab, bool arcana)
        {
            var player = PlayerController.Instance;
            var enemy = Object.Instantiate(enemyPrefab, player.transform.position + new Vector3(6f, 0f, 0f), Quaternion.identity);
            var drop = enemy.GetComponent<EnemyDrop>();
            drop.DropChestThisLife();
            if (arcana) drop.CarryArcanaThisLife();
            var before = Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None).Where(c => c.isActiveAndEnabled).ToList();
            enemy.GetComponent<HealthController>().TakeDamage(new DamagePacket(1000000, enemy.transform.position, Vector3.zero));
            return Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None).FirstOrDefault(c => c.isActiveAndEnabled && !before.Contains(c));
        }

        [UnityTest]
        public IEnumerator ChestsDroppedBeforeTenMinutes_AreBronze_FromTen_Silver_Arcana_Violet()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            var enemyPrefab = GetPrivateField<System.Collections.Generic.List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            var progression = LevelProgressionManager.Instance;

            progression.SkipTo(5f * 60f);
            yield return null;
            var bronze = KillChestCarrier(enemyPrefab, arcana: false);
            Assert.That(bronze, Is.Not.Null, "The 5:00 carrier dropped a chest. " + Errors());
            Assert.That(bronze.CanEvolve, Is.False, "VS: a chest from before 10:00 cannot evolve.");
            Assert.That(bronze.Kind, Is.EqualTo(ChestKind.Bronze));
            AssertColour(ChestLook.BandColour(bronze.transform), ChestLook.BronzeBands, "Bronze bands");
            bronze.gameObject.SetActive(false);

            progression.SkipTo(10f * 60f + 5f);
            yield return null;
            var silver = KillChestCarrier(enemyPrefab, arcana: false);
            Assert.That(silver, Is.Not.Null, "The 10:00 carrier dropped a chest.");
            Assert.That(silver.CanEvolve, Is.True, "VS: a chest from 10:00 on can evolve.");
            Assert.That(silver.Kind, Is.EqualTo(ChestKind.Silver));
            AssertColour(ChestLook.BandColour(silver.transform), ChestLook.SilverBands, "Silver bands");
            silver.gameObject.SetActive(false);

            var arcanaChest = KillChestCarrier(enemyPrefab, arcana: true);
            Assert.That(arcanaChest, Is.Not.Null, "The Arcana carrier dropped a chest.");
            Assert.That(arcanaChest.Kind, Is.EqualTo(ChestKind.Arcana));
            AssertColour(ChestLook.BandColour(arcanaChest.transform), ChestLook.ArcanaBands, "Arcana bands");
            AssertColour(ChestLook.SoulGlow(arcanaChest.transform), ChestLook.ArcanaSoulGlow * 2f, "Arcana seam glow");

            // A pooled chest reused later starts as a plain silver chest again (not the old look).
            arcanaChest.ResetPickup();
            Assert.That(arcanaChest.Kind, Is.EqualTo(ChestKind.Silver));
            AssertColour(ChestLook.BandColour(arcanaChest.transform), ChestLook.SilverBands, "Reset to silver");
            Assert.That(ChestLook.SoulGlow(arcanaChest.transform), Is.Not.EqualTo(ChestLook.ArcanaSoulGlow * 2f), "The violet glow is gone.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator TheOpeningScreen_ShowsTheChestInItsOwnLook()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var pool = PickupPoolManager.Instance;
            foreach (var kind in new[] { ChestKind.Bronze, ChestKind.Silver, ChestKind.Arcana })
            {
                var stage = ChestPreviewStage.Create(pool.ChestPrefab, pool.GoldCoinPrefab, kind == ChestKind.Arcana, kind);
                try
                {
                    yield return null;
                    AssertColour(ChestLook.BandColour(stage.transform), ChestLook.BandsFor(kind), $"{kind} preview bands");
                }
                finally { Object.Destroy(stage.gameObject); }
            }
            // The shared material is never changed (other chests keep their own look).
            var shared = pool.ChestPrefab.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).First(m => m != null && m.name.Contains("Chest_Iron"));
            AssertColour(shared.GetColor("_BaseColor"), new Color(0.1f, 0.1f, 0.12f), "The iron material itself");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
