# BASELINE-001 — TASK-001

Date: 2026-09-11 Asia/Calcutta. Captured commands ran 2026-09-10 20:02–20:05 UTC (next calendar day locally). Scope: infrastructure only.

**Overall: UNKNOWN; runtime gate BLOCKED/PRE-EXISTING. Not PASS.** Task work stops at this report; independent review pending. No TASK-002 or gameplay work.

## Baseline protection

Git branch main, HEAD 78d82fa90abbcdae4321c9b716ac2f7145c4e00e. Pre-existing tracked state: 327 unstaged deletions, 13 unstaged modifications; index empty. Bootstrap produced 36 files (12 Markdown, 24 placeholders), all previously untracked. `TASK-001/integrity.json` explicitly enumerates that set, separate from other pre-existing files.

HEAD alone does not describe the implementation. `TASK-001/baseline.json` records UTC time, branch/HEAD and SHA-256 of 3,782 existing files across Assets, Packages, ProjectSettings, Docs, CTO, Tools, AgentScripts and AGENTS.md. `git-before.txt` records the individual untracked/tracked state; `tracked-before.patch` retains the existing tracked diff. The command-capture helper was newly created immediately before this snapshot and explicitly excluded from pre-existing hashes; its entry and the new evidence directory in git-before are TASK-001 artifacts, not bootstrap files.

No existing deletions/modifications were discarded, staged or committed. Final tracked binary diff is identical to the captured baseline. Because tooling sources were already untracked, tracked diff alone is insufficient: content hashes identify exactly four changed baseline files, listed below. No gameplay/source-data hashes changed.

## Gate results

| Gate | Result | Evidence and qualification |
|---|---|---|
| Unity compilation | UNKNOWN | compile/result.json: exit 1073741845; project-in-use fatal error before compilation; timeout false |
| Standalone regression before | FAIL / PRE-EXISTING | regression-before: exit 1, 11 CS2001 missing source files |
| Standalone regression after mapping | PASS, limited scope | regression-after: exit 0, TOTAL PASSED: 27 |
| Final regression after validator-path repair | PASS, limited scope | regression-final: exit 0, same 27 assertions unchanged |
| Unity EditMode/PlayMode suites | UNKNOWN / NOT RUN | No annotated test sources or test asmdefs discovered; ad hoc eval scripts are not equivalent |
| Bible runtime acquisition | BLOCKED / PRE-EXISTING | historical Editor.log stack excerpt plus current unchanged source/prefab inspection |
| Asset validation | UNKNOWN | Editor Layer1 validator exists, but no fresh complete asset validation run; Bible self-reference is a confirmed static finding |
| Performance | UNKNOWN | No agreed target profile, no performance test run |
| Baseline integrity | Verified for recorded files | integrity.json: gameplay/tests preserved, expected four infrastructure/document changes, HEAD/index preserved |

## Unity compilation attempt

Discovered installation D:/6000.0.36f1/Editor/Unity.exe, project 6000.0.36f1 (9fe3b5f71dbb). CLI 1.0.0-beta.8 verified separately. Batch command was non-interactive with no graphics and a 120-second external timeout. Started 2026-09-10T20:02:27.499380Z, ended 20:02:36.121411Z, exit 1073741845. Wrapper process result may appear as shell exit 1 for this large Windows exit code; the actual child exit is preserved in JSON.

Editor.log reports: “It looks like another Unity instance is running with this project open.” The stack includes HandleProjectAlreadyOpenInAnotherInstance. The process exited before useful compilation evidence; this is an environment occupancy blocker, not proof of C# compiler failure. Licensing diagnostics also appear but the explicit terminating condition is project-in-use. No existing Editor was closed, no lock removed, no same-project retry performed. No fresh actual Unity compiler errors were obtained. Compile regressions caused by this task cannot be ruled in/out through Unity; the tooling class compiles in the standalone harness, which is narrower evidence.

