using UnityEngine;
using System;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Game mein survival timer hota hai.
    /// Ye manager game ka waqt track karta hai. Spawner aur UI dono is se time puchte hain.
    /// </summary>
    public class GameSessionManager : MonoBehaviour
    {
        public static GameSessionManager Instance { get; private set; }

        public event Action<int> OnSurvivalSecondChanged;
        private int _lastSecond = -1;

        public float SurvivalTime { get; private set; }
        private bool _isGameActive;
        public bool IsRunFinished { get; private set; }
        public bool IsVictory { get; private set; }
        public event Action OnVictory;

        /// <summary>
        /// Learning Comment:
        /// Run khatam hone ya restart hone ke baad state ko initial zero par laana zaroori hai.
        /// Is ke baghair agla match foran Game Over screen par phans jata hai.
        /// </summary>
        public void ResetSession()
        {
            SurvivalTime = 0f;
            _lastSecond = -1;
            _isGameActive = true;
            IsRunFinished = false;
            IsVictory = false;
            SoulHunter.Core.Services.GameTime.ResetAll();
        }

        public void CompleteCampaign()
        {
            if (IsRunFinished) return;
            IsRunFinished = true;
            IsVictory = true;
            _isGameActive = false;
            GameSystemManager.Instance?.SaveHighScore(SurvivalTime);
            SoulHunter.Core.Services.GameTime.Pause(this);
            OnVictory?.Invoke();
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                ResetSession();
            }
            else if (Instance != this)
            {
                // Learning Comment: Agar purana singleton instance pehle se majood ho toh naye run ke liye uska session reset karein
                Instance.ResetSession();
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            ResetSession();
        }

        private void Update()
        {
            if (_isGameActive)
            {
                // Time.deltaTime automatic Time.timeScale ka dhyan rakhta hai
                // (jab game pause hoga toh ye nahi badhega)
                SurvivalTime += Time.deltaTime;
                int second = Mathf.FloorToInt(SurvivalTime);
                if (second != _lastSecond)
                {
                    _lastSecond = second;
                    OnSurvivalSecondChanged?.Invoke(second);
                }
            }
        }

        public event Action OnGameOver;

        public void GameOver()
        {
            if (!_isGameActive || IsRunFinished) return;

            IsRunFinished = true;
            _isGameActive = false;
            Debug.Log($"[GameSessionManager] GAME OVER! Survival Time: {SurvivalTime} seconds");

            if (SoulHunter.Gameplay.Core.GameSystemManager.Instance != null)
            {
                SoulHunter.Gameplay.Core.GameSystemManager.Instance.SaveHighScore(SurvivalTime);
            }

            // Pause game
            SoulHunter.Core.Services.GameTime.Pause(this);

            OnGameOver?.Invoke();
        }

        public string GetFormattedTime()
        {
            int minutes = Mathf.FloorToInt(SurvivalTime / 60f);
            int seconds = Mathf.FloorToInt(SurvivalTime % 60f);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        private void OnDestroy()
        {
            // A scene transition must never carry a paused timescale into the next run.
            if (Instance == this)
            {
                SoulHunter.Core.Services.GameTime.ResetAll();
                Instance = null;
            }
        }
    }
}
