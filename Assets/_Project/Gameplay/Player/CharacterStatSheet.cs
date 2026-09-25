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
        public const float MightPerMetaLevel = 0.1f;
        public const int ArmorPerMetaLevel = 1;
        public const float GreedPerMetaLevel = 0.2f;
        public const int RevivalsPerMetaLevel = 1;

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

        /// <summary>Character stats plus meta power-up levels (0 when nothing was bought).</summary>
        public static CharacterStatSheet Build(CharacterData character, int metaMight = 0, int metaArmor = 0, int metaGreed = 0, int metaRevival = 0)
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
            sheet.Might += metaMight * MightPerMetaLevel;
            sheet.Armor += metaArmor * ArmorPerMetaLevel;
            sheet.Greed += metaGreed * GreedPerMetaLevel;
            sheet.Revivals += metaRevival * RevivalsPerMetaLevel;
            return sheet;
        }

        /// <summary>One stat line as the character select screen shows it; Changed marks a bonus or malus.</summary>
        public struct Row
        {
            public string Label;
            public string Value;
            public bool Changed;
            public Row(string label, string value, bool changed) { Label = label; Value = value; Changed = changed; }
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
