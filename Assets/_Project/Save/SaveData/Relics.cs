using System.Collections.Generic;

namespace SoulHunter.Core.Persistence
{
    /// <summary>The lasting gift of a relic, applied at the start of every run once the relic is on the save.</summary>
    public enum RelicBoon { None, Reroll, Skip, Banish, Luck, Greed, Growth, MoveSpeed, Might }

    /// <summary>One Soul Hunter relic: where it lies, the VS relic it stands for, and what it gives.</summary>
    public readonly struct RelicInfo
    {
        /// <summary>Soul Hunter's own name (never a VS name); also its Results and Collection name.</summary>
        public readonly string Name;
        /// <summary>The level (1-10) it lies on.</summary>
        public readonly int Level;
        /// <summary>Direction from the level's start (x east, z north); the spawner sets the distance.</summary>
        public readonly float DirX, DirZ;
        /// <summary>The level clock, in seconds, from which it lies on the map (0 = from the start).</summary>
        public readonly int AppearsAtSeconds;
        /// <summary>The VS relic and what it unlocks there (documentation; never shown in the game).</summary>
        public readonly string VsRelic;
        /// <summary>A PowerUp it unlocks for good, or null.</summary>
        public readonly PowerUpId? UnlocksPowerUp;
        public readonly bool OpensBestiary;
        public readonly RelicBoon Boon;
        /// <summary>The boon's size: whole charges for Reroll/Skip/Banish, a fraction (0.05 = +5%) for the stats.</summary>
        public readonly float Amount;

        public RelicInfo(string name, int level, float dirX, float dirZ, int appearsAtSeconds, string vsRelic,
            PowerUpId? unlocksPowerUp, bool opensBestiary, RelicBoon boon, float amount)
        {
            Name = name; Level = level; DirX = dirX; DirZ = dirZ; AppearsAtSeconds = appearsAtSeconds; VsRelic = vsRelic;
            UnlocksPowerUp = unlocksPowerUp; OpensBestiary = opensBestiary; Boon = boon; Amount = amount;
        }
    }

    /// <summary>
    /// Soul Hunter's relics (owner, 2026-10-12: build the relic row "as new systems, with some additions"; "relics must
    /// feel worth finding"). vampire.survivors.wiki/w/Relics (raw, fetched 2026-10-12): "Relics are permanent items that
    /// unlock new mechanics and features once collected. They are very similar to stage items as they are simply spawned on
    /// the map and are collected upon touching them. Once collected, they are permanently added to the collection menu and
    /// do not appear again." Each relic's own wiki page (fetched 2026-10-12) gives its stage, place and effect.
    ///
    /// The owner already opened most of the features VS relics unlock (the evolution list, every run mode, music
    /// selection), and nothing the owner opened is locked again. So:
    /// - Three relics unlock what Soul Hunter still locked: the Charm, Preserve and Defang PowerUps (wiki Apoplexy,
    ///   Wax_Fetish, Antidote: "Once collected, it permanently unlocks the ... PowerUp").
    /// - One opens the Bestiary (wiki Ars_Gouda), beside the existing rule (a run that reaches Level 3).
    /// - Every relic whose VS feature is already open gives a small lasting boon instead (Soul Hunter's addition, the
    ///   owner's balance method). Sizes: a stat boon is at most +5% of the stat's base value (the owner's 5% rule, and no
    ///   two relics raise the same stat); a charge boon is +1 (half a rank of the VS PowerUp, which gives 2 per rank).
    ///   Each boon follows its relic's VS theme; the reason is on its row.
    ///
    /// A found relic is one Collection entry ("Pickup." + its name, the id the Results pickup list already produces), so
    /// the save has a single record of it. Not built: the Milky Way Map's relic (the pause map is another task), Glass
    /// Vizard (the owner keeps the merchant on Level 3), Yellow Sign (its items are already open), the Forbidden Scrolls,
    /// the Eudaimonia Machine, and each relic's VS guards.
    /// </summary>
    public static class RelicBook
    {
        /// <summary>
        /// Gold for a relic found again in any way that leaves nothing new to give (never happens in play: a found relic
        /// "does not appear again"). Kept at the wiki's standard achievement reward ("500 gold coins", wiki Achievements).
        /// </summary>
        public const int FallbackGold = 500;

