using UnityEngine;
using System.Collections.Generic;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using System.Linq;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Learning Comment:
    /// Yeh class Level Up hone par Upgrade Menu ki logic chalati hai.
    /// Ye game ko pause krti hai aur 3 random valid upgrades chunti hai.
    /// </summary>
    public class LevelUpManager : MonoBehaviour
    {
        private List<UpgradeData> _allUpgrades;
        [SerializeField] private PlayerExperience _experience;
        [SerializeField] private WeaponManager _weaponManager;
        [SerializeField] private PlayerController _playerController;
        
        // Is event ko UI system listen karega
        public event System.Action<List<UpgradeData>> OnLevelUpUIRound;

        private void Start()
        {
            // Unity Inspector se bachne ke liye Resources folder se load kar rahe hain
            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            _allUpgrades = (catalog != null ? catalog.Upgrades : Resources.LoadAll<UpgradeData>("Upgrades")).Where(u => u != null && u.Level > 0).ToList();
            
            if (_allUpgrades.Count == 0)
            {
                Debug.LogWarning("[LevelUpManager] Koi UpgradeData nahi mila! Kya wo 'Resources/Upgrades' folder mein hain?");
            }

            if (_experience != null)
            {
                _experience.OnLevelUp += HandleLevelUp;
            }
        }
        
        private void OnDestroy()
        {
            if (_experience != null)
            {
                _experience.OnLevelUp -= HandleLevelUp;
            }
        }

        private void HandleLevelUp(int newLevel)
        {
            Debug.Log($"[LevelUpManager] Game Paused for Level {newLevel} Upgrades!");
            // Pause the game
            Time.timeScale = 0f;
            
            // Pick 3 random upgrades
            List<UpgradeData> choices = GetRandomValidUpgrades(3);
            if (choices.Count == 0) { ResumeGameplay(); return; }
            
            // Agar koi UI is event ko sun raha hai toh usko options bhejo
            if (OnLevelUpUIRound != null)
            {
                OnLevelUpUIRound.Invoke(choices);
            }
            else
            {
                // UI abhi nahi bani (UI BAAD MEIN BANAYEGEIN), isliye Backend test ke liye auto-select:
                if (choices.Count > 0)
                {
                    Debug.Log($"[LevelUpManager] (No UI Found) Auto-Selecting: {choices[0].UpgradeName}");
                    SelectUpgrade(choices[0]);
                }
                else
                {
                    // Agar koi upgrade hi na bacha ho
                    Debug.Log("[LevelUpManager] Max limit reached! No more upgrades available.");
                    ResumeGameplay();
                }
            }
        }
        
        /// <summary>
        /// Jab player UI mein kisi ek card par click karega, toh yeh function call hoga.
        /// </summary>
        public void SelectUpgrade(UpgradeData upgrade)
        {
            if (upgrade == null) return;
            if (_weaponManager != null)
            {
                _weaponManager.ApplyUpgrade(upgrade);
            }
            
            // Resume game
            ResumeGameplay();
            Debug.Log($"[LevelUpManager] Game Resumed. Acquired: {upgrade.UpgradeName}");
        }

        private void ResumeGameplay()
        {
            Time.timeScale = 1f;
            if (_playerController != null) _playerController.ResumeMovementAfterMenu();
        }

        public List<UpgradeData> GetRandomValidUpgrades(int count)
        {
            List<UpgradeData> validUpgrades = new List<UpgradeData>();
            if (_weaponManager == null || _allUpgrades == null) return validUpgrades;
            
            foreach (var up in _allUpgrades)
            {
                // Logic: Agar Upgrade Level 1 hai, toh player ke paas wo Level 0 par hona chahiye.
                int currentLvl = _weaponManager.GetWeaponLevel(up.Type);
                if (currentLvl == up.Level - 1 && up.Level <= _weaponManager.GetMaxWeaponLevel(up.Type) &&
                    !(currentLvl == 0 && WeaponManager.IsStagePassive(up.Type)))
                {
                    validUpgrades.Add(up);
                }
            }
            
            // Shuffle and pick
            List<UpgradeData> result = new List<UpgradeData>();
            while (result.Count < count && validUpgrades.Count > 0)
            {
                int index = Random.Range(0, validUpgrades.Count);
                result.Add(validUpgrades[index]);
                validUpgrades.RemoveAt(index); // Ensure no duplicates
            }
            return result;
        }

        public List<UpgradeData> GetRandomValidUpgradesForChest()
        {
            return GetRandomValidUpgrades(1);
        }
    }
}
