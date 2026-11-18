"""Run: <venv>/bin/python -I tools/blender/test_landmarks.py   (exits non-zero on failure)"""
import os
import sys

import bpy  # noqa: F401  (before mathutils)
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_common as kit  # noqa: E402
import build_landmarks as bl  # noqa: E402


def check(cond, msg):
    if not cond:
        print("FAIL:", msg)
        sys.exit(1)


def near(a, b, tol=0.1):
    return abs(a - b) <= tol * b


# ---- log
kit.new_scene()
log, col = bl.build_log(1)
w, d, h = kit.bounds(log)
check(near(w, 9.0, 0.12), "log length %.2f" % w)
# width includes the bend (+0.8 m) and root tendrils, height the branch stubs
check(1.8 <= d <= 4.5 and 1.8 <= h <= 4.0, "log section %.2f x %.2f" % (d, h))
check(kit.tri_count(log) <= 14000, "log tris %d" % kit.tri_count(log))
check(kit.tri_count(col) <= 300, "log collider tris %d" % kit.tri_count(col))
check(col.name == "Log_Hollow_COL", "collider name")
for o, label in ((log, "display"), (col, "collider")):
    # the bore must be open: a ray along the log axis passes through without touching the mesh
    check(not kit.ray_hits(o, Vector((-6, 0.0, 0.95)), Vector((1, 0, 0))), "%s: bore is blocked" % label)
check(min(v.co.z for v in log.data.vertices) > -0.05, "log below ground")

# ---- mushrooms
for idx, height in enumerate((7.0, 5.5, 4.5)):
    kit.new_scene()
    stalk, cap, col = bl.build_mushroom(idx, height, "giant")
    top = max(v.co.z for v in cap.data.vertices)
    check(near(top, height, 0.12), "mushroom %d height %.2f, wanted %.2f" % (idx, top, height))
    check(cap.name.startswith("Giant Caps"), "cap name %s" % cap.name)
    colh = max(v.co.z for v in col.data.vertices)
    check(colh < height * 0.85, "collider of mushroom %d covers the cap (%.2f)" % (idx, colh))
    check(kit.tri_count(stalk) + kit.tri_count(cap) <= 4000, "mushroom %d tris" % idx)
    check(kit.tri_count(col) <= 300, "mushroom %d collider tris" % idx)
    check(min(v.co.z for v in stalk.data.vertices) > -0.05, "mushroom %d below ground" % idx)

# ---- root arch
kit.new_scene()
arch, col = bl.build_root_arch(1)
w, d, h = kit.bounds(arch)
check(near(h, 8.0, 0.12), "arch height %.2f" % h)
left = [v.co.x for v in arch.data.vertices if v.co.z < 2.0 and v.co.x < 0]
right = [v.co.x for v in arch.data.vertices if v.co.z < 2.0 and v.co.x > 0]
gap = min(right) - max(left)
check(gap >= 2.2, "arch clear gap %.2f m" % gap)
check(kit.tri_count(arch) <= 10000, "arch tris %d" % kit.tri_count(arch))
check(kit.tri_count(col) <= 300, "arch collider tris %d" % kit.tri_count(col))
check(min(v.co.z for v in arch.data.vertices) > -0.05, "arch below ground")
# ---- rim cliff
for seed in (20, 21):
    kit.new_scene()
    cliff, col = bl.build_cliff(seed)
    w, d, h = kit.bounds(cliff)
    check(near(w, 28.0, 0.1) and near(h, 16.0, 0.1), "cliff %.1f x %.1f x %.1f" % (w, d, h))
    check(2.5 <= d <= 9.0, "cliff depth %.2f" % d)
    check(kit.tri_count(cliff) <= 8500, "cliff tris %d" % kit.tri_count(cliff))
    check(kit.tri_count(col) <= 20, "cliff collider tris %d" % kit.tri_count(col))
    check(min(v.co.z for v in cliff.data.vertices) > -0.05, "cliff below ground")

# ---- hanging root
kit.new_scene()
(root,) = bl.build_hang_root(40)
w, d, h = kit.bounds(root)
check(near(h, 10.0, 0.15), "hang root length %.2f" % h)
check(max(v.co.z for v in root.data.vertices) <= 0.45, "hang root top above the roof anchor")
check(kit.tri_count(root) <= 2500, "hang root tris %d" % kit.tri_count(root))
print("OK: log, 3 mushrooms, root arch, cliff, hanging root")
