"""Measures how far the Golem model's soles sit above (+) or below (-) the floor through a walk and run cycle with the foot-flat plus body-drop rule of GolemRigFx (run with bpy: python -I tools/blender/check_golem_feet.py). Needs Creature_GolemThachAnh_Meshy.fbx."""
import bpy, sys, math
from mathutils import Matrix, Vector
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath="/home/user/gametienhoa/Assets/_Project/Models/Wildlife/Meshy/Creature_GolemThachAnh_Meshy.fbx")
arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE'); mesh = [o for o in bpy.data.objects if o.type == 'MESH']
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode='POSE')
pb = arm.pose.bones
print("bones:", [b.name for b in pb if 'Leg' in b.name or 'Foot' in b.name or 'Toe' in b.name or b.name == 'Hips'])
def wpos(b): return arm.matrix_world @ b.head
def minz():
    dg = bpy.context.evaluated_depsgraph_get(); m = 1e9
    for o in mesh:
        e = o.evaluated_get(dg); me = e.to_mesh()
        for v in me.vertices: m = min(m, (o.matrix_world @ v.co).z)
        e.to_mesh_clear()
    return m
bpy.context.view_layer.update()
rest = minz()
L = wpos(pb['Hips']).z - wpos(pb['LeftFoot']).z
lat = (wpos(pb['LeftUpLeg']) - wpos(pb['RightUpLeg'])); lat.z = 0; lat.normalize()
print("legLen(hip->ankle)=%.3f  rest min z=%.3f  hips z=%.3f" % (L, rest, wpos(pb['Hips']).z))
def rot(b, deg):   # rotate bone about the lateral axis through its head (deg>0 = some fixed direction, sign fixed later)
    R = Matrix.Rotation(math.radians(deg), 4, lat); h = b.matrix.translation.copy()
    b.matrix = Matrix.Translation(h) @ R @ Matrix.Translation(-h) @ b.matrix
    bpy.context.view_layer.update()
def reset():
    for b in pb: b.rotation_mode='QUATERNION'; b.rotation_quaternion=(1,0,0,0); b.location=(0,0,0)
    bpy.context.view_layer.update()
# which sign swings the foot FORWARD (toe side)? toe direction from foot bone to its child / mesh
reset(); f0 = wpos(pb['LeftFoot']); rot(pb['LeftUpLeg'], 10); f1 = wpos(pb['LeftFoot']); s_hip = 1 if (f1 - f0).dot(lat.cross(Vector((0,0,1)))) > 0 else -1
# forward direction = sign making toe point: use the foot tail minus head as forward guess
fwd = lat.cross(Vector((0,0,1))); ft = pb['LeftFoot']; toe = (arm.matrix_world @ ft.tail) - (arm.matrix_world @ ft.head)
if toe.dot(fwd) < 0: fwd = -fwd
s_hip = 1 if (f1 - f0).dot(fwd) > 0 else -1
print("hip sign so +deg moves foot forward:", s_hip)
REST_ROT = {}
reset()
for sd in ('Left','Right'): REST_ROT[sd] = pb[sd+'Foot'].matrix.to_3x3().copy()
def pose(hL, kL, hR, kR, drop):
    reset(); _d = drop
    hb0 = pb['Hips']; hb0.location = hb0.bone.matrix_local.to_3x3().inverted() @ (arm.matrix_world.to_3x3().inverted() @ Vector((0,0,drop))); bpy.context.view_layer.update()
    for side,(h,k) in (("Left",(hL,kL)),("Right",(hR,kR))):
        rot(pb[side+'UpLeg'], s_hip*h); rot(pb[side+'Leg'], -s_hip*k); fb = pb[side+"Foot"]; fb.matrix = Matrix.Translation(fb.matrix.translation) @ REST_ROT[side].to_4x4(); bpy.context.view_layer.update()

def lowbone():
    return 0
REST_A = None
reset(); REST_A = {sd: wpos(pb[sd+'Foot']).z for sd in ('Left','Right')}; REST_LOW = 0
def needed():
    return max(REST_A[sd] - wpos(pb[sd+'Foot']).z for sd in ('Left','Right'))
bpy.context.view_layer.update()
dg0 = bpy.context.evaluated_depsgraph_get(); SOLE = {'Left': [], 'Right': []}
for o in mesh:
    e = o.evaluated_get(dg0); me = e.to_mesh()
    for v in me.vertices:
        w = o.matrix_world @ v.co
        if w.z < rest + 0.30:
            side = 'Left' if (w - wpos(pb['Hips'])).dot(lat) > 0 else 'Right'
            SOLE[side].append(w.copy())
    e.to_mesh_clear()