The future compile check requires an exclusive session or an explicitly managed isolated copy of the dirty baseline. A zero exit by itself must not be accepted without compiler/import completion evidence. No cached-ready result was substituted.

## Harness repair

Changed only Tools/ArchitectureChecks/run-regression.py and the Editor-only Assets/_Project/Tools/Validation/ProjectValidatorLayer2.cs, plus authorized governance updates. Regression.cs and its assertions/test doubles are byte-for-byte unchanged.

| Old source path relative to Assets/_Project/Scripts | Current path relative to Assets/_Project |
|---|---|
| Editor/Validator/ProjectValidatorLayer2.cs | Tools/Validation/ProjectValidatorLayer2.cs |
| _Foundation/Services/{GameServices,IGameService}.cs | Core/Services/{GameServices,IGameService}.cs |
| _Foundation/Events/{EventBus,GameEvent,EventSubscription,EventRegistry}.cs | Core/Events/{EventBus,GameEvent,EventSubscription,EventRegistry}.cs |
| _Foundation/Events/Core/EventSubscriptionEntry.cs | Core/Events/EventSubscriptionEntry.cs |
| _Foundation/Persistence/{SaveService,GameData}.cs | Save/SaveData/{SaveService,GameData}.cs |
| _Foundation/Scenes/SceneService.cs | Core/Services/SceneService.cs |

Runner now fails explicitly if any required source is missing. Layer2 scan root changed from stale /_Project/Scripts to /_Project, excluding Editor and relocated /_Project/Tools utilities to preserve its runtime-only intent. Its analyzer and acceptance assertions were not weakened. The scan entry point itself has not been validated in a fresh Unity compilation/session.

.NET SDKs discovered at D:/dotnet: 8.0.423, 8.0.424, 10.0.400; runner selects 8.0.424. The 27 tests exercise source analyzer, services/events, save migration and scene-service logic against explicit Unity test doubles. They do not validate native physics, assets, real Editor compilation or device performance.

## Bible investigation

Exact historical exception: NullReferenceException at BibleWeapon.SpawnBibles(), Assets/_Project/Gameplay/Weapons/BibleWeapon.cs:61. Retained evidence: TASK-001/bible-existing-stack.txt, sourced from the existing Editor.log, not a newly executed runtime reproduction.

Observed call chain: ChestPickup.Update/Collect -> ChestLogicController.ProcessChest -> LevelUpManager.SelectUpgrade -> WeaponManager.ApplyUpgrade/ActivateOrUpgradeWeapon -> BibleWeapon.OnEnable/SpawnBibles. Earlier prepared chest rewards reproduced this condition; the recorded sequential chest assertions passed while console exceptions occurred.

Immediately preceding error: adding TouchDamage failed because a concrete BoxCollider/CapsuleCollider/etc. was required. TouchDamage declares RequireComponent(typeof(Collider)); BibleWeapon adds it then dereferences the result without checking for null at line 61.

Static asset evidence: Bible_Weapon.prefab root GameObject fileID 3724841545492313711; _biblePrefab references that same fileID. It therefore references its own host, consistent with nested Bible_Weapon(Clone)(Clone) log names. Root components contain Transform and weapon script, no concrete collider. A self-instantiating host and missing collider are gameplay/asset wiring concerns, not validation-path defects. TouchDamage also targets Player tags, so merely suppressing the exception would not establish correct Bible enemy damage.

No safe infrastructure-only fix can establish Bible behavior. Mark BLOCKED/PRE-EXISTING; do not replace gameplay, silently add colliders, remove the weapon or skip acceptance. Fresh runtime reproduction remains NOT RUN in TASK-001.

## Test integrity and missing infrastructure

