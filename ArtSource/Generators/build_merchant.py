"""Soul Hunter NPC: the Wandering Merchant of Level 3, The Forgotten Village (concept, 2026-09-26).

A hooded, stooped travelling trader who survived the demons' burning of the village: a patched ash and charcoal
cloak with faded burgundy and smouldering ember edges, a deep hood with two soul-cyan eyes, an overloaded pack
frame (bundle, bedroll, a small soul-locked chest, scrolls, two ember lanterns, hanging trinkets), a crooked staff
with a hanging soul lantern, and a coin pouch on the belt. Same faceted low-poly style and scale as the hero
models (build_heroes.py): feet at z = 0, facing -Y, static idle pose, no rig.

Concept renders (default; nothing is written under Assets/):
    blender --background --python ArtSource/Generators/build_merchant.py
Export after owner approval (writes Assets/Art/Models/SH10_NPC_WanderingMerchant.fbx and
ArtSource/SH10_Merchant_v01/materials.json):
    blender --background --python ArtSource/Generators/build_merchant.py -- --export
SOULHUNTER_ROOT points the script at an isolated copy of the project.
"""
import json
import math
import os
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Quaternion, Vector

ROOT = Path(os.environ.get('SOULHUNTER_ROOT') or Path(__file__).resolve().parent.parent.parent)
OUT = ROOT / 'ArtSource' / 'SH10_Merchant_v01'
PREVIEWS = OUT / 'Previews'
MODELS = ROOT / 'Assets' / 'Art' / 'Models'
KAEL_BLEND = ROOT / 'ArtSource' / 'SH10_Heroes_v01' / 'Blender' / 'SH10_Hero_Kael.blend'
ASSET = 'SH10_NPC_WanderingMerchant'

# key: base colour, metallic, roughness, emission colour, emission strength
PALETTE = {
    'Ash': ((.13, .12, .11), 0, .9, None, 0),
    'AshLight': ((.24, .22, .20), 0, .9, None, 0),
    'Charcoal': ((.07, .068, .075), 0, .85, None, 0),
    'Burgundy': ((.26, .05, .07), 0, .85, None, 0),
    'Ember': ((.85, .30, .06), 0, .6, (1.0, .38, .08), 4.0),
    'Soul': ((.10, .85, .80), 0, .3, (.15, .95, .90), 8.0),
    'Dark': ((.012, .012, .016), 0, 1.0, None, 0),
    'Wood': ((.16, .095, .065), 0, .8, None, 0),
    'Leather': ((.24, .13, .07), 0, .75, None, 0),
    'Brass': ((.55, .38, .14), .8, .4, None, 0),
    'Gold': ((1.0, .72, .25), 1.0, .25, None, 0),
    'Iron': ((.10, .10, .12), .8, .45, None, 0),
    'Parchment': ((.72, .64, .46), 0, .8, None, 0),
    'Bone': ((.58, .57, .40), 0, .7, None, 0),
}

materials = {}
parts = []
FRAME = [Matrix.Identity(4)]

STOOP = math.radians(22)          # forward lean of the upper body and pack
PIVOT = Vector((0, 0, .95))       # hip pivot of the lean


# ---------------------------------------------------------------- building blocks

def material(key):
    if key not in materials:
        color, metallic, rough, emission, strength = PALETTE[key]
        m = bpy.data.materials.new(f'{ASSET}_{key}')
        m.diffuse_color = (*color, 1)
        m.use_nodes = True
        p = m.node_tree.nodes['Principled BSDF']
        p.inputs['Base Color'].default_value = (*color, 1)
        p.inputs['Metallic'].default_value = metallic
        p.inputs['Roughness'].default_value = rough
        if emission:
            p.inputs['Emission Color'].default_value = (*emission, 1)
            p.inputs['Emission Strength'].default_value = strength
        materials[key] = m
    return materials[key]


class frame:
    """with frame(M): parts created inside are transformed by M (on top of the enclosing frame)."""
    def __init__(self, m): self.m = m
    def __enter__(self): FRAME.append(FRAME[-1] @ self.m)
    def __exit__(self, *a): FRAME.pop()


def world(p):
    """A point given in the current frame, in world space (for things that hang straight down)."""
    return FRAME[-1] @ Vector(p)


