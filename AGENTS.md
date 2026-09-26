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

Owner decision pending: the player has no frozen state, and "wiping" the player is not a VS mechanic. So Shadow Kael's Clock Lancet only shows its beam, and his Pentagram only flashes.

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
- The boss died in about 5 s at player level 6, because its health is 50 x player level (provisional balance).
- A level-1 hero dropped into Stage 10 dies in about 11 s.
- The GUI editor ran at 17-38 FPS with 300 Stage 10 enemies before the boss appeared (Performance section).

Not yet played by the owner.

### Run structure

Owner decision (2026-09-24): one run plays through all 10 levels in order. This intentionally differs from Vampire Survivors' separate per-stage runs.

Killing a stage's boss clears that stage and starts the next one after a short transition; a stage whose boss is still alive ends at its 30-minute limit. Killing the Level 10 boss wins the run.

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

Not adopted: Stage 2 unlocking at player level 20 (conflicts with the one-run 10-level structure); Inlaid Library's wave tables, enemy/item/relic names and ×1.25 speed (VS game data, and taken from the fan wiki rather than poncle); its corridor and left/right spawning (Soul Hunter Level 2 is The Dark Forest). Stage events (wall/swarm/self-destruct enemies), destructibles, stage-placed items and Hyper mode are candidate features that need owner direction before they are built.

### Boss level scaling and chest elites (owner decisions, 2026-09-24)

Found while reviewing the VS Stage 4/5 write-ups: bosses had fixed health (1000 x stage) although one run keeps levelling across 10 stages, and only stage bosses dropped chests (~10 per run), so evolutions were rare. The owner chose:

- **Boss HP x player level:** `EnemyData.HealthPerPlayerLevel` (> 0 means max health = it x player level, then Curse). All ten stage boss assets use 50, which reproduces the old values if the player gains ~20 levels per stage; provisional balance. Shadow Kael scales too, on top of mirroring the loadout.
- **Timed chest elites:** every `_chestEliteIntervalSeconds` (300 s) of stage time before the boss (5/10/15/20 min), `EnemySpawner` spawns the stage elite with 5x health and 1.5x size that always drops a chest (`EnemyDrop.DropChestThisLife`, cleared on respawn so pooled reuse never drops extra chests). Not blocked by the enemy cap. Values provisional.

Validated by `BossScalingAndChestEliteTests` (T) and `ChestEliteAndBossPlayModeTests` (R); EditMode 246/246, PlayMode 23/23. Balance and feel in real play are unverified.

Stage 3-5 write-ups (Dairy Plant, Gallo Tower, Cappella Magna) added no other adoptable official mechanics; their stage data, names, unlock levels and Hyper/Hurry modes are VS content or conflict with the run structure. Candidate features still awaiting owner direction: shooting ring (needs enemy projectiles), step-on traps that trigger events, environmental weapons (rail cart), telegraphed strikes, invulnerable timed stalkers, stage-placed items, teleport points.

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
- Flashing VFX off: no hit flash, and the low-HP warning becomes a steady tint. Damage Numbers off hides combat numbers (merchant cost popups stay). Screen Shake off stops `CameraShake`.
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

Current priority is to use the established project truth to perform actual Soul Hunter development safely and incrementally.

Pending broader verification work may continue in parallel when useful:

- clean editor/package compile baseline;
- final requirement-to-acceptance reconciliation;
- representative runtime/build/performance acceptance.

These pending items are not a reason to endlessly postpone ordinary, explicitly authorized development work.

---

## 13. Non-Negotiable Truth Rule

Optimize for truth, not for a positive verdict.

A truthful `FAIL`, `UNKNOWN`, or `BLOCKED` is better than a false `PASS`.

Do not claim a feature, build, performance target, tool, or release state is complete unless the required evidence exists.
