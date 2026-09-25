"""Soul Hunter hero models: one low-poly, rigged model per playable character.
Run: blender --background --python ArtSource/Generators/build_heroes.py
Same faceted style, rig (bone names/positions) and clips as SH10_Kael (build_campaign_pack.py),
so SH10_Kael.controller drives every hero. Each hero gets its own silhouette, palette and a
prop that reads as its starting weapon. Never overwrites existing assets.
"""
import bpy
import json
import math
import os
from pathlib import Path
from mathutils import Vector

# SOULHUNTER_ROOT lets the batch run against an isolated copy of the project first.
ROOT = Path(os.environ.get("SOULHUNTER_ROOT") or Path(__file__).resolve().parent.parent.parent)
OUT = ROOT / 'ArtSource' / 'SH10_Heroes_v01'
MODELS = ROOT / 'Assets' / 'Art' / 'Models'
PREVIEWS = OUT / 'Previews'

BASE = {
    'Armor': (0.13, 0.18, 0.23), 'Cloth': (0.26, 0.035, 0.055), 'Trim': (0.48, 0.28, 0.10),
    'Dark': (0.055, 0.075, 0.095), 'Soul': (0.10, 0.85, 0.80), 'Skin': (0.62, 0.45, 0.34),
    'Hair': (0.12, 0.08, 0.06), 'Accent': (0.70, 0.10, 0.10), 'Wood': (0.16, 0.095, 0.065),
    'Bone': (0.58, 0.57, 0.40),
}

# name: palette overrides, bulk (limb/torso thickness), head style, body extras, right-hand prop, left-hand prop
HEROES = {
    'Corvan': dict(pal=dict(Armor=(.25, .17, .10), Cloth=(.10, .09, .07), Trim=(.55, .36, .12), Hair=(.20, .20, .22)),
                   bulk=1.05, head='wide_hat', body=['coat', 'scarf'], right='whip', left='lantern'),
    'Aria': dict(pal=dict(Armor=(.42, .28, .15), Cloth=(.10, .45, .20), Trim=(.85, .62, .20), Accent=(.85, .12, .12), Soul=(.45, .85, 1.0), Hair=(.55, .25, .08)),
                 bulk=.9, head='feather_cap', body=['short_cape'], right='lute', left=None),
    'Draven': dict(pal=dict(Armor=(.07, .07, .09), Cloth=(.18, .02, .05), Trim=(.38, .38, .42), Soul=(.95, .08, .08)),
                   bulk=.85, head='hood_mask', body=['scarf', 'belt_daggers'], right='knife', left='knife'),
    'Gideon': dict(pal=dict(Armor=(.36, .20, .11), Cloth=(.30, .09, .04), Trim=(.52, .48, .42), Soul=(1.0, .50, .10), Hair=(.25, .12, .05)),
                   bulk=1.45, head='horned_cap', body=['big_pauldrons', 'belt'], right='axe', left=None),
    'Isolde': dict(pal=dict(Armor=(.18, .10, .22), Cloth=(.30, .07, .40), Trim=(.15, .55, .28), Soul=(.40, 1.0, .30), Hair=(.08, .04, .10)),
                   bulk=.9, head='witch_hat', body=['robe'], right='flask', left=None),
    'Kaelen': dict(pal=dict(Armor=(.10, .10, .12), Cloth=(.50, .05, .05), Trim=(.72, .72, .76), Soul=(1.0, .35, .10), Hair=(.08, .08, .08)),
                   bulk=1.0, head='tricorn', body=['coat', 'crossbelt'], right='cross', left=None),
    'Lumina': dict(pal=dict(Armor=(.80, .76, .62), Cloth=(.88, .87, .82), Trim=(.90, .70, .25), Soul=(1.0, .85, .30), Hair=(.90, .75, .35)),
                   bulk=.95, head='halo', body=['robe', 'tabard'], right='book', left=None),
    'Elaria': dict(pal=dict(Armor=(.20, .22, .35), Cloth=(.10, .18, .58), Trim=(.75, .78, .85), Soul=(.30, .80, 1.0)),
                   bulk=.9, head='deep_hood', body=['robe', 'orbit_crystals'], right='staff', left=None),
    'Rurik': dict(pal=dict(Armor=(.30, .30, .32), Cloth=(.14, .30, .14), Trim=(.36, .22, .12), Soul=(.90, .20, .10), Hair=(.70, .30, .08), Accent=(.40, .26, .14)),
                  bulk=1.35, head='horned_helm', body=['fur', 'kilt'], right='bone_club', left=None),
    'Seraphina': dict(pal=dict(Armor=(.60, .62, .70), Cloth=(.55, .20, .62), Trim=(.85, .85, .92), Soul=(1.0, .35, .85), Hair=(.92, .92, .96)),
                      bulk=.85, head='tiara', body=['skirt', 'orbit_crystals'], right='wand', left=None),
    'Sylas': dict(pal=dict(Armor=(.12, .08, .20), Cloth=(.05, .03, .09), Trim=(.30, .20, .45), Soul=(.62, .22, 1.0)),
                  bulk=1.0, head='void_hood', body=['robe', 'void_rings'], right='rune', left=None),
    'Valerius': dict(pal=dict(Armor=(.62, .64, .68), Cloth=(.10, .15, .45), Trim=(.85, .65, .20), Accent=(.80, .08, .08), Soul=(.40, .70, 1.0)),
                     bulk=1.2, head='great_helm', body=['tabard', 'big_pauldrons'], right='sword', left='shield'),
}

