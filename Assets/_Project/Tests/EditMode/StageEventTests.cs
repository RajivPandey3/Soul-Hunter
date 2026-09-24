using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Data;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: StageEventTests verifies the VS stage-event schedule (default and authored)
    /// and the start positions/directions of the Swarm, Wall and ClosingRing formations.
    /// </summary>
    public class StageEventTests
    {
        private readonly List<(Vector3 position, Vector3 direction)> _formation = new List<(Vector3 position, Vector3 direction)>();
        private WaveData _wave;

        [SetUp]
        public void SetUp() => _wave = ScriptableObject.CreateInstance<WaveData>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_wave);

        [Test]
        public void DefaultSchedule_RunsEveryFormationBeforeTheBoss()
        {
            var events = _wave.GetEvents();
            Assert.That(events.Count, Is.EqualTo(5));
            Assert.That(events.Select(e => e.TimeSeconds), Is.Ordered);
            Assert.That(events.All(e => e.TimeSeconds < 1500f), "Every default event must come before the 25:00 boss.");
            Assert.That(events.Select(e => e.Type).Distinct().Count(), Is.EqualTo(3));
            Assert.That(events.All(e => e.TimeSeconds % 300f != 0f), "Events should not land on the 5-minute chest elites.");
        }

        [Test]
        public void AuthoredEvents_ReplaceTheDefaultsInTimeOrder()
        {
            _wave.Events.Add(new StageEventEntry { TimeSeconds = 90f, Type = StageEventType.Wall, Count = 4 });
            _wave.Events.Add(new StageEventEntry { TimeSeconds = 30f, Type = StageEventType.Swarm, Count = 3 });

            var events = _wave.GetEvents();
            Assert.That(events.Select(e => e.TimeSeconds), Is.EqualTo(new[] { 30f, 90f }));
        }

        [Test]
        public void Wall_IsALineAcrossTheDirection_AllFlyingTheSameWay()
        {
            var player = new Vector3(10f, 2f, -4f);
            EnemySpawner.BuildFormation(StageEventType.Wall, 5, player, Vector3.right, 20f, _formation);

            Assert.That(_formation.Count, Is.EqualTo(5));
            foreach (var (position, direction) in _formation)
            {
                Assert.That(direction, Is.EqualTo(Vector3.right));
                Assert.That(position.x, Is.EqualTo(player.x - 25f).Within(0.001f), "Wall starts beyond the spawn ring.");
                Assert.That(position.y, Is.EqualTo(player.y).Within(0.001f));
            }
            var across = _formation.Select(f => f.position.z).OrderBy(z => z).ToArray();
            Assert.That(across[1] - across[0], Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(across.Average(), Is.EqualTo(player.z).Within(0.001f), "The wall is centred on the player's line.");
        }

        [Test]
        public void Swarm_IsATightClusterFlyingAcrossThePlayer()
        {
            var player = Vector3.zero;
            EnemySpawner.BuildFormation(StageEventType.Swarm, 20, player, Vector3.forward, 20f, _formation);

            var start = player - Vector3.forward * 25f;
            Assert.That(_formation.Count, Is.EqualTo(20));
            Assert.That(_formation.All(f => f.direction == Vector3.forward));
            Assert.That(_formation.All(f => Vector3.Distance(f.position, start) <= 3.001f));
        }

        [Test]
        public void ClosingRing_SurroundsThePlayer_AndFliesInward()
        {
            var player = new Vector3(3f, 0f, 3f);
            EnemySpawner.BuildFormation(StageEventType.ClosingRing, 12, player, Vector3.right, 20f, _formation);

            Assert.That(_formation.Count, Is.EqualTo(12));
            foreach (var (position, direction) in _formation)
            {
                Assert.That(Vector3.Distance(position, player), Is.EqualTo(20f).Within(0.001f));
                Assert.That(Vector3.Dot(direction, (player - position).normalized), Is.EqualTo(1f).Within(0.001f));
            }
        }
    }
}
