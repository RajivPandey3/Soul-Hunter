using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using UnityEngine;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// WeaponManager.GetInventory feeds the HUD's inventory row (VS wiki, Passive items: 6 passive slots; weapons
    /// likewise): items in the order gained, weapons and passives apart, a spent Tirajisú gone, evolved weapons marked.
    /// </summary>
    public class InventoryListTests
    {
        private GameObject _player;
        private readonly List<UpgradeData> _created = new List<UpgradeData>();

        [TearDown]
        public void TearDown()
        {
            foreach (var upgrade in _created) Object.DestroyImmediate(upgrade);
            _created.Clear();
            if (_player != null) Object.DestroyImmediate(_player);
        }

        private UpgradeData Upgrade(UpgradeType type, int level = 1)
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeData>();
            upgrade.Type = type; upgrade.Level = level;
            _created.Add(upgrade);
            return upgrade;
        }

        private WeaponManager MakeManager()
        {
            _player = new GameObject("Player");
            _player.AddComponent<PlayerStats>();
            _player.AddComponent<HealthController>().Initialize(100);
            var weapons = _player.AddComponent<WeaponManager>();
            weapons.DefinitionLookup = _ => null; // no prefabs: nothing is spawned
            return weapons;
        }

        [Test]
        public void Inventory_ListsItemsInTheOrderGained_WeaponsAndPassivesApart()
        {
            var weapons = MakeManager();
            int changes = 0;
            weapons.OnInventoryChanged += () => changes++;

            weapons.ApplyUpgrade(Upgrade(UpgradeType.Spinach));
            weapons.ApplyUpgrade(Upgrade(UpgradeType.Knife));
            weapons.ApplyUpgrade(Upgrade(UpgradeType.Armor));
            weapons.ApplyUpgrade(Upgrade(UpgradeType.Whip));
            weapons.ApplyUpgrade(Upgrade(UpgradeType.Knife, 2)); // a level-up keeps the slot

            Assert.That(weapons.GetInventory(passives: false).Select(s => s.Type),
                Is.EqualTo(new[] { UpgradeType.Knife, UpgradeType.Whip }));
            Assert.That(weapons.GetInventory(passives: true).Select(s => s.Type),
                Is.EqualTo(new[] { UpgradeType.Spinach, UpgradeType.Armor }));
            Assert.That(weapons.GetInventory(false)[0].Level, Is.EqualTo(2));
            Assert.That(changes, Is.EqualTo(5), "Every gain or level-up tells the HUD.");
        }

        [Test]
        public void SpentTirajisu_LeavesItsSlot_TheOthersMoveUp()
        {
            var weapons = MakeManager();
            weapons.ApplyUpgrade(Upgrade(UpgradeType.Tiragisu));
            weapons.ApplyUpgrade(Upgrade(UpgradeType.Spinach));
            int changes = 0;
            weapons.OnInventoryChanged += () => changes++;

            weapons.ReleaseSpentTirajisu(revivalsLeft: 0);
            Assert.That(weapons.GetInventory(true).Select(s => s.Type), Is.EqualTo(new[] { UpgradeType.Spinach }));
            Assert.That(changes, Is.EqualTo(1));

            weapons.ApplyUpgrade(Upgrade(UpgradeType.Tiragisu));
            Assert.That(weapons.GetInventory(true).Select(s => s.Type),
                Is.EqualTo(new[] { UpgradeType.Spinach, UpgradeType.Tiragisu }), "Taken again, it goes to the end.");
        }

        [Test]
        public void EvolvedWeapon_KeepsItsSlot_AndIsMarked()
        {
            var weapons = MakeManager();
            weapons.ApplyUpgrade(Upgrade(UpgradeType.Whip));
            weapons.ApplyUpgrade(Upgrade(UpgradeType.Knife));
            var evolved = (List<UpgradeType>)typeof(WeaponManager)
                .GetField("_evolvedWeapons", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(weapons);
            evolved.Add(UpgradeType.Knife);

            var slots = weapons.GetInventory(false);
            Assert.That(slots.Select(s => s.Type), Is.EqualTo(new[] { UpgradeType.Whip, UpgradeType.Knife }));
            Assert.That(slots[0].Evolved, Is.False);
            Assert.That(slots[1].Evolved, Is.True);
        }
    }
}
