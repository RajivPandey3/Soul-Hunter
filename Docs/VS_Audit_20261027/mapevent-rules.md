# VS map events: rules, defaults, and the Soul Hunter gaps

Sources are raw wiki pages fetched 2026-10-27 into `vs-audit/wiki/ev/`, plus the stage pages already in `vs-audit/wiki/`. Parsed data:
- `vs-audit/mapevents2.json` comes from a re-parse with nested braces (`scripts/me2.py`). The older `mapevents.json` lost `enemy` and positional names whenever a nested template such as `{{!}}` appeared.
- `vs-audit/ev-statboxes.json` comes from `scripts/statbox.py`.
- `vs-audit/mapevent-variants.json` comes from `scripts/variants.py`.

SH code read: `AI/Enemies/EnemySpawner.cs` (RunStageEvent, TriggerStageEvent, SpawnFliers, BuildFormation, EventRadius, BeginWaveTakeover), `AI/Enemies/EnemyFlyState.cs`, `Data/Config/WaveData.cs` (StageEventEntry), `Data/Config/WaveTables.cs` (Events), and `World/Hazards/*`.

## 1. Template:Map_event: what each parameter means, and its default

Template source (raw):

> `0:{{#time: s|@{{#expr: {{{delay|0}}} round 0}}}} / {{{chance|100}}}%{{#if: {{{repeat|}}}| / x{{#ifeq: {{lc:{{{repeat|}}}}}|infinite|Inf|{{#expr: {{{repeat|}}} + 1}}}}}}`

Template:Map_event/doc (TemplateData), with what the template shows when a parameter is left out:

| Param | /doc description | Default when omitted |
|---|---|---|
| name / 1 | "Name of the event." | "Map event" (`{{{name|{{{1|Map event}}}}}}`) |
| enemy | "The name of the enemy unit in the event." | none (the unit line is not shown) |
| variant | "The specific variant of the enemy unit" | the base enemy |
| amount | "The amount of units per event." | **none in the template**; only the event pages give one (below) |
| chance | "Chance of the event occurring." | **100%** (`{{{chance|100}}}`). The Enemies page adds: "If the event has no chance listed or a chance of 0, it is guaranteed to play out." |
| repeat | "Maximum number of times the event can repeat." | none, so it fires once. Shown as x(repeat+1), so total firings = 1 + repeat. `Infinite` is allowed. |
| delay | "How many seconds the event is delayed." | **0**. It is shown as the second mark inside the minute ("0:ss"). |
| duration | "The duration of the event in seconds." | none in the template |
| interval | "The interval of the event in seconds." | none in the template |
| range | "The enemy distance (from the player) of the event." | none in the template |

Enemies page, "Map events", raw:
- "Map events are short events that spawn groups of enemies outside the regular spawning cycle."
- "Map events are usually tied to a wave and trigger at the same second marks every time for that wave ... Each event specifies how many enemies are spawned, how many times and what intervals the event is repeated."
- "chanceWithLuck = eventChance / totalLuck ... The chance is unique for each event and is evaluated for every repeat of the event."
- Traps: "The trap events have a global cooldown ... effectiveCooldown = trapCooldown · totalLuck."
- Spawning: "When 300 or more enemies are alive the game will not spawn more enemies periodically, and only bosses and enemies from map events can spawn up until 500 enemies are alive."
- Skills: "Fixed Direction: Causes the enemy to only move in a straight line, instead of continuously homing to the player." "Floaty: Also known as 'Medusa'. Causes the enemy to move in a wavy pattern." "HP x Level ... applied the moment the enemy was spawned."

**The spacing between repeats is not written anywhere on the wiki.** The pages read consistently as "delay is the first firing's second mark, and also the gap between repeats":
- Mad Forest 25:00 Flower Wall `repeat=5|delay=10|duration=10` is listed as "25:10 Lasts 10 seconds, repeats 5 times".
- Mad Forest 27:00 Ghost `repeat=19|delay=3` gives 20 firings that fit inside the minute.
- Inlaid 29:00 Shade Bomb `delay=1|repeat=50` gives 51 firings in about 51 s.

This reading is inferred, not quoted. The pages also contradict themselves on the repeat count: Bat_Swarm calls `repeat=2` "Repeats 2 times", while Swarm calls `repeat=3` "4 times", which agrees with the template's x(repeat+1).

