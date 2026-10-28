using System.Linq;
using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using UnityEngine;
using Kind = SoulHunter.Gameplay.Pickups.StageItems.Kind;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>VS stage items: four items on the diagonals a few tiles from each stage's start.</summary>
    public class StageItemTests
    {
        [Test]
        public void EveryStage_PlansOnePassive_OneWeapon_AChicken_AndAVacuumOrOrologion_OnTheDiagonals()
        {
            var upgrades = GameContentCatalog.Load().Upgrades;
            var passives = new System.Collections.Generic.HashSet<UpgradeData.UpgradeType>();
            for (int stage = 1; stage <= 10; stage++)
            {
                var full = StageItems.Plan(stage);
                // 2026-10-12: plus the two rings on every stage, and Miser's Idol on Level 2 (StageItems.MisersIdolStage).
                bool idolStage = stage == StageItems.MisersIdolStage || stage == StageItems.MisersIdolWestStage || stage == StageItems.MisersIdolNorthEastStage;
                int expected = idolStage ? 9 : 8;
                // 2026-10-28: Cappella Magna's Crown, Tirajisu, Duplicator and three Rosaries on Levels 8-10.
                if (stage >= StageItems.CappellaFirstStage) expected += 3 + StageItems.CappellaRosaries;
                Assert.That(full.Count, Is.EqualTo(expected), $"Stage {stage}: four diagonal items, the two Metaglios and the two rings.");
                var silver = full.Single(e => e.Upgrade == UpgradeData.UpgradeType.RingOfLongHours);
                var gold = full.Single(e => e.Upgrade == UpgradeData.UpgradeType.RingOfDarkHours);
                Assert.That(silver.Offset, Is.EqualTo(Vector3.forward * StageItems.RingDistance), "VS Silver Ring: north of the start.");
                Assert.That(gold.Offset, Is.EqualTo(Vector3.back * StageItems.RingDistance), "VS Gold Ring: south of the start.");
                Assert.That(full.Count(e => e.Upgrade == UpgradeData.UpgradeType.MisersIdol), Is.EqualTo(idolStage ? 1 : 0));
                // VS Metaglios (2026-10-01): Left west of the start, Right east.
                var left = full.Single(e => e.Upgrade == UpgradeData.UpgradeType.MetaglioLeft);
                var right = full.Single(e => e.Upgrade == UpgradeData.UpgradeType.MetaglioRight);
                Assert.That(left.Offset.x, Is.LessThan(0f).And.EqualTo(-StageItems.MetaglioDistance).Within(0.01f), "Metaglio Left lies west.");
                Assert.That(right.Offset.x, Is.EqualTo(StageItems.MetaglioDistance).Within(0.01f), "Metaglio Right lies east.");
                var plan = full.Where(e => (!UpgradeRarity.IsStageItemOnly(e.Upgrade) && e.Upgrade != UpgradeData.UpgradeType.MisersIdol) || e.Kind != Kind.Passive)
                    .Take(4).ToList(); // the four diagonal items come first; Cappella Magna's own items follow on Levels 8-10
                Assert.That(plan.Count, Is.EqualTo(4), $"Stage {stage}");
                Assert.That(plan.Count(e => e.Kind == Kind.Passive), Is.EqualTo(1));
                Assert.That(plan.Count(e => e.Kind == Kind.Weapon), Is.EqualTo(1));
                Assert.That(plan.Count(e => e.Kind == Kind.Chicken), Is.EqualTo(1));
                Assert.That(plan.Count(e => e.Kind == Kind.Magnet || e.Kind == Kind.TimeFreeze), Is.EqualTo(1));

                var directions = plan.Select(e => Mathf.Round(Mathf.Atan2(e.Offset.x, e.Offset.z) * Mathf.Rad2Deg)).ToList();
                Assert.That(directions.Distinct().Count(), Is.EqualTo(4), "One item per diagonal.");
                foreach (var e in plan)
                {
                    Assert.That(Mathf.Abs(Mathf.Abs(e.Offset.x) - Mathf.Abs(e.Offset.z)), Is.LessThan(0.01f), "On a diagonal.");
                    Assert.That(e.Offset.y, Is.EqualTo(0f));
                    Assert.That(e.Offset.magnitude, Is.InRange(55f - 0.01f, 85f + 0.01f), "A few tiles out (a tile is 20 units).");
                    if (e.Kind == Kind.Passive || e.Kind == Kind.Weapon)
                        Assert.That(upgrades.Any(u => u != null && u.Type == e.Upgrade && u.Level == 1), Is.True, $"{e.Upgrade} has a level-1 upgrade.");
                }
                passives.Add(plan.First(e => e.Kind == Kind.Passive).Upgrade);
            }
            Assert.That(passives.Count, Is.EqualTo(10), "Each stage offers a different passive.");
        }
    
        [Test]
        public void CappellaMagnasStageItems_LieOnLevelsEightToTen_NorthAndSouth()
        {
            foreach (int level in new[] { 8, 9, 10 })
            {
                var plan = StageItems.Plan(level);
                var crown = plan.Single(e => e.Upgrade == SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Crown && e.Offset.z > 0f);
                var tira = plan.Single(e => e.Upgrade == SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Tiragisu);
                Assert.That(crown.Offset.z, Is.EqualTo(StageItems.CappellaItemDistance), "Crown: north of the starting area.");
                Assert.That(tira.Offset.z, Is.GreaterThan(0f), "Tirajisu: north.");
                Assert.That(plan.Any(e => e.Upgrade == SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType.Duplicator && e.Offset.z < 0f), Is.True, "Duplicator: south.");
                var rosaries = plan.Where(e => e.Kind == StageItems.Kind.SoulToll).Select(e => e.Offset.z).ToArray();
                Assert.That(rosaries, Is.EqualTo(new[] { -100f, -120f, -140f }), "Three Rosaries south, a tile apart.");
            }
            Assert.That(StageItems.Plan(7).Any(e => e.Kind == StageItems.Kind.SoulToll), Is.False);
        }
    }
}
