using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.UI;
using SoulHunter.Gameplay.VFX;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>Bug MIS v2 (Docs/BUG_MIS_Level1_v2.md, 2026-10-02): the data and asset side of the fixes.</summary>
    public class BugMisV2Tests
    {
        [Test]
        public void C01_TheSongOfManaShot_HasNoOpaqueMesh()
        {
            var shot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/Projectiles/SH10_Proj_ManaShot.prefab");
            Assert.IsNotNull(shot);
            var meshes = shot.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToList();
            Assert.That(meshes, Is.Empty, "Only additive particles: the shot can never hide the hero.");
            Assert.That(shot.GetComponentsInChildren<ParticleSystem>(true).Any(p => p.name == "Column"), Is.True, "The soul-wisp column.");
        }

        [Test]
        public void C06_TheHitFlash_IsAShortTint()
        {
            Assert.That(DamageFlash.MaxBrightness, Is.LessThanOrEqualTo(1.4f));
            Assert.That(DamageFlash.MaxDuration / (DamageFlash.MaxDuration + DamageFlash.FlashGap), Is.LessThanOrEqualTo(0.25f),
                "Under constant hits a body keeps its own look at least three quarters of the time.");
        }

        [Test]
        public void C11_HudBackings_HideTheWorldBehindThem() =>
            Assert.That(HudBacking.Color.a, Is.GreaterThanOrEqualTo(0.85f));

        [TestCase(UpgradeData.UpgradeType.Whip, 4, "+5 damage, +10% area")]
        [TestCase(UpgradeData.UpgradeType.Knife, 5, "+1 pierce")]
        [TestCase(UpgradeData.UpgradeType.SongOfMana, 3, "+0.5 s duration, +0.8 s cooldown")]
        [TestCase(UpgradeData.UpgradeType.Laurel, 4, "+1 charge")]
        public void C14_AWeaponLevel_SaysWhatItAdds(UpgradeData.UpgradeType type, int level, string expected) =>
            Assert.That(UpgradeText.WeaponLevelText(type, level), Is.EqualTo(expected));

        [Test]
        public void C14_EveryBaseWeapon_HasTextForEveryLevel()
        {
            foreach (var definition in GameContentCatalog.Load().WeaponDefinitions.Where(d => d != null && !d.IsPassive))
                for (int level = 2; level <= definition.MaxLevel; level++)
                    Assert.That(UpgradeText.WeaponLevelText(definition.Type, level), Is.Not.Null.And.Not.Empty, $"{definition.Type} level {level}");
        }

        [Test]
        public void C15_C16_ItemNames_ReadAsItems()
        {
            var names = GameContentCatalog.Load().WeaponDefinitions.Where(d => d != null).ToDictionary(d => d.Type, d => d.DisplayName);
            Assert.That(names[UpgradeData.UpgradeType.SongOfMana], Is.EqualTo("Song of Mana"));
            Assert.That(names[UpgradeData.UpgradeType.MaxHealth], Is.Not.EqualTo("Max Health"), "Not a stat name.");
            Assert.That(names[UpgradeData.UpgradeType.PlayerSpeed], Is.Not.EqualTo("Player Speed"), "Not a stat name.");
        }

        [Test]
        public void C17_TheGoldReward_HasOneName() =>
            Assert.That(MaxedRewards.GoldCardName, Is.EqualTo("Gold Pouch"));

        [Test]
        public void C18_TheFourIcons_AreSoulHuntersOwn()
        {
            foreach (var type in new[] { UpgradeData.UpgradeType.Cross, UpgradeData.UpgradeType.Bible, UpgradeData.UpgradeType.Duplicator, UpgradeData.UpgradeType.Spellbinder })
            {
                var definition = GameContentCatalog.Load().WeaponDefinitions.First(d => d != null && d.Type == type);
                Assert.That(definition.Icon, Is.Not.Null, $"{type} icon");
                Assert.That(AssetDatabase.GetAssetPath(definition.Icon), Does.EndWith($"SH10_Icon_{type}.png"), $"{type} uses its new icon.");
            }
        }
    }
}
