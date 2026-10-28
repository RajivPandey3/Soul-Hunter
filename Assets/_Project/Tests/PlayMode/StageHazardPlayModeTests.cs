using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Hazards;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS stage hazards in the real game (2026-09-27): Death's Shadow (the VS Stalker XL since 2026-10-09: a 30% roll at
    /// 8:00 and 12:00 on Stage 5, a trail-and-dash chase, only instant kills defeat it, each with its VS chest), a Soul Meteor
    /// (Shooting Star) striking after 2 s for 30, a Pile Assault bullet hurting the player, and a Stage 3 rail cart that
    /// launches when hit and kills an enemy on its rail.
    /// </summary>
    public class StageHazardPlayModeTests : GameplayPlayModeTestBase
    {
        private StageHazards _hazards;
        private EnemySpawner _spawner;

        private IEnumerator Boot(int stage, bool invinciblePlayer)
        {
            yield return BootIntoGameplay();
            _spawner = Object.FindFirstObjectByType<EnemySpawner>();
            _spawner.enabled = false; // only the hazards bring enemies
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = invinciblePlayer;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            // The 0:00 Death's Shadow roll (1%, VS Dairy Plant) must not bring a shadow into these tests.
            if (StageHazards.Instance != null) StageHazards.Instance.ShadowRoll = () => 0.999f;
            LevelProgressionManager.Instance.StartStage(stage);
            yield return null;
            yield return null;
            yield return null;
            ClearStrays();
            _hazards = StageHazards.Instance;
            _hazards.ShadowRoll = null;
            Assert.That(_hazards, Is.Not.Null, "GameSessionManager adds StageHazards.");
            Assert.That(_hazards.Stage, Is.EqualTo(stage));
        }

        private static void ClearStrays()
        {
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null && stray.GetComponent<DeathsShadow>() == null && stray.GetComponent<PileShooter>() == null)
                    stray.gameObject.SetActive(false);
        }

        private static IEnumerator WaitGameSeconds(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until) yield return null;
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private static int Kills() => RunStatsTracker.Instance != null ? RunStatsTracker.Instance.TotalKills : 0;

        private const System.Reflection.BindingFlags Private = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        private const System.Reflection.BindingFlags PrivateStatic = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;

        private static ChestPickup[] ActiveChests() =>
            Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None).Where(c => c != null && c.gameObject.activeInHierarchy).ToArray();

        /// <summary>A Death's Shadow held still (Orologion-style freeze, which goes through freeze resistance) at an offset.</summary>
        private DeathsShadow HeldShadow(Vector3 offset)
        {
            var shadow = _hazards.SpawnDeathsShadow(60f);
            Assert.That(shadow, Is.Not.Null, "Death's Shadow spawned. " + Errors());
            shadow.transform.position = PlayerController.Instance.transform.position + offset;
            shadow.GetComponent<EnemyController>().Freeze(60f);
            UnityEngine.Physics.SyncTransforms();
            return shadow;
        }

        [UnityTest]
        public IEnumerator DeathsShadow_ComesOnStage5At8_IsTheStalkerXL_TrailsThenDashes_AndLeaves()
        {
            yield return Boot(9, invinciblePlayer: true); // Stage 9: the Stalker XL alone (Level 5 also has the Blinder since 2026-10-28)
            Assert.That(_hazards.Shadows, Is.Empty, "Nothing before 8:00.");
            _hazards.ShadowRoll = () => 0f; // the 30% roll succeeds
            LevelProgressionManager.Instance.SkipTo(8f * 60f);
            yield return WaitUntilRealtime(() => _hazards.Shadows.Count == 1, "Death's Shadow at 8:00");

            var shadow = _hazards.Shadows[0];
            var enemy = shadow.GetComponent<EnemyController>();
            var health = shadow.GetComponent<HealthController>();
            var touch = shadow.GetComponent<TouchDamage>();
            var player = PlayerController.Instance;
            int level = player.GetComponent<PlayerExperience>().CurrentLevel;
            float factor = _spawner.CurseForSpeedAndSpawns * SoulHunter.Gameplay.Data.RunModes.EnemySpeedMultiplier(LevelProgressionManager.Instance.StageElapsedTime / 60f);
            Assert.That(enemy.IsStageBoss, Is.False, "Not the stage boss: the boss logic is untouched.");
            Assert.That(shadow.transform.localScale.x, Is.EqualTo(_spawner.StageBossPrefab.transform.localScale.x * 1.5f).Within(0.01f), "The stage boss model, 1.5x.");
            Assert.That(shadow.RemainingSeconds, Is.InRange(115f, 120f), "The 8:00 event lasts 2 minutes.");
            Assert.That(enemy.MoveSpeed, Is.EqualTo(StageHazardTable.ShadowMoveSpeed * factor).Within(0.05f), "VS speed 300 = 9 units/s (x Curse and modes).");
            Assert.That(health.MaxHealth, Is.EqualTo(StageHazardTable.ShadowHealth(level)), "655,350 x player level.");
            Assert.That(touch.DamageAmount, Is.EqualTo(Mathf.RoundToInt(50 * SoulHunter.Gameplay.Data.RunModes.EnemyDamageMultiplier)), "VS damage 50.");
            Assert.That(health.KnockbackResistance, Is.EqualTo(1f), "VS knockback 0.");
            Assert.That(enemy.FreezeResistance, Is.EqualTo(2f), "VS res-freeze 2.");
            Assert.That(enemy.ResistsInstantKill, Is.False, "VS res-kill 0.");
            Assert.That(enemy.ResistsDebuffs, Is.True, "VS res-debuff 1.");

            // It closes in, then trails at its gap without touching.
            float before = Flat(shadow.transform.position, player.transform.position);
            yield return WaitGameSeconds(1.5f);
            float after = Flat(shadow.transform.position, player.transform.position);
            Assert.That(after, Is.LessThan(before - 2f), $"It follows the player ({before:0.0} -> {after:0.0}). " + Errors());
            yield return WaitGameSeconds(2f);
            float trailing = Flat(shadow.transform.position, player.transform.position);
            Assert.That(shadow.IsDashing, Is.False);
            Assert.That(trailing, Is.GreaterThan(shadow.TrailGap - 0.75f), $"While trailing it keeps its gap ({trailing:0.00}, gap {shadow.TrailGap:0.00}).");

            // Then it dashes in.
            shadow.PhaseRemainingSeconds = 0.01f; // test hook: the trail lasts 8 s
            yield return WaitGameSeconds(1f);
            float dashing = Flat(shadow.transform.position, player.transform.position);
            Assert.That(shadow.IsDashing, Is.True, "The 2 s dash.");
            Assert.That(dashing, Is.LessThan(shadow.TrailGap - 1f), $"It closes in on a hero who stands still ({dashing:0.00}).");

            int kills = Kills();
            int chests = ActiveChests().Length;
            shadow.RemainingSeconds = 0.3f; // test hook
            yield return WaitGameSeconds(0.6f);
            Assert.That(enemy.gameObject.activeSelf, Is.False, "It leaves when its time is up.");
            Assert.That(Kills(), Is.EqualTo(kills), "Leaving is not a kill.");
            Assert.That(ActiveChests().Length, Is.EqualTo(chests), "Leaving drops nothing (the boss model would drop a chest).");
            yield return null;
            Assert.That(enemy.GetComponent<DeathsShadow>(), Is.Null, "The pooled enemy is ordinary again.");
            Assert.That(health.IsInvincible, Is.False);
            Assert.That(health.TryPreventDeath, Is.Null);
            Assert.That(health.DamageInterceptor, Is.Null);
            Assert.That(enemy.AttackDistance, Is.EqualTo(_spawner.StageBossPrefab.GetComponent<EnemyController>().AttackDistance));
            Assert.That(enemy.ResistsDebuffs, Is.EqualTo(enemy.Data != null && enemy.Data.DebuffResistance), "The data's resistances again.");
            Assert.That(_hazards.Shadows, Is.Empty);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator DeathsShadow_ComesOnlyWhenIts30PercentRollSucceeds_AndTheNoonOneLastsAMinute()
        {
            yield return Boot(9, invinciblePlayer: true); // Stage 9: the Stalker XL alone (Level 5 also has the Blinder since 2026-10-28)
            int atStart = _hazards.ShadowEventsSkipped; // the 0:00 roll (1%) already failed in Boot
            Assert.That(atStart, Is.EqualTo(1), "Stage 5 rolls for Death's Shadow at 0:00 too.");
            _hazards.ShadowRoll = () => 0.99f; // fails 30% / Luck
            LevelProgressionManager.Instance.SkipTo(8f * 60f);
            yield return WaitUntilRealtime(() => _hazards.ShadowEventsSkipped == atStart + 1, "the 8:00 roll");
            yield return null;
            Assert.That(_hazards.Shadows, Is.Empty, "A failed roll sends nothing.");

            _hazards.ShadowRoll = () => 0.29f; // under 30% (Kael's Luck is 1)
            LevelProgressionManager.Instance.SkipTo(12f * 60f);
            yield return WaitUntilRealtime(() => _hazards.Shadows.Count == 1, "Death's Shadow at 12:00");
            Assert.That(_hazards.Shadows[0].RemainingSeconds, Is.InRange(55f, 60f), "The 12:00 event lasts 1 minute.");
            Assert.That(_hazards.ShadowEventsSkipped, Is.EqualTo(atStart + 1));
            _hazards.Shadows[0].Leave();
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator DeathsShadow_OrdinaryWeaponsWipesAndDebuffs_DoNothing()
        {
            yield return Boot(9, invinciblePlayer: true); // Stage 9: the Stalker XL alone (Level 5 also has the Blinder since 2026-10-28)
            var shadow = HeldShadow(new Vector3(6f, 0f, 0f));
            var enemy = shadow.GetComponent<EnemyController>();
            var health = shadow.GetComponent<HealthController>();
            var whip = PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>(true).First();
            Assert.That(StageHazardTable.KillerOf(whip, null), Is.EqualTo(ShadowKiller.None), $"{whip.GetType().Name} is an ordinary weapon.");
            int kills = Kills();
            int chests = ActiveChests().Length;

            using (DamageSource.From(whip)) health.TakeDamage(new DamagePacket(999999, shadow.transform.position, Vector3.forward));
            health.TakeDamage(new DamagePacket(999999, shadow.transform.position, Vector3.forward));
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth), "\"Otherwise intangible\": no damage at all.");
            Assert.That(enemy.IsKnockedBack, Is.False, "No knockback.");
            health.KillSilently(); // the stage-clear wipe: no source
            yield return null;
            Assert.That(shadow != null && shadow.gameObject.activeInHierarchy, Is.True, "A wipe does not remove it.");
            Assert.That(health.CurrentHealth, Is.GreaterThan(0));
            Assert.That(enemy.TryWeaponFreeze(1f, 2f), Is.False, "Clock Lancet (freeze chance 1) cannot freeze it (resistance 2).");
            Assert.That(enemy.ApplyGarlicDebuff(), Is.False, "Debuff resistance: no Garlic debuff.");
            Assert.That(enemy.ApplyLingeringSlow(0.05f, 0.2f), Is.False, "Debuff resistance: no Requiem slow.");
            Assert.That(Kills(), Is.EqualTo(kills), "Never a kill.");
            Assert.That(ActiveChests().Length, Is.EqualTo(chests));
            Assert.That(XPGem.ActiveGems.Count(g => g != null && g.isActiveAndEnabled && Flat(g.transform.position, shadow.transform.position) < 3f), Is.EqualTo(0), "XP 0.");
            shadow.Leave();
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator DeathsShadow_InstantKills_DefeatIt_WithTheirVsChests()
        {
            yield return Boot(9, invinciblePlayer: true); // Stage 9: the Stalker XL alone (Level 5 also has the Blinder since 2026-10-28)
            var player = PlayerController.Instance;
            int kills = Kills();

            // Pentagram: defeated, no chest ("regardless of it being a good or bad proc").
            WeaponManager.Instance.GiveWeapon(UpgradeType.Pentagram, 1);
            yield return null;
            var pentagram = player.GetComponentsInChildren<PentagramWeapon>(true).First();
            pentagram.enabled = false; // fired by the test
            yield return null;
            var shadow = HeldShadow(new Vector3(6f, 0f, 0f));
            int chests = ActiveChests().Length;
            typeof(PentagramWeapon).GetMethod("Attack", Private).Invoke(pentagram, null);
            yield return null;
            Assert.That(shadow == null || !shadow.gameObject.activeInHierarchy, Is.True, "Pentagram defeats it. " + Errors());
            Assert.That(DeathsShadow.LastDefeatedBy, Is.EqualTo(ShadowKiller.Pentagram));
            Assert.That(DeathsShadow.LastChest.HasValue, Is.False);
            Assert.That(ActiveChests().Length, Is.EqualTo(chests), "Pentagram: no chest.");
            pentagram.gameObject.SetActive(false);

            // Eclipse Sigil (VS Gorgeous Moon): a high-quality chest.
            var evolution = SoulHunter.Gameplay.Data.GameContentCatalog.Load().Evolutions.First(e => e.EvolvedName == "Evo_EclipseSigil");
            var sigil = Object.Instantiate(evolution.EvolvedWeaponPrefab, player.transform).GetComponent<EclipseSigilWeapon>();
            sigil.enabled = false;
            shadow = HeldShadow(new Vector3(-6f, 0f, 0f));
            Vector3 at = shadow.transform.position;
            typeof(EclipseSigilWeapon).GetMethod("EraseOne", PrivateStatic).Invoke(null, new object[] { shadow.GetComponent<EnemyController>(), sigil });
            yield return null;
            AssertDefeatedWithChest(shadow, at, ShadowKiller.EclipseSigil, ChestTiers.StalkerHighQuality);
            Object.Destroy(sigil.gameObject);

            // Crimson Shroud: a high-quality chest. Its pulse is placed by the shadow, away from the hero.
            var shroudData = Resources.Load<WeaponEvolutionData>("Metaglio/CrimsonShroudEvolution");
            Assert.That(shroudData != null && shroudData.EvolvedWeaponPrefab != null, Is.True, "The Crimson Shroud prefab.");
            shadow = HeldShadow(new Vector3(0f, 0f, 7f));
            at = shadow.transform.position;
            var shroud = Object.Instantiate(shroudData.EvolvedWeaponPrefab, at + Vector3.right, Quaternion.identity).GetComponent<CrimsonShroudWeapon>();
            shroud.enabled = false;
            typeof(CrimsonShroudWeapon).GetMethod("Pulse", Private).Invoke(shroud, new object[] { 10 });
            yield return null;
            AssertDefeatedWithChest(shadow, at, ShadowKiller.CrimsonShroud, ChestTiers.StalkerHighQuality);
            Object.Destroy(shroud.gameObject);

            // Soul Toll (VS Rosary): a high-quality chest.
            shadow = HeldShadow(new Vector3(0f, 0f, -7f));
            at = shadow.transform.position;
            ScreenWipePickup.Wipe(null);
            yield return null;
            AssertDefeatedWithChest(shadow, at, ShadowKiller.SoulToll, ChestTiers.StalkerHighQuality);

            Assert.That(Kills(), Is.EqualTo(kills), "Defeating it is not a kill.");
            Assert.That(_hazards.Shadows, Is.Empty);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static void AssertDefeatedWithChest(DeathsShadow shadow, Vector3 at, ShadowKiller killer, ChestTiers tiers)
        {
            Assert.That(shadow == null || !shadow.gameObject.activeInHierarchy, Is.True, $"{killer} defeats it.");
            Assert.That(DeathsShadow.LastDefeatedBy, Is.EqualTo(killer));
            Assert.That(DeathsShadow.LastChest, Is.EqualTo((ChestTiers?)tiers), $"{killer}'s chest config.");
            Assert.That(ActiveChests().Count(c => c.Tiers.Equals(tiers) && Flat(c.transform.position, at) < 4f), Is.GreaterThanOrEqualTo(1),
                $"{killer} drops its chest where the shadow was.");
        }

        [UnityTest]
        public IEnumerator SoulMeteor_HitsThePlayer_After2Seconds_For30_AndWaitsWhilePaused()
        {
            yield return Boot(6, invinciblePlayer: false);
            var player = PlayerController.Instance;
            var health = player.GetComponent<HealthController>();
            int start = health.CurrentHealth;
            var meteor = _hazards.DropMeteor(player.transform.position);
            Assert.That(meteor, Is.Not.Null);

            // A paused game holds the strike.
            var pauser = new object();
            SoulHunter.Core.Services.GameTime.Pause(pauser);
            yield return new WaitForSecondsRealtime(2.5f);
            Assert.That(meteor != null && !meteor.HasStruck, Is.True, "Nothing falls while the game is paused.");
            SoulHunter.Core.Services.GameTime.Resume(pauser);

            yield return WaitGameSeconds(1.6f);
            Assert.That(meteor != null && !meteor.HasStruck, Is.True, "No strike before 2 s.");
            Assert.That(health.CurrentHealth, Is.GreaterThanOrEqualTo(start), "The marker alone does not hurt.");

            Vector3 dropAt = meteor.transform.position;
            int beforeStrike = health.CurrentHealth;
            float deadline = Time.realtimeSinceStartup + 3f;
            // Failed once in a full run (2026-10-01): the strike dealt 0, most likely blocked by a 240 ms invulnerability from
            // something unrelated (VS: invulnerability blocks every hit). The test measures the meteor alone, so it clears
            // the hit invulnerability until the strike.
            var hitUntil = typeof(HealthController).GetField("_hitInvulnerableUntil", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            // Stops on HasStruck, not on the marker going: Destroy is deferred, so a loop that waited for null could read
            // the health once more after the strike and measure 0 (three failures in full runs, 2026-10-01 to -07).
            var struck = meteor; // the C# object keeps HasStruck after the GameObject is destroyed
            while (!struck.HasStruck && Time.realtimeSinceStartup < deadline)
            {
                hitUntil.SetValue(health, -1f);
                beforeStrike = health.CurrentHealth;
                yield return null;
            }
            Assert.That(struck.HasStruck, Is.True, "The marker struck.");
            Assert.That(struck.HitPlayer, Is.True, "The strike reached the player.");
            yield return null;
            Assert.That(meteor == null, Is.True, "The marker went after its strike.");
            int expected = Mathf.Max(1, StageHazardTable.MeteorPlayerDamage - health.Armor);
            Assert.That(beforeStrike - health.CurrentHealth, Is.InRange(expected - 2, expected), $"The strike deals 30 (minus Armor {health.Armor}); invincible {health.IsInvincible}; the player stood {Vector3.Distance(new Vector3(player.transform.position.x, 0f, player.transform.position.z), new Vector3(dropAt.x, 0f, dropAt.z)):0.00} from the marker (radius {StageHazardTable.MeteorRadius}). " + Errors());
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator PileAssault_StandsStill_ItsBulletHurtsThePlayer_ThenItLeaves()
        {
            yield return Boot(8, invinciblePlayer: false);
            var player = PlayerController.Instance;
            var health = player.GetComponent<HealthController>();
            var shooters = _hazards.StartPileAssault(count: 4, durationSeconds: 60f);
            Assert.That(shooters.Count, Is.EqualTo(4), "A ring of the stage's enemies. " + Errors());
            var spots = shooters.Select(s => s.transform.position).ToArray();

            int start = health.CurrentHealth;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (health.CurrentHealth >= start && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(health.CurrentHealth, Is.LessThan(start), "A bullet reached the player. " + Errors());
            int expected = Mathf.Max(1, StageHazardTable.PileBulletDamage - health.Armor);
            Assert.That(start - health.CurrentHealth, Is.InRange(1, expected * 4), "Bullet damage.");
            Assert.That(shooters.Sum(s => s.ShotsFired), Is.GreaterThan(0));
            for (int i = 0; i < shooters.Count; i++)
                Assert.That(Flat(shooters[i].transform.position, spots[i]), Is.LessThan(0.3f), "They stand still.");

            int kills = Kills();
            foreach (var s in shooters) s.RemainingSeconds = 0.2f; // test hook: VS assaults last 20-30 s
            yield return WaitGameSeconds(0.5f);
            Assert.That(shooters.All(s => s == null || !s.gameObject.activeInHierarchy), Is.True, "The assault ends.");
            Assert.That(Kills(), Is.EqualTo(kills), "Leaving is not a kill.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator RailCart_LaunchesWhenHit_AndKillsAnEnemyOnItsRail()
        {
            yield return Boot(3, invinciblePlayer: true);
            yield return WaitUntilRealtime(() => _hazards.Tracks.Count == StageHazardTable.RailCount, "the Stage 3 rails");
            var track = _hazards.Tracks[0];
            var cart = track.Cart;
            Assert.That(cart, Is.Not.Null);
            Assert.That(cart.IsMoving, Is.False);
            Assert.That(Flat(cart.transform.position, track.EndA), Is.LessThan(0.5f), "The cart waits at one end.");

            var enemyObject = _spawner.SpawnHazardEnemy(_spawner.StageEnemyPrefab, track.transform.position);
            var enemy = enemyObject.GetComponent<EnemyController>();
            enemy.Freeze(20f); // stays on the rail
            yield return new WaitForFixedUpdate();
            int kills = Kills();

            cart.Health.TakeDamage(new DamagePacket(RailCart.MaxHealth, cart.transform.position, Vector3.zero));
            Assert.That(cart.IsMoving, Is.True, "Enough damage launches it.");
            Assert.That(cart.gameObject.activeInHierarchy, Is.True, "It launches instead of breaking.");
            Assert.That(cart.Launches, Is.EqualTo(1));

            yield return WaitUntilRealtime(() => !enemyObject.activeSelf, "the cart to kill the enemy on its rail");
            Assert.That(cart.EnemiesHit, Is.GreaterThanOrEqualTo(1));
            if (RunStatsTracker.Instance != null)
                Assert.That(Kills(), Is.EqualTo(kills + 1), "The enemy's death is a kill; the cart is not.");

            yield return WaitUntilRealtime(() => !cart.IsMoving, "the cart to reach the far end");
            Assert.That(Flat(cart.transform.position, track.EndB), Is.LessThan(0.5f), "It stops at the far end.");
            Assert.That(cart.Health.CurrentHealth, Is.EqualTo(RailCart.MaxHealth), "Ready to be launched back.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator RailCart_DefeatsDeathsShadow_WithALowerQualityChest()
        {
            yield return Boot(3, invinciblePlayer: true);
            yield return WaitUntilRealtime(() => _hazards.Tracks.Count == StageHazardTable.RailCount, "the Stage 3 rails");
            var track = _hazards.Tracks[0];
            var cart = track.Cart;
            var shadow = _hazards.SpawnDeathsShadow(60f);
            Assert.That(shadow, Is.Not.Null, "Death's Shadow spawned. " + Errors());
            shadow.transform.position = track.transform.position;
            shadow.GetComponent<EnemyController>().Freeze(20f); // stays on the rail
            yield return new WaitForFixedUpdate();
            Vector3 at = shadow.transform.position;
            int kills = Kills();

            cart.Health.TakeDamage(new DamagePacket(RailCart.MaxHealth, cart.transform.position, Vector3.zero));
            Assert.That(cart.IsMoving, Is.True);
            yield return WaitUntilRealtime(() => shadow == null || !shadow.gameObject.activeInHierarchy, "the cart to defeat Death's Shadow");
            AssertDefeatedWithChest(shadow, at, ShadowKiller.RailCart, ChestTiers.StalkerLowQuality);
            Assert.That(Kills(), Is.EqualTo(kills), "Not a kill.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator NextStage_RemovesTheOldStagesHazards()
        {
            yield return Boot(3, invinciblePlayer: true);
            yield return WaitUntilRealtime(() => _hazards.Tracks.Count == StageHazardTable.RailCount, "the Stage 3 rails");
            var rails = _hazards.Tracks.ToList();
            var meteor = _hazards.DropMeteor(PlayerController.Instance.transform.position + Vector3.right * 20f);
            LevelProgressionManager.Instance.StartStage(4);
            yield return null;
            yield return null;
            Assert.That(rails.All(r => r == null), Is.True, "Stage 3's rails are removed.");
            Assert.That(meteor == null, Is.True, "Pending markers are removed.");
            Assert.That(_hazards.Tracks, Is.Empty, "Stage 4 has no rails.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    
        // ---------------------------------------------------------------- The Blinder (Level 5, Abyss Foscari)

        [UnityTest]
        public IEnumerator TheBlinder_HasItsStatbox_TakesOrdinaryDamage_AndWornDownLeavesItsGemButNoChest()
        {
            yield return Boot(5, invinciblePlayer: true);
            var shadow = _hazards.SpawnDeathsShadow(60f, StageHazardTable.Blinder);
            Assert.That(shadow, Is.Not.Null, Errors());
            shadow.transform.position = PlayerController.Instance.transform.position + new Vector3(6f, 0f, 0f);
            shadow.GetComponent<EnemyController>().Freeze(60f);
            UnityEngine.Physics.SyncTransforms();
            var health = shadow.GetComponent<HealthController>();
            var touch = shadow.GetComponent<TouchDamage>();
            int level = PlayerController.Instance.GetComponent<PlayerExperience>().CurrentLevel;
            Assert.That(health.MaxHealth, Is.EqualTo(500 * level), "health 500, HP x Level.");
            Assert.That(touch.DamageAmount, Is.EqualTo(25), "damage 25.");
            Assert.That(shadow.GetComponent<EnemyController>().TryWeaponFreeze(1f, 2f), Is.False, "res-freeze 2.");

            var whip = PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>(true).First();
            int chests = ActiveChests().Length;
            using (DamageSource.From(whip)) health.TakeDamage(new DamagePacket(10, shadow.transform.position, Vector3.forward));
            Assert.That(health.CurrentHealth, Is.LessThan(health.MaxHealth), "Ordinary weapons hurt the Blinder.");
            using (DamageSource.From(whip)) health.TakeDamage(new DamagePacket(health.CurrentHealth + 5, shadow.transform.position, Vector3.forward));
            yield return null;
            Assert.That(shadow == null || !shadow.gameObject.activeInHierarchy, Is.True, "Worn down, it leaves.");
            Assert.That(ActiveChests().Length, Is.EqualTo(chests), "No chest without an instant kill (balance method).");
            Assert.That(XPGem.ActiveGems.Any(g => g != null && g.isActiveAndEnabled && g.XPValue == StageHazardTable.Blinder.GemValue), Is.True, "xp 30.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator TheBlinder_DefeatedByPentagram_DropsTheHighQualityChest()
        {
            yield return Boot(5, invinciblePlayer: true);
            var shadow = _hazards.SpawnDeathsShadow(60f, StageHazardTable.Blinder);
            shadow.transform.position = PlayerController.Instance.transform.position + new Vector3(6f, 0f, 0f);
            shadow.GetComponent<EnemyController>().Freeze(60f);
            UnityEngine.Physics.SyncTransforms();
            // A gained weapon fires at once (wiki Weapons: "Upgrading a weapon will also reset its Cooldown and activate it
            // instantly"), so the chests are counted before it is given and that first activation is the one checked.
            int chests = ActiveChests().Length;
            WeaponManager.Instance.GiveWeapon(UpgradeType.Pentagram, 1);
            yield return null;
            yield return null;
            var pentagram = PlayerController.Instance.GetComponentsInChildren<PentagramWeapon>(true).First();
            pentagram.enabled = false; // no second activation
            Assert.That(DeathsShadow.LastDefeatedBy, Is.EqualTo(ShadowKiller.Pentagram), Errors());
            Assert.That(DeathsShadow.LastChest.HasValue, Is.True, "The Blinder: \"Defeating it with the aforementioned weapons ... will make it drop a Treasure Chest\", Pentagram first.");
            Assert.That(DeathsShadow.LastChest.Value.FivePercent, Is.EqualTo(6f));
            Assert.That(ActiveChests().Length, Is.GreaterThanOrEqualTo(1), "The chest stays: the activation's own item erase comes before its kills.");
            Assert.That(ActiveChests().Length, Is.LessThanOrEqualTo(chests + 1));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
