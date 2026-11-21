"""Auto-skin the Meshy bird boss (see rig_bird_kit.py for the bones): weights by distance to each bone, and a clean split for the
lower beak (the model has a real gap between the beaks).

    python -I tools/blender/rig_bird.py model.glb Assets/_Project/Models/Wildlife/Meshy/Creature_ChimHangMe_Meshy.fbx

Writes the FBX and <name>_albedo.png / <name>_mask.png next to it. A starting point: a hand-made rig replaces it.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # must come before mathutils
import mathutils

import rig_quadruped as rq
from rig_bird_kit import BONES

V = mathutils.Vector
HINGE, BEAK_TIP = V((0, -0.251, 0.27)), V((0, -0.391, 0.17))   # lower beak after the trim (measured, model centred)


def smooth(x):
    x = max(0.0, min(1.0, x))
    return x * x * (3 - 2 * x)


def main(src, dst):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=src)
    mesh_obj = next(o for o in bpy.data.objects if o.type == "MESH")
    me = mesh_obj.data
    ax = BEAK_TIP - HINGE
    blen = ax.length
    ax.normalize()
    beak = {}
    for v in me.vertices:
        d = v.co - HINGE
        t = d.dot(ax)
        if abs(v.co.x) <= 0.1 and 0.05 * blen <= t <= 1.2 * blen and (d - ax * t).length <= 0.045:
            beak[v.index] = smooth((t / blen - 0.08) / 0.3)
    low = min(v.co.z for v in me.vertices)
    me.transform(mathutils.Matrix.Translation((0, 0, -low)))
    rq.export_textures(mesh_obj, os.path.splitext(dst)[0])
    lift = V((0, 0, -low))
    pts = [v.co.copy() for v in me.vertices]

    arm_data = bpy.data.armatures.new("Armature")
    arm = bpy.data.objects.new("Armature", arm_data)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    eb = {}
    for nm, (a, b, _) in BONES.items():
        e = arm_data.edit_bones.new(nm)
        e.head, e.tail = V(a) + lift, V(b) + lift
        eb[nm] = e
    for nm, (_, _, par) in BONES.items():
        if par:
            eb[nm].parent = eb[par]
    bpy.ops.object.mode_set(mode="OBJECT")

    body = [nm for nm in BONES if nm not in ("Beak", "BeakTip")]
    for nm in BONES:
        mesh_obj.vertex_groups.new(name=nm)
    for i, p in enumerate(pts):
        ds = sorted((rq.seg_dist(p, V(BONES[nm][0]) + lift, V(BONES[nm][1]) + lift), nm) for nm in body)[:3]
        ws = [1.0 / (max(d, 0.01) ** 4) for d, _ in ds]
        total = sum(ws)
        weights = {nm: w / total for (d, nm), w in zip(ds, ws)}
        # the wing (outside the body, above the legs) follows its three segments by how far along the span the vertex is, so every feather moves
        ax = abs(p.x)
        if ax > 0.10 and p.z > lift.z - 0.12:
            side = "L" if p.x > 0 else "R"
            u = (ax - 0.14) / 0.81
            s3 = smooth((u - 0.69) / 0.12)
            s2 = smooth((u - 0.32) / 0.12)
            wing = {"Pinion_%s1" % side: 1.0 - s2, "Pinion_%s2" % side: s2 - s3, "Pinion_%s3" % side: s3}
            m = smooth((ax - 0.10) / 0.10)
            weights = {nm: (1 - m) * weights.get(nm, 0.0) + m * wing.get(nm, 0.0) for nm in set(weights) | set(wing)}
        w_beak = beak.get(i, 0.0)
        for nm, w in weights.items():
            if w > 1e-4:
                mesh_obj.vertex_groups[nm].add([i], w * (1.0 - w_beak), "REPLACE")
        if w_beak > 0.0:
            mesh_obj.vertex_groups["Beak"].add([i], w_beak, "REPLACE")
    me.polygons.foreach_set("use_smooth", [True] * len(me.polygons))
    mesh_obj.parent = arm
    mod = mesh_obj.modifiers.new("Armature", "ARMATURE")
    mod.object = arm

    os.makedirs(os.path.dirname(os.path.abspath(dst)), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in (arm, mesh_obj):
        o.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(
        filepath=dst, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE",
        add_leaf_bones=False, path_mode="STRIP", bake_anim=False)
    print("OK", dst, "bones:", ", ".join(BONES), "beak verts:", len(beak))


if __name__ == "__main__":
    main(sys.argv[-2], sys.argv[-1])
