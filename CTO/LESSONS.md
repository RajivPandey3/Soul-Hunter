# Lessons

- Dirty Git baselines can contain migrations represented as deletions plus untracked replacements. Do not infer deletion intent or clean them automatically.
- Script path drift invalidates remembered test results: the architecture runner still targets Assets/_Project/Scripts, while current sources live elsewhere.
- A passing assertion suite can coexist with runtime exceptions; the historical sequential-chest checks passed while Bible acquisition emitted errors.
- Ready status and a stopped Editor do not prove compilation or build health. The bootstrap compile-state query timed out.
- Default Git diff omits untracked files. Inspect newly created files explicitly and compare existing-content hashes.
- Historical evidence under ignored Logs is not automatically part of a Git checkpoint.
- Directory naming on Windows is case-insensitive; preserve existing casing when rename is prohibited.
