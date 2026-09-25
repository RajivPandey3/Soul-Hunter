"""Soul Hunter stage floors v2: one seamless 20x20 tile per level for InfiniteMap, in Soul Hunter's own
low-poly style, with the density of a hand-made VS stage: bright walkable ground, a cross of paths that
meets every tile edge at its midpoint (so paths join in any 90-degree rotation), dense ground detail and
themed obstacles that block the player.

Run: blender --background --python ArtSource/Generators/build_grounds.py [-- 1 2 ...]
Set SOULHUNTER_ROOT to run against an isolated copy of the project.

Each FBX holds three meshes:
  SH10_Lxx_Floor            walkable ground (one BoxCollider is added in Unity)
  SH10_Lxx_Floor_Obstacles  what obstacles look like (no collider)
  SH10_Lxx_Floor_Blockers   simple boxes around each obstacle; Unity turns them into the only obstacle
                            collider and hides them. Obstacles stay inside the tile, off the paths and out
                            of the centre, where the player spawns.
"""
import bpy
import bmesh
import json
import math
import os
import random
from pathlib import Path

ROOT = Path(os.environ.get("SOULHUNTER_ROOT") or Path(__file__).resolve().parent.parent.parent)
OUT = ROOT / 'ArtSource' / 'SH10_Floors_v02'
MODELS = ROOT / 'Assets' / 'Art' / 'Models'
PREVIEWS = OUT / 'Previews'
SIZE = 20.0
HALF = SIZE / 2
PATH_HALF = 1.5        # paths are 3 units wide
CENTER_CLEAR = 4.5     # no obstacle this close to the tile centre (spawn)
EDGE_MARGIN = 0.4

palette = {}
materials = {}
layers = {}            # 'ground' | 'obstacles' | 'blockers' -> list of objects
current = ['ground']


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
    layers.setdefault(current[0], []).append(obj)
    return obj


class layer:
    def __init__(self, name): self.name = name
    def __enter__(self): self.prev = current[0]; current[0] = self.name
    def __exit__(self, *a): current[0] = self.prev


def box(name, pos, size, mat, rot_z=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos, rotation=(0, 0, rot_z))
    obj = bpy.context.object
    obj.scale = size
    return finish(obj, name, mat)


def cone(name, pos, radius, top, height, mat, verts=8, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=radius, radius2=top, depth=height, location=pos, rotation=rot)
    return finish(bpy.context.object, name, mat)


def gem(name, pos, scale, mat, rot_z=0.0):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1, location=pos, rotation=(0, 0, rot_z))
    obj = bpy.context.object
    obj.scale = scale
    return finish(obj, name, mat)


def patch(name, x, y, radius, mat, rng, z=0.004, points=9, squash=1.0):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    rot = rng.uniform(0, math.tau)
    verts = []
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


def blocker(x, y, w, d, rot=0.0, h=2.5):
    """Invisible collision box around an obstacle's footprint."""
    with layer('blockers'):
        box('Blocker', (x, y, h / 2), (w, d, h), 'Blocker', rot)


# ---------- ground ----------

def base():
    box('Base', (0, 0, -.1), (SIZE, SIZE, .2), 'Base')


def in_path(x, y, r=0.0):
    return abs(x) < PATH_HALF + r or abs(y) < PATH_HALF + r


def spot(rng, margin, avoid_paths=False):
    m = HALF - margin
    for _ in range(200):
        x, y = rng.uniform(-m, m), rng.uniform(-m, m)
        if not avoid_paths or not in_path(x, y, margin):
            return x, y
    return m, m


def cross_paths(rng, mat, edge_mat=None, rough=True, z=0.0):
    """Two roads crossing at the centre, edge to edge; the ragged edges stay inside the tile.
    z lifts them above slab floors (slabs are 0.06 high)."""
    for axis in (0, 1):
        if axis == 0:
            box('PathX', (0, 0, z + .006), (SIZE, PATH_HALF * 2, .012), mat)
        else:
            box('PathY', (0, 0, z + .007), (PATH_HALF * 2, SIZE, .012), mat)
        if rough and edge_mat:
            for i in range(18):
                t = rng.uniform(-HALF + 1, HALF - 1)
                side = rng.choice([-1, 1]) * (PATH_HALF + rng.uniform(-.1, .25))
                x, y = (t, side) if axis == 0 else (side, t)
                patch('PathEdge', x, y, rng.uniform(.25, .55), edge_mat, rng, .009, 7)


