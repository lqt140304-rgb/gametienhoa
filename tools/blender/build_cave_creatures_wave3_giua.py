"""Dựng 12 Mô Hình 3D Đợt 3 (Tầng Giữa: Hang Nấm Huỳnh Quang, Suối Nước Nóng, Khe Gió Hú)
kèm Armature + NLA Animations + Preview Cycles.

Chạy headless:
  "D:/blender/blender.exe" -b -P tools/blender/build_cave_creatures_wave3_giua.py
"""
import argparse
import math
import os
import random
import sys
import time

import bmesh  # noqa: E402
import bpy  # noqa: E402
from mathutils import Euler, Vector  # noqa: E402

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_common as kc  # noqa: E402
import build_cave_creatures_wave1 as w1  # noqa: E402
import build_cave_creatures_wave2_nong as w2  # noqa: E402


def _sculpt_remesh_into(dst_bm, dst_col, src_bm, voxel_size=0.016, smooth_iter=6,
                        decimate_ratio=0.45, mat_index=0, color_fn=None):
    """Hòa trộn toàn bộ khối chồng lấn trong src_bm bằng Voxel Remesh + Smooth để tạo bề mặt điêu khắc hữu cơ liền mạch,
    sau đó tô Vertex Color theo vị trí/pháp tuyến và gộp vào dst_bm."""
    tmp_mesh = bpy.data.meshes.new("_tmp_sculpt")
    src_bm.to_mesh(tmp_mesh)
    src_bm.free()

    tmp_obj = bpy.data.objects.new("_tmp_sculpt_obj", tmp_mesh)
    bpy.context.collection.objects.link(tmp_obj)
    bpy.context.view_layer.objects.active = tmp_obj

    rem = tmp_obj.modifiers.new("Remesh", 'REMESH')
    rem.mode = 'VOXEL'
    rem.voxel_size = voxel_size
    rem.use_smooth_shade = True

    sm = tmp_obj.modifiers.new("Smooth", 'SMOOTH')
    sm.factor = 0.55
    sm.iterations = smooth_iter

    if 0.1 < decimate_ratio < 0.99:
        dec = tmp_obj.modifiers.new("Decimate", 'DECIMATE')
        dec.ratio = decimate_ratio

    deps = bpy.context.evaluated_depsgraph_get()
    eval_obj = tmp_obj.evaluated_get(deps)
    eval_mesh = bpy.data.meshes.new_from_object(eval_obj)

    res_bm = bmesh.new()
    res_bm.from_mesh(eval_mesh)
    res_bm.normal_update()

    vert_map = {}
    for v in res_bm.verts:
        nv = dst_bm.verts.new(v.co)
        vert_map[v.index] = nv
    for f in res_bm.faces:
        try:
            nf = dst_bm.faces.new([vert_map[v.index] for v in f.verts])
            nf.material_index = mat_index
            nf.smooth = True
            nrm = f.normal
            for loop in nf.loops:
                rgb = color_fn(loop.vert.co, nrm) if color_fn else (0.5, 0.5, 0.5)
                loop[dst_col] = (*rgb, 1.0)
        except ValueError:
            pass

    res_bm.free()
    bpy.data.meshes.remove(eval_mesh)
    bpy.data.objects.remove(tmp_obj, do_unlink=True)
    bpy.data.meshes.remove(tmp_mesh)


# ================================================================== HANG 6: HANG NẤM HUỲNH QUANG (4 LOÀI)

