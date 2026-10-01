using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Weapons;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS DOT Phase 1 row 5 (2026-10-01). vampire.survivors.wiki/w/Weapons: "Upgrading a weapon will also reset its
    /// Cooldown and activate it instantly."
    /// </summary>
    public class WeaponUpgradeFirePlayModeTests : GameplayPlayModeTestBase
    {
        private static readonly FieldInfo Timer = typeof(AutoAttackWeapon).GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic);

        [UnityTest]
        public IEnumerator UpgradingAWeapon_ResetsItsCooldown_AndFiresAtOnce()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            var whip = player.GetComponentsInChildren<AutoAttackWeapon>().First(w => w is WhipWeapon);
            Assert.That(whip.CurrentLevel, Is.EqualTo(1));

            // Let it fire, then wait part of its 1.35 s cooldown.
            yield return WaitUntilRealtime(() => (float)Timer.GetValue(whip) > 1.0f, "the whip to fire");
            yield return new WaitForSeconds(0.5f);
            float before = (float)Timer.GetValue(whip);
            Assert.That(before, Is.GreaterThan(0.2f), "Mid-cooldown before the upgrade.");

            var level2 = WeaponDefinitionLookup.Find(UpgradeData.UpgradeType.Whip).GetLevel(2);
            Assert.That(level2, Is.Not.Null, "The Whip's level 2 upgrade.");
            WeaponManager.Instance.ApplyUpgrade(level2);
            Assert.That(whip.CurrentLevel, Is.EqualTo(2));
            Assert.That((float)Timer.GetValue(whip), Is.LessThanOrEqualTo(0f), "The cooldown is reset.");
            yield return null;
            Assert.That((float)Timer.GetValue(whip), Is.GreaterThan(before + 0.3f), "It fired at once (a new full cooldown started).");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
