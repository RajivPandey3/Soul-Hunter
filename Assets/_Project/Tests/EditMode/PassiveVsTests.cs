using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;
using UnityEditor;
using UnityEngine;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// VS passives (each item's vampire.survivors.wiki infobox, fetched 2026-09-28; owner: "passives ka max level VS
    /// jaisa theek karo"): max level 5 (2 for Duplicator and Tirajisú, 9 for the Metaglio stage items), Hollow Heart
    /// multiplies Max Health by 1.2 without filling it, Attractorb multiplies Magnet in uneven steps, and the Metaglios'
    /// level 1 gives nothing.
    /// </summary>
    public class PassiveVsTests
    {
        private GameObject _player;

        [TearDown]
        public void TearDown()
        {
            if (_player != null) Object.DestroyImmediate(_player);
        }

        [TestCase(UpgradeType.Spinach, 5)]
        [TestCase(UpgradeType.Armor, 5)]
        [TestCase(UpgradeType.MaxHealth, 5)]
        [TestCase(UpgradeType.Pummarola, 5)]
        [TestCase(UpgradeType.EmptyTome, 5)]
        [TestCase(UpgradeType.Candelabrador, 5)]
        [TestCase(UpgradeType.Bracer, 5)]
        [TestCase(UpgradeType.Spellbinder, 5)]
        [TestCase(UpgradeType.PlayerSpeed, 5)]
        [TestCase(UpgradeType.Attractorb, 5)]
        [TestCase(UpgradeType.Clover, 5)]
        [TestCase(UpgradeType.Crown, 5)]
        [TestCase(UpgradeType.SkullOManiac, 5)]
        [TestCase(UpgradeType.Duplicator, 2)]
        [TestCase(UpgradeType.Tiragisu, 2)]
        [TestCase(UpgradeType.MetaglioLeft, 9)]
        [TestCase(UpgradeType.MetaglioRight, 9)]
        public void PassiveMaxLevels_AreTheVsOnes(UpgradeType type, int max)
        {
            Assert.That(UpgradeRarity.VsPassiveMaxLevel(type), Is.EqualTo(max));
            var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>($"Assets/_Project/Data/Weapons/Definitions/WD_{type}.asset");
            Assert.That(definition, Is.Not.Null, $"WD_{type}");
            Assert.That(definition.MaxLevel, Is.EqualTo(max), "The definition asset carries the VS max level.");
        }

        [Test]
        public void Weapons_HaveNoPassiveMaxLevel()
        {
            Assert.That(UpgradeRarity.VsPassiveMaxLevel(UpgradeType.Whip), Is.EqualTo(0));
        }

        [Test]
        public void HollowHeart_MultipliesMaxHealth_WithoutHealing()
        {
            _player = new GameObject("Player");
            var health = _player.AddComponent<HealthController>();
            health.Initialize(100);
            health.TakeDamage(new DamagePacket(50, Vector3.zero, Vector3.zero, DamageType.Normal));
            health.MultiplyMaxHealth(1.2f, healByIncrease: false);
            Assert.That(health.MaxHealth, Is.EqualTo(120));
            Assert.That(health.CurrentHealth, Is.EqualTo(50), "VS: does not heal to fill up the new Max Health.");
        }

        [Test]
        public void Attractorb_Steps_ReachAbout398Percent()
        {
            float magnet = 1f;
            for (int level = 1; level <= 5; level++) magnet *= WeaponManager.AttractorbStep(level);
            Assert.That(magnet, Is.EqualTo(3.98f).Within(0.01f), "VS table: 3.980025x at level 5.");
            Assert.That(WeaponManager.AttractorbStep(1), Is.EqualTo(1.5f));
        }

        [Test]
        public void MetaglioLevelOne_GivesNothing_LevelTwoGivesItsBonus()
        {
            _player = new GameObject("Player");
            var stats = _player.AddComponent<PlayerStats>();
            _player.AddComponent<HealthController>().Initialize(100);
            var weapons = _player.AddComponent<WeaponManager>();
            float curse = stats.Curse;
            var one = ScriptableObject.CreateInstance<UpgradeData>();
            one.Type = UpgradeType.MetaglioRight; one.Level = 1;
            var two = ScriptableObject.CreateInstance<UpgradeData>();
            two.Type = UpgradeType.MetaglioRight; two.Level = 2;
            try
            {
                Assert.That(weapons.AcquireStagePassive(one), Is.True, "A stage passive is picked up from the ground first.");
                Assert.That(stats.Curse, Is.EqualTo(curse).Within(1e-4f), "Level 1: nothing (VS).");
                weapons.ApplyUpgrade(two);
                Assert.That(stats.Curse, Is.EqualTo(curse + 0.05f).Within(1e-4f), "Level 2: +5% Curse.");
            }
            finally
            {
                Object.DestroyImmediate(one);
                Object.DestroyImmediate(two);
            }
        }

        [Test]
        public void Tirajisu_LeavesTheInventory_OnlyWhenTheLastRevivalIsUsed()
        {
            // VS wiki (Tirajisú): "Tirajisú will only be removed from the inventory when the last Revive is used up",
            // and "you can get it again through a level up".
            _player = new GameObject("Player");
            var stats = _player.AddComponent<PlayerStats>();
            _player.AddComponent<HealthController>().Initialize(100);
            var weapons = _player.AddComponent<WeaponManager>();
            weapons.DefinitionLookup = _ => null;
            var one = ScriptableObject.CreateInstance<UpgradeData>();
            one.Type = UpgradeType.Tiragisu; one.Level = 1;
            UpgradeType? removed = null;
            weapons.OnItemRemoved += type => removed = type;
            try
            {
                weapons.ApplyUpgrade(one);
                Assert.That(weapons.GetWeaponLevel(UpgradeType.Tiragisu), Is.EqualTo(1));
                int passives = weapons.GetPassiveCount();

                Assert.That(weapons.ReleaseSpentTirajisu(revivalsLeft: 1), Is.False, "A Revival is left: it stays.");
                Assert.That(weapons.GetWeaponLevel(UpgradeType.Tiragisu), Is.EqualTo(1));
                Assert.That(removed, Is.Null);

                Assert.That(weapons.ReleaseSpentTirajisu(revivalsLeft: 0), Is.True, "The last Revival is gone: it leaves.");
                Assert.That(weapons.GetWeaponLevel(UpgradeType.Tiragisu), Is.EqualTo(0));
                Assert.That(weapons.GetPassiveCount(), Is.EqualTo(passives - 1), "Its passive slot is free again.");
                Assert.That(removed, Is.EqualTo(UpgradeType.Tiragisu));

                int revivals = stats.Revivals;
                weapons.ApplyUpgrade(one);
                Assert.That(weapons.GetWeaponLevel(UpgradeType.Tiragisu), Is.EqualTo(1), "It can be taken again from level 1.");
                Assert.That(stats.Revivals, Is.EqualTo(revivals + 1), "Taking it again adds a Revival.");
                Assert.That(weapons.ReleaseSpentTirajisu(revivalsLeft: 0), Is.True);
                Assert.That(weapons.ReleaseSpentTirajisu(revivalsLeft: 0), Is.False, "Once gone, there is nothing left to remove.");
            }
            finally
            {
                Object.DestroyImmediate(one);
            }
        }
    }
}
