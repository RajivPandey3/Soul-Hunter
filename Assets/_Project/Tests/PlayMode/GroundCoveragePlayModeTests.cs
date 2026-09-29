using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Environment;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner report (2026-09-30): "ground late se spawn ho raha hai". Wherever the hero walks, every corner and edge of
    /// the screen must show a floor tile, never the bare background (the 3 x 3 grid left the screen edge bare near a
    /// tile edge; the grid is now 5 x 5).
    /// </summary>
    public class GroundCoveragePlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator TheWholeScreen_ShowsFloor_WhereverTheHeroWalks()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList()) if (stray != null) stray.gameObject.SetActive(false);
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            var map = Object.FindFirstObjectByType<InfiniteMap>();
            var cam = Camera.main;
            Assert.That(map, Is.Not.Null);
            Assert.That(cam, Is.Not.Null);
            // A batchmode window is narrow; test a wide 21:9 window (the widest view a player is likely to have).
            cam.aspect = 21f / 9f;

            // Steps that end near tile edges (every 20 units, +/-9.5) in every direction, and further away.
            var stops = new[]
            {
                new Vector3(9.5f, 0f, 0f), new Vector3(29.5f, 0f, 0f), new Vector3(49.5f, 0f, 9.5f), new Vector3(70f, 0f, -29.5f),
                new Vector3(-10.5f, 0f, -49.5f), new Vector3(-69.5f, 0f, 30f), new Vector3(-30f, 0f, 69.5f), new Vector3(9.5f, 0f, 9.5f),
            };
            var viewport = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            };
            var body = player.GetComponent<Rigidbody>();
            Vector3 origin = player.transform.position;
            foreach (var stop in stops)
            {
                Vector3 target = new Vector3(origin.x + stop.x, origin.y, origin.z + stop.z);
                if (body != null) body.position = target;
                player.transform.position = target;
                // The camera follows smoothly and the map moves its tiles in Update.
                yield return new WaitForSecondsRealtime(0.6f);
                foreach (var v in viewport)
                {
                    var ray = cam.ViewportPointToRay(new Vector3(v.x, v.y, 0f));
                    var ground = new Plane(Vector3.up, new Vector3(0f, FloorTop(map), 0f));
                    Assert.That(ground.Raycast(ray, out float distance), Is.True);
                    Vector3 point = ray.GetPoint(distance);
                    Assert.That(IsOnATile(map, point), Is.True,
                        $"Screen point {v} at {point} shows no floor while the hero stands at {target - origin} from the start.");
                }
            }
            cam.ResetAspect();
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static float FloorTop(InfiniteMap map)
        {
            var tile = map.ChunkAt(0, 0);
            var box = tile != null ? tile.GetComponentInChildren<BoxCollider>() : null;
            return box != null ? box.bounds.max.y : 0f;
        }

        private static bool IsOnATile(InfiniteMap map, Vector3 point)
        {
            float half = map.ChunkSize * 0.5f + 0.01f;
            for (int x = 0; x < InfiniteMap.GridSize; x++)
                for (int z = 0; z < InfiniteMap.GridSize; z++)
                {
                    var tile = map.ChunkAt(x, z);
                    if (tile == null) continue;
                    Vector3 c = tile.transform.position;
                    if (Mathf.Abs(point.x - c.x) <= half && Mathf.Abs(point.z - c.z) <= half) return true;
                }
            return false;
        }
    }
}
