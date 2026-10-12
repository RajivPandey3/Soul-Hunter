# Vampire Survivors DOT table vs Soul Hunter

Built 2026-09-30 from raw vampire.survivors.wiki pages (fetched that day) and the Soul Hunter code at commit 0f9a73cd.
Re-checked 2026-10-12: every row's "Soul Hunter now" and Status were checked again in the code (working tree after c284f5fa). The "VS rule" column is still the 2026-09-30 wiki reading; the pages were not fetched again, except that the Stalker row now describes the Stalker XL the owner chose.
Status: MATCH, PARTIAL, MISSING, DIFFERS-OWNER (an owner decision), BALANCE (the wiki is silent or contradicts itself, so the balance method decided: owner rule 2026-10-09), SH-ONLY (not in VS, no owner decision), N/A, UNKNOWN.

Counts (13 Oct, after the new items, unlocks, caskets, relics, map and menu work): MATCH 147, PARTIAL 4, MISSING 0, DIFFERS-OWNER 12, BALANCE 9, UNKNOWN 2, N/A 2
After the open-row pass of 12 Oct: MATCH 137, PARTIAL 10, MISSING 5, DIFFERS-OWNER 12, BALANCE 8, UNKNOWN 2, N/A 2.
On 2026-10-12 before that pass: MATCH 129, PARTIAL 26, MISSING 7, DIFFERS-OWNER 10, UNKNOWN 2, N/A 2.
On 2026-09-30 the rows counted MATCH 76, PARTIAL 59, MISSING 26, DIFFERS-OWNER 8, UNKNOWN 3, SH-ONLY 2, N/A 2.

## Top gaps

As of 13 Oct no row is MISSING. What is left:

1. **Level 5's 24:00 chest-less boss.** A stand-in type with 100 x Level health and no resistances (VS 200 x Level); it needs its own enemy type.
2. **Guards.** The Metaglios, the two Rings and the Idol lie unguarded (VS places guardian enemies around them).
3. **Omens.** Seven VS events are left out because they need VS-only enemies or hazards.
4. **Banish ranks.** Ranks above the Collection's size cannot be earned yet.
5. **Art.** The eight new items, the relics, the Soul Caskets and the map markers use placeholders; none is owner-approved.
6. **Owner approval pending.** The nine-button main menu, Quick Start, the start prompt, Peachone circling clockwise, and the story lines' place on screen.

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
| Revival | Death shows a Revive button instead of the Quit button, then an animation | Revival | UI | ReviveScreen: a lethal hit with a Revival left pauses and shows a Revive button | MATCH | Re-read 12 Oct: the wiki says the Revive button comes "instead of" Quit, so no Quit there. The revive effect is still a placeholder (art, owner) |
| Hit i-frames | Invulnerability Time 240 ms after a hit (cap 1 s) | Invulnerability_Time | Timing | 0.24 s after a hit; nothing in SH raises the stat, so the 1 s cap is not coded | MATCH |  |
| Hit i-frames | Floor Chicken also gives invulnerability (length not given) | Invulnerability_Time, Pickups | Behavior | HealthPickup grants 0.24 s after the heal | MATCH |  |
| Controls | Movement only; weapons fire on their own | Weapons | Input | Move + Dash + Soul Burst (from the owner's GDD) | DIFFERS-OWNER |  |
| Controls | (VS has no dash) | - | Behavior | Dash only moves and avoids damage (GDD); stops at walls. Fixed 2026-09-30 | DIFFERS-OWNER | Owner decision: dash from the GDD, no damage |
| Stats panel | Stats visible on character select, pause and level-up | Player_stats | UI | StatsPanel on the pause menu and the level-up screen (19 live rows) | MATCH |  |
| Character select | White = character/egg values, yellow = PowerUps; the list ends with Reroll/Skip/Banish; Magnet is in the list | Characters, Magnet | UI | CharacterStatSheet.SelectRows: character and Soul Egg values in the plain colour, PowerUp values in the highlight; Soul Egg bonuses included; Reroll/Skip/Banish rows once the character starts with any (12 Oct) | MATCH | Re-read 12 Oct: the page lists Magnet among the shown stats (one sentence on the Magnet page says it is hidden; the list and two tips say it is shown), so Magnet stays |
| Characters | Starting weapon and stat bonuses | Characters | Data | CharacterData, all 13 checked | MATCH |  |
| Characters | Per-level bonuses are capped (Antonio +50% Might at 50; Divano +5 Armor at 25) | Antonio_Belpaese, Armor_(stat) | Edge case | CharacterData.LevelBonusUntilLevel: Corvan 50, Valerius 25 | MATCH |  |
| Characters | Price rises +10% per character bought | Characters | Persistence | CharacterUnlocks.PriceFor: +10% of the base per paid character owned, rounded down | MATCH |  |
| Characters | Sort (default, A-Z, stages) and favourites | Characters | UI | Sort Default / A-Z / levels completed, ascending or descending; favourites first, unlocked at 50 Collection entries | MATCH |  |
| PowerUps | 23 PowerUps, VS ranks, prices, fees, Refund | PowerUps | Data | PowerUps.cs, PowerUpShopTests | MATCH |  |
| PowerUps | A maxed PowerUp can be switched off | PowerUps | UI | PowerUps.CanToggle / SetEnabled / ActiveRank; Buy becomes Turn Off / Turn On (not Reroll, Skip, Banish) | MATCH |  |
| PowerUps | Reroll/Skip/Banish/Omni/Charm/Defang ranks unlock by achievements | Reroll, Skip, Banish | Behavior | PowerUpUnlocks: Reroll (level 80 with five characters), Skip (survival in Levels 6-10), Banish (60-100 Collection entries), Omni (hold six evolved weapons at once), Charm, Defang and Preserve (relics) (13 Oct) | PARTIAL | Banish ranks above the Collection's size cannot be earned yet; the Omni condition is Soul Hunter's (VS: max Torrona's Box, which SH does not have) |

## Weapons & level-up

| System | VS rule | Wiki | DOT | Soul Hunter now | Status | Gap / action |
|---|---|---|---|---|---|---|
| Slots | 6 weapons, 6 passives | Weapons, Passive_items | Data | WeaponManager 6 / 6 | MATCH |  |
| Slots | Stage items can go past the 6 slots | Weapons, Passive_items | Edge case | StageItemPickup -> WeaponManager.AcquireStageItem: a stage passive or weapon walked over with full slots becomes an extra item (12 Oct); a maxed one pays 10 gold | MATCH | The VS pick-up-or-discard prompt is not built; the HUD row shows the first 6 |
| Slots | Each item once per run (Tirajisu can come back) | Weapons | Behavior | Level-0 check; Tirajisu release | MATCH |  |
| Level-up | Level-up cards show discovered evolutions (0.3.2) | Level_up | UI | LevelUpUI evolution hints for recipes saved by EvolutionDiscovery | MATCH |  |
| Level-up | Skip and Reroll unavailable when the inventory is maxed | Level_up | Edge case | CanReroll / CanSkip are false on the gold/chicken and Limit Break cards; the buttons hide | MATCH |  |
| Evolution | Max weapon + passive held + chest | Evolution | Behavior | RequirementsMet, passive level >= 1 | MATCH |  |
| Evolution | Whip, Wand, Knife, Axe, Cross, Bible, Garlic evolutions | Evolution | Data | All 7 pairs correct | MATCH |  |
| Evolution | Fire Wand+Spinach, Santa Water+Attractorb, Runetracer+Armor, Lightning Ring+Duplicator, Pentagram+Crown, Song of Mana+Skull O'Maniac | Evolution | Data | Six recipes with own names: Hellmaw Comet, Drowning Chalice, Severed Fate, Stormbound Halo, Eclipse Sigil, Requiem of Souls | MATCH |  |
| Evolution | Laurel + Metaglio Left + Right = Crimson Shroud | Evolution, Metaglio_Left | Behavior | Both Metaglios are stage items on every stage; Crimson Shroud needs Laurel 7 + both at 9 (chests level a found Metaglio; level-ups never offer it) | MATCH |  |
| Evolution | Peachone+Ebony Wings; Clock Lancet+Silver/Gold Ring | Evolution | Data | Dusk Raven (Ebony Wings) joins Peachone into Twilight Flock; Ring of Long Hours and Ring of Dark Hours (stage items) evolve Clock Lancet into Sundered Hour (13 Oct) | MATCH | Placeholder art; ring guards not built (see Stage row); Sundered Hour beams aim at the nearest enemy (owner aiming rule) |
| Evolution | Only evo chests (10:00+, MF 1:00); one evo per chest, first reward | Evolution | Timing | CanEvolveAt, one per chest | MATCH |  |
| Evolution | Base weapon removed, passive kept | Evolution | Behavior | DisableOldWeapon | MATCH |  |
| Union | All base weapons max -> one slot (e.g. Phiera+Eight+Tirajisu) | Evolution#Union | Data | Two unions: Twilight Flock (Peachone + Dusk Raven) and Revenant Wheel (Gun + Azure Repeater, with Tirajisu) (GameContentCatalog.Unions, 13 Oct) | MATCH | Revenant Wheel numbers the wiki leaves open are balance values; placeholder art |
| Upgrade | Levelling a weapon resets its cooldown and fires it at once | Weapons | Timing | WeaponManager calls ResetCooldownAndFire when an owned weapon gains a level | MATCH |  |
| Stats | Might, Area, Duration, Speed, Cooldown add together | Might, Area ... | Data | PlayerStats additive | MATCH |  |
| Crit | Base Whip/Knife/Axe crit only with the Slash Arcana | Whip, Knife, Axe, Slash_(XVI) | Behavior | BaseCritChance 0; Whip, Knife and Axe crit only with the Slash card (Keen Edge) | MATCH |  |
| Crit | Per-weapon crit: Heaven Sword 10%/2.5x, Bloody Tear 10%/2x | Heaven_Sword, Bloody_Tear | Data | Heaven Sword 10% / 2.5x (prefab), Bloody Tear 10% / 2x | MATCH |  |
| Hit interval | King Bible 1.7 s, Santa Water 0.5, Runetracer 0.5, Song of Mana 1.0 | Weapon pages | Timing | Bible 1.7 s hitbox delay, Santa Water 0.5 s; Runetracer and Severed Fate 0.5 s per enemy with the shared reset (HitboxDelay, 12 Oct); the rune now bounces off the screen edges | DIFFERS-OWNER | Song of Mana is an aimed flying shot (owner, 2026-09-24), so its 1.0 s delay does not apply |
| Knockback | Knife 0.5, Gun 0.1, Peachone 2, Garlic 0 | Knockback, weapon pages | Data | Knife 0.5, Gun 0.1, Peachone 2, Garlic 0 (AutoAttackWeapon.Knockback overrides) | MATCH |  |
| King Bible | 3 s on, then off until cooldown | King_Bible | Timing | BibleWeapon: books out for 3 s x Duration, then hidden until Cooldown + base Duration | MATCH |  |
| Pentagram | 1 s duration; erases ground items | Pentagram | Behavior | Glyph armed 1 s; one keep roll, then GroundItemEraser erases the items on screen | MATCH |  |
| Cherry Bomb | Explode chance 40% (60% max) | Cherry_Bomb | Behavior | ExplodeChance 0.4, 0.5 from level 3, 0.6 from level 6, x Luck | MATCH |  |
| Peachone | 4 s duration | Peachone | Timing | An activation bombards for 4 s x Duration, one set per second | MATCH |  |
| Aiming | Whip horizontal, Clock Lancet 12 directions, etc. | Weapon pages | Behavior | All aim at nearest enemy | DIFFERS-OWNER |  |
| Guns | Phiera and Eight The Sparrow are two weapons | Phiera_Der_Tuphello | Data | Gun (Phiera) and Azure Repeater (Eight The Sparrow) are two weapons (13 Oct) | MATCH | Placeholder art |
| Limit Break | Random per-stat cards with caps; reset on evo; not Laurel | Limit_Break | Behavior | LimitBreak.ProfileFor: each weapon is offered only the rows of its own wiki table, with the wiki steps and caps; Laurel gets none (12 Oct) | MATCH | Amount and Pierce caps apply to the bonus; Eclipse Sigil gets no card (its only row, Amount, is unused); RANDOM once / always not built |
| Arcana | Pick an Arcana at start (or random) | Arcanas | UI | Arcana mode: ArcanaChoiceScreen with every card plus Random | MATCH |  |
| Arcana | 11:00/21:00 chest: pick 1 of 4 (6), Reroll; normal chest at 3 | Arcanas | UI | Arcana chest: draft of 4 (6 with a free Reroll at 23+), Reroll and Skip; a regular chest at 3 Arcanas | MATCH |  |
| Arcana | 22 Arcanas; e.g. Awake: bonuses on each revival | Arcanas, Awake_(IV) | Data | 22 cards, each handled in ArcanaManager / ArcanaEffects; Awake: +3 Revivals and its bonus on every revive | MATCH |  |
| Arcana | - | - | Data | 18 cards have own names; Awake, Gemini, Iron Blue Will and Waltz of Pearls keep theirs (owner, 2026-10-09) | DIFFERS-OWNER | The seven older evolutions also still show VS names (owner: names stay) |

## Enemies & run clock

| System | VS rule | Wiki | DOT | Soul Hunter now | Status | Gap / action |
|---|---|---|---|---|---|---|
| Spawning | Enemies spawn just outside the screen | Enemies | Behavior | Wave enemies, chest elites, bosses, stragglers, map-event formations (EnemySpawner.EventRadius) and Death's Shadow all start past the camera edge (12 Oct) | MATCH |  |
| Spawning | Per-minute waves: minimum + interval | Enemies, Mad_Forest | Behavior | WaveData.Minutes | MATCH | Numbers are SH's curve |
| Spawning | Above the minimum: one of each wave type per tick | Enemies | Behavior | One of each wave type per tick; every minute of all 10 wave assets lists its types | MATCH |  |
| Spawning | 300 alive: only bosses and events spawn | Enemies | Edge case | _maxAliveEnemies 300 | MATCH |  |
| Spawning | Hard cap 500 whatever Curse/Charm | Curse | Edge case | EnemySpawner.HardCap 500 stops map events; Curse and Charm only raise the minimum (clamped to 300) | MATCH |  |
| Spawning | Starting spawns (MF 10) | Mad_Forest | Timing | No starting-spawn value; the minute-0 minimum (15 on Level 1) is filled at once | BALANCE | Owner answer 2026-10-09: the minute-0 minimum (15 on Level 1) is filled at once, which covers Mad Forest's 10 and Hyper's 15 |
| Spawning | Far enemies can despawn | Enemies | Behavior | Enemies beyond 40 units are moved back to just past the screen edge, keeping their health | BALANCE | The wiki gives no distance; moving far enemies back keeps the wave's pressure the same |
| Bosses | Bosses teleported back, never despawn | Enemies | Behavior | Included in relocation | MATCH |  |
| Bosses | Stage boss at 25:00 | Mad_Forest | Timing | 25:00, does not end stage | MATCH |  |
| Bosses | HP x Level at spawn | Enemies | Data | Wave enemies, chest elites and bosses use EnemyData.HealthForPlayerLevel at spawn | MATCH |  |
| Bosses | Resist freeze, instant kill, debuff, knockback | Enemies | Data | All bosses | MATCH |  |
| Chest elites | MF minutes 1,3,5,7,9,10,12,14,15,16,18,20,22,23,25,27,29; Arcana 11, 21 | Mad_Forest | Timing | DefaultChestEliteMinutes | MATCH |  |
| Mini-bosses | Chest-less wave bosses (MF 8:00 Giant Bat, 21:00 and 24:00 Venus) | Mad_Forest | Data | Level 1 (8:00, 21:00, 24:00), Level 3 (24:00), Level 4 (1:00, 4:00), Level 5 (24:00), Level 6 (8:00, 21:00, 24:00); the other source stages have none (ChestlessWaveBosses, 13 Oct) | PARTIAL | Level 5's 24:00 boss is a stand-in type with 100 x Level health and no resistances (VS Maronna Meea Boss: 200 x Level): outside the 5% rule until its own type is built |
| Chest elites | Chest chance and tier per wave row | Enemies, Mad_Forest | Data | ChestTiers.ForMinute gives each chest elite its Mad Forest row; three separate Luck rolls | BALANCE | Owner answer 2026-10-09: the owner fixed the chest minutes for every stage (Mad Forest's), so Mad Forest's rows stay on Levels 2-10 |
| Enemy stats | Per-enemy HP, power, speed, kb, XP (Pipeestrello: 1 HP, 5 dmg, 1 XP) | Enemies, Pipeestrello | Data | Per-type health, damage, speed, knockback and XP x10 for Level 1 (16 types) and Levels 2-10 (235 types) | MATCH |  |
| Knockback | Reverse 120 ms x weapon kb x enemy kb; Garlic +0.3 to 3 | Knockback | Behavior | EnemyController.ApplyKnockback | MATCH |  |
| Resistances | Freeze res vs weapon chance; Orologion ignores | Freeze | Interaction | TryWeaponFreeze | MATCH | Update citation |
| Resistances | Instant kill and debuff resistance | Enemies | Interaction | ResistsInstantKill, EnemyResistance | MATCH |  |
| Movement | Fixed Direction and Floaty | Enemies | Behavior | EnemyMovement | MATCH |  |
| Freeze | Player can push frozen enemies | Freeze | Interaction | EnemyFrozenState.PushFromPlayer: a frozen enemy is shoved out of the player's body as the player walks into it (12 Oct) | MATCH | The push radius is Soul Hunter's (the hero's 0.4 capsule) |
| Curse | Health at once; other Curse effects from the next minute | Curse | Timing | Health uses the Curse now; speed, interval and minimum use the Curse sampled when the stage minute began | MATCH |  |
| Curse | Curse x health, speed, minimum; interval / Curse | Curse | Data | EnemySpawner | MATCH |  |
| Map events | Timed events with repeat and delay | Enemies | Timing | StageEventEntry | MATCH |  |
| Map events | Chance = eventChance / Luck per repeat | Enemies | Behavior | EventFires | MATCH |  |
| Map events | Swarm pushes enemies; Flower Wall lasts 30/60 s | Mad_Forest | Behavior | Event fliers live 15 s and push nothing; Thorn Wall exists only as a slow wave type | DIFFERS-OWNER | Enemy bodies are triggers (owner-approved crowd plan, 2026-09-26: the horde never walls the player in), so a swarm cannot push and a Flower Wall cannot trap |
| Traps | Global trap cooldown x Luck | Enemies | Behavior | PressurePlate.GlobalReadyAt: one cooldown for all plates, 20 s x Luck | MATCH |  |
| Death | Kills give XP gems; >400 merge | Experience_Gem | Behavior | EnemyDrop, cap 400 | MATCH |  |
| Contact | Contact damage 'multiple times per second' | Armor_(stat) | Timing | 0.5 s per enemy | UNKNOWN | Wiki silent |
| Run clock | 30:00, then the Reaper | Stages, The_Reaper | Timing | Next stage at 30:00; L10 no limit | DIFFERS-OWNER |  |
| Stalker | Stalker XL (the latest): 655,350 x Level, 50 damage, only instant kills defeat it; event chance | The_Stalker | Behavior | Death's Shadow follows the VS Stalker XL: 655,350 x level, 50 damage, 1% at 0:00 and 30% at 8:00 and 12:00 (each / Luck) on Stages 5 and 9; only instant kills defeat it; its chests can evolve up to 3 weapons (items 1, 4, 5) | MATCH | The red-ghost jingle is not built |
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
| Light source | IL/DP/GT fixed spots | Light_source | Behavior | Random off-screen | BALANCE | Soul Hunter's floors are its own repeating tiles with no fixed map spots; each level keeps its source stage's chance, cap and maximum |
| Light source | Drop weights and levels; rarity x Luck | Light_source, Luck | Data | LightSourceDrops | MATCH | Little Clover 0.5 vs 1 conflict |
| Enemy drops | Enemies drop only gems | Pickups | Behavior | EnemyDrop | MATCH |  |
| Floor Chicken | Heals 30 | Floor_Chicken | Data | 30 x (1+Recovery) | MATCH |  |
| Coins | 1 / 10 / 100 / 25, x Greed | Coin pages | Data | GoldRewards | MATCH |  |
| Vacuum | Pulls every gem | Vacuum | Behavior | MagnetPickup | MATCH |  |
| Orologion | 10 s; new spawns also frozen | Orologion | Behavior | 10 s; EnemySpawner freezes new spawns for the time left | MATCH |  |
| Orologion | Some enemies resist | Orologion | Edge case | Ignores resistance | BALANCE | The wiki contradicts itself (Freeze: "The global freeze from Orologions are unaffected by freeze resistance"); that sentence is followed |
| Orologion | Halves Gold Fever decay | Orologion | Interaction | GoldFever ticks at half speed while the world is frozen | MATCH |  |
| Rosary | Kills screen; some immune | Rosary | Behavior | Soul Toll | MATCH |  |
| Rosary | Can kill Stalker, which drops a chest | Rosary | Edge case | Soul Toll defeats Death's Shadow, which drops a high-quality chest | MATCH |  |
| Nduja | 10 s fire: 30 dmg, 12, 0.5 s, area 200% | Nduja_Fritta_Tanto | Data | FireBreath: 30 damage, 12 flames, 0.5 s, each flame grows to 200% as it travels (FlameEndScale 2) | MATCH | Flame speed and cone width are Soul Hunter's numbers (the wiki gives only "speed 300%") |
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
| Chest skip | Skip rules | Treasure_Chest | Input | ChestSkipRules: first 6 unskippable, 5-item chests after 50 of them or 500 chests, coin-only chests skip themselves | BALANCE | The third unlock ("seen the final fireworks") is the VS ending, which Soul Hunter does not have |
| Greed | Additive; stage bonus separate | Greed | Data | ApplyGreed | MATCH |  |
| Greed | Stone Mask passive | Greed | Data | Miser's Idol: the VS Stone Mask levels and Greed, found as a stage item and then offered in level-ups (13 Oct) | MATCH | Placeholder art; not placed on Levels 5-6 |
| Stage gold | MF 1.0 ... CM 1.4; Hyper +50% | Stages | Data | GoldBonus | MATCH |  |
| Merchant | Moongolow / every stage later | Merchant | Behavior | Level 3 only | DIFFERS-OWNER |  |
| Merchant | Touch opens shop, one visit | Merchant | Interaction | WanderingMerchant | MATCH |  |
| Merchant | Bone, Cherry Bomb 1,000; Golden Egg 10,000 | Merchant | Data | Same | MATCH |  |
| Merchant | Endless: Revival ware each cycle | Stages | Behavior | Endless: "Second Breath" (+1 Revival, 10,000, once per visit) in place of the Soul Egg | MATCH |  |
| Merchant | Inverse: Skip/Banish/Reroll, Arcana | Merchant | Data | Inverse: Skip 100, Banish 500, Reroll 1,000 (cap 20) and Draw a Fate 20,000 (Arcana choice) | MATCH |  |
| Golden Egg | 19 stats, caps, next run | Golden_Egg | Persistence | SoulEggs | MATCH | Magnet +1% stand-in |
| Stage items | Extra copies with Luck | Mad_Forest | Data | 4 fixed items | BALANCE | The wiki contradicts itself (Passive_items: Spinach 40/30/10%; Mad_Forest: 30/20/10%) and gives no Luck formula; one copy of each item stays |

## Stages, modes & menus

| System | VS rule | Wiki | DOT | Soul Hunter now | Status | Gap / action |
|---|---|---|---|---|---|---|
| Stage | 30:00 limit | Stages | Timing | LevelProgressionManager | MATCH |  |
| Stage | Stage bonuses: speed and gold | Stages | Data | PlayerSpeedFor / GoldFor | MATCH | L6-10 SH mapping |
| Stage | Stage + mode bonuses summed, then multiply | Stages | Data | CurrentGold adds the stage and mode gold bonuses, then multiplies by Greed | MATCH |  |
| Stage | Clear: 500, then +100/+200/+300/+400 per unused revival, then +100 | The_Reaper, Revival | Data | StageClearGold stacks (owner chose these pages, 2026-09-30) | MATCH |  |
| Stage | Confined stages (corridors) | Stages | Behavior | L4 corridor | MATCH |  |
| Stage | Trap plates trigger events | Stages | Interaction | Rune Plates | MATCH |  |
| Stage | Pre-placed items and relics | Stages | Data | A passive, a weapon, a chicken, a Magnet or Time Freeze, both Metaglios, the two Rings and the Idol where placed, and 11 relics with lasting boons (RelicPickup, 13 Oct) | PARTIAL | Metaglio, ring and idol guards are not built |
| Stage | Coffins unlock characters | Stages | Interaction | Soul Caskets on Levels 1-7, each with elite guards, unlock Corvan, Draven, Gideon, Kaelen, Isolde, Seraphina and Lumina (SoulCasket, 13 Oct); the other characters keep their conditions | MATCH | Casket look is a placeholder |
| Unlocks | 20 light sources unlock Fire Wand | Light_source | Persistence | ItemUnlocks: every lockable weapon and passive has its VS condition in Soul Hunter terms (20 Soul Lanterns for the Fire Wand, 5 Roast Chickens for Garlic, 5,000 kills for the Lightning Ring ...); nine items open from the start; older saves keep everything (13 Oct) | MATCH | Conditions that name a VS character use the Soul Hunter character standing for it |
| Mode | Hurry: clock x2, XP +25% | Stages | Timing | RunModes | MATCH |  |
| Mode | Hyper: per-stage values | Stages | Data | Per-stage Hyper tables for player speed, projectile speed, Luck and enemy speed | MATCH |  |
| Mode | Inverse: gold, Luck, enemy HP/speed | Stages | Data | Gold +200%, Luck +20%, enemy health x(3 + 0.05 per minute), enemy speed +0.5% of the stage pace per minute | MATCH |  |
| Mode | Inverse: flipped darker map, alt music | Stages | UI | Stage items, Metaglios and Rune Plates turn 180 degrees; the floor darkens and shifts hue; "Visually Invert Stages" option; Level n plays Level 11-n's track (MusicSelection.StageTrackLevel, 13 Oct) | BALANCE | The floor tiles already lie in random quarter turns, so a flip would show nothing; SH has no alternate recordings, so another level's own track is used |
| Mode | Endless cycles; +100% HP, +50% spawn, +25% dmg, damage cap -1 | Stages | Behavior | Multipliers match; no damage cap | BALANCE | Owner answer 2026-10-09: the wiki never defines the damage cap, and Soul Hunter has none |
| Mode | Random Events | Random_Events | Behavior | Omens: RandomEventDirector picks at XX:30 (29:30 included) and fires at XX:00 from 19 events (13 Oct) | PARTIAL | Seven VS events are left out (they need VS-only enemies or hazards); equal weights |
| Mode | Random LevelUp | Stages | Behavior | Blind Fate: each level-up picks a random card with no screen; toggled in the pause menu | MATCH |  |
| Mode | Modes unlock via relics | Stages | Persistence | All modes open from the start (owner, 2026-10-09) | DIFFERS-OWNER |  |
| Audio | Track per stage; Music Selection | Music | Audio | A track per stage, or the Music selection: 11 tracks, Normal/Faster/Slower, a Keep lock | MATCH |  |
| HUD | XP bar at top | Level_up | UI | XPBarUI | MATCH |  |
| HUD | 6 + 6 slots | Passive_items | UI | InventoryHud | MATCH |  |
| HUD | Timer, kills, gold, health bar | - | UI | Built | UNKNOWN | Wiki silent |
| Pause | Options; Grimoire; Map | Options, Grim_Grimoire | UI | Resume, Options, Evolutions (the Grimoire, open from the start), Map (PauseMapScreen: stage items, caskets, relics, the merchant; optional guide arrows), Blind Fate, Quit, and the stats panel (13 Oct) | MATCH | Marker shapes are placeholders; one map zoom for every level |
| Pause | Quit still shows results | Results | Behavior | Quit ends the run into Results (`GameSessionManager.QuitRun`) | MATCH | Level 1 step 4 |
| Results | Stage, mode, gold bonus, time, gold, level, kills | Results | UI | `RunResultsScreen` top left | MATCH | Level 1 step 4; run time over all stages |
| Results | Per weapon: level, damage, time, DPS | Results | UI | Every hit credited to its weapon (`DamageSource`); SI prefixes; top damage/DPS yellow | MATCH | Level 1 step 4; damage counts overkill (wiki silent) |
| Results | Character, eggs, items, PowerUps, pickups, Arcanas; unlocks toggle; DONE | Results | UI | All built; DONE to the main menu | MATCH | Level 1 step 4; pickup names are Soul Hunter's, gems not listed (wiki silent); no mobile ad button |
| Options | Favorites, Display, Sound, Gameplay | Options | UI | Tabs Favorites, Display, Sound, Gameplay (and Controls); Favorites copies four common options | MATCH |  |
| Options | Display incl. Refresh Rate | Options | UI | Screen Refresh Rate row, shown only in Exclusive Fullscreen | MATCH |  |
| Options | Gameplay options | Options | UI | Joystick Type and Visually Invert Stages beside Damage Numbers, Show Stage Progression, Visible Joystick | MATCH |  |
| Options | Language, co-op, Twitch ... | Options | UI | - | N/A |  |
| Save | Delete Save, Data Recovery | Options | Persistence | Atomic saves with .prev and .bak; Options "Save Data" tab on the main menu: Data Recovery, and Delete Save behind six tab presses and two confirmations (13 Oct) | MATCH |  |
| Menu | Collection, Unlocks, Bestiary, Credits, Quick Start | Main_menu | UI | Collection, Unlocks, Credits, Bestiary, Quick Start (locked until Level 5 is cleared) and a start prompt (13 Oct) | MATCH | The nine-button, three-column menu layout needs owner approval |
| Menu | Stage select | Stages | UI | One run | DIFFERS-OWNER |  |
| Collection | Items seen, '?' unknown | Collection | UI | CollectionBook / CollectionTracker; "?" for unknown entries | MATCH |  |
| Bestiary | Defeated enemies list | Ars_Gouda | UI | Bestiary: kill counts per enemy type, names hidden until the first kill; opens at Level 3 | MATCH |  |
| Achievements | Unlocks via achievements | Achievements | Persistence | Unlock ids saved by CharacterUnlockTracker, PowerUpUnlocks, CollectionBook and ItemUnlockTracker unlock characters, PowerUp ranks, weapons and passives; the Unlocks menu shows each with its counter; the unused AchievementManager is deleted (13 Oct) | MATCH | Stages are not unlocked: one ten-level run (owner decision) |
| Secrets | Secrets menu | Secrets | UI | - | N/A |  |
| Story | - | - | UI | Story cards, victory line (GDD) | DIFFERS-OWNER |  |
