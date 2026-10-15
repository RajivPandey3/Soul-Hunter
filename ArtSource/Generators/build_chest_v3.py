"""Soul Hunter treasure chest v3, built from the owner's concept image Assets/Art/UI/Treasure_Chest.jpg (2026-10-02).

Owner: "chestbox it is not like to attractive" and "project ke asset folder mein chest ka design rakha hai ... approval
le lo"; chose "Concept image se naya 3D". The concept: an ornate gold-framed chest with a domed amber lid, gold ribs,
engraved runes, red gems, a skull hasp over a shield lock, dark wood panels, side handles and golden light at the seam.

Part and material names follow the v2 Soul Reliquary so the game code keeps working:
- "Lid" (the lid shell), "LidBand*" (lid ribs), "SoulSeam", "LockPlate", "Cranium" (ChestPreviewStage hinge and tests);
- "Chest_Iron" is the trim metal (ChestLook turns it bronze, silver or Arcana violet), "Chest_Soul" the glow.

Run (renders previews):  blender --background --python ArtSource/Generators/build_chest_v3.py -- <output folder>
Export to Unity:         blender --background --python ArtSource/Generators/build_chest_v3.py -- <out> --export <project root>
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_pickups_v2 as pv2  # noqa: E402  (helpers: materials, meshes, scene, camera, render)

mat, shade, link, mesh_obj, crystal = pv2.mat, pv2.shade, pv2.link, pv2.mesh_obj, pv2.crystal
OUT = sys.argv[sys.argv.index('--') + 1] if '--' in sys.argv else os.path.join(os.getcwd(), 'chest_v3')
os.makedirs(OUT, exist_ok=True)

W, D, H = 1.2, 0.8, 0.5   # body width (X), depth (Y), height (Z); same footprint as v2
BASE = 0.08               # feet height
SEAM = BASE + H           # top of the body


def chest_v3():
    root = link(bpy.data.objects.new('SoulChest', None))
    wood = mat('Chest_Wood', (0.2, 0.1, 0.05), rough=0.75)
    wood2 = mat('Chest_Wood2', (0.27, 0.14, 0.07), rough=0.7)
    trim = mat('Chest_Iron', (1.0, 0.7, 0.18), metallic=0.85, rough=0.22, emission=(1.0, 0.6, 0.12), strength=0.1)          # gold ornament (tinted per chest)
    amber = mat('Chest_Amber', (0.42, 0.14, 0.02), rough=0.3, emission=(0.85, 0.3, 0.03), strength=0.5)
    gem = mat('Chest_Gem', (0.55, 0.01, 0.03), rough=0.12, emission=(0.9, 0.03, 0.03), strength=1.0)
    rune = mat('Chest_Soul', (1.0, 0.85, 0.45), emission=(1.0, 0.75, 0.3), strength=8)  # glow (seam, runes, keyhole)
    dark = mat('Chest_Dark', (0.03, 0.02, 0.02), rough=0.8)

    def box(name, size, loc, material, bevel=0.01, rot=(0, 0, 0)):
        o = mesh_obj(name, lambda bm: bmesh.ops.create_cube(bm, size=1.0))
        o.scale = size
        o.location = loc
        o.rotation_euler = rot
        shade(o, material, bevel=bevel).parent = root
        return o

    def diamond(name, loc, size, rot=(math.radians(90), 0, 0), material=gem):
        o = mesh_obj(name, lambda bm: crystal(bm, 4, size, size * 0.5, size * 0.9, size * 0.9))
        o.location = loc
        o.rotation_euler = rot
        shade(o, material).parent = root
        return o

    # Body: dark wood panels in two tones.
    for i in range(4):
        box(f'Plank{i}', (W - 0.02, D - 0.02, H / 4 - 0.008), (0, 0, BASE + H / 8 + i * H / 4), wood if i % 2 else wood2, bevel=0.01)

    # Gold frame: top and bottom rims all round, corner posts, a front strap under the lock, side straps.
    box('RimTop', (W + 0.04, D + 0.04, 0.07), (0, 0, SEAM - 0.035), trim, bevel=0.012)
    box('RimBottom', (W + 0.04, D + 0.04, 0.07), (0, 0, BASE + 0.035), trim, bevel=0.012)
    for x in (-1, 1):
        for y in (-1, 1):
            box(f'Post{x}{y}', (0.1, 0.1, H + 0.02), (x * (W / 2 - 0.02), y * (D / 2 - 0.02), BASE + H / 2), trim, bevel=0.015)
            cap = mesh_obj(f'Cap{x}{y}', lambda bm: bmesh.ops.create_cone(bm, cap_ends=True, segments=4, radius1=0.085, radius2=0.0, depth=0.1))
            cap.location = (x * (W / 2 - 0.02), y * (D / 2 - 0.02), BASE - 0.02)
            cap.rotation_euler = (math.pi, 0, math.radians(45))
            shade(cap, trim).parent = root
            diamond(f'CornerGem{x}{y}', (x * (W / 2 - 0.02), y * (D / 2 + 0.035), BASE + H * 0.55), 0.035)
    for side in (-1, 1):
        box(f'Strap{side}', (0.06, D + 0.035, H - 0.06), (side * 0.3, 0, BASE + H / 2), trim, bevel=0.008)
    box('FrontStrap', (0.16, 0.03, H - 0.06), (0, -D / 2 - 0.012, BASE + H / 2), trim, bevel=0.008)

    # Engraved runes: small glowing marks along the top rim, front and sides.
    for i, x in enumerate([-0.5, -0.42, -0.2, -0.12, 0.12, 0.2, 0.42, 0.5]):
        box(f'RuneF{i}', (0.035, 0.012, 0.03 if i % 2 else 0.018), (x, -D / 2 - 0.027, SEAM - 0.035), rune, bevel=0.0)
    for side in (-1, 1):
        for i, y in enumerate([-0.22, -0.08, 0.08, 0.22]):
            box(f'RuneS{side}{i}', (0.012, 0.035, 0.025), (side * (W / 2 + 0.027), y, SEAM - 0.035), rune, bevel=0.0)
    # Gems set in the front planks between the straps.
    for x in (-0.15, 0.15, -0.46, 0.46):
        diamond(f'PanelGem{x}', (x, -D / 2 - 0.02, BASE + H * 0.35), 0.03)

    # Side handles: gold half rings on the ends.
    for side in (-1, 1):
        ring = mesh_obj(f'Handle{side}', lambda bm: pv2.torus(bm, 0.11, 0.018, 18, 6))
        ring.location = (side * (W / 2 + 0.055), 0, BASE + H * 0.55)
        ring.rotation_euler = (0, math.radians(90), 0)
        shade(ring, trim, smooth=True).parent = root
        box(f'HandleMount{side}', (0.03, 0.22, 0.05), (side * (W / 2 + 0.02), 0, BASE + H * 0.65), trim, bevel=0.008)

    # Lid: a domed half barrel of glowing amber (the concept's warm panels), ribbed with gold.
    def lid(bm):
        segs = 16
        R, r = D / 2, D / 2 - 0.05
        x0, x1 = -W / 2, W / 2
        ol = [bm.verts.new((x0, math.cos(math.pi * i / segs) * R, math.sin(math.pi * i / segs) * R)) for i in range(segs + 1)]
        orr = [bm.verts.new((x1, v.co.y, v.co.z)) for v in ol]
        il = [bm.verts.new((x0, math.cos(math.pi * i / segs) * r, math.sin(math.pi * i / segs) * r)) for i in range(segs + 1)]
        ir = [bm.verts.new((x1, v.co.y, v.co.z)) for v in il]
        for i in range(segs):
            bm.faces.new((ol[i], orr[i], orr[i + 1], ol[i + 1]))
            bm.faces.new((il[i + 1], ir[i + 1], ir[i], il[i]))
            bm.faces.new((ol[i + 1], il[i + 1], il[i], ol[i]))
            bm.faces.new((orr[i], ir[i], ir[i + 1], orr[i + 1]))
        bm.faces.new((ol[0], il[0], ir[0], orr[0]))
        bm.faces.new((orr[segs], ir[segs], il[segs], ol[segs]))
    lid_obj = mesh_obj('Lid', lid)
    lid_obj.location = (0, 0, SEAM + 0.02)
    shade(lid_obj, amber, bevel=0.008, smooth=True).parent = root
    # Lid ends: dark wood half discs closing the barrel, with a gem each.
    for side in (-1, 1):
        end = mesh_obj(f'LidEnd{side}', lambda bm: bmesh.ops.create_cone(bm, cap_ends=True, segments=16, radius1=D / 2 - 0.02, radius2=D / 2 - 0.02, depth=0.02))
        end.location = (side * (W / 2 - 0.01), 0, SEAM + 0.02)
        end.rotation_euler = (0, math.radians(90), 0)
        end.scale = (1, 1, 1)
        shade(end, wood2).parent = root
        diamond(f'LidEndGem{side}', (side * (W / 2 + 0.005), 0, SEAM + 0.2), 0.035, rot=(0, math.radians(90), 0))
    # Gold ribs over the lid: ends, centre and two between, each a ring segment.
    for x in (-W / 2 + 0.02, -0.3, 0.0, 0.3, W / 2 - 0.02):
        band = mesh_obj(f'LidBand{x:+.2f}', lambda bm: bmesh.ops.create_cone(bm, cap_ends=False, segments=32, radius1=D / 2 + 0.022, radius2=D / 2 + 0.022, depth=0.06 if abs(x) < 0.5 else 0.05))
        band.location = (x, 0, SEAM + 0.02)
        band.rotation_euler = (0, math.radians(90), 0)
        shade(band, trim, smooth=True).parent = root
    # Runes along the centre rib, gems at the rib crossings.
    for i in range(7):
        a = math.pi * (i + 1) / 8
        box(f'RuneLid{i}', (0.065, 0.02, 0.02), (0, math.cos(a) * (D / 2 + 0.03), SEAM + 0.02 + math.sin(a) * (D / 2 + 0.03)), rune,
            bevel=0.0, rot=(a - math.pi / 2, 0, 0))
    for x in (-0.3, 0.3):
        diamond(f'LidGem{x}', (x, -0.12, SEAM + 0.02 + math.sqrt((D / 2 + 0.03) ** 2 - 0.12 ** 2)), 0.04, rot=(math.radians(20), 0, 0))
    # Gold rim along the lid's lower edges (front and back).
    for y in (-1, 1):
        box(f'LidRim{y}', (W + 0.02, 0.05, 0.05), (0, y * (D / 2 - 0.005), SEAM + 0.045), trim, bevel=0.01)

    # Golden light leaking from the seam.
    box('SoulSeam', (W - 0.08, D - 0.04, 0.02), (0, 0, SEAM + 0.01), rune, bevel=0.0)

    # Lock: a gold shield plate on the body with a dark keyhole lit from inside.
    def shield(bm):
        pts = [(-0.13, 0.12), (0.13, 0.12), (0.13, -0.04), (0.0, -0.17), (-0.13, -0.04)]
        vs = [bm.verts.new((x, 0.0, z)) for x, z in pts]
        f = bm.faces.new(vs)
        geom = bmesh.ops.extrude_face_region(bm, geom=[f])
        for v in [e for e in geom['geom'] if isinstance(e, bmesh.types.BMVert)]:
            v.co.y -= 0.045
    plate = mesh_obj('LockPlate', shield)
    plate.location = (0, -D / 2 - 0.02, SEAM - 0.15)
    shade(plate, trim, bevel=0.012).parent = root
    box('KeyholeTop', (0.045, 0.02, 0.045), (0, -D / 2 - 0.07, SEAM - 0.13), dark, bevel=0.01)
    box('KeyholeSlot', (0.02, 0.02, 0.07), (0, -D / 2 - 0.07, SEAM - 0.18), dark, bevel=0.0)
    box('KeyGlow', (0.012, 0.01, 0.05), (0, -D / 2 - 0.079, SEAM - 0.16), rune, bevel=0.0)

    # Hasp hanging from the lid over the seam, with a gold skull (part of the lid: it lifts when the chest opens).
    box('Hasp', (0.16, 0.035, 0.16), (0, -D / 2 - 0.035, SEAM + 0.05), trim, bevel=0.015)
    cranium = mesh_obj('Cranium', lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=2, radius=0.065))
    cranium.location = (0, -D / 2 - 0.07, SEAM + 0.07)
    cranium.scale = (1.0, 0.85, 0.95)
    shade(cranium, trim, smooth=True).parent = root
    box('Jaw', (0.07, 0.045, 0.03), (0, -D / 2 - 0.08, SEAM + 0.025), trim, bevel=0.01)
    for x in (-0.024, 0.024):
        socket = mesh_obj(f'Socket{x}', lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=1, radius=0.018))
        socket.location = (x, -D / 2 - 0.122, SEAM + 0.075)
        shade(socket, dark, smooth=True).parent = root
        eye = mesh_obj(f'Eye{x}', lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=1, radius=0.009))
        eye.location = (x, -D / 2 - 0.135, SEAM + 0.075)
        shade(eye, gem).parent = root

    # Gold filigree (the concept's scrollwork): S-scrolls on the lid panels and the front wood panels.
    def scroll_points(size, turns=1.6, steps=40):
        """An S-scroll: one spiral unwinding into a point-mirrored second spiral (2D, centred on 0)."""
        pts = []
        a = size * 0.55
        for i in range(steps + 1):  # inner end of the left spiral, out to the middle
            th = turns * 2 * math.pi * (1 - i / steps)
            r = size * (0.08 + 0.42 * i / steps)
            pts.append((-a + r * math.cos(th), r * math.sin(th)))
        right = [(-x, -y) for x, y in reversed(pts)]
        return pts + right[1:]

    def filigree(name, points3d, thickness=0.009):
        curve = bpy.data.curves.new(name, 'CURVE')
        curve.dimensions = '3D'
        curve.bevel_depth = thickness
        curve.bevel_resolution = 2
        spline = curve.splines.new('POLY')
        spline.points.add(len(points3d) - 1)
        for i, (x, y, z) in enumerate(points3d):
            spline.points[i].co = (x, y, z, 1)
        o = link(bpy.data.objects.new(name, curve))
        o.data.materials.append(trim)
        o.parent = root
        return o

    lid_r = D / 2 + 0.006
    for i, xc in enumerate((-0.45, -0.15, 0.15, 0.45)):
        for j, ac in enumerate((math.radians(55), math.radians(125))):
            pts = []
            for u, v in scroll_points(0.11):
                ang = ac + v / lid_r
                pts.append((xc + u, math.cos(ang) * lid_r, SEAM + 0.02 + math.sin(ang) * lid_r))
            filigree(f'LidScroll{i}{j}', pts)
    for i, xc in enumerate((-0.44, -0.15, 0.15, 0.44)):
        if abs(xc) < 0.2:
            zc = BASE + H * 0.62
        else:
            zc = BASE + H * 0.5
        pts = [(xc + u, -D / 2 - 0.016, zc + v) for u, v in scroll_points(0.085)]
        filigree(f'BodyScroll{i}', pts, thickness=0.007)

    # Gold rivets along the frame.
    for i, x in enumerate([-0.52, -0.38, -0.24, 0.24, 0.38, 0.52]):
        for z in (BASE + 0.035, SEAM - 0.035):
            rv = mesh_obj(f'Rivet{i}{z:.2f}', lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=1, radius=0.016))
            rv.location = (x, -D / 2 - 0.032, z)
            shade(rv, trim, smooth=True).parent = root
    return root


def build_variants():
    """Bronze / silver / Arcana colourings for the preview (ChestLook tints Chest_Iron the same way in game)."""
    return {'Bronze': (0.55, 0.32, 0.13), 'Silver': (0.78, 0.8, 0.86), 'Arcana': (0.3, 0.16, 0.42)}


if __name__ == '__main__' and '--export' in sys.argv:
    import json
    project = sys.argv[sys.argv.index('--export') + 1]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    root = chest_v3()
    root.name = 'SH10_Pickup_Chest'
    bpy.ops.object.select_all(action='DESELECT')
    for o in [root] + list(root.children_recursive):
        o.select_set(True)
    models = os.path.join(project, 'Assets', 'Art', 'Models')
    bpy.ops.export_scene.fbx(filepath=os.path.join(models, 'SH10_Pickup_Chest.fbx'), use_selection=True, object_types={'MESH', 'EMPTY'},
                             use_mesh_modifiers=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL')
    pv2.remember_materials()
    manifest = os.path.join(project, 'ArtSource', 'SH10_Pickups_v2', 'materials.json')
    data = json.load(open(manifest, encoding='utf-8')) if os.path.exists(manifest) else {}
    data.update({k: v for k, v in pv2.MATERIALS.items() if k.startswith('Chest_')})
    json.dump(data, open(manifest, 'w', encoding='utf-8'), indent=2)
    print('EXPORTED chest v3', flush=True)
elif __name__ == '__main__':
    pv2.OUT = OUT
    s = pv2.setup_scene()
    s.render.resolution_x, s.render.resolution_y = 1600, 1000
    hero = chest_v3()
    # The concept's aura: a warm light inside the chest, sparkles around it, and a dark backdrop.
    glow = link(bpy.data.objects.new('ChestGlow', bpy.data.lights.new('ChestGlow', 'POINT')))
    glow.data.energy = 25
    glow.data.color = (1.0, 0.7, 0.3)
    glow.location = (0, -0.6, SEAM + 0.3)
    s.world.node_tree.nodes['Background'].inputs[0].default_value = (0.03, 0.03, 0.04, 1)
    spark = mat('Spark', (1, 0.85, 0.4), emission=(1, 0.8, 0.35), strength=25)
    import random
    random.seed(4)
    for k in range(40):
        a_ = random.uniform(0, 2 * math.pi)
        r_ = random.uniform(0.75, 1.3)
        sp = mesh_obj(f'Spark{k}', lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=1, radius=random.uniform(0.006, 0.016)))
        sp.location = (math.cos(a_) * r_, math.sin(a_) * r_ * 0.7, random.uniform(0.2, 1.4))
        shade(sp, spark)
    pv2.render(pv2.camera('Front', Vector((0, 0, 0.42)), 3.0, lens=50, height=0.45), 'chest_v3_front')
    hero.rotation_euler = (0, 0, math.radians(-30))
    pv2.render(pv2.camera('ThreeQuarter', Vector((0, 0, 0.42)), 3.1, lens=50, height=0.55), 'chest_v3_threequarter')
    hero.rotation_euler = (0, 0, 0)
    # The game camera: 55 degrees down, from the front.
    pv2.render(pv2.camera('Game', Vector((0, 0, 0.4)), 2.6, lens=50, height=math.tan(math.radians(55))), 'chest_v3_gamecam')
    # Bronze / silver / Arcana side by side, as the chest types look in play.
    trim = bpy.data.materials['Chest_Iron']
    soul = bpy.data.materials['Chest_Soul']
    for name, colour in build_variants().items():
        trim.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (*colour, 1)
        if name == 'Arcana':
            p = soul.node_tree.nodes['Principled BSDF']
            p.inputs['Emission Color'].default_value = (0.55, 0.2, 1.0, 1)
        pv2.render(pv2.camera('V' + name, Vector((0, 0, 0.42)), 3.0, lens=50, height=0.55), f'chest_v3_{name.lower()}')
