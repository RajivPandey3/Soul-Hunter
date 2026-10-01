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
    }
}
