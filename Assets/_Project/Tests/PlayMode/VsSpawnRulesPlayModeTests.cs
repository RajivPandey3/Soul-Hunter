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
    /// VS wiki (Enemies, Spawning, raw 2026-10-27): "Upon entering a stage, an initial amount of enemies depending on the
    /// stage modifiers spawns" (each stage infobox's starting-spawns), and "Enemies ... can despawn if the player moves far
    /// enough from them" while "Bosses do not despawn ... but get teleported back to the screen".
    /// </summary>
    public class VsSpawnRulesPlayModeTests : GameplayPlayModeTestBase
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        [UnityTest]
        public IEnumerator AStage_StartsWithItsInfoboxsStartingSpawns()
        {
            yield return BootIntoGameplay();
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            LevelProgressionManager.Instance.StartStage(2);
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            Assert.That(spawner.StartingSpawnsLeft, Is.EqualTo(50), "Inlaid Library: starting-spawns = 50.");
            yield return WaitUntilRealtime(() => spawner.StartingSpawnsLeft == 0, "the starting enemies to be sent");
            Assert.That(EnemyController.ActiveEnemies.Count(e => e != null && e.isActiveAndEnabled), Is.GreaterThanOrEqualTo(50));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator AnOrdinaryEnemyFarBehind_Despawns_AChestCarrierIsBroughtBack()
        {
            yield return BootIntoGameplay();
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            yield return WaitUntilRealtime(() => EnemyController.ActiveEnemies.Count(e => e != null && !e.IsFlying && !spawner.IsBoss(e)) >= 1,
                "an ordinary enemy");
            var ordinary = EnemyController.ActiveEnemies.First(e => e != null && !e.IsFlying && !spawner.IsBoss(e));
            var carrier = (GameObject)typeof(EnemySpawner).GetMethod("SpawnChestEliteAt", Private).Invoke(spawner, new object[] { 1f, false });
            Assert.IsNotNull(carrier);
            int kills = RunStatsTracker.Instance.TotalKills;
            int gems = SoulHunter.Gameplay.Pickups.XPGem.ActiveGems.Count(g => g != null && g.isActiveAndEnabled);

            Vector3 player = PlayerController.Instance.transform.position;
            ordinary.transform.position = player + new Vector3(-100f, 0f, 0f);
            carrier.transform.position = player + new Vector3(100f, 0f, 0f);

            yield return WaitUntilRealtime(() => !ordinary.isActiveAndEnabled, "the ordinary straggler to despawn");
            yield return WaitUntilRealtime(() => Vector3.Distance(carrier.transform.position, PlayerController.Instance.transform.position) < 40f,
                "the chest carrier to be brought back");
            Assert.That(carrier.activeInHierarchy, Is.True, "The chest carrier stays alive.");
            Assert.That(RunStatsTracker.Instance.TotalKills, Is.EqualTo(kills), "A despawn is not a kill.");
            Assert.That(SoulHunter.Gameplay.Pickups.XPGem.ActiveGems.Count(g => g != null && g.isActiveAndEnabled), Is.LessThanOrEqualTo(gems),
                "A despawn drops no gem.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
