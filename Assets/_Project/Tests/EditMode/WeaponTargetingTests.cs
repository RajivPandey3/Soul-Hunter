using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: WeaponTargetingTests guards weapon targeting: TouchDamage defaults to the "Player" tag,
    /// and Bloody Tear never changed it, so its whips hurt the player (Song of Mana, which had the same bug,
    /// now fires projectiles at the nearest enemy instead).
    /// Held by the player (or anything that is not a boss) they must hit "Enemy"; held by a boss
    /// (Shadow Kael's mirror) they must hit "Player".
    /// </summary>
    public class WeaponTargetingTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created) if (obj != null) Object.DestroyImmediate(obj);
            _created.Clear();
            foreach (var touch in Object.FindObjectsByType<TouchDamage>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(touch.gameObject);
        }

        private GameObject Owner(bool isPlayer)
        {
            var owner = new GameObject(isPlayer ? "Player" : "Boss");
            _created.Add(owner);
            if (isPlayer) owner.AddComponent<PlayerController>();
            else owner.AddComponent<SoulHunter.Gameplay.AI.EnemyController>(); // a boss, like Shadow Kael
            return owner;
        }

        private static void Attack(AutoAttackWeapon weapon) =>
            typeof(AutoAttackWeapon).GetMethod("Attack", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(weapon, null);

        private string BloodyTearTarget(bool heldByPlayer)
        {
            var weaponObject = new GameObject("BloodyTear");
            weaponObject.transform.SetParent(Owner(heldByPlayer).transform);
            var tear = weaponObject.AddComponent<BloodyTearWeapon>();
            var whip = new GameObject("WhipPrefab");
            _created.Add(whip);
            whip.AddComponent<PooledLifetime>(); // avoids Destroy(obj, t), an error in EditMode
            tear.WhipVisualPrefab = whip;

            Attack(tear);

            var spawned = weaponObject.GetComponentInChildren<TouchDamage>();
            Assert.That(spawned, Is.Not.Null, "Bloody Tear whip spawn hona chahiye.");
            return spawned.TargetTag;
        }

        private SongOfManaWeapon CreateSongOfMana(out GameObject owner)
        {
            owner = Owner(isPlayer: true);
            var weaponObject = new GameObject("SongOfMana");
            weaponObject.transform.SetParent(owner.transform);
            var song = weaponObject.AddComponent<SongOfManaWeapon>();
            var shot = new GameObject("ShotPrefab");
            _created.Add(shot);
            shot.AddComponent<SphereCollider>();
            shot.AddComponent<Rigidbody>();
            shot.AddComponent<Projectile>();
            song.VerticalBeamPrefab = shot;
            return song;
        }

        [Test]
        public void SongOfMana_FiresTowardTheNearestEnemy()
        {
            // Learning Comment: Owner decision: Song of Mana sabse nazdeek dushman ki taraf fire kare.
            var song = CreateSongOfMana(out var owner);
            var near = new GameObject("NearEnemy").AddComponent<SoulHunter.Gameplay.AI.EnemyController>();
            var far = new GameObject("FarEnemy").AddComponent<SoulHunter.Gameplay.AI.EnemyController>();
            _created.Add(near.gameObject); _created.Add(far.gameObject);
            near.transform.position = owner.transform.position + new Vector3(0f, 0f, 4f);
            far.transform.position = owner.transform.position + new Vector3(9f, 0f, 0f);
            SoulHunter.Gameplay.AI.EnemyController.ActiveEnemies.Add(near);
            SoulHunter.Gameplay.AI.EnemyController.ActiveEnemies.Add(far);
            try
            {
                var before = new HashSet<Projectile>(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None));
                Attack(song);

                Projectile fired = null;
                foreach (var p in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
                    if (!before.Contains(p)) { fired = p; _created.Add(p.gameObject); }
                Assert.That(fired, Is.Not.Null, "Song of Mana ko ek shot chalana chahiye.");
                Assert.That(Vector3.Angle(fired.transform.forward, Vector3.forward), Is.LessThan(1f),
                    "Shot nazdeek wale dushman (+Z) ki taraf jaye, door wale (+X) ki taraf nahi.");
            }
            finally
            {
                SoulHunter.Gameplay.AI.EnemyController.ActiveEnemies.Remove(near);
                SoulHunter.Gameplay.AI.EnemyController.ActiveEnemies.Remove(far);
            }
        }

        [Test]
        public void SongOfMana_WithNoEnemies_HoldsFire()
        {
            var song = CreateSongOfMana(out _);
            int before = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length;
            var saved = new List<SoulHunter.Gameplay.AI.EnemyController>(SoulHunter.Gameplay.AI.EnemyController.ActiveEnemies);
            SoulHunter.Gameplay.AI.EnemyController.ActiveEnemies.Clear();
            try
            {
                Attack(song);
                Assert.That(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length, Is.EqualTo(before));
            }
            finally
            {
                SoulHunter.Gameplay.AI.EnemyController.ActiveEnemies.AddRange(saved);
            }
        }

        [Test]
        public void BloodyTear_HeldByPlayer_HitsEnemiesNotThePlayer()
        {
            Assert.That(BloodyTearTarget(heldByPlayer: true), Is.EqualTo("Enemy"));
        }
    }
}
