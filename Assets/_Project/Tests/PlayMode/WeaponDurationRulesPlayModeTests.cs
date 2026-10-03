using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS DOT Phase 1 row 8 (2026-10-01). wiki Peachone: an activation bombards for the Duration (4 s), about one set per
    /// second, and the next comes after the Duration plus the 1 s Cooldown. wiki Cherry_Bomb: a bomb explodes only by
    /// chance (40% at level 1, x Luck).
    /// </summary>
    public class WeaponDurationRulesPlayModeTests : GameplayPlayModeTestBase
    {
        private static readonly FieldInfo Timer = typeof(AutoAttackWeapon).GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic);

        private IEnumerator BootWith(UpgradeData.UpgradeType type)
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>()) weapon.enabled = false;
            // The waits below run on game time. Level 1 opens with 1-HP bats (VS Mad Forest): a bomb that kills them levels
            // the hero, and the paused level-up screen stalled a full run (2026-10-04). No enemies and no gems, so no level-up.
            foreach (var enemy in EnemyController.ActiveEnemies.ToList()) if (enemy != null) enemy.gameObject.SetActive(false);
            foreach (var gem in Object.FindObjectsByType<SoulHunter.Gameplay.Pickups.XPGem>(FindObjectsSortMode.None)) gem.gameObject.SetActive(false);
            yield return CloseOpenLevelUps();
            WeaponManager.Instance.ApplyUpgrade(WeaponDefinitionLookup.Find(type).GetLevel(1));
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Peachone_BombardsFor4Sets_ThenRestsUntilTheNextActivation()
        {
            yield return BootWith(UpgradeData.UpgradeType.Peachone);
            var peachone = PlayerController.Instance.GetComponentsInChildren<PeachoneWeapon>().First();
            float timer = (float)Timer.GetValue(peachone);
            Assert.That(timer, Is.InRange(4.5f, 5.05f), "The next activation is Duration 4 s + Cooldown 1 s away.");
            yield return new WaitForSeconds(4.3f);
            Assert.That(peachone.LastActivationSets, Is.EqualTo(4), "Four sets in the 4 s Duration.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator CherryBombs_ExplodeOnlyByChance()
        {
            Assert.That(CherryBombWeapon.Explodes(0.4f, 1f, 0.39f), Is.True);
            Assert.That(CherryBombWeapon.Explodes(0.4f, 1f, 0.41f), Is.False);
            Assert.That(CherryBombWeapon.Explodes(0.4f, 1.5f, 0.59f), Is.True, "Luck multiplies the chance.");

            yield return BootWith(UpgradeData.UpgradeType.CherryBomb);
            var bomb = PlayerController.Instance.GetComponentsInChildren<CherryBombWeapon>().First();
            Assert.That(bomb.ExplodeChance, Is.EqualTo(0.4f), "40% at level 1.");
            bomb.enabled = true;
            for (int i = 0; i < 30; i++)
            {
                bomb.ResetCooldownAndFire();
                yield return null;
            }
            yield return new WaitForSeconds(2.6f); // every bomb's 2 s flight is over
            int thrown = bomb.Explosions + bomb.Fizzles;
            Assert.That(thrown, Is.GreaterThanOrEqualTo(25), "The bombs landed.");
            Assert.That(bomb.Explosions, Is.InRange(2, thrown - 2), $"Some explode, some do not ({bomb.Explosions} of {thrown}).");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
