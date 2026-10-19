# Soul Hunter — Full Bug MIS (find only, nothing fixed)

**Date:** 2026-10-19
**Asked by owner:** "is project mein kitna bug hai wo dhoondho, sirf dhoondho, ek ek game object ko check karo or MIS banakar save karo"
**Code state:** `main` at `ca1b93ec` (local, not pushed)
**Status:** the register itself changed nothing in the project. Batch 1 (2026-10-19, merged at `ac7077fc`) fixed these S2 rows: CMB-01, CMB-02, CMB-03, CMB-04, CORE-02 (row half wrong; see AGENTS.md), AI-01, AI-02, AI-03, AI-04, AI-05, AI-07, ITM-01, ITM-02, UI-01, UI-02, UI-03. CMB-05 waits for the owner. Every other row is OPEN.

## How it was found

1. **Every game object, by a tool.** A temporary Unity editor audit ran in the isolated copy only (`D:/SoulHunter-Validation/editmode-20260924-134853/project/Assets/AuditTemp/`; it is not in the project).
   - It covered 568 prefabs, 762 ScriptableObject assets and 16 scenes: **7,503 GameObjects, 13,303 components**.
   - Checked: missing scripts, references to missing objects, empty layer masks, renderers without materials or meshes, mesh colliders without meshes, animators without controllers, audio sources that play nothing, duplicate listeners or event systems.
   - Raw output: `D:/SoulHunter-Validation/run-20261019-audit/gameobject-audit.tsv` (678 lines). Each group was then checked against the code (section G).
2. **Code review by a group of five read-only agents**, one area each: combat and weapons, enemies and hazards, items/save/economy, UI, core flow.
   - Every finding was traced through its callers and callees.
   - The reports are in the same folder (`agent-*.md`).
   - The lead re-checked the top claims in the code (CMB-01, CMB-02, CORE-05), and all three hold.
3. **Duplicates** found by two agents are merged (noted under each row). The earlier registers (`BUG_MIS_Level1*.md`, `CTO_Check_MIS.md`) do not contain any of these rows.

**Not done:** nothing was run in play to reproduce these rows. They are code-reading findings ("Confirmed" means the code path was read end to end; "Likely" means it depends on engine behaviour or on reachability not traced fully). Each one needs a reproducing test before its fix, as section 10 of AGENTS.md requires.

## Summary

| Severity | Meaning | Count |
|---|---|---|
| S1 | crash, softlock, save loss | **0** |
| S2 | wrong gameplay or wrong rule | **17** |
| S3 | minor, cosmetic, performance, latent | **41** |
| — | game-object audit groups (section G) | 7 groups; **0 real defects**, 3 dead-data items |

58 unique code findings (61 reported, 3 duplicates merged).

---

## A. Combat and weapons (CMB)

