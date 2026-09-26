using NUnit.Framework;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Data;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>VS enemy movement skills: Homing, Floaty (wavy) and Fixed Direction.</summary>
    public class EnemyMovementTests
    {
        private const string EnemyDataFolder = "Assets/_Project/Data/Config/Enemies/";

        [Test]
        public void FloatyDirection_WeavesAroundTheHomingLine_AndAlwaysMovesForward()
        {
            Vector3 homing = Vector3.forward;
            const float weave = 0.7f, period = 2f;
            Vector3 middle = EnemyChaseState.FloatyDirection(homing, weave, period, 0f, 0f);
            Assert.That(Vector3.Angle(middle, homing), Is.LessThan(0.01f), "sin = 0: straight at the player.");

            Vector3 peak = EnemyChaseState.FloatyDirection(homing, weave, period, 0.5f, 0f); // sin = 1
            Vector3 trough = EnemyChaseState.FloatyDirection(homing, weave, period, 1.5f, 0f); // sin = -1
            Assert.That(Vector3.Angle(peak, homing), Is.EqualTo(Mathf.Atan(weave) * Mathf.Rad2Deg).Within(0.1f));
            Assert.That(Mathf.Sign(peak.x), Is.Not.EqualTo(Mathf.Sign(trough.x)), "It weaves to both sides.");

            for (float t = 0f; t < period; t += 0.05f)
            {
                Vector3 d = EnemyChaseState.FloatyDirection(homing, weave, period, t, 1.3f);
                Assert.That(d.magnitude, Is.EqualTo(1f).Within(0.001f));
                Assert.That(d.y, Is.EqualTo(0f));
                Assert.That(Vector3.Dot(d, homing), Is.GreaterThan(0.5f), "A Floaty enemy never turns away from the player.");
            }
        }

        [Test]
        public void StageEnemies_UseTheApprovedMovement()
        {
            // Owner-approved stage table (2026-09-26): Dark Forest (2) and Sea of Lost Souls (7) float; the rest home.
            foreach (var guid in AssetDatabase.FindAssets("t:EnemyData", new[] { EnemyDataFolder.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                string file = System.IO.Path.GetFileName(path);
                bool floaty = file == "L02_DarkForest_Enemy_Data.asset" || file == "L07_SeaofLostSouls_Enemy_Data.asset";
                Assert.That(data.Movement, Is.EqualTo(floaty ? EnemyMovement.Floaty : EnemyMovement.Homing), file);
            }
        }
    }
}
