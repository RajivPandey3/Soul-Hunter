using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// The UI theme is the single source of truth for the UI look (owner, 2026-10-04): GameContent names the active theme,
    /// every preset is complete and readable, UI code reads the theme (no colours of its own), and a theme change reaches
    /// themed elements at once.
    /// </summary>
    public class UiThemeTests
    {
        private static UiThemeAsset[] Presets() => AssetDatabase.FindAssets("t:UiThemeAsset", new[] { "Assets/_Project/Data/Config/UiThemes" })
            .Select(g => AssetDatabase.LoadAssetAtPath<UiThemeAsset>(AssetDatabase.GUIDToAssetPath(g))).Where(t => t != null).ToArray();

        [TearDown]
        public void RestoreActive() => UiThemeAsset.SetActive(null);

        [Test]
        public void GameContent_NamesTheActiveTheme_WithItsIconsAndPictures()
        {
            var theme = GameContentCatalog.Load().UiTheme;
            Assert.IsNotNull(theme, "GameContent has an active UI theme.");
            Assert.AreSame(theme, UiThemeAsset.Active, "The game uses GameContent's theme.");
            var i = theme.Icon;
            foreach (var (name, sprite) in new[] { ("Start", i.Start), ("Power Up", i.PowerUp), ("Options", i.Options), ("Quit", i.Quit), ("Back", i.Back),
                         ("Confirm", i.Confirm), ("Resume", i.Resume), ("Buy", i.Buy) })
                Assert.IsNotNull(sprite, $"The {name} icon is set.");
            Assert.IsNotNull(theme.Shape.Button, "The button picture is set.");
            Assert.That(theme.Shape.Button.border.x, Is.GreaterThan(0f), "The button picture is 9-sliced (its corners stay sharp at any size).");
            Assert.That(theme.Icon.Start.texture.width, Is.GreaterThanOrEqualTo(256), "Icons are high resolution.");
        }

        [Test]
        public void TheDefaultLook_IsEmberPactPro_TheOwnersChoice()
        {
            // Owner, 2026-10-07: "Ember Pact"; 2026-10-08, after the style sample: "ok pass now apply it in project every where".
            Assert.AreEqual("Assets/_Project/Data/Config/UiThemes/UiTheme_EmberPactPro.asset",
                AssetDatabase.GetAssetPath(GameContentCatalog.Load().UiTheme), "GameContent's active theme is Ember Pact Pro.");
        }

        [Test]
        public void ThePro_Look_HasEveryPictureFontAndRuleTheSampleShowed()
        {
            var t = GameContentCatalog.Load().UiTheme;
            Assert.IsNotNull(t.Shape.Button, "3D button plate");
            Assert.IsNotNull(t.Shape.Row, "bevelled rows, boxes, HUD plates and slots");
            Assert.IsNotNull(t.Shape.Inset, "slider groove");
            Assert.IsNotNull(t.Shape.Knob, "round knob");
            Assert.IsNotNull(t.Shape.Track, "switch track");
            Assert.IsNotNull(t.Shape.TickBox, "empty tick box");
            Assert.IsNotNull(t.Shape.Tick, "tick mark");
            Assert.IsNotNull(t.Font.Title, "Cinzel titles");
            Assert.IsNotNull(t.Font.Body, "Barlow labels");
            Assert.IsNotNull(t.Font.OnDarkMaterial, "raised text");
            Assert.IsTrue(t.Font.NoFakeBold, "no fake bold over the heavy fonts");
            Assert.IsTrue(t.Layouts.IconsLikeLabel, "button icons take the label's colour");
            Assert.IsFalse(t.Motions.Breathe, "every button looks the same size (owner, 2026-10-08)");
            Assert.IsFalse(t.Motions.HoverLift, "a hovered button does not grow either (owner, 2026-10-08)");
            Assert.IsNotNull(t.Icon.Coin, "gold amounts keep a coloured coin");
            Assert.AreNotEqual(t.Icon.Buy, t.Icon.Coin, "the coloured coin is not the one-colour Buy icon");
            Assert.IsTrue(t.Layouts.OpenTabBar, "the open tab is not shown by colour alone");
        }

        [Test]
        public void EveryPreset_IsReadable()
        {
            var presets = Presets();
            Assert.That(presets.Length, Is.GreaterThanOrEqualTo(4), "The four starting looks exist.");
            foreach (var t in presets)
            {
                Assert.That(Contrast(t.Text.OnDark, t.Surfaces.Panel), Is.GreaterThanOrEqualTo(4.5f), $"{t.DisplayName}: text on windows.");
                Assert.That(Contrast(t.Text.OnLight, t.Cards.Card), Is.GreaterThanOrEqualTo(4.5f), $"{t.DisplayName}: text on cards.");
                Assert.That(Contrast(t.Hud.LabelText, t.Hud.LabelBacking), Is.GreaterThanOrEqualTo(4.5f), $"{t.DisplayName}: HUD labels.");
                Assert.That(Contrast(t.Text.Accent, t.Surfaces.Panel), Is.GreaterThanOrEqualTo(3f), $"{t.DisplayName}: highlights on windows.");
                Assert.That(Contrast(t.Text.Locked, t.Buttons.Disabled), Is.GreaterThanOrEqualTo(3f), $"{t.DisplayName}: text on unusable buttons.");
                Assert.That(Contrast(t.Text.Locked, t.Cards.Locked), Is.GreaterThanOrEqualTo(3f), $"{t.DisplayName}: locked cards.");
                // VS PowerUps: a maxed PowerUp switched off is dark gray, apart from the yellowish maxed-and-on cell.
                Assert.That(Contrast(t.Text.OnDark, t.Cards.Off), Is.GreaterThanOrEqualTo(4.5f), $"{t.DisplayName}: Power Ups switched off.");
                Assert.That(t.Cards.Off, Is.Not.EqualTo(t.Cards.Maxed), $"{t.DisplayName}: off differs from maxed and on.");
                foreach (var (what, back) in new[] { ("ordinary", t.Buttons.Normal), ("main", t.Buttons.Primary), ("leaving", t.Buttons.Danger) })
                    Assert.That(Mathf.Max(Contrast(t.Text.OnDark, back), Contrast(t.Text.OnLight, back)), Is.GreaterThanOrEqualTo(4.5f),
                        $"{t.DisplayName}: {what} button labels (light or dark text, whichever reads best).");
                Assert.That(t.Buttons.Focus, Is.Not.EqualTo(t.Buttons.Normal), $"{t.DisplayName}: the chosen button differs.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(t.DisplayName) || string.IsNullOrWhiteSpace(t.Description), $"{t.name} has a name and description.");
            }
        }

        [Test]
        public void UiCode_KeepsNoColoursOfItsOwn()
        {
            // A colour literal with a real hue (not plain transparent white/black used for fades) belongs in the theme.
            var literal = new Regex(@"new\s+Color(32)?\s*\(\s*([0-9.]+)f?\s*,\s*([0-9.]+)f?\s*,\s*([0-9.]+)f?");
            var files = Directory.GetFiles("Assets/_Project/UI", "*.cs", SearchOption.AllDirectories)
                .Concat(new[] { "Assets/_Project/Core/Game/WanderingMerchant.cs", "Assets/_Project/Core/Game/CharacterUnlockTracker.cs" })
                .Where(f => !f.Replace('\\', '/').EndsWith("Theme/UiThemeAsset.cs"));
            var offenders = files.SelectMany(f => File.ReadAllLines(f).Select((line, n) => (f, n, line)))
                .Where(x =>
                {
                    var m = literal.Match(x.line);
                    if (!m.Success) return false;
                    var inv = System.Globalization.CultureInfo.InvariantCulture;
                    float r = float.Parse(m.Groups[2].Value, inv), g = float.Parse(m.Groups[3].Value, inv), b = float.Parse(m.Groups[4].Value, inv);
                    bool plain = (r == g && g == b) && (r == 0f || r == 1f || r == 255f);
                    return !plain;
                })
                .Select(x => $"{Path.GetFileName(x.f)}:{x.n + 1}: {x.line.Trim()}").ToList();
            Assert.That(offenders, Is.Empty, "Colours that belong in the UI theme:\n" + string.Join("\n", offenders));
            // Rich-text colours too: <color=#RRGGBB> written into a string is a colour of the screen's own (Results, 2026-10-08).
            var hex = new Regex(@"<color=#[0-9A-Fa-f]{6}");
            var hexOffenders = files.SelectMany(f => File.ReadAllLines(f).Select((line, n) => (f, n, line)))
                .Where(x => hex.IsMatch(x.line)).Select(x => $"{Path.GetFileName(x.f)}:{x.n + 1}: {x.line.Trim()}").ToList();
            Assert.That(hexOffenders, Is.Empty, "Rich-text colours that belong in the UI theme: " + string.Join(" | ", hexOffenders));
            // Bold goes through UITheme.Bold, so a theme with heavy fonts adds no fake bold (Pro look, 2026-10-08).
            var boldOffenders = files.Where(f => !f.Replace('\\', '/').EndsWith("HUD/UITheme.cs"))
                .SelectMany(f => File.ReadAllLines(f).Select((line, n) => (f, n, line)))
                .Where(x => x.line.Contains("<b>")).Select(x => $"{Path.GetFileName(x.f)}:{x.n + 1}").ToList();
            Assert.That(boldOffenders, Is.Empty, "Bold text that should use UITheme.Bold: " + string.Join(", ", boldOffenders));
            var oldPath = files.Where(f => File.ReadAllText(f).Contains("ApplyFocusColors")).ToList();
            Assert.That(oldPath, Is.Empty, "No second styling path: " + string.Join(", ", oldPath));
        }

        [Test]
        public void AThemeChange_RestylesThemedElements_AtOnce()
        {
            var look = ScriptableObject.CreateInstance<UiThemeAsset>();
            var go = new GameObject("Button", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            var text = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI)).GetComponent<TMPro.TextMeshProUGUI>();
            try
            {
                UiThemeAsset.SetActive(look);
                var button = go.GetComponent<UnityEngine.UI.Button>();
                UITheme.ApplyButton(button, go.GetComponent<UnityEngine.UI.Image>(), UITheme.ButtonKind.Primary);
                UITheme.ApplyText(text, ThemedElement.TextRole.Accent);
                Assert.AreEqual(look.Buttons.Primary, button.colors.normalColor);
                Assert.AreEqual(look.Text.Accent, text.color);

                look.Buttons.Primary = new Color(0.1f, 0.6f, 0.2f, 1f);
                look.Text.Accent = new Color(0.9f, 0.2f, 0.7f, 1f);
                UiThemeAsset.NotifyChanged(); // what Theme Studio and the Inspector do after an edit
                Assert.AreEqual(look.Buttons.Primary, button.colors.normalColor, "The button took the new colour at once.");
                Assert.AreEqual(look.Text.Accent, text.color, "The text took the new colour at once.");

                var other = ScriptableObject.CreateInstance<UiThemeAsset>();
                other.Buttons.Primary = Color.magenta;
                UiThemeAsset.SetActive(other);
                Assert.AreEqual(Color.magenta, button.colors.normalColor, "Switching the look restyles too.");
                Object.DestroyImmediate(other);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(text.gameObject);
                Object.DestroyImmediate(look);
            }
        }

        private static float Luminance(Color c)
        {
            float L(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * L(c.r) + 0.7152f * L(c.g) + 0.0722f * L(c.b);
        }

        private static float Contrast(Color a, Color b)
        {
            float la = Luminance(a), lb = Luminance(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }
    }
}
