"""Detailed mushrooms for the Slime Cavern (Blender bpy, headless): four decorative clusters (CaveShroom_0..3) and the edible fat cap (Forage_FatCap).
Each mushroom has a bulbous base, a ringed stalk, a lumpy cap that curls under at the rim, and gills on the underside (radial ridges); some have spots.
CaveShroom_2 and _3 have glowing caps: the self-lit parts are a separate object whose name contains "Glow". Origin on the ground, +z up. No colliders.

Run:  python -I tools/blender/build_cave_shrooms.py --out Assets/_Project/Models/Underroot --previews docs/art/previews
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


def _mix(a, b, t):
    t = max(0.0, min(1.0, t))
    return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))


def _vc(bm, name, fn):
    o = kit.finish_object(bm, name, kit.vertex_color_material("Prop_VC"))
    kit.paint(o, fn)
    return o


def _cap(bm, x, y, base_z, cap_r, cap_h, stalk_r, rng, seed, rings=8, segs=18):
    """A lumpy dome that curls under at the rim; its underside runs inward to the stalk with radial gill ridges."""
    off = rng.uniform(0, 50)
    grid = []
    top = bm.verts.new((x, y, base_z + cap_h))
    grid.append([top] * segs)
    sweep = math.pi * 0.5 + 0.38                                           # a little past the equator: the rim curls down and under
    for i in range(1, rings + 1):
        phi = sweep * i / rings
        ring = []
        for j in range(segs):
            a = TAU * j / segs
            lump = 1.0 + 0.07 * noise.noise(Vector((math.cos(a) * 1.7 + off, math.sin(a) * 1.7 + off, i * 0.35 + seed)))
            r = cap_r * math.sin(phi) * lump
            z = base_z + cap_h * math.cos(phi) - 0.04 * cap_r * (1.0 if phi > math.pi * 0.5 else 0.0)
            ring.append(bm.verts.new((x + math.cos(a) * r, y + math.sin(a) * r, z)))
        grid.append(ring)
    rim = grid[-1]
    for i in range(1, 4):                                                  # the underside: three rings in to the stalk, with gill ridges
        t = i / 3.0
        ring = []
        for j in range(segs):
            a = TAU * j / segs
            r = (rim and math.hypot(rim[j].co.x - x, rim[j].co.y - y)) * (1.0 - t) + stalk_r * 1.1 * t
            z = rim[j].co.z + (base_z + cap_h * 0.18 - rim[j].co.z) * t - (0.012 * cap_r * (1.0 if j % 2 else 0.0)) * (1.0 - 0.5 * t)
            ring.append(bm.verts.new((x + math.cos(a) * r, y + math.sin(a) * r, z)))
        grid.append(ring)
    for i in range(1, len(grid)):
        a, b = grid[i - 1], grid[i]
        for j in range(segs):
            k = (j + 1) % segs
            if i == 1:
                bm.faces.new([b[j], b[k], a[0]])
            else:
                bm.faces.new([a[j], a[k], b[k], b[j]][::-1])
    bm.faces.new(grid[-1][::-1])                                           # closes the underside around the stalk


def _stalk(bm, x, y, h, r, rng, ring_at=0.62):
    lean = Vector((rng.uniform(-0.04, 0.04), rng.uniform(-0.04, 0.04), 0.0)) * h
    path = [Vector((x, y, 0.0)) + lean * (k / 6.0) ** 2 + Vector((0, 0, h * k / 6.0)) for k in range(7)]
    radii = [r * f for f in (1.55, 1.25, 1.0, 0.92, 0.9, 0.95, 1.0)]       # a bulb at the foot, slimmest in the middle
    rings = kit.tube_rings(bm, path, radii, 7)
    kit.bridge_rings(bm, rings)
    kit.cap_ring(bm, rings[0], flip=True)
    kit.cap_ring(bm, rings[-1])
    # the skirt (a thin flared ring) under the cap
    z = h * ring_at
    sp = [Vector((x, y, z + 0.0)) + lean * ring_at ** 2, Vector((x, y, z - 0.03 * h)) + lean * ring_at ** 2]
    sr = kit.tube_rings(bm, sp, [r * 1.05, r * 2.1], 9)
    kit.bridge_rings(bm, sr)
    kit.cap_ring(bm, sr[0])
    kit.cap_ring(bm, sr[1], flip=True)
    return path[-1]


def _spots(bm, x, y, base_z, cap_r, cap_h, rng, count):
    for _ in range(count):
        phi = rng.uniform(0.15, 0.95)
        a = rng.uniform(0, TAU)
        r = cap_r * math.sin(phi) * 1.0
        z = base_z + cap_h * math.cos(phi)
        s = cap_r * rng.uniform(0.08, 0.16)
        gl._blob(bm, (x + math.cos(a) * r, y + math.sin(a) * r, z - s * 0.2), (s, s, s * 0.35), 6, 4)


def _cluster(spec, seed):
    """spec: list of (dx, dy, height, cap_radius, spots). Returns (stalks, caps, spots) bmeshes."""
    rng = random.Random(seed)
    sb, cb, pb = bmesh.new(), bmesh.new(), bmesh.new()
    for k, (dx, dy, h, cr, spots) in enumerate(spec):
        top = _stalk(sb, dx, dy, h, cr * 0.28, rng)
        cap_h = cr * rng.uniform(0.55, 0.75)
        _cap(cb, top.x, top.y, top.z - cap_h * 0.25, cr, cap_h, cr * 0.28, rng, seed + k)
        if spots:
            _spots(pb, top.x, top.y, top.z - cap_h * 0.25, cr, cap_h, rng, spots)
    return sb, cb, pb


def _cap_colour(light, dark, under=(0.82, 0.74, 0.58)):
    def f(co, nr):
        if nr.z < -0.25:
            return under                                               # gills
        return _mix(dark, light, max(0.0, min(1.0, (math.hypot(co.x, co.y) % 0.5) * 0.0 + 0.5 + 0.5 * math.sin(co.x * 19.0 + co.y * 13.0) * 0.4 + nr.z * 0.3)))
    return f


def build_shroom_0():
    spec = [(0.0, 0.0, 0.17, 0.07, 0), (0.13, 0.05, 0.11, 0.05, 0), (-0.11, 0.09, 0.14, 0.06, 0), (0.05, -0.13, 0.09, 0.045, 0), (-0.07, -0.1, 0.12, 0.055, 0)]
    sb, cb, pb = _cluster(spec, 11)
    stalks = _vc(sb, "CaveShroom_0", lambda co, nr: _mix((0.55, 0.5, 0.38), (0.86, 0.82, 0.7), co.z / 0.17))
    caps = _vc(cb, "CaveShroom_0 Caps", _cap_colour((0.80, 0.70, 0.52), (0.55, 0.42, 0.28)))
    return (stalks, caps)


def build_shroom_1():
    spec = [(0.0, 0.0, 0.27, 0.11, 6), (0.2, 0.1, 0.19, 0.08, 4), (-0.16, 0.14, 0.15, 0.065, 3)]
    sb, cb, pb = _cluster(spec, 12)
    stalks = _vc(sb, "CaveShroom_1", lambda co, nr: _mix((0.50, 0.42, 0.30), (0.80, 0.72, 0.58), co.z / 0.27))
    caps = _vc(cb, "CaveShroom_1 Caps", _cap_colour((0.66, 0.34, 0.16), (0.36, 0.17, 0.08)))
    spots = _vc(pb, "CaveShroom_1 Spots", lambda co, nr: (0.92, 0.9, 0.82))
    return (stalks, caps, spots)


def build_shroom_2():                                                         # slender, glowing teal caps
    spec = [(0.0, 0.0, 0.26, 0.07, 0), (0.12, 0.07, 0.2, 0.055, 0), (-0.1, 0.1, 0.17, 0.05, 0), (0.05, -0.12, 0.13, 0.04, 0)]
    sb, cb, pb = _cluster(spec, 13)
    stalks = _vc(sb, "CaveShroom_2", lambda co, nr: _mix((0.45, 0.55, 0.52), (0.8, 0.9, 0.88), co.z / 0.26))
    caps = _vc(cb, "CaveShroom_2 Glow", lambda co, nr: (0.72, 0.78, 0.78) if nr.z < -0.25 else (1.0, 1.0, 1.0))
    return (stalks, caps)


def build_shroom_3():                                                         # one big spotted cap with a glow rim, and two small
    spec = [(0.0, 0.0, 0.3, 0.14, 7), (0.24, 0.1, 0.12, 0.05, 0), (-0.18, -0.12, 0.1, 0.045, 0)]
    sb, cb, pb = _cluster(spec, 14)
    stalks = _vc(sb, "CaveShroom_3", lambda co, nr: _mix((0.55, 0.5, 0.42), (0.9, 0.86, 0.78), co.z / 0.3))
    caps = _vc(cb, "CaveShroom_3 Glow", lambda co, nr: (0.8, 0.75, 0.6) if nr.z < -0.25 else (1.0, 1.0, 1.0))
    spots = _vc(pb, "CaveShroom_3 Spots", lambda co, nr: (0.95, 0.93, 0.85))
    return (stalks, caps, spots)


def build_fatcap():                                                            # the edible one: chubby tan caps on thick stalks
    spec = [(0.0, 0.0, 0.3, 0.17, 0), (0.28, 0.1, 0.2, 0.12, 0), (-0.22, 0.15, 0.16, 0.1, 0)]
    sb, cb, pb = _cluster(spec, 15)
    stalks = _vc(sb, "Forage_FatCap", lambda co, nr: _mix((0.62, 0.55, 0.40), (0.92, 0.88, 0.74), co.z / 0.3))
    caps = _vc(cb, "Forage_FatCap Caps", _cap_colour((0.86, 0.66, 0.42), (0.62, 0.4, 0.2)))
    return (stalks, caps)


PIECES = (("CaveShroom_0", build_shroom_0), ("CaveShroom_1", build_shroom_1), ("CaveShroom_2", build_shroom_2), ("CaveShroom_3", build_shroom_3), ("Forage_FatCap", build_fatcap))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--previews", default=None)
    args = ap.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:])
    for name, fn in PIECES:
        kit.new_scene()
        objs = list(fn())
        kit.export_fbx(objs, os.path.join(args.out, name + ".fbx"))
        if args.previews:
            os.makedirs(args.previews, exist_ok=True)
            kit.render_preview(objs, os.path.join(args.previews, name + "_front.png"), "front", 2.6)
        print(name, sum(kit.tri_count(o) for o in objs))


if __name__ == "__main__":
    main()
