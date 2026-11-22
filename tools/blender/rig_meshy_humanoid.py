"""Turn a Meshy auto-rigged humanoid (the glb from /rigging) into Unity-friendly files, keeping Meshy's bones and its two clips:

    python -I tools/blender/rig_meshy_humanoid.py rigged.glb walking_armature.glb running_armature.glb out/Creature_Name

Writes out/Creature_Name.fbx (mesh + armature, no animation), out/Creature_Name_albedo.png and _mask.png (separate textures, see
rig_quadruped.export_textures), out/Creature_Name_Walk.fbx and _Run.fbx (armature + one baked clip each, tiny). Needs the Meshy rig's
bone names as they come (Hips, Spine02, Head, LeftArm, ...): Unity plays the clips by bone path, so keep every file from the same rig.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # must come before mathutils

import rig_quadruped as rq


def export_fbx(path, objs, anim):
    if anim:   # the baker walks the scene range (1-250 by default), not the clip's: fit it to the clip
        act = objs[0].animation_data.action
        bpy.context.scene.frame_start, bpy.context.scene.frame_end = int(act.frame_range[0]), int(round(act.frame_range[1]))
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE", add_leaf_bones=False,
        path_mode="STRIP", bake_anim=anim, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
        bake_anim_simplify_factor=0.0)


def main(rigged, walk, run, prefix):
    os.makedirs(os.path.dirname(os.path.abspath(prefix)), exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=rigged)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    mesh = max(meshes, key=lambda o: len(o.data.vertices))
    for o in meshes:
        if o is not mesh:
            bpy.data.objects.remove(o)   # Meshy adds a helper sphere to the glb
    arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    mesh.data.polygons.foreach_set("use_smooth", [True] * len(mesh.data.polygons))
    rq.export_textures(mesh, prefix)
    export_fbx(prefix + ".fbx", [arm, mesh], False)
    print("OK model", prefix + ".fbx", "bones:", len(arm.data.bones))
    for src, suffix in ((walk, "_Walk"), (run, "_Run")):
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.gltf(filepath=src)
        a = next(o for o in bpy.data.objects if o.type == "ARMATURE")
        for o in list(bpy.data.objects):
            if o.type != "ARMATURE":
                bpy.data.objects.remove(o)
        act = a.animation_data.action if a.animation_data else None
        print("OK clip", prefix + suffix + ".fbx", "frames:", act and tuple(round(x) for x in act.frame_range))
        export_fbx(prefix + suffix + ".fbx", [a], True)


if __name__ == "__main__":
    main(*sys.argv[-4:])
