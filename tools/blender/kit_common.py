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


def render_preview(objects, path, angle, distance=3.2):
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
    d = size * distance
    pos = {"front": (0, -d, size * 0.5), "side": (d, 0, size * 0.5), "top": (0, 0.001, d * 1.1)}[angle]
    cam.location = pos
    direction = -mathutils.Vector(pos) + mathutils.Vector((0, 0, size * 0.3))
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam
    for o in objects:
        o.hide_render = o.name.endswith("_COL")
    bpy.ops.render.render(write_still=True)


# ---------------------------------------------------------------- shared geometry helpers (landmarks)

def ring_frame(path, i):
    """Return (tangent, normal, binormal) for point i of a polyline path."""
    a = path[max(0, i - 1)]
    b = path[min(len(path) - 1, i + 1)]
    t = (b - a).normalized()
    up = mathutils.Vector((0, 0, 1))
    if abs(t.dot(up)) > 0.95:
        up = mathutils.Vector((1, 0, 0))
    n = t.cross(up).normalized()
    bn = t.cross(n).normalized()
    return t, n, bn


def tube_rings(bm, path, radii, segs, wobble=None):
    """Add rings of vertices around the path; returns list of rings (each a list of BMVert)."""
    rings = []
    for i, p in enumerate(path):
        _, n, bn = ring_frame(path, i)
        ring = []
        for k in range(segs):
            a = 2 * math.pi * k / segs
            r = radii[i] * (1.0 + (wobble(p, a) if wobble else 0.0))
            ring.append(bm.verts.new(p + (n * math.cos(a) + bn * math.sin(a)) * r))
        rings.append(ring)
    return rings


def bridge_rings(bm, rings, flip=False):
    for i in range(len(rings) - 1):
        a, b = rings[i], rings[i + 1]
        n = len(a)
        for k in range(n):
            quad = [a[k], a[(k + 1) % n], b[(k + 1) % n], b[k]]
            bm.faces.new(quad[::-1] if flip else quad)


def cap_ring(bm, ring, flip=False):
    bm.faces.new(ring[::-1] if flip else ring)


def finish_object(bm, name, material=None):
    """bmesh -> mesh object linked to the scene, with smooth shading and recalculated normals."""
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for p in mesh.polygons:
        p.use_smooth = True
    obj = bpy.data.objects.new(name, mesh)
    link(obj)
    if material is not None:
        mesh.materials.append(material)
    return obj


def paint(obj, fn, attribute="Col"):
    """Vertex colours: fn(position, normal) -> (r, g, b) linear."""
    mesh = obj.data
    mesh.calc_loop_triangles()
    attr = mesh.color_attributes.new(attribute, "BYTE_COLOR", "CORNER")
    for poly in mesh.polygons:
        for li in poly.loop_indices:
            v = mesh.vertices[mesh.loops[li].vertex_index]
            c = fn(v.co, v.normal)
            attr.data[li].color = (c[0], c[1], c[2], 1.0)


def collider_from_bm(bm, name):
    return finish_object(bm, name)


def ray_hits(obj, origin, direction, distance=1000.0):
    """True if a ray (object space = world here) hits the mesh."""
    hit, _, _, _ = obj.ray_cast(origin, direction, distance=distance)
    return hit
