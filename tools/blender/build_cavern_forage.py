"""Three more absorbable plants for the Slime Cavern (Blender bpy, headless): moss clump, ember berry bush, spore pod. Each is one FBX named Forage_<Name>;
the self-lit parts are a separate object whose name contains "Glow" (the game gives it a glowing material). Origin on the ground, +z up. No colliders.
(The other nine edible kinds of the cavern are in build_forage.py and the rocks in build_rocks.py.)

Run:  python -I tools/blender/build_cavern_forage.py --out Assets/_Project/Models/Underroot --previews docs/art/previews
"""
import argparse
import math
import os
import random
import sys

import bpy  # must come before bmesh/mathutils
import bmesh
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_common as kit  # noqa: E402
import build_groundlife as gl  # noqa: E402

TAU = 6.2832


def _mix(a, b, t):
    return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))


def _vc(bm, name, fn):
    o = kit.finish_object(bm, name, kit.vertex_color_material("Prop_VC"))
    kit.paint(o, fn)
    return o


def _leaf(bm, base, direction, length, width, curl, rng, segs=5):
    """A curved, tapered leaf blade (two-sided: a copy with reversed winding a hair above) growing from `base` along `direction` (+z is up)."""
    d = Vector(direction).normalized()
    side = Vector((-d.y, d.x, 0.0))
    if side.length < 1e-4:
        side = Vector((1.0, 0.0, 0.0))
    side.normalize()
    up = d.cross(side).normalized()
    rows = []
    for i in range(segs + 1):
        t = i / segs
        mid = Vector(base) + d * (length * t) + Vector((0, 0, -curl * length * t * t))        # the blade droops toward its tip
        w = width * 0.5 * (math.sin(math.pi * min(1.0, t * 0.92 + 0.04)) ** 0.7) * (1.0 - 0.15 * t)
        fold = up * (0.12 * w)                                                                   # the midrib stands a little proud
        rows.append((mid + side * w, mid + fold, mid - side * w))
    for flip in (False, True):
        off = Vector((0, 0, 0.002 if flip else 0.0))
        vr = [[bm.verts.new(tuple(v + off)) for v in row] for row in rows]
        for i in range(segs):
            for j in range(2):
                quad = [vr[i][j], vr[i][j + 1], vr[i + 1][j + 1], vr[i + 1][j]]
                bm.faces.new(quad[::-1] if flip else quad)


def build_glow_moss():
    """Low clumps of soft moss with pale glowing spore bulbs on top."""
    rng = random.Random(21)
    mb, gb = bmesh.new(), bmesh.new()
    for k in range(7):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.0, 0.32)
        r = rng.uniform(0.14, 0.26)
        gl._blob(mb, (math.cos(a) * d, math.sin(a) * d, 0.04), (r, r * 0.9, 0.06 + 0.04 * rng.random()), 9, 4)
    for k in range(9):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.0, 0.34)
        gl._blob(gb, (math.cos(a) * d, math.sin(a) * d, 0.12 + 0.05 * rng.random()), (0.035, 0.035, 0.035), 6, 4)
    moss = _vc(mb, "Forage_MossClump", lambda co, nr: _mix((0.08, 0.2, 0.08), (0.2, 0.42, 0.16), min(1.0, co.z / 0.11)))
    glow = _vc(gb, "MossClump Glow", lambda co, nr: (1.0, 1.0, 1.0))
    return (moss, glow)


def build_ember_berry():
    """A small low bush: thin branches, flat leaves, and bright berries (the glowing part)."""
    rng = random.Random(22)
    bb, gb = bmesh.new(), bmesh.new()
    for k in range(7):
        a = k * TAU / 7 + rng.uniform(-0.2, 0.2)
        d = Vector((math.cos(a), math.sin(a), 0.0))
        reach = rng.uniform(0.25, 0.5)
        h = rng.uniform(0.45, 0.7)
        path = [Vector((0, 0, 0.02)), d * reach * 0.4 + Vector((0, 0, h * 0.55)), d * reach + Vector((0, 0, h))]
        gl._tube(bb, path, 0.02, 0.008, 4)
        side = Vector((-d.y, d.x, 0))
        for t in (0.35, 0.6, 0.85):                              # leaves in pairs all along the branch
            c = path[0].lerp(path[1], min(1.0, t * 2.0)) if t < 0.5 else path[1].lerp(path[2], (t - 0.5) * 2.0)
            for sg in (1, -1):
                ld = (d * 0.5 + side * sg * 0.85 + Vector((0, 0, 0.3 + 0.25 * rng.random()))).normalized()
                _leaf(bb, c, ld, rng.uniform(0.15, 0.23), rng.uniform(0.08, 0.12), 0.4, rng)
        _leaf(bb, path[-1], (d * 0.6 + Vector((0, 0, 0.8))).normalized(), 0.18, 0.09, 0.3, rng)
        tip = path[-1]
        for j in range(2):
            gl._blob(gb, (tip.x + rng.uniform(-0.05, 0.05), tip.y + rng.uniform(-0.05, 0.05), tip.z + rng.uniform(-0.1, 0.0)), (0.042, 0.042, 0.042), 6, 4)
    bush = _vc(bb, "Forage_EmberBerry", lambda co, nr: _mix((0.22, 0.14, 0.08), (0.14, 0.3, 0.1), min(1.0, co.z / 0.4)))
    glow = _vc(gb, "EmberBerry Glow", lambda co, nr: (1.0, 1.0, 1.0))
    return (bush, glow)


def build_spore_pod():
    """Three to four slim stalks, each ending in a teardrop pod; the pods glow."""
    rng = random.Random(23)
    sb, pb = bmesh.new(), bmesh.new()
    for k in range(4):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.0, 0.28)
        x, y = math.cos(a) * d, math.sin(a) * d
        h = rng.uniform(0.3, 0.55)
        lean = Vector((rng.uniform(-0.08, 0.08), rng.uniform(-0.08, 0.08), 0.0))
        path = [Vector((x, y, 0.0)), Vector((x, y, h * 0.5)) + lean * 0.5, Vector((x, y, h)) + lean]
        gl._tube(sb, path, 0.035, 0.02, 5)
        top = path[-1]
        gl._blob(pb, (top.x, top.y, top.z + 0.08), (0.075, 0.075, 0.13), 8, 6)
    stalks = _vc(sb, "Forage_SporePod", lambda co, nr: _mix((0.10, 0.12, 0.08), (0.18, 0.26, 0.2), min(1.0, co.z / 0.5)))
    pods = _vc(pb, "SporePod Glow", lambda co, nr: (1.0, 1.0, 1.0))
    return (stalks, pods)


PIECES = (("MossClump", build_glow_moss), ("EmberBerry", build_ember_berry), ("SporePod", build_spore_pod))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--previews", default=None)
    args = ap.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:])
    for name, fn in PIECES:
        kit.new_scene()
        objs = list(fn())
        kit.export_fbx(objs, os.path.join(args.out, "Forage_%s.fbx" % name))
        if args.previews:
            os.makedirs(args.previews, exist_ok=True)
            kit.render_preview(objs, os.path.join(args.previews, "Forage_%s_front.png" % name), "front", 3.0)
        print(name, sum(kit.tri_count(o) for o in objs))


if __name__ == "__main__":
    main()
