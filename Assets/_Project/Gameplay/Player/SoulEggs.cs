using System;
using SoulHunter.Core.Persistence;
using UnityEngine;

namespace SoulHunter.Gameplay.Player
{
    /// <summary>
    /// VS Golden Egg rule, as Soul Hunter's Soul Egg: each egg permanently raises one random stat of the
    /// character it was bought for, by a small fixed amount. It counts from that character's next run.
    /// Amounts and caps follow the VS egg table (vampire.survivors.wiki/w/Golden_Egg, checked 2026-09-26);
    /// Magnet is +0.01 here, VS's +0.3 on its base 30, added to the Magnet base before the character, PowerUp and
    /// item multipliers (wiki Magnet; PlayerStats.AddMagnet).
    /// </summary>
    public static class SoulEggs
    {
        public enum Stat
        {
            MaxHealth, Recovery, Armor, Amount, Revival, Reroll, Skip, Banish,
            MoveSpeed, Might, Speed, Duration, Area, Luck, Growth, Greed, Curse, Cooldown, Magnet
        }

        public static readonly int StatCount = Enum.GetValues(typeof(Stat)).Length;

        // VS egg caps (totals from eggs).
        public const float ArmorCap = 50f, MightCap = 9f, SpeedCap = 4f, DurationCap = 4f, AreaCap = 9f, CooldownCap = 0.9f, AmountCap = 10f;

        /// <summary>How much one egg adds to a stat (fractions for multipliers: 0.01 = +1%).</summary>
        public static float AmountPerEgg(Stat stat) => stat switch
        {
            Stat.MaxHealth => 1f,
            Stat.Recovery or Stat.Armor or Stat.Amount or Stat.Revival or Stat.Reroll => 0.1f,
            Stat.Skip or Stat.Banish => 0.2f,
            Stat.Cooldown => 0.005f,
            _ => 0.01f
        };

        /// <summary>The egg record of a character, or null (or a new one when <paramref name="create"/> is true).</summary>
        public static SoulEggRecord For(GameData data, string character, bool create = false)
        {
            if (data == null || string.IsNullOrEmpty(character)) return null;
            data.SoulEggs ??= new System.Collections.Generic.List<SoulEggRecord>();
            foreach (var record in data.SoulEggs)
                if (record != null && record.Character == character) return record;
            if (!create) return null;
            var added = new SoulEggRecord { Character = character };
            data.SoulEggs.Add(added);
            return added;
        }

        /// <summary>Adds one egg to the character's record: a uniformly random stat, raised by its amount.</summary>
        public static Stat Add(GameData data, string character, System.Random rng)
        {
            var stat = (Stat)(rng ?? new System.Random()).Next(StatCount);
            Add(data, character, stat);
            return stat;
        }

        public static void Add(GameData data, string character, Stat stat)
        {
            var record = For(data, character, create: true);
            if (record == null) return;
            record.Eggs++;
            float amount = AmountPerEgg(stat);
            switch (stat)
            {
                case Stat.MaxHealth: record.MaxHealth += amount; break;
                case Stat.Recovery: record.Recovery += amount; break;
                case Stat.Armor: record.Armor += amount; break;
                case Stat.Amount: record.Amount += amount; break;
                case Stat.Revival: record.Revival += amount; break;
                case Stat.Reroll: record.Reroll += amount; break;
                case Stat.Skip: record.Skip += amount; break;
                case Stat.Banish: record.Banish += amount; break;
                case Stat.MoveSpeed: record.MoveSpeed += amount; break;
                case Stat.Might: record.Might += amount; break;
                case Stat.Speed: record.Speed += amount; break;
                case Stat.Duration: record.Duration += amount; break;
                case Stat.Area: record.Area += amount; break;
                case Stat.Luck: record.Luck += amount; break;
                case Stat.Growth: record.Growth += amount; break;
                case Stat.Greed: record.Greed += amount; break;
                case Stat.Curse: record.Curse += amount; break;
                case Stat.Cooldown: record.Cooldown += amount; break;
                case Stat.Magnet: record.Magnet += amount; break;
            }
        }

        /// <summary>Whole points of a fractional flat stat (0.1 per egg counts from the 10th egg).</summary>
        public static int Whole(float total) => Mathf.FloorToInt(total + 1e-4f);

        /// <summary>
        /// The eggs this run really started with: a copy taken by <see cref="Apply"/>. Eggs bought or found during the run go
        /// on the save's record but count "from the next run", so Cappella Magna's egg snap (Level 8, 4:00), Avatar of Gaea
        /// and the Results read this copy, not the live record (bug hunt 2026-10-29: the snap took eggs the run never had).
        /// </summary>
        public static SoulEggRecord AppliedThisRun { get; private set; }

        /// <summary>
        /// A new run starts with no eggs until <see cref="Apply"/> says otherwise (audit 2026-10-30): the copy used to be
        /// null for a run with no eggs, so the Results fell back to the live record and counted eggs bought mid-run, and a
        /// run whose character was not found kept the last run's copy.
        /// </summary>
        public static void BeginRun() => AppliedThisRun = new SoulEggRecord();

        public static SoulEggRecord Copy(SoulEggRecord record) =>
            record == null ? null : JsonUtility.FromJson<SoulEggRecord>(JsonUtility.ToJson(record));

