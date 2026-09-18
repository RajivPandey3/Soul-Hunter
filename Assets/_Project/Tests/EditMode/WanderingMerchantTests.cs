using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment:
    /// WanderingMerchantTests verifies the transaction logic, health restoration mechanics,
    /// permanent stat blessing upgrades, and timescale pause/resume lifecycle of WanderingMerchant in EditMode.
    /// 
    /// Tested core contracts:
    /// 1. TryPurchase returns false when EconomyService is null without altering state.
    /// 2. TryPurchase returns false and preserves harvested souls balance and target health
    ///    when EconomyService.HarvestedSouls is less than Cost.
    /// 3. TryPurchase returns true, deducts exactly Cost souls from EconomyService, and
    ///    closes the shop (restoring Time.timeScale = 1f) when funds suffice.
    /// 4. TryPurchase applies HealAmount to target HealthController without exceeding MaxHealth.
    /// 5. OpenShop sets IsShopOpen to true and freezes Time.timeScale to 0f; CloseShop sets
    ///    IsShopOpen to false and restores Time.timeScale to 1f.
    /// 6. OnDisable and OnDestroy safely restore Time.timeScale to 1f if the merchant is
    ///    disabled or destroyed while the shop is open.
    /// 7. TryPurchaseBlessing returns false and preserves state when EconomyService is null across all BlessingType values.
    /// 8. TryPurchaseBlessing returns false and preserves harvested souls balance and stats when
    ///    EconomyService.HarvestedSouls is below blessing cost.
    /// 9. TryPurchaseBlessing for Might, Armor, and MaxHealth successfully deducts blessing cost,
    ///    increases PlayerStats (Might, Armor, MaxHealthBonus) and HealthController.MaxHealth by configured bonuses,
    ///    and closes the shop restoring Time.timeScale to 1f.
    /// 10. TryPurchaseBlessing returns false when target components (PlayerStats, or HealthController for MaxHealth) are missing.
    /// </summary>
    public class WanderingMerchantTests
    {
        private GameObject _merchantObject;
        private WanderingMerchant _merchant;
        private GameObject _playerObject;
        private HealthController _playerHealth;
        private PlayerStats _playerStats;
        private EconomyService _economyService;
        private readonly List<GameObject> _cleanupList = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            // Learning Comment:
            // Test start hone se pehle Time.timeScale ko baseline 1f par reset karte hain
            // aur fresh isolated GameObjects initialize karte hain.
            Time.timeScale = 1f;
            _cleanupList.Clear();

            // Merchant GameObject setup
            _merchantObject = new GameObject("TestWanderingMerchant");
            _cleanupList.Add(_merchantObject);
            // WanderingMerchant requires a concrete Collider. Unity cannot auto-create
            // the abstract Collider type declared by RequireComponent in EditMode.
            _merchantObject.AddComponent<BoxCollider>();
            _merchant = _merchantObject.AddComponent<WanderingMerchant>();

            // Target Player GameObject setup with HealthController and PlayerStats
            _playerObject = new GameObject("TestPlayer");
            _playerObject.tag = "Player";
            _cleanupList.Add(_playerObject);
            _playerHealth = _playerObject.AddComponent<HealthController>();
            _playerHealth.Initialize(100);
            _playerHealth.KnockbackResistance = 1f; // Avoid coroutine knockback side-effects in EditMode
            _playerStats = _playerObject.AddComponent<PlayerStats>();

            // Fresh EconomyService instance
            _economyService = new EconomyService();
        }

        [TearDown]
        public void TearDown()
        {
            // Learning Comment:
            // Test complete hone par hamesha Time.timeScale ko 1f restore karte hain
            // taake test failure ki soorat mein bhi Unity Editor frozen na rahe.
            Time.timeScale = 1f;

            for (int i = _cleanupList.Count - 1; i >= 0; i--)
            {
                if (_cleanupList[i] != null)
                {
                    Object.DestroyImmediate(_cleanupList[i]);
                }
            }
            _cleanupList.Clear();
        }

        #region Initialization & Defaults Tests

        /// <summary>
        /// Learning Comment:
        /// WanderingMerchant ke initial default configuration values verify karte hain:
        /// Cost = 50, HealAmount = 50, IsShopOpen = false.
        /// </summary>
        [Test]
        public void WanderingMerchant_DefaultConfiguration_MatchesExpectedValues()
        {
            Assert.That(_merchant.Cost, Is.EqualTo(50), "Default potion cost should be 50 souls.");
            Assert.That(_merchant.HealAmount, Is.EqualTo(50), "Default potion heal amount should be 50 HP.");
            Assert.That(_merchant.IsShopOpen, Is.False, "Shop should initially be closed.");
            Assert.That(_merchant.MightBlessingCost, Is.EqualTo(100), "Default Might blessing cost should be 100 souls.");
            Assert.That(_merchant.ArmorBlessingCost, Is.EqualTo(100), "Default Armor blessing cost should be 100 souls.");
            Assert.That(_merchant.MaxHealthBlessingCost, Is.EqualTo(100), "Default MaxHealth blessing cost should be 100 souls.");
            Assert.That(_merchant.MightBonus, Is.EqualTo(0.1f).Within(0.0001f), "Default Might bonus should be +10%.");
            Assert.That(_merchant.ArmorBonus, Is.EqualTo(1), "Default Armor bonus should be +1.");
            Assert.That(_merchant.MaxHealthBonus, Is.EqualTo(20), "Default MaxHealth bonus should be +20 HP.");
        }

        #endregion

        #region TryPurchase Null Economy Tests

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check:
        /// TryPurchase returns false when EconomyService is null.
        /// Shop open state aur target health ko mutate nahi hona chahiye.
        /// </summary>
        [Test]
        public void TryPurchase_NullEconomyService_ReturnsFalseAndPreservesShopState()
        {
            _merchant.OpenShop();
            Assert.That(_merchant.IsShopOpen, Is.True);
            int initialHealth = _playerHealth.CurrentHealth;

            bool result = _merchant.TryPurchase(economy: null, targetHealth: _playerHealth);

            Assert.That(result, Is.False, "TryPurchase must return false when EconomyService is null.");
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(initialHealth), "Target health must not change when economy is null.");
            Assert.That(_merchant.IsShopOpen, Is.True, "Shop must remain open when purchase fails due to null economy.");
        }

        #endregion

        #region TryPurchase Insufficient Funds Tests

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check:
        /// TryPurchase returns false and preserves souls balance and health when
        /// EconomyService.HarvestedSouls is less than Cost.
        /// </summary>
        [Test]
        public void TryPurchase_InsufficientSouls_ReturnsFalseAndPreservesSoulsAndHealth()
        {
            _merchant.OpenShop();
            const int initialSouls = 30; // Cost is 50, so 30 is insufficient
            _economyService.AddSouls(initialSouls);

            // Damage player so healing effect could otherwise be observed
            _playerHealth.TakeDamage(new DamagePacket(60, Vector3.zero, Vector3.zero, DamageType.Normal));
            int healthBeforePurchase = _playerHealth.CurrentHealth; // 40 HP
            Assert.That(healthBeforePurchase, Is.EqualTo(40));

            bool result = _merchant.TryPurchase(_economyService, _playerHealth);

            Assert.That(result, Is.False, "TryPurchase must return false when HarvestedSouls < Cost.");
            Assert.That(_economyService.HarvestedSouls, Is.EqualTo(initialSouls),
                "Harvested souls balance must be preserved when purchase fails.");
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(healthBeforePurchase),
                "Target health must remain unchanged when purchase fails.");
            Assert.That(_merchant.IsShopOpen, Is.True, "Shop must remain open when transaction fails.");
        }

        /// <summary>
        /// Learning Comment:
        /// Boundary tests for insufficient souls: 0 souls and (Cost - 1) souls.
        /// Dono cases mein TryPurchase false return kare aur balance preserve rahe.
        /// </summary>
        [TestCase(0)]
        [TestCase(49)]
        public void TryPurchase_BoundaryInsufficientSouls_ReturnsFalseAndPreservesState(int souls)
        {
            _merchant.OpenShop();
            if (souls > 0)
            {
                _economyService.AddSouls(souls);
            }

            _playerHealth.TakeDamage(new DamagePacket(50, Vector3.zero, Vector3.zero, DamageType.Normal));
            int healthBefore = _playerHealth.CurrentHealth;

            bool result = _merchant.TryPurchase(_economyService, _playerHealth);

            Assert.That(result, Is.False, $"TryPurchase must fail for {souls} souls when Cost is {_merchant.Cost}.");
            Assert.That(_economyService.HarvestedSouls, Is.EqualTo(souls), "Souls balance must remain unchanged.");
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(healthBefore), "Health must remain unchanged.");
            Assert.That(_merchant.IsShopOpen, Is.True, "Shop must stay open.");
        }

        #endregion

        #region TryPurchase Sufficient Funds Tests

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check:
        /// TryPurchase returns true, deducts exactly Cost souls from EconomyService,
        /// and closes the shop when funds suffice.
        /// </summary>
        [Test]
        public void TryPurchase_SufficientSouls_ReturnsTrueDeductsCostAndClosesShop()
        {
            _merchant.OpenShop();
            Assert.That(_merchant.IsShopOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            const int initialSouls = 120;
            _economyService.AddSouls(initialSouls);

            bool result = _merchant.TryPurchase(_economyService, _playerHealth);

            Assert.That(result, Is.True, "TryPurchase must return true when player has sufficient souls.");
            int expectedRemainingSouls = initialSouls - _merchant.Cost; // 120 - 50 = 70
            Assert.That(_economyService.HarvestedSouls, Is.EqualTo(expectedRemainingSouls),
                $"Exactly Cost ({_merchant.Cost}) souls must be deducted from EconomyService.");
            Assert.That(_merchant.IsShopOpen, Is.False, "Shop must be closed upon successful purchase.");
            Assert.That(Time.timeScale, Is.EqualTo(1f), "Time.timeScale must be restored to 1f when shop closes.");
        }

        /// <summary>
        /// Learning Comment:
        /// Boundary test: Player ke paas exactly Cost (50) souls mojood hon.
        /// Transaction succeed honi chahiye aur remaining balance exactly 0 hona chahiye.
        /// </summary>
        [Test]
        public void TryPurchase_ExactCostSouls_ReturnsTrueAndLeavesZeroSouls()
        {
            _merchant.OpenShop();
            _economyService.AddSouls(_merchant.Cost); // Exactly 50

            bool result = _merchant.TryPurchase(_economyService, _playerHealth);

            Assert.That(result, Is.True, "TryPurchase must succeed when souls exactly equal Cost.");
            Assert.That(_economyService.HarvestedSouls, Is.EqualTo(0), "Harvested souls must become exactly 0.");
            Assert.That(_merchant.IsShopOpen, Is.False, "Shop must close after purchase.");
            Assert.That(Time.timeScale, Is.EqualTo(1f), "Time.timeScale must be restored to 1f.");
        }

        #endregion

        #region Health Restoration Tests

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check:
        /// TryPurchase applies HealAmount to target HealthController.
        /// Example: Max 100, Current 30 -> +50 HP -> Current 80.
        /// </summary>
        [Test]
        public void TryPurchase_AppliesHealAmountToTargetHealthController()
        {
            _economyService.AddSouls(100);
            _playerHealth.TakeDamage(new DamagePacket(70, Vector3.zero, Vector3.zero, DamageType.Normal));
            int healthBeforePurchase = _playerHealth.CurrentHealth; // 30 HP
            Assert.That(healthBeforePurchase, Is.EqualTo(30));

            bool result = _merchant.TryPurchase(_economyService, _playerHealth);

            Assert.That(result, Is.True);
            int expectedHealth = healthBeforePurchase + _merchant.HealAmount; // 30 + 50 = 80
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(expectedHealth),
                $"CurrentHealth must increase by exactly HealAmount ({_merchant.HealAmount}).");
        }

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check:
        /// TryPurchase applies healing without exceeding MaxHealth.
        /// Example: Max 100, Current 80 -> +50 HP -> Capped at 100 (not 130).
        /// </summary>
        [Test]
        public void TryPurchase_HealingDoesNotExceedMaxHealth()
        {
            _economyService.AddSouls(100);
            _playerHealth.TakeDamage(new DamagePacket(20, Vector3.zero, Vector3.zero, DamageType.Normal));
            int healthBeforePurchase = _playerHealth.CurrentHealth; // 80 HP
            Assert.That(healthBeforePurchase, Is.EqualTo(80));

            bool result = _merchant.TryPurchase(_economyService, _playerHealth);

            Assert.That(result, Is.True);
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(_playerHealth.MaxHealth),
                $"Current health must be capped at MaxHealth ({_playerHealth.MaxHealth}) without exceeding it.");
        }

        /// <summary>
        /// Learning Comment:
        /// Edge case: Player already full health (100/100) par ho.
        /// Purchase succeed ho aur health MaxHealth par hi rahe.
        /// </summary>
        [Test]
        public void TryPurchase_FullHealth_RemainsAtMaxHealth()
        {
            _economyService.AddSouls(100);
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(_playerHealth.MaxHealth));

            bool result = _merchant.TryPurchase(_economyService, _playerHealth);

            Assert.That(result, Is.True);
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(_playerHealth.MaxHealth));
        }

        /// <summary>
        /// Learning Comment:
        /// Jab targetHealth parameter provide na kiya jaye (null), toh TryPurchase
        /// internal cached _playerHealth (jo OnTriggerEnter se capture hoti hai) ko heal karta hai.
        /// </summary>
        [Test]
        public void TryPurchase_WithoutTargetHealthArgument_HealsCachedPlayerHealth()
        {
            _playerHealth.TakeDamage(new DamagePacket(60, Vector3.zero, Vector3.zero, DamageType.Normal));
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(40));

            // Player collider simulation
            var playerCollider = _playerObject.AddComponent<BoxCollider>();
            MethodInfo triggerMethod = typeof(WanderingMerchant).GetMethod("OnTriggerEnter",
                BindingFlags.Instance | BindingFlags.NonPublic);
            triggerMethod?.Invoke(_merchant, new object[] { playerCollider });

            Assert.That(_merchant.IsShopOpen, Is.True, "OnTriggerEnter with Player should open shop.");

            _economyService.AddSouls(100);
            bool result = _merchant.TryPurchase(_economyService); // targetHealth defaults to null

            Assert.That(result, Is.True);
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(40 + _merchant.HealAmount),
                "Cached _playerHealth must receive healing when targetHealth is omitted.");
        }

        #endregion

        #region OpenShop & CloseShop Timescale Tests

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check:
        /// OpenShop sets IsShopOpen to true and freezes Time.timeScale to 0f.
        /// </summary>
        [Test]
        public void OpenShop_SetsIsShopOpenTrue_AndFreezesTimeScaleToZero()
        {
            Assert.That(_merchant.IsShopOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            _merchant.OpenShop();

            Assert.That(_merchant.IsShopOpen, Is.True, "OpenShop must set IsShopOpen to true.");
            Assert.That(Time.timeScale, Is.EqualTo(0f), "OpenShop must freeze Time.timeScale to 0f.");
        }

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check:
        /// CloseShop sets IsShopOpen to false and restores Time.timeScale to 1f.
        /// </summary>
        [Test]
        public void CloseShop_SetsIsShopOpenFalse_AndRestoresTimeScaleToOne()
        {
            _merchant.OpenShop();
            Assert.That(_merchant.IsShopOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            _merchant.CloseShop();

            Assert.That(_merchant.IsShopOpen, Is.False, "CloseShop must set IsShopOpen to false.");
            Assert.That(Time.timeScale, Is.EqualTo(1f), "CloseShop must restore Time.timeScale to 1f.");
        }

        #endregion

        #region Lifecycle Safety (OnDisable & OnDestroy) Tests

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check:
        /// OnDisable safely restores Time.timeScale to 1f and closes shop if merchant is disabled while shop is open.
        /// Isse stage transitions ya object pooling ke waqt game permanently pause nahi rehta.
        /// </summary>
        [Test]
        public void OnDisable_WhenShopIsOpen_RestoresTimeScaleToOneAndClosesShop()
        {
            _merchant.OpenShop();
            Assert.That(_merchant.IsShopOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            MethodInfo onDisableMethod = typeof(WanderingMerchant).GetMethod("OnDisable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(onDisableMethod, Is.Not.Null, "WanderingMerchant must define OnDisable.");
            onDisableMethod.Invoke(_merchant, null);

            Assert.That(_merchant.IsShopOpen, Is.False, "OnDisable must close shop when shop was open.");
            Assert.That(Time.timeScale, Is.EqualTo(1f), "OnDisable must restore Time.timeScale to 1f.");
        }

        /// <summary>
        /// Learning Comment:
        /// OnDisable jab shop band ho toh Time.timeScale ko 1f par hi barqarar rakhe.
        /// </summary>
        [Test]
        public void OnDisable_WhenShopIsClosed_LeavesTimeScaleAtOne()
        {
            Assert.That(_merchant.IsShopOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            MethodInfo onDisableMethod = typeof(WanderingMerchant).GetMethod("OnDisable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            onDisableMethod?.Invoke(_merchant, null);

            Assert.That(_merchant.IsShopOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check:
        /// OnDestroy safely restores Time.timeScale to 1f if merchant is destroyed while shop is open.
        /// Room unload ya sudden destruction par time freeze leak na ho.
        /// </summary>
        [Test]
        public void OnDestroy_WhenShopIsOpen_RestoresTimeScaleToOne()
        {
            _merchant.OpenShop();
            Assert.That(_merchant.IsShopOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            MethodInfo onDestroyMethod = typeof(WanderingMerchant).GetMethod("OnDestroy",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(onDestroyMethod, Is.Not.Null, "WanderingMerchant must define OnDestroy.");
            onDestroyMethod.Invoke(_merchant, null);

            Assert.That(Time.timeScale, Is.EqualTo(1f),
                "OnDestroy must restore Time.timeScale to 1f when merchant is destroyed while shop is open.");
        }

        /// <summary>
        /// Learning Comment:
        /// OnDestroy jab shop band ho toh Time.timeScale ko 1f par hi barqarar rakhe.
        /// </summary>
        [Test]
        public void OnDestroy_WhenShopIsClosed_LeavesTimeScaleAtOne()
        {
            Assert.That(_merchant.IsShopOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            MethodInfo onDestroyMethod = typeof(WanderingMerchant).GetMethod("OnDestroy",
                BindingFlags.Instance | BindingFlags.NonPublic);
            onDestroyMethod?.Invoke(_merchant, null);

            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        #endregion

        #region Trigger Collision Filter Tests

        /// <summary>
        /// Learning Comment:
        /// OnTriggerEnter sirf "Player" tag wale colliders par shop open kare.
        /// Non-player tags (Untagged, Enemy, etc.) par shop open nahi honi chahiye aur Time.timeScale 1f rahe.
        /// </summary>
        [Test]
        public void OnTriggerEnter_NonPlayerTag_DoesNotOpenShopOrFreezeTime()
        {
            var nonPlayerObj = new GameObject("TestEnemy");
            _cleanupList.Add(nonPlayerObj);
            nonPlayerObj.tag = "Untagged";
            var nonPlayerCollider = nonPlayerObj.AddComponent<BoxCollider>();

            MethodInfo triggerMethod = typeof(WanderingMerchant).GetMethod("OnTriggerEnter",
                BindingFlags.Instance | BindingFlags.NonPublic);
            triggerMethod?.Invoke(_merchant, new object[] { nonPlayerCollider });

            Assert.That(_merchant.IsShopOpen, Is.False, "Shop must not open for non-player colliders.");
            Assert.That(Time.timeScale, Is.EqualTo(1f), "Time.timeScale must remain 1f.");
        }

        #endregion

        #region TryPurchaseBlessing Null Economy Tests

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check:
        /// EconomyService null hone par TryPurchaseBlessing ko false return karna chahiye
        /// aur player stats, health, souls balance, aur merchant shop state bilkul preserve rehni chahiye
        /// (tamam BlessingType values ke liye: Might, Armor, MaxHealth).
        /// </summary>
        [TestCase(WanderingMerchant.BlessingType.Might)]
        [TestCase(WanderingMerchant.BlessingType.Armor)]
        [TestCase(WanderingMerchant.BlessingType.MaxHealth)]
        public void TryPurchaseBlessing_NullEconomyService_AcrossAllBlessingTypes_ReturnsFalseAndPreservesState(WanderingMerchant.BlessingType blessing)
        {
            _merchant.OpenShop();
            Assert.That(_merchant.IsShopOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            float initialMight = _playerStats.Might;
            int initialArmor = _playerStats.Armor;
            int initialMaxHealthBonus = _playerStats.MaxHealthBonus;
            int initialMaxHealth = _playerHealth.MaxHealth;
            int initialCurrentHealth = _playerHealth.CurrentHealth;

            bool result = _merchant.TryPurchaseBlessing(blessing, economy: null, targetStats: _playerStats, targetHealth: _playerHealth);

            Assert.That(result, Is.False, $"TryPurchaseBlessing must return false when EconomyService is null for {blessing}.");
            Assert.That(_merchant.IsShopOpen, Is.True, "Shop must remain open when purchase fails due to null economy.");
            Assert.That(Time.timeScale, Is.EqualTo(0f), "Time.timeScale must remain frozen at 0f when purchase fails.");
            Assert.That(_playerStats.Might, Is.EqualTo(initialMight), "Player might must not change when economy is null.");
            Assert.That(_playerStats.Armor, Is.EqualTo(initialArmor), "Player armor must not change when economy is null.");
            Assert.That(_playerStats.MaxHealthBonus, Is.EqualTo(initialMaxHealthBonus), "Player max health bonus must not change when economy is null.");
            Assert.That(_playerHealth.MaxHealth, Is.EqualTo(initialMaxHealth), "HealthController MaxHealth must not change when economy is null.");
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(initialCurrentHealth), "HealthController CurrentHealth must not change when economy is null.");
        }

        #endregion

        #region TryPurchaseBlessing Insufficient Funds Tests

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check:
        /// Jab EconomyService.HarvestedSouls blessing cost se kam hon, toh TryPurchaseBlessing false return kare,
        /// harvested souls deduct na hon, aur player stats, health, aur shop state bilkul preserve rahe.
        /// </summary>
        [TestCase(WanderingMerchant.BlessingType.Might, 50)]
        [TestCase(WanderingMerchant.BlessingType.Armor, 50)]
        [TestCase(WanderingMerchant.BlessingType.MaxHealth, 50)]
        public void TryPurchaseBlessing_InsufficientSouls_ReturnsFalseAndPreservesSoulsAndStats(
            WanderingMerchant.BlessingType blessing, int currentSouls)
        {
            _merchant.OpenShop();
            Assert.That(_merchant.IsShopOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            _economyService.AddSouls(currentSouls);
            Assert.That(_economyService.HarvestedSouls, Is.EqualTo(currentSouls));

            float initialMight = _playerStats.Might;
            int initialArmor = _playerStats.Armor;
            int initialMaxHealthBonus = _playerStats.MaxHealthBonus;
            int initialMaxHealth = _playerHealth.MaxHealth;
            int initialCurrentHealth = _playerHealth.CurrentHealth;

            bool result = _merchant.TryPurchaseBlessing(blessing, _economyService, _playerStats, _playerHealth);

            Assert.That(result, Is.False, $"TryPurchaseBlessing must return false when souls ({currentSouls}) are below cost for {blessing}.");
            Assert.That(_economyService.HarvestedSouls, Is.EqualTo(currentSouls),
                "Harvested souls balance must be preserved when purchase fails due to insufficient funds.");
            Assert.That(_playerStats.Might, Is.EqualTo(initialMight), "Player might must remain unchanged.");
            Assert.That(_playerStats.Armor, Is.EqualTo(initialArmor), "Player armor must remain unchanged.");
            Assert.That(_playerStats.MaxHealthBonus, Is.EqualTo(initialMaxHealthBonus), "MaxHealthBonus must remain unchanged.");
            Assert.That(_playerHealth.MaxHealth, Is.EqualTo(initialMaxHealth), "MaxHealth must remain unchanged.");
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(initialCurrentHealth), "CurrentHealth must remain unchanged.");
            Assert.That(_merchant.IsShopOpen, Is.True, "Shop must remain open when transaction fails.");
            Assert.That(Time.timeScale, Is.EqualTo(0f), "Time.timeScale must remain frozen at 0f.");
        }

        /// <summary>
        /// Learning Comment:
        /// Boundary tests: 0 souls aur (BlessingCost - 1) souls par TryPurchaseBlessing fail hona chahiye
        /// aur balance ya stats mutate nahi hone chahiye.
        /// </summary>
        [TestCase(WanderingMerchant.BlessingType.Might, 0)]
        [TestCase(WanderingMerchant.BlessingType.Might, 99)]
        [TestCase(WanderingMerchant.BlessingType.Armor, 0)]
        [TestCase(WanderingMerchant.BlessingType.Armor, 99)]
        [TestCase(WanderingMerchant.BlessingType.MaxHealth, 0)]
        [TestCase(WanderingMerchant.BlessingType.MaxHealth, 99)]
        public void TryPurchaseBlessing_BoundaryInsufficientSouls_ReturnsFalseAndPreservesState(
            WanderingMerchant.BlessingType blessing, int souls)
        {
            _merchant.OpenShop();
            if (souls > 0)
            {
                _economyService.AddSouls(souls);
            }

            bool result = _merchant.TryPurchaseBlessing(blessing, _economyService, _playerStats, _playerHealth);

            Assert.That(result, Is.False, $"TryPurchaseBlessing must fail for {souls} souls on {blessing}.");
            Assert.That(_economyService.HarvestedSouls, Is.EqualTo(souls), "Souls balance must not change.");
            Assert.That(_playerStats.Might, Is.EqualTo(1.0f));
            Assert.That(_playerStats.Armor, Is.EqualTo(0));
            Assert.That(_playerStats.MaxHealthBonus, Is.EqualTo(0));
            Assert.That(_playerHealth.MaxHealth, Is.EqualTo(100));
            Assert.That(_merchant.IsShopOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
        }

        #endregion

        #region TryPurchaseBlessing Success Tests

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check:
        /// TryPurchaseBlessing for Might successfully deducts blessing cost (_mightBlessingCost = 100),
        /// increases PlayerStats.Might by configured bonus (+0.1f = +10%), closes shop, and restores Time.timeScale to 1f.
        /// </summary>
        [Test]
        public void TryPurchaseBlessing_Might_DeductsCost_IncreasesMight_AndClosesShop()
        {
            _merchant.OpenShop();
            Assert.That(_merchant.IsShopOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            const int initialSouls = 150;
            _economyService.AddSouls(initialSouls);
            float initialMight = _playerStats.Might;

            bool result = _merchant.TryPurchaseBlessing(WanderingMerchant.BlessingType.Might, _economyService, _playerStats, _playerHealth);

            Assert.That(result, Is.True, "TryPurchaseBlessing must succeed when player has sufficient souls for Might.");
            int expectedSouls = initialSouls - _merchant.MightBlessingCost;
            Assert.That(_economyService.HarvestedSouls, Is.EqualTo(expectedSouls),
                $"Exactly MightBlessingCost ({_merchant.MightBlessingCost}) souls must be deducted.");
            Assert.That(_playerStats.Might, Is.EqualTo(initialMight + _merchant.MightBonus).Within(0.0001f),
                $"Player Might must increase by configured bonus ({_merchant.MightBonus}).");
            Assert.That(_merchant.IsShopOpen, Is.False, "Shop must close upon successful blessing purchase.");
            Assert.That(Time.timeScale, Is.EqualTo(1f), "Time.timeScale must be restored to 1f when shop closes.");
        }

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check:
        /// TryPurchaseBlessing for Armor successfully deducts blessing cost (_armorBlessingCost = 100),
        /// increases PlayerStats.Armor by configured bonus (+1), closes shop, and restores Time.timeScale to 1f.
        /// </summary>
        [Test]
        public void TryPurchaseBlessing_Armor_DeductsCost_IncreasesArmor_AndClosesShop()
        {
            _merchant.OpenShop();
            Assert.That(_merchant.IsShopOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            const int initialSouls = 150;
            _economyService.AddSouls(initialSouls);
            int initialArmor = _playerStats.Armor;

            bool result = _merchant.TryPurchaseBlessing(WanderingMerchant.BlessingType.Armor, _economyService, _playerStats, _playerHealth);

            Assert.That(result, Is.True, "TryPurchaseBlessing must succeed when player has sufficient souls for Armor.");
            int expectedSouls = initialSouls - _merchant.ArmorBlessingCost;
            Assert.That(_economyService.HarvestedSouls, Is.EqualTo(expectedSouls),
                $"Exactly ArmorBlessingCost ({_merchant.ArmorBlessingCost}) souls must be deducted.");
            Assert.That(_playerStats.Armor, Is.EqualTo(initialArmor + _merchant.ArmorBonus),
                $"Player Armor must increase by configured bonus ({_merchant.ArmorBonus}).");
            Assert.That(_merchant.IsShopOpen, Is.False, "Shop must close upon successful blessing purchase.");
            Assert.That(Time.timeScale, Is.EqualTo(1f), "Time.timeScale must be restored to 1f when shop closes.");
        }

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check:
        /// TryPurchaseBlessing for MaxHealth successfully deducts blessing cost (_maxHealthBlessingCost = 100),
        /// increases PlayerStats.MaxHealthBonus and HealthController.MaxHealth by configured bonus (+20),
        /// closes shop, and restores Time.timeScale to 1f.
        /// </summary>
        [Test]
        public void TryPurchaseBlessing_MaxHealth_DeductsCost_IncreasesMaxHealthBonusAndHealth_AndClosesShop()
        {
            _merchant.OpenShop();
            Assert.That(_merchant.IsShopOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            const int initialSouls = 150;
            _economyService.AddSouls(initialSouls);
            int initialBonus = _playerStats.MaxHealthBonus;
            int initialMaxHealth = _playerHealth.MaxHealth;

            bool result = _merchant.TryPurchaseBlessing(WanderingMerchant.BlessingType.MaxHealth, _economyService, _playerStats, _playerHealth);

            Assert.That(result, Is.True, "TryPurchaseBlessing must succeed when player has sufficient souls for MaxHealth.");
            int expectedSouls = initialSouls - _merchant.MaxHealthBlessingCost;
            Assert.That(_economyService.HarvestedSouls, Is.EqualTo(expectedSouls),
                $"Exactly MaxHealthBlessingCost ({_merchant.MaxHealthBlessingCost}) souls must be deducted.");
            Assert.That(_playerStats.MaxHealthBonus, Is.EqualTo(initialBonus + _merchant.MaxHealthBonus),
                $"PlayerStats.MaxHealthBonus must increase by configured bonus ({_merchant.MaxHealthBonus}).");
            Assert.That(_playerHealth.MaxHealth, Is.EqualTo(initialMaxHealth + _merchant.MaxHealthBonus),
                $"HealthController.MaxHealth must increase by configured bonus ({_merchant.MaxHealthBonus}).");
            Assert.That(_merchant.IsShopOpen, Is.False, "Shop must close upon successful blessing purchase.");
            Assert.That(Time.timeScale, Is.EqualTo(1f), "Time.timeScale must be restored to 1f when shop closes.");
        }

        /// <summary>
        /// Learning Comment:
        /// Boundary test: Player ke paas exactly blessing cost (100 souls) mojood hon.
        /// Transaction succeed ho aur remaining souls balance exactly 0 ho jaye.
        /// </summary>
        [TestCase(WanderingMerchant.BlessingType.Might)]
        [TestCase(WanderingMerchant.BlessingType.Armor)]
        [TestCase(WanderingMerchant.BlessingType.MaxHealth)]
        public void TryPurchaseBlessing_ExactCostSouls_ReturnsTrueAndLeavesZeroSouls(WanderingMerchant.BlessingType blessing)
        {
            _merchant.OpenShop();
            int cost = blessing switch
            {
                WanderingMerchant.BlessingType.Might => _merchant.MightBlessingCost,
                WanderingMerchant.BlessingType.Armor => _merchant.ArmorBlessingCost,
                WanderingMerchant.BlessingType.MaxHealth => _merchant.MaxHealthBlessingCost,
                _ => 100
            };
            _economyService.AddSouls(cost);

            bool result = _merchant.TryPurchaseBlessing(blessing, _economyService, _playerStats, _playerHealth);

            Assert.That(result, Is.True, $"TryPurchaseBlessing must succeed for exact cost ({cost}) on {blessing}.");
            Assert.That(_economyService.HarvestedSouls, Is.EqualTo(0), "Harvested souls must become exactly 0.");
            Assert.That(_merchant.IsShopOpen, Is.False, "Shop must close after purchase.");
            Assert.That(Time.timeScale, Is.EqualTo(1f), "Time.timeScale must be restored to 1f.");
        }

        /// <summary>
        /// Learning Comment:
        /// OnTriggerEnter ke baad jab cached player components use hon (targetStats / targetHealth omitted),
        /// tab bhi TryPurchaseBlessing properly succeed kare aur cached components par bonus apply ho.
        /// </summary>
        [Test]
        public void TryPurchaseBlessing_WithCachedPlayerComponents_Succeeds()
        {
            var playerCollider = _playerObject.AddComponent<BoxCollider>();
            MethodInfo triggerMethod = typeof(WanderingMerchant).GetMethod("OnTriggerEnter",
                BindingFlags.Instance | BindingFlags.NonPublic);
            triggerMethod?.Invoke(_merchant, new object[] { playerCollider });

            Assert.That(_merchant.IsShopOpen, Is.True);
            _economyService.AddSouls(100);

            bool result = _merchant.TryPurchaseBlessing(WanderingMerchant.BlessingType.Might, _economyService);

            Assert.That(result, Is.True);
            Assert.That(_economyService.HarvestedSouls, Is.EqualTo(0));
            Assert.That(_playerStats.Might, Is.EqualTo(1.1f).Within(0.0001f));
            Assert.That(_merchant.IsShopOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        #endregion

        #region TryPurchaseBlessing Missing Components Tests

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check:
        /// TryPurchaseBlessing returns false when target components are missing.
        /// Jab targetStats null ho aur targetHealth (ya uska parent) ke paas PlayerStats na ho,
        /// toh tamam blessings fail hon aur souls deduct na hon.
        /// </summary>
        [TestCase(WanderingMerchant.BlessingType.Might)]
        [TestCase(WanderingMerchant.BlessingType.Armor)]
        [TestCase(WanderingMerchant.BlessingType.MaxHealth)]
        public void TryPurchaseBlessing_MissingPlayerStats_ReturnsFalseAndPreservesSouls(WanderingMerchant.BlessingType blessing)
        {
            _merchant.OpenShop();
            Assert.That(_merchant.IsShopOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            const int initialSouls = 100;
            _economyService.AddSouls(initialSouls);

            // HealthController isolated object par jahan PlayerStats mojood nahi hai
            var standaloneHealthObj = new GameObject("StandaloneHealthObj");
            _cleanupList.Add(standaloneHealthObj);
            var standaloneHealth = standaloneHealthObj.AddComponent<HealthController>();
            standaloneHealth.Initialize(100);

            bool result = _merchant.TryPurchaseBlessing(blessing, _economyService, targetStats: null, targetHealth: standaloneHealth);

            Assert.That(result, Is.False, $"TryPurchaseBlessing must fail when PlayerStats is missing for {blessing}.");
            Assert.That(_economyService.HarvestedSouls, Is.EqualTo(initialSouls), "Souls must not be spent when components cannot be resolved.");
            Assert.That(standaloneHealth.MaxHealth, Is.EqualTo(100), "Health must not be modified when component resolution fails.");
            Assert.That(_merchant.IsShopOpen, Is.True, "Shop must remain open when resolution fails.");
            Assert.That(Time.timeScale, Is.EqualTo(0f), "Time.timeScale must remain frozen at 0f.");
        }

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check:
        /// TryPurchaseBlessing returns false when target components are missing:
        /// Dono targetStats aur targetHealth null hon (aur merchant ke paas koi cached reference na ho).
        /// </summary>
        [TestCase(WanderingMerchant.BlessingType.Might)]
        [TestCase(WanderingMerchant.BlessingType.Armor)]
        [TestCase(WanderingMerchant.BlessingType.MaxHealth)]
        public void TryPurchaseBlessing_BothComponentsNull_ReturnsFalseAndPreservesSouls(WanderingMerchant.BlessingType blessing)
        {
            _merchant.OpenShop();
            Assert.That(_merchant.IsShopOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            const int initialSouls = 100;
            _economyService.AddSouls(initialSouls);

            bool result = _merchant.TryPurchaseBlessing(blessing, _economyService, targetStats: null, targetHealth: null);

            Assert.That(result, Is.False, $"TryPurchaseBlessing must fail when both components are null for {blessing}.");
            Assert.That(_economyService.HarvestedSouls, Is.EqualTo(initialSouls), "Souls must not be spent when components are null.");
            Assert.That(_merchant.IsShopOpen, Is.True, "Shop must remain open.");
            Assert.That(Time.timeScale, Is.EqualTo(0f), "Time.timeScale must remain frozen at 0f.");
        }

        /// <summary>
        /// Learning Comment:
        /// Mandatory Acceptance Check:
        /// MaxHealth blessing ke liye targetHealth (HealthController) zaroori hai.
        /// Agar targetStats mojood ho lekin HealthController missing ho (targetStats ke object ya parent par na ho aur targetHealth null ho),
        /// toh MaxHealth blessing false return kare aur souls deduct na hon.
        /// </summary>
        [Test]
        public void TryPurchaseBlessing_MaxHealth_MissingHealthController_ReturnsFalseAndPreservesSouls()
        {
            _merchant.OpenShop();
            Assert.That(_merchant.IsShopOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            const int initialSouls = 100;
            _economyService.AddSouls(initialSouls);

            // Standalone PlayerStats object without HealthController
            var standaloneStatsObj = new GameObject("StandaloneStatsObj");
            _cleanupList.Add(standaloneStatsObj);
            var standaloneStats = standaloneStatsObj.AddComponent<PlayerStats>();

            bool result = _merchant.TryPurchaseBlessing(
                WanderingMerchant.BlessingType.MaxHealth,
                _economyService,
                targetStats: standaloneStats,
                targetHealth: null);

            Assert.That(result, Is.False, "TryPurchaseBlessing for MaxHealth must return false when HealthController is missing.");
            Assert.That(_economyService.HarvestedSouls, Is.EqualTo(initialSouls), "Souls must not be spent.");
            Assert.That(standaloneStats.MaxHealthBonus, Is.EqualTo(0), "MaxHealthBonus must not change.");
            Assert.That(_merchant.IsShopOpen, Is.True, "Shop must remain open.");
            Assert.That(Time.timeScale, Is.EqualTo(0f), "Time.timeScale must remain frozen at 0f.");
        }

        #endregion
    }
}
