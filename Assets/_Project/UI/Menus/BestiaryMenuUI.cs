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

        protected override void Build()
        {
            var content = MakeScroll("BestiaryList", Panel, 0.05f, 0.08f, 0.45f, 0.82f, out _);
            var list = content.gameObject.AddComponent<VerticalLayoutGroup>();
            list.spacing = 8f;
            list.childControlWidth = list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;
            int number = 0;
            foreach (var enemy in BestiaryCatalog.All())
            {
                number++;
                var go = new GameObject($"Enemy_{number:000}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                go.transform.SetParent(content, false);
                go.GetComponent<LayoutElement>().preferredHeight = 60f;
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

            var box = MakeBox("BestiaryInfo", Panel, 0.47f, 0.08f, 0.95f, 0.82f);
            _name = MakeText("Name", box, 0.05f, 0.84f, 0.95f, 0.96f, "", 42f, TextAlignmentOptions.MidlineLeft, ThemedElement.TextRole.Accent, () => UITheme.Row);
            _details = MakeText("Details", box, 0.05f, 0.04f, 0.95f, 0.82f, "", 30f, TextAlignmentOptions.TopLeft, ThemedElement.TextRole.Body, () => UITheme.Row);
            _details.textWrappingMode = TextWrappingModes.Normal;
            _details.lineSpacing = 12f;
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
