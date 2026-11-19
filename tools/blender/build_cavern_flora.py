"""Vegetation of the Slime Cavern that cannot be eaten (Blender bpy, headless): cave ferns, reeds, moss cushions, leafy bushes, mushroom clusters with glowing
caps, root arcs and hanging vines. Light vertex colours (the cave is dim, dark paint would come out black). Origin on the ground, +z up; the vines hang from z = 0 down.
Names: Flora_<Kind>_<n>; a luminous part is its own object whose name contains "Glow" (the game gives it the self-lit material).

Run:  python -I tools/blender/build_cavern_flora.py --out Assets/_Project/Models/Underroot --previews docs/art/previews
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
import build_groundlife as gl  # noqa: E402

TAU = 6.2832
LEAF = (0.24, 0.46, 0.18)
LEAF_LIGHT = (0.42, 0.62, 0.26)
LEAF_DARK = (0.12, 0.28, 0.12)
MOSS = (0.22, 0.44, 0.17)
MOSS_LIGHT = (0.40, 0.60, 0.26)
STEM = (0.30, 0.34, 0.16)
ROOT = (0.24, 0.17, 0.11)
STALK = (0.68, 0.62, 0.50)
WHITE = (1.0, 1.0, 1.0)


def _mix(a, b, t):
    return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))


def _obj(bm, name, fn):
    o = kit.finish_object(bm, name, kit.vertex_color_material("Prop_VC"))
    kit.paint(o, fn)
    return o


def build_fern(i):
    rng = random.Random(1100 + i)
    fronds = (6, 8)[i]
    bm = bmesh.new()
    for k in range(fronds):
        a = k * TAU / fronds + rng.uniform(-0.25, 0.25)
        d = Vector((math.cos(a), math.sin(a), 0))
        reach = rng.uniform(0.7, 1.15) * (1.0 + 0.2 * i)
        rise = rng.uniform(0.45, 0.8)
        path = [d * (reach * (t / 6) ** 1.25) + Vector((0, 0, rise * math.sin(math.pi * 0.62 * t / 6) + 0.02)) for t in range(7)]
        gl._tube(bm, path, 0.022, 0.008, 4)
        side = Vector((-d.y, d.x, 0))
        for t in range(2, 7):
            c = path[t]
            s = 0.11 * (1.0 - t / 8.0) + 0.03
            for sg in (1, -1):
                gl._blob(bm, tuple(c + side * sg * s * 1.2), (s * 1.3, s * 0.4, s * 0.1), 5, 3)
    return (_obj(bm, "Flora_Fern_%d" % i, lambda co, nr: _mix(LEAF_DARK, LEAF_LIGHT, min(1.0, co.z / 0.7) * 0.8 + 0.2 * (0.5 + 0.5 * noise.noise(co * 4.0)))),)


def build_reed(i):
    rng = random.Random(1200 + i)
    bm = bmesh.new()
    n = (6, 9)[i]
    for k in range(n):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.0, 0.28)
        base = Vector((math.cos(a) * d, math.sin(a) * d, 0.0))
        h = rng.uniform(1.0, 1.7)
        lean = Vector((rng.uniform(-0.15, 0.15), rng.uniform(-0.15, 0.15), 0))
        path = [base + lean * (t / 6) * h + Vector((0, 0, h * t / 6)) for t in range(7)]
        gl._tube(bm, path, 0.022, 0.01, 4)
        if rng.random() < 0.4:
            top = path[-1]
            gl._blob(bm, (top.x, top.y, top.z - 0.1), (0.035, 0.035, 0.12), 5, 3)
    for k in range(5):                                       # long thin leaves
        a = rng.uniform(0, TAU)
        leaf = [Vector((0, 0, 0.05)), Vector((math.cos(a) * 0.2, math.sin(a) * 0.2, 0.7)), Vector((math.cos(a) * 0.5, math.sin(a) * 0.5, 0.95))]
        gl._tube(bm, leaf, 0.03, 0.006, 4)
    return (_obj(bm, "Flora_Reed_%d" % i, lambda co, nr: _mix(STEM, LEAF_LIGHT, min(1.0, co.z / 1.2))),)


def build_moss(i):
    rng = random.Random(1300 + i)
    bm = bmesh.new()
    n = (3, 4, 5)[i]
    for k in range(n):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.0, 0.3 + 0.1 * i)
        r = rng.uniform(0.22, 0.4)
        verts = gl._blob(bm, (math.cos(a) * d, math.sin(a) * d, 0.02), (r, r * 0.9, r * 0.55), 9, 5)
        for v in verts:
            v.co.z = max(0.0, v.co.z)
    off = Vector((rng.uniform(0, 40), rng.uniform(0, 40), rng.uniform(0, 40)))
    return (_obj(bm, "Flora_Moss_%d" % i, lambda co, nr: _mix(MOSS, MOSS_LIGHT, min(1.0, co.z / 0.25) * 0.7 + 0.3 * (0.5 + 0.5 * noise.noise(co * 6.0 + off)))),)


def build_bush(i):
    rng = random.Random(1400 + i)
    bm = bmesh.new()
    h = (0.7, 1.0)[i]
    for k in range(3):                                       # a few woody stems
        a = rng.uniform(0, TAU)
        tip = Vector((math.cos(a) * 0.28, math.sin(a) * 0.28, h * rng.uniform(0.7, 1.0)))
        gl._tube(bm, [Vector((0, 0, 0)), tip * 0.5, tip], 0.035, 0.012, 4)
    for k in range(13):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.05, 0.4)
        z = rng.uniform(0.15, h)
        r = rng.uniform(0.12, 0.2)
        gl._blob(bm, (math.cos(a) * d, math.sin(a) * d, z), (r, r * 0.9, r * 0.5), 6, 4)
    off = Vector((rng.uniform(0, 40), rng.uniform(0, 40), rng.uniform(0, 40)))
    return (_obj(bm, "Flora_Bush_%d" % i, lambda co, nr: _mix(STEM, _mix(LEAF, LEAF_LIGHT, 0.5 + 0.5 * noise.noise(co * 5.0 + off)), min(1.0, co.z / 0.35))),)


def build_mushrooms(i):
    rng = random.Random(1500 + i)
    sb, cb = bmesh.new(), bmesh.new()
    n = (4, 6)[i]
    for k in range(n):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.05, 0.4)
        x, y = math.cos(a) * d, math.sin(a) * d
        h = rng.uniform(0.12, 0.4)
        gl._tube(sb, [Vector((x, y, 0)), Vector((x, y, h * 0.5)), Vector((x, y, h))], 0.03, 0.022, 5)
        r = rng.uniform(0.09, 0.2)
        gl._blob(cb, (x, y, h + 0.02), (r, r, r * 0.5), 8, 4)
    stalks = kit.finish_object(sb, "Flora_Mushrooms_%d" % i, kit.vertex_color_material("Prop_VC"))
    kit.paint(stalks, lambda co, nr: STALK)
    caps = kit.finish_object(cb, "Cluster Glow_%d" % i, kit.vertex_color_material("Prop_VC"))
    kit.paint(caps, lambda co, nr: WHITE)
    return (stalks, caps)


def build_root_arc(i):
    rng = random.Random(1600 + i)
    bm = bmesh.new()
    span = (1.6, 2.4)[i]
    height = (0.7, 1.1)[i]
    path = [Vector((span * (t / 10 - 0.5), 0.12 * math.sin(t * 0.7), -0.15 + (height + 0.15) * math.sin(math.pi * t / 10) ** 0.8)) for t in range(11)]
    gl._tube(bm, path, 0.13, 0.13, 7)
    for k in range(4):
        t = rng.randint(2, 8)
        a = rng.uniform(0, TAU)
        p0 = path[t]
        tw = [p0, p0 + Vector((math.cos(a) * 0.2, math.sin(a) * 0.2, 0.12)), p0 + Vector((math.cos(a) * 0.5, math.sin(a) * 0.5, -0.05))]
        gl._tube(bm, tw, 0.05, 0.012, 5)
    return (_obj(bm, "Flora_RootArc_%d" % i, lambda co, nr: _mix(ROOT, (0.40, 0.30, 0.20), 0.5 + 0.5 * noise.noise(co * 3.0))),)


def build_vine(i):
    """Five leafy vines hanging from z = 0 (the roof): 5 to 9 m long, thin, with leaves along them. For the roof round the hole of light."""
    rng = random.Random(1700 + i)
    bm = bmesh.new()
    for k in range(5):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.0, 1.2)
        top = Vector((math.cos(a) * d, math.sin(a) * d, 0.0))
        length = rng.uniform(5.0, 9.0) * (1.0 + 0.15 * i)
        n = 9
        path = [top + Vector((0.25 * math.sin(t * 0.9 + k), 0.25 * math.cos(t * 0.7 + k), -length * t / (n - 1))) for t in range(n)]
        gl._tube(bm, path, 0.035, 0.012, 4)
        for t in range(2, n, 1):
            c = path[t]
            b = rng.uniform(0, TAU)
            r = rng.uniform(0.11, 0.18)
            gl._blob(bm, (c.x + math.cos(b) * 0.12, c.y + math.sin(b) * 0.12, c.z), (r, r * 0.5, r * 0.9), 5, 3)
    return (_obj(bm, "Flora_Vine_%d" % i, lambda co, nr: _mix(STEM, LEAF, min(1.0, abs(co.z) / 2.0)) if abs(nr.z) < 0.99 else LEAF),)


PIECES = (("Fern", 2, build_fern), ("Reed", 2, build_reed), ("Moss", 3, build_moss), ("Bush", 2, build_bush),
          ("Mushrooms", 2, build_mushrooms), ("RootArc", 2, build_root_arc), ("Vine", 2, build_vine))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--previews", default=None)
    args = ap.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:])
    for kind, count, fn in PIECES:
        for i in range(count):
            kit.new_scene()
            objs = list(fn(i))
            kit.export_fbx(objs, os.path.join(args.out, "Flora_%s_%d.fbx" % (kind, i)))
            if args.previews:
                os.makedirs(args.previews, exist_ok=True)
                kit.render_preview(objs, os.path.join(args.previews, "Flora_%s_%d_front.png" % (kind, i)), "front", 1.6)
            print(kind, i, sum(kit.tri_count(o) for o in objs))


if __name__ == "__main__":
    main()
