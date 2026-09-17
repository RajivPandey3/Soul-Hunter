# Initial bootstrap audit

Date: 2026-09-11 (Asia/Calcutta). Scope: read-first discovery and additive governance only.

## Baseline and Git

Existing repository: D:\Unity Projects\Soul-Hunter, branch main, HEAD 78d82fa90abbcdae4321c9b716ac2f7145c4e00e. Last commit subject: feat: Add VFXPoolManager, UIJuice, AI generated Sprites, and 10-level Game Design Document.

Before writes: 327 unstaged tracked deletions, 13 unstaged tracked modifications, 5,738 individual untracked files. Default grouped status reports 103 untracked entries. No staged diff. Deletions include old Assets/_Project/Scripts and asset paths, while reorganized source/assets are untracked; this suggests migration history, not authorization to delete or restore. No commit identifies the entire current dirty implementation.

The 13 existing modifications include Packages manifest/lock, build/graphics/physics/input/time/version-control settings, URP global settings and four asset/meta entries. All are outside bootstrap scope and preserved.

## Discovery coverage

| Area | Read evidence / result |
|---|---|
| Unity version/tools | ProjectVersion.txt, CLI --version/status, editor_status, executable product version; see CTO/ENVIRONMENT.md |
| Packages | manifest and lock inspected; package installation/resolution not executed |
| ProjectSettings | 26 files inventoried; build settings, time, input and product/screen settings sampled |
| Assets | 3,603 files: 154 C# files, 17 scenes, 206 prefabs, 350 .asset files, 4 asmdefs, 12 asmrefs; counts include all Assets content |
| ScriptableObjects | Upgrade, evolution, union, wave, enemy, character, Arcana, achievement and game-content catalog types found; .asset count is not a ScriptableObject-only count |
| Scenes | 14 enabled build entries: Bootstrap, Loading, MainMenu, Main_Gameplay, ten showcase scenes; additional review/root scenes exist |
| Tests | Assets/Tests and Assets/_Project/Tests have folder/meta scaffolds; no [Test]/[UnityTest] matches in searched Assets/_Project and Tools C# sources; no test asmdef among four discovered |
| Documentation | 9 files under Docs; game design, architecture baseline, earlier audits, gameplay specifications and DOT verification history |
| Automation | 121 files under Tools, one AgentScripts file; C# eval inspection/setup/verification, Python art/export utilities, PowerShell backup/delivery scripts, architecture regression runner and Editor validators |
| Instructions | No existing project/ancestor AGENTS.md found in searched root/D:/Unity Projects/D:/ locations; external SHRS constitution referenced by architecture baseline was read |
| Orchestration | Requested CTO/task/report governance absent before bootstrap; no .github workflow directory found; no protected-test registry or autonomous scheduler found |
| PowerShell/Git/Python | Versions recorded in CTO/ENVIRONMENT.md |
| Compile errors | Editor status reports not compiling; read-only compile-failed query timed out. No error-CS match in inspected Editor.log search. Fresh compile acceptance UNKNOWN |
| Runtime evidence | Historical Bible reward-path exception documented; no Play Mode or gameplay test executed in this audit |

This is discovery, not a line-by-line semantic review of every script, package, scene or asset reference.

## Architecture and existing systems

Four assemblies establish Foundation -> Gameplay -> UI dependency direction, with an Editor-only tooling assembly referencing all three. Foundation provides services/events, bootstrap, persistence, scenes and input. Gameplay owns combat, movement, AI states, waves, XP/level-up, weapons/passives/evolutions and pools. Presentation includes menus/HUD, camera, audio and VFX. Config data uses ScriptableObjects and Resources/catalog lookups.

Existing architectural documentation describes MonoBehaviour/OOP boundaries with data-oriented hot paths, reusable buffers and pooling. Code also uses static singleton registries and scene searches. Folder names and namespaces do not alone prove ownership compliance. Legacy disabled code and reorganized directories remain; no cleanup performed.

Systems include campaign levels/bosses, chest rewards, Laurel/Crimson Shroud, Metaglio progression, Magic Wand, terminal victory/restart, economy, Arcana and persistence. Presence is not completeness or correctness.

## Existing tests and validation limitations

Tools/Verify*.cs includes isolated Editor and Play Mode scripts for projectiles, Laurel, Magic Wand, Metaglio, chest/evolution, pickups and victory. They are executable eval snippets outside Assets, not a discovered NUnit acceptance suite. Some mutate runtime state, authored assets or scenes; their names do not establish read-only safety.

Tools/ArchitectureChecks/run-regression.py targets Assets/_Project/Scripts and explicitly names missing old paths, including Editor/Validator/ProjectValidatorLayer2.cs. The validator now exists under Assets/_Project/Tools/Validation. The runner uses temporary output and .NET 8. It was not executed; path drift is a deterministic pre-existing defect. Historical 27-test claims are not current proof.

