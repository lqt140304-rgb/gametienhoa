"""Run: <venv>/bin/python -I tools/blender/test_groundlife.py   (exits non-zero on failure)"""
import os
import sys

import bpy  # noqa: F401
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_common as kit  # noqa: E402
import build_groundlife as bg  # noqa: E402


def check(cond, msg):
    if not cond:
        print("FAIL:", msg)
        sys.exit(1)


def zrange(objs):
    zs = [v.co.z for o in objs for v in o.data.vertices]
    return min(zs), max(zs)


for prefix, count, fn in bg.PIECES:
    for i in range(count):
        kit.new_scene()
        objs = fn(i)
        lo, hi = zrange(objs)
        tris = sum(kit.tri_count(o) for o in objs)
        check(tris <= 2500, "%s_%d tris %d" % (prefix, i, tris))
        check(lo >= -0.3, "%s_%d sinks too deep %.2f" % (prefix, i, lo))
        check(0.3 <= hi <= 2.3, "%s_%d height %.2f" % (prefix, i, hi))
        glows = [o for o in objs if "Glow" in o.name]
        if prefix == "RootStub":
            check(not glows, "root stubs do not glow")
        else:
            check(len(glows) == 1 and glows[0].name.split("_")[-1] == str(i), "%s_%d needs exactly one Glow object: %s" % (prefix, i, [o.name for o in objs]))
print("OK: ground life")
