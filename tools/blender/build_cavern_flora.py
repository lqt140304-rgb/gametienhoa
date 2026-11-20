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


def _smooth_path(pts, sub=2):
    """Catmull-Rom through the points, `sub` extra points per span: a curve instead of a bent rod."""
    pts = [Vector(p) for p in pts]
    if len(pts) < 3:
        return pts
    out = []
    for i in range(len(pts) - 1):
        p0, p1, p2, p3 = pts[max(0, i - 1)], pts[i], pts[i + 1], pts[min(len(pts) - 1, i + 2)]
        for k in range(sub):
            t = k / sub
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t))
    out.append(pts[-1])
    return out


def _stem(bm, pts, r0, r1, segs=5, taper=1.0, cap_start=True):
    path = _smooth_path(pts)
    gl._tube(bm, path, r0, r1, max(4, min(segs, 6)), taper, cap_start)


_JIT = random.Random(5)


STEM_BROWN = (0.30, 0.23, 0.13)
STEM_BROWN_LIGHT = (0.44, 0.34, 0.19)
_LB = [None]            # the bmesh the current builder collects its leaves in (leaves and stems are separate objects, so they get different colours)


def _begin():
    bm, lb = bmesh.new(), bmesh.new()
    _LB[0] = lb
    return bm, lb


def _paint_obj(bm, name, fn):
    o = kit.finish_object(bm, name, kit.vertex_color_material("Prop_VC"))
    kit.paint(o, fn)
    return o


def _make(name, bm, lb, stem_fn=None, leaf_fn=None):
    """The stems object (brown) and the leaves object (green) of one plant. With no stems the leaves object takes the plain name."""
    off = Vector((_JIT.uniform(0, 40), _JIT.uniform(0, 40), _JIT.uniform(0, 40)))
    stem_fn = stem_fn or (lambda co, nr: _mix(STEM_BROWN, STEM_BROWN_LIGHT, 0.5 + 0.5 * noise.noise(co * 6.0 + off)))
    leaf_fn = leaf_fn or (lambda co, nr: _mix(LEAF_DARK, LEAF_LIGHT, 0.3 + 0.7 * (0.5 + 0.5 * noise.noise(co * 7.0 + off))))
    objs = []
    has_stem, has_leaf = len(bm.verts) > 0, len(lb.verts) > 0
    if has_stem:
        objs.append(_paint_obj(bm, name, stem_fn))
    else:
        bm.free()
    if has_leaf:
        objs.append(_paint_obj(lb, name if not has_stem else name + " Leaves", leaf_fn))
    else:
        lb.free()
    return tuple(objs)


def _leaf(bm_unused, base, direction, length, width, thick=0.012, bend=0.0, roll=0.0, segs=6, rings=3):
    """A very thin leaf, added to the current plant's leaf mesh: a flat, pointed ellipsoid lying along `direction` from `base`, fullest in the middle, its edges
    gently waved, its tip drooping (+bend) or curling up (-bend), its direction and size varied a little so no two leaves are alike. `roll` turns it round its own axis."""
    from mathutils import Quaternion
    lb = _LB[0]
    segs, rings = max(segs, 6), max(rings, 3)
    res = bmesh.ops.create_uvsphere(lb, u_segments=segs, v_segments=rings, radius=1.0)
    d = (Vector(direction).normalized() + Vector((_JIT.uniform(-0.14, 0.14), _JIT.uniform(-0.14, 0.14), _JIT.uniform(-0.1, 0.1)))).normalized()
    length *= _JIT.uniform(0.82, 1.2)
    width *= _JIT.uniform(0.85, 1.15) * 0.9
    bend += _JIT.uniform(-0.12, 0.12)
    q = Vector((1, 0, 0)).rotation_difference(d)
    q = Quaternion(d, roll) @ q
    thin = thick * 0.12                                                   # a sheet of paper, not a cushion
    wave = _JIT.uniform(4.0, 7.0)
    ph = _JIT.uniform(0.0, 6.28)
    for v in res["verts"]:
        t = (v.co.x + 1.0) * 0.5
        w = width * 0.5 * math.sin(math.pi * min(1.0, max(0.0, 0.08 + 0.92 * t))) ** 0.8
        p = Vector((v.co.x * length * 0.5, v.co.y * w, v.co.z * thin))
        p.z += 0.05 * width * math.sin(v.co.x * wave + ph) * abs(v.co.y)          # waved edges
        p.z -= bend * length * t * t                                              # the tip hangs
        v.co = Vector(base) + d * (length * 0.5) + q @ p
    return res["verts"]


