"""Additive art review kit. Run with Blender --background --python.
Never edits existing project assets. Static art only, not gameplay-ready actors.
"""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / 'ArtSource' / 'L01_Review_v01'
EXPORT = ROOT / 'Assets' / 'Art' / 'Models'
NAMES = ['Gravestone', 'CrossGrave', 'Coffin', 'Crypt', 'Gate', 'Fence',
         'DeadTree', 'RockCluster', 'SoulLantern', 'SoulWand', 'XPGem',
         'TreasureChest', 'KaelStudy', 'GraveGhoulStudy']
for name in NAMES:
    for dest in (SOURCE / (name + '.blend'), EXPORT / ('L01R_' + name + '.fbx')):
        if dest.exists():
            raise RuntimeError('Refusing to overwrite: ' + str(dest))
SOURCE.mkdir(parents=True, exist_ok=True)
EXPORT.mkdir(parents=True, exist_ok=True)

PALETTE = {
    'Slate': (0.20, 0.25, 0.30), 'Edge': (0.34, 0.40, 0.44),
    'Dark': (0.055, 0.075, 0.095), 'Iron': (0.13, 0.18, 0.23),
    'Bronze': (0.48, 0.28, 0.10), 'Wood': (0.16, 0.095, 0.065),
    'Soul': (0.10, 0.85, 0.80), 'Cloth': (0.26, 0.035, 0.055),
    'Bone': (0.58, 0.57, 0.40), 'Moss': (0.16, 0.23, 0.12)
}
materials = {}
parts = []

def material(key):
    if key not in materials:
        m = bpy.data.materials.new('L01R_' + key)
        m.diffuse_color = (*PALETTE[key], 1)
        m.use_nodes = True
        bsdf = m.node_tree.nodes.get('Principled BSDF')
        bsdf.inputs['Base Color'].default_value = (*PALETTE[key], 1)
        bsdf.inputs['Roughness'].default_value = .8
        materials[key] = m
    return materials[key]

def finish(obj, name, scale, mat):
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material(mat))
    parts.append(obj)
    return obj

def box(name, pos, size, mat='Slate', bevel=0.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    obj = finish(bpy.context.object, name, size, mat)
    if bevel:
        mod = obj.modifiers.new('Crafted_edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj

def cone(name, pos, radius, top, height, mat='Iron', verts=8):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=radius, radius2=top,
                                  depth=height, location=pos)
    return finish(bpy.context.object, name, (1, 1, 1), mat)

def gem(name, pos, scale, mat='Soul'):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1, location=pos)
    return finish(bpy.context.object, name, scale, mat)

def limb(name, start, end, radius, mat='Iron', tip=None):
    midpoint = (Vector(start) + Vector(end)) / 2
    direction = Vector(end) - Vector(start)
    obj = cone(name, midpoint, radius, radius if tip is None else tip,
               direction.length, mat)
    obj.rotation_euler = direction.to_track_quat('Z', 'Y').to_euler()
    return obj

def gravestone():
    box('Foot', (0,0,.09), (1,.55,.18), 'Dark')
    box('Plinth', (0,0,.22), (.85,.40,.14), 'Edge')
    box('Tablet', (0,0,.72), (.67,.24,.94))
    gem('Crown', (0,0,1.16), (.37,.15,.27), 'Slate')
    box('Inset', (0,-.135,.76), (.45,.028,.50), 'Dark', .01)
    box('Sigil', (0,-.16,.80), (.055,.025,.26), 'Soul', 0)
    box('SigilArm', (0,-.16,.85), (.20,.025,.04), 'Soul', 0)

def crossgrave():
    box('Foot', (0,0,.1), (.85,.6,.2), 'Dark')
    box('Stem', (0,0,.78), (.24,.22,1.35), 'Edge')
    box('Arms', (0,0,1.10), (.90,.25,.23), 'Slate')
    gem('Seal', (0,-.16,1.10), (.14,.06,.14), 'Bronze')

