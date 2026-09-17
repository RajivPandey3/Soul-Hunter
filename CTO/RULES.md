# Rules and acceptance authority

1. Read first; state scope, baseline, owner, risk and allowed paths before changes. Separate pre-existing failures from task-introduced failures.
2. UNKNOWN is never PASS. Completion by an agent and passing tests alone are insufficient for final acceptance.
3. Never weaken requirements, acceptance criteria or protected tests to manufacture PASS. Implementation agents may add evidence but cannot weaken/delete protected tests. Governance changes require explicit owner review and a recorded decision.
4. Final acceptance requires requirement coverage, implementation review, relevant reproducible tests, runtime/visual evidence where applicable, separate performance evidence, risk review and an authorized Git checkpoint. Missing evidence leaves the relevant gate UNKNOWN or NOT TESTED.
5. Each task records ID, requirement IDs, scope/non-goals, baseline HEAD plus dirty-state evidence, allowed files, dependencies, acceptance scenarios, risk, owner/reviewer, retry limit and evidence paths.
6. Lifecycle: Queue -> Active -> review -> Completed only after acceptance. Blocked means an external dependency or authority is needed; Failed records a failed attempt without erasing evidence. A retry stays linked to the original task.
7. Default retry bound: initial attempt plus at most two evidence-driven retries per failure signature. Stop blind repetition; record last evidence and escalate. A changed hypothesis must be explained.
8. P0 incidents and P1/high-risk work require a concrete plan and appropriate independent technical review; unresolved destructive, security, persistence, architecture or release decisions escalate to the user/owner. Do useful authorized read-only work while blocked. Routine reversible authorized work needs no redundant permission.
9. Preserve Unity file/meta pairs and existing dirty work. Never use broad reset/clean, indiscriminate staging, history rewrites or silent migrations. A checkpoint must describe tested dirty changes, not merely name an older HEAD.
10. Functional correctness and performance are separate gates. Editor readiness, static checks and one screenshot are not a build, target-device, or complete DOT pass.
11. Never expose tokens, credentials or full environment secrets. Pipeline descriptor files can contain bearer tokens.
12. This bootstrap installs policy and folders only. It does not enforce rules through CI, permissions or a running scheduler; enforcement remains pending.
