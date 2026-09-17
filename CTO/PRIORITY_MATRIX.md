# Priority matrix

| Priority | Trigger | Required handling |
|---|---|---|
| P0 | Data loss, credential exposure, destructive automation | Contain within authorization; user/owner escalation; independent review before resuming |
| P1 | Runtime exception, invalid acceptance, broken validation, high-risk save/architecture change | Reproduce/classify; bounded plan; appropriate reviewer before acceptance |
| P2 | Functional gap with isolated impact | Requirement and scenario; scoped implementation only when authorized |
| P3 | Presentation/documentation polish | Review against explicit criteria; no unsupported parity claims |

Current proposed ordering (not implementation authorization):
1. P1: establish reproducible dirty baseline/checkpoint plan and repair validation-path drift in a separately authorized tooling task.
2. P1: obtain deterministic compile/runtime baseline; then scope the documented Bible reward-path exception.
3. P1: define acceptance ownership, traceability and protected-test registration.
4. P2: agree target performance profile, reference/content scope and per-system acceptance scenarios.
5. P2/P3: prioritize product gaps from evidence, not speculation.
