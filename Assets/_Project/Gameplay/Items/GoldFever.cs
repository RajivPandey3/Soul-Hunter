using UnityEngine;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// VS Gold Fever (vampire.survivors.wiki, Gilded Clover, fetched 2026-09-27), started by Soul Hunter's Gilded Charm:
    /// it lasts 10 s; while it runs every kill has a 75% chance to drop a coin worth 0.5 gold before Greed and the stage
    /// bonus; picking up a gold coin, coin bag or rich coin bag adds 0.5, 5 or 10 s, a fever coin 0.01 s; the timer is
    /// capped at 10 s. Not adopted: Orologion halving the decay, and the character/arcana bonuses (Soul Hunter has none).
    /// Lives on the player for the run, so it ends with the scene. Timed in game time (a pause stops it).
    /// </summary>
    public sealed class GoldFever : MonoBehaviour
    {
        public const float StartSeconds = 10f;
        public const float MaxSeconds = 10f;
        public const float CoinChance = 0.75f;
        public const float FeverCoinBaseGold = 0.5f;
        public const float CoinExtension = 0.5f, CoinBagExtension = 5f, RichCoinBagExtension = 10f, FeverCoinExtension = 0.01f;

        /// <summary>The player's fever, or null before the first Gilded Charm of this run.</summary>
        public static GoldFever Current { get; private set; }

        /// <summary>True while a Gold Fever is running.</summary>
        public static bool Running => Current != null && Current.IsActive;

        public float RemainingSeconds { get; private set; }
        public bool IsActive => RemainingSeconds > 0f;

        /// <summary>Starts (or restarts at full length) the fever on the player.</summary>
        public static GoldFever Begin(GameObject player)
        {
            if (player == null) return null;
            var fever = player.GetComponent<GoldFever>();
            if (fever == null) fever = player.AddComponent<GoldFever>();
            fever.RemainingSeconds = StartSeconds;
            Current = fever;
            return fever;
        }

        private void Update()
        {
            // VS (wiki Orologion): "Orologion slows down Gold Fever's duration decay by half".
            if (RemainingSeconds > 0f) RemainingSeconds = Tick(RemainingSeconds, Time.deltaTime * (TimeFreezePickup.WorldFrozen ? 0.5f : 1f));
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        /// <summary>Remaining time after <paramref name="deltaSeconds"/> of play.</summary>
        public static float Tick(float remaining, float deltaSeconds) => Mathf.Max(0f, remaining - Mathf.Max(0f, deltaSeconds));

        /// <summary>
        /// Seconds a collected coin adds: coin 0.5, coin bag 5, rich coin bag 10 (by the coin's base value, using the light
        /// source's 1 / 10 / 100), a coin dropped by the fever itself 0.01.
        /// </summary>
        public static float ExtensionFor(int coinValue, bool feverCoin)
        {
            if (feverCoin) return FeverCoinExtension;
            if (coinValue >= LightSourceDrops.RichCoinBagValue) return RichCoinBagExtension;
            if (coinValue >= LightSourceDrops.CoinBagValue) return CoinBagExtension;
            return CoinExtension;
        }

        /// <summary>Only a running fever is extended, and never past the 10 s cap.</summary>
        public static float Extend(float remaining, float addSeconds)
        {
            if (remaining <= 0f) return 0f;
            return Mathf.Min(MaxSeconds, remaining + Mathf.Max(0f, addSeconds));
        }

        /// <summary>True when a kill during the fever drops a coin (75%).</summary>
        public static bool RollsCoin(float roll01) => roll01 < CoinChance;

        /// <summary>Called by GoldPickup when the player collects a coin.</summary>
        public static void OnCoinCollected(int coinValue, bool feverCoin)
        {
            if (!Running) return;
            Current.RemainingSeconds = Extend(Current.RemainingSeconds, ExtensionFor(coinValue, feverCoin));
        }

        /// <summary>Called by EnemyDrop for every kill that drops loot: during the fever, 75% chance of a fever coin.</summary>
        public static void OnEnemyKilled(Vector3 position)
        {
            if (!Running || !RollsCoin(Random.value)) return;
            var pool = PickupPoolManager.Instance;
            if (pool == null) return;
            // Set aside from the gem and from an ordinary coin (which EnemyDrop nudges +0.6 x).
            var coin = pool.SpawnGold(position + new Vector3(-0.6f, 0f, 0.4f), 1);
            if (coin != null) coin.IsFeverCoin = true;
        }
    }
}
