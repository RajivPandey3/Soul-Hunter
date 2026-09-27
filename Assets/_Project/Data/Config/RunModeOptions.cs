using SoulHunter.Core.Persistence;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>The six VS run modes the character select screen offers.</summary>
    public enum RunMode { Hurry, Hyper, Inverse, Endless, Arcana, LimitBreak }

    /// <summary>
    /// Learning Comment: bridge between the saved mode toggles (GameData, Foundation) and the run's RunModes flags
    /// (Gameplay). The character select screen writes the toggles; LevelProgressionManager copies them into RunModes when
    /// the gameplay scene starts a run, and resets RunModes when that run's scene goes away (restart, back to menu).
    /// Descriptions are Soul Hunter's own short wording of the VS mode rules.
    /// </summary>
    public static class RunModeOptions
    {
        public static readonly RunMode[] All =
            { RunMode.Hurry, RunMode.Hyper, RunMode.Inverse, RunMode.Endless, RunMode.Arcana, RunMode.LimitBreak };

        public static string Name(RunMode mode) => mode == RunMode.LimitBreak ? "Limit Break" : mode.ToString();

        public static string Description(RunMode mode)
        {
            switch (mode)
            {
                case RunMode.Hurry: return "Hurry: the stage clock runs twice as fast. +25% experience.";
                case RunMode.Hyper: return "Hyper: you and the horde move 65% faster, shots fly 15% faster. +50% gold, a bigger horde.";
                case RunMode.Inverse: return "Inverse: the horde starts at triple health and grows tougher every minute. +200% gold, +20% luck.";
                case RunMode.Endless: return "Endless: clearing Level 10 starts the story again at Level 1, and each cycle the horde grows stronger.";
                case RunMode.Arcana: return "Arcana: start with an Arcana card; Arcana chests come at 11:00 and 21:00.";
                case RunMode.LimitBreak: return "Limit Break: once everything is maxed, level-ups keep strengthening your weapons.";
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
        }
    }
}
