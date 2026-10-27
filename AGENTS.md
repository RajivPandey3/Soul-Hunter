# AGENTS.md — Soul Hunter Development Operating Contract

**Project:** Soul Hunter  
**Canonical project root:** `D:\Unity Projects\Soul-Hunter`  
**Unity:** `6000.0.36f1` (`9fe3b5f71dbb`)  
**Primary platform:** Steam / Windows  
**Secondary platform:** Android  
**Status date:** 2026-09-24

---

## 1. Purpose

This file is the operational contract for AI agents working on Soul Hunter.

Use the verified current project state and approved owner decisions below as the default working truth. Do not repeatedly re-audit the entire project before normal tasks. Perform only the minimum task-specific verification needed for the requested change.

This file does not declare unverified systems complete. Static presence is not runtime acceptance. `UNKNOWN` is not `PASS`.

---

## 2. Authority Order

When sources disagree, use this order:

1. Explicit project-owner decisions and approved Soul Hunter requirements.
2. Approved implementation contracts / decision records.
3. This `AGENTS.md` operational contract.
4. Current Soul Hunter implementation.
5. Current project documentation.
6. External games, tutorials, marketplace content, conventions, or references.

Owner decision (2026-09-24): Soul Hunter is a Vampire Survivors-style game with its own story. Vampire Survivors is the reference for core gameplay: the run loop, controls, weapon/passive/evolution structure, level-up choices, enemy waves, pickups, chests, and overall game feel should match it. Where Vampire Survivors and a Soul Hunter owner decision differ (for example the run structure and the Level 3, 7 and 10 decisions below), the Soul Hunter decision wins.

Match mechanics and feel only. Do not copy Vampire Survivors code, decompiled or extracted game data, art, audio, text, or character/item names. Soul Hunter's story, characters, names, and art stay its own.

Owner rule (2026-09-28): every Soul Hunter task, however small, is based 0–100% on the VS wiki (vampire.survivors.wiki), plus the owner's story (`Docs/Game_Design_Document.md`, never edited). Look the rule up on the wiki (raw: `https://vampire.survivors.wiki/w/<Page>?action=raw`) and cite the page. Never state a VS fact from memory; if the wiki is silent or thin, say so. Ask the owner only when the wiki has no answer, it conflicts with the story or an owner decision, or it is art approval.

Owner rule (2026-09-30): "everwhere follow DOT VS ONLY". Every change is a DOT implementation (section 10) of the VS rule, and VS only. Where the wiki gives a number or rule, use it; "provisional" Soul Hunter numbers are not acceptable there. Where the wiki is silent, say so and ask the owner. Owner decisions and the owner's story above still come first.

**Owner core rule (2026-09-30, the definition of Soul Hunter):** "SH = VS + my own story + own audio, own VFX, own assets, own UI, etc. Everything should be own, but the core logic of VS must be adopted top to bottom (DOT). No mistakes. If a VS rule has to change a little, 5% is allowed."
- **From VS, top to bottom:** every rule, number, formula, timing, flow and system (stats, weapons, evolutions, level-ups, enemies, waves, pickups, chests, stages, modes, menus and what they do). Nothing is left out without a reason recorded here.
- **Soul Hunter's own:** story, names, text, characters, art, models, VFX, audio, music and the UI's look. Never VS assets, names or text.
- **5% rule (owner confirmed 2026-09-30, "Numbers ±5%"):** a VS number may be tuned by at most ±5% of its wiki value, and every such change is recorded with the wiki value next to it. Bigger or structural changes need an explicit owner decision. Approved owner decisions in section 5 stay as the recorded exceptions: one 10-stage run, no Reaper, Level 3/7/10, nearest-enemy aiming, dash and Soul Burst from the GDD.
- **No mistakes:** each rule is re-checked against the raw wiki page and the code before it is built, then proven with tests and editor play. When the wiki is silent or contradicts itself, stop and ask the owner; never guess.
- Owner decisions 2026-09-30: the dash only moves and avoids damage (its 50 damage goes); the 6 Soul Hunter-invented weapon unions are removed; XP gem colours change at 20 and 90 XP (VS 2 and 9, scaled to Soul Hunter's 10-XP gems); stage-clear revival gold stacks; missing VS items and the VS unlock system are added later; Shadow Kael's mirrored Clock Lancet slows and his Pentagram erases items. Built: see "Phase 0 of the VS DOT plan" (section 7).
- The full VS rule list and the gaps are in `Docs/VS_DOT_Table.md`; the work order is in `Docs/VS_DOT_Plan.md`.

**Owner rule (2026-10-27): VS 100% first, then Soul Hunter's extras.** The owner said: "VS Wiki 100% match hona chahiye, to pahle match karo then plus apna extra thing". The scope is mechanics, rules and all VS content.
- An owner decision no longer replaces a VS rule. The VS rule is built, and the Soul Hunter feature sits on top of it as an extra, an option or a mode. This covers the 10-level campaign, aiming, the Level 3 merchant, the crowd and the open unlocks.
- The audit of 2026-10-27 is in `Docs/VS_Audit_20261027/`, and the work order is in `Docs/VS_100_Plan.md`.
- Phase 1a (2026-10-27): Levels 5, 6 and 7 now take every stage value from Abyss Foscari, Mt.Moonspell and Lake Foscari, as their enemy tables already did. Levels 8-10 stay on Cappella Magna.
  - Gold is 1.5 on Levels 5-7 and 1.4 on Levels 8-10.
  - Light sources on Levels 5-7 are 7.5% with a 50% cap; at most 18 on Level 5 and 12 on Levels 6-7.
  - Hyper projectile speed is 1 on Levels 6 and 7, because those infoboxes give none.
  - Code: `CampaignLevelCatalog`, `LightSourceSpawner`.

If current code conflicts with an approved owner decision, the approved requirement is authoritative until explicitly revised.

---

## 3. Architecture

Runtime dependency direction:

`Foundation -> Gameplay -> UI`

Editor tooling may depend on Foundation, Gameplay, and UI.

Do not introduce runtime reverse dependencies from Foundation to Gameplay/UI or from Gameplay to UI without an explicit architecture decision.

Prefer explicit ownership, deterministic initialization, and testable dependencies over new hidden global lookups or singleton coupling.

---

## 4. Canonical Project Safety

Canonical project:

`D:\Unity Projects\Soul-Hunter`

### Safety rules

- Do not run `git reset`, `git clean`, destructive checkout/restore, forced rebase, or delete large file sets without explicit owner approval.
- Do not silently discard current working-tree changes.
- Do not modify canonical Soul Hunter merely to make a validation step pass.
- Risky automation/build/setup methods should execute in an isolated workspace first.
- Never delete Unity lock files or kill a Unity process merely to bypass a lock.
- Do not deploy or publish without explicit authorization.
- Preserve evidence for meaningful accepted changes.

---

## 5. Approved Product Decisions

### Level 3

Authoritative level:

**The Forgotten Village**

Signature mechanic:

**Wandering Merchant**

The Burning Village fire hazards are gone; Level 3 spawns only the merchant.

Merchant rules (owner: "apply the VS rule everywhere", 2026-09-26; VS Merchant and Golden Egg pages on vampire.survivors.wiki, checked 2026-09-26):
- The merchant waits about 7 units from the player when Stage 3 starts, on the first spot that no stage obstacle covers, at the hero's height and facing the player.
- Touching it opens the shop and pauses the game. It sells Bone and Cherry Bomb for 1,000 gold each, at level 1, never twice and never past the 6 weapon slots. It also sells Soul Eggs (Soul Hunter's Golden Egg) for 10,000 gold, repeatable.
- Gold is the saved gold (`EconomyService.SpendGold`). One source says VS spends total gold; that source is thin.
- Several things can be bought in one visit. Leaving sends the merchant away for the rest of the stage.
- The old potion and blessings are removed, because the VS merchant does not sell them.
- Prices are the VS numbers and are provisional balance.

Before this, the shop took run "souls" that nothing ever granted (`AddSouls` had no caller), so nothing could be bought. The merchant was also a gold capsule.

Soul Eggs (`SoulEggs`, `GameData.SoulEggs`, repaired by `SaveService.Migrate`):
- Each egg raises one random stat of the character it was bought for, out of 19, by the VS egg amount, with the VS caps. It counts from that character's next run (`PlayerController` and `LevelUpManager` for Reroll/Skip/Banish).
- Magnet is +0.01, VS's +0.3 on its base 30, added to the Magnet base before the multipliers (2026-10-09, release wave 1).
- The character select screen does not show egg bonuses yet (UI section).

Model:
- The owner-approved Blender model (2026-09-26) is built by `ArtSource/Generators/build_merchant.py` (previews in `ArtSource/SH10_Merchant_v01/Previews`) and imported by `MerchantModelImporter` (menu Soul Hunter/Content/Import Wandering Merchant) as `Assets/Prefabs/NPC/SH10_NPC_WanderingMerchant.prefab`, referenced by `GameContentCatalog.WanderingMerchant`.
- It has 2,694 triangles, stands 1.11x the hero's height, uses URP Lit with glowing soul and ember lanterns, and casts shadows.
- From the game camera it reads by its pack and lanterns; the cyan glow is faint.

Tools:
- `AutoplayBot` walks to the merchant, and `-sh-autoplay-gold N` sets starting gold on the throw-away save.
- Menu Soul Hunter/Debug/Jump To Stage 3/7/10 jumps there in Play Mode, for hands-on tests.

Validation:
- `WanderingMerchantTests`, `SoulEggTests` (T) and `WanderingMerchantPlayModeTests` (R: spawns near the start in the open with the approved model, the shop opens on touch, gold and slot rules hold, the egg is saved and applies next run, and the real save is unchanged).
- EditMode 317/317 and PlayMode 65/65 (`D:\SoulHunter-Validation\run-20260926-merchant-model\`, `-model2\`).
- Editor play `autoplay-editor-07` to `-09`: 0 errors, min 56-67 FPS; partial `V`.
- The owner accepted it ("merchant OK", 2026-09-26).

Seen, not changed: the hero, the horde and the merchant stand on the player's plane (y ≈ 1) while the stage floor's top is y = 0. Whether characters look like they float needs a visual check (Art section).

### Level 7

Ghost / spectral enemies:

- ordinary `Normal` damage must not damage them;
- `Holy`, `Magic`, or explicitly tagged `Spectral` damage is valid;
- existing Holy 2x vulnerability may remain a balance parameter unless changed by a later balance decision.

Weapon damage types (owner chose "type by theme", 2026-09-26). Before this only Magic Wand, Song of Mana, Santa Water and Holy Wand could hurt ghosts:

| Type | Weapons |
|---|---|
| Holy (2x on ghosts) | Cross, Bible, Santa Water, Holy Wand and their evolutions |
| Magic | Magic Wand, Song of Mana, Fire Wand, Lightning Ring, Runetracer, Peachone |
| Spectral | Soul Burst (the ultimate) |
| Normal (passes through ghosts) | Whip, Knife, Axe, Bone, Gun, Cherry Bomb, Garlic and the rest |

Pentagram erases enemies without a damage type, so it also clears ghosts. The ghost check is in `HealthController.TakeDamage` (`EnemyController.IsSpectral`); the L7 boss is a ghost too.

Validation: `SpectralStagePlayModeTests` (R, real Stage 7): each Holy/Magic weapon hurts a ring of ghosts, each Normal weapon passes through them, Soul Burst reaches them, and the L7 boss ignores Normal damage and takes Holy at 2x. Results: see "Castle corridor" below (same run). That run also found and fixed Soul Burst's empty enemy mask: the ultimate had never hurt anything.

Editor play `autoplay-editor-27` (Stage 7 jumped to at level 1, god mode, 3 min): 0 errors, min 39 FPS. With only Kael's Whip (Normal) there were 3 kills in the first 30 s while the ghost horde grew to 229-300 (ghosts take no knockback). Once Bible and Lightning Ring came from level-ups, their hits showed on the ghosts, reaching 1528 kills and level 20 (partial `V`). In the one-run campaign a player arrives at Stage 7 with more weapons; whether a Normal-only loadout should be this helpless is a balance question for the owner.

Not yet played by the owner.

### Level 10

Boss:

**Shadow Kael**

Authoritative behavior:

Shadow Kael mirrors the player's combat loadout. At encounter start, the relevant player weapon/stat loadout must be captured and represented by the boss according to the implementation contract.

Implementation (2026-09-26):
- Capture: `EnemySpawner.SpawnBoss` calls `ShadowKaelController.CaptureFrom(player)` when the Stage 10 boss spawns. The copy is fixed from then on; later player upgrades do not reach the boss.
- Stats: the boss gets its own `PlayerStats` copy (`PlayerStats.CopyFrom`, no second meta bonus), so every mirrored weapon scales like the player's own: Might, Cooldown, Area, Amount, Duration, Speed, Luck, crits and bounces. Armor reaches the boss's `HealthController` through that copy. Move speed multiplies the stage speed.
- Weapons: `WeaponManager.GetEquippedWeapons()` lists the weapons the player is really using, at their levels: evolved and union weapons are included, and base weapons they replaced are left out. The boss is drawn 3x bigger, but its weapons keep the player's size.
- Revivals: a lethal hit with mirrored revivals left restores the boss through `HealthController.TryPreventDeath` without firing `OnDied`. A revival no longer clears the stage, wins the run, drops a chest or counts as a kill.

Defects found and fixed in the process:
- The boss mirrored only Might, Cooldown and Area, by multiplying base numbers. Area was applied as the weapon's transform scale.
- It mirrored a base weapon instead of its evolution.
- Any revival fired `OnDied`, so Stage 10 cleared on the first "death".
- 14 of 19 mirrored weapons never hurt the player:
  - Pooled `Projectile`s ignored the player, and pooled `ProjectileDamage` kept the "Enemy" tag. `AutoAttackWeapon.AimAtHostiles` now retargets every spawned object, and `Projectile.TargetsPlayer` resets on reuse.
  - Axe and Cross switched themselves off unless their parent was the player.
  - Knife and Thousand Edge needed the player's body to aim.
  - Magic Wand, Cross, Lightning Ring, Crimson Shroud and Bloody Tear picked targets from the enemy list.
- A boss-held Pentagram wiped the boss's own horde and the boss itself, and a boss-held Clock Lancet froze them. Both now skip enemies.

The player has no frozen state, and "wiping" the player is not a VS mechanic, so Shadow Kael's Clock Lancet only shows its beam and his Pentagram only flashes (open question in `Docs/VS_DOT_Plan.md`, Phase 0).

Tools: `LevelProgressionManager.SkipToBoss()` (menu Soul Hunter/Debug/Skip To Stage Boss, bot option `-sh-autoplay-boss-after S`). With it, the bot records a `shadow-kael` event listing the mirrored weapons, plus screenshots.

Validation:
- `ShadowKaelMirrorTests` (T): stat copy, evolved weapon, boss scale, armor, and a revival without `OnDied`.
- `ShadowKaelPlayModeTests` (R, real Stage 10):
  - the loadout is copied exactly and fixed at encounter start;
  - each of the 19 weapons held by the boss hurts the player (Laurel, Clock Lancet and Pentagram excepted, as above) and never hurts or freezes the boss or its horde;
  - a revival neither clears the stage nor wins the run, and the last death does.
- EditMode 319/319 and PlayMode 86/86, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-shadowkael\`).
- Editor play `autoplay-editor-11` and `-12` (Stage 10, god mode, boss after 90 s and 75 s): both times Shadow Kael copied the player's 6 weapons exactly (for example Whip, Gun, Fire Wand, Song of Mana, Axe Lv2, Lightning Ring), with 0 errors. A screenshot shows the boss's mirrored Lightning Ring striking the hero (partial `V`).
- Shadow Kael uses the generic Level 10 boss model, not a dark copy of Kael (Art section).

Seen, not changed:
- The boss died in about 5 s at player level 6, because its health was 50 x player level. Since 2026-09-27 (owner's story) it is 500 x player level, and at least twice a Stage 10 chest elite (4,250); see "Elites and bosses in play".
- A level-1 hero dropped into Stage 10 dies in about 11 s.
- The GUI editor ran at 17-38 FPS with 300 Stage 10 enemies before the boss appeared (Performance section).

Not yet played by the owner.

### Run structure

Owner decision (2026-09-24): one run plays through all 10 levels in order. This intentionally differs from Vampire Survivors' separate per-stage runs.

~~Killing a stage's boss clears that stage and starts the next one.~~ Superseded by the owner on 2026-09-27, after VS research. vampire.survivors.wiki/w/Mad_Forest: a 30-minute run; chest bosses at 1, 3, 5, 7, 9, 10, 12, 14, 15, 16, 18, 20, 22, 23, 25 and 29 minutes, plus Arcana-only chests at 11 and 21; at 30:00 the screen is cleared and the Reaper comes.
- Every stage lasts the full 30 minutes, like VS. The stage boss comes at 25:00 (at 20:00 on Levels 5 and 6 since 2026-10-13), and killing it no longer ends the stage (`LevelProgressionManager.IsBossDefeated`).
- At 30:00 the screen is cleared and the next stage starts after a short transition. There is no Reaper; the run moves on instead.
- Killing the Level 10 boss (Shadow Kael, the end of the story) still wins the run.
- Chest elites come at the VS minutes 1, 3, 5, 7, 9, 10, 12, 14, 15, 16, 18, 20, 22, 23, 27 and 29 (`EnemySpawner.DefaultChestEliteMinutes`), 16 per stage. They were every 5 minutes before the boss, and they also come during a boss fight.
  - My first list (1, 5, 10, 11, 15, 18, 20, 21, 29) came from a summarised page fetch and missed rows. The raw wave table gives the list above; the owner chose "the VS minutes", so the corrected list follows that choice.
- A 10-stage run is now about 5 hours.

**The ending (owner, 2026-09-27: "study the full story then finalize the death").** The story says: "The final boss is his own shadow. To free his daughter's soul, he must destroy himself." So:
- Level 10 (The Soul Core) has no time limit. 30:00 no longer wins. Only destroying Shadow Kael ends it (`LevelProgressionManager.CompleteStageAfterSurvival` skips the last stage). In Endless mode, his death starts the next cycle.
- The victory screen reads "SHADOW KAEL DESTROYED" with the twist's own last sentence (`CampaignStory.FinalLine`). It said "ALL TEN STAGES SURVIVED / The pact has reached its end.", which was not the owner's story.
- Tests: `RunModeTests` (T: 30:00 on Level 10 neither wins nor starts a new cycle; the boss kill does) and `CampaignStoryTests` (T: the line is the GDD's).
- EditMode 399/399, PlayMode 147/147, save untouched (`D:\SoulHunter-Validation\run-20260927-ending\`).
- Editor play `autoplay-editor-41`: Level 1, then a jump to Level 10 and a clock skip to 29:50; god mode, 0 errors, min 57 FPS.
  - After 30:00 the run went on at Level 10 ("The Soul Core has no time limit").
  - Shadow Kael (4,500 health) copied the player's 6 weapons.
  - The level-10 bot did not kill him in 3 minutes, so the victory screen was not seen in play; it is covered by tests only.
- Not yet played by the owner.

**Owner decision (2026-10-13): a level's guardian opens the next level.** On Levels 1 to 9 the next level starts at 30:00 only if this level's boss has been defeated; if it still stands, the run ends there. Nothing is saved: every run starts at Level 1. Details: section 7, "Nothing left".

Do not split the campaign into separate per-level runs or add a Reaper-style run end without a new owner decision.

### Weapon aiming

Owner decision (2026-09-24): all of the player's directional attacks fire toward the nearest enemy (`AutoAttackWeapon.AimDirection`), falling back to each weapon's previous direction when there is no enemy. This intentionally differs from Vampire Survivors, where Knife, Whip, Axe and others follow facing or random directions. Applied to Song of Mana (piercing Magic shots), Knife, Thousand Edge, Whip, Bloody Tear, Axe, Fire Wand, Holy Wand, Gun (one arm aimed, 4-way pattern kept), Cherry Bomb, Death Spiral (first scythe aimed), Bone, Runetracer, Clock Lancet, Santa Water (flask lands on the target) and Lightning Ring (nearest enemies first). Non-directional weapons are unchanged (Garlic, Bible, Laurel, Pentagram, Peachone). A boss's mirrored copy aims at the player. Validated by `WeaponAimingTests` (T) and `PlayerLoadoutPlayModeTests` (R); Axe is code-reviewed only (its throw needs a physics step).

Gun, Cherry Bomb and Death Spiral previously built directions as `Vector2`, which became (x, y, 0) and fired vertically instead of across the ground; they now aim on the ground plane.

---

## 6. Performance Contract

Primary planning/release baseline:

- platform: Steam / Windows
- resolution: `1920x1080`
- quality: Ultra on every device (owner decisions 2026-10-18: "quality default Ultra hoga", then "quality should be default ultra in all devises"). `QualitySettings` starts every platform on Ultra (Android, iPhone, tvOS and WebGL were Medium or lower). `PerformanceBenchmark` measures on Ultra. A player build keeps a quality level chosen before between launches. Ultra on a phone is not measured yet (`P`).
- target: `60 FPS`
- hard minimum: `30 FPS`
- low-end planning reference: Intel Core i5-8350U + Intel UHD 620 + 8 GB RAM

Android is secondary. Detailed Android device tiers and budgets remain a later platform-specific acceptance definition.

Do not claim performance acceptance without representative profiling.

Performance smoke test (2026-09-25): `PerformanceBenchmark` runs in a player build launched with `-sh-benchmark <results.json>` (never in a normal launch). It switches to Medium with V-Sync off, renders the 3D view at 1080p's pixel count (URP render scale when the window is smaller), plays as an invincible Kael who keeps walking, takes the first level-up choice and closes chests, and measures 30 s after an 8 s warm-up in three segments. The Windows build (`SoulHunterBuildRunner`, 132 MB, built in the isolated copy) was run on this machine, which is the low-end reference (i5-8350U, UHD 620, 16 GB), in a 1366x768 window rendered at 1920x1080:

| Segment | Enemies | Avg FPS | 1% low FPS | Frames > 33 ms |
|---|---|---|---|---|
| Stage 1 opening | 30 | 226-233 | 152-160 | 0 |
| Stage 1 at 20:30 | 117-184 | 121-141 | 84-88 | 1 |
| Stage 10 at 20:30 | ~300 | 85-88 | 45-51 | 0-1 |

Results are in `D:\SoulHunter-Validation\run-20260925-gaps2\bench1080-1.json` and `-2.json`; the save file was unchanged. The very first launch also had one 152 ms hitch in Stage 10 (likely first-run shader or cache work); it did not repeat. This is partial `P` evidence: 30 s segments, not a full 30-minute run, UI drawn at 768p, V-Sync off, 16 GB instead of 8 GB, and no Android device. The Standalone default quality in `QualitySettings` is still Ultra.

---

## 7. Known Current Defects / Risks

Treat the following as known risks until corrected and revalidated.

### Per-stage EnemyData and elites (fixed statically, runtime unverified)

Each level's Enemy, Elite and Boss gameplay prefab now has its own EnemyData asset in `Assets/_Project/Data/Config/Enemies/`. Bosses drop chests, contact damage comes from `DamageToPlayer`, and stage-boss health comes from its EnemyData. `Enemy_Entity.prefab` (the base template) still uses `Bat_Enemy_Data.asset`.

Elites spawn from `Assets/Prefabs/Enemies/Gameplay/SH10_L01`–`L10_EliteGameplay.prefab`, built like the enemy gameplay prefabs with the elite model as the `SH10_Visual` child. `SH10_Lxx_Elite.prefab` stays a visual-only model used by the showcase scenes.

The stat values are provisional balance numbers. `EnemyDataWiringTests` covers the wiring and passed (T) on 2026-09-24; elite and boss behaviour in play is still unverified (R).

### Bible prefab defect

`Bible_Weapon.prefab` previously had `_biblePrefab` referencing its own root object, and historical runtime evidence included Bible-related exceptions.

`_biblePrefab` then pointed at `SH10_Bible.prefab` with fileID `100100000` (the prefab asset, not its root GameObject), so spawning books still threw `InvalidCastException` (seen 2026-09-24 when a PlayMode chest reward granted the Bible). Fixed in `Bible_Weapon.prefab` and `UnholyVespers.prefab` to the root GameObject (`5068360835409548677`); no other field in the project used that fileID form.

Validated 2026-09-24: `BiblePrefabTests` (T) and the full PlayMode suite with 0 exceptions (R). Bible behaviour in hands-on play is still unverified.

### Whip enemy mask defect

`Whip_Weapon.prefab` shipped with an empty `_enemyLayer`, so the Whip never hit anything. It surfaced once character starting weapons were fixed (Antonio, now Corvan, starts with the Whip). `WhipWeapon` (and `GarlicWeapon`, defensively) now default an empty mask to the Enemy layer. Validated by `PlayerLoadoutPlayModeTests` (R): Corvan is visible, starts with the Whip, and the Whip damages an adjacent enemy.

### All-character sweep (2026-09-24)

`CharacterSweepPlayModeTests` plays every catalog character (13) in the real game with the spawner off and checks: model visible, starting weapon equipped, it damages an enemy 3 units away, the player loses no health, and no errors are logged. All 13 pass. The first run found three more defects, now fixed:

- Bible (Lumina): the `SH10_Bible` book model has no collider, so adding `TouchDamage` (which requires one) returned null and threw `NullReferenceException`. Books now get a trigger collider first.
- Bible: disabling the weapon (scene unload, or evolving into Unholy Vespers) reparented its books while deactivating, which Unity rejects with an error. Books are now only hidden on disable.
- Axe (Gideon): Vampire Survivors' up-the-screen arc had been built as a 3D height arc, so axes flew ~7 units over nearby enemies and landed ~15 units away. Axes now fly along the ground toward the target (gravity off, still spinning); speed 12 is provisional.

### Weapon self-damage defect

`TouchDamage.TargetTag` defaults to "Player" (it was written for enemy contact damage). Song of Mana and Bloody Tear never changed it, so their beams/whips damaged the player; Aria (Song of Mana) died within seconds of starting. All TouchDamage weapons now use `AutoAttackWeapon.HostileTag`: "Enemy", or "Player" when a boss holds the weapon (Shadow Kael's mirror). Validated by `WeaponTargetingTests` (T) and `PlayerLoadoutPlayModeTests` (R: Aria takes no damage from her own beam).

### Pause / time-scale conflicts

Pause menu, level-up, chest, Wandering Merchant, game over, Soul Burst hit-stop and stage start each wrote `Time.timeScale` directly, so one resumed the game underneath another that was still open: toggling pause twice during a level-up/chest/shop, Soul Burst's slow motion ending, or the next stage starting 5 s after a clear. All of them now go through `SoulHunter.Core.Services.GameTime` (reference-counted `Pause(owner)`/`Resume(owner)`, `SetSlowMotion`, `ResetAll` only at run start, restart and scene change). Do not write `Time.timeScale` directly in gameplay code. Validated 2026-09-24 by `GameTimeTests` (T), EditMode 235/235 and PlayMode 20/20 (R) in the isolated copy; hands-on play still unverified.

This came from a review of an external "Vampire Survivors engine" write-up (owner direction: adopt what is officially documented, follow the rest). Officially documented mechanics were already present statically (time survival, hordes, levelled weapons, gems that never despawn, persistent gold power-ups). Of the engineering recommendations, pooling (`WeaponPoolManager`, `PickupPoolManager`), data-driven enemies (`EnemyData`), damage data (`DamagePacket`), death events (`HealthController.OnDied`), multi-level-up and a stage clock already existed; the missing run-state/pause arbitration is what `GameTime` adds.

### Enemy minimum (2026-09-24)

A second write-up (VS Stage 2, Inlaid Library) was reviewed under the same direction. Adopted: the VS "enemy minimum". Each wave phase has `MinimumEnemies`, and `EnemySpawner` refills the swarm up to it immediately (at most `_maxMinimumTopUpPerFrame` = 8 per frame), on top of the interval spawns; before this the horde only grew by one group per interval, so fast killing emptied the screen. The automatic 5-minute phases use `WaveData.MinimumEnemies` (30) + `MinimumEnemiesPerPhase` (20) per phase: 30/50/70/90/110, provisional balance. Validated by `EnemyMinimumTests` (T) and `EnemyMinimumPlayModeTests` (R: opening horde reaches the minimum and refills after a full wipe); EditMode 237/237, PlayMode 21/21. Feel and performance at these counts are unverified (`V`, `P`).

Not adopted: Stage 2 unlocking at player level 20 (conflicts with the one-run 10-level structure); Inlaid Library's wave tables, enemy/item/relic names and ×1.25 speed (VS game data, and taken from the fan wiki rather than poncle); its corridor and left/right spawning (Soul Hunter Level 2 is The Dark Forest).

### Enemy crowd separation (owner report, 2026-09-26)

The owner saw the horde chase the player in single file ("rail ka dabba"), where VS shows a wide crowd. Every enemy homed on the player at the same speed with trigger bodies and nothing kept them apart, so they merged onto one path. VS research (vampire.survivors.wiki; the crowd-pushing part comes from thin forum sources and memory) says enemies push each other into crowds.

`EnemyCrowd` now gives each enemy a soft push away from the enemies it overlaps: 1 - distance / (sum of body radii), capped at length 1. Bodies stay triggers, so the horde never walls the player in. `EnemyChaseState` steers toward the player plus the push × 1.2 (provisional). A crowded enemy slows down but never goes faster than its move speed. Next to the player, enemies still make room for each other. Body radius is the collider radius × scale, so chest elites and bosses push from further out. Event fliers are left out.

Cost: once per rendered frame, the horde is read into arrays, sorted into a flat grid and every push is computed in one pass. The first version took 7.8 ms per step for 300 packed enemies (editor debug scripts) and dropped Stage 10 editor play to 19-27 FPS. The array version takes ~1.15 ms there. `-sh-crowd-off` (or `EnemyCrowd.Enabled`) switches it off for A/B checks.

Validation:
- `EnemyCrowdTests` (T): push maths, stacked pairs part in opposite directions, far enemies unaffected, bigger bodies, and a timing budget.
- `EnemyCrowdPlayModeTests` (R: 40 enemies chase a walking player). Old behaviour: width 0.07, 100% stacked. Now: width 0.98 (about four bodies side by side), 0% stacked.
- EditMode 324/324, PlayMode 87/87, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-crowd\`).
- Editor play `autoplay-editor-13` (Stage 1, 0 errors, min 55 FPS) and `-14` (Stage 10, god mode): the old line became a wide, dense crowd (partial `V`). `AutoplayBot` timed shots now skip paused screens; level-ups every ~15 s used to hide every shot.
- Windows build benchmark (reference laptop, 1080p pixel count, Medium), Stage 10 at 20:30 with ~300 enemies:
  - separation on: 68.2 / 79.4 FPS average, 1% low 32.9 / 61.1;
  - separation off: 84.5 / 76.5 FPS average, 1% low 56.4 / 48.3.
  - The runs are noisy. Separation looks to cost about 0-10% on average. Every run is above the 30 FPS hard minimum, but one 1% low (32.9) is close to it (partial `P`).

Mistake during validation: the build ran without `SOULHUNTER_BUILD_OUTPUT_EXE` and overwrote `D:\Unity Builds\SoulHunter\` (created 2026-09-23). The benchmark copy lives in the run folder.

Part 2, movement types (2026-09-26):
- `EnemyData.Movement` follows the VS movement skills (vampire.survivors.wiki, Enemies):
  - `Homing` (default).
  - `Floaty`: homes while weaving sideways by `FloatyWeave` (0.7) × sin over `FloatyPeriodSeconds` (1.8 s), with a random phase per enemy (provisional values).
  - `FixedDirection`: when spawned, it aims at the player and keeps that line at its own speed. It is not relocated or pushed by the crowd, and it returns to the pool without drops once it has crossed the screen.
- `EnemySpawner.SpawnEnemy` calls `EnemyController.BeginMovement()`. `EnemyFlyState` takes a speed multiplier: 1.5 for event formations, 1 for Fixed Direction.
- Per the approved table, the normal enemies of L2 Dark Forest and L7 Sea of Lost Souls are Floaty. Everything else homes. Fixed Direction is ready but no stage uses it yet (Part 4).

Found while testing Part 2:
- `EnemySpawner` stopped following stage changes once it had been disabled: it unsubscribed in `OnDisable` and only `Start` subscribed again. Game code never disables it, but tests that disabled it kept the old stage's enemies and boss. It now unsubscribes in `OnDestroy`.
- PlayMode tests read the owner's real save, which now selected Kaelen. Kaelen's starting weapon broke tests written for Kael (7 failures). `GameplayPlayModeTestBase.BootIntoGameplay` now plays as Kael and sends every save of the run to a throw-away file.

Part 2 validation:
- `EnemyMovementTests` (T): weave maths, and the approved movement per stage asset.
- `EnemyMovementPlayModeTests` (R): Stage 1 enemy moved 0.00 sideways; Stage 2 Floaty enemy weaved 0.73-0.88 and both reached the player; a Fixed Direction enemy kept its line when the player stepped 5 units aside, then left.
- EditMode 326/326, PlayMode 89/89, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-movement\`).
- Editor play `autoplay-editor-15` (Stage 2, god mode): 0 errors, min 49 FPS, enemies spread around the player (partial `V`). Only up to 39 enemies were alive at once on Stage 2 (Part 3).

Part 3, VS minute waves (2026-09-26):
- Correction: every stage already had its own WaveData (spawn interval 1.0-0.35 s and group size 1-6, rising by stage), not one shared file as first reported. What they shared was the automatic shape: the minimum rose 30/50/70/90/110 every 5 minutes, never falling.
- `WaveData.Minutes` (`MinuteWave`: minute, minimum, interval, elite bonus) is a VS minute table. `GetPhase` reads it when no Phases are authored, and `EnemySpawner` drops the 5-minute group growth when a table is present.
- `WaveTables.Build` gives each stage a 25-minute table (the boss comes at 25:00), all numbers provisional:
  - opening minimum 15 + 5 per stage, peak 150 + 16 per stage (at most 290);
  - a fast build-up (t^0.6) with a ±12% swell that differs per stage;
  - interval from 1.0× down to 0.4× the stage's `SpawnInterval`;
  - every fifth minute, shifted per stage, is quiet: 30% of the minimum and 1.5× the interval, but 30-50% extra elite chance (VS: Mad Forest drops to 10 at 5:00 with tougher enemies).
- Stage 1 opens 17, 39, 47, 50, 16 (quiet), 62 ..., close to VS Mad Forest (15, 30, 50, 40, ..., 10 at 5:00).
- `WaveTableBuilder` (menu Soul Hunter/Content/Build Wave Tables) writes the tables into the 10 stage assets (`WaveTables.StageWaveAssets`).
- Validation:
  - `WaveTableTests` (T): the assets match the builder; every stage rises and has at least 3 quiet minutes with elites; stages differ; `GetPhase` reads the minute.
  - `WaveTablePlayModeTests` (R): the horde swells to a busy minute's minimum; after a wipe in a quiet minute it refills only to the smaller one.
  - EditMode 337/337, PlayMode 103/103, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-waves2\`).
  - Editor play `autoplay-editor-21` (Stage 1, 6 min, god mode): the horde followed the table, 17 → 39 → 48 → 64 → 50 → 16 in the quiet 4:00 minute → 62 → 75, with 0 errors and min 75 FPS.
- Seen, not changed (balance): that run had 229 kills and reached level 8 in 6 minutes; the run before the retune had 1432 kills and level 19. The XP pace changed and needs the balance pass.

Also on 2026-09-26 (owner):
- The Stage 4/5 damage discs were removed (`CampaignHazard` deleted; `PressurePlatePlayModeTests.CastleAndSwamp_HaveNoDamageDiscs_AroundTheStart`).
- The save's Currency was set back from 16004 to 16002 (backup `soulhunter_save.before-gold-fix.json`).
- `AutoplayBot` checks the chest panel with `!= null` (an unassigned `ChestUI._chestPanel` threw in editor play).

Part 4, stage patterns (2026-09-26), following the approved table:
- `WaveData.SpawnSide` / `SideBias`: L4 Ruined Castle spawns 80% from the left/right (world ±X), L6 Frozen Peaks 80% from above/below (±Z), within a 70° fan, and all other stages from all sides (`WaveTables.SideFor`, `SpawnDirection`). Stage-event formations come from the stage's sides too.
- VS rule: a spawn never lands inside a stage obstacle; `PickSpawnPosition` tries up to 6 directions against `GroundCheck`.
- Events are authored per stage by `WaveTables.Events`, written into `WaveData.Events` (all provisional):
  - five events at 2:30 + 5-minute steps, counts 16 upward, 10 upward for the L8 elites;
  - L1 swarms; L2 mostly closing rings; L3 mostly walls; L4 walls; L5 swarms at 1.8× event speed (`StageEventEntry.Speed`); L6 mostly swarms; L7 mostly (ghost) swarms; L8 rings; L9 walls and rings alternating;
  - L10 every kind, nine events every 2:30, counts +8.
- Pace (`CampaignLevelCatalog`): L5 speed 1.25 → 0.9 ("a slow, heavy horde and fast swarms"), L6 1.15 → 1.45. The L4 and L5 modifier texts no longer mention traps or poison zones.
- Stragglers (beyond 40 units) still re-enter ahead of the player on every stage. On a side-rule stage, making them re-enter from the sides let a player who ran up or down leave the whole horde behind (editor play: 9 kills in 3 minutes on the Castle), because the Castle was not yet a walled corridor like VS's Inlaid Library. The owner then approved a real corridor (see "Castle corridor" below).
- Validation:
  - `StagePatternTests` (T): sides, events, assets and pace follow the table; the direction sampler keeps more than 75% to the sides.
  - `StagePatternPlayModeTests` (R): Castle spawns more than 65% left/right; Peaks more than 65% above/below and fast; no village spawn inside a house; Swamp slow with fast swarms.
  - `StageEventPlayModeTests.ScheduledSwarm_FiresOnTheStageClock` now reads the stage's authored first event (16, not the old default 20).
  - EditMode 342/342, PlayMode 107/107, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-patterns3\`).
  - Editor play `autoplay-editor-22` (Stage 4, god mode, 3 min): 0 errors, min 43 FPS, up to 233 enemies. The fleeing bot drags the horde into one blob behind itself, as in VS.
- Seen, not changed: stage events are not capped, so a run can pass 300 enemies briefly (316 seen).

Part 5, VS knockback (2026-09-26, vampire.survivors.wiki Knockback):
- A hit reverses an enemy's movement for 120 ms (`EnemyController.ApplyKnockback`; `EnemyChaseState` moves it away from its target) at move speed × weapon knockback (`DamagePacket.WeaponKnockback`, default 1) × the enemy's knockback taken.
- Knockback taken comes from `EnemyData.Knockback`: 1 for ordinary enemies, 0 for the L7 Sea of Lost Souls ghosts (VS ghosts take none), 0.5 for elites and 0 for bosses (provisional). Bosses are also immune through `KnockbackResistance`. Fliers and frozen enemies are not pushed.
- Garlic pushes weakly (0.3; provisional) but raises an enemy's knockback taken by 0.3 per hit, up to `MaxKnockback` 3 (VS). The value resets when a pooled enemy is reused.
- The old 0.15 s transform slide (`HealthController.KinematicKnockback`), which fought the chase velocity every physics step, is gone. **The player is no longer knocked back:** it used to slide 2 units whenever an enemy touched it, which VS never does.
- Validation:
  - `KnockbackTests` (T): the formula; the per-enemy data; the Garlic cap; a non-enemy never slides.
  - `KnockbackPlayModeTests` (R): a hit enemy backs off, then comes on again after about 120 ms; an L7 ghost is not pushed; Garlic raises knockback taken; the player does not slide when touched.
  - EditMode 346/346, PlayMode 111/111, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-knockback\`).
  - Editor play `autoplay-editor-24` (Stage 1, 3 min, no god mode): the hero survived, reaching level 13 with 670 kills, 0 errors and min 63 FPS.

The owner-approved crowd plan (Parts 1-5) is done. It is not yet played by the owner.

Each stage still has only one normal enemy type and one elite; new enemy models are Art section work.

Not yet played by the owner.

### Castle corridor (owner-approved, 2026-09-26)

L4 Ruined Castle is now a walled corridor like VS's Inlaid Library ("a long hallway ... that restricts movement to left and right"):
- `StageCorridor` (Data/Config) holds the corridor: it runs along world X, centred on the player's Z when the stage starts, with an open half-width of `WaveTables.CorridorHalfWidth` (9 on Stage 4, 18 units wide; 0 = open ground). The next stage ends it.
- `CorridorWalls` (added by `GameSessionManager`) lays 7 castle-wall modules of 20 units along each side and keeps them around the player, so the hallway never ends. The walls are on the `Obstacle` layer: they block the player only; enemies, weapons and projectiles pass over them.
- Everything else keeps inside the corridor: enemy spawns, stragglers, stage events and the boss (from one end) in `EnemySpawner`; Soul Lanterns come only from the left and right screen edges; stage items lie along the corridor near a wall each, and the four Rune Plates lie along it, two each way.
- Stage 4 spawns 100% from the ends (`WaveTables.SideBiasFor`, `Castle Gargoyles.asset` `SideBias` 1).
- Model: `build_light_source.py` also builds the castle wall (crenellated stone with ember slits), imported by `LightSourceImporter` as `Assets/Prefabs/Props/SH10_CastleWall.prefab` (`GameContentCatalog.CastleWall`). **The design is not yet owner-approved.**

Validation:
- `CorridorTests` (T): only Stage 4 is a corridor, ends-only spawning, clamping, a solid 20-unit wall on the Obstacle layer.
- `CorridorPlayModeTests` (R): the walls stop the player going up or down and follow it left and right; every enemy, item and plate is inside the corridor; leaving the Castle opens the ground again.
- Together with Level 7's `SpectralStagePlayModeTests`: EditMode 349/349, PlayMode 133/133, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260927-l7corridor2\`).
- The first run of that suite found that Soul Burst's `_enemyLayer` was empty in `Player.prefab` and `Main_Gameplay`, so the ultimate had never hurt any enemy. `SoulBurstController` now defaults an empty mask to the Enemy layer, as `WhipWeapon` does.

Editor play `autoplay-editor-26` (Stage 4, god mode, 3 min, after the fixes): both walls on screen, the horde inside the hallway, 478 kills, level 13, 0 errors, min 63 FPS (partial `V`).

Seen, not changed: in the earlier editor play `autoplay-editor-25` (Stage 4 jumped to at level 1, god mode) the bot pressed against a wall and kills nearly stopped after 20 s (61 → 74 by 3:00): level-1 weapons (10 damage) against 25-HP Castle enemies. Enemies that chase the player cross the walls, so some stood outside the corridor.

Not yet played by the owner.

### VS stage rules A to Z (owner request, 2026-09-27)

Owner: "ALL STAGE KE BARE MEIN A TO Z PATA KARO ... OR APPLY KARO", then "complete the table" and "extra feature also continue".

Research from vampire.survivors.wiki, fetched 2026-09-27; raw pages are in the session scratchpad under `vs/stages/`:
- the stage system (time limit, stage bonuses, waves, the 300 enemy cap, map events, modes);
- 16 stages, stage by stage;
- chests, pickups, light sources and the level-up screen.

Applied from the gap table (the "extra" features are listed at the end):
1. **Chests** (`ChestLogicController`):
   - Every chest pays VS gold on top of its items: 1 item 100–200, 3 items 300–600, 5 items 500–1000, then Greed. Before, only an empty chest paid a flat 100.
   - An item with nothing left to upgrade pays 25 gold (Big Coin Bag).
   - At most one evolution per chest. Only chests dropped at 10:00 or later, or at 1:00, can evolve (VS Mad Forest evo flags; `CanEvolveAt`, `ChestPickup.CanEvolve`, set by `EnemyDrop`).
   - Not tested: one evolution per chest, and a chest from before 10:00 not evolving, need a full evolution setup. Covered by code review and editor play.
2. **Stage bonuses** (`CampaignLevelCatalog.PlayerSpeedFor` / `GoldFor`, applied in `PlayerController.MoveSpeed` and `GoldRewards.ApplyGreed`):
   - Player speed per stage: ×1.10, 1.25, 1.25, 1.25, 1.40 … 1.40.
   - Gold per stage: ×1.0, 1.0, 1.2, 1.3, 1.4, 1.5 … 1.5.
   - This follows the VS stage list (Mad Forest ×1.1 speed … Cappella Magna ×1.4, DLC stages ×1.5 gold). Values are provisional.
   - Minutes 25–29 keep minute 24's peak wave (VS keeps 100–300 there).
3. **Map events, VS-dense** (`WaveTables.Events`, `StageEventEntry.Repeat` / `RepeatDelaySeconds` / `ChancePercent`, `EnemySpawner.EventFires`):
   - Most minutes from 2:00 to 12:00: 80% chance, fired twice.
   - A ring or wall at 5, 10, 15 and 25.
   - 70% chance with repeats at 13, 17 and 22.
   - Guaranteed repeated swarms at 20, 27 and 29.
   - The chance is divided by Luck, and 100% means guaranteed.
   - Kinds per stage keep the approved table. Events now also run during a boss fight.
   - Firings the Debug clock skipped more than 10 s past are dropped.
   - The 10 stage wave assets were rebuilt with `WaveTableBuilder`.
4. **New light-source pickups** (VS Rosary, Nduja Fritta Tanto, Little Clover and Gilded Clover, with the VS weights and levels, in `LightSourceDrops`). Soul Hunter names are provisional:
   - **Soul Toll** (weight 1, from level 8): kills every enemy on screen; stage bosses are immune. Normal drops (`ScreenWipePickup`).
   - **Hellfire Breath** (weight 1): a temporary weapon (`FireBreath`) for 10 s × Duration, not a weapon slot, and not copied by Shadow Kael.
     - Every 0.5 s it fires 12 (+ Amount) piercing flames, 30 × Might damage each, in a 30° cone at the nearest enemy.
     - The flames deal Magic damage like the Fire Wand, so they hurt Level 7 ghosts.
     - Speed 9 and the 30° cone are Soul Hunter's own numbers; VS gives only "speed 300%".
   - **Luck Charm** (weight 1): +10% Luck for the run.
   - **Gilded Charm** (weight 1, from level 30): pulls every coin on the ground to the player and starts a 10 s Gold Fever (`GoldFever`).
     - During the fever, each kill has a 75% chance to drop a coin worth 0.5 × Greed (at least 1).
     - Picking up a coin, coin bag or rich coin bag adds 0.5, 5 or 10 s, capped at 10 s.
   - The models are placeholder primitives from `ExtraPickupBuilder` (menu Soul Hunter/Content/Build Extra Pickups): `Assets/Prefabs/Items/SH10_Pickup_*.prefab`, referenced in `GameContentCatalog`. **Not owner-approved art.**
   - Not mapped: Luck raising rare light-source drops (no formula on the wiki); Orologion slowing the fever.
5. **Stage clear gold** (`LevelProgressionManager.StageClearGold`): 500 gold, plus 100 per unused revival, when a stage is cleared at 30:00 (or the final boss dies). Greed and the stage gold bonus apply.

Validation: EditMode 377/377; PlayMode 140/140, then the stage fixtures 47/47 after the L10 schedule fix; save untouched (`D:\SoulHunter-Validation\run-20260927-stagetable\`). Committed in `dba31112`.

Editor play `autoplay-editor-37` (Stage 1, no god mode, 6 min, clock skipped to 9:50 at 60 s): 0 errors, level 25, 2,427 kills, min 44 FPS.
- Chest elites came at 1:00 and at 10, 12 and 14 minutes.
- Each chest paid its upgrade plus 100–200 gold.
- Swarms and rings came about every minute.
- The skip made the 3, 5, 7 and 9-minute elites arrive together (a Debug-skip artifact).

Not yet played by the owner.

### Scalability: adding content (owner request, 2026-09-28)

The owner asked for three improvements: wave enemy types, boss ability components and a single weapon definition asset.

**1. Wave enemy types.** VS wiki, Enemies, "Waves": "Each wave specifies a minimum amount and a spawn interval ... If the minimum amount is not met ... enemies will be spawned until the quota is filled. If more enemies than the minimum amount is present, one of each type of enemies in the wave will be spawned."
- `MinuteWave.EnemyTypes` (enemy gameplay prefabs) reaches `WavePhase.EnemyTypes`.
- `EnemySpawner` fills the minimum with the wave's types in turn (`NextWavePrefab`) and, above the minimum, sends one of each type per tick.
- An empty list keeps the old behaviour (the stage's own enemy with the elite chance), so no stage changed yet: new enemy models are Art section work.
- To add an enemy type:
  1. Make its `EnemyData`.
  2. Make a gameplay prefab like `SH10_Lxx_EnemyGameplay`.
  3. List it in `WaveTables.EnemyTypesByStage` (stage → from-minute → prefab paths).
  4. Run Soul Hunter/Content/Build Wave Tables, which checks every path is an enemy with EnemyData.
  - No scene or spawner code changes.
- Tests: `WaveEnemyTypeTests` (T) and `WaveEnemyTypePlayModeTests` (R: a two-type wave fills its minimum with both types, then sends one of each per tick).
- EditMode 410/410, PlayMode 151/151, save untouched (`D:/SoulHunter-Validation/run-20260928-wavetypes/`).

**2. Enemy and boss abilities, data-driven** (VS behaviour catalog from the wiki boss and enemy pages, 2026-09-28):
- `EnemyData.Abilities` is a list of `EnemyAbilitySpec`, run by `EnemyAbilities` (added to every enemy, pooling-safe, pause and freeze aware). The kinds, with their VS sources:
  - **Summon:** Trickster, Directer, Avatar of Gaea.
  - **Shoot:** Twin Skulls 1 s / speed 250 / 25; Twin Snakes 2 s / 140 / 20; The Ender's interval shrinks with health.
  - **Lunge:** Orochimario heads every 5–10 s.
  - **InvulnerablePhase:** The Ender's 90 s shield, which releases the stored damage.
  - **SelfDestruct:** Sig.ra Rossi, 2 s fuse, 50 damage.
  - **HazardZone:** The Ender's 12-damage zones for 10 s.
  - **HalvingPulse:** Cosmic Egg.
- A number left at 0 uses that kind's VS default (`EnemyAbilityDefaults`). Values the wiki does not give are marked provisional in code: lunge speed and time, pulse interval and radius, arm distance, zone radius, summon ring and cap, and the 0.03 units/s-per-VS-speed conversion.
- To give a boss an ability: open its EnemyData, add an Abilities entry, pick a Kind and leave the numbers at 0 for VS. No boss has an ability yet; that is a content decision.
- **VS resistances** (Enemies page), new `EnemyData` fields:
  - `FreezeResistance`: weapon freezes (Clock Lancet, chance 1.0; the wiki gives none) fail when the resistance is higher. Orologion ignores it.
  - `InstantKillResistance`: Pentagram and Soul Toll skip the enemy (the stage-boss skip stays too).
  - `DebuffResistance`: blocks Garlic's knockback and freeze-resistance reductions. Garlic now also lowers freeze resistance by 0.1 per hit (Garlic page).
  - All 10 bosses have freeze resistance 1.1 (VS), so **Clock Lancet no longer freezes bosses**.
  - Stages 2–10 bosses also resist instant kill and debuffs. Stage 1 does not, like the Giant Blue Venus.
- **Bosses come back:** VS wiki (Enemies, Bosses): "Bosses do not despawn when the player moves away from them, but get teleported back to the screen." Straggler relocation now includes bosses; before, a boss left behind stayed there.
- Tests:
  - `EnemyAbilityTests` (T).
  - `EnemyAbilityPlayModeTests` (R):
    - summon up to the cap, a bullet that hurts the player, a lunge, the shield releasing its damage, a self-destruct after its fuse, a zone, and the pulse halving health;
    - nothing runs while paused or frozen;
    - the resistances hold in play.
  - `ChestEliteAndBossPlayModeTests.ABossLeftBehind_IsBroughtBackToTheScreen` (R).
- EditMode 417/417, PlayMode 161/161, save untouched (`D:/SoulHunter-Validation/run-20260928-abilities/`).

**3. One weapon definition asset per item, and VS level-up weighting.**
- `WeaponDefinition` (Create > Soul Hunter > Weapon Definition) holds an item's type, weapon or passive, name, description, icon, prefab, max level, VS rarity and level assets.
- The 36 definitions are in `Assets/_Project/Data/Weapons/Definitions/`, listed in `GameContentCatalog.WeaponDefinitions`. `WeaponDefinitionBuilder` (menu Soul Hunter/Content/Build Weapon Definitions) builds them from the old wiring; it reads the scene without saving it.
- `WeaponManager` (prefab, weapon or passive, max level) and `UpgradeText` (name, description) read the definition first and fall back to the old fields, so existing content behaves the same.
- To add a weapon or passive:
  1. Add the enum value at the **end** of `UpgradeData.UpgradeType` (it is stored as an int).
  2. Write the script and prefab (a passive needs its stat case in `WeaponManager.ApplyPassiveStat`).
  3. Make its level UpgradeData assets.
  4. Make one WeaponDefinition with the VS rarity, citing the wiki page, and add it to GameContent.
  - No scene binding is needed.
- **VS level-up weighting** (wiki Level_up):
  - Items are drawn from one pool by rarity: P = itemRarity / poolWeight.
  - Rarities come from each item's wiki infobox (`UpgradeRarity.VsRarity`), for example Whip 100, Garlic 70, Pentagram 60, Peachone 50, Bone 1, Cherry Bomb 1, Hollow Heart 90, Wings 50, Tirajisú 40, Metaglio 10.
  - While the inventory is not full, two extra checks each offer an owned item with chance 1 + 0.3x − 1/totalLuck (x = 2 on even levels, 1 on odd).
  - Chests use the weighted draw without those checks.
  - Visible effects:
    - Bone and Cherry Bomb (rarity 1) almost never appear in level-ups, as in VS; the Level 3 merchant sells both.
    - Owned items are offered more often.
- Stone Mask has no Soul Hunter item.
- **Passives on VS values** (owner, 2026-09-28: "passives ka max level VS jaisa theek karo"; each item's wiki infobox):
  - **Max level** (`UpgradeRarity.VsPassiveMaxLevel`, in the definitions and the builder):
    - 5: Spinach, Armor, Hollow Heart, Pummarola, Empty Tome, Candelabrador, Bracer, Spellbinder, Wings, Attractorb, Clover, Crown, Skull O'Maniac.
    - 2: Duplicator and Tirajisú.
    - 9: the Metaglios.
    - They were 8.
  - **Hollow Heart:** max health × 1.2 per level, without filling the gain ("does not heal to fill up the newly increased Max Health"). It was +20 flat with a heal.
  - **Attractorb:** Magnet multiplied by 1.5, then 1.33, 1.25, 1.2 and 1.33 (3.98× at level 5; `WeaponManager.AttractorbStep`, `PlayerStats.MultiplyMagnet`). It was +25% per level.
  - **Metaglio Left/Right:** level 1 gives nothing; the bonus starts at level 2.
  - The other passives already matched the wiki's per-level bonuses.
  - **Tirajisú leaves the inventory** (wiki Tirajisú tips, 2026-09-28): when a revival uses up the last Revival from any source, `PlayerController` calls `WeaponManager.ReleaseSpentTirajisu`. Its level goes back to 0, its passive slot frees up, `WeaponManager.OnItemRemoved` fires, and level-ups can offer it again from level 1. The wiki's stage-item Tirajisú does not apply: no Soul Hunter stage places one.
  - **Armor's retaliatory damage** needed no change. The wiki (Armor (item), Armor (stat)) says Armor "does not deal retaliatory damage by itself"; it adds +10% per point (capped at 50 Armor) to other retaliation sources. Soul Hunter's only one is Crimson Shroud, which already applies it (`CrimsonShroudWeapon.CalculateRetaliationDamage`). NO FUTURE and Heart of Fire have no Soul Hunter counterpart.
  - **Defect found by the Tirajisú PlayMode test: passives did nothing in the real game.** `Main_Gameplay` keeps `Weapon_Manager` under `--- MANAGERS ---`, and `WeaponManager.ApplyPassiveStat` looked for `PlayerStats` only in its parents, found none and returned silently. The level went up, but no stat changed (Spinach, Armor, Hollow Heart, Tirajisú, all of them). The old full PlayMode run logged "Passive Applied" 0 times. The EditMode passive tests put the manager on the player, so they missed it. It now falls back to `PlayerController.Instance`. `PassivePlayModeTests` (R): Spinach, Armor and Hollow Heart change the real player's stats. Every earlier passive balance note (for example the bot runs) was measured without passive effects.
  - **HUD inventory row** (owner: "HUD mein weapon aur passive slots bana do", 2026-09-28). The HUD had never shown an item: `HUD_Canvas.prefab` had empty `_weaponSlots`/`_passiveSlots` and no `WeaponManager`. `InventoryHud` (built by `GameHUDController`) shows 6 weapon and 6 passive slots (wiki Passive items: 6 passive slots) in the order gained (`WeaponManager.GetInventory`), redrawn on `WeaponManager.OnInventoryChanged` (gain, level-up, evolution, union, stage passive, Tirajisú leaving). A union takes its first weapon's slot with the union icon.
    - The wiki does not describe the HUD layout. Provisional Soul Hunter choices: the rows hang 22 units under the Gold label and follow it, because the labels hang from a fixed box at the screen centre and move with the aspect ratio (a fixed spot overlapped them off 16:9). An evolved weapon keeps its base icon with a gold frame, because evolutions have no icons (Art section). Metaglio Left/Right have no icon and show an empty frame. Stage passives beyond 6 are not shown.
    - Tests: `InventoryListTests` (T) and `InventoryHudPlayModeTests` (R: Kael's Whip shows at start, new items appear in order with their own icons, the row sits under Gold). EditMode 460/460, PlayMode 166/166, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20260928-inventoryhud2/`). Editor play `autoplay-editor-54` (Stage 1, 5 min): 0 errors, min 35 FPS; screenshots show the Whip alone at 0:12 and 6 weapons + 3 passives at 4:38 (partial `V`).
    - Seen, not changed: that run reached level 40 and 8,129 kills in 5 minutes, against level 20 and 1,392 kills in `autoplay-editor-53` with the same code apart from the HUD. It needs a look in the balance section.
  - Tests: `PassiveVsTests.Tirajisu_LeavesTheInventory_OnlyWhenTheLastRevivalIsUsed` (T) and `TirajisuPlayModeTests` (R: level 2 gives 2 revivals; the first death keeps it, the second removes it, then it can be taken again).
  - EditMode 457/457, PlayMode 165/165, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20260928-tirajisu3/`). Editor play `autoplay-editor-53` (Stage 1, 5 min, no god mode): level 20, 1,392 kills, 0 errors, min 67 FPS; Armor and Candelabrador were taken and applied their stats.
  - Tests: `PassiveVsTests` (T).
  - `StoryCardPlayModeTests` was made steady: a kill by the starting weapon could open a level-up screen, and the pause stopped the card's countdown. The test now turns the weapons off.
  - EditMode 456/456. The full PlayMode run gave 162/163 (the story-card test), and that test passed alone after the fix. Save untouched (`D:/SoulHunter-Validation/run-20260928-passives/`).
- Tests: `WeaponDefinitionTests` (T) and `WeaponDefinitionPlayModeTests` (R: every definition's prefab matches the old wiring; real level-ups offer valid, distinct items). `LevelUpCurveTests` now uses the VS rarity values.
- EditMode 435/435, PlayMode 163/163, save untouched, scene unchanged (`D:/SoulHunter-Validation/run-20260928-weapondefs/`).

### Balance pass, Part 3: enemy pressure along the stages (2026-09-28)

Measured with the bot, no god mode. `-sh-autoplay-start-level N` starts the hero at level N, so later stages can be played at a believable strength.
- Before:
  - Stage 1 over 30 min (`autoplay-editor-43`): level 83, hurt 5 times, at most 183 enemies.
  - Stage 5 from level 110 over 15 min (`-44`): hurt 9 times, at most 180 enemies.
  - Stage 10 from level 140 (`-45`): **dead after 2:27**, with 289 enemies at 2:00 and a Soul Meteor volley of 8.
- Changes:
  - Every stage's wave table now peaks at the 300 enemy minimum. VS Mad Forest reaches 300 from 11:00; the old peak was 150 + 16 per stage, 166 on Stage 1. The 10 wave assets were rebuilt.
  - Soul Meteors come every 4 minutes from 4:00 with 6 markers (VS Gallo Tower has stars at only a few set minutes). They were every even minute from 2:00 with 8.
- After:
  - Stage 1 over 15 min (`-46`): level 50, 11,650 kills, up to 233 enemies, hurt 4 times, min 33 FPS in the GUI editor.
  - Stage 10 from level 140 over 10 min (`-47`): survived; hurt 100 times, health between 79 and 100, up to 327 enemies, min 9 FPS in the GUI editor (27–47 FPS at the 2-minute samples).
  - The gem count stayed at or under 400.
- XP per stage was not changed. By about level 80 every weapon and passive is maxed, and after that VS level-ups also give only gold or a chicken.
- EditMode 399/399, PlayMode 149/149, save untouched (`D:\SoulHunter-Validation\run-20260928-balance3\`).
- **Waves during boss fights:** the spawner stopped the minute waves while a boss lived, so a boss stood alone on screen for minutes. VS keeps the waves going (Mad Forest's minimum is 100 at 25:00), and now so does Soul Hunter. `ChestEliteAndBossPlayModeTests.TheHorde_KeepsComing_DuringABossFight` (R).
- `AutoplayBot` now picks level-ups through the level-up screen. It used to call `SelectUpgrade` directly, which left the panel open after the last level-up (a bot artifact; a player's click closes it).
- EditMode 399/399, PlayMode 150/150, save untouched (`D:\SoulHunter-Validation\run-20260928-bosswaves\`).
- **Boss fight length (Part 4), measured with the bot in god mode:**
  - Stage 1 boss (3,150 health at level 63): 50 s.
  - Stage 5 boss (27,500 at level 110, `autoplay-editor-50`): 310 s.
  - Shadow Kael (75,000 at level 150, `-49`): 190 s, and the run ended in victory.
  - VS boss fights usually take well under 2 minutes (from memory, not a source). The bot runs from bosses, so these times are rough.
  - Health (50 × stage × level) is the owner's story choice; any change waits for the owner.

### Death Spiral crash and pooled whips (found 2026-09-28)

A full 30-minute Stage 1 editor run (`autoplay-editor-42`, no god mode) logged 30,204 errors, and the frame rate fell from 80 to 20.
- **Death Spiral** (the Axe evolution) threw a `NullReferenceException` on every attack. Its scythe is the SH10 model prefab, which has no collider. `Projectile` requires one, so `AddComponent<Projectile>()` returned null. Scythes now get a trigger collider first.
- **`AutoAttackWeapon.Update`** set the cooldown only after `Attack()`, so an attack that threw was retried every frame. The cooldown is now set first, for every weapon.
- **`WeaponPoolManager.GetFromPool`** could hand back a destroyed object. Bloody Tear parents its pooled whips to itself, so they die with the weapon (for example when a union replaces it). Destroyed entries are now skipped.
- New `EvolvedWeaponSweepPlayModeTests` (R): every evolution and union weapon must hurt enemies within 10 s without errors. Evolutions had no sweep before.
  - On the old code it fails with the `NullReferenceException`.
  - After the Death Spiral fix it caught the Bloody Tear `MissingReferenceException`.
  - It passes on the new code.

VS gem cap (vampire.survivors.wiki/w/Experience_Gem): past 400 gems on the ground, new XP goes into one gem instead of new gems (`PickupPoolManager.MaxGemsOnGround`). The rerun after the fixes (`autoplay-editor-43`) had 0 errors but still fell to 23–40 FPS in the last 10 minutes with only ~43 enemies; uncollected gems are the suspect, so `AutoplayBot` now also logs the gem count. `GemCapPlayModeTests` (R): 600 gems keep at most 401 on the ground and lose no XP. EditMode 399/399, PlayMode 149/149, save untouched.

Balance baseline from the first run: level 70 at 30:00, 18,350 kills, 17 chests, hurt 14 times, never below full health at the timeline samples; the stage boss (3,150 health) lived 50 s.

### VS extra features: run modes and stage hazards (owner: "extra feature also continue", 2026-09-27)

**Run modes** (VS "List of Modes"). Toggled on the character select screen (a row above the character grid), saved in `GameData` (`ModeHurry` … `ModeLimitBreak`; an older save loads them as off). They are copied into `RunModes` when `LevelProgressionManager` wakes and reset when it is destroyed. Numbers are the VS ones unless marked:
- **Hurry:** the stage clock runs 2× (`LevelProgressionManager.AdvanceClock`); XP +25%.
- **Hyper:** per-stage VS values since 2026-10-09 (release wave 1, P1-18); gold +50%, enemy minimum ×1.25 (no faster spawn interval).
- **Inverse:** enemies start at +200% health and gain +5% health and +0.5% speed per stage minute; gold +200%; Luck +20% (applied once in `PlayerStats.Start`). The flipped, darker VS map is not built.
- **Endless:** clearing Level 10 no longer wins. `RunModes.EndlessCycle` goes up and Level 1 starts again. Each cycle adds +100% enemy health, +50% spawn rate and count, and +25% enemy damage.
- **Arcana:**
  - One random Arcana at the start of the run.
  - Arcana chests at 11:00 and 21:00, carried by chest elites (`EnemyDrop.CarryArcanaThisLife`, `ChestPickup.IsArcana`, `ChestLogicController.ProcessArcanaChest`), up to 3 Arcanas.
  - **Changed:** `ArcanaManager` used to give an Arcana at 11:00 and 21:00 of every stage without any mode. VS gives none without the mode, so that is gone.
  - Since 2026-10-09 the start-of-run Arcana is chosen and Arcana chests offer a pick of 4; see "all 22 Arcanas" under the release push.
  - The four Arcanas Soul Hunter has follow their wiki pages since 2026-10-09; see "Arcanas" under "Release push, wave 1".
- **Limit Break:** once nothing is left to level, level-ups offer "LIMIT BREAK" cards: +10% damage to one held weapon per card (`LimitBreak`, hooked in `AutoAttackWeapon.ScaledDamage`; provisional). VS's random stat per card is not built.
- The Results screen shows the active modes and the Endless cycle (`RunResults.ModeText`). The HUD does not: VS shows none there (the wiki is silent, balance method).
- Hooks: `RunModes` is read by `EnemySpawner`, `EnemyController`, `PlayerExperience`, `AutoAttackWeapon` and `CampaignLevelCatalog.CurrentGold` / `CurrentPlayerSpeed`.
- `GameplayPlayModeTestBase.BootIntoGameplay` turns every mode off for the other tests.

**Stage hazards** (`StageHazards`, added by `GameSessionManager`; table in `StageHazardTable`). All numbers are provisional, and placement follows the story's stages:
- **Death's Shadow** (VS Stalker): Stages 5 and 9 at 8:00 and 12:00. Since 2026-10-09 it is the VS Stalker XL; see "Stalker XL" under "Release push, wave 1". The old version (30 damage, unkillable, always spawned) is gone.
  - The stage boss model at 1.5×, tinted dark, 18 units out.
  - It is not a stage boss. It restores the pooled enemy when it goes.
- **Soul Meteors** (VS Shooting Star): Stages 6 and 10, every even minute from 2:00 to 28:00.
  - A volley of 8 ground markers (the first on the player); each strikes 2 s later for 30 to the player (minus Armor) and 30 to enemies within 1.6.
  - The markers use the existing VFX magic circle.
- **Pile Assault** (VS): Stage 8 at 9:00, 10:00, 12:00 and 20:00.
  - 12 stage enemies on an ellipse stand still and each fire a slow bullet (10 damage) every 2.5 s.
  - They can be killed; the survivors leave after 20 s.
- **Rail carts** (VS Dairy Plant carts): Stage 3, two 24-unit rails near the start.
  - Weapons empty a cart's 30 HP to launch it; it runs along the rail dealing 200 to each enemy it touches, then waits at the far end.
  - It never hurts the player.
- Placeholder art from `StageHazardBuilder` (menu Soul Hunter/Content/Build Stage Hazards): `SH10_RailCart`, `SH10_HazardMarker`, and the hazard bullet (a tinted Magic Wand bolt). **Not owner-approved art.**
- Found in validation: `RailCartTrack` was declared inside `RailCart.cs`, so Unity could not save it on the prefab (a MonoBehaviour needs its own file). It now has its own file.

Validation:
- EditMode 399/399 after that fix. The full PlayMode run gave 147/147; after the fix, the hazard and run-mode fixtures gave 7/7. Save untouched (`D:\SoulHunter-Validation\run-20260927-extras\`).
- Editor play, god mode, 2.5 min each, 0 errors:
  - `autoplay-editor-38`, Stage 5 (clock at 7:50): Death's Shadow appeared at 8:00. Min 42 FPS, up to 317 enemies.
  - `-39`, Stage 6: a Soul Meteor volley at 2:00 struck. Min 38 FPS.
  - `-40`, Stage 3: both rail carts lie near the start and are visible. Min 63 FPS.
  - The shadow and meteors are confirmed in the log; in the screenshots they are hidden by level-up screens or off-frame (partial `V`).
- Run modes are covered by tests only; the bot does not turn modes on.

Not yet played by the owner.

### Balance pass, Part 1: weapons on VS numbers (owner request, 2026-09-27)

Owner: "balance pass shuru karo". Balance follows the VS rules plus the owner's story.

Baseline before Part 1 (`autoplay-editor-35`, Stage 1, 15 min, no god mode, old weapons): level 117, 63,998 kills, never more than 89 enemies, hero never hurt, min 8 FPS.
- Most weapons used placeholders (10 damage, 1.5 s).
- Pentagram erased the map every 1.5 s, and its level-ups took 5 s per level off that, going below zero.
- Holy Wand fired every 0.1 s and Thousand Edge every 0.05 s.

Now every weapon's level-1 damage, cooldown, amount, pierce and duration, and its level 2-8 table, follow vampire.survivors.wiki (fetched 2026-09-27; raw pages in the session scratchpad). Each script names its numbers as constants with a "VS wiki" comment and has `MaxLevel` 8 (7 for Clock Lancet and Laurel).
- Soul Hunter keeps its own level-1 Area and Speed; VS Area/Speed level-ups apply as percentages on top.
- Examples:
  - Whip 1.35 s; Axe 20 / 4 s with VS pierce (new `AxePierce`, an axe leaves after 3 enemies); Cross 5 / 2 s; King Bible 10, 1 book; Garlic 1.3 s.
  - Knife 6.5 / 1 s; Fire Wand 20 / 3 s, 3 shots; Santa Water 10 / 4.5 s; Lightning Ring 15 / 4.5 s; Peachone 10 / 1 s, 4 bombs.
  - Pentagram 90 s, 10% keep chance; Clock Lancet 2 s / 2 s freeze.
  - Evolutions: Thousand Edge 16.5 / 0.35 s ×6; Bloody Tear 40; Unholy Vespers 30; Heaven Sword 77 / 3.3 s; Death Spiral 60 / 4 s; Soul Eater 20, 3× Garlic's radius.
- Not mapped (listed in the scripts):
  - VS hit intervals and per-weapon knockback.
  - King Bible's on/off duration (our books always orbit).
  - Cherry Bomb's explode chance (our bomb only deals contact damage).
  - Pentagram's 1 s duration and its erasing of items already on the ground.
  - Peachone's duration.
  - Damage rounds down to whole numbers in `Projectile`, so Knife hits for 6 before Might.
- Kept although VS differs: Soul Eater and Bloody Tear healing (Soul Hunter's own), all aiming at the nearest enemy (owner decision), and the Gun's 4-way pattern (VS Amount counts per arm).

Validation:
- `WeaponLevelTableTests` (T) was updated to the VS tables (Garlic, Axe, Bible).
- `WeaponSweepPlayModeTests` (R) now resets a weapon's timer after placing its targets, because the first shot fires on equip and Pentagram's next shot is 90 s away.
- EditMode 353/353, PlayMode 135/136. The one failure was that sweep case (Pentagram); after the fix the sweep fixture passed 21/21. Save untouched (`D:\SoulHunter-Validation\run-20260927-weapons\`).
- After (`autoplay-editor-36`, same run): level 43, 9,484 kills, up to 118 enemies, min 43 FPS, 0 errors. That is about what a VS player reaches by 15:00 (from memory, not a source).
- Seen for later parts: the hero was hurt only twice in 15 minutes (enemy pressure, Part 3).

Not yet played by the owner.

### Boss level scaling and chest elites (owner decisions, 2026-09-24)

Found while reviewing the VS Stage 4/5 write-ups: bosses had fixed health (1000 x stage) although one run keeps levelling across 10 stages, and only stage bosses dropped chests (~10 per run), so evolutions were rare. The owner chose:

- **Boss HP x player level:** `EnemyData.HealthPerPlayerLevel` (> 0 means max health = it x player level, then Curse). All ten stage boss assets use 50, which reproduces the old values if the player gains ~20 levels per stage; provisional balance. Shadow Kael scales too, on top of mirroring the loadout.
- **Timed chest elites:** every `_chestEliteIntervalSeconds` (300 s) of stage time before the boss (5/10/15/20 min), `EnemySpawner` spawns the stage elite with 5x health and 1.5x size that always drops a chest (`EnemyDrop.DropChestThisLife`, cleared on respawn so pooled reuse never drops extra chests). Not blocked by the enemy cap. Values provisional.

Validated by `BossScalingAndChestEliteTests` (T) and `ChestEliteAndBossPlayModeTests` (R); EditMode 246/246, PlayMode 23/23. Balance and feel in real play are unverified.

### Elites and bosses in play; the owner's story (2026-09-27)

Elites and bosses were watched in editor play for the first time. `AutoplayBot` now records `elite-spawn`/`boss-spawn` (health, size, distance, player level), `-killed` (time alive) and `-despawned` events with screenshots. `-sh-autoplay-skip-to T` (after `-sh-autoplay-skip-after S`, default 10) moves the stage clock through `LevelProgressionManager.SkipTo`. Helpers: `EnemyDrop.WillDropChest`, `EnemySpawner.IsBoss`.

First look (`autoplay-editor-28` Stage 1, no god mode; `-29` Stage 4, god mode; 0 errors):
- The 5:00 chest elite came (5x health, 1.5x size), died in 21.8 s (Stage 1) and 94 s (Stage 4), and dropped a chest.
- The boss came at 25:00 (3x size; from one end of the Castle corridor), dropped a chest, and the next stage started.
- Found:
  - the Stage 1 boss died in 6.4 s;
  - the Stage 4 boss (600) had less health than its chest elite (625);
  - elites and bosses dropped only a chest, so their soul gems (50 and 250 XP) never appeared.

Owner decision (2026-09-27): "MERE STORY KO FOLLOW KARO", i.e. follow the story in `Docs/Game_Design_Document.md`; "STORY MEIN NO CHANGES" (do not edit the story; the old GDD level mechanics stay as the earlier owner decisions left them). The owner chose:
- ~~**Bosses grow with the story:** 50 × stage × level, never under twice a chest elite's.~~ Replaced on 2026-09-28 by the owner's choice "pura VS". Bosses and chest elites now use the VS wiki's HP x Level values (raw boss pages fetched 2026-09-28):
  - **Stage bosses, per level:** Stage 1: 150 (Mad Forest Giant Blue Venus); Stages 2–3: 250 (Inlaid Library Hag, Dairy Plant Sword Guardian); Stages 4–10: 500 (Gallo Tower Giant Enemy Crab, Mt.Moonspell Orochimario).
    - Later guardians are never weaker, per the story; Cappella Magna's 250 would have dropped.
    - Shadow Kael stays at 500.
    - The "twice a chest elite" floor is gone: in VS, Mad Forest's 20:00 Giant Mummy (250) is stronger than its 25:00 boss (150).
  - **Chest elites** (`EnemySpawner.ChestEliteHealth`, VS Mad Forest chest bosses):
    - 50 × level at most minutes (Glowing/Silver Bat), and at the Arcana minutes.
    - 150 × level at 10:00 (Giant Mantichana), 200 × at 15:00 (Giant Werewolf), 250 × at 20:00 (Giant Mummy).
    - A flat 500 at 5:00 (Mantichana, no HP x Level).
    - The old rule was 5 × the stage elite's EnemyData health.
  - **Maxed-out level-ups** (VS wiki Level_up): with nothing left to upgrade, the level-up screen offers two cards, "Gold Pouch" (+25 gold, the Big Coin Bag) or "Roast Chicken" (30 health, the Floor Chicken), and the player picks one (`MaxedRewards`). Before, both were given silently with no screen. The VS "always gold/chicken" option after 100 Big Coin Bags is not built.
  - Pickup values checked against the wiki: chicken 30, Orologion 10 s, coins 1/10/100, Big Coin Bag 25. All match.
  - Validation: EditMode 407/407 (the banish test now expects the reward cards), PlayMode 150/150, save untouched (`D:/SoulHunter-Validation/run-20260928-vsboss/`).
  - Editor play `autoplay-editor-51` (Stage 1, god mode, clock skipped to the boss at level 20): 0 errors, min 49 FPS.
    - The boss had 3,000 health (150 × 20) and died in 87 s.
    - The chest elites matched the VS values: 200 at 1:00 (level 4), 700 at 3:00, 500 at 5:00, 3,000 at 10:00.
- **Souls:** a boss or chest elite drops its soul gem (`EnemyData.ExperienceValue`) next to its chest (`EnemyDrop`).
- **The story is shown:** `StoryCardUI` (added by `GameHUDController`) shows "Level n: name" and the story when each stage starts, with the premise on Level 1 and the twist on Level 10. It does not pause and waits while a menu is open. The text is word for word from the GDD: `CampaignLevelCatalog` narratives now use the GDD's story lines, plus `CampaignStory.Premise` and `Twist`. The story is about Kael whichever character is played.

Validation:
- `CampaignStoryTests` (T: every line matches the GDD text), `BossScalingAndChestEliteTests` (T: 50 x stage, the elite floor) and `StoryCardPlayModeTests` (R: cards on Levels 1, 4 and 10, no pause, the card goes away).
- `ChestEliteAndBossPlayModeTests` (R): the elite also drops its gem; the boss has the story health and outlasts its chest elite.
- EditMode 353/353, PlayMode 134/134, 0 exceptions, save untouched (`D:\SoulHunter-Validation\run-20260927-story\`).
- Editor play `autoplay-editor-30` (Stage 1, no god mode, 0 errors): the story card opened the run; the elite died in 18.8 s; the boss (1,000 health at level 20) lived 24.9 s.
- `autoplay-editor-31` (Stage 10 jumped to at level 1, god mode, 0 errors): the twist card showed; Shadow Kael had 4,250 health (twice the 2,125 chest elite) and a level-1 hero could not kill him in 150 s.

The owner's own checks, played by the agent (owner: "DO ALL", 2026-09-27). New bot options: `-sh-autoplay-jump-to N` / `-jump-after S` (the Debug "Jump To Stage" action), and `elite-drops`/`boss-drops` events counting soul gems and chests near the body.
- `autoplay-editor-32` (new run from Level 1) found a real defect: Pentagram erased the 5:00 chest elite in 1.1 s and the Stage 1 boss in 0.2 s, with no chest and no gem. It also switched `EnemyDrop` off, and the component stayed off when the pool reused the enemy, so that enemy never dropped a gem or counted a kill again.
  - Fixed per VS (vampire.survivors.wiki/w/Pentagram, checked 2026-09-27): stage bosses are immune (`EnemyController.IsStageBoss`, set by `SpawnBoss`). An erased enemy loses only this life's drops (`EnemyDrop.EraseDropsThisLife`) and still counts as a kill. An erased chest carrier's chest passes to the next ordinary enemy the horde spawns (`EnemyDrop.PendingChests`, `EnemySpawner.GivePendingChest`).
  - `PentagramPlayModeTests` (R) fail on the old code (boss erased; pooled `EnemyDrop` off) and pass on the new.
- `autoplay-editor-33` (new run from Level 1, jump to Stage 10 at 20 s, Skip To Stage Boss at 60 s, god mode, 0 errors): the twist card showed, and Shadow Kael (4,250 health) copied Whip and Clock Lancet.
  - Found: an open level-up screen showed the story card through it. The card now hides while a menu has the game paused and comes back afterwards (`StoryCardPlayModeTests`).
- `autoplay-editor-34` (new run from Level 1, no god mode, 0 errors): story card; the 5:00 chest elite dropped its chest and 50 XP soul gem; the boss (1,000 health) dropped its 250 XP gem and a chest that the hero picked up at once. The boss died in 4.5 s, at player level 20.
- EditMode 353/353, PlayMode 136/136, save untouched (`D:\SoulHunter-Validation\run-20260927-pentagram\`). The later story-card change was rerun with its fixture and `PentagramPlayModeTests`: 3/3.

Not yet played by the owner.

Stage 3-5 write-ups (Dairy Plant, Gallo Tower, Cappella Magna) added no other adoptable official mechanics; their stage data, names, unlock levels and Hyper/Hurry modes are VS content or conflict with the run structure.

### Stage events (owner request, 2026-09-24)

VS-style scripted formations on the stage clock, in all 10 levels, using that stage's enemies (`PickSwarmPrefab`, so Blood Arenas stays elite-only and elites can appear): `Swarm` (tight cluster crossing the player's position), `Wall` (line across the travel direction, 1.5 apart) and `ClosingRing` (circle at the spawn radius flying inward, so it closes and opens out). Members use `EnemyFlyState`: fixed path at 1.5x speed, facing their path, not relocated as stragglers, returned to the pool without drops after 15 s. `WaveData.Events` holds authored events; empty means the default schedule 2:30 Swarm 20, 7:30 Wall 16, 12:30 Ring 16, 17:30 Swarm 24, 22:30 Wall 20 (between the 5-minute chest elites, before the boss). Events are not blocked by the enemy cap. Counts/times are provisional.

`EnemyFlyState` existed unused and its `Exit` set the collider solid, which would have made every pooled flier a wall; fixed (enemy bodies stay triggers). A Time Freeze or Clock Lancet hit used to send fliers to the chase state afterwards; now `EnemyController.Freeze` remembers the flight, so a frozen flier stays in place, is not relocated as a straggler and resumes its path and lifetime when the freeze ends (`StageEventPlayModeTests.FrozenFliers_ResumeTheirPath_AfterTimeFreeze`, R, 2026-09-25). The occasional "member did not move" failure of `EveryFormation_FliesItsPath` came from the player's starting weapon killing members that crossed the player; the test now disables the player's weapons.

Validated by `StageEventTests` (T) and `StageEventPlayModeTests` (R: the 2:30 swarm fires on the stage clock; every formation flies; pooled fliers stay triggers); EditMode 251/251, PlayMode 25/25. Look and feel in real play unverified.

### Audio

No scene contains an `AudioManager`; it creates itself from `Assets/Resources/AudioManager.prefab` at startup (`RuntimeInitializeOnLoadMethod`). Found 2026-09-25: that prefab was hand-written YAML with corrupted file IDs, which Unity imported as a DefaultAsset, so `Resources.Load` returned null, no AudioManager ever existed and the game had no sound at all (every PlayMode log showed "prefab not found"). Rebuilt with Unity's prefab writer (`AudioManagerPrefabBuilder`, same GUID). Enemy hits now play `EnemyHit` (throttled to one per 0.06 s via `PlaySFXThrottled`). Validated by `AudioPlayModeTests` (R: one listener, all five effects assigned and not silent, hitting an enemy plays EnemyHit); batchmode cannot confirm speakers, so hearing it is still unverified.

(Superseded 2026-09-25 by CC0 recordings; see "Look, feel and sound pass". The synthesised clips remain as a fallback.) Missing audio was then synthesised in code (owner choice 2026-09-25, retro style, no external samples): `RetroAudioSynth` (menu Soul Hunter/Content/Generate Retro Audio) writes 12 effects (EnemyDeath, PlayerHurt, GameOver, StageClear, BossWarning, UIClick, UIConfirm, Unlock, Shoot, Slash, Magic, Explosion) and 11 music loops (menu + one per stage, each with its own key, tempo, scale and progression; loops end at silence so they repeat without a click), and adds a 3 ms fade-in to the five older effects that started with a click. `AudioManagerPrefabBuilder` wires them all into the prefab. Hooks: stage music and stage-clear / game-over / victory sounds via `StageAudio` (added by `GameSessionManager`), menu music on MainMenu, enemy death, player hurt (throttled), boss warning, unlock notice, UI click/confirm, and a fire sound per weapon (`AutoAttackWeapon.FireSound`: Shoot, Slash, Magic, Explosion, or None for Garlic and Bible; throttled). SFX volume no longer applies MasterVolume twice. Validated by `AudioPlayModeTests` (R: every clip assigned, effects audible, menu and per-stage music, weapon, hit, death, hurt and game-over sounds); EditMode 281/281, PlayMode 33/33 (`D:\SoulHunter-Validation\run-20260925-audio2\`). Nobody has listened to it yet: how it sounds (`V`) and the mix are unverified.

### Accessibility (owner decision 2026-09-25: follow the original VS options)

Before: no options screen (only a master-volume slider inside the pause panel), fixed controls, no controller button for Dash/Ultimate/Pause, no focus on pause/level-up/game-over, a mouse-only IMGUI merchant shop, a dead Restart button, and no way to turn off shake, damage numbers or flashes.

Now (checked against the VS wiki options page, https://vampire.survivors.wiki/w/Options, on 2026-09-25; VS-only items with nothing behind them in Soul Hunter are left out: language, classic sounds, borders, pixel font, defanged enemies, co-op, Twitch):
- `OptionsMenuUI` (main menu Options button and pause menu), in VS-style sections. Sound: Sounds, Music. Display: Window Mode (Fullscreen Window / Exclusive Fullscreen / Maximised / Windowed), Resolution, V-Sync, Flashing VFX, Disable Blood (Blood Arenas floor stains switch to the plain floor material), Screen Shake. Gameplay: Damage Numbers, Show Stage Progression (HUD "Stage n/10: name"), Visible Joystick, Pause When Unfocused (extra). Controls (extra): remapping of keyboard (Move WASD, Dash, Ultimate, Pause) and controller (Dash, Ultimate, Pause) bindings with Reset. Stored in `GameData` via `GameSettings`; remaps are saved by action name + binding index because Unity's JSON uses per-launch random binding IDs and would never reload.
- Flashing VFX off: no hit flash. (The low-HP red screen tint was removed on the owner's request, 2026-09-30.) Damage Numbers off hides combat numbers (merchant cost popups stay). Screen Shake off stops `CameraShake`.
- Controller: Dash = A/Cross, Ultimate = Y/Triangle, Pause = Start, D-pad moves. Every screen focuses a control (menu, character select, pause, options, level-up, chest, game over, merchant), and focused controls turn orange (`OptionsMenuUI.ApplyFocusColors`; the old tint was invisible on dark buttons).
- `PauseMenuUI`: Resume / Options / Quit (to main menu); `GameSystemManager` pauses on focus loss when the option is on.
- The merchant shop is uGUI instead of IMGUI. Selected character cards also get an outline (not colour alone).
- Touch controls (`TouchControlsUI`, VS mobile control stick): stick + Dash + Pause through Input System on-screen controls (virtual gamepad); always on mobile, on desktop with Visible Joystick. Before this Android had no touch input at all.
- HUD readability (`HudReadability`): the kill, gold and time labels were dark blue on dark floors; now white with a black outline.

Validated by `AccessibilityTests` (T) and `AccessibilityPlayModeTests` (R: options sections and focus, pause menu, controller Start pauses, focus-loss pause, merchant shop focus, switches turning off numbers/shake/flash, stage label, white HUD labels, Disable Blood on the Blood Arenas floor, touch controls); EditMode 284/284, PlayMode 37/37, save file untouched (`D:\SoulHunter-Validation\run-20260925-access2\`). VS has no text-size option, so none was added. Window Mode and Resolution only apply in builds (not the editor Game view). Not tested on real hardware, a real controller, a touch device or a real alt-tab. `StageEventPlayModeTests` failed once in five full runs (a wall member moved 0.19 < 0.5 in 20 fixed updates) and passed alone three times: timing-sensitive, not caused by this change.

### Persistence

Hardened 2026-09-25 (`SaveService`, `GameData.SaveVersion`):
- Saves are atomic: written to `.tmp`, then swapped in with `File.Replace`, keeping the last good save as `.prev`. A failed write (locked file, full disk) logs, returns false and leaves the last good save; it no longer throws into gameplay.
- A damaged save (unparsable, cut off mid-write, empty or zero-filled) is copied to `.bak` and removed, and the `.prev` save is restored; only without a usable `.prev` does the game start fresh. A missing save with a `.prev` (crash between write and swap) is restored too.
- Schema versioning: `GameData.CurrentVersion` = 1; files without the field load as version 0 and are migrated (v0 -> v1: Antonio -> Corvan in the selected, unlocked and owned characters, duplicates removed). Every load also repairs impossible values (negative gold or levels, NaN/out-of-range volumes, bad window mode or resolution index, null lists). A save from a newer build is kept at its version with a warning.
- A `-sh-benchmark` launch uses a throw-away save in `temporaryCachePath`.

Validated by `SaveServiceTests` (T: atomic save and `.prev`, three kinds of damage restored from `.prev`, fresh start without `.prev`, missing save restored, legacy v0 migration, value repair, locked-file write failure) and both suites (EditMode 293/293, PlayMode 39/39, save file untouched, `D:\SoulHunter-Validation\run-20260925-gaps2\`). Not tested: a real power cut, or Android storage.

### Documentation

Some older status documents are stale or contradictory. Current owner decisions and verified evidence outrank stale status text.

### Character stat blocks and VS-style select screen (owner request, 2026-09-25)

Owner decisions (2026-09-25): every character gets the full VS stat block, every ability its description promises must be real, and the character select screen follows VS. `CharacterData.BaseMoveSpeed` is a **multiplier** (1.0 = the player's base speed 5); Kael, Corvan and Elaria were 4/3/3.5 and are now 1.0. Antonio (a VS character name and description) was renamed **Corvan, The Grave Warden** (`Char_Corvan.asset`, same GUID); `GameContentCatalog.CurrentName` maps a save that still says "Antonio" to Corvan.

- Data: `CharacterData` adds Recovery, Speed, Duration, Amount, Revival, Magnet, Luck, Growth, Greed and Curse, a level-up bonus (`LevelBonus` every N levels) and special abilities (crit multiplier, extra projectile bounces, low-health speed bonus, starting shield + its visual). `PlayerStats.ApplyCharacter` applies all of it; `CharacterStatSheet` computes character + gold power-ups for the screen and uses the same numbers.
- Abilities now backed by data (values provisional): Aria Growth +20%, Draven Amount +2, Gideon Area +50% (Might set to 1.0 to match his description), Isolde Cooldown -15%, Kaelen crits 3x, Lumina Recovery 1 HP/s, Rurik up to +50% speed at 0 HP, Seraphina +3 bounces on every `Projectile` weapon (Magic Wand, Song of Mana, Knife, Thousand Edge, Fire Wand, Holy Wand, Gun, Cherry Bomb, Death Spiral), Sylas 10 s starting shield shown with the Laurel shield model (`StartingShieldVisual`, blinks for the last 2 s), Valerius +1 Armor every 5 levels, Corvan +10% Might every 10 levels (until level 50 since 2026-10-09, VS Antonio).
- Screen: `CharacterSelectionUI` builds the layout at runtime inside the scene's existing panel: a grid of character cards (keyboard/gamepad focus selects), a stats panel on the left with bonuses highlighted, the selected character's info and starting weapon, and Confirm. Prev/Next are hidden.
- Upgrade icons: every `UpgradeData` (279 assets) now has an icon from the project's own `Assets/Art/UI` sprites, assigned by `UpgradeIconAssigner` (menu Soul Hunter/Content). Shown on the select screen and the HUD weapon slots.
- Character art: every character has its own rigged low-poly hero model, generated in Blender by `ArtSource/Generators/build_heroes.py` (sources and previews in `ArtSource/SH10_Heroes_v01/`) as `Assets/Art/Models/SH10_Hero_<Name>.fbx`, in Kael's faceted style with Kael's rig and clips, each with its own silhouette and palette. The right hand holds the character's actual starting weapon (`WEAPON_PROP` in the generator; `EveryHeroModel_HoldsItsCharactersStartingWeapon` checks the manifest against `StartingWeapon`). Kael's hero model `SH10_Hero_Kael` is his SH10_Kael geometry plus his Whip (the original `SH10_Kael` stays in `Player.prefab`). `HeroModelImporter` (menu Soul Hunter/Content/Import Hero Models) imports them, builds `Assets/Prefabs/Player/SH10_Hero_<Name>.prefab` (Animator on `SH10_Kael.controller` + `EntityAnimator`, so run/attack now play; Kael included), sets each `CharacterModelPrefab` and renders each menu portrait (`Assets/_Project/Art/Portraits/`). In play, `PlayerController.LoadVisualModel` spawns the selected hero (or replaces an authored 3D visual) and refreshes `DamageFlash`. Before this, every character appeared as the same placeholder sprite in play. Main_Gameplay's player also carried a `Hero_1_0` sprite child that was drawn next to every hero; it was removed and the ten `Hero_N_0` sprite prefabs were deleted (owner request, 2026-09-25). `CharacterSweepPlayModeTests` now fails if any sprite is drawn on the player. Weapons follow the VS rule (owner, 2026-09-25): a run starts with the character's starting weapon only (checked for all 13 characters by `CharacterSweepPlayModeTests`, R), any character can gain any weapon from level-ups and chests (the level-up pool is the whole catalog, S), up to 6 weapons and 6 passives (`CoreLoopFixTests`, T). No gold counter or co-op. Unlocks, buying and the stage filter: see "Character unlocks" below.

Validated by `CharacterStatBlockTests` (T: includes every character having its own animated hero model and portrait) and `CharacterSelectionPlayModeTests` / `CharacterSweepPlayModeTests` (R: a card for each character; clicking one shows its full stat block; Sylas's shield shows and ends; Seraphina's live projectiles carry 3 extra bounces; all 13 characters play with exactly their own model and an EntityAnimator); EditMode 266/266, PlayMode 28/28, 0 exceptions (`D:\SoulHunter-Validation\run-20260925-heroes\`). Batchmode screenshots of the menu and of Gideon in play were checked (partial `V`); hands-on feel, animation quality and balance are unverified.

### Character unlocks and filter (owner decision, 2026-09-25: follow the original VS rule)

Only Kael is available at the start. Every other character has an `OriginStage`, an unlock condition (`ClearStage`, `SurviveInStage`, `ReachPlayerLevel`, `KillsInRun`, `WeaponLevel`) and a `GoldPrice` in its `CharacterData`. `CharacterUnlockTracker` (added by `GameSessionManager`) checks the conditions during a run, stores unlocks in `GameData.UnlockedCharacters`, saves and shows a notice. On the select screen a locked character is a silhouette with its condition and cannot be confirmed; an unlocked one shows its price and Confirm becomes Buy (gold via `EconomyService`, stored in `GameData.OwnedCharacters`); gold is shown top left. The right-hand panel is the VS character filter: `FILTER: ON/OFF` plus one checkbox per level (count `x/10`) that hides characters from unchecked origin stages. Conditions, origins and prices are provisional Soul Hunter data (not VS data): Corvan survive 5:00 in Stage 1 (300 G), Aria player level 20 (500), Draven 1000 kills in a run (700), Gideon clear Stage 3 (900), Kaelen clear Stage 4 (1100), Valerius survive 15:00 in Stage 4 (1300), Isolde Santa Water level 4 (1500), Elaria Magic Wand level 8 (1700), Seraphina clear Stage 6 (1900), Lumina clear Stage 7 (2200), Rurik clear Stage 8 (2500), Sylas clear Stage 9 (3000). Gameplay itself does not re-check ownership; the menu is the only way into a run. The owner's existing save keeps its gold but now owns only Kael. Validated by `CharacterUnlockTests` (T, in-memory GameData) and `CharacterSelectionPlayModeTests` (R: locked/buy/owned states, filter, tracker follows stage clears); EditMode 276/276, PlayMode 31/31, save file unchanged by the tests (`D:\SoulHunter-Validation\run-20260925-unlocks\`). Whether VS's exact filter semantics match this is from memory, not a source.

### Stage floors (owner request, 2026-09-25)

The ground looked broken: InfiniteMap tiled Level 1's 10x10 ground at 20-unit spacing on every level (gaps showed the background), and each stage's showcase scene added its own fixed 3x3 ground on top. Now, as in VS where every stage has its own ground, `ArtSource/Generators/build_grounds.py` (Blender) builds a seamless 20x20 low-poly floor per level (`SH10_Lxx_Floor`: graveyard soil and bones, forest moss and ferns, village cobbled streets, castle flagstones, crimson swamp mud and puddles, snow and ice, soul-sea sand and shallows, blood-arena sand, throne marble with gold inlay, soul-core crystal plates). `StageFloorImporter` (menu Soul Hunter/Content/Import Stage Floors) builds prefabs with one flat BoxCollider and fills `GameContentCatalog.StageFloors`. `InfiniteMap` lays the current stage's floor, swaps it on `OnStageStarted` and turns each tile a random quarter turn; `CampaignSceneConnector` hides the showcase grounds (`IsShowcaseGround`). Validated by `StageFloorTests` (T) and `StageFloorPlayModeTests` (R: level floor, no gaps, swap on stage change, showcase ground hidden, player stands); EditMode 272/272, PlayMode 29/29 (`D:\SoulHunter-Validation\run-20260925-floors\`). In-game screenshots of all 10 levels checked (partial `V`). The showcase scenes also placed a still `SH10_Lxx_Enemy`, `Elite` and `Boss` model at x = -2.3, 0 and 2.3 on every stage, right at the player spawn (2, 1, 0), looking like enemies that never move or die; `CampaignSceneConnector.IsHiddenInPlay` now hides them too (scenery props stay). Reproduced by `StageFloorPlayModeTests` (failed with 9 statue objects) before the fix.

### Stage floors v2: VS-style obstacles, old ground removed (owner request, 2026-09-25)

The owner supplied VS stage ground blueprints as a reference (kept outside the project; nothing copied) and chose "their feel, our own art", obstacles, and removal of the old ground and showcase decor. `build_grounds.py` v2 (`ArtSource/SH10_Floors_v02`) builds each 20x20 floor with a 3-unit cross of paths meeting every tile edge at its midpoint (joins in any quarter turn), denser ground detail and 14-28 themed obstacles per level (graves, crypts and dead trees; pines; houses, wells, hay; pillars and walls; swamp trees; ice; soul ponds; arena weapon racks; braziers and obelisks; crystals and pylons). Each FBX has three meshes: the floor, the `_Obstacles` visuals and simple `_Blockers` boxes that `StageFloorImporter` turns into a hidden MeshCollider on the new `Obstacle` layer (14). The physics matrix lets that layer collide with the Player layer only, so obstacles block the player while enemies (triggers), weapons, Axe physics and projectiles pass over them. The tile centre (player spawn, radius 4.5) and the paths stay open. Floor materials of earlier versions are removed on import.

Removed (`LegacyGroundRemover`): the 90 `SH10_Lxx_Ground` instances in the ten showcase scenes, their 10 models, 10 prefabs, 20 materials and the unused `Ground.prefab`; `InfiniteMap._chunkPrefab` in `Main_Gameplay` now falls back to `SH10_L01_Floor`. `CampaignSceneConnector.IsHiddenInPlay` hides every `SH10_L*` object of a showcase scene in play (props and still display models); the showcase scenes themselves remain.

Validated by `StageFloorTests` (T: floor and collider sizes, obstacle collider/layer/hidden boxes, >= 12 obstacles, spawn centre and paths open, collision matrix, old assets gone) and `StageFloorPlayModeTests` (R: no showcase decor in play; the player walking straight into an obstacle face is stopped, 10/10 repeated runs); EditMode 284/284, PlayMode 38/38, save file untouched (`D:\SoulHunter-Validation\ground-20260925\`). In-game screenshots of all 10 levels checked (partial `V`). The player can slide round a small obstacle's corner, as in VS. The camera looks straight down, so obstacles read mostly by their tops. Feel, density and performance in real play are unverified (`V`, `P`).

### Open ground rule: 70% walkable, no traps (owner rule, 2026-09-26)

Owner rule: at least 70% of every stage's ground must be open for the player, who kept getting stuck between obstacles. VS research (vampire.survivors.wiki stage pages) found:
- Most stages are open ground with "very few obstacles" that "can be easily navigated around".
- Only a few stages are corridors.
- Walls block the player, but weapons pass over them.
- The spawn area is open.

The cause was `build_grounds.py`: obstacles stood only 0.15-0.8 units apart (the player is 0.8 wide) and many blocker boxes reached past their placement radius, so obstacles closed gaps and formed pockets.

Now:
- Obstacles keep a `MIN_GAP` of 1.8 between them and a 1.0 margin from every tile edge, so seams stay open.
- `Placer` measures each obstacle's real blocker footprint (the farthest box corner) and removes an obstacle that does not fit.
- Floors were regenerated (7-16 obstacles per tile, down from 14-31) and re-imported.

`FloorWalkabilityTests` (T) samples each 20×20 tile on a 0.25 grid with the player's capsule plus 0.35 elbow room. Blocker interiors count as blocked (the MeshCollider is surfaces only). It flood-fills from the spawn and the tile edges, and requires ≥70% reachable ground and no trapped pockets.

Reachable ground per level:

| Level | Before | After |
|---|---|---|
| L01 | 67% | 78% |
| L02 | 58% | 78% |
| L03 | 68% | 77% |
| L04 | 61% | 80% |
| L05 | 72% | 80% |
| L06 | 63% | 80% |
| L07 | 61% | 82% |
| L08 | 79% | 85% |
| L09 | 75% | 79% |
| L10 | 77% | 80% |

The earlier "trapped" readings were blocker interiors. `StageFloorTests` now requires at least 6 obstacles per floor (it was 12).

Found by the suites: the Wandering Merchant could stand on an obstacle. `CampaignSignatureSystem` checked its spot on the same `OnStageStarted` event on which `InfiniteMap` swaps floors, so it tested the previous stage's ground. It now spawns one frame later, after `Physics.SyncTransforms`.

Validation:
- EditMode 327/327, PlayMode 89/89, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-ground\`).
- Editor play `autoplay-editor-16` (Stage 3, god mode, 3 min): 0 errors, min 51 FPS; the village shows wide lanes and the merchant on open grass (partial `V`).

(2026-09-30: 70% of these obstacles were then removed; see "Owner play reports" below.)

VS ground features: the owner chose "VS wala" (2026-09-26), built one at a time.

**Light sources (Soul Lantern), done:**
- Spawning (`LightSourceSpawner`, added by `GameSessionManager`):
  - one attempt per second, while the game is not paused;
  - chance is 10% × Luck, capped at 50%, with at most 10 lanterns at once (VS Mad Forest numbers used on every stage; provisional);
  - a lantern appears just off-screen (8% past a screen edge, on the player's plane), never in or against an obstacle, and is recycled beyond 40 units.
- The lantern (`LightSource`):
  - has 10 HP and is tagged and layered "Enemy", so tag, layer and projectile weapons all break it;
  - is not an `EnemyController`, so nothing aims at it, the horde ignores it and breaking it is not a kill;
  - is a trigger, so it never blocks the player.
- Drops (`LightSourceDrops`, the VS table limited to Soul Hunter's pickups):
  - Gold Coin 50 (1 gold), Floor Chicken 12, Coin Bag 10 (10 gold);
  - Vacuum/Magnet 2 from level 12, Orologion/Time Freeze 2 from level 4, Rich Coin Bag 1 from level 5 (100 gold);
  - coin values are VS values from memory and provisional;
  - Rosary, Nduja and the clovers have no Soul Hunter pickup and are left out;
  - Luck does not change the drop weights yet (no source gives the formula).
- Model: `ArtSource/Generators/build_light_source.py` (Blender, reuses the pickups v2 setup) builds a stone plinth with an open iron lantern cage and a cyan soul flame over orange embers. It has 690 triangles and stands about 1.3 units tall. Previews are in `ArtSource/SH10_LightSource/Previews`.
  - `LightSourceImporter` (menu Soul Hunter/Content/Import Soul Lantern) makes URP Lit materials and a soft ground glow, builds `Assets/Prefabs/Props/SH10_SoulLantern.prefab` and sets `GameContentCatalog.SoulLantern`.
  - **The model is not yet owner-approved.**
- Validation:
  - `LightSourceTests` (T): drop weights and level gates, chance × Luck with the cap, and the prefab setup.
  - `LightSourcePlayModeTests` (R): lanterns appear off-screen and out of obstacles up to exactly 10; Whip, Garlic and Bible break one; it drops an item and is not a kill.
  - EditMode 330/330 and PlayMode 93/93, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-lantern\`).
  - Editor play `autoplay-editor-17` (Stage 1, 3 min): 0 errors, min 68 FPS; lanterns glow on the edges of the screenshots, but look small from the game camera (partial `V`).

- Owner (2026-09-26): the lantern looked too small, so `LightSourceImporter.Scale` = 1.4. The model, trigger and glow are now 1.4×, about 1.8 units tall. The model design is still awaiting approval.

**Stage items, done (owner OK'd the item list, 2026-09-26):**
- Layout (VS stage items, Dairy Plant style): `StageItemSpawner` (added by `GameSessionManager`) lays four items when each stage starts.
  - They go on the diagonals, 55/65/75/85 units from the player's position at the start (about 3-4 tiles), turned a quarter per stage.
  - Each lands on the nearest open ground (`GroundCheck`), one frame after the floor swap.
  - The next stage removes the leftovers; pooled pickups return to their pools.
- The four items: a passive, a weapon, a floor chicken, and a Vacuum/Magnet (odd stages) or Orologion/Time Freeze (even stages).

  | Stage | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
  |---|---|---|---|---|---|---|---|---|---|---|
  | Passive | Spinach | Armor | Empty Tome | Candelabrador | Bracer | Clover | Spellbinder | Duplicator | Pummarola | Attractorb |
  | Weapon | Knife | Axe | Cross | Santa Water | Fire Wand | Lightning Ring | Runetracer | Bone | Garlic | Peachone |

- Pickup (`StageItemPickup`): the passive or weapon shows its icon over the Soul Lantern's ground glow. Walking over it applies the next level's `UpgradeData` through `WeaponManager.ApplyUpgrade`, as a level-up would. With no free slot, or at max level, it pays out a coin bag instead (10 gold; provisional).
- `PickupPoolManager.SpawnChicken/SpawnMagnet/SpawnTimeFreeze` now return the spawned object.
- Validation:
  - `StageItemTests` (T): every stage has one of each kind on four different diagonals, 55-85 out, each with a level-1 upgrade, and a different passive per stage.
  - `StageItemPlayModeTests` (R): four items on open ground at their planned spots; walking onto the passive and the weapon raises each by one level; Stage 2 replaces Stage 1's items; with six weapons a weapon item becomes gold.
  - EditMode 331/331 and PlayMode 97/97, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-stageitems\`).
  - Editor play `autoplay-editor-18` (Stage 1, 3 min): 0 errors, min 68 FPS. The bot never walked out to the items, so they are not visually checked yet.

**Pressure plates (Soul Rune Plate), done (2026-09-26):**
- Rules (VS pressure plates, from the Gallo Tower, Dairy Plant and Boss Rash pages): a flat plate that starts a map event when stepped on, then waits at least 20 s. It never blocks movement.
- Soul Hunter version: `StageItemSpawner` lays four Rune Plates per stage, 30 units straight N/E/S/W of the start, between the item diagonals, on open ground. The next stage removes them. Stepping on a ready plate:
  - calls `EnemySpawner.TriggerStageEvent`, a random Swarm, Wall or Closing Ring of 16 of this stage's enemies (nothing during the boss or a stage transition);
  - plays a warning sound and a burst;
  - dims the plate's runes (MaterialPropertyBlock) for `CooldownSeconds` (20).
- All stages have plates, with provisional distance and count.
- Model: `build_light_source.py` now also builds the Rune Plate: an octagonal slab with a sunken ring of crimson runes, 308 triangles, drawn 1.3×, about 2.5 units across. `LightSourceImporter` (menu Soul Hunter/Content/Import Stage Props) builds `Assets/Prefabs/Props/SH10_RunePlate.prefab`, a Default-layer, untagged trigger (weapons ignore it), and sets `GameContentCatalog.RunePlate`. **The design is not yet owner-approved.**
- Validation:
  - `PressurePlateTests` (T): flat trigger, not an enemy, glowing runes, about 2.5 across; four plates straight out at 30.
  - `PressurePlatePlayModeTests` (R): four plates on open ground; stepping on one sends a formation; stepping again while it recharges does nothing, and it fires again after the cooldown; Stage 2 replaces the plates.
  - Editor play `autoplay-editor-19` (Stage 1, 3 min): the bot stepped on plates in real play (Closing Ring at 57 s, Swarm at 80 s, Closing Ring at 179 s), with 0 errors and min 55 FPS.
- Old Soul Hunter hazards: grey and green cylinder "Trap" and "Poison" damage discs still sit in a ring around the start of Stage 4 and 5 (`CampaignSignatureSystem.CreateHazards`). They are not a VS mechanic. Whether to keep them is an owner decision.

Found and fixed while validating the plates:
- **The owner's real save was written by tests.** The isolated copy shares the game's `persistentDataPath`, and tests that boot on their own were not redirected. Once lanterns dropped gold in every run, a test run wrote the save twice (Currency ending at 16004; the value before was probably 16002). `SaveService.TestSavePath` now makes every PlayMode test (set in `GameplayPlayModeTestBase` and `BootstrapPlayModeTests`) play on a copy of the save. The next full run left the save's hash and timestamp unchanged.
- `EnemySpawner.SpawnEnemy` skips pooled enemies that were destroyed elsewhere (the crowd test destroyed its strays; a plate event then got a destroyed enemy from the pool).
- EditMode 333/333, PlayMode 100/100, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-plates\`).

### Look, feel and sound pass (owner request, 2026-09-25)

The owner said the ground looked dated, the sound was not professional, the player was not smooth, attacks were not visible and the player should face the enemy when attacking. Owner choices: CC0 sound packs, a textured stylized ground, and a camera tilted to about 55 degrees.

- Camera: `Main Camera` was a child of the player with `CameraFollow` disabled, looking straight down, so only the tops of heads showed. It is now a scene root and `CameraFollow` follows at a 55-degree pitch (orthographic size 12, frame-rate-independent smoothing). The damage numbers were sized for the old top-down camera, which saw them edge-on. They now face the camera at 7 pt.
- Player facing: `PlayerController` mirrored the 3D model with `localScale.x = +-1` (it snapped and never faced up or down; the child camera was mirrored with it). The hero model now turns smoothly (slerp) toward where it walks. When a directional weapon fires, the hero turns to the enemy that weapon aims at for 0.45 s (`AutoAttackWeapon.ShowAttackOnHolder`, called before `Attack()` because a shot can kill its target). Garlic, Bible, Laurel, Pentagram and Peachone do not turn the hero. The model's front is worked out from its thigh bones, because Kael's authored visual faces +Z while the hero prefabs face -Z.
- Attack animation: `Attack` was a full-body state reached only from the manual attack input, so auto-weapons never showed it. `PlayerAnimatorSetup` moves it to an `UpperBodyAttack` override layer (mask: Spine and its children, `SH10_UpperBody.mask`) at 1.6x speed. Each weapon shot triggers it, at most every 0.6 s, and the legs keep running.
- Rendering: the pipeline used the URP 2D Renderer, which ignores 3D lights and shadows. `RendererSetup` switches to a 3D Universal Forward renderer (`UniversalRenderer.asset`) with HDR off, intermediate texture Auto and shadow distance 45. `Renderer2D.asset` stays on disk, unused (only the URP 2D template test scene has a 2D light). The stage light casts hard shadows at strength 0.6. The faceted shader (`SoulHunter/Review/FacetedPreview`) is now lit by the main light with shadows and has a ShadowCaster pass. The hero, bosses and stage obstacles cast shadows. For performance, the horde, pooled projectiles and pickups do not, and off-screen enemies do not animate (`AnimatorCullingMode.CullCompletely`).
- Ground: `build_ground_textures.py` (Blender + numpy) makes 16 seamless 512 px stylized detail textures (grass, dirt, cobble, flagstone, mud, planks, snow, ice, sand, marble, crystal plates, carpet, stone, leaves, water, grain) in `Assets/Art/Textures/Ground`. `build_grounds.py` box-maps world-space UVs (4 units per repeat on the ground, 2 on props) and lists each material's texture in the manifest. `StageFloorImporter` makes URP Lit materials: texture tinted by the palette colour divided by the texture mean (0.8), matte, and emissive for glow and vein keys. The floor receives shadows and the obstacles cast them.
- Sound: every effect and all 11 music tracks are now CC0 recordings, installed by `install_cc0_audio.py`. Effects come from Kenney (impact, interface, RPG, sci-fi, jingles) and OpenGameArt (artisticdude's RPG Sound Pack and Swishes, rubberduck's creature SFX). Music is by emmama (menu), cynicmusic, mintodog, katana, bobjt and isaiah658. Each licence page was checked as CC0 on 2026-09-25, and sources are listed in `Assets/_Project/Audio/CC0/CREDITS.md`. The script loudness-normalises each file and writes Ogg Vorbis. Music streams; effects decompress on load. `AudioManagerPrefabBuilder` prefers these files and keeps the synthesised clips as a fallback. The jingles were chosen by measured pitch trend (rising for level-up, stage clear and unlock; falling for game over), not by ear.

Validated by `PlayerPresentationPlayModeTests` (R: camera pitch and centring; hero turns smoothly and is not mirrored; faces the enemy and plays the upper-body pose while the legs keep their state; stage light shadows; the horde has no shadows or off-screen animation), `HeroAttackLayerTests`, `RenderingSetupTests` and `Cc0AudioTests` (T), and both full suites: EditMode 301/301, PlayMode 42/42, 0 shader errors, save file untouched (`D:\SoulHunter-Validation\run-20260925-feel\`). Performance (Windows build, this machine = the i5-8350U / UHD 620 reference, 1080p pixel count, Medium): Stage 10 at 20:30 with ~300 enemies went from 85-88 FPS (2D renderer, top-down) to 48-57 FPS with shadows on everything. Turning off horde, projectile and pickup shadows, HDR and the always-on intermediate texture, and culling off-screen animation brought it to 76 FPS average and a 37.5 FPS 1% low. A run with ~170 enemies gave 111 FPS. Nobody has looked at it or listened to it in real play yet: `V` for the look, animation and mix is still unverified.

### Projectiles, weapon effects, gameplay VFX and pickups (owner request, 2026-09-25)

The owner said VFX, gems, chest, gold and bullets were not proper. Found:
- Almost every weapon fired or placed the same orange sphere, `Bullet.prefab`: Magic Wand, Knife, Thousand Edge, Fire Wand, Holy Wand, Gun, Bone, Runetracer, Cherry Bomb (bomb and explosion), Song of Mana, Clock Lancet, Lightning Ring, Peachone, Pentagram and Santa Water. The Axe threw a cylinder and the Cross a magenta sphere (missing material).
- `Bullet.prefab` carries a `Projectile` script (moves the object forward and blocks `ProjectileDamage`) and is scaled 0.2, so area weapons flew away with a 0.1-unit hit area. Santa Water pools, lightning strikes and Peachone blasts were affected, and Bone and Runetracer were dragged by it.
- Peachone's `BirdVisual` was never assigned, so it never dropped a bomb, and its bird circled vertically (X-Y).
- The gameplay pickups (`XP_Gem`, `Chest`, `HealthChiken`) drew a placeholder cube over their SH10 model: the yellow squares in play. The gold coin lay flat, reading as a brown disc.
- No particle effect existed. `VFXPoolManager` was in no scene, so its death and chest effects never played.
- Knife, Thousand Edge, Whip, Axe and Cross took their "facing side when no enemy" from the player's `localScale.x`. That stopped working when the hero stopped mirroring.

Now, `WeaponVfxBuilder` (menu Soul Hunter/Content/Build Weapon VFX) generates everything from Unity meshes, the SH10 models, procedural textures and particle systems, using the `SoulHunter/VFX/Particle` shader (additive, or alpha for smoke):
- **Weapon prefabs:** 18 prefabs in `Assets/Prefabs/Weapons/Projectiles`, each with the components its weapon code expects.
  - Flying (moved by `Projectile`): magic bolt, fireball, holy bolt, gun tracer, thrown knife, Thousand Edge knife, mana shot, and a Cherry Bomb that explodes when its flight ends.
  - Physics (Rigidbody velocity + `ProjectileDamage`): spinning bone, glowing rune, tumbling SH10 axe.
  - Boomerang: a spinning SH10 cross.
  - Zones that stay where they are placed, with hit areas the size of what is drawn: holy water pool (radius 1.4), lightning strike with a new jagged bolt each time (0.9), Peachone blast (0.9).
  - Visual only: Pentagram flash, Clock Lancet freeze beam, Peachone bird.
  - Weapon prefab fields are rewired, and no weapon references `Bullet`, `Axe_Model` or `Cross_Model` any more.
- **Gameplay VFX:** `Resources/VfxLibrary` holds hit spark, enemy death (soul puff), gem pickup, gold pickup, heal, chest open, level-up and explosion. `VFXPoolManager` creates itself on first use, pools up to 32 of each kind and throttles frequent kinds. Hooks: enemy hit and death, XP gem, gold/magnet/freeze pickups, chicken, chest, level-up, Soul Burst.
- **Pickups:** placeholder cubes hidden. Gem, chest, chicken, magnet, freeze and coin bob and turn (`PickupIdleMotion`, which moves only the visual child). Gems glow cyan and the coin stands up and glows gold, using a new `_EmissionColor` on the faceted shader. The chest has a gold sparkle beacon. Visuals were enlarged for the tilted camera: gem x1.6, chest x1.4, coin x1.4, chicken x1.2.
- Peachone's bird now circles on the ground plane and drops blasts under itself. Every facing fallback reads `PlayerController.FacingDirection` (`AutoAttackWeapon.HolderFacing`).

Validated by `WeaponVfxTests` (T: no placeholder references, components per weapon object, library complete and non-looping, pickups without cubes) and `WeaponSweepPlayModeTests` (R: each of the 19 weapons in the real game, never the placeholder objects. Every one hurts enemies 3 units away, Clock Lancet freezes them and Laurel is equipped; kills, hits and level-ups play their effects). Both full suites: EditMode 326/326, PlayMode 62/62, save untouched (`D:\SoulHunter-Validation\run-20260925-vfx\`). `WeaponAimingTests` now expects the knife fallback along the hero's facing, which starts up the screen (+Z); it was +X from the old mirroring. Benchmark (reference laptop, 1080p pixel count, ~300 enemies): 78-84 FPS average and a 43-48 FPS 1% low. In-game screenshots were checked (partial `V`). Look and feel in hands-on play are unverified; the effects are Soul Hunter's own and are not VS art.

### AutoplayBot: the game plays itself for review (owner request, 2026-09-25)

`AutoplayBot` (Assets/_Project/Core/Game) lets an agent watch real runs. It flees the horde only when enemies get close (so weapons hit), collects gems, chests and food when hurt, slides around obstacles, prefers upgrades of owned weapons, then new weapons, then passives, and closes chests and the merchant. It records `shots/*.png` (timed and at level-ups, chests, stages and death; a player build captures the UI), `timeline.jsonl` (every 5 s), `events.jsonl` and `summary.json`. It runs only with `-sh-autoplay <folder>` (options `-sh-autoplay-minutes/-character/-stage/-shots/-god`). It plays on a throw-away save, and when started any other way (editor, tests) it first redirects saving with `SaveService.UseFile`.

The first test version added 22 gold to the owner's real save (4482 -> 4504), because `EconomyService` saves on every pickup. The value was restored to 4482, and `AutoplayBotPlayModeTests` now fails if the real save changes. The runner is `Tools/Autoplay/run-autoplay.ps1` (it builds in the isolated copy and plays). First runs, as Kael on stage 1 for 3 min: 0 errors, 59-60 FPS at 1280x720. The first bot version kited too far and got 78 kills. The tuned version reached level 18 with 1636 kills and 159 gold. That kill count is far above a typical VS run at 3:00, because the enemy minimum (30) refills instantly; it is a balance observation, not changed. Validated by `AutoplayBotPlayModeTests` (R); EditMode 326/326 and PlayMode 63/63 with the save unchanged (`D:\SoulHunter-Validation\run-20260925-autoplay\`).

The owner then rejected the online CC0 samples shown (Kenney and Quaternius coins, gems and chests, and the Kenney Particle Pack). They asked for modern, Soul Hunter-style 3D assets and VFX, made in Blender or Unity or taken from the Unity Asset Store. Asset Store packs need the owner's account (My Assets); agents cannot download them.

Editor play (owner request, 2026-09-25: "play it yourself in the Unity Editor"): `AutoplayEditorLauncher` has the menu items Soul Hunter/Autoplay/Play 3 (or 10) Minutes In Editor, and a CLI mode (`-executeMethod SoulHunter.Editor.Autoplay.AutoplayEditorLauncher.RunFromCommandLine -sh-autoplay <folder> -sh-autoplay-exit`). It opens Bootstrap and enters Play Mode with the bot; editor screenshots use the Game view with the UI. First editor run, on the isolated copy in a GUI editor for 3 min as Kael: level 14, 797 kills, 88 gold, 0 errors, min 57 FPS, save unchanged (`D:\SoulHunter-Validation\autoplay-editor-01\`).

Seen while playing:
- The level-up screen is unfinished next to VS: plain white buttons with red internal type names ("SongOfMana Lv.2 (Lv 2)"), no icon, description or NEW tag.
- The player HP bar is a red strip across the whole bottom of the screen instead of a small bar under the hero.

Flaky: one full PlayMode run stopped once in the Loading scene while booting (`WeaponSweep` Peachone), and the next test threw a NullReferenceException. The fixture then passed 21/21 twice.

### Pickups v2: Soul Gems, Soul Coins, Soul Reliquary chest (owner-approved, 2026-09-25)

Owner flow: online samples first, then approval. The CC0 samples were rejected in favour of "modern Soul Hunter-style" designs. `ArtSource/Generators/build_pickups_v2.py` builds them in Blender; concept renders are in `ArtSource/SH10_Pickups_v2/Previews`. The owner approved the close-ups:
- Soul Gems in three tiers: a cyan shard, a violet cluster, and a crimson crystal with a gold band, each with a glowing core.
- Soul Coins in bronze, silver and gold: reeded edge, bead ring, crescent and a soul flame.
- The Soul Reliquary chest: curved lid, iron bands with rivets, a cyan soul-light seam, a skull with glowing eyes and a shield lock.

`--export` writes `Assets/Art/Models/SH10_Pickup_*.fbx` and `materials.json`. `PickupV2Importer` (menu Soul Hunter/Content/Import Pickups v2) makes URP Lit materials (metals, and soul parts with emission), rebuilds `XP_Gem`, `SH10_Gold_Coin` and `Chest` with these models and a soft ground glow, and sets up `TieredVisual`:
- Gem tier by XP: under 25 cyan, under 100 violet, else crimson.
- Coin tier by gold: under 5 bronze, under 25 silver, else gold.

`EnemyData.ExperienceValue` is new (10 for enemies, 50 for elites, 250 for bosses; provisional balance). `EnemyDrop` passes it to `PickupPoolManager.SpawnGem(position, value)`, so elites and bosses drop bigger gems. `WeaponVfxBuilder` no longer touches these three prefabs.

Validated by `PickupV2Tests` (T: tier thresholds, approved models and materials per tier, chest model, elite and boss XP) and `WeaponSweepPlayModeTests.Gems_AndCoins_ShowTheirTierByValue` (R). Both suites: EditMode 344/344, PlayMode 64/64, save unchanged (`D:\SoulHunter-Validation\run-20260925-pickups\`). In-game screenshots were checked (partial `V`). From the 55-degree camera, the chest shows mostly its lid, with the skull and seam at the front.

### VS chest-opening screen and the frozen chest VFX (owner request, 2026-09-28)

Owner: "vs mein chest milne per VFX hai wo nahi dikh raha ... wiki se check karo or effect dalo". The chest screen was a text line over the running game with a white "Claim and Continue" button.

**Frozen VFX.** The chest-open effect started in the same frame the game paused, so its particles froze on their first frame and never showed. The level-up effect had the same problem. `VFXPoolManager.PlaysWhilePaused` runs those two kinds on unscaled time; combat effects still freeze with the game.

**The opening screen** (`ChestOpeningScreen`, `ChestPreviewStage`, opened by `ChestUI`). It follows vampire.survivors.wiki/w/Treasure_Chest (the page, its Audio section and its patch notes), in Soul Hunter's own art:
1. The game pauses and the screen dims.
   - The approved Soul Reliquary chest drops in with a flash and a ring (the intro blast), and "Treasure Found" plays.
   - An OPEN button rotates in.
2. OPEN:
   - the lid opens (the lid parts hang on a hinge at the back) and approved gold Soul Coins burst out;
   - the Treasure A/B/C jingle plays for a 1-, 3- or 5-item chest;
   - the background lights up on 3- and 5-item chests only ("Stopping background panel lighting up in lvl 1 chests");
   - the gold counts up, scales in and pulses;
   - each item spins past as a reel and stops, with stars behind it;
   - 5-item chests add fireworks.
3. DONE rotates in once the gold has counted up, and resumes the game.
- The 3D chest is a copy far below the stage (y = -1000), filmed by its own camera into a RenderTexture. The copy is laid out one frame late, because the renderer bounds of a copy made that frame read the stage position. The model's own up (body to lid) and front (towards the lock plate) are measured, and the copy is turned upright facing the camera; the FBX is turned inside the chest prefab.
- Effects use the approved VFX v2 sprites (`GameContentCatalog.ChestFx*`): magic circle, shock ring, impact star, sparkle.
- `ChestLogicController.LastResult` (`ChestResult`, `ChestReward`) records what a chest gave. `WeaponManager.LastEvolvedIcon` gives an evolution its base weapon's icon, and a union its own icon.

**Skipping** (`ChestSkipRules`, wiki Skipping):
- the first six chests on a save cannot be skipped;
- after that, 1- and 3-item chests skip with Esc, right click or gamepad B, jumping to the end;
- 5-item chests can be skipped after 50 five-item chests or 500 chests;
- once 5-item chests can be skipped, coin-only chests play a quick in-game effect with no screen;
- `GameData.ChestsOpened` / `FiveItemChestsOpened` count them (repaired by `SaveService.Migrate`);
- while the screen is open, the Pause action is off (as in `OptionsMenuUI`), so Esc does not also open the pause menu;
- the wiki's third unlock ("seen the final fireworks") has no Soul Hunter counterpart.

**Sounds:** four unused CC0 Kenney Music Jingles, picked by measured length and rising pitch, not by ear: TreasureFound HIT12 (0.28 s), TreasureA HIT01 (0.51 s), TreasureB NES13 (1.05 s), TreasureC NES00 (1.76 s). They were added with `install_cc0_audio.py --only`, credited in `CREDITS.md` and wired by `AudioManagerPrefabBuilder`.

Provisional: every timing, the coin counts (12/22/36) and the layout.

**First six chests: 1-1-3-1-1-5** (owner: "haan 1-1-3-1-1-5 bhi add karo", 2026-09-29; wiki Treasure_Chest: "The first six chests picked up in a save will always drop items in the set sequence 1-1-3-1-1-5"). `ChestLogicController.ItemCountFor` uses `FirstChestSequence` while `GameData.ChestsOpened` is under 6, whatever Luck or the roll; from the seventh chest the Luck roll applies again. An Arcana chest counts as one of the six (ChestUI counts every chest) but gives its Arcana; the wiki does not say how Arcana chests fit the sequence. The owner's save had opened 5 chests (2026-09-29), so its next chest is the sequence's 5-item chest. Tests: `ChestRewardTests.FirstSixChests_FollowTheVsSequence_ThenLuckRolls` (T) and `ChestFlowPlayModeTests.FirstSixChestsOnASave_Give_1_1_3_1_1_5_Items` (R: six real chests collected and opened). The first full run failed one older test: a new weapon from the chest fired in the collection frame and replaced Treasure Found as `AudioManager.LastClip`; the chest sound checks now read `AudioManager.RecentClips` (last 8). EditMode 471/471, PlayMode 171/171, save untouched (`D:/SoulHunter-Validation/run-20260929-firstchests2/`). Not yet played by the owner.

`AutoplayBot` and `PerformanceBenchmark` press OPEN, then DONE (`ChestUI.OpenAndClose`), instead of closing the old panel by reflection.

Validation:
- `ChestFlowPlayModeTests` (R):
  - a collected chest pauses the game and plays Treasure Found, and cannot be skipped among the first six;
  - OPEN opens the lid, bursts coins, plays the size jingle, reveals every item and counts the gold to the chest's gold;
  - DONE resumes the game, restores the pause key, counts the chest on the save and cleans up the preview;
  - a 5-item chest lights up, has fireworks and plays Treasure C;
  - the seventh chest can be skipped;
  - a coin-only chest opens no screen once 5-item chests are skippable;
  - the chest effect keeps playing while paused.
- `ChestPreviewPlayModeTests` (R): the camera is on the lock's side; OPEN turns the lid about its hinge, back and up over it.
- `ChestRewardTests` (T): the chest result, the skip rules and the pause-time effects.
- The first full run found DONE appearing before the gold had counted up on 1-item chests (fixed). While building the preview, the chest was filmed from the wrong side, first by guessing its front and then from stale bounds; the measured layout above fixed that.
- EditMode 470/470. Full PlayMode 169/170: the one failure was a test race (a weapon's sound in the collection frame replaced Treasure Found as the last clip). The test now turns weapons off, and the chest fixtures then passed 7/7. 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20260929-chest4/`).
- Editor play `autoplay-editor-55` to `-58` (Stage 1, 4 min each, 0 errors): screenshots of the closed chest (front, skull, lock, glowing seam) and the open chest (lid up, +144 gold, "Cross NEW!") (partial `V`).

Not yet played or heard by the owner.

### Blood on enemy deaths (owner request, 2026-09-29)

Owner: "vs mein enemy ke marne per blood effect dikhta hai wiki mein check karo or apply karo". What the wiki says (fetched 2026-09-29):
- vampire.survivors.wiki/w/Options: VS has a "Disable blood" option (Display section), added in v1.6.0 (Updates/v1.6.0 patch notes). So kills show blood unless it is switched off.
- The wiki does not describe how the blood looks. Enemy pages show only per-enemy death animations: the body breaks into pieces that scatter and fade (for example Skeleton, 18 frames × 140 ms = 2.5 s; Pipeestrello, 13 × 140 ms = 1.8 s).

Before, a kill showed only a faint blue soul puff, and Disable Blood only switched the Blood Arenas floor stains.

The first version (a small dark spray and a floor splat, commit `3e7d1271`) was rejected by the owner ("ye sahi nahi hai"). Online samples were shown for comparison (artifact "Soul Hunter Blood Samples": Vefects Free Blood VFX, Vefects Blood VFX, RVFX Blood Effects Pack, and CC0 OpenGameArt sheets by sinestesia, overcrafted and tobiasm). The owner then asked: "blood effect enemy ke neck se spray particle ki tarah VFX set karo".

The owner played that version (a neck jet plus a floor splat, commit `fa694b1c`) and decided (2026-09-29): "jameen per blood effect nahi chahiye, haa uper jo blood neck ke pass dikh raha hai use 300 particle mein dikhao". So there is no blood on the ground, and the neck spray has 300 particles.

Now (`VFXPoolManager.PlayBlood(neck, awayFromHero)`, called from `EnemyDrop` next to the soul puff):
- **Neck spray** (`VfxKind.BloodSpray`, `VFX_BloodSpray.prefab`, built by `WeaponVfxBuilder.BuildBlood`, menu Soul Hunter/Content/Build Blood VFX):
  - One particle system emits exactly 300 particles (`VFXPoolManager.SprayParticles`).
  - They come in three pulses, like a heartbeat: 140, then 100, then 60 particles, each pulse 10 bursts 0.02 s apart, starting at 0, 0.25 and 0.5 s.
  - The particles are stretched bright-red streaks and small drops in a 14° cone, alpha blended, living in world space, so the jet arcs and falls under gravity. Each lives 0.4–0.7 s; the spray is over by about 1.4 s.
- The jet is tilted 50° up and turned away from the hero, ±25° at random (`SprayDirection`).
- The neck is 80% of the way up the enemy's drawn body (`VFXPoolManager.NeckOf`, from `EnemyDrop.BodyBounds`, read at death while the model is still shown). The models share no neck bone.
- **Nothing is left on the ground.** The floor splat, its prefab (`VFX_Blood`) and its texture and material were removed.
- Disable Blood switches the spray off; the soul puff stays.
- Level 7 ghosts (`EnemyController.IsSpectral`) do not bleed. The wiki is silent on this; it is Soul Hunter's choice, because ghosts have no body.
- **Fewer particles when many die at once** (owner chose option 2, 2026-09-29):
  - Up to 8 sprays in the air (`FullSprays`) get all 300 each.
  - A spray started while more are playing shares that budget: 300 × 8 / (sprays playing + 1), at least 60 (`MinSprayParticles`) (`SprayParticlesFor`).
  - `SetSprayParticles` lowers each pulse's burst count before the spray is switched on. With the pool full (32 sprays), a new spray gets 80.
  - That is about 4,800 particles at most, instead of 9,600.
  - Values are provisional.
- At most 32 sprays play at once, with the oldest reused first. Kills in the same 0.01 s make one spray (the blood throttle). Colours, sizes, angles and timings are provisional. The spray is the same size on elites and bosses. **The look is not yet owner-approved.**
- `AutoplayBot` option `-sh-autoplay-no-blood` plays with Disable Blood on, for A/B checks.

Validation:
- `BloodPlayModeTests` (R):
  - A kill sprays from the neck (within 0.05 of 80% of the body height), away from the hero, rising at 50°.
  - It is one alpha-blended system with no ground splat.
  - Counting every particle it emits gives exactly 300, over 100 of them in the air together, and none left after 1.6 s.
  - With Disable Blood on there is no spray, but the puff plays. A ghost sprays none. Kills bleed again once the option is off.
- EditMode 471/471, PlayMode 173/173, 0 errors, save untouched (`D:/SoulHunter-Validation/run-20260929-spray300/`).
- Editor play `autoplay-editor-62` (Stage 1, 1.5 min, a screenshot every second): 0 errors. The shots show dense red jets spraying from killed enemies' necks and nothing on the ground (partial `V`, `run-20260929-spray300/editor-play-spray300-zoom.png`). That run's FPS fell from 70–99 to 36–51 once kills sped up (1,221 kills in 90 s).
- **Cost (GUI editor A/B, Stage 10, god mode, player level 60, 3 min, about 36 kills per second):**
  - blood on (`-63`): 6,556 kills; after 15 s, average 45.6 FPS, last minute 34.9, minimum 26;
  - blood off (`-64`): 6,607 kills; average 47.8, last minute 40.3, minimum 33.
  - About 5–13% slower, and the blood-on run went below the 30 FPS hard minimum in the GUI editor.
  - A repeat blood-on run (`-65`) killed only 2,981 and averaged 42.1 (minimum 34), so the runs are noisy.
  - Editor numbers are not build numbers; no build benchmark was run (`P` unverified).
- **With the budget (option 2):**
  - `BloodSprayBudgetTests` (T): the pulses add up to 300; up to 8 sprays get 300 each; above that fewer, never rising and never under 60.
  - `BloodPlayModeTests.ManyKillsAtOnce_ShareTheBudget_AndALoneKillGets300Again` (R): of 20 kills 0.02 s apart, the first 8 sprays have 300 and the last fewer (at least 60); a lone kill afterwards gets 300 again.
  - The 300-count test now warms up one spray first, because the first spray of a run can hitch past its first particles' 0.4 s life. It counted 272 once before that.
  - EditMode 473/473, PlayMode 174/174, save untouched (`D:/SoulHunter-Validation/run-20260929-spraybudget3/`).
  - GUI editor, same Stage 10 run:
    - with the budget (`-66`): 6,448 kills; average 42.1 FPS, last minute 34.1, minimum 30 (it was 26 in `-63`);
    - blood off (`-67`): only 3,804 kills; average 35.9, minimum 30.
    - The editor runs are too noisy to measure the saving; only a build benchmark at a high kill rate can (`P` unverified).
  - Stage 1 (`-68`): 0 errors, minimum 68 FPS.
- **Build benchmark** (owner: "build benchmark bhi chalao", 2026-09-29):
  - Setup: Windows build on the reference laptop (i5-8350U, UHD 620), Medium, 1366x768 window rendered at 1080p's pixel count, V-Sync off.
  - Built in the isolated copy to `D:/SoulHunter-Validation/run-20260929-bloodbench/build*/`; `D:\Unity Builds\SoulHunter` untouched.
  - New `PerformanceBenchmark` options:
    - `-sh-benchmark-level N`: start the hero at level N, for a high kill rate.
    - `-sh-benchmark-no-blood`: Disable Blood on.
    - `-sh-benchmark-blood-ab`: the same hero measures each late segment with blood, without, then with it again.
    - Each segment now also records its kills.
  - **Separate runs cannot compare blood.** The level-up picks are random, so loadouts and kill rates differ. In the first level-60 runs, the blood-on heroes drew Pentagram and Santa Water and killed 4–5× more (3,800–4,800 against 1,065–1,140 in the Stage 1 late segment), which swamped any blood cost.
  - Level-1 hero (first build: the level option did not apply, the blood switch did), Stage 10 at 20:30, about 290 enemies:
    - blood on, 3 runs: 60.8 / 75.8 / 64.5 FPS average, 1% low 33.7 / 46.3 / 34.5;
    - blood off, 2 runs: 74.1 / 69.7, 1% low 44.2 / 41.9.
    - Kills were not recorded; the ranges overlap.
  - **In-run A/B, level-60 hero** (`ab-lv60-1` and `-3`; `-2` is invalid, see below), average FPS for blood / no blood / blood again:
    - run 1: Stage 1 late 95.3 / 65.9 / 78.9; Stage 10 late 72.1 / 67.8 / 76.9;
    - run 3: Stage 1 late 57.2 / 44.6 / 45.7; Stage 10 late 47.1 / 31.9 / 37.9.
    - In every pair the no-blood segment was no faster; it also had the most kills (for example 782 against 287 and 434).
    - **The spray's cost is below the noise in a build.** FPS follows the kill rate and the loadout.
  - **Seen, not changed:**
    - With a level-60 hero at 300–1,985 kills per 30 s, the 1% low falls to 7–15 FPS with or without blood, well under the 30 FPS hard minimum. Heavy kill rates themselves (drops, gems, death effects) cost frames; this needs its own performance pass (`P`).
    - Run `-2` stayed paused (timeScale 0) from its second segment. First read as a level-up problem; it was a chest (fixed, below).
  - Found by the full PlayMode run: `AutoplayBot` never removed its level-up, stage, game-over, victory and damage handlers. A gem collected while a test tore down started a coroutine on the destroyed bot (`MissingReferenceException`, `AutoplayBotPlayModeTests`). All of them are now removed in `OnDestroy`.
  - EditMode 473/473, PlayMode 174/174, save untouched (`D:/SoulHunter-Validation/run-20260929-bloodbench2/`).

### Chest screen froze the game (fixed 2026-09-29)

Owner: "2 wala karo" (fix the benchmark stall).

A stall watchdog in `PerformanceBenchmark` (records who holds the pause when the game stays paused for 15 s, then closes a chest screen or takes a level-up card; each stall is listed in the results) showed:
- The pause was held by the scene's `ChestUI` on `--- MANAGERS ---/UI_Bindings`, the one `PickupPoolManager` hands chests to.
- That `ChestUI` has no canvas above it. It put the chest screen on `FindFirstObjectByType<Canvas>()`, which could be a temporary canvas, such as the character unlock notice (`CharacterUnlockTracker`) or the merchant's shop.
- When that canvas was destroyed, the screen went with it: once it stopped at "Revealing", once "no screen". The chest's pause was never released, so **the game stayed frozen for the rest of the run.**
- A real player could hit this when a chest opens while an unlock notice or the shop is up. In the level-60 benchmark, the start-level XP unlocks characters (Aria at player level 20).

Fix:
- `ChestUI.ScreenCanvas()` gives the chest screen its own overlay canvas, owned by the `ChestUI`: sorting order 400 (above the HUD, below the unlock notice's 500), CanvasScaler at 1920x1080 like the HUD.
- `ChestUI.Update` releases the pause if the screen is ever gone while a chest is open.
- `GameTime.PauserNames()` lists who holds the pause, for diagnostics.

Validation:
- `ChestFlowPlayModeTests.TheChestScreen_HasItsOwnCanvas_AndSurvivesATemporaryCanvasGoingAway` (R): the pool's `ChestUI` screen sits on its own canvas; a temporary overlay canvas destroyed mid-chest leaves the screen working and DONE resumes; a screen destroyed while open no longer leaves the game paused.
- EditMode 473/473, PlayMode 175/175, save untouched (`D:/SoulHunter-Validation/run-20260929-chestcanvas/`).
- Level-60 A/B benchmark, 5 builds' runs after the fix:
  - no run froze (timeScale 0 at the end of a segment is a chest screen open at that moment);
  - with a 5 s watchdog, one "stall" was a 5-item chest that legitimately takes 6–7 s, so the threshold is now 15 s; the last two runs had 0 stalls and 0 errors.
- Blood A/B in those runs again showed no cost (blood segments as fast or faster than the no-blood ones).

Seen, not changed: with a level-60 hero, FPS falls from segment to segment regardless of blood. Run `ab-lv60-5` went 41.9, 31.0, 20.3, then about 21 in Stage 10, with 2,500–4,500 kills per 30 s. Something builds up with heavy killing; this belongs to the heavy-kill performance pass (`P`). Fixed by the heavy-kill pass below.

### Heavy-kill performance pass (owner: "heavy kill performance pass shuru karo", 2026-09-29)

Tools:
- `PerformanceBenchmark -sh-benchmark-heavy`: a level-N hero (`-sh-benchmark-level 60`) plays Stage 1 from 20:30 as six back-to-back 20 s windows. Each window records FPS, kills, enemies, gems, playing VFX, live game objects, log lines, save-file writes, gen-0 GCs and managed memory.
- `-sh-benchmark-profile <folder>` (development build: `SOULHUNTER_BUILD_DEVELOPMENT=1` for `SoulHunterBuildRunner`) captures 5 s of windows 1 and 6.
- `ProfileSummary` (menu Soul Hunter/Debug/Summarize Profile, or `-executeMethod ...ProfileSummary.RunFromCommandLine -sh-profile <file.raw>`) writes the main thread's time per marker.
- `D:\SoulHunter-Validation\heavy-run.ps1` syncs the copy, runs both suites, builds, benchmarks and summarises.
- The benchmark turns off Pause When Unfocused on its throw-away save; a run sat paused when its window lost focus.

What the profile showed (reference laptop, release build):
- Live game objects grew from ~7,000 to 30,000 in two minutes, while FPS fell from 72 to 18.
- **Floor chickens and coins piled up without limit:** a kill rolled a chicken at 5% and a coin at 2%.
- Every coin and chicken on the ground ran `Physics.OverlapSphereNonAlloc` each frame (466 calls, 2 ms a frame), and 850 bobbing pickups each ran their own `Update` (1.2 ms).
- Gold pickups wrote the whole save file on every coin (up to 75 writes per 20 s).
- Per-kill and per-pickup `Debug.Log`s (for example "Rare Drop: Floor Chicken!" 733 times) each wrote a stack trace.

Changes:
- **Owner decision "VS rule: no enemy drops"** (AskUserQuestion; VS wiki: Floor Chicken, Gold Coin (pickup), Coin Bag, Vacuum and Orologion drop from light sources, and enemies drop Experience Gems). `EnemyDrop` now leaves only the gem.
  - Magnet 1%, Time Freeze 1%, coin 2% and chicken 5% are gone; they come from Soul Lanterns, stage items, chests and Gold Fever.
  - Defect fixed with it: a chicken used to **replace** the kill's gem, so 5% of kills gave no XP.
  - `EnemyData.DropChanceChicken/DropChanceGold/GoldValue` and `EnemyDrop.ChickenDropChance/GoldDropChance` stay on the assets but are unused. `GoldRewardTests.Elites_DropMoreAndBiggerCoins_ThanStageEnemies` was removed (superseded rule).
  - Balance note: fewer heals from kills; the owner chose it knowing that.
- `PlayerReach.Within(point, radius)`: the same answer as an overlap sphere on the Player layer (`Collider.ClosestPoint` against the player's Player-layer colliders, after a distance reject), with no physics query. Used by `Pickup` and `HealthPickup`.
- `PickupTicker` ticks every `Pickup` and `HealthPickup` in one loop (`ITickedPickup.Tick` replaces their `Update`). `PickupIdleMotionRunner` moves every bobbing pickup in one loop and skips pickups more than 30 units from the hero.
  - Found by the tests: a pooled copy can carry its template's list index (every pooled chicken said 1, so none was ticked and none could be picked up). Both runners now trust an index only when the list agrees.
- `SaveService.SaveSoon()`: gold is saved at most every 2 s (`SaveSoonSeconds`). `SaveFlusher` writes a pending save when due and on quit, so a crash can lose at most the last 2 s of gold. `EconomyService.AddGold` uses it; spending still saves at once. `SaveService.SavesWritten` counts writes.
- Removed hot info logs: rare drops, Healed, +gold, Shadow Scythe. "Game Saved" is logged in the editor only.

Validation:
- `PickupPerformancePlayModeTests` (R):
  - `PlayerReach` matches `Physics.CheckSphere` on 400 random points;
  - a coin and a chicken next to the hero are collected, and one 12 units away is not;
  - every enabled pickup is on the ticker once;
  - the bobbing runner moves pickups and drops collected ones;
  - a burst of 10 coins writes no save at once and exactly one after 2.5 s.
- `EnemyDropPlayModeTests` (R): 60 kills at Luck 10 give 60 gems and no chicken, coin, magnet or time freeze.
- EditMode 472/472, PlayMode 181/181, save untouched (`D:/SoulHunter-Validation/run-20260929-heavykill-fix4/`).

Benchmark (release build, level-60 hero, Stage 1 from 20:30, six 20 s windows):

| Run | Before (`run-20260929-heavykill`) | After (`-fix4`) |
|---|---|---|
| Live objects | 7,062 → 30,063 | flat, 7,500–9,200 |
| FPS by window | 72 → 51 → 29 → 23 → 20 → 18 | run 1: 70 / 63 / 61 / 68 / 74 / 68; run 3: 64 / 64 / 63 / 63 / 64 / 65 (up to 2,790 kills per window) |
| 1% low | fell to 9 | runs 1 and 3: 30–43 |
| Save writes per window | up to 75 | 0–9 |

- The profiled window 6 averages 16.7 ms (development build). Pickups are gone from the top of the profile: bobbing 0.23 ms. The rest is the normal horde (`EnemyController.FixedUpdate` 1.4 ms for ~300 enemies).
- Still under the hard minimum: run 2's loadout killed 5,400–7,500 per window (over 300 kills a second) and held 25–49 FPS average with a 1% low of about 25. Its two paused windows came from the focus-loss pause (now off in the benchmark). Kill handling itself (death, gem, VFX, blood, damage numbers) is the next cost at such rates.
- Not measured on a full 30-minute run or on the 8 GB reference.

Not yet played by the owner.

Not yet played by the owner.

### Lantern drop balance and the dying body (owner, 2026-09-29)

Owner: "lantern drops ka balance pass karo, blood drop ko bhi balance karo kyun ki enemy marne per gayab ho jata hai per blood dikhta rahta hai".

**Soul Lanterns, following vampire.survivors.wiki (Light source and Luck pages, fetched 2026-09-29).** The drop weights and level gates already matched the wiki table (Gold Coin 50, Floor Chicken 12, Coin Bag 10, Vacuum 2 from level 12, Orologion 2 from level 4, Rich Coin Bag 1 from level 5, and the Rosary / Nduja / clover stand-ins). Coin values 1 / 10 / 100 are now checked against the Gold Coin (pickup), Coin Bag and Rich Coin Bag pages. Added:
- **Luck** (wiki Luck, "Higher rarity pickups": `itemWeight = rarity x totalLuck`): every drop except Gold Coin and Coin Bag weighs its rarity × Luck (`LightSourceDrops.Weight`, `Pick(level, luck, roll)`). Before, Luck changed nothing.
- **Per level**: the wiki gives every VS stage its own chance, cap and maximum. The owner had no preference, so the stage-bonus climb is used (`LightSourceSpawner.ForStage`):

  | Level | VS stage | Chance | Cap | At once |
  |---|---|---|---|---|
  | L1 | Mad Forest | 10% | 50% | 10 |
  | L2 | Inlaid Library | 7.5% | 50% | 10 |
  | L3 | Dairy Plant | 20% | 60% | 10 |
  | L4 | Gallo Tower | 7.5% | 50% | 10 |
  | L5–L10 | Cappella Magna | 20% | 80% | 20 |

  Inlaid Library, Dairy Plant and Gallo Tower's fixed spawn spots are not modelled.
- **At the limit** (wiki): the attempt still rolls, at the base chance with no Luck. A success despawns the oldest lantern and places a new one at the off-screen spot nearest the player (`ReplaceOldest`). Before, spawning stopped at 10.
- `LightSource.AnyBroken` lets `AutoplayBot` log every broken lantern (`lantern` events). The bot now also walks to lanterns within 18 units and to items within 14 when fewer than 6 enemies are near, like a player. Before, it ignored them and broke only 2 lanterns in 5 minutes.

**The dying body** (the owner chose "Body briefly stays (VS-style)"; wiki enemy pages show death animations of about 1.8–2.5 s):
- `EnemyCorpses`: at death (`EnemyDrop`), a still copy of the enemy's drawn meshes stays where it died. Skinned meshes are baked in their last pose; the hit flash, a property block, is not copied.
- The copy holds, then sinks into the ground (1.1 × its height, eased) and shrinks to 0.6 over `LingerSeconds` 0.7, ending with the neck jet (last spurt at 0.5 s).
- The enemy itself still returns to the pool at once, so kills, spawning and pooling are unchanged.
- At most 32 corpses (8 meshes each); beyond that an enemy vanishes as before. Ghosts get a body too, but still no blood. Timings are provisional.

Validation:
- `LightSourceTests` (T): Luck weighting, per-level numbers and caps.
- `LightSourcePlayModeTests.AtTheLimit_ANewLanternReplacesTheOldest_NearestThePlayer` (R).
- `EnemyCorpsePlayModeTests` (R): the body copy has every drawn mesh, sinks and shrinks by 0.5 s, and is gone by 0.85 s while the enemy was pooled at once; 60 kills in one frame leave exactly 32 corpses, all gone after 0.9 s.
- EditMode 474/474, PlayMode 184/184, save untouched (`D:/SoulHunter-Validation/run-20260929-corpse-lantern/`).
- Heavy benchmark (release, level-60 hero), FPS by 20 s window:
  - run 2 (1,000–2,300 kills per window): 59–65;
  - run 1 (4,100–7,000 kills per window): 45–56.
  - The corpse update is not in the profile's top 60.
- Editor play:
  - `autoplay-editor-74` (Stage 1, 5 min, lantern-seeking bot): 7 lanterns broken (5 Gold Coins, 1 Rich Coin Bag, 1 chicken), 411 gold with 2 chests, 0 errors, min 60 FPS. **The bot died at 4:48** at level 14. Earlier bot runs survived 5 minutes when kills still dropped chickens.
  - `-73` (Stage 5, level-30 hero, god mode, 3 min): 16 lanterns (9 Gold Coins, 2 Coin Bags, 5 chickens), 262 gold.
  - The sinking body is hard to catch in 2 s screenshots; it is covered by the tests (partial `V`).
- Balance note for the owner: with VS drops, healing now comes from lanterns, stage items, Recovery and the maxed-level chicken card. Whether early Stage 1 is too hard is a balance decision.

Not yet played by the owner.

### Heal balance pass (owner: "heal balance pass karo", 2026-09-29)

Why: after the VS no-enemy-drops rule, a lantern-ignoring bot run had **no healing at all**: 19 hits of 5 over 5 minutes, Recovery 0, and it died at 4:48.

Checked against vampire.survivors.wiki (fetched 2026-09-29):
- **Damage matches VS.** Mad Forest's bat deals 5 and its skeleton and Mudman 10; Soul Hunter's L1 enemy deals 5 with 10 HP. Player Max Health is 100, and Recovery starts at 0 (Max Health and Recovery pages).
- The wiki does not give how often an enemy's damage lands; the Armor page says "multiple times per second". Soul Hunter's `TouchDamage` stays at 0.5 s per enemy.
- **What VS has and Soul Hunter lacked** (PowerUps page):
  - the **Max Health** PowerUp: +10% per rank, 3 ranks, multiplied (+33.1%), initial cost 200;
  - the **Recovery** PowerUp: +0.1 HP/s per rank, 5 ranks (+0.5), initial cost 200.
  - Soul Hunter's shop sold only Might, Armor, Greed and Revival.
- Recovery page: "Recovery also boosts healing from pickups, each 0.1 translates to an additive 10% boost".

Changes:
- `MetaPowerUps` (Foundation) holds the VS ranks, bonuses and prices. `GameData.MetaMaxHealthLevel` / `MetaRecoveryLevel` are clamped by `SaveService`'s repair; an older save loads them as 0.
- Superseded the next day by the full VS PowerUp shop (below); the heal effects stay the same.
- `PlayerController.ApplyHealingPowerUps`: max health × 1.1^rank after the character and Soul Eggs, with the run starting at full health, and Recovery +0.1 × rank via `PlayerStats.AddRegen` (`PlayerRecovery` heals each second).
- `HealthPickup.HealWithRecovery`: a floor chicken, and the maxed-level chicken card (`MaxedRewards`), heals 30 × (1 + Recovery).
- `CharacterStatSheet.Build` and the select screen include both PowerUps.
- `AutoplayBot -sh-autoplay-heal-powerups` plays with both at max rank. Otherwise the bot's throw-away save has none, so runs compare like with like.

Validation:
- `HealBalanceTests` (T): the multipliers, the Recovery cap, pickup healing (30 → 45 at Recovery 0.5) and the select-screen sheet (133 HP, 0.5 Recovery).
- `HealBalancePlayModeTests` (R):
  - a run with both at max starts Kael at 133/133 with 0.5 Recovery, regains health each second, and a chicken heals 45;
  - the MainMenu shop sells both only up to rank 3 and 5, charges only real purchases, and switches the maxed buttons off.
- EditMode 478/478, PlayMode 186/186, save untouched (`D:/SoulHunter-Validation/run-20260929-heal/`).
- Editor play, Stage 1, 6 min, no god mode, lantern-seeking bot:

  | Run | Healing PowerUps | Result | Hits taken | Lowest health |
  |---|---|---|---|---|
  | `-75` | no | survived | 0 | 100/100 |
  | `-77` | no | survived | 7 | 60/100 |
  | `-76` | rank 3 + 5 | survived | 17 | 110/133 |
  | `-78` | rank 3 + 5 | survived | 15 | 94/133 |

  - With the PowerUps the hero took more than twice the hits and never fell below 94/133. Lanterns broken per run: 8–25 (chickens 0–5).
  - Random loadouts make these runs rough; `-75` killed 24,991, most likely through Soul Toll wipes.
  - 0 errors in every run.

Not yet played by the owner.

### VS PowerUp shop (owner: "everwhere follow DOT VS ONLY", 2026-09-30)

Everything is from vampire.survivors.wiki/w/PowerUps and each PowerUp's own page (fetched 2026-09-30).

**The list.** `PowerUps` (Foundation, `Save/SaveData/PowerUps.cs`) holds 23 PowerUps in the wiki's order, each with its rank limit, bonus per rank and initial cost:

| PowerUp | Per rank | Ranks | Initial cost |
|---|---|---|---|
| Might | +5% | 5 | 200 |
| Armor | +1 | 3 | 600 |
| Max Health | ×1.1, multiplied | 3 | 200 |
| Recovery | +0.1 HP/s | 5 | 200 |
| Cooldown | −2.5% | 2 | 900 |
| Area | +5% | 2 | 300 |
| Speed | +10% projectile speed | 2 | 300 |
| Duration | +15% | 2 | 300 |
| Amount | +1 | 1 | 5,000 |
| Move Speed | +5% | 2 | 600 |
| Magnet | ×1.25, multiplied | 2 | 300 |
| Luck | +10% | 3 | 600 |
| Growth | +3% | 5 | 900 |
| Greed | +10% | 5 | 200 |
| Curse | +10% | 5 | 1,666 |
| Revival | +1 | 1 | 10,000 |
| Omni | +2% Might, projectile Speed, Duration and Area | 5 | 1,000 |
| Charm | +20 enemies per wave | 5 | 10,000 |
| Defang | 3% of enemies harmless | 5 | 10 |
| Reroll | +2 | 5 | 1,000 |
| Skip | +2 | 5 | 100 |
| Banish | +2 | 5 | 100 |
| Preserve | 10% chance to keep a Reroll/Skip/Banish | 5 | 500 |

**Price** (wiki Cost section): `InitialPrice` when nothing at all has been bought, else `InitialPrice × (1 + Bought) + floor(20 × 1.1^TotalBought)`. **Refund** returns every rank's price; the fees are additive since VS v0.7.2, so the order does not matter.

**Left out, and differences from VS:**
- The four Seals are left out: they are used in VS's Collection menu, which Soul Hunter does not have.
- ~~VS unlocks some ranks through achievements and items; every rank could be bought.~~ Locked since 2026-10-01; see "VS locked PowerUps" below.

**Save** (version 2):
- `GameData.PowerUpRanks` holds the ranks.
- `SaveService.Migrate` v1 → v2 refunds the old fixed-price shop once, as the owner chose ("Refund all"): Might 500, Armor 300, Greed 400 and Revival 1000 per level, and the 2026-09-29 Max Health / Recovery buttons 200. It then zeroes the old fields.
  - The owner's real save (Might level 9) gets 4,500 gold back on its next load. It had 21,271 gold on 2026-09-30.
- The repair keeps only known PowerUps, one entry each, within their VS ranks.

**Effects in a run:**
- `PlayerStats.ApplyPowerUps`, with Max Health in `PlayerController`. This replaces the old +10% Might / +20% Greed per level.
- Curse now also raises the wave's enemy minimum (Curse page: frequency, quantity, speed and health), and Charm adds its flat number after Curse (Charm page).
- **Reroll, Skip and Banish start at 0 as in VS**, +2 per rank plus Soul Eggs. Soul Hunter's old free 2/2/2 per run is gone; the scene's `_starting*` values are no longer read.
- **Skip gives experience**: 20% of the next level (the Skip page's tips; the page gives no formula).
- **Preserve** keeps a used charge at 10% per rank.
- **Defang** (`EnemySpawner.Defang`, `EnemyController.IsDefanged`):
  - applies only to ordinary wave spawns; chest elites, bosses and event enemies never (Defang page);
  - a defanged enemy deals no contact damage and runs no abilities, and is tinted green through `DamageFlash.SetTint`, which survives hit flashes;
  - the new VS Display option **Show Defanged Enemies** (on by default) turns the tint off.

**Shop screen** (`PowerUpMenuUI`):
- Built at runtime in `PowerUp_Panel` from a copy of an old button, so `MainMenu` is not edited; the old four-button grid is hidden.
- A 6 × 4 grid of dark cells (icon, name, rank; gold when selected, green when maxed), an info box (description, rank, VS price or MAX), **Buy** and **Refund PowerUps**.
- Icons are Soul Hunter's passive of the same stat, as VS PowerUps show their passive item; none for Greed, Omni, Charm, Defang, Reroll, Skip, Banish and Preserve.

`AutoplayBot`:
- `-sh-autoplay-heal-powerups` now sets Max Health 3 and Recovery 5 through `PowerUps`.
- `-sh-autoplay-shop-shot` opens the shop before the run, buys a few ranks and screenshots it.

Validation:
- `PowerUpShopTests` (T):
  - the wiki list;
  - the price formula, with the wiki's own example of five ranks of a 200-gold PowerUp: base costs 3,000 plus fees 22 + 24 + 26 + 29;
  - Refund returns exactly what was paid;
  - the owner-style v1 save (Might 9 → +4,500, Armor 1 → +300, Max Health 2 → +400) is refunded once;
  - the repair clamps.
- `HealBalanceTests` and `CharacterStatBlockTests` were moved to the catalog; `LevelUpActionTests` and `RunModeTests` grant charges explicitly.
- `PowerUpShopPlayModeTests` (R):
  - the MainMenu shop has a cell for every PowerUp, sells Recovery at 200, 422, 624, 826 and 1,029, stops at rank 5, shows the fee on the next PowerUp, and refunds everything;
  - a run with every PowerUp maxed gets each effect: Might +35% with Omni, Armor +3, Max Health ×1.331, Reroll/Skip/Banish 10 each, Charm 100, Defang 15%, Preserve 50%;
  - without PowerUps a run has no Reroll/Skip/Banish;
  - a defanged enemy deals no damage and is green;
  - Skip gives experience, and Preserve keeps some charges.
- EditMode 483/483, PlayMode 190/190, save untouched, both before and after the final UI fix (`D:/SoulHunter-Validation/run-20260930-powerups/`, `-powerups2/`).
- Editor play `autoplay-editor-80` (`-sh-autoplay-shop-shot`, 2 min): the shop screenshot shows all 23 cells readable, Recovery 5/5 green, Greed selected at price 242 (200 + fee after 8 ranks), 0 errors, min 84 FPS (partial `V`).

Not yet played by the owner.

### Release push, wave 1 (owner: "release level tak DOT formula ke anusar full and final close karo", 2026-10-09)

Specs for 9 items were written from the raw wiki pages and the code, then checked by a second agent that tried to refute each one (`D:/SoulHunter-Validation/release-specs-wave1.md`). Owner approvals and the open questions below are collected for the end, as the owner asked.

- **P1-13 Magnet** (wiki Magnet, Pickups, Attractorb, Treasure_Chest, Vacuum):
  - `PlayerStats.Magnet` = base × character × PowerUp × items ("Magnet = 30 * Character bonus * PowerUp multiplier"; "Magnet modifications stack multiplicatively"). The character bonus used to be added. A Soul Egg's +0.01 goes on the base. No cap.
  - One radius for every pickup, `PlayerStats.MagnetRadius` = 4 units × Magnet (`BaseMagnetRadius`; Soul Hunter's mapping of VS 30, wiki silent). The chicken's own 3-unit base is gone.
  - Coins, bags, Time Freeze, Vacuum, Soul Toll and the charms fly in inside it (`Pickup.Magnetize`, from `PlayerExperience`'s sweep). Before, they waited to be walked over (1.5 units). Chests are never pulled ("Doesn't get attracted to the player"); stage items stay walk-over (wiki silent).
- **P1-14 Chest tiers per boss** (wiki Treasure_Chest, Template:Treasure, Mad_Forest):
  - `ChestTiers`: each chest elite's minute and the 25:00 boss carry their Mad Forest row, on every stage: 1:00 0/0/30 with evolution; 3:00 0/5/40; 5:00 1/5/100; 7:00 and 9:00 3/10/50; 10:00 3/10/100 with evolution; 11:00 and 21:00 Arcana; 12:00 and 16:00 1/5/50 with evolution; later minutes 3/10/100 with evolution.
  - Three separate rolls, 5 then 3 then 1, each `random × 100 < chance × Luck`, then the fallback. 1-1-3-1-1-5 still comes first.
  - Whether a chest can evolve now comes from its boss, not the clock at the kill. An erased carrier passes its row on (`EnemyDrop.PendingChests` is a queue).
  - Only slot 1 can be an evolution. "Weapon" slots level an owned weapon, or any owned item when no weapon is left (wiki silent).
  - An Arcana chest with nothing to give opens bronze at the Arcana cap, and silver when every Arcana is in use (Treasure_Chest, Arcana).
- **P1-16 Revival** (wiki Revival, Soul_Eater, Invulnerability_Time):
  - A lethal hit with a Revival left pauses the game and shows a Revive button (`ReviveScreen`: its own Expand canvas, the theme's Primary button, the Pause action off). Revive spends exactly one Revival, restores 50% of Max Health where the hero fell, then resumes. With none left, the run ends as before.
  - A revive no longer fires `OnDied` (`HealthController.TryPreventDeath`, as Shadow Kael's already did).
  - The revive heal counts as healing (`HealthController.OnHealed`, overheal excluded). Soul Eater gains +1 damage per 60 healed, up to +60.
  - The revival invulnerability runs on game time: 1.5 s (Soul Hunter's number for the wiki's "large amount") + the base 240 ms. A pause no longer uses it up, and a dash no longer ends it.
  - The revive effect and sound are existing placeholders (level-up + heal).
- **P1-17 Character rules** (wiki Antonio_Belpaese, Characters):
  - Corvan's +10% Might every 10 levels stops at level 50 (`CharacterData.LevelBonusUntilLevel`, "+50%" max).
  - A character's price rises by 10% of its base for each paid character already bought (`CharacterUnlocks.PriceFor`, rounded to whole gold, Soul Hunter's rounding). The select screen shows and charges that price.
- **P1-18 Hyper per stage** (wiki Stages and each mapped stage's infobox), with the existing mapping (L1 Mad Forest, L2 Inlaid Library, L3 Dairy Plant, L4 Gallo Tower, L5-L10 Cappella Magna):
  - Player speed 2.0 / 1.9 / 1.9 / 1.9 / 2.0 (L5-L10).
  - Enemy speed is the VS hyper/normal ratio on top of Soul Hunter's own pace: L1 1.818, L3-L4 1.52, L5-L10 1.4286. L2 stays at 1.65 (owner question).
  - Projectile speed 1.25 on L1 and 1.15 elsewhere. Luck +10% on L2-L10, swapped per stage (`LevelProgressionManager.ApplyHyperLuck`).
  - Hyper no longer shortens the spawn interval; it only raises the enemy minimum ×1.25 (wiki Enemies: Hyper "increases the minimum amount of enemies spawned and the movement speed").
- **P2-03 Merchant wares in Inverse** (wiki Merchant; Stages, Patch 1.0):
  - Reroll Sigil 1,000, Skip Sigil 100 and Banish Sigil 500 gold, +1 each while fewer than 20 are held.
  - Draw a Fate 20,000: one random Arcana not held, once.
  - Past three wares the shop uses two columns, and it scales with Expand. The Soul Egg stays in every mode.
  - The ware names and the Emblem icon on Draw a Fate are not owner-approved.
- **P3-07 Modes on Results:** already built; `RunResultsTests.ModeText_ListsTheActiveModes_AndTheEndlessCycle` (T) pins it.

Tests:
- New EditMode (T): `MagnetVsTests`, `RevivalTests`, `SoulEaterHealingTests`, `HyperModeTests`. Extended: `ChestRewardTests` (tier table, separate rolls, odds), `CharacterStatBlockTests` and `CharacterUnlockTests` (cap, price), `WanderingMerchantTests`.
- New PlayMode (R): `MagnetPlayModeTests`, `RevivalPlayModeTests`, `WanderingMerchantInversePlayModeTests`, `ChestEliteAndBossPlayModeTests.ChestElite_CarriesItsMinutesTiers`, `PentagramPlayModeTests.AnErasedCarrier_PassesOnItsBossesChestTiers`.
- `TirajisuPlayModeTests`, `GameOverFlowPlayModeTests`, `RunResultsPlayModeTests` and the Shadow Kael tests now answer the Revive screen.

Validation: EditMode 614/614, PlayMode 304/304, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261009-wave1-full/`).

Build play (`D:/SoulHunter-Validation/run-20261009-wave1-screens/`, script `wave1-screens.ps1`; new bot options `-sh-autoplay-revival` and `-sh-autoplay-inverse`): 0 errors, save untouched (partial `V`).
- Inverse merchant on Level 3: all 7 wares in two columns inside the panel, and the game behind is dimmed.
- Revive on Level 10: the screen showed and the hero revived at 50/100.
- **Defect found and fixed:** the game behind the Revive screen was not dimmed. The screen was a canvas nested in the HUD, so it kept a 100x100 rect: the veil was a small square behind the panel and the Expand scaler did nothing. It is now a root object in the HUD's scene. `RevivalPlayModeTests.TheReviveScreen_VeilsTheWholeScreen` (R) checks for a root canvas and a veil that covers the screen. Revival fixture 6/6, EditMode 614/614 (`-reveil2/`).

**Owner answers (2026-10-09):** "jis question ka jabab wiki ke pass nahi hai waha balance method apnayegein". Each question was first checked on the wiki again, by a research agent with a second agent that tried to refute it (`D:/SoulHunter-Validation/release-answers-wave1.md`). Where the wiki has no answer, the value comes from the balance method, with the reason given. Stalker: the owner chose the latest VS version.
- **Chest level=2: wiki answers.** Treasure_Chest's v1.6.0 notes name the chest screens "lvl 1", "lvl 2" and "lvl 3" (1, 3 and 5 items), and Template:Treasure/doc says tier3/tier2/tier1 are "The % chance for 5 rewards / 3 rewards / 1 reward". So a level-2 row that fails all three rolls gives 3 items (`ChestTiers.FallbackItems`).
- **Weapon slot with no weapon left: wiki partly answers.** Treasure_Chest Patch 0.2.7: chests "can now only level up existing weapons (or give extra coins if no weapons can be leveled up)". The amount is not given; balance method: one Big Coin Bag (25 × Greed), as for any slot with nothing left. Before, the slot levelled any owned item.
- **Stages 2-10 chest rows: balance method.** The wiki has each mapped stage's own rows (Inlaid Library, Dairy Plant, Gallo Tower, Cappella Magna), but their minutes differ from the chest-elite minutes the owner fixed for every stage (Mad Forest's). The Mad Forest rows stay on all stages.
- **Valerius: wiki answers.** VS Divano Thelma has the same "+1 Armor every 5 levels", "until level 25" (+5 at most), so `Char_Valerius.asset` has `LevelBonusUntilLevel` 25.
- **Character price rounding: balance method.** Rounded down, as VS rounds the PowerUp fee (`floor(20 × 1.1^TotalBought)`).
- **Revival invulnerability: wiki silent** ("a large amount of invulnerability frames"; no patch note gives a length). Balance method: 1.5 s + the base 240 ms, which is clearly longer than the 1 s cap on the Invulnerability Time stat. The game stays paused under the Revive button: the wiki says the hero "resumes control ... exactly where they left off".
- **L2 Hyper enemy speed: the wiki contradicts itself.** The Inlaid Library infobox gives x1.25 in both columns; the Enemies page says Hyper speeds enemies up "in each stage". Balance method: every other base stage's Hyper enemy speed equals its Hyper player speed, and Dairy Plant and Gallo Tower share Inlaid Library's normal x1.25, so Level 2 uses their 1.9 / 1.25 (was 1.65).
- **L6-L10 Hyper gold: wiki answers.** The DLC stages go from +50% to +100% in Hyper (Mt.Moonspell, Lake Foscari, Polus Replica); Soul Hunter's L6-L10 use the DLC +50%, so 2.0 stays.
- **Endless Revival ware: wiki gives no price.** Balance method: 10,000, the price of the Golden Egg it replaces and of the Revival PowerUp. "Second Breath" is sold in place of the Soul Egg in Endless mode ("instead of Golden Eggs"), once per visit, so once per cycle (`WanderingMerchant.TryBuyRevival`).
- **Endless "max incoming damage cap": wiki silent** (it is never defined). Soul Hunter has no such cap, so nothing is built.
- **Inverse speed: wiki answers.** The infoboxes write "+0.005 per minute" added to the stage's enemy speed. Soul Hunter adds 0.5% of its own pace per minute, within 3% of that at 30:00 (±5% rule).
- **Inverse + Endless health: balance method.** They now add, x(3 + 0.05 × minute + cycle), not multiply: Endless raises "their base HP" and Inverse bonuses are "additive with other enemy bonus modifiers" (`RunModes.EnemyHealthMultiplier`).
- **Draw a Fate: wiki answers.** "All unlocked Arcanas are available as choices": the player chooses. Until the Arcana choice screen exists (P2-01) it stays random.
- **Hyper starting spawns: wiki answers** (10 → 15 on Mad Forest, 50 → 60 on Inlaid Library and Gallo Tower). Soul Hunter fills the minute-0 minimum at once, which on Level 1 already covers 15 (minimum 15, ×1.25 in Hyper). Not built separately.
- **Magnet radius: the wiki contradicts itself.** One tip puts off-screen reach at about 16× base Magnet, another calls 12× "around 4 times". Balance method: 4 units stays, the radius that bot runs were already balanced on.
- **Stage items and Magnet: wiki answers.** Mad_Groove_(VIII) is "the only way" to attract stage items, so a pickup the stage placed (chicken, Vacuum, Time Freeze) is not pulled and is picked up only by walking over it.
- **Modes on the HUD: wiki silent.** VS shows no modes on the HUD, so none are shown; Results shows them.
- Validation of these answers with the Stalker XL and the Arcanas below: EditMode 627/627, PlayMode 316/316, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261009-wave1b-full/`). Not played by eye yet (`V`).
- **Not changed:** the Passive_items "Stage items" rule that a stage passive found with 6 passives held becomes an extra passive (Soul Hunter pays a coin bag). It is a separate gap for a later wave.
- P3-01 evolution hints (not built): the indicator's look is art for the owner.

**Stalker XL (P1-15, owner: "letest", 2026-10-09).** The wiki has three Stalkers side by side (The_Stalker); the XL is the one that comes as a stage event and drops chests, so Death's Shadow follows it:
- 655,350 × player level health, speed VS 300 (9 units/s) × Curse × run-mode enemy speed, 50 contact damage (× the Endless multiplier), no knockback, freeze resistance 2 (Clock Lancet cannot freeze it; Orologion still does), debuff resistance 1, no XP (statbox).
- Stages 5 and 9 (Soul Hunter's placement): 8:00 at 30% for 120 s and 12:00 at 30% for 60 s (Dairy Plant rows). The chance is divided by Luck (wiki Luck: "chanceWithLuck = eventChance / totalLuck").
- Ordinary weapons cannot hurt it ("otherwise intangible"). Only instant kills defeat it: Pentagram (no chest: "they do not drop chests from the blast"); Eclipse Sigil, Crimson Shroud and Soul Toll (a high-quality chest that can evolve, 6/66/100); rail carts (a lower-quality chest, 3/10/100). It gives no gem and counts as no kill.
- The chase follows the wiki's "will follow the player around but will not actively make contact. Occasionally, it will speed up or slow down". The wiki gives no rhythm; balance method: 8 s trailing 3 units off, then 2 s closing in, at its own speed. A hero who keeps moving stays ahead; one who stands still is hit.
- Not built: the chests' extra evolutions (`reward4/5=evo`, "can exceptionally evolve up to 3 weapons"), the 1% event at 0:00, the red-ghost jingle, and the killers Soul Hunter does not have.
- Tests: `StageHazardTests` (T) and `StageHazardPlayModeTests` (R): stats, rhythm, the 30% roll, the 60 s event, that ordinary hits, the stage wipe, Clock Lancet, Garlic and Requiem do nothing, and each killer with its chest.

**Arcanas (part of P2-01).** The four Arcanas Soul Hunter has now follow their own wiki pages (fetched 2026-10-09):
- **Awake:** "Gives +3 Revivals. Consuming a Revival gives +10% Max Health, +1 Armor, and +5% Might, Area, Duration, and Speed", on every revive from any source. Before, it gave +1 Armor and +10% Max Health once. The extra Max Health is not filled in (wiki silent; Hollow Heart's rule).
- **Iron Blue Will:** only Knife, Thousand Edge, Axe and Gun get up to 3 bounces; Death Spiral bounces off the screen edge once. Before, every projectile got 1 bounce.
- **Waltz of Pearls:** only Magic Wand, Holy Wand and Fire Wand get up to 3 bounces; Hellmaw Comet bounces off the screen edge once, Cross and Heaven Sword twice. Before, every projectile got 3 bounces.
- **Gemini:** Peachone, Gun and Hellfire Breath get a counterpart. It levels with its weapon, takes no slot, cannot evolve and is not in the weapon list (`GeminiCounterpart`). The Peachone twin flies 1.5 units further out and the Gun twin's pattern is turned 15°: the wiki says only "slightly", so these are balance values. Before, Gemini did nothing.
- Shadow Kael's mirrored weapons never get Arcana effects (a leak gave his Magic Wand Waltz bounces).
- Tests: `ArcanaEffectTests` (T) and `ArcanaEffectsPlayModeTests` (R).
- Still to build in P2-01: the other 18 Arcanas, the start-of-run pick and the pick-from-four chest screen, and Soul Hunter's own card names and art (owner).

### Release push, wave 2a: PowerUp switch, character sort, Options, flaky tests (2026-10-09)

**P3-03 PowerUp on/off** (wiki PowerUps: "After maxing out a PowerUp (except Rerolls, Skips, Banishes and Seals), the player can then disable the PowerUp"; maxed and active is "yellowish", maxed and not active "dark gray"):
- A maxed PowerUp's Buy button becomes Turn Off / Turn On: free, saved at once (`GameData.DisabledPowerUps`; older saves load with every PowerUp on). Reroll, Skip and Banish cannot be switched off.
- A switched-off PowerUp gives nothing in a run. Every reader uses `PowerUps.ActiveRank`: stats, Max Health, the select-screen sheet and Results.
- The cell shows the theme's new `Cards.Off` colour and the word OFF; the info box says MAX - ON / MAX - OFF.
- Wiki silent, balance method: a switched-off PowerUp still counts for fees and is refunded (switching exists to avoid a refund), and Results lists only active PowerUps ("All the powerups that were active during the run").

**P3-03 character select** (wiki Characters: "the player can only keep at least one group of characters visible ... sort ... default ..., alphabetically, or through the number of complete stages per character ... ascending or descending ... favorited characters will show up first"):
- SORT: DEFAULT / A-Z / LEVELS and ASCENDING / DESCENDING tabs, at the theme's tab size.
- The level filter refuses an untick that would leave no character visible.
- `GameData.CharacterStages` records the levels each character cleared, at 30:00 or by killing Shadow Kael (`CharacterUnlockTracker`). The stats panel shows "Levels Completed n/10".
- Favourites come first under every sort. They stay locked until Soul Hunter's Mindbender exists ("Unlocked through Mindbender", which comes from 50 Collection entries), the same as Banish, Omni, Charm, Defang and Preserve.
- Wiki silent, balance method: neither the sort nor the filter is saved between sessions.

**P3-04 Options** (wiki Options: "There are six sections of options: Favorites, Display, Sound, Gameplay, Co-op, and Account"; "The Favorites section contains a copy of some commonly changed options"; Screen Refresh Rate "Only visible when using Exclusive Fullscreen Window Mode"; Gameplay "Joystick Type: Legacy / Default"):
- The tabs are Favorites, Display, Sound, Gameplay and Controls. Favorites holds Sounds, Music, Damage Numbers and Flashing VFX, bound to the same settings as their home rows.
- Screen Refresh Rate shows only in Exclusive Fullscreen on PC (`DisplaySettingsApplier.ShowsRefreshRate`) and is saved in whole hertz (`GameData.RefreshRateHz`, 0 = Default).
- Joystick Type (`GameData.JoystickType`): the wiki gives only the names. Balance method: Legacy is the old fixed stick, and Default is a floating stick that jumps to the thumb in the left part of the screen and returns home on release (`FloatingStickZone`). Default is the default.
- Not built: FPS Cap (only in a v1.6.0 history line, not in the current option list); Language and Classic Sound Effects (Soul Hunter has neither); Co-op and Account.

**T-04 flaky tests** (causes found by reading code and logs):
- Pentagram: a 0 keep threshold still kept items 0.5% of the time, because `GroundItemEraser` rounds a roll under 0.005 to 0. The test now uses -1.
- Soul Meteor: the test waited for the destroyed marker and could read the health after the strike, measuring 0. It now stops on `HasStruck`.
- Duplicator: fast shots in a long frame were counted again; only a shot that jumps back toward its holder counts as reused.
- AutoplayBot: a fresh output folder each run (Windows deletes can still be finishing).
- Crowd timing: best of 5 batches.
- Chest canvas test: weapons and horde off, as in the other chest tests (cause not proven; the timeout now reports the phase).
- StageEvent and Bible stalls were fixed earlier.

Tests: `PowerUpToggleTests` (T), `PowerUpTogglePlayModeTests`, `CharacterSortPlayModeTests` (R), and `AccessibilityTests` / `AccessibilityPlayModeTests` (Favorites sync, refresh rate visibility and save, joystick types). Updated: shop, standard-size and theme tests.

Validation:
- Full run: EditMode 638/638, PlayMode 324/325, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261009-p3-full2/`).
- The one failure was a real defect. The locked "FAVOURITE: LOCKED" label read at 1.6:1, because the button never got the theme's locked text colour (`ThemedElement.ShowUsable(false)`). That is fixed.
- `UiStandardSizePlayModeTests` now measures a locked button against its disabled colour, the plate the player sees.
- The standard-size, character select and sort fixtures then passed 11/11 (`-p3-fav2/`).
- Not seen by eye yet (`V`).

### Release push, wave 2b: evolution hints, Evolutions list, Collection, Bestiary, Unlocks, Credits (2026-10-09)

**P3-01 and P3-06** (wiki Level_up, Patch 0.3.2: "Added info for discovered evolutions on Level Up panels"; 1.4.200: passives get the indicator too; Grim_Grimoire: "view all the evolutions and unions that have been discovered on their save file"):
- **Discovery record:** `GameData.DiscoveredEvolutions`, repaired by `EvolutionDiscovery.Repair`. An evolution is written only when a chest evolves a weapon in a real run (`ChestLogicController`). Debug and bot starts (`-sh-autoplay-evolve`) are not written.
- **Level-up hint:** weapon and passive cards in a discovered recipe show the other item's icon, the result in the gold evolved frame, and a line such as "Evolves with Hollow Heart into Bloody Tear".
  - Hidden for a recipe already made this run, in Banish mode, and on gold, chicken and Limit Break cards.
  - Balance method (the wiki gives no condition): the hint shows whether or not the other item is held.
- **Evolutions list:** an "Evolutions" button left of Resume on the pause screen (`EvolutionRecipesScreen`), with eight recipes per page.
  - Discovered recipes come first. Undiscovered ones show as "?" rows: VS hides them, but Soul Hunter has no other place yet to show how many remain.
  - Held items are framed and labelled "Held". The recipe made this run is framed whole; recipes that can no longer be made are faded and say why.
  - Balance method: open from the start. VS gates it behind the Grim Grimoire relic, and Soul Hunter has no relics yet.
  - The Milky Way map is not built.

**P3-02 main menu sections** (wiki Main_menu, Collection, Banish, Mindbender, Ars_Gouda):
- **Layout:** Collection, Unlocks, Credits and Bestiary join the menu. With more than four buttons it lays them out in two columns at the standard size and gap (`MainMenuLayout`). Back returns focus to the button that opened the screen (`MainMenuNavigation.Show`). **The new layout needs owner approval.**
- **Collection:** 67 entries (weapons with their evolutions, passives, pickups, the Soul Egg, Arcanas). Unknown entries show "?"; a found entry is one "used at least once" (`GameData.CollectionFound`, filled by `CollectionTracker` during runs).
  - Evolutions are read from `DiscoveredEvolutions`, so there is one record, not two.
  - Found from the start: the Soul Gem and Soul Coin (VS: Experience Gem, Gold Coin). The Friendship Amulet has no counterpart, so a fresh save starts with 2 entries, not 3.
  - Unlocks from the entry count: Banish ranks at 60/70/80/90/100 entries (wiki Banish) and favourites (Mindbender) at 50. With 67 entries today, only Banish rank 1 can be earned; the wiki numbers are kept.
- **Bestiary:** 46 enemy types with kill counts (`GameData.BestiaryKills`, saved at most every 10 s), names hidden until the first kill, and stats.
  - Balance method: it opens when a run first reaches Level 3, Dairy Plant's place, where VS puts the Ars Gouda. Soul Hunter has no relics.
- **Unlocks:** every character and PowerUp task with Done / Not done, read only from the save.
- **Credits:** the CC0 audio and OFL font credit files.
- **Not built:** PRESS TO START and Quick Start (owner decisions), and Secrets, Adventures, Online, DLC and Account (no counterpart).
- **New theme icons:** Collection, Bestiary, Unlocks and Credits use existing art. **They need owner approval.**

Validation:
- `EvolutionDiscoveryTests` and `CollectionBookTests` (T); `EvolutionRecipesPlayModeTests`, `MainMenuSectionsPlayModeTests` and `CollectionDiscoveryPlayModeTests` (R). `MainMenuLayoutPlayModeTests` and `UiStandardSizePlayModeTests` were updated.
- Full run: EditMode 657/657, PlayMode 330/331, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261009-p3b-full2/`).
  - The failure was the test's generic "more than 2 texts" check, which does not fit Credits: it has a title and one scrolling body.
  - The menu fixtures then passed 4/4 (`-p3b-menu/`).
- The first run (`-p3b-full`) failed the Collection tests because `results-run.ps1` did not sync `Assets/Resources`, where the new `GameContent` lists are. It now mirrors that folder too.
- Not seen by eye yet (`V`).

### Release push: all 22 Arcanas and the choice screens (P2-01, 2026-10-09)

Owner (2026-10-09, AskUserQuestion): the 18 missing Arcanas get Soul Hunter's own English names tied to each card's effect. The four built earlier keep theirs: Awake, Gemini, Iron Blue Will and Waltz of Pearls.

Each card follows its own raw wiki page (fetched 2026-10-09, saved in `D:/SoulHunter-Validation/wiki-arcana/`). Every number sits next to its quote in `ArcanaEffects.cs`; the names are in `ArcanaCatalog`.

| Card | Name | Effect |
|---|---|---|
| 0 | Soul Barrage | Gems give no XP; each collected gem flies at an enemy and explodes for 10 × Growth × Might. Chests hold at least 3 items, and an Arcana chest adds 2. |
| II | Final Echo | Listed weapons' shots explode when they expire: 10 + 13.5 per +100% Curse. |
| III | Restless Pilgrim | Listed weapons recharge faster while the hero moves. |
| V | Wavering Winds | Projectile Speed sways ×0.5 to ×1.5; +1% per level. |
| VI | Mending Wrath | Healing ×2; each heal hits nearby enemies for the amount healed. |
| VIII | Gathering Call | At pick-up and every even minute, gems, pickups, chests, stage items and lanterns are drawn to the hero. |
| IX | Ironblood Oath | Armor adds damage to listed weapons; missing HP adds damage to all weapons; Armor hurts touching enemies; retaliation kills raise Max Health. |
| X | First Blade | The starting weapon and its evolution +3 Amount; listed weapons +1. |
| XII | Shattering Frost | Every freeze explodes; Time Freeze drops twice as often. |
| XIII | Changing Fortunes | Growth, Luck, Greed and Curse are doubled in turn, 10 s each; +1% to all four every 2 levels. |
| XIV | Icebound Snare | Listed weapons freeze on hit, 25% × Luck. |
| XV | Midas Pulse | A coin bag starts Gold Fever; gold gained heals that much. |
| XVI | Keen Edge | Turns on the per-weapon Slash crits and doubles crit damage. |
| XVII | Ebbing Hours | Duration sways ×0.5 to ×1.5; +1% per level. |
| XVIII | Swelling Shadows | Area sways ×0.75 to ×1.25; +1% per level. |
| XIX | Burning Core | Listed weapons' hits explode; lanterns explode; the hero explodes when hurt; Hellfire Breath drops 4× as often. |
| XX | Empty Hands | +3 Reroll, Skip and Banish; +20% Might and −8% Cooldown per empty weapon slot. |
| XXI | Crimson Orbit | The Magnet range and listed weapons make damaging zones. |

Choice screens (`ArcanaChoiceScreen`, `ArcanaDraft`; wiki Arcanas):
- **Start pick (Arcana mode):** choose any unlocked Arcana or a random one, on a paused game.
- **Arcana chest:** pick 1 of 4 unheld cards (6 with a free Reroll at 23+ unlocked). Rerolls never repeat a card until every card has been shown. The draft can be skipped.
- **Draw a Fate (Inverse merchant):** "All unlocked Arcanas are available as choices". It cannot be skipped, because the gold is already paid, but it offers Random.
- The 3-Arcana chest limit and the bronze/silver fallback stay. The screen uses the theme, with an "Unlocked n / m" count and a "Works with" line that marks held weapons.

Balance method (the wiki is silent, impossible in Soul Hunter, or self-contradicting):
- **Unlocks:** every card is unlocked. Every VS unlock route (Randomazzo, level 50 with VS characters, 31:00) is impossible here, and locking them would leave Arcana mode empty.
- **Final Echo:** the page's rule and its own examples disagree; the written rule is used.
- **Numbers the pages leave open:** blast sizes, sway periods, the Restless Pilgrim recharge rate, the Icebound Snare freeze length (2 s × Duration, against freeze resistance) and the Crimson Orbit zone shapes. Each choice has its reason in code.
- Special damage is Magic, so it reaches the Level 7 ghosts; Mending Wrath is Holy.
- Shadow Kael's copy never gets Arcana bonuses (`PlayerStats` keeps them apart).

**Not built:**
- weapons and pickups Soul Hunter does not have;
- Clock Lancet's blood zone (its section on the page is empty);
- Darkanas and the VS unlock conditions (owner: later);
- card art and zone visuals (owner);
- held Arcanas on the HUD or pause screen.

Performance of Soul Barrage and Burning Core is not profiled.

Tests:
- EditMode (T): `ArcanaCatalogTests` 9 and `ArcanaChoiceTests` 7. `CollectionBookTests` now counts 22 Arcanas.
- PlayMode (R): `ArcanaChoicePlayModeTests` 5 (start pick, chest choice with a Reroll through the UI, the limit, Skip, Draw a Fate, fit and contrast at three window sizes) and `ArcanaCardEffectsPlayModeTests` 8 (sample cards in a real run).
- Validation: EditMode 673/673, PlayMode 344/344, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261009-arcana-full/`).
- Not seen by eye yet (`V`).

**Build screenshots of the new UI** (`D:/SoulHunter-Validation/run-20261009-newui-screens2/`, script `newui-screens.ps1`; 0 errors, save untouched, partial `V`):
- New bot options: `-sh-autoplay-menu-sections-shot`, `-sh-autoplay-evolutions-shot`, `-sh-autoplay-discover`, `-sh-autoplay-arcana`, `-sh-autoplay-powerup-off-shot` and `-sh-autoplay-sort-shot`.
- **Defects found in the first capture, and fixed:**
  - The Bestiary menu button had no icon. The theme asset pointed at the single-sprite id 21300000, but `mono_soul_flame` is a multiple-sprite picture whose sprite is `mono_soul_flame_0`.
    - `CollectionBookTests` did not catch it: NUnit's `IsNotNull` lets Unity's "missing object" through. The test now uses Unity's own null check.
  - The pause stats column showed through at the edge of the Evolutions list. The pause screen now steps back (`CanvasGroup`) while the list is open.
- `EvolutionDiscoveryTests` now forbids discovery only from the bot's `-evolve` debug start; the explicit `-discover` tool is allowed.
- Tests: EditMode 673/673, then the pause, Evolutions, menu, theme and bot fixtures 19/19 (`-uifix2/`).

### Release push, wave 3: enemy types on Levels 2-10, run modes, hitch fixes (2026-10-10)

Owner ("pura final karo", 2026-10-09). Answers: "VS waves + apne niyam"; source stages "Theme ke hisaab se"; P3-05 "Sab banao, shuru se khule"; names "Apne English naam".

**P2-02, VS waves and enemy types on Levels 2-10.** Each level plays its source stage's minute table and enemy statboxes (raw wiki pages fetched 2026-10-09, kept in `D:/SoulHunter-Validation/p2-02-wiki/`):
- Sources: L2 Inlaid Library, L3 Dairy Plant, L4 Gallo Tower, L5 Abyss Foscari, L6 Mt.Moonspell, L7 Lake Foscari, L8-L10 Cappella Magna (each level with its own names).
- 235 types with Soul Hunter names (`StageEnemyTables`, one table per source stage). `StageEnemyTypeBuilder` (menu Soul Hunter/Content/Build Level 2-10 Enemy Types) writes the EnemyData assets (`Data/Config/Enemies/Level02`-`Level10`) and prefab variants (`Prefabs/Enemies/Gameplay/Level02`-`Level10`), rebuilds the wave tables and orders the Bestiary.
- A checker compared all 177 statbox rows and every minute's minimum and interval against the wiki pages.
- New data: `FloatyFixedDirection` movement (Sneaky Heads), `HealthPerPlayerLevelFraction` (6.5 x Level), `Lives` (Thrice-Risen: 3 lives, no resurrection after an instant kill, never carries a chest), `DefangResistance`.
- Owner rules kept:
  - L2 and L7 homing types are Floaty, and every L7 type is spectral with no knockback;
  - L8 uses the elite model and its events stay elite-only;
  - **enemy pace stays the approved stage table** (L5 0.9 "a slow, heavy horde", L6 1.45 ...). The VS stage speeds (x1.25 / x1.4) are not applied, because an approved decision comes first.
- **The spawn interval floor is gone for minute tables:** a 0.25 floor turned VS intervals under a quarter of the wave's own interval into that quarter, so Level 1's 0.1 s ticks were really 0.25 s. Minute tables now use the wiki interval (timer floor 0.02 s). This also changes Level 1.
- Balance method:
  - Lake Foscari 22:00 "0.8" is read as 80;
  - its 25:00 "1 (150)" is read as 150, because Soul Hunter keeps its own boss;
  - Level 10 holds the 29:00 row after 29:00;
  - a VS XP of 0 becomes the smallest gem;
  - size and model come from a health rule.
- Not built:
  - each source stage's own map events, chest bosses and spawn sides (the owner's stay);
  - starting spawns above the minute-0 minimum;
  - bullets that can be shot down;
  - own models per type: tints on existing models, **not owner-approved art**.
- Tests: `StageEnemyTypeTests` (T) and `StageEnemyTypePlayModeTests` (R). Updated: wave, pattern, ability, movement, knockback, pickup and Collection tests.

**P3-05, run modes** (wiki Stages, Random_Events, Level_up, Options, Music):
- **Omens** (VS Random Events): at XX:30 a wheel picks an event that fires at the next XX:00.
  - 16 events with Soul Hunter names, among them Mending Ring, Feast Ring, Gilded Fever, Gallows Ring, Toll of Souls, Wandering Relic, Lantern Bloom, Swelling Horde, Soul Meteors, Fortune's Favour and the formations.
  - Events that need VS-only enemies, items or hazards are left out.
  - Balance method: every event is equally likely; there is no pick at 29:30; formations are not picked during a boss.
- **Blind Fate** (VS Random LevelUp): each level-up picks one of its drawn cards with no screen, and the picked item shows over the hero. It can be switched in the pause menu.
- **Inverse look:**
  - stage items, Metaglios and Rune Plates turn 180° around the start;
  - floor materials turn their hue by half and darken, staying in the comfort band;
  - ambient light and fog are untouched, so Level 2's darkness is unchanged;
  - Options, Gameplay: "Visually Invert Stages" (on by default).
- **Music selection** (`MusicSelection`, Music card on character select):
  - the 11 tracks, plus "Each Level's Own";
  - Normal, Faster and Slower (two semitones each way);
  - a Keep lock that persists between runs; without it the choice lasts one run.
- All open from the start (owner).
- Not built:
  - Inverse alternate tracks (no audio);
  - Hyper's music change (the wiki does not say what it is);
  - character themes;
  - a heart pickup (Mending Ring uses half-size chickens).
- The Music screen got its own Expand canvas (as the chest, level-up and Arcana screens), with its lower rows stacked from the bottom edge in theme units. On the menu canvas, a short window pushed Keep out of the window and over the speed tabs. Its cards use the card text role (they read at 1.07:1 before).
- Tests: `RandomizerModeTests` (T) and `RandomizersPlayModeTests` (R). The music tests make the AudioManager first, as `AudioPlayModeTests` does.

**Late-run hitches (T-02/T-03).** A development-build profile (`D:/SoulHunter-Validation/run-20261009-hitchprof/capture.summary.txt`, level-90 hero, Stage 1 from 23:00) found two main-thread stalls:
- 263 ms `SoundManager.LoadFMODSound` / `File.Read` when the chest screen played its jingle. The effect clips are imported without Preload Audio Data, so each was read from disk on its first play. `AudioManager.PreloadEffects` now loads every effect at startup (not the streamed music).
- 110 ms `SaveFlusher.Update`. The timed gold save now writes on a worker thread (`SaveService.FlushPendingSaveInBackground`). The JSON is still made on the main thread; writes are queued in order on the file they were made for; quit, `UseFile` and loads wait for a queued write.
- Tests: `SaveServiceTests.BackgroundWrites_LandInOrder_OnTheFileTheyWereQueuedFor` (T) and `AudioPlayModeTests.EveryEffect_IsLoaded_BeforeItFirstPlays` (R).
- `AutoplayBot` hitch events also log gen-0/1/2 GCs, heap and object count. `ProfileSummary` lists the slowest frames with their top markers. The heap stayed at 7-15 MB, so the hitches were not GC.
- Re-measured on a release build of `1b1832d1` (reference laptop, 1366x768, god mode; `D:/SoulHunter-Validation/run-20261010-perf-after/`, against `run-20261009-perf30/`), 0 errors, save untouched:

  | Run | FPS median / 5th percentile / min | Hitches over 250 ms |
  |---|---|---|
  | Stage 1, 30 min, level-1 hero (level 50, 9,696 kills) | before 58 / 51 / 38; after 60 / 58 / 48 | before about 10 (345-541 ms); after 0 |
  | Stage 1 from 10:00, level-60 hero, 6 min | before 57 / 43 / 20; after 60 / 54 / 37 | after 1 (350 ms, 42 s in, 25 enemies, no GC; not explained) |

  These are 5 s samples, not a profiler capture. 1% lows were not measured, nor the 8 GB reference or Android (partial `P`).

Validation:
- First full run: EditMode 885/885, PlayMode 359/365, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261010-wave3-full/`). The failures:
  - four music tests (no AudioManager in the test boot);
  - the moth pace test (it expected the VS pace);
  - Drowning Chalice in `DuplicatorEverywherePlayModeTests`: the counter missed one pool; it passed on rerun.
- Fixed, then the related fixtures: 45/46, 38/39, 19/20, 21/22 and 20/20 (`-wave3-fix` to `-fix5`). These runs found the Music screen fit and contrast defects above.
- Second full run: EditMode 885/885, PlayMode 364/365, 0 exceptions, save untouched (`-wave3-full2/`). The one failure was the same Drowning Chalice case with identical timings, so it was not random:
  - the "with" volley started 4.6 s after the first one, while a pool lives 4 s;
  - the first volley's last pool was reused in the same frame at the same distance from the hero, so the counter did not see a new shot.
  - The test now waits, with the weapon's timer held, until the earlier shots are gone. The Duplicator fixtures then passed 33/33 (`-wave3-dup/`).

### Mobile: both orientations from the same screens (owner, 2026-10-10 to 2026-10-12)

Owner decision (2026-10-10): the game plays in landscape and in portrait, from the exact same screens, which adjust themselves; no second "portrait" copy of any screen. VS wiki (Options, raw 2026-10-10) has a mobile Orientation option with Portrait as default; auto-adjusting to both is an owner exception.

Why: a phone run (realme RMX5004, 1080x2400 at 480 dpi) showed the 16:9-only UI broken in portrait and every control at 10-25 dp.

**Rules** (UI/HUD/Theme):
- One scaling rule for every canvas (`ResponsiveCanvas`, `UiScreen`), with the safe area (`SafeAreaFitter`).
- On touch screens every control is at least 48 dp (`UiScreen.MinTouchUnits`; 0 on PC) and text at least 16 sp (`MinTextUnits`, `UiTextFloor`). A tab grows to its label.
- Layouts reflow by the units available and the orientation (`AdaptiveSplit`, `GridFit`, `ResponsiveAnchors`, `ResponsiveScroll`).
- **The PC look at 1920x1080 does not change.** Every phone rule is behind the touch check.
- Tests set the screen through `UiScreen.TestPixels/TestDpi/TestTouch` (`ScreenSimulation`).

**Phone HUD** (owner, 2026-10-12; PC HUD unchanged):
- One thin strip under the XP bar: item slots left, clock in the middle (number only), "Kills: n", Gold and Pause on the right (`GameHUDController.LayoutPhoneStrip`).
- Pause is a two-bar symbol, 48 dp, under "LV" (`TouchControlsUI.MakePauseASymbol`).
- The stage name shows for 5 s under the strip, then hides (`HudReadability.PhoneStageSeconds`). The unlock notice sits under it, and the story card under that.
- Item slots hide under pause, chest and merchant, and show on level-up (`InventoryHud.HideUnderMenus`).
- Small-screen fits: a LOCKED Power Up cell shows name + "LOCKED" and keeps its condition in the info box; character card names get a taller band; the level-up heading moves up when the stat rows do not fit.
- Found on the way: the scene has two `XPBarUI`, one without a bar. `FindFirstObjectByType` returned that one, so Pause was placed over "LV" (`BarWithLevelLabel`).

**Joystick** (owner, 2026-10-12): in portrait the floating stick comes to the thumb anywhere in the lower 82% of the screen; landscape keeps the left 45% (`TouchControlsUI.PlaceZone`).

The stick's look (owner, 2026-10-12: "square ko round kar do ... joystick ka aakar ekdam chhota kar do", "transparent ka colour white kar dena"): the square pad is not drawn. In its place is a round, white, see-through plate the size the knob used to be (`TouchControlsUI.Plate`, `PlateShare` 0.4), and the knob is a small dot on it (`Dot`, `DotShare` 0.14) that travels to the plate's rim. A handle-bar version was built first and rejected. Dash and Pause are round too (owner: "dash aur pause ko bhi gol kar do"), in the touch pad's dark see-through colour so their label and symbol read (`RoundPads`). Checked in `PhoneHudStripPlayModeTests`; EditMode 891/891 and the touch fixtures 13/13 (`run-20261012-round`); seen on the phone with real swipes in portrait and landscape (`run-20261012-android8`, the APK is there). Mistake in these touch runs: `-sh-autoplay-shots 0` makes the bot take a screenshot every frame, which dropped the phone to 6 FPS and the owner saw it as lag; with `-sh-autoplay-shots 999` the same build held 30 FPS (the phone's cap).

**Slippery obstacles** (owner, 2026-10-12: the hero stopped dead on any obstacle). `ObstacleSlide.Resolve`, used by `PlayerRunState`:
- A glancing walk slides along the face at no less than 70% of the hero's speed.
- Walking straight at a small obstacle, the hero steps round the nearer open side (a face up to about 3 units to its end).
- A long wall with no open side (the Castle corridor) still stops a hero walking straight at it.
- The hero's colliders use a zero-friction material.
- The wiki does not describe how the VS player meets obstacles; the numbers are Soul Hunter's own.

Validation:
- Tests: `ResponsiveUiTests` (T); `ResponsiveScreensPlayModeTests`, `CharacterSelectResponsivePlayModeTests`, `PowerUpShopResponsivePlayModeTests`, `OptionsResponsivePlayModeTests`, `RunResultsResponsivePlayModeTests`, `HudOverlaysResponsivePlayModeTests`, `PhoneHudStripPlayModeTests` and `ObstacleSlidePlayModeTests` (R). `StageFloorPlayModeTests` now allows the hero to slide round an obstacle but never through it.
- Full run on the final code: EditMode 891/891, PlayMode 384/384, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261012-final-full3/`).
- Phone, both orientations (`run-20261012-android5`, the APK is there): four bot runs, 0 errors, no text out of its box, nothing off screen, nothing under 48 dp, no overlaps. Min FPS 30 in one pass and 21-22 in the other.
- Every HUD state was screenshotted on the phone and checked: strip, pause symbol, stage name, notice, slots hidden and shown (partial `V`).
- Joystick, real touches on the phone in portrait: at 10 spots the stick came under the finger.
- PC screenshots (`run-20261012-pc-screens`): HUD, pause and level-up look the same as before.
- **Only tests, not seen on the phone:** the hero following the stick's direction, and sliding on obstacles (the bot was playing).
- Not captured on the phone: Revive, Music, and the chest screen in portrait.
- Android scripts are in `D:\SoulHunter-Validation` (`android-build.ps1`, `phone-run.ps1`). Never run the Android build while tests run (Gradle runs out of memory).

VS looked at on the owner's laptop (2026-10-12, screen only, about 80 s; no file read): Kills and Gold are an icon and a number without words; Pause is a symbol; the pause screen has no title and shows weapons with level pips. Soul Hunter differs there; whether to follow is open for the owner.

`Docs/VS_DOT_Table.md` was re-checked against the code on 2026-10-12 (commit `06c04f9e`): of 176 rows, 129 match, 26 are partial, 7 missing, 10 differ by owner decision.

**60 FPS on phones** (owner, 2026-10-12: "60 fps kar do"): Unity holds a phone at 30 frames a second by default, so `DisplaySettingsApplier` asks for `MobileFrameRate` 60 on mobile. Measured on the phone (`run-20261012-android9`, bot, first 95 s of Level 1, 15-43 enemies, battery saver on): 58-60 FPS in portrait and in landscape; the build before it held 30. Not measured: late-game hordes (300 enemies), heat over a long session, battery use (`P` partial).

Open: the phone's FPS under a full horde is not measured (the earlier 30-capped bot passes fell to 21-22 once); touch control art; an Orientation option in Options.

**App icon** (owner, 2026-10-12: the installed app showed Unity's icon; "start ke baad unity logo ke saath jo hudi bala picture hai wahi bana do", then "icon full bright, full contrast bala banao"; passed the same day): the hooded hunter of the splash logo (`Assets/sprite_126.png`, upscaled 4x with Real-ESRGAN, `ArtSource/SH10_AppIcon/`) on a dark plate at full brightness and contrast, 512 px, built by `ArtSource/Generators/build_app_icon.py` to `Assets/_Project/Art/AppIcon/SH10_AppIcon.png`. `SoulHunterBuildRunner.ApplyAppIcon` sets it as the default icon in the Android and Windows builds. Mistake on the way: a hand-written two-line `.meta` made Unity import the picture as a Cubemap, so two builds kept Unity's icon; the meta now has full texture settings. Seen on the phone's app drawer and App info page (`run-20261012-android13`, the APK is there). The Windows build's icon was not checked.

Passed by the owner (2026-10-12: "pass, poora test chalao aur commit karo"). Full run on the final code, after the stick, round pads and 60 FPS: EditMode 891/891, PlayMode 384/384, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261012-pass-full/`).

### VS table closed: open rows, new items, unlocks, caskets, relics, map (owner, 2026-10-12 and 2026-10-13)

Owner: "sare pending pura karo rule wiko se lo nahi mile balance method apnao", then "bache 15 rows bhi naya system banao saath mein kuch addition ke sath" (the additions were left to the agent: "tum apna best do"). `Docs/VS_DOT_Table.md` now counts MATCH 149, PARTIAL 2, MISSING 0, DIFFERS-OWNER 12, BALANCE 9, UNKNOWN 2, N/A 2 (176 rows). Raw wiki pages are in the session scratchpad (`wiki/`).

The owner offered their installed copy of VS as a source. Not used: the no-copy rule in section 2 forbids extracted game data, and only the owner can change that rule. Watching the running game on screen stays allowed.

**Open-row pass (2026-10-12):**
- **Limit Break per weapon** (`LimitBreak.Profile` / `ProfileFor`, from each weapon's own wiki Limit Break table): Base Damage +0.5 (+1 for Fire Wand, Holy Wand, Heaven Sword, Death Spiral, Hellmaw Comet, Drowning Chalice, Severed Fate and Requiem of Souls), Area +2.5%, Speed +5%, Amount to 20, Pierce to 10, Duration to the weapon's own total (3, 6, 9 or 10 s). Laurel and Eclipse Sigil get no cards.
- **Stage items past the six slots** (wiki Passive_items, "Stage items"): a stage item found with no free slot becomes an extra item; a maxed one pays gold (`WeaponManager.AcquireStageItem`).
- **Hitbox delay** (`HitboxDelay`, `ProjectileDamage.HitGate`): Runetracer 0.5 s per enemy, and it now bounces off the screen edges; Severed Fate uses the same gate.
- **Frozen enemies can be pushed** by walking into them (`EnemyFrozenState.PushFromPlayer`, radius 0.4, Soul Hunter's number).
- **Map-event formations start off screen** (`EnemySpawner.EventRadius`).
- **Stalker XL:** the 1% event at 0:00, and its chests evolve items 1, 4 and 5 (`ChestTiers.EvolutionSlots`).
- **Level 1 chest-less wave bosses** (wiki Mad_Forest): Great Grave Bat at 8:00, Grave Lily at 21:00 and 24:00, no chest (`MadForestEnemies.ChestlessBosses`).
- **Character select:** the stat block shows Soul Egg bonuses, marks values that come from PowerUps (`Row.FromPowerUps`), and lists Reroll, Skip and Banish once owned (`CharacterStatSheet.SelectRows`).
- Death's Shadow appears past the screen edge.
- Validation of this pass alone: EditMode 892/892, PlayMode 387/387, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261012-vsrows-full2/`).

**New items (2026-10-13; names approved by the owner, "naam theek hain").** Each follows its own wiki page; built by `NewVsItemsBuilder` (run in the isolated copy with `vsitems-build.ps1`).

| Soul Hunter | VS | What it is |
|---|---|---|
| Miser's Idol | Stone Mask | Greed passive, found as a stage item |
| Ring of Long Hours, Ring of Dark Hours | Silver Ring, Gold Ring | Stage items; with Clock Lancet they make Sundered Hour |
| Sundered Hour | Infinite Corridor | Clock Lancet's evolution |
| Dusk Raven | Ebony Wings | Weapon; Peachone's counterpart |
| Twilight Flock | Vandalier | Union of Peachone and Dusk Raven |
| Azure Repeater | Eight The Sparrow | The second gun |
| Revenant Wheel | Phieraggi | Union of Gun and Azure Repeater, with Tirajisú |

- Peachone now circles clockwise (wiki Peachone).
- Sundered Hour's beams aim at the nearest enemy (owner aiming rule).
- Revenant Wheel numbers the wiki leaves open are balance values, marked in code.
- The Idol also lies on Levels 5 and 6 since later that day (see "Nothing left"). Art is placeholder, **not owner-approved**.

**Guardians of the hidden stage items** (wiki City_Atlantean, Volcano_Atlantean, Moon_Atlantean, Sun_Atlantean and Passive_items "Stage items", raw, 2026-10-13).
- Both Metaglios and both Rings are guarded: Warden of the Left Hand, Warden of the Right Hand, Warden of Long Hours, Warden of Dark Hours (`ItemGuardians`). The Stone Mask page names no guard, so Miser's Idol has none. **The names are not owner-approved.**
- One statbox for all four, on every level: 30,000 health (no HP x Level), speed 200, damage 50, no knockback, no XP, res-freeze 2, res-kill, res-debuff.
- The guardian appears where the item lies when the hero comes close (`ItemGuardPost`). The item can be taken without killing it, and it follows the hero until it is defeated. It is brought back to the screen like a boss.
- An item drawn away from its place (Gathering Call, VS Mad Groove) wakes its guardian only when it is taken.
- Its death leaves a Soul Egg and no gem (`ItemGuardian`, `SoulEggPickup`). Walking over the egg gives it to the character being played, as a merchant egg does, and Results lists it.
- Balance values, the wiki says only "gets close": the wake distance is 12 units, and the model is the level's elite at 1.3x with a tint. **Placeholder look, not owner-approved.**
- Built by `ItemGuardianBuilder` (menu Soul Hunter/Content/Build Item Guardians; `guardians-build.ps1` in the isolated copy): 4 EnemyData, 40 prefab variants, `GameContent.ItemGuards`, and 4 Bestiary entries.
- Tests: `ItemGuardianTests` (T) and `ItemGuardianPlayModeTests` (R).
- Full run: EditMode 915/916, PlayMode 415/416, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261013-guardians-full/`). The two failures were test-side:
  - `CollectionBookTests` counted the Bestiary without the four guardians (282 expected, 286 listed). It now adds them.
  - `DuplicatorEverywherePlayModeTests` Bloody Tear counted 3 lashes of 4. The fourth lash reuses the first one's pooled object on the opposite side, exactly as far from the holder, and the shot counter only saw a reused object that jumped closer. It now also counts one that is no further out.
- After both fixes: EditMode 916/916 and the two Duplicator fixtures 33/33, 0 exceptions, save untouched (`-guardians-fix2/`). No single full run passed every test.
- Not seen by eye: no build play or screenshots of a guardian (`V` unverified). Not yet played by the owner.

**Item unlocks, Soul Caskets and relics.**
- `ItemUnlocks` / `ItemUnlockTracker`: weapons and passives unlock by their VS conditions in Soul Hunter terms (Fire Wand 20 Soul Lanterns, Garlic 5 Roast Chickens, Lightning Ring 5,000 kills, Peachone 10 minutes survived ...). Nine items are open from the start. A save from before this change keeps every item (`ItemUnlock.Initialized`). `ItemUnlocks.UnlockEverything` is for tests and the bot.
- PowerUps: ~~Omni unlocks by holding six evolved weapons at once~~ (since later that day by the Cursed Coffer at level 9, as VS's Torrona's Box; see "Nothing left"); Charm, Defang and Preserve by relics.
- `SoulCasket` (VS coffins): one on each of Levels 1-7, guarded by 8 elites, freeing Corvan, Draven, Gideon, Kaelen, Isolde, Seraphina and Lumina. The other characters keep their conditions.
- `RelicPickup`: 11 relics with lasting boons and Soul Hunter names (Hordecaller's Bell, Reaper's Ledger, Beast Codex, Candle Idol, Hourglass Tear, Fate Deck, Oathbound Psalm, Fangless Vial, Bard's Echo, Mirror Shard, Last Horn).
- The unused `AchievementManager` and `AchievementData` are deleted.
- Two unlocks in the same second (at 15:00, a Skip rank and an item) used to leave only the last notice. The newest still shows at once, and a notice it replaced within its first 1.5 s follows it (`CharacterUnlockTracker`).

**Menus and save** (both UI skills used).
- Pause **Map** (`PauseMapScreen`, `MapMarkers`): stage items, caskets, relics and the merchant; optional guide arrows (`MapGuides`, off by default).
- **Quick Start** (locked until Level 5 is cleared) and a start prompt (`StartPrompt`). The main menu now has nine buttons in three columns. **Layout not owner-approved.**
- Options, **Save Data** tab (main menu only): Data Recovery, and Delete Save after six tab presses and two confirmations (`SaveService` keeps `.deleted.bak` / `.replaced.bak`).

**Spawner.**
- Chest-less wave bosses on Levels 3-6 from their source stages (`ChestlessWaveBosses`).
  - Level 5's 24:00 boss first used a stand-in type (100 x Level, no resistances). It now has its own type, Great Bog Mother, from the wiki's Maronna_Meea "Maronna Meea Boss" statbox: 200 x Level health, speed 180, damage 20, no knockback, res-freeze 1.1, res-kill, res-debuff. No minute's wave lists it, so it is a boss-only type (`StageEnemyTable.BossOnly`, `WaveData.BossTypes`). Tests: `ChestlessBossAndOmenTests`, `StageEnemyTypeTests` (T) and `ChestlessBossAndOmenPlayModeTests.LevelFive_SendsItsOwnBossType_At24Minutes_WithItsStatbox` (R). EditMode 913/913, PlayMode 412/412, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261013-l5boss-full/`).
- Four more Omens (Stampede, Dread Circles, Trampling Ranks, Crossfire Rows; 19 in the pool), and the wheel also picks at 29:30. The last seven followed the same day (see "Nothing left").
- Inverse music: Level n plays Level 11-n's track (`MusicSelection.StageTrackLevel`); Soul Hunter has no alternate recordings.

**Story layer "He was not alone" (owner decision, 2026-10-13: "haan ye twist theek hai", "haan lines theek hai, game mein jod do").** `Docs/Game_Design_Document.md` is not edited; the layer adds to the story without changing a sentence of it.
- The other twelve heroes are earlier hunters who made the same pact; the Death God who sealed them in Soul Caskets was Kael. The relics are his daughter's keepsakes.
- Every line is in `StoryTwist` (Data/Config): the first and last casket lines, one line per hunter, one keepsake line per relic (11, approved in two batches), and one ending line shown under `CampaignStory.FinalLine` once every relic is found.
- `StoryCardUI` shows a card when a casket opens or a relic is found; `RunResultsScreen` adds the ending line.
- Tests: `StoryTwistTests` (T).

**Tests added:** `NewVsItemsTests`, `ItemUnlockTests`, `SaveDataRecoveryTests`, `ChestlessBossAndOmenTests`, `StoryTwistTests` (T); `NewVsItemsPlayModeTests`, `ItemUnlockPlayModeTests`, `PauseMapPlayModeTests`, `SaveDataOptionsPlayModeTests`, `StartPromptAndQuickStartPlayModeTests`, `ChestlessBossAndOmenPlayModeTests`, `VsParityWave4PlayModeTests` (R).

**Validation:**
- First full run: EditMode 911/912, PlayMode 410/411, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261013-vs15-full1/`). The two failures: the catalog names Elaria "Elaria (Mage)", so she had no hunter line; and the notice defect above.
- Second full run, after both fixes: EditMode 912/912, PlayMode 410/411, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261013-vs15-full2/`). The one failure was `DuplicatorPlayModeTests` Peachone, a test-side margin: its "fired together" window was 0.02 s, and two bombs (0.025 s apart, 0.065 units along the orbit) landed in frames 0.019 s apart. The window is now 0.01 s (the same frame); the first full run had passed it.
- The Duplicator fixtures then passed 33/33 with EditMode 912/912 (`-vs15-dup/`). No single full run passed every test.
- Not seen by eye: no build screenshots of the new screens were taken (`V` unverified).

Not yet played by the owner.

### Nothing left: the last Omens, the Cursed Coffer, the guardian rule, Level 9 (owner, 2026-10-13)

The agent played every level for 3 minutes in a build first (`D:/SoulHunter-Validation/run-20261013-allstages/`, script `allstages-play.ps1`: no god mode, hero at a believable level per stage, 0 errors, save untouched). Eight levels were survived; the bot died on Level 4 at 1:23 (pinned against the corridor wall) and on Level 9 after 16 s (the arena defect below). Owner, after that: "kuch bhi nahi bachna chahiye agar kahani ko tora gumana parta hai to use bhi ghuma sakte ho per kahani storg hona chahiye". `Docs/VS_DOT_Table.md` now counts MATCH 151, PARTIAL 0, MISSING 0, DIFFERS-OWNER 12, BALANCE 11, UNKNOWN 0, N/A 2 (176 rows).

**Owner decision: a level's guardian opens the next level** (2026-10-13: "lv 1 mein kuch achive kare tabhi lv 2 open ho sake athwa nahi"; chosen by AskUserQuestion: defeat the stage boss; otherwise the run ends; for this run only).
- On Levels 1 to 9 the next level starts at 30:00 only if this level's boss (it comes at 25:00; at 20:00 on Levels 5 and 6 since later that day) has been defeated. If it still stands at 30:00 the run ends there: the Results screen shows GAME OVER and "The guardian of Level n still stood at 30:00. Level n+1 stays closed." No stage-clear gold is paid (`LevelProgressionManager.StageFailed`, `OnStageFailed`, `RunResults.EndReason`).
- When the boss comes, the notice shows "Defeat the guardian before 30:00 to open Level n+1." (`EnemySpawner.SpawnBoss`).
- Nothing is kept on the save: every run starts at Level 1 and earns each level again. Level 10 is unchanged.
- This replaces "30:00 starts the next stage whatever happened to the boss" (2026-09-27). VS unlocks stages by hero level in the stage before (wiki Stages: 20, 40, 60, 80); the owner chose the boss.
- **Five minutes are not always enough** (build play below, god mode, a strong hero): the boss fell in 21 s on Level 1, 50 s on Level 3, 105 s on Level 7 and 99 s on Level 9, but the Level 5 boss (90,000 health, doubled by a level-9 Cursed Coffer's Curse) still stood at 30:00 and the run ended. Whether that is wanted is open for the owner.

**Smoke** (owner: "kala dhuan bahut adhik dikh raha tha"). Peachone and Dusk Raven drop up to 11 blasts a second on one spot, each with 8 dark smoke puffs, which piled into a black cloud over the hero. A weapon blast now has no smoke; a one-off explosion keeps 3 puffs at 0.3 alpha (`WeaponVfxBuilder.ThinSmoke`). Full run: EditMode 917/917, PlayMode 416/416 (`run-20261013-smoke-full`), commit `2e17745e`.

**The last seven Omens** (raw Random_Events, Candybox, The_Drowner, Dragon_Swarm, Flower_Wall_(event), fetched 2026-10-13; own names, **not owner-approved**). Every VS Random Event now has its Omen (27 in the pool):

| Omen | VS event | What it does |
|---|---|---|
| Armoury Cache | Candybox | A box near the hero; walking over it opens the list of base weapons this save has used and the hero does not hold; the chosen one joins at level 1, past the six slots like a stage item (`WeaponCachePickup`). The list is the Arcana choice screen in a weapon form (no random, Reroll or Skip). |
| Serpent Streams | Shrimp Swarm | Three files of 12 cross sideways, 4 s apart, weaving (`EnemyFlyState`'s weave, `EnemySpawner.SpawnFliers`). |
| Powder Ring | Roulette | 12 still enemies on the Pile Assault's ellipse; one arms when the hero is within 2 units and explodes 2 s later for 50 in 2.5 units (`MineSentry`; the numbers are the self-destruct rule the game already had). |
| Rootbound | Summon II Molise | For 2 minutes every ordinary enemy is swapped for Level 1's Grave Lily or Thorn Wall, standing still and leaving the smallest gem (`EnemySpawner.BeginWaveTakeover`, `StillEnemy`). |
| Nightfall | Summon Night | For 1 minute every ordinary enemy is swapped for Level 1's three bats. |
| Rising Tide | The Drowner | The Tide Caller (the level's boss model, blue, 800 x Level health) stands at the screen's bottom-left; water rises from the screen's bottom edge for 1 minute and deals 20 per 0.5 s to a hero standing in it (`RisingTide`). A frozen caller holds the water; defeating it ends the flood. |
| Thorn Circle | Flower Wall | 30 Thorn Walls in a ring that shrinks from 9 to 3 units over 50 s. Each living member carries a solid post that stops the hero; killing a member opens the ring there (`ThornCircle`). |

- Bosses, chest carriers, map-event fliers, hazard members and guardians are never swapped by a takeover.
- **Two things that touch earlier owner decisions** (done under "nothing left"; reported to the owner, not yet answered):
  - the Thorn Circle is the one place where enemy bodies stop the hero (the crowd rule of 2026-09-26 stays everywhere else);
  - as an Omen, Death's Shadow now comes on every level; its own timetable stays on Levels 5 and 9.
- Balance values (the wiki gives none): every count, radius and gap above, the tide's speed (one screen height over the minute) and its 0.5 s tick.
- Small differences: the ring does not hold other enemies in; the Tide Caller dies to any instant kill (VS: not to Pentagram); the Armoury Cache has no gift and exists only as an Omen. Looks are placeholders (the chest model for the cache, a flat blue sheet for the water).
- `OmenContentBuilder` (menu Soul Hunter/Content/Build Omen Content) names Level 1's bat and plant prefabs in `GameContent.OmenBats`, `OmenPlants` and `OmenThornWall`. `AutoplayBot -sh-autoplay-omen <name>` (and `-sh-autoplay-omen-after S`) fires one Omen and takes screenshots.

**Cursed Coffer** (wiki Torrona's_Box and Omni; own name, **not owner-approved**; icon is the Skull O'Maniac's): a passive of 9 levels, rarity 40, +4% Might, projectile Speed, Duration and Area at level 1, +3% at levels 2 to 8 (+25%), and +100% Curse at level 9. It unlocks by holding six evolved weapons at once, and the Omni PowerUp now unlocks by getting it to level 9, both as in VS. From 2026-10-12 until now Omni unlocked by the six evolutions directly; a save that earned it keeps it.

**Banish ranks.** The Collection has 105 entries, so all five ranks (60 to 100 entries) can be earned; the table's note that they could not was out of date. `LastOmensTests.TheCollection_IsLargeEnough_ForEveryBanishRank` (T).

**Miser's Idol on Levels 5 and 6** (wiki Abyss_Foscari: "20%, 20%, 20% chance for more Stone Mask to spawn. West of the starting area"; Mt.Moonspell: "northeast of the starting area"): 100 units west on Level 5, with three 20% rolls for one more each (10 units further each), and 100 units northeast on Level 6. The distance is a balance value (the Inlaid Library's five tiles).

**Level 9, the Closing Arena** (GDD: "The safe zone continuously shrinks. The player must survive in extremely tight spaces."). Defects found by the bot run and fixed:
- the zone was centred on the world's origin, not on the hero; it is now centred where the hero stands when the level starts;
- nothing showed it; a red ring is drawn at its edge;
- it was a wall that also hurt: the hero was put back on the edge every frame and took at least 1 damage per frame (60 a second at 60 FPS for a setting of 10), which killed the bot in 1.6 s. It is now a zone, as the story says: the hero may leave it (the level's stage items lie 45 to 100 units out) and loses 10 health a second outside, in ticks of 5 every 0.5 s (`CampaignSignatureSystem.EnforceContractingArena`).
- Seen, not changed: Level 7's Soul Tide (3 damage every 15 s) and Level 10's pulse (5 every 20 s) hurt the hero with no warning on screen.

**Screen wipes stalled the game** (found by the lag check the owner asked for, 2026-10-13). In build play a level-8 Pentagram erased about 300 enemies in one frame and the game stood still for 650-800 ms, every 60 s. Each death started its soul puff, neck spray, sinking body and damage number in that one frame.
- Only the first 32 deaths of a frame get their look (`EnemyDrop.ShownDeathsPerFrame`, `CountShownDeath`; 32 is the corpse cap), and only the first 32 instant-kill numbers are shown (`HealthController.InstantKillNumbersPerFrame`). Every death still counts as a kill and drops as before.
- `EnemyCorpsePlayModeTests.DeathsInOneFrame_ShowOnlyTheFirstThirtyTwo_AndTheNextFrameShowsAgain` (R).
- Measured in a build afterwards: that change alone was not enough (266-733 ms per wipe). The rest of the cause and the final numbers are in "CTO check, first pass" below. `AutoplayBot -sh-autoplay-give "Pentagram:8"` (and `-sh-autoplay-give-after S`) gives items at set levels for such a measurement.
- Other lag numbers from the same build runs: normal play held a median of 60 FPS over 23 runs (V-Sync on); Level 7 late fell to a median of 43 and a minimum of 27; a chest opening hitched 333 ms once. About 9-10 GCs a minute with no hitch tied to one; the heap grew about 1 MB a minute over 6 minutes (10 to 17 MB).

**Tests:** `LastOmensTests` (T) and `LastOmensPlayModeTests` (R: each of the seven Omens in the real game, the Shadow as an Omen on Level 1, the guardian rule with its Results line and its notice, and the Closing Arena). Updated: `RandomizerModeTests`, `ItemUnlockTests`, `NewVsItemsTests` (the Coffer's levels, the Idol's places), `StageItemTests`, `BossStageAdvanceTests`, `WeaponVfxTests`, `ChestlessBossAndOmenPlayModeTests`, `RandomizersPlayModeTests`.

**Validation:**
- First full run: EditMode 927/927, PlayMode 419/425, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261013-lastrows-full1/`). The six failures were test-side (older tests that expected the old rules) and were fixed; the fixtures then passed 34/34 and 11/11 (`-lastrows-fix1/`, `-fix2/`).
- Second full run: EditMode 927/927, PlayMode 426/427, 0 exceptions, save untouched (`-lastrows-full2/`). The one failure, `RunModesPlayModeTests.ArcanaMode_StartsWithAnArcana_AndAnArcanaChestGivesAnother`, was test-side: the run had drawn Soul Barrage, which adds two items to an Arcana chest, so the chest screen rightly followed the card choice and held the pause. The test now allows that.
- After that fix, the screen-wipe change and the bot option: EditMode 927/927 and nine fixtures 29/29, 0 exceptions, save untouched (`-lastrows-fix3/`). No single full run passed every test, and no full run covers the screen-wipe change.
- Build play (`D:/SoulHunter-Validation/run-20261013-lastrows-play/`, script `lastrows-play.ps1`, god mode): 0 errors, save untouched. Screenshots show the Level 9 ring, the Rising Tide's water, the Thorn Circle, Rootbound's plants and Nightfall's bats (partial `V`). The Armoury Cache's box was not caught in a screenshot; only its test confirms it is drawn.
- `Docs/CTO_Check_MIS.md` (owner, 2026-10-13: a CTO list of everything to check from the Start button to the end of a run) has 232 checks in 26 sections; 98 are still to be done.

Not yet played by the owner.

### CTO check, first pass: coin, screen wipes, memory, 30-minute runs (owner, 2026-10-13)

Owner: "as a CTO ... MIS banao" (a list of everything to check from the Start button to the end of a run: UI, sound, VFX, lag, memory), then "shuru karo" and "kaam jaari rakho". The list is `Docs/CTO_Check_MIS.md`: 232 checks, each with its status and evidence. After three batches: 128 OK, 16 with a problem, 88 not checked yet (sound by ear, real devices, release items and balance mostly need the owner or hardware).

**Coin** (owner: "coin kisi stiker ke jaisa lag raha hai use asli gold coin jaisa banao pahle tum khud check karo").
- Found with a render of the coin as the game camera sees it (`CoinLookPreview`, menu Soul Hunter/Content/Render Coin Looks): the coin lay face up and turned like a record, so it was always the same flat disc, and a 0.38 glow of its own colour removed all shading.
- Now (`PickupV2Importer.BuildCoin`, `CoinGlow`; menu Soul Hunter/Content/Rebuild Soul Coin): it stands on its edge and turns about the world's up axis (face, reeded edge, other face); drawn 1.25x thicker (`CoinThickness`); metallic 0.45, smoothness 0.7, glow 0.2 of its colour, so the sun shades it and flashes on it.
- Before and after: `D:/SoulHunter-Validation/run-20261013-coin/coin-before.png` (its close-up half is white, a first-render fault of the tool; the small half shows the old look) and `coin-after2.png`. Checked by the agent in those renders only; **not seen in play and not owner-approved**.
- `PickupV2Tests.TheCoin_StandsOnItsEdge_AndItsMetalIsShadedNotGlowing` (T). Script `D:\SoulHunter-Validation\coin-look.ps1`.

**Screen wipes, the real cause.** After the 32-deaths-per-frame change a build still stalled 266-483 ms per Pentagram wipe (300-733 ms in a 30-minute run). `CharacterUnlockTracker` ran its whole unlock check (every character and PowerUp condition) on every single kill, 300 times in a wipe's frame, and on every kill in normal play too. A kill now only marks the check as due and `Update` runs it once.
- Measured (`D:/SoulHunter-Validation/run-20261013-mis2/pentagram`, level-8 Pentagram, Level 1 from 12:00): four wipes of 92-164 enemies, each in a 17 ms frame; the enemy pass itself took 5-7 ms. `PentagramWeapon.LastEnemyWipeMs` / `LastItemWipeMs` and the bot's `wipe` event record this.
- A profiler capture of the old stall was made (`run-20261013-pentprof/capture.raw`, 3.5 GB) but summarising it was stopped when the laptop ran low on memory; it was not needed in the end.

**Memory.** The first 30-minute build run (`run-20261013-mis/long30`) grew from 432 to 743 MB of process memory. Suspected cause: pooled bodies of enemy types that no longer spawn were never freed (a run has about 250 types over ten levels).
- `EnemySpawner.TrimIdlePools`: a pool no spawn has used for 90 s (`PoolIdleSeconds`) is emptied, 4 bodies per frame (`PoolTrimPerFrame`). `PooledEnemies` and Unity's allocated memory are in the bot's timeline (`pooled`, `unityMb`).
- `EnemyMinimumPlayModeTests.PooledBodies_OfATypeThatNoLongerSpawns_AreFreed_AFewPerFrame` (R).
- Second 30-minute run (`run-20261013-mis2/long30`): never more than 398 pooled bodies; process memory 434 to 535 MB (+101). **Not a clean comparison:** that run's hero stayed at level 8 with 3,829 kills, against level 37 and 12,274 kills in the first. Levels 2-10 and a whole run are not measured (`P` partial).

**The two 30-minute Level 1 runs** (release build, reference laptop, 1366x768, V-Sync on, god mode, 0 errors, save untouched):

| | First (`run-20261013-mis`) | Second, after the fixes (`-mis2`) |
|---|---|---|
| FPS median / 5th percentile / min | 60 / 52 / 45 | 60 / 60 / 39 |
| Frames over 250 ms | 15, of 300-733 ms (most at a wipe) | 1 (the window lost focus and the game paused, as it should) |
| Managed heap | 8.4 to 20.9 MB | 8.3 to 12.9 MB |
| GC runs | 259 in 31 min, none tied to a hitch | not counted |
| Boss | 4,650 health at level 31, killed in 90 s | 1,200 at level 8, killed in 184 s |
| End | Level 2 opened | Level 2 opened |

- The bot's loadouts differ run to run (level 37 against level 8), so the second run is a lighter load.
- Launch to the first seconds of play took 2-27 s in four launches (a cold start is the slow one); launch to the main menu was not timed.
- The Windows build is 205.8 MB (142 MB on 2026-10-02).

**Also in this pass:**
- The guardian rule is told five minutes early: at 20:00 the notice shows "The guardian of Level n comes at 25:00. Defeat it before 30:00 to open Level n+1." (`LevelProgressionManager.GuardianWarning`, `GuardianWarningLeadSeconds`; `LastOmensPlayModeTests.TheGuardianRule_IsTold_FiveMinutesBeforeItComes`, R).
- Ten unused fields (old shop prices, old starting charges, two old magnet radii, a spawner flag) and three leftover logs were removed; the project's own code now compiles without warnings.
- Mistake: `-sh-autoplay-give` at 12 s silently gave nothing once the bot's level-ups had filled the six weapon slots (one whole run measured no Pentagram). The measurement runs now give at 0.5 s.

**Second batch by bot** (`D:/SoulHunter-Validation/run-20261013-mis3/`, script `mis3-run.ps1`; release build of `73470b6e`, god mode, 0 errors, save untouched):
- The coin in play (Gilded Fever on Level 1): coins show at different turns, face and edge; they are small from the game camera. Still not owner-approved.
- Level 7 late again (20:00, level-110 hero, up to 321 enemies, 6 min): FPS median 57, 5th percentile 51, min 36 (it was 43 / 27).
- Boss fights from 24:50 with a strong hero:

  | Level | Hero level | Boss health | Result | FPS median / min |
  |---|---|---|---|---|
  | 2 | 50 | 12,500 | killed in 58 s, Level 3 opened | 43 / 38 |
  | 4 | 80 | 40,000 | killed in 114 s, Level 5 opened | 54 / 44 |
  | 6 | 100 | 75,000 | **not killed by 30:00; the run ended**, "Level 7 stays closed" | 52.5 / 34 |
  | 8 | 120 | 120,000 | killed in 219 s, Level 9 opened | 49 / 25 |

  With the earlier Level 5 run, two of nine levels ended a god-mode run of a strong hero. The bot flees bosses, so a player may do better; whether five minutes stay is the owner's decision. Level 8's minimum of 25 FPS is under the 30 FPS hard minimum (one 5 s sample).
- Memory in those 6.5-minute runs: process memory grew 30-90 MB each (for example 538 to 628), 567-792 pooled bodies at most, Unity's own memory 124 to 164-182 MB. A whole ten-level run is not measured.

**Third batch: guardian time, gold coin, 60 FPS (owner, 2026-10-13).**
- **Guardians of Levels 5 and 6 come at 20:00** (owner: "boss ko pehle bulao"). `CampaignLevelCatalog.EarlyGuardianSeconds` 1200; the other levels keep 25:00. The warning comes five minutes before, at 15:00. Measured (`run-20261013-mis4`, god mode, clock skipped to 19:50): Level 5's boss (45,000 health, level-90 hero) fell in 166 s and Level 6's (50,000, level 100) in 61 s. `BossStageAdvanceTests.TheGuardiansOfLevelsFiveAndSix_ComeAtTwentyMinutes_TheOthersAtTwentyFive` (T).
  - The fight's length depends on the bot's random loadout: in `run-20261013-mis6/ab-l2` a level-50 hero with a weak draw (781 kills in 5 minutes) did not kill Level 2's boss (12,500) by 30:00 and the run ended, where the earlier run killed it in 58 s. So a weak loadout can still fail any level's five minutes.
- **Coin** (owner: "coin bada karo", then "coin abhi bhi bekar hai ... gold nahi lagta, stiker dikh raha hai"). Every coin is now the gold coin in one bright gold (`PickupV2Importer.CoinGold`); its worth shows by size (`CoinTierSizes` 0.85 / 1 / 1.25 of `CoinSize` 1.15, was 0.75); 1.8x thick; metallic 0.55, smoothness 0.82, glow 0.25; the flat glow disc under it is gone. Bronze and silver coins are no longer shown.
  - Renders `D:/SoulHunter-Validation/coin-gold-render.png` and, in play, `coin-gold-in-play.png` (`run-20261013-mis5/coin`). The agent's view: gold and 3D, but no strong glint, and the cyan soul flame remains. **Not owner-approved.**
  - Defect this caused, found by the full run: the chest screen's coin burst looked the coin model up by its old name `Tier_Gold` and threw no coins (`ChestFlowPlayModeTests` timed out). `ChestPreviewStage.CoinVisual` now takes the biggest tier; `PickupV2Tests.EveryCoin_IsGold_GrowsWithItsWorth_AndHasNoFlatGlowUnderIt` (T) guards the lookup.
- **60 FPS everywhere** (owner: "pure game ko 60 FPS par chalao"). `DisplaySettingsApplier.TargetFrameRate` 60 on every platform (it was phones only). With V-Sync on, a faster display skips refreshes (`VSyncCountFor`: 120 Hz gives 60, 240 Hz gives 60, 144 Hz gives 72). Measuring tools set `DisplaySettingsApplier.Uncapped`. `AccessibilityTests` (T).
  - This is a cap, not a speed-up. Measured in a build without the cap (`run-20261013-mis6`, script `mis6-run.ps1`, reference laptop, 1366x768, god mode, 0 errors, save untouched), switching Ultra and Medium every 30 s in the same run (`AutoplayBot -sh-autoplay-quality-ab "5,2"`, timeline field `quality`):

    | Run | Ultra: median / 5th percentile / min | Medium |
    |---|---|---|
    | Level 2 from 24:50, level-50 hero, about 310 enemies | 72 / 62 / 60 | 72 / 62 / 58 |
    | Level 8 from 24:50, level-120 hero, 11,616 kills in 6 min, about 270 enemies | 56 / 48 / 42 | 58 / 50 / 47 |

    These are 5 s averages; the Level 8 run's worst single reading was 29. **Quality level makes no real difference** (within noise), so the cost is in game code under heavy killing, not in rendering. Level 8 with a level-120 hero did not hold 60 (profiled and fixed below).
  - With the cap on (Level 1, 2 minutes): 60 / 60 / 60.
  - Not measured: V-Sync off with the cap, a display faster than 60 Hz, the phone.
- **The stored quality level.** A player build keeps its quality level between launches (registry), and `PerformanceBenchmark` and the A/B switch set it. After the A/B it read Medium on this laptop; what it was before is not known, so the earlier line "every bot run above was on that default (Ultra)" is not proven. The bot now puts back the level it found. A fresh install still starts on Ultra (owner decision open).
- **60 FPS under heavy killing: profiled and fixed** (owner: "profiler chalao aur 60 fps fix karo").
  - Tool: `AutoplayBot -sh-autoplay-profile <file>` writes the Unity profiler for 6 s only (`-sh-autoplay-profile-at S`, `-sh-autoplay-profile-seconds N`), and with `-sh-autoplay-profile-below FPS` only once two seconds in a row were that slow. Captures are 240-250 MB and are deleted after `ProfileSummary` (script `D:\SoulHunter-Validation\fps-profile.ps1`, development build).
  - Found (`run-20261013-fpsprof/l8slow.summary.txt`, Level 8 from 24:50, level-120 hero, 259-277 enemies packed on screen, 47-50 FPS): an average frame of 23.3 ms. Physics 4.4 ms: enemy bodies made trigger pairs with each other (171 `TouchDamage.OnTriggerEnter` calls a frame that did nothing). `CharacterUnlockTracker.Update` 1.3 ms: the whole unlock check ran in every frame that had a kill. The rest: rendering 3.5 ms, animation and skinning about 2 ms, `EnemyController.FixedUpdate` 2 ms, and the bot itself about 1.5-2 ms (measuring cost a player does not pay). A fast window (116 enemies on screen, 84 FPS) took 11.9 ms.
  - Fix 1: the Enemy layer no longer collides with itself (`ProjectSettings/DynamicsManager.asset`). Nothing needed those pairs: the crowd push is computed in `EnemyCrowd`, and rail carts, Soul Lanterns and weapons meet enemies through other layers or queries. `StageFloorTests.EnemyBodies_MakeNoPhysicsPairsWithEachOther` (T).
  - Fix 2: a kill runs the unlock check at most every 0.25 s (`CharacterUnlockTracker.KillCheckSeconds`); the check on each stage second stays.
  - Measured after (`run-20261013-mis7`, script `mis7-run.ps1`, reference laptop, 1366x768, god mode, 0 errors, save untouched; 5 s averages after the first 20 s):

    | Run | Kills | FPS median / 5th percentile / min | Samples under 60 |
    |---|---|---|---|
    | Before: Level 8, release, no cap (`-mis6`) | 11,616 | 56-58 / 48-50 / 42-47 | most |
    | Level 8, release, no cap, run a | 2,596 | 68 / 64 / 62 | 0 of 69 |
    | Level 8, release, no cap, run b | 4,139 | 85 / 65 / 63 | 0 of 69 |
    | Level 8, development build, no cap | 10,640 | 70 / 58 / 56 | 10 of 68 |
    | Level 8, release, 60 cap | 9,660 | 60 / 59 / 56 | 7 of 45 (56-59) |
    | Level 7 from 20:00, level-110 hero, release, no cap | 222 | 85 / 79 / 79 | 0 of 44 |

    The two no-cap release runs drew weak loadouts (few kills), so the development-build run and the capped run are the ones that compare with "before". **60 is now held most of the time on Level 8's heaviest fight; some 5 s windows still read 56-59.** No two seconds in a row fell under 52, so the slow-window capture did not start and there is no "after" profile. The summaries' `minFps` (21-30) is the single worst reading of a run, loading included.
  - Next costs if 60 must hold in every window: animation and skinning of on-screen enemies, `EnemyController.FixedUpdate`, per-enemy `TouchDamage.Update`. Done on 2026-10-14, below.
  - Not measured: without the bot, V-Sync off with the cap, a display faster than 60 Hz, the phone, the 8 GB reference.
- **60 FPS, second pass** (owner, 2026-10-14: "har hisse mein pakka 60 chahiye"). Profile of the code before it (`run-20261013-mis7/prof/l8now.summary.txt`, development build, 298 enemies): 15.7 ms a frame, spread over per-enemy script calls, physics, animation, damage numbers and the bot.
  - **One runner steps the horde** (`EnemyRunner`, created by `EnemyController.OnEnable`): `EnemyController.StepAll` calls each enemy's `FrameStep` / `PhysicsStep`; about 600 engine-to-script calls a frame are now 2. The step reads the target's position, the enemy's transform and the time once (`TryGetTargetPosition`, `CachedTransform`, `EnemyController.Now`), and a flat body's vertical velocity and locked rotation are not read from the Rigidbody.
  - **Crowd pushes every second rendered frame** (`EnemyCrowd.FramesPerRebuild` 2, `EnsureBuilt`); body radii are read for new spawns and every 16th rebuild.
  - **Contact damage is ticked only while touching** (`TouchDamageTicker`; `TouchDamage.Update` is gone). A switched-off `TouchDamage` keeps no contact.
  - **Damage numbers fade in 10 steps** (`DamagePopup.FadeSteps`): writing the colour every frame rebuilt about 25 texts a frame (0.6 ms).
  - `EntityAnimator.UpdateSpeed` skips a change under 0.01; `PickupIdleMotion` sets position and rotation in one call.
  - **The measuring tool's own cost, stated plainly:** `AutoplayBot` read the command line about ten times a frame and searched the whole scene every frame; it now caches the arguments and looks every 0.25 s. This makes bot runs faster without making the game faster for a player.
  - Tests: `HordeStepPlayModeTests` (R: one runner, an enemy chases and faces the hero, a switched-off enemy stands still, only a touching enemy is ticked, a switched-off touch deals nothing, a reused body hurts again).
  - Measured (`D:/SoulHunter-Validation/run-20261014-fps60`, script `mis8-run.ps1`, release build, reference laptop, 1366x768, god mode, Level 8 from 24:50 with a level-120 hero, 6 minutes, 0 errors, save untouched; 5 s averages after the first 20 s):

    | Run | Kills | Enemies avg / max | FPS median / 5th percentile / min | Samples under 60 |
    |---|---|---|---|---|
    | No cap, 1 | 1,882 | 306 / 388 | 88 / 69 / 65 | 0 of 62 |
    | No cap, 2 | 6,027 | 225 / 321 | 73 / 60 / 56 | 2 of 69 |
    | No cap, 3 | 10,960 | 236 / 336 | 70 / 61 / 58 | 1 of 69 |
    | 60 cap, 1 | 20,359 | 286 / 321 | **49 / 37 / 31** | 66 of 69 |
    | 60 cap, 2 | 5,451 | 226 / 337 | 60 / 59 / 57 | 11 of 69 (3 under 59) |
    | Level 7 from 20:00, level 110, no cap | 3,627 | 135 / 300 | 136 / 93 / 73 | 0 of 45 |

  - **Not reached: 60 in every window.** Without the cap the game now runs at a median of 70-88 where it was 56-58 two days before, but single windows still read 56-58 with 300 or more enemies packed on screen. With the cap and V-Sync on, a frame that takes a little over 16.7 ms waits for the next refresh and counts as 33 ms, so a fight that averages 62-70 without the cap shows 45-57 with it. "60 cap, 1" ran at 300 enemies almost the whole time and read 45-47 from its first seconds, before its kill rate rose, so the cause is the frame time at 300 enemies, not the killing.
  - Profile after the pass (`run-20261014-fps60/heavy2.summary.txt`, development build, about 290 enemies, 72-84 FPS): 13.9 ms a frame. No single cost stands out any more: rendering 3.8 ms, all scripts' `Update` 2.7 (the bot 0.6 of it), physics 1.4, animation 1.7, `EnemyRunner.FixedUpdate` 1.0, skinning 0.7.
  - What could still raise it, none done: IL2CPP for the Windows build (the module is not installed in this Unity; the owner installs it through the Hub); V-Sync off with the 60 cap (no halving to 30, but tearing is possible); baked or instanced horde animation (large art-pipeline work); holding map events to the 300 cap (they pushed the horde to 388).
  - `fps-profile.ps1 -Extra` passes bot options (for example `-sh-autoplay-give`) to a profile run. A run given that heavy loadout (Bible, Azure Repeater, Santa Water, Gun, Peachone at 8, Duplicator 2, Skull O'Maniac 5, Cursed Coffer 9) held 65-99 without the cap in the development build (`heavy`).
  - Validation: full run EditMode 931/931, PlayMode 431/432, 0 exceptions, save untouched (`run-20261014-fps60/full`). The one failure, `EnemyMinimumPlayModeTests.PooledBodies_OfATypeThatNoLongerSpawns_AreFreed_AFewPerFrame`, waited 0.3 s of game time; the cause was not proven (a level-up holding game time is the likely one). The test now waits on real time. Its fixture, `HordeStepPlayModeTests` and `EnemyCrowdPlayModeTests` then passed 5/5 (`-fps60/t2`); that run's EditMode failed the known `EnemyCrowdTests` timing check once (1.87 ms against 1.5) and passed 931/931 on the next run (`-fps60/t3`). No single full run passed every test.
- Validation of the profile fixes: one full run passed every test: EditMode 931/931, PlayMode 430/430, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261013-mis7/full/`). It also covers the 60 FPS cap.
- Validation before that: full run before the 60 FPS change: EditMode 930/930, PlayMode 429/430 (the chest coin defect above), 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261013-mis5-full/`). After the fix and the 60 FPS change: EditMode 930/930 and the chest, accessibility and bot fixtures 17/17 (`run-20261013-mis6/tests`), then with the bot putting the quality level back 7/7 (`-mis6/tests2`). No full run covers the 60 FPS change.

**Still open from the list** (all in `Docs/CTO_Check_MIS.md`): whether a weak loadout may fail a level's guardian; FPS with 300 or more enemies on screen (no cap: median 70-88, single windows 56-58; with V-Sync and the cap such a fight reads 45-57); the 50-damage item guardians; Level 4's wall; Level 8's floor seams; one model per level in many tints; 6 missing union references in `Main_Gameplay`; nobody has listened to the game.

**Validation:** one full run on the final code passed every test: EditMode 928/928, PlayMode 430/430, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261013-mis2-full/`). The build runs above are the play evidence (partial `P`, partial `V`).

Not yet played by the owner.

### 60 FPS, third pass: every level (owner, 2026-10-15: "full optimisation chalo, mera goal achieve karo")

Goal: 60 FPS in every part of every level on the reference laptop. Measured with `D:\SoulHunter-Validation\frametime-suite.ps1`: a release build, V-Sync and the frame cap off, 1366x768, god mode, no screenshots. Each level's late fight plays for 3 minutes, plus a Shadow Kael fight, a heavy-kill loadout and a Pentagram wipe run. The timeline counts every frame over 17.2 ms ("slow") in each 5 s window (`AutoplayBot` fields `slow`, `maxMs`, `shots`).

Found first:
- The owner's `SPIKE1.data` was an Editor capture. EditorLoop took about 14 ms a frame and the 650 ms spike was editor work; the game's own work was 4.7 ms.
- In a development build every spike of 25 ms or more came from the bot itself (screenshots and scene searches). Housekeeping searches are now gated and goal searches are spread over frames, and `-sh-autoplay-no-shots` turns screenshots off. `ProfileSummary.SlowFrameMs` is 25.

Changes:
- **One runner flies every shot** (`ProjectileRunner`, `Projectile.Tick`, `Projectile.ActiveShots`), in place of up to 560 separate `Update` calls. A shot well off the screen switches its looping glow and trail particles off until it returns (`Projectile.UpdateFx`, `FxStopped`); one-off bursts are left alone.
- **Enemy bullets fly on their own layer.** `EnemyShot` (layer 15) collides only with Player (`TagManager.asset`, `DynamicsManager.asset`). Before, the Lost Heads' bullets were on Default, which collides with Enemy, so each of about 390 bullets in the air made trigger pairs with the horde. `Projectile.TargetsPlayer` moves the shot to that layer, and a reused shot goes back to its own layer.
- **Enemy bullets have no trail** (`HazardBullets.Template`); they keep their core and halo. **A look change on placeholder art; the owner has not seen it.** The player's Magic Wand bolt keeps its trail.
- A flat enemy body (no gravity, height frozen) skips the ground scan (`EnemyController._scans`).
- Measurement fix: `-sh-autoplay-give` gave nothing once the start-level picks had filled the six weapon slots, so the suite's "heavykill" part never got its loadout. The bot now gives the items before those picks when give-after is at most 1 s (`AutoplayBot.GiveListedItems`). The suite's `-Fixed` switch gives every level fight the same loadout (Whip, Garlic, Bible, Dusk Raven, Santa Water, Lightning Ring at 8, Duplicator 2, Spinach 5).

Results (slow frames per part; the median FPS is in brackets where it matters):

| Part | before (random loadouts) | after (random) | after (fixed loadout) |
|---|---|---|---|
| L1 | 22 | 0 | 9 |
| L2 | 257 | 3 | 12 |
| L3 | 31 | 1 | 859 (69) in one run, 25 (102) in another |
| L4 | 134 | 48 (93) | 135 (84) |
| L5 | 242 | 432 (76) | 897 (84); 57 (128) in an 8-minute run |
| L6 | 74 | 6 | 2 |
| L7 | 18 | 6 | 11 |
| L8 | 138 | 7 | 11 |
| L9 | 425 | 22 | 6 |
| L10 (Shadow Kael) | 116 | 4 | 30 |
| Heavy kill | 48 | 3 | 8 |
| Pentagram wipe | 10 | 3 | 2 |

- **Not reached: 60 in every frame of every part.** The slow frames come when the horde is at the 300 cap **and** the hero kills about 60 or more a second (Level 5 at 20:00+, Level 3 late with a strong loadout). In those minutes the median is 62-90 FPS and 5-20% of frames take over 17.2 ms. The same level with about 200 enemies and 30 kills a second holds a median of 128 with about 1 slow frame per 5 s.
- Even with a fixed loadout, the kill rate differs run to run (Level 3: 15,232 and 3,927 kills), so single runs compare poorly. Comparisons were made inside one run, switching the feature every 30 s.
- **Tried and dropped,** because they measured no gain:
  - PhysX Automatic Box Pruning broadphase: Level 5 at 300 enemies was 62-70 FPS either way.
  - Drawing gems with GPU instancing: a development profile of Level 3 showed 2,009 draw calls, about 1,200 of them gems, and a 10.4 ms render thread. In release with an A/B in one run, Level 5 was 95 FPS without it and 90 with it.
  - Switching enemy bullet particles off on screen: 0.8 slow frames per window both ways.
- What is left in the profile (development build, Level 5, 300 enemies): the render thread (about 11 ms, about 1,950 draw calls, 600 SetPass) and skinning 136 enemies. The game scripts together take about 3 ms. Further steps are larger work and not done: IL2CPP (the owner installs the module), baked or instanced horde animation (art pipeline), V-Sync off with the 60 cap.
- Disk: drive D: filled up during these runs (a build failed with "not enough space"). This session's own release copies and profiler captures were deleted; timelines and summaries were kept. About 5 GB is free.

**V-Sync off by default (owner, "pehle V-Sync off"), measured as no gain.** `GameData.VSync` defaults to false, `GameData.CurrentVersion` 3, and `SaveService.Migrate` v2→v3 switches it off once; after that the player's choice stays (`AccessibilityTests`). An A/B in one run with the 60 cap (`AutoplayBot -sh-autoplay-vsync-ab`, switching every 30 s; `run-20261015-vsyncab`): the median was 60 both ways, but V-Sync off had more slow frames and jolts (frames over 25 ms, timeline field `jolts`): L2 141/11 against 7/6, L3 58/0 against 1/1, L5 67/0 against 0/0, L9 61/1 against 0/0. **V-Sync on is steadier.** Putting the default back to on waits for the owner.

**The horde on the GPU (vertex animation textures).** `HordeVatBaker` (Editor, `-executeMethod SoulHunter.Editor.Content.HordeVatBaker.BakeAll`, script `D:\SoulHunter-Validation\vat-bake.ps1`) bakes the 20 enemy and elite models (Idle, Run, Attack and Death, 98 frames at 24 fps) into `Assets/Art/Generated/HordeVat/` (73 MB; the textures are stored as text assets and could shrink) and lists them in `Resources/HordeVatLibrary`. `HordeVatDrawer` draws every body of a baked model in instanced draws (`Graphics.RenderMeshInstanced`, shader `SoulHunter/HordeVat`). The body's own renderer stays enabled with `forceRenderingOff`, so it is not skinned and its Animator is culled, while renderer checks, corpses and the hit flash keep working on it. The colour (type tint, flash, Defang, shade) is read from the material `DamageFlash` puts on the body. Bosses, Death's Shadow and any body with shadows or another material draw themselves. The horde plays only Idle, as before (no enemy prefab drives its Animator), each body at its own phase. `-sh-autoplay-vat-ab` switches it every 30 s.
- Defect found on the way: the first bake used `BakeMesh(…, true)`, which bakes in the renderer's scale (100), so the horde came out 1/100 size. It is `false` now; the baked body measures 1.49 x 2.03 x 0.70.
- A/B in one run (`run-20261015-vatab`, release, no cap), median FPS off → on: L1 110 → 142, L2 104 → 137, L5 111 → 119.5, L9 106 → 133; slow frames per 5 s window L5 4.6 → 2.2, the others 0.3-1.0 → 0.3-0.8.
- Screenshots with and without it (`run-20261015-vatab/look/shots/037_t0022.png`, `039_t0042.png`) show the same models, tints, size and facing. **Not yet seen by the owner.**
- Tests: `HordeVatTests` (T: 20 assets, clips, instanced materials, bounds, library) and `HordeVatPlayModeTests` (R: bodies handed to the drawer stay enabled, a few draws for the whole horde, baked height within 25% of the skinned one, the flash colour, switching off hands every body back).

Validation of all of the above: EditMode 935/935, PlayMode 441/441, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261015-vat-full/`).

**Draw calls (owner, 2026-10-15: "SRP batcher or call drop kam se kam karna hai").**
- Checked: the SRP Batcher is on (`UniversalRP.asset`) and the four custom shaders keep their numbers in `UnityPerMaterial`. A development profile after the GPU horde (`run-20261015-arch-prof/l5b.summary.txt`, Level 5, 100-158 enemies) gave 1,766 draw calls, 274 SetPass and 9.9 ms on the render thread per frame.
- `AutoplayBot -sh-autoplay-render-census "S,S"` lists what the camera draws by material and by source object. The timeline now records `drawCalls`, `setPass` and `batches` (development builds only; a release build writes -1).
- **Gems, a second time (mistake: the 2026-10-15 attempt above was rebuilt without searching first).** An instanced gem drawer halved the draw calls on Level 1 with 400 gems (769 to 362, SetPass 180 to 137), but FPS was 109 both ways (`run-20261015-gemab/l1`). With the earlier release A/B (3-5 FPS slower), it stays dropped and was removed again. The same run overwrote that folder's old `dev` build and `build.log`; its A/B timelines are kept.
- **Enemy bullets** (`EnemyBulletDrawer`, `EnemyBulletLook`, added to `HazardBullets.Template`):
  - Every bullet drew twice (its core mesh and its halo particle system). All bullets already shared one tinted material copy, so they were SRP Batcher-friendly. Level 5 keeps up to about 900 in the air.
  - Now every enemy bullet on screen is drawn in two instanced draws: all cores, and all halos as one soft camera-facing quad each (0.9 units, the particles' start colour). The core renderer stays enabled with `forceRenderingOff`, and the halo particle object is switched off. Flight, hits and pooling are unchanged.
  - The quad replaces the 2-3 overlapping halo particles, without their short smear behind a moving bullet. **A small look change on placeholder art; the owner has not seen it.** Screenshots with and without it (`run-20261015-bulletab3/look/shots/155_t0143.png`, `161_t0160.png`) look alike.
  - `SH10_Core_Magic.mat` and `SH10_Vfx_Soft.mat` have GPU instancing on, so the build keeps the variants (no effect on normal renderers under the SRP Batcher).
  - Measured, A/B every 30 s in one run (`-sh-autoplay-bullet-ab`; timeline `bulletDraw`, `bullets`, `bulletsAlive`), Level 5 from 19:50 with the fixed loadout:
    - development build (`run-20261015-bulletab3/l5`), the heavy minute: drawer on with 450-740 bullets, 500-870 draw calls, 76-89 FPS; off with 760-900 bullets, 960-2,180 draw calls, SetPass 240-418, 56-81 FPS and 7-118 slow frames per 5 s;
    - release builds (`run-20261015-bulletab2`), the heavy windows: on 108 against off 77 FPS (run 1), on 102 against off 97 (run 2).
    - The bullet count differs between windows, so the FPS gain is not proven; the draw-call drop is.
  - Tests: `EnemyBulletDrawerPlayModeTests` (R: 30 real enemy bullets in at most 2 draws, the core kept but not drawn, the halo particles off, they still fly, switching off hands every bullet back).
  - Validation: EditMode 935/935, PlayMode 442/442, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261015-bulletdraw-full/`).
- **Props: one mesh, one material** (owner: "castle wall or rail cart bhi jodo", "sab ko single mesh or single material mein convert karo"). A prefab census (`run-20261015-drawnow/mesh-census.txt`) showed the props were many small parts, one renderer each: Castle Wall 136, Chest 44, Rail Cart 30, Soul Lantern 29, Gold Coin 21, Rune Plate 19, XP Gem 12.
  - `PropMeshMerger` (Editor; menu Soul Hunter/Content/Merge Prop Meshes, or `-executeMethod SoulHunter.Editor.Content.PropMeshMerger.MergeAll`) joins each prop's parts into one mesh drawn with one shared material, `SH10_PropMerged` (shader `SoulHunter/PropMerged`, a copy of URP Lit's forward pass). Each part's URP Lit values are baked into its vertices: base colour (linear) in the vertex colour, glow in UV1 (HDR colours are already linear, so they are not converted again), and metallic, smoothness and a mask id in UV2. Meshes are in `Assets/Art/Models/PropMerged/`.
  - Only parts with plain URP Lit values are merged: opaque and with no texture. Disabled, inactive and mirrored parts are left as they are, and so are parts with a different shadow setting (they become a separate mesh).
  - Kept apart for the code that reads them: ground glows; the chest's Lid, LidBand, LockPlate and Cranium parts and its SoulSeam (`ChestMergeTarget`: the opening screen measures them, and the lid still swings on its hinge); the rail cart's rails and the moving cart.
  - Recolouring by material name still works through `PropMergedLook` (mask A / mask B): `ChestLook` (bronze, silver, Arcana), `SoulCasket`, the chest preview's seam and `PressurePlate`'s dimmed runes. `SoulCasket`'s tint now multiplies `_BaseColor`, which is slightly different from the old per-material tint.
  - Renderers per prefab, before → after: Castle Wall 136 → 1, Chest 45 → 8 (6 kept + lid + body), Rail Cart 30 → 4, Soul Lantern 29 → 2, Gold Coin 21 → 3 (one per tier), Rune Plate 19 → 1, XP Gem 13 → 7 (one per tier + glow). 293 → 28 in all.
  - Look: `PropLookCompare` (`-sh-before <old prefabs> -sh-out <folder>`) renders the old and merged prefab from the game camera under the same light. Mean difference (0-255): gems, coins, chest, plate and wall 0.00; cart 0.06 (`run-20261015-onemat2/look3`). The lantern read 11.2 in the tool, but a render of the old prefab, the FBX and the merged prefab in one scene shows them identical (`lantern-four.png`; the tool's first render comes out wrong). Positions and normals match the old parts exactly. **Not seen by the owner.**
  - Measured in development builds, same loadout and start, before (`run-20261015-bulletab3/dev`) and after (`run-20261015-onemat-draw`, script `run.ps1`), 5 s timeline windows after 20 s:

    | Part | Draw calls, median (min-max) | SetPass, median |
    |---|---|---|
    | Level 4 from 19:50, before | 1,745 (1,604-2,688) | 177 |
    | Level 4 from 19:50, after | 757 (374-909) | 232 |
    | Level 5 from 19:50, before | 811 (468-1,159) | 315 |
    | Level 5 from 19:50, after | 649 (246-809) | 197 |

    On Level 4 the corridor walls went from about 654 renderers on screen to a few. Level 4's SetPass did not fall: that run had more particle effects on screen (1,520 particles against 974), and SetPass follows the effects, so it is not a fair comparison. FPS was 60 in all four runs (the dev build's cap), so no FPS gain is shown. What is left: each gem still draws its merged mesh and its ground glow (264 + 264 at Level 4's 60 s). The glow is an additive transparent quad and the gem is opaque, so URP cannot draw them in one call; instancing the gems was already tried twice with no FPS gain, so the owner chose to leave the glow as it is (2026-10-15).
  - Also in this batch: damage numbers draw in one batch (`DamagePopupBatcher`, on by default; the fade is now smooth instead of 10 steps), and `SharedVfxEmitter` (blood spray and death puff from one emitter; **off by default**, not measured).
  - Tests: `PropMergeTests`, `ChestMergeTests`, `PlateMergeTests`, `WallCartMergeTests` (T), `DamagePopupBatcherPlayModeTests`, `SharedVfxEmitterPlayModeTests` (R); `PickupV2Tests`, `PressurePlateTests` and `ChestLookPlayModeTests` were updated.
  - Validation: full run EditMode 954/956, PlayMode 445/445, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261015-onemat-full/`). Both failures were test-side: the chest test expected 7 renderers (SoulSeam sits on the seam and is kept, so 8), and the rail cart test counted triangles from the `Sphere.fbx` builtin resource (760) instead of the primitive the builder uses (768). After both fixes, EditMode 956/956 (`-onemat-edit2/`). No PlayMode run after those test-only fixes.
- **Stage floors and everything else: one mesh, one material** (owner, 2026-10-15: "stage floor ko bhi single mesh or material karo", then "sabhi ko single mesh or single material banao").
  - **Floors** (`FloorMeshMerger`, menu Soul Hunter/Content/Merge Stage Floors; `StageFloorImporter` runs it after every import). A tile was two renderers with 5-13 textured URP Lit materials (8-13 draws), and InfiniteMap lays 25 tiles. Now each renderer is one mesh with the shared `SH10_FloorMerged` material (shader `SoulHunter/FloorMerged`, URP Lit's forward pass):
    - the 16 ground textures are one texture array (`Assets/Art/Textures/Ground/SH10_GroundArray.png`, a 5 x 4 flipbook imported as a 2D array, slice 0 white for untextured parts);
    - each part's linear tint (vertex colour), glow (UV1.xyz) and texture slice (UV1.w) are baked into the mesh, with its Inverse grade (`InverseMap.Grade`, UV2) and, for the Blood Arenas stains (vertex alpha 1), the plain floor that Disable Blood shows (UV3/UV4);
    - `FloorMergedLook` switches Inverse and Disable Blood with two global shader values, so no property block breaks the SRP Batcher. `InfiniteMap` and `InverseStageLook` call it.
    - The ground still only receives shadows and the obstacles cast them, so a tile stays 2 renderers. Only the tiles in view are drawn (about 8 of the 25), so the floor costs about 15 draws instead of the 60-100 of its many materials; the build A/B is below.
    - Look (`run-20261015-floormerge/look`): old and merged tile rendered from the game's 55-degree camera, normal, Inverse and Disable Blood: mean difference 0.00 on every level (Level 1's first normal render read 9.75, the tool's known first-render fault; its Inverse render read 0.00).
  - **Every FacetedPreview model** (`HordeMeshMerger.MergeEveryFaceted`, menu Soul Hunter/Content/Merge Every Faceted Model): the horde's existing bake (one mesh per renderer, colours in the vertices, the skin kept, the shared `SH10_HordeMerged` material) now also covers 107 other prefabs: the 13 heroes and `SH10_Kael` (Player nests it), every weapon model and projectile model, Floor Chicken, Vacuum, Orologion, the old SH10 gem and chest, and the showcase props. Heroes went from 6-10 draws to 1.
    - The hit flash already tints a HordeMerged model through a shared tinted copy (`DamageFlash`, `HordeMaterials`); the Laurel shield now also sets `_TintAmount` 1 so its charge colour still covers the whole model.
    - Left as it was: `L01R_GraveGhoulStudy` (scene `L01_AssetReview` only), which drew differently once merged (`HordeMeshMerger.LeftAsTheyAre`).
    - Re-importing heroes (`HeroModelImporter`) or other faceted models rebuilds their prefabs unmerged; run the menu item again after.
  - **Merchant and the light-source pickups** (`PropMeshMerger.Targets`): the Wandering Merchant (one mesh with 14 URP Lit colours, model object kept) and Gilded Charm, Luck Charm, Hellfire Breath and Soul Toll (3-6 primitives each) are one PropMerged mesh each.
  - Look (`run-20261015-allmerge/look`): 111 changed prefabs, old against merged from the game camera: mean difference 0.00 for all of them (Gilded Charm read 82 in the first pass and 0.00 when rendered again in another order, the tool's render-order fault).
  - What is still more than one material: the chest (lid, lock plate, bands, skull and seam stay apart so the lid opens and ChestLook recolours them), the unused `Enemy_Ghost` prefab, the ghoul study, and every transparent glow, trail and particle (they cannot share a draw with an opaque mesh).
  - **Draw-call A/B in builds** (`D:/SoulHunter-Validation/run-20261015-allmerge-draw`, scripts `run.ps1` and `quiet.ps1`): development builds, before = the props-merged build of `79cf85f6` (`run-20261015-onemat-draw/dev`), after = this code; 1366x768, god mode; median of the 5 s timeline windows after 10-20 s; save unchanged.

    | Level start, Kael's Whip only, 45 s | Draw calls before → after | SetPass before → after | Enemies |
    |---|---|---|---|
    | Level 1 | 172 → 52 | 67 → 24 | 15-16 |
    | Level 4 | 192 → 87 | 42 → 26 | 50 |
    | Level 5 | 132 → 44 | 33 → 17 | 104-112 |
    | Level 8 | 161 → 48 | 44 → 22 | 20 |

    - With the same enemies on screen, the floor, hero and weapon models cost 60-70% fewer draw calls and 40-65% fewer SetPass calls.
    - The heavy fight (level-90 hero, fixed loadout, 19:50 on Levels 1, 4 and 5) gave no fair comparison: the after runs had about twice the enemies on Levels 4 and 5 (199-201 against 104-105), so their totals (L1 486 → 612, L4 788 → 716, L5 472 → 641 draw calls) follow the horde, gems and effects, not this change.
    - FPS was 60 in every run (the development build's cap), so no FPS gain is shown.
  - **FPS A/B in release builds** (`D:/SoulHunter-Validation/run-20261015-allmerge-fps`, scripts `fps-ab.ps1`, `l4.ps1`, `fix.ps1`, `fix2.ps1`, `iso.ps1`): before = the same code with the prefabs of `46e0bfff` (props merged, floors and faceted models not); 1366x768, Ultra, frame cap and V-Sync off (`-sh-autoplay-quality-ab 5,5`), god mode, Kael's Whip only, 60 s from each level's start; the builds take turns so heat and background work fall on both; median FPS of the 5 s windows after 15 s; save unchanged.
    - Level 1 +5% and +3% (265-267 → 269-281), Level 5 +2% (242 → 247), Level 8 +4% (267 → 279).
    - **Level 4 lost FPS: 229.6 → 203.2 (-11.5%) over 5 rounds.** A development-build profile showed the CPU work lower (render thread 3.3 → 2.2 ms) and the main thread waiting longer for the GPU (0.95 → 2.23 ms): a GPU cost.
    - Cause 1, fixed: the ground texture array was written with an alpha channel, so it compressed to DXT5, twice the reads of the DXT1 ground textures. Written as RGB with no alpha source (`FloorMeshMerger`, checked by `FloorMergeTests`): Level 4 221.7 → 207.0 (-6.6%), Level 1 265.5 → 268.7.
    - Tried and dropped: drawing the floor last in the opaque queue (2449): Level 4 228.2 → 214.2 (-6%), no gain.
    - Cause 2, open: a build with the old floor prefabs and the new faceted models measured Level 4 at 218.5 against 217.8 for the full change and 229.8 before, so the remaining ~5% (about 0.25 ms of GPU at 220 FPS) comes from the faceted-model, merchant or charm merge, not from the floor. Not found yet.
    - These are quiet scenes (15-110 enemies) at 200-280 FPS; the heavy fights, where FPS falls under 60, were not measured in a release build.
  - Tests: `FloorMergeTests`, `AllMergeTests` (T); `RenderingSetupTests`, `WanderingMerchantTests`, `AccessibilityPlayModeTests` (Disable Blood) and `RandomizersPlayModeTests` (Inverse) were updated.
  - Validation, related fixtures only (section 10, the 2026-10-15 commit rule): EditMode 988/988; PlayMode 26/26 for the floor fixtures (`run-20261015-floormerge/fx1`) and 78/78 for heroes, weapons, pickups, merchant and the horde (`run-20261015-allmerge/fx1`); save untouched. **No full PlayMode run yet** (due before the next push or build). Not seen by the owner.

Validation before the V-Sync and GPU horde work: EditMode 933/933, PlayMode 440/440, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261015-perf-full/`). `ProjectileRunnerPlayModeTests` (R): one runner flies every shot at its speed and ends it at its lifetime; a switched-off shot stays put; off-screen particles go off and come back; a flat body skips the ground scan and still chases; an enemy bullet keeps its core and halo, has no trail and flies on the EnemyShot layer, which meets only the player.

### Chest hitches and a frozen run (owner, 2026-10-16: "CPU PROFILE CHALAO", "HAAN TIMER LAGAO AUR L5 CHALAO", "continue karo")

A development-build CPU profile of Level 5 (`D:/SoulHunter-Validation/run-20261016-cpuprof`) had a 9,050 ms frame when a chest elite died and its chest dropped, and a 348 ms frame at a chest opening. The profiler window had missed both.

**Timers** (`SlowSpan`, Core/Services): `using (SlowSpan.Measure("name"))` keeps sections of 5 ms or more and logs those of 50 ms or more. `AutoplayBot`'s `hitch` event lists them under `spans`. They wrap the save path, chest pickup, chest screen and enemy drops. A watchdog (`SlowSpan.StartWatchdog`, started by `AutoplayBot`, file `stall.txt`) writes the sections open on the main thread, the process CPU time and the thread count once frames have stopped for 3 s.

**Cause 1, fixed: the chest-close save.**
- Measured: 4 runs (`run-20261016-spans`), Level 5 from 19:50, level-90 hero, fixed loadout.
- `ChestUI.CloseChest` called `SaveService.SaveGame`, and its file write (`File.WriteAllText` / `File.Replace`) took 62-363 ms on the main thread. The JSON took under 5 ms.
- New `SaveService.SaveInBackground`: the JSON is made at once, and the write is queued on the worker that the timed gold save already used. It is the same atomic write with its `.prev`, and quit and loads wait for it.
- Saves made during a run now use it: the chest closing, Collection, Bestiary, character, PowerUp and item unlocks, Soul Egg, Soul Casket, relic, merchant purchase and spending gold. Menu screens still save at once.
- After the change (`run-20261016-bgsave2`, 4 runs): no save section over 50 ms during play, only one of 157 ms at quit.

**Cause 2, open: native frames and one frozen run.**
- One of those 4 runs froze for 8 minutes right after picking up a chest. Its log ends at the chest's gold line.
- The release A/B's "before" build (2026-10-15, without this change) also stalled at about 80 s.
- 16 more runs did not freeze (`run-20261016-hang`, `-hang2`). For these, every runtime `Update`, `FixedUpdate` and `LateUpdate` was wrapped in a timer (in the isolated copy only, `run-20261016-hang/wrap_updates.py`).
- In one of them, two frames of 1,547 and 1,415 ms came in the frames where chest elites died. They had no script section of 5 ms, no GC and no pause, so the time went outside script `Update`s: physics, rendering or the GPU, physics callbacks or coroutines.
- Three profiler captures of that window (`run-20261016-hangprof`) caught no slow frame (worst 30.7 ms).
- The cause is not found. The clock skip in these runs spawns about 11 chest elites at once (a debug-skip artifact).

Tests: `SaveServiceTests.SaveInBackground_WritesAtOnce_AtomicallyKeepingThePreviousSave` (T). `WanderingMerchantPlayModeTests` now waits for the queued write before reading the file. Validation is in the commit message.

**The hang hunt, second part (owner: "2 karo, hang ke peeche jao").**

Tools:
- WinDbg was installed (winget `Microsoft.WinDbg`, `cdbX64.exe`). It reads every thread's native stack from a running game without stopping it (`-pv`), with Unity's player PDBs from `PlaybackEngines/windowsstandalonesupport/Variations` and the Microsoft symbol server (cache in `D:\SoulHunter-Validation\symcache`).
- `D:\SoulHunter-Validation\run-20261016-hangdbg\go.ps1` plays Level 5 runs. When `stall.txt` appears, it takes the stacks twice, 5 s apart, then closes the game. No run has stalled under it yet.

Findings:
- **Every problem so far was in a development build.** A release build wrote a 78 KB log per run; a development build wrote 2.4 MB, because every line carries a stack trace. 15 release runs were clean (`run-20261016-hangdbg`).
- The frozen run's last log line was a chest's gold message. One development run (`-hangdbg2`, run 7) had a 328 ms frame at a chest elite's death, timed inside `EnemyDrop.HandleDeath` (314 ms). That method's own `Debug.Log` was its only unmeasured part.
- **Disk writes on this laptop stall.** D: is 98% full. Once the bot's reports went to a worker, its slowest write per 5 s window (`writeMs`) reached 735 ms and 137 ms (`-hangdbg3`), and the game did not hitch.
- Windows logged no GPU driver reset. The laptop shut down uncleanly twice on 2026-10-16 (11:19 and 14:44, Kernel-Power 41), and firmware limited the CPU once (event 37).
- The bot rewrote its whole `events.jsonl` and `timeline.jsonl` on the main thread at every event. The two 1.5 s frames came in the frames it wrote elite-death events.

Changes:
- `ReportWriter` (Core/Game): the bot's reports are written on one worker thread, the newest content per file wins, and the timeline records `writeMs`.
- `GameLog.Info` (Core/Services, `[Conditional("UNITY_EDITOR")]`): 40 routine gameplay logs moved to it from Debug.Log, in LevelUpManager (12 lines per level-up), PlayerExperience, WeaponManager, EnemySpawner, ChestLogicController, EnemyDrop, MagnetPickup and TimeFreezePickup. Player builds no longer write them. Warnings and errors are unchanged.
- More `SlowSpan`s inside `EnemyDrop.HandleDeath` and the pickup pool.
- After the change (`-hangdbg4`, 12 development runs), the log is 36-42 KB per run, with 0 hitches and 0 stalls.

The hang came about once in 30-40 runs, so 12 clean runs do not prove it is gone. **The cause is not proven**: no stack has been caught.

Full run before the push of `7ba222fc` and `f7298039`: EditMode 989/989, PlayMode 445/445, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261016-prepush-full/`).

### 60 FPS at 1080p, V-Sync off: the jolts (owner, 2026-10-17)

Owner: V-Sync stays off with the 60 cap; fix where it drops, nothing else first.

Measurement (`frametime-suite.ps1 -Capped -Fixed -More '-sh-autoplay-1080p'`, release, reference laptop):
- The 3D view renders 1080p's pixel count inside the 1366x768 window (`AutoplayBot -sh-autoplay-1080p`, PerformanceBenchmark's render scale).
- Quality was the stored Ultra.
- At 1366x768 every part held 60 with under 0.8% of frames over 17.2 ms and 2 frames over 25 ms in about 87,000 (`run-20261017-fps60cap`).
- At 1080p, slow frames doubled, and Level 3 had 19 jolts up to 43 ms (`run-20261017-fps60-1080`). That run's level-ups took Skull O'Maniac and Cursed Coffer, so the horde stayed at 300.

New tools:
- `AutoplayBot` logs a `jolt` event for every frame over 25 ms, with the SlowSpan sections of that frame.
- `-sh-autoplay-span-ms N` records sections from N ms (default 5).
- The profiler writes a new file every 4 s (`<name>-partN.raw`), because the editor keeps only the last 300 frames of a loaded capture. The file switch itself makes a 50-80 ms frame and a catch-up frame; ignore those.
- In development builds SlowSpan sections show in the profiler.

Tool defect found: `-sh-autoplay-give` set a passive's level without applying its stat. Every given passive (Duplicator, Spinach, Skull O'Maniac, Cursed Coffer) did nothing in earlier `-Fixed` runs, so those loads were lighter than they read. Passives are now given through `ApplyUpgrade`, one level at a time.

Fixes:
- **Chest pickup:** every chest made a 25-34 ms frame.
  - `ChestLogicController.ProcessChest` searched the scene twice per chest (`FindFirstObjectByType`, 5-7 ms at 300 enemies). It now uses `WeaponManager.Instance` and the new `LevelUpManager.Instance`.
  - `GameContentCatalog.Load()` ran `Resources.Load` on every call (about 2 ms; five in one 5-item chest). It now loads once.
- **Crowd hits:** a bomb or pool touching a crowd made 12-18 ms in one physics step.
  - Every hit searched the parents for its `IDamageable` and its `EnemyController`. `DamageableLookup` now caches the damageable per collider in all 8 hit paths, and `HealthController.Enemy` is cached.
  - `Projectile` skips the player search for Enemy-tagged colliders.

Result at 1080p, fixed loadout (`run-20261017-fix1`). Levels 1, 2, 3 and heavy kill each read slow frames / jolts / slowest frame:
- Level 1: 0.79% / 1 / 25 ms before, 0.64% / 0 / 17 ms after.
- Level 2: 1.62% / 1 / 28 ms before, 1.13% / 0 / 19 ms after.
- Level 3: 1.92% / 19 / 43 ms before, 0.74% / 0 / 17.5 ms after. The before run had the Curse passives and the after run did not, so this row does not compare like with like.
- Heavy kill: 1.24% / 1 / 42 ms before, 1.40% / 1 / 34 ms after.
- Level 3 with the Curse passives: 9 jolts before, 6 after (25-31 ms, cause not found yet).

Validation: EditMode 989/989; PlayMode fixtures 91/91 (chest flow, look and elites, weapon and evolved sweeps, Shadow Kael, spectral stage, Pentagram, accessibility, player loadout, level-up rules), 0 exceptions, save untouched (`run-20261017-fix1/tests`).

**Fix 2, weapon zones on their own layer.**
- How it was found:
  - A split profile showed `ProjectileDamage.OnTriggerEnter` called 904-1,165 times in one physics step, with 14-22 damage numbers.
  - A trigger counter (`ProjectileDamage.EnterStats`, `AutoplayBot -sh-autoplay-count-triggers`; jolt events list the top entries) gave the source: Lightning Ring strikes and Dusk Raven blasts, on Default, made 700-1,340 enters on Default objects (gems and pickups) per jolt, all doing nothing.
  - `ProfileSummary` now prints each marker's call count in the slowest frames (`[xN]`).
- The fix:
  - The unused `DamageZone` layer (11) now meets only Player and Enemy (`DynamicsManager.asset`).
  - `AutoAttackWeapon.AimAtHostiles` puts every spawned zone (ProjectileDamage or TouchDamage without a flying Projectile or a physics Rigidbody) on it.
  - Enemies, Soul Lanterns and rail carts are on the Enemy layer, so weapons still reach them. Shadow Kael's mirrored zones still reach the player.
- Test: `StageFloorTests.WeaponZones_MeetOnlyThePlayerAndEnemies` (T).
- Note: `results-run.ps1` and the measuring scripts copy only Assets, so a ProjectSettings change must be copied to the isolated copy by hand.
- Jolts over 25 ms after 30 s (`run-20261017-fix2`):
  - Level 1, Level 2 and heavy kill: 0.
  - Level 3 with the Curse passives: 6 before, 1 after.
  - Level 3: 2, in a run whose level-ups took the Curse items (300 enemies, 11,512 kills).
  - The jolts left are real damage: Lightning Ring dealt 400-691 hits to enemies in one physics step.
- Validation: EditMode 990/990, PlayMode fixtures 134/134 (weapon and evolved sweeps, Shadow Kael, both Duplicator fixtures, light sources, stage hazards, spectral stage, new evolution rules, Pentagram, player loadout, character sweep), 0 exceptions, save untouched (`run-20261017-fix2/tests2`). A first PlayMode run stopped while loading, before any test ran, with no error logged.

**Fix 3, damage numbers capped per frame.**
- Why the hits were still dear: Lightning Ring at level 8 with Cursed Coffer strikes about 4.5 units wide. The bot stands in the crowd, so one strike covers most of a 300 horde (correct VS behaviour, not a bug).
- Measured with `HitTiming` (`AutoplayBot -sh-autoplay-hit-timing`, release build; jolt events list the previous frame's sums; `run-20261017-hittime2`):
  - a hit on an immune enemy costs under 1 us, and finding the target about 1 us;
  - a damaging hit costs about 55 us, of which the damage number is about 22-25 us: 10-16 ms per jolt frame;
  - deaths cost 2-7 ms (about 70 us each), and the other parts of a hit under 1.5 ms together.
- The cause: the number pool holds 25, so in a 600-hit frame every number past the 25th overwrote one shown earlier in that frame, and was never seen.
- The fix: `DamagePopupManager.ShowDamage` starts at most `MAX_POPUPS` (25) numbers per frame (`ShownThisFrame`). The screen looks the same; the first 25 hits of a frame show their numbers instead of the last 25.
- Profiler markers `Hit.*` (free in release) replace the SlowSpans that the hit path had for diagnosis.
- Result, Level 3 with the Curse passives at 1080p (`run-20261017-fix3/measure`):
  - numbers in the heaviest frames take at most 2.7 ms;
  - 3 jolts, all 25-26 ms, in one second;
  - 2.28% slow frames.
  - Jolt counts this small vary between runs (1 after fix 2), so only the number cost is a clear gain.
- Test: `DamagePopupBatcherPlayModeTests.AFrame_StartsAtMostAPoolOfNumbers_AndTheNextFrameShowsAgain` (R).
- Validation: EditMode 990/990; PlayMode fixtures 41/41 (popup batcher, HUD fixes, accessibility, weapon sweep, Results, Pentagram, damage flash), 0 exceptions, save untouched (`run-20261017-fix3/tests`).
- Left: about 2% of frames at 17-20 ms with 300 enemies at 1080p, and deaths at about 70 us each.

**All parts after fixes 1-3** (`run-20261017-final1080`, same setup as the morning run):
- While playing (not paused), no part had a frame over 25 ms. Slowest running frames were 17.5-23.1 ms; Level 3 went from 43.1 to 22.3 ms and heavy kill from 42.5 to 17.5 ms.
- The three jolts left were chest screens opening, with the game paused.
- Slow-frame shares rose a little in several parts (for example 0.8% to 1.3%). The loads are heavier than in the morning: given passives now apply, and several parts had more enemies and kills.
- Deaths were not changed. A release run with frames over 20 ms logged (`AutoplayBot -sh-autoplay-jolt-ms 20`; release death timing in `HitTiming`) had no running frame over 19.5 ms in Level 3 with the Curse passives.

**Fix 4, chest opening (owner: "safe fix the chest opening", 2026-10-18).**
- The preview's 512x512 MSAA RenderTexture is kept between chests (`ChestPreviewStage.HandBackTexture`, `Create(..., reuse)`). Later chests now open in about 1 ms (`ChestUI.OpenChest` was 16-20 ms).
- `ChestUI` builds the chest screen and a hidden, switched-off preview one frame after the run starts (`PrewarmNextFrame`, `ChestOpeningScreen.Prewarm`), so the first chest no longer builds them. The first chest went from 20-22 ms to 7-10 ms in `OpenChest`, and from 34-43 ms to 27-31 ms per frame.
- Still in that paused frame: `Show.Layout` 3-4 ms on the first chest, and 4-8 ms when a chest makes an evolution (the new weapon is built).
- The chest's look and behaviour are unchanged. Disabling the Pause key costs under 1 ms.
- Validation: EditMode 990/990; PlayMode fixtures 23/23 (chest flow, preview, look, screen fit, chest elites and bosses, Arcana choice, Pentagram), 0 exceptions, save untouched (`run-20261018-chest2`).

**Full suite before push (2026-10-18).**
- The first run gave EditMode 990/990 and PlayMode 393/446 (`run-20261018-prepush-full`).
- The 53 failures came from the owner's real save, not from the game. It now holds 15 bought PowerUps (Might 5, Armor 3, Max Health 3, Recovery 5, Amount 1, Revival 1, Luck 3 ...). PlayMode tests play on a copy of that save and are written for a hero with none, so for example Duplicator +2 read 3, Shadow Kael gained a revival, and durations and damage moved.
- One more failure was an `IOException`: the previous test's background save was still writing the test copy when the next test replaced it.
- Fixes, tests only:
  - `SaveService.TestSaveLoaded` runs on the test copy right after it loads, and `GameplayPlayModeTestBase` clears PowerUps and Soul Eggs there. The real save is not touched, and tests that need PowerUps set them themselves.
  - The test-copy delete and copy retries briefly when the file is in use.
- Rerun: EditMode 990/990, PlayMode 446/446, 0 exceptions, save untouched (`run-20261018-prepush-full2`).

### Phone heavy fight at Ultra (owner: "1", 2026-10-18)

realme RMX5004, Ultra, release APK, Level 5 from 19:50 with a level-90 hero and the fixed loadout, landscape, god mode.
- **The main thread is the limit, not the GPU.** Development profiles (`D:/SoulHunter-Validation/run-20261018-phone-prof/`, `-prof2/`): main thread 17-25 ms per frame, render thread 3-4 ms. Physics was 5 ms with about 220 enemies and 550 enemy bullets, and 9-11 ms with 300 enemies and 900 bullets.
- **The phone throttles early.** `dumpsys thermalservice` reached Thermal Status 3 (severe) 30-47 s into every run, with the battery at 36-38.5 °C.
- **Bullets are what drops it.** In the release A/B run (`run-20261018-phone-bulletab/`), 300 enemies with no enemy bullets in the air held 59-60 FPS even at severe. With 400-1,050 Lost Head bullets in the air it read 45-57.
- **Fixed:** `EnemyBulletDrawer` keeps each bullet's batches and transforms (`EnemyBulletLook.CachedBy`) instead of about ten engine calls per bullet per frame. Development profiles: 1.16 ms per frame before, 0.74-0.92 ms after, with more bullets after. The timeline read about 400-590 bullets around the first profile and about 770-920 around the second, but the count inside each profile window was not recorded, so the per-bullet figure (about 2 µs before, under 1 µs after) is an estimate.
- **Tried and dropped:** enemy bullets hitting the player by distance (`PlayerReach`) with their collider and collisions off. In the A/B in one run (`-sh-autoplay-bulletphys-ab`, switching every 30 s) FPS at the same bullet counts was within noise (about 52 against 50 at ~1,000 bullets, 55 against 55 at 400-700). The bodies stay in PhysX because `Projectile` requires a Rigidbody. Patch kept in `run-20261018-phone-bulletab/bullet-distance.patch`.
- Validation: EditMode 991/991; PlayMode `EnemyBulletDrawerPlayModeTests`, `ProjectileRunnerPlayModeTests`, `StageEnemyTypePlayModeTests` and `HordeVatPlayModeTests` 15/15; 0 exceptions, save untouched (`run-20261018-bulletcache`).
- **Drawing is not the cost.** An A/B in one run (`run-20261018-phone-bulletlook/`, `-sh-autoplay-bulletlook-ab`, `EnemyBulletDrawer.DrawLevel`) drew cores and halos, cores only, then nothing. With nothing drawn and about 950 bullets in the air it read 46-49 FPS, the worst of the run.
- **Fixed: enemy bullets have no physics.** `EnemyBullet` (World/Hazards) has no Rigidbody and no Collider. It flies straight (`EnemyBulletRunner`) and hits the player when `PlayerReach` says they touch, through the same damage path (DamageableLookup, DamageSource "Pile Assault", TakeDamage), then ends (pierce 1). `HazardBullets.Fire` makes them from a copy of the bullet template with the Projectile, Rigidbody and Collider taken off; it returns a `Component`. Shadow Kael's mirrored shots stay Projectiles. `HazardBullets.LightBullets = false` fires the old kind (A/B and tests).
  - **Fair A/B, gain measured** (`D:/SoulHunter-Validation/run-20261018-phone-fair/`, `go.ps1`, `analyse.py`).
    - Setup: 4 runs of 4 minutes, each started on a cooled phone (thermal status 0, battery under 35.5 °C). The kind switches every 25 s (`-sh-autoplay-lightbullet-ab-seconds 25`), and runs 1 and 3 start light while runs 2 and 4 start physics (`-sh-autoplay-lightbullet-first`).
    - Counted: every 5 s window that starts at least 6 s after a switch (timeline `lightSince` >= 11), because bullets of the other kind live 6 s.

    | Windows | Light bullets | Physics bullets |
    |---|---|---|
    | 300+ bullets in the air | 13; mean 58.9, 57-60 FPS | 14; mean 52.1, 44-60 FPS |
    | Under 300 bullets | 16; mean 58.8, 57-60 FPS | 16; mean 58.9, 57-60 FPS |

    - With 800-990 bullets in the air, physics bullets read 44-49 FPS; light bullets with 850-936 read 58-60. Run 4 was lighter (224 enemies at most).
    - An earlier unfair pair (`run-20261018-phone-lightbullet/`: one order, one run on a hotter phone) was first reported as "58-60 against 49-56". The `a13a02ba` commit message carries that overstated reading.
  - Tests: `EnemyBulletPlayModeTests` (R): no Rigidbody or Collider; hits the hero once for its damage and ends; one passing 3 units by does not hit and ends at its lifetime; a pooled bullet starts where it is fired. `ProjectileRunnerPlayModeTests` keeps its checks on the physics kind. EditMode 991/991, PlayMode 58/58 (EnemyBullet, EnemyBulletDrawer, ProjectileRunner, StageHazard, EnemyAbility, StageEnemyType, ShadowKael, Dash), 0 exceptions, save untouched (`run-20261018-enemybullet`).
- **Open: a late drop with no bullets, seen once and not reproduced.** In the second light-bullet run, from 295 s (stage 23:53) to the end, FPS fell to 13-30 with about 295 enemies and almost no bullets, in both bullet kinds. The battery was at 39.2-39.5 °C, and the load (300 enemies, about 125 kills a second) was unchanged from the 80 s before, which held 57-60.
  - **Gold Fever is not the cause** (`run-20261018-phone-fever/`). A Gilded Fever Omen at 230 s with about 60 kills a second left at most 58 coins on the ground (the magnet collects them), and FPS stayed 58-60.
  - **Not reproduced** in a development profile of 23:25-24:23 (`run-20261018-phone-late/`; only about 200 enemies, 58-60 FPS) or in three 7-minute runs logging the CPU clusters' current frequency (`run-20261018-phone-freq/`, `-phone-hot/`).
  - In `-phone-hot` run 1: 300 enemies for over 4 minutes, battery up to 39.9 °C, 54-60 FPS. The 20 FPS in the last sample is the exit.
  - **The phone throttles in steps.** The big cluster (2.5 GHz) held 2.5 GHz to about 38 °C, 2.4 GHz from about 38.3 °C, and 2.1 GHz from about 39.5 °C, sometimes dropping to 1.3-1.9 GHz.
  - In `-phone-hot` run 2 (40-40.8 °C, big cluster at 2.1 GHz), a wave of about 600 light bullets with about 200 enemies dipped to 40-53 FPS. Without bullets it held 59-60.
- **Tried and dropped: a per-bullet fast path** (`EnemyBullet.FastPath`). The runner read the player's position once a frame and skipped PlayerReach for far bullets, and the drawer built matrices from the bullet's kept position and rotation instead of reading two transforms. The patch is kept in `run-20261018-phone-fastab/fastpath.patch`.
  - Phone A/B in one run (`run-20261018-phone-fastab/`): two 7-minute runs back to back, switching every 25 s, in both orders. Battery went from 34.0 to 40.2 °C; the big-cluster median was 2.5 GHz in run 1 and 2.4 GHz in run 2.
  - Result: fast 59.4 and slow 59.8 FPS mean over the clean windows with 300+ bullets (5 and 4 windows); 59.5 and 59.5 under 300. Minimum FPS was 57 and 58 for the two runs.
  - No gain was measured, and the 40-53 FPS dips of `-phone-hot` run 2 did not come back with either path.

### Full bug MIS, batch 1: the S2 fixes (owner, 2026-10-19)

Owner: "rules wiki ka follow hoga, jo bhi fix hoga wo safe hoga, fps 60 constant, v-sync off". The register is `Docs/BUG_MIS_Full.md`: 58 code findings, 0 S1, and a game-object audit of 7,503 objects. Batch 1 fixed 15 of the 17 S2 rows. Four fixer agents worked in separate git worktrees (`D:/SoulHunter-Validation/wt-*`) and the lead tested and merged their work.
- **Weapons**
  - CMB-01: evolutions built on a base script no longer get the base weapon's level table (`AutoAttackWeapon.LevelLocked`, also on Shadow Kael's copy). Wiki: Unholy_Vespers, Heaven_Sword and Soul_Eater give their own numbers, and "With Limit Break, [it] can be further leveled up". Unholy Vespers is 30 damage with 4 books, Heaven Sword 77 with 1 sword at 3.3 s, Soul Eater 20 at 1.0 s.
  - CMB-02: Pentagram erases only the enemies on the screen (wiki Pentagram: "every enemy, their drops, and all items on the screen").
  - CMB-03: a weapon zone forgets a killed enemy (`HealthController.Life`) and no longer hits its pooled body elsewhere.
  - CMB-04: Limit Break, Hyper and Arcana now reach the Magic Wand (Damage, Area, Speed, Amount), Crimson Shroud (Area, Amount, base damage) and the Peachone and Dusk Raven orbit (Speed; wiki: "Speed increases how fast the circle moves").
  - CORE-02 (the row was half wrong): wiki Gemini_(I) says counterparts "remain as independent weapons if their counterparts unite", so they keep firing. Taking Gemini while holding a union now gives its constituents' counterparts (Twilight Flock at its level, Revenant Wheel at max).
- **Enemies**
  - AI-01: Shadow Kael captures the loadout again at every encounter. A pooled body clears the old mirrored weapons.
  - AI-05: Endless contact damage is applied on every spawn, bosses included.
  - AI-02: a passed-on chest skips Fixed Direction enemies.
  - AI-03: the Tide Caller's death gives no chest (balance from The_Drowner's Boss Rash variant).
  - AI-04: the 30:00 clear takes instant-kill-resistant enemies away with no rewards, and caskets ignore guard deaths during the transition (Mad_Forest: "All enemies on the screen are cleared").
  - AI-07: the Closing Arena runs on the stage clock.
- **Items**
  - ITM-01: Data Recovery never overwrites a replaced-save backup (`.replaced.N.bak`).
  - ITM-02: the magnet sweep queries only the pickup layers and grows its buffer instead of overflowing.
- **UI**
  - UI-01: the Pause key is off on the level-up screen and in the merchant shop.
  - UI-02: Draw a Fate keeps focus on the Arcana cards.
  - UI-03: Results DONE keeps the game paused while the menu loads.
- **Open S2 rows**
  - CMB-05: Shadow Kael's mirrored Crimson Shroud and Laurel. Owner decision.
  - AI-03 and AI-04 each carry a balance choice the owner may revisit.
- **Validation:**
  - Per branch: save EditMode 992/992 with PlayMode 7/7; UI EditMode 991/991 with PlayMode 33/33.
  - Integration branch: EditMode 993/993 and PlayMode 178/178 (26 fixtures), 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261019-fix-integ/`).
  - The full PlayMode suite and an FPS check follow; this entry is updated with their results.
- Not played by the owner.

### Full bug MIS, batch 2: owner decisions and the S3 rows (owner, 2026-10-19)

Five fixer agents worked in their own worktrees (`D:/SoulHunter-Validation/s3-*`). The lead merged their work with the owner-decision branch into `fix/s3-integration` and fixed what the first Unity runs found: two glued method lines and two compile errors in new tests, and the HordeVatDrawer prune being starved by repeated requests.

**Owner decisions**
- CMB-05: Shadow Kael's mirrored Laurel and Crimson Shroud protect him from nothing. There is no shield block, no 10-damage cap, and no shield shown. His copy's retaliation also stops, because in VS it fires only on a block.
- AI-03 ("SH = VS", The_Drowner):
  - the Tide Caller is unaffected by Pentagram;
  - it keeps freeze, debuff and knockback resistance, but not instant-kill resistance;
  - Eclipse Sigil, Sundered Hour, Crimson Shroud and Soul Toll kill it and drop the Drowner's 6/66/100 chest;
  - ordinary damage defeats it with no chest.
- Bloody Tear ("SH = VS", Bloody_Tear): heals 8 only on a crit hit that damages, at most once a second. This replaces Soul Hunter's earlier per-lash heal.
- Holy Wand (Holy_Wand): 30 damage, cooldown 0.5 s, 4 bolts 0.1 s apart, and pierce 2 plus Limit Break Pierce cards. On pierce the owner said "SH = VS". The infobox says "pierce = 2" and the Tips say "its damage against multiple enemies is lacking even with the extra pierce"; only the Effects line says "non-piercing". Tests: WeaponS3Fixes, DuplicatorEverywhere, EvolvedWeaponSweep and ArcanaEffects 40/40, EditMode 999/999.

**Weapons**
- CMB-06: an axe's 5 s life is kept on the axe.
- CMB-07: Requiem and Garlic query only the hostile layer and grow their buffers.
- CMB-09: an upgrade restarts a Peachone or Dusk Raven activation instead of running two.
- CMB-12: a duplicate WeaponManager is destroyed.
- CMB-13: FireInSuccession allocates nothing. One `SuccessionRunner` fires the later shots, with the same timing.

**Enemies**
- AI-06: fliers and hazard members spawned during an Orologion freeze.
- AI-08: hazard spawns stop at 500; item guardians are exempt.
- AI-10: `_special` is cleared on reuse.
- AI-11: enemy bullets end on the stage clear and the stage change.
- AI-12: the drawers forget destroyed bodies.
- CORE-11: the Level 9 ring material is destroyed.

**Items and save**
- ITM-03: stage-item clean-up leaves reused pool objects alone.
- ITM-04: a failed background save is retried.
- ITM-05: a pending save is flushed on pause and focus loss.
- ITM-06: chest counters are saved on every path.
- ITM-07: the Orologion freeze is reset between runs.
- ITM-08 to ITM-11: performance fixes.
- ITM-12: the unlock text now reads "Clear Stage n: ... defeat its guardian and survive to 30:00".

**UI**
- UI-04: later chests reuse the preview texture.
- UI-05: a skip leaves no half-faded flash.
- UI-06: Esc and gamepad B close Evolutions and Map (`PauseKeyGate`).
- UI-07 to UI-09: no per-frame strings or arrays.
- UI-10: rich-text colours follow a live theme change. The stats panel now uses the theme accent, not #FFD940.
- UI-11: GameTime re-applies the time scale when it prunes a destroyed owner.

**Core**
- CORE-04: Omen formations are filtered at fire time.
- CORE-05: Soul Burst is ignored while paused.
- CMB-10: dash and Soul Burst immunity is counted (`HealthController.BeginAbilityImmunity`), and `IsInvincible` stays the god-mode switch.
- CORE-07: a pause holds a dash.
- CORE-08: with V-Sync on, the interval is the floor of hz/60. V-Sync stays off by default.
- CORE-09 (balance): Banish is unavailable on the Gold Pouch and Roast Chicken cards.
- ITM-13 (balance): a maxed level-up with no screen pays one reward.
- CORE-10: the maxed reward cards are reused.

**Validation**
- Integration, full run: EditMode 999/999, PlayMode 494/499 (`D:/SoulHunter-Validation/run-20261019-full-s3-integ3/`).
- After the three fixes: EditMode 999/999, and the HordeVat, UiThemeLive, WeaponS3Fixes and EnemyMinimum fixtures 21/21. 0 exceptions, save untouched (`-s3-fix1/`).
- **Full suite on main** (`159192f4`): EditMode 999/999, PlayMode 499/499, 0 exceptions, save untouched (`run-20261019-full-main-s3/`). The Holy Wand pierce commit after it ran its fixtures only (40/40).
- **FPS check** (`run-20261019-fps-after-mis/`): release build, 1080p pixel count, V-Sync off and the 60 cap, fixed loadout, 3 minutes per part, against `run-20261017-final1080`.
  - Slow frames (over 17.2 ms) went down in 10 of 13 parts, for example Level 7 1.30% to 0.79%, Shadow Kael 1.31% to 0.73% and Level 3 with Curse 1.58% to 1.10%. They went up on Level 1 (1.19% to 1.61%) and were about the same in heavy kill. Every part is between 0.7% and 1.7%.
  - Slowest running frame: better on Level 5 (25.8 to 17.5 ms), Level 7 (23.1 to 17.5), Shadow Kael (27.1 to 17.4) and Level 3 with Curse (42.3 to 19.2). Worse on Level 4 (18.8 to 34.1 ms, 2 jolts), the Pentagram wipe (17.7 to 25.6 ms, 1 jolt in a frame with a GC and two elite deaths) and Level 3 (22.3 to 24 ms; 4 frames of 25-31 ms in one second with no timed section of 1 ms or more).
  - Loads differ run to run (kills, enemies), so single parts compare loosely. **Not a constant 60**: about 1% of frames still exceed 17.2 ms, as before.
- **Jolt hunt** (owner: "option 2").
  - A development build profiled Level 4, Level 3 and the wipe for 150 s each (`run-20261019-jolt-prof/`). The bot's new `-sh-autoplay-profile-keep-jolts MS` keeps only the 4 s profiler parts that hold a running frame over MS ms.
  - Level 4 and Level 3 had no frame over 30 ms.
  - The wipe's two 33 ms frames were 23 ms of `DXGI.WaitOnSwapChain` right after 420-661 ms chest hitches. Those hitches happen in the development build only; the release run has none.
  - The release wipe jolt had `PickupPool.Instantiate XP_Gem 3 ms` with a GC: the 32 prewarmed gems ran out at the wipe.
  - **Fixed:** `PickupPoolManager.TopUpGems` fills the gem pool to the VS gem cap (400 on the ground plus pooled), 8 per frame after Start. `PickupPerformancePlayModeTests.TheGemPool_FillsToTheGemCap_SoAWipeSizedBurstMakesNoNewGems` (R).
  - Release re-measure (`run-20261019-fps-gems/`):

    | Part | Jolts | Slowest frame | Slow frames |
    |---|---|---|---|
    | Wipe | 0 (was 1) | 22.7 ms (was 25.6) | 1.33% |
    | Level 4 | 0 (was 2) | 17.6 ms (was 34.1) | 1.35% |
    | Level 3 | 0 | 17.6 ms (was 24) | 0.71% |

  - The Level 4 and Level 3 jolts never reproduced, so their change cannot be credited to the gem fix (one run per part).
- Not played by the owner.

### Keyboard on the level-up and chest screens (owner report, 2026-10-18)

Owner: "keyboard se game khel raha hoon to level up selection mein keyboard kaam nahi kar raha", "or chest ko bhi fix karo". VS wiki (Options, patch 0.2.4): "Spacebar can be used just like Enter to interact with the UI".

Found:
- Each scene's UI module (InputSystemUIInputModule with its embedded DefaultInputActions) submitted on Enter only. The cards stand one above the other, so up/down and W/S move between them.
- In a release build, Down and Enter sent through the game's own Input System did work (`AutoplayBot -sh-autoplay-manual-menus -sh-autoplay-menu-keys`, `run-20261018-realkeys/menukeys`). Space did nothing.
- Space is also Dash, and a dash started while a menu paused the game, then ran the moment the menu closed.
- Measuring traps:
  - keys sent through Windows (`keybd_event`) did not reach the game;
  - in a windowless editor (PlayMode tests) the Input System gives the UI no keyboard input at all, because no Game view has focus.

Fix:
- `UiSubmitKeys` adds Space to every UI module's Submit action when a scene loads (the scenes are not edited).
- `PlayerController.OnDashEvent` ignores a dash while `GameTime.IsPaused`. In play, Space still dashes.

Validation:
- `KeyboardMenusPlayModeTests` (R): Submit answers Space and Enter, Navigate has the arrows and W/A/S/D, Space is added once, and no dash starts while paused but one does in play.
- EditMode 990/990; PlayMode fixtures 20/20 (keyboard menus, dash, accessibility, level-up screen, level-up action buttons, chest flow), 0 exceptions, save untouched (`run-20261018-space-tests`).
- Release build (`run-20261018-realkeys/menukeys`, after the fix): on all 5 level-ups, Down moved to the next card and Space picked it (closed in about 1.1 s).
- Space on the chest screen was not seen in that build (no chest was collected in 3 minutes); it uses the same Submit action.

Not yet played by the owner.

### Focus loss stopped the game (owner: "1", 2026-10-17)

The stall that the WPR hang hunt caught (`run-20261017-wprhang`, run 1) was the Unity player stopping because its window lost focus:
- Main thread in `GetMessage`, process CPU frozen, no SlowSpan open.
- `ProjectSettings` has `runInBackground: 0`.

Notepad A/B (`D:\SoulHunter-Validation\run-20261017-focus\focus-test.ps1`), with Notepad forced to the foreground at 40 s:
- the old build stopped at 40 s with the same signature;
- a build with `runInBackground: 1` played on.

Consequence: the "Pause When Unfocused" option could not really be switched off, because every alt-tab froze the game.

Fix: `DisplaySettingsApplier.Initialize` sets `Application.runInBackground = true`. `ProjectSettings.asset` was not edited, because the owner's editor was open.
- Option on: alt-tab opens the pause menu, as before.
- Option off: the run plays on.

Validation:
- `AccessibilityPlayModeTests` (R): the game runs in the background, and with the option off alt-tab does not pause.
- EditMode 989/989 and the Accessibility fixtures 7/7, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261017-runinbg/`).
- Release build of the fix: Notepad at 41 s, and the run played to its end at 143-164 FPS (`run-20261017-focus/C-fixed`).

Not proven: that the older 8-minute hang had this cause.

HWiNFO free cannot log from the command line ("supported in HWiNFO64 Pro only"), and its error window can take focus, so it is not used. Not yet played by the owner.

### Level 1 to 100%: plan and step 1 (owner, 2026-10-01)

Owner: "haan plan save karo aur step 1 se shuru karo". The plan is in `Docs/VS_DOT_Plan.md`, "Level 1 to 100%" (six steps; no date promised).

Step 1 (Bug MIS O1 and O2), done; neither needed a game change:
- **O1:** wiki Experience_Gem: past 400 gems, XP "is instead accumulated into a single red Experience Gem". `GemCapPlayModeTests.TheMergedGem_IsARedGem_AndReachingItGivesAllItsXp` (R): the merged gem shows the red tier and is drawn, and reaching it gives all its XP (several levels at once). The bot never went back to it. The wiki does not say where the red gem lies.
- **O2:** `AutoplayBot` options `-sh-autoplay-xp-burst-after S` / `-sh-autoplay-xp-burst-levels N` (a chain of N level-ups), `-sh-autoplay-levelup-hold S` (keep each screen open), and a `levelup-frames` event (open-screen frames, average FPS, worst frame). Editor play, Stage 1: with a PNG per level-up the run fell to 14 FPS (`o2-shots`); without, never under 72 (`o2-noshots`); screens held 1 s: 88 FPS average over 1,816 open-screen frames, worst 98 ms (the opening frame) (`o2-hold`). The old 8-10 FPS came from the bot's screenshots.
- Validation: EditMode 496/496; PlayMode 228/229 (`D:/SoulHunter-Validation/run-20261001-step1/`). The one failure, `ChestFlowPlayModeTests.TheChestScreen_HasItsOwnCanvas_AndSurvivesATemporaryCanvasGoingAway`, timed out waiting for the reveal with no error logged; the chest fixtures then passed 9/9 three times and 6/6 once more. Not explained: the test's timeout now reports the screen's phase, the time scale and who holds the pause. No test wrote the real save.

### Level 1 step 3: six VS evolutions and the Metaglio stage items (2026-10-01)

Owner: "step 3 shuru karo"; names chosen by the owner (AskUserQuestion), looks from the approved VFX and the base weapons' own projectiles for now (new Blender art later, for approval). Recipes from each base weapon's wiki page; stats from each evolution's page (fetched 2026-10-01). VS names are not used in the game.

| Soul Hunter evolution | VS evolution | Recipe | Built (VS numbers) | Soul Hunter's own numbers (wiki silent) |
|---|---|---|---|---|
| Hellmaw Comet | Hellfire | Fire Wand 8 + Spinach | 100 damage, 3 s, 2 meteors 0.2 s apart, speed 100%, infinite pierce, through walls, ignores Duration | 4 s flight; drawn 2x the fireball |
| Drowning Chalice | La Borra | Santa Water 8 + Attractorb | 40 damage, 4 pools in a clockwise ring, Area 200%, 4 s, 4 s cooldown, 0.3 s interval, no knockback; pools creep to the player and grow only with Speed bonuses | ring 4 units, creep 0.8/s, growth 0.5/s per 100% Speed, at most 2x |
| Severed Fate | NO FUTURE | Runetracer 8 + Armor | 30 damage, Speed 280%, 3 s, cycle Cooldown 1 s + Duration 3 s, infinite pierce, bounces off the screen edges (`ScreenBounce`), retaliation every 0.5 s with +10% per Armor | explosion = the laser's damage within 3 units x Area |
| Stormbound Halo | Thunder Loop | Lightning Ring 8 + Duplicator | 65 damage, 6 strikes, Area 4, 4.5 s, no interval; each spot struck again | repeat after 0.5 s |
| Eclipse Sigil | Gorgeous Moon | Pentagram 8 + Crown | 60 s; 3 s in, 20 beams (8 x 0.75 s, 4 x 0.375 s, 8 x 0.2 s) each erasing the closest enemy + an extra 1-XP gem; at 12.1 s erases the screen (not stage bosses or instant-kill-resistant enemies; no items destroyed) + an extra gem each; then vacuums every gem | - |
| Requiem of Souls | Mannajja | Song of Mana 8 + Skull O'Maniac | 40 damage, 2 s wave around the player, 4.5 s, 0.1 s pulses, 1 s hitbox delay, ignores Amount and Speed; each hit slows the enemy 5% more, down to 20% (`EnemyController.ApplyLingeringSlow`, one life; not debuff-resistant enemies) | wave radius 6 units x Area; the look is the approved magic circle every 0.5 s |

- `EvolutionBuilder` (menu Soul Hunter/Content/Build Evolutions) writes the prefabs (`Assets/Prefabs/Weapons/Generated/<Name>.prefab`), the `Evo_<Name>` WeaponEvolutionData and `GameContentCatalog.Evolutions`; `WeaponManager.Awake` adds those to the scene's list, so `Main_Gameplay` is not edited.
- Shadow Kael: Stormbound Halo strikes the player; Eclipse Sigil shows only its glyph; Severed Fate does not retaliate. Not checked in a Stage 10 fight.
- **Metaglio Left/Right** (wiki Metaglio_Left / _Right): "cannot be obtained from level ups, but can be found as a stage item in every stage", Left west and Right east of the start (`StageItems.Plan`, `MetaglioDistance` 45, Soul Hunter's); level-ups never offer them (`UpgradeRarity.IsStageItemOnly`); a chest can still level a found one. The VS guards (City and Volcano Atlanteans) are not built. Picking one up used to pay the fallback gold instead (the pickup went through `ApplyUpgrade`, which refuses a stage passive at level 0); it now uses `WeaponManager.AcquireStagePassive`. Crimson Shroud (Laurel + both Metaglios) can now be reached.
- Tests: `NewEvolutionsPlayModeTests` (R: each evolves from its recipe in a fresh run and replaces its base weapon), `NewEvolutionRulesPlayModeTests` (R: Stormbound twice per spot; Severed Fate retaliation and Armor; Requiem slow and floor; Drowning Chalice creep; Eclipse Sigil's three phases), `EvolvedWeaponSweepPlayModeTests` (R: every evolution hurts enemies), `StageItemTests` (T) and `StageItemPlayModeTests` (R).
- Found while testing: the first rules test waited on game time and hung 30 minutes when a level-up paused the game (test-side; its waits now answer level-ups); a single run evolving all six filled the six weapon slots (test-side; one run per evolution).
- `StageHazardPlayModeTests.SoulMeteor...` read 0 damage once more in a full run (not alone); clearing invulnerability did not explain it, so its failure message now reports how far the player stood from the marker. Not yet explained.
- Validation: EditMode 520/520, PlayMode 249/249, no test wrote the real save (`D:/SoulHunter-Validation/run-20261001-step3b/`).
- Editor play with `AutoplayBot -sh-autoplay-evolve "Evo_A,Evo_B"` (new: starts the run with those evolutions), Stage 1, god mode, 90 s each: `evo-play-a` (Hellmaw Comet, Drowning Chalice, Severed Fate) and `evo-play-b` (Stormbound Halo, Eclipse Sigil, Requiem of Souls): all six evolved, 0 errors, 0 hitches; in play 60-86 FPS and 78-98 FPS (the 29 / 75 minimum rows are the loading screen). Screenshots show the meteors, the creeping pools and the double strikes (partial `V`). Not yet played by the owner.

### Level 1 step 4: the VS Results screen (owner: "VS_DOT_Plan ka Level 1 step 4 shuru karo", 2026-10-02)

vampire.survivors.wiki/w/Results (raw, fetched 2026-10-02): the screen "appears at the end of each run"; top left the stage name, mode and gold bonus with time survived, gold earned, level reached and enemies defeated; bottom left each weapon's level, damage, time used and DPS, with the most damage and the most DPS in yellow and damage written with SI prefixes up to E; top right the character and the Golden Eggs used; then every weapon and passive picked up with its level (yellow when max), the PowerUps active and their level, the pickups and how many, and the Arcanas; a "DONE" button to the main menu; a green button to show or hide the run's unlocks; v0.2.2: "quitting from the Pause menu doesn't skip the score page". Treasure_Chest: the pickups section shows chests by size (silver 3, gold 5; marked "verify" on the wiki).

Built:
- **Every hit is credited to its weapon** (`DamageSource`): a weapon, projectile or zone opens a scope around its `TakeDamage`, and `HealthController` records the damage an enemy takes in `RunStatsTracker.RecordHit`. `AutoAttackWeapon.AimAtHostiles` stamps the source on every spawned `Projectile`, `ProjectileDamage`, `TouchDamage` and `DamageCaster`; Whip, Garlic, Requiem of Souls, Severed Fate and Crimson Shroud hit through `AutoAttackWeapon.Hit`; Soul Burst and rail carts are named sources. Before, only some weapons recorded damage (by hard-coded name), Whip and Garlic recorded none, and overkill was cut off.
  - The full hit counts, overkill included (the wiki gives no rule). Shadow Kael's mirrored weapons are left out (`AutoAttackWeapon.IsHeldByEnemy`). Pentagram and Eclipse Sigil credit each erased enemy's max health (2026-10-03, wiki Pentagram and Gorgeous_Moon: they "always deal damage equal to the max health of the enemy"; a number under 660 is shown as 655-665; `HealthController.EraseByInstantKill`). Soul Toll is a pickup and has no row (the Rosary page gives no damage).
- **Rows and time used** (`RunStatsTracker.WeaponRecord`): `WeaponManager` starts a row when a weapon, evolution or union is spawned (name, icon, max level); time used is game time while the weapon is held and on, so a replaced base weapon stops counting. DPS = damage / time used. A ten-stage run shows its whole run time (`h:mm:ss` past an hour).
- **Items, pickups, unlocks:** the tracker follows `WeaponManager.OnInventoryChanged` (each item at the highest level it reached); every `Pickup`, the chicken and chests are counted under Soul Hunter's own names (`PickupNames`: Soul Coin, Soul Coin Bag, Rich Soul Coin Bag, Roast Chicken, Soul Magnet, Time Freeze, Soul Reliquary / x3 / x5, Arcana Reliquary, plus Soul Toll, Hellfire Breath, Luck Charm, Gilded Charm); experience gems are not listed (wiki silent). `CharacterUnlockTracker` records character and PowerUp unlocks.
- **The screen** (`RunResultsScreen`, opened by `GameOverUI` on death, victory and quit; `RunResults.Capture` gathers the data): its own overlay canvas (Expand 1920x1080, sorting 450), an opaque backdrop, the four VS areas, the yellow rules, SHOW/HIDE UNLOCKS (green) and DONE (focused) to the main menu. Victory keeps the title "SHADOW KAEL DESTROYED" and the GDD's last line. Layout and colours are Soul Hunter's; no mobile ad button.
- **Quit:** the pause menu's Quit ends the run (`GameSessionManager.QuitRun`, `IsQuit`) and shows the Results; DONE goes to the menu. The old text panel and its Restart button are hidden (VS has only DONE); the scene is not edited.
- `AutoplayBot -sh-autoplay-results-shot`: at the time limit it quits through the pause menu, screenshots the Results and logs a `results` event with the weapon table and pickups.

Validation:
- `RunResultsTests` (T: SI prefixes to E, DPS, time, yellow rows, max, tracker counts, pickup names); `RunResultsPlayModeTests` (R: the Whip's real hits credited to it with time used and DPS; a later Knife has its own clock; a boss-held weapon gets no row; Soul Burst a named row; coins, bag and chicken counted; the screen shows stage, mode, character, PowerUps, pickups, max levels and top damage/DPS in yellow, "5.00k", and the unlocks toggle); `GameOverFlowPlayModeTests` rewritten (R: death shows the paused Results and DONE loads the main menu, a second run from the menu works; pause Quit shows the Results first; the backdrop is opaque). `PlayerLoadoutPlayModeTests` reads Song of Mana's row.
- EditMode 547/547, PlayMode 263/263, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261002-results-full/`). Then the backdrop was made opaque and the bot option added: EditMode 547/547 and the four related fixtures 8/8 (`-results4/`). One EditMode run failed `EnemyCrowdTests` timing once (1.63 ms against 1.5); it passed in every other run.
- **GUI editor play was not possible:** this session runs elevated and Unity stops at its "running as administrator" dialog (also through `runas /trustlevel`). Played instead in a Windows player build (`D:/SoulHunter-Validation/run-20261002-results-build/`, 142 MB; `D:/Unity Builds/SoulHunter` untouched): `quit` (Stage 1, level 20, god mode, Hellmaw Comet, 3 min) and `quit2` ended through Quit into the Results; `death` (Stage 10 at level 1) died at 1:41 into "GAME OVER". 0 errors, min 43-60 FPS, save untouched. Screenshots show every area (partial `V`); the first build showed the game through the 0.97 backdrop, fixed and rechecked in `quit2`.
- Instant-kill damage (2026-10-03): `RunResultsTests.AnInstantKill_ShowsItsDamage_Or655To665WhenUnder660` (T) and `RunResultsPlayModeTests.PentagramErase_CreditsEachEnemysMaxHealth` (R: three 10-HP enemies erased by a real Pentagram give it exactly 30). EditMode 548/548, PlayMode 264/264, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261003-instantkill-full/`). The first full run stalled for an hour in `BibleCyclePlayModeTests` after a level-up and was stopped; that fixture then passed alone, and the rerun passed in full. The stall is not explained.
- Seen, not changed: a base weapon evolved at once (the bot's `-evolve`) shows a row with 0 damage and 00:00; how VS lists replaced weapons is not on the wiki.

Not yet played by the owner.

### Level 1 step 5: VS Mad Forest enemy types (owner, 2026-10-03)

**O4 (Pentagram erasing chest elites) needs no decision:** vampire.survivors.wiki/w/Pentagram (tips): "if a boss that would have dropped one is erased, the Treasure Chest is then dropped by a different enemy", which is what `EnemyDrop.PendingChests` already does.

**O3 (Level 1 too easy).** Cause: Level 1 had one enemy (10 HP, 5 damage) for all 30 minutes, while VS Mad Forest changes its enemies minute by minute (raw Mad_Forest "Waves" table and each enemy's statbox, fetched 2026-10-03). The owner chose "VS stats, existing models" (AskUserQuestion).
- `MadForestEnemies` (Data/Config): 16 types with the wiki's health, damage, move speed (VS x `EnemyAbilityDefaults.VsSpeedToUnits`, VS 100 = 3 units/s), knockback and its cap, and XP x 10 (Soul Hunter's gem scale). "HP x Level" (wiki skill on Pipeestrello 4/5 and Flower Wall) multiplies health by the player's level (`EnemySpawner`, `EnemyData.HealthPerPlayerLevel`). Names, tints and sizes are Soul Hunter's own:

  | Soul Hunter | VS variant | HP | Damage | Speed | Knockback | XP |
  |---|---|---|---|---|---|---|
  | Grave Bat | Pipeestrello 1 | 1 | 5 | 140 | 1 | 10 |
  | Red-Eye Bat / Blood-Eye Bat | Pipeestrello 2 / 3 | 5 | 5 | 140 | 1 | 10 |
  | Ember Bat / Ashen Bat | Pipeestrello 4 / 5 | 50 x level | 10 | 140 | 1 | 300 |
  | Restless Corpse | Zombie | 10 | 10 | 100 | 0.8 | 10 |
  | Bone Soldier | Skeleton | 15 | 10 | 100 | 1 | 20 |
  | Grave Wisp | Ghost 1 | 10 | 5 | 200 | 0 | 15 |
  | Ash / Moss Mire Fiend | Mudman 1 / 2 | 70 / 150 | 10 | 100 | 0.3 | 25 |
  | Great Grave Bat | Giant Bat | 270 | 10 | 140 | 0.1 | 25 |
  | Ghoul Hound | Werewolf | 180 | 14 | 130 | 0.8 | 20 |
  | Mantis Wraith | Mantichana | 500 | 20 | 80 | 0 | 30 |
  | Shroud Giant | Big Mummy | 500 | 20 | 80 | 0 | 30 |
  | Thorn Wall | Flower Wall | 30 x level | 1 | 20 | 1 | 20 |
  | Grave Lily | Venus | 500 | 20 | 80 | 0 | 30 |

- `MadForestEnemies.ByMinute` lists each minute's types as the wiki table does; `WaveTables.EnemyTypesByStage[1]` uses it, so the spawner fills each minute's VS minimum with those types and adds one of each per tick (the existing wave-type rule).
- `MadForestEnemyBuilder` (menu Soul Hunter/Content/Build Level 1 Enemy Types) writes `Assets/_Project/Data/Config/Enemies/MadForest/L01_<Type>_Data.asset` and `Assets/Prefabs/Enemies/Gameplay/MadForest/SH10_L01_<Type>_Gameplay.prefab` (variants of the Level 1 enemy or elite gameplay prefab, resized) and rebuilds the wave tables; only `Graveyard Wave.asset` changed. Run in the isolated copy and copied back.
- `EnemyData.VisualTint` / `EnemyController.TypeTint`: each type is drawn in its own colour through `DamageFlash.SetTint`; Defang's green still wins while defanged.
- **Look, not owner-approved:** the tint paints the whole model one flat colour, so the types read as pastel silhouettes, and the greens are weak on the graveyard grass. Own models per type are Art section work.

Found while testing: Level 1 now opens with 1-5 HP bats, so the starting weapon levels the hero during a test's boot; tests that wait on game time then hung behind the paused level-up screen (`BibleCyclePlayModeTests` stalled two full runs for over an hour; Duplicator cases timed out; the Dash check saw the pick's 240 ms invulnerability). `GameplayPlayModeTestBase.CloseOpenLevelUps` takes the first card of any screen left open after boot; the Bible and Duplicator tests also clear the opening horde (and gems). `D:\SoulHunter-Validation\results-run.ps1` now stops a PlayMode run whose log stops growing for 10 minutes, but only a Unity whose command line has `-runTests` and the isolated copy's path.

Mistake (2026-10-03): while the first stalled run was being cleaned up, the owner's open editor (PID 4388, canonical project, Hub-launched) was stopped by mistake; its command line was printed and the process stopped in one command. Unsaved editor work since the last save was lost; no backup scenes existed. The owner reopened the editor.

Validation:
- `MadForestEnemyTests` (T: each type's wiki stats, HP x Level holders, the minute table, the built assets, the wave asset's types per minute) and `MadForestEnemyPlayModeTests` (R: at 12:00 Ghoul Hounds spawn with 180 HP, 14 damage, speed 130 and their tint; at 28:00 Ember Bats have 50 x the player's level). `KnockbackTests`, `PickupV2Tests` and `WaveEnemyTypeTests` were adjusted for per-type values.
- EditMode 566/566, PlayMode 266/266, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261003-madforest-full4/`).
- Build play (`run-20261003-madforest-build/level1-30min`, Stage 1, no god mode): **the bot died at 11:32**, level 11, 2,944 kills, hurt 20 times (the old 30-minute run: hurt 7 times, level 123), 0 errors, min 57 FPS. From 4:00 the ground held the 400-gem cap: the bot flees the tougher horde and leaves its XP, so this run cannot judge VS balance. Needs the owner's play, or a bot that collects gems.

Played by the owner after this change (2026-10-03): "level 1 khel kar dekha thik hai".

### One dark UI theme for every panel (owner: "all panel jo bache huye hai sab ko pura advance or pura contrast or dark banao", 2026-10-04)

Before: each panel had its own colours (navy buttons and cards from `OptionsMenuUI.ApplyFocusColors`, a bright blue FILTER button, green stats labels, grey Power Up cells, nearly invisible Buy/Refund), and the pause menu had no veil over the game.
- `UITheme` (UI/HUD): near-black panels with a crimson edge, dark crimson buttons and cards with a red edge (Primary a brighter crimson), warm white text, gold for highlights and changed values, the game-wide orange for focus (Bug MIS B16 kept), a dark disabled colour (B17 kept), instant focus (B13 kept), and a black veil (0.9: the UI blends in linear space, so 0.74 only halved the brightness behind).
- `OptionsMenuUI.ApplyFocusColors` now applies the theme, so every button and card that used it changed at once.
- Applied to: Options (veil, panel, rows, text), pause menu (veil, crimson buttons, Resume primary, gold title), stats panel, level-up screen (veil, card text, gold tags), Power Up (backing panel, crimson cells, selected crimson-orange with a gold edge, maxed dark gold, Buy primary, Refund), character select (theme set in code over the scene's serialized colours; panels, cards with a gold edge on the selected one, FILTER, tick boxes, Confirm primary, white stats, gold title), main menu Back buttons and `MainMenuLayout` colours, chest screen (OPEN/DONE primary, item frames, black backdrop), Results screen (panels with edges, gold title, dim headers), the merchant shop (veil, panel, wares).
- `AutoplayBot -sh-autoplay-options-shot`: the Options screen over the pause menu.
- Test setup: `GameplayPlayModeTestBase.LevelUpUnstick` takes the first card of a level-up screen left open for 5 real seconds (`AutoCloseStuckLevelUps`, default on). Tests that wait on game time stalled whole runs behind an unanswered level-up three times (2026-10-02 to 2026-10-04); in the next full run it never had to act. `WeaponDurationRulesPlayModeTests` also clears the opening horde and gems.
- Validation: the UI fixtures 41/41; full run EditMode 566/566, PlayMode 285/286, the one failure `WeaponSweepPlayModeTests` expecting pure white action labels (now the theme's warm white), which then passed 21/21 alone. 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261004-theme-full2/`, `-theme-sweep/`).
- Build screenshots (`run-20261004-theme-build/panels`, `panels2`, 1366x768): Power Up, character select, pause with stats, Options, level-up and Results all dark with crimson edges and gold highlights; the veil darkens the game behind pause and level-up (partial `V`). The chest screen and the merchant shop were not captured (the bot did not open a chest in that run; the merchant is on Level 3).

Not yet seen by the owner.

### One UI theme, controlled by designers (owner, 2026-10-04)

Owner: "pure game ka single theme system banao, ... source of truth single hona chahiye, hard wiring nahi hona chahiye, koi parallel implementation nahi chahiye, alignment thik karo, theme system is supposed to be controlled by designer and layman persons", then "dono skill install karo or ise use karna mandatory hai" (ui-ux-pro-max and frontend-design; installed as plugins at user scope).
- **The source of truth:** `UiThemeAsset` (UI/HUD/Theme, Create > Soul Hunter > UI Theme), named by `GameContentCatalog.UiTheme`. It holds every UI colour (surfaces, buttons, cards, text, rarity, HUD, rewards), the button/card/panel pictures (9-sliced), the button icons, optional fonts, motion switches and the icon/label layout, each with a plain-language tooltip. `UITheme` is now only a facade over the active theme; no UI script keeps colours of its own (`UiThemeTests.UiCode_KeepsNoColoursOfItsOwn` fails on any hued colour literal under UI/, the merchant and the unlock notice, and on the removed second path `OptionsMenuUI.ApplyFocusColors`).
- **Live:** `ThemedElement` (added by `UITheme.ApplyButton/ApplyPanel/ApplyText/ApplySurface/AddVeil`) records what an element is (a button kind, a text role, a surface role, an icon slot) and restyles it on every `UiThemeAsset.Changed`, also in play; icons are read from the theme by slot each time. Snapshot copies (`LevelUpUI.DimColor`, `InventoryHud` frames, `HudBacking.Color`, `RunResultsScreen.Yellow`, the menu and Power Up cell colours) are now properties.
- **Moved into the theme:** Options, pause, stats panel, level-up, Power Up, character select, main menu, Results, chest screen and preview glow, merchant, HUD labels and backings, health bar, item slots, story card, unlock notice, touch controls.
- **Alignment:** a themed button shows its icon on the left, vertically centred, and centres its label in the space beside it (or across the whole button, a Theme Studio switch); both are placed again whenever the button's size changes.
- **Motion** (`ThemedMotion`, real time): following ui-ux-pro-max ("animate 1-2 key elements per view", "visible focus on every control", "respect reduced motion") and frontend-design ("spend boldness in one place"), only the chosen button breathes, gets the light sweep, moves its icon and wears a focus ring (`ThemeFocusRing`, its own Outline type so it never clashes with the edge line); every button lifts under the mouse and squashes when pressed; title bounce is off by default; with the player's Flashing VFX option off nothing moves on its own (the ring stays).
- **Art:** `ArtSource/Generators/build_ui_theme_art.py` renders 9 icons (512 px: soul flame, soul gem, gear, scythe, moon, back, check, play, coin) and 4 white plates (button, card, panel, shine) to `Assets/Art/UI/Theme` through Edge headless; their own art, **not yet owner-approved**.
- **Presets** (`UiThemeBuilder`, menu Soul Hunter/Content/Build UI Themes; creates only missing ones): Soul Candy (the default; B1 of the samples the owner saw), Moonlit Reaper (B2), Ember Pact (B3) and Classic Crimson (the 2026-10-04 dark crimson look, flat). The ui-ux-pro-max design-system search named "Claymorphism" for a casual game, which Soul Candy is; frontend-design notes that Classic Crimson is the "near-black with one vermilion accent" default, so it stays a preset, not the default.
- **Theme Studio** (menu Soul Hunter/Theme Studio): look cards with swatches, "Use in game" and "New look" (a copy); pages Colours / Pictures & icons / Motion / Text & spacing in plain words; a live preview drawn with the theme's own pictures; a readability check (text on windows, cards, HUD, unusable buttons, buttons with outline, focus distinct); Ctrl+Z undo and auto-save. Not opened by eye yet: this session runs elevated and the GUI editor stops at its admin dialog.
- Found by the tests: an icon change did not reach open buttons (they kept a sprite copy); unusable buttons were too light for their grey text (2.9:1, now at least 3:1 in every preset); the theme raised FILTER's text from 20 to 36 (the theme size is now only a ceiling).
- Tests: `UiThemeTests` (T: active theme with icons and 9-sliced plates, every preset readable, no colour literals or second path, a theme change restyles elements at once) and `UiThemeLivePlayModeTests` (R: editing the theme with the pause menu open recolours its buttons, focus, icon, veil and the HUD backing at once; only the chosen button moves and wears the ring, and Flashing VFX off stops the motion but not the ring); `MainMenuLayoutPlayModeTests` checks icon and label alignment; `LevelUpScreenPlayModeTests`, `RunResultsPlayModeTests` read theme colours.
- EditMode 570/570, PlayMode 288/288, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261004-theme2/`). The run before (282/287) found the defects above.
- Follow-up from the build screenshots (`run-20261004-theme2-build/panels`), 2026-10-06:
  - a button's label picks light or dark text by contrast against its colour (`UITheme.LightTextReads`), with an outline only on light text;
  - the "<- Back" arrow goes when the Back icon shows;
  - locked cards are darker in every preset, and Ember Pact's danger colour is darker;
  - in Options, the open tab uses the Primary button kind;
  - Reroll/Skip/Banish labels are readable (`UiThemeTests`, `WeaponSweepPlayModeTests`).
  - Validation: EditMode 570/570, PlayMode 286/288, save untouched (`D:/SoulHunter-Validation/run-20261006-theme4/`). The two failures passed when rerun twice (20/20 each, `-theme4-rerun1/`, `-rerun2/`):
    - `AutoplayBotPlayModeTests` (its shots folder was missing when a screenshot was written);
    - `DuplicatorEverywherePlayModeTests` Thousand Edge (the 1 s window caught 3 volleys without the Duplicator; the cooldown is 0.35 s).
    - Both are flaky tests, not caused by this change.
- **Default look: Ember Pact** (owner, 2026-10-07: "Ember Pact"), chosen from a side-by-side comparison of three Windows builds that differed only in the theme (7 screens each, 0 errors; `D:/SoulHunter-Validation/run-20261006-looks/looks.html`).
  - `GameContent.UiTheme` now names `UiTheme_EmberPact`, and `UiThemeBuilder` picks it when no theme is set.
  - Soul Candy, Moonlit Reaper and Classic Crimson stay available in Theme Studio.
  - `UiThemeTests.TheDefaultLook_IsEmberPact_TheOwnersChoice` (T).
  - EditMode 571/571, PlayMode 288/288, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261007-ember/`).
- **One standard size per kind of control** (owner, 2026-10-07, from a character select screenshot: "teeno fix karo ... button size sabka ek jaisa banao bada nahi standered ... tab ka size sahi karo pure project mein ek hee standered banao double nahi"):
  - Theme Studio, Text & spacing (`UiThemeAsset.Layout`):
    - every action button is 300x68: Start, Power Up, Options, Quit, Back, Confirm, Buy, Refund, Resume, OPEN, DONE, Reroll/Skip/Banish, Leave and Reset;
    - every tab or toggle chip is 150x54: the Options sections, the six run modes and FILTER;
    - tab text max 24, gap 12, tick box 0.5 of the height.
    - Units are the 1920x1080 layout's. ui-ux-pro-max: one size per control type, at least 8 between targets.
  - `ThemedElement.SizeRole` (Standard / Tab / Free) sets the size. A button keeps the centre its screen gave it; inside a layout group it asks the group for the size.
  - Free (the screen's own size) is for cards, the slider handle, controls inside Options rows and the merchant's two-line wares.
  - A screen that places its buttons again gets the size back at the end of the frame. The main menu sets anchors one at a time, and reacting to each change gave wrong centres.
  - `ThemedRow`: a row of standard controls, centred with the gap. It shrinks as one piece when a narrow window cannot fit it. Used for the run modes and for Buy/Refund.
  - `MainMenuLayout.PlaceButtons` stacks the theme buttons with the gap. The chest screen and the level-up bar read the theme size; the hard-wired 340x96 and the stretched Reroll/Skip/Banish are gone.
  - Labels:
    - a label with only the plain text role takes its button's light-or-dark choice. White on orange was 1.4-2.2:1; now every label on a standard button or tab reaches 4.5:1. This affected the run modes, the Options tabs, Back and the ON/OFF switches.
    - Labels stay on one line and shrink to fit, down to 12.
    - A button without an icon pads its label evenly on both sides.
  - The three screenshot fixes:
    - the mode chips are tab size, with the tick box in the icon's place and the label beside it;
    - card names are bold and padded (dark text at card size read grey);
    - the chosen card has a frame from the theme (`Cards.SelectedEdge`, `SelectedEdgeWidth` 5).
  - Also: Options slider values sit inside their row, clear of the handle.
  - `AutoplayBot -sh-autoplay-select-shot` logs every button's size. In the build: Confirm and Back 300x68, FILTER 150x54, the mode chips 150x54, shown at 139x50 in the shrunk row.
  - Tests: `UiStandardSizePlayModeTests` (R). On the main menu, character select, Power Up, pause, Options and level-up, every action button and tab has the theme's size. No two overlap, every label reads at 4.5:1 and stays inside its button. Plus the character select fixes.
  - Validation:
    - EditMode 571/571, PlayMode 290/290, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261007-sizes-full3/`).
    - Then even label padding was added and the UI fixtures were rerun (`-sizes10/`).
    - Earlier full runs failed once each in `LightSourcePlayModeTests`, `NewEvolutionRulesPlayModeTests`, `PlayerPresentationPlayModeTests` and the known `StageHazardPlayModeTests` Soul Meteor test. Each passed alone, and the final full run passed.
  - Build screenshots, Ember Pact (`run-20261007-sizes-build4/ember-pact/play/shots`, 1366x768): 0 errors (partial `V`).
- **Power Up shop redesign** (owner, 2026-10-07: "haan sab fix karo, icons bhi banao, skil through madad lo", after a screenshot review of nine problems; ui-ux-pro-max: state never by colour alone, locked items say why, 4.5:1 text, one icon size and style; frontend-design: one highlight per screen):
  - Header inside the screen's backing: Back on the left (it stuck out over the panel's top edge), a "Power Up" title, and the gold with its coin on the right.
  - The grid fills the space above the info box (`GridFit.MinHeightRatio` 0 here; four rows used to run under the box).
  - Each cell has one icon slot (64), a bold name, then the rank, "MAX", or "LOCKED" with what unlocks it (`PowerUpUnlocks.NextConditionText`). A locked cell has its icon faded and readable light text. Every cell's text reaches 4.5:1 on its own colour.
  - The chosen cell keeps its state's colour and wears the theme's frame (`Cards.SelectedEdge`, as the chosen character card).
  - The info box (a themed Row surface): the chosen PowerUp's big icon, its name and rank, what it does, the price with a coin (gold, or red with "need N more gold"), then Buy and "Refund All" in a `ThemedRow`.
  - **Icons** (Soul Hunter's own; not yet owner-approved): `ArtSource/Generators/build_powerup_icons.py` draws 9 badges in the round item-icon style (`build_item_icons.badge`):
    - one warm-gold filled glyph each, only the rim colour differs;
    - Area (rings spreading out; the Candelabrador candlestick read too small), Greed (coins), Omni (star with four sparks), Charm (bell), Defang (a whole and a snapped fang), Reroll (two chasing arrows), Skip (double chevron), Banish (card behind a forbidding ring) and Preserve (hourglass).
    - `PowerUpIconImporter` (menu Soul Hunter/Content/Import Power Up Icons) imports them and lists them in `GameContent.PowerUpIcons` (`GameContentCatalog.PowerUpIconFor`).
    - `PowerUpMenuUI.IconOf` uses them first, else the passive item's icon.
  - Tests: `PowerUpShopLookPlayModeTests` (R):
    - every PowerUp has an icon in the 64 slot;
    - Back is inside the backing and clear of the grid and the title; the title and the gold coin are shown;
    - only the chosen cell has the theme's frame;
    - MAX and LOCKED with the unlock condition appear in words;
    - every cell reads at 4.5:1;
    - too little gold says "need" and Buy is off.
  - The shop, unlock and heal tests kept their checks ("Price: 200", "MAX", "LOCKED", "Unlock").
  - EditMode 571/571, PlayMode 291/291, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261007-shop-full/`). Build screenshot `run-20261007-shop-build2/ember-pact/play/shots/000_powerups.png`, 0 errors (partial `V`).
- **Full HD item icons** (owner, 2026-10-07: "without circle ... jo image dikha rahe ho change mat karna", "hd ilustration model"):
  - The 30 old item icons (`Assets/Art/UI/sprite_*.png`) were upscaled to 512 px with Real-ESRGAN (realesrgan-x4plus-anime, the owner's choice) and imported uncompressed. The pictures are unchanged; the originals are in `ArtSource/SH10_OldIcons_Original`.
  - Their empty margin was trimmed, so every icon fills its slot the same way (review, 2026-10-08).
  - The 9 Power Up badges and Bible, Cross, Duplicator and Spellbinder have no circle either (owner, 2026-10-08: "circle wale icons bhi bina circle ke banao"; the new sample was approved). Built by `ArtSource/Generators/build_icons_free.py`, 512 px, uncompressed:
    - the Power Up icons keep their own gold pictures, with a dark outline and a soft shadow instead of the disc;
    - Cross and Bible are their models with a glow;
    - Duplicator and Spellbinder are the approved VFX sprites on a dark halo;
    - empty space is trimmed so they fill their slot like the HD icons.
    - A first try re-painted the glyphs in the old rim colours; it washed them out and broke Greed and Banish, so it was dropped.
    - `build_item_icons.py` and `build_powerup_icons.py` would write the circle versions back, and are marked superseded.
    - Validation: EditMode 572/572 and the shop, icon, level-up and Results fixtures 26/26, save untouched (`run-20261008-freeicons`). Build screenshot `run-20261008-freeicons-screens2/ember-pro/play/shots/000_powerups.png`, 0 errors (partial `V`).
- **Ember Pact Pro, the game's look** (owner, 2026-10-08: "pro type ... brightness or contrast full ... text bilkul sharp ... icon full hd", then a style sample, "ok pass now apply it in project every where but safely, mismatch is not allow"):
  - Built first as one sample screen with every UI item (`UiStyleSampleScreen`, `AutoplayBot -sh-autoplay-style-sample`), reviewed and fixed four times, then approved. Then applied everywhere and approved from the build screenshots of every screen (`D:/SoulHunter-Validation/ui_pro_all_screens.png`, "approved").
  - `UiProThemeBuilder` (menu Soul Hunter/Content/Build Pro Look) makes `UiTheme_EmberPactPro` from Ember Pact and sets it as `GameContent.UiTheme` (and `UiThemePreview`). The other looks stay unchanged in Theme Studio.
  - Fonts: Cinzel ExtraBold titles and Barlow Semi Condensed Bold text, both SIL OFL (`Assets/Art/Fonts`, `CREDITS.md`). They use sharp SDF assets (2048 atlas, 90 pt) and three raised-text materials: a title gradient with outline and shadow, light text with a dark outline and shadow, and dark text with a light emboss.
  - Fake bold is off: the fonts' bold spacing and weight are 0, the theme's `NoFakeBold` is on, and every rich-text bold goes through `UITheme.Bold`. Fake bold had made labels wide and soft.
  - Pictures (`ArtSource/Generators/build_ui_pro_plates.py`): 3D button, card and panel plates; bevelled rows (rows, info boxes, HUD plates, slots); a slider groove and round knob; a switch track; a tick box and tick; one-colour button icons that take the label's colour (`IconsLikeLabel`). Coins next to gold amounts stay in colour (`IconSlot.Coin`).
  - Shared controls in `ThemedElement`:
    - Options ON/OFF buttons are switches (`IsSwitch`): green ON or dark OFF, with a sliding knob and the word. A chosen switch keeps its colour and the gold ring marks it.
    - Sliders have a round knob.
    - Run-mode and level-filter tick boxes have a gold tick; the label starts just after the box.
    - The open tab has a gold bar under it (not shown by colour alone).
  - Owner fixes from the reviews:
    - Every button looks the same size: no breathing on the chosen one, a thin ring instead.
    - An unusable button (Buy without gold) is a stone plate with light text (`ShowUsable`).
    - Windows are opaque.
    - Selection is shown by the gold frame only.
    - The light streak was removed.
  - Also fixed for the look:
    - Character select: run modes in two rows of three, and the name and description in the theme's font.
    - Results: no colour written into its text; it uses the theme.
    - Chest: the title is in the title style (`TextRole.TitleArcana` for Arcana chests).
    - Options: row buttons use the row text size.
  - Guards (`UiThemeTests`):
    - The game's look has every picture and rule above.
    - UI code holds no `<color=#...>` and no `<b>` outside `UITheme`.
  - Validation:
    - EditMode 572/572, PlayMode 291/291, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261008-apply-full2/`).
    - The chest title fix after that: chest fixtures 12/12 (`-apply6`).
    - The first full run (`-apply-full`) failed four tests:
      - mode labels lost their shared size (a first-frame restyle; fixed);
      - two checks written for the old look, fake bold and a dark sold-out plate (they now check the heavy font and a readable 3:1 sold-out text);
      - the Pentagram chest test once, which passed on rerun.
  - Build screenshots of every screen (`run-20261008-pro-screens6`, `-7`), 0 errors (partial `V`). The Wandering Merchant shop was not captured.
- **The rest of the panels in the Pro look** (owner, 2026-10-08: "change all rest panel with my approval, and show me", then "approved now apply safely no mismatch"; before/after sheet `D:/SoulHunter-Validation/ui_pro_rest_panels.png`):
  - HUD (`UiProThemeBuilder` HUD colours):
    - the Kills, Gold, Time and stage plates are bevelled ember plates;
    - the item slots are bevelled (`InventoryHud` uses the slot roles);
    - the XP bar sits in the groove with the theme's `Hud.XpFill` (`XPBarUI.StyleBar`, `SurfaceRole.XpFill`), and the HP bar's back is the groove too;
    - the unlock notice uses the same plate.
  - Story card: an ember plate, with the title in the title look.
  - Wandering Merchant shop:
    - the gold is shown with the coin;
    - each ware is a card like the Power Up cells (`WanderingMerchant.WareCard`): item icon, name, what it does, and the price with the coin instead of "G". OWNED / NO FREE SLOT stay words, and are the card's first text;
    - unusable wares are the theme's stone plate (`ShowUsable`);
    - every shop text uses the theme's font (the gold line used the scene default).
  - New art, owner-approved: the Soul Egg icon (`SH10_Icon_SoulEgg`, `build_icons_free.soul_egg`, `GameContent.SoulEggIcon`), a gold egg with a crack and sparks in the gold-icon style.
  - Already matching, unchanged: Game Over (the Results screen), chest, level-up. The loading screen is a picture only.
  - Validation: EditMode 572/572 and the merchant, accessibility, HUD, story card and inventory fixtures 29/29, save untouched (`run-20261008-rest2`). Full run: EditMode 572/572, PlayMode 291/291, 0 exceptions, save untouched (`run-20261008-rest-full`). Build screenshots `run-20261008-rest-screens`, `-screens2`, 0 errors (partial `V`).
- **Main menu, owner review** (2026-10-08/09, from the owner's screenshots; "approved, please apply"):
  - HD logo: the two logo sprites (85x45 and 130x46, stretched about 3x) were upscaled 4x with the illustration model and joined into one picture with a tight word gap (`ArtSource/Generators/build_logo_hd.py`, `Assets/Art/UI/SH10_Logo.png`, `GameContent.MenuLogo`).
  - `MainMenuLayout` lays the logo and the four buttons out as one group in the middle of the frame's open area:
    - a gap of 3 x the button gap between the logo and Start;
    - the group shrinks as one piece in a short window;
    - the old text logo shows only when no `MenuLogo` is set.
  - "Power UP" became "Power Up", as the shop writes it.
  - A clearer Power Up button icon: a faceted gem (`mono_power`).
  - Every button keeps exactly its size and gap, on every screen:
    - the focus ring is drawn inside the button's edge (`Shape.FocusFrame`, `pro_focus`; an outline outside made Start look bigger);
    - no breathing and no hover growth;
    - the chosen button keeps its own colour, a little brighter (`Motions.FocusKeepsColour`, `UITheme.SelectedColour`), instead of turning pale;
    - the mouse chooses the button it points at, so only one button is ever lit;
    - the button gap is 16.
  - A poster hero above the frame was tried (a 3D model render, then a 2D hunter from the concept sheet). The owner rejected both ("iski jarurat nahi hai, old bala thhik tha"), and it was removed entirely.
  - Tests: `MainMenuLayoutPlayModeTests` (R) also checks that the logo is sharp (not stretched past its pixels), centred and clear of Start, that "Power Up" is spelled right, and that the four buttons are the same size with equal gaps. `UiThemeLivePlayModeTests` accepts the inside ring. `UiThemeTests` guards no hover growth.
  - Build screenshot `D:/SoulHunter-Validation/ui_mainmenu_sample3.png` (`run-20261009-menu-shots5`): all four buttons measured 213 px wide, 0 errors (partial `V`).
  - Validation: EditMode 572/572, PlayMode 290/291, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261009-menu-full2/`). The one failure, `PentagramPlayModeTests.Bosses_AreImmune_AndAnErasedEliteHandsItsChestOn` ("the erased elite's chest is not dropped where it died": 1, expected 0), is not menu code; the fixture then passed 2/2 alone on the same copy (`-menu-pentagram/`). Not explained; seen once.
- Open: Android 48dp touch sizes not measured (a 54-unit tab is about 18dp on a 360dp-tall phone); icons are PNG, not vector; the owner has not played with the Pro look yet.

Not yet seen by the owner.

### Duplicator everywhere (owner: "duplicater ko check karo kya wo har bar kaam karta hai", 2026-10-04)

vampire.survivors.wiki/w/Duplicator (+1 Amount per level, 2 levels) and /w/Amount ("how many extra projectiles most weapons have"); each weapon's own page says whether Amount affects it (fetched 2026-10-04): it does for Whip, Magic Wand, Knife, Axe, Cross, King Bible, Fire Wand, Santa Water, Runetracer, Lightning Ring, Peachone, Bone, Cherry Bomb, Gun, Bloody Tear, Holy Wand, Thousand Edge, Death Spiral, Heaven Sword, Unholy Vespers, La Borra, Hellfire, NO FUTURE, Thunder Loop; not for Garlic, Soul Eater, Song of Mana, Mannajja, Pentagram, Gorgeous Moon, Clock Lancet, Laurel. Crimson Shroud: "Amount creates more explosions" (it already adds one per Amount).
- Code check: every weapon the wiki lists reads Amount, and none of the ignoring ones does.
- Found and fixed (the same look as the owner's 2026-09-30 report): **Thousand Edge** threw all its knives in one frame from one spot, and **Bloody Tear**'s extra lashes landed on top of the first ones, so the Duplicator looked like it did nothing. Both now fire one after another at the wiki interval (`FireInSuccession`): Thousand Edge 0.05 s, Bloody Tear 0.1 s (`VsInterval`).
- `DuplicatorEverywherePlayModeTests` (R): each of the 10 evolutions that use Amount fires 2 more, none on top of another (Thousand Edge 6 to 8, Bloody Tear 2 to 4, Holy Wand 1 to 3, Death Spiral 9 to 11, Heaven Sword 1 to 3 since the CMB-01 fix of 2026-10-19 (3 to 5 before), Unholy Vespers +2 books, Drowning Chalice 4 to 6, Hellmaw Comet 2 to 4, Severed Fate 1 to 3, Stormbound Halo 6 to 8); the 8 ignoring weapons fire the same; a Knife gained after the Duplicator throws 3 at once. With `DuplicatorPlayModeTests` (14 base weapons and the Bible) that is every Soul Hunter weapon.
- Test-side fixes on the way: the shot counter waits for a volley still in flight and counts a pooled object reused in the same frame as a new shot; the targets are marked stage bosses so Eclipse Sigil cannot erase them (their gems levelled the hero and the paused level-up screen stalled a run, which the stall guard stopped); the Mad Forest health/speed checks allow for Curse (the shared boot can take Skull O'Maniac from a leftover level-up).
- EditMode 566/566, PlayMode 286/286, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261004-dup-full2/`).

Not yet played by the owner.

### Main menu layout (owner: "main menu panel thik karo", 2026-10-03)

The owner played Level 1 after step 5 and said it is fine, then asked for the main menu panel; they chose "Layout + look" (AskUserQuestion; not the Power Up or character select panels, not the VS menu items).
- Found: the frame is drawn into the background picture (1672x941, kept at its aspect) with its centre ornament at x 56%, but the title and buttons were centred on the screen, and Quit fell onto the frame's bottom border (off screen in a short window). The logo is a TextMeshPro text with two sprites ("SOUL", "HUNTER") that were imported with a horizontal bearing of minus half their width, so each was drawn half its width left of its slot; the text was split over two lines with spaces to hide it.
- `MainMenuLayout` (added by `MainMenuNavigation`): measures where the background picture is drawn, places the logo and Start / Power Up / Options / Quit in the frame's open area centred on the ornament, and refits on window changes. Buttons are dark crimson with a red edge; focus stays the game-wide orange. The scene is not edited.
- `Assets/Art/UI/sprite_4.asset` and `sprite_5.asset`: horizontal bearing 0 (only the menu logo uses them); the logo is one centred line.
- VS wiki Main_menu lists Collection, Unlocks, Credits and "Press to start", which Soul Hunter does not have yet (not asked for now).
- `MainMenuLayoutPlayModeTests` (R): on the real main menu every button is on screen, inside the frame, centred on the ornament, not overlapping, with the crimson look; the drawn logo sprites (not just the text box) are centred above the buttons.
- EditMode 566/566, PlayMode 267/267, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20261003-menu-full/`). Build screenshots (`run-20261003-menu-build/hd4`, 1366x768): logo and buttons centred in the frame, Quit inside it (partial `V`). Other window sizes were not checked by eye (the build kept the same window size).

Not yet seen by the owner.

### Level 1 step 6: Bug MIS v2 fixes, ground comfort, names (owner, 2026-10-02)

The step 6 CTO pass played Level 1 for 30 minutes and wrote `Docs/BUG_MIS_Level1_v2.md` (23 bugs, 0 S1). The owner answered: "1. approved, 2. as a cto tum recommend karo, 3. ... indian mithology ... game final ho jane ke baad, 4. jiska bhi look pro type nahi lage use badal do", and asked for a ground that does not tire the eyes over 30 minutes.

**Fixes** (each row's status is in the register):
- Song of Mana's shot is now additive light, not an opaque oval that hid the hero.
- The HUD timer shows the stage clock.
- Hellfire Breath has no core sphere.
- Lightning bolts are thin, fading and keep their world size.
- Level-up cards are at most 1100 wide and centred. The level-up veil is even: the panel's full-screen tinted texture tile caused the patchy dim; `AutoplayBot -sh-autoplay-ui-graphics` found it.
- The hit flash is a short warm tint.
- The open chest's lid is two-sided, with a dark inside.
- HUD slots are opaque, and an evolution has a gold border. Backings are 85%.
- Damage numbers spread across the screen and live 0.65 s.
- The unlock notice has a backing.
- Weapon level cards say what each level adds (the facts of each wiki "Levels" table, in own words).
- Readable placeholder names: Song of Mana, Hollow Heart, Wings. The gold reward has one name.
- Four new icons (`ItemIconBuilder` + `ArtSource/Generators/build_item_icons.py`; not owner-approved art).
- **C19 (CTO call):** each chained level-up screen shows its own level, and "LV" sits under the bar.
- The story card is clear of the slots, and the chest backdrop is 0.94.
- Drops move to the nearest open ground.

**Ground comfort:** `StageFloorImporter.ComfortGrade` puts every floor colour's brightness into 0.30-0.68 (it ran from 0.08 on Levels 9-10 to 1.0 snow on Level 6), caps saturation at 0.55 × 0.9, and lowers glows from 2.5x to 1x. The floors were re-imported; only materials changed. Before/after for all 10 levels: `D:/SoulHunter-Validation/ground-compare-1.png` and `-2.png`.
- Not changed: the Level 2 Dark Forest still uses black fog and 0.18-0.22 ambient light with a flicker (`CampaignSignatureSystem`). The GDD's Level 2 mechanic is "the map gets progressively darker", which conflicts with eye comfort: **owner decision**.

**Coins:** bronze, silver and gold coin metal is 0.55 metallic with a 0.38 glow of its own colour (`PickupV2Importer.RetuneCoins`). Before, they were fully metallic with nothing to reflect, so they read as dark rings.

**Names:** `Docs/Name_Recommendations_Indian_Mythology.md` (recommendation only; renaming waits for the final game).

**Tests:** `BugMisV2PlayModeTests` (R: stage clock, breath, bolt, cards and per-screen level, veil, chest lid/seam/light, slots, notice/story card, drops off obstacles) and `BugMisV2Tests` (T: mana shot, flash, backings, level texts for every weapon level, names, icons). Updated: `DamageFlashPlayModeTests`, `HudFixesPlayModeTests` (the number spread), `WeaponDefinitionTests`.

**Validation:**
- EditMode 531/531; final run PlayMode 259/259. No test wrote the real save (`D:/SoulHunter-Validation/run-20261002-bugmis2d/`).
- Editor play `bugmis2-verify` to `-verify6`: 0 errors, min 37-78 FPS. Screenshots show the centred cards with level texts, the even veil, the gold-bordered evolution, the notice backing, the new icons and the darker chest inside (partial `V`).

Not yet played by the owner.

### Level 1 step 3: chest looks (2026-10-02)

vampire.survivors.wiki/w/Treasure_Chest, "Types" (fetched 2026-10-01): bronze chests "are usually incapable of providing weapon evolutions ... typically obtained as drops from bosses spawned before 10:00"; silver chests "are capable of providing weapon evolutions and unions"; Arcana chests give an Arcana. Golden and dark chests are not built (Soul Hunter has no chest that gives unowned weapons, nor dark chests).
- `ChestLook` recolours the approved Soul Reliquary per chest (owner: "Pehle approved VFX se"): bronze or silver iron bands; an Arcana chest has dark violet bands and a violet soul seam. Property blocks only; the shared materials are untouched.
- `ChestPickup.CanEvolve` / `IsArcana` are properties that apply the look when `EnemyDrop` sets them (`CanEvolveAt`: from 10:00, and the 1:00 chest, so that one is silver); `ResetPickup` returns a pooled chest to silver. `ChestResult.Kind` carries the look to the opening screen, whose preview copy shows it (`ChestPreviewStage.Create(..., kind)`).
- `ChestLookPreview` (menu Soul Hunter/Content/Render Chest Looks) renders the three side by side: `D:/SoulHunter-Validation/run-20261001-chestlook/chest-looks-final.png`. **The colours are Soul Hunter's own and not yet owner-approved.**
- `ChestLookPlayModeTests` (R): a 5:00 carrier drops a bronze chest that cannot evolve, a 10:05 one a silver chest that can, an Arcana carrier a violet chest; a reset chest is silver again; the opening screen shows each look; the iron material itself is unchanged.
- Validation: EditMode 520/520, PlayMode 251/251, no test wrote the real save (`D:/SoulHunter-Validation/run-20261001-chestlook/`). Not yet played by the owner.

### Level 1 step 2, Phase 1 rows 9-12: spawning, Orologion, Curse, traps (2026-10-01)

- **Row 9, spawning** (vampire.survivors.wiki/w/Enemies): "Enemies generally spawn just outside the screen." They spawned 20 units out, inside a 16:9 view's sides (about 21). `EnemySpawner.DistanceToScreenEdge` finds the camera's edge along each direction (viewport bisection); wave enemies, bosses and returning stragglers appear 1.5-4.5 units past it (`OffScreenMargin`, `OffScreenSpread`; Soul Hunter's numbers). Wiki Curse: "Total enemy quantity allowed on screen is capped at 500, regardless of Curse and Charm": map events stop at `EnemySpawner.HardCap` 500; stage bosses and chest elites are not held back (Soul Hunter's choice). The 300 cap for periodic spawns was already there. The plan's earlier "500 hard cap" note was right; the Enemies page does not say it, the Curse page does. Mad Forest's 10 starting spawns are covered by the minute-0 minimum (15), filled at once.
- **Row 10, Orologion** (wiki Orologion): "new enemies will also become frozen" (`TimeFreezePickup.WorldFreezeEndsAt`, applied in `EnemySpawner.SpawnEnemy`); it "slows down Gold Fever's duration decay by half" (`GoldFever`). A stage boss spawned during the freeze is not frozen.
- **Row 11, Curse** (wiki Curse): the health effect applies to new enemies at once; speed, spawn frequency and quantity use the Curse held when the stage minute began (`EnemySpawner.MinuteCurse`).
- **Row 12, traps** (wiki Enemies, Traps): one global cooldown, "effectiveCooldown = trapCooldown x totalLuck" (`PressurePlate.GlobalReadyAt`, `EffectiveCooldown`); every plate dims while it runs; a stage's new plates start ready. The base 20 s stays (VS pressure plates, 2026-09-26).
- Tests: `OffScreenSpawnPlayModeTests` and `OrologionCurseTrapPlayModeTests` (R).
- Validation: EditMode 520/520; PlayMode 242/243, then 241/243 in a first run: three tests failed once each and passed on rerun, all test-side: the Duplicator Cherry Bomb count included random explosions (row 8; now counts bombs only), the lantern test identified the oldest lantern by place (two random spots can coincide; now by object), and the Soul Meteor test once read 0 damage, most likely a 240 ms invulnerability from something else (the test now clears it until the strike). The three fixtures then passed 24/24 twice. No test wrote the real save (`D:/SoulHunter-Validation/run-20261001-rows9to12-full2/`, `-fixtures-1/`, `-2/`).
- Editor play after rows 2-12 (`phase1-play`, Stage 1, 6 min, Kael): 0 errors, 0 hitches over 0.25 s, min 70 FPS, level 12, 867 kills; at 5:50 the horde was 10, the VS Mad Forest minute-5 minimum (partial `V`).
- Phase 1 rows 2-12 are built; not yet played by the owner.

### Level 1 step 2, Phase 1 rows 7-8: crits, weapon rules, Limit Break (2026-10-01)

**Row 7, crits** (vampire.survivors.wiki/w/Critical_Hit and each weapon's page): weapons have their own crit chance and multiplier (`AutoAttackWeapon.BaseCritChance`, `WeaponCritMultiplier`, `SlashCritChance`, `SlashCritMultiplier`, `CritFor`). Without the Slash Arcana only Bloody Tear (10%, 2x) and Heaven Sword (10%, 2.5x; `CrossWeapon._critChance/_critMultiplier` in `HeavenSword.prefab`) crit. Whip 20%/2x, Knife 30%/3x, Axe 30%/2x, Thousand Edge 30%/3x and Death Spiral 30%/2x crit only with Slash, which doubles every multiplier (`AutoAttackWeapon.SlashArcanaActive`; Soul Hunter has no Slash Arcana yet). The Cross never crits. Before, seven weapons had a provisional 10%/2x. The Critical_Hit page leaves Thousand Edge and Death Spiral out; their own pages are followed. Kaelen's 3x (Soul Hunter's own) is a factor over the VS default (`CharacterCritFactor`), so Bloody Tear crits 3x for him. `CritTests` (T).

**Row 8, weapon rules** (each weapon's wiki infobox and page):
- Knockback per weapon (`AutoAttackWeapon.Knockback`, carried by `Projectile`, `ProjectileDamage`, `TouchDamage`, `DamageCaster` through `AimAtHostiles`; `DamagePacket.KnockbackSet` lets 0 mean none): Knife and Thousand Edge 0.5, Peachone 2, Gun 0.1, Heaven Sword 6, Unholy Vespers 4 (prefab fields), Garlic and Soul Eater 0 (was 0.3), Santa Water 0 ("N/A"); the rest 1. Song of Mana, Cherry Bomb, Death Spiral and Holy Wand give none on the wiki and keep 1. The wiki gives no per-enemy hit delay except King Bible's. `KnockbackTests` (T).
- King Bible: out for the Duration (3 s, +0.5 at L4/L7), then rest; a new cycle every Cooldown 3 s + base Duration ("6 seconds total"); always out when Duration outlasts the cycle; 1.7 s hitbox delay per book. `BibleCyclePlayModeTests` (R).
- Pentagram: one roll per activation; normally it also erases the items on screen (not stage items) via `GroundItemEraser`, unless the keep chance x Luck spares them; a 15 s cooldown floor (`MinimumCooldown`); the glyph lasts 1 s. `PentagramVsPlayModeTests` (R).
- Peachone: an activation bombards for 4 s x Duration, one set per second x Cooldown, then the 1 s Cooldown; level cooldown steps are 0.25 s (wiki footnote). `WeaponDurationRulesPlayModeTests` (R).
- Cherry Bomb: explodes only by chance, 40% / 50% from L3 / 60% from L6, x Luck; an explosion deals the bomb's damage around it (the wiki gives no separate number). Bouncing and slowing down are not built.
- `DuplicatorPlayModeTests` was adjusted: the Bible test brings its books out before counting; the Peachone test fires exactly one set.

**Limit Break** (wiki Limit_Break "List of upgrades"; owner chose "wiki ke saaf numbers lagao", 2026-10-01): each card raises one stat of one held weapon, drawn by rarity among the stats it uses: Base Damage +1 (10, no cap; the wiki's "Might +0.3%" row is noted as base damage), Area +2% (10, +500%), Speed +5% (10, +200%), Duration +0.1 s (1, +2 s), Amount +1 (1, +10), Pierce +1 (5, +10; Knife, Gun, Magic Wand, Axe), Crit Chance +2.5% (5, 100%; crit-capable weapons), Chance +5% (10, 100%; Cherry Bomb, Pentagram). It was +10% damage per card. `RunModeTests` (T).

Validation: EditMode 520/520, PlayMode 239/239, no test wrote the real save (`D:/SoulHunter-Validation/run-20261001-rows789/`).

### Level 1 step 2, Phase 1 row 6: level-up rules (2026-10-01)

vampire.survivors.wiki/w/Reroll: "The items that were discarded by a reroll cannot appear in the new set of items." wiki Level_up: "Skips and Rerolls are unavailable when the player has maxed out their inventory"; maxed level-ups offer gold or a Floor Chicken, "with the option to always receive either Floor Chickens or Gold once the player has received 100 Big Coin Bags from any source"; "Always Floor Chicken" gives gold at full health.
- `LevelUpManager`: a reroll leaves the shown items out of the new set (they may return only if nothing else is left); `CanReroll`/`CanSkip` are false on maxed-out or Limit Break cards (`OfferingMaxedChoices`), and the level-up screen hides those buttons.
- Big Coin Bags are counted on the save (`GameData.BigCoinBagsReceived`, repaired by `SaveService`): the Gold Pouch card and a chest item with nothing left to upgrade (`GoldRewards.GrantBigCoinBags` / `CountBigCoinBags`).
- From 100 bags the maxed screen adds "Always Gold Pouch" and "Always Roast Chicken" (`MaxedRewards`); once picked, the run's later maxed level-ups pay out with no screen. Card names and text are Soul Hunter's; the wiki gives no UI.
- `LevelUpRulesPlayModeTests` (R). EditMode 498/498, PlayMode 234/234, no test wrote the real save (`D:/SoulHunter-Validation/run-20261001-lvlrules-full/`).

### Level 1 step 2, Phase 1 row 5: an upgrade fires at once (2026-10-01)

vampire.survivors.wiki/w/Weapons: "Upgrading a weapon will also reset its Cooldown and activate it instantly."
- `AutoAttackWeapon.ResetCooldownAndFire()`, called by `WeaponManager.ActivateOrUpgradeWeapon` whenever an owned weapon gains a level (level-up, chest, stage item). Magic Wand starts its next volley (not mid-volley); Laurel's missing shield charge comes back.
- `WeaponUpgradeFirePlayModeTests` (R: the Whip, mid-cooldown, fires again the frame after its upgrade). EditMode 498/498, PlayMode 232/232, no test wrote the real save (`D:/SoulHunter-Validation/run-20261001-upgradefire-full/`).

### Level 1 step 2, Phase 1 row 4: 240 ms invulnerability (2026-10-01)

vampire.survivors.wiki/w/Invulnerability_Time: "At base, the player starts with 240 milliseconds" of immunity after damage, added to every source of immunity, Floor Chicken included; Treasure_Chest (Patch 0.2.4): "a brief moment of invulnerability after picking up a treasure". The wiki gives the chicken and chest no length of their own; the owner chose 240 ms (AskUserQuestion).
- `HealthController.VsInvulnerabilityTime` = 0.24 s: after a hit (`PlayerController`; it was 0.5 s, and the serialized `_hitInvulnerabilitySeconds` field is gone), after a Floor Chicken (`HealthPickup`), after picking up a chest (`ChestPickup`; game time stands still on the chest screen, so it covers the resume) and after a level-up pick (`LevelUpManager.FallbackInvulnerabilitySeconds`, was 0.5 s).
- `InvulnerabilityTimePlayModeTests` (R). EditMode 498/498, PlayMode 231/231, no test wrote the real save (`D:/SoulHunter-Validation/run-20261001-invul-full/`).

### Level 1 step 2, Phase 1 row 3: VS stat caps (2026-10-01)

vampire.survivors.wiki/w/Player_stats, "Max value": Might 1000% ("Does not affect Soul Eater"), Area 1000%, Speed 500%, Duration 500%, Amount 10, Armor 50; Cooldown 10% (its floor was already applied).
- `PlayerStats` keeps what every source gathers (`RawMight`, `RawArea`, `RawProjectileSpeed`, `RawDuration`, `RawAmount`, `RawArmor`) and gives the game the capped value (`Might` ... `Armor`; `MaxMight` ... `MaxArmor`). Shadow Kael copies the raw values.
- Soul Eater (`GarlicWeapon` with life steal) reads `RawMight` (`AutoAttackWeapon.IgnoresMightCap`).
- `StatCapTests` (T). EditMode 498/498, PlayMode 230/230, no test wrote the real save (`D:/SoulHunter-Validation/run-20261001-caps/`).

### Level 1 step 2, Phase 1 row 2: level 20/40 Growth (2026-10-01)

vampire.survivors.wiki/w/Level_up: "at levels 20 and 40 ... the player also gains +100% Growth, increasing their experience gain, until they reach the next level"; wiki Growth: base 100%, bonuses additive, "This bonus is reverted at level 21 and 41". The XP requirement curve already matched (5, +10, +13 from 21, +16 from 41; +600 at 20, +2400 at 40).
- `PlayerExperience.GrowthBonusChange`: reaching level 20 or 40 adds +1 (100%) to the Growth stat (`PlayerStats.AddExpBonus`), reaching 21 or 41 takes it off; the stats panel shows it.
- Tests: `LevelUpCurveTests.TheGrowthStep_IsPlusOneAt20And40_AndMinusOneAt21And41` (T) and `LevelGrowthPlayModeTests` (R: Growth +100% at 20, a 10 XP gem gives twice as much, back at 21; again at 40/41).
- EditMode 497/497, PlayMode 230/230, no test wrote the real save (`D:/SoulHunter-Validation/run-20261001-growth-full/`).

### Chest screen OPEN off-screen in a wide window (owner: "HANG HO GAYA", 2026-10-01)

The owner's screenshot showed the chest screen ("TREASURE x3!") with no OPEN button, in a wide, short Game view (about 1366x575). `ChestUI.ScreenCanvas` scaled its 1920x1080 layout by width only, so the canvas was only 808 units high and OPEN (430 below the centre, 96 high) fell off the bottom; the paused game waited for a button nobody could reach. A 21:9 monitor would do the same.
- Fix: the chest canvas uses `ScreenMatchMode.Expand`, so the whole 1920x1080 layout fits any window.
- `ChestScreenFitPlayModeTests` (R): OPEN and DONE inside 1366x575, 2560x1080, 3440x1440, 1920x1080, 1280x1024 and 1080x1920. It fails on the old code ("OPEN at (0, -430) is off a 1366x575 window (canvas half 404)").
- EditMode 496/496, PlayMode 227/227 (`D:/SoulHunter-Validation/run-20261001-chestfit/`). The tests wrote only temp saves; the real save changed during the run because the owner was playing in their editor at the time (its Editor.log shows those saves).
- Not checked by eye at that window size (the editor launcher cannot set the Game view size).
- **Level-up screen too** (owner: "level up screen bhi chest jaisa kar do"): `LevelUpUI.EnsureOwnCanvas` moves the panel, once, when it first opens, from the HUD canvas (width-only scaling) to its own overlay canvas `LevelUpScreenCanvas` (Expand, 1920x1080, sorting order 300: above the HUD, below the chest screen and unlock notices). The HUD keeps its own scaling; `HUD_Canvas.prefab` and the scene are not edited. `LevelUpScreenFitPlayModeTests` (R): own Expand canvas, every card and Reroll/Skip/Banish button on screen. EditMode 496/496, PlayMode 228/228, no test wrote the real save (`D:/SoulHunter-Validation/run-20261001-levelupfit-full/`). Editor play with the owner's scene (`levelupfit-01`): same look at 1920x1080, 0 errors.

### Level-up Reroll/Skip/Banish and VS locked PowerUps (owner, 2026-10-01)

**"level up panel ke neeche skip etc 3 button kaam nahi karte".** They worked; the owner's save had no Reroll/Skip/Banish PowerUp, so all three were at 0 and disabled. vampire.survivors.wiki/w/Level_up: they "become available in this screen if the player invests in these PowerUps and/or uses a character that may start with these"; the Reroll, Skip and Banish pages: they start at 0, +2 per rank.
- `LevelUpUI.RefreshActionButtons` shows each button only while it has a use left (Banish also while armed) and hides the row when all three are gone; the cards take the space.
- `LevelUpActionButtonsPlayModeTests` (R): hidden at 0; with 1 use each, a click through the UI raycast rerolls, banishes the clicked card and skips with experience. It passes with the owner's scene and HUD prefab too.

**VS locked PowerUps ("adopt vs rule").** Each PowerUp's wiki page (fetched 2026-10-01): Reroll ranks unlock by reaching level 80 with five characters; Skip ranks by surviving 15 or 30 minutes on five stages; Banish ranks by 60-100 Collection entries; Omni by maxing Torrona's Box; Charm, Defang and Preserve by relics. The other 16 PowerUps are open from the start. The owner chose Soul Hunter conditions (`PowerUpUnlocks`, Save/SaveData/PowerUps.cs):
- Reroll: level 80 with Corvan, Aria, Draven, Gideon and Kaelen (one rank each).
- Skip: survive 15 min in Level 6, 30 in Level 7, 30 in Level 8, 15 in Level 9, 15 in Level 10 (one rank each; a stage cleared at 30:00 counts as 30 minutes).
- Banish, Omni, Charm, Defang and Preserve stay locked until Soul Hunter's Collection, relics and item exist (Phases 2-3).
- Unlocks are ids in `GameData.UnlockedAchievements`. `CharacterUnlockTracker` records them during a run (every stage second and on a stage clear), saves, and shows "PowerUp unlocked: Skip rank 1" (held while a menu pauses).
- The shop shows a locked PowerUp as LOCKED with its next condition, and Buy only sells unlocked ranks (`PowerUps.CanBuyNext`).
- A save that owns ranks now locked gets the VS Refund once, in the save repair (`PowerUpUnlocks.RefundIfLockedRanksOwned`). The owner's save owned none.
- `PowerUpUnlocks.UnlockEverything` is for tests and debug tools.
- Tests: `PowerUpUnlockTests` (T) and `PowerUpUnlockPlayModeTests` (R: shop LOCKED with its condition, no sale, one rank sold once unlocked; 15:00 in Level 6 unlocks Skip rank 1 with a notice).
- Validation: EditMode 496/496; PlayMode 224/225, the one failure being `UnlockNoticePlayModeTests` calling the old notice signature, fixed and rerun with the related fixtures 31/31; save untouched (`D:/SoulHunter-Validation/run-20261001-unlocks/`, `-unlocks2/`).
- Not yet played by the owner.

### Game "hangs" at the first level-up (owner: "kabhi bhi hang ho ja raha hai", 2026-09-30)

The owner's Editor log showed both runs stopping right after the first level-up paused the game. Cause: the owner's unsaved `Main_Gameplay` saves `LevelUp_Panel` inactive. The panel carries its own `LevelUpUI`, whose `Awake` hides the panel; with the panel inactive that `Awake` first ran when the first level-up switched it on (from the `LevelUpUI` on `UI_Bindings`), so it hid the panel again. The game stayed paused with no cards: a "hang". Later level-ups worked. The committed scene saves the panel active, so tests and bot runs never saw it.
- Fix: `LevelUpUI.Awake` does not hide the panel while a level-up round is open (`LevelUpManager.IsLevelUpActive`).
- `LevelUpPanelStartPlayModeTests` (R): a panel copy that has never woken is shown on the first level-up. It fails on the old code.
- `AutoplayBot` logs `hitch` (frames over 0.25 s, not screenshot frames) and `stuck-pause` (paused over 20 s, with `GameTime.PauserNames()`) events, and with `-sh-autoplay-levelup-ui` a `levelup-ui` event (panel and card screen rects).
- EditMode 491/491, PlayMode 222/222, save untouched (`D:/SoulHunter-Validation/run-20260930-hang/`).
- Editor play with the owner's scene and HUD prefab: before the fix (`hang-03`) the first level-up panel was inactive; after it (`hang-04`, 5 min, Draven) all 19 level-ups showed their panel, 0 hitches, 0 errors, min 61 FPS. Bot runs on the committed scene (`hang-01`, 8 min) had no hitch or stuck pause either.
- ~~Seen, not changed: with the owner's HUD layout the level-up stats panel has room for only 2 of its rows.~~ Corrected 2026-10-01: that was the bot's screenshot 1-2 frames after the screen opened. The panel was fitted to the cards' positions before the layout rebuild (2 of 5 rows) and corrected itself a frame later; settled, all 19 stats showed. `LevelUpUI.ShowLevelUpScreen` now forces the canvas layout and refits the panel when it opens. Editor play with the owner's scene (`stats-03`, shot at the opening frame): all 19 stats, 0 errors. `StatsPanelFirstFramePlayModeTests` (R) guards it but does not reproduce it in the test runner's small window. `AutoplayBot` logs the stats panel's geometry with `-sh-autoplay-levelup-ui` (and waits 0.5 s first). EditMode 496/496, PlayMode 226/226, save untouched (`D:/SoulHunter-Validation/run-20261001-stats/`).
- The owner played the hang fix, the Reroll/Skip/Banish change and the locked PowerUps (2026-10-01): "maine khel kar dekha abhi ok hai".

### Level 1 Bug MIS fixed (owner: "sab bug fix karo safely", 2026-09-30)

Owner's CTO brief: play Level 1 from the Play button to 30:00, only note bugs, show them, fix after approval. The register is `Docs/BUG_MIS_Level1.md` (runs `D:/SoulHunter-Validation/cto-level1-runA`, `-runC`; 17 bugs, 7 observations). All 17 are fixed:
- **B01 (waves):** every minute table covers 30 minutes (it covered 0-24, and minutes 25-29 reused minute 24; on Stage 1 that was a quiet minute, so the last six minutes held ~84 enemies). Stage 1 now plays the VS Mad Forest table exactly, from vampire.survivors.wiki/w/Mad_Forest "Waves" (`WaveTables.MadForestMinimum/Interval`; no elite bonus, which is not a VS field). Stages 2-10 keep their own curve for 0-24 and hold its peak for 25-29. The 10 wave assets were rebuilt.
- **B02 (white silhouettes):** the hit flash is 1.8× (was 3×, which saturated models to flat white), runs on real time (it froze white during pauses), and waits 0.15 s (`DamageFlash.FlashGap`) before flashing again, so an enemy under constant hits blinks.
- **B03:** `WeaponEvolutionData.DisplayName` shows evolution IDs as names on the chest ("Evo_BloodyTear" → "Bloody Tear"). The names themselves are still VS names (no-copy rule, Phase 2).
- **B04 (VS):** chests draw only owned items (vampire.survivors.wiki/w/Treasure_Chest: "a random weapon or passive item the player currently has"); nothing owned left pays the 25-gold coin bag.
- **B05:** the unlock notice waits hidden while a menu pauses the game.
- **B06, B14:** HUD labels get a dark backing sized to their text (`HudBacking`) and stay on one line.
- **B07:** damage numbers are drawn 4 units toward the orthographic camera (same screen spot), so models no longer cut them.
- **B08:** the HUD holds back the chest's gold until the count-up reaches it (`HudGoldHold`).
- **B09:** the level-up stats panel starts at 23% of the width (the HUD item slots end near 21%) and is only as tall as its rows. The font is sized from the free band; sizing it from the shrinking panel shrank the text every frame (caught in editor play, fixed before commit).
- **B10:** Gold Pouch uses `SH10_Icon_GoldPouch.png` (the approved gold Soul Coin rendered in Blender, transparent); Roast Chicken uses the project's `Floor_Chicken` sprite (`GameContentCatalog.GoldPouchIcon/RoastChickenIcon`); the chest's coin-bag card uses the gold icon too.
- **B11:** the chest preview's hinge seam used every part named "Lid*", including the wide LidBand rings, so most of the body swung up with the lid. Only the lid shell counts now; the body stays and the soul seam glows inside.
- **B12:** level-up card texts get a size before auto-size (TMP defaults up to 72 replaced the limits on the first screen).
- **B13, B16, B17:** `OptionsMenuUI.ApplyFocusColors` changes focus at once (no brown fade midpoint), sets a dark disabled colour (Reroll/Skip/Banish), and the main menu buttons use it (orange focus, not blue).
- **B15:** invisible card-sized spacers keep the 3-card layout when fewer cards are offered.
- Also: `LevelUpUI` no longer writes ~10 `Debug.Log` lines per level-up (costly in the editor; observation O2).
- Tests: `WaveTableTests`, `WaveTablePlayModeTests`, `DamageFlashPlayModeTests`, `ChestNameTests`, `LevelUpActionTests`, `ChestRewardTests` (now owns the item first), `UnlockNoticePlayModeTests`, `HudFixesPlayModeTests`, `ChestFlowPlayModeTests`, `StatsPanelPlayModeTests`, `RewardIconTests`, `ChestPreviewPlayModeTests`, `LevelUpScreenPlayModeTests`.
- The stats-panel font loop only shows at 1080p (in the test runner's small window the font is limited by width), so it was checked in editor play (`autoplay-editor` run `cto-level1-verify2`).
- Editor play `cto-level1-verify` (8 min from the menu): 0 errors, min 57 FPS; the screenshots show the orange menu focus, the chest opening with only its lid, owned-item chest rewards, HUD backings and one-line kills. `cto-level1-verify2` (3 min): the level-up panel readable and clear of the HUD row.
- Not yet played by the owner.

### Character select screen repaired (owner: "play button click karne ke baad aane bale panel to tumne bekar bana diya", 2026-09-30)

The owner only started seeing this screen again once Play booted through the menu (`PlayFromBootstrap`). Three causes, all older than that change:
- **Constant Pixel Size canvas.** The main menu's `CanvasScaler` used Constant Pixel Size, so the fixed 190×132-pixel cards (4 columns) overflowed the grid in any window smaller than 1920×1080. `MainMenuNavigation.ScaleWithScreen` now sets Scale With Screen Size, 1920×1080, match 0.5 (like the gameplay HUD) when the menu loads; 1080p is unchanged. The scene file is not edited.
- **The mode row (2026-09-27) pushed the grid down,** and the last row (Valerius) hid behind the info box. The grid now has 5 columns, and `GridFit` sizes the cards from the grid box, so all 13 characters show without scrolling.
- **TextMeshPro defaults replaced the text limits.** The panel is built while inactive, so each created text ran TMP's `Awake` later and took its defaults (size 36, auto-size up to 72). Gold, FILTER, Whip and the LOCKED badges (which wrapped to "LOCKE / D") rendered at 53-68. `AutoSize` now sets a size first, so the limits hold (Gold 32, FILTER 20, Whip 18). The six mode labels share one size (`SameFontSize`), so "Limit Break" fits on one line.
- `AutoplayBot -sh-autoplay-select-shot` screenshots the main menu and this screen, and logs the canvas scale and the key text sizes.
- `CharacterSelectionPlayModeTests.SelectScreen_FitsTheWindow_EveryCardVisible_NothingOverlaps` (R, in the test runner's non-1080p window): the menu scales with the screen; every card is fully inside the grid; the grid is clear of the mode row and the info box; the mode labels share one size; Gold/FILTER/Whip stay within their limits; each LOCKED badge is one line. It failed on the old code.
- EditMode 481/481, PlayMode 215/215, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20260930-charselect2/`).
- Editor play `autoplay-editor-91`: screenshots of the main menu and the character select screen, 0 errors (partial `V`). The portrait in the info box is still very dark (the hero render; Art section).

### Stats panel in pause and level-up (owner: "stats panel bana do", 2026-09-30)

vampire.survivors.wiki/w/Player_stats: stats "can be seen before the player starts the game in the Character Selection menu, in-game when paused, or when leveling up". Soul Hunter showed them only on character select, so Hollow Heart's new Max Health was invisible.
- `StatsPanel` (UI/HUD) lists the live stats in the wiki's order: Max Health, Recovery, Armor, Move Speed, Might, Area, Speed, Duration, Amount, Cooldown, Luck, Growth, Greed, Curse, Magnet, Revival, Reroll, Skip, Banish (`CharacterStatSheet.Live`, `InGameRows`). It uses the character select formats (absolute Max Health, +N, ±%, "-" when unchanged), with changed values in gold.
- **Pause menu** (`PauseMenuUI`): a single column on the right, under the timer. On the left the HUD's kills, gold and item rows drew over it (seen in `autoplay-editor-84`).
- **Level-up screen** (`LevelUpUI`): a 4-column grid that fits itself each frame between the "LEVEL UP!" text (its drawn bounds; the heading's rect reaches down to the cards) and the top card, at any window size. `ignoreLayout` keeps the card layout group from moving it.
- Each column has a label text and a value text, sized together to fit (a long label ran into its value in `-86`). It refreshes every 0.25 s while shown, so several level-ups in a row update it.
- The look is Soul Hunter's own. The wiki does not describe the layout.
- Tests: `StatsPanelTests` (T: order and formats) and `StatsPanelPlayModeTests` (R: the pause panel shows the live values and is clear of the buttons, and hides on resume; on the level-up screen, Hollow Heart picked while it is open shows 100 → 120 in gold while current health stays, and the panel is clear of the cards and below the heading text).
- `AutoplayBot` options `-sh-autoplay-pause-shot` (pause at 20 s, screenshot, resume) and `-sh-autoplay-levelup-shots` (every level-up screen).
- EditMode 481/481, PlayMode 214/214, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20260930-stats2/`).
- Editor play `autoplay-editor-87` (Stage 1, 1 min): both screens readable, 0 errors, min 87 FPS (partial `V`).
- Not yet played by the owner.

### Owner reports: Play did not boot; Max Health "does nothing" (2026-09-30)

**Play started Level 1 directly** (owner: "game play button click karte hi direct level 1 start ho raha hai, boot kyun nahi ho raha"). Unity's Play button starts the open scene, and the owner's editor had `Main_Gameplay` open. The project never had a start-scene hook (the only earlier one was a loose tool script removed as clutter).
- `PlayFromBootstrap` (Editor, `[InitializeOnLoad]`): while **Soul Hunter/Play From Bootstrap** is on (the default, stored in EditorPrefs), pressing Play on any game scene (one enabled in the build settings) starts from `Bootstrap`, as the build does. The test runner's own scene and batch mode are left alone. Switch the menu item off to play a single scene directly.
- CLI check (`-executeMethod SoulHunter.Editor.Autoplay.PlayFromBootstrap.CheckFromCommandLine -sh-bootcheck <file>`, GUI editor on the isolated copy): with `Main_Gameplay` open, Play loaded Loading at 0.2 s and MainMenu at 2.0 s (`D:/SoulHunter-Validation/run-20260930-boot/bootcheck.txt`).

**Max Health "does not increase"** (owner). Checked, and it works as VS says. vampire.survivors.wiki/w/Hollow_Heart: it "multiplies the current value of Max Health by 1.2" and "does not heal to fill up the newly increased Max Health". The HP bar shows current ÷ max, so after Hollow Heart at full health it looks emptier (100/120), and no number shows the new maximum. `PassivePlayModeTests` (R) confirms ×1.2 on the real player; `HealBalancePlayModeTests` (R) confirms the Max Health PowerUp (133/133 at rank 3). VS shows the stats in the pause and level-up screens; Soul Hunter does not yet (VS DOT table, Phase 3 "Stats panel").

**Found while re-testing the Duplicator work:**
- The Lightning Ring skipped a strike whose target died or was pooled in the 0.05 s gap. It now passes over dead targets to the next nearest one, so every strike lands while enemies remain.
- `DuplicatorPlayModeTests` measured the Magic Wand wrongly: the wand runs its own volley cycle in `Update`, so a volley did not always fall inside the test's window. The test now starts the wand's volley through its cooldown. The dummy targets keep their controllers on, because the Lightning Ring and Fire Wand target only live enemies. The fixture then passed 3 runs out of 3.
- Validation: EditMode 480/480, PlayMode 212/212, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20260930-boot2/`).
- Not yet played by the owner.

### Owner play reports: Duplicator, low-health tint, obstacles (2026-09-30)

The owner played Phase 0 and reported three things.

**1. "Duplicator kaam nahi kar raha".** The +1 Amount per level (vampire.survivors.wiki/w/Duplicator: 2 levels, +1 each) reached the weapons, but most weapons fired their extra shots in the same frame from the same spot. Axe (same line), Cross (a ±15° fan), Gun (±5° wobble) and the Whip's third lash (the same side as the first) landed on top of each other, so the Duplicator looked like it did nothing.
- VS rule, from each weapon's wiki infobox "interval": the Amount of projectiles leave one after another. `AutoAttackWeapon.FireInSuccession(count, interval, fireOne)` fires shot 0 at once and the rest on game time (a pause holds them; they stop if the weapon is switched off; Edit Mode fires them all at once).
- Intervals: Whip 0.1, Knife 0.1, Cross 0.1, Axe 0.2, Runetracer 0.2, Gun (Phiera) 0.2 per volley of four, Santa Water 0.3, Cherry Bomb 0.3, Lightning Ring 0.05, Fire Wand 0.02, Peachone 0.025 (it was 0.1). The Magic Wand already used 0.1. Each is `VsInterval` in its script.
- The Bone page gives no interval, so the Bone keeps firing together with its 15° spread (wiki silent).
- The Cross no longer fans: VS fires every cross at the closest enemy, 0.1 s apart.
- Lightning Ring strikes skip a target that died or was pooled in the 0.05 s between strikes.
- `DuplicatorPlayModeTests` (R, 14 weapons): Duplicator level 2 gives +2 Amount, each weapon fires 2 more, and no two shots appear together at one spot heading the same way. Before the fix, Axe, Cross, Runetracer, Bone and Gun failed. (Runetracer and Bone were a measuring error: they fly by velocity, which the test now reads.) The King Bible is checked by its orbiting books (+2).

**2. Low-health red screen removed** (owner: "player ka life kam hone per red flash pure screen pe aata hai, use hata do"). `LowHealthWarning` pulsed a red tint over the whole screen at 30% health and below; the wiki does not describe any such effect in VS. It is deleted, with its wiring in `GameHUDController` and its EditMode cases. Hit feedback on the hero's model (`DamageFlash`) stays. `LowHealthPlayModeTests` (R): at 10% health no overlay and no red full-screen image.

**3. 70% of the ground obstacles removed** (owner: "ground ka obstacle 70% hata do"). `build_grounds.py` keeps `OBSTACLE_KEEP` = 30% of the obstacles it placed on each floor (at least 1; a fixed per-level seed picks which; `thin_obstacles`). All 10 floors were rebuilt in Blender 5.2.1 against the isolated copy (`SOULHUNTER_ROOT`) and the FBX files copied back; prefabs, colliders and materials are unchanged.

| Level | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
|---|---|---|---|---|---|---|---|---|---|---|
| Obstacles before | 15 | 13 | 9 | 14 | 11 | 13 | 7 | 10 | 15 | 16 |
| Obstacles now | 4 | 4 | 3 | 4 | 3 | 4 | 2 | 3 | 4 | 5 |
| Reachable ground | 92% | 94% | 93% | 94% | 95% | 93% | 93% | 95% | 95% | 93% |

`StageFloorTests` now expects 1-5 blocker boxes per floor (it required at least 6). `FloorWalkabilityTests` (T) passes with no trapped pockets.

Validation: EditMode 480/480 (5 low-health cases removed), PlayMode 212/212, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20260930-ownerplay/`). Editor play `autoplay-editor-83` (Stage 1, 3 min, no god mode): 0 errors, min 77 FPS; the screenshots show one crypt and three gravestones per tile, and the Whip's two lashes as two arcs. The bot again left gems uncollected (131; `-78` left 383), which is bot behaviour.

Not yet played by the owner.

### Phase 0 of the VS DOT plan (owner decisions, 2026-09-30)

Built from the owner's answers (`Docs/VS_DOT_Plan.md`, Phase 0):
- **Dash:** it only moves and avoids damage, as the GDD says ("dash through enemies and avoid damage"); the flat 50 damage is gone.
  - **Defect found by the new test:** the dash moved `transform.position` while the player's Rigidbody interpolates, so Unity overwrote most of it: it covered about 0.7 of its 3 units. It now moves the Rigidbody.
  - The kinematic dash also went through stage obstacles and the Castle corridor walls. It now stops at the first `Obstacle`-layer collider (sphere cast, the capsule's 0.4 radius).
  - `DashPlayModeTests` (R): the dash covers its distance, an enemy in its path takes no damage, the hero is invulnerable only while dashing, and a wall 1.5 ahead stops it.
- **Invented unions removed:** the six `Union_*.asset` files are deleted. The union system stays for VS's own unions (Phase 2). `Main_Gameplay`'s `_availableUnions` list still holds six missing references, which `WeaponManager` skips; the owner's editor had the scene open with unsaved changes, so the scene file was not touched. `UnionPlayModeTests` (R): no union is live.
- **Gem colours:** vampire.survivors.wiki/w/Experience_Gem: blue "up to 2 XP", green "up to 9", red beyond. Scaled ×10: under 21, under 91, above (`XP_Gem.prefab`, `TieredVisual`, `PickupV2Importer.GemThresholds`). `PickupV2Tests` (T): the tiers and the prefab thresholds.
- **Stage-clear revival gold:** The_Reaper and Revival pages: +100, +200, +300, +400 per unused revival, then +100 each ("3 revives is 500 + 100 + 200 + 300 = 1,100"). The Stages page says a flat 100; the owner chose the stacking pages. `LevelProgressionManager.StageClearGold`; `BossStageAdvanceTests` (T).
- **Shadow Kael's mirrored Clock Lancet** (owner exception, VS has no player freeze): a beam that crosses the player makes it move at half speed for the weapon's freeze time (2 s at level 1, × the boss's mirrored Duration) (`PlayerController.Slow`, `IsSlowed`). The boss and its horde are still never frozen.
- **Shadow Kael's mirrored Pentagram** (owner exception using VS Pentagram's own item erase, vampire.survivors.wiki/w/Pentagram: "erasing every enemy, their drops, and all items on the screen, except for stage items"): it erases every gem, coin, bag, chicken, magnet, freeze and chest on screen, but not stage items and no enemy. The VS keep roll applies, with the boss's mirrored Luck: x ~ U[0,1] rounded to 2 decimals, and items go when x / Luck > the keep threshold (10% at level 1; 25/45/65% at levels 3/5/7) (`GroundItemEraser`, `StageItemMark`).
  - The player's own Pentagram does not erase ground items yet; that is a Phase 1 row, and it will use the same `GroundItemEraser`.
- Tests: `ShadowKaelPlayModeTests.MirroredClockLancet_SlowsThePlayerToHalfSpeed_ForItsFreezeTime` and `MirroredPentagram_ErasesItemsOnScreen_ButNotStageItems_NorTheHorde` (R); `PentagramItemRollTests` (T, the wiki's own worked examples).
- Validation: EditMode 485/485, PlayMode 197/197, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20260930-phase0b/`). The first full run failed the dash test (the interpolation defect above); the Pentagram test first failed because the boss's own first shot had already erased the items, so the test now stops the boss weapon's timer.
- Editor play `autoplay-editor-82` (Stage 10, level-60 hero, god mode, boss at 40 s, 2.5 min): Shadow Kael copied Whip, Runetracer, Knife, Bible, Magic Wand and a level-6 Pentagram; 0 errors, min 14 FPS in the GUI editor at 300 enemies (level-60 kill rate; known performance note).
- Seen, not changed (Art section): Stage 10's paths look flat bright cyan in that run.
- Not yet played by the owner.

### Ground late to appear; level-up pause (owner reports, 2026-09-30)

**"ground late se spawn ho raha hai".** `InfiniteMap` kept a 3 × 3 grid of 20-unit tiles, which reaches only 20 units past the hero near a tile edge. The 55-degree camera shows about 21 units to each side at 16:9 (28 at 21:9), so a screen edge showed bare background until the far tile wrapped over. The grid is now 5 × 5 (`InfiniteMap.GridSize`), reaching at least 40 units every way; a tile more than half the grid away wraps to the other side.
- `GroundCoveragePlayModeTests` (R): the hero steps to tile edges in every direction, and every screen corner and edge midpoint must fall on a tile, with the camera at 21:9 (a batchmode window is narrow). It fails on the old 3 × 3 grid ("Screen point (0,0) ... shows no floor") and passes on 5 × 5.
- Editor play `autoplay-editor-81` (Stage 1, 30 s, `-sh-autoplay-early-shots`: a shot every 0.2 s for the first 3 s): floor to every screen edge from the first frame, 0 errors, min 60 FPS (partial `V`).

**Level-up pause** (owner: the game kept running behind the level-up screen). vampire.survivors.wiki/w/Level_up (checked 2026-09-30): "Upon leveling up, the game is paused ..." and "After selecting an item, the game resumes and the character gains a brief moment of invulnerability" (Patch 0.2.12). The wiki gives no length.
- `LevelUpPausePlayModeTests` (R): with a real level-up open for 1.5 s, the time scale stays 0; the stage clock, enemy count, enemies, gems, the player, projectiles, every world transform, animator and game-time particle stay still. The test passed on the existing code, so the pause itself was not reproduced as broken; what the owner saw is still unexplained and needs the owner to show it (only the chest and level-up effects run on unscaled time, by design).
- Added: after the pick, the hero is invulnerable for its hit invulnerability (0.5 s fallback; Soul Hunter's number) (`LevelUpManager.ResumeGameplay`, `HealthController.GrantInvulnerability`).

Validation: EditMode 483/483, PlayMode 192/192, 0 exceptions, save untouched (`D:/SoulHunter-Validation/run-20260930-ground-levelup2/`).

Not yet played by the owner.

### VFX v2 sprites and VS-style level-up cards (owner-approved, 2026-09-26)

The owner approved the VFX v2 concept sheet: eight Soul Hunter sprites made procedurally by `ArtSource/Generators/build_vfx_textures.py` (soul flame, slash arc, magic circle, impact star, soul wisp, lightning bolt, shockwave ring, sparkle), written to `Assets/Art/Textures/VFX/SH10_Vfx2_*.png`. `WeaponVfxBuilder` now uses them:
- sparkles and shock rings everywhere;
- a spinning magic circle on level-up, on Pentagram and on the Holy Water pool rim;
- an impact star for hits and lightning, and soul wisps rising from dead enemies;
- soul flames for fireballs and explosions (explosions also get a shockwave).

The Whip had no visual at all; each lash now plays a slash arc along the attack and scaled to its area (`VfxKind.Slash`, `VfxQuadFade`, `VFXPoolManager.Play(kind, position, rotation, scale)`). Shadow Kael's mirrored Whip shows it too.

The owner also asked for a VS-style level-up screen, found while playing in the editor. Cards were white buttons with internal names ("SongOfMana Lv.2 (Lv 2)") and placeholder descriptions. `LevelUpUI.StyleCard` now shows the icon, a readable name (`UpgradeText.DisplayName`), a NEW!/Level N tag and a description. Weapons that are new describe what they do (Soul Hunter wording from each weapon's code); passives show their real per-level effect from `WeaponManager.ApplyUpgrade`. Descriptions written in the asset are kept. Old TMP and legacy `Text` labels are hidden, the cards use the focus colours, and Reroll/Skip/Banish are dark with white text.

The HP bar strip was left unchanged in that change. The owner later asked for the HP bar under the player (2026-09-26); see below.

Validated by `WeaponVfxTests` (T, Slash included) and `WeaponSweepPlayModeTests` (R: the Whip shows its slash; the level-up cards show an icon, a name without "Lv", a NEW!/Level tag, a real description and no stray labels). Both suites pass: EditMode 344/344, PlayMode 64/64, save unchanged (`D:\SoulHunter-Validation\run-20260925-vfx2\`). Editor play runs `autoplay-editor-02` and `-03` had 0 errors, and their screenshots were checked (partial `V`).

### HP bar under the player (owner request, 2026-09-26)

The player's health bar was a red strip across the whole bottom of the screen. It is now a small VS-style bar (96x11 canvas units, dark background, red fill) just under the hero's feet. `HealthBarFollow` (attached by `GameHUDController`) reuses the same HUD slider, reparents it to the root canvas (drawn under the menus) and moves it every `LateUpdate`, after `CameraFollow`. It is not interactable and hides its handle. Validated by `PlayerPresentationPlayModeTests.HealthBar_IsSmall_AndFollowsUnderTheHero` (R: narrower than 15% of the screen, centred on the feet and just below them while walking, full at start). Both suites pass: EditMode 344/344, PlayMode 65/65, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-hpbar\`). Editor play runs `autoplay-editor-04` and `-05` had 0 errors (`-05`: 3 min as Kael, level 14, 750 kills, min 65 FPS), and their screenshots show the bar under the hero (partial `V`).

Seen in `autoplay-editor-05` and fixed (owner approved, 2026-09-26):
- Eight `Assets/Art/UI` icon sprites had their file name drawn under the picture ("VFX_Slash.png", "Bone.png", "Crosshair.png", "Lamp_Post.png", "VFX_Portal.png", "VFX_Heal.png" and two small marks): sprite_40, 48, 88, 93, 77, 44, 35 and 102, used by Whip, Bone, Cherry Bomb, Gun, Candelabrador, Attractorb, Tiragisu and Skull O'Maniac. Only those label rows were cleared; the picture pixels are unchanged. `CharacterStatBlockTests.NoUpgradeIcon_ShowsAFileNameLabel` (T) failed on the old icons and passes now.
- Reroll/Skip/Banish text was red: their label is a legacy `Text` copied from the card, and only TMP labels were turned white. `LevelUpUI.WhitenLabels` now turns both white.
- The level-up panel's "LEVEL UP" text was drawn over the HUD stage name. `LevelUpUI.StyleHeading` shows a gold "LEVEL UP!" lower down, above the cards.

`WeaponSweepPlayModeTests` (R) checks the white labels, and that the heading is above the cards and below the stage name. EditMode 345/345, PlayMode 65/65, 0 exceptions, save unchanged (`D:\SoulHunter-Validation\run-20260926-uifix\`). Editor play `autoplay-editor-06` had 0 errors, and its level-up screenshot shows all three fixes (partial `V`).

### Validation run 2026-09-24

Commits `0beae7f`..`6f3f9c2` plus the pickup-pool fix were validated in an isolated workspace (`D:\SoulHunter-Validation\editmode-20260924-134853\`, which keeps the results XML and logs), with `Library` copied from the canonical project:

- `C` compile: 0 errors (the pixel-perfect package did not fail with the copied `Library`);
- `T` EditMode: 182/182 passed;
- `R` PlayMode: 5/5 passed (bootstrap, chest flow, game over/restart, time freeze, Wandering Merchant), 0 exceptions logged.

The first PlayMode run crashed Unity: the gold coin pool's prewarm loop never filled because copies of its inactive template never fire `OnDisable`. Fixed in `PickupPoolManager.PrewarmPool` and covered by `PickupPoolPrewarmTests`.

Still `UNKNOWN`: manual play of the new systems (level-up screen layout, elites, gold, evolutions, boss-advance), audio, `V`, `P` and `B`.

---

## 8. Unity Validation Status

Current project editor:

`D:\6000.0.36f1\Editor\Unity.exe`

An isolated fresh import was performed from an external source snapshot captured 2026-09-11 (that snapshot is no longer available).

Observed result:

- asset import completed;
- Unity batch process ultimately exited successfully;
- compilation emitted errors in `com.unity.2d.pixel-perfect` 5.0.3 Editor converter code referencing the old `UnityEngine.Experimental.Rendering.Universal.PixelPerfectCamera` namespace.

This matches Unity issue `UUM-77628`, fixed in Unity `6000.0.43f1`.

Therefore:

- do not automatically classify these package errors as a Soul Hunter gameplay-code defect;
- Unity `6000.0.36f1` remains the current project editor and may continue to be used;
- a clean package/editor compile baseline remains unresolved;
- do not force an editor upgrade merely to begin normal task work unless the task actually depends on closing this validation gate.

---

## 9. Requirement / Acceptance Rules

Evidence classes:

- `S` — static/source/serialized
- `C` — import/compile
- `T` — deterministic test/validator
- `R` — runtime/PlayMode
- `V` — visual/UX
- `P` — performance/profile
- `B` — player/build/release

Rules:

- static existence != runtime `PASS`;
- agent claim != proof;
- `UNKNOWN` != `PASS`;
- missing mandatory evidence remains `UNKNOWN`, `BLOCKED`, or `CONDITIONAL`;
- compile/test failure is `FAIL` for that acceptance path;
- known defects remain `FAIL` until corrected and revalidated;
- do not convert warnings/success logs into acceptance without required evidence;
- every meaningful accepted change should be traceable from requirement -> implementation -> evidence -> checkpoint.

The broad Requirement -> Acceptance matrix exists, but final row-by-row reconciliation of every authoritative product requirement is still incomplete. This does not require a full re-audit before ordinary development tasks.

---

## 10. Development Workflow

For a normal requested task:

1. Read this `AGENTS.md`.
2. Inspect only the files/systems relevant to the task.
3. Confirm the requirement and acceptance criteria.
4. Check for direct conflicts with approved decisions.
5. Choose the correct agent/tool.
6. Prefer an isolated workspace for risky or broad mutations.
7. Implement the smallest coherent change.
8. Validate at the evidence levels actually required by the change.
9. Report failures/unknowns truthfully.
10. Record evidence and checkpoint only after acceptance.

Do **not** perform a full-project audit before every feature, bug fix, UI change, asset task, or refactor.

Escalate to the owner only when:

- requirements genuinely conflict;
- a destructive action is necessary;
- permissions must expand;
- a product/design choice has no authoritative answer;
- acceptance cannot be reached without an owner decision.

### Bug fixes must use the Unity CLI (owner rule, 2026-09-24)

Every Soul Hunter bug fix is reproduced and verified with the Unity command-line test runner. Reading code or compiling outside Unity (`dotnet build`) is not enough: several bugs (the pickup-pool startup hang, the Whip's empty enemy mask, the Bible `InvalidCastException`, weapon self-damage) only showed up in real Unity runs.

For each bug fix:

1. Add or extend an EditMode or PlayMode test that reproduces the bug.
2. Run it against an isolated copy of the project, never the canonical path (the owner's editor keeps it open; never delete its lock or close it):
   - copy `Assets`, `Packages`, `ProjectSettings` and `Library` to a folder under `D:\SoulHunter-Validation\` (copying `Library` avoids the pixel-perfect package compile failure seen on fresh imports), then sync changed files into it;
   - `D:\6000.0.36f1\Editor\Unity.exe -batchmode -nographics -projectPath <copy> -runTests -testPlatform EditMode -testResults <run>\editmode-results.xml -logFile <run>\editor-edit.log`
   - `D:\6000.0.36f1\Editor\Unity.exe -batchmode -projectPath <copy> -runTests -testPlatform PlayMode -testResults <run>\playmode-results.xml -logFile <run>\editor-play.log`
3. Always run both suites: scene-startup bugs are invisible to EditMode.
4. Report the pass counts, compile errors and exceptions logged.
5. **When to commit and when to run the full PlayMode suite** (owner, 2026-10-15: "haan niyam badlo"; the full PlayMode suite takes about 80 minutes, because each of its ~445 tests boots the real game):
   - A commit needs the full EditMode suite and the PlayMode fixtures the change touches (`-testFilter "FixtureA|FixtureB"`), all passing, with the real save unchanged. The commit message names the fixtures that ran.
   - The full PlayMode suite runs before every push or build for others, and at least once a day while work goes on. A failure it finds is fixed in the next commit.
   - The save check stays on every run.

PlayMode tests boot the real game and share the canonical save file (`AppData/LocalLow/Nimrita Games/Soul Hunter/soulhunter_save.json`); tests must not write it deliberately.

### DOT

DOT means **Detail-Oriented Total Implementation**.

For relevant work consider:

- properties/data
- behavior
- state/timing
- inputs/outputs
- dependencies
- interactions
- edge cases
- errors
- UI
- audio
- VFX/feedback
- persistence
- compatibility
- integration
- performance
- regression
- documentation

Irrelevant dimensions may be explicitly `NOT_APPLICABLE`.

Code complete does not automatically mean DOT complete.

---

## 11. Asset / Art Workflow

Installed/useful local capability already confirmed:

- Blender 5.2.1

Web/design sources such as Dribbble, ArtStation, Behance, Pinterest, Figma, Canva, Sketchfab, Fab, Unity Asset Store, etc. are references or asset sources depending on their licenses.

Do not copy protected artwork merely because it is visible online.

Before adding a new external asset to the project:

- verify license / commercial-use rights;
- preserve attribution requirements if any;
- prefer source files suitable for the release pipeline;
- validate import cost, materials, shader compatibility, LOD/polycount where relevant;
- keep Soul Hunter's own art direction rather than copying another title.

Install additional tools only when they materially improve the active task.

---

## 12. Current Operating Direction

The expensive full static audit has already been completed.

Do not repeat it unless the owner explicitly requests a new full audit or the project state changes enough to invalidate the current verified baseline.

The only active plan is `Docs/VS_DOT_Plan.md` (owner, 2026-09-30: every older pending plan was removed). Work follows it, one row at a time.

---

## 13. Non-Negotiable Truth Rule

Optimize for truth, not for a positive verdict.

A truthful `FAIL`, `UNKNOWN`, or `BLOCKED` is better than a false `PASS`.

Do not claim a feature, build, performance target, tool, or release state is complete unless the required evidence exists.
