using System.Collections.Generic;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// VS rule: level-up choices rarity ke hisaab se weighted hote hain; strong ya rare items kam dikhte hain.
    /// Weight 100 = normal. Values Soul Hunter ki apni provisional balance values hain.
    /// </summary>
    public static class UpgradeRarity
    {
        public const int DefaultWeight = 100;

        private static readonly Dictionary<UpgradeData.UpgradeType, int> Weights = new Dictionary<UpgradeData.UpgradeType, int>
        {
            { UpgradeData.UpgradeType.Tiragisu, 30 },     // extra revival
            { UpgradeData.UpgradeType.Duplicator, 40 },   // +Amount for every weapon
            { UpgradeData.UpgradeType.SkullOManiac, 40 },
            { UpgradeData.UpgradeType.Pentagram, 40 },    // screen wipe
            { UpgradeData.UpgradeType.Crown, 60 },
            { UpgradeData.UpgradeType.Laurel, 60 },
            { UpgradeData.UpgradeType.ClockLancet, 60 },
            { UpgradeData.UpgradeType.Peachone, 60 },
            { UpgradeData.UpgradeType.Gun, 60 },
            { UpgradeData.UpgradeType.CherryBomb, 60 },
            { UpgradeData.UpgradeType.SongOfMana, 60 },
        };

        public static int GetWeight(UpgradeData.UpgradeType type) =>
            Weights.TryGetValue(type, out int weight) ? weight : DefaultWeight;
    }
}
