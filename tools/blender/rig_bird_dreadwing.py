"""Turn Meshy's auto-rigged Dreadwing Carrion (the glb from /rigging, bones Bone_000..073) into Unity files for BirdRigFx, keeping Meshy's skin weights:

    "D:/blender/blender.exe" -b -P tools/blender/rig_bird_dreadwing.py -- model.glb Assets/_Project/Models/Wildlife/Meshy/Creature_ChimHangMe_Dreadwing.fbx

Only the bones the code looks for are renamed (the rest keep their Bone_nnn names and follow their parents):
  Root (pelvis), Head (neck), Tail1, Pinion_L1/L2/L3 + Pinion_R1/R2/R3 (collar, shoulder, elbow; L is +X), WingTip_L/R (the hand end, where the wind blades start), Thigh_L/R + Shin_L/R (hip and knee).
The legs are NOT called Leg_*: CreatureLocomotion would swing them about a local axis that does not match this rig; BirdRigFx walks and tucks them with measured axes instead.
The model stands upright facing -Y with its feet on Z=0 (the glb already does). There is no jaw bone, so the beak does not open.
Also writes <fbx>_albedo.png / _mask.png (see rig_quadruped.export_textures) and <fbx>_Walk.fbx with the Idle and Walk clips (bird_walk_clip.py).
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # must come before mathutils

import rig_quadruped as rq

RENAME = {
    "Bone_000": "Root",
    "Bone_024": "Head",
    "Bone_007": "Tail1",
    "Bone_035": "Pinion_L1", "Bone_034": "Pinion_L2", "Bone_033": "Pinion_L3", "Bone_031": "WingTip_L",
    "Bone_030": "Pinion_R1", "Bone_029": "Pinion_R2", "Bone_028": "Pinion_R3", "Bone_026": "WingTip_R",
    "Bone_015": "Thigh_L", "Bone_014": "Shin_L",
    "Bone_020": "Thigh_R", "Bone_019": "Shin_R",
}


def main(src, dst):
    os.makedirs(os.path.dirname(os.path.abspath(dst)), exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=src)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    mesh = max(meshes, key=lambda o: len(o.data.vertices))
    for o in meshes:
        if o is not mesh:
            bpy.data.objects.remove(o)   # Meshy sometimes adds a helper sphere
    arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    low = min(v.co.z for v in mesh.data.vertices)
    assert abs(low) < 0.02, "the model is expected to stand on Z=0, lowest point is %.3f" % low
    mesh.data.polygons.foreach_set("use_smooth", [True] * len(mesh.data.polygons))
    rq.export_textures(mesh, os.path.splitext(dst)[0])
    for old, new in RENAME.items():
        arm.data.bones[old].name = new   # Blender renames the matching vertex group with it
    for old, new in RENAME.items():      # (and if it did not, do it here)
        vg = mesh.vertex_groups.get(old)
        if vg:
            vg.name = new
    bones = {b.name for b in arm.data.bones}
    stray = [vg.name for vg in mesh.vertex_groups if vg.name not in bones]
    assert not stray, "vertex groups without a bone: %s" % stray
    arm.name = "Armature"
    bpy.ops.object.select_all(action="DESELECT")
    for o in (arm, mesh):
        o.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(
        filepath=dst, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE", add_leaf_bones=False,
        path_mode="STRIP", bake_anim=False)
    print("OK", dst, "bones:", len(bones), "renamed:", ", ".join(RENAME.values()))
    import bird_walk_clip   # the walk and idle clips: <name>_Walk.fbx next to the model (armature + two baked actions)
    sheet = os.environ.get("BIRD_WALK_SHEET")   # optional: a PNG contact sheet of the walk, to check it by eye
    bird_walk_clip.export_clips(arm, mesh, os.path.splitext(dst)[0] + "_Walk.fbx", sheet)


if __name__ == "__main__":
    main(*sys.argv[sys.argv.index("--") + 1:][:2])
