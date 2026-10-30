using System.Collections.Generic;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// The weapons the Arcanas talk about, as Soul Hunter has them (evolutions are their own entries, as on the wiki's
    /// lists). VS weapon -> Soul Hunter weapon: Hellfire = Hellmaw Comet, La Borra = Drowning Chalice, NO FUTURE = Severed
    /// Fate, Thunder Loop = Stormbound Halo, Gorgeous Moon = Eclipse Sigil, Mannajja = Requiem of Souls, Phiera Der
    /// Tuphello = Gun, Nduja Fritta Tanto = Hellfire Breath (AGENTS.md, Level 1 step 3 and VS stage rules item 4).
    /// </summary>
    public enum ArcanaWeapon
    {
        None, Whip, BloodyTear, MagicWand, HolyWand, Knife, ThousandEdge, Axe, DeathSpiral, Cross, HeavenSword, Bible,
        UnholyVespers, FireWand, HellmawComet, Garlic, SoulEater, SantaWater, DrowningChalice, Runetracer, SeveredFate,
        LightningRing, StormboundHalo, Pentagram, EclipseSigil, Peachone, Bone, CherryBomb, Gun, SongOfMana,
        RequiemOfSouls, ClockLancet, Laurel, CrimsonShroud, HellfireBreath,
        /// <summary>
        /// VS Eight The Sparrow (added 2026-10-12). Its page lists Gemini and Iron Blue Will like the Gun, and Jail of
        /// Crystal where the Gun has Heart of Fire, so it is its own entry. Dusk Raven (VS Ebony Wings) needs none: its
        /// page lists exactly Peachone's cards, and it is identified as Peachone.
        /// </summary>
        AzureRepeater
    }

    /// <summary>
    /// The 22 base Arcanas (release item P2-01, 2026-10-09): the VS card each one is (by its tarot numeral, kept as the
    /// card's slot), Soul Hunter's own name and text (owner, 2026-10-09: own English names tied to the effect, never the
    /// VS names; Awake, Gemini, Iron Blue Will and Waltz of Pearls keep theirs), and the Soul Hunter weapons each card's
    /// wiki page lists. Every number comes from the card's own page, re-fetched raw on 2026-10-09
    /// (vampire.survivors.wiki/w/&lt;Card&gt;?action=raw); the rules themselves live in ArcanaManager. VS names appear here
    /// only as the mapping, never on screen.
    /// </summary>
    public static class ArcanaCatalog
    {
        public readonly struct Card
        {
            public readonly ArcanaData.ArcanaType Type;
            /// <summary>The tarot numeral (the VS card's slot, 0-XXI).</summary>
            public readonly string Numeral;
            /// <summary>Soul Hunter's own name.</summary>
            public readonly string Name;
            public readonly string Description;
            /// <summary>The VS card it maps to (never shown in the game).</summary>
            public readonly string VsName;
            /// <summary>The Soul Hunter weapons the card's wiki page lists.</summary>
            public readonly ArcanaWeapon[] Weapons;
            /// <summary>What the page lists that Soul Hunter does not have (not built), for the record.</summary>
            public readonly string NotInSoulHunter;

            public Card(ArcanaData.ArcanaType type, string numeral, string name, string vsName, string description,
                        ArcanaWeapon[] weapons, string notInSoulHunter)
            {
                Type = type; Numeral = numeral; Name = name; VsName = vsName; Description = description;
                Weapons = weapons ?? System.Array.Empty<ArcanaWeapon>(); NotInSoulHunter = notInSoulHunter ?? "";
            }
        }

        private static ArcanaWeapon[] W(params ArcanaWeapon[] weapons) => weapons;

        /// <summary>All 22 cards, in numeral order (0, I ... XXI).</summary>
        public static readonly Card[] All =
        {
            new Card(ArcanaData.ArcanaType.GameKiller, "0", "Soul Barrage", "Game Killer",
                "Soul Gems give no experience: each one you collect flies at the nearest enemy and explodes, harder with Growth. Every chest holds at least 3 items.",
                W(), "Stake to Your Heart (Darkana)"),
            new Card(ArcanaData.ArcanaType.Gemini, "I", "Gemini", "Gemini",
                "Pale Dove, Gun and Hellfire Breath gain a twin that levels with them and takes no slot.",
                W(ArcanaWeapon.Peachone, ArcanaWeapon.Gun, ArcanaWeapon.HellfireBreath), "Ebony Wings, Vandalier, Eight The Sparrow, Sorbetto and the DLC weapons"),
            new Card(ArcanaData.ArcanaType.TwilightRequiem, "II", "Final Echo", "Twilight Requiem",
                "Shots from Bible, Black Psalter, Lightning Ring, Stormbound Halo, Pale Dove, Rune Ricochet and Bone explode when they fade. Curse makes the blasts stronger.",
                W(ArcanaWeapon.Bible, ArcanaWeapon.UnholyVespers, ArcanaWeapon.LightningRing, ArcanaWeapon.StormboundHalo,
                  ArcanaWeapon.Peachone, ArcanaWeapon.Runetracer, ArcanaWeapon.Bone),
                "Ebony Wings, Shadow Pinion, Celestial Dusting, the Bracelets, Santa Javelin and the DLC weapons"),
            new Card(ArcanaData.ArcanaType.TragicPrincess, "III", "Restless Pilgrim", "Tragic Princess",
                "While you move, Garlic, Soul Devourer, Holy Water, Drowning Chalice, Lightning Ring and Stormbound Halo recharge faster. The faster you move, the faster they recharge.",
                W(ArcanaWeapon.Garlic, ArcanaWeapon.SoulEater, ArcanaWeapon.SantaWater, ArcanaWeapon.DrowningChalice,
                  ArcanaWeapon.LightningRing, ArcanaWeapon.StormboundHalo),
                "Santa Javelin, Levelin'Eh, Seraphic Cry, Carrello and the DLC weapons"),
            new Card(ArcanaData.ArcanaType.Awake, "IV", "Awake", "Awake",
                "+3 Revivals. Every Revival used grants x1.1 Max Health, +1 Armour and +5% Might, Area, Duration and Speed.",
                W(), ""),
            new Card(ArcanaData.ArcanaType.ChaosInTheDarkNight, "V", "Wavering Winds", "Chaos in the Dark Night",
                "Projectile Speed sways between -50% and +50% every 10 seconds. Each level gained adds +1% Speed.",
                W(), ""),
            new Card(ArcanaData.ArcanaType.SarabandeOfHealing, "VI", "Mending Wrath", "Sarabande of Healing",
                "All healing is doubled, and every heal strikes nearby enemies for the same amount.",
                W(ArcanaWeapon.BloodyTear, ArcanaWeapon.SoulEater),
                "Fuwalafuwaloo, Celestial Dusting, Night Sword and the DLC weapons"),
            new Card(ArcanaData.ArcanaType.IronBlueWill, "VII", "Iron Rebound", "Iron Blue Will",
                "Knife, Blade Storm, Axe and Gun shots bounce off up to 3 enemies; Reaper Spiral bounces off the screen edge once.",
                W(ArcanaWeapon.Knife, ArcanaWeapon.ThousandEdge, ArcanaWeapon.Axe, ArcanaWeapon.DeathSpiral, ArcanaWeapon.Gun),
                "Eight The Sparrow, Carrello and the DLC weapons"),
            new Card(ArcanaData.ArcanaType.MadGroove, "VIII", "Gathering Call", "Mad Groove",
                "Now and on every even minute, every item, chest, Soul Gem and Soul Lantern on the stage is drawn into a ring around you.",
                W(), "relics (Soul Hunter has none)"),
            new Card(ArcanaData.ArcanaType.DivineBloodline, "IX", "Ironblood Oath", "Divine Bloodline",
                "Cross, Bible, Garlic, Holy Water, Lightning Ring and Soul Hymn deal +5 damage per Armour. Every weapon deals +0.1 damage per missing health. Enemies that touch you take 10 damage per Armour, and enemies slain by retaliation give +0.5 Max Health.",
                W(ArcanaWeapon.Cross, ArcanaWeapon.Bible, ArcanaWeapon.Garlic, ArcanaWeapon.SantaWater,
                  ArcanaWeapon.LightningRing, ArcanaWeapon.SongOfMana),
                "Vento Sacro, Victory Sword and Silver Wind"),
            new Card(ArcanaData.ArcanaType.Beginning, "X", "First Blade", "Beginning",
                "Your starting weapon and its evolution fire +3 more. Bone and Cherry Bomb fire +1 more.",
                W(ArcanaWeapon.Bone, ArcanaWeapon.CherryBomb), "Carrello, Celestial Dusting and La Robba"),
            new Card(ArcanaData.ArcanaType.WaltzOfPearls, "XI", "Dancing Spheres", "Waltz of Pearls",
                "Magic Wand, Radiant Rod and Fire Wand shots bounce off up to 3 enemies; Hellmaw Comet bounces off the screen edge once, Cross and Sky Blade twice.",
                W(ArcanaWeapon.MagicWand, ArcanaWeapon.HolyWand, ArcanaWeapon.FireWand, ArcanaWeapon.HellmawComet,
                  ArcanaWeapon.Cross, ArcanaWeapon.HeavenSword),
                "Carrello and the DLC weapons"),
            new Card(ArcanaData.ArcanaType.OutOfBounds, "XII", "Shattering Frost", "Out of Bounds",
                "Every enemy you freeze bursts in a blast that grows stronger as the stage goes on. Time Freezes drop twice as often.",
                W(ArcanaWeapon.ClockLancet), "Infinite Corridor, Celestial Voulge, Mirage Robe and the Sorbetto pickup"),
            new Card(ArcanaData.ArcanaType.WickedSeason, "XIII", "Changing Fortunes", "Wicked Season",
                "Growth, Luck, Greed and Curse are doubled in turn, 10 seconds each. Every 2 levels gained add +1% to all four.",
                W(), ""),
            new Card(ArcanaData.ArcanaType.JailOfCrystal, "XIV", "Icebound Snare", "Jail of Crystal",
                "Magic Wand, Radiant Rod, Rune Ricochet and Severed Fate hits have a 25% chance to freeze, raised by Luck.",
                // Azure Repeater (VS Eight The Sparrow, added 2026-10-12; wiki Eight_The_Sparrow: "Jail of Crystal (XIV) -
                // Gives Eight The Sparrow a 25% chance to freeze enemies"). The card's text above is kept as it is: the
                // GameContent Arcana assets hold the same text and are checked against it (ArcanaCatalogTests).
                W(ArcanaWeapon.MagicWand, ArcanaWeapon.HolyWand, ArcanaWeapon.Runetracer, ArcanaWeapon.SeveredFate, ArcanaWeapon.AzureRepeater),
                "Cygnus, the Bracelets, Penshin Fatcha and the DLC weapons"),
            new Card(ArcanaData.ArcanaType.DiscoOfGold, "XV", "Midas Pulse", "Disco of Gold",
                "Picking up a coin bag starts a Gold Fever. Every gold you gain heals as much health.",
                W(), "Vicious Hunger"),
            new Card(ArcanaData.ArcanaType.Slash, "XVI", "Keen Edge", "Slash",
                "Whip, Knife, Axe, Blade Storm, Reaper Spiral and Crimson Lash can land critical hits, and every critical hit deals double damage.",
                W(ArcanaWeapon.Knife, ArcanaWeapon.ThousandEdge, ArcanaWeapon.Whip, ArcanaWeapon.BloodyTear, ArcanaWeapon.Axe,
                  ArcanaWeapon.DeathSpiral, ArcanaWeapon.HeavenSword),
                "Vento Sacro, Fuwalafuwaloo, Victory Sword, Celestial Voulge, Santa Javelin and the DLC weapons"),
            new Card(ArcanaData.ArcanaType.LostAndFoundPainting, "XVII", "Ebbing Hours", "Lost & Found Painting",
                "Duration sways between -50% and +50% every 10 seconds. Each level gained adds +1% Duration.",
                W(), ""),
            new Card(ArcanaData.ArcanaType.BoogalooOfIllusions, "XVIII", "Swelling Shadows", "Boogaloo of Illusions",
                "Area sways between -25% and +25% every 10 seconds. Each level gained adds +1% Area.",
                W(), ""),
            new Card(ArcanaData.ArcanaType.HeartOfFire, "XIX", "Burning Core", "Heart of Fire",
                "Fire Wand, Hellmaw Comet and Gun shots explode on impact. Soul Lanterns explode when broken, and you explode when hurt. Hellfire Breath drops four times as often.",
                W(ArcanaWeapon.FireWand, ArcanaWeapon.HellmawComet, ArcanaWeapon.Gun),
                "Valkyrie Turner, Red Muscle, Zhar Ptytsia, Tri-Bracelet, Flames of Misspell, Ammo Appalate and the DLC weapons"),
            new Card(ArcanaData.ArcanaType.SilentOldSanctuary, "XX", "Empty Hands", "Silent Old Sanctuary",
                "+3 Reroll, Skip and Banish. Each empty weapon slot gives +20% Might and -8% Cooldown.",
                W(), ""),
            new Card(ArcanaData.ArcanaType.BloodAstronomia, "XXI", "Crimson Orbit", "Blood Astronomia",
                "Enemies inside your Magnet range take damage that grows with Amount. Garlic, Soul Devourer, Pentagram, Eclipse Sigil, Soul Hymn, Requiem of Souls and Laurel also leave blood zones.",
                W(ArcanaWeapon.Garlic, ArcanaWeapon.SoulEater, ArcanaWeapon.Pentagram, ArcanaWeapon.EclipseSigil,
                  ArcanaWeapon.SongOfMana, ArcanaWeapon.RequiemOfSouls, ArcanaWeapon.ClockLancet, ArcanaWeapon.Laurel),
                "Sonic Dash and Rapidus Fio; Clock Lancet's section on the page is empty, so it gets no zone"),
        };

        private static Dictionary<ArcanaData.ArcanaType, Card> _byType;

        /// <summary>The card of a type (every type is in <see cref="All"/>).</summary>
        public static Card Of(ArcanaData.ArcanaType type)
        {
            if (_byType == null)
            {
                _byType = new Dictionary<ArcanaData.ArcanaType, Card>();
                foreach (var card in All) _byType[card.Type] = card;
            }
            return _byType[type];
        }

        /// <summary>True when the card's wiki page lists the weapon (the weapon Soul Hunter has).</summary>
        public static bool Lists(ArcanaData.ArcanaType type, ArcanaWeapon weapon)
        {
            if (weapon == ArcanaWeapon.None) return false;
            foreach (var listed in Of(type).Weapons) if (listed == weapon) return true;
            return false;
        }

        /// <summary>The player-facing name of a weapon in the "works with" line.</summary>
        public static string WeaponName(ArcanaWeapon weapon) => weapon switch
        {
            ArcanaWeapon.BloodyTear => "Crimson Lash",
            ArcanaWeapon.MagicWand => "Magic Wand",
            ArcanaWeapon.HolyWand => "Radiant Rod",
            ArcanaWeapon.ThousandEdge => "Blade Storm",
            ArcanaWeapon.DeathSpiral => "Reaper Spiral",
            ArcanaWeapon.HeavenSword => "Sky Blade",
            ArcanaWeapon.UnholyVespers => "Black Psalter",
            ArcanaWeapon.FireWand => "Fire Wand",
            ArcanaWeapon.HellmawComet => "Hellmaw Comet",
            ArcanaWeapon.SoulEater => "Soul Devourer",
            ArcanaWeapon.SantaWater => "Holy Water",
            ArcanaWeapon.DrowningChalice => "Drowning Chalice",
            ArcanaWeapon.SeveredFate => "Severed Fate",
            ArcanaWeapon.LightningRing => "Lightning Ring",
            ArcanaWeapon.StormboundHalo => "Stormbound Halo",
            ArcanaWeapon.EclipseSigil => "Eclipse Sigil",
            ArcanaWeapon.CherryBomb => "Cherry Bomb",
            ArcanaWeapon.SongOfMana => "Soul Hymn",
            ArcanaWeapon.RequiemOfSouls => "Requiem of Souls",
            ArcanaWeapon.ClockLancet => "Hour Lance",
            ArcanaWeapon.CrimsonShroud => "Blood Veil",
            ArcanaWeapon.AzureRepeater => "Azure Repeater",
            ArcanaWeapon.HellfireBreath => "Hellfire Breath",
            _ => SoulHunterNames.ReplaceAll(System.Text.RegularExpressions.Regex.Replace(weapon.ToString(), "(?<=[a-z])(?=[A-Z])", " ")) // one-word VS names (Peachone, Runetracer) too
        };
    }
}
