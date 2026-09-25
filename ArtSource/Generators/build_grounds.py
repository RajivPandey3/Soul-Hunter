"""Soul Hunter stage floors: one seamless 20x20 ground tile per level for InfiniteMap.
Run: blender --background --python ArtSource/Generators/build_grounds.py [-- 1 2 ...]
Faceted low-poly style like the SH10 packs. Tile edges are the plain base colour (or a grid whose
period divides 20), so tiles join without seams in any 90-degree rotation. Decor is flat or very
low so it never reads as an obstacle; the Unity prefab adds the only collider.
Set SOULHUNTER_ROOT to run against an isolated copy of the project.
"""
import bpy
import bmesh
import json
import math
import os
import random
from pathlib import Path

ROOT = Path(os.environ.get("SOULHUNTER_ROOT") or Path(__file__).resolve().parent.parent.parent)
OUT = ROOT / 'ArtSource' / 'SH10_Floors_v01'
MODELS = ROOT / 'Assets' / 'Art' / 'Models'
PREVIEWS = OUT / 'Previews'
SIZE = 20.0
HALF = SIZE / 2

palette = {}
materials = {}
parts = []


def material(key):
    if key not in materials:
        m = bpy.data.materials.new('Floor_' + key)
        m.diffuse_color = (*palette[key], 1)
        materials[key] = m
    return materials[key]


def finish(obj, name, mat):
    obj.name = name
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    obj.data.materials.append(material(mat))
    parts.append(obj)
    return obj


def box(name, pos, size, mat, rot_z=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos, rotation=(0, 0, rot_z))
    obj = bpy.context.object
    obj.scale = size
    return finish(obj, name, mat)


def cone(name, pos, radius, top, height, mat, verts=6, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=radius, radius2=top, depth=height, location=pos, rotation=rot)
    return finish(bpy.context.object, name, mat)


def gem(name, pos, scale, mat, rot_z=0.0):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1, location=pos, rotation=(0, 0, rot_z))
    obj = bpy.context.object
    obj.scale = scale
    return finish(obj, name, mat)


def patch(name, x, y, radius, mat, rng, z=0.004, points=9, squash=1.0):
    """Flat irregular polygon lying on the ground (a colour patch, puddle, stain...)."""
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    verts = []
    rot = rng.uniform(0, math.tau)
    for i in range(points):
        a = rot + i * math.tau / points
        r = radius * rng.uniform(.7, 1.15)
        verts.append(bm.verts.new((x + r * math.cos(a), y + r * math.sin(a) * squash, z)))
    bm.faces.new(verts)
    bm.to_mesh(mesh); bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    return finish(obj, name, mat)


def spot(rng, margin):
    """Random point that keeps an object of this radius inside the tile (seamless edges)."""
    m = HALF - margin
    return rng.uniform(-m, m), rng.uniform(-m, m)


def base():
    box('Base', (0, 0, -.1), (SIZE, SIZE, .2), 'Base')


def scatter_patches(rng, count, mats, rmin, rmax, z=.004, squash=1.0):
    for i in range(count):
        r = rng.uniform(rmin, rmax)
        x, y = spot(rng, r * 1.2)
        patch('Patch', x, y, r, rng.choice(mats), rng, z + i * 0.0002, rng.randint(7, 11), squash)


def pebbles(rng, count, mat, smin=.06, smax=.18):
    for _ in range(count):
        s = rng.uniform(smin, smax)
        x, y = spot(rng, s + .1)
        gem('Pebble', (x, y, s * .15), (s, s * rng.uniform(.6, 1), s * .45), mat, rng.uniform(0, math.tau))


def tufts(rng, count, mat, height=.25):
    for _ in range(count):
        x, y = spot(rng, .4)
        for _ in range(rng.randint(3, 5)):
            dx, dy = rng.uniform(-.15, .15), rng.uniform(-.15, .15)
            h = height * rng.uniform(.6, 1.2)
            cone('Blade', (x + dx, y + dy, h / 2), .035, 0, h, mat, 3,
                 (rng.uniform(-.35, .35), rng.uniform(-.35, .35), 0))