def scatter(rng, count, mats, rmin, rmax, z=.004, avoid_paths=True, squash=1.0):
    for i in range(count):
        r = rng.uniform(rmin, rmax)
        x, y = spot(rng, r * 1.2, avoid_paths)
        patch('Patch', x, y, r, rng.choice(mats), rng, z + i * 0.0001, rng.randint(7, 11), squash)


def tufts(rng, count, mat, height=.28, avoid_paths=True):
    for _ in range(count):
        x, y = spot(rng, .4, avoid_paths)
        for _ in range(rng.randint(3, 5)):
            h = height * rng.uniform(.6, 1.2)
            cone('Blade', (x + rng.uniform(-.15, .15), y + rng.uniform(-.15, .15), h / 2), .035, 0, h, mat, 3,
                 (rng.uniform(-.35, .35), rng.uniform(-.35, .35), 0))


def flowers(rng, count, mats):
    for _ in range(count):
        x, y = spot(rng, .3, True)
        gem('Flower', (x, y, .05), (.07, .07, .04), rng.choice(mats))


def pebbles(rng, count, mat, avoid_paths=False):
    for _ in range(count):
        s = rng.uniform(.06, .18)
        x, y = spot(rng, s + .1, avoid_paths)
        gem('Pebble', (x, y, s * .15), (s, s * rng.uniform(.6, 1), s * .45), mat, rng.uniform(0, math.tau))


def slabs(rng, cell, gap, mats, missing=0.0):
    n = int(round(SIZE / cell))
    for i in range(n):
        for j in range(n):
            if rng.random() < missing:
                continue
            cx, cy = -HALF + cell * (i + .5), -HALF + cell * (j + .5)
            box('Slab', (cx, cy, .03), (cell - gap, cell - gap, .06), rng.choice(mats))


# ---------- obstacles ----------

class Placer:
    """Places obstacles off the paths, out of the centre, inside the tile and apart from each other."""
    def __init__(self, rng):
        self.rng = rng
        self.taken = []

    def place(self, count, radius, build, gap=.4, tries=400):
        placed = 0
        for _ in range(tries):
            if placed >= count:
                break
            r = radius * self.rng.uniform(.85, 1.15)
            m = HALF - r - EDGE_MARGIN
            x, y = self.rng.uniform(-m, m), self.rng.uniform(-m, m)
            if in_path(x, y, r + .3) or math.hypot(x, y) < CENTER_CLEAR + r:
                continue
            if any(math.hypot(x - tx, y - ty) < r + tr + gap for tx, ty, tr in self.taken):
                continue
            self.taken.append((x, y, r))
            with layer('obstacles'):
                build(x, y, r, self.rng)
            placed += 1
        return placed


def pine(x, y, r, rng, leaves='Leaves', snow=None):
    cone('Trunk', (x, y, .4), r * .18, r * .15, .8, 'Wood', 6)
    for k, (z, s) in enumerate([(1.0, 1.0), (1.6, .78), (2.15, .52)]):
        cone('Needles', (x, y, z), r * s, 0, 1.0, leaves, 8, (0, 0, rng.uniform(0, 1)))
        if snow and k == 2:
            cone('Snowcap', (x, y, z + .32), r * s * .45, 0, .36, snow, 8)
    blocker(x, y, r * 1.5, r * 1.5)


def round_tree(x, y, r, rng, leaves='Leaves', trunk='Wood'):
    cone('Trunk', (x, y, .6), r * .2, r * .16, 1.2, trunk, 6)
    gem('Canopy', (x, y, 1.6), (r, r, r * .75), leaves, rng.uniform(0, math.tau))
    gem('Canopy2', (x + r * .35, y - r * .2, 1.95), (r * .6, r * .6, r * .5), leaves)
    blocker(x, y, r * 1.5, r * 1.5)


