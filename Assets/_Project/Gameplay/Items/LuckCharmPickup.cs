using UnityEngine;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Luck Charm: the VS luck pickup (vampire.survivors.wiki, Little Clover, fetched 2026-09-27): +10% Luck for the rest
    /// of the run. Drops from light sources (weight 1, any level). Name and placeholder art are Soul Hunter's own.
    /// </summary>
    public class LuckCharmPickup : Pickup
    {
        public const string DisplayName = "Luck Charm";
        public const float LuckBonus = 0.1f;

        protected override void OnPickedUp(Collider player)
        {
            var stats = player != null ? player.GetComponentInParent<PlayerStats>() : null;
            Apply(stats);
            Debug.Log($"[{DisplayName}] Luck +{LuckBonus:P0}");
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.UnlockSound);
        }

        /// <summary>Adds the charm's Luck to the player's stats.</summary>
        public static void Apply(PlayerStats stats)
        {
            if (stats != null) stats.AddLuck(LuckBonus);
        }
    }
}
