using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: WeaponAimingTests verifies the owner decision that the player's attacks fire toward the
    /// nearest enemy. One enemy sits straight ahead (+Z), nothing on the old facing axis (X); each weapon's shots,
    /// zones or strikes must go toward +Z. Also guards Gun, Cherry Bomb and Death Spiral, whose Vector2
    /// directions used to fire vertically (x, y, 0) instead of across the ground.
    /// </summary>
    public class WeaponAimingTests
    {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly List<Object> _created = new List<Object>();
        private GameObject _owner;
        private EnemyController _enemy;

        [SetUp]
        public void SetUp()
        {
            _owner = Track(new GameObject("Player"));
            var controller = _owner.AddComponent<PlayerController>();
            // Knife-style weapons read the player's velocity; PlayerController.Awake does not run in EditMode.
            typeof(PlayerController).GetField("<Rigidbody>k__BackingField", AnyInstance)
                .SetValue(controller, _owner.GetComponent<Rigidbody>());

            _enemy = Track(new GameObject("EnemyAhead")).AddComponent<EnemyController>();
            _enemy.transform.position = new Vector3(0f, 0f, 5f);
            EnemyController.ActiveEnemies.Add(_enemy);
        }

        [TearDown]
        public void TearDown()
        {
            EnemyController.ActiveEnemies.Remove(_enemy);
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t != null && t.name.EndsWith("(Clone)")) Object.DestroyImmediate(t.gameObject);
            foreach (var obj in _created) if (obj != null) Object.DestroyImmediate(obj);
            _created.Clear();
        }

        private T Track<T>(T obj) where T : Object { _created.Add(obj); return obj; }

        /// <summary>A stand-in prefab with a collider, rigidbody, projectile and pooled lifetime.</summary>
        private GameObject StandInPrefab(string name)
        {
            var prefab = Track(new GameObject(name));
            prefab.AddComponent<SphereCollider>();
            prefab.AddComponent<Rigidbody>();
            prefab.AddComponent<Projectile>();
            prefab.AddComponent<PooledLifetime>(); // avoids Destroy(obj, t), an error in EditMode
            return prefab;
        }

        private List<GameObject> FireAndCollect<T>(string prefabField) where T : AutoAttackWeapon
        {
            var weaponObject = new GameObject(typeof(T).Name);
            weaponObject.transform.SetParent(_owner.transform);
            var weapon = weaponObject.AddComponent<T>();
            var prefab = StandInPrefab(prefabField);
            SetMember(weapon, prefabField, prefab);

            typeof(T).GetMethod("Awake", AnyInstance)?.Invoke(weapon, null);
            typeof(AutoAttackWeapon).GetMethod("Attack", AnyInstance).Invoke(weapon, null);

            return Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.name == prefab.name + "(Clone)").Select(t => t.gameObject).ToList();
        }

        private static void SetMember(object target, string name, object value)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, AnyInstance | BindingFlags.DeclaredOnly);
                if (field != null) { field.SetValue(target, value); return; }
            }
            Assert.Fail($"{target.GetType().Name} has no field '{name}'.");
        }

        private static float AngleFromAhead(Vector3 direction)
        {
            direction.y = 0f;
            Assert.That(direction.sqrMagnitude, Is.GreaterThan(0.0001f), "Direction must lie on the ground plane.");
            return Vector3.Angle(Vector3.forward, direction);
        }

        private void AssertProjectileAimedAhead<T>(string prefabField, float tolerance = 12f) where T : AutoAttackWeapon
        {
            var shots = FireAndCollect<T>(prefabField);
            Assert.That(shots, Is.Not.Empty, $"{typeof(T).Name} did not fire.");
            float best = shots.Min(s => AngleFromAhead(s.transform.forward));
            Assert.That(best, Is.LessThan(tolerance), $"{typeof(T).Name} did not aim at the enemy ahead.");
            foreach (var shot in shots)
                Assert.That(Mathf.Abs(shot.transform.forward.y), Is.LessThan(0.01f), $"{typeof(T).Name} fired off the ground plane.");
        }

        [Test] public void Knife_AimsAtNearestEnemy() => AssertProjectileAimedAhead<KnifeWeapon>("_projectilePrefab");
        [Test] public void ThousandEdge_AimsAtNearestEnemy() => AssertProjectileAimedAhead<ThousandEdgeWeapon>("KnifePrefab");
        [Test] public void FireWand_AimsAtNearestEnemy() => AssertProjectileAimedAhead<FireWandWeapon>("FireballPrefab");
        [Test] public void HolyWand_AimsAtNearestEnemy() => AssertProjectileAimedAhead<HolyWandWeapon>("ProjectilePrefab");
        [Test] public void Gun_AimsOneArmAtNearestEnemy_OnTheGround() => AssertProjectileAimedAhead<GunWeapon>("BulletPrefab");
        [Test] public void CherryBomb_AimsAtNearestEnemy_OnTheGround() => AssertProjectileAimedAhead<CherryBombWeapon>("BombPrefab");
        [Test] public void DeathSpiral_AimsFirstScytheAtNearestEnemy_OnTheGround() => AssertProjectileAimedAhead<DeathSpiralWeapon>("ScythePrefab", 1f);

        [TestCase(typeof(BoneWeapon), "BonePrefab")]
        [TestCase(typeof(RunetracerWeapon), "RunePrefab")]
        public void BouncingWeapons_ThrowTowardNearestEnemy(System.Type weaponType, string prefabField)
        {
            var fire = typeof(WeaponAimingTests).GetMethod(nameof(FireAndCollect), AnyInstance).MakeGenericMethod(weaponType);
            var thrown = (List<GameObject>)fire.Invoke(this, new object[] { prefabField });
            Assert.That(thrown, Is.Not.Empty, $"{weaponType.Name} did not throw.");
            Assert.That(AngleFromAhead(thrown[0].GetComponent<Rigidbody>().linearVelocity), Is.LessThan(1f));
        }

        [Test]
        public void ClockLancet_BeamPointsAtNearestEnemy()
        {
            var beams = FireAndCollect<ClockLancetWeapon>("FreezeBeamPrefab");
            Assert.That(beams, Is.Not.Empty);
            Assert.That(AngleFromAhead(beams[0].transform.forward), Is.LessThan(1f));
        }

        [Test]
        public void SantaWater_FirstFlaskLandsOnNearestEnemy()
        {
            var zones = FireAndCollect<SantaWaterWeapon>("WaterZonePrefab");
            Assert.That(zones, Is.Not.Empty);
            float best = zones.Min(z => Vector3.Distance(new Vector3(z.transform.position.x, 0f, z.transform.position.z), _enemy.transform.position));
            Assert.That(best, Is.LessThan(0.01f), "One flask must land on the nearest enemy.");
        }

        [Test]
        public void LightningRing_StrikesNearestEnemy()
        {
            var strikes = FireAndCollect<LightningRingWeapon>("LightningStrikePrefab");
            Assert.That(strikes, Is.Not.Empty);
            Assert.That(Vector3.Distance(strikes[0].transform.position, _enemy.transform.position), Is.LessThan(0.01f));
        }

        [Test]
        public void BloodyTear_FirstLashTowardNearestEnemy()
        {
            var lashes = FireAndCollect<BloodyTearWeapon>("WhipVisualPrefab");
            Assert.That(lashes, Is.Not.Empty);
            float best = lashes.Min(l => AngleFromAhead(l.transform.position - _owner.transform.position));
            Assert.That(best, Is.LessThan(1f));
        }

        [Test]
        public void WithNoEnemies_KnifeFallsBackToItsOldDirection()
        {
            // Learning Comment: Koi dushman na ho toh Knife pehle ki tarah chalti rahe (facing: +X).
            EnemyController.ActiveEnemies.Remove(_enemy);
            var shots = FireAndCollect<KnifeWeapon>("_projectilePrefab");
            Assert.That(shots, Is.Not.Empty);
            Assert.That(Vector3.Angle(Vector3.right, shots[0].transform.forward), Is.LessThan(12f));
        }
    }
}
