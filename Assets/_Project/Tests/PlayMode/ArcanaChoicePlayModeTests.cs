using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// The VS Arcana choice in the real game (release item P2-01; vampire.survivors.wiki/w/Arcanas, raw, fetched 2026-10-09):
    /// Arcana mode opens the start pick of every unlocked Arcana (or a random one); an Arcana chest opens a choice of four
    /// unheld cards with Rerolls that never show a card twice; with three Arcanas held the Arcana elite's chest is a
    /// regular chest; the merchant's Draw a Fate offers every unlocked card. The screen is on screen and readable (4.5:1)
    /// at 1920x1080 and in a short, wide window. Every test plays on a throw-away save (GameplayPlayModeTestBase).
    /// </summary>
    public class ArcanaChoicePlayModeTests : GameplayPlayModeTestBase
    {
        private static readonly Vector2[] Windows = { new Vector2(1920, 1080), new Vector2(1366, 575), new Vector2(2560, 1080) };

        private static ArcanaChoiceScreen OpenScreen()
        {
            var screen = ArcanaChoiceScreen.Current;
            Assert.That(screen != null && screen.IsOpen, Is.True, "The Arcana choice is open.");
            return screen;
        }

        [UnityTest]
        public IEnumerator ArcanaMode_OpensTheStartPick_Paused_AndTakingACardAppliesItAndResumes()
        {
            yield return BootIntoGameplay(save => save.ModeArcana = true);
            var arcanas = ArcanaManager.Instance;
            var screen = OpenScreen();
            Assert.That(GameTime.IsPaused, Is.True, "The choice pauses the game.");
            Assert.That(arcanas.ActiveArcanas, Is.Empty, "Nothing is given before the pick.");
            Assert.That(screen.Draft.DraftKind, Is.EqualTo(ArcanaDraft.Kind.Start));
            Assert.That(screen.Cards.Count, Is.EqualTo(22), "Every unlocked Arcana (all 22 base cards).");
            Assert.That(screen.RandomButton.gameObject.activeInHierarchy, Is.True, "VS: 'the option to select a random one'.");
            Assert.That(screen.SkipButton.gameObject.activeInHierarchy, Is.False);
            Assert.That(screen.CountText.text, Does.Contain("22 / 22"));

            var wanted = screen.Cards.First(c => c.Arcana.Type == ArcanaData.ArcanaType.SilentOldSanctuary);
            Click(wanted.Button, wanted.Arcana.CardName);
            Assert.That(screen.Selected, Is.SameAs(wanted.Arcana));
            Assert.That(screen.InfoText.text, Is.EqualTo(wanted.Arcana.Description));
            Click(screen.TakeButton, "Take this card");
            yield return null;
            Assert.That(screen.IsOpen, Is.False);
            Assert.That(arcanas.ActiveArcanas, Is.EqualTo(new[] { wanted.Arcana }));
            Assert.That(arcanas.StartingArcana, Is.SameAs(wanted.Arcana));
            Assert.That(GameTime.IsPaused, Is.False, "Taking the card resumes the game. " + Errors());
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator TheChoiceScreen_FitsAnyWindow_AndEveryTextReads()
        {
            yield return BootIntoGameplay(save => save.ModeArcana = true);
            var screen = OpenScreen();
            var canvas = (RectTransform)screen.Canvas.transform;
            var scaler = screen.Canvas.GetComponent<CanvasScaler>();
            Assert.That(scaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.Expand));
            Canvas.ForceUpdateCanvases();

            var parts = new List<RectTransform> { (RectTransform)screen.TakeButton.transform, (RectTransform)screen.RandomButton.transform,
                screen.TitleText.rectTransform, screen.CountText.rectTransform, screen.InfoText.rectTransform, screen.WorksWithText.rectTransform };
            parts.AddRange(screen.Cards.Select(c => (RectTransform)c.Button.transform));
            var corners = new Vector3[4];
            foreach (var w in Windows)
            {
                float scale = Mathf.Min(w.x / scaler.referenceResolution.x, w.y / scaler.referenceResolution.y);
                Vector2 half = w / scale * 0.5f;
                foreach (var part in parts)
                {
                    part.GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        Vector2 local = canvas.InverseTransformPoint(corner);
                        Assert.That(Mathf.Abs(local.x) <= half.x + 0.5f && Mathf.Abs(local.y) <= half.y + 0.5f, Is.True,
                            $"{part.name} reaches {local} off a {w.x}x{w.y} window (canvas half {half}).");
                    }
                }
            }

            // ui-ux-pro-max: 4.5:1 for text. Cards against their own colour (and the focused card's); panel texts on the row.
            foreach (var card in screen.Cards)
            {
                var colours = card.Button.colors;
                foreach (var label in new TMP_Text[] { card.Name, card.Numeral })
                {
                    Assert.That(UITheme.Contrast(label.color, colours.normalColor), Is.GreaterThanOrEqualTo(4.5f), $"{card.Arcana.CardName} {label.name} on its card");
                    Assert.That(UITheme.Contrast(label.color, colours.selectedColor), Is.GreaterThanOrEqualTo(4.5f), $"{card.Arcana.CardName} {label.name} when chosen");
                }
            }
            foreach (var text in new TMP_Text[] { screen.InfoName, screen.InfoText, screen.WorksWithText })
                Assert.That(UITheme.Contrast(text.color, UITheme.Row), Is.GreaterThanOrEqualTo(4.5f), text.name);
            Assert.That(UITheme.Contrast(screen.CountText.color, UITheme.Panel), Is.GreaterThanOrEqualTo(4.5f), "Unlocked count");
            foreach (var button in new[] { screen.TakeButton, screen.RandomButton })
            {
                var label = button.GetComponentInChildren<TMP_Text>();
                Assert.That(UITheme.Contrast(label.color, button.colors.normalColor), Is.GreaterThanOrEqualTo(4.5f), button.name);
            }
            // Nothing overlaps: the grid of cards keeps clear of the info panel.
            float gridRight = screen.Cards.Max(c => { ((RectTransform)c.Button.transform).GetWorldCorners(corners); return canvas.InverseTransformPoint(corners[2]).x; });
            screen.InfoText.rectTransform.GetWorldCorners(corners);
            Assert.That(gridRight, Is.LessThan(canvas.InverseTransformPoint(corners[0]).x), "The cards stay left of the card's text.");

            screen.PressRandom();
            yield return null;
            Assert.That(ArcanaManager.Instance.ActiveArcanas.Count, Is.EqualTo(1), "Random gives one card.");
        }

        /// <summary>Takes a start card with no chest side effects (Keen Edge, Slash), so the chest tests see plain chests.</summary>
        private static void TakeStartCard()
        {
            var screen = OpenScreen();
            screen.Select(screen.Cards.First(c => c.Arcana.Type == ArcanaData.ArcanaType.Slash).Arcana);
            screen.PressTake();
        }

        private static ChestPickup ArcanaChestOnThePlayer()
        {
            var chestObject = PickupPoolManager.Instance.SpawnChest(PlayerController.Instance.transform.position);
            Assert.IsNotNull(chestObject, "SpawnChest produced nothing.");
            var chest = chestObject.GetComponent<ChestPickup>();
            chest.IsArcana = true;
            return chest;
        }

        [UnityTest]
        public IEnumerator AnArcanaChest_OffersFourUnheldCards_RerollsThroughTheButton_AndAtThreeHeldGivesARegularChest()
        {
            yield return BootIntoGameplay(save => save.ModeArcana = true);
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var arcanas = ArcanaManager.Instance;
            TakeStartCard();
            yield return null;
            ArcanaChoiceScreen screen;
            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            levelUp.AddCharges(2, 0, 0);

            var chest = ArcanaChestOnThePlayer();
            yield return WaitUntilRealtime(() => !chest.gameObject.activeInHierarchy, "the player to collect the Arcana chest");
            yield return null;
            screen = OpenScreen();
            Assert.That(screen.Draft.DraftKind, Is.EqualTo(ArcanaDraft.Kind.Chest));
            Assert.That(screen.Cards.Count, Is.EqualTo(4), "VS: four choices (22 unlocked, fewer than 23).");
            Assert.That(screen.Cards.Select(c => c.Arcana), Has.No.Member(arcanas.ActiveArcanas[0]), "Never a card already held.");
            Assert.That(screen.SkipButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(screen.RerollButton.gameObject.activeInHierarchy && screen.RerollButton.interactable, Is.True);

            var first = screen.Cards.Select(c => c.Arcana).ToList();
            int rerolls = levelUp.RerollsLeft;
            Click(screen.RerollButton, "Reroll");
            yield return null;
            Assert.That(screen.Cards.Count, Is.EqualTo(4));
            Assert.That(screen.Cards.Select(c => c.Arcana).Intersect(first), Is.Empty, "A reroll never shows a card again.");
            var stats = PlayerController.Instance.Stats;
            Assert.That(levelUp.RerollsLeft == rerolls - 1 || stats.Preserve > 0f, Is.True, "A Reroll spends a charge (Preserve may keep it).");

            var taken = screen.Cards.First(c => c.Arcana.Type != ArcanaData.ArcanaType.GameKiller).Arcana;
            screen.Select(taken);
            Click(screen.TakeButton, "Take this card");
            yield return null;
            Assert.That(arcanas.ActiveArcanas.Count, Is.EqualTo(2));
            Assert.That(arcanas.ActiveArcanas[1], Is.SameAs(taken));

            // A third Arcana (any source), then the next Arcana chest is a regular bronze chest (wiki Arcanas).
            arcanas.ApplyArcana(arcanas.AllArcanas.First(a => !arcanas.ActiveArcanas.Contains(a) && a.Type != ArcanaData.ArcanaType.GameKiller
                                                                && a.Type != ArcanaData.ArcanaType.MadGroove));
            Assert.That(arcanas.CanGrantFromChest, Is.False);
            var chestUI = GetPrivateField<ChestUI>(PickupPoolManager.Instance, "_chestUI");
            var bronze = ArcanaChestOnThePlayer();
            yield return WaitUntilRealtime(() => !bronze.gameObject.activeInHierarchy, "the player to collect the last Arcana chest");
            yield return null;
            Assert.That(screen.IsOpen, Is.False, "No Arcana choice past three Arcanas.");
            Assert.That(chestUI.IsOpen, Is.True, "It opens as a regular chest.");
            Assert.That(chestUI.Screen != null, Is.True);
            yield return chestUI.OpenAndClose();
            Assert.That(arcanas.ActiveArcanas.Count, Is.EqualTo(3));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator SkippingAChestDraft_GivesNoArcana_AndResumes()
        {
            yield return BootIntoGameplay(save => save.ModeArcana = true);
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var arcanas = ArcanaManager.Instance;
            TakeStartCard();
            yield return null;
            var chest = ArcanaChestOnThePlayer();
            yield return WaitUntilRealtime(() => !chest.gameObject.activeInHierarchy, "the player to collect the Arcana chest");
            yield return null;
            var screen = OpenScreen();
            Click(screen.SkipButton, "Skip");
            yield return null;
            Assert.That(arcanas.ActiveArcanas.Count, Is.EqualTo(1), "VS patch 0.11.0: a draft can be skipped.");
            Assert.That(GameTime.IsPaused, Is.False);
        }

        [UnityTest]
        public IEnumerator DrawAFate_OffersEveryUnheldCard_WithARandomOption()
        {
            yield return BootIntoGameplay();
            var arcanas = ArcanaManager.Instance;
            arcanas.ApplyArcana(arcanas.AllArcanas[0]);
            ArcanaData given = null;
            Assert.That(arcanas.OpenMerchantDraft(card => given = card), Is.True);
            var screen = OpenScreen();
            Assert.That(screen.Draft.DraftKind, Is.EqualTo(ArcanaDraft.Kind.Merchant));
            Assert.That(screen.Cards.Count, Is.EqualTo(21), "VS Merchant: 'All unlocked Arcanas are available as choices' (one is held).");
            Assert.That(screen.SkipButton.gameObject.activeInHierarchy, Is.False, "The gold is paid: no Skip.");
            Click(screen.RandomButton, "Take a random card");
            yield return null;
            Assert.That(given, Is.Not.Null);
            Assert.That(arcanas.ActiveArcanas.Count, Is.EqualTo(2));
        }
    }
}
