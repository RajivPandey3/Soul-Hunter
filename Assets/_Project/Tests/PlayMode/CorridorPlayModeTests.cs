using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Environment;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// The Ruined Castle as a walled corridor in the real game (VS's Inlaid Library): walls on both sides keep the
    /// player to left and right and follow it along, every enemy enters inside the corridor, and the stage's items,
    /// plates and lanterns lie inside it.
    /// </summary>
    public class CorridorPlayModeTests : GameplayPlayModeTestBase
    {
        private IEnumerator EnterCastle()
        {
            yield return BootIntoGameplay();
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            LevelProgressionManager.Instance.StartStage(4);
            for (int i = 0; i < 4; i++) yield return null;
            // A jump leaves Stage 1's opening horde where it stood; a real stage clear wipes the arena first.
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
            Assert.That(StageCorridor.Active, Is.True, "The Ruined Castle is a corridor. " + Errors());
        }

        private static void Walk(Vector3 worldDirection)
        {
            var player = PlayerController.Instance;
            var right = player.GetCameraRelativeMovement(Vector2.right);
            var forward = player.GetCameraRelativeMovement(Vector2.up);
            var input = new Vector2(Vector3.Dot(worldDirection, right), Vector3.Dot(worldDirection, forward)).normalized;
            typeof(PlayerController).GetField("_eventBusInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(player, worldDirection == Vector3.zero ? Vector2.zero : input);
        }

        [UnityTest]
        public IEnumerator Walls_KeepThePlayerToLeftAndRight_AndFollowIt()
        {
            yield return EnterCastle();
            var walls = Object.FindFirstObjectByType<CorridorWalls>();
            Assert.That(walls.NorthWalls.Count, Is.EqualTo(7));
            Assert.That(walls.SouthWalls.Count, Is.EqualTo(7));
            var player = PlayerController.Instance;

            // Walk north along the floor tile's open north-south path (clear of the castle pillars) for 4 s: 5 units/s
            // would carry the player 20 units, the wall stops it.
            Vector3 onPath = new Vector3(Mathf.Round(player.transform.position.x / 20f) * 20f, player.transform.position.y, StageCorridor.CentreZ);
            player.Rigidbody.position = onPath;
            player.transform.position = onPath;
            yield return new WaitForFixedUpdate();
            Walk(Vector3.forward);
            float until = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < until) yield return new WaitForFixedUpdate();
            Assert.That(player.transform.position.z, Is.LessThanOrEqualTo(StageCorridor.CentreZ + StageCorridor.HalfWidth + 0.1f),
                "The north wall stopped the player.");
            Assert.That(player.transform.position.z, Is.GreaterThan(StageCorridor.CentreZ + StageCorridor.HalfWidth - 1.5f), "It walked up to it.");

            // Along the corridor the walls come too (walk the open path down its middle, clear of the castle pillars).
            Walk(Vector3.zero);
            Vector3 middle = new Vector3(player.transform.position.x, player.transform.position.y, StageCorridor.CentreZ);
            player.Rigidbody.position = middle;
            player.transform.position = middle;
            yield return new WaitForFixedUpdate();
            Walk(Vector3.right);
            until = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < until) yield return new WaitForFixedUpdate();
            Walk(Vector3.zero);
            float x = player.transform.position.x;
            Assert.That(x, Is.GreaterThan(20f), "The player walked along the corridor.");
            Assert.That(walls.NorthWalls.Min(w => w.position.x) < x - 40f && walls.NorthWalls.Max(w => w.position.x) > x + 40f, Is.True,
                "Wall modules cover well past both ends of the screen.");
            Walk(Vector3.back);
            until = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < until) yield return new WaitForFixedUpdate();
            Walk(Vector3.zero);
            Assert.That(player.transform.position.z, Is.GreaterThanOrEqualTo(StageCorridor.CentreZ - StageCorridor.HalfWidth - 0.1f),
                "The south wall stops it too, further along.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator EveryEnemy_ItemAndPlate_IsInsideTheCorridor()
        {
            yield return EnterCastle();
            var seen = new HashSet<EnemyController>();
            float until = Time.realtimeSinceStartup + 8f;
            int checkedEnemies = 0;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
                foreach (var enemy in EnemyController.ActiveEnemies.ToList())
                {
                    if (enemy == null || !seen.Add(enemy)) continue;
                    checkedEnemies++;
                    Assert.That(StageCorridor.Contains(enemy.transform.position), Is.True, $"An enemy entered outside the corridor at {enemy.transform.position}.");
                }
            }
            Assert.That(checkedEnemies, Is.GreaterThan(20), "The castle horde spawned.");

            var items = Object.FindFirstObjectByType<StageItemSpawner>();
            yield return WaitUntilRealtime(() => items.Placed.Count == 6 && items.Plates.Count == 4, "the castle's items and plates");
            foreach (var item in items.Placed) Assert.That(StageCorridor.Contains(item.transform.position, 0.5f), Is.True, $"{item.name} is in the corridor.");
            foreach (var plate in items.Plates) Assert.That(StageCorridor.Contains(plate.transform.position, 0.5f), Is.True, "Plates are in the corridor.");

            var lanterns = Object.FindFirstObjectByType<LightSourceSpawner>();
            for (int i = 0; i < 20; i++)
            {
                Assert.That(LightSourceSpawner.TryFindSpot(PlayerController.Instance.transform.position, out var spot), Is.True);
                Assert.That(StageCorridor.Contains(spot, 1f), Is.True, "Lanterns appear inside the corridor.");
            }
            Assert.That(lanterns, Is.Not.Null);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator LeavingTheCastle_OpensTheGroundAgain()
        {
            yield return EnterCastle();
            LevelProgressionManager.Instance.StartStage(5);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.That(StageCorridor.Active, Is.False, "The swamp is open ground.");
            var walls = Object.FindFirstObjectByType<CorridorWalls>();
            Assert.That(walls.NorthWalls.Count + walls.SouthWalls.Count, Is.EqualTo(0), "The castle walls are gone.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
