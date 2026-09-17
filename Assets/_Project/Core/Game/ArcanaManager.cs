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
    /// </summary>
    public class ArcanaManager : MonoBehaviour
    {
        public static ArcanaManager Instance { get; private set; }
        // Player ke paas maujood active cards
        public List<ArcanaData> ActiveArcanas { get; private set; } = new List<ArcanaData>();

        [Tooltip("Test karne ke liye shuru mein kon sa card dena hai? (Optional)")]
        [SerializeField] private ArcanaData _startingArcanaTest;
        [SerializeField] private List<ArcanaData> _availableArcanas = new List<ArcanaData>();
        [SerializeField] private float _firstArcanaTime = 660f;
        [SerializeField] private float _secondArcanaTime = 1260f;
        public int ProjectileBounceCount { get; private set; }
        public bool GeminiEnabled { get; private set; }
        public float GeminiSpreadDegrees { get; private set; } = 12f;
        public event Action<ArcanaData> OnArcanaAwarded;
        private bool _firstTimedCardGranted;
        private bool _secondTimedCardGranted;
        private LevelProgressionManager _progression;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);
        }

        private void OnEnable()
        {
            _progression = LevelProgressionManager.Instance;
            _progression.OnStageStarted += HandleStageStarted;
        }

        private void HandleStageStarted(int stage)
        {
            // Arcana clocks are run-local to each campaign stage. Existing
            // cards persist, but the two timed award windows are re-armed.
            _firstTimedCardGranted = false;
            _secondTimedCardGranted = false;
        }

        private void Start()
        {
            // Agar test card diya hai toh start mein apply kardo
            if (_startingArcanaTest != null)
            {
                ApplyArcana(_startingArcanaTest);
            }
        }

        private void Update()
        {
            float time = _progression != null ? _progression.StageElapsedTime : 0f;
            if (!_firstTimedCardGranted && time >= _firstArcanaTime)
            {
                _firstTimedCardGranted = true;
                GrantNextAvailableArcana();
            }
            if (!_secondTimedCardGranted && time >= _secondArcanaTime)
            {
                _secondTimedCardGranted = true;
                GrantNextAvailableArcana();
            }
        }

        private void OnDestroy()
        {
            if (_progression != null) _progression.OnStageStarted -= HandleStageStarted;
            if (Instance == this) Instance = null;
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
            var playerStats = FindFirstObjectByType<PlayerStats>();
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
