using System.Collections.Generic;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Number and table rules of the VS Results screen (vampire.survivors.wiki/w/Results, fetched 2026-10-02): damage is
    /// written with SI prefixes up to E (exa), DPS is damage per second of time used, and the damage and DPS columns each
    /// show the one weapon with the most in yellow. The wiki gives no decimals; three significant digits are Soul Hunter's.
    /// </summary>
    public static class RunResultsFormat
    {
        private static readonly string[] Prefixes = { "", "k", "M", "G", "T", "P", "E" };

        /// <summary>1234 -> "1.23k", 45600 -> "45.6k", 999 -> "999". Up to E, as the wiki lists.</summary>
        public static string Damage(double amount)
        {
            if (amount < 0) amount = 0;
            int prefix = 0;
            while (amount >= 1000d && prefix < Prefixes.Length - 1) { amount /= 1000d; prefix++; }
            if (prefix == 0) return ((long)System.Math.Floor(amount)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            // Three significant digits, rounded down so 999.9k never reads as 1000k.
            string text = amount >= 100d ? System.Math.Floor(amount).ToString("0", System.Globalization.CultureInfo.InvariantCulture)
                : amount >= 10d ? (System.Math.Floor(amount * 10d) / 10d).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                : (System.Math.Floor(amount * 100d) / 100d).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            return text + Prefixes[prefix];
        }

        /// <summary>Damage per second of time used; 0 when the weapon was held for no time.</summary>
        public static double Dps(double damage, float secondsUsed) => secondsUsed > 0.01f ? damage / secondsUsed : 0d;

        /// <summary>"m:ss", or "h:mm:ss" from an hour (a ten-stage run is about five hours).</summary>
        public static string Time(float seconds)
        {
            int total = seconds > 0f ? (int)seconds : 0;
            int h = total / 3600, m = total / 60 % 60, s = total % 60;
            return h > 0 ? $"{h}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
        }

        /// <summary>Index of the largest value (the yellow row), or -1 when every value is 0.</summary>
        public static int Highest(IReadOnlyList<double> values)
        {
            int best = -1;
            double bestValue = 0d;
            for (int i = 0; i < values.Count; i++)
                if (values[i] > bestValue) { bestValue = values[i]; best = i; }
            return best;
        }
    }
}
