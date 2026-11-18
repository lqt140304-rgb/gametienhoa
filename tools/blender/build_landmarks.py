"""Builds the Underroot landmark props: hollow rotting log, giant/small mushrooms and the root arch (FBX + preview renders).

Run:  python -I tools/blender/build_landmarks.py --out Assets/_Project/Models/Underroot --previews docs/art/previews
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

BARK = (0.10, 0.06, 0.04)
BARK_DARK = (0.05, 0.03, 0.02)
MOSS = (0.03, 0.12, 0.03)
ROOT = (0.07, 0.05, 0.035)
STALK = (0.70, 0.62, 0.45)
CAPS = [(0.75, 0.62, 0.38), (0.68, 0.42, 0.16), (0.55, 0.45, 0.30)]


def _mix(a, b, t):
    return tuple(a[i] * (1 - t) + b[i] * t for i in range(3))


def _shade(p, scale, off):
    return 0.8 + 0.4 * (0.5 + 0.5 * noise.noise(p * scale + off))


# ---------------------------------------------------------------- hollow log

def _hollow_tube(bm, path, r_out, r_in, segs, wobble=None):
    outer = kit.tube_rings(bm, path, [r_out] * len(path), segs, wobble)
    inner = kit.tube_rings(bm, path, [r_in] * len(path), segs)
    kit.bridge_rings(bm, outer)
    kit.bridge_rings(bm, inner, flip=True)
    for end in (0, -1):  # rims close the wall at both ends; the bore stays open
        for k in range(segs):
            k2 = (k + 1) % segs
            bm.faces.new([outer[end][k], outer[end][k2], inner[end][k2], inner[end][k]])


def _ridged(v):
    """Ridged noise 0..1: sharp creases like bark furrows."""
    return 1.0 - abs(noise.noise(v))


def build_log(seed=0):
    """Return (display, collider): a 9 m hollow log, open at both ends. Rough bark (deep furrows, plates, knots), jagged broken
    ends, an uneven rotted bore, bracket fungus, moss tufts, snapped branch stubs and root tendrils at one end."""
    rng = random.Random(seed)
    off = Vector((rng.uniform(0, 50), rng.uniform(0, 50), rng.uniform(0, 50)))
    length, r_out, r_in, n, segs = 9.0, 0.95, 0.80, 40, 72
    path = [Vector((-length / 2 + length * i / (n - 1), math.sin((i / (n - 1) - 0.5) * 2.4) * 0.4, r_out + 0.12)) for i in range(n)]

    def bark(p, a):
        ca, sa = math.cos(a), math.sin(a)
        furrow = _ridged(Vector((ca * 5.5, sa * 5.5, p.x * 0.5)) + off)        # long creases along the trunk
        plates = noise.noise(Vector((ca * 10.0, sa * 10.0, p.x * 2.4)) + off) * 0.5  # broken plates across them
        knob = noise.noise(Vector((p.x * 1.1, ca * 1.4, sa * 1.4)) + off * 0.5)    # big swellings and knots
        return 0.15 * furrow + 0.06 * plates + 0.07 * knob - 0.09

    def bore(p, a):
        return 0.075 * noise.noise(Vector((p.x * 0.9, math.cos(a) * 1.5, math.sin(a) * 1.5)) + off * 2.0) - 0.02

    bm = bmesh.new()
    outer = kit.tube_rings(bm, path, [r_out] * n, segs, bark)
    inner = kit.tube_rings(bm, path, [r_in] * n, segs, bore)
    # broken ends: both ends are ragged, the wall splinters back by different amounts around the circle
    for ring_pair, sign in (((0, 1, 2), 1.0), ((n - 1, n - 2, n - 3), -1.0)):
        for depth, ri in enumerate(ring_pair):
            for k in range(segs):
                a = 2 * math.pi * k / segs
                splinter = max(0.0, noise.noise(Vector((math.cos(a) * 2.2, math.sin(a) * 2.2, sign * 3.0 + depth * 0.1)) + off)) * (0.6 if depth == 0 else 0.25 if depth == 1 else 0.0)
                for ring in (outer, inner):
                    ring[ri][k].co.x += sign * splinter
    kit.bridge_rings(bm, outer)
    kit.bridge_rings(bm, inner, flip=True)
    for end in (0, -1):  # rims close the wall at both ends; the bore stays open
        for k in range(segs):
            k2 = (k + 1) % segs
            bm.faces.new([outer[end][k], outer[end][k2], inner[end][k2], inner[end][k]])

    for i in range(4, n - 4, 5):  # bracket fungus: stacked shelves with curled lips along the back and sides
        a0 = rng.uniform(0.3, math.pi - 0.3) + (math.pi if rng.random() < 0.4 else 0.0)
        for f in range(rng.randint(2, 4)):
            a = a0 + rng.uniform(-0.15, 0.15)
            p = path[i] + Vector((rng.uniform(-0.1, 0.1), math.cos(a), math.sin(a))) * (r_out * 0.98) + Vector((0, 0, f * 0.16 - 0.1))
            res = bmesh.ops.create_uvsphere(bm, u_segments=10, v_segments=5, radius=1.0)
            sz = rng.uniform(0.25, 0.5) * (1.0 - 0.15 * f)
            bmesh.ops.scale(bm, vec=(sz, sz * 0.8, 0.06), verts=res["verts"])
            bmesh.ops.translate(bm, vec=p, verts=res["verts"])
    for i in range(3, n - 3, 4):  # moss tufts: lumpy mounds on the upper side
        if rng.random() < 0.7:
            p = path[i] + Vector((rng.uniform(-0.3, 0.3), rng.uniform(-0.45, 0.45), r_out * 0.92))
            res = bmesh.ops.create_uvsphere(bm, u_segments=8, v_segments=5, radius=1.0)
            bmesh.ops.scale(bm, vec=(rng.uniform(0.3, 0.6), rng.uniform(0.25, 0.45), rng.uniform(0.1, 0.2)), verts=res["verts"])
            bmesh.ops.translate(bm, vec=p, verts=res["verts"])
    for k, side in ((11, 1.0), (27, -1.0)):  # two snapped branch stubs, thick with ragged broken tops
        base = path[k] + Vector((0, 0.35 * side, r_out * 0.55))
        stub = [base, base + Vector((0, 0.4 * side, 0.5)), base + Vector((0.1 * side, 0.6 * side, 0.95))]
        rings = kit.tube_rings(bm, stub, [0.36, 0.30, 0.24], 9, lambda p, a: 0.25 * noise.noise(Vector((p.z * 4, math.cos(a) * 2, math.sin(a) * 2)) + off))
        kit.bridge_rings(bm, rings)
        kit.cap_ring(bm, rings[-1])
    for t in range(5):  # root tendrils reaching from the low end into the ground
        a = rng.uniform(0.0, 2 * math.pi)
        base = path[0] + Vector((0.1, math.cos(a) * 0.7, math.sin(a) * 0.7))
        end = base + Vector((rng.uniform(-0.8, -0.2), math.cos(a) * rng.uniform(0.5, 1.3), -0.7 - rng.uniform(0.0, 0.2)))
        mid = (base + end) * 0.5 + Vector((0, 0, 0.15))
        rings = kit.tube_rings(bm, [base, mid, end], [0.16, 0.12, 0.06], 6)
        kit.bridge_rings(bm, rings)
        kit.cap_ring(bm, rings[-1])
    log = kit.finish_object(bm, "Log_Hollow", kit.vertex_color_material("Prop_VC"))
    for v in log.data.vertices:  # tendrils and splinters never dip below the ground line
        v.co.z = max(0.0, v.co.z)

    cp = [path[i] for i in range(0, n, 9)] + [path[-1]]
    cbm = bmesh.new()
    _hollow_tube(cbm, cp, r_out, r_in - 0.08, 8)
    col = kit.finish_object(cbm, "Log_Hollow_COL")

    def colour(co, nr):
        n1 = 0.5 + 0.5 * noise.noise(co * 2.4 + off)
        n2 = 0.5 + 0.5 * noise.noise(co * 7.0 + off)
        pos = Vector((co.x, co.y, 0.0))
        radial = Vector((0.0, co.y - path[min(n - 1, max(0, int((co.x + length / 2) / length * (n - 1))))].y, co.z - path[0].z))
        inside = radial.length < r_in + 0.06 and nr.dot(Vector((0, -radial.y, -(co.z - path[0].z)))) > 0.1
        if inside:  # rotted, orange-brown, crumbling wood in the bore
            return _mix((0.12, 0.05, 0.02), (0.20, 0.10, 0.04), n1 * n2)
        a = math.atan2(radial.z, radial.y)
        furrow = _ridged(Vector((math.cos(a) * 5.5, math.sin(a) * 5.5, co.x * 0.5)) + off)
        base = _mix((0.26, 0.18, 0.11), BARK_DARK, min(1.0, max(0.0, (furrow - 0.55) * 2.6)))  # pale plate faces, near-black creases
        base = _mix(base, BARK, 0.35 * n2)
        base = _mix(base, (0.16, 0.14, 0.11), max(0.0, n1 - 0.62) * 1.6)  # grey dry wood where the bark fell off
        moss = min(1.0, max(0.0, (nr.z * 0.9 + 0.55 * (n1 - 0.5) - 0.25) / 0.45))
        return _mix(base, MOSS, moss)

    kit.paint(log, colour)
    return log, col


# ---------------------------------------------------------------- mushrooms

def build_mushroom(seed, height, kind="giant"):
    """Return (stalk, cap, collider). Cap object is named 'Giant Caps_<seed>' (giant) or 'Small Caps_<seed>' (the
    LakeLook glow lights look for 'Giant Caps'). The collider is the stalk only: the cap is never solid."""
    rng = random.Random(seed)
    lean = rng.uniform(-0.25, 0.25) * height * 0.1
    stalk_h = height * 0.80
    n = 10
    path = [Vector((lean * math.sin(i / (n - 1) * math.pi), 0, 0.02 + stalk_h * i / (n - 1))) for i in range(n)]
    radii = [height * 0.075 * (1.7 - 0.9 * (i / (n - 1)) ** 0.7) for i in range(n)]
    bm = bmesh.new()
    rings = kit.tube_rings(bm, path, radii, 14, lambda p, a: 0.05 * noise.noise(Vector((p.z * 1.5, math.cos(a), math.sin(a))) + Vector((seed, 0, 0))))
    kit.bridge_rings(bm, rings)
    kit.cap_ring(bm, rings[0], flip=True)
    kit.cap_ring(bm, rings[-1])
    stalk = kit.finish_object(bm, "Mushroom_%s_%d" % (kind.capitalize(), seed), kit.vertex_color_material("Prop_VC"))
    kit.paint(stalk, lambda co, nr: _mix(STALK, BARK, max(0.0, 1.0 - co.z / max(0.01, stalk_h) * 4.0) * 0.6))

    rc = height * 0.30
    zc = stalk_h
    cbm = bmesh.new()
    cring = []
    top = [(0.02, zc + rc * 0.55)]
    for j in range(1, 9):
        phi = j / 8 * math.pi / 2
        top.append((rc * math.sin(phi), zc + rc * 0.55 * math.cos(phi)))
    top += [(rc * 0.92, zc - 0.05 * rc), (rc * 0.3, zc - 0.18 * rc)]
    for rad, z in top:
        ring = []
        for k in range(16):
            a = 2 * math.pi * k / 16
            wob = 1.0 + 0.06 * noise.noise(Vector((math.cos(a) * 2, math.sin(a) * 2, z)) + Vector((seed, 3, 0)))
            ring.append(cbm.verts.new(Vector((lean + rad * wob * math.cos(a), rad * wob * math.sin(a), z))))
        cring.append(ring)
    kit.bridge_rings(cbm, cring, flip=True)
    kit.cap_ring(cbm, cring[0])
    kit.cap_ring(cbm, cring[-1], flip=True)
    cap = kit.finish_object(cbm, ("Giant Caps_%d" if kind == "giant" else "Small Caps_%d") % seed, kit.vertex_color_material("Prop_VC"))
    base = CAPS[seed % len(CAPS)]
    kit.paint(cap, lambda co, nr: _mix(base, (0.95, 0.9, 0.6), min(1.0, (0.3 + 0.7 * max(0.0, nr.z)) ** 3 * 0.5)) if nr.z > -0.2 else (0.22, 0.17, 0.10))

    colm = bmesh.new()
    cpath = [Vector((0, 0, 0.0)), Vector((lean * 0.5, 0, stalk_h * 0.5)), Vector((lean, 0, stalk_h))]
    crings = kit.tube_rings(colm, cpath, [radii[0] * 0.9, radii[4] * 0.95, radii[-1]], 8)
    kit.bridge_rings(colm, crings)
    kit.cap_ring(colm, crings[0], flip=True)
    kit.cap_ring(colm, crings[-1])
    col = kit.finish_object(colm, stalk.name + "_COL")
    return stalk, cap, col


# ---------------------------------------------------------------- root arch

def build_root_arch(seed=0):
    """Return (display, collider): a 4 m wide (clear gap) and 8 m tall arch of layered roots, thin roots hanging from the top."""
    rng = random.Random(seed)
    off = Vector((rng.uniform(0, 50), rng.uniform(0, 50), rng.uniform(0, 50)))
    width, height = 4.0, 8.0
    bm = bmesh.new()
    for i in range(6):
        xl = -(width / 2 + 0.9 + rng.uniform(0.0, 0.8))
        xr = width / 2 + 0.9 + rng.uniform(0.0, 0.8)
        h = height - 0.45 if i < 2 else height * rng.uniform(0.70, 0.93) - 0.3
        lean = rng.uniform(-1.0, 1.0)
        r0 = 0.9 if i < 2 else rng.uniform(0.45, 0.75)
        n = 18
        path, radii = [], []
        for k in range(n):
            t = k / (n - 1)
            s = math.sin(math.pi * t)
            p = Vector((xl + (xr - xl) * t, lean * s + 0.25 * noise.noise(Vector((t * 4, i, 0)) + off), max(0.0, h * s ** 0.85)))
            path.append(p)
            radii.append(r0 * (0.55 + 0.45 * (1.0 - s)) * 1.15)
        rings = kit.tube_rings(bm, path, radii, 10, lambda p, a: 0.10 * noise.noise(Vector((p.x * 0.6, p.y, p.z * 0.6 + a)) + off))
        kit.bridge_rings(bm, rings)
        kit.cap_ring(bm, rings[0], flip=True)
        kit.cap_ring(bm, rings[-1])
    for i in range(9):  # thin roots hanging from the top like a curtain
        sx = rng.uniform(-width * 0.45, width * 0.45)
        sy = rng.uniform(-0.5, 0.5)
        ln = rng.uniform(2.0, 5.0)
        hang = [Vector((sx + 0.15 * math.sin(k), sy, height - 0.7 - ln * k / 5)) for k in range(6)]
        rings = kit.tube_rings(bm, hang, [0.09 - 0.012 * k for k in range(6)], 5)
        kit.bridge_rings(bm, rings)
        kit.cap_ring(bm, rings[-1])
    arch = kit.finish_object(bm, "RootArch", kit.vertex_color_material("Prop_VC"))
    for v in arch.data.vertices:  # the feet are cut flat so the arch sits on (and a little into) the ground
        v.co.z = max(0.0, v.co.z)
    kit.paint(arch, lambda co, nr: _mix(ROOT, MOSS, min(1.0, max(0.0, (nr.z - 0.5) / 0.4)) * (0.5 if co.z < 5.0 else 0.15)) if nr.z > 0 else ROOT)
    cbm = bmesh.new()
    for side in (-1, 1):
        leg = [Vector((side * 2.95, 0, 0)), Vector((side * 2.95, 0, 2.5)), Vector((side * 2.95, 0, 5.0))]
        rings = kit.tube_rings(cbm, leg, [1.0, 1.0, 0.9], 8)
        kit.bridge_rings(cbm, rings)
        kit.cap_ring(cbm, rings[0], flip=True)
        kit.cap_ring(cbm, rings[-1])
    col = kit.finish_object(cbm, "RootArch_COL")
    return arch, col


# ---------------------------------------------------------------- driver

def _export(objs, name, out, previews):
    kit.export_fbx(objs, os.path.join(out, name + ".fbx"))
    if previews:
        os.makedirs(previews, exist_ok=True)
        for angle in ("front", "side", "top"):
            kit.render_preview(objs, os.path.join(previews, "%s_%s.png" % (name, angle)), angle)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--previews", default=None)
    args = ap.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:])

    kit.new_scene()
    log, col = build_log(1)
    _export([log, col], "Log_Hollow", args.out, args.previews)
    for idx, h in enumerate((7.0, 5.5, 4.5)):
        kit.new_scene()
        s, c, k = build_mushroom(idx, h, "giant")
        s.name = "Mushroom_Giant_%d" % idx
        k.name = s.name + "_COL"
        _export([s, c, k], "Mushroom_Giant_%d" % idx, args.out, args.previews)
    for idx, h in enumerate((0.9, 1.6, 2.4, 3.4)):
        kit.new_scene()
        s, c, k = build_mushroom(10 + idx, h, "small")
        s.name = "Mushroom_Small_%d" % idx
        k.name = s.name + "_COL"
        _export([s, c, k], "Mushroom_Small_%d" % idx, args.out, args.previews)
    kit.new_scene()
    arch, acol = build_root_arch(1)
    _export([arch, acol], "RootArch", args.out, args.previews)


if __name__ == "__main__":
    main()
