# SoulHunter Autopilot: Pending Roadmap & Action Plan
**Date:** September 17, 2026  
**System:** SoulHunter Autopilot Engine (v2.2.42)  
**Location:** `D:\SoulHunter-Automation\`  
**Target:** `D:\Unity Projects\Soul-Hunter\`  
**Status:** `ACTIVE_BACKLOG`

---

## 1. Executive Summary

This plan outlines the pending operational tasks, blocked task resolutions, and architectural enhancements for the **SoulHunter Autopilot** multi-agent automation platform. All tasks directly coordinate with `objective-ledger.json` and `task-ledger.json`.

---

## 2. Blocked Tasks Resolution Pipeline (from `task-ledger.json`)

Currently, 6 tasks in the Autopilot task ledger are in `TASK_BLOCKED` state. Resolving their prerequisites will restore full autonomous flow:

### 2.1 Task: `SHADOW-KAEL-WEAPON-HOSTILITY-FIX`
- **Objective:** Add `TargetLayer` / `TargetTag` dynamic setters to weapon scripts so mirrored weapons spawned by Shadow Kael (Boss) accurately acquire the player instead of enemies.
- **Prerequisite:** Audit existing `WeaponBase` and projectile emitters in `SoulHunter.Gameplay`.
- **Action:** Expose polymorphic target filter (`SetTargetLayerMask()`) to ensure clean dependency injection without violating Single Responsibility.

### 2.2 Task: `WIRE-VFX-POOL-MANAGER-AUTO-PERSIST` & `WIRE-VFX-POOL-MANAGER-STANDALONE`
- **Objective:** Ensure `VFXPoolManager` automatically persists across scene loads via `DontDestroyOnLoad` or `BootstrapInstaller` registration.
- **Prerequisite:** Inspect `Bootstrap.unity` scene and persistent service provider.
- **Action:** Implement safe singleton/service locator check without duplicate scene instances.

### 2.3 Task: `WIRE-PICKUPS-SCENE-RECOVERY`
- **Objective:** Update `SceneRecovery.cs` to assign fallback references for `Magnet_Pickup.prefab` and `TimeFreeze_Pickup.prefab`.
- **Prerequisite:** Verify prefab GUIDs in `Assets/_Project/Prefabs/Pickups/`.
- **Action:** Ensure procedural fallback instantiation if scene references are null.

### 2.4 Task: `CAMPAIGNSIGNATURE-DEPENDENCY-INJECTION-FIX`
- **Objective:** Refactor `CampaignSignatureSystem` to remove runtime `FindFirstObjectByType` lookups.
- **Action:** Wire direct reference via `LevelProgressionManager` or `EventBus`.

---

## 3. Active Objectives Integration (from `objective-ledger.json`)

Autopilot's scheduler is queued to execute the following 3 active objectives upon next resume:

1. **`ACHIEVEMENT-MANAGER-REFERENCE-REPAIR`**
   - Refactor `AchievementManager` to eradicate expensive runtime `FindFirstObjectByType` lookups.
   - Inject service via `ServiceContainer` or subscribe to `PlayerExperience.OnLevelChanged` / `EnemyBase.OnDeath` events.

2. **`CAMPAIGN-SCENE-CONNECTOR-REFERENCE-REPAIR`**
   - Refactor `CampaignSceneConnector` to decouple from scene hierarchy searches.
   - Connect through `GameServices.GetService<LevelProgressionManager>()`.

3. **`MERCHANT-MODAL-PAUSE-LIFECYCLE`**
   - Make wandering merchant modal pause ownership explicit.
   - Guarantee that disabling, destroying, or closing the UI cleanly restores `Time.timeScale` and input maps.

---

## 4. Automation Engine Technical Upgrades

### 4.1 Release Gate v3 Enhancements
- **Roslyn Linter Stage:** Add C# static analysis to the release gate runner to enforce:
  - No `GameObject.Find` or `FindFirstObjectByType` in hot loops (`Update`, `FixedUpdate`).
  - Strict compliance with `[SerializeField]` naming conventions (`_camelCase`).
  - Zero compiler warnings (`CS0168`, `CS0219`, `CS0414`).

### 4.2 Automated Headless PlayMode Telemetry
- Implement an automated 60-second headless survivor run in batchmode.
- Profile garbage collector allocations, frame times, and object pool recycle rates.

### 4.3 Quota Probe Dynamic Tiering
- Expand `LiveQuotaProbe.psm1` to monitor per-minute token rates and automatically throttle before hitting hard provider ceilings.
