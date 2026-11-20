"""Dựng 8 Mô Hình 3D Đợt 2 (Tầng Nông: Hang Rêu Thở & Tổ Bỏ Hoang) kèm Armature + NLA Animations + Preview Cycles.

Chạy headless:
  "D:/blender/blender.exe" -b -P tools/blender/build_cave_creatures_wave2_nong.py
"""
import argparse
import math
import os
import random
import sys
import time

import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_common as kc  # noqa: E402
import build_cave_creatures_wave1 as w1  # noqa: E402


# ================================================================== HANG 3: HANG RÊU THỞ (4 LOÀI)

def build_13_sen_reu(out_dir, prev_dir):
    """[M] Sên Rêu: sên mập mềm mại phủ rêu ẩm & nụ bào tử phát sáng trên lưng."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()
    rng = random.Random(313)

    def slug_skin(p, n):
        wave = 0.5 + 0.5 * math.sin(p.y * 16.0 + p.x * 12.0)
        base = w1._lerp3((0.16, 0.34, 0.16), (0.42, 0.62, 0.24), 0.35 + 0.55 * wave)
        if p.z < 0.05:
            base = w1._lerp3(base, (0.64, 0.72, 0.48), 0.55)
        return base

    # Thân sên uốn cong chữ S mềm mại (subdiv=2) + diềm váy chân trượt
    pts = [(0.04, -0.40, 0.09), (0.02, -0.22, 0.13), (0.0, 0.0, 0.14), (-0.03, 0.24, 0.11), (-0.05, 0.42, 0.05)]
    rads = [(0.08, 0.07), (0.13, 0.11), (0.15, 0.13), (0.12, 0.095), (0.05, 0.035)]
    w1._add_tube(bm, col, pts, rads, sides=18, subdiv=2, mat_index=0, color_fn=slug_skin)
    w1._add_tube(bm, col, [(0.04, -0.42, 0.025), (0.02, -0.20, 0.028), (0.0, 0.02, 0.028), (-0.04, 0.40, 0.020)], [(0.11, 0.024), (0.16, 0.026), (0.17, 0.026), (0.07, 0.018)], sides=14, subdiv=2, mat_index=0, color_fn=lambda _p, _n: (0.58, 0.68, 0.44))

    # 2 cặp râu mắt & xúc tu mềm
    for sx in (-1.0, 1.0):
        tip = (0.04 + sx * 0.08, -0.50, 0.25)
        w1._add_tube(bm, col, [(0.04 + sx * 0.04, -0.38, 0.14), (0.04 + sx * 0.07, -0.45, 0.20), tip], [0.018, 0.013, 0.011], sides=8, subdiv=1, mat_index=0, color_fn=slug_skin)
        w1._add_eye_bulb(bm, col, tip, 0.018, yaw=sx * 0.2, eye_col=(0.14, 0.10, 0.06), mat_eye=1)
        w1._add_tube(bm, col, [(0.04 + sx * 0.035, -0.41, 0.08), (0.04 + sx * 0.07, -0.48, 0.05)], [0.012, 0.005], sides=6, subdiv=0, mat_index=0, color_fn=slug_skin)

    # Rêu 3D & nụ bào tử thở sáng trên lưng
    w1._add_moss_tuft(bm, col, (0.01, -0.04, 0.26), scale=0.068, rng=rng)
    w1._add_moss_tuft(bm, col, (-0.02, 0.16, 0.22), scale=0.055, rng=rng)
    w1._add_luma_bud(bm, col, (0.02, -0.02, 0.29), Vector((0.1, 0.0, 1.0)), size=0.032, hue=0.38)
    w1._add_luma_bud(bm, col, (-0.04, 0.14, 0.24), Vector((-0.2, 0.2, 1.0)), size=0.026, hue=0.42)

    bones = [
        ("Root", (0, 0.0, 0.08), (0, -0.18, 0.08), None, 0.32, 1.0),
        ("Head", (0.02, -0.22, 0.12), (0.04, -0.42, 0.11), "Root", 0.20, 2.2),
        ("Eye_L", (-0.02, -0.38, 0.14), (-0.04, -0.50, 0.25), "Head", 0.09, 2.8),
        ("Eye_R", (0.08, -0.38, 0.14), (0.12, -0.50, 0.25), "Head", 0.09, 2.8),
        ("Back", (0, 0.0, 0.18), (0, 0.18, 0.24), "Root", 0.22, 2.0),
        ("Tail", (-0.02, 0.20, 0.09), (-0.05, 0.42, 0.04), "Root", 0.18, 2.2),
    ]
    clips = {
        "Idle": (1, 56, [
            (1, {"Head": {"rot": (0.04, 0, 0.08)}, "Eye_L": {"rot": (0.12, 0, 0.10)}, "Eye_R": {"rot": (-0.10, 0, -0.10)}, "Back": {"scale": (1, 1, 1)}}),
            (28, {"Head": {"rot": (-0.06, 0, -0.10)}, "Eye_L": {"rot": (-0.14, 0, -0.12)}, "Eye_R": {"rot": (0.14, 0, 0.12)}, "Back": {"scale": (1.08, 1.05, 1.12)}}),
            (56, {"Head": {"rot": (0.04, 0, 0.08)}, "Eye_L": {"rot": (0.12, 0, 0.10)}, "Eye_R": {"rot": (-0.10, 0, -0.10)}, "Back": {"scale": (1, 1, 1)}}),
        ]),
        "Walk": (1, 36, [
            (1, {"Head": {"loc": (0, -0.04, 0)}, "Back": {"scale": (1, 0.94, 1.06)}, "Tail": {"loc": (0, 0.03, 0)}}),
            (18, {"Head": {"loc": (0, 0.02, 0.01)}, "Back": {"scale": (1.06, 1.08, 0.95)}, "Tail": {"loc": (0, -0.03, 0)}}),
            (36, {"Head": {"loc": (0, -0.04, 0)}, "Back": {"scale": (1, 0.94, 1.06)}, "Tail": {"loc": (0, 0.03, 0)}}),
        ]),
        "Action": (1, 24, [
            (1, {"Back": {"scale": (1, 1, 1)}}),
            (10, {"Back": {"scale": (1.28, 1.24, 0.72)}, "Root": {"loc": (0, 0, -0.03)}}),
            (24, {"Back": {"scale": (1, 1, 1)}, "Root": {"loc": (0, 0, 0)}}),
        ]),
        "Hit": (1, 16, [
            (1, {"Root": {"scale": (1, 1, 1)}}),
            (5, {"Root": {"scale": (1.2, 0.75, 0.75), "rot": (0, 0, 0.18)}}),
            (16, {"Root": {"scale": (1, 1, 1), "rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_SenReu",
        os.path.join(out_dir, "Creature_SenReu.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_13_sen_reu.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_14_muoi_reu(out_dir, prev_dir):
    """[M] Muỗi Rêu: muỗi rêu bay lơ lửng, vòi chích dài, bụng sọc rêu hổ phách & cánh mỏng."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()

    def body_col(p, n):
        band = 0.5 + 0.5 * math.sin(p.y * 32.0)
        return w1._lerp3((0.12, 0.18, 0.10), (0.38, 0.48, 0.20), band * 0.65 + max(0.0, n.z) * 0.35)

    # Thân bay cao z ~ 0.46..0.60
    w1._add_ellipsoid(bm, col, (0, -0.10, 0.54), (0.075, 0.10, 0.085), segs=14, rings=10, rot=(0.12, 0.10, -0.10), mat_index=1, color_fn=body_col)
    w1._add_ellipsoid(bm, col, (0.01, -0.23, 0.56), (0.055, 0.055, 0.052), segs=12, rings=8, mat_index=1, color_fn=body_col)
    # Vòi hút máu dài + râu lông tơ
    w1._add_tube(bm, col, [(0.01, -0.27, 0.54), (0.02, -0.42, 0.45), (0.03, -0.54, 0.38)], [0.014, 0.008, 0.003], sides=8, subdiv=1, mat_index=1, color_fn=lambda _p, _n: (0.16, 0.06, 0.06))
    for sx in (-1.0, 1.0):
        w1._add_eye_bulb(bm, col, (0.01 + sx * 0.044, -0.26, 0.575), 0.024, yaw=sx * 0.45, eye_col=(0.88, 0.24, 0.16))
        w1._add_tube(bm, col, [(0.01 + sx * 0.02, -0.27, 0.59), (0.01 + sx * 0.08, -0.36, 0.66)], [0.008, 0.003], sides=6, subdiv=1, mat_index=0, color_fn=body_col)

    # Bụng thon sọc hổ phách-rêu cong xuống
    w1._add_tube(bm, col, [(0, -0.02, 0.51), (-0.01, 0.12, 0.44), (-0.02, 0.25, 0.35), (-0.02, 0.34, 0.27)], [0.052, 0.062, 0.045, 0.018], sides=12, subdiv=2, mat_index=0,
                 color_fn=lambda p, _n: w1._lerp3((0.20, 0.32, 0.14), (0.76, 0.34, 0.14), 0.5 + 0.5 * math.sin(p.y * 36.0)))

    # Đôi cánh mỏng ánh xanh lục
    for sx in (-1.0, 1.0):
        w1._add_3d_leaf_blade(bm, col, (sx * 0.05, -0.08, 0.60), Vector((sx * 0.90, 0.24, 0.28)), Vector((0, 0, 1)), 0.42, 0.095, thickness=0.010, curl=0.05, vein_mat=2,
                              color_fn=lambda p, _n: w1._lerp3((0.46, 0.86, 0.58), (0.82, 0.94, 0.62), abs(p.x) * 2.2))
        for py, mid_y, f_y in [(-0.14, -0.22, -0.18), (-0.08, -0.04, 0.04), (-0.02, 0.12, 0.22)]:
            w1._add_tube(bm, col, [(sx * 0.04, py, 0.48), (sx * 0.18, mid_y, 0.32), (sx * 0.15, f_y, 0.01)], [0.012, 0.008, 0.004], sides=6, subdiv=1, mat_index=1, color_fn=body_col)

    bones = [
        ("Root", (0, -0.06, 0.52), (0, -0.20, 0.54), None, 0.22, 1.0),
        ("Head", (0, -0.20, 0.55), (0.03, -0.50, 0.40), "Root", 0.20, 2.4),
        ("Abdomen", (0, -0.02, 0.51), (-0.02, 0.34, 0.27), "Root", 0.16, 2.2),
        ("Wing_L", (-0.05, -0.08, 0.60), (-0.42, 0.02, 0.70), "Root", 0.16, 2.8),
        ("Wing_R", (0.05, -0.08, 0.60), (0.42, 0.02, 0.70), "Root", 0.16, 2.8),
        ("Legs", (0, -0.06, 0.42), (0, 0.04, 0.04), "Root", 0.22, 2.2),
    ]
    clips = {
        "Idle": (1, 24, [
            (1, {"Root": {"loc": (0, 0, 0)}, "Wing_L": {"rot": (0, 0.35, 0)}, "Wing_R": {"rot": (0, -0.35, 0)}, "Abdomen": {"rot": (0.08, 0, 0)}}),
            (12, {"Root": {"loc": (0, 0, 0.05)}, "Wing_L": {"rot": (0, -0.58, 0)}, "Wing_R": {"rot": (0, 0.58, 0)}, "Abdomen": {"rot": (-0.08, 0, 0)}}),
            (24, {"Root": {"loc": (0, 0, 0)}, "Wing_L": {"rot": (0, 0.35, 0)}, "Wing_R": {"rot": (0, -0.35, 0)}, "Abdomen": {"rot": (0.08, 0, 0)}}),
        ]),
        "Walk": (1, 16, [
            (1, {"Root": {"rot": (0.18, 0.12, 0), "loc": (0, 0, 0.03)}, "Wing_L": {"rot": (0, -0.65, 0)}, "Wing_R": {"rot": (0, 0.65, 0)}, "Legs": {"rot": (0.28, 0, 0)}}),
            (8, {"Root": {"rot": (0.22, -0.12, 0), "loc": (0, 0, 0.08)}, "Wing_L": {"rot": (0, 0.45, 0)}, "Wing_R": {"rot": (0, -0.45, 0)}, "Legs": {"rot": (0.18, 0, 0)}}),
            (16, {"Root": {"rot": (0.18, 0.12, 0), "loc": (0, 0, 0.03)}, "Wing_L": {"rot": (0, -0.65, 0)}, "Wing_R": {"rot": (0, 0.65, 0)}, "Legs": {"rot": (0.28, 0, 0)}}),
        ]),
        "Action": (1, 18, [
            (1, {"Root": {"loc": (0, 0, 0)}, "Head": {"rot": (0, 0, 0)}}),
            (7, {"Root": {"loc": (0, -0.18, -0.06)}, "Head": {"rot": (-0.28, 0, 0)}}),
            (18, {"Root": {"loc": (0, 0, 0)}, "Head": {"rot": (0, 0, 0)}}),
        ]),
        "Hit": (1, 16, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (5, {"Root": {"rot": (-0.35, 0.45, 0.35)}}),
            (16, {"Root": {"rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_MuoiReu",
        os.path.join(out_dir, "Creature_MuoiReu.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_14_muoi_reu.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_15_ret_reu(out_dir, prev_dir):
    """[S] Rết Rêu: rết 7 đốt uốn lượn chữ S trong rêu, viền độc xanh chanh, 12 chân bò & nanh độc."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()

    def centipede_col(p, n):
        base = w1._lerp3((0.06, 0.14, 0.08), (0.20, 0.38, 0.14), max(0.0, n.z) * 0.7 + 0.15)
        if abs(p.x) > 0.10 and n.z > 0.1:
            base = w1._lerp3(base, (0.56, 0.86, 0.20), 0.55)
        return base

    seg_pts = [(0.06, -0.74, 0.14), (0.04, -0.52, 0.15), (-0.02, -0.28, 0.15), (-0.06, -0.02, 0.15), (-0.02, 0.24, 0.15), (0.06, 0.50, 0.14), (0.14, 0.74, 0.12)]
    for i, sp in enumerate(seg_pts):
        sc = 1.0 - (i / len(seg_pts)) * 0.22
        w1._add_ellipsoid(bm, col, sp, (0.14 * sc, 0.15 * sc, 0.085 * sc), segs=14, rings=8, rot=(0.08, 0, -0.15 if i < 2 else 0.12), mat_index=1, color_fn=centipede_col)
        if 0 < i < len(seg_pts) - 1:
            for sx in (-1.0, 1.0):
                w1._add_tube(bm, col, [(sp[0] + sx * 0.10, sp[1], sp[2] - 0.02), (sp[0] + sx * 0.22, sp[1] - 0.02, sp[2] + 0.04), (sp[0] + sx * 0.25, sp[1] + 0.03, 0.008)], [0.026, 0.018, 0.007], sides=6, subdiv=1, mat_index=1, color_fn=lambda _p, _n: (0.48, 0.72, 0.16))

    # Đầu rết, râu dài, mắt đỏ & cặp nanh độc
    hp = Vector(seg_pts[0])
    for sx in (-1.0, 1.0):
        w1._add_eye_bulb(bm, col, hp + Vector((sx * 0.08, -0.08, 0.04)), 0.024, yaw=sx * 0.4, eye_col=(0.94, 0.26, 0.12))
        w1._add_tube(bm, col, [hp + Vector((sx * 0.05, -0.11, 0.01)), hp + Vector((sx * 0.15, -0.26, 0.08)), hp + Vector((sx * 0.22, -0.36, 0.14))], [0.014, 0.009, 0.004], sides=6, subdiv=1, mat_index=1, color_fn=centipede_col)
        w1._add_tube(bm, col, [hp + Vector((sx * 0.04, -0.11, -0.02)), hp + Vector((sx * 0.09, -0.22, -0.01)), hp + Vector((sx * 0.02, -0.27, 0.01))], [0.024, 0.015, 0.005], sides=6, subdiv=0, mat_index=2, color_fn=lambda _p, _n: (0.58, 0.94, 0.22))
    # Đuôi chẻ đôi
    tp = Vector(seg_pts[-1])
    for sx in (-1.0, 1.0):
        w1._add_tube(bm, col, [tp, tp + Vector((sx * 0.09, 0.16, 0.02)), tp + Vector((sx * 0.14, 0.28, 0.05))], [0.028, 0.016, 0.006], sides=6, subdiv=1, mat_index=1, color_fn=centipede_col)

    bones = [
        ("Root", (-0.04, 0.0, 0.14), (-0.02, -0.28, 0.14), None, 0.32, 1.0),
        ("Head", (0.04, -0.45, 0.15), (0.06, -0.78, 0.14), "Root", 0.26, 2.2),
        ("Mid", (-0.02, 0.24, 0.15), (0.06, 0.50, 0.14), "Root", 0.26, 2.0),
        ("Tail", (0.08, 0.54, 0.14), (0.16, 0.92, 0.12), "Mid", 0.24, 2.2),
        ("Legs_L", (-0.18, 0.0, 0.12), (-0.24, -0.25, 0.01), "Root", 0.45, 2.0),
        ("Legs_R", (0.18, 0.0, 0.12), (0.24, -0.25, 0.01), "Root", 0.45, 2.0),
    ]
    clips = {
        "Idle": (1, 48, [
            (1, {"Head": {"rot": (0, 0, 0.14)}, "Tail": {"rot": (0, 0, -0.18)}}),
            (24, {"Head": {"rot": (0, 0, -0.18)}, "Tail": {"rot": (0, 0, 0.18)}}),
            (48, {"Head": {"rot": (0, 0, 0.14)}, "Tail": {"rot": (0, 0, -0.18)}}),
        ]),
        "Walk": (1, 20, [
            (1, {"Head": {"rot": (0, 0, 0.35)}, "Mid": {"rot": (0, 0, -0.28)}, "Tail": {"rot": (0, 0, 0.35)}, "Legs_L": {"rot": (0.35, 0, 0)}, "Legs_R": {"rot": (-0.30, 0, 0)}}),
            (10, {"Head": {"rot": (0, 0, -0.35)}, "Mid": {"rot": (0, 0, 0.28)}, "Tail": {"rot": (0, 0, -0.35)}, "Legs_L": {"rot": (-0.30, 0, 0)}, "Legs_R": {"rot": (0.35, 0, 0)}}),
            (20, {"Head": {"rot": (0, 0, 0.35)}, "Mid": {"rot": (0, 0, -0.28)}, "Tail": {"rot": (0, 0, 0.35)}, "Legs_L": {"rot": (0.35, 0, 0)}, "Legs_R": {"rot": (-0.30, 0, 0)}}),
        ]),
        "Attack": (1, 20, [
            (1, {"Head": {"rot": (0, 0, 0)}}),
            (6, {"Head": {"rot": (-0.32, 0, 0)}, "Root": {"loc": (0, 0.12, 0.06)}}),
            (11, {"Head": {"rot": (0.28, 0, 0)}, "Root": {"loc": (0, -0.28, 0.02)}}),
            (20, {"Head": {"rot": (0, 0, 0)}, "Root": {"loc": (0, 0, 0)}}),
        ]),
        "Hit": (1, 16, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (5, {"Root": {"rot": (0, 0, 0.38)}, "Head": {"rot": (-0.28, 0, 0)}}),
            (16, {"Root": {"rot": (0, 0, 0)}, "Head": {"rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_RetReu",
        os.path.join(out_dir, "Creature_RetReu.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_15_ret_reu.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_16_reu_me(out_dir, prev_dir):
    """[C] Rêu Mẹ (Chúa Hang): gò rêu cổ đại biết bò, 6 chân rễ xoắn, dương xỉ & nấm thở phát sáng."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()
    rng = random.Random(316)

    def moss_mound(p, n):
        cr = 0.5 + 0.5 * math.sin(p.x * 9.0 + p.y * 8.0) * math.cos(p.z * 11.0)
        return w1._lerp3((0.10, 0.24, 0.10), (0.28, 0.54, 0.18), 0.3 + 0.7 * cr)

    # Khối thân gò rêu khổng lồ (rộng ~1.8m)
    w1._add_ellipsoid(bm, col, (0, 0.05, 0.52), (0.82, 0.88, 0.44), segs=22, rings=14, mat_index=0, color_fn=moss_mound,
                      warp_fn=lambda p: (p.x * (1.0 + 0.08 * math.sin(p.y * 6.0)), p.y, p.z * (0.95 - 0.18 * max(0.0, -p.z))))
    w1._add_ellipsoid(bm, col, (0, -0.58, 0.36), (0.36, 0.28, 0.24), segs=16, rings=10, mat_index=0, color_fn=moss_mound)
    for sx in (-1.0, 1.0):
        w1._add_eye_bulb(bm, col, (sx * 0.20, -0.78, 0.42), 0.065, yaw=sx * 0.45, eye_col=(0.24, 0.94, 0.68))

    # Thảm lá dương xỉ + cụm nấm bào tử phát sáng trên lưng
    for k in range(8):
        ang = 2.0 * math.pi * k / 8.0
        bx, by = math.cos(ang) * 0.42, 0.05 + math.sin(ang) * 0.45
        w1._add_3d_leaf_blade(bm, col, (bx, by, 0.80), Vector((math.cos(ang), math.sin(ang), 0.55)), Vector((0, 0, 1)), 0.42, 0.12, thickness=0.018, curl=0.16, vein_mat=0, color_fn=moss_mound)
    for mx, my, mz, r, h in [(0, 0.05, 0.94, 0.22, 0.28), (-0.28, -0.18, 0.86, 0.14, 0.20), (0.30, 0.22, 0.84, 0.15, 0.22)]:
        w1._add_tube(bm, col, [(mx, my, mz - 0.08), (mx, my, mz + h * 0.7)], [r * 0.28, r * 0.18], sides=10, subdiv=1, mat_index=0, color_fn=lambda _p, _n: (0.42, 0.40, 0.30))
        w1._add_ellipsoid(bm, col, (mx, my, mz + h), (r, r, r * 0.42), segs=14, rings=8, mat_index=2, color_fn=lambda _p, _n: (0.28, 0.92, 0.68))

    # 6 chân rễ cây bám chặt mặt đất
    for k in range(6):
        ang = -2.2 + k * (4.4 / 5.0)
        sx = -1.0 if k % 2 == 0 else 1.0
        base = Vector((math.sin(ang) * 0.62, math.cos(ang) * 0.65, 0.34))
        mid = Vector((math.sin(ang) * 0.96, math.cos(ang) * 0.92, 0.22))
        foot = Vector((math.sin(ang) * 1.05, math.cos(ang) * 1.02, 0.02))
        w1._add_tube(bm, col, [base, mid, foot], [0.14, 0.10, 0.06], sides=10, subdiv=2, mat_index=0, color_fn=lambda _p, _n: (0.24, 0.18, 0.10))

    bones = [
        ("Root", (0, 0.05, 0.50), (0, -0.25, 0.50), None, 0.95, 1.0),
        ("Crown", (0, 0.05, 0.82), (0, 0.05, 1.25), "Root", 0.65, 2.0),
        ("Head", (0, -0.50, 0.36), (0, -0.82, 0.38), "Root", 0.34, 2.2),
        ("Roots_L", (-0.65, 0.0, 0.24), (-0.95, 0.0, 0.02), "Root", 0.52, 2.2),
        ("Roots_R", (0.65, 0.0, 0.24), (0.95, 0.0, 0.02), "Root", 0.52, 2.2),
    ]
    clips = {
        "Idle": (1, 60, [
            (1, {"Root": {"loc": (0, 0, 0)}, "Crown": {"scale": (1, 1, 1)}, "Head": {"rot": (0.04, 0, 0)}}),
            (30, {"Root": {"loc": (0, 0, 0.04)}, "Crown": {"scale": (1.10, 1.10, 1.12)}, "Head": {"rot": (-0.06, 0, 0.08)}}),
            (60, {"Root": {"loc": (0, 0, 0)}, "Crown": {"scale": (1, 1, 1)}, "Head": {"rot": (0.04, 0, 0)}}),
        ]),
        "Walk": (1, 36, [
            (1, {"Root": {"rot": (0, 0.06, 0)}, "Roots_L": {"rot": (0.35, 0, 0)}, "Roots_R": {"rot": (-0.30, 0, 0)}}),
            (18, {"Root": {"rot": (0, -0.06, 0)}, "Roots_L": {"rot": (-0.30, 0, 0)}, "Roots_R": {"rot": (0.35, 0, 0)}}),
            (36, {"Root": {"rot": (0, 0.06, 0)}, "Roots_L": {"rot": (0.35, 0, 0)}, "Roots_R": {"rot": (-0.30, 0, 0)}}),
        ]),
        "Action": (1, 36, [
            (1, {"Root": {"loc": (0, 0, 0)}, "Crown": {"scale": (1, 1, 1)}}),
            (15, {"Root": {"loc": (0, 0, -0.10)}, "Crown": {"scale": (1.28, 1.28, 1.28)}}),
            (36, {"Root": {"loc": (0, 0, 0)}, "Crown": {"scale": (1, 1, 1)}}),
        ]),
        "Hit": (1, 18, [
            (1, {"Root": {"loc": (0, 0, 0)}}),
            (6, {"Root": {"loc": (0, 0, -0.10), "rot": (0.14, 0, 0.14)}}),
            (18, {"Root": {"loc": (0, 0, 0), "rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_ReuMe",
        os.path.join(out_dir, "Creature_ReuMe.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_16_reu_me.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


# ================================================================== HANG 4: TỔ BỎ HOANG (3 LOÀI + 1 TRỨNG LẠ)

def build_17_chim_non(out_dir, prev_dir):
    """[M] Chim Non: chim non lông tơ kem-nâu ấm, cánh nhỏ xòe đập, mỏ há kêu chíp."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()

    def down_fur(p, n):
        base = w1._lerp3((0.46, 0.34, 0.22), (0.86, 0.78, 0.56), 0.4 + 0.5 * max(0.0, n.z))
        if p.y < -0.05 and p.z < 0.28:
            base = w1._lerp3(base, (0.94, 0.90, 0.76), 0.65)
        return base

    w1._add_ellipsoid(bm, col, (0, 0.04, 0.24), (0.16, 0.20, 0.16), segs=18, rings=12, rot=(0.25, 0, 0), mat_index=0, color_fn=down_fur)
    w1._add_ellipsoid(bm, col, (0.01, -0.12, 0.36), (0.13, 0.14, 0.12), segs=16, rings=10, rot=(0.1, 0, -0.15), mat_index=0, color_fn=down_fur)
    # Mỏ vàng mở nhẹ + mắt tròn đen bóng
    w1._add_crystal_prism(bm, col, (0.02, -0.23, 0.36), Vector((0.05, -0.96, 0.20)), 0.10, 0.045, 0.020, sides=4, mat_index=0, color_fn=lambda _p, _n: (0.94, 0.68, 0.18), flat_faces=flat)
    w1._add_crystal_prism(bm, col, (0.02, -0.23, 0.33), Vector((0.05, -0.96, -0.18)), 0.09, 0.040, 0.018, sides=4, mat_index=0, color_fn=lambda _p, _n: (0.88, 0.56, 0.14), flat_faces=flat)
    for sx in (-1.0, 1.0):
        w1._add_eye_bulb(bm, col, (0.01 + sx * 0.095, -0.19, 0.39), 0.032, yaw=sx * 0.4, eye_col=(0.08, 0.06, 0.05), mat_eye=1)
        w1._add_3d_leaf_blade(bm, col, (sx * 0.14, 0.0, 0.26), Vector((sx * 0.85, 0.35, 0.25)), Vector((0, 0, 1)), 0.22, 0.085, thickness=0.022, curl=0.08, vein_mat=0, color_fn=down_fur)
        w1._add_tube(bm, col, [(sx * 0.06, 0.02, 0.11), (sx * 0.07, 0.0, 0.02), (sx * 0.08, -0.08, 0.01)], [0.020, 0.014, 0.012], sides=8, subdiv=1, mat_index=0, color_fn=lambda _p, _n: (0.86, 0.62, 0.22))

    # Chùm lông đuôi nhỏ
    w1._add_3d_leaf_blade(bm, col, (0, 0.21, 0.22), Vector((0, 0.88, 0.45)), Vector((0, 0, 1)), 0.14, 0.055, thickness=0.018, curl=0.05, vein_mat=0, color_fn=down_fur)

    bones = [
        ("Root", (0, 0.04, 0.22), (0, -0.06, 0.24), None, 0.26, 1.0),
        ("Head", (0, -0.08, 0.32), (0.02, -0.26, 0.38), "Root", 0.20, 2.2),
        ("Wing_L", (-0.14, 0.0, 0.26), (-0.34, 0.08, 0.32), "Root", 0.16, 2.6),
        ("Wing_R", (0.14, 0.0, 0.26), (0.34, 0.08, 0.32), "Root", 0.16, 2.6),
        ("Tail", (0, 0.21, 0.22), (0, 0.34, 0.28), "Root", 0.12, 2.4),
        ("Feet", (0, 0.0, 0.10), (0, -0.04, 0.01), "Root", 0.16, 2.4),
    ]
    clips = {
        "Idle": (1, 40, [
            (1, {"Head": {"rot": (0.05, 0, 0.10)}, "Wing_L": {"rot": (0, 0.18, 0)}, "Wing_R": {"rot": (0, -0.18, 0)}}),
            (15, {"Head": {"rot": (-0.15, 0, -0.18)}, "Wing_L": {"rot": (0, -0.35, 0)}, "Wing_R": {"rot": (0, 0.35, 0)}}),
            (40, {"Head": {"rot": (0.05, 0, 0.10)}, "Wing_L": {"rot": (0, 0.18, 0)}, "Wing_R": {"rot": (0, -0.18, 0)}}),
        ]),
        "Walk": (1, 16, [
            (1, {"Root": {"loc": (0, 0, 0)}, "Wing_L": {"rot": (0, 0.40, 0)}, "Wing_R": {"rot": (0, -0.40, 0)}}),
            (8, {"Root": {"loc": (0, -0.04, 0.12), "rot": (-0.14, 0, 0)}, "Wing_L": {"rot": (0, -0.55, 0)}, "Wing_R": {"rot": (0, 0.55, 0)}}),
            (16, {"Root": {"loc": (0, 0, 0), "rot": (0, 0, 0)}, "Wing_L": {"rot": (0, 0.40, 0)}, "Wing_R": {"rot": (0, -0.40, 0)}}),
        ]),
        "Hit": (1, 14, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (5, {"Root": {"rot": (-0.28, 0.30, 0.28)}}),
            (14, {"Root": {"rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_ChimNon",
        os.path.join(out_dir, "Creature_ChimNon.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_17_chim_non.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_18_trung_la(out_dir, prev_dir):
    """[M / Kỳ Ngộ] Trứng Lạ: quả trứng ngọc nứt mạch vàng-ngọc lam phát sáng đặt trong ổ rơm rêu."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()
    rng = random.Random(318)

    def egg_shell(p, n):
        crack = abs(math.sin(p.x * 14.0 + p.z * 18.0) * math.cos(p.y * 14.0 - p.z * 12.0))
        base = w1._lerp3((0.76, 0.72, 0.62), (0.34, 0.52, 0.56), 0.5 + 0.5 * math.sin(p.z * 8.0))
        if crack > 0.78:
            base = (0.28, 0.94, 0.86)
        return base

    # Ổ rơm rêu ấm áp bên dưới
    w1._add_ellipsoid(bm, col, (0, 0, 0.08), (0.34, 0.34, 0.08), segs=18, rings=8, mat_index=0, color_fn=lambda _p, _n: (0.34, 0.26, 0.14))
    w1._add_moss_tuft(bm, col, (-0.20, -0.14, 0.12), scale=0.06, rng=rng)
    w1._add_moss_tuft(bm, col, (0.22, 0.12, 0.12), scale=0.06, rng=rng)
    # Quả trứng ngọc thon dài với mạch phát sáng
    w1._add_ellipsoid(bm, col, (0, 0, 0.28), (0.18, 0.18, 0.25), segs=20, rings=14, rot=(0.08, -0.06, 0), mat_index=2, color_fn=egg_shell)

    bones = [
        ("Root", (0, 0, 0.06), (0, 0, 0.14), None, 0.36, 1.0),
        ("Egg", (0, 0, 0.14), (0, 0, 0.50), "Root", 0.30, 2.4),
    ]
    clips = {
        "Idle": (1, 48, [
            (1, {"Egg": {"rot": (0.04, 0.04, 0), "scale": (1, 1, 1)}}),
            (12, {"Egg": {"rot": (-0.08, -0.06, 0), "scale": (1.06, 1.06, 1.04)}}),
            (24, {"Egg": {"rot": (0.06, -0.08, 0), "scale": (1, 1, 1)}}),
            (48, {"Egg": {"rot": (0.04, 0.04, 0), "scale": (1, 1, 1)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_TrungLa",
        os.path.join(out_dir, "Creature_TrungLa.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_18_trung_la.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_19_ran_trom_trung(out_dir, prev_dir):
    """[S] Rắn Trộm Trứng: rắn hang mảnh dẻ cuộn S, vảy đá xám-vàng hổ phách, hàm há & nanh cong."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()

    def snake_scale(p, n):
        diamond = math.sin(p.y * 18.0 + abs(p.x) * 14.0)
        base = w1._lerp3((0.18, 0.17, 0.14), (0.64, 0.48, 0.22), 0.5 + 0.5 * diamond)
        if n.z < -0.15:
            base = w1._lerp3(base, (0.80, 0.74, 0.54), 0.65)
        return base

    pts = [
        (0.06, -0.78, 0.34), (0.04, -0.58, 0.36), (0.0, -0.36, 0.24),
        (-0.14, -0.12, 0.10), (-0.04, 0.16, 0.10), (0.16, 0.42, 0.09),
        (0.04, 0.66, 0.07), (-0.14, 0.86, 0.05), (-0.08, 1.04, 0.03),
    ]
    rads = [0.055, 0.095, 0.072, 0.085, 0.090, 0.075, 0.055, 0.032, 0.012]
    w1._add_tube(bm, col, pts, rads, sides=16, subdiv=2, mat_index=0, color_fn=snake_scale)
    w1._add_ellipsoid(bm, col, (0.05, -0.66, 0.28), (0.070, 0.10, 0.025), segs=12, rings=6, rot=(0.24, 0, -0.12), mat_index=0, color_fn=snake_scale)
    w1._add_tube(bm, col, [(0.06, -0.72, 0.31), (0.09, -0.88, 0.28), (0.11, -0.98, 0.25)], [0.012, 0.008, 0.004], sides=6, subdiv=1, mat_index=0, color_fn=lambda _p, _n: (0.82, 0.14, 0.22))
    for sx in (-1.0, 1.0):
        w1._add_eye_bulb(bm, col, (0.05 + sx * 0.068, -0.64, 0.40), 0.024, yaw=sx * 0.5, eye_col=(0.98, 0.58, 0.08))

    bones = [
        ("Root", (-0.08, -0.10, 0.12), (0.0, -0.30, 0.18), None, 0.34, 1.0),
        ("Neck", (0.0, -0.34, 0.24), (0.04, -0.58, 0.36), "Root", 0.22, 2.0),
        ("Head", (0.04, -0.58, 0.36), (0.08, -0.86, 0.32), "Neck", 0.22, 2.4),
        ("Body", (-0.04, 0.16, 0.10), (0.14, 0.46, 0.09), "Root", 0.26, 2.0),
        ("Tail", (0.06, 0.62, 0.07), (-0.08, 1.04, 0.03), "Body", 0.22, 2.4),
    ]
    clips = {
        "Idle": (1, 56, [
            (1, {"Neck": {"rot": (0.05, 0, 0.12)}, "Head": {"rot": (-0.05, 0, -0.10)}, "Tail": {"rot": (0, 0, 0.20)}}),
            (28, {"Neck": {"rot": (-0.06, 0, -0.14)}, "Head": {"rot": (0.06, 0, 0.14)}, "Tail": {"rot": (0, 0, -0.22)}}),
            (56, {"Neck": {"rot": (0.05, 0, 0.12)}, "Head": {"rot": (-0.05, 0, -0.10)}, "Tail": {"rot": (0, 0, 0.20)}}),
        ]),
        "Walk": (1, 20, [
            (1, {"Neck": {"rot": (0, 0, 0.36)}, "Body": {"rot": (0, 0, -0.36)}, "Tail": {"rot": (0, 0, 0.42)}}),
            (10, {"Neck": {"rot": (0, 0, -0.36)}, "Body": {"rot": (0, 0, 0.36)}, "Tail": {"rot": (0, 0, -0.42)}}),
            (20, {"Neck": {"rot": (0, 0, 0.36)}, "Body": {"rot": (0, 0, -0.36)}, "Tail": {"rot": (0, 0, 0.42)}}),
        ]),
        "Attack": (1, 22, [
            (1, {"Neck": {"rot": (0, 0, 0)}, "Head": {"rot": (0, 0, 0)}}),
            (7, {"Neck": {"rot": (-0.35, 0, 0)}, "Head": {"rot": (-0.28, 0, 0)}}),
            (12, {"Neck": {"rot": (0.38, 0, 0)}, "Head": {"rot": (0.24, 0, 0), "scale": (1.25, 1.15, 1.25)}, "Root": {"loc": (0, -0.26, 0.04)}}),
            (22, {"Neck": {"rot": (0, 0, 0)}, "Head": {"rot": (0, 0, 0), "scale": (1, 1, 1)}, "Root": {"loc": (0, 0, 0)}}),
        ]),
        "Hit": (1, 16, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (5, {"Root": {"rot": (0, 0, 0.40)}, "Head": {"rot": (-0.30, 0, 0)}}),
            (16, {"Root": {"rot": (0, 0, 0)}, "Head": {"rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_RanTromTrung",
        os.path.join(out_dir, "Creature_RanTromTrung.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_19_ran_trom_trung.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_20_chim_hang_me(out_dir, prev_dir):
    """[C] Chim Hang Mẹ (Chúa Hang): đại bàng/cú hang khổng lồ, sải cánh rộng, mỏ khoằm & vuốt sắc."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()

    def feather_col(p, n):
        streak = 0.5 + 0.5 * math.sin(p.x * 16.0 + p.y * 12.0)
        base = w1._lerp3((0.18, 0.14, 0.12), (0.48, 0.36, 0.24), 0.35 + 0.55 * streak)
        if p.y < -0.05 and p.z < 0.75:
            base = w1._lerp3(base, (0.78, 0.70, 0.54), 0.55)
        return base

    w1._add_ellipsoid(bm, col, (0, 0.06, 0.64), (0.34, 0.44, 0.32), segs=20, rings=12, rot=(0.35, 0, 0), mat_index=0, color_fn=feather_col)
    w1._add_ellipsoid(bm, col, (0.02, -0.34, 0.96), (0.24, 0.25, 0.22), segs=18, rings=10, rot=(0.12, 0, -0.12), mat_index=0, color_fn=feather_col)
    # Mỏ khoằm sắc + mào tai lông + mắt vàng sáng
    w1._add_tube(bm, col, [(0.02, -0.54, 0.96), (0.04, -0.72, 0.92), (0.04, -0.76, 0.80)], [0.095, 0.055, 0.015], sides=8, subdiv=1, mat_index=1, color_fn=lambda _p, _n: (0.86, 0.68, 0.22))
    for sx in (-1.0, 1.0):
        w1._add_eye_bulb(bm, col, (0.02 + sx * 0.15, -0.48, 1.02), 0.052, yaw=sx * 0.45, eye_col=(0.98, 0.72, 0.10))
        w1._add_3d_leaf_blade(bm, col, (0.02 + sx * 0.14, -0.26, 1.12), Vector((sx * 0.55, 0.55, 0.62)), Vector((0, -1, 0)), 0.30, 0.075, thickness=0.022, curl=0.10, vein_mat=0, color_fn=feather_col)
        # Đôi cánh sải rộng ~2.4m (2 đốt)
        sh = Vector((sx * 0.28, -0.06, 0.78))
        el = Vector((sx * 0.78, 0.04, 0.96))
        tip = Vector((sx * 1.24, 0.24, 0.84))
        tail_w = Vector((sx * 0.44, 0.42, 0.56))
        w1._add_tube(bm, col, [sh, el, tip], [0.095, 0.065, 0.028], sides=10, subdiv=1, mat_index=0, color_fn=feather_col)
        w1._add_wing_panel_3d(bm, col, sh, [tail_w, tip, el], thickness=0.028, mat_index=0, color_fn=feather_col)
        # Chân vuốt bám đất
        w1._add_amphibian_limb(bm, col, [(sx * 0.16, 0.02, 0.36), (sx * 0.18, -0.02, 0.16), (sx * 0.20, -0.10, 0.03)], [0.085, 0.062, 0.052], toe_count=3, toe_len=0.14, webbed=False, mat_index=1, color_fn=lambda _p, _n: (0.76, 0.58, 0.20))

    # Đuôi quạt xòe
    w1._add_3d_leaf_blade(bm, col, (0, 0.46, 0.46), Vector((0, 0.94, -0.26)), Vector((0, 0, 1)), 0.52, 0.28, thickness=0.026, curl=-0.06, vein_mat=0, color_fn=feather_col)

    bones = [
        ("Root", (0, 0.06, 0.62), (0, -0.18, 0.72), None, 0.55, 1.0),
        ("Head", (0, -0.26, 0.88), (0.04, -0.68, 0.96), "Root", 0.36, 2.2),
        ("Wing_L1", (-0.28, -0.06, 0.78), (-0.78, 0.04, 0.96), "Root", 0.38, 2.5),
        ("Wing_L2", (-0.78, 0.04, 0.96), (-1.24, 0.24, 0.84), "Wing_L1", 0.40, 2.8),
        ("Wing_R1", (0.28, -0.06, 0.78), (0.78, 0.04, 0.96), "Root", 0.38, 2.5),
        ("Wing_R2", (0.78, 0.04, 0.96), (1.24, 0.24, 0.84), "Wing_R1", 0.40, 2.8),
        ("Tail", (0, 0.46, 0.46), (0, 0.94, 0.32), "Root", 0.34, 2.4),
        ("Legs", (0, 0.0, 0.32), (0, -0.08, 0.02), "Root", 0.32, 2.4),
    ]
    clips = {
        "Idle": (1, 56, [
            (1, {"Head": {"rot": (0.04, 0, 0.10)}, "Wing_L1": {"rot": (0, 0.14, 0)}, "Wing_R1": {"rot": (0, -0.14, 0)}, "Tail": {"rot": (0.06, 0, 0)}}),
            (28, {"Head": {"rot": (-0.08, 0, -0.16)}, "Wing_L1": {"rot": (0, -0.16, 0)}, "Wing_R1": {"rot": (0, 0.16, 0)}, "Tail": {"rot": (-0.06, 0, 0)}}),
            (56, {"Head": {"rot": (0.04, 0, 0.10)}, "Wing_L1": {"rot": (0, 0.14, 0)}, "Wing_R1": {"rot": (0, -0.14, 0)}, "Tail": {"rot": (0.06, 0, 0)}}),
        ]),
        "Walk": (1, 24, [
            (1, {"Root": {"loc": (0, 0, 0.06)}, "Wing_L1": {"rot": (0, -0.58, 0)}, "Wing_R1": {"rot": (0, 0.58, 0)}, "Wing_L2": {"rot": (0, -0.36, 0)}, "Wing_R2": {"rot": (0, 0.36, 0)}}),
            (12, {"Root": {"loc": (0, 0, 0.24)}, "Wing_L1": {"rot": (0, 0.52, 0)}, "Wing_R1": {"rot": (0, -0.52, 0)}, "Wing_L2": {"rot": (0, 0.32, 0)}, "Wing_R2": {"rot": (0, -0.32, 0)}}),
            (24, {"Root": {"loc": (0, 0, 0.06)}, "Wing_L1": {"rot": (0, -0.58, 0)}, "Wing_R1": {"rot": (0, 0.58, 0)}, "Wing_L2": {"rot": (0, -0.36, 0)}, "Wing_R2": {"rot": (0, 0.36, 0)}}),
        ]),
        "Attack": (1, 26, [
            (1, {"Root": {"loc": (0, 0, 0), "rot": (0, 0, 0)}}),
            (9, {"Root": {"loc": (0, 0.16, 0.36), "rot": (-0.28, 0, 0)}, "Wing_L1": {"rot": (0, -0.50, 0)}, "Wing_R1": {"rot": (0, 0.50, 0)}}),
            (16, {"Root": {"loc": (0, -0.36, 0.04), "rot": (0.30, 0, 0)}, "Wing_L1": {"rot": (0, 0.45, 0)}, "Wing_R1": {"rot": (0, -0.45, 0)}}),
            (26, {"Root": {"loc": (0, 0, 0), "rot": (0, 0, 0)}, "Wing_L1": {"rot": (0, 0, 0)}, "Wing_R1": {"rot": (0, 0, 0)}}),
        ]),
        "Hit": (1, 18, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (6, {"Root": {"rot": (-0.24, 0.28, 0.22)}}),
            (18, {"Root": {"rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_ChimHangMe",
        os.path.join(out_dir, "Creature_ChimHangMe.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_20_chim_hang_me.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


BUILDERS = [
    ("sen_reu", build_13_sen_reu),
    ("muoi_reu", build_14_muoi_reu),
    ("ret_reu", build_15_ret_reu),
    ("reu_me", build_16_reu_me),
    ("chim_non", build_17_chim_non),
    ("trung_la", build_18_trung_la),
    ("ran_trom_trung", build_19_ran_trom_trung),
    ("chim_hang_me", build_20_chim_hang_me),
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=w1.DEFAULT_OUT_DIR)
    ap.add_argument("--previews", default=w1.DEFAULT_PREV_DIR)
    ap.add_argument("--no-previews", action="store_true")
    ap.add_argument("--only", default=None)
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    args = ap.parse_args(argv)

    t0 = time.time()
    out_dir = os.path.abspath(args.out) if args.out else None
    prev_dir = None if args.no_previews else (os.path.abspath(args.previews) if args.previews else None)

    for key, fn in BUILDERS:
        if args.only and args.only != key:
            continue
        fn(out_dir, prev_dir)

    print(f"Hoàn tất dựng 8 mô hình Wave 2 (Tầng Nông) trong {time.time() - t0:.1f}s", flush=True)


if __name__ == "__main__":
    main()
