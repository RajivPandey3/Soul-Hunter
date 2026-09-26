using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Level 3 Wandering Merchant, VS Merchant rule: weapons for 1,000 gold (never past the weapon slots),
    /// repeatable Soul Eggs for 10,000 gold, several buys per visit, and one visit only (leaving sends it away).
    /// Gold is a local counter here, so no save file is touched.
    /// </summary>
    public class WanderingMerchantTests
    {
        private readonly List<GameObject> _cleanup = new List<GameObject>();
        private WanderingMerchant _merchant;
        private WeaponManager _weapons;
        private int _gold;

        private bool Spend(int amount)
        {
            if (amount < 0 || _gold < amount) return false;
            _gold -= amount;
            return true;
        }

        [SetUp]
        public void SetUp()
        {
            SoulHunter.Core.Services.GameTime.ResetAll();
            var merchantObject = new GameObject("TestWanderingMerchant");
            _cleanup.Add(merchantObject);
            merchantObject.AddComponent<BoxCollider>(); // RequireComponent(Collider) cannot add the abstract type
            _merchant = merchantObject.AddComponent<WanderingMerchant>();

            var weaponObject = new GameObject("TestWeapons");
            _cleanup.Add(weaponObject);
            _weapons = weaponObject.AddComponent<WeaponManager>();
            _gold = 0;
        }

        [TearDown]
        public void TearDown()
        {
            SoulHunter.Core.Services.GameTime.ResetAll();
            foreach (var go in _cleanup) if (go != null) Object.DestroyImmediate(go);
            _cleanup.Clear();
        }

        private void Trigger(GameObject other)
        {
            typeof(WanderingMerchant).GetMethod("OnTriggerEnter", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(_merchant, new object[] { other.GetComponent<Collider>() });
        }

        private GameObject NewTagged(string tag)
        {
            var go = new GameObject("Toucher", typeof(SphereCollider)) { tag = tag };
            _cleanup.Add(go);
            return go;
        }

        [Test]
        public void Defaults_FollowTheVsMerchant()
        {
            Assert.That(_merchant.WeaponWares, Is.EquivalentTo(new[] { UpgradeData.UpgradeType.Bone, UpgradeData.UpgradeType.CherryBomb }));
            Assert.That(_merchant.WeaponPrice, Is.EqualTo(1000));
            Assert.That(_merchant.SoulEggPrice, Is.EqualTo(10000));
        }

        [Test]
        public void BuyWeapon_WithEnoughGold_GivesItAtLevelOne_ChargesThePrice_AndKeepsTheShopOpen()
        {
            _gold = 1500;
            _merchant.OpenShop();

            Assert.That(_merchant.TryBuyWeapon(UpgradeData.UpgradeType.Bone, Spend, _weapons), Is.True);

            Assert.That(_weapons.GetWeaponLevel(UpgradeData.UpgradeType.Bone), Is.EqualTo(1));
            Assert.That(_gold, Is.EqualTo(500));
            Assert.That(_merchant.IsShopOpen, Is.True, "VS: several things can be bought in one visit.");
            Assert.That(Time.timeScale, Is.EqualTo(0f));
        }

        [Test]
        public void BuyWeapon_WithoutEnoughGold_ChangesNothing()
        {
            _gold = 999;
            Assert.That(_merchant.TryBuyWeapon(UpgradeData.UpgradeType.Bone, Spend, _weapons), Is.False);
            Assert.That(_gold, Is.EqualTo(999));
            Assert.That(_weapons.GetWeaponLevel(UpgradeData.UpgradeType.Bone), Is.EqualTo(0));
            Assert.That(_merchant.WeaponState(UpgradeData.UpgradeType.Bone, _weapons, _gold), Is.EqualTo(WanderingMerchant.WareState.TooExpensive));
        }

        [Test]
        public void BuyWeapon_AlreadyOwned_IsNotChargedAgain()
        {
            _gold = 5000;
            _weapons.GiveWeapon(UpgradeData.UpgradeType.CherryBomb, 1);

            Assert.That(_merchant.TryBuyWeapon(UpgradeData.UpgradeType.CherryBomb, Spend, _weapons), Is.False);
            Assert.That(_gold, Is.EqualTo(5000));
            Assert.That(_merchant.WeaponState(UpgradeData.UpgradeType.CherryBomb, _weapons, _gold), Is.EqualTo(WanderingMerchant.WareState.Owned));
        }

        [Test]
        public void BuyWeapon_WithAllSixWeaponSlotsFull_IsRefused_AndNotCharged()
        {
            _gold = 5000;
            foreach (var type in new[] { UpgradeData.UpgradeType.Whip, UpgradeData.UpgradeType.Garlic, UpgradeData.UpgradeType.Axe,
                         UpgradeData.UpgradeType.Bible, UpgradeData.UpgradeType.Cross, UpgradeData.UpgradeType.MagicWand })
                _weapons.GiveWeapon(type, 1);
            Assert.That(_weapons.GetActiveWeaponsCount(), Is.EqualTo(6));

            Assert.That(_merchant.TryBuyWeapon(UpgradeData.UpgradeType.Bone, Spend, _weapons), Is.False);
            Assert.That(_gold, Is.EqualTo(5000));
            Assert.That(_weapons.GetWeaponLevel(UpgradeData.UpgradeType.Bone), Is.EqualTo(0));
            Assert.That(_merchant.WeaponState(UpgradeData.UpgradeType.Bone, _weapons, _gold), Is.EqualTo(WanderingMerchant.WareState.SlotsFull));
        }

        [Test]
        public void BuyWeapon_ThatTheMerchantDoesNotSell_IsRefused()
        {
            _gold = 5000;
            Assert.That(_merchant.TryBuyWeapon(UpgradeData.UpgradeType.Whip, Spend, _weapons), Is.False);
            Assert.That(_gold, Is.EqualTo(5000));
        }

        [Test]
        public void SoulEgg_IsRepeatable_ChargesEachTime_AndIsStoredForTheCharacter()
        {
            var data = new GameData();
            _gold = 25000;

            Assert.That(_merchant.TryBuySoulEgg(data, "Kael", Spend, new System.Random(7), out _), Is.True);
            Assert.That(_merchant.TryBuySoulEgg(data, "Kael", Spend, new System.Random(8), out _), Is.True);
            Assert.That(_merchant.TryBuySoulEgg(data, "Kael", Spend, new System.Random(9), out _), Is.False, "Only 5,000 gold left.");

            Assert.That(_gold, Is.EqualTo(5000));
            Assert.That(SoulEggs.For(data, "Kael").Eggs, Is.EqualTo(2));
            Assert.That(SoulEggs.For(data, "Corvan"), Is.Null, "Eggs belong to the character they were bought for.");
            Assert.That(_merchant.LastMessage, Does.StartWith("Soul Egg: "));
        }

        [Test]
        public void Touch_ByThePlayer_OpensTheShop_AndPauses_OtherObjectsDoNot()
        {
            Trigger(NewTagged("Untagged"));
            Assert.That(_merchant.IsShopOpen, Is.False);

            Trigger(NewTagged("Player"));
            Assert.That(_merchant.IsShopOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
        }

        [Test]
        public void Leave_ClosesTheShop_ResumesTheGame_AndTheMerchantNeverOpensAgain()
        {
            _merchant.OpenShop();
            _merchant.Leave();

            Assert.That(_merchant.IsShopOpen, Is.False);
            Assert.That(_merchant.HasLeft, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            Trigger(NewTagged("Player"));
            _merchant.OpenShop();
            Assert.That(_merchant.IsShopOpen, Is.False, "VS: the merchant can be visited once.");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void DisablingOrDestroying_AnOpenShop_NeverLeavesTheGamePaused()
        {
            _merchant.OpenShop();
            typeof(WanderingMerchant).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_merchant, null);
            Assert.That(_merchant.IsShopOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            _merchant.OpenShop();
            typeof(WanderingMerchant).GetMethod("OnDestroy", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_merchant, null);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void ApprovedModel_IsTheCatalogMerchant_WithTriggerShopAndGlowingLantern()
        {
            var prefab = SoulHunter.Gameplay.Data.GameContentCatalog.Load().WanderingMerchant;
            Assert.IsNotNull(prefab, "GameContentCatalog.WanderingMerchant is set (MerchantModelImporter).");
            Assert.IsNotNull(prefab.GetComponent<WanderingMerchant>());
            var trigger = prefab.GetComponent<CapsuleCollider>();
            Assert.That(trigger != null && trigger.isTrigger, Is.True, "Touching it opens the shop.");
            var model = prefab.transform.Find("SH10_NPC_WanderingMerchant");
            Assert.IsNotNull(model, "Owner-approved Blender model (2026-09-26).");
            var renderers = model.GetComponentsInChildren<Renderer>();
            var materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).ToList();
            Assert.That(materials.Count, Is.GreaterThanOrEqualTo(14));
            Assert.That(materials.All(m => m.shader.name == "Universal Render Pipeline/Lit"), Is.True);
            Assert.That(materials.Any(m => m.name.EndsWith("_Soul") && m.IsKeywordEnabled("_EMISSION")), Is.True, "The soul lantern glows.");
            Assert.That(renderers.All(r => r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On), Is.True);
        }
    }
}
