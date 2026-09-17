using UnityEngine;
using System.Collections.Generic;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Learning Comment:
    /// Game ke doran ye script check karti hai ke kya kisi achievement ka target poora hua.
    /// Agar poora hota hai, toh ye UI ko notify karti hai.
    /// Dependencies ko Inspector ([SerializeField]) ya Configure/Initialize methods ke zariye deterministically
    /// inject kiya jata hai taake runtime scene searches (FindFirstObjectByType) par inhisar na rahe.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        [SerializeField] private List<AchievementData> _allAchievements;

        [Header("Dependencies")]
        [SerializeField] private GameSessionManager _session;
        [SerializeField] private RunStatsTracker _stats;
        [SerializeField] private WeaponManager _weapons;
        
        // Jo khul chuke hain unki list (Ideally ye SaveData me hoti hai)
        private HashSet<string> _unlockedAchievements = new HashSet<string>();

        private float _survivedTime = 0f;
        private SoulHunter.Core.Persistence.SaveService _save;

        public GameSessionManager Session => _session;
        public RunStatsTracker Stats => _stats;
        public WeaponManager Weapons => _weapons;
        public IReadOnlyCollection<string> UnlockedAchievements => _unlockedAchievements;

        /// <summary>
        /// Learning Comment:
        /// Deterministic dependency configuration / injection method.
        /// Runtime scene lookups ke bajaye dependencies direct pass ki ja sakti hain (e.g. testing ya setup scripts mein).
        /// </summary>
        public void Configure(GameSessionManager session, RunStatsTracker stats, WeaponManager weapons)
        {
            if (isActiveAndEnabled)
            {
                UnsubscribeEvents();
            }

            _session = session;
            _stats = stats;
            _weapons = weapons;

            if (isActiveAndEnabled)
            {
                SubscribeEvents();
            }
        }

        public void Initialize(GameSessionManager session, RunStatsTracker stats, WeaponManager weapons)
        {
            Configure(session, stats, weapons);
        }

        public void SetDependencies(GameSessionManager session, RunStatsTracker stats, WeaponManager weapons)
        {
            Configure(session, stats, weapons);
        }

        public void SetAchievements(List<AchievementData> achievements)
        {
            _allAchievements = achievements;
        }

        public bool IsAchievementUnlocked(string achievementName)
        {
            return _unlockedAchievements != null && _unlockedAchievements.Contains(achievementName);
        }

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
            // Learning Comment:
            // Agar serialized field ya injection se reference pehle se assign na ho, tou static singletons se link karein,
            // lekin FindFirstObjectByType jese non-deterministic runtime searches use na karein.
            if (_session == null) _session = GameSessionManager.Instance;
            if (_stats == null) _stats = RunStatsTracker.Instance;
            if (_weapons == null) _weapons = WeaponManager.Instance;

            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();

            if (_session != null) _session.OnSurvivalSecondChanged += HandleSurvivalSecond;
            if (_stats != null) _stats.OnKillsChanged += HandleKillsChanged;
            if (_weapons != null) _weapons.OnWeaponAcquiredOrUpgraded += HandleWeaponUpgrade;
        }

        private void UnsubscribeEvents()
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

        public void CheckAchievements()
        {
            if (_allAchievements == null) return;

            foreach (var ach in _allAchievements)
            {
                if (ach == null) continue;
                if (_unlockedAchievements.Contains(ach.AchievementName)) continue; // Pehle hi khul chuka hai

                bool isUnlocked = false;

                switch (ach.Type)
                {
                    case AchievementData.AchievementType.SurviveTime:
                        if (_survivedTime >= ach.TargetValue) isUnlocked = true;
                        break;
                    
                    case AchievementData.AchievementType.KillCount:
                        // Learning Comment:
                        // Global RunStatsTracker.Instance ke bajaye bound _stats instance ko evaluate karte hain.
                        if (_stats != null && _stats.TotalKills >= ach.TargetValue)
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
