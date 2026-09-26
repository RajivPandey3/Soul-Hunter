using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Part 5, VS knockback in the real game: a hit enemy backs off for about 120 ms and comes on again; Sea of Lost
    /// Souls ghosts are not pushed; Garlic makes enemies take more knockback; the player is never knocked back.
    /// </summary>
    public class KnockbackPlayModeTests : GameplayPlayModeTestBase
    {
        private EnemySpawner _spawner;

        private IEnumerator QuietArena(int stage)
        {
            yield return BootIntoGameplay();
            _spawner = Object.FindFirstObjectByType<EnemySpawner>();
            _spawner.enabled = false;
            if (stage != 1) LevelProgressionManager.Instance.StartStage(stage);
            yield return null;
            yield return null;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
        }

        private EnemyController Walker(float distance)
        {
            var prefab = GetPrivateField<List<GameObject>>(_spawner, "_stageEnemyPrefabs")[LevelProgressionManager.Instance.CurrentStage - 1];
            var player = PlayerController.Instance;
            Vector3 at = player.transform.position + Vector3.right * distance;
            var enemy = Object.Instantiate(prefab, at, Quaternion.identity).GetComponent<EnemyController>();
            enemy.Target = player.transform;
            var touch = enemy.GetComponent<TouchDamage>();
            if (touch != null) touch.DamageAmount = 0;
            return enemy;
        }

        private static float Gap(EnemyController enemy)
        {
            Vector3 d = enemy.transform.position - PlayerController.Instance.transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        private static IEnumerator Seconds(float s)
        {
            float until = Time.time + s;
            while (Time.time < until) yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator AHitEnemy_BacksOffBriefly_ThenComesOnAgain()
        {
            yield return QuietArena(1);
            var enemy = Walker(8f);
            yield return Seconds(0.5f); // walking in
            float atHit = Gap(enemy);
            enemy.GetComponent<HealthController>().TakeDamage(new DamagePacket(1, enemy.transform.position, Vector3.zero));
            Assert.That(enemy.IsKnockedBack, Is.True);
            yield return Seconds(0.1f);
            float pushed = Gap(enemy);
            Assert.That(pushed, Is.GreaterThan(atHit + 0.15f), $"Moved away from the player ({atHit:0.00} -> {pushed:0.00}).");
            yield return Seconds(0.6f);
            Assert.That(enemy.IsKnockedBack, Is.False, "The knockback lasts about 120 ms.");
            Assert.That(Gap(enemy), Is.LessThan(pushed), "Then it comes on again.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator SeaOfLostSoulsGhosts_AreNotPushed()
        {
            yield return QuietArena(7);
            var ghost = Walker(8f);
            Assert.That(ghost.KnockbackTaken, Is.EqualTo(0f), "VS ghosts take no knockback.");
            yield return Seconds(0.5f);
            float atHit = Gap(ghost);
            ghost.GetComponent<HealthController>().TakeDamage(new DamagePacket(1, ghost.transform.position, Vector3.zero, DamageType.Magic));
            yield return Seconds(0.1f);
            Assert.That(Gap(ghost), Is.LessThanOrEqualTo(atHit + 0.01f), "The ghost keeps coming.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Garlic_MakesEnemiesTakeMoreKnockback()
        {
            yield return QuietArena(1);
            var enemy = Walker(1.2f);
            enemy.GetComponent<HealthController>().Initialize(100000);
            WeaponManager.Instance.GiveWeapon(UpgradeType.Garlic, 1);
            float deadline = Time.realtimeSinceStartup + 6f;
            while (enemy.KnockbackTaken <= 1f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(enemy.KnockbackTaken, Is.GreaterThan(1f), "Garlic raised the knockback it takes.");
            Assert.That(enemy.KnockbackTaken, Is.LessThanOrEqualTo(enemy.MaxKnockback));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator ThePlayer_IsNeverKnockedBack_WhenTouched()
        {
            yield return QuietArena(1);
            var player = PlayerController.Instance;
            var health = player.GetComponent<HealthController>();
            health.IsInvincible = false;
            health.Initialize(100000);
            Vector3 start = player.transform.position;
            var enemy = Walker(0.3f);
            var touch = enemy.GetComponent<TouchDamage>();
            if (touch != null) touch.DamageAmount = 5;
            int before = health.CurrentHealth;
            yield return Seconds(1.5f);
            Assert.That(health.CurrentHealth, Is.LessThan(before), "The enemy's touch hurt the player.");
            Vector3 moved = player.transform.position - start;
            moved.y = 0f;
            Assert.That(moved.magnitude, Is.LessThan(0.05f), "The player did not slide.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
