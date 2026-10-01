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
            yield return WaitUntilRealtime(() => _items.Placed.Count == 6, "the stage's six items");
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
            Assert.That(_items.Placed.Count(i => i.GetComponent<StageItemPickup>() != null), Is.EqualTo(4), "A passive, a weapon and the two Metaglios.");
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
            Assert.That(_items.Placed.Count, Is.EqualTo(6), "Stage 2 has its own six items.");
            var stage2Passive = StageItems.Plan(2).First(e => e.Kind == Kind.Passive && !UpgradeRarity.IsStageItemOnly(e.Upgrade)).Upgrade;
            Assert.That(_items.Placed.Any(i => i.GetComponent<StageItemPickup>()?.Upgrade == stage2Passive), Is.True);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator WithNoFreeSlot_AWeaponItemBecomesGold()
        {
            yield return Boot();
            var weapons = WeaponManager.Instance;
            var weaponItem = _items.Placed.Select(i => i.GetComponent<StageItemPickup>()).First(p => p != null && StageItems.Plan(1).Any(e => e.Kind == Kind.Weapon && e.Upgrade == p.Upgrade));
            // Fill the six weapon slots with other weapons.
            foreach (var type in new[] { UpgradeData.UpgradeType.MagicWand, UpgradeData.UpgradeType.Garlic, UpgradeData.UpgradeType.Bible,
                                         UpgradeData.UpgradeType.Pentagram, UpgradeData.UpgradeType.Laurel, UpgradeData.UpgradeType.Gun })
                if (weapons.GetActiveWeaponsCount() < 6 && type != weaponItem.Upgrade) weapons.GiveWeapon(type, 1);
            Assert.That(weapons.GetActiveWeaponsCount(), Is.EqualTo(6));
            int coinsBefore = Object.FindObjectsByType<GoldPickup>(FindObjectsSortMode.None).Count(g => g.gameObject.activeInHierarchy);
            Assert.That(weaponItem.Take(), Is.False, "No slot: not an upgrade.");
            Assert.That(weapons.GetWeaponLevel(weaponItem.Upgrade), Is.EqualTo(0));
            Assert.That(Object.FindObjectsByType<GoldPickup>(FindObjectsSortMode.None).Count(g => g.gameObject.activeInHierarchy), Is.GreaterThan(coinsBefore),
                "It pays out a coin bag instead.");
            yield return null;
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
