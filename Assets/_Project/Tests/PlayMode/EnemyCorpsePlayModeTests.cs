using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.VFX;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS-style death (owner, 2026-09-29: the body stays briefly). A killed enemy returns to the pool at once, but a still
    /// copy of its body stays where it died and sinks into the ground for 0.7 s, while the neck blood sprays, then goes.
    /// At most 32 at once.
    /// </summary>
    public class EnemyCorpsePlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator AKilledEnemy_LeavesItsBodyToSink_WhileTheBloodSprays_ThenItIsGone()
        {
            yield return BootIntoGameplay();
            var enemy = QuietArenaWithOneEnemy(out var prefab);
            yield return null;
            var drawn = enemy.GetComponentsInChildren<Renderer>().Where(r => (r is SkinnedMeshRenderer || r is MeshRenderer) && r.enabled).ToList();
            Assert.That(drawn, Is.Not.Empty);
            Vector3 at = enemy.transform.position;
            Kill(enemy);
            Assert.That(enemy.activeSelf, Is.False, "The enemy itself returns to the pool at once (kills and spawning unchanged).");
            var corpse = EnemyCorpses.LastStarted;
            Assert.That(corpse, Is.Not.Null, "The body stays for a moment. " + Errors());
            Assert.That(EnemyCorpses.Active, Is.EqualTo(1));
            Assert.That(new Vector2(corpse.position.x - at.x, corpse.position.z - at.z).magnitude, Is.LessThan(1f), "Where the enemy died.");
            var parts = corpse.GetComponentsInChildren<MeshRenderer>();
            Assert.That(parts.Length, Is.EqualTo(Mathf.Min(drawn.Count, EnemyCorpses.MaxPartsPerCorpse)), "Every drawn mesh is copied.");
            Assert.That(parts.All(p => p.GetComponent<MeshFilter>().sharedMesh != null && p.sharedMaterials.Length > 0), Is.True);
            float startY = corpse.position.y;

            yield return new WaitForSeconds(0.5f);
            Assert.That(corpse.gameObject.activeSelf, Is.True, "Still there while the blood sprays (its last spurt starts at 0.5 s).");
            Assert.That(corpse.position.y, Is.LessThan(startY - 0.1f), "It sinks into the ground.");
            Assert.That(corpse.localScale.x, Is.LessThan(1f), "And shrinks.");

            yield return new WaitForSeconds(0.35f);
            Assert.That(corpse.gameObject.activeSelf, Is.False, "Gone by 0.85 s.");
            Assert.That(EnemyCorpses.Active, Is.EqualTo(0));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator AHordeOfKills_KeepsToTheCorpseBudget()
        {
            yield return BootIntoGameplay();
            QuietArenaWithOneEnemy(out var prefab).SetActive(false);
            var origin = PlayerController.Instance.transform.position + Vector3.forward * 20f;
            for (int i = 0; i < 60; i++)
                Kill(Object.Instantiate(prefab, origin + new Vector3(i % 10, 0f, i / 10) * 2f, Quaternion.identity));
            Assert.That(EnemyCorpses.Active, Is.EqualTo(EnemyCorpses.MaxCorpses), "At most 32 bodies sink at once; the rest vanish as before.");
            yield return new WaitForSeconds(EnemyCorpses.LingerSeconds + 0.2f);
            Assert.That(EnemyCorpses.Active, Is.EqualTo(0));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private GameObject QuietArenaWithOneEnemy(out GameObject prefab)
        {
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList()) if (stray != null) stray.gameObject.SetActive(false);
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            prefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            return Object.Instantiate(prefab, PlayerController.Instance.transform.position + Vector3.forward * 6f, Quaternion.identity);
        }

        private static void Kill(GameObject enemy) =>
            enemy.GetComponent<HealthController>().TakeDamage(new DamagePacket(1000000, enemy.transform.position, Vector3.zero, DamageType.Normal));
    }
}
