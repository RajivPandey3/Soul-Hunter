using UnityEngine;
using System.IO;
using SoulHunter.Core.Services;

namespace SoulHunter.Core.Persistence
{
    /// <summary>
    /// Learning Comment:
    /// Core engine service for disk I/O. Decoupled from gameplay.
    /// GameServices isay manage karta hai, aur game ise kisi bhi waqt Get<SaveService>() karke bula sakta hai.
    /// </summary>
    public class SaveService : IGameService
    {
        private string _filePath;
        public GameData CurrentData { get; private set; }

        public void Initialize()
        {
            _filePath = Path.Combine(Application.persistentDataPath, "soulhunter_save.json");
            LoadGame();
            Debug.Log("[SaveService] Initialized.");
        }

        public void SaveGame()
        {
            if (CurrentData == null) return;

            string json = JsonUtility.ToJson(CurrentData, true);
            File.WriteAllText(_filePath, json);
            Debug.Log($"[SaveService] Game Saved to: {_filePath}");
        }

        public void LoadGame()
        {
            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                CurrentData = JsonUtility.FromJson<GameData>(json);
                Debug.Log("[SaveService] Game Loaded successfully.");
            }
            else
            {
                // Create fresh data if no save exists
                CurrentData = new GameData();
                Debug.Log("[SaveService] No save file found. Created fresh GameData.");
            }
        }
    }
}
