"""Builds the giant axe of Golem Thạch Anh (a placeholder made of simple shapes, vertex-coloured) and exports two FBX files.

    python -I tools/blender/build_golem_axe.py out/GolemAxe  [preview.png]

Writes out/GolemAxe.fbx (upright, the way the golem holds it: handle along +Y in Unity, butt at the origin, empties Grip, Grip2 and Embed_Point) and
out/GolemAxe_Planted.fbx (the way it stands driven into the heart stone: Embed_Point, the middle of the cutting edge, is the origin, the edge points down and
forward, the handle leans out about 50 degrees). Two meshes in each: Axe_Body (steel, rust, the handle) and Axe_Crystals (amber crystals grown onto it);
colours are vertex colours. Size: 3.0 m long, the head 1.1 m across.
"""
import math
import os
import random
import sys

import bpy  # must come before bmesh and mathutils
import bmesh
from mathutils import Matrix, Vector

random.seed(7)
STEEL, EDGE, RUST, WOOD, AMBER, AMBER_TIP = (0.2, 0.21, 0.23), (0.55, 0.56, 0.58), (0.36, 0.17, 0.09), (0.2, 0.17, 0.15), (1.0, 0.66, 0.24), (1.0, 0.9, 0.55)


def paint(bm, fn):
    layer = bm.loops.layers.color.get("Col") or bm.loops.layers.color.new("Col")
    for f in bm.faces:
        for l in f.loops:
            c = fn(l.vert.co)
            l[layer] = (c[0], c[1], c[2], 1.0)


def mesh_object(name, bm):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = False
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def cylinder(bm, z0, z1, r0, r1, sides=14):
    ring0 = [bm.verts.new((r0 * math.cos(a), r0 * math.sin(a), z0)) for a in [2 * math.pi * i / sides for i in range(sides)]]
    ring1 = [bm.verts.new((r1 * math.cos(a), r1 * math.sin(a), z1)) for a in [2 * math.pi * i / sides for i in range(sides)]]
    for i in range(sides):
        j = (i + 1) % sides
        bm.faces.new((ring0[i], ring0[j], ring1[j], ring1[i]))
    bm.faces.new(ring0[::-1]); bm.faces.new(ring1)


def box(bm, x0, x1, y0, y1, z0, z1):
    v = [bm.verts.new(p) for p in [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]]
    for q in [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]:
        bm.faces.new([v[i] for i in q])


def blade(bm):
    outline = [(0.10, 2.80), (0.55, 3.12), (0.98, 3.22), (1.05, 2.96), (1.08, 2.72), (1.00, 2.67), (1.08, 2.62), (1.05, 2.25), (0.96, 1.95), (0.55, 2.04), (0.10, 2.10)]
    def half(x): return 0.012 + 0.055 * max(0.0, min(1.0, (1.08 - x) / 0.95))
    top = [bm.verts.new((x, half(x), z)) for x, z in outline]
    bot = [bm.verts.new((x, -half(x), z)) for x, z in outline]
    bm.faces.new(top[::-1]); bm.faces.new(bot)
    n = len(outline)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((top[i], top[j], bot[j], bot[i]))


def spike(bm, base_x, tip_x, z):
    a = [bm.verts.new(p) for p in [(base_x, -0.08, z - 0.1), (base_x, 0.08, z - 0.1), (base_x, 0.08, z + 0.1), (base_x, -0.08, z + 0.1)]]
    t = bm.verts.new((tip_x, 0, z))
    bm.faces.new(a[::-1])
    for i in range(4): bm.faces.new((a[i], a[(i + 1) % 4], t))


def crystal(bm, base, direction, length, radius, sides=6):
    d = Vector(direction).normalized()
    up = Vector((0, 0, 1)) if abs(d.z) < 0.9 else Vector((1, 0, 0))
    u = d.cross(up).normalized(); v = d.cross(u).normalized()
    b = Vector(base)
    r0 = [bm.verts.new(b + (u * math.cos(a) + v * math.sin(a)) * radius) for a in [2 * math.pi * i / sides for i in range(sides)]]
    r1 = [bm.verts.new(b + d * length * 0.72 + (u * math.cos(a) + v * math.sin(a)) * radius * 0.8) for a in [2 * math.pi * i / sides for i in range(sides)]]
    tip = bm.verts.new(b + d * length)
    bm.faces.new(r0[::-1])
    for i in range(sides):
        j = (i + 1) % sides
        bm.faces.new((r0[i], r0[j], r1[j], r1[i])); bm.faces.new((r1[i], r1[j], tip))


