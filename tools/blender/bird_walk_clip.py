"""Walk and idle clips for the renamed Dreadwing bird (called by rig_bird_dreadwing.py after the model is exported):

  Creature_ChimHangMe_Dreadwing_Walk.fbx  = the armature and the mesh (same hierarchy as the model, so the clip paths start with "Armature/") with two baked actions, "Idle" and "Walk" (30 fps)

Wings are folded against the body in both (the rest pose of an ordinary bird; BirdRigFx opens them for a fight or a flight). Walk is a 32-frame loop (two steps):
the thighs swing in turn, the knees lift the foot while the leg goes forward, the tarsus keeps the foot level, and the pelvis rides up and down so that the lowest foot is
always on the floor (planted from the posed mesh, so no foot floats or sinks). Only the bones the code does not overwrite are keyed (the legs, the pelvis' position, the wings):
CreatureLocomotion turns Root, Head and Tail1 itself and BirdRigFx adds the pitch, the head turn and the tuck on top.
Bone directions are measured in the file (the signs of "up" and "back" for each wing and "leg back" are probed), nothing is assumed about the bone axes.
"""
import math
import os

import bpy
import numpy as np
from mathutils import Matrix, Vector

FWD, UP, RIGHT = Vector((0, -1, 0)), Vector((0, 0, 1)), Vector((-1, 0, 0))   # the bird faces -Y in Blender
FOLD = (35, 70, 55)       # sweep of Pinion_1/2/3 with the wing folded (WingRules.FoldedSweep)
FRAMES = 32               # one walk cycle = two steps
HIP, KNEE, TARSUS = 22.0, 32.0, 0.8   # degrees of thigh swing, of knee lift, and how much of (thigh + knee) the tarsus gives back to keep the foot level


class Rig:
    def __init__(self, arm, mesh):
        self.arm, self.mesh = arm, mesh
        self.M = arm.matrix_world
        for pb in arm.pose.bones:
            pb.rotation_mode = "QUATERNION"
        self.side = {
            "L": dict(p=["Pinion_L1", "Pinion_L2", "Pinion_L3"], thigh="Thigh_L", shin="Shin_L", tars="Bone_013"),
            "R": dict(p=["Pinion_R1", "Pinion_R2", "Pinion_R3"], thigh="Thigh_R", shin="Shin_R", tars="Bone_018"),
        }
        for d in self.side.values():
            d["swing"] = self.probe(d["p"][0], UP, d["p"][1], 1)   # + : the elbow goes toward the tail (+Y)
        self.pitch = self.probe("Root", RIGHT, "Head", 2, inc=False, end="tail")   # + : the head goes down, the feet swing back
        self.reset()

    def update(self):
        bpy.context.view_layer.update()

    def reset(self):
        for pb in self.arm.pose.bones:
            pb.matrix_basis = Matrix.Identity(4)
        self.update()

    def local_axis(self, name, world_axis):
        b = self.arm.data.bones[name]
        return (b.matrix_local.to_3x3().inverted() @ (self.M.to_3x3().inverted() @ world_axis)).normalized()

    def basis(self, name, *rots):
        """matrix_basis = rotations about world axes written in the bone's own rest frame, like Rest * Q in Unity."""
        m = Matrix.Identity(4)
        for deg, ax in rots:
            m = m @ Matrix.Rotation(math.radians(deg), 4, self.local_axis(name, ax))
        return m

    def pos(self, name, end="head"):
        b = self.arm.pose.bones[name]
        return self.M @ (b.head if end == "head" else b.tail)

    def probe(self, bone, axis, landmark, comp, inc=True, end="head"):
        self.reset()
        a = self.pos(landmark, end)[comp]
        self.arm.pose.bones[bone].matrix_basis = self.basis(bone, (10, axis))
        self.update()
        b = self.pos(landmark, end)[comp]
        self.reset()
        return 1 if (b > a) == inc else -1

    def fold_wings(self):
        for d in self.side.values():
            for bone, sweep in zip(d["p"], FOLD):
                self.arm.pose.bones[bone].matrix_basis = self.basis(bone, (d["swing"] * sweep, UP))

    def lowest(self):
        ev = self.mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
        m = ev.to_mesh()
        a = np.empty(len(m.vertices) * 3)
        m.vertices.foreach_get("co", a)
        ev.to_mesh_clear()
        return float(a.reshape(-1, 3)[:, 2].min())   # the mesh is in world metres, Z up (armature and mesh have no object transform)

    def walk_pose(self, phase, plant=True):
        """Pose for one moment of the cycle (phase in radians); returns the root offset that keeps the lowest foot on Z=0."""
        self.reset()
        self.fold_wings()
        sw, cw = math.sin(phase), math.cos(phase)
        for s, d in self.side.items():
            sgn = 1 if s == "L" else -1
            thigh = sgn * HIP * sw                                         # + = foot back
            knee = KNEE * max(0.0, -sgn * cw)                              # lifts the foot while the leg goes forward
            tars = -TARSUS * (thigh + knee)
            self.arm.pose.bones[d["thigh"]].matrix_basis = self.basis(d["thigh"], (self.pitch * thigh, RIGHT))
            self.arm.pose.bones[d["shin"]].matrix_basis = self.basis(d["shin"], (self.pitch * knee, RIGHT))
            self.arm.pose.bones[d["tars"]].matrix_basis = self.basis(d["tars"], (self.pitch * tars, RIGHT))
        self.update()
        lift = -self.lowest() if plant else 0.0
        root = self.arm.data.bones["Root"]
        loc = root.matrix_local.to_3x3().inverted() @ (self.M.to_3x3().inverted() @ Vector((0, 0, lift)))
        self.arm.pose.bones["Root"].matrix_basis = Matrix.Translation(loc)
        self.update()
        return lift

    def idle_pose(self):
        self.reset()
        self.fold_wings()
        self.update()


