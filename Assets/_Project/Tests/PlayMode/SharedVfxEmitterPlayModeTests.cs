using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.VFX;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Draw calls (2026-10-15): with SharedVfxEmitter on, the blood neck sprays and death puffs of every kill play through
    /// one world-space copy of each of their particle systems, so all blood is one draw. The rules of BloodPlayModeTests
    /// hold there: 300 particles from the neck, away from the hero and rising, the shared budget when many die at once,
    /// nothing with Disable Blood on or from a ghost; and switching it off goes back to the pooled effects.
    /// </summary>
    public class SharedVfxEmitterPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator SharedSprays_Emit300FromTheNeck_InOneSystem_AndShareTheBudget()
        {
            yield return BootIntoGameplay();
            SharedVfxEmitter.Enabled = true;
            try
            {
                // Warm-up: the first shared spray makes its systems, and that frame can outlast its first particles.
                Kill(SpawnEnemyNearPlayer(5), DamageType.Normal);
                yield return null;
                var enemy = SpawnEnemyNearPlayer();
                yield return WaitForQuiet();
                int start = SharedVfxEmitter.Current != null ? SharedVfxEmitter.Current.Emitted(VfxKind.BloodSpray) : 0;
                Vector3 away = enemy.transform.position - PlayerController.Instance.transform.position;
                away.y = 0f;
                Bounds body = DrawnBody(enemy);
                Kill(enemy, DamageType.Normal);
                yield return null;

                var shared = SharedVfxEmitter.Current;
                Assert.That(shared, Is.Not.Null, "The shared emitter is made on the pool manager. " + Errors());
                Assert.That(VFXPoolManager.Instance.Playing(VfxKind.BloodSpray), Is.Empty, "No pooled spray is used.");
                Assert.That(VFXPoolManager.Instance.Playing(VfxKind.EnemyDeath), Is.Empty, "No pooled puff is used.");
                var bloodSystems = shared.Systems(VfxKind.BloodSpray);
                Assert.That(bloodSystems, Has.Count.EqualTo(1), "One shared blood system (no ground splat).");
                Assert.That(shared.Systems(VfxKind.EnemyDeath), Has.Count.EqualTo(2), "The soul puff's two systems are shared too.");
                Assert.That(shared.LiveCount(VfxKind.BloodSpray), Is.EqualTo(1));
                Assert.That(shared.LiveCount(VfxKind.EnemyDeath), Is.GreaterThanOrEqualTo(1), "The soul puff still plays.");
                Assert.That(shared.LastScheduled(VfxKind.BloodSpray), Is.EqualTo(VFXPoolManager.SprayParticles), "A lone kill gets 300.");
                Assert.That(VFXPoolManager.Instance.PooledCount(VfxKind.EnemyDeath), Is.GreaterThan(0), "The death effect counts as shown.");

                Assert.That(shared.LastPose(VfxKind.BloodSpray, out var at, out var rotation), Is.True);
                Assert.That(at.y, Is.EqualTo(body.min.y + body.size.y * VFXPoolManager.NeckHeight).Within(0.05f), "The jet starts at the neck.");
                Assert.That(new Vector2(at.x - body.center.x, at.z - body.center.z).magnitude, Is.LessThan(0.05f));
                Vector3 jet = rotation * Vector3.up;
                Vector3 flatJet = new Vector3(jet.x, 0f, jet.z).normalized;
                Assert.That(Vector3.Dot(flatJet, away.normalized), Is.GreaterThan(Mathf.Cos((VFXPoolManager.SprayYawJitter + 1f) * Mathf.Deg2Rad)),
                    "The jet points away from the hero.");
                Assert.That(Mathf.Asin(jet.y) * Mathf.Rad2Deg, Is.EqualTo(VFXPoolManager.SprayElevationDegrees).Within(1f), "The jet rises.");
                var renderer = bloodSystems[0].GetComponent<ParticleSystemRenderer>();
                Assert.That(renderer.sharedMaterial.GetFloat("_DstBlend"), Is.EqualTo((float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha), "Alpha blended.");
                Assert.That(bloodSystems[0].main.simulationSpace, Is.EqualTo(ParticleSystemSimulationSpace.World));

                // Every particle the spray emits, and where: near the neck when born, rising away from the hero.
                var buffer = new ParticleSystem.Particle[VFXPoolManager.SprayParticles * 4];
                var seen = new HashSet<uint>();
                int peak = 0, wrongWay = 0;
                float end = Time.time + 1.6f;
                while (Time.time < end)
                {
                    int alive = bloodSystems[0].GetParticles(buffer);
                    for (int i = 0; i < alive; i++)
                        if (seen.Add(buffer[i].randomSeed) && Vector3.Dot(buffer[i].velocity.normalized, jet) < 0.5f) wrongWay++;
                    peak = Mathf.Max(peak, alive);
                    yield return null;
                }
                Assert.That(shared.Emitted(VfxKind.BloodSpray) - start, Is.EqualTo(VFXPoolManager.SprayParticles), "The spray has 300 particles.");
                Assert.That(seen.Count, Is.EqualTo(VFXPoolManager.SprayParticles), "300 particles were in the shared system.");
                Assert.That(peak, Is.GreaterThan(100), "Many are in the air together.");
                Assert.That(wrongWay, Is.LessThan(30), "Born flying along the jet (its cone).");
                Assert.That(bloodSystems[0].particleCount, Is.EqualTo(0), "All gone.");
                Assert.That(shared.LiveCount(VfxKind.BloodSpray), Is.EqualTo(0), "The spray is over.");

                // Many kills at once share the budget, as the pooled sprays do; a lone kill gets 300 again.
                var enemies = new List<GameObject>();
                for (int i = 0; i < 20; i++) enemies.Add(SpawnEnemyNearPlayer(i));
                yield return WaitForQuiet();
                var counts = new List<int>();
                foreach (var e in enemies)
                {
                    Kill(e, DamageType.Normal);
                    yield return new WaitForSeconds(0.02f); // past the blood throttle
                    counts.Add(shared.LastScheduled(VfxKind.BloodSpray));
                }
                for (int i = 0; i < VFXPoolManager.FullSprays; i++)
                    Assert.That(counts[i], Is.EqualTo(300), $"Kill {i + 1} gets all 300. Counts: {string.Join(",", counts)}");
                Assert.That(counts[counts.Count - 1], Is.LessThan(300).And.GreaterThanOrEqualTo(VFXPoolManager.MinSprayParticles),
                    $"With many sprays in the air a new one gets fewer. Counts: {string.Join(",", counts)}");
                Assert.That(shared.Systems(VfxKind.BloodSpray), Has.Count.EqualTo(1), "Twenty sprays, still one system.");
                Assert.That(VFXPoolManager.Instance.Playing(VfxKind.BloodSpray), Is.Empty);
                yield return WaitForQuiet();
                Kill(SpawnEnemyNearPlayer(0), DamageType.Normal);
                yield return null;
                Assert.That(shared.LastScheduled(VfxKind.BloodSpray), Is.EqualTo(300), "A lone kill gets all 300 again.");
                Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
            }
            finally { SharedVfxEmitter.Enabled = false; }
        }

        [UnityTest]
        public IEnumerator SharedSprays_FollowDisableBlood_AndGhosts_AndSwitchBackToThePool()
        {
            yield return BootIntoGameplay();
            SharedVfxEmitter.Enabled = true;
            var data = GameServices.Instance.Get<SaveService>().CurrentData;
            bool before = data.DisableBlood;
            try
            {
                var enemy = SpawnEnemyNearPlayer();
                yield return WaitForQuiet();
                data.DisableBlood = true;
                GameSettings.NotifyChanged();
                Kill(enemy, DamageType.Normal);
                yield return null;
                var shared = SharedVfxEmitter.Current;
                Assert.That(shared, Is.Not.Null);
                Assert.That(shared.LiveCount(VfxKind.EnemyDeath), Is.GreaterThanOrEqualTo(1), "The kill still shows its soul puff.");
                Assert.That(shared.LiveCount(VfxKind.BloodSpray), Is.EqualTo(0), "VS Disable Blood: no spray.");
                data.DisableBlood = before;
                GameSettings.NotifyChanged();

                var ghost = SpawnEnemyNearPlayer();
                ghost.GetComponent<EnemyController>().ConfigureDamageVulnerability(DamageType.Holy, 2f, isSpectral: true);
                yield return WaitForQuiet();
                Kill(ghost, DamageType.Holy);
                yield return null;
                Assert.That(shared.LiveCount(VfxKind.BloodSpray), Is.EqualTo(0), "A ghost sprays no blood.");

                // Switched off: kills use the pooled effects again, and nothing is left emitting.
                SharedVfxEmitter.Enabled = false;
                var other = SpawnEnemyNearPlayer(3);
                yield return new WaitForSeconds(0.05f);
                int emitted = shared.Emitted(VfxKind.BloodSpray);
                Kill(other, DamageType.Normal);
                yield return null;
                Assert.That(VFXPoolManager.Instance.Playing(VfxKind.BloodSpray), Is.Not.Empty, "Off: the pooled spray plays.");
                Assert.That(shared.Emitted(VfxKind.BloodSpray), Is.EqualTo(emitted), "Off: the shared system emits nothing new.");
                Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
            }
            finally
            {
                SharedVfxEmitter.Enabled = false;
                data.DisableBlood = before;
                GameSettings.NotifyChanged();
            }
        }

        private static IEnumerator WaitForQuiet()
        {
            float end = Time.realtimeSinceStartup + 5f;
            while (Time.realtimeSinceStartup < end)
            {
                var pool = VFXPoolManager.Instance;
                var shared = SharedVfxEmitter.Current;
                bool busy = pool != null && pool.Playing(VfxKind.BloodSpray).Count > 0;
                busy |= shared != null && shared.LiveCount(VfxKind.BloodSpray) > 0;
                if (!busy) break;
                yield return null;
            }
            yield return new WaitForSeconds(0.05f); // past the blood throttle
        }

        private static Bounds DrawnBody(GameObject enemy)
        {
            var renderers = enemy.GetComponentsInChildren<Renderer>().Where(r => (r is SkinnedMeshRenderer || r is MeshRenderer) && r.enabled).ToArray();
            Assert.That(renderers, Is.Not.Empty, "The enemy has a drawn body.");
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        private GameObject SpawnEnemyNearPlayer(int ring = 0)
        {
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            var prefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            var offset = Quaternion.Euler(0f, ring * 18f, 0f) * Vector3.forward * 6f;
            return Object.Instantiate(prefab, PlayerController.Instance.transform.position + offset, Quaternion.identity);
        }

        private static void Kill(GameObject enemy, DamageType type) =>
            enemy.GetComponent<HealthController>().TakeDamage(new DamagePacket(100000, enemy.transform.position, Vector3.zero, type));
    }
}
