using NUnit.Framework;
using SoulHunter.Gameplay.AI;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: StragglerRelocationTests verifies where far-behind enemies are moved: onto the spawn
    /// ring around the player, inside the allowed arc around the player's heading, at the player's height.
    /// </summary>
    public class StragglerRelocationTests
    {
        private static readonly Vector3 Player = new Vector3(100f, 2f, -50f);

        [Test]
        public void MiddleRolls_LandStraightAhead_AtMiddleRadius()
        {
            Vector3 point = EnemySpawner.ComputeRelocationPoint(Player, Vector3.right, 60f, 15f, 25f, 0.5f, 0.5f);

            Assert.That(point.x, Is.EqualTo(Player.x + 20f).Within(0.001f));
            Assert.That(point.z, Is.EqualTo(Player.z).Within(0.001f));
            Assert.That(point.y, Is.EqualTo(Player.y));
        }

        [TestCase(0f, 0f)]
        [TestCase(1f, 1f)]
        [TestCase(0.2f, 0.7f)]
        [TestCase(0.9f, 0.1f)]
        public void AnyRoll_StaysOnRing_AndWithinSpreadOfHeading(float angleRoll, float radiusRoll)
        {
            // Learning Comment: Enemy hamesha player ke aage wale arc (±60°) mein aur 15-25 units ki doori par aaye.
            Vector3 heading = new Vector3(0f, 0f, 1f);
            Vector3 point = EnemySpawner.ComputeRelocationPoint(Player, heading, 60f, 15f, 25f, angleRoll, radiusRoll);

            Vector3 offset = point - Player;
            offset.y = 0f;
            Assert.That(offset.magnitude, Is.InRange(15f - 0.001f, 25f + 0.001f));
            Assert.That(Vector3.Angle(heading, offset), Is.LessThanOrEqualTo(60f + 0.01f));
        }

        [Test]
        public void VerticalOrZeroHeading_StillGivesAFlatPoint()
        {
            // Learning Comment: Heading mein Y ya zero vector ho tab bhi point zameen par (player ki height) aaye.
            Vector3 fromVertical = EnemySpawner.ComputeRelocationPoint(Player, Vector3.up, 60f, 15f, 25f, 0.5f, 0f);
            Vector3 fromZero = EnemySpawner.ComputeRelocationPoint(Player, Vector3.zero, 60f, 15f, 25f, 0.5f, 0f);

            Assert.That(fromVertical.y, Is.EqualTo(Player.y));
            Assert.That(Vector3.Distance(new Vector3(fromVertical.x, Player.y, fromVertical.z), Player), Is.EqualTo(15f).Within(0.001f));
            Assert.That(Vector3.Distance(fromZero, Player), Is.EqualTo(15f).Within(0.001f));
        }
    }
}
