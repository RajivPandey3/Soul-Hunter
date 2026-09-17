# Requirement-Inventory Reconciliation: 10-Stage Campaign & Owner Decisions

- **Document ID:** `P1-07-reconciliation`
- **Task ID:** `P1-07-REQUIREMENT-INVENTORY-RECONCILIATION`
- **Kind:** `REQUIREMENT_SLICE`
- **Validation Level:** `STATIC`
- **Status:** Closed / Complete
- **Date:** 2026-09-17
- **Canonical Repository:** `D:\Unity Projects\Soul-Hunter`
- **Authority Precedence:** Follows [AGENTS.md](file:///D:/Unity%20Projects/Soul-Hunter/AGENTS.md#L22-L36) (Explicit Owner Decisions > Decision Records > AGENTS.md Contract > Current Implementation > Documentation).

---

## 1. Executive Summary

This document establishes the authoritative requirement-to-inventory reconciliation across all ten campaign stages of *Soul Hunter* (L01 through L10) and provides deep technical audits of the three owner-approved product requirements documented in [AGENTS.md](file:///D:/Unity%20Projects/Soul-Hunter/AGENTS.md#L90-L125):
1. **Level 3: The Forgotten Village — Wandering Merchant**
2. **Level 7: The Sea of Lost Souls — Spectral Damage Rules**
3. **Level 10: The Soul Core — Shadow Kael Mirror Loadout**

For each system, this document cross-examines the Game Design Document ([Game_Design_Document.md](file:///D:/Unity%20Projects/Soul-Hunter/Docs/Game_Design_Document.md)), the project runtime catalog ([CampaignLevelCatalog.cs](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Data/Config/CampaignLevelCatalog.cs)), the active signature director ([CampaignSignatureSystem.cs](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Core/Game/CampaignSignatureSystem.cs)), the enemy controllers ([EnemyController.cs](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/AI/Enemies/EnemyController.cs), [ShadowKaelController.cs](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/AI/Enemies/ShadowKaelController.cs)), the combat pipeline ([HealthController.cs](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Gameplay/Combat/HealthController.cs), [DamagePacket.cs](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Gameplay/Combat/DamagePacket.cs)), and existing automated tests.

---

## 2. 10-Stage Campaign Reconciliation Matrix (L01 – L10)

| Stage | Name & Catalog Signature | Authoritative Requirement (GDD & AGENTS.md) | Implementing Components & Scripts | Current Implementation Status | Automated Test Coverage | Identified Implementation Gaps & Debt |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **L01** | **The Cursed Graveyard**<br>`CursedGraveyard` | Introduction to movement, Dash/Dodge (Shift), auto-attacks, and Soul Burst ultimate explosion (Q). | [`PlayerController`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Gameplay/Player/PlayerController.cs)<br>[`CampaignSignatureSystem`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Core/Game/CampaignSignatureSystem.cs)<br>[`CampaignLevelCatalog`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Data/Config/CampaignLevelCatalog.cs) | **Implemented** | `BootstrapPlayModeTests` verifies core player bootstrap.<br>[`CampaignLevelCatalogTests`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/CampaignLevelCatalogTests.cs) (catalog assertion coverage). | In-game HUD/tutorial sequence teaching dash and ultimate triggers is unverified/unscripted. |
| **L02** | **The Dark Forest**<br>`DarkForest` | Map progressively darkens (day/night cycle); light sources (torches/bonfires) keep enemies at standard speed while dark accelerates them. | [`CampaignSignatureSystem`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Core/Game/CampaignSignatureSystem.cs)<br>[`RenderSettings`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Core/Game/CampaignSignatureSystem.cs#L92-L96) | **Partial** | [`CampaignLevelCatalogTests`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/CampaignLevelCatalogTests.cs) (catalog assertion coverage). | Black fog and ambient intensity sinusoidal dip implemented in `CampaignSignatureSystem`; interactive bonfire/torch light volumes and enemy speed proximity modifier are missing. |
| **L03** | **The Forgotten Village**<br>`ForgottenVillage` | Mysterious NPC Wandering Merchant appears mid-game to sell unique permanent upgrades for souls. *(Owner-Approved: Wandering Merchant is authoritative; Burning Village fire hazards are obsolete divergence).* | [`WanderingMerchant`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Core/Game/WanderingMerchant.cs)<br>[`CampaignSignatureSystem`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Core/Game/CampaignSignatureSystem.cs#L109-L121) | **Partial / Prototype** | [`WanderingMerchantPlayModeTests`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/PlayMode/WanderingMerchantPlayModeTests.cs) (1 PlayMode test).<br>[`CampaignLevelCatalogTests`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/CampaignLevelCatalogTests.cs) (catalog assertion coverage). | Spawns primitive Capsule at stage start rather than mid-game; sells single +50 HP potion for Gold via legacy `OnGUI` rather than permanent upgrades for souls via UI Canvas. |
| **L04** | **The Ruined Castle**<br>`RuinedCastle` | Environmental hazards: fire-breathing gargoyles and spike traps that damage both player and enemies. | [`CampaignSignatureSystem`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Core/Game/CampaignSignatureSystem.cs#L103)<br>`CampaignHazard` | **Partial** | [`CampaignLevelCatalogTests`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/CampaignLevelCatalogTests.cs) (catalog assertion coverage). | Spawns 7 primitive cylinder trap hazards (damage 14); authored gargoyle turrets and spike animation traps are missing; enemy-damage layer interaction unverified. |
| **L05** | **The Crimson Swamp**<br>`CrimsonSwamp` | Toxic poison pools draining health over time; atmospheric corruption. | [`CampaignSignatureSystem`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Core/Game/CampaignSignatureSystem.cs#L104)<br>`CampaignHazard` | **Partial** | [`CampaignLevelCatalogTests`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/CampaignLevelCatalogTests.cs) (catalog assertion coverage). | Spawns 6 primitive cylinder poison hazards (damage 6); swamp puddle decal VFX and continuous tick status effect missing. |
| **L06** | **The Frozen Peaks**<br>`FrozenPeaks` | Periodic blizzards heavily slowing player movement speed. | [`CampaignSignatureSystem`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Core/Game/CampaignSignatureSystem.cs#L128-L135)<br>[`PlayerController`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Gameplay/Player/PlayerController.cs) | **Implemented** | [`CampaignLevelCatalogTests`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/CampaignLevelCatalogTests.cs) (catalog assertion coverage). | Blizzard timer loop (12s cooldown, 4s duration, 0.55x speed multiplier) fully functional in code; blizzard screen VFX overlay and audio cues uncoupled. |
| **L07** | **The Sea of Lost Souls**<br>`SeaOfLostSouls` | Intangible ghost enemies immune to normal physical damage; damaged only by Holy, Magic, or Spectral damage. | [`EnemyController`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/AI/Enemies/EnemyController.cs)<br>[`HealthController`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Gameplay/Combat/HealthController.cs)<br>[`EnemySpawner`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/AI/Enemies/EnemySpawner.cs#L268-L278)<br>[`CampaignSignatureSystem`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Core/Game/CampaignSignatureSystem.cs#L141-L148) | **Implemented** | [`SpectralCombatTests`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/SpectralCombatTests.cs) (14 EditMode tests verifying damage type whitelist, Holy 2x vulnerability, Magic/Spectral 1x acceptance, non-spectral acceptance, and MagicWand/SantaWater integration).<br>[`CampaignLevelCatalogTests`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/CampaignLevelCatalogTests.cs) (catalog assertion coverage). | Damage whitelist fully resolved in `HealthController.cs` (discards Normal, Fire, Poison, Shadow; accepts Holy at 2x, Magic/Spectral at 1x). Blanket stage flagging and ghost intangibility/translucency VFX remain visual/authoring debt. |
| **L08** | **The Blood Arenas**<br>`BloodArenas` | Boss rush: standard swarm replaced by continuous elite/boss encounters. | [`EnemySpawner`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/AI/Enemies/EnemySpawner.cs#L138-L155)<br>[`CampaignLevelCatalog`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Data/Config/CampaignLevelCatalog.cs#L63) | **Implemented** | [`CampaignLevelCatalogTests`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/CampaignLevelCatalogTests.cs) (catalog assertion coverage). | `EnemySpawner` forces `eliteOnly = true` and `CurrentEliteChance = 1.0f`; requires configured `_stageElitePrefab` asset. Scripted boss sequence unauthored. |
| **L09** | **The Throne of Death**<br>`ThroneOfDeath` | Continuously contracting boundary forcing player into tight survival spaces; boundary deals damage. | [`CampaignSignatureSystem`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Core/Game/CampaignSignatureSystem.cs#L159-L168) | **Implemented** | [`CampaignLevelCatalogTests`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/CampaignLevelCatalogTests.cs) (catalog assertion coverage). | Radial contraction from 35m to 10m over stage duration with 10 DPS boundary damage functional in code; visual boundary wall/ring shader missing. |
| **L10** | **The Soul Core**<br>`SoulCore` | Final boss Shadow Kael mirrors player's combat loadout (weapons, dash, stats, upgrades). | [`ShadowKaelController`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/AI/Enemies/ShadowKaelController.cs)<br>[`EnemySpawner`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/AI/Enemies/EnemySpawner.cs#L187-L194)<br>[`CampaignSignatureSystem`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Core/Game/CampaignSignatureSystem.cs#L150-L157) | **Implemented** | [`ShadowKaelMirrorTests`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs) (19 EditMode tests verifying stats mirroring, weapon replication, weapon targeting inversion to Player layer/tag, passive defense armor reduction, and revival mechanics).<br>[`CampaignLevelCatalogTests`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/CampaignLevelCatalogTests.cs) (catalog assertion coverage). | Weapon targeting inversion and passive defense/revival fully resolved in `ShadowKaelController.cs`. Generic boss 3x scaled model divergence and dash/dodge mirroring remain visual/authoring debt. |

---

## 3. Deep-Dive Reconciliation: Owner-Approved Requirements

### 3.1 Level 3: Wandering Merchant

#### Authoritative Baseline
- **GDD Specification:** *"Wandering Merchant: A mysterious NPC appears mid-game to sell unique permanent upgrades for souls."*
- **AGENTS.md Section 5:** *"Authoritative level: The Forgotten Village. Signature mechanic: Wandering Merchant. Existing Burning Village / fire-hazard behavior is implementation divergence, not the authoritative requirement."*

#### Implementation Inventory
- **Spawner:** [`CampaignSignatureSystem.SpawnMerchant()`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Core/Game/CampaignSignatureSystem.cs#L109-L121)
  - Instantiates a procedural Gold Capsule primitive (`PrimitiveType.Capsule`) at `_player.transform.position + new Vector3(8f, 0f, 8f)`.
  - Sets trigger collider, registers in `_runtimeHazards` for stage teardown, and attaches [`WanderingMerchant`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Core/Game/WanderingMerchant.cs).
- **Behavior & Economy:** [`WanderingMerchant.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Core/Game/WanderingMerchant.cs)
  - Pauses gameplay on trigger contact (`Time.timeScale = 0f`).
  - Renders an immediate-mode GUI (`OnGUI`) dialog titled `"WANDERING MERCHANT"`.
  - Interfaces with [`EconomyService`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Core/Services/EconomyService.cs) to check `CurrentGold` and execute `SpendGold(50)`.
  - Restores player health via `HealthController.Heal(50)`, displays damage popup, closes shop, resets `Time.timeScale = 1f`, and destroys the GameObject.

#### Test Coverage
- **Automated Tests:** [`WanderingMerchantPlayModeTests.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/PlayMode/WanderingMerchantPlayModeTests.cs)
  - `ForgottenVillage_SpawnsWanderingMerchant()`: Starts Stage 3 on `LevelProgressionManager` and verifies that a GameObject named `"WanderingMerchant"` with component `WanderingMerchant` is instantiated in the scene.
  - **Result:** Passing PlayMode assertion for presence.

#### Technical Gaps & Deficiencies
1. **Currency Discrepancy:** The mechanic spends Gold from `EconomyService` instead of Souls collected during the run.
2. **Catalog / Upgrade Scope:** The merchant only offers a single temporary health refill (+50 HP) instead of "unique permanent upgrades" (meta-progression stat boosts, rare weapon evolutions, or passive upgrades).
3. **UI Architecture Divergence:** Uses legacy Unity `OnGUI` instead of the project-standard Canvas UI system (`SoulHunter.UI`), violating input handling and resolution scaling contracts.
4. **Timing & Spawn Trigger:** Spawns immediately upon entering the stage rather than appearing as a mid-game timed or wave-triggered event.
5. **Asset Presentation:** Relies on a procedural capsule primitive rather than an authored NPC model, idle animation, and interaction prompt.

---

### 3.2 Level 7: Spectral Damage Rules

#### Authoritative Baseline
- **GDD Specification:** *"Ghost Enemies: Intangible enemies that can only be damaged by specific weapons (like Holy Water or Magic)."*
- **AGENTS.md Section 5:**
  - Ordinary `Normal` damage must not damage ghost/spectral enemies.
  - `Holy`, `Magic`, or explicitly tagged `Spectral` damage is valid.
  - Existing Holy 2x vulnerability may remain a balance parameter unless changed by a later balance decision.

#### Implementation Inventory
- **Damage Classification:** [`DamagePacket.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Gameplay/Combat/DamagePacket.cs#L5)
  - Defines `enum DamageType { Normal, Holy, Fire, Poison, Shadow, Magic, Spectral }`.
- **Enemy State & Configuration:** [`EnemyController.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/AI/Enemies/EnemyController.cs#L27-L39)
  - Fields: `_vulnerabilityType`, `_vulnerabilityMultiplier`, `_isSpectral`.
  - Method: `ConfigureDamageVulnerability(DamageType type, float multiplier, bool isSpectral)`.
  - Property: `IsSpectral => _isSpectral`.
- **Stage Rule Injection:** [`EnemySpawner.ConfigureStageDamageRule()`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/AI/Enemies/EnemySpawner.cs#L268-L278)
  - Checks if `CurrentLevel.Signature == CampaignSignature.SeaOfLostSouls`.
  - When true, configures `isSpectral = true`, vulnerability to `DamageType.Holy`, and multiplier `2f`.
- **Damage Resolution:** [`HealthController.TakeDamage()`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Gameplay/Combat/HealthController.cs#L59-L75)
  ```csharp
  var enemy = GetComponentInParent<SoulHunter.Gameplay.AI.EnemyController>();
  if (enemy != null)
  {
      if (enemy.IsSpectral)
      {
          bool isWhitelisted = packet.Type == DamageType.Holy ||
                               packet.Type == DamageType.Magic ||
                               packet.Type == DamageType.Spectral;
          if (!isWhitelisted) return;
      }

      incomingDamage = Mathf.Max(1, Mathf.RoundToInt(incomingDamage * enemy.GetDamageMultiplier(packet.Type)));
  }
  ```

#### Test Coverage
- **Automated Tests:** [`SpectralCombatTests.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/SpectralCombatTests.cs)
  - 14 EditMode unit and integration tests verifying spectral immunity to physical/elemental damage types (`Normal`, `Fire`, `Poison`, `Shadow`), Holy 2x vulnerability, Magic/Spectral 1x acceptance, non-spectral baseline acceptance, MagicWand projectile typing/damage, and SantaWater Holy TouchDamage configuration/damage reduction.
- **Status:** **PASS** (14 passing tests in EditMode).

#### Technical Gaps & Deficiencies
1. **Damage Type Whitelist vs. Blacklist [RESOLVED]:**
   - **Resolved in [`HealthController.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Gameplay/Combat/HealthController.cs#L59-L75):** The legacy check only rejected `DamageType.Normal`. `HealthController.TakeDamage()` has been updated to enforce an explicit whitelist (`packet.Type == DamageType.Holy || packet.Type == DamageType.Magic || packet.Type == DamageType.Spectral`). Any non-whitelisted damage (`DamageType.Normal`, `DamageType.Fire`, `DamageType.Poison`, `DamageType.Shadow`) is safely discarded (`return`) without deducting enemy health. Verified by 7 core combat assertions and 7 weapon integration tests in `SpectralCombatTests.cs`.
2. **Blanket Stage Flagging:**
   - `EnemySpawner` indiscriminately flags *every* enemy spawned in Stage 7 as spectral, regardless of archetype. Standard physical enemies or destructibles should not inherit spectral immunity unless authored as ghosts.
3. **Visual Intangibility & Phasing:**
   - No shader translucency or intangible pass-through collision behavior is enabled for spectral entities.

---

### 3.3 Level 10: Shadow Kael Mirror Loadout

#### Authoritative Baseline
- **GDD Specification:** *"Mirror Match: The Final Boss (Shadow Kael) uses the exact same weapons, dash, and upgrades that the player has equipped. It requires pure skill to defeat."*
- **AGENTS.md Section 5:**
  - Boss: **Shadow Kael**.
  - Authoritative behavior: Shadow Kael mirrors the player's combat loadout. At encounter start, the relevant player weapon/stat loadout must be captured and represented by the boss according to the implementation contract.
  - The currently observed generic boss implementation is incomplete relative to this requirement.

#### Implementation Inventory
- **Boss Controller:** [`ShadowKaelController.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/AI/Enemies/ShadowKaelController.cs)
  - In `Start()`:
    - Finds `PlayerController` in scene.
    - Reads `PlayerStats`: captures `Might`, `Cooldown`, `Area`, `MoveSpeedMultiplier`, `Armor`, and `Revivals`.
    - Reads `WeaponManager`: queries `GetActiveWeaponTypes()`, instantiates corresponding weapon prefabs as children of boss transform.
    - Matches weapon level via `autoAttackWeapon.LevelUp()`.
    - Multiplies weapon damage by `MirroredMight`, cooldown by `MirroredCooldown`, and local scale by `MirroredArea`.
    - Applies movement speed via `EnemyController.ApplyCampaignSpeed(MirroredMoveSpeed)`.
    - Reconfigures weapon targeting via `ConfigureMirroredWeapon(weaponInstance)`: recursively assigns transforms to `Enemy` layer, redirects `DamageCaster.TargetLayer` to `Player` mask, sets `ProjectileDamage.TargetTag` and `TouchDamage.TargetTag` to `"Player"`, and redirects `GarlicWeapon` and `WhipWeapon` detection layers to `Player`.
    - Configures passive defenses on `HealthController`: wires `DamageModifier` to `ApplyArmorDamageReduction` (applies flat armor damage reduction clamped to minimum 1) and subscribes `HandleDeath` to `HealthController.OnDied` (decrements `MirroredRevivals` and calls `HealthController.ReviveFromDeath()`).
- **Boss Spawning & Initialization:** [`EnemySpawner.SpawnBoss()`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/AI/Enemies/EnemySpawner.cs#L187-L194)
  - When `CurrentLevel.Number == 10`, dynamically attaches `ShadowKaelController` to a 3x scaled generic enemy prefab instance.

#### Test Coverage
- **Automated Tests:** [`ShadowKaelMirrorTests.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs)
  - 19 EditMode unit and integration tests verifying stat mirroring (`Might`, `Cooldown`, `Area`, `MoveSpeedMultiplier`, `Armor`, `Revivals`), active weapon count and level replication, enemy movement speed application, weapon layer and hostile targeting inversion (`DamageCaster`, `ProjectileDamage`, `TouchDamage`, `GarlicWeapon`, `WhipWeapon`), flat armor reduction, and revival exhaustion.
- **Status:** **PASS** (19 passing tests in EditMode).

#### Technical Gaps & Deficiencies
1. **Generic Boss Prefab Divergence:**
   - As flagged in `AGENTS.md`, the boss is instantiated from a generic base enemy prefab scaled up 3x (`Vector3.one * 3f`) with `ShadowKaelController` injected at runtime, rather than utilizing a distinct Shadow Kael model with player-identical rigging, animations, and dark VFX materials.
2. **AutoAttack Weapon Targeting Inversion [RESOLVED]:**
   - **Resolved in [`ShadowKaelController.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/AI/Enemies/ShadowKaelController.cs#L153-L200):** `ConfigureMirroredWeapon()` recursively sets child GameObjects to the `Enemy` layer and reconfigures `DamageCaster.TargetLayer` to `LayerMask.GetMask("Player")`, `ProjectileDamage.TargetTag` and `TouchDamage.TargetTag` to `"Player"`, and `GarlicWeapon`/`WhipWeapon` `TargetLayer` to `LayerMask.GetMask("Player")`. Mirrored weapons now accurately target and damage the player instead of other enemies. Verified by 5 EditMode tests in `ShadowKaelMirrorTests.cs`.
3. **Missing Dash / Dodge Mirroring:**
   - Shadow Kael does not execute dashes or mirror player dash evasive mechanics.
4. **Passive Upgrade Omission [RESOLVED]:**
   - **Resolved in [`ShadowKaelController.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/AI/Enemies/ShadowKaelController.cs#L38-L40):** `Start()` captures `PlayerStats.Armor` and `PlayerStats.Revivals` into `MirroredArmor` and `MirroredRevivals`. Wires `ApplyArmorDamageReduction` into `HealthController.DamageModifier` (reducing incoming damage by armor, clamped to minimum 1) and subscribes `HandleDeath` to `HealthController.OnDied` to invoke `HealthController.ReviveFromDeath()` when revivals remain. Verified by 6 EditMode tests in `ShadowKaelMirrorTests.cs`.
5. **Lifecycle / Race Conditions:**
   - Stat reading occurs in `Start()`. If boss pooling is utilized or if the player changes stats/weapons at the instant of boss spawn, captured stats may desynchronize.

---

## 4. Test Coverage Summary & Verification Plan

### 4.1 Current Test Inventory Status

| Target Area | Test File | Test Method | Type | Result |
| :--- | :--- | :--- | :--- | :--- |
| **Level 3 Merchant** | [`WanderingMerchantPlayModeTests.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/PlayMode/WanderingMerchantPlayModeTests.cs) | `ForgottenVillage_SpawnsWanderingMerchant` | PlayMode | **PASS** |
| **Level 7 Spectral Rules** | [`SpectralCombatTests.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/SpectralCombatTests.cs) | `TakeDamage_SpectralEnemy_DiscardsNonWhitelistedDamageTypes`<br>`TakeDamage_SpectralEnemy_DiscardsNormalFirePoisonAndShadowDamage`<br>`TakeDamage_SpectralEnemy_AppliesTwoFoldVulnerabilityForHolyDamage`<br>`TakeDamage_SpectralEnemy_AcceptsMagicAndSpectralDamageAtOneXMultiplier`<br>`TakeDamage_SpectralEnemy_AcceptsMagicAndSpectralDamageTypes`<br>`TakeDamage_NonSpectralEnemy_AcceptsPhysicalAndElementalDamageTypes`<br>`TakeDamage_NonSpectralEnemy_AcceptsNormalFirePoisonAndShadowDamage`<br>`MagicWandWeapon_FiresProjectile_InitializesWithMagicDamageType`<br>`MagicWandWeapon_Projectile_ReducesSpectralEnemyHealth`<br>`Projectile_NormalDamage_DiscardedBySpectralEnemyWithoutReducingHealth`<br>`SpectralEnemy_AcceptsMagicWandProjectiles_WhileDiscardingNormalProjectiles`<br>`SantaWaterWeapon_SpawnWaterZone_ConfiguresTouchDamageWithHolyDamageAndEnemyTargetTag`<br>`SantaWaterWeapon_WaterZoneContact_ReducesSpectralEnemyHealthByHolyVulnerabilityMultiplier`<br>`SpectralEnemy_DiscardsNormalTouchDamage_WhileAcceptingSantaWaterHolyDamage` | EditMode | **PASS** |
| **Level 10 Mirror Loadout**| [`ShadowKaelMirrorTests.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs) | `Start_CapturesPlayerStats_IntoMirroredProperties`<br>`Start_DefaultPlayerStats_MirroredAccurately`<br>`Start_CapturesWeaponCountAndReplicatesActiveWeaponLevel_SingleWeapon`<br>`Start_CapturesWeaponCountAndReplicatesActiveWeaponLevels_MultipleWeapons`<br>`Start_WithZeroActiveWeapons_MirroredWeaponCountIsZero`<br>`Start_AppliesMirroredStatsMultipliers_ToInstantiatedWeapon`<br>`Start_AppliesMirroredMoveSpeed_ToAttachedEnemyControllerViaApplyCampaignSpeed`<br>`Start_DefaultMoveSpeed_PreservesBaseEnemySpeed`<br>`ConfigureMirroredWeapon_AssignsRootAndNestedChildGameObjects_ToEnemyLayer`<br>`ConfigureMirroredWeapon_SetsDamageCasterTargetLayer_ToPlayerLayerMask`<br>`ConfigureMirroredWeapon_SetsProjectileDamageTargetTag_ToPlayer`<br>`ConfigureMirroredWeapon_SetsTouchDamageTargetTag_ToPlayer`<br>`ConfigureMirroredWeapon_SetsGarlicWeaponAndWhipWeaponTargetLayer_ToPlayerLayerMask`<br>`Start_CapturesPlayerPassiveStats_MirrorsArmorAndRevivalsMatchingPlayerStats`<br>`ApplyArmorDamageReduction_ReducesIncomingDamageByArmor_AndClampsToMinimumOfOne`<br>`TakeDamage_AttachedHealthController_AppliesFlatArmorDamageReductionViaDamageModifier`<br>`TakeDamage_LethalDamage_TriggersHandleDeathToDecrementRevivalsAndRestoreHealthViaReviveFromDeath`<br>`TakeDamage_WhenMirroredRevivalsReachesZero_LethalDamageCausesStandardDeathAndDeactivatesGameObject`<br>`TakeDamage_WhenMirroredRevivalsDepletedToZero_SubsequentLethalDamageCausesStandardDeathAndDeactivatesGameObject` | EditMode | **PASS** |
| **Campaign Catalog** | [`CampaignLevelCatalogTests.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/CampaignLevelCatalogTests.cs) | `Count_EqualsTenStages`<br>`AllStages_HaveUniqueCampaignSignatures`<br>`AllStages_HaveNonEmptyNameAndNarrativeStrings`<br>`AllStages_HavePositiveSpeedAndSpawnMultipliers`<br>`Get_ClampsIndexZeroToStageOne_AndIndexElevenToStageTen` | EditMode | **PASS** |

### 4.2 Verified Test Suites & Method Inventories

1. **[`SpectralCombatTests.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/SpectralCombatTests.cs) (EditMode) [VERIFIED / COMPLETE]:**
   - **Verification Status:** Complete.
   - Deterministic assertion coverage for Level 7 (The Sea of Lost Souls) spectral combat rules and damage type whitelist resolution.
   - Implemented in [`SpectralCombatTests.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/SpectralCombatTests.cs) with 14 passing EditMode tests:
     - [`TakeDamage_SpectralEnemy_DiscardsNonWhitelistedDamageTypes`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/SpectralCombatTests.cs#L87-L99): Verifies spectral enemy discards `DamageType.Normal`, `DamageType.Fire`, `DamageType.Poison`, and `DamageType.Shadow` without health reduction (TestCase coverage across all rejected types).
     - [`TakeDamage_SpectralEnemy_DiscardsNormalFirePoisonAndShadowDamage`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/SpectralCombatTests.cs#L102-L118): Single-test sequence verifying all non-whitelisted damage types are discarded.
     - [`TakeDamage_SpectralEnemy_AppliesTwoFoldVulnerabilityForHolyDamage`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/SpectralCombatTests.cs#L130-L143): Verifies `DamageType.Holy` applies 2.0x vulnerability multiplier damage to spectral enemies.
     - [`TakeDamage_SpectralEnemy_AcceptsMagicAndSpectralDamageAtOneXMultiplier`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/SpectralCombatTests.cs#L156-L168): Verifies `DamageType.Magic` and `DamageType.Spectral` are accepted at 1.0x unmitigated damage (TestCase coverage).
     - [`TakeDamage_SpectralEnemy_AcceptsMagicAndSpectralDamageTypes`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/SpectralCombatTests.cs#L171-L186): Sequential acceptance verification of Magic and Spectral damage on spectral enemies.
     - [`TakeDamage_NonSpectralEnemy_AcceptsPhysicalAndElementalDamageTypes`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/SpectralCombatTests.cs#L201-L213): Verifies non-spectral enemies accept Normal, Fire, Poison, and Shadow damage unmitigated (TestCase coverage).
     - [`TakeDamage_NonSpectralEnemy_AcceptsNormalFirePoisonAndShadowDamage`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/SpectralCombatTests.cs#L216-L228): Single-test sequence verifying physical and elemental damage acceptance on standard enemies.
     - [`MagicWandWeapon_FiresProjectile_InitializesWithMagicDamageType`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/SpectralCombatTests.cs#L329-L350): Verifies MagicWand weapon initializes fired projectiles with `DamageType.Magic`.
     - [`MagicWandWeapon_Projectile_ReducesSpectralEnemyHealth`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/SpectralCombatTests.cs#L358-L375): Verifies MagicWand projectile hit successfully reduces spectral enemy health by base damage.
     - [`Projectile_NormalDamage_DiscardedBySpectralEnemyWithoutReducingHealth`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/SpectralCombatTests.cs#L384-L396): Verifies standard Normal projectile is discarded by spectral enemy without health reduction.
     - [`SpectralEnemy_AcceptsMagicWandProjectiles_WhileDiscardingNormalProjectiles`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/SpectralCombatTests.cs#L404-L428): Verifies differential projectile processing (Magic accepted, Normal discarded) on spectral target.
     - [`SantaWaterWeapon_SpawnWaterZone_ConfiguresTouchDamageWithHolyDamageAndEnemyTargetTag`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/SpectralCombatTests.cs#L538-L562): Verifies SantaWaterWeapon spawns water zone configured with `DamageType.Holy`, `TargetTag="Enemy"`, and `SourceWeaponName="Santa Water"`.
     - [`SantaWaterWeapon_WaterZoneContact_ReducesSpectralEnemyHealthByHolyVulnerabilityMultiplier`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/SpectralCombatTests.cs#L572-L592): Verifies SantaWater water zone reduces spectral enemy health by `Mathf.RoundToInt(DamageAmount * 2f)`.
     - [`SpectralEnemy_DiscardsNormalTouchDamage_WhileAcceptingSantaWaterHolyDamage`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/SpectralCombatTests.cs#L601-L629): Verifies spectral enemy discards Normal TouchDamage while accepting SantaWater Holy damage with 2x multiplier.

2. **[`ShadowKaelMirrorTests.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs) (EditMode) [VERIFIED / COMPLETE]:**
   - **Verification Status:** Complete.
   - Deterministic assertion coverage for Level 10 (The Soul Core) Shadow Kael mirror match boss loadout, weapon replication, weapon targeting inversion, and passive defense/revival mechanics.
   - Implemented in [`ShadowKaelMirrorTests.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs) with 19 passing EditMode tests:
     - [`Start_CapturesPlayerStats_IntoMirroredProperties`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs#L129-L153): Verifies `Might`, `Cooldown`, `Area`, `MoveSpeedMultiplier`, `Armor`, and `Revivals` are captured from `PlayerStats`.
     - [`Start_DefaultPlayerStats_MirroredAccurately`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs#L160-L170): Verifies baseline player stats (1.0f multipliers, 0 armor/revivals) mirror accurately.
     - [`Start_CapturesWeaponCountAndReplicatesActiveWeaponLevel_SingleWeapon`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs#L182-L204): Verifies single active weapon count and weapon level replication on child `AutoAttackWeapon`.
     - [`Start_CapturesWeaponCountAndReplicatesActiveWeaponLevels_MultipleWeapons`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs#L212-L240): Verifies multiple active weapons replicate exact counts and respective levels.
     - [`Start_WithZeroActiveWeapons_MirroredWeaponCountIsZero`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs#L247-L254): Verifies zero active weapons results in 0 count and 0 child weapons.
     - [`Start_AppliesMirroredStatsMultipliers_ToInstantiatedWeapon`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs#L262-L283): Verifies stat multipliers (`Might`, `Cooldown`, `Area`) scale damage, cooldown, and local scale of replicated weapons.
     - [`Start_AppliesMirroredMoveSpeed_ToAttachedEnemyControllerViaApplyCampaignSpeed`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs#L295-L307): Verifies `EnemyController.MoveSpeed` is scaled by `MirroredMoveSpeed` via `ApplyCampaignSpeed`.
     - [`Start_DefaultMoveSpeed_PreservesBaseEnemySpeed`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs#L314-L322): Verifies default 1.0x move speed preserves baseline enemy speed.
     - [`ConfigureMirroredWeapon_AssignsRootAndNestedChildGameObjects_ToEnemyLayer`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs#L335-L369): Verifies root and all nested child GameObjects are recursively assigned to the `Enemy` layer.
     - [`ConfigureMirroredWeapon_SetsDamageCasterTargetLayer_ToPlayerLayerMask`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs#L378-L412): Verifies all `DamageCaster.TargetLayer` properties are redirected to `LayerMask.GetMask("Player")`.
     - [`ConfigureMirroredWeapon_SetsProjectileDamageTargetTag_ToPlayer`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs#L420-L446): Verifies `ProjectileDamage.TargetTag` is reconfigured to `"Player"`.
     - [`ConfigureMirroredWeapon_SetsTouchDamageTargetTag_ToPlayer`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs#L454-L480): Verifies `TouchDamage.TargetTag` is reconfigured to `"Player"`.
     - [`ConfigureMirroredWeapon_SetsGarlicWeaponAndWhipWeaponTargetLayer_ToPlayerLayerMask`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs#L489-L524): Verifies detection masks for `GarlicWeapon` and `WhipWeapon` target the `Player` layer.
     - [`Start_CapturesPlayerPassiveStats_MirrorsArmorAndRevivalsMatchingPlayerStats`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs#L536-L552): Verifies passive `Armor` and `Revivals` are mirrored from `PlayerStats`.
     - [`ApplyArmorDamageReduction_ReducesIncomingDamageByArmor_AndClampsToMinimumOfOne`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs#L560-L583): Verifies flat armor reduction logic and clamping to a minimum of 1 damage.
     - [`TakeDamage_AttachedHealthController_AppliesFlatArmorDamageReductionViaDamageModifier`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs#L591-L615): Verifies `HealthController.DamageModifier` integration reduces incoming damage by mirrored armor.
     - [`TakeDamage_LethalDamage_TriggersHandleDeathToDecrementRevivalsAndRestoreHealthViaReviveFromDeath`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs#L625-L653): Verifies lethal damage decrements `MirroredRevivals` and triggers `HealthController.ReviveFromDeath()`, keeping GameObject active.
     - [`TakeDamage_WhenMirroredRevivalsReachesZero_LethalDamageCausesStandardDeathAndDeactivatesGameObject`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs#L661-L688): Verifies lethal damage when `MirroredRevivals == 0` deactivates the GameObject.
     - [`TakeDamage_WhenMirroredRevivalsDepletedToZero_SubsequentLethalDamageCausesStandardDeathAndDeactivatesGameObject`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/ShadowKaelMirrorTests.cs#L697-L725): Verifies multi-stage lethal hits consume revivals first, and subsequent lethal hits at 0 revivals trigger deactivation.
3. **[`CampaignLevelCatalogTests.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/CampaignLevelCatalogTests.cs) (EditMode) [VERIFIED / COMPLETE]:**
   - **Verification Status:** Complete.
   - Deterministic assertion coverage across all 10 stages (L01 through L10) validating canonical stage count (10), unique campaign signatures, non-empty names/narratives, positive speed/spawn multipliers, and boundary clamping.
   - Implemented in [`CampaignLevelCatalogTests.cs`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/CampaignLevelCatalogTests.cs) with 5 passing EditMode tests:
     - [`Count_EqualsTenStages`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/CampaignLevelCatalogTests.cs#L13-L18): Verifies exactly 10 canonical stages exist.
     - [`AllStages_HaveUniqueCampaignSignatures`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/CampaignLevelCatalogTests.cs#L20-L33): Verifies each stage has a unique `CampaignSignature`.
     - [`AllStages_HaveNonEmptyNameAndNarrativeStrings`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/CampaignLevelCatalogTests.cs#L35-L45): Verifies every stage defines non-empty display name and lore narrative.
     - [`AllStages_HavePositiveSpeedAndSpawnMultipliers`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/CampaignLevelCatalogTests.cs#L47-L57): Verifies speed and spawn multipliers are strictly positive.
     - [`Get_ClampsIndexZeroToStageOne_AndIndexElevenToStageTen`](file:///D:/Unity%20Projects/Soul-Hunter/Assets/_Project/Tests/EditMode/CampaignLevelCatalogTests.cs#L59-L75): Verifies out-of-range boundary clamping (0 clamped to 1, 11 clamped to 10).

---

## 5. Architectural Alignment & Safety Guidelines

1. **Strict Adherence to AGENTS.md:**
   - Do not re-introduce the obsolete Burning Village fire-hazard design for Level 3.
   - Preserve Holy 2x vulnerability as a balance parameter for Level 7.
   - Maintain the requirement that Level 10 Shadow Kael mirrors combat loadout.
2. **Dependency Direction:**
   - Ensure all merchant UI updates strictly follow `Gameplay -> UI` or decouple via `EventBus` and `EconomyService`.
3. **Single Responsibility:**
   - Keep hazard spawning, merchant transactions, and signature environment modifications decoupled into dedicated modular components.
