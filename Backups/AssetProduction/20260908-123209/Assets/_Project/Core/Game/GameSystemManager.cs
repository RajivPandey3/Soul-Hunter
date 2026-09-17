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
            _isPaused = !_isPaused;

            if (PauseMenuPanel != null)
            {
                PauseMenuPanel.SetActive(_isPaused);
            }

            if (_isPaused)
            {
                Time.timeScale = 0f; // Game ko freeze kar do
            }
            else
            {
                Time.timeScale = 1f; // Game wapas chala do
            }
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
