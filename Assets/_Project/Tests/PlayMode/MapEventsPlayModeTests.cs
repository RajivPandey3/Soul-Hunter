using System.Collections;
using System.Collections.Generic;
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
    /// VS map events in the real game (Level 1, Mad Forest; raw pages 2026-10-27). Bat Swarm: "summons 50 Pipeestrellos
    /// that move in a straight line across the map"; Flower Wall: "a ring of durable Flower Wall units around the player,
    /// which slowly creeps inward".
    /// </summary>
    public class MapEventsPlayModeTests : GameplayPlayModeTestBase
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        private static void DisablePlayerWeapons()
        {
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
        }

        private List<EnemyController> Run(EnemySpawner spawner, StageEventEntry entry)
        {
            var level = LevelProgressionManager.Instance.CurrentLevel;
            var phase = GetPrivateField<WaveData>(spawner, "_currentWave").GetPhase(0f);
            return (List<EnemyController>)typeof(EnemySpawner).GetMethod("RunStageEvent", Private).Invoke(spawner, new object[] { entry, level, phase });
        }

        [UnityTest]
        public IEnumerator ABatSwarm_SendsFiftySwarmBats_StraightAtTheirOwnSpeed()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            DisablePlayerWeapons();
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
            var bats = MapEvents.For(1).First(e => e.EnemyTypeId == "SwarmBat");

            var spawned = Run(spawner, bats);
            Assert.That(spawned, Has.Count.EqualTo(50), Errors());
            Assert.That(spawned.All(e => e.name.StartsWith(MadForestEnemies.PrefabName("SwarmBat"))), Is.True, "Every member is a Swarm Bat.");
            Assert.That(spawned.All(e => e.IsFlying), Is.True, "They keep a straight course.");
            float expected = 700f * EnemyAbilityDefaults.VsSpeedToUnits;
            var starts = spawned.Select(e => e.transform.position).ToArray();
            float t0 = Time.time;
            for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
            float elapsed = Time.time - t0;
            float moved = Vector3.Distance(spawned[0].transform.position, starts[0]);
            Assert.That(moved / elapsed, Is.EqualTo(expected * spawned[0].CampaignSpeedMultiplier).Within(expected * 0.25f), "VS speed 700.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator AFlowerWall_RingsTheHero_WithThornWalls_ForItsDuration()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            DisablePlayerWeapons();
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
            var wall = MapEvents.For(1).First(e => e.Type == StageEventType.TrapRing);

            var spawned = Run(spawner, wall);
            Assert.That(spawned.Count, Is.GreaterThan(0), Errors());
            Assert.That(spawned.All(e => e.name.StartsWith(MadForestEnemies.PrefabName("ThornWall"))), Is.True, "Flower Walls are Thorn Walls.");
            var circle = Object.FindObjectsByType<SoulHunter.Gameplay.Hazards.ThornCircle>(FindObjectsSortMode.None).Single();
            Assert.That(circle.RemainingSeconds, Is.EqualTo(30f).Within(0.5f), "5:00 Flower Wall: duration 30.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
