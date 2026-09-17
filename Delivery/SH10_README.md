# Soul-Hunter — Campaign Art v01

This is an original, procedural low-poly **art starter pack for all ten campaign themes**. It is not a tested, complete game and is not a claim of 100% Vampire Survivors feature parity.

## Contents

- 10 level packs: each contains 1 ground tile, 5 themed props, and 3 humanoid enemy/elite/boss visual variants (90 assets).
- Shared: 1 player visual, 24 weapon visual assets, 6 pickup visual assets (31 assets).
- Total: 121 model assets / visual prefabs, including 31 generically rigged actor variants.
- Each actor has basic Idle, Run, Attack and Death skeletal clips plus an Animator Controller. These are simple procedural clips with rigid armor weights, not polished motion-capture or final character animation.
- Level showcase scenes are art previews, not playable campaign levels. They contain no spawner, combat, input, save or progression setup.

## Import and use

1. Back up your Unity project.
2. Import the relevant `.unitypackage` through Assets > Import Package > Custom Package.
3. The package preserves the project's folder paths. Find `SH10_` prefabs under `Assets/Prefabs/Environment`, `Enemies`, `Bosses`, `Player`, `Weapons` and `Items`.
4. Drag a prefab into a scene to place its **visuals**. Enemy/player visual prefabs do not contain gameplay controllers or damage scripts. Weapon and pickup visuals do not implement their gameplay effects.
5. Open `Assets/Scenes/Levels/SH10_Lxx_Showcase.unity` to inspect the themed set. These scenes are not in Build Settings. In the original Soul-Hunter project, Play may still start Bootstrap regardless of which showcase is open.

Each level folder includes editable Blender sources, FBX models, a preview PNG and its importable Unity package. Shared contains common visual assets. Materials and controllers are included in the Unity packages; no hand-painted texture set was created. Import packages rather than loose FBX if you want the assigned Unity materials/controllers.

Do not replace current gameplay prefabs blindly. Add these visuals as children/variants and preserve existing gameplay roots, tags, layers, collision and data references. Facing and weapon aiming still need integration fixes in the original code.

## Technical boundaries

- New assets use the `SH10_` prefix; current gameplay assets are not replaced.
- Static environment prefabs have mesh colliders. Collision navigation/horde performance needs final testing. Gates, spikes, water and hazards are visual geometry, not interactive gameplay implementations.
- Actors are Generic rigs, not certified Humanoid avatars. Configure Speed (float), Attack (trigger) and Death (trigger) through an integration layer. Root motion is off.
- Art uses palette colors and a simple faceted preview shader. It is not a finished URP lighting/shadow pipeline or a PBR texture production pass.
- Bosses are themed humanoid art variants; boss AI and unique attacks are not included. Several weapons share modeling motifs; these are starter visuals, not final bespoke weapon/VFX designs.
- The ground is a repeatable 10-metre tile. Showcase scenes demonstrate placement; they are not final level layouts.
- Import/reference checks and sampled Run mesh deformation were performed. Final gameplay, all animation transitions, balance, target-device performance and a standalone build are NOT certified.

## Original project's remaining work

The prior audit still applies: incomplete upgrade application/data, some missing weapon routes, wrong target filtering for some area weapons, possible duplicate projectile damage, incomplete shield/Arcana behaviors, and stage-specific environment/boss integration. New art does not fix these automatically.

See `Verification/` for import/skin checks and the source manifest. See the supplied audit for the prior integration blockers.

Status: **first 10-level art batch delivered; full gameplay-ready production remains incomplete**.
