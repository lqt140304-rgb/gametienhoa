"""Fantasy props hanging in the dark of the vault (Blender bpy, headless): dragon skulls and a dragon ribcage, each held up by root cables.

The top of every prop (where the cables meet) is at z = 0; the bones hang below. Eyes are a separate object ("Eye Glow_N") so the game can light them.
Run:  python -I tools/blender/build_bones.py --out Assets/_Project/Models/Underroot --previews docs/art/previews
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

BONE = (0.62, 0.58, 0.46)
BONE_OLD = (0.30, 0.27, 0.20)
SOCKET = (0.02, 0.02, 0.02)
CABLE = (0.10, 0.07, 0.045)
EYE = (0.35, 1.0, 0.85)
DROP = 7.5   # the skull's centre hangs this far below the anchor


def _mix(a, b, t):
    return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))


def _blob(bm, centre, radii, segs=10, rings=7):
    """An ellipsoid; returns its vertices."""
    res = bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=1.0)
    verts = res["verts"]
    for v in verts:
        v.co = Vector((v.co.x * radii[0] + centre[0], v.co.y * radii[1] + centre[1], v.co.z * radii[2] + centre[2]))
    return verts


def _tube(bm, path, r0, r1, segs=6, taper=1.0):
    n = len(path)
    radii = [max(0.015, r0 + (r1 - r0) * (k / (n - 1)) ** taper) for k in range(n)]
    rings = kit.tube_rings(bm, path, radii, segs)
    kit.bridge_rings(bm, rings)
    kit.cap_ring(bm, rings[0], flip=True)
    kit.cap_ring(bm, rings[-1])


def _cables(bm, anchors, rng):
    """Root cables from points on the bones up to the anchor at the origin."""
    for a in anchors:
        top = Vector((rng.uniform(-0.3, 0.3), rng.uniform(-0.3, 0.3), 0.0))
        path = [a + (top - a) * (k / 7.0) + Vector((0.25 * math.sin(k * 1.1 + a.x), 0.25 * math.cos(k * 0.9 + a.y), 0)) * math.sin(math.pi * k / 7.0) for k in range(8)]
        _tube(bm, path, 0.10, 0.07, 5)


def build_skull(index):
    """A dragon skull: cranium, long snout, curved horns, eye sockets, a row of teeth, a lower jaw hanging open. About 7 m long (+x is the snout)."""
    rng = random.Random(500 + index)
    s = (1.0, 1.25)[index]
    bm = bmesh.new()
    skull_verts = []
    skull_verts += _blob(bm, (0, 0, 0), (1.7 * s, 1.35 * s, 1.25 * s))
    skull_verts += _blob(bm, (2.8 * s, 0, -0.15 * s), (2.2 * s, 0.85 * s, 0.7 * s))
    skull_verts += _blob(bm, (1.0 * s, 0.85 * s, 0.9 * s), (0.7 * s, 0.4 * s, 0.35 * s), 8, 5)    # brow ridges
    skull_verts += _blob(bm, (1.0 * s, -0.85 * s, 0.9 * s), (0.7 * s, 0.4 * s, 0.35 * s), 8, 5)
    skull_verts += _blob(bm, (2.7 * s, 0, -1.15 * s), (2.0 * s, 0.6 * s, 0.25 * s), 8, 5)       # lower jaw, hanging open
    for side in (1, -1):
        for hi in range(2 if index else 1):
            y0 = side * (0.9 + 0.35 * hi) * s
            path = [Vector((-0.5 * s - 0.5 * hi * s - 1.7 * s * k / 9, y0 + side * (0.6 + 0.5 * hi) * s * (k / 9) ** 1.3, (0.9 + 0.2 * hi) * s + 2.4 * s * math.sin(k / 9 * 1.3))) for k in range(10)]
            _tube(bm, path, 0.34 * s, 0.03, 7, 0.9)
    for k in range(9):                       # teeth along the upper snout and the lower jaw
        x = (1.5 + 0.55 * k) * s
        for side in (1, -1):
            _tube(bm, [Vector((x, side * 0.62 * s, -0.55 * s)), Vector((x, side * 0.62 * s, -0.95 * s))], 0.09 * s, 0.01, 4)
            _tube(bm, [Vector((x + 0.2, side * 0.5 * s, -0.95 * s)), Vector((x + 0.2, side * 0.5 * s, -0.55 * s - 0.05 * s))], 0.08 * s, 0.01, 4)
    for v in bm.verts:                       # a little rough, like old bone
        v.co += Vector((noise.noise(v.co * 1.7) * 0.07, noise.noise(v.co * 1.7 + Vector((9, 0, 0))) * 0.07, noise.noise(v.co * 1.7 + Vector((0, 9, 0))) * 0.07))
    anchors = [Vector((0.2 * s, 0, 1.25 * s)), Vector((-1.2 * s, 0.9 * s, 2.4 * s)), Vector((-1.2 * s, -0.9 * s, 2.4 * s)), Vector((3.4 * s, 0, 0.55 * s))]
    for v in bm.verts:
        v.co.z -= DROP
    _cables(bm, [a - Vector((0, 0, DROP)) for a in anchors], rng)
    obj = kit.finish_object(bm, "DragonSkull_%d" % index, kit.vertex_color_material("Prop_VC"))
    sockets = [Vector((1.0 * s, side * 1.1 * s, 0.25 * s - DROP)) for side in (1, -1)]

    def colour(co, nr):
        if co.z > -2.0:
            return CABLE                                        # cables and horn tips up in the roots
        for sc in sockets:
            if (co - sc).length < 0.5 * s and nr.y * (1 if sc.y > 0 else -1) > 0.0:
                return SOCKET
        return _mix(BONE, BONE_OLD, 0.5 + 0.5 * noise.noise(co * 1.3))
    kit.paint(obj, colour)
    gbm = bmesh.new()
    for sc in sockets:
        _blob(gbm, (sc.x + 0.1 * s, sc.y * 1.0, sc.z), (0.22 * s, 0.22 * s, 0.22 * s), 8, 5)
    glow = kit.finish_object(gbm, "Eye Glow_%d" % index, kit.vertex_color_material("Prop_VC"))
    kit.paint(glow, lambda co, nr: EYE)
    return (obj, glow)


def build_ribcage(index=0):
    """A dragon ribcage hung by its spine: 8 m long, 6 pairs of curved ribs."""
    rng = random.Random(600 + index)
    bm = bmesh.new()
    n = 10
    spine = [Vector((-4.0 + 8.0 * k / (n - 1), 0.0, 0.25 * math.sin(k * 0.6))) for k in range(n)]
    _tube(bm, spine, 0.28, 0.16, 7)
    for k, p in enumerate(spine):
        _blob(bm, (p.x, 0, p.z + 0.1), (0.3, 0.3, 0.3), 6, 4)
    for k in range(1, n - 1):
        size = 2.6 * math.sin(math.pi * (0.15 + 0.75 * k / (n - 1))) + 0.6
        for side in (1, -1):
            path = [Vector((spine[k].x + 0.15 * t, side * size * math.sin(t / 8 * math.pi * 0.85), spine[k].z - size * (1 - math.cos(t / 8 * math.pi * 0.85)) * 0.9)) for t in range(9)]
            _tube(bm, path, 0.15, 0.04, 6, 0.8)
    for v in bm.verts:
        v.co += Vector((noise.noise(v.co * 1.5) * 0.06, noise.noise(v.co * 1.5 + Vector((7, 0, 0))) * 0.06, 0.0))
    top = 0.6
    for v in bm.verts:
        v.co.z -= (DROP - 1.0)
    _cables(bm, [Vector((x, 0, 0.3 - (DROP - 1.0))) for x in (-3.0, 0.0, 3.0)], rng)
    obj = kit.finish_object(bm, "DragonRibcage_0", kit.vertex_color_material("Prop_VC"))
    kit.paint(obj, lambda co, nr: CABLE if co.z > -1.2 and abs(co.y) < 0.5 and co.z > -(DROP - 1.0) + 1.0 else _mix(BONE, BONE_OLD, 0.5 + 0.5 * noise.noise(co * 1.1)))
    return (obj,)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--previews", default=None)
    args = ap.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:])
    jobs = [("DragonSkull_0", lambda: build_skull(0)), ("DragonSkull_1", lambda: build_skull(1)), ("DragonRibcage_0", lambda: build_ribcage(0))]
    for name, fn in jobs:
        kit.new_scene()
        objs = list(fn())
        kit.export_fbx(objs, os.path.join(args.out, name + ".fbx"))
        if args.previews:
            os.makedirs(args.previews, exist_ok=True)
            for angle in ("front", "side"):
                kit.render_preview(objs, os.path.join(args.previews, "%s_%s.png" % (name, angle)), angle, 1.5)
        print(name, sum(kit.tri_count(o) for o in objs))


if __name__ == "__main__":
    main()
