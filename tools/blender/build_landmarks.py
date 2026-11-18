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


def build_log(seed=0):
    """Return (display, collider): a 9 m hollow log, open at both ends, wall 0.15 m, with fungus, moss and two branch stubs."""
    rng = random.Random(seed)
    off = Vector((rng.uniform(0, 50), rng.uniform(0, 50), rng.uniform(0, 50)))
    length, r_out, r_in, n = 9.0, 0.95, 0.80, 28
    path = [Vector((-length / 2 + length * i / (n - 1), math.sin((i / (n - 1) - 0.5) * 2.4) * 0.4, r_out + 0.1)) for i in range(n)]
    wob = lambda p, a: 0.10 * noise.noise(Vector((p.x * 0.7, p.y * 0.7 + math.cos(a), p.z + math.sin(a))) + off)  # noqa: E731

    bm = bmesh.new()
    _hollow_tube(bm, path, r_out, r_in, 24, wob)
    for i in range(3, n - 3, 3):  # bracket fungus shelves along the back and sides
        for f in range(2):
            a = rng.uniform(0.2, math.pi - 0.2) + (math.pi if rng.random() < 0.5 else 0.0)
            p = path[i] + Vector((0, math.cos(a), math.sin(a))) * (r_out * 0.95) + Vector((0, 0, -0.15 + f * 0.2))
            res = bmesh.ops.create_uvsphere(bm, u_segments=8, v_segments=4, radius=1.0)
            bmesh.ops.scale(bm, vec=(0.38, 0.30, 0.07), verts=res["verts"])
            bmesh.ops.translate(bm, vec=p, verts=res["verts"])
    for k in (9, 19):  # broken branch stubs
        side = 1.0 if k == 9 else -1.0
        base = path[k] + Vector((0, 0.3 * side, r_out * 0.6))
        stub = [base, base + Vector((0, 0.5 * side, 0.9)), base + Vector((0.15, 0.7 * side, 1.7))]
        rings = kit.tube_rings(bm, stub, [0.30, 0.22, 0.17], 8)
        kit.bridge_rings(bm, rings)
        kit.cap_ring(bm, rings[-1])
    log = kit.finish_object(bm, "Log_Hollow", kit.vertex_color_material("Prop_VC"))
    cp = [path[i] for i in range(0, n, 5)] + [path[-1]]
    cbm = bmesh.new()
    _hollow_tube(cbm, cp, r_out, r_in, 8)
    col = kit.finish_object(cbm, "Log_Hollow_COL")
    kit.paint(log, lambda co, nr: _mix(_mix(BARK, BARK_DARK, 0.5 + 0.5 * math.sin(co.x * 3.0)),
                                       MOSS, min(1.0, max(0.0, (nr.z - 0.45) / 0.4))) if nr.z > 0 else BARK_DARK)
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
