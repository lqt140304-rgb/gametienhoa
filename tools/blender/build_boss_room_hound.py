"""Dựng Phòng Trùm Đợt 2 - Phòng 1: Chó Xương ("Nghĩa Địa Cự Thú") — Blender headless.

Tuân thủ docs/superpowers/specs/2026-11-22-boss-rooms-wave2-design.md:
- Sàn phẳng bán kính 32 m (đường kính 64 m), trần kín 33.5 m (không lỗ).
- Cửa vào duy nhất phía Nam (4.8 x 7.2 m), vòm Hàm & Xương Đùi Cự Thú kèm đoạn xích đen-xanh đứt.
- Kể chuyện môi trường:
  + Ổ Boss_Wait (0, 21, 0): khung xương lồng ngực & hộp sọ cự thú khổng lồ nửa vùi dưới bùn hóa thạch,
    cạnh cọc trụ đá cổ bị giật gãy cùng đoạn xích đen-xanh lạnh đứt lìa.
  + Dấu vết Vồ & Cào Xé: các rãnh móng cào 4 đường & vết trượt dài sát sàn (<= 0.03 m).
  + Dấu vết Phun Xương & Bọ Ăn Xác: mảnh xương vụn nhai nát & mảng đá ăn mòn quanh ổ/cột (<= 0.05 m).
  + Dấu vết Triệu Hồi: 5 ổ đất nứt có xương sườn & sọ chó nhỏ trồi nửa vời sát sàn ở r = 6..10 m (<= 0.05 m).
- Vật cản chiến đấu: 6 cột xương sườn cự thú (Prop_Pillar_01..06), 4 bục sọ & đốt sống (Prop_Perch_01..04, cao 0.9-1.4 m),
  6 cụm tinh thể ma trơi xanh lam/lục lạnh (Prop_Crystal_01..06).

Chạy:
  D:/blender/blender.exe -b -P tools/blender/build_boss_room_hound.py -- --out Assets/_Project/Models/Underroot --previews docs/art/previews
"""
import argparse
import math
import os
import random
import sys
import time

import bpy
import bmesh
from mathutils import Vector, Matrix, Euler, noise

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_common as kit  # noqa: E402
import boss_room_common as brc  # noqa: E402


WALL_TOP_Z = 24.5
DOME_PEAK_Z = 33.5
BOSS_WAIT_POS = (0.0, 21.0, 0.0)

PILLAR_ANGLES_FROM_NORTH = [32.0, 88.0, 138.0, 222.0, 272.0, 328.0]
PILLAR_RADII = [16.2, 15.5, 17.5, 17.0, 15.8, 16.5]
PERCH_ANGLES_FROM_NORTH = [52.0, 128.0, 232.0, 308.0]
PERCH_RADII = [11.5, 12.8, 12.2, 11.8]
CRYSTAL_ANGLES_FROM_NORTH = [25.0, 75.0, 125.0, 235.0, 285.0, 335.0]

# Bảng màu: bùn hóa thạch xám tro tối, xương khô trắng ngà, ánh ma trơi xanh lục/lam lạnh
MUD_DEEP = (0.018, 0.020, 0.022)
MUD_DARK = (0.042, 0.045, 0.048)
MUD_ASH = (0.088, 0.092, 0.095)
ACID_PIT = (0.032, 0.055, 0.036)

BONE_SHADOW = (0.16, 0.14, 0.11)
BONE_OLD = (0.34, 0.30, 0.23)
BONE_IVORY = (0.58, 0.53, 0.42)

ECTO_CYAN = (0.06, 0.72, 0.68)
ECTO_GREEN = (0.14, 0.88, 0.48)
CHAIN_DARK = (0.03, 0.05, 0.06)
CHAIN_CYAN = (0.08, 0.24, 0.28)


def _floor_z(x, y):
    r = math.hypot(x, y)
    d_door = math.hypot(x, y - brc.DOOR_Y)
    d_boss = math.hypot(x - BOSS_WAIT_POS[0], y - BOSS_WAIT_POS[1])
    flat_mask = (
        brc.smoothstep(3.0, 7.0, r)
        * brc.smoothstep(4.5, 9.0, d_door)
        * brc.smoothstep(4.5, 8.0, d_boss)
    )
    if r > brc.ROOM_RADIUS:
        flat_mask *= 1.0 - brc.smoothstep(brc.ROOM_RADIUS, brc.ROOM_RADIUS + 2.5, r)
    n = 0.030 * math.sin(x * 0.20 + 0.3) * math.cos(y * 0.22 - 0.5) + 0.020 * noise.noise(Vector((x * 0.16, y * 0.16, 2.3)))
    return n * flat_mask


