using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SoulHunter.Core.Persistence;
using UnityEngine;
using UnityEngine.TestTools;

namespace SoulHunter.Tests.EditMode
{
    public class SaveServiceTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void Initialize_InvalidJson_PreservesOriginalBytesBeforeSavingFreshData(bool existingBackup)
        {
            string directory = Path.Combine(Application.temporaryCachePath,
                nameof(SaveServiceTests), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string path = Path.Combine(directory, "save.json");
                const string invalidJson = "this is not JSON";
                // Exercise Unity's actual parser, not a serializer stub.
                Assert.Throws<ArgumentException>(() =>
                    JsonUtility.FromJsonOverwrite(invalidJson, new GameData()));
                File.WriteAllText(path, invalidJson);
                byte[] originalBytes = File.ReadAllBytes(path);
                if (existingBackup)
                    File.WriteAllText(path + ".bak", "previous backup");

                LogAssert.Expect(LogType.Error,
                    new Regex(@"\[SaveService\] Save file corrupted or unreadable\. Creating fresh GameData\. Exception:"));
                var service = new SaveService(path);
                service.Initialize();

                string backupPath = path + (existingBackup ? ".1.bak" : ".bak");
                Assert.That(File.Exists(backupPath), Is.True);
                Assert.That(File.ReadAllBytes(backupPath), Is.EqualTo(originalBytes));
                if (existingBackup)
                    Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo("previous backup"));
                var saved = new GameData();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(path), saved);
                Assert.That(saved.SettingsMigrated, Is.True);
                Assert.That(service.CurrentData.SettingsMigrated, Is.True);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void BackgroundWrites_LandInOrder_OnTheFileTheyWereQueuedFor()
        {
            // 2026-10-09: a late-run profile showed the timed save taking 110 ms on the main thread; the timed write now
            // runs on a worker. The newest data must win, and a redirect must not move older data to the new file.
            string directory = NewDirectory();
            try
            {
                string path = Path.Combine(directory, "save.json"), other = Path.Combine(directory, "other.json");
                var service = Loaded(path);
                for (int gold = 1; gold <= 20; gold++)
                {
                    service.CurrentData.Currency = gold;
                    service.SaveSoon();
                    service.FlushPendingSaveInBackground();
                }
                service.WaitForBackgroundWrite();
                var saved = new GameData();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(path), saved);
                Assert.That(saved.Currency, Is.EqualTo(service.CurrentData.Currency), "The last queued data is on disk.");
                Assert.That(File.Exists(path + ".tmp"), Is.False, "No temp file is left behind.");

                service.CurrentData.Currency = 777;
                service.FlushPendingSaveInBackground(); // nothing pending: no write
                service.CurrentData.Currency = 778;
                service.SaveSoon();
                service.UseFile(other); // waits for the queued write, which stays on the old file
                var old = new GameData();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(path), old);
                Assert.That(File.Exists(other), Is.False, "The redirect wrote nothing by itself.");
                service.SaveGame();
                var moved = new GameData();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(other), moved);
                Assert.That(moved.Currency, Is.EqualTo(778), "Later saves go to the new file.");
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void SaveInBackground_WritesAtOnce_AtomicallyKeepingThePreviousSave()
        {
            // 2026-10-16: the chest-close SaveGame spent 62-363 ms writing the file on the main thread. Saves made during a
            // run now queue the write at once on a worker; the file is the same atomic save with its ".prev".
            string directory = NewDirectory();
            try
            {
                string path = Path.Combine(directory, "save.json");
                var service = Loaded(path);
                service.CurrentData.Currency = 5;
                service.SaveGame();
                service.CurrentData.Currency = 9;
                service.SaveInBackground();
                Assert.That(service.HasPendingSave, Is.False, "Nothing waits for the timed flush.");
                service.WaitForBackgroundWrite();
                var saved = new GameData();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(path), saved);
                Assert.That(saved.Currency, Is.EqualTo(9));
                var previous = new GameData();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(path + ".prev"), previous);
                Assert.That(previous.Currency, Is.EqualTo(5), "The last good save is kept.");
                Assert.That(File.Exists(path + ".tmp"), Is.False);
            }
            finally { Directory.Delete(directory, true); }
        }

        // Learning Comment: Save kabhi aadha na likha jaye, aur kharab save par pichla achha save wapas mile.
        private static string NewDirectory()
        {
            string directory = Path.Combine(Application.temporaryCachePath, nameof(SaveServiceTests), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static SaveService Loaded(string path)
        {
            var service = new SaveService(path);
            service.Initialize();
            return service;
        }

        private static void WriteSave(string path, int gold, int version = GameData.CurrentVersion)
        {
            var data = new GameData { Currency = gold, SettingsMigrated = true, SaveVersion = version };
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
        }

        [Test]
        public void Save_IsAtomic_AndKeepsThePreviousSave()
        {
            string directory = NewDirectory();
            try
            {
                string path = Path.Combine(directory, "save.json");
                WriteSave(path, 100);
                var service = Loaded(path);
                service.CurrentData.Currency = 250;
                Assert.That(service.SaveGame(), Is.True);

                Assert.That(File.Exists(path + ".tmp"), Is.False, "No temp file is left behind.");
                Assert.That(Loaded(path).CurrentData.Currency, Is.EqualTo(250));
                var previous = new GameData();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(path + ".prev"), previous);
                Assert.That(previous.Currency, Is.EqualTo(100), "The previous save is kept as .prev.");
            }
            finally { Directory.Delete(directory, true); }
        }

        [TestCase("{\"Currency\": 999, \"Selected")] // cut off mid-write
        [TestCase("")]                               // empty file
        [TestCase("\0\0\0\0\0\0\0\0")]               // zero-filled after a power loss
        public void DamagedSave_IsBackedUp_AndThePreviousSaveIsRestored(string damaged)
        {
            string directory = NewDirectory();
            try
            {
                string path = Path.Combine(directory, "save.json");
                WriteSave(path + ".prev", 400);
                File.WriteAllText(path, damaged);
                byte[] damagedBytes = File.ReadAllBytes(path);

                LogAssert.Expect(LogType.Error, new Regex(@"\[SaveService\] Save file corrupted or unreadable\. Restored the previous save\."));
                var service = Loaded(path);

                Assert.That(service.CurrentData.Currency, Is.EqualTo(400), "Progress comes back from the previous save.");
                Assert.That(File.ReadAllBytes(path + ".bak"), Is.EqualTo(damagedBytes), "The damaged file is preserved.");
                Assert.That(service.SaveGame(), Is.True);
                var previous = new GameData();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(path + ".prev"), previous);
                Assert.That(previous.Currency, Is.EqualTo(400), "Saving again does not rotate the damaged file into .prev.");
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void DamagedSave_WithoutAPreviousSave_StartsFresh()
        {
            string directory = NewDirectory();
            try
            {
                string path = Path.Combine(directory, "save.json");
                File.WriteAllText(path, "");
                LogAssert.Expect(LogType.Error, new Regex(@"\[SaveService\] Save file corrupted or unreadable\. Creating fresh GameData\."));
                var service = Loaded(path);
                Assert.That(service.CurrentData.Currency, Is.EqualTo(0));
                Assert.That(service.CurrentData.SaveVersion, Is.EqualTo(GameData.CurrentVersion));
                Assert.That(File.Exists(path + ".bak"), Is.True);
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void MissingSave_WithAPreviousSave_IsRestored()
        {
            string directory = NewDirectory();
            try
            {
                string path = Path.Combine(directory, "save.json");
                WriteSave(path + ".prev", 70); // a crash between writing and replacing
                LogAssert.Expect(LogType.Warning, new Regex(@"Save file missing; restored the previous save"));
                Assert.That(Loaded(path).CurrentData.Currency, Is.EqualTo(70));
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void LegacySave_IsMigrated_ToTheCurrentVersion()
        {
            string directory = NewDirectory();
            try
            {
                string path = Path.Combine(directory, "save.json");
                // A save written before versioning: no SaveVersion field, and the old character name.
                File.WriteAllText(path, "{\"Currency\": 55, \"SettingsMigrated\": true, \"SelectedCharacterName\": \"Antonio\", " +
                                        "\"UnlockedCharacters\": [\"Antonio\", \"Corvan\"], \"OwnedCharacters\": [\"Kael\", \"Antonio\"]}");
                var data = Loaded(path).CurrentData;
                Assert.That(data.SaveVersion, Is.EqualTo(GameData.CurrentVersion));
                Assert.That(data.Currency, Is.EqualTo(55));
                Assert.That(data.SelectedCharacterName, Is.EqualTo("Corvan"));
                Assert.That(data.UnlockedCharacters, Is.EqualTo(new[] { "Corvan" }));
                Assert.That(data.OwnedCharacters, Is.EqualTo(new[] { "Kael", "Corvan" }));
                Assert.That(data.MusicVolume, Is.EqualTo(1f), "Fields missing from an old save keep their defaults.");
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void Migrate_RepairsImpossibleValues()
        {
            var data = new GameData
            {
                SaveVersion = GameData.CurrentVersion, Currency = -5, MasterVolume = float.NaN, MusicVolume = 3f, SfxVolume = -1f,
                HighScoreTime = float.PositiveInfinity, MetaMightLevel = -2, WindowMode = 9, ResolutionIndex = -7,
                SelectedCharacterName = "", OwnedCharacters = null, UnlockedCharacters = null, InputBindingOverrides = null
            };
            SaveService.Migrate(data);
            Assert.That(data.Currency, Is.EqualTo(0));
            Assert.That(data.MasterVolume, Is.EqualTo(1f));
            Assert.That(data.MusicVolume, Is.EqualTo(1f));
            Assert.That(data.SfxVolume, Is.EqualTo(0f));
            Assert.That(data.HighScoreTime, Is.EqualTo(0f));
            Assert.That(data.MetaMightLevel, Is.EqualTo(0));
            Assert.That(data.WindowMode, Is.EqualTo(0));
            Assert.That(data.ResolutionIndex, Is.EqualTo(-1));
            Assert.That(data.SelectedCharacterName, Is.EqualTo("Kael"));
            Assert.That(data.OwnedCharacters, Is.Not.Null);
            Assert.That(data.UnlockedCharacters, Is.Not.Null);
            Assert.That(data.InputBindingOverrides, Is.EqualTo(""));
        }

        [Test]
        public void FailedWrite_KeepsTheLastGoodSave_AndDoesNotThrow()
        {
            string directory = NewDirectory();
            try
            {
                string path = Path.Combine(directory, "save.json");
                WriteSave(path, 30);
                var service = Loaded(path);
                service.CurrentData.Currency = 31;
                bool saved;
                using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None)) // e.g. locked by antivirus
                {
                    LogAssert.Expect(LogType.Error, new Regex(@"\[SaveService\] Could not write the save; the last good save is kept\."));
                    saved = service.SaveGame();
                }
                Assert.That(saved, Is.False);
                Assert.That(Loaded(path).CurrentData.Currency, Is.EqualTo(30));
                Assert.That(File.Exists(path + ".tmp"), Is.False);
            }
            finally { Directory.Delete(directory, true); }
        }
    }
}