palette = dict(BASE)
materials = {}
parts = []
assignments = {}
current_bone = ['Spine']


def material(key):
    if key not in materials:
        m = bpy.data.materials.new('Hero_' + key)
        m.diffuse_color = (*palette[key], 1)
        m.use_nodes = True
        bsdf = m.node_tree.nodes.get('Principled BSDF')
        bsdf.inputs['Base Color'].default_value = (*palette[key], 1)
        bsdf.inputs['Roughness'].default_value = .8
        materials[key] = m
    return materials[key]


def finish(obj, name, scale, mat):
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material(mat))
    parts.append(obj)
    assignments[obj.name] = current_bone[0]
    return obj


def box(name, pos, size, mat='Armor', bevel=0.02):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    obj = finish(bpy.context.object, name, size, mat)
    if bevel:
        mod = obj.modifiers.new('Crafted_edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


def cone(name, pos, radius, top, height, mat='Armor', verts=8, rot=None):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=radius, radius2=top, depth=height, location=pos)
    obj = finish(bpy.context.object, name, (1, 1, 1), mat)
    if rot:
        obj.rotation_euler = rot
    return obj


def gem(name, pos, scale, mat='Soul'):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1, location=pos)
    return finish(bpy.context.object, name, scale, mat)


def limb(name, start, end, radius, mat='Armor', tip=None):
    midpoint = (Vector(start) + Vector(end)) / 2
    direction = Vector(end) - Vector(start)
    obj = cone(name, midpoint, radius, radius if tip is None else tip, direction.length, mat)
    obj.rotation_euler = direction.to_track_quat('Z', 'Y').to_euler()
    return obj


def torus(name, pos, major, minor, mat='Trim', rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=16, minor_segments=6,
                                     location=pos, rotation=rotation)
    return finish(bpy.context.object, name, (1, 1, 1), mat)


class bone:
    """with bone('Head'): parts created inside are skinned to that bone."""
    def __init__(self, name): self.name = name
    def __enter__(self): self.prev = current_bone[0]; current_bone[0] = self.name
    def __exit__(self, *a): current_bone[0] = self.prev


HAND = {'R': Vector((.50, -.12, .80)), 'L': Vector((-.50, -.12, .80))}


def body(bulk):
    with bone('Spine'):
        cone('Torso', (0, 0, 1.25), .23 * bulk, .35 * bulk, .50, 'Armor')
        box('Chest', (0, -.24 * bulk, 1.27), (.40 * bulk, .08, .30), 'Cloth')
        gem('SoulSeal', (0, -.30 * bulk, 1.30), (.07, .04, .12), 'Soul')
        cone('Pelvis', (0, 0, .98), .26 * bulk, .22 * bulk, .16, 'Dark')
    with bone('Head'):
        cone('Neck', (0, 0, 1.55), .09 * bulk, .08 * bulk, .12, 'Skin')
    for side, s in [('L', -1), ('R', 1)]:
        with bone('Thigh.' + side):
            limb('Thigh', (s * .18, 0, 1), (s * .18, 0, .58), .12 * bulk, 'Cloth')
        with bone('Shin.' + side):
            limb('Shin', (s * .18, 0, .58), (s * .18, 0, .18), .10 * bulk, 'Armor')
            box('Boot', (s * .18, -.09, .13), (.24 * bulk, .40, .26), 'Dark')
        with bone('UpperArm.' + side):
            gem('Shoulder', (s * .39 * min(bulk, 1.15), 0, 1.46), (.19 * bulk, .20 * bulk, .15), 'Armor')
            limb('UpperArm', (s * .40, 0, 1.42), (s * .50, 0, 1.12), .10 * bulk, 'Cloth')
        with bone('Forearm.' + side):
            limb('Forearm', (s * .50, 0, 1.12), (s * .50, -.1, .83), .095 * bulk, 'Armor')
            gem('Hand', HAND[side], (.075 * bulk, .075 * bulk, .08), 'Skin')