def dead_tree(x, y, r, rng):
    cone('Trunk', (x, y, .9), r * .25, r * .12, 1.8, 'Wood', 6)
    for k in range(3):
        a = rng.uniform(0, math.tau)
        box('Branch', (x + .35 * math.cos(a), y + .35 * math.sin(a), 1.3 + k * .2), (.7, .08, .08), 'Wood', a)
    blocker(x, y, r * .9, r * .9)


def gravestone(x, y, r, rng):
    a = rng.choice([0, math.pi / 2]) + rng.uniform(-.1, .1)
    box('Stone', (x, y, .45), (.7, .22, .9), 'Stone', a)
    gem('StoneTop', (x, y, .92), (.36, .12, .18), 'Stone', a)
    box('Mound', (x - .45 * math.sin(a), y + .45 * math.cos(a), .03), (.7, 1.1, .06), 'Soil', a)
    blocker(x, y, .9, .9, a)


def crypt(x, y, r, rng):
    box('Crypt', (x, y, .9), (2.4, 2.0, 1.8), 'Stone')
    cone('Roof', (x, y, 2.1), 1.8, 0, .7, 'Roof', 4, (0, 0, math.pi / 4))
    box('Door', (x, y - 1.01, .6), (.7, .04, 1.2), 'Dark')
    gem('Seal', (x, y - 1.04, 1.35), (.12, .04, .12), 'Glow')
    blocker(x, y, 2.6, 2.2)


def house(x, y, r, rng):
    a = rng.choice([0, math.pi / 2])
    box('Walls', (x, y, .8), (2.8, 2.2, 1.6), 'Wall', a)
    roof = cone('Roof', (x, y, 2.05), 2.1, 0, 1.0, 'Roof', 4, (0, 0, math.pi / 4 + a))
    roof.scale = (1.0, 0.8, 1.0)
    bpy.context.view_layer.objects.active = roof
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    box('Door', (x + 1.11 * math.sin(a), y - 1.11 * math.cos(a), .5), (.6, .04, 1.0), 'Wood', a)
    blocker(x, y, 3.0, 2.4, a)


def hay(x, y, r, rng):
    cone('Hay', (x, y, .45), .5, .5, 1.0, 'Hay', 10, (math.pi / 2, 0, rng.uniform(0, math.pi)))
    blocker(x, y, 1.0, 1.0)


def well(x, y, r, rng):
    cone('Well', (x, y, .4), .8, .8, .8, 'Stone', 10)
    cone('Water', (x, y, .81), .6, .6, .02, 'Water', 10)
    for s in (-1, 1):
        box('Post', (x + s * .7, y, 1.2), (.1, .1, 1.2), 'Wood')
    box('Beam', (x, y, 1.8), (1.6, .12, .12), 'Wood')
    blocker(x, y, 1.7, 1.7)


def pillar(x, y, r, rng, broken=False):
    h = rng.uniform(1.0, 1.8) if broken else 2.6
    box('PillarBase', (x, y, .15), (.9, .9, .3), 'Stone')
    cone('Pillar', (x, y, .3 + h / 2), .32, .3, h, 'Pillar', 10)
    if not broken:
        box('Capital', (x, y, .3 + h + .12), (.8, .8, .24), 'Trim')
    blocker(x, y, .95, .95)


def wall_segment(x, y, r, rng):
    a = rng.choice([0, math.pi / 2])
    length = rng.uniform(2.4, 3.4)
    box('Wall', (x, y, .8), (length, .6, 1.6), 'Pillar', a)
    for k in range(3):
        box('Crenel', (x + (k - 1) * length / 3 * math.cos(a), y + (k - 1) * length / 3 * math.sin(a), 1.75), (.45, .62, .3), 'Pillar', a)
    blocker(x, y, length, .7, a)


def rubble(x, y, r, rng):
    for _ in range(5):
        s = rng.uniform(.25, .45)
        gem('Rubble', (x + rng.uniform(-.5, .5), y + rng.uniform(-.5, .5), s * .4), (s, s * .8, s * .6), 'Pillar', rng.uniform(0, math.tau))
    blocker(x, y, 1.3, 1.3)


