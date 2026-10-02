# Vampire Survivors DOT table vs Soul Hunter

Built 2026-09-30 from raw vampire.survivors.wiki pages (fetched that day) and the Soul Hunter code at commit 0f9a73cd.
Status: MATCH, PARTIAL, MISSING, DIFFERS-OWNER (an owner decision), SH-ONLY (not in VS, no owner decision), N/A, UNKNOWN.

Counts: MATCH 72, PARTIAL 62, MISSING 27, DIFFERS-OWNER 8, UNKNOWN 3, SH-ONLY 2, N/A 2

## Top gaps

1. **Chests give new items.** VS chests only level up items you own (Treasure_Chest, patch 0.2.7). SH's chest draw can hand out a new weapon or passive.
2. **Growth at levels 20 and 40.** VS gives +100% Growth at levels 20 and 40 to offset the +600 / +2400 XP. SH charges the XP without the Growth, so levelling there takes about twice as long.
3. **Six evolutions missing.** Hellfire, La Borra, NO FUTURE, Thunder Loop, Gorgeous Moon and Mannajja have every part in SH but no evolution. Crimson Shroud can never be reached because nothing gives Metaglio.
4. **Stat caps missing.** VS caps Might and Area at 1000%, Speed and Duration at 500%, Amount at 10. SH caps only Soul Eggs.
5. **Invulnerability timings.** VS: 240 ms after a hit (SH 0.5 s); brief i-frames after a chicken and after a chest (SH none).
6. **Base weapons crit.** In VS, Whip/Knife/Axe crit only with the Slash Arcana. SH gives 6 weapons 10% at 2x.
7. **Results and menu shell.** The Results screen is built (Level 1 step 4, 2026-10-02); still no Collection, Bestiary, Unlocks or Credits.
8. **Orologion and spawning.** New spawns are not frozen during Orologion; enemies can spawn on screen at the sides; event spawns have no 500 cap.
9. **Arcana.** No choose screen at start and no pick-1-of-4 chest screen; only 4 Arcanas with partial effects, using VS names.

## Owner decisions

- Decided 30 Sep: 5% rule = numbers ±5%; dash without damage; invented unions removed; gems 20/90; revival gold stacks; Shadow Kael's Clock Lancet slows, Pentagram erases items (all built).
- Decided 30 Sep: add Ebony Wings, Silver/Gold Ring, Stone Mask and a second gun (Phase 2).
- Decided 30 Sep: the VS unlock system (Phase 3).
- Open: Soul Hunter names for the Arcanas (asked when they are built).

## Player & stats

