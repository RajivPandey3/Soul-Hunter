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

        /// <summary>
        /// PlayMode tests set this before booting the game: the player's save is copied there and every read and write
        /// uses the copy, so no test can change the real save (a stage lantern's gold coin once did). Null in the game.
        /// </summary>
        public static string TestSavePath;

        public void Initialize()
        {
            if (_filePath == null)
                _filePath = BenchmarkSavePath() ?? Path.Combine(Application.persistentDataPath, "soulhunter_save.json");
            if (!string.IsNullOrEmpty(TestSavePath) && TestSavePath != _filePath)
            {
                foreach (string file in new[] { TestSavePath, TestSavePath + ".prev", TestSavePath + ".tmp" })
                    if (File.Exists(file)) File.Delete(file);
                if (File.Exists(_filePath)) File.Copy(_filePath, TestSavePath);
                _filePath = TestSavePath;
            }
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
        /// <summary>Saves written since the game started (diagnostics: the performance benchmark counts them per segment).</summary>
        public static int SavesWritten { get; private set; }

        /// <summary>Unscaled time of the last save written (-1 before the first).</summary>
        public float LastSaveTime { get; private set; } = -1f;

        /// <summary>At most one coalesced save per this many seconds (SaveSoon).</summary>
        public const float SaveSoonSeconds = 2f;

        private bool _dirty;

        /// <summary>True while a SaveSoon request has not been written yet.</summary>
        public bool HasPendingSave => _dirty;

        /// <summary>
        /// Heavy-kill pass (2026-09-29): saves soon instead of now. Gold pickups saved the whole file on every coin, up to
        /// several times a second with a strong hero. Writes at once when the last save is SaveSoonSeconds old, otherwise
        /// marks the save pending; SaveFlusher writes it when that time has passed, and on quit. A crash can lose at most
        /// the last SaveSoonSeconds of such changes.
        /// </summary>
        public void SaveSoon()
        {
            if (CurrentData == null) return;
            if (LastSaveTime < 0f || Time.unscaledTime - LastSaveTime >= SaveSoonSeconds) { SaveGame(); return; }
            _dirty = true;
            SaveFlusher.Ensure(this);
        }

        /// <summary>Writes a pending SaveSoon now (quit, and when its time has come).</summary>
        public void FlushPendingSave()
        {
            if (_dirty) SaveGame();
        }

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
                SavesWritten++;
                _dirty = false;
                LastSaveTime = Time.unscaledTime;
#if UNITY_EDITOR
                Debug.Log($"[SaveService] Game Saved to: {_filePath}");
#endif
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
            if (data.SaveVersion < 2)
            {
                // v1 -> v2 (owner, 2026-09-30: "Refund all"): the old fixed-price shop is replaced by the VS PowerUps.
                // Like VS's Refund button, every old level's price comes back as gold and the VS shop starts at 0.
                int refund = System.Math.Max(0, data.MetaMightLevel) * PowerUps.OldMightPrice
                             + System.Math.Max(0, data.MetaArmorLevel) * PowerUps.OldArmorPrice
                             + System.Math.Max(0, data.MetaGreedLevel) * PowerUps.OldGreedPrice
                             + System.Math.Max(0, data.MetaRevivalLevel) * PowerUps.OldRevivalPrice
                             + System.Math.Max(0, data.MetaMaxHealthLevel) * PowerUps.OldMaxHealthPrice
                             + System.Math.Max(0, data.MetaRecoveryLevel) * PowerUps.OldRecoveryPrice;
                if (refund > 0) Debug.Log($"[SaveService] Old power-ups refunded for the VS PowerUp shop: +{refund} gold.");
                data.Currency = System.Math.Max(0, data.Currency) + refund;
                data.MetaMightLevel = data.MetaArmorLevel = data.MetaGreedLevel = data.MetaRevivalLevel = 0;
                data.MetaMaxHealthLevel = data.MetaRecoveryLevel = 0;
                data.SaveVersion = 2;
            }
            if (data.SaveVersion > GameData.CurrentVersion)
                Debug.LogWarning($"[SaveService] Save version {data.SaveVersion} is newer than this build ({GameData.CurrentVersion}).");

            data.UnlockedAchievements ??= new System.Collections.Generic.List<string>();
            data.UnlockedCharacters ??= new System.Collections.Generic.List<string>();
            data.OwnedCharacters ??= new System.Collections.Generic.List<string>();
            data.SoulEggs ??= new System.Collections.Generic.List<SoulEggRecord>();
            data.SoulEggs.RemoveAll(egg => egg == null || string.IsNullOrEmpty(egg.Character) || !AllFinite(egg));
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
            data.MetaMaxHealthLevel = Mathf.Max(0, data.MetaMaxHealthLevel);
            data.MetaRecoveryLevel = Mathf.Max(0, data.MetaRecoveryLevel);
            // VS PowerUps: known ids only, one entry each, ranks within the VS limits.
            data.PowerUpRanks ??= new System.Collections.Generic.List<PowerUpRank>();
            var seen = new System.Collections.Generic.HashSet<string>();
            data.PowerUpRanks.RemoveAll(r => r == null || !System.Enum.TryParse(r.Id, out PowerUpId _) || !seen.Add(r.Id));
            foreach (var r in data.PowerUpRanks)
                r.Rank = Mathf.Clamp(r.Rank, 0, PowerUps.Info((PowerUpId)System.Enum.Parse(typeof(PowerUpId), r.Id)).MaxRank);
            data.PowerUpRanks.RemoveAll(r => r.Rank == 0);
            // VS locked PowerUps (2026-10-01): ranks bought before a PowerUp was locked are refunded (VS Refund).
            int lockedRefund = PowerUpUnlocks.RefundIfLockedRanksOwned(data);
            if (lockedRefund > 0) Debug.Log($"[SaveService] PowerUps with locked ranks refunded: +{lockedRefund} gold.");
            data.ChestsOpened = Mathf.Max(0, data.ChestsOpened);
            data.BigCoinBagsReceived = Mathf.Max(0, data.BigCoinBagsReceived);
            data.FiveItemChestsOpened = Mathf.Max(0, data.FiveItemChestsOpened);
            // Run mode toggles (ModeHurry ... ModeLimitBreak) need no step: a save without them loads them as off, which
            // is the VS default, and a bool cannot hold an impossible value. The schema version stays at 1.
            if (data.WindowMode < 0 || data.WindowMode > 3) data.WindowMode = 0;
            if (data.ResolutionIndex < -1) data.ResolutionIndex = -1;
        }

        private static float Volume(float value) => float.IsFinite(value) ? Mathf.Clamp01(value) : 1f;

        private static bool AllFinite(SoulEggRecord egg)
        {
            foreach (float value in new[] { egg.MaxHealth, egg.Recovery, egg.Armor, egg.Amount, egg.Revival, egg.Reroll,
                         egg.Skip, egg.Banish, egg.MoveSpeed, egg.Might, egg.Speed, egg.Duration, egg.Area, egg.Luck,
                         egg.Growth, egg.Greed, egg.Curse, egg.Magnet, egg.Cooldown })
                if (!float.IsFinite(value)) return false;
            return egg.Eggs >= 0;
        }
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
