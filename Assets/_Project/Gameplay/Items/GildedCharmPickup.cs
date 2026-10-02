using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Gilded Charm: the VS gold pickup (vampire.survivors.wiki, Gilded Clover, fetched 2026-09-27). It gathers every coin
    /// left on the ground to the player and starts a Gold Fever (GoldFever). Drops from light sources (weight 1, from
    /// player level 30). Name and placeholder art are Soul Hunter's own.
    /// </summary>
    public class GildedCharmPickup : Pickup
    {
        public override string ResultsName => DisplayName;
        public const string DisplayName = "Gilded Charm";

        private static readonly List<GoldPickup> Buffer = new List<GoldPickup>();

        protected override void OnPickedUp(Collider player)
        {
            var controller = player != null ? player.GetComponentInParent<PlayerController>() : null;
            var target = controller != null ? controller.transform : player != null ? player.transform : null;
            if (target == null) return;
            GoldFever.Begin(target.gameObject);
            int pulled = GatherCoins(target);
            Debug.Log($"[{DisplayName}] Gold Fever! {pulled} coins gathered.");
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.UnlockSound);
        }

        /// <summary>Sends every coin on the ground flying to <paramref name="target"/>. Returns the number of coins.</summary>
        public static int GatherCoins(Transform target)
        {
            Buffer.Clear();
            Buffer.AddRange(GoldPickup.ActiveCoins);
            int count = 0;
            foreach (var coin in Buffer)
            {
                if (coin == null || !coin.isActiveAndEnabled) continue;
                coin.Magnetize(target);
                count++;
            }
            Buffer.Clear();
            return count;
        }
    }
}
