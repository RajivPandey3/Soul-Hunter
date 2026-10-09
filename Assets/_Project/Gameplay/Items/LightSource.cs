using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// VS "light source" (vampire.survivors.wiki, Light source): a breakable stage prop with 10 HP that weapons break,
    /// dropping one item from a weighted table. Soul Hunter's is the Soul Lantern. It is tagged and layered like an
    /// enemy so every weapon can hit it, but it is not an EnemyController: weapons do not aim at it, the horde and
    /// kill count ignore it, and it never hurts or blocks the player.
    /// </summary>
    [RequireComponent(typeof(HealthController))]
    public class LightSource : MonoBehaviour
    {
        public const int MaxHealth = 10;

        private HealthController _health;

        /// <summary>Raised when it breaks, with the item it dropped.</summary>
        public event System.Action<LightSource, LightSourceDrops.Drop> Broken;

        /// <summary>Any lantern broken, with its drop (the autoplay bot logs these to measure lantern income).</summary>
        public static event System.Action<LightSource, LightSourceDrops.Drop> AnyBroken;

        private void Awake()
        {
            _health = GetComponent<HealthController>();
            _health.KnockbackResistance = 1f; // a lantern does not slide when hit
        }

        private void OnEnable()
        {
            _health.Initialize(MaxHealth);
            _health.IsInvincible = false;
            _health.OnDied += Break;
        }

        private void OnDisable()
        {
            _health.OnDied -= Break;
        }

        private void Break()
        {
            var player = SoulHunter.Gameplay.Player.PlayerController.Instance;
            var experience = player != null ? player.GetComponent<SoulHunter.Gameplay.Player.PlayerExperience>() : null;
            int level = experience != null ? experience.CurrentLevel : 1;
            float luck = player != null && player.Stats != null ? player.Stats.Luck : 1f;
            var drop = LightSourceDrops.Pick(level, luck, Random.value);
            Vector3 at = transform.position;
            LightSourceDrops.Spawn(drop, at);
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.ChestOpen, at + Vector3.up * 0.8f);
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFXThrottled(audio.EnemyDeathSound, SoulHunter.Gameplay.Audio.AudioManager.EnemyDeathInterval, 0.7f);
            Broken?.Invoke(this, drop);
            AnyBroken?.Invoke(this, drop);
            // VS Heart of Fire (XIX): "Light sources explode."
            SoulHunter.Gameplay.Core.ArcanaManager.OnLightSourceBroken(at);
            // HealthController deactivates the lantern after OnDied; the spawner's pool takes it back.
        }
    }

    /// <summary>
    /// VS light-source drop table (vampire.survivors.wiki, Light source; weight, minimum player level): Gold Coin 50,
    /// Floor Chicken 12, Coin Bag 10, Vacuum (Magnet) 2 from level 12, Orologion (Time Freeze) 2 from level 4, Rich Coin
    /// Bag 1 from level 5, and (2026-09-27) Soul Hunter's own versions of the Rosary, Nduja Fritta Tanto, Little Clover
    /// and Gilded Clover: Soul Toll (screen wipe) 1 from level 8, Hellfire Breath 1, Luck Charm 1, and Gilded Charm 1 from
    /// level 30. Rerollo (weight unknown) is left out. Coin values 1 / 10 / 100 (wiki Gold Coin (pickup), Coin Bag and
    /// Rich Coin Bag pages, checked 2026-09-29). Luck (wiki Luck, "Higher rarity pickups"): every drop except Gold Coin
    /// and Coin Bag weighs rarity x totalLuck (Soul Hunter's Luck, 1 at base).
    /// </summary>
    public static class LightSourceDrops
    {
        public enum Drop { GoldCoin, Chicken, CoinBag, Magnet, TimeFreeze, RichCoinBag, ScreenWipe, FireBreath, GildedCharm, LuckCharm }

        public const int GoldCoinValue = 1, CoinBagValue = 10, RichCoinBagValue = 100;

        private static readonly (Drop drop, int weight, int minLevel)[] Table =
        {
            (Drop.GoldCoin, 50, 0), (Drop.Chicken, 12, 0), (Drop.CoinBag, 10, 0),
            (Drop.Magnet, 2, 12), (Drop.TimeFreeze, 2, 4), (Drop.RichCoinBag, 1, 5),
            (Drop.ScreenWipe, 1, 8), (Drop.FireBreath, 1, 0), (Drop.GildedCharm, 1, 30), (Drop.LuckCharm, 1, 0),
        };

        /// <summary>The table's weight and minimum player level for a drop (for tests and tuning).</summary>
        public static (int weight, int minLevel) Entry(Drop drop)
        {
            foreach (var e in Table) if (e.drop == drop) return (e.weight, e.minLevel);
            return (0, int.MaxValue);
        }

        /// <summary>VS Luck: weight x totalLuck for every drop except Gold Coin and Coin Bag; 0 below its level.</summary>
        public static float Weight(Drop drop, int playerLevel, float luck)
        {
            foreach (var e in Table)
            {
                if (e.drop != drop) continue;
                if (playerLevel < e.minLevel) return 0f;
                bool plainGold = drop == Drop.GoldCoin || drop == Drop.CoinBag;
                // The player's Arcanas: Out of Bounds (XII) doubles Orologion, Heart of Fire (XIX) quadruples Nduja.
                return e.weight * (plainGold ? 1f : Mathf.Max(0f, luck)) * SoulHunter.Gameplay.Core.ArcanaManager.DropWeightFactor(drop);
            }
            return 0f;
        }

        /// <summary>The drop for a uniform roll in [0, 1) at Luck 1.</summary>
        public static Drop Pick(int playerLevel, float roll01) => Pick(playerLevel, 1f, roll01);

        /// <summary>The drop for a uniform roll in [0, 1), among the entries the player's level allows, weighted by Luck.</summary>
        public static Drop Pick(int playerLevel, float luck, float roll01)
        {
            float total = 0f;
            foreach (var e in Table) total += Weight(e.drop, playerLevel, luck);
            float target = Mathf.Clamp01(roll01) * total;
            float running = 0f;
            foreach (var e in Table)
            {
                float weight = Weight(e.drop, playerLevel, luck);
                if (weight <= 0f) continue;
                running += weight;
                if (target < running) return e.drop;
            }
            return Drop.GoldCoin;
        }

        /// <summary>Share of rolls that give this drop at this level and Luck (for tests and tuning).</summary>
        public static float Chance(Drop drop, int playerLevel, float luck = 1f)
        {
            float total = 0f;
            foreach (var e in Table) total += Weight(e.drop, playerLevel, luck);
            return total > 0f ? Weight(drop, playerLevel, luck) / total : 0f;
        }

        public static void Spawn(Drop drop, Vector3 position)
        {
            switch (drop)
            {
                case Drop.ScreenWipe: ExtraPickupPool.Spawn(ExtraPickupPool.Kind.ScreenWipe, position); return;
                case Drop.FireBreath: ExtraPickupPool.Spawn(ExtraPickupPool.Kind.FireBreath, position); return;
                case Drop.LuckCharm: ExtraPickupPool.Spawn(ExtraPickupPool.Kind.LuckCharm, position); return;
                case Drop.GildedCharm: ExtraPickupPool.Spawn(ExtraPickupPool.Kind.GildedCharm, position); return;
            }
            var pool = PickupPoolManager.Instance;
            if (pool == null) return;
            switch (drop)
            {
                case Drop.GoldCoin: pool.SpawnGold(position, GoldCoinValue); break;
                case Drop.CoinBag: pool.SpawnGold(position, CoinBagValue); break;
                case Drop.RichCoinBag: pool.SpawnGold(position, RichCoinBagValue); break;
                case Drop.Chicken: pool.SpawnChicken(position); break;
                case Drop.Magnet: pool.SpawnMagnet(position); break;
                case Drop.TimeFreeze: pool.SpawnTimeFreeze(position); break;
            }
        }
    }
}
