using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.VFX;
using UnityEngine;
using Drop = SoulHunter.Gameplay.Pickups.LightSourceDrops.Drop;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// The four extra light-source pickups (VS Light source, Rosary, Nduja Fritta Tanto, Little Clover and Gilded Clover
    /// pages on vampire.survivors.wiki, fetched 2026-09-27), under Soul Hunter's own names: Soul Toll, Hellfire Breath,
    /// Luck Charm and Gilded Charm.
    /// </summary>
    public class ExtraPickupTests
    {
        [Test]
        public void DropTable_HasTheFourPickups_WithTheVsWeightsAndLevels()
        {
            Assert.That(LightSourceDrops.Entry(Drop.ScreenWipe), Is.EqualTo((1, 8)), "Soul Toll: weight 1 from level 8.");
            Assert.That(LightSourceDrops.Entry(Drop.FireBreath), Is.EqualTo((1, 0)), "Hellfire Breath: weight 1 from level 0.");
            Assert.That(LightSourceDrops.Entry(Drop.LuckCharm), Is.EqualTo((1, 0)), "Luck Charm: weight 1 from level 0.");
            Assert.That(LightSourceDrops.Entry(Drop.GildedCharm), Is.EqualTo((1, 30)), "Gilded Charm: weight 1 from level 30.");

            // Level 1 table: 50 + 12 + 10 + 1 + 1 = 74.
            Assert.That(LightSourceDrops.Chance(Drop.FireBreath, 1), Is.EqualTo(1f / 74f).Within(1e-5));
            Assert.That(LightSourceDrops.Chance(Drop.LuckCharm, 1), Is.EqualTo(1f / 74f).Within(1e-5));
            Assert.That(LightSourceDrops.Chance(Drop.ScreenWipe, 7), Is.EqualTo(0f));
            // Level 8: 74 + Orologion 2 + Rich Coin Bag 1 + Soul Toll 1 = 78.
            Assert.That(LightSourceDrops.Chance(Drop.ScreenWipe, 8), Is.EqualTo(1f / 78f).Within(1e-5));
            Assert.That(LightSourceDrops.Chance(Drop.GildedCharm, 29), Is.EqualTo(0f));
            // Level 30: everything, 81.
            Assert.That(LightSourceDrops.Chance(Drop.GildedCharm, 30), Is.EqualTo(1f / 81f).Within(1e-5));
        }

        [Test]
        public void GoldFever_TimerMaths_FollowsVs()
        {
            Assert.That(GoldFever.StartSeconds, Is.EqualTo(10f));
            Assert.That(GoldFever.MaxSeconds, Is.EqualTo(10f));

            Assert.That(GoldFever.ExtensionFor(LightSourceDrops.GoldCoinValue, false), Is.EqualTo(0.5f));
            Assert.That(GoldFever.ExtensionFor(LightSourceDrops.CoinBagValue, false), Is.EqualTo(5f));
            Assert.That(GoldFever.ExtensionFor(LightSourceDrops.RichCoinBagValue, false), Is.EqualTo(10f));
            Assert.That(GoldFever.ExtensionFor(1, true), Is.EqualTo(0.01f), "A coin the fever itself dropped adds 0.01 s.");

            Assert.That(GoldFever.Extend(4f, 0.5f), Is.EqualTo(4.5f).Within(1e-5));
            Assert.That(GoldFever.Extend(4f, 10f), Is.EqualTo(10f), "Capped at 10 s.");
            Assert.That(GoldFever.Extend(9.8f, 0.5f), Is.EqualTo(10f));
            Assert.That(GoldFever.Extend(0f, 5f), Is.EqualTo(0f), "A coin does not start a fever that is over.");

            Assert.That(GoldFever.Tick(10f, 0.25f), Is.EqualTo(9.75f).Within(1e-5));
            Assert.That(GoldFever.Tick(0.1f, 1f), Is.EqualTo(0f));

            Assert.That(GoldFever.RollsCoin(0f), Is.True);
            Assert.That(GoldFever.RollsCoin(0.749f), Is.True);
            Assert.That(GoldFever.RollsCoin(0.75f), Is.False, "75% chance per kill.");
        }

        [Test]
        public void FeverCoin_IsHalfAGoldBeforeGreed_AtLeastOne()
        {
            Assert.That(GoldFever.FeverCoinBaseGold, Is.EqualTo(0.5f));
            Assert.That(GoldRewards.ApplyGreedFraction(0.5f, 1f, 1f), Is.EqualTo(1), "0.5 rounds to at least 1.");
            Assert.That(GoldRewards.ApplyGreedFraction(0.5f, 4f, 1f), Is.EqualTo(2));
            Assert.That(GoldRewards.ApplyGreedFraction(0.5f, 2f, 3f), Is.EqualTo(3), "Stage gold bonus multiplies too.");
            Assert.That(GoldRewards.ApplyGreedFraction(0f, 2f, 1f), Is.EqualTo(0));
        }

        [Test]
        public void LuckCharm_AddsTenPercentLuck()
        {
            Assert.That(LuckCharmPickup.LuckBonus, Is.EqualTo(0.1f));
            var go = new GameObject("LuckCharmStats");
            try
            {
                var stats = go.AddComponent<PlayerStats>();
                float before = stats.Luck;
                LuckCharmPickup.Apply(stats);
                Assert.That(stats.Luck, Is.EqualTo(before + 0.1f).Within(1e-5));
                LuckCharmPickup.Apply(stats);
                Assert.That(stats.Luck, Is.EqualTo(before + 0.2f).Within(1e-5), "Each charm stacks for the run.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FireBreath_UsesTheVsNumbers()
        {
            Assert.That(FireBreath.BaseDamage, Is.EqualTo(30f));
            Assert.That(FireBreath.BaseCooldown, Is.EqualTo(0.5f));
            Assert.That(FireBreath.BaseDurationSeconds, Is.EqualTo(10f));
            Assert.That(FireBreath.FlamesPerBurst, Is.EqualTo(12));
            Assert.That(FireBreath.FlameInterval * FireBreath.FlamesPerBurst, Is.LessThanOrEqualTo(FireBreath.BaseCooldown),
                "A burst ends before the next one starts.");
        }

        [Test]
        public void Catalog_HasThePlaceholderPickups_AndTheFlame()
        {
            var catalog = GameContentCatalog.Load();
            Assert.That(catalog, Is.Not.Null);
            AssertPickup<ScreenWipePickup>(catalog.ScreenWipeItem, "ScreenWipeItem");
            AssertPickup<FireBreathPickup>(catalog.FireBreathItem, "FireBreathItem");
            AssertPickup<LuckCharmPickup>(catalog.LuckCharmItem, "LuckCharmItem");
            AssertPickup<GildedCharmPickup>(catalog.GildedCharmItem, "GildedCharmItem");
            Assert.That(catalog.FireBreathProjectile, Is.Not.Null, "FireBreathProjectile is set (Soul Hunter/Content/Build Extra Pickups).");
            Assert.That(catalog.FireBreathProjectile.GetComponent<Projectile>(), Is.Not.Null, "The flame reuses a flying projectile.");
        }

        private static void AssertPickup<T>(GameObject prefab, string field) where T : Pickup
        {
            Assert.That(prefab, Is.Not.Null, $"GameContentCatalog.{field} is set (Soul Hunter/Content/Build Extra Pickups).");
            Assert.That(prefab.GetComponent<T>(), Is.Not.Null, $"{field} carries {typeof(T).Name}.");
            var trigger = prefab.GetComponent<Collider>();
            Assert.That(trigger != null && trigger.isTrigger, Is.True, $"{field} is a trigger (never blocks).");
            var motion = prefab.GetComponent<PickupIdleMotion>();
            Assert.That(motion != null && motion.Visual != null, Is.True, $"{field} bobs and turns its visual.");
            Assert.That(motion.Visual.GetComponentsInChildren<MeshRenderer>().Length, Is.GreaterThan(0), $"{field} has a visible shape.");
            Assert.That(prefab.GetComponentsInChildren<Collider>().Length, Is.EqualTo(1), $"{field}: only the root trigger collides.");
        }
    }
}
