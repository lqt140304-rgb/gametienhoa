"""Builds six Underroot rocks (display mesh + simplified collider) as FBX with preview renders.

Run:  python -I tools/blender/build_rocks.py --out Assets/_Project/Models/Underroot --previews docs/art/previews
"""
import argparse
import math
import os
import random
import sys

import bpy  # must come before bmesh/mathutils
import bmesh
from mathutils import Vector, noise

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_common as kit  # noqa: E402

# Target largest dimension in metres per variant (spec: small steps up to big boulders).
SIZES = [0.3, 0.6, 1.0, 1.6, 2.4, 3.2]
MOSS = (0.03, 0.12, 0.03, 1.0)   # linear colour
STONE = (0.11, 0.11, 0.10, 1.0)


def build_rock(seed, size):
    """Return (display mesh object, collider object) for one rock of about `size` metres."""
    rng = random.Random(seed)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=4, radius=1.0)
    off = Vector((rng.uniform(0, 100), rng.uniform(0, 100), rng.uniform(0, 100)))
    squash = Vector((rng.uniform(0.8, 1.2), rng.uniform(0.8, 1.2), rng.uniform(0.7, 1.0)))
    for v in bm.verts:
        p = v.co.copy()
        n = noise.fractal(p * 0.9 + off, 0.5, 2.0, 3)
        ridge = noise.noise(p * 2.2 + off) * 0.06
        r = 1.0 + 0.28 * n + ridge
        v.co = Vector((p.x * r * squash.x, p.y * r * squash.y, p.z * r * squash.z))
    for v in bm.verts:  # flat bottom so the rock sits on the ground
        v.co.z = max(v.co.z, -0.45 * squash.z)
    bm.normal_update()
    mesh = bpy.data.meshes.new("Rock_%d" % seed)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Rock_%d" % seed, mesh)
    kit.link(obj)
    # normalise: largest horizontal/vertical extent == size, base at z = 0
    w, d, h = kit.bounds(obj)
    k = size / max(w, d, h)
    zmin = min(v.co.z for v in mesh.vertices)
    for v in mesh.vertices:
        v.co = Vector((v.co.x * k, v.co.y * k, (v.co.z - zmin) * k))
    for p in mesh.polygons:
        p.use_smooth = True
    # vertex colours: moss on upward-facing parts
    attr = mesh.color_attributes.new("Col", "BYTE_COLOR", "CORNER")
    for poly in mesh.polygons:
        for li in poly.loop_indices:
            vn = mesh.vertices[mesh.loops[li].vertex_index]
            p = vn.co
            up = max(0.0, vn.normal.z)
            t = min(1.0, max(0.0, (up - 0.5) / 0.35 + 0.35 * noise.noise(p * 1.8 + off)))
            shade = 0.85 + 0.3 * noise.noise(p * 2.5 + off)
            c = [STONE[i] * (1 - t) + MOSS[i] * t for i in range(3)]
            attr.data[li].color = (c[0] * shade, c[1] * shade, c[2] * shade, 1.0)
    obj.data.materials.append(kit.vertex_color_material())
    col = kit.make_collider(obj, 80)
    return obj, col


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--previews", default=None)
    args = ap.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:])
    for seed, size in enumerate(SIZES):
        kit.new_scene()
        obj, col = build_rock(seed, size)
        if kit.tri_count(col) < 4:
            raise SystemExit("collider of %s is empty" % obj.name)
        kit.export_fbx([obj, col], os.path.join(args.out, obj.name + ".fbx"))
        if args.previews:
            os.makedirs(args.previews, exist_ok=True)
            for angle in ("front", "top"):
                kit.render_preview([obj, col], os.path.join(args.previews, "%s_%s.png" % (obj.name, angle)), angle)
        print("%s: %d tris, collider %d tris" % (obj.name, kit.tri_count(obj), kit.tri_count(col)))


if __name__ == "__main__":
    main()
