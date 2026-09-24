using NUnit.Framework;
using SoulHunter.Gameplay.Data;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: EnemyMinimumTests verifies the VS enemy minimum data: the automatic
    /// 5-minute phases raise the minimum each phase, and hand-authored phases keep their own value.
    /// </summary>
    public class EnemyMinimumTests
    {
        private WaveData _wave;

        [SetUp]
        public void SetUp() => _wave = ScriptableObject.CreateInstance<WaveData>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_wave);

        [Test]
        public void AutomaticPhases_RaiseTheMinimumEveryFiveMinutes()
        {
            _wave.MinimumEnemies = 30;
            _wave.MinimumEnemiesPerPhase = 20;

            Assert.That(_wave.GetPhase(0f).MinimumEnemies, Is.EqualTo(30));
            Assert.That(_wave.GetPhase(300f).MinimumEnemies, Is.EqualTo(50));
            Assert.That(_wave.GetPhase(1799f).MinimumEnemies, Is.EqualTo(110));
        }

        [Test]
        public void AuthoredPhase_KeepsItsOwnMinimum()
        {
            _wave.Phases.Add(new WavePhase { StartTimeInSeconds = 0f, MinimumEnemies = 5 });

            Assert.That(_wave.GetPhase(10f).MinimumEnemies, Is.EqualTo(5));
        }
    }
}
