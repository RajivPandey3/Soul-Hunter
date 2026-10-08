using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.UI
{
    /// <summary>
    /// The VS PowerUps screen (owner, 2026-09-30: "everwhere follow DOT VS ONLY"; vampire.survivors.wiki/w/PowerUps):
    /// every PowerUp in the wiki's order as a grid of cells (icon, name, rank), an info box with the selected PowerUp's
    /// description, rank and VS price, a Buy button, and VS's "Refund PowerUps" button. Built at runtime inside the scene's
    /// PowerUp_Panel from a copy of an existing button, so the MainMenu scene is not edited; the old four-button grid is
    /// hidden. Icons: VS PowerUps show the matching passive item, so each uses Soul Hunter's passive of the same stat
    /// where one exists (none for Greed, Omni, Charm, Defang, Reroll, Skip, Banish, Preserve).
    /// </summary>
    public class PowerUpMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _goldText;

        [Header("Old buttons (hidden; the VS grid replaces them)")]
        [SerializeField] private Button _buyMightButton;
        [SerializeField] private Button _buyArmorButton;
        [SerializeField] private Button _buyGreedButton;
        [SerializeField] private Button _buyRevivalButton;

        [Header("Old prices (unused since 2026-09-30; the v2 save migration refunds them)")]
        [SerializeField] private int _mightPrice = 500;
        [SerializeField] private int _armorPrice = 300;
        [SerializeField] private int _greedPrice = 400;
        [SerializeField] private int _revivalPrice = 1000;

        private static readonly Dictionary<PowerUpId, UpgradeType> IconItem = new Dictionary<PowerUpId, UpgradeType>
        {
            { PowerUpId.Might, UpgradeType.Spinach }, { PowerUpId.Armor, UpgradeType.Armor }, { PowerUpId.MaxHealth, UpgradeType.MaxHealth },
            { PowerUpId.Recovery, UpgradeType.Pummarola }, { PowerUpId.Cooldown, UpgradeType.EmptyTome }, { PowerUpId.Area, UpgradeType.Candelabrador },
            { PowerUpId.Speed, UpgradeType.Bracer }, { PowerUpId.Duration, UpgradeType.Spellbinder }, { PowerUpId.Amount, UpgradeType.Duplicator },
            { PowerUpId.MoveSpeed, UpgradeType.PlayerSpeed }, { PowerUpId.Magnet, UpgradeType.Attractorb }, { PowerUpId.Luck, UpgradeType.Clover },
            { PowerUpId.Growth, UpgradeType.Crown }, { PowerUpId.Curse, UpgradeType.SkullOManiac }, { PowerUpId.Revival, UpgradeType.Tiragisu },
        };

        // Cell looks come from the theme (Theme Studio > Cards): plain, selected, maxed and locked.
        private static Color PlainCell => SoulHunter.Gameplay.UI.UITheme.Cell;
        private static Color SelectedCell => SoulHunter.Gameplay.UI.UITheme.CellSelected;
        private static Color MaxedCell => SoulHunter.Gameplay.UI.UITheme.CellMaxed;
        private static Color LockedCell => SoulHunter.Gameplay.UI.UITheme.CellLocked;

        private EconomyService _economy;
        private SaveService _saveService;
        private bool _listenersBound;
        private readonly Dictionary<PowerUpId, Button> _cells = new Dictionary<PowerUpId, Button>();
        private TextMeshProUGUI _info;
        private Button _buyButton, _refundButton;
        private PowerUpId _selected = PowerUpId.Might;

        /// <summary>The grid cell of a PowerUp (tests).</summary>
        public Button CellFor(PowerUpId id) => _cells.TryGetValue(id, out var cell) ? cell : null;
        public Button BuyButton => _buyButton;
        public Button RefundButton => _refundButton;
        public PowerUpId Selected => _selected;
        /// <summary>Everything the info box says: name and rank, description, price (tests).</summary>
        public string InfoText => string.Join("\n", new[] { _infoName, _info, _price }.Where(t => t != null).Select(t => t.text));

        private void Start()
        {
            if (GameServices.Instance != null)
            {
                GameServices.Instance.TryGet(out _economy);
                GameServices.Instance.TryGet(out _saveService);
            }
            BuildShop();
            if (_economy != null)
            {
                _economy.OnGoldChanged += HandleGoldChanged;
                _listenersBound = true;
            }
            UpdateUI();
        }

        private void HandleGoldChanged(int _) => UpdateUI();

        private void OnDestroy()
        {
            if (_listenersBound && _economy != null) _economy.OnGoldChanged -= HandleGoldChanged;
        }

        private void BuildShop()
        {
            Button template = null;
            foreach (var candidate in new[] { _buyMightButton, _buyArmorButton, _buyGreedButton, _buyRevivalButton })
                if (candidate != null) { template = candidate; break; }
            if (template == null) return;
            var oldGrid = template.transform.parent;
            var panel = oldGrid != null && oldGrid.parent != null ? (RectTransform)oldGrid.parent : (RectTransform)transform;
            foreach (var old in new[] { _buyMightButton, _buyArmorButton, _buyGreedButton, _buyRevivalButton })
                if (old != null) old.gameObject.SetActive(false);
            if (oldGrid != null && oldGrid != panel) oldGrid.gameObject.SetActive(false);
            var font = template.GetComponentInChildren<TextMeshProUGUI>();
            _font = font != null ? font.font : null;

            // One dark backing for the whole screen, so the menu art never competes with it.
            var backing = new GameObject("VS_PowerUpBacking", typeof(RectTransform), typeof(Image));
            var backingRect = (RectTransform)backing.transform;
            backingRect.SetParent(panel, false);
            backingRect.SetAsFirstSibling();
            Anchor(backingRect, 0.03f, 0.05f, 0.97f, 0.95f);
            var backingImage = backing.GetComponent<Image>();
            SoulHunter.Gameplay.UI.UITheme.ApplyPanel(backingImage, 0.93f);
            backingImage.raycastTarget = false;

            // Header: Back inside the backing on the left, the screen's title in the middle, gold on the right.
            foreach (var button in panel.GetComponentsInChildren<Button>(true))
                if (button.name.Contains("Back"))
                {
                    var back = (RectTransform)button.transform;
                    back.SetParent(panel, false);
                    back.anchorMin = back.anchorMax = new Vector2(0.03f, 0.885f);
                    back.pivot = new Vector2(0.5f, 0.5f);
                    back.anchoredPosition = new Vector2(24f + SoulHunter.Gameplay.UI.UITheme.Layout.ButtonWidth * 0.5f, 0f);
                    back.SetAsLastSibling();
                }
            var title = MakeText("VS_PowerUpTitle", panel, 0.3f, 0.84f, 0.7f, 0.93f, "Power Up", 56f, TextAlignmentOptions.Center,
                SoulHunter.Gameplay.UI.ThemedElement.TextRole.Title);
            title.fontStyle = FontStyles.Bold;
            var goldRow = new GameObject("VS_GoldRow", typeof(RectTransform)).GetComponent<RectTransform>();
            goldRow.SetParent(panel, false);
            Anchor(goldRow, 0.68f, 0.85f, 0.95f, 0.92f);
            _goldCoin = MakeIcon("Coin", goldRow, SoulHunter.Gameplay.UI.UITheme.IconFor(SoulHunter.Gameplay.UI.UITheme.IconSlot.Coin), 48f);
            _goldCoin.rectTransform.anchorMin = _goldCoin.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            if (_goldText != null)
            {
                _goldText.rectTransform.SetParent(goldRow, false);
                _goldText.rectTransform.anchorMin = new Vector2(1f, 0f);
                _goldText.rectTransform.anchorMax = new Vector2(1f, 1f);
                _goldText.rectTransform.pivot = new Vector2(1f, 0.5f);
                _goldText.rectTransform.sizeDelta = new Vector2(200f, 0f);
                _goldText.rectTransform.anchoredPosition = Vector2.zero;
                _goldText.alignment = TextAlignmentOptions.MidlineRight;
                _goldText.fontSize = 40f;
                _goldText.enableAutoSizing = true;
                _goldText.fontSizeMin = 18f;
                _goldText.fontSizeMax = 40f;
                SoulHunter.Gameplay.UI.UITheme.ApplyText(_goldText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Gold);
            }

            // The grid: the wiki's order, 6 across, cells sized to fill it.
            var grid = new GameObject("VS_PowerUpGrid", typeof(RectTransform)).GetComponent<RectTransform>();
            grid.SetParent(panel, false);
            Anchor(grid, 0.05f, 0.36f, 0.95f, 0.82f);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(250f, 110f);
            layout.spacing = new Vector2(12f, 12f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 6;
            layout.childAlignment = TextAnchor.UpperCenter;
            var fit = grid.gameObject.AddComponent<GridFit>();
            fit.Grid = layout;
            fit.Viewport = grid;
            fit.Count = PowerUps.All.Length;
            fit.MinHeightRatio = 0f; // all four rows fit above the info box
            foreach (var info in PowerUps.All)
            {
                var id = info.Id;
                var cell = Instantiate(template, grid);
                cell.name = "PowerUp_" + id;
                cell.gameObject.SetActive(true);
                cell.onClick.RemoveAllListeners();
                cell.onClick.AddListener(() => Select(id));
                AddIcon(cell, id);
                _cells[id] = cell;
            }

            // The info box: the selected PowerUp's big icon, name and rank, what it does, its price, and the actions.
            var box = new GameObject("VS_PowerUpInfoBox", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            box.SetParent(panel, false);
            Anchor(box, 0.05f, 0.08f, 0.95f, 0.335f);
            var boxImage = box.GetComponent<Image>();
            SoulHunter.Gameplay.UI.UITheme.ApplySurface(boxImage, SoulHunter.Gameplay.UI.ThemedElement.SurfaceRole.Row);
            boxImage.raycastTarget = false;
            _bigIcon = MakeIcon("BigIcon", box, null, 128f);
            _bigIcon.rectTransform.anchorMin = _bigIcon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            _bigIcon.rectTransform.anchoredPosition = new Vector2(28f + 64f, 0f);
            _infoName = MakeText("Name", box, 0.15f, 0.66f, 0.6f, 0.93f, "", 38f, TextAlignmentOptions.MidlineLeft,
                SoulHunter.Gameplay.UI.ThemedElement.TextRole.Accent);
            _info = MakeText("Description", box, 0.15f, 0.36f, 0.6f, 0.66f, "", 28f, TextAlignmentOptions.MidlineLeft,
                SoulHunter.Gameplay.UI.ThemedElement.TextRole.Body);
            _priceIcon = MakeIcon("PriceCoin", box, SoulHunter.Gameplay.UI.UITheme.IconFor(SoulHunter.Gameplay.UI.UITheme.IconSlot.Coin), 40f);
            _priceIcon.rectTransform.anchorMin = _priceIcon.rectTransform.anchorMax = new Vector2(0.15f, 0.2f);
            _priceIcon.rectTransform.anchoredPosition = new Vector2(20f, 0f);
            _price = MakeText("Price", box, 0.15f, 0.06f, 0.6f, 0.34f, "", 30f, TextAlignmentOptions.MidlineLeft,
                SoulHunter.Gameplay.UI.ThemedElement.TextRole.Gold);
            _price.rectTransform.offsetMin = new Vector2(50f, 0f);

            // Buy and Refund: one themed row of standard buttons on the box's right (shrinks as one piece if a narrow
            // window cannot fit both).
            var actionArea = new GameObject("VS_ActionArea", typeof(RectTransform)).GetComponent<RectTransform>();
            actionArea.SetParent(box, false);
            Anchor(actionArea, 0.6f, 0.1f, 0.99f, 0.9f);
            var actionRow = new GameObject("VS_ActionRow", typeof(RectTransform)).GetComponent<RectTransform>();
            actionRow.SetParent(actionArea, false);
            Anchor(actionRow, 0.5f, 0f, 0.5f, 1f);
            _buyButton = MakeButton(template, actionRow, "VS_Buy", "Buy", 0.5f, 0.5f, 0.5f, 0.5f, BuySelected);
            _refundButton = MakeButton(template, actionRow, "VS_Refund", "Refund All", 0.5f, 0.5f, 0.5f, 0.5f, Refund);
            actionRow.gameObject.AddComponent<SoulHunter.Gameplay.UI.ThemedRow>();
        }

        private TMP_FontAsset _font;
        private Image _bigIcon, _priceIcon, _goldCoin;
        private TextMeshProUGUI _infoName, _price;

        private TextMeshProUGUI MakeText(string name, RectTransform parent, float xMin, float yMin, float xMax, float yMax, string text,
            float maxSize, TextAlignmentOptions alignment, SoulHunter.Gameplay.UI.ThemedElement.TextRole role)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Anchor(rect, xMin, yMin, xMax, yMax);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) label.font = _font;
            label.fontSize = maxSize; // a size before auto-size (TMP's defaults would replace the limits)
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = maxSize;
            label.alignment = alignment;
            label.text = text;
            label.raycastTarget = false;
            SoulHunter.Gameplay.UI.UITheme.ApplyText(label, role);
            return label;
        }

        private static Image MakeIcon(string name, RectTransform parent, Sprite sprite, float size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.enabled = sprite != null;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static void Anchor(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static Button MakeButton(Button template, RectTransform panel, string name, string label, float xMin, float yMin, float xMax, float yMax,
            UnityEngine.Events.UnityAction action)
        {
            var button = Instantiate(template, panel);
            button.name = name;
            button.gameObject.SetActive(true);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            Anchor((RectTransform)button.transform, xMin, yMin, xMax, yMax);
            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) { text.text = label; text.fontStyle = FontStyles.Bold; }
            bool buy = name == "VS_Buy";
            SoulHunter.Gameplay.UI.UITheme.ApplyButton(button, button.image, buy ? SoulHunter.Gameplay.UI.UITheme.ButtonKind.Primary : SoulHunter.Gameplay.UI.UITheme.ButtonKind.Danger,
                buy ? SoulHunter.Gameplay.UI.UITheme.IconSlot.Buy : SoulHunter.Gameplay.UI.UITheme.IconSlot.None);
            return button;
        }

        /// <summary>
        /// The PowerUp's icon: Soul Hunter's own badge where one was made (GameContent.PowerUpIcons), else its passive
        /// item's icon (VS PowerUps show the matching passive).
        /// </summary>
        public static Sprite IconOf(PowerUpId id)
        {
            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            var own = catalog != null ? catalog.PowerUpIconFor(id) : null;
            if (own != null) return own;
            if (!IconItem.TryGetValue(id, out var item)) return null;
            var definition = SoulHunter.Gameplay.Data.WeaponDefinitionLookup.Find(item);
            return definition != null ? definition.Icon : null;
        }

        private const float CellIconSize = 64f;

        /// <summary>Every cell has the same icon slot (one size, one place), and its text starts after it.</summary>
        private static void AddIcon(Button cell, PowerUpId id)
        {
            var label = cell.GetComponentInChildren<TextMeshProUGUI>();
            var icon = MakeIcon("Icon", (RectTransform)cell.transform, IconOf(id), CellIconSize);
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(12f + CellIconSize * 0.5f, 0f);
            if (label != null)
            {
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(CellIconSize + 24f, 6f);
                label.rectTransform.offsetMax = new Vector2(-10f, -6f);
                label.fontSize = 26f;
                label.enableAutoSizing = true;
                label.fontSizeMin = 14f;
                label.fontSizeMax = 26f;
                label.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        /// <summary>Shows a PowerUp in the info box (a click on its cell).</summary>
        public void Select(PowerUpId id)
        {
            _selected = id;
            UpdateUI();
        }

        /// <summary>VS: buys the next rank of the selected PowerUp at its current price, if affordable and not maxed.</summary>
        public void BuySelected()
        {
            var data = _saveService != null ? _saveService.CurrentData : null;
            // VS: a locked rank cannot be bought (PowerUpUnlocks).
            if (_economy == null || data == null || !PowerUps.CanBuyNext(data, _selected)) return;
            int price = PowerUps.Price(data, _selected);
            if (!_economy.SpendGold(price)) { Debug.Log("[PowerUpMenu] Not enough gold!"); return; }
            PowerUps.SetRank(data, _selected, PowerUps.Rank(data, _selected) + 1);
            _saveService.SaveGame();
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.LevelUpSound);
            UpdateUI();
        }

        /// <summary>VS "Refund PowerUps": every rank comes off and its gold comes back.</summary>
        public void Refund()
        {
            var data = _saveService != null ? _saveService.CurrentData : null;
            if (_economy == null || data == null || PowerUps.TotalBought(data) == 0) return;
            int value = PowerUps.RefundValue(data);
            data.PowerUpRanks.Clear();
            _economy.AddGold(value);
            _saveService.SaveGame();
            UpdateUI();
        }

        private void UpdateUI()
        {
            var data = _saveService != null ? _saveService.CurrentData : null;
            int gold = _economy != null ? _economy.CurrentGold : 0;
            if (_economy != null && _goldText != null)
            {
                _goldText.text = gold.ToString();
                // The coin sits right before the number, however long it is.
                if (_goldCoin != null) _goldCoin.rectTransform.anchoredPosition = new Vector2(-(Mathf.Min(_goldText.preferredWidth, 200f) + 32f), 0f);
            }
            var theme = SoulHunter.Gameplay.UI.UiThemeAsset.Active;
            string cardSoft = "#" + ColorUtility.ToHtmlStringRGB(SoulHunter.Gameplay.UI.UITheme.CardTextSoft);
            string darkSoft = "#" + ColorUtility.ToHtmlStringRGB(SoulHunter.Gameplay.UI.UITheme.TextSoft);
            foreach (var pair in _cells)
            {
                var info = PowerUps.Info(pair.Key);
                int rank = PowerUps.Rank(data, pair.Key);
                bool locked = PowerUpUnlocks.UnlockedRanks(data, pair.Key) == 0;
                bool maxed = rank >= info.MaxRank;
                bool chosen = pair.Key == _selected;
                var text = pair.Value.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    // Words carry every state, not colour alone: the rank, MAX, or LOCKED and what unlocks it.
                    string status = locked ? $"LOCKED\n<size=70%>{PowerUpUnlocks.NextConditionText(data, pair.Key)}</size>"
                        : maxed ? SoulHunter.Gameplay.UI.UITheme.Bold("MAX") : $"<color={cardSoft}>{rank}/{info.MaxRank}</color>";
                    text.text = $"{SoulHunter.Gameplay.UI.UITheme.Bold(info.Name)}\n{status}";
                    text.alignment = TextAlignmentOptions.MidlineLeft;
                    SoulHunter.Gameplay.UI.UITheme.ApplyText(text, locked ? SoulHunter.Gameplay.UI.ThemedElement.TextRole.Soft
                        : SoulHunter.Gameplay.UI.ThemedElement.TextRole.Card);
                }
                var icon = pair.Value.transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null) icon.color = locked ? new Color(1f, 1f, 1f, 0.35f) : Color.white; // a locked PowerUp's icon is faded
                // The cell keeps its state's colour; the chosen one also wears the theme's frame, as the chosen character card.
                pair.Value.image.color = locked ? LockedCell : maxed ? MaxedCell : chosen ? SelectedCell : PlainCell;
                SoulHunter.Gameplay.UI.UITheme.Edge(pair.Value.gameObject, chosen ? theme.Cards.SelectedEdge : SoulHunter.Gameplay.UI.UITheme.PanelEdge,
                    chosen ? theme.Cards.SelectedEdgeWidth : 1.5f);
            }
            if (_info != null)
            {
                var info = PowerUps.Info(_selected);
                int rank = PowerUps.Rank(data, _selected);
                bool maxed = rank >= info.MaxRank;
                int unlocked = PowerUpUnlocks.UnlockedRanks(data, _selected);
                bool lockedRank = !maxed && rank >= unlocked;
                string condition = PowerUpUnlocks.NextConditionText(data, _selected);
                if (_bigIcon != null)
                {
                    _bigIcon.sprite = IconOf(_selected);
                    _bigIcon.enabled = _bigIcon.sprite != null;
                    _bigIcon.color = unlocked == 0 ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
                }
                if (_infoName != null) _infoName.text = $"{SoulHunter.Gameplay.UI.UITheme.Bold(info.Name)}  <color={darkSoft}><size=70%>Rank {rank}/{info.MaxRank}</size></color>";
                string unlock = condition == null ? "" : $"\n<color={darkSoft}>Unlock{(unlocked > 0 ? " next rank" : "")}: {condition}</color>";
                if (unlocked > 0 && unlocked < info.MaxRank) unlock = $"\n<color={darkSoft}>Unlocked ranks: {unlocked}/{info.MaxRank}</color>" + unlock;
                _info.text = info.Description + unlock;
                if (_price != null)
                {
                    int cost = PowerUps.Price(data, _selected);
                    bool afford = gold >= cost;
                    _price.text = maxed ? "MAX" : lockedRank ? "LOCKED" : afford ? $"Price: {cost}" : $"Price: {cost}  (need {cost - gold} more gold)";
                    SoulHunter.Gameplay.UI.UITheme.ApplyText(_price, maxed ? SoulHunter.Gameplay.UI.ThemedElement.TextRole.Accent
                        : lockedRank ? SoulHunter.Gameplay.UI.ThemedElement.TextRole.Soft
                        : afford ? SoulHunter.Gameplay.UI.ThemedElement.TextRole.Gold : SoulHunter.Gameplay.UI.ThemedElement.TextRole.Danger);
                    if (_priceIcon != null) _priceIcon.enabled = !maxed && !lockedRank;
                }
            }
            if (_buyButton != null)
                _buyButton.interactable = data != null && PowerUps.CanBuyNext(data, _selected) && _economy != null && gold >= PowerUps.Price(data, _selected);
            if (_refundButton != null) _refundButton.interactable = data != null && PowerUps.TotalBought(data) > 0;
        }
    }
}
