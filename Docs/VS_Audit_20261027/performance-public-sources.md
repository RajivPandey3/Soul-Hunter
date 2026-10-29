# VS performance under many enemies and kills: public sources (2026-10-29)

Owner: "pahle 1 karo phir 2" (public-source audit first, then the balance method). Only public pages were read: poncle's
Steam announcements (Steam news API, app 1794680), the wiki's raw pages and press articles. No game file, executable,
decompiled code or datamined page was opened (no-copy rule; reading VS's files is against its licence).

Quotes are exact as fetched; "(summary)" marks a paraphrase where the fetch tool shortened a long paragraph.

## 1. What poncle changed for performance, by date

| Date | Post | Quote |
|---|---|---|
| 2022-03-24 | Development Roadmap Overview | "the real bottleneck are the physics calculations"; "the game's performance dipping to an embarassing slideshow"; (summary) physics ran on one CPU core |
| 2022-04-05 | Patch 0.4.2 | "New entry in the Options menu "Less physics, more FPS"." (Automaton, 2022-04-06: enabled by default) |
| 2022-06-23 | Patch 0.7.3 | "the physics were too accurate and realistic"; "rewriting the entire physics system so that it feels arcade-y" |
| 2023-01-03 | 2022 wrap up | "Our first version of the new engine was literally 10 times more performing"; "we started over and focused on fidelity rather than performance."; "the gain in performance is not as significant as x10 anymore" |
| 2023-08-17 | v1.6.0 | "Optimised Thunder Loop Projectile to reduce hitch after evolving."; "Optimised in-game stats panel."; "Added "Disable Blood" option."; new V-Sync, FPS Cap, Refresh Rate settings |
| 2023-10-19 | Patch 1.7.0 | "Minor optimisation on damage number display."; "Added bounds on death particles."; "Most of the performance gains we've seen with the swap are in fact thanks to dropping the old tech stack" |
| 2024-02-29 | Patch 1.9.0 | "Optimisation for spawning of gems and coins"; "Optimisation for weapons that interact with pickups"; "Optimisation for Shadow Servant at high Amount" |
| 2026-06-06 | Update 1.15 | "Major optimization to enemy spawning"; "General memory and performance optimizations"; "Fixed Neo Galuga stage lagging when alien centipede spawns" |
| 2026-08-28 | 1.16.105 | "Fixed damage numbers glitching at low FPS and improved their perfomance" |
| 2026-10-16 | 1.16.111 | "Performance improvements for late-game frame drops" |

None of the posts says how these optimisations work.

## 2. Deaths and dead bodies

- "Added bounds on death particles." (1.7.0): VS caps its death particles.
- Wiki Enemies: "When 300 or more enemies are alive the game will not spawn more enemies periodically, and only bosses and
  enemies from map events can spawn up until 500 enemies are alive, at which point no enemies will spawn."
- Wiki enemy pages show death animations (Skeleton: a "Death animations" gallery) but give no rule for many deaths at once.
- **No source** on whether dead bodies are pooled, capped or handled over several frames.

## 3. Options that trade looks for speed

- "Less physics, more FPS" (0.4.2); no source says whether it survived the 2023 engine change.
- Disable Blood, Damage Numbers and Flashing VFX exist, but poncle presents them as preferences, not as performance options.

## 4. What this means for Soul Hunter

| VS did | Soul Hunter today |
|---|---|
| Called physics its real bottleneck and made it "arcade-y" | Enemy bodies are triggers; the Enemy layer does not meet itself (2026-10-13); weapon zones have their own layer (2026-10-17); enemy bullets have no physics (2026-10-18) |
| Capped death particles | Only the first 32 deaths of a frame get their look (`EnemyDrop.ShownDeathsPerFrame`, 2026-10-13) |
| Optimised damage numbers twice | At most 25 numbers start per frame, drawn in one batch (2026-10-17, 2026-10-15) |
| Optimised gem and coin spawning | The gem pool fills to the 400 cap before a wipe (2026-10-19) |
| Optimised spawning (1.15) | Map-event members come 16 a frame, at most 2 ms a frame (2026-10-29) |

What VS leaves open, Soul Hunter decides by the balance method: putting a killed enemy's body back in its pool
(`SetActive(false)`, 4.6 ms for 116 deaths in one step, `run-20261029-physjolt`) is not something a player sees, so it
can be spread over frames as long as the kill, its gem and the body's disappearance stay in the same frame.

Not found: interviews about performance (not searched in depth); about 4.5 KB at the end of the news feed was not read.
