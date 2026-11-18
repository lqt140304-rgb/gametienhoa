"""Rib segments of the root vault (Blender bpy, headless): a kit of 12 m root pieces that chain end to end.

Every piece starts at the origin heading +X with an entry mouth of radius ENTRY_RADIUS; the mouth is a plain circle (the bark relief fades out
towards both ends) so two pieces meet without a seam. `LAST_EXITS` lists the exit mouths of the last built piece as (position, direction,
radius): straight and bend have one, a fork two, a tip none; the foot ends on the ground.

Run:  python -I tools/blender/build_vault.py --out Assets/_Project/Models/Underroot --previews docs/art/previews
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

SEGMENT_LENGTH = 12.0
ENTRY_RADIUS = 1.0
EXIT_RADIUS = 0.8
FORK_RATIO = 0.6
BEND_DEGREES = 25.0
BARK_DARK = (0.05, 0.035, 0.025)
BARK = (0.16, 0.115, 0.075)
LICHEN = (0.16, 0.20, 0.10)
GLOW = (0.20, 0.42, 0.30)

LAST_EXITS = []


def _smooth(a, b, x):
    t = min(1.0, max(0.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


def _window(t):
    """0 at both ends, 1 in the middle: bark relief fades out so the mouths stay round."""
    return _smooth(0.0, 0.10, t) * _smooth(1.0, 0.90, t)


def _tube(bm, path, radii, segs, off, rough=1.0, cap_end=False):
    """A rooty tube: long rilled furrows, bumps and gentle swelling, circular at both ends."""
    n = len(path)

    def wob(p, a, _i=[0]):
        return 0.0

    rings = []
    for i, p in enumerate(path):
        t = i / (n - 1)
        w = _window(t) * rough
        _, nrm, bn = kit.ring_frame(path, i)
        ring = []
        for k in range(segs):
            a = 2 * math.pi * k / segs
            ca, sa = math.cos(a), math.sin(a)
            tw = a + t * 2.2   # the furrows twist along the root like the strands of a rope
            furrow = 1.0 - abs(noise.noise(Vector((math.cos(tw) * 3.6, math.sin(tw) * 3.6, t * 3.0)) + off))
            bump = noise.noise(Vector((ca * 1.3, sa * 1.3, t * 4.0)) + off * 0.5)
            knot = max(0.0, noise.noise(Vector((t * 3.0, ca * 0.8, sa * 0.8)) + off * 2.3) - 0.35) * 1.4
            r = radii[i] * (1.0 + w * (0.20 * furrow + 0.12 * bump + 0.18 * knot - 0.14))
            ring.append(bm.verts.new(p + (nrm * ca + bn * sa) * r))
        rings.append(ring)
    kit.bridge_rings(bm, rings)
    if cap_end:
        kit.cap_ring(bm, rings[-1])
    return rings


def _strand(bm, path, radii, off, phase, turns, offset, rad, segs=10, cap_end=False):
    """One strand winding round the core: starts and ends at the same angle when `turns` is a whole number, so pieces chain."""
    n = len(path)
    rings = []
    for i, p in enumerate(path):
        t = i / (n - 1)
        r = radii[i]
        _, nrm, bn = kit.ring_frame(path, i)
        th = phase + 2 * math.pi * turns * t
        c = p + (nrm * math.cos(th) + bn * math.sin(th)) * offset * r
        w = _window(t)
        _, n2, b2 = kit.ring_frame(path, i)
        ring = []
        for k in range(segs):
            a = 2 * math.pi * k / segs
            lump = noise.noise(Vector((math.cos(a) * 2, math.sin(a) * 2, t * 6.0)) + off + Vector((phase, 0, 0)))
            rr = rad * r * (1.0 + w * (0.22 * lump - 0.05))
            ring.append(bm.verts.new(c + (n2 * math.cos(a) + b2 * math.sin(a)) * rr))
        rings.append(ring)
    kit.bridge_rings(bm, rings)
    if cap_end:
        kit.cap_ring(bm, rings[-1])


def _root(bm, path, radii, off, cap_end=False, strands=3, turns=1):
    """A root as a twisted cable: a core and three strands winding round it (envelope radius = radii[i], so mouths match)."""
    core = [0.66 * r for r in radii]
    _tube(bm, path, core, 18, off, rough=1.3, cap_end=cap_end)
    for k in range(strands):
        _strand(bm, path, radii, off, 2 * math.pi * k / strands, turns, 0.60, 0.40, cap_end=cap_end)


def _straight_path(n=24, wiggle=0.15, rng=None):
    rng = rng or random.Random(0)
    ph = rng.uniform(0, 6.28)
    return [Vector((SEGMENT_LENGTH * i / (n - 1), wiggle * math.sin(math.pi * i / (n - 1)) * math.sin(ph + i * 0.4),
                    wiggle * math.sin(math.pi * i / (n - 1)) * math.cos(ph + i * 0.3))) for i in range(n)]


def _tendrils(bm, path, radii, rng, off, count):
    """Thin side roots clinging to and hanging from the main root."""
    n = len(path)
    for _ in range(count):
        i = rng.randint(3, n - 4)
        a = rng.uniform(0, 2 * math.pi)
        p = path[i]
        _, nrm, bn = kit.ring_frame(path, i)
        out = (nrm * math.cos(a) + bn * math.sin(a))
        base = p + out * radii[i] * 0.85
        ln = rng.uniform(1.0, 3.2)
        tip = base + out * rng.uniform(0.2, 0.7) + Vector((rng.uniform(-0.6, 0.6), rng.uniform(-0.4, 0.4), -ln))
        mid = (base + tip) * 0.5 + out * 0.25
        r0 = rng.uniform(0.07, 0.16)
        rings = kit.tube_rings(bm, [base, mid, tip], [r0, r0 * 0.7, 0.015], 6)
        kit.bridge_rings(bm, rings)
        kit.cap_ring(bm, rings[-1])


def _radii(n, r0, r1):
    return [r0 + (r1 - r0) * i / (n - 1) for i in range(n)]


def _paint(obj, off):
    def colour(co, nr):
        n1 = 0.5 + 0.5 * noise.noise(co * 0.9 + off)
        n2 = 0.5 + 0.5 * noise.noise(co * 3.5 + off)
        base = _mix(BARK_DARK, BARK, 0.3 + 0.7 * n2)
        lichen = max(0.0, n1 - 0.58) * 2.2 * (0.4 + 0.6 * max(0.0, nr.z))
        base = _mix(base, LICHEN, min(1.0, lichen))
        glow = max(0.0, noise.noise(co * 1.6 + off * 2.0) - 0.62) * 3.0 * max(0.0, -nr.z)   # faint luminous lichen on the underside
        return _mix(base, GLOW, min(0.8, glow))

    kit.paint(obj, colour)


def _mix(a, b, t):
    return tuple(a[i] * (1 - t) + b[i] * t for i in range(3))


def build_segment(kind, seed):
    """Return (display, collider_or_None). kind: 'straight', 'bend', 'fork', 'tip', 'foot'. Fills LAST_EXITS."""
    global LAST_EXITS
    LAST_EXITS = []
    rng = random.Random(seed * 7 + len(kind))
    off = Vector((rng.uniform(0, 60), rng.uniform(0, 60), rng.uniform(0, 60)))
    bm = bmesh.new()
    segs = 20
    col = None
    if kind == "straight":
        path = _straight_path(rng=rng)
        _root(bm, path, _radii(len(path), ENTRY_RADIUS, EXIT_RADIUS), off)
        _tendrils(bm, path, _radii(len(path), ENTRY_RADIUS, EXIT_RADIUS), rng, off, 6)
        LAST_EXITS = [(path[-1], Vector((1, 0, 0)), EXIT_RADIUS)]
    elif kind == "bend":
        n = 24
        ang = math.radians(BEND_DEGREES)
        rc = SEGMENT_LENGTH / ang
        path = [Vector((rc * math.sin(ang * i / (n - 1)), rc * (1 - math.cos(ang * i / (n - 1))), 0.0)) for i in range(n)]
        _root(bm, path, _radii(n, ENTRY_RADIUS, EXIT_RADIUS), off)
        _tendrils(bm, path, _radii(n, ENTRY_RADIUS, EXIT_RADIUS), rng, off, 6)
        LAST_EXITS = [(path[-1], Vector((math.cos(ang), math.sin(ang), 0)), EXIT_RADIUS)]
    elif kind == "fork":
        path = _straight_path(wiggle=0.1, rng=rng)
        _root(bm, path, _radii(len(path), ENTRY_RADIUS, EXIT_RADIUS), off)
        a = math.radians(rng.choice((-1, 1)) * 38.0)
        d = Vector((math.cos(a), math.sin(a), 0.0))
        start = path[7] * 1.0
        bp = [start + d * (SEGMENT_LENGTH - 4.0) * i / 15 for i in range(16)]
        rb = _radii(16, EXIT_RADIUS * 0.95, EXIT_RADIUS * FORK_RATIO)
        _root(bm, bp, rb, off * 1.7)
        _tendrils(bm, path, _radii(len(path), ENTRY_RADIUS, EXIT_RADIUS), rng, off, 4)
        LAST_EXITS = [(path[-1], Vector((1, 0, 0)), EXIT_RADIUS), (bp[-1], d, EXIT_RADIUS * FORK_RATIO)]
    elif kind == "tip":
        path = _straight_path(n=20, wiggle=0.25, rng=rng)
        radii = [ENTRY_RADIUS * (1.0 - 0.8 * (i / 19) ** 0.8) for i in range(20)]
        _root(bm, path, radii, off, cap_end=True)
    elif kind == "foot":
        n = 24
        path = []
        for i in range(n):
            t = i / (n - 1)
            ang = (t ** 1.6) * math.radians(80.0)   # starts straight, then bends down to the ground and flares out
            path.append(Vector((7.0 * math.sin(ang), 0.0, -7.0 * (1 - math.cos(ang)) * 1.25)))
        radii = [ENTRY_RADIUS * (1.0 + 1.0 * (i / (n - 1)) ** 3) for i in range(n)]
        _root(bm, path, radii, off, cap_end=True)
    else:
        raise ValueError(kind)
    name = "Rib_%s_%d" % (kind.capitalize(), seed)
    obj = kit.finish_object(bm, name, kit.vertex_color_material("Prop_VC"))
    _paint(obj, off)
    if kind == "foot":
        cbm = bmesh.new()
        lowest = min(v.co.z for v in obj.data.vertices)
        leg = [Vector((6.5, 0, lowest)), Vector((6.5, 0, lowest * 0.5)), Vector((5.5, 0, 0.0))]
        rings = kit.tube_rings(cbm, leg, [1.7, 1.3, 1.0], 8)
        kit.bridge_rings(cbm, rings)
        kit.cap_ring(cbm, rings[0], flip=True)
        kit.cap_ring(cbm, rings[-1])
        col = kit.finish_object(cbm, name + "_COL")
    return obj, col


KINDS = (("straight", 3), ("bend", 3), ("fork", 2), ("tip", 2), ("foot", 2))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--previews", default=None)
    args = ap.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:])
    for kind, count in KINDS:
        for seed in range(count):
            kit.new_scene()
            obj, col = build_segment(kind, seed)
            objs = [obj] + ([col] if col else [])
            kit.export_fbx(objs, os.path.join(args.out, obj.name + ".fbx"))
            if args.previews and seed == 0:
                os.makedirs(args.previews, exist_ok=True)
                kit.render_preview(objs, os.path.join(args.previews, obj.name + "_front.png"), "front", 2.4)
                kit.render_preview(objs, os.path.join(args.previews, obj.name + "_top.png"), "top", 2.4)
            print(obj.name, kit.tri_count(obj))


if __name__ == "__main__":
    main()
