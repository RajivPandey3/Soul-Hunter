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

            TextMeshProUGUI btnText =
                _upgradeButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);

            if (btnText != null)
            {
                btnText.text =
                    $"{chosenUpgrade.UpgradeName} (Lv {chosenUpgrade.Level})";
            }
            else
            {
                var legacyText =
                    _upgradeButtons[i].GetComponentInChildren<Text>(true);

                if (legacyText != null)
                    legacyText.text =
                        $"{chosenUpgrade.UpgradeName} (Lv {chosenUpgrade.Level})";
            }

            _upgradeButtons[i].onClick.RemoveAllListeners();

            UpgradeData finalUpgrade = chosenUpgrade;

            _upgradeButtons[i].onClick.AddListener(
                () => OnUpgradeSelected(finalUpgrade)
            );

            Debug.Log($"[LevelUpUI] Button {i} configured successfully");
        }
        else
        {
            _upgradeButtons[i].gameObject.SetActive(false);
        }
    }

    Debug.Log("[LevelUpUI] All buttons configured");

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
