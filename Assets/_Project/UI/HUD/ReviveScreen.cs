using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// VS Revival (vampire.survivors.wiki/w/Revival, fetched 2026-10-09): "Each point of Revival a player has allows them to
    /// revive once upon death with 50% of their Max Health" and, on death, "they will instead be greeted with a 'Revive'
    /// button". "An animation is played and the player resumes control of their character exactly where they left off".
    ///
    /// A lethal hit with a Revival left holds the player at 0 health (PlayerController, HealthController.HoldForRevive): the
    /// game pauses through GameTime and this screen shows one themed Primary "Revive" button (the theme's standard size)
    /// with the Revivals left. Pressing it spends exactly one Revival (PlayerController.ConfirmRevive) and resumes. The
    /// wiki is silent on whether the game is paused under the button; Soul Hunter pauses (open owner question, P1-16).
    /// Layout and wording are Soul Hunter's own, in the active UI theme; no colour is chosen here.
    /// </summary>
    public sealed class ReviveScreen : MonoBehaviour
    {
        /// <summary>Above the HUD (and the chest screen, 400), below the Results screen (450) and unlock notices (500).</summary>
        public const int ScreenSortingOrder = 420;
        private const float PanelWidth = 760f, PanelHeight = 380f;

        public static ReviveScreen Current { get; private set; }

        private PlayerController _player;
        private Canvas _canvas;
        private RectTransform _root;
        private bool _paused, _pauseActionWasEnabled;

        public bool IsOpen => _root != null && _root.gameObject.activeSelf;
        public Button ReviveButton { get; private set; }
        public TextMeshProUGUI TitleText { get; private set; }
        public TextMeshProUGUI RevivalsText { get; private set; }
        public TextMeshProUGUI HintText { get; private set; }
        public Canvas Canvas => _canvas;
        /// <summary>How many times this screen was opened in the run (tests, bot).</summary>
        public int TimesShown { get; private set; }

        /// <summary>Creates the screen under <paramref name="owner"/> (the HUD) and registers it as the player's Revive screen.</summary>
        public static ReviveScreen Attach(PlayerController player, Transform owner)
        {
            if (player == null) return null;
            var go = new GameObject("ReviveScreen", typeof(RectTransform));
            // A root object in the HUD's scene, never a child of the HUD: a canvas nested in another canvas keeps its own
            // (default 100x100) rect and ignores its scaler, so the veil was a small square hidden behind the panel.
            if (owner != null) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, owner.gameObject.scene);
            var screen = go.AddComponent<ReviveScreen>();
            screen._player = player;
            screen.Build();
            player.ShowReviveScreen = screen.Open;
            Current = screen;
            return screen;
        }

        // ---------- build ----------

        private void Build()
        {
            // Its own overlay canvas, like the chest screen (ChestUI.ScreenCanvas): Expand keeps the whole 1920x1080 layout
            // inside any window, so the button can never fall off a wide, short screen.
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = ScreenSortingOrder;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            gameObject.AddComponent<GraphicRaycaster>();

            var rootGo = new GameObject("Root", typeof(RectTransform));
            _root = (RectTransform)rootGo.transform;
            _root.SetParent(transform, false);
            _root.anchorMin = Vector2.zero; _root.anchorMax = Vector2.one;
            _root.offsetMin = _root.offsetMax = Vector2.zero;

            // The theme's veil over the paused game (it also takes every click meant for the world).
            UITheme.AddVeil(_root, "Veil");

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            var panel = (RectTransform)panelGo.transform;
            panel.SetParent(_root, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            UITheme.ApplyPanel(panelGo.GetComponent<Image>());

            // One thing to do on this screen, so one strong element: the Revive button. The title and the count stay quiet.
            TitleText = Label(panel, "Title", new Vector2(0f, 118f), new Vector2(PanelWidth - 80f, 96f), UITheme.Layout.TitleTextMax, ThemedElement.TextRole.Title);
            TitleText.text = "You have fallen";
            RevivalsText = Label(panel, "Revivals", new Vector2(0f, 36f), new Vector2(PanelWidth - 80f, 48f), 34f, ThemedElement.TextRole.Body);
            HintText = Label(panel, "Hint", new Vector2(0f, -14f), new Vector2(PanelWidth - 80f, 40f), 26f, ThemedElement.TextRole.Soft);
            HintText.text = "Rise again with half your health";

            var buttonGo = new GameObject("ReviveButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var buttonRect = (RectTransform)buttonGo.transform;
            buttonRect.SetParent(panel, false);
            buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(0f, -110f);
            buttonRect.sizeDelta = new Vector2(UITheme.Layout.ButtonWidth, UITheme.Layout.ButtonHeight);
            ReviveButton = buttonGo.GetComponent<Button>();
            ReviveButton.onClick.AddListener(PressRevive);
            var label = Label(buttonRect, "Label", Vector2.zero, buttonRect.sizeDelta, UITheme.Layout.ButtonTextMax, ThemedElement.TextRole.Body);
            label.text = "Revive";
            // After the label exists, so the theme lays it out too; Primary kind, the theme's standard button size.
            UITheme.ApplyButton(ReviveButton, buttonGo.GetComponent<Image>(), UITheme.ButtonKind.Primary, UITheme.IconSlot.Resume);

            _root.gameObject.SetActive(false);
        }

        private static TextMeshProUGUI Label(RectTransform parent, string name, Vector2 position, Vector2 size, float fontSize, ThemedElement.TextRole role)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var label = go.AddComponent<TextMeshProUGUI>();
            // A size before auto-size, so TextMeshPro's own defaults never replace the limits (character select, 2026-09-30).
            label.fontSize = fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            UITheme.ApplyText(label, role);
            ThemedElement.Outline(label, UiThemeAsset.Active);
            return label;
        }

        // ---------- flow ----------

        /// <summary>The player's Revive screen hook: pauses the game and shows the button. False when it cannot show.</summary>
        public bool Open(PlayerController player)
        {
            if (this == null || _root == null || player == null) return false;
            _player = player;
            var stats = player.Stats;
            int left = stats != null ? stats.Revivals : 0;
            RevivalsText.text = left == 1 ? "1 Revival left" : left + " Revivals left";
            _root.gameObject.SetActive(true);
            TimesShown++;
            if (!_paused) { SoulHunter.Core.Services.GameTime.Pause(this); _paused = true; }
            DisablePauseAction(); // Esc / Start must not open the pause menu over a death
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events != null) events.SetSelectedGameObject(ReviveButton.gameObject); // keyboard and controller focus
            return true;
        }

        /// <summary>The Revive button: spends one Revival and revives (PlayerController.ConfirmRevive), then resumes.</summary>
        public void PressRevive()
        {
            if (!IsOpen) return;
            Close(); // first: the game resumes before the revival's immunity starts counting on game time
            if (_player != null) _player.ConfirmRevive();
        }

        private void Close()
        {
            if (_root != null) _root.gameObject.SetActive(false);
            RestorePauseAction();
            if (_paused) { SoulHunter.Core.Services.GameTime.Resume(this); _paused = false; }
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events != null && ReviveButton != null && events.currentSelectedGameObject == ReviveButton.gameObject) events.SetSelectedGameObject(null);
        }

        // Safety net: a screen whose player is gone (scene teardown) or no longer waiting must not leave the game paused.
        private void Update()
        {
            if (IsOpen && (_player == null || !_player.IsAwaitingRevive)) Close();
        }

        private void OnDestroy()
        {
            RestorePauseAction();
            if (_paused) SoulHunter.Core.Services.GameTime.Resume(this);
            _paused = false;
            if (_player != null && _player.ShowReviveScreen == (System.Func<PlayerController, bool>)Open) _player.ShowReviveScreen = null;
            if (Current == this) Current = null;
        }

        // The same switch as the chest screen and Options (ChestUI.DisablePauseAction).
        private void DisablePauseAction()
        {
            var pause = PauseAction();
            if (pause != null && pause.enabled) { _pauseActionWasEnabled = true; pause.Disable(); }
        }

        private void RestorePauseAction()
        {
            if (!_pauseActionWasEnabled) return;
            _pauseActionWasEnabled = false;
            PauseAction()?.Enable();
        }

        private static UnityEngine.InputSystem.InputAction PauseAction()
        {
            if (SoulHunter.Core.Services.GameServices.Instance == null) return null;
            SoulHunter.Core.Services.GameServices.Instance.TryGet(out SoulHunter.Core.Input.InputService input);
            return input?.Actions?.FindAction(SoulHunter.Core.Input.InputService.PauseAction);
        }
    }
}
