using UnityEngine;
using System.Collections.Generic;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Combat
{
    [System.Serializable]
    public sealed class WeaponBinding
    {
        public UpgradeData.UpgradeType Type;
        public GameObject WeaponPrefab;
    }

    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Player ke paas kaun-kaun se hathiyar hain, aur wo kis level par hain, 
    /// ye sab WeaponManager handle karta hai. Naya upgrade milne par ye us hathiyar ko chalu kar deta hai.
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        // Learning Comment:
        // Deterministic singleton instance access: Other systems (e.g. AchievementManager)
        // can reference WeaponManager without relying on runtime scene searches.
        public static WeaponManager Instance { get; private set; }

        [Header("Weapon Objects (Drag from Player's Weapons_Container)")]
        [SerializeField] private GameObject _magicWandObject;
        [SerializeField] private GameObject _garlicWeaponObject;
        [SerializeField] private GameObject _whipWeaponObject;
        [SerializeField] private GameObject _axeWeaponObject;
        [SerializeField] private GameObject _bibleWeaponObject;
        [SerializeField] private GameObject _crossWeaponObject;
        [SerializeField] private List<WeaponBinding> _additionalWeaponBindings = new List<WeaponBinding>();
        [SerializeField] private int _maxWeaponSlots = 6;
        [SerializeField] private int _maxPassiveSlots = 6;
        
        [Header("Weapon Evolutions & Unions")]
        [SerializeField] private List<WeaponEvolutionData> _availableEvolutions;
        [SerializeField] private List<WeaponUnionData> _availableUnions;
        
        // Record rakhenge ki kaunsa hathiyar kis level par hai (0 matlab abhi nahi mila)
        private Dictionary<UpgradeData.UpgradeType, int> _weaponLevels = new Dictionary<UpgradeData.UpgradeType, int>();
        private List<UpgradeData.UpgradeType> _evolvedWeapons = new List<UpgradeData.UpgradeType>();
        private HashSet<UpgradeData.UpgradeType> _unionConsumedWeapons = new HashSet<UpgradeData.UpgradeType>();
        private int _activeUnionWeaponCount;

        // Event for UI to update Inventory Icons
        public event System.Action<UpgradeData> OnWeaponAcquiredOrUpgraded;

        private void Awake()
        {
            // Learning Comment:
            // Singleton lifecycle assignment: Runtime dependencies ke liye static Instance assign kiya jata hai
            // taake systems scene search kiye baghair deterministically bind ho sakein.
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            // Learning Comment:
            // Lifecycle cleanup: Scene unload ya object destruction par static Instance ko null reset karte hain.
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            // By default sab off kar do
            _weaponLevels[UpgradeData.UpgradeType.Whip] = 0;
            _weaponLevels[UpgradeData.UpgradeType.Garlic] = 0;
            _weaponLevels[UpgradeData.UpgradeType.Axe] = 0;
            _weaponLevels[UpgradeData.UpgradeType.Bible] = 0;
            _weaponLevels[UpgradeData.UpgradeType.Cross] = 0;
            _weaponLevels[UpgradeData.UpgradeType.MagicWand] = 0;
            _weaponLevels[UpgradeData.UpgradeType.PlayerSpeed] = 0;
            _weaponLevels[UpgradeData.UpgradeType.MaxHealth] = 0;

            UpgradeData.UpgradeType startWeapon = UpgradeData.UpgradeType.MagicWand; // Default

            // Try to load selected character from SaveService
            if (SoulHunter.Core.Services.GameServices.Instance != null)
            {
                var saveSvc = SoulHunter.Core.Services.GameServices.Instance.Get<SoulHunter.Core.Persistence.SaveService>();
                if (saveSvc != null && !string.IsNullOrEmpty(saveSvc.CurrentData.SelectedCharacterName))
                {
                    string heroName = saveSvc.CurrentData.SelectedCharacterName;
                    var chars = Resources.LoadAll<CharacterData>("Characters");
                    foreach (var c in chars)
                    {
                        if (c.CharacterName == heroName)
                        {
                            startWeapon = c.StartingWeapon;
                            break;
                        }
                    }
                }
            }

            // Character selection is authoritative. If no character data is
            // saved, the Soul Hunter default remains Magic Wand.
            GiveWeapon(startWeapon, 1);
        }

        public void GiveWeapon(UpgradeData.UpgradeType type, int level)
        {
            level = Mathf.Clamp(level, 0, GetMaxWeaponLevel(type));
            if (level > 0 && GetWeaponLevel(type) == 0 && IsWeaponType(type) && GetActiveWeaponsCount() >= _maxWeaponSlots)
            {
                Debug.Log($"[WeaponManager] Weapon slots full; {type} was not acquired.");
                return;
            }
            _weaponLevels[type] = level;
            if (IsWeaponType(type)) ActivateOrUpgradeWeapon(GetWeaponObject(type), level);
        }

        public int GetWeaponLevel(UpgradeData.UpgradeType type)
        {
            return _weaponLevels.ContainsKey(type) ? _weaponLevels[type] : 0;
        }

        public int GetMaxWeaponLevel(UpgradeData.UpgradeType type)
        {
            if (IsStagePassive(type)) return 9;
            var prefab = GetWeaponObject(type);
            var weapon = prefab != null ? prefab.GetComponent<AutoAttackWeapon>() : null;
            return weapon != null ? weapon.MaxLevel : int.MaxValue;
        }

        public int GetActiveWeaponsCount()
        {
            int count = _activeUnionWeaponCount;
            foreach (var kvp in _weaponLevels)
            {
                if (kvp.Value > 0 && IsWeaponType(kvp.Key) && !_unionConsumedWeapons.Contains(kvp.Key))
                {
                    count++;
                }
            }
            return count;
        }

        public void ApplyUpgrade(UpgradeData upgrade)
        {
            if (upgrade == null || upgrade.Level > GetMaxWeaponLevel(upgrade.Type)) return;
            if (IsStagePassive(upgrade.Type) && (GetWeaponLevel(upgrade.Type) == 0 || upgrade.Level != GetWeaponLevel(upgrade.Type) + 1)) return;
            Debug.Log($"[WeaponManager] Applying Upgrade: {upgrade.UpgradeName}");

            bool newWeapon = IsWeaponType(upgrade.Type) && GetWeaponLevel(upgrade.Type) == 0;
            if (newWeapon && GetActiveWeaponsCount() >= _maxWeaponSlots)
            {
                Debug.Log($"[WeaponManager] Weapon slots full; rejected {upgrade.UpgradeName}.");
                return;
            }
            bool newPassive = IsPassiveType(upgrade.Type) && GetWeaponLevel(upgrade.Type) == 0;
            if (newPassive && GetPassiveCount() >= _maxPassiveSlots)
            {
                Debug.Log($"[WeaponManager] Passive slots full; rejected {upgrade.UpgradeName}.");
                return;
            }

            // Level update karo
            _weaponLevels[upgrade.Type] = upgrade.Level;
            
            // UI ko batao
            OnWeaponAcquiredOrUpgraded?.Invoke(upgrade);

            // VS = SH Logic: Jo weapon mila hai usay ON kar do ya upgrade kar do
            if (IsWeaponType(upgrade.Type)) ActivateOrUpgradeWeapon(GetWeaponObject(upgrade.Type), upgrade.Level);
            else ApplyPassiveStat(upgrade);
        }

        private void ApplyPassiveStat(UpgradeData upgrade)
        {
            var stats = GetComponentInParent<SoulHunter.Gameplay.Player.PlayerStats>();
            var health = GetComponentInParent<HealthController>();
            if (stats == null) return;

            switch (upgrade.Type)
            {
                case UpgradeData.UpgradeType.PlayerSpeed: stats.AddMoveSpeed(0.1f); break; // +10% Speed
                case UpgradeData.UpgradeType.MaxHealth: if(health != null) health.IncreaseMaxHealth(20); break;
                
                // Base & Expansions
                case UpgradeData.UpgradeType.EmptyTome: stats.ReduceCooldown(0.08f); break; // -8% Cooldown
                case UpgradeData.UpgradeType.Spinach: stats.AddMight(0.1f); break; // +10% Damage
                case UpgradeData.UpgradeType.Bracer: stats.AddProjectileSpeed(0.1f); break; // +10% Proj Speed
                
                // Expansion 4 Passives
                case UpgradeData.UpgradeType.Candelabrador: stats.AddArea(0.1f); break; // +10% Area
                case UpgradeData.UpgradeType.Spellbinder: stats.AddDuration(0.1f); break; // +10% Duration
                case UpgradeData.UpgradeType.Duplicator: stats.AddAmount(1); break; // +1 Projectile
                case UpgradeData.UpgradeType.Armor: stats.AddArmor(1); break; // +1 Defense
                case UpgradeData.UpgradeType.Pummarola: stats.AddRegen(0.2f); break; // +0.2 HP/sec
                case UpgradeData.UpgradeType.Attractorb: stats.AddMagnet(0.25f); break; // +25% Pickup range
                case UpgradeData.UpgradeType.Clover: stats.AddLuck(0.1f); break; // +10% Luck
                case UpgradeData.UpgradeType.Crown: stats.AddExpBonus(0.08f); break; // +8% Exp
                case UpgradeData.UpgradeType.SkullOManiac: stats.AddCurse(0.1f); break; // +10% Curse
                case UpgradeData.UpgradeType.Tiragisu: stats.AddRevival(1); break; // +1 Revival
                case UpgradeData.UpgradeType.MetaglioLeft:
                    stats.AddRegen(0.1f);
                    if (health != null) health.MultiplyMaxHealth(1.05f);
                    break;
                case UpgradeData.UpgradeType.MetaglioRight: stats.AddCurse(0.05f); break;
            }

            Debug.Log($"[WeaponManager] Passive Applied: {upgrade.Type} is now Level {upgrade.Level}");
        }

        private Dictionary<GameObject, GameObject> _spawnedWeapons = new Dictionary<GameObject, GameObject>();

        public List<UpgradeData.UpgradeType> GetActiveWeaponTypes()
        {
            List<UpgradeData.UpgradeType> activeTypes = new List<UpgradeData.UpgradeType>();
            foreach (var kvp in _weaponLevels)
            {
                if (kvp.Value > 0 && IsWeaponType(kvp.Key) && !_unionConsumedWeapons.Contains(kvp.Key))
                {
                    activeTypes.Add(kvp.Key);
                }
            }
            return activeTypes;
        }

        public GameObject GetWeaponObject(UpgradeData.UpgradeType type)
        {
            switch (type)
            {
                case UpgradeData.UpgradeType.MagicWand: return _magicWandObject;
                case UpgradeData.UpgradeType.Garlic: return _garlicWeaponObject;
                case UpgradeData.UpgradeType.Whip: return _whipWeaponObject;
                case UpgradeData.UpgradeType.Axe: return _axeWeaponObject;
                case UpgradeData.UpgradeType.Bible: return _bibleWeaponObject;
                case UpgradeData.UpgradeType.Cross: return _crossWeaponObject;
            }
            if (_additionalWeaponBindings != null)
                foreach (var binding in _additionalWeaponBindings)
                    if (binding != null && binding.Type == type) return binding.WeaponPrefab;
            return null;
        }

        /// <summary>
        /// True when an item the player does not own yet still fits in a free
        /// weapon or passive slot. Owned items can always be levelled.
        /// </summary>
        public bool HasSlotFor(UpgradeData.UpgradeType type)
        {
            if (GetWeaponLevel(type) > 0) return true;
            return IsWeaponType(type)
                ? GetActiveWeaponsCount() < _maxWeaponSlots
                : GetPassiveCount() < _maxPassiveSlots;
        }

        public int GetPassiveCount()
        {
            int count = 0;
            foreach (var pair in _weaponLevels)
                if (pair.Value > 0 && IsPassiveType(pair.Key)) count++;
            return count;
        }

        public static bool IsStagePassive(UpgradeData.UpgradeType type) =>
            type == UpgradeData.UpgradeType.MetaglioLeft || type == UpgradeData.UpgradeType.MetaglioRight;

        public bool AcquireStagePassive(UpgradeData upgrade)
        {
            if (upgrade == null || !IsStagePassive(upgrade.Type) || upgrade.Level != 1 || GetWeaponLevel(upgrade.Type) != 0) return false;
            // Ground acquisition is allowed beyond the six normal passive slots.
            _weaponLevels[upgrade.Type] = 1;
            OnWeaponAcquiredOrUpgraded?.Invoke(upgrade);
            return true;
        }

        private static bool IsWeaponType(UpgradeData.UpgradeType type)
        {
            return !IsStagePassive(type) && type != UpgradeData.UpgradeType.PlayerSpeed && type != UpgradeData.UpgradeType.MaxHealth &&
                type != UpgradeData.UpgradeType.EmptyTome && type != UpgradeData.UpgradeType.Spinach && type != UpgradeData.UpgradeType.Bracer &&
                type != UpgradeData.UpgradeType.Candelabrador && type != UpgradeData.UpgradeType.Spellbinder && type != UpgradeData.UpgradeType.Duplicator &&
                type != UpgradeData.UpgradeType.Armor && type != UpgradeData.UpgradeType.Pummarola && type != UpgradeData.UpgradeType.Attractorb &&
                type != UpgradeData.UpgradeType.Clover && type != UpgradeData.UpgradeType.Crown && type != UpgradeData.UpgradeType.SkullOManiac && type != UpgradeData.UpgradeType.Tiragisu;
        }

        private static bool IsPassiveType(UpgradeData.UpgradeType type) => !IsWeaponType(type);

        private void ActivateOrUpgradeWeapon(GameObject weaponPrefab, int currentLevel)
        {
            if (weaponPrefab == null) return;

            // Naya Logic: Agar weapon pehle se spawn nahi hua, tou usey Player ke andar paida karo
            if (!_spawnedWeapons.ContainsKey(weaponPrefab))
            {
                var player = FindFirstObjectByType<SoulHunter.Gameplay.Player.PlayerController>();
                if (player != null)
                {
                    GameObject newWeapon = Instantiate(weaponPrefab, player.transform);
                    newWeapon.SetActive(true);
                    _spawnedWeapons[weaponPrefab] = newWeapon;
                    ApplyWeaponLevel(newWeapon, currentLevel);
                    Debug.Log($"[WeaponManager] {weaponPrefab.name} is now SPAWNED and ACTIVE on Player!");
                }
            }
            else
            {
                // Agar pehle se paida ho chuka hai, toh uski power badhani hogi.
                Debug.Log($"[WeaponManager] {weaponPrefab.name} Upgraded to Level {currentLevel}!");
                ApplyWeaponLevel(_spawnedWeapons[weaponPrefab], currentLevel);
            }
        }

        private void ApplyWeaponLevel(GameObject weaponObject, int targetLevel)
        {
            if (weaponObject == null) return;

            var weapon = weaponObject.GetComponent<AutoAttackWeapon>();
            if (weapon == null) return;

            int safeTargetLevel = Mathf.Clamp(targetLevel, 1, weapon.MaxLevel);
            while (weapon.CurrentLevel < safeTargetLevel)
            {
                int previousLevel = weapon.CurrentLevel;
                weapon.LevelUp();
                // Evolved weapons may intentionally lock their level and
                // override LevelUp as a no-op. Never spin forever on those.
                if (weapon.CurrentLevel <= previousLevel)
                {
                    weapon.CurrentLevel = safeTargetLevel;
                    break;
                }
            }
        }

        public bool TryEvolveWeapon(out string evolvedWeaponName)
        {
            evolvedWeaponName = "";
            var player = FindFirstObjectByType<SoulHunter.Gameplay.Player.PlayerController>();
            Transform spawnTarget = player != null ? player.transform : transform;
            
            // 1. Check Evolutions first. A pending evolution must not lose its
            // base weapon to a union that happens to be checked earlier.
            if (_availableEvolutions != null)
            {
                foreach (var evo in _availableEvolutions)
                {
                    if (evo == null || evo.EvolvedWeaponPrefab == null || _evolvedWeapons.Contains(evo.BaseWeapon) ||
                        _unionConsumedWeapons.Contains(evo.BaseWeapon)) continue;

                    if (evo.RequirementsMet(this))
                    {
                        // Evolution criteria met!
                        _evolvedWeapons.Add(evo.BaseWeapon);
                        
                        // Old weapon disable karo
                        DisableOldWeapon(evo.BaseWeapon);
                        
                        // Naya Evolved weapon Instantiate ya Enable karo
                        if (evo.EvolvedWeaponPrefab != null)
                        {
                            var newWep = Instantiate(evo.EvolvedWeaponPrefab, spawnTarget);
                            newWep.SetActive(true);
                            ApplyWeaponLevel(newWep, evo.BaseWeaponMaxLevel);
                        }

                        evolvedWeaponName = evo.EvolvedName;
                        Debug.Log($"[WeaponManager] EVOLUTION SUCCESS! {evo.BaseWeapon} evolved into {evo.EvolvedName}!");
                        return true;
                    }
                }
            }

            // 2. Check Unions
            if (_availableUnions != null)
            {
                foreach (var union in _availableUnions)
                {
                    if (union == null || union.UnionWeaponPrefab == null ||
                        _evolvedWeapons.Contains(union.WeaponA) || _evolvedWeapons.Contains(union.WeaponB) ||
                        _unionConsumedWeapons.Contains(union.WeaponA) || _unionConsumedWeapons.Contains(union.WeaponB)) continue;

                    int lvlA = GetWeaponLevel(union.WeaponA);
                    int lvlB = GetWeaponLevel(union.WeaponB);

                    if (lvlA >= union.WeaponAMaxLevel && lvlB >= union.WeaponBMaxLevel)
                    {
                        // Union criteria met!
                        _evolvedWeapons.Add(union.WeaponA); // Mark as evolved so we don't do it again
                        _unionConsumedWeapons.Add(union.WeaponA);
                        _unionConsumedWeapons.Add(union.WeaponB);
                        _activeUnionWeaponCount++;
                        
                        DisableOldWeapon(union.WeaponA);
                        DisableOldWeapon(union.WeaponB);
                        
                        if (union.UnionWeaponPrefab != null)
                        {
                            var newWep = Instantiate(union.UnionWeaponPrefab, spawnTarget);
                            newWep.SetActive(true);
                            ApplyWeaponLevel(newWep, Mathf.Max(union.WeaponAMaxLevel, union.WeaponBMaxLevel));
                        }

                        evolvedWeaponName = union.UnionName;
                        Debug.Log($"[WeaponManager] UNION SUCCESS! {union.WeaponA} + {union.WeaponB} = {union.UnionName}!");
                        return true;
                    }
                }
            }

            return false;
        }

        private void DisableOldWeapon(UpgradeData.UpgradeType type)
        {
            GameObject prefabToDisable = GetWeaponObject(type);

            if (prefabToDisable != null && _spawnedWeapons.ContainsKey(prefabToDisable))
            {
                _spawnedWeapons[prefabToDisable].SetActive(false);
            }
        }
    }
}
