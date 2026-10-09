using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;
using SoulHunter.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS PowerUp On/Off in the real game (vampire.survivors.wiki/w/PowerUps, fetched 2026-10-09): at max the Buy button
    /// becomes the PowerUp's switch ("an option for the player to activate a PowerUp in most cases, except for Reroll,
    /// Skip, Banish, and Seal"); a switched-off cell is dark gray ("fully upgraded and not active") and says OFF; a run
    /// gets nothing from it; Refund still returns everything. Every save goes to a throw-away file.
    /// </summary>
    public class PowerUpTogglePlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator AMaxedPowerUp_TurnsOffAndOn_InTheShop_AndRefundStillReturnsEverything()
        {
            SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            var save = GameServices.Instance.Get<SaveService>();
            save.UseFile(System.IO.Path.Combine(Application.temporaryCachePath, "playmode_test_save.json"));
            var data = save.CurrentData;
            data.Currency = 50000;
            data.PowerUpRanks.Clear();
            data.DisabledPowerUps.Clear();
            PowerUpUnlocks.UnlockEverything(data);
            Object.FindFirstObjectByType<MainMenuNavigation>().ShowPowerUps();
            for (int i = 0; i < 3; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
            var shop = Object.FindFirstObjectByType<PowerUpMenuUI>();
            int gold = data.Currency;

            shop.Select(PowerUpId.Curse);
            Assert.That(shop.BuyButtonText, Is.EqualTo("Buy"), "Below max: Buy.");
            for (int i = 0; i < 5; i++) shop.BuyButton.onClick.Invoke();
            Assert.That(PowerUps.Rank(data, PowerUpId.Curse), Is.EqualTo(5));
            int paid = gold - data.Currency;
            Assert.That(shop.BuyButtonText, Is.EqualTo("Turn Off"), "At max: the switch.");
            Assert.That(shop.BuyButton.interactable, Is.True, "Switching is always possible, and free.");
            StringAssert.Contains("MAX - ON", shop.InfoText);
            var cell = shop.CellFor(PowerUpId.Curse);
            Assert.That(cell.image.color, Is.EqualTo(UiThemeAsset.Active.Cards.Maxed), "Maxed and on: the theme's maxed colour.");

            shop.BuyButton.onClick.Invoke();
            yield return null;
            Assert.That(PowerUps.IsEnabled(data, PowerUpId.Curse), Is.False);
            Assert.That(data.Currency, Is.EqualTo(gold - paid), "Switching off refunds nothing (VS).");
            Assert.That(shop.BuyButtonText, Is.EqualTo("Turn On"));
            StringAssert.Contains("MAX - OFF", shop.InfoText, "The state in words.");
            Assert.That(cell.image.color, Is.EqualTo(UiThemeAsset.Active.Cards.Off), "Maxed and off: the theme's dark gray.");
            var label = cell.GetComponentInChildren<TextMeshProUGUI>();
            StringAssert.Contains("OFF", label.text, "The cell says OFF, not colour alone.");
            Assert.That(UITheme.Contrast(label.color, cell.image.color), Is.GreaterThanOrEqualTo(4.5f), "The off cell's text reads.");
            var written = JsonUtility.FromJson<GameData>(System.IO.File.ReadAllText(save.FilePath));
            Assert.That(written.DisabledPowerUps, Is.EqualTo(new[] { "Curse" }), "Saved at once (to the throw-away file).");

            shop.BuyButton.onClick.Invoke();
            Assert.That(PowerUps.IsEnabled(data, PowerUpId.Curse), Is.True, "And on again.");
            shop.BuyButton.onClick.Invoke(); // off again for the refund

            // Reroll, Skip and Banish have no switch (wiki).
            PowerUps.SetRank(data, PowerUpId.Reroll, 5);
            shop.Select(PowerUpId.Reroll);
            Assert.That(shop.BuyButtonText, Is.EqualTo("Buy"));
            Assert.That(shop.BuyButton.interactable, Is.False, "Maxed Reroll: nothing to buy, nothing to switch.");
            StringAssert.Contains("MAX", shop.InfoText);
            PowerUps.SetRank(data, PowerUpId.Reroll, 0);

            shop.RefundButton.onClick.Invoke();
            yield return null;
            Assert.That(data.Currency, Is.EqualTo(gold), "Refund returns every rank's gold, the switched-off one too.");
            Assert.That(data.DisabledPowerUps, Is.Empty);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator ARun_GetsNothing_FromASwitchedOffPowerUp()
        {
            yield return BootIntoGameplay(save =>
            {
                save.PowerUpRanks.Clear();
                save.SoulEggs?.Clear();
                PowerUps.SetRank(save, PowerUpId.MaxHealth, 3);
                PowerUps.SetRank(save, PowerUpId.Curse, 5);
                PowerUps.SetRank(save, PowerUpId.Might, 5);
                PowerUps.SetEnabled(save, PowerUpId.MaxHealth, false);
                PowerUps.SetEnabled(save, PowerUpId.Curse, false);
            });
            var kael = SoulHunter.Gameplay.Data.GameContentCatalog.FindCharacterExact("Kael");
            var player = PlayerController.Instance;
            var health = player.GetComponent<HealthController>();
            Assert.That(health.MaxHealth, Is.EqualTo(kael.BaseMaxHealth), "Max Health off: no x1.331.");
            Assert.That(player.Stats.Curse, Is.EqualTo(kael.StartingCurse).Within(0.001f), "Curse off: no +50%.");
            Assert.That(player.Stats.Might, Is.EqualTo(kael.StartingMight + 0.25f).Within(0.001f), "Might, still on, applies.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator ARun_GetsTheSamePowerUp_WhenItIsOn()
        {
            yield return BootIntoGameplay(save =>
            {
                save.PowerUpRanks.Clear();
                save.SoulEggs?.Clear();
                PowerUps.SetRank(save, PowerUpId.MaxHealth, 3);
                PowerUps.SetRank(save, PowerUpId.Curse, 5);
            });
            var kael = SoulHunter.Gameplay.Data.GameContentCatalog.FindCharacterExact("Kael");
            var player = PlayerController.Instance;
            var health = player.GetComponent<HealthController>();
            Assert.That(health.MaxHealth, Is.EqualTo(Mathf.RoundToInt(kael.BaseMaxHealth * 1.331f)).Within(1), "Max Health on: x1.331.");
            Assert.That(player.Stats.Curse, Is.EqualTo(kael.StartingCurse + 0.5f).Within(0.001f), "Curse on: +50%.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
