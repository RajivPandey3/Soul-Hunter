using System.Collections;
using System.Collections.Generic;
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
    /// Every evolved and union weapon in the real game. Found in a 30-minute editor run (2026-09-28): Death Spiral threw a
    /// NullReferenceException on every attack (its scythe model had no collider, so AddComponent&lt;Projectile&gt;
    /// returned null), and because the cooldown was set after the attack it retried every frame: 30,000 errors and the
    /// frame rate fell from 80 to 20. Base weapons had a sweep (WeaponSweepPlayModeTests); evolutions had none.
    /// </summary>
    public class EvolvedWeaponSweepPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator EveryEvolutionAndUnion_HurtsEnemies_WithoutErrors()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var existing in player.GetComponentsInChildren<AutoAttackWeapon>()) existing.gameObject.SetActive(false);

            var manager = WeaponManager.Instance;
            var evolutions = GetPrivateField<List<WeaponEvolutionData>>(manager, "_availableEvolutions") ?? new List<WeaponEvolutionData>();
            var unions = GetPrivateField<List<WeaponUnionData>>(manager, "_availableUnions") ?? new List<WeaponUnionData>();
            var prefabs = evolutions.Where(e => e != null && e.EvolvedWeaponPrefab != null).Select(e => (e.EvolvedName, e.EvolvedWeaponPrefab))
                .Concat(unions.Where(u => u != null && u.UnionWeaponPrefab != null).Select(u => (u.UnionName, u.UnionWeaponPrefab)))
                .ToList();
            Assert.That(prefabs.Count, Is.GreaterThan(0), "The game has evolutions.");

            var enemyPrefab = GetPrivateField<List<GameObject>>(Object.FindFirstObjectByType<EnemySpawner>(), "_stageEnemyPrefabs")[0];
            var failures = new List<string>();
            foreach (var (name, prefab) in prefabs)
            {
                var weapon = Object.Instantiate(prefab, player.transform);
                var enemies = new List<HealthController>();
                for (int i = 0; i < 8; i++)
                {
                    var enemy = Object.Instantiate(enemyPrefab, player.transform.position + Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * 3f, Quaternion.identity);
                    var touch = enemy.GetComponent<TouchDamage>();
                    if (touch != null) touch.enabled = false;
                    var health = enemy.GetComponent<HealthController>();
                    health.Initialize(100000);
                    enemies.Add(health);
                }
                int errorsBefore = Errors() == "(no errors logged)" ? 0 : Errors().Split('\n').Length;
                bool hurt = false;
                float deadline = Time.realtimeSinceStartup + 10f;
                while (!hurt && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                    hurt = enemies.Any(h => h == null || !h.gameObject.activeInHierarchy || h.CurrentHealth < h.MaxHealth);
                }
                int errorsAfter = Errors() == "(no errors logged)" ? 0 : Errors().Split('\n').Length;
                if (!hurt) failures.Add($"{name} never hurt an enemy 3 units away within 10 s.");
                if (errorsAfter > errorsBefore) failures.Add($"{name} logged errors.");
                Object.Destroy(weapon);
                foreach (var h in enemies) if (h != null) Object.Destroy(h.gameObject);
                yield return null;
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures) + "\n" + Errors());
        }
    }
}
