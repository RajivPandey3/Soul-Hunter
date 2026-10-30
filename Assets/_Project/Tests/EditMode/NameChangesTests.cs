using System.Linq;
using NUnit.Framework;
using UnityEditor;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Owner, 2026-10-30 (release audit question 1, list approved; Docs/Name_Changes_20261030.md): a player never reads a
    /// VS item name. This fails if a VS name comes back in a weapon definition, an upgrade card, an evolution or union
    /// name, or an Arcana's name, description or "works with" line.
    /// </summary>
    public class NameChangesTests
    {
        private static string FirstVsName(string text) =>
            string.IsNullOrEmpty(text) ? null : SoulHunterNames.Renames.Keys.FirstOrDefault(vs => SoulHunterNames.ReplaceAll(text) != text && text.Contains(vs));

        [Test]
        public void ReplaceAll_ChangesWholeNamesOnly()
        {
            Assert.That(SoulHunterNames.ReplaceAll("Whip, then Bloody Tear and Santa Water."), Is.EqualTo("Whip, then Crimson Lash and Holy Water."));
            Assert.That(SoulHunterNames.ReplaceAll("+1 Armor"), Is.EqualTo("+1 Armour"));
            Assert.That(SoulHunterNames.ReplaceAll("Armored Knight"), Is.EqualTo("Armored Knight"), "Only the whole word.");
            Assert.That(SoulHunterNames.For("Hellmaw Comet"), Is.EqualTo("Hellmaw Comet"), "Soul Hunter's own names stay.");
        }

        [Test]
        public void NoVisibleNameIsAVsName()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:WeaponDefinition"))
            {
                var def = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(FirstVsName(def.DisplayName), Is.Null, $"{def.name} shows '{def.DisplayName}'.");
                Assert.That(FirstVsName(def.Description), Is.Null, $"{def.name}'s description: '{def.Description}'.");
            }
            foreach (var guid in AssetDatabase.FindAssets("t:UpgradeData", new[] { "Assets/Resources" }))
            {
                var up = AssetDatabase.LoadAssetAtPath<UpgradeData>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(FirstVsName(up.UpgradeName), Is.Null, $"{up.name} shows '{up.UpgradeName}'.");
                Assert.That(FirstVsName(up.Description), Is.Null, $"{up.name}'s description: '{up.Description}'.");
            }
            foreach (var guid in AssetDatabase.FindAssets("t:WeaponEvolutionData"))
            {
                var evo = AssetDatabase.LoadAssetAtPath<WeaponEvolutionData>(AssetDatabase.GUIDToAssetPath(guid));
                string shown = WeaponEvolutionData.DisplayName(!string.IsNullOrEmpty(evo.EvolvedName) ? evo.EvolvedName : evo.name);
                Assert.That(FirstVsName(shown), Is.Null, $"{evo.name} shows '{shown}'.");
            }
            foreach (ArcanaData.ArcanaType type in System.Enum.GetValues(typeof(ArcanaData.ArcanaType)))
            {
                var card = ArcanaCatalog.Of(type);
                Assert.That(FirstVsName(card.Name), Is.Null, $"Arcana {type} is named '{card.Name}'.");
                Assert.That(FirstVsName(card.Description), Is.Null, $"Arcana {type}: '{card.Description}'.");
                foreach (var weapon in card.Weapons)
                    Assert.That(FirstVsName(ArcanaCatalog.WeaponName(weapon)), Is.Null, $"Arcana {type} works with '{ArcanaCatalog.WeaponName(weapon)}'.");
            }
        }
    }
}