def coffin():
    box('Casket', (0,0,.25), (.72,1.9,.48), 'Wood', .09)
    box('Lid', (0,0,.52), (.80,2,.12), 'Wood', .05)
    for y in [-.68,.68]: box('Band', (0,y,.60), (.82,.09,.035), 'Bronze', .008)
    box('Seal', (0,0,.61), (.06,.48,.045), 'Bronze', .006)
    box('SealArms', (0,-.06,.61), (.28,.06,.045), 'Bronze', .006)

def crypt():
    box('Foundation', (0,0,.12), (2.6,2.3,.24), 'Dark')
    box('Chamber', (0,0,1.1), (2.2,1.9,2.0))
    box('Door', (0,-.98,.86), (.95,.06,1.50), 'Dark')
    for x in [-.8,.8]:
        box('Pillar', (x,-1.03,1.1), (.26,.30,1.9), 'Edge')
        box('Capital', (x,-1.03,2.03), (.39,.40,.18), 'Bronze')
    box('Cornice', (0,0,2.16), (2.5,2.20,.22), 'Edge')
    roof = cone('Roof', (0,0,2.5), 1.7, 0, .65, 'Dark', 4)
    roof.rotation_euler.z = math.pi / 4
    gem('DoorSeal', (0,-1.03,1.3), (.12,.055,.20))

def fence():
    for x in [-1,1]:
        box('Post', (x,0,.85), (.18,.18,1.7), 'Iron')
        cone('Finial', (x,0,1.82), .12, 0, .25, 'Bronze')
    for z in [.40,1.10]: box('Rail',(0,0,z),(2.1,.09,.08),'Iron', .008)
    for x in [-.66,-.33,0,.33,.66]:
        box('Bar',(x,0,.85),(.055,.055,1.2),'Iron',.004)
        cone('Spear',(x,0,1.53),.075,0,.2,'Iron')

def gate():
    for x in [-1.65,1.65]:
        box('Base',(x,0,.15),(.85,.85,.30),'Dark')
        box('Pillar',(x,0,1.35),(.60,.6,2.5))
        box('Cap',(x,0,2.62),(.8,.8,.18),'Edge')
        gem('SoulFinial',(x,0,2.95),(.18,.18,.32))
    for x in [-1.2,-.9,-.6,-.3,.3,.6,.9,1.2]:
        box('GateBar',(x,0,1.15),(.055,.07,2.25),'Iron',.006)
        cone('Spear',(x,0,2.36),.085,0,.22,'Bronze')
    for z in [.30,1.55]:box('GateRail',(0,0,z),(2.8,.11,.10),'Iron')
    gem('Lock',(0,-.09,1.05),(.15,.06,.20),'Bronze')

def deadtree():
    limb('Trunk',(0,0,0),(.12,0,2.8),.22,'Wood',.07)
    for start,end in [((0,0,.6),(-.75,.25,0)),((0,0,.3),(.65,-.3,0)),
                      ((.08,0,1.4),(-.8,0,2.1)),((-.8,0,2.1),(-1,.2,2.8)),
                      ((.1,0,2.0),(.85,.25,2.65)),((.85,.25,2.65),(1.15,.2,3)),
                      ((.12,0,2.7),(-.15,.15,3.6))]:
        limb('Branch',start,end,.08,'Wood',.02)

def rocks():
    for p,s in [((0,0,.36),(.65,.53,.45)),((.6,.2,.22),(.40,.33,.3)),
                ((-.45,.2,.2),(.35,.5,.25))]:gem('Rock',p,s,'Slate')
    gem('Moss',(0,0,.72),(.3,.25,.06),'Moss')

def lantern():
    box('Foot',(0,0,.08),(.5,.5,.16),'Dark')
    cone('Stem',(0,0,.58),.08,.06,1.0,'Iron')
    box('Floor',(0,0,1.16),(.45,.45,.09),'Bronze')
    for x in [-.18,.18]:
        for y in [-.18,.18]:box('Frame',(x,y,1.45),(.045,.045,.55),'Iron',.004)
    gem('SoulFlame',(0,0,1.45),(.13,.13,.23))
    cone('Roof',(0,0,1.81),.34,0,.25,'Iron',4)

