# VS DOT Plan: Soul Hunter = VS core logic + the owner's own everything else

Owner rule (2026-09-30): "SH = VS + my own story + own audio, own VFX, own assets, own UI, etc. Everything should be own, but the core logic of VS must be adopted top to bottom (DOT). No mistakes. If a VS rule has to change a little, 5% is allowed."

Source table: `Docs/VS_DOT_Table.md` (177 VS rules from raw vampire.survivors.wiki pages, 2026-09-30). Viewer: https://claude.ai/artifact/Sxo83CvLXnW7Vt28LFZg2x

## The split

| Taken from VS (rules) | Soul Hunter's own (presentation) |
|---|---|
| Every rule, number, formula, timing and flow: stats, weapons, passives, evolutions, level-ups, enemies, waves, spawning, pickups, chests, gold, stages, modes, menus and what each menu does | Story (`Docs/Game_Design_Document.md`, never edited), names, text, characters, art, models, VFX, audio, music, the UI's look |

## The 5% rule

- A VS number may be tuned by at most ±5% of its wiki value. Every tuned number keeps the wiki value in a code comment and in AGENTS.md.
- Anything bigger, or a structural change, needs an explicit owner decision. The approved ones are the recorded exceptions (AGENTS.md section 5): one 10-stage run, no Reaper, Level 10 ends only when Shadow Kael dies, Level 3 merchant, Level 7 ghosts, Shadow Kael's mirror, nearest-enemy aiming, dash and Soul Burst (from the GDD).
- Existing Soul Hunter numbers that are more than 5% off VS without an owner decision are defects and are fixed in Phase 1.

## How every row is done (no mistakes)

1. Re-read the raw wiki page (`https://vampire.survivors.wiki/w/<Page>?action=raw`) and the current code for that row. The table was built by research agents; a row is not trusted until this re-check.
2. If the wiki is silent or contradicts itself, stop and ask the owner. No guesses and no "provisional" numbers.
3. Write a test that fails on the old behaviour, then the fix.
4. EditMode + PlayMode on the isolated copy, then editor play.
5. Record it in AGENTS.md and mark the row MATCH in `Docs/VS_DOT_Table.md`.
6. The owner plays it; final only after the owner's OK.

## Phase 0: owner decisions (asked first)

1. ~~5% rule~~ Confirmed 2026-09-30: numbers ±5%.
2. ~~Dash damage~~ Decided 2026-09-30: remove it (the dash only moves and avoids damage).
3. ~~Invented unions~~ Decided 2026-09-30: remove all 6.
4. ~~Gem colours~~ Decided 2026-09-30: 20 / 90 XP (VS 2 / 9 scaled x10).
5. ~~Revival gold~~ Decided 2026-09-30: stacking +100/+200/+300/+400, then +100 again (The_Reaper and Revival pages; 3 revivals = 500 + 600 = 1,100).
6. ~~Missing items~~ Decided 2026-09-30: add all as Soul Hunter items (own names/art, VS rules): Ebony Wings, Silver Ring, Gold Ring, Stone Mask, the second gun. Built in Phase 2; art needs owner approval.
7. ~~Unlocks~~ Decided 2026-09-30: the VS unlock system (achievements, relics, coffins; own names and text). Built in Phase 3.
8. Arcanas need Soul Hunter names (no-copy rule). Names proposed to the owner when Arcanas are built (Phase 2).
9. ~~Shadow Kael~~ Decided 2026-09-30: his mirrored Clock Lancet and Pentagram get an effect on the player (an owner exception; VS has no rule). The exact effect is proposed to the owner before it is built.

Shadow Kael details (owner, 2026-09-30): Clock Lancet slows the player to 50% for the weapon's freeze time; Pentagram erases the items on screen (VS Pentagram's item erase, stage items kept).

**Phase 0 status:** items 2, 3, 4, 5 and 9 are built (AGENTS.md section 7, "Phase 0 of the VS DOT plan"). Items 6 and 7 are Phase 2 and Phase 3 work; item 8 comes with the Arcanas.

## Phase 1: core rules that change play (no new art needed)

