# DOT implementation gates

Updated: 2026-09-10

DOT means Detail-Oriented Total implementation. Damage-over-time is only one sub-feature of the damage/status system. The user requires all-aspects matching and verification before advancing, plus smooth low-power Game View performance.

## Pass definition

Each property requires a reference behavior, implementation owner, authored data, runtime wiring and reproducible evidence. A functional sub-test is not full reference parity. Editor FPS is not target-device certification.

The user selected the latest released reference version. As verified on 2026-09-10, the latest official Steam announcement is stable PC version 1.16.107, released August 30, 2026: https://steamcommunity.com/app/1794680/allnews/ . Pin this snapshot for reproducible tests; do not silently substitute a beta or assume identical platform hotfixes. Include newly documented behavior in the inventory. DLC ownership/content scope and target hardware/resolution/FPS remain unspecified; these do not block repairs to existing shared combat systems.

## Current evidence

- Laurel damage interception implemented, including charge consumption, grace period, recharge, disable cleanup and persistent visual reuse.
- Tools/VerifyLaurelShield.cs: seven isolated Unity checks passed.
- Tools/VerifyLaurelPrefab.cs: generated prefab has visual reference and blocks damage.
- Tools/VerifyLaurelRuntime.cs: four real Play Mode timing/block checks passed; Logs/LaurelRuntimeVerification.json.
- Existing Laurel tuning retained. Exact reference level table, cooldown/passive interactions, evolution and visual feedback parity remain OPEN. These tests do not certify full Laurel parity.
- Victory result flow: functional and sampled visual checks passed; limitations recorded below.

## Remaining gates

All rows of survivor-systems-spec.md remain subject to a fresh per-property audit. Priority order: shield/effects, terminal run flow, boss mechanics and mirror loadout, dark-forest lights/speed, merchant/permanent purchases, weapon/passive/evolution roster, movement/input, targeting and enemy states, wave/drop/XP/choice rules, scene transitions/persistence, audio/visual feedback/accessibility, profiling and standalone build.

Performance gate must record resolution, quality tier, hardware, active enemies/projectiles/pickups, sample duration, frame-time percentiles and GC allocations. No performance pass has been recorded.

## Victory and performance evidence

- Tools/VerifyVictoryFlow.cs passed in Main_Gameplay: victory event once, finished state, visible result text/panel, pause cannot resume a finished run. Triggered the terminal method directly; did NOT simulate ten complete stages or certify final-boss gating.
- Initial screenshot failed visual review (narrow text and overlap). Result panel layout corrected. Assets/Logs/VictoryVerificationFixed.png reviewed at 1280x720: readable result text and separate Restart button.
- Tools/VerifyVictoryRestart.cs invoked the real Restart button; time scale restored to 1, and MainMenu subsequently loaded.
- Full story climax, final boss condition, long weapon-stat lists and all-resolution UI review remain open.
- Logs/EditorPerformanceBaseline.json: 600 early-run Editor frames at 1920x1080, Intel i5-8350U / UHD 620, peak 5 enemies. Median 7.60 ms, p95 9.37 ms, max 376.01 ms, peak recorded GC 119117 bytes/frame. Includes Editor/tooling overhead. NOT a horde stress test or performance pass.
- Further source inspection confirms damage report gaps: Projectile does not record weapon source; some paths can carry both Projectile and ProjectileDamage. This requires a single damage authority and effective-damage accounting regression tests.
- Current captured console error count after these checks: zero.

## Projectile damage authority pass (2026-09-10)

- ProjectileDamage now defers to an enabled Projectile on the same object, preventing the two components from applying two hits for one contact.
- Consumed/inactive Projectile ignores queued contacts; player child colliders are excluded via their PlayerController ancestor.
- MagicWandWeapon and Gemini initialization now pass Might-adjusted damage to the active Projectile owner, with a weapon source label.
- Projectile reports observed health loss instead of nominal damage; pooled initialization clears stale source labels. Full reporting across other damage owners is still open.
- Tools/VerifyProjectileAuthority.cs: five isolated Unity checks passed (one authority, consumed contacts, pool reinitialization, invulnerability, standalone fallback).
- Actual physics-order coverage across all prefabs, Magic Wand/Arcana end-to-end stat combinations and full weapon parity are not yet certified.

