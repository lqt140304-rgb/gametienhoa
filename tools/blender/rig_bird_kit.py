"""Hand-rigging kit for the Meshy Chim Hang Mẹ (vulture with spread wings, facing -Y):

    python -I tools/blender/rig_bird_kit.py model.glb out.blend

Lifts the model so its feet are on Z=0, adds bones named for CreatureLocomotion (Root, Head, Beak = lower beak, Pinion_L1/L2, Pinion_R1/R2,
Leg_L/R, Tail1). The wings are called Pinion, NOT Wing: CreatureLocomotion treats any Wing* bone as a flier and skips the leg steps at positions measured on that model, smooth-shades it and saves a .blend with textures packed. The mesh is NOT skinned:
adjust the bones in Edit Mode, then Ctrl+P > With Automatic Weights (or Envelope). Left is +X.
"""
import sys

import bpy  # must come before mathutils
import mathutils

V = mathutils.Vector
# (head, tail, parent) measured with feet at Z=-0.473 (model centred), converted below
BONES = {
    "Root": ((0, 0.25, -0.18), (0, -0.05, 0.05), None),
    "Head": ((0, -0.10, 0.20), (0, -0.28, 0.36), "Root"),
    "Beak": ((0, -0.251, 0.27), (0, -0.39, 0.17), "Head"),
    "BeakTip": ((0, -0.39, 0.17), (0, -0.41, 0.16), "Beak"),   # Unity measures which way the beak opens from this one
    "Pinion_L1": ((0.14, 0.08, 0.34), (0.55, 0.05, 0.30), "Root"),
    "Pinion_L2": ((0.55, 0.05, 0.30), (0.95, -0.03, 0.20), "Pinion_L1"),
    "Pinion_R1": ((-0.14, 0.08, 0.34), (-0.55, 0.05, 0.30), "Root"),
    "Pinion_R2": ((-0.55, 0.05, 0.30), (-0.95, -0.03, 0.20), "Pinion_R1"),
    "Leg_L": ((0.12, 0.0, -0.24), (0.12, -0.01, -0.47), "Root"),
    "Leg_R": ((-0.12, 0.0, -0.24), (-0.12, -0.01, -0.47), "Root"),
    "Tail1": ((0, 0.28, -0.30), (0, 0.43, -0.46), "Root"),
}


def main(src, dst):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=src)
    mesh_obj = next(o for o in bpy.data.objects if o.type == "MESH")
    low = min(v.co.z for v in mesh_obj.data.vertices)
    mesh_obj.data.transform(mathutils.Matrix.Translation((0, 0, -low)))
    mesh_obj.data.polygons.foreach_set("use_smooth", [True] * len(mesh_obj.data.polygons))
    lift = -low - 0.473   # the bone numbers above were measured at feet Z=-0.473
    arm_data = bpy.data.armatures.new("Armature")
    arm = bpy.data.objects.new("Armature", arm_data)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    eb = {}
    for nm, (a, b, _) in BONES.items():
        e = arm_data.edit_bones.new(nm)
        e.head, e.tail = V(a) + V((0, 0, 0.473 + lift)), V(b) + V((0, 0, 0.473 + lift))
        eb[nm] = e
    for nm, (_, _, par) in BONES.items():
        if par:
            eb[nm].parent = eb[par]
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=dst, compress=True)
    print("OK kit", dst, "bones:", ", ".join(BONES))


if __name__ == "__main__":
    main(sys.argv[-2], sys.argv[-1])
