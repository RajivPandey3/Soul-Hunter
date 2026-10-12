using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Core.Services;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Learning Comment:
    /// VS Rule: Player ka In-Game HUD (Heads-Up Display).
    /// Ye screen par top-left mein hathiyaron ki tasveerein (icons), Gold aur Kills dikhata hai.
    /// Sath hi Kael ki Health Bar ko update karta hai.
    /// </summary>
    public class GameHUDController : MonoBehaviour
    {
        [Header("Player Health")]
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private HealthController _playerHealth;

        [Header("Economy & Stats")]
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private TextMeshProUGUI _killText;

        [Header("Inventory (Top Left)")]
        [Tooltip("Empty = WeaponManager.Instance (HUD_Canvas is a prefab and cannot reference the scene's manager).")]
        [SerializeField] private WeaponManager _weaponManager;

        private SoulHunter.Gameplay.Core.RunStatsTracker _stats;

        private void Start()
        {
            // The HUD prefab's own scaler (1920x1080, match width) gives way to the game's one UI scale (UiScreen), so the
            // HUD fits landscape and portrait alike (owner, 2026-10-10). The prefab file is not edited.
            ResponsiveCanvas.Apply(GetComponentInParent<Canvas>());
            // VS pause menu (Resume / Options / Quit) on the GameSystemManager panel.
            PauseMenuUI.Attach(SoulHunter.Gameplay.Core.GameSystemManager.Instance);

            // Readable HUD labels (they were dark blue on the dark ground) and the stage progression label.
            var readability = GetComponent<HudReadability>();
            if (readability == null) readability = gameObject.AddComponent<HudReadability>();
            var timer = FindFirstObjectByType<TimerUI>();
            // Responsive (owner, 2026-10-10): the corner labels keep their corner on any canvas (they fell off a phone's
            // narrow canvas, hanging from the screen centre). The item slots follow the gold label (InventoryHud below).
            var safe = ResponsiveAnchors.SafeRoot(GetComponentInParent<Canvas>()?.rootCanvas, "HudSafeArea");
            if (_killText != null) ResponsiveAnchors.PinToNearestCorner(_killText.rectTransform, safe);
            if (_goldText != null) ResponsiveAnchors.PinToNearestCorner(_goldText.rectTransform, safe);
            if (timer != null && timer.TimeText != null) ResponsiveAnchors.PinToNearestCorner(timer.TimeText.rectTransform, safe);
            _timerRect = timer != null && timer.TimeText != null ? timer.TimeText.rectTransform : null;
            _safeRoot = safe;
            CaptureCornerLayout();
            LayoutCorners();
            UiScreen.Changed += LayoutCorners;
            readability.Setup(_goldText, _killText, timer != null ? timer.TimeText : null);
            if (GetComponent<TouchControlsUI>() == null) gameObject.AddComponent<TouchControlsUI>(); // touch stick (mobile / Visible Joystick)
            // The owner's story: each stage opens with its story line.
            var storyCard = GetComponent<StoryCardUI>();
            if (storyCard == null) storyCard = gameObject.AddComponent<StoryCardUI>();
            storyCard.Setup();
            // VS Randomizers (P3-05): the Omens wheel (bottom right, mode on only) and the Blind Fate pick over the hero.
            var omens = GetComponent<OmenRouletteUI>();
            if (omens == null) omens = gameObject.AddComponent<OmenRouletteUI>();
            omens.Setup();
            var blindFate = GetComponent<BlindFatePopup>();
            if (blindFate == null) blindFate = gameObject.AddComponent<BlindFatePopup>();
            blindFate.Setup();
            // VS Revival: a death with a Revival left shows the Revive button (P1-16).
            if (SoulHunter.Gameplay.Player.PlayerController.Instance != null && GetComponentInChildren<ReviveScreen>(true) == null)
                ReviveScreen.Attach(SoulHunter.Gameplay.Player.PlayerController.Instance, transform);

            // Health Bar setup
            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged += UpdateHealthBar;
                UpdateHealthBar(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
                // VS: a small HP bar under the hero instead of a strip across the bottom of the screen.
                HealthBarFollow.Attach(_healthSlider, _playerHealth.transform);

                // Owner (2026-09-30): no red full-screen tint at low health (the old LowHealthWarning was removed).
            }

            // VS inventory row: 6 weapon and 6 passive slots, top left.
            InventoryHud.Attach(GetComponentInParent<Canvas>(), _weaponManager != null ? _weaponManager : WeaponManager.Instance,
                _goldText != null ? _goldText.rectTransform : null);

            // VS HUD shows gold collected this run (the saved total lives in the menu shop).
            _stats = SoulHunter.Gameplay.Core.RunStatsTracker.Instance;
            if (_stats != null)
            {
                _stats.OnKillsChanged += UpdateKills;
                UpdateKills(_stats.TotalKills);
                _stats.OnGoldChanged += UpdateGoldUI;
                HudGoldHold.Changed += RefreshGold;
                UpdateGoldUI(_stats.TotalGoldCollected);
            }
            LayoutCorners(); // the item slots and the touch pads exist now (the phone's strip places them)
            StartCoroutine(LayoutOnceTheBarIsReady());
        }

        /// <summary>
        /// The XP bar makes its level label after this HUD starts; the phone's strip puts Pause under that label, so the
        /// strip is laid out again once it is there (on the phone Pause sat on "LV 4", 2026-10-12).
        /// </summary>
        private System.Collections.IEnumerator LayoutOnceTheBarIsReady()
        {
            for (int i = 0; i < 120; i++)
            {
                yield return null;
                var xp = BarWithLevelLabel();
                if (xp != null && !string.IsNullOrEmpty(xp.LevelLabel.text)) break;
            }
            yield return null;
            LayoutCorners();
        }

        private RectTransform _timerRect, _safeRoot;
        private Vector2 _killAt, _goldAt, _timerAt, _timerAnchor;
        private bool _cornersCaptured;
        private TMPro.TextAlignmentOptions _timerAlignment = TMPro.TextAlignmentOptions.Center;

        private void CaptureCornerLayout()
        {
            if (_cornersCaptured) return;
            _cornersCaptured = true;
            if (_killText != null) _killAt = _killText.rectTransform.anchoredPosition;
            if (_goldText != null) _goldAt = _goldText.rectTransform.anchoredPosition;
            if (_timerRect != null)
            {
                _timerAt = _timerRect.anchoredPosition;
                _timerAnchor = _timerRect.anchorMin;
                var t = _timerRect.GetComponent<TMPro.TMP_Text>();
                if (t != null) _timerAlignment = t.alignment;
            }
        }

        /// <summary>
        /// The corner labels on the canvas they are given (owner, 2026-10-10: landscape and portrait). On a 1080-unit-high
        /// canvas they keep the approved 1920x1080 places. On a shorter one (a phone held sideways) the stack from the top
        /// left (kills, gold, then the item slots) closes up, never tighter than a label and a gap. On a narrow one (a
        /// phone held upright) the timer moves down to the gold row, clear of the kills label.
        /// </summary>
        private void LayoutCorners()
        {
            if (this == null || _safeRoot == null) return;
            Canvas.ForceUpdateCanvases();
            Vector2 room = _safeRoot.rect.size;
            if (room.x <= 0f || room.y <= 0f) return;
            if (UiScreen.MinTouchUnits > 0f) { LayoutPhoneStrip(room); return; }
            LeavePhoneStrip();
            float gap = UITheme.Layout.ButtonGap;
            float squeeze = Mathf.Clamp(room.y / ResponsiveAnchors.Reference.y, 0.45f, 1f);
            RectTransform kill = _killText != null ? _killText.rectTransform : null, gold = _goldText != null ? _goldText.rectTransform : null;
            if (kill != null) kill.anchoredPosition = new Vector2(_killAt.x, _killAt.y * squeeze);
            if (gold != null)
            {
                float step = Mathf.Abs(_goldAt.y - _killAt.y) * squeeze;
                float minStep = (kill != null ? kill.rect.height : 50f) + gap;
                float killY = kill != null ? kill.anchoredPosition.y : _killAt.y;
                gold.anchoredPosition = new Vector2(_goldAt.x, killY - Mathf.Max(step, minStep));
            }
            if (_timerRect != null)
            {
                // Its approved place at the top right, unless that runs into the kills label (a phone held upright): then
                // the left column takes it too, under the item slots (kills, gold, slots, time).
                var timerText = _timerRect.GetComponent<TMPro.TMP_Text>();
                if (timerText != null) timerText.alignment = _timerAlignment;
                _timerRect.anchorMin = _timerRect.anchorMax = _timerAnchor;
                _timerRect.anchoredPosition = new Vector2(_timerAt.x, _timerAt.y * squeeze);
                Canvas.ForceUpdateCanvases();
                if (kill != null && WorldOverlap(kill, _timerRect))
                {
                    var slots = GetComponentInParent<Canvas>()?.rootCanvas.GetComponentInChildren<InventoryHud>();
                    float below = gold != null ? gold.anchoredPosition.y - gold.rect.height * 0.5f : kill.anchoredPosition.y - kill.rect.height;
                    if (slots != null)
                    {
                        var corners = new Vector3[4];
                        ((RectTransform)slots.transform).GetWorldCorners(corners);
                        Vector2 local = _safeRoot.InverseTransformPoint(corners[0]);
                        below = local.y - _safeRoot.rect.yMax; // from the top edge (negative)
                    }
                    _timerRect.anchorMin = _timerRect.anchorMax = new Vector2(0f, 1f);
                    float half = _timerRect.rect.height * 0.5f;
                    // Left-aligned with the kills label's left edge.
                    float left = kill.anchoredPosition.x - kill.rect.width * kill.pivot.x;
                    _timerRect.anchoredPosition = new Vector2(left + _timerRect.rect.width * _timerRect.pivot.x, below - gap - half);
                    if (timerText != null) timerText.alignment = TMPro.TextAlignmentOptions.Left;
                }
            }
        }

        /// <summary>
        /// The XP bar that shows the level. The scene has more than one XPBarUI (one sits on the managers object without
        /// a bar), so the first one found may have no label: on the phone the strip then ignored "LV 4" (2026-10-12).
        /// </summary>
        private static XPBarUI BarWithLevelLabel()
        {
            foreach (var bar in FindObjectsByType<XPBarUI>(FindObjectsSortMode.None))
                if (bar != null && bar.LevelLabel != null && bar.LevelLabel.isActiveAndEnabled) return bar;
            return null;
        }

        private struct LabelLook
        {
            public Vector2 AnchorMin, AnchorMax, Pivot, Size;
            public float FontSize, FontMin, FontMax;
            public bool AutoSize;
            public TMPro.TextAlignmentOptions Alignment;
        }

        private readonly System.Collections.Generic.Dictionary<TMPro.TMP_Text, LabelLook> _pcLooks = new System.Collections.Generic.Dictionary<TMPro.TMP_Text, LabelLook>();
        private bool _inStrip;

        /// <summary>
        /// On a phone, how far below the safe area's top the HUD strip and the stage name reach, in canvas units (0 on a
        /// PC). The unlock notice hangs under it: on the phone it was drawn over the stage name (2026-10-12).
        /// </summary>
        public static float PhoneClearTop { get; private set; }

        /// <summary>True while the phone's HUD strip is laid out (tests).</summary>
        public bool PhoneStrip => _inStrip;
        public TextMeshProUGUI KillText => _killText;
        public TextMeshProUGUI GoldText => _goldText;

        /// <summary>
        /// The phone's HUD (owner, 2026-10-12: "play ke dauran ... sab samne dikh raha hai"; chose the thin strip). The
        /// PC arrangement, at phone text sizes, put the stage name, kills, gold, twelve slots and the time down the top
        /// third of the screen. On a touch screen everything sits in one thin strip under the XP bar instead: the item
        /// slots at the left, the clock in the middle, kills and gold at the right with Pause, and the stage name shows
        /// for the first seconds of a stage. The wiki does not describe the VS HUD's layout; the sizes are Soul Hunter's
        /// (slots 20 dp, text 14 and 18 sp, Pause 48 dp). The same objects are moved; a PC keeps its approved places.
        /// </summary>
        private void LayoutPhoneStrip(Vector2 room)
        {
            float dp = UiScreen.MinTouchUnits / 48f, sp = UiScreen.MinTextUnits / 12f;
            float edge = 8f * dp, gap = 4f * dp, line = 20f * dp;
            var canvas = GetComponentInParent<Canvas>()?.rootCanvas;

            // Under the XP bar; the right-hand group also clears the level label that hangs under the bar's right end.
            float top = 16f * dp, rightTop = top;
            var xp = BarWithLevelLabel();
            var corners = new Vector3[4];
            if (xp != null && xp.LevelLabel != null)
            {
                var bar = (RectTransform)xp.LevelLabel.transform.parent;
                bar.GetWorldCorners(corners);
                top = Mathf.Max(0f, _safeRoot.rect.yMax - _safeRoot.InverseTransformPoint(corners[0]).y) + gap;
                // Pause clears the level label's drawn text (its box is much wider and taller than "LV 4").
                xp.LevelLabel.ForceMeshUpdate();
                Vector3 low = xp.LevelLabel.transform.TransformPoint(xp.LevelLabel.textBounds.min);
                rightTop = Mathf.Max(top, _safeRoot.rect.yMax - _safeRoot.InverseTransformPoint(low).y + gap);
            }
            float stackGap = gap + 20f; // the two backing bars (10 units of padding each) do not touch
            float labelsTop = top; // kills and gold sit left of Pause, on the clock's line

            // Left: the two rows of item slots.
            var slots = canvas != null ? canvas.GetComponentInChildren<InventoryHud>(true) : null;
            float slotScale = 20f * dp / InventoryHud.SlotSize;
            if (slots != null) slots.PinTopLeft(_safeRoot, new Vector2(edge, -top), slotScale);
            float stripBottom = top + 2f * (InventoryHud.SlotSize + InventoryHud.Spacing) * slotScale;

            // Right: Pause at the end, kills and gold beside it (side by side when the screen is wide, else stacked).
            float pause = UiScreen.MinTouchUnits;
            var touch = GetComponent<TouchControlsUI>();
            if (touch != null) touch.PlacePause(pause, edge, rightTop);
            float textRight = edge + pause + gap * 2f + 10f, columnWidth = 92f * dp; // 10: the backing bar's padding, so it stays clear of Pause
            bool sideBySide = room.x >= 560f * dp;
            StripLabel(_killText, new Vector2(1f, 1f), new Vector2(-(textRight + (sideBySide ? columnWidth + gap : 0f)), -labelsTop), new Vector2(columnWidth, line), 14f * sp, TMPro.TextAlignmentOptions.Right);
            StripLabel(_goldText, new Vector2(1f, 1f), new Vector2(-textRight, -(labelsTop + (sideBySide ? 0f : line + stackGap))), new Vector2(columnWidth, line), 14f * sp, TMPro.TextAlignmentOptions.Right);
            // What the stage name sits under: the slots, the clock and the labels. Pause is at the far right, clear of it.
            float middleBottom = Mathf.Max(stripBottom, labelsTop + (sideBySide ? line : line * 2f + stackGap) + 10f);
            stripBottom = Mathf.Max(middleBottom, rightTop + pause);

            // Middle: the clock alone.
            var timer = FindFirstObjectByType<TimerUI>();
            if (timer != null) timer.Short = true;
            StripLabel(_timerRect != null ? _timerRect.GetComponent<TMPro.TMP_Text>() : null, new Vector2(0.5f, 1f), new Vector2(0f, -top), new Vector2(72f * dp, 24f * dp), 18f * sp, TMPro.TextAlignmentOptions.Center);

            var readability = GetComponent<HudReadability>();
            if (readability != null) readability.PlaceForPhone(_safeRoot, middleBottom + gap * 2f, 22f * dp, 14f * sp);
            PhoneClearTop = Mathf.Max(stripBottom, middleBottom + gap * 2f + 22f * dp + 10f) + gap * 2f;
            if (!_inStrip) { _inStrip = true; RefreshLabels(); }
        }

        /// <summary>One strip label: its box hangs from <paramref name="anchor"/> on the safe area's top edge.</summary>
        private void StripLabel(TMPro.TMP_Text label, Vector2 anchor, Vector2 at, Vector2 size, float textSize, TMPro.TextAlignmentOptions alignment)
        {
            if (label == null) return;
            var rect = label.rectTransform;
            if (!_pcLooks.ContainsKey(label))
                _pcLooks[label] = new LabelLook { AnchorMin = rect.anchorMin, AnchorMax = rect.anchorMax, Pivot = rect.pivot, Size = rect.sizeDelta,
                    FontSize = label.fontSize, FontMin = label.fontSizeMin, FontMax = label.fontSizeMax, AutoSize = label.enableAutoSizing, Alignment = label.alignment };
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(anchor.x, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = at;
            label.enableAutoSizing = false;
            label.fontSize = textSize;
            label.alignment = alignment;
        }

        /// <summary>Back to the PC arrangement (a test that lays the same HUD out on a PC after a phone).</summary>
        private void LeavePhoneStrip()
        {
            PhoneClearTop = 0f;
            if (!_inStrip) return;
            _inStrip = false;
            foreach (var pair in _pcLooks)
            {
                if (pair.Key == null) continue;
                var rect = pair.Key.rectTransform;
                rect.anchorMin = pair.Value.AnchorMin;
                rect.anchorMax = pair.Value.AnchorMax;
                rect.pivot = pair.Value.Pivot;
                rect.sizeDelta = pair.Value.Size;
                pair.Key.fontSize = pair.Value.FontSize;
                pair.Key.fontSizeMin = pair.Value.FontMin;
                pair.Key.fontSizeMax = pair.Value.FontMax;
                pair.Key.enableAutoSizing = pair.Value.AutoSize;
                pair.Key.alignment = pair.Value.Alignment;
            }
            _pcLooks.Clear();
            var canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            var slots = canvas != null ? canvas.GetComponentInChildren<InventoryHud>(true) : null;
            if (slots != null) slots.PinTopLeft(null, Vector2.zero, 1f);
            var touch = GetComponent<TouchControlsUI>();
            if (touch != null) touch.PlacePause(0f, 0f, 0f);
            var timer = FindFirstObjectByType<TimerUI>();
            if (timer != null) timer.Short = false;
            var readability = GetComponent<HudReadability>();
            if (readability != null) readability.PlaceForPhone(null, -1f, 0f, 0f);
            RefreshLabels();
        }

        private int _kills;

        private void RefreshLabels()
        {
            UpdateKills(_kills);
            UpdateGoldUI(_runGold);
        }

        private static bool WorldOverlap(RectTransform a, RectTransform b)
        {
            var ca = new Vector3[4];
            var cb = new Vector3[4];
            a.GetWorldCorners(ca);
            b.GetWorldCorners(cb);
            return ca[2].x > cb[0].x && cb[2].x > ca[0].x && ca[2].y > cb[0].y && cb[2].y > ca[0].y;
        }

        private void OnDestroy()
        {
            UiScreen.Changed -= LayoutCorners;
            PhoneClearTop = 0f;
            if (_playerHealth != null) _playerHealth.OnHealthChanged -= UpdateHealthBar;

            if (_stats != null)
            {
                _stats.OnKillsChanged -= UpdateKills;
                _stats.OnGoldChanged -= UpdateGoldUI;
                HudGoldHold.Changed -= RefreshGold;
            }
        }

        private void UpdateHealthBar(int newHealth, int maxHealth)
        {
            if (_healthSlider != null && maxHealth > 0)
            {
                _healthSlider.value = (float)newHealth / maxHealth;
            }
        }

        private int _runGold;

        private void UpdateGoldUI(int gold)
        {
            _runGold = gold;
            if (_goldText != null)
            {
                // Gold still being counted up on the chest screen is held back (HudGoldHold).
                _goldText.text = "Gold: " + System.Math.Max(0, gold - HudGoldHold.Hidden).ToString();
            }
        }

        private void RefreshGold() => UpdateGoldUI(_runGold);

        private void UpdateKills(int kills)
        {
            _kills = kills;
            // The phone's strip has room for the short word only.
            if (_killText != null) _killText.text = (_inStrip ? "Kills: " : "Total Kills: ") + kills.ToString();
        }
    }
}
