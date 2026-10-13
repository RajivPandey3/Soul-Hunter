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
        private static Color OffCell => SoulHunter.Gameplay.UI.UITheme.CellOff;

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
        /// <summary>What the Buy button says now: "Buy", or "Turn Off" / "Turn On" for a maxed PowerUp (tests).</summary>
        public string BuyButtonText => _buyLabel != null ? _buyLabel.text : null;
        public Button RefundButton => _refundButton;
        /// <summary>The price line in the info box.</summary>
        public TextMeshProUGUI PriceText => _price;
        /// <summary>The window the info box's words show through (they scroll inside it).</summary>
        public RectTransform InfoTextWindow => _textArea;
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
            Relayout();
        }

        private void HandleGoldChanged(int _) => UpdateUI();

        private void OnEnable() => SoulHunter.Gameplay.UI.UiScreen.Changed += MarkLayoutDirty;

        private void OnDisable() => SoulHunter.Gameplay.UI.UiScreen.Changed -= MarkLayoutDirty;

        private void OnDestroy()
        {
            SoulHunter.Gameplay.UI.UiScreen.Changed -= MarkLayoutDirty;
            if (_listenersBound && _economy != null) _economy.OnGoldChanged -= HandleGoldChanged;
        }

        private void MarkLayoutDirty() => _layoutDirty = true;

        // ---------------------------------------------------------------- responsive layout (owner, 2026-10-10)
        // One screen for landscape and portrait, keeping the approved look (2026-10-07) wherever it fits: the content sits
        // in the safe area; the header holds Back, the title and the gold; the grid fills the room above the info box.
        // Only short or narrow room changes the arrangement, through the shared pieces' modes: a short body (a landscape
        // phone) puts the info box beside the grid; a header too narrow for Back (a portrait phone) moves Back to a footer
        // pinned to the bottom. The grid picks its columns from its width (GridFit.MinCellWidth) and scrolls when its rows
        // do not fit; the info box's text scrolls, and Buy and Refund All stay in a ThemedRow, always shown.

        // Roomy screens (short side 720 units or more: every PC window) and short ones (a phone): the short ones keep
        // thinner edges and a lower header, so the grid and the info box get the room.
        private const float CompactBelow = 720f;
        private static float Margin(bool compact) => compact ? 8f : 24f;      // backing inside the safe area
        private static float Padding(bool compact) => compact ? 12f : 20f;    // content inside the backing
        private static float HeaderHeight(bool compact) => compact ? 60f : 72f;
        private const float GoldWidth = 260f;
        private const float BoxPadding = 20f;
        private const float MinCellWidth = 180f;
        private const float MinTitleWidth = 160f;    // the header keeps at least this for the title beside Back and the gold
        private const float ShortBodyBelow = 700f;   // a body this short puts the info box beside the grid
        private const float MinInfoTextHeight = 160f; // the info box keeps this for its words above wrapped buttons
        private const float MinInfoTextWidth = 500f;  // a box this wide (plus the buttons) has its words beside the buttons
        private const float ShortInfoWidth = 440f;    // beside the grid (a landscape phone) the box is this wide: its buttons stack, and the grid keeps its columns

        private RectTransform _content, _backing, _inner, _header, _footer, _body, _gridArea, _gridViewport, _gridContent, _infoArea, _infoBox, _boxInner, _textArea;
        private RectTransform _titleRect;
        private TextMeshProUGUI _title;
        private ScrollRect _gridScroll;
        private SoulHunter.Gameplay.UI.AdaptiveSplit _split, _boxSplit;
        private SoulHunter.Gameplay.UI.GridFit _gridFit;
        private LayoutElement _bigIconLayout;
        private Button _back;
        private bool _layoutDirty;
        private Vector2 _lastInnerSize;
        private float _lastTouch = -1f, _lastFloor = -1f;

        /// <summary>The grid's scroll area and its visible window (tests).</summary>
        public ScrollRect GridScroll => _gridScroll;
        public RectTransform GridViewport => _gridViewport;
        public RectTransform InfoBox => _infoBox;
        public Button BackButton => _back;
        /// <summary>True when the grid and the info box are stacked (the approved arrangement; not on a short screen).</summary>
        public bool IsStacked => _split != null && _split.IsTall;
        /// <summary>True when Back sits in the header (false: in the footer, a narrow screen).</summary>
        public bool BackInHeader => _back != null && _back.transform.parent == _header;
        /// <summary>The grid's column count now.</summary>
        public int Columns => _gridContent != null ? _gridContent.GetComponent<GridLayoutGroup>().constraintCount : 0;

        /// <summary>
        /// The controls a player can reach right now: Back, Buy, Refund All and every cell wholly inside the grid's window
        /// (a cell scrolled out of the window is clipped by its mask and takes no clicks).
        /// </summary>
        public IEnumerable<Selectable> VisibleControls()
        {
            if (_back != null) yield return _back;
            if (_buyButton != null) yield return _buyButton;
            if (_refundButton != null) yield return _refundButton;
            if (_gridViewport == null) yield break;
            foreach (var cell in _cells.Values)
                if (cell != null && SoulHunter.Gameplay.UI.ResponsiveScroll.IsInView(_gridScroll, (RectTransform)cell.transform)) yield return cell;
        }

        private void LateUpdate()
        {
            if (_inner == null) return;
            float touch = SoulHunter.Gameplay.UI.UiScreen.MinTouchUnits, floor = SoulHunter.Gameplay.UI.UiScreen.MinTextUnits;
            if (_layoutDirty || _inner.rect.size != _lastInnerSize || !Mathf.Approximately(touch, _lastTouch) || !Mathf.Approximately(floor, _lastFloor))
                Relayout();
            FitLockedCells();
        }

        private readonly Dictionary<TextMeshProUGUI, string> _lockedBrief = new Dictionary<TextMeshProUGUI, string>();
        private bool _lockedFitDirty;
        private Vector2 _lockedFitSize;

        /// <summary>
        /// A LOCKED cell says what unlocks it while that fits. Where it cannot (a phone: text stays at 12 sp, and the lines
        /// ran 57 px out of the cell, owner's phone 2026-10-12) the cell says the name and LOCKED, and the info box gives
        /// the condition when the cell is chosen.
        /// </summary>
        private void FitLockedCells()
        {
            if (SoulHunter.Gameplay.UI.UiScreen.MinTouchUnits <= 0f) return; // a PC's cells shrink their text to fit, as approved
            Vector2 size = Vector2.zero;
            foreach (var label in _lockedBrief.Keys) { if (label != null) { size = label.rectTransform.rect.size; break; } }
            if (size.x <= 0f || size.y <= 0f) return; // not laid out yet
            if (!_lockedFitDirty && size == _lockedFitSize) return;
            bool resized = size != _lockedFitSize;
            _lockedFitSize = size;
            if (resized && !_lockedFitDirty) UpdateUI(); // the room changed: try the full text again
            _lockedFitDirty = false;
            foreach (var pair in _lockedBrief.ToList())
            {
                if (pair.Key == null) continue;
                pair.Key.ForceMeshUpdate();
                if (pair.Key.isTextOverflowing || pair.Key.textBounds.size.y > pair.Key.rectTransform.rect.height + 1f) pair.Key.text = pair.Value;
            }
        }

        /// <summary>Places the header, body, footer and info box for the screen's size now (no rebuild; selection and scroll kept).</summary>
        private void Relayout()
        {
            if (_inner == null) { UpdateUI(); return; }
            _layoutDirty = false;
            Vector2 room = _content.rect.size;
            bool compact = Mathf.Min(room.x, room.y) < CompactBelow;
            float margin = Margin(compact), padding = Padding(compact);
            _backing.offsetMin = new Vector2(margin, margin);
            _backing.offsetMax = new Vector2(-margin, -margin);
            _inner.offsetMin = new Vector2(padding, padding);
            _inner.offsetMax = new Vector2(-padding, -padding);
            _lastInnerSize = _inner.rect.size;
            _lastTouch = SoulHunter.Gameplay.UI.UiScreen.MinTouchUnits;
            _lastFloor = SoulHunter.Gameplay.UI.UiScreen.MinTextUnits;
            var theme = SoulHunter.Gameplay.UI.UiThemeAsset.Active;
            float gap = theme.Layouts.ButtonGap;
            float floor = _lastFloor;

            // Back at the theme's size (48 dp on touch).
            Vector2 backSize = new Vector2(theme.Layouts.ButtonWidth, Mathf.Max(theme.Layouts.ButtonHeight, _lastTouch));
            var element = _back != null ? _back.GetComponent<SoulHunter.Gameplay.UI.ThemedElement>() : null;
            if (element != null && element.StandardSize(theme) is Vector2 standard) backSize = standard;
            Vector2 buttonSize = backSize; // Buy and Refund All are the same standard buttons

            // Header (approved 2026-10-07): Back on the left, the title, the gold with its coin on the right. Back stays
            // there wherever the header fits it beside a readable title and the gold; only a narrow screen (a portrait
            // phone) moves it to a footer pinned to the bottom of the safe content.
            float headerWidth = _inner.rect.width;
            bool backInHeader = _back != null && headerWidth >= backSize.x + gap + MinTitleWidth + gap + GoldWidth;
            float headerHeight = Mathf.Max(HeaderHeight(compact), backInHeader ? backSize.y : 0f);
            _header.sizeDelta = new Vector2(0f, headerHeight);
            _footer.sizeDelta = new Vector2(0f, _back != null && !backInHeader ? backSize.y : 0f);
            if (_back != null)
            {
                var back = (RectTransform)_back.transform;
                if (back.parent != (backInHeader ? _header : _footer)) back.SetParent(backInHeader ? _header : _footer, false);
                back.anchorMin = back.anchorMax = new Vector2(0f, 0.5f);
                back.pivot = new Vector2(0.5f, 0.5f);
                back.sizeDelta = backSize;
                back.anchoredPosition = new Vector2(backSize.x * 0.5f, 0f);
            }
            float footer = _back != null && !backInHeader ? backSize.y + gap : 0f;
            _body.offsetMin = new Vector2(0f, footer);
            _body.offsetMax = new Vector2(0f, -(headerHeight + gap));

            // The title centres on the header when both sides leave it room, else takes the room between Back and the gold.
            float left = backInHeader ? backSize.x + gap : 0f;
            float side = Mathf.Max(left, GoldWidth);
            bool centred = headerWidth >= side * 2f + MinTitleWidth * 2f;
            _titleRect.offsetMin = new Vector2(centred ? side : left, 0f);
            _titleRect.offsetMax = new Vector2(-(centred ? side : GoldWidth + gap), 0f);
            _title.alignment = centred ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
            _title.fontSizeMin = Mathf.Max(16f, floor);
            if (_goldText != null) { _goldText.fontSizeMin = Mathf.Max(18f, floor); _goldText.fontSizeMax = Mathf.Max(40f, _goldText.fontSizeMin); }

            // Body (approved): the grid on top, filling the room above the info box at the bottom. Only a short body (a
            // landscape phone, under ShortBodyBelow units tall) puts the info box beside the grid, where a bottom box would
            // leave the grid no rows.
            Vector2 body = _body.rect.size;
            bool shortBody = body.y < ShortBodyBelow;
            bool oneLine = BoxWidthFor(body.x, shortBody, theme) - BoxPadding * 2f - gap >= buttonSize.x * 2f + gap;
            float actionsHeight = oneLine ? buttonSize.y : buttonSize.y * 2f + gap;
            _split.TallWhenAspectBelow = shortBody ? 1f : float.MaxValue; // short: side by side when wide; else always stacked
            float infoWanted = BoxPadding * 2f + gap + actionsHeight + InfoTextHeight(body.x, compact, floor);
            _split.TallShare = Mathf.Clamp(1f - infoWanted / Mathf.Max(1f, body.y), compact ? 0.3f : 0.4f, 0.64f);
            float infoNeeds = InfoWidthBesideGrid(theme);
            _split.WideShare = Mathf.Clamp(1f - infoNeeds / Mathf.Max(1f, body.x), 0.35f, 0.62f);
            _split.Layout(force: true);
            // PC rows fit their window as before; on a short screen cells keep room for their lines and the grid scrolls.
            // A touch screen's text cannot shrink under 12 sp, so its cells keep that room too (a tablet: a LOCKED cell's
            // four lines ran out of a squeezed cell).
            _gridFit.MinHeightRatio = compact || _lastTouch > 0f ? 0.6f : 0f;

            // Info box: the text beside Buy and Refund All in a wide box (approved), above them in a narrow one.
            Vector2 box = _boxInner.rect.size;
            bool boxSideBySide = box.x >= MinInfoTextWidth + buttonSize.x * 2f + gap * 3f;
            _boxSplit.TallWhenAspectBelow = boxSideBySide ? 0f : float.MaxValue;
            _boxSplit.WideShare = Mathf.Clamp(1f - (buttonSize.x * 2f + gap * 3f) / Mathf.Max(1f, box.x), 0.3f, 0.7f);
            bool boxOneLine = box.x - gap >= buttonSize.x * 2f + gap;
            float rowHeight = boxOneLine ? buttonSize.y : buttonSize.y * 2f + gap;
            _boxSplit.TallShare = Mathf.Clamp(1f - (rowHeight + gap) / Mathf.Max(1f, box.y), 0.1f, 0.9f);
            _boxSplit.Layout(force: true);

            // Text never under the floor (12 sp on touch).
            foreach (var cell in _cells.Values)
            {
                var label = cell != null ? cell.GetComponentInChildren<TextMeshProUGUI>() : null;
                if (label == null) continue;
                label.fontSizeMin = Mathf.Max(14f, floor);
                label.fontSizeMax = Mathf.Max(26f, label.fontSizeMin);
            }
            if (_infoName != null) _infoName.fontSize = Mathf.Max(38f, floor);
            if (_info != null) _info.fontSize = Mathf.Max(28f, floor);
            if (_price != null) _price.fontSize = Mathf.Max(30f, floor);

            // The big icon is 128 where the box has the height for it, smaller in a short box.
            float iconSize = _textArea.rect.height >= 150f ? 128f : 80f;
            if (_bigIconLayout != null) _bigIconLayout.minWidth = _bigIconLayout.preferredWidth = _bigIconLayout.minHeight = _bigIconLayout.preferredHeight = iconSize;
            UpdateUI();
        }

        /// <summary>The info box's width for a body this wide (stacked: the whole width; short and wide: its share).</summary>
        private float BoxWidthFor(float bodyWidth, bool shortBody, SoulHunter.Gameplay.UI.UiThemeAsset theme)
        {
            if (!shortBody || bodyWidth < _body.rect.height) return bodyWidth;
            float gap = theme.Layouts.ButtonGap;
            float infoNeeds = InfoWidthBesideGrid(theme);
            float share = Mathf.Clamp(1f - infoNeeds / Mathf.Max(1f, bodyWidth), 0.35f, 0.62f);
            return bodyWidth * (1f - share) - gap * 0.5f;
        }

        /// <summary>The width the info box asks for beside the grid (a short body): its words over one column of buttons.</summary>
        private static float InfoWidthBesideGrid(SoulHunter.Gameplay.UI.UiThemeAsset theme) =>
            Mathf.Max(theme.Layouts.ButtonWidth + theme.Layouts.ButtonGap * 2f, ShortInfoWidth) + BoxPadding * 2f;

        /// <summary>
        /// The height the info box keeps for its words under the grid. A PC keeps the approved <see cref="MinInfoTextHeight"/>.
        /// A phone's box is narrow, so the longest description and the price wrap: the box takes the height they need, and
        /// the price is not cut off at its lower edge (phone run, 2026-10-11).
        /// </summary>
        private float InfoTextHeight(float boxWidth, bool compact, float floor)
        {
            if (!compact || _info == null || _price == null || _infoName == null) return MinInfoTextHeight;
            float words = Mathf.Max(120f, boxWidth - BoxPadding * 2f - 128f - 20f);
            float nameSize = Mathf.Max(38f, floor), infoSize = Mathf.Max(28f, floor), priceSize = Mathf.Max(30f, floor);
            // Counted from the letters, not measured: the shop is laid out while its panel is still off, when the text
            // components may not measure themselves yet (measuring left the first phone build at the old height).
            int most = 0;
            foreach (var info in PowerUps.All) most = Mathf.Max(most, info.Description != null ? info.Description.Length : 0);
            float longest = WrappedHeight(most, infoSize, words);
            float price = WrappedHeight("Price: 99999  (need 99999 more gold)".Length, priceSize, Mathf.Max(80f, words - 50f));
            return Mathf.Max(MinInfoTextHeight, nameSize * 1.25f + longest + price + 6f * 2f + 8f);
        }

        /// <summary>The height of this many letters wrapped into this width (the theme's condensed font: about 0.45 of its size per letter).</summary>
        private static float WrappedHeight(int letters, float fontSize, float width) =>
            Mathf.Max(1, Mathf.CeilToInt(letters * fontSize * 0.45f / Mathf.Max(1f, width))) * fontSize * 1.2f;

        private static RectTransform NewRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Anchor(rect, 0f, 0f, 1f, 1f);
            return rect;
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

            // The content root in the safe area; one dark backing for the whole screen inside it, so the menu art never
            // competes with it.
            var content = _content = NewRect("VS_PowerUpContent", panel);
            content.SetAsFirstSibling();
            SoulHunter.Gameplay.UI.SafeAreaFitter.Fit(content);
            var backing = new GameObject("VS_PowerUpBacking", typeof(RectTransform), typeof(Image));
            var backingRect = (RectTransform)backing.transform;
            backingRect.SetParent(content, false);
            Anchor(backingRect, 0f, 0f, 1f, 1f);
            _backing = backingRect; // edges: Relayout
            var backingImage = backing.GetComponent<Image>();
            SoulHunter.Gameplay.UI.UITheme.ApplyPanel(backingImage, 0.93f);
            backingImage.raycastTarget = false;
            _inner = NewRect("VS_PowerUpInner", backingRect);

            // Header: the screen's title, and the gold on the right.
            _header = NewRect("VS_PowerUpHeader", _inner);
            _header.anchorMin = new Vector2(0f, 1f);
            _header.pivot = new Vector2(0.5f, 1f);
            _header.offsetMin = _header.offsetMax = Vector2.zero;
            _title = MakeText("VS_PowerUpTitle", _header, 0f, 0f, 1f, 1f, "Power Up", 56f, TextAlignmentOptions.Center,
                SoulHunter.Gameplay.UI.ThemedElement.TextRole.Title);
            _title.fontStyle = FontStyles.Bold;
            _titleRect = _title.rectTransform;
            var goldRow = NewRect("VS_GoldRow", _header);
            goldRow.anchorMin = new Vector2(1f, 0f);
            goldRow.anchorMax = new Vector2(1f, 1f);
            goldRow.pivot = new Vector2(1f, 0.5f);
            goldRow.sizeDelta = new Vector2(GoldWidth, 0f);
            goldRow.anchoredPosition = Vector2.zero;
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

            // Footer: Back when the header is too narrow for it (Relayout picks the header or the footer).
            _footer = NewRect("VS_PowerUpFooter", _inner);
            _footer.anchorMax = new Vector2(1f, 0f);
            _footer.pivot = new Vector2(0.5f, 0f);
            _footer.offsetMin = _footer.offsetMax = Vector2.zero;
            foreach (var button in panel.GetComponentsInChildren<Button>(true))
                if (button.name.Contains("Back"))
                {
                    _back = button;
                    button.transform.SetParent(_footer, false);
                    break;
                }

            // Body: the grid and the info box.
            _body = NewRect("VS_PowerUpBody", _inner);
            _gridArea = NewRect("VS_PowerUpGridArea", _body);
            _infoArea = NewRect("VS_PowerUpInfoArea", _body);
            _split = SoulHunter.Gameplay.UI.AdaptiveSplit.Between(_body, _gridArea, _infoArea, 0.62f, 0.42f); // upright: the info box needs room for its text and two stacked buttons (phone, 2026-10-10)

            // The grid: the wiki's order, as many columns as the width allows (at most 6), scrolling when it is taller than
            // its window.
            // Keyboard and gamepad: a focused cell scrolls into the grid's window (KeepSelectedInView).
            _gridScroll = SoulHunter.Gameplay.UI.ResponsiveScroll.Create(_gridArea, "VS_PowerUpGridViewport", "VS_PowerUpGrid", out _gridContent,
                sensitivity: 30f, fitContentHeight: true, keepSelectedInView: true);
            _gridViewport = _gridScroll.viewport;
            var layout = _gridContent.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(250f, 110f);
            layout.spacing = new Vector2(12f, 12f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 6;
            layout.childAlignment = TextAnchor.UpperCenter;
            var fit = _gridFit = _gridContent.gameObject.AddComponent<SoulHunter.Gameplay.UI.GridFit>();
            fit.Grid = layout;
            fit.Viewport = _gridViewport;
            fit.Count = PowerUps.All.Length;
            fit.MinCellWidth = MinCellWidth;
            fit.MaxColumns = 6;
            fit.MinHeightRatio = 0f; // Relayout: 0 on a PC (the rows fit, as approved), 0.6 on a short screen (rows scroll)
            foreach (var info in PowerUps.All)
            {
                var id = info.Id;
                var cell = Instantiate(template, _gridContent);
                cell.name = "PowerUp_" + id;
                cell.gameObject.SetActive(true);
                cell.onClick.RemoveAllListeners();
                cell.onClick.AddListener(() => Select(id));
                AddIcon(cell, id);
                _cells[id] = cell;
            }

            // The info box (approved): the selected PowerUp's big icon, then its name and rank, what it does and its price
            // (scrolling when the box is short), and Buy and Refund All, always shown (beside the text in a wide box, under
            // it in a narrow one).
            _infoBox = new GameObject("VS_PowerUpInfoBox", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _infoBox.SetParent(_infoArea, false);
            Anchor(_infoBox, 0f, 0f, 1f, 1f);
            var boxImage = _infoBox.GetComponent<Image>();
            SoulHunter.Gameplay.UI.UITheme.ApplySurface(boxImage, SoulHunter.Gameplay.UI.ThemedElement.SurfaceRole.Row);
            boxImage.raycastTarget = false;
            _boxInner = NewRect("VS_PowerUpInfoInner", _infoBox);
            _boxInner.offsetMin = new Vector2(BoxPadding, BoxPadding);
            _boxInner.offsetMax = new Vector2(-BoxPadding, -BoxPadding);

            _textArea = NewRect("VS_PowerUpInfoText", _boxInner);
            SoulHunter.Gameplay.UI.ResponsiveScroll.Create(_textArea, "Viewport", "Content", out var textContent,
                sensitivity: 30f, fitContentHeight: true);
            var line = textContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            line.spacing = 20f;
            line.childAlignment = TextAnchor.MiddleLeft;
            line.childControlWidth = line.childControlHeight = true;
            line.childForceExpandWidth = line.childForceExpandHeight = false;

            _bigIcon = MakeIcon("BigIcon", textContent, null, 128f);
            _bigIconLayout = Fixed(_bigIcon.gameObject, 128f);
            var words = new GameObject("Words", typeof(RectTransform)).GetComponent<RectTransform>();
            words.SetParent(textContent, false);
            words.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var flow = words.gameObject.AddComponent<VerticalLayoutGroup>();
            flow.spacing = 6f;
            flow.childAlignment = TextAnchor.MiddleLeft;
            flow.childControlWidth = flow.childControlHeight = true;
            flow.childForceExpandWidth = true;
            flow.childForceExpandHeight = false;
            _infoName = FlowText("Name", words, 38f, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Accent);
            _info = FlowText("Description", words, 28f, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Body);
            var priceRow = Row("PriceRow", words, 10f);
            _priceIcon = MakeIcon("PriceCoin", priceRow, SoulHunter.Gameplay.UI.UITheme.IconFor(SoulHunter.Gameplay.UI.UITheme.IconSlot.Coin), 40f);
            Fixed(_priceIcon.gameObject, 40f);
            _price = FlowText("Price", priceRow, 30f, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Gold);
            _price.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            // Buy and Refund All: one themed row of standard buttons, centred in its area; it wraps onto a second line in
            // a narrow box instead of shrinking.
            var actionArea = NewRect("VS_ActionArea", _boxInner);
            var actionRow = new GameObject("VS_ActionRow", typeof(RectTransform)).GetComponent<RectTransform>();
            actionRow.SetParent(actionArea, false);
            Anchor(actionRow, 0.5f, 0.5f, 0.5f, 0.5f);
            _buyButton = MakeButton(template, actionRow, "VS_Buy", "Buy", 0.5f, 0.5f, 0.5f, 0.5f, BuyOrSwitchSelected);
            _buyLabel = _buyButton.GetComponentInChildren<TextMeshProUGUI>();
            _refundButton = MakeButton(template, actionRow, "VS_Refund", "Refund All", 0.5f, 0.5f, 0.5f, 0.5f, Refund);
            actionRow.gameObject.AddComponent<SoulHunter.Gameplay.UI.ThemedRow>();
            _boxSplit = SoulHunter.Gameplay.UI.AdaptiveSplit.Between(_boxInner, _textArea, actionArea, 0.6f, 0.56f);
        }

        private static RectTransform Row(string name, RectTransform parent, float spacing)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var row = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = spacing;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            return rect;
        }

        private static LayoutElement Fixed(GameObject go, float size)
        {
            var element = go.AddComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = element.minHeight = element.preferredHeight = size;
            element.flexibleWidth = element.flexibleHeight = 0f;
            return element;
        }

        /// <summary>A text that flows in a layout (fixed size, wraps; its height follows its lines).</summary>
        private TextMeshProUGUI FlowText(string name, RectTransform parent, float size, SoulHunter.Gameplay.UI.ThemedElement.TextRole role)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) label.font = _font;
            label.enableAutoSizing = false;
            label.fontSize = Mathf.Max(size, SoulHunter.Gameplay.UI.UiScreen.MinTextUnits);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            SoulHunter.Gameplay.UI.UITheme.ApplyText(label, role);
            return label;
        }

        /// <summary>
        /// A smaller second line, but never under the text floor (12 sp on touch): on a phone it keeps its label's size.
        /// </summary>
        private static string Small(string text, float labelMinSize)
        {
            float floor = SoulHunter.Gameplay.UI.UiScreen.MinTextUnits;
            int percent = Mathf.Clamp(Mathf.CeilToInt(floor / Mathf.Max(1f, labelMinSize) * 100f), 70, 100);
            return percent >= 100 ? text : $"<size={percent}%>{text}</size>";
        }

        private TMP_FontAsset _font;
        private Image _bigIcon, _priceIcon, _goldCoin;
        private TextMeshProUGUI _infoName, _price, _buyLabel;
        private bool? _buyShowsSwitch; // which job the Buy button was last themed for (buy, or switch on/off)

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
            label.fontSizeMin = Mathf.Max(16f, SoulHunter.Gameplay.UI.UiScreen.MinTextUnits);
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

        /// <summary>
        /// The Buy button's job. VS (wiki PowerUps): "If it is fully upgraded, then there will be an option for the player to
        /// activate a PowerUp in most cases, except for Reroll, Skip, Banish, and Seal." So a maxed PowerUp's Buy becomes
        /// Turn Off / Turn On; anything else buys.
        /// </summary>
        public void BuyOrSwitchSelected()
        {
            var data = _saveService != null ? _saveService.CurrentData : null;
            if (data != null && PowerUps.CanToggle(data, _selected)) ToggleSelected();
            else BuySelected();
        }

        /// <summary>
        /// VS: switches the selected maxed PowerUp off or on, free and without a refund ("preventing the need to refund all
        /// PowerUps to test out PowerUp setups or to get rid of Curse"). Saved at once; the next run reads it.
        /// </summary>
        public void ToggleSelected()
        {
            var data = _saveService != null ? _saveService.CurrentData : null;
            if (data == null || !PowerUps.CanToggle(data, _selected)) return;
            PowerUps.SetEnabled(data, _selected, !PowerUps.IsEnabled(data, _selected));
            _saveService.SaveGame();
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.UIClickSound);
            UpdateUI();
        }

        /// <summary>VS "Refund PowerUps": every rank comes off and its gold comes back.</summary>
        public void Refund()
        {
            var data = _saveService != null ? _saveService.CurrentData : null;
            if (_economy == null || data == null || PowerUps.TotalBought(data) == 0) return;
            int value = PowerUps.RefundValue(data);
            data.PowerUpRanks.Clear();
            data.DisabledPowerUps?.Clear(); // a switched-off PowerUp is refunded too (its ranks were bought)
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
                bool off = maxed && !PowerUps.IsEnabled(data, pair.Key);
                bool chosen = pair.Key == _selected;
                var text = pair.Value.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    // Words carry every state, not colour alone: the rank, MAX, OFF, or LOCKED and what unlocks it.
                    string status = locked ? "LOCKED\n" + Small(PowerUpUnlocks.NextConditionText(data, pair.Key), text.fontSizeMin)
                        : off ? SoulHunter.Gameplay.UI.UITheme.Bold("OFF")
                        : maxed ? SoulHunter.Gameplay.UI.UITheme.Bold("MAX") : $"<color={cardSoft}>{rank}/{info.MaxRank}</color>";
                    text.text = $"{SoulHunter.Gameplay.UI.UITheme.Bold(info.Name)}\n{status}";
                    if (locked) _lockedBrief[text] = $"{SoulHunter.Gameplay.UI.UITheme.Bold(info.Name)}\nLOCKED"; else _lockedBrief.Remove(text);
                    _lockedFitDirty = true;
                    text.alignment = TextAlignmentOptions.MidlineLeft;
                    // The off cell is dark gray (VS), so it takes the light window text.
                    SoulHunter.Gameplay.UI.UITheme.ApplyText(text, locked ? SoulHunter.Gameplay.UI.ThemedElement.TextRole.Soft
                        : off ? SoulHunter.Gameplay.UI.ThemedElement.TextRole.Body
                        : SoulHunter.Gameplay.UI.ThemedElement.TextRole.Card);
                }
                var icon = pair.Value.transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null) icon.color = locked || off ? new Color(1f, 1f, 1f, 0.35f) : Color.white; // a locked or switched-off PowerUp's icon is faded
                // The cell keeps its state's colour; the chosen one also wears the theme's frame, as the chosen character card.
                // VS (wiki PowerUps): "fully upgraded and active ... yellowish ... fully upgraded and not active ... dark gray".
                pair.Value.image.color = locked ? LockedCell : off ? OffCell : maxed ? MaxedCell : chosen ? SelectedCell : PlainCell;
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
                if (_infoName != null) _infoName.text = $"{SoulHunter.Gameplay.UI.UITheme.Bold(info.Name)}  <color={darkSoft}>{Small($"Rank {rank}/{info.MaxRank}", _infoName.fontSize)}</color>";
                string unlock = condition == null ? "" : $"\n<color={darkSoft}>Unlock{(unlocked > 0 ? " next rank" : "")}: {condition}</color>";
                if (unlocked > 0 && unlocked < info.MaxRank) unlock = $"\n<color={darkSoft}>Unlocked ranks: {unlocked}/{info.MaxRank}</color>" + unlock;
                _info.text = info.Description + unlock;
                if (_price != null)
                {
                    int cost = PowerUps.Price(data, _selected);
                    bool afford = gold >= cost;
                    bool switchable = PowerUps.CanToggle(data, _selected);
                    string maxText = !switchable ? "MAX" : PowerUps.IsEnabled(data, _selected) ? "MAX - ON" : "MAX - OFF";
                    _price.text = maxed ? maxText : lockedRank ? "LOCKED" : afford ? $"Price: {cost}" : $"Price: {cost}  (need {cost - gold} more gold)";
                    SoulHunter.Gameplay.UI.UITheme.ApplyText(_price, maxed ? SoulHunter.Gameplay.UI.ThemedElement.TextRole.Accent
                        : lockedRank ? SoulHunter.Gameplay.UI.ThemedElement.TextRole.Soft
                        : afford ? SoulHunter.Gameplay.UI.ThemedElement.TextRole.Gold : SoulHunter.Gameplay.UI.ThemedElement.TextRole.Danger);
                    if (_priceIcon != null) _priceIcon.gameObject.SetActive(!maxed && !lockedRank);
                }
            }
            if (_buyButton != null)
            {
                // A maxed PowerUp (not Reroll/Skip/Banish) turns Buy into its switch: always usable, free, no coin icon.
                bool showsSwitch = data != null && PowerUps.CanToggle(data, _selected);
                if (_buyShowsSwitch != showsSwitch)
                {
                    _buyShowsSwitch = showsSwitch;
                    SoulHunter.Gameplay.UI.UITheme.ApplyButton(_buyButton, _buyButton.image, SoulHunter.Gameplay.UI.UITheme.ButtonKind.Primary,
                        showsSwitch ? SoulHunter.Gameplay.UI.UITheme.IconSlot.None : SoulHunter.Gameplay.UI.UITheme.IconSlot.Buy);
                }
                if (_buyLabel != null)
                    _buyLabel.text = !showsSwitch ? "Buy" : PowerUps.IsEnabled(data, _selected) ? "Turn Off" : "Turn On";
                _buyButton.interactable = showsSwitch
                    || (data != null && PowerUps.CanBuyNext(data, _selected) && _economy != null && gold >= PowerUps.Price(data, _selected));
            }
            if (_refundButton != null) _refundButton.interactable = data != null && PowerUps.TotalBought(data) > 0;
        }
    }
}
