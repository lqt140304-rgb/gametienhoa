"""Dựng Khoang Bước 3 (Giai đoạn 3A - V2 Kinh Dị Hắc Ám, Vết Nứt Vực Sâu Không Nước Ở Giữa, Bỏ 100% Thực Vật & Nhấn Mạnh Quặng Thô):
Khoang: Vực Thẳm Tinh Thể (CrystalVein - Tầng Giữa) trong Blender headless.

Thiết kế theo đúng góp ý mới nhất của người dùng:
- Giảm mạnh phần nước: BỎ hoàn toàn đầm nước ở giữa và tầng thấp; CHỈ giữ 2 hố dịch khoáng nhỏ trên cao phía Bắc
  (tầng thượng +8.85m và hố lửng +5.45m) cùng dải thác mảnh ngấm vào khe đá.
- Vùng giữa là Nền Đá Hắc Diện Gãy Vỡ & Vết Nứt Vực Thẳm Sâu Hun Hút (z = -13m .. -16m) kèm 36 Răng Nanh Quặng
  đâm ngang so le từ hai thành vực như hàm quái vật, bên dưới đáy vực phát ánh quặng đỏ máu hắt ngược lên.
- 0% CẦU (nối qua khe vực bằng 4 Dải Đá Ngầm / Cổ Đất Tự Nhiên) & 0% THỰC VẬT (bỏ sạch cỏ, nấm, hoa, râu cây).
- Nhấn mạnh Quặng Thô & Tính Kinh Dị:
  + 10 Khung Xương Sườn & Móng Vuốt Quặng Khổng Lồ (15m-24m) uốn quặp ôm lấy khe vực
  + 28 Vỉa Quặng Thô Nứt Toác & Khối U Khoáng Thạch
  + 14 Hốc Quặng "Con Mắt Vực Thẳm" nhìn chằm chằm từ các vách đá đen
  + 34 Đèn Chùm Răng Nanh Rủ Trần & 36 Gân Máu Khoáng Đen-Tím trên vòm trần
  + ~320 Phiến Đá Hắc Diện Vỡ & ~180 Cụm Quặng Thô Đa Diện dưới đất.

Chạy preview:
  D:/blender/blender.exe -b -P tools/blender/build_maze_chamber_crystal.py -- --previews D:/gametienhoa/docs/art/previews
"""
import argparse
import math
import os
import random
import sys
import time

import bpy  # must come before bmesh/mathutils
import bmesh
from mathutils import Vector, Matrix, Euler, noise
from mathutils.bvhtree import BVHTree

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_common as kit  # noqa: E402


# ---------------------------------------------------------------- Bảng màu Kinh Dị: Đen Hắc Diện - Tím Hắc Ám - Quặng Đỏ Máu (Linear RGB)

OBSIDIAN_ABYSS = (0.005, 0.004, 0.010)
OBSIDIAN_DARK = (0.016, 0.011, 0.028)
OBSIDIAN_CRAG = (0.038, 0.024, 0.062)
CORRUPT_CRUST = (0.085, 0.028, 0.135)
CRIMSON_STAIN = (0.20, 0.018, 0.095)
ASH_SLATE = (0.052, 0.042, 0.078)

ORE_PURPLE_DARK = (0.065, 0.008, 0.14)
ORE_VOID_VIOLET = (0.40, 0.035, 0.74)
ORE_AMETHYST_EDGE = (0.68, 0.14, 0.96)
ORE_BLOOD_DARK = (0.22, 0.012, 0.06)
ORE_BLOOD_BRIGHT = (0.86, 0.055, 0.24)
ORE_EYE_CORE = (1.00, 0.12, 0.36)

TOXIC_DEEP = (0.035, 0.005, 0.085)
TOXIC_VIOLET = (0.24, 0.025, 0.45)
TOXIC_FROTH = (0.60, 0.14, 0.84)

TERRACE2_WATER_Z = 5.45
TERRACE3_WATER_Z = 8.85

TUNNEL_ANGLES = [math.radians(25.0), math.radians(148.0), math.radians(268.0)]


# ---------------------------------------------------------------- Tiện ích Toán học & Parallel Transport

def _clamp01(x):
    return max(0.0, min(1.0, x))


def _smoothstep(a, b, x):
    t = _clamp01((x - a) / max(1e-5, b - a))
    return t * t * (3.0 - 2.0 * t)


def _mix(a, b, t):
    t = _clamp01(t)
    return (
        a[0] + (b[0] - a[0]) * t,
        a[1] + (b[1] - a[1]) * t,
        a[2] + (b[2] - a[2]) * t,
    )


def _mix_val(a, b, t):
    t = _clamp01(t)
    return a + (b - a) * t


def _ridged(p):
    return 1.0 - abs(noise.noise(p))


def _stepped(val, steps=6.0, sharp=0.82):
    s = val * steps
    fl = math.floor(s)
    frac = s - fl
    crisp = _smoothstep(0.5 - sharp * 0.44, 0.5 + sharp * 0.44, frac)
    return (fl + crisp) / steps


def _delta_ang(a, b):
    return (b - a + math.pi) % (2.0 * math.pi) - math.pi


def _smooth_path(pts, sub=3):
    pts = [Vector(p) for p in pts]
    if len(pts) < 3 or sub <= 1:
        return pts
    out = []
    for i in range(len(pts) - 1):
        p0, p1 = pts[max(0, i - 1)], pts[i]
        p2, p3 = pts[i + 1], pts[min(len(pts) - 1, i + 2)]
        for k in range(sub):
            t = k / sub
            t2, t3 = t * t, t * t * t
            out.append(0.5 * (
                (2.0 * p1)
                + (-p0 + p2) * t
                + (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3) * t2
                + (-p0 + 3.0 * p1 - 3.0 * p2 + p3) * t3
            ))
    out.append(pts[-1])
    return out


def _parallel_tube_rings(bm, path, radii, segs, wobble=None, twist_total=0.0):
    n_pts = len(path)
    rings = []
    if n_pts < 2:
        return rings

    tangents = []
    for i in range(n_pts):
        a = path[max(0, i - 1)]
        b = path[min(n_pts - 1, i + 1)]
        d = b - a
        tangents.append(d.normalized() if d.length > 1e-6 else Vector((0.0, 0.0, 1.0)))

    t0 = tangents[0]
    ref = Vector((1.0, 0.0, 0.0)) if abs(t0.dot(Vector((0.0, 0.0, 1.0)))) > 0.88 else Vector((0.0, 0.0, 1.0))
    norm = t0.cross(ref).normalized()
    binorm = t0.cross(norm).normalized()

    prev_t = t0
    for i, p in enumerate(path):
        t = tangents[i]
        if i > 0:
            rot = prev_t.rotation_difference(t)
            norm = (rot @ norm).normalized()
            norm = (norm - t * norm.dot(t)).normalized()
            binorm = t.cross(norm).normalized()
        prev_t = t

        tw = twist_total * (i / max(1, n_pts - 1))
        ring = []
        for k in range(segs):
            ang = 2.0 * math.pi * k / segs + tw
            w = wobble(p, ang, i) if wobble else 0.0
            r = max(0.004, radii[i] * (1.0 + w))
            ring.append(bm.verts.new(p + (norm * math.cos(ang) + binorm * math.sin(ang)) * r))
        rings.append(ring)
    return rings


def _add_crystal_prism_branch(bm, pts, r_start, r_end, tip_len, segs=6, twist=0.0):
    smooth_pts = _smooth_path(pts, sub=3)
    n = len(smooth_pts)
    radii = [r_start + (r_end - r_start) * (i / max(1, n - 1)) for i in range(n)]
    rgs = _parallel_tube_rings(bm, smooth_pts, radii, segs, twist_total=twist)
    kit.bridge_rings(bm, rgs)
    kit.cap_ring(bm, rgs[0])

    tan = (smooth_pts[-1] - smooth_pts[-2]).normalized()
    apex = smooth_pts[-1] + tan * tip_len
    apex_v = bm.verts.new(apex)
    top_ring = rgs[-1]
    for k in range(segs):
        bm.faces.new([top_ring[k], top_ring[(k + 1) % segs], apex_v])
    return smooth_pts


def _add_raw_ore_chunk(bm, center, scale_v, seed_v, rot_euler):
    """Khối quặng thô đa diện sắc cạnh (Octahedral/Hexagonal Crystal Ore Block) thay vì khối tròn."""
    res = bmesh.ops.create_icosphere(bm, subdivisions=1, radius=1.0)
    mat_rot = Euler(rot_euler, "XYZ").to_matrix().to_4x4()
    for v in res["verts"]:
        p = v.co.copy()
        n = _ridged(p * 2.4 + Vector((seed_v, seed_v * 1.1, seed_v * 0.7)))
        p.x *= scale_v.x * (0.72 + 0.52 * n)
        p.y *= scale_v.y * (0.72 + 0.52 * n)
        p.z *= scale_v.z * (0.78 + 0.65 * n)
        v.co = Vector(center) + (mat_rot @ p)


# ---------------------------------------------------------------- Vật liệu Hắc Ám, Đá Đen & Quặng Tinh Thể

