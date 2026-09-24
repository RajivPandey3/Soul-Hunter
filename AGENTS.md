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

`Whip_Weapon.prefab` shipped with an empty `_enemyLayer`, so the Whip never hit anything. It surfaced once character starting weapons were fixed (Antonio starts with the Whip). `WhipWeapon` (and `GarlicWeapon`, defensively) now default an empty mask to the Enemy layer. Validated by `PlayerLoadoutPlayModeTests` (R): Antonio is visible, starts with the Whip, and the Whip damages an adjacent enemy.

### Audio

No scene contains an `AudioManager`; it now creates itself from `Assets/Resources/AudioManager.prefab` at startup (`RuntimeInitializeOnLoadMethod`). Enemy-hit, death and music audio are still missing.

Treat audio as `UNKNOWN / HIGH RISK` until sound is confirmed in a runtime run.

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

### Character move speed data defect

`CharacterData.BaseMoveSpeed` mixes conventions: Kael (4), Antonio (3) and Elaria (3.5) look like absolute speeds, the other ten characters (0.8-1.3) look like multipliers, while the player's base speed is 5. It is therefore not applied; the other character stats (max health, Might, Area, Cooldown, Armor, starting weapon) are. Needs an owner decision on the convention before move speed can be applied.

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
