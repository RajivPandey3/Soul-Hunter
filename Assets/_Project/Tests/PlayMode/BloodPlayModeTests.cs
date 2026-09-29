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
    /// Blood on enemy deaths. VS has blood on kills and a "Disable blood" option that switches it off
    /// (vampire.survivors.wiki/w/Options, added in v1.6.0); the wiki does not describe its look. A killed enemy leaves a
    /// jet of blood from its neck, away from the hero, and a splat on the floor where the jet lands that fades after about 2.4 s; nothing with Disable Blood on, and
    /// no blood from Level 7 ghosts.
    /// </summary>
    public class BloodPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator AKill_SpraysBloodFromTheNeck_AwayFromTheHero_AndLeavesASplatThatFades()
        {
            yield return BootIntoGameplay();
            var enemy = SpawnEnemyNearPlayer();
            yield return WaitForNoBlood();
            Vector3 at = enemy.transform.position;
            Vector3 away = at - PlayerController.Instance.transform.position;
            away.y = 0f;
            Bounds body = DrawnBody(enemy);
            Kill(enemy, DamageType.Normal);
            yield return null;

            var vfx = VFXPoolManager.Instance;
            Assert.That(vfx.Playing(VfxKind.EnemyDeath), Is.Not.Empty, "The soul puff still plays.");

            // Owner (2026-09-29): the blood sprays from the neck like a particle jet.
            var sprays = vfx.Playing(VfxKind.BloodSpray);
            Assert.That(sprays, Has.Count.EqualTo(1), "A kill sprays blood. " + Errors());
            var spray = sprays[0].transform;
            Assert.That(spray.position.y, Is.EqualTo(body.min.y + body.size.y * VFXPoolManager.NeckHeight).Within(0.05f),
                $"The jet starts at the neck (body {body.min.y:0.00}..{body.max.y:0.00}).");
            Assert.That(new Vector2(spray.position.x - body.center.x, spray.position.z - body.center.z).magnitude, Is.LessThan(0.05f));
            Vector3 jet = spray.up;
            Vector3 flatJet = new Vector3(jet.x, 0f, jet.z).normalized;
            Assert.That(Vector3.Dot(flatJet, away.normalized), Is.GreaterThan(Mathf.Cos((VFXPoolManager.SprayYawJitter + 1f) * Mathf.Deg2Rad)),
                "The jet points away from the hero.");
            Assert.That(Mathf.Asin(jet.y) * Mathf.Rad2Deg, Is.EqualTo(VFXPoolManager.SprayElevationDegrees).Within(1f), "The jet rises.");

            var jetParticles = spray.Find("Jet").GetComponent<ParticleSystem>();
            yield return new WaitForSeconds(0.35f);
            Assert.That(jetParticles.particleCount, Is.GreaterThan(5), "The jet is still spurting after 0.35 s.");
            Assert.That(jetParticles.isEmitting, Is.True);

            var blood = vfx.Playing(VfxKind.Blood);
            Assert.That(blood, Has.Count.EqualTo(1), "The jet leaves a splat on the floor.");
            var root = blood[0].transform.position;
            Vector3 landing = at + flatJet * VFXPoolManager.SplatOffset;
            Assert.That(new Vector2(root.x - landing.x, root.z - landing.z).magnitude, Is.LessThan(0.02f), "The splat lies where the jet lands.");
            Assert.That(root.y, Is.EqualTo(VFXPoolManager.GroundHeight(landing) + 0.03f).Within(0.01f), "The splat lies on the floor, not at body height.");
            Assert.That(root.y, Is.LessThan(at.y), "The floor is below the enemy's body.");

            foreach (var renderer in blood[0].GetComponentsInChildren<ParticleSystemRenderer>().Concat(spray.GetComponentsInChildren<ParticleSystemRenderer>()))
                Assert.That(renderer.sharedMaterial.GetFloat("_DstBlend"), Is.EqualTo((float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha),
                    $"{renderer.name} is alpha blended (blood darkens the floor; it does not glow).");

            yield return new WaitForSeconds(1.2f);
            Assert.That(sprays[0].activeSelf, Is.False, "The jet is over.");
            Assert.That(blood[0].activeSelf, Is.True, "The splat stays on the ground for a while.");
            yield return new WaitForSeconds(2f);
            Assert.That(blood[0].activeSelf, Is.False, "It fades and returns to the pool.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static Bounds DrawnBody(GameObject enemy)
        {
            var renderers = enemy.GetComponentsInChildren<Renderer>().Where(r => (r is SkinnedMeshRenderer || r is MeshRenderer) && r.enabled).ToArray();
            Assert.That(renderers, Is.Not.Empty, "The enemy has a drawn body.");
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        [UnityTest]
        public IEnumerator DisableBlood_And_Ghosts_LeaveNoBlood()
        {
            yield return BootIntoGameplay();
            var data = GameServices.Instance.Get<SaveService>().CurrentData;
            bool before = data.DisableBlood;
            try
            {
                var enemy = SpawnEnemyNearPlayer();
                yield return WaitForNoBlood();
                data.DisableBlood = true;
                GameSettings.NotifyChanged();
                Kill(enemy, DamageType.Normal);
                yield return null;
                var vfx = VFXPoolManager.Instance;
                Assert.That(vfx.Playing(VfxKind.EnemyDeath), Is.Not.Empty, "The kill still shows its soul puff.");
                Assert.That(vfx.Playing(VfxKind.Blood), Is.Empty, "VS Disable Blood: no blood.");
                Assert.That(vfx.Playing(VfxKind.BloodSpray), Is.Empty, "VS Disable Blood: no spray.");
            }
            finally
            {
                data.DisableBlood = before;
                GameSettings.NotifyChanged();
            }

            // Level 7 ghosts are spectral: they die without blood.
            var ghost = SpawnEnemyNearPlayer();
            ghost.GetComponent<EnemyController>().ConfigureDamageVulnerability(DamageType.Holy, 2f, isSpectral: true);
            yield return new WaitForSeconds(0.05f); // past the blood throttle
            Kill(ghost, DamageType.Holy);
            yield return null;
            Assert.That(VFXPoolManager.Instance.Playing(VfxKind.Blood), Is.Empty, "A ghost leaves no blood.");
            Assert.That(VFXPoolManager.Instance.Playing(VfxKind.BloodSpray), Is.Empty, "A ghost sprays no blood.");

            Kill(SpawnEnemyNearPlayer(), DamageType.Normal);
            yield return null;
            Assert.That(VFXPoolManager.Instance.Playing(VfxKind.Blood), Is.Not.Empty, "With the option off again, kills bleed.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>Kills made while the game booted (before the weapons were switched off) may still be bleeding.</summary>
        private static IEnumerator WaitForNoBlood()
        {
            float end = Time.realtimeSinceStartup + 5f;
            while (VFXPoolManager.Instance != null && (VFXPoolManager.Instance.Playing(VfxKind.Blood).Count > 0 || VFXPoolManager.Instance.Playing(VfxKind.BloodSpray).Count > 0) && Time.realtimeSinceStartup < end)
                yield return null;
            yield return new WaitForSeconds(0.05f); // past the blood throttle
        }

        private GameObject SpawnEnemyNearPlayer()
        {
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            var prefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            return Object.Instantiate(prefab, PlayerController.Instance.transform.position + Vector3.forward * 6f, Quaternion.identity);
        }

        private static void Kill(GameObject enemy, DamageType type) =>
            enemy.GetComponent<HealthController>().TakeDamage(new DamagePacket(100000, enemy.transform.position, Vector3.zero, type));
    }
}
