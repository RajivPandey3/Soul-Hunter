using UnityEngine;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// A boss's chest config (VS "treasure" config). vampire.survivors.wiki/w/Treasure_Chest (raw, fetched 2026-10-09):
    /// "The chances for obtaining a 1, 3, or 5-item chest are individually configured per boss enemy." The fields follow
    /// vampire.survivors.wiki/w/Template:Treasure: level (the fallback), evo, tier3 (5 items), tier2 (3 items), tier1
    /// (1 item) and reward1..reward5 (what each item slot is). "open" in the template only shows the wiki table expanded;
    /// it has no gameplay meaning and is left out.
    /// Soul Hunter's chest elites come at the Mad Forest minutes on every stage (owner, 2026-09-27), so every stage uses
    /// the Mad Forest configs (<see cref="ForMinute"/>); the 25:00 stage boss uses the Giant Blue Venus config.
    /// </summary>
    [System.Serializable]
    public struct ChestTiers : System.IEquatable<ChestTiers>
    {
        /// <summary>Template:Treasure "level": the chest dropped when all three rolls fail.</summary>
        public int Level;
        /// <summary>Template:Treasure "evo": this chest can evolve (or unite) a weapon. Drives the bronze/silver look.</summary>
        public bool CanEvolve;
        /// <summary>Template:Treasure "tier3": base % chance of a 5-item chest (before Luck).</summary>
        public float FivePercent;
        /// <summary>Template:Treasure "tier2": base % chance of a 3-item chest (before Luck).</summary>
        public float ThreePercent;
        /// <summary>Template:Treasure "tier1": base % chance of a 1-item chest (before Luck).</summary>
        public float OnePercent;
        /// <summary>Template:Treasure "reward1=Arcana": the chest only awards an Arcana (Mad Forest 11:00 and 21:00).</summary>
        public bool IsArcana;
        /// <summary>
        /// Template:Treasure "rewardN=weapon", one bit per item slot (bit 0 = slot 1 ... bit 4 = slot 5): that slot is
        /// "Upgrade (weapon)" instead of "Upgrade (any)". Slot 1 is "Evolution" when evo=1 either way.
        /// </summary>
        public int WeaponSlots;

        public ChestTiers(int level, bool canEvolve, float five, float three, float one, int weaponSlots = 0, bool arcana = false)
        {
            Level = level;
            CanEvolve = canEvolve;
            FivePercent = five;
            ThreePercent = three;
            OnePercent = one;
            WeaponSlots = weaponSlots;
            IsArcana = arcana;
        }

        /// <summary>Bit for item slot <paramref name="slot"/> (1 to 5) in <see cref="WeaponSlots"/>.</summary>
        public static int Slot(int slot) => 1 << (slot - 1);

        /// <summary>True when item slot <paramref name="index"/> (0-based) is a weapon upgrade (Template:Treasure reward=weapon).</summary>
        public bool IsWeaponSlot(int index) => index >= 0 && index < 31 && (WeaponSlots & (1 << index)) != 0;

        /// <summary>
        /// The item count when all three rolls fail. Treasure_Chest: "this specific Silver Bat has a level 1 fallback, so it
        /// will drop a level 1 (1-item) chest if all 3 rolls fail." Its v1.6.0 notes name the three chest screens "lvl 1",
        /// "lvl 2" and "lvl 3" (the 3-item screen's reels are "lvl 2", the 5-item one's ribbon "lvl 3"), so level 2 = 3 items
        /// and level 3 = 5 items (wiki research 2026-10-09).
        /// </summary>
        public int FallbackItems => Level >= 3 ? 5 : Level == 2 ? 3 : 1;

        /// <summary>The same chest opened as a regular chest that can or cannot evolve (an Arcana chest with no Arcana left).</summary>
        public ChestTiers AsRegular(bool canEvolve)
        {
            var copy = this;
            copy.IsArcana = false;
            copy.CanEvolve = canEvolve;
            return copy;
        }

        // vampire.survivors.wiki/w/Mad_Forest (raw, fetched 2026-10-09), "Waves" table, each boss's {{treasure}} config.
        /// <summary>1:00 Glowing Bat: {{treasure|level=1|evo=1|tier3=0|tier2=0|tier1=30}}.</summary>
        public static readonly ChestTiers Minute1 = new ChestTiers(1, true, 0f, 0f, 30f);
        /// <summary>3:00 Glowing Bat: {{treasure|level=1|evo=|tier3=0|tier2=5|tier1=40|reward1=weapon}}.</summary>
        public static readonly ChestTiers Minute3 = new ChestTiers(1, false, 0f, 5f, 40f, Slot(1));
        /// <summary>5:00 Mantichana: {{treasure|level=1|evo=|tier3=1|tier2=5|tier1=100|reward1=weapon}}.</summary>
        public static readonly ChestTiers Minute5 = new ChestTiers(1, false, 1f, 5f, 100f, Slot(1));
        /// <summary>7:00 Glowing Bat: {{treasure|level=2|evo=|tier3=3|tier2=10|tier1=50|reward1=weapon}} (level 2: see FallbackItems).</summary>
        public static readonly ChestTiers Minute7 = new ChestTiers(2, false, 3f, 10f, 50f, Slot(1));
        /// <summary>9:00 Silver Bat: {{treasure|level=1|evo=|tier3=3|tier2=10|tier1=50|reward1=weapon|reward4=weapon|reward5=weapon}}.</summary>
        public static readonly ChestTiers Minute9 = new ChestTiers(1, false, 3f, 10f, 50f, Slot(1) | Slot(4) | Slot(5));
        /// <summary>10:00 Giant Mantichana: {{treasure|level=1|evo=1|tier3=3|tier2=10|tier1=100|reward4=weapon|reward5=weapon}}.</summary>
        public static readonly ChestTiers Minute10 = new ChestTiers(1, true, 3f, 10f, 100f, Slot(4) | Slot(5));
        /// <summary>11:00 and 21:00 Arcana holders: {{treasure|level=1|evo=|tier3=0|tier2=0|tier1=100|reward1=Arcana}}.</summary>
        public static readonly ChestTiers ArcanaMinutes = new ChestTiers(1, false, 0f, 0f, 100f, 0, arcana: true);
        /// <summary>12:00 and 16:00 Glowing Bat: {{treasure|level=1|evo=1|tier3=1|tier2=5|tier1=50}}.</summary>
        public static readonly ChestTiers Minute12And16 = new ChestTiers(1, true, 1f, 5f, 50f);
        /// <summary>14, 15, 18, 20, 22, 23, 25 (Giant Blue Venus), 27 and 29 minutes: {{treasure|level=1|evo=1|tier3=3|tier2=10|tier1=100}}.</summary>
        public static readonly ChestTiers Late = new ChestTiers(1, true, 3f, 10f, 100f);
        /// <summary>The 25:00 stage boss (Mad Forest's Giant Blue Venus config). Also a pooled chest's default.</summary>
        public static ChestTiers StageBoss => Late;

        // vampire.survivors.wiki/w/The_Stalker (raw, fetched 2026-10-09), "Behavior": what an instantly killed Stalker
        // drops (Soul Hunter's Death's Shadow). Neither config gives "level"; the only fallback the wiki defines is level 1.
        // Both also say reward4=evo|reward5=evo ("Both chests can exceptionally evolve up to 3 weapons if higher tier"),
        // which ChestTiers cannot express (Soul Hunter evolves at most one weapon per chest): not built.
        /// <summary>
        /// Stalker killed by "Gorgeous Moon, Infinite Corridor, Crimson Shroud, Victory Sword, and Rosary" (Soul Hunter:
        /// Eclipse Sigil, Crimson Shroud, Soul Toll): {{treasure|evo=1|tier3=6|tier2=66|tier1=100|reward4=evo|reward5=evo}}.
        /// </summary>
        public static readonly ChestTiers StalkerHighQuality = new ChestTiers(1, true, 6f, 66f, 100f);
        /// <summary>
        /// Stalker killed by "Dairy Cart and Stained Glass windows" (Soul Hunter: the rail cart):
        /// {{treasure|evo=1|tier3=3|tier2=10|tier1=100|reward4=evo|reward5=evo}}.
        /// </summary>
        public static readonly ChestTiers StalkerLowQuality = new ChestTiers(1, true, 3f, 10f, 100f);

        /// <summary>
        /// The Mad Forest config of the boss scheduled at <paramref name="minute"/> (1, 3, 5, 7, 9, 10, 11, 12, 14, 15, 16,
        /// 18, 20, 21, 22, 23, 25, 27, 29). False for any other minute.
        /// </summary>
        public static bool TryMadForest(float minute, out ChestTiers tiers)
        {
            int m = Mathf.RoundToInt(minute);
            tiers = default;
            if (Mathf.Abs(minute - m) > 0.01f) return false;
            switch (m)
            {
                case 1: tiers = Minute1; return true;
                case 3: tiers = Minute3; return true;
                case 5: tiers = Minute5; return true;
                case 7: tiers = Minute7; return true;
                case 9: tiers = Minute9; return true;
                case 10: tiers = Minute10; return true;
                case 11: case 21: tiers = ArcanaMinutes; return true;
                case 12: case 16: tiers = Minute12And16; return true;
                case 14: case 15: case 18: case 20: case 22: case 23: case 25: case 27: case 29: tiers = Late; return true;
                default: return false;
            }
        }

        /// <summary>The scheduled boss's config at <paramref name="minute"/>, or <see cref="Unscheduled"/> off the table.</summary>
        public static ChestTiers ForMinute(float minute) =>
            TryMadForest(minute, out var tiers) ? tiers : Unscheduled(minute * 60f);

        /// <summary>
        /// A chest whose carrier has no scheduled config (a debug spawn, a test, a chest-dropping prefab not spawned by the
        /// boss schedule). The wiki has no config for it, so Soul Hunter keeps its earlier rule: 3/10/100 (the most common
        /// Mad Forest config) and the evolution flag from the stage clock (<see cref="ChestLogicController.CanEvolveAt"/>).
        /// </summary>
        public static ChestTiers Unscheduled(float stageSeconds) =>
            new ChestTiers(1, ChestLogicController.CanEvolveAt(stageSeconds), 3f, 10f, 100f);

        public bool Equals(ChestTiers other) =>
            Level == other.Level && CanEvolve == other.CanEvolve && IsArcana == other.IsArcana && WeaponSlots == other.WeaponSlots &&
            Mathf.Approximately(FivePercent, other.FivePercent) && Mathf.Approximately(ThreePercent, other.ThreePercent) &&
            Mathf.Approximately(OnePercent, other.OnePercent);

        public override bool Equals(object obj) => obj is ChestTiers other && Equals(other);

        public override int GetHashCode() =>
            (Level, CanEvolve, IsArcana, WeaponSlots, FivePercent, ThreePercent, OnePercent).GetHashCode();

        public override string ToString() =>
            $"level={Level} evo={(CanEvolve ? 1 : 0)} tier3={FivePercent} tier2={ThreePercent} tier1={OnePercent} weaponSlots={WeaponSlots}{(IsArcana ? " Arcana" : "")}";
    }
}
