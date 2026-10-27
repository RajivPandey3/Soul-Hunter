# VS chest bosses and stage bosses: wiki statboxes vs Soul Hunter types

Date 2026-10-27. Raw pages (`?action=raw`, redirects followed) are in `vs-audit/wiki/b/`. Data: `boss-statboxes.json` (one object per stage and boss variant). Scripts: `scratchpad/fetchb.py`, `sbdump.py`, `shparse.py`, `bosscmp.py`.

Match rule: a type in that level's `*Enemies.cs` table whose health, HP x Level, damage, VS speed, knockback, max knockback, VS xp, res-freeze, res-kill and res-debuff all equal the statbox. Empty wiki resistance fields count as 0.

**83 (stage, variant) rows. 28 have a numerically equal SH type (25 under the same variant; 3 only through a twin statline of another variant). 55 need a new type.**


## Mad_Forest (L1)

| Min | Boss (variant) | HP | xLvl | Dmg | Spd | KB/max | XP | rF | rK | rD | SH type |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1,3,7,11,12,16,21,22,27,29 | Glowing Bat (Pipeestrello 4) | 50 | yes | 10 | 140 | 1/3 | 30 | 0 | 0 | 0 | EmberBat |
| 5 | Mantichana (Mantichana) | 500 | no | 20 | 80 | 0/3 | 3 | 0 | 0 | 0 | MantisWraith |
| 9,14,18,23 | Silver Bat (Pipeestrello 5) | 50 | yes | 10 | 140 | 1/3 | 30 | 0 | 0 | 0 | AshenBat |
| 10 | Giant Mantichana (Mantichana (boss)) | 150 | yes | 20 | 160 | 0/1 | 50 | 1.1 | 0 | 0 | **NEW** |
| 15 | Giant Werewolf (Werewolf 1 (boss)) | 200 | yes | 20 | 130 | 0.1/1 | 2 | 0 | 0 | 0 | **NEW** |
| 20 | Giant Mummy (Big Mummy (boss)) | 250 | yes | 20 | 80 | 0/1 | 25 | 1.1 | 0 | 0 | **NEW** |
| 25 | Giant Blue Venus (Giant Blue Venus) | 150 | yes | 30 | 160 | 0/1 | 50 | 1.1 | 0 | 0 | **NEW** |

## Inlaid_Library (L2)

| Min | Boss (variant) | HP | xLvl | Dmg | Spd | KB/max | XP | rF | rK | rD | SH type |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 3,5 | Giant Mummy (Big Mummy (boss)) | 250 | yes | 20 | 80 | 0/1 | 25 | 1.1 | 0 | 0 | **NEW** |
| 8 | Colossal Musc Musc (Colossal Musc Musc) | 50 | yes | 10 | 170 | 1/1 | 30 | 0 | 0 | 0 | SporeColossus |
| 10,23 | Colossal Lionhead (Lionhead (boss)) | 50 | yes | 10 | 170 | 1/1 | 30 | 0 | 0 | 0 | ThornLionKing |
| 11 | Colossal Sneaky Head (Colossal Sneaky Head) | 40 | yes | 10 | 170 | 1/1 | 30 | 0 | 0 | 0 | GloomMothQueen |
| 11,12,16,21,22 | Glowing Bat (Pipeestrello 4) | 50 | yes | 10 | 140 | 1/3 | 30 | 0 | 0 | 0 | **NEW** |
| 13 | Colossal Dust Elemental (Dust Elemental (boss)) | 50 | yes | 10 | 170 | 1/1 | 30 | 0 | 0 | 0 | AshColossus |
| 14 | Silver Bat (Pipeestrello 5) | 50 | yes | 10 | 140 | 1/3 | 30 | 0 | 0 | 0 | **NEW** |
| 15,26,27,28 | Queen Medusa (Merdusa (boss)) | 150 | yes | 20 | 160 | 0.5/1 | 30 | 1.1 | 0 | 0 | **NEW** |
| 18 | Master Witch (Undead Sassy Witch (boss)) | 150 | no | 30 | 200 | 0.6/1 | 3 | 1.1 | 0 | 0 | **NEW** |
| 20 | Nesuferit (Nesufritto) | 200 | yes | 30 | 160 | 0/1 | 50 | 1 | 0 | 0 | **NEW** |
| 25 | Hag (Hag) | 250 | yes | 30 | 210 | 0/1 | 50 | 1.1 | 1 | 1 | **NEW** |

