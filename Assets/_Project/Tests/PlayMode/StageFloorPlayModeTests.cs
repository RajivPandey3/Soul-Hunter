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
            for (int x = 0; x < 3; x++)
                for (int z = 0; z < 3; z++)
                {
                    var chunk = map.ChunkAt(x, z);
                    Assert.That(chunk, Is.Not.Null);
                    xs.Add(Mathf.Round(chunk.transform.position.x * 100f) / 100f);
                    zs.Add(Mathf.Round(chunk.transform.position.z * 100f) / 100f);
                }
            Assert.That(map.ChunkSize, Is.EqualTo(20f), "Chunk spacing must equal the 20-unit floor tile.");
            foreach (var axis in new[] { xs.ToArray(), zs.ToArray() })
            {
                Assert.That(axis.Length, Is.EqualTo(3));
                Assert.That(axis[1] - axis[0], Is.EqualTo(20f).Within(.01f));
                Assert.That(axis[2] - axis[1], Is.EqualTo(20f).Within(.01f));
            }
        }

        private void AssertNoShowcaseGroundActive()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || !scene.name.EndsWith("_Showcase")) continue;
                var active = scene.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<Transform>())
                    .Where(t => t.gameObject.activeInHierarchy && CampaignSceneConnector.IsShowcaseGround(t.name))
                    .Select(t => t.name).ToList();
                Assert.That(active, Is.Empty, $"{scene.name} ground overlaps the stage floor. " + Errors());
            }
        }
    }
}
