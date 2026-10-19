using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS DOT Phase 1 row 8 (2026-10-01). vampire.survivors.wiki/w/Pentagram: the glyph erases "every enemy, their drops,
    /// and all items on the screen, except for stage items"; a keep chance (x Luck) makes it spare the items; "a
    /// hard-coded limit of 15 seconds" on how often it activates; the glyph lasts 1 s.
    /// </summary>
    public class PentagramVsPlayModeTests : GameplayPlayModeTestBase
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator ThePlayersPentagram_ErasesGemsOnScreen_AndNeverFiresFasterThan15s()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.enabled = false;
            WeaponManager.Instance.ApplyUpgrade(WeaponDefinitionLookup.Find(UpgradeData.UpgradeType.Pentagram).GetLevel(1));
            yield return null;
            var pentagram = player.GetComponentsInChildren<PentagramWeapon>().First();
            pentagram.enabled = false;
            typeof(PentagramWeapon).GetField("_chanceToKeepGems", Private).SetValue(pentagram, -1f); // always erase items

            // Gems on screen, out of pickup range.
            Vector3 at = player.transform.position + new Vector3(6f, 0f, 3f);
            for (int i = 0; i < 5; i++) PickupPoolManager.Instance.SpawnGem(at + Vector3.right * i * 0.6f, 10);
            yield return null;
            int onScreen = XPGem.ActiveGems.Count(g => g.isActiveAndEnabled);
            Assert.That(onScreen, Is.GreaterThanOrEqualTo(5));

            pentagram.enabled = true;
            pentagram.ResetCooldownAndFire();
            yield return null;
            yield return null;
            Assert.That(pentagram.LastActivationErasedItems, Is.True);
            Assert.That(pentagram.LastItemsErased, Is.GreaterThanOrEqualTo(5), "The gems on screen were erased.");
            Assert.That(XPGem.ActiveGems.Count(g => g.isActiveAndEnabled && Vector3.Distance(g.transform.position, at) < 4f), Is.EqualTo(0));

            // 15 s floor: even with a huge Cooldown bonus the next activation is 15 s away.
            player.Stats.ReduceCooldown(5f); // clamps the Cooldown stat at its 10% floor
            pentagram.ResetCooldownAndFire();
            yield return null;
            float timer = (float)typeof(AutoAttackWeapon).GetField("_timer", Private).GetValue(pentagram);
            Assert.That(timer, Is.GreaterThanOrEqualTo(PentagramWeapon.VsMinimumCooldown - 0.1f), "Never faster than 15 s.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator TheItemWipe_ErasesExactlyTheItemsOnScreen_ButNotStageItems()
        {
            // Bug MIS ITM-11: the wipe is now linear (a set, ChestPickup.Active, components read only on screen). The same
            // items must go as with the old scan: every non-stage item on screen, nothing off it.
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.enabled = false;
            var pool = PickupPoolManager.Instance;
            Vector3 near = player.transform.position + new Vector3(6f, 0f, 2f);
            Vector3 far = player.transform.position + new Vector3(90f, 0f, 90f);
            for (int i = 0; i < 60; i++) pool.SpawnGem(near + new Vector3(i % 10, 0f, i / 10) * 0.5f, 10);
            for (int i = 0; i < 10; i++) pool.SpawnGem(far + Vector3.right * i, 10);
            var chestOn = pool.SpawnChest(player.transform.position + new Vector3(-7f, 0f, 2f));
            var chestOff = pool.SpawnChest(far + Vector3.forward * 5f);
            var stageChicken = pool.SpawnChicken(player.transform.position + new Vector3(-6f, 0f, -2f));
            stageChicken.AddComponent<StageItemMark>();
            var coin = pool.SpawnGold(player.transform.position + new Vector3(7f, 0f, -2f), 1);
            yield return null;

            // The old scan, written out: every active, unmarked item on screen, each once.
            var view = Camera.main;
            var expected = new System.Collections.Generic.HashSet<GameObject>();
            var all = new System.Collections.Generic.HashSet<GameObject>();
            void Old(GameObject go)
            {
                if (!go.activeInHierarchy) return;
                all.Add(go);
                if (go.GetComponent<StageItemMark>() != null || go.GetComponent<StageItemPickup>() != null) return;
                if (GroundItemEraser.OnScreen(view, go.transform.position)) expected.Add(go);
            }
            foreach (var gem in XPGem.ActiveGems) if (gem != null) Old(gem.gameObject);
            var ticked = new System.Collections.Generic.List<ITickedPickup>();
            PickupTicker.CopyTo(ticked);
            foreach (var pickup in ticked) if (pickup is Component c && c != null) Old(c.gameObject);
            foreach (var chest in Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None)) Old(chest.gameObject);
            Assert.That(expected, Does.Contain(chestOn).And.Contain(coin.gameObject).And.Not.Contain(chestOff).And.Not.Contain(stageChicken));

            int erased = GroundItemEraser.EraseOnScreen(view);
            Assert.That(erased, Is.EqualTo(expected.Count), "As many items go as the old scan found.");
            foreach (var go in all)
                Assert.That(go.activeInHierarchy, Is.EqualTo(!expected.Contains(go)), $"{go.name} {(expected.Contains(go) ? "is erased" : "stays")}.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
