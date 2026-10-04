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
        public enum TextRole { Body, Soft, Dim, Card, CardSoft, Title, Accent, Tag, Locked, Danger, HudLabel, Best, Gold, Arcana, Evolution }
        public enum SurfaceRole { Accent, Veil, Row, Backdrop, HudBacking, HealthFill, HealthBack, SlotEmpty, SlotFilled, StoryCard, TouchPad, TouchKnob, Card, CardSelected, CardMaxed, CardLocked }

        public Role Kind;
        public UITheme.ButtonKind ButtonKind;
        public TextRole Text;
        public SurfaceRole Surface;
        public Image Graphic;
        public UITheme.IconSlot IconSlot;
        /// <summary>The active theme's picture for this button's icon slot.</summary>
        public Sprite Icon => UITheme.IconFor(IconSlot);
        public bool StyleLabels = true;
        public float Alpha = -1f;

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
            if (Kind != Role.Button || !isActiveAndEnabled) return;
            var size = ((RectTransform)transform).rect.size;
            if ((size - _lastSize).sqrMagnitude < 0.25f) return;
            _lastSize = size;
            var selectable = GetComponent<Selectable>();
            var theme = UiThemeAsset.Active;
            if (ButtonKind == UITheme.ButtonKind.Card || selectable == null) return;
            PlaceIcon(transform as RectTransform, theme);
            foreach (var label in selectable.GetComponentsInChildren<TMP_Text>(true)) ButtonLabelLayout(label, theme);
        }

        /// <summary>Styles the element from the active theme.</summary>
        public void Apply()
        {
            if (this == null) return;
            switch (Kind)
            {
                case Role.Button: ApplyButton(); break;
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
            Color normal = ButtonKind switch
            {
                UITheme.ButtonKind.Primary => theme.Buttons.Primary,
                UITheme.ButtonKind.Danger => theme.Buttons.Danger,
                UITheme.ButtonKind.Card => theme.Cards.Card,
                UITheme.ButtonKind.Positive => theme.Rewards.Unlock,
                _ => theme.Buttons.Normal
            };
            Color hover = ButtonKind switch
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
            colors.selectedColor = card ? theme.Cards.Selected : theme.Buttons.Focus;
            colors.pressedColor = theme.Buttons.Pressed;
            colors.disabledColor = theme.Buttons.Disabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0f; // Bug MIS B13: focus changes at once
            selectable.colors = colors;
            if (graphic != null)
            {
                graphic.color = Color.white; // ColorTint multiplies it: the state colours carry the look
                var sprite = card ? theme.Shape.Card : theme.Shape.Button;
                if (sprite != null) { graphic.sprite = sprite; graphic.type = Image.Type.Sliced; graphic.pixelsPerUnitMultiplier = 2f; }
                UITheme.Edge(graphic.gameObject, theme.Buttons.Edge, theme.Shape.EdgeWidth);
            }
            if (!card) PlaceIcon(selectable.transform as RectTransform, theme);
            if (StyleLabels)
                foreach (var label in selectable.GetComponentsInChildren<TMP_Text>(true))
                {
                    var labelElement = label.GetComponent<ThemedElement>();
                    // A label with a role of its own (a card's title, a tab's highlight) keeps its colour; buttons still
                    // give it the cartoon outline and their layout.
                    if (labelElement == null || labelElement == this) ApplyText(label, card ? TextRole.Card : TextRole.Body);
                    if (!card) { Outline(label, theme); ButtonLabelLayout(label, theme); }
                }
            var motion = GetComponent<ThemedMotion>();
            if (motion == null) motion = gameObject.AddComponent<ThemedMotion>();
            motion.IsCard = card;
        }

        private void PlaceIcon(RectTransform button, UiThemeAsset theme)
        {
            if (button == null) return;
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
            iconRect.anchoredPosition = new Vector2(h * theme.Layouts.IconInset + size * 0.5f, 0f);
            _iconImage.transform.SetAsLastSibling();
        }

        private void ButtonLabelLayout(TMP_Text label, UiThemeAsset theme)
        {
            var rect = label.rectTransform;
            var button = transform as RectTransform;
            float h = button != null && button.rect.height > 1f ? button.rect.height : 64f;
            // With an icon, the label is centred in the space beside it (or in the whole button, as the designer picks).
            float left = Icon != null && theme.Layouts.CentreLabelBesideIcon ? h * (theme.Layouts.IconInset + theme.Layouts.IconSize) : 0f;
            if (rect.parent == transform)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(left, 0f);
                rect.offsetMax = new Vector2(Icon != null && !theme.Layouts.CentreLabelBesideIcon ? 0f : -h * 0.15f, 0f);
            }
            label.alignment = TextAlignmentOptions.Center;
            // The theme's largest button text is a ceiling: a label its screen sized smaller (FILTER, 20) keeps its size.
            float max = label.enableAutoSizing && label.fontSizeMax > 0f ? Mathf.Min(label.fontSizeMax, theme.Layouts.ButtonTextMax) : theme.Layouts.ButtonTextMax;
            label.enableAutoSizing = true;
            label.fontSizeMax = max;
            label.fontSizeMin = Mathf.Min(label.fontSizeMin > 0f ? label.fontSizeMin : 14f, max);
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
                _ => image.color
            };
            bool cardLike = Surface == SurfaceRole.Card || Surface == SurfaceRole.CardSelected || Surface == SurfaceRole.CardMaxed || Surface == SurfaceRole.CardLocked;
            if (cardLike && t.Shape.Card != null) { image.sprite = t.Shape.Card; image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 2f; }
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
                TextRole.Evolution => t.Rewards.Evolution,
                _ => t.Text.OnDark
            };
            bool title = role == TextRole.Title;
            var font = title ? t.Font.Title : t.Font.Body;
            if (font != null && text.font != font) text.font = font;
            if (title || outline || role == TextRole.HudLabel) Outline(text, t);
            if (title) text.fontStyle |= FontStyles.Bold;
            if (title && text.GetComponent<ThemedTitleMotion>() == null) text.gameObject.AddComponent<ThemedTitleMotion>();
        }
    }
}
