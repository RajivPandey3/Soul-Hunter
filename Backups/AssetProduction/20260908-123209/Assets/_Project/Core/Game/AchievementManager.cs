using UnityEngine;
using System.Collections.Generic;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Learning Comment:
    /// Game ke doran ye script check karti hai ke kya kisi achievement ka target poora hua.
    /// Agar poora hota hai, toh ye UI ko notify karti hai.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        [SerializeField] private List<AchievementData> _allAchievements;
        
        // Jo khul chuke hain unki list (Ideally ye SaveData me hoti hai)
        private HashSet<string> _unlockedAchievements = new HashSet<string>();

        private float _survivedTime = 0f;
        private GameSessionManager _session;
        private RunStatsTracker _stats;
        private SoulHunter.Gameplay.Combat.WeaponManager _weapons;
        private SoulHunter.Core.Persistence.SaveService _save;

        private void Start()
        {
            _save = SoulHunter.Core.Services.GameServices.Instance?.Get<SoulHunter.Core.Persistence.SaveService>();
            if (_save?.CurrentData?.UnlockedAchievements != null)
            {
                foreach (var name in _save.CurrentData.UnlockedAchievements)
                    if (!string.IsNullOrEmpty(name)) _unlockedAchievements.Add(name);
            }
            CheckAchievements();
        }

        private void OnEnable()
        {
            _session = GameSessionManager.Instance != null ? GameSessionManager.Instance : FindFirstObjectByType<GameSessionManager>();
            _stats = RunStatsTracker.Instance != null ? RunStatsTracker.Instance : FindFirstObjectByType<RunStatsTracker>();
            _weapons = FindFirstObjectByType<SoulHunter.Gameplay.Combat.WeaponManager>();
            if (_session != null) _session.OnSurvivalSecondChanged += HandleSurvivalSecond;
            if (_stats != null) _stats.OnKillsChanged += HandleKillsChanged;
            if (_weapons != null) _weapons.OnWeaponAcquiredOrUpgraded += HandleWeaponUpgrade;
        }

        private void OnDisable()
        {
            if (_session != null) _session.OnSurvivalSecondChanged -= HandleSurvivalSecond;
            if (_stats != null) _stats.OnKillsChanged -= HandleKillsChanged;
            if (_weapons != null) _weapons.OnWeaponAcquiredOrUpgraded -= HandleWeaponUpgrade;
        }

        private void HandleSurvivalSecond(int second)
        {
            _survivedTime = second;
            CheckAchievements();
        }

        private void HandleKillsChanged(int _)
        {
            CheckAchievements();
        }

        private void HandleWeaponUpgrade(SoulHunter.Gameplay.Combat.UpgradeData _)
        {
            CheckAchievements();
        }

        private void CheckAchievements()
        {
            if (_allAchievements == null) return;

            foreach (var ach in _allAchievements)
            {
                if (_unlockedAchievements.Contains(ach.AchievementName)) continue; // Pehle hi khul chuka hai

                bool isUnlocked = false;

                switch (ach.Type)
                {
                    case AchievementData.AchievementType.SurviveTime:
                        if (_survivedTime >= ach.TargetValue) isUnlocked = true;
                        break;
                    
                    case AchievementData.AchievementType.KillCount:
                        if (RunStatsTracker.Instance != null && RunStatsTracker.Instance.TotalKills >= ach.TargetValue)
                            isUnlocked = true;
                        break;

                    case AchievementData.AchievementType.WeaponLevelUp:
                        if (_weapons != null &&
                            System.Enum.TryParse(ach.TargetWeaponName, out SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType weaponType) &&
                            _weapons.GetWeaponLevel(weaponType) >= ach.TargetValue)
                            isUnlocked = true;
                        break;

                    // Weapon level check is more complex, requiring polling WeaponManager
                    // For now, we handle Survive and Kills automatically.
                }

                if (isUnlocked)
                {
                    UnlockAchievement(ach);
                }
            }
        }

        private void UnlockAchievement(AchievementData ach)
        {
            _unlockedAchievements.Add(ach.AchievementName);
            if (_save != null && _save.CurrentData != null)
            {
                if (_save.CurrentData.UnlockedAchievements == null)
                    _save.CurrentData.UnlockedAchievements = new List<string>();
                if (!_save.CurrentData.UnlockedAchievements.Contains(ach.AchievementName))
                {
                    _save.CurrentData.UnlockedAchievements.Add(ach.AchievementName);
                    _save.SaveGame();
                }
            }
            Debug.Log($"<color=cyan>[Achievement UNLOCKED] {ach.AchievementName} - {ach.UnlockedItemName} ab game mein available hai!</color>");
            
            // Yahan hum UI Popup trigger kar sakte hain
        }
    }
}
