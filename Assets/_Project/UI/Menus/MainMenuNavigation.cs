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
            if (scaler == null) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
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
            // Bug MIS B16 (2026-09-30): the focused Start button was pure blue, while every other screen focuses in orange.
            foreach (var button in new[] { StartButton, PowerUpButton, OptionsButton, QuitButton })
            {
                var image = button != null ? button.targetGraphic as Image : null;
                if (image != null) SoulHunter.Gameplay.UI.OptionsMenuUI.ApplyFocusColors(button, image);
            }
            // Owner (2026-10-03): title and buttons inside the frame art's open area, in the menu's own button look.
            Layout = MainMenuLayout.Attach(HomePanel, new[] { StartButton, PowerUpButton, OptionsButton, QuitButton });
            // The Back buttons of the character select and Power Up screens in the same look (owner, 2026-10-04).
            foreach (var back in new[] { CharacterBackButton, PowerUpBackButton })
                if (back != null) SoulHunter.Gameplay.UI.UITheme.ApplyButton(back, back.targetGraphic as Image, labels: true);
            ShowHome();
        }

        public MainMenuLayout Layout { get; private set; }

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
        public void ShowHome() { HomePanel.SetActive(true); CharacterPanel.SetActive(false); PowerUpPanel.SetActive(false); Focus(StartButton); }
        public void ShowCharacters() { HomePanel.SetActive(false); CharacterPanel.SetActive(true); PowerUpPanel.SetActive(false); }
        public void ShowPowerUps() { HomePanel.SetActive(false); CharacterPanel.SetActive(false); PowerUpPanel.SetActive(true); Focus(PowerUpBackButton); }
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
