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
        /// <summary>An item left the inventory (Tirajisú after the last Revival).</summary>
        public event System.Action<UpgradeData.UpgradeType> OnItemRemoved;
        /// <summary>Anything in the inventory changed: gained, levelled, evolved, united or removed. The HUD redraws on it.</summary>
        public event System.Action OnInventoryChanged;

        // Items in the order they were gained (the HUD fills its slots in this order, like VS).
        private readonly List<UpgradeData.UpgradeType> _acquireOrder = new List<UpgradeData.UpgradeType>();
        // A union takes the slot of its first weapon and shows the union's icon.
        private readonly Dictionary<UpgradeData.UpgradeType, Sprite> _unionIconBySlot = new Dictionary<UpgradeData.UpgradeType, Sprite>();

        /// <summary>Icon of the last evolution (its base weapon's) or union (its own), for the chest screen.</summary>
        public Sprite LastEvolvedIcon { get; private set; }

        /// <summary>The asset name of the last evolution or union made (for example "Evo_BloodyTear"); the save's discovery key.</summary>
        public string LastEvolutionId { get; private set; }

        // Evolutions and unions made in this run (by any path), by asset name.
        private readonly HashSet<string> _madeThisRun = new HashSet<string>();

        /// <summary>True when the evolution or union <paramref name="id"/> was made in this run (VS Grimoire: its row is fully highlighted).</summary>
        public bool MadeThisRun(string id) => !string.IsNullOrEmpty(id) && _madeThisRun.Contains(id);

        /// <summary>Every evolution recipe this run knows (the scene's list plus GameContent's), in that order.</summary>
        public IReadOnlyList<WeaponEvolutionData> Evolutions => (IReadOnlyList<WeaponEvolutionData>)_availableEvolutions ?? System.Array.Empty<WeaponEvolutionData>();

        /// <summary>Every union recipe this run knows (none are live since the invented ones were removed, 2026-09-30).</summary>
        public IReadOnlyList<WeaponUnionData> Unions => (IReadOnlyList<WeaponUnionData>)_availableUnions ?? System.Array.Empty<WeaponUnionData>();

        /// <summary>One HUD inventory slot.</summary>
        public struct InventorySlot
        {
            public UpgradeData.UpgradeType Type;
            public Sprite Icon;
            public int Level;
            /// <summary>An evolved weapon (it keeps its base weapon's slot) or a union.</summary>
            public bool Evolved;
        }

        private void Awake()
        {
            // Learning Comment:
            // Singleton lifecycle assignment: Runtime dependencies ke liye static Instance assign kiya jata hai
            // taake systems scene search kiye baghair deterministically bind ho sakein.
            if (Instance == null)
            {
                Instance = this;
            }
            // VS evolutions built since 2026-10-01 live in GameContent (EvolutionBuilder), so the scene is not edited.
            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            if (catalog != null && catalog.Evolutions != null)
            {
                _availableEvolutions ??= new List<WeaponEvolutionData>();
                foreach (var evolution in catalog.Evolutions)
                    if (evolution != null && !_availableEvolutions.Contains(evolution)) _availableEvolutions.Add(evolution);
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
                    // Characters live in the content catalog; there is no Resources/Characters folder.
                    var character = GameContentCatalog.FindCharacterExact(saveSvc.CurrentData.SelectedCharacterName);
                    if (character != null) startWeapon = character.StartingWeapon;
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
            TrackAcquired(type, level);
            _weaponLevels[type] = level;
            if (IsWeaponType(type)) ActivateOrUpgradeWeapon(GetWeaponObject(type), level, type);
            OnInventoryChanged?.Invoke();
        }

        private void TrackAcquired(UpgradeData.UpgradeType type, int newLevel)
        {
            if (newLevel > 0 && !_acquireOrder.Contains(type)) _acquireOrder.Add(type);
            else if (newLevel <= 0) _acquireOrder.Remove(type);
        }

        /// <summary>
        /// Weapons (<paramref name="passives"/> false) or passives in the order gained, as the HUD shows them. Weapons
        /// merged into a union give one slot, where the union's first weapon was.
        /// </summary>
        public List<InventorySlot> GetInventory(bool passives)
        {
            var slots = new List<InventorySlot>();
            foreach (var type in _acquireOrder)
            {
                int level = GetWeaponLevel(type);
                if (level <= 0 || IsWeaponType(type) == passives) continue;
                if (_unionIconBySlot.TryGetValue(type, out var unionIcon))
                {
                    slots.Add(new InventorySlot { Type = type, Icon = unionIcon, Level = level, Evolved = true });
                    continue;
                }
                if (_unionConsumedWeapons.Contains(type)) continue;
                var definition = DefinitionOf(type);
                slots.Add(new InventorySlot
                {
                    Type = type, Icon = definition != null ? definition.Icon : null, Level = level,
                    Evolved = !passives && _evolvedWeapons.Contains(type)
                });
            }
            return slots;
        }

        public int GetWeaponLevel(UpgradeData.UpgradeType type)
        {
            return _weaponLevels.ContainsKey(type) ? _weaponLevels[type] : 0;
        }

        /// <summary>
        /// Which WeaponDefinition this manager uses for a type. Defaults to GameContentCatalog.WeaponDefinitions; tests that
        /// wire their own prefabs into the old fields set it to <c>_ =&gt; null</c>.
        /// </summary>
        public System.Func<UpgradeData.UpgradeType, WeaponDefinition> DefinitionLookup { get; set; } = WeaponDefinitionLookup.Find;

        private WeaponDefinition DefinitionOf(UpgradeData.UpgradeType type) => DefinitionLookup != null ? DefinitionLookup(type) : null;

        public int GetMaxWeaponLevel(UpgradeData.UpgradeType type)
        {
            // The item's definition first; without one, the older rules (stage passives 9, else the prefab's MaxLevel).
            var definition = DefinitionOf(type);
            if (definition != null && definition.MaxLevel > 0) return definition.MaxLevel;
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
            TrackAcquired(upgrade.Type, upgrade.Level);
            _weaponLevels[upgrade.Type] = upgrade.Level;
            
            // UI ko batao
            OnWeaponAcquiredOrUpgraded?.Invoke(upgrade);

            // VS = SH Logic: Jo weapon mila hai usay ON kar do ya upgrade kar do
            if (IsWeaponType(upgrade.Type)) ActivateOrUpgradeWeapon(GetWeaponObject(upgrade.Type), upgrade.Level, upgrade.Type);
            else ApplyPassiveStat(upgrade);
            OnInventoryChanged?.Invoke();
        }

        private void ApplyPassiveStat(UpgradeData upgrade)
        {
            // Main_Gameplay keeps Weapon_Manager under MANAGERS, not under the player, so a parent lookup alone found no
            // PlayerStats and every passive did nothing in the real game (found 2026-09-28 by TirajisuPlayModeTests).
            var stats = GetComponentInParent<SoulHunter.Gameplay.Player.PlayerStats>();
            var health = GetComponentInParent<HealthController>();
            var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
            if (stats == null && player != null) stats = player.GetComponent<SoulHunter.Gameplay.Player.PlayerStats>();
            if (health == null && player != null) health = player.GetComponent<HealthController>();
            if (stats == null) return;

            switch (upgrade.Type)
            {
                case UpgradeData.UpgradeType.PlayerSpeed: stats.AddMoveSpeed(0.1f); break; // +10% Speed
                // VS wiki (Hollow_Heart): "multiplies Max Health by 1.2 per level" and "does not heal to fill up the newly
                // increased Max Health". (It was +20 flat, with a heal.)
                case UpgradeData.UpgradeType.MaxHealth: if (health != null) health.MultiplyMaxHealth(1.2f, healByIncrease: false); break;
                
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
                // VS wiki (Attractorb): multiplicative steps, 1.5x at level 1, then +33%, +25%, +20%, +33% (3.98x at 5).
                case UpgradeData.UpgradeType.Attractorb: stats.MultiplyMagnet(AttractorbStep(upgrade.Level)); break;
                case UpgradeData.UpgradeType.Clover: stats.AddLuck(0.1f); break; // +10% Luck
                case UpgradeData.UpgradeType.Crown: stats.AddExpBonus(0.08f); break; // +8% Exp
                case UpgradeData.UpgradeType.SkullOManiac: stats.AddCurse(0.1f); break; // +10% Curse
                case UpgradeData.UpgradeType.Tiragisu: stats.AddRevival(1); break; // +1 Revival
                // VS wiki (Metaglio_Left / Metaglio_Right): level 1 gives nothing; levels 2-9 add 0.1 Recovery and x1.05
                // Max Health (Left) or +5% Curse (Right).
                case UpgradeData.UpgradeType.MetaglioLeft:
                    if (upgrade.Level <= 1) break;
                    stats.AddRegen(0.1f);
                    if (health != null) health.MultiplyMaxHealth(1.05f);
                    break;
                case UpgradeData.UpgradeType.MetaglioRight: if (upgrade.Level > 1) stats.AddCurse(0.05f); break;
            }

            Debug.Log($"[WeaponManager] Passive Applied: {upgrade.Type} is now Level {upgrade.Level}");
        }

        /// <summary>
        /// VS wiki (Tirajisú): "Tirajisú will only be removed from the inventory when the last Revive is used up", from any
        /// Revival source. Its slot frees up and it can be offered again from level 1. Returns true when it was removed.
        /// </summary>
        public bool ReleaseSpentTirajisu(int revivalsLeft)
        {
            if (revivalsLeft > 0 || GetWeaponLevel(UpgradeData.UpgradeType.Tiragisu) == 0) return false;
            _weaponLevels[UpgradeData.UpgradeType.Tiragisu] = 0;
            TrackAcquired(UpgradeData.UpgradeType.Tiragisu, 0);
            OnItemRemoved?.Invoke(UpgradeData.UpgradeType.Tiragisu);
            OnInventoryChanged?.Invoke();
            Debug.Log("[WeaponManager] Last Revival used: Tirajisú left the inventory.");
            return true;
        }

        /// <summary>VS wiki (Attractorb) magnet multiplier gained at <paramref name="level"/>: 1.5, 1.33, 1.25, 1.2, 1.33.</summary>
        public static float AttractorbStep(int level)
        {
            switch (level)
            {
                case 1: return 1.5f;
                case 2: return 1.33f;
                case 3: return 1.25f;
                case 4: return 1.2f;
                case 5: return 1.33f;
                default: return 1f;
            }
        }

        private Dictionary<GameObject, GameObject> _spawnedWeapons = new Dictionary<GameObject, GameObject>();
        // Prefab -> live instance of every weapon spawned on the player (base, evolved and union), in the order gained.
        private readonly List<KeyValuePair<GameObject, GameObject>> _equippedWeapons = new List<KeyValuePair<GameObject, GameObject>>();

        /// <summary>
        /// The weapons the player is really using: each prefab with its live instance, skipping base weapons that an
        /// evolution or union replaced. Shadow Kael mirrors this list.
        /// </summary>
        public List<KeyValuePair<GameObject, AutoAttackWeapon>> GetEquippedWeapons()
        {
            var equipped = new List<KeyValuePair<GameObject, AutoAttackWeapon>>();
            foreach (var pair in _equippedWeapons)
            {
                if (pair.Key == null || pair.Value == null || !pair.Value.activeSelf) continue;
                var weapon = pair.Value.GetComponent<AutoAttackWeapon>();
                if (weapon != null) equipped.Add(new KeyValuePair<GameObject, AutoAttackWeapon>(pair.Key, weapon));
            }
            return equipped;
        }

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
            // The item's definition first; the scene fields and bindings below are the fallback for items without one.
            var definition = DefinitionOf(type);
            if (definition != null && definition.WeaponPrefab != null) return definition.WeaponPrefab;
            return LegacyWeaponObject(type);
        }

        /// <summary>The weapon prefab from this manager's own serialized fields and bindings (pre-definition wiring).</summary>
        public GameObject LegacyWeaponObject(UpgradeData.UpgradeType type)
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

        /// <summary>True when every weapon slot and every passive slot is taken (VS: "the player's inventory is full").</summary>
        public bool IsInventoryFull => GetActiveWeaponsCount() >= _maxWeaponSlots && GetPassiveCount() >= _maxPassiveSlots;

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
            TrackAcquired(upgrade.Type, 1);
            _weaponLevels[upgrade.Type] = 1;
            OnWeaponAcquiredOrUpgraded?.Invoke(upgrade);
            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>True for weapons, false for passives: the item's WeaponDefinition.IsPassive, else the built-in list.</summary>
        public static bool IsWeaponType(UpgradeData.UpgradeType type)
        {
            var definition = WeaponDefinitionLookup.Find(type);
            return definition != null ? !definition.IsPassive : LegacyIsWeaponType(type);
        }

        /// <summary>The built-in weapon/passive split used before WeaponDefinitions (every type not listed is a weapon).</summary>
        public static bool LegacyIsWeaponType(UpgradeData.UpgradeType type)
        {
            return !IsStagePassive(type) && type != UpgradeData.UpgradeType.PlayerSpeed && type != UpgradeData.UpgradeType.MaxHealth &&
                type != UpgradeData.UpgradeType.EmptyTome && type != UpgradeData.UpgradeType.Spinach && type != UpgradeData.UpgradeType.Bracer &&
                type != UpgradeData.UpgradeType.Candelabrador && type != UpgradeData.UpgradeType.Spellbinder && type != UpgradeData.UpgradeType.Duplicator &&
                type != UpgradeData.UpgradeType.Armor && type != UpgradeData.UpgradeType.Pummarola && type != UpgradeData.UpgradeType.Attractorb &&
                type != UpgradeData.UpgradeType.Clover && type != UpgradeData.UpgradeType.Crown && type != UpgradeData.UpgradeType.SkullOManiac && type != UpgradeData.UpgradeType.Tiragisu;
        }

        private static bool IsPassiveType(UpgradeData.UpgradeType type) => !IsWeaponType(type);

        private void ActivateOrUpgradeWeapon(GameObject weaponPrefab, int currentLevel, UpgradeData.UpgradeType type)
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
                    _equippedWeapons.Add(new KeyValuePair<GameObject, GameObject>(weaponPrefab, newWeapon));
                    ApplyWeaponLevel(newWeapon, currentLevel);
                    var definition = DefinitionOf(type);
                    TrackForResults(newWeapon, UpgradeText.NameOf(type), definition != null ? definition.Icon : null, GetMaxWeaponLevel(type), false);
                    Debug.Log($"[WeaponManager] {weaponPrefab.name} is now SPAWNED and ACTIVE on Player!");
                }
            }
            else
            {
                // Agar pehle se paida ho chuka hai, toh uski power badhani hogi.
                Debug.Log($"[WeaponManager] {weaponPrefab.name} Upgraded to Level {currentLevel}!");
                var spawned = _spawnedWeapons[weaponPrefab];
                var upgraded = spawned != null ? spawned.GetComponent<AutoAttackWeapon>() : null;
                int levelBefore = upgraded != null ? upgraded.CurrentLevel : 0;
                ApplyWeaponLevel(spawned, currentLevel);
                // VS (wiki Weapons): upgrading resets the weapon's Cooldown and fires it at once.
                if (upgraded != null && upgraded.CurrentLevel > levelBefore) upgraded.ResetCooldownAndFire();
            }
        }

        public static void ApplyWeaponLevel(GameObject weaponObject, int targetLevel)
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
                        LastEvolutionId = evo.name;
                        _madeThisRun.Add(evo.name);
                        var baseDefinition = DefinitionOf(evo.BaseWeapon);
                        LastEvolvedIcon = baseDefinition != null ? baseDefinition.Icon : null;
                        
                        // Old weapon disable karo
                        DisableOldWeapon(evo.BaseWeapon);
                        
                        // Naya Evolved weapon Instantiate ya Enable karo
                        if (evo.EvolvedWeaponPrefab != null)
                        {
                            var newWep = Instantiate(evo.EvolvedWeaponPrefab, spawnTarget);
                            newWep.SetActive(true);
                            _equippedWeapons.Add(new KeyValuePair<GameObject, GameObject>(evo.EvolvedWeaponPrefab, newWep));
                            ApplyWeaponLevel(newWep, evo.BaseWeaponMaxLevel);
                            TrackForResults(newWep, !string.IsNullOrEmpty(evo.EvolvedName) ? WeaponEvolutionData.DisplayName(evo.EvolvedName) : WeaponEvolutionData.DisplayName(evo.name), LastEvolvedIcon, 0, true);
                        }

                        evolvedWeaponName = evo.EvolvedName;
                        Debug.Log($"[WeaponManager] EVOLUTION SUCCESS! {evo.BaseWeapon} evolved into {evo.EvolvedName}!");
                        OnInventoryChanged?.Invoke();
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
                        LastEvolutionId = union.name;
                        _madeThisRun.Add(union.name);
                        _unionConsumedWeapons.Add(union.WeaponA);
                        _unionConsumedWeapons.Add(union.WeaponB);
                        _activeUnionWeaponCount++;
                        _unionIconBySlot[union.WeaponA] = union.UnionIcon;
                        LastEvolvedIcon = union.UnionIcon;
                        
                        DisableOldWeapon(union.WeaponA);
                        DisableOldWeapon(union.WeaponB);
                        
                        if (union.UnionWeaponPrefab != null)
                        {
                            var newWep = Instantiate(union.UnionWeaponPrefab, spawnTarget);
                            newWep.SetActive(true);
                            _equippedWeapons.Add(new KeyValuePair<GameObject, GameObject>(union.UnionWeaponPrefab, newWep));
                            ApplyWeaponLevel(newWep, Mathf.Max(union.WeaponAMaxLevel, union.WeaponBMaxLevel));
                            TrackForResults(newWep, !string.IsNullOrEmpty(union.UnionName) ? WeaponEvolutionData.DisplayName(union.UnionName) : union.name, union.UnionIcon, 0, true);
                        }

                        evolvedWeaponName = union.UnionName;
                        Debug.Log($"[WeaponManager] UNION SUCCESS! {union.WeaponA} + {union.WeaponB} = {union.UnionName}!");
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Starts the weapon's row on the VS Results screen (level, damage, time used, DPS).</summary>
        private static void TrackForResults(GameObject instance, string name, Sprite icon, int maxLevel, bool evolved)
        {
            var tracker = SoulHunter.Gameplay.Core.RunStatsTracker.Instance;
            var weapon = instance != null ? instance.GetComponent<AutoAttackWeapon>() : null;
            if (tracker != null && weapon != null) tracker.TrackWeapon(weapon, name, icon, maxLevel, evolved);
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
