using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Architecture guard (audit 2026-10-30, owner: a guard test now, the refactor after release). AGENTS section 3 says
    /// Gameplay must not depend on UI. UI/HUD compiles into the Gameplay assembly, so the compiler does not stop it, and
    /// 12 runtime files outside UI already call HUD types directly.
    ///
    /// This test freezes that list. A new runtime file, or a new HUD type in a listed file, fails the test: send an event
    /// and let the UI listen instead. The list only ever shrinks, as the refactor moves each file to events.
    /// Comments are ignored, and so are the nested names "Phase" and "Arrow", which are common words.
    /// </summary>
    public class GameplayUiCouplingGuardTests
    {
        private static readonly Dictionary<string, string[]> Allowed = new Dictionary<string, string[]>
        {
            ["Core/Game/ArcanaManager.cs"] = new[] { "ArcanaChoiceScreen" },
            ["Core/Game/AutoplayBot.cs"] = new[] { "ArcanaChoiceScreen", "ChestOpeningScreen", "ChestUI", "EvolutionRecipesScreen", "LevelUpUI", "OptionsMenuUI", "PauseMenuUI", "ReviveScreen", "StatsPanel", "UiScreen" },
            ["Core/Game/CharacterUnlockTracker.cs"] = new[] { "GameHUDController", "HudBacking", "ResponsiveCanvas", "SurfaceRole", "TextRole", "ThemedElement", "UiScreen", "UITheme" },
            ["Core/Game/LevelUpManager.cs"] = new[] { "LevelUpUI" },
            ["Core/Game/PerformanceBenchmark.cs"] = new[] { "ChestUI" },
            ["Core/Game/WanderingMerchant.cs"] = new[] { "ArcanaChoiceScreen", "Bar", "ButtonKind", "DamagePopupManager", "IconSlot", "ResponsiveCanvas", "ResponsiveScroll", "SafeAreaFitter", "SizeRole", "TextRole", "ThemedElement", "UiScreen", "UiTextFloor", "UITheme", "UiThemeAsset" },
            ["Data/Config/CreditsText.cs"] = new[] { "UITheme" },
            ["Data/Config/GameContentCatalog.cs"] = new[] { "UiThemeAsset" },
            ["Gameplay/Combat/HealthController.cs"] = new[] { "DamagePopupManager" },
            ["Gameplay/Items/ChestPickup.cs"] = new[] { "ChestUI" },
            ["Gameplay/Items/PickupPoolManager.cs"] = new[] { "ChestUI" },
            ["Gameplay/Items/WeaponCachePickup.cs"] = new[] { "ArcanaChoiceScreen" },
        };

        private static readonly HashSet<string> CommonWords = new HashSet<string> { "Phase", "Arrow" };
        private static readonly Regex TypeDeclaration = new Regex(@"^\s*(public|internal)?\s*(static |sealed |abstract |partial )*(class|struct|enum|interface)\s+(\w+)", RegexOptions.Multiline);
        private static readonly Regex LineComment = new Regex(@"//[^\n]*");

        [Test]
        public void NoNewGameplayCode_CallsTheHudDirectly()
        {
            string root = Path.Combine(Application.dataPath, "_Project");
            string Rel(string path) => path.Substring(root.Length + 1).Replace('\\', '/');

            var hudTypes = Directory.GetFiles(Path.Combine(root, "UI", "HUD"), "*.cs", SearchOption.AllDirectories)
                .SelectMany(f => TypeDeclaration.Matches(File.ReadAllText(f)).Cast<Match>().Select(m => m.Groups[4].Value))
                .Where(t => !CommonWords.Contains(t))
                .Distinct().ToList();
            Assert.That(hudTypes.Count, Is.GreaterThan(30), "The HUD's types were found.");

            var problems = new List<string>();
            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string rel = Rel(file);
                if (rel.StartsWith("UI/") || rel.StartsWith("Tests/") || rel.Contains("/Editor/") || rel.StartsWith("Editor/")) continue;
                string text = LineComment.Replace(File.ReadAllText(file), "");
                var used = hudTypes.Where(t => Regex.IsMatch(text, $@"\b{t}\b")).ToList();
                if (used.Count == 0) continue;
                Allowed.TryGetValue(rel, out var ok);
                var extra = used.Where(t => ok == null || !ok.Contains(t)).ToList();
                if (extra.Count > 0) problems.Add($"{rel} now uses {string.Join(", ", extra)}");
            }
            Assert.That(problems, Is.Empty,
                "Gameplay code must not call the HUD directly (AGENTS section 3); raise an event instead:\n" + string.Join("\n", problems));
        }
    }
}
