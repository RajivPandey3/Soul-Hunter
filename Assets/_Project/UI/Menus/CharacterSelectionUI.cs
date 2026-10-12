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
        /// <summary>The Music card's two lines, "Music" and the chosen track (tests: they read on the card).</summary>
        public IReadOnlyList<TextMeshProUGUI> MusicCardLabels =>
            _musicButton != null ? _musicButton.GetComponentsInChildren<TextMeshProUGUI>(true) : new TextMeshProUGUI[0];
        /// <summary>The chosen character's box, the mode box and the grid's box (tests: the arrangement).</summary>
        public RectTransform InfoBox => _infoBox;
        public RectTransform ModesBox => _modesBox;
        public RectTransform GridBox => _gridBox;
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
            _layoutDirty = true; // the stats' height can change with the character (laid out again at the end of the frame)
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
            // Only when the grid scrolls inside its own box; in the scrolling body the body follows the focus (KeepSelectedInView).
            if (_gridScroll == null || !_gridScroll.enabled || _cards.Count == 0 || _cardGrid == null) return;
            int columns = Mathf.Max(1, _cardGrid.constraintCount);
            int rows = Mathf.CeilToInt(_cards.Count / (float)columns);
            if (rows <= 1) return;
            // The selected card's place among the shown cards, in grid order (sort and filter move it).
            int position = 0;
            foreach (int i in _displayOrder)
            {
                if (i == _currentIndex) break;
                if (IsCardShown(i)) position++;
            }
            int row = position / columns;
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
        //
        // Responsive (owner, 2026-10-10: "play in both landscape and portrait ... reuse the exact same screens ... UI elements
        // auto adjust themselves ... 0 duplicacy or parallel implementation"). The screen is built once, as a header (title,
        // gold), a body that scrolls (the sections below) and an action row pinned to the bottom of the safe area (Back,
        // Confirm; ui-ux-pro-max: the action that ends a screen stays in reach). Relayout only places the same sections:
        // - ThreeColumns (a roomy PC screen): stats | modes, grid, info | filter, levels, sort, favourite (as before);
        // - Stacked (a short or narrow screen: a phone held either way): one scroll that starts with the chosen
        //   character (compact) and the whole card grid, so characters are the first thing seen (owner's phone, 2026-10-10:
        //   the modes and the Music card had pushed every card off the screen); then sort, favourite, modes and Music,
        //   stats, filter and levels. Its only switch is "wide": stats sit beside the filter when there is room.
        // Controls keep the theme's sizes (48 dp on touch); rows of tabs wrap (ThemedRow); the grid's columns follow its
        // width (GridFit.MinCellWidth). Nothing is rebuilt on a rotation, so the selection, sort and filter stay.

        /// <summary>How the sections are placed for the current screen (see the notes above).</summary>
        public enum ScreenLayout { ThreeColumns, Stacked }
        public ScreenLayout CurrentLayout { get; private set; }
        /// <summary>The safe-area content root, the scrolling body and its content (tests).</summary>
        public RectTransform ContentRoot => _content;
        public RectTransform BodyViewport => _body;
        public RectTransform ScrollContent => _sections;
        public Button BackButton => _backButton;
        public Button ConfirmButton => _selectAndPlayButton;
        public ScrollRect BodyScroll => _bodyScroll;

        private const float CardMinWidth = 140f; // a card narrower than this cannot show its name, portrait and weapon
        private const int CardMaxColumns = 5;    // 13 characters in 3 rows on a PC: all visible without scrolling
        private const float SectionPad = 8f;

        private RectTransform _content, _header, _body, _sections, _actions, _titleRect;
        private RectTransform _statsBox, _modesBox, _chipArea, _chipRowRect, _musicRect, _gridBox, _gridContent, _infoBox;
        private RectTransform _filterArea, _filterRowRect, _sortArea, _sortRowRect, _levelsBox, _levelsGrid, _favouriteArea;
        private SoulHunter.Gameplay.UI.ThemedRow _actionsRow, _chipRow, _filterRow, _sortRow;
        private GridLayoutGroup _cardGrid, _levelGrid;
        private ScrollRect _bodyScroll;
        private Button _backButton;
        private Vector2 _laidOutSize = new Vector2(-1f, -1f);
        private bool _layoutDirty, _wasShown;
        private int _revealFrames; // frames until the chosen card is scrolled into view (after the grid's cells are sized)

        private void OnEnable() => SoulHunter.Gameplay.UI.UiScreen.Changed += HandleScreenChanged;

        private void OnDisable() => SoulHunter.Gameplay.UI.UiScreen.Changed -= HandleScreenChanged;

        private void HandleScreenChanged()
        {
            if (_content == null) return;
            Relayout();
            _revealFrames = 2; // a rotation keeps the chosen character on screen
        }

        private void LateUpdate()
        {
            if (_content == null) return;
            if (!_content.gameObject.activeInHierarchy) { _wasShown = false; return; }
            // Opening the screen (its buttons restyle themselves on enable), a window change, or new content (stats).
            if (!_wasShown) _revealFrames = 2;
            if (!_wasShown || _layoutDirty || _content.rect.size != _laidOutSize) Relayout();
            _wasShown = true;
            if (_revealFrames > 0 && --_revealFrames == 0) RevealSelected();
            FitCardNames();
        }

        private float _namedCardHeight = -1f, _namedTextFloor = -1f;

        /// <summary>
        /// A card's name keeps a line of its smallest text. On a phone held upright the cards are about 48 units tall, and
        /// the name's fifth of that was lower than 12 sp text: the name was cut away whole (owner's phone, 2026-10-12).
        /// The name's band grows and the portrait and badge start under it. PC cards are tall enough, so they do not change.
        /// </summary>
        private void FitCardNames()
        {
            if (_cards.Count == 0 || _cards[0] == null) return;
            float height = ((RectTransform)_cards[0].transform).rect.height, floor = SoulHunter.Gameplay.UI.UiScreen.MinTextUnits;
            if (height <= 0f || (Mathf.Approximately(height, _namedCardHeight) && Mathf.Approximately(floor, _namedTextFloor))) return;
            _namedCardHeight = height;
            _namedTextFloor = floor;
            float bottom = 0.95f - Mathf.Clamp(floor * 1.35f / height, 0.21f, 0.42f);
            foreach (var card in _cards)
            {
                if (card == null) continue;
                if (card.transform.Find("Name") is RectTransform name) name.anchorMin = new Vector2(name.anchorMin.x, bottom);
                if (card.transform.Find("Portrait") is RectTransform portrait) portrait.anchorMax = new Vector2(portrait.anchorMax.x, bottom);
                if (card.transform.Find("Badge") is RectTransform badge)
                {
                    badge.anchorMax = new Vector2(badge.anchorMax.x, bottom);
                    badge.anchorMin = new Vector2(badge.anchorMin.x, Mathf.Min(0.5f, Mathf.Max(0.42f, bottom - 0.24f))); // clear of the weapon icon
                }
            }
        }

        private void BuildLayout()
        {
            var root = PanelRoot();
            if (root == null) return;

            MainMenuNavigation.ScaleWithScreen(root.GetComponentInParent<CanvasScaler>()); // also set by the menu at load

            // The content sits in the safe area (notch, gesture bar); the panel itself stays full screen.
            _content = CreateRect("CharacterContent", root, Vector2.zero, Vector2.one);
            SoulHunter.Gameplay.UI.SafeAreaFitter.Fit(_content);
            _header = CreateRect("Header", _content, Vector2.zero, Vector2.one);
            _body = CreateRect("Body", _content, Vector2.zero, Vector2.one);
            // The body is its own viewport; a drag between two sections still scrolls it.
            // Keyboard and controller focus scrolls into view (ui-ux-pro-max: focus not obscured).
            _bodyScroll = SoulHunter.Gameplay.UI.ResponsiveScroll.Create(_body, null, "Sections", out _sections, sensitivity: 30f,
                keepSelectedInView: true);
            var follow = _body.GetComponent<SoulHunter.Gameplay.UI.KeepSelectedInView>();
            if (follow != null) follow.Margin = SoulHunter.Gameplay.UI.UITheme.Layout.ButtonGap;
            _actions = CreateRect("Actions", _content, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));

            // Title along the top.
            var title = root.Find("Text (TMP)") as RectTransform;
            if (title != null && title.TryGetComponent(out TextMeshProUGUI titleText))
            {
                Place(title, _header, Vector2.zero, Vector2.one);
                _titleRect = title;
                titleText.text = "Character Selection";
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.fontStyle = FontStyles.Bold;
                SoulHunter.Gameplay.UI.UITheme.ApplyText(titleText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Title);
                AutoSize(titleText, 24f, 56f);
            }

            // Prev/Next are replaced by the grid.
            if (_nextButton) _nextButton.gameObject.SetActive(false);
            if (_prevButton) _prevButton.gameObject.SetActive(false);

            // The stat block (labels left, values right).
            _statsBox = CreatePanel("StatsPanel", _sections, Vector2.zero, Vector2.zero);
            if (_statsText != null)
            {
                Place(_statsText.rectTransform, _statsBox, new Vector2(0.07f, 0.03f), new Vector2(0.93f, 0.97f));
                _statsText.alignment = TextAlignmentOptions.TopLeft;
                _statsText.richText = true;
                AutoSize(_statsText, 12f, 28f);
            }

            // The grid of character cards (scrolls inside its box on a roomy screen; the body scrolls otherwise).
            _gridBox = CreatePanel("CharacterGrid", _sections, Vector2.zero, Vector2.zero);
            _gridScroll = SoulHunter.Gameplay.UI.ResponsiveScroll.Create(_gridBox, "Viewport", "Content", out _gridContent,
                sensitivity: 30f, dragSurface: false, fitContentHeight: true);
            var viewport = _gridScroll.viewport;
            viewport.offsetMin = new Vector2(10f, 10f);
            viewport.offsetMax = new Vector2(-10f, -10f);
            _cardGrid = _gridContent.gameObject.AddComponent<GridLayoutGroup>();
            _cardGrid.cellSize = _cardSize;
            _cardGrid.spacing = new Vector2(10f, 10f);
            _cardGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _cardGrid.constraintCount = _gridColumns;
            _cardGrid.childAlignment = TextAnchor.UpperCenter;
            // The cards are sized from the box; the columns follow its width (fewer on a phone held upright).
            var fit = _gridContent.gameObject.AddComponent<SoulHunter.Gameplay.UI.GridFit>();
            fit.Grid = _cardGrid;
            fit.Viewport = viewport;
            fit.Count = _availableCharacters.Count;
            fit.MinCellWidth = CardMinWidth;
            fit.MaxColumns = Mathf.Min(CardMaxColumns, Mathf.Max(1, _gridColumns));
            for (int i = 0; i < _availableCharacters.Count; i++) BuildCard(i, _gridContent);

            // The selected character.
            _infoBox = CreatePanel("SelectedInfo", _sections, Vector2.zero, Vector2.zero);
            if (_characterIconDisplay != null) Place(_characterIconDisplay.rectTransform, _infoBox, new Vector2(0.02f, 0.08f), new Vector2(0.2f, 0.92f));
            if (_characterNameText != null)
            {
                Place(_characterNameText.rectTransform, _infoBox, new Vector2(0.22f, 0.64f), new Vector2(0.78f, 0.95f));
                _characterNameText.alignment = TextAlignmentOptions.TopLeft;
                AutoSize(_characterNameText, 16f, 40f);
                SoulHunter.Gameplay.UI.UITheme.ApplyText(_characterNameText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Accent); // the scene's text takes the theme's font
            }
            if (_characterDescriptionText != null)
            {
                Place(_characterDescriptionText.rectTransform, _infoBox, new Vector2(0.22f, 0.06f), new Vector2(0.78f, 0.62f));
                _characterDescriptionText.alignment = TextAlignmentOptions.TopLeft;
                AutoSize(_characterDescriptionText, 12f, 28f);
                SoulHunter.Gameplay.UI.UITheme.ApplyText(_characterDescriptionText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Body);
            }
            _weaponIconDisplay = CreateImage("StartingWeapon_Icon", _infoBox, new Vector2(0.81f, 0.3f), new Vector2(0.97f, 0.92f), null);
            _weaponNameText = CreateText("StartingWeapon_Text", _infoBox, new Vector2(0.79f, 0.04f), new Vector2(0.99f, 0.28f), "", TextAlignmentOptions.Center, 12f, 18f);

            // The action row at the bottom: Back, then Confirm (it wraps to two lines on a narrow screen).
            var back = root.Find("Back_Button") as RectTransform;
            if (back != null)
            {
                _backButton = back.GetComponent<Button>();
                back.SetParent(_actions, false);
                back.anchorMin = back.anchorMax = new Vector2(0.5f, 0.5f);
                back.localScale = Vector3.one;
            }
            if (_selectAndPlayButton != null)
            {
                var confirm = (RectTransform)_selectAndPlayButton.transform;
                confirm.SetParent(_actions, false);
                confirm.anchorMin = confirm.anchorMax = new Vector2(0.5f, 0.5f);
                confirm.localScale = Vector3.one;
                confirm.SetAsLastSibling();
                _confirmLabel = _selectAndPlayButton.GetComponentInChildren<TextMeshProUGUI>();
                if (_confirmLabel != null) _confirmLabel.text = "Confirm";
                SoulHunter.Gameplay.UI.UITheme.ApplyButton(_selectAndPlayButton, _selectAndPlayButton.targetGraphic as Image,
                    SoulHunter.Gameplay.UI.UITheme.ButtonKind.Primary, SoulHunter.Gameplay.UI.UITheme.IconSlot.Confirm);
            }
            _actionsRow = _actions.gameObject.AddComponent<SoulHunter.Gameplay.UI.ThemedRow>();

            // Gold, which buys unlocked characters.
            _goldText = CreateText("Gold_Text", _header, Vector2.zero, Vector2.one, "", TextAlignmentOptions.MidlineLeft, 18f, 32f);
            SoulHunter.Gameplay.UI.UITheme.ApplyText(_goldText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Accent);

            // VS-style character filter by the stage each character comes from, the sort and the favourite.
            BuildFilterPanel(_sections);

            // VS run modes (Hurry, Hyper, Inverse, Endless, Arcana, Limit Break, Omens, Blind Fate) and the Music card.
            BuildModesRow(_sections);

            Relayout();
        }

        /// <summary>
        /// Places every section for the current safe area (see the notes above <see cref="ScreenLayout"/>). Called when the
        /// screen opens, on a rotation or window change (<see cref="SoulHunter.Gameplay.UI.UiScreen.Changed"/>) and when the
        /// stats change; it never rebuilds anything.
        /// </summary>
        public void Relayout()
        {
            if (_content == null) return;
            Vector2 size = _content.rect.size;
            if (size.x <= 1f || size.y <= 1f) return;
            _laidOutSize = size;
            _layoutDirty = false;
            var layout = SoulHunter.Gameplay.UI.UITheme.Layout;
            float gap = layout.ButtonGap;
            float W = size.x, H = size.y;

            // A roomy PC screen keeps the approved PC look: Back in its top-right corner and Confirm in the bottom-right
            // corner, at the same spots as before (owner-approved character select look, 2026-10-10). Elsewhere (phones)
            // both move into the action row pinned to the bottom of the safe area.
            bool roomy = W >= 1300f && H - 184f >= 600f;
            // A touch screen of that shape (a tablet): the right column's ten level rows are 48 dp each and no longer fit
            // above Confirm with the filter, sort and favourite (they ran over Sort and Confirm, audit 2026-10-12), so it
            // takes the one-scroll layout, like a phone.
            float touchRow = SoulHunter.Gameplay.UI.UiScreen.MinTouchUnits;
            if (roomy && touchRow > 0f)
            {
                int levelRows = _stageToggles.Count;
                float column = levelRows * touchRow + (levelRows - 1) * 6f + 3f * SectionPad + 32f // the levels box
                    + 4f * Mathf.Max(touchRow, layout.TabHeight) + 5f * gap;                        // filter, sort (two lines), favourite
                roomy = H * (1f - PcConfirmCentre.y) - layout.ButtonHeight * 0.5f - gap - 184f >= column;
            }
            Vector2 standard = new Vector2(layout.ButtonWidth, layout.ButtonHeight);
            if (_backButton != null && SoulHunter.Gameplay.UI.ThemedElement.For(_backButton.gameObject).StandardSize(SoulHunter.Gameplay.UI.UiThemeAsset.Active) is Vector2 b) standard = b;
            PlaceClosingButtons(roomy);

            // Header: title centred with the gold beside it, or the gold under the title on a narrow screen.
            bool oneLine = W >= 900f;
            float titleH = H >= 900f ? 76f : 56f, goldH = 40f;
            float headerH = oneLine ? titleH : titleH + goldH;
            if (roomy) headerH = Mathf.Max(headerH, H * (1f - PcBackCentre.y) + standard.y * 0.5f); // Back sits in the header
            TL(_header, 0f, 0f, W, headerH);
            if (_titleRect != null) TL(_titleRect, oneLine ? W * 0.25f : gap, 0f, oneLine ? W * 0.5f : W - 2f * gap, titleH);
            if (_goldText != null)
            {
                TL(_goldText.rectTransform, gap, oneLine ? 0f : titleH, oneLine ? W * 0.25f - gap : W - 2f * gap, oneLine ? titleH : goldH);
                _goldText.alignment = oneLine ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Midline;
            }

            // Action row pinned to the bottom edge of the safe area.
            _actions.anchorMin = _actions.anchorMax = new Vector2(0.5f, 0f);
            _actions.pivot = new Vector2(0.5f, 0f);
            _actions.anchoredPosition = new Vector2(0f, gap);
            float footerH = gap;
            if (!roomy)
            {
                _actionsRow.Layout();
                footerH = _actions.sizeDelta.y * _actions.localScale.y + 2f * gap;
            }

            float bodyTop = headerH + gap * 0.5f;
            float bodyH = Mathf.Max(1f, H - bodyTop - footerH);
            TL(_body, 0f, bodyTop, W, bodyH);
            float inner = W - 2f * gap;
            CurrentLayout = roomy ? ScreenLayout.ThreeColumns : ScreenLayout.Stacked;

            float contentH;
            switch (CurrentLayout)
            {
                case ScreenLayout.ThreeColumns:
                {
                    // The right column reaches left to Confirm's approved spot and ends above it.
                    float side = Mathf.Clamp(inner * 0.2f, 300f, 440f);
                    float rightSide = Mathf.Max(side, (W - gap) - (W * PcConfirmCentre.x - standard.x * 0.5f));
                    float centre = inner - side - rightSide - 2f * gap;
                    float x0 = gap, x1 = gap + side + gap, x2 = x1 + centre + gap;
                    float rightH = H * (1f - PcConfirmCentre.y) - standard.y * 0.5f - gap - bodyTop;
                    PlaceStats(x0, 0f, side, bodyH, true);
                    float y = PlaceRow(_filterArea, _filterRowRect, _filterRow, x2, 0f, rightSide) + gap;
                    float sortH = PlaceRow(_sortArea, _sortRowRect, _sortRow, x2, 0f, rightSide);
                    float favouriteH = FavouriteHeight();
                    float levelsH = PlaceLevels(x2, y, rightSide, rightH - y - sortH - favouriteH - 2f * gap, true);
                    y += levelsH + gap;
                    PlaceRow(_sortArea, _sortRowRect, _sortRow, x2, y, rightSide);
                    PlaceFavourite(x2, y + sortH + gap, rightSide);
                    float modesH = PlaceModes(x1, 0f, centre);
                    float infoH = Mathf.Clamp(bodyH * 0.24f, 200f, 280f);
                    PlaceGrid(x1, modesH + gap, centre, bodyH - modesH - infoH - 2f * gap, true);
                    PlaceInfo(x1, bodyH - infoH, centre, infoH);
                    contentH = bodyH;
                    break;
                }
                default:
                {
                    // One scroll. The chosen character (compact) and the whole grid come first, so the first view shows
                    // cards: at least two rows on a phone held upright, one when held sideways. A narrow screen's sections
                    // span its whole width (three cards and three chips a line on a 510-unit phone).
                    bool wide = W >= 900f;
                    float x = wide ? gap : 0f, w = W - 2f * x;
                    float y = PlaceInfo(x, 0f, w, wide ? 132f : 168f) + gap;
                    y += PlaceGrid(x, y, w, 0f, false) + gap;
                    y += PlaceRow(_sortArea, _sortRowRect, _sortRow, x, y, w) + gap;
                    y += PlaceFavourite(x, y, w) + gap;
                    y += PlaceModes(x, y, w) + gap;
                    // Stats, then the filter and its levels: side by side when wide, one under the other when narrow.
                    float statsW = wide ? Mathf.Floor((w - gap) * 0.5f) : w;
                    float statsH = PlaceStats(x, y, statsW, 0f, false);
                    float fx = wide ? x + statsW + gap : x;
                    float fy = wide ? y : y + statsH + gap;
                    float fw = wide ? w - statsW - gap : w;
                    float filterEnd = fy + PlaceRow(_filterArea, _filterRowRect, _filterRow, fx, fy, fw) + gap;
                    filterEnd += PlaceLevels(fx, filterEnd, fw, 0f, false);
                    contentH = Mathf.Max(y + statsH, filterEnd) + gap;
                    break;
                }
            }

            contentH = Mathf.Max(contentH, bodyH);
            _sections.anchorMin = new Vector2(0f, 1f);
            _sections.anchorMax = new Vector2(1f, 1f);
            _sections.pivot = new Vector2(0.5f, 1f);
            _sections.sizeDelta = new Vector2(0f, contentH);
            float maxScroll = contentH - bodyH;
            _bodyScroll.vertical = maxScroll > 1f;
            _sections.anchoredPosition = new Vector2(0f, Mathf.Clamp(_sections.anchoredPosition.y, 0f, Mathf.Max(0f, maxScroll)));
            SoulHunter.Gameplay.UI.UiTextFloor.RaiseAll(_content); // no text under the readable floor (the brief's rule 5)
        }

        // The approved PC spots (before 2026-10-10): Back centred at 86.5% across, 93.5% up; Confirm at 85.5% across, 13.5% up.
        private static readonly Vector2 PcBackCentre = new Vector2(0.865f, 0.935f);
        private static readonly Vector2 PcConfirmCentre = new Vector2(0.855f, 0.135f);

        /// <summary>
        /// On a roomy PC screen Back and Confirm keep their approved corners (the theme's size, centred on their old spots);
        /// otherwise both sit in the action row pinned to the bottom (Back first, Confirm last).
        /// </summary>
        private void PlaceClosingButtons(bool corners)
        {
            var back = _backButton != null ? (RectTransform)_backButton.transform : null;
            var confirm = _selectAndPlayButton != null ? (RectTransform)_selectAndPlayButton.transform : null;
            foreach (var (rect, spot) in new[] { (back, PcBackCentre), (confirm, PcConfirmCentre) })
            {
                if (rect == null) continue;
                rect.SetParent(corners ? _content : _actions, false);
                rect.anchorMin = rect.anchorMax = corners ? spot : new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                if (corners) rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.SetAsLastSibling(); // above the body (drawn and hit first); in the row, Back before Confirm
            }
            if (_backButton != null) SoulHunter.Gameplay.UI.ThemedElement.For(_backButton.gameObject).FitSize(SoulHunter.Gameplay.UI.UiThemeAsset.Active);
            if (_selectAndPlayButton != null) SoulHunter.Gameplay.UI.ThemedElement.For(_selectAndPlayButton.gameObject).FitSize(SoulHunter.Gameplay.UI.UiThemeAsset.Active);
        }

        /// <summary>Places a themed row's area at the given spot and returns the row's wrapped height.</summary>
        private static float PlaceRow(RectTransform area, RectTransform rowRect, SoulHunter.Gameplay.UI.ThemedRow row, float x, float y, float width)
        {
            TL(area, x, y, width, 10f);
            rowRect.anchorMin = rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.anchoredPosition = Vector2.zero;
            row.Layout();
            float h = rowRect.sizeDelta.y * rowRect.localScale.y;
            TL(area, x, y, width, h);
            return h;
        }

        private float PlaceModes(float x, float y, float width)
        {
            var layout = SoulHunter.Gameplay.UI.UITheme.Layout;
            float gap = layout.ButtonGap, avail = width - 2f * SectionPad;
            Vector2 chip = new Vector2(layout.TabWidth, layout.TabHeight);
            if (_modeToggles.Count > 0 && SoulHunter.Gameplay.UI.ThemedElement.For(_modeToggles[0].gameObject).StandardSize(SoulHunter.Gameplay.UI.UiThemeAsset.Active) is Vector2 s) chip = s;
            // At most four chips a line (two even lines of four on a PC, as approved); fewer when narrow. The chip area may
            // use the box's whole width: the row keeps its own gap of air at both ends (three chips a line on a 510-unit phone).
            int perLine = Mathf.Clamp(Mathf.FloorToInt(width / (chip.x + gap)), 1, 4);
            float chipAreaW = Mathf.Min(width, perLine * chip.x + (perLine - 1) * gap + gap);
            float musicW = Mathf.Min(360f, avail - chipAreaW - gap);
            bool musicBeside = musicW >= 200f;
            float chipsH = PlaceRow(_chipArea, _chipRowRect, _chipRow, 0f, 0f, chipAreaW);
            float groupW = musicBeside ? chipAreaW + gap + musicW : chipAreaW;
            float gx = (width - groupW) * 0.5f;
            TL(_chipArea, gx, SectionPad, chipAreaW, chipsH);
            float next = SectionPad + chipsH + gap;
            if (musicBeside) TL(_musicRect, gx + chipAreaW + gap, SectionPad, musicW, chipsH);
            else
            {
                float musicH = Mathf.Max(layout.ButtonHeight, SoulHunter.Gameplay.UI.UiScreen.MinTouchUnits);
                TL(_musicRect, gx, next, chipAreaW, musicH);
                next += musicH + gap;
            }
            // The hint says what the focused mode does: three lines on a narrow screen at the phone's text floor.
            float hintH = avail < 800f ? Mathf.Max(36f, SoulHunter.Gameplay.UI.UiScreen.MinTextUnits * 1.3f * 3f) : 36f;
            TL(_modeHintText.rectTransform, SectionPad, next, avail, hintH);
            float h = next + hintH + SectionPad;
            TL(_modesBox, x, y, width, h);
            return h;
        }

        private float PlaceGrid(float x, float y, float width, float height, bool scrollsInside)
        {
            _gridScroll.enabled = scrollsInside; // a grid inside the scrolling body shows every card and lets the body scroll
            if (!scrollsInside)
            {
                _gridContent.anchoredPosition = Vector2.zero;
                // The same cells GridFit makes for a viewport of this height (so the box fits every card exactly).
                float innerW = width - 20f, spacing = _cardGrid.spacing.x;
                float touch = SoulHunter.Gameplay.UI.UiScreen.MinTouchUnits;
                int columns = SoulHunter.Gameplay.UI.UiScreen.ColumnsFor(innerW, Mathf.Max(CardMinWidth, touch), spacing,
                    Mathf.Min(CardMaxColumns, Mathf.Max(1, _gridColumns)));
                float cellW = (innerW - spacing * (columns - 1)) / columns;
                float cellH = Mathf.Floor(Mathf.Max(cellW * 0.72f, cellW * 0.45f, touch));
                int rows = Mathf.CeilToInt(_availableCharacters.Count / (float)columns);
                height = rows * cellH + (rows - 1) * _cardGrid.spacing.y + 20f + 1f;
            }
            TL(_gridBox, x, y, width, height);
            return height;
        }

        private float PlaceInfo(float x, float y, float width, float height)
        {
            TL(_infoBox, x, y, width, height);
            return height;
        }

        private float PlaceStats(float x, float y, float width, float height, bool fill)
        {
            if (_statsText != null)
            {
                if (fill) AutoSize(_statsText, 12f, 28f);
                else
                {
                    // In the scrolling body the stats take their natural height at one readable size.
                    _statsText.enableAutoSizing = false;
                    _statsText.fontSize = Mathf.Max(20f, SoulHunter.Gameplay.UI.UiScreen.MinTextUnits);
                    float textW = width * 0.86f;
                    float preferred = _statsText.GetPreferredValues(_statsText.text, textW, 0f).y;
                    height = preferred / 0.94f + 8f;
                }
            }
            TL(_statsBox, x, y, width, height);
            return height;
        }

        private float FavouriteHeight() =>
            Mathf.Max(SoulHunter.Gameplay.UI.UITheme.Layout.ButtonHeight, SoulHunter.Gameplay.UI.UiScreen.MinTouchUnits);

        private float PlaceFavourite(float x, float y, float width)
        {
            float h = FavouriteHeight();
            TL(_favouriteArea, x, y, width, h);
            return h;
        }

        private float PlaceLevels(float x, float y, float width, float height, bool fill)
        {
            const float countH = 32f, spacing = 6f;
            float touch = SoulHunter.Gameplay.UI.UiScreen.MinTouchUnits;
            float avail = width - 2f * SectionPad;
            int columns = fill ? 1 : SoulHunter.Gameplay.UI.UiScreen.ColumnsFor(avail, 210f, spacing, 2);
            int rows = Mathf.CeilToInt(_stageToggles.Count / (float)columns);
            float cellH = fill
                ? Mathf.Clamp((height - 3f * SectionPad - countH - spacing * (rows - 1)) / rows, Mathf.Max(touch, 24f), 60f)
                : Mathf.Max(touch, 44f);
            float cellW = (avail - spacing * (columns - 1)) / columns;
            float gridH = rows * cellH + (rows - 1) * spacing;
            if (!fill) height = SectionPad + countH + SectionPad + gridH + SectionPad;
            TL(_levelsBox, x, y, width, height);
            if (_filterCountText != null) TL(_filterCountText.rectTransform, SectionPad, SectionPad, avail, countH);
            TL(_levelsGrid, SectionPad, SectionPad + countH + SectionPad, avail, gridH);
            _levelGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _levelGrid.constraintCount = columns;
            _levelGrid.spacing = new Vector2(spacing, spacing);
            _levelGrid.cellSize = new Vector2(Mathf.Floor(cellW), Mathf.Floor(cellH));
            // Each whole row is the touch target; its tick box is square, beside the level's name.
            float box = Mathf.Min(cellH * 0.62f, 34f);
            foreach (var toggle in _stageToggles)
            {
                var tick = toggle.transform.Find("Box") as RectTransform;
                if (tick != null)
                {
                    tick.anchorMin = tick.anchorMax = new Vector2(0f, 0.5f);
                    tick.pivot = new Vector2(0.5f, 0.5f);
                    tick.sizeDelta = new Vector2(box, box);
                    tick.anchoredPosition = new Vector2(box * 0.5f + 4f, 0f);
                }
                var label = toggle.transform.Find("Label") as RectTransform;
                if (label != null)
                {
                    label.anchorMin = Vector2.zero;
                    label.anchorMax = Vector2.one;
                    label.offsetMin = new Vector2(box + 12f, 0f);
                    label.offsetMax = Vector2.zero;
                }
            }
            return height;
        }

        /// <summary>Scrolls the body so the chosen character's card is on screen (the screen opened, or the phone turned).</summary>
        private void RevealSelected()
        {
            if (_bodyScroll == null || !_bodyScroll.vertical || _cards.Count == 0) return;
            var card = _cards[Mathf.Clamp(_currentIndex, 0, _cards.Count - 1)];
            if (card == null || !card.gameObject.activeInHierarchy) return;
            Canvas.ForceUpdateCanvases(); // the grid's cells as drawn
            SoulHunter.Gameplay.UI.ResponsiveScroll.KeepInView(_bodyScroll, (RectTransform)card.transform, SoulHunter.Gameplay.UI.UITheme.Layout.ButtonGap);
        }

        /// <summary>Places <paramref name="rect"/> by its top-left corner inside its parent (units from the parent's top-left).</summary>
        private static void TL(RectTransform rect, float x, float y, float width, float height)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height));
        }

        /// <summary>
        /// VS "List of Modes" toggles in one themed row (four a line at most, wrapping on a narrow screen), with a line under
        /// them that says what the focused (or hovered) mode does, and the Music card beside or under them.
        /// </summary>
        private void BuildModesRow(RectTransform parent)
        {
            _modesBox = CreatePanel("RunModes", parent, Vector2.zero, Vector2.zero);
            var data = SaveData();
            int count = RunModeOptions.All.Length;
            // P3-05 (2026-10-09): eight modes (the two Randomizers joined). Responsive (2026-10-10): one themed row that keeps
            // every chip at the theme's tab size (48 dp on touch) and wraps onto centred lines.
            _chipArea = CreateRect("ChipArea", _modesBox, Vector2.zero, Vector2.zero);
            _chipRowRect = CreateRect("Chips", _chipArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            for (int i = 0; i < count; i++)
            {
                var mode = RunModeOptions.All[i];
                var cell = CreateRect($"Mode_{mode}", _chipRowRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
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
            _chipRow = _chipRowRect.gameObject.AddComponent<SoulHunter.Gameplay.UI.ThemedRow>();
            // One size for all the labels (auto-size gave "Limit Break" a much smaller size than "Hurry").
            _modesBox.gameObject.AddComponent<SameFontSize>().Labels = _modeLabels.ToArray();
            _modeHintText = CreateText("ModeHint", _modesBox, Vector2.zero, Vector2.zero,
                "Modes: select one to see what it does.", TextAlignmentOptions.Left, 10f, 18f);
            SoulHunter.Gameplay.UI.UITheme.ApplyText(_modeHintText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Soft);
            BuildMusicCard(_modesBox);
        }

        /// <summary>
        /// VS Music Selection (wiki Stages: a window on the stage selection screen, "alongside the windows for modes"; Soul
        /// Hunter has no stage selection screen, so it sits beside the modes here). A card that names the chosen track and
        /// opens the Soundtrack screen (MusicSelectionScreen).
        /// </summary>
        private void BuildMusicCard(RectTransform box)
        {
            _musicRect = CreateRect("MusicCard", box, Vector2.zero, Vector2.zero);
            var rect = _musicRect;
            var image = rect.gameObject.AddComponent<Image>();
            _musicButton = rect.gameObject.AddComponent<Button>();
            _musicButton.targetGraphic = image;
            _musicButton.onClick.AddListener(OpenMusic);
            var title = CreateText("Title", rect, new Vector2(0.06f, 0.52f), new Vector2(0.94f, 0.94f), "Music", TextAlignmentOptions.Center, 14f, 26f);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            _musicCardText = CreateText("Track", rect, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.5f), "", TextAlignmentOptions.Center, 10f, 18f);
            _musicCardText.overflowMode = TextOverflowModes.Ellipsis;
            // A card, not an action button: the screen's own size (Free), the theme's card look. Its labels take the
            // theme's card text role (dark on the pale card, as the Music screen's cards do): a card does not restyle a
            // label that already has a role, so the plain light text stayed and could not be read (owner's phone, 2026-10-10).
            SoulHunter.Gameplay.UI.UITheme.ApplyText(title, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Card);
            SoulHunter.Gameplay.UI.UITheme.ApplyText(_musicCardText, SoulHunter.Gameplay.UI.ThemedElement.TextRole.Card);
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

        private void BuildFilterPanel(RectTransform parent)
        {
            // FILTER: ON/OFF, a tab in its own themed row.
            _filterArea = CreateRect("FilterArea", parent, Vector2.zero, Vector2.zero);
            _filterRowRect = CreateRect("FilterRow", _filterArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var toggleButton = CreateRect("FilterButton", _filterRowRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var filterImage = toggleButton.gameObject.AddComponent<Image>();
            var filterButton = toggleButton.gameObject.AddComponent<Button>();
            filterButton.targetGraphic = filterImage;
            filterButton.onClick.AddListener(() => SetFilter(!_filterOn));
            _filterButtonLabel = CreateText("Label", toggleButton, Vector2.zero, Vector2.one, "FILTER: OFF", TextAlignmentOptions.Center, 14f, 20f);
            SoulHunter.Gameplay.UI.UITheme.ApplyButton(filterButton, filterImage, SoulHunter.Gameplay.UI.UITheme.ButtonKind.Normal,
                SoulHunter.Gameplay.UI.UITheme.IconSlot.None, true, SoulHunter.Gameplay.UI.ThemedElement.SizeRole.Tab);
            _filterRow = _filterRowRect.gameObject.AddComponent<SoulHunter.Gameplay.UI.ThemedRow>();

            // The level list: a count and one tick row per level, in a grid whose rows are the touch targets.
            _levelsBox = CreatePanel("StageFilter", parent, Vector2.zero, Vector2.zero);
            _filterCountText = CreateText("Count", _levelsBox, Vector2.zero, Vector2.zero, "", TextAlignmentOptions.Center, 14f, 24f);
            _levelsGrid = CreateRect("Levels", _levelsBox, Vector2.zero, Vector2.zero);
            _levelGrid = _levelsGrid.gameObject.AddComponent<GridLayoutGroup>();
            _levelGrid.childAlignment = TextAnchor.UpperLeft;
            int stages = _stageShown.Length;
            for (int stage = 1; stage <= stages; stage++)
            {
                var row = CreateRect($"Stage_{stage}", _levelsGrid, Vector2.zero, Vector2.one);
                var hit = row.gameObject.AddComponent<Image>();
                hit.color = Color.clear; // the whole row takes the tap, not only its tick box
                var box = CreateRect("Box", row, new Vector2(0f, 0.18f), new Vector2(0.1f, 0.82f));
                var boxImage = box.gameObject.AddComponent<Image>();
                SoulHunter.Gameplay.UI.UITheme.ApplySurface(boxImage, SoulHunter.Gameplay.UI.ThemedElement.SurfaceRole.TickBox);
                boxImage.raycastTarget = false;
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
                    CampaignLevelCatalog.Get(stage).Name, TextAlignmentOptions.MidlineLeft, 17f, 17f);
                label.enableAutoSizing = false;
                label.fontSize = 17f;
                label.overflowMode = TextOverflowModes.Ellipsis;
                _stageToggles.Add(toggle);
            }

            BuildSortRow(parent);
            BuildFavouriteButton(parent);
        }

        /// <summary>
        /// VS sort: one tab picks the sort (default, A-Z, levels completed), the other ascending or descending. Both are the
        /// theme's tab size in one themed row, labelled in words like FILTER.
        /// </summary>
        private void BuildSortRow(RectTransform parent)
        {
            _sortArea = CreateRect("SortArea", parent, Vector2.zero, Vector2.zero);
            _sortRowRect = CreateRect("SortRow", _sortArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _sortLabel = CreateTab("Sort", _sortRowRect, () => SetSort((CharacterSort)(((int)_sort + 1) % 3)));
            _orderLabel = CreateTab("Order", _sortRowRect, () => SetSortDescending(!_sortDescending));
            _sortRow = _sortRowRect.gameObject.AddComponent<SoulHunter.Gameplay.UI.ThemedRow>();
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
        private void BuildFavouriteButton(RectTransform parent)
        {
            _favouriteArea = CreateRect("FavouriteArea", parent, Vector2.zero, Vector2.zero);
            var rect = CreateRect("FavouriteButton", _favouriteArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
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
