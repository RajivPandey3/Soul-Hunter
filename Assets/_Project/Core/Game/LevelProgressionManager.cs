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
        public float StageElapsedTime { get; private set; }
        public bool IsBossWindow => StageElapsedTime >= CurrentLevel.BossStartSeconds;

        public event Action<int> OnStageStarted;
        public event Action<int> OnStageCleared;
        public event Action<float> OnStageTimeChanged;
        public event Action<int> OnRunDurationReached;
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
        private int _lastStageSecond = -1;
        private bool _runDurationReached;
        private bool _stageTransitionQueued;
        private bool _bossDefeated;

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private void Update()
        {
            if (_gameWon || Time.timeScale <= 0f) return;

            StageElapsedTime += Time.deltaTime;
            OnStageTimeChanged?.Invoke(StageElapsedTime);

            int second = Mathf.FloorToInt(StageElapsedTime);
            if (second == _lastStageSecond) return;
            _lastStageSecond = second;

            if (!_runDurationReached && StageElapsedTime >= CurrentLevel.RunDurationSeconds)
            {
                _runDurationReached = true;
                Debug.Log($"[LevelProgressionManager] Stage {CurrentStage} reached its 30-minute survival limit.");
                OnRunDurationReached?.Invoke(CurrentStage);
                CompleteStageAfterSurvival();
            }
        }

        public void StartStage(int stageNumber)
        {
            if (_gameWon) return;
            CurrentStage = Mathf.Clamp(stageNumber, 1, MaxStages);
            StageElapsedTime = 0f;
            _lastStageSecond = -1;
            _runDurationReached = false;
            _stageTransitionQueued = false;
            _bossDefeated = false;
            // Stage transition ke baad previous level ka pause/slow state carry
            // forward nahi hona chahiye. Frozen Peaks ka intentional slow
            // modifier CampaignSignatureSystem khud dobara apply karega.
            Time.timeScale = 1f;
            var player = FindFirstObjectByType<SoulHunter.Gameplay.Player.PlayerController>();
            if (player != null) player.ResumeMovementAfterMenu();
            var level = CurrentLevel;
            Debug.Log($"[LevelProgressionManager] Stage {CurrentStage}: {level.Name} Started! Modifier: {level.Modifier}");
            OnStageStarted?.Invoke(CurrentStage);
        }

        public void ReportBossDefeated()
        {
            if (_bossDefeated || _gameWon) return;
            _bossDefeated = true;
            Debug.Log($"[LevelProgressionManager] Boss of Stage {CurrentStage} Defeated!");
            Debug.Log($"[LevelProgressionManager] Stage {CurrentStage} continues until the full 30-minute limit.");
        }

        private void CompleteStageAfterSurvival()
        {
            if (_stageTransitionQueued) return;
            _stageTransitionQueued = true;
            Debug.Log($"[LevelProgressionManager] Stage {CurrentStage} survived for the full 30 minutes.");
            WipeArena();
            OnStageCleared?.Invoke(CurrentStage);

            if (CurrentStage >= MaxStages) GameWon();
            else StartCoroutine(NextStageRoutine());
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
                    // Instant cleanup without showing a fake 99999 damage popup.
                    health.KillSilently();
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
            GameSessionManager.Instance?.CompleteCampaign();
            Debug.Log("[LevelProgressionManager] ALL 10 STAGES CLEARED! YOU BEAT THE GAME!");
            OnGameWon?.Invoke();
            Time.timeScale = 0f; // Game finish
        }
    }
}