def face(eyes='Dark'):
    gem('Face', (0, -.02, 1.76), (.17, .17, .21), 'Skin')
    for x in [-.065, .065]:
        box('Eye', (x, -.18, 1.79), (.045, .02, .03), eyes, 0)


def head(style):
    with bone('Head'):
        if style == 'wide_hat':
            face(); gem('Beard', (0, -.12, 1.64), (.13, .07, .08), 'Hair')
            cone('Brim', (0, 0, 1.93), .46, .46, .04, 'Dark', 12)
            cone('Crown', (0, 0, 2.07), .20, .17, .26, 'Dark', 10)
            cone('Band', (0, 0, 1.98), .205, .205, .05, 'Trim', 10)
        elif style == 'feather_cap':
            face(); gem('Hair', (0, .06, 1.80), (.19, .17, .19), 'Hair')
            cone('Cap', (.03, 0, 1.96), .20, .06, .22, 'Cloth', 8, (0, .35, 0))
            limb('Feather', (.10, .05, 1.98), (.34, .22, 2.28), .035, 'Accent', .005)
        elif style == 'hood_mask':
            cone('Hood', (0, .02, 1.84), .25, .06, .56, 'Cloth', 8)
            box('Shadow', (0, -.15, 1.77), (.24, .06, .2), 'Dark', 0)
            box('Mask', (0, -.19, 1.70), (.22, .03, .09), 'Armor', 0)
            for x in [-.06, .06]:
                box('Eye', (x, -.19, 1.79), (.05, .02, .025), 'Soul', 0)
        elif style == 'horned_cap':
            face(); gem('Beard', (0, -.12, 1.62), (.16, .09, .13), 'Hair')
            gem('SkullCap', (0, 0, 1.86), (.20, .20, .13), 'Armor')
            for s in [-1, 1]:
                limb('Horn', (s * .16, 0, 1.90), (s * .38, -.05, 2.18), .075, 'Bone', 0)
        elif style == 'witch_hat':
            face('Soul'); box('LongHair', (0, .10, 1.55), (.34, .12, .55), 'Hair')
            cone('Brim', (0, 0, 1.93), .44, .44, .035, 'Cloth', 12)
            cone('Point', (.05, .03, 2.30), .22, 0, .75, 'Cloth', 8, (-.15, .2, 0))
            cone('Band', (0, 0, 1.99), .205, .19, .07, 'Trim', 8)
        elif style == 'tricorn':
            face(); box('Scarf', (0, -.05, 1.60), (.30, .26, .08), 'Cloth')
            cone('Tricorn', (0, 0, 1.97), .40, .30, .12, 'Armor', 3, (0, 0, math.pi / 2))
            gem('HatCrown', (0, 0, 2.05), (.17, .17, .10), 'Armor')
            box('Emblem', (0, -.24, 1.99), (.05, .02, .12), 'Trim', 0)
        elif style == 'halo':
            face(); gem('Hair', (0, .07, 1.82), (.19, .17, .19), 'Hair'); gem('Bun', (0, .14, 1.97), (.09, .09, .09), 'Hair')
            torus('Halo', (0, .05, 2.14), .20, .025, 'Soul')
        elif style == 'deep_hood':
            cone('Hood', (0, .03, 1.86), .27, .08, .55, 'Cloth', 8)
            box('Shadow', (0, -.16, 1.76), (.25, .07, .22), 'Dark', 0)
            for x in [-.06, .06]:
                box('Eye', (x, -.20, 1.78), (.05, .02, .03), 'Soul', 0)
            box('HoodTrim', (0, -.20, 1.66), (.30, .04, .05), 'Trim', 0)
        elif style == 'horned_helm':
            face(); gem('Beard', (0, -.13, 1.60), (.18, .10, .17), 'Hair')
            gem('Helm', (0, 0, 1.88), (.21, .21, .14), 'Armor')
            for s in [-1, 1]:
                limb('Horn', (s * .18, 0, 1.90), (s * .42, 0, 2.05), .08, 'Bone', .01)
                limb('HornTip', (s * .42, 0, 2.05), (s * .46, 0, 2.30), .045, 'Bone', 0)
        elif style == 'tiara':
            face('Soul'); box('LongHair', (0, .10, 1.52), (.36, .12, .62), 'Hair'); gem('Hair', (0, .05, 1.82), (.19, .17, .19), 'Hair')
            torus('Tiara', (0, 0, 1.93), .17, .02, 'Trim')
            gem('TiaraGem', (0, -.17, 1.98), (.05, .03, .07), 'Soul')
        elif style == 'void_hood':
            cone('Hood', (0, .03, 1.86), .26, .03, .62, 'Cloth', 6)
            box('Void', (0, -.16, 1.76), (.24, .07, .24), 'Dark', 0)
            for x in [-.06, .06]:
                box('Eye', (x, -.20, 1.79), (.04, .02, .04), 'Soul', 0)
            torus('HaloRing', (0, 0, 2.02), .30, .02, 'Soul', (.4, 0, 0))
        elif style == 'great_helm':
            box('Helm', (0, 0, 1.78), (.32, .32, .36), 'Armor', .03)
            box('Visor', (0, -.165, 1.80), (.26, .02, .04), 'Dark', 0)
            box('Breaths', (0, -.165, 1.70), (.03, .02, .12), 'Dark', 0)
            box('Crest', (0, 0, 1.98), (.05, .30, .06), 'Trim', 0)
            cone('Plume', (0, .08, 2.12), .09, .01, .32, 'Accent', 6, (.5, 0, 0))
        else:
            raise RuntimeError(style)


