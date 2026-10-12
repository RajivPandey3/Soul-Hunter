# Vampire Survivors DOT table vs Soul Hunter

Built 2026-09-30 from raw vampire.survivors.wiki pages (fetched that day) and the Soul Hunter code at commit 0f9a73cd.
Re-checked 2026-10-12: every row's "Soul Hunter now" and Status were checked again in the code (working tree after c284f5fa). The "VS rule" column is still the 2026-09-30 wiki reading; the pages were not fetched again, except that the Stalker row now describes the Stalker XL the owner chose.
Status: MATCH, PARTIAL, MISSING, DIFFERS-OWNER (an owner decision), SH-ONLY (not in VS, no owner decision), N/A, UNKNOWN.

Counts: MATCH 129, PARTIAL 26, MISSING 7, DIFFERS-OWNER 10, UNKNOWN 2, N/A 2
On 2026-09-30 the rows counted MATCH 76, PARTIAL 59, MISSING 26, DIFFERS-OWNER 8, UNKNOWN 3, SH-ONLY 2, N/A 2.

## Top gaps

1. **Missing content.** Peachone and Clock Lancet have no evolution (no Ebony Wings, Silver/Gold Ring); no VS unions and no second gun; no Stone Mask.
2. **Missing systems.** Chest-less wave bosses (8:00, 17:00); pushing frozen enemies; coffins; item and weapon unlocks (for example 20 light sources for the Fire Wand).
3. **Locked PowerUps.** Omni, Charm, Defang and Preserve cannot be unlocked in play; only Banish rank 1 can be earned with today's 67 Collection entries.
4. **Map events.** Formations use a fixed radius that is inside a 16:9 view at the sides; swarms push nothing; no Flower Wall ring; Omens leave out about a dozen VS events.
5. **Character select.** One bonus colour, Magnet shown, no Reroll/Skip/Banish rows, Soul Egg bonuses not shown.
6. **Stage items past the slots.** Only the Metaglios; any other stage item with full slots pays 10 gold.
7. **Smaller rules.** Revive screen has no Quit; Limit Break does not leave out Laurel; Runetracer and Song of Mana hit intervals; Levels 2-10 chest rows; Endless damage cap; Inverse alternate music; starting spawns.
8. **Menus and save.** No pause Map, Quick Start, Delete Save or Data Recovery; AchievementManager is unused code.

## Owner decisions

- Decided 30 Sep: 5% rule = numbers ±5%; dash without damage; invented unions removed; gems 20/90; revival gold stacks; Shadow Kael's Clock Lancet slows, Pentagram erases items (all built).
- Decided 30 Sep: the VS unlock system (Phase 3).
- Decided 30 Sep, not built yet (12 Oct): Ebony Wings, Silver/Gold Ring, Stone Mask and the second gun.
- Decided 9 Oct: where the wiki is silent, the balance method decides; the Stalker follows the latest VS version (XL); all run modes are open from the start; 18 Arcanas have own names and four keep theirs; names stay as they are.
- Decided 12 Oct (phone only): the HUD is one thin strip under the XP bar, the stage name shows for its first seconds, Pause is a symbol, the floating stick takes the whole width when the phone is upright, and the hero slides along obstacles. The wiki does not describe these; they are not rows of this table.
- Open: Sort inside FILTER as in VS, or separate tabs; an Orientation option in Options > Display on phones; item slots on the phone's pause screen.

## Player & stats

