# AGENTS.md — Soul Hunter Autonomous Development Operating Contract

**Project:** Soul Hunter  
**Canonical project root:** `D:\Unity Projects\Soul-Hunter`  
**Unity:** `6000.0.36f1` (`9fe3b5f71dbb`)  
**Primary platform:** Steam / Windows  
**Secondary platform:** Android  
**Status date:** 2026-09-11

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

External survivor-style games are inspiration only. Soul Hunter is its own Survivor Roguelite and has no mandatory 1:1 parity or DLC-parity requirement.

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

Automation root:

`D:\SoulHunter-Automation`

Verified external source baseline:

`D:\SoulHunter-Automation\Baselines\SOUL-HUNTER-SOURCE-BASELINE-20260911-155232\project`

Verified selected-source baseline identity:

- file count: `3837`
- SHA-256 aggregate digest: `9f0af6e9d0925258aeb8ccc34273915a27e16eb4dffe572b313e1196e5683ec8`
- branch at capture: `main`
- HEAD at capture: `78d82fa90abbcdae4321c9b716ac2f7145c4e00e`

The external baseline is a verified snapshot of the selected authoritative source/governance roots. Do not describe it as a complete disaster-recovery image of every repository file.

### Safety rules

- Do not run `git reset`, `git clean`, destructive checkout/restore, forced rebase, or delete large file sets without explicit owner approval.
- Do not silently discard current working-tree changes.
- Do not modify canonical Soul Hunter merely to make a validation step pass.
- Risky automation/build/setup methods should execute in an isolated workspace first.
- Never delete Unity lock files or kill a Unity process merely to bypass a lock.
- Do not deploy or publish without explicit authorization.
- Preserve evidence for meaningful accepted changes.

The current implementation is highly divergent from old HEAD and must not be treated as recoverable from HEAD alone.

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

### EnemyData reference defect

22 active enemy/boss prefabs were observed referencing missing GUID:

`01217a48f2e5cb44588556a395026c0a`

No current active `Assets` meta was found defining that GUID in the verified audit snapshot.

Do not downgrade this to `UNKNOWN`; it is a known serialized-data defect.

### Bible prefab defect

`Bible_Weapon.prefab` was observed with `_biblePrefab` referencing its own root object.

Historical runtime evidence included Bible-related exceptions.

Treat Bible acquisition/use as high risk until repaired and validated with a zero-exception runtime scenario.

### Audio

`AudioManager.cs` exists, but deterministic serialized/runtime ownership was not proven.

Treat AudioManager integration as `UNKNOWN / HIGH RISK`, not as accepted.

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

---

## 8. Build / Setup Tooling

`Assets/_Project/Tools/Build/` source coverage has been audited.

Status:

**SOURCE AUDITED / EXECUTION UNVERIFIED / HARDENING REQUIRED**

Known risks include:

- inactive-object searches may create duplicates;
- hard-coded tag/layer assumptions;
- `UI` tag precondition inconsistency;
- broad `PrefabUtility.ApplyPrefabInstance` mutations;
- fuzzy/unsorted asset discovery;
- reflection-based private-field assignment;
- missing dependencies can produce warnings or silent returns while scripts still print completion messages.

A success log from these builders is not acceptance evidence.

Execute these tools only in an isolated workspace until deterministic/idempotent behavior and exact serialized deltas are proven.

---

## 9. Unity Validation Status

Current project editor:

`D:\6000.0.36f1\Editor\Unity.exe`

An isolated fresh import was performed from the verified external source baseline.

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

## 10. Requirement / Acceptance Rules

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

## 11. Automation System

Automation architecture:

**SOUL HUNTER AUTONOMOUS DEVELOPMENT SYSTEM v1.0**

Core flow:

`Objective -> Requirement/DOT -> Conflict Check -> DAG -> Routing -> Permission/Resource Gate -> Isolated Workspace -> Execute -> Validate -> Evidence -> Acceptance -> Retry/Recover/Escalate -> Checkpoint -> Next Task`

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

### Agent routing

Default routing:

- architecture / requirements / coordination -> ASTRA
- C# / algorithms / refactors / deterministic tests -> Codex
- scenes / prefabs / Inspector / serialized Unity work -> Antigravity
- filesystem / processes / workspaces / logs -> PowerShell
- Git mechanics -> Git / PowerShell
- deterministic compile / test / build / runtime validation -> Unity CLI

Mixed tasks should be decomposed by capability.

No worker may expand its own permissions.

### Automation current status

- accepted implementation: `99%`
- tasks: `40/41 DONE`
- final task: `SYS-041`
- final isolated E2E remains pending because of genuine Codex usage quota
- do not bypass that quota gate
- do not claim the automation system is 100% / final until the AcceptanceEngine receives the final required PASS evidence

Automation being 99% does not block normal owner-authorized Soul Hunter work that does not require the missing Codex E2E proof.

---

## 12. Development Workflow

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

---

## 13. Asset / Art Workflow

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

## 14. Current Operating Direction

The expensive full static audit has already been completed.

Do not repeat it unless the owner explicitly requests a new full audit or the project state changes enough to invalidate the current verified baseline.

Current priority is to use the established project truth to perform actual Soul Hunter development safely and incrementally.

Pending broader verification work may continue in parallel when useful:

- clean editor/package compile baseline;
- final requirement-to-acceptance reconciliation;
- representative runtime/build/performance acceptance;
- `SYS-041` automation E2E after Codex quota becomes available.

These pending items are not a reason to endlessly postpone ordinary, explicitly authorized development work.

---

## 15. Non-Negotiable Truth Rule

Optimize for truth, not for a positive verdict.

A truthful `FAIL`, `UNKNOWN`, or `BLOCKED` is better than a false `PASS`.

Do not claim a feature, build, performance target, tool, automation phase, or release state is complete unless the required evidence exists.
