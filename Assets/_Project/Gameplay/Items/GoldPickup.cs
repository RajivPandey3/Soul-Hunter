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

        /// <summary>Speed a gathered coin starts flying at, and how fast it speeds up (units/s, units/s^2).</summary>
        public const float PullStartSpeed = 12f, PullAcceleration = 40f;

        private Transform _pullTarget;
        private float _pullSpeed;

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

        /// <summary>True while this coin is flying to the player.</summary>
        public bool IsPulled => _pullTarget != null;

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

        /// <summary>Flies the coin to <paramref name="target"/>, where the normal pickup check collects it.</summary>
        public void Magnetize(Transform target)
        {
            if (_isCollected || target == null) return;
            _pullTarget = target;
            _pullSpeed = PullStartSpeed;
        }

        public override void Tick()
        {
            if (_pullTarget != null && !_isCollected)
            {
                if (!_pullTarget.gameObject.activeInHierarchy) _pullTarget = null;
                else
                {
                    _pullSpeed += PullAcceleration * Time.deltaTime;
                    transform.position = Vector3.MoveTowards(transform.position, _pullTarget.position, _pullSpeed * Time.deltaTime);
                }
            }
            base.Tick();
        }

        public override void ResetPickup()
        {
            base.ResetPickup();
            IsFeverCoin = false;
            _pullTarget = null;
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