def slabs(rng, cell, gap, mats, z_jitter=.03, missing=0.0, crack_mat=None):
    """Grid of stone slabs; cell divides SIZE so the grid continues across tiles."""
    n = int(round(SIZE / cell))
    for i in range(n):
        for j in range(n):
            if rng.random() < missing:
                continue
            cx, cy = -HALF + cell * (i + .5), -HALF + cell * (j + .5)
            h = .06 + rng.uniform(0, z_jitter)
            box('Slab', (cx, cy, h / 2), (cell - gap, cell - gap, h), rng.choice(mats))
            if crack_mat and rng.random() < .25:
                a = rng.uniform(0, math.pi)
                box('Crack', (cx, cy, h + .002), (cell * .7, .04, .004), crack_mat, a)


# ---------- level themes ----------

def l01(rng):  # The Cursed Graveyard
    base()
    scatter_patches(rng, 44, ['DeadGrass', 'GraveDirt', 'DeadGrass'], .35, 1.1)
    for _ in range(6):  # sunken grave plots
        x, y = spot(rng, 1.4)
        a = rng.choice([0, math.pi / 2])
        box('GravePlot', (x, y, .02), (1.0, 2.0, .04), 'GraveDirt', a)
    tufts(rng, 26, 'Grass')
    pebbles(rng, 30, 'Stone')
    for _ in range(7):  # scattered bones
        x, y = spot(rng, .5)
        a = rng.uniform(0, math.tau)
        box('Bone', (x, y, .03), (.45, .06, .06), 'Bone', a)
        gem('BoneEnd', (x + .22 * math.cos(a), y + .22 * math.sin(a), .04), (.06, .06, .05), 'Bone')
    scatter_patches(rng, 5, ['Glow'], .15, .35, z=.01)


def l02(rng):  # The Dark Forest
    base()
    scatter_patches(rng, 48, ['Soil', 'Leaves', 'Leaves'], .35, 1.0)
    for _ in range(8):  # surface roots
        x, y = spot(rng, 1.6)
        a = rng.uniform(0, math.tau)
        for k in range(3):
            box('Root', (x + k * .4 * math.cos(a), y + k * .4 * math.sin(a), .05), (.55, .12, .10), 'Root', a + rng.uniform(-.3, .3))
    for _ in range(16):  # ferns: flat stars
        x, y = spot(rng, .6)
        for k in range(5):
            a = k * math.tau / 5 + rng.uniform(-.2, .2)
            box('Frond', (x + .22 * math.cos(a), y + .22 * math.sin(a), .03), (.45, .09, .02), 'Fern', a)
    for _ in range(10):
        x, y = spot(rng, .3)
        cone('Stem', (x, y, .07), .03, .025, .14, 'Bone', 5)
        gem('Cap', (x, y, .16), (.09, .09, .045), 'Glow')
    pebbles(rng, 16, 'Stone')


def l03(rng):  # The Forgotten Village: cobbled streets crossing every tile
    base()
    scatter_patches(rng, 30, ['Dust', 'Dirt'], .35, .9)
    street = 3.2
    for axis in (0, 1):
        for k in range(int(SIZE / .5)):
            for w in range(int(street / .5)):
                along = -HALF + .25 + k * .5
                across = -street / 2 + .25 + w * .5
                x, y = (along, across) if axis == 0 else (across, along)
                if axis == 1 and abs(y) < street / 2:
                    continue  # crossing already cobbled
                s = rng.uniform(.25, .29)
                gem('Cobble', (x + rng.uniform(-.03, .03), y + rng.uniform(-.03, .03), .03), (s, s, .05),
                    rng.choice(['Cobble', 'Cobble', 'CobbleDark']), rng.uniform(0, math.tau))
    for _ in range(9):  # hay and straw
        x, y = spot(rng, 1)
        if abs(x) < street or abs(y) < street:
            continue
        patch('Hay', x, y, rng.uniform(.4, .7), 'Hay', rng, .01)
    tufts(rng, 14, 'Grass', .2)
    pebbles(rng, 20, 'CobbleDark')


