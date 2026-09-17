# Soul Hunter Autopilot System Package

This directory contains the authoritative, complete Autopilot & Orchestration package for the **Soul Hunter** project.

## Package Inventory
- **SoulHunter-Autopilot.ps1** / **SoulHunter-Autopilot-v2.2.42.ps1**: Core autonomous controller loop driving objective discovery, quota-aware semantic routing, safe disk management, and supervisor reviews.
- **SoulHunter-Orchestrator.ps1**: Isolated workspace task executor, semantic agent bridge (AGY / Codex), deterministic contract recovery, and Unity batchmode validator.
- **SoulHunter-Release-Gate-v3.ps1**: Supervised synchronous release gate verifying Unity compilation, scene builds, EditMode (12/12) and PlayMode (2/2) test suites.
- **LiveQuotaProbe.psm1**: Real-time probe verifying 5-hour and weekly quotas for AGY/Gemini, Claude, GPT, and Codex.
- **Quota/Quota-Reporter.ps1**: Independent quota reporter and state synchronizer.
- **Agents/Agent-Register.json**: Registered agent capability matrix and priority definitions.
- **objective-ledger.json** & **task-ledger.json**: Audited ledger tracking completed objectives and task statuses.

## Execution
`powershell
powershell -ExecutionPolicy Bypass -File SoulHunter-Autopilot.ps1 -Goal " Resume Soul Hunter development from the verified current checkpoint.\
`