        public static readonly RelicInfo[] All =
        {
            // wiki Achievements: "Find the Apoplexy at minute 9:00 in Bat Country" -> Charm. Bat Country reuses the Mad
            // Forest's ground (wiki Wax_Fetish trivia: the spell "recycletheforest for Bat Country"), so it lies on Level 1
            // (Mad Forest's place) from 9:00. North: the wiki gives no place.
            new RelicInfo("Hordecaller's Bell", 1, 0f, 1f, 540, "Apoplexy (Bat Country, 9:00): unlocks the Charm PowerUp",
                PowerUpId.Charm, false, RelicBoon.None, 0f),
            // wiki Grim_Grimoire: "located to the far west of the starting area in Inlaid Library"; unlocks the evolution
            // list, which the owner opened from the start. Boon: +1 Reroll per run (a book of recipes helps look for the
            // item a recipe needs).
            new RelicInfo("Reaper's Ledger", 2, -1f, 0f, 0, "Grim Grimoire (Inlaid Library, far west): evolution list",
                null, false, RelicBoon.Reroll, 1f),
            // wiki Ars_Gouda: "located to the far south of the starting area in Dairy Plant"; "permanently unlocks the
            // Bestiary". Soul Hunter's Bestiary also opens when a run reaches Level 3, so the relic adds +1 Banish per run
            // (knowing the enemies, the hunter knows what to leave behind).
            new RelicInfo("Beast Codex", 3, 0f, -1f, 0, "Ars Gouda (Dairy Plant, far south): Bestiary",
                null, true, RelicBoon.Banish, 1f),
            // wiki Wax_Fetish: "located in Mazerella ... permanently unlocks the Preserve PowerUp"; Mazerella reuses the
            // Dairy Plant ("recycletheplant for Mazerella"), so Level 3. North, away from the Beast Codex (wiki: no place).
            new RelicInfo("Candle Idol", 3, 0f, 1f, 0, "Wax Fetish (Mazerella): unlocks the Preserve PowerUp",
                PowerUpId.Preserve, false, RelicBoon.None, 0f),
            // wiki Sorceress'_Tears: "located to the far south from the starting area in Gallo Tower"; unlocks Hurry mode,
            // open since 2026-09-27. Boon: +3% Move Speed (a hurry of its own; VS Move Speed PowerUp is 5% per rank).
            new RelicInfo("Hourglass Tear", 4, 0f, -1f, 0, "Sorceress' Tears (Gallo Tower, far south): Hurry mode",
                null, false, RelicBoon.MoveSpeed, 0.03f),
            // wiki Randomazzo: "located to the far north of the starting area in Gallo Tower"; enables Arcanas, open as a
            // mode. Boon: +5% Luck (a deck of fates; VS Luck PowerUp is 10% per rank).
            new RelicInfo("Fate Deck", 4, 0f, 1f, 0, "Randomazzo (Gallo Tower, far north): Arcanas",
                null, false, RelicBoon.Luck, 0.05f),
            // wiki Great_Gospel: "located in Cappella Magna ... permanently unlocks Limit Break", open as a mode. Boon: +3%
            // Might (Limit Break's own gift is weapon power; VS Might PowerUp is 5% per rank). East: no place on the wiki row read.
            new RelicInfo("Oathbound Psalm", 5, 1f, 0f, 0, "Great Gospel (Cappella Magna): Limit Break",
                null, false, RelicBoon.Might, 0.03f),
            // wiki Antidote: "located in Whiteout ... permanently unlocks the Defang PowerUp". Soul Hunter's snow level is
            // Level 6, the Frozen Peaks (placement by theme; the wiki's stage has no counterpart in the level mapping).
            new RelicInfo("Fangless Vial", 6, -1f, 0f, 0, "Antidote (Whiteout): unlocks the Defang PowerUp",
                PowerUpId.Defang, false, RelicBoon.None, 0f),
            // wiki Magic_Banger: "located to the north-east of the starting area in Green Acres"; unlocks music selection,
            // open since 2026-10-09. Green Acres has no place in the level mapping; Level 7 has no other relic. Boon: +5%
            // Greed (a song worth coin; VS Greed PowerUp is 10% per rank).
            new RelicInfo("Bard's Echo", 7, 1f, 1f, 0, "Magic Banger (Green Acres, north-east): music selection",
                null, false, RelicBoon.Greed, 0.05f),
            // wiki Gracia's_Mirror: "found in Eudaimonia Machine ... unlocks inverse mode", open as a mode. Level 8 by
            // balance (the late levels had no relic). Boon: +3% Growth (VS Growth PowerUp is 3% per rank).
            new RelicInfo("Mirror Shard", 8, 0f, 1f, 0, "Gracia's Mirror (Eudaimonia Machine): Inverse mode",
                null, false, RelicBoon.Growth, 0.03f),
            // wiki Seventh_Trumpet: "found in Eudaimonia Machine ... unlocks endless mode", open as a mode. Level 9 by
            // balance. Boon: +1 Skip per run (an endless road is walked by passing things by).
            new RelicInfo("Last Horn", 9, 0f, -1f, 0, "Seventh Trumpet (Eudaimonia Machine): Endless mode",
                null, false, RelicBoon.Skip, 1f),
        };

