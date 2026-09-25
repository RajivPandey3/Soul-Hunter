"""Soul Hunter pickups v2 - concept renders for owner approval (2026-09-25).

Modern, Soul Hunter-style pickups made in Blender (no external art): three Soul Gem tiers, Soul Coins (gold, silver,
bronze) and a gothic reliquary chest. This pass only renders concepts; after approval the same models are exported
to Unity.

Run: blender --background --python ArtSource/Generators/build_pickups_v2.py -- <output folder>
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

OUT = sys.argv[sys.argv.index('--') + 1] if '--' in sys.argv else os.path.join(os.getcwd(), 'pickups_v2')
os.makedirs(OUT, exist_ok=True)


# ---------------------------------------------------------------- materials

def mat(name, color, metallic=0.0, rough=0.5, emission=None, strength=0.0, transmission=0.0, alpha=1.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes['Principled BSDF']
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Metallic'].default_value = metallic
    p.inputs['Roughness'].default_value = rough
    if 'Transmission Weight' in p.inputs:
        p.inputs['Transmission Weight'].default_value = transmission
    if emission:
        p.inputs['Emission Color'].default_value = (*emission, 1)
        p.inputs['Emission Strength'].default_value = strength
    p.inputs['Alpha'].default_value = alpha
    return m


def shade(obj, material, bevel=0.0, smooth=False):
    obj.data.materials.clear()
    obj.data.materials.append(material)
    if bevel > 0:
        mod = obj.modifiers.new('Bevel', 'BEVEL')
        mod.width = bevel
        mod.segments = 2
        mod.limit_method = 'ANGLE'
    for poly in obj.data.polygons:
        poly.use_smooth = smooth
    return obj


def link(obj, parent=None):
    bpy.context.scene.collection.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    return obj


def mesh_obj(name, build):
    bm = bmesh.new()
    build(bm)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    return link(bpy.data.objects.new(name, me))


# ---------------------------------------------------------------- shapes

def crystal(bm, sides, radius, height, top, bottom, twist=0.0, offset=Vector((0, 0, 0)), tilt=0.0):
    """A cut crystal: a prism band with a pointed top and a shorter pointed bottom."""
    ring_lo, ring_hi = [], []
    for i in range(sides):
        a = 2 * math.pi * i / sides
        ring_lo.append(Vector((math.cos(a) * radius, math.sin(a) * radius, 0)))
        a2 = a + twist
        ring_hi.append(Vector((math.cos(a2) * radius * 0.92, math.sin(a2) * radius * 0.92, height)))
    rot = Vector((0, 0, 1)).rotation_difference(Vector((math.sin(tilt), 0, math.cos(tilt))))
    def v(p): return bm.verts.new(rot @ p + offset)
    lo = [v(p) for p in ring_lo]
    hi = [v(p) for p in ring_hi]
    tip = v(Vector((0, 0, height + top)))
    foot = v(Vector((0, 0, -bottom)))
    for i in range(sides):
        j = (i + 1) % sides
        bm.faces.new((lo[i], lo[j], hi[j], hi[i]))
        bm.faces.new((hi[i], hi[j], tip))
        bm.faces.new((lo[j], lo[i], foot))


def soul_gem_small():
    root = link(bpy.data.objects.new('SoulGem_Small', None))
    glass = mat('Gem_Cyan', (0.05, 0.6, 0.85), rough=0.12, emission=(0.1, 0.7, 1.0), strength=1.6, transmission=0.15)
    core = mat('Gem_Cyan_Core', (0.8, 1, 1), emission=(0.5, 0.95, 1.0), strength=12)
    g = mesh_obj('Shard', lambda bm: crystal(bm, 6, 0.16, 0.18, 0.26, 0.2, twist=0.25))
    shade(g, glass, bevel=0.01).parent = root
    c = mesh_obj('Core', lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=1, radius=0.06))
    c.location = (0, 0, 0.1)
    shade(c, core).parent = root
    return root


def soul_gem_medium():
    root = link(bpy.data.objects.new('SoulGem_Medium', None))
    glass = mat('Gem_Violet', (0.35, 0.12, 0.8), rough=0.12, emission=(0.45, 0.2, 1.0), strength=1.8, transmission=0.15)
    core = mat('Gem_Violet_Core', (1, 0.9, 1), emission=(0.8, 0.5, 1.0), strength=12)
    parts = [(0.15, 0.24, 0.3, 0.0, Vector((0, 0, 0))), (0.1, 0.14, 0.2, 0.45, Vector((0.14, 0.02, -0.02))),
             (0.09, 0.12, 0.18, -0.5, Vector((-0.12, -0.04, -0.03)))]
    for i, (r, h, t, tilt, off) in enumerate(parts):
        g = mesh_obj(f'Crystal{i}', lambda bm, r=r, h=h, t=t, tilt=tilt, off=off: crystal(bm, 6, r, h, t, 0.12, 0.2, off, tilt))
        shade(g, glass, bevel=0.008).parent = root
    c = mesh_obj('Core', lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=1, radius=0.07))
    c.location = (0, 0, 0.12)
    shade(c, core).parent = root
    return root


def soul_gem_large():
    root = link(bpy.data.objects.new('SoulGem_Large', None))
    glass = mat('Gem_Crimson', (0.7, 0.03, 0.08), rough=0.12, emission=(1.0, 0.08, 0.15), strength=2.0, transmission=0.15)
    core = mat('Gem_Crimson_Core', (1, 0.9, 0.9), emission=(1.0, 0.4, 0.4), strength=14)
    gold = mat('Gold', (1.0, 0.72, 0.25), metallic=1.0, rough=0.25)
    g = mesh_obj('Heart', lambda bm: crystal(bm, 8, 0.24, 0.26, 0.34, 0.3, twist=0.2))
    shade(g, glass, bevel=0.012).parent = root
    c = mesh_obj('Core', lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=2, radius=0.1))
    c.location = (0, 0, 0.14)
    shade(c, core, smooth=True).parent = root
    ring = mesh_obj('Band', lambda bm: torus(bm, 0.27, 0.025, 24, 6))
    ring.location = (0, 0, 0.13)
    ring.rotation_euler = (math.radians(12), 0, 0)
    shade(ring, gold, smooth=True).parent = root
    return root


def torus(bm, major, minor, seg, rings):
    verts = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        row = []
        for j in range(rings):
            b = 2 * math.pi * j / rings
            r = major + minor * math.cos(b)
            row.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r, minor * math.sin(b))))
        verts.append(row)
    for i in range(seg):
        for j in range(rings):
            bm.faces.new((verts[i][j], verts[(i + 1) % seg][j], verts[(i + 1) % seg][(j + 1) % rings], verts[i][(j + 1) % rings]))


def coin(name, color, emblem_color):
    root = link(bpy.data.objects.new(name, None))
    metal = mat(name + '_Metal', color, metallic=1.0, rough=0.22)
    emblem = mat(name + '_Emblem', emblem_color, metallic=1.0, rough=0.12)
    soul = mat('Coin_Soul', (0.4, 0.95, 1.0), emission=(0.3, 0.9, 1.0), strength=8)

    def body(bm):
        # Reeded edge: 72 sides alternating slightly in radius, then a sunken field on both faces.
        segs = 72
        rings = []
        for z in (-0.05, 0.05):
            rings.append([bm.verts.new((math.cos(2 * math.pi * i / segs) * (0.5 if i % 2 else 0.488),
                                        math.sin(2 * math.pi * i / segs) * (0.5 if i % 2 else 0.488), z)) for i in range(segs)])
        for i in range(segs):
            j = (i + 1) % segs
            bm.faces.new((rings[0][i], rings[0][j], rings[1][j], rings[1][i]))
        top = bm.faces.new(rings[1])
        bottom = bm.faces.new(list(reversed(rings[0])))
        for face in (top, bottom):
            bmesh.ops.inset_region(bm, faces=[face], thickness=0.06, depth=0.0)
            bmesh.ops.inset_region(bm, faces=[face], thickness=0.015, depth=-0.022)
    b = mesh_obj(name + '_Body', body)
    shade(b, metal, bevel=0.006).parent = root

    def crescent(bm):
        # Quad strip between an outer arc and an offset inner arc: a clean crescent moon.
        n = 24
        outer, inner = [], []
        for i in range(n + 1):
            t = i / n
            a = math.radians(35 + 290 * t)
            outer.append(bm.verts.new((math.cos(a) * 0.3, math.sin(a) * 0.3, 0.0)))
            b = math.radians(55 + 250 * t)
            inner.append(bm.verts.new((math.cos(b) * 0.22 + 0.09, math.sin(b) * 0.22, 0.0)))
        for i in range(n):
            bm.faces.new((outer[i], outer[i + 1], inner[i + 1], inner[i]))
        geom = bmesh.ops.extrude_face_region(bm, geom=bm.faces[:])
        for v in [e for e in geom['geom'] if isinstance(e, bmesh.types.BMVert)]:
            v.co.z += 0.035

    def beads(bm):
        for i in range(28):
            a = 2 * math.pi * i / 28
            bmesh.ops.create_icosphere(bm, subdivisions=1, radius=0.016,
                                       matrix=Matrix.Translation((math.cos(a) * 0.405, math.sin(a) * 0.405, 0.0)))

    for side in (1, -1):
        m = mesh_obj(name + f'_Crescent{side}', crescent)
        m.location = (0, 0, 0.028 * side)
        m.rotation_euler = (0 if side > 0 else math.pi, 0, math.radians(90))
        shade(m, emblem, bevel=0.006).parent = root
        r = mesh_obj(name + f'_Beads{side}', beads)
        r.location = (0, 0, 0.05 * side)
        shade(r, emblem, smooth=True).parent = root
        flame = mesh_obj(name + f'_Flame{side}', lambda bm: crystal(bm, 6, 0.045, 0.05, 0.11, 0.035))
        flame.location = (0.0, -0.03, 0.045 * side)
        flame.rotation_euler = (math.radians(-90 if side > 0 else 90), 0, 0)
        shade(flame, soul).parent = root
    return root


def chest():
    root = link(bpy.data.objects.new('SoulChest', None))
    wood = mat('Chest_Wood', (0.22, 0.11, 0.06), rough=0.7)
    wood2 = mat('Chest_Wood2', (0.3, 0.16, 0.09), rough=0.65)
    iron = mat('Chest_Iron', (0.1, 0.1, 0.12), metallic=0.9, rough=0.32)
    gold = mat('Gold', (1.0, 0.72, 0.25), metallic=1.0, rough=0.22)
    bone = mat('Chest_Bone', (0.86, 0.83, 0.74), rough=0.45)
    dark = mat('Chest_Dark', (0.02, 0.02, 0.03), rough=0.8)
    soul = mat('Chest_Soul', (0.4, 0.95, 1.0), emission=(0.3, 0.9, 1.0), strength=12)

    def box(name, size, loc, material, bevel=0.01, rot=(0, 0, 0)):
        o = mesh_obj(name, lambda bm: bmesh.ops.create_cube(bm, size=1.0))
        o.scale = size
        o.location = loc
        o.rotation_euler = rot
        shade(o, material, bevel=bevel).parent = root
        return o

    W, D, H = 1.2, 0.8, 0.5
    base = 0.08
    for i in range(4):  # body planks, alternating tones
        box(f'Plank{i}', (W, D, H / 4 - 0.01), (0, 0, base + H / 8 + i * H / 4), wood if i % 2 else wood2, bevel=0.012)

    def lid(bm):
        # A half barrel along X with a thick shell.
        segs = 14
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
    lid_obj.location = (0, 0, base + H + 0.02)
    shade(lid_obj, wood2, bevel=0.01).parent = root
    # Soul light leaking from the seam between lid and body.
    box('SoulSeam', (W - 0.08, D - 0.04, 0.02), (0, 0, base + H + 0.01), soul, bevel=0.0)
    # Iron bands over body and lid, corner guards with spikes and rivets, feet.
    for x in (-0.42, 0.42):
        box(f'Band{x}', (0.07, D + 0.03, H + 0.02), (x, 0, base + H / 2), iron, bevel=0.008)
        band = mesh_obj(f'LidBand{x}', lambda bm: bmesh.ops.create_cone(bm, cap_ends=False, segments=28, radius1=D / 2 + 0.025, radius2=D / 2 + 0.025, depth=0.07))
        band.location = (x, 0, base + H + 0.02)
        band.rotation_euler = (0, math.radians(90), 0)
        shade(band, iron).parent = root
    for x in (-1, 1):
        for y in (-1, 1):
            box(f'Corner{x}{y}', (0.09, 0.09, H + 0.04), (x * (W / 2 - 0.02), y * (D / 2 - 0.02), base + H / 2), iron, bevel=0.012)
            spike = mesh_obj(f'Spike{x}{y}', lambda bm: bmesh.ops.create_cone(bm, cap_ends=True, segments=4, radius1=0.06, radius2=0.0, depth=0.12))
            spike.location = (x * (W / 2 - 0.02), y * (D / 2 - 0.02), base - 0.03)
            spike.rotation_euler = (math.pi, 0, math.radians(45))
            shade(spike, iron).parent = root
            for k in range(3):
                rv = mesh_obj(f'Rivet{x}{y}{k}', lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=1, radius=0.017))
                rv.location = (x * 0.42, y * (D / 2 + 0.02), base + 0.1 + k * 0.15)
                shade(rv, gold, smooth=True).parent = root

    def shield(bm):
        pts = [(-0.14, 0.16), (0.14, 0.16), (0.14, -0.02), (0.0, -0.18), (-0.14, -0.02)]
        vs = [bm.verts.new((x, 0.0, z)) for x, z in pts]
        f = bm.faces.new(vs)
        geom = bmesh.ops.extrude_face_region(bm, geom=[f])
        for v in [e for e in geom['geom'] if isinstance(e, bmesh.types.BMVert)]:
            v.co.y -= 0.04
    plate = mesh_obj('LockPlate', shield)
    plate.location = (0, -D / 2 - 0.005, base + H - 0.08)
    shade(plate, gold, bevel=0.012).parent = root
    key = mesh_obj('Keyhole', lambda bm: crystal(bm, 6, 0.035, 0.03, 0.05, 0.05))
    key.location = (0, -D / 2 - 0.05, base + H - 0.1)
    key.rotation_euler = (math.radians(90), 0, 0)
    shade(key, soul).parent = root
    # Skull on the lid: cranium, cheeks, jaw, dark sockets with soul eyes.
    cranium = mesh_obj('Cranium', lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=2, radius=0.11))
    cranium.location = (0, -0.3, base + H + 0.3)
    cranium.scale = (1.0, 0.95, 0.9)
    shade(cranium, bone, smooth=True).parent = root
    box('Cheeks', (0.16, 0.1, 0.07), (0, -0.33, base + H + 0.22), bone, bevel=0.02)
    box('Jaw', (0.12, 0.08, 0.05), (0, -0.34, base + H + 0.17), bone, bevel=0.015)
    for x in (-0.045, 0.045):
        socket = mesh_obj(f'Socket{x}', lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=1, radius=0.035))
        socket.location = (x, -0.39, base + H + 0.29)
        shade(socket, dark, smooth=True).parent = root
        eye = mesh_obj(f'Eye{x}', lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=1, radius=0.016))
        eye.location = (x, -0.415, base + H + 0.29)
        shade(eye, soul).parent = root
    for x in (-1, 1):
        for y in (-1, 1):
            box(f'Foot{x}{y}', (0.13, 0.13, 0.08), (x * 0.5, y * 0.3, 0.04), iron, bevel=0.015)
    return root


# ---------------------------------------------------------------- scene and render

def setup_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    s = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
    s.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines else 'BLENDER_EEVEE'
    s.render.resolution_x, s.render.resolution_y = 1600, 900
    s.view_settings.view_transform = 'Standard'  # true colours, as the game shows them (AgX washes the gems out)
    world = bpy.data.worlds.new('World')
    world.use_nodes = True
    world.node_tree.nodes['Background'].inputs[0].default_value = (0.16, 0.17, 0.2, 1)  # metals need something to reflect
    world.node_tree.nodes['Background'].inputs[1].default_value = 1.0
    s.world = world
    # Glow like the game's emissive parts (compositor API differs between Blender versions; optional).
    try:
        setup_glow(s)
    except Exception as e:
        print('GLOW_SKIPPED', e, flush=True)
    # Ground like a Soul Hunter floor, key + rim light.
    ground = mesh_obj('Ground', lambda bm: bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=20))
    shade(ground, mat('Ground', (0.07, 0.08, 0.07), rough=0.9))
    key = link(bpy.data.objects.new('Key', bpy.data.lights.new('Key', 'SUN')))
    key.data.energy = 3.0
    key.rotation_euler = (math.radians(50), 0, math.radians(-30))
    rim = link(bpy.data.objects.new('Rim', bpy.data.lights.new('Rim', 'AREA')))
    rim.data.energy = 400
    rim.data.size = 6
    rim.data.color = (0.6, 0.8, 1.0)
    rim.location = (0, 6, 4)
    rim.rotation_euler = (math.radians(-60), 0, 0)
    return s


def setup_glow(s):
    if hasattr(s, 'node_tree'):
        s.use_nodes = True
        tree = s.node_tree
        for node in list(tree.nodes):
            tree.nodes.remove(node)
        out = tree.nodes.new('CompositorNodeComposite')
        out_socket = out.inputs['Image']
    else:  # Blender 5: the compositor is a node group
        tree = bpy.data.node_groups.new('Compositor', 'CompositorNodeTree')
        s.compositing_node_group = tree
        tree.interface.new_socket('Image', in_out='OUTPUT', socket_type='NodeSocketColor')
        out = tree.nodes.new('NodeGroupOutput')
        out_socket = out.inputs[0]
    rl = tree.nodes.new('CompositorNodeRLayers')
    glare = tree.nodes.new('CompositorNodeGlare')
    if hasattr(glare, 'glare_type'):
        glare.glare_type = 'FOG_GLOW'
    elif 'Type' in glare.inputs:
        try: glare.inputs['Type'].default_value = 'Fog Glow'
        except Exception: pass
    for key, value in (('threshold', 1.5), ('size', 6)):
        if hasattr(glare, key): setattr(glare, key, value)
        elif key.capitalize() in glare.inputs: glare.inputs[key.capitalize()].default_value = value
    tree.links.new(rl.outputs['Image'], glare.inputs['Image'])
    tree.links.new(glare.outputs['Image'], out_socket)


def label(text, loc, size=0.13):
    curve = bpy.data.curves.new(text, 'FONT')
    curve.body = text
    curve.size = size
    curve.align_x = 'CENTER'
    o = link(bpy.data.objects.new('Label_' + text, curve))
    o.location = loc
    o.rotation_euler = (math.radians(60), 0, 0)
    o.data.materials.append(mat('Label', (0.85, 0.85, 0.85), rough=0.6))
    return o


def camera(name, target, distance, lens=50, height=0.55):
    cam = link(bpy.data.objects.new(name, bpy.data.cameras.new(name)))
    cam.data.lens = lens
    cam.location = target + Vector((0, -distance, distance * height))
    cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
    return cam


def render(cam, name):
    s = bpy.context.scene
    s.camera = cam
    s.render.filepath = os.path.join(OUT, name + '.png')
    bpy.ops.render.render(write_still=True)
    print('RENDERED', name, flush=True)


MATERIALS = {}


def remember_materials():
    """Material values for the Unity importer (the FBX carries only names)."""
    for m in bpy.data.materials:
        if not m.use_nodes or 'Principled BSDF' not in m.node_tree.nodes:
            continue
        p = m.node_tree.nodes['Principled BSDF']
        MATERIALS[m.name] = dict(color=list(p.inputs['Base Color'].default_value)[:3], metallic=p.inputs['Metallic'].default_value,
                                 roughness=p.inputs['Roughness'].default_value,
                                 emission=list(p.inputs['Emission Color'].default_value)[:3], strength=p.inputs['Emission Strength'].default_value)


def export_all(models_dir):
    """Clean scene, build each approved pickup at the origin and export it as its own FBX (modifiers applied)."""
    import json
    bpy.ops.wm.read_factory_settings(use_empty=True)
    builds = [('SH10_Pickup_SoulGem_T1', soul_gem_small), ('SH10_Pickup_SoulGem_T2', soul_gem_medium),
              ('SH10_Pickup_SoulGem_T3', soul_gem_large),
              ('SH10_Pickup_Coin_Gold', lambda: coin('Coin_Gold', (1.0, 0.72, 0.25), (1.0, 0.85, 0.45))),
              ('SH10_Pickup_Coin_Silver', lambda: coin('Coin_Silver', (0.78, 0.8, 0.86), (0.95, 0.96, 1.0))),
              ('SH10_Pickup_Coin_Bronze', lambda: coin('Coin_Bronze', (0.72, 0.42, 0.22), (0.9, 0.6, 0.35))),
              ('SH10_Pickup_Chest', chest)]
    os.makedirs(models_dir, exist_ok=True)
    for asset, build in builds:
        for o in list(bpy.data.objects):
            bpy.data.objects.remove(o, do_unlink=True)
        root = build()
        root.name = asset
        bpy.ops.object.select_all(action='DESELECT')
        for o in [root] + list(root.children_recursive):
            o.select_set(True)
        path = os.path.join(models_dir, asset + '.fbx')
        bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH', 'EMPTY'}, use_mesh_modifiers=True,
                                 axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL')
        print('EXPORTED', asset, flush=True)
    remember_materials()
    with open(os.path.join(os.path.dirname(OUT_MANIFEST), os.path.basename(OUT_MANIFEST)), 'w', encoding='utf-8') as f:
        json.dump(MATERIALS, f, indent=2)
    print('EXPORT_DONE', len(MATERIALS), 'materials', flush=True)


OUT_MANIFEST = ''

if __name__ == '__main__' and '--export' in sys.argv:
    # blender --background --python build_pickups_v2.py -- <renders folder> --export <project root>
    project = sys.argv[sys.argv.index('--export') + 1]
    OUT_MANIFEST = os.path.join(project, 'ArtSource', 'SH10_Pickups_v2', 'materials.json')
    os.makedirs(os.path.dirname(OUT_MANIFEST), exist_ok=True)
    export_all(os.path.join(project, 'Assets', 'Art', 'Models'))
elif __name__ == '__main__':
    s = setup_scene()
    items = [
        (soul_gem_small(), 'Soul Gem  Tier 1', 0.9), (soul_gem_medium(), 'Tier 2', 0.9), (soul_gem_large(), 'Tier 3', 0.9),
        (coin('Coin_Gold', (1.0, 0.72, 0.25), (1.0, 0.85, 0.45)), 'Soul Coin  Gold', 1.3),
        (coin('Coin_Silver', (0.78, 0.8, 0.86), (0.95, 0.96, 1.0)), 'Silver', 1.3),
        (coin('Coin_Bronze', (0.72, 0.42, 0.22), (0.9, 0.6, 0.35)), 'Bronze', 1.3),
        (chest(), 'Soul Reliquary Chest', 2.2),
    ]
    x = -4.2
    for obj, text, width in items:
        obj.location = (x + width / 2, 0, 0.56 if 'Coin' in obj.name else (0.02 if obj.name == 'SoulChest' else 0.25))
        if 'Coin' in obj.name:
            obj.rotation_euler = (math.radians(80), 0, math.radians(15))
        label(text, (x + width / 2, -0.9, 0.02))
        x += width + 0.35
    center_x = (-4.2 + x - 0.35) / 2
    sheet = camera('SheetCam', Vector((center_x - 0.35, 0, 0.45)), 13.5, lens=45, height=0.38)
    render(sheet, 'pickups_v2_sheet')
    # Close-ups for detail.
    for obj, name, dist in [(bpy.data.objects['SoulGem_Large'], 'closeup_gems', 2.6), (bpy.data.objects['Coin_Gold'], 'closeup_coins', 3.2),
                            (bpy.data.objects['SoulChest'], 'closeup_chest', 3.0)]:
        render(camera('Cam_' + name, obj.location + Vector((0, 0, 0.3)), dist, lens=50, height=0.5), name)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, 'pickups_v2.blend'))
    print('DONE', flush=True)
