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

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            SurvivalTime = 0f;
            _isGameActive = true;
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
            if (!_isGameActive) return;

            _isGameActive = false;
            Debug.Log($"[GameSessionManager] GAME OVER! Survival Time: {SurvivalTime} seconds");

            if (SoulHunter.Gameplay.Core.GameSystemManager.Instance != null)
            {
                SoulHunter.Gameplay.Core.GameSystemManager.Instance.SaveHighScore(SurvivalTime);
            }

            // Pause game
            Time.timeScale = 0f;

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
                Time.timeScale = 1f;
                Instance = null;
            }
        }
    }
}
