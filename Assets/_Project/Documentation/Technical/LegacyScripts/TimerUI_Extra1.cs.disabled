using UnityEngine;
using TMPro;
using SoulHunter.Gameplay.Core;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>Displays time only when the session publishes a new second.</summary>
    public class TimerUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _timeText;
        private GameSessionManager _session;
        private void Start()
        {
            _session = GameSessionManager.Instance;
            if (_session == null) return;
            _session.OnSurvivalSecondChanged += UpdateTime;
            UpdateTime(Mathf.FloorToInt(_session.SurvivalTime));
        }
        private void OnDestroy()
        {
            if (_session != null) _session.OnSurvivalSecondChanged -= UpdateTime;
        }
        private void UpdateTime(int seconds)
        {
            if (_timeText != null) _timeText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }
    }
}
