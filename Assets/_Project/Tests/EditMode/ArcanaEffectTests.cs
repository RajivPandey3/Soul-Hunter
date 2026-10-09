using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Weapons;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// The four Arcanas Soul Hunter has, each on its own wiki page (raw, fetched 2026-10-09):
    /// - Iron_Blue_Will_(VII): Knife, Thousand Edge, Axe and Phiera (Gun) "gain up to three bounces"; Death Spiral
    ///   "bounces off the edges of the screen once".
    /// - Waltz_of_Pearls_(XI): Magic Wand, Holy Wand and Fire Wand "can hit 3 more targets per shot"; Hellfire (Hellmaw
    ///   Comet) bounces off the screen edges once, Cross and Heaven Sword twice.
    /// - Gemini_(I): Peachone and Phiera (Gun) get counterparts.
    /// Unlisted weapons and Shadow Kael's mirrored copies are unaffected; a pooled Projectile no longer takes a global count.
    /// </summary>
    public class ArcanaEffectTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private readonly List<Object> _created = new List<Object>();

        private T Track<T>(T obj) where T : Object { _created.Add(obj); return obj; }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created) if (obj != null) Object.DestroyImmediate(obj);
            _created.Clear();
        }

        private ArcanaManager NewManager(params ArcanaData.ArcanaType[] held)
        {
            var manager = Track(new GameObject("Arcana")).AddComponent<ArcanaManager>();
            typeof(ArcanaManager).GetMethod("Awake", Private).Invoke(manager, null);
            foreach (var type in held)
            {
                var card = Track(ScriptableObject.CreateInstance<ArcanaData>());
                card.CardName = type.ToString();
                card.Type = type;
                manager.ApplyArcana(card);
            }
            return manager;
        }

        private T Weapon<T>(Transform parent = null) where T : AutoAttackWeapon
        {
            var go = Track(new GameObject(typeof(T).Name));
            if (parent != null) go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        private List<AutoAttackWeapon> AllWeapons() => new List<AutoAttackWeapon>
        {
            Weapon<KnifeWeapon>(), Weapon<ThousandEdgeWeapon>(), Weapon<AxeWeapon>(), Weapon<GunWeapon>(), Weapon<DeathSpiralWeapon>(),
            Weapon<MagicWandWeapon>(), Weapon<HolyWandWeapon>(), Weapon<FireWandWeapon>(), Weapon<HellmawCometWeapon>(), Weapon<CrossWeapon>(),
            Weapon<PeachoneWeapon>(), Weapon<BoneWeapon>(), Weapon<RunetracerWeapon>(), Weapon<WhipWeapon>(), Weapon<CherryBombWeapon>(),
            Weapon<SantaWaterWeapon>(), Weapon<LightningRingWeapon>(), Weapon<BloodyTearWeapon>(),
        };

        [Test]
        public void WithoutTheCards_NoWeaponBounces()
        {
            NewManager();
            foreach (var weapon in AllWeapons())
            {
                Assert.That(ArcanaManager.EnemyBouncesFor(weapon), Is.EqualTo(0), weapon.GetType().Name);
                Assert.That(ArcanaManager.ScreenBouncesFor(weapon), Is.EqualTo(0), weapon.GetType().Name);
            }
        }

        [Test]
        public void IronBlueWill_GivesOnlyItsListedWeaponsBounces()
        {
            NewManager(ArcanaData.ArcanaType.IronBlueWill);
            var enemyBounce = new HashSet<System.Type> { typeof(KnifeWeapon), typeof(ThousandEdgeWeapon), typeof(AxeWeapon), typeof(GunWeapon) };
            foreach (var weapon in AllWeapons())
            {
                var type = weapon.GetType();
                Assert.That(ArcanaManager.EnemyBouncesFor(weapon), Is.EqualTo(enemyBounce.Contains(type) ? 3 : 0), type.Name + " enemy bounces");
                Assert.That(ArcanaManager.ScreenBouncesFor(weapon), Is.EqualTo(type == typeof(DeathSpiralWeapon) ? 1 : 0), type.Name + " screen bounces");
            }
        }

        [Test]
        public void WaltzOfPearls_GivesOnlyItsListedWeaponsBounces()
        {
            NewManager(ArcanaData.ArcanaType.WaltzOfPearls);
            var enemyBounce = new HashSet<System.Type> { typeof(MagicWandWeapon), typeof(HolyWandWeapon), typeof(FireWandWeapon) };
            foreach (var weapon in AllWeapons())
            {
                var type = weapon.GetType();
                int screen = type == typeof(HellmawCometWeapon) ? 1 : type == typeof(CrossWeapon) ? 2 : 0;
                Assert.That(ArcanaManager.EnemyBouncesFor(weapon), Is.EqualTo(enemyBounce.Contains(type) ? 3 : 0), type.Name + " enemy bounces");
                Assert.That(ArcanaManager.ScreenBouncesFor(weapon), Is.EqualTo(screen), type.Name + " screen bounces");
            }
        }

        [Test]
        public void ShadowKaelsMirroredWeapons_GetNoArcanaBounces()
        {
            NewManager(ArcanaData.ArcanaType.IronBlueWill, ArcanaData.ArcanaType.WaltzOfPearls);
            var boss = Track(new GameObject("ShadowKael"));
            boss.AddComponent<EnemyController>();
            foreach (var weapon in new AutoAttackWeapon[]
                     { Weapon<KnifeWeapon>(boss.transform), Weapon<MagicWandWeapon>(boss.transform), Weapon<CrossWeapon>(boss.transform), Weapon<DeathSpiralWeapon>(boss.transform) })
            {
                Assert.That(weapon.IsHeldByEnemy, Is.True);
                Assert.That(ArcanaManager.EnemyBouncesFor(weapon), Is.EqualTo(0), weapon.GetType().Name);
                Assert.That(ArcanaManager.ScreenBouncesFor(weapon), Is.EqualTo(0), weapon.GetType().Name);
            }
        }

        [Test]
        public void APooledProjectile_TakesNoGlobalArcanaBounces()
        {
            // Before 2026-10-09 every Projectile read ArcanaManager.ProjectileBounceCount (3 with Waltz), Bone and Runetracer
            // and Shadow Kael's shots included.
            NewManager(ArcanaData.ArcanaType.WaltzOfPearls, ArcanaData.ArcanaType.IronBlueWill);
            var shotObject = Track(new GameObject("Shot"));
            shotObject.AddComponent<Rigidbody>();
            shotObject.AddComponent<SphereCollider>();
            var shot = shotObject.AddComponent<Projectile>();
            shot.Initialize(Vector3.forward, 5f, 10f, 1f);
            Assert.That(shot.MaxBounces, Is.EqualTo(0));
            Assert.That(shot.ScreenBouncesLeft, Is.EqualTo(0));
            shot.AddBounces(3);
            shot.AddScreenBounces(2);
            Assert.That(shot.MaxBounces, Is.EqualTo(3));
            Assert.That(shot.ScreenBouncesLeft, Is.EqualTo(2));
            shot.Initialize(Vector3.forward, 5f, 10f, 1f);
            Assert.That(shot.MaxBounces, Is.EqualTo(0), "A reused shot starts with no bounces.");
            Assert.That(shot.ScreenBouncesLeft, Is.EqualTo(0));
        }

        [Test]
        public void Gemini_ListsPeachoneAndGunOnly()
        {
            NewManager(ArcanaData.ArcanaType.Gemini);
            Assert.That(ArcanaManager.GeminiActive, Is.True);
            foreach (var weapon in AllWeapons())
                Assert.That(ArcanaManager.HasGeminiCounterpart(weapon), Is.EqualTo(weapon is PeachoneWeapon || weapon is GunWeapon), weapon.GetType().Name);
        }

        [Test]
        public void Awake_Numbers_AreTheWikis()
        {
            Assert.That(ArcanaManager.AwakeRevivals, Is.EqualTo(3));
            Assert.That(ArcanaManager.AwakeMaxHealthFactor, Is.EqualTo(1.1f));
            Assert.That(ArcanaManager.AwakeArmor, Is.EqualTo(1));
            Assert.That(ArcanaManager.AwakeStatBonus, Is.EqualTo(0.05f));
            Assert.That(ArcanaManager.ArcanaEnemyBounces, Is.EqualTo(3));
        }
    }
}
