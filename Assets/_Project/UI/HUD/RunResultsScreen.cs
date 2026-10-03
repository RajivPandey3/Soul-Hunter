using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SoulHunter.Gameplay.Core;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// The VS Results screen (vampire.survivors.wiki/w/Results, fetched 2026-10-02), in Soul Hunter's own look: top left
    /// the stage, mode, gold bonus, time survived, gold, level and kills; bottom left the weapon table (level, damage,
    /// time used, DPS; the most damage and the most DPS in yellow; damage with SI prefixes); right the character and Soul
    /// Eggs, the items picked up (levels yellow when max), the PowerUps, the pickups and the Arcanas. A green button shows
    /// or hides the run's unlocks when there are any, and DONE goes back to the main menu. The layout is Soul Hunter's;
    /// the wiki describes only what each corner holds.
    /// </summary>
    public sealed class RunResultsScreen : MonoBehaviour
    {
        public const int SortingOrder = 450;
        public static readonly Color Yellow = new Color(1f, 0.84f, 0.29f, 1f); // the VS yellow rows (kept distinct from the theme gold)
        private static readonly Color Backing = UITheme.Panel;
        private static readonly Color Green = new Color(0.16f, 0.55f, 0.24f, 1f);

        public RunResults Results { get; private set; }
        public Button DoneButton { get; private set; }
        public Button UnlocksButton { get; private set; }
        public GameObject UnlocksPanel { get; private set; }
        public TextMeshProUGUI TitleText { get; private set; }
        public TextMeshProUGUI SummaryText { get; private set; }
        public TextMeshProUGUI ListsText { get; private set; }
        /// <summary>One level label per item icon, in the items order (yellow when max).</summary>
        public readonly List<TextMeshProUGUI> ItemLevelTexts = new List<TextMeshProUGUI>();
        public event System.Action Done;

        private TMP_FontAsset _font;
        private RectTransform _root;

        /// <summary>Builds the screen on its own overlay canvas under <paramref name="owner"/>.</summary>
        public static RunResultsScreen Show(Transform owner, RunResults results, TMP_FontAsset font)
        {
            var go = new GameObject("RunResultsCanvas", typeof(RectTransform));
            go.transform.SetParent(owner, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            // Expand: the whole layout fits any window (the chest screen lost its OPEN button in a 21:9 window).
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            go.AddComponent<GraphicRaycaster>();
            var screen = go.AddComponent<RunResultsScreen>();
            screen._font = font;
            screen.Build(results);
            return screen;
        }

        private void Build(RunResults results)
        {
            Results = results;
            _root = (RectTransform)transform;
            // Opaque: the UI blends in linear space, so even a 0.97 dark layer let the game and the HUD show through.
            var backdrop = Panel(_root, "Backdrop", Vector2.zero, Vector2.one, new Color(0.012f, 0.008f, 0.01f, 1f));
            backdrop.GetComponent<Image>().raycastTarget = true;

            TitleText = Text(_root, "Title", new Vector2(0.2f, 0.9f), new Vector2(0.8f, 0.98f), TitleLine(results), 54f, TextAlignmentOptions.Center);
            TitleText.color = UITheme.Accent;
            TitleText.fontStyle = FontStyles.Bold;

            var summary = Panel(_root, "Summary", new Vector2(0.025f, 0.6f), new Vector2(0.535f, 0.885f), Backing);
            SummaryText = Text(summary, "SummaryText", new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), SummaryLines(results), 30f, TextAlignmentOptions.TopLeft);

            var table = Panel(_root, "Weapons", new Vector2(0.025f, 0.13f), new Vector2(0.535f, 0.585f), Backing);
            BuildWeaponTable(table, results);

            var side = Panel(_root, "Character", new Vector2(0.55f, 0.13f), new Vector2(0.975f, 0.885f), Backing);
            BuildSide(side, results);

            DoneButton = MakeButton(_root, "Done_Button", new Vector2(0.42f, 0.025f), new Vector2(0.58f, 0.105f), "DONE", null);
            DoneButton.onClick.AddListener(() => Done?.Invoke());

            if (results.Unlocks.Count > 0)
            {
                UnlocksButton = MakeButton(_root, "Unlocks_Button", new Vector2(0.2f, 0.025f), new Vector2(0.4f, 0.105f),
                    $"SHOW UNLOCKS ({results.Unlocks.Count})", Green);
                UnlocksButton.onClick.AddListener(ToggleUnlocks);
                UnlocksPanel = Panel(_root, "UnlocksPanel", new Vector2(0.25f, 0.2f), new Vector2(0.75f, 0.8f), new Color(0.04f, 0.1f, 0.06f, 0.97f)).gameObject;
                Text((RectTransform)UnlocksPanel.transform, "UnlocksText", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f),
                    "<color=#FFD64A><b>UNLOCKED THIS RUN</b></color>\n\n" + string.Join("\n", results.Unlocks), 32f, TextAlignmentOptions.Top);
                UnlocksPanel.SetActive(false);
            }
            Focus(DoneButton);
        }

        private static string TitleLine(RunResults results) => results.End switch
        {
            RunResults.EndKind.Victory => "<color=#FFD64A>SHADOW KAEL DESTROYED</color>",
            RunResults.EndKind.Death => "GAME OVER",
            _ => "RESULTS"
        };

        private static string SummaryLines(RunResults results)
        {
            var text = new StringBuilder();
            if (results.End == RunResults.EndKind.Victory)
                // The owner's story, word for word (Docs/Game_Design_Document.md).
                text.Append("<i>").Append(SoulHunter.Gameplay.Data.CampaignStory.FinalLine).Append("</i>\n");
            text.Append("<b>").Append(results.StageLine).Append("</b>\n");
            text.Append("Mode: ").Append(results.Mode).Append("     Gold bonus: ")
                .Append(results.GoldBonusPercent >= 0 ? "+" : "").Append(results.GoldBonusPercent).Append("%\n");
            text.Append("Survived: <color=#FFD64A>").Append(RunResultsFormat.Time(results.SecondsSurvived)).Append("</color>\n");
            text.Append("Gold earned: <color=#FFD64A>").Append(results.Gold).Append("</color>\n");
            text.Append("Level reached: <color=#FFD64A>").Append(results.Level).Append("</color>\n");
            text.Append("Enemies defeated: <color=#FFD64A>").Append(results.Kills).Append("</color>");
            return text.ToString();
        }

        /// <summary>One weapon table row: its name, level, damage, time used and DPS cells.</summary>
        public sealed class WeaponRowView
        {
            public TextMeshProUGUI Name, Level, Damage, Time, Dps;
        }

        /// <summary>The weapon table's rows, in the results' weapon order.</summary>
        public readonly List<WeaponRowView> WeaponRows = new List<WeaponRowView>();

        // Column left edges of the weapon table (fractions of its width): icon, name, level, damage, time, DPS.
        private static readonly float[] Columns = { 0.02f, 0.085f, 0.5f, 0.6f, 0.76f, 0.88f, 0.99f };

        private void BuildWeaponTable(RectTransform table, RunResults results)
        {
            int rows = results.Weapons.Count + 1;
            float rowHeight = Mathf.Min(0.085f, 0.94f / Mathf.Max(1, rows));
            float top = 0.97f;
            var header = Row(table, "Header", top, rowHeight);
            Cell(header, 1, "WEAPON", TextAlignmentOptions.Left).color = UITheme.TextDim;
            Cell(header, 2, "LV", TextAlignmentOptions.Center).color = UITheme.TextDim;
            Cell(header, 3, "DAMAGE", TextAlignmentOptions.Right).color = UITheme.TextDim;
            Cell(header, 4, "TIME", TextAlignmentOptions.Right).color = UITheme.TextDim;
            Cell(header, 5, "DPS", TextAlignmentOptions.Right).color = UITheme.TextDim;
            for (int i = 0; i < results.Weapons.Count; i++)
            {
                var data = results.Weapons[i];
                var row = Row(table, "Row_" + data.Name, top - rowHeight * (i + 1), rowHeight);
                if (data.Icon != null)
                {
                    var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    var rect = (RectTransform)icon.transform;
                    rect.SetParent(row, false);
                    rect.anchorMin = new Vector2(Columns[0], 0.08f);
                    rect.anchorMax = new Vector2(Columns[1] - 0.01f, 0.92f);
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    var image = icon.GetComponent<Image>();
                    image.sprite = data.Icon;
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                }
                var view = new WeaponRowView
                {
                    Name = Cell(row, 1, data.Name, TextAlignmentOptions.Left),
                    Level = Cell(row, 2, data.Level > 0 ? data.Level.ToString() : "-", TextAlignmentOptions.Center),
                    Damage = Cell(row, 3, RunResultsFormat.Damage(data.Damage), TextAlignmentOptions.Right),
                    Time = Cell(row, 4, RunResultsFormat.Time(data.SecondsUsed), TextAlignmentOptions.Right),
                    Dps = Cell(row, 5, RunResultsFormat.Damage(data.Dps), TextAlignmentOptions.Right)
                };
                if (data.Maxed) view.Level.color = Yellow;
                if (i == results.TopDamageRow) view.Damage.color = Yellow;
                if (i == results.TopDpsRow) view.Dps.color = Yellow;
                WeaponRows.Add(view);
            }
            if (results.Weapons.Count == 0)
                Cell(Row(table, "Empty", top - rowHeight, rowHeight), 1, "No damage dealt.", TextAlignmentOptions.Left);
        }

        private static RectTransform Row(RectTransform table, string name, float top, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(table, false);
            rect.anchorMin = new Vector2(0f, top - height);
            rect.anchorMax = new Vector2(1f, top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private TextMeshProUGUI Cell(RectTransform row, int column, string content, TextAlignmentOptions align)
        {
            var cell = Text(row, "Col" + column, new Vector2(Columns[column], 0f), new Vector2(Columns[column + 1] - 0.01f, 1f), content, 26f, align);
            cell.enableWordWrapping = false;
            cell.overflowMode = TextOverflowModes.Ellipsis;
            return cell;
        }

        private void BuildSide(RectTransform side, RunResults results)
        {
            Text(side, "CharacterText", new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.97f),
                $"<b>{results.Character}</b>" + (results.SoulEggs > 0 ? $"     <color=#FFD64A>Soul Eggs: {results.SoulEggs}</color>" : ""),
                34f, TextAlignmentOptions.Left);

            // Items: icons in rows of 8, each with its level (yellow when max).
            var grid = new GameObject("Items", typeof(RectTransform));
            var gridRect = (RectTransform)grid.transform;
            gridRect.SetParent(side, false);
            gridRect.anchorMin = new Vector2(0.04f, 0.6f);
            gridRect.anchorMax = new Vector2(0.96f, 0.87f);
            gridRect.offsetMin = gridRect.offsetMax = Vector2.zero;
            var layout = grid.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(76f, 86f);
            layout.spacing = new Vector2(10f, 8f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 9;
            foreach (var item in results.Items)
            {
                var cell = new GameObject(item.Name, typeof(RectTransform), typeof(Image));
                cell.transform.SetParent(gridRect, false);
                var frame = cell.GetComponent<Image>();
                frame.color = item.IsWeapon ? UITheme.Cell : UITheme.Row;
                UITheme.Edge(cell, UITheme.PanelEdge, 1.5f);
                frame.raycastTarget = false;
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                var iconRect = (RectTransform)icon.transform;
                iconRect.SetParent(cell.transform, false);
                iconRect.anchorMin = new Vector2(0.1f, 0.3f);
                iconRect.anchorMax = new Vector2(0.9f, 0.95f);
                iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
                var iconImage = icon.GetComponent<Image>();
                iconImage.sprite = item.Icon;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                if (item.Icon == null) iconImage.color = new Color(1f, 1f, 1f, 0f);
                var level = Text((RectTransform)cell.transform, "Level", new Vector2(0f, 0f), new Vector2(1f, 0.3f),
                    (item.Level > 0 ? "Lv " + item.Level : "-"), 20f, TextAlignmentOptions.Center);
                level.color = item.Maxed ? Yellow : Color.white;
                ItemLevelTexts.Add(level);
            }

            ListsText = Text(side, "ListsText", new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.58f), Lists(results), 24f, TextAlignmentOptions.TopLeft);
        }

        /// <summary>The PowerUps, pickups and Arcanas lists, in the wiki's order.</summary>
        public static string Lists(RunResults results)
        {
            var text = new StringBuilder();
            text.Append("<b><color=#B3A39A>POWERUPS</color></b>\n");
            text.Append(results.PowerUps.Count == 0 ? "None" : Join(results.PowerUps, "  "));
            text.Append("\n\n<b><color=#B3A39A>PICKUPS</color></b>\n");
            text.Append(results.Pickups.Count == 0 ? "None" : Join(results.Pickups, " x"));
            text.Append("\n\n<b><color=#B3A39A>ARCANAS</color></b>\n");
            text.Append(results.Arcanas.Count == 0 ? "None" : string.Join(",   ", results.Arcanas));
            return text.ToString();
        }

        private static string Join(List<KeyValuePair<string, int>> pairs, string between)
        {
            var parts = new List<string>(pairs.Count);
            foreach (var pair in pairs) parts.Add(pair.Key + between + pair.Value);
            return string.Join(",   ", parts);
        }

        public void ToggleUnlocks()
        {
            if (UnlocksPanel == null) return;
            UnlocksPanel.SetActive(!UnlocksPanel.activeSelf);
            var label = UnlocksButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = (UnlocksPanel.activeSelf ? "HIDE UNLOCKS" : "SHOW UNLOCKS") + $" ({Results.Unlocks.Count})";
            UnlocksPanel.transform.SetAsLastSibling();
            UnlocksButton.transform.SetAsLastSibling();
            DoneButton.transform.SetAsLastSibling();
        }

        private static void Focus(Selectable target)
        {
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events != null && target != null) events.SetSelectedGameObject(target.gameObject);
        }

        private RectTransform Panel(RectTransform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (name != "Backdrop") UITheme.Edge(go, UITheme.PanelEdge, 2f); // the theme's crimson edge
            return rect;
        }

        private TextMeshProUGUI Text(RectTransform parent, string name, Vector2 min, Vector2 max, string content, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) tmp.font = _font;
            // A size before auto-size: TMP's defaults (up to 72) replaced the limits on screens built at runtime.
            tmp.fontSize = size;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Min(12f, size);
            tmp.fontSizeMax = size;
            tmp.alignment = align;
            tmp.color = UITheme.Text;
            tmp.richText = true;
            tmp.raycastTarget = false;
            tmp.text = content;
            return tmp;
        }

        private Button MakeButton(RectTransform parent, string name, Vector2 min, Vector2 max, string label, Color? color)
        {
            var rect = Panel(parent, name, min, max, Color.white);
            var image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            OptionsMenuUI.ApplyFocusColors(button, image);
            if (color.HasValue)
            {
                var colors = button.colors;
                colors.normalColor = color.Value;
                colors.highlightedColor = color.Value * 1.25f;
                button.colors = colors;
            }
            var text = Text(rect, "Label", Vector2.zero, Vector2.one, label, 34f, TextAlignmentOptions.Center);
            text.fontStyle = FontStyles.Bold;
            return button;
        }
    }
}