KEYED_LEGS = lambda rig: [d[k] for d in rig.side.values() for k in ("thigh", "shin", "tars")]
KEYED_WINGS = lambda rig: [b for d in rig.side.values() for b in d["p"]]


def key_frame(rig, frame, legs, root):
    for name in KEYED_WINGS(rig) + (KEYED_LEGS(rig) if legs else []):
        rig.arm.pose.bones[name].keyframe_insert("rotation_quaternion", frame=frame)
    if root:
        rig.arm.pose.bones["Root"].keyframe_insert("location", frame=frame)


def make_action(rig, name, frames, pose_fn, legs, root):
    arm = rig.arm
    arm.animation_data_create()
    act = bpy.data.actions.new(name)
    arm.animation_data.action = act
    lifts = []
    for f in range(frames + 1):          # the last frame repeats the first so the loop closes
        lifts.append(pose_fn(f))
        key_frame(rig, 1 + f, legs, root)
    act.use_fake_user = True
    return act, lifts


def render_sheet(rig, path_png, phases, res=420):
    """A contact sheet of the walk (side view), for checking by eye."""
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.color_type = "SINGLE"
    sc.display.shading.single_color = (0.75, 0.75, 0.78)
    sc.display.shading.light = "STUDIO"
    sc.render.resolution_x = sc.render.resolution_y = res
    sc.world = bpy.data.worlds.new("W")
    sc.world.color = (0.62, 0.7, 0.8)
    cd = bpy.data.cameras.new("C")
    cam = bpy.data.objects.new("C", cd)
    sc.collection.objects.link(cam)
    sc.camera = cam
    cd.type = "ORTHO"
    cd.ortho_scale = 2.4
    cam.location = (6, 0, 0.85)
    cam.rotation_euler = (math.radians(90), 0, math.radians(90))
    imgs = []
    for i, ph in enumerate(phases):
        rig.walk_pose(ph)
        p = path_png.replace(".png", "_%d.png" % i)
        sc.render.filepath = p
        bpy.ops.render.render(write_still=True)
        im = bpy.data.images.load(p)
        imgs.append(np.array(im.pixels[:]).reshape(res, res, 4))
        bpy.data.images.remove(im)
    cols = 4
    rows = (len(imgs) + cols - 1) // cols
    big = np.ones((rows * res, cols * res, 4))
    for i, a in enumerate(imgs):
        r, c = divmod(i, cols)
        r = rows - 1 - r
        big[r * res:(r + 1) * res, c * res:(c + 1) * res] = a
    out = bpy.data.images.new("sheet", cols * res, rows * res, alpha=True)
    out.pixels = big.ravel().tolist()
    out.filepath_raw = path_png
    out.file_format = "PNG"
    out.save()
    rig.reset()


def export_clips(arm, mesh, dst_fbx, sheet=None):
    rig = Rig(arm, mesh)
    n = FRAMES
    if sheet:
        render_sheet(rig, sheet, [2 * math.pi * i / 8 for i in range(8)])
    walk, lifts = make_action(rig, "Walk", n, lambda f: rig.walk_pose(2 * math.pi * f / n), legs=True, root=True)
    idle, _ = make_action(rig, "Idle", 2, lambda f: rig.idle_pose(), legs=False, root=False)
    print("WALK frames", n, "root lift min/max (m): %.3f %.3f" % (min(lifts), max(lifts)), "pitch sign", rig.pitch, "wing swing", {k: v["swing"] for k, v in rig.side.items()})
    scene = bpy.context.scene
    scene.render.fps = 30
    scene.frame_start, scene.frame_end = 1, n + 1
    # the mesh stays in the clip file: Unity binds a clip to bones by their path from the file root, and the main model has Armature + geometry_0 under its root, so the paths must start with "Armature/" too
    # (an armature-only file gives "Root/..." paths that match nothing: that is why the golem clips never moved its legs)
    bpy.ops.object.select_all(action="DESELECT")
    for o in (arm, mesh):
        o.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(
        filepath=dst_fbx, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", bake_space_transform=True, add_leaf_bones=False, path_mode="STRIP",
        bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0)
    print("OK clips", dst_fbx)
