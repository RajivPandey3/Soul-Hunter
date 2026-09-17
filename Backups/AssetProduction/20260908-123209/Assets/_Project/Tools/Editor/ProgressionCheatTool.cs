using UnityEngine;
using UnityEditor;
using SoulHunter.Core.Services;

namespace SoulHunter.EditorSetup
{
    public class ProgressionCheatTool : EditorWindow
    {
        public static void ShowWindow()
        {
            GetWindow<ProgressionCheatTool>("Cheat Engine").Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Progression Testing Cheats", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Wipe All Save Data (Reset Game)", GUILayout.Height(30)))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("[Cheat Engine] 🧹 Save Data wiped clean. Game is reset to Day 1.");
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Grant 10,000 Gold", GUILayout.Height(30)))
            {
                if (Application.isPlaying && GameServices.Instance != null)
                {
                    var economy = GameServices.Instance.Get<EconomyService>();
                    if (economy != null)
                    {
                        economy.AddGold(10000);
                        Debug.Log($"[Cheat Engine] 💰 Granted 10,000 Gold! Total: {economy.CurrentGold}");
                    }
                }
                else
                {
                    Debug.LogWarning("You must be in Play Mode to add Gold to the active session!");
                }
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Force Level 100 (Max Level)", GUILayout.Height(30)))
            {
                if (Application.isPlaying)
                {
                    var expManager = FindFirstObjectByType<SoulHunter.Gameplay.Player.PlayerExperience>();
                    if (expManager != null)
                    {
                        expManager.AddXP(500000); // Add a massive amount of XP to force levels
                        Debug.Log("[Cheat Engine] 🚀 Forced Level Up! Open UI to claim upgrades.");
                    }
                }
                else
                {
                    Debug.LogWarning("You must be in Play Mode to force level ups!");
                }
            }
        }
    }
}
