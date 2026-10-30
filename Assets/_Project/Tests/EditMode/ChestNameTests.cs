using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using UnityEditor;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>Bug MIS B03 (2026-09-30): the chest reel showed evolution IDs such as "Evo_BloodyTear". Since 2026-10-30 VS names show as Soul Hunter's (SoulHunterNames).</summary>
    public class ChestNameTests
    {
        [TestCase("Evo_BloodyTear", "Crimson Lash")]
        [TestCase("Evo_ThousandEdge", "Blade Storm")]
        [TestCase("Evo_UnholyVespers", "Black Psalter")]
        [TestCase("Evo_SoulEater", "Soul Devourer")]
        [TestCase("Union_Something", "Something")]
        [TestCase("Evo_HellmawComet", "Hellmaw Comet")] // Soul Hunter's own names stay
        [TestCase("Holy Wand", "Radiant Rod")]
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
