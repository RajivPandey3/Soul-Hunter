using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Level 1 plan step 3 (2026-10-01): each new evolution's VS rule in the real game (wiki pages Hellfire, La_Borra,
    /// NO_FUTURE, Thunder_Loop, Gorgeous_Moon, Mannajja).
    /// </summary>
    public class NewEvolutionRulesPlayModeTests : GameplayPlayModeTestBase
    {
        private PlayerController _player;
        private GameObject _enemyPrefab;

        private IEnumerator Boot()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList()) if (stray != null) stray.gameObject.SetActive(false);
            _player = PlayerController.Instance;
            _player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in _player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            _enemyPrefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            yield return null;
        }

        /// <summary>
        /// Waits <paramref name="seconds"/> of game time. Kills drop gems, and a level-up pauses the game (WaitForSeconds then
        /// never ends: the first run hung for 30 minutes), so any level-up screen is answered.
        /// </summary>
        private static IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            float realDeadline = Time.realtimeSinceStartup + seconds * 3f + 10f;
            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            while (Time.time < end && Time.realtimeSinceStartup < realDeadline)
            {
                if (levelUp != null && levelUp.IsLevelUpActive)
                {
                    var choices = levelUp.GetLevelUpChoices(1);
                    if (choices.Count > 0) levelUp.SelectUpgrade(choices[0]);
                }
                yield return null;
            }
        }

        private T Equip<T>(string evolvedName) where T : AutoAttackWeapon
        {
            var evolution = GameContentCatalog.Load().Evolutions.First(e => e.EvolvedName == evolvedName);
            return Object.Instantiate(evolution.EvolvedWeaponPrefab, _player.transform).GetComponent<T>();
        }

        private List<HealthController> Enemies(int count, float distance, int health = 100000)
        {
            var list = new List<HealthController>();
            for (int i = 0; i < count; i++)
            {
                var enemy = Object.Instantiate(_enemyPrefab, _player.transform.position + Quaternion.Euler(0f, i * 360f / count, 0f) * Vector3.forward * distance, Quaternion.identity);
                var touch = enemy.GetComponent<TouchDamage>();
                if (touch != null) touch.enabled = false;
                var h = enemy.GetComponent<HealthController>();
                h.Initialize(health);
                list.Add(h);
            }
            return list;
        }

        [UnityTest]
        public IEnumerator StormboundHalo_StrikesEachSpotTwice()
        {
            yield return Boot();
            var targets = Enemies(3, 4f);
            var halo = Equip<StormboundHaloWeapon>("Evo_StormboundHalo");
            yield return Wait(0.9f);
            Assert.That(halo.Strikes, Is.EqualTo(6), "Three targets, each struck twice (VS: again in the same spot).");
            Assert.That(targets.All(t => t.CurrentHealth < t.MaxHealth), Is.True);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator SeveredFate_RetaliatesWhenTheHolderIsHit_PlusTenPercentPerArmor()
        {
            yield return Boot();
            var near = Enemies(4, 1.5f);
            var fate = Equip<SeveredFateWeapon>("Evo_SeveredFate");
            yield return null;
            float plain = fate.RetaliationDamage;
            _player.Stats.AddArmor(5);
            Assert.That(fate.RetaliationDamage, Is.EqualTo(plain * 1.5f).Within(0.01f), "+10% per Armor point (5 -> +50%).");
            var health = _player.GetComponent<HealthController>();
            health.IsInvincible = false;
            health.TakeDamage(new DamagePacket(1, _player.transform.position, Vector3.zero) { IgnoresHitInvulnerability = true });
            yield return null;
            Assert.That(fate.Retaliations, Is.EqualTo(1), "A hit on the holder sets off the explosion.");
            Assert.That(near.All(e => e.CurrentHealth < e.MaxHealth), Is.True, "Enemies around the holder are hurt.");
            health.TakeDamage(new DamagePacket(1, _player.transform.position, Vector3.zero) { IgnoresHitInvulnerability = true });
            Assert.That(fate.Retaliations, Is.EqualTo(1), "At most every 0.5 s.");
            health.IsInvincible = true;
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator RequiemOfSouls_SlowsEachEnemy_DownTo20Percent()
        {
            yield return Boot();
            var enemies = Enemies(2, 3f);
            var requiem = Equip<RequiemOfSoulsWeapon>("Evo_RequiemOfSouls");
            yield return Wait(1.2f);
            var controller = enemies[0].GetComponent<EnemyController>();
            Assert.That(controller.LingeringSlow, Is.LessThan(1f), "Hit and slowed.");
            Assert.That(controller.LingeringSlow, Is.GreaterThanOrEqualTo(0.85f), "At most one hit per second (1 s hitbox delay).");
            for (int i = 0; i < 30; i++) controller.ApplyLingeringSlow(RequiemOfSoulsWeapon.SlowStep, RequiemOfSoulsWeapon.SlowFloor);
            Assert.That(controller.LingeringSlow, Is.EqualTo(0.2f).Within(1e-4f), "Never below 20%.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator DrowningChalice_PoolsCreepTowardThePlayer()
        {
            yield return Boot();
            var chalice = Equip<DrowningChaliceWeapon>("Evo_DrowningChalice");
            yield return Wait(0.5f);
            var pools = Object.FindObjectsByType<PoolCreep>(FindObjectsSortMode.None).Where(p => p.isActiveAndEnabled).ToList();
            Assert.That(pools.Count, Is.GreaterThanOrEqualTo(1), "Pools fell around the player.");
            float before = Vector3.Distance(pools[0].transform.position, _player.transform.position);
            yield return Wait(1f);
            float after = Vector3.Distance(pools[0].transform.position, _player.transform.position);
            Assert.That(after, Is.LessThan(before - 0.3f), "The pool crept toward the player.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator EclipseSigil_Beams_ThenErasesTheScreen_ThenVacuumsTheGems()
        {
            yield return Boot();
            var enemies = Enemies(30, 6f, 50);
            int gemsBefore = XPGem.ActiveGems.Count;
            var sigil = Equip<EclipseSigilWeapon>("Evo_EclipseSigil");
            yield return Wait(EclipseSigilWeapon.ErasureAt + 0.6f);
            Assert.That(sigil.BeamsFired, Is.EqualTo(20), "20 beams.");
            Assert.That(sigil.Erasures, Is.EqualTo(1), "Then the erasure.");
            Assert.That(sigil.Vacuums, Is.EqualTo(1), "Then the vacuum.");
            Assert.That(enemies.All(e => e == null || !e.gameObject.activeInHierarchy || e.CurrentHealth <= 0), Is.True, "Every enemy on screen is gone.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
