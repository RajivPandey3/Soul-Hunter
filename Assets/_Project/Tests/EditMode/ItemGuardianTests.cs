using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// The guardians of the hidden stage items (VS Atlanteans; wiki City_Atlantean, Volcano_Atlantean, Moon_Atlantean,
    /// Sun_Atlantean and Passive_items "Stage items", raw, 2026-10-13): one shared statbox, one guardian per hidden item,
    /// none for the Idol, and a built prefab per level.
    /// </summary>
    public class ItemGuardianTests
    {
        [Test]
        public void TheStatbox_IsTheWikis()
        {
            // "health = 30,000 | movespeed = 200 | damage = 50 | knockback = 0 | knockback-max = 0 | xp = 0 |
            //  res-freeze = 2 | res-kill = 1 | res-debuff = 1"
            Assert.That(ItemGuardians.Health, Is.EqualTo(30000));
            Assert.That(ItemGuardians.VsMoveSpeed, Is.EqualTo(200f));
            Assert.That(ItemGuardians.Damage, Is.EqualTo(50));
            Assert.That(ItemGuardians.Knockback, Is.EqualTo(0f));
            Assert.That(ItemGuardians.MaxKnockback, Is.EqualTo(0f));
            Assert.That(ItemGuardians.Experience, Is.EqualTo(0));
            Assert.That(ItemGuardians.FreezeResistance, Is.EqualTo(2f));
            Assert.That(ItemGuardians.InstantKillResistance, Is.True);
            Assert.That(ItemGuardians.DebuffResistance, Is.True);
        }

        [Test]
        public void EachHiddenItem_HasItsGuardian_AndTheIdolHasNone()
        {
            Assert.That(ItemGuardians.All.Length, Is.EqualTo(4));
            Assert.That(ItemGuardians.For(UpgradeType.MetaglioLeft).Value.VsEnemy, Is.EqualTo("City Atlantean"));
            Assert.That(ItemGuardians.For(UpgradeType.MetaglioRight).Value.VsEnemy, Is.EqualTo("Volcano Atlantean"));
            Assert.That(ItemGuardians.For(UpgradeType.RingOfLongHours).Value.VsEnemy, Is.EqualTo("Moon Atlantean"), "Silver Ring.");
            Assert.That(ItemGuardians.For(UpgradeType.RingOfDarkHours).Value.VsEnemy, Is.EqualTo("Sun Atlantean"), "Gold Ring.");
            Assert.That(ItemGuardians.For(UpgradeType.MisersIdol).HasValue, Is.False, "The Stone Mask page names no guard.");
            Assert.That(ItemGuardians.For(UpgradeType.Spinach).HasValue, Is.False);
            Assert.That(ItemGuardians.All.Select(g => g.Id).Distinct().Count(), Is.EqualTo(4));
            Assert.That(ItemGuardians.All.Select(g => g.Name).Distinct().Count(), Is.EqualTo(4));
            foreach (var guard in ItemGuardians.All)
                Assert.That(guard.Name, Does.Not.Contain("Atlantean"), "Soul Hunter's own names.");
            // Every stage's plan lays all four guarded items.
            for (int stage = 1; stage <= 10; stage++)
            {
                var plan = StageItems.Plan(stage);
                foreach (var guard in ItemGuardians.All)
                    Assert.That(plan.Count(e => e.Kind == StageItems.Kind.Passive && e.Upgrade == guard.Item), Is.EqualTo(1), $"Stage {stage}: {guard.Item}");
            }
        }

        [Test]
        public void TheBuiltAssets_FollowTheStatbox_OnEveryLevelsEliteModel()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>("Assets/Resources/GameContent.asset");
            Assert.IsNotNull(catalog);
            Assert.That(catalog.ItemGuards, Is.Not.Null);
            Assert.That(catalog.ItemGuards.Length, Is.EqualTo(40), "Four guardians on ten levels.");
            foreach (var guard in ItemGuardians.All)
            {
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>(ItemGuardians.DataPath(guard.Id));
                Assert.IsNotNull(data, ItemGuardians.DataPath(guard.Id));
                Assert.That(data.EnemyName, Is.EqualTo(guard.Name));
                Assert.That(data.MaxHealth, Is.EqualTo(30000));
                Assert.That(data.HealthPerPlayerLevel, Is.EqualTo(0), "No HP x Level skill.");
                Assert.That(data.DamageToPlayer, Is.EqualTo(50));
                Assert.That(data.MoveSpeed, Is.EqualTo(6f).Within(1e-4), "VS 200 = 6 units/s.");
                Assert.That(data.Knockback, Is.EqualTo(0f));
                Assert.That(data.MaxKnockback, Is.EqualTo(0f));
                Assert.That(data.ExperienceValue, Is.EqualTo(0));
                Assert.That(data.FreezeResistance, Is.EqualTo(2f));
                Assert.That(data.InstantKillResistance, Is.True);
                Assert.That(data.DebuffResistance, Is.True);
                Assert.That(data.DropsChest, Is.False);
                Assert.That(data.Abilities, Is.Empty);
                Assert.That(data.VisualTint, Is.EqualTo(guard.Tint));
                Assert.That(catalog.BestiaryEnemies, Does.Contain(data), $"{guard.Name} is in the Bestiary.");
                for (int level = 1; level <= 10; level++)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ItemGuardians.PrefabPath(level, guard.Id));
                    Assert.IsNotNull(prefab, ItemGuardians.PrefabPath(level, guard.Id));
                    Assert.That(catalog.ItemGuards, Does.Contain(prefab));
                    Assert.AreSame(data, prefab.GetComponent<EnemyController>().Data);
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(prefab);
                    Assert.IsNotNull(source, "A prefab variant.");
                    Assert.That(source.name, Is.EqualTo($"SH10_L{level:00}_EliteGameplay"), "The level's own elite model.");
                }
            }
        }
    }
}
