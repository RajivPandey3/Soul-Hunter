using UnityEngine;
using System;

namespace SoulHunter.Gameplay.Player
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors ka Level Up System.
    /// XP collect hone par bar badhta hai. Full hone par agla level aata hai.
    /// </summary>
    public class PlayerExperience : MonoBehaviour
    {
        public int CurrentLevel { get; private set; } = 1;
        public int CurrentXP { get; private set; } = 0;
        public int XPToNextLevel { get; private set; } = 100;

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
                
                // Agle level ke liye XP requirement badhti jayegi
                XPToNextLevel = Mathf.FloorToInt(XPToNextLevel * 1.5f);
                
                // Event fire karo (Game pause karna aur Upgrade menu kholna aage banayenge)
                OnLevelUp?.Invoke(CurrentLevel);
                Debug.Log($"[PlayerExperience] Level Up! New Level: {CurrentLevel}");
            }

            OnXPChanged?.Invoke(CurrentXP, XPToNextLevel);
        }
    }
}
