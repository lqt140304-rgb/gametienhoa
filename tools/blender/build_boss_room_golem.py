"""Dựng Phòng Trùm 1: Golem Thạch Anh ("Lò Tinh Thể") — Blender headless.

Tuân thủ docs/BOSS_ROOMS_BRIEF.md và docs/lore/golem-thach-anh.md (Lore cách A: không nói ai làm cửa và rìu):
- Sàn phẳng bán kính 32 m (đường kính 64 m), trần kín 34.5 m (không lỗ).
- Cửa vào duy nhất phía Nam (4.8 x 7.2 m), khung vòm đá khảm tinh thể sáng ấm, vết trầy sâu trước cửa.
- Chi tiết kể chuyện môi trường: đường ray & xe goòng gỉ đứt ngang chở tinh thể cắt vuông;
  khối đá lõi phẳng giữa phòng có mạng nhện vết nứt phát sáng (rìu GolemAxe_Planted đặt ở (0,0,0.06), không bậc thang);
  6 cột đá (nứt chân quấn xích gỉ, 1 cột đổ nghiêng, 1 cột gãy đôi);
  8 cụm tinh thể tường (vài cụm đập vỡ rải vụn); 3 tượng đá nhỏ chưa thành hình nửa chìm vào sàn;
  nhũ đá trần gãy nham nhở; đài đá Boss_Wait phía Bắc (0, 23, 0).

Chạy:
  D:/blender/blender.exe -b -P tools/blender/build_boss_room_golem.py -- --out Assets/_Project/Models/Underroot --previews docs/art/previews
"""
import argparse
import math
import os
import random
import sys
import time

import bpy  # phải đứng trước bmesh/mathutils
import bmesh
from mathutils import Vector, Matrix, Euler, noise

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_common as kit  # noqa: E402
import boss_room_common as brc  # noqa: E402


WALL_TOP_Z = 25.5
DOME_PEAK_Z = 34.5
BOSS_WAIT_POS = (0.0, 23.0, 0.0)
PILLAR_ANGLES_FROM_NORTH = [35.0, 85.0, 135.0, 225.0, 275.0, 325.0]
PILLAR_RADII = [16.5, 16.0, 17.2, 16.8, 16.0, 16.5]
CRYSTAL_ANGLES_FROM_NORTH = [20.0, 58.0, 108.0, 148.0, 212.0, 252.0, 302.0, 340.0]

# Bảng màu Lore: xám thạch anh, vàng cam ấm của lõi, đỏ gỉ của thép
QUARTZ_GREY_DEEP = (0.022, 0.022, 0.025)
QUARTZ_GREY_DARK = (0.048, 0.047, 0.052)
QUARTZ_GREY_MID = (0.092, 0.088, 0.094)
QUARTZ_GREY_LIGHT = (0.145, 0.135, 0.138)
SCORCH_DARK = (0.015, 0.012, 0.011)

CORE_EMBER = (0.72, 0.12, 0.006)
CORE_ORANGE = (0.98, 0.30, 0.015)
CORE_AMBER = (1.00, 0.50, 0.05)
CORE_GOLD = (1.00, 0.72, 0.14)

STEEL_DARK = (0.04, 0.04, 0.045)
RUST_DARK = (0.14, 0.045, 0.016)
RUST_RED = (0.28, 0.082, 0.024)


def _floor_z(x, y):
    """Sàn gần phẳng tuyệt đối (y ~ 0, dao động <= 0.05 m ở rìa, phẳng ở tâm, trước cửa và Boss_Wait)."""
    r = math.hypot(x, y)
    d_door = math.hypot(x, y - brc.DOOR_Y)
    d_boss = math.hypot(x - BOSS_WAIT_POS[0], y - BOSS_WAIT_POS[1])
    flat_mask = (
        brc.smoothstep(3.0, 6.5, r)
        * brc.smoothstep(4.5, 9.0, d_door)
        * brc.smoothstep(4.0, 7.5, d_boss)
    )
    if r > brc.ROOM_RADIUS:
        flat_mask *= 1.0 - brc.smoothstep(brc.ROOM_RADIUS, brc.ROOM_RADIUS + 2.5, r)
    n = 0.035 * math.sin(x * 0.22 + 0.4) * math.cos(y * 0.24 - 0.3) + 0.025 * noise.noise(Vector((x * 0.14, y * 0.14, 1.7)))
    return n * flat_mask


def _wall_disp(px, py, z_base, r_scale, li):
    rock_n = brc.stepped(0.5 + 0.5 * noise.noise(Vector((px * 0.08, py * 0.08, z_base * 0.11))), steps=4.0, sharp=0.72) - 0.5
    crag = (brc.ridged(Vector((px * 0.07 + 2.1, py * 0.07, z_base * 0.10))) - 0.5) * 2.0
    r_disp = (rock_n * 1.9 + crag) * (0.4 if r_scale < 0.7 else 1.0)
    z_disp = noise.noise(Vector((px * 0.09, py * 0.09, 5.2))) * 1.4 * r_scale if li > 9 else 0.0
    return r_disp, z_disp


