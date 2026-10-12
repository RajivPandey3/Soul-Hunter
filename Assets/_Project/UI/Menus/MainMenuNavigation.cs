using UnityEngine;
using UnityEngine.UI;

namespace SoulHunter.UI
{
    public sealed class MainMenuNavigation : MonoBehaviour
    {
        public GameObject HomePanel;
        public GameObject CharacterPanel;
        public GameObject PowerUpPanel;
        public Button StartButton, PowerUpButton, CharacterBackButton, PowerUpBackButton, QuitButton;
        /// <summary>
        /// Owner report (2026-09-30): the character select screen fell apart outside 1920x1080. The main menu canvas used
        /// Constant Pixel Size, so fixed-pixel cards and fonts overflowed in a smaller window. Scale with the screen like
        /// the gameplay HUD (1920x1080 reference, match 0.5): 1080p is unchanged and every other size looks the same.
        /// </summary>
        public static void ScaleWithScreen(CanvasScaler scaler)
        {
            // Since 2026-10-10 the menu takes the game's one UI scale (UiScreen), landscape and portrait.
            if (scaler == null) return;
            SoulHunter.Gameplay.UI.ResponsiveCanvas.Apply(scaler.GetComponent<Canvas>());
        }

        private void Start()
        {
            ScaleWithScreen(GetComponentInParent<CanvasScaler>() ?? (HomePanel != null ? HomePanel.GetComponentInParent<CanvasScaler>() : null));
            StartButton.onClick.AddListener(ShowCharacters);
            PowerUpButton.onClick.AddListener(ShowPowerUps);
            CharacterBackButton.onClick.AddListener(ShowHome);
            PowerUpBackButton.onClick.AddListener(ShowHome);
            QuitButton.onClick.AddListener(Quit);
            foreach (var button in new[] { StartButton, PowerUpButton, CharacterBackButton, PowerUpBackButton, QuitButton })
                if (button != null) button.onClick.AddListener(PlayClick);
            OptionsButton = CreateOptionsButton();
            BuildSections();
            // Bug MIS B16 (2026-09-30): the focused Start button was pure blue, while every other screen focuses in orange.
            var menu = MenuButtons();
            foreach (var button in menu)
            {
                var image = button != null ? button.targetGraphic as Image : null;
                if (image != null) SoulHunter.Gameplay.UI.UITheme.ApplyButton(button, image, SoulHunter.Gameplay.UI.UITheme.ButtonKind.Normal);
            }
            // Owner (2026-10-03): title and buttons inside the frame art's open area, in the menu's own button look.
            Layout = MainMenuLayout.Attach(HomePanel, menu, MenuIcons(menu));
            // The Back buttons of the character select and Power Up screens in the same look (owner, 2026-10-04).
            foreach (var back in new[] { CharacterBackButton, PowerUpBackButton })
                if (back != null) SoulHunter.Gameplay.UI.UITheme.ApplyButton(back, back.targetGraphic as Image, SoulHunter.Gameplay.UI.UITheme.ButtonKind.Normal,
                    SoulHunter.Gameplay.UI.UITheme.IconSlot.Back);
            ShowHome();
        }

        public MainMenuLayout Layout { get; private set; }

        // ---------------------------------------------------------------- VS main menu sections (P3-02, 2026-10-09)
        // wiki Main_menu (raw, fetched 2026-10-09): Collection, Unlocks and Credits are "Available by default"; the Bestiary
        // "will only become visible by doing certain tasks" (Bestiary.UnlockLevel here). Labels are plain English (owner,
        // 2026-10-09). Not built, with why:
        // - PRESS TO START (wiki: "Once the player loads the game, a flashing PRESS TO START prompt will appear"): Soul
        //   Hunter boots straight to the menu; a title prompt changes the boot flow and waits for an owner decision.
        // - Quick Start ("a random stage with a randomly selected character", unlocked by the five main stages): Soul Hunter
        //   has one 10-level run and no stage select (owner, Run structure); needs an owner decision.
        // - Secrets (Forbidden Scrolls of Morbane), Adventures (Atlas Gate), Online, DLC Store, Poncle Account: no Soul
        //   Hunter counterpart (no relics, adventures, online play, DLC or accounts); the owner decides whether to leave them out.

