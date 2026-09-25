using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.Core;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// AutoplayBot (owner request 2026-09-25: "a script so you can play the game yourself") boots a real run, plays
    /// it and writes its report. This smoke test plays 20 seconds and checks the report exists and is sane.
    /// </summary>
    public class AutoplayBotPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator Bot_PlaysARun_AndWritesItsReport()
        {
            string output = Path.Combine(Application.temporaryCachePath, "autoplay-test");
            if (Directory.Exists(output)) Directory.Delete(output, true);
            string realSave = Path.Combine(Application.persistentDataPath, "soulhunter_save.json");
            byte[] before = File.Exists(realSave) ? File.ReadAllBytes(realSave) : null;
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            var bot = AutoplayBot.Launch(new AutoplayBot.Settings
            {
                OutputFolder = output, Minutes = 20f / 60f, God = true, ShotInterval = 5f, QuitWhenDone = false,
            });
            float deadline = Time.realtimeSinceStartup + 60f;
            while (!File.Exists(Path.Combine(output, "summary.json")) && Time.realtimeSinceStartup < deadline) yield return null;
            yield return new WaitForSecondsRealtime(0.5f);

            Assert.That(File.Exists(Path.Combine(output, "summary.json")), "No summary. " + Errors());
            string summary = File.ReadAllText(Path.Combine(output, "summary.json"));
            StringAssert.Contains("\"result\": \"time limit\"", summary);
            StringAssert.Contains("\"errors\": 0", summary);
            Assert.That(File.ReadAllLines(Path.Combine(output, "timeline.jsonl")).Length, Is.GreaterThanOrEqualTo(3), "Timeline every 5 s.");
            Assert.That(Directory.GetFiles(Path.Combine(output, "shots"), "*.png").Length, Is.GreaterThanOrEqualTo(3), "Screenshots.");
            Assert.That(bot.Finished, Is.True);
            // The first version of this test added the bot's gold to the real save (EconomyService saves on pickup).
            Assert.That(File.Exists(realSave) ? File.ReadAllBytes(realSave) : null, Is.EqualTo(before), "The bot must never change the real save.");
            Object.Destroy(bot.gameObject);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