def pond(x, y, r, rng, water='Water', rim='Soil'):
    patch('Rim', x, y, r * 1.12, rim, rng, .02, 12)
    patch('Pond', x, y, r, water, rng, .03, 12)
    blocker(x, y, r * 1.6, r * 1.6, 0, 1.0)


def stump(x, y, r, rng):
    cone('Stump', (x, y, .3), .45, .35, .6, 'Wood', 8)
    cone('Cut', (x, y, .61), .34, .34, .02, 'Hay', 8)
    blocker(x, y, .9, .9)


def rock(x, y, r, rng, mat='Stone'):
    gem('Rock', (x, y, r * .35), (r, r * .8, r * .7), mat, rng.uniform(0, math.tau))
    gem('Rock2', (x + r * .5, y + r * .3, r * .2), (r * .5, r * .45, r * .4), mat)
    blocker(x, y, r * 1.6, r * 1.6)


def crystal(x, y, r, rng, mat='Glow'):
    gem('Crystal', (x, y, 1.0), (.35 * r, .35 * r, 1.1 * r), mat, rng.uniform(0, math.tau))
    gem('Shard', (x + .3, y - .2, .5), (.18, .18, .55), mat)
    gem('Shard2', (x - .25, y + .25, .4), (.15, .15, .45), mat)
    blocker(x, y, r * 1.1, r * 1.1)


def brazier(x, y, r, rng):
    cone('Bowl', (x, y, .7), .3, .55, .35, 'Trim', 10)
    for k in range(3):
        a = k * math.tau / 3
        box('Leg', (x + .25 * math.cos(a), y + .25 * math.sin(a), .35), (.08, .08, .7), 'Dark', a)
    for k in range(3):
        gem('Flame', (x + (k - 1) * .15, y, 1.05), (.14, .14, .35), 'Glow')
    blocker(x, y, 1.0, 1.0)


def obelisk(x, y, r, rng):
    box('Plinth', (x, y, .15), (1.2, 1.2, .3), 'Dark')
    cone('Obelisk', (x, y, 1.6), .4, .12, 2.6, 'Pillar', 4, (0, 0, math.pi / 4))
    gem('Eye', (x, y - .3, 1.8), (.1, .04, .1), 'Glow')
    blocker(x, y, 1.3, 1.3)


def weapon_rack(x, y, r, rng):
    a = rng.choice([0, math.pi / 2])
    for s in (-1, 1):
        box('Post', (x + s * .8 * math.cos(a), y + s * .8 * math.sin(a), .65), (.12, .12, 1.3), 'Wood', a)
    box('Beam', (x, y, 1.15), (1.8, .14, .14), 'Wood', a)
    for k in (-1, 0, 1):
        box('Blade', (x + k * .5 * math.cos(a), y + k * .5 * math.sin(a), .75), (.06, .06, 1.1), 'Trim', a)
    blocker(x, y, 2.0, .6, a)


def pylon(x, y, r, rng):
    box('PylonBase', (x, y, .2), (1.0, 1.0, .4), 'PlateDark')
    cone('Pylon', (x, y, 1.4), .3, .1, 2.0, 'Plate', 6)
    gem('Core', (x, y, 2.45), (.25, .25, .25), 'Glow')
    blocker(x, y, 1.1, 1.1)


# ---------- levels ----------

def l01(rng):  # The Cursed Graveyard
    base(); cross_paths(rng, 'Path', 'Soil')
    scatter(rng, 40, ['DeadGrass', 'Soil', 'DeadGrass'], .35, .9)
    tufts(rng, 30, 'Tuft'); pebbles(rng, 26, 'Stone', True)
    for _ in range(6):
        x, y = spot(rng, .5, True); box('Bone', (x, y, .03), (.45, .06, .06), 'Bone', rng.uniform(0, math.tau))
    p = Placer(rng)
    p.place(1, 1.6, crypt)
    p.place(20, .55, gravestone, gap=.45)
    p.place(7, .6, dead_tree)