def l04(rng):  # The Ruined Castle: cracked flagstones
    base()
    slabs(rng, 2.5, .12, ['Flag', 'Flag', 'FlagDark'], .04, missing=.08, crack_mat='Base')
    scatter_patches(rng, 6, ['Moss'], .3, .8, z=.1)
    for _ in range(18):  # rubble
        x, y = spot(rng, .4)
        s = rng.uniform(.12, .3)
        gem('Rubble', (x, y, .1 + s * .3), (s, s * .8, s * .6), rng.choice(['Flag', 'FlagDark']), rng.uniform(0, math.tau))
    for _ in range(5):
        x, y = spot(rng, .3)
        gem('RuneShard', (x, y, .12), (.07, .07, .04), 'Glow')


def l05(rng):  # The Crimson Swamp
    base()
    scatter_patches(rng, 40, ['Mud', 'MudDark'], .4, 1.1)
    scatter_patches(rng, 14, ['Puddle'], .45, 1.0, z=.012, squash=.75)
    for _ in range(14):  # lily pads
        x, y = spot(rng, .4)
        cone('Pad', (x, y, .02), rng.uniform(.18, .3), rng.uniform(.18, .3), .02, 'Pad', 7)
    for _ in range(12):  # reed stubs
        x, y = spot(rng, .3)
        for _ in range(rng.randint(2, 4)):
            h = rng.uniform(.25, .5)
            cone('Reed', (x + rng.uniform(-.1, .1), y + rng.uniform(-.1, .1), h / 2), .03, .015, h, 'Reed', 4)
    for _ in range(10):
        x, y = spot(rng, .2)
        gem('Bubble', (x, y, .02), (.06, .06, .03), 'Glow')


def l06(rng):  # The Frozen Peaks
    base()
    scatter_patches(rng, 32, ['SnowShade', 'Snow'], .45, 1.2)
    scatter_patches(rng, 12, ['Ice'], .45, 1.0, z=.012)
    for _ in range(9):  # drifts
        x, y = spot(rng, 1.2)
        gem('Drift', (x, y, .0), (rng.uniform(.6, 1.1), rng.uniform(.3, .6), .12), 'Snow', rng.uniform(0, math.tau))
    for _ in range(12):  # rocks poking through
        x, y = spot(rng, .5)
        s = rng.uniform(.15, .35)
        gem('Rock', (x, y, s * .2), (s, s * .8, s * .5), 'Rock', rng.uniform(0, math.tau))
    for _ in range(6):
        x, y = spot(rng, 1)
        box('IceCrack', (x, y, .016), (rng.uniform(.8, 1.5), .03, .004), 'Glow', rng.uniform(0, math.pi))


def l07(rng):  # The Sea of Lost Souls
    base()
    scatter_patches(rng, 32, ['WetSand', 'Sand'], .45, 1.1)
    scatter_patches(rng, 12, ['Shallow'], .5, 1.1, z=.012, squash=.7)
    for _ in range(8):  # foam lines
        x, y = spot(rng, 1.2)
        a = rng.uniform(0, math.pi)
        box('Foam', (x, y, .016), (rng.uniform(.9, 1.8), .05, .004), 'Foam', a)
    for _ in range(14):  # shells
        x, y = spot(rng, .2)
        cone('Shell', (x, y, .04), .09, .0, .08, 'Foam', 5)
    scatter_patches(rng, 6, ['Glow'], .2, .45, z=.018)


