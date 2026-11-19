"""Run: <venv>/bin/python -I tools/blender/test_cave_shrooms.py   (exits non-zero on failure)"""
import os
import sys

import bpy  # noqa: F401
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_common as kit  # noqa: E402
import build_cave_shrooms as bs  # noqa: E402


def check(cond, msg):
    if not cond:
        print("FAIL:", msg)
        sys.exit(1)


for name, fn in bs.PIECES:
    kit.new_scene()
    objs = fn()
    zs = [v.co.z for o in objs for v in o.data.vertices]
    tris = sum(kit.tri_count(o) for o in objs)
    check(tris <= 6000, "%s tris %d" % (name, tris))
    check(min(zs) >= -0.2, "%s sinks %.2f" % (name, min(zs)))
    check(0.15 <= max(zs) <= 0.6, "%s height %.2f" % (name, max(zs)))
    check(objs[0].name == name, "%s name %s" % (name, objs[0].name))
    glows = [o for o in objs if "Glow" in o.name]
    check(len(glows) == (1 if name in ("CaveShroom_2", "CaveShroom_3") else 0), "%s glow objects %s" % (name, [o.name for o in objs]))
    check(all(len(o.data.vertices) > 0 for o in objs), "%s empty object" % name)
print("OK: cave shrooms")
