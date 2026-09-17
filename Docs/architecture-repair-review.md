# Architecture repair review — 2026-09-06

Authority: [SHRS Engineering Constitution](../../SHRS%20Standards%20Repository/docs/CONSTITUTION.md).
Scope: fix confirmed implementation defects without deleting assets, changing assembly dependency directions, or guessing unresolved architectural policy.

## Changes and rule mapping

| Rule / finding | Implemented correction |
| --- | --- |
| Safe runtime hierarchy / verification | UI, Cameras and Entities organizational roots are Untagged. Their object IDs and prefab references are preserved. HierarchyOrganizer no longer creates EditorOnly parents and records parenting undo. |
| Predictable service lifetime | Bootstrap owns a persistent framework. Re-entering Bootstrap reuses it. Registry initializes in registration order, rejects duplicates, releases IDisposable services in reverse order, and cleans failed startup. Input actions release on shutdown/replacement. |
| Central scene management | UI transitions use SceneService exclusively. It validates scene availability and queues a request made during scene activation; duplicate requests do not overlap. |
| Central persistence | High score and volume belong to SaveService/GameData. Existing PlayerPrefs keys migrate once and are retained. GameSystemManager keeps its serialized fields/button callback names as a UI compatibility adapter. |
| 3D collision blueprint | Knife/ThousandEdge use the player's cached 3D body and XZ spread. Bone/Runetracer use 3D bodies and enemy-targeted projectile damage. ClockLancet uses a buffered 3D raycast and the existing enemy frozen state. |
| Allocation discipline | Pickup, ChestPickup, HealthPickup and player dash reuse query buffers. |
| Event-driven HUD | Kill and survival-second producer events replace per-frame text polling. HUD releases subscriptions from its bound producers. |
| Canonical diagnostics | The old Core EventDebugger file remains as a deprecated, separately namespaced forwarding adapter. Foundation owns its implementation. |
| Subscription lifetime | EventBus returns a disposable subscription token while retaining manual Unsubscribe. Legacy subscription disposal removes its registry entry. |
| Evidence-based validation | Layer 2 masks comments/literals and checks brace-bounded frame methods. Results are source-review hints, not performance certification. |

## Validation evidence

- Five assemblies compiled with Roslyn against the project's installed Unity 6000.0.36f1 and package assembly references: Foundation, Gameplay, UI, Assembly-CSharp support scripts, and Editor.
- 27 managed regression checks passed, covering ordered startup/cleanup, failures, replacement, subscription disposal, validator false positives, save migration, and queued scene transitions.
- Run again with `python Tools/ArchitectureChecks/run-regression.py` (Python and .NET 8 SDK required; optional `DOTNET_EXE`).
- The harness compiles production managed classes with explicit Unity API test doubles. It does not test native Unity physics, native JsonUtility behavior, actual scene activation, or player builds.
- No asset files were deleted or moved. Existing script meta files and scene prefab GUID references are preserved.

## Remaining architecture decisions and Unity verification

- Play through Bootstrap → Loading → MainMenu → Main_Gameplay → restart in Unity; verify camera, HUD, player, input, projectile collisions, and save migration in a standalone build.
- Existing static GameServices/manager access remains a documented compatibility bridge. This does not satisfy the approved no-static-global service blueprint; replacing all scene clients with injection is a separate structural migration, not a completed claim here.
- Broad ECS language and approved MonoBehaviour system blueprints still need one consistent reviewed baseline. No ECS conversion was invented during this repair.
- The Constitution has no exact two-prefab limit. All 35 existing Assets prefabs remain; the user's intended Player/Enemy-only rule needs reference/role mapping before consolidation.
- PlayerController still owns more than locomotion routing. Extracting its dash/model/death responsibilities and migrating gameplay-owned UI across assembly boundaries remain structural work.
- Source-pattern validation cannot prove absence of indirect hot-path calls, native errors, or all allocations. No 100% compliance or release approval is claimed.
