# VS audit: enemies, waves, bosses, stages, map events, traps, run clock, modes

Audit date 2026-10-27. Read-only. Raw wiki pages (`?action=raw`, fetched today with curl) are kept in
`C:\Users\Lenovo\AppData\Local\Temp\claude\D--Unity-Projects-Soul-Hunter\6fdfafb8-2413-4f46-b8cb-631090820c24\scratchpad\vs-audit\wiki\`
(enemy statbox pages are in its `e\` subfolder). The parsing and compare scripts are in `...\vs-audit\scripts\` (waves.py, cmp.py, ev.py, ev2.py, sb.py, best.py).

Mapping used, the same as the code's: L1 Mad Forest, L2 Inlaid Library, L3 Dairy Plant, L4 Gallo Tower, L5 Abyss Foscari, L6 Mt.Moonspell, L7 Lake Foscari, L8-L10 Cappella Magna.
**Inconsistency found:** `CampaignLevelCatalog` (Hyper and stage bonuses) and `LightSourceSpawner` map L5-L10 to Cappella Magna. The enemy tables (`StageEnemyTables`) map L5 to Abyss Foscari, L6 to Mt.Moonspell and L7 to Lake Foscari. Rows marked (map) below come from this inconsistency.

Status key:
- MATCH: matches the wiki.
- DIFF: differs from the wiki.
- DIFF-OWNER: differs because of a recorded owner decision in AGENTS.md. It is kept here because the owner asked for a 100% wiki comparison.
- MISSING: not built.
- WIKI-CONFLICT: the wiki contradicts itself.

| Item | Rule | Wiki (page + short quote) | Soul Hunter now (file:line) | Status | What to change |
|---|---|---|---|---|---|
| Wave minimum, L1 | 30 per-minute minimums | Mad_Forest Waves: 15,30,50,40,30,10,20,80,100,30,10,300,20,150,20,100,100,20,60,100,100,300,200,300,300,100,150,300,300,300 | Data/Config/WaveTables.cs:41-42 | MATCH | - |
| Spawn interval, L1 | 30 per-minute intervals | Mad_Forest: 1.0,1.0,0.5,0.25,1.0,1.0,0.5,0.5,1.5,0.5,0.5,0.1,0.25,0.5,0.1 ... 0.1 | WaveTables.cs:43-44 | MATCH | - |
| Wave minimum and interval, L2-L4 and L8-L10 | Per-minute table | Inlaid_Library, Dairy_Plant, Gallo_Tower, Cappella_Magna Waves tables | InlaidLibraryEnemies.cs:79-80; DairyPlant/GalloTower/CappellaMagnaEnemies.cs (Minimum/Interval) | MATCH | All 30 rows checked by script on 7 stages |
| Wave minimum and interval, L5-L7 | Per-minute table | Abyss_Foscari, Mt.Moonspell, Lake_Foscari | Abyss/MtMoonspell/LakeFoscariEnemies.cs | MATCH | Except the next row |
| Lake Foscari 22:00 and 25:00 minimum | "0.8" and "1 (150)" | Lake_Foscari Waves | LakeFoscariEnemies.cs: 80 and 150 | WIKI-CONFLICT | Wiki text is malformed. Keep the balance reading. |
| Wave enemy types per minute, L1-L4, L8-L10 | Types listed per minute | Stage Waves tables (e.g. Mad_Forest 13:00 "Werewolf, Ghost (x2)") | MadForestEnemies.ByMinute; *Enemies.cs ByMinute | MATCH | Spot-checked all 30 minutes on Inlaid, Dairy and Gallo. Variant numbering follows the wiki sprites. |
| Wave enemy types, L5-L7 | Same | Rows use `{{Sprite ...link=}}` | Abyss/MtMoonspell/LakeFoscariEnemies.cs | NOT RE-CHECKED | My parser could not read those rows. AGENTS records that an earlier checker compared them. |
| "One of each type" above the minimum | "If more enemies than the minimum amount is present, one of each type ... will be spawned" | Enemies, Waves | EnemySpawner NextWavePrefab / per-tick types | MATCH | - |
| 300 periodic cap and 500 hard cap | "When 300 or more enemies are alive the game will not spawn more ... periodically ... up until 500" | Enemies, Waves | EnemySpawner.cs:22 (300), :28 HardCap 500, :264 | MATCH | - |
| Gold Finger spawn rule | "During a Gold Finger, the minimum and maximum spawns are both set to 500" | Enemies, Waves | none | MISSING | Add with the Gold Finger pickup (already known as missing). |
| Starting spawns | "an initial amount of enemies depending on the stage modifiers spawns"; starting-spawns MF 10, IL 50, DP 40, GT 50, AF 40, MM 50, LF 50, CM 40 | Enemies, Spawning; stage infoboxes | Only the minute-0 minimum is filled (AGENTS) | DIFF | Covered on MF, IL, GT, AF, MM and LF. Short on Dairy Plant (40 against minute-0 min 20) and Cappella Magna (40 against 20). Add a per-stage starting burst. |
| Hyper starting spawns | starting-spawns2: MF 15, IL 60, DP 60, GT 60, AF 60, MM 70, LF 70, CM 60 | Stage infoboxes | minute-0 min x1.25 | DIFF | Short on L3, L6, L7 and L8-L10. Use the infobox values. |
| Spawn position | "Enemies generally spawn just outside the screen" | Enemies | EnemySpawner off-screen margin (AGENTS row 9) | MATCH | - |
| Far enemies | "can despawn if the player moves far enough from them" | Enemies, Spawning | EnemySpawner.cs:53 relocates past 40 units | DIFF | Despawn them (refill by minimum) instead of teleporting, or record an owner exception. |
| Bosses far away | "Bosses do not despawn ... but get teleported back to the screen" | Enemies, Bosses | Straggler relocation includes bosses (AGENTS) | MATCH | - |
| Spawn sides, L1, L3, L8-L10 | "Standard spawn type ... all four directions" | Mad_Forest, Dairy_Plant, Cappella_Magna | WaveTables.cs SideFor AllSides | MATCH | - |
| Spawn sides and layout, L2 | Inlaid Library: "long hallway ... spawns ... only ... from west and east ends" | Inlaid_Library, Overview | L2 open, all sides (WaveTables.cs SideFor) | DIFF-OWNER | Owner kept the Dark Forest open (AGENTS, "Not adopted"). |
| Layout, L4 | Gallo Tower is a confined stage | Stages: "Inlaid Library, Gallo Tower ... confined layouts" | L4 horizontal walled corridor, ends-only spawns | DIFF | Gallo is a vertical tower, and the horizontal hallway is Inlaid's. Owner-approved corridor. |
| Spawn sides, L6 | Mt.Moonspell all sides | Mt.Moonspell | 80% top/bottom (WaveTables SideFor) | DIFF-OWNER | Owner table 2026-09-26 |
| Curse on waves | "effectiveSpawnInterval = spawnInterval / totalCurse"; health applies at once, other effects "at the start of the next minute" | Curse; Enemies | EnemySpawner.cs:298, 304, 597, 640 | MATCH | - |
| Curse cap on screen | "capped at 500, regardless of Curse and Charm" | Curse | HardCap 500 | MATCH | - |
| Knockback | "movement speed is reversed and multiplied ... lasts 120 milliseconds"; Garlic +0.3 per hit up to max | Knockback | EnemyController.ApplyKnockback (AGENTS Part 5) | MATCH | - |
| Frozen enemies pushed | "push them around by walking into them" | Freeze | EnemyFrozenState.PushFromPlayer | MATCH | - |
| Orologion through resistance | "Freeze from Orologion goes through freeze resistance" | Enemies, Resistances | TimeFreezePickup | MATCH | - |
| Enemy resistances (freeze, kill, debuff, defang) | Fields per statbox | Enemies, Resistances | EnemyData fields | PARTIAL | Bosses checked (1.1). Not checked per type on the 235 L2-L10 types (res-defang especially). |
| HP x Level | "applied the moment the enemy was spawned" | Enemies, Skills | EnemySpawner (HealthPerPlayerLevel at spawn) | MATCH | - |
| Floaty and Fixed Direction skills | "Fixed Direction ... straight line"; "Floaty ... wavy" | Enemies, Skills | EnemyData.Movement (Homing/Floaty/FixedDirection/FloatyFixed) | MATCH | Except the next row |
| L2/L7 all enemies Floaty | Only enemies with the Floaty skill weave | Statboxes (e.g. Dust Elemental has no skill) | InlaidLibraryEnemies AllFloaty=true; L7 all spectral and no knockback | DIFF-OWNER | Owner signature mechanic |
| Enemy movement speed | VS speed x stage enemy-speed (MF 1.1, IL/DP/GT 1.25, AF/MM/LF/CM 1.4) | Stage infoboxes "enemy-speed" | CampaignLevelCatalog.cs:72-81 own pace 1.0-1.6 | DIFF-OWNER | Owner kept own pace (2026-10-09). |
| Statbox spot-check (50 types) | HP, dmg, speed, knockback, kb-max, XP | Pipeestrello 1-5, Zombie, Skeleton, Ghost 1/2, Mudman 1/2, Giant Bat, Werewolf, Mantichana, Big Mummy, Flower Wall, Venus, Dust Elemental (+boss), Musc Musc / Big / Colossal, Mummy, Lionhead, Undead Witch, Merdusa, Merman 1/2/3/boss, Lizard Pawn, Minotaur, Sword Guardian, Skullino, Harzia / V, Manticore, Cauld, Edna, Conand, Balor, Spiritello, Dark Spiritello, Kamaitachi (+Boss), Tengu, Chompo, Red Chompo, Vulvio | MadForestEnemies.cs:40-60; *Enemies.cs T(...) rows | MATCH | All 50 matched (XP is wiki x10, Soul Hunter's gem scale) |
| Stage boss L1 | Giant Blue Venus: 150 HP x Level, speed 160, dmg 30, xp 50 | Venus statbox | Enemies/L01_..._Boss_Data.asset: 150/lvl, speed 3 (VS 100), dmg 10, XP 250 | DIFF | Speed should be 4.8, damage 30, XP 500 |
| Stage boss L2 | Hag: 250 x Lvl, speed 210, dmg 30, xp 50 | Hag | L02 boss asset: 250/lvl, speed 3, dmg 12, XP 250 | DIFF | Speed 6.3, dmg 30, XP 500 |
| Stage boss L3 | Sword Guardian (boss): 250 x Lvl, 210, 30, xp 50 | Sword_Guardian | L03 boss: 250/lvl, 3, 14, 250 | DIFF | Same fix |
| Stage boss L4 | Giant Enemy Crab: 500 x Lvl, 200, 30, xp 50; Pincer minions | Giant_Enemy_Crab | L04 boss: 500/lvl, 3, 16 | DIFF | Speed 6, dmg 30. Abilities missing (next row). |
| Stage boss L5 | Fomorians Ethlenn/Elatha: 150 x Lvl, 160, 20 (two bosses) at 25:00; Je-Ne-Viv 3000 x Lvl | Fomorians, Je-Ne-Viv | L05 boss: 500/lvl, one boss, at 20:00 | DIFF / DIFF-OWNER | 500 and 20:00 are owner decisions. Two-boss pair and Je-Ne-Viv are missing. |
| Stage boss L6 | Orochimario body and heads 500 x Lvl, body speed 190, dmg 25/20 | Orochimario | L06 boss: 500/lvl, 3, 20, at 20:00 | DIFF | Speed and heads missing; 20:00 is owner |
| Stage boss L7 | Avatar of Gaea (25:00) | Avatar_of_Gaea | L07 boss 500/lvl | DIFF | Boss stats not matched (page statbox gives a minion, 8 HP) |
| Stage boss L8-L10 | Trinacria (boss) 250 x Lvl, 210, 30; The Ender 2550 x Lvl | Trinacria, The_Ender | L08-L10 boss 500/lvl; L10 = Shadow Kael | DIFF-OWNER | Story: guardians never weaker; L10 is the story boss |
| Boss abilities | Crab pincers, Orochimario heads, Ender shield/zones, Trickster gems ... | Boss pages | EnemyData.Abilities exists, but "No boss has an ability yet" (AGENTS) | MISSING | Fill the Abilities lists per boss statbox/page |
| Boss time | 25:00 boss on every mapped stage | Stages ("defeating the 25:00 boss") | 25:00; L5/L6 at 20:00 (CampaignLevelCatalog.cs:68) | DIFF-OWNER | - |
| Chest-boss minutes, L1 | 1,3,5,7,9,10,11A,12,14,15,16,18,20,21A,22,23,25,27,29 | Mad_Forest Waves | EnemySpawner.cs:39 plus 11/21 Arcana plus 25 boss | MATCH | - |
| Chest-boss minutes, L2-L10 | IL 3,5,8,10-16,18,20-22,25-28; DP 1,3,5,7,9-12,16,18,20-23,25,27; GT 3,5,8,10-16,18,20-23,25-28; AF like DP; MM 1,3,5,7,9-12,14-16,18,20-23,25,27,29; LF 2,3,5,8,10-16,18,20-23,25-28; CM 1,3,5,7,9-12,15,18,20-25,27 | Each stage's Waves table | Mad Forest minutes on all stages (EnemySpawner.cs:39) | DIFF | Per-stage lists. AGENTS calls this a balance choice, but the wiki has the answer. |
| Chest tiers per boss, L2-L10 | Each stage's {{treasure}} rows | Stage Waves | ChestTiers uses Mad Forest rows on all stages | DIFF | Use each stage's rows |
| Evolving chests, L3 and L8-L10 | "All chests before the 10-minute mark can evolve weapons" (Dairy Plant, Cappella Magna notes) | Stages | Mad Forest evo flags on all stages | DIFF | Per-stage evo flags |
| Chest-boss stats | Glowing Bat 50 x Lvl, dmg 10, speed 140, xp 30; Mantichana 500 / 20 / 80; Big Mummy (boss) 250 x Lvl / 20 / 80 / xp 25; Werewolf 1 (boss) 200 x Lvl / 20 / 130 | Pipeestrello, Mantichana, Big Mummy, Werewolf | Health only (EnemySpawner.cs:1025-1031); body = stage elite asset (L01 elite: dmg 8, speed 3, XP 50) | DIFF | Damage, speed and XP from the chest boss's statbox; per-stage chest bosses |
| Chest-less wave bosses | MF Giant Bat 8:00, Venus 21:00/24:00 | Mad_Forest | MadForestEnemies ChestlessBosses; L3-L6 ChestlessWaveBosses | MATCH | (L2, L7, L8-10 not re-checked) |
| Map events, L1 | 2:00 Bat Swarm r2 (100%); 3/4/6/12:00 10%; 5:00 Flower Wall 100% 30 s; 7:00 80% r5; 8:00 80% r2 d15; 9:00 70% r2; 10:00 Flower Wall 60 s; 11:00 10% n50; 13:00 Ghost Swarm 70% r20 x2; 15:00 Flower Wall 80%; 20:00 Bat Swarm 70% r20 x2; 25:00 Flower Wall; 27:00 Ghost Swarm r19; 29:00 Bat Swarm r19 | Mad_Forest Waves, Map events column | WaveTables.cs:89-128: generic schedule (2,3,4,6-9,11,12 at 80% x2; Swarm at 5/10/15/25; 13/17/22 70% x3; 20 x5; 27/29 x6) | DIFF | Author the exact per-minute table; Flower Wall is a closing ring of Thorn Walls (already built as an Omen) |
| Map events, L2 | Shade Bomb (Sig.ra Rossi), Medusa Wall/Swarm (Sneaky Head), Skull Swarm cascade 1-2-4-8-16-32-32 at 20 and 25, Bat Swarm 25 | Inlaid_Library | Generic schedule, closing rings | DIFF | Per-stage table |
| Map events, L3 | Stalker 0:00 1%, 8:00 30%/120 s, 12:00 30%/60 s; Pile Assault (Twin Snakes/Demons, Lost Twin); Milk Swarms; Jellyfish Wall/Swarm; Minotaur Rush | Dairy_Plant | Generic walls; Stalker on L5 and L9 (StageHazardTable.cs:85) | DIFF | Stalker (Death's Shadow) belongs on L3; add Pile Assault, Jelly and Mino events |
| Map events, L4 | Shooting Star 0, 4, 15, 22, 30; Dragon Swarm/Stream; Pile Assault Undead Mage 9:00 n12 80%, 10:00 n6 x2 | Gallo_Tower | Generic walls; Shooting Stars on L6/L10 every 4 min (StageHazardTable.cs:120); Pile Assault on L8 at Gallo's times (:170) | DIFF | Move Meteors and Pile Assault to L4 with the wiki times and counts |
| Map events, L5 | The Blinder 0/8/12; Rotting Ghoul/Blood Moss/Snek Swarms; Lost Head Pile Assault; Rellyfish Wall | Abyss_Foscari | Generic swarms x1.8 speed; Death's Shadow 8/12 | DIFF | Per-stage table; The Blinder missing |
| Map events, L6 | Spiritello Swarms (2,3,4,6-9,11-13,20,27,29); Flower Wall Circle 5/10/15 | Mt.Moonspell | Generic; Soul Meteors | DIFF | Per-stage table |
| Map events, L7 | Medusa Swarms (Wiseparke), Sammy the Caterpillar, Sam the Clown swarms, Snek Skull Swarm cascade at 20 | Lake_Foscari | Generic | DIFF | Per-stage table |
| Map events, L8-L10 | Maddener 0:00, Drowner 5:00, Stalker 10:00, Trickster 15:00 (before The Ender) | Cappella_Magna | Generic rings; Drowner only as an Omen | DIFF / MISSING | Maddener and Trickster missing |
| Event chance and Luck | "chanceWithLuck = eventChance / totalLuck ... evaluated for every repeat" | Enemies, Wave events | EnemySpawner.cs:252, :694 | MATCH | - |
| Traps: which stages | Trap pizzas on Dairy Plant, Gallo Tower (Enemies); also Abyss Foscari and Lake Foscari pages | Enemies, Traps; DP/GT/AF/LF pages | Rune Plates on every level (StageItemSpawner) | DIFF | L1, L2, L6 and L8-L10 should have none |
| Trap events | "chosen at random from a set of predetermined options unique to the stage" (DP 9, GT 7, AF 11, LF 9 options) | Same | Random Swarm/Wall/Ring of 16 (PressurePlate) | DIFF | Per-stage event lists |
| Trap cooldown | "global cooldown ... effectiveCooldown = trapCooldown x totalLuck"; "cool down for at least 20 seconds" | Enemies, Traps; Dairy_Plant | PressurePlate.cs:15 20 s, global x Luck | MATCH | - |
| Dairy carts | Destructible carts on rails | Dairy_Plant | RailCart on L3 (StageHazardTable.cs:174) | MATCH | - |
| Stage player speed | MF x1.1; IL/DP/GT x1.25; AF/MM/LF/CM x1.4 | Stages list; infoboxes | CampaignLevelCatalog.cs:90 | MATCH | - |
| Stage gold | MF 1, IL 1, DP 1.2, GT 1.3, AF 1.5, MM 1.5, LF 1.5, CM 1.4 | Infobox "greed" | CampaignLevelCatalog.cs:91: L5 1.4, L8-L10 1.5 | DIFF (map) | L5 1.5, L8-L10 1.4 |
| Stage XP and enemy health | xp x1; enemy-health x1 on all mapped stages | Infoboxes | none | MATCH | - |
| Light sources | MF 10%/50%/10; IL 7.5/50/10; DP 20/60/10; GT 7.5/50/10; AF 7.5/50/18; MM 7.5/50/12; LF 7.5/50/12; CM 20/80/20 | Infobox dchance, dchance-max, dmax | LightSourceSpawner.cs:44-56: L5-L10 all Cappella 20/80/20 | DIFF (map) | L5 7.5/50/18; L6 and L7 7.5/50/12 |
| Hyper player speed | MF 2; IL/DP/GT 1.9; AF/MM/LF/CM 2 | Infobox player-speed2 | CampaignLevelCatalog.cs:112 | MATCH | - |
| Hyper enemy speed, L2 | Inlaid enemy-speed2 "x1.25" (no change) | Inlaid_Library infobox; Enemies "Hyper mode in each stage increases ... the movement speed" | x1.52 (CampaignLevelCatalog.cs:115) | WIKI-CONFLICT | Infobox says no change; owner balance choice recorded |
| Hyper enemy speed, others | MF 2/1.1; DP/GT 1.9/1.25; AF/MM/LF/CM 2/1.4 | Infoboxes | :115-118 | MATCH | - |
| Hyper projectile speed | MF 1.25; IL/DP/GT/AF/CM 1.15; MM and LF have no value | Infobox projectile-speed2 | 1.15 on L6/L7 (:113) | UNKNOWN | Wiki silent for MM/LF |
| Hyper gold | +50% on every stage (greed2 - greed = 0.5 everywhere) | Infoboxes | RunModes.cs:63 +0.5 | MATCH | Stage pages quoting "+100%" give the total |
| Hyper Luck | +10% except Mad Forest | Infobox luck2 | CampaignLevelCatalog HyperLuck | MATCH | - |
| Hyper enemy minimum | enemy-min2 x1.25 | Infoboxes | RunModes.cs:90 | MATCH | - |
| Hyper enemy health | "may also grant bonuses to ... enemy health" | Stages; Enemies | none | MATCH (n/a) | No mapped stage has one (only Il Molise, Moongolow, Green Acres, Boss Rash, The Coop) |
| Hyper unlock | "unlocked by defeating the 25:00 boss" (Abyss: Je-Ne-Viv) | Stages | Open from the start | DIFF-OWNER | - |
| Hurry | "doubles the stage's Clock Speed and grants a +25% bonus to XP"; events "at their usual timestamps" | Stages, Hurry Mode | RunModes.cs ClockSpeed 2, Xp 1.25 | MATCH | Unlock (Sorceress' Tears) is open from the start: DIFF-OWNER |
| Inverse gold | Stages: "+200% bonus to gold"; infobox greed3: MF 3 (+200%), IL x2 (+100%), DP x2.2 (+100%) | Stages; infoboxes | +2.0 everywhere (RunModes.cs:63) | WIKI-CONFLICT | Infobox gives +100% on IL/DP |
| Inverse enemy health | Stages "+200% HP"; IL infobox "x2 (+0.05 per minute)", MF/DP x3 | Same | x3 everywhere (RunModes.cs:84) | WIKI-CONFLICT | L2 x2 per infobox |
| Inverse speed and Luck | "+0.5 movement speed per minute" (infobox +0.005 additive); Luck +20% | Stages; infoboxes | x(1+0.005 min) multiplicative; Luck +0.2 | MATCH (within 5%) | - |
| Inverse rotate and darken; merchant wares | "rotates ... by 180° ... darker hue"; Merchant sells Skips, Banishes, Rerolls, Arcana | Stages | InverseMap; merchant P2-03 | MATCH | Alternate music missing (no tracks) |
| Endless cycle | "waves restart from the beginning" of the same stage; +100% base HP, +50% frequency and quantity, +25% damage per cycle | Stages, Endless Mode | Restarts at Level 1 (whole campaign); multipliers match (RunModes.cs:84-96) | DIFF-OWNER | - |
| Endless damage cap | "the player's maximum incoming damage cap decreases by 1" | Stages, Endless Mode | Not built (no cap exists) | MISSING | Wiki never defines the cap |
| Endless merchant | "reappears after each cycle and offers one additional Revival" | Stages | Built (AGENTS wave 1) | MATCH | - |
| Arcana mode | First Arcana at start chosen; two more from chests; up to 3 (4 in Inverse) | Stages, Arcana | ArcanaChoiceScreen, chests 11:00/21:00 | MATCH | Darkanas missing (owner: later) |
| Limit Break, Random LevelUp | As described | Stages | Built | MATCH | Detail covered by the weapons audit |
| Random Events | Roulette at XX:30, fires at XX:00, ~32 events | Random_Events | RandomEventDirector.cs: 27 Omens covering every listed event (some swarms grouped) | MATCH | VS-only enemies replaced by stage enemies (balance) |
| Mode unlocks | Hurry by Sorceress' Tears, Inverse by Gracia's Mirror, Endless by Seventh Trumpet, Arcana by Randomazzo, Limit Break by Great Gospel, Random Events by Trisection, Random LevelUp by Brave Story | Stages | All open from the start | DIFF-OWNER | - |
| Stage time limit | 30:00 on all mapped stages | Stages | 30:00; L10 none | MATCH / DIFF-OWNER | - |
| Reaper at time limit | "a Reaper appears every minute to kill the player" (655,350 x Lvl HP, speed 1200, 65,535 dmg, knockback -0.5) | Stages; The_Reaper | No Reaper; next level opens if guardian dead (LevelProgressionManager.cs:244) | DIFF-OWNER | - |
| Completion gold | "500 ... increases by 100 coins for each unused Revival" (stacking to +400 per The_Reaper) | Stages; The_Reaper | StageClearGold, stacking | MATCH | - |
| Stage unlocks | IL by level 20 in MF; DP by level 40 in IL; GT by level 60 in DP; CM by level 80 in GT; DLC by ownership | Stages | Guardian kill opens the next level, no saved unlock, no stage select | DIFF-OWNER | - |
| Stage items | MF: Spinach (up to 4, 30/20/10%), Clover (up to 4), Hollow Heart, Pummarola, Skull O'Maniac + 4 hidden; IL: Empty Tome (up to 4), Stone Mask; DP: Attractorb, Armor, Wings, Candelabrador; GT: Bracer, Spellbinder; CM: Crown, Tirajisú, Duplicator, Stained Glass; AF/MM/LF own lists | Stage pages, Items | StageItems.cs:32-38: one passive + one weapon + chicken + Vacuum/Orologion per level on diagonals (L1 Spinach + Knife ...) | DIFF | Use each stage's list, places and extra-copy chances; no weapons except the stages' own (Dairy Cart, Stained Glass) |
| Hidden stage items | Silver/Gold Ring and Metaglio L/R at 7 tilesets N/E/S/W (MF) | Mad_Forest | Metaglios 45 units W/E; Rings placed; guardians built | MATCH (roughly) | Distances and places differ per stage |
| Coffins | One coffin per stage with a character | Stage pages | Soul Caskets L1-L7 | MATCH (own) | - |

## Missing content

Bestiary coverage, matched by entry name against every `"VS name"` string in `Assets/_Project/Data/Config/*.cs`:
- The wiki lists 392 bestiary entries. 121 have a counterpart in Soul Hunter and **271 have none**.
- 250 of the 271 appear only on stages Soul Hunter does not map.

Missing per mapped stage (wiki entries for that stage, then missing):
- **Mad Forest** (16): The Reaper.
- **Inlaid Library** (20): Sig.ra Rossi (Shade Bomb event), Hag (replaced by the guardian), Nesufritto, The Reaper, The Trickster.
- **Dairy Plant** (22): The Reaper, The Stalker. Death's Shadow is the Stalker, on L5 and L9.
- **Gallo Tower** (22): Poltergeist (trap and roulette events), Giant Enemy Crab (guardian instead), The Drowner. The Drowner exists only as the Omen's Tide Caller.
- **Abyss Foscari** (22): The Blinder, Rellyfish, Fomorians (Ethlenn/Elatha boss pair), Je-Ne-Viv.
- **Mt.Moonspell** (16): Hitotsume-kozo, Kappa, Tanuki, Big Oni, Goshadokuro, Orochimario.
- **Lake Foscari** (22): Sammy the Caterpillar, Fungoman, Avatar of Gaea.
- **Cappella Magna** (21): Poltergeist, "Unknown", The Trickster, The Stalker (event), The Drowner (event), The Maddener, The Ender.

The match is by name only. Some "found" names may be stand-in types with no full stats, and a few Soul Hunter-renamed equivalents (Stalker, Drowner) are counted as missing.

Stages not in Soul Hunter: 81 stages in VS (34 primary, 47 Adventure).
- **Bonus/challenge/special/hidden:** Il Molise, Moongolow, Holy Forbidden, Green Acres, The Bone Zone, Boss Rash, Whiteout, The Lycaeum, The Coop, Space 54, Carlo Cart, Laborratory, Westwoods, Bat Country, Astral Stair, Mazerella, Tiny Bridge, Eudaimonia Machine, Room 1665.
- **DLC:** Red Moon Manor, Polus Replica, Neo Galuga, Hectic Highway, Ode to Castlevania, Emerald Diorama, Ante Chamber.
- **Adventures:** all of them.

Bestiary entries on those stages: Ode to Castlevania 56, Neo Galuga 25, Emerald Diorama 23, Red Moon Manor 20, Westwoods 18, Polus Replica 16, The Bone Zone 15, Ante Chamber 14, Moongolow 12, The Lycaeum 10, The Coop 8, Room 1665 8, Space 54 7, Astral Stair 7, Boss Rash 5, Il Molise 5, Whiteout 5, Laborratory 4, Mazerella 4, Tiny Bridge 1, Green Acres 1, Bat Country 1, Eudaimonia Machine 1.

Mapped stages' bosses and events missing:
- Je-Ne-Viv, The Ender, The Maddener, The Trickster, The Blinder.
- The 25:00 VS bosses: Soul Hunter uses its own guardians.
- The Reaper and the White Hand.

## Not checked (budget)

- Resistance fields (res-freeze, res-kill, res-debuff, res-defang) of the 235 L2-L10 types. Only bosses were checked.
- Per-minute enemy types of L5-L7 (parser could not read the Sprite-link rows).
- Chest-less wave bosses of L2, L7 and L8-L10.
- Each stage's full trap event list. Only the counts were read.
- Stage-item positions on L5-L7.
- Boss pages beyond the stats listed: Je-Ne-Viv and The Ender behaviour.
- Ghost/Big Mummy boss variants on other stages.
- Merchant per stage.
- The Hyper music change.
