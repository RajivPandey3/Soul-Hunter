"""Soul Hunter item icons, 3D (owner, 2026-10-07: "blender 3D wala sample dikhao without circale").

Each item is a small Blender model, lit by a warm key, a cool rim and a soft fill, rendered at 512 px on a transparent
background (no badge). Shapes are Soul Hunter's own. This pass renders the approval samples only.
Run: D:/Blender/blender.exe --background --python ArtSource/Generators/build_icons_3d.py -- <output folder>
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Vector

OUT = sys.argv[sys.argv.index('--') + 1] if '--' in sys.argv else os.path.join(os.getcwd(), 'icons_3d')
os.makedirs(OUT, exist_ok=True)
SIZE = 512


# ---------------------------------------------------------------- materials and helpers

def mat(name, color, metallic=0.0, rough=0.45, emission=None, strength=0.0, coat=0.0, alpha=1.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes['Principled BSDF']
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Metallic'].default_value = metallic
    p.inputs['Roughness'].default_value = rough
    if 'Coat Weight' in p.inputs:
        p.inputs['Coat Weight'].default_value = coat
    if emission:
        p.inputs['Emission Color'].default_value = (*emission, 1)
        p.inputs['Emission Strength'].default_value = strength
    if alpha < 1.0:  # see-through (glass): blended, so what is inside shows
        p.inputs['Alpha'].default_value = alpha
        if hasattr(m, 'surface_render_method'):
            m.surface_render_method = 'BLENDED'
        elif hasattr(m, 'blend_method'):
            m.blend_method = 'BLEND'
    return m


def link(obj, parent=None):
    bpy.context.scene.collection.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    return obj


def mesh_obj(name, build, parent=None):
    bm = bmesh.new()
    build(bm)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    return link(bpy.data.objects.new(name, me), parent)


def finish(obj, material, bevel=0.0, segments=3, subsurf=0, smooth=True):
    obj.data.materials.clear()
    obj.data.materials.append(material)
    if bevel > 0:
        mod = obj.modifiers.new('Bevel', 'BEVEL')
        mod.width = bevel
        mod.segments = segments
    if subsurf:
        mod = obj.modifiers.new('Subsurf', 'SUBSURF')
        mod.levels = mod.render_levels = subsurf
    for poly in obj.data.polygons:
        poly.use_smooth = smooth
    return obj


def extrude_shape(points, depth):
    """A flat 2D outline (x, z) extruded along y: shields, hearts, blades."""
    def build(bm):
        front = [bm.verts.new((x, -depth / 2, z)) for x, z in points]
        back = [bm.verts.new((x, depth / 2, z)) for x, z in points]
        bm.faces.new(front[::-1])
        bm.faces.new(back)
        n = len(points)
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((front[i], front[j], back[j], back[i]))
    return build


def lathe(profile, steps=48):
    """A profile (radius, z) spun round z: bottles, flasks, coins."""
    def build(bm):
        rings = []
        for k in range(steps):
            a = 2 * math.pi * k / steps
            rings.append([bm.verts.new((r * math.cos(a), r * math.sin(a), z)) for r, z in profile])
        for k in range(steps):
            a, b = rings[k], rings[(k + 1) % steps]
            for i in range(len(profile) - 1):
                bm.faces.new((a[i], b[i], b[i + 1], a[i + 1]))
    return build


def sphere(name, radius, location, scale=(1, 1, 1), parent=None):
    o = mesh_obj(name, lambda bm: bmesh.ops.create_uvsphere(bm, u_segments=32, v_segments=16, radius=radius), parent)
    o.location = location
    o.scale = scale
    return o


def cylinder(name, radius, depth, location, rotation=(0, 0, 0), parent=None, segments=32):
    o = mesh_obj(name, lambda bm: bmesh.ops.create_cone(bm, cap_ends=True, segments=segments, radius1=radius,
                                                          radius2=radius, depth=depth), parent)
    o.location = location
    o.rotation_euler = rotation
    return o


def root(name):
    return link(bpy.data.objects.new(name, None))


# ---------------------------------------------------------------- the sample items

def potion(name, liquid, glow):
    r = root(name)
    glass = mat(name + '_Glass', (0.35, 0.45, 0.4), rough=0.04, coat=1.0, alpha=0.12)
    finish(mesh_obj('Bottle', lathe([(0.0, -0.02), (0.30, 0.0), (0.42, 0.18), (0.44, 0.40), (0.36, 0.62), (0.14, 0.74),
                                      (0.12, 0.98), (0.15, 1.02), (0.0, 1.02)]), r), glass)
    finish(mesh_obj('Liquid', lathe([(0.0, 0.03), (0.28, 0.04), (0.39, 0.19), (0.405, 0.40), (0.33, 0.58), (0.0, 0.58)]), r),
           mat(name + '_Liquid', liquid, rough=0.15, emission=glow, strength=1.2, coat=0.6))
    finish(cylinder('Cork', 0.13, 0.16, (0, 0, 1.08), parent=r), mat('Cork', (0.45, 0.27, 0.13), rough=0.8), bevel=0.02)
    return r


def shield():
    r = root('Armor')
    outline = [(-0.55, 0.62), (0.0, 0.78), (0.55, 0.62), (0.52, 0.05), (0.0, -0.62), (-0.52, 0.05)]
    finish(mesh_obj('Plate', extrude_shape(outline, 0.16), r), mat('ShieldBlue', (0.08, 0.28, 0.85), metallic=0.6, rough=0.3),
           bevel=0.06, segments=4)
    rim = [(x * 1.08, z * 1.06 + 0.01) for x, z in outline]
    o = finish(mesh_obj('Rim', extrude_shape(rim, 0.1), r), mat('Gold', (1.0, 0.72, 0.25), metallic=1.0, rough=0.25), bevel=0.03)
    o.location = (0, 0.05, 0)
    boss = [(-0.06, 0.62), (0.06, 0.62), (0.06, -0.45), (0.0, -0.52), (-0.06, -0.45)]
    o = finish(mesh_obj('Spine', extrude_shape(boss, 0.12), r), mat('Gold', (1.0, 0.72, 0.25), metallic=1.0, rough=0.25), bevel=0.03)
    o.location = (0, -0.1, 0)
    return r


def heart():
    r = root('HollowHeart')
    pts = []
    for i in range(64):
        t = 2 * math.pi * i / 64
        x = 16 * math.sin(t) ** 3
        z = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((x / 30, z / 30))
    finish(mesh_obj('Heart', extrude_shape(pts, 0.3), r), mat('HeartRed', (0.62, 0.01, 0.04), rough=0.3, coat=0.9),
           bevel=0.12, segments=6)
    crack = [(-0.04, 0.33), (0.05, 0.33), (-0.02, 0.12), (0.08, 0.02), (-0.04, -0.2), (0.02, 0.02), (-0.08, 0.12)]
    o = finish(mesh_obj('Crack', extrude_shape(crack, 0.05), r), mat('Crack', (0.12, 0.0, 0.02), rough=0.6))
    o.location = (0, -0.17, 0)
    return r


def skull():
    r = root('Skull')
    bone = mat('Bone', (0.8, 0.72, 0.58), rough=0.6)
    finish(sphere('Cranium', 0.5, (0, 0, 0.12), (0.88, 0.85, 0.82), r), bone, subsurf=1)
    jaw = finish(cylinder('Jaw', 0.34, 0.32, (0, -0.06, -0.38), parent=r), bone, bevel=0.08, segments=4)
    jaw.scale = (1.05, 0.9, 1)
    dark = mat('Socket', (0.02, 0.01, 0.01), rough=0.9)
    eye = mat('EyeGlow', (1.0, 0.2, 0.1), emission=(1.0, 0.25, 0.08), strength=6)
    for x in (-0.19, 0.19):
        finish(sphere('Socket', 0.13, (x * 0.9, -0.34, 0.06), (1, 0.6, 1.1), r), dark)
        finish(sphere('Eye', 0.05, (x * 0.9, -0.43, 0.05), parent=r), eye)
    nose = [(-0.06, -0.12), (0.06, -0.12), (0.0, 0.0)]
    o = finish(mesh_obj('Nose', extrude_shape(nose, 0.08), r), dark)
    o.location = (0, -0.42, -0.02)
    for x in (-0.14, -0.05, 0.05, 0.14):
        finish(cylinder('Tooth', 0.035, 0.14, (x, -0.33, -0.36), (math.radians(0), 0, 0), r, 12), dark)
    return r


def axe():
    r = root('Axe')
    finish(cylinder('Haft', 0.055, 1.1, (0, 0, 0.1), parent=r), mat('Wood', (0.3, 0.14, 0.06), rough=0.7), bevel=0.01)
    blade = [(0.04 + (x - 0.04) * 1.4, 0.45 + (z - 0.45) * 1.4) for x, z in
             [(0.04, 0.58), (0.22, 0.62), (0.42, 0.80), (0.62, 0.86), (0.74, 0.62), (0.76, 0.40), (0.68, 0.14), (0.48, 0.04), (0.26, 0.20), (0.04, 0.32)]]
    finish(mesh_obj('Blade', extrude_shape(blade, 0.07), r), mat('Steel', (0.78, 0.8, 0.85), metallic=1.0, rough=0.22),
           bevel=0.025, segments=3)
    edge = [(0.04 + (x - 0.04) * 1.4, 0.45 + (z - 0.45) * 1.4) for x, z in
            [(0.60, 0.85), (0.72, 0.62), (0.74, 0.40), (0.66, 0.15), (0.71, 0.14), (0.80, 0.40), (0.78, 0.63), (0.65, 0.87)]]
    o = finish(mesh_obj('Edge', extrude_shape(edge, 0.03), r), mat('Edge', (1.0, 1.0, 1.0), metallic=1.0, rough=0.1))
    finish(cylinder('Collar', 0.08, 0.22, (0, 0, 0.45), parent=r), mat('DarkIron', (0.2, 0.2, 0.22), metallic=1.0, rough=0.4), bevel=0.02)
    r.rotation_euler = (0, math.radians(-35), 0)
    return r


def garlic():
    r = root('Garlic')
    skin = mat('GarlicSkin', (0.82, 0.72, 0.56), rough=0.6, coat=0.15)
    for i, (x, y, s) in enumerate([(0, 0, 0.36), (-0.22, 0.05, 0.27), (0.22, 0.05, 0.27), (-0.12, -0.18, 0.26),
                                   (0.12, -0.18, 0.26)]):
        finish(sphere('Clove%d' % i, s, (x, y, 0.02), (0.85, 0.85, 1.05), r), skin, subsurf=1)
    finish(mesh_obj('Tip', lathe([(0.0, 0.25), (0.13, 0.28), (0.05, 0.52), (0.02, 0.62), (0.0, 0.62)]), r), skin)
    finish(sphere('Root', 0.16, (0, 0, -0.33), (1, 1, 0.3), r), mat('Root', (0.55, 0.45, 0.32), rough=0.9))
    finish(sphere('Blush', 0.22, (-0.12, -0.24, -0.04), (0.7, 0.5, 0.9), r), mat('Blush', (0.55, 0.25, 0.5), rough=0.6))
    finish(sphere('Blush2', 0.2, (0.14, -0.23, -0.04), (0.6, 0.5, 0.85), r), mat('Blush', (0.55, 0.25, 0.5), rough=0.6))
    return r


def coins():
    r = root('Greed')
    gold = mat('CoinGold', (1.0, 0.7, 0.18), metallic=1.0, rough=0.22)
    for i in range(4):
        o = finish(cylinder('Coin%d' % i, 0.32, 0.08, (-0.22 + 0.02 * (i % 2), 0.0, -0.30 + i * 0.09), parent=r), gold, bevel=0.015)
    face = finish(cylinder('Face', 0.36, 0.09, (0.25, -0.05, 0.05), (math.radians(80), 0, math.radians(15)), r), gold, bevel=0.02)
    finish(cylinder('Mark', 0.18, 0.1, (0.26, -0.06, 0.05), (math.radians(80), 0, math.radians(15)), r, 6),
           mat('Ember', (1.0, 0.45, 0.1), emission=(1.0, 0.4, 0.05), strength=3))
    return r


def holy_water():
    r = root('SantaWater')
    glass = mat('HolyGlass', (0.35, 0.4, 0.5), rough=0.04, coat=1.0, alpha=0.12)
    finish(mesh_obj('Flask', lathe([(0.0, -0.4), (0.25, -0.38), (0.46, -0.18), (0.48, 0.05), (0.38, 0.28), (0.13, 0.4),
                                    (0.11, 0.68), (0.14, 0.72), (0.0, 0.72)]), r), glass)
    finish(mesh_obj('Water', lathe([(0.0, -0.36), (0.23, -0.35), (0.43, -0.16), (0.45, 0.04), (0.0, 0.04)]), r),
           mat('Water', (0.05, 0.3, 0.95), rough=0.1, emission=(0.1, 0.4, 1.0), strength=1.2, coat=0.6))
    finish(cylinder('Stopper', 0.12, 0.14, (0, 0, 0.78), parent=r), mat('Gold', (1.0, 0.72, 0.25), metallic=1.0, rough=0.25), bevel=0.02)
    cross_v = [(-0.035, 0.18), (0.035, 0.18), (0.035, -0.18), (-0.035, -0.18)]
    o = finish(mesh_obj('CrossV', extrude_shape(cross_v, 0.03), r), mat('Gold', (1.0, 0.72, 0.25), metallic=1.0, rough=0.25))
    o.location = (0, -0.47, -0.05)
    cross_h = [(-0.11, 0.05), (0.11, 0.05), (0.11, -0.02), (-0.11, -0.02)]
    o = finish(mesh_obj('CrossH', extrude_shape(cross_h, 0.03), r), mat('Gold', (1.0, 0.72, 0.25), metallic=1.0, rough=0.25))
    o.location = (0, -0.47, 0.0)
    return r


SAMPLES = [
    ('Might', lambda: potion('Might', (0.04, 0.6, 0.08), (0.1, 0.8, 0.12))),
    ('Armor', shield), ('HollowHeart', heart), ('SkullOManiac', skull),
    ('Axe', axe), ('Garlic', garlic), ('Greed', coins), ('SantaWater', holy_water),
]


# ---------------------------------------------------------------- scene, lights, camera

def setup_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    s = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
    s.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines else 'BLENDER_EEVEE'
    s.render.resolution_x = s.render.resolution_y = SIZE
    s.render.film_transparent = True
    s.render.image_settings.color_mode = 'RGBA'
    s.view_settings.view_transform = 'Standard'
    world = bpy.data.worlds.new('World')
    world.use_nodes = True
    world.node_tree.nodes['Background'].inputs[0].default_value = (0.25, 0.22, 0.24, 1)  # metals have something to reflect
    world.node_tree.nodes['Background'].inputs[1].default_value = 0.45
    s.world = world
    key = link(bpy.data.objects.new('Key', bpy.data.lights.new('Key', 'AREA')))
    key.data.energy, key.data.size, key.data.color = 400, 3, (1.0, 0.93, 0.85)
    key.location = (-2.5, -3.0, 3.0)
    key.rotation_euler = (Vector((0, 0, 0)) - key.location).to_track_quat('-Z', 'Y').to_euler()
    rim = link(bpy.data.objects.new('Rim', bpy.data.lights.new('Rim', 'AREA')))
    rim.data.energy, rim.data.size, rim.data.color = 380, 2, (0.55, 0.85, 1.0)
    rim.location = (2.5, 2.5, 2.0)
    rim.rotation_euler = (Vector((0, 0, 0)) - rim.location).to_track_quat('-Z', 'Y').to_euler()
    fill = link(bpy.data.objects.new('Fill', bpy.data.lights.new('Fill', 'AREA')))
    fill.data.energy, fill.data.size, fill.data.color = 90, 4, (1.0, 0.6, 0.4)
    fill.location = (3.0, -2.5, -0.5)
    fill.rotation_euler = (Vector((0, 0, 0)) - fill.location).to_track_quat('-Z', 'Y').to_euler()
    return s


def bounds(obj):
    deps = bpy.context.evaluated_depsgraph_get()
    lo = Vector((1e9, 1e9, 1e9)); hi = -lo
    for child in [obj] + list(obj.children_recursive):
        if child.type != 'MESH':
            continue
        ev = child.evaluated_get(deps)
        for corner in ev.bound_box:
            w = child.matrix_world @ Vector(corner)
            lo = Vector(map(min, lo, w)); hi = Vector(map(max, hi, w))
    return lo, hi


def render(obj, name):
    s = bpy.context.scene
    for o in s.objects:
        if o.type in ('MESH', 'EMPTY') and o != obj and o not in obj.children_recursive:
            o.hide_render = True
    for o in [obj] + list(obj.children_recursive):
        o.hide_render = False
    bpy.context.view_layer.update()
    lo, hi = bounds(obj)
    centre = (lo + hi) / 2
    size = max(hi.x - lo.x, hi.z - lo.z, hi.y - lo.y)
    cam = s.camera
    direction = Vector((0.25, -1.0, 0.35)).normalized()  # a little from above and the side: reads as 3D
    cam.location = centre + direction * 10
    cam.rotation_euler = (centre - cam.location).to_track_quat('-Z', 'Y').to_euler()
    cam.data.type = 'ORTHO'
    cam.data.ortho_scale = size * 1.18
    s.render.filepath = os.path.join(OUT, f'SH10_Icon3D_{name}.png')
    bpy.ops.render.render(write_still=True)
    print('RENDERED', name, flush=True)


def main():
    s = setup_scene()
    cam = link(bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam')))
    s.camera = cam
    items = []
    for i, (name, make) in enumerate(SAMPLES):
        obj = make()
        obj.location = (0, 0, 0)  # the lights aim at the origin; render() shows one item at a time
        items.append((name, obj))
    for name, obj in items:
        render(obj, name)


main()
