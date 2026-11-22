"""Dựng Phòng Trùm 2: Chim Hang Mẹ ("Tổ Kền Kền") — Blender headless.

Tuân thủ docs/BOSS_ROOMS_BRIEF.md mục 4.2:
- Chủ đề: hang cao, ánh xanh ngọc lạnh, đống xương và ổ lông.
- Kích thước: sàn phẳng bán kính 32 m (đường kính 64 m), vách 29 m, trần cao nhất có thể (38.5 m ở giữa), không vật cản giữa phòng.
- Cổng vào Nam (4.8 x 7.2 m): vòm xương khổng lồ + tinh thể xanh ngọc lạnh.
- 6 ụ đá/xương cao Prop_Perch_01..06 (cao 2.6..5.8 m) ở vòng ngoài (r = 21.5..24.5 m) làm chỗ chim đậu.
- 4 cụm Prop_Crystal_01..04 sát vách, ổ lông & xương lớn Boss_Wait ở (0, 21, 0), ổ trứng/đống xương phẳng thấp giữa phòng.

Chạy:
  D:/blender/blender.exe -b -P tools/blender/build_boss_room_bird.py -- --out Assets/_Project/Models/Underroot --previews docs/art/previews
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


WALL_TOP_Z = 29.0
DOME_PEAK_Z = 38.5
BOSS_WAIT_POS = (0.0, 21.0, 0.0)

PERCH_SPECS = [
    (35.0, 22.5, 2.8),
    (80.0, 23.8, 4.2),
    (130.0, 22.0, 5.4),
    (230.0, 22.0, 3.2),
    (280.0, 23.8, 4.8),
    (325.0, 22.5, 5.8),
]
CRYSTAL_ANGLES_FROM_NORTH = [55.0, 110.0, 250.0, 305.0]

# Bảng màu hang Chim: đá phiến lạnh xám xanh + xương ngà cũ + lông sẫm + tinh thể xanh ngọc lạnh
SLATE_DEEP = (0.015, 0.022, 0.026)
SLATE_DARK = (0.036, 0.048, 0.055)
SLATE_MID = (0.070, 0.088, 0.095)
SLATE_PALE = (0.115, 0.138, 0.142)

BONE_SHADOW = (0.16, 0.145, 0.12)
BONE_IVORY = (0.42, 0.38, 0.30)
FEATHER_NEST = (0.045, 0.040, 0.048)

JADE_DEEP = (0.01, 0.36, 0.38)
JADE_BRIGHT = (0.08, 0.85, 0.78)
JADE_ICE = (0.45, 1.00, 0.94)


def _floor_z(x, y):
    r = math.hypot(x, y)
    d_door = math.hypot(x, y - brc.DOOR_Y)
    d_boss = math.hypot(x - BOSS_WAIT_POS[0], y - BOSS_WAIT_POS[1])
    flat_mask = (
        brc.smoothstep(3.0, 6.5, r)
        * brc.smoothstep(4.5, 9.0, d_door)
        * brc.smoothstep(4.5, 8.0, d_boss)
    )
    if r > brc.ROOM_RADIUS:
        flat_mask *= 1.0 - brc.smoothstep(brc.ROOM_RADIUS, brc.ROOM_RADIUS + 2.5, r)
    n = 0.03 * math.sin(x * 0.18 - 0.5) * math.cos(y * 0.20 + 0.3) + 0.02 * noise.noise(Vector((x * 0.12, y * 0.12, 3.1)))
    return n * flat_mask


def _wall_disp(px, py, z_base, r_scale, li):
    rock_n = brc.stepped(0.5 + 0.5 * noise.noise(Vector((px * 0.075, py * 0.075, z_base * 0.09))), steps=5.0, sharp=0.70) - 0.5
    crag = (brc.ridged(Vector((px * 0.065 + 4.3, py * 0.065, z_base * 0.08))) - 0.5) * 1.9
    r_disp = (rock_n * 1.8 + crag) * (0.35 if r_scale < 0.7 else 1.0)
    z_disp = noise.noise(Vector((px * 0.08, py * 0.08, 9.4))) * 1.5 * r_scale if li > 9 else 0.0
    return r_disp, z_disp


def _paint_shell(pos, _n):
    r = math.hypot(pos.x, pos.y)
    n_c = noise.noise(Vector((pos.x * 0.14, pos.y * 0.14, pos.z * 0.14))) * 0.5 + 0.5
    n_f = brc.ridged(Vector((pos.x * 0.35, pos.y * 0.35, pos.z * 0.35)))
    if pos.z < 0.45 and r < brc.ROOM_RADIUS + 0.5:
        c = brc.mix(SLATE_DARK, SLATE_MID, n_c * 0.7)
        if r < 6.0 or math.hypot(pos.x, pos.y - BOSS_WAIT_POS[1]) < 5.5:
            c = brc.mix(c, FEATHER_NEST, 0.38)
        return c
    h_t = brc.clamp01(pos.z / DOME_PEAK_Z)
    c = brc.mix(SLATE_MID, SLATE_DEEP, h_t * 0.85)
    c = brc.mix(c, SLATE_PALE, n_f * 0.32 * (1.0 - h_t * 0.5))
    return c


def _build_bone_arch_and_ribs(rng, stone_mat, glow_mat):
    """Khung vòm xương khổng lồ ở lối vào Nam + vòm sườn đá & tinh thể xanh ngọc lạnh trên cao."""
    bm_arch_stone = bmesh.new()
    bm_arch_bones = bmesh.new()
    bm_arch_glow = bmesh.new()

    # Hai trụ đá ngầm đỡ khung xương cửa vào (giấu khe hở vách)
    for side in (-1.0, 1.0):
        px = side * 3.4
        brc.add_angular_stone_block(bm_arch_stone, (px, brc.DOOR_Y + 0.65, 2.2), (2.2, 2.5, 4.6), (0.0, 0.0, side * 0.1), rock_seed=21 + int(side + 2), chip=0.16)
        brc.add_angular_stone_block(bm_arch_stone, (side * 4.8, brc.DOOR_Y + 0.4, 5.2), (2.4, 2.2, 11.0), (0.04, -side * 0.1, side * 0.2), rock_seed=31 + int(side + 2), chip=0.18)

        # 3 cặp xương sườn quái thú khổng lồ cong chụm tạo vòm xương
        for ri in range(3):
            y_off = brc.DOOR_Y + 0.45 + ri * 0.75
            pts = [
                (side * (3.2 + ri * 0.3), y_off, 0.3),
                (side * (3.6 + ri * 0.2), y_off + 0.2, 3.8),
                (side * (3.0 + ri * 0.1), y_off + 0.35, 6.8 + ri * 0.3),
                (side * 1.4, y_off + 0.25, 8.9 + ri * 0.35),
                (-side * 0.25, y_off + 0.15, 9.6 + ri * 0.25),
            ]
            rads = [0.44, 0.38, 0.32, 0.24, 0.14]
            brc.add_curved_bone(bm_arch_bones, pts, rads, segs=8)

        # Tinh thể xanh ngọc lạnh dọc vòm
        for ki in range(3):
            brc.add_hex_crystal(
                bm_arch_glow,
                Vector((px - side * 0.45, brc.DOOR_Y + 1.35, 2.5 + ki * 2.2)),
                Vector((-side * 0.35, 0.7, 0.6)).normalized(),
                1.5 + ki * 0.2,
                0.20,
                seed=ki + int(side + 2),
            )

    # Khối sọ thú cổ đại sừng dài trên đỉnh vòm cửa (tạo silhouette nhận diện ngay từ xa)
    skull_y = brc.DOOR_Y + 1.15
    brc.add_rock_cluster(bm_arch_bones, (0.0, skull_y, 9.8), (1.45, 1.35, 1.15), seed=99, count=2)
    brc.add_rock_cluster(bm_arch_bones, (0.0, skull_y + 0.9, 8.6), (0.75, 1.05, 0.55), seed=105, count=1)  # mõm sọ
    for side in (-1.0, 1.0):
        horn_pts = [
            (side * 0.95, skull_y, 10.2),
            (side * 2.25, skull_y + 0.35, 11.4),
            (side * 3.15, skull_y + 0.65, 13.0),
            (side * 3.45, skull_y + 0.40, 14.4),
        ]
        brc.add_curved_bone(bm_arch_bones, horn_pts, [0.36, 0.28, 0.18, 0.06], segs=7)

    # Dầm đá trên cao bịt rãnh kéo cửa thép
    brc.add_angular_stone_block(bm_arch_stone, (0.0, brc.DOOR_Y + 0.6, 12.6), (7.6, 2.4, 5.2), (0.05, 0.0, 0.0), rock_seed=240, chip=0.16)

    arch_stone = kit.finish_object(bm_arch_stone, "Door_BoneArch_Stone", stone_mat)
    for p in arch_stone.data.polygons:
        p.use_smooth = False
    kit.paint(arch_stone, lambda p, n: brc.mix(SLATE_DEEP, SLATE_PALE, 0.35 + 0.4 * brc.ridged(p * 0.5)))

    arch_bones = kit.finish_object(bm_arch_bones, "Door_BoneArch", stone_mat)
    for p in arch_bones.data.polygons:
        p.use_smooth = False
    kit.paint(arch_bones, lambda p, n: brc.mix(BONE_SHADOW, BONE_IVORY, 0.45 + 0.4 * brc.ridged(p * 0.6)))

    arch_glow = kit.finish_object(bm_arch_glow, "Door_BoneArch_Glow", glow_mat)
    for p in arch_glow.data.polygons:
        p.use_smooth = False
    kit.paint(arch_glow, lambda p, n: brc.mix(JADE_DEEP, JADE_ICE, brc.clamp01((p.z - 1.5) / 8.0)))

    # Gân vòm cao (trên 25 m trở lên, không bao giờ cản đường bay ở giữa) & tinh thể xanh lạnh trên cao
    bm_ribs = bmesh.new()
    bm_veins = bmesh.new()
    for ri, adeg in enumerate((25.0, 70.0, 115.0, 152.0, 208.0, 245.0, 290.0, 335.0)):
        rad = math.radians(adeg)
        dx, dy = math.sin(rad), math.cos(rad)
        pts, rads = [], []
        for s in range(11):
            t = s / 10.0
            rr = brc.ROOM_RADIUS * (1.0 - 0.88 * (t ** 1.65)) + 0.3 * math.sin(t * math.pi)
            zz = -0.4 + t * (DOME_PEAK_Z - 0.6) + 5.2 * math.sin(t * math.pi * 0.82)
            pts.append(Vector((dx * rr, dy * rr, min(DOME_PEAK_Z - 0.4, zz))))
            rads.append(1.65 - 0.70 * t + 0.16 * math.sin(s * 1.5 + ri))
        rgs = brc.parallel_tube_rings(bm_ribs, pts, rads, 7, wobble=lambda p, a, idx: 0.14 * math.sin(a * 3.0 + idx))
        kit.bridge_rings(bm_ribs, rgs)
        kit.cap_ring(bm_ribs, rgs[0], flip=True)
        kit.cap_ring(bm_ribs, rgs[-1])

        for ci in range(3, 9, 2):
            anchor = pts[ci]
            inward = Vector((-dx * 0.85, -dy * 0.85, -0.35 if ci > 6 else 0.35)).normalized()
            for j in range(2):
                jd = (inward + Vector((rng.uniform(-0.25, 0.25), rng.uniform(-0.25, 0.25), rng.uniform(-0.2, 0.2)))).normalized()
                brc.add_hex_crystal(bm_veins, anchor + inward * 0.4, jd, rng.uniform(2.0, 4.0), rng.uniform(0.28, 0.50), seed=ri * 15 + ci + j)

    ribs_obj = kit.finish_object(bm_ribs, "Cavern_TallRibs", stone_mat)
    for p in ribs_obj.data.polygons:
        p.use_smooth = False
    kit.paint(ribs_obj, lambda p, n: brc.mix(SLATE_DEEP, SLATE_PALE, 0.25 * (1.0 - brc.clamp01(p.z / DOME_PEAK_Z)) + 0.22 * brc.ridged(p * 0.25)))

    veins_obj = kit.finish_object(bm_veins, "Cavern_JadeVeins_Glow", glow_mat)
    for p in veins_obj.data.polygons:
        p.use_smooth = False
    kit.paint(veins_obj, lambda p, n: brc.mix(JADE_DEEP, JADE_ICE, brc.clamp01(0.4 + 0.5 * noise.noise(p * 0.3))))

    return [arch_stone, arch_bones, arch_glow, ribs_obj, veins_obj]


def _build_perches_crystals_and_nest(rng, stone_mat, glow_mat):
    """6 ụ đá/xương Prop_Perch_01..06 (cao 2.8..5.8 m ở vòng ngoài), 4 cụm Prop_Crystal_01..04, ổ lớn Boss_Wait và vòng xương giữa phòng."""
    objs = []

    # 1. 6 Ụ đá/xương cao Prop_Perch_01..06 (2-6 m, đỉnh phẳng làm chỗ chim đậu)
    for idx, (adeg, prad, pheight) in enumerate(PERCH_SPECS, start=1):
        rad = math.radians(adeg)
        px = math.sin(rad) * prad
        py = math.cos(rad) * prad
        pz = _floor_z(px, py)

        bm_perch = bmesh.new()
        bm_perch_bone = bmesh.new()
        bm_perch_glow = bmesh.new()

        # Trụ đá vươn lên độ cao pheight, đỉnh mặt phẳng bán kính ~1.7 m để Chim đậu êm
        rings = []
        levels = [(-0.25, 2.35), (pheight * 0.25, 1.85), (pheight * 0.65, 1.55), (pheight * 0.94, 1.72), (pheight, 1.62)]
        for li, (zz, rr) in enumerate(levels):
            ring = []
            for k in range(10):
                a = 2.0 * math.pi * k / 10
                w = 1.0 + (0.05 if li >= 3 else 0.14) * math.sin(a * 3.0 + idx + li)
                ring.append(bm_perch.verts.new(Vector((math.cos(a) * rr * w, math.sin(a) * rr * w, zz))))
            rings.append(ring)
        kit.bridge_rings(bm_perch, rings)
        kit.cap_ring(bm_perch, rings[0], flip=True)
        kit.cap_ring(bm_perch, rings[-1])

        # Hai xương sườn lớn cắm quanh sườn ụ đá (không vượt quá mặt phẳng đỉnh pheight)
        for bi in range(2):
            ba = rad + (bi - 0.5) * 1.4
            bpts = [
                (math.cos(ba) * 1.7, math.sin(ba) * 1.7, 0.2),
                (math.cos(ba) * 1.9, math.sin(ba) * 1.9, pheight * 0.55),
                (math.cos(ba) * 1.35, math.sin(ba) * 1.35, pheight * 0.96),
            ]
            brc.add_curved_bone(bm_perch_bone, bpts, [0.26, 0.21, 0.13], segs=6)

        # Cụm tinh thể xanh lạnh nhỏ bên hông chân ụ
        brc.add_hex_crystal(bm_perch_glow, Vector((math.cos(rad + 2.2) * 1.5, math.sin(rad + 2.2) * 1.5, 0.4)), Vector((math.cos(rad + 2.2) * 0.5, math.sin(rad + 2.2) * 0.5, 0.85)), 1.35, 0.20, seed=idx * 7)

        pname = f"Prop_Perch_{idx:02d}"
        p_obj = kit.finish_object(bm_perch, pname, stone_mat)
        p_obj.location = Vector((px, py, pz))
        for p in p_obj.data.polygons:
            p.use_smooth = False
        kit.paint(p_obj, lambda p, n, seed=idx: brc.mix(SLATE_DARK, SLATE_PALE, 0.35 + 0.35 * brc.ridged(p * 0.5 + Vector((seed, 0, 0)))))

        pb_obj = kit.finish_object(bm_perch_bone, f"{pname}_Bones", stone_mat)
        pb_obj.parent = p_obj
        for p in pb_obj.data.polygons:
            p.use_smooth = False
        kit.paint(pb_obj, lambda p, n: brc.mix(BONE_SHADOW, BONE_IVORY, 0.55))

        pg_obj = kit.finish_object(bm_perch_glow, f"{pname}_Glow", glow_mat)
        pg_obj.parent = p_obj
        for p in pg_obj.data.polygons:
            p.use_smooth = False
        kit.paint(pg_obj, lambda p, n: brc.mix(JADE_BRIGHT, JADE_ICE, brc.clamp01(p.z / 2.0)))

        objs.extend([p_obj, pb_obj, pg_obj])

    # 2. 4 Cụm tinh thể xanh ngọc lạnh sát tường Prop_Crystal_01..04
    for idx, adeg in enumerate(CRYSTAL_ANGLES_FROM_NORTH, start=1):
        rad = math.radians(adeg)
        cx = math.sin(rad) * 28.2
        cy = math.cos(rad) * 28.2
        cz = _floor_z(cx, cy)

        bm_base = bmesh.new()
        bm_shards = bmesh.new()
        brc.add_rock_cluster(bm_base, (0.0, 0.0, 0.45), (1.9, 1.6, 0.9), seed=400 + idx, count=3)
        inward_ang = math.atan2(-cy, -cx)
        for si in range(6):
            a_off = (si - 2.5) * 0.42
            dv = Vector((math.cos(inward_ang + a_off) * 0.32, math.sin(inward_ang + a_off) * 0.32, 0.88)).normalized()
            brc.add_hex_crystal(bm_shards, Vector((math.cos(si) * 0.5, math.sin(si) * 0.5, 0.3)), dv, 3.0 + (si % 3) * 1.1, 0.34, seed=idx * 19 + si)

        cname = f"Prop_Crystal_{idx:02d}"
        c_base = kit.finish_object(bm_base, cname, stone_mat)
        c_base.location = Vector((cx, cy, cz))
        for p in c_base.data.polygons:
            p.use_smooth = False
        kit.paint(c_base, lambda p, n: brc.mix(SLATE_DARK, SLATE_PALE, 0.45 * brc.ridged(p * 0.7)))

        c_glow = kit.finish_object(bm_shards, f"{cname}_Glow", glow_mat)
        c_glow.parent = c_base
        for p in c_glow.data.polygons:
            p.use_smooth = False
        kit.paint(c_glow, lambda p, n: brc.mix(JADE_DEEP, JADE_ICE, brc.clamp01(p.z / 4.5)))

        objs.extend([c_base, c_glow])

    # 3. Ổ lông & đống xương khổng lồ phía Bắc (Boss_Wait ở (0, 21, 0): lòng giữa phẳng 0, vành ổ cao 0.9 m bao quanh bán kính 4.8 m)
    bm_nest = bmesh.new()
    bm_nest_bones = bmesh.new()
    nx, ny, nz = BOSS_WAIT_POS
    # Vành ổ dệt bằng cành rễ / lông đan vòng quanh bán kính 4.4..5.4 m (để hở 1 khe nhỏ mặt trước Nam cho chim bước ra êm)
    for k in range(18):
        ang = -math.pi * 0.30 + (k / 17.0) * math.pi * 1.60  # chừa mặt -Y (Nam) rộng thoáng
        a_next = ang + 0.32
        p0 = (math.sin(ang) * 4.7, math.cos(ang) * 4.7, 0.35 + 0.18 * (k % 2))
        p1 = (math.sin(a_next) * 5.0, math.cos(a_next) * 5.0, 0.65 + 0.2 * ((k + 1) % 2))
        brc.add_curved_bone(bm_nest, [p0, p1], [0.55, 0.48], segs=6, wobble_amp=0.14)
    # Xương sườn cắm quanh vành ổ
    for k in range(10):
        ang = -math.pi * 0.35 + (k / 9.0) * math.pi * 1.70
        sx, sy = math.sin(ang) * 5.1, math.cos(ang) * 5.1
        brc.add_curved_bone(
            bm_nest_bones,
            [(sx, sy, 0.1), (sx * 1.08, sy * 1.08, 1.5), (sx * 0.98, sy * 0.98, 2.7)],
            [0.24, 0.18, 0.08],
            segs=6,
        )

    nest_obj = kit.finish_object(bm_nest, "BossWait_NestRim", stone_mat)
    nest_obj.location = Vector((nx, ny, nz))
    for p in nest_obj.data.polygons:
        p.use_smooth = False
    kit.paint(nest_obj, lambda p, n: brc.mix(FEATHER_NEST, SLATE_MID, 0.4 * brc.ridged(p * 1.2)))

    nest_bones = kit.finish_object(bm_nest_bones, "BossWait_NestBones", stone_mat)
    nest_bones.parent = nest_obj
    for p in nest_bones.data.polygons:
        p.use_smooth = False
    kit.paint(nest_bones, lambda p, n: brc.mix(BONE_SHADOW, BONE_IVORY, 0.65))

    # 4. Vòng xương vỡ và mảnh vỏ trứng cực thấp (cao <= 0.10 m) quanh tâm (0,0,0) đánh dấu vùng Activate_Zone
    bm_center_bones = bmesh.new()
    for k in range(12):
        ang = k * (2.0 * math.pi / 12.0) + rng.uniform(-0.15, 0.15)
        dist = rng.uniform(3.2, 5.2)
        cx_b, cy_b = math.cos(ang) * dist, math.sin(ang) * dist
        brc.add_curved_bone(
            bm_center_bones,
            [(cx_b, cy_b, 0.03), (cx_b + math.cos(ang + 1.2) * 1.3, cy_b + math.sin(ang + 1.2) * 1.3, 0.05)],
            [0.09, 0.06],
            segs=5,
        )
    center_bones = kit.finish_object(bm_center_bones, "Story_CenterScatteredBones", stone_mat)
    for p in center_bones.data.polygons:
        p.use_smooth = False
    kit.paint(center_bones, lambda p, n: brc.mix(BONE_SHADOW, BONE_IVORY, 0.55))

    objs.extend([nest_obj, nest_bones, center_bones])
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

    stone_mat = brc.make_stone_material("BirdRoom_StoneMat")
    glow_mat = brc.make_glow_material("BirdRoom_JadeGlowMat", energy=0.95)

    print("[Chim 1/3] Dựng vỏ hang cao 38.5m, sàn 64m, hốc cửa Nam 4.8x7.2m...", flush=True)
    shell_obj, col_obj = brc.build_watertight_shell(
        "BossRoom_Bird", stone_mat, _floor_z, _wall_disp, _paint_shell,
        wall_top_z=WALL_TOP_Z, dome_peak_z=DOME_PEAK_Z,
    )
    arch_objs = _build_bone_arch_and_ribs(rng, stone_mat, glow_mat)

    print("[Chim 2/3] Dựng 6 ụ đậu Prop_Perch_01..06 (2.8..5.8m), 4 cụm tinh thể & ổ kền kền...", flush=True)
    prop_objs = _build_perches_crystals_and_nest(rng, stone_mat, glow_mat)

    door_obj = brc.build_steel_door("BossRoom_SteelDoor")

    if args.out:
        out_dir = os.path.abspath(args.out)
        room_objs = [shell_obj, col_obj] + arch_objs + prop_objs
        kit.export_fbx(room_objs, os.path.join(out_dir, "BossRoom_Bird.fbx"))

    if args.previews:
        prev_dir = os.path.abspath(args.previews)
        os.makedirs(prev_dir, exist_ok=True)
        door_obj.location = Vector((0.0, brc.DOOR_Y, 4.4))
        lights = [
            ("dome_high", (0.0, 0.0, 32.0), (0.55, 0.95, 0.92), 7800.0, 12.0),
            ("dome_south", (0.0, -18.0, 16.0), (0.35, 0.88, 0.84), 3400.0, 6.0),
            ("dome_north", (0.0, 18.0, 16.0), (0.30, 0.85, 0.82), 3400.0, 6.0),
            ("door_l", (-3.8, -27.5, 4.8), (0.20, 0.92, 0.85), 1200.0, 2.0),
            ("door_r", (3.8, -27.5, 4.8), (0.20, 0.92, 0.85), 1200.0, 2.0),
            ("nest_north", (0.0, 20.0, 5.5), (0.30, 0.90, 0.85), 2200.0, 3.0),
        ]
        cam, scene = brc.setup_cycles_scene((0.01, 0.025, 0.028), 0.35, lights)

        brc.render_view(cam, scene, (0.0, -25.8, 2.6), (0.0, 10.0, 4.5), 16.0,
                        os.path.join(prev_dir, "boss_room_bird_entry_view.png"))
        brc.render_view(cam, scene, (2.8, -14.5, 3.2), (0.0, brc.DOOR_Y, 5.2), 18.0,
                        os.path.join(prev_dir, "boss_room_bird_door_view.png"))
        brc.render_view(cam, scene, (-4.0, -10.0, 4.2), (17.0, 5.0, 3.8), 20.0,
                        os.path.join(prev_dir, "boss_room_bird_perches.png"))
        brc.render_view(cam, scene, (0.0, -24.5, 25.5), (0.0, 4.0, 1.5), 15.0,
                        os.path.join(prev_dir, "boss_room_bird_overview.png"))

    print(f"Hoàn tất Phòng Chim Hang Mẹ trong {time.time() - t0:.1f}s", flush=True)


if __name__ == "__main__":
    main()
