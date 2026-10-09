using UnityEngine;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// VS run modes (vampire.survivors.wiki/w/Stages, "List of Modes", fetched 2026-09-27), chosen on the character
    /// select screen and fixed for the run. Owner, 2026-09-27: "extra feature also continue". Soul Hunter plays the ten
    /// stages as one run, so each mode applies to the whole run. The multipliers below are the VS ones; Soul Hunter
    /// numbers are marked provisional.
    /// - Hurry: the stage clock runs twice as fast; XP +25%. Events still come at their clock times.
    /// - Hyper (per stage, 2026-10-09; each stage's wiki infobox, see CampaignLevelCatalog.HyperPlayerSpeedFor): player
    ///   move speed, enemy speed (the VS hyper/normal ratio on top of Soul Hunter's own stage pace), projectile speed
    ///   and Luck follow the stage; gold +50%; enemy minimum x1.25. No stage infobox and no wiki page gives Hyper a
    ///   spawn-interval change, so Hyper no longer shortens the interval (it used to divide it by 1.25).
    /// - Inverse: enemies start at +200% health and gain +5% health and +0.5% speed per stage minute; gold +200%,
    ///   Luck +20%. VS also turns the map 180 degrees and darkens it: InverseMap (P3-05), with the Options switch
    ///   "Visually Invert Stages".
    /// - Endless: the run does not end at Level 10; it starts again at Level 1 as a new cycle. Each cycle: enemy
    ///   health +100%, spawn rate and count +50%, enemy damage +25%.
    /// - Arcana: Arcana chests at 11:00 and 21:00 (VS). - Limit Break: once everything is maxed, level-ups keep
    ///   strengthening weapons instead of paying gold.
    /// </summary>
    public static class RunModes
    {
        public static bool Hurry, Hyper, Inverse, Endless, Arcana, LimitBreak;

        /// <summary>
        /// VS Randomizers (wiki Stages, "Randomizers", fetched 2026-10-09): "Random Events ... At the 30-second mark of every
        /// minute, the roulette selects a random event, which then triggers at the start of the following minute" (Soul
        /// Hunter: Omens, RandomEventDirector); "Random LevelUp ... the game automatically designates a random weapon or
        /// passive item to be added or upgraded" (Soul Hunter: Blind Fate, LevelUpManager). Owner (2026-10-09): unlocked from
        /// the start, like the other six modes. Random LevelUp "can now be toggled during a run" (wiki Level_up, 1 April 2026
        /// patch note), so the pause menu can change RandomLevelUp mid-run.
        /// </summary>
        public static bool RandomEvents, RandomLevelUp;

        /// <summary>
        /// Spawn-rate factor of a running Omens "Swelling Horde" (VS Ultra Wave: "Increases the enemy spawn rate for 1
        /// minute"); 1 otherwise. RandomEventDirector sets and clears it; FrequencyMultiplier applies it.
        /// </summary>
        public static float EventFrequencyBoost = 1f;

        /// <summary>Endless cycle: 0 on the first pass through the ten stages, then 1, 2, ...</summary>
        public static int EndlessCycle;

        public static void Reset()
        {
            Hurry = Hyper = Inverse = Endless = Arcana = LimitBreak = false;
            RandomEvents = RandomLevelUp = false;
            EventFrequencyBoost = 1f;
            EndlessCycle = 0;
        }

        public static float ClockSpeed => Hurry ? 2f : 1f;
        public static float XpMultiplier => Hurry ? 1.25f : 1f;

        // Hyper player move speed is a per-stage value, not a multiplier: CampaignLevelCatalog.CurrentPlayerSpeed.
        /// <summary>VS Hyper projectile speed of the stage being played (Mad Forest x1.25, the others x1.15).</summary>
        public static float ProjectileSpeedMultiplier =>
            Hyper ? CampaignLevelCatalog.HyperProjectileSpeedFor(CampaignLevelCatalog.CurrentStage) : 1f;

        /// <summary>VS: a stage's and its modes' gold bonuses add together.</summary>
        public static float GoldBonus => (Hyper ? 0.5f : 0f) + (Inverse ? 2f : 0f);
        /// <summary>The run-long Luck bonus, given once at the start (VS Inverse +20%). Hyper Luck depends on the stage:
        /// HyperLuckBonus, applied by LevelProgressionManager when each stage starts.</summary>
        public static float LuckBonus => Inverse ? 0.2f : 0f;

        /// <summary>VS Hyper Luck of the stage being played: +10% on every mapped stage except Mad Forest (Level 1).</summary>
        public static float HyperLuckBonus(int stage) => Hyper ? CampaignLevelCatalog.HyperLuckFor(stage) : 0f;

        /// <summary>
        /// Enemy speed from the modes, on top of the stage's own pace: Hyper uses the VS ratio of the stage's hyper and
        /// normal enemy speed (CampaignLevelCatalog.HyperEnemySpeedFor); Inverse adds +0.5% per stage minute.
        /// </summary>
        public static float EnemySpeedMultiplier(float stageMinutes) =>
            (Hyper ? CampaignLevelCatalog.HyperEnemySpeedFor(CampaignLevelCatalog.CurrentStage) : 1f)
            * (Inverse ? 1f + 0.005f * Mathf.Max(0f, stageMinutes) : 1f);

        /// <summary>
        /// Enemy health: Inverse x(3 + 0.05 per minute) (stage infoboxes enemy-health3 = 3, enemy-health per min3 = +0.05) and
        /// Endless +100% of base health per cycle. They add: Endless raises "their base HP" and Inverse bonuses are "additive
        /// with other enemy bonus modifiers" (wiki Stages; how the two stack is not stated, balance method 2026-10-09).
        /// </summary>
        public static float EnemyHealthMultiplier(float stageMinutes) =>
            (Inverse ? 3f + 0.05f * Mathf.Max(0f, stageMinutes) : 1f) + Mathf.Max(0, EndlessCycle);

        public static float EnemyDamageMultiplier => 1f + 0.25f * Mathf.Max(0, EndlessCycle);

        /// <summary>Enemy minimum: Hyper x1.25 (every stage infobox, enemy-min2); each Endless cycle +50% (Stages).</summary>
        public static float MinimumMultiplier => (Hyper ? 1.25f : 1f) * EndlessSpawnMultiplier;

        /// <summary>Spawn frequency: each Endless cycle +50% (Stages), times a running Omens spawn boost. Hyper has no
        /// spawn-interval value on the wiki.</summary>
        public static float FrequencyMultiplier => EndlessSpawnMultiplier * Mathf.Max(1f, EventFrequencyBoost);

        private static float EndlessSpawnMultiplier => 1f + 0.5f * Mathf.Max(0, EndlessCycle);
    }
}