## Magic Wand continuation (2026-09-10)

- Live prefab inspection found MagicWandWeapon inherited MonoBehaviour while WeaponManager only upgraded AutoAttackWeapon. Corrected inheritance and implemented the eight-level base progression; manager now respects the weapon's declared max level.
- Wand prefab baseline damage corrected from 15 to 10 through Unity PrefabUtility. All eight upgrade descriptions updated through AssetDatabase to match actual level effects.
- Sequential shots apply Might, Amount, Cooldown, Area and ProjectileSpeed. Targeting reads the active enemy registry instead of a truncated 50-collider buffer.
- Projectile contacts support pierce, bounce count, short duplicate-contact suppression, wall blocking and reset on reuse. Active Magic Wand projectiles are limited to 60 and release capacity when deactivated.
- Removed incorrect Magic Wand Gemini duplication; its bounce configuration checks Waltz of Pearls specifically.
- VerifyMagicWandLevels: 11 passed. VerifyProjectileAuthority: 5 passed. VerifyMagicWandContacts: 6 passed. VerifyMagicWandCapacity: 5 passed in Play Mode (Logs/MagicWandCapacityVerification.json).
- Final VerifyMagicWandRuntime: PASS, two 20-damage shots at speed 40 and area multiplier 1.5; interval 0.1015 seconds; closest target 500 -> 460 HP, further target unchanged. Uses actual colliders and pooled projectiles (Logs/MagicWandRuntimeVerification.json).
- Captured console errors after runtime verification: 0. Editor returned to stopped mode.
- Full weapon DOT is still OPEN. See magic-wand-verification.md for missing Arcana/Limit Break/calibration and performance gates. This milestone does not certify the full campaign or low-power performance.

## Laurel progression pass (2026-09-10)

Reference: https://vampire-survivors.fandom.com/wiki/Laurel (community gameplay documentation against the pinned release snapshot; not a reference-game replay).

- Corrected authored prefab to 10-second base recharge and 0.2-second initial block grace. Seven-level progression: timing improvements at 2/3/5/6; additional charge capacity at 4/7. Max base recharge is 8 seconds, capacity 3, grace 1 second.
- Only PlayerStats.Cooldown affects shield recharge. Mid-recharge changes preserve progress. Zero scaled delta does not advance recharge. Extra capacity earned from upgrades recharges normally rather than immediately refilling consumed charges.
- Reused shield visual uses blue/green/yellow property-block colors by remaining charge count. No per-hit material instances. OnBlocked emits once per consumed charge, not during grace.
- Weapon inventory/acquisition and level-up choices respect the prefab's MaxLevel. Removed the obsolete Laurel level-8 entry from GameContent catalog; retained its asset on disk for compatibility. Updated seven upgrade descriptions.
- VerifyLaurelProgression: 14 passed; VerifyLaurelShield: 7 regression checks passed.
- Full Laurel DOT remains open: Crimson Shroud and its two max-level prerequisites, Blood Astronomia, retaliation integrations, complete on-screen feedback review and stress-performance coverage.
- VerifyLaurelRuntime: 4 Play Mode checks passed with authored 10s base cooldown and a 50% cooldown bonus (effective 5s; observed completed by 5.3002s). Logs/LaurelRuntimeVerification.json contains evidence.
- VerifyLaurelChoiceGate: 3 Play Mode checks passed through actual LevelUpManager/WeaponManager APIs: level 7 offered, level 8 excluded, invalid application rejected without inventory/event change.
- Laurel milestone total: 28 checks passed; captured console errors 0. Full DOT limitations above remain open.

## Evolution prerequisite foundation (2026-09-10)

