using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>The timed VS stage hazards Soul Hunter uses (owner request 2026-09-27).</summary>
    public enum StageHazardKind
    {
        /// <summary>VS "The Stalker XL": a chaser only instant kills can defeat; follows the player for 1 or 2 minutes.</summary>
        DeathsShadow,
        /// <summary>VS "Shooting Star": ground markers, then a strike 2 s later (30 damage).</summary>
        SoulMeteors,
        /// <summary>VS "Pile Assault" / "Pile Shooter": a still ring of enemies around the player that shoots bullets.</summary>
        PileAssault,
        /// <summary>VS "The Blinder" (Abyss Foscari): a reaper that follows like the Stalker but has finite health.</summary>
        Blinder,
        /// <summary>VS Cappella Magna's reapers (Levels 8-10): The Maddener, The Stalker, The Trickster, The Drowner.</summary>
        Maddener,
        CappellaStalker,
        Trickster,
        Drowner,
    }

    /// <summary>
    /// One VS reaper's statbox and rules, for Death's Shadow (DeathsShadow). The Stalker XL (The_Stalker) and The Blinder
    /// (The_Blinder, raw 2026-10-28) share the behaviour ("This behavior is identical to that of The Stalker") and differ in:
    /// - health and speed (statbox);
    /// - whether ordinary weapons hurt it. The Stalker is "otherwise intangible". The Blinder page names only the instant
    ///   killers, but says that in Carlo Cart "its health scaling is much higher, making the only realistic way of
    ///   defeating it is with insta-kill effects": with Abyss Foscari's 500 x Level, ordinary damage can defeat it;
    /// - Pentagram's chest. The Stalker: "Pentagram and Grenade cause no chest to drop". The Blinder: "Defeating it with
    ///   the aforementioned weapons and items will make it drop a Treasure Chest", and Pentagram is the first of them.
    /// Balance method (the Blinder page is silent on it): defeated by ordinary damage, the Blinder leaves its 30 XP as a
    /// gem and no chest, like the chest-less patrol variant.
    /// </summary>
    public struct ReaperProfile
    {
        public string Name;
        public int HealthPerLevel;
        public float VsSpeed;
        public int ContactDamage;
        public bool Intangible;
        public bool PentagramGivesChest;
        public float VsXp;
        public float FreezeResistance;
        public bool InstantKillResistance;
        public bool DebuffResistance;
        /// <summary>False for the Cappella Magna reapers: "a weaker variant ... cannot drop a Treasure Chest" (The_Stalker).</summary>
        public bool DropsChest;
        /// <summary>Its colour (alpha 0: Death's Shadow's dark shade). Cappella: "a yellow reaper", "a green reaper", "a magenta reaper".</summary>
        public Color Tint;
        /// <summary>The Trickster's rings of Poltergeist Gems (TricksterRings).</summary>
        public bool SummonsGemRings;

        public float MoveSpeed => VsSpeed * EnemyAbilityDefaults.VsSpeedToUnits;
        public int GemValue => Mathf.Max(1, Mathf.RoundToInt(VsXp * 10f)); // Soul Hunter's gem scale (XP x 10)
        public int Health(int playerLevel) =>
            (int)System.Math.Min(int.MaxValue, (long)HealthPerLevel * System.Math.Max(1, playerLevel));
    }

    public struct StageHazardEntry
    {
        public StageHazardKind Kind;
        public float TimeSeconds;
        /// <summary>VS map-event chance in % (100 = always); divided by Luck when rolled (StageHazards).</summary>
        public float ChancePercent;
        /// <summary>How long the hazard stays, in seconds (0 = the kind's default).</summary>
        public float DurationSeconds;

        public StageHazardEntry(StageHazardKind kind, float timeSeconds, float chancePercent = 100f, float durationSeconds = 0f)
        {
            Kind = kind;
            TimeSeconds = timeSeconds;
            ChancePercent = chancePercent;
            DurationSeconds = durationSeconds;
        }
    }

    /// <summary>
    /// What defeated Death's Shadow (vampire.survivors.wiki/w/The_Stalker, "Behavior": "The Stalker is instantly killed
    /// upon taking damage from Pentagram, Gorgeous Moon, Infinite Corridor, Crimson Shroud, Victory Sword, Rosary, Just
    /// Vent, Grenade, or the Dairy Carts"). Soul Hunter has Pentagram, Eclipse Sigil (Gorgeous Moon), Crimson Shroud, Soul
    /// Toll (Rosary) and the Stage 3 rail carts (Dairy Cart); the others have no Soul Hunter counterpart.
    /// </summary>
    public enum ShadowKiller
    {
        /// <summary>Anything else: ordinary weapons, the stage-clear wipe. It cannot hurt Death's Shadow.</summary>
        None,
        Pentagram,
        EclipseSigil,
        CrimsonShroud,
        SoulToll,
        RailCart,
        /// <summary>Sundered Hour (VS Infinite Corridor), added 2026-10-12: its health-halving pulse.</summary>
        SunderedHour,
    }

    /// <summary>
    /// Per-stage hazard schedule and the pure maths behind the hazards (EditMode-testable). VS rules come from
    /// vampire.survivors.wiki (Dairy Plant: Stalker 120 s, Pile Shooter ellipse ring, dairy carts; Gallo Tower: Shooting
    /// Star markers hit after 2 s for 30 damage; Pile Assault shoots bullets at the player). Which Soul Hunter stage gets
    /// which hazard, the times and every tuning number below are Soul Hunter choices and provisional.
    /// </summary>
    public static class StageHazardTable
    {
        // ---- Death's Shadow (VS The Stalker XL) ----
        // Owner decision 2026-10-09: "use the latest VS Stalker". vampire.survivors.wiki/w/The_Stalker (raw, fetched
        // 2026-10-09) has three variants side by side; the XL (BOSS_XLSTALKER) is the one VS uses as a stage map event that
        // drops chests (Dairy Plant, The Bone Zone), which is what Death's Shadow is. Its statbox:
        // "| skills = [[HP x Level]] | health = 655,350 | movespeed = 300 | damage = 50 | knockback = 0 | knockback-max = 0
        //  | xp = 0 | res-freeze = 2 | res-kill = 0 | res-debuff = 1 | res-defang = 0".
        public const int ShadowHealthPerLevel = 655350;     // VS: health 655,350, HP x Level
        public const float ShadowVsMoveSpeed = 300f;        // VS: movespeed 300
        /// <summary>VS 300 in Soul Hunter units (EnemyAbilityDefaults.VsSpeedToUnits: VS 100 = 3 units/s), 9 units/s.</summary>
        public const float ShadowMoveSpeed = ShadowVsMoveSpeed * EnemyAbilityDefaults.VsSpeedToUnits;
        public const int ShadowContactDamage = 50;          // VS: damage 50
        public const float ShadowFreezeResistance = 2f;     // VS: res-freeze 2 (Clock Lancet's freeze chance is 1)
        public const bool ShadowInstantKillResistance = false; // VS: res-kill 0
        public const bool ShadowDebuffResistance = true;    // VS: res-debuff 1
        public const int ShadowExperience = 0;              // VS: xp 0 (it never drops a gem)
        public const float ShadowLifetimeSeconds = 120f;    // VS: "follows the player for 1 or 2 minutes" (the default)
        public const float ShadowScale = 1.5f;              // Soul Hunter's look: the stage boss model, 1.5x and dark
        public const float ShadowSpawnDistance = 18f;      // just off-screen
        /// <summary>
        /// VS Dairy Plant's own rows on Level 3 (Dairy Plant's place), and the owner's placement of 2026-09-27 on Stages 5
        /// and 9 kept as an extra (owner, 2026-10-27: "haan dono jagah rakho").
        /// </summary>
        public static readonly int[] ShadowStages = { 3, 5, 9 };
        /// <summary>
        /// The VS Dairy Plant rows of The Stalker XL, minute / % chance / seconds: "8:00 Event Spawn 1, lasts 2 minutes,
        /// 30% chance" and "12:00 Event Spawn 1, lasts 1 minute, 30% chance", and the 0:00 row (raw Dairy_Plant, fetched
        /// 2026-10-12: "{{map event|The Stalker|enemy=The Stalker|amount=1|duration=120|chance=1}}", "a low chance to spawn
        /// at the very start of the round"). Each chance is divided by Luck (wiki Luck, "Skipping events": "Most map
        /// events feature a chance for it to happen ... chanceWithLuck = eventChance / totalLuck").
        /// </summary>
        public static readonly (float minute, float chancePercent, float seconds)[] ShadowEvents =
            { (0f, 1f, 120f), (8f, 30f, 120f), (12f, 30f, 60f) };

        // The chase rhythm. The wiki: it "will follow the player around but will not actively make contact. Occasionally,
        // it will speed up or slow down, appearing to chase the player" (The_Stalker) and "greatly speeds up at fixed
        // intervals" (Random_Events). It gives no numbers, so these are Soul Hunter's balance choices (owner rule
        // 2026-10-09: choose by balance, record why):
        // - it trails at a gap for 8 s, then closes in for 2 s, and repeats. 8 + 2 s gives 12 lunges in the 120 s event
        //   and 6 in the 60 s one: often enough to be a threat, rare enough that the player can fight in between.
        // - both phases use the statbox speed (9 units/s). Soul Hunter's hero walks 5 units/s x the stage bonus (x1.40
        //   on Stages 5 and 9, 7 units/s), so a hero who keeps moving loses only 2 units of the gap per dash second and
        //   is not caught from the trailing gap in 2 s, while one who stands still is. No faster dash multiplier is
        //   invented; Curse and the run modes still speed it up ("Having a high Curse ... allows it to move very fast").
        // - the gap is its own body radius + 3 units, so the trailing shadow never touches the hero.
        public const float ShadowTrailSeconds = 8f;
        public const float ShadowDashSeconds = 2f;
        public const float ShadowTrailClearance = 3f;

        // ---- Soul Meteors (VS Shooting Star) ----
        public const float MeteorDelaySeconds = 2f;        // VS: impact 2 s after the marker appears
        public const int MeteorPlayerDamage = 30;          // VS: 30 damage
        public const int MeteorEnemyDamage = 30;
        public const float MeteorRadius = 1.6f;
        public const int MeteorVolleyCount = 6;             // balance 2026-09-28: 8 every 2 min killed a level-140 hero in 2.5 min
        public const float MeteorStaggerSeconds = 0.2f;    // markers of a volley appear one after another
        public const float MeteorMinSpread = 2f;
        public const float MeteorMaxSpread = 7f;
        public static readonly int[] MeteorStages = { 6, 10 };
        public const float MeteorFirstMinute = 4f;         // VS Gallo Tower: stars at 0, 4, 15, 22; here every 4 minutes
        public const float MeteorLastMinute = 28f;
        public const float MeteorEveryMinutes = 4f;

        // ---- Omens: Powder Ring (VS Roulette), Thorn Circle (VS Flower Wall), Rising Tide (VS The Drowner) ----
        // Random_Events gives no numbers for these; each is taken from a rule the game already has (balance method).
        /// <summary>A Powder Ring member arms at this distance, with this fuse, blast and damage: the self-destruct
        /// defaults (EnemyAbilityDefaults, VS Sig.ra Rossi "exploding 2 seconds later for 50 damage").</summary>
        public const float MineArmDistance = 2f;
        public const float MineFuseSeconds = 2f;
        public const float MineBlastRadius = 2.5f;
        public const int MineDamage = 50;
        /// <summary>The ring stands on the Pile Assault's ellipse, with its 12 members, for a minute (as VS's other still
        /// rings: Pile Assault and Scorpion Lair "disappear after 1 minute").</summary>
        public const int MineCount = PileCount;
        public const float MineSeconds = 60f;

        /// <summary>VS Flower Wall: "Disappears after ~50 seconds."</summary>
        public const float ThornSeconds = 50f;
        /// <summary>30 members 9 units out stand 1.9 apart, so their 1-unit posts leave no gap for the hero (0.4 wide
        /// each side); the ring ends 3 units out, close enough to press on the hero, never onto it.</summary>
        public const int ThornCount = 30;
        public const float ThornStartRadius = 9f;
        public const float ThornEndRadius = 3f;
        public const float ThornPostRadius = 1f;

        /// <summary>VS The Drowner: "for ~1 minute"; "20 damage per game-tick".</summary>
        public const float TideSeconds = 60f;
        public const int TideDamage = 20;
        /// <summary>The game's contact-damage interval (TouchDamage): the only "tick" Soul Hunter has.</summary>
        public const float TideTickSeconds = 0.5f;
        /// <summary>The_Drowner statbox: "health = 800 - 1250" with "HP x Level": the lower value per level.</summary>
        public const int TideCallerHealthPerLevel = 800;
        public static int TideCallerHealth(int playerLevel) => TideCallerHealthPerLevel * Mathf.Max(1, playerLevel);
        /// <summary>The water sheet: wider and deeper than any view (a 21:9 view is about 56 units wide).</summary>
        public const float TideSheetWidth = 240f;
        public const float TideSheetDepth = 160f;
        /// <summary>The Tide Caller stands this far inside the screen's bottom-left corner.</summary>
        public const float TideCallerInset = 3f;

        // ---- Pile Assault ----
        public const int PileCount = 12;
        public const float PileRadiusX = 10f;              // an ellipse, wider than tall like the screen
        public const float PileRadiusZ = 7f;
        public const float PileDurationSeconds = 20f;
        public const float PileFireIntervalSeconds = 2.5f;
        public const float PileBulletSpeed = 4f;           // slow bullets
        public const int PileBulletDamage = 10;
        public const float PileBulletLifetime = 6f;
        public const int PileStage = 8;
        public static readonly float[] PileMinutes = { 9f, 10f, 12f, 20f };

        /// <summary>
        /// A Pile Assault row's 'range' as a scale on the ring. The wiki gives the number only (0.4 on Gallo Tower, 0.7 in
        /// the template example); balance method: 0.7 is the ring as built, so 0.4 is a ring 0.57 as big. No range: 1.
        /// </summary>
        public static float PileRangeScale(float range) => range > 0f ? Mathf.Clamp(range / 0.7f, 0.3f, 1.5f) : 1f;

        /// <summary>
        /// A Shooting Star row's 'range' in units: how far from the player a star may land. The wiki gives "1 (max)" and
        /// "0.5 (max)"; balance method: 1 is the Soul Meteor volley's widest spread (7 units, inside the screen).
        /// </summary>
        public static float StarRange(float range) => MeteorMaxSpread * Mathf.Clamp(range, 0.1f, 1f);

        // ---- Jellyfish Wall (VS map event, PulsingRing) ----
        // The wiki: "an ellipsoid ring around the player ... periodically closes in on and moves away from the player". It
        // gives no size or rhythm; balance method: as wide as the Pile Assault ellipse plus a unit (inside the screen), in
        // to 55% and out again every 8 s (the ring's edge moves at most about 2.4 units/s, under the jellyfish's speed 6).
        public const float JellyRadiusX = 11f;
        public const float JellyRadiusZ = 8f;
        public const float JellyInnerScale = 0.55f;
        public const float JellyPulseSeconds = 8f;

        // ---- Rail carts (VS Dairy Plant carts) ----
        public const int RailCartStage = 3;
        public const int RailCount = 2;

        /// <summary>A missed hazard (the clock was skipped more than this far past it) does not fire.</summary>
        public const float MissedAfterSeconds = 10f;

        /// <summary>Timed hazards of a stage (1-10), sorted by time. Rail carts are not timed (HasRailCarts).</summary>
        public static List<StageHazardEntry> For(int stage)
        {
            var list = new List<StageHazardEntry>();
            if (System.Array.IndexOf(ShadowStages, stage) >= 0)
                foreach (var (minute, chance, seconds) in ShadowEvents)
                    list.Add(new StageHazardEntry(StageHazardKind.DeathsShadow, minute * 60f, chance, seconds));
            if (System.Array.IndexOf(BlinderStages, stage) >= 0)
                foreach (var (minute, chance, seconds) in BlinderEvents)
                    list.Add(new StageHazardEntry(StageHazardKind.Blinder, minute * 60f, chance, seconds));
            if (System.Array.IndexOf(CappellaStages, stage) >= 0)
                foreach (var (kind, seconds) in CappellaReapers)
                    list.Add(new StageHazardEntry(kind, seconds, 100f, kind == StageHazardKind.Drowner ? TideSeconds : ShadowLifetimeSeconds));
            if (System.Array.IndexOf(MeteorStages, stage) >= 0)
                for (float minute = MeteorFirstMinute; minute <= MeteorLastMinute + 0.01f; minute += MeteorEveryMinutes)
                    list.Add(new StageHazardEntry(StageHazardKind.SoulMeteors, minute * 60f));
            if (stage == PileStage)
                foreach (float minute in PileMinutes) list.Add(new StageHazardEntry(StageHazardKind.PileAssault, minute * 60f));
            list.Sort((a, b) => a.TimeSeconds.CompareTo(b.TimeSeconds));
            return list;
        }

        public static bool HasRailCarts(int stage) => stage == RailCartStage;

        /// <summary>Meteor tint: falling ice on the Frozen Peaks, soul fire elsewhere.</summary>
        public static Color MeteorColor(int stage) =>
            stage == 6 ? new Color(0.55f, 0.85f, 1f, 1f) : new Color(1f, 0.35f, 0.25f, 1f);

        /// <summary>
        /// Where a volley's marker lands relative to the player. The first marker is on the player (standing still is
        /// never safe); the others ring the player between MeteorMinSpread and MeteorMaxSpread.
        /// </summary>
        public static Vector3 MeteorOffset(int index, int count, float angleRoll01, float distanceRoll01)
        {
            if (index <= 0 || count <= 1) return Vector3.zero;
            float step = 360f / (count - 1);
            float angle = (index - 1) * step + (Mathf.Clamp01(angleRoll01) - 0.5f) * step * 0.6f;
            float distance = Mathf.Lerp(MeteorMinSpread, MeteorMaxSpread, Mathf.Clamp01(distanceRoll01));
            return Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;
        }

        /// <summary>A pile enemy's spot on the ellipse ring around the player.</summary>
        public static Vector3 PileOffset(int index, int count)
        {
            float angle = Mathf.PI * 2f * index / Mathf.Max(1, count);
            return new Vector3(Mathf.Cos(angle) * PileRadiusX, 0f, Mathf.Sin(angle) * PileRadiusZ);
        }

        /// <summary>
        /// VS Skull Pile Pile (vampire.survivors.wiki/w/Skull_Pile_Pile, raw, fetched 2026-10-13): "It spawns a pair of
        /// columns of 12 Skull Piles on both sides of the player."
        /// </summary>
        public const int PileColumnLength = 12;

        /// <summary>
        /// A column shooter's spot: the first <paramref name="perColumn"/> stand in a column left of the player, the rest in
        /// one to the right. The wiki gives no distances; the columns use the Pile Assault ellipse's own reach (as far out as
        /// its sides, as tall as its top and bottom), which is already known to sit inside the screen.
        /// </summary>
        public static Vector3 PileColumnOffset(int index, int perColumn)
        {
            perColumn = Mathf.Max(1, perColumn);
            bool right = index >= perColumn;
            int slot = right ? index - perColumn : index;
            float along = perColumn > 1 ? Mathf.Lerp(-PileRadiusZ, PileRadiusZ, slot / (float)(perColumn - 1)) : 0f;
            return new Vector3(right ? PileRadiusX : -PileRadiusX, 0f, along);
        }

        /// <summary>
        /// Speed multiplier that gives an enemy the Death's Shadow speed whatever its data speed. <paramref name="curseAndModes"/>
        /// is the player's Curse (as the spawner uses it for speed) x the run modes' enemy speed (Hyper, Inverse). The
        /// stage's own enemy pace is not applied: the XL has its own speed.
        /// </summary>
        public static float ShadowSpeedMultiplier(float enemyBaseSpeed, float curseAndModes = 1f) =>
            ShadowMoveSpeed * Mathf.Max(0.1f, curseAndModes) / Mathf.Max(0.1f, enemyBaseSpeed);

        /// <summary>The Stalker XL statbox (BOSS_XLSTALKER): 655,350 x Level, speed 300, damage 50, xp 0, intangible.</summary>
        public static readonly ReaperProfile StalkerXL = new ReaperProfile
        {
            Name = "Death's Shadow", HealthPerLevel = ShadowHealthPerLevel, VsSpeed = ShadowVsMoveSpeed, ContactDamage = ShadowContactDamage,
            Intangible = true, PentagramGivesChest = false, VsXp = ShadowExperience, FreezeResistance = ShadowFreezeResistance,
            InstantKillResistance = ShadowInstantKillResistance, DebuffResistance = ShadowDebuffResistance, DropsChest = true,
        };

        /// <summary>
        /// The Blinder statbox (BOSS_XLBLINDER, raw 2026-10-28): "|skills = [[HP x Level]] |health = 500 |movespeed = 150
        /// |damage = 25 |knockback = 0 |knockback-max = 0 |xp = 30 |res-freeze = 2 |res-kill = |res-debuff = 1".
        /// </summary>
        public static readonly ReaperProfile Blinder = new ReaperProfile
        {
            Name = "Blinding Shade", HealthPerLevel = 500, VsSpeed = 150f, ContactDamage = 25,
            Intangible = false, PentagramGivesChest = true, VsXp = 30f, FreezeResistance = 2f,
            InstantKillResistance = false, DebuffResistance = true, DropsChest = true,
        };

        /// <summary>
        /// Cappella Magna's reaper statbox, the same for The Maddener, The Stalker and The Trickster (raw 2026-10-28):
        /// health 2550, HP x Level, movespeed 150, damage 50, xp 0, res-freeze 2, res-debuff 1, res-defang 1. They take
        /// ordinary damage (they "take 10 times more damage from breaking Stained Glass windows", and Stained Glass deals 130
        /// base damage to enemies); the Stalker variant "cannot drop a Treasure Chest", and the Maddener and Trickster pages
        /// name no chest, so none drops one. The rows give no duration: balance method, the Stalker default of 2 minutes.
        /// While a Maddener lives, a share of ordinary spawns take its look (EnemyController.SetMaddened, EnemySpawner).
        /// </summary>
        private static ReaperProfile Cappella(string name, Color tint, bool gems) => new ReaperProfile
        {
            Name = name, HealthPerLevel = 2550, VsSpeed = 150f, ContactDamage = 50, Intangible = false, PentagramGivesChest = false,
            VsXp = 0f, FreezeResistance = 2f, InstantKillResistance = false, DebuffResistance = true, DropsChest = false,
            Tint = tint, SummonsGemRings = gems,
        };
        public static readonly ReaperProfile Maddener = Cappella("Madness Shade", new Color(0.85f, 0.72f, 0.12f, 1f), false);
        public static readonly ReaperProfile CappellaStalker = Cappella("Stalking Shade", new Color(0.18f, 0.62f, 0.26f, 1f), false);
        public static readonly ReaperProfile Trickster = Cappella("Trickster Shade", new Color(0.78f, 0.18f, 0.66f, 1f), true);

        /// <summary>The Drowner's Cappella statbox: health 2550, HP x Level, xp 50 (its flood is the Rising Tide).</summary>
        public const int CappellaDrownerHealthPerLevel = 2550;
        public const float CappellaDrownerVsXp = 50f;

        /// <summary>
        /// Cappella Magna's reapers on Levels 8-10: each "guaranteed to spawn ... if the player has not yet defeated The
        /// Ender" (Soul Hunter has none, so every run; owner 2026-10-28 for the Maddener): the Maddener at 0:10 (its page;
        /// the row's delay is 9.6), the Drowner at 5:10, the Stalker at 10:10 and the Trickster at 15:10.
        /// </summary>
        public static readonly int[] CappellaStages = { 8, 9, 10 };
        public static readonly (StageHazardKind kind, float seconds)[] CappellaReapers =
        {
            (StageHazardKind.Maddener, 9.6f), (StageHazardKind.Drowner, 310f),
            (StageHazardKind.CappellaStalker, 610f), (StageHazardKind.Trickster, 910f),
        };

        // ---- The Trickster's Poltergeist Gems (The_Trickster, Poltergeist, raw 2026-10-28) ----
        // "periodically summons ellipsoid rings of 48 Poltergeist Gems around the player that self-destruct when approached.
        // The gems lasts for 30 seconds"; "two rings upon spawning and one every 30 seconds thereafter"; "randomly ... either
        // a large or a small ring ... The gems in the larger ring remain stationary, however, the gems in the smaller ring
        // will home at the player". Balance method (no sizes on the wiki): the large ring is the Jellyfish Wall's, the small
        // one half of it.
        public const int TricksterGems = 48;
        public const float TricksterGemSeconds = 30f;
        public const float TricksterRingEverySeconds = 30f;
        public const float TricksterSmallRingScale = 0.5f;
        /// <summary>"When The Trickster dies, all remaining Poltergeist Gems explode": the gem's own blast (Poltergeist).</summary>
        public const int GemBlastDamage = 50;
        public const float GemBlastRadius = 2.5f;

        /// <summary>VS Abyss Foscari's Blinder rows on Level 5 (its place): 0:00 1% for 2 minutes; 8:00 30% and 12:00 99%, 1 minute.</summary>
        public static readonly int[] BlinderStages = { 5 };
        public static readonly (float minute, float chancePercent, float seconds)[] BlinderEvents =
            { (0f, 1f, 120f), (8f, 30f, 60f), (12f, 99f, 60f) };

        /// <summary>The reaper a scheduled entry sends.</summary>
        public static ReaperProfile ReaperFor(StageHazardKind kind) => kind switch
        {
            StageHazardKind.Blinder => Blinder,
            StageHazardKind.Maddener => Maddener,
            StageHazardKind.CappellaStalker => CappellaStalker,
            StageHazardKind.Trickster => Trickster,
            _ => StalkerXL,
        };

        /// <summary>True for a kind that sends a following reaper (Death's Shadow and its variants).</summary>
        public static bool IsFollowingReaper(StageHazardKind kind) =>
            kind == StageHazardKind.DeathsShadow || kind == StageHazardKind.Blinder || kind == StageHazardKind.Maddener
            || kind == StageHazardKind.CappellaStalker || kind == StageHazardKind.Trickster;

        /// <summary>A reaper's speed factor on a pooled body (as ShadowSpeedMultiplier, with the reaper's own speed).</summary>
        public static float ReaperSpeedMultiplier(ReaperProfile reaper, float enemyBaseSpeed, float curseAndModes = 1f) =>
            reaper.MoveSpeed * Mathf.Max(0.1f, curseAndModes) / Mathf.Max(0.1f, enemyBaseSpeed);

        /// <summary>The chest a reaper drops when <paramref name="killer"/> defeats it (the Blinder: Pentagram too).</summary>
        public static ChestTiers? ReaperChest(ReaperProfile reaper, ShadowKiller killer) =>
            !reaper.DropsChest ? (ChestTiers?)null
            : killer == ShadowKiller.Pentagram && reaper.PentagramGivesChest ? ChestTiers.StalkerHighQuality : ShadowChest(killer);

        /// <summary>VS "HP x Level": 655,350 x the player's level (capped at int.MaxValue).</summary>
        public static int ShadowHealth(int playerLevel) =>
            (int)System.Math.Min(int.MaxValue, (long)ShadowHealthPerLevel * System.Math.Max(1, playerLevel));

        /// <summary>The Luck-adjusted map-event roll (the same rule as the spawner's stage events).</summary>
        public static bool ShadowEventFires(float chancePercent, float luck, float roll01) =>
            EnemySpawner.EventFires(chancePercent, luck, roll01);

        /// <summary>
        /// The chest Death's Shadow drops when <paramref name="killer"/> defeats it, or null (The_Stalker: "Pentagram and
        /// Grenade cause no chest to drop, regardless of it being a good or bad proc"; Gorgeous Moon, Crimson Shroud and
        /// Rosary "cause a high-quality chest to drop"; "Dairy Cart ... cause a lower quality chest to drop").
        /// </summary>
        public static ChestTiers? ShadowChest(ShadowKiller killer)
        {
            switch (killer)
            {
                case ShadowKiller.EclipseSigil:
                case ShadowKiller.CrimsonShroud:
                case ShadowKiller.SoulToll:
                case ShadowKiller.SunderedHour: // The_Stalker (re-read 2026-10-12): "Gorgeous Moon, Infinite Corridor, Crimson Shroud, Victory Sword, and Rosary cause a high-quality chest to drop"
                    return ChestTiers.StalkerHighQuality;
                case ShadowKiller.RailCart:
                    return ChestTiers.StalkerLowQuality;
                default:
                    return null; // Pentagram, or nothing that can kill it
            }
        }

        /// <summary>
        /// Which killer a hit or an instant kill comes from, by its damage source (<see cref="DamageSource"/>). Shadow
        /// Kael's mirrored weapons never count (they are aimed at the player).
        /// </summary>
        /// <summary>
        /// The killers of the Rising Tide's Tide Caller (VS The Drowner, raw, 2026-10-19): "The Drowner is instantly killed
        /// upon taking damage from Gorgeous Moon, Infinite Corridor, Crimson Shroud, Victory Sword, Rosary, or Grenade", and
        /// "Unlike other Reaper types, it is unaffected by Pentagram". Soul Hunter: Eclipse Sigil, Sundered Hour, Crimson
        /// Shroud and Soul Toll (no Victory Sword or Grenade); the rail cart is not on the list.
        /// </summary>
        public static bool KillsTideCaller(ShadowKiller killer) =>
            killer == ShadowKiller.EclipseSigil || killer == ShadowKiller.SunderedHour
            || killer == ShadowKiller.CrimsonShroud || killer == ShadowKiller.SoulToll;

        public static ShadowKiller KillerOf(AutoAttackWeapon weapon, string sourceName)
        {
            if (weapon != null)
            {
                if (weapon.IsHeldByEnemy) return ShadowKiller.None;
                if (weapon is EclipseSigilWeapon) return ShadowKiller.EclipseSigil;
                if (weapon is PentagramWeapon) return ShadowKiller.Pentagram;
                if (weapon is CrimsonShroudWeapon) return ShadowKiller.CrimsonShroud;
                if (weapon is SunderedHourWeapon) return ShadowKiller.SunderedHour;
                return ShadowKiller.None;
            }
            if (sourceName == SoulHunter.Gameplay.Pickups.ScreenWipePickup.DisplayName) return ShadowKiller.SoulToll;
            if (sourceName == RailCart.SourceName) return ShadowKiller.RailCart;
            return ShadowKiller.None;
        }

        /// <summary>
        /// Candidate rails around the stage start: (centre offset, yaw). Yaw 0 runs along world X, 90 along Z. Nearest
        /// first; StageHazards takes the first ones on open ground that do not cross each other.
        /// </summary>
        public static List<(Vector3 offset, float yaw)> RailCandidates()
        {
            return new List<(Vector3, float)>
            {
                (new Vector3(0f, 0f, 10f), 0f), (new Vector3(0f, 0f, -10f), 0f),
                (new Vector3(10f, 0f, 0f), 90f), (new Vector3(-10f, 0f, 0f), 90f),
                (new Vector3(0f, 0f, 14f), 0f), (new Vector3(0f, 0f, -14f), 0f),
                (new Vector3(14f, 0f, 0f), 90f), (new Vector3(-14f, 0f, 0f), 90f),
                (new Vector3(0f, 0f, 18f), 0f), (new Vector3(0f, 0f, -18f), 0f),
            };
        }

        /// <summary>The two ends of a rail of this length, centred on centre and turned by yaw.</summary>
        public static (Vector3 a, Vector3 b) RailEnds(Vector3 centre, float yaw, float length)
        {
            Vector3 along = Quaternion.Euler(0f, yaw, 0f) * Vector3.right * (length * 0.5f);
            return (centre - along, centre + along);
        }

        /// <summary>Distance on the ground plane from p to the segment a-b.</summary>
        public static float SegmentDistanceXZ(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector2 a2 = new Vector2(a.x, a.z), b2 = new Vector2(b.x, b.z), p2 = new Vector2(p.x, p.z);
            Vector2 ab = b2 - a2;
            float len = ab.sqrMagnitude;
            float t = len > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p2 - a2, ab) / len) : 0f;
            return Vector2.Distance(p2, a2 + ab * t);
        }

        /// <summary>True when two ground segments stay at least minGap apart (they neither cross nor touch).</summary>
        public static bool SegmentsApart(Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1, float minGap)
        {
            if (SegmentsCrossXZ(a0, a1, b0, b1)) return false;
            float d = Mathf.Min(Mathf.Min(SegmentDistanceXZ(a0, a1, b0), SegmentDistanceXZ(a0, a1, b1)),
                                Mathf.Min(SegmentDistanceXZ(b0, b1, a0), SegmentDistanceXZ(b0, b1, a1)));
            return d >= minGap;
        }

        private static bool SegmentsCrossXZ(Vector3 p1, Vector3 p2, Vector3 q1, Vector3 q2)
        {
            float Cross(Vector3 o, Vector3 a, Vector3 b) => (a.x - o.x) * (b.z - o.z) - (a.z - o.z) * (b.x - o.x);
            float d1 = Cross(q1, q2, p1), d2 = Cross(q1, q2, p2), d3 = Cross(p1, p2, q1), d4 = Cross(p1, p2, q2);
            return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
        }
    }
}
