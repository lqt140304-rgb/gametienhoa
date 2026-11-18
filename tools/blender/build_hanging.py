"""Props that hang from the root vault (Blender bpy, headless): long roots in four lengths, thin root curtains, glowing pods, limestone stalactites.

Every prop's top is at z = 0 (hang it from the roof) and points down (-z). No colliders: they are up in the dark.
Run:  python -I tools/blender/build_hanging.py --out Assets/_Project/Models/Underroot --previews docs/art/previews
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

ROOT_LENGTHS = (4.0, 8.0, 14.0, 20.0)
ROOT = (0.13, 0.095, 0.06)
ROOT_DARK = (0.05, 0.035, 0.025)
LIME = (0.55, 0.52, 0.45)
LIME_DARK = (0.28, 0.26, 0.23)
POD = (0.10, 0.16, 0.12)
GLOW = (0.45, 0.95, 0.75)


def _mix(a, b, t):
    return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))


def _off(rng):
    return Vector((rng.uniform(0, 80), rng.uniform(0, 80), rng.uniform(0, 80)))


def build_root(index):
    """A root of ROOT_LENGTHS[index] m: thick at the roof, tapering to a thread, with side roots that grow with its length."""
    length = ROOT_LENGTHS[index]
    rng = random.Random(100 + index)
    off = _off(rng)
    n = max(8, int(length * 1.6))
    bm = bmesh.new()
    r0 = 0.18 + length * 0.016
    path = [Vector((0.35 * math.sin(k * 0.4 + index) * (k / (n - 1)) * min(2.0, length / 6.0), 0.3 * math.cos(k * 0.33 + index) * (k / (n - 1)),
                    -length * k / (n - 1))) for k in range(n)]
    radii = [max(0.02, r0 * (1.0 - k / (n - 1)) ** 1.3 + 0.02) for k in range(n)]
    rings = kit.tube_rings(bm, path, radii, 8, lambda p, a: 0.16 * noise.noise(Vector((p.z * 0.9, math.cos(a), math.sin(a))) + off))
    kit.bridge_rings(bm, rings)
    kit.cap_ring(bm, rings[0], flip=True)
    kit.cap_ring(bm, rings[-1])
    for t in range(2 + index):
        k0 = rng.randint(2, max(3, n // 3))
        b0 = path[k0]
        d = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), 0)).normalized()
        reach = 0.12 * length + 0.8
        side = [b0, b0 + d * reach * 0.4 + Vector((0, 0, -reach * 0.3)), b0 + d * reach * 0.8 + Vector((0, 0, -reach * 0.7)), b0 + d * reach + Vector((0, 0, -reach * 1.1))]
        sr = kit.tube_rings(bm, side, [radii[k0] * 0.55, radii[k0] * 0.4, radii[k0] * 0.2, 0.02], 6)
        kit.bridge_rings(bm, sr)
        kit.cap_ring(bm, sr[-1])
    obj = kit.finish_object(bm, "VaultRoot_%d" % index, kit.vertex_color_material("Prop_VC"))
    kit.paint(obj, lambda co, nr: _mix(ROOT, (0.16, 0.12, 0.08), 0.5 + 0.5 * noise.noise(co * 3.0 + off)) if co.z < -0.5 else ROOT_DARK)
    return (obj,)


def build_curtain(index):
    """A curtain of thin hanging roots side by side along x (6 m wide), 3 to 6 m long, tied at the top by a thin cross root."""
    rng = random.Random(200 + index)
    off = _off(rng)
    bm = bmesh.new()
    strands = 13
    for s in range(strands):
        x = (s / (strands - 1) - 0.5) * 6.0 + rng.uniform(-0.12, 0.12)
        y = rng.uniform(-0.25, 0.25)
        length = rng.uniform(3.0, 6.0) * (1.0 - 0.35 * abs(x) / 3.0)
        n = 7
        path = [Vector((x + 0.15 * math.sin(k * 0.8 + s), y, -length * k / (n - 1))) for k in range(n)]
        radii = [max(0.015, 0.07 * (1.0 - k / (n - 1)) + 0.012) for k in range(n)]
        rings = kit.tube_rings(bm, path, radii, 5)
        kit.bridge_rings(bm, rings)
        kit.cap_ring(bm, rings[0], flip=True)
        kit.cap_ring(bm, rings[-1])
    cross = [Vector((-3.1 + 6.2 * k / 8, 0.0, 0.05 * math.sin(k))) for k in range(9)]
    cr = kit.tube_rings(bm, cross, [0.10] * 9, 6)
    kit.bridge_rings(bm, cr)
    kit.cap_ring(bm, cr[0], flip=True)
    kit.cap_ring(bm, cr[-1])
    obj = kit.finish_object(bm, "VaultCurtain_%d" % index, kit.vertex_color_material("Prop_VC"))
    kit.paint(obj, lambda co, nr: _mix(ROOT, (0.20, 0.17, 0.10), 0.5 + 0.5 * noise.noise(co * 2.0 + off)))
    return (obj,)


def build_pod(index):
    """A luminous pod on a stalk: a 4 m long bag, the glowing mouth is its own object ("Pod Glow_N") so the game can light it."""
    rng = random.Random(300 + index)
    off = _off(rng)
    size = (0.8, 1.0, 1.25)[index]
    n = 14
    length = 4.0
    path = [Vector((0.2 * math.sin(k * 0.5 + index) * (k / (n - 1)), 0.0, -length * k / (n - 1))) for k in range(n)]
    radii = []
    for k in range(n):
        t = k / (n - 1)
        stalk = 0.06 + 0.04 * (1.0 - t)
        bag = 0.62 * size * math.sin(math.pi * min(1.0, max(0.0, (t - 0.3) / 0.7))) ** 0.7
        radii.append(max(0.03, stalk + bag))
    bm = bmesh.new()
    rings = kit.tube_rings(bm, path, radii, 9, lambda p, a: 0.10 * noise.noise(Vector((p.z * 1.2, math.cos(a), math.sin(a))) + off))
    kit.bridge_rings(bm, rings)
    kit.cap_ring(bm, rings[0], flip=True)
    kit.cap_ring(bm, rings[-1])
    pod = kit.finish_object(bm, "VaultPod_%d" % index, kit.vertex_color_material("Prop_VC"))
    kit.paint(pod, lambda co, nr: _mix(POD, (0.07, 0.30, 0.22), max(0.0, min(1.0, (-co.z - 2.2) / 1.8))))
    gbm = bmesh.new()
    gpath = [path[-1] + Vector((0, 0, 0.55 - 0.55 * k / 5.0 * 1.0)) for k in range(6)]
    gr = [0.01 + 0.30 * size * math.sin(math.pi * k / 5.0) ** 0.6 for k in range(6)]
    grings = kit.tube_rings(gbm, gpath, gr, 7)
    kit.bridge_rings(gbm, grings)
    kit.cap_ring(gbm, grings[0], flip=True)
    kit.cap_ring(gbm, grings[-1])
    glow = kit.finish_object(gbm, "Pod Glow_%d" % index, kit.vertex_color_material("Prop_VC"))
    kit.paint(glow, lambda co, nr: GLOW)
    return (pod, glow)


def build_stalactite(index):
    """A limestone stalactite 3, 4.5 or 6 m long: a ridged cone, pale, with a darker tip."""
    rng = random.Random(400 + index)
    off = _off(rng)
    length = (3.0, 4.5, 6.0)[index]
    n = 12
    path = [Vector((0.15 * math.sin(k * 0.6 + index), 0.1 * math.cos(k * 0.5), -length * k / (n - 1))) for k in range(n)]
    radii = [max(0.02, (0.35 + 0.10 * index) * (1.0 - k / (n - 1)) ** 1.1 + 0.02) for k in range(n)]
    bm = bmesh.new()
    rings = kit.tube_rings(bm, path, radii, 9, lambda p, a: 0.22 * noise.noise(Vector((p.z * 0.7, 2 * math.cos(a), 2 * math.sin(a))) + off))
    kit.bridge_rings(bm, rings)
    kit.cap_ring(bm, rings[0], flip=True)
    kit.cap_ring(bm, rings[-1])
    obj = kit.finish_object(bm, "VaultStalactite_%d" % index, kit.vertex_color_material("Prop_VC"))
    kit.paint(obj, lambda co, nr: _mix(LIME, LIME_DARK, min(1.0, max(0.0, (-co.z) / length)) * 0.8 + 0.2 * noise.noise(co * 2.0 + off)))
    return (obj,)


PIECES = (("VaultRoot", 4, build_root), ("VaultCurtain", 2, build_curtain), ("VaultPod", 3, build_pod), ("VaultStalactite", 3, build_stalactite))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--previews", default=None)
    args = ap.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:])
    for prefix, count, fn in PIECES:
        for i in range(count):
            kit.new_scene()
            objs = fn(i)
            kit.export_fbx(list(objs), os.path.join(args.out, "%s_%d.fbx" % (prefix, i)))
            if args.previews:
                os.makedirs(args.previews, exist_ok=True)
                kit.render_preview(list(objs), os.path.join(args.previews, "%s_%d_front.png" % (prefix, i)), "front", 1.6)
            print(prefix, i, sum(kit.tri_count(o) for o in objs))


if __name__ == "__main__":
    main()
