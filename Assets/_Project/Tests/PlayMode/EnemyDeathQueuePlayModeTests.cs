using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// The death queue (2026-10-29): a horde body that dies leaves the screen, the live list and every hit in its death's
    /// frame, with its kill and gem, and is switched off a frame or two later, at most EnemyDeathQueue.PerFrame a frame.
    /// A Lightning Ring strike that killed 116 enemies spent 4.56 ms switching them off in one physics step.
    /// </summary>
    public class EnemyDeathQueuePlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator AKilledHordeBody_LeavesAtOnce_AndIsSwitchedOffAFewAFrameLater()
        {
            yield return BootIntoGameplay();
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            player.GetComponent<HealthController>().IsInvincible = true;
            EnemyDeathQueue.Enabled = true;
            EnemyDeathQueue.PerFrame = 4;
            try
            {
                yield return WaitUntilRealtime(() => EnemyController.ActiveEnemies.Count(e => e != null && HordeVatDrawer.Draws(e)) >= 10,
                    "ten horde bodies drawn by the GPU drawer");
                var victims = EnemyController.ActiveEnemies.Where(e => e != null && HordeVatDrawer.Draws(e)).Take(10).ToList();
                int kills = RunStatsTracker.Instance.TotalKills;
                int gems = XPGem.ActiveGems.Count;

                foreach (var enemy in victims)
                    enemy.GetComponent<HealthController>().TakeDamage(new DamagePacket(1_000_000, enemy.transform.position, Vector3.zero, DamageType.Holy));

                // The death's frame: counted and dropped at once, gone from the live list, hit by nothing, harmless.
                Assert.That(RunStatsTracker.Instance.TotalKills - kills, Is.EqualTo(10), "Every kill counts at once. " + Errors());
                Assert.That(XPGem.ActiveGems.Count - gems, Is.GreaterThanOrEqualTo(1), "The gems drop at once.");
                foreach (var enemy in victims)
                {
                    Assert.That(EnemyController.ActiveEnemies.Contains(enemy), Is.False, "A dead body leaves the live list.");
                    Assert.That(enemy.IsDying, Is.True, "Refused: " + EnemyController.LastRefusal);
                    Assert.That(enemy.gameObject.activeSelf, Is.True, "Its switch-off waits.");
                    Assert.That(DamageableLookup.Of(enemy.GetComponent<Collider>()), Is.Null, "No shot spends a hit on it.");
                    var touch = enemy.GetComponent<TouchDamage>();
                    if (touch != null) Assert.That(touch.enabled, Is.False, "It cannot hurt the hero.");
                }
                Assert.That(EnemyDeathQueue.Count, Is.EqualTo(10));

                yield return null; // the next frame switches off at most PerFrame
                Assert.That(victims.Count(e => !e.gameObject.activeSelf), Is.InRange(1, 4));
                yield return WaitUntilRealtime(() => EnemyDeathQueue.Count == 0, "every waiting body switched off");
                Assert.That(victims.All(e => !e.gameObject.activeSelf && !e.IsDying), Is.True);

                // A body reused from the pool hurts again.
                yield return WaitUntilRealtime(() => victims.Any(e => e.gameObject.activeSelf), "a dead body reused by the spawner");
                var reused = victims.First(e => e.gameObject.activeSelf);
                Assert.That(reused.IsDying, Is.False);
                var reusedTouch = reused.GetComponent<TouchDamage>();
                if (reusedTouch != null) Assert.That(reusedTouch.enabled, Is.True, "Contact damage is back on its next life.");
                Assert.That(EnemyController.ActiveEnemies.Contains(reused), Is.True);
                Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
            }
            finally
            {
                EnemyDeathQueue.Enabled = false;
                EnemyDeathQueue.PerFrame = 40;
            }
        }

        [UnityTest]
        public IEnumerator AStageChange_SwitchesEveryWaitingBodyOffAtOnce()
        {
            yield return BootIntoGameplay();
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            player.GetComponent<HealthController>().IsInvincible = true;
            EnemyDeathQueue.Enabled = true;
            EnemyDeathQueue.PerFrame = 1;
            try
            {
                yield return WaitUntilRealtime(() => EnemyController.ActiveEnemies.Count(e => e != null && HordeVatDrawer.Draws(e)) >= 5,
                    "five horde bodies drawn by the GPU drawer");
                var victims = EnemyController.ActiveEnemies.Where(e => e != null && HordeVatDrawer.Draws(e)).Take(5).ToList();
                foreach (var enemy in victims)
                    enemy.GetComponent<HealthController>().TakeDamage(new DamagePacket(1_000_000, enemy.transform.position, Vector3.zero, DamageType.Holy));
                Assert.That(EnemyDeathQueue.Count, Is.EqualTo(5), "Refused: " + EnemyController.LastRefusal + " " + Errors());
                LevelProgressionManager.Instance.StartStage(2);
                Assert.That(EnemyDeathQueue.Count, Is.EqualTo(0), "The stage start empties the queue.");
                Assert.That(victims.All(e => !e.gameObject.activeSelf), Is.True);
                Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
            }
            finally
            {
                EnemyDeathQueue.Enabled = false;
                EnemyDeathQueue.PerFrame = 40;
            }
        }
    }
}
