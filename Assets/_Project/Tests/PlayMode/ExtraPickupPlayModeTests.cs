using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// The four extra light-source pickups in the real game (VS Rosary, Nduja Fritta Tanto, Little Clover and Gilded
    /// Clover rules, Soul Hunter names): Soul Toll wipes the screen but spares the stage boss, Hellfire Breath burns
    /// what is in front for 10 s and then stops, Luck Charm adds 10% Luck, and Gilded Charm gathers the coins on the
    /// ground and starts a Gold Fever in which kills drop coins.
    /// </summary>
    public class ExtraPickupPlayModeTests : GameplayPlayModeTestBase
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private EnemySpawner _spawner;

        private IEnumerator QuietArena()
        {
            yield return BootIntoGameplay();
            _spawner = Object.FindFirstObjectByType<EnemySpawner>();
            _spawner.enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            yield return null;
        }

        private GameObject SpawnEnemy(Vector3 offset)
        {
            var prefab = GetPrivateField<GameObject>(_spawner, "_stageEnemyPrefab");
            var enemy = (GameObject)typeof(EnemySpawner).GetMethod("SpawnEnemy", Private).Invoke(_spawner, new object[] { prefab });
            enemy.transform.position = PlayerController.Instance.transform.position + offset;
            var drop = enemy.GetComponent<EnemyDrop>();
            if (drop != null)
            {
                drop.ChickenDropChance = 0f; // a gem every time
                drop.GoldDropChance = 0f;    // only fever coins
            }
            Physics.SyncTransforms();
            return enemy;
        }

        /// <summary>Drops the pickup on the player and waits for it to be collected.</summary>
        private IEnumerator Collect(ExtraPickupPool.Kind kind)
        {
            var item = ExtraPickupPool.Spawn(kind, PlayerController.Instance.transform.position);
            Assert.IsNotNull(item, $"{kind} spawns. " + Errors());
            yield return WaitUntilRealtime(() => !item.activeSelf, $"the {kind} pickup to be collected");
        }

        private static bool Alive(GameObject enemy) => enemy.activeInHierarchy && enemy.GetComponent<HealthController>().CurrentHealth > 0;

        [UnityTest]
        public IEnumerator SoulToll_KillsEnemiesOnScreen_ButNotTheStageBoss_AndTheyDrop()
        {
            yield return QuietArena();
            var bossPrefab = GetPrivateField<GameObject>(_spawner, "_stageBossPrefab");
            Assert.IsTrue((bool)typeof(EnemySpawner).GetMethod("SpawnBoss", Private).Invoke(_spawner, new object[] { bossPrefab }));
            var boss = EnemyController.ActiveEnemies.Last(e => e != null && e.IsStageBoss);
            boss.transform.position = PlayerController.Instance.transform.position + Vector3.forward * 5f;
            var a = SpawnEnemy(Vector3.right * 6f);
            var b = SpawnEnemy(Vector3.left * 6f);
            Assert.That(ScreenWipePickup.IsOnScreen(Camera.main, a.transform.position), Is.True, "The test enemies stand on screen.");
            int kills = RunStatsTracker.Instance.TotalKills;
            int gems = XPGem.ActiveGems.Count(g => g != null && g.isActiveAndEnabled);

            yield return Collect(ExtraPickupPool.Kind.ScreenWipe);

            Assert.That(Alive(a) || Alive(b), Is.False, "Every ordinary enemy on screen dies. " + Errors());
            Assert.That(boss.isActiveAndEnabled && boss.GetComponent<HealthController>().CurrentHealth > 0, Is.True, "The stage boss is immune.");
            Assert.That(RunStatsTracker.Instance.TotalKills, Is.EqualTo(kills + 2), "Both count as kills (EnemyDrop ran).");
            Assert.That(XPGem.ActiveGems.Count(g => g != null && g.isActiveAndEnabled), Is.GreaterThanOrEqualTo(gems + 2), "They drop their gems normally.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator HellfireBreath_BurnsAnEnemyInFront_ThenStops()
        {
            yield return QuietArena();
            var player = PlayerController.Instance;
            var target = SpawnEnemy(Vector3.forward * 4f);
            // No gem: collecting it could level the hero up, and the level-up screen pauses the breath's game-time timer.
            target.GetComponent<EnemyDrop>().EraseDropsThisLife();
            bool hit = false;
            target.GetComponent<HealthController>().OnDamaged += () => hit = true;
            float started = Time.time;

            yield return Collect(ExtraPickupPool.Kind.FireBreath);
            var breath = player.GetComponentInChildren<FireBreath>();
            Assert.IsNotNull(breath, "The hero breathes fire. " + Errors());
            float length = breath.RemainingSeconds;
            Assert.That(length, Is.EqualTo(FireBreath.BaseDurationSeconds * player.Stats.Duration).Within(0.1f));

            float deadline = Time.realtimeSinceStartup + 10f;
            while (!hit && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(hit, Is.True, "A flame hit the enemy in front within 10 s. " + Errors());

            yield return WaitUntilRealtime(() => player.GetComponentInChildren<FireBreath>() == null, "the breath to end");
            Assert.That(Time.time - started, Is.GreaterThanOrEqualTo(length - 0.2f), "It lasts its full length.");

            // Flames live 1 s x Duration; after that nothing burns a new enemy.
            yield return new WaitForSeconds(FireBreath.FlameLifetime * player.Stats.Duration + 0.2f);
            var later = SpawnEnemy(Vector3.forward * 3f);
            later.GetComponent<EnemyDrop>().EraseDropsThisLife();
            bool hitLater = false;
            later.GetComponent<HealthController>().OnDamaged += () => hitLater = true;
            yield return new WaitForSeconds(1.5f);
            Assert.That(hitLater, Is.False, "No more fire once the breath has ended.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator LuckCharm_RaisesLuckByTenPercent()
        {
            yield return QuietArena();
            var stats = PlayerController.Instance.Stats;
            float before = stats.Luck;
            yield return Collect(ExtraPickupPool.Kind.LuckCharm);
            Assert.That(stats.Luck, Is.EqualTo(before + 0.1f).Within(1e-4), Errors());
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator GildedCharm_GathersTheCoins_AndKillsDropCoinsDuringTheFever()
        {
            yield return QuietArena();
            var player = PlayerController.Instance;
            var pool = PickupPoolManager.Instance;
            var coins = new[]
            {
                pool.SpawnGold(player.transform.position + new Vector3(14f, 0f, 0f), 1),
                pool.SpawnGold(player.transform.position + new Vector3(-12f, 0f, 6f), 10),
                pool.SpawnGold(player.transform.position + new Vector3(0f, 0f, -13f), 1),
            };
            Assert.That(coins.All(c => c != null && c.isActiveAndEnabled), Is.True, "Coins lie on the ground. " + Errors());
            yield return null;
            Assert.That(coins.All(c => c.isActiveAndEnabled), Is.True, "Far coins are not picked up on their own.");
            int goldBefore = RunStatsTracker.Instance.TotalGoldCollected;

            yield return Collect(ExtraPickupPool.Kind.GildedCharm);
            Assert.That(GoldFever.Running, Is.True, "A Gold Fever starts. " + Errors());
            yield return WaitUntilRealtime(() => coins.All(c => !c.isActiveAndEnabled), "the gathered coins to reach the hero");
            Assert.That(RunStatsTracker.Instance.TotalGoldCollected, Is.GreaterThan(goldBefore), "The gathered coins pay out.");
            Assert.That(GoldFever.Running, Is.True, "Still in the fever.");

            // 75% per kill: ten kills leave a fever coin with overwhelming odds (1 - 0.25^10).
            bool dropped = false;
            for (int i = 0; i < 10 && !dropped; i++)
            {
                var enemy = SpawnEnemy(new Vector3(8f, 0f, i - 5f));
                enemy.GetComponent<HealthController>().TakeDamage(new DamagePacket(1000000, enemy.transform.position, Vector3.zero, DamageType.Normal));
                dropped = GoldPickup.ActiveCoins.Any(c => c != null && c.isActiveAndEnabled && c.IsFeverCoin);
                yield return null;
            }
            Assert.That(dropped, Is.True, "A kill during the fever drops a coin.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
