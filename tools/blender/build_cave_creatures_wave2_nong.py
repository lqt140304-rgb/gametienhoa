"""Dựng 8 Mô Hình 3D Đợt 2 (Tầng Nông: Hang Rêu Thở & Tổ Bỏ Hoang) kèm Armature + NLA Animations + Preview Cycles.
Phiên bản nâng cấp: điêu khắc khối hữu cơ đầy đặn (không vuông góc/dẹp) + thể hiện rõ đặc trưng giống loài & khả năng.

Chạy headless:
  "D:/blender/blender.exe" -b -P tools/blender/build_cave_creatures_wave2_nong.py
"""
import argparse
import math
import os
import random
import sys
import time

import bmesh  # noqa: E402
import bpy  # noqa: E402
from mathutils import Euler, Matrix, Vector  # noqa: E402

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_common as kc  # noqa: E402
import build_cave_creatures_wave1 as w1  # noqa: E402


# ================================================================== HELPERS HỮU CƠ NÂNG CAO

def _add_sculpted_blob(bm, col, center, radii, rot=(0.0, 0.0, 0.0),
                       segs=24, rings=16, disp_fn=None, mat_index=0, color_fn=None):
    """Tạo khối hữu cơ phân giải cao với hàm biến dạng bề mặt để tránh phẳng/dẹp."""
    c = Vector(center)
    rx, ry, rz = radii
    rmat = Euler(rot, 'XYZ').to_matrix()
    ring_verts = []

    v_top_pos = c + rmat @ Vector((0.0, 0.0, rz))
    if disp_fn:
        v_top_pos += rmat @ disp_fn(Vector((0.0, 0.0, 1.0)))
    v_top = bm.verts.new(v_top_pos)

    for r_i in range(1, rings):
        phi = math.pi * r_i / rings
        sp, cp = math.sin(phi), math.cos(phi)
        row = []
        for s_i in range(segs):
            th = 2.0 * math.pi * s_i / segs
            u = Vector((sp * math.cos(th), sp * math.sin(th), cp))
            local = Vector((u.x * rx, u.y * ry, u.z * rz))
            if disp_fn:
                local += disp_fn(u)
            row.append(bm.verts.new(c + rmat @ local))
        ring_verts.append(row)

    v_bot_pos = c + rmat @ Vector((0.0, 0.0, -rz))
    if disp_fn:
        v_bot_pos += rmat @ disp_fn(Vector((0.0, 0.0, -1.0)))
    v_bot = bm.verts.new(v_bot_pos)

    bm.verts.ensure_lookup_table()
    faces = []
    for s_i in range(segs):
        sn = (s_i + 1) % segs
        faces.append((v_top, ring_verts[0][s_i], ring_verts[0][sn]))
    for r_i in range(len(ring_verts) - 1):
        r0, r1 = ring_verts[r_i], ring_verts[r_i + 1]
        for s_i in range(segs):
            sn = (s_i + 1) % segs
            faces.append((r0[s_i], r1[s_i], r1[sn], r0[sn]))
    last = ring_verts[-1]
    for s_i in range(segs):
        sn = (s_i + 1) % segs
        faces.append((last[s_i], v_bot, last[sn]))

    for f_verts in faces:
        try:
            f = bm.faces.new(f_verts)
            f.material_index = mat_index
            f.smooth = True
            for loop in f.loops:
                nrm = (loop.vert.co - c).normalized()
                rgb = color_fn(loop.vert.co, nrm) if color_fn else (0.5, 0.5, 0.5)
                loop[col] = (*rgb, 1.0)
        except ValueError:
            pass


def _add_layered_feather(bm, col, base, dir_v, up_v, length, width, thickness=0.012,
                         segs=6, mat_index=0, color_fn=None):
    """Lông vũ 3D có độ cong vòm và vuốt nhọn tự nhiên, không phẳng mỏng."""
    d = Vector(dir_v).normalized()
    u = Vector(up_v).normalized()
    side = d.cross(u)
    if side.length < 1e-5:
        side = d.cross(Vector((0.0, 1.0, 0.0)))
    side.normalize()
    u = side.cross(d).normalized()
    b = Vector(base)

    pts = []
    rads = []
    for i in range(segs + 1):
        t = i / segs
        arch = math.sin(t * math.pi) * length * 0.06
        pts.append(b + d * (t * length) + u * arch)
        w = width * math.sin(math.pow(t, 0.55) * math.pi) * (1.0 - 0.18 * t) + 0.004
        th = thickness * (1.0 - 0.65 * t) + 0.003
        rads.append((max(0.004, w), max(0.003, th)))
    w1._add_tube(bm, col, pts, rads, sides=10, subdiv=1, mat_index=mat_index, color_fn=color_fn)


def _add_jointed_limb(bm, col, pts, rads, claw_dir=None, claw_len=0.04,
                      mat_index=0, claw_mat=2, color_fn=None, claw_col=(0.14, 0.12, 0.10)):
    """Chi phân đốt hữu cơ có khớp phồng nhẹ + móng vuốt cong."""
    w1._add_tube(bm, col, pts, rads, sides=10, subdiv=1, mat_index=mat_index, color_fn=color_fn)
    for idx in range(1, len(pts) - 1):
        r_j = rads[idx] * 1.18
        _add_sculpted_blob(bm, col, pts[idx], (r_j, r_j, r_j), segs=12, rings=8,
                           mat_index=mat_index, color_fn=color_fn)
    if claw_dir is not None:
        end = Vector(pts[-1])
        cd = Vector(claw_dir).normalized()
        c_pts = [end, end + cd * (claw_len * 0.55) + Vector((0, 0, claw_len * 0.15)),
                 end + cd * claw_len - Vector((0, 0, claw_len * 0.25))]
        w1._add_tube(bm, col, c_pts, [rads[-1] * 0.85, rads[-1] * 0.5, 0.002],
                     sides=8, subdiv=0, mat_index=claw_mat, color_fn=lambda _p, _n: claw_col)


# ================================================================== HANG 3: HANG RÊU THỞ (4 LOÀI)