SOLE_LOCAL = {sd: [(pb[sd+'Foot'].matrix.inverted() @ (arm.matrix_world.inverted() @ p)) for p in pts] for sd, pts in SOLE.items()}
print('sole points', {k: len(v) for k, v in SOLE.items()})
def lowsole():
    m = 1e9
    for sd, pts in SOLE_LOCAL.items():
        M = arm.matrix_world @ pb[sd+'Foot'].matrix
        for p in pts: m = min(m, (M @ p).z)
    return m
REST_SOLE = lowsole(); print('rest sole', REST_SOLE)
def depth(a, k): return 0.5*(math.cos(math.radians(a)) + math.cos(math.radians(a-k)))
def gait(run, label):
    A = 24*(1+0.8*run); K = 40*(1+run); worst_up = -9; worst_dn = 9
    for i in range(16):
        ph = i/16
        hl = A*math.sin(2*math.pi*ph); hr = -hl
        kl = K*max(0, math.cos(2*math.pi*ph)); kr = K*max(0, -math.cos(2*math.pi*ph))
        pose(hl, kl, hr, kr, 0); drop = needed(); pose(hl, kl, hr, kr, drop)
        d = minz() - rest; print('  ph %.2f hipL %+.0f kneeL %.0f  drop %+.3f  err %+.3f' % (ph, hl, kl, drop, d)) if label=='walk' else None; worst_up = max(worst_up, d); worst_dn = min(worst_dn, d)
    print("%s: lowest-point error vs ground  floating up to %+.3f m, sinking down to %+.3f m (leg %.2f m)" % (label, worst_up, worst_dn, L))
gait(0, "walk"); gait(1, "run")
for c in (0.5, 1.0):
    a = c*35; k = c*70; pose(a, k, a, k, 0); dd = needed(); pose(a, k, a, k, dd); print("crouch %.1f: error %+.3f" % (c, minz()-rest))
a=35;k=70; pose(a,k,a,k,0); dd=REST_SOLE-lowsole(); pose(a,k,a,k,dd)
print("DEBUG sole err", lowsole()-REST_SOLE, "mesh err", minz()-rest, "hips z", wpos(pb['Hips']).z, "dd", dd)
dg = bpy.context.evaluated_depsgraph_get()
worst=None
for o in mesh:
    e=o.evaluated_get(dg); me=e.to_mesh()
    zs=sorted((o.matrix_world@v.co).z for v in me.vertices)[:3]; print("lowest verts", zs)
    e.to_mesh_clear()
print("---- worst-phase diagnosis")
A=24; K=40
for ph in (0.75,0.81):
    hl=A*math.sin(2*math.pi*ph); hr=-hl; kl=K*max(0,math.cos(2*math.pi*ph)); kr=K*max(0,-math.cos(2*math.pi*ph))
    pose(hl,kl,hr,kr,0); drop=REST_LOW-lowbone(); pose(hl,kl,hr,kr,drop)
    dg=bpy.context.evaluated_depsgraph_get(); best=(1e9,None)
    for o in mesh:
        e=o.evaluated_get(dg); me=e.to_mesh()
        for v in me.vertices:
            w=o.matrix_world@v.co
            if w.z<best[0]: best=(w.z,w.copy())
        e.to_mesh_clear()
    print("ph",ph,"drop %.3f"%drop,"ankles z L %.3f R %.3f"%(wpos(pb['LeftFoot']).z,wpos(pb['RightFoot']).z),"toe z L %.3f R %.3f"%(wpos(pb['LeftToeBase']).z,wpos(pb['RightToeBase']).z),"lowest vertex",best[1], "lat coord", best[1].dot(lat), "hips", wpos(pb['Hips']).dot(lat))
reset(); print("REST ankle L %.3f toe L %.3f"%(wpos(pb['LeftFoot']).z, wpos(pb['LeftToeBase']).z))
pose(-24,0,24,0,0)
print("posed L ankle %.3f toe %.3f ; angle foot vs rest %.1f deg"%(wpos(pb['LeftFoot']).z, wpos(pb['LeftToeBase']).z, math.degrees((pb['LeftFoot'].matrix.to_3x3().to_quaternion().rotation_difference(REST_ROT['Left'].to_quaternion())).angle)))
