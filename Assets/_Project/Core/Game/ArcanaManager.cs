using UnityEngine;
using System.Collections.Generic;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Combat;
using System;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Learning Comment:
    /// ArcanaManager game ke shuru mein ya chest (11:00 or 21:00 min) se naye cards (Arcanas) apply karta hai.
    /// Ye player stats aur weapons dono par gehra asar daalta hai.
    /// VS Arcana mode (vampire.survivors.wiki/w/Arcanas, 2026-09-27): Arcanas exist only with the mode on. The run
    /// starts with one Arcana, and Arcana chests (dropped by the 11:00 and 21:00 chest elites, EnemySpawner) give one
    /// more each, until the player holds three; after that those elites drop regular chests. VS lets the player choose
    /// from four; Soul Hunter has no Arcana choice screen yet, so a random unowned Arcana is given.
    /// Before 2026-09-27 this manager gave the next Arcana on every stage's 11:00 and 21:00 whatever the mode.
    /// </summary>
    public class ArcanaManager : MonoBehaviour
    {
        public static ArcanaManager Instance { get; private set; }
        // Player ke paas maujood active cards
        public List<ArcanaData> ActiveArcanas { get; private set; } = new List<ArcanaData>();

        [Tooltip("Test karne ke liye shuru mein kon sa card dena hai? (Optional)")]
        [SerializeField] private ArcanaData _startingArcanaTest;
        [SerializeField] private List<ArcanaData> _availableArcanas = new List<ArcanaData>();
        /// <summary>VS: with three or more Arcanas, the Arcana elites drop regular chests.</summary>
        public const int MaxArcanasFromChests = 3;
        public int ProjectileBounceCount { get; private set; }
        public bool GeminiEnabled { get; private set; }
        public float GeminiSpreadDegrees { get; private set; } = 12f;
        public event Action<ArcanaData> OnArcanaAwarded;
        /// <summary>The Arcana given at the start of an Arcana-mode run (null otherwise).</summary>
        public ArcanaData StartingArcana { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);
        }

        private void Start()
        {
            // Agar test card diya hai toh start mein apply kardo
            if (_startingArcanaTest != null)
            {
                ApplyArcana(_startingArcanaTest);
            }
            // VS Arcana mode: the run starts with one Arcana (RunModes is set in LevelProgressionManager.Awake).
            else if (RunModes.Arcana)
            {
                StartingArcana = GrantRandomAvailableArcana(UnityEngine.Random.value);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>True while an Arcana chest still gives an Arcana (fewer than three held and one left to give).</summary>
        public bool CanGrantFromChest => ActiveArcanas.Count < MaxArcanasFromChests && AvailableCount > 0;

        public int AvailableCount
        {
            get
            {
                int count = 0;
                foreach (var arcana in _availableArcanas)
                    if (arcana != null && !ActiveArcanas.Contains(arcana)) count++;
                return count;
            }
        }

        /// <summary>Gives a random Arcana the player does not hold yet. <paramref name="roll01"/> is uniform in [0, 1).</summary>
        public ArcanaData GrantRandomAvailableArcana(float roll01)
        {
            var available = new List<ArcanaData>();
            foreach (var arcana in _availableArcanas)
                if (arcana != null && !ActiveArcanas.Contains(arcana) && !available.Contains(arcana)) available.Add(arcana);
            if (available.Count == 0) return null;
            var pick = available[Mathf.Clamp(Mathf.FloorToInt(roll01 * available.Count), 0, available.Count - 1)];
            ApplyArcana(pick);
            return pick;
        }

        /// <summary>Tests and tools: the Arcanas this manager can give.</summary>
        public void SetAvailableArcanas(IEnumerable<ArcanaData> arcanas)
        {
            _availableArcanas = new List<ArcanaData>(arcanas);
        }

        public bool GrantNextAvailableArcana()
        {
            foreach (var arcana in _availableArcanas)
                if (arcana != null && !ActiveArcanas.Contains(arcana))
                {
                    ApplyArcana(arcana);
                    return true;
                }
            return false;
        }

        public void ApplyArcana(ArcanaData arcana)
        {
            if (arcana == null || ActiveArcanas.Contains(arcana)) return;

            ActiveArcanas.Add(arcana);
            Debug.Log($"[ArcanaManager] Card Selected: {arcana.CardNumber} - {arcana.CardName}");

            // Card ka jadu (Effect) yahan se shuru hota hai
            ExecuteArcanaEffect(arcana.Type);
            OnArcanaAwarded?.Invoke(arcana);
        }

        private void ExecuteArcanaEffect(ArcanaData.ArcanaType type)
        {
            // The player's stats, never Shadow Kael's mirrored copy.
            var playerStats = PlayerController.Instance != null ? PlayerController.Instance.Stats : FindFirstObjectByType<PlayerStats>();
            if (playerStats == null) return;

            switch (type)
            {
                case ArcanaData.ArcanaType.Awake:
                    // Awake: Gives +3 Revivals. In VS, each time you revive, you get stronger.
                    // For now, we give the revivals immediately.
                    playerStats.AddRevival(3);
                    playerStats.AddArmor(1);
                    var health = playerStats.GetComponent<HealthController>();
                    if (health != null) health.IncreaseMaxHealth(Mathf.Max(1, Mathf.RoundToInt(health.MaxHealth * 0.1f)));
                    Debug.Log("[ArcanaManager] AWAKE Effect: +3 Extra Lives added!");
                    break;
                
                case ArcanaData.ArcanaType.IronBlueWill:
                    // Ye backend flag set kar sakta hai jo projectiles (Axe/Cross) padhte hain.
                    ProjectileBounceCount = Mathf.Max(ProjectileBounceCount, 1);
                    Debug.Log("[ArcanaManager] IRON BLUE WILL Effect: Projectiles now bounce once.");
                    break;
                
                case ArcanaData.ArcanaType.Gemini:
                    // Twins for specific weapons
                    GeminiEnabled = true;
                    Debug.Log("[ArcanaManager] GEMINI Effect: Compatible projectiles now duplicate.");
                    break;
                    
                case ArcanaData.ArcanaType.WaltzOfPearls:
                    ProjectileBounceCount = Mathf.Max(ProjectileBounceCount, 3);
                    Debug.Log("[ArcanaManager] WALTZ OF PEARLS Effect: Magic Wand and Cross bounce three times.");
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
