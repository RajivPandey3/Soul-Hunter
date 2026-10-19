using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner (2026-10-04): the theme is the single source of truth and designers' edits show at once. In the real game, with
    /// the pause menu open, editing the active theme (as Theme Studio does) restyles its buttons, icon, veil and title, and
    /// the HUD's labels, without reopening anything.
    /// </summary>
    public class UiThemeLivePlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator EditingTheTheme_RestylesTheOpenPauseMenuAndTheHud_AtOnce()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var original = UiThemeAsset.Active;
            var look = Object.Instantiate(original); // edit a copy: the project's theme file stays untouched
            try
            {
                UiThemeAsset.SetActive(look);
                var system = GameSystemManager.Instance;
                system.TogglePause();
                yield return null;
                var pause = system.PauseMenuPanel.GetComponent<PauseMenuUI>();
                var resume = pause.ResumeButton;
                Assert.AreEqual(look.Buttons.Primary, resume.colors.normalColor, "Resume is the main action. " + Errors());
                Assert.AreEqual(look.Buttons.Danger, pause.QuitButton.colors.normalColor, "Quit is the leaving action.");
                var icon = resume.transform.Find("ThemeIcon");
                Assert.IsNotNull(icon, "Resume shows its icon.");
                Assert.AreSame(look.Icon.Resume, icon.GetComponent<Image>().sprite);
                var veil = system.PauseMenuPanel.transform.Find("ThemeVeil").GetComponent<Image>();
                var hudBacking = Object.FindFirstObjectByType<HudBacking>().GetComponent<Image>();

                look.Buttons.Primary = new Color(0.2f, 0.7f, 0.3f, 1f);
                look.Buttons.Focus = new Color(0.9f, 0.3f, 0.8f, 1f);
                look.Surfaces.Veil = new Color(0.3f, 0f, 0f, 0.5f);
                look.Hud.LabelBacking = new Color(0f, 0.2f, 0.4f, 0.7f);
                look.Icon.Resume = look.Icon.Quit;
                UiThemeAsset.NotifyChanged(); // what Theme Studio does after each edit
                yield return null;

                Assert.AreEqual(look.Buttons.Primary, resume.colors.normalColor, "The open menu's main button changed at once.");
                Assert.AreEqual(UITheme.SelectedColour(look.Buttons.Primary), resume.colors.selectedColor, "Its focus colour too.");
                Assert.AreSame(look.Icon.Quit, icon.GetComponent<Image>().sprite, "Its icon too.");
                Assert.AreEqual(look.Surfaces.Veil, veil.color, "The veil changed.");
                Assert.AreEqual(look.Hud.LabelBacking, hudBacking.color, "The HUD label backing changed.");

                system.Resume();
                yield return null;
                Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
            }
            finally
            {
                UiThemeAsset.SetActive(null); // back to GameContent's theme
                Object.Destroy(look);
            }
        }

        /// <summary>
        /// ui-ux-pro-max (2026-10-04): "animate 1-2 key elements per view", "visible focus on every control", "respect
        /// reduced motion". Only the chosen button breathes, sweeps and wears the focus ring; with the player's Flashing
        /// VFX option off nothing moves on its own, but the ring stays.
        /// </summary>
        [UnityTest]
        public IEnumerator OnlyTheChosenButtonMoves_WithAFocusRing_AndFlashingOffStopsIt()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var data = SoulHunter.Core.Persistence.GameSettings.Data;
            Assert.IsNotNull(data, "The run has a save.");
            data.FlashingVfx = true;
            var system = GameSystemManager.Instance;
            system.TogglePause();
            var pause = system.PauseMenuPanel.GetComponent<PauseMenuUI>();
            EventSystem.current.SetSelectedGameObject(pause.ResumeButton.gameObject);
            float wait = 0f, biggest = 1f;
            while (wait < 2.5f) { wait += Time.unscaledDeltaTime; biggest = Mathf.Max(biggest, pause.ResumeButton.transform.localScale.x); yield return null; }

            // The ring: an outline outside the button, or (Pro look, 2026-10-08) a frame inside its edge.
            System.Func<Selectable, bool> ringOn = sel =>
            {
                var outline = sel.targetGraphic.GetComponent<ThemeFocusRing>();
                var frame = sel.targetGraphic.transform.Find("ThemeFocusFrame");
                return (outline != null && outline.enabled) || (frame != null && frame.gameObject.activeSelf);
            };
            Assert.IsTrue(ringOn(pause.ResumeButton), "The chosen button wears the focus ring. " + Errors());
            var frameImage = pause.ResumeButton.targetGraphic.transform.Find("ThemeFocusFrame")?.GetComponent<Image>();
            var outlineRing = pause.ResumeButton.targetGraphic.GetComponent<ThemeFocusRing>();
            Assert.AreEqual(UiThemeAsset.Active.Buttons.Focus, frameImage != null && frameImage.gameObject.activeSelf ? frameImage.color : outlineRing.effectColor);
            if (frameImage != null && frameImage.gameObject.activeSelf)
                Assert.That(frameImage.rectTransform.rect.size, Is.EqualTo(((RectTransform)pause.ResumeButton.targetGraphic.transform).rect.size),
                    "The ring is inside the button: the chosen button keeps its size.");
            foreach (var other in new[] { pause.OptionsButton, pause.QuitButton })
            {
                Assert.IsFalse(ringOn(other), $"{other.name} is not chosen: no ring.");
                var otherShine = other.transform.Find("ThemeShineMask");
                Assert.IsTrue(otherShine == null || !otherShine.GetComponentInChildren<Image>(true).gameObject.activeInHierarchy, $"{other.name} does not sweep.");
                Assert.That(other.transform.localScale.x, Is.EqualTo(1f).Within(0.001f), $"{other.name} does not breathe.");
            }
            if (UiThemeAsset.Active.Motions.Breathe) Assert.That(biggest, Is.GreaterThan(1.005f), "The chosen button breathes.");

            data.FlashingVfx = false; // Options: Flashing VFX off
            wait = 0f;
            while (wait < 1f) { wait += Time.unscaledDeltaTime; yield return null; }
            float scale = 0f;
            for (int i = 0; i < 30; i++) { scale = Mathf.Max(scale, pause.ResumeButton.transform.localScale.x); yield return null; }
            Assert.That(scale, Is.EqualTo(1f).Within(0.002f), "With Flashing VFX off the chosen button stops breathing.");
            var shine = pause.ResumeButton.transform.Find("ThemeShineMask/ThemeShine");
            Assert.IsTrue(shine == null || !shine.gameObject.activeSelf, "and stops sweeping.");
            Assert.IsTrue(ringOn(pause.ResumeButton), "The focus ring stays: it is information, not decoration.");

            data.FlashingVfx = true;
            system.Resume();
            yield return null;
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// Bug MIS UI-10: the Results screen and the pause stats wrote the theme's gold and dim into their text as rich-text
        /// colours (the stats used a fixed gold of their own), so a live theme change left them in the old colours.
        /// </summary>
        [UnityTest]
        public IEnumerator EditingTheTheme_RecoloursTheRichTextOfResultsAndStats()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var weapon in SoulHunter.Gameplay.Player.PlayerController.Instance.GetComponentsInChildren<SoulHunter.Gameplay.Combat.AutoAttackWeapon>(true)) weapon.enabled = false;
            yield return CloseOpenLevelUps();
            var original = UiThemeAsset.Active;
            var look = Object.Instantiate(original);
            var owner = new GameObject("ResultsThemeOwner");
            try
            {
                UiThemeAsset.SetActive(look);
                // Hollow Heart changes Max Health, so the stats show one value in the highlight.
                SoulHunter.Gameplay.Combat.WeaponManager.Instance.GiveWeapon(SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.MaxHealth, 1);
                var system = GameSystemManager.Instance;
                system.TogglePause();
                yield return null;
                var stats = system.PauseMenuPanel.GetComponentInChildren<StatsPanel>(true);
                Assert.IsNotNull(stats, "The pause menu has a stats panel. " + Errors());
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.That(StatsText(stats), Does.Contain("<color=#" + Hex(look.Text.Accent)), "Changed stats use the theme's accent.");

                var screen = RunResultsScreen.Show(owner.transform, RunResults.Capture(RunResults.EndKind.Quit), null);
                yield return null;
                Assert.That(screen.SummaryText.text, Does.Contain("#" + Hex(look.Text.Accent)));
                Assert.That(screen.ListsText.text, Does.Contain("#" + Hex(look.Text.DimOnDark)));

                string oldAccent = Hex(look.Text.Accent);
                look.Text.Accent = new Color(0.2f, 0.9f, 0.4f, 1f);
                look.Text.DimOnDark = new Color(0.5f, 0.6f, 0.9f, 1f);
                UiThemeAsset.NotifyChanged(); // what Theme Studio does after each edit
                yield return null;
                Assert.That(screen.SummaryText.text, Does.Contain("#" + Hex(look.Text.Accent)), "The Results gold follows the theme at once.");
                Assert.That(screen.SummaryText.text, Does.Not.Contain("#" + oldAccent));
                Assert.That(screen.ListsText.text, Does.Contain("#" + Hex(look.Text.DimOnDark)), "The Results headers follow the theme.");
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.That(StatsText(stats), Does.Contain("<color=#" + Hex(look.Text.Accent)), "The stats follow the theme.");

                Object.Destroy(screen.gameObject);
                system.Resume();
                yield return null;
                Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
            }
            finally
            {
                Object.Destroy(owner);
                UiThemeAsset.SetActive(null);
                Object.Destroy(look);
            }
        }

        /// <summary>Bug MIS UI-10 on the main menu: the Power Up cells and the character stats follow a live theme change.</summary>
        [UnityTest]
        public IEnumerator EditingTheTheme_RecoloursThePowerUpCellsAndTheCharacterStats()
        {
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitUntilRealtime(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            var save = SoulHunter.Core.Services.GameServices.Instance.Get<SoulHunter.Core.Persistence.SaveService>();
            save.UseFile(System.IO.Path.Combine(Application.temporaryCachePath, "playmode_test_save.json"));
            save.CurrentData.PowerUpRanks.Clear();
            save.CurrentData.DisabledPowerUps.Clear();
            SoulHunter.Core.Persistence.PowerUps.SetRank(save.CurrentData, SoulHunter.Core.Persistence.PowerUpId.Might, 1);
            var original = UiThemeAsset.Active;
            var look = Object.Instantiate(original);
            try
            {
                UiThemeAsset.SetActive(look);
                var navigation = Object.FindFirstObjectByType<SoulHunter.UI.MainMenuNavigation>();
                navigation.ShowPowerUps();
                for (int i = 0; i < 3; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
                var shop = Object.FindFirstObjectByType<SoulHunter.UI.PowerUpMenuUI>();
                var cell = shop.CellFor(SoulHunter.Core.Persistence.PowerUpId.Might).GetComponentInChildren<TMP_Text>();
                Assert.That(cell.text, Does.Contain("#" + Hex(look.Text.SoftOnLight)), "A part-bought cell shows its rank in the theme's soft colour.");
                look.Text.SoftOnLight = new Color(0.1f, 0.3f, 0.6f, 1f);
                UiThemeAsset.NotifyChanged();
                yield return null;
                Assert.That(cell.text, Does.Contain("#" + Hex(look.Text.SoftOnLight)), "The cell follows the theme at once.");

                navigation.ShowCharacters();
                for (int i = 0; i < 3; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
                var select = Object.FindFirstObjectByType<SoulHunter.UI.CharacterSelectionUI>();
                Assert.That(select.StatsText.text, Does.Contain("#" + Hex(look.Text.Accent)), "PowerUp values show in the theme's accent.");
                look.Text.Accent = new Color(0.2f, 0.9f, 0.4f, 1f);
                UiThemeAsset.NotifyChanged();
                yield return null;
                Assert.That(select.StatsText.text, Does.Contain("#" + Hex(look.Text.Accent)), "The stats follow the theme at once.");
                Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
            }
            finally
            {
                UiThemeAsset.SetActive(null);
                Object.Destroy(look);
            }
        }

        private static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        private static string StatsText(StatsPanel panel)
        {
            var all = new System.Text.StringBuilder();
            foreach (var text in panel.GetComponentsInChildren<TMP_Text>()) all.Append(text.text);
            return all.ToString();
        }
    }
}