def extras(items, bulk):
    for item in items:
        if item == 'coat':
            with bone('Spine'):
                cone('Coat', (0, .02, .72), .44 * bulk, .30 * bulk, .72, 'Cloth', 10)
                box('CoatCollar', (0, .06, 1.52), (.46 * bulk, .20, .14), 'Cloth')
        elif item == 'robe':
            with bone('Spine'):
                cone('Robe', (0, 0, .55), .50 * bulk, .28 * bulk, 1.0, 'Cloth', 12)
                cone('RobeHem', (0, 0, .07), .51 * bulk, .51 * bulk, .05, 'Trim', 12)
        elif item == 'skirt':
            with bone('Spine'):
                cone('Skirt', (0, 0, .78), .46 * bulk, .27 * bulk, .45, 'Cloth', 12)
                cone('SkirtHem', (0, 0, .57), .47 * bulk, .47 * bulk, .04, 'Trim', 12)
        elif item == 'kilt':
            with bone('Spine'):
                cone('Kilt', (0, 0, .82), .38 * bulk, .27 * bulk, .36, 'Cloth', 10)
                box('Belt', (0, 0, 1.00), (.56 * bulk, .56 * bulk, .07), 'Trim')
        elif item == 'short_cape':
            with bone('Spine'):
                box('Cape', (0, .24, 1.12), (.55, .06, .75), 'Cloth')
                box('Clasp', (0, -.25, 1.48), (.12, .03, .06), 'Trim', 0)
        elif item == 'scarf':
            with bone('Spine'):
                torus('Scarf', (0, 0, 1.52), .17 * bulk, .06, 'Accent' if 'Accent' in palette else 'Cloth')
                box('ScarfTail', (.10, .20, 1.30), (.10, .04, .40), 'Accent')
        elif item == 'belt':
            with bone('Spine'):
                box('Belt', (0, 0, 1.02), (.62 * bulk, .58 * bulk, .09), 'Trim')
                gem('Buckle', (0, -.30 * bulk, 1.02), (.08, .03, .07), 'Soul')
        elif item == 'belt_daggers':
            with bone('Spine'):
                box('Belt', (0, 0, 1.02), (.50 * bulk, .50 * bulk, .07), 'Trim')
                for s in [-1, 1]:
                    limb('Sheath', (s * .27, 0, 1.0), (s * .30, .05, .74), .03, 'Dark')
        elif item == 'crossbelt':
            with bone('Spine'):
                limb('Crossbelt', (-.22, -.30, 1.45), (.24, -.30, 1.02), .03, 'Trim')
        elif item == 'tabard':
            with bone('Spine'):
                box('Tabard', (0, -.28 * bulk, .92), (.30, .04, .55), 'Cloth')
                box('TabardSigil', (0, -.31 * bulk, 1.02), (.10, .02, .16), 'Trim', 0)
        elif item == 'big_pauldrons':
            for side, s in [('L', -1), ('R', 1)]:
                with bone('UpperArm.' + side):
                    gem('Pauldron', (s * .44, 0, 1.52), (.26, .26, .18), 'Armor')
                    cone('Spike', (s * .52, 0, 1.70), .07, 0, .22, 'Trim', 6)
        elif item == 'fur':
            with bone('Spine'):
                for i in range(7):
                    a = i * math.tau / 7
                    gem('Fur', (.30 * math.cos(a), .24 * math.sin(a), 1.50), (.16, .16, .12), 'Accent')
        elif item == 'orbit_crystals':
            with bone('Spine'):
                for a in [.5, 2.6, 4.6]:
                    gem('Crystal', (.62 * math.cos(a), .45 * math.sin(a), 1.40 + .12 * math.sin(a * 2)), (.06, .06, .13), 'Soul')
        elif item == 'void_rings':
            with bone('Spine'):
                torus('VoidRing', (0, 0, 1.05), .48, .025, 'Soul', (.25, 0, 0))
                torus('VoidRing2', (0, 0, 1.35), .44, .02, 'Trim', (-.2, .15, 0))
        else:
            raise RuntimeError(item)


