"""Skin a mesh to a Rigify cat/dog METARIG used as a plain skeleton (no Generate), name its bones for CreatureLocomotion and export an FBX.

    python -I tools/blender/rig_from_metarig.py file.blend Mesh_0 metarig.001 out/Creature_Name [--clips]

Bone heat ("With Automatic Weights") fails on the dense Meshy meshes, so weights come from the distance to each bone (like rig_quadruped.py).
Renames: spine.004 -> Root, face -> Head, spine.003/.002/.001/spine/spine.012 -> Tail1..5, front_thigh.L/R -> Leg_FL/FR, thigh.L/R -> Leg_BL/BR
(each leg keeps only its first bone named Leg_*, lower bones become Shin_*/Foot_*/Toe_*, because CreatureLocomotion treats every Leg* bone as a leg).
Textures are not written: reuse the model's <name>_albedo.png and _mask.png.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # must come before mathutils
import mathutils

import rig_quadruped as rq

V = mathutils.Vector
RENAME = {
    "spine.004": "Root", "face": "Head", "spine.003": "Tail1", "spine.002": "Tail2", "spine.001": "Tail3", "spine": "Tail4", "spine.012": "Tail5",
    "front_thigh.L": "Leg_FL", "front_shin.L": "Shin_FL", "front_foot.L": "Foot_FL",
    "front_thigh.R": "Leg_FR", "front_shin.R": "Shin_FR", "front_foot.R": "Foot_FR",
    "thigh.L": "Leg_BL", "shin.L": "Shin_BL", "foot.L": "Foot_BL", "toe.L": "Toe_BL",
    "thigh.R": "Leg_BR", "shin.R": "Shin_BR", "foot.R": "Foot_BR", "toe.R": "Toe_BR",
}


def keep(n):
    return n.startswith(("spine", "shoulder", "front_", "pelvis", "thigh", "shin", "foot", "toe")) or n in ("face", "jaw")


def main(blend, mesh_name, arm_name, prefix, clips=False, blend_only=False):
    bpy.ops.wm.open_mainfile(filepath=blend)
    mesh = bpy.data.objects[mesh_name]
    arm = bpy.data.objects[arm_name]
    for o in list(bpy.data.objects):
        if o not in (mesh, arm):
            bpy.data.objects.remove(o)
    for b in arm.data.bones:
        b.use_deform = keep(b.name)
    for o in bpy.data.objects:
        o.select_set(False)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    for b in list(arm.data.edit_bones):
        if not keep(b.name):
            arm.data.edit_bones.remove(b)
    bpy.ops.object.mode_set(mode="OBJECT")
    segs = {}
    for b in arm.data.bones:
        segs[b.name] = (arm.matrix_world @ b.head_local, arm.matrix_world @ b.tail_local)
    pts = [mesh.matrix_world @ v.co for v in mesh.data.vertices]
    for nm in segs:
        mesh.vertex_groups.new(name=RENAME.get(nm, nm))
    # a leg bone only moves the leg: nothing farther than LEG_REACH from it, and nothing above its hip joint (that is the ribs and the back)
    LEG_REACH = 0.18
    leg_names = [n for n in segs if n.startswith(("front_", "thigh", "shin", "foot", "toe"))]
    ceiling = {"front_": max((segs[n][0].z for n in leg_names if n.startswith("front_thigh")), default=9e9) - 0.03,
               "rear": max((segs[n][0].z for n in leg_names if n.startswith("thigh")), default=9e9) - 0.03}

    def cap(p, nm, d):
        if nm not in leg_names:
            return d
        top = ceiling["front_"] if nm.startswith("front_") else ceiling["rear"]
        return 1e3 if d > LEG_REACH or p.z > top else d

    for i, p in enumerate(pts):
        ds = sorted((cap(p, nm, rq.seg_dist(p, a, b)), nm) for nm, (a, b) in segs.items())[:3]
        ds = [(d, nm) for d, nm in ds if d < 1e3] or sorted((rq.seg_dist(p, a, b), nm) for nm, (a, b) in segs.items() if nm not in leg_names)[:3]
        ws = [1.0 / (max(d, 0.01) ** 4) for d, _ in ds]
        total = sum(ws)
        for (d, nm), w in zip(ds, ws):
            mesh.vertex_groups[RENAME.get(nm, nm)].add([i], w / total, "REPLACE")
    bpy.ops.object.mode_set(mode="EDIT")
    for nm, new in RENAME.items():
        if nm in arm.data.edit_bones:
            arm.data.edit_bones[nm].name = new
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    jw = arm.data.edit_bones["jaw"]
    tip = arm.data.edit_bones.new("JawTip")   # a tiny leaf: Unity measures which way the jaw opens from it
    tip.head = jw.tail
    tip.tail = jw.tail + (jw.tail - jw.head).normalized() * 0.02
    tip.parent = jw
    tip.use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    mesh.parent = arm
    mesh.matrix_parent_inverse = arm.matrix_world.inverted()   # the armature object is offset: keep the mesh where it was
    mod = mesh.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    low = min(p.z for p in pts)
    arm.location.z -= low   # feet on Z=0; the mesh follows its parent
    mesh.data.polygons.foreach_set("use_smooth", [True] * len(mesh.data.polygons))
    action_names = []
    if clips:
        import author_cho_clips
        action_names = author_cho_clips.author_clips(arm)
    os.makedirs(os.path.dirname(os.path.abspath(prefix)), exist_ok=True)
    if blend_only:   # keep the skinned, animated rig as a .blend (to preview the clips) instead of exporting
        bpy.ops.wm.save_as_mainfile(filepath=prefix + ".blend")
        print("OK blend", prefix + ".blend", "actions:", action_names)
        return
    for o in bpy.data.objects:
        o.select_set(o in (arm, mesh))
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(
        filepath=prefix + ".fbx", use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE", add_leaf_bones=False,
        path_mode="STRIP", bake_anim=bool(action_names), bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0.0,
        use_armature_deform_only=True)
    print("OK", prefix + ".fbx", "bones:", ", ".join(b.name for b in arm.data.bones))


if __name__ == "__main__":
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    main(*args[-4:], clips="--clips" in sys.argv, blend_only="--blend-only" in sys.argv)
