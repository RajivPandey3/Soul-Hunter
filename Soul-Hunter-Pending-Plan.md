# Soul Hunter: Pending Development & Architecture Plan
**Date:** September 17, 2026  
**Project:** Soul Hunter (Unity 6000.0.36f1)  
**Location:** `D:\Unity Projects\Soul-Hunter\`  
**Status:** `ACTIVE_ROADMAP`  
**Governance:** Strict adherence to Global Engineering Constitution (Single Responsibility, Read First Match Later, Learning Comments)

---

## 1. Executive Summary

This document formalizes the pending architectural improvements, gameplay systems completion, weapon balance passes, and testing roadmaps for *Soul Hunter*. The project has achieved 100% Release Gate verification across 14 scenes; this plan focuses on completing remaining gameplay mechanics and polishing edge cases.

---

## 2. Priority 1: Architectural Refactoring & Service Decoupling

Following the Single Responsibility and "Read First, Match Later" rules, all legacy scene-search calls (`FindFirstObjectByType` / `GameObject.Find`) must be transitioned to explicit Dependency Injection or EventBus pub/sub:

### 2.1 `MERCHANT-MODAL-PAUSE-LIFECYCLE` (High Priority)
- **Problem:** When the Wandering Merchant modal opens, game time is paused (`Time.timeScale = 0`). If the modal object is disabled or destroyed unexpectedly, the game could remain permanently paused.
- **Solution:** Implement an explicit `PauseToken` or `IPauseService` ownership pattern in `MerchantUIController` ensuring `OnDisable()` and `OnDestroy()` always restore timescale cleanly.
- **Verification:** Unit test simulating modal lifecycle and asserting `Time.timeScale == 1.0f` on exit.

### 2.2 `ACHIEVEMENT-MANAGER-REFERENCE-REPAIR`
- **Problem:** `AchievementManager` currently relies on fallback scene lookups to track player progression and kill counts.
- **Solution:** Refactor `AchievementManager` to listen exclusively to `EventBus` signals (`OnEnemyKilledEvent`, `OnSoulCollectedEvent`, `OnLevelCompletedEvent`).

### 2.3 `CAMPAIGN-SCENE-CONNECTOR-REFERENCE-REPAIR`
- **Problem:** `CampaignSceneConnector` queries `FindFirstObjectByType<LevelProgressionManager>()` during scene load.
- **Solution:** Wire via `GameServices` container or establish explicit Inspector wiring via serialized fields.

---

## 3. Priority 2: Gameplay Systems & Weapon Evolutions

### 3.1 Weapon Evolutions & Unions
- **Objective:** Finalize data assets (`ScriptableObject`) and runtime evolution logic for all planned weapon pairings (e.g., Magic Wand + Spell Binder, Santa Water + Attractorb).
- **Deliverables:**
  - Create evolution data assets in `Assets/_Project/Data/Config/Weapons/Evolutions/`.
  - Add EditMode unit tests verifying that player inventory triggers union when both max-level weapon and requisite passive item are equipped.

### 3.2 Rare Pickups Prefab Wiring
- **Objective:** Complete scene wiring and fallback prefabs for rare pickups:
  - `Magnet_Pickup.prefab`: Full-screen soul shard attraction with tween animation.
  - `TimeFreeze_Pickup.prefab`: Freezes enemy animations and velocity for 5 seconds.
- **Deliverables:** Wire prefabs in `Assets/_Project/Prefabs/Pickups/` and link in `SceneRecovery.cs`.

### 3.3 Level 10 Boss (Shadow Kael) Weapon Target Retargeting
- **Objective:** Fix `SHADOW-KAEL-WEAPON-HOSTILITY-FIX`. When Shadow Kael mirrors the player's weapon loadout, ensure projectiles attack the Player Layer rather than Enemy Layer.
- **Deliverables:** Add dynamic `TargetLayerMask` property to `WeaponBase` so weapons configured on enemies target `LayerMask.NameToLayer("Player")`.

---

## 4. Priority 3: Audio, UI & Juice Polish

1. **Boss Audio Stems:**
   - Add dynamic layer transitions in `SoundManager` when Level 10 Shadow Kael enters Phase 2 (Enrage mode).
2. **Merchant UI Polish:**
   - Enhance purchase feedback in `MerchantUIController` using `UIJuice` punch scale and particle burst on upgrade purchase.
3. **HUD Boss Health Bar:**
   - Implement animated boss bar display triggered when entering Boss Arena zones.

---

## 5. Priority 4: 10-Level PlayMode Acceptance Matrix

Execute automated PlayMode validation across all 10 stages:
- **Level 1 (Crypt Entrance):** Basic swarms, movement and drop validation.
- **Level 2 (Catacombs):** Fast skeleton variants and obstacle collision.
- **Level 3 (Merchant's Outpost):** Shopkeeper spawn and non-hostile perimeter.
- **Level 4 to 6 (Mid-Tier Dungeons):** Elite waves and weapon union triggers.
- **Level 7 (Spectral Cathedrals):** Spectral intangibility and Holy damage multiplier verification.
- **Level 8 & 9 (Shadow Depths):** High-density swarm survivability.
- **Level 10 (Throne of Shadows):** Shadow Kael 2-phase encounter and victory gate.

---

## 6. Verification & Gate Strategy

Every step implemented from this roadmap will strictly require:
1. Isolated sandbox compilation with 0 errors.
2. EditMode unit tests matching the feature specification.
3. Batchmode execution of `SoulHunter-Release-Gate-v3.ps1`.
4. Merging only upon 100% Release Gate `PASS`.
