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
                _filePath = BenchmarkSavePath() ?? Path.Combine(Application.persistentDataPath, "soulhunter_save.json");
            LoadGame();
            MigrateSettings();
            Debug.Log("[SaveService] Initialized.");
        }

        /// <summary>
        /// A performance benchmark (-sh-benchmark) or autoplay (-sh-autoplay) run plays with a fresh throw-away save,
        /// so it neither reads nor changes the player's progress.
        /// </summary>
        private static string BenchmarkSavePath()
        {
            var args = System.Environment.GetCommandLineArgs();
            bool benchmark = System.Array.IndexOf(args, "-sh-benchmark") >= 0;
            if (!benchmark && System.Array.IndexOf(args, "-sh-autoplay") < 0) return null; // AutoplayBot plays on a throw-away save too
            string path = Path.Combine(Application.temporaryCachePath, benchmark ? "benchmark_save.json" : "autoplay_save.json");
            foreach (string file in new[] { path, path + ".prev", path + ".tmp" })
                if (File.Exists(file)) File.Delete(file);
            return path;
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

        /// <summary>
        /// Sends every later save to another file (the current data is kept in memory, nothing is written now).
        /// Tools that play the game (AutoplayBot) use it so the player's real progress is never changed.
        /// </summary>
        public void UseFile(string path)
        {
            if (!string.IsNullOrEmpty(path)) _filePath = path;
        }

        public string FilePath => _filePath;

        /// <summary>The previous good save, kept by every successful save (".prev").</summary>
        public string PreviousPath => _filePath + ".prev";
        private string TempPath => _filePath + ".tmp";

        /// <summary>
        /// Writes the save atomically: the JSON goes to a temp file first, then replaces the save while the old
        /// one is kept as ".prev". A crash or full disk mid-write leaves the last good save untouched.
        /// Returns false (and logs) if the save could not be written; the game keeps running.
        /// </summary>
        public bool SaveGame()
        {
            if (CurrentData == null) return false;

            try
            {
                if (CurrentData.SaveVersion < GameData.CurrentVersion) CurrentData.SaveVersion = GameData.CurrentVersion;
                string json = JsonUtility.ToJson(CurrentData, true);
                File.WriteAllText(TempPath, json);
                if (File.Exists(_filePath)) File.Replace(TempPath, _filePath, PreviousPath, true);
                else File.Move(TempPath, _filePath);
                Debug.Log($"[SaveService] Game Saved to: {_filePath}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveService] Could not write the save; the last good save is kept. Exception: {e.Message}");
                try { if (File.Exists(TempPath)) File.Delete(TempPath); } catch (System.Exception) { }
                return false;
            }
        }

        public void LoadGame()
        {
            if (!File.Exists(_filePath))
            {
                // A crash between writing and replacing can leave only the previous save behind.
                if (File.Exists(PreviousPath) && TryRead(PreviousPath, out var previous, out _))
                {
                    CurrentData = previous;
                    Debug.LogWarning("[SaveService] Save file missing; restored the previous save.");
                }
                else
                {
                    CurrentData = NewData();
                    Debug.Log("[SaveService] No save file found. Created fresh GameData.");
                }
            }
            else if (TryRead(_filePath, out var data, out string error))
            {
                CurrentData = data;
                Debug.Log("[SaveService] Game Loaded successfully.");
            }
            else
            {
                // Fail closed if backup fails: neither migration nor a later save may
                // overwrite the unreadable file until its contents are preserved.
                CurrentData = null;
                string backupPath = _filePath + ".bak";
                int suffix = 1;
                while (File.Exists(backupPath))
                    backupPath = _filePath + "." + suffix++ + ".bak";
                File.Copy(_filePath, backupPath, false);
                // Preserved; remove it so the next save does not rotate the damaged file into ".prev".
                File.Delete(_filePath);
                if (File.Exists(PreviousPath) && TryRead(PreviousPath, out var previous, out _))
                {
                    CurrentData = previous;
                    Debug.LogError($"[SaveService] Save file corrupted or unreadable. Restored the previous save. Exception: {error}");
                }
                else
                {
                    Debug.LogError($"[SaveService] Save file corrupted or unreadable. Creating fresh GameData. Exception: {error}");
                    CurrentData = NewData();
                }
            }
            Migrate(CurrentData);
        }

        private static GameData NewData() => new GameData { SaveVersion = GameData.CurrentVersion };

        private static bool TryRead(string path, out GameData data, out string error)
        {
            data = null;
            error = null;
            try
            {
                string json = File.ReadAllText(path);
                // Unity's parser accepts an empty or zero-filled file (a typical power-loss result) as "no fields".
                if (string.IsNullOrWhiteSpace(json.Replace('\0', ' ')) || json.TrimStart()[0] != '{')
                    throw new System.FormatException("The save is empty or not a JSON object.");
                data = new GameData();
                JsonUtility.FromJsonOverwrite(json, data);
                return true;
            }
            catch (System.Exception e)
            {
                error = e.Message;
                data = null;
                return false;
            }
        }

        /// <summary>
        /// Brings a loaded save up to GameData.CurrentVersion and repairs impossible values, so an old, hand-edited
        /// or partly damaged save still plays. A save from a newer build is left at its version.
        /// </summary>
        public static void Migrate(GameData data)
        {
            if (data == null) return;
            if (data.SaveVersion < 1)
            {
                // v0 -> v1: Antonio was renamed Corvan (2026-09-25).
                data.SelectedCharacterName = Renamed(data.SelectedCharacterName);
                RenameAll(data.UnlockedCharacters);
                RenameAll(data.OwnedCharacters);
                data.SaveVersion = 1;
            }
            if (data.SaveVersion > GameData.CurrentVersion)
                Debug.LogWarning($"[SaveService] Save version {data.SaveVersion} is newer than this build ({GameData.CurrentVersion}).");

            data.UnlockedAchievements ??= new System.Collections.Generic.List<string>();
            data.UnlockedCharacters ??= new System.Collections.Generic.List<string>();
            data.OwnedCharacters ??= new System.Collections.Generic.List<string>();
            data.InputBindingOverrides ??= "";
            if (string.IsNullOrEmpty(data.SelectedCharacterName)) data.SelectedCharacterName = "Kael";
            data.Currency = Mathf.Max(0, data.Currency);
            data.MasterVolume = Volume(data.MasterVolume);
            data.MusicVolume = Volume(data.MusicVolume);
            data.SfxVolume = Volume(data.SfxVolume);
            data.HighScoreTime = float.IsFinite(data.HighScoreTime) ? Mathf.Max(0f, data.HighScoreTime) : 0f;
            data.MetaMightLevel = Mathf.Max(0, data.MetaMightLevel);
            data.MetaArmorLevel = Mathf.Max(0, data.MetaArmorLevel);
            data.MetaGreedLevel = Mathf.Max(0, data.MetaGreedLevel);
            data.MetaRevivalLevel = Mathf.Max(0, data.MetaRevivalLevel);
            if (data.WindowMode < 0 || data.WindowMode > 3) data.WindowMode = 0;
            if (data.ResolutionIndex < -1) data.ResolutionIndex = -1;
        }

        private static float Volume(float value) => float.IsFinite(value) ? Mathf.Clamp01(value) : 1f;
        private static string Renamed(string name) => name == "Antonio" ? "Corvan" : name;

        private static void RenameAll(System.Collections.Generic.List<string> names)
        {
            if (names == null) return;
            for (int i = 0; i < names.Count; i++) names[i] = Renamed(names[i]);
            for (int i = names.Count - 1; i >= 0; i--)
                if (names.IndexOf(names[i]) != i) names.RemoveAt(i);
        }
    }
}
