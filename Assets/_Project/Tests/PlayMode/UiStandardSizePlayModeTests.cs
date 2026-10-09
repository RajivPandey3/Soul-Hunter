using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;
using SoulHunter.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner (2026-10-07): "button size sabka ek jaisa banao, bada nahi, standard ... tab ka size sahi karo, pure project
    /// mein ek hee standard". On every screen, every action button has the theme's one button size and every tab or toggle
    /// chip its one tab size; no two buttons overlap. Plus the character select fixes from the owner's screenshot: mode
    /// labels readable and inside their chip, card names bold, the chosen card framed.
    /// </summary>
    public class UiStandardSizePlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator MenuScreens_UseTheOneButtonSize_AndTheOneTabSize()
        {
            SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            yield return Settle();
            var navigation = Object.FindFirstObjectByType<MainMenuNavigation>();
            CheckScreen(navigation.HomePanel.transform, "Main menu", 4);

            navigation.ShowCharacters();
            yield return Settle();
            var select = navigation.CharacterPanel.transform;
            // Back, Confirm, Favourite; tabs: six run modes, FILTER, and the VS sort and order (2026-10-09).
            CheckScreen(select, "Character select", 3, tabs: 6 + 1 + 2);
            CheckCharacterSelect(select);

            navigation.ShowPowerUps();
            yield return Settle();
            CheckScreen(navigation.PowerUpPanel.transform, "Power Up", 3); // Back, Buy, Refund

            // The VS main menu sections (P3-02, 2026-10-09): Back is each one's only standard button.
            navigation.ShowCollection();
            yield return Settle();
            CheckScreen(navigation.Collection.transform, "Collection", 1);
            navigation.ShowUnlocks();
            yield return Settle();
            CheckScreen(navigation.Unlocks.transform, "Unlocks", 1);
            navigation.ShowCredits();
            yield return Settle();
            CheckScreen(navigation.Credits.transform, "Credits", 1);
            navigation.ShowHome();
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator GameScreens_UseTheOneButtonSize_AndTheOneTabSize()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.enabled = false;

            var system = GameSystemManager.Instance;
            system.TogglePause();
            yield return Settle();
            CheckScreen(system.PauseMenuPanel.transform, "Pause", 3);

            var options = OptionsMenuUI.Open();
            yield return Settle();
            CheckScreen(options.transform, "Options", 1, tabs: 5); // Back (Reset Controls sits on the hidden Controls tab); Favorites, Display, Sound, Gameplay, Controls
            options.Close();
            system.Resume();
            yield return null;

            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            levelUp.AddCharges(1, 1, 1);
            var experience = player.GetComponent<PlayerExperience>();
            experience.AddXP(experience.XPToNextLevel - experience.CurrentXP);
            yield return WaitUntilRealtime(() => levelUp.IsLevelUpActive, "a level-up");
            yield return Settle();
            var bar = Object.FindFirstObjectByType<LevelUpUI>().GetComponentsInChildren<Button>(false)
                .Where(b => b.name == "Reroll_Button" || b.name == "Skip_Button" || b.name == "Banish_Button").ToList();
            Assert.That(bar.Count, Is.EqualTo(3), "Reroll, Skip and Banish show with a use each.");
            var theme = UiThemeAsset.Active;
            foreach (var b in bar)
                AssertSize((RectTransform)b.transform, new Vector2(theme.Layouts.ButtonWidth, theme.Layouts.ButtonHeight), $"Level up {b.name}");
            AssertApart(bar.Select(b => (RectTransform)b.transform).ToList(), "Level up");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // ---------------------------------------------------------------- checks

        private static IEnumerator Settle()
        {
            for (int i = 0; i < 3; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
        }

        /// <summary>Every themed button under <paramref name="root"/> that is shown has its standard size; none overlap.</summary>
        private static void CheckScreen(Transform root, string screen, int standard, int tabs = -1)
        {
            var theme = UiThemeAsset.Active;
            var buttons = root.GetComponentsInChildren<ThemedElement>(false)
                .Where(e => e.Kind == ThemedElement.Role.Button && e.StandardSize(theme).HasValue && e.gameObject.activeInHierarchy).ToList();
            int standards = buttons.Count(e => e.Size == ThemedElement.SizeRole.Standard);
            int tabCount = buttons.Count(e => e.Size == ThemedElement.SizeRole.Tab);
            Assert.That(standards, Is.GreaterThanOrEqualTo(standard), $"{screen}: {standards} standard buttons ({Names(buttons)}).");
            if (tabs >= 0) Assert.That(tabCount, Is.EqualTo(tabs), $"{screen}: {tabCount} tabs ({Names(buttons)}).");
            foreach (var e in buttons) AssertSize((RectTransform)e.transform, e.StandardSize(theme).Value, $"{screen} {e.name}");
            // Every label on them reads: 4.5:1 against the button's own colour (ui-ux-pro-max, Color Contrast).
            foreach (var e in buttons)
            {
                var selectable = e.GetComponent<Selectable>();
                foreach (var label in e.GetComponentsInChildren<TextMeshProUGUI>(false).Where(t => !string.IsNullOrWhiteSpace(t.text)))
                {
                    // Against the plate the player sees: a locked button is drawn in its disabled colour.
                    var plate = selectable.IsInteractable() ? selectable.colors.normalColor : selectable.colors.disabledColor;
                    float contrast = UITheme.Contrast(label.color, plate);
                    Assert.That(contrast, Is.GreaterThanOrEqualTo(4.5f), $"{screen} {e.name}: '{label.text}' contrast {contrast:0.0}:1 on its button ({(selectable.IsInteractable() ? "usable" : "locked")}).");
                    Rect box = ScreenRect((RectTransform)e.transform), text = TextRect(label);
                    Assert.That(text.xMin >= box.xMin - 1f && text.xMax <= box.xMax + 1f, Is.True,
                        $"{screen} {e.name}: '{label.text}' {text} stays inside its button {box}.");
                }
            }
            AssertApart(buttons.Select(e => (RectTransform)e.transform).ToList(), screen);
        }

        private static string Names(IEnumerable<ThemedElement> e) => string.Join(", ", e.Select(x => $"{x.name}:{x.Size}"));

        private static void AssertSize(RectTransform rect, Vector2 expected, string what)
        {
            Vector2 size = rect.rect.size;
            Assert.That(Mathf.Abs(size.x - expected.x) < 0.6f && Mathf.Abs(size.y - expected.y) < 0.6f, Is.True,
                $"{what} is {size.x:0.#}x{size.y:0.#}, the theme's size is {expected.x:0.#}x{expected.y:0.#}.");
        }

        private static void AssertApart(List<RectTransform> rects, string screen)
        {
            for (int i = 0; i < rects.Count; i++)
                for (int j = i + 1; j < rects.Count; j++)
                {
                    Rect a = ScreenRect(rects[i]), b = ScreenRect(rects[j]);
                    Assert.That(a.Overlaps(b), Is.False, $"{screen}: {rects[i].name} {a} and {rects[j].name} {b} overlap.");
                }
        }

        private static void CheckCharacterSelect(Transform select)
        {
            var theme = UiThemeAsset.Active;
            var modes = select.GetComponentsInChildren<Toggle>(false).Where(t => t.name.StartsWith("Mode_")).ToList();
            Assert.That(modes.Count, Is.EqualTo(6));
            foreach (var mode in modes)
            {
                var label = mode.GetComponentsInChildren<TextMeshProUGUI>(true).First(t => t.name == "Label");
                var box = (RectTransform)mode.transform.Find("Box");
                Rect chip = ScreenRect((RectTransform)mode.transform), text = TextRect(label), tick = ScreenRect(box);
                Assert.That(text.yMin >= chip.yMin - 1f && text.yMax <= chip.yMax + 1f && text.xMin >= chip.xMin - 1f && text.xMax <= chip.xMax + 1f,
                    Is.True, $"{mode.name}: its label {text} is inside the chip {chip}.");
                Assert.That(text.xMin, Is.GreaterThanOrEqualTo(tick.xMax), $"{mode.name}: the label is beside the tick box, not over it.");
                float contrast = UITheme.Contrast(label.color, mode.colors.normalColor);
                Assert.That(contrast, Is.GreaterThanOrEqualTo(4.5f), $"{mode.name}: label contrast {contrast:0.0}:1 on its chip.");
            }

            var ui = select.GetComponentInChildren<CharacterSelectionUI>(true) ?? Object.FindFirstObjectByType<CharacterSelectionUI>();
            foreach (var card in ui.Cards)
            {
                var name = card.GetComponentsInChildren<TextMeshProUGUI>(true).First(t => t.name == "Name");
                // Bold: Unity's bold style, or the theme's heavy font with fake bold switched off (the Pro look, 2026-10-08).
                var look = UiThemeAsset.Active;
                bool heavyFont = look.Font.NoFakeBold && look.Font.Body != null && name.font == look.Font.Body;
                Assert.That((name.fontStyle & FontStyles.Bold) != 0 || heavyFont, Is.True, $"{card.name}: the name is bold (thin strokes read grey).");
                Assert.That(UITheme.Contrast(name.color, theme.Cards.Card), Is.GreaterThanOrEqualTo(4.5f), $"{card.name}: the name reads on the card.");
                Rect c = ScreenRect((RectTransform)card.transform), n = TextRect(name);
                Assert.That(n.xMin - c.xMin, Is.GreaterThan(0f), $"{card.name}: the name has space from the card's edge.");
            }
            var chosen = ui.Cards[ui.SelectedIndex].GetComponents<Outline>().First(o => !(o is ThemeFocusRing));
            Assert.That(chosen.enabled, Is.True, "The chosen card has its frame.");
            Assert.That(chosen.effectColor, Is.EqualTo(theme.Cards.SelectedEdge), "The frame is the theme's colour.");
            Assert.That(chosen.effectDistance.x, Is.EqualTo(theme.Cards.SelectedEdgeWidth), "The frame is the theme's thickness.");
            int framed = ui.Cards.Count(c => c.GetComponents<Outline>().Any(o => !(o is ThemeFocusRing) && o.enabled));
            Assert.That(framed, Is.EqualTo(1), "Only the chosen card is framed.");
        }

        private static Rect TextRect(TMP_Text text)
        {
            text.ForceMeshUpdate();
            var b = text.textBounds;
            Vector3 min = text.rectTransform.TransformPoint(b.min), max = text.rectTransform.TransformPoint(b.max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Rect ScreenRect(RectTransform t)
        {
            var corners = new Vector3[4];
            t.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
