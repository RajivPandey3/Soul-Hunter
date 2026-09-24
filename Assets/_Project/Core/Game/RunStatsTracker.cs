using UnityEngine;
using System.Collections.Generic;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Learning Comment:
    /// VS Rule: Post-Game Stats.
    /// Ye script track karti hai ke kis hathiyar ne kitna damage diya, aur kitne dushman mare.
    /// Game Over hone par ye data UI ko bheja jayega.
    /// </summary>
    public class RunStatsTracker : MonoBehaviour
    {
        public static RunStatsTracker Instance { get; private set; }

        public event System.Action<int> OnKillsChanged;

        public int TotalKills { get; private set; } = 0;
        public int TotalGoldCollected { get; private set; } = 0;

        // Weapon Name -> Total Damage
        private Dictionary<string, int> _weaponDamageStats = new Dictionary<string, int>();

        /// <summary>
        /// Learning Comment:
        /// Har naye match mein pichle run ke kills aur weapon damage stats ko clear karna zaroori hai.
        /// </summary>
        public void ResetStats()
        {
            TotalKills = 0;
            TotalGoldCollected = 0;
            _weaponDamageStats.Clear();
            OnKillsChanged?.Invoke(0);
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
        }

        public void AddKill()
        {
            TotalKills++;
            OnKillsChanged?.Invoke(TotalKills);
        }

        public void AddGold(int amount)
        {
            TotalGoldCollected += amount;
        }

        public void RecordDamage(string weaponName, int amount)
        {
            if (string.IsNullOrEmpty(weaponName)) weaponName = "Unknown";

            if (_weaponDamageStats.ContainsKey(weaponName))
            {
                _weaponDamageStats[weaponName] += amount;
            }
            else
            {
                _weaponDamageStats[weaponName] = amount;
            }
        }

        public Dictionary<string, int> GetWeaponStats()
        {
            return _weaponDamageStats;
        }
    }
}
