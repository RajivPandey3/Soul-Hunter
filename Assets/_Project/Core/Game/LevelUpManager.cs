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

        /// <summary>VS Golden Egg rule: the selected character's Soul Eggs add whole Reroll, Skip and Banish charges.</summary>
        private void AddSoulEggCharges()
        {
            if (SoulHunter.Core.Services.GameServices.Instance == null ||
                !SoulHunter.Core.Services.GameServices.Instance.TryGet<SoulHunter.Core.Persistence.SaveService>(out var save) ||
                save.CurrentData == null) return;
            var eggs = SoulEggs.For(save.CurrentData, save.CurrentData.SelectedCharacterName);
            if (eggs == null) return;
            RerollsLeft += SoulEggs.Whole(eggs.Reroll);
            SkipsLeft += SoulEggs.Whole(eggs.Skip);
            BanishesLeft += SoulEggs.Whole(eggs.Banish);
        }

        private void Start()
        {
            // Unity Inspector se bachne ke liye Resources folder se load kar rahe hain
            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            _allUpgrades = (catalog != null ? catalog.Upgrades : Resources.LoadAll<UpgradeData>("Upgrades")).Where(u => u != null && u.Level > 0).ToList();
            AddSoulEggCharges();

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

    List<UpgradeData> choices = GetLevelUpChoices(_currentChoiceCount);

    Debug.Log($"[LevelUpManager] <<< GetRandomValidUpgrades returned | count={choices.Count}");

    if (choices.Count == 0)
    {
        // VS rule: once every item is maxed, a level-up still pays out
        // (floor chicken heal + gold) instead of being thrown away. With Limit Break on, cards are offered instead
        // (see GetLevelUpChoices), so this only happens when the player holds no weapon at all.
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
            // VS Limit Break: a Limit Break card strengthens its weapon beyond max level instead of levelling it.
            if (!MaxedRewards.TryApply(upgrade, _playerController) && !LimitBreak.TryApplyCard(upgrade) && _weaponManager != null)
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
            PresentChoicesOrPayOut(GetLevelUpChoices(_currentChoiceCount));
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
            // A Limit Break card is not an item: banishing it only deals new cards and keeps its weapon in the pool.
            if (!LimitBreak.IsCard(upgrade) && !MaxedRewards.IsCard(upgrade))
            {
                _banishedTypes.Add(upgrade.Type);
                Debug.Log($"[LevelUpManager] Banished {upgrade.Type} for this run.");
            }
            PresentChoicesOrPayOut(GetLevelUpChoices(_currentChoiceCount));
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

        /// <summary>
        /// The choices of one level-up: normal upgrades, or, when nothing is left to offer and VS Limit Break mode is on,
        /// Limit Break cards (one per weapon held, picked at random). Chests keep using GetRandomValidUpgrades (VS: Limit
        /// Break does not change treasure chests).
        /// </summary>
        public List<UpgradeData> GetLevelUpChoices(int count)
        {
            var choices = GetRandomValidUpgrades(count);
            if (choices.Count == 0 && SoulHunter.Gameplay.Data.RunModes.LimitBreak)
                choices = LimitBreak.BuildCards(_weaponManager, count, IconFor);
            // VS wiki (Level_up): with nothing left to upgrade, the level-up offers gold or a Floor Chicken to choose from.
            if (choices.Count == 0 && _weaponManager != null)
                choices = MaxedRewards.BuildCards();
            return choices;
        }

        private Sprite IconFor(UpgradeData.UpgradeType type)
        {
            if (_allUpgrades != null)
                foreach (var upgrade in _allUpgrades)
                    if (upgrade != null && upgrade.Type == type && upgrade.Icon != null) return upgrade.Icon;
            return null;
        }

        /// <summary>
        /// The level-up draw (VS wiki, https://vampire.survivors.wiki/w/Level_up): the eligible items, then the two
        /// "owned item" checks, then rarity-weighted picks without repetition. Chests call it without the owned checks.
        /// </summary>
        public List<UpgradeData> GetRandomValidUpgrades(int count) => GetRandomValidUpgrades(count, ownedChecks: true);

        private List<UpgradeData> GetRandomValidUpgrades(int count, bool ownedChecks)
        {
            var validUpgrades = GetValidUpgrades();
            if (validUpgrades.Count == 0) return new List<UpgradeData>();

            // VS "Receiving owned items": only while the player's inventory is not full.
            float ownedChance = ownedChecks && !_weaponManager.IsInventoryFull ? OwnedChance(CurrentLuck(), LevelBeingOffered()) : 0f;
            var weapons = _weaponManager;
            return DrawChoices(validUpgrades, count, type => weapons.GetWeaponLevel(type) > 0, ownedChance, () => Random.value);
        }

        /// <summary>
        /// Every upgrade that can be offered now: the next level of an item (level 1 only while a slot is free), within its
        /// max level, not banished, and one per item (VS: an item cannot be offered more than once per level up).
        /// </summary>
        public List<UpgradeData> GetValidUpgrades()
        {
            List<UpgradeData> validUpgrades = new List<UpgradeData>();
            if (_weaponManager == null || _allUpgrades == null) return validUpgrades;
            var seen = new HashSet<UpgradeData.UpgradeType>();

            foreach (var up in _allUpgrades)
            {
                if (up == null) continue;
                // Logic: Agar Upgrade Level 1 hai, toh player ke paas wo Level 0 par hona chahiye.
                int currentLvl = _weaponManager.GetWeaponLevel(up.Type);
                // New items are only offered while a slot is free; otherwise picking
                // one would be rejected by WeaponManager and waste the level-up.
                if (currentLvl == up.Level - 1 && up.Level <= _weaponManager.GetMaxWeaponLevel(up.Type) &&
                    !(currentLvl == 0 && WeaponManager.IsStagePassive(up.Type)) &&
                    _weaponManager.HasSlotFor(up.Type) && !_banishedTypes.Contains(up.Type) && seen.Add(up.Type))
                {
                    validUpgrades.Add(up);
                }
            }
            return validUpgrades;
        }

        private float CurrentLuck()
        {
            var stats = _playerController != null ? _playerController.Stats : null;
            return stats != null ? stats.Luck : 1f;
        }

        /// <summary>The level this level-up reaches (several queued level-ups are offered in order).</summary>
        private int LevelBeingOffered()
        {
            if (_experience == null) return 1;
            return Mathf.Max(1, _experience.CurrentLevel - Mathf.Max(0, _pendingLevelUps - 1));
        }

        /// <summary>
        /// VS wiki (Level_up, "Receiving owned items"): ownedChance = 1 + 0.3x - 1/totalLuck, where x = 2 if the player's
        /// level is even and x = 1 if it is odd. At Luck 1 that is 30% on odd levels and 60% on even levels.
        /// </summary>
        public static float OwnedChance(float totalLuck, int playerLevel)
        {
            float luck = Mathf.Max(0.0001f, totalLuck);
            int x = playerLevel % 2 == 0 ? 2 : 1;
            return Mathf.Clamp01(1f + 0.3f * x - 1f / luck);
        }

        /// <summary>
        /// The VS level-up draw on an already filtered pool (VS wiki, https://vampire.survivors.wiki/w/Level_up):
        /// 1. "Receiving owned items": the owned-item check is performed twice; each success adds an owned item selected
        ///    at random (uniformly: the wiki says "randomly selected" and gives no weight). If the second check selects the
        ///    same item as the first, it is discarded and a new item is pulled from the pool of all items instead.
        /// 2. "Random selection of items": the remaining options are drawn from the pool at random without repetition,
        ///    each with P = itemRarity / itemPoolWeight (UpgradeRarity.GetWeight).
        /// <paramref name="roll01"/> returns uniform values in [0, 1); an <paramref name="ownedChance"/> of 0 skips step 1.
        /// </summary>
        public static List<UpgradeData> DrawChoices(List<UpgradeData> valid, int count, System.Func<UpgradeData.UpgradeType, bool> isOwned,
            float ownedChance, System.Func<float> roll01)
        {
            var result = new List<UpgradeData>();
            if (valid == null || count <= 0) return result;
            var pool = new List<UpgradeData>(valid);

            if (ownedChance > 0f && isOwned != null)
            {
                var owned = valid.Where(u => isOwned(u.Type)).ToList();
                for (int check = 0; check < 2 && owned.Count > 0 && result.Count < count; check++)
                {
                    if (roll01() >= ownedChance) continue;
                    var pick = owned[Mathf.Clamp(Mathf.FloorToInt(roll01() * owned.Count), 0, owned.Count - 1)];
                    if (result.Contains(pick))
                    {
                        // Same item as the first check: discarded, and a new item comes from the pool of all items.
                        if (pool.Count == 0) continue;
                        int index = PickWeightedIndex(pool, roll01());
                        result.Add(pool[index]);
                        pool.RemoveAt(index);
                    }
                    else
                    {
                        result.Add(pick);
                        pool.Remove(pick);
                    }
                }
            }

            // Rarity-weighted picks without duplicates.
            while (result.Count < count && pool.Count > 0)
            {
                int index = PickWeightedIndex(pool, roll01());
                result.Add(pool[index]);
                pool.RemoveAt(index); // Ensure no duplicates
            }
            return result;
        }

        /// <summary>
        /// Picks an index with probability proportional to each upgrade's rarity weight
        /// (VS wiki Level_up: P(item) = itemRarity / itemPoolWeight).
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

        /// <summary>A chest's item: the rarity-weighted draw without the level-up's owned-item checks.</summary>
        public List<UpgradeData> GetRandomValidUpgradesForChest()
        {
            return GetRandomValidUpgrades(1, ownedChecks: false);
        }
    }
}