## Dairy_Plant (L3)

| Min | Boss (variant) | HP | xLvl | Dmg | Spd | KB/max | XP | rF | rK | rD | SH type |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Lizard Rook (Lizard Rook) | 120 | no | 7 | 100 | 0/1 | 2.5 | 0 | 0 | 0 | SootBrute |
| 3,5,9 | Minotaur Boss (Minotaur Boss) | 60 | no | 11 | 200 | 0.5/3 | 30 | 0 | 0 | 0 | **NEW** |
| 7 | Merman Boss (Merman (boss)) | 50 | yes | 11 | 140 | 1/3 | 40 | 0 | 0 | 0 | EmberChieftain |
| 10,23 | Minotaur Boss (Minotaur Boss (large)) | 50 | no | 11 | 180 | 1/3 | 30 | 0 | 0 | 0 | OxDemonChampion |
| 11,21 | Glowing Bat (Pipeestrello 4) | 50 | yes | 10 | 140 | 1/3 | 30 | 0 | 0 | 0 | **NEW** |
| 12 | Giant Goat-Horned Shooter (Lost Twin (boss)) | 70 | yes | 10 | 0 | 0/0 | 30 | 0 | 0 | 0 | **NEW** |
| 16 | King Triton (Tritont (boss)) | 200 | yes | 20 | 180 | 0/1 | 50 | 1.1 | 1 | 1 | **NEW** |
| 18 | Axe Guardian (Axe Guardian) | 200 | no | 20 | 180 | 0/1 | 40 | 0 | 0 | 0 | **NEW** |
| 20 | Big Golem (Big Golem 2 (boss)) | 200 | yes | 20 | 180 | 0/1 | 50 | 1 | 0 | 0 | **NEW** |
| 22 | Big Golem (Big Golem 1 (boss)) | 200 | yes | 20 | 180 | 0/1 | 50 | 1 | 0 | 0 | **NEW** |
| 25 | Sword Guardian (Sword Guardian (boss)) | 250 | yes | 30 | 210 | 0/1 | 50 | 1.1 | 1 | 1 | **NEW** |
| 27 | Colossal Gallotrice (Colossal Gallotrice) | 200 | yes | 20 | 180 | 0/1 | 50 | 1 | 1 | 1 | **NEW** |

## Gallo_Tower (L4)

| Min | Boss (variant) | HP | xLvl | Dmg | Spd | KB/max | XP | rF | rK | rD | SH type |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 3,5,12,14,16,22 | Elite Devil (Elite Devil) | 50 | yes | 10 | 140 | 0.5/3 | 30 | 0 | 0 | 0 | **NEW** |
| 8 | Giant Skullone (Skullone (boss)) | 50 | yes | 10 | 140 | 1/3 | 30 | 0 | 0 | 0 | **NEW** |
| 10 | Colossal Harpy (Harzia (boss)) | 50 | yes | 10 | 170 | 1/1 | 30 | 0 | 0 | 0 | GreatBoneDrake (twin of Dragon Shrimp 1 (boss)) |
| 11,21 | Glowing Bat (Pipeestrello 4) | 50 | yes | 10 | 140 | 1/3 | 30 | 0 | 0 | 0 | **NEW** |
| 13 | Colossal Bone Dragon (Dragon Shrimp 1 (boss)) | 50 | yes | 10 | 170 | 1/1 | 30 | 0 | 0 | 0 | GreatBoneDrake |
| 15 | Gallo (enemy) (Gallo) | 200 | yes | 30 | 160 | 0/1 | 50 | 1.1 | 1 | 1 | **NEW** |
| 18 | Shield Guardian (Archon Disco (boss)) | 150 | yes | 20 | 160 | 0.5/1 | 30 | 1.1 | 0 | 0 | **NEW** |
| 20 | Colossal Flame Dragon (Dragon Shrimp 2 (boss)) | 60 | yes | 10 | 170 | 1/1 | 30 | 0 | 0 | 0 | GreatFlameDrake |
| 23,26 | Colossal Gallotrice (Colossal Gallotrice) | 200 | yes | 20 | 180 | 0/1 | 50 | 1 | 1 | 1 | **NEW** |
| 25 | Giant Enemy Crab (Giant Enemy Crab) | 500 | yes | 30 | 200 | 0/1 | 50 | 1.1 | 1 | 1 | **NEW** |
| 25 | Giant Enemy Crab (Pincer) | 5 | yes | 12 | 0 | 0/1 | 10 | 0 | 0 | 1 | **NEW** |
| 27,28 | Colossal Manticore (Colossal Manticore) | 200 | no | 20 | 180 | 0/1 | 50 | 1 | 1 | 1 | **NEW** |

