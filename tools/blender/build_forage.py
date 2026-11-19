"""Absorbable plants and minerals of the Underroot lake (Blender bpy, headless). Slimes eat them: water lily, cattail, bitter fern, fat mushroom,
sweet root, ice bloom (plants) and lime crystal, iron nodule, salt crust, glow quartz (minerals). Each is one FBX named Forage_<Name>;
glow quartz also has a separate "Quartz Glow" object (the game gives it the self-lit material). Origin on the ground, +z up. No colliders.

Run:  python -I tools/blender/build_forage.py --out Assets/_Project/Models/Underroot --previews docs/art/previews
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
    return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))


def _obj(bm, name, fn):
    o = kit.finish_object(bm, name, kit.vertex_color_material("Prop_VC"))
    kit.paint(o, fn)
    return o


def build_water_lily():
    rng = random.Random(1)
    bm = bmesh.new()
    for k in range(5):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.15, 0.55)
        r = rng.uniform(0.3, 0.5)
        gl._blob(bm, (math.cos(a) * d, math.sin(a) * d, 0.03), (r, r, 0.035), 10, 3)
    for k in range(9):                                   # petals
        a = k * TAU / 9
        gl._blob(bm, (math.cos(a) * 0.11, math.sin(a) * 0.11, 0.2), (0.15, 0.05, 0.13), 6, 4)
    gl._blob(bm, (0, 0, 0.14), (0.07, 0.07, 0.07), 6, 4)
    return (_obj(bm, "Forage_WaterLily", lambda co, nr: (0.9, 0.6, 0.7) if co.z > 0.1 and (co.x ** 2 + co.y ** 2) > 0.012 else ((0.95, 0.8, 0.2) if co.z > 0.1 else (0.10, 0.28, 0.10))),)


def build_cattail():
    rng = random.Random(2)
    bm = bmesh.new()
    for k in range(5):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.0, 0.3)
        h = rng.uniform(1.2, 1.8)
        base = Vector((math.cos(a) * d, math.sin(a) * d, 0.0))
        lean = Vector((rng.uniform(-0.12, 0.12), rng.uniform(-0.12, 0.12), 0))
        path = [base + lean * (i / 6) * h + Vector((0, 0, h * i / 6)) for i in range(7)]
        gl._tube(bm, path, 0.03, 0.015, 5)
        top = path[-1]
        gl._blob(bm, (top.x, top.y, top.z - 0.15), (0.05, 0.05, 0.17), 6, 4)       # the brown head
        for l in range(2):                                                            # a long leaf
            la = rng.uniform(0, TAU)
            leaf = [base + Vector((0, 0, 0.05)), base + Vector((math.cos(la) * 0.25, math.sin(la) * 0.25, 0.7)), base + Vector((math.cos(la) * 0.55, math.sin(la) * 0.55, 0.95))]
            gl._tube(bm, leaf, 0.03, 0.008, 4)
    return (_obj(bm, "Forage_Cattail", lambda co, nr: (0.28, 0.16, 0.08) if 1.0 < co.z and nr.z < 0.5 and (co.x ** 2 + co.y ** 2) > 0.0 and co.z < 1.9 and abs(nr.z) < 0.5 and co.z > 0.9 else _mix((0.10, 0.20, 0.06), (0.30, 0.40, 0.12), min(1.0, co.z / 1.2))),)


def build_bitter_fern():
    rng = random.Random(3)
    bm = bmesh.new()
    for k in range(7):
        a = k * TAU / 7 + rng.uniform(-0.2, 0.2)
        d = Vector((math.cos(a), math.sin(a), 0))
        reach = rng.uniform(0.7, 1.1)
        path = [d * (reach * (i / 7) ** 1.2) + Vector((0, 0, 0.6 * math.sin(math.pi * 0.55 * i / 7) * (1.0 + 0.2 * rng.random()) + 0.02)) for i in range(8)]
        gl._tube(bm, path, 0.025, 0.01, 4)
        side = Vector((-d.y, d.x, 0))
        for i in range(2, 8):
            c = path[i]
            s = 0.1 * (1.0 - i / 9.0) + 0.05
            for sg in (1, -1):
                gl._blob(bm, tuple(c + side * sg * s * 1.1), (s * 1.2, s * 0.35, s * 0.08), 6, 3)
    return (_obj(bm, "Forage_BitterFern", lambda co, nr: _mix((0.06, 0.16, 0.05), (0.22, 0.34, 0.10), min(1.0, co.z / 0.6)) if nr.z > -0.5 else (0.04, 0.10, 0.04)),)


def build_fat_mushroom():
    rng = random.Random(4)
    sb, cb = bmesh.new(), bmesh.new()
    for k in range(3):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.0, 0.35)
        x, y = math.cos(a) * d, math.sin(a) * d
        h = rng.uniform(0.3, 0.5)
        gl._tube(sb, [Vector((x, y, 0)), Vector((x, y, h * 0.5)), Vector((x, y, h))], 0.1, 0.07, 7)
        gl._blob(cb, (x, y, h + 0.04), (0.28, 0.28, 0.16), 10, 5)
    stalk = kit.finish_object(sb, "Forage_FatMushroom", kit.vertex_color_material("Prop_VC"))
    kit.paint(stalk, lambda co, nr: (0.62, 0.55, 0.40))
    caps = kit.finish_object(cb, "Forage_FatMushroom Cap", kit.vertex_color_material("Prop_VC"))
    kit.paint(caps, lambda co, nr: _mix((0.55, 0.30, 0.10), (0.85, 0.65, 0.32), max(0.0, min(1.0, (co.z - 0.35) / 0.2))))
    return (stalk, caps)


def build_sweet_root():
    rng = random.Random(5)
    bm = bmesh.new()
    gl._blob(bm, (0, 0, 0.14), (0.22, 0.2, 0.26), 9, 6)                       # the tuber, half buried
    gl._blob(bm, (0.2, 0.08, 0.1), (0.12, 0.11, 0.15), 7, 5)
    for k in range(5):
        a = rng.uniform(0, TAU)
        path = [Vector((0, 0, 0.3)), Vector((math.cos(a) * 0.15, math.sin(a) * 0.15, 0.55)), Vector((math.cos(a) * 0.4, math.sin(a) * 0.4, 0.65))]
        gl._tube(bm, path, 0.03, 0.01, 4)
        gl._blob(bm, (path[-1].x, path[-1].y, path[-1].z), (0.13, 0.09, 0.03), 6, 3)
    return (_obj(bm, "Forage_SweetRoot", lambda co, nr: _mix((0.70, 0.38, 0.12), (0.50, 0.25, 0.08), max(0.0, 1.0 - co.z * 3.0)) if co.z < 0.36 else (0.12, 0.30, 0.10)),)


def build_ice_bloom():
    rng = random.Random(6)
    bm = bmesh.new()
    for k in range(7):
        a = k * TAU / 7 + rng.uniform(-0.15, 0.15)
        tilt = rng.uniform(0.35, 0.8)
        h = rng.uniform(0.5, 0.85)
        dirv = Vector((math.cos(a) * math.sin(tilt), math.sin(a) * math.sin(tilt), math.cos(tilt)))
        path = [Vector((0, 0, 0.05)), dirv * h * 0.8 + Vector((0, 0, 0.05)), dirv * h + Vector((0, 0, 0.05))]
        rings = kit.tube_rings(bm, path, [0.07, 0.06, 0.01], 5)
        kit.bridge_rings(bm, rings)
        kit.cap_ring(bm, rings[0], flip=True)
        kit.cap_ring(bm, rings[-1])
    gl._blob(bm, (0, 0, 0.3), (0.1, 0.1, 0.16), 7, 5)
    return (_obj(bm, "Forage_IceBloom", lambda co, nr: _mix((0.45, 0.65, 0.80), (0.85, 0.95, 1.0), min(1.0, co.z / 0.7))),)


def build_lime_crystal():
    rng = random.Random(7)
    rb, cb = bmesh.new(), bmesh.new()
    gl._blob(rb, (0, 0, 0.08), (0.6, 0.5, 0.22), 9, 4)
    for k in range(6):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.0, 0.3)
        base = Vector((math.cos(a) * d, math.sin(a) * d, 0.1))
        tilt = rng.uniform(0.1, 0.5)
        dirv = Vector((math.cos(a) * tilt, math.sin(a) * tilt, 1.0)).normalized()
        h = rng.uniform(0.5, 1.15)
        path = [base, base + dirv * h * 0.85, base + dirv * h]
        rings = kit.tube_rings(cb, path, [0.09, 0.085, 0.012], 6)
        kit.bridge_rings(cb, rings)
        kit.cap_ring(cb, rings[0], flip=True)
        kit.cap_ring(cb, rings[-1])
    rock = kit.finish_object(rb, "Forage_LimeCrystal", kit.vertex_color_material("Prop_VC"))
    kit.paint(rock, lambda co, nr: (0.30, 0.28, 0.25))
    cry = kit.finish_object(cb, "Forage_LimeCrystal Spikes", kit.vertex_color_material("Prop_VC"))
    kit.paint(cry, lambda co, nr: _mix((0.70, 0.68, 0.62), (0.95, 0.93, 0.88), min(1.0, co.z / 1.0)))
    return (rock, cry)


def build_iron_nodule():
    rng = random.Random(8)
    bm = bmesh.new()
    verts = gl._blob(bm, (0, 0, 0.45), (0.65, 0.55, 0.5), 12, 8)
    for v in verts:
        v.co += v.co.normalized() * 0.18 * (noise.noise(v.co * 2.2) )
    for k in range(4):
        a = rng.uniform(0, TAU)
        gl._blob(bm, (math.cos(a) * 0.55, math.sin(a) * 0.5, rng.uniform(0.15, 0.4)), (0.22, 0.2, 0.2), 8, 5)
    off = Vector((rng.uniform(0, 40), rng.uniform(0, 40), rng.uniform(0, 40)))

    def colour(co, nr):
        band = 0.5 + 0.5 * math.sin(co.z * 14.0 + 3.0 * noise.noise(co * 2.0 + off))     # rusty veins
        return _mix((0.13, 0.11, 0.10), (0.48, 0.21, 0.08), max(0.0, band - 0.55) * 2.2)
    return (_obj(bm, "Forage_IronNodule", colour),)


def build_salt_crust():
    rng = random.Random(9)
    bm = bmesh.new()
    for k in range(5):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.0, 0.4)
        r = rng.uniform(0.25, 0.4)
        gl._blob(bm, (math.cos(a) * d, math.sin(a) * d, 0.05), (r, r * 0.9, 0.1 + 0.05 * rng.random()), 9, 4)
    for k in range(9):                                    # little crystal tips
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.0, 0.5)
        base = Vector((math.cos(a) * d, math.sin(a) * d, 0.08))
        gl._tube(bm, [base, base + Vector((0, 0, 0.18))], 0.04, 0.008, 4)
    return (_obj(bm, "Forage_SaltCrust", lambda co, nr: _mix((0.78, 0.76, 0.70), (0.96, 0.95, 0.92), min(1.0, co.z * 4.0))),)


def build_glow_quartz():
    rng = random.Random(10)
    rb, cb = bmesh.new(), bmesh.new()
    gl._blob(rb, (0, 0, 0.06), (0.45, 0.4, 0.18), 9, 4)
    for k in range(5):
        a = rng.uniform(0, TAU)
        d = rng.uniform(0.0, 0.25)
        base = Vector((math.cos(a) * d, math.sin(a) * d, 0.08))
        tilt = rng.uniform(0.1, 0.5)
        dirv = Vector((math.cos(a) * tilt, math.sin(a) * tilt, 1.0)).normalized()
        h = rng.uniform(0.4, 0.9)
        path = [base, base + dirv * h * 0.85, base + dirv * h]
        rings = kit.tube_rings(cb, path, [0.075, 0.07, 0.01], 6)
        kit.bridge_rings(cb, rings)
        kit.cap_ring(cb, rings[0], flip=True)
        kit.cap_ring(cb, rings[-1])
    rock = kit.finish_object(rb, "Forage_Quartz", kit.vertex_color_material("Prop_VC"))
    kit.paint(rock, lambda co, nr: (0.12, 0.11, 0.10))
    glow = kit.finish_object(cb, "Quartz Glow", kit.vertex_color_material("Prop_VC"))
    kit.paint(glow, lambda co, nr: (1.0, 1.0, 1.0))
    return (rock, glow)


PIECES = (("WaterLily", build_water_lily), ("Cattail", build_cattail), ("BitterFern", build_bitter_fern), ("FatMushroom", build_fat_mushroom),
          ("SweetRoot", build_sweet_root), ("IceBloom", build_ice_bloom), ("LimeCrystal", build_lime_crystal), ("IronNodule", build_iron_nodule),
          ("SaltCrust", build_salt_crust), ("Quartz", build_glow_quartz))


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
            kit.render_preview(objs, os.path.join(args.previews, "Forage_%s_front.png" % name), "front", 1.6)
        print(name, sum(kit.tri_count(o) for o in objs))


if __name__ == "__main__":
    main()
