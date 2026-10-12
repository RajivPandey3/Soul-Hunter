using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Player
{
    /// <summary>
    /// VS rule: a run starts from the character's stat block plus the permanent power-ups bought
    /// with gold. This is that sum, computed without a scene so the character select screen shows
    /// exactly the stats the run will start with. PlayerStats applies the same numbers in play.
    /// </summary>
    public struct CharacterStatSheet
    {

        public int MaxHealth;
        public float Recovery;
        public int Armor;
        public float MoveSpeed;
        public float Might;
        public float Speed;
        public float Duration;
        public float Area;
        public float Cooldown;
        public int Amount;
        public int Revivals;
        public float Magnet;
        public float Luck;
        public float Growth;
        public float Greed;
        public float Curse;
        public int Reroll;
        public int Skip;
        public int Banish;

        /// <summary>
        /// The player's stats right now (for the in-game stats panel): the live PlayerStats values, the current Max
        /// Health and the Reroll/Skip/Banish charges left.
        /// </summary>
        public static CharacterStatSheet Live(PlayerStats stats, int maxHealth, int reroll, int skip, int banish)
        {
            var sheet = Build(null);
            sheet.MaxHealth = maxHealth;
            if (stats != null)
            {
                sheet.Recovery = stats.Regen;
                sheet.Armor = stats.Armor;
                sheet.MoveSpeed = stats.MoveSpeedMultiplier;
                sheet.Might = stats.Might;
                sheet.Speed = stats.ProjectileSpeed;
                sheet.Duration = stats.Duration;
                sheet.Area = stats.Area;
                sheet.Cooldown = stats.Cooldown;
                sheet.Amount = stats.Amount;
                sheet.Revivals = stats.Revivals;
                sheet.Magnet = stats.Magnet;
                sheet.Luck = stats.Luck;
                sheet.Growth = stats.ExpBonus;
                sheet.Greed = stats.Greed;
                sheet.Curse = stats.Curse;
            }
            sheet.Reroll = reroll;
            sheet.Skip = skip;
            sheet.Banish = banish;
            return sheet;
        }

        /// <summary>
        /// The in-game list, in the order of vampire.survivors.wiki/w/Player_stats ("in-game when paused, or when leveling
        /// up"): Max Health, Recovery, Armor, Move Speed, Might, Area, Speed, Duration, Amount, Cooldown, Luck, Growth,
        /// Greed, Curse, Magnet, Revival, Reroll, Skip, Banish. Same formats as the character select screen.
        /// </summary>
        public Row[] InGameRows() => new[]
        {
            new Row("Max Health", MaxHealth.ToString(), MaxHealth != 100),
            new Row("Recovery", Recovery > 0f ? Recovery.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) : "-", Recovery > 0f),
            Flat("Armor", Armor),
            Percent("Move Speed", MoveSpeed),
            Percent("Might", Might),
            Percent("Area", Area),
            Percent("Speed", Speed),
            Percent("Duration", Duration),
            Flat("Amount", Amount),
            Percent("Cooldown", Cooldown),
            Percent("Luck", Luck),
            Percent("Growth", Growth),
            Percent("Greed", Greed),
            Percent("Curse", Curse),
            Percent("Magnet", Magnet),
            Flat("Revival", Revivals),
            Flat("Reroll", Reroll),
            Flat("Skip", Skip),
            Flat("Banish", Banish),
        };

        /// <summary>Character stats plus meta power-up levels (0 when nothing was bought).</summary>
        /// <summary>Character stats plus the VS PowerUps bought in <paramref name="data"/> (none when null).</summary>
        public static CharacterStatSheet Build(CharacterData character, SoulHunter.Core.Persistence.GameData data = null) =>
            Build(character, data, true);

        /// <summary>
        /// The character select rows (vampire.survivors.wiki/w/Characters, "Character stats", raw page fetched 2026-10-12):
        /// "Stats whose values are displayed in white are either default character stats or stats affected by Golden Eggs,
        /// while values displayed in yellow are values added from the PowerUps shop." A row's FromPowerUps is true when the
        /// PowerUps changed its value. The list ends with Reroll, Skip and Banish, shown once the character starts with any
        /// (the wiki's own note: "The last 3 stats will only be displayed if they were bought ... at least once").
        /// </summary>
        public static Row[] SelectRows(CharacterData character, SoulHunter.Core.Persistence.GameData data)
        {
            var full = Build(character, data, true);
            var withoutPowerUps = Build(character, data, false);
            var rows = new System.Collections.Generic.List<Row>(full.DisplayRows());
            var plain = new System.Collections.Generic.List<Row>(withoutPowerUps.DisplayRows());
            void Charges(string label, int value, int plainValue)
            {
                if (value <= 0) return;
                rows.Add(Flat(label, value));
                plain.Add(Flat(label, plainValue));
            }
            Charges("Reroll", full.Reroll, withoutPowerUps.Reroll);
            Charges("Skip", full.Skip, withoutPowerUps.Skip);
            Charges("Banish", full.Banish, withoutPowerUps.Banish);
            var result = rows.ToArray();
            for (int i = 0; i < result.Length; i++) result[i].FromPowerUps = result[i].Value != plain[i].Value;
            return result;
        }

        /// <summary>
        /// Character stats, then the character's Soul Eggs (as SoulEggs.Apply gives them to a run), then the PowerUps when
        /// <paramref name="withPowerUps"/> is true.
        /// </summary>
        public static CharacterStatSheet Build(CharacterData character, SoulHunter.Core.Persistence.GameData data, bool withPowerUps)
        {
            var sheet = new CharacterStatSheet
            {
                MaxHealth = 100, MoveSpeed = 1f, Might = 1f, Speed = 1f, Duration = 1f, Area = 1f,
                Cooldown = 1f, Magnet = 1f, Luck = 1f, Growth = 1f, Greed = 1f, Curse = 1f,
            };
            if (character != null)
            {
                if (character.BaseMaxHealth > 0) sheet.MaxHealth = character.BaseMaxHealth;
                sheet.Recovery = character.StartingRecovery;
                sheet.Armor = character.StartingArmor;
                sheet.MoveSpeed = character.BaseMoveSpeed;
                sheet.Might = character.StartingMight;
                sheet.Speed = character.StartingSpeed;
                sheet.Duration = character.StartingDuration;
                sheet.Area = character.StartingArea;
                sheet.Cooldown = character.StartingCooldown;
                sheet.Amount = character.StartingAmount;
                sheet.Revivals = character.StartingRevivals;
                sheet.Magnet = character.StartingMagnet;
                sheet.Luck = character.StartingLuck;
                sheet.Growth = character.StartingGrowth;
                sheet.Greed = character.StartingGreed;
                sheet.Curse = character.StartingCurse;
            }
            var eggs = data != null && character != null ? SoulEggs.For(data, character.CharacterName) : null;
            if (eggs != null)
            {
                // The same amounts and caps SoulEggs.Apply and LevelUpManager give a run.
                sheet.MaxHealth += SoulEggs.Whole(eggs.MaxHealth);
                sheet.Recovery += eggs.Recovery;
                sheet.Armor += UnityEngine.Mathf.Min(SoulEggs.Whole(eggs.Armor), (int)SoulEggs.ArmorCap);
                sheet.Amount += UnityEngine.Mathf.Min(SoulEggs.Whole(eggs.Amount), (int)SoulEggs.AmountCap);
                sheet.Revivals += SoulEggs.Whole(eggs.Revival);
                sheet.MoveSpeed += eggs.MoveSpeed;
                sheet.Might += UnityEngine.Mathf.Min(eggs.Might, SoulEggs.MightCap);
                sheet.Speed += UnityEngine.Mathf.Min(eggs.Speed, SoulEggs.SpeedCap);
                sheet.Duration += UnityEngine.Mathf.Min(eggs.Duration, SoulEggs.DurationCap);
                sheet.Area += UnityEngine.Mathf.Min(eggs.Area, SoulEggs.AreaCap);
                sheet.Luck += eggs.Luck;
                sheet.Growth += eggs.Growth;
                sheet.Greed += eggs.Greed;
                sheet.Curse += eggs.Curse;
                sheet.Magnet *= 1f + eggs.Magnet; // an egg's Magnet goes on the base, before the multipliers
                sheet.Cooldown -= UnityEngine.Mathf.Min(eggs.Cooldown, SoulEggs.CooldownCap);
                sheet.Reroll += SoulEggs.Whole(eggs.Reroll);
                sheet.Skip += SoulEggs.Whole(eggs.Skip);
                sheet.Banish += SoulEggs.Whole(eggs.Banish);
            }
            if (data != null && withPowerUps)
            {
                // The same VS PowerUp effects a run gets (PlayerStats.ApplyPowerUps, PlayerController for Max Health).
                int Rank(SoulHunter.Core.Persistence.PowerUpId id) => SoulHunter.Core.Persistence.PowerUps.ActiveRank(data, id); // off = 0
                float Bonus(SoulHunter.Core.Persistence.PowerUpId id) => SoulHunter.Core.Persistence.PowerUps.Bonus(id, Rank(id));
                float omni = Bonus(SoulHunter.Core.Persistence.PowerUpId.Omni);
                sheet.MaxHealth = UnityEngine.Mathf.RoundToInt(sheet.MaxHealth * SoulHunter.Core.Persistence.PowerUps.MaxHealthMultiplier(Rank(SoulHunter.Core.Persistence.PowerUpId.MaxHealth)));
                sheet.Might += Bonus(SoulHunter.Core.Persistence.PowerUpId.Might) + omni;
                sheet.Armor += Rank(SoulHunter.Core.Persistence.PowerUpId.Armor);
                sheet.Recovery += Bonus(SoulHunter.Core.Persistence.PowerUpId.Recovery);
                sheet.Cooldown -= Bonus(SoulHunter.Core.Persistence.PowerUpId.Cooldown);
                sheet.Area += Bonus(SoulHunter.Core.Persistence.PowerUpId.Area) + omni;
                sheet.Speed += Bonus(SoulHunter.Core.Persistence.PowerUpId.Speed) + omni;
                sheet.Duration += Bonus(SoulHunter.Core.Persistence.PowerUpId.Duration) + omni;
                sheet.Amount += Rank(SoulHunter.Core.Persistence.PowerUpId.Amount);
                sheet.MoveSpeed += Bonus(SoulHunter.Core.Persistence.PowerUpId.MoveSpeed);
                sheet.Magnet *= SoulHunter.Core.Persistence.PowerUps.MagnetMultiplier(Rank(SoulHunter.Core.Persistence.PowerUpId.Magnet));
                sheet.Luck += Bonus(SoulHunter.Core.Persistence.PowerUpId.Luck);
                sheet.Growth += Bonus(SoulHunter.Core.Persistence.PowerUpId.Growth);
                sheet.Greed += Bonus(SoulHunter.Core.Persistence.PowerUpId.Greed);
                sheet.Curse += Bonus(SoulHunter.Core.Persistence.PowerUpId.Curse);
                sheet.Revivals += Rank(SoulHunter.Core.Persistence.PowerUpId.Revival);
                sheet.Reroll += UnityEngine.Mathf.RoundToInt(Bonus(SoulHunter.Core.Persistence.PowerUpId.Reroll));
                sheet.Skip += UnityEngine.Mathf.RoundToInt(Bonus(SoulHunter.Core.Persistence.PowerUpId.Skip));
                sheet.Banish += UnityEngine.Mathf.RoundToInt(Bonus(SoulHunter.Core.Persistence.PowerUpId.Banish));
            }
            return sheet;
        }

        /// <summary>One stat line as the character select screen shows it; Changed marks a bonus or malus.</summary>
        public struct Row
        {
            public string Label;
            public string Value;
            public bool Changed;
            /// <summary>The PowerUps shop changed this value (VS shows it in yellow on the character select screen).</summary>
            public bool FromPowerUps;
            public Row(string label, string value, bool changed) { Label = label; Value = value; Changed = changed; FromPowerUps = false; }
        }

        /// <summary>VS-style stat list: absolute health, flat bonuses as +N, multipliers as +/-%, "-" when unchanged.</summary>
        public Row[] DisplayRows() => new[]
        {
            new Row("Max Health", MaxHealth.ToString(), MaxHealth != 100),
            new Row("Recovery", Recovery > 0f ? Recovery.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) : "-", Recovery > 0f),
            Flat("Armor", Armor),
            Percent("Move Speed", MoveSpeed),
            Percent("Might", Might),
            Percent("Speed", Speed),
            Percent("Duration", Duration),
            Percent("Area", Area),
            Percent("Cooldown", Cooldown),
            Flat("Amount", Amount),
            Flat("Revival", Revivals),
            Percent("Magnet", Magnet),
            Percent("Luck", Luck),
            Percent("Growth", Growth),
            Percent("Greed", Greed),
            Percent("Curse", Curse),
        };

        private static Row Flat(string label, int value) =>
            new Row(label, value == 0 ? "-" : (value > 0 ? "+" : "") + value, value != 0);

        private static Row Percent(string label, float multiplier)
        {
            int percent = UnityEngine.Mathf.RoundToInt((multiplier - 1f) * 100f);
            return new Row(label, percent == 0 ? "-" : (percent > 0 ? "+" : "") + percent + "%", percent != 0);
        }
    }
}
