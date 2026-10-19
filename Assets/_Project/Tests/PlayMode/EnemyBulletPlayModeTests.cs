using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Hazards;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Phone profile (2026-10-18): enemy bullets are EnemyBullets, with no Rigidbody and no Collider, so PhysX does not
    /// carry Level 5's 800-1,050 bullets. A bullet that reaches the hero deals its damage once and ends; one that passes by
    /// does not hit; one that hits nothing ends at its lifetime; and the drawer still draws it.
    /// </summary>
    public class EnemyBulletPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator EnemyBullets_HaveNoPhysics_HitTheHeroOnce_AndEndAtTheirLifetime()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var enemy in EnemyController.ActiveEnemies.ToArray()) enemy.gameObject.SetActive(false);
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            var health = player.GetComponent<HealthController>();
            yield return new WaitForSeconds(0.5f); // clear of any invulnerability

            Vector3 hero = player.transform.position;
            int before = health.CurrentHealth;
            var hit = HazardBullets.Fire(hero + Vector3.forward * 3f, Vector3.back, 6f, 7, 3f);
            Assert.That(hit, Is.InstanceOf<EnemyBullet>(), "Enemy bullets are EnemyBullets.");
            Assert.That(hit.GetComponent<Rigidbody>(), Is.Null, "No Rigidbody: PhysX does not carry it.");
            Assert.That(hit.GetComponentInChildren<Collider>(true), Is.Null, "And no Collider.");
            Assert.That(hit.GetComponent<EnemyBulletLook>(), Is.Not.Null, "The drawer still draws it.");
            Assert.That(((EnemyBullet)hit).HitRadius, Is.GreaterThan(0.01f).And.LessThan(1f), "It touches within the bolt's own size.");
            float t = 0f;
            while (hit.gameObject.activeSelf && t < 2f) { t += Time.deltaTime; yield return null; }
            Assert.That(hit.gameObject.activeSelf, Is.False, "The bullet ends on the hero.");
            Assert.That(health.CurrentHealth, Is.EqualTo(before - 7), "It deals its damage once.");

            yield return new WaitForSeconds(0.5f);
            hero = player.transform.position;
            before = health.CurrentHealth;
            var miss = HazardBullets.Fire(hero + Vector3.forward * 3f + Vector3.right * 3f, Vector3.back, 6f, 7, 1.2f);
            yield return null;
            Vector3 start = miss.transform.position;
            yield return new WaitForSeconds(0.4f);
            Assert.That(Vector3.Distance(start, miss.transform.position), Is.GreaterThan(1f), "It flies.");
            yield return new WaitForSeconds(1.2f);
            Assert.That(health.CurrentHealth, Is.EqualTo(before), "A bullet that passes by does not hit.");
            Assert.That(miss.gameObject.activeSelf, Is.False, "It ends at its lifetime.");

            // A reused bullet starts again where it is fired.
            var again = HazardBullets.Fire(hero + Vector3.forward * 8f, Vector3.forward, 1f, 7, 2f);
            yield return null;
            Assert.That(Vector3.Distance(again.transform.position, hero + Vector3.forward * 8f), Is.LessThan(0.5f), "A pooled bullet starts where it is fired.");
            again.gameObject.SetActive(false);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// Bug MIS AI-11. Enemy bullets (EnemyBullets, and the physics Projectiles of the A/B mode) outlived the stage: they
        /// kept flying and hitting through the stage clear and into the next level. StageHazards.ClearAll now ends them.
        /// </summary>
        [UnityTest]
        public IEnumerator EnemyBullets_EndWhenTheStageChanges()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            Vector3 hero = player.transform.position;
            var light = HazardBullets.Fire(hero + Vector3.forward * 12f, Vector3.forward, 0.5f, 7, 60f);
            Component heavy;
            HazardBullets.LightBullets = false;
            try { heavy = HazardBullets.Fire(hero + Vector3.right * 12f, Vector3.right, 0.5f, 7, 60f); }
            finally { HazardBullets.LightBullets = true; }
            Assert.That(light, Is.InstanceOf<EnemyBullet>());
            Assert.That(heavy, Is.InstanceOf<Projectile>(), "The physics bullet of the A/B mode.");
            yield return null;
            Assert.That(light.gameObject.activeSelf && heavy.gameObject.activeSelf, Is.True, "Both are in flight.");

            SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.StartStage(2);
            yield return null;
            Assert.That(light.gameObject.activeSelf, Is.False, "The enemy bullet ended with the stage.");
            Assert.That(heavy.gameObject.activeSelf, Is.False, "The physics enemy bullet ended with the stage.");
            Assert.That(EnemyBullet.ActiveCount, Is.Zero, "No enemy bullet flies into the next level.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