`interval` is used only by Shooting Star and Dragon Stream. Shooting_Star "Mechanics": "'Amount' specifies how many stars fall in each repeat and 'interval' the time between each falling star in the repeat". Dragon Stream: "periodically ... at set location until the end of its duration".

`range` is used by Pile Assault (0.7 or 0.4) and Shooting Star ("1 (max)", "0.5 (max)"). Pile_Assault: "Range determines the distance where enemies will be spawned relative to the player's position. Higher range values spawn enemies farther from the player". **The unit is not given.** It is probably a fraction of the screen; that is an inference.

## 2. Event kinds

Each kind lists its wiki rule, then the SH counterpart and the gap. Quotes are from the event page unless another page is named.

General SH mechanism for every "formation" event:
- `WaveTables.Events(stage)` holds an **invented schedule**: minutes 2-12, 5/10/15/25, 13/17/22, 20, 27 and 29, with counts of 12-30 and chances of 70-100.
- It has only three shapes (`StageEventType` Swarm, Wall and ClosingRing; `BuildFormation`). Each shape is chosen per stage from an owner-approved table.
- Units come from `PickSwarmPrefab`, which is the stage's single enemy prefab or its elite. They are **not the event's own VS variant**.
- Every unit flies with `EnemyFlyState` at **1.5x its move speed for a fixed 15 s**, then returns to the pool without drops.
- Repeats use `RepeatDelaySeconds`, and the chance is divided by Luck.
- SH's whole model (TimeSeconds, Repeat, RepeatDelay, Chance) could hold the VS table. What is missing is per-event units, amount, duration, delay and the exact rows.

