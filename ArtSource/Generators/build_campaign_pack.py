"""Additive campaign art batch; no existing asset overwrites.
Reuses only geometry functions from our local review generator, not its export loop.
Produces low-poly palette art and simple generic skeletal clips, not gameplay logic.
"""
import ast
import bpy
import json
import math
import types
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parent.parent
OUT=ROOT/'ArtSource'/'SH10_v01'
MODELS=ROOT/'Assets'/'Art'/'Models'
if OUT.exists(): raise RuntimeError('SH10_v01 already exists; choose a new version')
if list(MODELS.glob('SH10_*.fbx')): raise RuntimeError('SH10 models already exist')
tree=ast.parse((ROOT/'Tools'/'build_level01_review.py').read_text(encoding='utf-8'))
selected=[]
for node in tree.body:
    if isinstance(node,(ast.Import,ast.ImportFrom,ast.FunctionDef)): selected.append(node)
    elif isinstance(node,ast.Assign) and any(isinstance(t,ast.Name) and t.id in ('PALETTE','materials','parts') for t in node.targets): selected.append(node)
g=types.ModuleType('review_geometry')
exec(compile(ast.Module(body=selected,type_ignores=[]),'review_geometry','exec'),g.__dict__)
BASE=dict(g.PALETTE)
OUT.mkdir(parents=True)
MODELS.mkdir(parents=True,exist_ok=True)

LEVELS=[
('01_Cursed_Graveyard','The Cursed Graveyard',(.25,.30,.34),(.1,.85,.8),['Gravestone','CrossGrave','Crypt','Gate','DeadTree']),
('02_Dark_Forest','The Dark Forest',(.12,.22,.13),(.45,.9,.15),['Pine','FallenLog','Mushrooms','ForestShrine','Briar']),
('03_Burning_Village','The Burning Village',(.26,.13,.09),(1,.25,.04),['BurntHouse','Chimney','Cart','Rubble','Brazier']),
('04_Ruined_Castle','The Ruined Castle',(.32,.30,.36),(.55,.25,.95),['CastleWall','Tower','Stairs','SpikeTrap','Arch']),
('05_Crimson_Swamp','The Crimson Swamp',(.24,.11,.15),(.65,.1,.23),['BogPool','Reeds','Stump','Mushrooms','BogTotem']),
('06_Frozen_Peaks','The Frozen Peaks',(.47,.64,.75),(.2,.75,1),['IceCrystal','SnowDrift','IceArch','Glacier','FrozenObelisk']),
('07_Sea_of_Lost_Souls','The Sea of Lost Souls',(.12,.32,.37),(.15,.95,.85),['Jetty','Boat','Anchor','SpectralTree','SoulBeacon']),
('08_Blood_Arenas','The Blood Arenas',(.30,.12,.12),(.95,.07,.06),['ArenaWall','BloodBasin','SpikeBarricade','Banner','WeaponRack']),
('09_Throne_of_Death','The Throne of Death',(.20,.18,.28),(.75,.25,.95),['Throne','Pillar','RoyalStairs','Obelisk','DeathSigil']),
('10_Soul_Core','The Soul Core',(.13,.23,.30),(.1,.95,.95),['CorePrism','CoreRing','Pylon','MirrorSpikes','Portal'])]
WEAPONS=['MagicWand','Whip','Garlic','Axe','Bible','Cross','Knife','FireWand','SantaWater','Runetracer','LightningRing','Pentagram','Peachone','Bone','CherryBomb','ClockLancet','Laurel','Gun','SongOfMana','BloodyTear','HolyWand','DeathSpiral','ThousandEdge','Vandalier']
PICKUPS=['XP_Gem','Gold_Coin','Health_Chicken','Chest','Magnet','TimeFreeze']
manifest=[]