        public Button CollectionButton { get; private set; }
        /// <summary>Null until the Bestiary is unlocked on this save (VS: the button appears only after its unlock).</summary>
        public Button BestiaryButton { get; private set; }
        public Button UnlocksButton { get; private set; }
        public Button CreditsButton { get; private set; }
        public CollectionMenuUI Collection { get; private set; }
        public BestiaryMenuUI BestiaryScreen { get; private set; }
        public UnlocksMenuUI Unlocks { get; private set; }
        public CreditsMenuUI Credits { get; private set; }

        private readonly System.Collections.Generic.List<GameObject> _panels = new System.Collections.Generic.List<GameObject>();
        private Selectable _returnFocus;

        /// <summary>The home buttons in order: read left to right, top to bottom (Start first, Quit last).</summary>
        private Button[] MenuButtons()
        {
            var list = new System.Collections.Generic.List<Button> { StartButton, PowerUpButton, CollectionButton };
            if (BestiaryButton != null) list.Add(BestiaryButton);
            list.Add(UnlocksButton);
            list.Add(CreditsButton);
            list.Add(OptionsButton);
            list.Add(QuitButton);
            return list.ToArray();
        }

        private SoulHunter.Gameplay.UI.UITheme.IconSlot[] MenuIcons(Button[] buttons)
        {
            var icons = new SoulHunter.Gameplay.UI.UITheme.IconSlot[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                var b = buttons[i];
                icons[i] = b == null ? SoulHunter.Gameplay.UI.UITheme.IconSlot.None
                    : b == StartButton ? SoulHunter.Gameplay.UI.UITheme.IconSlot.Start
                    : b == PowerUpButton ? SoulHunter.Gameplay.UI.UITheme.IconSlot.PowerUp
                    : b == CollectionButton ? SoulHunter.Gameplay.UI.UITheme.IconSlot.Collection
                    : b == BestiaryButton ? SoulHunter.Gameplay.UI.UITheme.IconSlot.Bestiary
                    : b == UnlocksButton ? SoulHunter.Gameplay.UI.UITheme.IconSlot.Unlocks
                    : b == CreditsButton ? SoulHunter.Gameplay.UI.UITheme.IconSlot.Credits
                    : b == OptionsButton ? SoulHunter.Gameplay.UI.UITheme.IconSlot.Options
                    : SoulHunter.Gameplay.UI.UITheme.IconSlot.Quit;
            }
            return icons;
        }

        private void BuildSections()
        {
            _panels.Clear();
            foreach (var panel in new[] { HomePanel, CharacterPanel, PowerUpPanel }) if (panel != null) _panels.Add(panel);
            var data = SaveData();
            var back = PowerUpBackButton != null ? PowerUpBackButton : CharacterBackButton;
            var sibling = PowerUpPanel != null ? PowerUpPanel : HomePanel;
            Credits = MenuSectionScreen.Create<CreditsMenuUI>(sibling, "Credits_Panel", "Credits", back, ShowHome);
            Unlocks = MenuSectionScreen.Create<UnlocksMenuUI>(sibling, "Unlocks_Panel", "Unlocks", back, ShowHome);
            BestiaryScreen = MenuSectionScreen.Create<BestiaryMenuUI>(sibling, "Bestiary_Panel", "Bestiary", back, ShowHome);
            Collection = MenuSectionScreen.Create<CollectionMenuUI>(sibling, "Collection_Panel", "Collection", back, ShowHome);
            foreach (var screen in new MenuSectionScreen[] { Collection, BestiaryScreen, Unlocks, Credits }) _panels.Add(screen.gameObject);

            CollectionButton = CreateMenuButton("Collection_Button", "Collection", ShowCollection);
            if (SoulHunter.Core.Persistence.Bestiary.IsUnlocked(data)) BestiaryButton = CreateMenuButton("Bestiary_Button", "Bestiary", ShowBestiary);
            UnlocksButton = CreateMenuButton("Unlocks_Button", "Unlocks", ShowUnlocks);
            CreditsButton = CreateMenuButton("Credits_Button", "Credits", ShowCredits);
        }

        private static SoulHunter.Core.Persistence.GameData SaveData()
        {
            SoulHunter.Core.Persistence.SaveService save = null;
            SoulHunter.Core.Services.GameServices.Instance?.TryGet(out save);
            return save != null ? save.CurrentData : null;
        }

