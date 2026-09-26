using NUnit.Framework;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using UnityEngine;
using Drop = SoulHunter.Gameplay.Pickups.LightSourceDrops.Drop;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>VS light sources (vampire.survivors.wiki, Light source): drop table, spawn chance and the Soul Lantern prefab.</summary>
    public class LightSourceTests
    {
        [Test]
        public void DropTable_FollowsTheVsWeights_AndLevelGates()
        {
            // Level 1: coin 50, chicken 12, coin bag 10 (the level-gated entries are out).
            Assert.That(LightSourceDrops.Chance(Drop.GoldCoin, 1), Is.EqualTo(50f / 72f).Within(1e-4));
            Assert.That(LightSourceDrops.Chance(Drop.Chicken, 1), Is.EqualTo(12f / 72f).Within(1e-4));
            Assert.That(LightSourceDrops.Chance(Drop.TimeFreeze, 3), Is.EqualTo(0f));
            Assert.That(LightSourceDrops.Chance(Drop.TimeFreeze, 4), Is.GreaterThan(0f), "Orologion from level 4.");
            Assert.That(LightSourceDrops.Chance(Drop.RichCoinBag, 5), Is.GreaterThan(0f), "Rich Coin Bag from level 5.");
            Assert.That(LightSourceDrops.Chance(Drop.Magnet, 11), Is.EqualTo(0f));
            Assert.That(LightSourceDrops.Chance(Drop.Magnet, 12), Is.EqualTo(2f / 77f).Within(1e-4), "Vacuum from level 12.");

            // Pick walks the weights in order.
            Assert.That(LightSourceDrops.Pick(1, 0f), Is.EqualTo(Drop.GoldCoin));
            Assert.That(LightSourceDrops.Pick(1, 49.9f / 72f), Is.EqualTo(Drop.GoldCoin));
            Assert.That(LightSourceDrops.Pick(1, 50.1f / 72f), Is.EqualTo(Drop.Chicken));
            Assert.That(LightSourceDrops.Pick(1, 0.9999f), Is.EqualTo(Drop.CoinBag));
            Assert.That(LightSourceDrops.Pick(30, 0.9999f), Is.EqualTo(Drop.RichCoinBag));
        }

        [Test]
        public void SpawnChance_IsBaseTimesLuck_Capped()
        {
            Assert.That(LightSourceSpawner.SpawnChance(1f), Is.EqualTo(0.10f).Within(1e-5));
            Assert.That(LightSourceSpawner.SpawnChance(2.5f), Is.EqualTo(0.25f).Within(1e-5));
            Assert.That(LightSourceSpawner.SpawnChance(10f), Is.EqualTo(0.50f).Within(1e-5), "VS caps the chance.");
            Assert.That(LightSourceSpawner.MaxActive, Is.EqualTo(10));
        }

        [Test]
        public void SoulLantern_IsABreakableTenHpProp_ThatEveryWeaponCanHit_ButNoEnemy()
        {
            var prefab = GameContentCatalog.Load().SoulLantern;
            Assert.That(prefab, Is.Not.Null, "GameContentCatalog.SoulLantern is set (Soul Hunter/Content/Import Soul Lantern).");
            Assert.That(prefab.CompareTag("Enemy"), Is.True, "Tagged like an enemy, so tag-based weapons hit it.");
            Assert.That(prefab.layer, Is.EqualTo(LayerMask.NameToLayer("Enemy")), "On the Enemy layer, so area weapons hit it.");
            Assert.That(prefab.GetComponent<EnemyController>(), Is.Null, "Not an enemy: not chased, aimed at or counted as a kill.");
            Assert.That(prefab.GetComponent<LightSource>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<HealthController>().MaxHealth, Is.EqualTo(10));
            var trigger = prefab.GetComponent<Collider>();
            Assert.That(trigger != null && trigger.isTrigger, Is.True, "A trigger: it never blocks the player.");

            var instance = Object.Instantiate(prefab);
            try
            {
                var renderers = instance.GetComponentsInChildren<MeshRenderer>();
                Assert.That(renderers.Length, Is.GreaterThan(1));
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) if (r.name != "GroundGlow") bounds.Encapsulate(r.bounds);
                // Owner (2026-09-26): bigger than the first 1.3-unit version, which read too small in play.
                Assert.That(bounds.size.y, Is.InRange(1.6f, 2.1f), "About the hero's height (1.4x the Blender model).");
                Assert.That(bounds.min.y, Is.EqualTo(0f).Within(0.05f), "Stands on its pivot, like the heroes and the horde.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
