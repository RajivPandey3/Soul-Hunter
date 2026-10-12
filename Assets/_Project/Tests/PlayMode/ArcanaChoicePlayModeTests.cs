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
    /// regular chest; the merchant's Draw a Fate offers every unlocked card. The screen fits every PC and phone screen
    /// (ScreenSimulation) and every text reads (4.5:1, and not under 12 sp on the phone). Every test plays on a throw-away save (GameplayPlayModeTestBase).
    /// </summary>
    public class ArcanaChoicePlayModeTests : GameplayPlayModeTestBase
    {
        private static ArcanaChoiceScreen OpenScreen()
        {
            var screen = ArcanaChoiceScreen.Current;
            Assert.That(screen != null && screen.IsOpen, Is.True, "The Arcana choice is open.");
            return screen;
        }

        /// <summary>The action buttons shown and the cards in the grid's window (the rest are scrolled away, under its mask).</summary>
        private static IEnumerable<Selectable> VisibleControls(ArcanaChoiceScreen screen)
        {
            var list = new List<Selectable> { screen.TakeButton, screen.RerollButton, screen.RandomButton, screen.SkipButton };
            list.AddRange(screen.Cards.Where(c => ResponsiveScroll.IsInView(screen.CardsScroll, (RectTransform)c.Button.transform)).Select(c => (Selectable)c.Button));
            return list.Where(c => c.gameObject.activeInHierarchy).ToList();
        }

        /// <summary>
        /// Responsive (owner, 2026-10-10): on PC 16:9, 21:9, 5:4 and the phone held either way, the controls in view are on
        /// the canvas, apart and touch-sized; the actions stay at the panel's bottom under the cards and the card's panel;
        /// the cards and the panel never overlap (side by side when wide, stacked when tall); at least one card is in view
        /// and the last can be scrolled to; no text is under the 12 sp floor on the phone.
        /// </summary>
        private static IEnumerator AssertFitsEveryScreen(ArcanaChoiceScreen screen)
        {
            var problems = new List<string>();
            try
            {
                foreach (var d in ScreenSimulation.Devices)
                {
                    yield return ScreenSimulation.At(d.Pixels, d.Dpi, d.Touch, screen.Canvas);
                    problems.AddRange(ScreenSimulation.Problems(d.Name, VisibleControls(screen)));
                    var view = ScreenSimulation.UnitsRect(screen.CardsViewport);
                    var info = ScreenSimulation.UnitsRect(screen.InfoPanel);
                    var actions = ScreenSimulation.UnitsRect(screen.TakeButton.transform as RectTransform);
                    if (view.Overlaps(info)) problems.Add($"{d.Name}: the cards {view} overlap the card's panel {info}");
                    if (view.Overlaps(actions) || info.Overlaps(actions)) problems.Add($"{d.Name}: the actions {actions} run into the cards or the panel");
                    if (actions.yMax > Mathf.Min(view.yMin, info.yMin) + 0.5f) problems.Add($"{d.Name}: the actions {actions} are not under the cards and the panel");
                    bool tall = UiScreen.IsPortrait;
                    if (tall != screen.IsStacked) problems.Add($"{d.Name}: the cards and the panel are {(screen.IsStacked ? "stacked" : "side by side")} on a {(tall ? "tall" : "wide")} screen");
                    if (!screen.Cards.Any(c => ResponsiveScroll.IsInView(screen.CardsScroll, (RectTransform)c.Button.transform))) problems.Add($"{d.Name}: no card is in view");
                    screen.CardsScroll.verticalNormalizedPosition = 0f;
                    Canvas.ForceUpdateCanvases();
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    if (!ResponsiveScroll.IsInView(screen.CardsScroll, (RectTransform)screen.Cards.Last().Button.transform))
                        problems.Add($"{d.Name}: the last card cannot be scrolled into view");
                    screen.CardsScroll.verticalNormalizedPosition = 1f;
                    Canvas.ForceUpdateCanvases();
                    if (d.Touch)
                        foreach (var t in screen.GetComponentsInChildren<TMP_Text>())
                            if (!string.IsNullOrEmpty(t.text) && t.fontSize < UiScreen.MinTextUnits - 0.01f)
                                problems.Add($"{d.Name}: {t.name} '{t.text}' at {t.fontSize:0.#} is under {UiScreen.MinTextUnits:0.#} units");
                }
            }
            finally { ScreenSimulation.Restore(); }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
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
        public IEnumerator TheChoiceScreen_FitsEveryScreen_AndEveryTextReads()
        {
            yield return BootIntoGameplay(save => save.ModeArcana = true);
            var screen = OpenScreen();
            var scaler = screen.Canvas.GetComponent<CanvasScaler>();
            Assert.That(scaler.GetComponent<SoulHunter.Gameplay.UI.ResponsiveCanvas>(), Is.Not.Null, "It takes the game's one UI scale (ResponsiveCanvas, landscape and portrait).");
            Canvas.ForceUpdateCanvases();
            yield return AssertFitsEveryScreen(screen);

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
            yield return AssertFitsEveryScreen(screen); // four cards with Take, Reroll and Skip

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
