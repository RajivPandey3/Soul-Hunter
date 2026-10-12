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
    /// The VS Bestiary screen (release item P3-02, 2026-10-09; wiki Ars_Gouda, see BestiaryCatalog): a header with the
    /// unique enemies defeated out of all, a numbered list on the left (an unknown enemy's name is "---------------") and
    /// the chosen enemy's details on the right: defeats, where it appears, HP, Power, Speed, resistances and skills, each
    /// "?" until the first defeat, with "???" for its entry. It opens on the first enemy, as VS's does. Its main menu button
    /// shows once the Bestiary is unlocked (Bestiary.UnlockLevel).
    /// </summary>
    public sealed class BestiaryMenuUI : MenuSectionScreen
    {
        public const string UnknownEntry = "???";

        private readonly List<(EnemyData Enemy, Button Row, TextMeshProUGUI Label)> _rows = new List<(EnemyData, Button, TextMeshProUGUI)>();
        private TextMeshProUGUI _name, _details;
        private EnemyData _selected;

        public IReadOnlyList<(EnemyData Enemy, Button Row, TextMeshProUGUI Label)> Rows => _rows;
        public EnemyData Selected => _selected;
        public string InfoName => _name != null ? _name.text : null;
        public string InfoDetails => _details != null ? _details.text : null;

        /// <summary>A list entry's design height (canvas units); never under the touch target on a touch screen.</summary>
        public const float RowHeight = 60f;

        /// <summary>The list beside the details on a wide screen, above them on a tall one.</summary>
        public AdaptiveSplit Split { get; private set; }

        protected override void Build()
        {
            // Responsive (owner, 2026-10-10): the numbered list and the chosen enemy's details share Body through
            // AdaptiveSplit; both scroll.
            var listArea = NewRect("BestiaryListArea", Body);
            var infoArea = NewRect("BestiaryInfoArea", Body);
            Split = AdaptiveSplit.Between(Body, listArea, infoArea, 0.45f, 0.5f);
            var content = MakeScroll("BestiaryList", listArea, 0f, 0f, 1f, 1f, out _);
            Column(content, 8f);
            int number = 0;
            foreach (var enemy in BestiaryCatalog.All())
            {
                number++;
                var go = new GameObject($"Enemy_{number:000}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                go.transform.SetParent(content, false);
                go.GetComponent<LayoutElement>().preferredHeight = TouchHeight(RowHeight);
                var button = go.GetComponent<Button>();
                UITheme.ApplyButton(button, go.GetComponent<Image>(), UITheme.ButtonKind.Card, UITheme.IconSlot.None, false, ThemedElement.SizeRole.Free);
                var label = EntryText((RectTransform)go.transform, "Label", 28f, TextAlignmentOptions.MidlineLeft, ThemedElement.TextRole.Card, () => UITheme.Cell);
                label.rectTransform.offsetMin = new Vector2(18f, 4f);
                label.rectTransform.offsetMax = new Vector2(-12f, -4f);
                var captured = enemy;
                button.onClick.AddListener(() => Select(captured));
                go.AddComponent<FocusCallback>().Selected = () => Select(captured);
                _rows.Add((enemy, button, label));
            }

            var box = MakeBox("BestiaryInfo", infoArea, 0f, 0f, 1f, 1f);
            var info = MakeScroll("BestiaryInfoText", box, 0f, 0f, 1f, 1f, out _);
            Column(info, 12f, new RectOffset(22, 12, 16, 16));
            _name = FlowText(info, "Name", 42f, TextAlignmentOptions.MidlineLeft, ThemedElement.TextRole.Accent, () => UITheme.Row);
            _details = FlowText(info, "Details", 30f, TextAlignmentOptions.TopLeft, ThemedElement.TextRole.Body, () => UITheme.Row);
            _details.lineSpacing = 12f;
        }

        protected override void OnRelayout()
        {
            // A touch screen's entries are at least the 48 dp touch target.
            foreach (var row in _rows)
            {
                var element = row.Row != null ? row.Row.GetComponent<LayoutElement>() : null;
                if (element != null) element.preferredHeight = TouchHeight(RowHeight);
            }
        }

        public override void Refresh()
        {
            var data = Save;
            var theme = UiThemeAsset.Active;
            int number = 0, known = 0;
            foreach (var row in _rows)
            {
                number++;
                bool isKnown = Bestiary.IsKnown(data, BestiaryCatalog.IdOf(row.Enemy));
                if (isKnown) known++;
                row.Label.text = $"{number:000}   {(isKnown ? BestiaryCatalog.Name(row.Enemy) : BestiaryCatalog.UnknownName)}";
                bool chosen = row.Enemy == _selected;
                UITheme.Edge(row.Row.gameObject, chosen ? theme.Cards.SelectedEdge : UITheme.PanelEdge, chosen ? theme.Cards.SelectedEdgeWidth : 1.5f);
            }
            if (CountText != null) CountText.text = $"Defeated {known} / {_rows.Count}";
            if (_selected == null && _rows.Count > 0) { Select(_rows[0].Enemy); return; }
            ShowInfo(data);
        }

        public void Select(EnemyData enemy)
        {
            _selected = enemy;
            Refresh();
        }

        private void ShowInfo(GameData data)
        {
            if (_selected == null || _name == null) return;
            string id = BestiaryCatalog.IdOf(_selected);
            int kills = Bestiary.Kills(data, id);
            bool known = kills > 0;
            string Value(string v) => known ? v : BestiaryCatalog.UnknownValue;
            _name.text = known ? UITheme.Bold(BestiaryCatalog.Name(_selected)) : BestiaryCatalog.UnknownName;
            _details.text =
                $"Defeated: {kills}\n" +
                $"Appears in: {Value(BestiaryCatalog.Stages(_selected))}\n" +
                $"HP: {Value(BestiaryCatalog.Health(_selected))}\n" +
                $"Power: {Value(BestiaryCatalog.Power(_selected))}\n" +
                $"Speed: {Value(BestiaryCatalog.Speed(_selected))}\n" +
                $"Resistances: {Value(BestiaryCatalog.Resistances(_selected))}\n" +
                $"Skills: {Value(BestiaryCatalog.Skills(_selected))}" +
                (known ? "" : "\n\n" + UnknownEntry);
        }
    }
}