| # | Row | Fix |
|---|---|---|
| 0 | Owner decisions | Remove dash damage; remove the 6 invented unions; gem colours at 20 / 90 XP |
| 1 | Chest contents | Chests level up owned items only |
| 2 | XP curve | +100% Growth at levels 20 and 40 |
| 3 | Stat caps | Might/Area 1000%, Speed/Duration 500%, Amount 10 |
| 4 | Hit i-frames | 240 ms after a hit; i-frames after a chicken and after a chest |
| 5 | Weapon level-up | Reset cooldown and fire at once |
| 6 | Level-up | Reroll excludes the discarded set; Skip/Reroll off when maxed; 'always gold/chicken' after 100 bags |
| 7 | Crits | No base crits (Slash Arcana only); per-weapon crit multipliers |
| 8 | Weapons | Wiki hit intervals and knockback; King Bible on/off; Pentagram 1 s; Peachone 4 s; Cherry Bomb explode chance; Limit Break stat table |
| 9 | Spawning | Spawn just outside the camera view; 500 hard cap; starting spawns; HP x Level on the wave path |
| 10 | Orologion | Freeze new spawns; halve Gold Fever decay |
| 11 | Curse | Non-health effects from the next minute |
| 12 | Traps | Global cooldown x Luck |
| 13 | Magnet | Pulls every pickup; character Magnet multiplies |
| 14 | Chests | Per-boss 5/3/1 tiers with separate rolls |
| 15 | Stalker | Killable, VS damage and spawn chance; Rosary kills it for a chest |
| 16 | Revival | Revive button and animation; the heal fires heal hooks |
| 17 | Characters | Level-bonus caps; price +10% per purchase |
| 18 | Modes | Hyper per-stage values; Endless damage cap; Inverse numbers checked |

## Phase 2: VS content (own names, art, VFX and audio)

- Six missing evolutions: Fire Wand, Santa Water, Runetracer, Lightning Ring, Pentagram, Song of Mana.
- Metaglio Left/Right as stage items, so Crimson Shroud can be reached.
- Arcanas: all 22 (Soul Hunter names), the start pick and the pick-1-of-4 chest screen with Reroll.
- Enemy variety: several enemy types per wave and chest-less mini-bosses (needs models).
- Chest looks: normal, evolution-capable, Arcana.
- Merchant: Endless Revival ware; Inverse-mode wares.

## Phase 3: the shell around the run (VS flow, own UI look)

- Results screen: stage, mode, per-weapon level/damage/time/DPS, items, PowerUps, Arcanas; shown on quit too; DONE to the main menu.
- Stats panel in pause and level-up; evolution hints on level-up cards.
- Main menu: Collection, Bestiary, Unlocks, Credits, Quick Start.
- PowerUp on/off toggle for maxed PowerUps; character select colours, rows and sorting.
- Options: Refresh Rate, Joystick Type, Favorites.
- Modes: Random Events, Random LevelUp, Inverse flipped map; music selection.
- Pause: evolution recipes (Grimoire) and map.

## Level 1 to 100% (owner, 2026-10-01)

Owner: "haan plan save karo aur step 1 se shuru karo". "Level 1 100%" means every VS rule that comes up in a 30-minute
Level 1 run matches, and the owner plays a full run and signs it off. No date is promised: some steps wait on owner
decisions and art approval. A cycle is wiki check, code, its test, both full suites (~30 min) and editor play.

| Step | Work | Size | Status |
|---|---|---|---|
| 1 | Bug MIS O1 (the 400-gem merge gem is visible and reachable for a player) and O2 (level-up FPS without per-level-up bot screenshots) | 1 cycle | Done 2026-10-01: both verified, no game change (Bug MIS O1, O2) |
| 2 | Phase 1 rows 2-12, one at a time (XP curve, stat caps, hit i-frames, weapon level-up fire, level-up rules, crits, weapon intervals and knockback, spawning, Orologion, Curse, traps) | 11 cycles | Done 2026-10-01: rows 2-12 built (see AGENTS.md, Level 1 step 2) |
| 3 | Phase 2 parts seen in Level 1: six missing evolutions, Metaglio stage items, chest looks | 8-10 cycles + art approval | In progress: six evolutions and the Metaglio stage items built 2026-10-01 (approved VFX); chest looks next |
| 4 | VS results screen at the end of a run | 2 cycles | |
| 5 | Balance (Bug MIS O3) and the owner's decision on Pentagram erasing chest elites (O4) | 2-3 cycles | |
| 6 | The owner plays Level 1 from the menu to 30:00; new Bug MIS; fixes; final owner OK | owner's play | |

Done before this plan: the 17 Level 1 Bug MIS fixes, the first level-up hang, Reroll/Skip/Banish hidden at 0, VS locked
PowerUps, the stats panel's first frame, and the chest and level-up screens fitting any window (owner played the first
three and said OK). Phase 1 rows 0 and 1 are built (Phase 0; Bug MIS B04).

## Status

Phase 0 is built (2026-09-30), and Phase 1 rows 0 and 1. Update this file as rows move to MATCH.