def l08(rng):  # The Blood Arenas
    base()
    scatter_patches(rng, 34, ['SandDark', 'Sand'], .4, 1.0)
    bpy.ops.mesh.primitive_torus_add(major_radius=6.5, minor_radius=.08, major_segments=40, minor_segments=4, location=(0, 0, .0))
    obj = bpy.context.object; obj.scale = (1, 1, .1); finish(obj, 'ArenaRing', 'Line')
    for _ in range(12):  # blood splats: blob plus droplets
        x, y = spot(rng, 1)
        patch('Blood', x, y, rng.uniform(.3, .7), 'Blood', rng, .014)
        for _ in range(4):
            patch('Drop', x + rng.uniform(-.8, .8), y + rng.uniform(-.8, .8), rng.uniform(.05, .12), 'Blood', rng, .014, 6)
    for _ in range(6):  # broken blades
        x, y = spot(rng, .5)
        box('Blade', (x, y, .02), (.5, .07, .03), 'Steel', rng.uniform(0, math.tau))
    pebbles(rng, 16, 'SandDark')


def l09(rng):  # The Throne of Death: dark marble with gold inlay
    base()
    slabs(rng, 4.0, .16, ['Marble', 'MarbleDark'], .005)
    for i in range(5):
        for j in range(5):
            if (i + j) % 2 == 0:
                cx, cy = -HALF + 4 * (i + .5), -HALF + 4 * (j + .5)
                box('Inlay', (cx, cy, .075), (.6, .6, .01), 'Gold', math.pi / 4)
                box('InlayCore', (cx, cy, .08), (.34, .34, .01), 'Marble', math.pi / 4)
    for _ in range(4):
        x, y = spot(rng, .4)
        gem('SoulShard', (x, y, .1), (.08, .08, .05), 'Glow')


def l10(rng):  # The Soul Core: crystal plates with glowing veins
    base()
    slabs(rng, 2.0, .1, ['Plate', 'PlateDark', 'Plate'], .03)
    for _ in range(14):  # veins running across plates
        x, y = spot(rng, 2.2)
        a = rng.choice([0, math.pi / 2, math.pi / 4, -math.pi / 4])
        box('Vein', (x, y, .1), (rng.uniform(1.4, 3.6), .06, .01), 'Glow', a)
    for _ in range(10):
        x, y = spot(rng, .3)
        gem('Crystal', (x, y, .15), (.07, .07, .16), 'Glow', rng.uniform(0, math.tau))


LEVELS = {
    1: ('The Cursed Graveyard', l01, dict(Base=(.15, .14, .12), DeadGrass=(.23, .25, .15), GraveDirt=(.10, .09, .08), Grass=(.28, .32, .17),
                                          Stone=(.30, .32, .33), Bone=(.62, .60, .48), Glow=(.20, .85, .78))),
    2: ('The Dark Forest', l02, dict(Base=(.10, .17, .09), Soil=(.12, .10, .07), Leaves=(.26, .19, .08), Root=(.20, .13, .08),
                                     Fern=(.18, .36, .14), Bone=(.55, .52, .42), Glow=(.50, .85, .20), Stone=(.26, .28, .25))),
    3: ('The Forgotten Village', l03, dict(Base=(.30, .24, .16), Dust=(.36, .30, .21), Dirt=(.24, .18, .12), Cobble=(.42, .40, .37),
                                           CobbleDark=(.30, .28, .27), Hay=(.62, .50, .24), Grass=(.30, .34, .16))),
    4: ('The Ruined Castle', l04, dict(Base=(.12, .11, .14), Flag=(.36, .34, .38), FlagDark=(.26, .25, .29), Moss=(.20, .26, .14),
                                       Glow=(.60, .30, .95))),
    5: ('The Crimson Swamp', l05, dict(Base=(.20, .10, .10), Mud=(.26, .13, .12), MudDark=(.14, .07, .07), Puddle=(.32, .04, .08),
                                       Pad=(.22, .30, .12), Reed=(.36, .30, .16), Glow=(.75, .15, .25))),
    6: ('The Frozen Peaks', l06, dict(Base=(.74, .80, .87), Snow=(.88, .92, .96), SnowShade=(.62, .70, .80), Ice=(.50, .72, .90),
                                      Rock=(.28, .32, .38), Glow=(.30, .80, 1.0))),
    7: ('The Sea of Lost Souls', l07, dict(Base=(.15, .23, .25), WetSand=(.20, .28, .28), Sand=(.34, .38, .34), Shallow=(.08, .30, .36),
                                           Foam=(.70, .82, .80), Glow=(.20, .95, .85))),
    8: ('The Blood Arenas', l08, dict(Base=(.44, .32, .23), Sand=(.52, .40, .29), SandDark=(.34, .24, .17), Line=(.60, .50, .38),
                                      Blood=(.42, .03, .04), Steel=(.55, .56, .60))),
    9: ('The Throne of Death', l09, dict(Base=(.07, .05, .09), Marble=(.20, .16, .26), MarbleDark=(.14, .11, .19), Gold=(.72, .55, .20),
                                         Glow=(.75, .30, .95))),
    10: ('The Soul Core', l10, dict(Base=(.04, .06, .09), Plate=(.12, .18, .24), PlateDark=(.08, .12, .17), Glow=(.15, .95, .95))),
}