def _disc(lb, centre, rx, ry, thick=0.004, segs=8):
    """A flat round leaf (clover, moss pad) in the leaf mesh."""
    return gl._blob(lb, centre, (rx, ry, thick), segs, 3)


def build_fern(i):
    """A cave fern: 7 to 9 pinnate fronds arching out of a crown. Brown-green stems (the fronds' ribs), thin green leaflets that hang a little."""
    rng = random.Random(1100 + i)
    fronds = (7, 8, 9)[i]
    bm, lb = _begin()
    for k in range(fronds):
        a = k * TAU / fronds + rng.uniform(-0.2, 0.2)
        d = Vector((math.cos(a), math.sin(a), 0))
        reach = rng.uniform(0.7, 1.1) * (1.0 + 0.12 * i)
        rise = rng.uniform(0.4, 0.8)
        path = [d * (reach * (t / 7) ** 1.2) + Vector((0, 0, rise * math.sin(math.pi * 0.6 * t / 7) + 0.03)) for t in range(8)]
        _stem(bm, path, 0.018, 0.005, 4)
        side = Vector((-d.y, d.x, 0))
        for t in range(2, 8):
            c = path[t]
            tangent = (path[min(t + 1, 7)] - path[t - 1]).normalized()
            ln = (0.2 * (1.0 - t / 9.5) + 0.04)
            for sg in (1, -1):
                dirv = (side * sg * 1.0 + tangent * rng.uniform(0.35, 0.8) + Vector((0, 0, rng.uniform(-0.15, 0.2)))).normalized()
                _leaf(bm, c, dirv, ln, ln * 0.42, 0.008, bend=0.55, roll=rng.uniform(-0.5, 0.5))
    return _make("Flora_Fern_%d" % i, bm, lb)


def build_reed(i):
    """A clump of reeds: 9 to 14 pale olive stalks with long green blade leaves arching out and brown flower spikes on some."""
    rng = random.Random(1200 + i)
    bm, lb = _begin()
    n = (9, 11, 14)[i]
    for k in range(n):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.0, 0.3)
        base = Vector((math.cos(a) * d, math.sin(a) * d, 0.0))
        h = rng.uniform(1.0, 1.8)
        lean = Vector((rng.uniform(-0.15, 0.15), rng.uniform(-0.15, 0.15), 0))
        path = [base + lean * (t / 6) * h + Vector((0, 0, h * t / 6)) for t in range(7)]
        _stem(bm, path, 0.017, 0.008, 4)
        if rng.random() < 0.45:
            top = path[-1]
            gl._blob(bm, (top.x, top.y, top.z - 0.1), (0.03, 0.03, 0.13), 6, 3)
        for l in range(2):
            la = rng.uniform(0, TAU)
            _leaf(bm, base + Vector((0, 0, 0.05)), Vector((math.cos(la) * 0.45, math.sin(la) * 0.45, 0.9)), rng.uniform(0.7, 1.2), 0.05, 0.006, bend=0.55, roll=la)
    return _make("Flora_Reed_%d" % i, bm, lb, stem_fn=lambda co, nr: _mix((0.50, 0.47, 0.24), (0.62, 0.55, 0.30), min(1.0, co.z / 1.5)))


def build_moss(i):
    """A moss cushion: lumpy green mounds (the leaf object) with tiny tan spore stalks standing on them."""
    rng = random.Random(1300 + i)
    bm, lb = _begin()
    n = (3, 4, 5)[i]
    mounds = []
    for k in range(n):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.0, 0.3 + 0.1 * i)
        r = rng.uniform(0.22, 0.4)
        c = (math.cos(a) * d, math.sin(a) * d)
        verts = gl._blob(lb, (c[0], c[1], 0.02), (r, r * 0.9, r * 0.55), 9, 5)
        for v in verts:
            v.co.z = max(0.0, v.co.z)
        mounds.append((c, r))
    for k in range(18):
        c, r = rng.choice(mounds)
        a = rng.uniform(0, TAU)
        dd = rng.uniform(0.0, r * 0.8)
        x, y = c[0] + math.cos(a) * dd, c[1] + math.sin(a) * dd
        h = rng.uniform(0.1, 0.18)
        z0 = r * 0.5 * max(0.0, 1.0 - (dd / r) ** 2) * 0.9
        _stem(bm, [Vector((x, y, z0)), Vector((x, y, z0 + h))], 0.006, 0.004, 3)
        gl._blob(bm, (x, y, z0 + h + 0.012), (0.014, 0.014, 0.022), 5, 3)
    return _make("Flora_Moss_%d" % i, bm, lb, stem_fn=lambda co, nr: (0.55, 0.45, 0.28),
                 leaf_fn=lambda co, nr: _mix(MOSS, MOSS_LIGHT, min(1.0, co.z / 0.25) * 0.7 + 0.3 * (0.5 + 0.5 * noise.noise(co * 8.0))))


