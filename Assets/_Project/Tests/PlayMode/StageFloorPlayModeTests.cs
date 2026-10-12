using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Environment;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS-style stage ground in the real game: the map is tiled with the current stage's floor without gaps,
    /// it switches when the next stage starts, the showcase scene's own ground is hidden, and the player
    /// stands on it.
    /// </summary>
    public class StageFloorPlayModeTests : GameplayPlayModeTestBase
    {
        private static IEnumerator WaitSeconds(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        [UnityTest]
        public IEnumerator Map_UsesTheStageFloor_WithoutGaps_AndSwitchesWithTheStage()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner != null) spawner.enabled = false;
            var map = Object.FindFirstObjectByType<InfiniteMap>();
            Assert.IsNotNull(map, "InfiniteMap missing. " + Errors());
            var floors = GameContentCatalog.Load().StageFloors;

            Assert.That(map.CurrentFloorPrefab, Is.SameAs(floors[0]), "Level 1 floor. " + Errors());
            AssertGridHasNoGaps(map);

            yield return WaitSeconds(2f); // showcase scene loads additively
            AssertNoShowcaseGroundActive();
            var player = PlayerController.Instance;
            Assert.That(player.transform.position.y, Is.GreaterThan(-0.5f), "Player stands on the floor. " + Errors());

            LevelProgressionManager.Instance.StartStage(2);
            yield return null;
            Assert.That(map.CurrentFloorPrefab, Is.SameAs(floors[1]), "Level 2 floor after the stage changes. " + Errors());
            AssertGridHasNoGaps(map);
            yield return WaitSeconds(2f);
            AssertNoShowcaseGroundActive();
            Assert.That(player.transform.position.y, Is.GreaterThan(-0.5f), "Player still stands after the swap. " + Errors());
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static void AssertGridHasNoGaps(InfiniteMap map)
        {
            var xs = new System.Collections.Generic.SortedSet<float>();
            var zs = new System.Collections.Generic.SortedSet<float>();
            for (int x = 0; x < InfiniteMap.GridSize; x++)
                for (int z = 0; z < InfiniteMap.GridSize; z++)
                {
                    var chunk = map.ChunkAt(x, z);
                    Assert.That(chunk, Is.Not.Null);
                    xs.Add(Mathf.Round(chunk.transform.position.x * 100f) / 100f);
                    zs.Add(Mathf.Round(chunk.transform.position.z * 100f) / 100f);
                }
            Assert.That(map.ChunkSize, Is.EqualTo(20f), "Chunk spacing must equal the 20-unit floor tile.");
            foreach (var axis in new[] { xs.ToArray(), zs.ToArray() })
            {
                Assert.That(axis.Length, Is.EqualTo(InfiniteMap.GridSize));
                for (int i = 1; i < axis.Length; i++) Assert.That(axis[i] - axis[i - 1], Is.EqualTo(20f).Within(.01f));
            }
        }

        private void AssertNoShowcaseGroundActive()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || !scene.name.EndsWith("_Showcase")) continue;
                // The showcase's props and still Enemy/Elite/Boss display models are hidden: the stage floor
                // has its own obstacles, and the statues stood at the spawn point like enemies that never die.
                var shown = scene.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<Transform>())
                    .Where(t => t.gameObject.activeInHierarchy && t.name.StartsWith("SH10_L"))
                    .Select(t => t.name).ToList();
                Assert.That(shown, Is.Empty, $"{scene.name} shows showcase decor in play. " + Errors());
            }
        }

        [UnityTest]
        public IEnumerator StageObstacles_BlockThePlayer()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner != null) spawner.enabled = false;
            yield return WaitSeconds(1f);
            var player = PlayerController.Instance;
            int mask = 1 << LayerMask.NameToLayer("Obstacle");

            // Find an obstacle face and a free spot 3 units in front of it, then walk straight into the face.
            Vector3 start = default, direction = default;
            const float distance = 3f;
            bool found = false;
            var origin = player.transform.position;
            for (int x = -8; x <= 8 && !found; x += 2)
                for (int z = -8; z <= 8 && !found; z += 2)
                    for (int a = 0; a < 8 && !found; a++)
                    {
                        var probe = new Vector3(origin.x + x, origin.y + 0.5f, origin.z + z);
                        var dir = Quaternion.Euler(0f, a * 45f, 0f) * Vector3.forward;
                        if (!Physics.Raycast(probe, dir, out var hit, 8f, mask) || Mathf.Abs(hit.normal.y) > 0.1f) continue;
                        var normal = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;
                        var spot = new Vector3(hit.point.x, origin.y, hit.point.z) + normal * distance;
                        // Blockers are mesh colliders, so a sphere wholly inside one is not reported: also look down on the spot.
                        if (Physics.CheckSphere(spot + Vector3.up, 1f, mask) ||
                            Physics.Raycast(spot + Vector3.up * 20f, Vector3.down, 21f, mask)) continue;
                        // A flat face wider than the player, so it cannot slide round the edge (it may round a small one, as in VS).
                        var side = Vector3.Cross(Vector3.up, normal) * 0.55f; // wider than the 0.8 capsule
                        bool flat = true;
                        foreach (var offset in new[] { Vector3.zero, side, -side })
                            flat &= Physics.Raycast(spot + offset + Vector3.up * 0.5f, -normal, out var faceHit, 5f, mask) &&
                                    Mathf.Abs(faceHit.distance - distance) < 0.1f;
                        if (!flat) continue;
                        start = spot; direction = -normal; found = true;
                    }
            Assert.That(found, Is.True, "No stage obstacle near the player. " + Errors());

            var body = player.Rigidbody;
            body.position = start;
            player.transform.position = start;
            yield return new WaitForFixedUpdate();
            // Walk into the obstacle: camera-relative input that points along the world direction.
            var right = player.GetCameraRelativeMovement(Vector2.right);
            var forward = player.GetCameraRelativeMovement(Vector2.up);
            var input = new Vector2(Vector3.Dot(direction, right), Vector3.Dot(direction, forward)).normalized;
            var field = typeof(PlayerController).GetField("_eventBusInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(player, input);
            float maxY = start.y;
            int framesInside = 0;
            for (float end = Time.realtimeSinceStartup + 2f; Time.realtimeSinceStartup < end;) // 5 units/s would carry the player far past the obstacle
            {
                yield return null;
                maxY = Mathf.Max(maxY, player.transform.position.y);
                // Inside a blocker: its top is over the hero's centre (blockers are surfaces only, so look down on it).
                if (Physics.Raycast(body.position + Vector3.up * 20f, Vector3.down, 20f, mask)) framesInside++;
            }
            field.SetValue(player, Vector2.zero);

            var travelled = Vector3.ProjectOnPlane(player.transform.position - start, Vector3.up);
            var state = typeof(PlayerController).GetField("_currentState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(player);
            string info = $"maxY {maxY} faceBlocker {Physics.Raycast(start + Vector3.up * 0.5f, direction, out var face, 5f, mask)} {face.collider?.transform.parent?.name} bounds {face.collider?.bounds} start {start} dir {direction} input {input} now {player.transform.position} timeScale {Time.timeScale} " +
                          $"state {state?.GetType().Name} kinematic {body.isKinematic} constraints {body.constraints} " +
                          $"overlaps [{string.Join(", ", Physics.OverlapSphere(start + Vector3.up * 0.5f, 0.6f).Select(c => c.name + "/" + LayerMask.LayerToName(c.gameObject.layer)))}] ";
            Assert.That(travelled.magnitude, Is.GreaterThan(0.5f), "The player walked. " + info + Errors());
            // Slippery obstacles (owner, 2026-10-12): the hero may slide along the face and round its end, but never
            // goes through it.
            Assert.That(framesInside, Is.EqualTo(0), "The player never walked into the obstacle. " + info + Errors());
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
