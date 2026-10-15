using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
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
        private readonly List<Object> _assets = new List<Object>();
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
            RunModes.Reset();
            foreach (var asset in _assets) if (asset != null) Object.DestroyImmediate(asset);
            _assets.Clear();
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

        // ---------------------------------------------------------------- Inverse mode wares
        // vampire.survivors.wiki/w/Merchant (raw, 2026-10-09): Skip 100, Banish 500, Reroll 1,000, Pick a Card 20,000, all
        // "Inverse mode only"; charges "can be purchased repeatedly until the player has 20 ... Can only buy once if the
        // character already start with 20". Stages, Patch 1.0: "Skip, Banish, and Reroll (can carry up to 20) and 1 extra Arcana".

        private LevelUpManager NewLevelUp()
        {
            var go = new GameObject("TestLevelUp");
            _cleanup.Add(go);
            return go.AddComponent<LevelUpManager>();
        }

        private ArcanaManager NewArcanas(int count)
        {
            var go = new GameObject("TestArcanas");
            _cleanup.Add(go);
            var manager = go.AddComponent<ArcanaManager>();
            var arcanas = new List<ArcanaData>();
            for (int i = 0; i < count; i++)
            {
                var arcana = ScriptableObject.CreateInstance<ArcanaData>();
                _assets.Add(arcana);
                arcana.CardNumber = (i + 1).ToString();
                arcana.CardName = "Test Arcana " + i;
                arcana.Type = ArcanaData.ArcanaType.IronBlueWill;
                arcanas.Add(arcana);
            }
            manager.SetAvailableArcanas(arcanas);
            return manager;
        }

        [Test]
        public void InverseWares_HaveTheVsPrices_AndTheCapOf20()
        {
            Assert.That(_merchant.SkipPrice, Is.EqualTo(100));
            Assert.That(_merchant.BanishPrice, Is.EqualTo(500));
            Assert.That(_merchant.RerollPrice, Is.EqualTo(1000));
            Assert.That(_merchant.ArcanaPrice, Is.EqualTo(20000));
            Assert.That(_merchant.ChargePrice(WanderingMerchant.ChargeWare.Skip), Is.EqualTo(100));
            Assert.That(_merchant.ChargePrice(WanderingMerchant.ChargeWare.Banish), Is.EqualTo(500));
            Assert.That(_merchant.ChargePrice(WanderingMerchant.ChargeWare.Reroll), Is.EqualTo(1000));
            Assert.That(WanderingMerchant.MaxRunCharges, Is.EqualTo(20));
            Assert.That(WanderingMerchant.ChargeWareName(WanderingMerchant.ChargeWare.Skip), Is.EqualTo("Skip Sigil"), "Soul Hunter's own ware name.");
            Assert.That(WanderingMerchant.ChargeWareName(WanderingMerchant.ChargeWare.Banish), Is.EqualTo("Banish Sigil"));
            Assert.That(WanderingMerchant.ChargeWareName(WanderingMerchant.ChargeWare.Reroll), Is.EqualTo("Reroll Sigil"));
        }

        [Test]
        public void WithoutInverseMode_NoInverseWareIsSold_AndNothingIsCharged()
        {
            RunModes.Inverse = false;
            var levelUp = NewLevelUp();
            var arcanas = NewArcanas(3);
            _gold = 50000;
            _merchant.OpenShop();

            Assert.That(WanderingMerchant.SellsInverseWares, Is.False);
            foreach (WanderingMerchant.ChargeWare ware in System.Enum.GetValues(typeof(WanderingMerchant.ChargeWare)))
                Assert.That(_merchant.TryBuyCharge(ware, levelUp, Spend), Is.False, $"{ware} is Inverse mode only.");
            Assert.That(_merchant.TryBuyArcana(arcanas, Spend, 0.5f), Is.False, "Pick a Card is Inverse mode only.");
            Assert.That(_gold, Is.EqualTo(50000));
            Assert.That(levelUp.RerollsLeft + levelUp.SkipsLeft + levelUp.BanishesLeft, Is.EqualTo(0));
            Assert.That(arcanas.ActiveArcanas, Is.Empty);
        }

        [Test]
        public void InverseMode_Charges_AreRepeatable_PlusOneEach_UntilTwentyAreHeld()
        {
            RunModes.Inverse = true;
            var levelUp = NewLevelUp();
            levelUp.AddCharges(0, 17, 0);
            _gold = 10000;
            _merchant.OpenShop();

            Assert.That(WanderingMerchant.SellsInverseWares, Is.True);
            Assert.That(_merchant.TryBuyCharge(WanderingMerchant.ChargeWare.Skip, levelUp, Spend), Is.True);
            Assert.That(levelUp.SkipsLeft, Is.EqualTo(18));
            Assert.That(_gold, Is.EqualTo(9900), "Skip costs 100.");
            Assert.That(_merchant.TryBuyCharge(WanderingMerchant.ChargeWare.Skip, levelUp, Spend), Is.True);
            Assert.That(_merchant.TryBuyCharge(WanderingMerchant.ChargeWare.Skip, levelUp, Spend), Is.True);
            Assert.That(levelUp.SkipsLeft, Is.EqualTo(20));
            Assert.That(_merchant.ChargeState(WanderingMerchant.ChargeWare.Skip, levelUp, _gold), Is.EqualTo(WanderingMerchant.WareState.Full));
            Assert.That(_merchant.TryBuyCharge(WanderingMerchant.ChargeWare.Skip, levelUp, Spend), Is.False, "20 Skips held: no more.");
            Assert.That(levelUp.SkipsLeft, Is.EqualTo(20));
            Assert.That(_gold, Is.EqualTo(9700), "A refused purchase costs nothing.");

            Assert.That(_merchant.TryBuyCharge(WanderingMerchant.ChargeWare.Banish, levelUp, Spend), Is.True);
            Assert.That(_merchant.TryBuyCharge(WanderingMerchant.ChargeWare.Reroll, levelUp, Spend), Is.True);
            Assert.That(levelUp.BanishesLeft, Is.EqualTo(1));
            Assert.That(levelUp.RerollsLeft, Is.EqualTo(1));
            Assert.That(_gold, Is.EqualTo(9700 - 500 - 1000));
            Assert.That(_merchant.LastMessage, Does.Contain("Reroll Sigil"));
        }

        [Test]
        public void InverseMode_AShopOpenedWithTwentyHeld_SellsExactlyOne()
        {
            RunModes.Inverse = true;
            var levelUp = NewLevelUp();
            levelUp.AddCharges(20, 0, 0);
            _gold = 5000;
            _merchant.OpenShop();

            Assert.That(_merchant.ChargeState(WanderingMerchant.ChargeWare.Reroll, levelUp, _gold), Is.EqualTo(WanderingMerchant.WareState.Available));
            Assert.That(_merchant.TryBuyCharge(WanderingMerchant.ChargeWare.Reroll, levelUp, Spend), Is.True, "VS: can buy once when starting with 20.");
            Assert.That(levelUp.RerollsLeft, Is.EqualTo(21));
            Assert.That(_merchant.TryBuyCharge(WanderingMerchant.ChargeWare.Reroll, levelUp, Spend), Is.False, "Only once.");
            Assert.That(levelUp.RerollsLeft, Is.EqualTo(21));
            Assert.That(_gold, Is.EqualTo(4000));

            Assert.That(WanderingMerchant.ChargeAllowed(19, 0, false), Is.True);
            Assert.That(WanderingMerchant.ChargeAllowed(20, 19, true), Is.False);
            Assert.That(WanderingMerchant.ChargeAllowed(20, 20, false), Is.True);
            Assert.That(WanderingMerchant.ChargeAllowed(21, 20, true), Is.False);
        }

        [Test]
        public void EndlessMode_SellsARevival_InsteadOfTheSoulEgg_OncePerVisit()
        {
            // VS wiki Stages (Endless): the merchant "sells '+1 Revival' instead of Golden Eggs". Price: balance method, 10,000.
            var statsObject = new GameObject("TestStats");
            try
            {
                var stats = statsObject.AddComponent<PlayerStats>();
                var data = new GameData();
                _gold = 25000;
                Assert.That(_merchant.TryBuyRevival(stats, Spend), Is.False, "No Revival ware outside Endless.");
                Assert.That(_merchant.RevivalState(_gold), Is.EqualTo(WanderingMerchant.WareState.SoldOut));

                RunModes.Endless = true;
                Assert.That(_merchant.TryBuySoulEgg(data, "Kael", Spend, new System.Random(1), out _), Is.False, "No Soul Egg in Endless.");
                Assert.That(_merchant.RevivalPrice, Is.EqualTo(10000));
                int before = stats.Revivals;
                Assert.That(_merchant.TryBuyRevival(stats, Spend), Is.True);
                Assert.That(stats.Revivals, Is.EqualTo(before + 1));
                Assert.That(_gold, Is.EqualTo(15000));
                Assert.That(_merchant.TryBuyRevival(stats, Spend), Is.False, "One per visit.");
                Assert.That(_gold, Is.EqualTo(15000));
            }
            finally { Object.DestroyImmediate(statsObject); }
        }

        [Test]
        public void InverseMode_DrawAFate_Costs20000_GivesAnUnheldArcana_OncePerVisit()
        {
            RunModes.Inverse = true;
            var arcanas = NewArcanas(4);
            _gold = 19999;
            _merchant.OpenShop();

            Assert.That(_merchant.ArcanaState(arcanas, _gold), Is.EqualTo(WanderingMerchant.WareState.TooExpensive));
            Assert.That(_merchant.TryBuyArcana(arcanas, Spend, 0.3f), Is.False);
            Assert.That(arcanas.ActiveArcanas, Is.Empty);

            _gold = 45000;
            Assert.That(_merchant.TryBuyArcana(arcanas, Spend, 0.3f), Is.True);
            Assert.That(arcanas.ActiveArcanas.Count, Is.EqualTo(1));
            Assert.That(_gold, Is.EqualTo(25000));
            Assert.That(_merchant.LastMessage, Does.Contain(arcanas.ActiveArcanas[0].CardName));
            Assert.That(_merchant.ArcanaState(arcanas, _gold), Is.EqualTo(WanderingMerchant.WareState.SoldOut), "VS: 1 extra Arcana.");
            Assert.That(_merchant.TryBuyArcana(arcanas, Spend, 0.3f), Is.False);
            Assert.That(_gold, Is.EqualTo(25000));
        }

        [Test]
        public void InverseMode_DrawAFate_WithNoArcanaLeft_ChargesNothing()
        {
            RunModes.Inverse = true;
            var arcanas = NewArcanas(1);
            Assert.That(arcanas.GrantNextAvailableArcana(), Is.True, "The only Arcana is already held.");
            Assert.That(arcanas.AvailableCount, Is.EqualTo(0));
            _gold = 30000;

            Assert.That(_merchant.ArcanaState(arcanas, _gold), Is.EqualTo(WanderingMerchant.WareState.SoldOut));
            Assert.That(_merchant.TryBuyArcana(arcanas, Spend, 0.5f), Is.False);
            Assert.That(_gold, Is.EqualTo(30000));
        }

        [Test]
        public void WareColumns_FollowTheWidth_AndNarrowCardsGrowTaller()
        {
            // Responsive (owner, 2026-10-10): the columns come from the list's width, not the screen's shape.
            Assert.That(WanderingMerchant.WareColumns(3, 1330f, 12f), Is.EqualTo(1), "Three wares keep the approved one column.");
            Assert.That(WanderingMerchant.WareColumns(7, 1330f, 12f), Is.EqualTo(2), "Inverse mode's seven wares take two columns on a wide panel.");
            Assert.That(WanderingMerchant.WareColumns(7, 408f, 12f), Is.EqualTo(1), "A phone held upright shows one column.");
            Assert.That(WanderingMerchant.WareColumns(7, 2f * WanderingMerchant.MinWareWidth + 12f, 12f), Is.EqualTo(2));
            Assert.That(WanderingMerchant.WareColumns(7, 2f * WanderingMerchant.MinWareWidth + 11f, 12f), Is.EqualTo(1));
            Assert.That(WanderingMerchant.WareCardHeight(400f), Is.GreaterThan(WanderingMerchant.WareCardHeight(800f)),
                "A narrow card stacks its name and price over the description, so it is taller.");
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
            // Draw calls (2026-10-15): the 14 colours are baked into one mesh drawn with the shared PropMerged material;
            // PropMergedLook lists the original materials.
            Assert.That(renderers.Length, Is.EqualTo(1), "one mesh");
            Assert.That(renderers[0].sharedMaterial.shader.name, Is.EqualTo("SoulHunter/PropMerged"));
            var materials = renderers.SelectMany(r => r.GetComponent<SoulHunter.Gameplay.VFX.PropMergedLook>().Sources).Where(m => m != null).ToList();
            Assert.That(materials.Count, Is.GreaterThanOrEqualTo(14));
            Assert.That(materials.All(m => m.shader.name == "Universal Render Pipeline/Lit"), Is.True);
            Assert.That(materials.Any(m => m.name.EndsWith("_Soul") && m.IsKeywordEnabled("_EMISSION")), Is.True, "The soul lantern glows.");
            Assert.That(renderers.All(r => r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On), Is.True);
        }
    }
}
