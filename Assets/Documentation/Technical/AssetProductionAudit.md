# Soul-Hunter: asset production baseline and first review kit

Date: 2026-09-08. Status: dependency audit + static art-review milestone, NOT a finished 10-level game.

## Safety baseline

- Verified on-disk snapshot: `Backups/AssetProduction/20260908-123209/`.
- 1,521 files, 61,292,951 bytes; each copy checked against its source with SHA-256.
- Includes Assets, Packages and ProjectSettings. Does not include Library, editor caches, unsaved work, or the external player save file.
- `manifest.csv` records original relative paths, lengths and SHA-256 hashes. Restore selectively with Unity closed, after backing up newer work; do not merge the whole snapshot blindly.
- Existing gameplay scripts/scenes/data/prefabs were not intentionally modified during this milestone. No old assets deleted. New assets use the `L01R_` prefix.
- Existing Play-start scene and build scenes are retained. The new review scene is not added to the campaign.
- Post-generation verification compared all 1,521 baseline paths and SHA-256 hashes: **0 changed, 0 missing**. Additions are separate from the baseline. Review scene missing-script check: 0; preview shader error check: false; Unity console error check after import: 0.

## Evidence and scope

`Logs/AssetProductionAudit-20260908-070313.json` is the live Unity inventory taken BEFORE adding review models: 31 prefab files and 43 project ScriptableObjects in the inspected Data/Resources folders, plus the active gameplay scene. Missing-script count was zero in those prefabs. Null references are not automatically defects: Unity metadata fields and optional/runtime-bound references are expected to be null.

Source inspection and serialized-reference checks do not establish gameplay equivalence with Vampire Survivors. Full end-to-end coverage of every weapon, passive, evolution, boss and stage is still pending. Earlier basic gameplay smoke tests do not count as those tests.

## Confirmed production blockers

| Finding | Evidence | Required action |
|---|---|---|
| Only six weapons are routed by the central manager | WeaponManager.GiveWeapon / ApplyUpgrade switches | Preserve manager, add explicit routing and consistent progression for the intended roster |
| Every selected character starts with Magic Wand | WeaponManager.Start forces MagicWand; StartingWeapon assignment is commented | Use the selected CharacterData from the existing catalog |
| Existing weapon upgrade branch only logs | WeaponManager.ActivateOrUpgradeWeapon | Apply level changes to weapon behavior; test observable damage/rate/count changes |
| Three existing weapon model slots are empty | Axe_Weapon._axePrefab, Bible_Weapon._biblePrefab, Cross_Weapon._crossPrefab | Assign validated model/projectile prefabs |
| No authored level 2+ upgrade data | Eight UpgradeData assets: six level-1 entries, two level-0 placeholders | Author complete progression data; do not offer silent no-op choices |
| Evolution/union data have no result prefab | All six evolution and six union assets have null result-prefab references | Check recipes, create compatible evolved prefabs, connect and test replacement |
| Wrong damage target for some player weapons | Bible, SantaWater, SongOfMana, BloodyTear use TouchDamage; TouchDamage only accepts Player collisions | Separate player-target enemy contact from enemy-target player area damage; test both sides |
| Multiple possible projectile damage owners | Projectile and ProjectileDamage both implement trigger damage; several weapon scripts add both | One damage authority per projectile, with damage-stat propagation and source tracking |
| Magic Wand stat propagation mismatch | Base damage passed to Projectile.Initialize; Might-adjusted value only assigned to optional ProjectileDamage | Ensure the active damage owner receives the adjusted damage |
| Laurel shield is a stub | LaurelWeapon.Start contains simulated interception; no charge consumption hook | Implement actual damage interception and recharge tests before marking shield ready |
| Some Arcana effects only log | ArcanaManager IronBlueWill/Gemini/WaltzOfPearls branches | Wire actual behavior or explicitly keep unavailable; card art alone is not a feature |
| 2D-facing assumptions remain | PlayerController mirrors root scale; Whip/Axe/Cross/Knife/ThousandEdge depend on scale sign | Keep physics root stable; establish 3D visual-facing/aim contract without breaking weapon direction |
| Some projectile/orbit axes are inconsistent | Peachone/Vandalier orbit in XY; CherryBomb passes Vector2 to Vector3 projectile API | Validate movement in the game's XZ plane |
| Boss is currently a scaled regular enemy | EnemySpawner uses current-wave EnemyPrefab, scales it and changes HP | Stage-specific boss data/prefab routing; then behavior tests |
| Three waves reference one enemy prefab | WaveData references in live inventory | Separate enemy roster and stage-specific wave data |
| No imported clips in the two character controllers | AnimatorController.animationClips empty for both; Speed/Attack parameters exist | Rig characters and produce compatible clips/controllers |
| Magnet/time-freeze drop prefabs unassigned | Gameplay PickupPoolManager fields | Create and test pickups with existing collection scripts |
| Stage names are not complete environments | Campaign catalog has ten definitions; progression raises events; inspected world code has no stage-art switching | Add stage presentation/environment binding after the sample is approved |

