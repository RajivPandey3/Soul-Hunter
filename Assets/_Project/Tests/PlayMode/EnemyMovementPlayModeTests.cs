using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS movement skills in the real game: stage enemies spawned by EnemySpawner either home straight in, weave in
    /// (Floaty: Dark Forest and Sea of Lost Souls, per the owner-approved stage table), or keep one line across the
    /// screen whatever the player does (Fixed Direction).
    /// </summary>
    public class EnemyMovementPlayModeTests : GameplayPlayModeTestBase
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private EnemySpawner _spawner;

        private IEnumerator Arena(int stage)
        {
            _spawner = Object.FindFirstObjectByType<EnemySpawner>();
            _spawner.enabled = false;
            if (LevelProgressionManager.Instance.CurrentStage != stage) LevelProgressionManager.Instance.StartStage(stage);
            yield return null;
            yield return null;
            _spawner.enabled = false;
            // Deactivate (back to the spawner's pools), not Destroy: SpawnEnemy reuses pooled objects.
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            yield return null;
        }

        private EnemyController SpawnStageEnemy()
        {
            var prefab = GetPrivateField<GameObject>(_spawner, "_stageEnemyPrefab");
            var enemy = (GameObject)typeof(EnemySpawner).GetMethod("SpawnEnemy", Private).Invoke(_spawner, new object[] { prefab });
            Assert.IsNotNull(enemy, "The spawner spawns the stage enemy. " + Errors());
            var touch = enemy.GetComponent<TouchDamage>();
            if (touch != null) touch.DamageAmount = 0;
            return enemy.GetComponent<EnemyController>();
        }

        /// <summary>Largest sideways distance from the straight line start -> target, and how much closer it got.</summary>
        private static IEnumerator Track(EnemyController enemy, Vector3 target, float seconds, float[] result)
        {
            Vector3 start = enemy.transform.position;
            Vector3 line = target - start; line.y = 0f;
            Vector3 along = line.normalized;
            float maxSide = 0f;
            float until = Time.time + seconds;
            while (Time.time < until && enemy.isActiveAndEnabled)
            {
                yield return new WaitForFixedUpdate();
                Vector3 offset = enemy.transform.position - start; offset.y = 0f;
                maxSide = Mathf.Max(maxSide, (offset - along * Vector3.Dot(offset, along)).magnitude);
            }
            Vector3 end = enemy.transform.position - target; end.y = 0f;
            result[0] = maxSide;
            result[1] = line.magnitude - end.magnitude;
        }

        [UnityTest]
        public IEnumerator FloatyStageEnemy_WeavesIn_WhileAHomingOneComesStraight()
        {
            yield return BootIntoGameplay();
            var player = PlayerController.Instance;

            yield return Arena(1);
            var homing = SpawnStageEnemy();
            Assert.That(homing.Movement, Is.EqualTo(EnemyMovement.Homing), "Stage 1 enemies home.");
            var straight = new float[2];
            yield return Track(homing, player.transform.position, 4f, straight);
            homing.gameObject.SetActive(false);

            yield return Arena(2);
            var floaty = SpawnStageEnemy();
            Assert.That(floaty.Movement, Is.EqualTo(EnemyMovement.Floaty), "Stage 2 (Dark Forest) enemies float.");
            var weave = new float[2];
            yield return Track(floaty, player.transform.position, 4f, weave);

            string report = $"Homing: side {straight[0]:0.00}, closer by {straight[1]:0.0}. Floaty: side {weave[0]:0.00}, closer by {weave[1]:0.0}. ";
            Debug.Log("[EnemyMovementPlayModeTests] " + report);
            Assert.That(straight[0], Is.LessThan(0.2f), "A homing enemy comes straight in. " + report);
            Assert.That(weave[0], Is.GreaterThan(0.5f), "A Floaty enemy weaves side to side. " + report);
            Assert.That(weave[1], Is.GreaterThan(straight[1] * 0.6f), "It still closes in on the player. " + report);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator FixedDirectionEnemy_KeepsItsLine_WhenThePlayerMoves_ThenLeaves()
        {
            yield return BootIntoGameplay();
            var player = PlayerController.Instance;
            yield return Arena(1);
            var enemy = SpawnStageEnemy();

            // A runtime copy of the stage data set to Fixed Direction (the asset itself is left alone).
            var data = Object.Instantiate(enemy.Data);
            data.Movement = EnemyMovement.FixedDirection;
            typeof(EnemyController).GetField("_enemyData", Private).SetValue(enemy, data);
            Vector3 start = player.transform.position + new Vector3(0f, 0f, 8f);
            start.y = enemy.transform.position.y;
            enemy.transform.position = start;
            enemy.Rigidbody.position = start;
            Physics.SyncTransforms();
            enemy.BeginMovement();
            Assert.That(enemy.IsFlying, Is.True, "A Fixed Direction enemy is on its course.");

            // The player steps 5 units aside: a homing enemy would turn; this one must not.
            Vector3 target = player.transform.position;
            Vector3 aside = player.Rigidbody.position + Vector3.right * 5f;
            player.Rigidbody.position = aside;
            player.transform.position = aside;
            var course = new float[2];
            yield return Track(enemy, target, 2.5f, course);
            Assert.That(course[0], Is.LessThan(0.1f), $"It keeps its line (side {course[0]:0.00}).");
            Assert.That(course[1], Is.GreaterThan(enemy.MoveSpeed * 2.5f * 0.7f), "It moves along it at its own speed.");

            // It passes the player's old spot and leaves on its own (no relocation, back to the pool).
            float leaveBy = Time.time + (8f * 2f + 6f) / enemy.MoveSpeed + 2f;
            while (enemy.isActiveAndEnabled && Time.time < leaveBy) yield return null;
            Assert.That(enemy.isActiveAndEnabled, Is.False, "It leaves once it has crossed the screen.");
            Object.Destroy(data);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
