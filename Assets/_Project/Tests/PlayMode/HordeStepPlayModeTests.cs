using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// 60 FPS pass (2026-10-14): the horde is stepped by one EnemyRunner instead of an Update and a FixedUpdate per
    /// enemy, and contact damage is ticked only for a TouchDamage that is touching something. In the real game: an
    /// enemy still chases, a switched-off enemy stands still, only a touching enemy is ticked, and a switched-off
    /// TouchDamage deals nothing.
    /// </summary>
    public class HordeStepPlayModeTests : GameplayPlayModeTestBase
    {
        private EnemySpawner _spawner;

        private IEnumerator QuietArena()
        {
            yield return BootIntoGameplay();
            _spawner = Object.FindFirstObjectByType<EnemySpawner>();
            _spawner.enabled = false;
            yield return null;
            yield return null;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
        }

        private EnemyController Walker(float distance, int damage)
        {
            var prefab = GetPrivateField<List<GameObject>>(_spawner, "_stageEnemyPrefabs")[LevelProgressionManager.Instance.CurrentStage - 1];
            var player = PlayerController.Instance;
            var enemy = Object.Instantiate(prefab, player.transform.position + Vector3.right * distance, Quaternion.identity).GetComponent<EnemyController>();
            enemy.Target = player.transform;
            enemy.GetComponent<TouchDamage>().DamageAmount = damage;
            return enemy;
        }

        private static float Gap(EnemyController enemy)
        {
            Vector3 d = enemy.transform.position - PlayerController.Instance.transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        private static IEnumerator Seconds(float s)
        {
            float until = Time.time + s;
            while (Time.time < until) yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator OneRunner_StepsTheHorde_AndASwitchedOffEnemy_StandsStill()
        {
            yield return QuietArena();
            var enemy = Walker(8f, 0);
            yield return Seconds(0.5f);
            Assert.That(Object.FindObjectsByType<EnemyRunner>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(1), "One runner for the whole horde.");
            float walked = Gap(enemy);
            Assert.That(walked, Is.LessThan(7.5f), "The enemy chases the hero.");
            Vector3 facing = enemy.transform.Find("SH10_Visual").forward;
            Vector3 toHero = PlayerController.Instance.transform.position - enemy.transform.position;
            toHero.y = 0f;
            Assert.That(Vector3.Dot(facing, -toHero.normalized), Is.GreaterThan(0.95f), "And its model faces the hero (the models' authored front is -Z).");

            enemy.enabled = false;
            yield return Seconds(0.2f);
            float held = Gap(enemy);
            yield return Seconds(0.5f);
            Assert.That(Gap(enemy), Is.EqualTo(held).Within(0.02f), "A switched-off enemy is not stepped.");

            enemy.enabled = true;
            yield return Seconds(0.5f);
            Assert.That(Gap(enemy), Is.LessThan(held - 0.3f), "Switched on again, it comes on again.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator OnlyATouchingEnemy_IsTicked_AndASwitchedOffTouch_DealsNothing()
        {
            yield return QuietArena();
            var player = PlayerController.Instance;
            var health = player.GetComponent<HealthController>();
            health.IsInvincible = false;
            health.Initialize(100000);

            var far = Walker(9f, 5);
            far.Target = null; // it stays where it is
            yield return Seconds(0.3f);
            int idle = TouchDamageTicker.Count;
            int before = health.CurrentHealth;
            yield return Seconds(0.7f);
            Assert.That(health.CurrentHealth, Is.EqualTo(before), "An enemy that touches nothing hurts nobody.");
            Assert.That(TouchDamageTicker.Count, Is.EqualTo(idle), "And it is not ticked.");

            var near = Walker(0.3f, 5);
            yield return Seconds(1.2f);
            Assert.That(TouchDamageTicker.Count, Is.EqualTo(idle + 1), "The touching enemy is ticked.");
            Assert.That(health.CurrentHealth, Is.LessThanOrEqualTo(before - 10), "It hurts the hero every 0.5 s.");

            near.GetComponent<TouchDamage>().enabled = false;
            yield return null;
            Assert.That(TouchDamageTicker.Count, Is.EqualTo(idle), "Switched off, it leaves the ticker.");
            int afterOff = health.CurrentHealth;
            yield return Seconds(1.2f);
            Assert.That(health.CurrentHealth, Is.EqualTo(afterOff), "A switched-off TouchDamage deals nothing.");

            near.gameObject.SetActive(false);
            near.GetComponent<TouchDamage>().enabled = true;
            near.transform.position = player.transform.position + Vector3.right * 0.3f;
            near.gameObject.SetActive(true); // a pooled body coming back next to the hero
            near.Target = player.transform;
            yield return Seconds(1.2f);
            Assert.That(health.CurrentHealth, Is.LessThan(afterOff), "A reused body touches and hurts again.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
