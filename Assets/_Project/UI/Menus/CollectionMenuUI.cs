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

        /// <summary>The narrowest a grid cell may be (canvas units): the column count follows the width (GridFit).</summary>
        public const float MinCellWidth = 96f;

        /// <summary>The grid beside the info box on a wide screen, above it on a tall one.</summary>
        public AdaptiveSplit Split { get; private set; }
        public GridLayoutGroup Grid { get; private set; }

        protected override void Build()
        {
            // Responsive (owner, 2026-10-10): the grid and the info box share Body through AdaptiveSplit, the grid takes as
            // many columns as its width allows (GridFit, square cells, never under the touch target) and scrolls.
            var gridArea = NewRect("CollectionGridArea", Body);
            var infoArea = NewRect("CollectionInfoArea", Body);
            Split = AdaptiveSplit.Between(Body, gridArea, infoArea, 0.64f, 0.6f);
            var content = MakeScroll("CollectionGrid", gridArea, 0f, 0f, 1f, 1f, out var scroll);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            grid.spacing = new Vector2(10f, 10f);
            grid.cellSize = new Vector2(112f, 112f);
            grid.childAlignment = TextAnchor.UpperLeft;
            Grid = grid;
            var fit = content.gameObject.AddComponent<GridFit>();
            fit.Grid = grid;
            fit.Viewport = scroll.viewport;
            fit.MinCellWidth = MinCellWidth;
            fit.MaxColumns = Columns;
            fit.MinHeightRatio = 1f; // square cells

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
            fit.Count = _cells.Count;

            // The info box: the chosen entry's picture, name, kind and description, stacked; a long one scrolls.
            var box = MakeBox("CollectionInfo", infoArea, 0f, 0f, 1f, 1f);
            var info = MakeScroll("CollectionInfoText", box, 0f, 0f, 1f, 1f, out _);
            Column(info, 10f, new RectOffset(20, 12, 16, 16));
            _bigIcon = MakeImage("BigIcon", info, null);
            var iconSize = _bigIcon.gameObject.AddComponent<LayoutElement>();
            iconSize.minHeight = iconSize.preferredHeight = BigIconHeight;
            _name = FlowText(info, "Name", 40f, TextAlignmentOptions.Center, ThemedElement.TextRole.Accent, () => UITheme.Row);
            _kind = FlowText(info, "Kind", 28f, TextAlignmentOptions.Center, ThemedElement.TextRole.Body, () => UITheme.Row);
            _description = FlowText(info, "Description", 30f, TextAlignmentOptions.Top, ThemedElement.TextRole.Body, () => UITheme.Row);
        }

        /// <summary>The chosen entry's picture height in the info box (canvas units).</summary>
        public const float BigIconHeight = 140f;

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
            _bigIcon.gameObject.SetActive(_bigIcon.enabled); // no empty gap above an unknown entry's name
            _name.text = known ? UITheme.Bold(_selected.Name) : Unknown;
            _kind.text = CollectionCatalog.KindName(_selected.Kind);
            _description.text = known ? (_selected.Description ?? "") : UnknownText;
        }
    }
}
