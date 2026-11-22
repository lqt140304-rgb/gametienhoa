"""Dựng Phòng Trùm 3: Bọ Cạp Thạch Anh ("Bãi Cát Tinh Thể") — Blender headless.

Tuân thủ docs/BOSS_ROOMS_BRIEF.md mục 4.3 & thiết kế Bọ Cạp Thạch Anh:
- Chủ đề: bãi cát và đá sa thạch nâu đỏ phân tầng xói mòn, tinh thể thạch anh tím mọc lởm chởm.
- Kích thước: sàn phẳng bán kính 32 m (đường kính 64 m), vách 25 m, trần kín 31.5 m (>= 24 m và >= 30 m).
- Cổng vào Nam (4.8 x 7.2 m): vòm đá cát bị gió xói mòn phân tầng + tinh thể tím hồng.
- Vùng cát lún trung tâm-Bắc (r = 8.5 m tại (0, 10, 0)) làm chỗ Boss_Wait chôn mình trồi lên.
- 6 tảng đá thấp Prop_Pillar_01..06 (cao 1.4..2.0 m) làm chỗ núp Phóng Gai / đâm kẹt càng; 8 cụm Prop_Crystal_01..08.

Chạy:
  D:/blender/blender.exe -b -P tools/blender/build_boss_room_scorpion.py -- --out Assets/_Project/Models/Underroot --previews docs/art/previews
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


WALL_TOP_Z = 24.5
DOME_PEAK_Z = 31.5
SAND_PIT_CENTER = (0.0, 10.0)
SAND_PIT_RADIUS = 8.5
BOSS_WAIT_POS = (0.0, 10.0, 0.0)

LOW_ROCK_SPECS = [
    (42.0, 16.5, 1.85),
    (92.0, 17.8, 1.55),
    (140.0, 15.5, 1.75),
    (220.0, 15.5, 1.65),
    (268.0, 17.8, 1.95),
    (318.0, 16.5, 1.50),
]
CRYSTAL_ANGLES_FROM_NORTH = [22.0, 62.0, 110.0, 148.0, 212.0, 250.0, 298.0, 338.0]

# Bảng màu hang Bọ Cạp: cát nâu đỏ & sa thạch xói mòn + tinh thể tím thạch anh lởm chởm
SAND_DARK = (0.145, 0.058, 0.032)
SAND_MID = (0.245, 0.105, 0.052)
SAND_LIGHT = (0.345, 0.165, 0.082)
QUICKSAND_RIM = (0.285, 0.140, 0.068)
QUICKSAND_CENTER = (0.110, 0.042, 0.025)

ROCK_RED_DEEP = (0.068, 0.024, 0.016)
ROCK_RED_MID = (0.165, 0.062, 0.034)
ROCK_OCHRE = (0.280, 0.118, 0.054)

AMETHYST_DEEP = (0.24, 0.03, 0.48)
AMETHYST_BRIGHT = (0.64, 0.14, 0.92)
AMETHYST_TIP = (0.94, 0.42, 1.00)


def _floor_z(x, y):
    r = math.hypot(x, y)
    d_door = math.hypot(x, y - brc.DOOR_Y)
    d_pit = math.hypot(x - SAND_PIT_CENTER[0], y - SAND_PIT_CENTER[1])

    # Vùng cát lún trũng cực nhẹ hình chảo êm (<= 0.12 m, không cản bước)
    pit_dip = -0.12 * (1.0 - brc.smoothstep(2.0, SAND_PIT_RADIUS, d_pit))

    # Gợn sóng cồn cát nhẹ ở phần ngoài
    flat_mask = brc.smoothstep(3.0, 6.5, r) * brc.smoothstep(4.5, 9.0, d_door) * brc.smoothstep(SAND_PIT_RADIUS, SAND_PIT_RADIUS + 3.0, d_pit)
    if r > brc.ROOM_RADIUS:
        flat_mask *= 1.0 - brc.smoothstep(brc.ROOM_RADIUS, brc.ROOM_RADIUS + 2.5, r)
    dune = 0.045 * math.sin(x * 0.25 + y * 0.15) + 0.025 * noise.noise(Vector((x * 0.14, y * 0.14, 2.3)))
    return pit_dip + dune * flat_mask


def _wall_disp(px, py, z_base, r_scale, li):
    # Vỉa đá cát phân tầng xói mòn ngang rõ nét
    strata = brc.stepped(0.5 + 0.5 * math.sin(z_base * 0.42 + noise.noise(Vector((px * 0.05, py * 0.05, 0.0))) * 1.4), steps=6.0, sharp=0.84) - 0.5
    wind = noise.noise(Vector((px * 0.08, py * 0.08, z_base * 0.05))) * 1.2
    r_disp = (strata * 2.1 + wind) * (0.35 if r_scale < 0.7 else 1.0)
    z_disp = noise.noise(Vector((px * 0.09, py * 0.09, 6.7))) * 1.1 * r_scale if li > 9 else 0.0
    return r_disp, z_disp


def _paint_shell(pos, _n):
    r = math.hypot(pos.x, pos.y)
    d_pit = math.hypot(pos.x - SAND_PIT_CENTER[0], pos.y - SAND_PIT_CENTER[1])
    n_c = noise.noise(Vector((pos.x * 0.16, pos.y * 0.16, pos.z * 0.16))) * 0.5 + 0.5
    n_f = brc.ridged(Vector((pos.x * 0.35, pos.y * 0.35, pos.z * 0.35)))

    if pos.z < 0.45 and r < brc.ROOM_RADIUS + 0.5:
        ripple = 0.5 + 0.5 * math.sin(pos.x * 0.8 + pos.y * 0.35 + n_c * 2.5)
        c = brc.mix(SAND_MID, SAND_LIGHT, ripple * 0.48 + n_c * 0.30)
        # Vòng xoáy cát lún quanh Boss_Wait (0, 10)
        if d_pit < SAND_PIT_RADIUS + 1.2:
            swirl = 0.5 + 0.5 * math.sin(d_pit * 2.4 + math.atan2(pos.y - SAND_PIT_CENTER[1], pos.x - SAND_PIT_CENTER[0]) * 2.0)
            pit_t = 1.0 - brc.smoothstep(0.0, SAND_PIT_RADIUS, d_pit)
            pit_col = brc.mix(QUICKSAND_RIM, QUICKSAND_CENTER, brc.clamp01(pit_t * 0.8 + swirl * 0.35))
            c = brc.mix(c, pit_col, 1.0 - brc.smoothstep(SAND_PIT_RADIUS - 1.5, SAND_PIT_RADIUS + 1.2, d_pit))
        return c

    # Vách đá cát phân tầng xói mòn (dải sáng tối ngang rõ)
    band = 0.5 + 0.5 * math.sin(pos.z * 0.65 + n_c * 1.8)
    h_t = brc.clamp01(pos.z / DOME_PEAK_Z)
    c = brc.mix(ROCK_RED_MID, ROCK_OCHRE, band * 0.65)
    c = brc.mix(c, ROCK_RED_DEEP, h_t * 0.72 + (1.0 - n_f) * 0.20)
    return c


def _build_eroded_arch_and_ribs(rng, stone_mat, glow_mat):
    """Khung vòm đá cát bị gió xói mòn ở cửa Nam + gân đá sa thạch & tinh thể tím rủ trần."""
    bm_arch_stone = bmesh.new()
    bm_arch_glow = bmesh.new()

    # Hai bên cửa: các phiến đá cát xếp tầng ngang xói mòn lệch bậc bất đối xứng
    for side in (-1.0, 1.0):
        for ti in range(5):
            tz = 0.7 + ti * 1.55
            wx = 2.4 + 0.45 * math.sin(ti * 1.4 + side)
            wy = 2.6 - 0.15 * ti
            px = side * (3.3 + 0.25 * math.cos(ti * 1.7 + side))
            brc.add_angular_stone_block(bm_arch_stone, (px, brc.DOOR_Y + 0.65, tz), (wx, wy, 1.55), (0.0, side * 0.04, side * 0.08), rock_seed=30 + ti * 5 + int(side + 2), chip=0.18)

        brc.add_angular_stone_block(bm_arch_stone, (side * 5.0, brc.DOOR_Y + 0.35, 5.2), (2.7, 2.3, 11.2), (0.04, -side * 0.08, side * 0.15), rock_seed=80 + int(side + 2), chip=0.20)

        for ki in range(3):
            brc.add_hex_crystal(
                bm_arch_glow,
                Vector((side * 3.1, brc.DOOR_Y + 1.45, 2.0 + ki * 2.3)),
                Vector((-side * 0.35, 0.72, 0.58)).normalized(),
                1.5 + ki * 0.22,
                0.22,
                seed=ki * 9 + int(side + 2),
            )

    # Dầm đá cát bị gió khoét mòn xếp tầng phía trên cửa
    for vi, (vx, vz, sx, sy, sz, rot_y) in enumerate((
        (-2.4, 7.6, 2.8, 2.5, 1.4, -0.25),
        (2.4, 7.6, 2.8, 2.5, 1.4, 0.25),
        (-1.1, 8.9, 3.2, 2.4, 1.4, -0.10),
        (1.1, 8.9, 3.2, 2.4, 1.4, 0.10),
        (0.0, 10.6, 6.8, 2.5, 2.6, 0.0),
        (0.0, 13.0, 5.2, 2.3, 2.8, 0.0),
    )):
        brc.add_angular_stone_block(bm_arch_stone, (vx, brc.DOOR_Y + 0.72, vz), (sx, sy, sz), (0.05, rot_y, 0.0), rock_seed=150 + vi * 7, chip=0.18)

    for ki, (cx, cz, dx) in enumerate(((-1.3, 9.6, -0.3), (0.0, 10.2, 0.0), (1.3, 9.6, 0.3))):
        brc.add_hex_crystal(bm_arch_glow, Vector((cx, brc.DOOR_Y + 1.65, cz)), Vector((dx, 0.65, 0.75)).normalized(), 2.3 if ki == 1 else 1.65, 0.28 if ki == 1 else 0.20, seed=500 + ki)

    arch_stone = kit.finish_object(bm_arch_stone, "Door_SandArch", stone_mat)
    for p in arch_stone.data.polygons:
        p.use_smooth = False
    kit.paint(arch_stone, lambda p, n: brc.mix(ROCK_RED_MID, ROCK_OCHRE, 0.5 + 0.45 * math.sin(p.z * 1.4)))

    arch_glow = kit.finish_object(bm_arch_glow, "Door_SandArch_Glow", glow_mat)
    for p in arch_glow.data.polygons:
        p.use_smooth = False
    kit.paint(arch_glow, lambda p, n: brc.mix(AMETHYST_DEEP, AMETHYST_TIP, brc.clamp01((p.z - 1.5) / 8.5)))

    # Gân đá xói mòn & tinh thể tím rủ trần cao
    bm_ribs = bmesh.new()
    bm_veins = bmesh.new()
    for ri, adeg in enumerate((25.0, 68.0, 112.0, 150.0, 210.0, 248.0, 292.0, 335.0)):
        rad = math.radians(adeg)
        dx, dy = math.sin(rad), math.cos(rad)
        pts, rads = [], []
        for s in range(10):
            t = s / 9.0
            rr = brc.ROOM_RADIUS * (1.0 - 0.86 * (t ** 1.55)) + 0.35 * math.sin(t * math.pi)
            zz = -0.3 + t * (DOME_PEAK_Z - 0.5) + 4.2 * math.sin(t * math.pi * 0.85)
            pts.append(Vector((dx * rr, dy * rr, min(DOME_PEAK_Z - 0.4, zz))))
            rads.append(1.7 - 0.7 * t + 0.16 * math.sin(s * 1.5 + ri))
        rgs = brc.parallel_tube_rings(bm_ribs, pts, rads, 7, wobble=lambda p, a, idx: 0.15 * math.sin(a * 3.0 + idx))
        kit.bridge_rings(bm_ribs, rgs)
        kit.cap_ring(bm_ribs, rgs[0], flip=True)
        kit.cap_ring(bm_ribs, rgs[-1])

        for ci in range(2, 8, 2):
            anchor = pts[ci]
            inward = Vector((-dx * 0.85, -dy * 0.85, -0.40 if ci > 5 else 0.35)).normalized()
            for j in range(2):
                jd = (inward + Vector((rng.uniform(-0.28, 0.28), rng.uniform(-0.28, 0.28), rng.uniform(-0.2, 0.2)))).normalized()
                brc.add_hex_crystal(bm_veins, anchor + inward * 0.4, jd, rng.uniform(2.0, 4.0), rng.uniform(0.28, 0.52), seed=ri * 20 + ci + j)

    ribs_obj = kit.finish_object(bm_ribs, "Cavern_SandstoneRibs", stone_mat)
    for p in ribs_obj.data.polygons:
        p.use_smooth = False
    kit.paint(ribs_obj, lambda p, n: brc.mix(ROCK_RED_DEEP, ROCK_OCHRE, 0.35 + 0.35 * math.sin(p.z * 0.6)))

    veins_obj = kit.finish_object(bm_veins, "Cavern_AmethystVeins_Glow", glow_mat)
    for p in veins_obj.data.polygons:
        p.use_smooth = False
    kit.paint(veins_obj, lambda p, n: brc.mix(AMETHYST_DEEP, AMETHYST_TIP, brc.clamp01(0.4 + 0.5 * noise.noise(p * 0.3))))

    return [arch_stone, arch_glow, ribs_obj, veins_obj]


def _build_low_rocks_crystals_and_sandpit(rng, stone_mat, glow_mat):
    """6 tảng đá thấp Prop_Pillar_01..06 (cao 1.5..1.95 m làm chỗ núp Phóng Gai / kẹt càng), 8 cụm tinh thể tím Prop_Crystal_01..08."""
    objs = []

    # 1. 6 Tảng đá thấp (1-2 m)
    for idx, (adeg, rdist, height) in enumerate(LOW_ROCK_SPECS, start=1):
        rad = math.radians(adeg)
        rx = math.sin(rad) * rdist
        ry = math.cos(rad) * rdist
        rz = _floor_z(rx, ry)

        bm_rock = bmesh.new()
        bm_rock_glow = bmesh.new()
        brc.add_rock_cluster(bm_rock, (0.0, 0.0, height * 0.42), (1.75, 1.45, height * 0.68), seed=100 + idx, count=3)

        # Vài gai tinh thể tím nhỏ mọc bên cạnh đá thấp (không cao quá 2.1 m)
        for ci in range(3):
            ca = rad + (ci - 1) * 0.9
            brc.add_hex_crystal(bm_rock_glow, Vector((math.cos(ca) * 1.1, math.sin(ca) * 1.1, 0.3)), Vector((math.cos(ca) * 0.45, math.sin(ca) * 0.45, 0.85)).normalized(), 1.15, 0.18, seed=idx * 11 + ci)

        pname = f"Prop_Pillar_{idx:02d}"
        r_obj = kit.finish_object(bm_rock, pname, stone_mat)
        r_obj.location = Vector((rx, ry, rz))
        for p in r_obj.data.polygons:
            p.use_smooth = False
        kit.paint(r_obj, lambda p, n, seed=idx: brc.mix(ROCK_RED_DEEP, ROCK_OCHRE, 0.45 + 0.35 * brc.ridged(p * 0.6 + Vector((seed, 0, 0)))))

        rg_obj = kit.finish_object(bm_rock_glow, f"{pname}_Glow", glow_mat)
        rg_obj.parent = r_obj
        for p in rg_obj.data.polygons:
            p.use_smooth = False
        kit.paint(rg_obj, lambda p, n: brc.mix(AMETHYST_BRIGHT, AMETHYST_TIP, brc.clamp01(p.z / 1.8)))

        objs.extend([r_obj, rg_obj])

    # 2. 8 Cụm tinh thể tím mọc lởm chởm ven vách Prop_Crystal_01..08
    for idx, adeg in enumerate(CRYSTAL_ANGLES_FROM_NORTH, start=1):
        rad = math.radians(adeg)
        cr = 27.2 + (idx % 2) * 1.3
        cx = math.sin(rad) * cr
        cy = math.cos(rad) * cr
        cz = _floor_z(cx, cy)

        bm_base = bmesh.new()
        bm_shards = bmesh.new()
        brc.add_rock_cluster(bm_base, (0.0, 0.0, 0.45), (2.0, 1.7, 0.9), seed=500 + idx, count=3)

        inward_ang = math.atan2(-cy, -cx)
        for si in range(8):
            a_off = (si - 3.5) * 0.38
            dv = Vector((math.cos(inward_ang + a_off) * (0.30 + 0.10 * (si % 3)), math.sin(inward_ang + a_off) * (0.30 + 0.10 * (si % 3)), 0.85)).normalized()
            brc.add_hex_crystal(bm_shards, Vector((math.cos(si * 1.2) * 0.6, math.sin(si * 1.2) * 0.6, 0.3)), dv, 3.0 + (si % 3) * 1.35, 0.34 - (si % 2) * 0.06, seed=idx * 23 + si)

        cname = f"Prop_Crystal_{idx:02d}"
        c_base = kit.finish_object(bm_base, cname, stone_mat)
        c_base.location = Vector((cx, cy, cz))
        for p in c_base.data.polygons:
            p.use_smooth = False
        kit.paint(c_base, lambda p, n: brc.mix(ROCK_RED_DEEP, ROCK_OCHRE, 0.45 * brc.ridged(p * 0.7)))

        c_glow = kit.finish_object(bm_shards, f"{cname}_Glow", glow_mat)
        c_glow.parent = c_base
        for p in c_glow.data.polygons:
            p.use_smooth = False
        kit.paint(c_glow, lambda p, n: brc.mix(AMETHYST_DEEP, AMETHYST_TIP, brc.clamp01(p.z / 5.2)))

        objs.extend([c_base, c_glow])

    # 3. Gờ cát thấp bao quanh rìa vùng cát lún (r = 8.5 m tại (0, 10), cao <= 0.18 m, đi bộ qua êm) + gai tím nhỏ đánh dấu vùng cát
    bm_rim = bmesh.new()
    bm_rim_glow = bmesh.new()
    for k in range(16):
        ang = k * (2.0 * math.pi / 16.0)
        rx = SAND_PIT_CENTER[0] + math.cos(ang) * SAND_PIT_RADIUS
        ry = SAND_PIT_CENTER[1] + math.sin(ang) * SAND_PIT_RADIUS
        brc.add_rock_cluster(bm_rim, (rx, ry, 0.05), (0.95, 0.70, 0.18), seed=800 + k, count=1)
        if k % 2 == 0:
            brc.add_hex_crystal(bm_rim_glow, Vector((rx, ry, 0.05)), Vector((math.cos(ang) * 0.25, math.sin(ang) * 0.25, 0.95)).normalized(), 0.75, 0.12, seed=850 + k)

    rim_obj = kit.finish_object(bm_rim, "SandPit_RimMarkers", stone_mat)
    for p in rim_obj.data.polygons:
        p.use_smooth = False
    kit.paint(rim_obj, lambda p, n: brc.mix(QUICKSAND_RIM, ROCK_OCHRE, 0.5))

    rim_glow = kit.finish_object(bm_rim_glow, "SandPit_RimCrystals_Glow", glow_mat)
    for p in rim_glow.data.polygons:
        p.use_smooth = False
    kit.paint(rim_glow, lambda p, n: brc.mix(AMETHYST_BRIGHT, AMETHYST_TIP, brc.clamp01(p.z / 0.8)))

    objs.extend([rim_obj, rim_glow])
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

    stone_mat = brc.make_stone_material("ScorpionRoom_SandMat", roughness=0.88, bump_strength=0.68)
    glow_mat = brc.make_glow_material("ScorpionRoom_AmethystGlowMat", energy=0.95)

    print("[Bọ Cạp 1/3] Dựng vỏ hang cát nâu đỏ 64m, trần 31.5m, hốc cửa Nam 4.8x7.2m...", flush=True)
    shell_obj, col_obj = brc.build_watertight_shell(
        "BossRoom_Scorpion", stone_mat, _floor_z, _wall_disp, _paint_shell,
        wall_top_z=WALL_TOP_Z, dome_peak_z=DOME_PEAK_Z,
    )
    arch_objs = _build_eroded_arch_and_ribs(rng, stone_mat, glow_mat)

    print("[Bọ Cạp 2/3] Dựng vùng cát lún (r=8.5m), 6 tảng đá thấp (1.5-2m) & 8 cụm tinh thể tím...", flush=True)
    prop_objs = _build_low_rocks_crystals_and_sandpit(rng, stone_mat, glow_mat)

    door_obj = brc.build_steel_door("BossRoom_SteelDoor")

    if args.out:
        out_dir = os.path.abspath(args.out)
        room_objs = [shell_obj, col_obj] + arch_objs + prop_objs
        kit.export_fbx(room_objs, os.path.join(out_dir, "BossRoom_Scorpion.fbx"))

    if args.previews:
        prev_dir = os.path.abspath(args.previews)
        os.makedirs(prev_dir, exist_ok=True)
        door_obj.location = Vector((0.0, brc.DOOR_Y, 4.4))
        lights = [
            ("dome_warm", (0.0, 0.0, 26.0), (1.00, 0.66, 0.42), 6200.0, 10.0),
            ("dome_south", (0.0, -18.0, 14.0), (0.95, 0.50, 0.34), 3200.0, 6.0),
            ("dome_north", (0.0, 18.0, 14.0), (0.78, 0.38, 0.92), 3400.0, 6.0),
            ("door_l", (-3.8, -27.5, 4.5), (0.85, 0.30, 0.95), 1100.0, 2.0),
            ("door_r", (3.8, -27.5, 4.5), (0.85, 0.30, 0.95), 1100.0, 2.0),
            ("sand_pit", (0.0, 10.0, 4.5), (1.00, 0.56, 0.32), 2000.0, 3.5),
        ]
        cam, scene = brc.setup_cycles_scene((0.03, 0.015, 0.018), 0.35, lights)

        brc.render_view(cam, scene, (0.0, -25.8, 2.5), (0.0, 10.0, 1.8), 17.0,
                        os.path.join(prev_dir, "boss_room_scorpion_entry_view.png"))
        brc.render_view(cam, scene, (2.8, -14.5, 3.0), (0.0, brc.DOOR_Y, 4.5), 19.0,
                        os.path.join(prev_dir, "boss_room_scorpion_door_view.png"))
        brc.render_view(cam, scene, (-10.0, -6.0, 5.5), (0.0, 11.0, 0.5), 20.0,
                        os.path.join(prev_dir, "boss_room_scorpion_sand_pit.png"))
        brc.render_view(cam, scene, (2.0, -24.5, 21.0), (0.0, 6.0, 1.0), 15.0,
                        os.path.join(prev_dir, "boss_room_scorpion_overview.png"))

    print(f"Hoàn tất Phòng Bọ Cạp trong {time.time() - t0:.1f}s", flush=True)


if __name__ == "__main__":
    main()
