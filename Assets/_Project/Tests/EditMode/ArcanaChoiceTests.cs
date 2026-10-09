using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// The VS Arcana choice (vampire.survivors.wiki/w/Arcanas, raw, fetched 2026-10-09; release item P2-01): a chest offers
    /// "four or more randomly chosen Arcanas from the uncollected Arcanas", Rerolls give new choices "until all options
    /// have been displayed" and "previous options will not be offered again"; with 23+ unlocked, six and one free Reroll;
    /// the start of a run and the Inverse merchant offer every unlocked Arcana, with a random option.
    /// </summary>
    public class ArcanaChoiceTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private readonly List<Object> _created = new List<Object>();
        private readonly List<ArcanaManager> _managers = new List<ArcanaManager>();

        private T Track<T>(T obj) where T : Object { _created.Add(obj); return obj; }

        [TearDown]
        public void TearDown()
        {
            foreach (var manager in _managers) if (manager != null) typeof(ArcanaManager).GetMethod("OnDestroy", Private).Invoke(manager, null);
            _managers.Clear();
            foreach (var obj in _created) if (obj != null) Object.DestroyImmediate(obj);
            _created.Clear();
            SoulHunter.Gameplay.Combat.AutoAttackWeapon.SlashArcanaActive = false;
        }

        private List<ArcanaData> Deck(int count)
        {
            var deck = new List<ArcanaData>();
            for (int i = 0; i < count; i++)
            {
                var card = Track(ScriptableObject.CreateInstance<ArcanaData>());
                card.CardName = "Test card " + i;
                card.CardNumber = i.ToString();
                card.Type = ArcanaData.ArcanaType.IronBlueWill; // a card with no side effects in Edit Mode
                deck.Add(card);
            }
            return deck;
        }

        private static System.Func<float> Rolls(int seed)
        {
            var random = new System.Random(seed);
            return () => (float)random.NextDouble();
        }

        [Test]
        public void AChestOffersFourUnheldCards_AndRerollsShowOnlyNewOnes_UntilAllWereShown()
        {
            var deck = Deck(22);
            var held = new List<ArcanaData> { deck[0] };
            var draft = ArcanaDraft.ForChest(deck, held, Rolls(1));
            Assert.That(draft.ChoiceCount, Is.EqualTo(4));
            Assert.That(draft.FreeRerollsLeft, Is.EqualTo(0), "22 base cards: fewer than 23 unlocked, no free Reroll.");
            Assert.That(draft.Options.Count, Is.EqualTo(4));
            Assert.That(draft.Options.Distinct().Count(), Is.EqualTo(4));
            Assert.That(draft.Options, Has.No.Member(deck[0]), "Never a card already held.");
            Assert.That(draft.OffersRandom, Is.False);
            Assert.That(draft.CanSkip, Is.True, "VS patch 0.11.0: 'Added option to skip arcana drafts'.");

            Assert.That(draft.Reroll(() => false), Is.False, "No Reroll charge: no new cards.");
            int charges = 0;
            var seen = new HashSet<ArcanaData>(draft.Options);
            var sizes = new List<int>();
            while (draft.Reroll(() => { charges++; return true; }))
            {
                foreach (var card in draft.Options) Assert.That(seen.Add(card), Is.True, "A rerolled card is never shown again.");
                sizes.Add(draft.Options.Count);
            }
            Assert.That(seen.Count, Is.EqualTo(21), "Every unheld card was shown once.");
            Assert.That(sizes, Is.EqualTo(new[] { 4, 4, 4, 4, 1 }), "Fewer left than four: what is left.");
            Assert.That(charges, Is.EqualTo(5), "Each Reroll spent one charge.");
            Assert.That(draft.HasUnshown, Is.False);
            Assert.That(draft.Reroll(() => true), Is.False, "Rerolling stops once all were shown.");
        }

        [Test]
        public void With23OrMoreUnlocked_AChestOffersSix_AndOneFreeReroll()
        {
            Assert.That(ArcanaDraft.ChestChoiceCount(22), Is.EqualTo(4));
            Assert.That(ArcanaDraft.ChestChoiceCount(23), Is.EqualTo(6));
            Assert.That(ArcanaDraft.ChestFreeRerolls(22), Is.EqualTo(0));
            Assert.That(ArcanaDraft.ChestFreeRerolls(23), Is.EqualTo(1));
            var draft = ArcanaDraft.ForChest(Deck(30), null, Rolls(2));
            Assert.That(draft.Options.Count, Is.EqualTo(6));
            Assert.That(draft.Reroll(() => false), Is.True, "The free Reroll needs no charge.");
            Assert.That(draft.Reroll(() => false), Is.False, "Only one is free.");
        }

        [Test]
        public void TheStartPick_AndTheMerchant_OfferEveryUnheldCard_AndARandomOne()
        {
            var deck = Deck(22);
            var start = ArcanaDraft.ForEveryUnlocked(ArcanaDraft.Kind.Start, deck, new List<ArcanaData>(), Rolls(3));
            Assert.That(start.Options, Is.EquivalentTo(deck));
            Assert.That(start.OffersRandom, Is.True);
            Assert.That(start.CanSkip, Is.False);
            Assert.That(start.HasUnshown, Is.False, "Nothing to reroll.");
            Assert.That(deck, Does.Contain(start.RandomOption()));
            var merchant = ArcanaDraft.ForEveryUnlocked(ArcanaDraft.Kind.Merchant, deck, new List<ArcanaData> { deck[3], deck[5] }, Rolls(4));
            Assert.That(merchant.Options.Count, Is.EqualTo(20));
            Assert.That(merchant.Options, Has.No.Member(deck[3]));
            Assert.That(ArcanaDraft.ForEveryUnlocked(ArcanaDraft.Kind.Start, new List<ArcanaData>(), null, Rolls(5)).Options, Is.Empty,
                "Hotfix 1.14.105: nothing to choose, no selection.");
        }

        [Test]
        public void EveryCardIsUnlocked_UntilTheVsUnlockSystemExists()
        {
            var deck = Deck(3);
            Assert.That(ArcanaUnlocks.UnlockedCount(deck), Is.EqualTo(3));
            Assert.That(ArcanaUnlocks.IsUnlocked(null), Is.False);
        }

        private ArcanaManager NewManager(List<ArcanaData> deck)
        {
            var manager = Track(new GameObject("Arcana")).AddComponent<ArcanaManager>();
            typeof(ArcanaManager).GetMethod("Awake", Private).Invoke(manager, null);
            manager.SetAvailableArcanas(deck);
            _managers.Add(manager);
            return manager;
        }

        [Test]
        public void TheStartPick_CountsTowardTheThreeArcanaChestLimit()
        {
            var deck = Deck(6);
            var manager = NewManager(deck);
            manager.BeginStartChoice(); // Edit Mode: no screen, so a random card
            Assert.That(manager.StartingArcana, Is.Not.Null);
            Assert.That(manager.ActiveArcanas, Is.EqualTo(new[] { manager.StartingArcana }));
            var chest = Track(new GameObject("Chest")).AddComponent<ChestLogicController>();
            chest.ProcessArcanaChest();
            chest.ProcessArcanaChest();
            Assert.That(manager.ActiveArcanas.Count, Is.EqualTo(3));
            Assert.That(manager.CanGrantFromChest, Is.False, "VS: 'If the player has three or more Arcanas, the Arcana bosses will instead drop regular chests.'");
        }

        [Test]
        public void AChestThatOffersAChoice_GivesNothingUntilTheCardIsTaken_AndOnlyACardOnScreen()
        {
            var deck = Deck(10);
            var manager = NewManager(deck);
            var chest = Track(new GameObject("Chest")).AddComponent<ChestLogicController>();
            string message = chest.ProcessArcanaChest(ChestTiers.ArcanaMinutes, offerChoice: true);
            Assert.That(message, Does.StartWith("ARCANA CHEST!"));
            var draft = chest.LastResult.ArcanaDraft;
            Assert.IsNotNull(draft);
            Assert.That(chest.LastResult.IsArcana, Is.True);
            Assert.That(draft.Options.Count, Is.EqualTo(4));
            Assert.That(manager.ActiveArcanas, Is.Empty, "Nothing is given before the pick.");
            var offscreen = deck.First(c => !draft.Options.Contains(c));
            Assert.That(manager.Take(draft, offscreen), Is.Null, "A card that is not on screen cannot be taken.");
            Assert.That(manager.Take(draft, draft.Options[2]), Is.SameAs(draft.Options[2]));
            Assert.That(manager.ActiveArcanas.Count, Is.EqualTo(1));
            Assert.That(manager.Take(draft, null), Is.Null, "Skip gives nothing.");
        }

        [Test]
        public void DrawAFate_OffersEveryUnheldCard_AndIsNotHeldBackByTheChestLimit()
        {
            var deck = Deck(5);
            var manager = NewManager(deck);
            foreach (var card in deck.Take(3)) manager.ApplyArcana(card);
            ArcanaData given = null;
            Assert.That(manager.OpenMerchantDraft(card => given = card), Is.True);
            Assert.That(given, Is.Not.Null, "Edit Mode: the random option.");
            Assert.That(deck.Take(3), Has.No.Member(given), "An unheld card.");
            Assert.That(manager.ActiveArcanas.Count, Is.EqualTo(4));
            manager.ApplyArcana(deck[4] == given ? deck[3] : deck[4]);
            Assert.That(manager.OpenMerchantDraft(card => given = card), Is.False, "Nothing left: no ware.");
        }
    }
}
