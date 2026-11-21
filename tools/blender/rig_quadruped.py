"""Rig an AI-generated four-legged model (glTF/GLB) with the bone names CreatureLocomotion looks for.

    python -I tools/blender/rig_quadruped.py in.glb out.fbx

Bone names: Root, Head, Tail1, Tail2, Leg_FL, Leg_FR, Leg_BL, Leg_BR. The model must face -Y in Blender
(the Meshy fox does); it is lifted so its feet sit on Z=0. Also writes <out>_albedo/_normal/_mask.png. Bone positions come from the mesh itself, weights from the
distance to each bone (no bone-heat: dense fur meshes make it fail).
"""
import os
import sys

import bpy  # must come before bmesh/mathutils
import mathutils

V = mathutils.Vector


def seg_dist(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / max(ab.length_squared, 1e-9)))
    return (p - (a + ab * t)).length


def export_textures(obj, prefix, size=1024):
    """Write <prefix>_albedo/_normal/_mask.png (mask = Unity URP: R metallic, A smoothness) and strip the material to plain white,
    so Unity gets clean external textures (Meshy's glTF also carries an emission image that turns the model white)."""
    import numpy as np
    mat = obj.active_material
    imgs = {n.name: n.image for n in mat.node_tree.nodes if n.type == "TEX_IMAGE"}
    links = {(l.to_node.name, l.to_socket.name): l.from_node for l in mat.node_tree.links}
    bsdf = next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    base = links[("Principled BSDF", "Base Color")].image
    mr = links[("Separate Color", "Color")].image
    nrm = links[("Normal Map", "Color")].image

    def px(im):
        im = im.copy()
        im.scale(size, size)
        return np.array(im.pixels[:], dtype=np.float32).reshape(size, size, 4)

    def save(arr, path):
        im = bpy.data.images.new(os.path.basename(path), size, size, alpha=True)
        im.pixels = arr.ravel().tolist()
        im.filepath_raw = path
        im.file_format = "PNG"
        im.save()

    b = px(base)
    b[..., 3] = 1.0
    save(b, prefix + "_albedo.png")
    save(px(nrm), prefix + "_normal.png")
    m = px(mr)
    mask = np.zeros_like(m)
    mask[..., 0] = m[..., 2]          # metallic = blue
    mask[..., 3] = 1.0 - m[..., 1]    # smoothness = 1 - roughness (green)
    mask[..., 1] = mask[..., 2] = 0.0
    save(mask, prefix + "_mask.png")
    mat.node_tree.nodes.clear()
    out = mat.node_tree.nodes.new("ShaderNodeOutputMaterial")
    bs = mat.node_tree.nodes.new("ShaderNodeBsdfPrincipled")
    mat.node_tree.links.new(bs.outputs["BSDF"], out.inputs["Surface"])
    return prefix


def main(src, dst):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=src)
    mesh_obj = next(o for o in bpy.data.objects if o.type == "MESH")
    low = min(v.co.z for v in mesh_obj.data.vertices)
    mesh_obj.data.transform(mathutils.Matrix.Translation((0, 0, -low)))   # AI models are centred: put the feet on Z=0
    textures = export_textures(mesh_obj, os.path.splitext(dst)[0])
    vs = [v.co.copy() for v in mesh_obj.data.vertices]
    zs = [v.z for v in vs]
    zmin, zmax = min(zs), max(zs)
    ys = [v.y for v in vs]
    ymin, ymax = min(ys), max(ys)
    h = zmax - zmin
    foot_z = zmin + 0.02 * h
    feet = [v for v in vs if v.z < zmin + 0.06 * h]

    mid_y = 0.0  # fox is centred: front feet at y<mid, hind at y>mid (checked on the Meshy fox)
    sel_front = lambda v: v.y < mid_y
    def foot_at(front, right):
        sel = [v for v in feet if sel_front(v) == front and (v.x < 0) == right]
        if not sel:
            raise SystemExit("no feet found for front=%s right=%s: check the model faces -Y" % (front, right))
        return V((sum(v.x for v in sel) / len(sel), sum(v.y for v in sel) / len(sel), foot_z))

    hip_z = zmin + 0.42 * h
    top_pts = sorted(vs, key=lambda v: -v.z)[: max(10, len(vs) // 100)]
    head_y = sum(v.y for v in top_pts) / len(top_pts)           # ears
    bones = {}  # name -> (head, tail, parent)
    bones["Root"] = (V((0, ymax * 0.45, hip_z)), V((0, ymin * 0.25, hip_z + 0.05 * h)), None)
    bones["Head"] = (V((0, ymin * 0.45, zmin + 0.62 * h)), V((0, head_y, zmax - 0.05 * h)), "Root")
    bones["Tail1"] = (V((0, ymax * 0.5, hip_z)), V((0, ymax * 0.78, hip_z - 0.05 * h)), "Root")
    bones["Tail2"] = (V((0, ymax * 0.78, hip_z - 0.05 * h)), V((0, ymax, hip_z - 0.15 * h)), "Tail1")
    for nm, front, right in (("Leg_FL", True, False), ("Leg_FR", True, True), ("Leg_BL", False, False), ("Leg_BR", False, True)):
        f = foot_at(front, right)
        bones[nm] = (V((f.x, f.y, hip_z)), f, "Root")

    arm_data = bpy.data.armatures.new("Armature")
    arm = bpy.data.objects.new("Armature", arm_data)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    eb = {}
    for nm, (a, b, par) in bones.items():
        e = arm_data.edit_bones.new(nm)
        e.head, e.tail = a, b
        eb[nm] = e
    for nm, (_, _, par) in bones.items():
        if par:
            eb[nm].parent = eb[par]
    bpy.ops.object.mode_set(mode="OBJECT")

    # weights: nearest 3 bones, falling off with distance^4
    for nm in bones:
        mesh_obj.vertex_groups.new(name=nm)
    for i, p in enumerate(vs):
        ds = sorted(((seg_dist(p, a, b), nm) for nm, (a, b, _) in bones.items()))[:3]
        ws = [1.0 / (max(d, 0.01) ** 4) for d, _ in ds]
        total = sum(ws)
        for (d, nm), w in zip(ds, ws):
            mesh_obj.vertex_groups[nm].add([i], w / total, "REPLACE")
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
    print("OK", dst, "bones:", ", ".join(bones))


if __name__ == "__main__":
    main(sys.argv[-2], sys.argv[-1])