        /// <summary>A relic's stable id: its name's letters only ("Hordecaller's Bell" -> "HordecallersBell"). It never changes with the shown text's spacing.</summary>
        public static string IdOf(in RelicInfo relic)
        {
            var id = new System.Text.StringBuilder();
            foreach (char c in relic.Name) if (char.IsLetterOrDigit(c)) id.Append(c);
            return id.ToString();
        }

        private static List<string> _ids;

        /// <summary>Every relic's stable id, in table order (other systems key their own content by these).</summary>
        public static IReadOnlyList<string> Ids
        {
            get
            {
                if (_ids == null)
                {
                    _ids = new List<string>();
                    foreach (var relic in All) _ids.Add(IdOf(relic));
                }
                return _ids;
            }
        }

        /// <summary>The relic with the stable id <paramref name="id"/>, or null.</summary>
        public static RelicInfo? FindById(string id)
        {
            foreach (var relic in All) if (IdOf(relic) == id) return relic;
            return null;
        }

        /// <summary>The save's record of a relic: its Collection entry.</summary>
        public static string CollectionId(in RelicInfo relic) => "Pickup." + relic.Name;

        public static bool IsFound(GameData data, in RelicInfo relic) => CollectionBook.IsFound(data, CollectionId(relic));

        public static RelicInfo? Find(string name)
        {
            foreach (var relic in All) if (relic.Name == name) return relic;
            return null;
        }

        /// <summary>The relics of one level.</summary>
        public static List<RelicInfo> OnLevel(int level)
        {
            var list = new List<RelicInfo>();
            foreach (var relic in All) if (relic.Level == level) list.Add(relic);
            return list;
        }

