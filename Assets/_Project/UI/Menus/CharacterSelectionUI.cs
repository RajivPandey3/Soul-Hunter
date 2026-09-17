using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SoulHunter.Gameplay.Data;
using SoulHunter.Core.Services;

namespace SoulHunter.UI
{
    /// <summary>
    /// Learning Comment:
    /// VS Rule: Main Menu mein game shuru karne se pehle player hero chunta hai.
    /// Ye script us chunaav ko SaveService me mehfooz karti hai taake Gameplay scene mein wahi hero spawn ho.
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

        private int _currentIndex = 0;

        private void Start()
        {
            if (_availableCharacters == null || _availableCharacters.Count == 0)
            {
                // Fallback Load from Resources if empty
                var catalog = GameContentCatalog.Load();
                _availableCharacters = new List<CharacterData>(catalog != null ? catalog.Characters : Resources.LoadAll<CharacterData>("Characters"));
            }

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

        private void NextCharacter()
        {
            if (_availableCharacters.Count == 0) return;
            _currentIndex = (_currentIndex + 1) % _availableCharacters.Count;
            UpdateUI();
        }

        private void PrevCharacter()
        {
            if (_availableCharacters.Count == 0) return;
            _currentIndex--;
            if (_currentIndex < 0) _currentIndex = _availableCharacters.Count - 1;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_currentIndex < 0 || _currentIndex >= _availableCharacters.Count) return;

            CharacterData charData = _availableCharacters[_currentIndex];

            if (_characterNameText) _characterNameText.text = charData.CharacterName;
            if (_characterDescriptionText) _characterDescriptionText.text = charData.Description;
            if (_characterIconDisplay) _characterIconDisplay.sprite = charData.CharacterIcon;

            if (_statsText)
            {
                _statsText.text = $"Starting Weapon: {charData.StartingWeapon}\n" +
                                  $"Max Health: {charData.BaseMaxHealth}\n" +
                                  $"Speed: {charData.BaseMoveSpeed}\n" +
                                  $"Might: {charData.StartingMight * 100}%";
            }
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
    }
}
