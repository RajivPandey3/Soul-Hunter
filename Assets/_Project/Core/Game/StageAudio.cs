using UnityEngine;
using SoulHunter.Gameplay.Audio;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// VS rule: every stage has its own music. Plays the current stage's loop, the stage-clear jingle,
    /// and stops the music for the game-over or victory sound. GameSessionManager adds this to itself.
    /// </summary>
    public sealed class StageAudio : MonoBehaviour
    {
        private LevelProgressionManager _progression;
        private GameSessionManager _session;

        private void Start()
        {
            _progression = LevelProgressionManager.Instance;
            _session = GetComponent<GameSessionManager>();
            if (_progression != null)
            {
                _progression.OnStageStarted += HandleStageStarted;
                _progression.OnStageCleared += HandleStageCleared;
            }
            if (_session != null)
            {
                _session.OnGameOver += HandleGameOver;
                _session.OnVictory += HandleVictory;
            }
            HandleStageStarted(_progression != null ? _progression.CurrentStage : 1);
        }

        private void OnDestroy()
        {
            if (_progression != null)
            {
                _progression.OnStageStarted -= HandleStageStarted;
                _progression.OnStageCleared -= HandleStageCleared;
            }
            if (_session != null)
            {
                _session.OnGameOver -= HandleGameOver;
                _session.OnVictory -= HandleVictory;
            }
        }

        private static void HandleStageStarted(int stage)
        {
            var audio = AudioManager.Instance;
            if (audio != null) audio.PlayStageMusic(stage);
        }

        private static void HandleStageCleared(int _)
        {
            var audio = AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.StageClearSound);
        }

        private static void HandleGameOver()
        {
            var audio = AudioManager.Instance;
            if (audio == null) return;
            audio.StopMusic();
            audio.PlaySFX(audio.GameOverSound);
        }

        private static void HandleVictory()
        {
            var audio = AudioManager.Instance;
            if (audio == null) return;
            audio.StopMusic();
            audio.PlaySFX(audio.StageClearSound);
        }
    }
}
