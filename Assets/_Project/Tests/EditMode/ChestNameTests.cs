using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using UnityEditor;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>Bug MIS B03 (2026-09-30): the chest reel showed evolution IDs such as "Evo_BloodyTear".</summary>
    public class ChestNameTests
    {
        [TestCase("Evo_BloodyTear", "Bloody Tear")]
        [TestCase("Evo_ThousandEdge", "Thousand Edge")]
        [TestCase("Evo_UnholyVespers", "Unholy Vespers")]
        [TestCase("Evo_SoulEater", "Soul Eater")]
        [TestCase("Union_Something", "Something")]
        [TestCase("Holy Wand", "Holy Wand")]
        public void EvolutionIds_ShowAsNames(string id, string shown)
        {
            Assert.That(WeaponEvolutionData.DisplayName(id), Is.EqualTo(shown));
        }

        [Test]
        public void EveryEvolutionAsset_HasAReadableName()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:WeaponEvolutionData"))
            {
                var evo = AssetDatabase.LoadAssetAtPath<WeaponEvolutionData>(AssetDatabase.GUIDToAssetPath(guid));
                if (evo == null || string.IsNullOrEmpty(evo.EvolvedName)) continue;
                string shown = WeaponEvolutionData.DisplayName(evo.EvolvedName);
                Assert.That(shown.Contains("_"), Is.False, $"{evo.EvolvedName} shows as '{shown}'.");
                Assert.That(shown.StartsWith("Evo"), Is.False, $"{evo.EvolvedName} shows as '{shown}'.");
            }
        }
    }
}