| System | VS rule | Wiki | DOT | Soul Hunter now | Status | Gap / action |
|---|---|---|---|---|---|---|
| XP curve | 5 XP for level 2, then +10 per level to 20, +13 to 40, +16 after; +600 at level 20, +2400 at level 40 | Level_up | Data | Same curve in 10-XP gems (PlayerExperience.GemsToNextLevel) | MATCH |  |
| XP curve | At levels 20 and 40 the player gets +100% Growth (removed at 21 and 41) to offset the extra XP | Growth | Behavior | PlayerExperience.GrowthBonusChange: +100% Growth at 20 and 40, removed at 21 and 41 | MATCH |  |
| Level-up | 4th choice with chance 1 - 1/Luck | Level_up | Behavior | LevelUpManager.ChoiceCountForLuck | MATCH |  |
| Level-up | Rarity-weighted draw; two owned-item checks at 1 + 0.3x - 1/Luck | Level_up | Behavior | Built 2026-09-28 (WeaponDefinition, UpgradeRarity) | MATCH |  |
| Level-up | Items thrown away by a Reroll cannot come back in the new set | Reroll | Behavior | LevelUpManager.Reroll leaves the shown items out of the new set | MATCH |  |
| Level-up | Everything maxed: Gold or Chicken; an 'always' option after 100 Big Coin Bags | Level_up | UI | MaxedRewards: Always Gold / Always Chicken cards after 100 Big Coin Bags | MATCH |  |
| Level-up | Brief invulnerability after the pick (no length given) | Level_up | Timing | 0.24 s (HealthController.VsInvulnerabilityTime) | MATCH |  |
| Skip | Skip gives XP (Tips: 20% of the next level) | Skip | Behavior | SkipExperienceShare 0.2 | MATCH | Source is thin (Tips only) |
| Reroll/Skip/Banish | Base 0; +2 per PowerUp rank; eggs +0.1 / +0.2 / +0.2 | Reroll, Skip, Banish | Data | LevelUpManager: start 0, PowerUps + eggs | MATCH |  |
| Banish | Banished owned item keeps its rank; chests stop upgrading it | Banish | Interaction | _banishedTypes used by level-ups and chests | MATCH |  |
| Stat caps | Might 1000%, Area 1000%, Speed 500%, Duration 500%, Amount 10 | Player_stats, Might, Area ... | Edge case | PlayerStats keeps raw values and gives capped Might, Area, Speed, Duration, Amount (and Armor 50) | MATCH |  |
| Cooldown | Base 100%, floor 10%, additive | Cooldown | Data | Mathf.Max(0.1f, ...) in PlayerStats | MATCH |  |
| Armor | -1 damage per point, at least 1 per hit; retaliation +10%/point up to 50 | Armor_(stat) | Behavior | HealthController.TakeDamage; CrimsonShroud clamps 0-50 | MATCH |  |
| Max Health | Base 100; character bonus flat; PowerUps and Hollow Heart multiply | Max_Health | Data | BaseMaxHealth + x1.1^rank | MATCH |  |
| Recovery | Base 0 HP/s; each 0.1 adds +10% to pickup healing | Recovery | Behavior | PlayerRecovery, HealWithRecovery | MATCH |  |
| Magnet | (30 + eggs) x character x PowerUp, all multiplied | Magnet | Data | PlayerStats.Magnet = base (with eggs) x character x PowerUp x items; 4-unit base radius | MATCH |  |
| Move Speed | Base 100%, additive; stage bonus multiplies | Move_Speed | Data | PlayerController.MoveSpeed x stage speed | MATCH |  |
| Luck | No cap: 4th choice, chests, light sources, crits, owned offers; event chance / Luck | Luck | Behavior | Read by level-up, chest, lantern, crit and spawner | MATCH |  |
| Growth / Greed | Base 100%, additive; Growth x gem XP; stage gold bonus multiplies Greed | Growth, Greed | Behavior | AddXP x ExpBonus; GoldRewards.ApplyGreed | MATCH |  |
| Revival | Revive at 50% Max Health with long i-frames; the heal counts as healing | Revival | Behavior | ReviveFromDeath: 50%, 1.5 s + 0.24 s on game time, fires the heal hooks | MATCH |  |
| Revival | Death shows a Revive button (or Quit), then an animation | Revival | UI | ReviveScreen: a lethal hit with a Revival left pauses and shows a Revive button | PARTIAL | No Quit option on that screen; the revive effect is a placeholder (level-up + heal) |
| Hit i-frames | Invulnerability Time 240 ms after a hit (cap 1 s) | Invulnerability_Time | Timing | 0.24 s after a hit; nothing in SH raises the stat, so the 1 s cap is not coded | MATCH |  |
| Hit i-frames | Floor Chicken also gives invulnerability (length not given) | Invulnerability_Time, Pickups | Behavior | HealthPickup grants 0.24 s after the heal | MATCH |  |
| Controls | Movement only; weapons fire on their own | Weapons | Input | Move + Dash + Soul Burst (from the owner's GDD) | DIFFERS-OWNER |  |
| Controls | (VS has no dash) | - | Behavior | Dash only moves and avoids damage (GDD); stops at walls. Fixed 2026-09-30 | DIFFERS-OWNER | Owner decision: dash from the GDD, no damage |
| Stats panel | Stats visible on character select, pause and level-up | Player_stats | UI | StatsPanel on the pause menu and the level-up screen (19 live rows) | MATCH |  |
| Character select | White = character/egg values, yellow = PowerUps; Reroll/Skip/Banish rows; Magnet hidden | Characters, Magnet | UI | One bonus colour for every changed row; 16 rows with Magnet, without Reroll/Skip/Banish; Soul Egg bonuses not shown | PARTIAL | Split colours by source, add the three rows, hide Magnet, show egg bonuses |
| Characters | Starting weapon and stat bonuses | Characters | Data | CharacterData, all 13 checked | MATCH |  |
| Characters | Per-level bonuses are capped (Antonio +50% Might at 50; Divano +5 Armor at 25) | Antonio_Belpaese, Armor_(stat) | Edge case | CharacterData.LevelBonusUntilLevel: Corvan 50, Valerius 25 | MATCH |  |
| Characters | Price rises +10% per character bought | Characters | Persistence | CharacterUnlocks.PriceFor: +10% of the base per paid character owned, rounded down | MATCH |  |
| Characters | Sort (default, A-Z, stages) and favourites | Characters | UI | Sort Default / A-Z / levels completed, ascending or descending; favourites first, unlocked at 50 Collection entries | MATCH |  |
| PowerUps | 23 PowerUps, VS ranks, prices, fees, Refund | PowerUps | Data | PowerUps.cs, PowerUpShopTests | MATCH |  |
| PowerUps | A maxed PowerUp can be switched off | PowerUps | UI | PowerUps.CanToggle / SetEnabled / ActiveRank; Buy becomes Turn Off / Turn On (not Reroll, Skip, Banish) | MATCH |  |
| PowerUps | Reroll/Skip/Banish/Omni/Charm/Defang ranks unlock by achievements | Reroll, Skip, Banish | Behavior | PowerUpUnlocks: Reroll (level 80 with five characters), Skip (survival in Levels 6-10), Banish (60-100 Collection entries) | PARTIAL | Omni, Charm, Defang and Preserve cannot be unlocked in play; with 67 Collection entries only Banish rank 1 can be earned |

## Weapons & level-up

| System | VS rule | Wiki | DOT | Soul Hunter now | Status | Gap / action |
|---|---|---|---|---|---|---|
| Slots | 6 weapons, 6 passives | Weapons, Passive_items | Data | WeaponManager 6 / 6 | MATCH |  |
| Slots | Stage items can go past the 6 slots | Weapons, Passive_items | Edge case | Only Metaglio Left/Right go past the slots (AcquireStagePassive); any other stage item with no free slot pays 10 gold | PARTIAL | Other stage passives and weapons past the slots |
| Slots | Each item once per run (Tirajisu can come back) | Weapons | Behavior | Level-0 check; Tirajisu release | MATCH |  |
| Level-up | Level-up cards show discovered evolutions (0.3.2) | Level_up | UI | LevelUpUI evolution hints for recipes saved by EvolutionDiscovery | MATCH |  |
| Level-up | Skip and Reroll unavailable when the inventory is maxed | Level_up | Edge case | CanReroll / CanSkip are false on the gold/chicken and Limit Break cards; the buttons hide | MATCH |  |
| Evolution | Max weapon + passive held + chest | Evolution | Behavior | RequirementsMet, passive level >= 1 | MATCH |  |
| Evolution | Whip, Wand, Knife, Axe, Cross, Bible, Garlic evolutions | Evolution | Data | All 7 pairs correct | MATCH |  |
| Evolution | Fire Wand+Spinach, Santa Water+Attractorb, Runetracer+Armor, Lightning Ring+Duplicator, Pentagram+Crown, Song of Mana+Skull O'Maniac | Evolution | Data | Six recipes with own names: Hellmaw Comet, Drowning Chalice, Severed Fate, Stormbound Halo, Eclipse Sigil, Requiem of Souls | MATCH |  |
| Evolution | Laurel + Metaglio Left + Right = Crimson Shroud | Evolution, Metaglio_Left | Behavior | Both Metaglios are stage items on every stage; Crimson Shroud needs Laurel 7 + both at 9 (chests level a found Metaglio; level-ups never offer it) | MATCH |  |
| Evolution | Peachone+Ebony Wings; Clock Lancet+Silver/Gold Ring | Evolution | Data | SH has no Ebony Wings or Rings | MISSING | Owner: add counterparts or accept |
| Evolution | Only evo chests (10:00+, MF 1:00); one evo per chest, first reward | Evolution | Timing | CanEvolveAt, one per chest | MATCH |  |
| Evolution | Base weapon removed, passive kept | Evolution | Behavior | DisableOldWeapon | MATCH |  |
| Union | All base weapons max -> one slot (e.g. Phiera+Eight+Tirajisu) | Evolution#Union | Data | 6 invented unions removed 2026-09-30; system kept | MISSING | VS unions come with the second gun (Phase 2) |
| Upgrade | Levelling a weapon resets its cooldown and fires it at once | Weapons | Timing | WeaponManager calls ResetCooldownAndFire when an owned weapon gains a level | MATCH |  |
| Stats | Might, Area, Duration, Speed, Cooldown add together | Might, Area ... | Data | PlayerStats additive | MATCH |  |
| Crit | Base Whip/Knife/Axe crit only with the Slash Arcana | Whip, Knife, Axe, Slash_(XVI) | Behavior | BaseCritChance 0; Whip, Knife and Axe crit only with the Slash card (Keen Edge) | MATCH |  |
| Crit | Per-weapon crit: Heaven Sword 10%/2.5x, Bloody Tear 10%/2x | Heaven_Sword, Bloody_Tear | Data | Heaven Sword 10% / 2.5x (prefab), Bloody Tear 10% / 2x | MATCH |  |
| Hit interval | King Bible 1.7 s, Santa Water 0.5, Runetracer 0.5, Song of Mana 1.0 | Weapon pages | Timing | Bible 1.7 s hitbox delay, Santa Water 0.5 s | PARTIAL | Runetracer has no 0.5 s per-enemy interval; Song of Mana is a flying shot with no 1.0 s interval |
| Knockback | Knife 0.5, Gun 0.1, Peachone 2, Garlic 0 | Knockback, weapon pages | Data | Knife 0.5, Gun 0.1, Peachone 2, Garlic 0 (AutoAttackWeapon.Knockback overrides) | MATCH |  |
| King Bible | 3 s on, then off until cooldown | King_Bible | Timing | BibleWeapon: books out for 3 s x Duration, then hidden until Cooldown + base Duration | MATCH |  |
| Pentagram | 1 s duration; erases ground items | Pentagram | Behavior | Glyph armed 1 s; one keep roll, then GroundItemEraser erases the items on screen | MATCH |  |
| Cherry Bomb | Explode chance 40% (60% max) | Cherry_Bomb | Behavior | ExplodeChance 0.4, 0.5 from level 3, 0.6 from level 6, x Luck | MATCH |  |
| Peachone | 4 s duration | Peachone | Timing | An activation bombards for 4 s x Duration, one set per second | MATCH |  |
| Aiming | Whip horizontal, Clock Lancet 12 directions, etc. | Weapon pages | Behavior | All aim at nearest enemy | DIFFERS-OWNER |  |
| Guns | Phiera and Eight The Sparrow are two weapons | Phiera_Der_Tuphello | Data | One 'Gun' | PARTIAL | Owner: second gun? |
| Limit Break | Random per-stat cards with caps; reset on evo; not Laurel | Limit_Break | Behavior | LimitBreak.Table: eight per-stat rows with rarities and caps; bonuses belong to the live weapon, so an evolution starts clean | PARTIAL | Laurel is not left out; Area, Speed and Amount are offered to every weapon; weapon-specific values not used |
| Arcana | Pick an Arcana at start (or random) | Arcanas | UI | Arcana mode: ArcanaChoiceScreen with every card plus Random | MATCH |  |
| Arcana | 11:00/21:00 chest: pick 1 of 4 (6), Reroll; normal chest at 3 | Arcanas | UI | Arcana chest: draft of 4 (6 with a free Reroll at 23+), Reroll and Skip; a regular chest at 3 Arcanas | MATCH |  |
| Arcana | 22 Arcanas; e.g. Awake: bonuses on each revival | Arcanas, Awake_(IV) | Data | 22 cards, each handled in ArcanaManager / ArcanaEffects; Awake: +3 Revivals and its bonus on every revive | MATCH |  |
| Arcana | - | - | Data | 18 cards have own names; Awake, Gemini, Iron Blue Will and Waltz of Pearls keep theirs (owner, 2026-10-09) | DIFFERS-OWNER | The seven older evolutions also still show VS names (owner: names stay) |

## Enemies & run clock

| System | VS rule | Wiki | DOT | Soul Hunter now | Status | Gap / action |
|---|---|---|---|---|---|---|
| Spawning | Enemies spawn just outside the screen | Enemies | Behavior | Wave enemies, chest elites, bosses and stragglers appear 1.5-4.5 units past the camera edge | PARTIAL | Map-event formations use a fixed radius (15-20), inside a 16:9 view at the sides; Death's Shadow a fixed 18 |
| Spawning | Per-minute waves: minimum + interval | Enemies, Mad_Forest | Behavior | WaveData.Minutes | MATCH | Numbers are SH's curve |
| Spawning | Above the minimum: one of each wave type per tick | Enemies | Behavior | One of each wave type per tick; every minute of all 10 wave assets lists its types | MATCH |  |
| Spawning | 300 alive: only bosses and events spawn | Enemies | Edge case | _maxAliveEnemies 300 | MATCH |  |
| Spawning | Hard cap 500 whatever Curse/Charm | Curse | Edge case | EnemySpawner.HardCap 500 stops map events; Curse and Charm only raise the minimum (clamped to 300) | MATCH |  |
| Spawning | Starting spawns (MF 10) | Mad_Forest | Timing | No starting-spawn value; the minute-0 minimum (15 on Level 1) is filled at once | PARTIAL | A per-stage opening burst; Hyper's larger starting spawns |
| Spawning | Far enemies can despawn | Enemies | Behavior | Enemies beyond 40 units are moved back to just past the screen edge, keeping their health | PARTIAL | Real despawning of far enemies (the wiki gives no distance) |
| Bosses | Bosses teleported back, never despawn | Enemies | Behavior | Included in relocation | MATCH |  |
| Bosses | Stage boss at 25:00 | Mad_Forest | Timing | 25:00, does not end stage | MATCH |  |
| Bosses | HP x Level at spawn | Enemies | Data | Wave enemies, chest elites and bosses use EnemyData.HealthForPlayerLevel at spawn | MATCH |  |
| Bosses | Resist freeze, instant kill, debuff, knockback | Enemies | Data | All bosses | MATCH |  |
| Chest elites | MF minutes 1,3,5,7,9,10,12,14,15,16,18,20,22,23,25,27,29; Arcana 11, 21 | Mad_Forest | Timing | DefaultChestEliteMinutes | MATCH |  |
| Mini-bosses | Chest-less wave bosses (MF 8:00, 17:00) | Mad_Forest | Data | None | MISSING | Needs models |
| Chest elites | Chest chance and tier per wave row | Enemies, Mad_Forest | Data | ChestTiers.ForMinute gives each chest elite its Mad Forest row; three separate Luck rolls | PARTIAL | Levels 2-10 use Mad Forest's rows, not their source stage's (balance method: the owner fixed the minutes) |
| Enemy stats | Per-enemy HP, power, speed, kb, XP (Pipeestrello: 1 HP, 5 dmg, 1 XP) | Enemies, Pipeestrello | Data | Per-type health, damage, speed, knockback and XP x10 for Level 1 (16 types) and Levels 2-10 (235 types) | MATCH |  |
| Knockback | Reverse 120 ms x weapon kb x enemy kb; Garlic +0.3 to 3 | Knockback | Behavior | EnemyController.ApplyKnockback | MATCH |  |
| Resistances | Freeze res vs weapon chance; Orologion ignores | Freeze | Interaction | TryWeaponFreeze | MATCH | Update citation |
| Resistances | Instant kill and debuff resistance | Enemies | Interaction | ResistsInstantKill, EnemyResistance | MATCH |  |
| Movement | Fixed Direction and Floaty | Enemies | Behavior | EnemyMovement | MATCH |  |
| Freeze | Player can push frozen enemies | Freeze | Interaction | Enemy bodies are triggers and a frozen enemy's velocity is zeroed each step; nothing pushes it | MISSING | Pushing frozen enemies |
| Curse | Health at once; other Curse effects from the next minute | Curse | Timing | Health uses the Curse now; speed, interval and minimum use the Curse sampled when the stage minute began | MATCH |  |
| Curse | Curse x health, speed, minimum; interval / Curse | Curse | Data | EnemySpawner | MATCH |  |
| Map events | Timed events with repeat and delay | Enemies | Timing | StageEventEntry | MATCH |  |
| Map events | Chance = eventChance / Luck per repeat | Enemies | Behavior | EventFires | MATCH |  |
| Map events | Swarm pushes enemies; Flower Wall lasts 30/60 s | Mad_Forest | Behavior | Event fliers live 15 s and push nothing; Thorn Wall exists only as a slow wave type | PARTIAL | Swarms pushing other enemies; a Flower Wall ring event lasting 30/60 s |
| Traps | Global trap cooldown x Luck | Enemies | Behavior | PressurePlate.GlobalReadyAt: one cooldown for all plates, 20 s x Luck | MATCH |  |
| Death | Kills give XP gems; >400 merge | Experience_Gem | Behavior | EnemyDrop, cap 400 | MATCH |  |
| Contact | Contact damage 'multiple times per second' | Armor_(stat) | Timing | 0.5 s per enemy | UNKNOWN | Wiki silent |
| Run clock | 30:00, then the Reaper | Stages, The_Reaper | Timing | Next stage at 30:00; L10 no limit | DIFFERS-OWNER |  |
| Stalker | Stalker XL (the latest): 655,350 x Level, 50 damage, only instant kills defeat it; event chance | The_Stalker | Behavior | Death's Shadow follows the VS Stalker XL (owner, 2026-10-09: the latest version): 655,350 x level, 50 damage, 30% / Luck at 8:00 and 12:00 on Stages 5 and 9; only instant kills defeat it | PARTIAL | The 0:00 1% event and the chests' extra evolutions |
| Shooting Star | Markers, 2 s, 30 damage | Shooting_Star | Timing | SoulMeteor | MATCH |  |
| Clock Lancet | Freeze, no damage | Clock_Lancet | Behavior | ClockLancetWeapon; boss copy slows the player 50% (owner exception) | MATCH |  |

## Pickups & economy

| System | VS rule | Wiki | DOT | Soul Hunter now | Status | Gap / action |
|---|---|---|---|---|---|---|
| XP gems | Blue up to 2 XP, green up to 9, red above | Experience_Gem | Data | Up to 20 / up to 90 / above (x10 for 10-XP gems, owner 2026-09-30) | MATCH |  |
| XP gems | 400 on the ground, XP merges into one red gem | Experience_Gem | Performance | MaxGemsOnGround 400 | MATCH |  |
| Magnet | Gems pulled in the Magnet radius | Magnet | Behavior | PlayerExperience 4 u x Magnet | MATCH |  |
| Magnet | Magnet also collects other pickups | Magnet, Pickups | Behavior | The Magnet sweep pulls every Pickup (coins, bags, Time Freeze, Vacuum, charms) and chickens | MATCH |  |
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
| Orologion | 10 s; new spawns also frozen | Orologion | Behavior | 10 s; EnemySpawner freezes new spawns for the time left | MATCH |  |
| Orologion | Some enemies resist | Orologion | Edge case | Ignores resistance | PARTIAL | Wiki conflicts |
| Orologion | Halves Gold Fever decay | Orologion | Interaction | GoldFever ticks at half speed while the world is frozen | MATCH |  |
| Rosary | Kills screen; some immune | Rosary | Behavior | Soul Toll | MATCH |  |
| Rosary | Can kill Stalker, which drops a chest | Rosary | Edge case | Soul Toll defeats Death's Shadow, which drops a high-quality chest | MATCH |  |
| Nduja | 10 s fire: 30 dmg, 12, 0.5 s, area 200% | Nduja_Fritta_Tanto | Data | FireBreath | PARTIAL | Area 200% not mapped |
| Nduja | Fires where the hero faces | Nduja_Fritta_Tanto | Input | Nearest enemy | DIFFERS-OWNER |  |
| Clovers | Little +10% Luck; Gilded: gold fever | Little_Clover, Gilded_Clover | Data | Luck Charm, GoldFever | MATCH |  |
| Chest count | 5 -> 3 -> 1, each x Luck, per boss | Treasure_Chest | Data | Three separate rolls (5, 3, 1), each x Luck, against the boss's ChestTiers row | MATCH |  |
| Chest count | First six: 1-1-3-1-1-5 | Treasure_Chest | Persistence | FirstChestSequence | MATCH |  |
| Chest contents | Chests level up owned items only (0.2.7) | Treasure_Chest | Behavior | Chests draw only owned items; nothing left pays a Big Coin Bag | MATCH |  |
| Chest gold | 100-200 / 300-600 / 500-1000 x Greed | Treasure_Chest | Data | ChestLogicController | MATCH |  |
| Chest maxed | Each item -> Big Coin Bag 25 | Treasure_Chest | Edge case | GoldPerMaxedItem 25 | MATCH |  |
| Chest evolution | From 10:00 (MF 1:00); one per chest | Evolution | Behavior | CanEvolveAt | MATCH |  |
| Chest look | Bronze / silver (evo) / purple Arcana | Treasure_Chest | VFX | ChestLook: bronze, silver or violet (Arcana) bands | MATCH |  |
| Chest i-frames | Brief invulnerability after a chest (0.2.4) | Treasure_Chest | Behavior | ChestPickup grants 0.24 s on pickup | MATCH |  |
| Chest screen | Blast, OPEN, reels, count-up, lights, fireworks, jingles | Treasure_Chest | UI | ChestOpeningScreen | MATCH |  |
| Chest skip | Skip rules | Treasure_Chest | Input | ChestSkipRules: first 6 unskippable, 5-item chests after 50 of them or 500 chests, coin-only chests skip themselves | PARTIAL | The third unlock ("seen the final fireworks") has no Soul Hunter counterpart |
| Greed | Additive; stage bonus separate | Greed | Data | ApplyGreed | MATCH |  |
| Greed | Stone Mask passive | Greed | Data | No SH item | MISSING | Owner decision |
| Stage gold | MF 1.0 ... CM 1.4; Hyper +50% | Stages | Data | GoldBonus | MATCH |  |
| Merchant | Moongolow / every stage later | Merchant | Behavior | Level 3 only | DIFFERS-OWNER |  |
| Merchant | Touch opens shop, one visit | Merchant | Interaction | WanderingMerchant | MATCH |  |
| Merchant | Bone, Cherry Bomb 1,000; Golden Egg 10,000 | Merchant | Data | Same | MATCH |  |
| Merchant | Endless: Revival ware each cycle | Stages | Behavior | Endless: "Second Breath" (+1 Revival, 10,000, once per visit) in place of the Soul Egg | MATCH |  |
| Merchant | Inverse: Skip/Banish/Reroll, Arcana | Merchant | Data | Inverse: Skip 100, Banish 500, Reroll 1,000 (cap 20) and Draw a Fate 20,000 (Arcana choice) | MATCH |  |
| Golden Egg | 19 stats, caps, next run | Golden_Egg | Persistence | SoulEggs | MATCH | Magnet +1% stand-in |
| Stage items | Extra copies with Luck | Mad_Forest | Data | 4 fixed items | PARTIAL |  |

## Stages, modes & menus

| System | VS rule | Wiki | DOT | Soul Hunter now | Status | Gap / action |
|---|---|---|---|---|---|---|
| Stage | 30:00 limit | Stages | Timing | LevelProgressionManager | MATCH |  |
| Stage | Stage bonuses: speed and gold | Stages | Data | PlayerSpeedFor / GoldFor | MATCH | L6-10 SH mapping |
| Stage | Stage + mode bonuses summed, then multiply | Stages | Data | CurrentGold adds the stage and mode gold bonuses, then multiplies by Greed | MATCH |  |
| Stage | Clear: 500, then +100/+200/+300/+400 per unused revival, then +100 | The_Reaper, Revival | Data | StageClearGold stacks (owner chose these pages, 2026-09-30) | MATCH |  |
| Stage | Confined stages (corridors) | Stages | Behavior | L4 corridor | MATCH |  |
| Stage | Trap plates trigger events | Stages | Interaction | Rune Plates | MATCH |  |
| Stage | Pre-placed items and relics | Stages | Data | A passive, a weapon, a chicken, a Magnet or Time Freeze, and both Metaglios on every stage | PARTIAL | No relics; Metaglio guards not built |
| Stage | Coffins unlock characters | Stages | Interaction | Condition unlocks | MISSING | Owner call |
| Unlocks | 20 light sources unlock Fire Wand | Light_source | Persistence | No item unlocks | MISSING |  |
| Mode | Hurry: clock x2, XP +25% | Stages | Timing | RunModes | MATCH |  |
| Mode | Hyper: per-stage values | Stages | Data | Per-stage Hyper tables for player speed, projectile speed, Luck and enemy speed | MATCH |  |
| Mode | Inverse: gold, Luck, enemy HP/speed | Stages | Data | Gold +200%, Luck +20%, enemy health x(3 + 0.05 per minute), enemy speed +0.5% of the stage pace per minute | MATCH |  |
| Mode | Inverse: flipped darker map, alt music | Stages | UI | Stage items, Metaglios and Rune Plates turn 180 degrees; the floor darkens and shifts hue; "Visually Invert Stages" option | PARTIAL | Alternate Inverse music; the floor itself is not rotated |
| Mode | Endless cycles; +100% HP, +50% spawn, +25% dmg, damage cap -1 | Stages | Behavior | Multipliers match; no damage cap | PARTIAL |  |
| Mode | Random Events | Random_Events | Behavior | Omens: RandomEventDirector picks at XX:30 and fires at XX:00 from 16 events | PARTIAL | About a dozen VS events are left out (they need VS-only enemies or hazards); equal weights; no pick at 29:30 |
| Mode | Random LevelUp | Stages | Behavior | Blind Fate: each level-up picks a random card with no screen; toggled in the pause menu | MATCH |  |
| Mode | Modes unlock via relics | Stages | Persistence | All modes open from the start (owner, 2026-10-09) | DIFFERS-OWNER |  |
| Audio | Track per stage; Music Selection | Music | Audio | A track per stage, or the Music selection: 11 tracks, Normal/Faster/Slower, a Keep lock | MATCH |  |
| HUD | XP bar at top | Level_up | UI | XPBarUI | MATCH |  |
| HUD | 6 + 6 slots | Passive_items | UI | InventoryHud | MATCH |  |
| HUD | Timer, kills, gold, health bar | - | UI | Built | UNKNOWN | Wiki silent |
| Pause | Options; Grimoire; Map | Options, Grim_Grimoire | UI | Resume, Options, Evolutions (the Grimoire, open from the start), Blind Fate, Quit, and the stats panel | PARTIAL | The pause Map |
| Pause | Quit still shows results | Results | Behavior | Quit ends the run into Results (`GameSessionManager.QuitRun`) | MATCH | Level 1 step 4 |
| Results | Stage, mode, gold bonus, time, gold, level, kills | Results | UI | `RunResultsScreen` top left | MATCH | Level 1 step 4; run time over all stages |
| Results | Per weapon: level, damage, time, DPS | Results | UI | Every hit credited to its weapon (`DamageSource`); SI prefixes; top damage/DPS yellow | MATCH | Level 1 step 4; damage counts overkill (wiki silent) |
| Results | Character, eggs, items, PowerUps, pickups, Arcanas; unlocks toggle; DONE | Results | UI | All built; DONE to the main menu | MATCH | Level 1 step 4; pickup names are Soul Hunter's, gems not listed (wiki silent); no mobile ad button |
| Options | Favorites, Display, Sound, Gameplay | Options | UI | Tabs Favorites, Display, Sound, Gameplay (and Controls); Favorites copies four common options | MATCH |  |
| Options | Display incl. Refresh Rate | Options | UI | Screen Refresh Rate row, shown only in Exclusive Fullscreen | MATCH |  |
| Options | Gameplay options | Options | UI | Joystick Type and Visually Invert Stages beside Damage Numbers, Show Stage Progression, Visible Joystick | MATCH |  |
| Options | Language, co-op, Twitch ... | Options | UI | - | N/A |  |
| Save | Delete Save, Data Recovery | Options | Persistence | Atomic saves with .prev and .bak, restored automatically | PARTIAL | No player-facing Delete Save or Data Recovery |
| Menu | Collection, Unlocks, Bestiary, Credits, Quick Start | Main_menu | UI | Collection, Unlocks, Credits and Bestiary on the main menu | PARTIAL | Quick Start and PRESS TO START (owner decision) |
| Menu | Stage select | Stages | UI | One run | DIFFERS-OWNER |  |
| Collection | Items seen, '?' unknown | Collection | UI | CollectionBook / CollectionTracker; "?" for unknown entries | MATCH |  |
| Bestiary | Defeated enemies list | Ars_Gouda | UI | Bestiary: kill counts per enemy type, names hidden until the first kill; opens at Level 3 | MATCH |  |
| Achievements | Unlocks via achievements | Achievements | Persistence | Unlock ids saved by CharacterUnlockTracker, PowerUpUnlocks and CollectionBook, shown in the Unlocks menu | PARTIAL | AchievementManager is still unused code; nothing unlocks weapons, items or stages |
| Secrets | Secrets menu | Secrets | UI | - | N/A |  |
| Story | - | - | UI | Story cards, victory line (GDD) | DIFFERS-OWNER |  |
