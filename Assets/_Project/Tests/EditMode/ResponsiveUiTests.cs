using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Owner (2026-10-10): the game plays in landscape and portrait from the same screens, which adjust themselves, with no
    /// duplicate or parallel layout. These check the one shared rule and its building blocks.
    /// </summary>
    public class ResponsiveUiTests
    {
        [TearDown]
        public void Clear()
        {
            UiScreen.TestPixels = null;
            UiScreen.TestDpi = null;
            UiScreen.TestTouch = null;
        }

        [Test]
        public void TheScale_KeepsA1080pMonitor_AndMakesAPhonesBodyText16sp()
        {
            // A 1080p monitor is unchanged: 1 pixel per unit, a 1920x1080 canvas.
            Assert.That(UiScreen.ScaleFor(new Vector2(1920, 1080), 96f, false, UiScreen.BodyTextUnits), Is.EqualTo(1f).Within(1e-4f));
            // Portrait on the same rule: the short side is 1080 units, the canvas is tall.
            Assert.That(UiScreen.ScaleFor(new Vector2(1080, 1920), 96f, false, UiScreen.BodyTextUnits), Is.EqualTo(1f).Within(1e-4f));
            // The owner's phone (1080x2400 at 480 dpi, touch): 28-unit body text is 16 sp = 48 px, so 48/28 px per unit
            // (owner, 2026-10-11: text and icons were too big when a 68-unit button set the scale).
            float phone = UiScreen.ScaleFor(new Vector2(1080, 2400), 480f, true, UiScreen.BodyTextUnits);
            Assert.That(phone, Is.EqualTo(48f / 28f).Within(1e-3f));
            Assert.That(UiScreen.BodyTextUnits * phone * 160f / 480f, Is.EqualTo(16f).Within(0.05f), "The theme's body text is 16 sp on the phone.");
            Assert.That(phone, Is.LessThan(144f / 68f), "Smaller than the first rule (a 68-unit button at 48 dp).");
            // Rotated, the phone keeps the same scale (same short side), so nothing changes size on a rotation.
            Assert.That(UiScreen.ScaleFor(new Vector2(2400, 1080), 480f, true, UiScreen.BodyTextUnits), Is.EqualTo(phone).Within(1e-4f));
            // Without touch the dp rule does not apply.
            Assert.That(UiScreen.ScaleFor(new Vector2(1080, 2400), 480f, false, UiScreen.BodyTextUnits), Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void OnTouch_NoStandardControlIsUnder48dp()
        {
            UiScreen.TestPixels = new Vector2(1080, 2400);
            UiScreen.TestDpi = 480f;
            UiScreen.TestTouch = true;
            var go = new GameObject("Tab", typeof(RectTransform));
            try
            {
                var element = go.AddComponent<ThemedElement>();
                element.Kind = ThemedElement.Role.Button;
                element.Size = ThemedElement.SizeRole.Tab;
                var theme = UiThemeAsset.Active;
                Vector2 size = element.StandardSize(theme).Value;
                float dp = UiScreen.ScaleFactor * 160f / UiScreen.Dpi;
                Assert.That(Mathf.Min(size.x, size.y) * dp, Is.GreaterThanOrEqualTo(47.9f), $"A tab is {size} units = {size * dp} dp.");
                Assert.That(theme.Layouts.ButtonGap * dp, Is.GreaterThanOrEqualTo(8f), "8 dp between targets (ui-ux-pro-max Touch Spacing).");

                UiScreen.TestTouch = false;
                Assert.That(element.StandardSize(theme).Value, Is.EqualTo(new Vector2(theme.Layouts.TabWidth, theme.Layouts.TabHeight)),
                    "Without touch the theme's own size stays.");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void ARowWraps_InsteadOfShrinkingItsControls()
        {
            var theme = UiThemeAsset.Active;
            var parent = new GameObject("Parent", typeof(RectTransform)).GetComponent<RectTransform>();
            try
            {
                parent.sizeDelta = new Vector2(theme.Layouts.ButtonWidth * 1.6f, 600f); // room for one button per line
                var row = new GameObject("Row", typeof(RectTransform)).GetComponent<RectTransform>();
                row.SetParent(parent, false);
                for (int i = 0; i < 3; i++)
                {
                    var b = new GameObject("B" + i, typeof(RectTransform));
                    b.transform.SetParent(row, false);
                    var e = b.AddComponent<ThemedElement>();
                    e.Kind = ThemedElement.Role.Button;
                    e.Size = ThemedElement.SizeRole.Standard;
                }
                var themed = row.gameObject.AddComponent<ThemedRow>();
                themed.Layout();
                Assert.That(themed.Lines, Is.EqualTo(3), "Three buttons that do not fit side by side take three lines.");
                Assert.That(row.localScale.x, Is.EqualTo(1f), "The controls keep their full size.");
                float y0 = ((RectTransform)row.GetChild(0)).anchoredPosition.y, y1 = ((RectTransform)row.GetChild(1)).anchoredPosition.y;
                Assert.That(y0 - y1, Is.EqualTo(theme.Layouts.ButtonHeight + theme.Layouts.ButtonGap).Within(0.01f), "Lines are a gap apart.");

                parent.sizeDelta = new Vector2(theme.Layouts.ButtonWidth * 4f, 600f);
                themed.Layout();
                Assert.That(themed.Lines, Is.EqualTo(1), "With room they share one line.");
            }
            finally { Object.DestroyImmediate(parent.gameObject); }
        }

        [Test]
        public void AGrid_TakesTheColumnsItsWidthFits()
        {
            Assert.That(UiScreen.ColumnsFor(1700f, 300f, 12f, 8), Is.EqualTo(5));
            Assert.That(UiScreen.ColumnsFor(490f, 150f, 12f, 8), Is.EqualTo(3), "A portrait phone keeps 3 columns of 150.");
            Assert.That(UiScreen.ColumnsFor(100f, 300f, 12f, 8), Is.EqualTo(1), "Never fewer than one.");
            Assert.That(UiScreen.ColumnsFor(9000f, 100f, 12f, 6), Is.EqualTo(6), "Never more than the maximum.");
        }

        [Test]
        public void ASplit_IsSideBySideWhenWide_AndStackedWhenTall()
        {
            var root = new GameObject("Split", typeof(RectTransform)).GetComponent<RectTransform>();
            try
            {
                var first = new GameObject("First", typeof(RectTransform)).GetComponent<RectTransform>();
                var second = new GameObject("Second", typeof(RectTransform)).GetComponent<RectTransform>();
                first.SetParent(root, false);
                second.SetParent(root, false);
                root.sizeDelta = new Vector2(1600f, 900f);
                var split = AdaptiveSplit.Between(root, first, second, 0.6f, 0.4f);
                Assert.That(split.IsTall, Is.False);
                Assert.That(first.anchorMax.x, Is.EqualTo(0.6f).Within(1e-4f), "Wide: the first area takes the left 60%.");
                Assert.That(second.anchorMin.x, Is.EqualTo(0.6f).Within(1e-4f));

                root.sizeDelta = new Vector2(500f, 1100f);
                split.Layout(force: true);
                Assert.That(split.IsTall, Is.True);
                Assert.That(first.anchorMin.y, Is.EqualTo(0.6f).Within(1e-4f), "Tall: the first area takes the top 40%.");
                Assert.That(second.anchorMax.y, Is.EqualTo(0.6f).Within(1e-4f));
                Assert.That(first.anchorMax.x, Is.EqualTo(1f), "Tall: both areas use the full width.");
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        [Test]
        public void OnlyResponsiveCanvas_SetsUpACanvasScaler()
        {
            // 0 duplicacy (owner, 2026-10-10): 13 screens each set up their own CanvasScaler before; now one place does.
            string root = Path.Combine(Application.dataPath, "_Project");
            var offenders = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Replace('\\', '/').Contains("/Tests/") && !f.EndsWith("ResponsiveCanvas.cs"))
                .Where(f => Regex.IsMatch(File.ReadAllText(f), @"AddComponent<(UnityEngine\.UI\.)?CanvasScaler>|\.uiScaleMode\s*=|referenceResolution\s*="))
                .Select(f => f.Substring(root.Length + 1)).ToList();
            Assert.That(offenders, Is.Empty, "These set up a CanvasScaler of their own: " + string.Join(", ", offenders));
        }
    }
}