def prop(kind, side):
    if kind is None:
        return
    h = HAND[side]
    with bone('Forearm.' + side):
        if kind == 'whip':
            limb('WhipHandle', h + Vector((0, 0, .08)), h + Vector((0, 0, -.18)), .04, 'Wood')
            prev = h + Vector((0, 0, -.18))
            for i in range(8):
                nxt = prev + Vector((.06, -.05, -.07 + .03 * math.sin(i)))
                limb('Lash', prev, nxt, .02, 'Dark'); prev = nxt
        elif kind == 'lantern':
            limb('LanternHandle', h, h + Vector((0, 0, -.12)), .015, 'Trim')
            box('LanternCage', h + Vector((0, 0, -.25)), (.14, .14, .18), 'Trim', .01)
            gem('LanternSoul', h + Vector((0, 0, -.25)), (.06, .06, .08), 'Soul')
        elif kind == 'lute':
            gem('LuteBody', h + Vector((-.08, -.12, .06)), (.16, .07, .20), 'Wood')
            limb('LuteNeck', h + Vector((-.08, -.12, .20)), h + Vector((-.14, -.12, .52)), .025, 'Trim')
            gem('LuteSoul', h + Vector((-.08, -.18, .06)), (.04, .02, .04), 'Soul')
        elif kind == 'knife':
            box('Grip', h + Vector((0, -.02, -.02)), (.05, .05, .12), 'Wood', 0)
            gem('Blade', h + Vector((0, -.08, -.16)), (.035, .02, .15), 'Trim')
        elif kind == 'axe':
            limb('Haft', h + Vector((0, -.02, -.25)), h + Vector((0, -.02, .95)), .04, 'Wood')
            gem('AxeHead', h + Vector((.14, -.02, .82)), (.24, .05, .20), 'Trim')
            gem('AxeBack', h + Vector((-.08, -.02, .84)), (.08, .05, .10), 'Armor')
        elif kind == 'flask':
            gem('Flask', h + Vector((0, -.06, .06)), (.09, .09, .11), 'Soul')
            cone('FlaskNeck', h + Vector((0, -.06, .19)), .03, .025, .08, 'Trim', 6)
        elif kind == 'cross':
            box('CrossStem', h + Vector((0, -.08, .05)), (.05, .05, .40), 'Trim', 0)
            box('CrossArm', h + Vector((0, -.08, .13)), (.26, .05, .05), 'Trim', 0)
            gem('CrossSoul', h + Vector((0, -.11, .13)), (.04, .02, .04), 'Soul')
        elif kind == 'book':
            box('Book', h + Vector((-.04, -.10, .05)), (.24, .16, .30), 'Trim', .01)
            box('Pages', h + Vector((-.04, -.10, .05)), (.20, .17, .26), 'Bone', 0)
            gem('BookSeal', h + Vector((-.04, -.19, .05)), (.05, .02, .05), 'Soul')
        elif kind == 'staff':
            limb('Staff', h + Vector((0, 0, -.75)), h + Vector((0, 0, 1.10)), .03, 'Wood')
            torus('StaffRing', h + Vector((0, 0, 1.12)), .08, .015, 'Trim', (math.pi / 2, 0, 0))
            gem('StaffGem', h + Vector((0, 0, 1.14)), (.07, .07, .11), 'Soul')
        elif kind == 'bone_club':
            limb('Club', h + Vector((0, -.02, -.10)), h + Vector((.05, -.02, .72)), .07, 'Bone', .10)
            for dx in [-.08, .08]:
                gem('Knuckle', h + Vector((.05 + dx, -.02, .74)), (.09, .08, .08), 'Bone')
        elif kind == 'wand':
            limb('Wand', h + Vector((0, -.02, -.02)), h + Vector((0, -.05, .42)), .018, 'Trim')
            gem('Star', h + Vector((0, -.05, .47)), (.07, .03, .07), 'Soul')
        elif kind == 'rune':
            gem('Rune', h + Vector((0, -.08, .30)), (.08, .08, .15), 'Soul')
            torus('RuneRing', h + Vector((0, -.08, .30)), .14, .012, 'Trim', (math.pi / 2, 0, 0))
        elif kind == 'sword':
            box('SwordGrip', h + Vector((0, -.02, 0)), (.05, .05, .16), 'Wood', 0)
            box('SwordGuard', h + Vector((0, -.02, .10)), (.24, .05, .04), 'Trim', 0)
            gem('SwordBlade', h + Vector((0, -.02, .52)), (.05, .025, .40), 'Armor')
        elif kind == 'shield':
            box('Shield', h + Vector((-.08, -.10, .15)), (.08, .46, .62), 'Armor', .03)
            box('ShieldTrim', h + Vector((-.13, -.10, .15)), (.02, .40, .10), 'Trim', 0)
            gem('ShieldBoss', h + Vector((-.14, -.10, .20)), (.03, .08, .08), 'Soul')
        else:
            raise RuntimeError(kind)


