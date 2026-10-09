using System.Collections.Generic;
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
        public override string ResultsName => PickupNames.ForGold(Value);
        [SerializeField, Min(1)] private int _value = 1;

        /// <summary>Coins lying on the ground (the Gilded Charm gathers them all).</summary>
        public static readonly List<GoldPickup> ActiveCoins = new List<GoldPickup>();

        public int Value
        {
            get => _value;
            set => _value = Mathf.Max(1, value);
        }

        /// <summary>
        /// VS Gold Fever coin: worth 0.5 gold before Greed (at least 1 after rounding) and extends the fever by only
        /// 0.01 s. Set after spawning; cleared when the pool reuses the coin.
        /// </summary>
        public bool IsFeverCoin { get; set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!ActiveCoins.Contains(this)) ActiveCoins.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ActiveCoins.Remove(this);
        }

        public override void ResetPickup()
        {
            base.ResetPickup();
            IsFeverCoin = false;
        }

        protected override void OnPickedUp(Collider player)
        {
            var stats = player != null ? player.GetComponentInParent<PlayerStats>() : null;
            int gained = IsFeverCoin
                ? GoldRewards.GrantFraction(GoldFever.FeverCoinBaseGold, stats)
                : GoldRewards.Grant(_value, stats);
            GoldFever.OnCoinCollected(_value, IsFeverCoin);

            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.GemPickupSound);
        }
    }
}