| Event (VS id) | VS rule (quoted) | Defaults on the wiki | SH counterpart and gaps |
|---|---|---|---|
| **Bat Swarm** (BAT_SWARM, type Swarm) | "summons 50 Pipeestrellos that move in a straight line across the map instead of targeting the player. These units push other enemies along their path ... Bat Swarm units are immune to defang." Notes: "Has the Fixed Direction skill." | amount 50 | SH Swarm formation on L1 (spiral cluster crossing the player). Gaps: unit is the stage enemy, not Pipeestrello (swarm) (1 HP, 1 dmg, **speed 700**, res-defang 1); no push of other enemies; amount and timing are invented; 15 s life instead of crossing the map. Also on Inlaid Library 25:00 (amount 50). |
| **Ghost Swarm** (GHOST_SWARM, Swarm) | "summons 20 Swarm Ghost that move in a straight line across the map ... push other enemies along their path." | amount 20 | Same as Bat Swarm. No Ghost (swarm) type exists (10 HP, 5 dmg, speed 700, kb 0, Fixed Direction). |
| **Flower Wall** (FLOWER_WALL, type Circle) | "summons a ring of durable Flower Wall units around the player, which slowly creeps inward. The Flower Walls trap both the player and enemies inside until they are defeated or expire after their set duration." Stage note: "an ellipsoid ring of flowers". Mad Forest: 5:00 30 s; 10:00 60 s; 15:00 80% 60 s; 25:10 10 s x5 repeats. | amount not given for Mad Forest (doc example 100) | SH `ThornCircle` exists only as the Omen "Thorn Circle": 30 ThornWall (Flower Wall statbox, matches), radius 9 to 3 over 50 s, posts block the hero. Gaps: not on L1's clock at 5/10/15/25; ring is a circle, not an ellipse; does not hold other enemies (noted in code); amount unknown. The L1 ClosingRing at 5/10/15/25 in `WaveTables.Events` uses the stage enemy, not Thorn Walls. |
| **Circle** (GENERIC_CIRCLE) | "summons a large group of enemies around the character to attack the player." Mt.Moonspell 5:00 / 10:00 / 15:00 (80%) Moonspell Flower Wall amount 50, 30 / 60 / 30 s. | - | No SH counterpart on L6. Moonspell Flower Wall (30 x Level, 5 dmg, **speed 0**, kb 0) has no SH type. Flower_Wall: "both Moonspell variants are immobile". |
| **Shade Bomb** (SHADE_BOMB_RED) | "summons Red Shades that chases the player and self-destruct after a couple of seconds". Sig.ra_Rossi: "It chases the player and upon getting close enough it triggers its self-destruct, exploding 2 seconds later for 50 damage. It does not collide with other enemies." Notes: "Has no collision." | amount 1-4 per firing | SH has the data-driven ability `EnemyAbilityKind.SelfDestruct` (2 s fuse, 50 damage, from Sig.ra Rossi) and the Omen Powder Ring (`MineSentry`, still ring). **Not used on L2's clock.** No Sig.ra Rossi type (11 x Level, 3 dmg, speed 330). |
| **Medusa Wall** (MEDUSA_WALL) | "summons a line of Medusa Heads or Small Medusa Heads that randomly appears from either the left or right side and travels across the screen in a wavy pattern." | - | SH Wall formation (on L2 mostly ClosingRing). Gaps: VS is a line from the left or right in a wavy (Floaty + Fixed Direction) pattern. Units: Sneaky Head (1/1/240), Fast (1/1/600), Big (25/1/240, SH GloomMoth matches), Fast Big (25/1/600). |
| **Medusa Swarm** (MEDUSA_SWARM) | "summons a group of enemies that travel horizontally." Infobox: "Spawns a group of enemies that travel in a fixed direction." On Lake Foscari it is also used for Swimming Wiseparke and Sammy. Stage note: "Sammy Swarm ... spawns groups of Sammies which travel horizontally across the map until the end of the event." | - | SH Swarm (any direction). Gap: horizontal only, plus the variants. |
| **Skull Swarm** (SKULL_SWARM) | "spawns a group of enemies that beeline through the center of the screen at high speed. They do not collide with other enemies." Inlaid 20:05 / 25:05: "Every 10 seconds a new wave spawns, each double in size starting from 1 to 32, and one more 32, lasts 15 seconds." Lake Foscari 20:00 Swarm Snek: "Every 3 seconds ... from 1 to 64, lasts 15 seconds." | - | SH Swarm aims across the player's position (close to "through the center"). Gaps: doubling waves; units Skullone (swarm) (25/3/700, kb 0) and Swarm Snek (25/6/700). |
| **Swarm** (GENERIC_SWARM) | "summons a large group of enemies that set off to attack the player." Mt.Moonspell stage note: "Spritello Swarm ... sweep in a straight line across the map and can push other enemies along them." Lake: "Clown Swarm ... sweep in a straight line across the map and can push other enemies". **Mixed meaning:** Milk Elemental, Skeleton, Blood Moss and Snek units have no Fixed Direction (they home); Swarm Spiritello, Swarm Rotting Ghoul and Sam the Swarm Clown are Fixed Direction. | amount 25 / 50 per page rows | SH Swarm always flies a fixed line. Gap: homing swarms (Dairy Plant Milk Elemental and Skeleton, Abyss Blood Moss and Snek) should home at their own speed. Their SH types exist (SmokeWisp, ThickSmokeWisp, BurntBones, CrimsonSpore, CrimsonSporeCluster, SwampAdder) but are not used by events. Fixed swarms need new types. |
| **Pile Assault** (PILE_ASSAULT) | "summons a ring of enemies that surrounds the player and attacks from a distance, shooting bullets at regular intervals." Stage notes: "spawns a group of enemies around the player, shooting bullets at them" / "Pile Shooter ... shoot bullets in an ellipsoid ring around the player". Units: Twin Snakes "shoots bullets every 2 seconds ... cannot move"; Twin Demons every 1.5 s; Lost Twin every 1 s; Undead Mage every 2 s; Lost Head 2 s; Lost Head 2 1.5 s. | e.g. "Spawn 20, 0.7 range, lasts 5 seconds" | SH `PileShooter` / `StageHazards` Pile Assault: **only on L8**, 12 stage enemies on a 10x7 ellipse, 20 s, a bullet every 2.5 s (4 u/s, 10 dmg). Gaps: VS has it on L3 (Dairy), L4 (Gallo) and L5 (Abyss) with their own units, and all those SH types exist with matching stats (WellSerpents, BellImps, ShrineIdol, GateSorcerer, WhisperingHead, WailingHead); amount 6-20, range 0.4/0.7, durations 5-60 s; fire rate per unit. Bullet statboxes: health 1, kb 1, Fixed Direction (damage on the Bullet statboxes in Twin_Snakes / Lost_Twin / Undead_Mage / Lost_Head pages). |
| **Jellyfish Wall** (JELLY_WALL) | "summons 40 Wall Jellyfish by default, forming an ellipsoid ring around the player, lasting for 30 seconds. The ring of jellyfish periodically closes in on and moves away from the player, keeping the player and enemies contained within its perimeter." "The Wall Jellyfish takes 80% less knockback". | **amount 40, duration 30** | SH ClosingRing flies straight through the centre and on out (it "closes and opens out" once). Gaps: an ellipse that pulses in and out for 30 s and contains the player; units Jellyfish (wall) (180/8/200, kb 0.2) and Abyss Rellyfish (140/6/200, kb 0.2) / Swarm Rellyfish; amount 40/80. |
| **Jellyfish Swarm** (JELLY_SWARM) | "summons a large group of jellyfish, which slowly travels across the screen. Swarm Jellyfish have fixed movement, instead of homing to the player, and take reduced knockback." | - | SH Swarm at 1.5x speed (VS is slow, 150). Unit Jellyfish (swarm) (180/10/150, kb 0.2) is missing. |
| **Minotaur Rush** (MINO_RUSH) | "spawns a large group of mignotaurs, which travels across the screen at high speed, pushing along enemies in their way." Mignotaur (rush): "Does not collide with other enemies." | - | SH Swarm. Unit Mignotaur (rush) (250/9/**600**, kb 0.5, Fixed Direction) is missing; SH ChargingOxDemon is the plain Mignotaur (280). |
| **Shooting Star** (SHOOTING_STAR) | "rains groups of falling stars towards the player. The stars impact 2 seconds after an indicator marks their location, dealing 30 damage in the area." Mechanics: "landing locations are chosen at random within an area around of the player, with maximum distance determined by 'range'. 'Amount' specifies how many stars fall in each repeat and 'interval' the time between each falling star in the repeat." | - | SH `SoulMeteor` (2 s, 30 damage, radius 1.6) on **L6 and L10**, every 4 min from 4:00, 6 markers 0.2 s apart. Gaps: VS is on Gallo Tower (L4), at 0:05, 4:05, 4:10, 15:00, 15:05, 22:02 and 30:00 (infinite), with amount 1-12, interval 0-1.0 s and range 0.5/1 (max); chance and repeats. The SH meteor also hurts enemies (30); the wiki only says "dealing 30 damage in the area" (silent on enemies). |
| **Dragon Swarm** (DRAGON_SWARM) | "spawns groups of Serpentine Dragon Shrimps or Serpentine Flame Dragon Shrimps, which travel horizontally accross the map." Dragon_Shrimp: "the flag variants tend to make horizontal, zig-zag movements that do not follow the player's character. Flag variant Dragon Shrimps can only deal damage to the player character through their heads ... Has no collision." | - | SH Omen "Serpent Streams" (`SpawnFliers` with weave, stage enemies) only. Gap: no flag units (DS1 flag 5 x Level / 3 dmg / 400, DS2 flag 20 x Level / 6 / 400, kb 0, kb-max 0) and no segmented body with a damaging head only. |
| **Dragon Stream** (DRAGON_STREAM) | "summons groups of Serpentine Dragon Shrimps, travelling horizontally accross the screen, periodically at set location until the end of its duration. Spawns alternate between left and right side of the screen." | - | None (on L4's clock). Interval 1.0 / 1.33 s, amount 5, duration 60. |
| **Stalker** (The Stalker XL in Dairy Plant; The Stalker in Cappella) | "follows the player for 1 or 2 minutes, occasionally speeding up or slowing down." Behavior: "will follow the player around but will not actively make contact. Occasionally, it will speed up or slow down, appearing to chase the player, dealing massive damage upon contact." Killers: "Pentagram, Gorgeous Moon, Infinite Corridor, Crimson Shroud, Victory Sword, Rosary, ... or the Dairy Carts". Pentagram gives no chest; Gorgeous Moon and the others give {{treasure tier3=6 tier2=66 tier1=100 reward4=evo reward5=evo}}; carts give tier3=3 tier2=10. Cappella "weaker variant ... cannot drop a Treasure Chest". | Dairy: 0:00 1% 120 s; 8:00 30% 120 s; 12:00 30% 60 s | SH `DeathsShadow` follows the XL statbox and the Dairy rows exactly, but **on L5 and L9 (owner placement), not L3**. The trail 8 s / dash 2 s rhythm is balance (wiki silent). No Cappella "The Stalker" (2,550 x Level, 150 speed, res-defang 1, no chest, killable by Pentagram) on L8-10 at 10:10. |
| **Blinder / Sleeper** (Abyss Foscari) | "spawns a single black reaper that follows the player for 1 or 2 minutes, occasionally speeding up or slowing down." "This behavior is identical to that of The Stalker." Chest {{treasure tier3=6 tier2=66 tier1=100}} on the same killers. "Has 1% chance ... 0:00 ... Lasts 2 minutes"; "8:00 30% ... 1 minutes"; "12:00 99% ... 1 minute". | - | None on L5. Statbox: 500 x Level, 25 dmg, speed 150, xp 30, res-freeze 2, res-debuff 1. Unlike the Stalker XL it is killable by normal damage (finite health), which is an inference from the statbox. SH could reuse DeathsShadow with the Blinder statbox. |
| **Maddener** (Cappella 0:09.6, before The Ender) | "behaves similarly to the Stalker, chasing the player around and dealing massive damage on contact", plus "its ability to change the appearance of some enemies while it is alive". | - | None. 2,550 x Level, 50 dmg, 150, xp 0. |
| **Drowner** (Cappella 5:00; page prose 5:10) | Stage note: "spawns a blue reaper that floods the lower part of the stage, slowly moving upwards. Player takes damage if they enter the water. At 10 minute mark Drowner gains a notable MoveSpeed increase." Page: "Entering the water will cause the player to rapidly take 20 damage per game-tick. Freezing the Drowner also causes the flood to stop rising." Cappella variant: "it can be defeated with Pentagram and defeating it won't make it drop a Treasure Chest". | - | SH `RisingTide` exists only as the Omen "Rising Tide": Tide Caller 800 x Level, 60 s, 20 per 0.5 s tick. Gaps: not on L8-10's clock at 5:00; the Cappella statbox is 2,550 x Level (SH uses the 800 Boss Rash value); the 10-minute speed-up; Cappella rules (Pentagram kills it, no chest). |
| **Trickster** (Cappella 15:00/15:10) | "periodically summons ellipsoid rings of 48 Poltergeist Gems around the player that self-destruct when approached. The gems lasts for 30 seconds ... The Trickster summons two rings upon spawning and one every 30 seconds thereafter ... either a large or a small ring ... larger ring remain stationary ... smaller ring will home at the player ... When The Trickster dies, all remaining Poltergeist Gems explode." Stage note: "can also stick the gems to the player or attack the player directly". | rings of 48 | Only part of it: the SH Omen Powder Ring (`MineSentry`, 12 still self-destructing members, 60 s). No Trickster, no 48-gem rings, no homing small ring, no Poltergeist Gem type. |

Cappella reapers only come "before defeating The Ender". Soul Hunter has no Ender, so which condition applies on L8-10 is an **owner question**.

## 3. Traps (pressure plates)

Stage pages, raw:
- Dairy Plant: 9 options; "must cool down for at least 20 seconds".
- Gallo Tower: 7 options.
- Abyss Foscari: 11 options.
- Lake Foscari: 9 options.

Pile_Assault, Jellyfish_Wall and Circle list some trap rows, for example "1/9 chance ... Spawn 40, 0.7 range, lasts 5 seconds" and "Spawns 50 Wall Jellyfish instead of 40".

SH `PressurePlate` instead puts 4 plates on every stage. Each one fires a random Swarm, Wall or ClosingRing of 16 stage enemies, with a global cooldown of 20 x Luck. Gap: the per-stage option lists (only partly on the wiki: the Pile_Assault, Jellyfish_Wall, Circle and Swarm "From a trap pizza" rows).

## 4. Statboxes and SH types

The full list is in `mapevent-variants.json`: 43 stage/variant rows, each with statbox fields, `shType` (exact match of health, HP x Level, damage, VS speed, knockback, max knockback and xp on that stage's level) and notes.

Exact SH matches:
- L1: Flower Wall to ThornWall.
- L2: Big Sneaky Head to GloomMoth.
- L3: Twin Snakes to WellSerpents, Twin Demons to BellImps, Lost Twin to ShrineIdol, Milk Elemental to SmokeWisp, Milk Elemental 2 to ThickSmokeWisp, Skeleton to BurntBones.
- L4: Undead Mage to GateSorcerer.
- L5: Lost Head to WhisperingHead, Lost Head 2 to WailingHead, Blood Moss to CrimsonSpore, Swarm Blood Moss to CrimsonSporeCluster, Snek to SwampAdder.

That is 14 matches.

No SH type: 29 rows.
- 23 ordinary event units:
  - L1: Pipeestrello (swarm), Ghost (swarm).
  - L2: Sig.ra Rossi, Sneaky Head, Fast Sneaky Head, Fast Big Sneaky Head, Skullone (swarm), Pipeestrello (swarm).
  - L3: Jellyfish (wall), Jellyfish (swarm), Mignotaur (rush).
  - L4: Dragon Shrimp 1 (flag), Dragon Shrimp 2 (flag).
  - L5: Swarm Rotting Ghoul, Rellyfish, Swarm Rellyfish.
  - L6: Swarm Spiritello, Swarm Dusty Spiritello, Moonspell Flower Wall.
  - L7: Sammy the Caterpillar, Swimming Wiseparke, Sam the Swarm Clown, Swarm Snek.
  - These are 22 distinct statboxes, because Pipeestrello (swarm) appears on both L1 and L2.
- 6 reaper variants: The Stalker XL (L3; exists as DeathsShadow on L5/L9), The Blinder (L5), and The Maddener, The Drowner, The Stalker and The Trickster (L8-10).

Statbox notes that matter for building them:
- Every swarm or flag unit has the "Fixed Direction" skill. Sneaky Heads and Swimming Wiseparke also have "Floaty". Dragon Shrimp flag, Sneaky Head ("Has maximum speed of 340 / 700"), Skullone (swarm), Mignotaur (rush), Rellyfish and Spiritello swarm have "no collision" or "does not collide with other enemies".
- Pipeestrello (swarm) has res-defang 1, and the Bat Swarm page says its units are immune to defang. The Cappella reapers also have res-defang 1.
- XP is stored as the raw VS value, as the SH tables do.

## 5. Biggest gaps (summary)

1. **Event units.** SH events spawn the stage's single enemy prefab or elite (`PickSwarmPrefab`), never the event's VS variant. 22 new statbox types are needed, plus Fixed Direction / no-collision flags.
2. **Schedule.** `WaveTables.Events` is an invented, provisional schedule for all 10 levels. The VS stage tables (`mapevents2.json`, 189 entries) are not used: wrong minutes, delays, amounts, chances and repeats.
3. **Shapes.** SH has 3 straight-line shapes. VS also has:
   - a pulsing, containing ellipse (Jellyfish Wall);
   - a creeping, trapping ring (Flower Wall and Circle on the clock);
   - a still shooting ring with range (Pile Assault on L3/L4/L5);
   - a horizontal-only wavy line from left or right (Medusa);
   - doubling skull waves through the centre;
   - homing swarms (generic Swarm with non-fixed units);
   - segmented head-only-damage dragons, with a stream that alternates sides;
   - Shade Bomb chasers that self-destruct;
   - Shooting Star with amount, interval and range on L4;
   - the reapers Blinder, Maddener, Trickster, the Cappella Stalker and the Drowner on the clock.
4. **Lifetime and speed.** SH fliers live a fixed 15 s at 1.5x speed. VS uses the event's `duration` and the unit's own speed (700 for bat, ghost and skull swarms; 150 for jellyfish).
5. **Push.** VS swarm units "push other enemies along their path". SH fliers do not.
6. **Placement differs from the VS stages.** The Stalker XL is on L5/L9 instead of L3, Shooting Star on L6/L10 instead of L4, and Pile Assault on L8 instead of L3-L5; all three are owner placements in AGENTS.md. Changing them needs the owner.
7. **Unknowns (wiki silent).**
   - Repeat spacing: is it `delay`? This is inferred.
   - The unit of `range`.
   - Default amounts for events whose rows omit `amount` (Mad Forest Flower Wall, Bat Swarm uses the page default 50, Ghost 20, Jellyfish Wall 40).
   - The Cappella reapers' "before defeating The Ender" condition in SH (owner).
   - Whether Shooting Star hurts enemies.
