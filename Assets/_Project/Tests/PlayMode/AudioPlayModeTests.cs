using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Audio;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Audio in the real game: the AudioManager exists with one listener, every sound effect is assigned
    /// and audible (not silent), and gameplay events ask for their sound. Batchmode may not output
    /// sound, so the check reads AudioManager.LastClip rather than listening.
    /// </summary>
    public class AudioPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator Audio_IsSetUp_EveryEffectIsAudible_AndHittingAnEnemyPlaysItsSound()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) Object.Destroy(stray.gameObject);
            yield return null;

            var audio = AudioManager.Instance;
            Assert.IsNotNull(audio, "AudioManager missing. " + Errors());
            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Where(l => l.isActiveAndEnabled).ToList();
            Assert.That(listeners.Count, Is.EqualTo(1), "Exactly one active AudioListener. " + Errors());
            Assert.That(AudioListener.volume, Is.GreaterThan(0f), "Master volume is not muted.");

            var clips = new Dictionary<string, AudioClip>
            {
                { "GemPickup", audio.GemPickupSound }, { "LevelUp", audio.LevelUpSound }, { "EnemyHit", audio.EnemyHitSound },
                { "ChestOpen", audio.ChestOpenSound }, { "Heal", audio.HealSound },
            };
            foreach (var pair in clips)
            {
                Assert.IsNotNull(pair.Value, $"{pair.Key} sound is not assigned.");
                pair.Value.LoadAudioData();
                var samples = new float[pair.Value.samples * pair.Value.channels];
                Assert.That(pair.Value.GetData(samples, 0), Is.True, $"{pair.Key} data unreadable.");
                Assert.That(samples.Max(Mathf.Abs), Is.GreaterThan(0.05f), $"{pair.Key} is silent.");
            }

            // Hitting an enemy plays the enemy-hit sound (VS: every hit is heard).
            var prefabs = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs");
            var player = PlayerController.Instance;
            var enemy = Object.Instantiate(prefabs[0], player.transform.position + new Vector3(0f, 0f, 6f), Quaternion.identity);
            yield return null;
            var health = enemy.GetComponent<HealthController>();
            int before = audio.PlayedCount;
            health.TakeDamage(new DamagePacket(1, enemy.transform.position, Vector3.zero));
            yield return null;
            Assert.That(audio.PlayedCount, Is.GreaterThan(before), "Hitting an enemy played no sound. " + Errors());
            Assert.That(audio.LastClip, Is.SameAs(audio.EnemyHitSound), "Hitting an enemy should play EnemyHit. " + Errors());
            Object.Destroy(enemy);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
