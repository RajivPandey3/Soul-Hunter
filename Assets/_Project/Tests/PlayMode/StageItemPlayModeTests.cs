using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using Kind = SoulHunter.Gameplay.Pickups.StageItems.Kind;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS stage items in the real game: each stage lays its four items on open ground a few tiles from the start,
    /// walking over a passive or weapon gives it like a level-up, and the next stage replaces the leftovers.
    /// </summary>
    public class StageItemPlayModeTests : GameplayPlayModeTestBase
    {
        private StageItemSpawner _items;
        private Vector3 _start;

        private IEnumerator Boot()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
            _items = Object.FindFirstObjectByType<StageItemSpawner>();
            Assert.IsNotNull(_items, "Every run has a stage-item spawner. " + Errors());
            // Four diagonal items, the two Metaglios and (2026-10-12) the two rings.
            yield return WaitUntilRealtime(() => _items.Placed.Count == 8, "the stage's eight items");
            _start = PlayerController.Instance.transform.position;
        }

        private static void Teleport(Vector3 to)
        {
            var player = PlayerController.Instance;
            to.y = player.transform.position.y;
            player.Rigidbody.position = to;
            player.transform.position = to;
        }

        [UnityTest]
        public IEnumerator StageStart_LaysFourItems_OnOpenGround_AFewTilesOut()
        {
            yield return Boot();
            var plan = StageItems.Plan(LevelProgressionManager.Instance.CurrentStage);
            foreach (var item in _items.Placed)
            {
                Assert.That(item != null && item.activeInHierarchy, Is.True, "Placed items are in the world.");
                Vector3 offset = item.transform.position - _start;
                offset.y = 0f;
                Assert.That(plan.Min(e => Vector3.Distance(offset, e.Offset)), Is.LessThan(10f), $"{item.name} sits at its planned spot.");
                Assert.That(GroundCheck.IsOpen(item.transform.position, 0.5f), Is.True, $"{item.name} lies on open ground.");
            }
            Assert.That(_items.Placed.Count(i => i.GetComponent<StageItemPickup>() != null), Is.EqualTo(6), "A passive, a weapon, the two Metaglios and the two rings.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator WalkingOverPassiveAndWeapon_GivesThem_LikeALevelUp()
        {
            yield return Boot();
            var weapons = WeaponManager.Instance;
            foreach (var pickup in _items.Placed.Select(i => i.GetComponent<StageItemPickup>()).Where(p => p != null).ToList())
            {
                var type = pickup.Upgrade;
                int before = weapons.GetWeaponLevel(type);
                Teleport(pickup.transform.position);
                yield return WaitUntilRealtime(() => pickup == null, $"the {type} stage item to be picked up by walking onto it");
                Assert.That(weapons.GetWeaponLevel(type), Is.EqualTo(before + 1), $"{type} went up one level.");
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator NextStage_ReplacesTheLeftovers_WithItsOwnItems()
        {
            yield return Boot();
            var old = _items.Placed.ToList();
            Teleport(_start + new Vector3(7f, 0f, 3f));
            LevelProgressionManager.Instance.StartStage(2);
            yield return null;
            yield return null;
            yield return null;
            // Pooled chickens and Vacuums go back to their pools and may be handed straight out again as stage 2 items.
            Assert.That(old.All(i => i == null || !i.activeInHierarchy || _items.Placed.Contains(i)), Is.True, "Stage 1 leftovers are gone.");
            Assert.That(old.Where(i => i != null && i.GetComponent<StageItemPickup>() != null), Is.Empty, "Stage 1's passive and weapon are removed.");
            Assert.That(_items.Placed.Count, Is.EqualTo(9), "Stage 2 has its own items: the usual eight and Miser's Idol (VS Stone Mask on Inlaid Library).");
            var stage2Passive = StageItems.Plan(2).First(e => e.Kind == Kind.Passive && !UpgradeRarity.IsStageItemOnly(e.Upgrade)).Upgrade;
            Assert.That(_items.Placed.Any(i => i.GetComponent<StageItemPickup>()?.Upgrade == stage2Passive), Is.True);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator ATakenStageChicken_ReusedByThePoolAsADrop_SurvivesTheNextStage()
        {
            // Bug MIS ITM-03: the spawner kept the pooled chicken after it was taken; the pool handed the same object out
            // again as an ordinary drop, and the next stage's Clear() switched that live drop off.
            yield return Boot();
            var chicken = _items.Placed.First(i => i != null && i.GetComponent<HealthPickup>() != null);
            chicken.SetActive(false); // taken: back to its pool
            yield return null;
            var drop = PickupPoolManager.Instance.SpawnChicken(_start + new Vector3(9f, 0f, 9f));
            Assert.That(drop, Is.SameAs(chicken), "The pool reuses the last returned chicken (LIFO), as in a real run.");
            Assert.That(StageItemMark.StillOnGround(drop), Is.False, "A reused chicken is an ordinary drop, not a stage item.");
            Teleport(_start + new Vector3(-7f, 0f, -3f));
            LevelProgressionManager.Instance.StartStage(2);
            yield return null;
            yield return null;
            yield return null;
            Assert.That(drop != null && drop.activeInHierarchy, Is.True, "The ordinary drop is still on the ground after the stage changes.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator WithNoFreeSlot_AStageItemBecomesAnExtraItem_AndAMaxedOnePaysGold()
        {
            // VS wiki (Passive_items, "Stage items"): "If they already have 6 or more passive items, it becomes an additional
            // passive item and can be thereafter upgraded from level ups"; Weapons: "it is possible to carry more by picking
            // up weapons that generate with the stage".
            yield return Boot();
            var weapons = WeaponManager.Instance;
            var weaponItem = _items.Placed.Select(i => i.GetComponent<StageItemPickup>()).First(p => p != null && StageItems.Plan(1).Any(e => e.Kind == Kind.Weapon && e.Upgrade == p.Upgrade));
            var passiveItem = _items.Placed.Select(i => i.GetComponent<StageItemPickup>()).First(p => p != null && StageItems.Plan(1).Any(e => e.Kind == Kind.Passive && e.Upgrade == p.Upgrade && !WeaponManager.IsStagePassive(e.Upgrade)));
            // Fill the six weapon slots with other weapons.
            foreach (var type in new[] { UpgradeData.UpgradeType.MagicWand, UpgradeData.UpgradeType.Garlic, UpgradeData.UpgradeType.Bible,
                                         UpgradeData.UpgradeType.Pentagram, UpgradeData.UpgradeType.Laurel, UpgradeData.UpgradeType.Gun })
                if (weapons.GetActiveWeaponsCount() < 6 && type != weaponItem.Upgrade) weapons.GiveWeapon(type, 1);
            Assert.That(weapons.GetActiveWeaponsCount(), Is.EqualTo(6));
            // And the six passive slots with other passives.
            foreach (var type in new[] { UpgradeData.UpgradeType.Armor, UpgradeData.UpgradeType.EmptyTome, UpgradeData.UpgradeType.Bracer,
                                         UpgradeData.UpgradeType.Clover, UpgradeData.UpgradeType.Crown, UpgradeData.UpgradeType.Pummarola,
                                         UpgradeData.UpgradeType.Candelabrador })
                if (weapons.GetPassiveCount() < 6 && type != passiveItem.Upgrade) weapons.GiveWeapon(type, 1);
            Assert.That(weapons.GetPassiveCount(), Is.EqualTo(6));
            Assert.That(weapons.HasSlotFor(weaponItem.Upgrade), Is.False, "A level-up could not give it.");

            var weaponType = weaponItem.Upgrade;
            Assert.That(weaponItem.Take(), Is.True, "A stage weapon goes past the six slots.");
            Assert.That(weapons.GetWeaponLevel(weaponType), Is.EqualTo(1));
            Assert.That(weapons.GetActiveWeaponsCount(), Is.EqualTo(7));
            Assert.That(weapons.GetEquippedWeapons().Count, Is.EqualTo(7), "The seventh weapon is really held.");
            var passiveType = passiveItem.Upgrade;
            Assert.That(passiveItem.Take(), Is.True, "A stage passive goes past the six slots.");
            Assert.That(weapons.GetWeaponLevel(passiveType), Is.EqualTo(1));
            Assert.That(weapons.GetPassiveCount(), Is.EqualTo(7));
            Assert.That(weapons.HasSlotFor(passiveType), Is.True, "Once held it can be levelled from level-ups.");

            // A stage item of something already at its max level has no level to give: a coin bag (wiki silent; balance method).
            weapons.GiveWeapon(passiveType, weapons.GetMaxWeaponLevel(passiveType));
            var maxed = StageItemPickup.Create(passiveType, weapons.transform.position + Vector3.right * 30f);
            int coinsBefore = Object.FindObjectsByType<GoldPickup>(FindObjectsSortMode.None).Count(g => g.gameObject.activeInHierarchy);
            Assert.That(maxed.Take(), Is.False, "Nothing left to level.");
            Assert.That(Object.FindObjectsByType<GoldPickup>(FindObjectsSortMode.None).Count(g => g.gameObject.activeInHierarchy), Is.GreaterThan(coinsBefore),
                "It pays out a coin bag instead.");
            yield return null;
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    
        [UnityTest]
        public IEnumerator LevelEight_HasEightStainedGlassWindows_ThatBreakIntoABlastOf130()
        {
            yield return BootIntoGameplay();
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            LevelProgressionManager.Instance.StartStage(8);
            for (int i = 0; i < 3; i++) yield return null;
            var spawner = Object.FindFirstObjectByType<StageItemSpawner>();
            Assert.That(spawner.Windows.Count, Is.EqualTo(8), Errors());
            var window = spawner.Windows[0];
            var enemy = Object.FindObjectsByType<SoulHunter.Gameplay.AI.EnemyController>(FindObjectsSortMode.None).FirstOrDefault(e => e.isActiveAndEnabled && !e.IsStageBoss);
            Assert.That(enemy, Is.Not.Null, "an enemy to catch in the blast");
            enemy.transform.position = window.transform.position + Vector3.right * 1f;
            enemy.Freeze(10f);
            var enemyHealth = enemy.GetComponent<HealthController>();
            enemyHealth.Initialize(10000);
            UnityEngine.Physics.SyncTransforms();
            var glass = window.GetComponent<HealthController>();
            Assert.That(glass.MaxHealth, Is.EqualTo(StainedGlass.MaxHealth), "50 damage breaks it.");
            glass.TakeDamage(new DamagePacket(StainedGlass.MaxHealth, window.transform.position, Vector3.forward));
            yield return null;
            Assert.That(window == null || window.IsBroken, Is.True);
            float might = PlayerController.Instance.Stats.Might;
            Assert.That(10000 - enemyHealth.CurrentHealth, Is.EqualTo(Mathf.RoundToInt(StainedGlass.BaseDamage * might)), "130 base damage.");
            LevelProgressionManager.Instance.StartStage(7);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.That(spawner.Windows.Count, Is.EqualTo(0), "Level 7 has none.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