## Abyss_Foscari (L5)

| Min | Boss (variant) | HP | xLvl | Dmg | Spd | KB/max | XP | rF | rK | rD | SH type |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Rotting Ghoul (Rotting Ghoul) | 120 | no | 7 | 100 | 0/1 | 4 | 0 | 0 | 0 | BogGhoul |
| 3,5,9,11,21 | Colossal Redusa Head (Colossal Redusa Head) | 100 | yes | 20 | 180 | 0/1 | 30 | 0 | 0 | 0 | ColossalRotHead |
| 7,12 | Flaming Skull (Flaming Skull Boss) | 100 | yes | 20 | 180 | 0/1 | 30 | 0 | 0 | 0 | GreatMarshfireSkull |
| 10,23 | Colossal Meatball (Colossal Meatball) | 100 | yes | 20 | 180 | 0/1 | 30 | 1 | 0 | 0 | **NEW** |
| 16,22 | Warm Cauld (Warm Cauld) | 200 | yes | 20 | 180 | 0/1 | 30 | 1.1 | 1 | 1 | GreatBogMother (twin of Maronna Meea Boss) |
| 18 | Edna (Edna Boss) | 100 | yes | 20 | 180 | 0/1 | 30 | 0 | 1 | 0 | **NEW** |
| 20 | Well Well Well Dweller (Well Well Well Dweller) | 200 | yes | 20 | 180 | 0/1 | 30 | 1.1 | 1 | 1 | GreatBogMother (twin of Maronna Meea Boss) |
| 25 | Fomorian Bosses (Ethlenn) | 150 | yes | 20 | 160 | 0.5/1 | 30 | 1.1 | 0 | 0 | **NEW** |
| 25 | Fomorian Bosses (Elatha) | 150 | yes | 20 | 160 | 0.5/1 | 30 | 1.1 | 0 | 0 | **NEW** |
| 27 | Maronna Meea Boss (Maronna Meea Boss) | 200 | yes | 20 | 180 | 0/1 | 30 | 1.1 | 1 | 1 | GreatBogMother |

## Mt.Moonspell (L6)

| Min | Boss (variant) | HP | xLvl | Dmg | Spd | KB/max | XP | rF | rK | rD | SH type |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1,3,9,11,14,15,16,18,21,22 | Spiritello Boss (Spiritello Boss 1) | 50 | yes | 10 | 140 | 1/3 | 30 | 0 | 0 | 0 | **NEW** |
| 5 | Mikoshi-nyudo (Mikoshi-nyudo) | 150 | no | 10 | 140 | 0.3/3 | 2.5 | 0 | 0 | 0 | LoomingDrift |
| 7 | Raiju Boss (Raiju Boss) | 50 | yes | 10 | 170 | 1/1 | 30 | 0 | 0 | 0 | GreatStormHound |
| 10 | Thunderous Oni (Windy Oni) | 500 | no | 20 | 70 | 0/3 | 2.5 | 0 | 0 | 0 | GaleOgre |
| 10 | Thunderous Oni (Thunderous Oni) | 500 | no | 20 | 70 | 0/3 | 2.5 | 0 | 0 | 0 | ThunderOgre |
| 12 | Tanuki Boss (Tanuki Boss) | 60 | yes | 20 | 200 | 1/1 | 30 | 0 | 1 | 0 | **NEW** |
| 20 | Kamaitachi Boss (Kamaitachi Boss) | 50 | yes | 10 | 170 | 1/1 | 30 | 0 | 0 | 0 | GreatBlizzardWeasel |
| 23 | Tengu Boss (Tengu Boss) | 60 | yes | 20 | 200 | 1/1 | 30 | 0 | 0 | 0 | **NEW** |
| 25 | Orochimario (Orochimario body) | 500 | yes | 25 | 190 | 0/1 | 30 | 1.1 | 1 | 0 | **NEW** |
| 25 | Orochimario (Orochimario head) | 500 | yes | 20 | 0 | 0/1 | 30 | 1.1 | 1 | 0 | **NEW** |
| 27,29 | Colossal Spiritello Boss (Spiritello Boss 2) | 200 | yes | 10 | 140 | 1/3 | 30 | 0 | 0 | 0 | **NEW** |

## Lake_Foscari (L7)

