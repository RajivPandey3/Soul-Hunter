namespace SoulHunter.Core.Persistence
{
    /// <summary>
    /// VS PowerUps for healing (vampire.survivors.wiki/w/PowerUps, fetched 2026-09-29; heal balance pass):
    /// Max Health "Augments Max Health by 10% per rank", 3 ranks, stacking multiplicatively (+33.1% at rank 3), initial
    /// cost 200; Recovery "Recovers 0.1 HP per rank (max 0.5) per second", 5 ranks, initial cost 200. VS raises the price
    /// with each purchase; Soul Hunter's shop uses fixed prices, so these stay at the initial cost (provisional).
    /// </summary>
    public static class MetaPowerUps
    {
        public const int MaxHealthRanks = 3, RecoveryRanks = 5;
        public const float MaxHealthPerRank = 0.10f, RecoveryPerRank = 0.1f;
        public const int MaxHealthPrice = 200, RecoveryPrice = 200;

        /// <summary>Max Health multiplier for a rank: 1.1 per rank, multiplied (VS).</summary>
        public static float MaxHealthMultiplier(int rank)
        {
            float multiplier = 1f;
            for (int i = 0; i < UnityEngine.Mathf.Clamp(rank, 0, MaxHealthRanks); i++) multiplier *= 1f + MaxHealthPerRank;
            return multiplier;
        }

        /// <summary>Recovery (HP per second) for a rank.</summary>
        public static float Recovery(int rank) => UnityEngine.Mathf.Clamp(rank, 0, RecoveryRanks) * RecoveryPerRank;
    }
}
