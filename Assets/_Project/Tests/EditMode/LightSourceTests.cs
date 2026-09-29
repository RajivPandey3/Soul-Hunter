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
            // Level 1: coin 50, chicken 12, coin bag 10, Hellfire Breath 1, Luck Charm 1 (the level-gated entries are out).
            Assert.That(LightSourceDrops.Chance(Drop.GoldCoin, 1), Is.EqualTo(50f / 74f).Within(1e-4));
            Assert.That(LightSourceDrops.Chance(Drop.Chicken, 1), Is.EqualTo(12f / 74f).Within(1e-4));
            Assert.That(LightSourceDrops.Chance(Drop.TimeFreeze, 3), Is.EqualTo(0f));
            Assert.That(LightSourceDrops.Chance(Drop.TimeFreeze, 4), Is.GreaterThan(0f), "Orologion from level 4.");
            Assert.That(LightSourceDrops.Chance(Drop.RichCoinBag, 5), Is.GreaterThan(0f), "Rich Coin Bag from level 5.");
            Assert.That(LightSourceDrops.Chance(Drop.Magnet, 11), Is.EqualTo(0f));
            // Level 12: 74 + Orologion 2 + Rich Coin Bag 1 + Soul Toll 1 + Vacuum 2 = 80.
            Assert.That(LightSourceDrops.Chance(Drop.Magnet, 12), Is.EqualTo(2f / 80f).Within(1e-4), "Vacuum from level 12.");

            // Pick walks the weights in order.
            Assert.That(LightSourceDrops.Pick(1, 0f), Is.EqualTo(Drop.GoldCoin));
            Assert.That(LightSourceDrops.Pick(1, 49.9f / 74f), Is.EqualTo(Drop.GoldCoin));
            Assert.That(LightSourceDrops.Pick(1, 50.1f / 74f), Is.EqualTo(Drop.Chicken));
            Assert.That(LightSourceDrops.Pick(1, 71.9f / 74f), Is.EqualTo(Drop.CoinBag));
            Assert.That(LightSourceDrops.Pick(1, 0.9999f), Is.EqualTo(Drop.LuckCharm));
            // Level 30 (all 81): ... Rich Coin Bag ends at 77.
            Assert.That(LightSourceDrops.Pick(30, 76.5f / 81f), Is.EqualTo(Drop.RichCoinBag));
        }

        [Test]
        public void Luck_RaisesEveryDrop_ButGoldCoinAndCoinBag()
        {
            // VS wiki (Luck, "Higher rarity pickups"): itemWeight = rarity x totalLuck, except Gold Coin and Coin Bag.
            Assert.That(LightSourceDrops.Weight(Drop.GoldCoin, 30, 2f), Is.EqualTo(50f));
            Assert.That(LightSourceDrops.Weight(Drop.CoinBag, 30, 2f), Is.EqualTo(10f));
            Assert.That(LightSourceDrops.Weight(Drop.Chicken, 30, 2f), Is.EqualTo(24f));
            Assert.That(LightSourceDrops.Weight(Drop.Magnet, 30, 2f), Is.EqualTo(4f));
            Assert.That(LightSourceDrops.Weight(Drop.Magnet, 11, 2f), Is.EqualTo(0f), "Still gated by level.");
            // Level 1, Luck 2: coin 50, bag 10, chicken 24, Hellfire Breath 2, Luck Charm 2 = 88.
            Assert.That(LightSourceDrops.Chance(Drop.Chicken, 1, 2f), Is.EqualTo(24f / 88f).Within(1e-5));
            Assert.That(LightSourceDrops.Chance(Drop.GoldCoin, 1, 2f), Is.EqualTo(50f / 88f).Within(1e-5));
            Assert.That(LightSourceDrops.Pick(1, 2f, 0.999f), Is.EqualTo(Drop.LuckCharm));
            Assert.That(LightSourceDrops.Chance(Drop.Chicken, 1), Is.EqualTo(12f / 74f).Within(1e-5), "Luck 1 keeps the plain table.");
        }

        [Test]
        public void EachLevel_UsesItsVsStageLanternNumbers()
        {
            // VS wiki (Light source, destructible table), following the stage-bonus climb.
            var expected = new (float chance, float max, int count)[]
            {
                (0.10f, 0.50f, 10), (0.075f, 0.50f, 10), (0.20f, 0.60f, 10), (0.075f, 0.50f, 10),
                (0.20f, 0.80f, 20), (0.20f, 0.80f, 20), (0.20f, 0.80f, 20), (0.20f, 0.80f, 20), (0.20f, 0.80f, 20), (0.20f, 0.80f, 20),
            };
            for (int stage = 1; stage <= 10; stage++)
            {
                var numbers = LightSourceSpawner.ForStage(stage);
                Assert.That(numbers.Chance, Is.EqualTo(expected[stage - 1].chance).Within(1e-5), $"Level {stage} chance");
                Assert.That(numbers.MaxChance, Is.EqualTo(expected[stage - 1].max).Within(1e-5), $"Level {stage} cap");
                Assert.That(numbers.Max, Is.EqualTo(expected[stage - 1].count), $"Level {stage} lanterns at once");
            }
            Assert.That(LightSourceSpawner.SpawnChance(LightSourceSpawner.ForStage(5), 3f), Is.EqualTo(0.60f).Within(1e-5));
            Assert.That(LightSourceSpawner.SpawnChance(LightSourceSpawner.ForStage(5), 10f), Is.EqualTo(0.80f).Within(1e-5), "Cappella Magna caps at 80%.");
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