def wand():
    cone('Shaft',(0,0,.62),.055,.045,1.24,'Wood')
    for z in [.12,.75,1.12]:cone('Binding',(0,0,z),.065,.065,.1,'Bronze')
    for x in [-.12,.12]:limb('Prong',(0,0,1.10),(x,0,1.47),.03,'Bronze')
    gem('SoulCrystal',(0,0,1.38),(.12,.12,.22))

def xp():
    gem('Gem',(0,0,.24),(.18,.18,.24))
    cone('Collar',(0,0,.10),.16,.12,.10,'Bronze')

def chest():
    box('Body',(0,0,.3),(1,.7,.60),'Wood',.06)
    box('Lid',(0,0,.68),(1.05,.75,.20),'Wood',.07)
    for x in [-.38,.38]:box('Band',(x,0,.70),(.1,.78,.18),'Bronze')
    box('Latch',(0,-.40,.48),(.20,.06,.22),'Bronze')
    gem('LockSoul',(0,-.44,.5),(.05,.025,.075))

def actor(ghoul=False):
    armor='Bone' if ghoul else 'Iron'
    for x in [-.18,.18]:
        box('Boot',(x,-.08,.13),(.24,.38,.26),'Dark')
        limb('Shin',(x,0,.26),(x,.03,.61),.10,armor)
        limb('Thigh',(x,.03,.61),(x,0,.98),.13,armor)
    cone('Torso',(0,0,1.22),.25,.34,.48,armor)
    box('Belt',(0,-.02,1.0),(.53,.34,.09),'Bronze')
    gem('Buckle',(0,-.21,1.0),(.065,.035,.065))
    for sign in [-1,1]:
        gem('Shoulder',(sign*.39,0,1.43),(.22,.24,.19),armor)
        limb('UpperArm',(sign*.4,0,1.37),(sign*.49,-.02,1.09),.09,armor)
        limb('Forearm',(sign*.49,-.02,1.09),(sign*.50,-.11,.84),.10,armor)
        gem('Hand',(sign*.50,-.11,.80),(.09,.085,.11),'Bone' if ghoul else 'Dark')
    gem('Head',(0,0,1.70),(.23,.20,.27),armor)
    box('Visor',(0,-.186,1.73),(.32,.045,.065),'Dark',.01)
    for x in [-.08,.08]:box('Eye',(x,-.215,1.73),(.065,.02,.03),'Soul',0)
    if not ghoul:
        box('ChestPlate',(0,-.23,1.26),(.40,.07,.32),'Edge')
        gem('ChestSoul',(0,-.285,1.3),(.055,.025,.10))
        cape=box('Cape',(0,.20,.96),(.65,.07,1.08),'Cloth')
        cape.rotation_euler.x=-.13
    else:
        for x in [-.11,0,.11]:box('Rib',(x,-.27,1.22),(.045,.07,.3),'Dark',.008)

builders=[gravestone,crossgrave,coffin,crypt,gate,fence,deadtree,rocks,lantern,
          wand,xp,chest,lambda:actor(False),lambda:actor(True)]
manifest=[]
for name, build in zip(NAMES, builders):
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    parts=[]
    build()
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts: obj.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]
    bpy.ops.object.join()
    model=bpy.context.object
    model.name='L01R_'+name
    bpy.context.scene.cursor.location=(0,0,0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project()
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.scene.unit_settings.system='METRIC'
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(name+'.blend')))
    bpy.ops.export_scene.fbx(filepath=str(EXPORT/('L01R_'+name+'.fbx')),
        use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',
        apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
    triangles=sum(len(p.vertices)-2 for p in model.data.polygons)
    manifest.append({'name':model.name,'triangles':triangles,
        'source':str(SOURCE/(name+'.blend')),'model':str(EXPORT/('L01R_'+name+'.fbx')),
        'status':'static visual review; no rig or gameplay components'})
(SOURCE/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('REVIEW_KIT_COMPLETE '+json.dumps({'assets':len(manifest),'triangles':sum(x['triangles'] for x in manifest)}))