| ID | Sev | What is wrong | Where | Scenario | Confidence |
|---|---|---|---|---|---|
| CMB-01 | S2 | Evolutions built on a base script (Unholy Vespers, Heaven Sword, Soul Eater) also get the base weapon's whole level 2-8 table. | `WeaponManager.cs:607` `ApplyWeaponLevel(newWep, evo.BaseWeaponMaxLevel)`; `BibleWeapon.cs:92-104`; `CrossWeapon.cs:72-84`; `GarlicWeapon.cs:117-130` | Unholy Vespers orbits 7 books at 50 damage instead of 4 books at 30. Heaven Sword: 107 damage and 3 swords instead of 77 and 1. Soul Eater: 30 damage, 0.7 s, radius 15 instead of 20, 1.0 s, 7.5. Shadow Kael copies the inflated values. | Confirmed (lead re-checked) |
| CMB-02 | S2 | Pentagram erases every active enemy on the map, not only those on screen. The item erase is limited to the screen; the enemy loop is not. | `PentagramWeapon.cs:82-111` | Off-screen stragglers, incoming formations and off-screen chest elites are erased. Their chests are passed on and their max health is credited on Results. | Confirmed (lead re-checked) |
| CMB-03 | S2 | A long-lived TouchDamage zone keeps a killed enemy in its contact list (no OnTriggerExit for a deactivated collider). The pooled body is hit again wherever it respawns. | `TouchDamage.cs` RegisterContact/Tick (~120-150) | King Bible books (1.7 s interval) build up a stale list all run, and respawned low-HP enemies die off-screen at once. Santa Water and Drowning Chalice pools do the same for their lifetime. | Likely |
| CMB-04 | S2 | Limit Break cards that change nothing. | `LimitBreak.cs:72,88,91,102`; `MagicWandWeapon.cs:37,102-104`; `CrimsonShroudWeapon.cs:20,54,61` | Magic Wand reads `_stats` directly, so only Pierce cards work (Damage, Area, Speed and Amount are dead). The Hyper projectile-speed bonus also never reaches the Magic Wand. Crimson Shroud's D/A/N cards are dead. Peachone and Dusk Raven ignore Speed (the card and Bracer). | Confirmed |
| CMB-05 | S2 | Shadow Kael's mirrored Crimson Shroud caps every hit on the boss at 10. A mirrored Laurel also blocks the player's hits. | `CrimsonShroudWeapon.cs:23-30,46-50`; `ShadowKaelController.cs:72-77` | A player holding Crimson Shroud at Level 10 deals at most 10 per hit to a 75,000-HP boss, and Soul Burst's 9999 becomes 10. **Owner decision:** strict mirror, or an exception? | Confirmed (intent unknown) |
| CMB-06 | S3 | A recycled axe is switched off early by the previous throw's 5 s coroutine (no per-throw token). Axes in flight when the Axe evolves never return to the pool. | `AxeWeapon.cs:141-148` | An axe used up by pierce at 0.4 s and re-thrown at 4 s vanishes at 5 s after 1 s of flight. | Confirmed |
| CMB-07 | S3 | Fixed NonAlloc buffers drop targets in big crowds. | `RequiemOfSoulsWeapon.cs:36,73` (mask `~0`, 128 slots); `GarlicWeapon.cs:104` (200); `SoulBurstController.cs:26` (300) | In a late 300-enemy crowd, part of the crowd is untouched. Requiem's slots also fill with gems and props. | Likely |
| CMB-08 | S3 | Bloody Tear heals when an enemy is near the lash, not when the lash hits. | `BloodyTearWeapon.cs:92-105` | Level 7 ghosts (immune to Normal damage) still heal the player 8 per lash. | Confirmed |
| CMB-09 | S3 | Upgrading Peachone or Dusk Raven during an activation starts a second, overlapping bombardment. The Gemini copy does the same. | `PeachoneWeapon.cs:100-105`; `AutoAttackWeapon.cs:417`; `GeminiCounterpart.cs:31-33` | Two sets a second for the rest of the first activation. | Confirmed (VS behaviour unverified) |
| CMB-10 | S3 | Dash and Soul Burst share one `IsInvincible` bool with no counting. Each ends the other's protection and clears the bot/benchmark god mode. *(= CORE-06)* | `SoulBurstController.cs:136,148`; `PlayerController.cs:396,432` | Dash during the 2 s burst immunity: the immunity ends with the dash. | Confirmed |
| CMB-11 | S3 | Holy Wand fires all shots in one frame from one spot (no FireInSuccession). Its cooldown 0.1 s and damage 40 are hard-coded, and the DamageAmount field (10) is unused, so Arcana zones read 10. | `HolyWandWeapon.cs:15,31` | Against the 2026-09-30 succession rule; the numbers need a wiki check. | Likely |
| CMB-12 | S3 | A duplicate WeaponManager is never destroyed: the guard sits inside the `Evolutions != null` branch. Latent; only one exists today. | `WeaponManager.cs:105-114` | A second manager would also give the start weapon. | Confirmed |
| CMB-13 | S3 perf | FireInSuccession allocates a closure, an iterator and a WaitForSeconds on every attack. | `AutoAttackWeapon.cs:185-206` | Thousand Edge every 0.35 s, Gun volleys and others; adds to GC. | Confirmed |
| CMB-14 | S3 | Dead DamageCaster (empty target layer) on every stage-boss prefab and on 324 enemy prefabs. Nothing calls it. | `Prefabs/Bosses/Gameplay/*`, `Prefabs/Enemies/Gameplay/**` | Dead data. | Confirmed (also found by the audit, G-2) |

