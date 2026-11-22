"""Dựng Phòng Trùm Đợt 2 - Phòng 4: Rắn Dung Nham ("Hố Mạch Dung Nham") — Blender headless.

Tuân thủ docs/superpowers/specs/2026-11-22-boss-rooms-wave2-design.md:
- Sàn phẳng bán kính 32 m (đường kính 64 m), trần kín 31.5 m (không lỗ).
- Cửa vào duy nhất phía Nam (4.8 x 7.2 m), vòm Cột Bazan Nứt Dung Nham rỉ ánh dung nham đỏ cam.
- Kể chuyện môi trường:
  + Ổ Boss_Wait (0, 21, 0): bệ đá bazan cuộn tròn viền mạch dung nham đỏ rực, cạnh vỏ Cua Đá Nóng & Ốc Lưu Huỳnh.
  + Dấu vết Thân Nóng & Lột Xác: rãnh trườn chữ S phát sáng đỏ cam sát sàn (<= 0.05 m),
    và xác vỏ rắn lột hóa đá/kết tinh dung nham khổng lồ dọc vách Đông-Bắc.
  + Dấu vết Phun Lửa & Phun Nọc: mảng đá cháy đen hình rẻ quạt trước ổ & mặt trong cột.
- Vật cản chiến đấu: 6 cụm cột đá bazan lục giác (Prop_Pillar_01..06, r = 14..18 m, cao 2.2-4.5 m),
  8 ụ ống khói địa nhiệt & tinh thể lưu huỳnh vàng-đỏ dọc tường (Prop_Crystal_01..08, r = 27..29 m).

Chạy:
  D:/blender/blender.exe -b -P tools/blender/build_boss_room_snake.py -- --out Assets/_Project/Models/Underroot --previews docs/art/previews
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


WALL_TOP_Z = 22.5
DOME_PEAK_Z = 31.5
BOSS_WAIT_POS = (0.0, 21.0, 0.0)

PILLAR_ANGLES_FROM_NORTH = [35.0, 88.0, 142.0, 218.0, 272.0, 325.0]
PILLAR_RADII = [15.5, 14.5, 17.2, 17.5, 14.8, 16.0]
CRYSTAL_ANGLES_FROM_NORTH = [20.0, 62.0, 108.0, 152.0, 208.0, 252.0, 298.0, 340.0]

# Bảng màu: đá bazan đen tím, mạch dung nham & hơi lưu huỳnh đỏ cam rực cháy
BASALT_DEEP = (0.018, 0.015, 0.022)
BASALT_DARK = (0.036, 0.030, 0.042)
BASALT_MID = (0.068, 0.056, 0.075)
SCORCH_BLACK = (0.012, 0.010, 0.012)

LAVA_EMBER = (0.78, 0.12, 0.015)
LAVA_ORANGE = (1.00, 0.34, 0.025)
SULFUR_YELLOW = (0.95, 0.78, 0.08)


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
    n = 0.025 * math.sin(x * 0.24) * math.cos(y * 0.24) + 0.018 * noise.noise(Vector((x * 0.15, y * 0.15, 0.9)))
    return n * flat_mask


def _wall_disp(px, py, z_base, r_scale, li):
    # Vách bazan phân bậc góc cạnh
    rock_n = brc.stepped(0.5 + 0.5 * noise.noise(Vector((px * 0.09, py * 0.09, z_base * 0.07))), steps=5.0, sharp=0.84) - 0.5
    crag = (brc.ridged(Vector((px * 0.07, py * 0.07, z_base * 0.11))) - 0.5) * 1.6
    r_disp = (rock_n * 2.1 + crag) * (0.45 if r_scale < 0.7 else 1.0)
    z_disp = noise.noise(Vector((px * 0.09, py * 0.09, 2.7))) * 1.1 * r_scale if li > 9 else 0.0
    return r_disp, z_disp


def _paint_shell(pos, _n):
    r = math.hypot(pos.x, pos.y)
    n_c = noise.noise(Vector((pos.x * 0.15, pos.y * 0.15, pos.z * 0.15))) * 0.5 + 0.5
    n_f = brc.ridged(Vector((pos.x * 0.36, pos.y * 0.36, pos.z * 0.36)))

    if pos.z < 0.45 and r < brc.ROOM_RADIUS + 0.5:
        c = brc.mix(BASALT_DARK, BASALT_MID, n_c * 0.58)
        # Mảng cháy đen hình rẻ quạt trước ổ Boss_Wait (y in 8..19)
        if 7.0 < pos.y < 20.0 and abs(pos.x) < (21.0 - pos.y) * 0.65:
            c = brc.mix(c, SCORCH_BLACK, 0.65)
        return c

    h_t = brc.clamp01(pos.z / DOME_PEAK_Z)
    c = brc.mix(BASALT_MID, BASALT_DEEP, h_t * 0.85)
    c = brc.mix(c, LAVA_EMBER, max(0.0, n_f - 0.78) * 1.5 * (1.0 - h_t * 0.7))
    return c


def _add_hex_basalt_column(bm, base_xy, radius, height, seed=0):
    """Thêm 1 trụ bazan lục giác phân bậc sắc cạnh."""
    bx, by = base_xy
    pts = [Vector((bx, by, 0.0)), Vector((bx, by, height * 0.88)), Vector((bx, by, height))]
    rgs = brc.parallel_tube_rings(bm, pts, [radius, radius * 0.96, radius * 0.82], 6)
    kit.bridge_rings(bm, rgs)
    kit.cap_ring(bm, rgs[0], flip=True)
    kit.cap_ring(bm, rgs[-1])


def _build_arch_and_ceiling(rng, stone_mat, glow_mat):
    bm_arch = bmesh.new()
    bm_glow = bmesh.new()

    # 1. Vòm Cột Bazan Nứt Dung Nham: các cột lục giác xếp bậc gãy góc, khe giữa rỉ dung nham
    for side in (-1.0, 1.0):
        px = side * 3.3
        for ci, (ox, oy, h) in enumerate(((0.0, 0.5, 4.5), (side * 0.9, 0.8, 7.6), (side * 1.8, 0.3, 9.8))):
            _add_hex_basalt_column(bm_arch, (px + ox, brc.DOOR_Y + oy), 0.78, h, seed=ci * 7 + int(side + 2))
            # Khe dung nham sáng rỉ giữa các cột bazan
            brc.add_hex_crystal(bm_glow, Vector((px + ox - side * 0.45, brc.DOOR_Y + oy + 0.55, 1.5 + ci * 2.0)), Vector((-side * 0.2, 0.8, 0.55)).normalized(), 1.6, 0.16, seed=ci + int(side + 3))

    # Thanh đà bazan gãy góc bắc ngang trên đỉnh cửa
    for vi in range(7):
        t = vi / 6.0
        ang = math.pi * (0.12 + 0.76 * t)
        vx = math.cos(ang) * 3.3
        vz = 7.0 + math.sin(ang) * 2.5
        brc.add_angular_stone_block(bm_arch, (vx, brc.DOOR_Y + 0.75, vz), (1.35, 2.45, 1.35), (0.0, ang - math.pi * 0.5, 0.0), rock_seed=90 + vi, chip=0.10)
        if vi % 2 == 1:
            brc.add_hex_crystal(bm_glow, Vector((vx, brc.DOOR_Y + 1.4, vz - 0.3)), Vector((0.0, 0.8, 0.4)).normalized(), 1.3, 0.18, seed=vi)

    # 2. Nhũ đá bazan rủ từ trần vòm (> 23 m)
    for si in range(16):
        ang = rng.uniform(0.0, 2.0 * math.pi)
        rad = rng.uniform(10.0, 26.0)
        sx, sy = math.cos(ang) * rad, math.sin(ang) * rad
        sz_top = WALL_TOP_Z + (DOME_PEAK_Z - WALL_TOP_Z) * (1.0 - (rad / brc.ROOM_RADIUS) ** 1.7)
        brc.add_hex_crystal(bm_arch, Vector((sx, sy, sz_top + 0.4)), Vector((0.0, 0.0, -1.0)), rng.uniform(2.2, 4.4), rng.uniform(0.38, 0.65), seed=si * 11)

    arch_obj = kit.finish_object(bm_arch, "BossRoom_Snake_Arch", stone_mat)
    kit.paint(arch_obj, lambda p, n: brc.mix(BASALT_DEEP, BASALT_MID, brc.clamp01(p.z / 10.0) * 0.5 + 0.2))

    glow_obj = kit.finish_object(bm_glow, "BossRoom_Snake_ArchGlow", glow_mat)
    kit.paint(glow_obj, lambda p, n: brc.mix(LAVA_EMBER, LAVA_ORANGE, 0.5 + 0.5 * math.sin(p.z * 1.4)))

    return [arch_obj, glow_obj]


def _build_pillars_and_crystals(rng, stone_mat, glow_mat):
    objs = []

    # 1. 6 cụm cột đá bazan lục giác (r = 14..18 m, cao 2.2-4.5 m), mặt hướng tâm nung đỏ sém, mặt ngoài nguội đen
    for idx, (ang_deg, rad) in enumerate(zip(PILLAR_ANGLES_FROM_NORTH, PILLAR_RADII), 1):
        ang = math.radians(90.0 - ang_deg)
        cx, cy = math.cos(ang) * rad, math.sin(ang) * rad
        inward = Vector((-math.cos(ang), -math.sin(ang), 0.0)).normalized()

        bm_p = bmesh.new()
        for ci in range(5):
            ca = ci * (2.0 * math.pi / 5.0) + idx * 0.4
            dist = 0.0 if ci == 0 else 0.95
            col_h = (3.4 + (idx % 3) * 0.55) if ci == 0 else (2.1 + (ci % 3) * 0.65)
            _add_hex_basalt_column(bm_p, (cx + math.cos(ca) * dist, cy + math.sin(ca) * dist), 0.72 if ci == 0 else 0.54, col_h, seed=idx * 10 + ci)

        p_obj = kit.finish_object(bm_p, f"Prop_Pillar_{idx:02d}", stone_mat)
        # Tô màu mặt hướng về tâm phòng đỏ sém hơn mặt ngoài
        def _paint_pillar(p, _n, center=Vector((cx, cy, 0.0)), inw=inward):
            to_p = (Vector((p.x, p.y, 0.0)) - center)
            facing = to_p.dot(inw)
            heat = brc.clamp01((facing + 0.5) / 1.5) * (1.0 - brc.clamp01(p.z / 4.5) * 0.5)
            return brc.mix(BASALT_DEEP, LAVA_EMBER, heat * 0.55)
        kit.paint(p_obj, _paint_pillar)
        objs.append(p_obj)

    # 2. 8 ụ ống khói địa nhiệt & tinh thể lưu huỳnh vàng-đỏ dọc tường (r = 27..29 m)
    for idx, ang_deg in enumerate(CRYSTAL_ANGLES_FROM_NORTH, 1):
        ang = math.radians(90.0 - ang_deg)
        rad = 27.8 + (idx % 2) * 0.8
        cx, cy = math.cos(ang) * rad, math.sin(ang) * rad

        bm_cr = bmesh.new()
        # Ống khói địa nhiệt bazan thấp + tinh thể lưu huỳnh mọc bên trong/xung quanh
        _add_hex_basalt_column(bm_cr, (cx, cy), 1.15, 1.85, seed=idx * 15)
        for k in range(5):
            oa = k * 1.25
            brc.add_hex_crystal(
                bm_cr,
                Vector((cx + math.cos(oa) * 0.75, cy + math.sin(oa) * 0.75, 0.2)),
                Vector((math.cos(oa) * 0.22, math.sin(oa) * 0.22, 0.96)).normalized(),
                rng.uniform(2.2, 3.8),
                rng.uniform(0.24, 0.42),
                seed=idx * 10 + k,
            )

        cr_obj = kit.finish_object(bm_cr, f"Prop_Crystal_{idx:02d}_Glow", glow_mat)
        kit.paint(cr_obj, lambda p, n: brc.mix(LAVA_ORANGE, SULFUR_YELLOW, brc.clamp01((p.z - 0.5) / 3.2)))
        objs.append(cr_obj)

    return objs


def _build_story_details_and_boss_nest(rng, stone_mat, glow_mat):
    objs = []
    bx, by, _ = BOSS_WAIT_POS

    # 1. Ổ Boss_Wait (0, 21, 0): Bệ đá bazan cuộn tròn viền quanh bởi mạch dung nham đỏ rực
    bm_nest = bmesh.new()
    bm_n_lava = bmesh.new()
    brc.add_angular_stone_block(bm_nest, (bx, by, 0.02), (7.0, 6.2, 0.14), (0.0, 0.0, 0.0), rock_seed=301, chip=0.06)

    # Vành đá bazan cuộn tròn ôm phía sau & hai bên ổ
    for bi in range(9):
        t = bi / 8.0
        ba = math.pi * (-0.15 + 1.30 * t)
        rx = bx + math.cos(ba) * 5.2
        ry = by + math.sin(ba) * 4.4
        _add_hex_basalt_column(bm_nest, (rx, ry), 0.78, 1.2 + 1.6 * math.sin(t * math.pi), seed=320 + bi)

    # Mạch dung nham phát sáng viền quanh ổ (<= 0.04 m)
    lava_pts = []
    lava_rads = []
    for li in range(14):
        t = li / 13.0
        la = math.pi * (-0.10 + 1.20 * t)
        lava_pts.append(Vector((bx + math.cos(la) * 4.2, by + math.sin(la) * 3.5, 0.02)))
        lava_rads.append(0.18)
    l_rgs = brc.parallel_tube_rings(bm_n_lava, lava_pts, lava_rads, 4)
    kit.bridge_rings(bm_n_lava, l_rgs)
    kit.cap_ring(bm_n_lava, l_rgs[0], flip=True)
    kit.cap_ring(bm_n_lava, l_rgs[-1])

    nest_obj = kit.finish_object(bm_nest, "Boss_Nest_BasaltCoil", stone_mat)
    kit.paint(nest_obj, lambda p, n: brc.mix(BASALT_DEEP, BASALT_MID, 0.5))
    objs.append(nest_obj)

    nlava_obj = kit.finish_object(bm_n_lava, "Boss_Nest_LavaRim_Glow", glow_mat)
    kit.paint(nlava_obj, lambda p, n: LAVA_ORANGE)
    objs.append(nlava_obj)

    # 2. Dấu vết Thân Nóng: Các rãnh trườn chữ S uốn lượn phát sáng đỏ cam sát sàn (<= 0.05 m)
    bm_trails = bmesh.new()
    trail_configs = [
        (0.0, -8.0, 14.0, 2.8, 0.25),
        (-6.0, -14.0, 10.0, 2.2, -0.35),
        (7.5, -6.0, 11.0, 2.4, 0.45),
    ]
    for tx, ty_start, length, amp, yaw in trail_configs:
        mat_t = Euler((0.0, 0.0, yaw), "XYZ").to_matrix().to_4x4()
        pts = []
        rads = []
        for si in range(16):
            t = si / 15.0
            lx = math.sin(t * math.pi * 2.5) * amp
            ly = t * length
            p = Vector((tx, ty_start, 0.02)) + (mat_t @ Vector((lx, ly, 0.0)))
            pts.append(p)
            rads.append(0.045 * math.sin(t * math.pi) + 0.015)
        rgs = brc.parallel_tube_rings(bm_trails, pts, rads, 4)
        kit.bridge_rings(bm_trails, rgs)
        kit.cap_ring(bm_trails, rgs[0], flip=True)
        kit.cap_ring(bm_trails, rgs[-1])

    trail_obj = kit.finish_object(bm_trails, "Story_SnakeLavaTrails_Glow", glow_mat)
    kit.paint(trail_obj, lambda p, n: brc.mix(LAVA_EMBER, LAVA_ORANGE, 0.7))
    objs.append(trail_obj)

    # 3. Xác vỏ rắn lột hóa đá/kết tinh dung nham khổng lồ dọc vách Đông-Bắc (r ~ 25 m)
    bm_shed = bmesh.new()
    shed_pts = []
    shed_rads = []
    for si in range(12):
        t = si / 11.0
        ang = math.radians(28.0 + t * 34.0)
        rad = 25.0 + 1.2 * math.sin(t * math.pi * 2.0)
        z = 0.35 + 0.65 * math.sin(t * math.pi)
        shed_pts.append(Vector((math.cos(ang) * rad, math.sin(ang) * rad, z)))
        shed_rads.append(0.95 * (1.0 - 0.35 * t))
    s_rgs = brc.parallel_tube_rings(bm_shed, shed_pts, shed_rads, 7, wobble=lambda p, a, i: 0.14 * math.sin(a * 3.0 + i))
    kit.bridge_rings(bm_shed, s_rgs)
    kit.cap_ring(bm_shed, s_rgs[0], flip=True)
    kit.cap_ring(bm_shed, s_rgs[-1])

    shed_obj = kit.finish_object(bm_shed, "Story_SnakeShedSkin", stone_mat)
    kit.paint(shed_obj, lambda p, n: brc.mix(BASALT_MID, SULFUR_YELLOW, 0.25))
    objs.append(shed_obj)

    # 4. Vỏ Cua Đá Nóng & Ốc Lưu Huỳnh bị nhiệt nung dính sát đất quanh ổ (<= 0.05 m)
    bm_prey = bmesh.new()
    for pi in range(14):
        ang = rng.uniform(0.0, 2.0 * math.pi)
        rad = rng.uniform(3.5, 8.5)
        px = bx + math.cos(ang) * rad
        py = by - 2.0 + math.sin(ang) * rad * 0.7
        brc.add_angular_stone_block(bm_prey, (px, py, 0.025), (0.42, 0.32, 0.05), (0.0, 0.0, rng.uniform(0.0, 6.28)), rock_seed=600 + pi, chip=0.14)
    prey_obj = kit.finish_object(bm_prey, "Story_SnakePreyShells", stone_mat)
    kit.paint(prey_obj, lambda p, n: brc.mix(BASALT_MID, LAVA_EMBER, 0.45))
    objs.append(prey_obj)

    return objs


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=None)
    ap.add_argument("--previews", default=None)
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    args = ap.parse_args(argv)

    t0 = time.time()
    rng = random.Random(20261126)
    kit.new_scene()

    stone_mat = brc.make_stone_material("SnakeRoom_StoneMat", roughness=0.78, bump_strength=0.72)
    glow_mat = brc.make_glow_material("SnakeRoom_GlowMat", energy=1.25)

    print("[Snake 1/3] Dựng vỏ phòng 64m, trần 31.5m, vòm Cột Bazan Nứt Dung Nham...", flush=True)
    shell_obj, col_obj = brc.build_watertight_shell(
        "BossRoom_Snake", stone_mat, _floor_z, _wall_disp, _paint_shell,
        wall_top_z=WALL_TOP_Z, dome_peak_z=DOME_PEAK_Z,
    )
    arch_objs = _build_arch_and_ceiling(rng, stone_mat, glow_mat)

    print("[Snake 2/3] Dựng 6 cụm cột bazan lục giác & 8 ụ ống khói lưu huỳnh...", flush=True)
    prop_objs = _build_pillars_and_crystals(rng, stone_mat, glow_mat)

    print("[Snake 3/3] Dựng ổ Boss_Wait, rãnh trườn chữ S, xác vỏ rắn lột...", flush=True)
    story_objs = _build_story_details_and_boss_nest(rng, stone_mat, glow_mat)

    if args.out:
        out_dir = os.path.abspath(args.out)
        room_objs = [shell_obj, col_obj] + arch_objs + prop_objs + story_objs
        kit.export_fbx(room_objs, os.path.join(out_dir, "BossRoom_Snake.fbx"))

    if args.previews:
        prev_dir = os.path.abspath(args.previews)
        os.makedirs(prev_dir, exist_ok=True)
        door_obj = brc.build_steel_door("BossRoom_SteelDoor")
        door_obj.location = Vector((0.0, brc.DOOR_Y, 4.4))

        lights = [
            ("dome_main", (0.0, 0.0, 23.5), (1.00, 0.45, 0.16), 5400.0, 10.0),
            ("dome_south", (0.0, -18.0, 14.0), (1.00, 0.40, 0.14), 2800.0, 6.0),
            ("dome_north", (0.0, 18.0, 14.0), (1.00, 0.36, 0.10), 3200.0, 6.0),
            ("door_l", (-3.5, -27.5, 4.2), (1.00, 0.32, 0.06), 1100.0, 2.0),
            ("door_r", (3.5, -27.5, 4.2), (1.00, 0.32, 0.06), 1100.0, 2.0),
            ("boss_nest", (0.0, 19.0, 5.0), (1.00, 0.38, 0.08), 2200.0, 2.8),
        ]
        cam, scene = brc.setup_cycles_scene((0.022, 0.008, 0.006), 0.35, lights)
        brc.render_view(cam, scene, (0.0, -25.8, 2.4), (0.0, 10.0, 2.5), 18.0,
                        os.path.join(prev_dir, "boss_room_snake_entry_view.png"))
        brc.render_view(cam, scene, (3.6, -14.5, 3.0), (-1.5, -29.0, 3.2), 19.0,
                        os.path.join(prev_dir, "boss_room_snake_door_view.png"))
        brc.render_view(cam, scene, (8.0, 9.0, 3.6), (0.0, 20.5, 2.0), 21.0,
                        os.path.join(prev_dir, "boss_room_snake_nest_view.png"))
        brc.render_view(cam, scene, (2.2, -25.0, 20.0), (0.0, 4.0, 1.0), 15.0,
                        os.path.join(prev_dir, "boss_room_snake_overview.png"))

    print(f"Hoàn tất Phòng Rắn Dung Nham trong {time.time() - t0:.1f}s", flush=True)


if __name__ == "__main__":
    main()