def l02(rng):  # The Dark Forest
    base(); cross_paths(rng, 'Path', 'Soil')
    scatter(rng, 46, ['Grass2', 'Moss', 'Grass2'], .35, 1.0)
    tufts(rng, 40, 'Tuft'); flowers(rng, 24, ['Flower1', 'Flower2'])
    p = Placer(rng)
    p.place(22, .9, pine, gap=.15)
    p.place(6, 1.0, round_tree, gap=.2)
    p.place(3, .5, stump)


def l03(rng):  # The Forgotten Village
    base(); cross_paths(rng, 'Path', 'PathEdge')
    scatter(rng, 30, ['Grass2', 'Moss'], .35, .9)
    for _ in range(3):  # crop fields (walkable)
        x, y = spot(rng, 2.2, True)
        if math.hypot(x, y) < CENTER_CLEAR + 2: continue
        for k in range(5):
            box('Row', (x, y - 1 + k * .5, .02), (3.0, .22, .04), 'Crop')
    tufts(rng, 24, 'Tuft'); flowers(rng, 16, ['Flower1', 'Flower2'])
    p = Placer(rng)
    p.place(3, 1.8, house, gap=.8)
    p.place(1, .9, well)
    p.place(5, .6, hay)
    p.place(5, 1.0, round_tree)


def l04(rng):  # The Ruined Castle
    base(); slabs(rng, 2.5, .1, ['Flag', 'FlagDark', 'Flag'], missing=.05)
    cross_paths(rng, 'Carpet', None, rough=False, z=.06)
    box('CarpetTrimX', (0, PATH_HALF, .074), (SIZE, .15, .01), 'Trim'); box('CarpetTrimX2', (0, -PATH_HALF, .074), (SIZE, .15, .01), 'Trim')
    box('CarpetTrimY', (PATH_HALF, 0, .075), (.15, SIZE, .01), 'Trim'); box('CarpetTrimY2', (-PATH_HALF, 0, .075), (.15, SIZE, .01), 'Trim')
    scatter(rng, 10, ['Moss'], .2, .5, z=.07)
    p = Placer(rng)
    p.place(10, .5, pillar)
    p.place(5, 1.5, wall_segment, gap=.6)
    p.place(6, .7, rubble)
    p.place(3, .5, lambda x, y, r, g: pillar(x, y, r, g, broken=True))


def l05(rng):  # The Crimson Swamp
    base(); cross_paths(rng, 'Plank', None, rough=False)
    for k in range(-9, 10, 1):  # plank seams
        box('SeamX', (k + .5, 0, .014), (.05, PATH_HALF * 2, .01), 'Dark'); box('SeamY', (0, k + .5, .015), (PATH_HALF * 2, .05, .01), 'Dark')
    scatter(rng, 40, ['Mud', 'MudDark'], .4, 1.0)
    tufts(rng, 26, 'Reed', .45)
    p = Placer(rng)
    p.place(5, 1.3, pond, gap=.5)
    p.place(8, .6, dead_tree)
    p.place(5, .5, stump)


def l06(rng):  # The Frozen Peaks
    base(); cross_paths(rng, 'Ice', 'SnowShade')
    scatter(rng, 34, ['SnowShade', 'Snow'], .45, 1.1)
    pebbles(rng, 20, 'Rock', True)
    p = Placer(rng)
    p.place(14, .9, lambda x, y, r, g: pine(x, y, r, g, 'Leaves', 'Snow'), gap=.2)
    p.place(6, .6, crystal)
    p.place(5, .7, lambda x, y, r, g: rock(x, y, r, g, 'Rock'))


def l07(rng):  # The Sea of Lost Souls
    base(); cross_paths(rng, 'Plank', None, rough=False)
    for k in range(-9, 10, 1):
        box('SeamX', (k + .5, 0, .014), (.05, PATH_HALF * 2, .01), 'Dark'); box('SeamY', (0, k + .5, .015), (PATH_HALF * 2, .05, .01), 'Dark')
    scatter(rng, 30, ['Sand', 'WetSand'], .4, 1.0)
    flowers(rng, 20, ['Glow', 'Glow2'])
    p = Placer(rng)
    p.place(5, 1.4, lambda x, y, r, g: pond(x, y, r, g, 'Water', 'WetSand'), gap=.5)
    p.place(13, .9, lambda x, y, r, g: round_tree(x, y, r, g, g.choice(['Leaves', 'Leaves2']), 'Wood'))


