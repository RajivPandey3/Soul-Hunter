using UnityEngine;
using System.Collections.Generic;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Learning Comment:
    /// VS Rule: Post-Game Stats.
    /// Ye script track karti hai ke kis hathiyar ne kitna damage diya, aur kitne dushman mare.
    /// Game Over hone par ye data UI ko bheja jayega.
    ///
    /// VS Results (vampire.survivors.wiki/w/Results, fetched 2026-10-02): per weapon its level, damage, time used and
    /// DPS; every weapon and passive picked up with its level; the pickups and how many; the run's unlocks.
    /// </summary>
    public class RunStatsTracker : MonoBehaviour
    {
        public static RunStatsTracker Instance { get; private set; }

        public event System.Action<int> OnKillsChanged;
        /// <summary>Gold collected this run (after Greed) changed.</summary>
        public event System.Action<int> OnGoldChanged;

        public int TotalKills { get; private set; } = 0;
        public int TotalGoldCollected { get; private set; } = 0;

        /// <summary>One row of the Results weapon table: a held weapon, or a damage source that is not one (Soul Burst).</summary>
        public sealed class WeaponRecord
        {
            public AutoAttackWeapon Weapon;
            public string Name;
            public Sprite Icon;
            public int Level;
            public int MaxLevel;
            /// <summary>An evolution or union (it cannot level further, so its level shows as max).</summary>
            public bool Evolved;
            public double Damage;
            /// <summary>Game seconds the weapon was held and switched on (VS "time used").</summary>
            public float SecondsUsed;
            public double Dps => RunResultsFormat.Dps(Damage, SecondsUsed);
        }

        /// <summary>One item picked up this run, at the highest level it reached.</summary>
        public struct ItemRecord
        {
            public UpgradeData.UpgradeType Type;
            public bool IsWeapon;
            public int Level;
        }

        private readonly List<WeaponRecord> _weapons = new List<WeaponRecord>();
        private readonly Dictionary<AutoAttackWeapon, WeaponRecord> _byWeapon = new Dictionary<AutoAttackWeapon, WeaponRecord>();
        private readonly Dictionary<string, WeaponRecord> _byName = new Dictionary<string, WeaponRecord>();
        private readonly List<ItemRecord> _items = new List<ItemRecord>();
        private readonly List<KeyValuePair<string, int>> _pickups = new List<KeyValuePair<string, int>>();
        private readonly List<string> _unlocks = new List<string>();
        private WeaponManager _watchedManager;

        // Weapon Name -> Total Damage
        private Dictionary<string, int> _weaponDamageStats = new Dictionary<string, int>();

        public IReadOnlyList<WeaponRecord> Weapons => _weapons;
        public IReadOnlyList<ItemRecord> Items => _items;
        public IReadOnlyList<KeyValuePair<string, int>> Pickups => _pickups;
        public IReadOnlyList<string> Unlocks => _unlocks;

        /// <summary>
        /// Learning Comment:
        /// Har naye match mein pichle run ke kills aur weapon damage stats ko clear karna zaroori hai.
        /// </summary>
        public void ResetStats()
        {
            TotalKills = 0;
            TotalGoldCollected = 0;
            _weaponDamageStats.Clear();
            _weapons.Clear();
            _byWeapon.Clear();
            _byName.Clear();
            _items.Clear();
            _pickups.Clear();
            _unlocks.Clear();
            OnKillsChanged?.Invoke(0);
            OnGoldChanged?.Invoke(0);
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                ResetStats();
            }
            else if (Instance != this)
            {
                Instance.ResetStats();
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            if (_watchedManager != null) _watchedManager.OnInventoryChanged -= RecordInventory;
        }

        private void Update()
        {
            // Inventory: follow the run's WeaponManager (it can be created after this tracker).
            var manager = WeaponManager.Instance;
            if (manager != _watchedManager)
            {
                if (_watchedManager != null) _watchedManager.OnInventoryChanged -= RecordInventory;
                _watchedManager = manager;
                if (manager != null) { manager.OnInventoryChanged += RecordInventory; RecordInventory(); }
            }

            // Time used: game time (0 while paused) while the weapon is held and on.
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            foreach (var record in _weapons)
            {
                if (record.Weapon != null)
                {
                    if (!record.Weapon.isActiveAndEnabled) continue;
                    record.Level = Mathf.Max(record.Level, record.Weapon.CurrentLevel);
                    record.SecondsUsed += dt;
                }
                else if (record.Damage > 0d) record.SecondsUsed += dt; // a source that is not a weapon: from its first hit
            }
        }

        public void AddKill() => AddKill(null);

        /// <summary>
        /// Counts one kill. <paramref name="type"/> is the enemy's EnemyData (null when unknown); the VS Bestiary counts
        /// defeats per type on the save (CollectionTracker listens to <see cref="EnemyKilled"/>).
        /// </summary>
        public void AddKill(SoulHunter.Gameplay.Data.EnemyData type)
        {
            TotalKills++;
            OnKillsChanged?.Invoke(TotalKills);
            if (type != null) EnemyKilled?.Invoke(type);
        }

        /// <summary>A player-side kill of an enemy type (VS Bestiary).</summary>
        public static event System.Action<SoulHunter.Gameplay.Data.EnemyData> EnemyKilled;
        /// <summary>A pickup collected, by its Results name (VS Collection: a pickup is found once used).</summary>
        public static event System.Action<string> PickupRecorded;

        public void AddGold(int amount)
        {
            TotalGoldCollected += amount;
            OnGoldChanged?.Invoke(TotalGoldCollected);
        }

        /// <summary>Starts a weapon's row when the player gains it, so its time used counts from then.</summary>
        public void TrackWeapon(AutoAttackWeapon weapon, string name, Sprite icon, int maxLevel, bool evolved)
        {
            if (weapon == null || weapon.IsHeldByEnemy) return;
            if (!_byWeapon.TryGetValue(weapon, out var record))
            {
                record = new WeaponRecord { Weapon = weapon };
                _byWeapon[weapon] = record;
                _weapons.Add(record);
            }
            if (!string.IsNullOrEmpty(name)) record.Name = name;
            if (icon != null) record.Icon = icon;
            record.MaxLevel = maxLevel;
            record.Evolved = evolved;
            record.Level = Mathf.Max(record.Level, weapon.CurrentLevel);
        }

        /// <summary>
        /// Credits one hit on an enemy to its weapon (<see cref="DamageSource"/>). Shadow Kael's mirrored weapons are not
        /// the player's and are left out.
        /// </summary>
        public void RecordHit(AutoAttackWeapon weapon, string name, int amount)
        {
            if (amount <= 0) return;
            WeaponRecord record;
            if (weapon != null)
            {
                if (weapon.IsHeldByEnemy) return;
                if (!_byWeapon.TryGetValue(weapon, out record))
                {
                    record = new WeaponRecord { Weapon = weapon, Name = weapon.name.Replace("(Clone)", "").Trim(), Level = weapon.CurrentLevel };
                    _byWeapon[weapon] = record;
                    _weapons.Add(record);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(name)) return;
                if (!_byName.TryGetValue(name, out record))
                {
                    record = new WeaponRecord { Name = name };
                    _byName[name] = record;
                    _weapons.Add(record);
                }
            }
            record.Damage += amount;
            string key = string.IsNullOrEmpty(record.Name) ? "Unknown" : record.Name;
            _weaponDamageStats[key] = (_weaponDamageStats.TryGetValue(key, out int sum) ? sum : 0) + amount;
        }

        /// <summary>Older string-keyed damage (kept for callers that still name their source).</summary>
        public void RecordDamage(string weaponName, int amount)
        {
            if (string.IsNullOrEmpty(weaponName)) weaponName = "Unknown";
            RecordHit(null, weaponName, amount);
        }

        public Dictionary<string, int> GetWeaponStats()
        {
            return _weaponDamageStats;
        }

        /// <summary>Counts one pickup collected (VS Results "pickups and the amount of them").</summary>
        public void RecordPickup(string pickupName)
        {
            if (string.IsNullOrEmpty(pickupName)) return;
            PickupRecorded?.Invoke(pickupName);
            for (int i = 0; i < _pickups.Count; i++)
                if (_pickups[i].Key == pickupName) { _pickups[i] = new KeyValuePair<string, int>(pickupName, _pickups[i].Value + 1); return; }
            _pickups.Add(new KeyValuePair<string, int>(pickupName, 1));
        }

        public int PickupCount(string pickupName)
        {
            foreach (var pair in _pickups) if (pair.Key == pickupName) return pair.Value;
            return 0;
        }

        /// <summary>Something unlocked during this run (a character, a PowerUp rank), listed on the Results screen.</summary>
        public void RecordUnlock(string text)
        {
            if (!string.IsNullOrEmpty(text) && !_unlocks.Contains(text)) _unlocks.Add(text);
        }

        private void RecordInventory()
        {
            if (_watchedManager == null) return;
            AddItems(_watchedManager.GetInventory(false), true);
            AddItems(_watchedManager.GetInventory(true), false);
        }

        private void AddItems(List<WeaponManager.InventorySlot> slots, bool weapons)
        {
            foreach (var slot in slots)
            {
                int index = _items.FindIndex(item => item.Type == slot.Type);
                if (index < 0) _items.Add(new ItemRecord { Type = slot.Type, IsWeapon = weapons, Level = slot.Level });
                else if (slot.Level > _items[index].Level)
                    _items[index] = new ItemRecord { Type = slot.Type, IsWeapon = weapons, Level = slot.Level };
            }
        }
    }
}
