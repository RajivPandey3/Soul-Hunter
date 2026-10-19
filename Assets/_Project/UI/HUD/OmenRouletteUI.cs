using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// The Omens wheel (VS Random Events roulette; release item P3-05). vampire.survivors.wiki/w/Random_Events: "a three-ring
    /// roulette ... appear[s] at the bottom right corner ... at every XX:30 mark, where the three rings will start rotating and
    /// selecting a random event, which will then trigger at the next XX:00 mark"; v1.6.0: "Fixed issue with trisection event
    /// text overlapping gold fever" (the event is named on screen). Soul Hunter's own look, drawn only from the UI theme (no
    /// new art): a HUD plate with three rings of round pips (light, gold, light), a gold hub and pointer, and three lines of
    /// HUD text (Omen / the event / when). The spin is the one moving moment (frontend-design: one place for motion); with
    /// the Flashing VFX option off it does not spin (reduced motion) and shows the pick at once. It runs on game time, so a
    /// pause holds it. Bottom right, clear of the touch stick, Dash and Pause pads in landscape and portrait (HudClearance). The HUD adds it.
    /// </summary>
    public sealed class OmenRouletteUI : MonoBehaviour
    {
        /// <summary>How long the rings spin after a pick (game seconds). The wiki gives none; 3 s reads as a spin and ends
        /// well before XX:00 (balance method).</summary>
        public const float SpinSeconds = 3f;
        /// <summary>How long "Now" stays after an event fires.</summary>
        public const float FiredHoldSeconds = 4f;
        public static readonly Vector2 Size = new Vector2(400f, 150f);
        private const float WheelSize = 130f;
        private const float Margin = 24f;

        private RectTransform _root;
        private CanvasGroup _group;
        private readonly RectTransform[] _rings = new RectTransform[3];
        private readonly List<Image> _goldPips = new List<Image>();
        private TextMeshProUGUI _heading, _name, _when;
        private RandomEventDirector _director;
        private float _spinLeft, _firedLeft;
        private float _nameTick;
        private OmenEvent? _shown;
        private float _shownAt;
        private List<OmenEvent> _spinNames;

        public bool IsShowing => _root != null && _root.gameObject.activeSelf;
        public bool IsSpinning => _spinLeft > 0f;
        public string EventText => _name != null ? _name.text : "";
        public string WhenText => _when != null ? _when.text : "";
        public RectTransform Root => _root;
        /// <summary>The plate and the texts on it, for the readability tests.</summary>
        public Image Plate { get; private set; }
        // Place reads this every frame: one array, made once the texts exist (Bug MIS UI-07).
        private TextMeshProUGUI[] _texts;
        public IReadOnlyList<TextMeshProUGUI> Texts => _texts != null && _texts[0] == _heading ? _texts : (_texts = new[] { _heading, _name, _when });
        public float RingAngle(int ring) => _rings[ring] != null ? _rings[ring].localEulerAngles.z : 0f;

        public void Setup()
        {
            if (_root != null) return;
            Build();
            UiThemeAsset.Changed += RefreshGoldPips;
            Refresh();
        }

        private void OnDestroy()
        {
            UiThemeAsset.Changed -= RefreshGoldPips;
            Unhook();
        }

        private void Hook(RandomEventDirector director)
        {
            if (_director == director) return;
            Unhook();
            _director = director;
            if (_director == null) return;
            _director.OnPicked += HandlePicked;
            _director.OnFired += HandleFired;
        }

        private void Unhook()
        {
            if (_director == null) return;
            _director.OnPicked -= HandlePicked;
            _director.OnFired -= HandleFired;
            _director = null;
        }

        private void HandlePicked(OmenEvent e, float firesAt)
        {
            _shown = e;
            _shownAt = firesAt;
            _firedLeft = 0f;
            var progression = LevelProgressionManager.Instance;
            _spinNames = OmenTable.Pool(progression != null ? progression.CurrentStage : 1, true);
            // Reduced motion: with Flashing VFX off nothing spins (ui-ux-pro-max: respect reduced motion).
            _spinLeft = SoulHunter.Core.Persistence.GameSettings.FlashingVfx ? SpinSeconds : 0f;
            Refresh();
        }

        private void HandleFired(OmenEvent e)
        {
            _shown = e;
            _spinLeft = 0f;
            _firedLeft = FiredHoldSeconds;
            Refresh();
        }

        private void Update()
        {
            if (_root == null) return;
            if (_director == null) Hook(RandomEventDirector.Instance);
            bool on = RunModes.RandomEvents && _director != null;
            if (_root.gameObject.activeSelf != on) _root.gameObject.SetActive(on);
            if (!on) return;

            Place();

            float dt = Time.deltaTime; // game time: a pause holds the wheel
            if (_spinLeft > 0f)
            {
                float speed = 720f * Mathf.Pow(_spinLeft / SpinSeconds, 2f); // eases out to a stop
                for (int i = 0; i < _rings.Length; i++)
                    _rings[i].localEulerAngles = new Vector3(0f, 0f, _rings[i].localEulerAngles.z + (i % 2 == 0 ? -1f : 1.4f) * speed * dt);
                _spinLeft = Mathf.Max(0f, _spinLeft - dt);
                _nameTick -= dt;
                if (_nameTick <= 0f && _spinNames != null && _spinNames.Count > 0)
                {
                    _nameTick = 0.12f;
                    _name.text = OmenTable.Name(_spinNames[Random.Range(0, _spinNames.Count)]);
                }
                if (_spinLeft <= 0f) Refresh();
                return;
            }
            if (_firedLeft > 0f)
            {
                _firedLeft -= dt;
                if (_firedLeft <= 0f) { _shown = null; Refresh(); }
            }
            else if (!_shown.HasValue) Refresh(); // the idle line counts toward the next spin
        }

        /// <summary>
        /// Bottom right of the safe area, clear of the touch pads (owner, 2026-10-10: landscape and portrait). Before, it was
        /// lifted to 30% of the height, which on a phone held sideways (510 units) still covered the Dash button and in
        /// portrait ran into the stick. Now it takes the nearest free spot: above the pads (portrait) or beside them
        /// (landscape), in canvas units, so it keeps its size on every screen.
        /// </summary>
        public void Place()
        {
            if (_root == null) return;
            var space = _root.parent as RectTransform;
            if (space == null) return;
            foreach (var text in Texts) UiTextFloor.Apply(text); // on a touch screen no line is under 12 sp
            Rect safe = HudClearance.SafeRect(space, Margin);
            Vector2 size = new Vector2(Mathf.Min(Size.x, safe.width), Size.y);
            var wanted = new Rect(safe.xMax - size.x, safe.yMin, size.x, size.y);
            Rect box = HudClearance.Clear(wanted, safe, HudClearance.TouchPads(space), HudClearance.Gap);
            _root.anchorMin = _root.anchorMax = _root.pivot = new Vector2(1f, 0f);
            _root.sizeDelta = box.size;
            _root.anchoredPosition = new Vector2(box.xMax - space.rect.xMax, box.yMin - space.rect.yMin);
        }

        /// <summary>Writes the three lines for the wheel's state.</summary>
        private void Refresh()
        {
            if (_name == null) return;
            _heading.text = "Omen";
            if (_spinLeft > 0f) { _idleMark = int.MinValue; _when.text = "Choosing"; return; }
            if (_firedLeft > 0f && _shown.HasValue)
            {
                _idleMark = int.MinValue;
                _name.text = OmenTable.Name(_shown.Value);
                _when.text = "Now";
                return;
            }
            if (_shown.HasValue)
            {
                _idleMark = int.MinValue;
                _name.text = OmenTable.Name(_shown.Value);
                _when.text = "Comes at " + Clock(_shownAt);
                return;
            }
            _name.text = "The wheel waits";
            var progression = LevelProgressionManager.Instance;
            float now = progression != null ? progression.StageElapsedTime : 0f;
            // The next XX:30 mark.
            float next = (Mathf.Floor((now - OmenTable.MarkSeconds) / 60f) + 1f) * 60f + OmenTable.MarkSeconds;
            // Bug MIS UI-07: the idle line is refreshed every frame; its string is built only when the mark it names changes.
            int mark = Mathf.Max(0, Mathf.RoundToInt(next));
            if (mark == _idleMark) return;
            _idleMark = mark;
            _when.text = "Spins at " + Clock(next);
        }

        private int _idleMark = int.MinValue;

        /// <summary>m:ss of the stage clock.</summary>
        public static string Clock(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{total / 60}:{total % 60:00}";
        }

        private void Build()
        {
            var go = new GameObject("OmenRoulette", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _root = (RectTransform)go.transform;
            _root.SetParent(HudReadability.CanvasRoot(transform), false);
            _root.SetAsFirstSibling(); // under the menus
            _root.anchorMin = _root.anchorMax = new Vector2(1f, 0f);
            _root.pivot = new Vector2(1f, 0f);
            _root.sizeDelta = Size;
            _root.anchoredPosition = new Vector2(-Margin, Margin);
            _group = go.GetComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;
            Plate = go.GetComponent<Image>();
            UITheme.ApplySurface(Plate, ThemedElement.SurfaceRole.HudBacking);
            Plate.raycastTarget = false;

            // The wheel, on the left of the plate.
            var wheel = Rect("Wheel", _root, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(WheelSize, WheelSize));
            wheel.anchoredPosition = new Vector2(12f + WheelSize * 0.5f, 0f);
            float[] radii = { 58f, 41f, 24f };
            int[] counts = { 12, 9, 6 };
            float[] pip = { 13f, 12f, 11f };
            for (int r = 0; r < 3; r++)
            {
                _rings[r] = Rect("Ring" + r, wheel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
                for (int i = 0; i < counts[r]; i++)
                {
                    var p = Rect("Pip", _rings[r], new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(pip[r], pip[r]));
                    float a = Mathf.PI * 2f * i / counts[r];
                    p.anchoredPosition = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * radii[r];
                    Pip(p, r == 1);
                }
            }
            Pip(Rect("Hub", wheel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(18f, 18f)), true);
            // The pointer, at the top of the wheel: the pip under it is "the one".
            var pointer = Rect("Pointer", wheel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(10f, 10f));
            pointer.anchoredPosition = new Vector2(0f, 4f);
            Pip(pointer, true);

            // Three lines: what it is, the event, when.
            float left = 12f + WheelSize + 16f;
            _heading = Label("Heading", left, 0.70f, 0.94f, 22f);
            _name = Label("Event", left, 0.36f, 0.70f, 32f);
            _name.fontStyle = FontStyles.Bold;
            _when = Label("When", left, 0.08f, 0.34f, 22f);
            go.SetActive(false);
        }

        private TextMeshProUGUI Label(string name, float left, float yMin, float yMax, float size)
        {
            var rect = Rect(name, _root, new Vector2(0f, yMin), new Vector2(1f, yMax), Vector2.zero);
            rect.offsetMin = new Vector2(left, 0f);
            rect.offsetMax = new Vector2(-12f, 0f);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Left;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.fontSize = size; // a size first, so TextMeshPro's later Awake keeps these limits (CharacterSelectionUI.AutoSize)
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = size;
            text.raycastTarget = false;
            UITheme.ApplyText(text, ThemedElement.TextRole.HudLabel); // the HUD's light text with its outline
            return text;
        }

        /// <summary>A round pip: the theme's knob picture, light (Knob) or gold (Accent; the knob picture is set here).</summary>
        private void Pip(RectTransform rect, bool gold)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            if (gold)
            {
                UITheme.ApplySurface(image, ThemedElement.SurfaceRole.Accent);
                image.sprite = UITheme.Shapes.Knob;
                image.preserveAspect = true;
                _goldPips.Add(image);
            }
            else UITheme.ApplySurface(image, ThemedElement.SurfaceRole.Knob);
        }

        private void RefreshGoldPips()
        {
            foreach (var image in _goldPips) if (image != null) image.sprite = UITheme.Shapes.Knob;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }
    }
}