## B. Enemies, spawning and hazards (AI)

| ID | Sev | What is wrong | Where | Scenario | Confidence |
|---|---|---|---|---|---|
| AI-01 | S2 | Endless: Shadow Kael never re-captures the loadout. `_captured` is never reset, and boss bodies are pooled and never trimmed. *(= CORE-01, found by two agents)* | `ShadowKaelController.cs:25,32,42-43`; `EnemySpawner.cs:427-438,460-466` | Endless cycle 2: the boss keeps cycle 1's weapons and stats, with its revivals spent and no mirrored speed. Breaks AGENTS §5 Level 10 ("captured at encounter start"). | Confirmed |
| AI-02 | S2 | A chest passed on by Pentagram can go to a Fixed Direction / Floaty Fixed enemy. That enemy leaves the screen and is pooled without OnDied, so the chest is lost. | `EnemySpawner.cs:693-704`; `EnemyController.cs:475-486`; `EnemyFlyState.cs:53` | On Level 2 (Gloom Moth) or Level 4 (Gliding Gargoyle), a chest or Arcana chest disappears. | Confirmed |
| AI-03 | S2 | Killing the Rising Tide's Tide Caller gives a free, unscheduled chest. It is a boss body with DropsChest, and its erased drops queue the chest. | `RisingTide.cs:72-73`; `StageHazards.cs:343-346`; `EnemyDrop.cs:169-172` | Rising Tide Omen, then kill the caller: a later enemy carries a chest. | Confirmed |
| AI-04 | S2 | The 30:00 stage-clear wipe kills instant-kill-resistant enemies too. | `LevelProgressionManager.cs:323-337` (WipeArena); `ItemGuardian.cs:43-48`; `SoulCasket.cs:213-216` | Kite an item guardian (30,000 HP, res-kill) until 30:00: it dies and drops its Soul Egg (worth 10,000 gold). Casket guards are wiped, so the casket becomes ready. | Confirmed |
| AI-05 | S2 | Endless: stage bosses keep cycle-0 contact damage. The multiplier is applied only in Awake, and boss pools are reused. | `EnemyController.cs:198-199` | The "+25% enemy damage per cycle" never reaches bosses or Shadow Kael. | Confirmed |
| AI-06 | S3 | Fliers and hazard members spawned during an Orologion are not frozen: their state replaces the freeze. | `EnemySpawner.cs:596-597,745-746,775-776`; PileShooter / MineSentry / StillEnemy `Begin` | During a Time Freeze, formations fly and pile shooters fire. Against the VS "new enemies will also become frozen". | Confirmed |
| AI-07 | S3→S2 | Level 9's Closing Arena shrinks on its own clock (`Time.deltaTime`), not the stage clock. *(= CORE-03, rated S2 there; counted as S2)* | `CampaignSignatureSystem.cs:53-54,206` | Hurry mode: at 30:00 the radius is ~22.5 instead of 10. Debug skips are ignored. | Confirmed |
| AI-08 | S3 | Hazard and special spawns ignore the 300 and 500 caps. SoulCasket's comment claims a cap that does not exist. | `EnemySpawner.cs:1045-1053`; `SoulCasket.cs:211` | 500 on screen, then a Thorn Circle, a casket ring and summons push the count past 500. | Confirmed |
| AI-09 | S3 | Still hazard members (pile shooters, mines, Rootbound plants) are moved in front of a fleeing player as stragglers. | `EnemySpawner.cs:347-365` | Run 40 units from a Pile Assault: its shooters reappear ahead of you. **Design call.** | Confirmed |
| AI-10 | S3 | `_special` keeps pooled bodies that later serve as ordinary wave enemies, so Nightfall/Rootbound skip swapping them. | `EnemySpawner.cs:124,814,819,1049` | Some ordinary enemies stay during a takeover. | Confirmed |
| AI-11 | S3 | Enemy bullets survive the stage clear and stage change. | `StageHazards.cs:434-451`; `EnemyBullet.cs` | Lost Heads' bullets keep hitting through the 5 s transition and into the next stage. | Confirmed |
| AI-12 | S3 mem | The DontDestroyOnLoad drawers never forget destroyed bodies, batches or material copies. | `HordeVatDrawer.cs:74,216`; `EnemyBulletDrawer.cs:89-91` | They grow over long Endless sessions and repeated runs. Size not measured. | Confirmed |