| Min | Boss (variant) | HP | xLvl | Dmg | Spd | KB/max | XP | rF | rK | rD | SH type |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 2,3 | Foscaritrice (Foscaritrice) | 40 | yes | 10 | 170 | 0.8/3 | 3 | 0 | 0 | 0 | **NEW** |
| 5,12,13 | Colossal Foscaritrice (Colossal Foscaritrice) | 40 | yes | 10 | 170 | 0.8/3 | 3 | 0 | 0 | 0 | **NEW** |
| 8 | Nutmeg Boss (Nutmeg Boss) | 50 | yes | 10 | 170 | 1/3 | 30 | 0 | 0 | 0 | **NEW** |
| 10,14 | Njuggles Boss (Njuggles Boss) | 50 | yes | 10 | 170 | 0.3/3 | 30 | 0 | 0 | 0 | **NEW** |
| 11,16 | Sam the Boss Clown (Sam the Boss Clown) | 40 | yes | 10 | 170 | 0.8/3 | 30 | 0 | 0 | 0 | **NEW** |
| 11,21 | Arcana Foscaritrice (Arcana Foscaritrice) | 40 | yes | 10 | 170 | 0.8/3 | 3 | 0 | 0 | 0 | **NEW** |
| 15 | Vulvio Boss (Vulvio Boss) | 150 | yes | 20 | 160 | 0.5/1 | 30 | 1.1 | 0 | 0 | **NEW** |
| 18 | Lammuga Boss (Lammuga Boss) | 150 | yes | 30 | 200 | 0/1 | 30 | 0 | 0 | 0 | **NEW** |
| 20 | Missing Church Boss (Missing Church Boss 2) | 180 | yes | 30 | 180 | 0/1 | 30 | 1 | 0 | 0 | **NEW** |
| 20 | Missing Church Boss (Missing Church Boss) | 200 | yes | 30 | 160 | 0/1 | 30 | 1 | 0 | 0 | **NEW** |
| 22,23,28 | Deep Green Knight (Deep Green Knight) | 200 | yes | 20 | 180 | 0/1 | 30 | 0 | 0 | 0 | **NEW** |
| 25 | Avatar of Gaea (Avatar of Gaea) | 8 | no | 5 | 140 | 0/1 | 30 | 2 | 0 | 1 | **NEW** |
| 26,27 | Big Green Knight (Big Green Knight) | 80 | yes | 20 | 120 | 0/1 | 25 | 1 | 0 | 0 | GreatKelpKnight |

## Cappella_Magna (L8-10)

| Min | Boss (variant) | HP | xLvl | Dmg | Spd | KB/max | XP | rF | rK | rD | SH type |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1,5 | Fallen Archangel / Colossal Fallen Archangel (Fake Fallen Cherubbello (boss)) | 50 | yes | 11 | 140 | 1/3 | 30 | 0 | 0 | 0 | ColossalWarden |
| 3,7,9,11,12,21,22,23,24,27 | Colossal Eyeball (Unknown (boss)) | 60 | yes | 11 | 200 | 0.5/3 | 30 | 0 | 0 | 0 | **NEW** |
| 10 | Colossal Succubus (Succubus (boss)) | 60 | yes | 11 | 140 | 1/3 | 30 | 0 | 0 | 0 | ColossalTemptress |
| 15 | Colossal Green Knight (Archon Rame (boss)) | 70 | yes | 11 | 140 | 1/3 | 30 | 0 | 0 | 0 | **NEW** |
| 18 | Axe Guardian (Axe Guardian) | 200 | no | 20 | 180 | 0/1 | 40 | 0 | 0 | 0 | **NEW** |
| 20 | Colossal Archdemon (Archdemon (boss)) | 80 | yes | 11 | 140 | 1/3 | 30 | 0 | 0 | 0 | ColossalArchfiend |
| 25 | Trinacria (Trinacria (boss)) | 250 | yes | 30 | 210 | 0/1 | 50 | 1.1 | 1 | 1 | **NEW** |

## 25:00 stage bosses (skills from their pages)