def _wall_disp(px, py, z_base, r_scale, li):
    rock_n = brc.stepped(0.5 + 0.5 * noise.noise(Vector((px * 0.08, py * 0.08, z_base * 0.10))), steps=4.0, sharp=0.68) - 0.5
    crag = (brc.ridged(Vector((px * 0.075 + 1.4, py * 0.075, z_base * 0.09))) - 0.5) * 1.8
    r_disp = (rock_n * 1.8 + crag) * (0.45 if r_scale < 0.7 else 1.0)
    z_disp = noise.noise(Vector((px * 0.09, py * 0.09, 4.1))) * 1.3 * r_scale if li > 9 else 0.0
    return r_disp, z_disp


def _paint_shell(pos, _n):
    r = math.hypot(pos.x, pos.y)
    n_c = noise.noise(Vector((pos.x * 0.15, pos.y * 0.15, pos.z * 0.15))) * 0.5 + 0.5
    n_f = brc.ridged(Vector((pos.x * 0.38, pos.y * 0.38, pos.z * 0.38)))

    if pos.z < 0.45 and r < brc.ROOM_RADIUS + 0.5:
        c = brc.mix(MUD_DARK, MUD_ASH, n_c * 0.58)
        # Vệt axit Bọ Ăn Xác ăn mòn lốm đốm quanh vòng ngoài ổ & chân cột
        if 8.0 < r < 22.0 and n_f > 0.68:
            c = brc.mix(c, ACID_PIT, (n_f - 0.68) * 2.2)
        # Vết trầy móng trước cửa
        if abs(pos.x) < 2.4 and -31.5 < pos.y < -25.0:
            c = brc.mix(c, MUD_DEEP, 0.50)
        return c

    h_t = brc.clamp01(pos.z / DOME_PEAK_Z)
    c = brc.mix(MUD_DARK, MUD_DEEP, h_t * 0.85)
    c = brc.mix(c, MUD_ASH, n_f * 0.25 * (1.0 - h_t * 0.6))
    return c


def _add_chain_link(bm, center, rot_euler, radius=0.22, thickness=0.055):
    """Thêm 1 mắt xích sắt hình bầu dục."""
    mat_r = Euler(rot_euler, "XYZ").to_matrix().to_4x4()
    pts = []
    n_seg = 12
    for i in range(n_seg + 1):
        a = 2.0 * math.pi * i / n_seg
        lp = Vector((math.cos(a) * radius * 1.45, math.sin(a) * radius * 0.85, 0.0))
        pts.append(Vector(center) + (mat_r @ lp))
    rgs = brc.parallel_tube_rings(bm, pts, [thickness] * len(pts), 6)
    kit.bridge_rings(bm, rgs)


def _add_chain_strand(bm, p_start, p_end, sag=0.6, link_r=0.20):
    a, b = Vector(p_start), Vector(p_end)
    dist = (b - a).length
    n_links = max(3, int(dist / (link_r * 2.1)))
    d = (b - a).normalized()
    yaw = math.atan2(d.y, d.x)
    pitch = -math.asin(max(-0.99, min(0.99, d.z)))
    for i in range(n_links):
        t = (i + 0.5) / n_links
        pos = a.lerp(b, t) - Vector((0.0, 0.0, sag * math.sin(t * math.pi)))
        roll = (math.pi * 0.5) if (i % 2 == 1) else 0.0
        _add_chain_link(bm, pos, (roll, pitch, yaw), radius=link_r, thickness=link_r * 0.26)


