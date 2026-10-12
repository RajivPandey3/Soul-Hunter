using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.UI;
using SoulHunter.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// The VS main menu sections in the real MainMenu (release item P3-02, 2026-10-09; wiki Main_menu, Collection, Ars_Gouda):
    /// Collection, Bestiary (once unlocked), Unlocks and Credits each open from their own button, focus their Back button,
    /// show the save's entries in readable text (4.5:1 on their surface), and Back returns focus to the button that opened
    /// them. Every save the test makes goes to a throw-away file.
    /// </summary>
    public class MainMenuSectionsPlayModeTests : GameplayPlayModeTestBase
    {
        private static IEnumerator BootMenu(System.Action<GameData> configure)
        {
            SceneManager.LoadSceneAsync("Bootstrap");
            float until = Time.realtimeSinceStartup + TimeoutSeconds;
            while (SceneManager.GetActiveScene().name != "MainMenu" && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"), "MainMenu loaded.");
            var save = GameServices.Instance.Get<SaveService>();
            save.UseFile(System.IO.Path.Combine(Application.temporaryCachePath, "playmode_test_save.json"));
            configure?.Invoke(save.CurrentData);
            // The menu reads the save when it is built: load it again with the test's save.
            var old = Object.FindFirstObjectByType<MainMenuNavigation>();
            SceneManager.LoadSceneAsync("MainMenu");
            until = Time.realtimeSinceStartup + TimeoutSeconds;
            while ((Object.FindFirstObjectByType<MainMenuNavigation>() == null || Object.FindFirstObjectByType<MainMenuNavigation>() == old)
                   && Time.realtimeSinceStartup < until) yield return null;
            for (int i = 0; i < 5; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
        }

        private static void FreshRecords(GameData data)
        {
            data.CollectionFound = new System.Collections.Generic.List<string>(CollectionBook.FoundFromTheStart) { "Item.Whip" };
            data.DiscoveredEvolutions = new System.Collections.Generic.List<string>();
            data.BestiaryKills = new System.Collections.Generic.List<EnemyKillRecord>
            {
                new EnemyKillRecord { Enemy = BestiaryCatalog.IdOf(BestiaryCatalog.All()[0]), Kills = 7 }
            };
            Bestiary.Unlock(data);
        }

        [UnityTest]
        public IEnumerator EachSection_Opens_ShowsTheSavesEntriesReadably_AndBackReturnsFocus()
        {
            yield return BootMenu(FreshRecords);
            var nav = Object.FindFirstObjectByType<MainMenuNavigation>();
            Assert.IsNotNull(nav.CollectionButton);
            Assert.IsNotNull(nav.BestiaryButton, "Unlocked on this save: the Bestiary button shows.");
            Assert.IsNotNull(nav.UnlocksButton);
            Assert.IsNotNull(nav.CreditsButton);

            // Collection.
            yield return Open(nav, nav.CollectionButton, nav.Collection);
            var collection = nav.Collection;
            var whip = collection.Cells.First(c => c.Entry.Id == "Item.Whip");
            Assert.That(whip.Icon.enabled && whip.Icon.sprite != null, Is.True, "A found weapon shows its icon.");
            var unknown = collection.Cells.First(c => !CollectionBook.IsFound(GameServices.Instance.Get<SaveService>().CurrentData, c.Entry.Id));
            Assert.That(unknown.Label.text, Is.EqualTo(CollectionMenuUI.Unknown), "An unknown entry is a question mark.");
            Assert.That(unknown.Icon.enabled, Is.False);
            Click(unknown.Cell, "an unknown entry");
            Assert.That(collection.InfoDescription, Is.EqualTo(CollectionMenuUI.UnknownText), "wiki: 'unknown and not yet found'.");
            Click(whip.Cell, "Whip");
            StringAssert.Contains(whip.Entry.Name, collection.InfoName);
            Assert.That(collection.CountText.text, Is.EqualTo($"Found 3 / {CollectionCatalog.All().Count}"));
            AssertReadable(collection, "Collection");
            yield return Back(nav, collection, nav.CollectionButton);

            // Bestiary: the first enemy is known (7 defeats), the second is not.
            yield return Open(nav, nav.BestiaryButton, nav.BestiaryScreen);
            var bestiary = nav.BestiaryScreen;
            Assert.That(bestiary.Selected, Is.SameAs(BestiaryCatalog.All()[0]), "It opens on the first enemy, as VS's does.");
            StringAssert.Contains("001", bestiary.Rows[0].Label.text);
            StringAssert.Contains(BestiaryCatalog.Name(bestiary.Rows[0].Enemy), bestiary.Rows[0].Label.text);
            StringAssert.Contains("Defeated: 7", bestiary.InfoDetails);
            StringAssert.Contains(BestiaryCatalog.UnknownName, bestiary.Rows[1].Label.text, "wiki: an unknown enemy's name is dashes.");
            Click(bestiary.Rows[1].Row, "an unknown enemy");
            StringAssert.Contains("HP: ?", bestiary.InfoDetails);
            StringAssert.Contains(BestiaryMenuUI.UnknownEntry, bestiary.InfoDetails);
            Assert.That(bestiary.CountText.text, Is.EqualTo($"Defeated 1 / {BestiaryCatalog.All().Count}"));
            AssertReadable(bestiary, "Bestiary");
            yield return Back(nav, bestiary, nav.BestiaryButton);

            // Unlocks.
            yield return Open(nav, nav.UnlocksButton, nav.Unlocks);
            var unlocks = nav.Unlocks;
            Assert.That(unlocks.RowTexts.Any(t => t.text.Contains("The Bestiary") && t.text.Contains(UnlocksMenuUI.DoneWord)), Is.True,
                "The Bestiary task reads done on this save.");
            Assert.That(unlocks.RowTexts.Any(t => t.text.Contains("Fill 60 entries in the Collection.") && t.text.Contains(UnlocksMenuUI.NotDoneWord)), Is.True);
            StringAssert.StartsWith("Done ", unlocks.CountText.text);
            AssertReadable(unlocks, "Unlocks");
            yield return Back(nav, unlocks, nav.UnlocksButton);

            // Credits.
            yield return Open(nav, nav.CreditsButton, nav.Credits);
            StringAssert.Contains("Kenney", nav.Credits.BodyText);
            StringAssert.Contains("Cinzel", nav.Credits.BodyText);
            AssertReadable(nav.Credits, "Credits", minimumTexts: 2); // a title and one scrolling body of credit lines
            yield return Back(nav, nav.Credits, nav.CreditsButton);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator TheBestiaryButton_StaysHidden_UntilTheBestiaryIsUnlocked()
        {
            yield return BootMenu(data => data.UnlockedAchievements.Remove(Bestiary.UnlockId));
            var nav = Object.FindFirstObjectByType<MainMenuNavigation>();
            Assert.IsNull(nav.BestiaryButton, "VS: the Bestiary appears only after its unlock.");
            Assert.That(nav.Layout.Buttons.Length, Is.EqualTo(8), "Start, Quick Start, Power Up, Collection, Unlocks, Credits, Options, Quit.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// Owner (2026-10-10): the same section screens in landscape and portrait. Each is laid out on a PC (16:9, 21:9, 5:4)
        /// and on the owner's phone held either way (1080x2400 at 480 dpi, touch): every shown control on the canvas, none
        /// overlapping, none under 48 dp on the phone; Back inside the window and under the content; no text the screen
        /// made under the touch floor (12 sp); the Collection grid takes fewer columns in portrait than on a PC.
        /// </summary>
        [UnityTest]
        public IEnumerator EachSection_FitsEveryScreen_WithBackPinnedAtTheBottom()
        {
            yield return BootMenu(FreshRecords);
            var nav = Object.FindFirstObjectByType<MainMenuNavigation>();
            var sections = new (Button Opener, MenuSectionScreen Screen)[]
            {
                (nav.CollectionButton, nav.Collection), (nav.BestiaryButton, nav.BestiaryScreen),
                (nav.UnlocksButton, nav.Unlocks), (nav.CreditsButton, nav.Credits)
            };
            foreach (var (opener, screen) in sections)
            {
                yield return Open(nav, opener, screen);
                var canvas = screen.GetComponentInParent<Canvas>().rootCanvas;
                var problems = new System.Collections.Generic.List<string>();
                var columns = new System.Collections.Generic.Dictionary<string, int>();
                try
                {
                    foreach (var d in ScreenSimulation.Devices)
                    {
                        yield return ScreenSimulation.At(d.Pixels, d.Dpi, d.Touch, canvas);
                        for (int i = 0; i < 3; i++) { Canvas.ForceUpdateCanvases(); yield return null; } // GridFit and the split settle
                        Canvas.ForceUpdateCanvases();
                        string what = $"{screen.name} on {d.Name}";
                        problems.AddRange(ScreenSimulation.Problems(what, ShownControls(screen)));
                        Rect back = ScreenSimulation.UnitsRect((RectTransform)screen.BackButton.transform);
                        Rect frame = ScreenSimulation.UnitsRect(screen.Frame), body = ScreenSimulation.UnitsRect(screen.Body);
                        if (!Inside(back, frame)) problems.Add($"{what}: Back {back} is outside the window {frame}.");
                        if (back.yMax > body.yMin + 0.5f) problems.Add($"{what}: Back {back} is not under the content {body}.");
                        if (!Inside(body, frame)) problems.Add($"{what}: the content {body} is outside the window {frame}.");
                        float floor = UiScreen.MinTextUnits - 0.01f;
                        foreach (var text in screen.OwnTexts().Concat(screen.BackButton.GetComponentsInChildren<TMP_Text>()))
                        {
                            if (!text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text)) continue;
                            text.ForceMeshUpdate();
                            if (text.fontSize < floor) problems.Add($"{what}: '{Short(text.text)}' is {text.fontSize:0.#} units, under the {UiScreen.MinTextUnits:0.#} floor.");
                        }
                        if (screen is CollectionMenuUI collection) columns[d.Name] = collection.Grid.constraintCount;
                    }
                }
                finally { ScreenSimulation.Restore(); }
                Assert.That(problems, Is.Empty, string.Join("\n", problems));
                if (screen is CollectionMenuUI)
                {
                    Assert.That(columns["Phone portrait"], Is.LessThan(columns["PC 1920x1080"]), "Fewer grid columns on a phone held upright.");
                    Assert.That(columns["Phone portrait"], Is.GreaterThanOrEqualTo(2), "Still a grid, not a list, in portrait.");
                }
                for (int i = 0; i < 2; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
                yield return Back(nav, screen, opener);
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// The controls a player can see: Back, the scrollbars, and the list entries wholly inside their scroll area's view
        /// (entries scrolled out of view are masked and cannot be touched).
        /// </summary>
        private static System.Collections.Generic.IEnumerable<Selectable> ShownControls(MenuSectionScreen screen)
        {
            foreach (var control in screen.GetComponentsInChildren<Selectable>())
            {
                var scroll = control.GetComponentInParent<ScrollRect>();
                if (scroll != null && control.transform.IsChildOf(scroll.content))
                {
                    Rect view = ScreenSimulation.UnitsRect(scroll.viewport), box = ScreenSimulation.UnitsRect((RectTransform)control.transform);
                    if (!Inside(box, view)) continue;
                }
                yield return control;
            }
        }

        private static bool Inside(Rect inner, Rect outer) =>
            inner.xMin >= outer.xMin - 0.5f && inner.yMin >= outer.yMin - 0.5f && inner.xMax <= outer.xMax + 0.5f && inner.yMax <= outer.yMax + 0.5f;

        private static string Short(string text) => text.Length > 40 ? text.Substring(0, 40) + "..." : text;

        private static IEnumerator Open(MainMenuNavigation nav, Button opener, MenuSectionScreen screen)
        {
            Click(opener, opener.name);
            for (int i = 0; i < 3; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
            Assert.That(screen.gameObject.activeInHierarchy, Is.True, $"{screen.name} opened.");
            Assert.That(nav.HomePanel.activeSelf, Is.False, "The home panel is hidden.");
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(screen.BackButton.gameObject), $"{screen.name}: Back is focused.");
            var rect = ((RectTransform)screen.BackButton.transform).rect.size;
            Assert.That(rect.x, Is.EqualTo(UITheme.Layout.ButtonWidth).Within(0.6f), "Back has the theme's one button size.");
            Assert.That(rect.y, Is.EqualTo(UITheme.Layout.ButtonHeight).Within(0.6f));
        }

        private static IEnumerator Back(MainMenuNavigation nav, MenuSectionScreen screen, Button opener)
        {
            Click(screen.BackButton, "Back");
            yield return null;
            Assert.That(screen.gameObject.activeSelf, Is.False);
            Assert.That(nav.HomePanel.activeSelf, Is.True, "Back returns to the home panel.");
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(opener.gameObject), "Back focuses the button that opened the screen.");
        }

        /// <summary>Every shown text reads at 4.5:1 or better on the surface it sits on (ui-ux-pro-max, Color Contrast).</summary>
        private static void AssertReadable(MenuSectionScreen screen, string what, int minimumTexts = 3)
        {
            int checkedTexts = 0;
            foreach (var (text, surface) in screen.ReadableTexts())
            {
                float contrast = UITheme.Contrast(text.color, surface);
                Assert.That(contrast, Is.GreaterThanOrEqualTo(4.5f), $"{what}: '{text.text}' reads at {contrast:0.0}:1.");
                checkedTexts++;
            }
            Assert.That(checkedTexts, Is.GreaterThanOrEqualTo(minimumTexts), $"{what} shows its texts.");
        }
    }
}
