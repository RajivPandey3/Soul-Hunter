using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Audio;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Release item P3-05 (owner 2026-10-09): the VS Randomizers (Omens = Random Events, Blind Fate = Random LevelUp), the
    /// Inverse map look and layout, and Music Selection (Soul Hunter's Music screen). vampire.survivors.wiki/w/Stages,
    /// Random_Events, Level_up, Options and Music (raw pages re-fetched 2026-10-09). Pure rules only; the PlayMode suite
    /// plays them in the real game.
    /// </summary>
    public class RandomizerModeTests
    {
        [SetUp]
        public void SetUp()
        {
            RunModes.Reset();
            MusicSelection.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            RunModes.Reset();
            MusicSelection.Reset();
        }

        // ---------------------------------------------------------------- the two new modes

        [Test]
        public void OldSave_LoadsTheRandomizersOff_TheInverseLookOn_AndEachLevelsOwnMusic()
        {
            var data = new GameData();
            JsonUtility.FromJsonOverwrite("{\"SaveVersion\": 2, \"Currency\": 50, \"ModeHurry\": true}", data);
            SaveService.Migrate(data);
            Assert.That(data.ModeRandomEvents || data.ModeRandomLevelUp, Is.False, "A save without the fields loads both Randomizers off.");
            Assert.That(data.VisuallyInvertStages, Is.True, "\"Visually Invert Stages\" starts on.");
            Assert.That(data.MusicChoice, Is.EqualTo(MusicSelection.LevelsOwn));
            Assert.That(data.MusicTempo, Is.EqualTo(MusicSelection.Normal));
            Assert.That(data.MusicLocked, Is.False);
        }

        [Test]
        public void SaveRepair_KeepsMusicChoicesInRange()
        {
            var data = new GameData { MusicChoice = 99, MusicTempo = -3 };
            SaveService.Migrate(data);
            Assert.That(data.MusicChoice, Is.EqualTo(0));
            Assert.That(data.MusicTempo, Is.EqualTo(0));
            data.MusicChoice = 11;
            data.MusicTempo = 2;
            SaveService.Migrate(data);
            Assert.That(data.MusicChoice, Is.EqualTo(11), "Level 10's track is a real choice.");
            Assert.That(data.MusicTempo, Is.EqualTo(2));
        }

        [Test]
        public void Randomizers_GetAndSet_BeginRun_AndReset()
        {
            var data = new GameData();
            RunModeOptions.Set(data, RunMode.RandomEvents, true);
            RunModeOptions.Set(data, RunMode.RandomLevelUp, true);
            Assert.That(data.ModeRandomEvents && data.ModeRandomLevelUp, Is.True);
            Assert.That(RunModeOptions.Get(data, RunMode.RandomEvents) && RunModeOptions.Get(data, RunMode.RandomLevelUp), Is.True);
            RunModeOptions.BeginRun(data);
            Assert.That(RunModeOptions.IsActive(RunMode.RandomEvents) && RunModeOptions.IsActive(RunMode.RandomLevelUp), Is.True);
            RunModes.EventFrequencyBoost = 2f;
            RunModes.Reset();
            Assert.That(RunModes.RandomEvents || RunModes.RandomLevelUp, Is.False);
            Assert.That(RunModes.EventFrequencyBoost, Is.EqualTo(1f));
        }

        [Test]
        public void Randomizers_HaveSoulHuntersOwnNames_AndShowOnResults()
        {
            Assert.That(RunModeOptions.Name(RunMode.RandomEvents), Is.EqualTo("Omens"));
            Assert.That(RunModeOptions.Name(RunMode.RandomLevelUp), Is.EqualTo("Blind Fate"));
            RunModes.RandomEvents = RunModes.RandomLevelUp = true;
            Assert.That(RunResults.ModeText(), Does.Contain("Omens").And.Contain("Blind Fate"));
        }

        [Test]
        public void SwellingHorde_DoublesTheSpawnRate_OnTopOfEndless()
        {
            RunModes.EndlessCycle = 1;
            Assert.That(RunModes.FrequencyMultiplier, Is.EqualTo(1.5f).Within(1e-4f));
            RunModes.EventFrequencyBoost = OmenTable.SwellingFactor;
            Assert.That(RunModes.FrequencyMultiplier, Is.EqualTo(3f).Within(1e-4f));
        }

        // ---------------------------------------------------------------- Omens timetable

        [Test]
        public void Omens_PickAtXX30_FireAtXX00_OnTheStageClock()
        {
            var marks = new List<(float Mark, bool Pick)>();
            OmenTable.Crossed(29.9f, 30.1f, marks);
            Assert.That(marks, Is.EqualTo(new List<(float, bool)> { (30f, true) }), "0:30 picks.");
            OmenTable.Crossed(59.9f, 60.05f, marks);
            Assert.That(marks, Is.EqualTo(new List<(float, bool)> { (60f, false) }), "1:00 fires.");
            OmenTable.Crossed(0f, 0.5f, marks);
            Assert.That(marks, Is.Empty, "0:00 is no mark: nothing was picked.");
            OmenTable.Crossed(89f, 120.5f, marks);
            Assert.That(marks, Is.EqualTo(new List<(float, bool)> { (120f, false) }), "1:30 lies more than 10 s back: dropped; 2:00 stays.");
            OmenTable.Crossed(89f, 92f, marks);
            Assert.That(marks, Is.EqualTo(new List<(float, bool)> { (90f, true) }));
            OmenTable.Crossed(10f, 400f, marks);
            Assert.That(marks.All(m => 400f - m.Mark <= OmenTable.StaleSeconds), Is.True, "A Debug skip drops the marks it jumped past.");
        }

        [Test]
        public void Omens_Pool_DeathsShadowOnlyOnStages5And9_NoFormationsInABossFight()
        {
            for (int stage = 1; stage <= 10; stage++)
            {
                var pool = OmenTable.Pool(stage, true);
                Assert.That(pool.Contains(OmenEvent.DeathsShadow), Is.True, $"Stage {stage}: as an Omen the Shadow comes on every level (2026-10-13).");
                Assert.That(OmenTable.Pool(stage, false).Any(OmenTable.IsFormation), Is.False);
                Assert.That(pool.Distinct().Count(), Is.EqualTo(pool.Count));
            }
            Assert.That(OmenTable.Pool(1, true).Count, Is.EqualTo(27), "Every VS Random Event has its Omen: 20, and the last seven of 2026-10-13.");
            Assert.That(OmenTable.Pool(1, true).Count, Is.EqualTo(System.Enum.GetValues(typeof(OmenEvent)).Length), "No event is left out of the wheel.");
        }

        [Test]
        public void Omens_PickIsEvenOverThePool_AndNamesAreSoulHuntersOwn()
        {
            var pool = OmenTable.Pool(5, true);
            var seen = new HashSet<OmenEvent>();
            for (int i = 0; i < pool.Count; i++) seen.Add(OmenTable.Pick(pool, (i + 0.5f) / pool.Count));
            Assert.That(seen.Count, Is.EqualTo(pool.Count), "Every event has an equal slice of the roll.");
            Assert.That(OmenTable.Pick(pool, 1f), Is.EqualTo(pool[pool.Count - 1]));
            var vsNames = new[] { "Pet", "Candybox", "Firebreathing", "Rosary", "Light Sources", "Ultra Wave", "Chicken Bonanza",
                "Shooting Stars", "Luck Boost", "Jelly Wall", "Bat Swarm", "Skeleton Swarm", "Free Passive", "The Stalker", "Pile Assault", "Gold Fever",
                "Shrimp Swarm", "Roulette", "Summon II Molise", "Summon Night", "The Drowner", "Flower Wall" };
            var names = System.Enum.GetValues(typeof(OmenEvent)).Cast<OmenEvent>().Select(OmenTable.Name).ToList();
            Assert.That(names.Distinct().Count(), Is.EqualTo(names.Count));
            Assert.That(names.Intersect(vsNames), Is.Empty, "No VS event name is shown.");
        }

        [Test]
        public void Omens_Numbers_FollowTheWiki()
        {
            Assert.That(OmenTable.MendingHealAmount, Is.EqualTo(2), "Pet: \"each healing them for 2 hit points\".");
            Assert.That(OmenTable.FeastHeal, Is.EqualTo(30), "Chicken Bonanza: \"each one healing for 30 hit points\".");
            Assert.That(OmenTable.FortuneLuck, Is.EqualTo(0.5f), "Luck Boost: \"by 50%\".");
            Assert.That(OmenTable.SwellingSeconds, Is.EqualTo(60f), "Ultra Wave: \"for 1 minute\".");
            Assert.That(OmenTable.GallowsSeconds, Is.EqualTo(60f), "Pile Assault: \"Disappears after 1 minute\".");
            Assert.That(OmenTable.ShadowSeconds, Is.EqualTo(120f), "The Stalker: \"for ~2 minutes\".");
            Assert.That(OmenTable.RushWaves * OmenTable.RushGapSeconds, Is.EqualTo(30f), "Bat Swarm: \"for ~30 seconds\".");
        }

        // ---------------------------------------------------------------- Blind Fate

        [Test]
        public void BlindFate_PicksEvenlyAmongTheChoices_AndNeverAnAlwaysCard()
        {
            var a = ScriptableObject.CreateInstance<UpgradeData>();
            var b = ScriptableObject.CreateInstance<UpgradeData>();
            var c = ScriptableObject.CreateInstance<UpgradeData>();
            try
            {
                var choices = new List<UpgradeData> { a, b, c };
                Assert.That(LevelUpManager.RandomPick(choices, 0f), Is.SameAs(a));
                Assert.That(LevelUpManager.RandomPick(choices, 0.5f), Is.SameAs(b));
                Assert.That(LevelUpManager.RandomPick(choices, 0.99f), Is.SameAs(c));
                Assert.That(LevelUpManager.RandomPick(new List<UpgradeData>(), 0.5f), Is.Null);

                var cards = MaxedRewards.BuildCards(true);
                for (float roll = 0f; roll <= 1f; roll += 0.05f)
                {
                    var pick = LevelUpManager.RandomPick(cards, roll);
                    Assert.That(MaxedRewards.TryGetKind(pick, out var kind), Is.True);
                    Assert.That(kind == MaxedRewards.Kind.Gold || kind == MaxedRewards.Kind.Chicken, Is.True, "Never an \"Always\" card.");
                }
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
                Object.DestroyImmediate(c);
            }
        }

        // ---------------------------------------------------------------- Inverse map

        [Test]
        public void InverseMap_TurnsTheLayout180Degrees_OnlyInInverse()
        {
            var offset = new Vector3(40f, 0.5f, -55f);
            Assert.That(InverseMap.Layout(offset), Is.EqualTo(offset), "Without Inverse nothing turns.");
            RunModes.Inverse = true; // no save in EditMode: "Visually Invert Stages" reads as on
            Assert.That(InverseMap.Active, Is.True);
            Assert.That(InverseMap.Layout(offset), Is.EqualTo(new Vector3(-40f, 0.5f, 55f)));
        }

        [Test]
        public void InverseMap_Grade_IsDarker_TurnsTheHue_AndStaysInTheComfortBand()
        {
            Assert.That(InverseMap.ComfortMin, Is.EqualTo(0.30f), "StageFloorImporter.ComfortMin");
            Assert.That(InverseMap.ComfortMax, Is.EqualTo(0.68f), "StageFloorImporter.ComfortMax");
            Assert.That(InverseMap.TextureMean, Is.EqualTo(0.8f), "StageFloorImporter.TextureMean");
            foreach (var colour in new[] { new Color(0.35f, 0.62f, 0.30f), new Color(0.8f, 0.7f, 0.5f), new Color(0.4f, 0.4f, 0.45f), new Color(0.2f, 0.25f, 0.3f) })
            {
                Color.RGBToHSV(colour, out float h, out float s, out float v);
                Color.RGBToHSV(InverseMap.Grade(colour), out float h2, out float s2, out float v2);
                if (v >= InverseMap.ComfortMinTint) Assert.That(v2, Is.LessThanOrEqualTo(v + 1e-4f), $"{colour}: darker or equal.");
                Assert.That(v2, Is.GreaterThanOrEqualTo(Mathf.Min(v, InverseMap.ComfortMinTint) - 1e-4f), $"{colour}: never below the comfort floor.");
                if (s > 0.05f) Assert.That(Mathf.Abs(Mathf.DeltaAngle(h * 360f, h2 * 360f)), Is.EqualTo(180f).Within(1f), $"{colour}: hue turned.");
                Assert.That(s2, Is.EqualTo(s).Within(1e-3f), "Saturation kept.");
            }
            var mid = new Color(0.5f, 0.6f, 0.4f);
            Color.RGBToHSV(mid, out _, out _, out float before);
            Color.RGBToHSV(InverseMap.Grade(mid), out _, out _, out float after);
            Assert.That(after, Is.EqualTo(before * InverseMap.Darken).Within(1e-3f), "A mid colour is 80% as bright.");
        }

        // ---------------------------------------------------------------- Music

        [Test]
        public void Music_Tempos_AreTwoSemitonesFasterOrSlower()
        {
            Assert.That(MusicSelection.PitchFor(MusicSelection.Normal), Is.EqualTo(1f));
            Assert.That(MusicSelection.PitchFor(MusicSelection.Faster), Is.EqualTo(Mathf.Pow(2f, 2f / 12f)).Within(1e-3f));
            Assert.That(MusicSelection.PitchFor(MusicSelection.Slower), Is.EqualTo(Mathf.Pow(2f, -2f / 12f)).Within(1e-3f));
            Assert.That(MusicSelection.ChoiceName(MusicSelection.LevelsOwn), Is.Not.Empty);
            Assert.That(MusicSelection.ChoiceName(MusicSelection.FirstLevelTrack), Does.StartWith("Level 1: "));
            Assert.That(MusicSelection.ChoiceCount, Is.EqualTo(12), "Each level's own, the main theme, and the ten level tracks.");
        }

        [Test]
        public void Music_ChoiceLastsOneRun_UnlessLocked()
        {
            var data = new GameData { MusicChoice = 5, MusicTempo = MusicSelection.Slower };
            MusicSelection.BeginRun(data);
            Assert.That(MusicSelection.RunChoice, Is.EqualTo(5));
            Assert.That(MusicSelection.RunTempo, Is.EqualTo(MusicSelection.Slower));
            Assert.That(data.MusicChoice, Is.EqualTo(MusicSelection.LevelsOwn), "Unlocked: the next run is back to each level's own track.");
            Assert.That(data.MusicTempo, Is.EqualTo(MusicSelection.Normal));

            data = new GameData { MusicChoice = 3, MusicTempo = MusicSelection.Faster, MusicLocked = true };
            MusicSelection.BeginRun(data);
            Assert.That(MusicSelection.RunChoice, Is.EqualTo(3));
            Assert.That(data.MusicChoice, Is.EqualTo(3), "Locked: the choice persists between runs (VS Patch 1.0).");
            Assert.That(data.MusicTempo, Is.EqualTo(MusicSelection.Faster));

            MusicSelection.Reset();
            Assert.That(MusicSelection.RunChoice, Is.EqualTo(MusicSelection.LevelsOwn));
            Assert.That(MusicSelection.RunTempo, Is.EqualTo(MusicSelection.Normal));
        }
    }
}
