using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Each VS stage's own trap events in the real game (raw stage pages, "Traps", 2026-10-28): stepping on a trap
    /// "triggers an unavoidable map event, chosen at random between" the stage's options.
    /// </summary>
    public class StageTrapsPlayModeTests : GameplayPlayModeTestBase
    {
        private IEnumerator EnterLevel(int level, System.Action<EnemySpawner> found)
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            player.GetComponent<HealthController>().IsInvincible = true;
            LevelProgressionManager.Instance.StartStage(level);
            yield return null;
            yield return null;
            spawner.enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList()) if (stray != null) stray.gameObject.SetActive(false);
            found(spawner);
        }

        [UnityTest]
        public IEnumerator LevelThree_APlate_FiresOneOfDairyPlantsTrapEvents_WithItsOwnUnit()
        {
            EnemySpawner spawner = null;
            yield return EnterLevel(3, s => spawner = s);
            var items = Object.FindFirstObjectByType<StageItemSpawner>();
            yield return WaitUntilRealtime(() => items != null && items.Plates.Count == 4, "the stage's Rune Plates");
            PressurePlate.ResetGlobalCooldown();
            var plate = items.Plates[0];
            Assert.That(plate.Step(), Is.True, Errors());
            var trap = plate.LastTrap;
            Assert.That(trap, Is.Not.Null);
            Assert.That(StageTraps.For(3).Any(r => r.Type == trap.Type && r.EnemyTypeId == trap.EnemyTypeId && r.Count == trap.Count),
                Is.True, "One of Dairy Plant's 9 options.");
            string prefab = ChestlessWaveBosses.PrefabName(3, trap.EnemyTypeId);
            yield return null;
            var units = EnemyController.ActiveEnemies.Where(e => e != null && e.isActiveAndEnabled && e.GetComponent<SoulHunter.Gameplay.Hazards.DeathsShadow>() == null).ToArray();
            Assert.That(units.Length, Is.GreaterThan(0));
            Assert.That(units.All(e => e.name.StartsWith(prefab)), Is.True, $"Every unit is the row's own ({prefab}).");
            Assert.That(plate.Step(), Is.False, "Then it cools down for 20 s.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator LevelSeven_ACircle_RingsTheHero_MantichanasHome_AndPotsStand()
        {
            EnemySpawner spawner = null;
            yield return EnterLevel(7, s => spawner = s);
            Vector3 hero = PlayerController.Instance.transform.position;
            var mantis = spawner.TriggerMapEvent(StageTraps.For(7).First(r => r.EnemyTypeId == "TideMantis"));
            Assert.That(mantis, Has.Count.EqualTo(8), Errors());
            Assert.That(mantis.All(m => m.GetComponent<SoulHunter.Gameplay.Hazards.EventLifetime>() != null), Is.True, "They go at the event's 60 s.");
            Assert.That(mantis.All(m => m.GetComponent<SoulHunter.Gameplay.Hazards.StillEnemy>() == null), Is.True, "Foscari Mantichanas move (speed 80).");
            float before = mantis.Average(m => Flat(m.transform.position - hero));
            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            Assert.That(mantis.Average(m => Flat(m.transform.position - hero)), Is.LessThan(before), "They come at the hero.");

            var pots = spawner.TriggerMapEvent(StageTraps.For(7).First(r => r.EnemyTypeId == "DrownedUrn"));
            Assert.That(pots, Has.Count.EqualTo(50));
            Assert.That(pots.All(p => p.GetComponent<SoulHunter.Gameplay.Hazards.StillEnemy>() != null), Is.True, "Foscari Pots cannot move.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator LevelFour_APoltergeistRoulette_StandsRoundTheHero_AndSelfDestructs()
        {
            EnemySpawner spawner = null;
            yield return EnterLevel(4, s => spawner = s);
            var ring = spawner.TriggerMapEvent(StageTraps.For(4).First(r => r.EnemyTypeId == "StillCandle" && r.Count == 50));
            Assert.That(ring, Has.Count.EqualTo(50), Errors());
            Assert.That(ring.All(p => p.GetComponent<SoulHunter.Gameplay.Hazards.StillEnemy>() != null), Is.True);
            var health = ring[0].GetComponent<HealthController>();
            int level = PlayerController.Instance.GetComponent<PlayerExperience>().CurrentLevel;
            Assert.That(health.MaxHealth, Is.EqualTo(10 * level), "Poltergeist (dest): 10 HP x Level.");
            Assert.That(ring[0].GetComponent<EnemyAbilities>(), Is.Not.Null, "It carries its self-destruct.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator LevelFive_ATrapSwarm_SweepsStraight_EvenWithAHomingUnit()
        {
            EnemySpawner spawner = null;
            yield return EnterLevel(5, s => spawner = s);
            var moss = spawner.TriggerMapEvent(StageTraps.For(5).First(r => r.EnemyTypeId == "CrimsonSpore" && r.Count == 50));
            Assert.That(moss, Has.Count.EqualTo(50), Errors());
            Assert.That(moss.All(m => m.IsFlying), Is.True, "Blood Moss on a fixed course: 'sweep in a straight line'.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }
    }
}
