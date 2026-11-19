"""Run: <venv>/bin/python -I tools/blender/test_cavern_flora.py   (exits non-zero on failure)"""
import os
import sys

import bpy  # noqa: F401
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_common as kit  # noqa: E402
import build_cavern_flora as bf  # noqa: E402


def check(cond, msg):
    if not cond:
        print("FAIL:", msg)
        sys.exit(1)


for kind, count, fn in bf.PIECES:
    for i in range(count):
        kit.new_scene()
        objs = fn(i)
        zs = [v.co.z for o in objs for v in o.data.vertices]
        tris = sum(kit.tri_count(o) for o in objs)
        name = "Flora_%s_%d" % (kind, i)
        check(tris <= 3800, "%s tris %d" % (name, tris))
        check(objs[0].name == name, "%s name %s" % (name, objs[0].name))
        if kind == "Vine":
            check(max(zs) <= 0.3 and -14.0 <= min(zs) <= -4.0, "%s hangs from the roof: %.2f..%.2f" % (name, min(zs), max(zs)))
        else:
            check(min(zs) >= -0.3, "%s sinks %.2f" % (name, min(zs)))
            check(0.02 <= max(zs) <= 2.2, "%s height %.2f" % (name, max(zs)))
        glows = [o for o in objs if "Glow" in o.name]
        check(len(glows) == (1 if kind in ("Mushrooms", "Flower") else 0), "%s glow objects %s" % (name, [o.name for o in objs]))
print("OK: cavern flora")
