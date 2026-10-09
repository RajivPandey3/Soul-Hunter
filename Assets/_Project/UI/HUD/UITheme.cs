using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// The only way UI code gets a look: every value comes from the active <see cref="UiThemeAsset"/> (the single source
    /// of truth, edited by designers in Soul Hunter > Theme Studio). UI scripts never keep colours of their own; they say
    /// what an element is (a button, the main action, a card, a title) and the theme decides how it looks. Elements styled
    /// here carry a <see cref="ThemedElement"/>, so a theme change re-applies them at once, also during play.
    /// </summary>
    public static class UITheme
    {
        private static UiThemeAsset T => UiThemeAsset.Active;

        // Surfaces
        public static Color Veil => T.Surfaces.Veil;
        public static Color Panel => T.Surfaces.Panel;
        public static Color PanelEdge => T.Surfaces.PanelEdge;
        public static Color Row => T.Surfaces.Row;
        public static Color Backdrop => T.Surfaces.Backdrop;

        // Buttons (state colours; the graphic stays white so they show)
        public static Color Button => T.Buttons.Normal;
        public static Color ButtonHover => T.Buttons.Hover;
        public static Color Primary => T.Buttons.Primary;
        public static Color PrimaryHover => T.Buttons.PrimaryHover;
        public static Color DangerButton => T.Buttons.Danger;
        public static Color Focus => T.Buttons.Focus;
        public static Color Pressed => T.Buttons.Pressed;
        public static Color Disabled => T.Buttons.Disabled;
        public static Color ButtonEdge => T.Buttons.Edge;

        // Cards and cells
        public static Color Cell => T.Cards.Card;
        public static Color CellSelected => T.Cards.Selected;
        public static Color CellMaxed => T.Cards.Maxed;
        public static Color CellLocked => T.Cards.Locked;
        /// <summary>A maxed Power Up switched off (VS PowerUps: "dark gray").</summary>
        public static Color CellOff => T.Cards.Off;

        // Text
        public static Color Text => T.Text.OnDark;
        public static Color TextSoft => T.Text.SoftOnDark;
        public static Color TextDim => T.Text.DimOnDark;
        public static Color CardText => T.Text.OnLight;
        public static Color CardTextSoft => T.Text.SoftOnLight;
        public static Color TextLocked => T.Text.Locked;
        public static Color Accent => T.Text.Accent;
        /// <summary>A chosen (focused) button's colour: its own colour a little brighter when the theme keeps colours
        /// (the ring marks it), else the theme's Focus colour.</summary>
        public static Color SelectedColour(Color normal) => T.Motions.FocusKeepsColour ? Color.Lerp(normal, Color.white, 0.22f) : T.Buttons.Focus;
        /// <summary>Bold rich text, or the plain text when the theme's fonts are already heavy (no fake bold: it widens the letters).</summary>
        public static string Bold(string text) => T.Font.NoFakeBold ? text : "<b>" + text + "</b>";
        public static Color Danger => T.Text.Danger;
        public static Color Tag => T.Text.Tag;
        public static Color Outline => T.Text.Outline;

        // Rarity, HUD, rewards, icons, motion, layout, shapes
        public static UiThemeAsset.RarityColours Rarity => T.Rarity;
        public static UiThemeAsset.HudColours Hud => T.Hud;
        public static UiThemeAsset.RewardColours Rewards => T.Rewards;
        public static UiThemeAsset.Icons Icons => T.Icon;
        public static UiThemeAsset.Motion Motion => T.Motions;
        public static UiThemeAsset.Layout Layout => T.Layouts;
        public static UiThemeAsset.Shapes Shapes => T.Shape;
        public static UiThemeAsset.Fonts Fonts => T.Font;

        /// <summary>WCAG contrast ratio of two colours (1 to 21).</summary>
        public static float Contrast(Color a, Color b)
        {
            float la = Luminance(a), lb = Luminance(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }

        private static float Luminance(Color c)
        {
            float L(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * L(c.r) + 0.7152f * L(c.g) + 0.0722f * L(c.b);
        }

        /// <summary>
        /// The theme's text colour that reads best on <paramref name="back"/>: its light text or its dark text, whichever
        /// has more contrast (ui-ux-pro-max: 4.5:1 for normal text). Button labels use it, so a designer can make any
        /// button light or dark and its label stays readable.
        /// </summary>
        public static bool LightTextReads(Color back) => Contrast(Text, back) >= Contrast(CardText, back);

        /// <summary>Which of the theme's icons a button shows; the picture itself is read from the theme every time, so a
        /// designer's new icon shows at once.</summary>
        public enum IconSlot { None, Start, PowerUp, Options, Quit, Back, Confirm, Resume, Buy, Emblem, Coin, Collection, Bestiary, Unlocks, Credits }

        /// <summary>The active theme's picture for a slot (null when the slot is empty).</summary>
        public static Sprite IconFor(IconSlot slot) => slot switch
        {
            IconSlot.Start => Icons.Start,
            IconSlot.PowerUp => Icons.PowerUp,
            IconSlot.Options => Icons.Options,
            IconSlot.Quit => Icons.Quit,
            IconSlot.Back => Icons.Back,
            IconSlot.Confirm => Icons.Confirm,
            IconSlot.Resume => Icons.Resume,
            IconSlot.Buy => Icons.Buy,
            IconSlot.Emblem => Icons.Emblem,
            IconSlot.Coin => Icons.Coin != null ? Icons.Coin : Icons.Buy,
            IconSlot.Collection => Icons.Collection,
            IconSlot.Bestiary => Icons.Bestiary,
            IconSlot.Unlocks => Icons.Unlocks,
            IconSlot.Credits => Icons.Credits,
            _ => null
        };

        /// <summary>What a button is; the theme picks its colours.</summary>
        public enum ButtonKind { Normal, Primary, Danger, Card, Positive }

        /// <summary>
        /// Styles a button or card from the theme and keeps it themed (re-applied on every theme change): state colours,
        /// the theme's button picture, its edge, an optional icon, the label's colour/font/outline, and its motion.
        /// </summary>
        public static void ApplyButton(Selectable selectable, Image graphic, ButtonKind kind, IconSlot icon = IconSlot.None, bool labels = true,
            ThemedElement.SizeRole size = ThemedElement.SizeRole.Standard, RectTransform leading = null)
        {
            if (selectable == null) return;
            var element = ThemedElement.For(selectable.gameObject);
            element.Kind = ThemedElement.Role.Button;
            element.ButtonKind = kind;
            element.Graphic = graphic;
            element.StyleLabels = labels;
            element.IconSlot = icon;
            element.Size = size; // every action button takes the theme's one standard size unless it says otherwise
            if (leading != null) element.LeadingGraphic = leading;
            element.Apply();
        }

        /// <summary>Older call shape (main action yes/no); the same theme path.</summary>
        public static void ApplyButton(Selectable selectable, Image graphic, bool primary = false, bool edge = true, bool labels = false) =>
            ApplyButton(selectable, graphic, primary ? ButtonKind.Primary : ButtonKind.Normal, IconSlot.None, labels);

        /// <summary>A panel or window: the theme's panel colour, picture and edge; kept themed.</summary>
        public static void ApplyPanel(Image image, float alpha = -1f)
        {
            if (image == null) return;
            var element = ThemedElement.For(image.gameObject);
            element.Kind = ThemedElement.Role.Panel;
            element.Alpha = alpha;
            element.Apply();
        }

        /// <summary>A text in one of the theme's text roles; kept themed.</summary>
        public static T ApplyText<T>(T text, ThemedElement.TextRole role) where T : TMP_Text
        {
            if (text == null) return null;
            var element = ThemedElement.For(text.gameObject);
            element.Kind = ThemedElement.Role.Text;
            element.Text = role;
            element.Apply();
            return text;
        }

        /// <summary>An image filled with one of the theme's surface colours (rows, backings, HUD plates); kept themed.</summary>
        public static Image ApplySurface(Image image, ThemedElement.SurfaceRole role)
        {
            if (image == null) return null;
            var element = ThemedElement.For(image.gameObject);
            element.Kind = ThemedElement.Role.Surface;
            element.Surface = role;
            element.Apply();
            return image;
        }

        /// <summary>A thin coloured edge (an Outline effect), added once. Width 0 hides it.</summary>
        public static Outline Edge(GameObject target, Color colour, float width)
        {
            Outline edge = null;
            foreach (var outline in target.GetComponents<Outline>())
                if (!(outline is ThemeFocusRing)) { edge = outline; break; } // never the focus ring
            if (width <= 0f) { if (edge != null) edge.enabled = false; return edge; }
            if (edge == null) edge = target.AddComponent<Outline>();
            edge.enabled = true;
            edge.effectColor = colour;
            edge.effectDistance = new Vector2(width, -width);
            edge.useGraphicAlpha = true;
            return edge;
        }

        /// <summary>A full-screen veil as the first child of <paramref name="parent"/>, behind everything in it; kept themed.</summary>
        public static Image AddVeil(Transform parent, string name = "ThemeVeil")
        {
            if (parent == null) return null;
            var existing = parent.Find(name);
            Image veil;
            if (existing != null) veil = existing.GetComponent<Image>();
            else
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)go.transform;
                rect.SetParent(parent, false);
                rect.anchorMin = new Vector2(-1f, -1f); // reaches past the panel to the whole screen
                rect.anchorMax = new Vector2(2f, 2f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                veil = go.GetComponent<Image>();
            }
            veil.transform.SetAsFirstSibling();
            veil.raycastTarget = true;
            ApplySurface(veil, ThemedElement.SurfaceRole.Veil);
            return veil;
        }
    }
}