        /// <summary>What the relic gives, in one short line (the notice, the Unlocks screen, the Collection).</summary>
        public static string GiftText(in RelicInfo relic)
        {
            var parts = new List<string>();
            if (relic.UnlocksPowerUp.HasValue) parts.Add($"unlocks the {PowerUps.Info(relic.UnlocksPowerUp.Value).Name} PowerUp");
            if (relic.OpensBestiary) parts.Add("opens the Bestiary");
            switch (relic.Boon)
            {
                case RelicBoon.Reroll: parts.Add($"+{(int)relic.Amount} Reroll in every run"); break;
                case RelicBoon.Skip: parts.Add($"+{(int)relic.Amount} Skip in every run"); break;
                case RelicBoon.Banish: parts.Add($"+{(int)relic.Amount} Banish in every run"); break;
                case RelicBoon.Luck: parts.Add($"+{Percent(relic.Amount)}% Luck in every run"); break;
                case RelicBoon.Greed: parts.Add($"+{Percent(relic.Amount)}% Greed in every run"); break;
                case RelicBoon.Growth: parts.Add($"+{Percent(relic.Amount)}% Growth in every run"); break;
                case RelicBoon.MoveSpeed: parts.Add($"+{Percent(relic.Amount)}% Move Speed in every run"); break;
                case RelicBoon.Might: parts.Add($"+{Percent(relic.Amount)}% Might in every run"); break;
            }
            string text = string.Join(", ", parts);
            return text.Length > 0 ? char.ToUpperInvariant(text[0]) + text.Substring(1) + "." : "";
        }

        private static int Percent(float fraction) => (int)System.Math.Round(fraction * 100f);

        /// <summary>The boon of one kind the save's relics give together (charges, or a fraction).</summary>
        public static float BoonTotal(GameData data, RelicBoon boon)
        {
            float total = 0f;
            if (data == null || boon == RelicBoon.None) return 0f;
            foreach (var relic in All)
                if (relic.Boon == boon && IsFound(data, relic)) total += relic.Amount;
            return total;
        }

        /// <summary>How many relics the save has found.</summary>
        public static int FoundCount(GameData data)
        {
            int n = 0;
            foreach (var relic in All) if (IsFound(data, relic)) n++;
            return n;
        }

        /// <summary>
        /// Records the relic on the save with what it unlocks. Returns the notice lines (the relic first, then any count
        /// unlock its Collection entry reached); empty when it was already found.
        /// </summary>
        public static List<string> MarkFound(GameData data, in RelicInfo relic)
        {
            var lines = new List<string>();
            if (data == null || IsFound(data, relic)) return lines;
            var countUnlocks = CollectionBook.MarkFound(data, CollectionId(relic));
            data.UnlockedAchievements ??= new List<string>();
            if (relic.UnlocksPowerUp.HasValue)
            {
                string id = PowerUpUnlocks.WholeId(relic.UnlocksPowerUp.Value);
                if (!data.UnlockedAchievements.Contains(id)) data.UnlockedAchievements.Add(id);
            }
            if (relic.OpensBestiary) Bestiary.Unlock(data);
            lines.Add($"Relic found: {relic.Name}. {GiftText(relic)}");
            lines.AddRange(countUnlocks);
            return lines;
        }
    }

    /// <summary>One Soul Casket: the level it lies on and the character it frees.</summary>
    public readonly struct CasketInfo
    {
        public readonly int Level;
        public readonly string Character;
        public readonly string VsCoffin;

        public CasketInfo(int level, string character, string vsCoffin)
        {
            Level = level; Character = character; VsCoffin = vsCoffin;
        }
    }