def build_bush(i):
    """A leafy bush: brown woody stems carrying 26 to 40 thin green leaves facing out and up, with a scatter of small buds."""
    rng = random.Random(1400 + i)
    bm, lb = _begin()
    h = (0.7, 0.95, 1.2)[i]
    leaves = (26, 32, 40)[i]
    tips = []
    for k in range(4):
        a = rng.uniform(0, TAU)
        tip = Vector((math.cos(a) * 0.3, math.sin(a) * 0.3, h * rng.uniform(0.7, 1.0)))
        _stem(bm, [Vector((0, 0, 0)), tip * 0.5 + Vector((0, 0, 0.05)), tip], 0.032, 0.009, 5)
        tips.append(tip)
    for k in range(leaves):
        tip = rng.choice(tips)
        base = tip * rng.uniform(0.35, 1.0)
        a = rng.uniform(0, TAU)
        dirv = Vector((math.cos(a), math.sin(a), rng.uniform(0.2, 0.9)))
        ln = rng.uniform(0.16, 0.26)
        _leaf(bm, base, dirv, ln, ln * 0.45, 0.01, bend=rng.uniform(0.1, 0.5), roll=rng.uniform(0, TAU))
    for k in range(6):
        tip = rng.choice(tips)
        c = tip * rng.uniform(0.6, 1.0)
        gl._blob(bm, (c.x + rng.uniform(-0.025, 0.025), c.y + rng.uniform(-0.025, 0.025), c.z + 0.02), (0.03, 0.03, 0.04), 5, 3)
    return _make("Flora_Bush_%d" % i, bm, lb)


def build_mushrooms(i):
    rng = random.Random(1500 + i)
    sb, cb = bmesh.new(), bmesh.new()
    n = (4, 6, 8)[i]
    for k in range(n):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.05, 0.4)
        x, y = math.cos(a) * d, math.sin(a) * d
        h = rng.uniform(0.12, 0.4)
        lean = rng.uniform(-0.05, 0.05)
        gl._tube(sb, [Vector((x, y, 0)), Vector((x + lean, y, h * 0.5)), Vector((x + lean * 2, y, h))], 0.03, 0.02, 6)
        gl._blob(sb, (x + lean * 2, y, h * 0.62), (0.05, 0.05, 0.012), 7, 3)
        r = rng.uniform(0.09, 0.2)
        gl._blob(cb, (x + lean * 2, y, h + 0.02), (r, r, r * 0.5), 9, 4)
    for k in range(5):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.3, 0.55)
        gl._blob(sb, (math.cos(a) * d, math.sin(a) * d, 0.02), (0.025, 0.025, 0.03), 5, 3)
    stalks = kit.finish_object(sb, "Flora_Mushrooms_%d" % i, kit.vertex_color_material("Prop_VC"))
    kit.paint(stalks, lambda co, nr: STALK)
    caps = kit.finish_object(cb, "Cluster Glow_%d" % i, kit.vertex_color_material("Prop_VC"))
    kit.paint(caps, lambda co, nr: WHITE)
    return (stalks, caps)


def build_root_arc(i):
    rng = random.Random(1600 + i)
    bm, lb = _begin()
    span = (1.6, 2.4)[i]
    height = (0.7, 1.1)[i]
    path = [Vector((span * (t / 10 - 0.5), 0.12 * math.sin(t * 0.7), -0.15 + (height + 0.15) * math.sin(math.pi * t / 10) ** 0.8)) for t in range(11)]
    _stem(bm, path, 0.13, 0.13, 8)
    for k in range(6):
        t = rng.randint(1, 9)
        a = rng.uniform(0, TAU)
        p0 = path[t]
        tw = [p0, p0 + Vector((math.cos(a) * 0.2, math.sin(a) * 0.2, 0.1)), p0 + Vector((math.cos(a) * 0.5, math.sin(a) * 0.5, -0.04)), p0 + Vector((math.cos(a) * 0.75, math.sin(a) * 0.75, -0.1))]
        _stem(bm, tw, 0.05, 0.01, 5)
    for k in range(5):
        t = rng.randint(2, 8)
        a = rng.uniform(0, TAU)
        _leaf(bm, path[t] + Vector((0, 0, 0.1)), Vector((math.cos(a), math.sin(a), 0.7)), 0.18, 0.08, 0.008, bend=0.3, roll=a)
    return _make("Flora_RootArc_%d" % i, bm, lb, stem_fn=lambda co, nr: _mix(ROOT, (0.40, 0.30, 0.20), 0.5 + 0.5 * noise.noise(co * 3.0)))