def preview(name):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'FLAT'
    scene.display.shading.color_type = 'MATERIAL'
    scene.render.resolution_x = scene.render.resolution_y = 512
    cam_data = bpy.data.cameras.new('TopCam'); cam_data.type = 'ORTHO'; cam_data.ortho_scale = SIZE
    cam = bpy.data.objects.new('TopCam', cam_data); bpy.context.collection.objects.link(cam)
    cam.location = (0, 0, 30); cam.rotation_euler = (0, 0, 0)
    scene.camera = cam
    scene.render.filepath = str(PREVIEWS / f'{name}.png')
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam); bpy.data.cameras.remove(cam_data)


def build(level):
    global materials, parts
    title, fn, colors = LEVELS[level]
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    for m in list(bpy.data.materials): bpy.data.materials.remove(m)
    materials, parts = {}, []
    palette.clear(); palette.update(colors)
    fn(random.Random(1000 + level))
    bpy.ops.object.select_all(action='DESELECT')
    for p in parts: p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    mesh = bpy.context.object
    asset = f'SH10_L{level:02}_Floor'
    mesh.name = asset
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    for key, m in materials.items(): m.name = asset + '_' + key
    preview(asset)
    mesh.select_set(True)
    source = OUT / 'Blender' / f'{asset}.blend'; source.parent.mkdir(parents=True, exist_ok=True)
    model = MODELS / f'{asset}.fbx'
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    bpy.ops.export_scene.fbx(filepath=str(model), use_selection=True, object_types={'MESH'},
                             axis_forward='-Z', axis_up='Y', apply_unit_scale=True)
    triangles = sum(len(p.vertices) - 2 for p in mesh.data.polygons)
    print('FLOOR_DONE', asset, triangles, flush=True)
    return dict(name=asset, level=level, title=title, size=SIZE, triangles=triangles,
                model=str(model.relative_to(ROOT)).replace('\\', '/'),
                materials={asset + '_' + k: list(palette[k]) for k in materials})


if __name__ == '__main__':
    import sys
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    levels = [int(a) for a in args] or list(LEVELS)
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    manifest_path = OUT / 'manifest.json'
    manifest = {m['name']: m for m in json.loads(manifest_path.read_text(encoding='utf-8'))} if manifest_path.exists() else {}
    for level in levels:
        entry = build(level)
        manifest[entry['name']] = entry
    manifest_path.write_text(json.dumps(sorted(manifest.values(), key=lambda m: m['level']), indent=2), encoding='utf-8')
    print('FLOORS_COMPLETE', len(levels), flush=True)
