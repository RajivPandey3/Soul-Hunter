using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Weapons;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment:
    /// SpectralCombatTests validates Level 7 (The Sea of Lost Souls) combat rules for spectral enemies.
    /// According to AGENTS.md Section 5 and Docs/Requirements/P1-07-reconciliation.md Section 3.2:
    /// - Spectral enemies are intangible and immune to non-whitelisted damage types (Normal, Fire, Poison, Shadow).
    /// - Holy damage deals 2x vulnerability multiplier damage to spectral enemies.
    /// - Magic and Spectral damage types deal unmitigated 1x damage to spectral enemies.
    /// - Non-spectral enemies accept all standard damage types (Normal, Fire, Poison, Shadow) normally.
    /// - MagicWandWeapon initializes projectiles with DamageType.Magic to damage Level 7 spectral enemies.
    /// - SantaWaterWeapon spawns water zones configured with DamageType.Holy to damage Level 7 spectral enemies with 2x vulnerability while Normal damage is discarded.
    /// </summary>
    public class SpectralCombatTests
    {
        private GameObject _enemyObject;
        private EnemyController _enemyController;
        private HealthController _healthController;
        private BoxCollider _enemyCollider;
        private readonly List<GameObject> _cleanupList = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            // Learning Comment:
            // Naye test se pehle fresh enemy GameObject banate hain aur us par
            // EnemyController, HealthController, aur BoxCollider attach karte hain.
            _cleanupList.Clear();
            _enemyObject = new GameObject(nameof(SpectralCombatTests));
            _enemyObject.tag = "Enemy";
            _enemyObject.transform.position = new Vector3(0f, 0f, 5f);
            _enemyCollider = _enemyObject.AddComponent<BoxCollider>();
            _enemyController = _enemyObject.AddComponent<EnemyController>();
            _healthController = _enemyObject.AddComponent<HealthController>();

            // Initial health ko 100 par explicitly set karte hain
            _healthController.Initialize(100);

            // EditMode mein coroutine knockback side-effects se bachne ke liye KnockbackResistance ko 1 set karte hain
            _healthController.KnockbackResistance = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            // Learning Comment:
            // Test complete hone ke baad scene cleanup karte hain taake hierarchy leak na ho.
            for (int i = _cleanupList.Count - 1; i >= 0; i--)
            {
                if (_cleanupList[i] != null)
                {
                    Object.DestroyImmediate(_cleanupList[i]);
                }
            }
            _cleanupList.Clear();

            if (_enemyObject != null)
            {
                Object.DestroyImmediate(_enemyObject);
            }

            if (_enemyController != null && EnemyController.ActiveEnemies.Contains(_enemyController))
            {
                EnemyController.ActiveEnemies.Remove(_enemyController);
            }
        }

        #region Spectral Enemy Immunity Tests

        /// <summary>
        /// Learning Comment:
        /// Level 7 spectral rules ke tehat spectral enemies physical aur elemental non-holy damage (Normal, Fire, Poison, Shadow)
        /// ko discard kar dete hain aur unki health par koi farq nahi padta.
        /// </summary>
        [TestCase(DamageType.Normal)]
        [TestCase(DamageType.Fire)]
        [TestCase(DamageType.Poison)]
        [TestCase(DamageType.Shadow)]
        public void TakeDamage_SpectralEnemy_DiscardsNonWhitelistedDamageTypes(DamageType damageType)
        {
            // Learning Comment: Enemy ko spectral declare karte hain with Holy 2x vulnerability (Level 7 standard)
            _enemyController.ConfigureDamageVulnerability(DamageType.Holy, 2f, isSpectral: true);

            int initialHealth = _healthController.CurrentHealth;
            var packet = new DamagePacket(25, Vector3.zero, Vector3.zero, damageType);

            _healthController.TakeDamage(packet);

            Assert.That(_healthController.CurrentHealth, Is.EqualTo(initialHealth),
                $"Spectral enemy should discard {damageType} damage and retain initial health.");
        }

        [Test]
        public void TakeDamage_SpectralEnemy_DiscardsNormalFirePoisonAndShadowDamage()
        {
            // Learning Comment: Ek hi test mein saare non-whitelisted damage types verify karte hain.
            _enemyController.ConfigureDamageVulnerability(DamageType.Holy, 2f, isSpectral: true);

            int initialHealth = _healthController.CurrentHealth;
            DamageType[] rejectedTypes = { DamageType.Normal, DamageType.Fire, DamageType.Poison, DamageType.Shadow };

            foreach (var damageType in rejectedTypes)
            {
                var packet = new DamagePacket(20, Vector3.zero, Vector3.zero, damageType);
                _healthController.TakeDamage(packet);

                Assert.That(_healthController.CurrentHealth, Is.EqualTo(initialHealth),
                    $"Spectral enemy must discard {damageType} damage without taking damage.");
            }
        }

        #endregion

        #region Spectral Enemy Holy Vulnerability Tests

        /// <summary>
        /// Learning Comment:
        /// Spectral enemies Holy damage ke khilaf 2x vulnerability rakhte hain.
        /// Agar 20 Holy damage diya jaye toh 40 damage deduct hona chahiye.
        /// </summary>
        [Test]
        public void TakeDamage_SpectralEnemy_AppliesTwoFoldVulnerabilityForHolyDamage()
        {
            _enemyController.ConfigureDamageVulnerability(DamageType.Holy, 2f, isSpectral: true);

            const int baseDamage = 20;
            int initialHealth = _healthController.CurrentHealth;
            var packet = new DamagePacket(baseDamage, Vector3.zero, Vector3.zero, DamageType.Holy);

            _healthController.TakeDamage(packet);

            int expectedDamage = Mathf.RoundToInt(baseDamage * 2f);
            Assert.That(_healthController.CurrentHealth, Is.EqualTo(initialHealth - expectedDamage),
                "Holy damage on spectral enemies must apply a 2x vulnerability multiplier.");
        }

        #endregion

        #region Spectral Enemy Magic & Spectral Tests

        /// <summary>
        /// Learning Comment:
        /// Magic aur Spectral damage types spectral whitelist mein shaamil hain aur
        /// bina kisi extra multiplier ke baseline 1x damage deal karte hain.
        /// </summary>
        [TestCase(DamageType.Magic)]
        [TestCase(DamageType.Spectral)]
        public void TakeDamage_SpectralEnemy_AcceptsMagicAndSpectralDamageAtOneXMultiplier(DamageType damageType)
        {
            _enemyController.ConfigureDamageVulnerability(DamageType.Holy, 2f, isSpectral: true);

            const int baseDamage = 25;
            int initialHealth = _healthController.CurrentHealth;
            var packet = new DamagePacket(baseDamage, Vector3.zero, Vector3.zero, damageType);

            _healthController.TakeDamage(packet);

            Assert.That(_healthController.CurrentHealth, Is.EqualTo(initialHealth - baseDamage),
                $"Spectral enemy must accept {damageType} damage at 1x damage.");
        }

        [Test]
        public void TakeDamage_SpectralEnemy_AcceptsMagicAndSpectralDamageTypes()
        {
            _enemyController.ConfigureDamageVulnerability(DamageType.Holy, 2f, isSpectral: true);

            // Magic damage verification
            int healthBeforeMagic = _healthController.CurrentHealth;
            _healthController.TakeDamage(new DamagePacket(15, Vector3.zero, Vector3.zero, DamageType.Magic));
            Assert.That(_healthController.CurrentHealth, Is.EqualTo(healthBeforeMagic - 15),
                "Magic damage should be accepted at 1x on spectral enemies.");

            // Spectral damage verification
            int healthBeforeSpectral = _healthController.CurrentHealth;
            _healthController.TakeDamage(new DamagePacket(20, Vector3.zero, Vector3.zero, DamageType.Spectral));
            Assert.That(_healthController.CurrentHealth, Is.EqualTo(healthBeforeSpectral - 20),
                "Spectral damage should be accepted at 1x on spectral enemies.");
        }

        #endregion

        #region Non-Spectral Enemy Tests

        /// <summary>
        /// Learning Comment:
        /// Normal (non-spectral) enemies par koi damage type discard nahi hota.
        /// Normal, Fire, Poison, aur Shadow damage sab 1x damage deal karte hain.
        /// </summary>
        [TestCase(DamageType.Normal)]
        [TestCase(DamageType.Fire)]
        [TestCase(DamageType.Poison)]
        [TestCase(DamageType.Shadow)]
        public void TakeDamage_NonSpectralEnemy_AcceptsPhysicalAndElementalDamageTypes(DamageType damageType)
        {
            _enemyController.ConfigureDamageVulnerability(DamageType.Normal, 1f, isSpectral: false);

            const int baseDamage = 15;
            int initialHealth = _healthController.CurrentHealth;
            var packet = new DamagePacket(baseDamage, Vector3.zero, Vector3.zero, damageType);

            _healthController.TakeDamage(packet);

            Assert.That(_healthController.CurrentHealth, Is.EqualTo(initialHealth - baseDamage),
                $"Non-spectral enemy must take unmitigated damage from {damageType}.");
        }

        [Test]
        public void TakeDamage_NonSpectralEnemy_AcceptsNormalFirePoisonAndShadowDamage()
        {
            _enemyController.ConfigureDamageVulnerability(DamageType.Normal, 1f, isSpectral: false);

            DamageType[] physicalAndElemental = { DamageType.Normal, DamageType.Fire, DamageType.Poison, DamageType.Shadow };
            foreach (var dmgType in physicalAndElemental)
            {
                int healthBefore = _healthController.CurrentHealth;
                _healthController.TakeDamage(new DamagePacket(10, Vector3.zero, Vector3.zero, dmgType));
                Assert.That(_healthController.CurrentHealth, Is.EqualTo(healthBefore - 10),
                    $"Non-spectral enemy should receive damage from {dmgType}.");
            }
        }

        #endregion

        #region Magic Wand Spectral Combat Tests

        /// <summary>
        /// Learning Comment:
        /// Test fixture ke liye MagicWandWeapon aur projectile prefab create karte hain.
        /// Private field _projectilePrefab ko reflection se wire karte hain aur Awake invoke karte hain.
        /// Tamam GameObjects _cleanupList mein register hote hain taake TearDown mein destroy ho sakein.
        /// </summary>
        private MagicWandWeapon CreateMagicWandWeapon(out GameObject projectilePrefab)
        {
            var wandObject = new GameObject("TestMagicWandWeapon");
            _cleanupList.Add(wandObject);
            var wand = wandObject.AddComponent<MagicWandWeapon>();

            // Learning Comment:
            // Awake() ko invoke karte hain taake internal releaseHandler aur stats initialize ho jayein.
            MethodInfo awakeMethod = typeof(MagicWandWeapon).GetMethod("Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            awakeMethod?.Invoke(wand, null);

            projectilePrefab = new GameObject("TestProjectilePrefab");
            _cleanupList.Add(projectilePrefab);
            projectilePrefab.AddComponent<Projectile>();
            projectilePrefab.SetActive(false);

            FieldInfo prefabField = typeof(MagicWandWeapon).GetField("_projectilePrefab",
                BindingFlags.Instance | BindingFlags.NonPublic);
            prefabField?.SetValue(wand, projectilePrefab);

            return wand;
        }

        /// <summary>
        /// Learning Comment:
        /// MagicWandWeapon ke private FireWand() method ko invoke karke fired Projectile instance capture karte hain.
        /// Instantiated projectile ko scene leak se bachane ke liye _cleanupList mein add karte hain.
        /// </summary>
        private Projectile FireMagicWand(MagicWandWeapon wand)
        {
            if (_enemyController != null && !EnemyController.ActiveEnemies.Contains(_enemyController))
            {
                EnemyController.ActiveEnemies.Add(_enemyController);
            }

            Projectile firedProjectile = null;
            void Handler(Projectile p)
            {
                firedProjectile = p;
                if (p != null)
                {
                    _cleanupList.Add(p.gameObject);
                }
            }

            wand.OnProjectileFired += Handler;

            MethodInfo fireMethod = typeof(MagicWandWeapon).GetMethod("FireWand",
                BindingFlags.Instance | BindingFlags.NonPublic);
            fireMethod?.Invoke(wand, null);

            wand.OnProjectileFired -= Handler;
            return firedProjectile;
        }

        /// <summary>
        /// Learning Comment:
        /// Normal damage deal karne wala standard test projectile instantiate karte hain.
        /// </summary>
        private Projectile CreateNormalProjectile(int damage = 10)
        {
            var projObject = new GameObject("TestNormalProjectile");
            _cleanupList.Add(projObject);
            var projectile = projObject.AddComponent<Projectile>();
            projectile.Initialize(Vector3.forward, 20f, damage, 3f, DamageType.Normal, "NormalWeapon");
            projObject.SetActive(true);
            return projectile;
        }

        /// <summary>
        /// Learning Comment:
        /// Projectile ke private OnTriggerEnter method ko target collider ke sath execute karwate hain
        /// taake combat damage transmission pipeline accurately trigger ho.
        /// </summary>
        private void TriggerProjectileHit(Projectile projectile, Collider targetCollider)
        {
            MethodInfo triggerMethod = typeof(Projectile).GetMethod("OnTriggerEnter",
                BindingFlags.Instance | BindingFlags.NonPublic);
            triggerMethod?.Invoke(projectile, new object[] { targetCollider });
        }

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check 1:
        /// MagicWandWeapon fired projectiles ko DamageType.Magic ke sath initialize karta hai.
        /// Level 7 spectral combat rules ke mutabiq Magic damage spectral enemies par 1x unmitigated damage deal karta hai.
        /// </summary>
        [Test]
        public void MagicWandWeapon_FiresProjectile_InitializesWithMagicDamageType()
        {
            var wand = CreateMagicWandWeapon(out _);
            var projectile = FireMagicWand(wand);

            Assert.That(projectile, Is.Not.Null, "MagicWandWeapon should successfully instantiate and fire a projectile.");

            FieldInfo damageTypeField = typeof(Projectile).GetField("_damageType",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(damageTypeField, Is.Not.Null, "Projectile must have _damageType field.");

            DamageType actualType = (DamageType)damageTypeField.GetValue(projectile);
            Assert.That(actualType, Is.EqualTo(DamageType.Magic),
                "MagicWandWeapon must initialize fired projectiles with DamageType.Magic.");

            FieldInfo sourceNameField = typeof(Projectile).GetField("_sourceWeaponName",
                BindingFlags.Instance | BindingFlags.NonPublic);
            string sourceName = (string)sourceNameField?.GetValue(projectile);
            Assert.That(sourceName, Is.EqualTo("Magic Wand"),
                "Fired projectile source weapon name should be 'Magic Wand'.");
        }

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check 2:
        /// MagicWandWeapon projectile spectral enemy (IsSpectral == true) par hit hone par
        /// HealthController ki health ko effectively reduce karta hai.
        /// </summary>
        [Test]
        public void MagicWandWeapon_Projectile_ReducesSpectralEnemyHealth()
        {
            _enemyController.ConfigureDamageVulnerability(DamageType.Holy, 2f, isSpectral: true);
            Assert.That(_enemyController.IsSpectral, Is.True, "Enemy must be spectral (Level 7 rules).");

            int initialHealth = _healthController.CurrentHealth;
            var wand = CreateMagicWandWeapon(out _);
            var projectile = FireMagicWand(wand);

            Assert.That(projectile, Is.Not.Null, "Fired projectile should not be null.");

            TriggerProjectileHit(projectile, _enemyCollider);

            Assert.That(_healthController.CurrentHealth, Is.LessThan(initialHealth),
                "MagicWandWeapon projectile must reduce the health of spectral enemies.");
            Assert.That(_healthController.CurrentHealth, Is.EqualTo(initialHealth - wand.BaseDamage),
                $"MagicWandWeapon projectile must deal exactly base damage ({wand.BaseDamage}) to spectral enemy.");
        }

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check 3:
        /// Normal damage projectiles spectral enemies (IsSpectral == true) par discard ho jate hain
        /// aur HealthController ki health bilkul reduce nahi hoti.
        /// </summary>
        [Test]
        public void Projectile_NormalDamage_DiscardedBySpectralEnemyWithoutReducingHealth()
        {
            _enemyController.ConfigureDamageVulnerability(DamageType.Holy, 2f, isSpectral: true);
            Assert.That(_enemyController.IsSpectral, Is.True, "Enemy must be spectral (Level 7 rules).");

            int initialHealth = _healthController.CurrentHealth;
            var normalProjectile = CreateNormalProjectile(damage: 25);

            TriggerProjectileHit(normalProjectile, _enemyCollider);

            Assert.That(_healthController.CurrentHealth, Is.EqualTo(initialHealth),
                "Normal damage projectile must be discarded by spectral enemy without reducing health.");
        }

        /// <summary>
        /// Learning Comment:
        /// Level 7 spectral combat rule integration verification:
        /// MagicWandWeapon projectiles successfully damage spectral enemies while Normal damage projectiles are discarded.
        /// </summary>
        [Test]
        public void SpectralEnemy_AcceptsMagicWandProjectiles_WhileDiscardingNormalProjectiles()
        {
            _enemyController.ConfigureDamageVulnerability(DamageType.Holy, 2f, isSpectral: true);
            Assert.That(_enemyController.IsSpectral, Is.True, "Enemy must be spectral.");

            int initialHealth = _healthController.CurrentHealth;

            // 1. Normal damage projectile hit test: Discarded
            var normalProjectile = CreateNormalProjectile(damage: 20);
            TriggerProjectileHit(normalProjectile, _enemyCollider);

            Assert.That(_healthController.CurrentHealth, Is.EqualTo(initialHealth),
                "Spectral enemy must discard Normal damage projectile without any health reduction.");

            // 2. Magic Wand projectile hit test: Accepted and health reduced
            var wand = CreateMagicWandWeapon(out _);
            var wandProjectile = FireMagicWand(wand);
            Assert.That(wandProjectile, Is.Not.Null, "MagicWand projectile must be fired.");

            TriggerProjectileHit(wandProjectile, _enemyCollider);

            Assert.That(_healthController.CurrentHealth, Is.EqualTo(initialHealth - wand.BaseDamage),
                "Spectral enemy must accept MagicWand projectile and reduce health accordingly.");
        }

        #endregion

        #region Santa Water Spectral Combat Tests

        /// <summary>
        /// Learning Comment:
        /// Test fixture ke liye SantaWaterWeapon aur water zone prefab create karte hain.
        /// WaterZonePrefab par BoxCollider attach karte hain taake TouchDamage component requirements meet hon.
        /// Tamam GameObjects _cleanupList mein register hote hain taake TearDown mein destroy ho sakein.
        /// </summary>
        private SantaWaterWeapon CreateSantaWaterWeapon(out GameObject waterZonePrefab, float damage = 15f)
        {
            var weaponObject = new GameObject("TestSantaWaterWeapon");
            _cleanupList.Add(weaponObject);
            var weapon = weaponObject.AddComponent<SantaWaterWeapon>();
            weapon.DamageAmount = damage;

            waterZonePrefab = new GameObject("TestWaterZonePrefab");
            _cleanupList.Add(waterZonePrefab);
            waterZonePrefab.AddComponent<BoxCollider>();

            weapon.WaterZonePrefab = waterZonePrefab;
            return weapon;
        }

        /// <summary>
        /// Learning Comment:
        /// SantaWaterWeapon ke private SpawnWaterZone method ko reflection ke zariye invoke karte hain.
        /// Naye spawn hone wale water zone GameObject ko capture karte hain aur _cleanupList mein add karte hain
        /// taake TearDown mein hierarchy clean ho sake aur leak na ho.
        /// </summary>
        private GameObject SpawnWaterZone(SantaWaterWeapon weapon, Vector3 position)
        {
            var beforeObjects = new HashSet<GameObject>();
            foreach (var td in Object.FindObjectsOfType<TouchDamage>())
            {
                if (td != null) beforeObjects.Add(td.gameObject);
            }

            MethodInfo spawnMethod = typeof(SantaWaterWeapon).GetMethod("SpawnWaterZone",
                BindingFlags.Instance | BindingFlags.NonPublic);
            spawnMethod?.Invoke(weapon, new object[] { position });

            GameObject spawnedZone = null;
            foreach (var td in Object.FindObjectsOfType<TouchDamage>())
            {
                if (td != null && !beforeObjects.Contains(td.gameObject))
                {
                    spawnedZone = td.gameObject;
                    break;
                }
            }

            if (spawnedZone == null && weapon.WaterZonePrefab != null)
            {
                spawnedZone = GameObject.Find(weapon.WaterZonePrefab.name + "(Clone)");
            }

            if (spawnedZone != null && !_cleanupList.Contains(spawnedZone))
            {
                _cleanupList.Add(spawnedZone);
            }

            return spawnedZone;
        }

        /// <summary>
        /// Learning Comment:
        /// Normal damage deal karne wala test TouchDamage GameObject instantiate karte hain.
        /// TargetTag "Enemy" aur DamageType.Normal set karte hain taake spectral rejection test ho sake.
        /// Newly instantiated GameObject ko _cleanupList mein track karte hain.
        /// </summary>
        private TouchDamage CreateNormalTouchDamage(float damage = 20f)
        {
            var touchObject = new GameObject("TestNormalTouchDamage");
            _cleanupList.Add(touchObject);
            touchObject.AddComponent<BoxCollider>();
            var touchDamage = touchObject.AddComponent<TouchDamage>();
            touchDamage.DamageAmount = damage;
            touchDamage.DamageType = DamageType.Normal;
            touchDamage.TargetTag = "Enemy";
            touchDamage.SourceWeaponName = "NormalTouchWeapon";
            return touchDamage;
        }

        /// <summary>
        /// Learning Comment:
        /// TouchDamage component ke OnTriggerEnter aur Update methods ko reflection se invoke karte hain
        /// taake contact registration aur damage packet transmission dono accurately execute hon.
        /// </summary>
        private void TriggerTouchDamageContact(TouchDamage touchDamage, Collider targetCollider)
        {
            MethodInfo triggerMethod = typeof(TouchDamage).GetMethod("OnTriggerEnter",
                BindingFlags.Instance | BindingFlags.NonPublic);
            triggerMethod?.Invoke(touchDamage, new object[] { targetCollider });

            MethodInfo updateMethod = typeof(TouchDamage).GetMethod("Update",
                BindingFlags.Instance | BindingFlags.NonPublic);
            updateMethod?.Invoke(touchDamage, null);
        }

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check 1:
        /// SantaWaterWeapon.SpawnWaterZone ek water zone GameObject instantiate karta hai jisme
        /// TouchDamage component attached hota hai aur wo DamageType.Holy, TargetTag="Enemy",
        /// aur SourceWeaponName="Santa Water" ke sath configure hota hai.
        /// </summary>
        [Test]
        public void SantaWaterWeapon_SpawnWaterZone_ConfiguresTouchDamageWithHolyDamageAndEnemyTargetTag()
        {
            const float configuredDamage = 18f;
            var weapon = CreateSantaWaterWeapon(out _, damage: configuredDamage);

            var waterZone = SpawnWaterZone(weapon, new Vector3(1f, 0f, 2f));

            Assert.That(waterZone, Is.Not.Null,
                "SantaWaterWeapon.SpawnWaterZone must successfully instantiate a water zone GameObject.");

            var touchDamage = waterZone.GetComponent<TouchDamage>();
            Assert.That(touchDamage, Is.Not.Null,
                "Spawned water zone must have a TouchDamage component attached.");

            Assert.That(touchDamage.DamageType, Is.EqualTo(DamageType.Holy),
                "TouchDamage on SantaWater zone must be configured with DamageType.Holy.");
            Assert.That(touchDamage.TargetTag, Is.EqualTo("Enemy"),
                "TouchDamage on SantaWater zone must have TargetTag set to 'Enemy'.");
            Assert.That(touchDamage.SourceWeaponName, Is.EqualTo("Santa Water"),
                "TouchDamage on SantaWater zone must have SourceWeaponName set to 'Santa Water'.");
            Assert.That(touchDamage.DamageAmount, Is.EqualTo(configuredDamage),
                "TouchDamage on SantaWater zone must match weapon's DamageAmount.");
            Assert.That(touchDamage.DamageInterval, Is.EqualTo(0.5f),
                "TouchDamage on SantaWater zone must have DamageInterval of 0.5 seconds.");
        }

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check 2:
        /// Jab SantaWaterWeapon water zone kisi spectral enemy (isSpectral=true) se contact karta hai
        /// jiski Holy vulnerability 2f multiplier par set ho, toh HealthController ki health
        /// exactly Mathf.RoundToInt(DamageAmount * 2f) se reduce hoti hai.
        /// </summary>
        [Test]
        public void SantaWaterWeapon_WaterZoneContact_ReducesSpectralEnemyHealthByHolyVulnerabilityMultiplier()
        {
            _enemyController.ConfigureDamageVulnerability(DamageType.Holy, 2f, isSpectral: true);
            Assert.That(_enemyController.IsSpectral, Is.True, "Enemy must be spectral (Level 7 rules).");

            const float weaponDamage = 15f;
            var weapon = CreateSantaWaterWeapon(out _, damage: weaponDamage);
            var waterZone = SpawnWaterZone(weapon, _enemyObject.transform.position);

            Assert.That(waterZone, Is.Not.Null, "Water zone must be successfully spawned.");
            var touchDamage = waterZone.GetComponent<TouchDamage>();
            Assert.That(touchDamage, Is.Not.Null, "Spawned water zone must have TouchDamage.");

            int initialHealth = _healthController.CurrentHealth;
            int expectedDamage = Mathf.RoundToInt(weaponDamage * 2f); // 15 * 2 = 30

            TriggerTouchDamageContact(touchDamage, _enemyCollider);

            Assert.That(_healthController.CurrentHealth, Is.EqualTo(initialHealth - expectedDamage),
                $"Spectral enemy health must be reduced by exactly Mathf.RoundToInt(DamageAmount * 2f) ({expectedDamage}).");
        }

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check 3:
        /// Spectral enemies DamageType.Normal wale TouchDamage instances ko discard karte hain aur health
        /// bilkul reduce nahi hoti, jabke SantaWaterWeapon ke Holy damage ko accept karke 2x damage lete hain.
        /// </summary>
        [Test]
        public void SpectralEnemy_DiscardsNormalTouchDamage_WhileAcceptingSantaWaterHolyDamage()
        {
            _enemyController.ConfigureDamageVulnerability(DamageType.Holy, 2f, isSpectral: true);
            Assert.That(_enemyController.IsSpectral, Is.True, "Enemy must be spectral (Level 7 rules).");

            int initialHealth = _healthController.CurrentHealth;

            // 1. Normal TouchDamage contact: Discarded without reducing health
            var normalTouch = CreateNormalTouchDamage(damage: 25f);
            TriggerTouchDamageContact(normalTouch, _enemyCollider);

            Assert.That(_healthController.CurrentHealth, Is.EqualTo(initialHealth),
                "Spectral enemy must discard Normal TouchDamage without taking any health damage.");

            // 2. Santa Water Holy TouchDamage contact: Accepted with 2x vulnerability multiplier
            const float santaDamage = 20f;
            var weapon = CreateSantaWaterWeapon(out _, damage: santaDamage);
            var waterZone = SpawnWaterZone(weapon, _enemyObject.transform.position);
            Assert.That(waterZone, Is.Not.Null, "SantaWater water zone must be spawned.");

            var santaTouch = waterZone.GetComponent<TouchDamage>();
            Assert.That(santaTouch, Is.Not.Null, "SantaWater water zone must have TouchDamage component.");

            int expectedHolyDamage = Mathf.RoundToInt(santaDamage * 2f); // 20 * 2 = 40
            TriggerTouchDamageContact(santaTouch, _enemyCollider);

            Assert.That(_healthController.CurrentHealth, Is.EqualTo(initialHealth - expectedHolyDamage),
                $"Spectral enemy must accept Santa Water Holy damage and reduce health by {expectedHolyDamage}.");
        }

        #endregion
    }
}