def _build_arch_and_ceiling_bones(rng, bone_mat, stone_mat, glow_mat, chain_mat):
    bm_arch = bmesh.new()
    bm_bone = bmesh.new()
    bm_chain = bmesh.new()
    bm_glow = bmesh.new()

    # 1. Vòm Hàm & Xương Đùi Cự Thú tại lối vào Nam
    for side in (-1.0, 1.0):
        px = side * 3.15
        brc.add_angular_stone_block(bm_arch, (px, brc.DOOR_Y + 0.70, 0.60), (2.4, 2.8, 1.2), (0.0, 0.0, side * 0.08), rock_seed=21 + int(side + 2), chip=0.15)
        # Hai xương đùi cự thú dựng làm cột hai bên cổng
        femur_pts = [
            (px, brc.DOOR_Y + 0.85, 0.8),
            (px - side * 0.15, brc.DOOR_Y + 0.90, 3.5),
            (px - side * 0.45, brc.DOOR_Y + 0.95, 6.2),
            (px - side * 0.95, brc.DOOR_Y + 0.90, 7.8),
        ]
        brc.add_curved_bone(bm_bone, femur_pts, [0.72, 0.48, 0.52, 0.78], segs=9, wobble_amp=0.10)

        # Khớp cầu đầu xương & xích đứt treo lủng lẳng hai bên ngưỡng cửa
        brc.add_rock_cluster(bm_bone, (px - side * 0.10, brc.DOOR_Y + 1.0, 1.15), (0.85, 0.85, 0.65), seed=55 + int(side + 2), count=2)
        _add_chain_strand(bm_chain, (px - side * 0.3, brc.DOOR_Y + 1.35, 5.8), (px - side * 0.55, brc.DOOR_Y + 1.65, 2.3), sag=0.15, link_r=0.19)

    # Hàm răng thú khổng lồ úp trên đỉnh vòm (xương hàm trên cong + dãy răng nhọn)
    jaw_pts = []
    jaw_rad = []
    for ji in range(9):
        t = ji / 8.0
        ang = math.pi * (0.08 + 0.84 * t)
        jx = math.cos(ang) * 3.35
        jz = 6.95 + math.sin(ang) * 2.55
        jaw_pts.append((jx, brc.DOOR_Y + 0.95, jz))
        jaw_rad.append(0.55 + 0.25 * math.sin(ang * math.pi))
    brc.add_curved_bone(bm_bone, jaw_pts, jaw_rad, segs=8, wobble_amp=0.09)

    # Răng thú chĩa xuống dưới vòm
    for ti in range(7):
        t = (ti + 0.5) / 7.0
        ang = math.pi * (0.18 + 0.64 * t)
        tx = math.cos(ang) * 2.95
        tz = 6.90 + math.sin(ang) * 2.15
        t_dir = Vector((-math.cos(ang) * 0.15, 0.08, -1.0)).normalized()
        brc.add_hex_crystal(bm_bone, Vector((tx, brc.DOOR_Y + 0.95, tz)), t_dir, 1.15 + 0.35 * math.sin(t * math.pi), 0.20, seed=ti + 10)

    # Hai cụm ma trơi xanh lam soi ngưỡng cửa
    for side in (-1.0, 1.0):
        brc.add_hex_crystal(bm_glow, Vector((side * 3.5, brc.DOOR_Y + 1.5, 2.4)), Vector((-side * 0.3, 0.7, 0.6)).normalized(), 1.4, 0.22, seed=int(side + 3))

    # 2. Nhũ đá & xương sườn hóa thạch rủ từ trần vòm (ở cao độ > 24 m, an toàn)
    for si in range(18):
        ang = rng.uniform(0.0, 2.0 * math.pi)
        rad = rng.uniform(10.0, 26.0)
        sx = math.cos(ang) * rad
        sy = math.sin(ang) * rad
        sz_top = WALL_TOP_Z + (DOME_PEAK_Z - WALL_TOP_Z) * (1.0 - (rad / brc.ROOM_RADIUS) ** 1.7)
        slen = rng.uniform(2.5, 5.2)
        brc.add_hex_crystal(bm_arch, Vector((sx, sy, sz_top + 0.5)), Vector((rng.uniform(-0.08, 0.08), rng.uniform(-0.08, 0.08), -1.0)).normalized(), slen, rng.uniform(0.32, 0.62), seed=si * 9)

    arch_obj = kit.finish_object(bm_arch, "BossRoom_Hound_Arch", stone_mat)
    kit.paint(arch_obj, lambda p, n: brc.mix(MUD_DARK, MUD_ASH, brc.ridged(p * 0.3)))

    bone_obj = kit.finish_object(bm_bone, "BossRoom_Hound_ArchJaw", bone_mat)
    kit.paint(bone_obj, lambda p, n: brc.mix(BONE_SHADOW, BONE_IVORY, 0.55 + 0.35 * brc.ridged(p * 0.45)))

    chain_obj = kit.finish_object(bm_chain, "BossRoom_Hound_ArchChains", chain_mat)
    kit.paint(chain_obj, lambda p, n: brc.mix(CHAIN_DARK, CHAIN_CYAN, 0.5 + 0.5 * math.sin(p.z * 2.2)))

    glow_obj = kit.finish_object(bm_glow, "BossRoom_Hound_ArchGlow", glow_mat)
    kit.paint(glow_obj, lambda p, n: brc.mix(ECTO_CYAN, ECTO_GREEN, 0.5 + 0.5 * math.sin(p.z)))

    return [arch_obj, bone_obj, chain_obj, glow_obj]


