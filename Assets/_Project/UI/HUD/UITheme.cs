using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Soul Hunter's one UI look (owner, 2026-10-04: "all panel ... pura advance, pura contrast, dark"): near-black panels
    /// with a crimson edge, dark crimson buttons and cards with a red edge, warm white text, gold for highlights and
    /// changed values, and the game-wide orange for keyboard/controller focus (Bug MIS B16). In-game menus sit on a dark
    /// veil so the game behind does not compete with them. Every panel takes its colours from here.
    /// </summary>
    public static class UITheme
    {
        // Surfaces
        // The UI blends in linear space: a 0.74 black layer only halved the brightness behind it, so the veil is 0.9.
        public static readonly Color Veil = new Color(0f, 0f, 0f, 0.9f);
        public static readonly Color Panel = new Color(0.035f, 0.022f, 0.028f, 0.97f);
        public static readonly Color PanelEdge = new Color(0.52f, 0.08f, 0.09f, 1f);
        public static readonly Color Row = new Color(0.085f, 0.045f, 0.05f, 1f);

        // Buttons and cards (state colours; the graphic stays white so they show)
        public static readonly Color Button = new Color(0.17f, 0.04f, 0.05f, 1f);
        public static readonly Color ButtonHover = new Color(0.36f, 0.07f, 0.08f, 1f);
        public static readonly Color Primary = new Color(0.46f, 0.07f, 0.08f, 1f);
        public static readonly Color PrimaryHover = new Color(0.62f, 0.10f, 0.10f, 1f);
        public static readonly Color Focus = new Color(0.85f, 0.45f, 0.08f, 1f);
        public static readonly Color Pressed = new Color(1f, 0.7f, 0.2f, 1f);
        public static readonly Color Disabled = new Color(0.07f, 0.05f, 0.06f, 0.85f);
        public static readonly Color ButtonEdge = new Color(0.78f, 0.14f, 0.12f, 0.9f);

        // Cells (Power Up, character cards)
        public static readonly Color Cell = new Color(0.10f, 0.045f, 0.05f, 0.96f);
        public static readonly Color CellSelected = new Color(0.62f, 0.13f, 0.08f, 0.98f);
        public static readonly Color CellMaxed = new Color(0.30f, 0.22f, 0.05f, 0.96f);
        public static readonly Color CellLocked = new Color(0.045f, 0.04f, 0.045f, 0.96f);

        // Text
        public static readonly Color Text = new Color(0.96f, 0.92f, 0.86f, 1f);
        public static readonly Color TextSoft = new Color(0.86f, 0.80f, 0.74f, 1f);
        public static readonly Color TextDim = new Color(0.70f, 0.64f, 0.60f, 1f);
        public static readonly Color TextLocked = new Color(0.45f, 0.42f, 0.42f, 1f);
        public static readonly Color Accent = new Color(1f, 0.80f, 0.30f, 1f);
        public static readonly Color Danger = new Color(1f, 0.35f, 0.3f, 1f);

        /// <summary>
        /// The state colours of a button or card. ColorTint multiplies the graphic colour, so the graphic stays white and
        /// the state colours carry the look. Focus changes at once (Bug MIS B13); disabled is dark so its label reads (B17).
        /// </summary>
        public static void ApplyButton(Selectable selectable, Image graphic, bool primary = false, bool edge = true, bool labels = false)
        {
            if (selectable == null) return;
            if (graphic != null) graphic.color = Color.white;
            var colors = selectable.colors;
            colors.normalColor = primary ? Primary : Button;
            colors.highlightedColor = primary ? PrimaryHover : ButtonHover;
            colors.selectedColor = Focus;
            colors.pressedColor = Pressed;
            colors.disabledColor = Disabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0f;
            selectable.colors = colors;
            if (edge && graphic != null) Edge(graphic.gameObject, ButtonEdge, 2f);
            if (!labels) return;
            foreach (var label in selectable.GetComponentsInChildren<TMP_Text>(true)) label.color = Text;
            foreach (var label in selectable.GetComponentsInChildren<UnityEngine.UI.Text>(true)) label.color = Text;
        }

        /// <summary>A panel: near-black with a crimson edge.</summary>
        public static void ApplyPanel(Image image, float alpha = 0.97f)
        {
            if (image == null) return;
            image.color = new Color(Panel.r, Panel.g, Panel.b, alpha);
            Edge(image.gameObject, PanelEdge, 2f);
        }

        /// <summary>A thin coloured edge (an Outline effect), added once.</summary>
        public static Outline Edge(GameObject target, Color colour, float width)
        {
            var edge = target.GetComponent<Outline>();
            if (edge == null) edge = target.AddComponent<Outline>();
            edge.effectColor = colour;
            edge.effectDistance = new Vector2(width, -width);
            edge.useGraphicAlpha = true;
            return edge;
        }

        /// <summary>A full-screen dark veil as the first child of <paramref name="parent"/>, behind everything in it.</summary>
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
            veil.color = Veil;
            veil.raycastTarget = true;
            return veil;
        }
    }
}