BONES = [('Root', (0, 0, 0), (0, 0, .5), None), ('Spine', (0, 0, 1), (0, 0, 1.5), 'Root'), ('Head', (0, 0, 1.5), (0, 0, 1.98), 'Spine')]
for _side, _s in [('L', -1), ('R', 1)]:
    BONES += [('Thigh.' + _side, (_s * .18, 0, 1), (_s * .18, 0, .58), 'Root'),
              ('Shin.' + _side, (_s * .18, 0, .58), (_s * .18, 0, .15), 'Thigh.' + _side),
              ('UpperArm.' + _side, (_s * .4, 0, 1.42), (_s * .5, 0, 1.12), 'Spine'),
              ('Forearm.' + _side, (_s * .5, 0, 1.12), (_s * .5, -.1, .83), 'UpperArm.' + _side)]


def rig():
    bpy.ops.object.select_all(action='DESELECT')
    arm = bpy.data.armatures.new('GenericRig')
    obj = bpy.data.objects.new('Rig', arm)
    bpy.context.collection.objects.link(obj)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode='EDIT')
    for name, head_pos, tail, parent in BONES:
        b = arm.edit_bones.new(name); b.head = head_pos; b.tail = tail
        if parent:
            b.parent = arm.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT')
    for p in parts:
        group = p.vertex_groups.new(name=assignments[p.name])
        group.add(list(range(len(p.data.vertices))), 1, 'REPLACE')
    return obj


