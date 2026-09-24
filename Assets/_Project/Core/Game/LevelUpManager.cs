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

        // VS rule: Reroll naye choices deta hai, Skip level-up chhod deta hai, Banish item ko
        // is run ke liye pool se hata deta hai. Starting charges provisional hain (baad mein
        // meta shop / character se aayenge).
        [Header("Level-up actions (charges per run)")]
        [SerializeField] private int _startingRerolls = 2;
        [SerializeField] private int _startingSkips = 2;
        [SerializeField] private int _startingBanishes = 2;

        public int RerollsLeft { get; private set; }
        public int SkipsLeft { get; private set; }
        public int BanishesLeft { get; private set; }
        public bool IsLevelUpActive => _isLevelUpActive;

        private readonly HashSet<UpgradeData.UpgradeType> _banishedTypes = new HashSet<UpgradeData.UpgradeType>();

        public bool IsBanished(UpgradeData.UpgradeType type) => _banishedTypes.Contains(type);

        public const int BaseChoiceCount = 3;
        private int _currentChoiceCount = BaseChoiceCount;

        /// <summary>
        /// VS rule: the chance of a 4th level-up choice is 1 - 1/Luck (0% at Luck 1, 50% at Luck 2).
        /// <paramref name="roll01"/> is a uniform random value in [0, 1).
        /// </summary>
        public static int ChoiceCountForLuck(float luck, float roll01)
        {
            if (luck <= 1f) return BaseChoiceCount;
            float fourthChance = 1f - 1f / luck;
            return roll01 < fourthChance ? BaseChoiceCount + 1 : BaseChoiceCount;
        }

        private void Awake()
        {
            RerollsLeft = Mathf.Max(0, _startingRerolls);
            SkipsLeft = Mathf.Max(0, _startingSkips);
            BanishesLeft = Mathf.Max(0, _startingBanishes);

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
    // VS rule: Luck gives a chance of a 4th choice, rolled once per level-up.
    var stats = _playerController != null ? _playerController.Stats : null;
    _currentChoiceCount = ChoiceCountForLuck(stats != null ? stats.Luck : 1f, Random.value);

    Debug.Log($"[LevelUpManager] >>> Calling GetRandomValidUpgrades({_currentChoiceCount})");

    List<UpgradeData> choices = GetRandomValidUpgrades(_currentChoiceCount);

    Debug.Log($"[LevelUpManager] <<< GetRandomValidUpgrades returned | count={choices.Count}");

    if (choices.Count == 0)
    {
        // VS rule: once every item is maxed, a level-up still pays out
        // (floor chicken heal + gold) instead of being thrown away.
        Debug.Log($"[LevelUpManager] No valid upgrades available. Granting {_pendingLevelUps} maxed-out reward(s).");
        for (; _pendingLevelUps > 0; _pendingLevelUps--) GrantMaxedOutReward();
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
    SoulHunter.Core.Services.GameTime.Pause(this);

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
            CompleteCurrentLevelUp();
        }

        /// <summary>Offers a fresh set of choices for the current level-up. Returns false when not allowed.</summary>
        public bool Reroll()
        {
            if (!_isLevelUpActive || RerollsLeft <= 0) return false;
            RerollsLeft--;
            PresentChoicesOrPayOut(GetRandomValidUpgrades(_currentChoiceCount));
            return true;
        }

        /// <summary>Gives up the current level-up without taking anything. Returns false when not allowed.</summary>
        public bool Skip()
        {
            if (!_isLevelUpActive || SkipsLeft <= 0) return false;
            SkipsLeft--;
            Debug.Log("[LevelUpManager] Level-up skipped.");
            CompleteCurrentLevelUp();
            return true;
        }

        /// <summary>
        /// Removes an item from the pool for the rest of the run (level-ups and chests),
        /// then re-rolls the current choices. Returns false when not allowed.
        /// </summary>
        public bool Banish(UpgradeData upgrade)
        {
            if (!_isLevelUpActive || BanishesLeft <= 0 || upgrade == null) return false;
            BanishesLeft--;
            _banishedTypes.Add(upgrade.Type);
            Debug.Log($"[LevelUpManager] Banished {upgrade.Type} for this run.");
            PresentChoicesOrPayOut(GetRandomValidUpgrades(_currentChoiceCount));
            return true;
        }

        private void PresentChoicesOrPayOut(List<UpgradeData> choices)
        {
            if (choices.Count == 0)
            {
                // Banishing the last available item: pay out like a maxed-out level-up.
                GrantMaxedOutReward();
                CompleteCurrentLevelUp();
                return;
            }
            OnLevelUpUIRound?.Invoke(choices);
        }

        private void CompleteCurrentLevelUp()
        {
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

        public const int MaxedOutHealAmount = 30;
        public const int MaxedOutGoldAmount = 25;

        private void GrantMaxedOutReward()
        {
            var health = _playerController != null ? _playerController.GetComponent<HealthController>() : null;
            if (health != null) health.Heal(MaxedOutHealAmount);

            SoulHunter.Gameplay.Pickups.GoldRewards.Grant(MaxedOutGoldAmount, _playerController != null ? _playerController.Stats : null);
        }

        private void ResumeGameplay()
        {
            SoulHunter.Core.Services.GameTime.Resume(this);
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
                // New items are only offered while a slot is free; otherwise picking
                // one would be rejected by WeaponManager and waste the level-up.
                if (currentLvl == up.Level - 1 && up.Level <= _weaponManager.GetMaxWeaponLevel(up.Type) &&
                    !(currentLvl == 0 && WeaponManager.IsStagePassive(up.Type)) &&
                    _weaponManager.HasSlotFor(up.Type) && !_banishedTypes.Contains(up.Type))
                {
                    validUpgrades.Add(up);
                }
            }
            
            // Rarity-weighted pick without duplicates (VS rule: rare items show up less often)
            List<UpgradeData> result = new List<UpgradeData>();
            while (result.Count < count && validUpgrades.Count > 0)
            {
                int index = PickWeightedIndex(validUpgrades, Random.value);
                result.Add(validUpgrades[index]);
                validUpgrades.RemoveAt(index); // Ensure no duplicates
            }
            return result;
        }

        /// <summary>
        /// Picks an index with probability proportional to each upgrade's rarity weight.
        /// <paramref name="roll01"/> is a uniform random value in [0, 1).
        /// </summary>
        public static int PickWeightedIndex(List<UpgradeData> upgrades, float roll01)
        {
            int total = 0;
            foreach (var up in upgrades) total += Mathf.Max(0, UpgradeRarity.GetWeight(up.Type));
            if (total <= 0) return Mathf.Clamp(Mathf.FloorToInt(roll01 * upgrades.Count), 0, upgrades.Count - 1);

            float target = roll01 * total;
            for (int i = 0; i < upgrades.Count; i++)
            {
                target -= Mathf.Max(0, UpgradeRarity.GetWeight(upgrades[i].Type));
                if (target < 0f) return i;
            }
            return upgrades.Count - 1;
        }

        public List<UpgradeData> GetRandomValidUpgradesForChest()
        {
            return GetRandomValidUpgrades(1);
        }
    }
}