def _quat(rot):
    if isinstance(rot, Quaternion):
        return rot
    return Euler(rot).to_quaternion()


def make(prim, name, mat, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1), bevel=0.0, in_frame=True, **kw):
    getattr(bpy.ops.mesh, f'primitive_{prim}_add')(**kw)
    obj = bpy.context.object
    obj.name = name
    m = Matrix.LocRotScale(Vector(loc), _quat(rot), Vector(scale))
    if in_frame:
        m = FRAME[-1] @ m
    obj.data.transform(m)
    obj.data.materials.append(material(mat))
    if bevel:
        mod = obj.modifiers.new('Crafted_edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(obj)
    return obj


def box(name, loc, size, mat, bevel=0.0, rot=(0, 0, 0), **kw):
    return make('cube', name, mat, loc, rot, size, bevel, size=1, **kw)


def cone(name, loc, r1, r2, depth, mat, verts=8, rot=(0, 0, 0), **kw):
    return make('cone', name, mat, loc, rot, vertices=verts, radius1=r1, radius2=r2, depth=depth, **kw)


def cyl(name, loc, r, depth, mat, verts=8, rot=(0, 0, 0), **kw):
    return make('cylinder', name, mat, loc, rot, vertices=verts, radius=r, depth=depth, **kw)


def gem(name, loc, scale, mat, rot=(0, 0, 0), **kw):
    return make('ico_sphere', name, mat, loc, rot, scale, subdivisions=1, radius=1, **kw)


def limb(name, a, b, r1, mat, r2=None, verts=6, **kw):
    a, b = Vector(a), Vector(b)
    d = b - a
    return cone(name, (a + b) / 2, r1, r1 if r2 is None else r2, d.length, mat, verts, d.to_track_quat('Z', 'Y'), **kw)


def torus(name, loc, major, minor, mat, rot=(0, 0, 0), seg=12, ring=4, **kw):
    return make('torus', name, mat, loc, rot, major_radius=major, minor_radius=minor,
                major_segments=seg, minor_segments=ring, **kw)


def tatter(obj, amount, seed=0):
    """Ragged hem: alternate the bottom ring of a cone up and down."""
    zs = [v.co.z for v in obj.data.vertices]
    low = min(zs)
    ring = [v for v in obj.data.vertices if v.co.z < low + 1e-4]
    ring.sort(key=lambda v: math.atan2(v.co.y, v.co.x))
    for i, v in enumerate(ring):
        v.co.z += amount * ((i + seed) % 2) + amount * .35 * ((i * 7 + seed) % 3 == 0)
    return [v.co.copy() for v in ring]


def stoop_matrix():
    return Matrix.Translation(PIVOT) @ Matrix.Rotation(STOOP, 4, 'X') @ Matrix.Translation(-PIVOT)


# ---------------------------------------------------------------- the merchant

def lantern(name, top, glow, size=1.0, in_frame=False):
    """A lantern hanging straight down from `top` (world space): chain, roof, cage, glowing core, base."""
    s = size
    t = Vector(top)
    limb(name + 'Chain', t, t + Vector((0, 0, -.09 * s)), .012, 'Iron', in_frame=in_frame)
    c = t + Vector((0, 0, -.22 * s))
    cone(name + 'Roof', c + Vector((0, 0, .11 * s)), .11 * s, .02 * s, .07 * s, 'Brass', 6, in_frame=in_frame)
    gem(name + 'Core', c, (.075 * s, .075 * s, .10 * s), glow, in_frame=in_frame)
    for k in range(4):
        a = k * math.pi / 2 + math.pi / 4
        o = Vector((math.cos(a) * .085 * s, math.sin(a) * .085 * s, 0))
        limb(name + 'Bar', c + o + Vector((0, 0, .08 * s)), c + o + Vector((0, 0, -.08 * s)), .012 * s, 'Iron', verts=4, in_frame=in_frame)
    cone(name + 'Base', c + Vector((0, 0, -.10 * s)), .10 * s, .10 * s, .035 * s, 'Brass', 6, in_frame=in_frame)
    return c


def legs_and_robe():
    for s in (-1, 1):
        box('Boot', (s * .15, -.30, .08), (.17, .30, .16), 'Leather', .02)
        box('BootWrap', (s * .15, -.26, .17), (.18, .20, .05), 'Charcoal')
    # Charcoal under-robe, then the patched ash robe; both with ragged, smouldering hems.
    under = cone('UnderRobe', (0, 0, .60), .47, .30, .92, 'Charcoal', 12)
    tatter(under, .06, 1)
    robe = cone('Robe', (0, .02, .66), .50, .33, .82, 'Ash', 10)
    hem = tatter(robe, .08)
    for i, p in enumerate(hem):
        if i % 2 == 0 and p.y > -.2 or i % 4 == 0:   # embers still glowing on the burnt hem
            gem('Ember', p + Vector((0, 0, .015)), (.035, .035, .03), 'Ember')
    # Patches sewn over burn holes (on the cone surface, tilted with its slope).
    slope = math.atan(.17 / .82)
    for ang, z, w, h, key, roll in [(-1.95, .52, .19, .17, 'Burgundy', .25), (-.9, .80, .15, .13, 'AshLight', -.3),
                                    (.5, .45, .17, .19, 'Burgundy', -.2), (2.2, .70, .20, .16, 'AshLight', .35),
                                    (1.4, .38, .15, .15, 'Burgundy', .15)]:
        r = .50 - .17 * (z - .25) / .82 + .012
        loc = (math.cos(ang) * r, .02 + math.sin(ang) * r, z)
        rot = (Matrix.Rotation(ang + math.pi / 2, 4, 'Z') @ Matrix.Rotation(-slope, 4, 'X')
               @ Matrix.Rotation(roll, 4, 'Y')).to_quaternion()
        box('Patch', loc, (w, .02, h), key, rot=rot)


def upper_body():
    """Built upright, then leaned forward by the stoop frame."""
    cone('Torso', (0, 0, 1.24), .26, .33, .56, 'Charcoal', 8)
    torus('Belt', (0, 0, 1.03), .30, .035, 'Leather')
    box('Buckle', (0, -.33, 1.03), (.09, .03, .07), 'Brass')
    # Mantle (faded burgundy, ragged) and ash neck wrap.
    mantle = cone('Mantle', (0, .02, 1.40), .48, .22, .32, 'Burgundy', 8)
    tatter(mantle, .05, 1)
    box('MantlePatch', (.26, -.28, 1.38), (.13, .03, .11), 'AshLight', rot=(.9, 0, .55))
    torus('NeckWrap', (0, 0, 1.54), .19, .06, 'Ash', seg=10)
    # Deep hood: a round hood, a peak that droops back, a dark face with two soul eyes, a heavy brow.
    gem('Hood', (0, .03, 1.75), (.28, .30, .29), 'Charcoal')
    cone('HoodTail', (0, .30, 1.74), .15, .0, .40, 'Charcoal', 6, rot=(-1.95, 0, 0))
    gem('Face', (0, -.215, 1.69), (.16, .09, .17), 'Dark')
    torus('HoodRim', (0, -.25, 1.69), .155, .045, 'Burgundy', rot=(math.pi / 2, 0, 0), seg=8)
    gem('Brow', (0, -.24, 1.86), (.21, .10, .06), 'Charcoal', rot=(.35, 0, 0))
    for x in (-.055, .055):
        box('Eye', (x, -.285, 1.715), (.045, .02, .024), 'Soul')
    gem('FaceWrap', (0, -.265, 1.585), (.15, .07, .06), 'Ash')
    # Pack straps over the shoulders and down the chest.
    for s in (-1, 1):
        box('Strap', (s * .19, -.315, 1.18), (.07, .025, .30), 'Leather', rot=(-.13, 0, 0))
    # Right arm reaches forward to the staff; left hand grips the pack strap.
    limb('UpperArmR', (.36, 0, 1.40), (.46, -.14, 1.16), .09, 'Ash')
    limb('ForearmR', (.46, -.14, 1.16), (.44, -.38, 1.12), .085, 'Ash', .115)
    cone('CuffR', (.44, -.39, 1.12), .12, .12, .05, 'Burgundy', 8, rot=(math.pi / 2, 0, 0))
    gem('HandR', (.44, -.46, 1.12), (.07, .07, .075), 'Leather')
    limb('UpperArmL', (-.36, 0, 1.40), (-.44, -.14, 1.18), .09, 'Ash')
    limb('ForearmL', (-.44, -.14, 1.18), (-.26, -.34, 1.26), .085, 'Ash', .11)
    cone('CuffL', (-.27, -.33, 1.26), .115, .115, .05, 'Burgundy', 8, rot=Vector((.9, -.9, .4)).to_track_quat('Z', 'Y'))
    gem('HandL', (-.21, -.37, 1.30), (.07, .06, .075), 'Leather')
    # Coin pouch on the belt, coins sewn on and spilling from it.
    box('Pouch', (-.19, -.32, .93), (.18, .11, .17), 'Leather', .02)
    box('PouchFlap', (-.19, -.37, .99), (.19, .03, .08), 'Burgundy')
    for i, x in enumerate((-.245, -.19, -.135)):
        cyl('Coin', (x, -.385, .895), .038, .016, 'Gold', 10, rot=(math.pi / 2, 0, 0))
    gem('PouchClasp', (-.19, -.39, .985), (.03, .015, .025), 'Brass')
    box('SidePouch', (.24, -.20, .94), (.10, .14, .14), 'Leather', .015, rot=(0, 0, -.6))


def pack():
    """The overloaded pack frame on the back; leans with the upper body."""
    for s in (-1, 1):
        limb('Pole', (s * .30, .36, .74), (s * .30, .36, 1.90), .035, 'Wood', verts=6)
        limb('HookArm', (s * .30, .36, 1.82), (s * .50, .36, 1.88), .025, 'Wood', verts=4)
    for z in (.82, 1.58, 1.86):
        limb('CrossBar', (-.33, .36, z), (.33, .36, z), .03, 'Wood', verts=6)
    # Main bundle: charcoal cloth, patched, roped.
    box('Bundle', (0, .60, 1.26), (.62, .40, .70), 'Charcoal', .04)
    box('BundlePatch', (-.12, .81, 1.40), (.22, .02, .20), 'Burgundy', rot=(0, .15, 0))
    box('BundlePatch', (.18, .81, 1.12), (.16, .02, .16), 'AshLight', rot=(0, -.2, 0))
    for z in (1.06, 1.46):
        box('Rope', (0, .60, z), (.65, .43, .035), 'Leather')
    # Rolled blanket, then the small soul-locked chest on top.
    cyl('Bedroll', (0, .56, 1.68), .17, .80, 'Burgundy', 8, rot=(0, math.pi / 2, 0))
    for x in (-.25, .25):
        cyl('BedrollTie', (x, .56, 1.68), .18, .04, 'Leather', 8, rot=(0, math.pi / 2, 0))
    box('Chest', (0, .54, 1.91), (.38, .26, .18), 'Wood', .02)
    cyl('ChestLid', (0, .54, 2.00), .13, .38, 'Wood', 8, rot=(0, math.pi / 2, 0), scale=(1, 1, .6))
    for x in (-.13, .13):
        box('ChestBand', (x, .54, 1.94), (.04, .28, .24), 'Brass')
    box('ChestLock', (0, .405, 1.93), (.07, .03, .08), 'Gold')
    gem('ChestKeyhole', (0, .39, 1.93), (.022, .012, .022), 'Soul')
    # Scrolls poking out on the left, a map on the right.
    for i, (y, z, a) in enumerate([(.52, 1.46, .55), (.66, 1.36, .75), (.60, 1.54, .35)]):
        c = Vector((-.40, y, z))
        rot = Euler((0, math.pi / 2 - a, .2 * i))
        cyl('Scroll', c, .045, .46, 'Parchment', 6, rot=rot)
        cyl('ScrollTie', c, .05, .04, 'Burgundy', 6, rot=rot)
    cyl('Map', (.40, .56, 1.30), .05, .42, 'Parchment', 6, rot=(0, math.pi / 2 + .5, 0))
    # Pan and a bone charm tied on the side of the bundle.
    cyl('Pan', (.34, .72, 1.00), .13, .03, 'Iron', 10, rot=(0, math.pi / 2, 0))
    limb('PanHandle', (.34, .72, 1.12), (.34, .72, 1.34), .018, 'Wood', verts=4)


def pack_hangings():
    """Things hanging straight down from the (leaning) pack: two ember lanterns and trinkets."""
    with frame(stoop_matrix()):
        hooks = [world((s * .49, .36, 1.87)) for s in (-1, 1)]
        bar = [world((x, .36, .82)) for x in (-.30, -.18, .18, .30)]
    for i, h in enumerate(hooks):
        lantern(f'PackLantern{i}', h, 'Ember', .85)
    # Trinkets on strings from the bottom crossbar: bells, a bone charm, a soul crystal, a coin string.
    for i, top in enumerate(bar):
        length = (.14, .22, .18, .12)[i]
        end = top + Vector((0, 0, -length))
        limb('String', top, end, .008, 'Leather', verts=3, in_frame=False)
        if i == 0:
            cone('Bell', end + Vector((0, 0, -.04)), .05, .015, .08, 'Brass', 6, in_frame=False)
        elif i == 1:
            limb('BoneCharm', end, end + Vector((0, 0, -.14)), .022, 'Bone', verts=4, in_frame=False)
            gem('BoneKnob', end + Vector((0, 0, -.15)), (.035, .035, .03), 'Bone', in_frame=False)
        elif i == 2:
            gem('SoulCharm', end + Vector((0, 0, -.05)), (.035, .035, .06), 'Soul', in_frame=False)
        else:
            for k in range(3):
                cyl('HangingCoin', end + Vector((0, 0, -.03 - .05 * k)), .035, .012, 'Gold', 8,
                    rot=(math.pi / 2, 0, .5 * k), in_frame=False)


def staff(hand):
    """Crooked staff planted through the right hand, a soul lantern hanging from its crook."""
    foot = Vector((hand.x + .05, hand.y - .04, 0))
    top = Vector((hand.x, hand.y + .01, 1.92))
    limb('Staff', foot, top, .035, 'Wood', .03, in_frame=False)
    cone('StaffFerrule', foot + Vector((0, 0, .05)), .04, .045, .10, 'Iron', 6, in_frame=False)
    crook = [top, top + Vector((0, -.10, .12)), top + Vector((0, -.26, .12)), top + Vector((0, -.34, .02))]
    for i in range(3):
        limb('Crook', crook[i], crook[i + 1], .03, 'Wood', verts=6, in_frame=False)
    torus('StaffWrap', hand + Vector((0, 0, .16)), .045, .014, 'Burgundy', in_frame=False)
    torus('StaffWrap', hand + Vector((0, 0, .22)), .045, .014, 'Burgundy', in_frame=False)
    gem('CrookGem', top + Vector((0, -.05, .07)), (.04, .04, .05), 'Soul', in_frame=False)
    return lantern('SoulLantern', crook[-1] + Vector((0, 0, -.02)), 'Soul', 1.15)


def build():
    """Builds every part at the origin; returns the world positions of the lantern cores (for preview lights)."""
    legs_and_robe()
    with frame(stoop_matrix()):
        upper_body()
        pack()
        hand = world((.44, -.46, 1.12))
    pack_hangings()
    soul = staff(hand)
    hooks = [o for o in parts if o.name.startswith('PackLantern') and 'Core' in o.name]
    embers = [sum((v.co for v in o.data.vertices), Vector()) / len(o.data.vertices) for o in hooks]
    return soul, embers


def join():
    bpy.ops.object.select_all(action='DESELECT')
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = ASSET
    obj.data.name = ASSET
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    for poly in obj.data.polygons:
        poly.use_smooth = False
    return obj


def triangles(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


# ---------------------------------------------------------------- preview scene

def setup_scene():
    s = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
    s.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines else 'BLENDER_EEVEE'
    s.view_settings.view_transform = 'Standard'
    w = bpy.data.worlds.new('World')
    w.use_nodes = True
    w.node_tree.nodes['Background'].inputs[0].default_value = (0.13, 0.14, 0.17, 1)
    w.node_tree.nodes['Background'].inputs[1].default_value = 1.0
    s.world = w
    try:
        setup_glow(s)
    except Exception as e:
        print('GLOW_SKIPPED', e, flush=True)
    key = bpy.data.objects.new('Key', bpy.data.lights.new('Key', 'SUN'))
    key.data.energy = 3.2
    key.rotation_euler = (math.radians(50), 0, math.radians(-30))
    s.collection.objects.link(key)
    rim = bpy.data.objects.new('Rim', bpy.data.lights.new('Rim', 'AREA'))
    rim.data.energy = 350
    rim.data.size = 6
    rim.data.color = (1.0, .75, .55)   # warm back light: the village still burns behind him
    rim.location = (0, 6, 4)
    rim.rotation_euler = (math.radians(-60), 0, 0)
    s.collection.objects.link(rim)
    return s


def setup_glow(s):
    if hasattr(s, 'node_tree'):
        s.use_nodes = True
        tree = s.node_tree
        for node in list(tree.nodes):
            tree.nodes.remove(node)
        out_socket = tree.nodes.new('CompositorNodeComposite').inputs['Image']
    else:  # Blender 5: the compositor is a node group
        tree = bpy.data.node_groups.new('Compositor', 'CompositorNodeTree')
        s.compositing_node_group = tree
        tree.interface.new_socket('Image', in_out='OUTPUT', socket_type='NodeSocketColor')
        out_socket = tree.nodes.new('NodeGroupOutput').inputs[0]
    rl = tree.nodes.new('CompositorNodeRLayers')
    glare = tree.nodes.new('CompositorNodeGlare')
    if hasattr(glare, 'glare_type'):
        glare.glare_type = 'FOG_GLOW'
    elif 'Type' in glare.inputs:
        try: glare.inputs['Type'].default_value = 'Fog Glow'
        except Exception: pass
    for k, v in (('threshold', 1.5), ('size', 6)):
        if hasattr(glare, k): setattr(glare, k, v)
        elif k.capitalize() in glare.inputs: glare.inputs[k.capitalize()].default_value = v
    tree.links.new(rl.outputs['Image'], glare.inputs['Image'])
    tree.links.new(glare.outputs['Image'], out_socket)


def point_light(name, loc, color, energy, radius=.05):
    light = bpy.data.lights.new(name, 'POINT')
    light.energy = energy
    light.color = color
    light.shadow_soft_size = radius
    o = bpy.data.objects.new(name, light)
    o.location = loc
    bpy.context.scene.collection.objects.link(o)
    return o


def ground(color, size=40):
    bpy.ops.mesh.primitive_plane_add(size=size)
    g = bpy.context.object
    g.name = 'Ground'
    m = bpy.data.materials.new('PreviewGround')
    m.use_nodes = True
    m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (*color, 1)
    m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .95
    g.data.materials.append(m)
    return g


def load_kael():
    with bpy.data.libraries.load(str(KAEL_BLEND), link=False) as (src, dst):
        dst.objects = [n for n in src.objects if n == 'SH10_Hero_Kael']
    kael = dst.objects[0]
    bpy.context.scene.collection.objects.link(kael)
    kael.parent = None
    for mod in list(kael.modifiers):
        kael.modifiers.remove(mod)
    kael.matrix_world = Matrix.Identity(4)
    return kael


def label(text, loc, size=.12):
    curve = bpy.data.curves.new(text, 'FONT')
    curve.body = text
    curve.size = size
    curve.align_x = 'CENTER'
    o = bpy.data.objects.new('Label_' + text, curve)
    o.location = loc
    o.rotation_euler = (math.radians(90), 0, 0)
    m = bpy.data.materials.new('Label')
    m.use_nodes = True
    m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.85, .85, .85, 1)
    o.data.materials.append(m)
    bpy.context.scene.collection.objects.link(o)
    return o


def persp_camera(name, target, distance, azimuth_deg, elevation_deg, lens=50):
    a, e = math.radians(azimuth_deg), math.radians(elevation_deg)
    cam = bpy.data.objects.new(name, bpy.data.cameras.new(name))
    cam.data.lens = lens
    cam.location = Vector(target) + distance * Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.collection.objects.link(cam)
    return cam


def ortho_camera(name, target, pitch_deg, scale, azimuth_deg=0):
    cam = bpy.data.objects.new(name, bpy.data.cameras.new(name))
    cam.data.type = 'ORTHO'
    cam.data.ortho_scale = scale
    cam.rotation_euler = (math.radians(90 - pitch_deg), 0, math.radians(azimuth_deg))
    forward = cam.rotation_euler.to_matrix() @ Vector((0, 0, -1))
    cam.location = Vector(target) - forward * 30
    cam.data.clip_end = 100
    bpy.context.scene.collection.objects.link(cam)
    return cam


def render(cam, name, res=(1600, 900)):
    s = bpy.context.scene
    s.camera = cam
    s.render.resolution_x, s.render.resolution_y = res
    s.render.filepath = str(PREVIEWS / f'{name}.png')
    bpy.ops.render.render(write_still=True)
    print('RENDERED', name, flush=True)


def previews(merchant, soul, embers):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    s = setup_scene()
    g = ground((.07, .075, .07))
    point_light('SoulGlow', soul, (.3, .95, 1.0), 25)
    for i, e in enumerate(embers):
        point_light(f'EmberGlow{i}', e, (1.0, .5, .15), 8)
    height = max((merchant.matrix_world @ v.co).z for v in merchant.data.vertices)
    target = (0, -.1, 1.15)
    render(persp_camera('Front34', target, 6.2, -35, 12), 'merchant_front34')
    render(persp_camera('Back34', target, 6.2, 180 + 35, 14), 'merchant_back34')
    render(persp_camera('Side', target, 6.2, -90, 8), 'merchant_side')

    kael = load_kael()
    kael.location = (-1.25, 0, 0)
    kael_h = max((kael.matrix_world @ v.co).z for v in kael.data.vertices)
    label(f'Kael  {kael_h:.2f}', (-1.25, -.6, -.02 + 0.0), .11).location.z = 0.02
    label(f'Merchant  {height:.2f} ({height / kael_h:.2f}x)', (.2, -.9, .02), .11)
    for h, x in ((kael_h, -1.25 - .75), (height, .2 + .85)):
        box('HeightMark', (x, 0, h), (.30, .01, .015), 'Dark', in_frame=False)
        parts.pop()
    cam = ortho_camera('Scale', (-.55, 0, 1.2), 6, 4.6, -12)
    render(cam, 'merchant_scale_vs_kael', (1400, 1000))

    # Game camera: orthographic, 55-degree pitch, Unity orthographic size 12 (24 units tall at 1080p).
    # 960x540 is a crop at the same pixels per unit as the game at 1920x1080.
    g.data.materials[0].node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.035, .03, .028, 1)
    kael.location = (-2.2, -.6, 0)
    for o in bpy.data.objects:
        if o.name.startswith('Label_') or o.name.startswith('HeightMark'):
            o.hide_render = True
    s.world.node_tree.nodes['Background'].inputs[0].default_value = (0.05, 0.05, 0.06, 1)
    bpy.data.objects['Rim'].data.energy = 60   # no hot spot on the ground from the back light
    render(ortho_camera('Game', (-1.0, -.3, .8), 55, 12 * 16 / 9), 'merchant_game_camera', (960, 540))
    render(ortho_camera('GameZoom', (-1.0, .1, 1.1), 55, 4.6), 'merchant_game_camera_zoom', (960, 540))
    return height


