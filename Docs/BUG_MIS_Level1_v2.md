# Bug MIS v2: Level 1, step 6 CTO pass (2026-10-02)

Owner's brief: "ab as a cto step 6 tum pura 30 mnt khelo bug ka mis bana kar save karo phir dikhao apprval lo fix karo". Play Level 1 for the full 30 minutes, **only note bugs**, show this register, and fix only after the owner approves.

**Build observed:**
- Commit `b524b24a` (after Level 1 plan steps 1-3).
- Isolated copy `D:\SoulHunter-Validation\editmode-20260924-134853\project`, GUI editor, 1920×1080 Game view.

**Run:** `D:\SoulHunter-Validation\cto-level1-run2A\`
- AutoplayBot played Kael with no god mode and a fresh throw-away save (no PowerUps, no Arcana or other modes).
- It started from the Play button: Bootstrap → Main Menu → character select → Level 1.
- It played 33 real minutes: Stage 1 to 30:00, the screen clear, then Stage 2 to 1:37.
- Result: survived, level 93, 30,913 kills, 7,598 gold, 17 chests, 92 level-ups, **0 errors** in the log, 1 hitch (252 ms).
- Recorded: 490 screenshots, a timeline row every 5 s, events, and the editor log.

**Review:**
- Screenshots were checked by 12 review agents in batches. 484 of 490 were opened; 6 (from 246-365) were never opened by any agent.
- I checked every bug marked "seen by me" on the cited screenshot, and traced causes in code where noted.
- Events and timeline were checked against the wiki.

**Severity:** S1 blocker/crash/data loss · S2 major (wrong rule or a feature broken in normal play) · S3 minor · S4 cosmetic.

**Status:** awaiting the owner's approval. Nothing is fixed yet.

## Bugs

| ID | Sev | Area | Bug | Evidence (shots) | Expected | Cause (where found) |
|---|---|---|---|---|---|---|
| C01 | S2 | Weapon look | Song of Mana's shot is a solid, unlit pale-lavender oval about 70×160 px. It often covers the hero completely, and sometimes the HUD. | 091, 130, 230, 269, 325, 433, 484 (seen by me: 269, 325) | A readable beam that never hides the hero | `WeaponVfxBuilder`: `SH10_Proj_ManaShot` core is an opaque stretched sphere (0.3×1.1×0.3), scaled further by Area |
| C02 | S2 | HUD rule | The HUD Time is the run's total time, not the stage clock. Stage 2 opens at "30:06" and counts on, while its waves, elites and boss run on 0:00-30:00. | 471, 472, 489 | The stage's own 30-minute clock (wiki Stages: each stage has a 30:00 limit; its events follow that clock) | `TimerUI` shows `GameSessionManager.SurvivalTime` |
| C03 | S3 | Weapon look | Hellfire Breath flames are clusters of flat, solid pale-yellow discs. | 137, 240, 303, 305 (seen by me: 303) | Flames | `FireBreath` uses the fireball's opaque core sphere, which grows while it flies |
| C04 | S3 | Weapon look | Lightning Ring strikes are thick, opaque white zigzag lines, 300-500 px long, with no glow. One crossed the stage title. | 364, 368, 369, 426 (seen by me: 364) | The approved lightning VFX look | Lightning strike prefab |
| C05 | S3 | Level-up UI | Level-up cards stretch edge to edge (x 0-1920) with 1, 2 or 3 cards. The icon and the NEW!/Level tag are about 1,600 px apart. | 004, 052, 297, 309, 442 (seen by me: 004, 052) | Cards sized like before, centred | Likely since the level-up screen moved to its own Expand canvas (2026-10-01) |
| C06 | S3 | Hit flash | Big bodies under constant hits read as flat white most of the time: the boss in 4 of 8 frames, and elites. The 2026-09-30 B02 fix is not enough for them. | 269, 405, 406, 409, 411 (seen by me: 269) | Keep their look; flash briefly | `DamageFlash` |
| C07 | S3 | Chest UI | The opened chest lid is see-through and banded, and the inside is a flat, solid cyan slab. | 024, 056, 085, 137, 423, 458 (seen by me: 423) | The approved Soul Reliquary, open | `ChestPreviewStage` lid materials |
| C08 | S4 | Level-up UI | The dim behind the level-up screen covers the HUD unevenly. Labels are dimmed while numbers, the stage name, the Time and some slot icons stay bright. | 052, 095, 330, 374 (seen by me: 052) | All of the HUD dimmed alike | HUD draw order |
| C09 | S4 | HUD | Empty item slots are see-through, so a lantern or gravestone behind them looks like an item in the slot. | 049, 050, 164, 192 | Slots with their own backing | `InventoryHud` |
| C10 | S4 | HUD | An evolved weapon's slot becomes a solid yellow square, with low contrast to the orange icon. | 269 onward (seen by me) | A gold frame around the icon (AGENTS.md, HUD inventory row) | `InventoryHud` |
| C11 | S4 | HUD | Fire, blood, lightning and damage numbers draw over the HUD (stage name, Gold, item slots). | 304, 361, 426, 446 ("3Gold: 6628"), 474 | The HUD stays readable | World effects drawn after the HUD backings |
| C12 | S4 | Combat text | Under heavy kills, damage numbers stack into unreadable clumps ("6565", "3Ø30"). | 107, 244, 284, 364 (seen by me: 364) | Readable numbers (wiki silent on the layout) | `DamagePopup` |
| C13 | S4 | UI | The unlock notice ("New character unlocked: Aria!") has no backing; yellow text sits over the horde. | 069, 076, 115 | Readable, like the HUD labels | `CharacterUnlockTracker` notice |
| C14 | S4 | Level-up text | Weapon level-up cards say only "Stronger Whip: level 2." and not what changes. | 004, 095, 102 (seen by me: 004) | What the level adds (each weapon's wiki level table) | `UpgradeText` |
| C15 | S4 | Names | "Song Of Mana" has a capital "Of". | 085, 095 | Consistent casing | `UpgradeText.DisplayName` |
| C16 | S4 | Names | Item cards read like stat names: "Max Health" (Hollow Heart) and "Player Speed" (Wings). | 052, 205, 218 (seen by me: 052) | Item names (Soul Hunter's own; see O7) | `UpgradeText.DisplayName` from the enum |
| C17 | S4 | Names | One reward has two names: the chest card says "Coin Bag +25 gold" and the maxed level-up card says "Gold Pouch +25 gold", with the same icon. | 423, 442 (seen by me: 423) | One name | Chest reward text |
| C18 | S4 | Icons | Wrong or odd icons: Cross shows a blue comet, Bible a brass horn, and Duplicator and Spellbinder are opaque squares. | 052 (Cross, seen by me), 045 (Bible), 067 (Duplicator), 035 (Spellbinder) | An icon that shows the item | `UpgradeIconAssigner` picks |
| C19 | S4 | Level-up UI | Through a chain of 30 level-ups, every screen shows the final level ("LV 50" on all 30) instead of stepping 21, 22 and so on. | 200-229, 374-402 | Each screen shows its own level. The wiki does not say | HUD reads the final level |
| C20 | S4 | HUD | "LV n" is drawn over the right end of the XP bar. | every gameplay shot | Clear of the bar | HUD layout |
| C21 | S4 | UI | The Level 1 story card covers the right column of the HUD item slots. | 002, 003 | Clear of the HUD | `StoryCardUI` position |
| C22 | S4 | Chest UI | World effects (a lightning bolt) stay bright through the chest screen's dim, so the bolt looks attached to the chest. | 084, 423 (seen by me: 423) | Dimmed with the world | Effects drawn above the dim |
| C23 | S4 | Pickups | A floor chicken and Gold Fever coins land on or against house walls, and may be unreachable. Low confidence. | 250, 252, 264, 265 | Pickups on open ground | Drop position, no `GroundCheck` |

## Observations (not counted as bugs; some need an owner decision)

| ID | What | Evidence | Note |
|---|---|---|---|
| O1 | The level stayed at 20 from 7:45 to 14:20, and at 61 from 21:50 to 26:00, while kills went on. The level then jumped +30 at once (30 level-up screens in a row). | timeline: gems 400 from 9:00; level 20→50 at 14:23, 61→92 at 26:00 | VS rule (wiki Experience_Gem): past 400 gems, new XP goes into one red gem. The bot does not go back for it; a player who does not would see the same. Elite and boss soul gems also went into it ("soulGems 0"). |
| O2 | Hundreds of Gold Fever coins lay uncollected for minutes, and read as dark brown rings ("donuts"). | 144, 263, 269, 338-362 | The bot does not walk over coins, and Magnet was base (no Attractorb). The bronze coin is approved art; from the game camera it reads poorly. |
| O3 | Balance: hurt only 7 times in 30 minutes, and at full health (249) from 15:00. The 15:00 chest elite lived 346 s and the 20:00 one 458 s; the boss lived 104.6 s at level 61. | events | Bug MIS O3 (plan step 5). |
| O4 | Performance in the GUI editor: 33-41 FPS with 300 enemies (minimum 22). One 252 ms hitch at 22:30 with nothing paused. Open level-up screens averaged 34.9 FPS. | timeline, events | Editor numbers, not a build. Belongs to a performance pass. |
| O5 | Chest shots show no OPEN or DONE button. | 023, 055, 084, 136, 422, 423, 431 | Bot timing: it takes the shot as the phase starts, before the button rotates in (`ChestOpeningScreen`: `Current = Phase.Done; yield return RotateIn(_doneButton)`). Not a game bug. |
| O6 | Only one evolution all run (Bloody Tear). | log | The bot's picks allowed no other (no Duplicator, Armor or Skull; Metaglios never collected). |
| O7 | Item names are still VS names (Santa Water, Peachone, Crown, Spinach, Clover, "Tiragisu", Laurel, ...). | every level-up | The no-copy rule wants Soul Hunter's own names (Phase 2). Owner decision on names. |
| O8 | The 1:27 chest has silver bands. | 023 | Correct: the 1:00 chest can evolve (VS Mad Forest), so it is silver. |
| O9 | The horde was thin at 12:28-12:40 (10-20 enemies). | 173-175 | Correct: the wiki's minute-12 minimum for Mad Forest is 20. |
| O10 | Low confidence, not checked by me: the boss looked tilted (419, 420); the Clock Lancet beam started off the hero (238, 243); the Pentagram glyph sat off the hero (321); a beam was hidden behind a crypt roof (416). | listed | Check while fixing. |

## Checked and correct

- **Run flow:** Play → Main Menu → character select (13 cards fit, LOCKED badges on one line).
- **Chests:**
  - The first six chests gave 1-1-3-1-1-5 items (wiki Treasure_Chest).
  - The 7th chest could be skipped.
  - Bands were bronze before 10:00 (the 1:00 chest silver), and silver after.
- **Chest elites:**
  - They came at the VS minutes 1, 3, 5, 7, 9, 10, 12, 14, 15, 16, 18, 20, 22, 23, 27 and 29 (16).
  - Their health matched the wiki, for example 50 × 4 = 200 at 1:00, 500 flat at 5:00, 150 × 20 at 10:00, 200 × 51 at 15:00 and 250 × 59 at 20:00.
- **Boss:** it came at 25:00 with 150 × level 61 = 9,150 health, and dropped a chest.
- **Waves:** they followed the Mad Forest table (for example 10 at 5:50, 300 at 11:00).
- **Stage clear:** at 30:00 the screen was cleared, +500 gold was paid, and Stage 2 started with its story card and a 1:00 elite at 50 × 93.
- **UI and errors:**
  - No internal names ("Evo_", "Lv."), no magenta, no missing textures, no stuck screens.
  - The stats panel was readable, and the evolution showed as "Bloody Tear EVOLUTION!".
  - 0 errors in the log.

## What I ask the owner

1. Approve fixing C01-C23. Recommended order: C01-C02, C05-C07, then the S4s.
2. **C19:** should each chained level-up screen show its own level? The wiki does not say.
3. **O7:** item names. Keep the VS names for now, or give Soul Hunter its own names (Phase 2)?
4. **O2:** should the bronze coin look change? The model is approved art, but it reads as a dark ring from the game camera.
