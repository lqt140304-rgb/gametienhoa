"""Dựng Phòng Trùm Đợt 2 - Phòng 2: Cáo Gió ("Khe Gió Hú") — Blender headless.

Tuân thủ docs/superpowers/specs/2026-11-22-boss-rooms-wave2-design.md:
- Sàn phẳng bán kính 32 m (đường kính 64 m), trần kín 36.0 m (không lỗ).
- Cửa vào duy nhất phía Nam (4.8 x 7.2 m), vòm Đá Xoáy Phong Thạch uốn cong ôm cửa khảm tinh thể bạc xanh.
- Kể chuyện môi trường:
  + Ổ Boss_Wait (0, 22, 0): bệ đá phiến phẳng hình lưỡi liềm bị gió mài nhẵn, khe nứt gió phía sau,
    xung quanh rải vỏ Dế Gió và xương Chuột Gió.
  + Dấu vết Gọi Bầy (Chồn Khe): 6 hốc đá / cửa hang thấp sát chân tường với xương mồi nhỏ.
  + Dấu vết Giáo Gió & Bão Giáo: các vết chém gió cắt xiên sâu trên thân cột & mảnh phong thạch găm nghiêng.
  + Dấu vết Vòng Xoáy: các dải vân cát bạc xoáy đồng tâm sát sàn (<= 0.03 m).
- Vật cản chiến đấu: 6 cột đá phiến xoắn ốc chịu vết chém Giáo Gió (Prop_Pillar_01..06, r = 14..17.2 m, d ~ 2.5 m),
  4 mỏm đá nghiêng vòng ngoài (Prop_Perch_01..04, r = 21..24 m, cao 1.8-3.2 m),
  6 cụm tinh thể phong thạch nghiêng 45° theo chiều gió xoáy (Prop_Crystal_01..06).

Chạy:
  D:/blender/blender.exe -b -P tools/blender/build_boss_room_fox.py -- --out Assets/_Project/Models/Underroot --previews docs/art/previews
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


WALL_TOP_Z = 26.0
DOME_PEAK_Z = 36.0
BOSS_WAIT_POS = (0.0, 22.0, 0.0)

PILLAR_ANGLES_FROM_NORTH = [30.0, 90.0, 145.0, 215.0, 270.0, 330.0]
PILLAR_RADII = [15.5, 14.5, 16.8, 16.5, 14.8, 15.8]
PERCH_ANGLES_FROM_NORTH = [58.0, 122.0, 238.0, 302.0]
PERCH_RADII = [22.0, 23.2, 22.8, 21.8]
CRYSTAL_ANGLES_FROM_NORTH = [22.0, 72.0, 128.0, 232.0, 288.0, 338.0]

# Bảng màu: đá phiến xám bạc gió bào, ánh trắng bạc - xanh lam gió lạnh
SLATE_DEEP = (0.025, 0.030, 0.038)
SLATE_DARK = (0.058, 0.066, 0.080)
SLATE_MID = (0.115, 0.130, 0.152)
SLATE_SILVER = (0.210, 0.235, 0.265)

WIND_CYAN = (0.28, 0.78, 0.96)
WIND_WHITE = (0.78, 0.94, 1.00)
PREY_BONE = (0.45, 0.42, 0.36)


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
    n = 0.028 * math.sin(r * 0.45 - math.atan2(y, x) * 2.0) + 0.015 * noise.noise(Vector((x * 0.12, y * 0.12, 3.1)))
    return n * flat_mask


def _wall_disp(px, py, z_base, r_scale, li):
    ang = math.atan2(py, px)
    # Vách đá phiến lượn sóng xoắn ốc theo luồng gió bào mòn
    flute = math.sin(ang * 12.0 + z_base * 0.18) * 0.85
    crag = (brc.ridged(Vector((px * 0.065, py * 0.065, z_base * 0.08))) - 0.5) * 1.6
    r_disp = (flute + crag) * (0.45 if r_scale < 0.7 else 1.0)
    z_disp = noise.noise(Vector((px * 0.08, py * 0.08, 6.4))) * 1.1 * r_scale if li > 9 else 0.0
    return r_disp, z_disp


def _paint_shell(pos, _n):
    r = math.hypot(pos.x, pos.y)
    n_c = noise.noise(Vector((pos.x * 0.14, pos.y * 0.14, pos.z * 0.14))) * 0.5 + 0.5
    n_f = brc.ridged(Vector((pos.x * 0.34, pos.y * 0.34, pos.z * 0.34)))

    if pos.z < 0.45 and r < brc.ROOM_RADIUS + 0.5:
        c = brc.mix(SLATE_DARK, SLATE_MID, n_c * 0.6)
        ang = math.atan2(pos.y, pos.x)
        swirl = 0.5 + 0.5 * math.sin(r * 0.9 - ang * 3.0)
        c = brc.mix(c, SLATE_SILVER, swirl * 0.25)
        return c

    h_t = brc.clamp01(pos.z / DOME_PEAK_Z)
    c = brc.mix(SLATE_MID, SLATE_DEEP, h_t * 0.82)
    c = brc.mix(c, SLATE_SILVER, n_f * 0.32 * (1.0 - h_t * 0.55))
    return c


def _build_arch_and_ceiling(rng, stone_mat, glow_mat):
    bm_arch = bmesh.new()
    bm_glow = bmesh.new()

    # 1. Vòm Đá Xoáy Phong Thạch: các phiến đá mỏng uốn cong xoáy ôm lấy lối vào Nam
    for side in (-1.0, 1.0):
        for si in range(7):
            t = si / 6.0
            ang = math.pi * (0.05 + 0.45 * t)
            px = side * (3.65 - 0.85 * t + 0.4 * math.sin(t * 3.1))
            pz = 0.8 + t * 7.8
            py = brc.DOOR_Y + 0.75 + 0.35 * math.sin(t * math.pi)
            brc.add_angular_stone_block(
                bm_arch,
                (px, py, pz),
                (1.85 - 0.35 * t, 2.55, 1.55),
                (0.12 * side, -side * (0.18 + 0.45 * t), side * 0.28),
                rock_seed=10 + si * 7 + int(side + 2),
                chip=0.14,
            )
            if si in (2, 4, 6):
                c_dir = Vector((-side * 0.45, 0.65, 0.60)).normalized()
                brc.add_hex_crystal(bm_glow, Vector((px - side * 0.3, py + 0.8, pz)), c_dir, 1.85, 0.22, seed=si * 5 + int(side + 2))

    # Đỉnh vòm xoáy ghép từ các phiến đá chéo
    for vi in range(7):
        t = vi / 6.0
        ang = math.pi * (0.15 + 0.70 * t)
        vx = math.cos(ang) * 3.1
        vz = 7.2 + math.sin(ang) * 2.6
        brc.add_angular_stone_block(
            bm_arch,
            (vx, brc.DOOR_Y + 0.75, vz),
            (1.55, 2.55, 1.15),
            (0.18, ang - math.pi * 0.5, 0.14),
            rock_seed=80 + vi * 9,
            chip=0.14,
        )
    brc.add_hex_crystal(bm_glow, Vector((0.0, brc.DOOR_Y + 1.5, 9.6)), Vector((0.0, 0.75, 0.65)).normalized(), 2.2, 0.32, seed=99)

    # 2. Phiến nhũ đá dẹt gió bào trên trần vòm (> 25 m)
    for si in range(16):
        ang = rng.uniform(0.0, 2.0 * math.pi)
        rad = rng.uniform(11.0, 26.0)
        sx, sy = math.cos(ang) * rad, math.sin(ang) * rad
        sz_top = WALL_TOP_Z + (DOME_PEAK_Z - WALL_TOP_Z) * (1.0 - (rad / brc.ROOM_RADIUS) ** 1.7)
        brc.add_angular_stone_block(bm_arch, (sx, sy, sz_top - 1.2), (1.8, 0.9, 3.8), (0.15, 0.25, ang + 0.5), rock_seed=150 + si, chip=0.18)

    arch_obj = kit.finish_object(bm_arch, "BossRoom_Fox_Arch", stone_mat)
    kit.paint(arch_obj, lambda p, n: brc.mix(SLATE_DARK, SLATE_SILVER, brc.clamp01(p.z / 12.0) * 0.5 + 0.2))

    glow_obj = kit.finish_object(bm_glow, "BossRoom_Fox_ArchGlow", glow_mat)
    kit.paint(glow_obj, lambda p, n: brc.mix(WIND_CYAN, WIND_WHITE, 0.5 + 0.5 * math.sin(p.z)))

    return [arch_obj, glow_obj]


def _build_pillars_perches_and_crystals(rng, stone_mat, glow_mat):
    objs = []

    # 1. 6 cột đá phiến bị gió tiện rãnh xoắn ốc & chi chít vết Giáo Gió (d ~ 2.5 m)
    for idx, (ang_deg, rad) in enumerate(zip(PILLAR_ANGLES_FROM_NORTH, PILLAR_RADII), 1):
        ang = math.radians(90.0 - ang_deg)
        cx, cy = math.cos(ang) * rad, math.sin(ang) * rad

        bm_p = bmesh.new()
        bm_pg = bmesh.new()
        pts = []
        rads = []
        h_max = 16.0 + (idx % 3) * 2.2
        for si in range(10):
            t = si / 9.0
            # Xoắn nhẹ quanh trục thẳng đứng tạo dáng cột bị gió tiện
            swirl_x = 0.22 * math.cos(t * math.pi * 2.0 + idx)
            swirl_y = 0.22 * math.sin(t * math.pi * 2.0 + idx)
            pts.append(Vector((cx + swirl_x, cy + swirl_y, t * h_max)))
            rads.append(1.38 * (1.0 - 0.16 * t + 0.10 * math.sin(t * math.pi * 3.0)))

        rgs = brc.parallel_tube_rings(
            bm_p, pts, rads, 10,
            wobble=lambda p, a, i: 0.14 * math.sin(a * 3.0 + i * 0.75) - (0.24 if (i in (3, 6) and abs(math.sin(a + i)) > 0.85) else 0.0),
        )
        kit.bridge_rings(bm_p, rgs)
        kit.cap_ring(bm_p, rgs[0], flip=True)
        kit.cap_ring(bm_p, rgs[-1])

        # Mảnh phong thạch sắc nhọn găm nghiêng vào thân cột (dấu vết Giáo Gió)
        inward = Vector((-math.cos(ang), -math.sin(ang), 0.0)).normalized()
        for gi in range(2):
            gz = 2.6 + gi * 2.4
            g_pos = Vector((cx, cy, gz)) + inward * 1.15
            g_dir = (inward + Vector((0.35, -0.25, 0.45))).normalized()
            brc.add_hex_crystal(bm_pg, g_pos, g_dir, 1.35, 0.16, seed=idx * 10 + gi)

        p_obj = kit.finish_object(bm_p, f"Prop_Pillar_{idx:02d}", stone_mat)
        kit.paint(p_obj, lambda p, n: brc.mix(SLATE_DARK, SLATE_SILVER, brc.clamp01(p.z / 16.0) * 0.65 + 0.2))

        pg_obj = kit.finish_object(bm_pg, f"Prop_Pillar_{idx:02d}_Glow", glow_mat)
        pg_obj.parent = p_obj
        kit.paint(pg_obj, lambda p, n: brc.mix(WIND_CYAN, WIND_WHITE, 0.6))
        objs.append(p_obj)

    # 2. 4 mỏm đá nghiêng theo chiều gió ở vòng ngoài (r = 21..24 m, cao 1.8-3.2 m)
    for idx, (ang_deg, rad) in enumerate(zip(PERCH_ANGLES_FROM_NORTH, PERCH_RADII), 1):
        ang = math.radians(90.0 - ang_deg)
        px, py = math.cos(ang) * rad, math.sin(ang) * rad
        top_h = 1.9 + (idx % 3) * 0.55

        bm_perch = bmesh.new()
        brc.add_angular_stone_block(bm_perch, (px, py, top_h * 0.5), (3.2, 2.4, top_h), (0.0, 0.18, ang + math.pi * 0.5), rock_seed=210 + idx * 13, chip=0.14)
        brc.add_rock_cluster(bm_perch, (px, py, top_h * 0.35), (2.2, 1.8, top_h * 0.5), seed=240 + idx, count=2)

        perch_obj = kit.finish_object(bm_perch, f"Prop_Perch_{idx:02d}", stone_mat)
        kit.paint(perch_obj, lambda p, n: brc.mix(SLATE_MID, SLATE_SILVER, brc.clamp01(p.z / 3.2) * 0.6 + 0.2))
        objs.append(perch_obj)

    # 3. 6 cụm tinh thể phong thạch xanh bạc mọc nghiêng 45° theo hướng gió xoáy dọc tường
    for idx, ang_deg in enumerate(CRYSTAL_ANGLES_FROM_NORTH, 1):
        ang = math.radians(90.0 - ang_deg)
        rad = 28.0 + (idx % 2) * 0.7
        cx, cy = math.cos(ang) * rad, math.sin(ang) * rad
        tangent = Vector((-math.sin(ang), math.cos(ang), 0.0)).normalized()
        inward = Vector((-math.cos(ang), -math.sin(ang), 0.0)).normalized()

        bm_cr = bmesh.new()
        for k in range(5):
            off = Vector((math.cos(k * 1.4) * 0.8, math.sin(k * 1.4) * 0.8, 0.0))
            # Nghiêng ~45 độ dọc theo chiều xoáy ngược kim đồng hồ
            dir_v = (tangent * 0.58 + inward * 0.35 + Vector((0.0, 0.0, 0.72))).normalized()
            brc.add_hex_crystal(bm_cr, Vector((cx, cy, 0.0)) + off, dir_v, rng.uniform(2.8, 4.8), rng.uniform(0.30, 0.52), seed=idx * 12 + k)

        cr_obj = kit.finish_object(bm_cr, f"Prop_Crystal_{idx:02d}_Glow", glow_mat)
        kit.paint(cr_obj, lambda p, n: brc.mix(WIND_CYAN, WIND_WHITE, brc.clamp01(p.z / 4.2)))
        objs.append(cr_obj)

    return objs


def _build_story_details_and_boss_dais(rng, stone_mat, glow_mat):
    objs = []

    # 1. Ổ Boss_Wait (0, 22, 0): Bệ đá phiến phẳng hình lưỡi liềm bị gió mài nhẵn + khe nứt gió phía sau
    bm_dais = bmesh.new()
    bx, by, _ = BOSS_WAIT_POS
    brc.add_angular_stone_block(bm_dais, (bx, by, 0.04), (6.8, 5.2, 0.14), (0.0, 0.0, 0.0), rock_seed=301, chip=0.06)
    # Cánh đá phiến hình lưỡi liềm ôm phía sau Boss_Wait (y > by + 2.5)
    for ci in range(7):
        t = ci / 6.0
        ca = math.pi * (0.12 + 0.76 * t)
        dx = bx + math.cos(ca) * 5.8
        dy = by + 1.2 + math.sin(ca) * 4.2
        brc.add_angular_stone_block(bm_dais, (dx, dy, 1.8 + 1.4 * math.sin(t * math.pi)), (2.0, 1.5, 4.2 + 2.0 * math.sin(t * math.pi)), (0.14, 0.0, ca - math.pi * 0.5), rock_seed=310 + ci, chip=0.14)

    dais_obj = kit.finish_object(bm_dais, "Boss_Dais_Crescent", stone_mat)
    kit.paint(dais_obj, lambda p, n: brc.mix(SLATE_DARK, SLATE_SILVER, brc.clamp01(p.z / 5.5) * 0.65 + 0.15))
    objs.append(dais_obj)

    # 2. 6 hốc đá / cửa hang thấp Chồn Khe sát chân tường (Gọi Bầy), tránh cửa Nam (270°)
    bm_dens = bmesh.new()
    for di, ang_deg in enumerate((25.0, 65.0, 115.0, 155.0, 205.0, 335.0)):
        ang = math.radians(ang_deg)
        dx, dy = math.cos(ang) * 29.8, math.sin(ang) * 29.8
        brc.add_rock_cluster(bm_dens, (dx, dy, 0.8), (2.2, 2.0, 1.6), seed=400 + di, count=3)
    den_obj = kit.finish_object(bm_dens, "Story_ChonKheBurrows", stone_mat)
    kit.paint(den_obj, lambda p, n: SLATE_DEEP)
    objs.append(den_obj)

    # 3. Dải vân cát bạc xoáy đồng tâm sát sàn (<= 0.03 m, không collider)
    bm_swirls = bmesh.new()
    for si in range(4):
        rad_base = 5.5 + si * 4.2
        pts = []
        rads = []
        for k in range(18):
            t = k / 17.0
            ang = si * 1.4 + t * math.pi * 1.35
            r = rad_base + t * 2.2
            pts.append(Vector((math.cos(ang) * r, math.sin(ang) * r, 0.015)))
            rads.append(0.18 * math.sin(t * math.pi) + 0.04)
        rgs = brc.parallel_tube_rings(bm_swirls, pts, rads, 4)
        kit.bridge_rings(bm_swirls, rgs)
        kit.cap_ring(bm_swirls, rgs[0], flip=True)
        kit.cap_ring(bm_swirls, rgs[-1])

    swirl_obj = kit.finish_object(bm_swirls, "Story_FoxWindSwirls", stone_mat)
    kit.paint(swirl_obj, lambda p, n: SLATE_SILVER)
    objs.append(swirl_obj)

    # 4. Vỏ Dế Gió & xương Chuột Gió rải quanh chân bệ Boss_Wait & cửa hốc Chồn Khe (<= 0.05 m)
    bm_prey = bmesh.new()
    for pi in range(16):
        ang = rng.uniform(0.0, 2.0 * math.pi)
        rad = rng.uniform(3.2, 8.0)
        px = bx + math.cos(ang) * rad
        py = by - 2.0 + math.sin(ang) * rad * 0.7
        brc.add_angular_stone_block(bm_prey, (px, py, 0.025), (0.35, 0.18, 0.05), (0.0, 0.0, rng.uniform(0.0, 6.28)), rock_seed=500 + pi, chip=0.12)
    prey_obj = kit.finish_object(bm_prey, "Story_PreyShells", stone_mat)
    kit.paint(prey_obj, lambda p, n: PREY_BONE)
    objs.append(prey_obj)

    return objs


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=None)
    ap.add_argument("--previews", default=None)
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    args = ap.parse_args(argv)

    t0 = time.time()
    rng = random.Random(20261124)
    kit.new_scene()

    stone_mat = brc.make_stone_material("FoxRoom_StoneMat", roughness=0.76, bump_strength=0.65)
    glow_mat = brc.make_glow_material("FoxRoom_GlowMat", energy=1.15)

    print("[Fox 1/3] Dựng vỏ phòng 64m, trần 36.0m, vòm Đá Xoáy Phong Thạch...", flush=True)
    shell_obj, col_obj = brc.build_watertight_shell(
        "BossRoom_Fox", stone_mat, _floor_z, _wall_disp, _paint_shell,
        wall_top_z=WALL_TOP_Z, dome_peak_z=DOME_PEAK_Z,
    )
    arch_objs = _build_arch_and_ceiling(rng, stone_mat, glow_mat)

    print("[Fox 2/3] Dựng 6 cột đá phiến xoắn ốc, 4 mỏm đá vòng ngoài, 6 cụm phong thạch 45°...", flush=True)
    prop_objs = _build_pillars_perches_and_crystals(rng, stone_mat, glow_mat)

    print("[Fox 3/3] Dựng bệ lưỡi liềm Boss_Wait, 6 hốc Chồn Khe & vân cát xoáy...", flush=True)
    story_objs = _build_story_details_and_boss_dais(rng, stone_mat, glow_mat)

    if args.out:
        out_dir = os.path.abspath(args.out)
        room_objs = [shell_obj, col_obj] + arch_objs + prop_objs + story_objs
        kit.export_fbx(room_objs, os.path.join(out_dir, "BossRoom_Fox.fbx"))

    if args.previews:
        prev_dir = os.path.abspath(args.previews)
        os.makedirs(prev_dir, exist_ok=True)
        door_obj = brc.build_steel_door("BossRoom_SteelDoor")
        door_obj.location = Vector((0.0, brc.DOOR_Y, 4.4))

        lights = [
            ("dome_main", (0.0, 0.0, 27.0), (0.72, 0.88, 1.00), 5800.0, 10.0),
            ("dome_south", (0.0, -18.0, 15.0), (0.60, 0.84, 1.00), 3000.0, 6.0),
            ("dome_north", (0.0, 18.0, 15.0), (0.68, 0.90, 1.00), 3200.0, 6.0),
            ("door_l", (-3.5, -27.5, 4.2), (0.45, 0.82, 1.00), 1100.0, 2.0),
            ("door_r", (3.5, -27.5, 4.2), (0.45, 0.82, 1.00), 1100.0, 2.0),
            ("boss_dais", (0.0, 19.0, 5.5), (0.65, 0.92, 1.00), 2000.0, 2.8),
        ]
        cam, scene = brc.setup_cycles_scene((0.014, 0.018, 0.025), 0.35, lights)
        brc.render_view(cam, scene, (0.0, -25.8, 2.4), (0.0, 10.0, 2.5), 18.0,
                        os.path.join(prev_dir, "boss_room_fox_entry_view.png"))
        brc.render_view(cam, scene, (3.6, -14.5, 3.0), (-1.5, -29.0, 3.2), 19.0,
                        os.path.join(prev_dir, "boss_room_fox_door_view.png"))
        brc.render_view(cam, scene, (8.0, 10.0, 3.8), (0.0, 21.5, 2.2), 21.0,
                        os.path.join(prev_dir, "boss_room_fox_dais_view.png"))
        brc.render_view(cam, scene, (2.2, -25.0, 22.0), (0.0, 4.0, 1.0), 15.0,
                        os.path.join(prev_dir, "boss_room_fox_overview.png"))

    print(f"Hoàn tất Phòng Cáo Gió trong {time.time() - t0:.1f}s", flush=True)


if __name__ == "__main__":
    main()
