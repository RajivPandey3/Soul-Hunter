using UnityEngine;
using System.Collections.Generic;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Learning Comment:
    /// ArcanaManager game ke shuru mein ya chest (11:00 or 21:00 min) se naye cards (Arcanas) apply karta hai.
    /// Ye player stats aur weapons dono par gehra asar daalta hai.
    /// </summary>
    public class ArcanaManager : MonoBehaviour
    {
        // Player ke paas maujood active cards
        public List<ArcanaData> ActiveArcanas { get; private set; } = new List<ArcanaData>();

        [Tooltip("Test karne ke liye shuru mein kon sa card dena hai? (Optional)")]
        [SerializeField] private ArcanaData _startingArcanaTest;

        private void Start()
        {
            // Agar test card diya hai toh start mein apply kardo
            if (_startingArcanaTest != null)
            {
                ApplyArcana(_startingArcanaTest);
            }
        }

        public void ApplyArcana(ArcanaData arcana)
        {
            if (arcana == null || ActiveArcanas.Contains(arcana)) return;

            ActiveArcanas.Add(arcana);
            Debug.Log($"[ArcanaManager] Card Selected: {arcana.CardNumber} - {arcana.CardName}");

            // Card ka jadu (Effect) yahan se shuru hota hai
            ExecuteArcanaEffect(arcana.Type);
        }

        private void ExecuteArcanaEffect(ArcanaData.ArcanaType type)
        {
            var playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats == null) return;

            switch (type)
            {
                case ArcanaData.ArcanaType.Awake:
                    // Awake: Gives +3 Revivals. In VS, each time you revive, you get stronger.
                    // For now, we give the revivals immediately.
                    playerStats.AddRevival(3);
                    Debug.Log("[ArcanaManager] AWAKE Effect: +3 Extra Lives added!");
                    break;
                
                case ArcanaData.ArcanaType.IronBlueWill:
                    // Ye backend flag set kar sakta hai jo projectiles (Axe/Cross) padhte hain.
                    Debug.Log("[ArcanaManager] IRON BLUE WILL Effect: Weapons will now bounce!");
                    break;
                
                case ArcanaData.ArcanaType.Gemini:
                    // Twins for specific weapons
                    Debug.Log("[ArcanaManager] GEMINI Effect: Specific weapons will spawn twins!");
                    break;
                    
                case ArcanaData.ArcanaType.WaltzOfPearls:
                    Debug.Log("[ArcanaManager] WALTZ OF PEARLS Effect: Magic Wand and Cross can bounce 3 times!");
                    break;
            }
        }

        public bool HasArcana(ArcanaData.ArcanaType type)
        {
            foreach (var a in ActiveArcanas)
            {
                if (a.Type == type) return true;
            }
            return false;
        }
    }
}
