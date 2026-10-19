using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Hazards;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Draw calls (2026-10-15): in the real game every enemy bullet on screen is drawn by EnemyBulletDrawer, all cores in
    /// one instanced draw and all halos in another. Each bullet keeps its core renderer (not drawn) and its halo particles
    /// are off; it still flies and hurts as before, and switching the drawer off hands every bullet back.
    /// </summary>
    public class EnemyBulletDrawerPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator EnemyBullets_AreDrawnInTwoInstancedDraws_AndHandBackWhenSwitchedOff()
        {
            yield return BootIntoGameplay();
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            Vector3 centre = player.transform.position;
            // 30 slow bullets 6 units out, flying away from the hero: on screen for the whole test.
            var bullets = Enumerable.Range(0, 30).Select(i =>
            {
                float a = i * Mathf.PI * 2f / 30f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                return HazardBullets.Fire(centre + dir * 6f, dir, 0.2f, 1, 30f);
            }).ToList();
            Assert.That(bullets.All(b => b != null), Is.True, "Every bullet is fired.");
            yield return null;
            yield return null;

            Assert.That(EnemyBulletDrawer.Exists, Is.True, "The drawer is made with the first enemy bullet.");
            foreach (var b in bullets)
            {
                var look = b.GetComponent<EnemyBulletLook>();
                Assert.That(look, Is.Not.Null, "Every pooled bullet carries the marker.");
                var core = b.GetComponentInChildren<MeshRenderer>(true);
                Assert.That(core.enabled && core.forceRenderingOff, Is.True, "The core renderer stays enabled but is drawn by the drawer.");
                var halo = b.transform.Find("Halo");
                if (halo != null) Assert.That(halo.gameObject.activeSelf, Is.False, "The halo particles are off; the drawer draws a quad.");
            }
            Assert.That(EnemyBulletDrawer.LastBullets, Is.GreaterThanOrEqualTo(30));
            Assert.That(EnemyBulletDrawer.LastDraws, Is.LessThanOrEqualTo(2), "All cores in one draw, all halos in another.");

            // The bullets still fly.
            Vector3 before = bullets[0].transform.position;
            yield return new WaitForSeconds(0.3f);
            Assert.That(Vector3.Distance(before, bullets[0].transform.position), Is.GreaterThan(0.02f), "A drawn bullet still flies.");

            EnemyBulletDrawer.Enabled = false;
            yield return null;
            yield return null;
            foreach (var b in bullets.Where(b => b != null && b.gameObject.activeInHierarchy))
            {
                Assert.That(b.GetComponentInChildren<MeshRenderer>(true).forceRenderingOff, Is.False, "Switched off, the core draws itself again.");
                var halo = b.transform.Find("Halo");
                if (halo != null) Assert.That(halo.gameObject.activeSelf, Is.True, "And its halo particles are back.");
            }
            Assert.That(EnemyBulletDrawer.LastBullets, Is.EqualTo(0));
            EnemyBulletDrawer.Enabled = true;
            foreach (var b in bullets.Where(b => b != null)) b.gameObject.SetActive(false);
            yield return null;
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// Bug MIS AI-12. The drawer outlives scenes and kept a batch and a halo material copy per bullet template for ever;
        /// the template is made again in every scene. A scene load (ForgetAll) now drops them and destroys the copies, and
        /// a bullet in flight is drawn again from fresh batches.
        /// </summary>
        [UnityTest]
        public IEnumerator ForgettingBatches_DestroysTheHaloCopies_AndBulletsAreDrawnAgain()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<SoulHunter.Gameplay.AI.EnemySpawner>().enabled = false;
            var hero = PlayerController.Instance.transform.position;
            var bullet = HazardBullets.Fire(hero + Vector3.forward * 6f, Vector3.right, 0.2f, 1, 30f);
            yield return null;
            yield return null;
            Assert.That(EnemyBulletDrawer.KnownBatches, Is.GreaterThanOrEqualTo(1));
            const System.Reflection.BindingFlags any = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            var drawer = typeof(EnemyBulletDrawer).GetField("_instance", any).GetValue(null);
            var halos = (System.Collections.IDictionary)typeof(EnemyBulletDrawer).GetField("_halos", any).GetValue(drawer);
            var copies = new System.Collections.Generic.List<Material>();
            foreach (var b in halos.Values) copies.Add((Material)b.GetType().GetField("Material").GetValue(b));

            EnemyBulletDrawer.ForgetAll();
            Assert.That(EnemyBulletDrawer.KnownBatches, Is.Zero, "Every batch is forgotten.");
            yield return null; // Destroy completes at the end of the frame
            Assert.That(copies.TrueForAll(m => m == null), Is.True, "Its halo material copies are destroyed.");
            yield return null;
            Assert.That(EnemyBulletDrawer.LastBullets, Is.GreaterThanOrEqualTo(1), "The bullet in flight is drawn again.");
            Assert.That(bullet.GetComponentInChildren<MeshRenderer>(true).forceRenderingOff, Is.True);
            bullet.gameObject.SetActive(false);
            yield return null;
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