## C. Items, chests, economy, save (ITM)

| ID | Sev | What is wrong | Where | Scenario | Confidence |
|---|---|---|---|---|---|
| ITM-01 | S2 | Data Recovery overwrites `.replaced.bak` on every restore, against its own "a restore loses nothing". | `SaveService.cs:385-420` (400) | Restore twice (a double press, or reopening the tab before playing): the newer progress A is lost for good. | Confirmed |
| ITM-02 | S2 | The magnet sweep uses one 256-slot OverlapSphereNonAlloc with no layer mask, triggers included. Enemies and weapon zones fill it, so gems and coins in range are skipped. | `PlayerExperience.cs:55,67` | Late game with a big Magnet (~16 units) inside the horde: gems in range are not pulled. Enemy bullets no longer have colliders since `a13a02ba`, so they no longer fill it. | Confirmed (overflow not measured) |
| ITM-03 | S3 | StageItemSpawner keeps references to pooled pickups after they are taken. At the next stage, Clear() switches off an unrelated live pickup that reused the object. | `StageItems.cs:226,265` | Take the stage chicken, a lantern drops a chicken, leave it: at 30:00 it vanishes. The same for Vacuum and Orologion. | Confirmed |
| ITM-04 | S3 | A failed background save is only logged. `_dirty` was already cleared, so nothing retries. | `SaveService.cs:192-208,255-261` | A locked or full disk (D: is 98% full) loses gold and unlocks until the next save. | Confirmed |
| ITM-05 | S3 | No flush on OnApplicationPause or focus loss. The first SaveSoon after a load writes without `SaveFlusher.Ensure`. | `SaveFlusher.cs:26-38`; `SaveService.cs:147-153` | Android sends the app to the background and kills it: up to 2 s of gold (10 s of Bestiary) is lost. | Likely |
| ITM-06 | S3 | ChestsOpened / FiveItemChestsOpened are saved only in CloseChest. The quick coin-only chest and the Arcana-draft path never save the counter. | `ChestUI.cs:76-118`; `ChestPickup.cs:104` | Quit before the next save: the 1-1-3-1-1-5 sequence or the skip state repeats a chest. | Confirmed |
| ITM-07 | S3 | `TimeFreezePickup.WorldFreezeEndsAt` is a static that is never reset between runs. | `TimeFreezePickup.cs:18-23` | Orologion, then restart within 10 s: the next run's first enemies spawn frozen and Gold Fever decays at half speed. | Confirmed |
| ITM-08 | S3 perf | Every pickup sets `_checkSoon`, so the full item-unlock Evaluate runs (lambdas, List.Contains, summing about 290 Bestiary records) on every frame with a pickup. | `ItemUnlockTracker.cs:107-119,157-178` | Gold Fever coins, gem streams. | Confirmed (cost not measured) |
| ITM-09 | S3 perf | Per kill: `type.name` allocates a string, plus a `BestiaryKills.Find` closure and a linear scan. | `CollectionTracker.cs:125-137`; `CollectionBook.cs:188-198` | 300 times in a wipe frame. | Confirmed |
| ITM-10 | S3 perf | `GoldPickup.ActiveCoins` Contains/Remove are O(n). | `GoldPickup.cs:34-43` | Hundreds of coins during Gold Fever. | Confirmed |
| ITM-11 | S3 perf | GroundItemEraser runs `Doomed.Contains` (O(n²)), `FindObjectsByType<ChestPickup>`, and two GetComponent per item on every Pentagram activation. | `GroundItemEraser.cs:31-34,47` | Wipe frames. | Confirmed |
| ITM-12 | S3 | The "Defeat the boss of Stage n" unlock text does not match the code, which unlocks only at 30:00 (or the Shadow Kael kill). | `CharacterUnlockTracker.cs:157-168`; `CharacterUnlocks.cs:53,125` | Kill the boss, die before 30:00: Gideon, Kaelen, Seraphina, Lumina, Rurik and Sylas are not unlocked. | Confirmed |
| ITM-13 | S3 | The legacy maxed-out payout gives both 30 HP and 25 gold, with no choice, no Recovery bonus, and no Big Coin Bag count. | `LevelUpManager.cs:216,365-371,399-405` | Banish the last offered upgrade. | Likely |

