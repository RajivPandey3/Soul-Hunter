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

        // Bug MIS ITM-10: Contains and Remove searched the whole list on every coin, with hundreds on the ground during
        // Gold Fever. Each coin keeps its place in the list and leaves by a swap with the last one (as PickupTicker); the
        // list's order is not used by any reader. Not serialized, so a pooled copy starts at -1 like a new coin.
        private int _activeIndex = -1;

        private bool IsListed => _activeIndex >= 0 && _activeIndex < ActiveCoins.Count && ReferenceEquals(ActiveCoins[_activeIndex], this);

        protected override void OnEnable()
        {
            base.OnEnable();
            if (IsListed) return;
            // A stale index (never expected) falls back to the old search, so a coin is never listed twice.
            if (_activeIndex >= 0 && ActiveCoins.Contains(this)) { _activeIndex = ActiveCoins.IndexOf(this); return; }
            _activeIndex = ActiveCoins.Count;
            ActiveCoins.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (IsListed)
            {
                int last = ActiveCoins.Count - 1;
                var moved = ActiveCoins[last];
                ActiveCoins[_activeIndex] = moved;
                if (moved != null) moved._activeIndex = _activeIndex;
                ActiveCoins.RemoveAt(last);
            }
            else ActiveCoins.Remove(this); // a stale index (never expected): the old search
            _activeIndex = -1;
        }

        // Domain reload may be off, so coins from the last play session must not stay listed.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ActiveCoins.Clear();

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
            // VS Disco of Gold (XV): "Picking up coin bags from the floor triggers Gold Fever."
            if (!IsFeverCoin) SoulHunter.Gameplay.Core.ArcanaManager.OnCoinPickedUp(_value, player != null ? player.gameObject : null);

            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.GemPickupSound);
        }
    }
}
