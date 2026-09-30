# AGENTS.md — Soul Hunter Development Operating Contract

**Project:** Soul Hunter  
**Canonical project root:** `D:\Unity Projects\Soul-Hunter`  
**Unity:** `6000.0.36f1` (`9fe3b5f71dbb`)  
**Primary platform:** Steam / Windows  
**Secondary platform:** Android  
**Status date:** 2026-09-24

---

## 1. Purpose

This file is the operational contract for AI agents working on Soul Hunter.

Use the verified current project state and approved owner decisions below as the default working truth. Do not repeatedly re-audit the entire project before normal tasks. Perform only the minimum task-specific verification needed for the requested change.

This file does not declare unverified systems complete. Static presence is not runtime acceptance. `UNKNOWN` is not `PASS`.

---

## 2. Authority Order

When sources disagree, use this order:

1. Explicit project-owner decisions and approved Soul Hunter requirements.
2. Approved implementation contracts / decision records.
3. This `AGENTS.md` operational contract.
4. Current Soul Hunter implementation.
5. Current project documentation.
6. External games, tutorials, marketplace content, conventions, or references.

Owner decision (2026-09-24): Soul Hunter is a Vampire Survivors-style game with its own story. Vampire Survivors is the reference for core gameplay: the run loop, controls, weapon/passive/evolution structure, level-up choices, enemy waves, pickups, chests, and overall game feel should match it. Where Vampire Survivors and a Soul Hunter owner decision differ (for example the run structure and the Level 3, 7 and 10 decisions below), the Soul Hunter decision wins.

Match mechanics and feel only. Do not copy Vampire Survivors code, decompiled or extracted game data, art, audio, text, or character/item names. Soul Hunter's story, characters, names, and art stay its own.

Owner rule (2026-09-28): every Soul Hunter task, however small, is based 0–100% on the VS wiki (vampire.survivors.wiki), plus the owner's story (`Docs/Game_Design_Document.md`, never edited). Look the rule up on the wiki (raw: `https://vampire.survivors.wiki/w/<Page>?action=raw`) and cite the page. Never state a VS fact from memory; if the wiki is silent or thin, say so. Ask the owner only when the wiki has no answer, it conflicts with the story or an owner decision, or it is art approval.

Owner rule (2026-09-30): "everwhere follow DOT VS ONLY". Every change is a DOT implementation (section 10) of the VS rule, and VS only. Where the wiki gives a number or rule, use it; "provisional" Soul Hunter numbers are not acceptable there. Where the wiki is silent, say so and ask the owner. Owner decisions and the owner's story above still come first.

