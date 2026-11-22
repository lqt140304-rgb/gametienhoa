"""Dựng Phòng Trùm Đợt 2 - Phòng 3: Cóc Hồ Già ("Đầm Rễ Cổ") — Blender headless.

Tuân thủ docs/superpowers/specs/2026-11-22-boss-rooms-wave2-design.md:
- Sàn phẳng bán kính 32 m (đường kính 64 m), trần kín 32.0 m (mạng rễ cổ thụ đan kín 100%, không lỗ).
- Cửa vào duy nhất phía Nam (4.8 x 7.2 m), vòm Rễ Cổ Thụ Đan Đá kèm nấm & tinh thể phát quang xanh lục.
- Kể chuyện môi trường:
  + Ổ Boss_Wait (0, 20, 0): đệm bùn rêu nông ôm giữa 2 chùm rễ cổ thụ, vương vãi vỏ Ốc Hồ, Bọ Cánh Cứng, Thằn Lằn.
  + Dấu vết Lưỡi Bắn (6-12 m): các rãnh trượt bùn thẳng tắp hướng về ổ Cóc (<= 0.04 m).
  + Dấu vết Da Nhầy, Phình Nổ & Ợ Chấn: các vũng bùn/chất nhầy phát quang xanh lục đục sát sàn (<= 0.05 m).
- Vật cản chiến đấu: 6 trụ rễ đại thụ đan đá rêu (Prop_Pillar_01..06, r = 13..17 m, cao 1.8-2.8 m),
  8 cụm nấm & quặng sinh học phát quang xanh cốm - vàng dịu (Prop_Crystal_01..08, r = 27..29 m).

Chạy:
  D:/blender/blender.exe -b -P tools/blender/build_boss_room_toad.py -- --out Assets/_Project/Models/Underroot --previews docs/art/previews
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


WALL_TOP_Z = 23.0
DOME_PEAK_Z = 32.0
BOSS_WAIT_POS = (0.0, 20.0, 0.0)

PILLAR_ANGLES_FROM_NORTH = [34.0, 86.0, 140.0, 220.0, 274.0, 326.0]
PILLAR_RADII = [14.5, 13.8, 16.5, 16.2, 13.5, 14.8]
CRYSTAL_ANGLES_FROM_NORTH = [18.0, 58.0, 106.0, 150.0, 210.0, 254.0, 302.0, 342.0]

# Bảng màu: đá bùn nâu lục ẩm, rễ gỗ sẫm, ánh xanh lục - vàng cốm sinh học
MUD_DEEP = (0.022, 0.028, 0.018)
MUD_BROWN = (0.058, 0.055, 0.036)
MOSS_GREEN = (0.068, 0.115, 0.042)
ROOT_DARK = (0.045, 0.030, 0.020)
ROOT_BARK = (0.095, 0.068, 0.042)

BIO_GREEN = (0.28, 0.85, 0.22)
BIO_LIME = (0.68, 0.94, 0.18)
SLIME_GLOW = (0.20, 0.58, 0.16)
SHELL_PALE = (0.42, 0.38, 0.28)


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
    n = 0.032 * math.sin(x * 0.18 + 0.5) * math.cos(y * 0.18 - 0.3) + 0.020 * noise.noise(Vector((x * 0.14, y * 0.14, 1.2)))
    return n * flat_mask


def _wall_disp(px, py, z_base, r_scale, li):
    rock_n = brc.stepped(0.5 + 0.5 * noise.noise(Vector((px * 0.08, py * 0.08, z_base * 0.10))), steps=4.0, sharp=0.62) - 0.5
    crag = (brc.ridged(Vector((px * 0.07, py * 0.07, z_base * 0.09))) - 0.5) * 1.6
    r_disp = (rock_n * 1.7 + crag) * (0.45 if r_scale < 0.7 else 1.0)
    z_disp = noise.noise(Vector((px * 0.09, py * 0.09, 3.3))) * 1.2 * r_scale if li > 9 else 0.0
    return r_disp, z_disp


def _paint_shell(pos, _n):
    r = math.hypot(pos.x, pos.y)
    n_c = noise.noise(Vector((pos.x * 0.14, pos.y * 0.14, pos.z * 0.14))) * 0.5 + 0.5
    n_f = brc.ridged(Vector((pos.x * 0.35, pos.y * 0.35, pos.z * 0.35)))

    if pos.z < 0.45 and r < brc.ROOM_RADIUS + 0.5:
        c = brc.mix(MUD_BROWN, MOSS_GREEN, n_c * 0.52)
        if n_f > 0.65:
            c = brc.mix(c, MUD_DEEP, (n_f - 0.65) * 1.8)
        return c

    h_t = brc.clamp01(pos.z / DOME_PEAK_Z)
    c = brc.mix(MUD_BROWN, MUD_DEEP, h_t * 0.85)
    c = brc.mix(c, MOSS_GREEN, n_f * 0.38 * (1.0 - h_t * 0.4))
    return c


def _add_mushroom_cap(bm, base, radius, height, seed=0):
    """Thêm 1 cây nấm phát quang low-poly (thân + mũ nấm)."""
    stem_pts = [Vector(base), Vector(base) + Vector((0.0, 0.0, height * 0.85))]
    rgs = brc.parallel_tube_rings(bm, stem_pts, [radius * 0.22, radius * 0.16], 6)
    kit.bridge_rings(bm, rgs)
    kit.cap_ring(bm, rgs[0], flip=True)

    top = Vector(base) + Vector((0.0, 0.0, height * 0.85))
    cap_pts = [top - Vector((0.0, 0.0, height * 0.10)), top + Vector((0.0, 0.0, height * 0.25))]
    c_rgs = brc.parallel_tube_rings(bm, cap_pts, [radius, radius * 0.38], 8, wobble=lambda p, a, i: 0.08 * math.sin(a * 3.0 + seed))
    kit.bridge_rings(bm, c_rgs)
    kit.cap_ring(bm, c_rgs[0], flip=True)
    kit.cap_ring(bm, c_rgs[-1])


def _build_arch_and_canopy_roots(rng, stone_mat, root_mat, glow_mat):
    bm_arch = bmesh.new()
    bm_roots = bmesh.new()
    bm_glow = bmesh.new()

    # 1. Vòm Rễ Cổ Thụ Đan Đá tại lối vào Nam
    for side in (-1.0, 1.0):
        px = side * 3.25
        for tz, wx, hh in ((1.0, 2.2, 2.0), (3.4, 2.0, 2.5), (5.8, 2.1, 2.4)):
            brc.add_angular_stone_block(bm_arch, (px, brc.DOOR_Y + 0.70, tz), (wx, 2.6, hh), (0.0, 0.0, side * 0.10), rock_seed=30 + int(tz * 3) + int(side + 2), chip=0.14)

        # Chùm rễ cổ thụ to uốn lượn quấn quanh cột đá và đan lên đỉnh vòm
        for ri in range(3):
            r_pts = [
                Vector((px + side * (0.4 + ri * 0.45), brc.DOOR_Y + 0.5 + ri * 0.3, 0.0)),
                Vector((px - side * (0.1 + ri * 0.15), brc.DOOR_Y + 0.9 + ri * 0.2, 3.2)),
                Vector((px - side * (0.6 + ri * 0.25), brc.DOOR_Y + 0.75, 6.4)),
                Vector((side * (1.8 - ri * 0.5), brc.DOOR_Y + 0.80, 8.8 + ri * 0.3)),
                Vector((-side * 0.6, brc.DOOR_Y + 0.85, 9.4)),
            ]
            rgs = brc.parallel_tube_rings(bm_roots, r_pts, [0.58, 0.46, 0.42, 0.36, 0.24], 7, wobble=lambda p, a, i: 0.10 * math.sin(a * 2.0 + i))
            kit.bridge_rings(bm_roots, rgs)
            kit.cap_ring(bm_roots, rgs[0], flip=True)
            kit.cap_ring(bm_roots, rgs[-1])

        # Nấm & tinh thể phát quang xanh lục bám trên vòm rễ
        for ki in range(3):
            brc.add_hex_crystal(bm_glow, Vector((px - side * 0.3, brc.DOOR_Y + 1.4, 2.0 + ki * 2.2)), Vector((-side * 0.35, 0.72, 0.58)).normalized(), 1.25, 0.22, seed=ki * 4 + int(side + 2))

    # 2. Mạng rễ đại thụ đan dọc trần vòm kín (vắt ngang ở độ cao > 23 m)
    for ri in range(10):
        ang = math.radians(ri * 36.0 + 12.0)
        p0 = Vector((math.cos(ang) * 30.5, math.sin(ang) * 30.5, WALL_TOP_Z - 1.5))
        p1 = Vector((math.cos(ang + 0.25) * 18.0, math.sin(ang + 0.25) * 18.0, WALL_TOP_Z + 3.5))
        p2 = Vector((math.cos(ang + 0.85) * 8.0, math.sin(ang + 0.85) * 8.0, DOME_PEAK_Z - 1.8))
        p3 = Vector((math.cos(ang + 1.95) * 20.0, math.sin(ang + 1.95) * 20.0, WALL_TOP_Z + 2.5))
        rgs = brc.parallel_tube_rings(bm_roots, [p0, p1, p2, p3], [0.95, 0.72, 0.52, 0.75], 7)
        kit.bridge_rings(bm_roots, rgs)
        kit.cap_ring(bm_roots, rgs[0], flip=True)
        kit.cap_ring(bm_roots, rgs[-1])

    arch_obj = kit.finish_object(bm_arch, "BossRoom_Toad_Arch", stone_mat)
    kit.paint(arch_obj, lambda p, n: brc.mix(MUD_BROWN, MOSS_GREEN, 0.4 + 0.3 * brc.ridged(p * 0.3)))

    roots_obj = kit.finish_object(bm_roots, "BossRoom_Toad_RootsCanopy", root_mat)
    kit.paint(roots_obj, lambda p, n: brc.mix(ROOT_DARK, ROOT_BARK, 0.45 + 0.4 * brc.ridged(p * 0.4)))

    glow_obj = kit.finish_object(bm_glow, "BossRoom_Toad_ArchGlow", glow_mat)
    kit.paint(glow_obj, lambda p, n: brc.mix(BIO_GREEN, BIO_LIME, 0.5 + 0.5 * math.sin(p.z)))

    return [arch_obj, roots_obj, glow_obj]


def _build_pillars_and_crystals(rng, stone_mat, root_mat, glow_mat):
    objs = []

    # 1. 6 trụ rễ đại thụ đan vào tảng đá rêu (r = 13..17 m, cao 1.8-2.8 m)
    for idx, (ang_deg, rad) in enumerate(zip(PILLAR_ANGLES_FROM_NORTH, PILLAR_RADII), 1):
        ang = math.radians(90.0 - ang_deg)
        cx, cy = math.cos(ang) * rad, math.sin(ang) * rad
        top_h = 2.0 + (idx % 3) * 0.38

        bm_p = bmesh.new()
        bm_pr = bmesh.new()
        # Khối đá rêu ở lõi cột
        brc.add_rock_cluster(bm_p, (cx, cy, top_h * 0.45), (1.75, 1.75, top_h * 0.62), seed=idx * 19, count=3)

        # 3 sợi rễ to đan xoắn quanh tảng đá rêu
        for ri in range(3):
            ra0 = ang + ri * (2.0 * math.pi / 3.0)
            r_pts = [
                Vector((cx + math.cos(ra0) * 1.9, cy + math.sin(ra0) * 1.9, 0.0)),
                Vector((cx + math.cos(ra0 + 0.6) * 1.5, cy + math.sin(ra0 + 0.6) * 1.5, top_h * 0.55)),
                Vector((cx + math.cos(ra0 + 1.2) * 0.9, cy + math.sin(ra0 + 1.2) * 0.9, top_h * 1.05)),
            ]
            rgs = brc.parallel_tube_rings(bm_pr, r_pts, [0.42, 0.34, 0.25], 6)
            kit.bridge_rings(bm_pr, rgs)
            kit.cap_ring(bm_pr, rgs[0], flip=True)
            kit.cap_ring(bm_pr, rgs[-1])

        p_obj = kit.finish_object(bm_p, f"Prop_Pillar_{idx:02d}", stone_mat)
        kit.paint(p_obj, lambda p, n: brc.mix(MUD_BROWN, MOSS_GREEN, brc.clamp01(p.z / 2.6) * 0.65 + 0.2))

        pr_obj = kit.finish_object(bm_pr, f"Prop_Pillar_{idx:02d}_Roots", root_mat)
        pr_obj.parent = p_obj
        kit.paint(pr_obj, lambda p, n: brc.mix(ROOT_DARK, ROOT_BARK, 0.55))
        objs.append(p_obj)

    # 2. 8 cụm nấm & quặng sinh học phát quang xanh cốm - vàng dịu dọc tường (r = 27..29 m)
    for idx, ang_deg in enumerate(CRYSTAL_ANGLES_FROM_NORTH, 1):
        ang = math.radians(90.0 - ang_deg)
        rad = 27.8 + (idx % 2) * 0.8
        cx, cy = math.cos(ang) * rad, math.sin(ang) * rad

        bm_cr = bmesh.new()
        for k in range(4):
            off_a = k * 1.55
            mx = cx + math.cos(off_a) * 0.9
            my = cy + math.sin(off_a) * 0.9
            _add_mushroom_cap(bm_cr, (mx, my, 0.0), rng.uniform(0.65, 1.15), rng.uniform(1.6, 3.2), seed=idx * 10 + k)

        cr_obj = kit.finish_object(bm_cr, f"Prop_Crystal_{idx:02d}_Glow", glow_mat)
        kit.paint(cr_obj, lambda p, n: brc.mix(BIO_GREEN, BIO_LIME, brc.clamp01(p.z / 2.8)))
        objs.append(cr_obj)

    return objs


def _build_story_details_and_boss_nest(rng, stone_mat, root_mat, glow_mat):
    objs = []
    bx, by, _ = BOSS_WAIT_POS

    # 1. Ổ Boss_Wait (0, 20, 0): Đệm bùn rêu nông ôm giữa hai chùm rễ cổ thụ
    bm_nest = bmesh.new()
    bm_nroots = bmesh.new()
    brc.add_angular_stone_block(bm_nest, (bx, by, 0.02), (7.2, 6.0, 0.14), (0.0, 0.0, 0.0), rock_seed=310, chip=0.06)

    for side in (-1.0, 1.0):
        for ri in range(3):
            r_pts = [
                Vector((bx + side * (4.5 + ri * 0.8), by - 2.0 + ri * 1.8, 0.0)),
                Vector((bx + side * (4.8 + ri * 0.7), by + 0.5 + ri * 1.8, 2.8)),
                Vector((bx + side * (3.2 + ri * 0.5), by + 2.5 + ri * 1.5, 5.4)),
            ]
            rgs = brc.parallel_tube_rings(bm_nroots, r_pts, [0.75, 0.55, 0.35], 7)
            kit.bridge_rings(bm_nroots, rgs)
            kit.cap_ring(bm_nroots, rgs[0], flip=True)
            kit.cap_ring(bm_nroots, rgs[-1])

    nest_obj = kit.finish_object(bm_nest, "Boss_Nest_MossPad", stone_mat)
    kit.paint(nest_obj, lambda p, n: MOSS_GREEN)
    objs.append(nest_obj)

    nroots_obj = kit.finish_object(bm_nroots, "Boss_Nest_TwinRoots", root_mat)
    kit.paint(nroots_obj, lambda p, n: brc.mix(ROOT_DARK, ROOT_BARK, 0.5))
    objs.append(nroots_obj)

    # 2. Dấu vết Lưỡi Bắn (6-12 m): các rãnh trượt bùn thẳng hướng về ổ Cóc (<= 0.04 m)
    bm_drags = bmesh.new()
    for di, x_off in enumerate((-3.2, 0.5, 4.0)):
        pts = [
            Vector((x_off, by - 12.5, 0.02)),
            Vector((x_off * 0.7, by - 8.0, 0.02)),
            Vector((x_off * 0.4, by - 4.0, 0.02)),
        ]
        rgs = brc.parallel_tube_rings(bm_drags, pts, [0.035, 0.040, 0.030], 4)
        kit.bridge_rings(bm_drags, rgs)
        kit.cap_ring(bm_drags, rgs[0], flip=True)
        kit.cap_ring(bm_drags, rgs[-1])

    drag_obj = kit.finish_object(bm_drags, "Story_ToadTongueDrag", stone_mat)
    kit.paint(drag_obj, lambda p, n: MUD_DEEP)
    objs.append(drag_obj)

    # 3. Dấu vết Da Nhầy, Phình Nổ & Ợ Chấn: vũng bùn/chất nhầy phát quang xanh lục đục sát sàn (<= 0.05 m)
    bm_slime = bmesh.new()
    puddles = [
        (0.0, 12.0, 2.2),
        (-6.5, 4.0, 1.8),
        (7.0, -3.5, 2.0),
        (-3.5, -10.5, 1.6),
    ]
    for pi, (px, py, rad) in enumerate(puddles):
        brc.add_angular_stone_block(bm_slime, (px, py, 0.015), (rad * 2.0, rad * 1.6, 0.03), (0.0, 0.0, pi * 0.8), rock_seed=420 + pi, chip=0.14)

    slime_obj = kit.finish_object(bm_slime, "Story_ToadSlimePuddles_Glow", glow_mat)
    kit.paint(slime_obj, lambda p, n: SLIME_GLOW)
    objs.append(slime_obj)

    # 4. Vỏ Ốc Hồ rỗng, Bọ Cánh Cứng & xương Thằn Lằn quanh ổ Cóc (<= 0.05 m)
    bm_shells = bmesh.new()
    for si in range(18):
        ang = rng.uniform(0.0, 2.0 * math.pi)
        rad = rng.uniform(3.5, 9.5)
        sx = bx + math.cos(ang) * rad
        sy = by - 2.0 + math.sin(ang) * rad * 0.75
        brc.add_angular_stone_block(bm_shells, (sx, sy, 0.025), (0.38, 0.28, 0.05), (0.0, 0.0, rng.uniform(0.0, 6.28)), rock_seed=530 + si, chip=0.12)

    shell_obj = kit.finish_object(bm_shells, "Story_ToadPreyShells", stone_mat)
    kit.paint(shell_obj, lambda p, n: SHELL_PALE)
    objs.append(shell_obj)

    return objs


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=None)
    ap.add_argument("--previews", default=None)
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    args = ap.parse_args(argv)

    t0 = time.time()
    rng = random.Random(20261125)
    kit.new_scene()

    stone_mat = brc.make_stone_material("ToadRoom_StoneMat", roughness=0.72, bump_strength=0.65)
    root_mat = brc.make_stone_material("ToadRoom_RootMat", roughness=0.86, bump_strength=0.78)
    glow_mat = brc.make_glow_material("ToadRoom_GlowMat", energy=0.95)

    print("[Toad 1/3] Dựng vỏ phòng 64m, trần 32.0m (mạng rễ đan kín), vòm Rễ Cổ Thụ...", flush=True)
    shell_obj, col_obj = brc.build_watertight_shell(
        "BossRoom_Toad", stone_mat, _floor_z, _wall_disp, _paint_shell,
        wall_top_z=WALL_TOP_Z, dome_peak_z=DOME_PEAK_Z,
    )
    arch_objs = _build_arch_and_canopy_roots(rng, stone_mat, root_mat, glow_mat)

    print("[Toad 2/3] Dựng 6 trụ rễ đại thụ đan đá rêu & 8 cụm nấm sinh học phát quang...", flush=True)
    prop_objs = _build_pillars_and_crystals(rng, stone_mat, root_mat, glow_mat)

    print("[Toad 3/3] Dựng ổ Boss_Wait, rãnh Lưỡi Bắn, vũng nhầy & vỏ Ốc Hồ...", flush=True)
    story_objs = _build_story_details_and_boss_nest(rng, stone_mat, root_mat, glow_mat)

    if args.out:
        out_dir = os.path.abspath(args.out)
        room_objs = [shell_obj, col_obj] + arch_objs + prop_objs + story_objs
        kit.export_fbx(room_objs, os.path.join(out_dir, "BossRoom_Toad.fbx"))

    if args.previews:
        prev_dir = os.path.abspath(args.previews)
        os.makedirs(prev_dir, exist_ok=True)
        door_obj = brc.build_steel_door("BossRoom_SteelDoor")
        door_obj.location = Vector((0.0, brc.DOOR_Y, 4.4))

        lights = [
            ("dome_main", (0.0, 0.0, 24.0), (0.55, 0.88, 0.35), 5400.0, 10.0),
            ("dome_south", (0.0, -18.0, 14.0), (0.48, 0.85, 0.32), 2800.0, 6.0),
            ("dome_north", (0.0, 18.0, 14.0), (0.58, 0.90, 0.30), 3200.0, 6.0),
            ("door_l", (-3.5, -27.5, 4.2), (0.42, 0.92, 0.25), 1100.0, 2.0),
            ("door_r", (3.5, -27.5, 4.2), (0.42, 0.92, 0.25), 1100.0, 2.0),
            ("boss_nest", (0.0, 18.0, 5.0), (0.45, 0.92, 0.28), 2000.0, 2.8),
        ]
        cam, scene = brc.setup_cycles_scene((0.012, 0.018, 0.009), 0.35, lights)
        brc.render_view(cam, scene, (0.0, -25.8, 2.4), (0.0, 10.0, 2.5), 18.0,
                        os.path.join(prev_dir, "boss_room_toad_entry_view.png"))
        brc.render_view(cam, scene, (3.6, -14.5, 3.0), (-1.5, -29.0, 3.2), 19.0,
                        os.path.join(prev_dir, "boss_room_toad_door_view.png"))
        brc.render_view(cam, scene, (8.0, 8.0, 3.6), (0.0, 19.5, 2.0), 21.0,
                        os.path.join(prev_dir, "boss_room_toad_nest_view.png"))
        brc.render_view(cam, scene, (2.2, -25.0, 20.5), (0.0, 4.0, 1.0), 15.0,
                        os.path.join(prev_dir, "boss_room_toad_overview.png"))

    print(f"Hoàn tất Phòng Cóc Hồ Già trong {time.time() - t0:.1f}s", flush=True)


if __name__ == "__main__":
    main()
