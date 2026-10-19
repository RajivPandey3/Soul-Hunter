using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Weapons;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Full bug MIS, S3 weapon rows (2026-10-19). Each test fails on the code before its fix.
    /// </summary>
    public class WeaponS3FixesPlayModeTests : GameplayPlayModeTestBase
    {
        // ------------------------------------------------------------------ CMB-06 Axe lifetime

        [UnityTest]
        public IEnumerator AnAxeInFlight_ReturnsToThePool_WhenTheAxeWeaponIsSwitchedOff()
        {
            yield return Prepare();
            var axeWeapon = Give<AxeWeapon>(UpgradeType.Axe);
            yield return null;
            var thrown = LiveAxes();
            Assert.That(thrown.Count, Is.EqualTo(1), "The Axe throws one axe when gained.");
            // As when the Axe evolves into Death Spiral: the weapon's object is switched off with an axe still flying.
            axeWeapon.gameObject.SetActive(false);
            yield return new WaitForSeconds(AxeWeapon.AxeLifetimeSeconds + 0.5f);
            Assert.IsFalse(thrown[0].gameObject.activeSelf, "The axe went back to the pool after its 5 s, weapon off or not.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator ARecycledAxe_KeepsItsFullLife_WhateverItsEarlierThrowLeftRunning()
        {
            yield return Prepare();
            var axeWeapon = Give<AxeWeapon>(UpgradeType.Axe);
            yield return null;
            axeWeapon.enabled = false; // no throws of its own; the test throws
            var first = LiveAxes().Single();
            yield return new WaitForSeconds(0.4f);
            first.gameObject.SetActive(false); // used up by its pierce
            yield return new WaitForSeconds(3.6f);

            axeWeapon.enabled = true;
            axeWeapon.ResetCooldownAndFire();
            yield return null;
            axeWeapon.enabled = false;
            var second = LiveAxes().Single();
            Assert.AreSame(first, second, "The pool hands back the same axe.");
            float thrownAt = Time.time;

            yield return new WaitForSeconds(1.5f); // 5.5 s after the first throw, 1.5 s into this one
            Assert.IsTrue(second.gameObject.activeSelf, "The first throw's 5 s does not switch the re-thrown axe off.");
            while (Time.time < thrownAt + AxeWeapon.AxeLifetimeSeconds + 0.3f) yield return null;
            Assert.IsFalse(second.gameObject.activeSelf, "The re-thrown axe goes back after its own 5 s.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static List<AxePierce> LiveAxes() =>
            Object.FindObjectsByType<AxePierce>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();

        // ------------------------------------------------------------------ helpers

        /// <summary>Boots a quiet run: no spawner, an invincible hero, no weapons, no enemies and no gems (no level-ups).</summary>
        private IEnumerator Prepare()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var existing in player.GetComponentsInChildren<AutoAttackWeapon>()) existing.gameObject.SetActive(false);
            foreach (var other in EnemyController.ActiveEnemies.ToList()) if (other != null) other.gameObject.SetActive(false);
            foreach (var gem in Object.FindObjectsByType<XPGem>(FindObjectsSortMode.None)) gem.gameObject.SetActive(false);
            yield return CloseOpenLevelUps();
        }

        private static T Give<T>(UpgradeType type) where T : AutoAttackWeapon
        {
            WeaponManager.Instance.ApplyUpgrade(WeaponDefinitionLookup.Find(type).GetLevel(1));
            var weapon = PlayerController.Instance.GetComponentsInChildren<T>().FirstOrDefault();
            Assert.IsNotNull(weapon, $"{typeof(T).Name} is equipped.");
            return weapon;
        }

        /// <summary>Unkillable, still targets of this stage's enemy type around the hero, within <paramref name="radius"/>.</summary>
        private static List<EnemyController> SpawnTargets(int count, float radius, int health = 1000000)
        {
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var enemyPrefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            var centre = PlayerController.Instance.transform.position;
            var list = new List<EnemyController>();
            for (int i = 0; i < count; i++)
            {
                float angle = i * 137.5f; // golden angle: an even disc
                float distance = radius * Mathf.Sqrt((i + 0.5f) / count);
                var enemy = Object.Instantiate(enemyPrefab, centre + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance, Quaternion.identity);
                enemy.GetComponent<HealthController>().Initialize(health);
                var controller = enemy.GetComponent<EnemyController>();
                controller.IsStageBoss = true; // immune to instant kills, so nothing erases them
                var touch = enemy.GetComponent<TouchDamage>();
                if (touch != null) touch.enabled = false;
                var body = enemy.GetComponent<Rigidbody>();
                if (body != null) body.isKinematic = true;
                list.Add(controller);
            }
            return list;
        }
    }
}
