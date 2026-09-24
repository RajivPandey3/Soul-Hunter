using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: CoreLoopFixTests covers the VS core-loop fixes: Armor reduces damage taken,
    /// level-ups never offer items without a free slot, and every evolution spawns its own evolved weapon.
    /// </summary>
    public class CoreLoopFixTests
    {
        private GameObject _object;

        [TearDown]
        public void TearDown()
        {
            if (_object != null) Object.DestroyImmediate(_object);
        }

        [Test]
        public void Armor_ReducesEachHitByArmor_WithMinimumOneDamage()
        {
            // Learning Comment: VS mein har Armor point har hit se 1 damage kam karta hai, lekin hit kam se kam 1 damage deta hai.
            _object = new GameObject("ArmorTarget");
            var health = _object.AddComponent<HealthController>();
            health.Initialize(100);
            health.Armor = 3;

            health.TakeDamage(new DamagePacket(10, Vector3.zero, Vector3.zero));
            Assert.That(health.CurrentHealth, Is.EqualTo(93));

            health.TakeDamage(new DamagePacket(2, Vector3.zero, Vector3.zero));
            Assert.That(health.CurrentHealth, Is.EqualTo(92), "Armor se zyada kamzor hit phir bhi 1 damage de.");
        }

        [Test]
        public void PlayerStatsAddArmor_UpdatesHealthControllerArmor()
        {
            // Learning Comment: Armor passive lene par player ka HealthController turant naya Armor use kare.
            _object = new GameObject("ArmorPlayer");
            var health = _object.AddComponent<HealthController>();
            var stats = _object.AddComponent<PlayerStats>();

            stats.AddArmor(2);

            Assert.That(health.Armor, Is.EqualTo(2));
        }

        [Test]
        public void HasSlotFor_RejectsNewItemsOnlyWhenTheirSlotsAreFull()
        {
            // Learning Comment: Slots bhar jane par naya item offer nahi hona chahiye, lekin owned items level ho sakte hain.
            _object = new GameObject("Inventory");
            var weapons = _object.AddComponent<WeaponManager>();
            var ownedWeapons = new[]
            {
                UpgradeData.UpgradeType.MagicWand, UpgradeData.UpgradeType.Whip, UpgradeData.UpgradeType.Garlic,
                UpgradeData.UpgradeType.Axe, UpgradeData.UpgradeType.Bible, UpgradeData.UpgradeType.Cross
            };
            foreach (var type in ownedWeapons) weapons.GiveWeapon(type, 1);

            Assert.That(weapons.HasSlotFor(UpgradeData.UpgradeType.Knife), Is.False, "7th weapon ke liye slot nahi hai.");
            Assert.That(weapons.HasSlotFor(UpgradeData.UpgradeType.Whip), Is.True, "Owned weapon level ho sakta hai.");
            Assert.That(weapons.HasSlotFor(UpgradeData.UpgradeType.Spinach), Is.True, "Passive slots abhi khali hain.");

            var ownedPassives = new[]
            {
                UpgradeData.UpgradeType.Spinach, UpgradeData.UpgradeType.Armor, UpgradeData.UpgradeType.EmptyTome,
                UpgradeData.UpgradeType.Bracer, UpgradeData.UpgradeType.Candelabrador, UpgradeData.UpgradeType.Spellbinder
            };
            foreach (var type in ownedPassives) weapons.GiveWeapon(type, 1);

            Assert.That(weapons.HasSlotFor(UpgradeData.UpgradeType.Duplicator), Is.False, "7th passive ke liye slot nahi hai.");
            Assert.That(weapons.HasSlotFor(UpgradeData.UpgradeType.Armor), Is.True, "Owned passive level ho sakta hai.");
        }

        [Test]
        public void EveryEnabledEvolution_SpawnsTheEvolvedWeaponItIsNamedAfter()
        {
            // Learning Comment: Har evolution ko apna hi evolved weapon dena chahiye (e.g. Evo_HolyWand -> HolyWand),
            // kisi doosre weapon ka prefab nahi.
            int enabled = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:WeaponEvolutionData", new[] { "Assets/_Project/Data/Weapons" }))
            {
                var evo = AssetDatabase.LoadAssetAtPath<WeaponEvolutionData>(AssetDatabase.GUIDToAssetPath(guid));
                if (evo == null || evo.EvolvedWeaponPrefab == null) continue;
                enabled++;
                string expected = evo.EvolvedName.Replace("Evo_", "");
                Assert.That(evo.EvolvedWeaponPrefab.name, Is.EqualTo(expected), $"{evo.name} galat weapon spawn karta hai.");
            }

            Assert.That(enabled, Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void ThousandEdge_EvolvesFromKnifeAndBracer()
        {
            // Learning Comment: VS recipe: Knife (Lv8) + Bracer -> Thousand Edge.
            var evo = AssetDatabase.LoadAssetAtPath<WeaponEvolutionData>("Assets/_Project/Data/Weapons/Evo_ThousandEdge.asset");
            Assert.That(evo, Is.Not.Null);
            Assert.That(evo.BaseWeapon, Is.EqualTo(UpgradeData.UpgradeType.Knife));
            Assert.That(evo.RequiredPassive, Is.EqualTo(UpgradeData.UpgradeType.Bracer));
            Assert.That(evo.EvolvedWeaponPrefab, Is.Not.Null);
            Assert.That(evo.EvolvedWeaponPrefab.GetComponent<ThousandEdgeWeapon>(), Is.Not.Null);
        }
    }
}
