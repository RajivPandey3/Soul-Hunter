using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SoulHunter.Gameplay.Core;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Learning Comment:
    /// Jab player marta hai, toh ye UI dikhti hai aur game ko pause kar deti hai.
    /// Ye aapko aapka final survival time batati hai.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private TextMeshProUGUI _survivalTimeText;
        [SerializeField] private Button _restartButton;
        private GameSessionManager _session;

        private void Start()
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(RestartGame);
            }

            _session = GameSessionManager.Instance;
            if (_session != null)
            {
                _session.OnGameOver += ShowGameOverScreen;
            }
        }

        private void OnDestroy()
        {
            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(RestartGame);
            }

            if (_session != null)
            {
                _session.OnGameOver -= ShowGameOverScreen;
            }
            Time.timeScale = 1f;
        }

        private void ShowGameOverScreen()
        {
            Time.timeScale = 0f; // Pause the game

            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
            }

            string finalStatsText = "";

            // Survival Time
            if (GameSessionManager.Instance != null)
            {
                finalStatsText += $"<color=yellow>You Survived For: {GameSessionManager.Instance.GetFormattedTime()}</color>\n\n";
            }

            // Kills aur Damage Report
            if (RunStatsTracker.Instance != null)
            {
                finalStatsText += $"<color=red>Total Kills: {RunStatsTracker.Instance.TotalKills}</color>\n";
                finalStatsText += "------------------------\n";

                var damageDict = RunStatsTracker.Instance.GetWeaponStats();
                if (damageDict.Count > 0)
                {
                    finalStatsText += "Weapon Damage Output:\n";
                    foreach (var kvp in damageDict)
                    {
                        finalStatsText += $"- {kvp.Key}: {kvp.Value} dmg\n";
                    }
                }
                else
                {
                    finalStatsText += "No damage dealt!\n";
                }
            }

            if (_survivalTimeText != null)
            {
                _survivalTimeText.text = finalStatsText;
            }
        }

        private void RestartGame()
        {
            Time.timeScale = 1f;
            // Scene reload karo (Bootstrap scene ka naam ya index daalein, yahan index 0 maante hain)
            var scenes = SoulHunter.Core.Services.GameServices.Instance?.Get<SoulHunter.Core.Scenes.SceneService>();
            if (scenes != null) scenes.LoadSceneAsync("Bootstrap");
            else Debug.LogError("[GameOverUI] SceneService is unavailable.");
        }
    }
}
