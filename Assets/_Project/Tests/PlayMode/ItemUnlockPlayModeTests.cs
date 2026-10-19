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
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// The VS unlock system in the real game (2026-10-12): a locked item is never offered by a level-up and is offered once
    /// unlocked; breaking 20 Soul Lanterns unlocks its item with a notice; a Soul Casket's guards must fall before it opens
    /// and frees its character; a relic is collected, recorded and gives its boon. Every save goes to the throw-away file.
    /// </summary>
    public class ItemUnlockPlayModeTests : GameplayPlayModeTestBase
    {
        private static GameData Save() => GameServices.Instance.Get<SaveService>().CurrentData;

        private IEnumerator QuietRun(System.Action<GameData> configureSave)
        {
            yield return BootIntoGameplay(configureSave);
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
            // The hero's weapons off: no kill, gem or level-up changes what the test measures.
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>()) weapon.enabled = false;
            yield return null;
        }

        [UnityTest]
        public IEnumerator ALockedItem_IsNeverOffered_AndIsOfferedOnceUnlocked()
        {
            yield return QuietRun(ItemUnlocks.LockEverything);
            var data = Save();
            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            Assert.IsNotNull(levelUp, Errors());
            Assert.That(ItemUnlocks.IsUnlocked(data, "FireWand"), Is.False, "The run starts with the Fire Wand locked.");
            Assert.That(ItemUnlocks.IsUnlocked(data, "Garlic"), Is.False);

            var valid = levelUp.GetValidUpgrades();
            Assert.That(valid, Is.Not.Empty, "Items open from the start are offered. " + Errors());
            Assert.That(valid.Any(u => u.Type == UpgradeType.FireWand), Is.False, "A locked weapon is not in the pool.");
            Assert.That(valid.Any(u => u.Type == UpgradeType.Garlic), Is.False);
            Assert.That(valid.Any(u => u.Type == UpgradeType.Knife), Is.True, "The Knife is unlocked by default.");
            // Nothing new in the pool is locked, and real draws never hold a locked item.
            var weapons = WeaponManager.Instance;
            foreach (var upgrade in valid)
                if (weapons.GetWeaponLevel(upgrade.Type) == 0)
                    Assert.That(ItemUnlocks.IsUnlocked(data, upgrade.Type.ToString()), Is.True, upgrade.Type + " is offered although locked.");
            for (int i = 0; i < 40; i++)
                foreach (var choice in levelUp.GetRandomValidUpgrades(4))
                    Assert.That(choice.Type, Is.Not.EqualTo(UpgradeType.FireWand).And.Not.EqualTo(UpgradeType.Garlic));

            Assert.That(ItemUnlocks.Unlock(data, "FireWand"), Is.True);
            valid = levelUp.GetValidUpgrades();
            Assert.That(valid.Any(u => u.Type == UpgradeType.FireWand && u.Level == 1), Is.True, "Unlocked: the Fire Wand is offered as a new item.");
            Assert.That(valid.Any(u => u.Type == UpgradeType.Garlic), Is.False, "Garlic is still locked.");

            // An item held although locked (a character's starting weapon, a stage item) can be levelled.
            weapons.GiveWeapon(UpgradeType.Garlic, 1);
            yield return null;
            Assert.That(levelUp.GetValidUpgrades().Any(u => u.Type == UpgradeType.Garlic && u.Level == 2), Is.True);
        }

        [UnityTest]
        public IEnumerator APickupEveryFrame_RunsTheUnlockCheckAtMostFourTimesASecond_AndStillUnlocks()
        {
            // Bug MIS ITM-08: every pickup made the whole item-unlock check run on its frame (Gold Fever coins, gem streams).
            yield return QuietRun(ItemUnlocks.LockEverything);
            var data = Save();
            var tracker = GameSessionManager.Instance.GetComponent<ItemUnlockTracker>();
            var stats = RunStatsTracker.Instance;
            yield return null;
            int before = tracker.EvaluationCount;
            float end = Time.unscaledTime + 1f;
            int frames = 0;
            while (Time.unscaledTime < end)
            {
                stats.RecordPickup(PickupNames.Coin);
                frames++;
                yield return null;
            }
            int checks = tracker.EvaluationCount - before;
            // 1 s at PickupCheckSeconds 0.25 plus up to two new stage seconds and the first frame.
            Assert.That(checks, Is.LessThanOrEqualTo(8), $"{checks} checks in {frames} frames with a pickup each.");
            Assert.That(checks, Is.GreaterThanOrEqualTo(2), "Pickups still make the check run.");

            // Five chickens still unlock the Garlic, now within PickupCheckSeconds.
            data.FloorChickensFound = 4;
            stats.RecordPickup(PickupNames.Chicken);
            yield return WaitUntilRealtime(() => ItemUnlocks.IsUnlocked(data, "Garlic"), "the Garlic to unlock at 5 chickens");
        }

        [UnityTest]
        public IEnumerator BreakingTwentyLanterns_UnlocksItsItem_WithANotice()        [UnityTest]
        public IEnumerator BreakingTwentyLanterns_UnlocksItsItem_WithANotice()
        {
            yield return QuietRun(ItemUnlocks.LockEverything);
            var data = Save();
            var session = GameSessionManager.Instance;
            Assert.IsNotNull(session.GetComponent<ItemUnlockTracker>(), "The session owns the item unlock tracker.");
            var lanterns = Object.FindFirstObjectByType<LightSourceSpawner>();
            lanterns.enabled = false; // only this test's lanterns
            Vector3 far = PlayerController.Instance.transform.position + Vector3.right * 30f;

            for (int i = 1; i <= 20; i++)
            {
                var lantern = lanterns.Spawn(far + Vector3.forward * (i % 5));
                Assert.IsNotNull(lantern, "The Soul Lantern prefab is wired. " + Errors());
                lantern.GetComponent<HealthController>().TakeDamage(new DamagePacket(999, lantern.transform.position, Vector3.zero));
                yield return null;
                Assert.That(data.LightSourcesBroken, Is.EqualTo(i), "Each broken lantern is counted on the save.");
                if (i == 19)
                {
                    yield return null;
                    Assert.That(ItemUnlocks.IsUnlocked(data, "FireWand"), Is.False, "19 lanterns are not enough.");
                }
            }
            yield return WaitUntilRealtime(() => ItemUnlocks.IsUnlocked(data, "FireWand"), "the Fire Wand to unlock at 20 lanterns");

            var stats = RunStatsTracker.Instance;
            string line = stats.Unlocks.FirstOrDefault(u => u.StartsWith("New item unlocked: Fire Wand"));
            Assert.IsNotNull(line, "The unlock is announced and listed for the Results. " + string.Join(" | ", stats.Unlocks));
            Assert.That(line.Length, Is.GreaterThan("New item unlocked: Fire Wand.".Length), "The notice also says what the item does: " + line);
            Assert.IsNotNull(session.GetComponent<CharacterUnlockTracker>().Notice, "The unlock notice is shown.");
            // Offered from now on.
            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            Assert.That(levelUp.GetValidUpgrades().Any(u => u.Type == UpgradeType.FireWand), Is.True);
        }

        [UnityTest]
        public IEnumerator ASoulCasket_OpensOnlyAfterItsGuardsFall_AndFreesItsCharacter()
        {
            var first = CasketBook.OnLevel(1).Value;
            yield return QuietRun(save =>
            {
                save.UnlockedCharacters.Remove(first.Character);
                save.UnlockedAchievements.Remove(CasketBook.OpenedId(1));
                save.UnlockedAchievements.Remove(CasketBook.FreedId(first.Character));
            });
            var data = Save();
            var sites = GameSessionManager.Instance.GetComponent<UnlockSiteSpawner>();
            Assert.IsNotNull(sites, "The session owns the casket and relic spawner.");
            yield return WaitUntilRealtime(() => sites.Casket != null, "Level 1's Soul Casket to be placed");
            var casket = sites.Casket;
            var player = PlayerController.Instance;
            float distance = Vector3.Distance(casket.transform.position, player.transform.position);
            Assert.That(distance, Is.GreaterThan(90f), "The casket lies far from the start, beyond the stage items.");
            Assert.That(casket.GuardsSpawned, Is.False, "No guards until the hero comes near.");
            Assert.That(casket.MapVisible, Is.True);
            Assert.That(casket.MapKind, Is.EqualTo("Casket"));
            Assert.That(casket.GetComponentsInChildren<Renderer>().Length, Is.GreaterThan(0), "It is drawn (the approved chest model, darkened).");

            string opened = null;
            System.Action<string> onOpened = name => opened = name;
            SoulCasket.CasketOpened += onOpened;
            try
            {
                casket.SpawnGuards();
                Assert.That(casket.Guards.Count, Is.GreaterThan(0).And.LessThanOrEqualTo(SoulCasket.GuardCount), "A ring of the level's elites. " + Errors());
                foreach (var guard in casket.Guards)
                    Assert.IsNotNull(guard.GetComponent<EnemyController>(), "A guard is a real enemy.");
                Assert.That(casket.IsReady, Is.False);
                Assert.That(casket.Open(), Is.False, "It cannot be opened while guards live.");
                Assert.That(casket.SeamColour, Is.EqualTo(SoulCasket.GuardedSeam));

                foreach (var guard in casket.Guards.ToList())
                    guard.GetComponent<HealthController>().TakeDamage(new DamagePacket(9999999, guard.transform.position, Vector3.zero));
                yield return WaitUntilRealtime(() => casket.IsReady, "the casket to be ready once every guard is down", () => $"{casket.GuardsLeft} guards left");
                Assert.That(casket.SeamColour, Is.EqualTo(SoulCasket.ReadySeam), "A ready casket glows.");

                int casketsBefore = RunStatsTracker.Instance.PickupCount(CasketBook.PickupName);
                Assert.That(casket.Open(), Is.True);
                Assert.That(casket.FreedCharacter, Is.True);
                Assert.That(casket.MapVisible, Is.False);
                Assert.That(data.UnlockedCharacters, Does.Contain(first.Character), "The character is unlocked on the save.");
                Assert.That(CasketBook.WasFreed(data, first.Character), Is.True);
                Assert.That(CasketBook.IsOpened(data, 1), Is.True);
                Assert.That(opened, Is.EqualTo(first.Character), "CasketOpened is raised with the character's name.");
                Assert.That(RunStatsTracker.Instance.PickupCount(CasketBook.PickupName), Is.EqualTo(casketsBefore + 1), "The Results list the casket.");
                Assert.That(RunStatsTracker.Instance.Unlocks.Any(u => u.Contains(first.Character)), Is.True, "The unlock is announced.");
                yield return null;
                yield return null;
                Assert.That(CollectionBook.IsFound(data, CasketBook.CollectionId), Is.True, "Its Collection entry is found.");
                // The next visit to Level 1 places no casket: it is opened on the save.
                sites.Place(1);
                Assert.IsNull(sites.Casket);
            }
            finally
            {
                SoulCasket.CasketOpened -= onOpened;
            }
        }

        [UnityTest]
        public IEnumerator ARelic_IsCollected_Recorded_AndGivesItsBoon()
        {
            var bell = RelicBook.All.First(r => r.UnlocksPowerUp == PowerUpId.Charm);
            var deck = RelicBook.All.First(r => r.Boon == RelicBoon.Luck);
            yield return QuietRun(save =>
            {
                save.CollectionFound.Remove(RelicBook.CollectionId(bell));
                save.CollectionFound.Remove(RelicBook.CollectionId(deck));
                save.UnlockedAchievements.Remove(PowerUpUnlocks.WholeId(PowerUpId.Charm));
            });
            var data = Save();
            var player = PlayerController.Instance;
            var sites = GameSessionManager.Instance.GetComponent<UnlockSiteSpawner>();
            yield return WaitUntilRealtime(() => sites.PlacedFor == 1, "Level 1's sites to be placed");
            Assert.That(sites.Relics, Is.Empty, "Level 1's relic appears at 9:00, not at the start.");
            Assert.That(PowerUpUnlocks.UnlockedRanks(data, PowerUpId.Charm), Is.EqualTo(0));

            LevelProgressionManager.Instance.SkipTo(bell.AppearsAtSeconds + 1f);
            yield return WaitUntilRealtime(() => sites.Relics.Count == 1, "the relic to appear at its minute");
            var relic = sites.Relics[0];
            Assert.That(relic.Info.Name, Is.EqualTo(bell.Name));
            Assert.That(relic.MapKind, Is.EqualTo("Relic"));
            Assert.That(relic.MapVisible, Is.True);
            Assert.That(Vector3.Distance(relic.transform.position, player.transform.position), Is.GreaterThan(60f), "It lies far from the start.");

            string found = null;
            System.Action<string> onFound = id => found = id;
            RelicPickup.RelicFound += onFound;
            try
            {
                Assert.That(relic.Take(), Is.True);
                Assert.That(RelicBook.IsFound(data, bell), Is.True, "Recorded on the save.");
                Assert.That(found, Is.EqualTo(RelicBook.IdOf(bell)), "RelicFound is raised with the relic's stable id.");
                Assert.That(PowerUpUnlocks.UnlockedRanks(data, PowerUpId.Charm), Is.EqualTo(PowerUps.Info(PowerUpId.Charm).MaxRank), "The Charm PowerUp is unlocked.");
                Assert.That(RunStatsTracker.Instance.PickupCount(bell.Name), Is.EqualTo(1), "The Results list the relic.");
                Assert.That(RunStatsTracker.Instance.Unlocks.Any(u => u.StartsWith("Relic found: " + bell.Name) && u.Contains("Charm")), Is.True,
                    "The notice says what was found and what it does.");

                // A boon relic raises the stat in this run at once, by its small amount.
                float luckBefore = player.Stats.Luck;
                var second = RelicPickup.Create(deck, player.transform.position + Vector3.right * 20f);
                found = null;
                Assert.That(second.Take(), Is.True);
                Assert.That(player.Stats.Luck - luckBefore, Is.EqualTo(deck.Amount).Within(0.0005f), "+5% Luck.");
                Assert.That(found, Is.EqualTo(RelicBook.IdOf(deck)));
                Assert.That(RelicBook.BoonTotal(data, RelicBoon.Luck), Is.EqualTo(deck.Amount).Within(0.0001f), "Later runs get it from the save.");

                // Found once: a copy gives nothing more, and the level no longer places it.
                var copy = RelicPickup.Create(deck, player.transform.position + Vector3.right * 22f);
                found = null;
                Assert.That(copy.Take(), Is.False);
                Assert.IsNull(found);
                Assert.That(player.Stats.Luck - luckBefore, Is.EqualTo(deck.Amount).Within(0.0005f));
                sites.Place(1);
                yield return null;
                yield return null;
                Assert.That(sites.Relics, Is.Empty, "A found relic does not appear again.");
            }
            finally
            {
                RelicPickup.RelicFound -= onFound;
            }
        }

        /// <summary>
        /// Bug MIS AI-04. Guards cleared by the stage's end (the 30:00 wipe) are not defeated by the hero, and the next level
        /// replaces the casket: it stays sealed through the transition.
        /// </summary>
        [UnityTest]
        public IEnumerator ASoulCasket_StaysSealed_WhenTheStageClearWipesItsGuards()
        {
            var first = CasketBook.OnLevel(1).Value;
            yield return QuietRun(save =>
            {
                save.UnlockedCharacters.Remove(first.Character);
                save.UnlockedAchievements.Remove(CasketBook.OpenedId(1));
                save.UnlockedAchievements.Remove(CasketBook.FreedId(first.Character));
            });
            var sites = GameSessionManager.Instance.GetComponent<UnlockSiteSpawner>();
            yield return WaitUntilRealtime(() => sites.Casket != null, "Level 1's Soul Casket to be placed");
            var casket = sites.Casket;
            casket.SpawnGuards();
            Assert.That(casket.Guards.Count, Is.GreaterThan(0), "A ring of guards. " + Errors());

            typeof(LevelProgressionManager).GetMethod("CompleteStage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(LevelProgressionManager.Instance, new object[] { "test" });
            for (int i = 0; i < 5; i++) yield return null;
            Assert.That(casket.Guards.All(g => g == null || !g.activeInHierarchy), Is.True, "The clear empties the arena.");
            Assert.That(casket.IsReady, Is.False, "The casket stays sealed.");
            Assert.That(casket.Open(), Is.False);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