def l08(rng):  # The Blood Arenas
    base(); cross_paths(rng, 'Path', 'SandDark')
    bpy.ops.mesh.primitive_torus_add(major_radius=7.5, minor_radius=.1, major_segments=48, minor_segments=4, location=(0, 0, .0))
    obj = bpy.context.object; obj.scale = (1, 1, .1); finish(obj, 'ArenaRing', 'Line')
    scatter(rng, 30, ['SandDark', 'Sand'], .35, .9)
    for _ in range(12):
        x, y = spot(rng, 1, True); patch('Blood', x, y, rng.uniform(.3, .7), 'Blood', rng, .014)
    p = Placer(rng)
    p.place(8, .5, pillar)
    p.place(5, 1.1, weapon_rack, gap=.5)
    p.place(4, 1.5, wall_segment, gap=.6)


def l09(rng):  # The Throne of Death
    base(); slabs(rng, 4.0, .16, ['Marble', 'MarbleDark'])
    cross_paths(rng, 'Carpet', None, rough=False, z=.06)
    for s in (-1, 1):
        box('TrimX', (0, s * PATH_HALF, .074), (SIZE, .18, .01), 'Gold'); box('TrimY', (s * PATH_HALF, 0, .075), (.18, SIZE, .01), 'Gold')
    p = Placer(rng)
    p.place(10, .5, pillar)
    p.place(6, .55, brazier)
    p.place(3, .7, obelisk)


def l10(rng):  # The Soul Core
    base(); slabs(rng, 2.0, .1, ['Plate', 'PlateDark', 'Plate'])
    cross_paths(rng, 'Vein', None, rough=False, z=.06)
    for _ in range(10):
        x, y = spot(rng, 2.2, True)
        box('Vein', (x, y, .07), (rng.uniform(1.4, 3.2), .06, .01), 'Glow', rng.choice([0, math.pi / 2, math.pi / 4]))
    p = Placer(rng)
    p.place(14, .7, crystal)
    p.place(5, .55, pylon)


COMMON = dict(Blocker=(1.0, 0.0, 1.0), Dark=(.06, .06, .08), Wood=(.36, .22, .12), Stone=(.52, .52, .56), Glow=(.25, .95, .9),
              Hay=(.78, .62, .32))