def _paint_shell(pos, _n):
    r = math.hypot(pos.x, pos.y)
    n_c = noise.noise(Vector((pos.x * 0.14, pos.y * 0.14, pos.z * 0.14))) * 0.5 + 0.5
    n_f = brc.ridged(Vector((pos.x * 0.35, pos.y * 0.35, pos.z * 0.35)))

    if pos.z < 0.45 and r < brc.ROOM_RADIUS + 0.5:
        c = brc.mix(QUARTZ_GREY_DARK, QUARTZ_GREY_MID, n_c * 0.65)
        # Vết cháy sém nhạt quanh trung tâm (chiến đấu Giáng Thạch)
        if 2.2 < r < 11.0:
            scorch = (1.0 - brc.smoothstep(2.2, 11.0, r)) * (0.35 + 0.35 * n_f)
            c = brc.mix(c, SCORCH_DARK, scorch * 0.55)
        # Vết nứt tỏa & rãnh kéo rìu trên sàn
        ang = math.atan2(pos.y, pos.x)
        crack_spoke = max(0.0, abs(math.sin(ang * 7.0 + n_c * 0.8)) - 0.94) / 0.06
        if 1.5 < r < 14.0 and crack_spoke > 0.0:
            c = brc.mix(c, SCORCH_DARK, crack_spoke * 0.65)
        # Vết trầy sâu trước cửa thép (ngay ngưỡng cửa Nam)
        if abs(pos.x) < 2.6 and -32.2 < pos.y < -27.0:
            gouge = max(0.0, math.sin(pos.x * 9.0) * 0.5 + 0.5)
            c = brc.mix(c, RUST_DARK, 0.25 * gouge)
        return c

    h_t = brc.clamp01(pos.z / DOME_PEAK_Z)
    c = brc.mix(QUARTZ_GREY_MID, QUARTZ_GREY_DEEP, h_t * 0.85)
    c = brc.mix(c, QUARTZ_GREY_LIGHT, n_f * 0.34 * (1.0 - h_t * 0.6))
    return c