def build_21_nam_di_bo(out_dir, prev_dir):
    """[M] Nấm Đi Bộ — Khả năng: Kháng Độc.
    Mũ nấm huỳnh quang xanh-lam có nếp mang dưới mũ, cuống nấm mập hữu cơ, 4 rễ-chân nấm nhỏ + bào tử sáng."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()
    rng = random.Random(321)

    def cap_col(p, n):
        ring = 0.5 + 0.5 * math.sin(math.hypot(p.x, p.y) * 24.0)
        return w1._lerp3((0.18, 0.42, 0.68), (0.36, 0.92, 0.86), 0.35 + 0.55 * ring)

    def stalk_col(p, n):
        return w1._lerp3((0.76, 0.72, 0.62), (0.58, 0.64, 0.54), max(0.0, min(1.0, (0.35 - p.z) * 2.5)))

    # Cuống nấm mập hữu cơ hơi cong tự nhiên
    w1._add_tube(bm, col, [(0.0, 0.02, 0.14), (0.0, 0.0, 0.28), (0.0, -0.02, 0.42)],
                 [(0.115, 0.115), (0.085, 0.085), (0.072, 0.072)], sides=20, subdiv=2,
                 mat_index=0, color_fn=stalk_col)

    # Mũ nấm nhám hữu cơ (hết bóng nhựa) + tầng mang nấm xếp tỏa tròn dưới mũ + vòng cổ nấm + đốm bào tử phát sáng
    w2._add_sculpted_blob(
        bm, col, (0.0, -0.02, 0.47), (0.24, 0.24, 0.11),
        segs=28, rings=16,
        disp_fn=lambda u: Vector((0.0, 0.0, 0.018 * math.sin(math.atan2(u.y, u.x) * 7.0) * (1.0 - abs(u.z)) - 0.04 * max(0.0, -u.z))),
        mat_index=0, color_fn=cap_col,
    )
    # Tầng mang nấm (gills) xếp tỏa tròn dưới mũ
    for k in range(12):
        ang = k * (math.pi * 2.0 / 12.0)
        w2._add_layered_feather(bm, col, (math.cos(ang) * 0.06, -0.02 + math.sin(ang) * 0.06, 0.39),
                                (math.cos(ang), math.sin(ang), 0.12), (0, 0, 1),
                                length=0.16, width=0.024, thickness=0.014, mat_index=0, color_fn=stalk_col)
    # Vòng váy cổ nấm trên cuống
    w2._add_sculpted_blob(bm, col, (0.0, -0.01, 0.34), (0.115, 0.115, 0.028), segs=18, rings=8, mat_index=0, color_fn=stalk_col)
    # 5 đốm bào tử phát sáng trên đỉnh mũ
    for k in range(5):
        ang = k * (2.0 * math.pi / 5.0) + 0.3
        r = 0.12
        w2._add_sculpted_blob(bm, col, (math.cos(ang) * r, -0.02 + math.sin(ang) * r, 0.56),
                              (0.032, 0.032, 0.022), segs=12, rings=8, mat_index=3,
                              color_fn=lambda _p, _n: (0.65, 1.0, 0.92))

    # 2 mắt nhỏ thân thiện trên cuống nấm
    for sx in (-1.0, 1.0):
        w1._add_eye_bulb(bm, col, (sx * 0.048, -0.088, 0.30), 0.016, yaw=sx * 0.25,
                         eye_col=(0.10, 0.12, 0.18), mat_eye=1)

    # 4 rễ-chân nấm mập tròn bám đất
    for sx, sy in [(-1.0, -1.0), (1.0, -1.0), (-1.0, 1.0), (1.0, 1.0)]:
        hip = (sx * 0.06, sy * 0.05, 0.15)
        knee = (sx * 0.11, sy * 0.10, 0.09)
        foot = (sx * 0.13, sy * 0.12, 0.012)
        w2._add_jointed_limb(bm, col, [hip, knee, foot], [0.034, 0.026, 0.022],
                             mat_index=0, color_fn=stalk_col)

    bones = [
        ("Root", (0, 0, 0.14), (0, 0, 0.30), None, 0.24, 1.0),
        ("Cap", (0, -0.01, 0.32), (0, -0.02, 0.56), "Root", 0.26, 2.2),
        ("Leg_FL", (-0.06, -0.05, 0.15), (-0.13, -0.12, 0.01), "Root", 0.12, 2.5),
        ("Leg_FR", (0.06, -0.05, 0.15), (0.13, -0.12, 0.01), "Root", 0.12, 2.5),
        ("Leg_BL", (-0.06, 0.05, 0.15), (-0.13, 0.12, 0.01), "Root", 0.12, 2.5),
        ("Leg_BR", (0.06, 0.05, 0.15), (0.13, 0.12, 0.01), "Root", 0.12, 2.5),
    ]
    clips = {
        "Idle": (1, 48, [
            (1, {"Cap": {"rot": (0.05, 0.05, 0), "scale": (1.0, 1.0, 1.0)}}),
            (24, {"Cap": {"rot": (-0.06, -0.05, 0), "scale": (1.06, 1.06, 0.96)}}),
            (48, {"Cap": {"rot": (0.05, 0.05, 0), "scale": (1.0, 1.0, 1.0)}}),
        ]),
        "Walk": (1, 20, [
            (1, {"Root": {"loc": (0, 0, 0.02)}, "Leg_FL": {"rot": (0.45, 0, 0)}, "Leg_BR": {"rot": (0.45, 0, 0)}, "Leg_FR": {"rot": (-0.40, 0, 0)}, "Leg_BL": {"rot": (-0.40, 0, 0)}, "Cap": {"rot": (0.08, 0, 0)}}),
            (10, {"Root": {"loc": (0, 0, 0.06)}, "Leg_FL": {"rot": (-0.40, 0, 0)}, "Leg_BR": {"rot": (-0.40, 0, 0)}, "Leg_FR": {"rot": (0.45, 0, 0)}, "Leg_BL": {"rot": (0.45, 0, 0)}, "Cap": {"rot": (-0.08, 0, 0)}}),
            (20, {"Root": {"loc": (0, 0, 0.02)}, "Leg_FL": {"rot": (0.45, 0, 0)}, "Leg_BR": {"rot": (0.45, 0, 0)}, "Leg_FR": {"rot": (-0.40, 0, 0)}, "Leg_BL": {"rot": (-0.40, 0, 0)}, "Cap": {"rot": (0.08, 0, 0)}}),
        ]),
        "Hit": (1, 18, [
            (1, {"Root": {"scale": (1, 1, 1)}}),
            (6, {"Root": {"scale": (1.18, 1.18, 0.78)}}),
            (18, {"Root": {"scale": (1, 1, 1)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_NamDiBo",
        os.path.join(out_dir, "Creature_NamDiBo.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_21_nam_di_bo.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_22_buom_bao_tu(out_dir, prev_dir):
    """[M] Bướm Bào Tử — Khả năng: Mây Bào Tử.
    Thân bướm lông mịn, cặp râu lược cong, 4 cánh rộng có đốm mắt bào tử phát sáng xanh-tím rắc bụi sáng."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()

    def body_col(p, n):
        return w1._lerp3((0.20, 0.16, 0.30), (0.46, 0.38, 0.62), 0.4 + 0.4 * math.sin(p.y * 22.0))

    def wing_col(p, n):
        d = math.hypot(abs(p.x) - 0.30, p.y - 0.02)
        if d < 0.10:
            return (0.45, 0.98, 0.88)
        return w1._lerp3((0.26, 0.24, 0.54), (0.58, 0.42, 0.88), min(1.0, abs(p.x) * 1.6))

    # Thân bướm chia 3 phần đầu-ngực-bụng thon tròn mềm mại
    w2._add_sculpted_blob(bm, col, (0.0, -0.20, 0.44), (0.052, 0.055, 0.050), segs=16, rings=10, mat_index=0, color_fn=body_col)
    w2._add_sculpted_blob(bm, col, (0.0, -0.06, 0.46), (0.072, 0.105, 0.070), segs=18, rings=12, mat_index=0, color_fn=body_col)
    w1._add_tube(bm, col, [(0.0, 0.05, 0.45), (0.0, 0.18, 0.43), (0.0, 0.30, 0.39)],
                 [(0.058, 0.054), (0.048, 0.045), (0.018, 0.016)], sides=14, subdiv=1, mat_index=0, color_fn=body_col)

    # Mắt kép + cặp râu lược phát sáng
    for sx in (-1.0, 1.0):
        w1._add_eye_bulb(bm, col, (sx * 0.042, -0.23, 0.455), 0.022, yaw=sx * 0.3,
                         eye_col=(0.45, 0.95, 0.92), mat_eye=3)
        w1._add_tube(bm, col, [(sx * 0.02, -0.24, 0.475), (sx * 0.08, -0.34, 0.545), (sx * 0.14, -0.40, 0.575)],
                     [0.010, 0.008, 0.004], sides=6, subdiv=1, mat_index=3, color_fn=lambda _p, _n: (0.6, 0.96, 0.85))

    # 4 cánh rộng (2 cánh trước lớn + 2 cánh sau) có độ cong vòm & đốm mắt bào tử sáng
    for sx in (-1.0, 1.0):
        # Cánh trước rộng
        w2._add_layered_feather(bm, col, (sx * 0.05, -0.06, 0.48), (sx * 0.92, -0.15, 0.22), (0, 0, 1),
                               length=0.46, width=0.16, thickness=0.012, mat_index=3, color_fn=wing_col)
        # Cánh sau nhỏ hơn xòe xuống
        w2._add_layered_feather(bm, col, (sx * 0.05, 0.02, 0.46), (sx * 0.82, 0.52, 0.10), (0, 0, 1),
                               length=0.34, width=0.125, thickness=0.010, mat_index=3, color_fn=wing_col)

    bones = [
        ("Root", (0, 0.0, 0.45), (0, -0.15, 0.45), None, 0.28, 1.0),
        ("Wing_L", (-0.05, -0.04, 0.48), (-0.46, -0.08, 0.56), "Root", 0.24, 2.4),
        ("Wing_R", (0.05, -0.04, 0.48), (0.46, -0.08, 0.56), "Root", 0.24, 2.4),
        ("Abdomen", (0, 0.05, 0.45), (0, 0.30, 0.39), "Root", 0.16, 2.2),
    ]
    clips = {
        "Idle": (1, 36, [
            (1, {"Root": {"loc": (0, 0, 0.0)}, "Wing_L": {"rot": (0, -0.35, 0)}, "Wing_R": {"rot": (0, 0.35, 0)}}),
            (18, {"Root": {"loc": (0, 0, 0.06)}, "Wing_L": {"rot": (0, 0.42, 0)}, "Wing_R": {"rot": (0, -0.42, 0)}}),
            (36, {"Root": {"loc": (0, 0, 0.0)}, "Wing_L": {"rot": (0, -0.35, 0)}, "Wing_R": {"rot": (0, 0.35, 0)}}),
        ]),
        "Walk": (1, 18, [
            (1, {"Root": {"loc": (0, 0, 0.06)}, "Wing_L": {"rot": (0, -0.60, 0)}, "Wing_R": {"rot": (0, 0.60, 0)}}),
            (9, {"Root": {"loc": (0, 0, 0.0)}, "Wing_L": {"rot": (0, 0.56, 0)}, "Wing_R": {"rot": (0, -0.56, 0)}}),
            (18, {"Root": {"loc": (0, 0, 0.06)}, "Wing_L": {"rot": (0, -0.60, 0)}, "Wing_R": {"rot": (0, 0.60, 0)}}),
        ]),
        "Action": (1, 24, [
            (1, {"Wing_L": {"rot": (0, 0, 0)}, "Wing_R": {"rot": (0, 0, 0)}, "Abdomen": {"rot": (0, 0, 0)}}),
            (8, {"Wing_L": {"rot": (0, -0.70, 0)}, "Wing_R": {"rot": (0, 0.70, 0)}, "Abdomen": {"rot": (-0.25, 0, 0)}}),
            (16, {"Wing_L": {"rot": (0, 0.65, 0)}, "Wing_R": {"rot": (0, -0.65, 0)}, "Abdomen": {"rot": (0.20, 0, 0)}}),
            (24, {"Wing_L": {"rot": (0, 0, 0)}, "Wing_R": {"rot": (0, 0, 0)}, "Abdomen": {"rot": (0, 0, 0)}}),
        ]),
        "Hit": (1, 16, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (6, {"Root": {"rot": (-0.28, 0.25, 0.20)}}),
            (16, {"Root": {"rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_BuomBaoTu",
        os.path.join(out_dir, "Creature_BuomBaoTu.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_22_buom_bao_tu.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_23_ech_doc_dom(out_dir, prev_dir):
    """[S] Ếch Độc Đốm — Khả năng: Phun Độc.
    Thân ếch cơ bắp đùi bật nhảy mạnh, da đốm độc phát sáng cam-tím, túi thanh quản & tuyến độc mang tai phồng căng."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()
    body_bm, body_col, _ = w1._new_bm()

    def frog_skin(p, n):
        spot = math.sin(p.x * 26.0) * math.cos(p.y * 24.0) * math.sin(p.z * 22.0)
        if spot > 0.42:
            return (0.94, 0.36, 0.88)
        base = w1._lerp3((0.12, 0.16, 0.34), (0.22, 0.48, 0.42), 0.45 + 0.4 * max(0.0, n.z))
        if n.z < -0.25:
            base = w1._lerp3(base, (0.72, 0.82, 0.65), 0.6)
        return base

    # Thân ếch ngồi rình săn mồi + đầu rộng + 4 chân + đùi sau bật nhảy điêu khắc liền mạch
    w2._add_sculpted_blob(body_bm, body_col, (0.0, 0.04, 0.20), (0.185, 0.235, 0.145), rot=(-0.42, 0.0, 0.0),
                          segs=24, rings=16, mat_index=0, color_fn=frog_skin)
    w2._add_sculpted_blob(body_bm, body_col, (0.0, -0.18, 0.30), (0.165, 0.150, 0.095), rot=(-0.12, 0.0, 0.0),
                          segs=22, rings=14, mat_index=0, color_fn=frog_skin)
    for sx in (-1.0, 1.0):
        w2._add_sculpted_blob(body_bm, body_col, (sx * 0.095, -0.21, 0.35), (0.042, 0.045, 0.032), segs=12, rings=8, mat_index=0, color_fn=frog_skin)
        w1._add_tube(body_bm, body_col, [(sx * 0.14, -0.11, 0.19), (sx * 0.18, -0.16, 0.10), (sx * 0.15, -0.20, 0.018)],
                     [0.038, 0.028, 0.022], sides=10, subdiv=1, mat_index=0, color_fn=frog_skin)
        w2._add_sculpted_blob(body_bm, body_col, (sx * 0.17, 0.14, 0.16), (0.080, 0.118, 0.074), rot=(0.35, 0, sx * 0.25),
                              segs=16, rings=10, mat_index=0, color_fn=frog_skin)
        w1._add_tube(body_bm, body_col, [(sx * 0.21, 0.20, 0.15), (sx * 0.24, 0.04, 0.06), (sx * 0.22, 0.0, 0.018)],
                     [0.042, 0.028, 0.022], sides=10, subdiv=1, mat_index=0, color_fn=frog_skin)

    _sculpt_remesh_into(bm, col, body_bm, voxel_size=0.014, smooth_iter=6, decimate_ratio=0.45, mat_index=0, color_fn=frog_skin)

    # ĐẶC TRƯNG KHẢ NĂNG (Phun Độc): Túi thanh quản phồng dưới cằm + 2 tuyến độc mang tai phát sáng + mắt cam
    w2._add_sculpted_blob(bm, col, (0.0, -0.22, 0.21), (0.125, 0.115, 0.085),
                          segs=18, rings=12, mat_index=3, color_fn=lambda _p, _n: (0.72, 0.28, 0.94))
    for sx in (-1.0, 1.0):
        w2._add_sculpted_blob(bm, col, (sx * 0.135, -0.10, 0.34), (0.058, 0.072, 0.048),
                              segs=14, rings=10, mat_index=3, color_fn=lambda _p, _n: (0.86, 0.26, 0.96))
        w1._add_eye_bulb(bm, col, (sx * 0.095, -0.24, 0.375), 0.036, yaw=sx * 0.35,
                         eye_col=(0.98, 0.62, 0.12), mat_eye=3)

    bones = [
        ("Root", (0, 0.02, 0.18), (0, -0.12, 0.26), None, 0.28, 1.0),
        ("Head", (0, -0.12, 0.26), (0, -0.26, 0.32), "Root", 0.22, 2.2),
        ("Sac", (0, -0.18, 0.22), (0, -0.26, 0.18), "Head", 0.16, 2.5),
        ("Leg_BL", (-0.17, 0.14, 0.16), (-0.22, 0.0, 0.02), "Root", 0.18, 2.4),
        ("Leg_BR", (0.17, 0.14, 0.16), (0.22, 0.0, 0.02), "Root", 0.18, 2.4),
    ]
    clips = {
        "Idle": (1, 48, [
            (1, {"Sac": {"scale": (1.0, 1.0, 1.0)}, "Head": {"rot": (0.04, 0, 0)}}),
            (24, {"Sac": {"scale": (1.32, 1.24, 1.24)}, "Head": {"rot": (-0.06, 0, 0)}}),
            (48, {"Sac": {"scale": (1.0, 1.0, 1.0)}, "Head": {"rot": (0.04, 0, 0)}}),
        ]),
        "Walk": (1, 22, [
            (1, {"Root": {"loc": (0, 0, 0.0)}, "Leg_BL": {"rot": (0.45, 0, 0)}, "Leg_BR": {"rot": (0.45, 0, 0)}}),
            (10, {"Root": {"loc": (0, -0.06, 0.22), "rot": (-0.24, 0, 0)}, "Leg_BL": {"rot": (-0.55, 0, 0)}, "Leg_BR": {"rot": (-0.55, 0, 0)}}),
            (22, {"Root": {"loc": (0, 0, 0.0)}, "Leg_BL": {"rot": (0.45, 0, 0)}, "Leg_BR": {"rot": (0.45, 0, 0)}}),
        ]),
        "Attack": (1, 24, [
            (1, {"Sac": {"scale": (1, 1, 1)}, "Head": {"rot": (0, 0, 0)}}),
            (9, {"Sac": {"scale": (1.45, 1.35, 1.35)}, "Head": {"rot": (-0.22, 0, 0)}}),
            (15, {"Sac": {"scale": (0.78, 0.78, 0.78)}, "Head": {"rot": (0.28, 0, 0)}}),
            (24, {"Sac": {"scale": (1, 1, 1)}, "Head": {"rot": (0, 0, 0)}}),
        ]),
        "Hit": (1, 18, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (6, {"Root": {"rot": (-0.25, 0.22, 0.18)}}),
            (18, {"Root": {"rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_EchDocDom",
        os.path.join(out_dir, "Creature_EchDocDom.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_23_ech_doc_dom.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_24_nam_khong_lo(out_dir, prev_dir):
    """[C] Nấm Khổng Lồ — Khả năng: Nổ Bào Tử.
    Chúa hang to lớn bệ vệ: mũ nấm khổng lồ nhiều tầng rủ màn bào tử sáng, bọc bào tử chín phồng sắp nổ,
    6 rễ-chân to khỏe + nấm con mọc quanh chân."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()

    def giant_cap(p, n):
        ring = 0.5 + 0.5 * math.sin(math.hypot(p.x, p.y) * 14.0)
        return w1._lerp3((0.24, 0.14, 0.56), (0.36, 0.94, 0.88), 0.3 + 0.55 * ring)

    def bark_stalk(p, n):
        return w1._lerp3((0.44, 0.40, 0.36), (0.68, 0.64, 0.56), max(0.0, min(1.0, p.z * 0.9)))

    # Thân trụ nấm khổng lồ có rãnh gân & vòng cổ nấm
    w1._add_tube(bm, col, [(0.0, 0.0, 0.24), (0.0, 0.0, 0.56), (0.0, 0.0, 0.88)],
                 [(0.28, 0.28), (0.21, 0.21), (0.18, 0.18)], sides=24, subdiv=2, mat_index=0, color_fn=bark_stalk)
    w2._add_sculpted_blob(bm, col, (0.0, 0.0, 0.68), (0.28, 0.28, 0.06), segs=24, rings=10, mat_index=0, color_fn=bark_stalk)

    # Mũ nấm khổng lồ nhám hữu cơ 2 tầng + tầng mang nấm xếp tỏa + các bọc bào tử chín phát sáng sắp nổ
    w2._add_sculpted_blob(
        bm, col, (0.0, 0.0, 0.96), (0.64, 0.64, 0.24),
        segs=32, rings=18,
        disp_fn=lambda u: Vector((0.0, 0.0, 0.032 * math.sin(math.atan2(u.y, u.x) * 8.0) * (1.0 - abs(u.z)) - 0.06 * max(0.0, -u.z))),
        mat_index=0, color_fn=giant_cap,
    )
    w2._add_sculpted_blob(bm, col, (0.0, 0.0, 1.18), (0.36, 0.36, 0.14), segs=24, rings=12, mat_index=0, color_fn=giant_cap)
    for k in range(16):
        ang = k * (math.pi * 2.0 / 16.0)
        w2._add_layered_feather(bm, col, (math.cos(ang) * 0.18, math.sin(ang) * 0.18, 0.80),
                                (math.cos(ang), math.sin(ang), 0.10), (0, 0, 1),
                                length=0.42, width=0.055, thickness=0.024, mat_index=0, color_fn=bark_stalk)

    # 6 bọc bào tử phồng căng quanh vành mũ + 3 mắt phát sáng trên thân
    for k in range(6):
        ang = k * (math.pi / 3.0)
        ca, sa = math.cos(ang), math.sin(ang)
        w2._add_sculpted_blob(bm, col, (ca * 0.46, sa * 0.46, 0.98), (0.095, 0.095, 0.075),
                              segs=14, rings=10, mat_index=3, color_fn=lambda _p, _n: (0.68, 1.0, 0.88))
    for mx, mz in [(-0.10, 0.50), (0.10, 0.50), (0.0, 0.58)]:
        w1._add_eye_bulb(bm, col, (mx, -0.20, mz), 0.032, yaw=mx * 2.0, eye_col=(0.45, 0.98, 0.92), mat_eye=3)

    # 6 rễ-chân trụ to khỏe + nấm con mọc cạnh chân
    for k in range(6):
        ang = k * (math.pi / 3.0) + 0.3
        ca, sa = math.cos(ang), math.sin(ang)
        w2._add_jointed_limb(bm, col, [
            (ca * 0.22, sa * 0.22, 0.26),
            (ca * 0.42, sa * 0.42, 0.18),
            (ca * 0.52, sa * 0.52, 0.02),
        ], [0.09, 0.068, 0.050], mat_index=0, color_fn=bark_stalk)
        if k % 2 == 0:
            w2._add_sculpted_blob(bm, col, (ca * 0.44, sa * 0.44, 0.10), (0.075, 0.075, 0.045),
                                  segs=14, rings=8, mat_index=3, color_fn=giant_cap)

    bones = [
        ("Root", (0, 0, 0.24), (0, 0, 0.58), None, 0.52, 1.0),
        ("Cap", (0, 0, 0.62), (0, 0, 1.15), "Root", 0.56, 2.2),
        ("Leg_L", (-0.22, 0, 0.24), (-0.50, 0, 0.02), "Root", 0.24, 2.4),
        ("Leg_R", (0.22, 0, 0.24), (0.50, 0, 0.02), "Root", 0.24, 2.4),
    ]
    clips = {
        "Idle": (1, 60, [
            (1, {"Cap": {"scale": (1.0, 1.0, 1.0)}}),
            (30, {"Cap": {"scale": (1.08, 1.08, 0.94)}}),
            (60, {"Cap": {"scale": (1.0, 1.0, 1.0)}}),
        ]),
        "Walk": (1, 32, [
            (1, {"Root": {"rot": (0.04, 0.06, 0)}, "Leg_L": {"rot": (0.30, 0, 0)}, "Leg_R": {"rot": (-0.28, 0, 0)}}),
            (16, {"Root": {"rot": (-0.04, -0.06, 0)}, "Leg_L": {"rot": (-0.28, 0, 0)}, "Leg_R": {"rot": (0.30, 0, 0)}}),
            (32, {"Root": {"rot": (0.04, 0.06, 0)}, "Leg_L": {"rot": (0.30, 0, 0)}, "Leg_R": {"rot": (-0.28, 0, 0)}}),
        ]),
        "Action": (1, 32, [
            (1, {"Cap": {"scale": (1, 1, 1)}, "Root": {"loc": (0, 0, 0)}}),
            (12, {"Cap": {"scale": (0.86, 0.86, 1.18)}, "Root": {"loc": (0, 0, -0.08)}}),
            (20, {"Cap": {"scale": (1.28, 1.28, 0.85)}, "Root": {"loc": (0, 0, 0.06)}}),
            (32, {"Cap": {"scale": (1, 1, 1)}, "Root": {"loc": (0, 0, 0)}}),
        ]),
        "Hit": (1, 20, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (7, {"Root": {"rot": (-0.18, 0.16, 0.14)}}),
            (20, {"Root": {"rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_NamKhongLo",
        os.path.join(out_dir, "Creature_NamKhongLo.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_24_nam_khong_lo.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


# ================================================================== HANG 7: SUỐI NƯỚC NÓNG (4 LOÀI)

def build_25_oc_luu_huynh(out_dir, prev_dir):
    """[M] Ốc Lưu Huỳnh — Khả năng: Hơi Nước.
    Vỏ ốc xoắn dày có tinh thể lưu huỳnh vàng, ống xi-phông phụt hơi nước phát sáng trên vỏ."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()

    def shell_col(p, n):
        return w1._lerp3((0.32, 0.24, 0.16), (0.94, 0.78, 0.18), 0.35 + 0.5 * math.sin(p.x * 18.0 + p.z * 14.0))

    # Thân ốc mềm bò dưới vỏ + 2 râu cảm nhiệt
    w1._add_tube(bm, col, [(0.0, -0.26, 0.05), (0.0, -0.04, 0.06), (0.0, 0.20, 0.04)],
                 [(0.075, 0.045), (0.115, 0.055), (0.065, 0.035)], sides=16, subdiv=1,
                 mat_index=0, color_fn=lambda _p, _n: (0.76, 0.56, 0.36))
    for sx in (-1.0, 1.0):
        w1._add_tube(bm, col, [(sx * 0.04, -0.24, 0.07), (sx * 0.08, -0.34, 0.14)], [0.014, 0.008],
                     sides=8, subdiv=0, mat_index=0, color_fn=lambda _p, _n: (0.85, 0.62, 0.35))
        w1._add_eye_bulb(bm, col, (sx * 0.045, -0.25, 0.085), 0.015, yaw=sx * 0.2, eye_col=(0.15, 0.10, 0.06), mat_eye=1)

    # Vỏ ốc xoắn dày chịu nhiệt + ống xi-phông phụt hơi nước (Hơi Nước)
    for k, (cx, cy, cz, rad) in enumerate([(0.0, 0.02, 0.19, 0.155), (0.09, 0.06, 0.22, 0.115), (0.15, 0.08, 0.24, 0.078)]):
        w2._add_sculpted_blob(bm, col, (cx, cy, cz), (rad, rad * 1.1, rad), segs=20, rings=14, mat_index=2, color_fn=shell_col)
    # Ống phụt hơi nước trên đỉnh vỏ
    w1._add_tube(bm, col, [(-0.04, 0.02, 0.31), (-0.06, 0.06, 0.42)], [(0.042, 0.042), (0.054, 0.054)],
                 sides=14, subdiv=1, mat_index=3, color_fn=lambda _p, _n: (0.98, 0.88, 0.56))

    bones = [
        ("Root", (0, 0, 0.06), (0, -0.14, 0.06), None, 0.25, 1.0),
        ("Shell", (0, 0.02, 0.16), (0, 0.04, 0.36), "Root", 0.24, 2.2),
        ("Head", (0, -0.16, 0.06), (0, -0.32, 0.12), "Root", 0.14, 2.4),
    ]
    clips = {
        "Idle": (1, 48, [
            (1, {"Shell": {"rot": (0.04, 0, 0)}, "Head": {"rot": (0.06, 0, 0)}}),
            (24, {"Shell": {"rot": (-0.05, 0.04, 0)}, "Head": {"rot": (-0.08, 0, 0)}}),
            (48, {"Shell": {"rot": (0.04, 0, 0)}, "Head": {"rot": (0.06, 0, 0)}}),
        ]),
        "Walk": (1, 30, [
            (1, {"Head": {"loc": (0, -0.02, 0)}, "Shell": {"rot": (0.05, 0, 0.05)}}),
            (15, {"Head": {"loc": (0, 0.02, 0.01)}, "Shell": {"rot": (-0.05, 0, -0.05)}}),
            (30, {"Head": {"loc": (0, -0.02, 0)}, "Shell": {"rot": (0.05, 0, 0.05)}}),
        ]),
        "Action": (1, 24, [
            (1, {"Shell": {"scale": (1, 1, 1)}}),
            (10, {"Shell": {"scale": (1.14, 1.14, 0.90)}}),
            (16, {"Shell": {"scale": (0.92, 0.92, 1.16)}}),
            (24, {"Shell": {"scale": (1, 1, 1)}}),
        ]),
        "Hit": (1, 18, [
            (1, {"Root": {"scale": (1, 1, 1)}}),
            (6, {"Root": {"scale": (1.12, 0.85, 0.85)}}),
            (18, {"Root": {"scale": (1, 1, 1)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_OcLuuHuynh",
        os.path.join(out_dir, "Creature_OcLuuHuynh.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_25_oc_luu_huynh.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_26_cua_da_nong(out_dir, prev_dir):
    """[M] Cua Đá Nóng — Khả năng: Chịu Nhiệt.
    Mai cua đá núi lửa có khe nóng đỏ rực, 2 càng to bất đối xứng, 8 chân đi ngang phân đốt."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()

    def magma_shell(p, n):
        crack = math.sin(p.x * 24.0 + p.y * 18.0)
        if crack > 0.65:
            return (0.98, 0.42, 0.10)
        return w1._lerp3((0.24, 0.14, 0.12), (0.58, 0.22, 0.12), 0.4 + 0.4 * max(0.0, n.z))

    # Mai cua rộng tròn khối đá núi lửa + khe nóng đỏ rực
    w2._add_sculpted_blob(
        bm, col, (0.0, 0.0, 0.18), (0.24, 0.19, 0.11),
        segs=24, rings=14,
        disp_fn=lambda u: Vector((0.0, 0.0, 0.016 * math.sin(u.x * 8.0) * math.cos(u.y * 8.0))),
        mat_index=2, color_fn=magma_shell,
    )
    for sx in (-1.0, 1.0):
        w1._add_eye_bulb(bm, col, (sx * 0.075, -0.175, 0.245), 0.022, yaw=sx * 0.2, eye_col=(0.98, 0.56, 0.12), mat_eye=3)

    # 2 càng to (càng phải to hơn càng trái)
    for sx, scale in [(-1.0, 0.85), (1.0, 1.15)]:
        arm = [(sx * 0.18, -0.10, 0.16), (sx * 0.30, -0.22, 0.18), (sx * 0.24, -0.34, 0.20)]
        w2._add_jointed_limb(bm, col, arm, [0.042 * scale, 0.046 * scale, 0.054 * scale],
                             mat_index=2, color_fn=magma_shell)
        # 2 gọng kìm
        w1._add_tube(bm, col, [(sx * 0.24, -0.34, 0.21), (sx * 0.16, -0.44, 0.22)],
                     [0.036 * scale, 0.008], sides=8, subdiv=0, mat_index=2, color_fn=magma_shell)
        w1._add_tube(bm, col, [(sx * 0.24, -0.34, 0.17), (sx * 0.17, -0.43, 0.17)],
                     [0.028 * scale, 0.007], sides=8, subdiv=0, mat_index=2, color_fn=magma_shell)

    # 8 chân cua đi ngang (4 bên trái, 4 bên phải)
    for i, ly in enumerate([-0.06, 0.01, 0.08, 0.14]):
        for sx in (-1.0, 1.0):
            w2._add_jointed_limb(bm, col, [
                (sx * 0.20, ly, 0.15),
                (sx * 0.34, ly + 0.03, 0.19),
                (sx * 0.40, ly + 0.05, 0.02),
            ], [0.024, 0.018, 0.010], claw_dir=(sx * 0.3, 0.1, -0.6), claw_len=0.025,
                mat_index=2, color_fn=magma_shell)

    bones = [
        ("Root", (0, 0, 0.18), (0, -0.10, 0.18), None, 0.28, 1.0),
        ("Claw_L", (-0.18, -0.10, 0.16), (-0.24, -0.36, 0.20), "Root", 0.20, 2.4),
        ("Claw_R", (0.18, -0.10, 0.16), (0.24, -0.36, 0.20), "Root", 0.20, 2.4),
        ("Legs_L", (-0.20, 0.04, 0.15), (-0.38, 0.04, 0.03), "Root", 0.22, 2.2),
        ("Legs_R", (0.20, 0.04, 0.15), (0.38, 0.04, 0.03), "Root", 0.22, 2.2),
    ]
    clips = {
        "Idle": (1, 44, [
            (1, {"Claw_L": {"rot": (0.08, 0, 0)}, "Claw_R": {"rot": (-0.06, 0, 0)}}),
            (22, {"Claw_L": {"rot": (-0.08, 0, 0)}, "Claw_R": {"rot": (0.10, 0, 0)}}),
            (44, {"Claw_L": {"rot": (0.08, 0, 0)}, "Claw_R": {"rot": (-0.06, 0, 0)}}),
        ]),
        "Walk": (1, 18, [
            (1, {"Root": {"loc": (-0.04, 0, 0.02)}, "Legs_L": {"rot": (0, 0.25, 0)}, "Legs_R": {"rot": (0, -0.25, 0)}}),
            (9, {"Root": {"loc": (0.04, 0, 0.04)}, "Legs_L": {"rot": (0, -0.25, 0)}, "Legs_R": {"rot": (0, 0.25, 0)}}),
            (18, {"Root": {"loc": (-0.04, 0, 0.02)}, "Legs_L": {"rot": (0, 0.25, 0)}, "Legs_R": {"rot": (0, -0.25, 0)}}),
        ]),
        "Hit": (1, 16, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (6, {"Root": {"rot": (-0.22, 0.20, 0.18)}}),
            (16, {"Root": {"rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_CuaDaNong",
        os.path.join(out_dir, "Creature_CuaDaNong.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_26_cua_da_nong.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_27_ky_nhong_lua(out_dir, prev_dir):
    """[S] Kỳ Nhông Lửa — Khả năng: Thân Nóng.
    Kỳ nhông da đỏ rực, mào gai lửa phát sáng chạy dọc sống lưng tới đuôi, 4 chân bám đá chắc."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()
    body_bm, body_col, _ = w1._new_bm()

    def fire_skin(p, n):
        base = w1._lerp3((0.72, 0.14, 0.06), (0.98, 0.45, 0.10), 0.4 + 0.4 * math.sin(p.y * 16.0))
        if n.z < -0.25:
            base = w1._lerp3(base, (0.98, 0.78, 0.28), 0.65)
        return base

    # Thân kỳ nhông + đầu bò sát có hàm-má + vai/đùi + 4 chân điêu khắc liền mạch bằng Voxel Remesh
    pts = [(0.0, -0.44, 0.25), (0.02, -0.24, 0.20), (0.04, -0.02, 0.18),
           (-0.02, 0.22, 0.16), (-0.08, 0.48, 0.11), (0.02, 0.74, 0.05)]
    rads = [(0.080, 0.064), (0.095, 0.082), (0.118, 0.098),
            (0.094, 0.080), (0.055, 0.048), (0.014, 0.014)]
    w1._add_tube(body_bm, body_col, pts, rads, sides=20, subdiv=2, mat_index=0, color_fn=fire_skin)
    w2._add_sculpted_blob(body_bm, body_col, (0.0, -0.48, 0.25), (0.085, 0.105, 0.060),
                          disp_fn=lambda u: Vector((0.0, -0.03 * max(0.0, -u.y), 0.0)), segs=18, rings=12, mat_index=0, color_fn=fire_skin)
    for sx, sy, hip_y, foot_y in [(-1, -1, -0.14, -0.22), (1, -1, -0.14, -0.22), (-1, 1, 0.20, 0.16), (1, 1, 0.20, 0.16)]:
        w2._add_sculpted_blob(body_bm, body_col, (sx * 0.09, hip_y, 0.17), (0.048, 0.062, 0.048), segs=12, rings=8, mat_index=0, color_fn=fire_skin)
        w1._add_tube(body_bm, body_col, [(sx * 0.09, hip_y, 0.16), (sx * 0.20, hip_y - 0.02, 0.19), (sx * 0.24, foot_y, 0.018)],
                     [0.036, 0.028, 0.022], sides=10, subdiv=1, mat_index=0, color_fn=fire_skin)

    _sculpt_remesh_into(bm, col, body_bm, voxel_size=0.015, smooth_iter=6, decimate_ratio=0.45, mat_index=0, color_fn=fire_skin)

    # Đầu mắt vàng rực + mào gai lửa phát sáng dọc sống lưng + móng vuốt sắc nét
    for sx in (-1.0, 1.0):
        w1._add_eye_bulb(bm, col, (sx * 0.062, -0.46, 0.285), 0.022, yaw=sx * 0.35, eye_col=(1.0, 0.85, 0.20), mat_eye=3)
    for i in range(8):
        t = i / 7.0
        y = -0.32 + t * 0.82
        x = 0.04 * math.sin(t * math.pi * 1.4)
        z = 0.26 - t * 0.14
        h = 0.095 * math.sin(t * math.pi * 0.85 + 0.2)
        w1._add_tube(bm, col, [(x, y, z), (x, y + 0.03, z + h)], [0.024, 0.004], sides=8, subdiv=0,
                     mat_index=3, color_fn=lambda _p, _n: (1.0, 0.48, 0.08))

    bones = [
        ("Root", (0, 0.0, 0.18), (0, -0.14, 0.19), None, 0.30, 1.0),
        ("Head", (0.02, -0.24, 0.20), (0, -0.46, 0.24), "Root", 0.20, 2.2),
        ("Tail", (-0.02, 0.22, 0.16), (0.02, 0.72, 0.05), "Root", 0.24, 2.0),
        ("Leg_FL", (-0.09, -0.14, 0.16), (-0.24, -0.22, 0.02), "Root", 0.16, 2.4),
        ("Leg_FR", (0.09, -0.14, 0.16), (0.24, -0.22, 0.02), "Root", 0.16, 2.4),
    ]
    clips = {
        "Idle": (1, 48, [
            (1, {"Head": {"rot": (0.04, 0, 0.08)}, "Tail": {"rot": (0, 0, -0.10)}}),
            (24, {"Head": {"rot": (-0.05, 0, -0.08)}, "Tail": {"rot": (0, 0, 0.10)}}),
            (48, {"Head": {"rot": (0.04, 0, 0.08)}, "Tail": {"rot": (0, 0, -0.10)}}),
        ]),
        "Walk": (1, 20, [
            (1, {"Head": {"rot": (0, 0, 0.16)}, "Tail": {"rot": (0, 0, -0.22)}, "Leg_FL": {"rot": (0.45, 0, 0)}, "Leg_FR": {"rot": (-0.40, 0, 0)}}),
            (10, {"Head": {"rot": (0, 0, -0.16)}, "Tail": {"rot": (0, 0, 0.22)}, "Leg_FL": {"rot": (-0.40, 0, 0)}, "Leg_FR": {"rot": (0.45, 0, 0)}}),
            (20, {"Head": {"rot": (0, 0, 0.16)}, "Tail": {"rot": (0, 0, -0.22)}, "Leg_FL": {"rot": (0.45, 0, 0)}, "Leg_FR": {"rot": (-0.40, 0, 0)}}),
        ]),
        "Attack": (1, 22, [
            (1, {"Root": {"loc": (0, 0, 0)}}),
            (8, {"Root": {"loc": (0, 0.10, 0.04)}, "Head": {"rot": (-0.22, 0, 0)}}),
            (14, {"Root": {"loc": (0, -0.24, 0.02)}, "Head": {"rot": (0.18, 0, 0)}}),
            (22, {"Root": {"loc": (0, 0, 0)}}),
        ]),
        "Hit": (1, 18, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (6, {"Root": {"rot": (-0.22, 0.24, 0.20)}}),
            (18, {"Root": {"rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_KyNhongLua",
        os.path.join(out_dir, "Creature_KyNhongLua.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_27_ky_nhong_lua.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_28_ran_dung_nham(out_dir, prev_dir):
    """[C] Rắn Dung Nham — Khả năng: Phun Lửa.
    Chúa hang khổng lồ: thân uốn 3D nâng đầu uy nghi, lớp giáp đen than nứt lộ dung nham sáng rực, vương miện sừng lửa."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()

    def obsidian_col(p, n):
        return w1._lerp3((0.12, 0.10, 0.11), (0.28, 0.18, 0.14), 0.3 + 0.3 * math.sin(p.y * 12.0))

    def lava_col(p, n):
        return w1._lerp3((0.96, 0.26, 0.04), (1.0, 0.78, 0.16), 0.5 + 0.5 * math.sin(p.y * 18.0 + p.z * 14.0))

    # Lõi thân dung nham phát sáng + các đốt giáp đá núi lửa bọc ngoài
    pts = [(0.0, -0.62, 0.68), (0.04, -0.44, 0.54), (0.12, -0.22, 0.30),
           (0.18, 0.04, 0.16), (-0.08, 0.32, 0.14), (-0.22, 0.60, 0.11), (0.04, 0.88, 0.06)]
    rads = [(0.115, 0.090), (0.110, 0.095), (0.145, 0.130),
            (0.155, 0.135), (0.130, 0.115), (0.092, 0.082), (0.025, 0.022)]
    w1._add_tube(bm, col, pts, rads, sides=22, subdiv=2, mat_index=3, color_fn=lava_col)

    for i in range(1, len(pts) - 1):
        px, py, pz = pts[i]
        rx, rz = rads[i]
        w2._add_sculpted_blob(bm, col, (px, py, pz + 0.02), (rx * 1.12, 0.11, rz * 1.08),
                              segs=18, rings=10, mat_index=2, color_fn=obsidian_col)

    # Đầu rồng rắn có vương miện sừng lửa & hàm dưới mở hé phun lửa
    w2._add_sculpted_blob(bm, col, (0.0, -0.68, 0.70), (0.145, 0.175, 0.092),
                          segs=20, rings=12, mat_index=2, color_fn=obsidian_col)
    w1._add_tube(bm, col, [(0.0, -0.60, 0.62), (0.0, -0.76, 0.58)], [(0.095, 0.036), (0.045, 0.018)],
                 sides=12, subdiv=1, mat_index=2, color_fn=obsidian_col)
    for sx in (-1.0, 1.0):
        w1._add_eye_bulb(bm, col, (sx * 0.095, -0.72, 0.73), 0.030, yaw=sx * 0.4, eye_col=(1.0, 0.68, 0.12), mat_eye=3)
        w1._add_tube(bm, col, [(sx * 0.07, -0.64, 0.76), (sx * 0.14, -0.58, 0.88), (sx * 0.18, -0.52, 0.98)],
                     [0.032, 0.020, 0.005], sides=8, subdiv=0, mat_index=3, color_fn=lava_col)

    bones = [
        ("Root", (0.10, 0.04, 0.16), (0.12, -0.16, 0.24), None, 0.42, 1.0),
        ("Neck", (0.10, -0.22, 0.30), (0.04, -0.46, 0.54), "Root", 0.32, 2.2),
        ("Head", (0.02, -0.50, 0.58), (0.0, -0.78, 0.68), "Neck", 0.28, 2.4),
        ("Body_Mid", (-0.08, 0.32, 0.14), (-0.22, 0.60, 0.11), "Root", 0.34, 2.0),
        ("Tail", (-0.22, 0.60, 0.11), (0.04, 0.88, 0.06), "Body_Mid", 0.26, 2.2),
    ]
    clips = {
        "Idle": (1, 56, [
            (1, {"Neck": {"rot": (0.06, 0, 0.08)}, "Head": {"rot": (-0.08, 0, -0.06)}}),
            (28, {"Neck": {"rot": (-0.06, 0, -0.08)}, "Head": {"rot": (0.08, 0, 0.06)}}),
            (56, {"Neck": {"rot": (0.06, 0, 0.08)}, "Head": {"rot": (-0.08, 0, -0.06)}}),
        ]),
        "Walk": (1, 24, [
            (1, {"Neck": {"rot": (0, 0, 0.22)}, "Body_Mid": {"rot": (0, 0, -0.24)}, "Tail": {"rot": (0, 0, 0.26)}}),
            (12, {"Neck": {"rot": (0, 0, -0.22)}, "Body_Mid": {"rot": (0, 0, 0.24)}, "Tail": {"rot": (0, 0, -0.26)}}),
            (24, {"Neck": {"rot": (0, 0, 0.22)}, "Body_Mid": {"rot": (0, 0, -0.24)}, "Tail": {"rot": (0, 0, 0.26)}}),
        ]),
        "Attack": (1, 26, [
            (1, {"Neck": {"rot": (0, 0, 0)}, "Head": {"rot": (0, 0, 0)}}),
            (9, {"Neck": {"rot": (0.30, 0, 0)}, "Head": {"rot": (-0.28, 0, 0)}}),
            (16, {"Neck": {"rot": (-0.38, 0, 0)}, "Head": {"rot": (0.24, 0, 0)}}),
            (26, {"Neck": {"rot": (0, 0, 0)}, "Head": {"rot": (0, 0, 0)}}),
        ]),
        "Hit": (1, 18, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (6, {"Root": {"rot": (-0.22, 0.24, 0.20)}}),
            (18, {"Root": {"rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_RanDungNham",
        os.path.join(out_dir, "Creature_RanDungNham.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_28_ran_dung_nham.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


# ================================================================== HANG 8: KHE GIÓ HÚ (4 LOÀI)

def build_29_de_gio(out_dir, prev_dir):
    """[M] Dế Gió — Khả năng: Nghe Xa.
    Thân dế khí động học, cặp râu siêu dài nhạy âm, màng cánh rung, cặp đùi sau dài gập khúc bật nhảy."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()

    def wind_chitin(p, n):
        return w1._lerp3((0.28, 0.34, 0.38), (0.62, 0.74, 0.78), 0.35 + 0.4 * max(0.0, n.z))

    w2._add_sculpted_blob(bm, col, (0.0, -0.18, 0.16), (0.068, 0.068, 0.065), segs=16, rings=10, mat_index=2, color_fn=wind_chitin)
    w2._add_sculpted_blob(bm, col, (0.0, 0.02, 0.17), (0.088, 0.185, 0.082), segs=20, rings=12, mat_index=2, color_fn=wind_chitin)
    for sx in (-1.0, 1.0):
        w1._add_eye_bulb(bm, col, (sx * 0.048, -0.22, 0.18), 0.022, yaw=sx * 0.3, eye_col=(0.45, 0.90, 0.98), mat_eye=3)
        # Râu siêu dài nhạy âm (Nghe Xa)
        w1._add_tube(bm, col, [(sx * 0.03, -0.23, 0.19), (sx * 0.14, -0.44, 0.34), (sx * 0.24, -0.62, 0.44)],
                     [0.010, 0.006, 0.002], sides=6, subdiv=1, mat_index=3, color_fn=lambda _p, _n: (0.6, 0.95, 0.98))
        # Cánh rung âm
        w2._add_layered_feather(bm, col, (sx * 0.04, -0.06, 0.24), (sx * 0.18, 0.96, 0.12), (0, 0, 1),
                               length=0.28, width=0.055, thickness=0.008, mat_index=4, color_fn=lambda _p, _n: (0.72, 0.88, 0.94))
        # Đùi sau nhảy xa (nối thẳng không tạo khớp cầu tách rời)
        w1._add_tube(bm, col, [(sx * 0.07, 0.06, 0.16), (sx * 0.14, 0.22, 0.34), (sx * 0.16, 0.30, 0.015)],
                     [0.036, 0.022, 0.010], sides=10, subdiv=0, mat_index=2, color_fn=wind_chitin)
        # Chân trước
        w2._add_jointed_limb(bm, col, [(sx * 0.06, -0.10, 0.14), (sx * 0.12, -0.14, 0.09), (sx * 0.13, -0.16, 0.012)],
                             [0.018, 0.012, 0.008], mat_index=2, color_fn=wind_chitin)

    bones = [
        ("Root", (0, 0.02, 0.17), (0, -0.12, 0.17), None, 0.25, 1.0),
        ("Head", (0, -0.12, 0.17), (0, -0.26, 0.18), "Root", 0.16, 2.2),
        ("Leg_BL", (-0.08, 0.06, 0.16), (-0.16, 0.30, 0.02), "Root", 0.18, 2.4),
        ("Leg_BR", (0.08, 0.06, 0.16), (0.16, 0.30, 0.02), "Root", 0.18, 2.4),
    ]
    clips = {
        "Idle": (1, 36, [
            (1, {"Head": {"rot": (0.05, 0, 0.08)}}),
            (18, {"Head": {"rot": (-0.06, 0, -0.08)}}),
            (36, {"Head": {"rot": (0.05, 0, 0.08)}}),
        ]),
        "Walk": (1, 18, [
            (1, {"Root": {"loc": (0, 0, 0.0)}, "Leg_BL": {"rot": (0.45, 0, 0)}, "Leg_BR": {"rot": (0.45, 0, 0)}}),
            (9, {"Root": {"loc": (0, -0.04, 0.20), "rot": (-0.20, 0, 0)}, "Leg_BL": {"rot": (-0.50, 0, 0)}, "Leg_BR": {"rot": (-0.50, 0, 0)}}),
            (18, {"Root": {"loc": (0, 0, 0.0)}, "Leg_BL": {"rot": (0.45, 0, 0)}, "Leg_BR": {"rot": (0.45, 0, 0)}}),
        ]),
        "Hit": (1, 16, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (6, {"Root": {"rot": (-0.25, 0.22, 0.18)}}),
            (16, {"Root": {"rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_DeGio",
        os.path.join(out_dir, "Creature_DeGio.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_29_de_gio.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_30_chuot_gio(out_dir, prev_dir):
    """[M] Chuột Gió — Khả năng: Thu Nhỏ.
    Chuột hang nhỏ nhắn thuôn mềm dễ chui khe hẹp, đôi tai to tròn bắt gió, đuôi dài thăng bằng."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()
    body_bm, body_col, _ = w1._new_bm()

    def fur_col(p, n):
        return w1._lerp3((0.56, 0.60, 0.66), (0.86, 0.88, 0.92), 0.3 + 0.5 * max(0.0, -n.z + 0.3))

    # Ghép nhiều khối cơ thể chồng lấn + Voxel Remesh liền mạch: Vai + Lưng gù + Hông/Đùi sau + Đầu + Má + 4 chân
    w2._add_sculpted_blob(body_bm, body_col, (0.0, -0.02, 0.12), (0.082, 0.098, 0.080), segs=18, rings=12, mat_index=0, color_fn=fur_col)
    w2._add_sculpted_blob(body_bm, body_col, (0.0, 0.08, 0.13), (0.098, 0.115, 0.092), segs=20, rings=14, mat_index=0, color_fn=fur_col)
    w2._add_sculpted_blob(body_bm, body_col, (0.0, -0.14, 0.14), (0.068, 0.088, 0.062),
                          disp_fn=lambda u: Vector((0.0, -0.025 * max(0.0, -u.y), 0.0)),
                          segs=20, rings=12, mat_index=0, color_fn=fur_col)
    for sx in (-1.0, 1.0):
        w2._add_sculpted_blob(body_bm, body_col, (sx * 0.044, -0.14, 0.125), (0.032, 0.038, 0.028), segs=12, rings=8, mat_index=0, color_fn=fur_col)
        w2._add_sculpted_blob(body_bm, body_col, (sx * 0.068, 0.09, 0.095), (0.038, 0.052, 0.045), segs=14, rings=10, mat_index=0, color_fn=fur_col)
    for sx, sy in [(-1, -1), (1, -1), (-1, 1), (1, 1)]:
        w1._add_tube(body_bm, body_col, [(sx * 0.06, sy * 0.09, 0.08), (sx * 0.075, sy * 0.10, 0.015)],
                     [0.022, 0.014], sides=8, subdiv=0, mat_index=0, color_fn=fur_col)

    _sculpt_remesh_into(bm, col, body_bm, voxel_size=0.011, smooth_iter=6, decimate_ratio=0.45, mat_index=0, color_fn=fur_col)

    # Mũi hồng + 2 tai to tròn bắt gió + mắt + đuôi dài
    w2._add_sculpted_blob(bm, col, (0.0, -0.25, 0.135), (0.016, 0.016, 0.014), segs=10, rings=8, mat_index=0, color_fn=lambda _p, _n: (0.94, 0.62, 0.68))
    for sx in (-1.0, 1.0):
        w2._add_sculpted_blob(bm, col, (sx * 0.068, -0.10, 0.215), (0.045, 0.018, 0.052), rot=(0.2, sx * 0.35, -sx * 0.3),
                              segs=16, rings=10, mat_index=0, color_fn=lambda _p, _n: (0.88, 0.72, 0.76))
        w1._add_eye_bulb(bm, col, (sx * 0.044, -0.18, 0.16), 0.017, yaw=sx * 0.35, eye_col=(0.08, 0.08, 0.10), mat_eye=1)

    w1._add_tube(bm, col, [(0.0, 0.18, 0.10), (0.05, 0.34, 0.09), (-0.02, 0.50, 0.12)],
                 [0.018, 0.012, 0.005], sides=10, subdiv=1, mat_index=0, color_fn=lambda _p, _n: (0.86, 0.68, 0.72))

    bones = [
        ("Root", (0, 0.04, 0.12), (0, -0.08, 0.13), None, 0.22, 1.0),
        ("Head", (0, -0.09, 0.13), (0, -0.24, 0.14), "Root", 0.16, 2.2),
        ("Tail", (0, 0.18, 0.10), (0, 0.50, 0.12), "Root", 0.18, 2.2),
    ]
    clips = {
        "Idle": (1, 36, [
            (1, {"Head": {"rot": (0.05, 0, 0.08)}, "Tail": {"rot": (0, 0, -0.12)}}),
            (18, {"Head": {"rot": (-0.08, 0, -0.08)}, "Tail": {"rot": (0, 0, 0.12)}}),
            (36, {"Head": {"rot": (0.05, 0, 0.08)}, "Tail": {"rot": (0, 0, -0.12)}}),
        ]),
        "Walk": (1, 14, [
            (1, {"Root": {"loc": (0, 0, 0.0)}, "Tail": {"rot": (0, 0, 0.25)}}),
            (7, {"Root": {"loc": (0, 0, 0.04)}, "Tail": {"rot": (0, 0, -0.25)}}),
            (14, {"Root": {"loc": (0, 0, 0.0)}, "Tail": {"rot": (0, 0, 0.25)}}),
        ]),
        "Hit": (1, 16, [
            (1, {"Root": {"scale": (1, 1, 1)}}),
            (6, {"Root": {"scale": (0.72, 0.72, 0.72)}}),
            (16, {"Root": {"scale": (1, 1, 1)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_ChuotGio",
        os.path.join(out_dir, "Creature_ChuotGio.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_30_chuot_gio.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_31_chon_khe(out_dir, prev_dir):
    """[S] Chồn Khe — Khả năng: Vồ.
    Điêu khắc liền mạch bằng Voxel Remesh + Smooth từ nhiều khối cơ bắp:
    vai trước, lưng uốn, eo thắt, hông sau, đùi-cẳng-bàn có ngón, má lông, gờ mày và đuôi chồn xù."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()
    body_bm, body_col, _ = w1._new_bm()

    def ferret_fur(p, n):
        base = w1._lerp3((0.34, 0.25, 0.21), (0.72, 0.62, 0.50), 0.35 + 0.4 * max(0.0, n.z))
        if p.y < -0.26 or p.y > 0.36:
            base = w1._lerp3(base, (0.16, 0.14, 0.16), 0.60)
        if n.z < -0.35 and p.y > -0.25 and p.y < 0.32:
            base = w1._lerp3(base, (0.82, 0.75, 0.62), 0.55)
        return base

    # Khung cột sống + cơ bắp vai/lưng/eo/hông + đuôi chồn xù điêu khắc liền mạch
    w1._add_tube(body_bm, body_col, [(0.0, -0.34, 0.28), (0.02, -0.10, 0.21), (-0.01, 0.12, 0.20), (0.0, 0.28, 0.22)],
                 [(0.068, 0.062), (0.088, 0.080), (0.088, 0.080), (0.082, 0.075)], sides=18, subdiv=2, mat_index=0, color_fn=ferret_fur)
    w2._add_sculpted_blob(body_bm, body_col, (0.01, -0.22, 0.24), (0.090, 0.145, 0.085), rot=(-0.22, 0.0, 0.0), segs=18, rings=12, mat_index=0, color_fn=ferret_fur)
    w2._add_sculpted_blob(body_bm, body_col, (0.02, -0.04, 0.20), (0.098, 0.155, 0.088), rot=(0.04, 0.0, -0.06), segs=18, rings=12, mat_index=0, color_fn=ferret_fur)
    w2._add_sculpted_blob(body_bm, body_col, (-0.01, 0.14, 0.20), (0.096, 0.150, 0.088), rot=(0.08, 0.0, 0.05), segs=18, rings=12, mat_index=0, color_fn=ferret_fur)
    w2._add_sculpted_blob(body_bm, body_col, (0.0, 0.26, 0.22), (0.104, 0.135, 0.094), rot=(-0.06, 0.0, 0.0), segs=18, rings=12, mat_index=0, color_fn=ferret_fur)
    w1._add_tube(body_bm, body_col, [(0.0, 0.28, 0.22), (0.02, 0.44, 0.25), (0.06, 0.62, 0.27), (0.10, 0.78, 0.23)],
                 [(0.078, 0.072), (0.085, 0.080), (0.074, 0.070), (0.024, 0.022)], sides=16, subdiv=2, mat_index=0, color_fn=ferret_fur)

    # Đầu chồn + gờ lông mày + má + mõm + tai + bả vai/đùi/cẳng/bàn chân 3 ngón liền mạch
    w2._add_sculpted_blob(body_bm, body_col, (0.0, -0.37, 0.30), (0.074, 0.088, 0.064), segs=18, rings=12, mat_index=0, color_fn=ferret_fur)
    w2._add_sculpted_blob(body_bm, body_col, (0.0, -0.47, 0.30), (0.048, 0.075, 0.042),
                          disp_fn=lambda u: Vector((0.0, -0.028 * max(0.0, -u.y), 0.0)), segs=16, rings=10, mat_index=0, color_fn=ferret_fur)
    for sx in (-1.0, 1.0):
        w2._add_sculpted_blob(body_bm, body_col, (sx * 0.052, -0.40, 0.285), (0.040, 0.046, 0.032), segs=12, rings=8, mat_index=0, color_fn=ferret_fur)
        w2._add_sculpted_blob(body_bm, body_col, (sx * 0.036, -0.44, 0.340), (0.020, 0.032, 0.015), segs=10, rings=8, mat_index=0, color_fn=ferret_fur)
        w2._add_sculpted_blob(body_bm, body_col, (sx * 0.052, -0.38, 0.365), (0.024, 0.018, 0.034), segs=10, rings=8, mat_index=0, color_fn=ferret_fur)
        # Bả vai trước & đùi sau cơ bắp + chân nối mượt
        w2._add_sculpted_blob(body_bm, body_col, (sx * 0.072, -0.19, 0.20), (0.044, 0.055, 0.052), segs=12, rings=8, mat_index=0, color_fn=ferret_fur)
        w2._add_sculpted_blob(body_bm, body_col, (sx * 0.078, 0.23, 0.20), (0.052, 0.072, 0.062), segs=12, rings=10, mat_index=0, color_fn=ferret_fur)
        for hip_y, knee_y, foot_y in [(-0.18, -0.20, -0.26), (0.22, 0.26, 0.20)]:
            w1._add_tube(body_bm, body_col, [(sx * 0.08, hip_y, 0.17), (sx * 0.11, knee_y, 0.10), (sx * 0.10, foot_y, 0.018)],
                         [0.036, 0.026, 0.022], sides=10, subdiv=1, mat_index=0, color_fn=ferret_fur)
            w2._add_sculpted_blob(body_bm, body_col, (sx * 0.10, foot_y - 0.015, 0.016), (0.028, 0.034, 0.016), segs=10, rings=8, mat_index=0, color_fn=ferret_fur)

    _sculpt_remesh_into(bm, col, body_bm, voxel_size=0.015, smooth_iter=6, decimate_ratio=0.45, mat_index=0, color_fn=ferret_fur)

    # Mũi bóng, mắt sáng & móng vuốt sắc gắn sau remesh để giữ độ sắc nét
    w2._add_sculpted_blob(bm, col, (0.0, -0.56, 0.295), (0.014, 0.014, 0.012), segs=10, rings=8, mat_index=1, color_fn=lambda _p, _n: (0.08, 0.08, 0.08))
    for sx in (-1.0, 1.0):
        w1._add_eye_bulb(bm, col, (sx * 0.048, -0.46, 0.326), 0.016, yaw=sx * 0.35, eye_col=(0.95, 0.72, 0.22), mat_eye=3)
        for foot_y in (-0.26, 0.20):
            for dx in (-0.014, 0.0, 0.014):
                w1._add_tube(bm, col, [(sx * 0.10 + dx, foot_y - 0.035, 0.016), (sx * 0.10 + dx, foot_y - 0.058, 0.006)],
                             [0.006, 0.002], sides=6, subdiv=0, mat_index=2, color_fn=lambda _p, _n: (0.12, 0.10, 0.10))

    bones = [
        ("Root", (0, 0.02, 0.20), (0, -0.10, 0.21), None, 0.28, 1.0),
        ("Spine_Front", (0, -0.12, 0.21), (0, -0.28, 0.25), "Root", 0.24, 2.0),
        ("Head", (0, -0.30, 0.26), (0, -0.52, 0.30), "Spine_Front", 0.22, 2.2),
        ("Hip", (0, 0.12, 0.20), (0, 0.28, 0.22), "Root", 0.24, 2.0),
        ("Tail", (0, 0.30, 0.23), (0.09, 0.74, 0.24), "Hip", 0.24, 2.2),
    ]
    clips = {
        "Idle": (1, 44, [
            (1, {"Spine_Front": {"rot": (0.04, 0, 0.06)}, "Head": {"rot": (0.06, 0, 0.08)}, "Tail": {"rot": (0.06, 0, -0.10)}}),
            (22, {"Spine_Front": {"rot": (-0.04, 0, -0.06)}, "Head": {"rot": (-0.06, 0, -0.08)}, "Tail": {"rot": (-0.06, 0, 0.10)}}),
            (44, {"Spine_Front": {"rot": (0.04, 0, 0.06)}, "Head": {"rot": (0.06, 0, 0.08)}, "Tail": {"rot": (0.06, 0, -0.10)}}),
        ]),
        "Walk": (1, 18, [
            (1, {"Root": {"loc": (0, 0, 0.0), "rot": (0.12, 0, 0)}, "Spine_Front": {"rot": (0.14, 0, 0)}, "Hip": {"rot": (-0.12, 0, 0)}}),
            (9, {"Root": {"loc": (0, -0.05, 0.12), "rot": (-0.14, 0, 0)}, "Spine_Front": {"rot": (-0.14, 0, 0)}, "Hip": {"rot": (0.14, 0, 0)}}),
            (18, {"Root": {"loc": (0, 0, 0.0), "rot": (0.12, 0, 0)}, "Spine_Front": {"rot": (0.14, 0, 0)}, "Hip": {"rot": (-0.12, 0, 0)}}),
        ]),
        "Attack": (1, 22, [
            (1, {"Root": {"loc": (0, 0, 0)}}),
            (7, {"Root": {"loc": (0, 0.14, -0.04), "rot": (0.24, 0, 0)}, "Spine_Front": {"rot": (0.22, 0, 0)}}),
            (14, {"Root": {"loc": (0, -0.34, 0.18), "rot": (-0.28, 0, 0)}, "Spine_Front": {"rot": (-0.24, 0, 0)}}),
            (22, {"Root": {"loc": (0, 0, 0)}}),
        ]),
        "Hit": (1, 18, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (6, {"Root": {"rot": (-0.24, 0.24, 0.20)}}),
            (18, {"Root": {"rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_ChonKhe",
        os.path.join(out_dir, "Creature_ChonKhe.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_31_chon_khe.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


def build_32_cao_gio(out_dir, prev_dir):
    """[C] Cáo Gió — Khả năng: Ảo Ảnh.
    Điêu khắc liền mạch bằng Voxel Remesh + Smooth: lồng ngực, bờm lông cổ nhiều lớp nhọn,
    eo thắt, hông, bả vai-đùi-chân thon, đầu cáo có gờ mày-má-tai và 3 đuôi xù phát sáng ảo ảnh."""
    kc.new_scene()
    mats = w1._make_creature_materials()
    bm, col, flat = w1._new_bm()
    body_bm, body_col, _ = w1._new_bm()

    def silver_fur(p, n):
        base = w1._lerp3((0.64, 0.72, 0.82), (0.95, 0.97, 1.0), 0.4 + 0.4 * max(0.0, n.z))
        if p.z < 0.16:
            base = w1._lerp3(base, (0.26, 0.32, 0.44), 0.60)
        if p.y > 0.65:
            base = w1._lerp3(base, (0.48, 0.94, 1.0), min(1.0, (p.y - 0.65) * 2.5))
        return base

    def mirage_tip(p, n):
        return w1._lerp3((0.72, 0.86, 0.98), (0.48, 0.94, 1.0), max(0.0, min(1.0, (p.y - 0.35) * 1.5)))

    # Thân Cáo Gió ghép khung cột sống + Lồng ngực + Bờm lông cổ nhiều chùm + Eo + Hông
    w1._add_tube(body_bm, body_col, [(0.0, -0.34, 0.49), (0.0, -0.14, 0.43), (0.0, 0.06, 0.40), (0.0, 0.28, 0.39)],
                 [(0.105, 0.100), (0.135, 0.125), (0.105, 0.100), (0.118, 0.112)], sides=20, subdiv=2, mat_index=0, color_fn=silver_fur)
    w2._add_sculpted_blob(body_bm, body_col, (0.0, -0.16, 0.44), (0.145, 0.185, 0.145), rot=(-0.20, 0.0, 0.0), segs=22, rings=14, mat_index=0, color_fn=silver_fur)
    # Bờm lông cổ nhọn nhiều lớp quanh ngực & vai
    for ang_deg in (-60, -25, 0, 25, 60):
        rad = math.radians(ang_deg)
        w2._add_layered_feather(body_bm, body_col, (math.sin(rad) * 0.10, -0.24, 0.42),
                                (math.sin(rad) * 0.4, 0.45, -0.75), (0, -1, 0.3),
                                length=0.19, width=0.055, thickness=0.028, mat_index=0, color_fn=silver_fur)
    w2._add_sculpted_blob(body_bm, body_col, (0.0, 0.06, 0.40), (0.108, 0.165, 0.105), segs=18, rings=12, mat_index=0, color_fn=silver_fur)
    w2._add_sculpted_blob(body_bm, body_col, (0.0, 0.24, 0.39), (0.126, 0.165, 0.120), rot=(0.08, 0.0, 0.0), segs=20, rings=12, mat_index=0, color_fn=silver_fur)

    # Đầu cáo thanh tú (Sọ + Gờ mày + Má lông + Mõm + Tai)
    w2._add_sculpted_blob(body_bm, body_col, (0.0, -0.39, 0.59), (0.105, 0.125, 0.098), segs=20, rings=12, mat_index=0, color_fn=silver_fur)
    w2._add_sculpted_blob(body_bm, body_col, (0.0, -0.52, 0.575), (0.062, 0.120, 0.054),
                          disp_fn=lambda u: Vector((0.0, -0.05 * max(0.0, -u.y), 0.0)), segs=18, rings=12, mat_index=0, color_fn=silver_fur)
    for sx in (-1.0, 1.0):
        w2._add_sculpted_blob(body_bm, body_col, (sx * 0.075, -0.40, 0.57), (0.054, 0.068, 0.048), segs=14, rings=10, mat_index=0, color_fn=silver_fur)
        w2._add_sculpted_blob(body_bm, body_col, (sx * 0.050, -0.46, 0.635), (0.025, 0.042, 0.018), segs=10, rings=8, mat_index=0, color_fn=silver_fur)
        w1._add_tube(body_bm, body_col, [(sx * 0.065, -0.38, 0.65), (sx * 0.095, -0.36, 0.78), (sx * 0.11, -0.34, 0.89)],
                     [0.046, 0.028, 0.008], sides=10, subdiv=1, mat_index=0, color_fn=silver_fur)
        # Bả vai, đùi sau & 4 chân thon liền mạch
        w2._add_sculpted_blob(body_bm, body_col, (sx * 0.090, -0.18, 0.36), (0.052, 0.065, 0.075), segs=14, rings=10, mat_index=0, color_fn=silver_fur)
        w2._add_sculpted_blob(body_bm, body_col, (sx * 0.094, 0.24, 0.34), (0.058, 0.086, 0.085), rot=(0.2, 0, 0), segs=14, rings=10, mat_index=0, color_fn=silver_fur)
        for hip_y, foot_y in [(-0.18, -0.22), (0.24, 0.26)]:
            w1._add_tube(body_bm, body_col, [(sx * 0.095, hip_y, 0.34), (sx * 0.105, hip_y + 0.03, 0.18), (sx * 0.098, foot_y, 0.020)],
                         [0.042, 0.026, 0.020], sides=10, subdiv=1, mat_index=0, color_fn=silver_fur)
            w2._add_sculpted_blob(body_bm, body_col, (sx * 0.098, foot_y - 0.018, 0.018), (0.028, 0.036, 0.016), segs=10, rings=8, mat_index=0, color_fn=silver_fur)

    # 3 đuôi xù liền mạch điêu khắc chung trong body_bm
    for sx, dz in [(-0.34, 0.02), (0.0, 0.16), (0.34, 0.02)]:
        t_pts = [
            (0.0, 0.34, 0.38),
            (sx * 0.45, 0.54, 0.45 + dz * 0.5),
            (sx * 0.95, 0.76, 0.53 + dz),
            (sx * 1.12, 0.96, 0.46 + dz * 0.8),
        ]
        w1._add_tube(body_bm, body_col, t_pts, [(0.058, 0.054), (0.098, 0.090), (0.105, 0.095), (0.026, 0.024)],
                     sides=16, subdiv=2, mat_index=0, color_fn=silver_fur)

    _sculpt_remesh_into(bm, col, body_bm, voxel_size=0.018, smooth_iter=6, decimate_ratio=0.45, mat_index=0, color_fn=silver_fur)

    # Chi tiết sắc nét sau remesh: Mũi đen, mắt lam phát sáng, móng vuốt & chóp lông đuôi ảo ảnh
    w2._add_sculpted_blob(bm, col, (0.0, -0.67, 0.57), (0.020, 0.020, 0.018), segs=12, rings=8, mat_index=1, color_fn=lambda _p, _n: (0.08, 0.08, 0.10))
    for sx in (-1.0, 1.0):
        w1._add_eye_bulb(bm, col, (sx * 0.078, -0.48, 0.610), 0.022, yaw=sx * 0.35, eye_col=(0.38, 0.92, 1.0), mat_eye=3)
        for foot_y in (-0.22, 0.26):
            for dx in (-0.014, 0.0, 0.014):
                w1._add_tube(bm, col, [(sx * 0.098 + dx, foot_y - 0.04, 0.018), (sx * 0.098 + dx, foot_y - 0.068, 0.006)],
                             [0.006, 0.002], sides=6, subdiv=0, mat_index=2, color_fn=lambda _p, _n: (0.14, 0.14, 0.18))
    for sx, dz in [(-0.34, 0.02), (0.0, 0.16), (0.34, 0.02)]:
        w2._add_layered_feather(bm, col, (sx * 0.78, 0.68, 0.50 + dz * 0.8),
                                (sx * 0.45, 0.85, -0.15), (0, 0, 1),
                                length=0.38, width=0.065, thickness=0.036, mat_index=3, color_fn=mirage_tip)

    bones = [
        ("Root", (0, 0.02, 0.40), (0, -0.14, 0.42), None, 0.38, 1.0),
        ("Head", (0, -0.24, 0.46), (0, -0.50, 0.60), "Root", 0.26, 2.2),
        ("Tail_C", (0, 0.34, 0.38), (0, 0.98, 0.52), "Root", 0.28, 2.2),
        ("Tail_L", (0, 0.34, 0.38), (-0.28, 0.94, 0.46), "Root", 0.24, 2.4),
        ("Tail_R", (0, 0.34, 0.38), (0.28, 0.94, 0.46), "Root", 0.24, 2.4),
    ]
    clips = {
        "Idle": (1, 52, [
            (1, {"Head": {"rot": (0.04, 0, 0.08)}, "Tail_C": {"rot": (0.06, 0, 0)}, "Tail_L": {"rot": (0, 0, 0.12)}, "Tail_R": {"rot": (0, 0, -0.12)}}),
            (26, {"Head": {"rot": (-0.06, 0, -0.08)}, "Tail_C": {"rot": (-0.06, 0, 0)}, "Tail_L": {"rot": (0, 0, -0.12)}, "Tail_R": {"rot": (0, 0, 0.12)}}),
            (52, {"Head": {"rot": (0.04, 0, 0.08)}, "Tail_C": {"rot": (0.06, 0, 0)}, "Tail_L": {"rot": (0, 0, 0.12)}, "Tail_R": {"rot": (0, 0, -0.12)}}),
        ]),
        "Walk": (1, 20, [
            (1, {"Root": {"loc": (0, 0, 0.0)}, "Tail_C": {"rot": (0, 0, 0.18)}}),
            (10, {"Root": {"loc": (0, 0, 0.08)}, "Tail_C": {"rot": (0, 0, -0.18)}}),
            (20, {"Root": {"loc": (0, 0, 0.0)}, "Tail_C": {"rot": (0, 0, 0.18)}}),
        ]),
        "Action": (1, 28, [
            (1, {"Root": {"scale": (1, 1, 1)}, "Tail_L": {"rot": (0, 0, 0)}, "Tail_R": {"rot": (0, 0, 0)}}),
            (12, {"Root": {"scale": (1.06, 1.06, 1.06)}, "Tail_L": {"rot": (0, 0, 0.35)}, "Tail_R": {"rot": (0, 0, -0.35)}}),
            (28, {"Root": {"scale": (1, 1, 1)}, "Tail_L": {"rot": (0, 0, 0)}, "Tail_R": {"rot": (0, 0, 0)}}),
        ]),
        "Hit": (1, 18, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (6, {"Root": {"rot": (-0.22, 0.24, 0.18)}}),
            (18, {"Root": {"rot": (0, 0, 0)}}),
        ]),
    }

    w1._finalize_and_export(
        bm, flat, mats, "Creature_CaoGio",
        os.path.join(out_dir, "Creature_CaoGio.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_32_cao_gio.png") if prev_dir else None,
        bone_specs=bones, clips_spec=clips,
    )


BUILDERS = [
    ("nam_di_bo", build_21_nam_di_bo),
    ("buom_bao_tu", build_22_buom_bao_tu),
    ("ech_doc_dom", build_23_ech_doc_dom),
    ("nam_khong_lo", build_24_nam_khong_lo),
    ("oc_luu_huynh", build_25_oc_luu_huynh),
    ("cua_da_nong", build_26_cua_da_nong),
    ("ky_nhong_lua", build_27_ky_nhong_lua),
    ("ran_dung_nham", build_28_ran_dung_nham),
    ("de_gio", build_29_de_gio),
    ("chuot_gio", build_30_chuot_gio),
    ("chon_khe", build_31_chon_khe),
    ("cao_gio", build_32_cao_gio),
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

    print(f"Hoàn tất dựng 12 mô hình Wave 3 (Tầng Giữa) trong {time.time() - t0:.1f}s", flush=True)


if __name__ == "__main__":
    main()
