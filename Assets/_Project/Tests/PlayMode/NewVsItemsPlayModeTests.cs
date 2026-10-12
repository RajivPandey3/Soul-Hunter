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
using SoulHunter.Gameplay.Player;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// The VS items added on 2026-10-12 in the real game (NewVsItemsBuilder must have run): each new item can be gained
    /// and applies its stat, Sundered Hour and the two unions form from their VS recipes, and each new weapon hurts
    /// enemies. Raw wiki pages fetched 2026-10-12: Stone_Mask, Silver_Ring, Gold_Ring, Infinite_Corridor, Ebony_Wings,
    /// Vandalier, Eight_The_Sparrow, Phieraggi, Evolution.
    /// </summary>
    public class NewVsItemsPlayModeTests : GameplayPlayModeTestBase
    {
        private PlayerController _player;
        private WeaponManager _weapons;
        private readonly List<GameObject> _spawned = new List<GameObject>();

        private IEnumerator Boot()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
            _player = PlayerController.Instance;
            _player.GetComponent<HealthController>().IsInvincible = true;
            // The starting weapon stays held (it fills a slot) but is switched off: no kills, no level-up screens.
            foreach (var existing in _player.GetComponentsInChildren<AutoAttackWeapon>()) existing.enabled = false;
            _weapons = WeaponManager.Instance;
            _spawned.Clear();
        }

        private static UpgradeData Level(UpgradeType type, int level)
        {
            var upgrade = GameContentCatalog.Load().Upgrades.FirstOrDefault(u => u != null && u.Type == type && u.Level == level);
            Assert.That(upgrade, Is.Not.Null, $"{type} level {level}: run Soul Hunter/Content/Build New VS Items.");
            return upgrade;
        }

        private List<HealthController> RingOfEnemies(float distance, int health = 100000, int count = 8)
        {
            var prefab = GetPrivateField<List<GameObject>>(Object.FindFirstObjectByType<EnemySpawner>(), "_stageEnemyPrefabs")[0];
            var enemies = new List<HealthController>();
            for (int i = 0; i < count; i++)
            {
                var enemy = Object.Instantiate(prefab, _player.transform.position + Quaternion.Euler(0f, i * 360f / count, 0f) * Vector3.forward * distance, Quaternion.identity);
                var touch = enemy.GetComponent<TouchDamage>();
                if (touch != null) touch.enabled = false;
                var hp = enemy.GetComponent<HealthController>();
                hp.Initialize(health);
                enemies.Add(hp);
                _spawned.Add(enemy);
            }
            return enemies;
        }

        private IEnumerator WaitUntilHurt(List<HealthController> enemies, string what, float seconds = 10f)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            bool hurt = false;
            while (!hurt && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                hurt = enemies.Any(h => h == null || !h.gameObject.activeInHierarchy || h.CurrentHealth < h.MaxHealth);
            }
            Assert.That(hurt, Is.True, $"{what} never hurt an enemy within {seconds} s. " + Errors());
        }

        private void ClearEnemies()
        {
            foreach (var go in _spawned) if (go != null) Object.Destroy(go);
            _spawned.Clear();
        }

        [UnityTest]
        public IEnumerator ThePassives_CanBeGained_AndApplyTheirStats()
        {
            yield return Boot();
            var stats = _player.GetComponent<PlayerStats>();
            float greed = stats.Greed, duration = stats.RawDuration, area = stats.RawArea, curse = stats.Curse;

            // Miser's Idol (VS Stone Mask): an ordinary passive, +10% Greed per level, 5 levels.
            Assert.That(_weapons.GetMaxWeaponLevel(UpgradeType.MisersIdol), Is.EqualTo(5));
            _weapons.ApplyUpgrade(Level(UpgradeType.MisersIdol, 1));
            Assert.That(_weapons.GetWeaponLevel(UpgradeType.MisersIdol), Is.EqualTo(1));
            Assert.That(stats.Greed, Is.EqualTo(greed + 0.1f).Within(1e-4f));

            // The rings: only from the ground; level 1 gives nothing; then +5% each level.
            foreach (var ring in new[] { UpgradeType.RingOfLongHours, UpgradeType.RingOfDarkHours })
            {
                Assert.That(_weapons.GetMaxWeaponLevel(ring), Is.EqualTo(9));
                _weapons.ApplyUpgrade(Level(ring, 1));
                Assert.That(_weapons.GetWeaponLevel(ring), Is.EqualTo(0), $"{ring} cannot come from a level-up.");
                Assert.That(_weapons.AcquireStageItem(Level(ring, 1)), Is.True, $"{ring} is picked up from the ground.");
            }
            Assert.That(stats.RawDuration, Is.EqualTo(duration).Within(1e-4f), "Level 1 gives nothing.");
            Assert.That(stats.Curse, Is.EqualTo(curse).Within(1e-4f), "Level 1 gives nothing.");
            _weapons.ApplyUpgrade(Level(UpgradeType.RingOfLongHours, 2));
            _weapons.ApplyUpgrade(Level(UpgradeType.RingOfDarkHours, 2));
            Assert.That(stats.RawDuration, Is.EqualTo(duration + 0.05f).Within(1e-4f));
            Assert.That(stats.RawArea, Is.EqualTo(area + 0.05f).Within(1e-4f));
            Assert.That(stats.Curse, Is.EqualTo(curse + 0.05f).Within(1e-4f));

            // Level-ups never offer a ring, a union's result or a second copy of anything.
            var levelUps = Object.FindFirstObjectByType<LevelUpManager>();
            for (int i = 0; i < 40; i++)
                foreach (var choice in levelUps.GetRandomValidUpgrades(4))
                {
                    Assert.That(choice.Type, Is.Not.EqualTo(UpgradeType.TwilightFlock), "A union only comes from a chest.");
                    Assert.That(UpgradeRarity.IsStageItemOnly(choice.Type), Is.False, $"{choice.Type} is a stage item only.");
                }
            Assert.That(levelUps.GetValidUpgrades().Any(u => u.Type == UpgradeType.MisersIdol && u.Level == 2), Is.True, "Miser's Idol is in the pool.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator DuskRaven_AndAzureRepeater_CanBeGained_AndHurtEnemies()
        {
            foreach (var type in new[] { UpgradeType.DuskRaven, UpgradeType.AzureRepeater })
            {
                yield return Boot();
                Assert.That(Object.FindFirstObjectByType<LevelUpManager>().GetValidUpgrades().Any(u => u.Type == type && u.Level == 1), Is.True, $"{type} is offered by level-ups.");
                int before = _weapons.GetActiveWeaponsCount();
                _weapons.ApplyUpgrade(Level(type, 1));
                yield return null;
                Assert.That(_weapons.GetWeaponLevel(type), Is.EqualTo(1));
                Assert.That(_weapons.GetActiveWeaponsCount(), Is.EqualTo(before + 1));
                var held = _weapons.GetEquippedWeapons().Select(p => p.Value).FirstOrDefault(w => w.gameObject.name.StartsWith(type.ToString()));
                Assert.That(held, Is.Not.Null, $"{type} is held.");
                Assert.That(held.GetType().Name, Is.EqualTo(type + "Weapon"));
                var enemies = RingOfEnemies(3f);
                yield return WaitUntilHurt(enemies, type.ToString());
                ClearEnemies();
                Assert.That(Errors(), Is.EqualTo("(no errors logged)"), type.ToString());
            }
        }

        [UnityTest]
        public IEnumerator SunderedHour_EvolvesFromTheLancetAndBothMaxedRings_FreezesAndHalves()
        {
            yield return Boot();
            _weapons.GiveWeapon(UpgradeType.ClockLancet, 7);
            _weapons.GiveWeapon(UpgradeType.RingOfLongHours, 9);
            _weapons.GiveWeapon(UpgradeType.RingOfDarkHours, 8);
            yield return null;
            Assert.That(_weapons.TryEvolveWeapon(out _), Is.False, "Both rings must be at max level (9).");
            _weapons.GiveWeapon(UpgradeType.RingOfDarkHours, 9);
            Assert.That(_weapons.TryEvolveWeapon(out string name), Is.True, "Clock Lancet 7 + both rings at 9.");
            Assert.That(name, Is.EqualTo("Evo_SunderedHour"));
            yield return null;
            var hour = _player.GetComponentInChildren<SunderedHourWeapon>();
            Assert.That(hour, Is.Not.Null.And.Property("isActiveAndEnabled").True);
            Assert.That(_player.GetComponentsInChildren<ClockLancetWeapon>(false), Is.Empty, "The Clock Lancet is replaced.");

            var enemies = RingOfEnemies(3f, 1000, 4);
            var controllers = enemies.Select(h => h.GetComponent<EnemyController>()).ToList();
            yield return WaitUntilRealtime(() => controllers.Any(c => c != null && c.IsFrozen), "a beam to freeze an enemy");
            Assert.That(enemies.All(h => h.CurrentHealth == 1000), Is.True, "The beam does no damage.");

            hour.HalveEveryEnemyOnScreen();
            Assert.That(enemies.Select(h => h.CurrentHealth), Is.All.EqualTo(500), "Every enemy on screen loses half its health.");
            hour.HalveEveryEnemyOnScreen();
            Assert.That(enemies.Select(h => h.CurrentHealth), Is.All.EqualTo(250));
            Assert.That(hour.Pulses, Is.EqualTo(2));
            ClearEnemies();
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator TwilightFlock_UnitesTheTwoMaxedBirds_FreesASlot_AndLevelsOnItsOwn()
        {
            yield return Boot();
            _weapons.GiveWeapon(UpgradeType.Peachone, 8);
            _weapons.GiveWeapon(UpgradeType.DuskRaven, 7);
            yield return null;
            Assert.That(_weapons.TryEvolveWeapon(out _), Is.False, "Both birds must be at max level.");
            _weapons.ApplyUpgrade(Level(UpgradeType.DuskRaven, 8));
            int before = _weapons.GetActiveWeaponsCount();
            Assert.That(_weapons.TryEvolveWeapon(out string name), Is.True);
            Assert.That(name, Is.EqualTo("Union_TwilightFlock"));
            yield return null;
            Assert.That(_weapons.GetActiveWeaponsCount(), Is.EqualTo(before - 1), "Acquisition frees a weapon slot.");
            Assert.That(_weapons.GetWeaponLevel(UpgradeType.TwilightFlock), Is.EqualTo(1), "The union arrives at level 1.");
            var flock = _player.GetComponentInChildren<TwilightFlockWeapon>();
            Assert.That(flock, Is.Not.Null);
            Assert.That(flock.CurrentLevel, Is.EqualTo(1));
            Assert.That(_player.GetComponentsInChildren<PeachoneWeapon>(false), Is.Empty, "Both birds are gone.");
            var slots = _weapons.GetInventory(false);
            Assert.That(slots.Count(s => s.Type == UpgradeType.TwilightFlock), Is.EqualTo(1));
            Assert.That(slots.Any(s => s.Type == UpgradeType.Peachone || s.Type == UpgradeType.DuskRaven), Is.False);

            // Level-ups now offer its next level, and it takes it.
            Assert.That(Object.FindFirstObjectByType<LevelUpManager>().GetValidUpgrades().Any(u => u.Type == UpgradeType.TwilightFlock && u.Level == 2), Is.True);
            _weapons.ApplyUpgrade(Level(UpgradeType.TwilightFlock, 2));
            Assert.That(flock.CurrentLevel, Is.EqualTo(2));
            Assert.That(flock.LevelArea, Is.EqualTo(2.4f).Within(1e-4f));

            var enemies = RingOfEnemies(3f);
            yield return WaitUntilHurt(enemies, "Twilight Flock");
            ClearEnemies();
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator RevenantWheel_UnitesTheTwoMaxedGuns_OnlyWithTirajisu_AndItsLasersHurt()
        {
            yield return Boot();
            _weapons.GiveWeapon(UpgradeType.Gun, 8);
            _weapons.GiveWeapon(UpgradeType.AzureRepeater, 8);
            yield return null;
            Assert.That(_weapons.TryEvolveWeapon(out _), Is.False, "Phieraggi also requires Tirajisu.");
            _weapons.ApplyUpgrade(Level(UpgradeType.Tiragisu, 1)); // +1 Revival
            int before = _weapons.GetActiveWeaponsCount();
            Assert.That(_weapons.TryEvolveWeapon(out string name), Is.True);
            Assert.That(name, Is.EqualTo("Union_RevenantWheel"));
            yield return null;
            Assert.That(_weapons.GetActiveWeaponsCount(), Is.EqualTo(before - 1), "Acquisition frees a weapon slot.");
            var wheel = _player.GetComponentInChildren<RevenantWheelWeapon>();
            Assert.That(wheel, Is.Not.Null);
            Assert.That(_player.GetComponentsInChildren<GunWeapon>(false), Is.Empty, "Both guns are gone.");
            int revivals = _player.GetComponent<PlayerStats>().Revivals;
            Assert.That(revivals, Is.GreaterThanOrEqualTo(1));
            Assert.That(wheel.LaserCount, Is.EqualTo(4 + Mathf.Min(10, revivals)), "+1 laser per Revival.");
            Assert.That(wheel.BaseDamage, Is.EqualTo(15f + Mathf.Min(10, revivals)), "+1 base damage per Revival.");

            var enemies = RingOfEnemies(4f);
            yield return WaitUntilRealtime(() => wheel.ActiveLasers > 0, "the first laser");
            yield return WaitUntilHurt(enemies, "Revenant Wheel");
            // "It retracts when not firing": with the weapon switched off, the lasers go.
            wheel.gameObject.SetActive(false);
            Assert.That(wheel.ActiveLasers, Is.EqualTo(0));
            ClearEnemies();
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
