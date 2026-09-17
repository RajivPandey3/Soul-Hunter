# Soul-Hunter Survivor Systems Specification

**Status:** Gameplay baseline  
**Source:** Soul-Hunter GDD and the approved Hybrid/loose-coupling architecture  
**Asset policy:** All art, audio, names, lore, UI styling, and VFX authored for Soul-Hunter

Soul-Hunter will deliver a complete survivor-gameplay loop familiar to players of the genre. The implementation will be original Soul-Hunter code and data. It will not copy proprietary Vampire Survivors source code, artwork, audio, names, or files.

## Coverage contract

The following systems are required for the complete 0–100% survivor loop. Each system is an independent owner and communicates through services, events, state interfaces, or explicit data models.

| Area | Required behavior | Soul-Hunter identity |
|---|---|---|
| Player movement | Eight-direction movement, facing, collision, slow effects, dash, invulnerability window | Kael's movement and Soul Reap dash |
| Automatic weapons | Timed attacks without an attack button, target selection, aim rules, projectile and area weapons | Soul weapons and Death-God pact powers |
| Weapon stats | Damage, cooldown, area, speed, duration, amount, knockback, piercing, critical hits | Upgrade data is authored for Soul-Hunter |
| Passive upgrades | Max health, armor, speed, might, magnet, luck, cooldown, area, duration, greed | Permanent and run-only soul upgrades |
| Experience | Enemies drop soul fragments; collection radius, magnet pull, pickup feedback, level threshold | Soul count and progression are the primary currency of the run |
| Level-up choice | Pause the run, present weighted choices, reroll/banish/skip hooks, apply one choice safely | Kael chooses which part of the pact to strengthen |
| Weapon evolution | Max-level weapon plus required passive creates an evolved weapon | Evolution names and effects belong to Soul-Hunter |
| Enemy ecosystem | Melee, ranged, flying, elite, ghost, swarm and boss behaviors | Ten level-specific underworld factions |
| Waves | Time-based spawn schedules, density ramp, elite timing, boss trigger, cleanup between stages | Each level has its own wave data and narrative pressure |
| Bosses | Large health, phases, telegraphed attacks, rewards, arena control, death event | Death-God guardians and Shadow Kael |
| Drops | Soul fragments, gold, health, magnet, time freeze, chest and temporary power drops | Drops are presented as souls, relics and pact rewards |
| Chests | Open animation, reward roll, weapon/passive rewards, evolution result, event notification | Relics reveal Kael's past and the pact's cost |
| Damage model | Health, armor, invulnerability frames, knockback resistance, status effects and death events | Corruption, poison, freeze and soul damage |
| Runtime composition | Spawners and factories create temporary enemies, projectiles, drops and effects; pools reuse them | Scene stays small while the horde scales |
| Map | Arena bounds, camera follow, scrolling/infinite environment, obstacles and level modifiers | Graveyard, forest, village, castle, swamp, peaks, sea, arenas, throne and Soul Core |
| Level modifiers | Darkness, lights, traps, poison, blizzard slow, intangible ghosts, boss-only rush, shrinking arena | The GDD mechanic for each of the ten levels |
| Presentation | HUD health, soul count, timer, level, weapon slots, passives, damage feedback, level-up and game-over screens | Gothic Soul-Hunter visual language and narrative text |
| Audio/VFX | Hit, death, pickup, level-up, evolution, dash, boss, ambient and story cues | Original sound and visual assets only |
| Run lifecycle | Bootstrap, loading, menu, character selection, run start, pause, stage clear, victory, death, restart | The ten-level pact journey |
| Persistence | Settings, selected character, meta upgrades, unlocked content and best results | The daughter, souls harvested and pact consequences persist |
| Meta progression | Spend earned souls between runs, unlock characters/weapons/relics, preserve milestones | Death-God economy and long-term corruption choices |
| Accessibility | Rebindable controls, screen shake toggle, damage number toggle, color/readability options, pause behavior | Settings are owned by the presentation layer |
| Verification | Scene checks, missing reference checks, pool checks, deterministic wave data checks, compile/build checks and playtest checklist | Every release records evidence against this table |

## Ten-level campaign

| Level | Narrative and mechanic | Gameplay implementation |
|---|---|---|
| 1 — Cursed Graveyard | Kael starts beside his daughter's grave. | Dash/dodge and Soul Burst unlock; wandering souls teach movement, auto-attacks and pickups. |
| 2 — Dark Forest | The forest rejects Kael's presence. | Day/night darkness, bonfires and torches; enemies accelerate in darkness. |
| 3 — Forgotten Village | Burned villagers reveal the pact's first lie. | Wandering Merchant sells permanent upgrades for harvested souls. |
| 4 — Ruined Castle | The Gatekeeper warns Kael about deception. | Fire gargoyles and spike traps damage enemies and player. |
| 5 — Crimson Swamp | Kael's soul begins to rot. | Poison pools drain health and leave corruption effects. |
| 6 — Frozen Peaks | Cold mirrors Kael's fading humanity. | Periodic blizzards slow movement and change safe positioning. |
| 7 — Sea of Lost Souls | The lost dead erode Kael's sanity. | Ghost enemies require Holy Water, magic or another tagged spectral damage source. |
| 8 — Blood Arenas | The Death God's elite guards test Kael. | Boss rush: elite waves replace ordinary enemies. |
| 9 — Throne of Death | Kael reaches the 99,999th soul. | Closing arena continuously shrinks while the throne guardian attacks. |
| 10 — Soul Core | The mirror reveals Kael as the Death God. | Shadow Kael mirrors the equipped weapon/stat loadout and becomes the final skill check. |

## Implementation boundaries

`Player_Entity` and `Enemy_Entity` remain the canonical base templates. Weapon, pickup, projectile, VFX and boss variants are data-driven runtime compositions or supporting assets; their existence does not change the two-template entity rule.

The Foundation owns lifecycle, services, persistence, input adapters and event contracts. Gameplay owns the run loop, combat, waves, upgrades and level rules. Presentation owns HUD, menus, camera, audio, VFX and narrative display. The UI observes gameplay events and does not directly decide damage, rewards or progression.

## Completion rule

“100% complete” means every row in the coverage table has an implemented owner, authored Soul-Hunter data, a verified scene/runtime path, and a recorded test result. Similarity to the survivor genre is a behavior target; proprietary Vampire Survivors assets and implementation are not project dependencies.

## Parity gate

Before a release is called feature-complete, the team must create a reference-version inventory and map every observed gameplay behavior to one row in this specification. The inventory must include movement responses, attack targeting and timing, weapon stat interactions, passive stacking, level-up weighting, evolution prerequisites, enemy spawning and status rules, drops, chest outcomes, stage modifiers, boss phases, pause/restart behavior, UI transitions, audio/VFX triggers, persistence, and accessibility settings.

No behavior may be marked complete from a name or a screenshot alone. It needs a Soul-Hunter owner, data, runtime path, and a repeatable test. Any behavior that has no mapping becomes a new reviewed requirement before implementation continues.

The target is complete survivor-gameplay behavior with original Soul-Hunter code, assets, names, presentation, and narrative. A proprietary file, asset, effect, or source implementation is never treated as a project dependency.
