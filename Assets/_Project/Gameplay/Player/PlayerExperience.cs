using UnityEngine;
using System;

namespace SoulHunter.Gameplay.Player
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors ka Level Up System.
    /// XP collect hone par bar badhta hai. Full hone par agla level aata hai.
    /// VS XP curve: har level +10 gems (level 20 tak), phir +13 (level 40 tak), phir +16;
    /// level 20 aur 40 se nikalne par ek badi jump. Curve basic gems mein likha hai aur
    /// _xpPerBasicGem se scale hota hai (Soul Hunter ka gem 10 XP ka hai).
    /// </summary>
    public class PlayerExperience : MonoBehaviour
    {
        [Tooltip("XP of one basic gem. The VS curve is written in basic gems and scaled by this.")]
        [SerializeField, Min(1)] private int _xpPerBasicGem = 10;

        public int CurrentLevel { get; private set; } = 1;
        public int CurrentXP { get; private set; } = 0;
        private int _xpToNextLevel;
        /// <summary>XP needed for the next level. Computed on first read so UI that binds early never sees 0.</summary>
        public int XPToNextLevel
        {
            get
            {
                if (_xpToNextLevel <= 0) _xpToNextLevel = XPRequiredForLevel(CurrentLevel);
                return _xpToNextLevel;
            }
            private set => _xpToNextLevel = value;
        }

        /// <summary>Basic gems needed to go from <paramref name="level"/> to the next level (VS curve).</summary>
        public static int GemsToNextLevel(int level)
        {
            level = Mathf.Max(1, level);
            int gems = 5 + 10 * (Mathf.Min(level, 20) - 1);
            if (level > 20) gems += 13 * (Mathf.Min(level, 40) - 20);
            if (level > 40) gems += 16 * (level - 40);
            if (level == 20) gems += 600;
            if (level == 40) gems += 2400;
            return gems;
        }

        public int XPRequiredForLevel(int level) => GemsToNextLevel(level) * Mathf.Max(1, _xpPerBasicGem);

        // UI ya Upgrade Menu ko batane ke liye events
        public event Action<int, int> OnXPChanged;
        public event Action<int> OnLevelUp;
        public event Action<int> OnSoulCollected; // Fired when a gem is absorbed

        [Header("Magnet Settings (Soul Gathering)")]
        [SerializeField] private float _magnetRadius = 4f;
        private Collider[] _magnetHits = new Collider[100];
        private float _magnetCheckTimer = 0f;

        private void Update()
        {
            _magnetCheckTimer += Time.deltaTime;
            // Optimize: Check radius only 5 times a second (0.2s) instead of every frame
            if (_magnetCheckTimer >= 0.2f)
            {
                _magnetCheckTimer = 0f;
                var stats = GetComponent<PlayerStats>();
                float pickupRadius = _magnetRadius * (stats != null ? Mathf.Max(0.1f, stats.Magnet) : 1f);
                int count = UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, pickupRadius, _magnetHits);
                for (int i = 0; i < count; i++)
                {
                    if (_magnetHits[i] == null) continue;
                    
                    var gem = _magnetHits[i].GetComponent<SoulHunter.Gameplay.Pickups.XPGem>();
                    if (gem != null)
                    {
                        gem.Magnetize(this.transform);
                    }
                }
            }
        }

        public void AddXP(int amount)
        {
            if (amount <= 0) return;
            var stats = GetComponent<PlayerStats>();
            int adjustedAmount = Mathf.Max(1, Mathf.RoundToInt(amount * (stats != null ? stats.ExpBonus : 1f)));
            CurrentXP += adjustedAmount;
            OnSoulCollected?.Invoke(adjustedAmount);
            
            // Level Up logic (Vampire Survivors style: bar bar level up ho sakta hai agar XP bohot zyada ho)
            while (CurrentXP >= XPToNextLevel)
            {
                CurrentXP -= XPToNextLevel;
                CurrentLevel++;
                
                // Agle level ke liye XP requirement (VS curve)
                XPToNextLevel = XPRequiredForLevel(CurrentLevel);
                
                // Event fire karo (Game pause karna aur Upgrade menu kholna aage banayenge)
                OnLevelUp?.Invoke(CurrentLevel);
                Debug.Log($"[PlayerExperience] Level Up! New Level: {CurrentLevel}");
            }

            OnXPChanged?.Invoke(CurrentXP, XPToNextLevel);
        }
    }
}