- Protected/Acceptance/Regression/Integration/Performance/Scenarios under Tests contain bootstrap placeholders only. No protected-test owner/registry or executable enforcement exists.
- Assets/Tests and Assets/_Project/Tests contain folder metadata only. No [Test]/[UnityTest] sources discovered across Assets.
- Tools/ArchitectureChecks provides 27 standalone checks. Regression.cs hash is unchanged in integrity.json.
- Tools/Verify*.cs includes isolated and runtime checks (Laurel, Shroud, Metaglio, projectiles, chests, pickup layers, victory). Some use reflection/prepared state and mutate runtime; some neighboring tools mutate assets. No blanket execution was attempted.
- Editor validator layers exist; no fresh asset or integration acceptance report was created by those validators.
- Historical profiling helpers exist; they are not a target-device budget or current performance proof.
- Independent review, deterministic scenarios, protected registration and an isolated Unity validation lifecycle remain missing.

## Exact execution commands

Working directory for all: D:/Unity Projects/Soul-Hunter. Each captured command directory contains stdout.txt, stderr.txt and result.json with exact argument array, cwd, UTC timestamps, PID, timeout and exit code.

```powershell
python Tools/Validation/capture-command.py --output Reports/Validation/TASK-001/compile --timeout 120 -- 'D:\6000.0.36f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\Unity Projects\Soul-Hunter' -logFile 'D:\Unity Projects\Soul-Hunter\Reports\Validation\TASK-001\compile\Editor.log'
python Tools/Validation/capture-command.py --output Reports/Validation/TASK-001/regression-before --timeout 120 -- python Tools/ArchitectureChecks/run-regression.py
python Tools/Validation/capture-command.py --output Reports/Validation/TASK-001/regression-after --timeout 120 -- python Tools/ArchitectureChecks/run-regression.py
python Tools/Validation/capture-command.py --output Reports/Validation/TASK-001/regression-final --timeout 120 -- python Tools/ArchitectureChecks/run-regression.py
python Tools/Validation/capture-command.py --output Reports/Validation/TASK-001/cli-status --timeout 30 -- 'C:\Users\Lenovo\AppData\Local\Unity\bin\unity.exe' command editor_status --project-path 'D:\Unity Projects\Soul-Hunter' --format json
python Tools/Validation/capture-command.py --output Reports/Validation/TASK-001/cli-version --timeout 30 -- 'C:\Users\Lenovo\AppData\Local\Unity\bin\unity.exe' --version
python Tools/Validation/capture-command.py --output Reports/Validation/TASK-001/capture-failure-check --timeout 5 -- python -c 'import sys; print(123); sys.exit(7)'
python Tools/Validation/capture-command.py --output Reports/Validation/TASK-001/capture-timeout-check --timeout 1 -- python -c 'import time; time.sleep(10)'
python Tools/Validation/task001-evidence.py
git status --porcelain=v1 -uall
git diff --binary
git diff --cached --stat
```

Use a new output directory on rerun; recorder refuses to overwrite previous captures. Intentional recorder failure test preserved exit 7/stdout; intentional timeout recorded timedOut=true and child exit 1 after terminating only its launched test process tree (wrapper exits 124). These are expected infrastructure self-checks, not project failures. No existing Unity process was terminated. Git/discovery/inventory reads returned 0; initial stale runner exit 1 and Unity batch exit above are distinct failures.

## Changed files and stopping state

Existing files edited: Tools/ArchitectureChecks/run-regression.py; Assets/_Project/Tools/Validation/ProjectValidatorLayer2.cs; CTO/STATUS.md; CTO/ENVIRONMENT.md.

Added: Tools/Validation/capture-command.py; Tools/Validation/task001-evidence.py; Tasks/Blocked/TASK-001.md; this report; evidence under Reports/Validation/TASK-001. No gameplay code/data, assertions, protected tests, Packages, ProjectSettings, scenes, prefabs or Git history changed. No staging or commit. Existing tracked diff retained byte-for-byte; untracked additions and baseline hashes inspected.

Stop here. Required compilation/runtime/asset gates remain unverified or blocked. This infrastructure delivery is not final project PASS and does not authorize the next task.
