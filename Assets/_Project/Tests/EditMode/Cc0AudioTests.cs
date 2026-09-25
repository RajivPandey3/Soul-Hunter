using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: Owner ne "sound pro nahi hai" kaha aur CC0 sound packs chune (2026-09-25). Har game sound aur
    /// har stage ka music ek CC0 recording ho, uska source CREDITS.md mein likha ho, aur music stream ho (memory bache).
    /// </summary>
    public class Cc0AudioTests
    {
        private const string Folder = "Assets/_Project/Audio/CC0";

        private static readonly string[] Sounds =
        {
            "GemPickup", "LevelUp", "EnemyHit", "ChestOpen", "Heal", "EnemyDeath", "PlayerHurt", "GameOver", "StageClear",
            "BossWarning", "UIClick", "UIConfirm", "Unlock", "Shoot", "Slash", "Magic", "Explosion",
        };

        [Test]
        public void AudioManager_UsesTheCc0Recordings_ForEverySoundAndStage()
        {
            var prefab = Resources.Load<GameObject>("AudioManager");
            Assert.IsNotNull(prefab);
            var audio = prefab.GetComponent<SoulHunter.Gameplay.Audio.AudioManager>();
            var clips = new[]
            {
                audio.GemPickupSound, audio.LevelUpSound, audio.EnemyHitSound, audio.ChestOpenSound, audio.HealSound,
                audio.EnemyDeathSound, audio.PlayerHurtSound, audio.GameOverSound, audio.StageClearSound, audio.BossWarningSound,
                audio.UIClickSound, audio.UIConfirmSound, audio.UnlockSound, audio.ShootSound, audio.SlashSound, audio.MagicSound,
                audio.ExplosionSound,
            };
            for (int i = 0; i < Sounds.Length; i++)
                Assert.That(AssetDatabase.GetAssetPath(clips[i]), Is.EqualTo($"{Folder}/SFX/{Sounds[i]}.ogg"), Sounds[i]);
            Assert.That(AssetDatabase.GetAssetPath(audio.MenuMusic), Is.EqualTo($"{Folder}/Music/Music_Menu.ogg"));
            for (int stage = 1; stage <= 10; stage++)
            {
                string path = AssetDatabase.GetAssetPath(audio.StageMusic[stage - 1]);
                Assert.That(path, Is.EqualTo($"{Folder}/Music/Music_Stage{stage:00}.ogg"));
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(AudioClipLoadType.Streaming), $"{path} streams.");
            }
        }

        [Test]
        public void Credits_NameTheCc0SourceOfEveryFile()
        {
            string credits = File.ReadAllText($"{Folder}/CREDITS.md");
            StringAssert.Contains("CC0", credits);
            foreach (string name in Sounds.Concat(new[] { "Music_Menu" }).Concat(Enumerable.Range(1, 10).Select(i => $"Music_Stage{i:00}")))
                StringAssert.Contains($"| {name} |", credits, $"{name} has no credits line.");
            Assert.That(credits, Does.Not.Contain("CC-BY"), "Only CC0 sources are allowed.");
        }
    }
}