def build_13_sen_reu(out_dir, prev_dir):
    """[M] Sên Rêu — Khả năng: Tiết Nhớt.
    Thân sên mập uốn chữ S, áo choàng gợn sóng, túi tuyến nhớt phát sáng xanh ngọc trên lưng,
    cụm rêu sống + vệt nhớt ướt bóng phía sau."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()
    rng = random.Random(313)

    def slug_skin(p, n):
        wave = 0.5 + 0.5 * math.sin(p.y * 15.0 + p.x * 11.0 + p.z * 8.0)
        base = w1._lerp3((0.15, 0.34, 0.16), (0.44, 0.64, 0.25), 0.32 + 0.55 * wave)
        if p.z < 0.06:
            base = w1._lerp3(base, (0.66, 0.75, 0.48), 0.55)
        return base

    def slime_sac_col(p, n):
        pulse = 0.5 + 0.5 * math.sin(p.y * 22.0 + p.z * 18.0)
        return w1._lerp3((0.18, 0.78, 0.58), (0.55, 1.0, 0.42), pulse)

    # Thân sên đầy đặn 3D nhiều vòng + diềm chân gợn sóng bám đất
    pts = [
        (0.05, -0.44, 0.12), (0.04, -0.28, 0.15), (0.02, -0.10, 0.16),
        (0.0, 0.08, 0.15), (-0.03, 0.26, 0.11), (-0.05, 0.44, 0.06),
    ]
    rads = [
        (0.085, 0.075), (0.125, 0.115), (0.155, 0.140),
        (0.145, 0.130), (0.110, 0.092), (0.045, 0.032),
    ]
    w1._add_tube(bm, col, pts, rads, sides=22, subdiv=2, mat_index=0, color_fn=slug_skin)

    # Diềm bàn chân gợn sóng (pedal wave) & áo choàng (mantle) ở lưng trước
    foot_pts = [(0.05, -0.45, 0.028), (0.03, -0.22, 0.030), (0.01, 0.0, 0.030),
                (-0.02, 0.22, 0.026), (-0.05, 0.44, 0.018)]
    foot_rads = [(0.115, 0.028), (0.165, 0.032), (0.185, 0.032), (0.145, 0.026), (0.065, 0.018)]
    w1._add_tube(bm, col, foot_pts, foot_rads, sides=18, subdiv=2, mat_index=0,
                 color_fn=lambda p, _n: w1._lerp3((0.55, 0.68, 0.42), (0.72, 0.84, 0.52), 0.5 + 0.5 * math.sin(p.y * 28.0)))

    # Áo choàng phồng ôm lưng giữa
    _add_sculpted_blob(
        bm, col, (0.01, -0.06, 0.19), (0.15, 0.21, 0.11), rot=(0.08, 0.0, -0.06),
        segs=24, rings=14,
        disp_fn=lambda u: u * (0.014 * math.sin(u.y * 12.0) * math.cos(u.x * 8.0)),
        mat_index=0, color_fn=slug_skin,
    )

    # ĐẶC TRƯNG KHẢ NĂNG (Tiết Nhớt): 4 túi tuyến nhớt phát sáng xanh ngọc mọng trên lưng + vệt nhớt ướt ở đuôi
    sac_specs = [
        ((0.02, -0.04, 0.29), (0.075, 0.095, 0.065), (0.0, 0.0, 0.1)),
        ((-0.05, 0.09, 0.27), (0.055, 0.068, 0.052), (0.1, -0.2, 0.0)),
        ((0.06, 0.10, 0.26), (0.052, 0.062, 0.048), (0.1, 0.2, 0.0)),
        ((-0.02, 0.22, 0.21), (0.048, 0.056, 0.042), (0.2, 0.0, 0.0)),
    ]
    for pos, rad, rot in sac_specs:
        _add_sculpted_blob(
            bm, col, pos, rad, rot=rot, segs=18, rings=12,
            disp_fn=lambda u: u * (0.008 * math.sin(u.x * 10.0 + u.y * 10.0)),
            mat_index=3, color_fn=slime_sac_col,
        )

    # Vệt nhớt ướt bóng kéo dài phía sau đuôi
    w1._add_tube(bm, col, [(-0.04, 0.36, 0.014), (-0.06, 0.56, 0.010), (-0.04, 0.74, 0.006)],
                 [(0.08, 0.012), (0.095, 0.009), (0.05, 0.005)],
                 sides=14, subdiv=1, mat_index=3, color_fn=slime_sac_col)

    # 2 cặp râu mắt dài có nhãn cầu bóng + 2 râu khứu giác phía dưới
    for sx in (-1.0, 1.0):
        tip = (0.05 + sx * 0.095, -0.54, 0.29)
        w1._add_tube(bm, col, [(0.05 + sx * 0.045, -0.40, 0.17), (0.05 + sx * 0.078, -0.48, 0.23), tip],
                     [0.022, 0.015, 0.014], sides=10, subdiv=1, mat_index=0, color_fn=slug_skin)
        w1._add_eye_bulb(bm, col, tip, 0.022, yaw=sx * 0.22, eye_col=(0.12, 0.09, 0.06), mat_eye=1)
        w1._add_tube(bm, col, [(0.05 + sx * 0.04, -0.44, 0.09), (0.05 + sx * 0.085, -0.53, 0.055)],
                     [0.015, 0.006], sides=8, subdiv=0, mat_index=0, color_fn=slug_skin)

    # Rêu 3D & nụ bào tử thở sáng xen kẽ túi nhớt
    w1._add_moss_tuft(bm, col, (-0.08, -0.06, 0.25), scale=0.065, rng=rng)
    w1._add_moss_tuft(bm, col, (0.08, -0.04, 0.25), scale=0.060, rng=rng)
    w1._add_moss_tuft(bm, col, (-0.03, 0.20, 0.20), scale=0.058, rng=rng)
    w1._add_luma_bud(bm, col, (0.03, -0.14, 0.28), Vector((0.1, -0.2, 1.0)), size=0.032, hue=0.38)
    w1._add_luma_bud(bm, col, (-0.04, 0.16, 0.24), Vector((-0.2, 0.2, 1.0)), size=0.028, hue=0.42)

    bones = [
        ("Root", (0, 0.0, 0.09), (0, -0.18, 0.09), None, 0.34, 1.0),
        ("Head", (0.03, -0.24, 0.14), (0.05, -0.44, 0.13), "Root", 0.22, 2.2),
        ("Eye_L", (-0.01, -0.40, 0.17), (-0.045, -0.54, 0.29), "Head", 0.10, 2.8),
        ("Eye_R", (0.11, -0.40, 0.17), (0.145, -0.54, 0.29), "Head", 0.10, 2.8),
        ("Body_Mid", (0.0, 0.0, 0.15), (0.0, -0.10, 0.28), "Root", 0.25, 2.0),
        ("Tail", (-0.02, 0.16, 0.12), (-0.05, 0.44, 0.06), "Root", 0.22, 2.0),
    ]
    clips = {
        "Idle": (1, 60, [
            (1, {"Head": {"rot": (0.05, 0, 0)}, "Eye_L": {"rot": (0.12, 0.08, 0)}, "Eye_R": {"rot": (0.12, -0.08, 0)}}),
            (30, {"Head": {"rot": (-0.06, 0.04, 0.05)}, "Eye_L": {"rot": (-0.14, -0.10, 0)}, "Eye_R": {"rot": (-0.14, 0.10, 0)}}),
            (60, {"Head": {"rot": (0.05, 0, 0)}, "Eye_L": {"rot": (0.12, 0.08, 0)}, "Eye_R": {"rot": (0.12, -0.08, 0)}}),
        ]),
        "Walk": (1, 44, [
            (1, {"Head": {"loc": (0, -0.03, 0.01)}, "Body_Mid": {"scale": (1.06, 0.94, 0.98)}, "Tail": {"loc": (0, 0.03, 0)}}),
            (22, {"Head": {"loc": (0, 0.02, 0.0)}, "Body_Mid": {"scale": (0.94, 1.07, 1.04)}, "Tail": {"loc": (0, -0.04, 0.01)}}),
            (44, {"Head": {"loc": (0, -0.03, 0.01)}, "Body_Mid": {"scale": (1.06, 0.94, 0.98)}, "Tail": {"loc": (0, 0.03, 0)}}),
        ]),
        "Action": (1, 32, [
            (1, {"Body_Mid": {"scale": (1, 1, 1)}, "Head": {"rot": (0, 0, 0)}}),
            (12, {"Body_Mid": {"scale": (1.18, 1.18, 0.85)}, "Head": {"rot": (-0.22, 0, 0)}}),
            (20, {"Body_Mid": {"scale": (0.86, 0.86, 1.24)}, "Head": {"rot": (0.15, 0, 0)}}),
            (32, {"Body_Mid": {"scale": (1, 1, 1)}, "Head": {"rot": (0, 0, 0)}}),
        ]),
        "Hit": (1, 20, [
            (1, {"Root": {"scale": (1, 1, 1)}}),
            (7, {"Root": {"scale": (1.14, 0.80, 1.18)}}),
            (20, {"Root": {"scale": (1, 1, 1)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_SenReu",
        os.path.join(out_dir, "Creature_SenReu.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_13_sen_reu.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_14_muoi_reu(out_dir, prev_dir):
    """[M] Muỗi Rêu — Khả năng: Hút Máu.
    Ngực gù cơ bắp phủ rêu, mắt kép to lồi, vòi hút máu dài sắc có xúc tu,
    bụng sau phồng căng túi máu đỏ phát sáng mờ chia 6 đốt, 6 chân gập khúc 3 đốt, cánh gân mảnh."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()
    rng = random.Random(314)

    def chitin(p, n):
        return w1._lerp3((0.14, 0.24, 0.14), (0.30, 0.44, 0.22), 0.4 + 0.3 * math.sin(p.y * 22.0))

    def blood_sac(p, n):
        stripe = 0.5 + 0.5 * math.sin(p.y * 38.0)
        return w1._lerp3((0.48, 0.08, 0.10), (0.95, 0.18, 0.20), stripe)

    # Đầu tròn + 2 mắt kép to lồi + cặp râu lông tơ muỗi
    _add_sculpted_blob(bm, col, (0.0, -0.25, 0.43), (0.068, 0.064, 0.062),
                       segs=18, rings=12, mat_index=0, color_fn=chitin)
    for sx in (-1.0, 1.0):
        _add_sculpted_blob(bm, col, (sx * 0.048, -0.28, 0.445), (0.036, 0.038, 0.036),
                           segs=16, rings=10, mat_index=1,
                           color_fn=lambda _p, _n: (0.45, 0.10, 0.10))
        # Râu muỗi có đốt
        w1._add_tube(bm, col, [(sx * 0.025, -0.29, 0.465), (sx * 0.075, -0.36, 0.535), (sx * 0.11, -0.41, 0.57)],
                     [0.008, 0.005, 0.003], sides=6, subdiv=0, mat_index=0, color_fn=chitin)

    # ĐẶC TRƯNG KHẢ NĂNG (Hút Máu): Vòi hút máu dài sắc nhọn + 2 xúc tu hàm (palps)
    w1._add_tube(bm, col, [(0.0, -0.29, 0.41), (0.0, -0.44, 0.34), (0.0, -0.58, 0.25)],
                 [0.015, 0.009, 0.003], sides=10, subdiv=1, mat_index=2,
                 color_fn=lambda p, _n: w1._lerp3((0.18, 0.12, 0.10), (0.68, 0.12, 0.12), max(0.0, min(1.0, (-p.y - 0.35) * 4.0))))
    for sx in (-1.0, 1.0):
        w1._add_tube(bm, col, [(sx * 0.018, -0.29, 0.415), (sx * 0.028, -0.37, 0.38)],
                     [0.008, 0.004], sides=6, subdiv=0, mat_index=0, color_fn=chitin)

    # Ngực gù cơ bắp phủ rêu nhẹ + cổ nối
    w1._add_tube(bm, col, [(0.0, -0.21, 0.43), (0.0, -0.14, 0.45)], [(0.045, 0.042), (0.062, 0.060)],
                 sides=14, subdiv=1, mat_index=0, color_fn=chitin)
    _add_sculpted_blob(bm, col, (0.0, -0.08, 0.47), (0.105, 0.135, 0.112), rot=(0.15, 0.0, 0.0),
                       segs=22, rings=14,
                       disp_fn=lambda u: Vector((0.0, 0.0, 0.012 * max(0.0, u.z))),
                       mat_index=0, color_fn=chitin)
    w1._add_moss_tuft(bm, col, (0.0, -0.07, 0.585), scale=0.042, rng=rng)

    # ĐẶC TRƯNG KHẢ NĂNG (Hút Máu): Bụng sau 6 đốt phồng căng túi máu đỏ phát sáng mờ
    abd_pts = [(0.0, 0.03, 0.44), (0.0, 0.13, 0.41), (0.0, 0.25, 0.36), (0.0, 0.37, 0.29), (0.0, 0.47, 0.22)]
    abd_rads = [(0.055, 0.052), (0.095, 0.088), (0.118, 0.105), (0.092, 0.082), (0.036, 0.032)]
    w1._add_tube(bm, col, abd_pts, abd_rads, sides=20, subdiv=2, mat_index=3, color_fn=blood_sac)
    # Vòng đốt sừng ôm quanh bụng
    for by, bz, br in [(0.10, 0.42, 0.088), (0.20, 0.38, 0.115), (0.31, 0.33, 0.105)]:
        _add_sculpted_blob(bm, col, (0.0, by, bz), (br * 1.03, 0.022, br * 0.96),
                           segs=18, rings=8, mat_index=0, color_fn=chitin)

    # Cánh màng gân lá trong mờ + khung gân cứng + halteres thăng bằng
    for sx in (-1.0, 1.0):
        wing_pts = [
            (sx * 0.06, -0.07, 0.54),
            (sx * 0.24, -0.02, 0.62),
            (sx * 0.46, 0.07, 0.66),
            (sx * 0.62, 0.16, 0.64),
        ]
        wing_rads = [(0.025, 0.008), (0.072, 0.007), (0.088, 0.006), (0.036, 0.005)]
        w1._add_tube(bm, col, wing_pts, wing_rads, sides=12, subdiv=1, mat_index=4,
                     color_fn=lambda _p, _n: (0.64, 0.86, 0.72))
        # Gân cánh trước
        w1._add_tube(bm, col, [(sx * 0.06, -0.08, 0.545), (sx * 0.36, 0.01, 0.645), (sx * 0.61, 0.13, 0.645)],
                     [0.010, 0.007, 0.004], sides=8, subdiv=0, mat_index=0, color_fn=chitin)
        # Halteres (cánh thăng bằng nhỏ)
        w1._add_tube(bm, col, [(sx * 0.06, 0.02, 0.47), (sx * 0.12, 0.05, 0.49)],
                     [0.006, 0.012], sides=6, subdiv=0, mat_index=3, color_fn=lambda _p, _n: (0.6, 0.9, 0.5))

    # 6 chân dài gập khúc 3 đốt rõ khối
    leg_bases = [(-0.12, 0.16), (0.01, 0.22), (0.14, 0.18)]
    for i, (ly, span) in enumerate(leg_bases):
        for sx in (-1.0, 1.0):
            hip = (sx * 0.065, -0.08 + ly * 0.5, 0.38)
            knee = (sx * (0.16 + span * 0.5), -0.10 + ly * 1.2, 0.49)
            ankle = (sx * (0.24 + span * 0.75), -0.08 + ly * 1.6, 0.20)
            foot = (sx * (0.28 + span * 0.85), -0.06 + ly * 1.9, 0.02)
            _add_jointed_limb(bm, col, [hip, knee, ankle, foot], [0.014, 0.011, 0.008, 0.004],
                              claw_dir=(sx * 0.4, -0.4, -0.3), claw_len=0.025,
                              mat_index=0, color_fn=chitin)

    bones = [
        ("Root", (0, 0.0, 0.42), (0, -0.12, 0.46), None, 0.28, 1.0),
        ("Head", (0, -0.18, 0.44), (0, -0.32, 0.42), "Root", 0.20, 2.2),
        ("Wing_L", (-0.06, -0.07, 0.54), (-0.56, 0.14, 0.65), "Root", 0.22, 2.5),
        ("Wing_R", (0.06, -0.07, 0.54), (0.56, 0.14, 0.65), "Root", 0.22, 2.5),
        ("Abdomen", (0, 0.04, 0.44), (0, 0.46, 0.22), "Root", 0.20, 2.2),
        ("Proboscis", (0, -0.29, 0.41), (0, -0.58, 0.25), "Head", 0.12, 2.8),
    ]
    clips = {
        "Idle": (1, 40, [
            (1, {"Root": {"loc": (0, 0, 0.0)}, "Wing_L": {"rot": (0, -0.35, 0)}, "Wing_R": {"rot": (0, 0.35, 0)}, "Abdomen": {"rot": (0.05, 0, 0)}}),
            (20, {"Root": {"loc": (0, 0, 0.04)}, "Wing_L": {"rot": (0, 0.38, 0)}, "Wing_R": {"rot": (0, -0.38, 0)}, "Abdomen": {"rot": (-0.08, 0, 0)}}),
            (40, {"Root": {"loc": (0, 0, 0.0)}, "Wing_L": {"rot": (0, -0.35, 0)}, "Wing_R": {"rot": (0, 0.35, 0)}, "Abdomen": {"rot": (0.05, 0, 0)}}),
        ]),
        "Walk": (1, 16, [
            (1, {"Root": {"loc": (0, -0.02, 0.05)}, "Wing_L": {"rot": (0, -0.62, 0)}, "Wing_R": {"rot": (0, 0.62, 0)}}),
            (8, {"Root": {"loc": (0, 0.02, 0.0)}, "Wing_L": {"rot": (0, 0.58, 0)}, "Wing_R": {"rot": (0, -0.58, 0)}}),
            (16, {"Root": {"loc": (0, -0.02, 0.05)}, "Wing_L": {"rot": (0, -0.62, 0)}, "Wing_R": {"rot": (0, 0.62, 0)}}),
        ]),
        "Action": (1, 24, [
            (1, {"Root": {"rot": (0, 0, 0)}, "Proboscis": {"rot": (0, 0, 0)}, "Abdomen": {"scale": (1, 1, 1)}}),
            (8, {"Root": {"rot": (0.28, 0, 0)}, "Proboscis": {"rot": (0.18, 0, 0)}, "Abdomen": {"scale": (1.16, 1.12, 1.16)}}),
            (15, {"Root": {"rot": (-0.15, 0, 0)}, "Proboscis": {"rot": (-0.12, 0, 0)}, "Abdomen": {"scale": (1.22, 1.14, 1.22)}}),
            (24, {"Root": {"rot": (0, 0, 0)}, "Proboscis": {"rot": (0, 0, 0)}, "Abdomen": {"scale": (1, 1, 1)}}),
        ]),
        "Hit": (1, 16, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (6, {"Root": {"rot": (-0.30, 0.25, 0.20)}}),
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
    """[S] Rết Rêu — Khả năng: Nọc Tê.
    10 đốt giáp vòm 3D xếp lớp phủ rêu, đầu có cặp kìm nọc độc cong móc câu phát sáng tím-xanh,
    16 chân phân đốt có đùi-cẳng-móng và 2 đuôi kìm."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()
    rng = random.Random(315)

    def armor_col(p, n):
        seg_wave = 0.5 + 0.5 * math.sin(p.y * 26.0)
        base = w1._lerp3((0.12, 0.26, 0.14), (0.34, 0.54, 0.22), seg_wave)
        if abs(p.x) > 0.08:
            base = w1._lerp3(base, (0.48, 0.36, 0.16), 0.55)
        return base

    def venom_col(p, n):
        return w1._lerp3((0.35, 0.16, 0.65), (0.58, 0.96, 0.42), 0.5 + 0.5 * math.sin(p.y * 30.0))

    # Thân rết uốn chữ S mượt (10 đốt giáp vòm xếp lớp, không dẹp)
    seg_centers = []
    for i in range(10):
        t = i / 9.0
        y = -0.52 + t * 1.06
        x = 0.11 * math.sin(t * math.pi * 1.9)
        z = 0.125 + 0.015 * math.sin(t * math.pi)
        seg_centers.append((x, y, z))

    # Lõi thân nối liền các đốt
    w1._add_tube(bm, col, seg_centers, [(0.075, 0.060)] * 10, sides=16, subdiv=1,
                 mat_index=0, color_fn=armor_col)

    for i, (cx, cy, cz) in enumerate(seg_centers):
        t = i / 9.0
        w_s = 0.105 * (0.84 + 0.28 * math.sin(t * math.pi))
        h_s = 0.080 * (0.85 + 0.25 * math.sin(t * math.pi))
        yaw = 0.22 * math.cos(t * math.pi * 1.9)
        # Đốt giáp vòm có gờ sống lưng
        _add_sculpted_blob(
            bm, col, (cx, cy, cz + 0.01), (w_s, 0.082, h_s), rot=(0.06, 0.0, yaw),
            segs=18, rings=10,
            disp_fn=lambda u: Vector((0.0, 0.0, 0.014 * max(0.0, u.z) * (1.0 - abs(u.x)))),
            mat_index=2, color_fn=armor_col,
        )
        if i % 2 == 1 and 0 < i < 9:
            w1._add_moss_tuft(bm, col, (cx, cy, cz + h_s + 0.01), scale=0.042, rng=rng)

        # Cặp chân phân đốt (8 cặp = 16 chân có khớp đùi-cẳng-móng)
        if 1 <= i <= 8:
            for sx in (-1.0, 1.0):
                hip = (cx + sx * w_s * 0.78, cy, cz - 0.02)
                knee = (cx + sx * (w_s + 0.09), cy + sx * 0.025, cz + 0.045)
                ankle = (cx + sx * (w_s + 0.145), cy + sx * 0.035, 0.045)
                foot = (cx + sx * (w_s + 0.165), cy + sx * 0.045, 0.008)
                _add_jointed_limb(bm, col, [hip, knee, ankle, foot], [0.020, 0.015, 0.011, 0.006],
                                  claw_dir=(sx * 0.6, -0.3, -0.4), claw_len=0.024,
                                  mat_index=2, color_fn=lambda _p, _n: (0.42, 0.30, 0.14))

    # Đầu rết + mắt kép cụm + râu phân đốt dài
    hx, hy, hz = seg_centers[0]
    _add_sculpted_blob(bm, col, (hx, hy - 0.05, hz), (0.095, 0.095, 0.072),
                       segs=18, rings=12, mat_index=2, color_fn=armor_col)
    for sx in (-1.0, 1.0):
        w1._add_eye_bulb(bm, col, (hx + sx * 0.055, hy - 0.10, hz + 0.03), 0.018,
                         yaw=sx * 0.4, eye_col=(0.95, 0.65, 0.15), mat_eye=3)
        # Râu dài phân đốt
        w1._add_tube(bm, col, [
            (hx + sx * 0.04, hy - 0.12, hz + 0.02),
            (hx + sx * 0.11, hy - 0.24, hz + 0.07),
            (hx + sx * 0.16, hy - 0.36, hz + 0.10),
        ], [0.015, 0.009, 0.004], sides=8, subdiv=1, mat_index=0, color_fn=armor_col)

        # ĐẶC TRƯNG KHẢ NĂNG (Nọc Tê): Cặp kìm nọc độc (forcipules) cong móc câu + tuyến nọc phát sáng
        _add_sculpted_blob(bm, col, (hx + sx * 0.055, hy - 0.11, hz - 0.025), (0.032, 0.038, 0.028),
                           segs=14, rings=10, mat_index=3, color_fn=venom_col)
        w1._add_tube(bm, col, [
            (hx + sx * 0.06, hy - 0.13, hz - 0.02),
            (hx + sx * 0.10, hy - 0.21, hz - 0.03),
            (hx + sx * 0.03, hy - 0.26, hz - 0.02),
        ], [0.022, 0.014, 0.003], sides=10, subdiv=1, mat_index=3, color_fn=venom_col)

    # 2 đuôi kìm cảm giác phía sau
    tx, ty, tz = seg_centers[-1]
    for sx in (-1.0, 1.0):
        w1._add_tube(bm, col, [(tx + sx * 0.04, ty + 0.03, tz), (tx + sx * 0.11, ty + 0.18, tz + 0.05),
                               (tx + sx * 0.09, ty + 0.26, tz + 0.08)],
                     [0.018, 0.011, 0.004], sides=8, subdiv=1, mat_index=2, color_fn=armor_col)

    bones = [
        ("Root", (0, 0.0, 0.12), (0, -0.14, 0.12), None, 0.30, 1.0),
        ("Head", (hx, hy + 0.04, hz), (hx, hy - 0.22, hz), "Root", 0.22, 2.2),
        ("Fang_L", (hx - 0.05, hy - 0.12, hz - 0.02), (hx - 0.04, hy - 0.26, hz - 0.02), "Head", 0.12, 2.8),
        ("Fang_R", (hx + 0.05, hy - 0.12, hz - 0.02), (hx + 0.04, hy - 0.26, hz - 0.02), "Head", 0.12, 2.8),
        ("Body_Mid", seg_centers[4], seg_centers[6], "Root", 0.26, 2.0),
        ("Tail", seg_centers[7], (tx, ty + 0.20, tz + 0.04), "Body_Mid", 0.24, 2.0),
    ]
    clips = {
        "Idle": (1, 48, [
            (1, {"Head": {"rot": (0, 0, 0.10)}, "Fang_L": {"rot": (0, 0, 0.10)}, "Fang_R": {"rot": (0, 0, -0.10)}, "Tail": {"rot": (0, 0, -0.12)}}),
            (24, {"Head": {"rot": (0, 0, -0.10)}, "Fang_L": {"rot": (0, 0, -0.05)}, "Fang_R": {"rot": (0, 0, 0.05)}, "Tail": {"rot": (0, 0, 0.12)}}),
            (48, {"Head": {"rot": (0, 0, 0.10)}, "Fang_L": {"rot": (0, 0, 0.10)}, "Fang_R": {"rot": (0, 0, -0.10)}, "Tail": {"rot": (0, 0, -0.12)}}),
        ]),
        "Walk": (1, 18, [
            (1, {"Head": {"rot": (0, 0, 0.20)}, "Body_Mid": {"rot": (0, 0, -0.22)}, "Tail": {"rot": (0, 0, 0.24)}}),
            (9, {"Head": {"rot": (0, 0, -0.20)}, "Body_Mid": {"rot": (0, 0, 0.22)}, "Tail": {"rot": (0, 0, -0.24)}}),
            (18, {"Head": {"rot": (0, 0, 0.20)}, "Body_Mid": {"rot": (0, 0, -0.22)}, "Tail": {"rot": (0, 0, 0.24)}}),
        ]),
        "Attack": (1, 22, [
            (1, {"Root": {"loc": (0, 0, 0)}, "Head": {"rot": (0, 0, 0)}}),
            (7, {"Root": {"loc": (0, 0.10, 0.04)}, "Head": {"rot": (-0.22, 0, 0)}, "Fang_L": {"rot": (0, 0, -0.35)}, "Fang_R": {"rot": (0, 0, 0.35)}}),
            (13, {"Root": {"loc": (0, -0.22, 0.02)}, "Head": {"rot": (0.18, 0, 0)}, "Fang_L": {"rot": (0, 0, 0.24)}, "Fang_R": {"rot": (0, 0, -0.24)}}),
            (22, {"Root": {"loc": (0, 0, 0)}, "Head": {"rot": (0, 0, 0)}}),
        ]),
        "Hit": (1, 18, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (6, {"Root": {"rot": (-0.22, 0.20, 0.25)}}),
            (18, {"Root": {"rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_RetReu",
        os.path.join(out_dir, "Creature_RetReu.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_15_ret_reu.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_16_reu_me(out_dir, prev_dir):
    """[C] Rêu Mẹ — Khả năng: Tái Sinh.
    Sinh vật rêu cổ thụ biết bò: lõi thân hữu cơ uốn vươn, 4 rễ-chân to mập bám đất,
    4 tầng tán rêu/địa y bồng bềnh xếp lớp, 3 búp hoa Tái Sinh phát sáng xanh lục rực rỡ + mạch rễ sáng."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()
    rng = random.Random(316)

    def bark_moss(p, n):
        noise = 0.5 + 0.5 * math.sin(p.x * 11.0 + p.y * 13.0 + p.z * 9.0)
        base = w1._lerp3((0.16, 0.22, 0.10), (0.28, 0.52, 0.20), noise)
        if n.z > 0.35:
            base = w1._lerp3(base, (0.36, 0.68, 0.26), 0.55)
        return base

    def regen_glow(p, n):
        return w1._lerp3((0.28, 0.92, 0.45), (0.78, 1.0, 0.52), 0.5 + 0.5 * math.sin(p.z * 16.0))

    # Lõi thân rêu cổ thụ phồng cao 3D có nếp vỏ cây & các tầng rêu xếp lớp
    _add_sculpted_blob(
        bm, col, (0.0, 0.0, 0.34), (0.36, 0.38, 0.28),
        segs=28, rings=18,
        disp_fn=lambda u: u * (0.042 * math.sin(u.x * 6.0 + u.y * 5.0) + 0.025 * math.cos(u.z * 9.0)),
        mat_index=0, color_fn=bark_moss,
    )
    # Tầng tán rêu bồng bềnh xếp lớp quanh vai & đỉnh
    canopy_blobs = [
        ((0.0, -0.05, 0.58), (0.26, 0.28, 0.16), (0.1, 0.0, 0.0)),
        ((-0.24, 0.06, 0.44), (0.20, 0.22, 0.14), (0.0, -0.3, 0.2)),
        ((0.24, 0.04, 0.44), (0.20, 0.22, 0.14), (0.0, 0.3, -0.2)),
        ((0.0, 0.24, 0.40), (0.22, 0.20, 0.14), (-0.3, 0.0, 0.0)),
    ]
    for pos, rad, rot in canopy_blobs:
        _add_sculpted_blob(
            bm, col, pos, rad, rot=rot, segs=20, rings=12,
            disp_fn=lambda u: u * (0.030 * math.sin(u.x * 8.0) * math.cos(u.y * 7.0)),
            mat_index=0, color_fn=bark_moss,
        )

    # 3 mắt cảm quang ấm ẩn trong khe rêu phía trước
    for mx, mz, r in [(-0.09, 0.33, 0.036), (0.09, 0.33, 0.036), (0.0, 0.39, 0.026)]:
        w1._add_eye_bulb(bm, col, (mx, -0.34, mz), r, yaw=mx * 2.0,
                         eye_col=(0.45, 0.96, 0.42), mat_eye=3)

    # 4 rễ-chân to mập bám đất chia khớp rõ rệt + ngón rễ
    for sx, sy in [(-1.0, -1.0), (1.0, -1.0), (-1.0, 1.0), (1.0, 1.0)]:
        hip = (sx * 0.24, sy * 0.20, 0.24)
        knee = (sx * 0.42, sy * 0.34, 0.18)
        ankle = (sx * 0.52, sy * 0.44, 0.07)
        foot = (sx * 0.56, sy * 0.48, 0.015)
        w1._add_tube(bm, col, [hip, knee, ankle, foot], [0.11, 0.082, 0.064, 0.048],
                     sides=12, subdiv=1, mat_index=0, color_fn=bark_moss)
        # 2 ngón rễ xòe bám đất
        for dy in (-0.06, 0.06):
            w1._add_tube(bm, col, [foot, (foot[0] + sx * 0.11, foot[1] + sy * 0.06 + dy, 0.008)],
                         [0.032, 0.010], sides=8, subdiv=0, mat_index=0, color_fn=bark_moss)

    # ĐẶC TRƯNG KHẢ NĂNG (Tái Sinh): 3 búp hoa/bào tử Tái Sinh lớn phát sáng + mạch rễ sáng dẫn năng lượng
    bud_positions = [
        ((0.0, -0.02, 0.72), Vector((0.0, -0.1, 1.0)), 0.095),
        ((-0.22, 0.08, 0.55), Vector((-0.5, 0.1, 1.0)), 0.065),
        ((0.22, 0.06, 0.55), Vector((0.5, 0.1, 1.0)), 0.065),
    ]
    for bpos, bdir, bsize in bud_positions:
        w1._add_luma_bud(bm, col, bpos, bdir, size=bsize, hue=0.35)
        # Đài hoa lá rêu ôm quanh búp
        for k in range(4):
            ang = k * math.pi * 0.5 + 0.4
            d_vec = Vector((math.cos(ang) * 0.7, math.sin(ang) * 0.7, 0.5))
            _add_layered_feather(bm, col, Vector(bpos) - Vector((0, 0, bsize * 0.4)), d_vec, Vector((0, 0, 1)),
                                 length=bsize * 1.35, width=bsize * 0.55, thickness=0.012,
                                 mat_index=0, color_fn=bark_moss)

    # Mạch rễ sáng chạy dọc thân nối tới búp trung tâm
    for k in range(4):
        ang = k * math.pi * 0.5 + 0.7
        ca, sa = math.cos(ang), math.sin(ang)
        w1._add_tube(bm, col, [
            (ca * 0.36, sa * 0.36, 0.14),
            (ca * 0.38, sa * 0.38, 0.36),
            (ca * 0.20, sa * 0.20, 0.60),
            (ca * 0.04, sa * 0.04, 0.70),
        ], [0.016, 0.018, 0.014, 0.008], sides=8, subdiv=1, mat_index=3, color_fn=regen_glow)

    for _ in range(8):
        ang = rng.uniform(0, 2 * math.pi)
        r = rng.uniform(0.18, 0.36)
        w1._add_moss_tuft(bm, col, (math.cos(ang) * r, math.sin(ang) * r, rng.uniform(0.32, 0.58)),
                          scale=rng.uniform(0.055, 0.085), rng=rng)

    bones = [
        ("Root", (0, 0, 0.24), (0, 0, 0.50), None, 0.55, 1.0),
        ("Crown", (0, 0, 0.52), (0, 0, 0.80), "Root", 0.35, 2.2),
        ("Leg_FL", (-0.24, -0.20, 0.24), (-0.54, -0.46, 0.02), "Root", 0.25, 2.4),
        ("Leg_FR", (0.24, -0.20, 0.24), (0.54, -0.46, 0.02), "Root", 0.25, 2.4),
        ("Leg_BL", (-0.24, 0.20, 0.24), (-0.54, 0.46, 0.02), "Root", 0.25, 2.4),
    ]
    clips = {
        "Idle": (1, 64, [
            (1, {"Root": {"scale": (1.0, 1.0, 1.0)}, "Crown": {"scale": (1.0, 1.0, 1.0)}}),
            (32, {"Root": {"scale": (1.05, 1.05, 0.96)}, "Crown": {"scale": (1.10, 1.10, 1.10)}}),
            (64, {"Root": {"scale": (1.0, 1.0, 1.0)}, "Crown": {"scale": (1.0, 1.0, 1.0)}}),
        ]),
        "Walk": (1, 36, [
            (1, {"Root": {"rot": (0.04, 0.06, 0)}, "Leg_FL": {"rot": (0.35, 0, 0)}, "Leg_BL": {"rot": (-0.30, 0, 0)}}),
            (18, {"Root": {"rot": (-0.04, -0.06, 0)}, "Leg_FL": {"rot": (-0.30, 0, 0)}, "Leg_BL": {"rot": (0.35, 0, 0)}}),
            (36, {"Root": {"rot": (0.04, 0.06, 0)}, "Leg_FL": {"rot": (0.35, 0, 0)}, "Leg_BL": {"rot": (-0.30, 0, 0)}}),
        ]),
        "Action": (1, 44, [
            (1, {"Root": {"loc": (0, 0, 0), "scale": (1, 1, 1)}, "Crown": {"scale": (1, 1, 1)}}),
            (16, {"Root": {"loc": (0, 0, -0.06), "scale": (1.14, 1.14, 0.88)}, "Crown": {"scale": (1.28, 1.28, 1.28)}}),
            (28, {"Root": {"loc": (0, 0, 0.04), "scale": (0.94, 0.94, 1.10)}, "Crown": {"scale": (1.16, 1.16, 1.16)}}),
            (44, {"Root": {"loc": (0, 0, 0), "scale": (1, 1, 1)}, "Crown": {"scale": (1, 1, 1)}}),
        ]),
        "Hit": (1, 22, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (7, {"Root": {"rot": (-0.18, 0.16, 0.12)}}),
            (22, {"Root": {"rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_ReuMe",
        os.path.join(out_dir, "Creature_ReuMe.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_16_reu_me.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


# ================================================================== HANG 4: TỔ BỎ HOANG (4 LOÀI)

def build_17_chim_non(out_dir, prev_dir):
    """[M] Chim Non — Khả năng: Rơi Nhẹ.
    Thân tròn mũm mĩm nhiều lớp lông tơ xù, đầu to mắt long lanh, mỏ mở nhẹ kêu chíp,
    cặp cánh xòe quạt nhiều lông ngắn để lướt rơi nhẹ."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()

    def down_feather(p, n):
        blend = max(0.0, min(1.0, (p.z - 0.12) * 3.0))
        base = w1._lerp3((0.86, 0.78, 0.56), (0.64, 0.48, 0.30), blend)
        if p.y < -0.05 and p.z < 0.24:
            base = w1._lerp3(base, (0.95, 0.88, 0.68), 0.65)
        return base

    # Thân tròn mũm mĩm (ngực nở, mông tròn) + đầu to
    _add_sculpted_blob(bm, col, (0.0, 0.02, 0.20), (0.145, 0.165, 0.142), rot=(-0.20, 0.0, 0.0),
                       segs=24, rings=16,
                       disp_fn=lambda u: Vector((0.0, -0.015 * max(0.0, -u.y), 0.0)),
                       mat_index=0, color_fn=down_feather)
    _add_sculpted_blob(bm, col, (0.0, -0.09, 0.34), (0.125, 0.125, 0.118),
                       segs=24, rings=16, mat_index=0, color_fn=down_feather)

    # Chùm lông tơ xù trên đỉnh đầu, hai má và ức
    for ang, tilt in [(0.0, 0.2), (-0.4, 0.5), (0.4, 0.5)]:
        _add_layered_feather(bm, col, (math.sin(ang) * 0.03, -0.08, 0.45),
                             (math.sin(ang) * 0.4, 0.35, 0.85), (0, -1, 0.3),
                             length=0.075, width=0.028, thickness=0.010, mat_index=0, color_fn=down_feather)
    for sx in (-1.0, 1.0):
        _add_layered_feather(bm, col, (sx * 0.09, -0.11, 0.32), (sx * 0.6, 0.5, 0.2), (0, 0, 1),
                             length=0.065, width=0.032, thickness=0.012, mat_index=0, color_fn=down_feather)

    # Mỏ ngắn mở nhẹ (hàm trên & hàm dưới) như đang kêu chíp
    w1._add_tube(bm, col, [(0.0, -0.20, 0.345), (0.0, -0.27, 0.340), (0.0, -0.31, 0.330)],
                 [(0.038, 0.024), (0.024, 0.015), (0.005, 0.004)], sides=10, subdiv=1,
                 mat_index=2, color_fn=lambda _p, _n: (0.94, 0.68, 0.22))
    w1._add_tube(bm, col, [(0.0, -0.19, 0.322), (0.0, -0.25, 0.305), (0.0, -0.29, 0.295)],
                 [(0.030, 0.016), (0.018, 0.010), (0.004, 0.003)], sides=10, subdiv=1,
                 mat_index=2, color_fn=lambda _p, _n: (0.86, 0.56, 0.18))

    # Mắt to tròn long lanh
    for sx in (-1.0, 1.0):
        w1._add_eye_bulb(bm, col, (sx * 0.085, -0.175, 0.365), 0.032, yaw=sx * 0.35,
                         eye_col=(0.08, 0.06, 0.05), mat_eye=1)

    # ĐẶC TRƯNG KHẢ NĂNG (Rơi Nhẹ): Cặp cánh xòe quạt nhiều lớp lông ngắn + đuôi lông tơ
    for sx in (-1.0, 1.0):
        # Xương cánh ngắn
        w1._add_tube(bm, col, [(sx * 0.12, -0.02, 0.24), (sx * 0.24, 0.02, 0.26), (sx * 0.32, 0.06, 0.24)],
                     [0.036, 0.028, 0.018], sides=10, subdiv=1, mat_index=0, color_fn=down_feather)
        # 5 lông cánh xòe quạt tách rõ từng lông
        for f_i in range(5):
            ft = f_i / 4.0
            base_p = (sx * (0.14 + ft * 0.16), -0.01 + ft * 0.05, 0.245)
            dir_v = (sx * (0.92 - ft * 0.45), 0.35 + ft * 0.60, -0.06 + (f_i % 2) * 0.05)
            _add_layered_feather(bm, col, base_p, dir_v, (0, 0, 1),
                                 length=0.16 - ft * 0.025, width=0.028, thickness=0.009,
                                 mat_index=0, color_fn=down_feather)

    # Đuôi xòe 3 lông
    for sx in (-0.35, 0.0, 0.35):
        _add_layered_feather(bm, col, (sx * 0.05, 0.15, 0.17), (sx * 0.5, 1.0, 0.25), (0, 0, 1),
                             length=0.11, width=0.036, thickness=0.010, mat_index=0, color_fn=down_feather)

    # Chân 3 ngón trước + 1 ngón sau
    for sx in (-1.0, 1.0):
        _add_jointed_limb(bm, col, [(sx * 0.065, 0.01, 0.10), (sx * 0.072, 0.03, 0.05), (sx * 0.072, -0.01, 0.012)],
                          [0.022, 0.014, 0.010], claw_dir=(0, -1, 0), claw_len=0.03,
                          mat_index=2, color_fn=lambda _p, _n: (0.86, 0.62, 0.26))
        for dx in (-0.026, 0.0, 0.026):
            w1._add_tube(bm, col, [(sx * 0.072, -0.01, 0.012), (sx * 0.072 + dx, -0.055, 0.008)],
                         [0.008, 0.004], sides=6, subdiv=0, mat_index=2, color_fn=lambda _p, _n: (0.86, 0.62, 0.26))

    bones = [
        ("Root", (0, 0.02, 0.20), (0, -0.04, 0.28), None, 0.25, 1.0),
        ("Head", (0, -0.05, 0.28), (0, -0.12, 0.40), "Root", 0.18, 2.2),
        ("Wing_L", (-0.12, -0.01, 0.24), (-0.36, 0.08, 0.24), "Root", 0.16, 2.4),
        ("Wing_R", (0.12, -0.01, 0.24), (0.36, 0.08, 0.24), "Root", 0.16, 2.4),
        ("Leg_L", (-0.065, 0.01, 0.10), (-0.072, -0.02, 0.01), "Root", 0.10, 2.6),
        ("Leg_R", (0.065, 0.01, 0.10), (0.072, -0.02, 0.01), "Root", 0.10, 2.6),
    ]
    clips = {
        "Idle": (1, 44, [
            (1, {"Head": {"rot": (0.05, 0, 0.08)}, "Wing_L": {"rot": (0, -0.12, 0)}, "Wing_R": {"rot": (0, 0.12, 0)}}),
            (22, {"Head": {"rot": (-0.10, 0, -0.10)}, "Wing_L": {"rot": (0, 0.16, 0)}, "Wing_R": {"rot": (0, -0.16, 0)}}),
            (44, {"Head": {"rot": (0.05, 0, 0.08)}, "Wing_L": {"rot": (0, -0.12, 0)}, "Wing_R": {"rot": (0, 0.12, 0)}}),
        ]),
        "Walk": (1, 18, [
            (1, {"Root": {"loc": (0, 0, 0.0)}, "Wing_L": {"rot": (0, -0.45, 0)}, "Wing_R": {"rot": (0, 0.45, 0)}, "Leg_L": {"rot": (0.35, 0, 0)}, "Leg_R": {"rot": (-0.35, 0, 0)}}),
            (9, {"Root": {"loc": (0, 0, 0.08)}, "Wing_L": {"rot": (0, 0.40, 0)}, "Wing_R": {"rot": (0, -0.40, 0)}, "Leg_L": {"rot": (-0.35, 0, 0)}, "Leg_R": {"rot": (0.35, 0, 0)}}),
            (18, {"Root": {"loc": (0, 0, 0.0)}, "Wing_L": {"rot": (0, -0.45, 0)}, "Wing_R": {"rot": (0, 0.45, 0)}, "Leg_L": {"rot": (0.35, 0, 0)}, "Leg_R": {"rot": (-0.35, 0, 0)}}),
        ]),
        "Hit": (1, 18, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (6, {"Root": {"rot": (-0.25, 0.24, 0.15)}, "Wing_L": {"rot": (0, -0.55, 0)}, "Wing_R": {"rot": (0, 0.55, 0)}}),
            (18, {"Root": {"rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_ChimNon",
        os.path.join(out_dir, "Creature_ChimNon.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_17_chim_non.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_18_trung_la(out_dir, prev_dir):
    """[Kỳ Ngộ] Trứng Lạ — Khả năng: Nở trong Túi, cho dòng gene lạ.
    Ổ rơm-lá-rêu đan vòng bên dưới, quả trứng phồng 3D hữu cơ với mạng gân mạch sống
    phát sáng tím-ngọc xoắn quanh vỏ và các mầm vảy gene lạ nhú lên."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()
    rng = random.Random(318)

    def shell_col(p, n):
        swirl = 0.5 + 0.5 * math.sin(math.atan2(p.y, p.x) * 4.0 + p.z * 14.0)
        base = w1._lerp3((0.76, 0.72, 0.62), (0.48, 0.34, 0.58), 0.30 + 0.45 * swirl)
        return base

    def gene_vein_col(p, n):
        return w1._lerp3((0.42, 0.88, 0.96), (0.86, 0.38, 0.98), 0.5 + 0.5 * math.sin(p.z * 20.0))

    # Ổ rơm - cành khô - rêu đan vòng dưới đáy trứng
    for k in range(14):
        ang = k * (2.0 * math.pi / 14.0) + rng.uniform(-0.1, 0.1)
        ca, sa = math.cos(ang), math.sin(ang)
        t_ang = ang + math.pi * 0.5
        tc, ts = math.cos(t_ang), math.sin(t_ang)
        r_ring = 0.23 + rng.uniform(-0.02, 0.03)
        z_ring = 0.035 + rng.uniform(-0.01, 0.02)
        p0 = (ca * r_ring - tc * 0.13, sa * r_ring - ts * 0.13, z_ring)
        p1 = (ca * (r_ring + 0.02), sa * (r_ring + 0.02), z_ring + 0.025)
        p2 = (ca * r_ring + tc * 0.13, sa * r_ring + ts * 0.13, z_ring)
        w1._add_tube(bm, col, [p0, p1, p2], [0.022, 0.026, 0.020], sides=8, subdiv=0,
                     mat_index=0, color_fn=lambda _p, _n: (0.46, 0.34, 0.20))
    for k in range(4):
        ang = k * math.pi * 0.5 + 0.3
        w1._add_moss_tuft(bm, col, (math.cos(ang) * 0.21, math.sin(ang) * 0.21, 0.06), scale=0.052, rng=rng)

    # Quả trứng hữu cơ phồng đầy (đáy tròn rộng, đỉnh thon tự nhiên)
    _add_sculpted_blob(
        bm, col, (0.0, 0.0, 0.27), (0.185, 0.185, 0.255),
        segs=28, rings=20,
        disp_fn=lambda u: Vector((
            u.x * (0.025 * (1.0 - u.z) - 0.012 * max(0.0, u.z)),
            u.y * (0.025 * (1.0 - u.z) - 0.012 * max(0.0, u.z)),
            0.006 * math.sin(u.x * 9.0 + u.y * 9.0),
        )),
        mat_index=2, color_fn=shell_col,
    )

    # ĐẶC TRƯNG KHẢ NĂNG (Gene Lạ): 5 đường gân mạch sống phát sáng xoắn quanh vỏ + mầm vảy nhú
    for k in range(5):
        base_ang = k * (2.0 * math.pi / 5.0)
        vein_pts = []
        vein_rads = []
        for s in range(7):
            t = s / 6.0
            z = 0.08 + t * 0.40
            ang = base_ang + t * 1.35
            # Bán kính khớp vỏ trứng
            u_z = (z - 0.27) / 0.255
            r_shell = 0.186 * math.sqrt(max(0.05, 1.0 - u_z * u_z)) * (1.0 + 0.022 * (1.0 - u_z))
            vein_pts.append((math.cos(ang) * r_shell, math.sin(ang) * r_shell, z))
            vein_rads.append(0.013 * math.sin(t * math.pi) + 0.005)
        w1._add_tube(bm, col, vein_pts, vein_rads, sides=8, subdiv=1, mat_index=3, color_fn=gene_vein_col)

    # Mầm vảy/sừng gene lạ nhú lên quanh vai trứng
    for k in range(4):
        ang = k * math.pi * 0.5 + 0.2
        ca, sa = math.cos(ang), math.sin(ang)
        w1._add_tube(bm, col, [(ca * 0.13, sa * 0.13, 0.38), (ca * 0.18, sa * 0.18, 0.45), (ca * 0.20, sa * 0.20, 0.51)],
                     [0.024, 0.015, 0.004], sides=8, subdiv=0, mat_index=3, color_fn=gene_vein_col)

    bones = [
        ("Root", (0, 0, 0.04), (0, 0, 0.26), None, 0.35, 1.0),
        ("Egg", (0, 0, 0.14), (0, 0, 0.48), "Root", 0.32, 2.2),
    ]
    clips = {
        "Idle": (1, 48, [
            (1, {"Egg": {"rot": (0.04, 0.04, 0), "scale": (1.0, 1.0, 1.0)}}),
            (12, {"Egg": {"rot": (-0.06, 0.05, 0.04), "scale": (1.04, 1.04, 0.97)}}),
            (24, {"Egg": {"rot": (0.05, -0.05, -0.03), "scale": (0.98, 0.98, 1.04)}}),
            (36, {"Egg": {"rot": (-0.04, -0.04, 0.02), "scale": (1.03, 1.03, 0.98)}}),
            (48, {"Egg": {"rot": (0.04, 0.04, 0), "scale": (1.0, 1.0, 1.0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_TrungLa",
        os.path.join(out_dir, "Creature_TrungLa.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_18_trung_la.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_19_ran_trom_trung(out_dir, prev_dir):
    """[S] Rắn Trộm Trứng — Khả năng: Nuốt Nhanh.
    Thân rắn uốn chữ S 3D nâng đầu rình mồi, vảy lưng có sống & hoa văn khoang,
    hàm dưới tách khớp mở rộng, cổ họng phình tròn rõ khối quả trứng đang nuốt."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()

    def snake_scale(p, n):
        band = 0.5 + 0.5 * math.sin(p.y * 22.0 + p.x * 16.0)
        base = w1._lerp3((0.18, 0.16, 0.14), (0.64, 0.50, 0.28), band)
        if n.z < -0.25:
            base = w1._lerp3(base, (0.86, 0.80, 0.62), 0.72)
        return base

    # Thân rắn uốn chữ S 3D nâng cổ lên phía trước, phần cổ có ĐOẠN PHÌNH TRÒN ĐANG NUỐT TRỨNG
    pts = [
        (0.0, -0.44, 0.34),    # đầu nâng cao
        (0.02, -0.32, 0.28),   # cổ trên
        (0.05, -0.20, 0.18),   # ĐOẠN PHÌNH ĐANG NUỐT TRỨNG!
        (0.11, -0.04, 0.095),  # vai
        (0.02, 0.16, 0.085),   # thân giữa
        (-0.13, 0.34, 0.075),  # thân sau
        (-0.04, 0.52, 0.055),  # đuôi cong
        (0.09, 0.64, 0.032),   # chóp đuôi
    ]
    rads = [
        (0.062, 0.050), (0.058, 0.052), (0.112, 0.105),
        (0.082, 0.074), (0.078, 0.070), (0.064, 0.058),
        (0.042, 0.038), (0.012, 0.012),
    ]
    w1._add_tube(bm, col, pts, rads, sides=20, subdiv=2, mat_index=0, color_fn=snake_scale)

    # Hàng gờ vảy sống lưng dọc thân
    for i in range(1, len(pts) - 1):
        px, py, pz = pts[i]
        rx, rz = rads[i]
        _add_sculpted_blob(bm, col, (px, py, pz + rz * 0.72), (0.018, 0.045, 0.024),
                           segs=10, rings=8, mat_index=2, color_fn=lambda _p, _n: (0.28, 0.22, 0.16))

    # Đầu rắn tam giác sắc nét + HÀM DƯỚI TÁCH KHỚP MỞ RỘNG (Nuốt Nhanh)
    _add_sculpted_blob(bm, col, (0.0, -0.48, 0.35), (0.078, 0.092, 0.046), rot=(0.08, 0.0, 0.0),
                       segs=20, rings=12,
                       disp_fn=lambda u: Vector((0.0, -0.02 * max(0.0, -u.y), 0.01 * max(0.0, u.z))),
                       mat_index=0, color_fn=snake_scale)
    # Hàm dưới mở nhẹ linh hoạt
    w1._add_tube(bm, col, [(0.0, -0.43, 0.315), (0.0, -0.52, 0.285), (0.0, -0.57, 0.280)],
                 [(0.055, 0.022), (0.042, 0.016), (0.022, 0.010)], sides=12, subdiv=1,
                 mat_index=0, color_fn=snake_scale)

    # Mắt rắn vàng đồng tử dọc + lưỡi chẻ đôi thè ra
    for sx in (-1.0, 1.0):
        w1._add_eye_bulb(bm, col, (sx * 0.052, -0.50, 0.368), 0.019, yaw=sx * 0.45,
                         eye_col=(0.98, 0.78, 0.14), mat_eye=3)
    for sx in (-1.0, 1.0):
        w1._add_tube(bm, col, [(0.0, -0.56, 0.315), (0.0, -0.63, 0.305), (sx * 0.024, -0.68, 0.295)],
                     [0.005, 0.004, 0.002], sides=6, subdiv=0, mat_index=3,
                     color_fn=lambda _p, _n: (0.94, 0.18, 0.24))

    bones = [
        ("Root", (0.04, 0.08, 0.09), (0.06, -0.10, 0.11), None, 0.30, 1.0),
        ("Neck", (0.06, -0.16, 0.15), (0.02, -0.34, 0.28), "Root", 0.22, 2.2),
        ("Head", (0.01, -0.36, 0.30), (0.0, -0.55, 0.33), "Neck", 0.18, 2.4),
        ("Body_Mid", (0.02, 0.14, 0.08), (-0.11, 0.34, 0.07), "Root", 0.24, 2.0),
        ("Tail", (-0.11, 0.34, 0.07), (0.08, 0.62, 0.03), "Body_Mid", 0.20, 2.2),
    ]
    clips = {
        "Idle": (1, 52, [
            (1, {"Neck": {"rot": (0.05, 0, 0.10)}, "Head": {"rot": (-0.06, 0, -0.06)}}),
            (26, {"Neck": {"rot": (-0.06, 0, -0.10)}, "Head": {"rot": (0.06, 0, 0.06)}}),
            (52, {"Neck": {"rot": (0.05, 0, 0.10)}, "Head": {"rot": (-0.06, 0, -0.06)}}),
        ]),
        "Walk": (1, 20, [
            (1, {"Neck": {"rot": (0, 0, 0.24)}, "Body_Mid": {"rot": (0, 0, -0.26)}, "Tail": {"rot": (0, 0, 0.26)}}),
            (10, {"Neck": {"rot": (0, 0, -0.24)}, "Body_Mid": {"rot": (0, 0, 0.26)}, "Tail": {"rot": (0, 0, -0.26)}}),
            (20, {"Neck": {"rot": (0, 0, 0.24)}, "Body_Mid": {"rot": (0, 0, -0.26)}, "Tail": {"rot": (0, 0, 0.26)}}),
        ]),
        "Attack": (1, 24, [
            (1, {"Neck": {"rot": (0, 0, 0)}, "Head": {"rot": (0, 0, 0)}}),
            (8, {"Neck": {"rot": (0.28, 0, 0)}, "Head": {"rot": (-0.20, 0, 0)}}),
            (14, {"Neck": {"rot": (-0.42, 0, 0)}, "Head": {"rot": (0.25, 0, 0)}}),
            (24, {"Neck": {"rot": (0, 0, 0)}, "Head": {"rot": (0, 0, 0)}}),
        ]),
        "Hit": (1, 18, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (6, {"Root": {"rot": (-0.22, 0.26, 0.20)}}),
            (18, {"Root": {"rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_RanTromTrung",
        os.path.join(out_dir, "Creature_RanTromTrung.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_19_ran_trom_trung.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_20_chim_hang_me(out_dir, prev_dir):
    """[C] Chim Hang Mẹ — Khả năng: Nhảy Cao (bổ nhào từ trên cao).
    Thân khí động học nở ức cơ bắp, mào lông nhọn, mỏ khoằm sắc,
    sải cánh lớn chia tầng lông sơ cấp/thứ cấp 3D, cặp đùi bật nhảy cơ bắp + 4 vuốt cong."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()

    def plumage(p, n):
        base = w1._lerp3((0.18, 0.16, 0.20), (0.44, 0.34, 0.28), 0.35 + 0.35 * math.sin(p.y * 10.0))
        if p.z < 0.45 and p.y < 0.0:
            base = w1._lerp3(base, (0.76, 0.68, 0.54), 0.50)
        return base

    # Thân khí động học nở ức cơ bắp + cổ vững
    _add_sculpted_blob(bm, col, (0.0, 0.02, 0.48), (0.205, 0.285, 0.190), rot=(-0.22, 0.0, 0.0),
                       segs=26, rings=18,
                       disp_fn=lambda u: Vector((0.0, -0.025 * max(0.0, -u.y), 0.015 * max(0.0, u.z))),
                       mat_index=0, color_fn=plumage)
    # Đầu + mào lông nhọn 3 chùm
    _add_sculpted_blob(bm, col, (0.0, -0.25, 0.67), (0.125, 0.145, 0.115),
                       segs=22, rings=14, mat_index=0, color_fn=plumage)
    for sx, len_m in [(-0.04, 0.16), (0.0, 0.21), (0.04, 0.16)]:
        _add_layered_feather(bm, col, (sx, -0.20, 0.74), (sx * 1.5, 0.85, 0.55), (0, 0, 1),
                             length=len_m, width=0.036, thickness=0.014, mat_index=0, color_fn=plumage)

    # Mỏ khoằm sắc bén + mắt sáng rực nhìn xuyên tối
    w1._add_tube(bm, col, [(0.0, -0.37, 0.665), (0.0, -0.47, 0.648), (0.0, -0.52, 0.595)],
                 [(0.048, 0.042), (0.028, 0.024), (0.004, 0.004)], sides=10, subdiv=1,
                 mat_index=2, color_fn=lambda _p, _n: (0.22, 0.18, 0.16))
    for sx in (-1.0, 1.0):
        w1._add_eye_bulb(bm, col, (sx * 0.095, -0.31, 0.69), 0.030, yaw=sx * 0.35,
                         eye_col=(0.98, 0.68, 0.18), mat_eye=3)

    # Sải cánh rộng chia tầng xương cánh + lông sơ cấp & thứ cấp 3D
    for sx in (-1.0, 1.0):
        w1._add_tube(bm, col, [
            (sx * 0.16, -0.05, 0.55),
            (sx * 0.42, -0.08, 0.66),
            (sx * 0.76, -0.02, 0.72),
            (sx * 0.96, 0.08, 0.68),
        ], [0.055, 0.044, 0.032, 0.018], sides=12, subdiv=1, mat_index=0, color_fn=plumage)

        # 8 lông cánh lớn (4 sơ cấp ngoài + 4 thứ cấp trong) tách rõ từng lông
        for f_i in range(8):
            ft = f_i / 7.0
            bx = sx * (0.20 + ft * 0.72)
            by = -0.05 + ft * 0.10
            bz = 0.56 + math.sin(ft * math.pi * 0.7) * 0.15
            dir_v = (sx * (0.18 + ft * 0.68), 0.94 - ft * 0.18, -0.08 + (f_i % 2) * 0.04)
            _add_layered_feather(bm, col, (bx, by, bz), dir_v, (0, 0, 1),
                                 length=0.40 + ft * 0.18, width=0.044, thickness=0.013,
                                 mat_index=0, color_fn=plumage)

    # Đuôi xòe quạt 5 lông dài khí động học
    for sx in (-0.5, -0.25, 0.0, 0.25, 0.5):
        _add_layered_feather(bm, col, (sx * 0.10, 0.25, 0.42), (sx * 0.45, 1.0, 0.08), (0, 0, 1),
                             length=0.40, width=0.058, thickness=0.014, mat_index=0, color_fn=plumage)

    # ĐẶC TRƯNG KHẢ NĂNG (Nhảy Cao): Cặp đùi to cơ bắp phủ lông + cẳng chân dài khớp bật + 4 vuốt cong sắc
    for sx in (-1.0, 1.0):
        # Đùi to khỏe
        _add_sculpted_blob(bm, col, (sx * 0.13, 0.02, 0.30), (0.078, 0.085, 0.115), rot=(0.25, sx * 0.15, 0.0),
                           segs=16, rings=10, mat_index=0, color_fn=plumage)
        # Cẳng chân khớp bật
        ankle = (sx * 0.15, 0.07, 0.16)
        foot = (sx * 0.15, -0.01, 0.025)
        _add_jointed_limb(bm, col, [(sx * 0.14, 0.02, 0.24), ankle, foot], [0.036, 0.024, 0.020],
                          mat_index=2, color_fn=lambda _p, _n: (0.78, 0.62, 0.28))
        # 3 vuốt trước + 1 vuốt sau cong sắc
        for dx in (-0.045, 0.0, 0.045):
            w1._add_tube(bm, col, [foot, (foot[0] + dx, -0.10, 0.022), (foot[0] + dx * 1.1, -0.14, 0.005)],
                         [0.012, 0.008, 0.002], sides=8, subdiv=0, mat_index=2,
                         color_fn=lambda _p, _n: (0.12, 0.10, 0.10))
        w1._add_tube(bm, col, [foot, (foot[0], 0.08, 0.020), (foot[0], 0.11, 0.005)],
                     [0.012, 0.007, 0.002], sides=8, subdiv=0, mat_index=2,
                     color_fn=lambda _p, _n: (0.12, 0.10, 0.10))

    bones = [
        ("Root", (0, 0.02, 0.48), (0, -0.12, 0.56), None, 0.42, 1.0),
        ("Head", (0, -0.15, 0.58), (0, -0.32, 0.68), "Root", 0.24, 2.2),
        ("Wing_L1", (-0.16, -0.05, 0.55), (-0.54, -0.05, 0.69), "Root", 0.28, 2.2),
        ("Wing_L2", (-0.54, -0.05, 0.69), (-0.96, 0.08, 0.68), "Wing_L1", 0.26, 2.4),
        ("Wing_R1", (0.16, -0.05, 0.55), (0.54, -0.05, 0.69), "Root", 0.28, 2.2),
        ("Wing_R2", (0.54, -0.05, 0.69), (0.96, 0.08, 0.68), "Wing_R1", 0.26, 2.4),
        ("Tail", (0, 0.25, 0.42), (0, 0.60, 0.45), "Root", 0.26, 2.0),
        ("Talons", (0, 0.0, 0.24), (0, -0.02, 0.02), "Root", 0.24, 2.2),
    ]
    clips = {
        "Idle": (1, 56, [
            (1, {"Head": {"rot": (0.04, 0, 0.10)}, "Wing_L1": {"rot": (0, 0.14, 0)}, "Wing_R1": {"rot": (0, -0.14, 0)}, "Tail": {"rot": (0.06, 0, 0)}}),
            (28, {"Head": {"rot": (-0.08, 0, -0.16)}, "Wing_L1": {"rot": (0, -0.16, 0)}, "Wing_R1": {"rot": (0, 0.16, 0)}, "Tail": {"rot": (-0.06, 0, 0)}}),
            (56, {"Head": {"rot": (0.04, 0, 0.10)}, "Wing_L1": {"rot": (0, 0.14, 0)}, "Wing_R1": {"rot": (0, -0.14, 0)}, "Tail": {"rot": (0.06, 0, 0)}}),
        ]),
        "Walk": (1, 24, [
            (1, {"Root": {"loc": (0, 0, 0.06)}, "Wing_L1": {"rot": (0, -0.58, 0)}, "Wing_R1": {"rot": (0, 0.58, 0)}, "Wing_L2": {"rot": (0, -0.36, 0)}, "Wing_R2": {"rot": (0, 0.36, 0)}}),
            (12, {"Root": {"loc": (0, 0, 0.26)}, "Wing_L1": {"rot": (0, 0.52, 0)}, "Wing_R1": {"rot": (0, -0.52, 0)}, "Wing_L2": {"rot": (0, 0.32, 0)}, "Wing_R2": {"rot": (0, -0.32, 0)}}),
            (24, {"Root": {"loc": (0, 0, 0.06)}, "Wing_L1": {"rot": (0, -0.58, 0)}, "Wing_R1": {"rot": (0, 0.58, 0)}, "Wing_L2": {"rot": (0, -0.36, 0)}, "Wing_R2": {"rot": (0, 0.36, 0)}}),
        ]),
        "Attack": (1, 26, [
            (1, {"Root": {"loc": (0, 0, 0), "rot": (0, 0, 0)}}),
            (9, {"Root": {"loc": (0, 0.16, 0.42), "rot": (-0.28, 0, 0)}, "Wing_L1": {"rot": (0, -0.50, 0)}, "Wing_R1": {"rot": (0, 0.50, 0)}}),
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

    print(f"Hoàn tất dựng 8 mô hình Wave 2 (Tầng Nông - Chi tiết cao) trong {time.time() - t0:.1f}s", flush=True)


if __name__ == "__main__":
    main()