Additional stats, revival consumers, special hazards, achievements, audio and performance need dedicated runtime coverage. No claim that the above table lists every defect.

## Weapon scope discovered

24 weapon source files exist. Six have current controller prefabs: MagicWand, Garlic, Whip, Axe, Bible, Cross. These are not all functionally complete.

The other 18 source files are: BloodyTear, Bone, CherryBomb, ClockLancet, DeathSpiral, FireWand, Gun, HolyWand, Knife, Laurel, LightningRing, Peachone, Pentagram, Runetracer, SantaWater, SongOfMana, ThousandEdge, Vandalier. No matching controller components appeared in the baseline prefab inventory. A source file is not a ready-to-use prefab.

Recipe data also names HeavenSword, SoulEater and UnholyVespers; corresponding named weapon source files were not found in the inspected weapon folder. This mismatch must be reconciled before declaring a full roster.

## Asset/code contract

- Keep the existing XZ movement plane and 3D Rigidbody/Collider foundation.
- Separate visual meshes from gameplay roots, hitboxes and targeting. Do not let artist scale/rotation silently change collision or aim.
- Retain the existing project architecture and service/event/save framework.
- Rigged actors should support the current Speed float and Attack trigger; death/hit/dash clips need explicit integration decisions.
- Each transient weapon prefab needs a documented owner for movement, damage and pool lifetime.
- Environment decoration should not accidentally block horde paths. Collision is a separate validated production step.
- Shared weapons/loot should not be copied ten times. Level-specific content should reference shared prefabs.
- Render-pipeline changes are NOT part of this review. The preview shader is explicitly a simple faceted art-preview shader, not a final lighting pipeline.

## First review delivery (new, additive)

14 original low-poly static models, 4,866 total source triangles:

- Environment: Gravestone, CrossGrave, Coffin, Crypt, Gate, Fence, DeadTree, RockCluster, SoulLantern.
- Weapon/loot studies: SoulWand, XPGem, TreasureChest.
- Actor shape studies: KaelStudy, GraveGhoulStudy.

Locations:

- Editable per-asset Blender source: `ArtSource/L01_Review_v01/*.blend`.
- Source geometry manifest: `ArtSource/L01_Review_v01/manifest.json`.
- Imported models: `Assets/Art/Models/L01R_*.fbx`.
- Unity materials: `Assets/Art/Materials/L01R_*.mat`.
- Individual visual prefabs: `Assets/Prefabs/{Environment,Weapons,Items,Characters}/L01R_*.prefab`.
- Review scene: `Assets/Scenes/Test/L01_AssetReview.unity`.
- Screenshot: `Logs/L01Review.png`.
- Import bounds report: `Logs/L01ReviewImport.json`.

These are **static visual-review prefabs**, not finished gameplay prefabs. They have no character rig, animation clips, gameplay scripts, damage setup or production collision. They can be dragged into a scene to inspect the art. They must not replace the current playable Player/Enemy/Weapon prefabs yet. Existing runtime fallback visuals remain unchanged.

The source meshes are UV-unwrapped and use palette materials, not hand-painted texture sets. The modular crypt door/gate/treasure lid are not interactive moving parts in this first review. Actor shapes are proportion/color studies, not final character sculpts.

To view: open the review scene and inspect Scene/Game view in Edit mode, or open the screenshot. Play remains configured to start Bootstrap; pressing Play does not turn this review scene into gameplay.

## Remaining staged plan

1. Review the static art direction before producing the other nine level kits. Approval concerns the look, not gameplay completeness.
2. Build an isolated Level 1 playable test scene with a rigged player/enemy and a small verified weapon/loot subset. Preserve current playable scene as baseline.
3. Fix the integration blockers with narrowly scoped patches and deterministic checks (damage, target filtering, upgrades, pool reuse, facing).
4. Complete and test the remaining weapon/evolution/passive roster; author full progression data.
5. Produce level-specific environment/enemy/boss assets for all ten stages, sharing common assets.
6. Verify full stage transitions, death/restart, rewards, hazards, saves and performance on the target hardware.

No full rewrite, destructive folder reorganization, bulk replacement or promise of 100% feature parity is justified by the current evidence.
