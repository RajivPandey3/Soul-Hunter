using UnityEngine;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Learning Comment:
    /// Gold Coin Pickup (VS style).
    /// Dushman kabhi kabhi coin girate hain; player ke chhoone par Value x Greed gold milta hai.
    /// Elite dushman zyada value wala coin (bag) girate hain.
    /// </summary>
    public class GoldPickup : Pickup
    {
        [SerializeField, Min(1)] private int _value = 1;

        public int Value
        {
            get => _value;
            set => _value = Mathf.Max(1, value);
        }

        protected override void OnPickedUp(Collider player)
        {
            var stats = player != null ? player.GetComponentInParent<PlayerStats>() : null;
            int gained = GoldRewards.Grant(_value, stats);
            Debug.Log($"[GoldPickup] +{gained} gold");

            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.GemPickupSound);
        }
    }
}