- **Mad_Forest Giant Blue Venus**: HP x Level | Boss; 1.5x scale; defeating it unlocks Hyper. No other behaviour described on the page.
- **Inlaid_Library Hag**: HP x Level | Boss only; resistant to freeze, Rosary, instant kill, debuffs; reduced knockback; unlocks Hyper. No attack behaviour described.
- **Dairy_Plant Sword Guardian (boss)**: HP x Level | HP x Level; resistant to freeze, instant kill, debuff; reduced knockback; unlocks Hyper. No attack behaviour described.
- **Gallo_Tower Giant Enemy Crab**: HP x Level | Two Pincers (5 x Lvl, dmg 12, speed 0) grow when damaged and regenerate after destruction up to 17 times; in Gallo Tower summons a Drowner if the player stays below it over 6 s; its chest can evolve up to 5 weapons; unlocks Hyper.
- **Gallo_Tower Pincer**: HP x Level
- **Abyss_Foscari Ethlenn**: HP x Level | Fomorian Bosses: Ethlenn and Elatha spawn together at 25:00 with one chest; reduced knockback; no other behaviour described. (Conand, Balor, Indech are the normal Fomorians, 700 HP.)
- **Abyss_Foscari Elatha**: HP x Level | Pair with Ethlenn (Fomorian Bosses, 25:00).
- **Mt.Moonspell Orochimario body**: HP x Level | 8 heads, first head double health; each head adds its health to the body (total 5,000 x Lvl); damage to heads also hurts the body; heads can be defeated individually and lunge at the player every 5-10 s; resistant to freeze, Rosary, instant kill, debuff, knockback; unlocks Hyper.
- **Mt.Moonspell Orochimario head**: HP x Level | Orochimario's head (speed 0); lunges at the player every 5-10 s.
- **Lake_Foscari Avatar of Gaea**: - | Kill-count health, invulnerable phase, enemy minimum 150, Ghostly Apparition summons (see notes); resistant to freeze, debuff, knockback; unlocks Hyper.
- **Cappella_Magna Trinacria (boss)**: HP x Level | Unlisted boss variant: HP x Level; resistant to freeze, instant kill, debuff; res-defang 1; unlocks Hyper. No attack behaviour described.

## Wiki problems and judgement calls
- **Mt.Moonspell 10:00 Windy Oni + Thunderous Oni:** the stage cell uses the normal sprites and both pages' Appearances tables list 10:00 'Boss' under the normal statbox (500 HP, speed 70); the 'Oni Boss' statboxes (70 x Lvl, speed 180) appear at 29:00 'Normal'. Recorded as the normal statboxes (match GaleOgre/ThunderOgre). If the Boss statboxes are meant, GreatGaleOgre/GreatThunderOgre match them exactly.
- **Mt.Moonspell 5:00 Mikoshi-nyudo:** the page has only the normal statbox (150, no HP x Level, xp 2.5), listed as 05:00 Boss. Matches LoomingDrift.
- **Mad Forest 5:00 Mantichana** links the normal Mantichana (500, xp 3), not the boss variant (that is the 10:00 Giant Mantichana).
- **Werewolf 1 (boss)** statbox xp is 2; **Undead Sassy Witch (boss)** (Master Witch) has xp 3 and no HP x Level; **Foscaritrice / Colossal / Arcana Foscaritrice** have xp 3. Quoted as written; they look like wiki errors next to the other chest bosses (25-50).
- **Trinacria page trivia** says the boss shares Gallotrice/Manticore's statline, but its statbox (250 x Lvl, 30, 210) does not; statbox used.
- **Avatar of Gaea** statbox health 8 is only its start (kill-count health, invulnerable phase); the 30-ish numbers do not describe the fight.
- **Merman (boss)** has no image; mapped by its Appearances row (Dairy Plant 7:00 Boss). **Minotaur Boss** sprite without suffix = 'Minotaur Boss' (3/5/9:00), '-2' = 'Minotaur Boss (large)' (10/23:00). **Big Golem** -4 = 2 (boss) at 20:00, -3 = 1 (boss) at 22:00.
- **Orochimario** res-debuff field is literally 'None' while the page text says debuff resistant (recorded false from the statbox; page says resistant).
- Fallen Archangel (1:00) and Colossal Fallen Archangel (5:00) both resolve to the single 'Fake Fallen Cherubbello (boss)' statbox.
- Soul Hunter's `MadForestEnemies` has no resistance fields, so any boss with res-freeze 1.1 (Giant Mantichana, Big Mummy boss, Giant Blue Venus) cannot match there. `LakeFoscariEnemies` sets NoKnockback for the whole table (owner decision); raw T() knockback numbers were compared.
- Twin-statline matches (counted as matches but no own variant): Harzia (boss) = GreatBoneDrake numbers; Warm Cauld and Well Well Well Dweller = GreatBogMother numbers.
