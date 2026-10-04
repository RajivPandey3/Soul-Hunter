using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;
using SoulHunter.Core.Services;

namespace SoulHunter.UI
{
    /// <summary>
    /// Learning Comment:
    /// VS Rule: Main Menu mein game shuru karne se pehle player hero chunta hai.
    /// VS jaisi screen: beech mein saare characters ka grid (naam, model icon, starting weapon icon),
    /// baayein poora stat block (character + gold power-ups, yani run isi se shuru hoga),
    /// neeche chune hue character ki info, aur Confirm. Layout runtime par isi panel mein banta hai.
    /// Chunaav SaveService me mehfooz hota hai taake Gameplay scene mein wahi hero spawn ho.
    /// </summary>
    public class CharacterSelectionUI : MonoBehaviour
    {
        [Header("Character List")]
        [Tooltip("Saray characters yahan drag karein")]
        [SerializeField] private List<CharacterData> _availableCharacters;

        [Header("UI Elements")]
        [SerializeField] private Image _characterIconDisplay;
        [SerializeField] private TextMeshProUGUI _characterNameText;
        [SerializeField] private TextMeshProUGUI _characterDescriptionText;
        [SerializeField] private TextMeshProUGUI _statsText;

        [Header("Buttons")]
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _selectAndPlayButton;

        [Header("Grid Layout (VS style)")]
        [SerializeField] private Vector2 _cardSize = new Vector2(190f, 132f);
        [SerializeField, Min(1)] private int _gridColumns = 5; // 13 characters in 3 rows: all visible without scrolling
        // Colours come from the UI theme (Theme Studio); this screen keeps none of its own.
        private static Color BonusColor => SoulHunter.Gameplay.UI.UITheme.Accent;
        private static readonly Color LockedPortraitColor = new Color(0f, 0f, 0f, 0.9f); // a silhouette, not a look

        private int _currentIndex = 0;
        private readonly List<Button> _cards = new List<Button>();
        private readonly List<Image> _cardBackgrounds = new List<Image>();
        private readonly List<Image> _cardPortraits = new List<Image>();
        private readonly List<Outline> _cardOutlines = new List<Outline>();
        private readonly List<TextMeshProUGUI> _cardBadges = new List<TextMeshProUGUI>();
        private Image _weaponIconDisplay;
        private TextMeshProUGUI _weaponNameText;
        private TextMeshProUGUI _goldText;
        private TextMeshProUGUI _confirmLabel;
        private TextMeshProUGUI _filterButtonLabel;
        private TextMeshProUGUI _filterCountText;
        private readonly List<Toggle> _stageToggles = new List<Toggle>();
        private readonly List<Toggle> _modeToggles = new List<Toggle>();
        private TextMeshProUGUI _modeHintText;
        private ScrollRect _gridScroll;
        private readonly System.Collections.Generic.List<TextMeshProUGUI> _modeLabels = new System.Collections.Generic.List<TextMeshProUGUI>();
        private Dictionary<UpgradeData.UpgradeType, UpgradeData> _weaponInfo;
        private EconomyService _economy;

        // VS-style character filter: by the stage each character comes from.
        private bool _filterOn;
        private readonly bool[] _stageShown = { true, true, true, true, true, true, true, true, true, true };

        public int SelectedIndex => _currentIndex;
        public CharacterData SelectedCharacter =>
            _availableCharacters != null && _currentIndex >= 0 && _currentIndex < _availableCharacters.Count ? _availableCharacters[_currentIndex] : null;
        public IReadOnlyList<Button> Cards => _cards;
        public TextMeshProUGUI StatsText => _statsText;
        public string ConfirmText => _confirmLabel != null ? _confirmLabel.text : null;
        public bool ConfirmInteractable => _selectAndPlayButton != null && _selectAndPlayButton.interactable;
        public string GoldText => _goldText != null ? _goldText.text : null;
        public string BadgeText(int index) => index >= 0 && index < _cardBadges.Count ? _cardBadges[index].text : null;
        public bool FilterOn => _filterOn;
        public string FilterCountText => _filterCountText != null ? _filterCountText.text : null;
        public IReadOnlyList<Toggle> StageToggles => _stageToggles;
        /// <summary>The VS run mode toggles, in RunModeOptions.All order (Hurry, Hyper, Inverse, Endless, Arcana, Limit Break).</summary>
        public IReadOnlyList<Toggle> ModeToggles => _modeToggles;
        public string ModeHintText => _modeHintText != null ? _modeHintText.text : null;