LEVELS = {
    1: ('The Cursed Graveyard', l01, dict(Base=(.20, .26, .15), DeadGrass=(.30, .34, .18), Soil=(.26, .20, .14), Path=(.44, .34, .22),
                                          Tuft=(.36, .42, .20), Bone=(.78, .76, .62), Roof=(.22, .22, .28))),
    2: ('The Dark Forest', l02, dict(Base=(.20, .42, .16), Grass2=(.28, .52, .20), Moss=(.16, .34, .13), Soil=(.40, .30, .18),
                                     Path=(.56, .43, .26), Tuft=(.32, .58, .22), Leaves=(.10, .40, .16), Flower1=(.95, .85, .30),
                                     Flower2=(.90, .45, .65))),
    3: ('The Forgotten Village', l03, dict(Base=(.32, .52, .22), Grass2=(.38, .58, .26), Moss=(.26, .44, .18), Path=(.62, .50, .32),
                                           PathEdge=(.52, .42, .26), Crop=(.55, .40, .20), Tuft=(.42, .64, .26), Wall=(.80, .72, .58),
                                           Roof=(.70, .24, .18), Hay=(.86, .72, .34), Water=(.20, .45, .70), Leaves=(.18, .48, .20),
                                           Flower1=(.95, .90, .40), Flower2=(.85, .40, .40))),
    4: ('The Ruined Castle', l04, dict(Base=(.20, .20, .24), Flag=(.50, .50, .55), FlagDark=(.40, .40, .46), Carpet=(.62, .10, .12),
                                       Trim=(.85, .66, .25), Moss=(.28, .40, .20), Pillar=(.62, .62, .66))),
    5: ('The Crimson Swamp', l05, dict(Base=(.34, .16, .15), Mud=(.42, .20, .18), MudDark=(.24, .10, .10), Plank=(.52, .36, .22),
                                       Reed=(.45, .40, .20), Water=(.40, .06, .12), Soil=(.28, .14, .12), Hay=(.66, .52, .30))),
    6: ('The Frozen Peaks', l06, dict(Base=(.86, .90, .95), Snow=(.96, .98, 1.0), SnowShade=(.74, .82, .92), Ice=(.60, .80, .96),
                                      Rock=(.34, .38, .46), Leaves=(.14, .36, .30), Glow=(.45, .85, 1.0))),
    7: ('The Sea of Lost Souls', l07, dict(Base=(.10, .28, .36), Sand=(.16, .36, .42), WetSand=(.08, .22, .30), Plank=(.46, .34, .24),
                                           Water=(.18, .70, .82), Leaves=(.45, .25, .80), Leaves2=(.15, .70, .75), Glow2=(.80, .40, 1.0))),
    8: ('The Blood Arenas', l08, dict(Base=(.68, .52, .36), Sand=(.76, .60, .42), SandDark=(.56, .42, .28), Path=(.60, .46, .32),
                                      Line=(.86, .76, .60), Blood=(.55, .04, .05), Pillar=(.70, .64, .56), Trim=(.75, .76, .80))),
    9: ('The Throne of Death', l09, dict(Base=(.08, .05, .10), Marble=(.30, .24, .38), MarbleDark=(.20, .16, .28), Carpet=(.52, .05, .14),
                                         Gold=(.90, .70, .25), Pillar=(.44, .38, .52), Trim=(.90, .70, .25), Glow=(.95, .45, 1.0))),
    10: ('The Soul Core', l10, dict(Base=(.04, .06, .09), Plate=(.16, .24, .32), PlateDark=(.10, .16, .22), Vein=(.15, .85, .90),
                                    Glow=(.30, 1.0, 1.0))),
}


def preview(name):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'MATERIAL'
    scene.render.resolution_x = scene.render.resolution_y = 512
    cam_data = bpy.data.cameras.new('TopCam'); cam_data.type = 'ORTHO'; cam_data.ortho_scale = SIZE
    cam = bpy.data.objects.new('TopCam', cam_data); bpy.context.collection.objects.link(cam)
    cam.location = (0, 0, 30); cam.rotation_euler = (0, 0, 0)
    scene.camera = cam
    for obj in layers.get('blockers', []): obj.hide_render = True
    scene.render.filepath = str(PREVIEWS / f'{name}.png')
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam); bpy.data.cameras.remove(cam_data)


def join(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    return obj


def build(level):
    global materials, layers
    title, fn, colors = LEVELS[level]
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    for m in list(bpy.data.materials): bpy.data.materials.remove(m)
    materials, layers = {}, {}
    current[0] = 'ground'
    palette.clear(); palette.update(COMMON); palette.update(colors)
    fn(random.Random(2000 + level))
    asset = f'SH10_L{level:02}_Floor'
    preview(asset)
    meshes = [join(layers['ground'], asset)]
    counts = {'obstacles': 0}
    if layers.get('obstacles'):
        meshes.append(join(layers['obstacles'], asset + '_Obstacles'))
        counts['obstacles'] = len(layers['blockers'])
        meshes.append(join(layers['blockers'], asset + '_Blockers'))
    for key, m in materials.items(): m.name = asset + '_' + key
    bpy.ops.object.select_all(action='DESELECT')
    for m in meshes: m.select_set(True)
    source = OUT / 'Blender' / f'{asset}.blend'; source.parent.mkdir(parents=True, exist_ok=True)
    model = MODELS / f'{asset}.fbx'
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    bpy.ops.export_scene.fbx(filepath=str(model), use_selection=True, object_types={'MESH'},
                             axis_forward='-Z', axis_up='Y', apply_unit_scale=True)
    triangles = sum(len(p.vertices) - 2 for m in meshes for p in m.data.polygons)
    print('FLOOR_DONE', asset, triangles, 'obstacles', counts['obstacles'], flush=True)
    return dict(name=asset, level=level, title=title, size=SIZE, triangles=triangles, obstacles=counts['obstacles'],
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
