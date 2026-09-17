# TASK-001: Reproducible Baseline and Validation Harness Repair

Status: BLOCKED for full baseline acceptance; infrastructure repair delivered for review.
Authorization: user explicitly authorized TASK-001 infrastructure work and updates to CTO/STATUS.md and CTO/ENVIRONMENT.md. This supersedes their earlier bootstrap-only phase text for this task only.

Requirements: GOV-003, GOV-004, GOV-005, TRACE-001; user TASK-001 phases 1-8. Risk: P1 validation integrity. Owner: primary infrastructure agent. Independent review: pending; no self-granted final acceptance.

Allowed edits: validation harness/tools, task/evidence reports, the two authorized CTO files. Editor-only validation path repair is allowed; gameplay source, acceptance assertions, scenes, prefabs, Packages and ProjectSettings are not.

Baseline: main / 78d82fa90abbcdae4321c9b716ac2f7145c4e00e plus Reports/Validation/TASK-001/baseline.json, git-before.txt and tracked-before.patch. Preserve all existing 327 deletions/13 modifications and untracked work. Bootstrap set is enumerated in integrity.json.

Acceptance: bounded compile command with stdout/stderr/exit report; unchanged regression assertions against mapped sources; historical Bible diagnosis; suite inventory; verified-only environment updates; final diff and source integrity check. All required gates need evidence for PASS.

Outcome: 27/27 unchanged standalone regressions pass. Full Unity compilation UNKNOWN because batch process cannot open the already-open project. Runtime validation BLOCKED/PRE-EXISTING by Bible acquisition defect. Asset acceptance UNKNOWN. No gameplay changed and no commit.

Retry bound: initial plus two evidence-driven retries per signature. One batch attempt; no repeat against unchanged lock. Regression before, path repair, then final editor-validator path repair each had distinct evidence. No TASK-002 started.

Report: Reports/Validation/BASELINE-001.md. Next decision belongs to the owner: arrange an exclusive or isolated validation project and review this tooling diff. Do not close existing Editors, alter gameplay, or begin another task automatically.
