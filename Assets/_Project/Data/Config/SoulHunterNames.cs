using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// Owner, 2026-10-30 (release audit question 1, "Sirf VS naam badlo"; list approved "Haan, ye list lagao";
    /// Docs/Name_Changes_20261030.md): the names a player sees that were VS's own, and Soul Hunter's names for them.
    /// Only text a player reads changes. Code ids, enums, asset names, the save and every rule stay as they are. Evolution
    /// and union names are made from their ids (WeaponEvolutionData.DisplayName) and pass through <see cref="For"/>; the
    /// other visible texts were written with the new names. "Armor" becomes "Armour" (British English, question 7).
    /// </summary>
    public static class SoulHunterNames
    {
        public static readonly IReadOnlyDictionary<string, string> Renames = new Dictionary<string, string>
        {
            // Passives
            ["Candelabrador"] = "Grave Candle",
            ["Pummarola"] = "Heartroot",
            ["Tirajisu"] = "Undying Sweet",
            ["Tiragisu"] = "Undying Sweet",
            ["Tirajisú"] = "Undying Sweet",
            ["Skull O'Maniac"] = "Wrath Skull",
            ["Spellbinder"] = "Lingering Sigil",
            ["Attractorb"] = "Soul Lodestone",
            ["Duplicator"] = "Twin Mirror",
            ["Empty Tome"] = "Swift Grimoire",
            ["Hollow Heart"] = "Iron Heart",
            ["Metaglio Left"] = "Left Soul Gauntlet",
            ["Metaglio Right"] = "Right Soul Gauntlet",
            ["Armor"] = "Armour",
            // Weapons
            ["Peachone"] = "Pale Dove",
            ["Runetracer"] = "Rune Ricochet",
            ["Santa Water"] = "Holy Water",
            ["Song of Mana"] = "Soul Hymn",
            ["Song Of Mana"] = "Soul Hymn",
            ["Clock Lancet"] = "Hour Lance",
            // Evolutions and the Laurel's union
            ["Bloody Tear"] = "Crimson Lash",
            ["Heaven Sword"] = "Sky Blade",
            ["Thousand Edge"] = "Blade Storm",
            ["Death Spiral"] = "Reaper Spiral",
            ["Unholy Vespers"] = "Black Psalter",
            ["Holy Wand"] = "Radiant Rod",
            ["Soul Eater"] = "Soul Devourer",
            ["Crimson Shroud"] = "Blood Veil",
            // Arcanas
            ["Iron Blue Will"] = "Iron Rebound",
            ["Waltz of Pearls"] = "Dancing Spheres",
        };

        private static Regex _pattern;

        /// <summary>The Soul Hunter name for a whole name, or the name itself when it is not a VS name.</summary>
        public static string For(string name) =>
            !string.IsNullOrEmpty(name) && Renames.TryGetValue(name, out var renamed) ? renamed : name;

        /// <summary>Every VS name inside a text, as whole words, longest first.</summary>
        public static string ReplaceAll(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            _pattern ??= new Regex(@"(?<![\w'])(" + string.Join("|", Renames.Keys.OrderByDescending(k => k.Length).Select(Regex.Escape)) + @")(?![\w])");
            return _pattern.Replace(text, m => Renames[m.Value]);
        }
    }
}
