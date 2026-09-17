using System;

namespace SoulHunter.Gameplay.Data
{
    public enum CampaignSignature
    {
        CursedGraveyard,
        DarkForest,
        ForgottenVillage,
        RuinedCastle,
        CrimsonSwamp,
        FrozenPeaks,
        SeaOfLostSouls,
        BloodArenas,
        ThroneOfDeath,
        SoulCore
    }

    [Serializable]
    public readonly struct CampaignLevelDefinition
    {
        public readonly int Number;
        public readonly string Name;
        public readonly string Narrative;
        public readonly string Modifier;
        public readonly float EnemySpeedMultiplier;
        public readonly float SpawnIntervalMultiplier;
        public readonly float RunDurationSeconds;
        public readonly float BossStartSeconds;
        public readonly CampaignSignature Signature;
        public readonly string ShowcaseSceneName;

        public CampaignLevelDefinition(int number, string name, string narrative, string modifier,
            CampaignSignature signature, string showcaseSceneName,
            float enemySpeedMultiplier = 1f, float spawnIntervalMultiplier = 1f,
            float runDurationSeconds = 1800f, float bossStartSeconds = 1500f)
        {
            Number = number;
            Name = name;
            Narrative = narrative;
            Modifier = modifier;
            EnemySpeedMultiplier = enemySpeedMultiplier;
            SpawnIntervalMultiplier = spawnIntervalMultiplier;
            RunDurationSeconds = runDurationSeconds;
            BossStartSeconds = bossStartSeconds;
            Signature = signature;
            ShowcaseSceneName = showcaseSceneName;
        }
    }

    /// <summary>Canonical ten-stage Soul-Hunter campaign catalog.</summary>
    public static class CampaignLevelCatalog
    {
        private static readonly CampaignLevelDefinition[] Levels =
        {
            new(1, "The Cursed Graveyard", "Kael starts beside his daughter's grave.", "Teach movement, dash, souls and auto-attacks", CampaignSignature.CursedGraveyard, "SH10_L01_Showcase", 1.00f, 1.00f),
            new(2, "The Dark Forest", "The forest rejects Kael's presence.", "Darkness accelerates enemy pressure", CampaignSignature.DarkForest, "SH10_L02_Showcase", 1.10f, 0.95f),
            new(3, "The Forgotten Village", "Kael finds the remains of a village burned by demons.", "Wandering Merchant placeholder", CampaignSignature.ForgottenVillage, "SH10_L03_Showcase", 1.15f, 0.90f),
            new(4, "The Ruined Castle", "The Gatekeeper warns Kael about deception.", "Gargoyles, traps and elite encounters", CampaignSignature.RuinedCastle, "SH10_L04_Showcase", 1.20f, 0.88f),
            new(5, "The Crimson Swamp", "Kael's soul begins to rot.", "Poison zones and corruption effects", CampaignSignature.CrimsonSwamp, "SH10_L05_Showcase", 1.25f, 0.85f),
            new(6, "The Frozen Peaks", "The cold reflects Kael's fading humanity.", "Blizzards and periodic movement slow", CampaignSignature.FrozenPeaks, "SH10_L06_Showcase", 1.15f, 0.82f),
            new(7, "The Sea of Lost Souls", "The lost dead erode Kael's sanity.", "Spectral enemies and holy-damage checks", CampaignSignature.SeaOfLostSouls, "SH10_L07_Showcase", 1.30f, 0.80f),
            new(8, "The Blood Arenas", "The Death God's elite guards test Kael.", "Elite waves replace ordinary swarms", CampaignSignature.BloodArenas, "SH10_L08_Showcase", 1.40f, 0.75f),
            new(9, "The Throne of Death", "Kael reaches the 99,999th soul.", "Arena boundary contracts over time", CampaignSignature.ThroneOfDeath, "SH10_L09_Showcase", 1.50f, 0.70f),
            new(10, "The Soul Core", "The mirror reveals Kael as the Death God.", "Shadow Kael final skill-check encounter", CampaignSignature.SoulCore, "SH10_L10_Showcase", 1.60f, 0.65f)
        };

        public static int Count => Levels.Length;

        public static CampaignLevelDefinition Get(int stage)
        {
            int index = Math.Max(1, Math.Min(stage, Levels.Length)) - 1;
            return Levels[index];
        }
    }
}