def clips(rig_obj):
    # Identical to build_campaign_pack.clips so SH10_Kael.controller fits every hero.
    rig_obj.animation_data_create()
    for name in ['Idle', 'Run', 'Attack', 'Death']:
        action = bpy.data.actions.new(name); rig_obj.animation_data.action = action
        for frame in [1, 7, 13, 19, 25]:
            t = (frame - 1) / 24; phase = math.sin(t * math.tau)
            for b in rig_obj.pose.bones:
                b.rotation_mode = 'XYZ'; b.rotation_euler = (0, 0, 0); b.location = (0, 0, 0)
            if name == 'Idle':
                rig_obj.pose.bones['Spine'].rotation_euler.x = .025 * phase
            elif name == 'Run':
                for side, s in [('L', 1), ('R', -1)]:
                    rig_obj.pose.bones['Thigh.' + side].rotation_euler.x = s * .55 * phase
                    rig_obj.pose.bones['Shin.' + side].rotation_euler.x = max(0, -s * phase) * .7
                    rig_obj.pose.bones['UpperArm.' + side].rotation_euler.x = -s * .45 * phase
            elif name == 'Attack':
                rig_obj.pose.bones['UpperArm.R'].rotation_euler.x = -1.7 * math.sin(t * math.pi)
                rig_obj.pose.bones['Spine'].rotation_euler.z = .22 * math.sin(t * math.pi)
            else:
                rig_obj.pose.bones['Root'].rotation_euler.x = -math.pi / 2 * t
            for b in rig_obj.pose.bones:
                b.keyframe_insert(data_path='rotation_euler', frame=frame, group=b.name)
        action.use_fake_user = True
    rig_obj.animation_data.action = bpy.data.actions.get('Idle'); bpy.context.scene.frame_set(1)


def preview(name):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'MATERIAL'
    scene.render.film_transparent = True
    scene.render.resolution_x = scene.render.resolution_y = 384
    cam_data = bpy.data.cameras.new('PreviewCam'); cam_data.type = 'ORTHO'; cam_data.ortho_scale = 2.9
    cam = bpy.data.objects.new('PreviewCam', cam_data); bpy.context.collection.objects.link(cam)
    cam.location = (1.2, -6, 1.35); cam.rotation_euler = (math.radians(90), 0, math.radians(11))
    scene.camera = cam
    scene.render.filepath = str(PREVIEWS / f'{name}.png')
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam); bpy.data.cameras.remove(cam_data)


def build(name, spec):
    global materials, parts, assignments
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    for a in list(bpy.data.actions): bpy.data.actions.remove(a)
    materials, parts, assignments = {}, [], {}
    palette.clear(); palette.update(BASE); palette.update(spec['pal'])
    bulk = spec['bulk']
    body(bulk); head(spec['head']); extras(spec['body'], bulk)
    prop(spec['right'], 'R'); prop(spec['left'], 'L')
    rig_obj = rig()
    bpy.ops.object.select_all(action='DESELECT')
    for p in parts: p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]; bpy.ops.object.join(); mesh = bpy.context.object
    asset = f'SH10_Hero_{name}'
    mesh.name = asset
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.uv.smart_project(); bpy.ops.object.mode_set(mode='OBJECT')
    for key, m in materials.items(): m.name = asset + '_' + key
    mod = mesh.modifiers.new('Rig', 'ARMATURE'); mod.object = rig_obj; mesh.parent = rig_obj
    preview(name)
    clips(rig_obj); rig_obj.select_set(True); mesh.select_set(True)
    scene = bpy.context.scene
    scene.frame_start = 1; scene.frame_end = 25; scene.render.fps = 24; scene.unit_settings.system = 'METRIC'
    source = OUT / 'Blender' / f'{asset}.blend'; source.parent.mkdir(parents=True, exist_ok=True)
    model = MODELS / f'{asset}.fbx'
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    bpy.ops.export_scene.fbx(filepath=str(model), use_selection=True, object_types={'MESH', 'ARMATURE'},
                             axis_forward='-Z', axis_up='Y', apply_unit_scale=True, add_leaf_bones=False,
                             bake_anim=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True)
    triangles = sum(len(p.vertices) - 2 for p in mesh.data.polygons)
    print('HERO_DONE', asset, triangles, flush=True)
    return dict(name=asset, character=name, model=str(model.relative_to(ROOT)).replace('\\', '/'),
                source=str(source.relative_to(ROOT)).replace('\\', '/'), triangles=triangles,
                materials={asset + '_' + k: list(palette[k]) for k in materials})


if __name__ == '__main__':
    import sys
    only = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    names = only or list(HEROES)
    for n in names:
        if (MODELS / f'SH10_Hero_{n}.fbx').exists() and not only:
            raise RuntimeError(f'Refusing to overwrite SH10_Hero_{n}.fbx')
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    manifest_path = OUT / 'manifest.json'
    manifest = {m['name']: m for m in json.loads(manifest_path.read_text(encoding='utf-8'))} if manifest_path.exists() else {}
    for n in names:
        entry = build(n, HEROES[n])
        manifest[entry['name']] = entry
    manifest_path.write_text(json.dumps(list(manifest.values()), indent=2), encoding='utf-8')
    print('HEROES_COMPLETE', len(names), flush=True)