## D. UI (UI)

| ID | Sev | What is wrong | Where | Scenario | Confidence |
|---|---|---|---|---|---|
| UI-01 | S2 | Esc/Start during a level-up or the merchant shop opens the pause menu hidden under them (HUD order 0 against 300/900) and moves focus to the hidden Resume. | `GameSystemManager.cs:52-71`; `PauseMenuUI.cs:209-221`; `LevelUpUI.cs:665,688`; `WanderingMerchant.cs:490`; `HUD_Canvas.prefab:2201` | Keyboard and controller reach a dead end (focus on an inactive object). Quit from the shop shows Results under the shop. | Confirmed |
| UI-02 | S2 | Draw a Fate: after the Arcana screen opens, the shop rebuild moves focus to a hidden ware. | `WanderingMerchant.cs:310-316,354-359,523-537`; `ArcanaChoiceScreen.cs:420-425` | Inverse mode on keyboard: the next Enter buys a Soul Egg or Sigil with the player's gold instead of picking a card. | Confirmed |
| UI-03 | S2 | Results DONE resumes time (ResetAll) before the async MainMenu load, so gameplay runs under Results. | `GameOverUI.cs:76-85`; `GameSystemManager.cs:84`; `SceneService.cs:39-46` | During the load: coins are saved, a level-up screen can open, unlock notices appear. | Confirmed |
| UI-04 | S3 | The chest preview RenderTexture is reused only for the prewarmed first chest. Close() destroys the stage and its texture. | `ChestOpeningScreen.cs:120-129,247-251,366-371`; `ChestPreviewStage.cs:365-368` | Every later chest allocates a new 512² MSAA RT, against the AGENTS 2026-10-18 note. | Confirmed |
| UI-05 | S3 | Skipping a chest during its blast stops Pulse mid-fade. The flash and ring stay half-faded into the next chest. | `ChestOpeningScreen.cs:235-266,337-341,699-715` | From the 7th chest, skip 0.25-0.6 s in. | Confirmed |
| UI-06 | S3 | Esc on the Evolutions and Map screens resumes the whole game instead of going back. Gamepad B does nothing there. | `EvolutionRecipesScreen.cs`; `PauseMapScreen.cs`; `PauseMenuUI.cs:216-220` | Pause, Evolutions, Esc. | Confirmed |
| UI-07 | S3 perf | The idle Omen wheel builds a string every frame. | `OmenRouletteUI.cs:135-136,178-183` | Random Events mode. | Confirmed |
| UI-08 | S3 perf | The XP bar builds the "LV n" string every frame. | `XPBarUI.cs:88,101-108` | Always. | Confirmed |
| UI-09 | S3 perf | StatsPanel allocates `new Vector3[4]` every frame while shown. | `StatsPanel.cs:158-168,276` | Pause and level-up. | Confirmed |
| UI-10 | S3 | Theme colours baked into `<color=#..>` rich text do not follow a live theme change. The guard regex misses interpolated hex. | `RunResultsScreen.cs:575-586`; `PowerUpMenuUI.cs:730-780`; `StatsPanel.cs:342`; `CharacterSelectionUI.cs:434` | A Theme Studio edit with these screens open leaves mixed colours. | Confirmed |
| UI-11 | S3 latent | `GameTime.IsPaused` prunes destroyed owners but does not re-apply timeScale. It can say "not paused" while timeScale is 0. | `GameTime.cs:19-22,82-92` | No live trigger found. | Likely |

## E. Core flow (CORE)

