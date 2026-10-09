using System.Collections.Generic;
using SoulHunter.Core.Persistence;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// The Bestiary's entries and what each shows (release item P3-02, 2026-10-09). vampire.survivors.wiki/w/Ars_Gouda (raw,
    /// fetched 2026-10-09): the header shows "The number of unique enemies ever defeated" and "The maximum number of unique
    /// enemies the player can encounter"; the left panel lists the enemies by Bestiary number, "An enemy that isn't
    /// encountered yet will have their number listed, but their name listed as ---------------"; the right panel shows "The
    /// number of times this enemy has been defeated by the player", the enemy's looks, "An entry that describes the enemy",
    /// its "HP", "Power", "Speed", "resistances", "skills" and "The stages that the enemy appears in". The Bestiary opens on
    /// the first enemy. Enemies come from GameContent.BestiaryEnemies, in order: Level 1's VS enemy types, then each level's
    /// enemy, elite and boss. Not built: descriptions (Soul Hunter has no enemy texts yet), enemy pictures (no portraits
    /// exist; owner: no new art) and yellow names for unlock-tied enemies (no Soul Hunter unlock asks for kills of one type).
    /// </summary>
    public static class BestiaryCatalog
    {
        public const string UnknownName = "---------------";
        public const string UnknownValue = "?";

        public static IReadOnlyList<EnemyData> All()
        {
            var catalog = GameContentCatalog.Load();
            var list = new List<EnemyData>();
            if (catalog?.BestiaryEnemies == null) return list;
            foreach (var enemy in catalog.BestiaryEnemies)
                if (enemy != null && !list.Contains(enemy)) list.Add(enemy);
            return list;
        }

        /// <summary>The save id of an enemy type: its EnemyData asset name.</summary>
        public static string IdOf(EnemyData enemy) => enemy != null ? enemy.name : null;

        public static string Name(EnemyData enemy) =>
            enemy == null ? UnknownName : !string.IsNullOrEmpty(enemy.EnemyName) ? enemy.EnemyName : enemy.name;

        /// <summary>The level an enemy belongs to, from its asset name ("L04_..." is Level 4); 0 when it has none.</summary>
        public static int LevelOf(EnemyData enemy)
        {
            string id = IdOf(enemy);
            if (id == null || id.Length < 3 || id[0] != 'L' || !int.TryParse(id.Substring(1, 2), out int level)) return 0;
            return level;
        }

        public static string Stages(EnemyData enemy)
        {
            int level = LevelOf(enemy);
            return level > 0 ? $"Level {level}: {CampaignLevelCatalog.Get(level).Name}" : UnknownValue;
        }

        /// <summary>VS "HP": its health, or the VS "HP x Level" skill (health = this x the player's level).</summary>
        public static string Health(EnemyData enemy) =>
            enemy.HealthPerPlayerLevel > 0 ? $"{enemy.HealthPerPlayerLevel} x player level" : enemy.MaxHealth.ToString();

        /// <summary>VS "Power": the damage it deals to the player.</summary>
        public static string Power(EnemyData enemy) => enemy.DamageToPlayer.ToString();

        /// <summary>VS "Speed", on the wiki's scale (VS 100 = 3 units a second, EnemyAbilityDefaults.VsSpeedToUnits).</summary>
        public static string Speed(EnemyData enemy) =>
            UnityEngine.Mathf.RoundToInt(enemy.MoveSpeed / EnemyAbilityDefaults.VsSpeedToUnits).ToString();

        public static string Resistances(EnemyData enemy)
        {
            var list = new List<string>();
            if (enemy.FreezeResistance > 0f) list.Add($"Freeze {enemy.FreezeResistance:0.#}");
            if (enemy.InstantKillResistance) list.Add("Instant kill");
            if (enemy.DebuffResistance) list.Add("Debuffs");
            if (enemy.Knockback <= 0f) list.Add("Knockback");
            return list.Count > 0 ? string.Join(", ", list) : "None";
        }

        public static string Skills(EnemyData enemy)
        {
            var list = new List<string>();
            if (enemy.Movement == EnemyMovement.Floaty) list.Add("Floaty");
            else if (enemy.Movement == EnemyMovement.FixedDirection) list.Add("Fixed direction");
            if (enemy.HealthPerPlayerLevel > 0) list.Add("HP x Level");
            if (enemy.Abilities != null)
                foreach (var ability in enemy.Abilities)
                    if (ability != null) list.Add(Spaced(ability.Kind.ToString()));
            return list.Count > 0 ? string.Join(", ", list) : "None";
        }

        /// <summary>Unique enemy types this save has defeated, among the Bestiary's.</summary>
        public static int KnownCount(GameData data)
        {
            int n = 0;
            foreach (var enemy in All()) if (Bestiary.IsKnown(data, IdOf(enemy))) n++;
            return n;
        }

        private static string Spaced(string name)
        {
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i])) text.Append(' ');
                text.Append(i > 0 && char.IsUpper(name[i]) ? char.ToLowerInvariant(name[i]) : name[i]);
            }
            return text.ToString();
        }
    }
}
