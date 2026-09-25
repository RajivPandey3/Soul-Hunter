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

Existing Burning Village / fire-hazard behavior is implementation divergence, not the authoritative requirement.

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

The currently observed generic boss implementation is incomplete relative to this requirement.

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

`EnemyFlyState` existed unused and its `Exit` set the collider solid, which would have made every pooled flier a wall; fixed (enemy bodies stay triggers). Known limitation: a Time Freeze sends fliers to the chase state afterwards.

Validated by `StageEventTests` (T) and `StageEventPlayModeTests` (R: the 2:30 swarm fires on the stage clock; every formation flies; pooled fliers stay triggers); EditMode 251/251, PlayMode 25/25. Look and feel in real play unverified.

### Audio

No scene contains an `AudioManager`; it creates itself from `Assets/Resources/AudioManager.prefab` at startup (`RuntimeInitializeOnLoadMethod`). Found 2026-09-25: that prefab was hand-written YAML with corrupted file IDs, which Unity imported as a DefaultAsset, so `Resources.Load` returned null, no AudioManager ever existed and the game had no sound at all (every PlayMode log showed "prefab not found"). Rebuilt with Unity's prefab writer (`AudioManagerPrefabBuilder`, same GUID). Enemy hits now play `EnemyHit` (throttled to one per 0.06 s via `PlaySFXThrottled`). Validated by `AudioPlayModeTests` (R: one listener, all five effects assigned and not silent, hitting an enemy plays EnemyHit); batchmode cannot confirm speakers, so hearing it is still unverified.

Missing audio was then synthesised in code (owner choice 2026-09-25, retro style, no external samples): `RetroAudioSynth` (menu Soul Hunter/Content/Generate Retro Audio) writes 12 effects (EnemyDeath, PlayerHurt, GameOver, StageClear, BossWarning, UIClick, UIConfirm, Unlock, Shoot, Slash, Magic, Explosion) and 11 music loops (menu + one per stage, each with its own key, tempo, scale and progression; loops end at silence so they repeat without a click), and adds a 3 ms fade-in to the five older effects that started with a click. `AudioManagerPrefabBuilder` wires them all into the prefab. Hooks: stage music and stage-clear / game-over / victory sounds via `StageAudio` (added by `GameSessionManager`), menu music on MainMenu, enemy death, player hurt (throttled), boss warning, unlock notice, UI click/confirm, and a fire sound per weapon (`AutoAttackWeapon.FireSound`: Shoot, Slash, Magic, Explosion, or None for Garlic and Bible; throttled). SFX volume no longer applies MasterVolume twice. Validated by `AudioPlayModeTests` (R: every clip assigned, effects audible, menu and per-stage music, weapon, hit, death, hurt and game-over sounds); EditMode 281/281, PlayMode 33/33 (`D:SoulHunter-Validationun-20260925-audio2`). Nobody has listened to it yet: how it sounds (`V`) and the mix are unverified.

### Accessibility

Not currently evidenced as complete:

- rebindable controls
- screen-shake toggle
- damage-number toggle
- color/readability options

### Persistence

JSON persistence exists, but robust general schema migration and corrupt-save recovery were not proven.

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

Only Kael is available at the start. Every other character has an `OriginStage`, an unlock condition (`ClearStage`, `SurviveInStage`, `ReachPlayerLevel`, `KillsInRun`, `WeaponLevel`) and a `GoldPrice` in its `CharacterData`. `CharacterUnlockTracker` (added by `GameSessionManager`) checks the conditions during a run, stores unlocks in `GameData.UnlockedCharacters`, saves and shows a notice. On the select screen a locked character is a silhouette with its condition and cannot be confirmed; an unlocked one shows its price and Confirm becomes Buy (gold via `EconomyService`, stored in `GameData.OwnedCharacters`); gold is shown top left. The right-hand panel is the VS character filter: `FILTER: ON/OFF` plus one checkbox per level (count `x/10`) that hides characters from unchecked origin stages. Conditions, origins and prices are provisional Soul Hunter data (not VS data): Corvan survive 5:00 in Stage 1 (300 G), Aria player level 20 (500), Draven 1000 kills in a run (700), Gideon clear Stage 3 (900), Kaelen clear Stage 4 (1100), Valerius survive 15:00 in Stage 4 (1300), Isolde Santa Water level 4 (1500), Elaria Magic Wand level 8 (1700), Seraphina clear Stage 6 (1900), Lumina clear Stage 7 (2200), Rurik clear Stage 8 (2500), Sylas clear Stage 9 (3000). Gameplay itself does not re-check ownership; the menu is the only way into a run. The owner's existing save keeps its gold but now owns only Kael. Validated by `CharacterUnlockTests` (T, in-memory GameData) and `CharacterSelectionPlayModeTests` (R: locked/buy/owned states, filter, tracker follows stage clears); EditMode 276/276, PlayMode 31/31, save file unchanged by the tests (`D:SoulHunter-Validationun-20260925-unlocks`). Whether VS's exact filter semantics match this is from memory, not a source.

### Stage floors (owner request, 2026-09-25)

The ground looked broken: InfiniteMap tiled Level 1's 10x10 ground at 20-unit spacing on every level (gaps showed the background), and each stage's showcase scene added its own fixed 3x3 ground on top. Now, as in VS where every stage has its own ground, `ArtSource/Generators/build_grounds.py` (Blender) builds a seamless 20x20 low-poly floor per level (`SH10_Lxx_Floor`: graveyard soil and bones, forest moss and ferns, village cobbled streets, castle flagstones, crimson swamp mud and puddles, snow and ice, soul-sea sand and shallows, blood-arena sand, throne marble with gold inlay, soul-core crystal plates). `StageFloorImporter` (menu Soul Hunter/Content/Import Stage Floors) builds prefabs with one flat BoxCollider and fills `GameContentCatalog.StageFloors`. `InfiniteMap` lays the current stage's floor, swaps it on `OnStageStarted` and turns each tile a random quarter turn; `CampaignSceneConnector` hides the showcase grounds (`IsShowcaseGround`). Validated by `StageFloorTests` (T) and `StageFloorPlayModeTests` (R: level floor, no gaps, swap on stage change, showcase ground hidden, player stands); EditMode 272/272, PlayMode 29/29 (`D:SoulHunter-Validationun-20260925-floors`). In-game screenshots of all 10 levels checked (partial `V`). The showcase scenes also placed a still `SH10_Lxx_Enemy`, `Elite` and `Boss` model at x = -2.3, 0 and 2.3 on every stage, right at the player spawn (2, 1, 0), looking like enemies that never move or die; `CampaignSceneConnector.IsHiddenInPlay` now hides them too (scenery props stay). Reproduced by `StageFloorPlayModeTests` (failed with 9 statue objects) before the fix.

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
