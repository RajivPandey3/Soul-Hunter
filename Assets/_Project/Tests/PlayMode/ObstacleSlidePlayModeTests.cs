using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner (2026-10-12: "player agar kisi obstacle se takra ke ekdam se atak ja raha hai tum ise slippery banao"). The
    /// real hero walks, in the real game, at boxes placed on the Obstacle layer far from the stage's own obstacles:
    /// straight at a small one (it steps round and goes on), at a slant into a long wall (it slides along it and keeps
    /// most of its speed), and straight at a long wall (it stops in front, as the Castle corridor needs). It never goes
    /// through any of them. Before, each of these stopped the hero dead.
    /// </summary>
    public class ObstacleSlidePlayModeTests : GameplayPlayModeTestBase
    {
        private readonly List<GameObject> _made = new List<GameObject>();

        private GameObject Box(Vector3 centre, Vector3 size)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "TestObstacle";
            box.layer = LayerMask.NameToLayer("Obstacle");
            box.transform.position = centre;
            box.transform.localScale = size;
            _made.Add(box);
            return box;
        }

        [TearDown]
        public void RemoveBoxes()
        {
            foreach (var box in _made) if (box != null) Object.Destroy(box);
            _made.Clear();
        }

        private IEnumerator Walk(PlayerController player, Vector3 from, Vector3 direction, float seconds, System.Action<Vector3> eachStep)
        {
            player.Rigidbody.position = from;
            player.transform.position = from;
            player.Rigidbody.linearVelocity = Vector3.zero;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            var right = player.GetCameraRelativeMovement(Vector2.right);
            var forward = player.GetCameraRelativeMovement(Vector2.up);
            var input = new Vector2(Vector3.Dot(direction, right), Vector3.Dot(direction, forward)).normalized;
            var field = typeof(PlayerController).GetField("_eventBusInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(player, input);
            for (float t = 0f; t < seconds; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                eachStep(player.Rigidbody.position);
            }
            field.SetValue(player, Vector2.zero);
            yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator TheHero_SlidesAlongObstacles_AndNeverGoesThroughThem()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner != null) spawner.enabled = false;
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            yield return CloseOpenLevelUps();
            float speed = player.MoveSpeed;
            float y = player.Rigidbody.position.y;
            Assert.That(speed, Is.GreaterThan(1f), "The hero has a walking speed.");
            // Only the test's own boxes block: the floor tiles follow the hero with their obstacles, so those are switched off.
            foreach (var blocker in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (blocker.gameObject.layer == LayerMask.NameToLayer("Obstacle")) blocker.enabled = false;
            Vector3 field = new Vector3(4000f, y, 4000f);

            // 1) Straight at a small obstacle (a gravestone): the hero steps round it and goes on.
            Vector3 stoneAt = field + new Vector3(0f, 0f, 3f);
            var stone = Box(stoneAt, new Vector3(1f, 4f, 1f));
            var stoneBounds = new Bounds(stoneAt, new Vector3(1f, 4f, 1f));
            bool inside = false;
            Vector3 last = field;
            yield return Walk(player, field, Vector3.forward, 2f, p => { inside |= stoneBounds.Contains(p); last = p; });
            Assert.That(inside, Is.False, "The hero never went through the small obstacle. " + Errors());
            Assert.That(last.z, Is.GreaterThan(stoneAt.z + 1f), $"Walking straight at a small obstacle, the hero stepped round it and went on (ended at {last}, obstacle at {stoneAt}).");
            Object.Destroy(stone);
            yield return new WaitForFixedUpdate();

            // 2) At a slant into a long wall: the hero slides along it and keeps most of its speed.
            Vector3 wallField = field + new Vector3(200f, 0f, 0f);
            Vector3 wallAt = wallField + new Vector3(0f, 0f, 2f);
            var wall = Box(wallAt, new Vector3(60f, 4f, 1f));
            var wallBounds = new Bounds(wallAt, new Vector3(60f, 4f, 1f));
            inside = false;
            var track = new List<Vector3>();
            yield return Walk(player, wallField, new Vector3(1f, 0f, 1f).normalized, 2f, p => { inside |= wallBounds.Contains(p); track.Add(p); });
            Assert.That(inside, Is.False, "The hero never went through the long wall. " + Errors());
            // Its last second is all along the wall.
            int half = track.Count / 2;
            float alongSpeed = (track[track.Count - 1].x - track[half].x) / ((track.Count - 1 - half) * Time.fixedDeltaTime);
            Assert.That(alongSpeed, Is.GreaterThan(speed * (ObstacleSlide.SlideSpeedShare - 0.08f)),
                $"Sliding along the wall the hero kept at least {ObstacleSlide.SlideSpeedShare:P0} of its speed {speed:0.##} (it moved at {alongSpeed:0.##}).");
            Assert.That(track[track.Count - 1].z, Is.LessThan(wallAt.z - 0.5f + 0.01f), "The hero stayed in front of the wall.");

            // 3) Straight at the long wall (the Castle corridor): no way round, so the hero stops in front of it.
            inside = false;
            last = wallField;
            yield return Walk(player, wallField, Vector3.forward, 1.5f, p => { inside |= wallBounds.Contains(p); last = p; });
            Assert.That(inside, Is.False, "The hero never went through the wall it walked straight at. " + Errors());
            Assert.That(Mathf.Abs(last.x - wallField.x), Is.LessThan(0.5f), $"Walking straight at a long wall, the hero did not drift along it (ended at {last}).");
            Assert.That(last.z, Is.LessThan(wallAt.z - 0.5f + 0.01f).And.GreaterThan(wallField.z + 0.5f), "The hero walked up to the wall and stopped in front of it.");
            Object.Destroy(wall);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
