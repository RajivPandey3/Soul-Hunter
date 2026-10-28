using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// Each VS stage's own trap events (raw stage pages, "Traps", fetched 2026-10-28). Stepping on a trap "triggers an
    /// unavoidable map event, chosen at random between" the stage's "predetermined options", and the traps "have a 20
    /// second cooldown" (PressurePlate). Every option is one row of the stage's table, equally likely: the event pages give
    /// "1/9 chance to occur after stepping on a trap pizza" (Pile_Assault, Jellyfish_Wall), and Poltergeist's "2/7" and
    /// "4/7" count its rows. A row repeated in the table ("appearing a second time in the list of events") is listed again.
    /// Soul Hunter's levels with a VS trap table: 3 Dairy Plant, 4 Gallo Tower, 5 Abyss Foscari, 7 Lake Foscari. The other
    /// levels' source stages have no traps; their Rune Plates keep Soul Hunter's random formation as an extra.
    /// Balance method (the wiki is silent): the trap Swarms' "range" (0.7, 0.9) is not used (the formation keeps Soul
    /// Hunter's spread); Jellyfish Wall's range 0.9 is not used (Dairy Plant's rows give none); Poltergeist Roulette's
    /// range 2 is held at the Pile Assault ellipse's largest size (StageHazardTable.PileRangeScale).
    /// </summary>
    public static class StageTraps
    {
        private const StageEventType Swarm = StageEventType.Swarm, PulsingRing = StageEventType.PulsingRing,
            PileRing = StageEventType.PileRing, TrapRing = StageEventType.TrapRing, Circle = StageEventType.Circle;

        private static StageEventEntry T(StageEventType type, string typeId, int amount, float duration, float range = 0f,
            bool straight = false) => new StageEventEntry
        {
            Type = type, EnemyTypeId = typeId, Count = Mathf.Max(1, amount), DurationSeconds = duration, Range = range,
            Straight = straight, Repeat = 1, ChancePercent = 100f,
        };

        // Dairy Plant, "Trap events in Dairy Plant" (9).
        private static List<StageEventEntry> DairyPlant() => new List<StageEventEntry>
        {
            T(PileRing, "WellSerpents", 40, 5f, 0.7f), // Pile Assault, Twin Snakes
            T(PileRing, "BellImps", 30, 5f, 0.7f), // Pile Assault, Twin Demons
            T(PileRing, "ShrineIdol", 20, 5f, 0.7f), // Pile Assault, Lost Twin
            T(PulsingRing, "RingEmber", 50, 30f), // Jellyfish Wall, Wall Jellyfish
            T(PulsingRing, "RingEmber", 40, 30f), // Jellyfish Wall
            T(Swarm, "StampedeOxDemon", 40, 10f), // Minotaur Rush, Rushing Mignotaur
            T(Swarm, "StampedeOxDemon", 20, 10f), // Minotaur Rush
            T(Swarm, "SmokeClump", 200, 20f), // Musc Musc Swarm, Big Musc Musc
            T(Swarm, "DriftEmber", 50, 20f), // Jellyfish Swarm, Swarm Jellyfish
        };

        // Gallo Tower, "Trap events in Gallo Tower" (7). Poltergeist Roulette: Poltergeists that "cannot move" (the
        // "(dest)" statbox, the page's "4/7 chance") in a ring that self-destructs when the player comes close; Poltergeist
        // Swarm: Aggressive Poltergeists (the moving statbox, "2/7"); Impefinger Swarm.
        private static List<StageEventEntry> GalloTower() => new List<StageEventEntry>
        {
            T(PileRing, "StillCandle", 50, 30f, 1f), // Poltergeist Roulette
            T(PileRing, "StillCandle", 100, 30f, 2f), // Poltergeist Roulette
            T(PileRing, "StillCandle", 50, 30f, 2f), // Poltergeist Roulette
            T(PileRing, "StillCandle", 50, 30f, 2f), // Poltergeist Roulette (listed twice)
            T(Swarm, "RestlessCandle", 20, 30f), // Poltergeist Swarm
            T(Swarm, "RestlessCandle", 60, 30f), // Poltergeist Swarm
            T(Swarm, "CryptImp", 20, 30f), // Impefinger Swarm
        };

        // Abyss Foscari, "Trap events in Abyss Foscari" (11). Its Swarms: each group "sweep[s] in a straight line across
        // the map", whatever its unit (Swarm Rotting Ghouls, Blood Mosses, Burning Skulls).
        private static List<StageEventEntry> AbyssFoscari() => new List<StageEventEntry>
        {
            T(PileRing, "WhisperingHead", 40, 5f, 0.7f), // Pile Assault, Lost Head
            T(PileRing, "WailingHead", 30, 5f, 0.7f), // Pile Assault, Lost Head 2
            T(PileRing, "WailingHead", 20, 5f, 0.7f), // Pile Assault, Lost Head 2
            T(PulsingRing, "RingMire", 80, 30f), // Jellyfish Wall, Rellyfish
            T(PulsingRing, "RingMire", 40, 30f), // Jellyfish Wall
            T(Swarm, "StampedeGhoul", 40, 10f, straight: true), // Swarm, Swarm Rotting Ghoul
            T(Swarm, "StampedeGhoul", 20, 10f, straight: true), // Swarm
            T(Swarm, "CrimsonSpore", 200, 20f, straight: true), // Swarm, Blood Moss
            T(Swarm, "CrimsonSpore", 50, 20f, straight: true), // Swarm
            T(Swarm, "MarshfireSkull", 50, 20f, straight: true), // Swarm, Burning Skull
            T(Swarm, "MarshfireSkull", 50, 20f, straight: true), // Swarm (listed twice)
        };

        // Lake Foscari, "Trap events in Lake Foscari" (9). Flower Wall: "summons 100 Foscari Flower Wall in an elliptic
        // shape around the player" (the Thorn Circle with the row's amount and duration); Circle: "summons 8 Foscari
        // Mantichana" / "50 Foscari Pot in an elliptic shape around the player".
        private static List<StageEventEntry> LakeFoscari() => new List<StageEventEntry>
        {
            T(TrapRing, "TideThorn", 100, 60f, 0.9f), // Flower Wall
            T(TrapRing, "TideThorn", 100, 60f, 0.9f), // Flower Wall (second time)
            T(TrapRing, "TideThorn", 100, 60f, 0.9f), // Flower Wall (third time)
            T(Circle, "TideMantis", 8, 60f, 0.9f), // Circle, Foscari Mantichana
            T(Circle, "TideMantis", 8, 60f, 0.9f), // Circle
            T(Circle, "TideMantis", 8, 60f, 0.9f), // Circle
            T(Circle, "DrownedUrn", 50, 60f, 0.5f), // Circle, Foscari Pot
            T(Circle, "DrownedUrn", 50, 60f, 0.5f), // Circle
            T(Circle, "DrownedUrn", 50, 60f, 0.5f), // Circle
        };

        /// <summary>The trap options of <paramref name="stage"/>; null when its source stage has no traps.</summary>
        public static List<StageEventEntry> For(int stage)
        {
            switch (stage)
            {
                case 3: return DairyPlant();
                case 4: return GalloTower();
                case 5: return AbyssFoscari();
                case 7: return LakeFoscari();
                default: return null;
            }
        }

        /// <summary>One option, each equally likely (<paramref name="roll"/> in [0, 1)).</summary>
        public static StageEventEntry Pick(List<StageEventEntry> options, float roll)
        {
            if (options == null || options.Count == 0) return null;
            return options[Mathf.Clamp(Mathf.FloorToInt(roll * options.Count), 0, options.Count - 1)];
        }
    }
}
