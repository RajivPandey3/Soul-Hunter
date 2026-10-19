using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.VFX;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// 60 FPS, horde on the GPU (2026-10-15): in the real game the horde is drawn by HordeVatDrawer in a few instanced draws;
    /// each body's own renderer stays enabled but is not drawn, a tinted or flashing body is drawn in that colour, and
    /// switching the drawer off hands every body back to its own renderer.
    /// </summary>
    public class HordeVatPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator TheHorde_IsDrawnInAFewInstancedDraws_AndHandsBackWhenSwitchedOff()
        {
            yield return BootIntoGameplay();
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            yield return WaitUntilRealtime(() => EnemyController.ActiveEnemies.Count(e => e != null) >= 10, "ten enemies on the field");
            yield return null;
            yield return null;

            Assert.That(HordeVatDrawer.Exists, Is.True, "The drawer is made when baked models exist.");
            var bodies = EnemyController.ActiveEnemies.Where(e => e != null)
                .Select(e => e.transform.Find("SH10_Visual")?.GetComponentInChildren<SkinnedMeshRenderer>())
                .Where(r => r != null && r.shadowCastingMode == ShadowCastingMode.Off).ToList();
            Assert.That(bodies.Count, Is.GreaterThanOrEqualTo(10));
            foreach (var r in bodies)
            {
                Assert.That(r.enabled, Is.True, "The body's renderer stays enabled for flash, corpse and bounds.");
                Assert.That(r.forceRenderingOff, Is.True, "But it is drawn by the GPU horde, not by itself.");
            }
            Assert.That(HordeVatDrawer.LastBodies, Is.GreaterThan(0));
            // The GPU body is the skinned body's size: the baked bounds, placed by the visual, match the renderer's bounds.
            var library = HordeVatLibrary.Load();
            foreach (var r in bodies.Take(5))
            {
                var asset = library.For(r.sharedMesh);
                Assert.That(asset, Is.Not.Null, r.name);
                var visual = r.transform.parent;
                while (visual != null && visual.name != "SH10_Visual") visual = visual.parent;
                Vector3 size = visual.localToWorldMatrix.MultiplyVector(asset.Bounds.size);
                float baked = Mathf.Abs(size.y), skinned = r.bounds.size.y;
                Assert.That(baked, Is.EqualTo(skinned).Within(skinned * 0.25f), $"{r.name}: baked height {baked:0.00}, skinned {skinned:0.00}.");
            }
            Assert.That(HordeVatDrawer.LastDraws, Is.LessThanOrEqualTo(4), "One model's bodies take one draw.");

            // A hit still flashes the body: the drawer reads the flash material DamageFlash puts on it.
            var hit = EnemyController.ActiveEnemies.First(e => e != null);
            var hitRenderer = hit.transform.Find("SH10_Visual").GetComponentInChildren<SkinnedMeshRenderer>();
            var rest = hitRenderer.sharedMaterial;
            hit.GetComponent<HealthController>().TakeDamage(new DamagePacket(1, hit.transform.position, Vector3.zero));
            Assert.That(hitRenderer.sharedMaterial, Is.Not.SameAs(rest), "The flash material is on the body.");
            Assert.That(HordeMaterials.IsHorde(hitRenderer.sharedMaterial), Is.True);

            HordeVatDrawer.Enabled = false;
            yield return null;
            yield return null;
            Assert.That(bodies.Where(r => r != null).Any(r => r.forceRenderingOff), Is.False, "Switched off, every body draws itself again.");
            Assert.That(HordeVatDrawer.LastBodies, Is.EqualTo(0));
            HordeVatDrawer.Enabled = true;
            yield return null;
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static int DeadBodiesKnown()
        {
            const System.Reflection.BindingFlags any = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            var drawer = typeof(HordeVatDrawer).GetField("_instance", any).GetValue(null);
            var bodies = (System.Collections.IDictionary)typeof(HordeVatDrawer).GetField("_bodies", any).GetValue(drawer);
            int dead = 0;
            foreach (var key in bodies.Keys) if ((Object)key == null) dead++;
            return dead;
        }

        /// <summary>
        /// Bug MIS AI-12. The drawer outlives scenes and kept every body it had drawn after the body was destroyed (here: an
        /// idle pool freed by EnemySpawner.TrimIdlePools). It now forgets them a frame later.
        /// </summary>
        [UnityTest]
        public IEnumerator FreedPooledBodies_AreForgottenByTheDrawer()
        {
            yield return BootIntoGameplay();
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            float idle = EnemySpawner.PoolIdleSeconds;
            try
            {
                var spawn = typeof(EnemySpawner).GetMethod("SpawnEnemy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var bodies = new System.Collections.Generic.List<GameObject>();
                for (int i = 0; i < 8; i++) bodies.Add((GameObject)spawn.Invoke(spawner, new object[] { spawner.StageElitePrefab }));
                yield return null;
                yield return null; // the drawer has met them
                Assert.That(HordeVatDrawer.KnownBodies, Is.GreaterThanOrEqualTo(8));
                foreach (var body in bodies) body.SetActive(false);
                EnemySpawner.PoolIdleSeconds = 0.2f;
                yield return WaitUntilRealtime(() => bodies.TrueForAll(b => b == null), "the idle pooled bodies to be freed");
                yield return null;
                yield return null;
                Assert.That(DeadBodiesKnown(), Is.Zero, "The drawer forgot every destroyed body.");
            }
            finally { EnemySpawner.PoolIdleSeconds = idle; }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
