# Survivor Implementation Status

**Review date:** 2026-09-06  
**Owner:** CTO architecture pass  
**Scope:** Vampire Survivors-style survivor loop implemented with original Soul-Hunter code, assets, names, and narrative.

This is the working audit for the 0–100 feature specification in [survivor-systems-spec](survivor-systems-spec.md). “Implemented” means the behavior has a runtime owner and a verifiable code path. “Partial” means a path exists but still needs production wiring, pooling, tuning, or play-mode verification. “Missing” means the feature has no reliable runtime owner yet.

| Area | Status | Evidence / remaining work |
|---|---|---|
| Player movement, dash, health, death | Implemented | `PlayerController`, health events, 3D physics path. Needs Unity play-mode balance pass. |
| Auto-fire targeting and weapon cadence | Partial | Weapon scripts cover the main families; several still use direct `Instantiate` and need `WeaponPoolManager` adapters. |
| Weapon upgrades and level-up choices | Implemented | `LevelUpManager`, `UpgradeData`, `WeaponManager`; needs content completeness and UI play-mode verification. |
| Weapon evolution / unions | Partial | `WeaponManager` has union/evolved paths; every intended evolution needs data asset and acceptance test. |
| XP gems, magnet collection, level progression | Implemented | `PlayerExperience`, pooled gem path, non-alloc magnet scan. |
| Rare pickups (chicken, chest, magnet, time freeze) | Partial | Pool and drop paths exist; prefab references for magnet/time-freeze are not present in the repository and must be wired in Unity. |
| Enemy AI, waves, elite and boss events | Partial | Spawner and event director exist; boss/elite transient objects still instantiate directly and need pooling/tuning. |
| Damage, knockback, invulnerability, status effects | Partial | Core damage paths exist; effect stacking and immunity rules need a single data-driven owner. |
| Map, props, camera follow, boundaries | Partial | Infinite map and prop spawner exist; performance and 10-level boundary validation remain. |
| Run timer, kills, pause, HUD, audio/VFX events | Implemented | Event-driven timer/kills and pooled VFX; full presentation tuning remains. |
| Ten-level Soul-Hunter campaign | Partial | Level narrative/modifier mapping is documented; scene/content assets and progression gates need completion. |
| Save, meta progression, settings | Partial | `SaveService` owns JSON persistence and migration; meta currencies/upgrades still need complete content. |
| Accessibility and verification | Partial | Architecture regression suite passes; no Unity play-mode/build evidence yet. |

## Latest pass

Bone, Runetracer, Fire Wand, Gun, Holy Wand, Magic Wand, Santa Water, Lightning Ring, Pentagram, and Death Spiral now acquire transient projectiles/effects through `WeaponPoolManager` when it is available. Their lifetime deactivates the object so the pool can reuse it; the direct-instantiation fallback remains for scenes that have not installed the pool yet.

Enemy instances now clear AI state, target, and rigidbody velocity on pool disable/re-enable. VFX prefabs supplied by content teams receive the same auto-disable lifecycle as procedural VFX, including child particle systems.

Bible orbitals, Clock Lancet beams, and Song of Mana beams now use pooled lifetimes. Touch damage resets its timer and target state on reuse, and Bible orbitals are deactivated/reparented instead of destroyed when the weapon is disabled.

`PropSpawner` now reuses inactive destructible props through a local pool. `InfiniteMap` validates its chunk prefab and skips missing chunk references during repositioning.

Pause input now travels through `InputService` and `EventBus`; `GameSystemManager` no longer polls keyboard state every frame and disposes its pause subscription with the UI object.

`PowerUpMenuUI` now subscribes to `EconomyService.OnGoldChanged`, so gold labels and purchase state refresh immediately after a transaction and listeners are removed on teardown.

The ten Soul-Hunter stages now have a canonical runtime catalog with narrative hooks and modifiers in `CampaignLevelCatalog`; `LevelProgressionManager` exposes the current definition and logs it at stage start. Magnet and Orologion also have procedural runtime fallback pickups when scene prefabs are not assigned.

High-frequency enemy targeting and arena scans now use the active enemy registry; remaining `FindFirstObjectByType` calls are setup-time/service-boundary lookups. Remaining `Instantiate` calls are pool warm-up paths, intentional persistent objects, or compatibility fallbacks when a pool is not installed.

## CTO execution order

1. Wire and validate all pickup prefabs, then add deterministic drop tests.
2. Route projectile and short-lived weapon effects through `WeaponPoolManager`/VFX pools.
3. Finish data assets for every weapon evolution, enemy tier, boss, and level modifier.
4. Replace remaining global manager dependencies with `GameServices` registrations through compatibility adapters.
5. Run Unity editor validation, play-mode smoke runs, and a 10-level acceptance matrix.

No asset is deleted by this audit. Unused candidates remain available until a reference scan and an explicit cleanup pass approve quarantine.
