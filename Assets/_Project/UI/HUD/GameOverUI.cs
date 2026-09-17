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
                _session.OnVictory += ShowVictoryScreen;
                if (_session.IsRunFinished)
                {
                    if (_session.IsVictory) ShowVictoryScreen();
                    else ShowGameOverScreen();
                }
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
                _session.OnVictory -= ShowVictoryScreen;
            }
            Time.timeScale = 1f;
        }

        private void ShowVictoryScreen()
        {
            ShowGameOverScreen();
            if (_gameOverPanel != null)
                foreach (var label in _gameOverPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
                    if (label != _survivalTimeText && label.text.Trim().ToUpperInvariant().Contains("GAME OVER"))
                        label.text = "PACT COMPLETE";
            if (_survivalTimeText != null)
                _survivalTimeText.text = "<b>ALL TEN STAGES SURVIVED</b>\nThe pact has reached its end.\n\n" + _survivalTimeText.text;
        }

        private void ShowGameOverScreen()
        {
            Time.timeScale = 0f; // Pause the game

            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
                LayoutResults();
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

        private void LayoutResults()
        {
            var panelRect = _gameOverPanel.GetComponent<RectTransform>();
            if (panelRect == null) return;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.localScale = Vector3.one;
            panelRect.SetAsLastSibling();
            var backdrop = _gameOverPanel.GetComponent<Image>();
            if (backdrop == null) backdrop = _gameOverPanel.AddComponent<Image>();
            backdrop.color = new Color(0.025f, 0.035f, 0.055f, 0.97f);
            backdrop.raycastTarget = true;
            if (_survivalTimeText != null)
            {
                var rect = _survivalTimeText.rectTransform;
                rect.anchorMin = new Vector2(0.15f, 0.24f);
                rect.anchorMax = new Vector2(0.85f, 0.85f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                _survivalTimeText.enableAutoSizing = true;
                _survivalTimeText.fontSizeMin = 14;
                _survivalTimeText.fontSizeMax = 28;
                _survivalTimeText.alignment = TextAlignmentOptions.Center;
                _survivalTimeText.overflowMode = TextOverflowModes.Truncate;
            }
            if (_restartButton != null)
            {
                var rect = _restartButton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.35f, 0.09f);
                rect.anchorMax = new Vector2(0.65f, 0.18f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
        }
    }
}
