using System.Linq;
using NUnit.Framework;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: Owner ne Blender close-ups approve kiye (2026-09-25): Soul Gems ke 3 tier (cyan, violet,
    /// crimson), Soul Coins (bronze, silver, gold) aur Soul Reliquary chest. Gem ka tier XP se aur coin ka gold se chalta hai.
    /// </summary>
    public class PickupV2Tests
    {
        /// <summary>
        /// Owner (2026-10-13): "coin kisi stiker ke jaisa lag raha hai use asli gold coin jaisa banao". The coin lay face up
        /// and turned like a record, and a strong glow of its own colour removed all shading. It now stands on its edge
        /// (its thin side is horizontal, so the turn shows face, edge, face) and its glow is weak next to its colour.
        /// </summary>
        [Test]
        public void TheCoin_StandsOnItsEdge_AndItsMetalIsShadedNotGlowing()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/SH10_Gold_Coin.prefab");
            var copy = Object.Instantiate(prefab);
            try
            {
                foreach (var tier in copy.GetComponent<TieredVisual>().Tiers)
                {
                    tier.SetActive(true);
                    var renderers = tier.GetComponentsInChildren<MeshRenderer>();
                    var bounds = renderers[0].bounds;
                    foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                    float thin = Mathf.Min(bounds.size.x, bounds.size.z);
                    Assert.That(bounds.size.y, Is.GreaterThan(thin * 3f), tier.name + " stands on its edge: taller than it is thick.");
                    Assert.That(thin, Is.GreaterThan(bounds.size.y * 0.12f), tier.name + " is thick enough for its edge to show.");
                    foreach (var material in renderers.SelectMany(r => r.sharedMaterials).Where(m => m.name.Contains("_Metal") || m.name.Contains("_Emblem")))
                    {
                        Color glow = material.GetColor("_EmissionColor"), colour = material.GetColor("_BaseColor");
                        Assert.That(glow.maxColorComponent, Is.LessThanOrEqualTo(colour.maxColorComponent * 0.25f), material.name + ": the glow must not flatten the shading.");
                        Assert.That(material.GetFloat("_Smoothness"), Is.GreaterThanOrEqualTo(0.6f), material.name + " catches the light.");
                    }
                }
            }
            finally { Object.DestroyImmediate(copy); }
        }

        [TestCase(1, 0)] [TestCase(10, 0)] [TestCase(20, 0)] [TestCase(21, 1)] [TestCase(50, 1)] [TestCase(90, 1)] [TestCase(91, 2)] [TestCase(250, 2)]
        public void GemTier_FollowsXp(int xp, int tier)
        {
            Assert.That(TieredVisual.TierFor(xp, VsGemThresholds), Is.EqualTo(tier));
        }

        /// <summary>
        /// vampire.survivors.wiki/w/Experience_Gem: "Blue Experience Gems grant up to 2 XP, green Experience Gems up to 9 XP
        /// and red gems contain anything beyond." Soul Hunter's basic gem is 10 XP (VS 1), so x10 (owner, 2026-09-30):
        /// up to 20, up to 90, beyond. A tier shows while XP is under its threshold.
        /// </summary>
        private static readonly int[] VsGemThresholds = { 21, 91 };

        [Test]
        public void TheGemPrefab_UsesTheVsColourThresholds()
        {
            var tiered = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/XP_Gem.prefab").GetComponent<TieredVisual>();
            Assert.That(tiered.Thresholds, Is.EqualTo(VsGemThresholds));
        }

        [TestCase(1, 0)] [TestCase(4, 0)] [TestCase(5, 1)] [TestCase(10, 1)] [TestCase(25, 2)] [TestCase(50, 2)]
        public void CoinTier_FollowsGold(int gold, int tier)
        {
            Assert.That(TieredVisual.TierFor(gold, new[] { 5, 25 }), Is.EqualTo(tier));
        }

        [TestCase("Assets/Prefabs/Items/XP_Gem.prefab", new[] { "SH10_Pickup_SoulGem_T1", "SH10_Pickup_SoulGem_T2", "SH10_Pickup_SoulGem_T3" })]
        // Owner (2026-10-13): every coin is the gold coin; its worth shows by its size.
        [TestCase("Assets/Prefabs/Items/SH10_Gold_Coin.prefab", new[] { "SH10_Pickup_Coin_Gold", "SH10_Pickup_Coin_Gold", "SH10_Pickup_Coin_Gold" })]
        public void TieredPickup_ShowsTheApprovedModels(string path, string[] models)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var tiered = prefab.GetComponent<TieredVisual>();
            Assert.IsNotNull(tiered, path);
            Assert.That(tiered.Tiers.Length, Is.EqualTo(3));
            for (int i = 0; i < 3; i++)
            {
                Assert.That(tiered.Tiers[i].GetComponentsInChildren<Transform>(true).Any(t => t.name == models[i]), $"{path} tier {i} is {models[i]}.");
                Assert.That(tiered.Tiers[i].activeSelf, Is.EqualTo(i == 0), "Only the lowest tier shows until a value is set.");
            }
            foreach (var r in prefab.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name != "GroundGlow" && r.enabled))
                foreach (var m in r.sharedMaterials)
                    Assert.That(m.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"), $"{r.name} material {m.name}.");
        }

        [Test]
        public void EveryCoin_IsGold_GrowsWithItsWorth_AndHasNoFlatGlowUnderIt()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/SH10_Gold_Coin.prefab");
            Assert.That(prefab.GetComponentsInChildren<Transform>(true).Any(t => t.name == "GroundGlow"), Is.False, "No flat disc under the coin.");
            Assert.That(SoulHunter.Gameplay.UI.ChestPreviewStage.CoinVisual(prefab), Is.Not.Null, "The chest screen's coin burst finds its coin model.");
            var copy = Object.Instantiate(prefab);
            try
            {
                float last = 0f;
                foreach (var tier in copy.GetComponent<TieredVisual>().Tiers)
                {
                    tier.SetActive(true);
                    var renderers = tier.GetComponentsInChildren<MeshRenderer>();
                    float height = renderers.Max(r => r.bounds.max.y) - renderers.Min(r => r.bounds.min.y);
                    Assert.That(height, Is.GreaterThan(last * 1.1f), tier.name + " is bigger than the coin worth less.");
                    last = height;
                    foreach (var material in renderers.SelectMany(r => r.sharedMaterials).Where(m => m.name.Contains("_Metal")))
                    {
                        Color c = material.GetColor("_BaseColor");
                        Assert.That(c.r > 0.9f && c.g > 0.6f && c.g < 0.9f && c.b < 0.3f, material.name + " is gold, not bronze or silver: " + c);
                    }
                }
            }
            finally { Object.DestroyImmediate(copy); }
        }

        [Test]
        public void Chest_ShowsTheSoulReliquary()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/Chest.prefab");
            Assert.That(prefab.GetComponentsInChildren<Transform>(true).Any(t => t.name == "SH10_Pickup_Chest"));
            Assert.That(prefab.GetComponentsInChildren<Transform>(true).Any(t => t.name == "SH10_Chest"), Is.False, "The old chest model is gone.");
            var glowing = prefab.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials)
                .Any(m => m != null && m.IsKeywordEnabled("_EMISSION"));
            Assert.That(glowing, "The soul lock, seam and eyes glow.");
        }

        [Test]
        public void ElitesAndBosses_DropBiggerGems()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyData", new[] { "Assets/_Project/Data/Config/Enemies" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/MadForest/") || StageEnemyTables.IsTypeAsset(path)) continue; // VS XP x10 per type (MadForestEnemyTests, StageEnemyTypeTests)
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                int expected = data.name.Contains("_Boss_") ? 250 : data.name.Contains("_Elite_") ? 50 : 10;
                Assert.That(data.ExperienceValue, Is.EqualTo(expected), data.name);
            }
        }
    }
}
