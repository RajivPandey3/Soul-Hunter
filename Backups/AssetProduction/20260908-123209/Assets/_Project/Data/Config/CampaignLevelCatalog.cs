using System;

namespace SoulHunter.Gameplay.Data
{
    [Serializable]
    public readonly struct CampaignLevelDefinition
    {
        public readonly int Number;
        public readonly string Name;
        public readonly string Narrative;
        public readonly string Modifier;
        public readonly float EnemySpeedMultiplier;
        public readonly float SpawnIntervalMultiplier;

        public CampaignLevelDefinition(int number, string name, string narrative, string modifier, float enemySpeedMultiplier = 1f, float spawnIntervalMultiplier = 1f)
        {
            Number = number;
            Name = name;
            Narrative = narrative;
            Modifier = modifier;
            EnemySpeedMultiplier = enemySpeedMultiplier;
            SpawnIntervalMultiplier = spawnIntervalMultiplier;
        }
    }

    /// <summary>Canonical ten-stage Soul-Hunter campaign catalog.</summary>
    public static class CampaignLevelCatalog
    {
        private static readonly CampaignLevelDefinition[] Levels =
        {
            new(1, "The Cursed Graveyard", "Kael starts beside his daughter's grave.", "Teach movement, dash, souls and auto-attacks", 1.00f, 1.00f),
            new(2, "The Dark Forest", "The forest rejects Kael's presence.", "Darkness accelerates enemy pressure", 1.10f, 0.95f),
            new(3, "The Burning Village", "Kael finds the remains of a village burned by demons.", "Fire hazards and dense swarm waves", 1.15f, 0.90f),
            new(4, "The Ruined Castle", "The Gatekeeper warns Kael about deception.", "Gargoyles, traps and elite encounters", 1.20f, 0.88f),
            new(5, "The Crimson Swamp", "Kael's soul begins to rot.", "Poison zones and corruption effects", 1.25f, 0.85f),
            new(6, "The Frozen Peaks", "The cold reflects Kael's fading humanity.", "Blizzards and periodic movement slow", 1.15f, 0.82f),
            new(7, "The Sea of Lost Souls", "The lost dead erode Kael's sanity.", "Spectral enemies and holy-damage checks", 1.30f, 0.80f),
            new(8, "The Blood Arenas", "The Death God's elite guards test Kael.", "Elite waves replace ordinary swarms", 1.40f, 0.75f),
            new(9, "The Throne of Death", "Kael reaches the 99,999th soul.", "Arena boundary contracts over time", 1.50f, 0.70f),
            new(10, "The Soul Core", "The mirror reveals Kael as the Death God.", "Shadow Kael final skill-check encounter", 1.60f, 0.65f)
        };

        public static int Count => Levels.Length;

        public static CampaignLevelDefinition Get(int stage)
        {
            int index = Math.Max(1, Math.Min(stage, Levels.Length)) - 1;
            return Levels[index];
        }
    }
}

