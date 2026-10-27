using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Release item P2-02 (owner, 2026-10-09): Levels 2-10 play their VS source stage's wave enemies with the wiki statbox
    /// stats (vampire.survivors.wiki raw pages fetched 2026-10-09): L2 Inlaid Library, L3 Dairy Plant, L4 Gallo Tower, L5
    /// Abyss Foscari, L6 Mt.Moonspell, L7 Lake Foscari, L8-L10 Cappella Magna. The test cases below were written from the
    /// statboxes themselves (health, HP x Level, power, speed, knockback, knockback cap, XP), not from the tables.
    /// </summary>
    public class StageEnemyTypeTests
    {
        [TestCase(2, "AshSprite", 5f, false, 5, 140f, 1f, 3f, 1f)] // Dust Elemental
        [TestCase(2, "SporeMote", 1f, false, 2, 160f, 1f, 3f, 1f)] // Musc Musc
        [TestCase(2, "SporeClump", 13f, false, 4, 160f, 1f, 3f, 1f)] // Big Musc Musc
        [TestCase(2, "VineBoundDead", 15f, false, 3, 140f, 1f, 3f, 2f)] // Mummy
        [TestCase(2, "ForestWisp", 10f, false, 5, 200f, 0f, 3f, 1.5f)] // Ghost 1
        [TestCase(2, "BarkHusk", 70f, false, 8, 100f, 0.3f, 3f, 2f)] // Testa di Mano 2
        [TestCase(2, "ElderBarkHusk", 150f, false, 8, 100f, 0.2f, 3f, 2f)] // Testa di Mano 3
        [TestCase(2, "ThornLion", 3f, true, 3, 200f, 1f, 3f, 2f)] // Lionhead
        [TestCase(2, "GloomMoth", 25f, false, 1, 240f, 1f, 3f, 2f)] // Big Sneaky Head
        [TestCase(2, "HuntingGloomMoth", 25f, false, 1, 240f, 1f, 3f, 2f)] // Aggressive Big Sneaky Head
        [TestCase(2, "BriarWitch", 220f, false, 14, 130f, 0.8f, 3f, 3f)] // Undead Witch
        [TestCase(2, "BriarMatron", 500f, false, 20, 200f, 1.5f, 3f, 3f)] // Undead Sassy Witch
        [TestCase(2, "LanternSkull", 6.5f, true, 14, 140f, 0f, 3f, 2f)] // Skullone (scaling)
        [TestCase(2, "GreatNightBat", 270f, false, 10, 140f, 0.1f, 3f, 2.5f)] // Giant Bat
        [TestCase(2, "GorgonWillow", 800f, false, 15, 150f, 1f, 3f, 3f)] // Merdusa
        [TestCase(2, "AshColossus", 50f, true, 10, 170f, 1f, 1f, 30f)] // Dust Elemental (boss)
        [TestCase(2, "SporeColossus", 50f, true, 10, 170f, 1f, 1f, 30f)] // Colossal Musc Musc
        [TestCase(2, "ThornLionKing", 50f, true, 10, 170f, 1f, 1f, 30f)] // Lionhead (boss)
        [TestCase(2, "GloomMothQueen", 40f, true, 10, 170f, 1f, 1f, 30f)] // Colossal Sneaky Head
        [TestCase(3, "CinderPeasant", 5f, false, 4, 140f, 1f, 3f, 1f)] // Merman 1
        [TestCase(3, "CharredVillager", 70f, false, 8, 100f, 1f, 3f, 2.5f)] // Merman 2
        [TestCase(3, "EmberElder", 150f, false, 8, 100f, 1f, 3f, 2.5f)] // Merman 3
        [TestCase(3, "AshHound", 15f, false, 6, 100f, 1f, 3f, 2f)] // Lizard Pawn
        [TestCase(3, "SootBrute", 120f, false, 7, 100f, 0f, 1f, 2.5f)] // Lizard Rook
        [TestCase(3, "SmokeWisp", 5f, false, 5, 140f, 1f, 3f, 1f)] // Milk Elemental
        [TestCase(3, "ThickSmokeWisp", 10f, false, 5, 140f, 1f, 3f, 2f)] // Milk Elemental 2
        [TestCase(3, "BurntBones", 15f, false, 10, 100f, 1f, 3f, 2f)] // Skeleton
        [TestCase(3, "RooftopStalker", 150f, false, 8, 200f, 1f, 3f, 2.5f)] // Skeleton Ninja 1
        [TestCase(3, "RooftopReaver", 250f, false, 9, 200f, 1f, 3f, 3f)] // Skeleton Ninja 2
        [TestCase(3, "OxDemon", 250f, false, 10, 130f, 0.5f, 3f, 2.75f)] // Minotaur
        [TestCase(3, "ChargingOxDemon", 250f, false, 10, 280f, 0.5f, 3f, 2.75f)] // Mignotaur
        [TestCase(3, "WellSerpents", 25f, false, 10, 0f, 0f, 0f, 1.5f)] // Twin Snakes
        [TestCase(3, "BellImps", 150f, false, 10, 0f, 0f, 0f, 1.75f)] // Twin Demons
        [TestCase(3, "ShrineIdol", 180f, false, 10, 0f, 0f, 0f, 2f)] // Lost Twin
        [TestCase(3, "ClayGolem", 300f, false, 11, 100f, 0f, 1f, 3f)] // Melone
        [TestCase(3, "DemonAxeman", 300f, false, 15, 140f, 0f, 2f, 2.5f)] // Archon Ascia
        [TestCase(3, "DemonSpearman", 300f, false, 15, 160f, 1f, 2f, 3f)] // Archon Lancia
        [TestCase(3, "PitFiend", 500f, false, 15, 250f, 0f, 2f, 3f)] // Tritont
        [TestCase(3, "HearthColossus", 700f, false, 20, 140f, 0f, 2f, 3f)] // Big Golem 1
        [TestCase(3, "FloatingEmber", 180f, false, 8, 150f, 1f, 3f, 2.5f)] // Jellyfish
        [TestCase(3, "AshWing", 350f, false, 10, 160f, 0f, 2f, 2.5f)] // Skelewing
        [TestCase(3, "RuinedKnight", 700f, false, 20, 140f, 0f, 2f, 3f)] // Sword Guardian
        [TestCase(3, "CinderCockatrice", 700f, false, 15, 140f, 0f, 2f, 3f)] // Gallotrice
        [TestCase(3, "EmberChieftain", 50f, true, 11, 140f, 1f, 3f, 40f)] // Merman (boss)
        [TestCase(3, "OxDemonChampion", 50f, false, 11, 180f, 1f, 3f, 30f)] // Minotaur Boss (large)
        [TestCase(3, "MassiveHearthColossus", 80f, true, 20, 120f, 0f, 1f, 25f)] // Big Golem 1 (LV29)
        [TestCase(3, "MassiveKilnColossus", 80f, true, 20, 120f, 0f, 1f, 25f)] // Big Golem 2 (LV29)
        [TestCase(4, "GateSkull", 6f, false, 5, 140f, 1f, 3f, 1f)] // Skullino
        [TestCase(4, "RedGateSkull", 10f, false, 5, 140f, 1f, 3f, 1f)] // Skulorosso
        [TestCase(4, "RampartBat", 5f, false, 5, 140f, 1f, 3f, 1f)] // Pipeestrello 3
        [TestCase(4, "ThriceRisen", 15f, false, 5, 100f, 1f, 3f, 1f)] // Scarleton
        [TestCase(4, "CastleBones", 15f, false, 10, 100f, 1f, 3f, 2f)] // Skeleton
        [TestCase(4, "StoneSkull", 20f, false, 10, 100f, 1f, 3f, 2f)] // Skullone (easy)
        [TestCase(4, "HeadlessWarden", 35f, false, 8, 100f, 0.3f, 3f, 1f)] // Testa di Mano 1
        [TestCase(4, "GargoyleImp", 70f, false, 8, 100f, 0.3f, 3f, 2f)] // Ghiavolo
        [TestCase(4, "BloodBat", 11f, false, 5, 140f, 1f, 3f, 1f)] // Bloodbath
        [TestCase(4, "HallWraith", 10f, false, 5, 200f, 0f, 3f, 1.5f)] // Ghost 2
        [TestCase(4, "CryptImp", 13f, false, 4, 160f, 1f, 3f, 1f)] // Impefinger
        [TestCase(4, "HarpyGargoyle", 25f, false, 1, 240f, 1f, 3f, 2f)] // Harzia
        [TestCase(4, "GlidingGargoyle", 25f, false, 1, 240f, 1f, 3f, 2f)] // Harzia V
        [TestCase(4, "BoneDrake", 4f, true, 3, 200f, 0.6f, 3f, 2f)] // Dragon Shrimp 1
        [TestCase(4, "FlameDrake", 20f, true, 8, 200f, 0.6f, 3f, 2f)] // Dragon Shrimp 2
        [TestCase(4, "GateSorcerer", 220f, false, 14, 130f, 0f, 0f, 3f)] // Undead Mage
        [TestCase(4, "WingedBones", 220f, false, 14, 130f, 0.1f, 3f, 3f)] // Skelewing (ino)
        [TestCase(4, "GatekeeperBlade", 500f, false, 20, 200f, 1.5f, 3f, 3f)] // Archon Spada
        [TestCase(4, "GatekeeperShield", 500f, false, 20, 200f, 1.5f, 3f, 3f)] // Archon Disco
        [TestCase(4, "Basilisk", 700f, false, 15, 140f, 0f, 2f, 3f)] // Gallotrice
        [TestCase(4, "CastleChimera", 700f, false, 15, 140f, 0f, 2f, 3f)] // Manticore
        [TestCase(4, "FleshGolem", 900f, false, 20, 200f, 0f, 2f, 3f)] // Meat Golem 1
        [TestCase(4, "BloatedFleshGolem", 900f, false, 20, 200f, 0f, 2f, 3f)] // Meat Golem 2
        [TestCase(4, "GreatBoneDrake", 50f, true, 10, 170f, 1f, 1f, 30f)] // Dragon Shrimp 1 (boss)
        [TestCase(4, "GreatFlameDrake", 60f, true, 10, 170f, 1f, 1f, 30f)] // Dragon Shrimp 2 (boss)
        [TestCase(5, "BogLeech", 5f, false, 4, 140f, 1f, 3f, 1f)] // Cauld
        [TestCase(5, "RotHead", 15f, false, 6, 100f, 1f, 3f, 2.2f)] // Redusa Head
        [TestCase(5, "CrimsonSpore", 5f, false, 5, 140f, 1f, 3f, 1f)] // Blood Moss
        [TestCase(5, "CrimsonSporeCluster", 10f, false, 5, 140f, 1f, 3f, 2f)] // Swarm Blood Moss
        [TestCase(5, "BogGhoul", 120f, false, 7, 100f, 0f, 1f, 4f)] // Rotting Ghoul
        [TestCase(5, "MarshfireSkull", 60f, false, 8, 100f, 1f, 3f, 2.5f)] // Burning Skull
        [TestCase(5, "MireWorm", 130f, false, 8, 200f, 1f, 3f, 4f)] // Missing Church
        [TestCase(5, "GreatMireWorm", 200f, false, 9, 200f, 1f, 3f, 4f)] // Missing Church 2
        [TestCase(5, "SwampAdder", 15f, false, 10, 100f, 1f, 3f, 2f)] // Snek
        [TestCase(5, "FleshClot", 250f, false, 10, 130f, 0.5f, 3f, 2.75f)] // Meatball
        [TestCase(5, "WhisperingHead", 25f, false, 10, 0f, 0f, 0f, 1.5f)] // Lost Head
        [TestCase(5, "WailingHead", 150f, false, 10, 0f, 0f, 0f, 1.75f)] // Lost Head 2
        [TestCase(5, "TrailingRot", 150f, false, 8, 100f, 1f, 3f, 3f)] // Followa
        [TestCase(5, "ChillBogLeech", 300f, false, 15, 140f, 0f, 2f, 2.5f)] // Cold Cauld
        [TestCase(5, "MireCrone", 300f, false, 11, 100f, 0f, 1f, 3.5f)] // Edna
        [TestCase(5, "PitLurker", 300f, false, 15, 160f, 1f, 2f, 3f)] // Well Dweller
        [TestCase(5, "BogKelpie", 250f, false, 10, 280f, 0.5f, 3f, 2.75f)] // Wet Njuggles
        [TestCase(5, "RotwoodPenitent", 500f, false, 15, 150f, 0f, 2f, 3f)] // Crocifiggitone
        [TestCase(5, "SilentDrowned", 180f, false, 8, 150f, 1f, 3f, 2.5f)] // Still Notadam
        [TestCase(5, "SwampGiantHorn", 700f, false, 20, 140f, 0f, 2f, 3f)] // Conand
        [TestCase(5, "SwampGiantEye", 700f, false, 20, 140f, 0f, 2f, 3f)] // Balor
        [TestCase(5, "SwampGiantMaw", 700f, false, 20, 140f, 0f, 2f, 3f)] // Indech
        [TestCase(5, "HangedHand", 700f, false, 15, 140f, 0f, 2f, 3f)] // Glove of Glory
        [TestCase(5, "BogMother", 700f, false, 20, 140f, 0f, 2f, 3f)] // Maronna Meea
        [TestCase(5, "GreatMarshfireSkull", 100f, true, 20, 180f, 0f, 1f, 30f)] // Flaming Skull Boss
        [TestCase(5, "ColossalRotHead", 100f, true, 20, 180f, 0f, 1f, 30f)] // Colossal Redusa Head
        [TestCase(5, "BurstingFleshClot", 80f, true, 20, 120f, 0f, 1f, 0f)] // Spicy Meatball
        [TestCase(5, "GreatTrailingRot", 80f, true, 20, 120f, 0f, 1f, 25f)] // Giant Followa
        [TestCase(6, "FrostSprite", 8f, false, 5, 140f, 1f, 3f, 1f)] // Spiritello
        [TestCase(6, "BlackFrostSprite", 13f, false, 5, 200f, 0f, 3f, 1.5f)] // Dark Spiritello
        [TestCase(6, "IceBat", 1f, false, 5, 140f, 1f, 3f, 1f)] // Pipeestrello 1
        [TestCase(6, "RimeBat", 5f, false, 5, 140f, 1f, 3f, 1f)] // Pipeestrello 2
        [TestCase(6, "FrostEyeBat", 5f, false, 5, 140f, 1f, 3f, 1f)] // Pipeestrello 3
        [TestCase(6, "FrozenCorpse", 10f, false, 10, 100f, 0.8f, 3f, 1f)] // Zombie
        [TestCase(6, "SnowSerpent", 18f, false, 10, 100f, 1f, 3f, 2f)] // Tsuchinoko
        [TestCase(6, "BlizzardWeasel", 150f, false, 10, 140f, 0.3f, 3f, 2.5f)] // Kamaitachi
        [TestCase(6, "StormHound", 100f, false, 10, 140f, 0.3f, 3f, 2.5f)] // Raiju
        [TestCase(6, "PeakCrone", 270f, false, 10, 140f, 0.1f, 3f, 2.5f)] // Yamamba
        [TestCase(6, "ElderPeakCrone", 180f, false, 14, 130f, 0.8f, 3f, 2f)] // Elite Yamamba
        [TestCase(6, "LoomingDrift", 150f, false, 10, 140f, 0.3f, 3f, 2.5f)] // Mikoshi-nyudo
        [TestCase(6, "SummitCrowman", 500f, false, 20, 140f, 0f, 3f, 3f)] // Tengu
        [TestCase(6, "ThunderOgre", 500f, false, 20, 70f, 0f, 3f, 2.5f)] // Thunderous Oni
        [TestCase(6, "GaleOgre", 500f, false, 20, 70f, 0f, 3f, 2.5f)] // Windy Oni
        [TestCase(6, "GlacierSpider", 800f, false, 20, 100f, 0f, 3f, 3f)] // Tsuchigumo
        [TestCase(6, "GreatStormHound", 50f, true, 10, 170f, 1f, 1f, 30f)] // Raiju Boss
        [TestCase(6, "GreatBlizzardWeasel", 50f, true, 10, 170f, 1f, 1f, 30f)] // Kamaitachi Boss
        [TestCase(6, "GreatGlacierSpider", 100f, true, 20, 180f, 1f, 1f, 30f)] // Tsuchigumo Boss
        [TestCase(6, "GreatThunderOgre", 70f, true, 20, 180f, 1f, 1f, 30f)] // Thunderous Oni Boss
        [TestCase(6, "GreatGaleOgre", 70f, true, 20, 180f, 1f, 1f, 30f)] // Windy Oni Boss
        [TestCase(7, "TidalImp", 8f, false, 5, 140f, 1f, 3f, 1f)] // Chompo
        [TestCase(7, "FoamMote", 1f, false, 2, 160f, 1f, 3f, 1f)] // Brownie
        [TestCase(7, "MournfulShade", 15f, false, 3, 140f, 1f, 3f, 2f)] // Ceffoose
        [TestCase(7, "ShoreWraith", 10f, false, 5, 140f, 1f, 3f, 1f)] // Hill Trow
        [TestCase(7, "DeepWraith", 70f, false, 8, 100f, 0.3f, 3f, 2f)] // Sea Trow
        [TestCase(7, "SoulMinnow", 10f, false, 5, 200f, 0f, 3f, 1.5f)] // Wiseparke
        [TestCase(7, "GreatSoulMinnow", 20f, false, 5, 200f, 0f, 3f, 1.5f)] // Big Wiseparke
        [TestCase(7, "PaleSoulMinnow", 30f, false, 5, 200f, 0f, 3f, 1.5f)] // Pink Wiseparke
        [TestCase(7, "SealskinSpirit", 4f, false, 3, 200f, 1f, 3f, 2f)] // Nutmeg
        [TestCase(7, "TideHowler", 150f, false, 8, 100f, 0.2f, 3f, 2f)] // Vulvio
        [TestCase(7, "WeepingLamb", 200f, false, 1, 100f, 0f, 1f, 2f)] // Lammuga
        [TestCase(7, "KelpSteed", 70f, false, 5, 160f, 0.3f, 3f, 2f)] // Njuggles
        [TestCase(7, "GrinningMourner", 220f, false, 14, 130f, 0.8f, 3f, 3f)] // Sam the Sundown Clown
        [TestCase(7, "HollowSailor", 13f, false, 4, 160f, 1f, 3f, 4f)] // Notadam
        [TestCase(7, "DrownedPenitent", 220f, false, 14, 130f, 0.4f, 3f, 3f)] // Crocifriggitore
        [TestCase(7, "SunkenBell", 130f, false, 8, 200f, 1f, 3f, 4f)] // Missing Church
        [TestCase(7, "GreatSunkenBell", 200f, false, 9, 200f, 1f, 3f, 4f)] // Missing Church 2
        [TestCase(7, "BloodTideImp", 4f, true, 3, 200f, 1f, 3f, 2f)] // Red Chompo
        [TestCase(7, "CrimsonDrownedKnight", 500f, false, 20, 100f, 1f, 3f, 3f)] // Red Knight
        [TestCase(7, "KelpKnight", 500f, false, 20, 180f, 0.5f, 3f, 3f)] // Green Knight
        [TestCase(7, "GrievingApparition", 5f, true, 5, 160f, 0f, 1f, 1f)] // Ghostly Apparition
        [TestCase(7, "WailingApparition", 5f, true, 5, 200f, 0f, 1f, 1f)] // Ghostly Apparition 2
        [TestCase(7, "BrineCrone", 700f, false, 15, 140f, 0f, 2f, 3f)] // Average Hag
        [TestCase(7, "GreatCrimsonDrownedKnight", 80f, true, 20, 120f, 0f, 1f, 25f)] // Big Red Knight
        [TestCase(7, "GreatKelpKnight", 80f, true, 20, 120f, 0f, 1f, 25f)] // Big Green Knight
        [TestCase(7, "MotherOfTides", 900f, false, 15, 140f, 0.1f, 2f, 0f)] // Maronna
        [TestCase(7, "DrownedHand", 900f, false, 15, 190f, 0.1f, 2f, 0f)] // Hand of Glory
        [TestCase(8, "WingedThrall", 5f, false, 4, 140f, 1f, 3f, 1f)] // Fake Fallen Cherub 1
        [TestCase(8, "WingedVeteran", 15f, false, 6, 100f, 1f, 3f, 2f)] // Fake Fallen Cherub 2
        [TestCase(8, "WingedWarden", 150f, false, 7, 100f, 0f, 1f, 3f)] // Fake Fallen Cherubbello
        [TestCase(8, "AcolyteYellow", 5f, false, 5, 140f, 1f, 3f, 1f)] // Reaper Trainee (yellow)
        [TestCase(8, "AcolyteBlue", 5f, false, 5, 140f, 1f, 3f, 1f)] // Reaper Trainee (blue)
        [TestCase(8, "AcolyteGray", 15f, false, 5, 140f, 1f, 3f, 1.5f)] // Reaper Trainee (gray)
        [TestCase(8, "AcolyteGreen", 40f, false, 5, 140f, 1f, 3f, 1f)] // Reaper Trainee (green)
        [TestCase(8, "AcolytePurple", 180f, false, 5, 140f, 1f, 3f, 2.5f)] // Reaper Trainee (purple)
        [TestCase(8, "FourArmed", 70f, false, 8, 100f, 1f, 3f, 2.5f)] // Tetrabrachia 1
        [TestCase(8, "FourArmedFury", 150f, false, 8, 200f, 1f, 3f, 3f)] // Tetrabrachia 2
        [TestCase(8, "Temptress", 250f, false, 10, 130f, 0.5f, 3f, 2.75f)] // Succubus
        [TestCase(8, "Swordsman", 150f, false, 10, 120f, 0f, 0f, 1.75f)] // Sword Knight
        [TestCase(8, "FlameKnight", 150f, false, 8, 120f, 1f, 3f, 3f)] // Archon Fiamma
        [TestCase(8, "CopperKnight", 300f, false, 11, 100f, 0f, 1f, 3f)] // Archon Rame
        [TestCase(8, "GoldKnight", 300f, false, 15, 140f, 0f, 2f, 2.5f)] // Archon Oro
        [TestCase(8, "Priest", 250f, false, 10, 140f, 0.5f, 3f, 2.75f)] // Demon Priest
        [TestCase(8, "Beast", 300f, false, 15, 160f, 1f, 2f, 3f)] // Demon Beast
        [TestCase(8, "GreatBeast", 500f, false, 15, 150f, 0f, 2f, 3f)] // Demon Beast 2
        [TestCase(8, "Archfiend", 700f, false, 20, 140f, 0f, 2f, 3f)] // Archdemon
        [TestCase(8, "Executioner", 10f, true, 11, 140f, 1f, 3f, 3f)] // Stage Killer 1
        [TestCase(8, "GreatExecutioner", 11f, true, 11, 140f, 1f, 3f, 3f)] // Stage Killer 2
        [TestCase(8, "Seraph", 300f, false, 11, 100f, 0f, 1f, 3f)] // Fallen Cherub
        [TestCase(8, "SwiftSeraph", 250f, false, 10, 280f, 0.5f, 3f, 2.75f)] // Fallen Cherubbello
        [TestCase(8, "Throne", 300f, false, 15, 160f, 1f, 2f, 3f)] // Fallen Throne
        [TestCase(8, "Triskele", 700f, false, 15, 140f, 0f, 2f, 3f)] // Trinacria
        [TestCase(8, "ColossalWarden", 50f, true, 11, 140f, 1f, 3f, 30f)] // Fake Fallen Cherubbello (boss)
        [TestCase(8, "ColossalTemptress", 60f, true, 11, 140f, 1f, 3f, 30f)] // Succubus (boss)
        [TestCase(8, "ColossalArchfiend", 80f, true, 11, 140f, 1f, 3f, 30f)] // Archdemon (boss)
        [TestCase(8, "EliteAcolyte", 80f, true, 20, 120f, 0f, 1f, 25f)] // Reaper Trainee (TRAINEE_ANY)
        public void EachType_HasItsWikiStatbox(int level, string id, float health, bool timesLevel, int damage, float vsSpeed,
            float knockback, float maxKnockback, float vsXp)
        {
            int[] levels = level == 8 ? new[] { 8, 9, 10 } : new[] { level }; // Cappella Magna serves Levels 8-10
            foreach (int l in levels)
            {
                var table = StageEnemyTables.For(l);
                var type = table.Find(id);
                Assert.AreEqual(health, type.Health, 1e-4, $"L{l} {id} health");
                Assert.AreEqual(timesLevel, type.HealthTimesLevel, $"L{l} {id} HP x Level");
                Assert.AreEqual(damage, type.Damage, $"L{l} {id} power");
                Assert.AreEqual(vsSpeed, type.VsSpeed, 1e-4, $"L{l} {id} speed");
                Assert.AreEqual(knockback, type.Knockback, 1e-4, $"L{l} {id} knockback (statbox)");
                Assert.AreEqual(maxKnockback, type.MaxKnockback, 1e-4, $"L{l} {id} knockback cap");
                Assert.AreEqual(vsXp, type.VsXp, 1e-4, $"L{l} {id} XP");
                Assert.AreEqual(vsSpeed * 0.03f, type.MoveSpeed, 1e-5, "VS 100 = 3 units/s.");
                Assert.AreEqual(Mathf.Max(1, Mathf.RoundToInt(vsXp * 10f)), type.ExperienceValue, "XP x10, at least 1.");
            }
        }

        [Test]
        public void EveryLevelTwoToTen_HasATable_WithThirtyMinutesOfKnownTypes()
        {
            for (int level = 2; level <= 10; level++)
            {
                var table = StageEnemyTables.For(level);
                Assert.IsNotNull(table, $"Level {level}");
                Assert.AreEqual(level, table.Level);
                Assert.AreEqual(30, table.ByMinute.Length, $"Level {level}: minutes 0-29.");
                Assert.AreEqual(30, table.Minimum.Length);
                Assert.AreEqual(30, table.Interval.Length);
                foreach (var minute in table.ByMinute)
                {
                    Assert.That(minute, Is.Not.Empty, $"Level {level}: every minute lists a type.");
                    foreach (var id in minute) Assert.DoesNotThrow(() => table.Find(id), $"Level {level}: {id}");
                }
                foreach (var type in table.Types)
                    Assert.That(table.ByMinute.Any(m => m.Contains(type.Id)) || table.BossOnly.Contains(type.Id), $"Level {level}: {type.Id} appears in a minute, or is a boss-only type.");
                foreach (var id in table.BossOnly)
                {
                    Assert.DoesNotThrow(() => table.Find(id), $"Level {level}: {id}");
                    Assert.That(table.ByMinute.Any(m => m.Contains(id)), Is.False, $"Level {level}: {id} is boss-only.");
                }
                Assert.AreEqual(table.Types.Length, table.Types.Select(t => t.Id).Distinct().Count(), "Unique ids.");
                Assert.AreEqual(table.Types.Length, table.Types.Select(t => t.Name).Distinct().Count(), "Unique names.");
            }
            Assert.IsNull(StageEnemyTables.For(1), "Level 1 is MadForestEnemies.");
        }

        [Test]
        public void SpotRows_MatchTheWikiTables()
        {
            CollectionAssert.AreEqual(new[] { "HuntingGloomMoth" }, StageEnemyTables.For(2).ByMinute[15], "Inlaid Library 15:00 Aggressive Big Sneaky Head.");
            CollectionAssert.AreEqual(new[] { "AshColossus", "SporeColossus", "ThornLionKing", "GloomMothQueen" }, StageEnemyTables.For(2).ByMinute[29], "Inlaid Library 29:00, the four colossi.");
            CollectionAssert.AreEqual(new[] { "BriarWitch", "BarkHusk", "ElderBarkHusk" }, StageEnemyTables.For(2).ByMinute[16], "16:00 Apprentice Witch, Testa di Mano 2 and 3.");
            CollectionAssert.AreEqual(new[] { "RooftopReaver", "WellSerpents", "BellImps" }, StageEnemyTables.For(3).ByMinute[12], "Dairy Plant 12:00.");
            CollectionAssert.AreEqual(new[] { "MassiveHearthColossus", "MassiveKilnColossus" }, StageEnemyTables.For(3).ByMinute[29], "Dairy Plant 29:00, Big Golem 1 and 2 (LV29).");
            CollectionAssert.AreEqual(new[] { "GreatBoneDrake", "GreatFlameDrake" }, StageEnemyTables.For(4).ByMinute[29], "Gallo Tower 29:00.");
            CollectionAssert.AreEqual(new[] { "GlidingGargoyle", "BoneDrake" }, StageEnemyTables.For(4).ByMinute[11], "Gallo Tower 11:00 Wandering Harzia, Dragon Shrimp.");
            CollectionAssert.AreEqual(new[] { "BogLeech" }, StageEnemyTables.For(5).ByMinute[0], "Abyss Foscari 0:00 Cauld.");
            CollectionAssert.AreEqual(new[] { "SwampGiantHorn", "SwampGiantEye", "SwampGiantMaw" }, StageEnemyTables.For(5).ByMinute[25], "Abyss Foscari 25:00, the Fomorians.");
            CollectionAssert.AreEqual(new[] { "SummitCrowman" }, StageEnemyTables.For(6).ByMinute[17], "Mt.Moonspell 17:00 Tengu.");
            CollectionAssert.AreEqual(new[] { "IceBat", "RimeBat", "FrostEyeBat" }, StageEnemyTables.For(6).ByMinute[2], "Mt.Moonspell 2:00, three bats.");
            CollectionAssert.AreEqual(new[] { "GrievingApparition", "WailingApparition" }, StageEnemyTables.For(7).ByMinute[25], "Lake Foscari 25:00.");
            for (int level = 8; level <= 10; level++)
            {
                CollectionAssert.AreEqual(new[] { "FlameKnight" }, StageEnemyTables.For(level).ByMinute[13], "Cappella Magna 13:00 Archon Fiamma.");
                CollectionAssert.AreEqual(new[] { "Executioner" }, StageEnemyTables.For(level).ByMinute[21], "21:00 Stage Killer.");
                CollectionAssert.AreEqual(new[] { "EliteAcolyte" }, StageEnemyTables.For(level).ByMinute[29], "29:00 Elite Reaper Trainee.");
            }
        }

        [Test]
        public void SkillsAndResistances_FollowTheStatboxes()
        {
            Assert.AreEqual(EnemyMovement.FloatyFixedDirection, StageEnemyTables.For(2).Find("GloomMoth").Movement, "Big Sneaky Head: Floaty, Fixed Direction.");
            Assert.AreEqual(EnemyMovement.FixedDirection, StageEnemyTables.For(4).Find("GlidingGargoyle").Movement, "Harzia V: Fixed Direction.");
            Assert.AreEqual(EnemyMovement.Homing, StageEnemyTables.For(4).Find("HarpyGargoyle").Movement, "Harzia: no skill.");
            var lantern = StageEnemyTables.For(2).Find("LanternSkull");
            Assert.AreEqual(6, lantern.HealthPerLevelWhole);
            Assert.AreEqual(0.5f, lantern.HealthPerLevelFraction, 1e-5, "Skullone (scaling): 6.5 x Level.");
            Assert.AreEqual(3, StageEnemyTables.For(4).Find("ThriceRisen").Lives, "Scarleton: Has 3 lives.");
            Assert.AreEqual(1f, StageEnemyTables.For(3).Find("MassiveHearthColossus").FreezeResistance, "Big Golem (LV29) res-freeze 1.");
            var spider = StageEnemyTables.For(6).Find("GreatGlacierSpider");
            Assert.That(spider.FreezeResistance == 1f && spider.InstantKillResistance && spider.DebuffResistance, "Tsuchigumo Boss 1/1/1.");
            var ghost = StageEnemyTables.For(7).Find("GrievingApparition");
            Assert.That(ghost.FreezeResistance == 2f && !ghost.InstantKillResistance && ghost.DebuffResistance, "Ghostly Apparition: freeze 2, debuff 1.");
            Assert.That(StageEnemyTables.For(5).Find("BurstingFleshClot").InstantKillResistance, "Spicy Meatball res-kill 1.");
            for (int level = 8; level <= 10; level++)
            {
                Assert.That(StageEnemyTables.For(level).Find("Triskele").DefangResistance, "Trinacria res-defang 1.");
                Assert.That(StageEnemyTables.For(level).Find("Executioner").DefangResistance, Is.False, "Stage Killer res-defang 0.");
                Assert.AreEqual(1f, StageEnemyTables.For(level).Find("EliteAcolyte").FreezeResistance, "Reaper Trainee res-freeze 1.");
            }
        }

        [TestCase(3, "WellSerpents", 2f, 140f, 20)]   // Twin Snakes: "Shoots a bullet every 2 seconds"; Bullet 140 / 20
        [TestCase(3, "BellImps", 1.5f, 250f, 25)]     // Twin Demons: every 1.5 s; Bullet 2 250 / 25
        [TestCase(3, "ShrineIdol", 1f, 300f, 40)]     // Lost Twin: every second; Bullet 300 / 40
        [TestCase(4, "GateSorcerer", 2f, 250f, 25)]   // Undead Mage: every 2 s; Bullet 2 250 / 25
        [TestCase(5, "WhisperingHead", 2f, 140f, 20)] // Lost Head: every 2 s; Bullet 1 140 / 20
        [TestCase(5, "WailingHead", 1.5f, 250f, 25)]  // Lost Head 2: every 1.5 s; Bullet 2 250 / 25
        public void Shooters_FireTheirStatboxBullets(int level, string id, float interval, float bulletSpeed, int bulletDamage)
        {
            var type = StageEnemyTables.For(level).Find(id);
            Assert.That(type.Shoots);
            Assert.AreEqual(interval, type.ShootInterval, 1e-5);
            Assert.AreEqual(bulletSpeed, type.ShootVsSpeed, 1e-5);
            Assert.AreEqual(bulletDamage, type.ShootDamage);
            int shooters = StageEnemyTables.All.Sum(t => t.Types.Count(x => x.Shoots));
            Assert.AreEqual(6, shooters, "Only the six shooting statboxes shoot.");
        }

        [Test]
        public void OwnerSignatures_FloatyOnTwoAndSeven_GhostsUnpushed_EliteModelsOnEight()
        {
            foreach (var table in StageEnemyTables.All)
                foreach (var type in table.Types)
                {
                    var movement = table.MovementOf(type);
                    // Map event units keep their statbox skill (Fixed Direction swarms, the homing Shade Bomb).
                    bool eventUnit = type.KeepsMovement || type.Movement == EnemyMovement.FixedDirection;
                    if ((table.Level == 2 || table.Level == 7) && !eventUnit)
                        Assert.That(movement == EnemyMovement.Floaty || movement == EnemyMovement.FloatyFixedDirection, $"L{table.Level} {type.Id} floats.");
                    else
                        Assert.AreEqual(type.Movement, movement, $"L{table.Level} {type.Id} keeps its statbox movement.");
                    Assert.AreEqual(table.Level == 7 ? 0f : type.Knockback, table.KnockbackOf(type), 1e-5, $"L{table.Level} {type.Id} knockback");
                    if (table.Level == 8) Assert.That(table.EliteModelOf(type), $"L8 {type.Id}: elite-class model.");
                }
        }

        /// <summary>Never VS names: no shown name contains a VS enemy's coined name.</summary>
        [Test]
        public void Names_AreSoulHuntersOwn()
        {
            string[] vsWords =
            {
                "Musc", "Testa", "Lionhead", "Skullone", "Merdusa", "Medusa", "Merman", "Mignotaur", "Minotaur", "Melone", "Archon",
                "Tritont", "Skelewing", "Gallotrice", "Skullino", "Skulorosso", "Pipeestrello", "Scarleton", "Ghiavolo", "Bloodbath",
                "Impefinger", "Harzia", "Cauld", "Redusa", "Snek", "Followa", "Edna", "Njuggles", "Crocifiggitone", "Notadam",
                "Conand", "Balor", "Indech", "Maronna", "Spiritello", "Tsuchinoko", "Kamaitachi", "Raiju", "Yamamba", "Mikoshi",
                "Tengu", "Oni", "Tsuchigumo", "Chompo", "Brownie", "Ceffoose", "Trow", "Wiseparke", "Nutmeg", "Vulvio", "Lammuga",
                "Crocifriggitore", "Cherub", "Tetrabrachia", "Succubus", "Archdemon", "Trinacria", "Stagekiller", "Trainee",
                "Mummy", "Dullahan", "Sneaky", "Elemental", "Dweller", "Meatball", "Moss",
            };
            foreach (var table in StageEnemyTables.All)
                foreach (var type in table.Types)
                {
                    Assert.That(type.Name, Is.Not.Empty);
                    Assert.That(type.Name, Is.Not.EqualTo(type.VsVariant).IgnoreCase, type.Id);
                    foreach (var word in vsWords)
                        Assert.That(Regex.IsMatch(type.Name, $@"\b{word}\b", RegexOptions.IgnoreCase), Is.False, $"L{table.Level} {type.Name} uses the VS word {word}.");
                }
        }

        [Test]
        public void TheBuiltAssets_MatchTheTables()
        {
            foreach (var table in StageEnemyTables.All)
                foreach (var type in table.Types)
                {
                    var data = AssetDatabase.LoadAssetAtPath<EnemyData>(table.DataPath(type.Id));
                    Assert.IsNotNull(data, $"{table.DataPath(type.Id)} (run Soul Hunter/Content/Build Level 2-10 Enemy Types).");
                    Assert.AreEqual(type.Name, data.EnemyName);
                    Assert.AreEqual(type.MaxHealth, data.MaxHealth, type.Id);
                    Assert.AreEqual(type.HealthPerLevelWhole, data.HealthPerPlayerLevel, type.Id);
                    Assert.AreEqual(type.HealthPerLevelFraction, data.HealthPerPlayerLevelFraction, 1e-5, type.Id);
                    Assert.AreEqual(type.Damage, data.DamageToPlayer, type.Id);
                    Assert.AreEqual(type.MoveSpeed, data.MoveSpeed, 1e-5, type.Id);
                    Assert.AreEqual(table.KnockbackOf(type), data.Knockback, 1e-5, type.Id);
                    Assert.AreEqual(type.MaxKnockback, data.MaxKnockback, 1e-5, type.Id);
                    Assert.AreEqual(type.ExperienceValue, data.ExperienceValue, type.Id);
                    Assert.AreEqual(table.MovementOf(type), data.Movement, type.Id);
                    Assert.AreEqual(type.FreezeResistance, data.FreezeResistance, 1e-5, type.Id);
                    Assert.AreEqual(type.InstantKillResistance, data.InstantKillResistance, type.Id);
                    Assert.AreEqual(type.DebuffResistance, data.DebuffResistance, type.Id);
                    Assert.AreEqual(type.DefangResistance, data.DefangResistance, type.Id);
                    Assert.AreEqual(type.Lives, data.Lives, type.Id);
                    Assert.IsFalse(data.DropsChest, type.Id);
                    Assert.AreEqual(type.Tint, data.VisualTint, type.Id);
                    Assert.AreEqual((type.Shoots ? 1 : 0) + (type.SelfDestructs ? 1 : 0), data.Abilities.Count, type.Id);
                    if (type.SelfDestructs) Assert.AreEqual(EnemyAbilityKind.SelfDestruct, data.Abilities[data.Abilities.Count - 1].Kind, type.Id);
                    if (type.Shoots)
                    {
                        Assert.AreEqual(EnemyAbilityKind.Shoot, data.Abilities[0].Kind);
                        Assert.AreEqual(type.ShootInterval, data.Abilities[0].IntervalSeconds, 1e-5);
                        Assert.AreEqual(type.ShootVsSpeed * 0.03f, data.Abilities[0].Speed, 1e-5);
                        Assert.AreEqual(type.ShootDamage, data.Abilities[0].Damage);
                    }
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(table.PrefabPath(type.Id));
                    Assert.IsNotNull(prefab, table.PrefabPath(type.Id));
                    var controller = prefab.GetComponent<EnemyController>();
                    Assert.IsNotNull(controller);
                    Assert.AreSame(data, controller.Data, $"{type.Id} prefab uses its own data.");
                    // The level's own model: a variant of its enemy or elite gameplay prefab.
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(prefab);
                    string expected = $"SH10_L{table.Level:00}_{(table.EliteModelOf(type) ? "Elite" : "Enemy")}Gameplay";
                    Assert.IsNotNull(source, $"{type.Id} is a prefab variant.");
                    Assert.AreEqual(expected, source.name, $"L{table.Level} {type.Id} uses the level's own model.");
                }
        }

        [Test]
        public void EachLevelsWaveAsset_ListsEachMinutesTypes()
        {
            foreach (var table in StageEnemyTables.All)
            {
                var wave = AssetDatabase.LoadAssetAtPath<WaveData>(WaveTables.StageWaveAssets[table.Level - 1]);
                Assert.IsNotNull(wave);
                Assert.AreEqual(30, wave.Minutes.Count);
                for (int m = 0; m < 30; m++)
                {
                    var names = wave.Minutes[m].EnemyTypes.Select(p => p != null ? p.name : "null").ToArray();
                    var expected = table.ByMinute[m].Select(table.PrefabName).ToArray();
                    CollectionAssert.AreEqual(expected, names, $"Level {table.Level} minute {m}.");
                    Assert.AreEqual(table.Minimum[m], wave.Minutes[m].Minimum, $"Level {table.Level} minute {m} minimum.");
                    Assert.AreEqual(table.Interval[m], wave.Minutes[m].Interval, 1e-4, $"Level {table.Level} minute {m} interval.");
                }
            }
        }

        [Test]
        public void HealthPerLevelFraction_IsAddedAndRounded()
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            try
            {
                data.HealthPerPlayerLevel = 6;
                data.HealthPerPlayerLevelFraction = 0.5f;
                Assert.AreEqual(65, data.HealthForPlayerLevel(10), "6.5 x 10.");
                Assert.AreEqual(20, data.HealthForPlayerLevel(3), "6.5 x 3 = 19.5, rounded half to even gives 20.");
                data.HealthPerPlayerLevelFraction = 0f;
                Assert.AreEqual(60, data.HealthForPlayerLevel(10), "Whole values unchanged.");
            }
            finally { Object.DestroyImmediate(data); }
        }
    }
}
