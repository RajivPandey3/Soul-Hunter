using UnityEngine;
using System;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Learning Comment:
    /// GDD Feature: 10-Level Progression System.
    /// Ye manager stages (1 se 10) ko handle karta hai. Har stage mein time chalta hai, boss aata hai,
    /// aur boss ke marne par next stage shuru hota hai.
    /// </summary>
    public class LevelProgressionManager : MonoBehaviour
    {
        private static LevelProgressionManager _instance;
        public static LevelProgressionManager Instance 
        { 
            get 
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<LevelProgressionManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("LevelProgressionManager (Auto)");
                        _instance = go.AddComponent<LevelProgressionManager>();
                    }
                }
                return _instance;
            }
        }
        
        public int CurrentStage { get; private set; } = 1;
        public const int MaxStages = 10;
        public CampaignLevelDefinition CurrentLevel => CampaignLevelCatalog.Get(CurrentStage);
        
        public event Action<int> OnStageStarted;
        public event Action<int> OnStageCleared;
        public event Action OnGameWon;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else if (_instance != this) Destroy(gameObject);
        }

        private void Start()
        {
            // Game start hone par Stage 1 shuru karo
            StartStage(1);
        }

        private bool _gameWon;

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        public void StartStage(int stageNumber)
        {
            if (_gameWon) return;
            CurrentStage = Mathf.Clamp(stageNumber, 1, MaxStages);
            var level = CurrentLevel;
            Debug.Log($"[LevelProgressionManager] Stage {CurrentStage}: {level.Name} Started! Modifier: {level.Modifier}");
            OnStageStarted?.Invoke(CurrentStage);
        }

        public void ReportBossDefeated()
        {
            Debug.Log($"[LevelProgressionManager] Boss of Stage {CurrentStage} Defeated!");
            
            // Game Feel: Boss marne par arena ke baqi saare chote dushman raakh ban jayen
            WipeArena();

            OnStageCleared?.Invoke(CurrentStage);
            
            if (CurrentStage >= MaxStages)
            {
                GameWon();
            }
            else
            {
                // 5 seconds ka sakoon (breather) aur phir next stage
                StartCoroutine(NextStageRoutine());
            }
        }

        private void WipeArena()
        {
            var enemies = SoulHunter.Gameplay.AI.EnemyController.ActiveEnemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                if (enemy == null) continue;
                var health = enemy.GetComponent<HealthController>();
                // Only kill them if they are alive (don't double kill)
                if (health != null && health.CurrentHealth > 0)
                {
                    // Instant kill
                    health.TakeDamage(new DamagePacket(99999, enemy.transform.position, Vector3.zero));
                }
            }
        }

        private System.Collections.IEnumerator NextStageRoutine()
        {
            yield return new WaitForSeconds(5f); 
            StartStage(CurrentStage + 1);
        }

        private void GameWon()
        {
            if (_gameWon) return;
            _gameWon = true;
            Debug.Log("[LevelProgressionManager] ALL 10 STAGES CLEARED! YOU BEAT THE GAME!");
            OnGameWon?.Invoke();
            Time.timeScale = 0f; // Game finish
        }
    }
}
