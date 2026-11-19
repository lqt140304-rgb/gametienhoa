"""Dựng Khoang Mẫu Bước 2 (V6 - Mở Rộng Map 230m x 175m, Nguồn Nước Thác-Suối-Hồ, Vòm Trần Chi Tiết & Thảm Cỏ Dày):
Khoang: Rừng Nấm Phát Sáng (LumaMushroomGrove) trong Blender headless.

Nâng cấp V6 theo góp ý người dùng:
- Mở rộng quy mô khoang ~230m x 175m (trần hang 38m–48m), tăng chiều sâu cảnh quan và phân tầng địa hình.
- Thêm Nguồn Nước hoàn chỉnh (MazeChamber_Luma_Water): Thác nước 2 tầng đổ từ vách Bắc -> Hồ Thượng (+7.35m)
  -> tràn qua ghềnh đá xuống Hồ Hạ & Dòng Suối Ngọc Lam (-0.85m) uốn lượn xuyên qua dưới gầm Cầu Đá Vòm Tự Nhiên,
  kèm bọt thác & bèo bào tử nổi ven bờ.
- Làm giàu chi tiết Vòm Trần: 4 vòm gân đá nổi, 6 hệ rễ cổ thụ đan vắt qua trần, 26 cụm nhũ đá gắn bào tử sáng,
  và 42 dây leo phát sáng treo lồng đèn rủ xuống từ trần.
- Giảm khoảng đất trống, phủ rậm rạp với ~420 bụi cỏ lá cong nhiều lớp sắc độ + ~220 cụm thực vật nhỏ biến dị
  (Cỏ Sừng Rẽ Đôi, Dương Xỉ Xoắn Kép, Mầm Nấm Chồng Tầng), cộng thêm 5 Nấm Đại Thụ & 16 Cây Vòi Rồng phân tầng.
- Toàn bộ đặt chính xác bằng BVHTree raycast xuống mặt sàn/cầu/thềm/vách, cắm gốc âm xuống nền (0% lơ lửng).

Chạy preview:
  D:/blender/blender.exe -b -P tools/blender/build_maze_chamber_luma.py -- --previews D:/gametienhoa/docs/art/previews
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


# ---------------------------------------------------------------- Bảng màu đa sắc giàu sức sống (Linear RGB)

STONE_DEEP = (0.024, 0.028, 0.036)
STONE_DARK = (0.062, 0.068, 0.078)
STONE_MID = (0.145, 0.145, 0.140)
STONE_WARM = (0.245, 0.185, 0.125)
OCHRE_VEIN = (0.38, 0.22, 0.08)
TEAL_SLATE = (0.06, 0.165, 0.175)

MOSS_DEEP = (0.035, 0.165, 0.052)
MOSS_EMERALD = (0.075, 0.295, 0.095)
MOSS_LUSH = (0.14, 0.36, 0.085)
MOSS_GOLD = (0.26, 0.34, 0.065)
SOIL_TRAIL = (0.22, 0.17, 0.115)
SOIL_PEBBLE = (0.30, 0.23, 0.145)
SOIL_SPORE = (0.17, 0.075, 0.185)

BARK_DARK = (0.048, 0.035, 0.028)
BARK_PLUM = (0.16, 0.070, 0.13)
STALK_PALE = (0.42, 0.46, 0.38)

GLOW_TEAL = (0.035, 0.80, 0.64)
GLOW_LIME = (0.18, 0.86, 0.32)
GLOW_AMBER = (0.95, 0.38, 0.04)
GLOW_GOLD = (0.98, 0.64, 0.09)
GLOW_MAGENTA = (0.84, 0.12, 0.76)
GLOW_VIOLET = (0.46, 0.16, 0.94)

WATER_DEEP = (0.035, 0.38, 0.46)
WATER_SHALLOW = (0.10, 0.72, 0.76)
WATER_FOAM = (0.68, 0.96, 0.98)

LOWER_WATER_Z = -0.85
UPPER_WATER_Z = 7.35


# ---------------------------------------------------------------- Toán học & Tiện ích hình học

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


def _ridged(p):
    return 1.0 - abs(noise.noise(p))


def _stepped(val, steps=5.0, sharp=0.72):
    s = val * steps
    fl = math.floor(s)
    frac = s - fl
    crisp = _smoothstep(0.5 - sharp * 0.45, 0.5 + sharp * 0.45, frac)
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


def _parallel_tube_rings(bm, path, radii, segs, wobble=None):
    """Tạo vòng đỉnh dọc đường cong bằng Parallel Transport Frame (không bao giờ bị vặn xoắn)."""
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

        ring = []
        for k in range(segs):
            ang = 2.0 * math.pi * k / segs
            w = wobble(p, ang, i) if wobble else 0.0
            r = max(0.005, radii[i] * (1.0 + w))
            ring.append(bm.verts.new(p + (norm * math.cos(ang) + binorm * math.sin(ang)) * r))
        rings.append(ring)
    return rings


# ---------------------------------------------------------------- Vật liệu Preview

def _make_rock_material(name):
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
    n_coarse.inputs["Detail"].default_value = 8.0
    n_coarse.inputs["Roughness"].default_value = 0.72
    links.new(texcoord.outputs["Object"], n_coarse.inputs["Vector"])

    n_fine = nodes.new("ShaderNodeTexNoise")
    n_fine.inputs["Scale"].default_value = 2.4
    n_fine.inputs["Detail"].default_value = 6.0
    n_fine.inputs["Roughness"].default_value = 0.68
    links.new(texcoord.outputs["Object"], n_fine.inputs["Vector"])

    add_n = nodes.new("ShaderNodeMath")
    add_n.operation = "ADD"
    links.new(n_coarse.outputs["Fac"], add_n.inputs[0])
    links.new(n_fine.outputs["Fac"], add_n.inputs[1])

    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.65
    bump.inputs["Distance"].default_value = 0.25
    links.new(add_n.outputs["Value"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    ramp = nodes.new("ShaderNodeMapRange")
    ramp.inputs["From Min"].default_value = 0.25
    ramp.inputs["From Max"].default_value = 0.85
    ramp.inputs["To Min"].default_value = 0.72
    ramp.inputs["To Max"].default_value = 0.96
    links.new(n_coarse.outputs["Fac"], ramp.inputs["Value"])
    links.new(ramp.outputs["Result"], bsdf.inputs["Roughness"])

    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def _make_glow_material(name, energy=0.95):
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
    bsdf.inputs["Roughness"].default_value = 0.36
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def _make_water_material(name):
    """Vật liệu mặt nước sinh thái phát quang dịu, bóng gợn sóng phản chiếu ánh sáng vòm hang."""
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
        bsdf.inputs["Emission Strength"].default_value = 0.45

    bsdf.inputs["Roughness"].default_value = 0.22
    if "IOR" in bsdf.inputs:
        bsdf.inputs["IOR"].default_value = 1.333

    texcoord = nodes.new("ShaderNodeTexCoord")
    n_wave = nodes.new("ShaderNodeTexNoise")
    n_wave.inputs["Scale"].default_value = 2.2
    n_wave.inputs["Detail"].default_value = 6.0
    n_wave.inputs["Roughness"].default_value = 0.50
    links.new(texcoord.outputs["Object"], n_wave.inputs["Vector"])

    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.26
    bump.inputs["Distance"].default_value = 0.08
    links.new(n_wave.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


# ---------------------------------------------------------------- Toán học Địa hình Mở Rộng (~230m x 175m)

TUNNEL_ANGLES = [math.radians(20.0), math.radians(156.0), math.radians(264.0)]


def _stream_center_y(x):
    return 14.0 * math.sin(x * 0.036) - 6.0


def _lower_pool_dist(x, y):
    """Lòng hồ hạ lưu ở chân thác nước tại (-12, 8) nối liền vào dòng suối chảy qua dưới gầm cầu."""
    return math.sqrt(((x + 12.0) / 18.0) ** 2 + ((y - 6.0) / 14.0) ** 2)


def _upper_pool_dist(x, y):
    """Hồ thượng trên Thềm Ban Công Cao ở (-14, 44)."""
    return math.sqrt(((x + 14.0) / 15.0) ** 2 + ((y - 45.0) / 10.5) ** 2)


def _boundary_u(x, y):
    """Mở rộng map lên ~230m x 175m (rx=112m, ry=86m) với đường biên gãy khúc bất đối xứng."""
    ang = math.atan2(y, x)
    warp = (
        0.085 * math.sin(ang * 3.0 + 0.55)
        + 0.055 * math.cos(ang * 5.0 - 1.1)
        + 0.035 * noise.noise(Vector((math.cos(ang) * 2.2, math.sin(ang) * 2.2, 1.3)))
    )
    return math.sqrt((x / 112.0) ** 2 + (y / 86.0) ** 2) + warp


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


def _bridge_curve_point(t):
    """Cầu Đá Vòm mở rộng uốn cong bắc qua suối ngọc lam từ đồi Đông Nam (36, -24) sang Thềm Bắc Tây (-18, 32)."""
    p0 = Vector((36.0, -24.0, 1.2))
    p1 = Vector((-18.0, 32.0, 8.0))
    base = p0.lerp(p1, t)
    span_dir = (p1 - p0).normalized()
    perp = Vector((-span_dir.y, span_dir.x, 0.0))
    sway = math.sin(t * math.pi) * 8.5 - math.sin(t * math.pi * 2.0) * 2.8
    arch = math.sin(t * math.pi) * 6.8
    return base + perp * sway + Vector((0.0, 0.0, arch))


def terrain_height(x, y):
    p_noise = Vector((x * 0.030 + 4.1, y * 0.030 - 2.3, 0.0))
    hills = (
        1.85 * noise.noise(p_noise)
        + 0.95 * noise.noise(Vector((x * 0.072, y * 0.072, 3.7)))
        + 0.35 * (_ridged(Vector((x * 0.13, y * 0.13, 7.2))) - 0.5)
    )

    # 1. Dòng suối uốn lượn & Hồ Hạ (lòng sâu xuống -2.35m để chứa mặt nước tại -0.85m!)
    sy = _stream_center_y(x)
    d_stream = abs(y - sy)
    stream_cut = -2.85 * (1.0 - _smoothstep(3.8, 12.5, d_stream))
    pool_t = 1.0 - _smoothstep(0.55, 1.15, _lower_pool_dist(x, y))
    pool_cut = -3.15 * pool_t
    base_y = 0.85 + hills * 0.70 + min(stream_cut, pool_cut)

    # 2. Mô đất gối chân cầu phía Đông Nam (36, -24)
    d_abut = math.sqrt((x - 36.0) ** 2 + (y + 24.0) ** 2)
    abut_t = 1.0 - _smoothstep(2.5, 17.5, d_abut)
    if abut_t > 0.01:
        base_y = max(base_y, 2.8 * abut_t + 0.35 * hills)

    # 3. Thềm Ban Công Cao Tây Bắc (+8.0m, đủ rộng cho cả Hồ Thượng!)
    tx = (x + 15.0) / 50.0
    ty = (y - 44.0) / 25.0
    terrace_dist = math.sqrt(tx * tx + ty * ty) + 0.13 * noise.noise(Vector((x * 0.05, y * 0.05, 9.1)))
    terrace_t = 1.0 - _smoothstep(0.74, 1.03, terrace_dist)
    terrace_h = 8.15 + 0.45 * noise.noise(Vector((x * 0.08, y * 0.08, 2.4)))
    if terrace_t > 0.01:
        terrace_stepped = _stepped(terrace_t, steps=3.0, sharp=0.68)
        target_t_h = (1.0 - terrace_stepped) * base_y + terrace_stepped * terrace_h
        # Khoét lòng Hồ Thượng trên thềm cao (đáy +5.8m, mặt nước +7.35m) & khe tràn thác ở (x=-13, y=26)
        up_pool = 1.0 - _smoothstep(0.55, 1.15, _upper_pool_dist(x, y))
        spillway = math.exp(-((x + 13.0) / 5.5) ** 2) * (1.0 - _smoothstep(22.0, 38.0, abs(y - 28.0)))
        if up_pool > 0.01:
            target_t_h -= 2.45 * up_pool
        if spillway > 0.05 and y < 34.0 and y > 20.0:
            target_t_h -= 1.5 * spillway
        base_y = max(base_y, target_t_h)

    # 4. Dốc đá xoắn thoải sườn Đông (< 25 độ) nối từ bờ suối lên Thềm Cao
    ramp_ang = math.atan2(y - 4.0, x - 34.0)
    ramp_r = math.sqrt((x - 34.0) ** 2 + (y - 4.0) ** 2)
    if 16.0 < ramp_r < 48.0 and -1.85 < ramp_ang < 2.25:
        u_ramp = _clamp01((ramp_ang + 1.65) / 3.70)
        radial_w = 1.0 - _smoothstep(6.5, 16.0, abs(ramp_r - (32.0 + 4.0 * math.sin(u_ramp * 3.0))))
        if radial_w > 0.01:
            target_h = 0.5 + u_ramp * 7.8 + 0.35 * noise.noise(Vector((x * 0.1, y * 0.1, 4.4)))
            base_y = max(base_y, (1.0 - radial_w) * base_y + radial_w * target_h)

    # 5. Đồi cỏ Tây-Nam (+4.2m) & Đồi Nấm Đông-Bắc (+5.0m) cho cảnh quan mở rộng thêm tầng
    for kx, ky, krx, kry, kh in ((-62.0, -28.0, 26.0, 20.0, 4.2), (62.0, 34.0, 24.0, 20.0, 4.8)):
        kn_t = 1.0 - _smoothstep(0.60, 1.05, math.sqrt(((x - kx) / krx) ** 2 + ((y - ky) / kry) ** 2))
        if kn_t > 0.01:
            base_y = max(base_y, (1.0 - kn_t) * base_y + kn_t * (kh + 0.4 * hills))

    # 6. Vách đá bao quanh phân tầng bậc cao 38m–46m
    u = _boundary_u(x, y)
    clear_f, in_alcove = _alcove_depth_factor(x, y)
    if u > 0.74:
        wt = _smoothstep(0.74, 1.02, u)
        strata = _stepped(wt, steps=6.0, sharp=0.80)
        crag = _ridged(Vector((x * 0.06, y * 0.06, 12.0))) * 5.2 * wt
        wall_h = (39.5 * strata + crag) * clear_f
        base_y = max(base_y, wall_h)

    if in_alcove and u < 0.91:
        base_y = min(base_y, 1.1 + 0.2 * hills)

    return base_y


def ceiling_height(x, y):
    u = _clamp01(_boundary_u(x, y) / 1.04)
    arch = 47.0 - 11.5 * (u ** 1.9)
    rough = 2.8 * noise.noise(Vector((x * 0.045, y * 0.045, 18.0))) + 1.6 * (_ridged(Vector((x * 0.095, y * 0.095, 21.0))) - 0.5)
    return arch + rough


def _in_ceiling_slit(x, y):
    d1 = math.sqrt(((x + 12.0) / 16.0) ** 2 + ((y - 6.0) / 6.2) ** 2)
    d2 = math.sqrt(((x - 34.0) / 12.0) ** 2 + ((y - 22.0) / 5.5) ** 2)
    w1 = d1 + 0.20 * noise.noise(Vector((x * 0.16, y * 0.16, 5.0)))
    w2 = d2 + 0.20 * noise.noise(Vector((x * 0.16, y * 0.16, 9.0)))
    return w1 < 0.80 or w2 < 0.80


# ---------------------------------------------------------------- Dựng Địa hình, Cầu Đá, Gân Vòm Trần & Nhũ Đá

def _add_faceted_boulder(bm, pos, scale_v, seed_v, rot_euler):
    res = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0)
    rot = Euler(rot_euler, "XYZ").to_matrix().to_4x4()
    trans = Matrix.Translation(pos) @ rot
    for v in res["verts"]:
        p = v.co.copy()
        n = noise.noise(p * 1.6 + seed_v)
        cr = (_ridged(p * 2.5 + seed_v) - 0.45) * 0.42
        p.x *= scale_v.x * (1.0 + 0.28 * n + cr)
        p.y *= scale_v.y * (1.0 + 0.26 * n)
        p.z *= scale_v.z * (1.0 + 0.30 * n + cr)
        v.co = trans @ p


def _add_stone_arch_bridge(bm):
    steps_u = 46
    steps_w = 12
    pts = [_bridge_curve_point(i / (steps_u - 1)) for i in range(steps_u)]

    top_grid = []
    bot_grid = []

    for i, pos in enumerate(pts):
        t = i / (steps_u - 1)
        prev_p = pts[max(0, i - 1)]
        next_p = pts[min(steps_u - 1, i + 1)]
        fwd = Vector((next_p.x - prev_p.x, next_p.y - prev_p.y, 0.0)).normalized()
        right = Vector((-fwd.y, fwd.x, 0.0))

        flare_ends = 4.2 * ((abs(t - 0.5) * 2.0) ** 2.4)
        jagged = 0.95 * math.sin(t * 17.0) + 0.6 * noise.noise(pos * 0.20)
        half_w = 3.8 + flare_ends + jagged
        arch_thick = 1.45 + 2.4 * ((abs(t - 0.5) * 2.0) ** 3.0)

        row_top = []
        row_bot = []
        for j in range(steps_w):
            v = (j / (steps_w - 1)) * 2.0 - 1.0
            edge_asym = 1.0 + 0.22 * noise.noise(Vector((t * 6.0, v * 2.5, 3.1)))
            lat = v * half_w * edge_asym
            wp = Vector((pos.x + right.x * lat, pos.y + right.y * lat, pos.z))

            slab = 0.16 * noise.noise(wp * 0.25) + 0.08 * (_stepped(t * 5.0, steps=5.0) - t * 5.0)
            curb = (0.52 if abs(v) > 0.78 else 0.0) * (0.65 + 0.65 * _ridged(wp * 0.45))
            end_blend = min(_smoothstep(0.0, 0.14, t), 1.0 - _smoothstep(0.86, 1.0, t))
            g_h = terrain_height(wp.x, wp.y)
            tz = end_blend * (pos.z + slab + curb) + (1.0 - end_blend) * (g_h - 0.15)

            pillar_t = _smoothstep(0.06, 0.18, t) * (1.0 - _smoothstep(0.82, 0.94, t))
            arch_bz = pos.z - arch_thick + 0.45 * _ridged(wp * 0.35)
            bz = min(tz - 0.70, pillar_t * arch_bz + (1.0 - pillar_t) * (g_h - 1.8))

            row_top.append(bm.verts.new((wp.x, wp.y, tz)))
            bot_lat = lat * (0.86 if (0.15 < t < 0.85) else 1.12)
            row_bot.append(bm.verts.new((pos.x + right.x * bot_lat, pos.y + right.y * bot_lat, bz)))

        top_grid.append(row_top)
        bot_grid.append(row_bot)

    for i in range(steps_u - 1):
        for j in range(steps_w - 1):
            bm.faces.new([top_grid[i][j], top_grid[i + 1][j], top_grid[i + 1][j + 1], top_grid[i][j + 1]])
            bm.faces.new([bot_grid[i][j], bot_grid[i][j + 1], bot_grid[i + 1][j + 1], bot_grid[i + 1][j]])
        bm.faces.new([bot_grid[i][0], bot_grid[i + 1][0], top_grid[i + 1][0], top_grid[i][0]])
        bm.faces.new([top_grid[i][-1], top_grid[i + 1][-1], bot_grid[i + 1][-1], bot_grid[i][-1]])

    for end_i, flip in ((0, True), (-1, False)):
        for j in range(steps_w - 1):
            quad = [bot_grid[end_i][j], top_grid[end_i][j], top_grid[end_i][j + 1], bot_grid[end_i][j + 1]]
            bm.faces.new(quad[::-1] if flip else quad)

    for bt in (0.06, 0.12, 0.88, 0.94):
        bp = _bridge_curve_point(bt)
        for s_side in (-1.0, 1.0):
            bx = bp.x + s_side * 5.2
            by = bp.y - s_side * 2.8
            _add_faceted_boulder(
                bm,
                Vector((bx, by, terrain_height(bx, by) - 0.3)),
                Vector((3.0, 2.5, 2.2)),
                Vector((bt * 11.0, s_side * 4.0, 2.5)),
                (0.15 * s_side, -0.1, bt * 3.0),
            )

    terrace_lip_rocks = [
        (-38.0, 23.0, (3.8, 2.8, 2.4)), (-24.0, 21.5, (3.4, 2.5, 2.6)),
        (-2.0, 21.0, (3.4, 2.5, 2.2)), (14.0, 24.0, (3.2, 2.5, 2.2)),
        (-52.0, 32.0, (4.0, 3.0, 2.8)), (26.0, 30.0, (3.5, 2.6, 2.4)),
    ]
    for li, (lx, ly, lsc) in enumerate(terrace_lip_rocks):
        lz = terrain_height(lx, ly) - lsc[2] * 0.45
        _add_faceted_boulder(
            bm,
            Vector((lx, ly, lz)),
            Vector(lsc),
            Vector((li * 3.1, 7.2, li * 1.4)),
            (0.12, -0.15, li * 0.9),
        )


def _add_rock_pillar(bm, cx, cy, base_z, top_z, r_base, r_mid, seed_v):
    rings = []
    segs = 14
    steps = 12
    for i in range(steps):
        t = i / (steps - 1)
        z = base_z + t * (top_z - base_z)
        lean_x = 2.4 * math.sin(t * math.pi) * (1.0 if seed_v.x > 0 else -1.0)
        lean_y = 1.6 * math.cos(t * math.pi * 0.7)
        r = r_base * ((1.0 - t) ** 1.4) + r_mid * (math.sin(t * math.pi) * 0.65 + 0.55) + (t ** 2) * r_base * 0.85
        ring = []
        for k in range(segs):
            ang = 2.0 * math.pi * k / segs
            ca, sa = math.cos(ang), math.sin(ang)
            crag = 0.72 + 0.45 * _ridged(Vector((ca * 1.8, sa * 1.8, z * 0.14)) + seed_v)
            strata = 0.14 * (_stepped(z * 0.10, steps=4.0) - z * 0.10)
            rr = r * (crag + strata)
            ring.append(bm.verts.new((cx + lean_x + ca * rr, cy + lean_y + sa * rr, z)))
        rings.append(ring)
    kit.bridge_rings(bm, rings)
    kit.cap_ring(bm, rings[0], flip=True)
    kit.cap_ring(bm, rings[-1])


def _add_ceiling_rock_ribs(bm, rng):
    """4 dải gân đá nổi vòm chéo qua trần hang để vòm trần giàu kiến trúc và khối đổ bóng."""
    rib_paths = [
        [(-85, -35), (-35, -10), (15, 22), (75, 45)],
        [(-78, 38), (-24, 24), (26, -8), (82, -32)],
        [(-40, -68), (-16, -24), (8, 18), (38, 66)],
        [(35, -62), (18, -14), (-12, 32), (-48, 64)],
    ]
    for ri, raw_xy in enumerate(rib_paths):
        pts = []
        for k in range(14):
            u = k / 13.0
            idx = u * (len(raw_xy) - 1)
            i0 = min(len(raw_xy) - 2, int(idx))
            ft = idx - i0
            x = raw_xy[i0][0] * (1 - ft) + raw_xy[i0 + 1][0] * ft + math.sin(u * 6.0 + ri) * 4.5
            y = raw_xy[i0][1] * (1 - ft) + raw_xy[i0 + 1][1] * ft + math.cos(u * 5.0 + ri) * 4.0
            if _in_ceiling_slit(x, y):
                x += 8.0
            cz = ceiling_height(x, y) - (1.8 + 1.4 * math.sin(u * math.pi))
            pts.append(Vector((x, y, cz)))
        radii = [2.8 + 1.4 * _ridged(p * 0.08) + (1.2 if k in (0, len(pts) - 1) else 0.0) for k, p in enumerate(pts)]
        rgs = _parallel_tube_rings(bm, pts, radii, 9, lambda p, a, idx: 0.22 * _ridged(p * 0.12 + Vector((a, idx, 0))))
        kit.bridge_rings(bm, rgs)
        kit.cap_ring(bm, rgs[0], flip=True)
        kit.cap_ring(bm, rgs[-1])


def _add_stalactite_cluster(bm, center, seed_v, rng):
    _add_faceted_boulder(
        bm,
        center + Vector((0, 0, 0.4)),
        Vector((rng.uniform(2.6, 4.4), rng.uniform(2.2, 3.8), rng.uniform(1.8, 3.2))),
        seed_v,
        (rng.uniform(-0.2, 0.2), rng.uniform(-0.2, 0.2), rng.uniform(0, 3.14)),
    )
    count = rng.randint(3, 5)
    placed_tips = []
    for c_i in range(count):
        off = Vector((rng.uniform(-2.0, 2.0), rng.uniform(-2.0, 2.0), -0.4))
        p0 = center + off
        length = rng.uniform(3.2, 8.2) * (1.0 - c_i * 0.14)
        r0 = rng.uniform(0.65, 1.35) * (1.0 - c_i * 0.12)
        segs = 7
        steps = 6
        rings = []
        for i in range(steps):
            t = i / (steps - 1)
            flute = 1.0 + 0.18 * math.sin(t * 8.0 + c_i)
            r = max(0.08, r0 * ((1.0 - t) ** 1.15) * flute)
            ring = []
            for k in range(segs):
                ang = 2.0 * math.pi * k / segs
                wob = 0.78 + 0.40 * _ridged(Vector((math.cos(ang) * 1.5, math.sin(ang) * 1.5, t * 3.5)) + seed_v + Vector((c_i, 0, 0)))
                ring.append(bm.verts.new((
                    p0.x + math.cos(ang) * r * wob + 0.24 * math.sin(t * 2.5) * (1 if c_i % 2 else -1),
                    p0.y + math.sin(ang) * r * wob,
                    p0.z - t * length,
                )))
            rings.append(ring)
        kit.bridge_rings(bm, rings)
        kit.cap_ring(bm, rings[-1], flip=True)
        placed_tips.append(p0 - Vector((0, 0, length * 0.65)))
    return placed_tips


def build_chamber_shell(rng):
    nx, ny = 148, 114
    x_min, x_max = -118.0, 118.0
    y_min, y_max = -92.0, 92.0

    bm = bmesh.new()
    grid = [[None] * nx for _ in range(ny)]

    for j in range(ny):
        y = y_min + (j / (ny - 1)) * (y_max - y_min)
        for i in range(nx):
            x = x_min + (i / (nx - 1)) * (x_max - x_min)
            if _boundary_u(x, y) > 1.07:
                continue
            grid[j][i] = bm.verts.new((x, y, terrain_height(x, y)))

    for j in range(ny - 1):
        for i in range(nx - 1):
            a, b, c, d = grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i]
            if a and b and c and d:
                bm.faces.new([a, b, c, d])

    _add_stone_arch_bridge(bm)

    # 3 cột đá đại cảnh nối trần ở các khu vực lệch tâm của khoang 230m
    for cx, cy, rb, rm, sv in (
        (-54.0, -24.0, 5.6, 3.0, Vector((1.2, 3.4, 5.6))),
        (56.0, 32.0, 5.2, 2.8, Vector((-2.5, 1.8, 8.1))),
        (48.0, -48.0, 4.6, 2.4, Vector((3.1, -1.2, 2.9))),
    ):
        bz = terrain_height(cx, cy) - 1.5
        tz = ceiling_height(cx, cy) + 2.0
        _add_rock_pillar(bm, cx, cy, bz, tz, rb, rm, sv)

    rock_spots = [
        (28.0, -28.0, (3.2, 2.6, 2.0)), (44.0, -18.0, (3.4, 2.8, 2.2)),
        (-24.0, 16.0, (3.8, 2.8, 2.4)), (-40.0, 14.0, (3.2, 2.6, 2.0)),
        (10.0, 22.0, (3.4, 2.5, 2.1)), (-32.0, -18.0, (2.6, 2.0, 1.5)),
        (14.0, -20.0, (2.5, 2.0, 1.4)), (-10.0, -8.0, (2.0, 1.6, 1.0)),
        (52.0, -6.0, (3.8, 3.0, 2.8)), (-56.0, 8.0, (4.0, 3.2, 3.0)),
        (-16.0, 28.0, (3.2, 2.4, 2.2)), (-8.0, 29.0, (2.8, 2.2, 2.0)),  # đá gờ thác hạ
    ]
    for ri, (rx, ry, rscale) in enumerate(rock_spots):
        rz = terrain_height(rx, ry) - rscale[2] * 0.38
        _add_faceted_boulder(
            bm,
            Vector((rx, ry, rz)),
            Vector(rscale),
            Vector((ri * 2.1, ri * 3.7, 1.5)),
            (rng.uniform(-0.2, 0.2), rng.uniform(-0.2, 0.2), rng.uniform(0.0, 3.14)),
        )

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    walk_bvh = BVHTree.FromBMesh(bm)

    bm_col = bm.copy()
    col_obj = kit.finish_object(bm_col, "MazeChamber_Luma_COL")

    # Vòm Trần kín + gân đá vòm trần + 26 cụm nhũ đá phân tầng
    cnx, cny = 86, 68
    c_grid = [[None] * cnx for _ in range(cny)]
    for j in range(cny):
        y = y_min + (j / (cny - 1)) * (y_max - y_min)
        for i in range(cnx):
            x = x_min + (i / (cnx - 1)) * (x_max - x_min)
            u = _boundary_u(x, y)
            if u > 1.07 or _in_ceiling_slit(x, y):
                continue
            c_grid[j][i] = bm.verts.new((x, y, ceiling_height(x, y)))

    for j in range(cny - 1):
        for i in range(cnx - 1):
            a, b, c, d = c_grid[j][i], c_grid[j][i + 1], c_grid[j + 1][i + 1], c_grid[j + 1][i]
            if a and b and c and d:
                bm.faces.new([a, d, c, b])

    _add_ceiling_rock_ribs(bm, rng)

    stalactite_bud_spots = []
    for s_i in range(26):
        sx = rng.uniform(-82.0, 82.0)
        sy = rng.uniform(-62.0, 62.0)
        if _boundary_u(sx, sy) > 0.85 or _in_ceiling_slit(sx, sy):
            continue
        cz = ceiling_height(sx, sy) + 0.5
        tips = _add_stalactite_cluster(bm, Vector((sx, sy, cz)), Vector((s_i * 2.3, 4.1, 7.2)), rng)
        stalactite_bud_spots.extend(tips)

    sharp_edges = [e for e in bm.edges if len(e.link_faces) == 2 and e.calc_face_angle(0.0) > math.radians(24.0)]
    if sharp_edges:
        bmesh.ops.split_edges(bm, edges=sharp_edges)

    shell_mat = _make_rock_material("MazeChamber_Luma_RockMat")
    shell_obj = kit.finish_object(bm, "MazeChamber_Luma_Shell", shell_mat)
    for p in shell_obj.data.polygons:
        p.use_smooth = True

    def paint_shell(p, n):
        n_c = 0.5 + 0.5 * noise.noise(p * 0.06 + Vector((1.7, 3.1, 2.2)))
        n_f = 0.5 + 0.5 * noise.noise(p * 0.28 + Vector((8.2, 4.4, 1.5)))
        crev = _clamp01((0.68 - _ridged(p * 0.11 + Vector((4.1, 9.2, 0.0)))) * 2.5)
        u_dist = _boundary_u(p.x, p.y)
        _, in_alcove = _alcove_depth_factor(p.x, p.y)

        strata_band = math.sin(p.z * 0.48 + noise.noise(p * 0.10) * 2.4)
        stone = _mix(STONE_DARK, STONE_MID, n_c)
        stone = _mix(stone, TEAL_SLATE, _smoothstep(0.55, 0.88, noise.noise(p * 0.07 + Vector((21, 5, 3))) + 0.35) * 0.55)
        stone = _mix(stone, STONE_WARM, _smoothstep(0.15, 0.80, strata_band))
        stone = _mix(stone, OCHRE_VEIN, _smoothstep(0.62, 0.94, strata_band) * n_f)
        stone = _mix(stone, STONE_DEEP, crev * 0.78)

        if in_alcove and u_dist > 0.78:
            return _mix(stone, STONE_DEEP, _smoothstep(0.78, 0.92, u_dist) * 0.88)

        # Kiểm tra mặt cầu đá
        on_bridge = False
        for bt_i in range(36):
            bp = _bridge_curve_point(bt_i / 35.0)
            if abs(p.x - bp.x) < 7.2 and abs(p.y - bp.y) < 7.2 and abs(p.z - bp.z) < 2.8:
                on_bridge = True
                break

        if on_bridge:
            if n.z > 0.68:
                edge_moss = _smoothstep(0.45, 0.88, n_f) * 0.55
                deck = _mix(SOIL_TRAIL, STONE_WARM, n_c * 0.6)
                return _mix(deck, MOSS_LUSH, edge_moss)
            return stone

        # Mặt đất và triền thung lũng: phủ rêu lục đậm-tươi nhiều sắc độ thay vì đất trần, chỉ chừa sỏi ven lòng suối/hồ
        if n.z > 0.72 and u_dist < 0.88:
            d_stream = abs(p.y - _stream_center_y(p.x))
            d_pool = _lower_pool_dist(p.x, p.y)
            d_upool = _upper_pool_dist(p.x, p.y)

            moss_mix = _mix(MOSS_DEEP, MOSS_EMERALD, n_c)
            moss_mix = _mix(moss_mix, MOSS_LUSH, _smoothstep(0.35, 0.78, n_f) * 0.78)
            moss_mix = _mix(moss_mix, MOSS_GOLD, _smoothstep(0.62, 0.92, n_f) * 0.55)

            in_wet_bed = (p.z < -0.55 and (d_stream < 8.5 or d_pool < 1.15)) or (p.z > 5.0 and p.z < 7.55 and d_upool < 1.1)
            if in_wet_bed:
                pebble = _mix(SOIL_TRAIL, SOIL_PEBBLE, _smoothstep(0.3, 0.85, n_f))
                ground_c = _mix(moss_mix, pebble, 0.85)
            else:
                spore_zone = _smoothstep(0.42, 0.82, noise.noise(p * 0.045 + Vector((14.0, -7.0, 2.0))) + 0.25)
                ground_c = _mix(moss_mix, SOIL_SPORE, spore_zone * 0.55)
            slope_t = _smoothstep(0.72, 0.89, n.z)
            return _mix(stone, ground_c, slope_t * 0.96)

        # Vòm trần hang cũng điểm thêm rêu ẩm màu tối gần các gờ ướt
        if n.z < -0.55:
            moss_roof = _smoothstep(0.45, 0.85, n_c) * 0.42
            return _mix(stone, MOSS_DEEP, moss_roof)

        return stone

    kit.paint(shell_obj, paint_shell)
    return shell_obj, col_obj, walk_bvh, stalactite_bud_spots


# ---------------------------------------------------------------- Nguồn Nước (Hồ Thượng, Thác 2 Tầng, Suối Ngọc Lam & Bèo Sáng)

def _build_water_ribbon_sheet(bm, x0, x1, y0, y1, nx, ny, z_fn, mask_fn, foam_hints):
    grid = [[None] * nx for _ in range(ny)]
    for j in range(ny):
        y = y0 + (j / (ny - 1)) * (y1 - y0)
        for i in range(nx):
            x = x0 + (i / (nx - 1)) * (x1 - x0)
            if mask_fn(x, y):
                z = z_fn(x, y)
                grid[j][i] = bm.verts.new((x, y, z))
    for j in range(ny - 1):
        for i in range(nx - 1):
            a, b, c, d = grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i]
            if a and b and c and d:
                bm.faces.new([a, b, c, d])


def _add_waterfall_curtain(bm, top_p, bot_p, half_w_top, half_w_bot, curve_fwd, foam_hints):
    """Dải thác nước đổ cong từ vách cao xuống hồ, gợn sóng dọc theo dòng chảy và tạo vòng bọt trắng ở chân thác."""
    rows = 16
    cols = 10
    span = bot_p - top_p
    horiz = Vector((span.x, span.y, 0.0))
    fwd = horiz.normalized() if horiz.length > 0.2 else Vector((0.0, -1.0, 0.0))
    right = Vector((-fwd.y, fwd.x, 0.0))

    grid = []
    for i in range(rows):
        t = i / (rows - 1)
        # Parabol rơi trọng lực tự nhiên
        xy_t = t ** 0.72
        z_t = t ** 1.45
        p = Vector((
            top_p.x + span.x * xy_t + fwd.x * math.sin(t * math.pi) * curve_fwd,
            top_p.y + span.y * xy_t + fwd.y * math.sin(t * math.pi) * curve_fwd,
            top_p.z + span.z * z_t,
        ))
        hw = half_w_top * (1.0 - t) + half_w_bot * t
        row = []
        for j in range(cols):
            v = (j / (cols - 1)) * 2.0 - 1.0
            rib = 0.24 * math.sin(v * 7.0 + t * 5.0)
            wp = p + right * (v * hw) + fwd * rib
            row.append(bm.verts.new(wp))
        grid.append(row)

    for i in range(rows - 1):
        for j in range(cols - 1):
            bm.faces.new([grid[i][j], grid[i + 1][j], grid[i + 1][j + 1], grid[i][j + 1]])

    # Vòng bọt thác nổi nhẹ trên mặt hồ chân thác
    for ri in range(18):
        ang = ri * (2.0 * math.pi / 18.0)
        rx = bot_p.x + math.cos(ang) * (half_w_bot * 1.15)
        ry = bot_p.y + math.sin(ang) * (half_w_bot * 0.85)
        foam_hints.append((Vector((rx, ry, bot_p.z)), 3.8))


def build_water_system(walk_bvh, rng):
    """Dựng nguồn nước hoàn chỉnh: Dòng Suối + Hồ Hạ (-0.85m), Hồ Thượng (+7.35m), Thác Nước 2 tầng & Bèo Bào Tử."""
    bm_water = bmesh.new()
    foam_hints = []

    # 1. Mặt nước Hồ Hạ & Dòng Suối Uốn Chữ S (-0.85m): nới rộng mép +1.3m vào trong bờ đất để bờ hồ trơn tru không bị răng cưa!
    _build_water_ribbon_sheet(
        bm_water,
        -100.0, 100.0, -56.0, 42.0,
        nx=118, ny=78,
        z_fn=lambda x, y: LOWER_WATER_Z + 0.03 * math.sin(x * 0.6 + y * 0.5),
        mask_fn=lambda x, y: (
            _boundary_u(x, y) < 0.88
            and terrain_height(x, y) < LOWER_WATER_Z + 1.35
            and (abs(y - _stream_center_y(x)) < 13.5 or _lower_pool_dist(x, y) < 1.28)
        ),
        foam_hints=foam_hints,
    )

    # 2. Mặt nước Hồ Thượng trên Thềm Ban Công Cao (+7.35m) - giới hạn gọn trong lòng bồn y > 30 để không tràn vệt rời bên mép dốc
    _build_water_ribbon_sheet(
        bm_water,
        -32.0, 4.0, 31.0, 58.0,
        nx=42, ny=34,
        z_fn=lambda x, y: UPPER_WATER_Z + 0.02 * math.sin(x * 0.8 + y * 0.7),
        mask_fn=lambda x, y: _upper_pool_dist(x, y) < 1.05 and y > 31.5 and terrain_height(x, y) < UPPER_WATER_Z + 0.65,
        foam_hints=foam_hints,
    )

    # 3. Thác Nước Tầng Trên (vách hang Bắc (-15, 68, z=25m) -> Hồ Thượng (-14, 52, z=7.35m))
    _add_waterfall_curtain(bm_water, Vector((-15.0, 68.0, 25.0)), Vector((-14.0, 52.0, UPPER_WATER_Z - 0.1)), 2.5, 4.0, 2.2, foam_hints)

    # 4. Thác Nước Tầng Dưới (mép tràn Thềm Cao (-13, 28, z=7.35m) -> Hồ Hạ (-12, 12, z=-0.85m))
    _add_waterfall_curtain(bm_water, Vector((-13.0, 27.5, UPPER_WATER_Z - 0.1)), Vector((-12.0, 13.0, LOWER_WATER_Z - 0.1)), 2.8, 4.4, 2.5, foam_hints)

    bmesh.ops.recalc_face_normals(bm_water, faces=bm_water.faces[:])
    water_obj = kit.finish_object(bm_water, "MazeChamber_Luma_Water", _make_water_material("MazeChamber_Luma_WaterMat"))

    def paint_water(p, n):
        # Dải dòng thác có vân sọc nước lam ngọc xen kẽ vệt bọt trắng tự nhiên thay vì trắng đục nguyên tấm
        is_fall = (p.z > LOWER_WATER_Z + 0.25 and p.z < UPPER_WATER_Z - 0.15) or (p.z > UPPER_WATER_Z + 0.25)
        if is_fall:
            streak = _ridged(Vector((p.x * 1.35, p.y * 1.35, p.z * 0.22)))
            splash = _smoothstep(LOWER_WATER_Z + 2.5, LOWER_WATER_Z + 0.2, p.z) if p.z < 5.0 else _smoothstep(UPPER_WATER_Z + 2.5, UPPER_WATER_Z + 0.2, p.z)
            foam_f = _clamp01((streak - 0.55) * 1.8 + splash * 0.65)
            return _mix((0.06, 0.56, 0.72), WATER_FOAM, foam_f)

        foam = 0.0
        for fp, fr in foam_hints:
            d = (p - fp).length
            if d < fr * 1.5:
                foam = max(foam, 1.0 - _smoothstep(0.0, fr * 1.5, d))

        h = terrain_height(p.x, p.y)
        w_lvl = UPPER_WATER_Z if p.z > 3.0 else LOWER_WATER_Z
        depth = _clamp01((w_lvl - h) / 1.55)
        col = _mix(WATER_SHALLOW, WATER_DEEP, depth)
        return _mix(col, WATER_FOAM, _clamp01(foam * 0.78))

    kit.paint(water_obj, paint_water)
    return [water_obj]


# ---------------------------------------------------------------- Raycast & Hỗ trợ

def ray_ground(walk_bvh, x, y, start_z=28.0):
    loc, norm, _, _ = walk_bvh.ray_cast(Vector((x, y, start_z)), Vector((0.0, 0.0, -1.0)), 65.0)
    return loc, norm


def ray_wall(walk_bvh, origin, direction, max_dist=90.0):
    loc, norm, _, _ = walk_bvh.ray_cast(origin, direction.normalized(), max_dist)
    return loc, norm


# ---------------------------------------------------------------- Hệ Thực Vật Lớn + Rễ Vòm Trần & Dây Leo Phát Sáng

def _add_mushroom_cap(bm_glow, bm_gill, apex, dir_n, radius, thickness, sag=0.22, tilt=0.12, seed=0.0, segs=22):
    dn = dir_n.normalized()
    ref = Vector((1.0, 0.0, 0.0)) if abs(dn.z) > 0.92 else Vector((0.0, 0.0, 1.0))
    u = dn.cross(ref).normalized()
    v = dn.cross(u).normalized()

    rings_n = 7 if segs >= 18 else 4
    top_rings = []
    bot_rings = []

    for ri in range(1, rings_n):
        t = ri / (rings_n - 1)
        r = radius * t
        ring_top = []
        ring_bot = []
        for k in range(segs):
            ang = 2.0 * math.pi * k / segs
            ca, sa = math.cos(ang), math.sin(ang)
            wob = 1.0 + 0.17 * math.sin(ang * 3.0 + seed) + 0.11 * math.cos(ang * 5.0 - seed * 1.3)
            rr = r * wob
            radial = u * (ca * rr) + v * (sa * rr)
            drop = sag * (t ** 1.70) * radius + tilt * ca * rr * 0.35 + 0.065 * radius * math.sin(ang * 4.0 + seed) * (t ** 2)
            p_top = apex + radial - dn * drop + dn * (thickness * (1.0 - t * 0.72))
            p_bot = apex + radial - dn * (drop + thickness * 0.20 * (1.0 - t))
            ring_top.append(bm_glow.verts.new(p_top))
            ring_bot.append(bm_glow.verts.new(p_bot))
        top_rings.append(ring_top)
        bot_rings.append(ring_bot)

    center_top = bm_glow.verts.new(apex + dn * (thickness * 1.05))
    center_bot = bm_glow.verts.new(apex - dn * (thickness * 0.12))

    for k in range(segs):
        k2 = (k + 1) % segs
        bm_glow.faces.new([center_top, top_rings[0][k], top_rings[0][k2]])
        bm_glow.faces.new([center_bot, bot_rings[0][k2], bot_rings[0][k]])

    kit.bridge_rings(bm_glow, top_rings, flip=False)
    kit.bridge_rings(bm_glow, bot_rings, flip=True)
    for k in range(segs):
        k2 = (k + 1) % segs
        bm_glow.faces.new([top_rings[-1][k], bot_rings[-1][k], bot_rings[-1][k2], top_rings[-1][k2]])

    if bm_gill is not None:
        gills = 14
        for gi in range(gills):
            ang = 2.0 * math.pi * gi / gills + seed * 0.2
            ca, sa = math.cos(ang), math.sin(ang)
            wob = 1.0 + 0.12 * math.sin(ang * 3.0 + seed)
            dir_r = (u * ca + v * sa).normalized()
            pts = []
            for s in range(5):
                ft = 0.15 + 0.75 * (s / 4.0)
                rr = radius * ft * wob
                drop = sag * (ft ** 1.70) * radius + tilt * ca * rr * 0.35
                pts.append(apex + dir_r * rr - dn * (drop + 0.08 * radius))
            radii = [0.025 * radius * math.sin(math.pi * (k / 4.0)) + 0.015 * radius for k in range(5)]
            rgs = _parallel_tube_rings(bm_gill, pts, radii, 5)
            kit.bridge_rings(bm_gill, rgs)


def _add_twisted_branch(bm_flora, pts, r_start, r_end, segs=10, flute=True, flare_root=True):
    smooth_pts = _smooth_path(pts, sub=3)
    n = len(smooth_pts)
    radii = []
    for i in range(n):
        t = i / max(1, n - 1)
        flare = (1.0 + 0.60 * math.exp(-t * 9.0)) if flare_root else 1.0
        radii.append(max(0.03, (r_start * (1.0 - t) + r_end * t) * flare))

    def wob(p, a, idx):
        if not flute:
            return 0.0
        tw = a + idx * 0.25
        return 0.13 * math.sin(tw * 3.0) + 0.07 * noise.noise(p * 1.4)

    rings = _parallel_tube_rings(bm_flora, smooth_pts, radii, segs, wob)
    kit.bridge_rings(bm_flora, rings)
    kit.cap_ring(bm_flora, rings[0], flip=True)
    kit.cap_ring(bm_flora, rings[-1])
    return smooth_pts[-1]


def build_giant_flora_and_ceiling_life(walk_bvh, stalactite_buds, rng):
    bm_flora = bmesh.new()
    bm_glow = bmesh.new()
    cap_hue_zones = []

    # ---- 1. 5 Nấm Đại Thụ Xoắn Kép / Rẽ Đôi / Chồng Tầng khắp khoang mở rộng 230m
    mother_specs = [
        (-28.0, -14.0, 1.25, "helix_twin", "teal", "amber"),     # Thung lũng Tây-Nam bờ suối
        (26.0, 12.0, 1.05, "trident_pagoda", "teal", "magenta"), # Sườn dốc Đông
        (-34.0, 42.0, 1.00, "split_tower", "amber", "teal"),     # Trên Thềm Ban Công Cao bên cạnh Hồ Thượng!
        (60.0, -24.0, 1.10, "helix_twin", "magenta", "amber"),   # Cánh Đông-Nam mở rộng
        (-62.0, 14.0, 1.00, "trident_pagoda", "amber", "magenta"),# Cánh Tây mở rộng
    ]

    for idx, (mx, my, sc, style, hue1, hue2) in enumerate(mother_specs):
        loc, _ = ray_ground(walk_bvh, mx, my)
        if loc is None or loc.z < LOWER_WATER_Z + 0.15:
            continue
        root_p = loc - Vector((0.0, 0.0, 0.80))

        if style == "helix_twin":
            pts_a = [
                root_p,
                root_p + Vector((0.9, -0.6, 4.5 * sc)),
                root_p + Vector((3.0, 2.0, 9.5 * sc)),
                root_p + Vector((5.2, 1.4, 15.0 * sc)),
                root_p + Vector((3.8, -0.5, 18.0 * sc)),
            ]
            pts_b = [
                root_p + Vector((-0.6, 0.4, 0.0)),
                root_p + Vector((-1.6, 0.9, 4.0 * sc)),
                root_p + Vector((-4.2, -1.6, 8.2 * sc)),
                root_p + Vector((-6.2, -2.8, 11.4 * sc)),
            ]
            tip_a = _add_twisted_branch(bm_flora, pts_a, 1.40 * sc, 0.60 * sc, segs=12)
            tip_b = _add_twisted_branch(bm_flora, pts_b, 1.15 * sc, 0.48 * sc, segs=11)

            _add_mushroom_cap(bm_glow, bm_glow, tip_a, Vector((-0.18, 0.08, 1.0)), 7.2 * sc, 1.35 * sc, sag=0.25, seed=1.1 + idx)
            cap_hue_zones.append((tip_a, 7.8 * sc, hue1))

            stack1 = tip_a + Vector((1.7 * sc, 1.0 * sc, 2.4 * sc))
            _add_twisted_branch(bm_flora, [tip_a - Vector((0, 0, 0.4)), tip_a + Vector((0.9 * sc, 0.5 * sc, 1.2 * sc)), stack1], 0.36 * sc, 0.22 * sc, segs=8, flare_root=False)
            _add_mushroom_cap(bm_glow, bm_glow, stack1, Vector((0.22, 0.14, 1.0)), 3.5 * sc, 0.78 * sc, sag=0.24, seed=2.4 + idx)
            cap_hue_zones.append((stack1, 3.9 * sc, hue2))

            stack2 = stack1 + Vector((-1.0 * sc, 0.8 * sc, 1.7 * sc))
            _add_twisted_branch(bm_flora, [stack1, stack2], 0.20 * sc, 0.12 * sc, segs=7, flare_root=False)
            _add_mushroom_cap(bm_glow, None, stack2, Vector((-0.12, 0.2, 1.0)), 1.9 * sc, 0.48 * sc, sag=0.22, seed=3.1 + idx)
            cap_hue_zones.append((stack2, 2.3 * sc, "magenta"))

            _add_mushroom_cap(bm_glow, bm_glow, tip_b, Vector((-0.26, -0.15, 1.0)), 5.2 * sc, 1.0 * sc, sag=0.26, seed=4.2 + idx)
            cap_hue_zones.append((tip_b, 5.8 * sc, hue2))
            stack_b = tip_b + Vector((-1.5 * sc, 0.5 * sc, 2.0 * sc))
            _add_twisted_branch(bm_flora, [tip_b, stack_b], 0.28 * sc, 0.15 * sc, segs=7, flare_root=False)
            _add_mushroom_cap(bm_glow, None, stack_b, Vector((-0.20, 0.15, 1.0)), 2.4 * sc, 0.55 * sc, seed=5.5 + idx)
            cap_hue_zones.append((stack_b, 2.8 * sc, hue1))

        else:
            trunk_pts = [
                root_p,
                root_p + Vector((0.6 * (-1 if idx % 2 else 1), 0.7, 4.0 * sc)),
                root_p + Vector((-1.1 * (-1 if idx % 2 else 1), -0.8, 8.2 * sc)),
            ]
            split_p = _add_twisted_branch(bm_flora, trunk_pts, 1.30 * sc, 0.60 * sc, segs=12)
            branches = [
                (Vector((3.4 * sc, 1.7 * sc, 5.8 * sc)), 5.6 * sc, hue1, 0.58 * sc),
                (Vector((-3.6 * sc, -1.9 * sc, 4.0 * sc)), 4.2 * sc, hue2, 0.45 * sc),
                (Vector((0.8 * sc, -3.6 * sc, 2.6 * sc)), 3.1 * sc, "amber" if hue1 != "amber" else "magenta", 0.35 * sc),
            ]
            for bi, (off_b, cap_r, bhue, b_r0) in enumerate(branches):
                b_start = split_p - Vector((0.0, 0.0, 0.65 * sc))
                b_pts = [b_start, split_p + off_b * 0.48 + Vector((0, 0, 0.9)), split_p + off_b]
                tip = _add_twisted_branch(bm_flora, b_pts, b_r0, 0.28 * sc, segs=9, flare_root=False)
                _add_mushroom_cap(bm_glow, bm_glow, tip, (off_b * 0.22 + Vector((0, 0, 1.0))).normalized(), cap_r, cap_r * 0.22, seed=bi * 2.1 + idx)
                cap_hue_zones.append((tip, cap_r * 1.12, bhue))
                if bi == 0:
                    top2 = tip + Vector((-1.1 * sc, 0.9 * sc, 2.3 * sc))
                    _add_twisted_branch(bm_flora, [tip, top2], 0.28 * sc, 0.15 * sc, segs=7, flare_root=False)
                    _add_mushroom_cap(bm_glow, None, top2, Vector((-0.18, 0.12, 1.0)), cap_r * 0.52, cap_r * 0.15, seed=7.3 + idx)
                    cap_hue_zones.append((top2, cap_r * 0.65, hue2))

        for ri in range(5):
            ra = ri * (2.0 * math.pi / 5.0) + idx * 0.7
            rx = loc.x + math.cos(ra) * 4.2 * sc
            ry = loc.y + math.sin(ra) * 4.2 * sc
            rloc, _ = ray_ground(walk_bvh, rx, ry)
            if rloc is not None and abs(rloc.z - loc.z) < 3.5:
                _add_twisted_branch(
                    bm_flora,
                    [loc + Vector((0, 0, 1.7 * sc)), loc + Vector((math.cos(ra) * 2.1 * sc, math.sin(ra) * 2.1 * sc, 0.9 * sc)), rloc - Vector((0, 0, 0.6))],
                    0.48 * sc, 0.16 * sc, segs=7, flute=False, flare_root=False,
                )

    # ---- 2. 16 Cây Vòi Rồng Cong Rẽ Đôi & Nấm Chồng Trung Bình khắp khoang mở rộng
    tree_spots = [
        (-44.0, -4.0, 1.15, "magenta"), (-6.0, -32.0, 1.20, "amber"), (12.0, 22.0, 1.05, "magenta"),
        (42.0, -8.0, 1.15, "amber"), (-48.0, 32.0, 0.95, "magenta"), (6.0, 48.0, 1.05, "teal"),
        (-40.0, -36.0, 1.20, "amber"), (52.0, 16.0, 0.95, "magenta"), (18.0, -42.0, 1.05, "teal"),
        (-66.0, -24.0, 1.10, "teal"), (68.0, 28.0, 1.00, "amber"), (74.0, -14.0, 1.15, "magenta"),
        (-2.0, -16.0, 0.95, "amber"), (-26.0, -34.0, 1.10, "magenta"), (28.0, 38.0, 1.00, "teal"),
        (-60.0, 38.0, 1.05, "amber"),
    ]
    for ti, (tx, ty, tsc, thue) in enumerate(tree_spots):
        loc, norm = ray_ground(walk_bvh, tx, ty)
        if loc is None or norm.z < 0.58 or loc.z < LOWER_WATER_Z + 0.15:
            continue
        base = loc - Vector((0.0, 0.0, 0.55))
        lean_ang = ti * 1.37
        lx, ly = math.cos(lean_ang), math.sin(lean_ang)
        fork_p = base + Vector((lx * 2.2 * tsc, ly * 2.2 * tsc, 3.4 * tsc))
        _add_twisted_branch(bm_flora, [base, base + Vector((lx * 0.8, ly * 0.8, 1.7 * tsc)), fork_p], 0.54 * tsc, 0.28 * tsc, segs=8)

        for side in (-1.0, 1.0):
            sa = lean_ang + side * 0.95
            bx, by = math.cos(sa), math.sin(sa)
            arm_mid = fork_p + Vector((bx * 2.0 * tsc, by * 2.0 * tsc, 1.8 * tsc))
            arm_tip = arm_mid + Vector((-by * 1.5 * side * tsc, bx * 1.5 * side * tsc, 1.4 * tsc))
            _add_twisted_branch(bm_flora, [fork_p - Vector((0, 0, 0.18 * tsc)), arm_mid, arm_tip], 0.24 * tsc, 0.08 * tsc, segs=7, flare_root=False)

            pod_r = rng.uniform(0.60, 1.05) * tsc
            if ti % 2 == 0:
                _add_mushroom_cap(bm_glow, None, arm_tip, Vector((bx * 0.25, by * 0.25, 1.0)), pod_r * 1.75, pod_r * 0.55, seed=ti + side, segs=14)
                cap_hue_zones.append((arm_tip, pod_r * 2.2, thue))
            else:
                hook_tip = arm_tip + Vector((bx * 0.45 * tsc, by * 0.45 * tsc, -0.75 * tsc))
                _add_twisted_branch(bm_flora, [arm_tip, arm_tip + Vector((bx * 0.25 * tsc, by * 0.25 * tsc, -0.2 * tsc)), hook_tip], 0.08 * tsc, 0.04 * tsc, segs=6, flute=False, flare_root=False)
                _add_mushroom_cap(bm_glow, None, hook_tip, Vector((0, 0, 1.0)), pod_r * 1.30, pod_r * 0.46, sag=0.35, seed=ti + 3.0, segs=12)
                cap_hue_zones.append((hook_tip, pod_r * 1.7, thue))

    # ---- 3. 6 Hệ Rễ Cổ Thụ Vắt Vòm Trần (Ceiling Root Arches)
    root_anchors = [
        ((-78, -22), (-10, 8), (58, 36)),
        ((-66, 36), (12, 12), (76, -24)),
        ((-32, -62), (-18, -14), (-8, 52)),
        ((46, -54), (24, 0), (8, 58)),
        ((-84, 4), (-38, -24), (-14, -56)),
        ((72, 22), (44, -18), (6, -58)),
    ]
    for r_i, (a_xy, mid_xy, b_xy) in enumerate(root_anchors):
        pts = []
        for s in range(11):
            t = s / 10.0
            mt = 1.0 - t
            x = mt * mt * a_xy[0] + 2 * mt * t * mid_xy[0] + t * t * b_xy[0] + math.sin(t * 7.0 + r_i) * 3.0
            y = mt * mt * a_xy[1] + 2 * mt * t * mid_xy[1] + t * t * b_xy[1] + math.cos(t * 6.0 + r_i) * 2.8
            cz = ceiling_height(x, y) - (0.6 + 6.5 * math.sin(t * math.pi))
            pts.append(Vector((x, y, cz)))
        _add_twisted_branch(bm_flora, pts, 1.35, 0.95, segs=8, flare_root=False)

    # ---- 4. 42 Dây Leo Rủ & Chùm Đèn Bào Tử Từ Vòm Trần (Hanging Ceiling Vines & Spore Lanterns)
    for v_i in range(42):
        vx = rng.uniform(-76.0, 76.0)
        vy = rng.uniform(-56.0, 56.0)
        if _boundary_u(vx, vy) > 0.85 or _in_ceiling_slit(vx, vy):
            continue
        # Tránh che sát ngay trước ống kính camera toàn cảnh (24, -56, 27)
        if math.hypot(vx - 24.0, vy + 56.0) < 14.0:
            continue
        cz = ceiling_height(vx, vy)
        drop_len = rng.uniform(7.5, 17.5)
        hue = ("teal", "amber", "magenta")[v_i % 3]
        v_pts = []
        for s in range(6):
            ft = s / 5.0
            v_pts.append(Vector((
                vx + math.sin(ft * 4.5 + v_i) * 1.4,
                vy + math.cos(ft * 4.0 + v_i) * 1.4,
                cz + 0.6 - ft * drop_len,
            )))
        rgs = _parallel_tube_rings(bm_flora, v_pts, [0.16 * (1.0 - 0.65 * (k / 5.0)) for k in range(6)], 5)
        kit.bridge_rings(bm_flora, rgs)
        kit.cap_ring(bm_flora, rgs[-1])

        # Gắn 2–3 chùm nang bào tử phát sáng dọc dây leo rủ để vòm trần rực rỡ chiều sâu
        for b_k in (0.45, 0.72, 1.0):
            idx_p = min(len(v_pts) - 1, int(b_k * (len(v_pts) - 1)))
            bp = v_pts[idx_p]
            rad_b = rng.uniform(0.32, 0.68) * (0.7 + 0.5 * b_k)
            res = bmesh.ops.create_icosphere(bm_glow, subdivisions=1, radius=rad_b)
            for vv in res["verts"]:
                vv.co = Vector((vv.co.x * 0.85, vv.co.y * 0.85, vv.co.z * 1.35)) + bp
            cap_hue_zones.append((bp, rad_b * 1.8, hue))

    # Gắn bào tử phát sáng ngay đầu các cụm nhũ đá trần
    for sb_i, sbp in enumerate(stalactite_buds):
        if sb_i % 2 == 0:
            hue = ("amber", "teal", "magenta")[sb_i % 3]
            res = bmesh.ops.create_icosphere(bm_glow, subdivisions=1, radius=rng.uniform(0.35, 0.65))
            for vv in res["verts"]:
                vv.co += sbp
            cap_hue_zones.append((sbp, 1.5, hue))

    # ---- 5. Nấm Tai Bậc Thang bám sườn vách đá quanh khoang mở rộng
    for si in range(32):
        ang = si * (2.0 * math.pi / 32.0) + 0.15
        if min(abs(_delta_ang(ang, ta)) for ta in TUNNEL_ANGLES) < math.radians(13.0):
            continue
        origin = Vector((math.cos(ang) * 28.0, math.sin(ang) * 20.0, rng.uniform(4.0, 14.0)))
        dir_w = Vector((math.cos(ang) * 110.0, math.sin(ang) * 84.0, rng.uniform(-1.5, 3.0)))
        hit_loc, hit_n = ray_wall(walk_bvh, origin, dir_w, max_dist=105.0)
        if hit_loc is None or abs(hit_n.z) > 0.65:
            continue
        hue = "amber" if si % 2 == 0 else ("magenta" if si % 5 == 0 else "teal")
        for f_k in range(rng.randint(2, 4)):
            sh_p = hit_loc - hit_n * 0.24 + Vector((rng.uniform(-0.9, 0.9), rng.uniform(-0.9, 0.9), f_k * 0.78 - 0.5))
            sr = rng.uniform(1.3, 2.7) * (1.0 - f_k * 0.16)
            _add_mushroom_cap(bm_glow, None, sh_p, (hit_n * 0.35 + Vector((0, 0, 0.94))).normalized(), sr, sr * 0.24, sag=0.14, seed=si + f_k, segs=12)
            cap_hue_zones.append((sh_p, sr * 1.3, hue))

    # ---- 6. Mạch Sợi Nấm Dẫn Đường trên cầu đá
    for vi in range(10):
        u_t = 0.08 + (vi / 9.0) * 0.84
        side_off = rng.uniform(-2.4, 2.4)
        pts = []
        last_z = None
        for s in range(6):
            ft = (s - 2.5) / 5.0
            uu = _clamp01(u_t + ft * 0.13)
            cp = _bridge_curve_point(uu)
            loc, _ = ray_ground(walk_bvh, cp.x + side_off + math.sin(s * 1.4 + vi) * 0.7, cp.y + math.cos(s * 1.2 + vi) * 0.7)
            if loc is None:
                continue
            if last_z is not None and abs(loc.z - last_z) > 0.75:
                break
            last_z = loc.z
            pts.append(loc + Vector((0, 0, 0.06)))
        if len(pts) >= 3:
            sp = _smooth_path(pts, sub=2)
            rgs = _parallel_tube_rings(bm_glow, sp, [0.11 * math.sin(math.pi * k / max(1, len(sp) - 1)) + 0.03 for k in range(len(sp))], 5)
            kit.bridge_rings(bm_glow, rgs)
            kit.cap_ring(bm_glow, rgs[0], flip=True)
            kit.cap_ring(bm_glow, rgs[-1])
            cap_hue_zones.append((pts[len(pts) // 2], 4.5, "amber" if vi % 2 == 0 else "teal"))

    flora_obj = kit.finish_object(bm_flora, "MazeChamber_Luma_Flora", _make_rock_material("MazeChamber_FloraMat"))
    kit.paint(flora_obj, lambda p, n: _mix(BARK_DARK, _mix(STALK_PALE, BARK_PLUM, 0.5 + 0.5 * noise.noise(p * 0.22)), _smoothstep(-1.0, 12.0, p.z)))

    glow_obj = kit.finish_object(bm_glow, "MazeChamber_Luma_Glow", _make_glow_material("MazeChamber_Luma_GlowMat", energy=0.95))

    def paint_glow(p, n):
        best_tag = "teal"
        best_d = 1e9
        for zp, zr, tag in cap_hue_zones:
            d = (p - zp).length - zr
            if d < best_d:
                best_d = d
                best_tag = tag
        t_n = 0.5 + 0.5 * noise.noise(p * 0.45)
        if best_tag == "amber":
            return _mix(GLOW_AMBER, GLOW_GOLD, t_n)
        if best_tag == "magenta":
            return _mix(GLOW_MAGENTA, GLOW_VIOLET, t_n)
        return _mix(GLOW_TEAL, GLOW_LIME, t_n * 0.70)

    kit.paint(glow_obj, paint_glow)
    return [flora_obj, glow_obj]


# ---------------------------------------------------------------- Thảm Cỏ Rêu Dày & 3 Mẫu Thực Vật Nhỏ Biến Dị

def _build_lush_grass_blade_clump():
    """Bụi cỏ lá cong 8–9 ngọn mảnh nhẹ (~54 tris/bụi) phủ dày mặt đất, nhiều lớp sắc độ."""
    bm = bmesh.new()
    rng = random.Random(400)
    for k in range(8):
        ang = k * (2.0 * math.pi / 8.0) + rng.uniform(-0.25, 0.25)
        dist = rng.uniform(0.05, 0.48)
        ox, oy = math.cos(ang) * dist, math.sin(ang) * dist
        h = rng.uniform(0.65, 1.35)
        lean = rng.uniform(0.30, 0.72)
        side_v = Vector((-math.sin(ang), math.cos(ang), 0.0))
        fwd_v = Vector((math.cos(ang), math.sin(ang), 0.0))

        # Mỗi ngọn cỏ là một dải cong 3 đốt thuôn nhọn (chỉ 6 tris -> rải hàng trăm bụi cực nhẹ!)
        w0 = rng.uniform(0.075, 0.11)
        pts = [
            (Vector((ox, oy, -0.10)), w0),
            (Vector((ox, oy, h * 0.42)) + fwd_v * (lean * 0.28), w0 * 0.75),
            (Vector((ox, oy, h * 0.82)) + fwd_v * (lean * 0.65), w0 * 0.42),
            (Vector((ox, oy, h)) + fwd_v * lean, 0.008),
        ]
        rows = []
        for p, w in pts[:-1]:
            rows.append((bm.verts.new(p - side_v * w), bm.verts.new(p + side_v * w)))
        tip_v = bm.verts.new(pts[-1][0])
        for r_i in range(len(rows) - 1):
            bm.faces.new([rows[r_i][0], rows[r_i][1], rows[r_i + 1][1], rows[r_i + 1][0]])
        bm.faces.new([rows[-1][0], rows[-1][1], tip_v])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm


def _build_mutant_reed_template():
    bm = bmesh.new()
    rng = random.Random(401)
    for k in range(7):
        ang = k * (2.0 * math.pi / 7.0) + rng.uniform(-0.2, 0.2)
        dist = rng.uniform(0.05, 0.38)
        ox, oy = math.cos(ang) * dist, math.sin(ang) * dist
        h = rng.uniform(0.85, 1.55)
        lean = rng.uniform(0.35, 0.72)
        stem = [
            Vector((ox, oy, -0.15)),
            Vector((ox + math.cos(ang) * lean * 0.35, oy + math.sin(ang) * lean * 0.35, h * 0.35)),
            Vector((ox + math.cos(ang) * lean * 0.65, oy + math.sin(ang) * lean * 0.65, h * 0.62)),
        ]
        fork_p = stem[-1]
        rgs = _parallel_tube_rings(bm, stem, [0.060, 0.050, 0.040], 4)
        kit.bridge_rings(bm, rgs)
        for side in (-1.0, 1.0):
            fa = ang + side * rng.uniform(0.55, 0.95)
            prong = [
                fork_p,
                fork_p + Vector((math.cos(fa) * 0.25, math.sin(fa) * 0.25, h * 0.22)),
                fork_p + Vector((math.cos(fa) * 0.42, math.sin(fa) * 0.42, h * 0.38)),
            ]
            rgs2 = _parallel_tube_rings(bm, prong, [0.038, 0.024, 0.008], 4)
            kit.bridge_rings(bm, rgs2)
            kit.cap_ring(bm, rgs2[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm


def _build_twin_curl_fern_template():
    bm = bmesh.new()
    rng = random.Random(402)
    for k in range(6):
        ang = k * (2.0 * math.pi / 6.0) + rng.uniform(-0.25, 0.25)
        h = rng.uniform(0.75, 1.30)
        pts = []
        for s in range(5):
            ft = s / 4.0
            curl_r = 0.70 * ft - 0.32 * max(0.0, ft - 0.6) ** 1.5
            curl_z = h * math.sin(ft * math.pi * 0.65) - 0.22 * max(0.0, ft - 0.72)
            tw = math.sin(ft * 4.5 + k) * 0.14
            pts.append(Vector((
                math.cos(ang + tw) * curl_r,
                math.sin(ang + tw) * curl_r,
                -0.12 + curl_z,
            )))
        rgs = _parallel_tube_rings(bm, pts, [0.052 * (1.0 - 0.70 * (s / 4.0)) for s in range(5)], 4)
        kit.bridge_rings(bm, rgs)
        kit.cap_ring(bm, rgs[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm


def _build_stacked_sprout_template():
    bm = bmesh.new()
    rng = random.Random(403)
    for k in range(4):
        ang = k * (2.0 * math.pi / 4.0) + 0.3
        ox, oy = math.cos(ang) * 0.26, math.sin(ang) * 0.26
        h = rng.uniform(0.70, 1.30)
        tip = Vector((ox + math.cos(ang) * 0.36, oy + math.sin(ang) * 0.36, h))
        pts = [Vector((ox * 0.5, oy * 0.5, -0.15)), Vector((ox, oy, h * 0.55)), tip]
        rgs = _parallel_tube_rings(bm, pts, [0.048, 0.036, 0.025], 4)
        kit.bridge_rings(bm, rgs)
        _add_mushroom_cap(bm, None, tip, Vector((math.cos(ang) * 0.2, math.sin(ang) * 0.2, 1.0)), 0.34, 0.08, sag=0.18, seed=k, segs=8)
        top2 = tip + Vector((-math.cos(ang) * 0.12, -math.sin(ang) * 0.12, 0.24))
        _add_mushroom_cap(bm, None, top2, Vector((-math.cos(ang) * 0.18, math.sin(ang) * 0.18, 1.0)), 0.20, 0.06, sag=0.16, seed=k + 3.0, segs=8)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm


def _copy_bmesh_into(src_bm, dst_bm, xform):
    vmap = {}
    for v in src_bm.verts:
        vmap[v.index] = dst_bm.verts.new(xform @ v.co)
    for f in src_bm.faces:
        try:
            dst_bm.faces.new([vmap[v.index] for v in f.verts])
        except ValueError:
            pass


def build_undergrowth_carpets(walk_bvh, rng):
    """Phủ dày đặc map mở rộng với ~420 bụi cỏ lá cong xanh/vàng/tím + ~220 cụm thực vật nhỏ biến dị + bèo sáng ven hồ."""
    bm_grass_template = _build_lush_grass_blade_clump()
    templates = [
        ("reed", _build_mutant_reed_template()),
        ("fern", _build_twin_curl_fern_template()),
        ("sprout", _build_stacked_sprout_template()),
    ]
    bm_grass = bmesh.new()
    bm_glow = bmesh.new()
    grass_hints = []
    glow_hints = []

    # 1. Rải dày ~850 bụi cỏ cong đa sắc khắp thung lũng, hai bên bờ suối, đồi Tây/Đông, thềm ban công & rìa cầu đá
    for _ in range(1100):
        wx = rng.uniform(-96.0, 96.0)
        wy = rng.uniform(-74.0, 74.0)
        if _boundary_u(wx, wy) > 0.81:
            continue
        loc, norm = ray_ground(walk_bvh, wx, wy)
        if loc is None or norm.z < 0.70:
            continue
        if loc.z < LOWER_WATER_Z + 0.08:
            continue
        if loc.z > 5.0 and loc.z < UPPER_WATER_Z + 0.08 and _upper_pool_dist(wx, wy) < 1.12:
            continue
        # Chừa khoảng thoáng sát ngay ống kính camera valley để cỏ không che khung hình
        if (loc - Vector((20.0, -24.0, 2.8))).length < 5.0:
            continue

        sc = rng.uniform(0.75, 1.38)
        yaw = rng.uniform(0.0, 2.0 * math.pi)
        up_align = Vector((0.0, 0.0, 1.0)).rotation_difference(norm).to_matrix().to_4x4()
        xform = (
            Matrix.Translation(loc - Vector((0.0, 0.0, 0.06)))
            @ up_align
            @ Matrix.Rotation(yaw, 4, "Z")
            @ Matrix.Scale(sc, 4)
        )
        _copy_bmesh_into(bm_grass_template, bm_grass, xform)
        grass_hints.append((loc.copy(), rng.randint(0, 2)))

    # 2. Rải ~220 cụm thực vật nhỏ biến dị phát sáng theo 26 quần thể dọc bờ nước, gốc cây, thềm cao & dưới gầm cầu
    patch_centers = [
        (-28.0, -16.0, 0), (-14.0, -18.0, 1), (-4.0, -4.0, 2), (10.0, -14.0, 0),
        (20.0, 4.0, 1), (-32.0, 12.0, 2), (32.0, -18.0, 0), (46.0, -6.0, 1),
        (38.0, 18.0, 2), (12.0, 32.0, 0), (-18.0, 36.0, 1), (-38.0, 40.0, 2),
        (-52.0, -24.0, 1), (8.0, -38.0, 2), (-32.0, -32.0, 0), (56.0, 8.0, 0),
        (6.0, 16.0, 1), (-8.0, -22.0, 2), (24.0, -32.0, 1), (-62.0, 10.0, 0),
        (62.0, 28.0, 2), (-46.0, 28.0, 1), (-18.0, 4.0, 0), (16.0, -4.0, 2),
        (-24.0, -8.0, 1), (-4.0, 24.0, 0),
    ]

    for px, py, base_kind in patch_centers:
        count = rng.randint(7, 11)
        for j in range(count):
            ang = rng.uniform(0.0, 2.0 * math.pi)
            dist = rng.uniform(0.4, 6.2)
            wx, wy = px + math.cos(ang) * dist, py + math.sin(ang) * dist
            loc, norm = ray_ground(walk_bvh, wx, wy)
            if loc is None or norm.z < 0.70 or loc.z < LOWER_WATER_Z + 0.05:
                continue
            if loc.z > 5.0 and loc.z < UPPER_WATER_Z + 0.05 and _upper_pool_dist(wx, wy) < 1.1:
                continue

            kind_idx = (base_kind + (1 if j % 3 == 0 else 0)) % 3
            _, src_bm = templates[kind_idx]
            sc = rng.uniform(0.95, 1.65)
            yaw = rng.uniform(0.0, 2.0 * math.pi)
            up_align = Vector((0.0, 0.0, 1.0)).rotation_difference(norm).to_matrix().to_4x4()
            xform = (
                Matrix.Translation(loc - Vector((0.0, 0.0, 0.08)))
                @ up_align
                @ Matrix.Rotation(yaw, 4, "Z")
                @ Matrix.Scale(sc, 4)
            )
            _copy_bmesh_into(src_bm, bm_glow, xform)
            glow_hints.append((loc.copy(), kind_idx))

    # 3. Rải Bèo Bào Tử Nước phát sáng nổi dọc mép Hồ Hạ & Suối (-0.85m)
    for pi in range(28):
        ang = rng.uniform(0.0, 2.0 * math.pi)
        r_f = rng.uniform(0.45, 0.92)
        px = -12.0 + math.cos(ang) * 16.0 * r_f if pi < 16 else rng.uniform(-45.0, 45.0)
        py = (6.0 + math.sin(ang) * 12.0 * r_f) if pi < 16 else (_stream_center_y(px) + rng.uniform(-5.5, 5.5))
        if terrain_height(px, py) > LOWER_WATER_Z - 0.15:
            continue
        pad_r = rng.uniform(0.55, 1.25)
        pad_p = Vector((px, py, LOWER_WATER_Z + 0.03))
        _add_mushroom_cap(bm_glow, None, pad_p, Vector((0, 0, 1)), pad_r, 0.06, sag=-0.05, seed=pi * 1.7, segs=10)
        glow_hints.append((pad_p, 2))

    bm_grass_template.free()
    for _, src_bm in templates:
        src_bm.free()

    grass_obj = kit.finish_object(bm_grass, "MazeChamber_Undergrowth_Grass", _make_rock_material("MazeChamber_GrassMat"))

    def paint_grass(p, n):
        n_c = 0.5 + 0.5 * noise.noise(p * 0.18 + Vector((3.0, 7.0, 1.0)))
        tip_t = _clamp01((p.z - terrain_height(p.x, p.y)) * 0.75)
        base_col = _mix(MOSS_DEEP, MOSS_EMERALD, n_c)
        if n_c > 0.65:
            tip_col = _mix(MOSS_LUSH, MOSS_GOLD, (n_c - 0.65) * 2.5)
        elif n_c < 0.25:
            tip_col = (0.18, 0.42, 0.36)
        else:
            tip_col = (0.28, 0.55, 0.12)
        return _mix(base_col, tip_col, tip_t)

    kit.paint(grass_obj, paint_grass)

    glow_obj = kit.finish_object(bm_glow, "MazeChamber_Undergrowth_Glow", _make_glow_material("MazeChamber_UndergrowthGlowMat", energy=0.80))

    def paint_under(p, n):
        best_k = 1
        best_d = 1e9
        for hp, k_idx in glow_hints:
            d = (p - hp).length
            if d < best_d:
                best_d = d
                best_k = k_idx
        t_h = 0.5 + 0.5 * math.sin(p.z * 2.4 + p.x * 0.3)
        if best_k == 0:
            return _mix((0.14, 0.56, 0.18), GLOW_AMBER, _clamp01(0.42 + 0.58 * t_h))
        if best_k == 1:
            return _mix(GLOW_VIOLET, GLOW_MAGENTA, t_h)
        return _mix(GLOW_TEAL, GLOW_GOLD, t_h)

    kit.paint(glow_obj, paint_under)
    return [grass_obj, glow_obj]


# ---------------------------------------------------------------- Ánh sáng & 4 Góc Camera Preview 3D

def render_previews(all_objects, out_dir):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 20
    scene.cycles.max_bounces = 4
    scene.cycles.diffuse_bounces = 2
    if hasattr(scene.cycles, "use_denoising"):
        scene.cycles.use_denoising = True
    scene.render.resolution_x, scene.render.resolution_y = 960, 640
    scene.render.image_settings.file_format = "PNG"

    world = bpy.data.worlds.new("LumaChamberWorld")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.04, 0.08, 0.10, 1.0)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.80
    scene.world = world

    for o in list(scene.collection.objects):
        if o.type in {"CAMERA", "LIGHT"}:
            bpy.data.objects.remove(o, do_unlink=True)
        o.hide_render = o.name.endswith("_COL")

    sun = bpy.data.objects.new("_prev_sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy = 3.0
    sun.data.color = (1.0, 0.91, 0.74)
    sun.rotation_euler = (math.radians(34), math.radians(15), math.radians(26))
    kit.link(sun)

    fill_lights = [
        ("teal_pool", Vector((-12.0, 6.0, 12.0)), (0.18, 0.94, 0.88), 6200.0),
        ("waterfall_glow", Vector((-13.0, 32.0, 16.0)), (0.35, 0.90, 1.00), 5800.0),
        ("amber_bridge", Vector((12.0, -4.0, 16.0)), (1.00, 0.62, 0.20), 5200.0),
        ("under_bridge", Vector((8.0, -2.0, 4.5)), (0.20, 0.95, 0.82), 4200.0),
        ("magenta_terrace", Vector((-26.0, 40.0, 18.0)), (0.95, 0.30, 0.90), 5000.0),
        ("ceiling_wash", Vector((0.0, 4.0, 28.0)), (0.40, 0.88, 0.85), 5500.0),
        ("warm_east", Vector((48.0, -10.0, 15.0)), (0.96, 0.68, 0.30), 4800.0),
        ("west_knoll", Vector((-52.0, -12.0, 15.0)), (0.32, 0.80, 1.00), 4600.0),
    ]
    for lname, lpos, lcol, lenergy in fill_lights:
        lobj = bpy.data.objects.new("_prev_" + lname, bpy.data.lights.new(lname, "POINT"))
        lobj.location = lpos
        lobj.data.color = lcol
        lobj.data.energy = lenergy
        lobj.data.shadow_soft_size = 7.5
        kit.link(lobj)

    cam = bpy.data.objects.new("_prev_cam", bpy.data.cameras.new("cam"))
    kit.link(cam)
    scene.camera = cam

    views = {
        # 1. Toàn cảnh khoang mở rộng 230m (nằm hoàn toàn trước trụ đá (48,-48), thấy trọn thác 2 tầng, cầu đá, dòng suối & trần rậm)
        "overview": (Vector((24.0, -56.0, 27.0)), Vector((-12.0, 14.0, 6.0)), 18.0),
        # 2. Tầm mắt Slime nhìn xuyên gầm Cầu Đá hướng tới Thác Nước đổ vào Hồ Hạ & Nấm Đại Thụ + dây leo rủ trần
        "valley": (Vector((20.0, -24.0, 2.8)), Vector((-13.0, 14.0, 6.5)), 21.0),
        # 3. Từ Thềm Ban Công Cao bên cạnh Hồ Thượng & đỉnh Thác nhìn xuống toàn bộ Cầu Đá, sông ngọc lam & bờ cỏ
        "terrace": (Vector((-27.0, 48.0, 13.5)), Vector((12.0, -6.0, 4.8)), 20.0),
        # 4. Cận cảnh rìa hồ mượt, thảm cỏ lá cong rậm rạp, bèo sáng & 3 mẫu cây biến dị
        "closeup": (Vector((-34.0, -20.0, 3.2)), Vector((-24.0, -12.0, 2.2)), 26.0),
    }

    for vname, (cpos, target, lens) in views.items():
        cam.location = cpos
        cam.data.lens = lens
        cam.rotation_euler = (target - cpos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(out_dir, f"maze_chamber_luma_{vname}.png")
        bpy.ops.render.render(write_still=True)


def export_standalone_flora_assets(out_dir):
    """Xuất các model thực vật biến dị đơn lẻ để làm Prefab vật lý tương tác rẽ ngang trong Unity."""
    print("Xuất các FBX thực vật biến dị tương tác...")
    # 1. Cỏ Sừng Rẽ Đôi (Mutant Reed)
    bm_reed = _build_mutant_reed_template()
    reed_obj = kit.finish_object(bm_reed, "MazeFlora_MutantReed", _make_glow_material("MazeFlora_ReedMat", energy=0.85))
    kit.paint(reed_obj, lambda p, n: _mix((0.18, 0.52, 0.22), GLOW_AMBER, _clamp01(p.z * 0.7)))
    kit.export_fbx([reed_obj], os.path.join(out_dir, "MazeFlora_MutantReed.fbx"))
    bpy.data.objects.remove(reed_obj, do_unlink=True)

    # 2. Dương Xỉ Xoắn Kép (Twin Curl Fern)
    bm_fern = _build_twin_curl_fern_template()
    fern_obj = kit.finish_object(bm_fern, "MazeFlora_TwinFern", _make_glow_material("MazeFlora_FernMat", energy=0.85))
    kit.paint(fern_obj, lambda p, n: _mix((0.15, 0.45, 0.38), GLOW_VIOLET, _clamp01(p.z * 0.8)))
    kit.export_fbx([fern_obj], os.path.join(out_dir, "MazeFlora_TwinFern.fbx"))
    bpy.data.objects.remove(fern_obj, do_unlink=True)

    # 3. Mầm Nấm Chồng Tầng (Stacked Sprout)
    bm_sprout = _build_stacked_sprout_template()
    sprout_obj = kit.finish_object(bm_sprout, "MazeFlora_StackedSprout", _make_glow_material("MazeFlora_SproutMat", energy=0.90))
    kit.paint(sprout_obj, lambda p, n: _mix((0.25, 0.18, 0.42), GLOW_TEAL, _clamp01(p.z * 0.75)))
    kit.export_fbx([sprout_obj], os.path.join(out_dir, "MazeFlora_StackedSprout.fbx"))
    bpy.data.objects.remove(sprout_obj, do_unlink=True)

    # 4. Bụi Cỏ Lá Cong (Grass Clump)
    bm_grass = _build_lush_grass_blade_clump()
    grass_obj = kit.finish_object(bm_grass, "MazeFlora_GrassClump", _make_rock_material("MazeFlora_GrassMat"))
    kit.paint(grass_obj, lambda p, n: _mix(MOSS_DEEP, MOSS_LUSH, _clamp01(p.z * 0.9)))
    kit.export_fbx([grass_obj], os.path.join(out_dir, "MazeFlora_GrassClump.fbx"))
    bpy.data.objects.remove(grass_obj, do_unlink=True)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=None)
    ap.add_argument("--previews", default=None)
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    args = ap.parse_args(argv)

    t0 = time.time()
    rng = random.Random(202606)
    kit.new_scene()

    print("[1/5] Dựng khoang mở rộng (~230m x 175m), cầu đá vòm, gân trần & nhũ đá...")
    shell_obj, col_obj, walk_bvh, stalactite_buds = build_chamber_shell(rng)

    print("[2/5] Dựng nguồn nước (Thác 2 tầng, Hồ Thượng, Hồ Hạ & Dòng Suối Ngọc Lam)...")
    water_objs = build_water_system(walk_bvh, rng)

    print("[3/5] Dựng Nấm Đại Thụ, Cây Vòi Rồng, Rễ Vòm Trần & 42 Dây Leo Phát Sáng...")
    flora_objs = build_giant_flora_and_ceiling_life(walk_bvh, stalactite_buds, rng)

    print("[4/5] Rải dày ~420 bụi cỏ lá cong + ~220 cụm thực vật biến dị + bèo sáng...")
    under_objs = build_undergrowth_carpets(walk_bvh, rng)

    all_objs = [shell_obj, col_obj] + water_objs + flora_objs + under_objs

    if args.out:
        out_dir = os.path.abspath(args.out)
        print(f"Xuất FBX MazeChamber_Luma vào {out_dir}...")
        kit.export_fbx(all_objs, os.path.join(out_dir, "MazeChamber_Luma.fbx"))
        export_standalone_flora_assets(out_dir)

    if args.previews:
        prev_dir = os.path.abspath(args.previews)
        os.makedirs(prev_dir, exist_ok=True)
        print("[5/5] Render 4 góc ảnh 3D preview bằng Cycles...")
        render_previews(all_objs, prev_dir)

    print(
        f"Hoàn tất MazeChamber_Luma V6 trong {time.time() - t0:.1f}s | "
        f"Shell={kit.tri_count(shell_obj)} tris, "
        f"Water={sum(kit.tri_count(o) for o in water_objs)} tris, "
        f"Flora={sum(kit.tri_count(o) for o in flora_objs)} tris, "
        f"Undergrowth={sum(kit.tri_count(o) for o in under_objs)} tris"
    )


if __name__ == "__main__":
    main()
