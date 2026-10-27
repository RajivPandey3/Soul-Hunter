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
    
        // ---------------------------------------------------------------- Level 2, Inlaid Library

        private IEnumerator EnterLevelTwo(System.Action<EnemySpawner> found)
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            DisablePlayerWeapons();
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
            LevelProgressionManager.Instance.StartStage(2);
            yield return null;
            yield return null;
            spawner.enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList()) if (stray != null) stray.gameObject.SetActive(false);
            found(spawner);
        }

        [UnityTest]
        public IEnumerator LevelTwo_AMedusaWall_ComesFromTheLeftOrRight_AndWeavesAcross()
        {
            EnemySpawner spawner = null;
            yield return EnterLevelTwo(s => spawner = s);
            var wall = MapEvents.For(2).First(e => e.Type == StageEventType.Wall && e.EnemyTypeId == "GloomMote");
            var table = InlaidLibraryEnemies.Table;

            var spawned = Run(spawner, wall);
            Assert.That(spawned, Has.Count.EqualTo(6), Errors());
            Assert.That(spawned.All(e => e.name.StartsWith(table.PrefabName("GloomMote"))), Is.True, "Medusa Wall units are Gloom Motes.");
            Assert.That(spawned.All(e => e.IsFlying), Is.True, "Fixed Direction.");
            var player = PlayerController.Instance.transform.position;
            float side = Mathf.Sign(spawned[0].transform.position.x - player.x);
            Assert.That(spawned.All(e => Mathf.Sign(e.transform.position.x - player.x) == side), Is.True, "One side, left or right.");
            var starts = spawned.Select(e => e.transform.position).ToArray();
            float minZ = float.MaxValue, maxZ = float.MinValue;
            for (int i = 0; i < 60; i++)
            {
                yield return new WaitForFixedUpdate();
                float dz = spawned[0].transform.position.z - starts[0].z;
                minZ = Mathf.Min(minZ, dz); maxZ = Mathf.Max(maxZ, dz);
            }
            float dx = spawned[0].transform.position.x - starts[0].x;
            Assert.That(-side * dx, Is.GreaterThan(1f), "It travels horizontally, toward the other side.");
            Assert.That(maxZ - minZ, Is.GreaterThan(0.1f), "A wavy pattern (Floaty).");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator LevelTwo_AShadeBomb_ChasesTheHero_AndCarriesItsSelfDestruct()
        {
            EnemySpawner spawner = null;
            yield return EnterLevelTwo(s => spawner = s);
            var bomb = MapEvents.For(2).First(e => e.Type == StageEventType.Chasers && e.Count == 2);

            var spawned = Run(spawner, bomb);
            Assert.That(spawned, Has.Count.EqualTo(2), Errors());
            var shade = spawned[0];
            Assert.That(shade.IsFlying, Is.False, "A Shade Bomb homes; it keeps no fixed course.");
            Assert.That(shade.Data.Movement, Is.EqualTo(EnemyMovement.Homing), "No Floaty skill on Sig.ra Rossi.");
            Assert.That(shade.Data.Abilities.Any(a => a.Kind == EnemyAbilityKind.SelfDestruct), Is.True);
            float before = Vector3.Distance(shade.transform.position, PlayerController.Instance.transform.position);
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            Assert.That(Vector3.Distance(shade.transform.position, PlayerController.Instance.transform.position), Is.LessThan(before - 1f), "It closes in.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator LevelTwo_ASkullSwarm_BeelinesAtItsOwnSpeed()
        {
            EnemySpawner spawner = null;
            yield return EnterLevelTwo(s => spawner = s);
            var skulls = MapEvents.For(2).First(e => e.EnemyTypeId == "SwarmSkull" && e.Count == 32);

            var spawned = Run(spawner, skulls);
            Assert.That(spawned, Has.Count.EqualTo(32), Errors());
            Assert.That(spawned.All(e => e.IsFlying), Is.True);
            float expected = 700f * EnemyAbilityDefaults.VsSpeedToUnits * spawned[0].CampaignSpeedMultiplier;
            var start = spawned[0].transform.position;
            float t0 = Time.time;
            for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
            float speed = Vector3.Distance(spawned[0].transform.position, start) / (Time.time - t0);
            Assert.That(speed, Is.EqualTo(expected).Within(expected * 0.25f), "Skullone (swarm) speed 700.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
