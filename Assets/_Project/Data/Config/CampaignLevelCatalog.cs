using System;
using UnityEngine;

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

        /// <summary>VS stage bonus: the player's move speed on this stage (CampaignLevelCatalog.StageBonuses).</summary>
        public float PlayerSpeedMultiplier => CampaignLevelCatalog.PlayerSpeedFor(Number);
        /// <summary>VS stage bonus: gold from pickups and chests on this stage, on top of Greed.</summary>
        public float GoldMultiplier => CampaignLevelCatalog.GoldFor(Number);

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
        // Enemy pace and spawn interval per level. Pace stays the owner-approved stage table (2026-09-26 crowd plan Part 4:
        // e.g. L5 "a slow, heavy horde and fast swarms" at 0.9, L6 1.45): the owner chose "VS waves + own rules"
        // (2026-10-09), and an approved decision comes before the VS stage speed (x1.25 / x1.4 on the source stages).
        // The spawn interval multiplier is 1 (release item P2-02): each minute's VS interval is used as the wiki gives it.
        private static readonly CampaignLevelDefinition[] Levels =
        {
            new(1, "The Cursed Graveyard", "Kael starts his journey near his daughter's grave, harvesting wandering souls.", "Teach movement, dash, souls and auto-attacks", CampaignSignature.CursedGraveyard, "SH10_L01_Showcase", 1.00f, 1.00f),
            new(2, "The Dark Forest", "The souls run in fear. Kael realizes the forest is rejecting his dark presence.", "Darkness accelerates enemy pressure", CampaignSignature.DarkForest, "SH10_L02_Showcase", 1.10f, 1.00f),
            new(3, "The Forgotten Village", "Kael encounters the remnants of a village burned down by demons.", "A Wandering Merchant sells weapons and Soul Eggs for gold", CampaignSignature.ForgottenVillage, "SH10_L03_Showcase", 1.15f, 1.00f),
            new(4, "The Ruined Castle", "The gateway to the Underworld. The Gatekeeper warns Kael that he is being deceived.", "Castle corridors: gargoyles come mostly from the left and right", CampaignSignature.RuinedCastle, "SH10_L04_Showcase", 1.20f, 1.00f),
            new(5, "The Crimson Swamp", "Kael's soul begins to rot. He hears whispers and voices.", "A slow, heavy horde and fast swarms", CampaignSignature.CrimsonSwamp, "SH10_L05_Showcase", 0.90f, 1.00f),
            new(6, "The Frozen Peaks", "The icy descent into the deep underworld. The cold reflects Kael's fading humanity.", "Blizzards and periodic movement slow; fast enemies from above and below", CampaignSignature.FrozenPeaks, "SH10_L06_Showcase", 1.45f, 1.00f),
            new(7, "The Sea of Lost Souls", "The resting place of souls who gave up. Kael starts losing his sanity.", "Spectral enemies and holy-damage checks", CampaignSignature.SeaOfLostSouls, "SH10_L07_Showcase", 1.30f, 1.00f),
            new(8, "The Blood Arenas", "The Death God's elite guards test Kael's worthiness.", "Elite waves replace ordinary swarms", CampaignSignature.BloodArenas, "SH10_L08_Showcase", 1.40f, 1.00f),
            new(9, "The Throne of Death", "The 99,999th soul is collected. Kael approaches the final throne.", "Arena boundary contracts over time", CampaignSignature.ThroneOfDeath, "SH10_L09_Showcase", 1.50f, 1.00f),
            new(10, "The Soul Core", "The truth is revealed. Kael is the monster. He must fight his own corrupted soul.", "Shadow Kael final skill-check encounter", CampaignSignature.SoulCore, "SH10_L10_Showcase", 1.60f, 1.00f)
        };

        public static int Count => Levels.Length;

        // VS stage bonuses (vampire.survivors.wiki/w/Stages, fetched 2026-09-27): every stage has its own player move
        // speed and gold multipliers, growing along the stage list (Mad Forest x1.1 speed; Inlaid Library x1.25;
        // Dairy Plant x1.25 and x1.2 gold; Gallo Tower x1.3 gold; Cappella Magna x1.4 both; the DLC stages x1.4 and
        // x1.5 gold). Soul Hunter's ten stages follow that climb, in story order. Provisional balance values.
        private static readonly float[] PlayerSpeedBonus = { 1.10f, 1.25f, 1.25f, 1.25f, 1.40f, 1.40f, 1.40f, 1.40f, 1.40f, 1.40f };
        private static readonly float[] GoldBonus = { 1.00f, 1.00f, 1.20f, 1.30f, 1.40f, 1.50f, 1.50f, 1.50f, 1.50f, 1.50f };

        public static float PlayerSpeedFor(int stage) => PlayerSpeedBonus[Mathf.Clamp(stage, 1, PlayerSpeedBonus.Length) - 1];
        public static float GoldFor(int stage) => GoldBonus[Mathf.Clamp(stage, 1, GoldBonus.Length) - 1];

        // VS Hyper mode, per stage (2026-10-09). The Stages page: "The overall bonuses granted by hyper mode differ with
        // each stage". Each value is the stage infobox's Hyper column (raw pages fetched 2026-10-09), on the same mapping
        // as the stage bonuses above: L1 Mad Forest, L2 Inlaid Library, L3 Dairy Plant, L4 Gallo Tower, L5-L10 Cappella
        // Magna (a Soul Hunter choice, not a wiki fact).
        // - Player speed (absolute, replaces the normal value): Mad Forest "player-speed2 = 2"; Inlaid Library, Dairy
        //   Plant and Gallo Tower "x1.9"; Cappella Magna "x2". The Stages page lists the same: +100%, +90%, +90%, +90%,
        //   +100% Move Speed.
        // - Projectile speed: Mad Forest "projectile-speed2 = 1.25"; the others "x1.15".
        // - Luck: "x1.1" on the four later stages (their pages: "+10% Luck bonuses", added like Inverse's +20%); Mad
        //   Forest: "the only stage that does not grant a Luck bonus in its hyper mode".
        // - Enemy speed: the ratio hyper / normal of the infobox's enemy-speed, applied on top of Soul Hunter's own stage
        //   pace (CampaignLevelDefinition.EnemySpeedMultiplier): Mad Forest 2 / 1.1, Dairy Plant and Gallo Tower
        //   1.9 / 1.25, Cappella Magna 2 / 1.4. Inlaid Library gives x1.25 in both columns (ratio 1), which contradicts
        //   the Enemies page ("Hyper mode in each stage increases ... the movement speed of enemies"). Balance method
        //   (owner rule 2026-10-09): every other base stage's Hyper enemy speed equals its Hyper player speed, and Dairy
        //   Plant and Gallo Tower share Inlaid Library's normal x1.25, so Level 2 uses their 1.9 / 1.25.
        private static readonly float[] HyperPlayerSpeed = { 2.0f, 1.9f, 1.9f, 1.9f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f };
        private static readonly float[] HyperProjectileSpeed = { 1.25f, 1.15f, 1.15f, 1.15f, 1.15f, 1.15f, 1.15f, 1.15f, 1.15f, 1.15f };
        private static readonly float[] HyperLuck = { 0f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };
        private static readonly float[] HyperEnemySpeed =
        {
            2.0f / 1.1f, 1.9f / 1.25f, 1.9f / 1.25f, 1.9f / 1.25f, 2.0f / 1.4f, 2.0f / 1.4f, 2.0f / 1.4f, 2.0f / 1.4f, 2.0f / 1.4f, 2.0f / 1.4f
        };

        public static float HyperPlayerSpeedFor(int stage) => HyperPlayerSpeed[Mathf.Clamp(stage, 1, HyperPlayerSpeed.Length) - 1];
        public static float HyperProjectileSpeedFor(int stage) => HyperProjectileSpeed[Mathf.Clamp(stage, 1, HyperProjectileSpeed.Length) - 1];
        public static float HyperLuckFor(int stage) => HyperLuck[Mathf.Clamp(stage, 1, HyperLuck.Length) - 1];
        public static float HyperEnemySpeedFor(int stage) => HyperEnemySpeed[Mathf.Clamp(stage, 1, HyperEnemySpeed.Length) - 1];

        /// <summary>The bonuses of the stage being played, or none outside a run (menus, EditMode tests). In Hyper mode
        /// the stage's Hyper move speed replaces its normal one (VS infobox player-speed2).</summary>
        public static float CurrentPlayerSpeed => CurrentStage > 0
            ? (RunModes.Hyper ? HyperPlayerSpeedFor(CurrentStage) : PlayerSpeedFor(CurrentStage))
            : 1f;
        /// <summary>VS: the stage's gold bonus and the run modes' gold bonuses add together.</summary>
        public static float CurrentGold => (CurrentStage > 0 ? GoldFor(CurrentStage) : 1f) + RunModes.GoldBonus;

        /// <summary>Set by LevelProgressionManager when a stage starts; 0 outside a run.</summary>
        public static int CurrentStage { get; set; }

        public static CampaignLevelDefinition Get(int stage)
        {
            int index = Math.Max(1, Math.Min(stage, Levels.Length)) - 1;
            return Levels[index];
        }
    }
}
