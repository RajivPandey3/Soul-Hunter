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

        // Pending level-ups ki queue (agar ek sath bohot saara XP mile aur multiple level ups hon)
        private int _pendingLevelUps = 0;
        private bool _isLevelUpActive = false;

        private void Awake()
        {
            // Learning Comment:
            // "Read First, Match Later" aur Self-Healing pattern:
            // Agar Inspector mein references unlinked / null reh jayein, toh scene se dynamically dhoond kar bind karein.
            if (_experience == null)
            {
                _experience = FindFirstObjectByType<PlayerExperience>();
            }
            if (_weaponManager == null)
            {
                _weaponManager = FindFirstObjectByType<WeaponManager>();
            }
            if (_playerController == null)
            {
                _playerController = FindFirstObjectByType<PlayerController>();
            }
        }

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
            else
            {
                Debug.LogError("[LevelUpManager] PlayerExperience nahi mila! Level Up event bind nahi ho saka.");
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
            Debug.Log($"[LevelUpManager] Level Up Event received! New Level: {newLevel}");
            _pendingLevelUps++;

            // Agar pehle se koi Level Up menu nahi khula, toh agla process karo
            if (!_isLevelUpActive)
            {
                ProcessNextLevelUp();
            }
        }

        private void ProcessNextLevelUp()
{
    Debug.Log($"[LevelUpManager] >>> ProcessNextLevelUp ENTER | pending={_pendingLevelUps} | active={_isLevelUpActive} | timeScale={Time.timeScale}");

    if (_pendingLevelUps <= 0)
    {
        Debug.Log("[LevelUpManager] No pending level-ups. Resuming gameplay.");
        _isLevelUpActive = false;
        ResumeGameplay();
        return;
    }

    _isLevelUpActive = true;

    Debug.Log($"[LevelUpManager] >>> Preparing level-up UI | pending={_pendingLevelUps}");

    // IMPORTANT: generate choices BEFORE pausing.
    Debug.Log("[LevelUpManager] >>> Calling GetRandomValidUpgrades(3)");

    List<UpgradeData> choices = GetRandomValidUpgrades(3);

    Debug.Log($"[LevelUpManager] <<< GetRandomValidUpgrades returned | count={choices.Count}");

    if (choices.Count == 0)
    {
        Debug.LogWarning("[LevelUpManager] No valid upgrades available. Cancelling level-up menu.");
        _pendingLevelUps = 0;
        _isLevelUpActive = false;
        ResumeGameplay();
        return;
    }

    Debug.Log("[LevelUpManager] >>> Valid choices found:");

    for (int i = 0; i < choices.Count; i++)
    {
        Debug.Log($"[LevelUpManager] Choice {i}: {choices[i].UpgradeName} | Level={choices[i].Level} | Type={choices[i].Type}");
    }

    // ONLY pause once we know the UI can be presented.
    Time.timeScale = 0f;

    Debug.Log($"[LevelUpManager] >>> Game PAUSED | timeScale={Time.timeScale}");

    if (OnLevelUpUIRound != null)
    {
        Debug.Log("[LevelUpManager] >>> Sending choices to LevelUpUI");

        OnLevelUpUIRound.Invoke(choices);

        Debug.Log("[LevelUpManager] <<< LevelUpUI event returned");
    }
    else
    {
        Debug.LogWarning("[LevelUpManager] No LevelUpUI subscriber found. Auto-selecting first upgrade.");

        SelectUpgrade(choices[0]);
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
            
            Debug.Log($"[LevelUpManager] Acquired Upgrade: {upgrade.UpgradeName}");
            _pendingLevelUps = Mathf.Max(0, _pendingLevelUps - 1);

            // Agar aur level ups pending hain toh agla round dikhao, warna game resume karo
            if (_pendingLevelUps > 0)
            {
                ProcessNextLevelUp();
            }
            else
            {
                _isLevelUpActive = false;
                ResumeGameplay();
                Debug.Log("[LevelUpManager] All Level Ups processed. Game Resumed.");
            }
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
