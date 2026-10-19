using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Audio;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Hazards;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// In the real game (raw wiki pages fetched 2026-10-13): a Level 2-10 chest-less wave boss (Gallo Tower 1:00 Scarleton
    /// and 4:00 Undead Mage on Level 4), the four Omens added from Random_Events, the 29:30 pick, and the Inverse mode
    /// track. Every test plays on a throw-away save (GameplayPlayModeTestBase).
    /// </summary>
    public class ChestlessBossAndOmenPlayModeTests : GameplayPlayModeTestBase
    {
        /// <summary>The hero cannot die or kill: no level-up pauses the clock, and every spawned enemy stays to be counted.</summary>
        private static void MakeHeroHarmlessAndSafe()
        {
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
        }

        private static int Alive(string prefabName) =>
            EnemyController.ActiveEnemies.Count(e => e != null && e.gameObject.activeInHierarchy && e.name.StartsWith(prefabName));

        private static int Fliers() =>
            EnemyController.ActiveEnemies.Count(e => e != null && e.gameObject.activeInHierarchy && e.IsFlying);

        // ---------------------------------------------------------------- Chest-less wave bosses

        [UnityTest]
        public IEnumerator LevelFour_SendsItsChestlessWaveBosses_At1And4Minutes()
        {
            // Raw Gallo_Tower "Waves": 1:00 "Bosses & Treasure" = Scarleton, 4:00 = Undead Mage, both with no {{treasure}}.
            yield return BootIntoGameplay();
            MakeHeroHarmlessAndSafe();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var progression = LevelProgressionManager.Instance;
            progression.StartStage(4);
            yield return null;
            yield return null;
            Assert.That(spawner.ChestlessBossesSpawned, Is.EqualTo(0), "A new stage starts its own list.");
            string first = ChestlessWaveBosses.PrefabName(4, "ThriceRisen");
            string second = ChestlessWaveBosses.PrefabName(4, "GateSorcerer");
            Assert.That(Alive(first), Is.Zero, "No Thrice-Risen before 1:00 (its wave starts at 3:00).");

            progression.SkipTo(60f);
            yield return WaitUntilRealtime(() => spawner.ChestlessBossesSpawned == 1, "the 1:00 wave boss");
            Assert.That(Alive(first), Is.EqualTo(1), "One Thrice-Risen (VS Scarleton) comes at 1:00. " + Errors());
            var boss = EnemyController.ActiveEnemies.First(e => e != null && e.gameObject.activeInHierarchy && e.name.StartsWith(first));
            var drop = boss.GetComponent<EnemyDrop>();
            Assert.That(drop == null || !drop.WillDropChest, Is.True, "It carries no chest.");
            Assert.That(boss.IsStageBoss, Is.False, "It is a wave boss, not the level's 25:00 boss.");

            Assert.That(Alive(second), Is.Zero, "No Gate Sorcerer before 4:00 (its wave starts at 16:00).");
            progression.SkipTo(240f);
            yield return WaitUntilRealtime(() => spawner.ChestlessBossesSpawned == 2, "the 4:00 wave boss");
            Assert.That(Alive(second), Is.EqualTo(1), "One Gate Sorcerer (VS Undead Mage) comes at 4:00.");
            var mage = EnemyController.ActiveEnemies.First(e => e != null && e.gameObject.activeInHierarchy && e.name.StartsWith(second));
            var mageDrop = mage.GetComponent<EnemyDrop>();
            Assert.That(mageDrop == null || !mageDrop.WillDropChest, Is.True, "It carries no chest either.");
            yield return null;
            yield return null;
            Assert.That(spawner.ChestlessBossesSpawned, Is.EqualTo(2), "Each comes once.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator LevelFive_SendsItsOwnBossType_At24Minutes_WithItsStatbox()
        {
            // Raw Abyss_Foscari "Waves": 24:00 "Bosses & Treasure" = Maronna Meea Boss, no {{treasure}}. Maronna_Meea:
            // "skills = HP x Level | health = 200 ... res-freeze = 1.1 | res-kill = 1 | res-debuff = 1".
            yield return BootIntoGameplay();
            MakeHeroHarmlessAndSafe();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var progression = LevelProgressionManager.Instance;
            progression.StartStage(5);
            yield return null;
            yield return null;
            string wanted = ChestlessWaveBosses.PrefabName(5, "GreatBogMother");
            Assert.That(Alive(wanted), Is.Zero, "No minute's wave lists it.");

            progression.SkipTo(24f * 60f);
            yield return WaitUntilRealtime(() => spawner.ChestlessBossesSpawned == 1, "the 24:00 wave boss");
            Assert.That(Alive(wanted), Is.EqualTo(1), "One Great Bog Mother (VS Maronna Meea Boss) comes at 24:00. " + Errors());
            var boss = EnemyController.ActiveEnemies.First(e => e != null && e.gameObject.activeInHierarchy && e.name.StartsWith(wanted));
            int level = PlayerController.Instance.GetComponent<PlayerExperience>().CurrentLevel;
            float curse = PlayerController.Instance.GetComponent<PlayerStats>().Curse;
            Assert.AreEqual(EnemySpawner.ScaleHealthByCurse(200 * level, curse), boss.GetComponent<HealthController>().MaxHealth,
                $"HP x Level: 200 x the player's level {level} x Curse {curse}.");
            Assert.That(boss.FreezeResistance, Is.EqualTo(1.1f).Within(1e-4), "res-freeze 1.1");
            Assert.That(boss.TryWeaponFreeze(ClockLancetWeapon.VsFreezeChance, 1f), Is.False, "Clock Lancet cannot freeze it.");
            Assert.That(boss.ApplyGarlicDebuff(), Is.False, "res-debuff: Garlic's debuffs do nothing.");
            Assert.That(boss.Data.InstantKillResistance, Is.True, "res-kill");
            var drop = boss.GetComponent<EnemyDrop>();
            Assert.That(drop == null || !drop.WillDropChest, Is.True, "It carries no chest.");
            Assert.That(boss.IsStageBoss, Is.False);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator ALevelWithoutChestlessBosses_SendsNone()
        {
            // Raw Inlaid_Library "Waves": every boss cell has a {{treasure}}.
            yield return BootIntoGameplay();
            MakeHeroHarmlessAndSafe();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var progression = LevelProgressionManager.Instance;
            progression.StartStage(2);
            yield return null;
            progression.SkipTo(8f * 60f); // Level 1's first chest-less boss minute
            float until = Time.time + 0.5f;
            while (Time.time < until) yield return null;
            Assert.That(spawner.ChestlessBossesSpawned, Is.EqualTo(0), "Level 2 has no chest-less wave boss.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // ---------------------------------------------------------------- Omens

        private IEnumerator BootWithOmens()
        {
            yield return BootIntoGameplay(save => save.ModeRandomEvents = true);
            MakeHeroHarmlessAndSafe();
            yield return CloseOpenLevelUps();
            Assert.IsNotNull(RandomEventDirector.Instance, "GameSessionManager adds the Omens director. " + Errors());
        }

        [UnityTest]
        public IEnumerator Stampede_SendsAFastGroup_AndAnotherAfterItsGap()
        {
            yield return BootWithOmens();
            int before = Fliers();
            Assert.That(RandomEventDirector.Instance.Fire(OmenEvent.Stampede), Is.True, "Stampede. " + Errors());
            Assert.That(Fliers() - before, Is.EqualTo(OmenTable.StampedeMembers), "A large group at once.");
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            int checkedFliers = 0;
            foreach (var enemy in EnemyController.ActiveEnemies.ToList())
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy || !enemy.IsFlying || enemy.IsFrozen) continue;
                Vector3 velocity = enemy.Rigidbody.linearVelocity;
                velocity.y = 0f;
                float expected = enemy.MoveSpeed * EnemyFlyState.SpeedMultiplier * OmenTable.StampedeSpeed;
                // Read after the physics step, so a little drag may already have acted: a band, well above the usual pace
                // (which is expected / 2.14).
                Assert.That(velocity.magnitude, Is.InRange(expected * 0.8f, expected * 1.05f + 0.01f),
                    $"{enemy.name} runs at {OmenTable.StampedeSpeed:0.00}x the usual event pace (VS Mignotaur (rush) 600 against 280).");
                checkedFliers++;
            }
            Assert.That(checkedFliers, Is.GreaterThanOrEqualTo(OmenTable.StampedeMembers));
            // "several groups ... for ~30 seconds": the next group comes one gap later.
            yield return WaitUntilRealtime(() => Fliers() - before >= OmenTable.StampedeMembers * 2, "the second group of the Stampede");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator DreadCircles_ClosesSeveralRings_OneAfterAnother()
        {
            yield return BootWithOmens();
            int before = Fliers();
            Assert.That(RandomEventDirector.Instance.Fire(OmenEvent.DreadCircles), Is.True, "Dread Circles. " + Errors());
            Assert.That(Fliers() - before, Is.EqualTo(OmenTable.DreadMembers), "The first ring at once.");
            yield return WaitUntilRealtime(() => Fliers() - before >= OmenTable.DreadMembers * OmenTable.DreadRings, "every ring of Dread Circles");
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(Fliers() - before, Is.EqualTo(OmenTable.DreadMembers * OmenTable.DreadRings), "Several rings, and no more.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator TramplingRanks_SendsSeveralRanks_OneAfterAnother()
        {
            yield return BootWithOmens();
            int before = Fliers();
            Assert.That(RandomEventDirector.Instance.Fire(OmenEvent.TramplingRanks), Is.True, "Trampling Ranks. " + Errors());
            Assert.That(Fliers() - before, Is.EqualTo(OmenTable.TramplingMembers), "The first rank at once.");
            yield return WaitUntilRealtime(() => Fliers() - before >= OmenTable.TramplingMembers * OmenTable.TramplingRanksCount, "every rank of Trampling Ranks");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator CrossfireRows_StandsTwoShootingColumns_LeftAndRight_ForAMinute()
        {
            yield return BootWithOmens();
            var hazards = StageHazards.Instance;
            Assert.IsNotNull(hazards, "GameSessionManager adds the stage hazards.");
            int before = hazards.PileShooters.Count(p => p != null && p.isActiveAndEnabled);
            Vector3 hero = PlayerController.Instance.transform.position;
            Assert.That(RandomEventDirector.Instance.Fire(OmenEvent.CrossfireRows), Is.True, "Crossfire Rows. " + Errors());
            var shooters = hazards.PileShooters.Where(p => p != null && p.isActiveAndEnabled).ToList();
            Assert.That(shooters.Count - before, Is.EqualTo(OmenTable.CrossfirePerRow * 2), "A pair of columns of 12 (VS Skull Pile Pile).");
            Assert.That(shooters.Count(p => p.transform.position.x < hero.x - 5f), Is.EqualTo(OmenTable.CrossfirePerRow), "One column left of the hero.");
            Assert.That(shooters.Count(p => p.transform.position.x > hero.x + 5f), Is.EqualTo(OmenTable.CrossfirePerRow), "One column right of the hero.");
            foreach (var shooter in shooters)
                Assert.That(shooter.RemainingSeconds, Is.EqualTo(OmenTable.CrossfireSeconds).Within(1f), "They stay a minute (VS Scorpion Lair).");
            // They shoot: the first shots are staggered over the first few seconds.
            yield return WaitUntilRealtime(() => shooters.Any(p => p != null && p.ShotsFired > 0), "a column shooter to fire");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// Bug MIS CORE-04 (2026-10-19): the formation filter read the clock at the XX:30 pick, so a 24:30 pick sent its
        /// formation at 25:00 together with the guardian. It now reads the fire time, and checks again when it fires.
        /// </summary>
        [UnityTest]
        public IEnumerator The2430Pick_SendsNoFormation_WithTheGuardianAt2500()
        {
            yield return BootWithOmens();
            var director = RandomEventDirector.Instance;
            var progression = LevelProgressionManager.Instance;
            float boss = progression.CurrentLevel.BossStartSeconds;
            Assert.That(boss, Is.EqualTo(1500f), "Level 1's guardian comes at 25:00.");

            progression.SkipTo(boss - 30.6f);
            yield return WaitUntilRealtime(() => progression.StageElapsedTime >= boss - 29.8f, "24:30");
            Assert.That(director.Pending.HasValue, Is.True, "24:30 picks an event.");
            Assert.That(OmenTable.IsFormation(director.Pending.Value), Is.False, $"{director.Pending.Value}: no formation fires at the guardian's minute.");
            // Many picks at the same mark: the pool has no formation (8 of 27 events are formations).
            var pickAt = typeof(RandomEventDirector).GetMethod("PickAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            for (int i = 0; i < 60; i++)
            {
                pickAt.Invoke(director, new object[] { boss - OmenTable.MarkSeconds });
                Assert.That(OmenTable.IsFormation(director.Pending.Value), Is.False, $"Pick {i}: {director.Pending.Value}.");
            }

            // A formation forced into the 24:30 pick (as a clock skip could leave one) is dropped when it would fire.
            director.ForcedPick = () => OmenEvent.HordeSwarm;
            pickAt.Invoke(director, new object[] { boss - OmenTable.MarkSeconds });
            director.ForcedPick = null;
            Assert.That(director.Pending, Is.EqualTo(OmenEvent.HordeSwarm));
            int fired = director.FiredCount;
            progression.SkipTo(boss - 0.6f);
            yield return WaitUntilRealtime(() => progression.StageElapsedTime >= boss + 0.3f, "25:00");
            Assert.That(director.FiredCount, Is.EqualTo(fired), "The formation is dropped at the guardian's minute.");
            Assert.That(director.Pending, Is.Null);
            Assert.That(OmenTable.FormationsAllowedAt(boss - 1f, boss, false), Is.True);
            Assert.That(OmenTable.FormationsAllowedAt(boss, boss, false), Is.False);
            Assert.That(OmenTable.FormationsAllowedAt(boss + 60f, boss, true), Is.True, "Once the guardian falls, formations come again.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator The2930Pick_IsMade_AndFiresAt3000_BeforeTheStageClears()
        {
            // Raw Random_Events: "This happens at every XX:30 mark ... trigger at the next XX:00 mark", with no exception.
            yield return BootWithOmens();
            var director = RandomEventDirector.Instance;
            var progression = LevelProgressionManager.Instance;
            float limit = progression.CurrentLevel.RunDurationSeconds;
            Assert.That(limit, Is.EqualTo(1800f), "Level 1 is a 30-minute stage.");

            progression.SkipTo(limit - 30.6f);
            yield return WaitUntilRealtime(() => progression.StageElapsedTime >= limit - 29.8f, "29:30");
            Assert.That(director.Pending.HasValue, Is.True, "29:30 picks an event, like every XX:30.");
            var picked = director.Pending.Value;
            Assert.That(OmenTable.OutlastsStageClear(picked), Is.True, $"{picked}: the last mark picks an event that outlasts the stage clear.");
            Assert.That(director.PendingFiresAt, Is.EqualTo(limit));

            int fired = director.FiredCount;
            // Owner (2026-10-13): the level clears at 30:00 only when its guardian has been defeated.
            progression.ReportBossDefeated();
            progression.SkipTo(limit - 0.6f);
            yield return WaitUntilRealtime(() => director.FiredCount == fired + 1, "the 30:00 event");
            Assert.That(director.LastFired, Is.EqualTo(picked), "30:00 fires it.");
            yield return WaitUntilRealtime(() => progression.IsStageTransitioning || progression.CurrentStage == 2, "the stage to clear at 30:00");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // ---------------------------------------------------------------- Inverse mode music

        [UnityTest]
        public IEnumerator Inverse_EachLevelPlaysItsMirrorLevelsTrack()
        {
            AudioManager.EnsureInstance(); // as AudioPlayModeTests: the game makes it at startup, a test boot may not
            yield return BootIntoGameplay(save => save.ModeInverse = true);
            MakeHeroHarmlessAndSafe();
            var audio = AudioManager.Instance;
            Assert.IsNotNull(audio, "AudioManager exists. " + Errors());
            yield return null;
            Assert.That(RunModes.Inverse, Is.True);
            Assert.That(audio.CurrentMusic, Is.SameAs(audio.StageMusic[9]), "In Inverse, Level 1 plays Level 10's track.");
            Assert.That(audio.CurrentMusic, Is.Not.SameAs(audio.StageMusic[0]));
            LevelProgressionManager.Instance.StartStage(4);
            yield return null;
            Assert.That(audio.CurrentMusic, Is.SameAs(audio.StageMusic[6]), "Level 4 plays Level 7's track.");
        }

        [UnityTest]
        public IEnumerator Inverse_AChosenTrack_StillWins()
        {
            AudioManager.EnsureInstance();
            yield return BootIntoGameplay(save => { save.ModeInverse = true; save.MusicChoice = MusicSelection.FirstLevelTrack + 2; });
            var audio = AudioManager.Instance;
            yield return null;
            Assert.That(audio.CurrentMusic, Is.SameAs(audio.StageMusic[2]), "The Soundtrack card's Level 3 track plays, Inverse or not.");
        }
    }
}
