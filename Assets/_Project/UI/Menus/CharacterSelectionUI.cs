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

        /// <summary>
        /// VS sort (vampire.survivors.wiki/w/Characters, Filtering/sorting, fetched 2026-10-09): "through the default
        /// character order, alphabetically, or through the number of complete stages per character, and the sorting can
        /// either be in ascending or descending order. In any case, favorited characters will show up first".
        /// </summary>
        public enum CharacterSort { Default, Alphabetical, StagesCompleted }

        // The wiki does not say whether the sort is kept between sessions; like the level filter (not saved either), it
        // lasts while the menu is open, so the two FILTER-panel choices behave the same (balance method, 2026-10-09).
        private CharacterSort _sort = CharacterSort.Default;
        private bool _sortDescending;
        private TextMeshProUGUI _sortLabel, _orderLabel, _favouriteLabel;
        private Button _musicButton;
        private TextMeshProUGUI _musicCardText;
        private MusicSelectionScreen _musicScreen;
        /// <summary>The Music card beside the modes (VS Music Selection) and its screen once opened (tests).</summary>
        public Button MusicButton => _musicButton;
        public string MusicCardText => _musicCardText != null ? _musicCardText.text : null;
        public MusicSelectionScreen MusicScreen => _musicScreen;
        private Button _favouriteButton;
        private readonly List<int> _displayOrder = new List<int>();

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
        /// <summary>The VS run mode toggles, in RunModeOptions.All order (Hurry, Hyper, Inverse, Endless, Arcana, Limit Break, Omens, Blind Fate).</summary>
        public IReadOnlyList<Toggle> ModeToggles => _modeToggles;
        public string ModeHintText => _modeHintText != null ? _modeHintText.text : null;
        public CharacterSort Sort => _sort;
        public bool SortDescending => _sortDescending;
        public string SortText => _sortLabel != null ? _sortLabel.text : null;
        public string OrderText => _orderLabel != null ? _orderLabel.text : null;
        public string FavouriteText => _favouriteLabel != null ? _favouriteLabel.text : null;
        public Button FavouriteButton => _favouriteButton;
        /// <summary>Character indices in grid order (favourites, then the sort); hidden ones included (tests).</summary>
        public IReadOnlyList<int> DisplayOrder => _displayOrder;
        /// <summary>The characters the grid shows, in grid order (tests).</summary>
        public List<string> VisibleNames()
        {
            var names = new List<string>();
            foreach (int i in _displayOrder) if (IsCardShown(i)) names.Add(_availableCharacters[i].CharacterName);
            return names;
        }

        /// <summary>Picks the VS sort (default order, A-Z, or levels completed).</summary>
        public void SetSort(CharacterSort sort)
        {
            if (_sort != sort) PlayUISound(false);
            _sort = sort;
            UpdateUI();
        }

        /// <summary>Ascending or descending, for any sort (VS).</summary>
        public void SetSortDescending(bool descending)
        {
            if (_sortDescending != descending) PlayUISound(false);
            _sortDescending = descending;
            UpdateUI();
        }

        /// <summary>
        /// Marks or clears the selected character as a favourite (VS: from the character information panel). Locked until
        /// Mindbender, as in VS (<see cref="SoulHunter.Core.Persistence.CharacterFavourites"/>); saved at once.
        /// </summary>
        public void SetFavourite(bool favourite)
        {
            var data = SaveData();
            var character = SelectedCharacter;
            if (data == null || character == null || !SoulHunter.Core.Persistence.CharacterFavourites.IsUnlocked(data)) return;
            if (SoulHunter.Core.Persistence.CharacterFavourites.IsFavourite(data, character.CharacterName) == favourite) return;
            SoulHunter.Core.Persistence.CharacterFavourites.Set(data, character.CharacterName, favourite);
            GameServices.Instance?.Get<SoulHunter.Core.Persistence.SaveService>()?.SaveGame();
            PlayUISound(false);
            UpdateUI();
        }

        private void ToggleFavourite()
        {
            var character = SelectedCharacter;
            if (character != null) SetFavourite(!SoulHunter.Core.Persistence.CharacterFavourites.IsFavourite(SaveData(), character.CharacterName));
        }

        /// <summary>
        /// Grid order: favourites first, then the sort, ties in the default order. Descending reverses the sort key, not the
        /// tie order (the wiki does not say; a stable default order keeps equal characters where the player expects them).
        /// </summary>
        public static List<int> Order(IReadOnlyList<CharacterData> characters, SoulHunter.Core.Persistence.GameData data, CharacterSort sort, bool descending)
        {
            var order = new List<int>();
            for (int i = 0; i < characters.Count; i++) order.Add(i);
            int Key(int a, int b)
            {
                switch (sort)
                {
                    case CharacterSort.Alphabetical:
                        return string.Compare(characters[a].CharacterName, characters[b].CharacterName, System.StringComparison.OrdinalIgnoreCase);
                    case CharacterSort.StagesCompleted:
                        return SoulHunter.Core.Persistence.CharacterStages.Count(data, characters[a].CharacterName)
                            .CompareTo(SoulHunter.Core.Persistence.CharacterStages.Count(data, characters[b].CharacterName));
                    default:
                        return a.CompareTo(b);
                }
            }
            order.Sort((a, b) =>
            {
                bool fa = SoulHunter.Core.Persistence.CharacterFavourites.IsFavourite(data, characters[a].CharacterName);
                bool fb = SoulHunter.Core.Persistence.CharacterFavourites.IsFavourite(data, characters[b].CharacterName);
                if (fa != fb) return fa ? -1 : 1;
                int key = Key(a, b);
                if (descending) key = -key;
                return key != 0 ? key : a.CompareTo(b);
            });
            return order;
        }

        private void ApplyOrder()
        {
            _displayOrder.Clear();
            _displayOrder.AddRange(Order(_availableCharacters, SaveData(), _sort, _sortDescending));
            for (int position = 0; position < _displayOrder.Count; position++)
            {
                int index = _displayOrder[position];
                if (index < _cards.Count && _cards[index] != null) _cards[index].transform.SetSiblingIndex(position);
            }
        }

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
            // VS (wiki Characters): "the player can only keep at least one group of characters visible". Soul Hunter's
            // groups are origin levels, and some levels have no character, so an untick that would leave the filtered
            // grid without a character is refused and the box stays ticked.
            if (!shown && _stageShown[stage - 1])
            {
                _stageShown[stage - 1] = false;
                bool anyLeft = false;
                for (int i = 0; i < _availableCharacters.Count && !anyLeft; i++) anyLeft = ShownByLevels(i);
                if (!anyLeft) shown = true;
            }
            _stageShown[stage - 1] = shown;
            if (stage - 1 < _stageToggles.Count && _stageToggles[stage - 1].isOn != shown) _stageToggles[stage - 1].SetIsOnWithoutNotify(shown);
            UpdateUI();
        }

        private bool ShownByLevels(int index) =>
            _stageShown[Mathf.Clamp(_availableCharacters[index].OriginStage, 1, _stageShown.Length) - 1];

        public bool IsCardShown(int index) =>
            index >= 0 && index < _availableCharacters.Count &&
            (!_filterOn || ShownByLevels(index)); // a favourite hidden by the filter stays hidden (VS: "but not if it is filtered")

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
            if (_musicScreen != null && _musicScreen.IsOpen) return; // the Music screen keeps its own focus
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
                    : $"{SoulHunter.Gameplay.UI.UITheme.Bold("LOCKED")}\nUnlock: {CharacterUnlocks.ConditionText(charData)}";
                if (unlocked && !owned) _characterDescriptionText.text += $"\n{SoulHunter.Gameplay.UI.UITheme.Bold("Unlocked!")} Buy for {CharacterUnlocks.PriceFor(charData, data)} gold.";
            }
            if (_characterIconDisplay)
            {
                _characterIconDisplay.sprite = charData.CharacterIcon;
                _characterIconDisplay.preserveAspect = true;
                _characterIconDisplay.color = unlocked ? Color.white : LockedPortraitColor;
            }

            // Cards: silhouette + LOCKED, price when buyable, FAVOURITE in words, filter by origin stage, VS order.
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _availableCharacters[i];
                bool cardUnlocked = CharacterUnlocks.IsUnlocked(card, data);
                bool cardOwned = CharacterUnlocks.IsOwned(card, data);
                bool favourite = SoulHunter.Core.Persistence.CharacterFavourites.IsFavourite(data, card.CharacterName);
                if (i < _cardPortraits.Count && _cardPortraits[i] != null) _cardPortraits[i].color = cardUnlocked ? Color.white : LockedPortraitColor;
                if (i < _cardBadges.Count && _cardBadges[i] != null)
                    _cardBadges[i].text = !cardUnlocked ? "LOCKED" : !cardOwned ? $"{CharacterUnlocks.PriceFor(card, data)} G" : favourite ? "FAVOURITE" : "";
                _cards[i].gameObject.SetActive(IsCardShown(i));
            }
            ApplyOrder();

            if (_sortLabel) _sortLabel.text = _sort == CharacterSort.Alphabetical ? "SORT: A-Z" : _sort == CharacterSort.StagesCompleted ? "SORT: LEVELS" : "SORT: DEFAULT";
            if (_orderLabel) _orderLabel.text = _sortDescending ? "DESCENDING" : "ASCENDING";
            bool favouritesOpen = SoulHunter.Core.Persistence.CharacterFavourites.IsUnlocked(data);
            if (_favouriteButton)
            {
                _favouriteButton.interactable = favouritesOpen;
                // A locked button shows its label in the theme's locked text colour, readable on the disabled plate.
                SoulHunter.Gameplay.UI.ThemedElement.For(_favouriteButton.gameObject).ShowUsable(favouritesOpen);
            }
            if (_favouriteLabel)
                _favouriteLabel.text = !favouritesOpen ? "FAVOURITE: LOCKED"
                    : SoulHunter.Core.Persistence.CharacterFavourites.IsFavourite(data, charData.CharacterName) ? "FAVOURITE: ON" : "FAVOURITE: OFF";

            // Confirm plays an owned character, buys an unlocked one, and is disabled for a locked one.
            int gold = _economy != null ? _economy.CurrentGold : 0;
            if (_goldText) _goldText.text = $"Gold: {gold}";
            if (_selectAndPlayButton)
                _selectAndPlayButton.interactable = owned || (unlocked && gold >= CharacterUnlocks.PriceFor(charData, data));
            if (_confirmLabel)
                _confirmLabel.text = owned ? "Confirm" : unlocked ? $"Buy ({CharacterUnlocks.PriceFor(charData, data)} G)" : "Locked";

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
                if (_cardOutlines[i] != null) { ApplySelectedEdge(_cardOutlines[i]); _cardOutlines[i].enabled = i == _currentIndex; }
            ScrollToSelected();
        }

        private void UpdateStats(CharacterData charData)
        {
            if (_statsText == null) return;
            var data = SaveData();
            var sheet = CharacterStatSheet.Build(charData, data);

            // One text with values in a column (<pos>), so labels and values always share a font size.
            var text = new System.Text.StringBuilder(SoulHunter.Gameplay.UI.UITheme.Bold("Stats") + "\n");
            string bonus = ColorUtility.ToHtmlStringRGB(BonusColor);
            foreach (var row in sheet.DisplayRows())
            {
                text.Append(row.Label).Append("<pos=68%>");
                text.Append(row.Changed ? $"<color=#{bonus}>{row.Value}</color>" : row.Value).Append('\n');
            }
            text.Append("\nStarting Weapon\n").Append(WeaponName(charData.StartingWeapon));
            // VS stage completion panel (wiki Characters): what the "levels" sort counts for this character.
            text.Append("\nLevels Completed<pos=68%>")
                .Append(SoulHunter.Core.Persistence.CharacterStages.Count(data, charData.CharacterName))
                .Append('/').Append(SoulHunter.Core.Persistence.CharacterStages.LevelCount);
            _statsText.richText = true;
            SoulHunter.Gameplay.UI.UITheme.ApplyText(_statsText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Body);
            _statsText.text = text.ToString();
        }

        private void ScrollToSelected()
        {
            if (_gridScroll == null || _cards.Count == 0) return;
            int rows = Mathf.CeilToInt(_cards.Count / (float)_gridColumns);
            if (rows <= 1) return;
            // The selected card's place among the shown cards, in grid order (sort and filter move it).
            int position = 0;
            foreach (int i in _displayOrder)
            {
                if (i == _currentIndex) break;
                if (IsCardShown(i)) position++;
            }
            int row = position / _gridColumns;
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
                int price = CharacterUnlocks.PriceFor(charData, data);
                if (_economy != null && CharacterUnlocks.TryBuy(charData, data, _economy.SpendGold))
                {
                    GameServices.Instance.Get<SoulHunter.Core.Persistence.SaveService>()?.SaveGame();
                    Debug.Log($"[CharacterSelection] Bought {charData.CharacterName} for {price} gold.");
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
            var gridBox = CreatePanel("CharacterGrid", root, new Vector2(0.27f, 0.33f), new Vector2(0.73f, 0.725f));
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
                SoulHunter.Gameplay.UI.UITheme.ApplyText(_characterNameText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Accent); // the scene's text takes the theme's font
            }
            if (_characterDescriptionText != null)
            {
                Place(_characterDescriptionText.rectTransform, infoBox, new Vector2(0.22f, 0.06f), new Vector2(0.78f, 0.62f));
                _characterDescriptionText.alignment = TextAlignmentOptions.TopLeft;
                AutoSize(_characterDescriptionText, 12f, 28f);
                SoulHunter.Gameplay.UI.UITheme.ApplyText(_characterDescriptionText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Body);
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
            var box = CreatePanel("RunModes", root, new Vector2(0.265f, 0.735f), new Vector2(0.735f, 0.88f));
            var data = SaveData();
            int count = RunModeOptions.All.Length;
            // The chips sit in one themed row: the theme's tab size and gap, centred, shrunk as one piece if a narrow
            // window cannot fit all six.
            // Two rows of three, so every chip keeps the theme's full tab size and "Limit Break" is not squeezed (2026-10-08).
            // P3-05 (2026-10-09): eight modes (the two Randomizers joined), two rows of four on the left, and the Soundtrack
            // card on the right. The chips sit in their own area, so a narrow window shrinks them to it, never under the card.
            var chipArea = CreateRect("ChipArea", box, new Vector2(0f, 0.25f), new Vector2(0.75f, 0.97f));
            var chips = CreateRect("Chips", chipArea, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 1f));
            var chips2 = CreateRect("Chips2", chipArea, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.48f));
            int perRow = (count + 1) / 2;
            for (int i = 0; i < count; i++)
            {
                var mode = RunModeOptions.All[i];
                var cell = CreateRect($"Mode_{mode}", i < perRow ? chips : chips2, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                var background = cell.gameObject.AddComponent<Image>();
                var toggle = cell.gameObject.AddComponent<Toggle>();
                toggle.targetGraphic = background;

                var tickBox = CreateRect("Box", cell, new Vector2(0.04f, 0.18f), new Vector2(0.2f, 0.82f));
                var tickBoxImage = tickBox.gameObject.AddComponent<Image>();
                SoulHunter.Gameplay.UI.UITheme.ApplySurface(tickBoxImage, SoulHunter.Gameplay.UI.ThemedElement.SurfaceRole.TickBox);
                tickBoxImage.raycastTarget = false;
                var check = CreateRect("Check", tickBox, new Vector2(0.16f, 0.16f), new Vector2(0.84f, 0.84f));
                var checkImage = check.gameObject.AddComponent<Image>();
                SoulHunter.Gameplay.UI.UITheme.ApplySurface(checkImage, SoulHunter.Gameplay.UI.ThemedElement.SurfaceRole.Tick); // tick = on
                checkImage.raycastTarget = false;
                toggle.graphic = checkImage;

                var label = CreateText("Label", cell, new Vector2(0.24f, 0f), new Vector2(0.98f, 1f),
                    RunModeOptions.Name(mode), TextAlignmentOptions.Left, 8f, 26f); // the theme's tab text size caps it
                label.textWrappingMode = TextWrappingModes.NoWrap;
                _modeLabels.Add(label);
                // The theme's one tab size; the label takes light or dark text by contrast and sits beside the tick box.
                SoulHunter.Gameplay.UI.UITheme.ApplyButton(toggle, background, SoulHunter.Gameplay.UI.UITheme.ButtonKind.Normal,
                    SoulHunter.Gameplay.UI.UITheme.IconSlot.None, true, SoulHunter.Gameplay.UI.ThemedElement.SizeRole.Tab, tickBox);

                toggle.SetIsOnWithoutNotify(RunModeOptions.Get(data, mode));
                toggle.onValueChanged.AddListener(on => SetMode(mode, on));
                var hint = cell.gameObject.AddComponent<FocusHint>();
                hint.Hint = RunModeOptions.Description(mode);
                hint.Show = ShowModeHint;
                _modeToggles.Add(toggle);
            }
            chips.gameObject.AddComponent<SoulHunter.Gameplay.UI.ThemedRow>();
            chips2.gameObject.AddComponent<SoulHunter.Gameplay.UI.ThemedRow>();
            // One size for all six labels (auto-size gave "Limit Break" a much smaller size than "Hurry").
            box.gameObject.AddComponent<SameFontSize>().Labels = _modeLabels.ToArray();
            _modeHintText = CreateText("ModeHint", box, new Vector2(0.015f, 0.02f), new Vector2(0.985f, 0.24f),
                "Modes: select one to see what it does.", TextAlignmentOptions.Left, 10f, 18f);
            SoulHunter.Gameplay.UI.UITheme.ApplyText(_modeHintText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Soft);
            BuildMusicCard(box);
        }

        /// <summary>
        /// VS Music Selection (wiki Stages: a window on the stage selection screen, "alongside the windows for modes"; Soul
        /// Hunter has no stage selection screen, so it sits beside the modes here). A card that names the chosen track and
        /// opens the Soundtrack screen (MusicSelectionScreen).
        /// </summary>
        private void BuildMusicCard(RectTransform box)
        {
            var rect = CreateRect("MusicCard", box, new Vector2(0.77f, 0.27f), new Vector2(0.985f, 0.95f));
            var image = rect.gameObject.AddComponent<Image>();
            _musicButton = rect.gameObject.AddComponent<Button>();
            _musicButton.targetGraphic = image;
            _musicButton.onClick.AddListener(OpenMusic);
            var title = CreateText("Title", rect, new Vector2(0.06f, 0.52f), new Vector2(0.94f, 0.94f), "Music", TextAlignmentOptions.Center, 14f, 26f);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            _musicCardText = CreateText("Track", rect, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.5f), "", TextAlignmentOptions.Center, 10f, 18f);
            _musicCardText.overflowMode = TextOverflowModes.Ellipsis;
            // A card, not an action button: the screen's own size (Free), the theme's card look; the labels take light or
            // dark text by contrast with it.
            SoulHunter.Gameplay.UI.UITheme.ApplyButton(_musicButton, image, SoulHunter.Gameplay.UI.UITheme.ButtonKind.Card,
                SoulHunter.Gameplay.UI.UITheme.IconSlot.None, true, SoulHunter.Gameplay.UI.ThemedElement.SizeRole.Free);
            var hint = rect.gameObject.AddComponent<FocusHint>();
            hint.Hint = "Music: pick the track for your next run, play it faster or slower, and keep it for later runs.";
            hint.Show = ShowModeHint;
            RefreshMusicCard();
        }

        /// <summary>Opens the Soundtrack screen over this one; closing it focuses the Music card again.</summary>
        public void OpenMusic()
        {
            var root = PanelRoot();
            if (root == null) return;
            PlayUISound(false);
            if (_musicScreen == null) _musicScreen = MusicSelectionScreen.Create(root, _characterNameText != null ? _characterNameText.font : null);
            _musicScreen.Open(() =>
            {
                RefreshMusicCard();
                if (_musicButton != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_musicButton.gameObject);
            });
        }

        private void RefreshMusicCard()
        {
            if (_musicCardText == null) return;
            var data = SaveData();
            int choice = data != null ? data.MusicChoice : 0;
            int tempo = data != null ? data.MusicTempo : 0;
            string text = SoulHunter.Gameplay.Audio.MusicSelection.ChoiceName(choice);
            if (tempo != 0) text += ", " + SoulHunter.Gameplay.Audio.MusicSelection.TempoNames[Mathf.Clamp(tempo, 0, 2)];
            if (data != null && data.MusicLocked) text += " (kept)";
            _musicCardText.text = text;
        }

        private void BuildFilterPanel(RectTransform root)
        {
            var toggleButton = CreatePanel("FilterButton", root, new Vector2(0.76f, 0.81f), new Vector2(0.95f, 0.87f)); // clear of Back above, the stage list below
            var filterButton = toggleButton.gameObject.AddComponent<Button>();
            filterButton.targetGraphic = toggleButton.GetComponent<Image>();
            filterButton.onClick.AddListener(() => SetFilter(!_filterOn));
            _filterButtonLabel = CreateText("Label", toggleButton, Vector2.zero, Vector2.one, "FILTER: OFF", TextAlignmentOptions.Center, 14f, 20f);
            SoulHunter.Gameplay.UI.UITheme.ApplyButton(filterButton, toggleButton.GetComponent<Image>(), SoulHunter.Gameplay.UI.UITheme.ButtonKind.Normal,
                SoulHunter.Gameplay.UI.UITheme.IconSlot.None, true, SoulHunter.Gameplay.UI.ThemedElement.SizeRole.Tab);

            // The level list ends above the sort row and the favourite switch (2026-10-09; it reached down to 0.22).
            var list = CreatePanel("StageFilter", root, new Vector2(0.76f, 0.37f), new Vector2(0.95f, 0.8f));
            _filterCountText = CreateText("Count", list, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.99f), "", TextAlignmentOptions.Center, 14f, 24f);
            int stages = _stageShown.Length;
            float rowHeight = 0.88f / stages;
            for (int stage = 1; stage <= stages; stage++)
            {
                float top = 0.89f - (stage - 1) * rowHeight;
                var row = CreateRect($"Stage_{stage}", list, new Vector2(0.05f, top - rowHeight), new Vector2(0.97f, top));
                var box = CreateRect("Box", row, new Vector2(0f, 0.18f), new Vector2(0.1f, 0.82f));
                var boxImage = box.gameObject.AddComponent<Image>();
                SoulHunter.Gameplay.UI.UITheme.ApplySurface(boxImage, SoulHunter.Gameplay.UI.ThemedElement.SurfaceRole.TickBox);
                var check = CreateRect("Check", box, new Vector2(0.16f, 0.16f), new Vector2(0.84f, 0.84f));
                var checkImage = check.gameObject.AddComponent<Image>();
                SoulHunter.Gameplay.UI.UITheme.ApplySurface(checkImage, SoulHunter.Gameplay.UI.ThemedElement.SurfaceRole.Tick); // tick = shown
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

            BuildSortRow(root);
            BuildFavouriteButton(root);
        }

        /// <summary>
        /// VS sort under the level filter: one tab picks the sort (default, A-Z, levels completed), the other ascending or
        /// descending. Both are the theme's tab size in one themed row, labelled in words like FILTER.
        /// </summary>
        private void BuildSortRow(RectTransform root)
        {
            // The row sits in the filter column, so a narrow window shrinks it to the column, not past it.
            var area = CreateRect("SortArea", root, new Vector2(0.76f, 0.295f), new Vector2(0.95f, 0.355f));
            var row = CreateRect("SortRow", area, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f));
            _sortLabel = CreateTab("Sort", row, () => SetSort((CharacterSort)(((int)_sort + 1) % 3)));
            _orderLabel = CreateTab("Order", row, () => SetSortDescending(!_sortDescending));
            row.gameObject.AddComponent<SoulHunter.Gameplay.UI.ThemedRow>();
        }

        private TextMeshProUGUI CreateTab(string name, RectTransform row, UnityEngine.Events.UnityAction action)
        {
            var cell = CreateRect(name + "Button", row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var image = cell.gameObject.AddComponent<Image>();
            var button = cell.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            var label = CreateText("Label", cell, Vector2.zero, Vector2.one, "", TextAlignmentOptions.Center, 12f, 20f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            SoulHunter.Gameplay.UI.UITheme.ApplyButton(button, image, SoulHunter.Gameplay.UI.UITheme.ButtonKind.Normal,
                SoulHunter.Gameplay.UI.UITheme.IconSlot.None, true, SoulHunter.Gameplay.UI.ThemedElement.SizeRole.Tab);
            return label;
        }

        /// <summary>
        /// VS favourite (wiki Characters: from the character information panel; locked until Mindbender). A standard
        /// button that says its state in words; unusable, and saying LOCKED, until favourites are unlocked.
        /// </summary>
        private void BuildFavouriteButton(RectTransform root)
        {
            var rect = CreateRect("FavouriteButton", root, new Vector2(0.76f, 0.215f), new Vector2(0.95f, 0.275f));
            var image = rect.gameObject.AddComponent<Image>();
            _favouriteButton = rect.gameObject.AddComponent<Button>();
            _favouriteButton.targetGraphic = image;
            _favouriteButton.onClick.AddListener(ToggleFavourite);
            _favouriteLabel = CreateText("Label", rect, Vector2.zero, Vector2.one, "", TextAlignmentOptions.Center, 12f, 24f);
            _favouriteLabel.textWrappingMode = TextWrappingModes.NoWrap;
            SoulHunter.Gameplay.UI.UITheme.ApplyButton(_favouriteButton, image, SoulHunter.Gameplay.UI.UITheme.ButtonKind.Normal,
                SoulHunter.Gameplay.UI.UITheme.IconSlot.None, true, SoulHunter.Gameplay.UI.ThemedElement.SizeRole.Standard);
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

            var nameText = SoulHunter.Gameplay.UI.UITheme.ApplyText(CreateText("Name", card, new Vector2(0.07f, 0.74f), new Vector2(0.93f, 0.95f), character.CharacterName,
                TextAlignmentOptions.TopLeft, 12f, 24f), SoulHunter.Gameplay.UI.ThemedElement.TextRole.Card);
            nameText.fontStyle = FontStyles.Bold; // regular strokes are so thin at card size that dark text reads grey
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
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
            ApplySelectedEdge(outline);
            outline.enabled = false;
            _cardOutlines.Add(outline);
            _cardBadges.Add(badge);
        }

        /// <summary>The theme's frame around the chosen card: its colour and thickness (Theme Studio, Cards).</summary>
        private static void ApplySelectedEdge(Outline outline)
        {
            var cards = SoulHunter.Gameplay.UI.UiThemeAsset.Active.Cards;
            outline.effectColor = cards.SelectedEdge;
            outline.effectDistance = new Vector2(cards.SelectedEdgeWidth, -cards.SelectedEdgeWidth);
            outline.useGraphicAlpha = true;
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
        [Tooltip("Cells are at least this share of their width tall (0 = whatever height fits the viewport).")]
        public float MinHeightRatio = 0.45f;
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
            Grid.cellSize = new Vector2(Mathf.Floor(width), Mathf.Floor(Mathf.Max(height, width * MinHeightRatio)));
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
            bool restyled = false; // a theme restyle turns auto-size back on: share the size again
            foreach (var label in Labels) if (label != null && label.enableAutoSizing) restyled = true;
            if ((size == _lastSize && !restyled) || size.x <= 0f) return;
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
