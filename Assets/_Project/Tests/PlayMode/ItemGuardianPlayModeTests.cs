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
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// The guardians of the hidden stage items in the real game (VS Atlanteans; wiki Passive_items "Stage items" and the
    /// four Atlantean pages): the guardian comes when the hero is close, the item can be taken while it lives, its death
    /// leaves a Soul Egg and no gem, and an item drawn away from its place wakes its guardian only when taken.
    /// </summary>
    public class ItemGuardianPlayModeTests : GameplayPlayModeTestBase
    {
        private StageItemSpawner _items;

        private IEnumerator Boot()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            _items = Object.FindFirstObjectByType<StageItemSpawner>();
            yield return WaitUntilRealtime(() => _items.Placed.Count == 8, "the stage's eight items");
        }

        private static void Teleport(Vector3 to)
        {
            var player = PlayerController.Instance;
            to.y = player.transform.position.y;
            player.Rigidbody.position = to;
            player.transform.position = to;
        }

        private ItemGuardPost PostOf(UpgradeType item) =>
            _items.Placed.Where(i => i != null).Select(i => i.GetComponent<StageItemPickup>())
                .First(p => p != null && p.Upgrade == item).GetComponent<ItemGuardPost>();

        [UnityTest]
        public IEnumerator TheGuardian_ComesWhenTheHeroIsClose_AndTheItemCanStillBeTaken()
        {
            yield return Boot();
            foreach (var guard in ItemGuardians.All)
            {
                var each = PostOf(guard.Item);
                Assert.IsNotNull(each, $"{guard.Item} is guarded.");
                Assert.That(each.Spawned, Is.False, $"{guard.Name} is not there before the hero comes close.");
            }
            var unguarded = _items.Placed.Select(i => i.GetComponent<StageItemPickup>())
                .Where(p => p != null && !ItemGuardians.For(p.Upgrade).HasValue).ToList();
            Assert.That(unguarded.Count, Is.EqualTo(2), "The stage's own passive and weapon.");
            foreach (var plain in unguarded) Assert.IsNull(plain.GetComponent<ItemGuardPost>(), $"{plain.Upgrade} has no guardian.");

            var post = PostOf(UpgradeType.MetaglioLeft);
            Vector3 home = post.Home;
            // Just outside the wake distance: nothing.
            Teleport(home + Vector3.right * (ItemGuardians.WakeDistance + 3f));
            yield return null;
            yield return null;
            Assert.That(post.Spawned, Is.False, "Not close yet.");

            Teleport(home + Vector3.right * (ItemGuardians.WakeDistance - 2f));
            yield return WaitUntilRealtime(() => post.Spawned, "the guardian of Metaglio Left");
            var body = post.Body.GetComponent<EnemyController>();
            Assert.That(body.Data.EnemyName, Is.EqualTo("Warden of the Left Hand"));
            Assert.That(body.name, Does.StartWith(ItemGuardians.PrefabName(1, "WardenLeftHand")), "Level 1's own model.");
            Vector3 from = body.transform.position - home;
            from.y = 0f;
            Assert.That(from.magnitude, Is.LessThan(2.5f), "It appears where the item lies.");
            float curse = PlayerController.Instance.GetComponent<PlayerStats>().Curse;
            Assert.That(body.GetComponent<HealthController>().MaxHealth, Is.EqualTo(EnemySpawner.ScaleHealthByCurse(30000, curse)), "VS health 30,000 (x Curse).");
            Assert.That(body.GetComponent<TouchDamage>().DamageAmount, Is.EqualTo(50f), "VS damage 50.");
            Assert.That(body.TryWeaponFreeze(ClockLancetWeapon.VsFreezeChance, 1f), Is.False, "res-freeze 2: Clock Lancet cannot freeze it.");
            Assert.That(body.ApplyGarlicDebuff(), Is.False, "res-debuff.");
            Assert.That(body.Data.InstantKillResistance, Is.True, "res-kill.");
            Assert.That(body.IsStageBoss, Is.False);

            // "The player does not have to defeat the guard to pick up the item."
            int before = WeaponManager.Instance.GetWeaponLevel(UpgradeType.MetaglioLeft);
            var pickup = post.GetComponent<StageItemPickup>();
            Teleport(pickup.transform.position);
            yield return WaitUntilRealtime(() => pickup == null, "Metaglio Left to be picked up");
            Assert.That(WeaponManager.Instance.GetWeaponLevel(UpgradeType.MetaglioLeft), Is.EqualTo(before + 1));
            Assert.That(body.gameObject.activeInHierarchy, Is.True, "The guardian lives on and keeps following.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator ADefeatedGuardian_LeavesASoulEgg_AndNoGem()
        {
            yield return Boot();
            GameServices.Instance.TryGet<SaveService>(out var save);
            save.CurrentData.SelectedCharacterName = "Kael";
            save.CurrentData.SoulEggs.RemoveAll(e => e.Character == "Kael");

            var post = PostOf(UpgradeType.RingOfLongHours);
            var body = post.Spawn();
            Assert.IsNotNull(body, "The guardian of the Ring of Long Hours. " + Errors());
            yield return null;
            int gems = XPGem.ActiveGems.Count;
            int eggs = ItemGuardian.EggsDropped;
            Vector3 fell = body.transform.position;
            body.GetComponent<HealthController>().TakeDamage(new DamagePacket(10000000, fell, Vector3.zero, DamageType.Holy));
            yield return null;
            yield return null;
            Assert.That(ItemGuardian.EggsDropped, Is.EqualTo(eggs + 1), "Its death drops one Soul Egg.");
            Assert.That(XPGem.ActiveGems.Count, Is.EqualTo(gems), "\"Instead of experience\": no gem.");
            var egg = Object.FindFirstObjectByType<SoulEggPickup>();
            Assert.IsNotNull(egg, "The egg lies on the ground.");
            Vector3 off = egg.transform.position - fell;
            off.y = 0f;
            Assert.That(off.magnitude, Is.LessThan(1f), "Where the guardian fell.");
            Assert.IsNull(SoulEggs.For(save.CurrentData, "Kael"), "Nothing is given until it is picked up.");

            Teleport(egg.transform.position);
            yield return WaitUntilRealtime(() => egg == null, "the Soul Egg to be picked up by walking onto it");
            Assert.That(SoulEggs.For(save.CurrentData, "Kael").Eggs, Is.EqualTo(1), "One egg for the character being played.");
            Assert.That(RunStatsTracker.Instance.Pickups.Any(p => p.Key == SoulEggPickup.PickupName && p.Value == 1), Is.True, "Listed on the Results.");

            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator AnItemDrawnAway_WakesItsGuardian_OnlyWhenTaken()
        {
            yield return Boot();
            var post = PostOf(UpgradeType.RingOfDarkHours);
            Vector3 home = post.Home;
            // Gathering Call (VS Mad Groove) draws stage items to the hero: the item leaves its place.
            Vector3 hero = PlayerController.Instance.transform.position;
            post.transform.position = hero + Vector3.right * 3f;
            float until = Time.realtimeSinceStartup + 0.4f;
            while (Time.realtimeSinceStartup < until) yield return null;
            Assert.That(post.Spawned, Is.False, "\"The guard will not appear until the item is interacted with.\"");

            var pickup = post.GetComponent<StageItemPickup>();
            pickup.Take();
            Assert.That(post.Spawned, Is.True, "Taking it wakes the guardian.");
            Vector3 from = post.Body.transform.position - home;
            from.y = 0f;
            Assert.That(from.magnitude, Is.LessThan(2.5f), "It appears where the item was laid, not on the hero.");
            yield return null;
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator TheIdol_OnLevelTwo_HasNoGuardian()
        {
            yield return Boot();
            LevelProgressionManager.Instance.StartStage(2);
            yield return WaitUntilRealtime(() => _items.Placed.Count == 9 &&
                _items.Placed.Any(i => i != null && i.GetComponent<StageItemPickup>() is StageItemPickup p && p.Upgrade == UpgradeType.MisersIdol),
                "Level 2's items with the Idol");
            var idol = _items.Placed.Select(i => i.GetComponent<StageItemPickup>()).First(p => p != null && p.Upgrade == UpgradeType.MisersIdol);
            Assert.IsNull(idol.GetComponent<ItemGuardPost>(), "The Stone Mask page names no guard.");
            var left = PostOf(UpgradeType.MetaglioLeft);
            Assert.That(left.Level, Is.EqualTo(2), "The new stage's items carry the new level's guardians.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// Bug MIS AI-04. The 30:00 clear ("All enemies on the screen are cleared", wiki Mad_Forest) used to kill every
        /// enemy, so a kited guardian (res-kill) dropped its Soul Egg. It is now taken off the field without dying; an
        /// ordinary enemy still dies and counts as a kill.
        /// </summary>
        [UnityTest]
        public IEnumerator TheStageClear_TakesAGuardianAway_WithoutItsSoulEgg_AndOrdinaryEnemiesStillDie()
        {
            yield return Boot();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var body = PostOf(UpgradeType.RingOfDarkHours).Spawn();
            Assert.IsNotNull(body, "The guardian of the Ring of Dark Hours. " + Errors());
            var ordinary = (GameObject)typeof(EnemySpawner).GetMethod("SpawnEnemy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(spawner, new object[] { spawner.StageEnemyPrefab });
            Assert.IsNotNull(ordinary, "An ordinary enemy. " + Errors());
            yield return null;
            int eggs = ItemGuardian.EggsDropped;
            int kills = RunStatsTracker.Instance.TotalKills;

            typeof(LevelProgressionManager).GetMethod("CompleteStage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(LevelProgressionManager.Instance, new object[] { "test" });
            yield return null;
            yield return null;
            Assert.That(body.activeInHierarchy, Is.False, "The guardian leaves the field with the clear.");
            Assert.That(ItemGuardian.EggsDropped, Is.EqualTo(eggs), "No Soul Egg: it was not defeated.");
            Assert.That(Object.FindFirstObjectByType<SoulEggPickup>(), Is.Null);
            Assert.That(ordinary.activeInHierarchy, Is.False, "The ordinary enemy is cleared too.");
            Assert.That(RunStatsTracker.Instance.TotalKills, Is.EqualTo(kills + 1), "Only the ordinary enemy counts as a kill.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
