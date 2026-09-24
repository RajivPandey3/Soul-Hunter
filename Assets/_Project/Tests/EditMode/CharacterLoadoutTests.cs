using System.Linq;
using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: CharacterLoadoutTests verifies VS-style characters: the selected character is found
    /// by exact name only, its stat modifiers apply once, and every character starts with a real weapon.
    /// </summary>
    public class CharacterLoadoutTests
    {
        private GameObject _object;
        private CharacterData _character;

        [TearDown]
        public void TearDown()
        {
            if (_object != null) Object.DestroyImmediate(_object);
            if (_character != null) Object.DestroyImmediate(_character);
        }

        [Test]
        public void FindCharacterExact_FindsByName_AndNeverFallsBack()
        {
            // Learning Comment: Galat naam par kisi aur character ka loadout nahi milna chahiye.
            var kael = GameContentCatalog.FindCharacterExact("Kael");
            Assert.That(kael, Is.Not.Null);
            Assert.That(kael.CharacterName, Is.EqualTo("Kael"));

            Assert.That(GameContentCatalog.FindCharacterExact("Hero_1_0"), Is.Null);
            Assert.That(GameContentCatalog.FindCharacterExact(""), Is.Null);
            Assert.That(GameContentCatalog.FindCharacterExact(null), Is.Null);
        }

        [Test]
        public void ApplyCharacter_AddsModifiersOnce_AndSyncsArmor()
        {
            _object = new GameObject("Player");
            var health = _object.AddComponent<HealthController>();
            var stats = _object.AddComponent<PlayerStats>();
            _character = ScriptableObject.CreateInstance<CharacterData>();
            _character.StartingMight = 1.5f;
            _character.StartingArea = 1.2f;
            _character.StartingCooldown = 0.9f;
            _character.StartingArmor = 2;

            stats.ApplyCharacter(_character);
            stats.ApplyCharacter(_character); // must not stack

            Assert.That(stats.Might, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(stats.Area, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(stats.Cooldown, Is.EqualTo(0.9f).Within(0.001f));
            Assert.That(stats.Armor, Is.EqualTo(2));
            Assert.That(health.Armor, Is.EqualTo(2), "Armor HealthController tak pahunche.");
        }

        [Test]
        public void EveryCharacter_StartsWithAWeaponThatHasALevelOneUpgrade()
        {
            // Learning Comment: Har character ka starting weapon asli weapon ho jiska Lv1 upgrade maujood ho.
            var catalog = GameContentCatalog.Load();
            Assert.That(catalog, Is.Not.Null);
            var levelOne = Resources.LoadAll<UpgradeData>("Upgrades").Where(u => u.Level == 1).Select(u => u.Type).ToList();

            foreach (var character in catalog.Characters.Where(c => c != null))
            {
                Assert.That(levelOne, Does.Contain(character.StartingWeapon),
                    $"{character.name} ka starting weapon {character.StartingWeapon} ka Lv1 upgrade nahi hai.");
                Assert.That(character.BaseMaxHealth, Is.GreaterThan(0), $"{character.name} max health.");
            }
        }
    }
}