def _build_arch_ribs_and_stalactites(rng, stone_mat, glow_mat):
    bm_arch_stone = bmesh.new()
    bm_arch_glow = bmesh.new()

    # 1. Khung vòm đá cổ khảm tinh thể vàng cam quanh lối vào Nam
    for side in (-1.0, 1.0):
        px = side * 3.35
        brc.add_angular_stone_block(bm_arch_stone, (px, brc.DOOR_Y + 0.75, 0.55), (2.7, 2.9, 1.1), (0.0, 0.0, side * 0.06), rock_seed=41 + int(side + 2), chip=0.12)
        for tier_i, (tz, hh, wx, wy) in enumerate(((2.3, 2.5, 2.0, 2.4), (4.7, 2.3, 1.85, 2.25), (6.9, 2.0, 2.1, 2.45))):
            brc.add_angular_stone_block(bm_arch_stone, (px, brc.DOOR_Y + 0.60, tz), (wx, wy, hh), (0.0, side * 0.03, side * 0.08), rock_seed=11 + tier_i * 7 + int(side + 2), chip=0.14)
        brc.add_angular_stone_block(bm_arch_stone, (side * 5.2, brc.DOOR_Y + 0.4, 5.5), (2.6, 2.4, 11.5), (0.04, -side * 0.08, side * 0.18), rock_seed=71 + int(side + 2), chip=0.18)

        for k in range(3):
            base_c = Vector((px - side * 0.35, brc.DOOR_Y + 1.45, 2.1 + k * 2.2))
            dir_c = Vector((-side * 0.32, 0.72, 0.62)).normalized()
            brc.add_hex_crystal(bm_arch_glow, base_c, dir_c, 1.75 + k * 0.25, 0.25 - k * 0.02, seed=k * 5 + int(side + 2))

    n_voussoirs = 9
    for vi in range(n_voussoirs):
        t = vi / (n_voussoirs - 1)
        ang = math.pi * (0.10 + 0.80 * t)
        vx = math.cos(ang) * 3.25
        vz = 6.95 + math.sin(ang) * 2.85
        is_key = (vi == n_voussoirs // 2)
        brc.add_angular_stone_block(
            bm_arch_stone,
            (vx, brc.DOOR_Y + 0.65, vz),
            (1.65 if is_key else 1.25, 2.55 if is_key else 2.25, 2.15 if is_key else 1.65),
            (0.0, ang - math.pi * 0.5, 0.0),
            rock_seed=100 + vi * 13,
            chip=0.12,
        )
        if vi % 2 == 1 or is_key:
            c_dir = Vector((math.cos(ang) * 0.38, 0.68, math.sin(ang) * 0.62)).normalized()
            brc.add_hex_crystal(bm_arch_glow, Vector((vx, brc.DOOR_Y + 1.35, vz + 0.15)), c_dir, 2.3 if is_key else 1.55, 0.30 if is_key else 0.20, seed=vi * 9)

    for bi, (bx, bz, sx, sy, sz, rz) in enumerate((
        (0.0, 11.4, 7.6, 2.5, 3.4, 0.0),
        (-2.2, 13.8, 5.2, 2.3, 3.6, 0.10),
        (2.2, 13.8, 5.2, 2.3, 3.6, -0.10),
        (0.0, 15.8, 5.6, 2.1, 3.8, 0.0),
    )):
        brc.add_angular_stone_block(bm_arch_stone, (bx, brc.DOOR_Y + 0.85, bz), (sx, sy, sz), (0.06, rz, 0.0), rock_seed=220 + bi * 11, chip=0.14)

    brc.add_hex_crystal(bm_arch_glow, Vector((0.0, brc.DOOR_Y + 1.75, 11.2)), Vector((0.0, 0.55, 0.85)), 3.2, 0.42, seed=901)
    brc.add_hex_crystal(bm_arch_glow, Vector((-1.1, brc.DOOR_Y + 1.65, 10.6)), Vector((-0.3, 0.55, 0.8)), 2.2, 0.30, seed=902)
    brc.add_hex_crystal(bm_arch_glow, Vector((1.1, brc.DOOR_Y + 1.65, 10.6)), Vector((0.3, 0.55, 0.8)), 2.2, 0.30, seed=903)

    arch_stone_obj = kit.finish_object(bm_arch_stone, "Door_StoneArch", stone_mat)
    for p in arch_stone_obj.data.polygons:
        p.use_smooth = False
    kit.paint(arch_stone_obj, lambda p, n: brc.mix(QUARTZ_GREY_DEEP, QUARTZ_GREY_LIGHT, 0.28 + 0.45 * brc.ridged(p * 0.55)))

    arch_glow_obj = kit.finish_object(bm_arch_glow, "Door_StoneArch_Glow", glow_mat)
    for p in arch_glow_obj.data.polygons:
        p.use_smooth = False
    kit.paint(arch_glow_obj, lambda p, n: brc.mix(CORE_EMBER, CORE_GOLD, brc.clamp01((p.z - 1.5) / 9.0)))

    # 2. Gân xương sườn hầm + Nhũ đá trần gãy nham nhở + Khe sáng ấm trên trần (kín không thủng)
    bm_ribs = bmesh.new()
    bm_veins = bmesh.new()
    rib_angles_deg = [24.0, 62.0, 102.0, 146.0, 214.0, 258.0, 298.0, 336.0]
    for ri, adeg in enumerate(rib_angles_deg):
        rad = math.radians(adeg)
        dx, dy = math.sin(rad), math.cos(rad)
        pts, rads = [], []
        for s in range(11):
            t = s / 10.0
            rr = brc.ROOM_RADIUS * (1.0 - 0.88 * (t ** 1.55)) + 0.35 * math.sin(t * math.pi)
            zz = -0.4 + t * (DOME_PEAK_Z - 0.5) + 4.8 * math.sin(t * math.pi * 0.85)
            pts.append(Vector((dx * rr, dy * rr, min(DOME_PEAK_Z - 0.4, zz))))
            rads.append(1.75 - 0.75 * t + 0.18 * math.sin(s * 1.6 + ri))
        rgs = brc.parallel_tube_rings(bm_ribs, pts, rads, 7, wobble=lambda p, a, idx: 0.15 * math.sin(a * 3.0 + p.z * 0.35 + idx))
        kit.bridge_rings(bm_ribs, rgs)
        kit.cap_ring(bm_ribs, rgs[0], flip=True)
        kit.cap_ring(bm_ribs, rgs[-1])

        for ci in range(2, 9, 2):
            anchor = pts[ci]
            inward = Vector((-dx * 0.85, -dy * 0.85, -0.42 if ci > 5 else 0.35)).normalized()
            for j in range(2):
                jitter_d = (inward + Vector((rng.uniform(-0.28, 0.28), rng.uniform(-0.28, 0.28), rng.uniform(-0.22, 0.25)))).normalized()
                brc.add_hex_crystal(bm_veins, anchor + inward * 0.45, jitter_d, rng.uniform(2.2, 4.5), rng.uniform(0.32, 0.58), seed=ri * 20 + ci + j)

    # Nhũ đá thạch anh treo trần (gãy nham nhở ở giữa chỗ Golem nhảy Giáng Thạch)
    for si in range(18):
        ang = si * (2.0 * math.pi / 18.0) + rng.uniform(-0.1, 0.1)
        sr = rng.uniform(5.0, 23.0)
        sx, sy = math.cos(ang) * sr, math.sin(ang) * sr
        sz = DOME_PEAK_Z - 1.2 - (sr / brc.ROOM_RADIUS) ** 2 * 6.5
        is_broken = sr < 13.0
        slen = rng.uniform(1.4, 2.5) if is_broken else rng.uniform(2.8, 5.2)
        brc.add_hex_crystal(bm_ribs, Vector((sx, sy, sz + 0.6)), Vector((rng.uniform(-0.1, 0.1), rng.uniform(-0.1, 0.1), -1.0)), slen, rng.uniform(0.45, 0.85), seed=700 + si)
        if si % 2 == 0:
            brc.add_hex_crystal(bm_veins, Vector((sx, sy, sz + 0.4)), Vector((rng.uniform(-0.25, 0.25), rng.uniform(-0.25, 0.25), -0.95)), slen * 0.72, 0.24, seed=800 + si)

    ribs_obj = kit.finish_object(bm_ribs, "Cavern_StoneRibs", stone_mat)
    for p in ribs_obj.data.polygons:
        p.use_smooth = False
    kit.paint(ribs_obj, lambda p, n: brc.mix(QUARTZ_GREY_DEEP, QUARTZ_GREY_LIGHT, 0.25 * (1.0 - brc.clamp01(p.z / DOME_PEAK_Z)) + 0.25 * brc.ridged(p * 0.25)))

    veins_obj = kit.finish_object(bm_veins, "Cavern_QuartzVeins_Glow", glow_mat)
    for p in veins_obj.data.polygons:
        p.use_smooth = False
    kit.paint(veins_obj, lambda p, n: brc.mix(CORE_ORANGE, CORE_GOLD, brc.clamp01(0.4 + 0.5 * noise.noise(p * 0.35))))

    return [arch_stone_obj, arch_glow_obj, ribs_obj, veins_obj]


def _build_pillars_and_crystals(rng, stone_mat, glow_mat, steel_mat):
    """6 Cột đá Prop_Pillar_01..06 (xích gỉ quấn, 1 đổ nghiêng, 1 gãy đôi) & 8 cụm Prop_Crystal_01..08 (vài cụm đập vỡ)."""
    objs = []

    for idx, (adeg, prad) in enumerate(zip(PILLAR_ANGLES_FROM_NORTH, PILLAR_RADII), start=1):
        rad = math.radians(adeg)
        px = math.sin(rad) * prad
        py = math.cos(rad) * prad
        pz = _floor_z(px, py)

        bm_p = bmesh.new()
        bm_pg = bmesh.new()
        bm_chain = bmesh.new()

        is_leaning = (idx == 3)
        is_broken = (idx == 4)

        if is_broken:
            # Cột gãy đôi: gốc cột nham nhở cao 4.8 m tại chỗ + khúc gãy nằm đổ về phía tường
            heights = [-0.3, 0.45, 1.5, 3.2, 4.8]
            radii = [1.85, 1.62, 1.44, 1.38, 1.15]
            tilt_mat = Euler((0.04, -0.06, 0.2), "XYZ").to_matrix().to_4x4()
        else:
            heights = [-0.3, 0.45, 1.5, 4.5, 9.0, 14.5, 20.0, 26.0, 33.5]
            radii = [1.85, 1.62, 1.42, 1.36, 1.32, 1.38, 1.55, 1.95, 2.70]
            tilt_mat = Euler((math.radians(13.0), math.radians(-8.0), 0.0), "XYZ").to_matrix().to_4x4() if is_leaning else Matrix.Identity(4)

        rings = []
        n_segs = 10
        for zi, (zz, rr) in enumerate(zip(heights, radii)):
            ring = []
            for k in range(n_segs):
                a = 2.0 * math.pi * k / n_segs
                w = 1.0 + 0.12 * math.sin(a * 3.0 + zi * 1.4 + idx) + 0.09 * noise.noise(Vector((math.cos(a) + idx, math.sin(a), zz * 0.2)))
                if is_broken and zi == len(heights) - 1:
                    zz_local = zz + 0.45 * math.sin(a * 2.0 + 0.7)
                else:
                    zz_local = zz
                ring.append(bm_p.verts.new(Vector((math.cos(a) * rr * w, math.sin(a) * rr * w, zz_local))))
            rings.append(ring)
        kit.bridge_rings(bm_p, rings)
        kit.cap_ring(bm_p, rings[0], flip=True)
        kit.cap_ring(bm_p, rings[-1])

        # Nếu gãy đôi, thêm đoạn thân cột gãy nằm đổ dài ra ngoài phía tường
        if is_broken:
            fallen_pts = [Vector((-2.2 - i * 1.8, -1.6 - i * 1.4, 1.15)) for i in range(5)]
            fallen_r = [1.32, 1.35, 1.36, 1.30, 1.18]
            frgs = brc.parallel_tube_rings(bm_p, fallen_pts, fallen_r, 9, wobble=lambda p, a, i: 0.10 * math.sin(a * 3.0 + i))
            kit.bridge_rings(bm_p, frgs)
            kit.cap_ring(bm_p, frgs[0], flip=True)
            kit.cap_ring(bm_p, frgs[-1])

        # Vòng xích gỉ quấn quanh chân cột
        for cz in (1.35, 2.05):
            chain_pts = [Vector((math.cos(a) * 1.48, math.sin(a) * 1.48, cz + 0.06 * math.sin(a * 3.0))) for a in [2 * math.pi * k / 16 for k in range(17)]]
            crgs = brc.parallel_tube_rings(bm_chain, chain_pts, [0.11] * len(chain_pts), 6)
            kit.bridge_rings(bm_chain, crgs)

        # Tinh thể mọc từ khe nứt cột
        c_count = 2 if is_broken else 4
        for ci in range(c_count):
            ca = idx * 1.1 + ci * (math.pi * 0.5)
            base_c = Vector((math.cos(ca) * 1.18, math.sin(ca) * 1.18, 1.6 + ci * 1.2))
            dir_c = Vector((math.cos(ca) * 0.72, math.sin(ca) * 0.72, 0.65)).normalized()
            brc.add_hex_crystal(bm_pg, base_c, dir_c, 1.1 + (ci % 2) * 0.45, 0.21, seed=idx * 10 + ci)

        bmesh.ops.transform(bm_p, matrix=tilt_mat, verts=bm_p.verts[:])
        bmesh.ops.transform(bm_pg, matrix=tilt_mat, verts=bm_pg.verts[:])
        bmesh.ops.transform(bm_chain, matrix=tilt_mat, verts=bm_chain.verts[:])

        pname = f"Prop_Pillar_{idx:02d}"
        p_obj = kit.finish_object(bm_p, pname, stone_mat)
        p_obj.location = Vector((px, py, pz))
        for p in p_obj.data.polygons:
            p.use_smooth = False
        kit.paint(p_obj, lambda p, n, seed=idx: brc.mix(QUARTZ_GREY_DEEP, QUARTZ_GREY_LIGHT, 0.35 * (1.0 - brc.clamp01(p.z / 24.0)) + 0.35 * brc.ridged(p * 0.4 + Vector((seed, 0, 0)))))

        pg_obj = kit.finish_object(bm_pg, f"{pname}_Glow", glow_mat)
        pg_obj.parent = p_obj
        for p in pg_obj.data.polygons:
            p.use_smooth = False
        kit.paint(pg_obj, lambda p, n: brc.mix(CORE_ORANGE, CORE_GOLD, brc.clamp01(p.z / 7.0)))

        pchain_obj = kit.finish_object(bm_chain, f"{pname}_Chain", steel_mat)
        pchain_obj.parent = p_obj
        for p in pchain_obj.data.polygons:
            p.use_smooth = False
        kit.paint(pchain_obj, lambda p, n: brc.mix(RUST_DARK, RUST_RED, 0.55))

        objs.extend([p_obj, pg_obj, pchain_obj])

    # 8 Cụm tinh thể tường (cụm 7 & 8 bị đập vỡ rải vụn quanh chân)
    for idx, adeg in enumerate(CRYSTAL_ANGLES_FROM_NORTH, start=1):
        rad = math.radians(adeg)
        cr = 27.8 + (idx % 2) * 1.1
        cx = math.sin(rad) * cr
        cy = math.cos(rad) * cr
        cz = _floor_z(cx, cy)

        bm_base = bmesh.new()
        bm_shards = bmesh.new()
        brc.add_rock_cluster(bm_base, (0.0, 0.0, 0.40), (1.85, 1.55, 0.85), seed=300 + idx, count=3)

        inward_ang = math.atan2(-cy, -cx)
        is_shattered = idx in (4, 7)
        if is_shattered:
            # Gốc tinh thể gãy cụt + vụn tinh thể rơi trên sàn xung quanh
            for si in range(4):
                a_off = (si - 1.5) * 0.6
                dir_v = Vector((math.cos(inward_ang + a_off) * 0.35, math.sin(inward_ang + a_off) * 0.35, 0.90)).normalized()
                brc.add_hex_crystal(bm_shards, Vector((math.cos(si) * 0.5, math.sin(si) * 0.5, 0.25)), dir_v, 1.15 + 0.3 * (si % 2), 0.30, seed=idx * 20 + si)
            for di in range(6):
                da = inward_ang + rng.uniform(-0.9, 0.9)
                dd = rng.uniform(1.8, 3.6)
                brc.add_hex_crystal(bm_shards, Vector((math.cos(da) * dd, math.sin(da) * dd, 0.08)), Vector((math.cos(da + 1.2), math.sin(da + 1.2), 0.22)).normalized(), rng.uniform(0.55, 0.95), 0.14, seed=idx * 40 + di)
        else:
            for si in range(7):
                a_off = (si - 3) * 0.42
                dir_v = Vector((math.cos(inward_ang + a_off) * (0.28 + 0.10 * (si % 3)), math.sin(inward_ang + a_off) * (0.28 + 0.10 * (si % 3)), 0.88 - 0.08 * abs(si - 3))).normalized()
                brc.add_hex_crystal(bm_shards, Vector((math.cos(si * 1.3) * 0.55, math.sin(si * 1.3) * 0.55, 0.3)), dir_v, 3.2 + (si % 3) * 1.25, 0.38 - (si % 2) * 0.08, seed=idx * 17 + si)

        cname = f"Prop_Crystal_{idx:02d}"
        c_base = kit.finish_object(bm_base, cname, stone_mat)
        c_base.location = Vector((cx, cy, cz))
        for p in c_base.data.polygons:
            p.use_smooth = False
        kit.paint(c_base, lambda p, n: brc.mix(QUARTZ_GREY_DARK, QUARTZ_GREY_LIGHT, 0.45 * brc.ridged(p * 0.8)))

        c_glow = kit.finish_object(bm_shards, f"{cname}_Glow", glow_mat)
        c_glow.parent = c_base
        for p in c_glow.data.polygons:
            p.use_smooth = False
        kit.paint(c_glow, lambda p, n: brc.mix(CORE_EMBER, CORE_GOLD, brc.clamp01(p.z / 5.0)))

        objs.extend([c_base, c_glow])

    return objs


def _build_story_details_and_boss_mound(rng, stone_mat, glow_mat, steel_mat):
    """Chi tiết kể chuyện môi trường theo Lore cách A (KHÔNG bậc quanh rìu, KHÔNG chữ viết/xương người)."""
    # 1. Khối đá lõi phẳng giữa phòng tại (0, 0, 0): cao chỉ 0.06 m (mặt trên phẳng để GolemAxe_Planted cắm vào), mạng nhện vết nứt ấm rộng 5 m
    bm_core = bmesh.new()
    bm_core_glow = bmesh.new()

    segs = 12
    r_bot, r_top, h_top = 1.95, 1.45, 0.06
    bot_r = [bm_core.verts.new(Vector((math.cos(2 * math.pi * k / segs) * r_bot, math.sin(2 * math.pi * k / segs) * r_bot, -0.02))) for k in range(segs)]
    top_r = [bm_core.verts.new(Vector((math.cos(2 * math.pi * k / segs) * r_top, math.sin(2 * math.pi * k / segs) * r_top, h_top))) for k in range(segs)]
    kit.bridge_rings(bm_core, [bot_r, top_r])
    kit.cap_ring(bm_core, bot_r, flip=True)
    kit.cap_ring(bm_core, top_r)

    # Mạng nhện các khe nứt tỏa ấm từ (0, 0) ra bán kính 5 m (cao <= 0.015 m, không cản chân)
    for k in range(10):
        ang = k * (2.0 * math.pi / 10.0) + 0.17 * (k % 3)
        len_crack = 2.6 + (k % 3) * 1.0
        pts = [Vector((math.cos(ang + 0.06 * math.sin(s)) * (0.4 + s * len_crack / 4.0), math.sin(ang + 0.06 * math.sin(s)) * (0.4 + s * len_crack / 4.0), 0.015 if s < 2 else 0.008)) for s in range(5)]
        crgs = brc.parallel_tube_rings(bm_core_glow, pts, [0.07, 0.055, 0.04, 0.025, 0.01], 4)
        kit.bridge_rings(bm_core_glow, crgs)
        kit.cap_ring(bm_core_glow, crgs[-1])

    # Vảy tinh thể nhỏ dẹt quanh khối đá lõi (cao <= 0.05 m)
    for k in range(14):
        ang = k * (2.0 * math.pi / 14.0)
        dist = rng.uniform(1.6, 3.8)
        brc.add_hex_crystal(bm_core_glow, Vector((math.cos(ang) * dist, math.sin(ang) * dist, 0.0)), Vector((math.cos(ang + 0.8), math.sin(ang + 0.8), 0.15)).normalized(), rng.uniform(0.28, 0.55), 0.06, seed=900 + k)

    core_obj = kit.finish_object(bm_core, "Axe_CoreStone", stone_mat)
    for p in core_obj.data.polygons:
        p.use_smooth = False
    kit.paint(core_obj, lambda p, n: brc.mix(QUARTZ_GREY_DARK, CORE_EMBER, 0.18 if p.z > 0.03 else 0.05))

    core_glow = kit.finish_object(bm_core_glow, "Axe_CoreCracks_Glow", glow_mat)
    for p in core_glow.data.polygons:
        p.use_smooth = False
    kit.paint(core_glow, lambda p, n: brc.mix(CORE_EMBER, CORE_AMBER, brc.clamp01(1.0 - math.hypot(p.x, p.y) / 5.2)))

    # 2. Đường ray gỉ & xe goòng đổ nghiêng (đứt ngang ở y = -15.5 m, xe goòng đặt gọn bên trái lối vào tại (-8.2, -22.5))
    bm_rail = bmesh.new()
    bm_cart_ore = bmesh.new()
    rail_pts = [Vector((-1.3 - (t ** 1.6) * 4.2, -31.0 + t * 15.5, 0.025)) for t in [i / 8.0 for i in range(9)]]
    for i in range(len(rail_pts) - 1):
        p0, p1 = rail_pts[i], rail_pts[i + 1]
        d = (p1 - p0).normalized()
        perp = Vector((-d.y, d.x, 0.0))
        seg_len = (p1 - p0).length
        ang_z = math.atan2(d.y, d.x)
        # Hai thanh ray song song (cao 0.04 m sát sàn)
        for off in (-0.42, 0.42):
            mid = (p0 + p1) * 0.5 + perp * off
            m = Matrix.Translation(mid) @ Euler((0.0, 0.0, ang_z), "XYZ").to_matrix().to_4x4() @ Matrix.Diagonal((seg_len * 1.02, 0.08, 0.04, 1.0))
            bmesh.ops.create_cube(bm_rail, size=1.0, matrix=m)
        # Tà vẹt gỗ hóa đá / sắt
        m_tie = Matrix.Translation((p0 + p1) * 0.5) @ Euler((0.0, 0.0, ang_z), "XYZ").to_matrix().to_4x4() @ Matrix.Diagonal((0.14, 1.15, 0.025, 1.0))
        bmesh.ops.create_cube(bm_rail, size=1.0, matrix=m_tie)

    # Xe goòng gỉ nghiêng ở (-8.2, -22.5, 0) chạm mặt sàn (không lơ lửng)
    cart_mat = Matrix.Translation((-8.2, -22.5, 0.28)) @ Euler((math.radians(22.0), math.radians(-16.0), math.radians(38.0)), "XYZ").to_matrix().to_4x4()
    for pos, sz in (
        ((0.0, 0.0, -0.35), (1.35, 0.95, 0.08)),
        ((-0.65, 0.0, 0.05), (0.08, 0.95, 0.72)),
        ((0.65, 0.0, 0.05), (0.08, 0.95, 0.72)),
        ((0.0, -0.44, 0.05), (1.35, 0.08, 0.72)),
        ((0.0, 0.44, 0.05), (1.35, 0.08, 0.72)),
    ):
        bmesh.ops.create_cube(bm_rail, size=1.0, matrix=cart_mat @ Matrix.Translation(pos) @ Matrix.Diagonal((*sz, 1.0)))
    # Vài mẩu tinh thể cắt vuông nằm trong xe và rơi nằm sát đất bên cạnh
    for ci, cp in enumerate(((0.1, 0.0, -0.10), (-0.25, 0.15, -0.05))):
        bmesh.ops.create_cube(bm_cart_ore, size=0.34, matrix=cart_mat @ Matrix.Translation(cp) @ Euler((0.2 * ci, 0.4, 0.3 * ci), "XYZ").to_matrix().to_4x4())
    for ci, (wx, wy, wz) in enumerate(((-7.1, -21.4, 0.14), (-7.5, -21.0, 0.12))):
        bmesh.ops.create_cube(bm_cart_ore, size=0.30, matrix=Matrix.Translation((wx, wy, wz)) @ Euler((0.1, 0.2 * ci, 0.6 + ci), "XYZ").to_matrix().to_4x4())

    rail_obj = kit.finish_object(bm_rail, "Story_BrokenRailsAndCart", steel_mat)
    for p in rail_obj.data.polygons:
        p.use_smooth = False
    kit.paint(rail_obj, lambda p, n: brc.mix(STEEL_DARK, RUST_RED, 0.48 + 0.3 * brc.ridged(p * 1.4)))

    cart_ore_obj = kit.finish_object(bm_cart_ore, "Story_CartCutOre_Glow", glow_mat)
    for p in cart_ore_obj.data.polygons:
        p.use_smooth = False
    kit.paint(cart_ore_obj, lambda p, n: CORE_AMBER)

    # 3. Ba tượng Golem nhỏ thất bại nửa chìm vào sàn sát vách tường (không chữ, không xương người)
    bm_failed = bmesh.new()
    for fi, (fx, fy, yaw) in enumerate(((-25.5, 11.0, 0.8), (26.0, -7.5, -1.9), (19.0, 21.0, -2.5))):
        fmat = Matrix.Translation((fx, fy, 0.0)) @ Euler((0.0, 0.0, yaw), "XYZ").to_matrix().to_4x4()
        # Thân đá thô hình người hụt chìm nửa dưới đất
        brc.add_rock_cluster(bm_failed, (fmat @ Vector((0.0, 0.0, 0.65))), (0.95, 0.70, 0.95), seed=550 + fi, count=2)
        brc.add_rock_cluster(bm_failed, (fmat @ Vector((0.0, 0.12, 1.65))), (0.42, 0.42, 0.48), seed=560 + fi, count=1)
        brc.add_rock_cluster(bm_failed, (fmat @ Vector((-0.85, 0.05, 0.85))), (0.36, 0.36, 0.72), seed=570 + fi, count=1)
    failed_obj = kit.finish_object(bm_failed, "Story_FailedStatues", stone_mat)
    for p in failed_obj.data.polygons:
        p.use_smooth = False
    kit.paint(failed_obj, lambda p, n: brc.mix(QUARTZ_GREY_DEEP, QUARTZ_GREY_MID, 0.55))

    # 4. Khối đá lớn Boss_Wait ở phía Bắc (0, +23, 0)
    bx, by, bz = BOSS_WAIT_POS
    bm_mound = bmesh.new()
    bm_mound_glow = bmesh.new()
    brc.add_angular_stone_block(bm_mound, (0.0, 0.9, 0.08), (7.6, 6.4, 0.32), (0.0, 0.0, 0.0), rock_seed=401, chip=0.10)
    for bi, (pos, ext, rot) in enumerate((
        ((0.0, 3.2, 2.2), (4.4, 3.0, 4.8), (0.12, 0.0, 0.0)),
        ((-2.3, 2.6, 1.6), (2.8, 2.6, 3.4), (0.08, -0.15, 0.3)),
        ((2.3, 2.6, 1.6), (2.8, 2.6, 3.4), (0.08, 0.15, -0.3)),
        ((-3.6, 1.4, 0.9), (2.4, 2.2, 2.2), (0.0, -0.2, 0.5)),
        ((3.6, 1.4, 0.9), (2.4, 2.2, 2.2), (0.0, 0.2, -0.5)),
    )):
        brc.add_angular_stone_block(bm_mound, pos, ext, rot, rock_seed=410 + bi, chip=0.16)

    for ki, (kx, ky, kz, dx, dz, ln) in enumerate((
        (-1.6, 2.5, 2.6, -0.25, 0.9, 2.8),
        (1.6, 2.5, 2.6, 0.25, 0.9, 2.8),
        (0.0, 3.2, 3.7, 0.0, 0.95, 3.4),
        (-2.9, 1.8, 1.6, -0.4, 0.85, 2.0),
        (2.9, 1.8, 1.6, 0.4, 0.85, 2.0),
    )):
        brc.add_hex_crystal(bm_mound_glow, Vector((kx, ky, kz)), Vector((dx, -0.35, dz)), ln, 0.36, seed=450 + ki)

    mound_obj = kit.finish_object(bm_mound, "BossWait_Mound", stone_mat)
    mound_obj.location = Vector((bx, by, bz))
    for p in mound_obj.data.polygons:
        p.use_smooth = False
    kit.paint(mound_obj, lambda p, n: brc.mix(QUARTZ_GREY_DARK, QUARTZ_GREY_LIGHT, 0.35 + 0.4 * brc.ridged(p * 0.4)))

    mound_glow = kit.finish_object(bm_mound_glow, "BossWait_Mound_Glow", glow_mat)
    mound_glow.parent = mound_obj
    for p in mound_glow.data.polygons:
        p.use_smooth = False
    kit.paint(mound_glow, lambda p, n: brc.mix(CORE_ORANGE, CORE_GOLD, brc.clamp01((p.z - 1.5) / 5.0)))

    return [core_obj, core_glow, rail_obj, cart_ore_obj, failed_obj, mound_obj, mound_glow]


def _import_official_planted_axe_for_preview(fbx_path):
    """Chỉ dùng khi render ảnh preview Blender để kiểm chứng dáng đặt rìu GolemAxe_Planted thật."""
    if not os.path.exists(fbx_path):
        return []
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=fbx_path)
    imported = [o for o in bpy.data.objects if o not in before]
    for o in imported:
        o.location = Vector((0.0, 0.0, 0.06))
        if o.type == "MESH" and o.data and "Crystal" in o.name:
            m = brc.make_glow_material("Preview_OfficialAxeCrystalMat", energy=1.6)
            o.data.materials.clear()
            o.data.materials.append(m)
        elif o.type == "MESH" and o.data:
            m = brc.make_steel_material("Preview_OfficialAxeSteelMat")
            o.data.materials.clear()
            o.data.materials.append(m)
    return imported


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=None)
    ap.add_argument("--previews", default=None)
    ap.add_argument("--view", default="all", choices=["all", "door"])
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    args = ap.parse_args(argv)

    t0 = time.time()
    rng = random.Random(20261122)
    kit.new_scene()

    stone_mat = brc.make_stone_material("GolemRoom_StoneMat")
    glow_mat = brc.make_glow_material("GolemRoom_GlowMat", energy=0.90)
    steel_mat = brc.make_steel_material("GolemRoom_SteelMat")

    print("[Golem 1/3] Dựng vỏ phòng 64m, trần 34.5m, hốc cửa Nam 4.8x7.2m...", flush=True)
    shell_obj, col_obj = brc.build_watertight_shell(
        "BossRoom_Golem", stone_mat, _floor_z, _wall_disp, _paint_shell,
        wall_top_z=WALL_TOP_Z, dome_peak_z=DOME_PEAK_Z,
    )
    arch_objs = _build_arch_ribs_and_stalactites(rng, stone_mat, glow_mat)

    print("[Golem 2/3] Dựng 6 cột đá (xích gỉ, đổ nghiêng, gãy đôi) & 8 cụm tinh thể...", flush=True)
    prop_objs = _build_pillars_and_crystals(rng, stone_mat, glow_mat, steel_mat)

    print("[Golem 3/3] Dựng khối đá lõi giữa phòng, đường ray & xe goòng gỉ, tượng hụt & Boss_Wait...", flush=True)
    story_objs = _build_story_details_and_boss_mound(rng, stone_mat, glow_mat, steel_mat)

    print("Dựng cánh cửa thép chung BossRoom_SteelDoor...", flush=True)
    door_obj = brc.build_steel_door("BossRoom_SteelDoor")

    if args.out:
        out_dir = os.path.abspath(args.out)
        room_objs = [shell_obj, col_obj] + arch_objs + prop_objs + story_objs
        kit.export_fbx(room_objs, os.path.join(out_dir, "BossRoom_Golem.fbx"))
        door_obj.location = Vector((0.0, 0.0, 0.0))
        kit.export_fbx([door_obj], os.path.join(out_dir, "BossRoom_SteelDoor.fbx"))

    if args.previews:
        prev_dir = os.path.abspath(args.previews)
        os.makedirs(prev_dir, exist_ok=True)
        door_obj.location = Vector((0.0, brc.DOOR_Y, 4.4))
        repo_root = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
        axe_fbx = os.path.join(repo_root, "Assets", "_Project", "Models", "Props", "GolemAxe", "GolemAxe_Planted.fbx")
        _import_official_planted_axe_for_preview(axe_fbx)

        lights = [
            ("dome_main", (0.0, 0.0, 26.0), (1.00, 0.72, 0.48), 5800.0, 10.0),
            ("dome_south", (0.0, -18.0, 15.0), (1.00, 0.60, 0.25), 3200.0, 6.0),
            ("dome_north", (0.0, 18.0, 15.0), (1.00, 0.56, 0.22), 3200.0, 6.0),
            ("door_l", (-3.6, -27.5, 4.2), (1.00, 0.42, 0.08), 1100.0, 2.0),
            ("door_r", (3.6, -27.5, 4.2), (1.00, 0.42, 0.08), 1100.0, 2.0),
            ("axe_core", (0.0, 0.0, 3.0), (1.00, 0.52, 0.12), 1600.0, 1.8),
            ("boss_throne", (0.0, 20.5, 5.2), (1.00, 0.42, 0.08), 1800.0, 2.5),
            ("minecart", (-7.0, -21.0, 3.2), (1.00, 0.55, 0.20), 900.0, 1.5),
        ]
        cam, scene = brc.setup_cycles_scene((0.02, 0.012, 0.008), 0.35, lights)

        if args.view == "door":
            brc.render_view(cam, scene, (3.6, -14.5, 3.0), (-2.0, -29.0, 3.2), 19.0,
                            os.path.join(prev_dir, "boss_room_golem_door_view.png"))
        else:
            brc.render_view(cam, scene, (0.0, -25.8, 2.4), (0.0, 10.0, 2.6), 18.0,
                            os.path.join(prev_dir, "boss_room_golem_entry_view.png"))
            brc.render_view(cam, scene, (3.6, -14.5, 3.0), (-2.0, -29.0, 3.2), 19.0,
                            os.path.join(prev_dir, "boss_room_golem_door_view.png"))
            brc.render_view(cam, scene, (5.2, -7.2, 2.2), (-0.2, 1.2, 1.4), 22.0,
                            os.path.join(prev_dir, "boss_room_golem_axe_center.png"))
            brc.render_view(cam, scene, (2.2, -25.0, 21.5), (0.0, 4.0, 1.0), 15.0,
                            os.path.join(prev_dir, "boss_room_golem_overview.png"))

    print(f"Hoàn tất Phòng Golem trong {time.time() - t0:.1f}s", flush=True)


if __name__ == "__main__":
    main()
