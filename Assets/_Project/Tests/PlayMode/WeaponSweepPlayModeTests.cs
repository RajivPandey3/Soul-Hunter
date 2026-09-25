using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.VFX;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Every weapon in the real game (owner request 2026-09-25: proper projectiles and VFX). Almost every weapon used to
    /// fire or place the same orange sphere (Bullet.prefab); area weapons flew away with it and physics weapons were
    /// held by its Projectile script. Each weapon must now show its own visual, never the Bullet, and hurt (or, for
    /// Clock Lancet, freeze) enemies around the player. Laurel is a shield and only has to appear.
    /// </summary>
    public class WeaponSweepPlayModeTests : GameplayPlayModeTestBase
    {
        private static IEnumerable<UpgradeType> Weapons() => new[]
        {
            UpgradeType.MagicWand, UpgradeType.Whip, UpgradeType.Garlic, UpgradeType.Axe, UpgradeType.Bible, UpgradeType.Cross,
            UpgradeType.LightningRing, UpgradeType.FireWand, UpgradeType.SantaWater, UpgradeType.Knife, UpgradeType.Runetracer,
            UpgradeType.Pentagram, UpgradeType.ClockLancet, UpgradeType.Laurel, UpgradeType.Bone, UpgradeType.Peachone,
            UpgradeType.SongOfMana, UpgradeType.CherryBomb, UpgradeType.Gun,
        };

        [UnityTest]
        public IEnumerator Weapon_ShowsItsOwnVisual_AndAffectsEnemies([ValueSource(nameof(Weapons))] UpgradeType type)
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) Object.Destroy(stray.gameObject);
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            var weapons = WeaponManager.Instance;
            var weaponPrefab = weapons.GetWeaponObject(type);
            Assert.IsNotNull(weaponPrefab, $"{type} has no weapon prefab.");
            // Only the weapon under test fights: the character's starting weapon is switched off.
            foreach (var existing in player.GetComponentsInChildren<AutoAttackWeapon>())
                if (!existing.gameObject.name.StartsWith(weaponPrefab.name)) existing.gameObject.SetActive(false);
            yield return null;

            weapons.GiveWeapon(type, 1);
            yield return null;
            var weapon = player.GetComponentsInChildren<AutoAttackWeapon>().FirstOrDefault(w => w.gameObject.name.StartsWith(weaponPrefab.name));
            Assert.IsNotNull(weapon, $"{type} was not equipped on the player. " + Errors());

            // A ring of harmless enemies around the player.
            var prefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            var enemies = new List<HealthController>();
            var start = new Dictionary<HealthController, int>();
            for (int i = 0; i < 8; i++)
            {
                var offset = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * 3f;
                var enemy = Object.Instantiate(prefab, player.transform.position + offset, Quaternion.identity);
                var touch = enemy.GetComponent<TouchDamage>();
                if (touch != null) touch.enabled = false;
                var health = enemy.GetComponent<HealthController>();
                enemies.Add(health);
                start[health] = health.CurrentHealth;
            }

            bool affected = false, sawBullet = false;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!affected && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                sawBullet |= Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                    .Any(t => t.gameObject.activeInHierarchy && (t.name.StartsWith("Bullet") || t.name.StartsWith("Axe_Model") || t.name.StartsWith("Cross_Model")));
                affected = type == UpgradeType.Laurel ||
                           (type == UpgradeType.ClockLancet
                               ? enemies.Any(h => h != null && h.GetComponent<EnemyController>().IsFrozen)
                               : enemies.Any(h => h == null || !h.gameObject.activeInHierarchy || h.CurrentHealth < start[h]));
            }
            Assert.IsFalse(sawBullet, $"{type} still spawns the placeholder Bullet/Axe_Model/Cross_Model. " + Errors());
            Assert.IsTrue(affected, $"{type} never {(type == UpgradeType.ClockLancet ? "froze" : "hurt")} an enemy 3 units away within 10 s. " + Errors());
            foreach (var h in enemies) if (h != null) Object.Destroy(h.gameObject);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Kills_Pickups_AndLevelUps_PlayTheirEffects()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            var player = PlayerController.Instance;
            var prefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            var enemy = Object.Instantiate(prefab, player.transform.position + Vector3.forward * 6f, Quaternion.identity);
            var health = enemy.GetComponent<HealthController>();
            health.TakeDamage(new DamagePacket(1, enemy.transform.position, Vector3.zero, DamageType.Normal));
            yield return null;
            var vfx = VFXPoolManager.Instance;
            Assert.IsNotNull(vfx, "VFXPoolManager was not created on first use. " + Errors());
            Assert.That(vfx.PooledCount(VfxKind.HitSpark), Is.GreaterThan(0), "Hitting an enemy shows a hit spark.");
            health.TakeDamage(new DamagePacket(100000, enemy.transform.position, Vector3.zero, DamageType.Normal));
            yield return null;
            Assert.That(vfx.PooledCount(VfxKind.EnemyDeath), Is.GreaterThan(0), "A killed enemy shows a death effect.");

            player.GetComponent<PlayerExperience>().AddXP(100000);
            yield return null;
            Assert.That(vfx.PooledCount(VfxKind.LevelUp), Is.GreaterThan(0), "Levelling up shows its effect.");
            var effects = vfx.GetComponentsInChildren<ParticleSystem>(true);
            Assert.That(effects, Is.Not.Empty);
            Assert.That(effects.All(p => p.GetComponent<ParticleSystemRenderer>().sharedMaterial != null), "Every effect has its material.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
