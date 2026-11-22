"""Export a Meshy model with no rig as a Unity-ready FBX (feet on Z=0, smooth shading, separate textures).

    python -I tools/blender/export_static.py model.glb out/Creature_Name

Writes out/Creature_Name.fbx, _albedo.png and _mask.png (see rig_quadruped.export_textures). For a model that will be rigged by hand later,
or to look at the model in the game before it has bones.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # must come before mathutils
import mathutils

import rig_quadruped as rq


def main(src, prefix):
    os.makedirs(os.path.dirname(os.path.abspath(prefix)), exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=src)
    mesh = max((o for o in bpy.data.objects if o.type == "MESH"), key=lambda o: len(o.data.vertices))
    low = min(v.co.z for v in mesh.data.vertices)
    mesh.data.transform(mathutils.Matrix.Translation((0, 0, -low)))
    mesh.data.polygons.foreach_set("use_smooth", [True] * len(mesh.data.polygons))
    rq.export_textures(mesh, prefix)
    bpy.ops.object.select_all(action="DESELECT")
    mesh.select_set(True)
    bpy.context.view_layer.objects.active = mesh
    bpy.ops.export_scene.fbx(
        filepath=prefix + ".fbx", use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE", add_leaf_bones=False,
        path_mode="STRIP", bake_anim=False)
    d = mesh.dimensions
    print("OK", prefix + ".fbx", "size (m): %.2f x %.2f x %.2f" % (d.x, d.y, d.z))


if __name__ == "__main__":
    main(sys.argv[-2], sys.argv[-1])