        /// <summary>
        /// Turns a VS run mode on or off for the next runs. The choice is saved at once (GameData.Mode*), and the next run
        /// copies it into RunModes when it starts.
        /// </summary>
        public void SetMode(RunMode mode, bool on)
        {
            var data = SaveData();
            if (data == null) return;
            if (RunModeOptions.Get(data, mode) != on)
            {
                RunModeOptions.Set(data, mode, on);
                GameServices.Instance?.Get<SoulHunter.Core.Persistence.SaveService>()?.SaveGame();
                PlayUISound(false);
            }
            int index = System.Array.IndexOf(RunModeOptions.All, mode);
            if (index >= 0 && index < _modeToggles.Count && _modeToggles[index].isOn != on) _modeToggles[index].SetIsOnWithoutNotify(on);
            ShowModeHint(RunModeOptions.Description(mode));
        }

        private void ShowModeHint(string hint)
        {
            if (_modeHintText != null) _modeHintText.text = hint;
        }

        /// <summary>Turns the stage filter on or off (VS "FILTER: ON/OFF").</summary>
        public void SetFilter(bool on)
        {
            _filterOn = on;
            UpdateUI();
        }

        /// <summary>Shows or hides the characters that come from a stage while the filter is on.</summary>
        public void SetStageShown(int stage, bool shown)
        {
            if (stage < 1 || stage > _stageShown.Length) return;
            _stageShown[stage - 1] = shown;
            if (stage - 1 < _stageToggles.Count && _stageToggles[stage - 1].isOn != shown) _stageToggles[stage - 1].SetIsOnWithoutNotify(shown);
            UpdateUI();
        }

        public bool IsCardShown(int index) =>
            index >= 0 && index < _availableCharacters.Count &&
            (!_filterOn || _stageShown[Mathf.Clamp(_availableCharacters[index].OriginStage, 1, _stageShown.Length) - 1]);

        /// <summary>Redraws everything from the current save data (after an unlock or purchase).</summary>
        public void Refresh() => UpdateUI();

        private void Start()
        {
            if (_availableCharacters == null || _availableCharacters.Count == 0)
            {
                // Fallback Load from Resources if empty
                var catalog = GameContentCatalog.Load();
                _availableCharacters = new List<CharacterData>(catalog != null ? catalog.Characters : Resources.LoadAll<CharacterData>("Characters"));
            }
            _availableCharacters.RemoveAll(c => c == null);

            _currentIndex = Mathf.Max(0, _availableCharacters.FindIndex(c => c.CharacterName == SavedSelection()));
            if (GameServices.Instance != null && GameServices.Instance.TryGet(out _economy)) _economy.OnGoldChanged += HandleGoldChanged;
            BuildLayout();

            if (_availableCharacters.Count > 0)
            {
                UpdateUI();
            }

            if (_nextButton) _nextButton.onClick.AddListener(NextCharacter);
            if (_prevButton) _prevButton.onClick.AddListener(PrevCharacter);
            if (_selectAndPlayButton) _selectAndPlayButton.onClick.AddListener(ConfirmSelectionAndStart);
        }

        private void OnDestroy()
        {
            if (_nextButton) _nextButton.onClick.RemoveListener(NextCharacter);
            if (_prevButton) _prevButton.onClick.RemoveListener(PrevCharacter);
            if (_selectAndPlayButton) _selectAndPlayButton.onClick.RemoveListener(ConfirmSelectionAndStart);
            if (_economy != null) _economy.OnGoldChanged -= HandleGoldChanged;
        }

        private void HandleGoldChanged(int _) => UpdateUI();

        private void Update()
        {
            // Keyboard/gamepad: when the panel is open and nothing is focused, focus the selected card.
            if (_cards.Count == 0 || EventSystem.current == null) return;
            var card = _cards[Mathf.Clamp(_currentIndex, 0, _cards.Count - 1)];
            if (card != null && card.gameObject.activeInHierarchy && EventSystem.current.currentSelectedGameObject == null)
                EventSystem.current.SetSelectedGameObject(card.gameObject);
        }

        private void NextCharacter()
        {
            if (_availableCharacters.Count == 0) return;
            SelectCharacter((_currentIndex + 1) % _availableCharacters.Count);
        }

        private void PrevCharacter()
        {
            if (_availableCharacters.Count == 0) return;
            SelectCharacter((_currentIndex - 1 + _availableCharacters.Count) % _availableCharacters.Count);
        }

