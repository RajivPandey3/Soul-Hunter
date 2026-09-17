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
    }
}