def torus(pos,major,minor,mat='Bronze',rotation=(0,0,0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=16,minor_segments=6,location=pos,rotation=rotation)
    return g.finish(bpy.context.object,'Ring',(1,1,1),mat)

def pillar(x=0,y=0,h=2):
    g.box('Base',(x,y,.12),(.75,.75,.24),'Dark')
    g.cone('Shaft',(x,y,h/2),.24,.20,h,'Slate')
    g.box('Crown',(x,y,h),(.70,.70,.18),'Edge')

def environment(kind):
    if kind=='Ground':
        g.box('Tile',(0,0,-.12),(10,10,.24),'Moss',0)
        for x,y in [(-3,-2),(2,3),(-1,3),(3,-3)]:g.gem('TerrainFacet',(x,y,-.01),(1.1,.7,.055),'Slate')
    elif kind=='Gravestone':g.gravestone()
    elif kind=='CrossGrave':g.crossgrave()
    elif kind=='Crypt':g.crypt()
    elif kind=='Gate':g.gate()
    elif kind in ('DeadTree','SpectralTree'):
        g.deadtree()
        if kind=='SpectralTree':
            for p in [(-.8,0,2.1),(.85,.25,2.65),(0,0,3.4)]:g.gem('SoulFruit',p,(.12,.12,.18),'Soul')
    elif kind=='Pine':
        g.cone('Trunk',(0,0,.7),.16,.13,1.4,'Wood')
        for z,r in [(1.15,1),(1.85,.76),(2.5,.48)]:g.cone('Needles',(0,0,z),r,0,1.3,'Moss')
    elif kind=='FallenLog':
        g.limb('Log',(-1.3,0,.3),(1.3,0,.3),.30,'Wood')
        for x in [-.6,.7]:g.limb('Branch',(x,0,.3),(x+.25,.5,.65),.09,'Wood',.03)
    elif kind=='Mushrooms':
        for x,y,z in [(-.4,0,.45),(.3,.2,.7),(.6,-.3,.30)]:
            g.cone('Stem',(x,y,z/2),.07,.05,z,'Bone');g.gem('Cap',(x,y,z),(.3,.3,.12),'Soul')
    elif kind in ('ForestShrine','BogTotem','SoulBeacon'):
        pillar(h=1.2);g.gem('Soul',(0,0,1.7),(.3,.3,.55),'Soul')
        for x in [-.5,.5]:g.limb('Prong',(x,0,.3),(x*.6,0,1.6),.09,'Wood',.03)
    elif kind=='Briar':
        for i in range(5):
            x=(i-2)*.35;g.limb('Root',(x,-.4,0),(x,.4,.7),.09,'Wood',.025)
            g.limb('Thorn',(x,0,.35),(x+.3,0,.65),.06,'Wood',0)
    elif kind=='BurntHouse':
        g.box('Foundation',(0,0,.08),(3,2.5,.16),'Dark')
        for x in [-1.2,1.2]:g.box('Wall',(x,0,1),(.16,2.3,2),'Wood')
        g.box('Back',(0,1.1,.85),(2.5,.15,1.7),'Wood')
        for x in [-1,0,1]:g.limb('BurntRafter',(x,-1.1,2),(x,0,2.8),.07,'Dark')
    elif kind=='Chimney':
        for z in range(6):g.box('BrickCourse',(0,0,.18+z*.30),(.65,.65,.28),'Slate')
        g.box('BlackOpening',(0,0,1.88),(.45,.45,.04),'Dark')
    elif kind=='Cart':
        g.box('Bed',(0,0,.65),(1.2,1.6,.14),'Wood')
        for x in [-.65,.65]:
            torus((x,0,.4),.35,.06,'Iron',(0,math.pi/2,0))
            g.box('Side',(x,0,.9),(.08,1.6,.45),'Wood')
        g.limb('Handle',(0,.7,.6),(0,2,.45),.05,'Wood')
    elif kind in ('Rubble','SnowDrift','Glacier'):
        g.rocks()
        if kind=='Glacier':g.gem('Peak',(0,0,1.3),(1.2,.9,1.7),'Edge')
    elif kind=='Brazier':
        g.cone('Bowl',(0,0,.6),.25,.5,.35,'Iron')
        for x,y in [(-.3,-.2),(.3,-.2),(0,.3)]:g.limb('Leg',(x,y,0),(x,y,.6),.04,'Iron')
        for x in [-.2,0,.2]:g.gem('Flame',(x,0,1.0),(.16,.18,.5),'Soul')
    elif kind in ('CastleWall','ArenaWall'):
        g.box('Wall',(0,0,1.0),(3,.6,2),'Slate')
        for x in [-1.2,-.4,.4,1.2]:g.box('Crenellation',(x,0,2.15),(.45,.65,.4),'Edge')
    elif kind=='Tower':
        g.cone('Tower',(0,0,1.4),.8,.8,2.8,'Slate',12)
        g.cone('Coping',(0,0,2.85),.95,.95,.22,'Edge',12)
        for i in range(8):
            a=i*math.tau/8;g.box('Merlon',(.8*math.cos(a),.8*math.sin(a),3.1),(.3,.3,.4),'Slate')
    elif kind in ('Stairs','RoyalStairs'):
        for i in range(5):g.box('Step',(0,i*.35,.12*(i+1)),(2,.38,.24*(i+1)),'Edge',.01)
    elif kind in ('SpikeTrap','SpikeBarricade','MirrorSpikes'):
        g.box('Base',(0,0,.08),(2,1,.16),'Iron')
        for x in [-.75,-.25,.25,.75]:
            for y in [-.25,.25]:g.cone('Spike',(x,y,.58),.12,0,1,'Edge')
    elif kind in ('Arch','IceArch','Portal'):
        for x in [-1.1,1.1]:pillar(x=x,h=2)
        g.box('Lintel',(0,0,2.15),(2.9,.6,.35),'Edge')
        g.gem('Keystone',(0,-.35,2.2),(.2,.12,.3),'Soul')
        if kind=='Portal':torus((0,0,1.2),.83,.08,'Soul',(math.pi/2,0,0))
    elif kind in ('BogPool','BloodBasin'):
        g.cone('Pool',(0,0,.08),1,1,.16,'Soul',16);torus((0,0,.14),1,.12,'Slate')
    elif kind=='Reeds':
        for i in range(7):
            x=(i%3-1)*.19;y=(i//3-1)*.2;z=.8+(i%3)*.18
            g.limb('Stem',(x,y,0),(x+.1,y,z),.018,'Moss');g.cone('Head',(x+.1,y,z),.045,.045,.24,'Wood')
    elif kind=='Stump':
        g.cone('Stump',(0,0,.45),.6,.4,.9,'Wood');g.cone('Cut',(0,0,.91),.38,.38,.02,'Bronze')
    elif kind in ('IceCrystal','FrozenObelisk','Obelisk','CorePrism','Pylon'):
        g.box('Plinth',(0,0,.1),(1,1,.2),'Dark')
        g.gem('Crystal',(0,0,1.4),(.48,.48,1.4),'Soul' if kind in ('IceCrystal','CorePrism') else 'Edge')
        torus((0,0,.8),.5,.055,'Bronze')
    elif kind=='Jetty':
        for y in range(8):g.box('Plank',(0,y*.25,.35),(1.4,.23,.10),'Wood')
        for x in [-.6,.6]:
            for y in [0,1.75]:g.cone('Pile',(x,y,.35),.09,.08,1.1,'Wood')
    elif kind=='Boat':
        g.gem('Hull',(0,0,.45),(.70,1.5,.4),'Wood')
        g.box('Seat',(0,0,.72),(1.1,.2,.1),'Bronze')
        g.limb('Oar',(-.7,-.3,.8),(.9,.8,.5),.045,'Wood')
    elif kind=='Anchor':
        g.box('Stem',(0,0,.7),(.12,.12,1.4),'Iron')
        torus((0,0,1.5),.15,.04,'Iron',(math.pi/2,0,0))
        for s in [-1,1]:g.limb('Fluke',(0,0,.1),(s*.65,0,.5),.09,'Iron');g.cone('Tip',(s*.65,0,.6),.15,0,.3,'Iron')
    elif kind=='Banner':
        g.cone('Pole',(0,0,1.25),.05,.05,2.5,'Iron');g.box('Banner',(.42,0,1.8),(.8,.04,1),'Cloth')
        g.gem('Emblem',(.42,-.03,1.8),(.15,.02,.22),'Bronze')
    elif kind=='WeaponRack':
        for x in [-.8,.8]:g.box('Post',(x,0,.65),(.12,.15,1.3),'Wood')
        g.box('Beam',(0,0,1.15),(1.8,.15,.14),'Wood')
        for x in [-.5,0,.5]:g.limb('Weapon',(x,-.1,.1),(x,-.1,1.6),.035,'Iron');g.gem('Blade',(x,-.1,1.4),(.14,.035,.32),'Edge')
    elif kind=='Throne':
        g.box('Dais',(0,0,.15),(2,2,.3),'Dark');g.box('Seat',(0,0,.7),(1.1,1,.25),'Cloth')
        g.box('Back',(0,.4,1.4),(1.1,.2,1.5),'Iron')
        for x in [-.65,.65]:g.box('Arm',(x,0,1),(.2,1,.2),'Bronze');g.cone('Horn',(x,.4,2.1),.16,0,.8,'Bone')
        g.gem('Crown',(0,.23,1.75),(.22,.06,.25),'Soul')
    elif kind=='Pillar':pillar(h=3)
    elif kind in ('DeathSigil','CoreRing'):
        torus((0,0,.1),1.25,.08,'Soul')
        for i in range(6):
            a=i*math.tau/6;g.gem('Rune',(math.cos(a),math.sin(a),.14),(.1,.1,.2),'Bronze')
    else:raise RuntimeError(kind)

def weapon(kind):
    if kind in ('MagicWand','FireWand','HolyWand'):g.wand()
    elif kind in ('Axe','DeathSpiral'):
        g.cone('Haft',(0,0,.65),.055,.05,1.3,'Wood')
        g.gem('Blade',(.20,0,1.1),(.45,.08,.34),'Edge')
        if kind=='DeathSpiral':g.gem('SecondBlade',(-.20,0,1.1),(.45,.08,.34),'Edge')
    elif kind in ('Knife','ThousandEdge'):
        g.box('Grip',(0,0,.16),(.09,.1,.32),'Wood');g.box('Guard',(0,0,.34),(.3,.1,.06),'Bronze')
        g.gem('Blade',(0,0,.63),(.10,.045,.36),'Edge')
    elif kind in ('Bible','SongOfMana'):
        g.box('Pages',(0,0,.18),(.5,.65,.25),'Bone');g.box('Cover',(0,0,.33),(.56,.70,.05),'Cloth');g.box('LowerCover',(0,0,.035),(.56,.70,.05),'Cloth')
        g.gem('Seal',(0,0,.38),(.1,.1,.05),'Soul')
    elif kind=='Cross':g.box('Stem',(0,0,.45),(.12,.12,.9),'Bronze');g.box('Arms',(0,0,.6),(.7,.12,.12),'Edge')
    elif kind in ('Whip','BloodyTear'):
        g.limb('Handle',(0,0,0),(0,0,.4),.05,'Wood')
        for i in range(10):g.limb('Chain',(i*.12,0,.4+math.sin(i*.4)*.2),((i+1)*.12,0,.4+math.sin((i+1)*.4)*.2),.025,'Iron')
    elif kind=='Garlic':
        for i in range(6):a=i*math.tau/6;g.gem('Clove',(.12*math.cos(a),.12*math.sin(a),.2),(.14,.14,.22),'Bone')
        g.cone('Stalk',(0,0,.48),.08,.025,.3,'Moss')
    elif kind in ('SantaWater','TimeFreeze'):
        g.gem('Bottle',(0,0,.25),(.22,.22,.3),'Soul');g.cone('Neck',(0,0,.57),.08,.08,.2,'Bronze')
    elif kind=='Bone':
        g.limb('Bone',(0,0,.15),(0,0,.8),.07,'Bone')
        for z in [.12,.84]:
            for x in [-.08,.08]:g.gem('Joint',(x,0,z),(.12,.1,.1),'Bone')
    elif kind=='CherryBomb':g.gem('Bomb',(0,0,.3),(.3,.3,.3),'Iron');g.limb('Fuse',(0,0,.55),(.1,0,.78),.025,'Bronze')
    elif kind=='Gun':g.box('Barrel',(0,0,.4),(.15,.65,.14),'Iron');g.box('Grip',(0,.2,.22),(.12,.17,.35),'Wood')
    elif kind in ('Peachone','Vandalier'):
        g.gem('Body',(0,0,.25),(.18,.35,.2),'Bone');g.gem('Head',(0,-.32,.4),(.14,.15,.14),'Bone')
        for s in [-1,1]:g.gem('Wing',(s*.34,0,.3),(.4,.24,.05),'Edge')
    elif kind=='ClockLancet':
        torus((0,0,.4),.3,.04,'Bronze',(math.pi/2,0,0));g.limb('Hand',(0,0,.4),(0,0,.65),.02,'Soul')
    elif kind in ('LightningRing','Laurel','Pentagram'):
        torus((0,0,.12),.6,.06,'Soul')
        for i in range(5):a=i*math.tau/5;g.gem('Rune',(.6*math.cos(a),.6*math.sin(a),.16),(.08,.08,.12),'Bronze')
    elif kind=='Runetracer':g.gem('Rune',(0,0,.4),(.18,.18,.4),'Soul')
    else:raise RuntimeError(kind)

def pickup(kind):
    if kind=='XP_Gem':g.xp()
    elif kind=='Chest':g.chest()
    elif kind=='Gold_Coin':g.cone('Coin',(0,0,.22),.22,.22,.06,'Bronze',16)
    elif kind=='Health_Chicken':g.gem('Meat',(0,0,.2),(.27,.4,.2),'Bronze');g.limb('Bone',(0,.2,.2),(0,.6,.2),.06,'Bone')
    elif kind=='Magnet':
        for x in [-.2,.2]:g.box('Arm',(x,0,.3),(.12,.15,.6),'Cloth');g.box('Tip',(x,0,.63),(.12,.15,.12),'Edge')
        g.box('Base',(0,0,.06),(.5,.15,.12),'Cloth')
    elif kind=='TimeFreeze':
        for z in [.06,.7]:g.box('Frame',(0,0,z),(.4,.4,.08),'Bronze')
        g.cone('Upper',(0,0,.51),.04,.17,.3,'Soul');g.cone('Lower',(0,0,.22),.17,.04,.3,'Soul')

def rig_actor(role,level):
    # Segmented low-poly armor uses rigid weights; deformation polish is a later pass.
    assignments={}
    def record(bone,fn):
        before=len(g.parts);fn()
        for obj in g.parts[before:]:assignments[obj.name]=bone
    metal='Iron' if role=='Kael' else 'Bone' if role=='Enemy' else 'Edge'
    record('Spine',lambda:g.cone('Torso',(0,0,1.25),.23,.35,.50,metal))
    record('Spine',lambda:g.box('Breastplate',(0,-.24,1.26),(.40,.08,.30),'Slate'))
    record('Spine',lambda:g.gem('SoulSeal',(0,-.30,1.3),(.07,.04,.12),'Soul'))
    record('Head',lambda:g.gem('Helmet',(0,0,1.75),(.23,.22,.28),metal))
    record('Head',lambda:g.box('Visor',(0,-.21,1.77),(.32,.045,.07),'Dark'))
    for x in [-.08,.08]:record('Head',lambda x=x:g.box('Eye',(x,-.24,1.77),(.06,.02,.035),'Soul',0))
    for side,s in [('L',-1),('R',1)]:
        record('Thigh.'+side,lambda s=s:g.limb('Thigh',(s*.18,0,1),(s*.18,0,.58),.12,metal))
        record('Shin.'+side,lambda s=s:g.limb('Shin',(s*.18,0,.58),(s*.18,0,.18),.10,metal))
        record('Shin.'+side,lambda s=s:g.box('Boot',(s*.18,-.09,.13),(.24,.4,.26),'Dark'))
        record('UpperArm.'+side,lambda s=s:g.gem('Pauldron',(s*.39,0,1.46),(.22,.23,.17),metal))
        record('UpperArm.'+side,lambda s=s:g.limb('UpperArm',(s*.40,0,1.42),(s*.50,0,1.12),.10,metal))
        record('Forearm.'+side,lambda s=s:g.limb('Forearm',(s*.50,0,1.12),(s*.50,-.1,.83),.095,metal))
        if role=='Boss':record('Head',lambda s=s:g.limb('Horn',(s*.17,0,1.9),(s*.4,.05,2.4),.12,'Bronze',0))
    if role in ('Kael','Elite','Boss'):
        record('Spine',lambda:g.box('Cape',(0,.23,1.0),(.65,.07,1.02),'Cloth'))
    if level in (2,5,7):record('Spine',lambda:g.gem('Growth',(.3,.15,1.6),(.24,.2,.34),'Moss'))
    if level in (3,6,10):record('Spine',lambda:g.gem('Crystal',(-.35,0,1.58),(.16,.18,.36),'Soul'))
    bones=[('Root',(0,0,0),(0,0,.5),None),('Spine',(0,0,1),(0,0,1.5),'Root'),('Head',(0,0,1.5),(0,0,1.98),'Spine')]
    for side,s in [('L',-1),('R',1)]:
        bones += [('Thigh.'+side,(s*.18,0,1),(s*.18,0,.58),'Root'),('Shin.'+side,(s*.18,0,.58),(s*.18,0,.15),'Thigh.'+side),('UpperArm.'+side,(s*.4,0,1.42),(s*.5,0,1.12),'Spine'),('Forearm.'+side,(s*.5,0,1.12),(s*.5,-.1,.83),'UpperArm.'+side)]
    bpy.ops.object.select_all(action='DESELECT')
    arm=bpy.data.armatures.new('GenericRig');rig=bpy.data.objects.new('Rig',arm);bpy.context.collection.objects.link(rig);rig.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='EDIT')
    for name,head,tail,parent in bones:
        b=arm.edit_bones.new(name);b.head=head;b.tail=tail
        if parent:b.parent=arm.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT')
    for obj in g.parts:
        group=obj.vertex_groups.new(name=assignments[obj.name]);group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
    return rig

def clips(rig):
    rig.animation_data_create()
    for name in ['Idle','Run','Attack','Death']:
        action=bpy.data.actions.new(name);rig.animation_data.action=action
        for frame in [1,7,13,19,25]:
            t=(frame-1)/24;phase=math.sin(t*math.tau)
            for b in rig.pose.bones:
                b.rotation_mode='XYZ';b.rotation_euler=(0,0,0);b.location=(0,0,0)
            if name=='Idle':rig.pose.bones['Spine'].rotation_euler.x=.025*phase
            elif name=='Run':
                for side,s in [('L',1),('R',-1)]:
                    rig.pose.bones['Thigh.'+side].rotation_euler.x=s*.55*phase
                    rig.pose.bones['Shin.'+side].rotation_euler.x=max(0,-s*phase)*.7
                    rig.pose.bones['UpperArm.'+side].rotation_euler.x=-s*.45*phase
            elif name=='Attack':rig.pose.bones['UpperArm.R'].rotation_euler.x=-1.7*math.sin(t*math.pi);rig.pose.bones['Spine'].rotation_euler.z=.22*math.sin(t*math.pi)
            else:rig.pose.bones['Root'].rotation_euler.x=-math.pi/2*t
            for b in rig.pose.bones:b.keyframe_insert(data_path='rotation_euler',frame=frame,group=b.name)
        action.use_fake_user=True
    rig.animation_data.action=bpy.data.actions.get('Idle');bpy.context.scene.frame_set(1)

def export(pack,name,category,build,actor=False,stone=None,soul=None):
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    for a in list(bpy.data.actions):bpy.data.actions.remove(a)
    g.parts=[];g.materials={};g.PALETTE=dict(BASE)
    if stone:g.PALETTE.update(Slate=stone,Edge=tuple(min(1,c*1.5) for c in stone),Moss=tuple(c*.7 for c in stone))
    if soul:g.PALETTE['Soul']=soul
    rig=build()
    bpy.ops.object.select_all(action='DESELECT')
    for p in g.parts:p.select_set(True)
    bpy.context.view_layer.objects.active=g.parts[0];bpy.ops.object.join();mesh=bpy.context.object
    mesh.name=name;bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project();bpy.ops.object.mode_set(mode='OBJECT')
    for key,m in g.materials.items():m.name=name+'_'+key
    if actor:
        mod=mesh.modifiers.new('Rig','ARMATURE');mod.object=rig;mesh.parent=rig;clips(rig);rig.select_set(True)
    bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=25;bpy.context.scene.render.fps=24;bpy.context.scene.unit_settings.system='METRIC'
    source=OUT/pack/'Blender'/f'{name}.blend';source.parent.mkdir(parents=True,exist_ok=True)
    model=MODELS/f'{name}.fbx'
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    bpy.ops.export_scene.fbx(filepath=str(model),use_selection=True,object_types={'MESH','ARMATURE'} if actor else {'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,add_leaf_bones=False,bake_anim=actor,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True)
    manifest.append(dict(name=name,pack=pack,category=category,actor=actor,source=str(source),model=str(model),triangles=sum(len(p.vertices)-2 for p in mesh.data.polygons),materials={m.name:list(g.PALETTE[k]) for k,m in g.materials.items()},clips=['Idle','Run','Attack','Death'] if actor else [],status='art asset; gameplay integration not verified'))
    print('ASSET_DONE',name,flush=True)

for index,(pack,title,stone,soul,props) in enumerate(LEVELS,1):
    for prop in ['Ground']+props:export(pack,f'SH10_L{index:02}_{prop}','Environment',lambda p=prop:environment(p),stone=stone,soul=soul)
    for role in ['Enemy','Elite','Boss']:export(pack,f'SH10_L{index:02}_{role}','Enemies' if role!='Boss' else 'Bosses',lambda r=role,l=index:rig_actor(r,l),True,stone,soul)
export('Shared','SH10_Kael','Player',lambda:rig_actor('Kael',1),True)
for kind in WEAPONS:export('Shared','SH10_'+kind,'Weapons',lambda k=kind:weapon(k))
for kind in PICKUPS:export('Shared','SH10_'+kind,'Items',lambda k=kind:pickup(k))
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
(OUT/'levels.json').write_text(json.dumps([dict(folder=l[0],name=l[1]) for l in LEVELS],indent=2),encoding='utf-8')
print('CAMPAIGN_ART_COMPLETE',len(manifest),flush=True)