**Owner core rule (2026-09-30, the definition of Soul Hunter):** "SH = VS + my own story + own audio, own VFX, own assets, own UI, etc. Everything should be own, but the core logic of VS must be adopted top to bottom (DOT). No mistakes. If a VS rule has to change a little, 5% is allowed."
- **From VS, top to bottom:** every rule, number, formula, timing, flow and system (stats, weapons, evolutions, level-ups, enemies, waves, pickups, chests, stages, modes, menus and what they do). Nothing is left out without a reason recorded here.
- **Soul Hunter's own:** story, names, text, characters, art, models, VFX, audio, music and the UI's look. Never VS assets, names or text.
- **5% rule (owner confirmed 2026-09-30, "Numbers ±5%"):** a VS number may be tuned by at most ±5% of its wiki value, and every such change is recorded with the wiki value next to it. Bigger or structural changes need an explicit owner decision. Approved owner decisions in section 5 stay as the recorded exceptions: one 10-stage run, no Reaper, Level 3/7/10, nearest-enemy aiming, dash and Soul Burst from the GDD.
- **No mistakes:** each rule is re-checked against the raw wiki page and the code before it is built, then proven with tests and editor play. When the wiki is silent or contradicts itself, stop and ask the owner; never guess.
- Owner decisions 2026-09-30: the dash only moves and avoids damage (its 50 damage goes); the 6 Soul Hunter-invented weapon unions are removed; XP gem colours change at 20 and 90 XP (VS 2 and 9, scaled to Soul Hunter's 10-XP gems); stage-clear revival gold stacks; missing VS items and the VS unlock system are added later; Shadow Kael's mirrored Clock Lancet slows and his Pentagram erases items. Built: see "Phase 0 of the VS DOT plan" (section 7).
- The full VS rule list and the gaps are in `Docs/VS_DOT_Table.md`; the work order is in `Docs/VS_DOT_Plan.md`.

If current code conflicts with an approved owner decision, the approved requirement is authoritative until explicitly revised.

---

## 3. Architecture

Runtime dependency direction:

`Foundation -> Gameplay -> UI`

Editor tooling may depend on Foundation, Gameplay, and UI.

Do not introduce runtime reverse dependencies from Foundation to Gameplay/UI or from Gameplay to UI without an explicit architecture decision.

Prefer explicit ownership, deterministic initialization, and testable dependencies over new hidden global lookups or singleton coupling.

---

## 4. Canonical Project Safety

Canonical project:

`D:\Unity Projects\Soul-Hunter`

### Safety rules

- Do not run `git reset`, `git clean`, destructive checkout/restore, forced rebase, or delete large file sets without explicit owner approval.
- Do not silently discard current working-tree changes.
- Do not modify canonical Soul Hunter merely to make a validation step pass.
- Risky automation/build/setup methods should execute in an isolated workspace first.
- Never delete Unity lock files or kill a Unity process merely to bypass a lock.
- Do not deploy or publish without explicit authorization.
- Preserve evidence for meaningful accepted changes.

---

## 5. Approved Product Decisions

### Level 3

Authoritative level:

**The Forgotten Village**

Signature mechanic:

**Wandering Merchant**

The Burning Village fire hazards are gone; Level 3 spawns only the merchant.

Merchant rules (owner: "apply the VS rule everywhere", 2026-09-26; VS Merchant and Golden Egg pages on vampire.survivors.wiki, checked 2026-09-26):
- The merchant waits about 7 units from the player when Stage 3 starts, on the first spot that no stage obstacle covers, at the hero's height and facing the player.
- Touching it opens the shop and pauses the game. It sells Bone and Cherry Bomb for 1,000 gold each, at level 1, never twice and never past the 6 weapon slots. It also sells Soul Eggs (Soul Hunter's Golden Egg) for 10,000 gold, repeatable.
- Gold is the saved gold (`EconomyService.SpendGold`). One source says VS spends total gold; that source is thin.
- Several things can be bought in one visit. Leaving sends the merchant away for the rest of the stage.
- The old potion and blessings are removed, because the VS merchant does not sell them.
- Prices are the VS numbers and are provisional balance.

Before this, the shop took run "souls" that nothing ever granted (`AddSouls` had no caller), so nothing could be bought. The merchant was also a gold capsule.

Soul Eggs (`SoulEggs`, `GameData.SoulEggs`, repaired by `SaveService.Migrate`):
- Each egg raises one random stat of the character it was bought for, out of 19, by the VS egg amount, with the VS caps. It counts from that character's next run (`PlayerController` and `LevelUpManager` for Reroll/Skip/Banish).
- Magnet is +1% because Soul Hunter's Magnet is a multiplier.
- The character select screen does not show egg bonuses yet (UI section).

Model:
- The owner-approved Blender model (2026-09-26) is built by `ArtSource/Generators/build_merchant.py` (previews in `ArtSource/SH10_Merchant_v01/Previews`) and imported by `MerchantModelImporter` (menu Soul Hunter/Content/Import Wandering Merchant) as `Assets/Prefabs/NPC/SH10_NPC_WanderingMerchant.prefab`, referenced by `GameContentCatalog.WanderingMerchant`.
- It has 2,694 triangles, stands 1.11x the hero's height, uses URP Lit with glowing soul and ember lanterns, and casts shadows.
- From the game camera it reads by its pack and lanterns; the cyan glow is faint.

Tools:
- `AutoplayBot` walks to the merchant, and `-sh-autoplay-gold N` sets starting gold on the throw-away save.
- Menu Soul Hunter/Debug/Jump To Stage 3/7/10 jumps there in Play Mode, for hands-on tests.

Validation:
- `WanderingMerchantTests`, `SoulEggTests` (T) and `WanderingMerchantPlayModeTests` (R: spawns near the start in the open with the approved model, the shop opens on touch, gold and slot rules hold, the egg is saved and applies next run, and the real save is unchanged).
- EditMode 317/317 and PlayMode 65/65 (`D:\SoulHunter-Validation\run-20260926-merchant-model\`, `-model2\`).
- Editor play `autoplay-editor-07` to `-09`: 0 errors, min 56-67 FPS; partial `V`.
- The owner accepted it ("merchant OK", 2026-09-26).

Seen, not changed: the hero, the horde and the merchant stand on the player's plane (y ≈ 1) while the stage floor's top is y = 0. Whether characters look like they float needs a visual check (Art section).

### Level 7

Ghost / spectral enemies:

- ordinary `Normal` damage must not damage them;
- `Holy`, `Magic`, or explicitly tagged `Spectral` damage is valid;
- existing Holy 2x vulnerability may remain a balance parameter unless changed by a later balance decision.

Weapon damage types (owner chose "type by theme", 2026-09-26). Before this only Magic Wand, Song of Mana, Santa Water and Holy Wand could hurt ghosts:

| Type | Weapons |
|---|---|
| Holy (2x on ghosts) | Cross, Bible, Santa Water, Holy Wand and their evolutions |
| Magic | Magic Wand, Song of Mana, Fire Wand, Lightning Ring, Runetracer, Peachone |
| Spectral | Soul Burst (the ultimate) |
| Normal (passes through ghosts) | Whip, Knife, Axe, Bone, Gun, Cherry Bomb, Garlic and the rest |

Pentagram erases enemies without a damage type, so it also clears ghosts. The ghost check is in `HealthController.TakeDamage` (`EnemyController.IsSpectral`); the L7 boss is a ghost too.

Validation: `SpectralStagePlayModeTests` (R, real Stage 7): each Holy/Magic weapon hurts a ring of ghosts, each Normal weapon passes through them, Soul Burst reaches them, and the L7 boss ignores Normal damage and takes Holy at 2x. Results: see "Castle corridor" below (same run). That run also found and fixed Soul Burst's empty enemy mask: the ultimate had never hurt anything.

Editor play `autoplay-editor-27` (Stage 7 jumped to at level 1, god mode, 3 min): 0 errors, min 39 FPS. With only Kael's Whip (Normal) there were 3 kills in the first 30 s while the ghost horde grew to 229-300 (ghosts take no knockback). Once Bible and Lightning Ring came from level-ups, their hits showed on the ghosts, reaching 1528 kills and level 20 (partial `V`). In the one-run campaign a player arrives at Stage 7 with more weapons; whether a Normal-only loadout should be this helpless is a balance question for the owner.

Not yet played by the owner.

### Level 10

Boss:

**Shadow Kael**

Authoritative behavior:

Shadow Kael mirrors the player's combat loadout. At encounter start, the relevant player weapon/stat loadout must be captured and represented by the boss according to the implementation contract.

Implementation (2026-09-26):
- Capture: `EnemySpawner.SpawnBoss` calls `ShadowKaelController.CaptureFrom(player)` when the Stage 10 boss spawns. The copy is fixed from then on; later player upgrades do not reach the boss.
- Stats: the boss gets its own `PlayerStats` copy (`PlayerStats.CopyFrom`, no second meta bonus), so every mirrored weapon scales like the player's own: Might, Cooldown, Area, Amount, Duration, Speed, Luck, crits and bounces. Armor reaches the boss's `HealthController` through that copy. Move speed multiplies the stage speed.
- Weapons: `WeaponManager.GetEquippedWeapons()` lists the weapons the player is really using, at their levels: evolved and union weapons are included, and base weapons they replaced are left out. The boss is drawn 3x bigger, but its weapons keep the player's size.
- Revivals: a lethal hit with mirrored revivals left restores the boss through `HealthController.TryPreventDeath` without firing `OnDied`. A revival no longer clears the stage, wins the run, drops a chest or counts as a kill.

Defects found and fixed in the process:
- The boss mirrored only Might, Cooldown and Area, by multiplying base numbers. Area was applied as the weapon's transform scale.
- It mirrored a base weapon instead of its evolution.
- Any revival fired `OnDied`, so Stage 10 cleared on the first "death".
- 14 of 19 mirrored weapons never hurt the player:
  - Pooled `Projectile`s ignored the player, and pooled `ProjectileDamage` kept the "Enemy" tag. `AutoAttackWeapon.AimAtHostiles` now retargets every spawned object, and `Projectile.TargetsPlayer` resets on reuse.
  - Axe and Cross switched themselves off unless their parent was the player.
  - Knife and Thousand Edge needed the player's body to aim.
  - Magic Wand, Cross, Lightning Ring, Crimson Shroud and Bloody Tear picked targets from the enemy list.
- A boss-held Pentagram wiped the boss's own horde and the boss itself, and a boss-held Clock Lancet froze them. Both now skip enemies.

The player has no frozen state, and "wiping" the player is not a VS mechanic, so Shadow Kael's Clock Lancet only shows its beam and his Pentagram only flashes (open question in `Docs/VS_DOT_Plan.md`, Phase 0).

Tools: `LevelProgressionManager.SkipToBoss()` (menu Soul Hunter/Debug/Skip To Stage Boss, bot option `-sh-autoplay-boss-after S`). With it, the bot records a `shadow-kael` event listing the mirrored weapons, plus screenshots.

Validation:
- `ShadowKaelMirrorTests` (T): stat copy, evolved weapon, boss scale, armor, and a revival without `OnDied`.
- `ShadowKaelPlayModeTests` (R, real Stage 10):
  - the loadout is copied exactly and fixed at encounter start;
  - each of the 19 weapons held by the boss hurts the player (Laurel, Clock Lancet and Pentagram excepted, as above) and never hurts or freezes the boss or its horde;
  - a revival neither clears the stage nor wins the run, and the last death does.
- EditMode 319/319 and PlayMode 86/86, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-shadowkael\`).
- Editor play `autoplay-editor-11` and `-12` (Stage 10, god mode, boss after 90 s and 75 s): both times Shadow Kael copied the player's 6 weapons exactly (for example Whip, Gun, Fire Wand, Song of Mana, Axe Lv2, Lightning Ring), with 0 errors. A screenshot shows the boss's mirrored Lightning Ring striking the hero (partial `V`).
- Shadow Kael uses the generic Level 10 boss model, not a dark copy of Kael (Art section).

Seen, not changed:
- The boss died in about 5 s at player level 6, because its health was 50 x player level. Since 2026-09-27 (owner's story) it is 500 x player level, and at least twice a Stage 10 chest elite (4,250); see "Elites and bosses in play".
- A level-1 hero dropped into Stage 10 dies in about 11 s.
- The GUI editor ran at 17-38 FPS with 300 Stage 10 enemies before the boss appeared (Performance section).

Not yet played by the owner.

### Run structure

Owner decision (2026-09-24): one run plays through all 10 levels in order. This intentionally differs from Vampire Survivors' separate per-stage runs.

~~Killing a stage's boss clears that stage and starts the next one.~~ Superseded by the owner on 2026-09-27, after VS research. vampire.survivors.wiki/w/Mad_Forest: a 30-minute run; chest bosses at 1, 3, 5, 7, 9, 10, 12, 14, 15, 16, 18, 20, 22, 23, 25 and 29 minutes, plus Arcana-only chests at 11 and 21; at 30:00 the screen is cleared and the Reaper comes.
- Every stage lasts the full 30 minutes, like VS. The stage boss comes at 25:00, and killing it no longer ends the stage (`LevelProgressionManager.IsBossDefeated`).
- At 30:00 the screen is cleared and the next stage starts after a short transition. There is no Reaper; the run moves on instead.
- Killing the Level 10 boss (Shadow Kael, the end of the story) still wins the run.
- Chest elites come at the VS minutes 1, 3, 5, 7, 9, 10, 12, 14, 15, 16, 18, 20, 22, 23, 27 and 29 (`EnemySpawner.DefaultChestEliteMinutes`), 16 per stage. They were every 5 minutes before the boss, and they also come during a boss fight.
  - My first list (1, 5, 10, 11, 15, 18, 20, 21, 29) came from a summarised page fetch and missed rows. The raw wave table gives the list above; the owner chose "the VS minutes", so the corrected list follows that choice.
- A 10-stage run is now about 5 hours.

**The ending (owner, 2026-09-27: "study the full story then finalize the death").** The story says: "The final boss is his own shadow. To free his daughter's soul, he must destroy himself." So:
- Level 10 (The Soul Core) has no time limit. 30:00 no longer wins. Only destroying Shadow Kael ends it (`LevelProgressionManager.CompleteStageAfterSurvival` skips the last stage). In Endless mode, his death starts the next cycle.
- The victory screen reads "SHADOW KAEL DESTROYED" with the twist's own last sentence (`CampaignStory.FinalLine`). It said "ALL TEN STAGES SURVIVED / The pact has reached its end.", which was not the owner's story.
- Tests: `RunModeTests` (T: 30:00 on Level 10 neither wins nor starts a new cycle; the boss kill does) and `CampaignStoryTests` (T: the line is the GDD's).
- EditMode 399/399, PlayMode 147/147, save untouched (`D:\SoulHunter-Validation\run-20260927-ending\`).
- Editor play `autoplay-editor-41`: Level 1, then a jump to Level 10 and a clock skip to 29:50; god mode, 0 errors, min 57 FPS.
  - After 30:00 the run went on at Level 10 ("The Soul Core has no time limit").
  - Shadow Kael (4,500 health) copied the player's 6 weapons.
  - The level-10 bot did not kill him in 3 minutes, so the victory screen was not seen in play; it is covered by tests only.
- Not yet played by the owner.

Do not split the campaign into separate per-level runs or add a Reaper-style run end without a new owner decision.

### Weapon aiming

Owner decision (2026-09-24): all of the player's directional attacks fire toward the nearest enemy (`AutoAttackWeapon.AimDirection`), falling back to each weapon's previous direction when there is no enemy. This intentionally differs from Vampire Survivors, where Knife, Whip, Axe and others follow facing or random directions. Applied to Song of Mana (piercing Magic shots), Knife, Thousand Edge, Whip, Bloody Tear, Axe, Fire Wand, Holy Wand, Gun (one arm aimed, 4-way pattern kept), Cherry Bomb, Death Spiral (first scythe aimed), Bone, Runetracer, Clock Lancet, Santa Water (flask lands on the target) and Lightning Ring (nearest enemies first). Non-directional weapons are unchanged (Garlic, Bible, Laurel, Pentagram, Peachone). A boss's mirrored copy aims at the player. Validated by `WeaponAimingTests` (T) and `PlayerLoadoutPlayModeTests` (R); Axe is code-reviewed only (its throw needs a physics step).

Gun, Cherry Bomb and Death Spiral previously built directions as `Vector2`, which became (x, y, 0) and fired vertically instead of across the ground; they now aim on the ground plane.

---

## 6. Performance Contract

Primary planning/release baseline:

- platform: Steam / Windows
- resolution: `1920x1080`
- quality: Medium
- target: `60 FPS`
- hard minimum: `30 FPS`
- low-end planning reference: Intel Core i5-8350U + Intel UHD 620 + 8 GB RAM

Android is secondary. Detailed Android device tiers and budgets remain a later platform-specific acceptance definition.

Do not claim performance acceptance without representative profiling.

Performance smoke test (2026-09-25): `PerformanceBenchmark` runs in a player build launched with `-sh-benchmark <results.json>` (never in a normal launch). It switches to Medium with V-Sync off, renders the 3D view at 1080p's pixel count (URP render scale when the window is smaller), plays as an invincible Kael who keeps walking, takes the first level-up choice and closes chests, and measures 30 s after an 8 s warm-up in three segments. The Windows build (`SoulHunterBuildRunner`, 132 MB, built in the isolated copy) was run on this machine, which is the low-end reference (i5-8350U, UHD 620, 16 GB), in a 1366x768 window rendered at 1920x1080:

| Segment | Enemies | Avg FPS | 1% low FPS | Frames > 33 ms |
|---|---|---|---|---|
| Stage 1 opening | 30 | 226-233 | 152-160 | 0 |
| Stage 1 at 20:30 | 117-184 | 121-141 | 84-88 | 1 |
| Stage 10 at 20:30 | ~300 | 85-88 | 45-51 | 0-1 |

Results are in `D:\SoulHunter-Validation\run-20260925-gaps2\bench1080-1.json` and `-2.json`; the save file was unchanged. The very first launch also had one 152 ms hitch in Stage 10 (likely first-run shader or cache work); it did not repeat. This is partial `P` evidence: 30 s segments, not a full 30-minute run, UI drawn at 768p, V-Sync off, 16 GB instead of 8 GB, and no Android device. The Standalone default quality in `QualitySettings` is still Ultra.

---

## 7. Known Current Defects / Risks

Treat the following as known risks until corrected and revalidated.

### Per-stage EnemyData and elites (fixed statically, runtime unverified)

Each level's Enemy, Elite and Boss gameplay prefab now has its own EnemyData asset in `Assets/_Project/Data/Config/Enemies/`. Bosses drop chests, contact damage comes from `DamageToPlayer`, and stage-boss health comes from its EnemyData. `Enemy_Entity.prefab` (the base template) still uses `Bat_Enemy_Data.asset`.

Elites spawn from `Assets/Prefabs/Enemies/Gameplay/SH10_L01`–`L10_EliteGameplay.prefab`, built like the enemy gameplay prefabs with the elite model as the `SH10_Visual` child. `SH10_Lxx_Elite.prefab` stays a visual-only model used by the showcase scenes.

The stat values are provisional balance numbers. `EnemyDataWiringTests` covers the wiring and passed (T) on 2026-09-24; elite and boss behaviour in play is still unverified (R).

### Bible prefab defect

`Bible_Weapon.prefab` previously had `_biblePrefab` referencing its own root object, and historical runtime evidence included Bible-related exceptions.

`_biblePrefab` then pointed at `SH10_Bible.prefab` with fileID `100100000` (the prefab asset, not its root GameObject), so spawning books still threw `InvalidCastException` (seen 2026-09-24 when a PlayMode chest reward granted the Bible). Fixed in `Bible_Weapon.prefab` and `UnholyVespers.prefab` to the root GameObject (`5068360835409548677`); no other field in the project used that fileID form.

Validated 2026-09-24: `BiblePrefabTests` (T) and the full PlayMode suite with 0 exceptions (R). Bible behaviour in hands-on play is still unverified.

### Whip enemy mask defect

`Whip_Weapon.prefab` shipped with an empty `_enemyLayer`, so the Whip never hit anything. It surfaced once character starting weapons were fixed (Antonio, now Corvan, starts with the Whip). `WhipWeapon` (and `GarlicWeapon`, defensively) now default an empty mask to the Enemy layer. Validated by `PlayerLoadoutPlayModeTests` (R): Corvan is visible, starts with the Whip, and the Whip damages an adjacent enemy.

### All-character sweep (2026-09-24)

`CharacterSweepPlayModeTests` plays every catalog character (13) in the real game with the spawner off and checks: model visible, starting weapon equipped, it damages an enemy 3 units away, the player loses no health, and no errors are logged. All 13 pass. The first run found three more defects, now fixed:

- Bible (Lumina): the `SH10_Bible` book model has no collider, so adding `TouchDamage` (which requires one) returned null and threw `NullReferenceException`. Books now get a trigger collider first.
- Bible: disabling the weapon (scene unload, or evolving into Unholy Vespers) reparented its books while deactivating, which Unity rejects with an error. Books are now only hidden on disable.
- Axe (Gideon): Vampire Survivors' up-the-screen arc had been built as a 3D height arc, so axes flew ~7 units over nearby enemies and landed ~15 units away. Axes now fly along the ground toward the target (gravity off, still spinning); speed 12 is provisional.

### Weapon self-damage defect

`TouchDamage.TargetTag` defaults to "Player" (it was written for enemy contact damage). Song of Mana and Bloody Tear never changed it, so their beams/whips damaged the player; Aria (Song of Mana) died within seconds of starting. All TouchDamage weapons now use `AutoAttackWeapon.HostileTag`: "Enemy", or "Player" when a boss holds the weapon (Shadow Kael's mirror). Validated by `WeaponTargetingTests` (T) and `PlayerLoadoutPlayModeTests` (R: Aria takes no damage from her own beam).

### Pause / time-scale conflicts

Pause menu, level-up, chest, Wandering Merchant, game over, Soul Burst hit-stop and stage start each wrote `Time.timeScale` directly, so one resumed the game underneath another that was still open: toggling pause twice during a level-up/chest/shop, Soul Burst's slow motion ending, or the next stage starting 5 s after a clear. All of them now go through `SoulHunter.Core.Services.GameTime` (reference-counted `Pause(owner)`/`Resume(owner)`, `SetSlowMotion`, `ResetAll` only at run start, restart and scene change). Do not write `Time.timeScale` directly in gameplay code. Validated 2026-09-24 by `GameTimeTests` (T), EditMode 235/235 and PlayMode 20/20 (R) in the isolated copy; hands-on play still unverified.

This came from a review of an external "Vampire Survivors engine" write-up (owner direction: adopt what is officially documented, follow the rest). Officially documented mechanics were already present statically (time survival, hordes, levelled weapons, gems that never despawn, persistent gold power-ups). Of the engineering recommendations, pooling (`WeaponPoolManager`, `PickupPoolManager`), data-driven enemies (`EnemyData`), damage data (`DamagePacket`), death events (`HealthController.OnDied`), multi-level-up and a stage clock already existed; the missing run-state/pause arbitration is what `GameTime` adds.

### Enemy minimum (2026-09-24)

A second write-up (VS Stage 2, Inlaid Library) was reviewed under the same direction. Adopted: the VS "enemy minimum". Each wave phase has `MinimumEnemies`, and `EnemySpawner` refills the swarm up to it immediately (at most `_maxMinimumTopUpPerFrame` = 8 per frame), on top of the interval spawns; before this the horde only grew by one group per interval, so fast killing emptied the screen. The automatic 5-minute phases use `WaveData.MinimumEnemies` (30) + `MinimumEnemiesPerPhase` (20) per phase: 30/50/70/90/110, provisional balance. Validated by `EnemyMinimumTests` (T) and `EnemyMinimumPlayModeTests` (R: opening horde reaches the minimum and refills after a full wipe); EditMode 237/237, PlayMode 21/21. Feel and performance at these counts are unverified (`V`, `P`).

Not adopted: Stage 2 unlocking at player level 20 (conflicts with the one-run 10-level structure); Inlaid Library's wave tables, enemy/item/relic names and ×1.25 speed (VS game data, and taken from the fan wiki rather than poncle); its corridor and left/right spawning (Soul Hunter Level 2 is The Dark Forest).

### Enemy crowd separation (owner report, 2026-09-26)

The owner saw the horde chase the player in single file ("rail ka dabba"), where VS shows a wide crowd. Every enemy homed on the player at the same speed with trigger bodies and nothing kept them apart, so they merged onto one path. VS research (vampire.survivors.wiki; the crowd-pushing part comes from thin forum sources and memory) says enemies push each other into crowds.

`EnemyCrowd` now gives each enemy a soft push away from the enemies it overlaps: 1 - distance / (sum of body radii), capped at length 1. Bodies stay triggers, so the horde never walls the player in. `EnemyChaseState` steers toward the player plus the push × 1.2 (provisional). A crowded enemy slows down but never goes faster than its move speed. Next to the player, enemies still make room for each other. Body radius is the collider radius × scale, so chest elites and bosses push from further out. Event fliers are left out.

Cost: once per rendered frame, the horde is read into arrays, sorted into a flat grid and every push is computed in one pass. The first version took 7.8 ms per step for 300 packed enemies (editor debug scripts) and dropped Stage 10 editor play to 19-27 FPS. The array version takes ~1.15 ms there. `-sh-crowd-off` (or `EnemyCrowd.Enabled`) switches it off for A/B checks.

Validation:
- `EnemyCrowdTests` (T): push maths, stacked pairs part in opposite directions, far enemies unaffected, bigger bodies, and a timing budget.
- `EnemyCrowdPlayModeTests` (R: 40 enemies chase a walking player). Old behaviour: width 0.07, 100% stacked. Now: width 0.98 (about four bodies side by side), 0% stacked.
- EditMode 324/324, PlayMode 87/87, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-crowd\`).
- Editor play `autoplay-editor-13` (Stage 1, 0 errors, min 55 FPS) and `-14` (Stage 10, god mode): the old line became a wide, dense crowd (partial `V`). `AutoplayBot` timed shots now skip paused screens; level-ups every ~15 s used to hide every shot.
- Windows build benchmark (reference laptop, 1080p pixel count, Medium), Stage 10 at 20:30 with ~300 enemies:
  - separation on: 68.2 / 79.4 FPS average, 1% low 32.9 / 61.1;
  - separation off: 84.5 / 76.5 FPS average, 1% low 56.4 / 48.3.
  - The runs are noisy. Separation looks to cost about 0-10% on average. Every run is above the 30 FPS hard minimum, but one 1% low (32.9) is close to it (partial `P`).

Mistake during validation: the build ran without `SOULHUNTER_BUILD_OUTPUT_EXE` and overwrote `D:\Unity Builds\SoulHunter\` (created 2026-09-23). The benchmark copy lives in the run folder.

Part 2, movement types (2026-09-26):
- `EnemyData.Movement` follows the VS movement skills (vampire.survivors.wiki, Enemies):
  - `Homing` (default).
  - `Floaty`: homes while weaving sideways by `FloatyWeave` (0.7) × sin over `FloatyPeriodSeconds` (1.8 s), with a random phase per enemy (provisional values).
  - `FixedDirection`: when spawned, it aims at the player and keeps that line at its own speed. It is not relocated or pushed by the crowd, and it returns to the pool without drops once it has crossed the screen.
- `EnemySpawner.SpawnEnemy` calls `EnemyController.BeginMovement()`. `EnemyFlyState` takes a speed multiplier: 1.5 for event formations, 1 for Fixed Direction.
- Per the approved table, the normal enemies of L2 Dark Forest and L7 Sea of Lost Souls are Floaty. Everything else homes. Fixed Direction is ready but no stage uses it yet (Part 4).

Found while testing Part 2:
- `EnemySpawner` stopped following stage changes once it had been disabled: it unsubscribed in `OnDisable` and only `Start` subscribed again. Game code never disables it, but tests that disabled it kept the old stage's enemies and boss. It now unsubscribes in `OnDestroy`.
- PlayMode tests read the owner's real save, which now selected Kaelen. Kaelen's starting weapon broke tests written for Kael (7 failures). `GameplayPlayModeTestBase.BootIntoGameplay` now plays as Kael and sends every save of the run to a throw-away file.

Part 2 validation:
- `EnemyMovementTests` (T): weave maths, and the approved movement per stage asset.
- `EnemyMovementPlayModeTests` (R): Stage 1 enemy moved 0.00 sideways; Stage 2 Floaty enemy weaved 0.73-0.88 and both reached the player; a Fixed Direction enemy kept its line when the player stepped 5 units aside, then left.
- EditMode 326/326, PlayMode 89/89, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-movement\`).
- Editor play `autoplay-editor-15` (Stage 2, god mode): 0 errors, min 49 FPS, enemies spread around the player (partial `V`). Only up to 39 enemies were alive at once on Stage 2 (Part 3).

Part 3, VS minute waves (2026-09-26):
- Correction: every stage already had its own WaveData (spawn interval 1.0-0.35 s and group size 1-6, rising by stage), not one shared file as first reported. What they shared was the automatic shape: the minimum rose 30/50/70/90/110 every 5 minutes, never falling.
- `WaveData.Minutes` (`MinuteWave`: minute, minimum, interval, elite bonus) is a VS minute table. `GetPhase` reads it when no Phases are authored, and `EnemySpawner` drops the 5-minute group growth when a table is present.
- `WaveTables.Build` gives each stage a 25-minute table (the boss comes at 25:00), all numbers provisional:
  - opening minimum 15 + 5 per stage, peak 150 + 16 per stage (at most 290);
  - a fast build-up (t^0.6) with a ±12% swell that differs per stage;
  - interval from 1.0× down to 0.4× the stage's `SpawnInterval`;
  - every fifth minute, shifted per stage, is quiet: 30% of the minimum and 1.5× the interval, but 30-50% extra elite chance (VS: Mad Forest drops to 10 at 5:00 with tougher enemies).
- Stage 1 opens 17, 39, 47, 50, 16 (quiet), 62 ..., close to VS Mad Forest (15, 30, 50, 40, ..., 10 at 5:00).
- `WaveTableBuilder` (menu Soul Hunter/Content/Build Wave Tables) writes the tables into the 10 stage assets (`WaveTables.StageWaveAssets`).
- Validation:
  - `WaveTableTests` (T): the assets match the builder; every stage rises and has at least 3 quiet minutes with elites; stages differ; `GetPhase` reads the minute.
  - `WaveTablePlayModeTests` (R): the horde swells to a busy minute's minimum; after a wipe in a quiet minute it refills only to the smaller one.
  - EditMode 337/337, PlayMode 103/103, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-waves2\`).
  - Editor play `autoplay-editor-21` (Stage 1, 6 min, god mode): the horde followed the table, 17 → 39 → 48 → 64 → 50 → 16 in the quiet 4:00 minute → 62 → 75, with 0 errors and min 75 FPS.
- Seen, not changed (balance): that run had 229 kills and reached level 8 in 6 minutes; the run before the retune had 1432 kills and level 19. The XP pace changed and needs the balance pass.

Also on 2026-09-26 (owner):
- The Stage 4/5 damage discs were removed (`CampaignHazard` deleted; `PressurePlatePlayModeTests.CastleAndSwamp_HaveNoDamageDiscs_AroundTheStart`).
- The save's Currency was set back from 16004 to 16002 (backup `soulhunter_save.before-gold-fix.json`).
- `AutoplayBot` checks the chest panel with `!= null` (an unassigned `ChestUI._chestPanel` threw in editor play).

Part 4, stage patterns (2026-09-26), following the approved table:
- `WaveData.SpawnSide` / `SideBias`: L4 Ruined Castle spawns 80% from the left/right (world ±X), L6 Frozen Peaks 80% from above/below (±Z), within a 70° fan, and all other stages from all sides (`WaveTables.SideFor`, `SpawnDirection`). Stage-event formations come from the stage's sides too.
- VS rule: a spawn never lands inside a stage obstacle; `PickSpawnPosition` tries up to 6 directions against `GroundCheck`.
- Events are authored per stage by `WaveTables.Events`, written into `WaveData.Events` (all provisional):
  - five events at 2:30 + 5-minute steps, counts 16 upward, 10 upward for the L8 elites;
  - L1 swarms; L2 mostly closing rings; L3 mostly walls; L4 walls; L5 swarms at 1.8× event speed (`StageEventEntry.Speed`); L6 mostly swarms; L7 mostly (ghost) swarms; L8 rings; L9 walls and rings alternating;
  - L10 every kind, nine events every 2:30, counts +8.
- Pace (`CampaignLevelCatalog`): L5 speed 1.25 → 0.9 ("a slow, heavy horde and fast swarms"), L6 1.15 → 1.45. The L4 and L5 modifier texts no longer mention traps or poison zones.
- Stragglers (beyond 40 units) still re-enter ahead of the player on every stage. On a side-rule stage, making them re-enter from the sides let a player who ran up or down leave the whole horde behind (editor play: 9 kills in 3 minutes on the Castle), because the Castle was not yet a walled corridor like VS's Inlaid Library. The owner then approved a real corridor (see "Castle corridor" below).
- Validation:
  - `StagePatternTests` (T): sides, events, assets and pace follow the table; the direction sampler keeps more than 75% to the sides.
  - `StagePatternPlayModeTests` (R): Castle spawns more than 65% left/right; Peaks more than 65% above/below and fast; no village spawn inside a house; Swamp slow with fast swarms.
  - `StageEventPlayModeTests.ScheduledSwarm_FiresOnTheStageClock` now reads the stage's authored first event (16, not the old default 20).
  - EditMode 342/342, PlayMode 107/107, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-patterns3\`).
  - Editor play `autoplay-editor-22` (Stage 4, god mode, 3 min): 0 errors, min 43 FPS, up to 233 enemies. The fleeing bot drags the horde into one blob behind itself, as in VS.
- Seen, not changed: stage events are not capped, so a run can pass 300 enemies briefly (316 seen).

Part 5, VS knockback (2026-09-26, vampire.survivors.wiki Knockback):
- A hit reverses an enemy's movement for 120 ms (`EnemyController.ApplyKnockback`; `EnemyChaseState` moves it away from its target) at move speed × weapon knockback (`DamagePacket.WeaponKnockback`, default 1) × the enemy's knockback taken.
- Knockback taken comes from `EnemyData.Knockback`: 1 for ordinary enemies, 0 for the L7 Sea of Lost Souls ghosts (VS ghosts take none), 0.5 for elites and 0 for bosses (provisional). Bosses are also immune through `KnockbackResistance`. Fliers and frozen enemies are not pushed.
- Garlic pushes weakly (0.3; provisional) but raises an enemy's knockback taken by 0.3 per hit, up to `MaxKnockback` 3 (VS). The value resets when a pooled enemy is reused.
- The old 0.15 s transform slide (`HealthController.KinematicKnockback`), which fought the chase velocity every physics step, is gone. **The player is no longer knocked back:** it used to slide 2 units whenever an enemy touched it, which VS never does.
- Validation:
  - `KnockbackTests` (T): the formula; the per-enemy data; the Garlic cap; a non-enemy never slides.
  - `KnockbackPlayModeTests` (R): a hit enemy backs off, then comes on again after about 120 ms; an L7 ghost is not pushed; Garlic raises knockback taken; the player does not slide when touched.
  - EditMode 346/346, PlayMode 111/111, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-knockback\`).
  - Editor play `autoplay-editor-24` (Stage 1, 3 min, no god mode): the hero survived, reaching level 13 with 670 kills, 0 errors and min 63 FPS.

The owner-approved crowd plan (Parts 1-5) is done. It is not yet played by the owner.

Each stage still has only one normal enemy type and one elite; new enemy models are Art section work.

Not yet played by the owner.

### Castle corridor (owner-approved, 2026-09-26)

L4 Ruined Castle is now a walled corridor like VS's Inlaid Library ("a long hallway ... that restricts movement to left and right"):
- `StageCorridor` (Data/Config) holds the corridor: it runs along world X, centred on the player's Z when the stage starts, with an open half-width of `WaveTables.CorridorHalfWidth` (9 on Stage 4, 18 units wide; 0 = open ground). The next stage ends it.
- `CorridorWalls` (added by `GameSessionManager`) lays 7 castle-wall modules of 20 units along each side and keeps them around the player, so the hallway never ends. The walls are on the `Obstacle` layer: they block the player only; enemies, weapons and projectiles pass over them.
- Everything else keeps inside the corridor: enemy spawns, stragglers, stage events and the boss (from one end) in `EnemySpawner`; Soul Lanterns come only from the left and right screen edges; stage items lie along the corridor near a wall each, and the four Rune Plates lie along it, two each way.
- Stage 4 spawns 100% from the ends (`WaveTables.SideBiasFor`, `Castle Gargoyles.asset` `SideBias` 1).
- Model: `build_light_source.py` also builds the castle wall (crenellated stone with ember slits), imported by `LightSourceImporter` as `Assets/Prefabs/Props/SH10_CastleWall.prefab` (`GameContentCatalog.CastleWall`). **The design is not yet owner-approved.**

Validation:
- `CorridorTests` (T): only Stage 4 is a corridor, ends-only spawning, clamping, a solid 20-unit wall on the Obstacle layer.
- `CorridorPlayModeTests` (R): the walls stop the player going up or down and follow it left and right; every enemy, item and plate is inside the corridor; leaving the Castle opens the ground again.
- Together with Level 7's `SpectralStagePlayModeTests`: EditMode 349/349, PlayMode 133/133, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260927-l7corridor2\`).
- The first run of that suite found that Soul Burst's `_enemyLayer` was empty in `Player.prefab` and `Main_Gameplay`, so the ultimate had never hurt any enemy. `SoulBurstController` now defaults an empty mask to the Enemy layer, as `WhipWeapon` does.

Editor play `autoplay-editor-26` (Stage 4, god mode, 3 min, after the fixes): both walls on screen, the horde inside the hallway, 478 kills, level 13, 0 errors, min 63 FPS (partial `V`).

Seen, not changed: in the earlier editor play `autoplay-editor-25` (Stage 4 jumped to at level 1, god mode) the bot pressed against a wall and kills nearly stopped after 20 s (61 → 74 by 3:00): level-1 weapons (10 damage) against 25-HP Castle enemies. Enemies that chase the player cross the walls, so some stood outside the corridor.

Not yet played by the owner.

### VS stage rules A to Z (owner request, 2026-09-27)

Owner: "ALL STAGE KE BARE MEIN A TO Z PATA KARO ... OR APPLY KARO", then "complete the table" and "extra feature also continue".

Research from vampire.survivors.wiki, fetched 2026-09-27; raw pages are in the session scratchpad under `vs/stages/`:
- the stage system (time limit, stage bonuses, waves, the 300 enemy cap, map events, modes);
- 16 stages, stage by stage;
- chests, pickups, light sources and the level-up screen.

Applied from the gap table (the "extra" features are listed at the end):
1. **Chests** (`ChestLogicController`):
   - Every chest pays VS gold on top of its items: 1 item 100–200, 3 items 300–600, 5 items 500–1000, then Greed. Before, only an empty chest paid a flat 100.
   - An item with nothing left to upgrade pays 25 gold (Big Coin Bag).
   - At most one evolution per chest. Only chests dropped at 10:00 or later, or at 1:00, can evolve (VS Mad Forest evo flags; `CanEvolveAt`, `ChestPickup.CanEvolve`, set by `EnemyDrop`).
   - Not tested: one evolution per chest, and a chest from before 10:00 not evolving, need a full evolution setup. Covered by code review and editor play.
2. **Stage bonuses** (`CampaignLevelCatalog.PlayerSpeedFor` / `GoldFor`, applied in `PlayerController.MoveSpeed` and `GoldRewards.ApplyGreed`):
   - Player speed per stage: ×1.10, 1.25, 1.25, 1.25, 1.40 … 1.40.
   - Gold per stage: ×1.0, 1.0, 1.2, 1.3, 1.4, 1.5 … 1.5.
   - This follows the VS stage list (Mad Forest ×1.1 speed … Cappella Magna ×1.4, DLC stages ×1.5 gold). Values are provisional.
   - Minutes 25–29 keep minute 24's peak wave (VS keeps 100–300 there).
3. **Map events, VS-dense** (`WaveTables.Events`, `StageEventEntry.Repeat` / `RepeatDelaySeconds` / `ChancePercent`, `EnemySpawner.EventFires`):
   - Most minutes from 2:00 to 12:00: 80% chance, fired twice.
   - A ring or wall at 5, 10, 15 and 25.
   - 70% chance with repeats at 13, 17 and 22.
   - Guaranteed repeated swarms at 20, 27 and 29.
   - The chance is divided by Luck, and 100% means guaranteed.
   - Kinds per stage keep the approved table. Events now also run during a boss fight.
   - Firings the Debug clock skipped more than 10 s past are dropped.
   - The 10 stage wave assets were rebuilt with `WaveTableBuilder`.
4. **New light-source pickups** (VS Rosary, Nduja Fritta Tanto, Little Clover and Gilded Clover, with the VS weights and levels, in `LightSourceDrops`). Soul Hunter names are provisional:
   - **Soul Toll** (weight 1, from level 8): kills every enemy on screen; stage bosses are immune. Normal drops (`ScreenWipePickup`).
   - **Hellfire Breath** (weight 1): a temporary weapon (`FireBreath`) for 10 s × Duration, not a weapon slot, and not copied by Shadow Kael.
     - Every 0.5 s it fires 12 (+ Amount) piercing flames, 30 × Might damage each, in a 30° cone at the nearest enemy.
     - The flames deal Magic damage like the Fire Wand, so they hurt Level 7 ghosts.
     - Speed 9 and the 30° cone are Soul Hunter's own numbers; VS gives only "speed 300%".
   - **Luck Charm** (weight 1): +10% Luck for the run.
   - **Gilded Charm** (weight 1, from level 30): pulls every coin on the ground to the player and starts a 10 s Gold Fever (`GoldFever`).
     - During the fever, each kill has a 75% chance to drop a coin worth 0.5 × Greed (at least 1).
     - Picking up a coin, coin bag or rich coin bag adds 0.5, 5 or 10 s, capped at 10 s.
   - The models are placeholder primitives from `ExtraPickupBuilder` (menu Soul Hunter/Content/Build Extra Pickups): `Assets/Prefabs/Items/SH10_Pickup_*.prefab`, referenced in `GameContentCatalog`. **Not owner-approved art.**
   - Not mapped: Luck raising rare light-source drops (no formula on the wiki); Orologion slowing the fever.
5. **Stage clear gold** (`LevelProgressionManager.StageClearGold`): 500 gold, plus 100 per unused revival, when a stage is cleared at 30:00 (or the final boss dies). Greed and the stage gold bonus apply.

Validation: EditMode 377/377; PlayMode 140/140, then the stage fixtures 47/47 after the L10 schedule fix; save untouched (`D:\SoulHunter-Validation\run-20260927-stagetable\`). Committed in `dba31112`.

Editor play `autoplay-editor-37` (Stage 1, no god mode, 6 min, clock skipped to 9:50 at 60 s): 0 errors, level 25, 2,427 kills, min 44 FPS.
- Chest elites came at 1:00 and at 10, 12 and 14 minutes.
- Each chest paid its upgrade plus 100–200 gold.
- Swarms and rings came about every minute.
- The skip made the 3, 5, 7 and 9-minute elites arrive together (a Debug-skip artifact).

Not yet played by the owner.

### Scalability: adding content (owner request, 2026-09-28)

The owner asked for three improvements: wave enemy types, boss ability components and a single weapon definition asset.

**1. Wave enemy types.** VS wiki, Enemies, "Waves": "Each wave specifies a minimum amount and a spawn interval ... If the minimum amount is not met ... enemies will be spawned until the quota is filled. If more enemies than the minimum amount is present, one of each type of enemies in the wave will be spawned."
- `MinuteWave.EnemyTypes` (enemy gameplay prefabs) reaches `WavePhase.EnemyTypes`.
- `EnemySpawner` fills the minimum with the wave's types in turn (`NextWavePrefab`) and, above the minimum, sends one of each type per tick.
- An empty list keeps the old behaviour (the stage's own enemy with the elite chance), so no stage changed yet: new enemy models are Art section work.
- To add an enemy type:
  1. Make its `EnemyData`.
  2. Make a gameplay prefab like `SH10_Lxx_EnemyGameplay`.
  3. List it in `WaveTables.EnemyTypesByStage` (stage → from-minute → prefab paths).
  4. Run Soul Hunter/Content/Build Wave Tables, which checks every path is an enemy with EnemyData.
  - No scene or spawner code changes.
- Tests: `WaveEnemyTypeTests` (T) and `WaveEnemyTypePlayModeTests` (R: a two-type wave fills its minimum with both types, then sends one of each per tick).
- EditMode 410/410, PlayMode 151/151, save untouched (`D:/SoulHunter-Validation/run-20260928-wavetypes/`).

**2. Enemy and boss abilities, data-driven** (VS behaviour catalog from the wiki boss and enemy pages, 2026-09-28):
- `EnemyData.Abilities` is a list of `EnemyAbilitySpec`, run by `EnemyAbilities` (added to every enemy, pooling-safe, pause and freeze aware). The kinds, with their VS sources:
  - **Summon:** Trickster, Directer, Avatar of Gaea.
  - **Shoot:** Twin Skulls 1 s / speed 250 / 25; Twin Snakes 2 s / 140 / 20; The Ender's interval shrinks with health.
  - **Lunge:** Orochimario heads every 5–10 s.
  - **InvulnerablePhase:** The Ender's 90 s shield, which releases the stored damage.
  - **SelfDestruct:** Sig.ra Rossi, 2 s fuse, 50 damage.
  - **HazardZone:** The Ender's 12-damage zones for 10 s.
  - **HalvingPulse:** Cosmic Egg.
- A number left at 0 uses that kind's VS default (`EnemyAbilityDefaults`). Values the wiki does not give are marked provisional in code: lunge speed and time, pulse interval and radius, arm distance, zone radius, summon ring and cap, and the 0.03 units/s-per-VS-speed conversion.
- To give a boss an ability: open its EnemyData, add an Abilities entry, pick a Kind and leave the numbers at 0 for VS. No boss has an ability yet; that is a content decision.
- **VS resistances** (Enemies page), new `EnemyData` fields:
  - `FreezeResistance`: weapon freezes (Clock Lancet, chance 1.0; the wiki gives none) fail when the resistance is higher. Orologion ignores it.
  - `InstantKillResistance`: Pentagram and Soul Toll skip the enemy (the stage-boss skip stays too).
  - `DebuffResistance`: blocks Garlic's knockback and freeze-resistance reductions. Garlic now also lowers freeze resistance by 0.1 per hit (Garlic page).
  - All 10 bosses have freeze resistance 1.1 (VS), so **Clock Lancet no longer freezes bosses**.
  - Stages 2–10 bosses also resist instant kill and debuffs. Stage 1 does not, like the Giant Blue Venus.
- **Bosses come back:** VS wiki (Enemies, Bosses): "Bosses do not despawn when the player moves away from them, but get teleported back to the screen." Straggler relocation now includes bosses; before, a boss left behind stayed there.
- Tests:
  - `EnemyAbilityTests` (T).
  - `EnemyAbilityPlayModeTests` (R):
    - summon up to the cap, a bullet that hurts the player, a lunge, the shield releasing its damage, a self-destruct after its fuse, a zone, and the pulse halving health;
    - nothing runs while paused or frozen;
    - the resistances hold in play.
  - `ChestEliteAndBossPlayModeTests.ABossLeftBehind_IsBroughtBackToTheScreen` (R).
- EditMode 417/417, PlayMode 161/161, save untouched (`D:/SoulHunter-Validation/run-20260928-abilities/`).

**3. One weapon definition asset per item, and VS level-up weighting.**
- `WeaponDefinition` (Create > Soul Hunter > Weapon Definition) holds an item's type, weapon or passive, name, description, icon, prefab, max level, VS rarity and level assets.
- The 36 definitions are in `Assets/_Project/Data/Weapons/Definitions/`, listed in `GameContentCatalog.WeaponDefinitions`. `WeaponDefinitionBuilder` (menu Soul Hunter/Content/Build Weapon Definitions) builds them from the old wiring; it reads the scene without saving it.
- `WeaponManager` (prefab, weapon or passive, max level) and `UpgradeText` (name, description) read the definition first and fall back to the old fields, so existing content behaves the same.
- To add a weapon or passive:
  1. Add the enum value at the **end** of `UpgradeData.UpgradeType` (it is stored as an int).
  2. Write the script and prefab (a passive needs its stat case in `WeaponManager.ApplyPassiveStat`).
  3. Make its level UpgradeData assets.
  4. Make one WeaponDefinition with the VS rarity, citing the wiki page, and add it to GameContent.
  - No scene binding is needed.
- **VS level-up weighting** (wiki Level_up):
  - Items are drawn from one pool by rarity: P = itemRarity / poolWeight.
  - Rarities come from each item's wiki infobox (`UpgradeRarity.VsRarity`), for example Whip 100, Garlic 70, Pentagram 60, Peachone 50, Bone 1, Cherry Bomb 1, Hollow Heart 90, Wings 50, Tirajisú 40, Metaglio 10.
  - While the inventory is not full, two extra checks each offer an owned item with chance 1 + 0.3x − 1/totalLuck (x = 2 on even levels, 1 on odd).
  - Chests use the weighted draw without those checks.
  - Visible effects:
    - Bone and Cherry Bomb (rarity 1) almost never appear in level-ups, as in VS; the Level 3 merchant sells both.
    - Owned items are offered more often.
- Stone Mask has no Soul Hunter item.
- **Passives on VS values** (owner, 2026-09-28: "passives ka max level VS jaisa theek karo"; each item's wiki infobox):
  - **Max level** (`UpgradeRarity.VsPassiveMaxLevel`, in the definitions and the builder):
    - 5: Spinach, Armor, Hollow Heart, Pummarola, Empty Tome, Candelabrador, Bracer, Spellbinder, Wings, Attractorb, Clover, Crown, Skull O'Maniac.
    - 2: Duplicator and Tirajisú.
    - 9: the Metaglios.
    - They were 8.
  - **Hollow Heart:** max health × 1.2 per level, without filling the gain ("does not heal to fill up the newly increased Max Health"). It was +20 flat with a heal.
  - **Attractorb:** Magnet multiplied by 1.5, then 1.33, 1.25, 1.2 and 1.33 (3.98× at level 5; `WeaponManager.AttractorbStep`, `PlayerStats.MultiplyMagnet`). It was +25% per level.
  - **Metaglio Left/Right:** level 1 gives nothing; the bonus starts at level 2.
  - The other passives already matched the wiki's per-level bonuses.
  - **Tirajisú leaves the inventory** (wiki Tirajisú tips, 2026-09-28): when a revival uses up the last Revival from any source, `PlayerController` calls `WeaponManager.ReleaseSpentTirajisu`. Its level goes back to 0, its passive slot frees up, `WeaponManager.OnItemRemoved` fires, and level-ups can offer it again from level 1. The wiki's stage-item Tirajisú does not apply: no Soul Hunter stage places one.
  - **Armor's retaliatory damage** needed no change. The wiki (Armor (item), Armor (stat)) says Armor "does not deal retaliatory damage by itself"; it adds +10% per point (capped at 50 Armor) to other retaliation sources. Soul Hunter's only one is Crimson Shroud, which already applies it (`CrimsonShroudWeapon.CalculateRetaliationDamage`). NO FUTURE and Heart of Fire have no Soul Hunter counterpart.
  - **Defect found by the Tirajisú PlayMode test: passives did nothing in the real game.** `Main_Gameplay` keeps `Weapon_Manager` under `--- MANAGERS ---`, and `WeaponManager.ApplyPassiveStat` looked for `PlayerStats` only in its parents, found none and returned silently. The level went up, but no stat changed (Spinach, Armor, Hollow Heart, Tirajisú, all of them). The old full PlayMode run logged "Passive Applied" 0 times. The EditMode passive tests put the manager on the player, so they missed it. It now falls back to `PlayerController.Instance`. `PassivePlayModeTests` (R): Spinach, Armor and Hollow Heart change the real player's stats. Every earlier passive balance note (for example the bot runs) was measured without passive effects.
  - **HUD inventory row** (owner: "HUD mein weapon aur passive slots bana do", 2026-09-28). The HUD had never shown an item: `HUD_Canvas.prefab` had empty `_weaponSlots`/`_passiveSlots` and no `WeaponManager`. `InventoryHud` (built by `GameHUDController`) shows 6 weapon and 6 passive slots (wiki Passive items: 6 passive slots) in the order gained (`WeaponManager.GetInventory`), redrawn on `WeaponManager.OnInventoryChanged` (gain, level-up, evolution, union, stage passive, Tirajisú leaving). A union takes its first weapon's slot with the union icon.
    - The wiki does not describe the HUD layout. Provisional Soul Hunter choices: the rows hang 22 units under the Gold label and follow it, because the labels hang from a fixed box at the screen centre and move with the aspect ratio (a fixed spot overlapped them off 16:9). An evolved weapon keeps its base icon with a gold frame, because evolutions have no icons (Art section). Metaglio Left/Right have no icon and show an empty frame. Stage passives beyond 6 are not shown.
    - Tests: `InventoryListTests` (T) and `InventoryHudPlayModeTests` (R: Kael's Whip shows at start, new items appear in order with their own icons, the row sits under Gold). EditMode 460/460, PlayMode 166/166, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20260928-inventoryhud2/`). Editor play `autoplay-editor-54` (Stage 1, 5 min): 0 errors, min 35 FPS; screenshots show the Whip alone at 0:12 and 6 weapons + 3 passives at 4:38 (partial `V`).
    - Seen, not changed: that run reached level 40 and 8,129 kills in 5 minutes, against level 20 and 1,392 kills in `autoplay-editor-53` with the same code apart from the HUD. It needs a look in the balance section.
  - Tests: `PassiveVsTests.Tirajisu_LeavesTheInventory_OnlyWhenTheLastRevivalIsUsed` (T) and `TirajisuPlayModeTests` (R: level 2 gives 2 revivals; the first death keeps it, the second removes it, then it can be taken again).
  - EditMode 457/457, PlayMode 165/165, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20260928-tirajisu3/`). Editor play `autoplay-editor-53` (Stage 1, 5 min, no god mode): level 20, 1,392 kills, 0 errors, min 67 FPS; Armor and Candelabrador were taken and applied their stats.
  - Tests: `PassiveVsTests` (T).
  - `StoryCardPlayModeTests` was made steady: a kill by the starting weapon could open a level-up screen, and the pause stopped the card's countdown. The test now turns the weapons off.
  - EditMode 456/456. The full PlayMode run gave 162/163 (the story-card test), and that test passed alone after the fix. Save untouched (`D:/SoulHunter-Validation/run-20260928-passives/`).
- Tests: `WeaponDefinitionTests` (T) and `WeaponDefinitionPlayModeTests` (R: every definition's prefab matches the old wiring; real level-ups offer valid, distinct items). `LevelUpCurveTests` now uses the VS rarity values.
- EditMode 435/435, PlayMode 163/163, save untouched, scene unchanged (`D:/SoulHunter-Validation/run-20260928-weapondefs/`).

### Balance pass, Part 3: enemy pressure along the stages (2026-09-28)

Measured with the bot, no god mode. `-sh-autoplay-start-level N` starts the hero at level N, so later stages can be played at a believable strength.
- Before:
  - Stage 1 over 30 min (`autoplay-editor-43`): level 83, hurt 5 times, at most 183 enemies.
  - Stage 5 from level 110 over 15 min (`-44`): hurt 9 times, at most 180 enemies.
  - Stage 10 from level 140 (`-45`): **dead after 2:27**, with 289 enemies at 2:00 and a Soul Meteor volley of 8.
- Changes:
  - Every stage's wave table now peaks at the 300 enemy minimum. VS Mad Forest reaches 300 from 11:00; the old peak was 150 + 16 per stage, 166 on Stage 1. The 10 wave assets were rebuilt.
  - Soul Meteors come every 4 minutes from 4:00 with 6 markers (VS Gallo Tower has stars at only a few set minutes). They were every even minute from 2:00 with 8.
- After:
  - Stage 1 over 15 min (`-46`): level 50, 11,650 kills, up to 233 enemies, hurt 4 times, min 33 FPS in the GUI editor.
  - Stage 10 from level 140 over 10 min (`-47`): survived; hurt 100 times, health between 79 and 100, up to 327 enemies, min 9 FPS in the GUI editor (27–47 FPS at the 2-minute samples).
  - The gem count stayed at or under 400.
- XP per stage was not changed. By about level 80 every weapon and passive is maxed, and after that VS level-ups also give only gold or a chicken.
- EditMode 399/399, PlayMode 149/149, save untouched (`D:\SoulHunter-Validation\run-20260928-balance3\`).
- **Waves during boss fights:** the spawner stopped the minute waves while a boss lived, so a boss stood alone on screen for minutes. VS keeps the waves going (Mad Forest's minimum is 100 at 25:00), and now so does Soul Hunter. `ChestEliteAndBossPlayModeTests.TheHorde_KeepsComing_DuringABossFight` (R).
- `AutoplayBot` now picks level-ups through the level-up screen. It used to call `SelectUpgrade` directly, which left the panel open after the last level-up (a bot artifact; a player's click closes it).
- EditMode 399/399, PlayMode 150/150, save untouched (`D:\SoulHunter-Validation\run-20260928-bosswaves\`).
- **Boss fight length (Part 4), measured with the bot in god mode:**
  - Stage 1 boss (3,150 health at level 63): 50 s.
  - Stage 5 boss (27,500 at level 110, `autoplay-editor-50`): 310 s.
  - Shadow Kael (75,000 at level 150, `-49`): 190 s, and the run ended in victory.
  - VS boss fights usually take well under 2 minutes (from memory, not a source). The bot runs from bosses, so these times are rough.
  - Health (50 × stage × level) is the owner's story choice; any change waits for the owner.

### Death Spiral crash and pooled whips (found 2026-09-28)

A full 30-minute Stage 1 editor run (`autoplay-editor-42`, no god mode) logged 30,204 errors, and the frame rate fell from 80 to 20.
- **Death Spiral** (the Axe evolution) threw a `NullReferenceException` on every attack. Its scythe is the SH10 model prefab, which has no collider. `Projectile` requires one, so `AddComponent<Projectile>()` returned null. Scythes now get a trigger collider first.
- **`AutoAttackWeapon.Update`** set the cooldown only after `Attack()`, so an attack that threw was retried every frame. The cooldown is now set first, for every weapon.
- **`WeaponPoolManager.GetFromPool`** could hand back a destroyed object. Bloody Tear parents its pooled whips to itself, so they die with the weapon (for example when a union replaces it). Destroyed entries are now skipped.
- New `EvolvedWeaponSweepPlayModeTests` (R): every evolution and union weapon must hurt enemies within 10 s without errors. Evolutions had no sweep before.
  - On the old code it fails with the `NullReferenceException`.
  - After the Death Spiral fix it caught the Bloody Tear `MissingReferenceException`.
  - It passes on the new code.

VS gem cap (vampire.survivors.wiki/w/Experience_Gem): past 400 gems on the ground, new XP goes into one gem instead of new gems (`PickupPoolManager.MaxGemsOnGround`). The rerun after the fixes (`autoplay-editor-43`) had 0 errors but still fell to 23–40 FPS in the last 10 minutes with only ~43 enemies; uncollected gems are the suspect, so `AutoplayBot` now also logs the gem count. `GemCapPlayModeTests` (R): 600 gems keep at most 401 on the ground and lose no XP. EditMode 399/399, PlayMode 149/149, save untouched.

Balance baseline from the first run: level 70 at 30:00, 18,350 kills, 17 chests, hurt 14 times, never below full health at the timeline samples; the stage boss (3,150 health) lived 50 s.

### VS extra features: run modes and stage hazards (owner: "extra feature also continue", 2026-09-27)

**Run modes** (VS "List of Modes"). Toggled on the character select screen (a row above the character grid), saved in `GameData` (`ModeHurry` … `ModeLimitBreak`; an older save loads them as off). They are copied into `RunModes` when `LevelProgressionManager` wakes and reset when it is destroyed. Numbers are the VS ones unless marked:
- **Hurry:** the stage clock runs 2× (`LevelProgressionManager.AdvanceClock`); XP +25%.
- **Hyper:** player and enemy move speed +65%, projectile speed +15%, gold +50%, spawn rate and enemy minimum ×1.25.
- **Inverse:** enemies start at +200% health and gain +5% health and +0.5% speed per stage minute; gold +200%; Luck +20% (applied once in `PlayerStats.Start`). The flipped, darker VS map is not built.
- **Endless:** clearing Level 10 no longer wins. `RunModes.EndlessCycle` goes up and Level 1 starts again. Each cycle adds +100% enemy health, +50% spawn rate and count, and +25% enemy damage.
- **Arcana:**
  - One random Arcana at the start of the run.
  - Arcana chests at 11:00 and 21:00, carried by chest elites (`EnemyDrop.CarryArcanaThisLife`, `ChestPickup.IsArcana`, `ChestLogicController.ProcessArcanaChest`), up to 3 Arcanas.
  - **Changed:** `ArcanaManager` used to give an Arcana at 11:00 and 21:00 of every stage without any mode. VS gives none without the mode, so that is gone.
  - Not built: the VS pick-from-four choice screen (here the Arcana is random) and showing the start-of-run Arcana on screen.
- **Limit Break:** once nothing is left to level, level-ups offer "LIMIT BREAK" cards: +10% damage to one held weapon per card (`LimitBreak`, hooked in `AutoAttackWeapon.ScaledDamage`; provisional). VS's random stat per card is not built.
- Not built: the HUD and game-over screen do not show the active modes or the Endless cycle.
- Hooks: `RunModes` is read by `EnemySpawner`, `EnemyController`, `PlayerExperience`, `AutoAttackWeapon` and `CampaignLevelCatalog.CurrentGold` / `CurrentPlayerSpeed`.
- `GameplayPlayModeTestBase.BootIntoGameplay` turns every mode off for the other tests.

**Stage hazards** (`StageHazards`, added by `GameSessionManager`; table in `StageHazardTable`). All numbers are provisional, and placement follows the story's stages:
- **Death's Shadow** (VS Stalker): Stages 5 and 9 at 8:00 and 12:00.
  - The stage boss model at 1.5×, tinted dark, 18 units out, at the player's base speed, with 30 contact damage.
  - It cannot be hurt, pushed or killed (not even by Soul Toll, Pentagram or the stage-clear wipe), drops nothing, and leaves after 120 s.
  - It is not a stage boss. It restores the pooled enemy when it goes.
- **Soul Meteors** (VS Shooting Star): Stages 6 and 10, every even minute from 2:00 to 28:00.
  - A volley of 8 ground markers (the first on the player); each strikes 2 s later for 30 to the player (minus Armor) and 30 to enemies within 1.6.
  - The markers use the existing VFX magic circle.
- **Pile Assault** (VS): Stage 8 at 9:00, 10:00, 12:00 and 20:00.
  - 12 stage enemies on an ellipse stand still and each fire a slow bullet (10 damage) every 2.5 s.
  - They can be killed; the survivors leave after 20 s.
- **Rail carts** (VS Dairy Plant carts): Stage 3, two 24-unit rails near the start.
  - Weapons empty a cart's 30 HP to launch it; it runs along the rail dealing 200 to each enemy it touches, then waits at the far end.
  - It never hurts the player.
- Placeholder art from `StageHazardBuilder` (menu Soul Hunter/Content/Build Stage Hazards): `SH10_RailCart`, `SH10_HazardMarker`, and the hazard bullet (a tinted Magic Wand bolt). **Not owner-approved art.**
- Found in validation: `RailCartTrack` was declared inside `RailCart.cs`, so Unity could not save it on the prefab (a MonoBehaviour needs its own file). It now has its own file.

Validation:
- EditMode 399/399 after that fix. The full PlayMode run gave 147/147; after the fix, the hazard and run-mode fixtures gave 7/7. Save untouched (`D:\SoulHunter-Validation\run-20260927-extras\`).
- Editor play, god mode, 2.5 min each, 0 errors:
  - `autoplay-editor-38`, Stage 5 (clock at 7:50): Death's Shadow appeared at 8:00. Min 42 FPS, up to 317 enemies.
  - `-39`, Stage 6: a Soul Meteor volley at 2:00 struck. Min 38 FPS.
  - `-40`, Stage 3: both rail carts lie near the start and are visible. Min 63 FPS.
  - The shadow and meteors are confirmed in the log; in the screenshots they are hidden by level-up screens or off-frame (partial `V`).
- Run modes are covered by tests only; the bot does not turn modes on.

Not yet played by the owner.

### Balance pass, Part 1: weapons on VS numbers (owner request, 2026-09-27)

Owner: "balance pass shuru karo". Balance follows the VS rules plus the owner's story.

Baseline before Part 1 (`autoplay-editor-35`, Stage 1, 15 min, no god mode, old weapons): level 117, 63,998 kills, never more than 89 enemies, hero never hurt, min 8 FPS.
- Most weapons used placeholders (10 damage, 1.5 s).
- Pentagram erased the map every 1.5 s, and its level-ups took 5 s per level off that, going below zero.
- Holy Wand fired every 0.1 s and Thousand Edge every 0.05 s.

Now every weapon's level-1 damage, cooldown, amount, pierce and duration, and its level 2-8 table, follow vampire.survivors.wiki (fetched 2026-09-27; raw pages in the session scratchpad). Each script names its numbers as constants with a "VS wiki" comment and has `MaxLevel` 8 (7 for Clock Lancet and Laurel).
- Soul Hunter keeps its own level-1 Area and Speed; VS Area/Speed level-ups apply as percentages on top.
- Examples:
  - Whip 1.35 s; Axe 20 / 4 s with VS pierce (new `AxePierce`, an axe leaves after 3 enemies); Cross 5 / 2 s; King Bible 10, 1 book; Garlic 1.3 s.
  - Knife 6.5 / 1 s; Fire Wand 20 / 3 s, 3 shots; Santa Water 10 / 4.5 s; Lightning Ring 15 / 4.5 s; Peachone 10 / 1 s, 4 bombs.
  - Pentagram 90 s, 10% keep chance; Clock Lancet 2 s / 2 s freeze.
  - Evolutions: Thousand Edge 16.5 / 0.35 s ×6; Bloody Tear 40; Unholy Vespers 30; Heaven Sword 77 / 3.3 s; Death Spiral 60 / 4 s; Soul Eater 20, 3× Garlic's radius.
- Not mapped (listed in the scripts):
  - VS hit intervals and per-weapon knockback.
  - King Bible's on/off duration (our books always orbit).
  - Cherry Bomb's explode chance (our bomb only deals contact damage).
  - Pentagram's 1 s duration and its erasing of items already on the ground.
  - Peachone's duration.
  - Damage rounds down to whole numbers in `Projectile`, so Knife hits for 6 before Might.
- Kept although VS differs: Soul Eater and Bloody Tear healing (Soul Hunter's own), all aiming at the nearest enemy (owner decision), and the Gun's 4-way pattern (VS Amount counts per arm).

Validation:
- `WeaponLevelTableTests` (T) was updated to the VS tables (Garlic, Axe, Bible).
- `WeaponSweepPlayModeTests` (R) now resets a weapon's timer after placing its targets, because the first shot fires on equip and Pentagram's next shot is 90 s away.
- EditMode 353/353, PlayMode 135/136. The one failure was that sweep case (Pentagram); after the fix the sweep fixture passed 21/21. Save untouched (`D:\SoulHunter-Validation\run-20260927-weapons\`).
- After (`autoplay-editor-36`, same run): level 43, 9,484 kills, up to 118 enemies, min 43 FPS, 0 errors. That is about what a VS player reaches by 15:00 (from memory, not a source).
- Seen for later parts: the hero was hurt only twice in 15 minutes (enemy pressure, Part 3).

Not yet played by the owner.

### Boss level scaling and chest elites (owner decisions, 2026-09-24)

Found while reviewing the VS Stage 4/5 write-ups: bosses had fixed health (1000 x stage) although one run keeps levelling across 10 stages, and only stage bosses dropped chests (~10 per run), so evolutions were rare. The owner chose:

- **Boss HP x player level:** `EnemyData.HealthPerPlayerLevel` (> 0 means max health = it x player level, then Curse). All ten stage boss assets use 50, which reproduces the old values if the player gains ~20 levels per stage; provisional balance. Shadow Kael scales too, on top of mirroring the loadout.
- **Timed chest elites:** every `_chestEliteIntervalSeconds` (300 s) of stage time before the boss (5/10/15/20 min), `EnemySpawner` spawns the stage elite with 5x health and 1.5x size that always drops a chest (`EnemyDrop.DropChestThisLife`, cleared on respawn so pooled reuse never drops extra chests). Not blocked by the enemy cap. Values provisional.

Validated by `BossScalingAndChestEliteTests` (T) and `ChestEliteAndBossPlayModeTests` (R); EditMode 246/246, PlayMode 23/23. Balance and feel in real play are unverified.

### Elites and bosses in play; the owner's story (2026-09-27)

Elites and bosses were watched in editor play for the first time. `AutoplayBot` now records `elite-spawn`/`boss-spawn` (health, size, distance, player level), `-killed` (time alive) and `-despawned` events with screenshots. `-sh-autoplay-skip-to T` (after `-sh-autoplay-skip-after S`, default 10) moves the stage clock through `LevelProgressionManager.SkipTo`. Helpers: `EnemyDrop.WillDropChest`, `EnemySpawner.IsBoss`.

First look (`autoplay-editor-28` Stage 1, no god mode; `-29` Stage 4, god mode; 0 errors):
- The 5:00 chest elite came (5x health, 1.5x size), died in 21.8 s (Stage 1) and 94 s (Stage 4), and dropped a chest.
- The boss came at 25:00 (3x size; from one end of the Castle corridor), dropped a chest, and the next stage started.
- Found:
  - the Stage 1 boss died in 6.4 s;
  - the Stage 4 boss (600) had less health than its chest elite (625);
  - elites and bosses dropped only a chest, so their soul gems (50 and 250 XP) never appeared.

Owner decision (2026-09-27): "MERE STORY KO FOLLOW KARO", i.e. follow the story in `Docs/Game_Design_Document.md`; "STORY MEIN NO CHANGES" (do not edit the story; the old GDD level mechanics stay as the earlier owner decisions left them). The owner chose:
- ~~**Bosses grow with the story:** 50 × stage × level, never under twice a chest elite's.~~ Replaced on 2026-09-28 by the owner's choice "pura VS". Bosses and chest elites now use the VS wiki's HP x Level values (raw boss pages fetched 2026-09-28):
  - **Stage bosses, per level:** Stage 1: 150 (Mad Forest Giant Blue Venus); Stages 2–3: 250 (Inlaid Library Hag, Dairy Plant Sword Guardian); Stages 4–10: 500 (Gallo Tower Giant Enemy Crab, Mt.Moonspell Orochimario).
    - Later guardians are never weaker, per the story; Cappella Magna's 250 would have dropped.
    - Shadow Kael stays at 500.
    - The "twice a chest elite" floor is gone: in VS, Mad Forest's 20:00 Giant Mummy (250) is stronger than its 25:00 boss (150).
  - **Chest elites** (`EnemySpawner.ChestEliteHealth`, VS Mad Forest chest bosses):
    - 50 × level at most minutes (Glowing/Silver Bat), and at the Arcana minutes.
    - 150 × level at 10:00 (Giant Mantichana), 200 × at 15:00 (Giant Werewolf), 250 × at 20:00 (Giant Mummy).
    - A flat 500 at 5:00 (Mantichana, no HP x Level).
    - The old rule was 5 × the stage elite's EnemyData health.
  - **Maxed-out level-ups** (VS wiki Level_up): with nothing left to upgrade, the level-up screen offers two cards, "Gold Pouch" (+25 gold, the Big Coin Bag) or "Roast Chicken" (30 health, the Floor Chicken), and the player picks one (`MaxedRewards`). Before, both were given silently with no screen. The VS "always gold/chicken" option after 100 Big Coin Bags is not built.
  - Pickup values checked against the wiki: chicken 30, Orologion 10 s, coins 1/10/100, Big Coin Bag 25. All match.
  - Validation: EditMode 407/407 (the banish test now expects the reward cards), PlayMode 150/150, save untouched (`D:/SoulHunter-Validation/run-20260928-vsboss/`).
  - Editor play `autoplay-editor-51` (Stage 1, god mode, clock skipped to the boss at level 20): 0 errors, min 49 FPS.
    - The boss had 3,000 health (150 × 20) and died in 87 s.
    - The chest elites matched the VS values: 200 at 1:00 (level 4), 700 at 3:00, 500 at 5:00, 3,000 at 10:00.
- **Souls:** a boss or chest elite drops its soul gem (`EnemyData.ExperienceValue`) next to its chest (`EnemyDrop`).
- **The story is shown:** `StoryCardUI` (added by `GameHUDController`) shows "Level n: name" and the story when each stage starts, with the premise on Level 1 and the twist on Level 10. It does not pause and waits while a menu is open. The text is word for word from the GDD: `CampaignLevelCatalog` narratives now use the GDD's story lines, plus `CampaignStory.Premise` and `Twist`. The story is about Kael whichever character is played.

Validation:
- `CampaignStoryTests` (T: every line matches the GDD text), `BossScalingAndChestEliteTests` (T: 50 x stage, the elite floor) and `StoryCardPlayModeTests` (R: cards on Levels 1, 4 and 10, no pause, the card goes away).
- `ChestEliteAndBossPlayModeTests` (R): the elite also drops its gem; the boss has the story health and outlasts its chest elite.
- EditMode 353/353, PlayMode 134/134, 0 exceptions, save untouched (`D:\SoulHunter-Validation\run-20260927-story\`).
- Editor play `autoplay-editor-30` (Stage 1, no god mode, 0 errors): the story card opened the run; the elite died in 18.8 s; the boss (1,000 health at level 20) lived 24.9 s.
- `autoplay-editor-31` (Stage 10 jumped to at level 1, god mode, 0 errors): the twist card showed; Shadow Kael had 4,250 health (twice the 2,125 chest elite) and a level-1 hero could not kill him in 150 s.

The owner's own checks, played by the agent (owner: "DO ALL", 2026-09-27). New bot options: `-sh-autoplay-jump-to N` / `-jump-after S` (the Debug "Jump To Stage" action), and `elite-drops`/`boss-drops` events counting soul gems and chests near the body.
- `autoplay-editor-32` (new run from Level 1) found a real defect: Pentagram erased the 5:00 chest elite in 1.1 s and the Stage 1 boss in 0.2 s, with no chest and no gem. It also switched `EnemyDrop` off, and the component stayed off when the pool reused the enemy, so that enemy never dropped a gem or counted a kill again.
  - Fixed per VS (vampire.survivors.wiki/w/Pentagram, checked 2026-09-27): stage bosses are immune (`EnemyController.IsStageBoss`, set by `SpawnBoss`). An erased enemy loses only this life's drops (`EnemyDrop.EraseDropsThisLife`) and still counts as a kill. An erased chest carrier's chest passes to the next ordinary enemy the horde spawns (`EnemyDrop.PendingChests`, `EnemySpawner.GivePendingChest`).
  - `PentagramPlayModeTests` (R) fail on the old code (boss erased; pooled `EnemyDrop` off) and pass on the new.
- `autoplay-editor-33` (new run from Level 1, jump to Stage 10 at 20 s, Skip To Stage Boss at 60 s, god mode, 0 errors): the twist card showed, and Shadow Kael (4,250 health) copied Whip and Clock Lancet.
  - Found: an open level-up screen showed the story card through it. The card now hides while a menu has the game paused and comes back afterwards (`StoryCardPlayModeTests`).
- `autoplay-editor-34` (new run from Level 1, no god mode, 0 errors): story card; the 5:00 chest elite dropped its chest and 50 XP soul gem; the boss (1,000 health) dropped its 250 XP gem and a chest that the hero picked up at once. The boss died in 4.5 s, at player level 20.
- EditMode 353/353, PlayMode 136/136, save untouched (`D:\SoulHunter-Validation\run-20260927-pentagram\`). The later story-card change was rerun with its fixture and `PentagramPlayModeTests`: 3/3.

Not yet played by the owner.

Stage 3-5 write-ups (Dairy Plant, Gallo Tower, Cappella Magna) added no other adoptable official mechanics; their stage data, names, unlock levels and Hyper/Hurry modes are VS content or conflict with the run structure.

### Stage events (owner request, 2026-09-24)

VS-style scripted formations on the stage clock, in all 10 levels, using that stage's enemies (`PickSwarmPrefab`, so Blood Arenas stays elite-only and elites can appear): `Swarm` (tight cluster crossing the player's position), `Wall` (line across the travel direction, 1.5 apart) and `ClosingRing` (circle at the spawn radius flying inward, so it closes and opens out). Members use `EnemyFlyState`: fixed path at 1.5x speed, facing their path, not relocated as stragglers, returned to the pool without drops after 15 s. `WaveData.Events` holds authored events; empty means the default schedule 2:30 Swarm 20, 7:30 Wall 16, 12:30 Ring 16, 17:30 Swarm 24, 22:30 Wall 20 (between the 5-minute chest elites, before the boss). Events are not blocked by the enemy cap. Counts/times are provisional.

`EnemyFlyState` existed unused and its `Exit` set the collider solid, which would have made every pooled flier a wall; fixed (enemy bodies stay triggers). A Time Freeze or Clock Lancet hit used to send fliers to the chase state afterwards; now `EnemyController.Freeze` remembers the flight, so a frozen flier stays in place, is not relocated as a straggler and resumes its path and lifetime when the freeze ends (`StageEventPlayModeTests.FrozenFliers_ResumeTheirPath_AfterTimeFreeze`, R, 2026-09-25). The occasional "member did not move" failure of `EveryFormation_FliesItsPath` came from the player's starting weapon killing members that crossed the player; the test now disables the player's weapons.

Validated by `StageEventTests` (T) and `StageEventPlayModeTests` (R: the 2:30 swarm fires on the stage clock; every formation flies; pooled fliers stay triggers); EditMode 251/251, PlayMode 25/25. Look and feel in real play unverified.

### Audio

No scene contains an `AudioManager`; it creates itself from `Assets/Resources/AudioManager.prefab` at startup (`RuntimeInitializeOnLoadMethod`). Found 2026-09-25: that prefab was hand-written YAML with corrupted file IDs, which Unity imported as a DefaultAsset, so `Resources.Load` returned null, no AudioManager ever existed and the game had no sound at all (every PlayMode log showed "prefab not found"). Rebuilt with Unity's prefab writer (`AudioManagerPrefabBuilder`, same GUID). Enemy hits now play `EnemyHit` (throttled to one per 0.06 s via `PlaySFXThrottled`). Validated by `AudioPlayModeTests` (R: one listener, all five effects assigned and not silent, hitting an enemy plays EnemyHit); batchmode cannot confirm speakers, so hearing it is still unverified.

(Superseded 2026-09-25 by CC0 recordings; see "Look, feel and sound pass". The synthesised clips remain as a fallback.) Missing audio was then synthesised in code (owner choice 2026-09-25, retro style, no external samples): `RetroAudioSynth` (menu Soul Hunter/Content/Generate Retro Audio) writes 12 effects (EnemyDeath, PlayerHurt, GameOver, StageClear, BossWarning, UIClick, UIConfirm, Unlock, Shoot, Slash, Magic, Explosion) and 11 music loops (menu + one per stage, each with its own key, tempo, scale and progression; loops end at silence so they repeat without a click), and adds a 3 ms fade-in to the five older effects that started with a click. `AudioManagerPrefabBuilder` wires them all into the prefab. Hooks: stage music and stage-clear / game-over / victory sounds via `StageAudio` (added by `GameSessionManager`), menu music on MainMenu, enemy death, player hurt (throttled), boss warning, unlock notice, UI click/confirm, and a fire sound per weapon (`AutoAttackWeapon.FireSound`: Shoot, Slash, Magic, Explosion, or None for Garlic and Bible; throttled). SFX volume no longer applies MasterVolume twice. Validated by `AudioPlayModeTests` (R: every clip assigned, effects audible, menu and per-stage music, weapon, hit, death, hurt and game-over sounds); EditMode 281/281, PlayMode 33/33 (`D:\SoulHunter-Validation\run-20260925-audio2\`). Nobody has listened to it yet: how it sounds (`V`) and the mix are unverified.

### Accessibility (owner decision 2026-09-25: follow the original VS options)

Before: no options screen (only a master-volume slider inside the pause panel), fixed controls, no controller button for Dash/Ultimate/Pause, no focus on pause/level-up/game-over, a mouse-only IMGUI merchant shop, a dead Restart button, and no way to turn off shake, damage numbers or flashes.

Now (checked against the VS wiki options page, https://vampire.survivors.wiki/w/Options, on 2026-09-25; VS-only items with nothing behind them in Soul Hunter are left out: language, classic sounds, borders, pixel font, defanged enemies, co-op, Twitch):
- `OptionsMenuUI` (main menu Options button and pause menu), in VS-style sections. Sound: Sounds, Music. Display: Window Mode (Fullscreen Window / Exclusive Fullscreen / Maximised / Windowed), Resolution, V-Sync, Flashing VFX, Disable Blood (Blood Arenas floor stains switch to the plain floor material), Screen Shake. Gameplay: Damage Numbers, Show Stage Progression (HUD "Stage n/10: name"), Visible Joystick, Pause When Unfocused (extra). Controls (extra): remapping of keyboard (Move WASD, Dash, Ultimate, Pause) and controller (Dash, Ultimate, Pause) bindings with Reset. Stored in `GameData` via `GameSettings`; remaps are saved by action name + binding index because Unity's JSON uses per-launch random binding IDs and would never reload.
- Flashing VFX off: no hit flash. (The low-HP red screen tint was removed on the owner's request, 2026-09-30.) Damage Numbers off hides combat numbers (merchant cost popups stay). Screen Shake off stops `CameraShake`.
- Controller: Dash = A/Cross, Ultimate = Y/Triangle, Pause = Start, D-pad moves. Every screen focuses a control (menu, character select, pause, options, level-up, chest, game over, merchant), and focused controls turn orange (`OptionsMenuUI.ApplyFocusColors`; the old tint was invisible on dark buttons).
- `PauseMenuUI`: Resume / Options / Quit (to main menu); `GameSystemManager` pauses on focus loss when the option is on.
- The merchant shop is uGUI instead of IMGUI. Selected character cards also get an outline (not colour alone).
- Touch controls (`TouchControlsUI`, VS mobile control stick): stick + Dash + Pause through Input System on-screen controls (virtual gamepad); always on mobile, on desktop with Visible Joystick. Before this Android had no touch input at all.
- HUD readability (`HudReadability`): the kill, gold and time labels were dark blue on dark floors; now white with a black outline.

Validated by `AccessibilityTests` (T) and `AccessibilityPlayModeTests` (R: options sections and focus, pause menu, controller Start pauses, focus-loss pause, merchant shop focus, switches turning off numbers/shake/flash, stage label, white HUD labels, Disable Blood on the Blood Arenas floor, touch controls); EditMode 284/284, PlayMode 37/37, save file untouched (`D:\SoulHunter-Validation\run-20260925-access2\`). VS has no text-size option, so none was added. Window Mode and Resolution only apply in builds (not the editor Game view). Not tested on real hardware, a real controller, a touch device or a real alt-tab. `StageEventPlayModeTests` failed once in five full runs (a wall member moved 0.19 < 0.5 in 20 fixed updates) and passed alone three times: timing-sensitive, not caused by this change.

### Persistence

Hardened 2026-09-25 (`SaveService`, `GameData.SaveVersion`):
- Saves are atomic: written to `.tmp`, then swapped in with `File.Replace`, keeping the last good save as `.prev`. A failed write (locked file, full disk) logs, returns false and leaves the last good save; it no longer throws into gameplay.
- A damaged save (unparsable, cut off mid-write, empty or zero-filled) is copied to `.bak` and removed, and the `.prev` save is restored; only without a usable `.prev` does the game start fresh. A missing save with a `.prev` (crash between write and swap) is restored too.
- Schema versioning: `GameData.CurrentVersion` = 1; files without the field load as version 0 and are migrated (v0 -> v1: Antonio -> Corvan in the selected, unlocked and owned characters, duplicates removed). Every load also repairs impossible values (negative gold or levels, NaN/out-of-range volumes, bad window mode or resolution index, null lists). A save from a newer build is kept at its version with a warning.
- A `-sh-benchmark` launch uses a throw-away save in `temporaryCachePath`.

Validated by `SaveServiceTests` (T: atomic save and `.prev`, three kinds of damage restored from `.prev`, fresh start without `.prev`, missing save restored, legacy v0 migration, value repair, locked-file write failure) and both suites (EditMode 293/293, PlayMode 39/39, save file untouched, `D:\SoulHunter-Validation\run-20260925-gaps2\`). Not tested: a real power cut, or Android storage.

### Documentation

Some older status documents are stale or contradictory. Current owner decisions and verified evidence outrank stale status text.

### Character stat blocks and VS-style select screen (owner request, 2026-09-25)

Owner decisions (2026-09-25): every character gets the full VS stat block, every ability its description promises must be real, and the character select screen follows VS. `CharacterData.BaseMoveSpeed` is a **multiplier** (1.0 = the player's base speed 5); Kael, Corvan and Elaria were 4/3/3.5 and are now 1.0. Antonio (a VS character name and description) was renamed **Corvan, The Grave Warden** (`Char_Corvan.asset`, same GUID); `GameContentCatalog.CurrentName` maps a save that still says "Antonio" to Corvan.

- Data: `CharacterData` adds Recovery, Speed, Duration, Amount, Revival, Magnet, Luck, Growth, Greed and Curse, a level-up bonus (`LevelBonus` every N levels) and special abilities (crit multiplier, extra projectile bounces, low-health speed bonus, starting shield + its visual). `PlayerStats.ApplyCharacter` applies all of it; `CharacterStatSheet` computes character + gold power-ups for the screen and uses the same numbers.
- Abilities now backed by data (values provisional): Aria Growth +20%, Draven Amount +2, Gideon Area +50% (Might set to 1.0 to match his description), Isolde Cooldown -15%, Kaelen crits 3x, Lumina Recovery 1 HP/s, Rurik up to +50% speed at 0 HP, Seraphina +3 bounces on every `Projectile` weapon (Magic Wand, Song of Mana, Knife, Thousand Edge, Fire Wand, Holy Wand, Gun, Cherry Bomb, Death Spiral), Sylas 10 s starting shield shown with the Laurel shield model (`StartingShieldVisual`, blinks for the last 2 s), Valerius +1 Armor every 5 levels, Corvan +10% Might every 10 levels.
- Screen: `CharacterSelectionUI` builds the layout at runtime inside the scene's existing panel: a grid of character cards (keyboard/gamepad focus selects), a stats panel on the left with bonuses highlighted, the selected character's info and starting weapon, and Confirm. Prev/Next are hidden.
- Upgrade icons: every `UpgradeData` (279 assets) now has an icon from the project's own `Assets/Art/UI` sprites, assigned by `UpgradeIconAssigner` (menu Soul Hunter/Content). Shown on the select screen and the HUD weapon slots.
- Character art: every character has its own rigged low-poly hero model, generated in Blender by `ArtSource/Generators/build_heroes.py` (sources and previews in `ArtSource/SH10_Heroes_v01/`) as `Assets/Art/Models/SH10_Hero_<Name>.fbx`, in Kael's faceted style with Kael's rig and clips, each with its own silhouette and palette. The right hand holds the character's actual starting weapon (`WEAPON_PROP` in the generator; `EveryHeroModel_HoldsItsCharactersStartingWeapon` checks the manifest against `StartingWeapon`). Kael's hero model `SH10_Hero_Kael` is his SH10_Kael geometry plus his Whip (the original `SH10_Kael` stays in `Player.prefab`). `HeroModelImporter` (menu Soul Hunter/Content/Import Hero Models) imports them, builds `Assets/Prefabs/Player/SH10_Hero_<Name>.prefab` (Animator on `SH10_Kael.controller` + `EntityAnimator`, so run/attack now play; Kael included), sets each `CharacterModelPrefab` and renders each menu portrait (`Assets/_Project/Art/Portraits/`). In play, `PlayerController.LoadVisualModel` spawns the selected hero (or replaces an authored 3D visual) and refreshes `DamageFlash`. Before this, every character appeared as the same placeholder sprite in play. Main_Gameplay's player also carried a `Hero_1_0` sprite child that was drawn next to every hero; it was removed and the ten `Hero_N_0` sprite prefabs were deleted (owner request, 2026-09-25). `CharacterSweepPlayModeTests` now fails if any sprite is drawn on the player. Weapons follow the VS rule (owner, 2026-09-25): a run starts with the character's starting weapon only (checked for all 13 characters by `CharacterSweepPlayModeTests`, R), any character can gain any weapon from level-ups and chests (the level-up pool is the whole catalog, S), up to 6 weapons and 6 passives (`CoreLoopFixTests`, T). No gold counter or co-op. Unlocks, buying and the stage filter: see "Character unlocks" below.

Validated by `CharacterStatBlockTests` (T: includes every character having its own animated hero model and portrait) and `CharacterSelectionPlayModeTests` / `CharacterSweepPlayModeTests` (R: a card for each character; clicking one shows its full stat block; Sylas's shield shows and ends; Seraphina's live projectiles carry 3 extra bounces; all 13 characters play with exactly their own model and an EntityAnimator); EditMode 266/266, PlayMode 28/28, 0 exceptions (`D:\SoulHunter-Validation\run-20260925-heroes\`). Batchmode screenshots of the menu and of Gideon in play were checked (partial `V`); hands-on feel, animation quality and balance are unverified.

### Character unlocks and filter (owner decision, 2026-09-25: follow the original VS rule)

Only Kael is available at the start. Every other character has an `OriginStage`, an unlock condition (`ClearStage`, `SurviveInStage`, `ReachPlayerLevel`, `KillsInRun`, `WeaponLevel`) and a `GoldPrice` in its `CharacterData`. `CharacterUnlockTracker` (added by `GameSessionManager`) checks the conditions during a run, stores unlocks in `GameData.UnlockedCharacters`, saves and shows a notice. On the select screen a locked character is a silhouette with its condition and cannot be confirmed; an unlocked one shows its price and Confirm becomes Buy (gold via `EconomyService`, stored in `GameData.OwnedCharacters`); gold is shown top left. The right-hand panel is the VS character filter: `FILTER: ON/OFF` plus one checkbox per level (count `x/10`) that hides characters from unchecked origin stages. Conditions, origins and prices are provisional Soul Hunter data (not VS data): Corvan survive 5:00 in Stage 1 (300 G), Aria player level 20 (500), Draven 1000 kills in a run (700), Gideon clear Stage 3 (900), Kaelen clear Stage 4 (1100), Valerius survive 15:00 in Stage 4 (1300), Isolde Santa Water level 4 (1500), Elaria Magic Wand level 8 (1700), Seraphina clear Stage 6 (1900), Lumina clear Stage 7 (2200), Rurik clear Stage 8 (2500), Sylas clear Stage 9 (3000). Gameplay itself does not re-check ownership; the menu is the only way into a run. The owner's existing save keeps its gold but now owns only Kael. Validated by `CharacterUnlockTests` (T, in-memory GameData) and `CharacterSelectionPlayModeTests` (R: locked/buy/owned states, filter, tracker follows stage clears); EditMode 276/276, PlayMode 31/31, save file unchanged by the tests (`D:\SoulHunter-Validation\run-20260925-unlocks\`). Whether VS's exact filter semantics match this is from memory, not a source.

### Stage floors (owner request, 2026-09-25)

The ground looked broken: InfiniteMap tiled Level 1's 10x10 ground at 20-unit spacing on every level (gaps showed the background), and each stage's showcase scene added its own fixed 3x3 ground on top. Now, as in VS where every stage has its own ground, `ArtSource/Generators/build_grounds.py` (Blender) builds a seamless 20x20 low-poly floor per level (`SH10_Lxx_Floor`: graveyard soil and bones, forest moss and ferns, village cobbled streets, castle flagstones, crimson swamp mud and puddles, snow and ice, soul-sea sand and shallows, blood-arena sand, throne marble with gold inlay, soul-core crystal plates). `StageFloorImporter` (menu Soul Hunter/Content/Import Stage Floors) builds prefabs with one flat BoxCollider and fills `GameContentCatalog.StageFloors`. `InfiniteMap` lays the current stage's floor, swaps it on `OnStageStarted` and turns each tile a random quarter turn; `CampaignSceneConnector` hides the showcase grounds (`IsShowcaseGround`). Validated by `StageFloorTests` (T) and `StageFloorPlayModeTests` (R: level floor, no gaps, swap on stage change, showcase ground hidden, player stands); EditMode 272/272, PlayMode 29/29 (`D:\SoulHunter-Validation\run-20260925-floors\`). In-game screenshots of all 10 levels checked (partial `V`). The showcase scenes also placed a still `SH10_Lxx_Enemy`, `Elite` and `Boss` model at x = -2.3, 0 and 2.3 on every stage, right at the player spawn (2, 1, 0), looking like enemies that never move or die; `CampaignSceneConnector.IsHiddenInPlay` now hides them too (scenery props stay). Reproduced by `StageFloorPlayModeTests` (failed with 9 statue objects) before the fix.

### Stage floors v2: VS-style obstacles, old ground removed (owner request, 2026-09-25)

The owner supplied VS stage ground blueprints as a reference (kept outside the project; nothing copied) and chose "their feel, our own art", obstacles, and removal of the old ground and showcase decor. `build_grounds.py` v2 (`ArtSource/SH10_Floors_v02`) builds each 20x20 floor with a 3-unit cross of paths meeting every tile edge at its midpoint (joins in any quarter turn), denser ground detail and 14-28 themed obstacles per level (graves, crypts and dead trees; pines; houses, wells, hay; pillars and walls; swamp trees; ice; soul ponds; arena weapon racks; braziers and obelisks; crystals and pylons). Each FBX has three meshes: the floor, the `_Obstacles` visuals and simple `_Blockers` boxes that `StageFloorImporter` turns into a hidden MeshCollider on the new `Obstacle` layer (14). The physics matrix lets that layer collide with the Player layer only, so obstacles block the player while enemies (triggers), weapons, Axe physics and projectiles pass over them. The tile centre (player spawn, radius 4.5) and the paths stay open. Floor materials of earlier versions are removed on import.

Removed (`LegacyGroundRemover`): the 90 `SH10_Lxx_Ground` instances in the ten showcase scenes, their 10 models, 10 prefabs, 20 materials and the unused `Ground.prefab`; `InfiniteMap._chunkPrefab` in `Main_Gameplay` now falls back to `SH10_L01_Floor`. `CampaignSceneConnector.IsHiddenInPlay` hides every `SH10_L*` object of a showcase scene in play (props and still display models); the showcase scenes themselves remain.

Validated by `StageFloorTests` (T: floor and collider sizes, obstacle collider/layer/hidden boxes, >= 12 obstacles, spawn centre and paths open, collision matrix, old assets gone) and `StageFloorPlayModeTests` (R: no showcase decor in play; the player walking straight into an obstacle face is stopped, 10/10 repeated runs); EditMode 284/284, PlayMode 38/38, save file untouched (`D:\SoulHunter-Validation\ground-20260925\`). In-game screenshots of all 10 levels checked (partial `V`). The player can slide round a small obstacle's corner, as in VS. The camera looks straight down, so obstacles read mostly by their tops. Feel, density and performance in real play are unverified (`V`, `P`).

### Open ground rule: 70% walkable, no traps (owner rule, 2026-09-26)

Owner rule: at least 70% of every stage's ground must be open for the player, who kept getting stuck between obstacles. VS research (vampire.survivors.wiki stage pages) found:
- Most stages are open ground with "very few obstacles" that "can be easily navigated around".
- Only a few stages are corridors.
- Walls block the player, but weapons pass over them.
- The spawn area is open.

The cause was `build_grounds.py`: obstacles stood only 0.15-0.8 units apart (the player is 0.8 wide) and many blocker boxes reached past their placement radius, so obstacles closed gaps and formed pockets.

Now:
- Obstacles keep a `MIN_GAP` of 1.8 between them and a 1.0 margin from every tile edge, so seams stay open.
- `Placer` measures each obstacle's real blocker footprint (the farthest box corner) and removes an obstacle that does not fit.
- Floors were regenerated (7-16 obstacles per tile, down from 14-31) and re-imported.

`FloorWalkabilityTests` (T) samples each 20×20 tile on a 0.25 grid with the player's capsule plus 0.35 elbow room. Blocker interiors count as blocked (the MeshCollider is surfaces only). It flood-fills from the spawn and the tile edges, and requires ≥70% reachable ground and no trapped pockets.

Reachable ground per level:

| Level | Before | After |
|---|---|---|
| L01 | 67% | 78% |
| L02 | 58% | 78% |
| L03 | 68% | 77% |
| L04 | 61% | 80% |
| L05 | 72% | 80% |
| L06 | 63% | 80% |
| L07 | 61% | 82% |
| L08 | 79% | 85% |
| L09 | 75% | 79% |
| L10 | 77% | 80% |

The earlier "trapped" readings were blocker interiors. `StageFloorTests` now requires at least 6 obstacles per floor (it was 12).

Found by the suites: the Wandering Merchant could stand on an obstacle. `CampaignSignatureSystem` checked its spot on the same `OnStageStarted` event on which `InfiniteMap` swaps floors, so it tested the previous stage's ground. It now spawns one frame later, after `Physics.SyncTransforms`.

Validation:
- EditMode 327/327, PlayMode 89/89, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-ground\`).
- Editor play `autoplay-editor-16` (Stage 3, god mode, 3 min): 0 errors, min 51 FPS; the village shows wide lanes and the merchant on open grass (partial `V`).

(2026-09-30: 70% of these obstacles were then removed; see "Owner play reports" below.)

VS ground features: the owner chose "VS wala" (2026-09-26), built one at a time.

**Light sources (Soul Lantern), done:**
- Spawning (`LightSourceSpawner`, added by `GameSessionManager`):
  - one attempt per second, while the game is not paused;
  - chance is 10% × Luck, capped at 50%, with at most 10 lanterns at once (VS Mad Forest numbers used on every stage; provisional);
  - a lantern appears just off-screen (8% past a screen edge, on the player's plane), never in or against an obstacle, and is recycled beyond 40 units.
- The lantern (`LightSource`):
  - has 10 HP and is tagged and layered "Enemy", so tag, layer and projectile weapons all break it;
  - is not an `EnemyController`, so nothing aims at it, the horde ignores it and breaking it is not a kill;
  - is a trigger, so it never blocks the player.
- Drops (`LightSourceDrops`, the VS table limited to Soul Hunter's pickups):
  - Gold Coin 50 (1 gold), Floor Chicken 12, Coin Bag 10 (10 gold);
  - Vacuum/Magnet 2 from level 12, Orologion/Time Freeze 2 from level 4, Rich Coin Bag 1 from level 5 (100 gold);
  - coin values are VS values from memory and provisional;
  - Rosary, Nduja and the clovers have no Soul Hunter pickup and are left out;
  - Luck does not change the drop weights yet (no source gives the formula).
- Model: `ArtSource/Generators/build_light_source.py` (Blender, reuses the pickups v2 setup) builds a stone plinth with an open iron lantern cage and a cyan soul flame over orange embers. It has 690 triangles and stands about 1.3 units tall. Previews are in `ArtSource/SH10_LightSource/Previews`.
  - `LightSourceImporter` (menu Soul Hunter/Content/Import Soul Lantern) makes URP Lit materials and a soft ground glow, builds `Assets/Prefabs/Props/SH10_SoulLantern.prefab` and sets `GameContentCatalog.SoulLantern`.
  - **The model is not yet owner-approved.**
- Validation:
  - `LightSourceTests` (T): drop weights and level gates, chance × Luck with the cap, and the prefab setup.
  - `LightSourcePlayModeTests` (R): lanterns appear off-screen and out of obstacles up to exactly 10; Whip, Garlic and Bible break one; it drops an item and is not a kill.
  - EditMode 330/330 and PlayMode 93/93, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-lantern\`).
  - Editor play `autoplay-editor-17` (Stage 1, 3 min): 0 errors, min 68 FPS; lanterns glow on the edges of the screenshots, but look small from the game camera (partial `V`).

- Owner (2026-09-26): the lantern looked too small, so `LightSourceImporter.Scale` = 1.4. The model, trigger and glow are now 1.4×, about 1.8 units tall. The model design is still awaiting approval.

**Stage items, done (owner OK'd the item list, 2026-09-26):**
- Layout (VS stage items, Dairy Plant style): `StageItemSpawner` (added by `GameSessionManager`) lays four items when each stage starts.
  - They go on the diagonals, 55/65/75/85 units from the player's position at the start (about 3-4 tiles), turned a quarter per stage.
  - Each lands on the nearest open ground (`GroundCheck`), one frame after the floor swap.
  - The next stage removes the leftovers; pooled pickups return to their pools.
- The four items: a passive, a weapon, a floor chicken, and a Vacuum/Magnet (odd stages) or Orologion/Time Freeze (even stages).

  | Stage | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
  |---|---|---|---|---|---|---|---|---|---|---|
  | Passive | Spinach | Armor | Empty Tome | Candelabrador | Bracer | Clover | Spellbinder | Duplicator | Pummarola | Attractorb |
  | Weapon | Knife | Axe | Cross | Santa Water | Fire Wand | Lightning Ring | Runetracer | Bone | Garlic | Peachone |

- Pickup (`StageItemPickup`): the passive or weapon shows its icon over the Soul Lantern's ground glow. Walking over it applies the next level's `UpgradeData` through `WeaponManager.ApplyUpgrade`, as a level-up would. With no free slot, or at max level, it pays out a coin bag instead (10 gold; provisional).
- `PickupPoolManager.SpawnChicken/SpawnMagnet/SpawnTimeFreeze` now return the spawned object.
- Validation:
  - `StageItemTests` (T): every stage has one of each kind on four different diagonals, 55-85 out, each with a level-1 upgrade, and a different passive per stage.
  - `StageItemPlayModeTests` (R): four items on open ground at their planned spots; walking onto the passive and the weapon raises each by one level; Stage 2 replaces Stage 1's items; with six weapons a weapon item becomes gold.
  - EditMode 331/331 and PlayMode 97/97, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-stageitems\`).
  - Editor play `autoplay-editor-18` (Stage 1, 3 min): 0 errors, min 68 FPS. The bot never walked out to the items, so they are not visually checked yet.

**Pressure plates (Soul Rune Plate), done (2026-09-26):**
- Rules (VS pressure plates, from the Gallo Tower, Dairy Plant and Boss Rash pages): a flat plate that starts a map event when stepped on, then waits at least 20 s. It never blocks movement.
- Soul Hunter version: `StageItemSpawner` lays four Rune Plates per stage, 30 units straight N/E/S/W of the start, between the item diagonals, on open ground. The next stage removes them. Stepping on a ready plate:
  - calls `EnemySpawner.TriggerStageEvent`, a random Swarm, Wall or Closing Ring of 16 of this stage's enemies (nothing during the boss or a stage transition);
  - plays a warning sound and a burst;
  - dims the plate's runes (MaterialPropertyBlock) for `CooldownSeconds` (20).
- All stages have plates, with provisional distance and count.
- Model: `build_light_source.py` now also builds the Rune Plate: an octagonal slab with a sunken ring of crimson runes, 308 triangles, drawn 1.3×, about 2.5 units across. `LightSourceImporter` (menu Soul Hunter/Content/Import Stage Props) builds `Assets/Prefabs/Props/SH10_RunePlate.prefab`, a Default-layer, untagged trigger (weapons ignore it), and sets `GameContentCatalog.RunePlate`. **The design is not yet owner-approved.**
- Validation:
  - `PressurePlateTests` (T): flat trigger, not an enemy, glowing runes, about 2.5 across; four plates straight out at 30.
  - `PressurePlatePlayModeTests` (R): four plates on open ground; stepping on one sends a formation; stepping again while it recharges does nothing, and it fires again after the cooldown; Stage 2 replaces the plates.
  - Editor play `autoplay-editor-19` (Stage 1, 3 min): the bot stepped on plates in real play (Closing Ring at 57 s, Swarm at 80 s, Closing Ring at 179 s), with 0 errors and min 55 FPS.
- Old Soul Hunter hazards: grey and green cylinder "Trap" and "Poison" damage discs still sit in a ring around the start of Stage 4 and 5 (`CampaignSignatureSystem.CreateHazards`). They are not a VS mechanic. Whether to keep them is an owner decision.

Found and fixed while validating the plates:
- **The owner's real save was written by tests.** The isolated copy shares the game's `persistentDataPath`, and tests that boot on their own were not redirected. Once lanterns dropped gold in every run, a test run wrote the save twice (Currency ending at 16004; the value before was probably 16002). `SaveService.TestSavePath` now makes every PlayMode test (set in `GameplayPlayModeTestBase` and `BootstrapPlayModeTests`) play on a copy of the save. The next full run left the save's hash and timestamp unchanged.
- `EnemySpawner.SpawnEnemy` skips pooled enemies that were destroyed elsewhere (the crowd test destroyed its strays; a plate event then got a destroyed enemy from the pool).
- EditMode 333/333, PlayMode 100/100, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-plates\`).

### Look, feel and sound pass (owner request, 2026-09-25)

The owner said the ground looked dated, the sound was not professional, the player was not smooth, attacks were not visible and the player should face the enemy when attacking. Owner choices: CC0 sound packs, a textured stylized ground, and a camera tilted to about 55 degrees.

- Camera: `Main Camera` was a child of the player with `CameraFollow` disabled, looking straight down, so only the tops of heads showed. It is now a scene root and `CameraFollow` follows at a 55-degree pitch (orthographic size 12, frame-rate-independent smoothing). The damage numbers were sized for the old top-down camera, which saw them edge-on. They now face the camera at 7 pt.
- Player facing: `PlayerController` mirrored the 3D model with `localScale.x = +-1` (it snapped and never faced up or down; the child camera was mirrored with it). The hero model now turns smoothly (slerp) toward where it walks. When a directional weapon fires, the hero turns to the enemy that weapon aims at for 0.45 s (`AutoAttackWeapon.ShowAttackOnHolder`, called before `Attack()` because a shot can kill its target). Garlic, Bible, Laurel, Pentagram and Peachone do not turn the hero. The model's front is worked out from its thigh bones, because Kael's authored visual faces +Z while the hero prefabs face -Z.
- Attack animation: `Attack` was a full-body state reached only from the manual attack input, so auto-weapons never showed it. `PlayerAnimatorSetup` moves it to an `UpperBodyAttack` override layer (mask: Spine and its children, `SH10_UpperBody.mask`) at 1.6x speed. Each weapon shot triggers it, at most every 0.6 s, and the legs keep running.
- Rendering: the pipeline used the URP 2D Renderer, which ignores 3D lights and shadows. `RendererSetup` switches to a 3D Universal Forward renderer (`UniversalRenderer.asset`) with HDR off, intermediate texture Auto and shadow distance 45. `Renderer2D.asset` stays on disk, unused (only the URP 2D template test scene has a 2D light). The stage light casts hard shadows at strength 0.6. The faceted shader (`SoulHunter/Review/FacetedPreview`) is now lit by the main light with shadows and has a ShadowCaster pass. The hero, bosses and stage obstacles cast shadows. For performance, the horde, pooled projectiles and pickups do not, and off-screen enemies do not animate (`AnimatorCullingMode.CullCompletely`).
- Ground: `build_ground_textures.py` (Blender + numpy) makes 16 seamless 512 px stylized detail textures (grass, dirt, cobble, flagstone, mud, planks, snow, ice, sand, marble, crystal plates, carpet, stone, leaves, water, grain) in `Assets/Art/Textures/Ground`. `build_grounds.py` box-maps world-space UVs (4 units per repeat on the ground, 2 on props) and lists each material's texture in the manifest. `StageFloorImporter` makes URP Lit materials: texture tinted by the palette colour divided by the texture mean (0.8), matte, and emissive for glow and vein keys. The floor receives shadows and the obstacles cast them.
- Sound: every effect and all 11 music tracks are now CC0 recordings, installed by `install_cc0_audio.py`. Effects come from Kenney (impact, interface, RPG, sci-fi, jingles) and OpenGameArt (artisticdude's RPG Sound Pack and Swishes, rubberduck's creature SFX). Music is by emmama (menu), cynicmusic, mintodog, katana, bobjt and isaiah658. Each licence page was checked as CC0 on 2026-09-25, and sources are listed in `Assets/_Project/Audio/CC0/CREDITS.md`. The script loudness-normalises each file and writes Ogg Vorbis. Music streams; effects decompress on load. `AudioManagerPrefabBuilder` prefers these files and keeps the synthesised clips as a fallback. The jingles were chosen by measured pitch trend (rising for level-up, stage clear and unlock; falling for game over), not by ear.

Validated by `PlayerPresentationPlayModeTests` (R: camera pitch and centring; hero turns smoothly and is not mirrored; faces the enemy and plays the upper-body pose while the legs keep their state; stage light shadows; the horde has no shadows or off-screen animation), `HeroAttackLayerTests`, `RenderingSetupTests` and `Cc0AudioTests` (T), and both full suites: EditMode 301/301, PlayMode 42/42, 0 shader errors, save file untouched (`D:\SoulHunter-Validation\run-20260925-feel\`). Performance (Windows build, this machine = the i5-8350U / UHD 620 reference, 1080p pixel count, Medium): Stage 10 at 20:30 with ~300 enemies went from 85-88 FPS (2D renderer, top-down) to 48-57 FPS with shadows on everything. Turning off horde, projectile and pickup shadows, HDR and the always-on intermediate texture, and culling off-screen animation brought it to 76 FPS average and a 37.5 FPS 1% low. A run with ~170 enemies gave 111 FPS. Nobody has looked at it or listened to it in real play yet: `V` for the look, animation and mix is still unverified.

### Projectiles, weapon effects, gameplay VFX and pickups (owner request, 2026-09-25)

The owner said VFX, gems, chest, gold and bullets were not proper. Found:
- Almost every weapon fired or placed the same orange sphere, `Bullet.prefab`: Magic Wand, Knife, Thousand Edge, Fire Wand, Holy Wand, Gun, Bone, Runetracer, Cherry Bomb (bomb and explosion), Song of Mana, Clock Lancet, Lightning Ring, Peachone, Pentagram and Santa Water. The Axe threw a cylinder and the Cross a magenta sphere (missing material).
- `Bullet.prefab` carries a `Projectile` script (moves the object forward and blocks `ProjectileDamage`) and is scaled 0.2, so area weapons flew away with a 0.1-unit hit area. Santa Water pools, lightning strikes and Peachone blasts were affected, and Bone and Runetracer were dragged by it.
- Peachone's `BirdVisual` was never assigned, so it never dropped a bomb, and its bird circled vertically (X-Y).
- The gameplay pickups (`XP_Gem`, `Chest`, `HealthChiken`) drew a placeholder cube over their SH10 model: the yellow squares in play. The gold coin lay flat, reading as a brown disc.
- No particle effect existed. `VFXPoolManager` was in no scene, so its death and chest effects never played.
- Knife, Thousand Edge, Whip, Axe and Cross took their "facing side when no enemy" from the player's `localScale.x`. That stopped working when the hero stopped mirroring.

Now, `WeaponVfxBuilder` (menu Soul Hunter/Content/Build Weapon VFX) generates everything from Unity meshes, the SH10 models, procedural textures and particle systems, using the `SoulHunter/VFX/Particle` shader (additive, or alpha for smoke):
- **Weapon prefabs:** 18 prefabs in `Assets/Prefabs/Weapons/Projectiles`, each with the components its weapon code expects.
  - Flying (moved by `Projectile`): magic bolt, fireball, holy bolt, gun tracer, thrown knife, Thousand Edge knife, mana shot, and a Cherry Bomb that explodes when its flight ends.
  - Physics (Rigidbody velocity + `ProjectileDamage`): spinning bone, glowing rune, tumbling SH10 axe.
  - Boomerang: a spinning SH10 cross.
  - Zones that stay where they are placed, with hit areas the size of what is drawn: holy water pool (radius 1.4), lightning strike with a new jagged bolt each time (0.9), Peachone blast (0.9).
  - Visual only: Pentagram flash, Clock Lancet freeze beam, Peachone bird.
  - Weapon prefab fields are rewired, and no weapon references `Bullet`, `Axe_Model` or `Cross_Model` any more.
- **Gameplay VFX:** `Resources/VfxLibrary` holds hit spark, enemy death (soul puff), gem pickup, gold pickup, heal, chest open, level-up and explosion. `VFXPoolManager` creates itself on first use, pools up to 32 of each kind and throttles frequent kinds. Hooks: enemy hit and death, XP gem, gold/magnet/freeze pickups, chicken, chest, level-up, Soul Burst.
- **Pickups:** placeholder cubes hidden. Gem, chest, chicken, magnet, freeze and coin bob and turn (`PickupIdleMotion`, which moves only the visual child). Gems glow cyan and the coin stands up and glows gold, using a new `_EmissionColor` on the faceted shader. The chest has a gold sparkle beacon. Visuals were enlarged for the tilted camera: gem x1.6, chest x1.4, coin x1.4, chicken x1.2.
- Peachone's bird now circles on the ground plane and drops blasts under itself. Every facing fallback reads `PlayerController.FacingDirection` (`AutoAttackWeapon.HolderFacing`).

Validated by `WeaponVfxTests` (T: no placeholder references, components per weapon object, library complete and non-looping, pickups without cubes) and `WeaponSweepPlayModeTests` (R: each of the 19 weapons in the real game, never the placeholder objects. Every one hurts enemies 3 units away, Clock Lancet freezes them and Laurel is equipped; kills, hits and level-ups play their effects). Both full suites: EditMode 326/326, PlayMode 62/62, save untouched (`D:\SoulHunter-Validation\run-20260925-vfx\`). `WeaponAimingTests` now expects the knife fallback along the hero's facing, which starts up the screen (+Z); it was +X from the old mirroring. Benchmark (reference laptop, 1080p pixel count, ~300 enemies): 78-84 FPS average and a 43-48 FPS 1% low. In-game screenshots were checked (partial `V`). Look and feel in hands-on play are unverified; the effects are Soul Hunter's own and are not VS art.

### AutoplayBot: the game plays itself for review (owner request, 2026-09-25)

`AutoplayBot` (Assets/_Project/Core/Game) lets an agent watch real runs. It flees the horde only when enemies get close (so weapons hit), collects gems, chests and food when hurt, slides around obstacles, prefers upgrades of owned weapons, then new weapons, then passives, and closes chests and the merchant. It records `shots/*.png` (timed and at level-ups, chests, stages and death; a player build captures the UI), `timeline.jsonl` (every 5 s), `events.jsonl` and `summary.json`. It runs only with `-sh-autoplay <folder>` (options `-sh-autoplay-minutes/-character/-stage/-shots/-god`). It plays on a throw-away save, and when started any other way (editor, tests) it first redirects saving with `SaveService.UseFile`.

The first test version added 22 gold to the owner's real save (4482 -> 4504), because `EconomyService` saves on every pickup. The value was restored to 4482, and `AutoplayBotPlayModeTests` now fails if the real save changes. The runner is `Tools/Autoplay/run-autoplay.ps1` (it builds in the isolated copy and plays). First runs, as Kael on stage 1 for 3 min: 0 errors, 59-60 FPS at 1280x720. The first bot version kited too far and got 78 kills. The tuned version reached level 18 with 1636 kills and 159 gold. That kill count is far above a typical VS run at 3:00, because the enemy minimum (30) refills instantly; it is a balance observation, not changed. Validated by `AutoplayBotPlayModeTests` (R); EditMode 326/326 and PlayMode 63/63 with the save unchanged (`D:\SoulHunter-Validation\run-20260925-autoplay\`).

The owner then rejected the online CC0 samples shown (Kenney and Quaternius coins, gems and chests, and the Kenney Particle Pack). They asked for modern, Soul Hunter-style 3D assets and VFX, made in Blender or Unity or taken from the Unity Asset Store. Asset Store packs need the owner's account (My Assets); agents cannot download them.

Editor play (owner request, 2026-09-25: "play it yourself in the Unity Editor"): `AutoplayEditorLauncher` has the menu items Soul Hunter/Autoplay/Play 3 (or 10) Minutes In Editor, and a CLI mode (`-executeMethod SoulHunter.Editor.Autoplay.AutoplayEditorLauncher.RunFromCommandLine -sh-autoplay <folder> -sh-autoplay-exit`). It opens Bootstrap and enters Play Mode with the bot; editor screenshots use the Game view with the UI. First editor run, on the isolated copy in a GUI editor for 3 min as Kael: level 14, 797 kills, 88 gold, 0 errors, min 57 FPS, save unchanged (`D:\SoulHunter-Validation\autoplay-editor-01\`).

Seen while playing:
- The level-up screen is unfinished next to VS: plain white buttons with red internal type names ("SongOfMana Lv.2 (Lv 2)"), no icon, description or NEW tag.
- The player HP bar is a red strip across the whole bottom of the screen instead of a small bar under the hero.

Flaky: one full PlayMode run stopped once in the Loading scene while booting (`WeaponSweep` Peachone), and the next test threw a NullReferenceException. The fixture then passed 21/21 twice.

### Pickups v2: Soul Gems, Soul Coins, Soul Reliquary chest (owner-approved, 2026-09-25)

Owner flow: online samples first, then approval. The CC0 samples were rejected in favour of "modern Soul Hunter-style" designs. `ArtSource/Generators/build_pickups_v2.py` builds them in Blender; concept renders are in `ArtSource/SH10_Pickups_v2/Previews`. The owner approved the close-ups:
- Soul Gems in three tiers: a cyan shard, a violet cluster, and a crimson crystal with a gold band, each with a glowing core.
- Soul Coins in bronze, silver and gold: reeded edge, bead ring, crescent and a soul flame.
- The Soul Reliquary chest: curved lid, iron bands with rivets, a cyan soul-light seam, a skull with glowing eyes and a shield lock.

`--export` writes `Assets/Art/Models/SH10_Pickup_*.fbx` and `materials.json`. `PickupV2Importer` (menu Soul Hunter/Content/Import Pickups v2) makes URP Lit materials (metals, and soul parts with emission), rebuilds `XP_Gem`, `SH10_Gold_Coin` and `Chest` with these models and a soft ground glow, and sets up `TieredVisual`:
- Gem tier by XP: under 25 cyan, under 100 violet, else crimson.
- Coin tier by gold: under 5 bronze, under 25 silver, else gold.

`EnemyData.ExperienceValue` is new (10 for enemies, 50 for elites, 250 for bosses; provisional balance). `EnemyDrop` passes it to `PickupPoolManager.SpawnGem(position, value)`, so elites and bosses drop bigger gems. `WeaponVfxBuilder` no longer touches these three prefabs.

Validated by `PickupV2Tests` (T: tier thresholds, approved models and materials per tier, chest model, elite and boss XP) and `WeaponSweepPlayModeTests.Gems_AndCoins_ShowTheirTierByValue` (R). Both suites: EditMode 344/344, PlayMode 64/64, save unchanged (`D:\SoulHunter-Validation\run-20260925-pickups\`). In-game screenshots were checked (partial `V`). From the 55-degree camera, the chest shows mostly its lid, with the skull and seam at the front.

### VS chest-opening screen and the frozen chest VFX (owner request, 2026-09-28)

Owner: "vs mein chest milne per VFX hai wo nahi dikh raha ... wiki se check karo or effect dalo". The chest screen was a text line over the running game with a white "Claim and Continue" button.

**Frozen VFX.** The chest-open effect started in the same frame the game paused, so its particles froze on their first frame and never showed. The level-up effect had the same problem. `VFXPoolManager.PlaysWhilePaused` runs those two kinds on unscaled time; combat effects still freeze with the game.

**The opening screen** (`ChestOpeningScreen`, `ChestPreviewStage`, opened by `ChestUI`). It follows vampire.survivors.wiki/w/Treasure_Chest (the page, its Audio section and its patch notes), in Soul Hunter's own art:
1. The game pauses and the screen dims.
   - The approved Soul Reliquary chest drops in with a flash and a ring (the intro blast), and "Treasure Found" plays.
   - An OPEN button rotates in.
2. OPEN:
   - the lid opens (the lid parts hang on a hinge at the back) and approved gold Soul Coins burst out;
   - the Treasure A/B/C jingle plays for a 1-, 3- or 5-item chest;
   - the background lights up on 3- and 5-item chests only ("Stopping background panel lighting up in lvl 1 chests");
   - the gold counts up, scales in and pulses;
   - each item spins past as a reel and stops, with stars behind it;
   - 5-item chests add fireworks.
3. DONE rotates in once the gold has counted up, and resumes the game.
- The 3D chest is a copy far below the stage (y = -1000), filmed by its own camera into a RenderTexture. The copy is laid out one frame late, because the renderer bounds of a copy made that frame read the stage position. The model's own up (body to lid) and front (towards the lock plate) are measured, and the copy is turned upright facing the camera; the FBX is turned inside the chest prefab.
- Effects use the approved VFX v2 sprites (`GameContentCatalog.ChestFx*`): magic circle, shock ring, impact star, sparkle.
- `ChestLogicController.LastResult` (`ChestResult`, `ChestReward`) records what a chest gave. `WeaponManager.LastEvolvedIcon` gives an evolution its base weapon's icon, and a union its own icon.

**Skipping** (`ChestSkipRules`, wiki Skipping):
- the first six chests on a save cannot be skipped;
- after that, 1- and 3-item chests skip with Esc, right click or gamepad B, jumping to the end;
- 5-item chests can be skipped after 50 five-item chests or 500 chests;
- once 5-item chests can be skipped, coin-only chests play a quick in-game effect with no screen;
- `GameData.ChestsOpened` / `FiveItemChestsOpened` count them (repaired by `SaveService.Migrate`);
- while the screen is open, the Pause action is off (as in `OptionsMenuUI`), so Esc does not also open the pause menu;
- the wiki's third unlock ("seen the final fireworks") has no Soul Hunter counterpart.

**Sounds:** four unused CC0 Kenney Music Jingles, picked by measured length and rising pitch, not by ear: TreasureFound HIT12 (0.28 s), TreasureA HIT01 (0.51 s), TreasureB NES13 (1.05 s), TreasureC NES00 (1.76 s). They were added with `install_cc0_audio.py --only`, credited in `CREDITS.md` and wired by `AudioManagerPrefabBuilder`.

Provisional: every timing, the coin counts (12/22/36) and the layout.

**First six chests: 1-1-3-1-1-5** (owner: "haan 1-1-3-1-1-5 bhi add karo", 2026-09-29; wiki Treasure_Chest: "The first six chests picked up in a save will always drop items in the set sequence 1-1-3-1-1-5"). `ChestLogicController.ItemCountFor` uses `FirstChestSequence` while `GameData.ChestsOpened` is under 6, whatever Luck or the roll; from the seventh chest the Luck roll applies again. An Arcana chest counts as one of the six (ChestUI counts every chest) but gives its Arcana; the wiki does not say how Arcana chests fit the sequence. The owner's save had opened 5 chests (2026-09-29), so its next chest is the sequence's 5-item chest. Tests: `ChestRewardTests.FirstSixChests_FollowTheVsSequence_ThenLuckRolls` (T) and `ChestFlowPlayModeTests.FirstSixChestsOnASave_Give_1_1_3_1_1_5_Items` (R: six real chests collected and opened). The first full run failed one older test: a new weapon from the chest fired in the collection frame and replaced Treasure Found as `AudioManager.LastClip`; the chest sound checks now read `AudioManager.RecentClips` (last 8). EditMode 471/471, PlayMode 171/171, save untouched (`D:/SoulHunter-Validation/run-20260929-firstchests2/`). Not yet played by the owner.

`AutoplayBot` and `PerformanceBenchmark` press OPEN, then DONE (`ChestUI.OpenAndClose`), instead of closing the old panel by reflection.

Validation:
- `ChestFlowPlayModeTests` (R):
  - a collected chest pauses the game and plays Treasure Found, and cannot be skipped among the first six;
  - OPEN opens the lid, bursts coins, plays the size jingle, reveals every item and counts the gold to the chest's gold;
  - DONE resumes the game, restores the pause key, counts the chest on the save and cleans up the preview;
  - a 5-item chest lights up, has fireworks and plays Treasure C;
  - the seventh chest can be skipped;
  - a coin-only chest opens no screen once 5-item chests are skippable;
  - the chest effect keeps playing while paused.
- `ChestPreviewPlayModeTests` (R): the camera is on the lock's side; OPEN turns the lid about its hinge, back and up over it.
- `ChestRewardTests` (T): the chest result, the skip rules and the pause-time effects.
- The first full run found DONE appearing before the gold had counted up on 1-item chests (fixed). While building the preview, the chest was filmed from the wrong side, first by guessing its front and then from stale bounds; the measured layout above fixed that.
- EditMode 470/470. Full PlayMode 169/170: the one failure was a test race (a weapon's sound in the collection frame replaced Treasure Found as the last clip). The test now turns weapons off, and the chest fixtures then passed 7/7. 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20260929-chest4/`).
- Editor play `autoplay-editor-55` to `-58` (Stage 1, 4 min each, 0 errors): screenshots of the closed chest (front, skull, lock, glowing seam) and the open chest (lid up, +144 gold, "Cross NEW!") (partial `V`).

Not yet played or heard by the owner.

### Blood on enemy deaths (owner request, 2026-09-29)

Owner: "vs mein enemy ke marne per blood effect dikhta hai wiki mein check karo or apply karo". What the wiki says (fetched 2026-09-29):
- vampire.survivors.wiki/w/Options: VS has a "Disable blood" option (Display section), added in v1.6.0 (Updates/v1.6.0 patch notes). So kills show blood unless it is switched off.
- The wiki does not describe how the blood looks. Enemy pages show only per-enemy death animations: the body breaks into pieces that scatter and fade (for example Skeleton, 18 frames × 140 ms = 2.5 s; Pipeestrello, 13 × 140 ms = 1.8 s).

Before, a kill showed only a faint blue soul puff, and Disable Blood only switched the Blood Arenas floor stains.

The first version (a small dark spray and a floor splat, commit `3e7d1271`) was rejected by the owner ("ye sahi nahi hai"). Online samples were shown for comparison (artifact "Soul Hunter Blood Samples": Vefects Free Blood VFX, Vefects Blood VFX, RVFX Blood Effects Pack, and CC0 OpenGameArt sheets by sinestesia, overcrafted and tobiasm). The owner then asked: "blood effect enemy ke neck se spray particle ki tarah VFX set karo".

The owner played that version (a neck jet plus a floor splat, commit `fa694b1c`) and decided (2026-09-29): "jameen per blood effect nahi chahiye, haa uper jo blood neck ke pass dikh raha hai use 300 particle mein dikhao". So there is no blood on the ground, and the neck spray has 300 particles.

Now (`VFXPoolManager.PlayBlood(neck, awayFromHero)`, called from `EnemyDrop` next to the soul puff):
- **Neck spray** (`VfxKind.BloodSpray`, `VFX_BloodSpray.prefab`, built by `WeaponVfxBuilder.BuildBlood`, menu Soul Hunter/Content/Build Blood VFX):
  - One particle system emits exactly 300 particles (`VFXPoolManager.SprayParticles`).
  - They come in three pulses, like a heartbeat: 140, then 100, then 60 particles, each pulse 10 bursts 0.02 s apart, starting at 0, 0.25 and 0.5 s.
  - The particles are stretched bright-red streaks and small drops in a 14° cone, alpha blended, living in world space, so the jet arcs and falls under gravity. Each lives 0.4–0.7 s; the spray is over by about 1.4 s.
- The jet is tilted 50° up and turned away from the hero, ±25° at random (`SprayDirection`).
- The neck is 80% of the way up the enemy's drawn body (`VFXPoolManager.NeckOf`, from `EnemyDrop.BodyBounds`, read at death while the model is still shown). The models share no neck bone.
- **Nothing is left on the ground.** The floor splat, its prefab (`VFX_Blood`) and its texture and material were removed.
- Disable Blood switches the spray off; the soul puff stays.
- Level 7 ghosts (`EnemyController.IsSpectral`) do not bleed. The wiki is silent on this; it is Soul Hunter's choice, because ghosts have no body.
- **Fewer particles when many die at once** (owner chose option 2, 2026-09-29):
  - Up to 8 sprays in the air (`FullSprays`) get all 300 each.
  - A spray started while more are playing shares that budget: 300 × 8 / (sprays playing + 1), at least 60 (`MinSprayParticles`) (`SprayParticlesFor`).
  - `SetSprayParticles` lowers each pulse's burst count before the spray is switched on. With the pool full (32 sprays), a new spray gets 80.
  - That is about 4,800 particles at most, instead of 9,600.
  - Values are provisional.
- At most 32 sprays play at once, with the oldest reused first. Kills in the same 0.01 s make one spray (the blood throttle). Colours, sizes, angles and timings are provisional. The spray is the same size on elites and bosses. **The look is not yet owner-approved.**
- `AutoplayBot` option `-sh-autoplay-no-blood` plays with Disable Blood on, for A/B checks.

Validation:
- `BloodPlayModeTests` (R):
  - A kill sprays from the neck (within 0.05 of 80% of the body height), away from the hero, rising at 50°.
  - It is one alpha-blended system with no ground splat.
  - Counting every particle it emits gives exactly 300, over 100 of them in the air together, and none left after 1.6 s.
  - With Disable Blood on there is no spray, but the puff plays. A ghost sprays none. Kills bleed again once the option is off.
- EditMode 471/471, PlayMode 173/173, 0 errors, save untouched (`D:/SoulHunter-Validation/run-20260929-spray300/`).
- Editor play `autoplay-editor-62` (Stage 1, 1.5 min, a screenshot every second): 0 errors. The shots show dense red jets spraying from killed enemies' necks and nothing on the ground (partial `V`, `run-20260929-spray300/editor-play-spray300-zoom.png`). That run's FPS fell from 70–99 to 36–51 once kills sped up (1,221 kills in 90 s).
- **Cost (GUI editor A/B, Stage 10, god mode, player level 60, 3 min, about 36 kills per second):**
  - blood on (`-63`): 6,556 kills; after 15 s, average 45.6 FPS, last minute 34.9, minimum 26;
  - blood off (`-64`): 6,607 kills; average 47.8, last minute 40.3, minimum 33.
  - About 5–13% slower, and the blood-on run went below the 30 FPS hard minimum in the GUI editor.
  - A repeat blood-on run (`-65`) killed only 2,981 and averaged 42.1 (minimum 34), so the runs are noisy.
  - Editor numbers are not build numbers; no build benchmark was run (`P` unverified).
- **With the budget (option 2):**
  - `BloodSprayBudgetTests` (T): the pulses add up to 300; up to 8 sprays get 300 each; above that fewer, never rising and never under 60.
  - `BloodPlayModeTests.ManyKillsAtOnce_ShareTheBudget_AndALoneKillGets300Again` (R): of 20 kills 0.02 s apart, the first 8 sprays have 300 and the last fewer (at least 60); a lone kill afterwards gets 300 again.
  - The 300-count test now warms up one spray first, because the first spray of a run can hitch past its first particles' 0.4 s life. It counted 272 once before that.
  - EditMode 473/473, PlayMode 174/174, save untouched (`D:/SoulHunter-Validation/run-20260929-spraybudget3/`).
  - GUI editor, same Stage 10 run:
    - with the budget (`-66`): 6,448 kills; average 42.1 FPS, last minute 34.1, minimum 30 (it was 26 in `-63`);
    - blood off (`-67`): only 3,804 kills; average 35.9, minimum 30.
    - The editor runs are too noisy to measure the saving; only a build benchmark at a high kill rate can (`P` unverified).
  - Stage 1 (`-68`): 0 errors, minimum 68 FPS.
- **Build benchmark** (owner: "build benchmark bhi chalao", 2026-09-29):
  - Setup: Windows build on the reference laptop (i5-8350U, UHD 620), Medium, 1366x768 window rendered at 1080p's pixel count, V-Sync off.
  - Built in the isolated copy to `D:/SoulHunter-Validation/run-20260929-bloodbench/build*/`; `D:\Unity Builds\SoulHunter` untouched.
  - New `PerformanceBenchmark` options:
    - `-sh-benchmark-level N`: start the hero at level N, for a high kill rate.
    - `-sh-benchmark-no-blood`: Disable Blood on.
    - `-sh-benchmark-blood-ab`: the same hero measures each late segment with blood, without, then with it again.
    - Each segment now also records its kills.
  - **Separate runs cannot compare blood.** The level-up picks are random, so loadouts and kill rates differ. In the first level-60 runs, the blood-on heroes drew Pentagram and Santa Water and killed 4–5× more (3,800–4,800 against 1,065–1,140 in the Stage 1 late segment), which swamped any blood cost.
  - Level-1 hero (first build: the level option did not apply, the blood switch did), Stage 10 at 20:30, about 290 enemies:
    - blood on, 3 runs: 60.8 / 75.8 / 64.5 FPS average, 1% low 33.7 / 46.3 / 34.5;
    - blood off, 2 runs: 74.1 / 69.7, 1% low 44.2 / 41.9.
    - Kills were not recorded; the ranges overlap.
  - **In-run A/B, level-60 hero** (`ab-lv60-1` and `-3`; `-2` is invalid, see below), average FPS for blood / no blood / blood again:
    - run 1: Stage 1 late 95.3 / 65.9 / 78.9; Stage 10 late 72.1 / 67.8 / 76.9;
    - run 3: Stage 1 late 57.2 / 44.6 / 45.7; Stage 10 late 47.1 / 31.9 / 37.9.
    - In every pair the no-blood segment was no faster; it also had the most kills (for example 782 against 287 and 434).
    - **The spray's cost is below the noise in a build.** FPS follows the kill rate and the loadout.
  - **Seen, not changed:**
    - With a level-60 hero at 300–1,985 kills per 30 s, the 1% low falls to 7–15 FPS with or without blood, well under the 30 FPS hard minimum. Heavy kill rates themselves (drops, gems, death effects) cost frames; this needs its own performance pass (`P`).
    - Run `-2` stayed paused (timeScale 0) from its second segment. First read as a level-up problem; it was a chest (fixed, below).
  - Found by the full PlayMode run: `AutoplayBot` never removed its level-up, stage, game-over, victory and damage handlers. A gem collected while a test tore down started a coroutine on the destroyed bot (`MissingReferenceException`, `AutoplayBotPlayModeTests`). All of them are now removed in `OnDestroy`.
  - EditMode 473/473, PlayMode 174/174, save untouched (`D:/SoulHunter-Validation/run-20260929-bloodbench2/`).

### Chest screen froze the game (fixed 2026-09-29)

Owner: "2 wala karo" (fix the benchmark stall).

A stall watchdog in `PerformanceBenchmark` (records who holds the pause when the game stays paused for 15 s, then closes a chest screen or takes a level-up card; each stall is listed in the results) showed:
- The pause was held by the scene's `ChestUI` on `--- MANAGERS ---/UI_Bindings`, the one `PickupPoolManager` hands chests to.
- That `ChestUI` has no canvas above it. It put the chest screen on `FindFirstObjectByType<Canvas>()`, which could be a temporary canvas, such as the character unlock notice (`CharacterUnlockTracker`) or the merchant's shop.
- When that canvas was destroyed, the screen went with it: once it stopped at "Revealing", once "no screen". The chest's pause was never released, so **the game stayed frozen for the rest of the run.**
- A real player could hit this when a chest opens while an unlock notice or the shop is up. In the level-60 benchmark, the start-level XP unlocks characters (Aria at player level 20).

Fix:
- `ChestUI.ScreenCanvas()` gives the chest screen its own overlay canvas, owned by the `ChestUI`: sorting order 400 (above the HUD, below the unlock notice's 500), CanvasScaler at 1920x1080 like the HUD.
- `ChestUI.Update` releases the pause if the screen is ever gone while a chest is open.
- `GameTime.PauserNames()` lists who holds the pause, for diagnostics.

Validation:
- `ChestFlowPlayModeTests.TheChestScreen_HasItsOwnCanvas_AndSurvivesATemporaryCanvasGoingAway` (R): the pool's `ChestUI` screen sits on its own canvas; a temporary overlay canvas destroyed mid-chest leaves the screen working and DONE resumes; a screen destroyed while open no longer leaves the game paused.
- EditMode 473/473, PlayMode 175/175, save untouched (`D:/SoulHunter-Validation/run-20260929-chestcanvas/`).
- Level-60 A/B benchmark, 5 builds' runs after the fix:
  - no run froze (timeScale 0 at the end of a segment is a chest screen open at that moment);
  - with a 5 s watchdog, one "stall" was a 5-item chest that legitimately takes 6–7 s, so the threshold is now 15 s; the last two runs had 0 stalls and 0 errors.
- Blood A/B in those runs again showed no cost (blood segments as fast or faster than the no-blood ones).

Seen, not changed: with a level-60 hero, FPS falls from segment to segment regardless of blood. Run `ab-lv60-5` went 41.9, 31.0, 20.3, then about 21 in Stage 10, with 2,500–4,500 kills per 30 s. Something builds up with heavy killing; this belongs to the heavy-kill performance pass (`P`). Fixed by the heavy-kill pass below.

### Heavy-kill performance pass (owner: "heavy kill performance pass shuru karo", 2026-09-29)

Tools:
- `PerformanceBenchmark -sh-benchmark-heavy`: a level-N hero (`-sh-benchmark-level 60`) plays Stage 1 from 20:30 as six back-to-back 20 s windows. Each window records FPS, kills, enemies, gems, playing VFX, live game objects, log lines, save-file writes, gen-0 GCs and managed memory.
- `-sh-benchmark-profile <folder>` (development build: `SOULHUNTER_BUILD_DEVELOPMENT=1` for `SoulHunterBuildRunner`) captures 5 s of windows 1 and 6.
- `ProfileSummary` (menu Soul Hunter/Debug/Summarize Profile, or `-executeMethod ...ProfileSummary.RunFromCommandLine -sh-profile <file.raw>`) writes the main thread's time per marker.
- `D:\SoulHunter-Validation\heavy-run.ps1` syncs the copy, runs both suites, builds, benchmarks and summarises.
- The benchmark turns off Pause When Unfocused on its throw-away save; a run sat paused when its window lost focus.

What the profile showed (reference laptop, release build):
- Live game objects grew from ~7,000 to 30,000 in two minutes, while FPS fell from 72 to 18.
- **Floor chickens and coins piled up without limit:** a kill rolled a chicken at 5% and a coin at 2%.
- Every coin and chicken on the ground ran `Physics.OverlapSphereNonAlloc` each frame (466 calls, 2 ms a frame), and 850 bobbing pickups each ran their own `Update` (1.2 ms).
- Gold pickups wrote the whole save file on every coin (up to 75 writes per 20 s).
- Per-kill and per-pickup `Debug.Log`s (for example "Rare Drop: Floor Chicken!" 733 times) each wrote a stack trace.

Changes:
- **Owner decision "VS rule: no enemy drops"** (AskUserQuestion; VS wiki: Floor Chicken, Gold Coin (pickup), Coin Bag, Vacuum and Orologion drop from light sources, and enemies drop Experience Gems). `EnemyDrop` now leaves only the gem.
  - Magnet 1%, Time Freeze 1%, coin 2% and chicken 5% are gone; they come from Soul Lanterns, stage items, chests and Gold Fever.
  - Defect fixed with it: a chicken used to **replace** the kill's gem, so 5% of kills gave no XP.
  - `EnemyData.DropChanceChicken/DropChanceGold/GoldValue` and `EnemyDrop.ChickenDropChance/GoldDropChance` stay on the assets but are unused. `GoldRewardTests.Elites_DropMoreAndBiggerCoins_ThanStageEnemies` was removed (superseded rule).
  - Balance note: fewer heals from kills; the owner chose it knowing that.
- `PlayerReach.Within(point, radius)`: the same answer as an overlap sphere on the Player layer (`Collider.ClosestPoint` against the player's Player-layer colliders, after a distance reject), with no physics query. Used by `Pickup` and `HealthPickup`.
- `PickupTicker` ticks every `Pickup` and `HealthPickup` in one loop (`ITickedPickup.Tick` replaces their `Update`). `PickupIdleMotionRunner` moves every bobbing pickup in one loop and skips pickups more than 30 units from the hero.
  - Found by the tests: a pooled copy can carry its template's list index (every pooled chicken said 1, so none was ticked and none could be picked up). Both runners now trust an index only when the list agrees.
- `SaveService.SaveSoon()`: gold is saved at most every 2 s (`SaveSoonSeconds`). `SaveFlusher` writes a pending save when due and on quit, so a crash can lose at most the last 2 s of gold. `EconomyService.AddGold` uses it; spending still saves at once. `SaveService.SavesWritten` counts writes.
- Removed hot info logs: rare drops, Healed, +gold, Shadow Scythe. "Game Saved" is logged in the editor only.

Validation:
- `PickupPerformancePlayModeTests` (R):
  - `PlayerReach` matches `Physics.CheckSphere` on 400 random points;
  - a coin and a chicken next to the hero are collected, and one 12 units away is not;
  - every enabled pickup is on the ticker once;
  - the bobbing runner moves pickups and drops collected ones;
  - a burst of 10 coins writes no save at once and exactly one after 2.5 s.
- `EnemyDropPlayModeTests` (R): 60 kills at Luck 10 give 60 gems and no chicken, coin, magnet or time freeze.
- EditMode 472/472, PlayMode 181/181, save untouched (`D:/SoulHunter-Validation/run-20260929-heavykill-fix4/`).

Benchmark (release build, level-60 hero, Stage 1 from 20:30, six 20 s windows):

| Run | Before (`run-20260929-heavykill`) | After (`-fix4`) |
|---|---|---|
| Live objects | 7,062 → 30,063 | flat, 7,500–9,200 |
| FPS by window | 72 → 51 → 29 → 23 → 20 → 18 | run 1: 70 / 63 / 61 / 68 / 74 / 68; run 3: 64 / 64 / 63 / 63 / 64 / 65 (up to 2,790 kills per window) |
| 1% low | fell to 9 | runs 1 and 3: 30–43 |
| Save writes per window | up to 75 | 0–9 |

- The profiled window 6 averages 16.7 ms (development build). Pickups are gone from the top of the profile: bobbing 0.23 ms. The rest is the normal horde (`EnemyController.FixedUpdate` 1.4 ms for ~300 enemies).
- Still under the hard minimum: run 2's loadout killed 5,400–7,500 per window (over 300 kills a second) and held 25–49 FPS average with a 1% low of about 25. Its two paused windows came from the focus-loss pause (now off in the benchmark). Kill handling itself (death, gem, VFX, blood, damage numbers) is the next cost at such rates.
- Not measured on a full 30-minute run or on the 8 GB reference.

Not yet played by the owner.

Not yet played by the owner.

### Lantern drop balance and the dying body (owner, 2026-09-29)

Owner: "lantern drops ka balance pass karo, blood drop ko bhi balance karo kyun ki enemy marne per gayab ho jata hai per blood dikhta rahta hai".

**Soul Lanterns, following vampire.survivors.wiki (Light source and Luck pages, fetched 2026-09-29).** The drop weights and level gates already matched the wiki table (Gold Coin 50, Floor Chicken 12, Coin Bag 10, Vacuum 2 from level 12, Orologion 2 from level 4, Rich Coin Bag 1 from level 5, and the Rosary / Nduja / clover stand-ins). Coin values 1 / 10 / 100 are now checked against the Gold Coin (pickup), Coin Bag and Rich Coin Bag pages. Added:
- **Luck** (wiki Luck, "Higher rarity pickups": `itemWeight = rarity x totalLuck`): every drop except Gold Coin and Coin Bag weighs its rarity × Luck (`LightSourceDrops.Weight`, `Pick(level, luck, roll)`). Before, Luck changed nothing.
- **Per level**: the wiki gives every VS stage its own chance, cap and maximum. The owner had no preference, so the stage-bonus climb is used (`LightSourceSpawner.ForStage`):

  | Level | VS stage | Chance | Cap | At once |
  |---|---|---|---|---|
  | L1 | Mad Forest | 10% | 50% | 10 |
  | L2 | Inlaid Library | 7.5% | 50% | 10 |
  | L3 | Dairy Plant | 20% | 60% | 10 |
  | L4 | Gallo Tower | 7.5% | 50% | 10 |
  | L5–L10 | Cappella Magna | 20% | 80% | 20 |

  Inlaid Library, Dairy Plant and Gallo Tower's fixed spawn spots are not modelled.
- **At the limit** (wiki): the attempt still rolls, at the base chance with no Luck. A success despawns the oldest lantern and places a new one at the off-screen spot nearest the player (`ReplaceOldest`). Before, spawning stopped at 10.
- `LightSource.AnyBroken` lets `AutoplayBot` log every broken lantern (`lantern` events). The bot now also walks to lanterns within 18 units and to items within 14 when fewer than 6 enemies are near, like a player. Before, it ignored them and broke only 2 lanterns in 5 minutes.

**The dying body** (the owner chose "Body briefly stays (VS-style)"; wiki enemy pages show death animations of about 1.8–2.5 s):
- `EnemyCorpses`: at death (`EnemyDrop`), a still copy of the enemy's drawn meshes stays where it died. Skinned meshes are baked in their last pose; the hit flash, a property block, is not copied.
- The copy holds, then sinks into the ground (1.1 × its height, eased) and shrinks to 0.6 over `LingerSeconds` 0.7, ending with the neck jet (last spurt at 0.5 s).
- The enemy itself still returns to the pool at once, so kills, spawning and pooling are unchanged.
- At most 32 corpses (8 meshes each); beyond that an enemy vanishes as before. Ghosts get a body too, but still no blood. Timings are provisional.

Validation:
- `LightSourceTests` (T): Luck weighting, per-level numbers and caps.
- `LightSourcePlayModeTests.AtTheLimit_ANewLanternReplacesTheOldest_NearestThePlayer` (R).
- `EnemyCorpsePlayModeTests` (R): the body copy has every drawn mesh, sinks and shrinks by 0.5 s, and is gone by 0.85 s while the enemy was pooled at once; 60 kills in one frame leave exactly 32 corpses, all gone after 0.9 s.
- EditMode 474/474, PlayMode 184/184, save untouched (`D:/SoulHunter-Validation/run-20260929-corpse-lantern/`).
- Heavy benchmark (release, level-60 hero), FPS by 20 s window:
  - run 2 (1,000–2,300 kills per window): 59–65;
  - run 1 (4,100–7,000 kills per window): 45–56.
  - The corpse update is not in the profile's top 60.
- Editor play:
  - `autoplay-editor-74` (Stage 1, 5 min, lantern-seeking bot): 7 lanterns broken (5 Gold Coins, 1 Rich Coin Bag, 1 chicken), 411 gold with 2 chests, 0 errors, min 60 FPS. **The bot died at 4:48** at level 14. Earlier bot runs survived 5 minutes when kills still dropped chickens.
  - `-73` (Stage 5, level-30 hero, god mode, 3 min): 16 lanterns (9 Gold Coins, 2 Coin Bags, 5 chickens), 262 gold.
  - The sinking body is hard to catch in 2 s screenshots; it is covered by the tests (partial `V`).
- Balance note for the owner: with VS drops, healing now comes from lanterns, stage items, Recovery and the maxed-level chicken card. Whether early Stage 1 is too hard is a balance decision.

Not yet played by the owner.

### Heal balance pass (owner: "heal balance pass karo", 2026-09-29)

Why: after the VS no-enemy-drops rule, a lantern-ignoring bot run had **no healing at all**: 19 hits of 5 over 5 minutes, Recovery 0, and it died at 4:48.

Checked against vampire.survivors.wiki (fetched 2026-09-29):
- **Damage matches VS.** Mad Forest's bat deals 5 and its skeleton and Mudman 10; Soul Hunter's L1 enemy deals 5 with 10 HP. Player Max Health is 100, and Recovery starts at 0 (Max Health and Recovery pages).
- The wiki does not give how often an enemy's damage lands; the Armor page says "multiple times per second". Soul Hunter's `TouchDamage` stays at 0.5 s per enemy.
- **What VS has and Soul Hunter lacked** (PowerUps page):
  - the **Max Health** PowerUp: +10% per rank, 3 ranks, multiplied (+33.1%), initial cost 200;
  - the **Recovery** PowerUp: +0.1 HP/s per rank, 5 ranks (+0.5), initial cost 200.
  - Soul Hunter's shop sold only Might, Armor, Greed and Revival.
- Recovery page: "Recovery also boosts healing from pickups, each 0.1 translates to an additive 10% boost".

Changes:
- `MetaPowerUps` (Foundation) holds the VS ranks, bonuses and prices. `GameData.MetaMaxHealthLevel` / `MetaRecoveryLevel` are clamped by `SaveService`'s repair; an older save loads them as 0.
- Superseded the next day by the full VS PowerUp shop (below); the heal effects stay the same.
- `PlayerController.ApplyHealingPowerUps`: max health × 1.1^rank after the character and Soul Eggs, with the run starting at full health, and Recovery +0.1 × rank via `PlayerStats.AddRegen` (`PlayerRecovery` heals each second).
- `HealthPickup.HealWithRecovery`: a floor chicken, and the maxed-level chicken card (`MaxedRewards`), heals 30 × (1 + Recovery).
- `CharacterStatSheet.Build` and the select screen include both PowerUps.
- `AutoplayBot -sh-autoplay-heal-powerups` plays with both at max rank. Otherwise the bot's throw-away save has none, so runs compare like with like.

Validation:
- `HealBalanceTests` (T): the multipliers, the Recovery cap, pickup healing (30 → 45 at Recovery 0.5) and the select-screen sheet (133 HP, 0.5 Recovery).
- `HealBalancePlayModeTests` (R):
  - a run with both at max starts Kael at 133/133 with 0.5 Recovery, regains health each second, and a chicken heals 45;
  - the MainMenu shop sells both only up to rank 3 and 5, charges only real purchases, and switches the maxed buttons off.
- EditMode 478/478, PlayMode 186/186, save untouched (`D:/SoulHunter-Validation/run-20260929-heal/`).
- Editor play, Stage 1, 6 min, no god mode, lantern-seeking bot:

  | Run | Healing PowerUps | Result | Hits taken | Lowest health |
  |---|---|---|---|---|
  | `-75` | no | survived | 0 | 100/100 |
  | `-77` | no | survived | 7 | 60/100 |
  | `-76` | rank 3 + 5 | survived | 17 | 110/133 |
  | `-78` | rank 3 + 5 | survived | 15 | 94/133 |

  - With the PowerUps the hero took more than twice the hits and never fell below 94/133. Lanterns broken per run: 8–25 (chickens 0–5).
  - Random loadouts make these runs rough; `-75` killed 24,991, most likely through Soul Toll wipes.
  - 0 errors in every run.

Not yet played by the owner.

### VS PowerUp shop (owner: "everwhere follow DOT VS ONLY", 2026-09-30)

Everything is from vampire.survivors.wiki/w/PowerUps and each PowerUp's own page (fetched 2026-09-30).

**The list.** `PowerUps` (Foundation, `Save/SaveData/PowerUps.cs`) holds 23 PowerUps in the wiki's order, each with its rank limit, bonus per rank and initial cost:

| PowerUp | Per rank | Ranks | Initial cost |
|---|---|---|---|
| Might | +5% | 5 | 200 |
| Armor | +1 | 3 | 600 |
| Max Health | ×1.1, multiplied | 3 | 200 |
| Recovery | +0.1 HP/s | 5 | 200 |
| Cooldown | −2.5% | 2 | 900 |
| Area | +5% | 2 | 300 |
| Speed | +10% projectile speed | 2 | 300 |
| Duration | +15% | 2 | 300 |
| Amount | +1 | 1 | 5,000 |
| Move Speed | +5% | 2 | 600 |
| Magnet | ×1.25, multiplied | 2 | 300 |
| Luck | +10% | 3 | 600 |
| Growth | +3% | 5 | 900 |
| Greed | +10% | 5 | 200 |
| Curse | +10% | 5 | 1,666 |
| Revival | +1 | 1 | 10,000 |
| Omni | +2% Might, projectile Speed, Duration and Area | 5 | 1,000 |
| Charm | +20 enemies per wave | 5 | 10,000 |
| Defang | 3% of enemies harmless | 5 | 10 |
| Reroll | +2 | 5 | 1,000 |
| Skip | +2 | 5 | 100 |
| Banish | +2 | 5 | 100 |
| Preserve | 10% chance to keep a Reroll/Skip/Banish | 5 | 500 |

**Price** (wiki Cost section): `InitialPrice` when nothing at all has been bought, else `InitialPrice × (1 + Bought) + floor(20 × 1.1^TotalBought)`. **Refund** returns every rank's price; the fees are additive since VS v0.7.2, so the order does not matter.

**Left out, and differences from VS:**
- The four Seals are left out: they are used in VS's Collection menu, which Soul Hunter does not have.
- ~~VS unlocks some ranks through achievements and items; every rank could be bought.~~ Locked since 2026-10-01; see "VS locked PowerUps" below.

**Save** (version 2):
- `GameData.PowerUpRanks` holds the ranks.
- `SaveService.Migrate` v1 → v2 refunds the old fixed-price shop once, as the owner chose ("Refund all"): Might 500, Armor 300, Greed 400 and Revival 1000 per level, and the 2026-09-29 Max Health / Recovery buttons 200. It then zeroes the old fields.
  - The owner's real save (Might level 9) gets 4,500 gold back on its next load. It had 21,271 gold on 2026-09-30.
- The repair keeps only known PowerUps, one entry each, within their VS ranks.

**Effects in a run:**
- `PlayerStats.ApplyPowerUps`, with Max Health in `PlayerController`. This replaces the old +10% Might / +20% Greed per level.
- Curse now also raises the wave's enemy minimum (Curse page: frequency, quantity, speed and health), and Charm adds its flat number after Curse (Charm page).
- **Reroll, Skip and Banish start at 0 as in VS**, +2 per rank plus Soul Eggs. Soul Hunter's old free 2/2/2 per run is gone; the scene's `_starting*` values are no longer read.
- **Skip gives experience**: 20% of the next level (the Skip page's tips; the page gives no formula).
- **Preserve** keeps a used charge at 10% per rank.
- **Defang** (`EnemySpawner.Defang`, `EnemyController.IsDefanged`):
  - applies only to ordinary wave spawns; chest elites, bosses and event enemies never (Defang page);
  - a defanged enemy deals no contact damage and runs no abilities, and is tinted green through `DamageFlash.SetTint`, which survives hit flashes;
  - the new VS Display option **Show Defanged Enemies** (on by default) turns the tint off.

**Shop screen** (`PowerUpMenuUI`):
- Built at runtime in `PowerUp_Panel` from a copy of an old button, so `MainMenu` is not edited; the old four-button grid is hidden.
- A 6 × 4 grid of dark cells (icon, name, rank; gold when selected, green when maxed), an info box (description, rank, VS price or MAX), **Buy** and **Refund PowerUps**.
- Icons are Soul Hunter's passive of the same stat, as VS PowerUps show their passive item; none for Greed, Omni, Charm, Defang, Reroll, Skip, Banish and Preserve.

`AutoplayBot`:
- `-sh-autoplay-heal-powerups` now sets Max Health 3 and Recovery 5 through `PowerUps`.
- `-sh-autoplay-shop-shot` opens the shop before the run, buys a few ranks and screenshots it.

Validation:
- `PowerUpShopTests` (T):
  - the wiki list;
  - the price formula, with the wiki's own example of five ranks of a 200-gold PowerUp: base costs 3,000 plus fees 22 + 24 + 26 + 29;
  - Refund returns exactly what was paid;
  - the owner-style v1 save (Might 9 → +4,500, Armor 1 → +300, Max Health 2 → +400) is refunded once;
  - the repair clamps.
- `HealBalanceTests` and `CharacterStatBlockTests` were moved to the catalog; `LevelUpActionTests` and `RunModeTests` grant charges explicitly.
- `PowerUpShopPlayModeTests` (R):
  - the MainMenu shop has a cell for every PowerUp, sells Recovery at 200, 422, 624, 826 and 1,029, stops at rank 5, shows the fee on the next PowerUp, and refunds everything;
  - a run with every PowerUp maxed gets each effect: Might +35% with Omni, Armor +3, Max Health ×1.331, Reroll/Skip/Banish 10 each, Charm 100, Defang 15%, Preserve 50%;
  - without PowerUps a run has no Reroll/Skip/Banish;
  - a defanged enemy deals no damage and is green;
  - Skip gives experience, and Preserve keeps some charges.
- EditMode 483/483, PlayMode 190/190, save untouched, both before and after the final UI fix (`D:/SoulHunter-Validation/run-20260930-powerups/`, `-powerups2/`).
- Editor play `autoplay-editor-80` (`-sh-autoplay-shop-shot`, 2 min): the shop screenshot shows all 23 cells readable, Recovery 5/5 green, Greed selected at price 242 (200 + fee after 8 ranks), 0 errors, min 84 FPS (partial `V`).

Not yet played by the owner.

### Level-up Reroll/Skip/Banish and VS locked PowerUps (owner, 2026-10-01)

**"level up panel ke neeche skip etc 3 button kaam nahi karte".** They worked; the owner's save had no Reroll/Skip/Banish PowerUp, so all three were at 0 and disabled. vampire.survivors.wiki/w/Level_up: they "become available in this screen if the player invests in these PowerUps and/or uses a character that may start with these"; the Reroll, Skip and Banish pages: they start at 0, +2 per rank.
- `LevelUpUI.RefreshActionButtons` shows each button only while it has a use left (Banish also while armed) and hides the row when all three are gone; the cards take the space.
- `LevelUpActionButtonsPlayModeTests` (R): hidden at 0; with 1 use each, a click through the UI raycast rerolls, banishes the clicked card and skips with experience. It passes with the owner's scene and HUD prefab too.

**VS locked PowerUps ("adopt vs rule").** Each PowerUp's wiki page (fetched 2026-10-01): Reroll ranks unlock by reaching level 80 with five characters; Skip ranks by surviving 15 or 30 minutes on five stages; Banish ranks by 60-100 Collection entries; Omni by maxing Torrona's Box; Charm, Defang and Preserve by relics. The other 16 PowerUps are open from the start. The owner chose Soul Hunter conditions (`PowerUpUnlocks`, Save/SaveData/PowerUps.cs):
- Reroll: level 80 with Corvan, Aria, Draven, Gideon and Kaelen (one rank each).
- Skip: survive 15 min in Level 6, 30 in Level 7, 30 in Level 8, 15 in Level 9, 15 in Level 10 (one rank each; a stage cleared at 30:00 counts as 30 minutes).
- Banish, Omni, Charm, Defang and Preserve stay locked until Soul Hunter's Collection, relics and item exist (Phases 2-3).
- Unlocks are ids in `GameData.UnlockedAchievements`. `CharacterUnlockTracker` records them during a run (every stage second and on a stage clear), saves, and shows "PowerUp unlocked: Skip rank 1" (held while a menu pauses).
- The shop shows a locked PowerUp as LOCKED with its next condition, and Buy only sells unlocked ranks (`PowerUps.CanBuyNext`).
- A save that owns ranks now locked gets the VS Refund once, in the save repair (`PowerUpUnlocks.RefundIfLockedRanksOwned`). The owner's save owned none.
- `PowerUpUnlocks.UnlockEverything` is for tests and debug tools.
- Tests: `PowerUpUnlockTests` (T) and `PowerUpUnlockPlayModeTests` (R: shop LOCKED with its condition, no sale, one rank sold once unlocked; 15:00 in Level 6 unlocks Skip rank 1 with a notice).
- Validation: EditMode 496/496; PlayMode 224/225, the one failure being `UnlockNoticePlayModeTests` calling the old notice signature, fixed and rerun with the related fixtures 31/31; save untouched (`D:/SoulHunter-Validation/run-20261001-unlocks/`, `-unlocks2/`).
- Not yet played by the owner.

### Game "hangs" at the first level-up (owner: "kabhi bhi hang ho ja raha hai", 2026-09-30)

The owner's Editor log showed both runs stopping right after the first level-up paused the game. Cause: the owner's unsaved `Main_Gameplay` saves `LevelUp_Panel` inactive. The panel carries its own `LevelUpUI`, whose `Awake` hides the panel; with the panel inactive that `Awake` first ran when the first level-up switched it on (from the `LevelUpUI` on `UI_Bindings`), so it hid the panel again. The game stayed paused with no cards: a "hang". Later level-ups worked. The committed scene saves the panel active, so tests and bot runs never saw it.
- Fix: `LevelUpUI.Awake` does not hide the panel while a level-up round is open (`LevelUpManager.IsLevelUpActive`).
- `LevelUpPanelStartPlayModeTests` (R): a panel copy that has never woken is shown on the first level-up. It fails on the old code.
- `AutoplayBot` logs `hitch` (frames over 0.25 s, not screenshot frames) and `stuck-pause` (paused over 20 s, with `GameTime.PauserNames()`) events, and with `-sh-autoplay-levelup-ui` a `levelup-ui` event (panel and card screen rects).
- EditMode 491/491, PlayMode 222/222, save untouched (`D:/SoulHunter-Validation/run-20260930-hang/`).
- Editor play with the owner's scene and HUD prefab: before the fix (`hang-03`) the first level-up panel was inactive; after it (`hang-04`, 5 min, Draven) all 19 level-ups showed their panel, 0 hitches, 0 errors, min 61 FPS. Bot runs on the committed scene (`hang-01`, 8 min) had no hitch or stuck pause either.
- Seen, not changed: with the owner's HUD layout the level-up stats panel has room for only 2 of its rows (8 of 19 stats show).

### Level 1 Bug MIS fixed (owner: "sab bug fix karo safely", 2026-09-30)

Owner's CTO brief: play Level 1 from the Play button to 30:00, only note bugs, show them, fix after approval. The register is `Docs/BUG_MIS_Level1.md` (runs `D:/SoulHunter-Validation/cto-level1-runA`, `-runC`; 17 bugs, 7 observations). All 17 are fixed:
- **B01 (waves):** every minute table covers 30 minutes (it covered 0-24, and minutes 25-29 reused minute 24; on Stage 1 that was a quiet minute, so the last six minutes held ~84 enemies). Stage 1 now plays the VS Mad Forest table exactly, from vampire.survivors.wiki/w/Mad_Forest "Waves" (`WaveTables.MadForestMinimum/Interval`; no elite bonus, which is not a VS field). Stages 2-10 keep their own curve for 0-24 and hold its peak for 25-29. The 10 wave assets were rebuilt.
- **B02 (white silhouettes):** the hit flash is 1.8× (was 3×, which saturated models to flat white), runs on real time (it froze white during pauses), and waits 0.15 s (`DamageFlash.FlashGap`) before flashing again, so an enemy under constant hits blinks.
- **B03:** `WeaponEvolutionData.DisplayName` shows evolution IDs as names on the chest ("Evo_BloodyTear" → "Bloody Tear"). The names themselves are still VS names (no-copy rule, Phase 2).
- **B04 (VS):** chests draw only owned items (vampire.survivors.wiki/w/Treasure_Chest: "a random weapon or passive item the player currently has"); nothing owned left pays the 25-gold coin bag.
- **B05:** the unlock notice waits hidden while a menu pauses the game.
- **B06, B14:** HUD labels get a dark backing sized to their text (`HudBacking`) and stay on one line.
- **B07:** damage numbers are drawn 4 units toward the orthographic camera (same screen spot), so models no longer cut them.
- **B08:** the HUD holds back the chest's gold until the count-up reaches it (`HudGoldHold`).
- **B09:** the level-up stats panel starts at 23% of the width (the HUD item slots end near 21%) and is only as tall as its rows. The font is sized from the free band; sizing it from the shrinking panel shrank the text every frame (caught in editor play, fixed before commit).
- **B10:** Gold Pouch uses `SH10_Icon_GoldPouch.png` (the approved gold Soul Coin rendered in Blender, transparent); Roast Chicken uses the project's `Floor_Chicken` sprite (`GameContentCatalog.GoldPouchIcon/RoastChickenIcon`); the chest's coin-bag card uses the gold icon too.
- **B11:** the chest preview's hinge seam used every part named "Lid*", including the wide LidBand rings, so most of the body swung up with the lid. Only the lid shell counts now; the body stays and the soul seam glows inside.
- **B12:** level-up card texts get a size before auto-size (TMP defaults up to 72 replaced the limits on the first screen).
- **B13, B16, B17:** `OptionsMenuUI.ApplyFocusColors` changes focus at once (no brown fade midpoint), sets a dark disabled colour (Reroll/Skip/Banish), and the main menu buttons use it (orange focus, not blue).
- **B15:** invisible card-sized spacers keep the 3-card layout when fewer cards are offered.
- Also: `LevelUpUI` no longer writes ~10 `Debug.Log` lines per level-up (costly in the editor; observation O2).
- Tests: `WaveTableTests`, `WaveTablePlayModeTests`, `DamageFlashPlayModeTests`, `ChestNameTests`, `LevelUpActionTests`, `ChestRewardTests` (now owns the item first), `UnlockNoticePlayModeTests`, `HudFixesPlayModeTests`, `ChestFlowPlayModeTests`, `StatsPanelPlayModeTests`, `RewardIconTests`, `ChestPreviewPlayModeTests`, `LevelUpScreenPlayModeTests`.
- The stats-panel font loop only shows at 1080p (in the test runner's small window the font is limited by width), so it was checked in editor play (`autoplay-editor` run `cto-level1-verify2`).
- Editor play `cto-level1-verify` (8 min from the menu): 0 errors, min 57 FPS; the screenshots show the orange menu focus, the chest opening with only its lid, owned-item chest rewards, HUD backings and one-line kills. `cto-level1-verify2` (3 min): the level-up panel readable and clear of the HUD row.
- Not yet played by the owner.

### Character select screen repaired (owner: "play button click karne ke baad aane bale panel to tumne bekar bana diya", 2026-09-30)

The owner only started seeing this screen again once Play booted through the menu (`PlayFromBootstrap`). Three causes, all older than that change:
- **Constant Pixel Size canvas.** The main menu's `CanvasScaler` used Constant Pixel Size, so the fixed 190×132-pixel cards (4 columns) overflowed the grid in any window smaller than 1920×1080. `MainMenuNavigation.ScaleWithScreen` now sets Scale With Screen Size, 1920×1080, match 0.5 (like the gameplay HUD) when the menu loads; 1080p is unchanged. The scene file is not edited.
- **The mode row (2026-09-27) pushed the grid down,** and the last row (Valerius) hid behind the info box. The grid now has 5 columns, and `GridFit` sizes the cards from the grid box, so all 13 characters show without scrolling.
- **TextMeshPro defaults replaced the text limits.** The panel is built while inactive, so each created text ran TMP's `Awake` later and took its defaults (size 36, auto-size up to 72). Gold, FILTER, Whip and the LOCKED badges (which wrapped to "LOCKE / D") rendered at 53-68. `AutoSize` now sets a size first, so the limits hold (Gold 32, FILTER 20, Whip 18). The six mode labels share one size (`SameFontSize`), so "Limit Break" fits on one line.
- `AutoplayBot -sh-autoplay-select-shot` screenshots the main menu and this screen, and logs the canvas scale and the key text sizes.
- `CharacterSelectionPlayModeTests.SelectScreen_FitsTheWindow_EveryCardVisible_NothingOverlaps` (R, in the test runner's non-1080p window): the menu scales with the screen; every card is fully inside the grid; the grid is clear of the mode row and the info box; the mode labels share one size; Gold/FILTER/Whip stay within their limits; each LOCKED badge is one line. It failed on the old code.
- EditMode 481/481, PlayMode 215/215, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20260930-charselect2/`).
- Editor play `autoplay-editor-91`: screenshots of the main menu and the character select screen, 0 errors (partial `V`). The portrait in the info box is still very dark (the hero render; Art section).

### Stats panel in pause and level-up (owner: "stats panel bana do", 2026-09-30)

vampire.survivors.wiki/w/Player_stats: stats "can be seen before the player starts the game in the Character Selection menu, in-game when paused, or when leveling up". Soul Hunter showed them only on character select, so Hollow Heart's new Max Health was invisible.
- `StatsPanel` (UI/HUD) lists the live stats in the wiki's order: Max Health, Recovery, Armor, Move Speed, Might, Area, Speed, Duration, Amount, Cooldown, Luck, Growth, Greed, Curse, Magnet, Revival, Reroll, Skip, Banish (`CharacterStatSheet.Live`, `InGameRows`). It uses the character select formats (absolute Max Health, +N, ±%, "-" when unchanged), with changed values in gold.
- **Pause menu** (`PauseMenuUI`): a single column on the right, under the timer. On the left the HUD's kills, gold and item rows drew over it (seen in `autoplay-editor-84`).
- **Level-up screen** (`LevelUpUI`): a 4-column grid that fits itself each frame between the "LEVEL UP!" text (its drawn bounds; the heading's rect reaches down to the cards) and the top card, at any window size. `ignoreLayout` keeps the card layout group from moving it.
- Each column has a label text and a value text, sized together to fit (a long label ran into its value in `-86`). It refreshes every 0.25 s while shown, so several level-ups in a row update it.
- The look is Soul Hunter's own. The wiki does not describe the layout.
- Tests: `StatsPanelTests` (T: order and formats) and `StatsPanelPlayModeTests` (R: the pause panel shows the live values and is clear of the buttons, and hides on resume; on the level-up screen, Hollow Heart picked while it is open shows 100 → 120 in gold while current health stays, and the panel is clear of the cards and below the heading text).
- `AutoplayBot` options `-sh-autoplay-pause-shot` (pause at 20 s, screenshot, resume) and `-sh-autoplay-levelup-shots` (every level-up screen).
- EditMode 481/481, PlayMode 214/214, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20260930-stats2/`).
- Editor play `autoplay-editor-87` (Stage 1, 1 min): both screens readable, 0 errors, min 87 FPS (partial `V`).
- Not yet played by the owner.

### Owner reports: Play did not boot; Max Health "does nothing" (2026-09-30)

**Play started Level 1 directly** (owner: "game play button click karte hi direct level 1 start ho raha hai, boot kyun nahi ho raha"). Unity's Play button starts the open scene, and the owner's editor had `Main_Gameplay` open. The project never had a start-scene hook (the only earlier one was a loose tool script removed as clutter).
- `PlayFromBootstrap` (Editor, `[InitializeOnLoad]`): while **Soul Hunter/Play From Bootstrap** is on (the default, stored in EditorPrefs), pressing Play on any game scene (one enabled in the build settings) starts from `Bootstrap`, as the build does. The test runner's own scene and batch mode are left alone. Switch the menu item off to play a single scene directly.
- CLI check (`-executeMethod SoulHunter.Editor.Autoplay.PlayFromBootstrap.CheckFromCommandLine -sh-bootcheck <file>`, GUI editor on the isolated copy): with `Main_Gameplay` open, Play loaded Loading at 0.2 s and MainMenu at 2.0 s (`D:/SoulHunter-Validation/run-20260930-boot/bootcheck.txt`).

**Max Health "does not increase"** (owner). Checked, and it works as VS says. vampire.survivors.wiki/w/Hollow_Heart: it "multiplies the current value of Max Health by 1.2" and "does not heal to fill up the newly increased Max Health". The HP bar shows current ÷ max, so after Hollow Heart at full health it looks emptier (100/120), and no number shows the new maximum. `PassivePlayModeTests` (R) confirms ×1.2 on the real player; `HealBalancePlayModeTests` (R) confirms the Max Health PowerUp (133/133 at rank 3). VS shows the stats in the pause and level-up screens; Soul Hunter does not yet (VS DOT table, Phase 3 "Stats panel").

**Found while re-testing the Duplicator work:**
- The Lightning Ring skipped a strike whose target died or was pooled in the 0.05 s gap. It now passes over dead targets to the next nearest one, so every strike lands while enemies remain.
- `DuplicatorPlayModeTests` measured the Magic Wand wrongly: the wand runs its own volley cycle in `Update`, so a volley did not always fall inside the test's window. The test now starts the wand's volley through its cooldown. The dummy targets keep their controllers on, because the Lightning Ring and Fire Wand target only live enemies. The fixture then passed 3 runs out of 3.
- Validation: EditMode 480/480, PlayMode 212/212, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20260930-boot2/`).
- Not yet played by the owner.

### Owner play reports: Duplicator, low-health tint, obstacles (2026-09-30)

The owner played Phase 0 and reported three things.

**1. "Duplicator kaam nahi kar raha".** The +1 Amount per level (vampire.survivors.wiki/w/Duplicator: 2 levels, +1 each) reached the weapons, but most weapons fired their extra shots in the same frame from the same spot. Axe (same line), Cross (a ±15° fan), Gun (±5° wobble) and the Whip's third lash (the same side as the first) landed on top of each other, so the Duplicator looked like it did nothing.
- VS rule, from each weapon's wiki infobox "interval": the Amount of projectiles leave one after another. `AutoAttackWeapon.FireInSuccession(count, interval, fireOne)` fires shot 0 at once and the rest on game time (a pause holds them; they stop if the weapon is switched off; Edit Mode fires them all at once).
- Intervals: Whip 0.1, Knife 0.1, Cross 0.1, Axe 0.2, Runetracer 0.2, Gun (Phiera) 0.2 per volley of four, Santa Water 0.3, Cherry Bomb 0.3, Lightning Ring 0.05, Fire Wand 0.02, Peachone 0.025 (it was 0.1). The Magic Wand already used 0.1. Each is `VsInterval` in its script.
- The Bone page gives no interval, so the Bone keeps firing together with its 15° spread (wiki silent).
- The Cross no longer fans: VS fires every cross at the closest enemy, 0.1 s apart.
- Lightning Ring strikes skip a target that died or was pooled in the 0.05 s between strikes.
- `DuplicatorPlayModeTests` (R, 14 weapons): Duplicator level 2 gives +2 Amount, each weapon fires 2 more, and no two shots appear together at one spot heading the same way. Before the fix, Axe, Cross, Runetracer, Bone and Gun failed. (Runetracer and Bone were a measuring error: they fly by velocity, which the test now reads.) The King Bible is checked by its orbiting books (+2).

**2. Low-health red screen removed** (owner: "player ka life kam hone per red flash pure screen pe aata hai, use hata do"). `LowHealthWarning` pulsed a red tint over the whole screen at 30% health and below; the wiki does not describe any such effect in VS. It is deleted, with its wiring in `GameHUDController` and its EditMode cases. Hit feedback on the hero's model (`DamageFlash`) stays. `LowHealthPlayModeTests` (R): at 10% health no overlay and no red full-screen image.

**3. 70% of the ground obstacles removed** (owner: "ground ka obstacle 70% hata do"). `build_grounds.py` keeps `OBSTACLE_KEEP` = 30% of the obstacles it placed on each floor (at least 1; a fixed per-level seed picks which; `thin_obstacles`). All 10 floors were rebuilt in Blender 5.2.1 against the isolated copy (`SOULHUNTER_ROOT`) and the FBX files copied back; prefabs, colliders and materials are unchanged.

| Level | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
|---|---|---|---|---|---|---|---|---|---|---|
| Obstacles before | 15 | 13 | 9 | 14 | 11 | 13 | 7 | 10 | 15 | 16 |
| Obstacles now | 4 | 4 | 3 | 4 | 3 | 4 | 2 | 3 | 4 | 5 |
| Reachable ground | 92% | 94% | 93% | 94% | 95% | 93% | 93% | 95% | 95% | 93% |

`StageFloorTests` now expects 1-5 blocker boxes per floor (it required at least 6). `FloorWalkabilityTests` (T) passes with no trapped pockets.

Validation: EditMode 480/480 (5 low-health cases removed), PlayMode 212/212, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20260930-ownerplay/`). Editor play `autoplay-editor-83` (Stage 1, 3 min, no god mode): 0 errors, min 77 FPS; the screenshots show one crypt and three gravestones per tile, and the Whip's two lashes as two arcs. The bot again left gems uncollected (131; `-78` left 383), which is bot behaviour.

Not yet played by the owner.

### Phase 0 of the VS DOT plan (owner decisions, 2026-09-30)

Built from the owner's answers (`Docs/VS_DOT_Plan.md`, Phase 0):
- **Dash:** it only moves and avoids damage, as the GDD says ("dash through enemies and avoid damage"); the flat 50 damage is gone.
  - **Defect found by the new test:** the dash moved `transform.position` while the player's Rigidbody interpolates, so Unity overwrote most of it: it covered about 0.7 of its 3 units. It now moves the Rigidbody.
  - The kinematic dash also went through stage obstacles and the Castle corridor walls. It now stops at the first `Obstacle`-layer collider (sphere cast, the capsule's 0.4 radius).
  - `DashPlayModeTests` (R): the dash covers its distance, an enemy in its path takes no damage, the hero is invulnerable only while dashing, and a wall 1.5 ahead stops it.
- **Invented unions removed:** the six `Union_*.asset` files are deleted. The union system stays for VS's own unions (Phase 2). `Main_Gameplay`'s `_availableUnions` list still holds six missing references, which `WeaponManager` skips; the owner's editor had the scene open with unsaved changes, so the scene file was not touched. `UnionPlayModeTests` (R): no union is live.
- **Gem colours:** vampire.survivors.wiki/w/Experience_Gem: blue "up to 2 XP", green "up to 9", red beyond. Scaled ×10: under 21, under 91, above (`XP_Gem.prefab`, `TieredVisual`, `PickupV2Importer.GemThresholds`). `PickupV2Tests` (T): the tiers and the prefab thresholds.
- **Stage-clear revival gold:** The_Reaper and Revival pages: +100, +200, +300, +400 per unused revival, then +100 each ("3 revives is 500 + 100 + 200 + 300 = 1,100"). The Stages page says a flat 100; the owner chose the stacking pages. `LevelProgressionManager.StageClearGold`; `BossStageAdvanceTests` (T).
- **Shadow Kael's mirrored Clock Lancet** (owner exception, VS has no player freeze): a beam that crosses the player makes it move at half speed for the weapon's freeze time (2 s at level 1, × the boss's mirrored Duration) (`PlayerController.Slow`, `IsSlowed`). The boss and its horde are still never frozen.
- **Shadow Kael's mirrored Pentagram** (owner exception using VS Pentagram's own item erase, vampire.survivors.wiki/w/Pentagram: "erasing every enemy, their drops, and all items on the screen, except for stage items"): it erases every gem, coin, bag, chicken, magnet, freeze and chest on screen, but not stage items and no enemy. The VS keep roll applies, with the boss's mirrored Luck: x ~ U[0,1] rounded to 2 decimals, and items go when x / Luck > the keep threshold (10% at level 1; 25/45/65% at levels 3/5/7) (`GroundItemEraser`, `StageItemMark`).
  - The player's own Pentagram does not erase ground items yet; that is a Phase 1 row, and it will use the same `GroundItemEraser`.
- Tests: `ShadowKaelPlayModeTests.MirroredClockLancet_SlowsThePlayerToHalfSpeed_ForItsFreezeTime` and `MirroredPentagram_ErasesItemsOnScreen_ButNotStageItems_NorTheHorde` (R); `PentagramItemRollTests` (T, the wiki's own worked examples).
- Validation: EditMode 485/485, PlayMode 197/197, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20260930-phase0b/`). The first full run failed the dash test (the interpolation defect above); the Pentagram test first failed because the boss's own first shot had already erased the items, so the test now stops the boss weapon's timer.
- Editor play `autoplay-editor-82` (Stage 10, level-60 hero, god mode, boss at 40 s, 2.5 min): Shadow Kael copied Whip, Runetracer, Knife, Bible, Magic Wand and a level-6 Pentagram; 0 errors, min 14 FPS in the GUI editor at 300 enemies (level-60 kill rate; known performance note).
- Seen, not changed (Art section): Stage 10's paths look flat bright cyan in that run.
- Not yet played by the owner.

