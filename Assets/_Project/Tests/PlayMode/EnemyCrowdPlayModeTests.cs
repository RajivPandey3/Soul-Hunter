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
    /// Owner report (2026-09-26): the horde chased the player in single file, "rail ka dabba", where VS shows a wide
    /// crowd. Real game: 40 enemies chase a player who keeps walking in one direction; with crowd separation the
    /// horde must be wide and must not stack, and the same run without it (the old behaviour) is measured for contrast.
    /// </summary>
    public class EnemyCrowdPlayModeTests : GameplayPlayModeTestBase
    {
        private const int Count = 40;

        private struct Shape
        {
            public float Width;          // spread across the walking direction (standard deviation, units)
            public float StackedFraction; // enemies whose nearest neighbour is closer than one body radius
            public int Measured;
            public override string ToString() => $"width {Width:0.00}, stacked {StackedFraction:P0} of {Measured}";
        }

        private Shape _shape;

        private IEnumerator Chase(bool crowd)
        {
            EnemyCrowd.Enabled = crowd;
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            // Deactivate (back to the spawner's pools), not Destroy: pooled enemies are reused.
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            yield return null;

            // A loose pack behind the player, like a spawn from one side.
            var prefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            Random.InitState(1234);
            Vector3 start = player.transform.position;
            var pack = new List<EnemyController>();
            for (int i = 0; i < Count; i++)
            {
                Vector2 jitter = Random.insideUnitCircle * 3f;
                var enemy = Object.Instantiate(prefab, start + new Vector3(-10f + jitter.x, 0f, jitter.y), Quaternion.identity);
                var touch = enemy.GetComponent<TouchDamage>();
                if (touch != null) touch.DamageAmount = 0;
                var controller = enemy.GetComponent<EnemyController>();
                controller.Target = player.transform;
                pack.Add(controller);
            }

            // The player walks steadily along +X, a little slower than the horde, for 8 s.
            float speed = pack[0].MoveSpeed * 0.8f;
            float until = Time.time + 8f;
            while (Time.time < until)
            {
                yield return new WaitForFixedUpdate();
                Vector3 next = player.Rigidbody.position + Vector3.right * (speed * Time.fixedDeltaTime);
                player.Rigidbody.position = next;
                player.transform.position = next;
            }

            var positions = pack.Where(e => e != null && e.isActiveAndEnabled).Select(e => e.transform.position).ToList();
            float meanZ = positions.Average(p => p.z);
            float radius = pack[0].CrowdRadius;
            int stacked = positions.Count(p => positions.Where(q => q != p)
                .Min(q => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(q.x, q.z))) < radius);
            _shape = new Shape
            {
                Width = Mathf.Sqrt(positions.Average(p => (p.z - meanZ) * (p.z - meanZ))),
                StackedFraction = stacked / (float)positions.Count,
                Measured = positions.Count,
            };
            foreach (var e in pack) if (e != null) Object.Destroy(e.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ChasingHorde_FormsAWideCrowd_NotASingleFileTrain()
        {
            yield return BootIntoGameplay();
            yield return Chase(crowd: false);
            var before = _shape;
            yield return Chase(crowd: true);
            var after = _shape;
            EnemyCrowd.Enabled = true;
            string report = $"Without separation (old): {before}. With separation: {after}. ";
            Debug.Log("[EnemyCrowdPlayModeTests] " + report);

            Assert.That(after.Measured, Is.EqualTo(Count), "The pack stays alive for the check. " + report + Errors());
            Assert.That(after.StackedFraction, Is.LessThan(0.15f), "Enemies must not stack on each other. " + report);
            // 40 bodies of radius 0.5 chasing from one side form a trailing pack; a width (standard deviation) of
            // 0.75 is about three bodies side by side. The old chase measured 0.07 with every enemy stacked.
            Assert.That(after.Width, Is.GreaterThan(0.75f), "The horde must be a wide crowd, not a line. " + report);
            Assert.That(after.Width, Is.GreaterThan(before.Width * 5f), "Much wider than the old single-file chase. " + report);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTearDown]
        public IEnumerator RestoreCrowd()
        {
            EnemyCrowd.Enabled = true;
            yield return null;
        }
    }
}