- WeaponEvolutionData supports additional independently level-gated passive requirements, retaining compatibility with existing single-passive assets.
- Missing result prefabs no longer consume evolutions/unions or change union slot accounting. Weapons already consumed by a union cannot evolve again, and unions reject already evolved ingredients.
- Tools/VerifyEvolutionRequirements.cs: 10 isolated Unity checks passed, including missing/underlevel second passive, complete prerequisites, underlevel base weapon, legacy data, missing result rejection, successful retry, duplicate rejection and union accounting/consumption.
- This is infrastructure proof with test passive types. Crimson Shroud gameplay, Metaglio content, authored evolution wiring and actual chest/Play Mode integration remain OPEN. No full DOT or performance pass is claimed.
- Laurel progression regression: 14 checks passed after this change. Captured Unity console errors: 0; Editor stopped.

## Crimson Shroud core component (2026-09-10)

- Added CrimsonShroudWeapon using Laurel's shield timing: three charges, 8s base recharge, 1s grace, level cap 1. HealthController exposes a damage modifier; Shroud caps unblocked incoming damage at 10 and restores the previous modifier on disable.
- Block consumption queues retaliation using incoming damage capped at 100, Might, Curse and Armor bonus (capped at 50 Armor). Area scales the provisional 3-unit radius; Amount adds pulses. Fixed 50-entry queue, 0.1s spacing and at most four catch-up pulses per update bound work. These bounds and world-space radius still need reference calibration.
- Retaliation damages active enemy registry targets in radius and reports observed health loss. Persistent shield uses red/orange/green charge colors. No retaliation shockwave visual yet.
- Created Assets/Prefabs/Weapons/Generated/CrimsonShroud.prefab through Unity PrefabUtility, reusing Laurel's shield visual.
- Tools/VerifyCrimsonShroud.cs: 15 isolated checks passed against the authored prefab, including charge/grace, empty-shield cap, small hits, damage scaling, paused pulses, intervals, nearby versus distant damage, recharge, queue bound and cleanup. Laurel progression regression: 14 passed. Console errors: 0.
- OPEN: actual Play Mode timing/chest evolution; Metaglio passives and acquisition; Reaper-specific 1% bonus; incoming Armor reduction integration; exact mixed-color/retaliation effects; Arcana/Limit Break; reference comparison and horde performance. This is a component milestone, NOT full Crimson Shroud DOT.
- Reference inventory: https://vampire-survivors.fandom.com/wiki/Crimson_Shroud and https://vampire-survivors.fandom.com/wiki/Retaliate . Search excerpts were reachable; direct Crimson page fetch returned 402 during this pass.

## Metaglio progression and evolution authoring (2026-09-10)

- Appended two enum values without shifting existing serialized IDs. Authored 18 upgrade assets and catalog entries. Level 1 has no stat bonus and is excluded from initial level-up acquisition. AcquireStagePassive accepts each once and bypasses the normal passive slot cap; subsequent upgrades require the next level and stop at 9.
- Left levels 2-9 add 0.1 Recovery and multiply precise Max Health by 1.05. Fractional Max Health is retained across upgrades (100 -> displayed 148 at level 9). Right adds 0.05 Curse per upgrade, ending at 1.4 multiplier.
- Added separate PlayerRecovery component to the player prefab and gameplay scene player; fractional HP accumulates using scaled time, with no healing while paused/dead/full.
- Authored CrimsonShroudEvolution (Laurel 7, Left 9, Right 9) and wired the gameplay scene WeaponManager through Unity SerializedObject. Scene saved through EditorSceneManager.
- VerifyMetaglio: 16 isolated Unity checks passed, including progression, acquisition restrictions, duplicate rejection, recovery, prerequisite gates and actual evolved prefab instantiation once. VerifyCrimsonShroud regression: 15 passed. Captured console errors before final evolution assertion: 0.
- OPEN: stage pickup objects/placement, Yellow Sign unlock and guardians, icons, actual chest Play Mode flow, Curse consumers, reference healing timing/rounding and target-device profiling. No full passive or Crimson DOT certification.
- Reference: https://vampire-survivors.fandom.com/wiki/Metaglio_Left and https://primagames.com/tips/how-to-get-metaglio-right-in-vampire-survivors .

## Crimson chest backend in Play Mode (2026-09-10)

