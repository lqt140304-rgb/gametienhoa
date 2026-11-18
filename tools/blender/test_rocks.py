"""Run: <venv>/bin/python -I tools/blender/test_rocks.py   (exits non-zero on failure)"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_common as kit  # noqa: E402
import build_rocks as br  # noqa: E402


def check(cond, msg):
    if not cond:
        print("FAIL:", msg)
        sys.exit(1)


sigs = []
for seed, size in enumerate(br.SIZES):
    kit.new_scene()
    obj, col = br.build_rock(seed, size)
    t, c = kit.tri_count(obj), kit.tri_count(col)
    check(400 <= t <= 2000, "rock %d has %d tris" % (seed, t))
    check(12 <= c <= 300, "collider %d has %d tris" % (seed, c))
    check(col.name == obj.name + "_COL", "collider name")
    w, d, h = kit.bounds(obj)
    check(abs(max(w, d, h) - size) <= 0.1 * size, "size of rock %d is %.2f, wanted %.2f" % (seed, max(w, d, h), size))
    check(min(v.co.z for v in obj.data.vertices) > -1e-4, "rock %d base below ground" % seed)
    top = [p.normal.z for p in obj.data.polygons if p.center.z > 0.8 * h]
    if top:
        check(sum(top) / len(top) > 0.5, "rock %d top is too steep" % seed)  # normal.z > 0.5 means slope < 60 deg
    sigs.append(round(sum(v.co.x * 1.3 + v.co.y * 2.1 + v.co.z * 3.7 for v in obj.data.vertices), 3))
check(len(set(sigs)) == len(sigs), "seeds must give different meshes")
print("OK: %d rocks" % len(sigs))
