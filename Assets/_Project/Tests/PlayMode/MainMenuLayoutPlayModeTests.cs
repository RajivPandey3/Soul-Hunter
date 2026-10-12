using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner (2026-10-03, "main menu panel thik karo"): the title and the buttons sit inside the background frame's
    /// open area, centred on its ornament, all on screen, none overlapping, in the menu's own button look. Since P3-02
    /// (2026-10-09) the menu has seven or eight buttons (Collection, Unlocks, Credits and, once unlocked, Bestiary joined
    /// them) in two columns, read left to right, top to bottom; every button keeps the one size and the one gap.
    /// </summary>
    public class MainMenuLayoutPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator TitleAndButtons_SitInsideTheFrame_OnScreen_CentredOnTheOrnament()
        {
            var load = SceneManager.LoadSceneAsync("Bootstrap");
            Assert.IsNotNull(load);
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu");
            for (int i = 0; i < 5; i++) yield return null;

            var nav = Object.FindFirstObjectByType<MainMenuNavigation>();
            Assert.IsNotNull(nav, "MainMenuNavigation.");
            var layout = nav.Layout;
            Assert.IsNotNull(layout, "The menu has its frame layout.");
            Assert.IsTrue(layout.Title != null || layout.Logo != null, "The logo was found.");
            int count = layout.Buttons.Length;
            Assert.That(count, Is.EqualTo(nav.BestiaryButton != null ? 9 : 8), "Start, Quick Start, Power Up, Collection, (Bestiary), Unlocks, Credits, Options, Quit.");
            // Up to eight buttons: two columns of four. Nine (with the Bestiary): three columns where the frame is wide
            // enough for them, else two (MainMenuLayout.MaxTwoColumns).
            int columns = layout.Columns;
            if (count <= MainMenuLayout.MaxTwoColumns) Assert.That(columns, Is.EqualTo(2));
            else Assert.That(columns, Is.EqualTo(2).Or.EqualTo(3));
            Assert.That(layout.Buttons[1], Is.SameAs(nav.QuickStartButton), "Quick Start follows Start (wiki Main_menu's order).");

            Rect area = ScreenRect(layout.Area);
            var screen = new Rect(0f, 0f, Screen.width, Screen.height);
            float frameCentre = area.xMin + area.width * MainMenuLayout.FrameCentreX;
            float innerTop = area.yMin + area.height * 0.60f, innerBottom = area.yMin + area.height * 0.10f;

            Rect block = ScreenRect((RectTransform)layout.Buttons[0].transform);
            for (int i = 0; i < count; i++)
            {
                var button = layout.Buttons[i];
                Assert.IsNotNull(button, $"Button {i}.");
                Rect r = ScreenRect((RectTransform)button.transform);
                block = Rect.MinMaxRect(Mathf.Min(block.xMin, r.xMin), Mathf.Min(block.yMin, r.yMin), Mathf.Max(block.xMax, r.xMax), Mathf.Max(block.yMax, r.yMax));
                Assert.That(Inside(r, screen), $"{button.name} {r} is on screen {screen}.");
                Assert.That(r.yMin >= innerBottom - 1f && r.yMax <= innerTop + 1f, $"{button.name} {r} is inside the frame (y {innerBottom}-{innerTop}).");
                Assert.That(r.xMin >= area.xMin && r.xMax <= area.xMax, $"{button.name} {r} is inside the frame's width.");
                for (int j = 0; j < i; j++)
                    Assert.That(r.Overlaps(ScreenRect((RectTransform)layout.Buttons[j].transform)), Is.False, $"{button.name} does not overlap {layout.Buttons[j].name}.");
                // The look comes from the active theme (2026-10-04): Start is the main action, Quit (last) the leaving one.
                Assert.IsNotNull(button.GetComponent<SoulHunter.Gameplay.UI.ThemedElement>(), $"{button.name} is themed.");
                var expected = i == 0 ? SoulHunter.Gameplay.UI.UITheme.Primary : i == count - 1 ? SoulHunter.Gameplay.UI.UITheme.DangerButton : SoulHunter.Gameplay.UI.UITheme.Button;
                Assert.AreEqual(expected, button.colors.normalColor, $"{button.name} has its theme colour.");
                AssertIconAndLabelAligned(button);
            }
            Assert.That(layout.Buttons[count - 1], Is.SameAs(nav.QuitButton), "Quit is the last button.");
            // The button block is centred on the frame's ornament (a short last row is centred too).
            Assert.That(block.center.x, Is.EqualTo(frameCentre).Within(screen.width * 0.01f), "The buttons are centred on the frame's ornament.");
            if (count % columns == 1)
                Assert.That(ScreenRect((RectTransform)layout.Buttons[count - 1].transform).center.x, Is.EqualTo(frameCentre).Within(screen.width * 0.01f),
                    "The lone last button is centred.");
            if (layout.Logo != null)
            {
                // The HD logo (owner review 2026-10-08): on screen, inside the frame, centred on the ornament, a clear gap
                // (at least the theme's button gap) above Start, and sharp: the picture has at least the pixels it is drawn with.
                Rect logo = ScreenRect(layout.Logo.rectTransform);
                Rect start = ScreenRect((RectTransform)layout.Buttons[0].transform);
                Assert.That(Inside(logo, screen), $"The logo {logo} is on screen.");
                Assert.That(logo.yMax, Is.LessThanOrEqualTo(innerTop + 1f), "The logo is inside the frame's open area.");
                Assert.That(logo.center.x, Is.EqualTo(frameCentre).Within(screen.width * 0.01f), "The logo is centred on the ornament.");
                float scale = start.height / SoulHunter.Gameplay.UI.UITheme.Layout.ButtonHeight;
                Assert.That(logo.yMin - start.yMax, Is.GreaterThanOrEqualTo(SoulHunter.Gameplay.UI.UITheme.Layout.ButtonGap * scale - 1f),
                    "A clear gap between the logo and Start.");
                Assert.That(layout.Logo.sprite.rect.width, Is.GreaterThanOrEqualTo(logo.width), "The logo is not stretched past its pixels.");
                Assert.That(nav.PowerUpButton.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo("Power Up"), "Power Up, as the shop writes it.");
                // Every button the same size, with the same gap between each, across a row and down a column (owner
                // review 2026-10-08: one size and one gap).
                Rect r0 = ScreenRect((RectTransform)layout.Buttons[0].transform);
                float gap0 = SoulHunter.Gameplay.UI.UITheme.Layout.ButtonGap * scale;
                for (int b = 0; b < count; b++)
                {
                    Rect rb = ScreenRect((RectTransform)layout.Buttons[b].transform);
                    Assert.That(rb.width, Is.EqualTo(r0.width).Within(0.5f), $"{layout.Buttons[b].name} is as wide as Start.");
                    Assert.That(rb.height, Is.EqualTo(r0.height).Within(0.5f), $"{layout.Buttons[b].name} is as tall as Start.");
                    if (b % columns != 0)
                        Assert.That(rb.xMin - ScreenRect((RectTransform)layout.Buttons[b - 1].transform).xMax, Is.EqualTo(gap0).Within(0.6f),
                            $"The gap beside {layout.Buttons[b].name} is the theme's gap.");
                    if (b >= columns)
                        Assert.That(ScreenRect((RectTransform)layout.Buttons[b - columns].transform).yMin - rb.yMax, Is.EqualTo(gap0).Within(0.6f),
                            $"The gap above {layout.Buttons[b].name} is the theme's gap.");
                }
                Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
                yield break;
            }
            Rect title = ScreenRect(layout.Title.rectTransform);
            Assert.That(Inside(title, screen), "The title is on screen.");
            Assert.That(title.yMin, Is.GreaterThanOrEqualTo(ScreenRect((RectTransform)layout.Buttons[0].transform).yMax - 0.5f), "The title is above the buttons.");
            // What is drawn of the logo (the sprite quads themselves), not just its text box, is centred on the ornament.
            // The logo sprites were imported with a bearing of minus half their width, which drew each one half its width
            // left of its slot; the text box looked centred while the logo was not.
            layout.Title.ForceMeshUpdate();
            var info = layout.Title.textInfo;
            float minX = float.MaxValue, maxX = float.MinValue;
            for (int i = 0; i < info.characterCount; i++)
            {
                var c = info.characterInfo[i];
                if (!c.isVisible) continue;
                minX = Mathf.Min(minX, c.bottomLeft.x);
                maxX = Mathf.Max(maxX, c.topRight.x);
            }
            Assert.That(maxX, Is.GreaterThan(minX), "The logo draws its sprites.");
            Vector3 drawnCentre = layout.Title.rectTransform.TransformPoint(new Vector3((minX + maxX) * 0.5f, 0f, 0f));
            float drawnX = RectTransformUtility.WorldToScreenPoint(null, drawnCentre).x;
            Assert.That(drawnX, Is.EqualTo(frameCentre).Within(screen.width * 0.01f), "The drawn logo is centred on the ornament.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static bool Inside(Rect r, Rect outer) =>
            r.xMin >= outer.xMin - 0.5f && r.yMin >= outer.yMin - 0.5f && r.xMax <= outer.xMax + 0.5f && r.yMax <= outer.yMax + 0.5f;

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var canvas = rect.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
        }

        /// <summary>
        /// The theme's icon sits on the button's left, vertically centred; the label is centred in the space beside it and
        /// never overlaps the icon.
        /// </summary>
        private static void AssertIconAndLabelAligned(Button button)
        {
            var icon = button.transform.Find("ThemeIcon") as RectTransform;
            if (SoulHunter.Gameplay.UI.UITheme.Icons.Start == null) return; // a theme without icons
            Assert.IsNotNull(icon, $"{button.name} shows its theme icon.");
            Assert.IsTrue(icon.GetComponent<Image>().enabled && icon.GetComponent<Image>().sprite != null, $"{button.name}'s icon is drawn.");
            Rect b = ScreenRect((RectTransform)button.transform), ic = ScreenRect(icon);
            Assert.That(ic.center.y, Is.EqualTo(b.center.y).Within(b.height * 0.08f), $"{button.name}'s icon is vertically centred.");
            Assert.That(ic.xMin, Is.GreaterThanOrEqualTo(b.xMin - 0.5f), $"{button.name}'s icon is inside the button.");
            Assert.That(ic.center.x, Is.LessThan(b.center.x), $"{button.name}'s icon is on the left.");
            var label = button.GetComponentInChildren<TMPro.TMP_Text>();
            label.ForceMeshUpdate();
            var bounds = label.textBounds;
            float textLeft = RectTransformUtility.WorldToScreenPoint(null, label.rectTransform.TransformPoint(bounds.min)).x;
            Assert.That(textLeft, Is.GreaterThanOrEqualTo(ic.xMax - 1f), $"{button.name}'s label does not overlap its icon.");
            Rect lr = ScreenRect(label.rectTransform);
            Assert.That(lr.center.y, Is.EqualTo(b.center.y).Within(b.height * 0.08f), $"{button.name}'s label is vertically centred.");
        }
    }
}
