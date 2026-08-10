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

            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.OnGameOver += ShowGameOverScreen;
            }
        }

        private void OnDestroy()
        {
            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(RestartGame);
            }

            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.OnGameOver -= ShowGameOverScreen;
            }
        }

        private void ShowGameOverScreen()
        {
            Time.timeScale = 0f; // Pause the game
            
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
            }

            if (_survivalTimeText != null && GameSessionManager.Instance != null)
            {
                _survivalTimeText.text = "You Survived For:\n" + GameSessionManager.Instance.GetFormattedTime();
            }
        }

        private void RestartGame()
        {
            Time.timeScale = 1f;
            // Scene reload karo (Bootstrap scene ka naam ya index daalein, yahan index 0 maante hain)
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }
}
