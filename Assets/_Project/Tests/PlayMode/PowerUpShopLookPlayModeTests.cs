using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.UI;
using SoulHunter.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// The Power Up shop's look (owner, 2026-10-07: "haan sab fix karo, icons bhi banao", after a screenshot review): every
    /// PowerUp has an icon in the same slot; Back sits inside the screen's backing, clear of the title and the grid; the
    /// screen has a title and the gold in its header; the chosen cell wears the theme's frame; maxed and locked cells say
    /// so in words, and locked ones say what unlocks them; every cell's text reads at 4.5:1; the price says when the gold
    /// is not enough.
    /// </summary>
    public class PowerUpShopLookPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator TheShop_HasIconsTitleGoldFrameAndReadableStates_AndBackSitsInside()
        {
            SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            var save = GameServices.Instance.Get<SaveService>();
            save.UseFile(System.IO.Path.Combine(Application.temporaryCachePath, "playmode_test_save.json"));
            save.CurrentData.Currency = 50000;
            save.CurrentData.PowerUpRanks.Clear();
            save.CurrentData.DisabledPowerUps.Clear();
            save.CurrentData.UnlockedAchievements.Clear(); // Reroll, Skip, Banish, Omni, Charm, Defang, Preserve locked
            PowerUps.SetRank(save.CurrentData, PowerUpId.Recovery, 5);
            var navigation = Object.FindFirstObjectByType<MainMenuNavigation>();
            navigation.ShowPowerUps();
            for (int i = 0; i < 4; i++) { Canvas.ForceUpdateCanvases(); yield return null; }

            var shop = Object.FindFirstObjectByType<PowerUpMenuUI>();
            var theme = UiThemeAsset.Active;
            shop.Select(PowerUpId.Greed);
            yield return null;

            // Icons: one per PowerUp, all the same slot size.
            foreach (var info in PowerUps.All)
            {
                var icon = shop.CellFor(info.Id).transform.Find("Icon")?.GetComponent<Image>();
                Assert.That(icon != null && icon.sprite != null && icon.enabled, Is.True, $"{info.Id} shows an icon.");
                Assert.That(((RectTransform)icon.transform).rect.size, Is.EqualTo(new Vector2(64f, 64f)), $"{info.Id}'s icon has the one slot size.");
            }

            // Header: Back inside the backing, apart from the title and the grid; the title and the gold.
            var panel = navigation.PowerUpPanel.transform;
            // (Responsive since 2026-10-10: the parts sit inside the safe-area content, so they are found by name. The
            // grid is measured by its visible window: its rows scroll inside it.)
            Rect backing = ScreenRect((RectTransform)Deep(panel, "VS_PowerUpBacking"));
            Rect back = ScreenRect((RectTransform)navigation.PowerUpBackButton.transform);
            Rect grid = ScreenRect(shop.GridViewport);
            var title = Deep(panel, "VS_PowerUpTitle").GetComponent<TextMeshProUGUI>();
            Assert.That(backing.Contains(back.min) && backing.Contains(back.max), Is.True, $"Back {back} is inside the backing {backing}.");
            Assert.That(back.Overlaps(grid), Is.False, "Back is clear of the grid.");
            Assert.That(back.Overlaps(TextRect(title)), Is.False, "Back is clear of the title.");
            Assert.That(title.text, Is.EqualTo("Power Up"));
            Assert.That(Deep(panel, "VS_GoldRow").Find("Coin").GetComponent<Image>().sprite, Is.Not.Null, "The gold has its coin.");

            // The chosen cell wears the theme's frame; no other cell does.
            var chosen = shop.CellFor(PowerUpId.Greed).GetComponents<Outline>().First(o => !(o is ThemeFocusRing));
            Assert.That(chosen.effectColor, Is.EqualTo(theme.Cards.SelectedEdge));
            Assert.That(chosen.effectDistance.x, Is.EqualTo(theme.Cards.SelectedEdgeWidth));
            Assert.That(PowerUps.All.Count(i => shop.CellFor(i.Id).GetComponents<Outline>().Any(o => !(o is ThemeFocusRing) && o.enabled
                && o.effectColor == theme.Cards.SelectedEdge)), Is.EqualTo(1), "Only the chosen cell is framed.");

            // States in words, and readable.
            StringAssert.Contains("MAX", Label(shop, PowerUpId.Recovery).text, "A maxed PowerUp says MAX.");
            var locked = Label(shop, PowerUpId.Reroll).text;
            StringAssert.Contains("LOCKED", locked);
            StringAssert.Contains(PowerUpUnlocks.NextConditionText(save.CurrentData, PowerUpId.Reroll), locked, "A locked PowerUp says what unlocks it.");
            foreach (var info in PowerUps.All)
            {
                var cell = shop.CellFor(info.Id);
                float contrast = UITheme.Contrast(Label(shop, info.Id).color, cell.image.color);
                Assert.That(contrast, Is.GreaterThanOrEqualTo(4.5f), $"{info.Id}: its text reads at {contrast:0.0}:1 on its cell.");
            }

            // The price tells when the gold is not enough.
            save.CurrentData.Currency = 10;
            shop.Select(PowerUpId.Might);
            yield return null;
            StringAssert.Contains("need", shop.InfoText, "Too little gold: the price says how much more is needed.");
            Assert.That(shop.BuyButton.interactable, Is.False);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static Transform Deep(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        private static TextMeshProUGUI Label(PowerUpMenuUI shop, PowerUpId id) => shop.CellFor(id).GetComponentInChildren<TextMeshProUGUI>();

        private static Rect TextRect(TMP_Text text)
        {
            text.ForceMeshUpdate();
            var b = text.textBounds;
            Vector3 min = text.rectTransform.TransformPoint(b.min), max = text.rectTransform.TransformPoint(b.max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Rect ScreenRect(RectTransform t)
        {
            var corners = new Vector3[4];
            t.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
