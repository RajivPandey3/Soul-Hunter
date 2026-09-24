using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// End-to-end time freeze flow: the pickup pool uses the authored TimeFreeze_Pickup
    /// prefab (not the runtime placeholder), and collecting it freezes active enemies.
    /// </summary>
    public class TimeFreezeFlowPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator TimeFreezePickup_UsesAuthoredPrefab_AndFreezesEnemies()
        {
            yield return BootIntoGameplay();

            var pool = PickupPoolManager.Instance;
            Assert.IsNotNull(pool, "No PickupPoolManager in Main_Gameplay.");
            var prefab = GetPrivateField<GameObject>(pool, "_timeFreezePrefab");
            Assert.IsNotNull(prefab, "PickupPoolManager has no time freeze prefab.");
            Assert.AreEqual("TimeFreeze_Pickup", prefab.name,
                "Time freeze pool is using a runtime placeholder instead of the authored prefab.");

            // Wait for the first wave so there is something to freeze.
            yield return WaitUntilRealtime(() => EnemyController.ActiveEnemies.Any(e => e != null && e.isActiveAndEnabled),
                "enemies to spawn");

            pool.SpawnTimeFreeze(PlayerController.Instance.transform.position);
            var pickup = Object.FindObjectsByType<TimeFreezePickup>(FindObjectsSortMode.None)
                .FirstOrDefault(p => p.gameObject.activeInHierarchy);
            Assert.IsNotNull(pickup, "SpawnTimeFreeze did not produce an active pickup.");
            StringAssert.StartsWith("TimeFreeze_Pickup", pickup.gameObject.name, "Spawned pickup is not the authored prefab.");

            yield return WaitUntilRealtime(() => !pickup.gameObject.activeInHierarchy, "the player to collect the time freeze pickup");

            var enemies = EnemyController.ActiveEnemies.Where(e => e != null && e.isActiveAndEnabled).ToArray();
            Assert.IsNotEmpty(enemies, "No active enemies to check after collecting the pickup.");
            foreach (var enemy in enemies)
            {
                var state = GetPrivateField<IEnemyState>(enemy, "_currentState");
                Assert.IsInstanceOf<EnemyFrozenState>(state, $"Enemy '{enemy.name}' is not frozen after collecting time freeze. " + Errors());
            }
        }
    }
}
