using System;
using System.Collections.Generic;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Which Arcanas a save has unlocked (release item P2-01, 2026-10-09).
    ///
    /// vampire.survivors.wiki/w/Arcanas (raw, fetched 2026-10-09): "Obtaining the Randomazzo in Gallo Tower unlocks the
    /// first Arcana, Sarabande of Healing (VI). The rest of the Arcanas in the base set are unlocked by reaching level 50
    /// with certain characters, or by reaching the 31:00 mark in certain stages." Soul Hunter has no Randomazzo (no
    /// relics), none of those VS characters, and no stage reaches 31:00 (each stage ends at 30:00, owner run structure).
    /// The 2026-10-01 precedent locks relic unlocks until relics exist, but every Arcana unlock starts from the Randomazzo,
    /// so that would leave Arcana mode with nothing to pick. Balance method (owner, 2026-10-09, the wiki gives no answer
    /// for a game without these unlock paths): every card is unlocked, so the mode is playable, until the owner adds the
    /// VS unlock system ("missing VS items and the VS unlock system are added later", owner 2026-09-30). The count still
    /// drives the VS 23+ rule (six chest choices and a free Reroll), which 22 base cards never reach.
    /// </summary>
    public static class ArcanaUnlocks
    {
        public static bool IsUnlocked(ArcanaData card) => card != null;

        public static int UnlockedCount(IEnumerable<ArcanaData> all)
        {
            int count = 0;
            if (all != null) foreach (var card in all) if (IsUnlocked(card)) count++;
            return count;
        }
    }

    /// <summary>
    /// One Arcana choice (vampire.survivors.wiki/w/Arcanas, raw, fetched 2026-10-09):
    /// - an Arcana chest: "The menu offers four or more randomly chosen Arcanas from the uncollected Arcanas. The player
    ///   may spend Rerolls to get new choices until all options have been displayed, and unlike normal level ups, previous
    ///   options will not be offered again." "If the player has unlocked a total of 23 or more Arcanas or Darkanas ...
    ///   Arcana Chests will instead offer six random Arcanas as opposed to four, and each Arcana Chest will allow the
    ///   player one free Reroll." Patch 0.11.0: "Added option to skip arcana drafts".
    /// - the start of a run: "the player is allowed to choose one of any unlocked Arcanas ... They are also given the option
    ///   to select a random one." Hotfix 1.14.105: the selection "doesn't open at the start of the game if there are no
    ///   available arcana to choose".
    /// - the Inverse merchant (Arcanas and Merchant pages): "All unlocked Arcanas are available as choices."
    /// When fewer uncollected Arcanas are left than the draft shows, it shows what is left (the wiki is silent; balance
    /// method: the only choice that never invents a card). Pure rules: no Unity objects, so EditMode tests drive them.
    /// </summary>
    public sealed class ArcanaDraft
    {
        /// <summary>
        /// Weapons: the Armoury Cache's list (VS Candybox: "opens a list of all base weapons remaining to be obtained in the
        /// run"). Its cards are stand-ins made for the screen (the weapon's name, icon and text), every one is shown, one
        /// must be chosen, and there is no random pick, Reroll or Skip.
        /// </summary>
        public enum Kind { Start, Chest, Merchant, Weapons }

        /// <summary>VS chest draft: four choices.</summary>
        public const int ChestChoices = 4;
        /// <summary>VS: six choices and one free Reroll once 23 or more Arcanas are unlocked.</summary>
        public const int LargeChestChoices = 6;
        public const int LargeDraftUnlocks = 23;

        public static int ChestChoiceCount(int unlockedTotal) => unlockedTotal >= LargeDraftUnlocks ? LargeChestChoices : ChestChoices;
        public static int ChestFreeRerolls(int unlockedTotal) => unlockedTotal >= LargeDraftUnlocks ? 1 : 0;

        private readonly List<ArcanaData> _pool;
        private readonly List<ArcanaData> _options = new List<ArcanaData>();
        private readonly HashSet<ArcanaData> _shown = new HashSet<ArcanaData>();
        private readonly Func<float> _random;

        public Kind DraftKind { get; }
        /// <summary>Cards on screen now (all of the pool for the start pick and the merchant).</summary>
        public IReadOnlyList<ArcanaData> Options => _options;
        /// <summary>Every card offered so far in this draft.</summary>
        public IReadOnlyCollection<ArcanaData> Shown => _shown;
        public int ChoiceCount { get; }
        public int FreeRerollsLeft { get; private set; }
        /// <summary>A start or merchant draft offers a random card too; a chest draft can be skipped.</summary>
        public bool OffersRandom => DraftKind == Kind.Start || DraftKind == Kind.Merchant;
        public bool CanSkip => DraftKind == Kind.Chest;
        /// <summary>Only a chest draft rerolls, and only while a card has not been shown yet.</summary>
        public bool HasUnshown => DraftKind == Kind.Chest && UnshownCount > 0;
        public int UnshownCount
        {
            get
            {
                int n = 0;
                foreach (var card in _pool) if (!_shown.Contains(card)) n++;
                return n;
            }
        }

        /// <param name="pool">The unheld, unlocked Arcanas that can be offered.</param>
        /// <param name="random">Uniform rolls in [0, 1).</param>
        public ArcanaDraft(Kind kind, IEnumerable<ArcanaData> pool, int choiceCount, int freeRerolls, Func<float> random)
        {
            DraftKind = kind;
            _pool = new List<ArcanaData>();
            if (pool != null) foreach (var card in pool) if (card != null && !_pool.Contains(card)) _pool.Add(card);
            _random = random ?? (() => UnityEngine.Random.value);
            ChoiceCount = kind == Kind.Chest ? Math.Max(1, choiceCount) : _pool.Count;
            FreeRerollsLeft = kind == Kind.Chest ? Math.Max(0, freeRerolls) : 0;
            DrawOptions();
        }

        /// <summary>A chest draft for a player holding <paramref name="held"/> out of <paramref name="all"/>.</summary>
        public static ArcanaDraft ForChest(IEnumerable<ArcanaData> all, ICollection<ArcanaData> held, Func<float> random)
        {
            var pool = Unheld(all, held);
            int unlocked = ArcanaUnlocks.UnlockedCount(all);
            return new ArcanaDraft(Kind.Chest, pool, ChestChoiceCount(unlocked), ChestFreeRerolls(unlocked), random);
        }

        /// <summary>The start pick or the merchant's Draw a Fate: every unlocked Arcana not held.</summary>
        public static ArcanaDraft ForEveryUnlocked(Kind kind, IEnumerable<ArcanaData> all, ICollection<ArcanaData> held, Func<float> random) =>
            new ArcanaDraft(kind, Unheld(all, held), 0, 0, random);

        public static List<ArcanaData> Unheld(IEnumerable<ArcanaData> all, ICollection<ArcanaData> held)
        {
            var pool = new List<ArcanaData>();
            if (all == null) return pool;
            foreach (var card in all)
                if (card != null && ArcanaUnlocks.IsUnlocked(card) && (held == null || !held.Contains(card)) && !pool.Contains(card)) pool.Add(card);
            return pool;
        }

        private void DrawOptions()
        {
            _options.Clear();
            var fresh = new List<ArcanaData>();
            foreach (var card in _pool) if (!_shown.Contains(card)) fresh.Add(card);
            int count = Math.Min(ChoiceCount, fresh.Count);
            for (int i = 0; i < count; i++)
            {
                int index = Math.Min(fresh.Count - 1, (int)(Clamp01(_random()) * fresh.Count));
                _options.Add(fresh[index]);
                _shown.Add(fresh[index]);
                fresh.RemoveAt(index);
            }
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v >= 1f ? 0.9999f : v;

        /// <summary>
        /// New choices from the cards not shown yet. <paramref name="spendCharge"/> pays with a Reroll charge (VS Rerolls;
        /// Preserve may keep it) once the free Reroll is used. False when nothing is left to show or nothing paid.
        /// </summary>
        public bool Reroll(Func<bool> spendCharge)
        {
            if (!HasUnshown) return false;
            if (FreeRerollsLeft > 0) FreeRerollsLeft--;
            else if (spendCharge == null || !spendCharge()) return false;
            DrawOptions();
            return true;
        }

        /// <summary>True when <paramref name="card"/> may be taken from this draft now.</summary>
        public bool CanChoose(ArcanaData card) => card != null && _options.Contains(card);

        /// <summary>A random card among those on screen (the start pick's and the merchant's random option).</summary>
        public ArcanaData RandomOption()
        {
            if (_options.Count == 0) return null;
            return _options[Math.Min(_options.Count - 1, (int)(Clamp01(_random()) * _options.Count))];
        }
    }
}