    /// <summary>
    /// Soul Caskets, Soul Hunter's coffins (owner, 2026-10-12). vampire.survivors.wiki/w/Coffin (raw, fetched 2026-10-12):
    /// "Coffins are objects found in certain stages that can be opened to unlock new characters for purchase"; "Upon
    /// arriving at the coffin, an expanding and contracting ring of enemies appears around it. Once all of these enemies
    /// are defeated, the player can open the coffin by moving over it"; "The rescued character joins the survivors and can
    /// be purchased".
    /// The wiki's coffin stages that the level mapping has: Mad Forest, Inlaid Library, Dairy Plant, Gallo Tower, Mt. Moonspell,
    /// Lake Foscari and Cappella Magna, so Levels 1-4, 6, 7 and 8 each hold one casket. Abyss Foscari (Level 5) is not on the
    /// Coffin page's list, so Level 5 has none; Cappella Magna's coffin ("south of the starting area") lies on Level 8, the
    /// first of its three levels (2026-10-29: it was on Level 5 from the older mapping, where Levels 5-10 all read Cappella). Soul Hunter's addition: a casket frees an
    /// EXISTING locked character, the one whose origin level it lies on, as a second way to that character; the
    /// character's own condition stays. A freed character is still bought with gold at its own price (the wiki's 1,000
    /// minimum for coffin characters is not used: the owner's prices stay).
    /// Where the wiki is silent or has no counterpart (balance method):
    /// - A casket already opened on the save is not placed again. The wiki's "Opening any previously-opened coffin will
    ///   unlock Random" needs VS's Random character, which Soul Hunter does not have.
    /// - If the character was already unlocked by its own condition, opening the casket pays <see cref="AlreadyFreeGold"/>.
    /// </summary>
    public static class CasketBook
    {
        public const string PickupName = "Soul Casket";
        /// <summary>The Collection entry every opened casket fills (one entry, as every Soul Reliquary size is one).</summary>
        public const string CollectionId = "Pickup." + PickupName;
        /// <summary>The wiki's standard achievement reward ("500 gold coins", wiki Achievements), before Greed.</summary>
        public const int AlreadyFreeGold = 500;
        /// <summary>What the Unlocks screen shows as the done condition of a character a casket freed.</summary>
        public const string FreedText = "Freed from a Soul Casket.";

        public static readonly CasketInfo[] All =
        {
            new CasketInfo(1, "Corvan", "Coffin: Mad Forest (Pugnala)"),
            new CasketInfo(2, "Draven", "Coffin: Inlaid Library (Giovanna)"),
            new CasketInfo(3, "Gideon", "Coffin: Dairy Plant (Poppea)"),
            new CasketInfo(4, "Kaelen", "Coffin: Gallo Tower (Concetta)"),
            new CasketInfo(6, "Seraphina", "Coffin: Mt. Moonspell (Miang)"),
            new CasketInfo(7, "Lumina", "Coffin: Lake Foscari (Eleanor)"),
            new CasketInfo(8, "Rurik", "Coffin: Cappella Magna (Zi'Assunta)"),
        };

        public static string OpenedId(int level) => "Casket.Opened.L" + level;
        public static string FreedId(string character) => "Casket.Freed." + character;

        public static CasketInfo? OnLevel(int level)
        {
            foreach (var casket in All) if (casket.Level == level) return casket;
            return null;
        }

        private static bool Has(GameData data, string id) => data?.UnlockedAchievements != null && data.UnlockedAchievements.Contains(id);

        public static bool IsOpened(GameData data, int level) => Has(data, OpenedId(level));

        /// <summary>True when a casket (not the character's own condition) unlocked <paramref name="character"/>.</summary>
        public static bool WasFreed(GameData data, string character) => Has(data, FreedId(character));

        /// <summary>
        /// Records the casket as opened and its character as unlocked. <paramref name="freed"/> is true when the casket is
        /// what unlocked the character (false: it was unlocked already). Returns false when this casket was opened before.
        /// </summary>
        public static bool Open(GameData data, in CasketInfo casket, out bool freed)
        {
            freed = false;
            if (data == null || IsOpened(data, casket.Level)) return false;
            data.UnlockedAchievements ??= new List<string>();
            data.UnlockedAchievements.Add(OpenedId(casket.Level));
            data.UnlockedCharacters ??= new List<string>();
            if (!data.UnlockedCharacters.Contains(casket.Character))
            {
                data.UnlockedCharacters.Add(casket.Character);
                data.UnlockedAchievements.Add(FreedId(casket.Character));
                freed = true;
            }
            return true;
        }
    }
}
