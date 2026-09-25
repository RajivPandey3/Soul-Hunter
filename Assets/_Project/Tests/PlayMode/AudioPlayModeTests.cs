using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Audio;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Audio in the real game: the AudioManager exists with one listener, every sound and music loop is
    /// assigned and audible, the menu and each stage play their own music, and gameplay events ask for
    /// their sound. Batchmode may not output sound, so checks read AudioManager state rather than listening.
    /// </summary>
    public class AudioPlayModeTests : GameplayPlayModeTestBase
    {
        private static void AssertAudible(AudioClip clip, string name)
        {
            Assert.IsNotNull(clip, $"{name} is not assigned.");
            clip.LoadAudioData();
            var samples = new float[clip.samples * clip.channels];
            Assert.That(clip.GetData(samples, 0), Is.True, $"{name} data unreadable.");
            Assert.That(samples.Max(Mathf.Abs), Is.GreaterThan(0.05f), $"{name} is silent.");
        }

        private static IEnumerator WaitSeconds(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        [UnityTest]
        public IEnumerator Menu_PlaysMenuMusic_AndEverySoundIsAudible()
        {
            AudioManager.EnsureInstance(); // earlier tests destroy DontDestroyOnLoad objects; the game creates it once at startup
            var load = SceneManager.LoadSceneAsync("Bootstrap");
            Assert.IsNotNull(load, "Bootstrap is not in Build Settings.");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            yield return null;
            var audio = AudioManager.Instance;
            Assert.IsNotNull(audio, "AudioManager missing. " + Errors());
            Assert.That(audio.CurrentMusic, Is.SameAs(audio.MenuMusic), "The menu plays its own music.");
            Assert.That(audio.IsMusicPlaying, Is.True);

            var clips = new Dictionary<string, AudioClip>
            {
                { "GemPickup", audio.GemPickupSound }, { "LevelUp", audio.LevelUpSound }, { "EnemyHit", audio.EnemyHitSound },
                { "ChestOpen", audio.ChestOpenSound }, { "Heal", audio.HealSound }, { "EnemyDeath", audio.EnemyDeathSound },
                { "PlayerHurt", audio.PlayerHurtSound }, { "GameOver", audio.GameOverSound }, { "StageClear", audio.StageClearSound },
                { "BossWarning", audio.BossWarningSound }, { "UIClick", audio.UIClickSound }, { "UIConfirm", audio.UIConfirmSound },
                { "Unlock", audio.UnlockSound }, { "Shoot", audio.ShootSound }, { "Slash", audio.SlashSound },
                { "Magic", audio.MagicSound }, { "Explosion", audio.ExplosionSound },
            };
            foreach (var pair in clips) AssertAudible(pair.Value, pair.Key);
            // Music is Vorbis-compressed in memory, so its samples cannot be read here; the generator checks its level.
            Assert.IsNotNull(audio.MenuMusic, "Menu music is not assigned.");
            Assert.That(audio.MenuMusic.length, Is.GreaterThan(8f));
            Assert.That(audio.StageMusic, Is.Not.Null.And.Length.EqualTo(10), "One music loop per stage.");
            for (int i = 0; i < audio.StageMusic.Length; i++)
            {
                Assert.IsNotNull(audio.StageMusic[i], $"Stage {i + 1} music is not assigned.");
                Assert.That(audio.StageMusic[i].length, Is.GreaterThan(8f), $"Stage {i + 1} music is a real loop.");
            }
            Assert.That(audio.StageMusic.Distinct().Count(), Is.EqualTo(10), "Every stage has its own music.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Gameplay_StageMusic_AndEventSounds()
        {
            AudioManager.EnsureInstance();
            yield return BootIntoGameplay();
            var audio = AudioManager.Instance;
            Assert.IsNotNull(audio, "AudioManager missing. " + Errors());
            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Where(l => l.isActiveAndEnabled).ToList();
            Assert.That(listeners.Count, Is.EqualTo(1), "Exactly one active AudioListener. " + Errors());
            Assert.That(AudioListener.volume, Is.GreaterThan(0f), "Master volume is not muted.");
            Assert.That(audio.CurrentMusic, Is.SameAs(audio.StageMusic[0]), "Stage 1 plays its music. " + Errors());

            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) Object.Destroy(stray.gameObject);
            yield return null;

            // Weapons are heard when they attack.
            var weaponClips = new[] { audio.ShootSound, audio.SlashSound, audio.MagicSound, audio.ExplosionSound };
            bool weaponHeard = false;
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!weaponHeard && Time.realtimeSinceStartup < deadline)
            {
                weaponHeard = weaponClips.Contains(audio.LastClip);
                yield return null;
            }
            Assert.That(weaponHeard, Is.True, "The starting weapon made no sound in 5 s. " + Errors());

            // Hitting and killing an enemy.
            var prefabs = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs");
            var player = PlayerController.Instance;
            var enemy = Object.Instantiate(prefabs[0], player.transform.position + new Vector3(0f, 0f, 30f), Quaternion.identity);
            yield return null;
            var health = enemy.GetComponent<HealthController>();
            yield return WaitSeconds(AudioManager.EnemyHitInterval + 0.02f);
            health.TakeDamage(new DamagePacket(1, enemy.transform.position, Vector3.zero));
            Assert.That(audio.LastClip, Is.SameAs(audio.EnemyHitSound), "Hitting an enemy plays EnemyHit. " + Errors());
            yield return WaitSeconds(AudioManager.EnemyDeathInterval + 0.02f);
            health.TakeDamage(new DamagePacket(99999, enemy.transform.position, Vector3.zero));
            Assert.That(audio.LastClip, Is.SameAs(audio.EnemyDeathSound), "Killing an enemy plays EnemyDeath. " + Errors());

            // The player being hit.
            var playerHealth = player.GetComponent<HealthController>();
            playerHealth.ShieldedUntil = -1f;
            yield return WaitSeconds(AudioManager.PlayerHurtInterval + 0.02f);
            playerHealth.TakeDamage(new DamagePacket(1, player.transform.position, Vector3.zero) { IgnoresHitInvulnerability = true });
            Assert.That(audio.LastClip, Is.SameAs(audio.PlayerHurtSound), "Getting hit plays PlayerHurt. " + Errors());

            // A new stage switches the music.
            LevelProgressionManager.Instance.StartStage(3);
            yield return null;
            Assert.That(audio.CurrentMusic, Is.SameAs(audio.StageMusic[2]), "Stage 3 plays its own music. " + Errors());

            // Game over stops the music and plays its sound.
            GameSessionManager.Instance.GameOver();
            yield return null;
            Assert.That(audio.IsMusicPlaying, Is.False, "Music stops on game over.");
            Assert.That(audio.LastClip, Is.SameAs(audio.GameOverSound), "Game over plays its sound. " + Errors());
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
