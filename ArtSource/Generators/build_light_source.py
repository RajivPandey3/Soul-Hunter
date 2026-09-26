"""Soul Hunter destructible light source - the Soul Lantern (VS "light source", owner request 2026-09-26).

A breakable stage prop: octagonal stone plinth, short column, a wrought-iron lantern cage with a pointed roof and a
cyan soul flame inside (orange embers below it). Soul Hunter's own design; it reuses the pickups v2 materials,
lighting and render setup.

Concept render for owner approval:
  blender --background --python ArtSource/Generators/build_light_source.py -- <renders folder>
Export after approval (FBX + material values for LightSourceImporter):
  blender --background --python ArtSource/Generators/build_light_source.py -- <renders folder> --export <project root>
"""
import importlib.util
import json
import math
import os
import sys

import bmesh
import bpy
from mathutils import Vector

_spec = importlib.util.spec_from_file_location('pickups_v2', os.path.join(os.path.dirname(os.path.abspath(__file__)), 'build_pickups_v2.py'))
pv2 = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(pv2)
mat, shade, link, mesh_obj = pv2.mat, pv2.shade, pv2.link, pv2.mesh_obj
OUT = pv2.OUT
ASSET = 'SH10_Prop_SoulLantern'


def cylinder(name, verts, radius, depth, z, material, top=None, bevel=0.0, smooth=False):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=radius, radius2=radius if top is None else top, depth=depth,
                                    location=(0, 0, z + depth / 2))
    o = bpy.context.object
    o.name = name
    return shade(o, material, bevel=bevel, smooth=smooth)


def soul_lantern():
    stone = mat('Lantern_Stone', (0.34, 0.35, 0.39), rough=0.85)
    stone_dark = mat('Lantern_StoneDark', (0.2, 0.21, 0.25), rough=0.9)
    iron = mat('Lantern_Iron', (0.12, 0.12, 0.14), metallic=0.85, rough=0.45)
    soul = mat('Lantern_Soul', (0.35, 0.95, 1.0), emission=(0.35, 0.95, 1.0), strength=6.0)
    ember = mat('Lantern_Ember', (1.0, 0.45, 0.12), emission=(1.0, 0.45, 0.12), strength=4.0)

    root = link(bpy.data.objects.new('SoulLantern', None))
    parts = [
        cylinder('Plinth', 8, 0.34, 0.1, 0.0, stone_dark, bevel=0.012),
        cylinder('PlinthTop', 8, 0.28, 0.06, 0.1, stone, top=0.24, bevel=0.01),
        cylinder('Column', 8, 0.12, 0.46, 0.16, stone, top=0.1, bevel=0.008),
        cylinder('Cap', 8, 0.2, 0.05, 0.62, stone_dark, bevel=0.008),
        cylinder('CageFloor', 8, 0.19, 0.03, 0.67, iron),
        cylinder('CageRing', 8, 0.2, 0.03, 1.0, iron),
        cylinder('Roof', 8, 0.17, 0.13, 1.03, iron, top=0.02),
        cylinder('Finial', 6, 0.025, 0.1, 1.16, iron, top=0.0),
    ]
    for p in parts:
        p.parent = root
    # Eight cage bars around the glass.
    for k in range(8):
        a = k * math.tau / 8
        bpy.ops.mesh.primitive_cube_add(size=1, location=(math.cos(a) * 0.18, math.sin(a) * 0.18, 0.85))
        bar = bpy.context.object
        bar.name = f'Bar{k}'
        bar.scale = (0.022, 0.022, 0.33)
        bar.rotation_euler = (0, 0, a)
        shade(bar, iron).parent = root
    # Soul flame: a tall, twisted teardrop; embers at its foot.
    # Open cage (no glass), so the flame reads between the bars from the tilted game camera.
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.135, location=(0, 0, 0.85))
    flame = bpy.context.object
    flame.name = 'SoulFlame'
    flame.scale = (1.0, 1.0, 1.6)
    shade(flame, soul, smooth=True).parent = root
    for k in range(3):
        a = k * math.tau / 3 + 0.4
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.045, location=(math.cos(a) * 0.09, math.sin(a) * 0.09, 0.72))
        shade(bpy.context.object, ember).parent = root
    # Two small hanging chains' worth of rivets on the cap, for a gothic read from above.
    for k in range(8):
        a = k * math.tau / 8 + math.pi / 8
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.018, location=(math.cos(a) * 0.19, math.sin(a) * 0.19, 0.675))
        shade(bpy.context.object, iron).parent = root
    return root


def export(project):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    root = soul_lantern()
    root.name = ASSET
    bpy.ops.object.select_all(action='DESELECT')
    for o in [root] + list(root.children_recursive):
        o.select_set(True)
    models = os.path.join(project, 'Assets', 'Art', 'Models')
    os.makedirs(models, exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(models, ASSET + '.fbx'), use_selection=True, object_types={'MESH', 'EMPTY'},
                             use_mesh_modifiers=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
                             apply_scale_options='FBX_SCALE_ALL')
    pv2.remember_materials()
    manifest = os.path.join(project, 'ArtSource', 'SH10_LightSource', 'materials.json')
    os.makedirs(os.path.dirname(manifest), exist_ok=True)
    with open(manifest, 'w', encoding='utf-8') as f:
        json.dump(pv2.MATERIALS, f, indent=2)
    tris = sum(len(p.vertices) - 2 for o in root.children_recursive if o.type == 'MESH' for p in o.data.polygons)
    print('EXPORT_DONE', ASSET, 'triangles', tris, flush=True)


if __name__ == '__main__' and '--export' in sys.argv:
    export(sys.argv[sys.argv.index('--export') + 1])
elif __name__ == '__main__':
    pv2.setup_scene()
    lantern = soul_lantern()
    # The hero's height for scale (a plain grey stand-in, ~1.9 units like the SH10 heroes).
    hero = mesh_obj('HeroScale', lambda bm: bmesh.ops.create_cone(bm, cap_ends=True, segments=8, radius1=0.28, radius2=0.2, depth=1.9))
    hero.location = (0.9, 0.2, 0.95)
    shade(hero, mat('HeroScale', (0.3, 0.3, 0.33), rough=0.8))
    pv2.label('Soul Lantern', (0, -0.8, 0.02), 0.16)
    pv2.label('hero height', (0.9, -0.8, 0.02), 0.1)
    pv2.render(pv2.camera('Front', Vector((0.3, 0, 0.7)), 4.2, lens=50, height=0.45), 'soul_lantern_front')
    # The game camera looks down at about 55 degrees.
    pv2.render(pv2.camera('Game', Vector((0.3, 0, 0.5)), 5.0, lens=50, height=1.43), 'soul_lantern_gamecam')
    pv2.render(pv2.camera('Close', Vector((0, 0, 0.8)), 1.9, lens=50, height=0.35), 'soul_lantern_closeup')
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, 'soul_lantern.blend'))
    print('DONE', flush=True)