Editor validators Layer1-4 and LiveBackgroundValidator exist. No conventional BuildPipeline.BuildPlayer match appeared in the searched project/tool C# code; export/showcase/delivery helpers exist. A reproducible standalone release build path is NOT VERIFIED.

## Existing problems and new diagnostic observations

- Pre-existing large dirty/untracked state lacks a current accepted checkpoint.
- Pre-existing architecture validation path drift.
- Historical BibleWeapon.SpawnBibles NullReferenceException at line 61 after TouchDamage addition; prior sequential chest report records six assertions passing with 81 console errors. Not reproduced here; unresolved.
- Full DOT, reference/content scope, complete trace matrix and numeric target performance acceptance are incomplete.
- Historical Logs evidence is ignored by Git; artifact retention/checkpoint association needs explicit design.
- Empty Unity test scaffolding is not protected/acceptance coverage.
- CLI command resolution prefers D:\Unity; explicit binary is necessary.
- During THIS audit the read-only compile-state eval timed out after 5000ms. Current console sample contained one Pipeline request-timeout error. This is an audit diagnostic failure, distinct from a gameplay regression; no product files were changed.
- No fresh compilation, build, gameplay, screenshot or performance test was performed. UNKNOWN remains UNKNOWN.

## Missing infrastructure and risks

Missing enforcement includes task schema/runner, bounded retry enforcement, reviewer routing, protected-test ownership/hashes, requirements-to-evidence mapping, deterministic scenario seeds, artifact retention, validated checkpoint strategy and approved target-device profiles. Bootstrap folders do not implement these controls.

High risks: accidental loss from cleaning migration-shaped changes; false acceptance from partial tests; nondeterministic chest reward coverage; drift between tools and reorganized code; high-volume gameplay performance without target evidence; unreviewed governance changes.

## Recommended next steps

1. Review this baseline and agree an additive checkpoint plan that separates existing work from bootstrap docs; do not stage/commit automatically.
2. Authorize a tooling-only task to repair deterministic validation path drift and define protected test ownership/acceptance artifacts. First reproduce the runner prerequisite failure without gameplay edits.
3. Obtain a fresh compile baseline in a controlled editor session; do not interpret CLI ready as PASS.
4. Separately authorize reproduction and repair of the historical Bible exception, with unchanged requirements and independent review.
5. Define target hardware/performance profile and product/reference scope; build traceable acceptance tasks before broad autonomous execution.

## Files created by this bootstrap

- AGENTS.md
- CTO/MISSION.md, RULES.md, GLOBAL_REQUIREMENTS.md, PRIORITY_MATRIX.md, AGENT_CAPABILITIES.md, PERFORMANCE_BUDGET.md, ENVIRONMENT.md, STATUS.md, DECISIONS.md, LESSONS.md
- Reports/Reviews/INITIAL-AUDIT.md (this report)
- .gitkeep in each newly empty requested directory: Docs/{Requirements,Systems,Design,ADR}; Tasks/{Queue,Active,Blocked,Completed,Failed}; Tests/{Protected,Acceptance,Regression,Integration,Performance,Scenarios}; Reports/{Validation,Performance,Screenshots,Logs}; Tools/{Orchestration,Unity,Validation,Git,Emergency}.

Total: 12 Markdown files and 24 empty-directory placeholders. Existing Docs/architecture and Docs/gameplay are preserved as the Windows case-insensitive counterparts of Docs/Architecture and Docs/Gameplay. Reports/Reviews contains this report and needs no placeholder.

No gameplay code, ProjectSettings, Packages, scenes, prefabs, assets, existing documentation, existing scripts, Git history or index were intentionally changed. No files deleted, renamed or reset. No commits.

## Verification method

Before creation, SHA-256 aggregates of path plus per-file content were captured for Assets, Packages, ProjectSettings, Docs, Tools and AgentScripts, along with HEAD and tracked binary diff digest. Final verification compares existing files excluding only the newly listed placeholders, inspects tracked Git diff and untracked additions, checks all requested paths and reviews generated document content. A match establishes preservation of the inspected baseline, not product acceptance.

Baseline counts/digests:
- Assets: 3603 files; 43418ebb7365082abc35c7342c71a499dfed99d18b03d20e327295b9b1155c3c
- Packages: 2 files; 410930d5f2bf234205963ed643ffa17012cfb9a670a6aa3ceb0fccafb068fe0b
- ProjectSettings: 26 files; 7ad9f96c80956607c57325d7a4c996b4a15b680c53876b8e01c342cf5da8ed94
- Docs: 9 files; 05b67092e686c0f1e92f72c53327c174fa2aff8f2d5b490ee9fb342b519c30f4
- Tools: 121 files; 0a80815a482df59113e550e7ecbfef025a70e8887ae2afdfd64c3584f02a1762
- AgentScripts: 1 files; 0abcc82f9a4aa5e2950f472c79567a029a4012982c5878a12e71bd96cb5d72d9
Tracked diff digest: 1aa2df9bca383b8d8c658dd327af26ee5bd19524df761e1770d987f165dbe5ac.
