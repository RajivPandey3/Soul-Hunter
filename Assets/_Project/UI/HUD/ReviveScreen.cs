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
        private RectTransform _root, _safe, _panel;
        private Vector2 _lastSafe = new Vector2(-1f, -1f);
        private readonly System.Collections.Generic.Dictionary<TMP_Text, float> _sizes = new System.Collections.Generic.Dictionary<TMP_Text, float>();
        private bool _paused;

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
            // Its own overlay canvas, like the chest screen (ChestUI.ScreenCanvas), on the game's one UI scale.
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = ScreenSortingOrder;
            SoulHunter.Gameplay.UI.ResponsiveCanvas.Apply(_canvas); // the game's one UI scale (UiScreen), landscape and portrait
            gameObject.AddComponent<GraphicRaycaster>();

            var rootGo = new GameObject("Root", typeof(RectTransform));
            _root = (RectTransform)rootGo.transform;
            _root.SetParent(transform, false);
            _root.anchorMin = Vector2.zero; _root.anchorMax = Vector2.one;
            _root.offsetMin = _root.offsetMax = Vector2.zero;

            // The theme's veil over the paused game (it also takes every click meant for the world).
            UITheme.AddVeil(_root, "Veil");

            // Responsive (owner, 2026-10-10): the panel sits in the middle of the safe area (notch, gesture bar) and is
            // never wider than it; the veil stays full screen. The button keeps the theme's standard size, which on a
            // touch screen is never under the 48 dp touch target (ThemedElement.StandardSize).
            var safeGo = new GameObject("Safe", typeof(RectTransform));
            _safe = (RectTransform)safeGo.transform;
            _safe.SetParent(_root, false);
            SafeAreaFitter.Fit(_safe);

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            _panel = (RectTransform)panelGo.transform;
            _panel.SetParent(_safe, false);
            _panel.anchorMin = _panel.anchorMax = _panel.pivot = new Vector2(0.5f, 0.5f);
            _panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            UITheme.ApplyPanel(panelGo.GetComponent<Image>());

            // One thing to do on this screen, so one strong element: the Revive button. The title and the count stay quiet.
            TitleText = Label(_panel, "Title", UITheme.Layout.TitleTextMax, ThemedElement.TextRole.Title);
            TitleText.text = "You have fallen";
            RevivalsText = Label(_panel, "Revivals", 34f, ThemedElement.TextRole.Body);
            HintText = Label(_panel, "Hint", 26f, ThemedElement.TextRole.Soft);
            HintText.text = "Rise again with half your health";

            var buttonGo = new GameObject("ReviveButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var buttonRect = (RectTransform)buttonGo.transform;
            buttonRect.SetParent(_panel, false);
            buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(UITheme.Layout.ButtonWidth, UITheme.Layout.ButtonHeight);
            ReviveButton = buttonGo.GetComponent<Button>();
            ReviveButton.onClick.AddListener(PressRevive);
            var label = Label(buttonRect, "Label", UITheme.Layout.ButtonTextMax, ThemedElement.TextRole.Body);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.text = "Revive";
            // After the label exists, so the theme lays it out too; Primary kind, the theme's standard button size.
            UITheme.ApplyButton(ReviveButton, buttonGo.GetComponent<Image>(), UITheme.ButtonKind.Primary, UITheme.IconSlot.Resume);
            Layout();

            _root.gameObject.SetActive(false);
        }

        private TextMeshProUGUI Label(RectTransform parent, string name, float fontSize, ThemedElement.TextRole role)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
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
            if (parent == _panel) _sizes[label] = fontSize; // the touch floor and the panel width are applied in Layout
            return label;
        }

        // ---------- layout (every screen change; nothing is rebuilt) ----------

        private void OnEnable() => UiScreen.Changed += Layout;
        private void OnDisable() => UiScreen.Changed -= Layout;

        private void LateUpdate()
        {
            if (_safe != null && _safe.rect.size != _lastSafe) Layout();
        }

        /// <summary>
        /// Centres the panel in the safe area, at most <see cref="PanelWidth"/> wide (narrower on a portrait phone), stacks
        /// the title, the count, the hint and the Revive button in it, and keeps every text at or over the touch floor.
        /// </summary>
        public void Layout()
        {
            if (this == null || _panel == null || ReviveButton == null) return;
            _lastSafe = _safe.rect.size;
            float gap = UITheme.Layout.ButtonGap;
            float floor = Mathf.Max(12f, UiScreen.MinTextUnits);
            var button = (RectTransform)ReviveButton.transform;
            var element = ReviveButton.GetComponent<ThemedElement>();
            Vector2 buttonSize = element != null && element.StandardSize(UiThemeAsset.Active) is Vector2 standard ? standard : button.rect.size;

            float margin = gap * 2f;
            float width = Mathf.Min(PanelWidth, Mathf.Max(buttonSize.x + margin * 2f, _lastSafe.x - margin * 2f));
            float inner = width - margin * 2f;
            float title = Mathf.Max(96f, floor * 1.6f), revivals = Mathf.Max(48f, floor * 1.5f), hint = Mathf.Max(40f, floor * 1.5f);
            const float pad = 28f;
            float height = pad + title + revivals + hint + gap * 2f + buttonSize.y + pad;
            _panel.sizeDelta = new Vector2(width, height);
            _panel.anchoredPosition = Vector2.zero;

            float y = height * 0.5f - pad;
            Stack(TitleText, ref y, title, inner);
            Stack(RevivalsText, ref y, revivals, inner);
            Stack(HintText, ref y, hint, inner);
            // The button keeps the centre placed here; the theme gives it its standard size.
            button.anchorMin = button.anchorMax = button.pivot = new Vector2(0.5f, 0.5f);
            button.anchoredPosition = new Vector2(0f, -height * 0.5f + pad + buttonSize.y * 0.5f);
            button.sizeDelta = buttonSize;

            foreach (var pair in _sizes)
            {
                if (pair.Key == null) continue;
                pair.Key.fontSizeMax = Mathf.Max(pair.Value, floor);
                pair.Key.fontSizeMin = floor;
            }
        }

        private static void Stack(TMP_Text text, ref float y, float height, float width)
        {
            var rect = text.rectTransform;
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0f, y - height * 0.5f);
            y -= height;
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
            SoulHunter.Core.Input.PauseKeyHolds.Hold(this, PauseAction()); // counted: overlapping screens share the key
        }

        private void RestorePauseAction()
        {
            SoulHunter.Core.Input.PauseKeyHolds.Release(this, PauseAction());
        }

        private static UnityEngine.InputSystem.InputAction PauseAction()
        {
            if (SoulHunter.Core.Services.GameServices.Instance == null) return null;
            SoulHunter.Core.Services.GameServices.Instance.TryGet(out SoulHunter.Core.Input.InputService input);
            return input?.Actions?.FindAction(SoulHunter.Core.Input.InputService.PauseAction);
        }
    }
}