def _build_pillars_perches_and_crystals(rng, bone_mat, glow_mat, chain_mat):
    objs = []

    # 1. 6 cột xương sườn cự thú hóa thạch (r = 15..18 m) kèm vòng xích đứt
    for idx, (ang_deg, rad) in enumerate(zip(PILLAR_ANGLES_FROM_NORTH, PILLAR_RADII), 1):
        ang = math.radians(90.0 - ang_deg)
        cx, cy = math.cos(ang) * rad, math.sin(ang) * rad
        inward = Vector((-math.cos(ang), -math.sin(ang), 0.0)).normalized()

        bm_p = bmesh.new()
        # Chân trụ đá bùn ôm gốc xương sườn
        brc.add_rock_cluster(bm_p, (cx, cy, 0.4), (1.75, 1.75, 1.15), seed=idx * 17, count=3)

        # Thân xương sườn cong cao ~10-12 m nghiêng vào tâm phòng
        h_max = 10.5 + (idx % 2) * 1.8
        rib_pts = []
        rib_rad = []
        for si in range(7):
            t = si / 6.0
            curve_in = 1.65 * (t ** 1.55)
            p = Vector((cx, cy, t * h_max)) + inward * curve_in
            rib_pts.append(p)
            rib_rad.append(0.95 * (1.0 - 0.52 * t))
        brc.add_curved_bone(bm_p, rib_pts, rib_rad, segs=9, wobble_amp=0.10)

        # Vòng xích sắt quấn quanh chân cột & rũ xuống đất
        bm_c = bmesh.new()
        for ci in range(4):
            ca = ci * (math.pi * 0.5)
            p0 = Vector((cx + math.cos(ca) * 1.05, cy + math.sin(ca) * 1.05, 1.85))
            p1 = Vector((cx + math.cos(ca + 1.3) * 1.05, cy + math.sin(ca + 1.3) * 1.05, 1.65))
            _add_chain_strand(bm_c, p0, p1, sag=0.10, link_r=0.16)
        if idx % 2 == 1:
            _add_chain_strand(bm_c, Vector((cx, cy, 1.75)) + inward * 0.95, Vector((cx, cy, 0.12)) + inward * 2.1, sag=0.28, link_r=0.17)

        p_obj = kit.finish_object(bm_p, f"Prop_Pillar_{idx:02d}", bone_mat)
        kit.paint(p_obj, lambda p, n: brc.mix(BONE_SHADOW, BONE_IVORY, brc.clamp01((p.z - 0.5) / 8.0) * 0.75 + 0.15))

        c_obj = kit.finish_object(bm_c, f"Prop_Pillar_{idx:02d}_Chains", chain_mat)
        c_obj.parent = p_obj
        kit.paint(c_obj, lambda p, n: brc.mix(CHAIN_DARK, CHAIN_CYAN, 0.6))
        objs.append(p_obj)

    # 2. 4 bục hộp sọ & đốt sống lưng cự thú (r = 10.5..13.5 m, cao 0.9-1.4 m, mặt trên phẳng cho Perch)
    for idx, (ang_deg, rad) in enumerate(zip(PERCH_ANGLES_FROM_NORTH, PERCH_RADII), 1):
        ang = math.radians(90.0 - ang_deg)
        px, py = math.cos(ang) * rad, math.sin(ang) * rad
        top_z = 0.95 + (idx % 3) * 0.20  # 0.95m, 1.15m, 1.35m -> khớp tầm 0.9-1.4 m

        bm_perch = bmesh.new()
        brc.add_angular_stone_block(bm_perch, (px, py, top_z * 0.5), (2.4, 2.2, top_z), (0.0, 0.0, ang + 0.3), rock_seed=200 + idx * 11, chip=0.10)
        # Bờ răng cưa xương đốt sống quanh bục nhưng giữ mặt trên rộng phẳng
        brc.add_rock_cluster(bm_perch, (px, py, top_z * 0.35), (1.75, 1.65, top_z * 0.55), seed=300 + idx, count=3)

        perch_obj = kit.finish_object(bm_perch, f"Prop_Perch_{idx:02d}", bone_mat)
        kit.paint(perch_obj, lambda p, n: brc.mix(BONE_OLD, BONE_IVORY, 0.45 + 0.35 * brc.clamp01(p.z / 1.4)))
        objs.append(perch_obj)

    # 3. 6 cụm tinh thể ma trơi xanh lam/lục lạnh dọc chân tường (r = 27.5..29.0 m)
    for idx, ang_deg in enumerate(CRYSTAL_ANGLES_FROM_NORTH, 1):
        ang = math.radians(90.0 - ang_deg)
        rad = 28.0 + (idx % 2) * 0.8
        cx, cy = math.cos(ang) * rad, math.sin(ang) * rad
        inward = Vector((-math.cos(ang), -math.sin(ang), 0.0)).normalized()

        bm_cr = bmesh.new()
        for k in range(5):
            off = Vector((math.cos(k * 1.3) * 0.85, math.sin(k * 1.3) * 0.85, 0.0))
            dir_v = (inward * 0.38 + Vector((0.0, 0.0, 0.92)) + Vector((math.cos(k * 2.1) * 0.18, math.sin(k * 2.1) * 0.18, 0.0))).normalized()
            brc.add_hex_crystal(bm_cr, Vector((cx, cy, 0.0)) + off, dir_v, rng.uniform(2.4, 4.4), rng.uniform(0.32, 0.55), seed=idx * 10 + k)

        cr_obj = kit.finish_object(bm_cr, f"Prop_Crystal_{idx:02d}_Glow", glow_mat)
        kit.paint(cr_obj, lambda p, n: brc.mix(ECTO_CYAN, ECTO_GREEN, brc.clamp01(p.z / 4.0)))
        objs.append(cr_obj)

    return objs