def build_vine(i):
    """Five vines hanging from z = 0 (the roof), 5 to 9 m long: thin brown stems, thin green leaves along them, curling tendrils at the ends. For the roof round the hole of light."""
    rng = random.Random(1700 + i)
    bm, lb = _begin()
    for k in range(5):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.0, 1.2)
        top = Vector((math.cos(a) * d, math.sin(a) * d, 0.0))
        length = rng.uniform(5.0, 9.0) * (1.0 + 0.15 * i)
        n = 9
        path = [top + Vector((0.25 * math.sin(t * 0.9 + k), 0.25 * math.cos(t * 0.7 + k), -length * t / (n - 1))) for t in range(n)]
        _stem(bm, path, 0.03, 0.01, 4)
        for t in range(2, n):
            c = path[t]
            for l in range(2):
                b = rng.uniform(0, TAU)
                _leaf(bm, c, Vector((math.cos(b), math.sin(b), -0.3)), rng.uniform(0.18, 0.3), 0.12, 0.008, bend=0.3, roll=b)
        e = path[-1]
        _stem(bm, [e, e + Vector((0.15, 0, -0.2)), e + Vector((0.25, 0.1, -0.15)), e + Vector((0.22, 0.2, -0.05))], 0.009, 0.004, 3)
    return _make("Flora_Vine_%d" % i, bm, lb)


def build_flower(i):
    """A cave bellflower: a thin brown-green stem with a few thin green leaves at its foot and a glowing nodding bell (its own object, "Bloom Glow")."""
    rng = random.Random(1800 + i)
    bm, lb = _begin()
    gb = bmesh.new()
    n = (2, 3, 2)[i]
    for k in range(n):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.0, 0.18)
        base = Vector((math.cos(a) * d, math.sin(a) * d, 0.0))
        h = rng.uniform(0.3, 0.55) * (1.0 + 0.15 * i)
        lean = Vector((rng.uniform(-0.1, 0.1), rng.uniform(-0.1, 0.1), 0))
        path = [base, base + Vector((0, 0, h * 0.5)) + lean * 0.5, base + Vector((0, 0, h * 0.9)) + lean, base + Vector((0.04, 0, h)) + lean * 1.2]
        _stem(bm, path, 0.011, 0.006, 4)
        e = path[-1]
        gl._blob(gb, (e.x + 0.02, e.y, e.z - 0.05), (0.045, 0.045, 0.07), 7, 4)
        gl._blob(gb, (e.x + 0.02, e.y, e.z - 0.115), (0.058, 0.058, 0.012), 7, 3)
    for k in range(4):
        a = rng.uniform(0, TAU)
        _leaf(bm, (0, 0, 0.02), Vector((math.cos(a), math.sin(a), 0.5)), 0.2, 0.07, 0.008, bend=0.4, roll=a)
    objs = list(_make("Flora_Flower_%d" % i, bm, lb, stem_fn=lambda co, nr: _mix((0.30, 0.34, 0.16), (0.40, 0.44, 0.22), min(1.0, co.z / 0.4))))
    bell = kit.finish_object(gb, "Bloom Glow_%d" % i, kit.vertex_color_material("Prop_VC"))
    kit.paint(bell, lambda co, nr: WHITE)
    return tuple(objs + [bell])


def build_clover(i):
    """A rosette of thin round green leaves on short brown-green stems (a low carpet plant)."""
    rng = random.Random(1900 + i)
    bm, lb = _begin()
    n = (10, 14)[i]
    for k in range(n):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.05, 0.24)
        h = rng.uniform(0.08, 0.2)
        x, y = math.cos(a) * d, math.sin(a) * d
        _stem(bm, [Vector((x * 0.3, y * 0.3, 0.0)), Vector((x, y, h))], 0.005, 0.0035, 3)
        r = rng.uniform(0.05, 0.09)
        _disc(lb, (x, y, h + 0.004), r, r, 0.004)
    return _make("Flora_Clover_%d" % i, bm, lb, stem_fn=lambda co, nr: (0.34, 0.38, 0.17))


