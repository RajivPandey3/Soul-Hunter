using UnityEngine;
using UnityEngine.UI;

namespace SoulHunter.UI
{
    public sealed class MainMenuNavigation : MonoBehaviour
    {
        public GameObject HomePanel;
        public GameObject CharacterPanel;
        public GameObject PowerUpPanel;
        public Button StartButton, PowerUpButton, CharacterBackButton, PowerUpBackButton, QuitButton;
        private void Start()
        {
            StartButton.onClick.AddListener(ShowCharacters);
            PowerUpButton.onClick.AddListener(ShowPowerUps);
            CharacterBackButton.onClick.AddListener(ShowHome);
            PowerUpBackButton.onClick.AddListener(ShowHome);
            QuitButton.onClick.AddListener(Quit);
            ShowHome();
        }
        public void ShowHome() { HomePanel.SetActive(true); CharacterPanel.SetActive(false); PowerUpPanel.SetActive(false); }
        public void ShowCharacters() { HomePanel.SetActive(false); CharacterPanel.SetActive(true); PowerUpPanel.SetActive(false); }
        public void ShowPowerUps() { HomePanel.SetActive(false); CharacterPanel.SetActive(false); PowerUpPanel.SetActive(true); }
        private void Quit()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}