def _build_story_details_and_boss_nest(rng, stone_mat, bone_mat, chain_mat, glow_mat):
    objs = []

    # 1. Ổ Boss_Wait (0, 21, 0): Khung xương lồng ngực & hộp sọ cự thú khổng lồ nửa vùi,
    #    cùng cọc trụ đá cổ bị giật gãy và đoạn xích đen-xanh lạnh đứt lìa
    bm_nest = bmesh.new()
    bm_nest_chain = bmesh.new()
    bx, by, _ = BOSS_WAIT_POS

    # Đệm bùn hóa thạch thấp phẳng để Boss_Wait đứng vững
    brc.add_angular_stone_block(bm_nest, (bx, by, 0.02), (7.5, 6.2, 0.16), (0.0, 0.0, 0.0), rock_seed=401, chip=0.06)

    # Các cung xương sườn khổng lồ nửa vùi vươn lên phía sau và hai bên Boss_Wait (tránh chặn mặt trước)
    for side in (-1.0, 1.0):
        for ri in range(4):
            ry = by + 1.8 + ri * 1.45
            rib_pts = [
                (bx + side * 4.2, ry, 0.0),
                (bx + side * 4.8, ry + 0.3, 2.4),
                (bx + side * 3.8, ry + 0.5, 5.2),
                (bx + side * 1.4, ry + 0.6, 6.8),
            ]
            brc.add_curved_bone(bm_nest, rib_pts, [0.55, 0.44, 0.34, 0.22], segs=8, wobble_amp=0.08)

    # Cọc trụ đá cổ bị giật gãy bên trái ổ & sợi xích đứt vắt ra sàn
    post_pos = Vector((bx - 5.2, by - 1.2, 0.0))
    brc.add_angular_stone_block(bm_nest, post_pos + Vector((0.0, 0.0, 1.25)), (1.35, 1.35, 2.7), (0.12, -0.18, 0.3), rock_seed=412, chip=0.16)
    _add_chain_strand(bm_nest_chain, post_pos + Vector((0.4, 0.0, 1.8)), Vector((bx - 2.2, by - 2.6, 0.10)), sag=0.35, link_r=0.20)

    nest_obj = kit.finish_object(bm_nest, "Boss_Nest_Ribcage", bone_mat)
    kit.paint(nest_obj, lambda p, n: brc.mix(BONE_SHADOW, BONE_IVORY, brc.clamp01(p.z / 5.5) * 0.75 + 0.15))
    objs.append(nest_obj)

    nest_chain_obj = kit.finish_object(bm_nest_chain, "Boss_Nest_BrokenChain", chain_mat)
    kit.paint(nest_chain_obj, lambda p, n: brc.mix(CHAIN_DARK, CHAIN_CYAN, 0.55))
    objs.append(nest_chain_obj)

    # 2. Dấu vết Vồ & Cào Xé: các rãnh móng cào 4 đường & vết trượt dài sát sàn (<= 0.03 m, không collider)
    bm_claws = bmesh.new()
    claw_patches = [
        ((0.0, 4.0), 0.12, 6.2),
        ((-5.5, -4.5), -0.45, 5.4),
        ((6.0, -8.5), 0.38, 5.8),
    ]
    for (cx, cy), yaw, length in claw_patches:
        mat_c = Euler((0.0, 0.0, yaw), "XYZ").to_matrix().to_4x4()
        for g_idx in range(4):
            gx = (g_idx - 1.5) * 0.48
            pts = [
                Vector((cx, cy, 0.02)) + (mat_c @ Vector((gx, -length * 0.5, 0.0))),
                Vector((cx, cy, 0.02)) + (mat_c @ Vector((gx, 0.0, 0.0))),
                Vector((cx, cy, 0.02)) + (mat_c @ Vector((gx, length * 0.5, 0.0))),
            ]
            rgs = brc.parallel_tube_rings(bm_claws, pts, [0.025, 0.030, 0.020], 4)
            kit.bridge_rings(bm_claws, rgs)
            kit.cap_ring(bm_claws, rgs[0], flip=True)
            kit.cap_ring(bm_claws, rgs[-1])

    claw_obj = kit.finish_object(bm_claws, "Story_HoundClawMarks", stone_mat)
    kit.paint(claw_obj, lambda p, n: MUD_DEEP)
    objs.append(claw_obj)

    # 3. Dấu vết Triệu Hồi (3 chó con chui đất): 5 ổ đất nứt có xương trồi sát sàn ở r = 6..10 m (<= 0.05 m)
    bm_burrows = bmesh.new()
    bm_burrow_glow = bmesh.new()
    for bi in range(5):
        ang = math.radians(35.0 + bi * 68.0)
        rad = 6.8 + (bi % 2) * 2.4
        ux, uy = math.cos(ang) * rad, math.sin(ang) * rad
        # Gờ đất nứt thấp <= 0.05 m
        for seg in range(7):
            sa = 2.0 * math.pi * seg / 7.0
            sx = ux + math.cos(sa) * 1.15
            sy = uy + math.sin(sa) * 1.15
            brc.add_angular_stone_block(bm_burrows, (sx, sy, 0.02), (0.65, 0.42, 0.05), (0.0, 0.0, sa), rock_seed=500 + bi * 10 + seg, chip=0.10)
        # Mảnh tinh thể ma trơi nhỏ báo hiệu ổ triệu hồi
        brc.add_hex_crystal(bm_burrow_glow, Vector((ux, uy, 0.01)), Vector((0.1, 0.1, 1.0)).normalized(), 0.05, 0.18, seed=bi)

    burrow_obj = kit.finish_object(bm_burrows, "Story_HoundSummonBurrows", stone_mat)
    kit.paint(burrow_obj, lambda p, n: MUD_ASH)
    objs.append(burrow_obj)

    bglow_obj = kit.finish_object(bm_burrow_glow, "Story_HoundSummonGlow", glow_mat)
    kit.paint(bglow_obj, lambda p, n: ECTO_CYAN)
    objs.append(bglow_obj)

    # 4. Đống xương vụn Phun Xương & mảnh đá ăn mòn axit sát sàn (<= 0.05 m)
    bm_splinters = bmesh.new()
    for si in range(18):
        ang = rng.uniform(0.0, 2.0 * math.pi)
        rad = rng.uniform(4.5, 22.0)
        sx, sy = math.cos(ang) * rad, math.sin(ang) * rad
        brc.add_angular_stone_block(bm_splinters, (sx, sy, 0.025), (rng.uniform(0.35, 0.75), rng.uniform(0.12, 0.22), 0.05), (0.0, 0.0, rng.uniform(0.0, 6.28)), rock_seed=600 + si, chip=0.12)

    splint_obj = kit.finish_object(bm_splinters, "Story_BoneSplinters", bone_mat)
    kit.paint(splint_obj, lambda p, n: BONE_OLD)
    objs.append(splint_obj)

    return objs


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=None)
    ap.add_argument("--previews", default=None)
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    args = ap.parse_args(argv)

    t0 = time.time()
    rng = random.Random(20261123)
    kit.new_scene()

    stone_mat = brc.make_stone_material("HoundRoom_StoneMat", roughness=0.88, bump_strength=0.70)
    bone_mat = brc.make_stone_material("HoundRoom_BoneMat", roughness=0.72, bump_strength=0.45)
    glow_mat = brc.make_glow_material("HoundRoom_GlowMat", energy=1.05)
    chain_mat = brc.make_steel_material("HoundRoom_ChainMat")

    print("[Hound 1/3] Dựng vỏ phòng 64m, trần 33.5m, vòm Hàm & Xương Đùi Cự Thú...", flush=True)
    shell_obj, col_obj = brc.build_watertight_shell(
        "BossRoom_Hound", stone_mat, _floor_z, _wall_disp, _paint_shell,
        wall_top_z=WALL_TOP_Z, dome_peak_z=DOME_PEAK_Z,
    )
    arch_objs = _build_arch_and_ceiling_bones(rng, bone_mat, stone_mat, glow_mat, chain_mat)

    print("[Hound 2/3] Dựng 6 cột xương sườn, 4 bục Perch, 6 cụm ma trơi...", flush=True)
    prop_objs = _build_pillars_perches_and_crystals(rng, bone_mat, glow_mat, chain_mat)

    print("[Hound 3/3] Dựng ổ Boss_Wait, vết cào xé, 5 ổ triệu hồi & xương vụn...", flush=True)
    story_objs = _build_story_details_and_boss_nest(rng, stone_mat, bone_mat, chain_mat, glow_mat)

    if args.out:
        out_dir = os.path.abspath(args.out)
        room_objs = [shell_obj, col_obj] + arch_objs + prop_objs + story_objs
        kit.export_fbx(room_objs, os.path.join(out_dir, "BossRoom_Hound.fbx"))

    if args.previews:
        prev_dir = os.path.abspath(args.previews)
        os.makedirs(prev_dir, exist_ok=True)
        door_obj = brc.build_steel_door("BossRoom_SteelDoor")
        door_obj.location = Vector((0.0, brc.DOOR_Y, 4.4))

        lights = [
            ("dome_main", (0.0, 0.0, 25.0), (0.45, 0.85, 0.78), 5200.0, 10.0),
            ("dome_south", (0.0, -18.0, 15.0), (0.35, 0.78, 0.72), 2800.0, 6.0),
            ("dome_north", (0.0, 18.0, 15.0), (0.38, 0.86, 0.65), 3200.0, 6.0),
            ("door_l", (-3.5, -27.5, 4.2), (0.20, 0.90, 0.78), 1100.0, 2.0),
            ("door_r", (3.5, -27.5, 4.2), (0.20, 0.90, 0.78), 1100.0, 2.0),
            ("boss_nest", (0.0, 18.0, 5.0), (0.25, 0.92, 0.68), 2000.0, 2.8),
        ]
        cam, scene = brc.setup_cycles_scene((0.008, 0.015, 0.016), 0.35, lights)
        brc.render_view(cam, scene, (0.0, -25.8, 2.4), (0.0, 10.0, 2.5), 18.0,
                        os.path.join(prev_dir, "boss_room_hound_entry_view.png"))
        brc.render_view(cam, scene, (3.6, -14.5, 3.0), (-1.5, -29.0, 3.2), 19.0,
                        os.path.join(prev_dir, "boss_room_hound_door_view.png"))
        brc.render_view(cam, scene, (8.0, 9.0, 3.6), (0.0, 20.5, 2.2), 21.0,
                        os.path.join(prev_dir, "boss_room_hound_nest_view.png"))
        brc.render_view(cam, scene, (2.2, -25.0, 21.0), (0.0, 4.0, 1.0), 15.0,
                        os.path.join(prev_dir, "boss_room_hound_overview.png"))

    print(f"Hoàn tất Phòng Chó Xương trong {time.time() - t0:.1f}s", flush=True)


if __name__ == "__main__":
    main()
