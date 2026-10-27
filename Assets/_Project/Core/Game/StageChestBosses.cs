using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Each Soul Hunter level's chest bosses: the minute, the VS boss and its {{treasure}} config, from the source stage's
    /// "Waves" table (raw pages fetched 2026-10-27). Source stages: L1 Mad Forest, L2 Inlaid Library, L3 Dairy Plant,
    /// L4 Gallo Tower, L5 Abyss Foscari, L6 Mt.Moonspell, L7 Lake Foscari, L8-L10 Cappella Magna.
    /// Arcana chests (11:00 and 21:00, "reward1=arcana") are left out: they come only in Arcana mode
    /// (<c>EnemySpawner.ArcanaChestMinutes</c>). The 25:00 stage boss is <see cref="StageBossFor"/>.
    /// Wiki gaps, settled by the balance method:
    /// - a config with no "level" shows "Level: -" in Template:Treasure; it uses the one fallback the wiki defines, level 1;
    /// - Inlaid Library 18:00 Master Witch writes "level2=10|level1=100", read as tier2 / tier1.
    /// </summary>
    public static class StageChestBosses
    {
        public readonly struct Row
        {
            public readonly float Minute;
            /// <summary>The VS boss the row is from (for docs and tests; the game shows Soul Hunter's names).</summary>
            public readonly string VsBoss;
            public readonly ChestTiers Tiers;
            public Row(float minute, string vsBoss, ChestTiers tiers) { Minute = minute; VsBoss = vsBoss; Tiers = tiers; }
        }

        private const int W1 = 1, W4 = 8, W5 = 16, E2 = 2, E3 = 4, E4 = 8, E5 = 16;

        private static Row R(float minute, string boss, int level, bool evo, float five, float three, float one,
            int weaponSlots = 0, int evolutionSlots = 0) =>
            new Row(minute, boss, new ChestTiers(level, evo, five, three, one, weaponSlots).WithEvolutionSlots(evolutionSlots));

        private static readonly Row[] MadForest =
        {
            R(1, "Glowing Bat", 1, true, 0, 0, 30),
            R(3, "Glowing Bat", 1, false, 0, 5, 40, W1),
            R(5, "Mantichana", 1, false, 1, 5, 100, W1),
            R(7, "Glowing Bat", 2, false, 3, 10, 50, W1),
            R(9, "Silver Bat", 1, false, 3, 10, 50, W1 | W4 | W5),
            R(10, "Giant Mantichana", 1, true, 3, 10, 100, W4 | W5),
            R(12, "Glowing Bat", 1, true, 1, 5, 50),
            R(14, "Silver Bat", 1, true, 3, 10, 100),
            R(15, "Giant Werewolf", 1, true, 3, 10, 100),
            R(16, "Glowing Bat", 1, true, 1, 5, 50),
            R(18, "Silver Bat", 1, true, 3, 10, 100),
            R(20, "Giant Mummy", 1, true, 3, 10, 100),
            R(22, "Glowing Bat", 1, true, 3, 10, 100),
            R(23, "Silver Bat", 1, true, 3, 10, 100),
            R(27, "Glowing Bat", 1, true, 3, 10, 100),
            R(29, "Glowing Bat", 1, true, 3, 10, 100),
        };

        private static readonly Row[] InlaidLibrary =
        {
            R(3, "Giant Mummy", 1, false, 0.1f, 5, 100),
            R(5, "Giant Mummy", 1, false, 1, 5, 100, W1),
            R(8, "Colossal Musc Musc", 2, false, 3, 10, 50, W1),
            R(10, "Colossal Lionhead", 1, true, 3, 10, 100, W4 | W5),
            R(11, "Colossal Sneaky Head", 1, true, 0, 0, 100, W4 | W5),
            R(12, "Glowing Bat", 1, true, 1, 5, 50),
            R(13, "Colossal Dust Elemental", 1, true, 1, 5, 50),
            R(14, "Silver Bat", 1, true, 1, 5, 50),
            R(15, "Queen Medusa", 1, true, 3, 10, 80),
            R(16, "Glowing Bat", 1, true, 1, 5, 50),
            R(18, "Master Witch", 1, true, 3, 10, 100),
            R(20, "Nesuferit", 1, true, 3, 10, 100, 0, E2 | E3),
            R(22, "Glowing Bat", 1, true, 3, 10, 100),
            R(23, "Colossal Lionhead", 1, true, 3, 10, 100),
            R(26, "Queen Medusa", 1, true, 3, 10, 50),
            R(27, "Queen Medusa", 1, true, 3, 10, 50),
            R(28, "Queen Medusa", 1, true, 3, 10, 100),
        };

        private static readonly Row[] DairyPlant =
        {
            R(1, "Lizard Rook", 1, true, 0, 0, 30),
            R(3, "Minotaur Boss", 1, true, 3, 20, 100),
            R(5, "Minotaur Boss", 1, true, 3, 10, 100),
            R(7, "Merman Boss", 1, true, 3, 10, 100),
            R(9, "Minotaur Boss", 1, true, 3, 20, 100),
            R(10, "Minotaur Boss", 1, true, 3, 20, 100, 0, E2 | E3),
            R(12, "Giant Goat-Horned Shooter", 1, true, 3, 20, 100),
            R(16, "King Triton", 1, true, 3, 10, 100),
            R(18, "Axe Guardian", 1, true, 3, 10, 100),
            R(20, "Big Golem", 1, true, 3, 2, 100, 0, E2 | E3),
            R(22, "Big Golem", 1, true, 3, 10, 100),
            R(23, "Minotaur Boss", 1, true, 0, 0, 30),
            R(27, "Colossal Gallotrice", 1, true, 3, 10, 100),
        };

        private static readonly Row[] GalloTower =
        {
            R(3, "Elite Devil", 1, false, 0.1f, 5, 100),
            R(5, "Elite Devil", 1, false, 5, 10, 100, W1),
            R(8, "Giant Skullone", 2, false, 3, 10, 50, W1),
            R(10, "Colossal Harpy", 1, true, 3, 10, 100, W4 | W5, E2 | E3),
            R(12, "Elite Devil", 1, true, 1, 5, 50),
            R(13, "Colossal Bone Dragon", 1, true, 1, 5, 50),
            R(14, "Elite Devil", 1, true, 1, 5, 50),
            R(15, "Gallo", 1, true, 1, 10, 100),
            R(16, "Elite Devil", 1, true, 1, 5, 50),
            R(18, "Shield Guardian", 1, true, 3, 10, 100),
            R(20, "Colossal Flame Dragon", 1, true, 3, 10, 100, 0, E4 | E5),
            R(22, "Elite Devil", 1, true, 3, 10, 100),
            R(23, "Colossal Gallotrice", 1, true, 3, 10, 50),
            R(26, "Colossal Gallotrice", 1, true, 3, 10, 50),
            R(27, "Colossal Manticore", 1, true, 3, 10, 50),
            R(28, "Colossal Manticore", 1, true, 3, 10, 100),
        };

        private static readonly Row[] AbyssFoscari =
        {
            R(1, "Rotting Ghoul", 1, true, 0, 0, 100),
            R(3, "Colossal Redusa Head", 1, true, 3, 20, 100),
            R(5, "Colossal Redusa Head", 1, true, 3, 10, 100),
            R(7, "Flaming Skull", 1, true, 3, 20, 100),
            R(9, "Colossal Redusa Head", 1, true, 3, 20, 100),
            R(10, "Colossal Meatball", 1, true, 3, 20, 100, 0, E2 | E3),
            R(12, "Flaming Skull", 1, true, 3, 20, 100),
            R(16, "Warm Cauld", 1, true, 3, 10, 100),
            R(18, "Edna", 1, true, 3, 10, 100),
            R(20, "Well Well Well Dweller", 1, true, 3, 10, 100, 0, E2 | E3),
            R(22, "Warm Cauld", 1, true, 3, 10, 100),
            R(23, "Colossal Meatball", 1, true, 0, 0, 30),
            R(27, "Maronna Meea Boss", 1, true, 3, 10, 100),
        };

        private static readonly Row[] MtMoonspell =
        {
            R(1, "Spiritello Boss", 1, true, 0, 0, 50),
            R(3, "Spiritello Boss", 1, false, 0, 5, 40, W1),
            R(5, "Mikoshi-nyudo", 1, false, 1, 5, 100, W1),
            R(7, "Raiju Boss", 2, true, 3, 10, 50),
            R(9, "Spiritello Boss", 1, true, 3, 10, 50, W4 | W5),
            R(10, "Thunderous Oni", 1, true, 3, 10, 100, W4 | W5),
            R(12, "Tanuki Boss", 1, true, 1, 5, 50),
            R(14, "Spiritello Boss", 1, true, 3, 10, 100),
            R(15, "Spiritello Boss", 1, true, 3, 10, 100),
            R(16, "Spiritello Boss", 1, true, 1, 5, 50),
            R(18, "Spiritello Boss", 1, true, 3, 10, 100),
            R(20, "Kamaitachi Boss", 1, true, 3, 10, 100),
            R(22, "Spiritello Boss", 1, true, 3, 10, 100),
            R(23, "Tengu Boss", 1, true, 3, 10, 100),
            R(27, "Colossal Spiritello Boss", 1, true, 3, 10, 100, 0, E2 | E3 | E4 | E5),
            R(29, "Colossal Spiritello Boss", 1, true, 3, 10, 100, 0, E2 | E3 | E4 | E5),
        };

        private static readonly Row[] LakeFoscari =
        {
            R(2, "Foscaritrice", 1, true, 0.1f, 2, 50),
            R(3, "Foscaritrice", 1, false, 0.1f, 1, 100),
            R(5, "Colossal Foscaritrice", 1, false, 1, 5, 100),
            R(8, "Nutmeg Boss", 2, false, 3, 10, 50, W1),
            R(10, "Njuggles Boss", 1, true, 3, 10, 100, W4 | W5),
            R(11, "Sam the Boss Clown", 1, true, 0, 0, 100),
            R(12, "Colossal Foscaritrice", 1, true, 1, 5, 50),
            R(13, "Colossal Foscaritrice", 1, true, 1, 5, 50),
            R(14, "Njuggles Boss", 1, true, 1, 5, 50),
            R(15, "Vulvio Boss", 1, true, 3, 10, 100),
            R(16, "Sam the Boss Clown", 1, true, 1, 5, 50),
            R(18, "Lammuga Boss", 1, true, 3, 10, 100),
            R(20, "Missing Church Boss", 1, true, 3, 10, 100, 0, E2 | E3),
            R(22, "Deep Green Knight", 1, true, 3, 10, 100),
            R(23, "Deep Green Knight", 1, true, 3, 10, 100),
            R(26, "Big Green Knight", 1, true, 3, 10, 50),
            R(27, "Big Green Knight", 1, true, 3, 10, 100),
            R(28, "Deep Green Knight", 1, true, 3, 10, 100),
        };

        private static readonly Row[] CappellaMagna =
        {
            R(1, "Fallen Archangel", 1, true, 0, 0, 30),
            R(3, "Colossal Eyeball", 1, true, 3, 20, 100),
            R(5, "Colossal Fallen Archangel", 1, true, 3, 10, 100),
            R(7, "Colossal Eyeball", 1, true, 3, 20, 100),
            R(9, "Colossal Eyeball", 1, true, 3, 20, 100),
            R(10, "Colossal Succubus", 1, true, 3, 20, 100, 0, E2 | E3),
            R(12, "Colossal Eyeball", 1, true, 3, 20, 100),
            R(15, "Colossal Green Knight", 1, true, 3, 10, 100),
            R(18, "Axe Guardian", 1, true, 3, 10, 100),
            R(20, "Colossal Archdemon", 1, true, 3, 2, 100, 0, E2 | E3),
            R(22, "Colossal Eyeball", 1, true, 3, 10, 100),
            R(23, "Colossal Eyeball", 1, true, 0, 0, 30),
            R(24, "Colossal Eyeball", 1, true, 3, 10, 100),
            R(27, "Colossal Eyeball", 1, true, 3, 10, 100),
        };

        // The 25:00 stage bosses' {{treasure}} configs: Giant Blue Venus, Hag, Sword Guardian, Giant Enemy Crab, the
        // Fomorians, Orochimario, Avatar of Gaea, Trinacria.
        private static readonly ChestTiers[] StageBosses =
        {
            new ChestTiers(1, true, 3, 10, 100),
            new ChestTiers(1, true, 3, 10, 100),
            new ChestTiers(1, true, 3, 10, 100),
            new ChestTiers(1, true, 3, 10, 100).WithEvolutionSlots(E2 | E3 | E4 | E5),
            new ChestTiers(1, true, 3, 10, 100),
            new ChestTiers(1, true, 3, 10, 100).WithEvolutionSlots(E2 | E3 | E4 | E5),
            new ChestTiers(1, true, 3, 10, 100),
            new ChestTiers(1, true, 3, 10, 100),
        };

        private static int Source(int stage) => Mathf.Clamp(stage, 1, 10) >= 8 ? 8 : Mathf.Clamp(stage, 1, 10);

        /// <summary>The chest bosses of Soul Hunter level <paramref name="stage"/> (1-10), in minute order.</summary>
        public static IReadOnlyList<Row> For(int stage)
        {
            switch (Source(stage))
            {
                case 1: return MadForest;
                case 2: return InlaidLibrary;
                case 3: return DairyPlant;
                case 4: return GalloTower;
                case 5: return AbyssFoscari;
                case 6: return MtMoonspell;
                case 7: return LakeFoscari;
                default: return CappellaMagna;
            }
        }

        /// <summary>The minutes of <paramref name="stage"/>'s chest bosses.</summary>
        public static float[] MinutesFor(int stage)
        {
            var rows = For(stage);
            var minutes = new float[rows.Count];
            for (int i = 0; i < rows.Count; i++) minutes[i] = rows[i].Minute;
            return minutes;
        }

        /// <summary>The config of <paramref name="stage"/>'s chest boss at <paramref name="minute"/>; false off the table.</summary>
        public static bool TryFor(int stage, float minute, out ChestTiers tiers)
        {
            var rows = For(stage);
            for (int i = 0; i < rows.Count; i++)
                if (Mathf.Abs(rows[i].Minute - minute) < 0.01f) { tiers = rows[i].Tiers; return true; }
            tiers = default;
            return false;
        }

        /// <summary>The stage boss's chest config on <paramref name="stage"/>.</summary>
        public static ChestTiers StageBossFor(int stage) => StageBosses[Source(stage) - 1];
    }
}
