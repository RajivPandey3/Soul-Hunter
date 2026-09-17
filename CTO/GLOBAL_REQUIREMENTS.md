# Global requirements

| ID | Requirement | Current evidence / acceptance |
|---|---|---|
| GOV-001 | DOT = Detail-Oriented Total; complete all-aspects matching | User mandate; product verification incomplete |
| GOV-002 | Unknown, agent completion and tests alone cannot grant final PASS | CTO/RULES.md; enforcement infrastructure pending |
| GOV-003 | Preserve baseline and distinguish existing/new failures | Reports/Reviews/INITIAL-AUDIT.md |
| GOV-004 | Protect requirements, governance and protected tests | CTO/RULES.md; access/CI controls not installed |
| GOV-005 | Bounded retries and risk review | CTO/RULES.md |
| PERF-001 | Smooth performance on agreed low-power target | Profile unspecified; numeric budgets unlocked |
| TRACE-001 | Every meaningful requirement eventually traces end-to-end | Schema below; product inventory not yet migrated |

Required trace row: requirement ID and version/source -> behavior/properties -> implementation paths -> tests/scenarios -> dated evidence and environment -> review verdict -> authorized Git checkpoint (HEAD plus dirty patch identity until committed).

Track functional, visual, performance and integration verdicts separately. Preserve historical failures and superseding evidence. Existing dot-verification-gates.md is evidence input, not a complete traceability matrix.

Reference game/platform/version, DLC scope, target devices and all-aspects inventories must be agreed before full parity acceptance. Existing documentation pins a reference snapshot; this audit did not revalidate the external release claim.
