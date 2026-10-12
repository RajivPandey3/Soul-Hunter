using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Marks a UI element with what it is (a button, a panel, a title...) and styles it from the active theme. It re-applies
    /// whenever the theme changes (<see cref="UiThemeAsset.Changed"/>), so designers see their edits at once. Added by
    /// <see cref="UITheme"/>; UI scripts never set these colours themselves.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThemedElement : MonoBehaviour
    {
        public enum Role { None, Button, Panel, Text, Surface }
        public enum TextRole { Body, Soft, Dim, Card, CardSoft, Title, Accent, Tag, Locked, Danger, HudLabel, Best, Gold, Arcana, Evolution, TitleArcana }
        public enum SurfaceRole { Accent, Veil, Row, Backdrop, HudBacking, HealthFill, HealthBack, SlotEmpty, SlotFilled, StoryCard, TouchPad, TouchKnob, Card, CardSelected, CardMaxed, CardLocked, Groove, Knob, SwitchOn, SwitchOff, TickBox, Tick, XpFill }

        public Role Kind;
        /// <summary>An ON/OFF switch (Options): a track with a sliding knob and the word; <see cref="SwitchOn"/> is its state.</summary>
        public bool IsSwitch;
        public bool SwitchOn;
        public UITheme.ButtonKind ButtonKind;
        public TextRole Text;
        public SurfaceRole Surface;
        public Image Graphic;
        public UITheme.IconSlot IconSlot;
        /// <summary>The active theme's picture for this button's icon slot.</summary>
        public Sprite Icon => UITheme.IconFor(IconSlot);
        public bool StyleLabels = true;
        public float Alpha = -1f;
        /// <summary>
        /// Which of the theme's standard sizes a button takes (owner, 2026-10-07: one size per kind of control in the
        /// whole game). <see cref="SizeRole.Free"/> keeps the size its screen gave it (cards, sliders, controls inside a
        /// row, the merchant's two-line wares).
        /// </summary>
        public enum SizeRole { Standard, Tab, Free }
        public SizeRole Size = SizeRole.Standard;
        /// <summary>A tick box (or other small graphic) that sits where an icon would, with the label beside it.</summary>
        public RectTransform LeadingGraphic;

        private Image _iconImage;
        private bool _subscribed;

        public static ThemedElement For(GameObject target)
        {
            var element = target.GetComponent<ThemedElement>();
            if (element == null) element = target.AddComponent<ThemedElement>();
            element.Subscribe(); // also for elements built while inactive, or outside Play Mode
            return element;
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            UiThemeAsset.Changed += Apply;
            _subscribed = true;
        }

        private void OnEnable()
        {
            Subscribe();
            Apply();
        }

        private void OnDestroy()
        {
            if (_subscribed) UiThemeAsset.Changed -= Apply;
            _subscribed = false;
        }

        private Vector2 _lastSize;

        // Buttons place their icon and label from their own size: when the size changes (layout, window), place them again.
        private void OnRectTransformDimensionsChange()
        {
            if (Kind != Role.Button || !isActiveAndEnabled || _fitting) return;
            // A screen that places its buttons again (the main menu refits to the window) gets the standard size back, once
            // its placing is finished: anchors are set one at a time, and each one already reports a size change here.
            if (StandardSize(UiThemeAsset.Active) is Vector2 standard && !InLayoutGroup()
                && ((transform as RectTransform).rect.size - standard).sqrMagnitude > 0.25f)
                _refitPending = true;
            var size = ((RectTransform)transform).rect.size;
            if ((size - _lastSize).sqrMagnitude < 0.25f) return;
            _lastSize = size;
            var selectable = GetComponent<Selectable>();
            var theme = UiThemeAsset.Active;
            if (ButtonKind == UITheme.ButtonKind.Card || selectable == null) return;
            PlaceIcon(transform as RectTransform, theme);
            foreach (var label in selectable.GetComponentsInChildren<TMP_Text>(true)) ButtonLabelLayout(label, theme);
            if (theme.Layouts.CentreIconWithLabel) PlaceIcon(transform as RectTransform, theme); // beside the laid-out label
        }

        /// <summary>Styles the element from the active theme.</summary>
        public void Apply()
        {
            if (this == null) return;
            switch (Kind)
            {
                case Role.Button: ApplyButton(); if (_unusable) ShowLocked(); break;
                case Role.Panel: ApplyPanel(); break;
                case Role.Text: ApplyText(GetComponent<TMP_Text>(), Text); break;
                case Role.Surface: ApplySurface(); break;
            }
        }

        // ---------------------------------------------------------------- buttons and cards

        private void ApplyButton()
        {
            var selectable = GetComponent<Selectable>();
            if (selectable == null) return;
            var graphic = Graphic != null ? Graphic : selectable.targetGraphic as Image;
            var theme = UiThemeAsset.Active;
            bool card = ButtonKind == UITheme.ButtonKind.Card;
            var slider = selectable as Slider;
            bool knob = slider != null && theme.Shape.Knob != null;
            Color normal = IsSwitch ? (SwitchOn ? theme.Rewards.Unlock : theme.Surfaces.Backdrop)
                : knob ? theme.Text.OnDark
                : ButtonKind switch
            {
                UITheme.ButtonKind.Primary => theme.Buttons.Primary,
                UITheme.ButtonKind.Danger => theme.Buttons.Danger,
                UITheme.ButtonKind.Card => theme.Cards.Card,
                UITheme.ButtonKind.Positive => theme.Rewards.Unlock,
                _ => theme.Buttons.Normal
            };
            Color hover = IsSwitch || knob ? Color.Lerp(normal, Color.white, 0.2f) : ButtonKind switch
            {
                UITheme.ButtonKind.Primary => theme.Buttons.PrimaryHover,
                UITheme.ButtonKind.Card => Color.Lerp(theme.Cards.Card, theme.Cards.Selected, 0.45f),
                UITheme.ButtonKind.Danger => Color.Lerp(theme.Buttons.Danger, Color.white, 0.25f),
                UITheme.ButtonKind.Positive => Color.Lerp(theme.Rewards.Unlock, Color.white, 0.25f),
                _ => theme.Buttons.Hover
            };
            var colors = selectable.colors;
            colors.normalColor = normal;
            colors.highlightedColor = hover;
            // A switch keeps its ON/OFF colour when chosen (the gold ring marks it), so its state stays readable.
            colors.selectedColor = card ? theme.Cards.Selected : IsSwitch ? normal
                : UITheme.SelectedColour(normal);
            colors.pressedColor = theme.Buttons.Pressed;
            colors.disabledColor = theme.Buttons.Disabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0f; // Bug MIS B13: focus changes at once
            selectable.colors = colors;
            if (graphic != null)
            {
                graphic.color = Color.white; // ColorTint multiplies it: the state colours carry the look
                var sprite = card ? theme.Shape.Card : IsSwitch && theme.Shape.Track != null ? theme.Shape.Track : knob ? theme.Shape.Knob : theme.Shape.Button;
                if (sprite != null)
                {
                    graphic.sprite = sprite;
                    graphic.type = knob ? Image.Type.Simple : Image.Type.Sliced;
                    graphic.preserveAspect = knob;
                    graphic.pixelsPerUnitMultiplier = 2f;
                }
                UITheme.Edge(graphic.gameObject, theme.Buttons.Edge, theme.Shape.EdgeWidth);
            }
            if (IsSwitch) { ApplySwitch(theme); return; }
            if (!card) FitSize(theme);
            if (!card) PlaceIcon(selectable.transform as RectTransform, theme);
            OpenTabMark(theme);
            if (StyleLabels)
                foreach (var label in selectable.GetComponentsInChildren<TMP_Text>(true))
                {
                    var labelElement = label.GetComponent<ThemedElement>();
                    // A label with a role of its own (a card's title, a tab's highlight) keeps its colour; buttons still
                    // give it the cartoon outline and their layout.
                    // Light or dark text, whichever reads best on this button's colour; the outline only on light text.
                    bool light = !card && UITheme.LightTextReads(normal);
                    var role = card ? TextRole.Card : light ? TextRole.Body : TextRole.Card;
                    if (labelElement == null || labelElement == this) ApplyText(label, role);
                    else if (!card && labelElement.Kind == Role.Text && (labelElement.Text == TextRole.Body || labelElement.Text == TextRole.Card))
                    {
                        // A label that only has the plain text role (screens give every new text one) takes the
                        // button's light-or-dark choice, and keeps it when the theme changes.
                        labelElement.Text = role;
                        labelElement.Apply();
                    }
                    if (!card)
                    {
                        if (theme.Font.OnDarkMaterial == null) // the theme's text materials carry outline and shadow
                        {
                            if (light) Outline(label, theme);
                            else if (label.gameObject.activeInHierarchy && label.outlineWidth > 0f) label.outlineWidth = 0f;
                        }
                        ButtonLabelLayout(label, theme);
                    }
                }
            if (!card && theme.Layouts.CentreIconWithLabel) PlaceIcon(selectable.transform as RectTransform, theme); // after the label
            var motion = GetComponent<ThemedMotion>();
            if (motion == null) motion = gameObject.AddComponent<ThemedMotion>();
            motion.IsCard = card;
        }

        private Image _switchKnob;
        private Image _openTabBar;

        /// <summary>The ON/OFF switch: the theme's size, the knob on the ON (right) or OFF (left) end, the word on the other.</summary>
        private void ApplySwitch(UiThemeAsset theme)
        {
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(theme.Layouts.SwitchWidth, theme.Layouts.SwitchHeight);
            float h = theme.Layouts.SwitchHeight;
            if (_switchKnob == null)
            {
                var knobGo = new GameObject("SwitchKnob", typeof(RectTransform), typeof(Image));
                knobGo.transform.SetParent(transform, false);
                _switchKnob = knobGo.GetComponent<Image>();
                _switchKnob.raycastTarget = false;
            }
            _switchKnob.sprite = theme.Shape.Knob != null ? theme.Shape.Knob : theme.Shape.Button;
            _switchKnob.type = theme.Shape.Knob != null ? Image.Type.Simple : Image.Type.Sliced;
            _switchKnob.preserveAspect = theme.Shape.Knob != null;
            _switchKnob.color = theme.Text.OnDark;
            var knobRect = _switchKnob.rectTransform;
            float k = h * 0.84f;
            knobRect.anchorMin = knobRect.anchorMax = new Vector2(SwitchOn ? 1f : 0f, 0.5f);
            knobRect.sizeDelta = new Vector2(k, k);
            knobRect.anchoredPosition = new Vector2(SwitchOn ? -(h * 0.5f) : h * 0.5f, 0f);
            foreach (var label in GetComponentsInChildren<TMP_Text>(true))
            {
                ApplyText(label, TextRole.Body); // light words on the green or dark track
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.alignment = TextAlignmentOptions.Center;
                label.margin = Vector4.zero;
                var lr = label.rectTransform;
                lr.anchorMin = new Vector2(SwitchOn ? 0.06f : 0.38f, 0f);
                lr.anchorMax = new Vector2(SwitchOn ? 0.62f : 0.94f, 1f);
                lr.offsetMin = lr.offsetMax = Vector2.zero;
                label.enableAutoSizing = true;
                label.fontSizeMin = Mathf.Max(12f, UiScreen.MinTextUnits); // never under 12 sp on a touch screen
                label.fontSizeMax = Mathf.Min(theme.Layouts.TabTextMax, h * 0.48f);
            }
            var motion = GetComponent<ThemedMotion>();
            if (motion == null) motion = gameObject.AddComponent<ThemedMotion>();
            motion.IsCard = false;
        }

        /// <summary>The open tab (a Primary tab) gets a gold bar under it, so it is not shown by colour alone.</summary>
        private void OpenTabMark(UiThemeAsset theme)
        {
            bool open = Size == SizeRole.Tab && ButtonKind == UITheme.ButtonKind.Primary && theme.Layouts.OpenTabBar;
            if (!open) { if (_openTabBar != null) _openTabBar.gameObject.SetActive(false); return; }
            if (_openTabBar == null)
            {
                var bar = new GameObject("OpenTabBar", typeof(RectTransform), typeof(Image));
                bar.transform.SetParent(transform, false);
                _openTabBar = bar.GetComponent<Image>();
                _openTabBar.raycastTarget = false;
            }
            _openTabBar.gameObject.SetActive(true);
            _openTabBar.color = theme.Text.Accent;
            var r = _openTabBar.rectTransform;
            r.anchorMin = new Vector2(0.18f, 0f);
            r.anchorMax = new Vector2(0.82f, 0f);
            r.pivot = new Vector2(0.5f, 1f);
            r.sizeDelta = new Vector2(0f, 5f);
            r.anchoredPosition = new Vector2(0f, -6f);
        }

        private bool _fitting;
        private bool _refitPending;

        private string _fitText;

        private void LateUpdate()
        {
            // A changed label can need a wider control on a touch screen (a control in a ThemedRow is refitted by its row).
            // Never a card: its texts are placed by its own screen (a refit here centred a level-up card's name, tag and
            // description on top of each other).
            if (Kind == Role.Button && Size != SizeRole.Free && ButtonKind != UITheme.ButtonKind.Card && UiScreen.IsTouch)
            {
                string text = LabelText;
                if (!ReferenceEquals(text, _fitText) && text != _fitText) _refitPending = true;
                _fitText = text;
            }
            if (!_refitPending) return;
            _refitPending = false;
            var theme = UiThemeAsset.Active;
            if (ButtonKind == UITheme.ButtonKind.Card) return;
            FitSize(theme);
            PlaceIcon(transform as RectTransform, theme);
            var selectable = GetComponent<Selectable>();
            if (selectable != null) foreach (var label in selectable.GetComponentsInChildren<TMP_Text>(true)) ButtonLabelLayout(label, theme);
        }

        /// <summary>The theme's size for this button, or null when it keeps its screen's size.</summary>
        public Vector2? StandardSize(UiThemeAsset theme)
        {
            if (Kind != Role.Button || ButtonKind == UITheme.ButtonKind.Card) return null;
            Vector2? size = Size switch
            {
                SizeRole.Standard => new Vector2(theme.Layouts.ButtonWidth, theme.Layouts.ButtonHeight),
                SizeRole.Tab => new Vector2(theme.Layouts.TabWidth, theme.Layouts.TabHeight),
                _ => (Vector2?)null
            };
            // On a touch screen no control is smaller than the 48 dp touch target (UiScreen; owner 2026-10-10): a tab grows
            // to it, the same rule for every screen.
            if (size is Vector2 v)
            {
                float min = UiScreen.MinTouchUnits;
                float height = Mathf.Max(v.y, min);
                // ...and grows wider to hold its label (owner, 2026-10-11: "tab se text bahar ja raha hai"): text on a
                // phone cannot shrink under 12 sp, so a fixed-width tab let "SORT: DEFAULT" run to its edges. A row of
                // tabs shares the widest one's width (ThemedRow sets RowWidth). The wiki gives no tab size (Options,
                // Characters: silent), so this is the ui-ux-pro-max rule: a label stays inside its control.
                float width = Mathf.Max(Mathf.Max(v.x, min), Mathf.Max(LabelFitWidth(theme, height), RowWidth));
                return new Vector2(width, height);
            }
            return null;
        }

        /// <summary>Set by a <see cref="ThemedRow"/>: the width its tabs share (0 = this control's own width).</summary>
        [System.NonSerialized] public float RowWidth;

        /// <summary>The label's text, for a row to notice a changed label ("SORT: A-Z" to "SORT: DEFAULT").</summary>
        public string LabelText { get { var label = MainLabel(); return label != null ? label.text : null; } }

        /// <summary>
        /// On a touch screen, the width this control needs for its label on one line with its padding: a tab at the theme's
        /// tab text size, an action button at the readable floor (it shrinks to that before it grows). 0 on a PC, where text
        /// can shrink further and the approved sizes stay.
        /// </summary>
        public float LabelFitWidth(UiThemeAsset theme, float height)
        {
            if (!UiScreen.IsTouch || Kind != Role.Button) return 0f;
            var label = MainLabel();
            if (label == null || string.IsNullOrEmpty(label.text)) return 0f;
            // A label made this frame has no font yet (TextMeshPro sets it up when it wakes) and cannot be measured: the
            // control is fitted again once it can (LateUpdate sees the label's text for the first time).
            if (label.font == null || label.fontSharedMaterial == null || !label.gameObject.activeInHierarchy) return 0f;
            float floor = Mathf.Max(12f, UiScreen.MinTextUnits);
            float font = Size == SizeRole.Tab ? Mathf.Max(floor, theme.Layouts.TabTextMax) : Mathf.Min(floor, theme.Layouts.ButtonTextMax);
            float measuredAt = label.enableAutoSizing ? label.fontSizeMax : label.fontSize; // what TMP measures a preferred width at
            if (measuredAt <= 0f) return 0f;
            float margins = label.margin.x + label.margin.z;
            float text = Mathf.Max(0f, label.GetPreferredValues(label.text, Mathf.Infinity, Mathf.Infinity).x - margins) * font / measuredAt;
            LabelInsets(theme, height, out float left, out float right, out float margin);
            return Mathf.Ceil(text + left + right + margin + 2f);
        }

        /// <summary>The room a button of height <paramref name="h"/> keeps on each side of its label, and the icon's margin inside it.</summary>
        private void LabelInsets(UiThemeAsset theme, float h, out float left, out float right, out float margin)
        {
            margin = 0f;
            if (theme.Layouts.CentreIconWithLabel && Icon != null)
            {
                // Group mode: the label spans the button (with even padding); a left margin keeps room for the icon.
                left = right = h * 0.15f;
                margin = h * (theme.Layouts.IconSize + theme.Layouts.IconGap);
            }
            else if (LeadingGraphic != null)
            {
                // A tick box: the label starts just after it.
                left = h * (theme.Layouts.IconInset + theme.Layouts.TickBoxSize + theme.Layouts.IconGap);
                right = h * 0.15f;
            }
            else if (Icon != null)
            {
                left = theme.Layouts.CentreLabelBesideIcon ? h * (theme.Layouts.IconInset + theme.Layouts.IconSize) : 0f;
                right = theme.Layouts.CentreLabelBesideIcon ? h * 0.15f : 0f;
            }
            else left = right = h * 0.3f; // bare: even padding, so a long label shrinks instead of touching the edges
        }

        private bool InLayoutGroup()
        {
            var group = transform.parent != null ? transform.parent.GetComponent<LayoutGroup>() : null;
            var own = GetComponent<LayoutElement>();
            return group != null && group.enabled && (own == null || !own.ignoreLayout);
        }

        /// <summary>
        /// Gives the button its standard size. In a layout group it asks the group for that size; otherwise it keeps the
        /// centre its screen placed it at and takes the size there (anchors collapse to that centre point).
        /// </summary>
        public void FitSize(UiThemeAsset theme)
        {
            if (!(StandardSize(theme) is Vector2 size) || !(transform is RectTransform rect)) return;
            _fitting = true;
            try
            {
                if (InLayoutGroup())
                {
                    var layout = GetComponent<LayoutElement>();
                    if (layout == null) layout = gameObject.AddComponent<LayoutElement>();
                    layout.minWidth = layout.preferredWidth = size.x;
                    layout.minHeight = layout.preferredHeight = size.y;
                    layout.flexibleWidth = layout.flexibleHeight = 0f;
                    return;
                }
                // Centre = lerp(anchorMin, anchorMax, pivot-free 0.5) * parent + mid of the offsets: kept exactly.
                Vector2 anchorCentre = (rect.anchorMin + rect.anchorMax) * 0.5f;
                Vector2 offsetCentre = (rect.offsetMin + rect.offsetMax) * 0.5f;
                rect.anchorMin = rect.anchorMax = anchorCentre;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = size;
                rect.anchoredPosition = offsetCentre;
            }
            finally { _fitting = false; }
        }

        private void PlaceIcon(RectTransform button, UiThemeAsset theme)
        {
            if (button == null) return;
            if (LeadingGraphic != null)
            {
                // A tick box takes the icon's place: left inset, square, vertically centred.
                float bh = button.rect.height > 1f ? button.rect.height : 46f;
                float box = bh * theme.Layouts.TickBoxSize;
                LeadingGraphic.anchorMin = LeadingGraphic.anchorMax = new Vector2(0f, 0.5f);
                LeadingGraphic.pivot = new Vector2(0.5f, 0.5f);
                LeadingGraphic.sizeDelta = new Vector2(box, box);
                LeadingGraphic.anchoredPosition = new Vector2(bh * theme.Layouts.IconInset + box * 0.5f, 0f);
            }
            if (Icon == null) { if (_iconImage != null) _iconImage.enabled = false; return; }
            if (_iconImage == null)
            {
                var existing = button.Find("ThemeIcon");
                if (existing != null) _iconImage = existing.GetComponent<Image>();
                else
                {
                    var go = new GameObject("ThemeIcon", typeof(RectTransform), typeof(Image), typeof(ThemedIconMotion));
                    var rect = (RectTransform)go.transform;
                    rect.SetParent(button, false);
                    _iconImage = go.GetComponent<Image>();
                    _iconImage.raycastTarget = false;
                    _iconImage.preserveAspect = true;
                }
            }
            _iconImage.enabled = true;
            _iconImage.sprite = Icon;
            var iconRect = (RectTransform)_iconImage.transform;
            float h = button.rect.height > 1f ? button.rect.height : 64f;
            float size = h * theme.Layouts.IconSize;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(size, size);
            float x = h * theme.Layouts.IconInset + size * 0.5f;
            var label = theme.Layouts.CentreIconWithLabel ? MainLabel() : null;
            if (label != null && label.rectTransform.parent == button)
            {
                // Icon and label as one centred group: the label is centred in the space after a margin the size of
                // the icon and its gap, and the icon sits just left of the drawn text, so the pair's middle is the
                // button's middle.
                label.ForceMeshUpdate();
                float textWidth = label.textBounds.size.x > 0f ? label.textBounds.size.x : 0f;
                var lr = label.rectTransform;
                float left = lr.offsetMin.x + label.margin.x, right = button.rect.width + lr.offsetMax.x;
                float textLeft = (left + right) * 0.5f - textWidth * 0.5f;
                x = Mathf.Max(h * 0.12f + size * 0.5f, textLeft - h * theme.Layouts.IconGap - size * 0.5f);
            }
            iconRect.anchoredPosition = new Vector2(x, 0f);
            var labelForColour = theme.Layouts.IconsLikeLabel ? (label != null ? label : MainLabel()) : null;
            _iconImage.color = labelForColour != null ? labelForColour.color : Color.white;
            _iconImage.transform.SetAsLastSibling();
        }

        /// <summary>The button's own label (its first text), or null.</summary>
        /// <summary>
        /// An unusable button (Buy without the gold) shows its label and icon in the theme's locked text colour, so it
        /// reads as "not now" on the disabled plate instead of dark text on a dark plate; usable again, the normal look returns.
        /// </summary>
        public void ShowUsable(bool usable)
        {
            if (Kind != Role.Button) return;
            _unusable = !usable;
            Apply(); // a restyle (theme change, rotation) keeps the unusable look
        }

        private bool _unusable;

        private void ShowLocked()
        {
            var locked = UiThemeAsset.Active.Text.Locked;
            foreach (var text in GetComponentsInChildren<TMP_Text>(true)) text.color = locked;
            if (_iconImage != null) _iconImage.color = locked;
        }

        private TMP_Text _mainLabel;

        private TMP_Text MainLabel()
        {
            // Remembered once found: rows and the label-fit check ask every frame on a touch screen.
            if (_mainLabel != null && _mainLabel.rectTransform.parent == transform) return _mainLabel;
            _mainLabel = null;
            foreach (var t in GetComponentsInChildren<TMP_Text>(true))
                if (t.rectTransform.parent == transform) { _mainLabel = t; break; }
            return _mainLabel;
        }

        private void ButtonLabelLayout(TMP_Text label, UiThemeAsset theme)
        {
            var rect = label.rectTransform;
            var button = transform as RectTransform;
            float h = button != null && button.rect.height > 1f ? button.rect.height : 64f;
            // With an icon, the label is centred in the space beside it (or in the whole button, as the designer picks).
            // A tick box: the label starts just after it (left-aligned), so a long label ("Limit Break") keeps its size.
            if (rect.parent == transform)
            {
                LabelInsets(theme, h, out float left, out float right, out float margin);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(left, 0f);
                rect.offsetMax = new Vector2(-right, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                label.margin = new Vector4(margin, 0f, 0f, 0f);
            }
            // Group mode centres on the text's optical middle line (caps and lower case alike).
            label.alignment = LeadingGraphic != null ? TextAlignmentOptions.MidlineLeft
                : theme.Layouts.CentreIconWithLabel ? TextAlignmentOptions.Midline : TextAlignmentOptions.Center;
            // The Back icon already is the arrow: a "<- Back" label (from the scene) would show it twice.
            if (IconSlot == UITheme.IconSlot.Back && label.text.StartsWith("<-")) label.text = label.text.Substring(2).TrimStart();
            // The theme's largest button text is a ceiling: a label its screen sized smaller (FILTER, 20) keeps its size.
            float ceiling = Size == SizeRole.Tab ? theme.Layouts.TabTextMax : theme.Layouts.ButtonTextMax;
            float max = label.enableAutoSizing && label.fontSizeMax > 0f ? Mathf.Min(label.fontSizeMax, ceiling) : ceiling;
            label.enableAutoSizing = true;
            label.fontSizeMax = max;
            // A long label shrinks to fit its standard-size button on one line (a copied template's large minimum
            // used to let "Refund PowerUps" run past its button).
            label.fontSizeMin = Mathf.Min(Mathf.Max(12f, UiScreen.MinTextUnits), max); // 12 sp floor on touch
            if (Size != SizeRole.Free) label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        // ---------------------------------------------------------------- panels, surfaces, text

        private void ApplyPanel()
        {
            var image = GetComponent<Image>();
            if (image == null) return;
            var theme = UiThemeAsset.Active;
            var colour = theme.Surfaces.Panel;
            if (Alpha >= 0f) colour.a = Alpha;
            image.color = colour;
            if (theme.Shape.Panel != null) { image.sprite = theme.Shape.Panel; image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 2f; }
            UITheme.Edge(image.gameObject, theme.Surfaces.PanelEdge, theme.Shape.Panel != null ? 0f : 2f);
        }

        private void ApplySurface()
        {
            var image = GetComponent<Image>();
            if (image == null) return;
            var t = UiThemeAsset.Active;
            image.color = Surface switch
            {
                SurfaceRole.Accent => t.Text.Accent,
                SurfaceRole.Veil => t.Surfaces.Veil,
                SurfaceRole.Row => t.Surfaces.Row,
                SurfaceRole.Backdrop => t.Surfaces.Backdrop,
                SurfaceRole.HudBacking => t.Hud.LabelBacking,
                SurfaceRole.HealthFill => t.Hud.HealthFill,
                SurfaceRole.HealthBack => t.Hud.HealthBack,
                SurfaceRole.SlotEmpty => t.Hud.SlotEmpty,
                SurfaceRole.SlotFilled => t.Hud.SlotFilled,
                SurfaceRole.StoryCard => t.Hud.StoryCard,
                SurfaceRole.TouchPad => t.Hud.TouchPad,
                SurfaceRole.TouchKnob => t.Hud.TouchKnob,
                SurfaceRole.Card => t.Cards.Card,
                SurfaceRole.CardSelected => t.Cards.Selected,
                SurfaceRole.CardMaxed => t.Cards.Maxed,
                SurfaceRole.CardLocked => t.Cards.Locked,
                SurfaceRole.Groove => t.Shape.Inset != null ? t.Surfaces.Backdrop : t.Surfaces.Row,
                SurfaceRole.Knob => t.Text.OnDark,
                SurfaceRole.SwitchOn => t.Rewards.Unlock,
                SurfaceRole.SwitchOff => t.Surfaces.Backdrop,
                SurfaceRole.TickBox => t.Shape.TickBox != null ? t.Text.OnDark : t.Surfaces.Row,
                SurfaceRole.XpFill => t.Hud.XpFill,
                SurfaceRole.Tick => t.Text.Accent,
                _ => image.color
            };
            bool cardLike = Surface == SurfaceRole.Card || Surface == SurfaceRole.CardSelected || Surface == SurfaceRole.CardMaxed || Surface == SurfaceRole.CardLocked;
            if (cardLike && t.Shape.Card != null) { image.sprite = t.Shape.Card; image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 2f; }
            // Rows, boxes, HUD labels and slots share the bevelled row picture, so nothing next to a 3D button is flat.
            // A theme without its own groove or tick-box picture shows them as rows, as before.
            bool rowLike = Surface == SurfaceRole.Row || Surface == SurfaceRole.HudBacking || Surface == SurfaceRole.SlotEmpty
                || Surface == SurfaceRole.SlotFilled || Surface == SurfaceRole.StoryCard
                || (Surface == SurfaceRole.Groove && t.Shape.Inset == null) || (Surface == SurfaceRole.TickBox && t.Shape.TickBox == null);
            if (rowLike && t.Shape.Row != null) { image.sprite = t.Shape.Row; image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 2f; }
            // Slider grooves, round knobs and switch tracks (review 2026-10-08: a switch should read as a switch).
            Sprite shape = Surface switch
            {
                SurfaceRole.Groove => t.Shape.Inset,
                SurfaceRole.HealthBack => t.Shape.Inset, // the health bar sits in a groove too
                SurfaceRole.Knob => t.Shape.Knob,
                SurfaceRole.TickBox => t.Shape.TickBox,
                SurfaceRole.Tick => t.Shape.Tick,
                SurfaceRole.SwitchOn or SurfaceRole.SwitchOff => t.Shape.Track,
                _ => null
            };
            if (shape != null)
            {
                image.sprite = shape;
                bool simple = Surface == SurfaceRole.Knob || Surface == SurfaceRole.TickBox || Surface == SurfaceRole.Tick;
                image.type = simple ? Image.Type.Simple : Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 2f;
                image.preserveAspect = simple;
            }
        }

        /// <summary>The theme's dark outline around a text (titles, button labels, HUD labels); width 0 = none.</summary>
        public static void Outline(TMP_Text text, UiThemeAsset theme)
        {
            // TMP builds its material on Awake: an inactive text cannot take an outline yet (it gets one when it is
            // enabled, as themed elements re-apply then).
            if (text == null || !text.gameObject.activeInHierarchy) return;
            // Setting an outline gives the text its own material instance, so the shared font material stays clean.
            float width = theme.Text.OutlineWidth;
            if (width <= 0f && text.outlineWidth <= 0f) return;
            text.outlineWidth = width;
            text.outlineColor = theme.Text.Outline;
        }

        public static void ApplyText(TMP_Text text, TextRole role, bool outline = false)
        {
            if (text == null) return;
            var t = UiThemeAsset.Active;
            text.color = role switch
            {
                TextRole.Soft => t.Text.SoftOnDark,
                TextRole.Dim => t.Text.DimOnDark,
                TextRole.Card => t.Text.OnLight,
                TextRole.CardSoft => t.Text.SoftOnLight,
                TextRole.Title => t.Text.Accent,
                TextRole.Accent => t.Text.Accent,
                TextRole.Tag => t.Text.Tag,
                TextRole.Locked => t.Text.Locked,
                TextRole.Danger => t.Text.Danger,
                TextRole.HudLabel => t.Hud.LabelText,
                TextRole.Best => t.Rewards.Best,
                TextRole.Gold => t.Rewards.Gold,
                TextRole.Arcana => t.Rewards.Arcana,
                TextRole.TitleArcana => t.Rewards.Arcana, // an Arcana chest's title: the title look in violet
                TextRole.Evolution => t.Rewards.Evolution,
                _ => t.Text.OnDark
            };
            bool title = role == TextRole.Title || role == TextRole.TitleArcana;
            var font = title ? t.Font.Title : t.Font.Body;
            if (font != null && text.font != font) text.font = font;
            var material = TextMaterial(t, role, text.font);
            if (material != null)
            {
                // The theme's raised look: outline and shadow live in the shared material (crisp, no per-text copies).
                if (text.fontSharedMaterial != material) text.fontSharedMaterial = material;
            }
            else if (title || outline || role == TextRole.HudLabel) Outline(text, t);
            if (title && t.Font.TitleGradient)
            {
                Color c = text.color;
                Color top = Color.Lerp(c, Color.white, 0.35f), bottom = Color.Lerp(c, Color.black, 0.25f);
                text.enableVertexGradient = true;
                text.colorGradient = new VertexGradient(top, top, bottom, bottom);
                text.color = Color.white; // the gradient carries the colour
            }
            if (title && t.Font.TitleMaterial == null) text.fontStyle |= FontStyles.Bold; // a real heavy title font needs no fake bold
            if (t.Font.NoFakeBold && (text.font == t.Font.Body || text.font == t.Font.Title)) text.fontStyle &= ~FontStyles.Bold;
            if (title && text.GetComponent<ThemedTitleMotion>() == null) text.gameObject.AddComponent<ThemedTitleMotion>();
        }

        /// <summary>The theme's text material for this role, when it was made for the text's font; null otherwise.</summary>
        public static Material TextMaterial(UiThemeAsset t, TextRole role, TMP_FontAsset font)
        {
            if (font == null) return null;
            if (role == TextRole.Title || role == TextRole.TitleArcana) return font == t.Font.Title ? t.Font.TitleMaterial : null;
            if (font != t.Font.Body) return null;
            bool onLight = role == TextRole.Card || role == TextRole.CardSoft;
            return onLight ? t.Font.OnLightMaterial : t.Font.OnDarkMaterial;
        }
    }
}
