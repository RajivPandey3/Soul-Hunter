using System.Collections.Generic;
using NUnit.Framework;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: CampaignLevelCatalogTests verifies that CampaignLevelCatalog fulfills all core campaign contracts:
    /// exactly 10 stages, unique signatures, non-empty names/narratives, valid positive multipliers, and correct boundary clamping.
    /// </summary>
    public class CampaignLevelCatalogTests
    {
        [Test]
        public void Count_EqualsTenStages()
        {
            // Learning Comment: CampaignLevelCatalog 10 canonical stages ko represent karta hai, isliye total count 10 hona zaroori hai.
            Assert.That(CampaignLevelCatalog.Count, Is.EqualTo(10));
        }

        [Test]
        public void AllStages_HaveUniqueCampaignSignatures()
        {
            // Learning Comment: Har stage ka ek distinct mechanic/signature hona chahiye taake gameplay uniqueness bani rahe.
            var signatures = new HashSet<CampaignSignature>();
            for (int stage = 1; stage <= CampaignLevelCatalog.Count; stage++)
            {
                var level = CampaignLevelCatalog.Get(stage);
                bool added = signatures.Add(level.Signature);
                Assert.That(added, Is.True, $"Stage {stage} signature '{level.Signature}' duplicate nahi honi chahiye.");
            }

            Assert.That(signatures.Count, Is.EqualTo(10));
        }

        [Test]
        public void AllStages_HaveNonEmptyNameAndNarrativeStrings()
        {
            // Learning Comment: Har campaign stage ka valid display name aur narrative lore description hona zaroori hai.
            for (int stage = 1; stage <= CampaignLevelCatalog.Count; stage++)
            {
                var level = CampaignLevelCatalog.Get(stage);
                Assert.That(string.IsNullOrWhiteSpace(level.Name), Is.False, $"Stage {stage} ka Name non-empty hona chahiye.");
                Assert.That(string.IsNullOrWhiteSpace(level.Narrative), Is.False, $"Stage {stage} ka Narrative non-empty hona chahiye.");
            }
        }

        [Test]
        public void AllStages_HavePositiveSpeedAndSpawnMultipliers()
        {
            // Learning Comment: Multipliers hamesha strictly positive (> 0) hone chahiye taake enemy movement aur spawn rate broken/zero na ho.
            for (int stage = 1; stage <= CampaignLevelCatalog.Count; stage++)
            {
                var level = CampaignLevelCatalog.Get(stage);
                Assert.That(level.EnemySpeedMultiplier, Is.GreaterThan(0f), $"Stage {stage} EnemySpeedMultiplier strictly positive hona chahiye.");
                Assert.That(level.SpawnIntervalMultiplier, Is.GreaterThan(0f), $"Stage {stage} SpawnIntervalMultiplier strictly positive hona chahiye.");
            }
        }

        [Test]
        public void Get_ClampsIndexZeroToStageOne_AndIndexElevenToStageTen()
        {
            // Learning Comment: CampaignLevelCatalog.Get out-of-range stage numbers ko clamp karta hai:
            // 0 stage 1 (CursedGraveyard) pe clamp hona chahiye, aur 11 stage 10 (SoulCore) pe.
            var stageZero = CampaignLevelCatalog.Get(0);
            var stageOne = CampaignLevelCatalog.Get(1);
            Assert.That(stageZero.Number, Is.EqualTo(1));
            Assert.That(stageZero.Signature, Is.EqualTo(CampaignSignature.CursedGraveyard));
            Assert.That(stageZero.Signature, Is.EqualTo(stageOne.Signature));

            var stageEleven = CampaignLevelCatalog.Get(11);
            var stageTen = CampaignLevelCatalog.Get(10);
            Assert.That(stageEleven.Number, Is.EqualTo(10));
            Assert.That(stageEleven.Signature, Is.EqualTo(CampaignSignature.SoulCore));
            Assert.That(stageEleven.Signature, Is.EqualTo(stageTen.Signature));
        }
    }
}