        /// <summary>Selects a character on the grid (click, keyboard/gamepad focus or Prev/Next).</summary>
        public void SelectCharacter(int index)
        {
            if (index < 0 || index >= _availableCharacters.Count) return;
            if (index != _currentIndex) PlayUISound(false);
            _currentIndex = index;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_currentIndex < 0 || _currentIndex >= _availableCharacters.Count) return;

            CharacterData charData = _availableCharacters[_currentIndex];
            var data = SaveData();
            bool unlocked = CharacterUnlocks.IsUnlocked(charData, data);
            bool owned = CharacterUnlocks.IsOwned(charData, data);

            if (_characterNameText) _characterNameText.text = charData.CharacterName;
            if (_characterDescriptionText)
            {
                _characterDescriptionText.text = unlocked
                    ? charData.Description
                    : $"<b>LOCKED</b>\nUnlock: {CharacterUnlocks.ConditionText(charData)}";
                if (unlocked && !owned) _characterDescriptionText.text += $"\n<b>Unlocked!</b> Buy for {charData.GoldPrice} gold.";
            }
            if (_characterIconDisplay)
            {
                _characterIconDisplay.sprite = charData.CharacterIcon;
                _characterIconDisplay.preserveAspect = true;
                _characterIconDisplay.color = unlocked ? Color.white : LockedPortraitColor;
            }

            // Cards: silhouette + LOCKED, price when buyable, filter by origin stage.
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _availableCharacters[i];
                bool cardUnlocked = CharacterUnlocks.IsUnlocked(card, data);
                bool cardOwned = CharacterUnlocks.IsOwned(card, data);
                if (i < _cardPortraits.Count && _cardPortraits[i] != null) _cardPortraits[i].color = cardUnlocked ? Color.white : LockedPortraitColor;
                if (i < _cardBadges.Count && _cardBadges[i] != null)
                    _cardBadges[i].text = !cardUnlocked ? "LOCKED" : cardOwned ? "" : $"{card.GoldPrice} G";
                _cards[i].gameObject.SetActive(IsCardShown(i));
            }

            // Confirm plays an owned character, buys an unlocked one, and is disabled for a locked one.
            int gold = _economy != null ? _economy.CurrentGold : 0;
            if (_goldText) _goldText.text = $"Gold: {gold}";
            if (_selectAndPlayButton)
                _selectAndPlayButton.interactable = owned || (unlocked && gold >= charData.GoldPrice);
            if (_confirmLabel)
                _confirmLabel.text = owned ? "Confirm" : unlocked ? $"Buy ({charData.GoldPrice} G)" : "Locked";

            if (_filterButtonLabel) _filterButtonLabel.text = _filterOn ? "FILTER: ON" : "FILTER: OFF";
            if (_filterCountText)
            {
                int shown = 0;
                foreach (bool s in _stageShown) if (s) shown++;
                _filterCountText.text = $"{shown}/{_stageShown.Length}";
            }

            var weapon = WeaponInfo(charData.StartingWeapon);
            if (_weaponIconDisplay)
            {
                _weaponIconDisplay.sprite = weapon != null ? weapon.Icon : null;
                _weaponIconDisplay.enabled = _weaponIconDisplay.sprite != null;
            }
            if (_weaponNameText) _weaponNameText.text = WeaponName(charData.StartingWeapon);

            UpdateStats(charData);