- Started via Bootstrap/MainMenu character-selection Start button, then tested the actual gameplay WeaponManager and authored evolution list.
- Tools/VerifyCrimsonChestRuntime.cs: six checks passed. Granted Laurel 7 and acquired/upgraded both Metaglio passives through inventory APIs; ProcessChest returned Crimson Shroud, disabled the old Laurel, instantiated level-1 Shroud with three charges, intercepted a 999-damage hit while consuming one charge and queuing retaliation, and rejected a repeated evolution.
- Evidence: Logs/CrimsonChestRuntimeVerification.json. Captured console errors: 0. Requested Editor stop after verification.
- This calls ChestLogicController.ProcessChest directly with prepared inventory. Ground acquisition, physical chest pickup, chest reward UI, chest eligibility/timing and natural progression remain OPEN. Initial attempts before scene loading completed were readiness failures; no pass was claimed until gameplay was available.

## Physical chest collection fix (2026-09-10)

- Reproduced failure with the authored Chest.prefab instantiated at the actual gameplay player: chest stayed active. Inspection found chest mask 64 (Player layer 6), but the gameplay player's collider was on Default layer 0.
- Tools/FixPlayerPickupLayer.cs corrected PlayerController roots and child collider layers in Player.prefab and Main_Gameplay through Unity PrefabUtility/EditorSceneManager. No physics collision matrix changes.
- Restarted from Bootstrap/MainMenu. PreparePhysicalChestTest prepares the inventory and spawns the real chest at the player; normal frame Update/physics overlap performs collection (no direct Collect/ProcessChest call).
- VerifyPhysicalChestTest: five functional checks passed: chest deactivation, active Crimson reward panel, paused time, evolved component, Claim button closing panel/restoring time. Logs/PhysicalChestVerification.json. Console errors: 0.
- Remaining: screenshot/layout review, pooled reuse/multiple simultaneous chest behavior, full collision/pickup regression after player layer correction, natural drops/timing/eligibility and complete chest presentation parity. Prepared inventory is not natural stage progression.

## Player layer regression and chest layout (2026-09-10)

- Runtime regression reproduced a health pickup failure: contact damage reduced HP 100 -> 80, XP collected (+10), but chicken disappeared without healing. HealthChiken.prefab used mask 1 (Default), allowing its own collider to be selected. HealthPickup now validates PlayerController ancestry before accepting an overlap hit; the prefab mask was corrected to Player through Unity.
- Final VerifyPlayerLayerRuntime: three checks passed through real trigger contact and normal pickup Updates: HP 100 -> 80 -> 100 and XP +10; elapsed 0.943s. Evidence: Logs/PlayerLayerRuntimeVerification.json. This is a bounded contact/health/XP test, not all collision scenarios.
- Chest screenshot initially failed review: reward text overlapped the Claim button; an extra Open button and placeholder image were visible. Scene layout now separates responsive reward and Claim regions and disables the obsolete button/image without deleting them.
- Final Logs/ChestView.png reviewed at 1920x1080: reward is readable with no button overlap, one Claim button. Full art/presentation parity, contrast/theme polish and other resolutions remain open.
- VerifyPhysicalChestTest rerun after layout change: five functional checks passed; captured console errors 0. Requested Editor stop after evidence capture.

## Sequential chest and pool regression (2026-09-10)

- Reproduced second overlapping chest consuming and overwriting the first reward while the modal was open. ChestPickup now rejects Update/Collect while paused, inactive or already collected. Subsequent chest stays available until Claim resumes the game.
- VerifySequentialChests: six Play Mode assertions passed, including first reward, deferred second pickup, collection after Claim, reset/re-enable and two real PickupPoolManager spawn/return cycles with stable stack count and identical object reuse. Uses explicit Update invocation for deterministic same-frame ordering with real physics overlap and UI buttons.
- Evidence: Logs/SequentialChestVerification.json. Overall gate is NOT clean: random chest upgrades exposed NullReferenceException in BibleWeapon.SpawnBibles line 61, following AddComponent<TouchDamage>. Console error total was 81. No zero-error or complete chest certification claimed. Editor stop requested.
- Next blocker: Bible reward acquisition/component setup and its damage targeting, followed by repeat chest test with clean console. Chest timing/eligibility and full presentation remain open.
