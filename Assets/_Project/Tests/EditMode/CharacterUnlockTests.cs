using System.Linq;
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
    /// Learning Comment: CharacterUnlockTests verifies the VS character rule: only the starting character is
    /// free; every other character unlocks by meeting its condition in a run and is then bought once with gold.
    /// All checks use an in-memory GameData, never the save file.
    /// </summary>
    public class CharacterUnlockTests
    {
        private static CharacterData Catalog(string name) => GameContentCatalog.FindCharacterExact(name);

        private static RunProgress Progress(int stage = 1, float seconds = 0f, int cleared = 0, int level = 1, int kills = 0,
            System.Func<UpgradeData.UpgradeType, int> weapons = null) =>
            new RunProgress(stage, seconds, cleared, level, kills, weapons ?? (_ => 0));

        [Test]
        public void OnlyKael_StartsUnlockedAndFree_EveryOtherCharacterHasAConditionAndPrice()
        {
            // Learning Comment: VS ki tarah shuru mein sirf ek hero; baaki sharton se khulte hain aur gold se kharide jaate hain.
            var data = new GameData();
            foreach (var c in GameContentCatalog.Load().Characters.Where(c => c != null))
            {
                Assert.That(c.OriginStage, Is.InRange(1, 10), c.CharacterName);
                if (c.CharacterName == "Kael")
                {
                    Assert.That(CharacterUnlocks.IsOwned(c, data), Is.True, "Kael is playable from the start.");
                    continue;
                }
                Assert.That(c.Unlock, Is.Not.EqualTo(CharacterData.UnlockCondition.None), $"{c.CharacterName} needs an unlock condition.");
                Assert.That(c.GoldPrice, Is.GreaterThan(0), $"{c.CharacterName} needs a price.");
                Assert.That(CharacterUnlocks.IsUnlocked(c, data), Is.False, $"{c.CharacterName} starts locked.");
                Assert.That(CharacterUnlocks.ConditionText(c), Is.Not.Empty);
                if (c.Unlock == CharacterData.UnlockCondition.ClearStage || c.Unlock == CharacterData.UnlockCondition.SurviveInStage)
                    Assert.That(c.UnlockStage, Is.InRange(1, 10), c.CharacterName);
            }
        }

        [Test]
        public void Conditions_AreMetOnlyByTheRightProgress()
        {
            // Learning Comment: Har shart sirf sahi progress par poori ho.
            var gideon = Catalog("Gideon");       // clear stage 3
            Assert.That(CharacterUnlocks.IsMet(gideon, Progress(cleared: 2)), Is.False);
            Assert.That(CharacterUnlocks.IsMet(gideon, Progress(cleared: 3)), Is.True);

            var corvan = Catalog("Corvan");       // survive 5:00 in stage 1
            Assert.That(CharacterUnlocks.IsMet(corvan, Progress(stage: 1, seconds: 299f)), Is.False);
            Assert.That(CharacterUnlocks.IsMet(corvan, Progress(stage: 1, seconds: 300f)), Is.True);
            Assert.That(CharacterUnlocks.IsMet(corvan, Progress(stage: 2, seconds: 400f)), Is.False, "Time counts only in its stage.");

            var aria = Catalog("Aria");           // reach level 20
            Assert.That(CharacterUnlocks.IsMet(aria, Progress(level: 19)), Is.False);
            Assert.That(CharacterUnlocks.IsMet(aria, Progress(level: 20)), Is.True);

            var draven = Catalog("Draven");       // 1000 kills
            Assert.That(CharacterUnlocks.IsMet(draven, Progress(kills: 999)), Is.False);
            Assert.That(CharacterUnlocks.IsMet(draven, Progress(kills: 1000)), Is.True);

            var isolde = Catalog("Isolde");       // Santa Water level 4
            Assert.That(CharacterUnlocks.IsMet(isolde, Progress(weapons: t => t == UpgradeData.UpgradeType.SantaWater ? 3 : 8)), Is.False);
            Assert.That(CharacterUnlocks.IsMet(isolde, Progress(weapons: t => t == UpgradeData.UpgradeType.SantaWater ? 4 : 0)), Is.True);
        }

        [Test]
        public void UnlockThenBuy_WithGold_MakesTheCharacterPlayable()
        {
            // Learning Comment: Khulne ke baad bhi gold se kharidna padta hai; kam gold par kharid nahi hoti.
            var data = new GameData();
            var gideon = Catalog("Gideon");
            int gold = gideon.GoldPrice - 1;
            System.Func<int, bool> spend = price => { if (gold < price) return false; gold -= price; return true; };

            Assert.That(CharacterUnlocks.TryBuy(gideon, data, spend), Is.False, "Cannot buy a locked character.");
            var unlocked = CharacterUnlockTracker.UnlockInto(data, Progress(cleared: 3));
            Assert.That(unlocked.Select(c => c.CharacterName), Does.Contain("Gideon"));
            Assert.That(unlocked.Select(c => c.CharacterName), Does.Not.Contain("Kaelen"), "Stage 4 is not cleared yet.");
            Assert.That(CharacterUnlocks.IsUnlocked(gideon, data), Is.True);
            Assert.That(CharacterUnlocks.IsOwned(gideon, data), Is.False);

            Assert.That(CharacterUnlocks.TryBuy(gideon, data, spend), Is.False, "Not enough gold.");
            gold += 1;
            Assert.That(CharacterUnlocks.TryBuy(gideon, data, spend), Is.True);
            Assert.That(gold, Is.EqualTo(0));
            Assert.That(CharacterUnlocks.IsOwned(gideon, data), Is.True);
            Assert.That(CharacterUnlocks.TryBuy(gideon, data, spend), Is.True, "Buying again is free (already owned).");
            Assert.That(CharacterUnlockTracker.UnlockInto(data, Progress(cleared: 3)), Is.Empty, "Unlocks happen once.");
        }

        [Test]
        public void Price_RisesTenPercentOfBase_PerCharacterPurchased()
        {
            // VS wiki Characters: "purchased using gold, increased by 10% additively per characters purchased".
            var data = new GameData();
            var corvan = Catalog("Corvan");
            int basePrice = corvan.GoldPrice;
            Assert.That(CharacterUnlocks.PriceFor(corvan, data), Is.EqualTo(basePrice));
            data.OwnedCharacters = new System.Collections.Generic.List<string> { "Kael" };
            Assert.That(CharacterUnlocks.PriceFor(corvan, data), Is.EqualTo(basePrice), "A free character is not a purchase.");
            data.OwnedCharacters.Add("Gideon");
            Assert.That(CharacterUnlocks.PriceFor(corvan, data), Is.EqualTo(Mathf.RoundToInt(basePrice * 1.1f)));
            data.OwnedCharacters.Add("Aria");
            data.OwnedCharacters.Add("Aria");
            Assert.That(CharacterUnlocks.PriceFor(corvan, data), Is.EqualTo(Mathf.RoundToInt(basePrice * 1.2f)), "A name listed twice counts once.");
            Assert.That(CharacterUnlocks.PriceFor(Catalog("Kael"), data), Is.EqualTo(0), "Kael stays free.");
        }

        [Test]
        public void TryBuy_SpendsTheScaledPrice()
        {
            var data = new GameData { OwnedCharacters = new System.Collections.Generic.List<string> { "Corvan" } };
            var gideon = Catalog("Gideon");
            CharacterUnlocks.MarkUnlocked(gideon, data);
            int spent = -1;
            Assert.That(CharacterUnlocks.TryBuy(gideon, data, price => { spent = price; return true; }), Is.True);
            Assert.That(spent, Is.EqualTo(Mathf.RoundToInt(gideon.GoldPrice * 1.1f)));
        }

        [Test]
        public void OldSaves_WithoutCharacterLists_StillHaveKael()
        {
            // Learning Comment: Purani save mein nayi lists null hon to bhi crash na ho aur Kael khele.
            var data = new GameData { UnlockedCharacters = null, OwnedCharacters = null };
            Assert.That(CharacterUnlocks.IsOwned(Catalog("Kael"), data), Is.True);
            Assert.That(CharacterUnlocks.IsUnlocked(Catalog("Gideon"), data), Is.False);
            Assert.That(CharacterUnlocks.MarkUnlocked(Catalog("Gideon"), data), Is.True);
            Assert.That(data.UnlockedCharacters, Does.Contain("Gideon"));
        }
    }
}