        /// <summary>A home button copied from Power Up (its look and size), with its own label and action only.</summary>
        private Button CreateMenuButton(string name, string label, UnityEngine.Events.UnityAction action)
        {
            if (PowerUpButton == null) return null;
            var copy = Instantiate(PowerUpButton.gameObject, PowerUpButton.transform.parent);
            copy.name = name;
            var button = copy.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent(); // drop the copied Power Up listeners
            button.onClick.AddListener(PlayClick);
            button.onClick.AddListener(action);
            foreach (var tmp in copy.GetComponentsInChildren<TMPro.TMP_Text>(true)) tmp.text = label;
            foreach (var legacy in copy.GetComponentsInChildren<Text>(true)) legacy.text = label;
            return button;
        }

        /// <summary>
        /// The one panel switcher: shows <paramref name="panel"/> and hides every other menu panel, then focuses
        /// <paramref name="focus"/> (keyboard and controller). <paramref name="returnTo"/> is what Back focuses on the home
        /// screen afterwards: the button that opened the screen (ui-ux-pro-max, focus management).
        /// </summary>
        public void Show(GameObject panel, Selectable focus, Selectable returnTo = null)
        {
            foreach (var p in _panels) if (p != null && p != panel) p.SetActive(false);
            if (panel != null) panel.SetActive(true);
            if (returnTo != null) _returnFocus = returnTo;
            Focus(focus);
        }

        public void ShowCollection() => ShowSection(Collection, CollectionButton);
        public void ShowBestiary() => ShowSection(BestiaryScreen, BestiaryButton);
        public void ShowUnlocks() => ShowSection(Unlocks, UnlocksButton);
        public void ShowCredits() => ShowSection(Credits, CreditsButton);

        private void ShowSection(MenuSectionScreen screen, Button opener)
        {
            if (screen != null) Show(screen.gameObject, screen.BackButton, opener);
        }

        /// <summary>VS main menu "Options": a copy of the Power Up button placed below it; Quit moves down one step.</summary>
        public Button OptionsButton { get; private set; }

        private Button CreateOptionsButton()
        {
            if (PowerUpButton == null) return null;
            var copy = Instantiate(PowerUpButton.gameObject, PowerUpButton.transform.parent);
            copy.name = "Options_Button";
            var button = copy.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent(); // drop the copied Power Up listeners
            button.onClick.AddListener(PlayClick);
            button.onClick.AddListener(() => SoulHunter.Gameplay.UI.OptionsMenuUI.Open(() => Focus(OptionsButton)));
            foreach (var tmp in copy.GetComponentsInChildren<TMPro.TMP_Text>(true)) tmp.text = "Options";
            foreach (var legacy in copy.GetComponentsInChildren<Text>(true)) legacy.text = "Options";

            if (copy.transform.parent.GetComponent<LayoutGroup>() == null && StartButton != null)
            {
                var start = (RectTransform)StartButton.transform;
                var powerUp = (RectTransform)PowerUpButton.transform;
                var step = powerUp.anchoredPosition - start.anchoredPosition;
                ((RectTransform)copy.transform).anchoredPosition = powerUp.anchoredPosition + step;
                if (QuitButton != null && QuitButton.transform.parent == powerUp.parent)
                    ((RectTransform)QuitButton.transform).anchoredPosition = powerUp.anchoredPosition + step * 2f;
            }
            copy.transform.SetSiblingIndex(PowerUpButton.transform.GetSiblingIndex() + 1);
            return button;
        }

        private static void Focus(Selectable target)
        {
            if (target != null && UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(target.gameObject);
        }
        private static void PlayClick()
        {
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.UIClickSound);
        }

        // Every screen focuses a button, so the menus work with keyboard and controller alone.
        public void ShowHome()
        {
            var focus = _returnFocus != null ? _returnFocus : StartButton;
            _returnFocus = null;
            Show(HomePanel, focus);
        }
        public void ShowCharacters() => Show(CharacterPanel, null, StartButton); // the select screen focuses its own card
        public void ShowPowerUps() => Show(PowerUpPanel, PowerUpBackButton, PowerUpButton);
        private void Quit()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}