def build_leaves(i):
    """A drift of fallen leaves lying on the ground: browns and golds, each turned its own way, paper thin."""
    rng = random.Random(2000 + i)
    bm, lb = _begin()
    for k in range((9, 12, 15)[i]):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.0, 0.32)
        ln = rng.uniform(0.1, 0.18)
        _leaf(bm, (math.cos(a) * d, math.sin(a) * d, 0.006 + 0.004 * k / 8), Vector((math.cos(a + 1.3), math.sin(a + 1.3), 0.0)), ln, ln * 0.5, 0.004, bend=-0.1, roll=rng.uniform(0, TAU))
    off = Vector((rng.uniform(0, 40), rng.uniform(0, 40), rng.uniform(0, 40)))
    return _make("Flora_Leaves_%d" % i, bm, lb, leaf_fn=lambda co, nr: _mix((0.40, 0.28, 0.12), (0.62, 0.48, 0.20), 0.5 + 0.5 * noise.noise(co * 14.0 + off)))


def build_twigs(i):
    """A few fallen sticks and branchlets crossed on the ground."""
    rng = random.Random(2100 + i)
    bm = bmesh.new()
    for k in range((3, 4)[i]):
        a = rng.uniform(0, TAU)
        ln = rng.uniform(0.3, 0.6)
        c = Vector((rng.uniform(-0.12, 0.12), rng.uniform(-0.12, 0.12), 0.03 + 0.02 * k))
        d = Vector((math.cos(a), math.sin(a), 0))
        p = [c - d * ln * 0.5, c - d * ln * 0.1 + Vector((0, 0, 0.02)), c + d * ln * 0.5 + Vector((0, 0, 0.01))]
        _stem(bm, p, 0.014, 0.006, 5)
        m = c + d * ln * 0.1
        s = Vector((-d.y, d.x, 0.2)).normalized()
        _stem(bm, [m, m + s * 0.12], 0.007, 0.003, 4)
    return (_paint_obj(bm, "Flora_Twigs_%d" % i, lambda co, nr: _mix((0.30, 0.22, 0.14), (0.46, 0.35, 0.24), 0.5 + 0.5 * noise.noise(co * 10.0))),)


def build_pebbles(i):
    """A cluster of small rounded stones, half buried."""
    rng = random.Random(2200 + i)
    bm = bmesh.new()
    for k in range((5, 7, 9)[i]):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.0, 0.3)
        r = rng.uniform(0.03, 0.09)
        verts = gl._blob(bm, (math.cos(a) * d, math.sin(a) * d, r * 0.25), (r * rng.uniform(0.9, 1.3), r, r * 0.65), 7, 4)
        for v in verts:
            v.co += v.co.normalized() * 0.01 * noise.noise(v.co * 20.0)
            v.co.z = max(-0.01, v.co.z)
    off = Vector((rng.uniform(0, 40), rng.uniform(0, 40), rng.uniform(0, 40)))
    return (_paint_obj(bm, "Flora_Pebbles_%d" % i, lambda co, nr: _mix((0.36, 0.35, 0.33), (0.56, 0.54, 0.50), 0.5 + 0.5 * noise.noise(co * 25.0 + off))),)


def build_lichen(i):
    """Pale grey-green lichen: a few thin irregular discs lying on the ground or on a stone."""
    rng = random.Random(2300 + i)
    bm = bmesh.new()
    for k in range((3, 5)[i]):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.0, 0.3)
        r = rng.uniform(0.1, 0.22)
        verts = gl._blob(bm, (math.cos(a) * d, math.sin(a) * d, 0.012), (r, r * rng.uniform(0.7, 1.0), 0.014), 10, 3)
        for v in verts:
            v.co.x += 0.35 * r * noise.noise(v.co * 6.0)
    off = Vector((rng.uniform(0, 40), rng.uniform(0, 40), rng.uniform(0, 40)))
    return (_paint_obj(bm, "Flora_Lichen_%d" % i, lambda co, nr: _mix((0.50, 0.58, 0.44), (0.70, 0.74, 0.58), 0.5 + 0.5 * noise.noise(co * 12.0 + off))),)


PIECES = (("Fern", 3, build_fern), ("Reed", 3, build_reed), ("Moss", 3, build_moss), ("Bush", 3, build_bush), ("Mushrooms", 3, build_mushrooms),
          ("RootArc", 2, build_root_arc), ("Vine", 2, build_vine), ("Flower", 3, build_flower), ("Clover", 2, build_clover), ("Leaves", 3, build_leaves),
          ("Twigs", 2, build_twigs), ("Pebbles", 3, build_pebbles), ("Lichen", 2, build_lichen))


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
