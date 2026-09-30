# Bug MIS: Level 1 observation (CTO pass, 2026-09-30)

Owner's brief: start from the Play button, observe everything in Level 1 up to 30:00, **only note bugs** (no fixes), then present this register. Fixes start only after the owner approves.

**Build observed:** commit `e40ec0e3`, isolated copy `D:\SoulHunter-Validation\editmode-20260924-134853\project`, GUI editor, 1920×1080 Game view.

**Runs:**
- **Run A (realistic):** AutoplayBot as Kael, no god mode, 31 min: Play → Main Menu → character select → Level 1 (`D:\SoulHunter-Validation\cto-level1-runA\`).
  - 303 screenshots, 342 events, a timeline row every 5 s.
  - The hero survived, but the stage clock reached only 29:35: 122 level-ups and 17 chests paused it for 85 s.
- **Run B** (level-100 start, clock skipped to 28:40): not usable. 99 queued level-ups plus all the skipped elites at once; died at 29:36.
- **Run C** (god mode, clock skipped to 29:40): observed the 30:00 stage end and the move to Stage 2.
- **Screenshot review:** three agents plus me; every finding below was checked by me on the cited screenshot. About 100 screenshots could not be opened (image request limit); I sampled that gap myself.

**Severity:** S1 blocker/crash/data loss · S2 major (a rule or feature wrong, visible in normal play) · S3 minor · S4 cosmetic.

**Status:** Open = noted, not fixed, waiting for the owner's approval.

**Reference:** VS rules come from vampire.survivors.wiki; Soul Hunter owner decisions are in AGENTS.md.

| ID | Sev | Area | Bug | When / evidence | Expected (source) | Status |
|---|---|---|---|---|---|---|
| B01 | S2 | Waves | The last six minutes hold only ~84 enemies. Minutes 24-29 use the "quiet minute" value (30% of peak), copied onto 25-29. | Run A timeline: 297 at 23:48, then 79-85 from 24:48 to 29:35. | VS Mad Forest minimums: 24:00 300 · 25:00 100 · 26:00 150 · 27-29:00 300 (wiki Mad_Forest wave table) | Open |
| B02 | S2 | Visual | Enemies, and the stage boss when it first appears, drawn as flat pure-white silhouettes; seen from about 5:30 on, and often. The white 3x boss covers the stage title. | Shots 070, 072, 088, 092, 111, 179 (many around the chest while paused), 223, 276 (boss). | Enemies keep their material; a hit flash is brief | Open |
| B03 | S2 | Chest UI | The chest reel shows internal asset names for evolutions: "Evo_BloodyTear", "Evo_SoulEater", "Evo_UnholyVespers", "Evo_ThousandEdge". | Shots 109, 180, 208, 227 | The player-facing weapon name | Open |
| B04 | S2 | Chest rule | Chests give NEW items ("Knife NEW!", "Cross NEW!", "Fire Wand" etc.). | Shots 020, 109 | VS: chests only level up items you already own (wiki Treasure_Chest, patch 0.2.7). Already Phase 1 row 1 | Open |
| B05 | S3 | UI | The "New character unlocked: Corvan!" notice and "LEVEL UP!" are drawn on top of each other; both unreadable. | Shot 045 (5:02) | One waits for the other, or they sit apart | Open |
| B06 | S3 | UI | Floating damage numbers draw over the HUD text ("Tota50 Kills", "Stage 1/10: The 30rsed…", "Gold" digits, Time) and over the level-up buttons. | Shots 047, 072, 100, 107, 111, 251, 257, 273 | The HUD stays readable | Open |
| B07 | S3 | Visual | Damage-number glyphs appear cut ("3Ↄ", "3)", "C0"), as if clipped by 3D models. | Shots 280, 302, 257 | Whole numbers | Open |
| B08 | S3 | Chest UI | The HUD gold already includes the chest's gold before the count-up (1054 → 1813 while the screen counts "+759"). | Shots 107-109, 178-180 | Gold rises with the count-up | Open |
| B09 | S3 | UI (new) | The level-up stats panel covers the 6th HUD item column (Fire Wand icon), and the lower half of the panel is empty. | Shots 102, 133, 134, 201 | Clear of the HUD row, sized to its rows | Open |
| B10 | S3 | UI | The "Gold Pouch" and "Roast Chicken" level-up cards have no icon. | Shot 134 and every maxed level-up | An icon like every other card | Open |
| B11 | S3 | Visual | The opened chest's lid shows a flat, untextured pale panel above a plain slab. | Shots 020, 050, 109, 261 | The textured Soul Reliquary, open | Open |
| B12 | S4 | UI | The first level-up screen uses larger card fonts than later ones (TMP-default problem, as on character select). | Shot 003 vs 006 | Same size on every screen | Open |
| B13 | S4 | UI | The focused level-up card is sometimes dull brown instead of orange. | Shot 006 | Orange focus | Open |
| B14 | S4 | HUD | "Total Kills:" wraps to two lines at 4+ digits, so the HUD shifts. | Shots 045, 107, 134 | One line | Open |
| B15 | S4 | UI | With one choice left, the card stretches to about 225 px, and the heading and stats panel jump down. | Shot 133 | Same card size and layout | Open |
| B16 | S4 | UI | The main menu's focused Start button is pure blue; every other screen focuses in orange. | Shot 000 | One focus colour | Open |
| B17 | S4 | UI | Reroll/Skip/Banish: white text on light-grey buttons, low contrast. | Shots 003, 201 | Readable contrast | Open |

## Not bugs, or needs a clean check (observations)

| ID | Kind | What was seen | Finding |
|---|---|---|---|
| O1 | To verify | The level stayed at 123 from ~14:20 to 29:35 while 33,000 more enemies died; 400 gems on the ground from minute 4. | Past the 400-gem cap, XP goes into one merge gem (VS rule), and the bot never went back for it: a test-bot limitation. To check: that the merge gem is visible and reachable for a real player. |
| O2 | To verify | 8-10 FPS while level-up screens were open (a chain of 55 at 12:15; 99 in Run B). | The bot saved a 1920×1080 PNG on every level-up (`-sh-autoplay-levelup-shots`). Likely the tool; re-measure without per-level-up screenshots. In play the lows were 31-38 FPS in the GUI editor at 300 enemies (known). |
| O3 | Balance | Hurt 7 times in 30 min, level 123, 107,893 kills; the boss (18,450 HP) died in 52 s. | Balance, not a bug; for the balance pass. |
| O4 | Design check | The player's Pentagram erased each chest elite ~17 s after it appeared (its 60 s cooldown lined up with the elite minutes); the chest passed to the next enemy. | Implemented on purpose (2026-09-27); the wiki does not say what happens to an erased chest carrier. |
| O5 | OK | Run C: at 30:00 the screen cleared (boss and elites died, chests dropped); Stage 2 started after the chests. | Works as the owner decided. |
| O6 | OK (VS) | Chest elites at 1,3,5,7,9,10,12,14,15,16,18,20,22,23,27,29 min (16); elite/boss HP 50/150/200/250 × level; boss at 25:00 (150 × 123); first six chests 1-1-3-1-1-5, skippable from the 7th; lantern drops Coin 73, Chicken 21, Bag 14, Soul Toll 4, Orologion 3, Hellfire 3, Vacuum 1. | All match the VS rules. |
| O7 | OK | 0 exceptions and 0 game errors in 31 min (5 editor-internal lines: licensing and the update check). | Clean. |
