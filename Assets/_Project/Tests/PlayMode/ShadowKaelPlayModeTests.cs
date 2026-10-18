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
            // Laurel is a shield. Clock Lancet slows and Pentagram erases items (owner decision 2026-09-30, tests below);
            // neither deals damage, so here they must only leave the boss and its horde alone.
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

        /// <summary>Spawns Shadow Kael holding only <paramref name="type"/>, held 3 units right of the player, harmless on contact.</summary>
        private IEnumerator KaelHolding(UpgradeType type, System.Action<ShadowKaelController, AutoAttackWeapon> ready)
        {
            yield return EnterStage10();
            var player = PlayerController.Instance;
            var weapons = WeaponManager.Instance;
            var weaponPrefab = weapons.GetWeaponObject(type);
            foreach (var existing in player.GetComponentsInChildren<AutoAttackWeapon>())
                if (!existing.gameObject.name.StartsWith(weaponPrefab.name)) existing.gameObject.SetActive(false);
            weapons.GiveWeapon(type, 1);
            var kael = SpawnShadowKael();
            foreach (var own in player.GetComponentsInChildren<AutoAttackWeapon>()) own.gameObject.SetActive(false);
            kael.GetComponent<TouchDamage>().DamageAmount = 0;
            Vector3 bossPos = player.transform.position + Vector3.right * 3f;
            bossPos.y = kael.transform.position.y;
            kael.transform.position = bossPos;
            var body = kael.GetComponent<Rigidbody>();
            if (body != null) body.position = bossPos;
            Physics.SyncTransforms();
            ready(kael, kael.GetComponentInChildren<AutoAttackWeapon>(true));
        }

        [UnityTest]
        public IEnumerator MirroredClockLancet_SlowsThePlayerToHalfSpeed_ForItsFreezeTime()
        {
            // Owner decision (2026-09-30): the player moves at half speed for the weapon's freeze time (2 s at level 1).
            ShadowKaelController kael = null;
            AutoAttackWeapon lancet = null;
            yield return KaelHolding(UpgradeType.ClockLancet, (k, w) => { kael = k; lancet = w; });
            var player = PlayerController.Instance;
            Vector3 bossPos = kael.transform.position;
            float normal = player.MoveSpeed;
            float deadline = Time.realtimeSinceStartup + 6f;
            while (!player.IsSlowed && Time.realtimeSinceStartup < deadline)
            {
                kael.transform.position = bossPos;
                yield return null;
            }
            Assert.That(player.IsSlowed, Is.True, "The mirrored beam slows the player. " + Errors());
            Assert.That(player.MoveSpeed, Is.EqualTo(normal * 0.5f).Within(0.001f), "Half speed.");
            Assert.That(kael.GetComponent<EnemyController>().IsFrozen, Is.False, "The boss is never frozen by its own lancet.");

            // With the lancet gone, the slow ends after the freeze time (2 s x Duration 1).
            lancet.gameObject.SetActive(false);
            yield return new WaitForSeconds(ClockLancetWeapon.VsFreezeDuration + 0.2f);
            Assert.That(player.IsSlowed, Is.False, "The slow ends after the freeze time.");
            Assert.That(player.MoveSpeed, Is.EqualTo(normal).Within(0.001f));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator MirroredPentagram_ErasesItemsOnScreen_ButNotStageItems_NorTheHorde()
        {
            // Owner decision (2026-09-30), using VS Pentagram's item erase (vampire.survivors.wiki/w/Pentagram): "erasing
            // every enemy, their drops, and all items on the screen, except for stage items". The boss's copy erases no enemy.
            ShadowKaelController kael = null;
            AutoAttackWeapon pentagram = null;
            yield return KaelHolding(UpgradeType.Pentagram, (k, w) => { kael = k; pentagram = w; });
            var player = PlayerController.Instance;
            Vector3 at = player.transform.position;
            // Its own timer off: the boss's Pentagram fires on its own right after spawning and would erase the items
            // below before the controlled activation (seen 2026-09-30).
            pentagram.enabled = false;
            yield return null;

            var pool = PickupPoolManager.Instance;
            for (int i = 0; i < 5; i++) pool.SpawnGem(at + new Vector3(-3f + i * 1.5f, 0f, 7f), 10); // beyond the magnet (4 units)
            var chicken = pool.SpawnChicken(at + new Vector3(-6f, 0f, -4f));
            // A stage item brought on screen keeps its mark and must stay.
            var stageItem = Object.FindFirstObjectByType<StageItemSpawner>().Placed.FirstOrDefault(p => p != null && p.activeInHierarchy && p.GetComponent<StageItemMark>() != null);
            Assert.IsNotNull(stageItem, "Stage 10 placed its stage items.");
            stageItem.transform.position = at + new Vector3(6f, 0f, -4f);
            var enemyPrefab = GetPrivateField<List<GameObject>>(_spawner, "_stageEnemyPrefabs")[0];
            var enemy = Object.Instantiate(enemyPrefab, kael.transform.position + Vector3.forward * 2f, Quaternion.identity);
            yield return null;
            Assert.That(GroundItemEraser.OnScreen(Camera.main, chicken.transform.position), Is.True, "The chicken is on screen.");
            Assert.That(GroundItemEraser.OnScreen(Camera.main, stageItem.transform.position), Is.True, "The stage item is on screen.");

            // A keep chance below zero makes the VS roll always erase (x / Luck is never below 0), so the real Attack()
            // path runs deterministically. The roll itself is checked below and in PentagramItemRollTests.
            typeof(PentagramWeapon).GetField("_chanceToKeepGems", Private).SetValue(pentagram, -1f);
            int gemsOnScreen = XPGem.ActiveGems.Count(g => g != null && g.gameObject.activeInHierarchy && GroundItemEraser.OnScreen(Camera.main, g.transform.position));
            string before = $"gems on screen {gemsOnScreen}, all gems {XPGem.ActiveGems.Count}, ticked {PickupTicker.Count}, chicken active {chicken.activeInHierarchy}, " +
                            $"chicken mark {chicken.GetComponent<StageItemMark>() != null}, camera {Camera.main.name}, luck {kael.MirroredStats.Luck}";
            typeof(AutoAttackWeapon).GetMethod("Attack", Private).Invoke(pentagram, null);

            Assert.That(((PentagramWeapon)pentagram).LastItemsErased, Is.GreaterThanOrEqualTo(6), "Five gems and a chicken were erased. Before: " + before);
            Assert.That(XPGem.ActiveGems.Count(g => g != null && GroundItemEraser.OnScreen(Camera.main, g.transform.position)), Is.EqualTo(0), "No gem is left on screen.");
            Assert.That(chicken.activeInHierarchy, Is.False, "The chicken is erased.");
            Assert.That(stageItem.activeInHierarchy, Is.True, "Stage items are never erased.");
            Assert.That(enemy.activeInHierarchy, Is.True, "The boss's Pentagram erases no enemy.");
            Assert.That(kael.gameObject.activeInHierarchy, Is.True);

            // VS keep roll: with enough Luck (x / totalLuck never above 10%), nothing is erased.
            Assert.That(GroundItemEraser.ErasesItems(0.1f, 10.1f, 1f), Is.False);
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

            health.EndRevivalInvulnerability(); // the revival's immunity (game time) would block the next hit
            health.TakeDamage(new DamagePacket(health.CurrentHealth + 100, kael.transform.position, Vector3.zero, DamageType.Magic));
            yield return WaitUntilRealtime(() => (bool)typeof(LevelProgressionManager).GetField("_bossDefeated", Private).GetValue(LevelProgressionManager.Instance),
                "the final death to defeat the Stage 10 boss");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// CMB-01 (2026-10-19). VS wiki Unholy_Vespers ("Damage 30", "Amount 4"), Heaven_Sword ("Damage 77", "Amount 1",
        /// "Cooldown 3.3 seconds") and Soul_Eater ("Damage 20", "Cooldown 1.0 second"): an evolution has no level table,
        /// "With Limit Break, [it] can be further leveled up". Built on the Bible, Cross and Garlic scripts, they used to get
        /// the base weapon's whole level 2-8 table on evolving (Vespers 50 damage and 7 books, Heaven Sword 107 and 3 swords,
        /// Soul Eater 30 and 0.7 s), and Shadow Kael copied it.
        /// </summary>
        [UnityTest]
        public IEnumerator BaseScriptEvolutions_KeepTheirOwnNumbers_OnThePlayerAndOnShadowKael()
        {
            yield return EnterStage10();
            var player = PlayerController.Instance;
            var weapons = WeaponManager.Instance;
            var known = GetPrivateField<List<WeaponEvolutionData>>(weapons, "_availableEvolutions");
            foreach (var asset in new[] { "Evo_UnholyVespers", "Evo_HeavenSword", "Evo_SoulEater" })
            {
                var evolution = known.FirstOrDefault(e => e != null && e.name == asset);
                Assert.IsNotNull(evolution, $"{asset} is one of WeaponManager's evolutions.");
                weapons.GiveWeapon(evolution.BaseWeapon, evolution.BaseWeaponMaxLevel);
                weapons.GiveWeapon(evolution.RequiredPassive, Mathf.Max(1, evolution.RequiredPassiveMaxLevel));
                yield return null;
                Assert.That(weapons.TryEvolveWeapon(out _), Is.True, $"{asset} evolves.");
                yield return null;
            }

            void Check(IEnumerable<AutoAttackWeapon> held, string who)
            {
                var vespers = held.OfType<SoulHunter.Gameplay.Weapons.BibleWeapon>().Single(w => w.gameObject.name.StartsWith("UnholyVespers"));
                Assert.That(vespers.DamageAmount, Is.EqualTo(30f), $"{who}: Unholy Vespers 30 damage.");
                Assert.That(vespers.LevelBookCount, Is.EqualTo(0), $"{who}: Unholy Vespers gets no level books (4 in all).");
                Assert.That(vespers.LevelAreaMultiplier, Is.EqualTo(1f));
                var sword = held.OfType<SoulHunter.Gameplay.Weapons.CrossWeapon>().Single(w => w.gameObject.name.StartsWith("HeavenSword"));
                Assert.That(sword.DamageAmount, Is.EqualTo(77f), $"{who}: Heaven Sword 77 damage.");
                Assert.That(sword.LevelCrossCount, Is.EqualTo(1), $"{who}: one sword.");
                Assert.That(sword.AttackCooldown, Is.EqualTo(3.3f).Within(0.001f));
                var eater = held.OfType<SoulHunter.Gameplay.Weapons.GarlicWeapon>().Single(w => w.gameObject.name.StartsWith("SoulEater"));
                Assert.That(eater.DamageAmount, Is.EqualTo(20f), $"{who}: Soul Eater 20 damage.");
                Assert.That(eater.AttackCooldown, Is.EqualTo(1f).Within(0.001f), $"{who}: Soul Eater 1.0 s.");
                Assert.That(eater.LevelAreaMultiplier, Is.EqualTo(1f), $"{who}: Soul Eater keeps its own radius.");
                Assert.That(vespers.CurrentLevel, Is.EqualTo(8), $"{who}: the level is still recorded.");
            }

            Check(weapons.GetEquippedWeapons().Select(p => p.Value).ToList(), "player");
            var kael = SpawnShadowKael();
            Check(kael.GetComponentsInChildren<AutoAttackWeapon>(true).ToList(), "Shadow Kael");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
