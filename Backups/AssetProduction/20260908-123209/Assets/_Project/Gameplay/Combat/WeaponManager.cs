using UnityEngine;
using System.Collections.Generic;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Player ke paas kaun-kaun se hathiyar hain, aur wo kis level par hain, 
    /// ye sab WeaponManager handle karta hai. Naya upgrade milne par ye us hathiyar ko chalu kar deta hai.
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        [Header("Weapon Objects (Drag from Player's Weapons_Container)")]
        [SerializeField] private GameObject _magicWandObject;
        [SerializeField] private GameObject _garlicWeaponObject;
        [SerializeField] private GameObject _whipWeaponObject;
        [SerializeField] private GameObject _axeWeaponObject;
        [SerializeField] private GameObject _bibleWeaponObject;
        [SerializeField] private GameObject _crossWeaponObject;
        
        [Header("Weapon Evolutions & Unions")]
        [SerializeField] private List<WeaponEvolutionData> _availableEvolutions;
        [SerializeField] private List<WeaponUnionData> _availableUnions;
        
        // Record rakhenge ki kaunsa hathiyar kis level par hai (0 matlab abhi nahi mila)
        private Dictionary<UpgradeData.UpgradeType, int> _weaponLevels = new Dictionary<UpgradeData.UpgradeType, int>();
        private List<UpgradeData.UpgradeType> _evolvedWeapons = new List<UpgradeData.UpgradeType>();

        // Event for UI to update Inventory Icons
        public event System.Action<UpgradeData> OnWeaponAcquiredOrUpgraded;

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
                            // startWeapon = c.StartingWeapon; // Temporarily disabled for testing
                            break;
                        }
                    }
                }
            }

            // Manually activate the starting weapon!
            startWeapon = UpgradeData.UpgradeType.MagicWand; // FORCE MAGIC WAND FOR TESTING
            GiveWeapon(startWeapon, 1);
        }

        public void GiveWeapon(UpgradeData.UpgradeType type, int level)
        {
            _weaponLevels[type] = level;
            switch (type)
            {
                case UpgradeData.UpgradeType.MagicWand: ActivateOrUpgradeWeapon(_magicWandObject, level); break;
                case UpgradeData.UpgradeType.Garlic: ActivateOrUpgradeWeapon(_garlicWeaponObject, level); break;
                case UpgradeData.UpgradeType.Whip: ActivateOrUpgradeWeapon(_whipWeaponObject, level); break;
                case UpgradeData.UpgradeType.Axe: ActivateOrUpgradeWeapon(_axeWeaponObject, level); break;
                case UpgradeData.UpgradeType.Bible: ActivateOrUpgradeWeapon(_bibleWeaponObject, level); break;
                case UpgradeData.UpgradeType.Cross: ActivateOrUpgradeWeapon(_crossWeaponObject, level); break;
            }
        }

        public int GetWeaponLevel(UpgradeData.UpgradeType type)
        {
            return _weaponLevels.ContainsKey(type) ? _weaponLevels[type] : 0;
        }

        public int GetActiveWeaponsCount()
        {
            int count = 0;
            foreach (var kvp in _weaponLevels)
            {
                if (kvp.Value > 0 && kvp.Key != UpgradeData.UpgradeType.PlayerSpeed && kvp.Key != UpgradeData.UpgradeType.MaxHealth)
                {
                    count++;
                }
            }
            return count;
        }

        public void ApplyUpgrade(UpgradeData upgrade)
        {
            Debug.Log($"[WeaponManager] Applying Upgrade: {upgrade.UpgradeName}");

            // Level update karo
            _weaponLevels[upgrade.Type] = upgrade.Level;
            
            // UI ko batao
            OnWeaponAcquiredOrUpgraded?.Invoke(upgrade);

            // VS = SH Logic: Jo weapon mila hai usay ON kar do ya upgrade kar do
            switch (upgrade.Type)
            {
                case UpgradeData.UpgradeType.MagicWand: ActivateOrUpgradeWeapon(_magicWandObject, upgrade.Level); break;
                case UpgradeData.UpgradeType.Garlic: ActivateOrUpgradeWeapon(_garlicWeaponObject, upgrade.Level); break;
                case UpgradeData.UpgradeType.Whip: ActivateOrUpgradeWeapon(_whipWeaponObject, upgrade.Level); break;
                case UpgradeData.UpgradeType.Axe: ActivateOrUpgradeWeapon(_axeWeaponObject, upgrade.Level); break;
                case UpgradeData.UpgradeType.Bible: ActivateOrUpgradeWeapon(_bibleWeaponObject, upgrade.Level); break;
                case UpgradeData.UpgradeType.Cross: ActivateOrUpgradeWeapon(_crossWeaponObject, upgrade.Level); break;
                
                // If it's not one of the pre-linked weapon gameobjects, treat it as a passive/stat upgrade
                default: 
                    ApplyPassiveStat(upgrade); 
                    break;
            }
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
            }

            Debug.Log($"[WeaponManager] Passive Applied: {upgrade.Type} is now Level {upgrade.Level}");
        }

        private Dictionary<GameObject, GameObject> _spawnedWeapons = new Dictionary<GameObject, GameObject>();

        private void ActivateOrUpgradeWeapon(GameObject weaponPrefab, int currentLevel)
        {
            if (weaponPrefab == null) return;

            // Naya Logic: Agar weapon pehle se spawn nahi hua, tou usey Player ke andar paida karo
            if (!_spawnedWeapons.ContainsKey(weaponPrefab))
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    GameObject newWeapon = Instantiate(weaponPrefab, player.transform);
                    newWeapon.SetActive(true);
                    _spawnedWeapons[weaponPrefab] = newWeapon;
                    Debug.Log($"[WeaponManager] {weaponPrefab.name} is now SPAWNED and ACTIVE on Player!");
                }
            }
            else
            {
                // Agar pehle se paida ho chuka hai, toh uski power badhani hogi.
                Debug.Log($"[WeaponManager] {weaponPrefab.name} Upgraded to Level {currentLevel}!");
                // (Future mein yahan weapon scripts fetch karke unka damage/size badhayenge)
            }
        }

        public bool TryEvolveWeapon(out string evolvedWeaponName)
        {
            evolvedWeaponName = "";
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Transform spawnTarget = player != null ? player.transform : transform;
            
            // 1. Check Unions first
            if (_availableUnions != null)
            {
                foreach (var union in _availableUnions)
                {
                    if (union == null || _evolvedWeapons.Contains(union.WeaponA)) continue;

                    int lvlA = GetWeaponLevel(union.WeaponA);
                    int lvlB = GetWeaponLevel(union.WeaponB);

                    if (lvlA >= union.WeaponAMaxLevel && lvlB >= union.WeaponBMaxLevel)
                    {
                        // Union criteria met!
                        _evolvedWeapons.Add(union.WeaponA); // Mark as evolved so we don't do it again
                        
                        DisableOldWeapon(union.WeaponA);
                        DisableOldWeapon(union.WeaponB);
                        
                        if (union.UnionWeaponPrefab != null)
                        {
                            var newWep = Instantiate(union.UnionWeaponPrefab, spawnTarget);
                            newWep.SetActive(true);
                        }

                        evolvedWeaponName = union.UnionName;
                        Debug.Log($"[WeaponManager] UNION SUCCESS! {union.WeaponA} + {union.WeaponB} = {union.UnionName}!");
                        return true;
                    }
                }
            }

            // 2. Check Evolutions
            if (_availableEvolutions != null)
            {
                foreach (var evo in _availableEvolutions)
                {
                    if (evo == null || _evolvedWeapons.Contains(evo.BaseWeapon)) continue;

                    int baseLvl = GetWeaponLevel(evo.BaseWeapon);
                    int passiveLvl = GetWeaponLevel(evo.RequiredPassive);

                    if (baseLvl >= evo.BaseWeaponMaxLevel && passiveLvl > 0)
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
                        }

                        evolvedWeaponName = evo.EvolvedName;
                        Debug.Log($"[WeaponManager] EVOLUTION SUCCESS! {evo.BaseWeapon} evolved into {evo.EvolvedName}!");
                        return true;
                    }
                }
            }

            return false;
        }

        private void DisableOldWeapon(UpgradeData.UpgradeType type)
        {
            GameObject prefabToDisable = null;

            switch (type)
            {
                case UpgradeData.UpgradeType.MagicWand: prefabToDisable = _magicWandObject; break;
                case UpgradeData.UpgradeType.Garlic: prefabToDisable = _garlicWeaponObject; break;
                case UpgradeData.UpgradeType.Whip: prefabToDisable = _whipWeaponObject; break;
                case UpgradeData.UpgradeType.Axe: prefabToDisable = _axeWeaponObject; break;
                case UpgradeData.UpgradeType.Bible: prefabToDisable = _bibleWeaponObject; break;
                case UpgradeData.UpgradeType.Cross: prefabToDisable = _crossWeaponObject; break;
            }

            if (prefabToDisable != null && _spawnedWeapons.ContainsKey(prefabToDisable))
            {
                _spawnedWeapons[prefabToDisable].SetActive(false);
            }
        }
    }
}
