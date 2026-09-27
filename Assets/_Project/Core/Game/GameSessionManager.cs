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
                // VS rule: characters unlock during runs; the session owns the tracker so every run has one.
                if (GetComponent<CharacterUnlockTracker>() == null) gameObject.AddComponent<CharacterUnlockTracker>();
                if (GetComponent<StageAudio>() == null) gameObject.AddComponent<StageAudio>();
                // VS light sources: breakable Soul Lanterns appear just off-screen and drop items.
                if (GetComponent<SoulHunter.Gameplay.Pickups.LightSourceSpawner>() == null)
                    gameObject.AddComponent<SoulHunter.Gameplay.Pickups.LightSourceSpawner>();
                // VS stage items: a passive, a weapon, a chicken and a Vacuum/Orologion a few tiles from each stage's start.
                if (GetComponent<SoulHunter.Gameplay.Pickups.StageItemSpawner>() == null)
                    gameObject.AddComponent<SoulHunter.Gameplay.Pickups.StageItemSpawner>();
                // Corridor stages (the Ruined Castle, as VS's Inlaid Library) are walled along their length.
                if (GetComponent<SoulHunter.Gameplay.Environment.CorridorWalls>() == null)
                    gameObject.AddComponent<SoulHunter.Gameplay.Environment.CorridorWalls>();
                // VS stage hazards on the stage clock: Death's Shadow (Stalker), Soul Meteors (Shooting Star),
                // Pile Assaults, and the rail carts of Stage 3.
                if (GetComponent<SoulHunter.Gameplay.Hazards.StageHazards>() == null)
                    gameObject.AddComponent<SoulHunter.Gameplay.Hazards.StageHazards>();
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
