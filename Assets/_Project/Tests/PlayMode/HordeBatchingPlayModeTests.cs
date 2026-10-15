using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.VFX;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// CPU audit (2026-10-14): enemies draw with one horde material each, and their tint and hit flash are shared material
    /// copies, never property blocks, so the SRP Batcher keeps batching the horde.
    /// </summary>
    public class HordeBatchingPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator TintedEnemies_ShareOneTintMaterial_WithNoPropertyBlock_AndTheFlashComesBack()
        {
            yield return BootIntoGameplay();
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            LevelProgressionManager.Instance.SkipTo(12f * 60f + 1f); // Ghoul Hounds, a tinted Level 1 type
            EnemyController[] hounds = null;
            yield return WaitUntilRealtime(() => (hounds = EnemyController.ActiveEnemies
                    .Where(e => e != null && e.Data != null && e.Data.EnemyName == "Ghoul Hound" && !e.IsDefanged).Take(2).ToArray()).Length == 2,
                "two Ghoul Hounds at 12:00");
            yield return null;

            var tint = hounds[0].TypeTint.Value;
            Renderer[] drawn = hounds.Select(h => h.GetComponentsInChildren<Renderer>().First(r => r.enabled && !(r is ParticleSystemRenderer) && HordeMaterials.IsHorde(r.sharedMaterial))).ToArray();
            foreach (var r in drawn)
            {
                Assert.That(r.sharedMaterials.Length, Is.EqualTo(1), "One material: one draw call.");
                Assert.That(r.HasPropertyBlock(), Is.False, "No property block, so the SRP Batcher keeps the body.");
                Assert.That(r.sharedMaterial.GetFloat("_TintAmount"), Is.EqualTo(1f));
                Color c = r.sharedMaterial.GetColor("_Color");
                Assert.That(Mathf.Abs(c.r - tint.r) + Mathf.Abs(c.g - tint.g) + Mathf.Abs(c.b - tint.b), Is.LessThan(1e-3f), $"Drawn in its type's tint {tint}, got {c}.");
            }
            Assert.That(drawn[1].sharedMaterial, Is.SameAs(drawn[0].sharedMaterial), "Both hounds share one tint material.");

            var flash = hounds[0].GetComponent<DamageFlash>();
            var rest = drawn[0].sharedMaterial;
            hounds[0].GetComponent<HealthController>().TakeDamage(new DamagePacket(1, hounds[0].transform.position, Vector3.zero));
            Assert.That(flash.IsFlashing, Is.True, "A hit flashes.");
            Assert.That(drawn[0].sharedMaterial, Is.Not.SameAs(rest), "The flash is another shared material.");
            Assert.That(drawn[0].HasPropertyBlock(), Is.False, "Still no property block while flashing.");
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(flash.IsFlashing, Is.False);
            Assert.That(drawn[0].sharedMaterial, Is.SameAs(rest), "Back to the tint after the flash.");
            Assert.That(flash.RestMaterial(drawn[0]), Is.SameAs(rest), "A dying body copies the rest look, not the flash.");
            Assert.That(DamageFlashRunner.Count, Is.EqualTo(0), "Only flashing bodies are ticked, and an ended flash leaves the runner.");

            // Death's Shadow darkens a body's own colours through a shared shaded material, over the type's tint.
            var shade = new Color(0.16f, 0.1f, 0.22f, 1f);
            flash.SetShade(shade);
            Assert.That(drawn[0].HasPropertyBlock(), Is.False, "The shade is a material, not a property block.");
            Assert.That(drawn[0].sharedMaterial.GetFloat("_TintAmount"), Is.EqualTo(0f), "The shade keeps each part's colour.");
            Color shaded = drawn[0].sharedMaterial.GetColor("_Color");
            Assert.That(Mathf.Abs(shaded.r - shade.r) + Mathf.Abs(shaded.g - shade.g) + Mathf.Abs(shaded.b - shade.b), Is.LessThan(1e-3f));
            flash.SetShade(null);
            Assert.That(drawn[0].sharedMaterial, Is.SameAs(rest), "Without the shade the body wears its tint again.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator BobbingOnlyOnScreen_CostsLessThanTheOldCircle_WithFourHundredGems()
        {
            yield return BootIntoGameplay();
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            var hero = PlayerController.Instance.transform.position;
            var pool = SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance;
            int placed = 0;
            for (int x = -10; x < 10 && placed < 400; x++)
                for (int z = -10; z < 10 && placed < 400; z++)
                {
                    var at = hero + new Vector3(x * 3f + 1.5f, 0f, z * 3f + 1.5f);
                    if ((at - hero).magnitude < 7f) continue; // out of the hero's reach
                    pool.SpawnGem(at, 10);
                    placed++;
                }
            yield return null;
            int listed = PickupIdleMotionRunner.Count;
            Assert.That(listed, Is.GreaterThanOrEqualTo(300), "Hundreds of gems bob on the ground.");

            // The same gems, the same frame, both ways, alternating so neither gets a warmer cache.
            var watch = new System.Diagnostics.Stopwatch();
            double circle = 0, screen = 0;
            for (int round = 0; round < 200; round++)
            {
                PickupIdleMotionRunner.ScreenCulling = (round & 1) == 0;
                watch.Restart();
                PickupIdleMotionRunner.StepAll(Time.time, 0.016f);
                watch.Stop();
                if (PickupIdleMotionRunner.ScreenCulling) screen += watch.Elapsed.TotalMilliseconds; else circle += watch.Elapsed.TotalMilliseconds;
            }
            PickupIdleMotionRunner.ScreenCulling = true;
            Debug.Log($"[HordeBatching] {listed} bobbing pickups: old circle {circle / 100:F4} ms, on screen only {screen / 100:F4} ms a frame.");
            Assert.That(screen, Is.LessThan(circle), $"Screen-only bobbing ({screen / 100:F4} ms) costs less than the 30-unit circle ({circle / 100:F4} ms).");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator BobbingPickups_MoveOnScreen_AndStandStillOffScreen()
        {
            const System.Reflection.BindingFlags All = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            Assert.That(typeof(SoulHunter.Gameplay.Pickups.XPGem).GetMethod("Update", All), Is.Null, "GemRunner ticks every gem.");
            Assert.That(typeof(DamageFlash).GetMethod("Update", All), Is.Null, "DamageFlashRunner ticks only flashing bodies.");
            yield return BootIntoGameplay();
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            yield return null;
            var hero = PlayerController.Instance.transform.position;
            Assert.That(PickupIdleMotionRunner.ViewOnGround(Camera.main, hero.y, out var min, out var max), Is.True, "The camera sees the ground.");
            var pool = SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance;
            // Both inside the old 30-unit circle; only one is on screen.
            var onScreen = pool.SpawnChicken(new Vector3(hero.x + 2f, hero.y, hero.z));
            var offScreen = pool.SpawnChicken(new Vector3(max.x + 3f, hero.y, hero.z));
            Assert.That(Vector3.Distance(offScreen.transform.position, hero), Is.LessThan(PickupIdleMotionRunner.ActiveRadius), "The off-screen one is near enough that the old circle moved it.");
            var a = onScreen.GetComponentInChildren<PickupIdleMotion>().Visual;
            var b = offScreen.GetComponentInChildren<PickupIdleMotion>().Visual;
            yield return null;
            Vector3 aStart = a.localPosition; Quaternion bStart = b.localRotation; Vector3 bPos = b.localPosition;
            yield return new WaitForSeconds(0.3f);
            Assert.That(Vector3.Distance(a.localPosition, aStart), Is.GreaterThan(1e-4f), "A pickup on screen bobs.");
            Assert.That(b.localRotation, Is.EqualTo(bStart), "A pickup off screen does not turn.");
            Assert.That(b.localPosition, Is.EqualTo(bPos), "A pickup off screen does not bob.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
