using UnityEngine;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Shows one of several models by value, like VS gems growing in colour and size with their XP: Soul Gems
    /// (cyan / violet / crimson by XP) and Soul Coins (bronze / silver / gold by gold value).
    /// Tier i is shown while value &lt; Thresholds[i]; the last tier covers everything above.
    /// </summary>
    public sealed class TieredVisual : MonoBehaviour
    {
        public int[] Thresholds = { 25, 100 };
        public GameObject[] Tiers = new GameObject[0];

        public int CurrentTier { get; private set; } = -1;

        public static int TierFor(int value, int[] thresholds)
        {
            for (int i = 0; i < thresholds.Length; i++)
                if (value < thresholds[i]) return i;
            return thresholds.Length;
        }

        public void SetValue(int value)
        {
            int tier = Mathf.Min(TierFor(value, Thresholds), Tiers.Length - 1);
            CurrentTier = tier;
            for (int i = 0; i < Tiers.Length; i++)
                if (Tiers[i] != null) Tiers[i].SetActive(i == tier);
        }
    }
}