| System | VS rule | Wiki | DOT | Soul Hunter now | Status | Gap / action |
|---|---|---|---|---|---|---|
| XP curve | 5 XP for level 2, then +10 per level to 20, +13 to 40, +16 after; +600 at level 20, +2400 at level 40 | Level_up | Data | Same curve in 10-XP gems (PlayerExperience.GemsToNextLevel) | MATCH |  |
| XP curve | At levels 20 and 40 the player gets +100% Growth (removed at 21 and 41) to offset the extra XP | Growth | Behavior | No Growth bonus; the extra 600/2400 is charged with no offset | MISSING | Add +100% Growth while at level 20 or 40; levelling there now takes about 2x VS |
| Level-up | 4th choice with chance 1 - 1/Luck | Level_up | Behavior | LevelUpManager.ChoiceCountForLuck | MATCH |  |
| Level-up | Rarity-weighted draw; two owned-item checks at 1 + 0.3x - 1/Luck | Level_up | Behavior | Built 2026-09-28 (WeaponDefinition, UpgradeRarity) | MATCH |  |
| Level-up | Items thrown away by a Reroll cannot come back in the new set | Reroll | Behavior | Reroll() draws again from the full pool | PARTIAL | Exclude the previous choices |
| Level-up | Everything maxed: Gold or Chicken; an 'always' option after 100 Big Coin Bags | Level_up | UI | Two cards (MaxedRewards); no 'always' option | PARTIAL | Add the counter and the option |
| Level-up | Brief invulnerability after the pick (no length given) | Level_up | Timing | 0.5 s (LevelUpManager.ResumeGameplay, 0f9a73cd) | MATCH |  |
| Skip | Skip gives XP (Tips: 20% of the next level) | Skip | Behavior | SkipExperienceShare 0.2 | MATCH | Source is thin (Tips only) |
| Reroll/Skip/Banish | Base 0; +2 per PowerUp rank; eggs +0.1 / +0.2 / +0.2 | Reroll, Skip, Banish | Data | LevelUpManager: start 0, PowerUps + eggs | MATCH |  |
| Banish | Banished owned item keeps its rank; chests stop upgrading it | Banish | Interaction | _banishedTypes used by level-ups and chests | MATCH |  |
| Stat caps | Might 1000%, Area 1000%, Speed 500%, Duration 500%, Amount 10 | Player_stats, Might, Area ... | Edge case | Only Soul Eggs are capped; PlayerStats does not clamp | MISSING | Clamp totals in PlayerStats |
| Cooldown | Base 100%, floor 10%, additive | Cooldown | Data | Mathf.Max(0.1f, ...) in PlayerStats | MATCH |  |
| Armor | -1 damage per point, at least 1 per hit; retaliation +10%/point up to 50 | Armor_(stat) | Behavior | HealthController.TakeDamage; CrimsonShroud clamps 0-50 | MATCH |  |
| Max Health | Base 100; character bonus flat; PowerUps and Hollow Heart multiply | Max_Health | Data | BaseMaxHealth + x1.1^rank | MATCH |  |
| Recovery | Base 0 HP/s; each 0.1 adds +10% to pickup healing | Recovery | Behavior | PlayerRecovery, HealWithRecovery | MATCH |  |
| Magnet | (30 + eggs) x character x PowerUp, all multiplied | Magnet | Data | 4 units base; character Magnet is added, eggs +1% | PARTIAL | Multiply the character Magnet |
| Move Speed | Base 100%, additive; stage bonus multiplies | Move_Speed | Data | PlayerController.MoveSpeed x stage speed | MATCH |  |
| Luck | No cap: 4th choice, chests, light sources, crits, owned offers; event chance / Luck | Luck | Behavior | Read by level-up, chest, lantern, crit and spawner | MATCH |  |
| Growth / Greed | Base 100%, additive; Growth x gem XP; stage gold bonus multiplies Greed | Growth, Greed | Behavior | AddXP x ExpBonus; GoldRewards.ApplyGreed | MATCH |  |
| Revival | Revive at 50% Max Health with long i-frames; the heal counts as healing | Revival | Behavior | ReviveFromDeath 50%, 1.5 s; health set directly | PARTIAL | Fire the heal hooks (Soul Eater) |
| Revival | Death shows a Revive button (or Quit), then an animation | Revival | UI | Revival is automatic | MISSING | Revive button + animation |
| Hit i-frames | Invulnerability Time 240 ms after a hit (cap 1 s) | Invulnerability_Time | Timing | 0.5 s (_hitInvulnerabilitySeconds) | PARTIAL | Set 0.24 s |
| Hit i-frames | Floor Chicken also gives invulnerability (length not given) | Invulnerability_Time, Pickups | Behavior | HealthPickup gives none | MISSING | Add brief i-frames |
| Controls | Movement only; weapons fire on their own | Weapons | Input | Move + Dash + Soul Burst (from the owner's GDD) | DIFFERS-OWNER |  |
| Controls | (VS has no dash) | - | Behavior | Dash only moves and avoids damage (GDD); stops at walls. Fixed 2026-09-30 | DIFFERS-OWNER | Owner decision: dash from the GDD, no damage |
| Stats panel | Stats visible on character select, pause and level-up | Player_stats | UI | Character select only | MISSING | Add a stats panel to pause and level-up |
| Character select | White = character/egg values, yellow = PowerUps; Reroll/Skip/Banish rows; Magnet hidden | Characters, Magnet | UI | All bonuses yellow; no R/S/B rows; Magnet shown | PARTIAL | Split colours, add rows, hide Magnet |
| Characters | Starting weapon and stat bonuses | Characters | Data | CharacterData, all 13 checked | MATCH |  |
| Characters | Per-level bonuses are capped (Antonio +50% Might at 50; Divano +5 Armor at 25) | Antonio_Belpaese, Armor_(stat) | Edge case | ApplyLevelBonus has no cap (Corvan, Valerius) | PARTIAL | Add caps (level 50 / 25) |
| Characters | Price rises +10% per character bought | Characters | Persistence | Fixed GoldPrice | PARTIAL | Add +10% per purchase |
| Characters | Sort (default, A-Z, stages) and favourites | Characters | UI | Level filter only | PARTIAL | Add sorting |
| PowerUps | 23 PowerUps, VS ranks, prices, fees, Refund | PowerUps | Data | PowerUps.cs, PowerUpShopTests | MATCH |  |
| PowerUps | A maxed PowerUp can be switched off | PowerUps | UI | No toggle | MISSING | Add On/Off for maxed PowerUps |
| PowerUps | Reroll/Skip/Banish/Omni/Charm/Defang ranks unlock by achievements | Reroll, Skip, Banish | Behavior | Every rank buyable | PARTIAL | Owner decision (SH has no achievements) |

## Weapons & level-up

| System | VS rule | Wiki | DOT | Soul Hunter now | Status | Gap / action |
|---|---|---|---|---|---|---|
| Slots | 6 weapons, 6 passives | Weapons, Passive_items | Data | WeaponManager 6 / 6 | MATCH |  |
| Slots | Stage items can go past the 6 slots | Weapons, Passive_items | Edge case | Full slots turn a stage item into 10 gold | PARTIAL | Allow past the slots |
| Slots | Each item once per run (Tirajisu can come back) | Weapons | Behavior | Level-0 check; Tirajisu release | MATCH |  |
| Level-up | Level-up cards show discovered evolutions (0.3.2) | Level_up | UI | No evolution hint | MISSING | Evolution marker on cards |
| Level-up | Skip and Reroll unavailable when the inventory is maxed | Level_up | Edge case | Buttons check only charges | PARTIAL | Disable on maxed / gold-chicken screen |
| Evolution | Max weapon + passive held + chest | Evolution | Behavior | RequirementsMet, passive level >= 1 | MATCH |  |
| Evolution | Whip, Wand, Knife, Axe, Cross, Bible, Garlic evolutions | Evolution | Data | All 7 pairs correct | MATCH |  |
| Evolution | Fire Wand+Spinach, Santa Water+Attractorb, Runetracer+Armor, Lightning Ring+Duplicator, Pentagram+Crown, Song of Mana+Skull O'Maniac | Evolution | Data | None exist; SH has every part | MISSING | 6 evolutions (own names and art) |
| Evolution | Laurel + Metaglio Left + Right = Crimson Shroud | Evolution, Metaglio_Left | Behavior | Nothing gives Metaglio (AcquireStagePassive only called by a test) | PARTIAL | Place Metaglio as stage items |
| Evolution | Peachone+Ebony Wings; Clock Lancet+Silver/Gold Ring | Evolution | Data | SH has no Ebony Wings or Rings | MISSING | Owner: add counterparts or accept |
| Evolution | Only evo chests (10:00+, MF 1:00); one evo per chest, first reward | Evolution | Timing | CanEvolveAt, one per chest | MATCH |  |
| Evolution | Base weapon removed, passive kept | Evolution | Behavior | DisableOldWeapon | MATCH |  |
| Union | All base weapons max -> one slot (e.g. Phiera+Eight+Tirajisu) | Evolution#Union | Data | 6 invented unions removed 2026-09-30; system kept | MISSING | VS unions come with the second gun (Phase 2) |
| Upgrade | Levelling a weapon resets its cooldown and fires it at once | Weapons | Timing | ApplyWeaponLevel leaves the timer | MISSING | Reset timer on level-up |
| Stats | Might, Area, Duration, Speed, Cooldown add together | Might, Area ... | Data | PlayerStats additive | MATCH |  |
| Crit | Base Whip/Knife/Axe crit only with the Slash Arcana | Whip, Knife, Axe, Slash_(XVI) | Behavior | 10% at 2x on 6 weapons (provisional) | SH-ONLY | Remove base crits |
| Crit | Per-weapon crit: Heaven Sword 10%/2.5x, Bloody Tear 10%/2x | Heaven_Sword, Bloody_Tear | Data | Global 2x | PARTIAL | Per-weapon multiplier |
| Hit interval | King Bible 1.7 s, Santa Water 0.5, Runetracer 0.5, Song of Mana 1.0 | Weapon pages | Timing | Bible 0.5 s; others not mapped | PARTIAL | Use wiki values |
| Knockback | Knife 0.5, Gun 0.1, Peachone 2, Garlic 0 | Knockback, weapon pages | Data | 1 for all; Garlic 0.3 | PARTIAL | Use wiki values |
| King Bible | 3 s on, then off until cooldown | King_Bible | Timing | Books always orbit | PARTIAL | On/off cycle |
| Pentagram | 1 s duration; erases ground items | Pentagram | Behavior | Not mapped | PARTIAL |  |
| Cherry Bomb | Explode chance 40% (60% max) | Cherry_Bomb | Behavior | Contact damage only | MISSING | Explode roll |
| Peachone | 4 s duration | Peachone | Timing | Not mapped | PARTIAL |  |
| Aiming | Whip horizontal, Clock Lancet 12 directions, etc. | Weapon pages | Behavior | All aim at nearest enemy | DIFFERS-OWNER |  |
| Guns | Phiera and Eight The Sparrow are two weapons | Phiera_Der_Tuphello | Data | One 'Gun' | PARTIAL | Owner: second gun? |
| Limit Break | Random per-stat cards with caps; reset on evo; not Laurel | Limit_Break | Behavior | +10% damage per card; Laurel not excluded | PARTIAL | VS stat table |
| Arcana | Pick an Arcana at start (or random) | Arcanas | UI | Random | PARTIAL | Choose screen |
| Arcana | 11:00/21:00 chest: pick 1 of 4 (6), Reroll; normal chest at 3 | Arcanas | UI | Random grant; normal at 3 matches | PARTIAL | Pick-from-four screen |
| Arcana | 22 Arcanas; e.g. Awake: bonuses on each revival | Arcanas, Awake_(IV) | Data | 4 only; Awake at once; Gemini unread; Iron Blue Will 1 bounce | PARTIAL | Fix effects |
| Arcana | - | - | Data | Uses VS Arcana names | SH-ONLY | No-copy rule: rename |

## Enemies & run clock

| System | VS rule | Wiki | DOT | Soul Hunter now | Status | Gap / action |
|---|---|---|---|---|---|---|
| Spawning | Enemies spawn just outside the screen | Enemies | Behavior | Fixed 20-30 unit ring; can be on screen at the sides | PARTIAL | Derive from the camera view |
| Spawning | Per-minute waves: minimum + interval | Enemies, Mad_Forest | Behavior | WaveData.Minutes | MATCH | Numbers are SH's curve |
| Spawning | Above the minimum: one of each wave type per tick | Enemies | Behavior | Built; no stage lists types (group of 1-6) | PARTIAL | Needs enemy content (Art) |
| Spawning | 300 alive: only bosses and events spawn | Enemies | Edge case | _maxAliveEnemies 300 | MATCH |  |
| Spawning | Hard cap 500 whatever Curse/Charm | Curse | Edge case | Events uncapped (316 seen) | PARTIAL | Cap at 500 |
| Spawning | Starting spawns (MF 10) | Mad_Forest | Timing | Minimum top-up only | PARTIAL | Per-stage opening burst |
| Spawning | Far enemies can despawn | Enemies | Behavior | Relocated ahead, keep health | PARTIAL | Wiki gives no distance |
| Bosses | Bosses teleported back, never despawn | Enemies | Behavior | Included in relocation | MATCH |  |
| Bosses | Stage boss at 25:00 | Mad_Forest | Timing | 25:00, does not end stage | MATCH |  |
| Bosses | HP x Level at spawn | Enemies | Data | Bosses/elites yes; wave path ignores HealthPerPlayerLevel | PARTIAL | Use EffectiveHealth in SpawnEnemy |
| Bosses | Resist freeze, instant kill, debuff, knockback | Enemies | Data | All bosses | MATCH |  |
| Chest elites | MF minutes 1,3,5,7,9,10,12,14,15,16,18,20,22,23,25,27,29; Arcana 11, 21 | Mad_Forest | Timing | DefaultChestEliteMinutes | MATCH |  |
| Mini-bosses | Chest-less wave bosses (MF 8:00, 17:00) | Mad_Forest | Data | None | MISSING | Needs models |
| Chest elites | Chest chance and tier per wave row | Enemies, Mad_Forest | Data | Always drops | PARTIAL | Per-row tiers |
| Enemy stats | Per-enemy HP, power, speed, kb, XP (Pipeestrello: 1 HP, 5 dmg, 1 XP) | Enemies, Pipeestrello | Data | SH placeholders; speed 3 for all; XP 10 | PARTIAL | Check XP vs curve |
| Knockback | Reverse 120 ms x weapon kb x enemy kb; Garlic +0.3 to 3 | Knockback | Behavior | EnemyController.ApplyKnockback | MATCH |  |
| Resistances | Freeze res vs weapon chance; Orologion ignores | Freeze | Interaction | TryWeaponFreeze | MATCH | Update citation |
| Resistances | Instant kill and debuff resistance | Enemies | Interaction | ResistsInstantKill, EnemyResistance | MATCH |  |
| Movement | Fixed Direction and Floaty | Enemies | Behavior | EnemyMovement | MATCH |  |
| Freeze | Player can push frozen enemies | Freeze | Interaction | Enemies are triggers | UNKNOWN | Check in play |
| Curse | Health at once; other Curse effects from the next minute | Curse | Timing | All at once | PARTIAL | Delay to the minute |
| Curse | Curse x health, speed, minimum; interval / Curse | Curse | Data | EnemySpawner | MATCH |  |
| Map events | Timed events with repeat and delay | Enemies | Timing | StageEventEntry | MATCH |  |
| Map events | Chance = eventChance / Luck per repeat | Enemies | Behavior | EventFires | MATCH |  |
| Map events | Swarm pushes enemies; Flower Wall lasts 30/60 s | Mad_Forest | Behavior | Fliers 15 s, no push | PARTIAL |  |
| Traps | Global trap cooldown x Luck | Enemies | Behavior | 20 s per plate | PARTIAL | Global x Luck |
| Death | Kills give XP gems; >400 merge | Experience_Gem | Behavior | EnemyDrop, cap 400 | MATCH |  |
| Contact | Contact damage 'multiple times per second' | Armor_(stat) | Timing | 0.5 s per enemy | UNKNOWN | Wiki silent |
| Run clock | 30:00, then the Reaper | Stages, The_Reaper | Timing | Next stage at 30:00; L10 no limit | DIFFERS-OWNER |  |
| Stalker | Killable (HP x Level 2,550), 50 dmg, spawn chance | The_Stalker | Behavior | Death's Shadow: always, unkillable, 30 dmg | PARTIAL | Make it VS |
| Shooting Star | Markers, 2 s, 30 damage | Shooting_Star | Timing | SoulMeteor | MATCH |  |
| Clock Lancet | Freeze, no damage | Clock_Lancet | Behavior | ClockLancetWeapon; boss copy slows the player 50% (owner exception) | MATCH |  |

## Pickups & economy

| System | VS rule | Wiki | DOT | Soul Hunter now | Status | Gap / action |
|---|---|---|---|---|---|---|
| XP gems | Blue up to 2 XP, green up to 9, red above | Experience_Gem | Data | Up to 20 / up to 90 / above (x10 for 10-XP gems, owner 2026-09-30) | MATCH |  |
| XP gems | 400 on the ground, XP merges into one red gem | Experience_Gem | Performance | MaxGemsOnGround 400 | MATCH |  |
| Magnet | Gems pulled in the Magnet radius | Magnet | Behavior | PlayerExperience 4 u x Magnet | MATCH |  |
| Magnet | Magnet also collects other pickups | Magnet, Pickups | Behavior | Coins/chicken fixed 1.5 radius | PARTIAL | Scale by Magnet |
| Chest | Chest is not pulled | Treasure_Chest | Behavior | No magnet | MATCH |  |
| Light source | 10 HP, broken by weapons | Light_source | Data | LightSource | MATCH |  |
| Light source | 1 s attempts, chance x Luck, capped; replace oldest | Light_source | Timing | LightSourceSpawner | MATCH |  |
| Light source | Per-stage chance/cap/max | Stage pages | Data | ForStage L1-5 | MATCH | CM max 20 unverified |
| Light source | IL/DP/GT fixed spots | Light_source | Behavior | Random off-screen | PARTIAL |  |
| Light source | Drop weights and levels; rarity x Luck | Light_source, Luck | Data | LightSourceDrops | MATCH | Little Clover 0.5 vs 1 conflict |
| Enemy drops | Enemies drop only gems | Pickups | Behavior | EnemyDrop | MATCH |  |
| Floor Chicken | Heals 30 | Floor_Chicken | Data | 30 x (1+Recovery) | MATCH |  |
| Coins | 1 / 10 / 100 / 25, x Greed | Coin pages | Data | GoldRewards | MATCH |  |
| Vacuum | Pulls every gem | Vacuum | Behavior | MagnetPickup | MATCH |  |
| Orologion | 10 s; new spawns also frozen | Orologion | Behavior | Only enemies alive at pickup | PARTIAL | Freeze new spawns |
| Orologion | Some enemies resist | Orologion | Edge case | Ignores resistance | PARTIAL | Wiki conflicts |
| Orologion | Halves Gold Fever decay | Orologion | Interaction | Not built | MISSING |  |
| Rosary | Kills screen; some immune | Rosary | Behavior | Soul Toll | MATCH |  |
| Rosary | Can kill Stalker, which drops a chest | Rosary | Edge case | Shadow immune | PARTIAL |  |
| Nduja | 10 s fire: 30 dmg, 12, 0.5 s, area 200% | Nduja_Fritta_Tanto | Data | FireBreath | PARTIAL | Area 200% not mapped |
| Nduja | Fires where the hero faces | Nduja_Fritta_Tanto | Input | Nearest enemy | DIFFERS-OWNER |  |
| Clovers | Little +10% Luck; Gilded: gold fever | Little_Clover, Gilded_Clover | Data | Luck Charm, GoldFever | MATCH |  |
| Chest count | 5 -> 3 -> 1, each x Luck, per boss | Treasure_Chest | Data | One global 3%/10% roll | PARTIAL | Per-elite tiers |
| Chest count | First six: 1-1-3-1-1-5 | Treasure_Chest | Persistence | FirstChestSequence | MATCH |  |
| Chest contents | Chests level up owned items only (0.2.7) | Treasure_Chest | Behavior | Chest draw can give NEW items (GetRandomValidUpgradesForChest) | PARTIAL | Owned items only |
| Chest gold | 100-200 / 300-600 / 500-1000 x Greed | Treasure_Chest | Data | ChestLogicController | MATCH |  |
| Chest maxed | Each item -> Big Coin Bag 25 | Treasure_Chest | Edge case | GoldPerMaxedItem 25 | MATCH |  |
| Chest evolution | From 10:00 (MF 1:00); one per chest | Evolution | Behavior | CanEvolveAt | MATCH |  |
| Chest look | Bronze / silver (evo) / purple Arcana | Treasure_Chest | VFX | One look | MISSING |  |
| Chest i-frames | Brief invulnerability after a chest (0.2.4) | Treasure_Chest | Behavior | None | MISSING | Add on close |
| Chest screen | Blast, OPEN, reels, count-up, lights, fireworks, jingles | Treasure_Chest | UI | ChestOpeningScreen | MATCH |  |
| Chest skip | Skip rules | Treasure_Chest | Input | ChestSkipRules | MATCH |  |
| Greed | Additive; stage bonus separate | Greed | Data | ApplyGreed | MATCH |  |
| Greed | Stone Mask passive | Greed | Data | No SH item | MISSING | Owner decision |
| Stage gold | MF 1.0 ... CM 1.4; Hyper +50% | Stages | Data | GoldBonus | MATCH |  |
| Merchant | Moongolow / every stage later | Merchant | Behavior | Level 3 only | DIFFERS-OWNER |  |
| Merchant | Touch opens shop, one visit | Merchant | Interaction | WanderingMerchant | MATCH |  |
| Merchant | Bone, Cherry Bomb 1,000; Golden Egg 10,000 | Merchant | Data | Same | MATCH |  |
| Merchant | Endless: Revival ware each cycle | Stages | Behavior | Not built | MISSING |  |
| Merchant | Inverse: Skip/Banish/Reroll, Arcana | Merchant | Data | Not built | MISSING |  |
| Golden Egg | 19 stats, caps, next run | Golden_Egg | Persistence | SoulEggs | MATCH | Magnet +1% stand-in |
| Stage items | Extra copies with Luck | Mad_Forest | Data | 4 fixed items | PARTIAL |  |

## Stages, modes & menus

| System | VS rule | Wiki | DOT | Soul Hunter now | Status | Gap / action |
|---|---|---|---|---|---|---|
| Stage | 30:00 limit | Stages | Timing | LevelProgressionManager | MATCH |  |
| Stage | Stage bonuses: speed and gold | Stages | Data | PlayerSpeedFor / GoldFor | MATCH | L6-10 SH mapping |
| Stage | Stage + mode bonuses summed, then multiply | Stages | Data | GoldBonus + ApplyGreed | PARTIAL | Check order |
| Stage | Clear: 500, then +100/+200/+300/+400 per unused revival, then +100 | The_Reaper, Revival | Data | StageClearGold stacks (owner chose these pages, 2026-09-30) | MATCH |  |
| Stage | Confined stages (corridors) | Stages | Behavior | L4 corridor | MATCH |  |
| Stage | Trap plates trigger events | Stages | Interaction | Rune Plates | MATCH |  |
| Stage | Pre-placed items and relics | Stages | Data | 4 items; no relics | PARTIAL |  |
| Stage | Coffins unlock characters | Stages | Interaction | Condition unlocks | MISSING | Owner call |
| Unlocks | 20 light sources unlock Fire Wand | Light_source | Persistence | No item unlocks | MISSING |  |
| Mode | Hurry: clock x2, XP +25% | Stages | Timing | RunModes | MATCH |  |
| Mode | Hyper: per-stage values | Stages | Data | Flat; x1.25 spawn not on wiki | PARTIAL |  |
| Mode | Inverse: gold, Luck, enemy HP/speed | Stages | Data | Matches | PARTIAL | +0.5 speed unit unclear |
| Mode | Inverse: flipped darker map, alt music | Stages | UI | Not built | MISSING |  |
| Mode | Endless cycles; +100% HP, +50% spawn, +25% dmg, damage cap -1 | Stages | Behavior | Multipliers match; no damage cap | PARTIAL |  |
| Mode | Random Events | Random_Events | Behavior | None | MISSING |  |
| Mode | Random LevelUp | Stages | Behavior | None | MISSING |  |
| Mode | Modes unlock via relics | Stages | Persistence | All open | PARTIAL | Owner call |
| Audio | Track per stage; Music Selection | Music | Audio | 11 CC0 loops | PARTIAL |  |
| HUD | XP bar at top | Level_up | UI | XPBarUI | MATCH |  |
| HUD | 6 + 6 slots | Passive_items | UI | InventoryHud | MATCH |  |
| HUD | Timer, kills, gold, health bar | - | UI | Built | UNKNOWN | Wiki silent |
| Pause | Options; Grimoire; Map | Options, Grim_Grimoire | UI | Resume/Options/Quit | PARTIAL |  |
| Pause | Quit still shows results | Results | Behavior | Quit ends the run into Results (`GameSessionManager.QuitRun`) | MATCH | Level 1 step 4 |
| Results | Stage, mode, gold bonus, time, gold, level, kills | Results | UI | `RunResultsScreen` top left | MATCH | Level 1 step 4; run time over all stages |
| Results | Per weapon: level, damage, time, DPS | Results | UI | Every hit credited to its weapon (`DamageSource`); SI prefixes; top damage/DPS yellow | MATCH | Level 1 step 4; damage counts overkill (wiki silent) |
| Results | Character, eggs, items, PowerUps, pickups, Arcanas; unlocks toggle; DONE | Results | UI | All built; DONE to the main menu | MATCH | Level 1 step 4; pickup names are Soul Hunter's, gems not listed (wiki silent); no mobile ad button |
| Options | Favorites, Display, Sound, Gameplay | Options | UI | No Favorites; Controls extra | PARTIAL |  |
| Options | Display incl. Refresh Rate | Options | UI | No Refresh Rate | PARTIAL |  |
| Options | Gameplay options | Options | UI | Joystick Type missing | PARTIAL |  |
| Options | Language, co-op, Twitch ... | Options | UI | - | N/A |  |
| Save | Delete Save, Data Recovery | Options | Persistence | Atomic + .prev | PARTIAL |  |
| Menu | Collection, Unlocks, Bestiary, Credits, Quick Start | Main_menu | UI | Start, Power Up, Options, Quit | PARTIAL |  |
| Menu | Stage select | Stages | UI | One run | DIFFERS-OWNER |  |
| Collection | Items seen, '?' unknown | Collection | UI | None | MISSING |  |
| Bestiary | Defeated enemies list | Ars_Gouda | UI | None | MISSING |  |
| Achievements | Unlocks via achievements | Achievements | Persistence | AchievementManager unused | PARTIAL | Wire or remove |
| Secrets | Secrets menu | Secrets | UI | - | N/A |  |
| Story | - | - | UI | Story cards, victory line (GDD) | DIFFERS-OWNER |  |
