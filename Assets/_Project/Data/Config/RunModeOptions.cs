using SoulHunter.Core.Persistence;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// The VS run modes the character select screen offers: the six modes, then the two VS Randomizers (release item
    /// P3-05, owner 2026-10-09: unlocked from the start like the others). New values go at the end.
    /// </summary>
    public enum RunMode { Hurry, Hyper, Inverse, Endless, Arcana, LimitBreak, RandomEvents, RandomLevelUp }

    /// <summary>
    /// Learning Comment: bridge between the saved mode toggles (GameData, Foundation) and the run's RunModes flags
    /// (Gameplay). The character select screen writes the toggles; LevelProgressionManager copies them into RunModes when
    /// the gameplay scene starts a run, and resets RunModes when that run's scene goes away (restart, back to menu).
    /// Descriptions are Soul Hunter's own short wording of the VS mode rules.
    /// </summary>
    public static class RunModeOptions
    {
        public static readonly RunMode[] All =
            { RunMode.Hurry, RunMode.Hyper, RunMode.Inverse, RunMode.Endless, RunMode.Arcana, RunMode.LimitBreak,
              RunMode.RandomEvents, RunMode.RandomLevelUp };

        /// <summary>
        /// The names the screens show. The two Randomizers get Soul Hunter's own names (no VS names for new things):
        /// Omens (VS Random Events) and Blind Fate (VS Random LevelUp).
        /// </summary>
        public static string Name(RunMode mode)
        {
            switch (mode)
            {
                case RunMode.LimitBreak: return "Limit Break";
                case RunMode.RandomEvents: return "Omens";
                case RunMode.RandomLevelUp: return "Blind Fate";
            }
            return mode.ToString();
        }

        public static string Description(RunMode mode)
        {
            switch (mode)
            {
                case RunMode.Hurry: return "Hurry: the stage clock runs twice as fast. +25% experience.";
                case RunMode.Hyper: return "Hyper: you and the horde move faster and shots fly faster, by stage. +50% gold, +10% Luck after Level 1, a bigger horde.";
                case RunMode.Inverse: return "Inverse: the horde starts at triple health and grows tougher every minute. +200% gold, +20% luck.";
                case RunMode.Endless: return "Endless: clearing Level 10 starts the story again at Level 1, and each cycle the horde grows stronger.";
                case RunMode.Arcana: return "Arcana: start with an Arcana card; Arcana chests come at 11:00 and 21:00.";
                case RunMode.LimitBreak: return "Limit Break: once everything is maxed, level-ups keep strengthening your weapons.";
                case RunMode.RandomEvents: return "Omens: at every half minute a wheel picks an event, and it happens on the next full minute.";
                case RunMode.RandomLevelUp: return "Blind Fate: every level-up picks for you at random. You can switch it in the pause menu.";
            }
            return "";
        }

        public static bool Get(GameData data, RunMode mode)
        {
            if (data == null) return false;
            switch (mode)
            {
                case RunMode.Hurry: return data.ModeHurry;
                case RunMode.Hyper: return data.ModeHyper;
                case RunMode.Inverse: return data.ModeInverse;
                case RunMode.Endless: return data.ModeEndless;
                case RunMode.Arcana: return data.ModeArcana;
                case RunMode.LimitBreak: return data.ModeLimitBreak;
                case RunMode.RandomEvents: return data.ModeRandomEvents;
                case RunMode.RandomLevelUp: return data.ModeRandomLevelUp;
            }
            return false;
        }

        public static void Set(GameData data, RunMode mode, bool on)
        {
            if (data == null) return;
            switch (mode)
            {
                case RunMode.Hurry: data.ModeHurry = on; break;
                case RunMode.Hyper: data.ModeHyper = on; break;
                case RunMode.Inverse: data.ModeInverse = on; break;
                case RunMode.Endless: data.ModeEndless = on; break;
                case RunMode.Arcana: data.ModeArcana = on; break;
                case RunMode.LimitBreak: data.ModeLimitBreak = on; break;
                case RunMode.RandomEvents: data.ModeRandomEvents = on; break;
                case RunMode.RandomLevelUp: data.ModeRandomLevelUp = on; break;
            }
        }

        /// <summary>The run flag of one mode, as RunModes holds it right now.</summary>
        public static bool IsActive(RunMode mode)
        {
            switch (mode)
            {
                case RunMode.Hurry: return RunModes.Hurry;
                case RunMode.Hyper: return RunModes.Hyper;
                case RunMode.Inverse: return RunModes.Inverse;
                case RunMode.Endless: return RunModes.Endless;
                case RunMode.Arcana: return RunModes.Arcana;
                case RunMode.LimitBreak: return RunModes.LimitBreak;
                case RunMode.RandomEvents: return RunModes.RandomEvents;
                case RunMode.RandomLevelUp: return RunModes.RandomLevelUp;
            }
            return false;
        }

        /// <summary>Starts a run's modes: clears everything left from an earlier run, then copies the saved toggles.</summary>
        public static void BeginRun(GameData data)
        {
            RunModes.Reset();
            if (data == null) return;
            RunModes.Hurry = data.ModeHurry;
            RunModes.Hyper = data.ModeHyper;
            RunModes.Inverse = data.ModeInverse;
            RunModes.Endless = data.ModeEndless;
            RunModes.Arcana = data.ModeArcana;
            RunModes.LimitBreak = data.ModeLimitBreak;
            RunModes.RandomEvents = data.ModeRandomEvents;
            RunModes.RandomLevelUp = data.ModeRandomLevelUp;
        }
    }
}
