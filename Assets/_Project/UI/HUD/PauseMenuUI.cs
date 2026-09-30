using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SoulHunter.Gameplay.Core;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// VS-style pause menu: Resume, Options and Quit (to the main menu), usable with keyboard or
    /// controller (Resume is focused when the menu opens). Replaces the old panel's dead Restart button and
    /// its volume slider (volume now lives in Options). The HUD attaches it to GameSystemManager's panel.
    /// </summary>
    public sealed class PauseMenuUI : MonoBehaviour
    {
        private GameSystemManager _system;
        private Button _resume;
        private Button _options;
        private Button _quit;

        public Button ResumeButton => _resume;
        public Button OptionsButton => _options;
        public Button QuitButton => _quit;

        public static PauseMenuUI Attach(GameSystemManager system)
        {
            if (system == null || system.PauseMenuPanel == null) return null;
            var ui = system.PauseMenuPanel.GetComponent<PauseMenuUI>();
            if (ui != null) return ui;
            ui = system.PauseMenuPanel.AddComponent<PauseMenuUI>();
            ui._system = system;
            ui.Build();
            system.OnPauseChanged += ui.HandlePauseChanged;
            return ui;
        }

        private void OnDestroy()
        {
            if (_system != null) _system.OnPauseChanged -= HandlePauseChanged;
        }

        private void Build()
        {
            if (_system.VolumeSlider != null) _system.VolumeSlider.gameObject.SetActive(false);
            foreach (var child in GetComponentsInChildren<Button>(true))
                if (child.name.Contains("Restart")) child.gameObject.SetActive(false);

            _resume = CreateButton("Resume_Button", new Vector2(0.38f, 0.52f), new Vector2(0.62f, 0.6f), "Resume", () => _system.Resume());
            _options = CreateButton("Options_Button", new Vector2(0.38f, 0.42f), new Vector2(0.62f, 0.5f), "Options",
                () => OptionsMenuUI.Open(() => Select(_options)));
            _quit = CreateButton("Quit_Button", new Vector2(0.38f, 0.32f), new Vector2(0.62f, 0.4f), "Quit", () => _system.QuitToMainMenu());
            // VS: the player's stats are shown while paused (wiki Player_stats). A column on the right of the buttons, under
            // the timer: the HUD's kills, gold and item rows sit top left and draw over anything placed there.
            StatsPanel.Attach(transform, new Vector2(0.7f, 0.1f), new Vector2(0.96f, 0.84f), 1, 30f);

            // The old "GAME PAUSED" title sat in the middle, behind the buttons: move it above them.
            foreach (var title in GetComponentsInChildren<TMP_Text>(true))
                if (title.text.ToUpperInvariant().Contains("PAUSED"))
                {
                    var rect = title.rectTransform;
                    rect.anchorMin = new Vector2(0.3f, 0.64f);
                    rect.anchorMax = new Vector2(0.7f, 0.74f);
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    title.alignment = TextAlignmentOptions.Center;
                    title.enableAutoSizing = true;
                    title.fontSizeMax = 60f;
                }
        }

        private void HandlePauseChanged(bool paused)
        {
            if (paused) Select(_resume);
        }

        private static void Select(Selectable target)
        {
            if (target != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target.gameObject);
        }

        private Button CreateButton(string name, Vector2 min, Vector2 max, string label, System.Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            OptionsMenuUI.ApplyFocusColors(button, image);
            button.onClick.AddListener(() =>
            {
                var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
                if (audio != null) audio.PlaySFX(audio.UIConfirmSound);
                onClick();
            });
            var textGo = new GameObject("Text", typeof(RectTransform));
            var textRect = (RectTransform)textGo.transform;
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16f;
            text.fontSizeMax = 40f;
            text.raycastTarget = false;
            return button;
        }
    }
}
