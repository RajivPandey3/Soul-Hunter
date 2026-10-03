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
    /// Owner (2026-10-03, "main menu panel thik karo"): the title and the four buttons sit inside the background frame's
    /// open area, centred on its ornament, all on screen, none overlapping, in the menu's own button look.
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
            Assert.IsNotNull(layout.Title, "The logo was found.");
            Assert.That(layout.Buttons.Length, Is.EqualTo(4));

            Rect area = ScreenRect(layout.Area);
            var screen = new Rect(0f, 0f, Screen.width, Screen.height);
            float frameCentre = area.xMin + area.width * MainMenuLayout.FrameCentreX;
            float innerTop = area.yMin + area.height * 0.60f, innerBottom = area.yMin + area.height * 0.10f;

            Rect previous = default;
            for (int i = 0; i < layout.Buttons.Length; i++)
            {
                var button = layout.Buttons[i];
                Assert.IsNotNull(button, $"Button {i}.");
                Rect r = ScreenRect((RectTransform)button.transform);
                Assert.That(Inside(r, screen), $"{button.name} {r} is on screen {screen}.");
                Assert.That(r.yMin >= innerBottom - 1f && r.yMax <= innerTop + 1f, $"{button.name} {r} is inside the frame (y {innerBottom}-{innerTop}).");
                Assert.That(r.center.x, Is.EqualTo(frameCentre).Within(screen.width * 0.01f), $"{button.name} is centred on the frame's ornament.");
                if (i > 0) Assert.That(r.yMax, Is.LessThanOrEqualTo(previous.yMin + 0.5f), $"{button.name} does not overlap the button above.");
                previous = r;
                Assert.IsNotNull(button.GetComponent<Outline>(), $"{button.name} has the red edge.");
                Assert.AreEqual(MainMenuLayout.ButtonNormal, button.colors.normalColor, $"{button.name} is dark crimson.");
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
    }
}