### Ground late to appear; level-up pause (owner reports, 2026-09-30)

**"ground late se spawn ho raha hai".** `InfiniteMap` kept a 3 × 3 grid of 20-unit tiles, which reaches only 20 units past the hero near a tile edge. The 55-degree camera shows about 21 units to each side at 16:9 (28 at 21:9), so a screen edge showed bare background until the far tile wrapped over. The grid is now 5 × 5 (`InfiniteMap.GridSize`), reaching at least 40 units every way; a tile more than half the grid away wraps to the other side.
- `GroundCoveragePlayModeTests` (R): the hero steps to tile edges in every direction, and every screen corner and edge midpoint must fall on a tile, with the camera at 21:9 (a batchmode window is narrow). It fails on the old 3 × 3 grid ("Screen point (0,0) ... shows no floor") and passes on 5 × 5.
- Editor play `autoplay-editor-81` (Stage 1, 30 s, `-sh-autoplay-early-shots`: a shot every 0.2 s for the first 3 s): floor to every screen edge from the first frame, 0 errors, min 60 FPS (partial `V`).

**Level-up pause** (owner: the game kept running behind the level-up screen). vampire.survivors.wiki/w/Level_up (checked 2026-09-30): "Upon leveling up, the game is paused ..." and "After selecting an item, the game resumes and the character gains a brief moment of invulnerability" (Patch 0.2.12). The wiki gives no length.
- `LevelUpPausePlayModeTests` (R): with a real level-up open for 1.5 s, the time scale stays 0; the stage clock, enemy count, enemies, gems, the player, projectiles, every world transform, animator and game-time particle stay still. The test passed on the existing code, so the pause itself was not reproduced as broken; what the owner saw is still unexplained and needs the owner to show it (only the chest and level-up effects run on unscaled time, by design).
- Added: after the pick, the hero is invulnerable for its hit invulnerability (0.5 s fallback; Soul Hunter's number) (`LevelUpManager.ResumeGameplay`, `HealthController.GrantInvulnerability`).

Validation: EditMode 483/483, PlayMode 192/192, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20260930-ground-levelup2/`).

Not yet played by the owner.

### VFX v2 sprites and VS-style level-up cards (owner-approved, 2026-09-26)

The owner approved the VFX v2 concept sheet: eight Soul Hunter sprites made procedurally by `ArtSource/Generators/build_vfx_textures.py` (soul flame, slash arc, magic circle, impact star, soul wisp, lightning bolt, shockwave ring, sparkle), written to `Assets/Art/Textures/VFX/SH10_Vfx2_*.png`. `WeaponVfxBuilder` now uses them:
- sparkles and shock rings everywhere;
- a spinning magic circle on level-up, on Pentagram and on the Holy Water pool rim;
- an impact star for hits and lightning, and soul wisps rising from dead enemies;
- soul flames for fireballs and explosions (explosions also get a shockwave).

The Whip had no visual at all; each lash now plays a slash arc along the attack and scaled to its area (`VfxKind.Slash`, `VfxQuadFade`, `VFXPoolManager.Play(kind, position, rotation, scale)`). Shadow Kael's mirrored Whip shows it too.

The owner also asked for a VS-style level-up screen, found while playing in the editor. Cards were white buttons with internal names ("SongOfMana Lv.2 (Lv 2)") and placeholder descriptions. `LevelUpUI.StyleCard` now shows the icon, a readable name (`UpgradeText.DisplayName`), a NEW!/Level N tag and a description. Weapons that are new describe what they do (Soul Hunter wording from each weapon's code); passives show their real per-level effect from `WeaponManager.ApplyUpgrade`. Descriptions written in the asset are kept. Old TMP and legacy `Text` labels are hidden, the cards use the focus colours, and Reroll/Skip/Banish are dark with white text.

The HP bar strip was left unchanged in that change. The owner later asked for the HP bar under the player (2026-09-26); see below.

Validated by `WeaponVfxTests` (T, Slash included) and `WeaponSweepPlayModeTests` (R: the Whip shows its slash; the level-up cards show an icon, a name without "Lv", a NEW!/Level tag, a real description and no stray labels). Both suites pass: EditMode 344/344, PlayMode 64/64, save unchanged (`D:\SoulHunter-Validation\run-20260925-vfx2\`). Editor play runs `autoplay-editor-02` and `-03` had 0 errors, and their screenshots were checked (partial `V`).

### HP bar under the player (owner request, 2026-09-26)

The player's health bar was a red strip across the whole bottom of the screen. It is now a small VS-style bar (96x11 canvas units, dark background, red fill) just under the hero's feet. `HealthBarFollow` (attached by `GameHUDController`) reuses the same HUD slider, reparents it to the root canvas (drawn under the menus) and moves it every `LateUpdate`, after `CameraFollow`. It is not interactable and hides its handle. Validated by `PlayerPresentationPlayModeTests.HealthBar_IsSmall_AndFollowsUnderTheHero` (R: narrower than 15% of the screen, centred on the feet and just below them while walking, full at start). Both suites pass: EditMode 344/344, PlayMode 65/65, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-hpbar\`). Editor play runs `autoplay-editor-04` and `-05` had 0 errors (`-05`: 3 min as Kael, level 14, 750 kills, min 65 FPS), and their screenshots show the bar under the hero (partial `V`).

Seen in `autoplay-editor-05` and fixed (owner approved, 2026-09-26):
- Eight `Assets/Art/UI` icon sprites had their file name drawn under the picture ("VFX_Slash.png", "Bone.png", "Crosshair.png", "Lamp_Post.png", "VFX_Portal.png", "VFX_Heal.png" and two small marks): sprite_40, 48, 88, 93, 77, 44, 35 and 102, used by Whip, Bone, Cherry Bomb, Gun, Candelabrador, Attractorb, Tiragisu and Skull O'Maniac. Only those label rows were cleared; the picture pixels are unchanged. `CharacterStatBlockTests.NoUpgradeIcon_ShowsAFileNameLabel` (T) failed on the old icons and passes now.
- Reroll/Skip/Banish text was red: their label is a legacy `Text` copied from the card, and only TMP labels were turned white. `LevelUpUI.WhitenLabels` now turns both white.
- The level-up panel's "LEVEL UP" text was drawn over the HUD stage name. `LevelUpUI.StyleHeading` shows a gold "LEVEL UP!" lower down, above the cards.

`WeaponSweepPlayModeTests` (R) checks the white labels, and that the heading is above the cards and below the stage name. EditMode 345/345, PlayMode 65/65, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-uifix\`). Editor play `autoplay-editor-06` had 0 errors, and its level-up screenshot shows all three fixes (partial `V`).

### Validation run 2026-09-24

Commits `0beae7f`..`6f3f9c2` plus the pickup-pool fix were validated in an isolated workspace (`D:\SoulHunter-Validation\editmode-20260924-134853\`, which keeps the results XML and logs), with `Library` copied from the canonical project:

- `C` compile: 0 errors (the pixel-perfect package did not fail with the copied `Library`);
- `T` EditMode: 182/182 passed;
- `R` PlayMode: 5/5 passed (bootstrap, chest flow, game over/restart, time freeze, Wandering Merchant), 0 exceptions logged.

The first PlayMode run crashed Unity: the gold coin pool's prewarm loop never filled because copies of its inactive template never fire `OnDisable`. Fixed in `PickupPoolManager.PrewarmPool` and covered by `PickupPoolPrewarmTests`.

Still `UNKNOWN`: manual play of the new systems (level-up screen layout, elites, gold, evolutions, boss-advance), audio, `V`, `P` and `B`.

---

## 8. Unity Validation Status

Current project editor:

`D:\6000.0.36f1\Editor\Unity.exe`

An isolated fresh import was performed from an external source snapshot captured 2026-09-11 (that snapshot is no longer available).

Observed result:

- asset import completed;
- Unity batch process ultimately exited successfully;
- compilation emitted errors in `com.unity.2d.pixel-perfect` 5.0.3 Editor converter code referencing the old `UnityEngine.Experimental.Rendering.Universal.PixelPerfectCamera` namespace.

This matches Unity issue `UUM-77628`, fixed in Unity `6000.0.43f1`.

Therefore:

- do not automatically classify these package errors as a Soul Hunter gameplay-code defect;
- Unity `6000.0.36f1` remains the current project editor and may continue to be used;
- a clean package/editor compile baseline remains unresolved;
- do not force an editor upgrade merely to begin normal task work unless the task actually depends on closing this validation gate.

---

## 9. Requirement / Acceptance Rules

Evidence classes:

- `S` — static/source/serialized
- `C` — import/compile
- `T` — deterministic test/validator
- `R` — runtime/PlayMode
- `V` — visual/UX
- `P` — performance/profile
- `B` — player/build/release

Rules:

- static existence != runtime `PASS`;
- agent claim != proof;
- `UNKNOWN` != `PASS`;
- missing mandatory evidence remains `UNKNOWN`, `BLOCKED`, or `CONDITIONAL`;
- compile/test failure is `FAIL` for that acceptance path;
- known defects remain `FAIL` until corrected and revalidated;
- do not convert warnings/success logs into acceptance without required evidence;
- every meaningful accepted change should be traceable from requirement -> implementation -> evidence -> checkpoint.

The broad Requirement -> Acceptance matrix exists, but final row-by-row reconciliation of every authoritative product requirement is still incomplete. This does not require a full re-audit before ordinary development tasks.

---

## 10. Development Workflow

For a normal requested task:

1. Read this `AGENTS.md`.
2. Inspect only the files/systems relevant to the task.
3. Confirm the requirement and acceptance criteria.
4. Check for direct conflicts with approved decisions.
5. Choose the correct agent/tool.
6. Prefer an isolated workspace for risky or broad mutations.
7. Implement the smallest coherent change.
8. Validate at the evidence levels actually required by the change.
9. Report failures/unknowns truthfully.
10. Record evidence and checkpoint only after acceptance.

Do **not** perform a full-project audit before every feature, bug fix, UI change, asset task, or refactor.

Escalate to the owner only when:

- requirements genuinely conflict;
- a destructive action is necessary;
- permissions must expand;
- a product/design choice has no authoritative answer;
- acceptance cannot be reached without an owner decision.

### Bug fixes must use the Unity CLI (owner rule, 2026-09-24)

Every Soul Hunter bug fix is reproduced and verified with the Unity command-line test runner. Reading code or compiling outside Unity (`dotnet build`) is not enough: several bugs (the pickup-pool startup hang, the Whip's empty enemy mask, the Bible `InvalidCastException`, weapon self-damage) only showed up in real Unity runs.

For each bug fix:

1. Add or extend an EditMode or PlayMode test that reproduces the bug.
2. Run it against an isolated copy of the project, never the canonical path (the owner's editor keeps it open; never delete its lock or close it):
   - copy `Assets`, `Packages`, `ProjectSettings` and `Library` to a folder under `D:\SoulHunter-Validation\` (copying `Library` avoids the pixel-perfect package compile failure seen on fresh imports), then sync changed files into it;
   - `D:\6000.0.36f1\Editor\Unity.exe -batchmode -nographics -projectPath <copy> -runTests -testPlatform EditMode -testResults <run>\editmode-results.xml -logFile <run>\editor-edit.log`
   - `D:\6000.0.36f1\Editor\Unity.exe -batchmode -projectPath <copy> -runTests -testPlatform PlayMode -testResults <run>\playmode-results.xml -logFile <run>\editor-play.log`
3. Always run both suites: scene-startup bugs are invisible to EditMode.
4. Report the pass counts, compile errors and exceptions logged; commit only when both suites pass.

PlayMode tests boot the real game and share the canonical save file (`AppData/LocalLow/Nimrita Games/Soul Hunter/soulhunter_save.json`); tests must not write it deliberately.

### DOT

DOT means **Detail-Oriented Total Implementation**.

For relevant work consider:

- properties/data
- behavior
- state/timing
- inputs/outputs
- dependencies
- interactions
- edge cases
- errors
- UI
- audio
- VFX/feedback
- persistence
- compatibility
- integration
- performance
- regression
- documentation

Irrelevant dimensions may be explicitly `NOT_APPLICABLE`.

Code complete does not automatically mean DOT complete.

---

## 11. Asset / Art Workflow

Installed/useful local capability already confirmed:

- Blender 5.2.1

Web/design sources such as Dribbble, ArtStation, Behance, Pinterest, Figma, Canva, Sketchfab, Fab, Unity Asset Store, etc. are references or asset sources depending on their licenses.

Do not copy protected artwork merely because it is visible online.

Before adding a new external asset to the project:

- verify license / commercial-use rights;
- preserve attribution requirements if any;
- prefer source files suitable for the release pipeline;
- validate import cost, materials, shader compatibility, LOD/polycount where relevant;
- keep Soul Hunter's own art direction rather than copying another title.

Install additional tools only when they materially improve the active task.

---

## 12. Current Operating Direction

The expensive full static audit has already been completed.

Do not repeat it unless the owner explicitly requests a new full audit or the project state changes enough to invalidate the current verified baseline.

The only active plan is `Docs/VS_DOT_Plan.md` (owner, 2026-09-30: every older pending plan was removed). Work follows it, one row at a time.

---

## 13. Non-Negotiable Truth Rule

Optimize for truth, not for a positive verdict.

A truthful `FAIL`, `UNKNOWN`, or `BLOCKED` is better than a false `PASS`.

Do not claim a feature, build, performance target, tool, or release state is complete unless the required evidence exists.
