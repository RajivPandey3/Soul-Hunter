using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Weapons;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment:
    /// ShadowKaelMirrorTests verifies that ShadowKaelController (the Level 10 Boss controller)
    /// correctly captures and mirrors the player's combat loadout at initialization (Start).
    /// As established in AGENTS.md Section 5 and Docs/Requirements/P1-07-reconciliation.md Section 3.3:
    /// 1. Player stats (Might, Cooldown, Area, MoveSpeedMultiplier) must be copied into
    ///    MirroredMight, MirroredCooldown, MirroredArea, and MirroredMoveSpeed.
    /// 2. Active weapon count and weapon levels from WeaponManager must be replicated onto
    ///    newly instantiated child AutoAttackWeapon instances.
    /// 3. Attached EnemyController must apply the mirrored movement speed via ApplyCampaignSpeed.
    /// 4. All instantiated GameObjects must be properly cleaned up in TearDown to prevent hierarchy leaks.
    /// 5. Passive defense stats (Armor, Revivals) must be mirrored into MirroredArmor and MirroredRevivals.
    /// 6. Attached HealthController must apply flat armor damage reduction via DamageModifier and handle death revivals.
    /// </summary>
    public class ShadowKaelMirrorTests
    {
        private GameObject _playerObject;
        private PlayerController _playerController;
        private PlayerStats _playerStats;
        private WeaponManager _weaponManager;

        private GameObject _shadowKaelObject;
        private ShadowKaelController _shadowKaelController;
        private EnemyController _enemyController;
        private HealthController _healthController;

        private readonly List<GameObject> _cleanupList = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            // Learning Comment:
            // Test fixture ke liye player setup karte hain jisme PlayerController, PlayerStats,
            // aur WeaponManager components mojood hon.
            _playerObject = new GameObject("TestPlayer");
            _cleanupList.Add(_playerObject);
            _playerController = _playerObject.AddComponent<PlayerController>();
            _playerStats = _playerObject.AddComponent<PlayerStats>();
            _weaponManager = _playerObject.AddComponent<WeaponManager>();

            // Learning Comment:
            // Shadow Kael boss GameObject create karte hain aur us par ShadowKaelController,
            // EnemyController, aur HealthController attach karte hain.
            _shadowKaelObject = new GameObject("TestShadowKael");
            _cleanupList.Add(_shadowKaelObject);
            _enemyController = _shadowKaelObject.AddComponent<EnemyController>();
            _healthController = _shadowKaelObject.AddComponent<HealthController>();
            _healthController.Initialize(100);
            _healthController.KnockbackResistance = 1f;
            _shadowKaelController = _shadowKaelObject.AddComponent<ShadowKaelController>();
        }

        [TearDown]
        public void TearDown()
        {
            // Learning Comment:
            // Har test ke baad scene clean karte hain taake hierarchy leak na ho.
            for (int i = _cleanupList.Count - 1; i >= 0; i--)
            {
                if (_cleanupList[i] != null)
                {
                    Object.DestroyImmediate(_cleanupList[i]);
                }
            }
            _cleanupList.Clear();
        }

        #region Helper Methods

        /// <summary>
        /// Learning Comment:
        /// EditMode mein Start() automatically run nahi hota, isliye reflection ke zariye
        /// ShadowKaelController.Start() ko invoke karte hain.
        /// </summary>
        private void InvokeStart(ShadowKaelController controller)
        {
            MethodInfo startMethod = typeof(ShadowKaelController).GetMethod("Start",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            startMethod?.Invoke(controller, null);
        }

        /// <summary>
        /// Learning Comment:
        /// WeaponManager ke private serialized weapon prefab field ko set karte hain.
        /// </summary>
        private void SetWeaponPrefab(WeaponManager manager, string fieldName, GameObject prefab)
        {
            FieldInfo field = typeof(WeaponManager).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(manager, prefab);
        }

        /// <summary>
        /// Learning Comment:
        /// Mock weapon prefab banate hain jisme AutoAttackWeapon component attach ho.
        /// </summary>
        private GameObject CreateMockWeaponPrefab(string name, int initialLevel = 1, float baseDamage = 10f, float baseCooldown = 1.5f)
        {
            var prefab = new GameObject(name);
            _cleanupList.Add(prefab);
            var weapon = prefab.AddComponent<AutoAttackWeapon>();
            weapon.CurrentLevel = initialLevel;
            weapon.DamageAmount = baseDamage;
            weapon.AttackCooldown = baseCooldown;
            return prefab;
        }

        #endregion

        #region Player Stats Mirroring Tests

        /// <summary>
        /// Learning Comment:
        /// Verify karta hai ke ShadowKaelController player ke PlayerStats se Might, Cooldown,
        /// Area, aur MoveSpeedMultiplier ko sahi tareeqe se mirror karta hai.
        /// </summary>
        [Test]
        public void Start_CapturesPlayerStats_IntoMirroredProperties()
        {
            // Custom stats set karte hain
            _playerStats.AddMight(0.5f); // 1.0 + 0.5 = 1.5
            _playerStats.ReduceCooldown(0.2f); // 1.0 - 0.2 = 0.8
            _playerStats.AddArea(0.4f); // 1.0 + 0.4 = 1.4
            _playerStats.AddMoveSpeed(0.3f); // 1.0 + 0.3 = 1.3
            _playerStats.AddArmor(5);
            _playerStats.AddRevival(2);

            InvokeStart(_shadowKaelController);

            Assert.That(_shadowKaelController.MirroredMight, Is.EqualTo(1.5f).Within(0.0001f),
                "MirroredMight should match PlayerStats.Might.");
            Assert.That(_shadowKaelController.MirroredCooldown, Is.EqualTo(0.8f).Within(0.0001f),
                "MirroredCooldown should match PlayerStats.Cooldown.");
            Assert.That(_shadowKaelController.MirroredArea, Is.EqualTo(1.4f).Within(0.0001f),
                "MirroredArea should match PlayerStats.Area.");
            Assert.That(_shadowKaelController.MirroredMoveSpeed, Is.EqualTo(1.3f).Within(0.0001f),
                "MirroredMoveSpeed should match PlayerStats.MoveSpeedMultiplier.");
            Assert.That(_shadowKaelController.MirroredArmor, Is.EqualTo(5),
                "MirroredArmor should match PlayerStats.Armor.");
            Assert.That(_shadowKaelController.MirroredRevivals, Is.EqualTo(2),
                "MirroredRevivals should match PlayerStats.Revivals.");
        }

        /// <summary>
        /// Learning Comment:
        /// Baseline / default player stats (1.0f) ka mirror hona test karte hain.
        /// </summary>
        [Test]
        public void Start_DefaultPlayerStats_MirroredAccurately()
        {
            InvokeStart(_shadowKaelController);

            Assert.That(_shadowKaelController.MirroredMight, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(_shadowKaelController.MirroredCooldown, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(_shadowKaelController.MirroredArea, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(_shadowKaelController.MirroredMoveSpeed, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(_shadowKaelController.MirroredArmor, Is.EqualTo(0));
            Assert.That(_shadowKaelController.MirroredRevivals, Is.EqualTo(0));
        }

        #endregion

        #region Weapon Mirroring Tests

        /// <summary>
        /// Learning Comment:
        /// Test karta hai ke jab player ke paas single weapon ho, toh ShadowKaelController
        /// MirroredWeaponCount ko 1 set kare aur child weapon instance par sahi level replicate kare.
        /// </summary>
        [Test]
        public void Start_CapturesWeaponCountAndReplicatesActiveWeaponLevel_SingleWeapon()
        {
            // MagicWand prefab setup karte hain
            var wandPrefab = CreateMockWeaponPrefab("MockMagicWand", initialLevel: 1, baseDamage: 10f, baseCooldown: 1.5f);
            SetWeaponPrefab(_weaponManager, "_magicWandObject", wandPrefab);

            // Player ko MagicWand level 3 dete hain
            const int targetLevel = 3;
            _weaponManager.GiveWeapon(UpgradeData.UpgradeType.MagicWand, targetLevel);

            InvokeStart(_shadowKaelController);

            // MirroredWeaponCount check karte hain
            Assert.That(_shadowKaelController.MirroredWeaponCount, Is.EqualTo(1),
                "Shadow Kael must capture active weapon count of 1.");

            // Child weapon instance check karte hain
            var childWeapon = _shadowKaelObject.GetComponentInChildren<AutoAttackWeapon>();
            Assert.That(childWeapon, Is.Not.Null,
                "Shadow Kael must instantiate a child weapon instance for active player weapon.");
            Assert.That(childWeapon.CurrentLevel, Is.EqualTo(targetLevel),
                "Child weapon instance must have its level replicated to match player weapon level.");
        }

        /// <summary>
        /// Learning Comment:
        /// Multiple weapons ke saath verify karte hain ke har weapon ka count aur level
        /// Shadow Kael par theek se replicate ho.
        /// </summary>
        [Test]
        public void Start_CapturesWeaponCountAndReplicatesActiveWeaponLevels_MultipleWeapons()
        {
            var wandPrefab = CreateMockWeaponPrefab("MockMagicWand", initialLevel: 1);
            var garlicPrefab = CreateMockWeaponPrefab("MockGarlic", initialLevel: 1);
            SetWeaponPrefab(_weaponManager, "_magicWandObject", wandPrefab);
            SetWeaponPrefab(_weaponManager, "_garlicWeaponObject", garlicPrefab);

            const int wandLevel = 4;
            const int garlicLevel = 2;
            _weaponManager.GiveWeapon(UpgradeData.UpgradeType.MagicWand, wandLevel);
            _weaponManager.GiveWeapon(UpgradeData.UpgradeType.Garlic, garlicLevel);

            InvokeStart(_shadowKaelController);

            Assert.That(_shadowKaelController.MirroredWeaponCount, Is.EqualTo(2),
                "Shadow Kael must mirror total active weapon count of 2.");

            var childWeapons = _shadowKaelObject.GetComponentsInChildren<AutoAttackWeapon>();
            Assert.That(childWeapons.Length, Is.EqualTo(2),
                "Shadow Kael must instantiate exactly 2 child weapon instances.");

            var levels = new List<int>();
            foreach (var w in childWeapons)
            {
                levels.Add(w.CurrentLevel);
            }
            Assert.That(levels, Does.Contain(wandLevel), $"Replicated weapons must include level {wandLevel}.");
            Assert.That(levels, Does.Contain(garlicLevel), $"Replicated weapons must include level {garlicLevel}.");
        }

        /// <summary>
        /// Learning Comment:
        /// Player ke paas zero weapons hone par MirroredWeaponCount 0 hona chahiye aur koi child weapon na bane.
        /// </summary>
        [Test]
        public void Start_WithZeroActiveWeapons_MirroredWeaponCountIsZero()
        {
            InvokeStart(_shadowKaelController);

            Assert.That(_shadowKaelController.MirroredWeaponCount, Is.EqualTo(0));
            var childWeapons = _shadowKaelObject.GetComponentsInChildren<AutoAttackWeapon>();
            Assert.That(childWeapons.Length, Is.EqualTo(0));
        }

        /// <summary>
        /// Learning Comment:
        /// Player weapon mirroring ke waqt stats multipliers (Might, Cooldown, Area) bhi
        /// child weapon instance par sahi apply hote hain.
        /// </summary>
        [Test]
        public void Start_AppliesMirroredStatsMultipliers_ToInstantiatedWeapon()
        {
            var wandPrefab = CreateMockWeaponPrefab("MockMagicWand", initialLevel: 1, baseDamage: 10f, baseCooldown: 2f);
            SetWeaponPrefab(_weaponManager, "_magicWandObject", wandPrefab);

            _playerStats.AddMight(0.5f); // Might = 1.5
            _playerStats.ReduceCooldown(0.2f); // Cooldown = 0.8
            _playerStats.AddArea(0.4f); // Area = 1.4

            _weaponManager.GiveWeapon(UpgradeData.UpgradeType.MagicWand, 1);

            InvokeStart(_shadowKaelController);

            var childWeapon = _shadowKaelObject.GetComponentInChildren<AutoAttackWeapon>();
            Assert.That(childWeapon, Is.Not.Null);
            // Base level 1 damage = 10 * 1.5 = 15
            Assert.That(childWeapon.DamageAmount, Is.EqualTo(15f).Within(0.001f));
            // Base cooldown = 2 * 0.8 = 1.6
            Assert.That(childWeapon.AttackCooldown, Is.EqualTo(1.6f).Within(0.001f));
            // Local scale = Vector3.one * 1.4
            Assert.That(childWeapon.transform.localScale.x, Is.EqualTo(1.4f).Within(0.001f));
        }

        #endregion

        #region Enemy Movement Speed Integration Tests

        /// <summary>
        /// Learning Comment:
        /// Verify karta hai ke ShadowKaelController attached EnemyController par MirroredMoveSpeed
        /// ApplyCampaignSpeed ke zariye apply karta hai.
        /// </summary>
        [Test]
        public void Start_AppliesMirroredMoveSpeed_ToAttachedEnemyControllerViaApplyCampaignSpeed()
        {
            float baseSpeed = _enemyController.MoveSpeed; // Default fallback is 3f

            const float speedBonus = 0.5f;
            _playerStats.AddMoveSpeed(speedBonus); // MoveSpeedMultiplier becomes 1.5f

            InvokeStart(_shadowKaelController);

            float expectedSpeed = baseSpeed * 1.5f;
            Assert.That(_enemyController.MoveSpeed, Is.EqualTo(expectedSpeed).Within(0.001f),
                "Attached EnemyController must have its MoveSpeed scaled by MirroredMoveSpeed via ApplyCampaignSpeed.");
        }

        /// <summary>
        /// Learning Comment:
        /// Baseline player move speed (1.0f) par EnemyController ki move speed unchanged (1x) rehni chahiye.
        /// </summary>
        [Test]
        public void Start_DefaultMoveSpeed_PreservesBaseEnemySpeed()
        {
            float baseSpeed = _enemyController.MoveSpeed;

            InvokeStart(_shadowKaelController);

            Assert.That(_enemyController.MoveSpeed, Is.EqualTo(baseSpeed).Within(0.001f),
                "Default 1.0x move speed should retain baseline enemy move speed.");
        }

        #endregion

        #region ConfigureMirroredWeapon Targeting & Layer Inversion Tests

        /// <summary>
        /// Learning Comment:
        /// Test karta hai ke ConfigureMirroredWeapon root weapon GameObject aur uske tamaam
        /// nested child GameObjects ko recursively "Enemy" layer assign karta hai.
        /// Is se mirrored weapon player ke bajaye enemy layer hierarchy ka hissa banti hai.
        /// </summary>
        [Test]
        public void ConfigureMirroredWeapon_AssignsRootAndNestedChildGameObjects_ToEnemyLayer()
        {
            // Learning Comment:
            // Root weapon GameObject aur uske nested child aur grandchild GameObjects banate hain.
            var rootWeapon = new GameObject("MockWeaponRoot");
            _cleanupList.Add(rootWeapon);

            var childObject = new GameObject("MockWeaponChild");
            childObject.transform.SetParent(rootWeapon.transform);
            _cleanupList.Add(childObject);

            var nestedChildObject = new GameObject("MockWeaponGrandChild");
            nestedChildObject.transform.SetParent(childObject.transform);
            _cleanupList.Add(nestedChildObject);

            // Shuru mein kisi aur layer (Default: 0) par set karte hain
            rootWeapon.layer = 0;
            childObject.layer = 0;
            nestedChildObject.layer = 0;

            // Execute
            _shadowKaelController.ConfigureMirroredWeapon(rootWeapon);

            // Assert
            int expectedEnemyLayer = LayerMask.NameToLayer("Enemy");
            Assert.That(rootWeapon.layer, Is.EqualTo(LayerMask.NameToLayer("Enemy")),
                "Root weapon GameObject should be assigned to LayerMask.NameToLayer(\"Enemy\").");
            Assert.That(childObject.layer, Is.EqualTo(LayerMask.NameToLayer("Enemy")),
                "Nested child GameObject should be assigned to LayerMask.NameToLayer(\"Enemy\").");
            Assert.That(nestedChildObject.layer, Is.EqualTo(LayerMask.NameToLayer("Enemy")),
                "Deeply nested child GameObject should be assigned to LayerMask.NameToLayer(\"Enemy\").");
            Assert.That(rootWeapon.layer, Is.EqualTo(expectedEnemyLayer));
            Assert.That(childObject.layer, Is.EqualTo(expectedEnemyLayer));
            Assert.That(nestedChildObject.layer, Is.EqualTo(expectedEnemyLayer));
        }

        /// <summary>
        /// Learning Comment:
        /// Test karta hai ke ConfigureMirroredWeapon weapon hierarchy par lage DamageCaster component
        /// ke TargetLayer ko Player layer mask par redirect karta hai.
        /// Is se Shadow Kael ke weapons enemies ke bajaye Player ko hit/damage karte hain.
        /// </summary>
        [Test]
        public void ConfigureMirroredWeapon_SetsDamageCasterTargetLayer_ToPlayerLayerMask()
        {
            // Learning Comment:
            // Mock weapon GameObject par DamageCaster component attach karte hain
            // aur shuru mein Enemy layer target karwate hain.
            var rootWeapon = new GameObject("MockDamageCasterWeapon");
            _cleanupList.Add(rootWeapon);

            var damageCaster = rootWeapon.AddComponent<DamageCaster>();
            damageCaster.TargetLayer = LayerMask.GetMask("Enemy");

            var childObject = new GameObject("MockDamageCasterChild");
            childObject.transform.SetParent(rootWeapon.transform);
            _cleanupList.Add(childObject);

            var childDamageCaster = childObject.AddComponent<DamageCaster>();
            childDamageCaster.TargetLayer = LayerMask.GetMask("Enemy");

            // Execute
            _shadowKaelController.ConfigureMirroredWeapon(rootWeapon);

            // Assert
            LayerMask expectedPlayerMask = LayerMask.GetMask("Player");
            Assert.That(damageCaster.TargetLayer, Is.EqualTo((LayerMask)LayerMask.GetMask("Player")),
                "DamageCaster.TargetLayer on root should be set to LayerMask.GetMask(\"Player\").");
            Assert.That(damageCaster.TargetLayer.value, Is.EqualTo(LayerMask.GetMask("Player")),
                "DamageCaster.TargetLayer.value on root should match LayerMask.GetMask(\"Player\").");
            Assert.That(damageCaster.TargetLayer, Is.EqualTo(expectedPlayerMask));

            Assert.That(childDamageCaster.TargetLayer, Is.EqualTo((LayerMask)LayerMask.GetMask("Player")),
                "DamageCaster.TargetLayer on nested child should be set to LayerMask.GetMask(\"Player\").");
            Assert.That(childDamageCaster.TargetLayer.value, Is.EqualTo(LayerMask.GetMask("Player")),
                "DamageCaster.TargetLayer.value on nested child should match LayerMask.GetMask(\"Player\").");
            Assert.That(childDamageCaster.TargetLayer, Is.EqualTo(expectedPlayerMask));
        }

        /// <summary>
        /// Learning Comment:
        /// Test karta hai ke ConfigureMirroredWeapon ProjectileDamage component ke TargetTag ko
        /// "Player" par reconfigure karta hai taake Shadow Kael ke projectiles Player ko nuksaan dein.
        /// </summary>
        [Test]
        public void ConfigureMirroredWeapon_SetsProjectileDamageTargetTag_ToPlayer()
        {
            // Learning Comment:
            // Mock weapon GameObject aur nested child par ProjectileDamage component attach karte hain
            // aur shuru mein TargetTag "Enemy" set karte hain.
            var rootWeapon = new GameObject("MockProjectileWeapon");
            _cleanupList.Add(rootWeapon);

            var projectileDamage = rootWeapon.AddComponent<ProjectileDamage>();
            projectileDamage.TargetTag = "Enemy";

            var childObject = new GameObject("MockProjectileChild");
            childObject.transform.SetParent(rootWeapon.transform);
            _cleanupList.Add(childObject);

            var childProjectileDamage = childObject.AddComponent<ProjectileDamage>();
            childProjectileDamage.TargetTag = "Enemy";

            // Execute
            _shadowKaelController.ConfigureMirroredWeapon(rootWeapon);

            // Assert
            Assert.That(projectileDamage.TargetTag, Is.EqualTo("Player"),
                "ProjectileDamage.TargetTag on root should be set to \"Player\".");
            Assert.That(childProjectileDamage.TargetTag, Is.EqualTo("Player"),
                "ProjectileDamage.TargetTag on nested child should be set to \"Player\".");
        }

        /// <summary>
        /// Learning Comment:
        /// Test karta hai ke ConfigureMirroredWeapon TouchDamage component ke TargetTag ko
        /// "Player" par set karta hai taake melee/touch contact par Player detect aur damage ho.
        /// </summary>
        [Test]
        public void ConfigureMirroredWeapon_SetsTouchDamageTargetTag_ToPlayer()
        {
            // Learning Comment:
            // Mock weapon GameObject aur nested child banate hain aur TouchDamage attach karte hain.
            // Shuru mein TargetTag "Enemy" karte hain verify karne ke liye ke ye "Player" par update ho.
            var rootWeapon = new GameObject("MockTouchDamageWeapon");
            _cleanupList.Add(rootWeapon);

            rootWeapon.AddComponent<BoxCollider>();
            var touchDamage = rootWeapon.AddComponent<TouchDamage>();
            touchDamage.TargetTag = "Enemy";

            var childObject = new GameObject("MockTouchDamageChild");
            childObject.transform.SetParent(rootWeapon.transform);
            _cleanupList.Add(childObject);

            childObject.AddComponent<BoxCollider>();
            var childTouchDamage = childObject.AddComponent<TouchDamage>();
            childTouchDamage.TargetTag = "Enemy";

            // Execute
            _shadowKaelController.ConfigureMirroredWeapon(rootWeapon);

            // Assert
            Assert.That(touchDamage.TargetTag, Is.EqualTo("Player"),
                "TouchDamage.TargetTag on root should be set to \"Player\".");
            Assert.That(childTouchDamage.TargetTag, Is.EqualTo("Player"),
                "TouchDamage.TargetTag on nested child should be set to \"Player\".");
        }

        /// <summary>
        /// Learning Comment:
        /// Test karta hai ke ConfigureMirroredWeapon GarlicWeapon aur WhipWeapon dono ke
        /// TargetLayer detection masks ko LayerMask.GetMask("Player") par set karta hai.
        /// Is tarah Garlic ka death aura aur Whip ka slash attack Player ko detect kar sakein.
        /// </summary>
        [Test]
        public void ConfigureMirroredWeapon_SetsGarlicWeaponAndWhipWeaponTargetLayer_ToPlayerLayerMask()
        {
            // Learning Comment:
            // Weapon root GameObject banate hain aur uske under GarlicWeapon aur WhipWeapon components attach karte hain.
            var rootWeapon = new GameObject("MockAoeWeaponsRoot");
            _cleanupList.Add(rootWeapon);

            var garlicChild = new GameObject("MockGarlicChild");
            garlicChild.transform.SetParent(rootWeapon.transform);
            _cleanupList.Add(garlicChild);
            var garlicWeapon = garlicChild.AddComponent<GarlicWeapon>();
            garlicWeapon.TargetLayer = LayerMask.GetMask("Enemy");

            var whipChild = new GameObject("MockWhipChild");
            whipChild.transform.SetParent(rootWeapon.transform);
            _cleanupList.Add(whipChild);
            var whipWeapon = whipChild.AddComponent<WhipWeapon>();
            whipWeapon.TargetLayer = LayerMask.GetMask("Enemy");

            // Execute
            _shadowKaelController.ConfigureMirroredWeapon(rootWeapon);

            // Assert
            LayerMask expectedPlayerMask = LayerMask.GetMask("Player");
            Assert.That(garlicWeapon.TargetLayer, Is.EqualTo((LayerMask)LayerMask.GetMask("Player")),
                "GarlicWeapon.TargetLayer should be set to LayerMask.GetMask(\"Player\").");
            Assert.That(garlicWeapon.TargetLayer.value, Is.EqualTo(LayerMask.GetMask("Player")),
                "GarlicWeapon.TargetLayer value should match LayerMask.GetMask(\"Player\").");
            Assert.That(garlicWeapon.TargetLayer, Is.EqualTo(expectedPlayerMask));

            Assert.That(whipWeapon.TargetLayer, Is.EqualTo((LayerMask)LayerMask.GetMask("Player")),
                "WhipWeapon.TargetLayer should be set to LayerMask.GetMask(\"Player\").");
            Assert.That(whipWeapon.TargetLayer.value, Is.EqualTo(LayerMask.GetMask("Player")),
                "WhipWeapon.TargetLayer value should match LayerMask.GetMask(\"Player\").");
            Assert.That(whipWeapon.TargetLayer, Is.EqualTo(expectedPlayerMask));
        }

        #endregion

        #region Passive Defense & Revival Mirroring Tests

        /// <summary>
        /// Learning Comment:
        /// Test karta hai ke Start() player ke passive stats (Armor aur Revivals) ko
        /// MirroredArmor aur MirroredRevivals mein PlayerStats ke mutabiq mirror karta hai.
        /// </summary>
        [Test]
        public void Start_CapturesPlayerPassiveStats_MirrorsArmorAndRevivalsMatchingPlayerStats()
        {
            // PlayerStats par custom Armor aur Revivals values configure karte hain
            const int expectedArmor = 6;
            const int expectedRevivals = 3;
            _playerStats.AddArmor(expectedArmor);
            _playerStats.AddRevival(expectedRevivals);

            // Execute Start()
            InvokeStart(_shadowKaelController);

            // Assertions
            Assert.That(_shadowKaelController.MirroredArmor, Is.EqualTo(expectedArmor),
                "Start() must mirror player passive Armor into MirroredArmor matching PlayerStats.");
            Assert.That(_shadowKaelController.MirroredRevivals, Is.EqualTo(expectedRevivals),
                "Start() must mirror player passive Revivals into MirroredRevivals matching PlayerStats.");
        }

        /// <summary>
        /// Learning Comment:
        /// Test karta hai ke ApplyArmorDamageReduction incoming damage ko MirroredArmor se reduce karta hai,
        /// aur agar incoming damage armor se barabar ya kam ho toh usay minimum 1 damage par clamp karta hai.
        /// </summary>
        [Test]
        public void ApplyArmorDamageReduction_ReducesIncomingDamageByArmor_AndClampsToMinimumOfOne()
        {
            _shadowKaelController.MirroredArmor = 5;

            // Jab incoming damage armor se zyada ho (20 - 5 = 15)
            int reducedDamage = _shadowKaelController.ApplyArmorDamageReduction(20);
            Assert.That(reducedDamage, Is.EqualTo(15),
                "ApplyArmorDamageReduction should reduce incoming damage by MirroredArmor when damage exceeds armor.");

            // Jab incoming damage exactly armor ke barabar ho (5 - 5 = 0 -> clamped to 1)
            int equalDamage = _shadowKaelController.ApplyArmorDamageReduction(5);
            Assert.That(equalDamage, Is.EqualTo(1),
                "ApplyArmorDamageReduction should clamp to minimum of 1 when incoming damage equals armor.");

            // Jab incoming damage armor se kam ho (3 - 5 = -2 -> clamped to 1)
            int belowDamage = _shadowKaelController.ApplyArmorDamageReduction(3);
            Assert.That(belowDamage, Is.EqualTo(1),
                "ApplyArmorDamageReduction should clamp to minimum of 1 when incoming damage is below armor.");

            // Jab incoming damage zero ho (0 - 5 = -5 -> clamped to 1)
            int zeroDamage = _shadowKaelController.ApplyArmorDamageReduction(0);
            Assert.That(zeroDamage, Is.EqualTo(1),
                "ApplyArmorDamageReduction should clamp to minimum of 1 when incoming damage is zero.");
        }

        /// <summary>
        /// Learning Comment:
        /// Test karta hai ke attached HealthController par ShadowKaelController Start() ke dauran
        /// DamageModifier wire karta hai, jo incoming damage par flat armor reduction lagata hai.
        /// </summary>
        [Test]
        public void TakeDamage_AttachedHealthController_AppliesFlatArmorDamageReductionViaDamageModifier()
        {
            // Player stats mein armor add karte hain taake Start() ke zariye MirroredArmor capture ho
            const int armorValue = 4;
            _playerStats.AddArmor(armorValue);

            // Start() invoke karne par _healthController.DamageModifier set ho jata hai
            InvokeStart(_shadowKaelController);

            Assert.That(_shadowKaelController.MirroredArmor, Is.EqualTo(armorValue));
            Assert.That(_healthController.DamageModifier, Is.Not.Null,
                "HealthController.DamageModifier must be configured by ShadowKaelController.Start().");

            _healthController.Initialize(100);

            // Case 1: Damage > Armor (Damage amount 20, expected reduction: 20 - 4 = 16 damage taken, remaining HP: 84)
            _healthController.TakeDamage(new DamagePacket(20, Vector3.zero, Vector3.zero, DamageType.Normal));
            Assert.That(_healthController.CurrentHealth, Is.EqualTo(84),
                "HealthController should apply flat armor damage reduction via DamageModifier (100 - (20 - 4) = 84).");

            // Case 2: Damage <= Armor (Damage amount 3 <= 4, clamped to min 1 damage taken, remaining HP: 83)
            _healthController.TakeDamage(new DamagePacket(3, Vector3.zero, Vector3.zero, DamageType.Normal));
            Assert.That(_healthController.CurrentHealth, Is.EqualTo(83),
                "HealthController should clamp damage to minimum of 1 via DamageModifier when incoming damage is less than armor.");
        }

        /// <summary>
        /// Learning Comment:
        /// Test karta hai ke jab Shadow Kael lethal damage leta hai, toh attached HealthController
        /// OnDied event fire karta hai, jisse HandleDeath trigger hota hai.
        /// HandleDeath MirroredRevivals ko decrement karta hai aur HealthController.ReviveFromDeath
        /// ko call karke health restore karta hai aur boss ko zinda (active) rakhta hai.
        /// </summary>
        [Test]
        public void TakeDamage_LethalDamage_TriggersHandleDeathToDecrementRevivalsAndRestoreHealthViaReviveFromDeath()
        {
            // Player stats mein 2 revivals add karte hain
            _playerStats.AddRevival(2);

            InvokeStart(_shadowKaelController);

            Assert.That(_shadowKaelController.MirroredRevivals, Is.EqualTo(2),
                "Precondition: MirroredRevivals should start at 2.");

            _healthController.Initialize(100);
            Assert.That(_healthController.CurrentHealth, Is.EqualTo(100));

            // Lethal damage packet deal karte hain (amount 500 exceeds 100 HP)
            _healthController.TakeDamage(new DamagePacket(500, Vector3.zero, Vector3.zero, DamageType.Normal));

            // Assertions:
            // 1. MirroredRevivals decrement ho kar 1 ho jana chahiye
            Assert.That(_shadowKaelController.MirroredRevivals, Is.EqualTo(1),
                "Lethal damage must trigger HandleDeath to decrement MirroredRevivals from 2 to 1.");

            // 2. HealthController.ReviveFromDeath health restore karta hai (50% of 100 = 50 HP)
            Assert.That(_healthController.CurrentHealth, Is.EqualTo(50),
                "HealthController.ReviveFromDeath must restore boss health on revival.");

            // 3. GameObject active rehna chahiye (Die() ne deactivate nahi kiya)
            Assert.That(_shadowKaelObject.activeSelf, Is.True,
                "Shadow Kael GameObject must remain active when revived from lethal damage.");
        }

        /// <summary>
        /// Learning Comment:
        /// Test karta hai ke jab MirroredRevivals zero ho (ya zero tak pohanch jaye),
        /// toh lethal damage standard death trigger karta hai aur GameObject deactivate (SetActive(false)) ho jata hai.
        /// </summary>
        [Test]
        public void TakeDamage_WhenMirroredRevivalsReachesZero_LethalDamageCausesStandardDeathAndDeactivatesGameObject()
        {
            // Player stats mein 0 revivals rakhte hain (default baseline)
            InvokeStart(_shadowKaelController);

            Assert.That(_shadowKaelController.MirroredRevivals, Is.EqualTo(0),
                "Precondition: MirroredRevivals must be 0.");

            _healthController.Initialize(100);
            Assert.That(_healthController.CurrentHealth, Is.EqualTo(100));
            Assert.That(_shadowKaelObject.activeSelf, Is.True);

            // Lethal damage deal karte hain
            _healthController.TakeDamage(new DamagePacket(500, Vector3.zero, Vector3.zero, DamageType.Normal));

            // Assertions:
            // 1. MirroredRevivals remains 0
            Assert.That(_shadowKaelController.MirroredRevivals, Is.EqualTo(0),
                "MirroredRevivals should remain 0.");

            // 2. Health remains 0 (not revived)
            Assert.That(_healthController.CurrentHealth, Is.EqualTo(0),
                "CurrentHealth should remain 0 on standard death.");

            // 3. GameObject must be deactivated (SetActive(false))
            Assert.That(_shadowKaelObject.activeSelf, Is.False,
                "When MirroredRevivals reaches zero, lethal damage must deactivate the Shadow Kael GameObject.");
        }

        /// <summary>
        /// Learning Comment:
        /// Test karta hai ke jab multiple revivals mojood hon toh consecutive lethal hits par
        /// pehle revivals decrement hotay hain aur aakhir mein zero pohanchne par standard death
        /// GameObject ko deactivate karti hai.
        /// </summary>
        [Test]
        public void TakeDamage_WhenMirroredRevivalsDepletedToZero_SubsequentLethalDamageCausesStandardDeathAndDeactivatesGameObject()
        {
            // Player stats mein 1 revival dete hain
            _playerStats.AddRevival(1);

            InvokeStart(_shadowKaelController);
            Assert.That(_shadowKaelController.MirroredRevivals, Is.EqualTo(1));

            _healthController.Initialize(100);

            // 1st lethal hit: Revival consume hoga, MirroredRevivals 0 banega aur health restore hogi
            _healthController.TakeDamage(new DamagePacket(500, Vector3.zero, Vector3.zero, DamageType.Normal));
            Assert.That(_shadowKaelController.MirroredRevivals, Is.EqualTo(0),
                "First lethal hit should consume revival and decrement MirroredRevivals to 0.");
            Assert.That(_healthController.CurrentHealth, Is.EqualTo(50),
                "Boss health should be revived to 50.");
            Assert.That(_shadowKaelObject.activeSelf, Is.True,
                "Boss GameObject should still be active.");

            // Post-revival invulnerability flag reset karte hain taake agla hit register ho sake
            _healthController.IsInvincible = false;

            // 2nd lethal hit: Revivals 0 hain, standard death hogi aur GameObject deactivate hoga
            _healthController.TakeDamage(new DamagePacket(500, Vector3.zero, Vector3.zero, DamageType.Normal));
            Assert.That(_shadowKaelController.MirroredRevivals, Is.EqualTo(0));
            Assert.That(_healthController.CurrentHealth, Is.EqualTo(0));
            Assert.That(_shadowKaelObject.activeSelf, Is.False,
                "Subsequent lethal hit with 0 revivals must trigger standard death and deactivate GameObject.");
        }

        #endregion
    }
}
