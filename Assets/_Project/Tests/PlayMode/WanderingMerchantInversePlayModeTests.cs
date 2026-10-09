using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Level 3's Wandering Merchant in Inverse mode, in the real game (vampire.survivors.wiki/w/Merchant, raw, 2026-10-09):
    /// Skip 100, Banish 500, Reroll 1,000 ("Inverse mode only. Can be purchased repeatedly until the player has 20") and
    /// Pick a Card 20,000 ("Gives an extra Arcana for this run"), here the Skip / Banish / Reroll Sigils and Draw a Fate.
    /// Without Inverse mode none of them is sold. Every card stays on screen without overlap, and the keyboard moves only
    /// between the shop's buttons. Every test plays on a throw-away save (GameplayPlayModeTestBase).
    /// </summary>
    public class WanderingMerchantInversePlayModeTests : GameplayPlayModeTestBase
    {
        private static string RealSave => Path.Combine(Application.persistentDataPath, "soulhunter_save.json");

        private IEnumerator OpenStage3Shop(bool inverse, int gold)
        {
            yield return BootIntoGameplay(save =>
            {
                save.ModeInverse = inverse;
                save.Currency = gold;
            });
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner != null) spawner.enabled = false;
            LevelProgressionManager.Instance.StartStage(3);
            yield return WaitUntilRealtime(() => Object.FindFirstObjectByType<WanderingMerchant>() != null, "Level 3 to spawn the merchant");
            Object.FindFirstObjectByType<WanderingMerchant>().OpenShop();
            yield return null;
        }

        private static WanderingMerchant Merchant => Object.FindFirstObjectByType<WanderingMerchant>();

        private static Button Card(WanderingMerchant merchant, string name) =>
            merchant.ShopUI.GetComponentsInChildren<Button>()
                .FirstOrDefault(b => b.transform.Find("Name") is Transform t && t.GetComponent<TMPro.TextMeshProUGUI>().text == name);

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners); // an overlay canvas: world corners are screen pixels
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [UnityTest]
        public IEnumerator InverseMode_SellsSigilsAndDrawAFate_BuyingRaisesTheCharge_AndTheCapHolds()
        {
            byte[] realBefore = File.Exists(RealSave) ? File.ReadAllBytes(RealSave) : null;
            yield return OpenStage3Shop(inverse: true, gold: 50000);
            Assert.That(RunModes.Inverse, Is.True);
            var merchant = Merchant;
            Assert.That(merchant.IsShopOpen, Is.True, "The shop opened. " + Errors());

            // The wares: Bone, Cherry Bomb, Soul Egg, Draw a Fate and the three Sigils, plus Leave.
            foreach (var name in new[] { "Soul Egg", "Draw a Fate", "Reroll Sigil", "Skip Sigil", "Banish Sigil" })
                Assert.IsNotNull(Card(merchant, name), $"Inverse mode sells {name}.");
            Assert.That(merchant.ShopUI.GetComponentsInChildren<Button>().Length, Is.EqualTo(8), "7 wares and Leave.");

            // Every card on screen, inside the panel, none overlapping another or Leave.
            Canvas.ForceUpdateCanvases();
            yield return null;
            var scaler = merchant.ShopUI.GetComponent<CanvasScaler>();
            Assert.That(scaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.Expand), "The shop fits any window, like the chest screen.");
            var panel = ScreenRect((RectTransform)merchant.ShopUI.transform.Find("Panel"));
            var screen = new Rect(0f, 0f, Screen.width, Screen.height);
            var buttons = merchant.ShopUI.GetComponentsInChildren<Button>().ToList();
            var rects = buttons.Select(b => ScreenRect((RectTransform)b.transform)).ToList();
            for (int i = 0; i < rects.Count; i++)
            {
                Assert.That(screen.Contains(rects[i].min) && screen.Contains(rects[i].max - Vector2.one * 0.5f), Is.True, $"{buttons[i].name} {rects[i]} is off the {Screen.width}x{Screen.height} screen.");
                Assert.That(panel.Contains(rects[i].min) && panel.Contains(rects[i].max - Vector2.one * 0.5f), Is.True, $"{buttons[i].name} {rects[i]} leaves the panel {panel}.");
                for (int j = i + 1; j < rects.Count; j++)
                    Assert.That(rects[i].Overlaps(rects[j]), Is.False, $"Buttons {i} and {j} overlap: {rects[i]} / {rects[j]}.");
            }

            // Keyboard and controller: a shop button is focused and moves only to the shop's usable buttons.
            var selected = EventSystem.current.currentSelectedGameObject;
            Assert.That(selected != null && selected.transform.IsChildOf(merchant.ShopUI.transform), Is.True, "A shop button is focused.");
            foreach (var button in buttons.Where(b => b.interactable))
            {
                var nav = button.navigation;
                Assert.That(nav.mode, Is.EqualTo(Navigation.Mode.Explicit));
                foreach (var target in new[] { nav.selectOnUp, nav.selectOnDown, nav.selectOnLeft, nav.selectOnRight }.Where(t => t != null))
                    Assert.That(target.transform.IsChildOf(merchant.ShopUI.transform) && target.interactable, Is.True, $"{button.name} navigates out of the shop.");
            }
            var skipCard = Card(merchant, "Skip Sigil");
            var neighbours = new[] { skipCard.navigation.selectOnUp, skipCard.navigation.selectOnDown, skipCard.navigation.selectOnLeft, skipCard.navigation.selectOnRight };
            Assert.That(neighbours.Count(n => n != null), Is.GreaterThanOrEqualTo(2), "A card in the grid reaches its neighbours.");

            // Skip Sigil through its button: +1 Skip, 100 gold.
            var economy = GameServices.Instance.Get<EconomyService>();
            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            int skipsBefore = levelUp.SkipsLeft, goldBefore = economy.CurrentGold;
            Click(skipCard, "Skip Sigil");
            yield return null;
            Assert.That(levelUp.SkipsLeft, Is.EqualTo(skipsBefore + 1), "Skip Sigil gives +1 Skip. " + Errors());
            Assert.That(economy.CurrentGold, Is.EqualTo(goldBefore - 100), "VS price: 100.");

            // Repeatable while fewer than 20 are held; at 20 the merchant refuses and charges nothing.
            if (levelUp.SkipsLeft < WanderingMerchant.MaxRunCharges && skipsBefore < WanderingMerchant.MaxRunCharges - 1)
            {
                Assert.That(merchant.BuyCharge(WanderingMerchant.ChargeWare.Skip), Is.True, "Skips can be bought again.");
                Assert.That(economy.CurrentGold, Is.EqualTo(goldBefore - 200));
            }
            if (levelUp.SkipsLeft < WanderingMerchant.MaxRunCharges)
                levelUp.AddCharges(0, WanderingMerchant.MaxRunCharges - levelUp.SkipsLeft, 0);
            int capped = levelUp.SkipsLeft, goldAtCap = economy.CurrentGold;
            Assert.That(merchant.BuyCharge(WanderingMerchant.ChargeWare.Skip), Is.False, "20 Skips held: no more.");
            Assert.That(levelUp.SkipsLeft, Is.EqualTo(capped));
            Assert.That(economy.CurrentGold, Is.EqualTo(goldAtCap));
            yield return null;
            var fullCard = Card(merchant, "Skip Sigil");
            Assert.That(fullCard.interactable, Is.False, "The full Skip Sigil cannot be bought.");
            Assert.That(fullCard.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Any(t => t.text == "FULL"), Is.True, "It says FULL in words.");

            // Banish and Reroll at their VS prices.
            int banishes = levelUp.BanishesLeft, rerolls = levelUp.RerollsLeft, gold = economy.CurrentGold;
            Assert.That(merchant.BuyCharge(WanderingMerchant.ChargeWare.Banish), Is.True);
            Assert.That(merchant.BuyCharge(WanderingMerchant.ChargeWare.Reroll), Is.True);
            Assert.That(levelUp.BanishesLeft, Is.EqualTo(banishes + 1));
            Assert.That(levelUp.RerollsLeft, Is.EqualTo(rerolls + 1));
            Assert.That(economy.CurrentGold, Is.EqualTo(gold - 1500));

            // Draw a Fate: 20,000 gold, one more Arcana, once.
            var arcanas = ArcanaManager.Instance;
            Assert.IsNotNull(arcanas, "Main_Gameplay has an ArcanaManager.");
            int held = arcanas.ActiveArcanas.Count;
            gold = economy.CurrentGold;
            Click(Card(merchant, "Draw a Fate"), "Draw a Fate");
            yield return null;
            Assert.That(arcanas.ActiveArcanas.Count, Is.EqualTo(held + 1), "Draw a Fate gives an Arcana. " + Errors());
            Assert.That(economy.CurrentGold, Is.EqualTo(gold - 20000));
            Assert.That(merchant.BuyArcana(), Is.False, "VS: 1 extra Arcana.");
            Assert.That(economy.CurrentGold, Is.EqualTo(gold - 20000));
            Assert.That(merchant.ShopUI.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Any(t => t.text.Contains(arcanas.ActiveArcanas.Last().CardName)),
                Is.True, "The shop says which Arcana was drawn.");

            merchant.Leave();
            yield return null;
            Assert.That(GameTime.IsPaused, Is.False);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
            Assert.That(File.Exists(RealSave) ? File.ReadAllBytes(RealSave) : null, Is.EqualTo(realBefore), "The real save is never changed.");
        }

        [UnityTest]
        public IEnumerator WithoutInverseMode_TheMerchantSellsNoSigilsOrFate()
        {
            byte[] realBefore = File.Exists(RealSave) ? File.ReadAllBytes(RealSave) : null;
            yield return OpenStage3Shop(inverse: false, gold: 50000);
            var merchant = Merchant;
            Assert.That(merchant.IsShopOpen, Is.True, "The shop opened. " + Errors());
            Assert.That(RunModes.Inverse, Is.False);
            var names = merchant.ShopUI.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Select(t => t.text).ToList();
            Assert.That(names.Any(t => t.Contains("Sigil") || t.Contains("Draw a Fate")), Is.False, "Inverse mode wares only in Inverse mode.");
            Assert.That(merchant.ShopUI.GetComponentsInChildren<Button>().Length, Is.EqualTo(4), "Bone, Cherry Bomb, Soul Egg and Leave.");

            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            var economy = GameServices.Instance.Get<EconomyService>();
            int skips = levelUp.SkipsLeft, gold = economy.CurrentGold;
            Assert.That(merchant.BuyCharge(WanderingMerchant.ChargeWare.Skip), Is.False);
            Assert.That(merchant.BuyArcana(), Is.False);
            Assert.That(levelUp.SkipsLeft, Is.EqualTo(skips));
            Assert.That(economy.CurrentGold, Is.EqualTo(gold));

            merchant.Leave();
            yield return null;
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
            Assert.That(File.Exists(RealSave) ? File.ReadAllBytes(RealSave) : null, Is.EqualTo(realBefore), "The real save is never changed.");
        }
    }
}
