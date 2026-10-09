using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.UI
{
    /// <summary>
    /// The VS Collection screen (release item P3-02, 2026-10-09). vampire.survivors.wiki/w/Collection (raw, fetched
    /// 2026-10-09): it "lists all of the items that the player has used in their game so far"; "most items that have not
    /// been used yet are marked with a question mark, and clicking on a locked item slot will indicate that the item is
    /// unknown and not yet found"; "the background of the question mark slots is marked differently, depending on the type
    /// of the item". wiki Main_menu: "Available by default". A grid of every entry (CollectionCatalog): a found one shows
    /// its icon (or its name when it has none yet), an unknown one a "?"; a coloured strip under each cell marks its kind
    /// (theme colours), and the info box names the kind in words. The wiki does not say what a found entry shows; Soul
    /// Hunter shows its name, kind and description. Seals (VS's item disabling) are not built (no Seal PowerUp).
    /// </summary>
    public sealed class CollectionMenuUI : MenuSectionScreen
    {
        public const int Columns = 8;
        public const string Unknown = "?";
        public const string UnknownText = "Unknown. Not found yet.";

        private readonly List<(CollectionEntry Entry, Button Cell, Image Icon, TextMeshProUGUI Label, Image Strip)> _cells =
            new List<(CollectionEntry, Button, Image, TextMeshProUGUI, Image)>();
        private Image _bigIcon;
        private TextMeshProUGUI _name, _kind, _description;
        private CollectionEntry _selected;

        public IReadOnlyList<(CollectionEntry Entry, Button Cell, Image Icon, TextMeshProUGUI Label, Image Strip)> Cells => _cells;
        public CollectionEntry Selected => _selected;
        public string InfoName => _name != null ? _name.text : null;
        public string InfoDescription => _description != null ? _description.text : null;

        protected override void Build()
        {
            var content = MakeScroll("CollectionGrid", Panel, 0.05f, 0.08f, 0.62f, 0.82f, out _);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            grid.spacing = new Vector2(10f, 10f);
            grid.cellSize = new Vector2(112f, 112f);
            grid.childAlignment = TextAnchor.UpperLeft;
            content.gameObject.AddComponent<SquareCells>().Grid = grid;

            foreach (var entry in CollectionCatalog.All())
            {
                var go = new GameObject("Entry_" + entry.Id, typeof(RectTransform), typeof(Image), typeof(Button));
                var rect = (RectTransform)go.transform;
                rect.SetParent(content, false);
                var button = go.GetComponent<Button>();
                // A card: the theme's card colour and plate; its texts are themed on their own (labels: false).
                UITheme.ApplyButton(button, go.GetComponent<Image>(), UITheme.ButtonKind.Card, UITheme.IconSlot.None, false,
                    ThemedElement.SizeRole.Free);
                var icon = MakeImage("Icon", rect, null);
                Anchor(icon.rectTransform, 0.14f, 0.2f, 0.86f, 0.9f);
                var label = EntryText(rect, "Label", 40f, TextAlignmentOptions.Center, ThemedElement.TextRole.Card, () => UITheme.Cell);
                label.rectTransform.offsetMin = new Vector2(6f, 14f);
                label.rectTransform.offsetMax = new Vector2(-6f, -4f);
                label.textWrappingMode = TextWrappingModes.Normal;
                var strip = new GameObject("KindStrip", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                strip.rectTransform.SetParent(rect, false);
                Anchor(strip.rectTransform, 0.08f, 0.04f, 0.92f, 0.11f);
                strip.raycastTarget = false;
                var captured = entry;
                button.onClick.AddListener(() => Select(captured));
                go.AddComponent<FocusCallback>().Selected = () => Select(captured);
                _cells.Add((entry, button, icon, label, strip));
            }

            var box = MakeBox("CollectionInfo", Panel, 0.64f, 0.08f, 0.95f, 0.82f);
            _bigIcon = MakeImage("BigIcon", box, null);
            Anchor(_bigIcon.rectTransform, 0.3f, 0.66f, 0.7f, 0.95f);
            _name = MakeText("Name", box, 0.06f, 0.55f, 0.94f, 0.65f, "", 40f, TextAlignmentOptions.Center, ThemedElement.TextRole.Accent, () => UITheme.Row);
            _kind = MakeText("Kind", box, 0.06f, 0.48f, 0.94f, 0.55f, "", 28f, TextAlignmentOptions.Center, ThemedElement.TextRole.Body, () => UITheme.Row);
            _description = MakeText("Description", box, 0.08f, 0.06f, 0.92f, 0.46f, "", 30f, TextAlignmentOptions.Top, ThemedElement.TextRole.Body, () => UITheme.Row);
            _description.textWrappingMode = TextWrappingModes.Normal;
        }

        /// <summary>The theme colour that marks an entry's kind (wiki: each type has its own slot background).</summary>
        public static Color KindColour(CollectionKind kind) => kind switch
        {
            CollectionKind.Weapon => UITheme.Rarity.Rare,
            CollectionKind.Evolution => UITheme.Rewards.Evolution,
            CollectionKind.Passive => UITheme.Rarity.Epic,
            CollectionKind.Pickup => UITheme.Rarity.Good, // VS: "pickups are highlighted with a grassy background"
            CollectionKind.Relic => UITheme.Rarity.Legend,
            _ => UITheme.Rewards.Arcana
        };

        public override void Refresh()
        {
            var data = Save;
            var theme = UiThemeAsset.Active;
            int found = 0;
            foreach (var cell in _cells)
            {
                bool known = CollectionBook.IsFound(data, cell.Entry.Id);
                if (known) found++;
                cell.Icon.sprite = known ? cell.Entry.Icon : null;
                cell.Icon.enabled = known && cell.Entry.Icon != null;
                // Unknown: a "?". Found without a picture yet: its name, small, so the cell is never blank.
                cell.Label.text = !known ? Unknown : cell.Entry.Icon == null ? cell.Entry.Name : "";
                cell.Label.fontSizeMax = known ? 22f : 52f;
                cell.Strip.color = KindColour(cell.Entry.Kind);
                bool chosen = cell.Entry == _selected;
                UITheme.Edge(cell.Cell.gameObject, chosen ? theme.Cards.SelectedEdge : UITheme.PanelEdge, chosen ? theme.Cards.SelectedEdgeWidth : 1.5f);
            }
            if (CountText != null) CountText.text = $"Found {found} / {_cells.Count}";
            if (_selected == null && _cells.Count > 0) { Select(_cells[0].Entry); return; }
            ShowInfo(data);
        }

        public void Select(CollectionEntry entry)
        {
            _selected = entry;
            Refresh();
        }

        private void ShowInfo(GameData data)
        {
            if (_selected == null || _name == null) return;
            bool known = CollectionBook.IsFound(data, _selected.Id);
            _bigIcon.sprite = known ? _selected.Icon : null;
            _bigIcon.enabled = known && _selected.Icon != null;
            _name.text = known ? UITheme.Bold(_selected.Name) : Unknown;
            _kind.text = CollectionCatalog.KindName(_selected.Kind);
            _description.text = known ? (_selected.Description ?? "") : UnknownText;
        }
    }

    /// <summary>Keeps a grid's cells square and filling its width (Columns across), whatever the window size.</summary>
    public sealed class SquareCells : MonoBehaviour
    {
        public GridLayoutGroup Grid;
        private float _lastWidth;

        private void LateUpdate()
        {
            if (Grid == null) return;
            var rect = (RectTransform)transform;
            float width = rect.rect.width;
            if (width <= 0f || Mathf.Abs(width - _lastWidth) < 0.5f) return;
            _lastWidth = width;
            int columns = Mathf.Max(1, Grid.constraintCount);
            float size = Mathf.Floor((width - Grid.spacing.x * (columns - 1)) / columns);
            Grid.cellSize = new Vector2(size, size);
        }
    }
}
