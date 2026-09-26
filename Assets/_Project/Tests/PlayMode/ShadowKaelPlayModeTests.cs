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
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Level 10 in the real game (AGENTS.md: Shadow Kael mirrors the player's combat loadout at encounter start).
    /// The boss copies the player's stats and the weapons the player is using, at their levels; every mirrored weapon
    /// attacks the player and never the boss or its horde; a mirrored revival is not the boss kill.
    /// </summary>
    public class ShadowKaelPlayModeTests : GameplayPlayModeTestBase
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        private static IEnumerable<UpgradeType> Weapons() => new[]
        {
            UpgradeType.MagicWand, UpgradeType.Whip, UpgradeType.Garlic, UpgradeType.Axe, UpgradeType.Bible, UpgradeType.Cross,
            UpgradeType.LightningRing, UpgradeType.FireWand, UpgradeType.SantaWater, UpgradeType.Knife, UpgradeType.Runetracer,
            UpgradeType.Pentagram, UpgradeType.ClockLancet, UpgradeType.Laurel, UpgradeType.Bone, UpgradeType.Peachone,
            UpgradeType.SongOfMana, UpgradeType.CherryBomb, UpgradeType.Gun,
        };

        private EnemySpawner _spawner;

        /// <summary>Boots, jumps to Stage 10 with the spawner off and no horde, and makes the player hard to kill.</summary>
        private IEnumerator EnterStage10()
        {
            yield return BootIntoGameplay();
            _spawner = Object.FindFirstObjectByType<EnemySpawner>();
            _spawner.enabled = false;
            LevelProgressionManager.Instance.StartStage(10);
            yield return null;
            yield return null;
            _spawner.enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) Object.Destroy(stray.gameObject);
            yield return null;
            PlayerController.Instance.GetComponent<HealthController>().Initialize(1000000);
        }

        private ShadowKaelController SpawnShadowKael()
        {
            var bossPrefab = GetPrivateField<GameObject>(_spawner, "_stageBossPrefab");
            Assert.IsNotNull(bossPrefab, "Stage 10 has a boss prefab.");
            Assert.IsTrue((bool)typeof(EnemySpawner).GetMethod("SpawnBoss", Private).Invoke(_spawner, new object[] { bossPrefab }),
                "The Stage 10 boss spawns. " + Errors());
            var kael = Object.FindObjectsByType<ShadowKaelController>(FindObjectsSortMode.None).FirstOrDefault(k => k.isActiveAndEnabled);
            Assert.IsNotNull(kael, "The Stage 10 boss is Shadow Kael. " + Errors());
            return kael;
        }

        private static string BaseName(GameObject go) => go.name.Replace("(Clone)", "").Trim();

        private static IEnumerable<Transform> LiveWeaponObjects() =>
            Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.gameObject.activeInHierarchy && (t.name.StartsWith("SH10_Proj") || t.name.StartsWith("SH10_Fx") || t.name.StartsWith("SH10_Bible(")));

        /// <summary>One live weapon object and how it targets, for a failure message.</summary>
        private static string Describe(Transform t, PlayerController player)
        {
            var p = t.GetComponent<Projectile>();
            var d = t.GetComponent<ProjectileDamage>();
            var touch = t.GetComponent<TouchDamage>();
            var c = t.GetComponent<Collider>();
            return $"{t.name}@{(t.position - player.transform.position).magnitude:0.0} y{t.position.y:0.0} proj={(p != null ? (p.enabled ? "on" : "off") + "/tp=" + p.TargetsPlayer : "-")} " +
                   $"pd={(d != null ? d.TargetTag + "/" + d.enabled + "/" + d.DamageAmount : "-")} td={(touch != null ? touch.TargetTag : "-")} " +
                   $"col={(c != null ? (c.isTrigger ? "trigger" : "solid") + "/" + c.enabled : "-")} rb={(t.GetComponent<Rigidbody>() != null)} layer={t.gameObject.layer}";
        }

        [UnityTest]
        public IEnumerator ShadowKael_CopiesTheLoadout_AtEncounterStart()
        {
            yield return EnterStage10();
            var player = PlayerController.Instance;
            var weapons = WeaponManager.Instance;
            player.Stats.AddMight(0.5f);
            player.Stats.AddAmount(1);
            player.Stats.AddArmor(2);
            weapons.GiveWeapon(UpgradeType.MagicWand, 3);
            weapons.GiveWeapon(UpgradeType.Garlic, 2);
            weapons.GiveWeapon(UpgradeType.SantaWater, 1);
            yield return null;
            var equipped = weapons.GetEquippedWeapons();
            Assert.That(equipped.Count, Is.GreaterThanOrEqualTo(4), "Kael's Whip plus three weapons.");

            var kael = SpawnShadowKael();
            var expected = equipped.Select(p => p.Key.name + ":" + p.Value.CurrentLevel).OrderBy(s => s).ToList();
            var mirrored = kael.GetComponentsInChildren<AutoAttackWeapon>(true).Select(w => BaseName(w.gameObject) + ":" + w.CurrentLevel).OrderBy(s => s).ToList();
            Assert.That(mirrored, Is.EqualTo(expected), "The boss holds exactly the player's weapons at their levels.");
            Assert.That(kael.MirroredStats.Might, Is.EqualTo(player.Stats.Might).Within(0.001f));
            Assert.That(kael.MirroredStats.Amount, Is.EqualTo(player.Stats.Amount));
            Assert.That(kael.GetComponent<HealthController>().Armor, Is.EqualTo(player.Stats.Armor), "Mirrored Armor.");

            // Fixed at encounter start: a later player upgrade does not reach the boss.
            weapons.GiveWeapon(UpgradeType.Knife, 1);
            yield return null;
            Assert.That(kael.GetComponentsInChildren<AutoAttackWeapon>(true).Count(), Is.EqualTo(expected.Count));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator MirroredWeapon_AttacksThePlayer_NeverTheBossOrItsHorde([ValueSource(nameof(Weapons))] UpgradeType type)
        {
            yield return EnterStage10();
            var player = PlayerController.Instance;
            var weapons = WeaponManager.Instance;
            var weaponPrefab = weapons.GetWeaponObject(type);
            Assert.IsNotNull(weaponPrefab, $"{type} has no weapon prefab.");
            // Only the weapon under test is mirrored: the character's starting weapon is switched off first.
            foreach (var existing in player.GetComponentsInChildren<AutoAttackWeapon>())
                if (!existing.gameObject.name.StartsWith(weaponPrefab.name)) existing.gameObject.SetActive(false);
            // Same frame: the player's copy never gets to fire, so everything that hits below is the boss's.
            weapons.GiveWeapon(type, 1);
            var kael = SpawnShadowKael();
            var mirrored = kael.GetComponentsInChildren<AutoAttackWeapon>(true);
            Assert.That(mirrored.Select(w => BaseName(w.gameObject)), Is.EqualTo(new[] { weaponPrefab.name }), $"The boss mirrors {type}.");
            foreach (var own in player.GetComponentsInChildren<AutoAttackWeapon>()) own.gameObject.SetActive(false);

            // Contact damage off: only the mirrored weapon can hurt the player.
            var boss = kael.gameObject;
            boss.GetComponent<TouchDamage>().DamageAmount = 0;
            // Held at a fixed distance: the Bible and Peachone orbit at radius 3, so a boss standing on the player never
            // reaches it; a level-1 Garlic aura is smaller than that.
            Vector3 bossPos = player.transform.position + Vector3.right * (type == UpgradeType.Garlic ? 1.5f : 3f);
            bossPos.y = boss.transform.position.y;
            boss.transform.position = bossPos;
            var bossBody = boss.GetComponent<Rigidbody>();
            if (bossBody != null) bossBody.position = bossPos;
            Physics.SyncTransforms();
            var bossHealth = boss.GetComponent<HealthController>();
            int bossStart = bossHealth.CurrentHealth;

            // Harmless horde beside the boss, away from the player.
            var enemyPrefab = GetPrivateField<List<GameObject>>(_spawner, "_stageEnemyPrefabs")[0];
            var horde = new List<HealthController>();
            for (int i = 0; i < 4; i++)
            {
                var offset = Quaternion.Euler(0f, i * 90f, 0f) * Vector3.forward * 2.5f;
                var enemy = Object.Instantiate(enemyPrefab, bossPos + offset, Quaternion.identity);
                var touch = enemy.GetComponent<TouchDamage>();
                if (touch != null) touch.DamageAmount = 0;
                horde.Add(enemy.GetComponent<HealthController>());
            }
            var hordeStart = horde.ToDictionary(h => h, h => h.CurrentHealth);

            var playerHealth = player.GetComponent<HealthController>();
            int playerStart = playerHealth.CurrentHealth;
            // Laurel is a shield. Clock Lancet (freeze) and Pentagram (screen wipe) have no player version yet
            // (owner decision pending), so held by the boss they must only leave the boss and its horde alone.
            bool attacks = type != UpgradeType.Laurel && type != UpgradeType.ClockLancet && type != UpgradeType.Pentagram;
            float deadline = Time.realtimeSinceStartup + (attacks ? 10f : 4f);
            var closest = new Dictionary<string, string>();
            var closestDistance = new Dictionary<string, float>();
            while (Time.realtimeSinceStartup < deadline && !(attacks && playerHealth.CurrentHealth < playerStart))
            {
                boss.transform.position = bossPos;
                if (bossBody != null) bossBody.position = bossPos;
                if (type == UpgradeType.Peachone)
                {
                    // Peachone's blasts land under the circling bird, rarely on a player who stands still: step onto one.
                    var blast = LiveWeaponObjects().FirstOrDefault(t => t.name.StartsWith("SH10_Fx_PeachoneBlast"));
                    if (blast != null)
                    {
                        Vector3 onBlast = new Vector3(blast.position.x, player.transform.position.y, blast.position.z);
                        player.Rigidbody.position = onBlast;
                        player.transform.position = onBlast;
                    }
                }
                yield return null;
                foreach (var t in LiveWeaponObjects())
                {
                    float d = (t.position - player.transform.position).magnitude;
                    if (closestDistance.TryGetValue(t.name, out float best) && best <= d) continue;
                    closestDistance[t.name] = d;
                    closest[t.name] = Describe(t, player);
                }
            }
            string seen = "Closest approach: " + (closest.Count == 0 ? "(nothing spawned)" : string.Join("; ", closest.Values)) + ". ";

            if (attacks) Assert.That(playerHealth.CurrentHealth, Is.LessThan(playerStart), $"The mirrored {type} never hurt the player within 10 s. " + seen + Errors());
            Assert.That(boss.activeInHierarchy && bossHealth.CurrentHealth == bossStart, Is.True,
                $"The mirrored {type} hurt Shadow Kael himself ({bossHealth.CurrentHealth}/{bossStart}, active {boss.activeInHierarchy}). " + Errors());
            Assert.That(boss.GetComponent<EnemyController>().IsFrozen, Is.False, $"The mirrored {type} froze Shadow Kael.");
            var hurt = horde.Where(h => h == null || !h.gameObject.activeInHierarchy || h.CurrentHealth < hordeStart[h]).ToList();
            Assert.That(hurt, Is.Empty, $"The mirrored {type} hurt {hurt.Count} of the boss's own horde.");
            Assert.That(horde.Any(h => h != null && h.GetComponent<EnemyController>().IsFrozen), Is.False, $"The mirrored {type} froze the horde.");
            foreach (var h in horde) if (h != null) Object.Destroy(h.gameObject);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator MirroredRevival_IsNotTheBossKill_TheLastDeathWinsTheRun()
        {
            yield return EnterStage10();
            var player = PlayerController.Instance;
            player.Stats.AddRevival(1);
            foreach (var own in player.GetComponentsInChildren<AutoAttackWeapon>()) own.gameObject.SetActive(false);
            var kael = SpawnShadowKael();
            Assert.That(kael.MirroredRevivals, Is.EqualTo(player.Stats.Revivals));
            var health = kael.GetComponent<HealthController>();
            int chestsBefore = Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None).Count(c => c.gameObject.activeInHierarchy);
            bool won = false;
            LevelProgressionManager.Instance.OnGameWon += () => won = true;

            health.TakeDamage(new DamagePacket(health.CurrentHealth + 100, kael.transform.position, Vector3.zero, DamageType.Magic));
            for (int i = 0; i < 5; i++) yield return null;
            Assert.That(kael.gameObject.activeInHierarchy && health.CurrentHealth > 0, Is.True, "Shadow Kael revives.");
            Assert.That(kael.MirroredRevivals, Is.EqualTo(player.Stats.Revivals - 1));
            Assert.That((bool)typeof(LevelProgressionManager).GetField("_bossDefeated", Private).GetValue(LevelProgressionManager.Instance), Is.False,
                "A revival must not clear Stage 10.");
            Assert.That(won, Is.False, "A revival must not win the run.");
            Assert.That(Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None).Count(c => c.gameObject.activeInHierarchy), Is.EqualTo(chestsBefore),
                "A revival drops no chest.");

            health.IsInvincible = false;
            health.TakeDamage(new DamagePacket(health.CurrentHealth + 100, kael.transform.position, Vector3.zero, DamageType.Magic));
            yield return WaitUntilRealtime(() => (bool)typeof(LevelProgressionManager).GetField("_bossDefeated", Private).GetValue(LevelProgressionManager.Instance),
                "the final death to defeat the Stage 10 boss");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
