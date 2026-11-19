"""Run: <venv>/bin/python -I tools/blender/test_bones.py   (exits non-zero on failure)"""
import os
import sys

import bpy  # noqa: F401
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_common as kit  # noqa: E402
import build_bones as bb  # noqa: E402


def check(cond, msg):
    if not cond:
        print("FAIL:", msg)
        sys.exit(1)


def span(o, axis):
    vals = [getattr(v.co, axis) for v in o.data.vertices]
    return min(vals), max(vals)


for i in range(2):
    kit.new_scene()
    skull, glow = bb.build_skull(i)
    check(skull.name == "DragonSkull_%d" % i and glow.name == "Eye Glow_%d" % i, "names")
    lo, hi = span(skull, "x")
    check(6.0 * (1, 1.25)[i] <= hi - lo <= 9.5 * (1, 1.25)[i], "skull length %.2f" % (hi - lo))
    zlo, zhi = span(skull, "z")
    check(-0.3 <= zhi <= 0.4, "anchor at the top %.2f" % zhi)
    check(zlo < -bb.DROP - 0.5, "skull hangs below %.2f" % zlo)
    check(kit.tri_count(skull) + kit.tri_count(glow) <= 9000, "skull tris %d" % kit.tri_count(skull))
kit.new_scene()
(rc,) = bb.build_ribcage()
lo, hi = span(rc, "x")
check(7.0 <= hi - lo <= 10.0, "ribcage length %.2f" % (hi - lo))
check(-0.3 <= span(rc, "z")[1] <= 0.4, "ribcage anchor")
check(kit.tri_count(rc) <= 9000, "ribcage tris %d" % kit.tri_count(rc))
print("OK: bones")
