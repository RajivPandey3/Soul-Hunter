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
        [SerializeField, Min(1)] private int _gridColumns = 4;
        [SerializeField] private Color _panelColor = new Color(0.10f, 0.10f, 0.16f, 0.88f);
        [SerializeField] private Color _cardColor = new Color(0.28f, 0.30f, 0.42f, 1f);
        [SerializeField] private Color _selectedCardColor = new Color(0.86f, 0.66f, 0.22f, 1f);
        [SerializeField] private Color _bonusColor = new Color(1f, 0.85f, 0.25f, 1f);

        private int _currentIndex = 0;
        private readonly List<Button> _cards = new List<Button>();
        private readonly List<Image> _cardBackgrounds = new List<Image>();
        private Image _weaponIconDisplay;
        private TextMeshProUGUI _weaponNameText;
        private ScrollRect _gridScroll;
        private Dictionary<UpgradeData.UpgradeType, UpgradeData> _weaponInfo;

        public int SelectedIndex => _currentIndex;
        public CharacterData SelectedCharacter =>
            _availableCharacters != null && _currentIndex >= 0 && _currentIndex < _availableCharacters.Count ? _availableCharacters[_currentIndex] : null;
        public IReadOnlyList<Button> Cards => _cards;
        public TextMeshProUGUI StatsText => _statsText;

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
        }

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
            _currentIndex = index;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_currentIndex < 0 || _currentIndex >= _availableCharacters.Count) return;

            CharacterData charData = _availableCharacters[_currentIndex];

            if (_characterNameText) _characterNameText.text = charData.CharacterName;
            if (_characterDescriptionText) _characterDescriptionText.text = charData.Description;
            if (_characterIconDisplay)
            {
                _characterIconDisplay.sprite = charData.CharacterIcon;
                _characterIconDisplay.preserveAspect = true;
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
                if (_cardBackgrounds[i] != null) _cardBackgrounds[i].color = i == _currentIndex ? _selectedCardColor : _cardColor;
            ScrollToSelected();
        }

        private void UpdateStats(CharacterData charData)
        {
            if (_statsText == null) return;
            var data = SaveData();
            var sheet = CharacterStatSheet.Build(charData,
                data != null ? data.MetaMightLevel : 0, data != null ? data.MetaArmorLevel : 0,
                data != null ? data.MetaGreedLevel : 0, data != null ? data.MetaRevivalLevel : 0);

            // One text with values in a column (<pos>), so labels and values always share a font size.
            var text = new System.Text.StringBuilder("<b>Stats</b>\n");
            string bonus = ColorUtility.ToHtmlStringRGB(_bonusColor);
            foreach (var row in sheet.DisplayRows())
            {
                text.Append(row.Label).Append("<pos=68%>");
                text.Append(row.Changed ? $"<color=#{bonus}>{row.Value}</color>" : row.Value).Append('\n');
            }
            text.Append("\nStarting Weapon\n").Append(WeaponName(charData.StartingWeapon));
            _statsText.richText = true;
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

            CharacterData charData = _availableCharacters[_currentIndex];

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

        // ---------- Data ----------

        private static SoulHunter.Core.Persistence.GameData SaveData()
        {
            if (GameServices.Instance == null) return null;
            GameServices.Instance.TryGet(out SoulHunter.Core.Persistence.SaveService save);
            return save != null ? save.CurrentData : null;
        }

        private static string SavedSelection() => SaveData()?.SelectedCharacterName;

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

            // Title and Back along the top.
            var title = root.Find("Text (TMP)") as RectTransform;
            if (title != null && title.TryGetComponent(out TextMeshProUGUI titleText))
            {
                Place(title, root, new Vector2(0.27f, 0.895f), new Vector2(0.73f, 0.975f));
                titleText.text = "Character Selection";
                titleText.alignment = TextAlignmentOptions.Center;
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
            var gridBox = CreatePanel("CharacterGrid", root, new Vector2(0.27f, 0.33f), new Vector2(0.73f, 0.88f));
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
            _weaponNameText = CreateText("StartingWeapon_Text", infoBox, new Vector2(0.79f, 0.04f), new Vector2(0.99f, 0.28f), "", TextAlignmentOptions.Center, 12f, 22f);

            // Bottom right: Confirm.
            if (_selectAndPlayButton != null)
            {
                Place((RectTransform)_selectAndPlayButton.transform, root, new Vector2(0.76f, 0.08f), new Vector2(0.95f, 0.19f));
                var label = _selectAndPlayButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = "Confirm";
            }
        }

        private void BuildCard(int index, RectTransform content)
        {
            var character = _availableCharacters[index];
            var card = CreateRect($"Card_{character.CharacterName}", content, Vector2.zero, Vector2.one);
            var background = card.gameObject.AddComponent<Image>();
            background.color = _cardColor;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => SelectCharacter(index));
            var focus = card.gameObject.AddComponent<CharacterCard>();
            focus.Index = index;
            focus.Selected = SelectCharacter;

            CreateText("Name", card, new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.97f), character.CharacterName, TextAlignmentOptions.TopLeft, 12f, 24f);
            CreateImage("Portrait", card, new Vector2(0.05f, 0.05f), new Vector2(0.6f, 0.74f), character.CharacterIcon);
            var weapon = WeaponInfo(character.StartingWeapon);
            CreateImage("Weapon", card, new Vector2(0.62f, 0.05f), new Vector2(0.95f, 0.42f), weapon != null ? weapon.Icon : null);

            _cards.Add(button);
            _cardBackgrounds.Add(background);
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
            image.color = _panelColor;
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
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
        }
    }
}
