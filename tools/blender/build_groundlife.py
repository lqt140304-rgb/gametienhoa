"""Ground-level life for the Underroot lake (Blender bpy, headless): luminous mushroom clusters, crystal clusters, bulb plants (lantern flowers) and
root stubs. Every luminous part is its own object named "... Glow ..." so the game gives it the self-lit material; the rest is dim.

Run:  python -I tools/blender/build_groundlife.py --out Assets/_Project/Models/Underroot --previews docs/art/previews
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

WHITE = (1.0, 1.0, 1.0)
STALK = (0.55, 0.50, 0.40)
STALK_DARK = (0.20, 0.16, 0.11)
ROCK = (0.12, 0.11, 0.10)
ROOT = (0.09, 0.065, 0.045)
GREEN = (0.10, 0.20, 0.08)


def _mix(a, b, t):
    return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))


def _blob(bm, centre, radii, segs=8, rings=5):
    res = bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=1.0)
    for v in res["verts"]:
        v.co = Vector((v.co.x * radii[0] + centre[0], v.co.y * radii[1] + centre[1], v.co.z * radii[2] + centre[2]))
    return res["verts"]


def _tube(bm, path, r0, r1, segs=6, taper=1.0, cap_start=True):
    n = len(path)
    radii = [max(0.01, r0 + (r1 - r0) * (k / (n - 1)) ** taper) for k in range(n)]
    rings = kit.tube_rings(bm, path, radii, segs)
    kit.bridge_rings(bm, rings)
    if cap_start:
        kit.cap_ring(bm, rings[0], flip=True)
    kit.cap_ring(bm, rings[-1])


def _grounded(*objs):
    for o in objs:
        for v in o.data.vertices:
            v.co.z = max(-0.05, v.co.z)


def build_shrooms(index):
    """A cluster of 3, 5 or 7 small mushrooms (0.3 to 0.9 m): dim stalks, glowing caps."""
    rng = random.Random(800 + index)
    count = (3, 5, 7)[index]
    sb, cb = bmesh.new(), bmesh.new()
    for k in range(count):
        a = rng.uniform(0, math.tau) if hasattr(math, "tau") else rng.uniform(0, 6.2832)
        d = rng.uniform(0.1, 0.55 + 0.1 * index)
        x, y = math.cos(a) * d, math.sin(a) * d
        h = rng.uniform(0.3, 0.9) * (1.0 if k else 1.15)
        lean = Vector((rng.uniform(-0.12, 0.12), rng.uniform(-0.12, 0.12), 0))
        path = [Vector((x, y, 0)) + lean * (i / 5) * h + Vector((0, 0, h * i / 5)) for i in range(6)]
        _tube(sb, path, 0.05 * h + 0.02, 0.03 * h + 0.015, 6)
        top = path[-1]
        cap_r = h * rng.uniform(0.33, 0.5)
        _blob(cb, (top.x, top.y, top.z + cap_r * 0.1), (cap_r, cap_r, cap_r * 0.55), 10, 5)
    stalks = kit.finish_object(sb, "LumaShroom_%d" % index, kit.vertex_color_material("Prop_VC"))
    caps = kit.finish_object(cb, "Shroom Glow_%d" % index, kit.vertex_color_material("Prop_VC"))
    kit.paint(stalks, lambda co, nr: _mix(STALK_DARK, STALK, min(1.0, co.z * 1.5)))
    kit.paint(caps, lambda co, nr: WHITE)
    return (stalks, caps)


def build_crystals(index):
    """A cluster of 5, 7 or 9 crystal spikes (0.5 to 1.6 m) rising from a dark rock: hexagonal, tapering to a point; the crystals glow."""
    rng = random.Random(850 + index)
    count = (5, 7, 9)[index]
    rb, cb = bmesh.new(), bmesh.new()
    _blob(rb, (0, 0, 0.05), (0.55 + 0.1 * index, 0.5 + 0.1 * index, 0.22), 9, 4)
    for k in range(count):
        a = rng.uniform(0, 6.2832)
        d = rng.uniform(0.0, 0.4)
        base = Vector((math.cos(a) * d, math.sin(a) * d, 0.05))
        tilt = rng.uniform(0.05, 0.45)
        direction = Vector((math.cos(a) * tilt, math.sin(a) * tilt, 1.0)).normalized()
        h = rng.uniform(0.5, 1.6) * (1.0 if k else 1.2)
        r = 0.07 + 0.05 * (h / 1.6)
        path = [base, base + direction * h * 0.82, base + direction * h]
        radii = [r, r * 0.95, 0.012]
        rings = kit.tube_rings(cb, path, radii, 6)
        kit.bridge_rings(cb, rings)
        kit.cap_ring(cb, rings[0], flip=True)
        kit.cap_ring(cb, rings[-1])
    rock = kit.finish_object(rb, "CrystalCluster_%d" % index, kit.vertex_color_material("Prop_VC"))
    cry = kit.finish_object(cb, "Crystal Glow_%d" % index, kit.vertex_color_material("Prop_VC"))
    kit.paint(rock, lambda co, nr: ROCK)
    kit.paint(cry, lambda co, nr: _mix((0.55, 0.55, 0.55), WHITE, min(1.0, co.z / 1.2)))
    return (rock, cry)


def build_bulb_plant(index):
    """A lantern flower: a curved stalk (1.4 or 2.0 m) with a few big leaves and 3 to 5 hanging glowing bulbs."""
    rng = random.Random(900 + index)
    height = (1.4, 2.0)[index % 2]
    bulbs = (3, 5)[index % 2]
    sb, gb = bmesh.new(), bmesh.new()
    n = 10
    arch = Vector((rng.uniform(-0.6, 0.6), rng.uniform(-0.6, 0.6), 0))
    path = [Vector((arch.x * (i / (n - 1)) ** 2, arch.y * (i / (n - 1)) ** 2, height * math.sin(i / (n - 1) * 1.45))) for i in range(n)]
    _tube(sb, path, 0.045, 0.02, 6)
    for k in range(3):                                           # leaves
        a = rng.uniform(0, 6.2832)
        p0 = path[1 + k]
        leaf = [p0, p0 + Vector((math.cos(a) * 0.35, math.sin(a) * 0.35, 0.15)), p0 + Vector((math.cos(a) * 0.7, math.sin(a) * 0.7, -0.05))]
        _tube(sb, leaf, 0.05, 0.01, 4)
    for k in range(bulbs):
        i = n - 1 - (k % 3)
        top = path[i] + Vector((rng.uniform(-0.15, 0.15), rng.uniform(-0.15, 0.15), 0))
        drop = rng.uniform(0.2, 0.45)
        stem = [path[i], top + Vector((0, 0, -drop * 0.3)), top + Vector((0, 0, -drop))]
        _tube(sb, stem, 0.012, 0.008, 4)
        s = rng.uniform(0.09, 0.14)
        _blob(gb, (top.x, top.y, top.z - drop - s * 0.8), (s, s, s * 1.3), 8, 5)
    stalk = kit.finish_object(sb, "BulbPlant_%d" % index, kit.vertex_color_material("Prop_VC"))
    bulb = kit.finish_object(gb, "Bulb Glow_%d" % index, kit.vertex_color_material("Prop_VC"))
    kit.paint(stalk, lambda co, nr: _mix(STALK_DARK, GREEN, min(1.0, co.z / 1.0)))
    kit.paint(bulb, lambda co, nr: WHITE)
    return (stalk, bulb)


def build_root_stub(index):
    """A dark root arching out of the ground: two feet buried, a loop 0.5 to 0.9 m high, with a couple of thin tendrils. No glow."""
    rng = random.Random(950 + index)
    bm = bmesh.new()
    span = (1.2, 1.8)[index % 2]
    height = (0.5, 0.9)[index % 2]
    path = [Vector((span * (i / 9 - 0.5), 0.1 * math.sin(i * 0.7), -0.15 + (height + 0.15) * math.sin(math.pi * i / 9) ** 0.8)) for i in range(10)]
    _tube(bm, path, 0.14, 0.14, 7, cap_start=True)
    for k in range(3):
        i = rng.randint(2, 7)
        a = rng.uniform(0, 6.2832)
        p0 = path[i]
        t = [p0, p0 + Vector((math.cos(a) * 0.2, math.sin(a) * 0.2, 0.12)), p0 + Vector((math.cos(a) * 0.5, math.sin(a) * 0.5, -0.05))]
        _tube(bm, t, 0.05, 0.012, 5)
    obj = kit.finish_object(bm, "RootStub_%d" % index, kit.vertex_color_material("Prop_VC"))
    off = Vector((rng.uniform(0, 50), rng.uniform(0, 50), rng.uniform(0, 50)))
    kit.paint(obj, lambda co, nr: _mix(ROOT, (0.18, 0.13, 0.09), 0.5 + 0.5 * noise.noise(co * 3.0 + off)))
    return (obj,)


PIECES = (("LumaShroom", 3, build_shrooms), ("CrystalCluster", 3, build_crystals), ("BulbPlant", 2, build_bulb_plant), ("RootStub", 2, build_root_stub))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--previews", default=None)
    args = ap.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:])
    for prefix, count, fn in PIECES:
        for i in range(count):
            kit.new_scene()
            objs = list(fn(i))
            kit.export_fbx(objs, os.path.join(args.out, "%s_%d.fbx" % (prefix, i)))
            if args.previews:
                os.makedirs(args.previews, exist_ok=True)
                kit.render_preview(objs, os.path.join(args.previews, "%s_%d_front.png" % (prefix, i)), "front", 1.6)
            print(prefix, i, sum(kit.tri_count(o) for o in objs))


if __name__ == "__main__":
    main()