def build():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    body = bmesh.new()
    cylinder(body, 0.0, 2.85, 0.065, 0.076)                                        # the handle
    for z in (0.5, 1.1, 1.7, 2.25): cylinder(body, z, z + 0.07, 0.085, 0.085)       # rusty bands
    box(body, -0.13, 0.13, -0.11, 0.11, 2.05, 2.85)                                  # the collar of the head
    blade(body); spike(body, -0.13, -0.58, 2.45)
    def col(p):
        if p.z < 2.0 or (p.x * p.x + p.y * p.y < 0.02 and 2.0 <= p.z <= 2.9 and False): pass
        r = random.random() * 0.06
        if p.x > 0.14 and p.z > 1.9:                                                 # the blade: lighter toward the edge, rust blotches
            e = max(0.0, min(1.0, (p.x - 0.5) / 0.55)); base = tuple(STEEL[i] * (1 - e) + EDGE[i] * e for i in range(3))
            if (math.sin(p.z * 13) + math.sin(p.x * 17 + 1)) > 1.5: base = RUST
            return tuple(min(1, base[i] + r) for i in range(3))
        if abs(p.x) <= 0.14 and 2.05 <= p.z <= 2.86: return STEEL
        if p.x < -0.14: return STEEL
        for z in (0.5, 1.1, 1.7, 2.25):
            if z - 0.001 <= p.z <= z + 0.071 and (p.x * p.x + p.y * p.y) > 0.0064: return RUST
        return tuple(WOOD[i] + r for i in range(3))
    paint(body, col)
    crys = bmesh.new()
    for z, ang in [(0.35, 20), (0.7, 140), (1.0, 260), (1.35, 60), (1.75, 190), (2.0, 310)]:     # crystals grown onto the handle
        a = math.radians(ang); d = (math.cos(a) * 0.8, math.sin(a) * 0.8, 0.5)
        crystal(crys, (0.07 * math.cos(a), 0.07 * math.sin(a), z), d, random.uniform(0.18, 0.4), random.uniform(0.03, 0.05))
    for p, d in [((0.14, 0.1, 2.85), (0.3, 0.4, 1)), ((0.12, -0.1, 2.1), (0.2, -0.5, -0.8)), ((0.9, 0.04, 3.2), (0.2, 0.3, 1))]:   # and onto the head
        crystal(crys, p, d, random.uniform(0.15, 0.3), 0.04)
    paint(crys, lambda p: AMBER)
    ob_body = mesh_object("Axe_Body", body); ob_cr = mesh_object("Axe_Crystals", crys)
    for ob, mname, em in ((ob_body, "Axe_Steel", 0.0), (ob_cr, "Axe_Crystal", 1.0)):
        m = bpy.data.materials.new(mname); m.use_nodes = True
        bsdf = m.node_tree.nodes["Principled BSDF"]
        attr = m.node_tree.nodes.new("ShaderNodeVertexColor"); attr.layer_name = "Col"
        m.node_tree.links.new(attr.outputs["Color"], bsdf.inputs["Base Color"])
        bsdf.inputs["Roughness"].default_value = 0.55 if em == 0 else 0.2
        bsdf.inputs["Metallic"].default_value = 0.6 if em == 0 else 0.0
        if em: m.node_tree.links.new(attr.outputs["Color"], bsdf.inputs["Emission Color"]); bsdf.inputs["Emission Strength"].default_value = 2.5
        ob.data.materials.append(m)
    empties = {}
    for n, p in (("Grip", (0, 0, 0.8)), ("Grip2", (0, 0, 1.5)), ("Embed_Point", (1.08, 0, 2.62))):
        e = bpy.data.objects.new(n, None); e.empty_display_type = "ARROWS"; e.location = p
        bpy.context.scene.collection.objects.link(e); empties[n] = e
    return [ob_body, ob_cr] + list(empties.values())


def to_planted(objs):
    g = Vector((-math.sin(math.radians(50)), 0, math.cos(math.radians(50))))     # from the head to the butt: up, leaning back 50 degrees from the vertical
    e = Vector((-g.z, 0, -abs(g.x)))   # the cutting edge: square to the handle, pointing down and a little back
    R = Matrix(((e.x, 0, -g.x), (0, 1, 0), (e.z, 0, -g.z))).to_4x4()             # columns: X -> e, Y -> Y, Z -> -g (the butt end is where the handle comes out of the stone)
    embed = Vector((1.08, 0, 2.62))
    T = Matrix.Translation(-(R @ embed)) @ R
    for ob in objs:
        if ob.type == "MESH": ob.data.transform(T)
        else: ob.location = (T @ ob.location.to_4d()).to_3d()


def export(path, objs):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, mesh_smooth_type="FACE", add_leaf_bones=False, path_mode="STRIP", bake_anim=False, colors_type="LINEAR")


def preview(path, objs):
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"; sc.cycles.device = "CPU"; sc.cycles.samples = 24
    sc.render.resolution_x, sc.render.resolution_y = 700, 900
    w = bpy.data.worlds.new("w"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs["Color"].default_value = (0.05, 0.05, 0.06, 1); sc.world = w
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam)
    cam.data.type = "ORTHO"; cam.data.ortho_scale = 4.2; cam.location = (4, -6, 3.0)
    cam.rotation_euler = (Vector((0.15, 0, 1.5)) - cam.location).to_track_quat("-Z", "Y").to_euler(); sc.camera = cam
    for loc, en in (((3, -4, 6), 600), ((-4, -2, 2), 250)):
        l = bpy.data.objects.new("l", bpy.data.lights.new("l", "AREA")); l.data.energy = en; l.data.size = 4; l.location = loc
        l.rotation_euler = (Vector((0, 0, 1.5)) - l.location).to_track_quat("-Z", "Y").to_euler(); sc.collection.objects.link(l)
    sc.render.filepath = path; bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    prefix = args[0]; prev = args[1] if len(args) > 1 else None
    os.makedirs(os.path.dirname(os.path.abspath(prefix)), exist_ok=True)
    objs = build()
    export(prefix + ".fbx", objs)
    to_planted(objs)
    export(prefix + "_Planted.fbx", objs)
    if prev: preview(prev, objs)
    ob = [o for o in objs if o.type == "MESH"]
    print("OK", prefix, "planted height of the butt end (m): %.2f" % max(v.co.z for o in ob for v in o.data.vertices))
