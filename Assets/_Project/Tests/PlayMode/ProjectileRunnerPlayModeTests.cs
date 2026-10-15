using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// 60 FPS pass (2026-10-15): flying shots are moved by one ProjectileRunner instead of an Update each, a shot far off
    /// the screen switches its glow and trail particles off until it comes back, and a flat enemy body no longer runs the
    /// ground scan. In the real game: a shot flies at its speed and ends at its lifetime, a switched-off shot stays put,
    /// the particles go off and come back, a one-off burst is left alone, and the horde still chases without the scan.
    /// </summary>
    public class ProjectileRunnerPlayModeTests : GameplayPlayModeTestBase
    {
        private IEnumerator QuietArena()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            yield return null;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
        }

        /// <summary>A shot built like the weapon shots: a looping trail (and optionally a one-off burst) under it.</summary>
        private static Projectile Shot(Vector3 at, bool withBurst = false)
        {
            var go = new GameObject("TestShot");
            go.SetActive(false);
            go.transform.position = at;
            var trail = new GameObject("Trail").AddComponent<ParticleSystem>();
            trail.transform.SetParent(go.transform, false);
            var main = trail.main;
            main.loop = true;
            main.playOnAwake = true;
            if (withBurst)
            {
                var burst = new GameObject("Burst").AddComponent<ParticleSystem>();
                burst.transform.SetParent(go.transform, false);
                var burstMain = burst.main;
                burstMain.loop = false;
                burstMain.playOnAwake = false;
                burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            go.AddComponent<SphereCollider>().radius = 0.2f;
            go.AddComponent<Rigidbody>();
            var shot = go.AddComponent<Projectile>();
            go.SetActive(true);
            return shot;
        }

        [UnityTest]
        public IEnumerator OneRunner_FliesEveryShot_AtItsSpeed_AndEndsItAtItsLifetime()
        {
            yield return QuietArena();
            Vector3 start = PlayerController.Instance.transform.position + Vector3.forward * 2f;
            var shot = Shot(start);
            shot.Initialize(Vector3.right, 4f, 1f, 0.5f);
            yield return null;
            Assert.That(ProjectileRunner.Exists, Is.True, "The runner is made by the first shot.");
            Assert.That(Projectile.ActiveShots, Has.Member(shot));
            float t0 = Time.time;
            Vector3 p0 = shot.transform.position;
            yield return new WaitForSeconds(0.25f);
            float flown = (shot.transform.position - p0).x, expected = 4f * (Time.time - t0);
            Assert.That(flown, Is.EqualTo(expected).Within(0.25f), "It flies along its direction at its speed.");
            Assert.That(shot.transform.position.z, Is.EqualTo(start.z).Within(0.01f));
            yield return new WaitForSeconds(0.45f);
            Assert.That(shot.gameObject.activeSelf, Is.False, "Its lifetime ends it.");
            Assert.That(Projectile.ActiveShots, Has.No.Member(shot), "And it leaves the runner's list.");
            Object.Destroy(shot.gameObject);
        }

        [UnityTest]
        public IEnumerator ASwitchedOffShot_IsNotMoved_AndAReusedShot_FliesAgain()
        {
            yield return QuietArena();
            var shot = Shot(PlayerController.Instance.transform.position + Vector3.forward * 2f);
            shot.Initialize(Vector3.right, 4f, 1f, 5f);
            yield return null;
            shot.enabled = false;
            Vector3 held = shot.transform.position;
            yield return new WaitForSeconds(0.2f);
            Assert.That(shot.transform.position, Is.EqualTo(held), "A switched-off shot is not moved.");
            Assert.That(Projectile.ActiveShots, Has.No.Member(shot));
            shot.enabled = true;
            shot.Initialize(Vector3.right, 4f, 1f, 5f);
            yield return new WaitForSeconds(0.2f);
            Assert.That(shot.transform.position.x, Is.GreaterThan(held.x + 0.3f), "Switched on again, it flies.");
            Assert.That(Projectile.ActiveShots.Count(s => s == shot), Is.EqualTo(1), "Listed once.");
            Object.Destroy(shot.gameObject);
        }

        [UnityTest]
        public IEnumerator AShotFarOffTheScreen_SwitchesItsTrailOff_AndOnAgainWhenItComesBack()
        {
            yield return QuietArena();
            Vector3 hero = PlayerController.Instance.transform.position;
            var shot = Shot(hero + Vector3.forward * 2f, withBurst: true);
            shot.Initialize(Vector3.right, 0.01f, 1f, 30f);
            var trail = shot.transform.Find("Trail").GetComponent<ParticleSystem>();
            var burst = shot.transform.Find("Burst").GetComponent<ParticleSystem>();
            for (int i = 0; i < 8; i++) yield return null;
            Assert.That(shot.FxStopped, Is.False, "On screen its trail runs.");
            Assert.That(trail.isPlaying, Is.True);

            shot.transform.position = hero + Vector3.right * 300f;
            for (int i = 0; i < 8; i++) yield return null;
            Assert.That(shot.FxStopped, Is.True, "Far off the screen its trail is switched off.");
            Assert.That(trail.isEmitting, Is.False);
            Assert.That(trail.particleCount, Is.EqualTo(0));

            shot.transform.position = hero + Vector3.forward * 2f;
            for (int i = 0; i < 8; i++) yield return null;
            Assert.That(shot.FxStopped, Is.False, "Back on screen its trail comes back.");
            Assert.That(trail.isPlaying, Is.True);
            Assert.That(burst.isPlaying, Is.False, "A one-off burst is never started by this.");
            Object.Destroy(shot.gameObject);
        }

        [UnityTest]
        public IEnumerator AFlatEnemy_SkipsTheGroundScan_AndStillChasesTheHero()
        {
            yield return QuietArena();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var prefab = GetPrivateField<System.Collections.Generic.List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            var player = PlayerController.Instance;
            var enemy = Object.Instantiate(prefab, player.transform.position + Vector3.right * 8f, Quaternion.identity).GetComponent<EnemyController>();
            enemy.Target = player.transform;
            enemy.GetComponent<TouchDamage>().DamageAmount = 0;
            yield return new WaitForSeconds(0.6f);
            bool scans = (bool)typeof(EnemyController).GetField("_scans", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(enemy);
            Assert.That(scans, Is.False, "A body with no gravity and its height frozen skips the scan.");
            Vector3 gap = enemy.transform.position - player.transform.position;
            gap.y = 0f;
            Assert.That(gap.magnitude, Is.LessThan(7.5f), "It still chases the hero.");
            Assert.That(enemy.transform.position.y, Is.EqualTo(player.transform.position.y).Within(1.5f), "And stays on the ground plane.");
            Object.Destroy(enemy.gameObject);
        }

        [UnityTest]
        public IEnumerator EnemyBullets_KeepTheirCoreAndHalo_ButNoTrail_AndTheMagicWandBoltKeepsItsTrail()
        {
            yield return QuietArena();
            Vector3 hero = PlayerController.Instance.transform.position;
            var bullet = SoulHunter.Gameplay.Hazards.HazardBullets.Fire(hero + Vector3.forward * 6f, Vector3.right, 1f, 0, 5f);
            Assert.That(bullet, Is.Not.Null);
            yield return null;
            var systems = bullet.GetComponentsInChildren<ParticleSystem>(true);
            Assert.That(systems.Any(fx => fx.gameObject.name == "Trail"), Is.False, "An enemy bullet has no trail.");
            Assert.That(systems.Any(fx => fx.gameObject.name == "Halo"), Is.True, "It keeps its halo.");
            Assert.That(bullet.GetComponentsInChildren<MeshRenderer>(true).Any(r => r.enabled), Is.True, "And its glowing core.");
            Assert.That(bullet.TargetsPlayer, Is.True);
            int shotLayer = LayerMask.NameToLayer("EnemyShot");
            Assert.That(bullet.gameObject.layer, Is.EqualTo(shotLayer), "An enemy bullet flies on the EnemyShot layer.");
            Assert.That(Physics.GetIgnoreLayerCollision(shotLayer, LayerMask.NameToLayer("Enemy")), Is.True, "It makes no pairs with the horde.");
            Assert.That(Physics.GetIgnoreLayerCollision(shotLayer, LayerMask.NameToLayer("Default")), Is.True);
            Assert.That(Physics.GetIgnoreLayerCollision(shotLayer, LayerMask.NameToLayer("Player")), Is.False, "But it still meets the player.");

            var bolt = SoulHunter.Gameplay.Data.GameContentCatalog.Load().HazardBullet;
            Assert.That(bolt.GetComponentsInChildren<ParticleSystem>(true).Any(fx => fx.gameObject.name == "Trail"), Is.True,
                "The Magic Wand bolt the bullet is made from keeps its trail.");
            bullet.gameObject.SetActive(false);
            bullet.gameObject.SetActive(true);
            Assert.That(bullet.gameObject.layer, Is.Not.EqualTo(shotLayer), "Reused from the pool, the shot is back on its own layer.");
            bullet.gameObject.SetActive(false);
        }
    }
}
