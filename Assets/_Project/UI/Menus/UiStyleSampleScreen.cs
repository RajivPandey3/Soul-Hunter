using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.UI
{
    /// <summary>
    /// One screen with nearly every UI element in the look waiting for approval (owner, 2026-10-08: "ek scene dikhao
    /// jisme lagbhag saare item ho, mai use paas karunga, phir sabhi per apply kar dena"): buttons in their states with
    /// icons, tabs, toggle chips, a slider and a switch, a level-up card, shop cells in all four states, the info box
    /// with price and actions, every text style, HUD labels and slots, and the Full HD icons. Built at runtime on its own
    /// canvas with <see cref="GameContentCatalog.UiThemePreview"/> active; <see cref="Hide"/> restores the game's look.
    /// Opened by AutoplayBot -sh-autoplay-style-sample.
    /// </summary>
    public sealed class UiStyleSampleScreen : MonoBehaviour
    {
        private static UiStyleSampleScreen _open;
        private UiThemeAsset _previous;

        public static UiStyleSampleScreen Current => _open;

        public static void Show()
        {
            if (_open != null) return;
            var go = new GameObject("UiStyleSample", typeof(RectTransform));
            _open = go.AddComponent<UiStyleSampleScreen>();
            _open.Build();
        }

        public static void Hide()
        {
            if (_open == null) return;
            UiThemeAsset.SetActive(_open._previous);
            Destroy(_open.gameObject);
            _open = null;
        }

        private void Build()
        {
            var catalog = GameContentCatalog.Load();
            _previous = catalog != null ? catalog.UiTheme : null;
            if (catalog != null && catalog.UiThemePreview != null) UiThemeAsset.SetActive(catalog.UiThemePreview);

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            gameObject.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)transform;

            var backdrop = Rect("Backdrop", root, 0f, 0f, 1f, 1f);
            UITheme.ApplySurface(backdrop.gameObject.AddComponent<Image>(), ThemedElement.SurfaceRole.Backdrop);
            var window = Rect("Window", root, 0.015f, 0.02f, 0.985f, 0.98f);
            UITheme.ApplyPanel(window.gameObject.AddComponent<Image>());

            // Header
            var title = Text("Title", window, 0.02f, 0.88f, 0.6f, 0.98f, "Soul Hunter", 72f, TextAlignmentOptions.MidlineLeft, ThemedElement.TextRole.Title);
            Text("Subtitle", window, 0.02f, 0.835f, 0.6f, 0.885f, "Style sample: Ember Pact Pro", 28f, TextAlignmentOptions.MidlineLeft, ThemedElement.TextRole.Soft);
            var gold = Rect("Gold", window, 0.78f, 0.9f, 0.98f, 0.97f);
            var coin = Icon("Coin", gold, UITheme.IconFor(UITheme.IconSlot.Coin), 52f);
            coin.rectTransform.anchorMin = coin.rectTransform.anchorMax = new Vector2(0.18f, 0.5f);
            Text("Amount", gold, 0.28f, 0f, 1f, 1f, "15,894", 44f, TextAlignmentOptions.MidlineLeft, ThemedElement.TextRole.Gold);

            // Column 1: action buttons
            Heading(window, 0.02f, 0.77f, "Buttons");
            var col1 = Rect("Buttons", window, 0.02f, 0.06f, 0.2f, 0.76f);
            AddButton(col1, 0.9f, "Start", UITheme.ButtonKind.Primary, UITheme.IconSlot.Start);
            AddButton(col1, 0.78f, "Power Up", UITheme.ButtonKind.Normal, UITheme.IconSlot.PowerUp);
            var focused = AddButton(col1, 0.66f, "Options", UITheme.ButtonKind.Normal, UITheme.IconSlot.Options);
            AddButton(col1, 0.54f, "Back", UITheme.ButtonKind.Normal, UITheme.IconSlot.Back);
            AddButton(col1, 0.42f, "Quit", UITheme.ButtonKind.Danger, UITheme.IconSlot.Quit);
            AddButton(col1, 0.30f, "Confirm", UITheme.ButtonKind.Positive, UITheme.IconSlot.Confirm);
            var disabled = AddButton(col1, 0.18f, "Buy", UITheme.ButtonKind.Primary, UITheme.IconSlot.Buy);
            disabled.interactable = false;
            Text("States", col1, 0f, 0.0f, 1f, 0.1f, "Options = focused, Buy = not enough gold", 18f, TextAlignmentOptions.Center, ThemedElement.TextRole.Dim);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(focused.gameObject);

            // Column 2: tabs, chips, slider, switch
            Heading(window, 0.215f, 0.77f, "Tabs and switches");
            var tabs = Rect("Tabs", Rect("TabsArea", window, 0.205f, 0.66f, 0.6f, 0.75f), 0.5f, 0f, 0.5f, 1f);
            string[] sections = { "Sound", "Display", "Gameplay", "Controls" };
            for (int i = 0; i < sections.Length; i++)
            {
                var tab = ButtonAt(tabs, (i + 0.5f) / sections.Length, 0.5f, sections[i], i == 0 ? UITheme.ButtonKind.Primary : UITheme.ButtonKind.Normal,
                    UITheme.IconSlot.None, ThemedElement.SizeRole.Tab);
            }
            tabs.gameObject.AddComponent<ThemedRow>();
            var chips = Rect("Chips", Rect("ChipsArea", window, 0.205f, 0.56f, 0.6f, 0.65f), 0.5f, 0f, 0.5f, 1f);
            string[] modes = { "Hurry", "Hyper", "Arcana", "Endless" };
            for (int i = 0; i < modes.Length; i++) Chip(chips, (i + 0.5f) / modes.Length, modes[i], i == 0 || i == 2);
            chips.gameObject.AddComponent<ThemedRow>();
            var sliderRow = Row(window, 0.215f, 0.47f, 0.59f, 0.545f, "Music");
            Slider(sliderRow, 0.8f);
            var switchRow = Row(window, 0.215f, 0.385f, 0.59f, 0.46f, "Screen Shake");
            var toggleSwitch = Switch(switchRow, 0.68f, true);
            Switch(switchRow, 0.9f, false);

            // Text styles
            Heading(window, 0.215f, 0.325f, "Text");
            var texts = Rect("Texts", window, 0.215f, 0.04f, 0.59f, 0.32f);
            Text("Heading", texts, 0f, 0.78f, 1f, 1f, "Level Up!", 48f, TextAlignmentOptions.MidlineLeft, ThemedElement.TextRole.Title);
            Text("Accent", texts, 0f, 0.6f, 1f, 0.78f, "Greed   Rank 2/5", 34f, TextAlignmentOptions.MidlineLeft, ThemedElement.TextRole.Accent);
            Text("Body", texts, 0f, 0.42f, 1f, 0.6f, "Gains 10% more Gold per rank (max +50%).", 28f, TextAlignmentOptions.MidlineLeft, ThemedElement.TextRole.Body);
            Text("Soft", texts, 0f, 0.27f, 1f, 0.42f, "Reach level 80 with Corvan to unlock.", 22f, TextAlignmentOptions.MidlineLeft, ThemedElement.TextRole.Soft);
            Text("Danger", texts, 0f, 0.12f, 1f, 0.27f, "Price: 1,250  (need 240 more gold)", 24f, TextAlignmentOptions.MidlineLeft, ThemedElement.TextRole.Danger);
            Text("Numbers", texts, 0f, 0f, 1f, 0.12f, "Max Health 133   Might +35%   Cooldown -5%", 22f, TextAlignmentOptions.MidlineLeft, ThemedElement.TextRole.Body);

            // Column 3: cards
            Heading(window, 0.61f, 0.77f, "Cards");
            var icons = ItemIcons();
            var levelUp = Card(window, 0.61f, 0.61f, 0.98f, 0.76f, ThemedElement.SurfaceRole.Card, IconOf(UpgradeType.Whip), "Whip", "+1 projectile. Lashes the nearest enemy.", "Level 2");
            string[] states = { "Might\n2/5", "Armor\n0/3", "Recovery\nMAX", "Luck\nLOCKED" };
            UpgradeType[] cellIcons = { UpgradeType.Spinach, UpgradeType.Armor, UpgradeType.Pummarola, UpgradeType.Clover };
            ThemedElement.SurfaceRole[] roles = { ThemedElement.SurfaceRole.Card, ThemedElement.SurfaceRole.CardSelected, ThemedElement.SurfaceRole.CardMaxed, ThemedElement.SurfaceRole.CardLocked };
            for (int i = 0; i < 4; i++)
            {
                float x0 = 0.61f + i * 0.0935f;
                var cell = Cell(window, x0, 0.47f, x0 + 0.0885f, 0.595f, roles[i], IconOf(cellIcons[i]), states[i], i == 3);
                if (i == 1) SelectedFrame(cell);
            }

            // Info box with price and actions
            var info = Rect("InfoBox", window, 0.61f, 0.225f, 0.98f, 0.455f);
            UITheme.ApplySurface(info.gameObject.AddComponent<Image>(), ThemedElement.SurfaceRole.Row);
            var big = Icon("BigIcon", info, IconOf(UpgradeType.Pummarola), 120f);
            big.rectTransform.anchorMin = big.rectTransform.anchorMax = new Vector2(0.12f, 0.62f);
            Text("Name", info, 0.24f, 0.76f, 0.98f, 0.97f, "Recovery", 36f, TextAlignmentOptions.MidlineLeft, ThemedElement.TextRole.Accent);
            Text("Desc", info, 0.24f, 0.57f, 0.98f, 0.76f, "Recovers 0.1 HP per second per rank.", 24f, TextAlignmentOptions.MidlineLeft, ThemedElement.TextRole.Body);
            var price = Icon("PriceCoin", info, UITheme.IconFor(UITheme.IconSlot.Coin), 34f);
            price.rectTransform.anchorMin = price.rectTransform.anchorMax = new Vector2(0.27f, 0.46f);
            Text("Price", info, 0.3f, 0.37f, 0.98f, 0.55f, "Price: 422", 28f, TextAlignmentOptions.MidlineLeft, ThemedElement.TextRole.Gold);
            var actions = Rect("Actions", Rect("ActionsArea", info, 0.02f, 0.03f, 0.98f, 0.33f), 0.5f, 0f, 0.5f, 1f);
            ButtonAt(actions, 0.27f, 0.5f, "Buy", UITheme.ButtonKind.Primary, UITheme.IconSlot.Buy, ThemedElement.SizeRole.Standard);
            ButtonAt(actions, 0.73f, 0.5f, "Refund All", UITheme.ButtonKind.Danger, UITheme.IconSlot.None, ThemedElement.SizeRole.Standard);
            actions.gameObject.AddComponent<ThemedRow>(); // the theme's gap between the two buttons

            // HUD bits and the icon gallery
            var hud = Rect("Hud", window, 0.61f, 0.135f, 0.98f, 0.205f);
            HudLabel(hud, 0f, 0.3f, "Total Kills: 431");
            HudLabel(hud, 0.33f, 0.6f, "Gold: 176");
            HudLabel(hud, 0.66f, 0.98f, "Time: 01:14");
            var gallery = Rect("Gallery", window, 0.61f, 0.025f, 0.98f, 0.125f);
            int count = Mathf.Min(icons.Count, 16);
            for (int i = 0; i < count; i++)
            {
                var slot = Rect("Slot" + i, gallery, i / (float)count + 0.002f, 0.05f, (i + 1) / (float)count - 0.002f, 0.95f);
                UITheme.ApplySurface(slot.gameObject.AddComponent<Image>(), ThemedElement.SurfaceRole.SlotFilled);
                var im = Icon("Icon", slot, icons[i], 0f);
                im.rectTransform.anchorMin = new Vector2(0.08f, 0.08f);
                im.rectTransform.anchorMax = new Vector2(0.92f, 0.92f);
                im.rectTransform.sizeDelta = Vector2.zero;
            }
            _ = title; _ = levelUp; _ = toggleSwitch;
        }

        // ---------------------------------------------------------------- pieces

        private static List<Sprite> ItemIcons()
        {
            var list = new List<Sprite>();
            var catalog = GameContentCatalog.Load();
            if (catalog?.WeaponDefinitions != null)
                foreach (var d in catalog.WeaponDefinitions)
                    if (d != null && d.Icon != null && !d.Icon.name.StartsWith("SH10_Icon") && !list.Contains(d.Icon)) list.Add(d.Icon);
            return list;
        }

        private static Sprite IconOf(UpgradeType type) => WeaponDefinitionLookup.Find(type)?.Icon;

        private static RectTransform Rect(string name, Transform parent, float x0, float y0, float x1, float y1)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, float x0, float y0, float x1, float y1, string text, float size,
            TextAlignmentOptions alignment, ThemedElement.TextRole role)
        {
            var rect = Rect(name, parent, x0, y0, x1, y1);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = size;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = size;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.text = text;
            label.raycastTarget = false;
            UITheme.ApplyText(label, role);
            return label;
        }

        private static void Heading(Transform parent, float x, float y, string text) =>
            Text("H_" + text, parent, x, y, x + 0.34f, y + 0.045f, text, 26f, TextAlignmentOptions.MidlineLeft, ThemedElement.TextRole.Dim);

        private static Image Icon(string name, Transform parent, Sprite sprite, float size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(size, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.enabled = sprite != null;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static Button AddButton(RectTransform column, float y, string label, UITheme.ButtonKind kind, UITheme.IconSlot icon) =>
            ButtonAt(column, 0.5f, y, label, kind, icon, ThemedElement.SizeRole.Standard);

        private static Button ButtonAt(RectTransform parent, float x, float y, string label, UITheme.ButtonKind kind, UITheme.IconSlot icon,
            ThemedElement.SizeRole size)
        {
            var rect = Rect("Button_" + label, parent, x, y, x, y);
            var image = rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = Text("Label", rect, 0f, 0f, 1f, 1f, label, 34f, TextAlignmentOptions.Center, ThemedElement.TextRole.Body);
            UITheme.ApplyButton(button, image, kind, icon, true, size);
            return button;
        }

        private static void Chip(RectTransform parent, float x, string label, bool on)
        {
            var rect = Rect("Chip_" + label, parent, x, 0.5f, x, 0.5f);
            var image = rect.gameObject.AddComponent<Image>();
            var toggle = rect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = image;
            var box = Rect("Box", rect, 0f, 0f, 0f, 0f);
            UITheme.ApplySurface(box.gameObject.AddComponent<Image>(), ThemedElement.SurfaceRole.TickBox);
            var check = Rect("Check", box, 0.14f, 0.14f, 0.86f, 0.86f);
            var checkImage = check.gameObject.AddComponent<Image>();
            checkImage.raycastTarget = false;
            UITheme.ApplySurface(checkImage, ThemedElement.SurfaceRole.Tick);
            toggle.graphic = checkImage;
            toggle.SetIsOnWithoutNotify(on);
            Text("Label", rect, 0f, 0f, 1f, 1f, label, 26f, TextAlignmentOptions.Center, ThemedElement.TextRole.Body);
            UITheme.ApplyButton(toggle, image, UITheme.ButtonKind.Normal, UITheme.IconSlot.None, true, ThemedElement.SizeRole.Tab, box);
        }

        private static RectTransform Switch(RectTransform row, float x, bool on)
        {
            var track = Rect("Switch", row, x, 0.5f, x, 0.5f);
            track.sizeDelta = new Vector2(132f, 52f);
            var image = track.gameObject.AddComponent<Image>();
            var toggle = track.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = image;
            toggle.SetIsOnWithoutNotify(on);
            UITheme.ApplySurface(image, on ? ThemedElement.SurfaceRole.SwitchOn : ThemedElement.SurfaceRole.SwitchOff);
            var knob = Rect("Knob", track, on ? 1f : 0f, 0.5f, on ? 1f : 0f, 0.5f);
            knob.sizeDelta = new Vector2(44f, 44f);
            knob.anchoredPosition = new Vector2(on ? -28f : 28f, 0f);
            UITheme.ApplySurface(knob.gameObject.AddComponent<Image>(), ThemedElement.SurfaceRole.Knob);
            Text("Word", track, on ? 0.08f : 0.42f, 0f, on ? 0.6f : 0.94f, 1f, on ? "ON" : "OFF", 24f, TextAlignmentOptions.Center,
                ThemedElement.TextRole.Body);
            return track;
        }

        private static RectTransform Row(Transform parent, float x0, float y0, float x1, float y1, string label)
        {
            var row = Rect("Row_" + label, parent, x0, y0, x1, y1);
            UITheme.ApplySurface(row.gameObject.AddComponent<Image>(), ThemedElement.SurfaceRole.Row);
            Text("Label", row, 0.04f, 0f, 0.5f, 1f, label, 28f, TextAlignmentOptions.MidlineLeft, ThemedElement.TextRole.Body);
            return row;
        }

        private static void Slider(RectTransform row, float value)
        {
            var area = Rect("Slider", row, 0.5f, 0.38f, 0.82f, 0.62f);
            var back = area.gameObject.AddComponent<Image>();
            UITheme.ApplySurface(back, ThemedElement.SurfaceRole.Groove);
            var fill = Rect("Fill", area, 0.02f, 0.18f, value, 0.82f);
            UITheme.ApplySurface(fill.gameObject.AddComponent<Image>(), ThemedElement.SurfaceRole.Accent);
            var handle = Rect("Handle", area, value, 0.5f, value, 0.5f);
            handle.sizeDelta = new Vector2(44f, 44f);
            UITheme.ApplySurface(handle.gameObject.AddComponent<Image>(), ThemedElement.SurfaceRole.Knob);
            Text("Value", row, 0.84f, 0f, 0.97f, 1f, $"{value * 100f:0}%", 26f, TextAlignmentOptions.MidlineRight, ThemedElement.TextRole.Body);
        }

        private static RectTransform Card(Transform parent, float x0, float y0, float x1, float y1, ThemedElement.SurfaceRole role,
            Sprite icon, string name, string desc, string tag)
        {
            var card = Rect("Card_" + name, parent, x0, y0, x1, y1);
            UITheme.ApplySurface(card.gameObject.AddComponent<Image>(), role);
            var im = Icon("Icon", card, icon, 96f);
            im.rectTransform.anchorMin = im.rectTransform.anchorMax = new Vector2(0.09f, 0.5f);
            Text("Name", card, 0.19f, 0.52f, 0.75f, 0.9f, name, 36f, TextAlignmentOptions.MidlineLeft, ThemedElement.TextRole.Card);
            Text("Desc", card, 0.19f, 0.12f, 0.97f, 0.5f, desc, 24f, TextAlignmentOptions.MidlineLeft, ThemedElement.TextRole.CardSoft);
            Text("Tag", card, 0.75f, 0.52f, 0.97f, 0.9f, tag, 26f, TextAlignmentOptions.MidlineRight, ThemedElement.TextRole.Tag);
            return card;
        }

        private static RectTransform Cell(Transform parent, float x0, float y0, float x1, float y1, ThemedElement.SurfaceRole role,
            Sprite icon, string text, bool locked)
        {
            var cell = Rect("Cell_" + text.Split('\n')[0], parent, x0, y0, x1, y1);
            UITheme.ApplySurface(cell.gameObject.AddComponent<Image>(), role);
            var im = Icon("Icon", cell, icon, 64f);
            im.rectTransform.anchorMin = im.rectTransform.anchorMax = new Vector2(0.5f, 0.68f);
            if (locked) im.color = new Color(1f, 1f, 1f, 0.4f);
            var label = Text("Label", cell, 0.06f, 0.04f, 0.94f, 0.4f, text, 24f, TextAlignmentOptions.Center,
                locked ? ThemedElement.TextRole.Soft : ThemedElement.TextRole.Card);
            label.textWrappingMode = TextWrappingModes.Normal;
            return cell;
        }

        private static void SelectedFrame(RectTransform cell)
        {
            var outline = cell.gameObject.AddComponent<Outline>();
            var cards = UiThemeAsset.Active.Cards;
            outline.effectColor = cards.SelectedEdge;
            outline.effectDistance = new Vector2(cards.SelectedEdgeWidth, -cards.SelectedEdgeWidth);
        }

        private static void HudLabel(Transform parent, float x0, float x1, string text)
        {
            var back = Rect("Hud_" + text, parent, x0, 0f, x1, 1f);
            UITheme.ApplySurface(back.gameObject.AddComponent<Image>(), ThemedElement.SurfaceRole.HudBacking);
            Text("Label", back, 0.05f, 0f, 0.95f, 1f, text, 30f, TextAlignmentOptions.Center, ThemedElement.TextRole.HudLabel);
        }

        private void OnDestroy()
        {
            if (_open == this) { UiThemeAsset.SetActive(_previous); _open = null; }
        }
    }
}
