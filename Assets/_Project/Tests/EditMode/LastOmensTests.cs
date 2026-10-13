using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Hazards;
using SoulHunter.Gameplay.Pickups;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// The rows of Docs/VS_DOT_Table.md closed on 2026-10-13 (owner: "kuch bhi nahi bachna chahiye"): the last seven Omens
    /// (raw Random_Events, Candybox, The_Drowner, Dragon_Swarm, Flower_Wall_(event)), the Collection's size against the
    /// Banish ranks (raw Banish), and Level 9's Closing Arena (the owner's story).
    /// </summary>
    public class LastOmensTests
    {
        private static readonly OmenEvent[] Last =
        {
            OmenEvent.ArmouryCache, OmenEvent.SerpentStreams, OmenEvent.PowderRing, OmenEvent.Rootbound, OmenEvent.Nightfall,
            OmenEvent.RisingTide, OmenEvent.ThornCircle,
        };

        [Test]
        public void TheLastSevenOmens_AreInEveryPool_UnderTheirOwnNames()
        {
            for (int stage = 1; stage <= 10; stage++)
                foreach (var e in Last)
                    Assert.That(OmenTable.Pool(stage, true).Contains(e), Is.True, $"Stage {stage}: {e}");
            CollectionAssert.AreEqual(
                new[] { "Armoury Cache", "Serpent Streams", "Powder Ring", "Rootbound", "Nightfall", "Rising Tide", "Thorn Circle" },
                Last.Select(OmenTable.Name).ToArray());
            // Only the streams go through the spawner's map-event path (refused during a boss fight).
            Assert.That(Last.Where(OmenTable.IsFormation).ToArray(), Is.EqualTo(new[] { OmenEvent.SerpentStreams }));
            foreach (var e in Last) Assert.That(OmenTable.OutlastsStageClear(e), Is.False, e.ToString());
        }

        [Test]
        public void TheLastSevenOmens_Numbers_FollowTheWiki()
        {
            Assert.That(OmenTable.RootboundSeconds, Is.EqualTo(120f), "Summon II Molise: \"Lasts for ~2 minutes.\"");
            Assert.That(OmenTable.NightfallSeconds, Is.EqualTo(60f), "Summon Night: \"for ~1 minute\".");
            Assert.That(StageHazardTable.TideSeconds, Is.EqualTo(60f), "The Drowner: \"for ~1 minute\".");
            Assert.That(StageHazardTable.TideDamage, Is.EqualTo(20), "The_Drowner: \"20 damage per game-tick\".");
            Assert.That(StageHazardTable.TideCallerHealth(1), Is.EqualTo(800), "The_Drowner statbox: health 800 - 1250, HP x Level.");
            Assert.That(StageHazardTable.TideCallerHealth(40), Is.EqualTo(32000));
            Assert.That(StageHazardTable.ThornSeconds, Is.EqualTo(50f), "Flower Wall: \"Disappears after ~50 seconds.\"");
            Assert.That(StageHazardTable.MineFuseSeconds, Is.EqualTo(2f), "The self-destruct rule the game already has (VS Sig.ra Rossi).");
            Assert.That(StageHazardTable.MineDamage, Is.EqualTo(50));
            Assert.That(OmenTable.SerpentStreamCount, Is.GreaterThan(1), "Shrimp Swarm: \"several\".");
            Assert.That(OmenTable.SerpentWeave, Is.GreaterThan(0f), "\"fly in wave-like patterns\".");
            // The ring's posts leave no way through for the hero (0.4 wide each side) while every member stands.
            float spacing = 2f * Mathf.PI * StageHazardTable.ThornStartRadius / StageHazardTable.ThornCount;
            Assert.That(spacing, Is.LessThan(2f * StageHazardTable.ThornPostRadius), "Neighbouring posts overlap.");
            Assert.That(StageHazardTable.ThornEndRadius, Is.GreaterThan(StageHazardTable.ThornPostRadius + 0.5f), "The ring never closes onto the hero.");
        }

        [Test]
        public void ASerpentStream_IsAFile_ComingFromOneSide_AlongALane()
        {
            // Dragon_Swarm: they "travel horizontally accross the map".
            var hero = new Vector3(10f, 1f, -4f);
            var fromLeft = OmenTable.SerpentPaths(hero, true, 3f, 25f, 12);
            Assert.That(fromLeft.Count, Is.EqualTo(12));
            Assert.That(fromLeft.All(p => p.direction == Vector3.right), Is.True);
            Assert.That(fromLeft[0].position.x, Is.EqualTo(hero.x - 25f).Within(1e-4f), "The head starts at the screen edge.");
            Assert.That(fromLeft.All(p => Mathf.Abs(p.position.z - (hero.z + 3f)) < 1e-4f), Is.True, "One lane.");
            for (int i = 1; i < fromLeft.Count; i++)
                Assert.That(fromLeft[i - 1].position.x - fromLeft[i].position.x, Is.EqualTo(OmenTable.SerpentSpacing).Within(1e-4f), "One behind the other.");
            var fromRight = OmenTable.SerpentPaths(hero, false, -2f, 25f, 4);
            Assert.That(fromRight.All(p => p.direction == Vector3.left && p.position.x >= hero.x + 25f - 1e-4f), Is.True);
        }

        [Test]
        public void TheWeaponList_ShowsEveryCard_AndMustBeAnswered()
        {
            // Candybox: "opens a list of all base weapons remaining to be obtained in the run".
            var cards = new List<ArcanaData>();
            try
            {
                for (int i = 0; i < 9; i++)
                {
                    var card = ScriptableObject.CreateInstance<ArcanaData>();
                    card.CardName = "Weapon " + i;
                    cards.Add(card);
                }
                var draft = new ArcanaDraft(ArcanaDraft.Kind.Weapons, cards, 0, 3, () => 0.5f);
                Assert.That(draft.Options.Count, Is.EqualTo(9), "All of them at once, not a draw of four.");
                Assert.That(draft.OffersRandom, Is.False);
                Assert.That(draft.CanSkip, Is.False);
                Assert.That(draft.HasUnshown, Is.False, "Nothing to reroll.");
                Assert.That(draft.FreeRerollsLeft, Is.EqualTo(0));
                Assert.That(cards.All(draft.CanChoose), Is.True);
                // The Arcana drafts keep their rules.
                Assert.That(new ArcanaDraft(ArcanaDraft.Kind.Start, cards, 0, 0, null).OffersRandom, Is.True);
                Assert.That(new ArcanaDraft(ArcanaDraft.Kind.Merchant, cards, 0, 0, null).OffersRandom, Is.True);
                var chest = new ArcanaDraft(ArcanaDraft.Kind.Chest, cards, 4, 0, () => 0.5f);
                Assert.That(chest.OffersRandom, Is.False);
                Assert.That(chest.CanSkip, Is.True);
            }
            finally { foreach (var card in cards) Object.DestroyImmediate(card); }
        }

        [Test]
        public void Built_TheOmenTypes_AreLevelOnesBatsAndPlants()
        {
            var catalog = GameContentCatalog.Load();
            Assert.IsNotNull(catalog);
            Assert.That(catalog.OmenBats, Is.Not.Null, "Run Soul Hunter/Content/Build Omen Content.");
            CollectionAssert.AreEqual(new[] { "SH10_L01_GraveBat_Gameplay", "SH10_L01_RedEyeBat_Gameplay", "SH10_L01_BloodEyeBat_Gameplay" },
                catalog.OmenBats.Select(p => p != null ? p.name : null).ToArray());
            CollectionAssert.AreEqual(new[] { "SH10_L01_GraveLily_Gameplay", "SH10_L01_ThornWall_Gameplay" },
                catalog.OmenPlants.Select(p => p != null ? p.name : null).ToArray());
            Assert.That(catalog.OmenThornWall != null && catalog.OmenThornWall.name == "SH10_L01_ThornWall_Gameplay", Is.True);
            foreach (var prefab in catalog.OmenBats.Concat(catalog.OmenPlants))
                Assert.That(prefab.GetComponent<EnemyController>() != null && prefab.GetComponent<EnemyController>().Data != null, Is.True, prefab.name);
            // Flower_Wall statbox, as Level 1 already has it: 30 x Level health, 1 damage.
            var wall = catalog.OmenThornWall.GetComponent<EnemyController>().Data;
            Assert.That(wall.HealthPerPlayerLevel, Is.EqualTo(30));
            Assert.That(wall.DamageToPlayer, Is.EqualTo(1));
        }

        [Test]
        public void TheCollection_IsLargeEnough_ForEveryBanishRank()
        {
            // wiki Banish: ranks at 60, 70, 80, 90 and 100 Collection entries.
            int entries = CollectionCatalog.All().Count;
            Assert.That(entries, Is.GreaterThanOrEqualTo(PowerUpUnlocks.BanishCollectionEntries.Max()),
                $"The Collection has {entries} entries; the last Banish rank needs {PowerUpUnlocks.BanishCollectionEntries.Max()}.");
            Assert.That(CollectionCatalog.All().Select(e => e.Id).Distinct().Count(), Is.EqualTo(entries));
            Assert.That(CollectionCatalog.Find(CollectionCatalog.ItemId(SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.CursedCoffer)), Is.Not.Null,
                "The Cursed Coffer has its entry.");
        }

        [Test]
        public void TheClosingArena_ShrinksOverTheLevel_AndItsDamageIsPerSecond()
        {
            // GDD, Level 9: "The safe zone continuously shrinks."
            Assert.That(CampaignSignatureSystem.ArenaRadiusAt(0f, 1800f, 35f, 10f), Is.EqualTo(35f));
            Assert.That(CampaignSignatureSystem.ArenaRadiusAt(900f, 1800f, 35f, 10f), Is.EqualTo(22.5f).Within(1e-4f));
            Assert.That(CampaignSignatureSystem.ArenaRadiusAt(1800f, 1800f, 35f, 10f), Is.EqualTo(10f));
            Assert.That(CampaignSignatureSystem.ArenaRadiusAt(5000f, 1800f, 35f, 10f), Is.EqualTo(10f), "It stops at the end radius.");
            var go = new GameObject("Signatures");
            try
            {
                var system = go.AddComponent<CampaignSignatureSystem>();
                // 10 a second in ticks of half a second: 5 per tick, whatever the frame rate (it was at least 1 per frame).
                Assert.That(system.ArenaTickDamage / CampaignSignatureSystem.ArenaTickSeconds, Is.EqualTo(10f).Within(1e-4f));
                Assert.That(system.ArenaActive, Is.False, "Only on Level 9.");
                Assert.That(system.OutsideArena(new Vector3(500f, 0f, 500f)), Is.False, "No zone off Level 9.");
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
