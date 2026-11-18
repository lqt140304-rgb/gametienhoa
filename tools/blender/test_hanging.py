"""Run: <venv>/bin/python -I tools/blender/test_hanging.py   (exits non-zero on failure)"""
import os
import sys

import bpy  # noqa: F401
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_common as kit  # noqa: E402
import build_hanging as bh  # noqa: E402


def check(cond, msg):
    if not cond:
        print("FAIL:", msg)
        sys.exit(1)


def zrange(o):
    zs = [v.co.z for v in o.data.vertices]
    return min(zs), max(zs)


for i, length in enumerate(bh.ROOT_LENGTHS):
    kit.new_scene()
    (r,) = bh.build_root(i)
    lo, hi = zrange(r)
    check(abs(-lo - length) <= 0.1 * length, "root %d length %.2f vs %.1f" % (i, -lo, length))
    check(-0.2 <= hi <= 0.35, "root %d top at %.2f" % (i, hi))
    check(kit.tri_count(r) <= 2500, "root %d tris %d" % (i, kit.tri_count(r)))
    check(r.name == "VaultRoot_%d" % i, "name")
for i in range(2):
    kit.new_scene()
    (c,) = bh.build_curtain(i)
    lo, hi = zrange(c)
    check(2.5 <= -lo <= 6.5 and hi <= 0.3, "curtain %d z %.2f..%.2f" % (i, lo, hi))
    check(kit.tri_count(c) <= 2500, "curtain tris %d" % kit.tri_count(c))
for i in range(3):
    kit.new_scene()
    pod, glow = bh.build_pod(i)
    check(glow.name == "Pod Glow_%d" % i and pod.name == "VaultPod_%d" % i, "pod names")
    check(kit.tri_count(pod) + kit.tri_count(glow) <= 2500, "pod tris")
    check(zrange(pod)[1] <= 0.1 and 3.0 <= -zrange(pod)[0] <= 4.6, "pod extent")
for i, length in enumerate((3.0, 4.5, 6.0)):
    kit.new_scene()
    (s,) = bh.build_stalactite(i)
    lo, hi = zrange(s)
    check(abs(-lo - length) <= 0.1 * length and hi <= 0.3, "stalactite %d %.2f..%.2f" % (i, lo, hi))
    check(kit.tri_count(s) <= 2500, "stalactite tris")
print("OK: hanging props")
