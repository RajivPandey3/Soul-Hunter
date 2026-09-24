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
            // Learning Comment: Ensure EventSystem exists taake Restart button mouse clicks receive kar sake
            EnsureEventSystem();

            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(RestartGame);
                _restartButton.onClick.AddListener(RestartGame);
            }

            _session = GameSessionManager.Instance;
            if (_session != null)
            {
                _session.OnGameOver -= ShowGameOverScreen;
                _session.OnGameOver += ShowGameOverScreen;
                _session.OnVictory -= ShowVictoryScreen;
                _session.OnVictory += ShowVictoryScreen;
                // Learning Comment: Jab scene naya load hota hai toh Game Over panel auto-show nahi hona chahiye.
                // Game Over tabhi trigger hoga jab player waqai mare aur OnGameOver event fire ho.
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
                finalStatsText += $"<color=yellow>Gold Earned: {RunStatsTracker.Instance.TotalGoldCollected}</color>\n";
                var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
                var experience = player != null ? player.GetComponent<SoulHunter.Gameplay.Player.PlayerExperience>() : null;
                if (experience != null) finalStatsText += $"Level Reached: {experience.CurrentLevel}\n";
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
            // Learning Comment: Game over ke waqt Time.timeScale = 0 hota hai.
            // Scene reload se pehle timeScale ko wapas 1 karna zaroori hai taake physics aur animations unpause ho jayein.
            Time.timeScale = 1f;
            Debug.Log("[GameOverUI] Restart button clicked! Reloading game session...");

            // Learning Comment: Naye run se pehle session state aur run stats ko reset karna laazmi hai
            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.ResetSession();
            }
            if (RunStatsTracker.Instance != null)
            {
                RunStatsTracker.Instance.ResetStats();
            }

            // Learning Comment: "Bootstrap" load karne se Bootstrap -> Loading (3s wait) -> MainMenu khul jata tha.
            // Restart button ko seedha "Main_Gameplay" reload karna chahiye taake player bina delay ke dubara match khel sake.
            try
            {
                var scenes = SoulHunter.Core.Services.GameServices.Instance?.Get<SoulHunter.Core.Scenes.SceneService>();
                if (scenes != null)
                {
                    scenes.LoadSceneAsync("Main_Gameplay");
                    return;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[GameOverUI] GameServices SceneService unavailable: " + ex.Message);
            }

            // Fallback: Direct Unity SceneManager ke zariye "Main_Gameplay" reload karo
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main_Gameplay");
        }

        private void EnsureEventSystem()
        {
            // Learning Comment: Unity UI mein buttons click tabhi hote hain jab scene mein EventSystem ho.
            // Agar Main_Gameplay scene mein EventSystem missing ho toh yeh method usay auto-create kar deta hai.
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var existing = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
                if (existing == null)
                {
                    var eventSystemGO = new GameObject("EventSystem_AutoGenerated");
                    eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();

                    // Unity Input System module dynamically attach karo
                    var inputType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                    if (inputType != null)
                    {
                        eventSystemGO.AddComponent(inputType);
                    }
                    else
                    {
                        eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    }

                    Debug.Log("[GameOverUI] Auto-created missing EventSystem with InputModule.");
                }
            }
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
                _restartButton.transform.SetAsLastSibling(); // Ensure button is in front of panel backdrop
                _restartButton.gameObject.SetActive(true);
                _restartButton.interactable = true;
            }
        }
    }
}
