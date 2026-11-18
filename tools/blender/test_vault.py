"""Run: <venv>/bin/python -I tools/blender/test_vault.py   (exits non-zero on failure)"""
import math
import os
import sys

import bpy  # noqa: F401  (before mathutils)
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_common as kit  # noqa: E402
import build_vault as bv  # noqa: E402


def check(cond, msg):
    if not cond:
        print("FAIL:", msg)
        sys.exit(1)


def mouth(obj, pos, direction, depth_from=-0.15, depth_to=0.15):
    """Mean distance to the axis and centre offset of the vertices lying just behind the plane through pos."""
    d = direction.normalized()
    pts = [v.co for v in obj.data.vertices if depth_from <= (pos - v.co).dot(d) <= depth_to]
    check(len(pts) >= 8, "no ring near the mouth at %s" % pos)
    c = sum((p for p in pts), Vector()) / len(pts)
    radii = [((p - c) - d * (p - c).dot(d)).length for p in pts]
    return max(radii), (c - pos) - d * (c - pos).dot(d)   # the cable's envelope radius


for kind, count in bv.KINDS:
    for seed in range(count):
        kit.new_scene()
        obj, col = bv.build_segment(kind, seed)
        tris = kit.tri_count(obj)
        check(tris <= (9000 if kind == "fork" else 6500), "%s %d has %d tris" % (kind, seed, tris))
        # entry mouth: radius 1.0, centred on the axis
        pts = [v.co for v in obj.data.vertices if -0.4 <= v.co.x <= 0.4]   # the whole first ring (its plane is almost x = 0)
        check(len(pts) >= 8, "%s %d: no entry ring" % (kind, seed))
        c = sum((p for p in pts), Vector()) / len(pts)
        radii = [math.hypot(p.y - c.y, p.z - c.z) for p in pts]
        mr = max(radii)   # the cable's envelope radius
        check(abs(mr - bv.ENTRY_RADIUS) <= 0.06 * bv.ENTRY_RADIUS + 0.03, "%s %d entry radius %.3f" % (kind, seed, mr))
        check(abs(c.y) <= 0.12 and abs(c.z) <= 0.12, "%s %d entry off axis (%.2f, %.2f)" % (kind, seed, c.y, c.z))
        # exit mouths
        exits = {"straight": 1, "bend": 1, "fork": 2, "tip": 0, "foot": 0}[kind]
        check(len(bv.LAST_EXITS) == exits, "%s has %d exits" % (kind, len(bv.LAST_EXITS)))
        for pos, d, rad in bv.LAST_EXITS:
            mr, off = mouth(obj, pos, d)
            check(abs(mr - rad) <= 0.12 * rad + 0.03, "%s %d exit radius %.3f wanted %.3f" % (kind, seed, mr, rad))
            check(off.length <= 0.12, "%s %d exit off axis by %.2f" % (kind, seed, off.length))
        if kind != "foot":
            check(min(v.co.z for v in obj.data.vertices) > -4.0, "%s %d hangs too low" % (kind, seed))
        if kind == "foot":
            check(col is not None and kit.tri_count(col) <= 80, "foot collider")
            check(min(v.co.z for v in obj.data.vertices) < -5.0, "foot does not reach the ground")
        else:
            check(col is None, "%s has no collider" % kind)
print("OK: %d rib kinds" % len(bv.KINDS))
