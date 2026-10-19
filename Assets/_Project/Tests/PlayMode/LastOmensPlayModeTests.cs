using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Hazards;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// In the real game: the last seven Omens (raw Random_Events, Candybox, The_Drowner, Dragon_Swarm, Flower_Wall_(event),
    /// fetched 2026-10-13), Death's Shadow as an Omen on any level, and Level 9's Closing Arena after its fix. Every test
    /// plays on a throw-away save (GameplayPlayModeTestBase).
    /// </summary>
    public class LastOmensPlayModeTests : GameplayPlayModeTestBase
    {
        private HealthController _heroHealth;

        private IEnumerator BootWithOmens()
        {
            yield return BootIntoGameplay(save => save.ModeRandomEvents = true);
            var player = PlayerController.Instance;
            _heroHealth = player.GetComponent<HealthController>();
            _heroHealth.IsInvincible = true;
            // The hero cannot kill: no level-up pauses the clock, and every enemy stays to be counted.
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            yield return CloseOpenLevelUps();
            Assert.IsNotNull(RandomEventDirector.Instance, "GameSessionManager adds the Omens director. " + Errors());
        }

        private static void MoveHero(Vector3 position)
        {
            var player = PlayerController.Instance;
            position.y = player.transform.position.y;
            if (player.Rigidbody != null) player.Rigidbody.position = position;
            player.transform.position = position;
            UnityEngine.Physics.SyncTransforms();
        }

        private static IEnumerator GameSeconds(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until) yield return null;
        }

        private static System.Collections.Generic.List<EnemyController> Alive() =>
            EnemyController.ActiveEnemies.Where(e => e != null && e.gameObject.activeInHierarchy).ToList();

        // ---------------------------------------------------------------- Powder Ring (VS Roulette)

        [UnityTest]
        public IEnumerator PowderRing_StandsStill_AndAMemberBlowsUpWhenTheHeroComesClose()
        {
            yield return BootWithOmens();
            int killsBefore = RunStatsTracker.Instance != null ? RunStatsTracker.Instance.TotalKills : 0;
            Assert.That(RandomEventDirector.Instance.Fire(OmenEvent.PowderRing), Is.True, "Powder Ring. " + Errors());
            var mines = StageHazards.Instance.Mines.ToList();
            Assert.That(mines.Count, Is.EqualTo(StageHazardTable.MineCount), "A ring of the stage's enemies.");
            var mine = mines[0];
            Vector3 spot = mine.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(Vector3.Distance(mine.transform.position, spot), Is.LessThan(0.05f), "It does not move.");
            Assert.That(mines.All(m => !m.IsArmed), Is.True, "Nobody is armed while the hero is in the middle.");

            // Only the ring stays on the field, so nothing else can hurt the hero while it is measured.
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var other in Alive().Where(e => e.GetComponent<MineSentry>() == null)) other.gameObject.SetActive(false);
            _heroHealth.IsInvincible = false;
            int health = _heroHealth.CurrentHealth;
            MoveHero(spot + Vector3.right * 1.8f); // inside the arm distance (2), clear of the member's body
            yield return WaitUntilRealtime(() => mine == null || mine.IsArmed, "the member arming");
            Assert.That(_heroHealth.CurrentHealth, Is.EqualTo(health), "The fuse first: no damage yet.");
            yield return GameSeconds(StageHazardTable.MineFuseSeconds + 0.3f);
            Assert.That(mine == null || !mine.gameObject.activeInHierarchy, Is.True, "The exploded member is gone.");
            Assert.That(health - _heroHealth.CurrentHealth, Is.EqualTo(StageHazardTable.MineDamage), "Its blast hurt the hero once.");
            if (RunStatsTracker.Instance != null)
                Assert.That(RunStatsTracker.Instance.TotalKills, Is.EqualTo(killsBefore), "A member that blew itself up is not a kill.");
            Assert.That(StageHazards.Instance.Mines.Count(m => m != null && m.isActiveAndEnabled && !m.IsArmed), Is.GreaterThanOrEqualTo(StageHazardTable.MineCount - 3),
                "The far members wait.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // ---------------------------------------------------------------- Thorn Circle (VS Flower Wall)

        [UnityTest]
        public IEnumerator ThornCircle_ClosesIn_StopsTheHero_AndOpensWhereAMemberFalls()
        {
            yield return BootWithOmens();
            Vector3 centre = PlayerController.Instance.transform.position;
            Assert.That(RandomEventDirector.Instance.Fire(OmenEvent.ThornCircle), Is.True, "Thorn Circle. " + Errors());
            var circle = StageHazards.Instance.ThornCircles.Single();
            Assert.That(circle.Standing, Is.EqualTo(StageHazardTable.ThornCount));
            Assert.That(circle.Posts, Is.EqualTo(StageHazardTable.ThornCount), "One solid post per member.");
            Assert.That(circle.Radius, Is.EqualTo(StageHazardTable.ThornStartRadius).Within(0.01f));
            var members = Alive().Where(e => e.GetComponent<StillEnemy>() != null).ToList();
            Assert.That(members.Count, Is.EqualTo(StageHazardTable.ThornCount));
            Assert.That(members.All(e => e.name.StartsWith("SH10_L01_ThornWall")), Is.True, "Level 1's Thorn Wall on every level.");

            yield return GameSeconds(2f);
            Assert.That(circle.Radius, Is.LessThan(StageHazardTable.ThornStartRadius - 0.1f), "It creeps inward.");
            Assert.That(circle.Radius, Is.GreaterThan(StageHazardTable.ThornEndRadius));
            UnityEngine.Physics.SyncTransforms();
            int obstacles = LayerMask.GetMask("Obstacle");
            var member = members[0];
            Vector3 flat = member.transform.position - centre;
            flat.y = 0f;
            Assert.That(flat.magnitude, Is.EqualTo(circle.Radius).Within(0.3f), "Members stand on the ring.");
            Assert.That(UnityEngine.Physics.CheckSphere(member.transform.position, 0.3f, obstacles, QueryTriggerInteraction.Ignore), Is.True,
                "A member's post is solid for the hero (the Obstacle layer).");
            // Between two neighbours the posts still overlap: no way through.
            Vector3 between = centre + Quaternion.Euler(0f, -360f / StageHazardTable.ThornCount * 0.5f, 0f) * flat;
            between.y = member.transform.position.y;
            Assert.That(UnityEngine.Physics.CheckSphere(between, 0.4f, obstacles, QueryTriggerInteraction.Ignore), Is.True, "No gap between neighbours.");

            member.GetComponent<HealthController>().KillSilently();
            yield return null;
            yield return null;
            UnityEngine.Physics.SyncTransforms();
            Assert.That(circle.Standing, Is.EqualTo(StageHazardTable.ThornCount - 1));
            Assert.That(circle.Posts, Is.EqualTo(StageHazardTable.ThornCount - 1), "Its post went with it: the ring is open there.");

            circle.Leave();
            yield return null;
            Assert.That(Alive().Count(e => e.GetComponent<StillEnemy>() != null), Is.Zero, "The survivors leave when the ring ends.");
            Assert.That(GameObject.Find("ThornCircle_Post"), Is.Null, "No post is left behind.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // ---------------------------------------------------------------- Rising Tide (VS The Drowner)

        [UnityTest]
        public IEnumerator RisingTide_FloodsFromBelow_HurtsOnlyInTheWater_AndEndsWithItsCaller()
        {
            yield return BootWithOmens();
            var player = PlayerController.Instance;
            int level = player.GetComponent<PlayerExperience>().CurrentLevel;
            Assert.That(RandomEventDirector.Instance.Fire(OmenEvent.RisingTide), Is.True, "Rising Tide. " + Errors());
            var tide = StageHazards.Instance.Tides.Single();
            Assert.IsNotNull(tide.Caller, "The Tide Caller stands at the screen's corner.");
            Assert.That(tide.Caller.GetComponent<HealthController>().MaxHealth, Is.EqualTo(StageHazardTable.TideCallerHealth(level)), "800 x Level.");
            Assert.That(tide.Caller.transform.position.x, Is.LessThan(player.transform.position.x), "Bottom-left: left of the hero ...");
            Assert.That(tide.Caller.transform.position.z, Is.LessThan(player.transform.position.z), "... and below it.");
            Assert.That(tide.WaterLine, Is.LessThan(player.transform.position.z), "The water starts below the hero.");

            _heroHealth.IsInvincible = false;
            int health = _heroHealth.CurrentHealth;
            float line = tide.WaterLine;
            yield return GameSeconds(1f);
            Assert.That(tide.WaterLine, Is.GreaterThan(line), "It rises.");
            Assert.That(_heroHealth.CurrentHealth, Is.EqualTo(health), "Dry ground does not hurt.");

            MoveHero(new Vector3(player.transform.position.x, 0f, tide.WaterLine - 3f));
            yield return GameSeconds(0.8f);
            int lost = health - _heroHealth.CurrentHealth;
            Assert.That(lost, Is.GreaterThanOrEqualTo(StageHazardTable.TideDamage), "The water hurts.");
            Assert.That(lost % StageHazardTable.TideDamage, Is.Zero, "In ticks of 20.");
            Assert.That(lost, Is.LessThanOrEqualTo(StageHazardTable.TideDamage * 3), "On the tick, not every frame.");
            _heroHealth.IsInvincible = true;

            // "Freezing the Drowner also causes the flood to stop rising."
            tide.Caller.Freeze(2f);
            yield return null;
            float held = tide.WaterLine;
            yield return GameSeconds(0.5f);
            Assert.That(tide.WaterLine, Is.EqualTo(held).Within(1e-4f), "A frozen caller holds the water.");

            // "The flood will disappear if Drowner is defeated".
            tide.Caller.GetComponent<HealthController>().KillSilently();
            yield return null;
            yield return null;
            Assert.That(StageHazards.Instance.Tides.Count(t => t != null), Is.Zero, "No caller, no flood.");
            Assert.That(GameObject.Find("RisingTide"), Is.Null);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// Bug MIS AI-03. The Tide Caller is the level's boss model, whose data drops a chest; its drops are erased, and an
        /// erased carrier passed its chest on, so killing it gave a free chest. Defeated by ordinary damage (here a silent
        /// kill with no killer), the VS Drowner leaves none (/w/The_Drowner: only "instant-kill effects" drop one).
        /// </summary>
        [UnityTest]
        public IEnumerator RisingTide_TheTideCallersDeath_GivesNoChest()
        {
            yield return BootWithOmens();
            EnemyDrop.PendingChests.Clear();
            Assert.That(RandomEventDirector.Instance.Fire(OmenEvent.RisingTide), Is.True, "Rising Tide. " + Errors());
            var caller = StageHazards.Instance.Tides.Single().Caller;
            Assert.IsNotNull(caller, "The Tide Caller stands at the screen's corner.");
            Assert.That(caller.GetComponent<EnemyDrop>().WillDropChest, Is.False, "This life carries no chest.");
            int chestsBefore = Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None).Count(c => c.gameObject.activeInHierarchy);

            caller.GetComponent<HealthController>().KillSilently();
            yield return null;
            yield return null;
            Assert.That(EnemyDrop.PendingChests.Count, Is.Zero, "No chest is passed on to another enemy.");
            Assert.That(Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None).Count(c => c.gameObject.activeInHierarchy),
                Is.EqualTo(chestsBefore), "No chest drops where it fell.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// Owner 2026-10-19 ("SH = VS"), /w/The_Drowner (raw): "Unlike other Reaper types, it is unaffected by Pentagram";
        /// "instantly killed upon taking damage from Gorgeous Moon, Infinite Corridor, Crimson Shroud, ... Rosary"; "Defeating
        /// it with instant-kill effects will make it drop a Treasure Chest" (evo=1, tier3=6, tier2=66, tier1=100).
        /// </summary>
        [UnityTest]
        public IEnumerator RisingTide_TheTideCaller_IgnoresPentagram_AndDropsAChestToAnInstantKill()
        {
            yield return BootWithOmens();
            EnemyDrop.PendingChests.Clear();
            Assert.That(RandomEventDirector.Instance.Fire(OmenEvent.RisingTide), Is.True, "Rising Tide. " + Errors());
            var caller = StageHazards.Instance.Tides.Single().Caller;
            Assert.IsNotNull(caller);
            Assert.That(caller.IgnoresPentagram, Is.True, "Pentagram passes it by.");
            Assert.That(caller.ResistsInstantKill, Is.False, "Its instant killers reach it (the boss model's resistance is not the Drowner's).");
            Assert.That(caller.ResistsDebuffs, Is.True, "Statbox: debuff resistance.");
            int chestsBefore = Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None).Count(c => c.gameObject.activeInHierarchy);

            // Soul Toll (VS Rosary) erases it: a chest with the Drowner's tiers drops where it fell.
            using (DamageSource.From(null, ScreenWipePickup.DisplayName)) caller.GetComponent<HealthController>().KillSilently();
            yield return null;
            yield return null;
            Assert.That(RisingTide.LastChest, Is.EqualTo(ChestTiers.StalkerHighQuality), "The Drowner's 6/66/100 chest.");
            Assert.That(Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None).Count(c => c.gameObject.activeInHierarchy),
                Is.EqualTo(chestsBefore + 1), "One chest drops.");
            Assert.That(EnemyDrop.PendingChests.Count, Is.Zero, "None is passed on to another enemy.");
            Assert.That(caller.IgnoresPentagram, Is.False, "The pooled body is ordinary again.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // ---------------------------------------------------------------- Nightfall and Rootbound (VS Summon Night, Summon II Molise)

        private IEnumerator OnLevelThreeWithAHorde()
        {
            yield return BootWithOmens();
            LevelProgressionManager.Instance.StartStage(3);
            yield return null;
            // A jump leaves Level 1's opening horde where it stood (a real stage clear wipes the arena first).
            foreach (var old in Alive()) old.gameObject.SetActive(false);
            yield return null;
            yield return WaitUntilRealtime(() => Alive().Count >= 10, "Level 3's opening horde");
            Assert.That(Alive().Any(e => e.name.StartsWith("SH10_L01_")), Is.False, "Level 3 has none of Level 1's types by itself.");
        }

        [UnityTest]
        public IEnumerator Nightfall_TurnsEveryEnemyIntoBats_ForAMinute()
        {
            yield return OnLevelThreeWithAHorde();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            int before = Alive().Count;
            Assert.That(RandomEventDirector.Instance.Fire(OmenEvent.Nightfall), Is.True, "Nightfall. " + Errors());
            Assert.That(spawner.WaveTakeoverActive, Is.True);
            var now = Alive();
            Assert.That(now.Count, Is.EqualTo(before), "Replaced one for one, where they stood.");
            Assert.That(now.All(e => e.name.Contains("Bat")), Is.True, "Every enemy is a bat: " + string.Join(", ", now.Where(e => !e.name.Contains("Bat")).Select(e => e.name).Distinct()));
            Assert.That(now.Select(e => e.name.Replace("(Clone)", "")).Distinct().Count(), Is.GreaterThan(1), "\"different kinds of bats\".");
            yield return GameSeconds(1.5f);
            Assert.That(Alive().All(e => e.name.Contains("Bat")), Is.True, "New spawns are bats too.");

            float clock = LevelProgressionManager.Instance.StageElapsedTime;
            LevelProgressionManager.Instance.SkipTo(clock + OmenTable.NightfallSeconds + 1f);
            yield return WaitUntilRealtime(() => !spawner.WaveTakeoverActive, "the takeover ending");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Rootbound_TurnsEveryEnemyIntoPlants_ThatStandStill_AndGiveLittle()
        {
            yield return OnLevelThreeWithAHorde();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            Assert.That(RandomEventDirector.Instance.Fire(OmenEvent.Rootbound), Is.True, "Rootbound. " + Errors());
            Assert.That(spawner.WaveTakeoverUntil - LevelProgressionManager.Instance.StageElapsedTime, Is.EqualTo(OmenTable.RootboundSeconds).Within(1f), "~2 minutes.");
            yield return GameSeconds(0.5f); // some new spawns too
            var plants = Alive();
            Assert.That(plants.Count, Is.GreaterThanOrEqualTo(10));
            Assert.That(plants.All(e => e.name.Contains("GraveLily") || e.name.Contains("ThornWall")), Is.True, "Every enemy is a plant.");
            Assert.That(plants.All(e => e.GetComponent<StillEnemy>() != null), Is.True, "\"These don't move\".");
            Assert.That(plants.All(e => e.GetComponent<EnemyDrop>().GemValue == OmenTable.RootboundGem), Is.True, "\"don't give a lot of experience points\".");
            var watched = plants.Take(6).ToList();
            var spots = watched.Select(e => e.transform.position).ToList();
            yield return GameSeconds(0.6f);
            for (int i = 0; i < watched.Count; i++)
                if (watched[i] != null && watched[i].gameObject.activeInHierarchy)
                    Assert.That(Vector3.Distance(watched[i].transform.position, spots[i]), Is.LessThan(0.1f), watched[i].name + " moved.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // ---------------------------------------------------------------- Serpent Streams (VS Shrimp Swarm)

        [UnityTest]
        public IEnumerator SerpentStreams_CrossSideways_Weaving_OneStreamAfterAnother()
        {
            yield return BootWithOmens();
            int Fliers() => Alive().Count(e => e.IsFlying);
            int before = Fliers();
            Assert.That(RandomEventDirector.Instance.Fire(OmenEvent.SerpentStreams), Is.True, "Serpent Streams. " + Errors());
            Assert.That(Fliers() - before, Is.EqualTo(OmenTable.SerpentMembers), "The first stream at once.");
            var stream = Alive().Where(e => e.IsFlying).ToList();
            float lane = stream[0].transform.position.z;
            Assert.That(stream.All(e => Mathf.Abs(e.transform.position.z - lane) < 0.01f), Is.True, "One file on one lane.");
            float side = 0f, along = 0f;
            for (int i = 0; i < 40; i++)
            {
                yield return new WaitForFixedUpdate();
                foreach (var e in stream)
                {
                    if (e == null || !e.gameObject.activeInHierarchy || e.IsFrozen) continue;
                    along = Mathf.Max(along, Mathf.Abs(e.Rigidbody.linearVelocity.x));
                    side = Mathf.Max(side, Mathf.Abs(e.Rigidbody.linearVelocity.z));
                }
            }
            Assert.That(along, Is.GreaterThan(1f), "They travel across the screen.");
            Assert.That(side, Is.GreaterThan(0.2f), "\"wave-like patterns\": they also weave.");
            yield return GameSeconds(OmenTable.SerpentGapSeconds + 0.3f);
            Assert.That(Fliers() - before, Is.GreaterThan(OmenTable.SerpentMembers), "The next stream follows.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // ---------------------------------------------------------------- Armoury Cache (VS Candybox)

        [UnityTest]
        public IEnumerator ArmouryCache_ListsTheBaseWeaponsThisSaveHasUsed_AndGivesTheChosenOne()
        {
            yield return BootWithOmens();
            var data = GameServices.Instance.Get<SaveService>().CurrentData;
            foreach (var type in new[] { UpgradeType.Knife, UpgradeType.Axe, UpgradeType.Garlic, UpgradeType.Whip })
                CollectionBook.MarkFound(data, CollectionCatalog.ItemId(type));
            // "it has to be used at least once in the entire save file beforehand": the Bible never was, on this save.
            data.CollectionFound.RemoveAll(id => id == CollectionCatalog.ItemId(UpgradeType.Bible));
            var weapons = WeaponManager.Instance;
            Assert.That(weapons.GetWeaponLevel(UpgradeType.Whip), Is.GreaterThan(0), "Kael holds the Whip.");

            Assert.That(RandomEventDirector.Instance.Fire(OmenEvent.ArmouryCache), Is.True, "Armoury Cache. " + Errors());
            var cache = Object.FindFirstObjectByType<WeaponCachePickup>();
            Assert.IsNotNull(cache, "A cache lies on the floor.");
            var drawn = cache.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled && r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh != null).ToList();
            Assert.That(drawn.Count, Is.GreaterThan(0), "The cache is drawn (the chest's meshes).");
            var bounds = drawn[0].bounds;
            foreach (var r in drawn) bounds.Encapsulate(r.bounds);
            Assert.That(bounds.size.magnitude, Is.InRange(0.5f, 6f), "At a size the player can see: " + bounds.size);
            Assert.That(Vector3.Distance(bounds.center, cache.transform.position), Is.LessThan(2.5f), "Where the cache lies: " + bounds.center + " against " + cache.transform.position);
            Assert.That(Vector3.Distance(cache.transform.position, PlayerController.Instance.transform.position), Is.LessThan(OmenTable.CacheDistance + 12f), "Near the hero.");

            Assert.That(cache.Take(), Is.True, "It opens its list. " + Errors());
            yield return null;
            var screen = ArcanaChoiceScreen.Current;
            Assert.That(screen != null && screen.IsOpen, Is.True, "The list is on screen. " + Errors());
            Assert.That(Time.timeScale, Is.Zero, "The game waits for the choice.");
            Assert.That(screen.TitleText.text, Does.Contain("Armoury Cache"));
            var names = screen.Cards.Select(c => c.Arcana.CardName).ToList();
            CollectionAssert.AreEquivalent(WeaponCachePickup.Choices(weapons, data).Select(UpgradeText.NameOf).ToList(), names, "One card per weapon the cache can give.");
            foreach (var type in new[] { UpgradeType.Knife, UpgradeType.Axe, UpgradeType.Garlic })
                Assert.That(names, Does.Contain(UpgradeText.NameOf(type)), "Used on this save and not held.");
            Assert.That(names, Does.Not.Contain(UpgradeText.NameOf(UpgradeType.Whip)), "The Whip is held.");
            Assert.That(names, Does.Not.Contain(UpgradeText.NameOf(UpgradeType.Bible)), "Never used on this save.");
            Assert.That(names, Does.Not.Contain(UpgradeText.NameOf(UpgradeType.TwilightFlock)), "A union is not a base weapon.");
            Assert.That(screen.RandomButton.gameObject.activeSelf, Is.False);
            Assert.That(screen.SkipButton.gameObject.activeSelf, Is.False);
            Assert.That(screen.RerollButton.gameObject.activeSelf, Is.False);
            Assert.That(screen.Cards.All(c => string.IsNullOrEmpty(c.Numeral.text)), Is.True, "A weapon card shows its icon, not a numeral.");

            var axe = screen.Cards.First(c => c.Arcana.CardName == UpgradeText.NameOf(UpgradeType.Axe));
            screen.Select(axe.Arcana);
            Click(screen.TakeButton, "Take");
            yield return WaitUntilRealtime(() => Time.timeScale > 0f, "the game resuming");
            Assert.That(weapons.GetWeaponLevel(UpgradeType.Axe), Is.EqualTo(1), "The chosen weapon joins the inventory at level 1.");
            Assert.That(weapons.GetWeaponLevel(UpgradeType.Knife), Is.Zero);
            yield return null;
            Assert.That(Object.FindFirstObjectByType<WeaponCachePickup>(), Is.Null, "The cache is used up.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // ---------------------------------------------------------------- Death's Shadow as an Omen

        [UnityTest]
        public IEnumerator DeathsShadow_ComesAsAnOmen_OnALevelWithoutItsOwn()
        {
            yield return BootWithOmens();
            Assert.That(LevelProgressionManager.Instance.CurrentStage, Is.EqualTo(1));
            Assert.That(OmenTable.ShadowStage(1), Is.False, "Level 1's own timetable has no Shadow.");
            Assert.That(RandomEventDirector.Instance.Fire(OmenEvent.DeathsShadow), Is.True, "Random Events are the same on any stage. " + Errors());
            Assert.That(StageHazards.Instance.Shadows.Count, Is.EqualTo(1));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // ---------------------------------------------------------------- the guardian opens the next level

        [UnityTest]
        public IEnumerator WithItsGuardianStillStanding_ThirtyMinutes_EndsTheRun_AndSaysWhy()
        {
            // Owner (2026-10-13): Level 2 opens only if Level 1's boss has been defeated by 30:00; otherwise the run ends.
            yield return BootWithOmens();
            var progression = LevelProgressionManager.Instance;
            var session = GameSessionManager.Instance;
            progression.SkipTo(progression.CurrentLevel.RunDurationSeconds - 0.6f);
            yield return WaitUntilRealtime(() => session.IsRunFinished, "the run ending at 30:00");
            Assert.That(progression.StageFailed, Is.True);
            Assert.That(progression.IsBossDefeated, Is.False);
            Assert.That(progression.CurrentStage, Is.EqualTo(1), "Level 2 did not open.");
            Assert.That(progression.IsStageTransitioning, Is.False);
            Assert.That(session.IsQuit, Is.False, "It ends as a lost run, not as a quit.");
            yield return WaitUntilRealtime(() => Object.FindFirstObjectByType<RunResultsScreen>() != null, "the Results screen");
            var screen = Object.FindFirstObjectByType<RunResultsScreen>();
            string shown = string.Join("\n", screen.GetComponentsInChildren<TMPro.TMP_Text>(true).Select(t => t.text));
            Assert.That(shown, Does.Contain("GAME OVER"));
            Assert.That(shown, Does.Contain(LevelProgressionManager.GuardianFailed(1)), "The Results say why the run ended.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator TheGuardian_IsAnnounced_WhenItComes()
        {
            yield return BootWithOmens();
            var progression = LevelProgressionManager.Instance;
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            progression.SkipToBoss();
            yield return WaitUntilRealtime(() => EnemyController.ActiveEnemies.Any(e => e != null && spawner.IsBoss(e)), "the Level 1 boss");
            yield return null;
            var texts = Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(t => t.text).ToList();
            Assert.That(texts.Any(t => t != null && t.Contains(LevelProgressionManager.GuardianGoal(1))), Is.True,
                "The player is told what the guardian is for. " + Errors());
        }

        /// <summary>CTO MIS X3: the rule is told five minutes before the guardian comes, not only when it is already there.</summary>
        [UnityTest]
        public IEnumerator TheGuardianRule_IsTold_FiveMinutesBeforeItComes()
        {
            yield return BootWithOmens();
            var progression = LevelProgressionManager.Instance;
            float bossAt = progression.CurrentLevel.BossStartSeconds;
            string warning = LevelProgressionManager.GuardianWarning(1, bossAt);
            Assert.That(warning, Does.Contain("25:00").And.Contain("Level 2"));
            bool Shown() => Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(t => t.text != null && t.text.Contains(warning));
            Assert.That(Shown(), Is.False, "Not told at the start of the level.");
            // Other notices that the skipped minutes bring (unlocks) show first, in these frames.
            progression.SkipTo(bossAt - LevelProgressionManager.GuardianWarningLeadSeconds - 3f);
            yield return null;
            yield return null;
            Assert.That(Shown(), Is.False, "Not told before 20:00.");
            progression.SkipTo(bossAt - LevelProgressionManager.GuardianWarningLeadSeconds);
            progression.AdvanceClock(1.1f);
            Assert.That(Shown(), Is.True, "At 20:00 the player is told the guardian is coming and what defeating it opens. " + Errors());
        }

        // ---------------------------------------------------------------- Level 9: the Closing Arena

        [UnityTest]
        public IEnumerator TheClosingArena_IsCentredOnTheHero_Shown_AndCostsTenASecondOutside()
        {
            yield return BootWithOmens();
            var player = PlayerController.Instance;
            MoveHero(new Vector3(140f, 0f, -90f)); // far from the world's origin, as a hero arriving from Level 8 is
            yield return null;
            LevelProgressionManager.Instance.StartStage(9);
            yield return null;
            yield return null;
            var system = Object.FindFirstObjectByType<CampaignSignatureSystem>();
            Assert.That(system.ArenaActive, Is.True);
            Vector3 centre = system.ArenaCentre;
            Assert.That(new Vector2(centre.x - 140f, centre.z + 90f).magnitude, Is.LessThan(1f), "The zone is where the hero starts the level.");
            Assert.That(system.ArenaRing != null && system.ArenaRing.enabled, Is.True, "Its edge is drawn.");
            float radius = system.ArenaRadius;
            Vector3 ringPoint = system.ArenaRing.GetPosition(0);
            Assert.That(new Vector2(ringPoint.x - centre.x, ringPoint.z - centre.z).magnitude, Is.EqualTo(radius).Within(0.2f), "The ring is the edge.");

            _heroHealth.IsInvincible = false;
            int health = _heroHealth.CurrentHealth;
            yield return GameSeconds(0.8f);
            Assert.That(_heroHealth.CurrentHealth, Is.EqualTo(health), "Inside the zone nothing hurts.");

            Vector3 outside = centre + Vector3.right * (radius + 4f);
            MoveHero(outside);
            yield return GameSeconds(1.2f);
            Assert.That(Mathf.Abs(player.transform.position.x - outside.x), Is.LessThan(0.5f), "The hero is not thrown back: it is a zone, not a wall.");
            int lost = health - _heroHealth.CurrentHealth;
            // 10 a second in ticks of 5: two or three ticks in 1.2 s (it was 60 a second and more).
            Assert.That(lost % system.ArenaTickDamage, Is.Zero, "Whole ticks.");
            Assert.That(lost, Is.GreaterThanOrEqualTo(system.ArenaTickDamage * 2), "Outside costs health.");
            Assert.That(lost, Is.LessThanOrEqualTo(system.ArenaTickDamage * 3), "Ten a second, not one per frame.");

            MoveHero(centre);
            health = _heroHealth.CurrentHealth;
            yield return GameSeconds(0.8f);
            Assert.That(_heroHealth.CurrentHealth, Is.EqualTo(health), "Back inside it stops.");

            _heroHealth.IsInvincible = true;
            LevelProgressionManager.Instance.StartStage(10);
            yield return null;
            yield return null;
            Assert.That(system.ArenaActive, Is.False);
            Assert.That(system.ArenaRing == null, Is.True, "The ring goes with the level.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// Bug MIS AI-07. The zone closes on the level's clock (StageElapsedTime), so a clock skip (and Hurry's 2x clock) moves
        /// it; it used to count its own Time.deltaTime and stayed near its start radius.
        /// </summary>
        [UnityTest]
        public IEnumerator TheClosingArena_ClosesOnTheStageClock()
        {
            yield return BootWithOmens();
            LevelProgressionManager.Instance.StartStage(9);
            yield return null;
            yield return null;
            var system = Object.FindFirstObjectByType<CampaignSignatureSystem>();
            Assert.That(system.ArenaActive, Is.True);
            var level = LevelProgressionManager.Instance.CurrentLevel;
            float before = system.ArenaRadius;
            LevelProgressionManager.Instance.SkipTo(level.RunDurationSeconds * 0.5f);
            yield return null;
            float half = system.ArenaRadius;
            Assert.That(half, Is.LessThan(before - 5f), "Half the level gone on the clock: the zone has closed in.");
            LevelProgressionManager.Instance.SkipTo(level.RunDurationSeconds * 0.75f); // not to 30:00: that would end the run
            yield return null;
            float later = system.ArenaRadius;
            Assert.That(before - half, Is.EqualTo(2f * (half - later)).Within(0.5f), "It closes evenly along the stage clock.");
            LevelProgressionManager.Instance.StartStage(10);
            yield return null;
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
