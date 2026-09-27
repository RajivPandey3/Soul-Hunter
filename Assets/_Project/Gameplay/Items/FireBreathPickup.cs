using UnityEngine;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Hellfire Breath: the VS fire-breath pickup (vampire.survivors.wiki, Nduja Fritta Tanto, fetched 2026-09-27). The
    /// player breathes cones of fire for 10 s (FireBreath). Drops from light sources (weight 1, any level). Name and
    /// placeholder art are Soul Hunter's own.
    /// </summary>
    public class FireBreathPickup : Pickup
    {
        protected override void OnPickedUp(Collider player)
        {
            var controller = player != null ? player.GetComponentInParent<PlayerController>() : null;
            if (controller == null) return;
            FireBreath.Begin(controller.transform);
            Debug.Log($"[{FireBreath.DisplayName}] The hero breathes fire.");
        }
    }
}
