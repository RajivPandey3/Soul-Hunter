# Decisions

| ID | Date | Decision and rationale |
|---|---|---|
| BOOT-001 | 2026-09-11 | Add documentation only; preserve all pre-existing project files and Git history per user scope |
| BOOT-002 | 2026-09-11 | Reuse existing lowercase Docs/architecture and Docs/gameplay; Windows case-insensitivity makes requested names aliases; no rename |
| BOOT-003 | 2026-09-11 | No fabricated hardware FPS budget; lock only after target profile is defined |
| BOOT-004 | 2026-09-11 | Do not rerun mutating historical eval/setup scripts during discovery; inspect their code/evidence |
| BOOT-005 | 2026-09-11 | Keep existing SHRS architecture reference; local bootstrap rules govern workflow under current user scope, not architectural replacement |
| BOOT-006 | 2026-09-11 | Use empty .gitkeep placeholders for newly created empty directories; no orchestration executable or scheduler added |

Existing external reference: D:\Unity Projects\SHRS Standards Repository\docs\CONSTITUTION.md (v1.0, read). It calls for architecture review and reviewed PRs for canonical changes. This bootstrap does not merge/push or modify its architecture. Conflicts must be recorded and resolved by the owner; user authorization controls the present scope.