            for (int i = 0; i < _cardBackgrounds.Count; i++)
                if (_cardBackgrounds[i] != null) SoulHunter.Gameplay.UI.UITheme.ApplySurface(_cardBackgrounds[i],
                    i == _currentIndex ? SoulHunter.Gameplay.UI.ThemedElement.SurfaceRole.CardSelected : SoulHunter.Gameplay.UI.ThemedElement.SurfaceRole.Card);
            for (int i = 0; i < _cardOutlines.Count; i++)
                if (_cardOutlines[i] != null) _cardOutlines[i].enabled = i == _currentIndex;
            ScrollToSelected();
        }

        private void UpdateStats(CharacterData charData)
        {
            if (_statsText == null) return;
            var data = SaveData();
            var sheet = CharacterStatSheet.Build(charData, data);

            // One text with values in a column (<pos>), so labels and values always share a font size.
            var text = new System.Text.StringBuilder("<b>Stats</b>\n");
            string bonus = ColorUtility.ToHtmlStringRGB(BonusColor);
            foreach (var row in sheet.DisplayRows())
            {
                text.Append(row.Label).Append("<pos=68%>");
                text.Append(row.Changed ? $"<color=#{bonus}>{row.Value}</color>" : row.Value).Append('\n');
            }
            text.Append("\nStarting Weapon\n").Append(WeaponName(charData.StartingWeapon));
            _statsText.richText = true;
            SoulHunter.Gameplay.UI.UITheme.ApplyText(_statsText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Body);
            _statsText.text = text.ToString();
        }

        private void ScrollToSelected()
        {
            if (_gridScroll == null || _cards.Count == 0) return;
            int rows = Mathf.CeilToInt(_cards.Count / (float)_gridColumns);
            if (rows <= 1) return;
            int row = _currentIndex / _gridColumns;
            _gridScroll.verticalNormalizedPosition = 1f - row / (float)(rows - 1);
        }

        private void ConfirmSelectionAndStart()
        {
            if (_availableCharacters.Count == 0) return;

            PlayUISound(true);
            CharacterData charData = _availableCharacters[_currentIndex];

            // VS rule: a locked character cannot be picked; an unlocked one is bought first, then confirmed.
            var data = SaveData();
            if (!CharacterUnlocks.IsOwned(charData, data))
            {
                if (_economy != null && CharacterUnlocks.TryBuy(charData, data, _economy.SpendGold))
                {
                    GameServices.Instance.Get<SoulHunter.Core.Persistence.SaveService>()?.SaveGame();
                    Debug.Log($"[CharacterSelection] Bought {charData.CharacterName} for {charData.GoldPrice} gold.");
                }
                UpdateUI();
                return;
            }

            // Save selected character name to GameData
            if (GameServices.Instance != null)
            {
                var saveService = GameServices.Instance.Get<SoulHunter.Core.Persistence.SaveService>();
                if (saveService != null)
                {
                    saveService.CurrentData.SelectedCharacterName = charData.CharacterName;
                    saveService.SaveGame();
                    Debug.Log($"[CharacterSelection] Locked in {charData.CharacterName}! Starting game...");
                }
            }

            // Scene change logic goes here (assuming SceneService is used)
            var sceneService = GameServices.Instance?.Get<SoulHunter.Core.Scenes.SceneService>();
            if (sceneService != null)
            {
                sceneService.LoadSceneAsync("Main_Gameplay");
            }
            else
            {
                Debug.LogError("[CharacterSelection] Start from the Bootstrap scene; SceneService is unavailable.");
            }
        }

        // Yeh Quit button ke OnClick() par lagayenge
        public void QuitGame()
        {
            Debug.Log("[MainMenu] Quitting Game...");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        private static void PlayUISound(bool confirm)
        {
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(confirm ? audio.UIConfirmSound : audio.UIClickSound);
        }

        // ---------- Data ----------

        private static SoulHunter.Core.Persistence.GameData SaveData()
        {
            if (GameServices.Instance == null) return null;
            GameServices.Instance.TryGet(out SoulHunter.Core.Persistence.SaveService save);
            return save != null ? save.CurrentData : null;
        }

        private static string SavedSelection() => GameContentCatalog.CurrentName(SaveData()?.SelectedCharacterName);

        private UpgradeData WeaponInfo(UpgradeData.UpgradeType type)
        {
            if (_weaponInfo == null)
            {
                _weaponInfo = new Dictionary<UpgradeData.UpgradeType, UpgradeData>();
                foreach (var upgrade in Resources.LoadAll<UpgradeData>("Upgrades"))
                    if (upgrade != null && upgrade.Level == 1 && !_weaponInfo.ContainsKey(upgrade.Type)) _weaponInfo[upgrade.Type] = upgrade;
            }
            return _weaponInfo.TryGetValue(type, out var info) ? info : null;
        }

        private string WeaponName(UpgradeData.UpgradeType type)
        {
            var info = WeaponInfo(type);
            string name = info != null && !string.IsNullOrEmpty(info.UpgradeName) ? info.UpgradeName : type.ToString();
            // "MagicWand Lv.1" -> "Magic Wand"
            name = System.Text.RegularExpressions.Regex.Replace(name, @"\s*Lv\.?\s*\d+$", "");
            return System.Text.RegularExpressions.Regex.Replace(name, @"(?<=[a-z])(?=[A-Z])", " ");
        }

        // ---------- Layout ----------

        private void BuildLayout()
        {
            var root = PanelRoot();
            if (root == null) return;

            MainMenuNavigation.ScaleWithScreen(root.GetComponentInParent<CanvasScaler>()); // also set by the menu at load

            // Title and Back along the top.
            var title = root.Find("Text (TMP)") as RectTransform;
            if (title != null && title.TryGetComponent(out TextMeshProUGUI titleText))
            {
                Place(title, root, new Vector2(0.27f, 0.895f), new Vector2(0.73f, 0.975f));
                titleText.text = "Character Selection";
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.fontStyle = FontStyles.Bold;
                SoulHunter.Gameplay.UI.UITheme.ApplyText(titleText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Title);
                AutoSize(titleText, 24f, 56f);
            }
            var back = root.Find("Back_Button") as RectTransform;
            if (back != null) Place(back, root, new Vector2(0.78f, 0.9f), new Vector2(0.95f, 0.97f));

            // Prev/Next are replaced by the grid.
            if (_nextButton) _nextButton.gameObject.SetActive(false);
            if (_prevButton) _prevButton.gameObject.SetActive(false);

            // Left: stat block (labels left, values right).
            var statsBox = CreatePanel("StatsPanel", root, new Vector2(0.03f, 0.08f), new Vector2(0.25f, 0.88f));
            if (_statsText != null)
            {
                Place(_statsText.rectTransform, statsBox, new Vector2(0.07f, 0.03f), new Vector2(0.93f, 0.97f));
                _statsText.alignment = TextAlignmentOptions.TopLeft;
                _statsText.richText = true;
                AutoSize(_statsText, 12f, 28f);
            }

            // Centre: scrollable grid of character cards.
            var gridBox = CreatePanel("CharacterGrid", root, new Vector2(0.27f, 0.33f), new Vector2(0.73f, 0.785f));
            var viewport = CreateRect("Viewport", gridBox, Vector2.zero, Vector2.one);
            viewport.offsetMin = new Vector2(10f, 10f);
            viewport.offsetMax = new Vector2(-10f, -10f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = CreateRect("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f));
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = _cardSize;
            grid.spacing = new Vector2(10f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = _gridColumns;
            grid.childAlignment = TextAnchor.UpperCenter;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            // The cards are sized from the box, so every card fits whatever the window (they were a fixed 190x132).
            var fit = content.gameObject.AddComponent<GridFit>();
            fit.Grid = grid;
            fit.Viewport = viewport;
            fit.Count = _availableCharacters.Count;
            _gridScroll = gridBox.gameObject.AddComponent<ScrollRect>();
            _gridScroll.horizontal = false;
            _gridScroll.movementType = ScrollRect.MovementType.Clamped;
            _gridScroll.scrollSensitivity = 30f;
            _gridScroll.viewport = viewport;
            _gridScroll.content = content;
            for (int i = 0; i < _availableCharacters.Count; i++) BuildCard(i, content);

            // Bottom centre: the selected character.
            var infoBox = CreatePanel("SelectedInfo", root, new Vector2(0.27f, 0.08f), new Vector2(0.73f, 0.31f));
            if (_characterIconDisplay != null) Place(_characterIconDisplay.rectTransform, infoBox, new Vector2(0.02f, 0.08f), new Vector2(0.2f, 0.92f));
            if (_characterNameText != null)
            {
                Place(_characterNameText.rectTransform, infoBox, new Vector2(0.22f, 0.64f), new Vector2(0.78f, 0.95f));
                _characterNameText.alignment = TextAlignmentOptions.TopLeft;
                AutoSize(_characterNameText, 16f, 40f);
            }
            if (_characterDescriptionText != null)
            {
                Place(_characterDescriptionText.rectTransform, infoBox, new Vector2(0.22f, 0.06f), new Vector2(0.78f, 0.62f));
                _characterDescriptionText.alignment = TextAlignmentOptions.TopLeft;
                AutoSize(_characterDescriptionText, 12f, 28f);
            }
            _weaponIconDisplay = CreateImage("StartingWeapon_Icon", infoBox, new Vector2(0.81f, 0.3f), new Vector2(0.97f, 0.92f), null);
            _weaponNameText = CreateText("StartingWeapon_Text", infoBox, new Vector2(0.79f, 0.04f), new Vector2(0.99f, 0.28f), "", TextAlignmentOptions.Center, 12f, 18f);

            // Bottom right: Confirm.
            if (_selectAndPlayButton != null)
            {
                Place((RectTransform)_selectAndPlayButton.transform, root, new Vector2(0.76f, 0.08f), new Vector2(0.95f, 0.19f));
                _confirmLabel = _selectAndPlayButton.GetComponentInChildren<TextMeshProUGUI>();
                if (_confirmLabel != null) _confirmLabel.text = "Confirm";
                SoulHunter.Gameplay.UI.UITheme.ApplyButton(_selectAndPlayButton, _selectAndPlayButton.targetGraphic as Image,
                    SoulHunter.Gameplay.UI.UITheme.ButtonKind.Primary, SoulHunter.Gameplay.UI.UITheme.IconSlot.Confirm);
            }

            // Top left: gold, which buys unlocked characters.
            _goldText = CreateText("Gold_Text", root, new Vector2(0.03f, 0.9f), new Vector2(0.25f, 0.97f), "", TextAlignmentOptions.Left, 18f, 32f);
            SoulHunter.Gameplay.UI.UITheme.ApplyText(_goldText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Accent);

            // Right: VS-style character filter by the stage each character comes from.
            BuildFilterPanel(root);

            // Above the grid: VS run modes (Hurry, Hyper, Inverse, Endless, Arcana, Limit Break).
            BuildModesRow(root);
        }

        /// <summary>
        /// VS "List of Modes" toggles in one row above the character grid, with a line under them that says what the
        /// focused (or hovered) mode does. Keyboard/gamepad focus uses the options screen's orange focus colour.
        /// </summary>
        private void BuildModesRow(RectTransform root)
        {
            var box = CreatePanel("RunModes", root, new Vector2(0.27f, 0.795f), new Vector2(0.73f, 0.88f));
            var data = SaveData();
            int count = RunModeOptions.All.Length;
            float width = 0.98f / count;
            for (int i = 0; i < count; i++)
            {
                var mode = RunModeOptions.All[i];
                float left = 0.01f + i * width;
                var cell = CreateRect($"Mode_{mode}", box, new Vector2(left + 0.004f, 0.44f), new Vector2(left + width - 0.004f, 0.95f));
                var background = cell.gameObject.AddComponent<Image>();
                var toggle = cell.gameObject.AddComponent<Toggle>();
                toggle.targetGraphic = background;
                SoulHunter.Gameplay.UI.UITheme.ApplyButton(toggle, background, SoulHunter.Gameplay.UI.UITheme.ButtonKind.Normal, SoulHunter.Gameplay.UI.UITheme.IconSlot.None, false);

                var tickBox = CreateRect("Box", cell, new Vector2(0.04f, 0.18f), new Vector2(0.2f, 0.82f));
                var tickBoxImage = tickBox.gameObject.AddComponent<Image>();
                SoulHunter.Gameplay.UI.UITheme.ApplySurface(tickBoxImage, SoulHunter.Gameplay.UI.ThemedElement.SurfaceRole.Row);
                tickBoxImage.raycastTarget = false;
                tickBox.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
                var check = CreateRect("Check", tickBox, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
                var checkImage = check.gameObject.AddComponent<Image>();
                SoulHunter.Gameplay.UI.UITheme.ApplySurface(checkImage, SoulHunter.Gameplay.UI.ThemedElement.SurfaceRole.Accent); // tick = on
                checkImage.raycastTarget = false;
                toggle.graphic = checkImage;

                var label = CreateText("Label", cell, new Vector2(0.24f, 0f), new Vector2(0.98f, 1f),
                    RunModeOptions.Name(mode), TextAlignmentOptions.Left, 8f, 20f);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                _modeLabels.Add(label);

                toggle.SetIsOnWithoutNotify(RunModeOptions.Get(data, mode));
                toggle.onValueChanged.AddListener(on => SetMode(mode, on));
                var hint = cell.gameObject.AddComponent<FocusHint>();
                hint.Hint = RunModeOptions.Description(mode);
                hint.Show = ShowModeHint;
                _modeToggles.Add(toggle);
            }
            // One size for all six labels (auto-size gave "Limit Break" a much smaller size than "Hurry").
            box.gameObject.AddComponent<SameFontSize>().Labels = _modeLabels.ToArray();
            _modeHintText = CreateText("ModeHint", box, new Vector2(0.015f, 0.04f), new Vector2(0.985f, 0.42f),
                "Modes: select one to see what it does.", TextAlignmentOptions.Left, 10f, 18f);
            SoulHunter.Gameplay.UI.UITheme.ApplyText(_modeHintText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Soft);
        }

        private void BuildFilterPanel(RectTransform root)
        {
            var toggleButton = CreatePanel("FilterButton", root, new Vector2(0.76f, 0.82f), new Vector2(0.95f, 0.88f));
            var filterButton = toggleButton.gameObject.AddComponent<Button>();
            filterButton.targetGraphic = toggleButton.GetComponent<Image>();
            filterButton.onClick.AddListener(() => SetFilter(!_filterOn));
            _filterButtonLabel = CreateText("Label", toggleButton, Vector2.zero, Vector2.one, "FILTER: OFF", TextAlignmentOptions.Center, 14f, 20f);
            SoulHunter.Gameplay.UI.UITheme.ApplyButton(filterButton, toggleButton.GetComponent<Image>(), SoulHunter.Gameplay.UI.UITheme.ButtonKind.Normal);

            var list = CreatePanel("StageFilter", root, new Vector2(0.76f, 0.22f), new Vector2(0.95f, 0.8f));
            _filterCountText = CreateText("Count", list, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.99f), "", TextAlignmentOptions.Center, 14f, 24f);
            int stages = _stageShown.Length;
            float rowHeight = 0.88f / stages;
            for (int stage = 1; stage <= stages; stage++)
            {
                float top = 0.89f - (stage - 1) * rowHeight;
                var row = CreateRect($"Stage_{stage}", list, new Vector2(0.05f, top - rowHeight), new Vector2(0.97f, top));
                var box = CreateRect("Box", row, new Vector2(0f, 0.18f), new Vector2(0.1f, 0.82f));
                var boxImage = box.gameObject.AddComponent<Image>();
                SoulHunter.Gameplay.UI.UITheme.ApplySurface(boxImage, SoulHunter.Gameplay.UI.ThemedElement.SurfaceRole.Row);
                var check = CreateRect("Check", box, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
                var checkImage = check.gameObject.AddComponent<Image>();
                SoulHunter.Gameplay.UI.UITheme.ApplySurface(checkImage, SoulHunter.Gameplay.UI.ThemedElement.SurfaceRole.Accent); // tick = shown
                checkImage.raycastTarget = false;
                var toggle = row.gameObject.AddComponent<Toggle>();
                toggle.targetGraphic = boxImage;
                toggle.graphic = checkImage;
                toggle.isOn = _stageShown[stage - 1];
                int s = stage;
                toggle.onValueChanged.AddListener(on => SetStageShown(s, on));
                // One fixed size for every row (auto-size would give each name its own size).
                var label = CreateText("Label", row, new Vector2(0.14f, 0f), new Vector2(1f, 1f),
                    CampaignLevelCatalog.Get(stage).Name, TextAlignmentOptions.Left, 17f, 17f);
                label.enableAutoSizing = false;
                label.fontSize = 17f;
                label.overflowMode = TextOverflowModes.Ellipsis;
                _stageToggles.Add(toggle);
            }
        }

        private void BuildCard(int index, RectTransform content)
        {
            var character = _availableCharacters[index];
            var card = CreateRect($"Card_{character.CharacterName}", content, Vector2.zero, Vector2.one);
            var background = card.gameObject.AddComponent<Image>();
            SoulHunter.Gameplay.UI.UITheme.ApplySurface(background, SoulHunter.Gameplay.UI.ThemedElement.SurfaceRole.Card);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => SelectCharacter(index));
            var focus = card.gameObject.AddComponent<CharacterCard>();
            focus.Index = index;
            focus.Selected = SelectCharacter;

            SoulHunter.Gameplay.UI.UITheme.ApplyText(CreateText("Name", card, new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.97f), character.CharacterName,
                TextAlignmentOptions.TopLeft, 12f, 24f), SoulHunter.Gameplay.UI.ThemedElement.TextRole.Card);
            var portrait = CreateImage("Portrait", card, new Vector2(0.05f, 0.05f), new Vector2(0.6f, 0.74f), character.CharacterIcon);
            var weapon = WeaponInfo(character.StartingWeapon);
            CreateImage("Weapon", card, new Vector2(0.62f, 0.05f), new Vector2(0.95f, 0.42f), weapon != null ? weapon.Icon : null);
            var badge = CreateText("Badge", card, new Vector2(0.45f, 0.5f), new Vector2(0.97f, 0.74f), "", TextAlignmentOptions.Right, 8f, 18f);
            badge.textWrappingMode = TextWrappingModes.NoWrap; // "LOCKED" wrapped to "LOCKE / D"
            SoulHunter.Gameplay.UI.UITheme.ApplyText(badge, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Tag);

            _cards.Add(button);
            _cardBackgrounds.Add(background);
            _cardPortraits.Add(portrait);
            // The selected card gets a white outline as well as its colour, so selection never depends on colour alone.
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = SoulHunter.Gameplay.UI.UITheme.Focus; // the theme's focus colour around the selected card
            outline.effectDistance = new Vector2(4f, -4f);
            outline.enabled = false;
            _cardOutlines.Add(outline);
            _cardBadges.Add(badge);
        }

        private RectTransform PanelRoot()
        {
            Transform anchor = _characterNameText != null ? _characterNameText.transform
                : _selectAndPlayButton != null ? _selectAndPlayButton.transform : null;
            return anchor != null ? anchor.parent as RectTransform : null;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private RectTransform CreatePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = CreateRect(name, parent, anchorMin, anchorMax);
            var image = rect.gameObject.AddComponent<Image>();
            SoulHunter.Gameplay.UI.UITheme.ApplyPanel(image);
            rect.SetSiblingIndex(0); // behind the scene's own texts and buttons
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Sprite sprite)
        {
            var image = CreateRect(name, parent, anchorMin, anchorMax).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = sprite != null;
            return image;
        }

        private TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, string text, TextAlignmentOptions alignment, float minSize, float maxSize)
        {
            var label = CreateRect(name, parent, anchorMin, anchorMax).gameObject.AddComponent<TextMeshProUGUI>();
            if (_characterNameText != null) label.font = _characterNameText.font;
            label.text = text;
            label.alignment = alignment;
            SoulHunter.Gameplay.UI.UITheme.ApplyText(label, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Body);
            label.raycastTarget = false;
            AutoSize(label, minSize, maxSize);
            return label;
        }

        private static void Place(RectTransform rect, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void AutoSize(TextMeshProUGUI text, float min, float max)
        {
            // The panel is built while it is still inactive, so TextMeshPro's Awake runs later, when the panel opens. A text
            // whose size was never set gets TMP's defaults there (36, auto-size 18-72), which replaced these limits: Gold,
            // FILTER, Whip and the LOCKED badges showed at up to 72 (found 2026-09-30). A set size keeps them.
            text.fontSize = max;
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
        }
    }

    /// <summary>Sizes a grid's cells from its viewport: all Count cards fit in the grid's columns, 0.72 as tall as wide at most.</summary>
    public sealed class GridFit : MonoBehaviour
    {
        public GridLayoutGroup Grid;
        public RectTransform Viewport;
        public int Count;
        private Vector2 _lastSize;

        private void LateUpdate()
        {
            if (Grid == null || Viewport == null || Count <= 0) return;
            Vector2 size = Viewport.rect.size;
            if (size == _lastSize || size.x <= 0f || size.y <= 0f) return;
            _lastSize = size;
            int columns = Mathf.Max(1, Grid.constraintCount);
            int rows = Mathf.CeilToInt(Count / (float)columns);
            float width = (size.x - Grid.spacing.x * (columns - 1)) / columns;
            float height = Mathf.Min(width * 0.72f, (size.y - Grid.spacing.y * (rows - 1)) / rows);
            Grid.cellSize = new Vector2(Mathf.Floor(width), Mathf.Floor(Mathf.Max(height, width * 0.45f)));
        }
    }

    /// <summary>Gives every label the size the tightest of them needs (after auto-size has measured them once).</summary>
    public sealed class SameFontSize : MonoBehaviour
    {
        public TextMeshProUGUI[] Labels;
        private Vector2 _lastSize;

        private void LateUpdate()
        {
            if (Labels == null || Labels.Length == 0 || Labels[0] == null) return;
            Vector2 size = Labels[0].rectTransform.rect.size;
            if (size == _lastSize || size.x <= 0f) return;
            _lastSize = size;
            float smallest = float.MaxValue;
            foreach (var label in Labels)
            {
                label.enableAutoSizing = true;
                label.ForceMeshUpdate();
                smallest = Mathf.Min(smallest, label.fontSize);
            }
            foreach (var label in Labels)
            {
                label.enableAutoSizing = false;
                label.fontSize = smallest;
            }
        }
    }
}