def _make_obsidian_material(name):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()

    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")

    vcol = nodes.new("ShaderNodeVertexColor")
    vcol.layer_name = "Col"
    links.new(vcol.outputs["Color"], bsdf.inputs["Base Color"])

    texcoord = nodes.new("ShaderNodeTexCoord")

    n_coarse = nodes.new("ShaderNodeTexNoise")
    n_coarse.inputs["Scale"].default_value = 0.28
    n_coarse.inputs["Detail"].default_value = 9.0
    n_coarse.inputs["Roughness"].default_value = 0.80
    links.new(texcoord.outputs["Object"], n_coarse.inputs["Vector"])

    n_fine = nodes.new("ShaderNodeTexNoise")
    n_fine.inputs["Scale"].default_value = 3.2
    n_fine.inputs["Detail"].default_value = 7.0
    n_fine.inputs["Roughness"].default_value = 0.72
    links.new(texcoord.outputs["Object"], n_fine.inputs["Vector"])

    add_n = nodes.new("ShaderNodeMath")
    add_n.operation = "ADD"
    links.new(n_coarse.outputs["Fac"], add_n.inputs[0])
    links.new(n_fine.outputs["Fac"], add_n.inputs[1])

    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.85
    bump.inputs["Distance"].default_value = 0.28
    links.new(add_n.outputs["Value"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    ramp = nodes.new("ShaderNodeMapRange")
    ramp.inputs["From Min"].default_value = 0.20
    ramp.inputs["From Max"].default_value = 0.85
    ramp.inputs["To Min"].default_value = 0.30
    ramp.inputs["To Max"].default_value = 0.86
    links.new(n_coarse.outputs["Fac"], ramp.inputs["Value"])
    links.new(ramp.outputs["Result"], bsdf.inputs["Roughness"])

    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def _make_ore_crystal_material(name, energy=0.18, roughness=0.10):
    """Vật liệu quặng tinh thể hắc ám sắc cạnh: phát sáng âm ỉ (energy thấp) để giữ khối tối sâu và mặt cắt kim loại sắc."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()

    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    vcol = nodes.new("ShaderNodeVertexColor")
    vcol.layer_name = "Col"
    links.new(vcol.outputs["Color"], bsdf.inputs["Base Color"])
    if "Emission Color" in bsdf.inputs:
        links.new(vcol.outputs["Color"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = energy
    bsdf.inputs["Roughness"].default_value = roughness
    if "Metallic" in bsdf.inputs:
        bsdf.inputs["Metallic"].default_value = 0.24
    if "IOR" in bsdf.inputs:
        bsdf.inputs["IOR"].default_value = 1.72
    if "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = 0.98
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def _make_toxic_fluid_material(name):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()

    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    vcol = nodes.new("ShaderNodeVertexColor")
    vcol.layer_name = "Col"
    links.new(vcol.outputs["Color"], bsdf.inputs["Base Color"])
    if "Emission Color" in bsdf.inputs:
        links.new(vcol.outputs["Color"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = 0.50

    bsdf.inputs["Roughness"].default_value = 0.14
    if "IOR" in bsdf.inputs:
        bsdf.inputs["IOR"].default_value = 1.42

    texcoord = nodes.new("ShaderNodeTexCoord")
    n_wave = nodes.new("ShaderNodeTexNoise")
    n_wave.inputs["Scale"].default_value = 2.4
    n_wave.inputs["Detail"].default_value = 6.0
    n_wave.inputs["Roughness"].default_value = 0.55
    links.new(texcoord.outputs["Object"], n_wave.inputs["Vector"])

    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.28
    bump.inputs["Distance"].default_value = 0.10
    links.new(n_wave.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


# ---------------------------------------------------------------- Toán học Địa hình Vết Nứt Vực Thẳm Sâu (-14m, Không Nước Ở Giữa)

def _boundary_u(x, y):
    ang = math.atan2(y, x)
    warp = (
        0.090 * math.sin(ang * 3.0 + 0.85)
        + 0.065 * math.cos(ang * 5.0 - 0.6)
        + 0.045 * _ridged(Vector((math.cos(ang) * 2.5, math.sin(ang) * 2.5, 4.1)))
    )
    return math.sqrt((x / 114.0) ** 2 + (y / 88.0) ** 2) + warp


def _alcove_depth_factor(x, y):
    u = _boundary_u(x, y)
    if u < 0.65:
        return 1.0, False
    ang = math.atan2(y, x)
    min_d = min(abs(_delta_ang(ang, ta)) for ta in TUNNEL_ANGLES)
    if min_d < math.radians(15.0):
        side_t = _smoothstep(math.radians(7.5), math.radians(15.0), min_d)
        back_close = _smoothstep(0.91, 0.98, u)
        return max(side_t, back_close), True
    return 1.0, False


def _chasm_center_y(x):
    """Đường tâm Vết Nứt Vực Thẳm chính xé ngang khoang với biên răng cưa."""
    return -3.0 + 11.5 * math.sin(x * 0.048 - 0.15) + 4.2 * math.sin(x * 0.145)


def _chasm_branch_y(x):
    """Vết nứt nhánh chữ Y phía Nam."""
    return -22.0 + 6.5 * math.sin(x * 0.055 + 0.8) + 2.8 * math.cos(x * 0.16)


# 4 Cổ Đất Tự Nhiên (Land Necks) cắt ngang khe nứt để tạo lối đi bộ & chia khe vực thành các hố thẳm đáng sợ
LAND_NECK_X = [-34.0, -4.0, 22.0, 48.0]


def _land_neck_factor(x):
    w = 0.0
    for nx_p in LAND_NECK_X:
        d = abs(x - nx_p)
        w = max(w, 1.0 - _smoothstep(2.8, 6.2, d))
    return w


def _terrace2_dist(x, y):
    """Hồ dịch độc lửng nhỏ trên cao phía Bắc (-7, 36, +5.45m)."""
    dx = (x + 7.0) * math.cos(-0.20) + (y - 36.0) * math.sin(-0.20)
    dy = -(x + 7.0) * math.sin(-0.20) + (y - 36.0) * math.cos(-0.20)
    return math.sqrt((dx / 12.0) ** 2 + (dy / 9.2) ** 2)


def _terrace3_dist(x, y):
    """Hồ dịch độc thượng nguồn nhỏ phía Bắc (8, 49, +8.85m)."""
    dx = (x - 8.0) * math.cos(-0.28) + (y - 49.0) * math.sin(-0.28)
    dy = -(x - 8.0) * math.sin(-0.28) + (y - 49.0) * math.cos(-0.28)
    return math.sqrt((dx / 12.5) ** 2 + (dy / 9.5) ** 2)


def _terrace_profile_height(d, water_z, rim_z, spill_notch_angle, dx, dy):
    bed_z = water_z - 1.40
    ang = math.atan2(dy, dx)
    in_notch = 1.0 - _smoothstep(0.14, 0.34, abs(_delta_ang(ang, spill_notch_angle)))
    lip_z = rim_z - (rim_z - water_z + 0.14) * in_notch

    if d < 0.72:
        t = _smoothstep(0.55, 0.72, d)
        return bed_z + (water_z - 0.40 - bed_z) * t
    elif d < 0.87:
        t = _smoothstep(0.72, 0.86, d)
        return (water_z - 0.40) + (lip_z - (water_z - 0.40)) * t
    else:
        return lip_z


def _east_ramp_info(x, y):
    pts = [
        (34.0, -20.0, 0.45),
        (40.0, -4.0, 2.60),
        (36.0, 14.0, 5.30),
        (26.0, 32.0, 7.90),
        (17.0, 44.0, 9.25),
    ]
    best_w = 0.0
    best_h = 0.0
    for i in range(len(pts) - 1):
        ax, ay, az = pts[i]
        bx, by, bz = pts[i + 1]
        abx, aby = bx - ax, by - ay
        l2 = max(1e-4, abx * abx + aby * aby)
        t = _clamp01(((x - ax) * abx + (y - ay) * aby) / l2)
        px = ax + abx * t
        py = ay + aby * t
        dist = math.sqrt((x - px) ** 2 + (y - py) ** 2)
        w = 1.0 - _smoothstep(7.5, 16.0, dist)
        if w > best_w:
            best_w = w
            best_h = az + (bz - az) * t
    return best_w, best_h


def terrain_height(x, y):
    p_noise = Vector((x * 0.040 + 5.2, y * 0.040 - 3.1, 0.0))
    n1 = noise.noise(p_noise * 0.85)
    n2 = _ridged(p_noise * 1.45)
    n3 = _ridged(p_noise * 4.2)

    # 1. Nền đất đá hắc diện LỒI LÕM GÃY BẬC MẠNH (±4m .. ±7.5m): sống đá đứt gãy + hõm sụt lún + góc cạnh sắc
    fault_ridges = 3.6 * (n2 - 0.42) + 2.6 * n1 + 1.15 * (n3 - 0.5)
    h = 1.65 + fault_ridges
    east_rise = _smoothstep(18.0, 58.0, x) * 4.8 + _smoothstep(16.0, 58.0, y) * 3.8
    west_knoll = _smoothstep(-18.0, -58.0, x) * _smoothstep(15.0, -40.0, y) * 4.6
    h += east_rise + west_knoll

    ramp_w, ramp_h = _east_ramp_info(x, y)
    h = _mix_val(h, ramp_h + 0.55 * n1 + 0.65 * (n2 - 0.45), ramp_w * 0.82)

    # 2. Vết Nứt Vực Thẳm Sâu Hun Hút (Abyssal Maw Chasm, tụt sâu xuống -13.5m .. -15.5m)
    sy1 = _chasm_center_y(x)
    w1_jagged = 8.4 + 2.4 * noise.noise(Vector((x * 0.09, 1.3, 2.7)))
    d_s1 = abs(y - sy1)

    sy2 = _chasm_branch_y(x)
    branch_mask = _smoothstep(-32.0, -18.0, x) * (1.0 - _smoothstep(28.0, 42.0, x))
    d_s2 = abs(y - sy2) / max(0.05, branch_mask) if branch_mask > 0.05 else 999.0

    neck_w = _land_neck_factor(x)
    in_bounds = 1.0 - _smoothstep(0.64, 0.78, _boundary_u(x, y))

    chasm_cut1 = (1.0 - _smoothstep(w1_jagged * 0.42, w1_jagged, d_s1)) * (1.0 - neck_w * 0.92) * in_bounds
    chasm_cut2 = (1.0 - _smoothstep(2.4, 5.8, d_s2)) * (1.0 - neck_w * 0.85) * in_bounds
    chasm_cut = max(chasm_cut1, chasm_cut2)

    abyss_floor = -14.8 + 1.8 * n1 + 1.6 * _ridged(p_noise * 3.2)
    if chasm_cut > 0.01:
        steep_cut = chasm_cut ** 0.75
        h = _mix_val(h, abyss_floor, steep_cut)

    # 3. CHỈ giữ 2 hố dịch khoáng nhỏ trên cao phía Bắc (+5.45m và +8.85m)
    d2 = _terrace2_dist(x, y)
    if d2 < 1.32:
        w2 = 1.0 - _smoothstep(0.88, 1.32, d2)
        t2_h = _terrace_profile_height(d2, TERRACE2_WATER_Z, 6.20, math.radians(-105.0), x + 7.0, y - 36.0)
        h = _mix_val(h, max(h, t2_h) if d2 >= 0.87 else t2_h, w2)

    d3 = _terrace3_dist(x, y)
    if d3 < 1.32:
        w3 = 1.0 - _smoothstep(0.88, 1.32, d3)
        t3_h = _terrace_profile_height(d3, TERRACE3_WATER_Z, 9.60, math.radians(-130.0), x - 8.0, y - 49.0)
        h = _mix_val(h, max(h, t3_h) if d3 >= 0.87 else t3_h, w3)

    # Bậc thềm đá vát cạnh sắc nét rõ rệt khắp toàn bộ mặt đất nền
    if h > -1.0:
        strata = _stepped((h + 3.5) / 14.0, steps=9.0, sharp=0.88) * 14.0 - 3.5
        strata_w = _smoothstep(-1.0, 1.8, h) * 0.58 * (1.0 - ramp_w * 0.35)
        h = _mix_val(h, strata, strata_w)

    # Vách bao gãy khúc vươn lên trần + 3 hốc cửa hầm
    u = _boundary_u(x, y)
    alcove_f, in_alcove = _alcove_depth_factor(x, y)
    if u > 0.72:
        wall_t = _smoothstep(0.72, 1.02, u)
        wall_t *= alcove_f
        strata_wall = _stepped(wall_t, steps=7.0, sharp=0.88)
        rock_crag = 5.6 * _ridged(Vector((x * 0.074, y * 0.074, 2.3)))
        h += wall_t * 25.0 + strata_wall * 10.5 + wall_t * rock_crag

    if abs(x - 9.0) < 15.0 and y > 54.0:
        wf_w = (1.0 - _smoothstep(5.5, 14.0, abs(x - 9.0))) * _smoothstep(54.0, 68.0, y)
        h += wf_w * 9.0

    if in_alcove and u < 0.91:
        h = min(h, 1.1 + 0.25 * n1)

    return h


def ceiling_height(x, y):
    u = _clamp01(_boundary_u(x, y) / 1.04)
    dome = 48.0 - 15.0 * (u ** 1.75)
    c_noise = 4.4 * noise.noise(Vector((x * 0.05, y * 0.05, 7.3))) + 3.6 * _ridged(Vector((x * 0.082, y * 0.082, 2.4)))
    return dome + c_noise


def _in_ceiling_slit(x, y):
    d1 = math.sqrt(((x + 14.0) / 15.0) ** 2 + ((y - 10.0) / 4.8) ** 2)
    return d1 < 1.0 and abs(y - 10.0 - 2.8 * math.sin(x * 0.14)) < 2.2


# ---------------------------------------------------------------- Vách Đá Hắc Diện, Trụ Gai & Vòm Trần

def _add_faceted_obsidian_rock(bm, pos, scale_v, seed_v, rot_euler):
    res = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0)
    mat_rot = Euler(rot_euler, "XYZ").to_matrix().to_4x4()
    for v in res["verts"]:
        p = v.co.copy()
        n = _ridged(p * 2.1 + Vector((seed_v, seed_v * 0.7, seed_v * 1.5)))
        n2 = noise.noise(p * 3.8 + Vector((seed_v * 2.2, 0.0, seed_v)))
        p.x *= scale_v.x * (0.68 + 0.62 * n + 0.15 * n2)
        p.y *= scale_v.y * (0.68 + 0.62 * n + 0.15 * n2)
        p.z *= scale_v.z * (0.74 + 0.54 * n + 0.14 * n2)
        v.co = Vector(pos) + (mat_rot @ p)


def _add_rock_pillar(bm, cx, cy, base_z, top_z, r_base, r_mid, seed_v):
    rings = []
    segs = 10
    steps = 11
    for i in range(steps + 1):
        t = i / steps
        z = base_z + (top_z - base_z) * t
        flair = (1.0 - t) ** 2 * 1.9 + (t ** 3) * 1.6
        r_t = r_mid * (0.85 + 0.52 * flair) + r_base * 0.25 * (1.0 - t)
        cx_w = cx + 2.6 * math.sin(t * math.pi * 1.6 + seed_v)
        cy_w = cy + 2.2 * math.cos(t * math.pi * 1.3 + seed_v)
        ring = []
        for k in range(segs):
            ang = 2.0 * math.pi * k / segs
            crag = 0.70 + 0.50 * _ridged(Vector((math.cos(ang) * 2.0 + seed_v, math.sin(ang) * 2.0, t * 4.8)))
            ring.append(bm.verts.new(Vector((cx_w + math.cos(ang) * r_t * crag, cy_w + math.sin(ang) * r_t * crag, z))))
        rings.append(ring)
    kit.bridge_rings(bm, rings)
    kit.cap_ring(bm, rings[0])
    kit.cap_ring(bm, rings[-1])


def _add_ceiling_obsidian_ribs(bm, rng):
    rib_arcs = [
        ((-88.0, -34.0), (88.0, 38.0), 3.6, 3.2),
        ((-55.0, 70.0), (62.0, -68.0), 3.4, 3.0),
        ((-84.0, 28.0), (76.0, -38.0), 3.2, 2.8),
        ((-26.0, -76.0), (42.0, 74.0), 3.4, 3.0),
        ((-70.0, -58.0), (22.0, 76.0), 3.0, 2.6),
        ((78.0, 12.0), (-32.0, -72.0), 3.1, 2.6),
    ]
    for ri, (p0, p1, w_base, d_drop) in enumerate(rib_arcs):
        pts, radii = [], []
        for s in range(18):
            t = s / 17.0
            px = p0[0] + (p1[0] - p0[0]) * t + 7.5 * math.sin(t * math.pi * 2.0 + ri)
            py = p0[1] + (p1[1] - p0[1]) * t + 6.5 * math.cos(t * math.pi * 1.6 + ri)
            pz = ceiling_height(px, py) - d_drop * (0.45 + 0.55 * math.sin(t * math.pi))
            pts.append(Vector((px, py, pz)))
            radii.append(w_base * (0.75 + 0.50 * abs(math.cos(t * math.pi * 3.0 + ri))))
        rgs = _parallel_tube_rings(
            bm, pts, radii, 6,
            wobble=lambda p, a, idx: 0.32 * _ridged(p * 0.25 + Vector((a, idx * 0.3, 1.4))),
        )
        kit.bridge_rings(bm, rgs)


def _add_stalactite_cluster(bm, center, seed_v, rng):
    _add_faceted_obsidian_rock(
        bm,
        center + Vector((0.0, 0.0, 0.9)),
        Vector((rng.uniform(2.0, 3.5), rng.uniform(2.0, 3.5), rng.uniform(2.2, 3.8))),
        seed_v,
        (rng.uniform(-0.2, 0.2), rng.uniform(-0.2, 0.2), rng.uniform(0, 6.28)),
    )
    buds = []
    n_cones = rng.randint(2, 4)
    for k in range(n_cones):
        ox = rng.uniform(-1.5, 1.5) if k > 0 else 0.0
        oy = rng.uniform(-1.5, 1.5) if k > 0 else 0.0
        length = rng.uniform(4.8, 11.8) if k == 0 else rng.uniform(3.0, 6.8)
        r_top = rng.uniform(0.70, 1.35) if k == 0 else rng.uniform(0.40, 0.80)
        pts = [
            center + Vector((ox, oy, 1.5)),
            center + Vector((ox * 1.1, oy * 1.1, -length * 0.45)),
            center + Vector((ox * 1.15, oy * 1.15, -length)),
        ]
        rgs = _parallel_tube_rings(bm, pts, [r_top, r_top * 0.44, 0.08], 6)
        kit.bridge_rings(bm, rgs)
        kit.cap_ring(bm, rgs[-1])
        buds.append(pts[-1])
    return buds


def build_chamber_shell(rng):
    nx, ny = 196, 152
    x_min, x_max = -126.0, 126.0
    y_min, y_max = -98.0, 98.0

    bm = bmesh.new()
    bm_col = bmesh.new()

    grid = [[None] * nx for _ in range(ny)]
    col_grid = [[None] * nx for _ in range(ny)]
    terrain_faces = []
    col_terrain_faces = []

    for iy in range(ny):
        y = y_min + (y_max - y_min) * iy / (ny - 1)
        for ix in range(nx):
            x = x_min + (x_max - x_min) * ix / (nx - 1)
            if _boundary_u(x, y) > 1.12:
                continue
            z = terrain_height(x, y)
            grid[iy][ix] = bm.verts.new(Vector((x, y, z)))
            col_grid[iy][ix] = bm_col.verts.new(Vector((x, y, z)))

    for iy in range(ny - 1):
        for ix in range(nx - 1):
            a, b = grid[iy][ix], grid[iy][ix + 1]
            c, d = grid[iy + 1][ix + 1], grid[iy + 1][ix]
            if a and b and c and d:
                terrain_faces.append(bm.faces.new([a, b, c, d]))
                col_terrain_faces.append(bm_col.faces.new([col_grid[iy][ix], col_grid[iy][ix + 1], col_grid[iy + 1][ix + 1], col_grid[iy + 1][ix]]))

    # Trần hang Geode tối sâu (cao 40m-50m)
    ceil_nx, ceil_ny = 84, 66
    cgrid = [[None] * ceil_nx for _ in range(ceil_ny)]
    ceiling_faces = []
    for iy in range(ceil_ny):
        y = y_min + (y_max - y_min) * iy / (ceil_ny - 1)
        for ix in range(ceil_nx):
            x = x_min + (x_max - x_min) * ix / (ceil_nx - 1)
            u = _boundary_u(x, y)
            if u > 1.07 or _in_ceiling_slit(x, y):
                continue
            z = ceiling_height(x, y)
            cgrid[iy][ix] = bm.verts.new(Vector((x, y, z)))

    for iy in range(ceil_ny - 1):
        for ix in range(ceil_nx - 1):
            a, b = cgrid[iy][ix], cgrid[iy][ix + 1]
            c, d = cgrid[iy + 1][ix + 1], cgrid[iy + 1][ix]
            if a and b and c and d:
                ceiling_faces.append(bm.faces.new([d, c, b, a]))

    terrain_set = set(terrain_faces)
    ceiling_set = set(ceiling_faces)
    col_terrain_set = set(col_terrain_faces)

    # 4 Trụ Đá Hắc Diện Khổng Lồ
    pillars = [
        (-48.0, -22.0, 5.4, 4.3, 1.2),
        (52.0, 16.0, 5.8, 4.5, 2.7),
        (-44.0, 48.0, 5.2, 4.1, 4.1),
        (50.0, -48.0, 5.0, 3.9, 5.5),
    ]
    for px, py, rb, rm, sv in pillars:
        bz = terrain_height(px, py) - 2.0
        tz = ceiling_height(px, py) + 2.5
        _add_rock_pillar(bm, px, py, bz, tz, rb, rm, sv)
        _add_rock_pillar(bm_col, px, py, bz, tz, rb, rm, sv)

    # Đắp 42 Mỏm Đá Hắc Diện Góc Cạnh & Phiến Đá Nghiêng trực tiếp trên nền & mép vực (làm nền lồi lõm rõ rệt)
    cam_clears = [(24.0, -56.0), (6.0, -25.0), (-22.0, 44.0), (-25.0, -15.0)]
    crags_placed = 0
    for ci in range(120):
        if crags_placed >= 42:
            break
        ang = rng.uniform(0.0, 2.0 * math.pi)
        rad = rng.uniform(0.10, 0.70) ** 0.75
        cx = math.cos(ang) * 90.0 * rad
        cy = math.sin(ang) * 70.0 * rad
        if any((cx - kx) ** 2 + (cy - ky) ** 2 < 64.0 for kx, ky in cam_clears):
            continue
        cz = terrain_height(cx, cy)
        if cz < -2.0 or cz > 12.0:
            continue
        if (_terrace2_dist(cx, cy) < 0.90 and cz < TERRACE2_WATER_Z + 0.3) or (_terrace3_dist(cx, cy) < 0.90 and cz < TERRACE3_WATER_Z + 0.3):
            continue
        sc = rng.uniform(2.2, 4.8)
        rot = (rng.uniform(-0.45, 0.45), rng.uniform(-0.45, 0.45), rng.uniform(0.0, 6.28))
        scale_v = Vector((sc * rng.uniform(0.9, 1.5), sc * rng.uniform(0.6, 1.2), sc * rng.uniform(0.7, 1.45)))
        _add_faceted_obsidian_rock(bm, Vector((cx, cy, cz - sc * 0.38)), scale_v, ci * 4.3, rot)
        _add_faceted_obsidian_rock(bm_col, Vector((cx, cy, cz - sc * 0.38)), scale_v, ci * 4.3, rot)
        crags_placed += 1

    # Tính pháp tuyến RIÊNG cho khối phụ, TUYỆT ĐỐI không lật ngược mặt lưới địa hình hở!
    col_extra = [f for f in bm_col.faces if f not in col_terrain_set]
    if col_extra:
        bmesh.ops.recalc_face_normals(bm_col, faces=col_extra)
    bm_col.normal_update()
    for f in col_terrain_faces:
        if f.normal.z < 0.0:
            f.normal_flip()
    col_obj = kit.finish_object(bm_col, "MazeChamber_Crystal_COL", recalc_normals=False)

    _add_ceiling_obsidian_ribs(bm, rng)

    stalactite_buds = []
    for si in range(34):
        ang = rng.uniform(0.0, 2.0 * math.pi)
        rad = rng.uniform(0.10, 0.78)
        sx = math.cos(ang) * 96.0 * rad
        sy = math.sin(ang) * 74.0 * rad
        if _in_ceiling_slit(sx, sy):
            continue
        cz = ceiling_height(sx, sy)
        stalactite_buds.extend(_add_stalactite_cluster(bm, Vector((sx, sy, cz)), si * 2.1, rng))

    # Đá tảng gãy góc dọc biên vách
    for bi in range(48):
        ang = 2.0 * math.pi * bi / 48.0 + rng.uniform(-0.08, 0.08)
        if min(abs(_delta_ang(ang, ta)) for ta in TUNNEL_ANGLES) < math.radians(13.0):
            continue
        r_fac = rng.uniform(0.68, 0.88)
        bx = math.cos(ang) * 102.0 * r_fac
        by = math.sin(ang) * 78.0 * r_fac
        bz = terrain_height(bx, by) - 1.6
        sv = rng.uniform(2.8, 6.2)
        _add_faceted_obsidian_rock(
            bm, Vector((bx, by, bz)),
            Vector((sv * rng.uniform(0.9, 1.5), sv * rng.uniform(0.8, 1.4), sv * rng.uniform(0.9, 1.7))),
            bi * 3.1,
            (rng.uniform(-0.25, 0.25), rng.uniform(-0.25, 0.25), rng.uniform(0, 6.28)),
        )

    extra_faces = [f for f in bm.faces if f not in terrain_set and f not in ceiling_set]
    if extra_faces:
        bmesh.ops.recalc_face_normals(bm, faces=extra_faces)
    bm.normal_update()
    for f in terrain_faces:
        if f.normal.z < 0.0:
            f.normal_flip()
    for f in ceiling_faces:
        if f.normal.z > 0.0:
            f.normal_flip()

    sharp_edges = [e for e in bm.edges if e.is_manifold and e.calc_face_angle(0.0) >= math.radians(20.0)]
    if sharp_edges:
        bmesh.ops.split_edges(bm, edges=sharp_edges)

    bm.normal_update()
    for f in terrain_faces:
        if f.is_valid and f.normal.z < 0.0:
            f.normal_flip()
    for f in ceiling_faces:
        if f.is_valid and f.normal.z > 0.0:
            f.normal_flip()

    shell_mat = _make_obsidian_material("MazeChamber_Crystal_ShellMat")
    shell_obj = kit.finish_object(bm, "MazeChamber_Crystal_Shell", shell_mat, recalc_normals=False)
    for p in shell_obj.data.polygons:
        p.use_smooth = False

    def paint_shell(p, n):
        n_c = 0.5 + 0.5 * noise.noise(p * 0.075 + Vector((2.1, 4.3, 1.9)))
        n_d = _ridged(p * 0.18 + Vector((6.2, 1.4, 3.0)))
        u_here = _boundary_u(p.x, p.y)

        # Vách sâu trong Vết Nứt Vực Thẳm: đen tuyền với mạch quặng máu đỏ rỉ
        if p.z < -2.2:
            if n_d > 0.70:
                return _mix(CRIMSON_STAIN, ORE_BLOOD_DARK, _smoothstep(0.70, 0.92, n_d))
            return _mix(OBSIDIAN_ABYSS, OBSIDIAN_DARK, n_c * 0.65)

        if p.z > 13.0 or u_here > 0.72:
            if n_d > 0.74:
                return _mix(CORRUPT_CRUST, CRIMSON_STAIN, _smoothstep(0.74, 0.93, n_d))
            return _mix(OBSIDIAN_ABYSS, OBSIDIAN_CRAG, n_c * 0.85)

        # Gờ vát dốc lồi lõm góc cạnh (n.z < 0.74): tạo tương phản đậm rõ từng mảng gãy góc trên nền đất!
        if n.z < 0.74:
            if n_d > 0.64:
                return _mix(CORRUPT_CRUST, CRIMSON_STAIN, (n_d - 0.64) * 2.4)
            return _mix(OBSIDIAN_ABYSS, OBSIDIAN_CRAG, 0.25 + 0.75 * n_d)

        d2 = _terrace2_dist(p.x, p.y)
        d3 = _terrace3_dist(p.x, p.y)
        if min(d2, d3) < 1.12:
            return _mix(ASH_SLATE, CORRUPT_CRUST, n_d * 0.75)

        # Đỉnh gò lồi lên sáng màu tro tím / quặng rỉ để tương phản với khe trũng tối
        if p.z > 2.2:
            return _mix(OBSIDIAN_CRAG, CORRUPT_CRUST if n_d > 0.55 else ASH_SLATE, 0.35 + 0.65 * n_c)
        if n_d > 0.68:
            return _mix(CORRUPT_CRUST, CRIMSON_STAIN, (n_d - 0.68) * 2.5)
        return _mix(OBSIDIAN_DARK, ASH_SLATE, n_c * 0.75)

    kit.paint(shell_obj, paint_shell)

    deps = bpy.context.evaluated_depsgraph_get()
    walk_bvh = BVHTree.FromObject(shell_obj, deps)
    return shell_obj, col_obj, walk_bvh, stalactite_buds


# ---------------------------------------------------------------- Nguồn Nước Tối Giản: Chỉ 2 Hố Nhỏ Đoạn Trên Cao

def _build_water_ribbon_sheet(bm, x0, x1, y0, y1, nx, ny, z_fn, mask_fn, out_faces=None):
    grid = [[None] * nx for _ in range(ny)]
    for iy in range(ny):
        y = y0 + (y1 - y0) * iy / (ny - 1)
        for ix in range(nx):
            x = x0 + (x1 - x0) * ix / (nx - 1)
            if not mask_fn(x, y):
                continue
            grid[iy][ix] = bm.verts.new(Vector((x, y, z_fn(x, y))))
    for iy in range(ny - 1):
        for ix in range(nx - 1):
            a, b = grid[iy][ix], grid[iy][ix + 1]
            c, d = grid[iy + 1][ix + 1], grid[iy + 1][ix]
            if a and b and c and d:
                f = bm.faces.new([a, b, c, d])
                if out_faces is not None:
                    out_faces.append(f)


def _add_waterfall_curtain(bm, top_p, bot_p, half_w_top, half_w_bot, curve_fwd, foam_hints):
    rows = 14
    cols = 8
    drop_dir = Vector((bot_p.x - top_p.x, bot_p.y - top_p.y, 0.0))
    fwd = drop_dir.normalized() if drop_dir.length > 0.05 else Vector((0.0, -1.0, 0.0))
    side = Vector((-fwd.y, fwd.x, 0.0))

    grid = []
    back_grid = []
    for r in range(rows):
        t = r / (rows - 1)
        arc_fwd = math.sin(t * math.pi * 0.85) * curve_fwd
        w = half_w_top + (half_w_bot - half_w_top) * (t ** 0.75)
        center = top_p.lerp(bot_p, t) + fwd * arc_fwd
        row = []
        brow = []
        for c in range(cols):
            u = (c / (cols - 1)) * 2.0 - 1.0
            ripple = 0.18 * math.sin(u * 4.5 + t * 5.0) + 0.12 * noise.noise(Vector((u * 3.0, t * 4.0, 2.2)))
            p = center + side * (u * w) + fwd * ripple
            row.append(bm.verts.new(p))
            brow.append(bm.verts.new(p - fwd * 0.08))
        grid.append(row)
        back_grid.append(brow)

    for r in range(rows - 1):
        for c in range(cols - 1):
            bm.faces.new([grid[r][c], grid[r][c + 1], grid[r + 1][c + 1], grid[r + 1][c]])
            bm.faces.new([back_grid[r][c], back_grid[r + 1][c], back_grid[r + 1][c + 1], back_grid[r][c + 1]])

    foam_hints.append(Vector((bot_p.x, bot_p.y, bot_p.z)))
    res = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0)
    for v in res["verts"]:
        p = v.co.copy()
        n = 0.65 + 0.45 * _ridged(p * 2.4 + bot_p)
        p.x *= half_w_bot * 0.95 * n
        p.y *= half_w_bot * 0.65 * n
        p.z = max(-0.1, p.z) * 0.42 * n
        v.co = bot_p + p


def build_water_system(walk_bvh, rng):
    """CHỈ giữ nguồn dịch độc nhỏ ở đoạn trên cao phía Bắc (+8.85m & +5.45m); BỎ hoàn toàn nước ở giữa khoang."""
    bm_water = bmesh.new()
    foam_hints = []
    sheet_faces = []

    # Hố dịch độc nhỏ tầng lửng (+5.45m)
    _build_water_ribbon_sheet(
        bm_water, -22.0, 8.0, 24.0, 48.0, 34, 30,
        lambda x, y: TERRACE2_WATER_Z + 0.02 * math.sin(x * 0.5 + y * 0.4),
        lambda x, y: _terrace2_dist(x, y) < 0.89,
        sheet_faces,
    )

    # Hố dịch độc thượng nguồn (+8.85m)
    _build_water_ribbon_sheet(
        bm_water, -8.0, 24.0, 36.0, 62.0, 34, 30,
        lambda x, y: TERRACE3_WATER_Z + 0.02 * math.sin(x * 0.5 + y * 0.4),
        lambda x, y: _terrace3_dist(x, y) < 0.89,
        sheet_faces,
    )

    # Thác dịch độc mảnh từ khe đá vách Bắc -> Hố +8.85m -> Hố +5.45m -> ngấm tắt dần vào hốc đá
    _add_waterfall_curtain(bm_water, Vector((10.0, 66.0, 23.0)), Vector((9.0, 58.0, 14.5)), 2.2, 2.8, 1.8, foam_hints)
    _add_waterfall_curtain(bm_water, Vector((9.0, 58.0, 14.8)), Vector((8.0, 53.0, TERRACE3_WATER_Z - 0.1)), 2.6, 3.6, 2.0, foam_hints)
    _add_waterfall_curtain(bm_water, Vector((1.0, 42.0, TERRACE3_WATER_Z + 0.05)), Vector((-2.5, 38.0, TERRACE2_WATER_Z - 0.1)), 2.0, 2.9, 1.3, foam_hints)
    _add_waterfall_curtain(bm_water, Vector((-8.0, 27.5, TERRACE2_WATER_Z + 0.05)), Vector((-8.5, 22.0, 1.6)), 1.4, 2.0, 1.2, foam_hints)

    bm_water.normal_update()
    for f in sheet_faces:
        if f.normal.z < 0.0:
            f.normal_flip()
    water_obj = kit.finish_object(bm_water, "MazeChamber_Crystal_Water", _make_toxic_fluid_material("MazeChamber_Crystal_FluidMat"), recalc_normals=False)

    def paint_toxic_fluid(p, n):
        if n.z < 0.55:
            streak = 0.5 + 0.5 * math.sin(p.x * 2.4 + p.y * 1.6 + noise.noise(p * 0.5) * 3.0)
            return _mix(TOXIC_DEEP, TOXIC_FROTH, 0.22 + 0.52 * streak)
        min_d2 = 1e9
        for fh in foam_hints:
            d2 = (p - fh).length_squared
            if d2 < min_d2:
                min_d2 = d2
        if min_d2 < 20.0:
            foam_t = 1.0 - _smoothstep(0.8, 4.2, math.sqrt(min_d2))
            if foam_t > 0.05:
                return _mix(TOXIC_VIOLET, TOXIC_FROTH, foam_t * 0.85)
        n_w = 0.5 + 0.5 * noise.noise(p * 0.16 + Vector((1.5, 2.8, 0.0)))
        return _mix(TOXIC_DEEP, TOXIC_VIOLET, 0.30 + 0.70 * n_w)

    kit.paint(water_obj, paint_toxic_fluid)
    return [water_obj]


# ---------------------------------------------------------------- Xương Sườn & Móng Vuốt, Răng Nanh Vực, Hốc Mắt & Quặng Thô

def ray_ground(walk_bvh, x, y, start_z=28.0):
    loc, norm, _, _ = walk_bvh.ray_cast(Vector((x, y, start_z)), Vector((0.0, 0.0, -1.0)), 75.0)
    return loc, norm


def _add_eldritch_rib_and_claw_monolith(bm_rock, bm_crystal, base, inward_dir, scale, rng, glow_hints, pal_id, variant=0):
    """Khung Xương Sườn Đá Đen, Móng Vuốt & Mạch Quặng Thô Khổng Lồ (14m-23m), bọc giáp đá hắc diện góc cạnh."""
    # Bệ đá nứt toác nhiều khối góc cạnh làm móng neo vững chắc
    _add_faceted_obsidian_rock(
        bm_rock,
        base + Vector((0.0, 0.0, -0.5)),
        Vector((4.0 * scale, 3.6 * scale, 2.8 * scale)),
        base.x + base.y,
        (0.0, 0.0, rng.uniform(0.0, 6.28)),
    )
    _add_faceted_obsidian_rock(
        bm_rock,
        base + Vector((inward_dir.x * 1.4 * scale, inward_dir.y * 1.4 * scale, 1.4 * scale)),
        Vector((2.6 * scale, 2.4 * scale, 3.2 * scale)),
        base.x * 0.7 - base.y,
        (rng.uniform(-0.3, 0.3), rng.uniform(-0.3, 0.3), rng.uniform(0.0, 6.28)),
    )
    # Gắn 5 khối quặng thô đa diện lớn mọc quanh chân đá
    for ck in range(5):
        ca = ck * (2.0 * math.pi / 5.0) + rng.uniform(-0.25, 0.25)
        cpos = base + Vector((math.cos(ca) * 2.5 * scale, math.sin(ca) * 2.5 * scale, rng.uniform(0.6, 1.8) * scale))
        _add_raw_ore_chunk(
            bm_crystal, cpos,
            Vector((1.35 * scale, 1.15 * scale, rng.uniform(1.8, 3.4) * scale)),
            base.x + ck,
            (rng.uniform(-0.45, 0.45), rng.uniform(-0.45, 0.45), ca),
        )
        glow_hints.append((cpos, (pal_id + ck) % 2))

    dx, dy = inward_dir.x, inward_dir.y
    perp_x, perp_y = -dy, dx
    h = rng.uniform(14.5, 22.0) * scale

    if variant == 1:
        # Kiểu Vỉa Quặng Đổ Nghiêng Bất Đối Xứng (cột quặng góc nghiêng gắt 30°-50° + đá bọc lưng)
        main_pts = [
            base + Vector((0.0, 0.0, -1.8)),
            base + Vector((dx * 2.4, dy * 2.4, h * 0.35)),
            base + Vector((dx * 5.8 + perp_x * 1.6, dy * 5.8 + perp_y * 1.6, h * 0.65)),
            base + Vector((dx * 8.2 + perp_x * 2.4, dy * 8.2 + perp_y * 2.4, h * 0.85)),
        ]
        # Giáp đá hắc diện bọc phần gốc thân
        _add_crystal_prism_branch(bm_rock, main_pts[:3], 1.75 * scale, 1.05 * scale, 1.4 * scale, segs=6, twist=0.15)
        _add_crystal_prism_branch(bm_crystal, main_pts, 1.38 * scale, 0.52 * scale, 3.6 * scale, segs=6, twist=0.22)
        glow_hints.append((main_pts[-1], pal_id))

        # Cột phụ đâm xiên chéo thấp bên hông (bất đối xứng)
        sub_pts = [
            base + Vector((-perp_x * 1.8, -perp_y * 1.8, -1.0)),
            base + Vector((dx * 2.0 - perp_x * 3.8, dy * 2.0 - perp_y * 3.8, h * 0.28)),
            base + Vector((dx * 4.5 - perp_x * 5.2, dy * 4.5 - perp_y * 5.2, h * 0.50)),
        ]
        _add_crystal_prism_branch(bm_crystal, sub_pts, 1.05 * scale, 0.40 * scale, 2.6 * scale, segs=6)
        glow_hints.append((sub_pts[-1], 1 if pal_id == 0 else 0))
    else:
        # Kiểu Xương Sườn / Móng Vuốt Quái Thú (thân đá hắc diện đốt sống + ngọn vuốt quặng máu)
        main_pts = [
            base + Vector((0.0, 0.0, -1.8)),
            base + Vector((dx * 1.2, dy * 1.2, h * 0.36)),
            base + Vector((dx * 5.4, dy * 5.4, h * 0.74)),
            base + Vector((dx * 9.8, dy * 9.8, h * 0.88)),
        ]
        _add_crystal_prism_branch(bm_rock, main_pts[:3], 1.65 * scale, 0.95 * scale, 1.6 * scale, segs=6, twist=0.2)
        _add_crystal_prism_branch(bm_crystal, main_pts, 1.42 * scale, 0.48 * scale, 3.8 * scale, segs=6, twist=0.25)
        glow_hints.append((main_pts[-1], pal_id))

        # Khớp đốt đá & khối quặng mọc dọc sống lưng xương sườn
        for step_t in (0.35, 0.62):
            spine_p = main_pts[1].lerp(main_pts[2], step_t)
            _add_faceted_obsidian_rock(
                bm_rock, spine_p,
                Vector((1.4 * scale, 1.4 * scale, 1.1 * scale)),
                spine_p.z, (0.2, 0.2, step_t * 3.0),
            )
            _add_raw_ore_chunk(
                bm_crystal, spine_p + Vector((0.0, 0.0, 0.9 * scale)),
                Vector((0.8 * scale, 0.8 * scale, 1.4 * scale)),
                spine_p.x, (0.3, -0.3, step_t * 4.0),
            )

        # Chỉ 1 ngón vuốt phụ bất đối xứng chẻ ngang (tránh tạo hình cây chữ Y đối xứng!)
        side = 1.0 if (int(abs(base.x)) % 2 == 0) else -1.0
        fork_p = main_pts[1].lerp(main_pts[2], 0.45)
        fx = dx * 0.68 + perp_x * side * 0.74
        fy = dy * 0.68 + perp_y * side * 0.74
        bh = h * 0.54
        branch_pts = [
            fork_p,
            fork_p + Vector((fx * 2.8, fy * 2.8, bh * 0.45)),
            fork_p + Vector((fx * 5.2, fy * 5.2, bh * 0.78)),
        ]
        _add_crystal_prism_branch(bm_crystal, branch_pts, 0.86 * scale, 0.30 * scale, 2.4 * scale, segs=6)
        glow_hints.append((branch_pts[-1], 1 if pal_id == 0 else 0))

    # 6 gai chông quặng sắc nhọn chĩa tua tủa quanh gốc
    for k in range(6):
        sa = k * (2.0 * math.pi / 6.0) + rng.uniform(-0.25, 0.25)
        sx, sy = math.cos(sa), math.sin(sa)
        sh = rng.uniform(3.2, 7.6) * scale
        sprout_pts = [
            base + Vector((sx * 0.9, sy * 0.9, -1.4)),
            base + Vector((sx * 2.6, sy * 2.6, sh * 0.48)),
            base + Vector((sx * 4.1, sy * 4.1, sh)),
        ]
        _add_crystal_prism_branch(bm_crystal, sprout_pts, 0.68 * scale, 0.22 * scale, 1.5 * scale, segs=6)
        glow_hints.append((sprout_pts[-1], pal_id))


def _add_raw_ore_vein_outcrop(bm_rock, bm_crystal, loc, scale, rng, glow_hints, pal_id):
    """Vỉa Quặng Thô Lộ Thiên & Khối U Quặng Nứt Toác: đá đen nứt nhồi đầy các khối quặng tinh thể thô."""
    _add_faceted_obsidian_rock(
        bm_rock,
        loc + Vector((0.0, 0.0, -0.35)),
        Vector((2.6 * scale, 2.1 * scale, 1.5 * scale)),
        loc.x + loc.y,
        (rng.uniform(-0.2, 0.2), rng.uniform(-0.2, 0.2), rng.uniform(0.0, 6.28)),
    )
    for k in range(rng.randint(5, 8)):
        sa = rng.uniform(0.0, 2.0 * math.pi)
        dist = rng.uniform(0.25, 1.5) * scale
        pos = loc + Vector((math.cos(sa) * dist, math.sin(sa) * dist, rng.uniform(0.2, 1.1) * scale))
        sx = rng.uniform(0.58, 1.25) * scale
        sy = rng.uniform(0.54, 1.12) * scale
        sz = rng.uniform(1.05, 2.45) * scale
        _add_raw_ore_chunk(
            bm_crystal, pos, Vector((sx, sy, sz)),
            loc.x * 0.1 + k * 2.3,
            (rng.uniform(-0.55, 0.55), rng.uniform(-0.55, 0.55), sa),
        )
        glow_hints.append((pos, pal_id))
    for k in range(3):
        sa = k * 2.1 + rng.uniform(-0.35, 0.35)
        p0 = loc + Vector((0.0, 0.0, -0.5))
        p1 = loc + Vector((math.cos(sa) * 2.1 * scale, math.sin(sa) * 2.1 * scale, rng.uniform(1.8, 3.8) * scale))
        _add_crystal_prism_branch(bm_crystal, [p0, p0.lerp(p1, 0.5), p1], 0.46 * scale, 0.16 * scale, 1.2 * scale, segs=6)
        glow_hints.append((p1, pal_id))


def _add_abyssal_eye_geode(bm_rock, bm_crystal, loc, norm, scale, glow_hints):
    """Hốc Quặng 'Con Mắt Vực Thẳm' trên vách đá đen với nhân quặng đỏ máu nhìn xuống lòng vực."""
    ref = Vector((0.0, 0.0, 1.0)) if abs(norm.z) < 0.85 else Vector((1.0, 0.0, 0.0))
    u_tan = norm.cross(ref).normalized()
    v_tan = norm.cross(u_tan).normalized()

    for side in (-1.0, 1.0):
        lid_pts = [
            loc + v_tan * (2.6 * scale) - norm * (0.4 * scale),
            loc + u_tan * (side * 1.3 * scale) + norm * (0.6 * scale),
            loc - v_tan * (2.6 * scale) - norm * (0.4 * scale),
        ]
        rgs = _parallel_tube_rings(bm_rock, _smooth_path(lid_pts, 3), [0.55 * scale] * 7, 5)
        kit.bridge_rings(bm_rock, rgs)

    pupil_pts = [
        loc - v_tan * (1.2 * scale) + norm * (0.2 * scale),
        loc + norm * (0.5 * scale),
        loc + v_tan * (1.2 * scale) + norm * (0.2 * scale),
    ]
    _add_crystal_prism_branch(bm_crystal, pupil_pts, 0.25 * scale, 0.35 * scale, 0.9 * scale, segs=4)
    _add_raw_ore_chunk(
        bm_crystal, loc + norm * (0.3 * scale),
        Vector((0.75 * scale, 0.75 * scale, 1.65 * scale)),
        loc.x + loc.y,
        (0.0, 0.0, 0.0),
    )
    glow_hints.append((loc + norm * (0.4 * scale), 3))


def build_crystal_architecture_and_ceiling_life(walk_bvh, stalactite_buds, rng):
    bm_stone_root = bmesh.new()
    bm_crystal = bmesh.new()
    glow_hints = []

    # 1. 10 Khung Xương Sườn & Vỉa Quặng Đổ Nghiêng (bất đối xứng, bọc đá hắc diện)
    monolith_specs = [
        (-28.0, _chasm_center_y(-28.0) - 10.5, 1.0, 1.12, 0, 0),
        (-22.0, _chasm_center_y(-22.0) + 10.5, -1.0, 1.08, 1, 1),
        (-12.0, _chasm_center_y(-12.0) - 11.0, 1.0, 1.15, 0, 0),
        (4.0, _chasm_center_y(4.0) + 11.0, -1.0, 1.10, 1, 1),
        (16.0, _chasm_center_y(16.0) - 10.5, 1.0, 1.05, 0, 0),
        (32.0, _chasm_center_y(32.0) + 10.0, -1.0, 1.04, 0, 1),
        (-42.0, -16.0, 0.8, 1.10, 1, 0),
        (38.0, 28.0, -0.8, 1.04, 0, 1),
        (-16.0, 48.0, -0.7, 1.06, 0, 0),
        (18.0, -32.0, 0.9, 1.02, 1, 1),
    ]
    for gx, gy, y_dir, gscale, pal_id, var in monolith_specs:
        loc, _ = ray_ground(walk_bvh, gx, gy, start_z=30.0)
        if loc is None:
            continue
        inward = Vector((rng.uniform(-0.35, 0.35), y_dir, 0.0)).normalized()
        _add_eldritch_rib_and_claw_monolith(bm_stone_root, bm_crystal, loc, inward, gscale, rng, glow_hints, pal_id, variant=var)

    # 2. 36 Răng Nanh Vực Thẳm đâm ngang từ vách đứng của Khe Vực (-4m .. -10m, có ổ đá neo chắc -> 0% lơ lửng)
    for fi in range(36):
        fx = -46.0 + 92.0 * (fi / 35.0) + rng.uniform(-1.0, 1.0)
        if _land_neck_factor(fx) > 0.35:
            continue
        side_y = 1.0 if (fi % 2 == 0) else -1.0
        cy = _chasm_center_y(fx)
        fz = rng.uniform(-9.5, -4.0)
        probe_o = Vector((fx, cy, fz))
        probe_d = Vector((0.0, side_y, 0.0))
        hit_loc, hit_norm, _, _ = walk_bvh.ray_cast(probe_o, probe_d, 15.0)
        if hit_loc is None or abs(hit_norm.z) > 0.48:
            continue
        _add_faceted_obsidian_rock(
            bm_stone_root, hit_loc - hit_norm * 0.4,
            Vector((1.4, 1.4, 1.2)), fi * 2.7, (0.0, 0.0, 0.0),
        )
        fang_dir = Vector((rng.uniform(-0.3, 0.3), -side_y, rng.uniform(0.25, 0.60))).normalized()
        fang_len = rng.uniform(3.2, 5.8)
        p0 = hit_loc - hit_norm * 1.4
        p1 = hit_loc + fang_dir * fang_len
        _add_crystal_prism_branch(bm_crystal, [p0, p0.lerp(p1, 0.55), p1], rng.uniform(0.55, 0.90), 0.16, 1.4, segs=6)
        glow_hints.append((p1, 1 if fi % 3 == 0 else 0))

    # 16 Vỉa Quặng Đỏ Máu & Tím Thẫm rực sáng dưới đáy Vết Nứt Vực Thẳm (-14m)
    for bi in range(16):
        bx = -44.0 + 88.0 * (bi / 15.0) + rng.uniform(-1.5, 1.5)
        if _land_neck_factor(bx) > 0.35:
            continue
        by = _chasm_center_y(bx) + rng.uniform(-2.2, 2.2)
        loc, _ = ray_ground(walk_bvh, bx, by, start_z=-2.0)
        if loc is None or loc.z > -8.0:
            continue
        _add_raw_ore_vein_outcrop(bm_stone_root, bm_crystal, loc, rng.uniform(1.1, 1.65), rng, glow_hints, 1 if bi % 2 == 0 else 0)

    # 3. 32 Vỉa Quặng Thô Lộ Thiên & Khối U Quặng Nứt Toác kích thước lớn trên gò đá nền (tránh che camera)
    cam_clears = [(24.0, -56.0), (6.0, -25.0), (-22.0, 44.0), (-25.0, -15.0)]
    placed_seams = 0
    for ti in range(120):
        if placed_seams >= 32:
            break
        ang = rng.uniform(0.0, 2.0 * math.pi)
        rad = rng.uniform(0.10, 0.72) ** 0.80
        tx = math.cos(ang) * 92.0 * rad
        ty = math.sin(ang) * 72.0 * rad
        if any((tx - kx) ** 2 + (ty - ky) ** 2 < 49.0 for kx, ky in cam_clears):
            continue
        loc, norm = ray_ground(walk_bvh, tx, ty, start_z=24.0)
        if loc is None or norm.z < 0.48 or loc.z < -2.0 or loc.z > 13.0:
            continue
        if (_terrace2_dist(loc.x, loc.y) < 0.88 and loc.z < TERRACE2_WATER_Z + 0.15) or (_terrace3_dist(loc.x, loc.y) < 0.88 and loc.z < TERRACE3_WATER_Z + 0.15):
            continue
        pal = 1 if (ti % 3 == 0) else 0
        _add_raw_ore_vein_outcrop(bm_stone_root, bm_crystal, loc, rng.uniform(1.10, 1.75), rng, glow_hints, pal)
        placed_seams += 1

    # 4. 14 Hốc Quặng "Con Mắt Vực Thẳm" nhìn chằm chằm từ vách đá đen & trụ đá
    placed_eyes = 0
    for ei in range(60):
        if placed_eyes >= 14:
            break
        ang = rng.uniform(0.0, 2.0 * math.pi)
        if min(abs(_delta_ang(ang, ta)) for ta in TUNNEL_ANGLES) < math.radians(14.0):
            continue
        origin = Vector((math.cos(ang) * 35.0, math.sin(ang) * 28.0, rng.uniform(7.5, 18.0)))
        direction = Vector((math.cos(ang), math.sin(ang), rng.uniform(-0.04, 0.14))).normalized()
        loc, norm, _, _ = walk_bvh.ray_cast(origin, direction, 85.0)
        if loc is None or abs(norm.z) > 0.38:
            continue
        _add_abyssal_eye_geode(bm_stone_root, bm_crystal, loc, norm, rng.uniform(1.2, 1.8), glow_hints)
        placed_eyes += 1

    # 5. 8 Hệ Gân Đá Hắc Diện Vặn Xoắn trên vòm trần
    root_anchors = [
        ((-52.0, 24.0), (14.0, -14.0)),
        ((-36.0, -32.0), (34.0, 26.0)),
        ((44.0, -18.0), (-10.0, 38.0)),
        ((-60.0, -8.0), (-12.0, -40.0)),
        ((18.0, 48.0), (-38.0, 20.0)),
        ((46.0, 28.0), (18.0, -34.0)),
        ((-22.0, 42.0), (30.0, 14.0)),
        ((-12.0, -44.0), (40.0, -8.0)),
    ]
    for ri, (p0, p1) in enumerate(root_anchors):
        pts, radii = [], []
        for s in range(13):
            t = s / 12.0
            px = p0[0] + (p1[0] - p0[0]) * t + 4.5 * math.sin(t * math.pi * 2.0 + ri)
            py = p0[1] + (p1[1] - p0[1]) * t + 3.8 * math.cos(t * math.pi * 1.6 + ri)
            cz = ceiling_height(px, py)
            pz = cz - 1.5 - 5.2 * math.sin(t * math.pi)
            pts.append(Vector((px, py, pz)))
            radii.append(0.75 * (1.0 - 0.35 * math.sin(t * math.pi)))
        rgs = _parallel_tube_rings(bm_stone_root, pts, radii, 7)
        kit.bridge_rings(bm_stone_root, rgs)

        for k in range(2, len(pts) - 2, 2):
            cp = pts[k]
            sh_pts = [cp, cp + Vector((rng.uniform(-0.8, 0.8), rng.uniform(-0.8, 0.8), -rng.uniform(2.2, 4.6)))]
            _add_crystal_prism_branch(bm_crystal, sh_pts, 0.48, 0.16, 1.4, segs=6)
            glow_hints.append((sh_pts[-1], 1 if (ri + k) % 4 == 0 else 0))

    # 6. 34 Cụm Răng Nanh Quặng Rủ Ngược trên vòm trần
    for bi, tip in enumerate(stalactite_buds[:34]):
        pal = 1 if (bi % 4 == 0) else 0
        for k in range(rng.randint(4, 7)):
            ang = k * 1.15 + rng.uniform(-0.2, 0.2)
            dx, dy = math.cos(ang), math.sin(ang)
            drop_l = rng.uniform(3.2, 7.0) if k == 0 else rng.uniform(1.8, 4.4)
            c_pts = [
                tip + Vector((dx * 0.25, dy * 0.25, 0.8)),
                tip + Vector((dx * 0.85, dy * 0.85, -drop_l * 0.6)),
                tip + Vector((dx * 1.15, dy * 1.15, -drop_l)),
            ]
            _add_crystal_prism_branch(bm_crystal, c_pts, 0.48, 0.22, 1.5, segs=6)
            glow_hints.append((c_pts[-1], pal))

    # 7. 42 Chuỗi Tơ Khoáng Đen-Tím rủ từ trần xuống (6m-16m)
    for vi in range(42):
        ang = rng.uniform(0.0, 2.0 * math.pi)
        rad = rng.uniform(0.08, 0.75)
        vx = math.cos(ang) * 92.0 * rad
        vy = math.sin(ang) * 70.0 * rad
        if _in_ceiling_slit(vx, vy):
            continue
        cz = ceiling_height(vx, vy)
        drop_len = rng.uniform(6.0, 16.0)
        if cz - drop_len < max(terrain_height(vx, vy), 9.0) + 6.5:
            continue

        pal = 1 if (vi % 4 == 0) else 0
        v_pts, v_rad = [], []
        for s in range(8):
            t = s / 7.0
            sway_x = 0.70 * math.sin(t * 4.5 + vi)
            sway_y = 0.65 * math.cos(t * 3.8 + vi * 0.7)
            v_pts.append(Vector((vx + sway_x, vy + sway_y, cz + 0.5 - drop_len * t)))
            v_rad.append(0.11 * (1.0 - 0.50 * t))
        rgs = _parallel_tube_rings(bm_stone_root, v_pts, v_rad, 5)
        kit.bridge_rings(bm_stone_root, rgs)

        for kn in (3, 5, 7):
            kp = v_pts[kn]
            _add_raw_ore_chunk(
                bm_crystal, kp,
                Vector((0.36, 0.36, 0.75 if kn < 7 else 1.10)),
                vi * 1.7 + kn,
                (0.0, 0.0, rng.uniform(0, 6.28)),
            )
            glow_hints.append((kp, pal))

    bmesh.ops.recalc_face_normals(bm_stone_root, faces=bm_stone_root.faces[:])
    root_obj = kit.finish_object(bm_stone_root, "MazeChamber_Crystal_Roots", _make_obsidian_material("MazeChamber_Crystal_RootMat"))
    for p in root_obj.data.polygons:
        p.use_smooth = False
    kit.paint(root_obj, lambda p, n: _mix(OBSIDIAN_ABYSS, CORRUPT_CRUST, 0.22 + 0.50 * _ridged(p * 0.22)))

    bmesh.ops.recalc_face_normals(bm_crystal, faces=bm_crystal.faces[:])
    crystal_obj = kit.finish_object(bm_crystal, "MazeChamber_Crystal_Glow", _make_ore_crystal_material("MazeChamber_Crystal_OreMat", energy=0.18, roughness=0.10))
    for p in crystal_obj.data.polygons:
        p.use_smooth = False

    def paint_crystal(p, n):
        best_pal = 0
        best_d2 = 1e9
        for hp, pal_id in glow_hints:
            d2 = (p - hp).length_squared
            if d2 < best_d2:
                best_d2 = d2
                best_pal = pal_id
        if best_pal == 3:
            return ORE_EYE_CORE
        t_height = _clamp01(0.12 + 0.88 * abs(math.sin(p.z * 0.14 + p.x * 0.06)))
        if best_pal == 1:
            return _mix(ORE_BLOOD_DARK, ORE_BLOOD_BRIGHT, t_height * 0.85)
        return _mix(ORE_PURPLE_DARK, ORE_AMETHYST_EDGE, t_height * 0.80)

    kit.paint(crystal_obj, paint_crystal)
    return [root_obj, crystal_obj]


# ---------------------------------------------------------------- Phiến Đá Hắc Diện Vỡ & 4 Mẫu Quặng/Khoáng Biến Dị (0% Thực Vật)

def _build_obsidian_slab_cluster():
    """Phiến đá hắc diện vỡ gãy nhô nghiêng (0% cây cỏ)."""
    bm = bmesh.new()
    rng = random.Random(707)
    for k in range(4):
        ang = k * (2.0 * math.pi / 4.0) + rng.uniform(-0.35, 0.35)
        dist = rng.uniform(0.08, 0.34)
        ox, oy = math.cos(ang) * dist, math.sin(ang) * dist
        h = rng.uniform(0.40, 0.95)
        _add_raw_ore_chunk(
            bm,
            Vector((ox, oy, h * 0.35 - 0.15)),
            Vector((rng.uniform(0.24, 0.42), rng.uniform(0.14, 0.26), h)),
            k * 3.1,
            (rng.uniform(-0.45, 0.45), rng.uniform(-0.45, 0.45), ang),
        )
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm


def _build_raw_ore_chunk_cluster():
    """Mẫu 1: Khối Quặng Thô Đa Diện mọc cụm từ nền đá đen."""
    bm = bmesh.new()
    rng = random.Random(801)
    for k in range(4):
        ang = k * (2.0 * math.pi / 4.0) + rng.uniform(-0.3, 0.3)
        dist = rng.uniform(0.08, 0.28)
        ox, oy = math.cos(ang) * dist, math.sin(ang) * dist
        h = rng.uniform(0.48, 1.05)
        _add_raw_ore_chunk(
            bm,
            Vector((ox, oy, h * 0.4 - 0.10)),
            Vector((rng.uniform(0.20, 0.34), rng.uniform(0.18, 0.30), h)),
            k * 4.7,
            (rng.uniform(-0.5, 0.5), rng.uniform(-0.5, 0.5), ang),
        )
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm


def _build_split_void_spine_cluster():
    """Mẫu 2: Gai Sừng Tinh Thể Nứt Đôi — chồi khoáng cứng rẽ nhánh nhọn hoắt."""
    bm = bmesh.new()
    rng = random.Random(802)
    for k in range(4):
        ang = k * (2.0 * math.pi / 4.0) + rng.uniform(-0.25, 0.25)
        dist = rng.uniform(0.06, 0.26)
        ox, oy = math.cos(ang) * dist, math.sin(ang) * dist
        h = rng.uniform(0.75, 1.35)
        fork_p = Vector((ox + math.cos(ang) * 0.22, oy + math.sin(ang) * 0.22, h * 0.55))
        _add_crystal_prism_branch(bm, [Vector((ox, oy, -0.18)), fork_p], 0.08, 0.06, 0.05, segs=4)
        for side in (-1.0, 1.0):
            fa = ang + side * 0.65
            tip = fork_p + Vector((math.cos(fa) * 0.24, math.sin(fa) * 0.24, h * 0.38))
            _add_crystal_prism_branch(bm, [fork_p, tip], 0.055, 0.025, 0.22, segs=4)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm


def _build_barbed_mineral_tendril_cluster():
    """Mẫu 3: Râu Khoáng Gai Xoắn Quái Dị — các ngạnh khoáng cong đính gai sắc."""
    bm = bmesh.new()
    rng = random.Random(803)
    for k in range(4):
        ang = k * (2.0 * math.pi / 4.0) + rng.uniform(-0.25, 0.25)
        h = rng.uniform(0.65, 1.18)
        pts = [
            Vector((0.0, 0.0, -0.15)),
            Vector((math.cos(ang) * 0.35, math.sin(ang) * 0.35, h * 0.65)),
            Vector((math.cos(ang + 0.25) * 0.45, math.sin(ang + 0.25) * 0.45, h)),
        ]
        _add_crystal_prism_branch(bm, pts, 0.065, 0.025, 0.25, segs=4)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm


def _build_stacked_shard_crust_cluster():
    """Mẫu 4: Phiến Tinh Thể Vảy Rồng Xếp Chồng — lưỡi dao pha lê 4 cạnh sắc nhọn (0% tròn như nấm)."""
    bm = bmesh.new()
    rng = random.Random(804)
    for k in range(5):
        ang = k * (2.0 * math.pi / 5.0) + rng.uniform(-0.25, 0.25)
        dist = rng.uniform(0.08, 0.30)
        ox, oy = math.cos(ang) * dist, math.sin(ang) * dist
        h = rng.uniform(0.55, 1.15)
        _add_raw_ore_chunk(
            bm,
            Vector((ox, oy, h * 0.45 - 0.08)),
            Vector((rng.uniform(0.28, 0.44), 0.08, h * 0.85)),
            k * 2.9,
            (rng.uniform(-0.4, 0.4), rng.uniform(0.35, 0.75), ang),
        )
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm


def _copy_bmesh_into(src_bm, dst_bm, xform):
    vmap = {}
    for v in src_bm.verts:
        vmap[v] = dst_bm.verts.new(xform @ v.co)
    for f in src_bm.faces:
        dst_bm.faces.new([vmap[v] for v in f.verts])


def _is_under_upper_fluid(x, y, z):
    if _terrace2_dist(x, y) < 0.85 and z < TERRACE2_WATER_Z + 0.10:
        return True
    if _terrace3_dist(x, y) < 0.85 and z < TERRACE3_WATER_Z + 0.10:
        return True
    return False


def build_raw_ore_deposits(walk_bvh, rng):
    """0% thực vật! Rải ~360 Phiến Đá Hắc Diện Vỡ + ~240 Cụm Quặng Thô 4 loại sắc cạnh (~45k-75k tris)."""
    bm_slabs = bmesh.new()
    bm_ores = bmesh.new()
    ore_hints = []

    bm_slab_tmpl = _build_obsidian_slab_cluster()
    ore_templates = [
        _build_raw_ore_chunk_cluster(),
        _build_split_void_spine_cluster(),
        _build_barbed_mineral_tendril_cluster(),
        _build_stacked_shard_crust_cluster(),
    ]

    # 1. Rải ~360 Phiến Đá Hắc Diện Gãy Sắc
    slabs_placed = 0
    for _ in range(1100):
        if slabs_placed >= 360:
            break
        ang = rng.uniform(0.0, 2.0 * math.pi)
        rad = rng.uniform(0.04, 0.74) ** 0.74
        gx = math.cos(ang) * 94.0 * rad
        gy = math.sin(ang) * 74.0 * rad
        loc, norm = ray_ground(walk_bvh, gx, gy, start_z=24.0)
        if loc is None or norm.z < 0.45 or loc.z < -3.0 or loc.z > 13.5 or _is_under_upper_fluid(loc.x, loc.y, loc.z):
            continue

        sc = rng.uniform(0.90, 1.85)
        yaw = rng.uniform(0.0, 2.0 * math.pi)
        rot_tilt = Vector((0.0, 0.0, 1.0)).rotation_difference(norm).to_matrix().to_4x4()
        xform = Matrix.Translation(loc + Vector((0.0, 0.0, -0.06))) @ rot_tilt @ Matrix.Rotation(yaw, 4, "Z") @ Matrix.Scale(sc, 4)
        _copy_bmesh_into(bm_slab_tmpl, bm_slabs, xform)
        slabs_placed += 1

    # 2. Rải ~240 Cụm Quặng Thô & Khoáng Biến Dị (4 kiểu sắc cạnh)
    ores_placed = 0
    for _ in range(1000):
        if ores_placed >= 240:
            break
        ang = rng.uniform(0.0, 2.0 * math.pi)
        rad = rng.uniform(0.04, 0.72) ** 0.78
        ux = math.cos(ang) * 92.0 * rad
        uy = math.sin(ang) * 72.0 * rad
        loc, norm = ray_ground(walk_bvh, ux, uy, start_z=24.0)
        if loc is None or norm.z < 0.45 or loc.z < -3.0 or loc.z > 13.0 or _is_under_upper_fluid(loc.x, loc.y, loc.z):
            continue

        k_idx = rng.randrange(len(ore_templates))
        src_bm = ore_templates[k_idx]
        sc = rng.uniform(0.85, 1.55)
        yaw = rng.uniform(0.0, 2.0 * math.pi)
        rot_tilt = Vector((0.0, 0.0, 1.0)).rotation_difference(norm).to_matrix().to_4x4()
        xform = Matrix.Translation(loc + Vector((0.0, 0.0, -0.05))) @ rot_tilt @ Matrix.Rotation(yaw, 4, "Z") @ Matrix.Scale(sc, 4)
        _copy_bmesh_into(src_bm, bm_ores, xform)
        ore_hints.append((loc, 1 if ores_placed % 3 == 0 else 0))
        ores_placed += 1

    bm_slab_tmpl.free()
    for sbm in ore_templates:
        sbm.free()

    bmesh.ops.recalc_face_normals(bm_slabs, faces=bm_slabs.faces[:])
    slabs_obj = kit.finish_object(bm_slabs, "MazeChamber_Crystal_Slabs", _make_obsidian_material("MazeChamber_Crystal_SlabMat"))
    for p in slabs_obj.data.polygons:
        p.use_smooth = False
    kit.paint(slabs_obj, lambda p, n: _mix(OBSIDIAN_ABYSS, CORRUPT_CRUST, 0.25 + 0.55 * _ridged(p * 0.28)))

    bmesh.ops.recalc_face_normals(bm_ores, faces=bm_ores.faces[:])
    ores_obj = kit.finish_object(bm_ores, "MazeChamber_Crystal_SmallOres", _make_ore_crystal_material("MazeChamber_Crystal_SmallOreMat", energy=0.22, roughness=0.12))
    for p in ores_obj.data.polygons:
        p.use_smooth = False

    def paint_ores(p, n):
        best_pal = 0
        best_d2 = 1e9
        for hp, pal_id in ore_hints:
            d2 = (p - hp).length_squared
            if d2 < best_d2:
                best_d2 = d2
                best_pal = pal_id
        t_h = _clamp01((p.z - terrain_height(p.x, p.y)) * 1.1)
        if best_pal == 1:
            return _mix(ORE_BLOOD_DARK, ORE_BLOOD_BRIGHT, 0.15 + 0.80 * t_h)
        return _mix(ORE_PURPLE_DARK, ORE_VOID_VIOLET, 0.15 + 0.85 * t_h)

    kit.paint(ores_obj, paint_ores)
    return [slabs_obj, ores_obj]


# ---------------------------------------------------------------- Ánh sáng Kinh Dị Vực Thẳm & 4 Góc Camera Preview 3D

def render_previews(all_objects, out_dir):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 22
    scene.cycles.max_bounces = 4
    scene.cycles.diffuse_bounces = 2
    if hasattr(scene.cycles, "use_denoising"):
        scene.cycles.use_denoising = True
    scene.render.resolution_x, scene.render.resolution_y = 960, 640
    scene.render.image_settings.file_format = "PNG"

    world = bpy.data.worlds.new("AbyssalOreWorld")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.012, 0.006, 0.024, 1.0)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.60
    scene.world = world

    for o in list(scene.collection.objects):
        if o.type in {"CAMERA", "LIGHT"}:
            bpy.data.objects.remove(o, do_unlink=True)
        o.hide_render = o.name.endswith("_COL")

    sun = bpy.data.objects.new("_prev_sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy = 1.9
    sun.data.color = (0.74, 0.50, 1.0)
    sun.rotation_euler = (math.radians(38), math.radians(18), math.radians(28))
    kit.link(sun)

    fill_lights = [
        ("chasm_underglow_w", Vector((-20.0, _chasm_center_y(-20.0), -10.5)), (0.94, 0.08, 0.34), 7200.0),
        ("chasm_underglow_c", Vector((-2.0, _chasm_center_y(-2.0), -10.5)), (0.74, 0.12, 1.00), 7500.0),
        ("chasm_underglow_e", Vector((16.0, _chasm_center_y(16.0), -10.5)), (0.92, 0.08, 0.42), 7000.0),
        ("north_upper_pool", Vector((2.0, 44.0, 18.0)), (0.66, 0.22, 1.00), 5600.0),
        ("south_crag_rim", Vector((0.0, -24.0, 12.0)), (0.70, 0.14, 0.96), 5400.0),
        ("ceiling_horror", Vector((0.0, 6.0, 34.0)), (0.58, 0.14, 0.92), 6000.0),
        ("west_wall_eye", Vector((-46.0, 12.0, 18.0)), (0.88, 0.12, 0.44), 5000.0),
    ]
    for lname, lpos, lcol, lenergy in fill_lights:
        lobj = bpy.data.objects.new("_prev_" + lname, bpy.data.lights.new(lname, "POINT"))
        lobj.location = lpos
        lobj.data.color = lcol
        lobj.data.energy = lenergy
        lobj.data.shadow_soft_size = 7.0
        kit.link(lobj)

    cam = bpy.data.objects.new("_prev_cam", bpy.data.cameras.new("cam"))
    kit.link(cam)
    scene.camera = cam

    views = {
        "overview": (Vector((24.0, -56.0, 30.0)), Vector((-4.0, 6.0, -1.0)), 18.0),
        "valley": (Vector((6.0, -25.0, 6.0)), Vector((-6.0, -3.0, -5.5)), 20.0),
        "terraces": (Vector((-22.0, 44.0, 15.0)), Vector((6.0, 6.0, 0.5)), 20.0),
        "closeup": (Vector((-25.0, -15.0, 4.2)), Vector((-14.0, -6.0, 0.8)), 24.0),
    }

    for vname, (cpos, target, lens) in views.items():
        cam.location = cpos
        cam.data.lens = lens
        cam.rotation_euler = (target - cpos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(out_dir, f"maze_chamber_crystal_{vname}.png")
        bpy.ops.render.render(write_still=True)


def export_standalone_mineral_assets(out_dir):
    """Xuất các model khoáng biến dị / quặng thô đơn lẻ để làm Prefab tương tác rẽ ngang trong Unity."""
    print("Xuất các FBX quặng & khoáng biến dị tương tác...", flush=True)

    # 1. Gai Sừng Tinh Thể Nứt Đôi (Void Spine)
    bm_spine = _build_split_void_spine_cluster()
    spine_obj = kit.finish_object(bm_spine, "MazeMineral_VoidSpine_Glow", _make_ore_crystal_material("MazeMineral_VoidSpineMat", energy=0.25, roughness=0.10))
    for p in spine_obj.data.polygons:
        p.use_smooth = False
    kit.paint(spine_obj, lambda p, n: _mix(ORE_VOID_VIOLET, ORE_AMETHYST_EDGE, _clamp01(p.z * 0.6)))
    kit.export_fbx([spine_obj], os.path.join(out_dir, "MazeMineral_VoidSpine.fbx"))
    bpy.data.objects.remove(spine_obj, do_unlink=True)

    # 2. Râu Khoáng Gai Xoắn (Barbed Mineral Tendril)
    bm_tendril = _build_barbed_mineral_tendril_cluster()
    tendril_obj = kit.finish_object(bm_tendril, "MazeMineral_BarbedTendril_Glow", _make_ore_crystal_material("MazeMineral_TendrilMat", energy=0.25, roughness=0.10))
    for p in tendril_obj.data.polygons:
        p.use_smooth = False
    kit.paint(tendril_obj, lambda p, n: _mix(ORE_BLOOD_DARK, ORE_BLOOD_BRIGHT, _clamp01(p.z * 0.7)))
    kit.export_fbx([tendril_obj], os.path.join(out_dir, "MazeMineral_BarbedTendril.fbx"))
    bpy.data.objects.remove(tendril_obj, do_unlink=True)

    # 3. Phiến Tinh Thể Vảy Rồng (Shard Crust)
    bm_crust = _build_stacked_shard_crust_cluster()
    crust_obj = kit.finish_object(bm_crust, "MazeMineral_ShardCrust_Glow", _make_ore_crystal_material("MazeMineral_ShardMat", energy=0.25, roughness=0.10))
    for p in crust_obj.data.polygons:
        p.use_smooth = False
    kit.paint(crust_obj, lambda p, n: _mix(ORE_PURPLE_DARK, ORE_VOID_VIOLET, _clamp01(p.z * 0.8)))
    kit.export_fbx([crust_obj], os.path.join(out_dir, "MazeMineral_ShardCrust.fbx"))
    bpy.data.objects.remove(crust_obj, do_unlink=True)

    # 4. Khối Quặng Thô Đa Diện (Raw Chunk)
    bm_chunk = _build_raw_ore_chunk_cluster()
    chunk_obj = kit.finish_object(bm_chunk, "MazeMineral_RawChunk", _make_obsidian_material("MazeMineral_ChunkMat"))
    for p in chunk_obj.data.polygons:
        p.use_smooth = False
    kit.paint(chunk_obj, lambda p, n: _mix(OBSIDIAN_DARK, ORE_BLOOD_BRIGHT, _clamp01(p.z * 0.8)))
    kit.export_fbx([chunk_obj], os.path.join(out_dir, "MazeMineral_RawChunk.fbx"))
    bpy.data.objects.remove(chunk_obj, do_unlink=True)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=None)
    ap.add_argument("--previews", default=None)
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    args = ap.parse_args(argv)

    t0 = time.time()
    rng = random.Random(202611)
    kit.new_scene()

    print("[1/5] Dựng Nền Lồi Lõm Gãy Bậc, 42 Mỏm Đá & Vết Nứt Vực Thẳm (-15m, 0% cầu)...", flush=True)
    shell_obj, col_obj, walk_bvh, stalactite_buds = build_chamber_shell(rng)

    print("[2/5] Dựng 2 Hố Dịch Độc Nhỏ Chỉ Ở Đoạn Trên Cao Phía Bắc...", flush=True)
    water_objs = build_water_system(walk_bvh, rng)

    print("[3/5] Dựng 10 Xương Sườn/Vỉa Quặng Bất Đối Xứng, 36 Răng Nanh Vực, 32 Vỉa Quặng Thô & 14 Con Mắt...", flush=True)
    crystal_objs = build_crystal_architecture_and_ceiling_life(walk_bvh, stalactite_buds, rng)

    print("[4/5] Rải ~360 Phiến Đá Hắc Diện Vỡ & ~240 Cụm Quặng Thô 4 Loại (0% Thực Vật)...", flush=True)
    ore_objs = build_raw_ore_deposits(walk_bvh, rng)

    all_objs = [shell_obj, col_obj] + water_objs + crystal_objs + ore_objs

    if args.out:
        out_dir = os.path.abspath(args.out)
        print(f"Xuất FBX MazeChamber_Crystal vào {out_dir}...", flush=True)
        kit.export_fbx(all_objs, os.path.join(out_dir, "MazeChamber_Crystal.fbx"))
        export_standalone_mineral_assets(out_dir)

    if args.previews:
        prev_dir = os.path.abspath(args.previews)
        os.makedirs(prev_dir, exist_ok=True)
        print("[5/5] Render 4 góc ảnh 3D preview V3 (Nền Lồi Lõm - Vực Thẳm - Quặng Thô)...", flush=True)
        render_previews(all_objs, prev_dir)

    print(
        f"Hoàn tất MazeChamber_Crystal V3 trong {time.time() - t0:.1f}s | "
        f"Shell={kit.tri_count(shell_obj)} tris, "
        f"Water={sum(kit.tri_count(o) for o in water_objs)} tris, "
        f"Crystal={sum(kit.tri_count(o) for o in crystal_objs)} tris, "
        f"RawOres={sum(kit.tri_count(o) for o in ore_objs)} tris",
        flush=True,
    )


if __name__ == "__main__":
    main()