        /// <summary>
        /// Applies a character's eggs to the run's stats (once, at run start, after the character's own stats).
        /// Reroll, Skip and Banish charges are added by LevelUpManager.
        /// </summary>
        public static void Apply(SoulEggRecord record, PlayerStats stats)
        {
            if (stats == null) return;
            AppliedThisRun = Copy(record) ?? new SoulEggRecord();
            if (record == null) return;
            int maxHealth = Whole(record.MaxHealth);
            if (maxHealth > 0) stats.AddMaxHealth(maxHealth);
            if (record.Recovery > 0f) stats.AddRegen(record.Recovery);
            int armor = Mathf.Min(Whole(record.Armor), (int)ArmorCap);
            if (armor > 0) stats.AddArmor(armor);
            int amount = Mathf.Min(Whole(record.Amount), (int)AmountCap);
            if (amount > 0) stats.AddAmount(amount);
            int revival = Whole(record.Revival);
            if (revival > 0) stats.AddRevival(revival);
            if (record.MoveSpeed > 0f) stats.AddMoveSpeed(record.MoveSpeed);
            if (record.Might > 0f) stats.AddMight(Mathf.Min(record.Might, MightCap));
            if (record.Speed > 0f) stats.AddProjectileSpeed(Mathf.Min(record.Speed, SpeedCap));
            if (record.Duration > 0f) stats.AddDuration(Mathf.Min(record.Duration, DurationCap));
            if (record.Area > 0f) stats.AddArea(Mathf.Min(record.Area, AreaCap));
            if (record.Luck > 0f) stats.AddLuck(record.Luck);
            if (record.Growth > 0f) stats.AddExpBonus(record.Growth);
            if (record.Greed > 0f) stats.AddGreed(record.Greed);
            if (record.Curse > 0f) stats.AddCurse(record.Curse);
            if (record.Magnet > 0f) stats.AddMagnet(record.Magnet);
            if (record.Cooldown > 0f) stats.ReduceCooldown(Mathf.Min(record.Cooldown, CooldownCap));
        }

        /// <summary>
        /// Takes back what <see cref="Apply"/> gave (VS Cappella Magna, raw 2026-10-28: "Before defeating Ender, all egg
        /// bonuses will be snapped away"; owner 2026-10-28: "VS jaisa"). Every stat loses exactly the egg amount Apply added
        /// (with the same caps). Max Health loses the egg's points times <paramref name="maxHealthMultiplier"/> (the Max
        /// Health PowerUp multiplied them after Apply), without killing the hero. A Revival already spent is not taken.
        /// </summary>
        public static void Remove(SoulEggRecord record, PlayerStats stats, float maxHealthMultiplier = 1f)
        {
            if (record == null || stats == null) return;
            int maxHealth = Whole(record.MaxHealth);
            if (maxHealth > 0)
            {
                stats.RemoveMaxHealthBonus(maxHealth);
                var health = stats.GetComponent<SoulHunter.Gameplay.Combat.HealthController>() ?? stats.GetComponentInParent<SoulHunter.Gameplay.Combat.HealthController>();
                if (health != null) health.ReduceMaxHealth(maxHealth * Mathf.Max(1f, maxHealthMultiplier, health.MaxHealthScale));
            }
            if (record.Recovery > 0f) stats.AddRegen(-record.Recovery);
            int armor = Mathf.Min(Whole(record.Armor), (int)ArmorCap);
            if (armor > 0) stats.AddArmor(-armor);
            int amount = Mathf.Min(Whole(record.Amount), (int)AmountCap);
            if (amount > 0) stats.AddAmount(-amount);
            int revival = Mathf.Min(Whole(record.Revival), stats.Revivals);
            if (revival > 0) stats.AddRevival(-revival);
            if (record.MoveSpeed > 0f) stats.AddMoveSpeed(-record.MoveSpeed);
            if (record.Might > 0f) stats.AddMight(-Mathf.Min(record.Might, MightCap));
            if (record.Speed > 0f) stats.AddProjectileSpeed(-Mathf.Min(record.Speed, SpeedCap));
            if (record.Duration > 0f) stats.AddDuration(-Mathf.Min(record.Duration, DurationCap));
            if (record.Area > 0f) stats.AddArea(-Mathf.Min(record.Area, AreaCap));
            if (record.Luck > 0f) stats.AddLuck(-record.Luck);
            if (record.Growth > 0f) stats.AddExpBonus(-record.Growth);
            if (record.Greed > 0f) stats.AddGreed(-record.Greed);
            if (record.Curse > 0f) stats.AddCurse(-record.Curse);
            if (record.Magnet > 0f) stats.AddMagnet(-record.Magnet);
            if (record.Cooldown > 0f) stats.ReduceCooldown(-Mathf.Min(record.Cooldown, CooldownCap));
        }

        /// <summary>Short text for the shop after buying, e.g. "+1% Might".</summary>
        public static string Describe(Stat stat)
        {
            float amount = AmountPerEgg(stat);
            return stat switch
            {
                Stat.MaxHealth => "+1 Max Health",
                Stat.Cooldown => "-0.5% Cooldown",
                Stat.Recovery or Stat.Armor or Stat.Amount or Stat.Revival or Stat.Reroll or Stat.Skip or Stat.Banish =>
                    $"+{amount:0.0} {stat}",
                Stat.Growth => "+1% Growth",
                Stat.MoveSpeed => "+1% Move Speed",
                _ => $"+1% {stat}"
            };
        }
    }
}
