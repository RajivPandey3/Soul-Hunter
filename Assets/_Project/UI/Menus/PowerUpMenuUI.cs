using System.Collections.Generic;
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
        public string InfoText => _info != null ? _info.text : "";

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

            // A dark backing behind the grid, the info and the buttons, so the menu art never competes with them.
            var backing = new GameObject("VS_PowerUpBacking", typeof(RectTransform), typeof(Image));
            var backingRect = (RectTransform)backing.transform;
            backingRect.SetParent(panel, false);
            backingRect.SetAsFirstSibling();
            Anchor(backingRect, 0.03f, 0.08f, 0.97f, 0.93f);
            var backingImage = backing.GetComponent<Image>();
            SoulHunter.Gameplay.UI.UITheme.ApplyPanel(backingImage, 0.93f);
            backingImage.raycastTarget = false;

            // The grid: the wiki's order, 6 across.
            var grid = new GameObject("VS_PowerUpGrid", typeof(RectTransform)).GetComponent<RectTransform>();
            grid.SetParent(panel, false);
            Anchor(grid, 0.06f, 0.37f, 0.94f, 0.9f);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(250f, 136f);
            layout.spacing = new Vector2(10f, 10f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 6;
            layout.childAlignment = TextAnchor.UpperCenter;
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

            var infoGo = new GameObject("VS_PowerUpInfo", typeof(RectTransform));
            infoGo.transform.SetParent(panel, false);
            Anchor((RectTransform)infoGo.transform, 0.06f, 0.2f, 0.6f, 0.35f);
            _info = infoGo.AddComponent<TextMeshProUGUI>();
            var font = template.GetComponentInChildren<TextMeshProUGUI>();
            if (font != null) _info.font = font.font;
            _info.fontSize = 28f;
            _info.alignment = TextAlignmentOptions.TopLeft;
            SoulHunter.Gameplay.UI.UITheme.ApplyText(_info, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Body);

            _buyButton = MakeButton(template, panel, "VS_Buy", "Buy", 0.62f, 0.24f, 0.77f, 0.34f, BuySelected);
            _refundButton = MakeButton(template, panel, "VS_Refund", "Refund PowerUps", 0.79f, 0.24f, 0.94f, 0.34f, Refund);
            if (_goldText != null) Anchor(_goldText.rectTransform, 0.06f, 0.12f, 0.45f, 0.19f);
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

        private static void AddIcon(Button cell, PowerUpId id)
        {
            var label = cell.GetComponentInChildren<TextMeshProUGUI>();
            // No icon: keep the text off the cell's edge.
            if (label != null) label.rectTransform.offsetMin = new Vector2(16f, label.rectTransform.offsetMin.y);
            if (!IconItem.TryGetValue(id, out var item)) return;
            var definition = SoulHunter.Gameplay.Data.WeaponDefinitionLookup.Find(item);
            if (definition == null || definition.Icon == null) return;
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(cell.transform, false);
            var rect = (RectTransform)iconGo.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(10f, 0f);
            rect.sizeDelta = new Vector2(56f, 56f);
            var image = iconGo.AddComponent<Image>();
            image.sprite = definition.Icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var text = cell.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) { text.rectTransform.offsetMin = new Vector2(70f, text.rectTransform.offsetMin.y); }
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
            if (_economy != null && _goldText != null) _goldText.text = $"Gold: {_economy.CurrentGold}";
            foreach (var pair in _cells)
            {
                var info = PowerUps.Info(pair.Key);
                int rank = PowerUps.Rank(data, pair.Key);
                bool locked = PowerUpUnlocks.UnlockedRanks(data, pair.Key) == 0;
                var text = pair.Value.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.fontSize = 24f;
                    text.color = locked ? SoulHunter.Gameplay.UI.UITheme.TextLocked : SoulHunter.Gameplay.UI.UITheme.CardText; // cells are cards
                    text.alignment = TextAlignmentOptions.MidlineLeft;
                    text.text = locked ? $"{info.Name}\nLOCKED" : $"{info.Name}\n{rank}/{info.MaxRank}";
                }
                // Dark cells so the white names read on the menu art; gold for the selected one, green once maxed.
                pair.Value.image.color = pair.Key == _selected ? SelectedCell : (rank >= info.MaxRank ? MaxedCell : (locked ? LockedCell : PlainCell));
                SoulHunter.Gameplay.UI.UITheme.Edge(pair.Value.gameObject, pair.Key == _selected ? SoulHunter.Gameplay.UI.UITheme.Accent
                    : SoulHunter.Gameplay.UI.UITheme.PanelEdge, pair.Key == _selected ? 3f : 1.5f);
            }
            if (_info != null)
            {
                var info = PowerUps.Info(_selected);
                int rank = PowerUps.Rank(data, _selected);
                bool maxed = rank >= info.MaxRank;
                int unlocked = PowerUpUnlocks.UnlockedRanks(data, _selected);
                string condition = PowerUpUnlocks.NextConditionText(data, _selected);
                string price = maxed ? "MAX" : (rank >= unlocked ? "LOCKED" : $"Price: {PowerUps.Price(data, _selected)}");
                string unlock = condition == null ? "" : $"\n<color=#FFB347>Unlock{(unlocked > 0 ? " next rank" : "")}: {condition}</color>";
                if (unlocked > 0 && unlocked < info.MaxRank) unlock = $"\nUnlocked ranks: {unlocked}/{info.MaxRank}" + unlock;
                _info.text = $"<b>{info.Name}</b>  {rank}/{info.MaxRank}\n{info.Description}\n{price}{unlock}";
            }
            if (_buyButton != null)
                _buyButton.interactable = data != null && PowerUps.CanBuyNext(data, _selected) && _economy != null && _economy.CurrentGold >= PowerUps.Price(data, _selected);
            if (_refundButton != null) _refundButton.interactable = data != null && PowerUps.TotalBought(data) > 0;
        }
    }
}
