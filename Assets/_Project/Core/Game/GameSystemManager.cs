using UnityEngine;
using UnityEngine.UI;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Ye script Game ko Pause karne, Volume set karne, aur High Score save karne ka kaam karti hai.
    /// Ye "Main Menu" aur "Pause Menu" dono ke liye ek Master script hai.
    /// </summary>

    /// <summary>
    /// Learning Comment:
    /// Yeh script GameSystemManager.cs ke core logic ko handle karti hai.
    /// </summary>
    public class GameSystemManager : MonoBehaviour
    {
        public static GameSystemManager Instance { get; private set; }

        [Header("UI Panels")]
        public GameObject PauseMenuPanel;

        [Header("Settings UI")]
        public Slider VolumeSlider;

        private bool _isPaused = false;

        public bool IsPaused => _isPaused;
        /// <summary>Raised when the pause menu opens (true) or closes (false); the pause UI sets focus on it.</summary>
        public event System.Action<bool> OnPauseChanged;
        private SoulHunter.Core.Persistence.SaveService _save;
        private SoulHunter.Core.Events.EventBus _eventBus;
        private SoulHunter.Core.Events.EventSubscription _pauseSubscription;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { enabled = false; return; }

        }

        private void Start()
        {
            if (SoulHunter.Core.Services.GameServices.Instance != null)
            {
                SoulHunter.Core.Services.GameServices.Instance.TryGet(out _save);
                SoulHunter.Core.Services.GameServices.Instance.TryGet(out _eventBus);
            }
            if (_eventBus != null) _pauseSubscription = _eventBus.Subscribe<SoulHunter.Core.Events.PauseToggleEvent>(_ => TogglePause());
            LoadGameData();
        }

        public void TogglePause()
        {
            if (GameSessionManager.Instance != null && GameSessionManager.Instance.IsRunFinished) return;
            _isPaused = !_isPaused;

            if (PauseMenuPanel != null)
            {
                PauseMenuPanel.SetActive(_isPaused);
            }

            if (_isPaused)
            {
                SoulHunter.Core.Services.GameTime.Pause(this); // Game ko freeze kar do
            }
            else
            {
                SoulHunter.Core.Services.GameTime.Resume(this); // Game wapas chala do
            }
            OnPauseChanged?.Invoke(_isPaused);
        }

        public void Resume()
        {
            if (_isPaused) TogglePause();
        }

        /// <summary>Leaves the run for the main menu (VS pause menu "Quit").</summary>
        public void QuitToMainMenu()
        {
            if (_isPaused) TogglePause();
            SoulHunter.Core.Services.GameTime.ResetAll();
            if (SoulHunter.Core.Services.GameServices.Instance != null &&
                SoulHunter.Core.Services.GameServices.Instance.TryGet(out SoulHunter.Core.Scenes.SceneService scenes))
                scenes.LoadSceneAsync("MainMenu");
        }

        // Options: pause when the window loses focus (alt-tab), unless another screen already paused the game.
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus || _isPaused || !SoulHunter.Core.Persistence.GameSettings.PauseOnFocusLoss) return;
            if (SoulHunter.Core.Services.GameTime.IsPaused) return;
            if (GameSessionManager.Instance == null || GameSessionManager.Instance.IsRunFinished) return;
            TogglePause();
        }

        // ==========================================
        // VOLUME SYSTEM (Sound Manager Connection)
        // ==========================================
        public void OnVolumeChanged(float value)
        {
            value = Mathf.Clamp01(value);
            if (SoulHunter.Gameplay.Audio.AudioManager.Instance != null)
                SoulHunter.Gameplay.Audio.AudioManager.Instance.SetMasterVolume(value);
            else
                AudioListener.volume = value;
            _save?.SetMasterVolume(value);
        }

        // Compatibility callback retained for existing callers; persistence owns the score.
        public void SaveHighScore(float currentTime) { _save?.RecordHighScore(currentTime); }

        private void LoadGameData()
        {
            if (_save == null) return;
            AudioListener.volume = _save.CurrentData.MasterVolume;
            if (VolumeSlider != null) VolumeSlider.SetValueWithoutNotify(AudioListener.volume);
        }

        private void OnDestroy()
        {
            _pauseSubscription?.Dispose();
            if (Instance == this) Instance = null;
        }

        // Button Click se game restart ya quit karne ke liye
        public void QuitGame()
        {
            Debug.Log("Game Quit!");
            Application.Quit();
        }
    }
}
