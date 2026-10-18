using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SoulHunter.Core.Persistence;
using UnityEngine;
using UnityEngine.TestTools;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// VS Options, Account section (vampire.survivors.wiki/w/Options, raw, fetched 2026-10-12): "DATA RECOVERY ... Restore"
    /// and "Delete Save". SaveService.RestorePrevious and DeleteSave on throw-away files only (never the real save):
    /// - the previous good save (".prev") is described without being changed, and restored on request, with the save it
    ///   replaces kept as ".replaced.bak";
    /// - with no previous save, or an unreadable one, nothing changes;
    /// - Delete Save keeps a ".deleted.bak" copy (numbered when one exists), removes the progress and the previous save,
    ///   keeps the device's options and writes a fresh save.
    /// </summary>
    public class SaveDataRecoveryTests
    {
        private string _testSavePathBefore;

        [SetUp]
        public void UseOwnFiles()
        {
            // A PlayMode run earlier in the same editor session leaves its redirect behind; these tests name their own files.
            _testSavePathBefore = SaveService.TestSavePath;
            SaveService.TestSavePath = null;
        }

        [TearDown]
        public void PutRedirectBack() => SaveService.TestSavePath = _testSavePathBefore;

        private static string NewDirectory()
        {
            string directory = Path.Combine(Application.temporaryCachePath, nameof(SaveDataRecoveryTests), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static SaveService Loaded(string path)
        {
            var service = new SaveService(path);
            service.Initialize();
            return service;
        }

        private static GameData Read(string path)
        {
            var data = new GameData();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(path), data);
            return data;
        }

        /// <summary>A save with 100 gold and Corvan bought, then a second write with 900 gold and Aria too: ".prev" holds the first.</summary>
        private static SaveService TwoSaves(string path)
        {
            var first = new GameData { Currency = 100, SettingsMigrated = true, SaveVersion = GameData.CurrentVersion };
            first.OwnedCharacters.Add("Corvan");
            first.UnlockedCharacters.Add("Corvan");
            File.WriteAllText(path, JsonUtility.ToJson(first, true));
            var service = Loaded(path);
            service.CurrentData.Currency = 900;
            service.CurrentData.OwnedCharacters.Add("Aria");
            service.CurrentData.UnlockedCharacters.Add("Aria");
            Assert.That(service.SaveGame(), Is.True);
            Assert.That(File.Exists(service.PreviousPath), Is.True, "Every save keeps the one before it.");
            return service;
        }

        [Test]
        public void ThePreviousSave_IsDescribed_WithoutBeingChanged()
        {
            string directory = NewDirectory();
            try
            {
                string path = Path.Combine(directory, "save.json");
                var service = TwoSaves(path);
                byte[] previousBefore = File.ReadAllBytes(service.PreviousPath);
                byte[] currentBefore = File.ReadAllBytes(path);

                Assert.That(service.TryDescribePrevious(out var previous), Is.True);
                Assert.That(previous.Gold, Is.EqualTo(100));
                Assert.That(previous.CharactersBought, Is.EqualTo(1));
                Assert.That(previous.CharactersUnlocked, Is.EqualTo(1));
                Assert.That(previous.Written, Is.GreaterThan(DateTime.MinValue), "The file's time is read.");

                var current = service.DescribeCurrent();
                Assert.That(current.Gold, Is.EqualTo(900));
                Assert.That(current.CharactersBought, Is.EqualTo(2));

                Assert.That(File.ReadAllBytes(service.PreviousPath), Is.EqualTo(previousBefore), "Describing reads only.");
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(currentBefore));
                Assert.That(service.CurrentData.Currency, Is.EqualTo(900), "The save in memory is untouched.");
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void Restore_BringsThePreviousSaveBack_AndKeepsTheReplacedOne()
        {
            string directory = NewDirectory();
            try
            {
                string path = Path.Combine(directory, "save.json");
                var service = TwoSaves(path);
                byte[] replaced = File.ReadAllBytes(path);

                Assert.That(service.RestorePrevious(), Is.True);
                Assert.That(service.CurrentData.Currency, Is.EqualTo(100), "The save in memory is the previous one.");
                Assert.That(service.CurrentData.OwnedCharacters, Is.EqualTo(new[] { "Corvan" }));
                Assert.That(Read(path).Currency, Is.EqualTo(100), "The file is the previous one.");
                Assert.That(File.Exists(service.ReplacedBackupPath), Is.True);
                Assert.That(File.ReadAllBytes(service.ReplacedBackupPath), Is.EqualTo(replaced), "The replaced save is kept byte for byte.");
                Assert.That(File.Exists(path + ".tmp"), Is.False, "No temp file is left behind.");

                // The restored save loads on the next launch, and later saves work as before.
                Assert.That(Loaded(path).CurrentData.Currency, Is.EqualTo(100));
                service.CurrentData.Currency = 150;
                Assert.That(service.SaveGame(), Is.True);
                Assert.That(Read(path).Currency, Is.EqualTo(150));
                Assert.That(Read(service.PreviousPath).Currency, Is.EqualTo(100));
            }
            finally { Directory.Delete(directory, true); }
        }

        /// <summary>
        /// Bug MIS ITM-01: a second restore (a double press, or a save written in between) copied over ".replaced.bak",
        /// so the newer progress the first restore had kept was lost. Backups are never overwritten now.
        /// </summary>
        [Test]
        public void RestoringTwice_NeverLosesTheNewerSave_TheFirstRestoreKept()
        {
            string directory = NewDirectory();
            try
            {
                string path = Path.Combine(directory, "save.json");
                var service = TwoSaves(path);
                byte[] newest = File.ReadAllBytes(path); // 900 gold, Corvan and Aria

                Assert.That(service.RestorePrevious(), Is.True);
                Assert.That(service.LastReplacedBackupPath, Is.EqualTo(service.ReplacedBackupPath));

                // A second press before anything is written: it replaces nothing, so it keeps nothing and overwrites nothing.
                Assert.That(service.RestorePrevious(), Is.True);
                Assert.That(Read(path).Currency, Is.EqualTo(100));
                Assert.That(service.LastReplacedBackupPath, Is.Null, "The second press replaced nothing.");
                Assert.That(File.ReadAllBytes(service.ReplacedBackupPath), Is.EqualTo(newest), "The newer progress is still kept.");

                // A save in between, then another restore: the save it replaces gets its own numbered copy.
                service.CurrentData.Currency = 150;
                Assert.That(service.SaveGame(), Is.True);
                byte[] between = File.ReadAllBytes(path);
                Assert.That(service.RestorePrevious(), Is.True);
                Assert.That(Read(path).Currency, Is.EqualTo(100));
                Assert.That(service.LastReplacedBackupPath, Is.EqualTo(path + ".replaced.1.bak"));
                Assert.That(File.ReadAllBytes(path + ".replaced.1.bak"), Is.EqualTo(between));
                Assert.That(File.ReadAllBytes(service.ReplacedBackupPath), Is.EqualTo(newest), "The first backup is never overwritten.");
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void Restore_WithoutAPreviousSave_ChangesNothing()
        {
            string directory = NewDirectory();
            try
            {
                string path = Path.Combine(directory, "save.json");
                var first = new GameData { Currency = 70, SettingsMigrated = true, SaveVersion = GameData.CurrentVersion };
                File.WriteAllText(path, JsonUtility.ToJson(first, true));
                var service = Loaded(path);
                Assert.That(File.Exists(service.PreviousPath), Is.False, "A loaded save that was never written again has no previous save.");
                byte[] before = File.ReadAllBytes(path);

                Assert.That(service.TryDescribePrevious(out var summary), Is.False);
                Assert.That(summary, Is.Null);
                LogAssert.Expect(LogType.Warning, new Regex(@"\[SaveService\] Data Recovery: no readable previous save\."));
                Assert.That(service.RestorePrevious(), Is.False);
                Assert.That(service.CurrentData.Currency, Is.EqualTo(70));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
                Assert.That(File.Exists(service.ReplacedBackupPath), Is.False);
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void Restore_WithADamagedPreviousSave_ChangesNothing()
        {
            string directory = NewDirectory();
            try
            {
                string path = Path.Combine(directory, "save.json");
                var service = TwoSaves(path);
                File.WriteAllText(service.PreviousPath, "not a save");
                byte[] before = File.ReadAllBytes(path);

                Assert.That(service.TryDescribePrevious(out _), Is.False);
                LogAssert.Expect(LogType.Warning, new Regex(@"\[SaveService\] Data Recovery: no readable previous save\."));
                Assert.That(service.RestorePrevious(), Is.False);
                Assert.That(service.CurrentData.Currency, Is.EqualTo(900));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void DeleteSave_KeepsACopy_RemovesProgress_AndKeepsTheDevicesOptions()
        {
            string directory = NewDirectory();
            try
            {
                string path = Path.Combine(directory, "save.json");
                var service = TwoSaves(path);
                service.CurrentData.FlashingVfx = false;
                service.CurrentData.MusicVolume = 0.25f;
                service.CurrentData.InputBindingOverrides = "Dash|1|<Keyboard>/k";
                service.CurrentData.ModeHurry = true;
                service.CurrentData.ChestsOpened = 12;
                Assert.That(service.SaveGame(), Is.True);
                byte[] deleted = File.ReadAllBytes(path);

                Assert.That(service.DeleteSave(), Is.True);

                string backup = path + ".deleted.bak";
                Assert.That(service.LastDeletedBackupPath, Is.EqualTo(backup));
                Assert.That(File.ReadAllBytes(backup), Is.EqualTo(deleted), "The deleted save is kept byte for byte.");
                Assert.That(File.Exists(service.PreviousPath), Is.False, "The previous save goes too: Data Recovery cannot bring the progress back.");
                Assert.That(service.TryDescribePrevious(out _), Is.False);

                var fresh = service.CurrentData;
                Assert.That(fresh.Currency, Is.EqualTo(0));
                Assert.That(fresh.OwnedCharacters, Is.Empty);
                Assert.That(fresh.UnlockedCharacters, Is.Empty);
                Assert.That(fresh.ChestsOpened, Is.EqualTo(0));
                Assert.That(fresh.ModeHurry, Is.False, "Run modes are progress-side choices and reset.");
                Assert.That(fresh.SelectedCharacterName, Is.EqualTo("Kael"));
                Assert.That(fresh.SaveVersion, Is.EqualTo(GameData.CurrentVersion));
                Assert.That(fresh.FlashingVfx, Is.False, "An accessibility option survives the delete.");
                Assert.That(fresh.MusicVolume, Is.EqualTo(0.25f));
                Assert.That(fresh.InputBindingOverrides, Is.EqualTo("Dash|1|<Keyboard>/k"));
                Assert.That(fresh.SettingsMigrated, Is.True);

                var onDisk = Read(path);
                Assert.That(onDisk.Currency, Is.EqualTo(0), "A fresh save is written at once.");
                Assert.That(onDisk.FlashingVfx, Is.False);
                Assert.That(Loaded(path).CurrentData.OwnedCharacters, Is.Empty, "The next launch loads the fresh save.");

                // A second delete never overwrites the first copy.
                service.CurrentData.Currency = 5;
                Assert.That(service.SaveGame(), Is.True);
                Assert.That(service.DeleteSave(), Is.True);
                Assert.That(service.LastDeletedBackupPath, Is.EqualTo(path + ".deleted.1.bak"));
                Assert.That(File.ReadAllBytes(backup), Is.EqualTo(deleted), "The first copy is untouched.");
                Assert.That(Read(path + ".deleted.1.bak").Currency, Is.EqualTo(5));
            }
            finally { Directory.Delete(directory, true); }
        }
    }
}
