using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Learning Comment:
    /// Kami #2 Fix: Chest Animation / UI.
    /// Ye UI tab khulegi jab player Chest uthayega. Ye usko ek random upgrade degi.
    /// Asli VS mein slot machine hoti hai, par prototype ke liye ek shandaar popup kaafi hai.
    /// </summary>
    public class ChestUI : MonoBehaviour
    {
        [SerializeField] private GameObject _chestPanel;
        [SerializeField] private TextMeshProUGUI _rewardText;
        [SerializeField] private Button _claimButton;
        [SerializeField] private WeaponManager _weaponManager;
        [SerializeField] private System.Collections.Generic.List<UpgradeData> _availableUpgrades;

        private void Start()
        {
            if (_chestPanel != null) _chestPanel.SetActive(false);
            
            if (_claimButton != null)
            {
                _claimButton.onClick.AddListener(CloseChest);
            }
        }

        private void OnDestroy()
        {
            if (_claimButton != null) _claimButton.onClick.RemoveListener(CloseChest);
            // Modal UI can be destroyed during a scene transition while the run is paused.
            Time.timeScale = 1f;
        }

        public void OpenChest(string message)
        {
            if (SoulHunter.Gameplay.Audio.AudioManager.Instance != null)
            {
                SoulHunter.Gameplay.Audio.AudioManager.Instance.PlaySFX(SoulHunter.Gameplay.Audio.AudioManager.Instance.ChestOpenSound);
            }

            Time.timeScale = 0f; // Game pause
            
            if (_rewardText != null)
            {
                _rewardText.text = message;
            }

            if (_chestPanel != null) _chestPanel.SetActive(true);
        }

        private void CloseChest()
        {
            if (_chestPanel != null) _chestPanel.SetActive(false);
            Time.timeScale = 1f; // Game resume
        }
    }
}
