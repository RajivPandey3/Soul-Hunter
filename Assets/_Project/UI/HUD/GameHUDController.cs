using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Core.Services;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Learning Comment:
    /// VS Rule: Player ka In-Game HUD (Heads-Up Display).
    /// Ye screen par top-left mein hathiyaron ki tasveerein (icons), Gold aur Kills dikhata hai.
    /// Sath hi Kael ki Health Bar ko update karta hai.
    /// </summary>
    public class GameHUDController : MonoBehaviour
    {
        [Header("Player Health")]
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private HealthController _playerHealth;

        [Header("Economy & Stats")]
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private TextMeshProUGUI _killText;

        [Header("Inventory (Top Left)")]
        [SerializeField] private WeaponManager _weaponManager;
        [Tooltip("VS mein 6 weapons aur 6 passives ke slots hote hain")]
        [SerializeField] private Image[] _weaponSlots;
        [SerializeField] private Image[] _passiveSlots;

        private SoulHunter.Gameplay.Core.RunStatsTracker _stats;
        private WeaponManager _boundWeapons;

        private int _currentWeaponIndex = 0;
        private int _currentPassiveIndex = 0;

        // WeaponType tracking taake ek hi hathiyar ki tasveer dobara add na ho, sirf level barhe
        private List<UpgradeData.UpgradeType> _ownedWeapons = new List<UpgradeData.UpgradeType>();
        private List<UpgradeData.UpgradeType> _ownedPassives = new List<UpgradeData.UpgradeType>();

        private void Start()
        {
            // Health Bar setup
            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged += UpdateHealthBar;
                UpdateHealthBar(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);

                // VS-style low-HP feedback: pulsing red tint at 30% HP or below.
                var lowHealthWarning = GetComponent<LowHealthWarning>();
                if (lowHealthWarning == null) lowHealthWarning = gameObject.AddComponent<LowHealthWarning>();
                lowHealthWarning.Bind(_playerHealth);
            }

            // Weapon Manager setup
            if (_weaponManager != null)
            {
                _boundWeapons = _weaponManager;
                _boundWeapons.OnWeaponAcquiredOrUpgraded += HandleWeaponAcquired;
            }

            // VS HUD shows gold collected this run (the saved total lives in the menu shop).
            _stats = SoulHunter.Gameplay.Core.RunStatsTracker.Instance;
            if (_stats != null)
            {
                _stats.OnKillsChanged += UpdateKills;
                UpdateKills(_stats.TotalKills);
                _stats.OnGoldChanged += UpdateGoldUI;
                UpdateGoldUI(_stats.TotalGoldCollected);
            }

            // Slots ko shuru mein khali karo
            ClearSlots(_weaponSlots);
            ClearSlots(_passiveSlots);
            
     }

        private void OnDestroy()
        {
            if (_playerHealth != null) _playerHealth.OnHealthChanged -= UpdateHealthBar;

            if (_boundWeapons != null) _boundWeapons.OnWeaponAcquiredOrUpgraded -= HandleWeaponAcquired;
            if (_stats != null)
            {
                _stats.OnKillsChanged -= UpdateKills;
                _stats.OnGoldChanged -= UpdateGoldUI;
            }
        }

        private void UpdateHealthBar(int newHealth, int maxHealth)
        {
            if (_healthSlider != null && maxHealth > 0)
            {
                _healthSlider.value = (float)newHealth / maxHealth;
            }
        }

        private void UpdateGoldUI(int gold)
        {
            if (_goldText != null)
            {
                _goldText.text = "Gold: " + gold.ToString();
            }
        }

        private void UpdateKills(int kills)
        {
            if (_killText != null) _killText.text = "Total Kills: " + kills.ToString();
        }

        private void HandleWeaponAcquired(UpgradeData upgrade)
        {
            if (upgrade == null || upgrade.Icon == null) return;

            bool isPassive = upgrade.Type == UpgradeData.UpgradeType.PlayerSpeed ||
                             upgrade.Type == UpgradeData.UpgradeType.MaxHealth;

            var activeList = isPassive ? _ownedPassives : _ownedWeapons;
            var activeSlots = isPassive ? _passiveSlots : _weaponSlots;
            ref int currentIndex = ref (isPassive ? ref _currentPassiveIndex : ref _currentWeaponIndex);

            // Agar naya hathiyar hai, toh naye slot mein tasveer lagao
            if (!activeList.Contains(upgrade.Type))
            {
                if (currentIndex < activeSlots.Length)
                {
                    activeSlots[currentIndex].sprite = upgrade.Icon;
                    activeSlots[currentIndex].color = Color.white; // Make visible
                    activeList.Add(upgrade.Type);
                    currentIndex++;
                }
            }
            // Agar pehle se hai, toh hum slot ke neeche level dikha sakte hain (VS style)
        }

        private void ClearSlots(Image[] slots)
        {
            foreach (var slot in slots)
            {
                if (slot != null)
                {
                    slot.sprite = null;
                    slot.color = new Color(0, 0, 0, 0); // Transparent
                }
            }
        }
    }
}
