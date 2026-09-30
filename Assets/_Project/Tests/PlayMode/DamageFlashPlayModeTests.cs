using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.VFX;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Bug MIS B02 (2026-09-30): enemies and the boss looked like flat white silhouettes. Every hit restarted the white
    /// flash, so an enemy under constant fire stayed white, and the flash froze during pauses.
    /// </summary>
    public class DamageFlashPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator AnEnemyHitEveryFrame_Blinks_ItDoesNotStayWhite()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            var prefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            var enemy = Object.Instantiate(prefab, player.transform.position + Vector3.forward * 6f, Quaternion.identity);
            var health = enemy.GetComponent<HealthController>();
            health.Initialize(1000000);
            var flash = enemy.GetComponent<DamageFlash>();
            Assert.IsNotNull(flash);
            yield return null;

            int frames = 0, white = 0, longestWhite = 0, run = 0;
            float end = Time.realtimeSinceStartup + 1.5f;
            while (Time.realtimeSinceStartup < end)
            {
                health.TakeDamage(new DamagePacket(1, enemy.transform.position, Vector3.zero));
                yield return null;
                frames++;
                if (flash.IsFlashing) { white++; run++; longestWhite = Mathf.Max(longestWhite, run); }
                else run = 0;
            }
            Assert.That(white, Is.GreaterThan(0), "Hits still flash.");
            Assert.That(white / (float)frames, Is.LessThanOrEqualTo(0.6f), $"White {white} of {frames} frames: it blinks.");

            // A flash that starts just before a pause ends during the pause (it used to stay white until the game resumed).
            yield return new WaitForSecondsRealtime(DamageFlash.FlashGap + 0.05f);
            health.TakeDamage(new DamagePacket(1, enemy.transform.position, Vector3.zero));
            Assert.That(flash.IsFlashing, Is.True);
            GameTime.Pause(this);
            try
            {
                yield return new WaitForSecondsRealtime(0.4f);
                Assert.That(flash.IsFlashing, Is.False, "The flash ends while the game is paused.");
            }
            finally { GameTime.Resume(this); }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