# ---------------------------------------------------------------- export (after owner approval)

def export(merchant):
    MODELS.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    merchant.select_set(True)
    bpy.context.view_layer.objects.active = merchant
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.uv.smart_project(); bpy.ops.object.mode_set(mode='OBJECT')
    path = MODELS / f'{ASSET}.fbx'
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={'MESH'}, use_mesh_modifiers=True,
                             axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL')
    info = {}
    for key, m in materials.items():
        color, metallic, rough, emission, strength = PALETTE[key]
        info[m.name] = dict(color=list(color), metallic=metallic, roughness=rough,
                            emission=list(emission or (0, 0, 0)), strength=strength)
    (OUT / 'materials.json').write_text(json.dumps(info, indent=2), encoding='utf-8')
    print('EXPORTED', path, flush=True)


if __name__ == '__main__':
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    bpy.ops.wm.read_factory_settings(use_empty=True)
    soul, embers = build()
    merchant = join()
    tris = triangles(merchant)
    print('MERCHANT_TRIANGLES', tris, flush=True)
    if '--export' in args:
        export(merchant)
    else:
        h = previews(merchant, soul, embers)
        print('MERCHANT_HEIGHT', round(h, 3), flush=True)
        OUT.mkdir(parents=True, exist_ok=True)
        bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'merchant_v01.blend'))
    print('DONE', flush=True)
