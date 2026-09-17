# Soul-Hunter Architecture Baseline

**Status:** Working baseline  
**Authority:** [SHRS Engineering Constitution](../../../SHRS%20Standards%20Repository/docs/CONSTITUTION.md)  
**Game identity:** Vampire Survivors-style survival gameplay with Soul-Hunter narrative

## 1. Product definition

Soul-Hunter uses a survivor-gameplay foundation: automatic weapons, enemy waves, experience gems, level-up choices, weapon evolution, survival stages, bosses, pooling, and runtime spawning.

The original layer is the Soul-Hunter narrative: Kael harvests souls to restore his daughter, the Death God controls the pact and its consequences, and the final reveal turns Kael's corrupted self into the final adversary.

The gameplay foundation supplies the loop. The narrative layer supplies the characters, world, progression meaning, presentation, and ending. Neither layer should bypass the other's ownership boundaries.

## 2. Runtime architecture

Soul-Hunter follows a Hybrid System:

- **OOP / MonoBehaviours:** Unity scene integration, bootstrap, presentation, input adapters, entity-facing components, and lifecycle boundaries.
- **Data-oriented structures:** event payloads, state data, reusable query buffers, pooled runtime data, and high-volume calculations where profiling justifies them.
- **Loose coupling:** systems communicate through service contracts, events, state interfaces, and explicit data models. UI observes gameplay state; it does not own gameplay decisions.
- **Runtime composition:** the scene contains the stable bootstrap and required authored entry points. Waves, projectiles, pickups, effects, and temporary combat objects are created or acquired at runtime and returned to pools where appropriate.

## 3. Entity templates and assets

`Player_Entity` and `Enemy_Entity` are the canonical base entity templates. A template defines the reusable entity contract: required components, controller, health, environment interaction, visual binding, and damage boundaries.

Supporting prefabs and ScriptableObjects are implementation assets. Their count is not itself an architecture rule. Each asset must have a named owner, a single responsibility, and a verified reference or runtime creation path before it is consolidated or retired.

Runtime-created objects must be configured through explicit factories, spawners, or pool managers. They must not create hidden global ownership or bypass the service/event boundaries.

## 4. System ownership

- **Foundation:** bootstrap, services, persistence, scenes, input adapters, and event contracts.
- **Gameplay:** player/enemy behavior, combat, weapons, waves, progression, pickups, pooling, and game rules.
- **Presentation:** HUD, menus, camera, audio, VFX, and narrative-facing display.
- **Editor:** validation, setup, migration, and diagnostic tools; editor code must not become a runtime dependency.

Every new class, asset, or scene object must be assigned to one ownership area before implementation.

## 5. Change and cleanup policy

The Constitution lifecycle applies: need → requirement → design → architecture review → implementation → verification → merge → release.

Cleanup is evidence-based. First classify and trace references, then arrange or quarantine unused candidates, then verify the game in Unity. Permanent deletion is the last step and requires a recorded ownership decision. Existing assets and `.meta` files remain preserved until that process is complete.

## 6. Acceptance checks

An architecture change is ready for review when:

1. its owner and boundary are documented;
2. its dependencies point in the approved direction;
3. runtime-created objects have an explicit creation or pooling owner;
4. UI and narrative presentation observe gameplay contracts rather than modifying them directly;
5. Unity scene, prefab, and build references are verified;
6. managed compilation and relevant runtime tests pass;
7. the change has a traceable review record.