| ID | Sev | What is wrong | Where | Scenario | Confidence |
|---|---|---|---|---|---|
| CORE-01 | — | Merged into AI-01. | | | |
| CORE-02 | S2 | The Gemini copy of Gun, Peachone or Dusk Raven keeps firing after a union replaces its weapon, and the union never gets a copy. | `ArcanaManager.cs:322-348`; `GeminiCounterpart.cs`; `WeaponManager.cs:693-700` | Gemini plus Revenant Wheel or Twilight Flock: orphan copies attack at their old level. | Confirmed |
| CORE-03 | — | Merged into AI-07. | | | |
| CORE-04 | S3 | The Omen formation filter is checked at pick time (XX:30). A 24:30 pick fires its formation at 25:00 together with the guardian. | `RandomEventDirector.cs:416-421,428` | Against OmenTable's own rule. | Confirmed (frame order Likely) |
| CORE-05 | S3 | Soul Burst has no pause check. | `SoulBurstController.cs:81-90` | Press Ultimate on the level-up, chest, merchant or pause screen: 9999 damage to everything within 30 units behind the menu. | Confirmed (lead re-checked) |
| CORE-06 | — | Merged into CMB-10. | | | |
| CORE-07 | S3 | A pause during a dash ends the dash for good (`step <= 0 → break`). | `PlayerController.cs:415-427` | A level-up from XP mid-dash: the rest of the dash is lost. | Confirmed |
| CORE-08 | S3 | V-Sync on: `RoundToInt(hz/60)` runs some displays under 60. | `DisplaySettingsApplier.cs:86` | 90 Hz gives 45 FPS, 100 Hz gives 50, 165 Hz gives 55. V-Sync is off by default. | Confirmed |
| CORE-09 | S3 | Banish on the Gold Pouch / Roast Chicken cards spends a charge with no effect. | `LevelUpManager.cs:351-363` | Maxed-out level-up, press Banish. | Confirmed |
| CORE-10 | S3 mem | Each maxed-out level-up, reroll or banish creates 2-4 UpgradeData ScriptableObjects that are never destroyed. | `MaxedRewards.cs:41-62`; LimitBreak card builder | A 5-hour run maxed from about level 80. | Confirmed (size not measured) |
| CORE-11 | S3 mem | Each Level 9 start makes a new ring Material that is never destroyed. | `CampaignSignatureSystem.cs:267` | Endless cycles. | Confirmed |

## G. Every game object: audit groups

| ID | Found | Count | Real defect? |
|---|---|---|---|
| G-1 | `EnvironmentScanner._feetPivot` points at a missing object on enemy gameplay prefabs | 324 | No. The scanner is off on every horde prefab, so this is dead data (S3). |
| G-2 | `DamageCaster._targetLayer` empty on enemy prefabs | 324 | No. Nothing calls an enemy's DamageCaster (see CMB-14). Dead component (S3). |
| G-3 | Player `EnvironmentScanner`: ground layer 0, feet pivot missing | 1 | It never reports grounded. Movement does not need it today. Dead logic (S3). |
| G-4 | `Main_Gameplay` WeaponManager `_availableUnions`: 6 missing references | 6 | Already known (the six removed invented unions). Skipped by code. Clean-up (S3). |
| G-5 | Empty layer masks on Soul Burst, Whip and pickup `_playerLayer` | 7 | No. The code defaults them to Enemy, or the field is unused since PlayerReach. |
| G-6 | Main menu buttons: sliced Image without sprite; DamageText MeshRenderer without mesh; `Cross_Model` empty material slot | 8 | No. The theme sets the plates at runtime, TMP builds its own mesh, and Cross_Model is unused. |
| G-7 | Unused `Renderer2DData` probe-volume debug resources missing | 6 | No. The 2D renderer is unused. |

No missing scripts, no animators without controllers, no audio sources playing nothing, and no duplicate AudioListeners or EventSystems in any scene.

## Owner decisions needed before fixing

- CMB-05: should Shadow Kael's mirrored Crimson Shroud and Laurel protect him?
- AI-09: should still hazard rings follow a fleeing player?
- CMB-11: the Holy Wand numbers need a wiki check before change.
