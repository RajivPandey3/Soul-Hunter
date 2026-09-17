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
    /// </summary>
    public class LevelUpUI : MonoBehaviour
    {
        [SerializeField] private SoulHunter.Gameplay.Core.LevelUpManager _levelUpManager;
        [SerializeField] private GameObject _levelUpPanel;
        [SerializeField] private Button[] _upgradeButtons;

        private void OnEnable()
        {
            if (_levelUpManager != null)
            {
                _levelUpManager.OnLevelUpUIRound += ShowLevelUpScreen;
            }
            
            if (_levelUpPanel != null)
            {
                _levelUpPanel.SetActive(false);
            }
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
            if (SoulHunter.Gameplay.Audio.AudioManager.Instance != null)
            {
                SoulHunter.Gameplay.Audio.AudioManager.Instance.PlaySFX(SoulHunter.Gameplay.Audio.AudioManager.Instance.LevelUpSound);
            }

            // Assign to UI Buttons
            for (int i = 0; i < _upgradeButtons.Length; i++)
            {
                if (_upgradeButtons[i] == null) continue;

                if (i < choices.Count)
                {
                    _upgradeButtons[i].gameObject.SetActive(true);
                    UpgradeData chosenUpgrade = choices[i];
                    
                    TextMeshProUGUI btnText = _upgradeButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null)
                    {
                        btnText.text = $"{chosenUpgrade.UpgradeName} (Lv {chosenUpgrade.Level})";
                    }
                    else
                    {
                        var legacyText = _upgradeButtons[i].GetComponentInChildren<Text>(true);
                        if (legacyText != null) legacyText.text = $"{chosenUpgrade.UpgradeName} (Lv {chosenUpgrade.Level})";
                    }

                    _upgradeButtons[i].onClick.RemoveAllListeners();
                    
                    UpgradeData finalUpgrade = chosenUpgrade;
                    _upgradeButtons[i].onClick.AddListener(() => OnUpgradeSelected(finalUpgrade));
                }
                else
                {
                    // Hide buttons if we don't have enough valid upgrades
                    _upgradeButtons[i].gameObject.SetActive(false);
                }
            }

            if (_levelUpPanel != null)
            {
                _levelUpPanel.SetActive(true);
            }
        }

        private void OnUpgradeSelected(UpgradeData selectedUpgrade)
        {
            if (_levelUpManager != null)
            {
                _levelUpManager.SelectUpgrade(selectedUpgrade);
            }

            if (_levelUpPanel != null)
            {
                _levelUpPanel.SetActive(false);
            }
        }
    }
}
