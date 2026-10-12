using UnityEngine;
using TMPro;
using SoulHunter.Gameplay.Core;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Shows the stage clock. Bug MIS v2 C02 (2026-10-02): it showed the run's total survival time, so Stage 2 opened at
    /// "30:06" while its waves, elites and boss run on its own 0:00-30:00 clock (vampire.survivors.wiki/w/Stages: each
    /// stage has a 30-minute limit, and its events follow that clock). The text changes only when the second changes.
    /// </summary>
    public class TimerUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _timeText;
        public TextMeshProUGUI TimeText => _timeText;
        private int _shown = -1;
        private bool _short;

        /// <summary>The phone's HUD strip shows the clock alone ("00:35"); a PC keeps "Time: 00:35".</summary>
        public bool Short
        {
            get => _short;
            set { if (_short == value) return; _short = value; _shown = -1; Update(); }
        }

        /// <summary>The seconds the label shows (tests).</summary>
        public int ShownSeconds => _shown;

        private void Update()
        {
            int seconds = Mathf.FloorToInt(CurrentSeconds());
            if (seconds == _shown) return;
            _shown = seconds;
            if (_timeText != null) _timeText.text = _short ? $"{seconds / 60:00}:{seconds % 60:00}" : $"Time: {seconds / 60:00}:{seconds % 60:00}";
        }

        private static float CurrentSeconds()
        {
            var progression = LevelProgressionManager.Instance;
            if (progression != null) return progression.StageElapsedTime;
            var session = GameSessionManager.Instance;
            return session != null ? session.SurvivalTime : 0f;
        }
    }
}
