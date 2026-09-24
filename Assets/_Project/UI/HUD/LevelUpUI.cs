using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Level up par game pause hota hai aur 3 random cards aate hain.
    /// Ye script buttons par random upgrades set karti hai aur click hone par WeaponManager ko bhejti hai.
    /// Reroll / Skip / Banish buttons: agar Inspector mein assign na hon toh pehle upgrade button ko
    /// clone karke cards ke neeche ek row mein bana diye jaate hain.
    /// </summary>
    public class LevelUpUI : MonoBehaviour
    {
        [SerializeField] private SoulHunter.Gameplay.Core.LevelUpManager _levelUpManager;
        [SerializeField] private GameObject _levelUpPanel;
        [SerializeField] private Button[] _upgradeButtons;
        [SerializeField] private Button _rerollButton;
        [SerializeField] private Button _skipButton;
        [SerializeField] private Button _banishButton;

        private const string ActionBarName = "LevelUp_ActionBar";
        private List<UpgradeData> _currentChoices = new List<UpgradeData>();
        private bool _banishMode;

        private void Awake()
        {
            // Learning Comment:
            // "Read First, Match Later":
            // Agar Inspector mein _levelUpManager assign nahi hai toh dynamic find karo.
            if (_levelUpManager == null)
            {
                _levelUpManager = FindFirstObjectByType<SoulHunter.Gameplay.Core.LevelUpManager>();
            }

            if (_levelUpPanel == null)
            {
                _levelUpPanel = gameObject;
            }

            // Game shuru hote waqt LevelUp Panel ko hide rakhein (Awake mein ek martaba)
            if (_levelUpPanel != null)
            {
                _levelUpPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (_levelUpManager == null)
            {
                _levelUpManager = FindFirstObjectByType<SoulHunter.Gameplay.Core.LevelUpManager>();
            }

            if (_levelUpManager != null)
            {
                _levelUpManager.OnLevelUpUIRound += ShowLevelUpScreen;
            }
            
            // CRITICAL FIX: Yahan se _levelUpPanel.SetActive(false) hata diya gaya hai!
            // Agar yeh script panel par hi lagi ho, to ShowLevelUpScreen ke SetActive(true) call
            // karte hi OnEnable chalega aur panel usi frame mein band ho jata tha.
        }

        private void OnDisable()
        {
            if (_levelUpManager != null)
            {
                _levelUpManager.OnLevelUpUIRound -= ShowLevelUpScreen;
            }
        }

        private void ShowLevelUpScreen(List<UpgradeData> choices)
{
    Debug.Log($"[LevelUpUI] >>> ShowLevelUpScreen ENTER | choices={choices?.Count ?? -1}");

    if (choices == null)
    {
        Debug.LogError("[LevelUpUI] choices == NULL");
        return;
    }

    _currentChoices = choices;
    _banishMode = false;
    EnsureActionButtons();
    EnsureCardCount(choices.Count);

    Debug.Log($"[LevelUpUI] Panel={_levelUpPanel} | PanelActive={(_levelUpPanel != null && _levelUpPanel.activeSelf)}");
    Debug.Log($"[LevelUpUI] Buttons array null={_upgradeButtons == null} | count={(_upgradeButtons != null ? _upgradeButtons.Length : -1)}");

    if (SoulHunter.Gameplay.Audio.AudioManager.Instance != null)
    {
        Debug.Log("[LevelUpUI] Playing LevelUp SFX");
        SoulHunter.Gameplay.Audio.AudioManager.Instance.PlaySFX(
            SoulHunter.Gameplay.Audio.AudioManager.Instance.LevelUpSound
        );
    }

    for (int i = 0; i < _upgradeButtons.Length; i++)
    {
        Debug.Log($"[LevelUpUI] Processing button {i}");

        if (_upgradeButtons[i] == null)
        {
            Debug.LogError($"[LevelUpUI] Button {i} is NULL");
            continue;
        }

        if (i < choices.Count)
        {
            _upgradeButtons[i].gameObject.SetActive(true);

            UpgradeData chosenUpgrade = choices[i];

            Debug.Log(
                $"[LevelUpUI] Button {i} -> {chosenUpgrade.UpgradeName} Lv.{chosenUpgrade.Level}"
            );

            SetButtonLabel(_upgradeButtons[i], CardLabel(chosenUpgrade));

            _upgradeButtons[i].onClick.RemoveAllListeners();

            UpgradeData finalUpgrade = chosenUpgrade;

            _upgradeButtons[i].onClick.AddListener(
                () => OnCardClicked(finalUpgrade)
            );

            Debug.Log($"[LevelUpUI] Button {i} configured successfully");
        }
        else
        {
            _upgradeButtons[i].gameObject.SetActive(false);
        }
    }

    Debug.Log("[LevelUpUI] All buttons configured");

    BindActionButtons();
    RefreshActionButtons();

    if (_levelUpPanel != null)
    {
        Debug.Log("[LevelUpUI] Activating LevelUpPanel");
        _levelUpPanel.SetActive(true);

        Debug.Log(
            $"[LevelUpUI] LevelUpPanel active AFTER SetActive = {_levelUpPanel.activeSelf}"
        );
    }
    else
    {
        Debug.LogError("[LevelUpUI] _levelUpPanel is NULL!");
    }

    Debug.Log("[LevelUpUI] <<< ShowLevelUpScreen EXIT");
}

        private string CardLabel(UpgradeData upgrade)
        {
            string label = $"{upgrade.UpgradeName} (Lv {upgrade.Level})";
            return _banishMode ? $"Banish: {label}" : label;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null) return;
            var tmpText = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmpText != null) { tmpText.text = label; return; }
            var legacyText = button.GetComponentInChildren<Text>(true);
            if (legacyText != null) legacyText.text = label;
        }

        private void OnCardClicked(UpgradeData upgrade)
        {
            if (_banishMode)
            {
                _banishMode = false;
                if (_levelUpManager != null) _levelUpManager.Banish(upgrade);
                HidePanelIfLevelUpFinished();
                return;
            }
            OnUpgradeSelected(upgrade);
        }

        private void OnRerollClicked()
        {
            _banishMode = false;
            if (_levelUpManager != null) _levelUpManager.Reroll();
            HidePanelIfLevelUpFinished();
        }

        private void OnSkipClicked()
        {
            if (_levelUpManager == null || _levelUpManager.SkipsLeft <= 0) return;
            // Close first so a queued level-up can reopen the panel cleanly.
            if (_levelUpPanel != null) _levelUpPanel.SetActive(false);
            _levelUpManager.Skip();
        }

        private void OnBanishClicked()
        {
            if (_levelUpManager == null) return;
            if (!_banishMode && _levelUpManager.BanishesLeft <= 0) return;
            // Banish is a two-step action: arm it, then click the card to remove.
            _banishMode = !_banishMode;
            for (int i = 0; i < _upgradeButtons.Length && i < _currentChoices.Count; i++)
                SetButtonLabel(_upgradeButtons[i], CardLabel(_currentChoices[i]));
            RefreshActionButtons();
        }

        private void HidePanelIfLevelUpFinished()
        {
            if (_levelUpPanel != null && _levelUpManager != null && !_levelUpManager.IsLevelUpActive)
                _levelUpPanel.SetActive(false);
        }

        private void BindActionButtons()
        {
            if (_rerollButton != null) { _rerollButton.onClick.RemoveAllListeners(); _rerollButton.onClick.AddListener(OnRerollClicked); }
            if (_skipButton != null) { _skipButton.onClick.RemoveAllListeners(); _skipButton.onClick.AddListener(OnSkipClicked); }
            if (_banishButton != null) { _banishButton.onClick.RemoveAllListeners(); _banishButton.onClick.AddListener(OnBanishClicked); }
        }

        private void RefreshActionButtons()
        {
            if (_levelUpManager == null) return;
            if (_rerollButton != null)
            {
                SetButtonLabel(_rerollButton, $"Reroll ({_levelUpManager.RerollsLeft})");
                _rerollButton.interactable = _levelUpManager.RerollsLeft > 0;
            }
            if (_skipButton != null)
            {
                SetButtonLabel(_skipButton, $"Skip ({_levelUpManager.SkipsLeft})");
                _skipButton.interactable = _levelUpManager.SkipsLeft > 0;
            }
            if (_banishButton != null)
            {
                SetButtonLabel(_banishButton, _banishMode ? "Cancel Banish" : $"Banish ({_levelUpManager.BanishesLeft})");
                _banishButton.interactable = _banishMode || _levelUpManager.BanishesLeft > 0;
            }
        }

        /// <summary>
        /// Builds the Reroll / Skip / Banish row under the upgrade cards by cloning the first card,
        /// unless the buttons are assigned in the Inspector. The row is created once and shared,
        /// so a second LevelUpUI on the same panel reuses it.
        /// </summary>
        private void EnsureActionButtons()
        {
            if (_rerollButton != null && _skipButton != null && _banishButton != null) return;

            Button template = null;
            foreach (var button in _upgradeButtons)
                if (button != null) { template = button; break; }
            if (template == null) return;

            Transform container = template.transform.parent;
            Transform bar = container.Find(ActionBarName);
            if (bar == null)
            {
                var templateRect = (RectTransform)template.transform;
                var barObject = new GameObject(ActionBarName, typeof(RectTransform));
                barObject.layer = template.gameObject.layer;
                bar = barObject.transform;
                bar.SetParent(container, false);
                bar.SetAsLastSibling();
                ((RectTransform)bar).sizeDelta = templateRect.sizeDelta;

                var layout = barObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 24f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;
                barObject.AddComponent<LayoutElement>().preferredHeight = templateRect.sizeDelta.y;

                CreateActionButton(template, bar, "Reroll_Button");
                CreateActionButton(template, bar, "Skip_Button");
                CreateActionButton(template, bar, "Banish_Button");
            }

            if (_rerollButton == null) _rerollButton = FindActionButton(bar, "Reroll_Button");
            if (_skipButton == null) _skipButton = FindActionButton(bar, "Skip_Button");
            if (_banishButton == null) _banishButton = FindActionButton(bar, "Banish_Button");
        }

        /// <summary>
        /// Luck can deal a 4th choice, but the panel only ships 3 cards. Clone the last card for any
        /// missing ones (reusing an existing clone by name, so a second LevelUpUI shares it).
        /// Extra cards are hidden on rounds with fewer choices by the normal card loop.
        /// </summary>
        private void EnsureCardCount(int count)
        {
            if (_upgradeButtons == null || _upgradeButtons.Length >= count) return;

            Button template = null;
            for (int i = _upgradeButtons.Length - 1; i >= 0 && template == null; i--)
                template = _upgradeButtons[i];
            if (template == null) return;

            Transform container = template.transform.parent;
            var cards = new List<Button>(_upgradeButtons);
            for (int i = cards.Count; i < count; i++)
            {
                string cardName = $"Upgrade_Button_{i}";
                Transform existing = container.Find(cardName);
                Button card = existing != null ? existing.GetComponent<Button>() : null;
                if (card == null)
                {
                    card = Instantiate(template, container, false);
                    card.name = cardName;
                    card.onClick = new Button.ButtonClickedEvent();
                    // Keep the new card with the others, above the action row.
                    card.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
                }
                cards.Add(card);
                template = card;
            }
            _upgradeButtons = cards.ToArray();
        }

        private static void CreateActionButton(Button template, Transform parent, string name)
        {
            Button clone = Instantiate(template, parent, false);
            clone.name = name;
            clone.gameObject.SetActive(true);
            clone.onClick = new Button.ButtonClickedEvent(); // drop any listeners copied from the card
        }

        private static Button FindActionButton(Transform bar, string name)
        {
            Transform child = bar.Find(name);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private void OnUpgradeSelected(UpgradeData selectedUpgrade)
        {
            // Panel ko pehle band karein taake agar agla level up queue mein ho toh wo cleanly open ho sake
            if (_levelUpPanel != null)
            {
                _levelUpPanel.SetActive(false);
            }

            if (_levelUpManager != null)
            {
                _levelUpManager.SelectUpgrade(selectedUpgrade);
            }
        }
    }
}
