"""Shared helpers for the Underroot props kit (Blender bpy, headless). Units are metres."""
import math
import os

import bpy  # must come before bmesh/mathutils
import bmesh
import mathutils


def new_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0


def tri_count(obj):
    mesh = obj.data
    mesh.calc_loop_triangles()
    return len(mesh.loop_triangles)


def link(obj):
    bpy.context.scene.collection.objects.link(obj)
    return obj


def make_collider(obj, target_tris):
    """Duplicate obj, decimate to about target_tris and name it <obj.name>_COL."""
    col = obj.copy()
    col.data = obj.data.copy()
    col.name = obj.name + "_COL"
    link(col)
    count = max(1, tri_count(col))
    if count > target_tris:
        mod = col.modifiers.new("decimate", "DECIMATE")
        mod.ratio = max(0.01, target_tris / count)
        bpy.context.view_layer.objects.active = col
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if len(col.data.color_attributes):
        for attr in list(col.data.color_attributes):
            col.data.color_attributes.remove(attr)
    return col


def vertex_color_material(name="Rock_Mat", attribute="Col", roughness=0.9):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Roughness"].default_value = roughness
    col = nodes.new("ShaderNodeVertexColor")
    col.layer_name = attribute
    links.new(col.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def bounds(obj):
    xs = [v.co.x for v in obj.data.vertices]
    ys = [v.co.y for v in obj.data.vertices]
    zs = [v.co.z for v in obj.data.vertices]
    return (max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs))


def export_fbx(objects, path):
    """Export objects to FBX: 1 unit = 1 m, Y up when imported by Unity."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
    )


def render_preview(objects, path, angle):
    """Render objects (visible mesh only) from 'front', 'side' or 'top' with a simple sun and ground."""
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 24
    scene.render.resolution_x, scene.render.resolution_y = 800, 600
    scene.render.filepath = path
    scene.render.image_settings.file_format = "PNG"
    world = bpy.data.worlds.new("w")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.04, 0.07, 0.06, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.8
    scene.world = world
    for o in list(scene.collection.objects):
        if o.type in {"CAMERA", "LIGHT"} or o.name.startswith("_preview"):
            bpy.data.objects.remove(o, do_unlink=True)
    size = max(max(bounds(o)) for o in objects if not o.name.endswith("_COL"))
    ground = bpy.data.meshes.new("_preview_ground")
    gobj = bpy.data.objects.new("_preview_ground", ground)
    link(gobj)
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=size * 3)
    bm.to_mesh(ground)
    bm.free()
    sun = bpy.data.objects.new("_preview_sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy = 3.0
    sun.rotation_euler = (math.radians(50), math.radians(10), math.radians(35))
    link(sun)
    cam = bpy.data.objects.new("_preview_cam", bpy.data.cameras.new("cam"))
    link(cam)
    d = size * 3.2
    pos = {"front": (0, -d, size * 0.5), "side": (d, 0, size * 0.5), "top": (0, 0.001, d * 1.1)}[angle]
    cam.location = pos
    direction = -mathutils.Vector(pos) + mathutils.Vector((0, 0, size * 0.3))
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam
    for o in objects:
        o.hide_render = o.name.endswith("_COL")
    bpy.ops.render.render(write_still=True)
