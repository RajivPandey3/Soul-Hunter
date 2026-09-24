using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: WeaponTargetingTests guards the self-damage bug: TouchDamage defaults to the "Player"
    /// tag, and Song of Mana and Bloody Tear never changed it, so their beams/whips hurt the player.
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

        private string SongOfManaTarget(bool heldByPlayer)
        {
            var weaponObject = new GameObject("SongOfMana");
            weaponObject.transform.SetParent(Owner(heldByPlayer).transform);
            var song = weaponObject.AddComponent<SongOfManaWeapon>();
            var beam = new GameObject("BeamPrefab");
            _created.Add(beam);
            beam.AddComponent<BoxCollider>();
            beam.AddComponent<PooledLifetime>(); // avoids Destroy(obj, t), an error in EditMode
            song.VerticalBeamPrefab = beam;

            Attack(song);

            var spawned = weaponObject.GetComponentInChildren<TouchDamage>();
            Assert.That(spawned, Is.Not.Null, "Song of Mana beam spawn hona chahiye.");
            return spawned.TargetTag;
        }

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

        [Test]
        public void SongOfMana_HeldByPlayer_HitsEnemiesNotThePlayer()
        {
            Assert.That(SongOfManaTarget(heldByPlayer: true), Is.EqualTo("Enemy"));
        }

        [Test]
        public void SongOfMana_HeldByBoss_HitsThePlayer()
        {
            Assert.That(SongOfManaTarget(heldByPlayer: false), Is.EqualTo("Player"));
        }

        [Test]
        public void BloodyTear_HeldByPlayer_HitsEnemiesNotThePlayer()
        {
            Assert.That(BloodyTearTarget(heldByPlayer: true), Is.EqualTo("Enemy"));
        }
    }
}
