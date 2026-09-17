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

        public SaveService() { }

        public SaveService(string filePath)
        {
            _filePath = filePath;
        }

        public void Initialize()
        {
            if (_filePath == null)
                _filePath = Path.Combine(Application.persistentDataPath, "soulhunter_save.json");
            LoadGame();
            MigrateSettings();
            Debug.Log("[SaveService] Initialized.");
        }

        private void MigrateSettings()
        {
            if (CurrentData == null) CurrentData = new GameData();
            if (CurrentData.SettingsMigrated) return;
            // Read legacy keys once; never delete the player's existing data.
            CurrentData.MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("MasterVolume", 1f));
            CurrentData.HighScoreTime = Mathf.Max(0f, PlayerPrefs.GetFloat("HighScoreTime", 0f));
            CurrentData.SettingsMigrated = true;
            SaveGame();
        }

        public void SetMasterVolume(float volume)
        {
            CurrentData.MasterVolume = Mathf.Clamp01(volume);
            SaveGame();
        }

        public void RecordHighScore(float survivalTime)
        {
            if (survivalTime <= CurrentData.HighScoreTime) return;
            CurrentData.HighScoreTime = survivalTime;
            SaveGame();
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
                try
                {
                    string json = File.ReadAllText(_filePath);
                    CurrentData = new GameData();
                    JsonUtility.FromJsonOverwrite(json, CurrentData);
                    Debug.Log("[SaveService] Game Loaded successfully.");
                }
                catch (System.Exception e)
                {
                    // Fail closed if backup fails: neither migration nor a later save may
                    // overwrite the unreadable file until its contents are preserved.
                    CurrentData = null;
                    string backupPath = _filePath + ".bak";
                    int suffix = 1;
                    while (File.Exists(backupPath))
                        backupPath = _filePath + "." + suffix++ + ".bak";
                    File.Copy(_filePath, backupPath, false);
                    Debug.LogError($"[SaveService] Save file corrupted or unreadable. Creating fresh GameData. Exception: {e.Message}");
                    CurrentData = new GameData();
                }
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
