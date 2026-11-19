"""Dựng 3 mẫu cổng & đường hầm mê cung tự nhiên (Blender bpy, headless) - Phiên bản Chi tiết cao & Bất đối xứng:
- Bề mặt đá phân tầng vỉa nghiêng, góc cạnh sắc nét, khe nứt sâu và texture sần sùi (Vertex Color + UV + Bump).
- Miệng hang và lòng hầm phá vỡ đối xứng (vách hàm ếch nhô một bên, bậc đá thấp bên kia, có hốc đá, nhũ đá và đá tảng rìa).
- Dấu hiệu hang tiếp theo (rễ hoa đom đóm / nấm lam / mạch tinh thể) mọc theo cụm bất đối xứng, rõ dần khi vào sâu.

Chạy preview:
  D:/blender/blender.exe -b -P tools/blender/build_maze_tunnels.py -- --previews D:/gametienhoa/docs/art/previews
"""
import argparse
import math
import os
import random
import sys

import bpy  # must come before bmesh/mathutils
import bmesh
from mathutils import Vector, Matrix, Euler, noise

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_common as kit  # noqa: E402

# Bảng màu linear chi tiết đá tự nhiên
STONE_SHADOW = (0.045, 0.040, 0.035)
STONE_DARK = (0.085, 0.075, 0.065)
STONE_MID = (0.175, 0.155, 0.130)
STONE_LIGHT = (0.31, 0.275, 0.225)
LICHEN_OCHRE = (0.26, 0.24, 0.14)
MOSS_DARK = (0.045, 0.13, 0.038)
MOSS_BRIGHT = (0.16, 0.34, 0.095)
SOIL_PATH = (0.11, 0.09, 0.068)
SOIL_PEBBLE = (0.21, 0.175, 0.14)
BARK_DARK = (0.065, 0.045, 0.030)
BARK_LIGHT = (0.165, 0.115, 0.072)
STALK_COL = (0.68, 0.64, 0.54)


def _mix(a, b, t):
    t = max(0.0, min(1.0, t))
    return (
        a[0] + (b[0] - a[0]) * t,
        a[1] + (b[1] - a[1]) * t,
        a[2] + (b[2] - a[2]) * t,
    )


def _clamp01(x):
    return max(0.0, min(1.0, x))


def _smoothstep(a, b, x):
    t = _clamp01((x - a) / max(1e-5, b - a))
    return t * t * (3.0 - 2.0 * t)


def _ridged(p):
    return 1.0 - abs(noise.noise(p))


def _stepped_strata(val, steps=5.0, sharpness=0.6):
    """Tạo bậc đá phẳng vát cạnh theo vỉa trầm tích thay vì uốn tròn mềm như đất sét."""
    s = val * steps
    fl = math.floor(s)
    frac = s - fl
    crisp = _smoothstep(0.5 - sharpness * 0.45, 0.5 + sharpness * 0.45, frac)
    return (fl + crisp) / steps


def _smooth_path(pts, sub=3):
    pts = [Vector(p) for p in pts]
    if len(pts) < 3:
        return pts
    out = []
    for i in range(len(pts) - 1):
        p0, p1 = pts[max(0, i - 1)], pts[i]
        p2, p3 = pts[i + 1], pts[min(len(pts) - 1, i + 2)]
        for k in range(sub):
            t = k / sub
            t2, t3 = t * t, t * t * t
            out.append(0.5 * (
                (2 * p1) + (-p0 + p2) * t
                + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2
                + (-p0 + 3 * p1 - 3 * p2 + p3) * t3
            ))
    out.append(pts[-1])
    return out


def _tunnel_center_at(y, length, curve_x, curve_z):
    t = _clamp01(y / length)
    s = t * t * (3.0 - 2.0 * t)
    cx = math.sin(t * math.pi * 1.15) * curve_x + s * (curve_x * 0.72)
    cz = math.sin(t * math.pi) * curve_z
    return cx, cz


def _rock_displacement(p, seed_vec, strata_tilt):
    """Điêu khắc đá góc cạnh đa tầng: vỉa trầm tích nghiêng bậc thang + nứt gãy đa giác + hạt đá thô."""
    # Tọa độ xoay nghiêng theo vỉa địa chất
    tilted_z = p.z * math.cos(strata_tilt) + p.x * math.sin(strata_tilt)
    strata_raw = (tilted_z * 0.55 + noise.noise(p * 0.42 + seed_vec) * 0.65)
    strata_step = (_stepped_strata(strata_raw, steps=4.0, sharpness=0.82) - strata_raw) * 1.15

    # Các khối đá gãy góc cạnh (Voronoi/ridged sắc)
    crack_big = (_ridged(p * 0.52 + seed_vec) - 0.55) * 0.85
    crack_mid = (_ridged(p * 1.45 + seed_vec * 1.9) - 0.50) * 0.36
    # Gợn phiến đá vát
    facet_vec = p * 0.85 + seed_vec
    plate = (math.floor(noise.noise(facet_vec) * 4.5) / 4.5) * 0.32
    micro = noise.noise(p * 4.8 + seed_vec * 3.1) * 0.08
    return strata_step + crack_big + crack_mid + plate + micro


def _textured_rock_material(name):
    """Vật liệu Blender kết hợp Vertex Color + Procedural Noise Bump/Roughness để preview thấy rõ vân đá sần sùi."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()

    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")

    vcol = nodes.new("ShaderNodeVertexColor")
    vcol.layer_name = "Col"
    links.new(vcol.outputs["Color"], bsdf.inputs["Base Color"])

    # Lấy tọa độ Object để tạo Bump sần sùi và độ nhám thay đổi theo thớ đá
    texcoord = nodes.new("ShaderNodeTexCoord")

    noise_coarse = nodes.new("ShaderNodeTexNoise")
    noise_coarse.inputs["Scale"].default_value = 3.2
    noise_coarse.inputs["Detail"].default_value = 8.0
    noise_coarse.inputs["Roughness"].default_value = 0.72
    links.new(texcoord.outputs["Object"], noise_coarse.inputs["Vector"])

    noise_fine = nodes.new("ShaderNodeTexNoise")
    noise_fine.inputs["Scale"].default_value = 18.0
    noise_fine.inputs["Detail"].default_value = 6.0
    noise_fine.inputs["Roughness"].default_value = 0.65
    links.new(texcoord.outputs["Object"], noise_fine.inputs["Vector"])

    mix_h = nodes.new("ShaderNodeMath")
    mix_h.operation = 'ADD'
    links.new(noise_coarse.outputs["Fac"], mix_h.inputs[0])
    links.new(noise_fine.outputs["Fac"], mix_h.inputs[1])

    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.55
    bump.inputs["Distance"].default_value = 0.08
    links.new(mix_h.outputs["Value"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    rough_ramp = nodes.new("ShaderNodeMapRange")
    rough_ramp.inputs["From Min"].default_value = 0.2
    rough_ramp.inputs["From Max"].default_value = 0.85
    rough_ramp.inputs["To Min"].default_value = 0.78
    rough_ramp.inputs["To Max"].default_value = 0.98
    links.new(noise_coarse.outputs["Fac"], rough_ramp.inputs["Value"])
    links.new(rough_ramp.outputs["Result"], bsdf.inputs["Roughness"])

    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def _add_faceted_rock_feature(bm, pos, scale_vec, seed_vec, rot_euler):
    """Thêm khối đá nứt góc cạnh / gờ đá cắm chặt gốc vào đất."""
    res = bmesh.ops.create_icosphere(bm, subdivisions=3, radius=1.0)
    rot = Euler(rot_euler, 'XYZ').to_matrix().to_4x4()
    trans = Matrix.Translation(pos) @ rot
    for v in res["verts"]:
        p = v.co.copy()
        n = noise.noise(p * 1.6 + seed_vec)
        cr = (_ridged(p * 2.4 + seed_vec) - 0.45) * 0.40
        p.x *= scale_vec.x * (1.0 + 0.30 * n + cr)
        p.y *= scale_vec.y * (1.0 + 0.25 * n)
        p.z *= scale_vec.z * (1.0 + 0.30 * n + cr)
        v.co = trans @ p


def build_tunnel_shell(kind, width, height, length, seed):
    rng = random.Random(seed)
    off = Vector((rng.uniform(0, 60), rng.uniform(0, 60), rng.uniform(0, 60)))

    # Bất đối xứng theo từng loại hầm: hướng uốn, bên hàm ếch nhô cao, độ nghiêng vỉa đá
    curve_x = 0.95 if kind == "Large" else (0.75 if kind == "Medium" else -0.60)
    curve_z = 0.18 if kind == "Large" else 0.10
    overhang_side = 1.0 if kind != "Medium" else -1.0
    strata_tilt = 0.42 if kind == "Large" else (-0.34 if kind == "Medium" else 0.28)

    y_start = -2.6
    slices = 80
    segs = 68

    bm = bmesh.new()
    bm_col = bmesh.new()

    rings = []
    col_rings = []
    ring_meta = []

    for i in range(slices):
        u = i / (slices - 1)
        y = y_start + u * (length - y_start)
        cx, cz = _tunnel_center_at(max(0.0, y), length, curve_x, curve_z)

        flare = 0.0
        if y < 1.5:
            f = _clamp01((1.5 - y) / 4.1)
            flare = f * f * 1.75

        # Dọc lòng hầm thỉnh thoảng mở rộng thành hốc lõm (alcove) rồi thu hẹp bất đối xứng
        alcove_left = 0.28 * math.exp(-((y - length * 0.35) ** 2) / 1.8)
        alcove_right = 0.32 * math.exp(-((y - length * 0.62) ** 2) / 2.2)

        pinch = 1.0 - 0.10 * math.sin(u * math.pi * 2.8 + seed)
        rx = (width * 0.5) * (1.0 + flare * 0.95) * pinch
        rz = height * (1.0 + flare * 0.45) * (0.95 + 0.09 * math.cos(u * math.pi * 2.3))

        ring = []
        col_ring = []
        for k in range(segs):
            ang = -math.pi * 0.5 + (2.0 * math.pi * k / segs)
            ca, sa = math.cos(ang), math.sin(ang)

            # Phá vỡ đối xứng trái/phải của mặt cắt hang
            asym = 1.0 + 0.24 * math.sin(ang * 2.0 + seed * 0.7) + 0.15 * math.cos(ang * 3.0 - seed)
            side_bias = 1.0 + (0.28 if ca * overhang_side > 0 else -0.14) * _smoothstep(-0.3, 0.8, sa)

            # Hốc đá mở rộng lệch một bên ở giữa hầm
            alcove = (alcove_left if ca < 0 else alcove_right) * (1.0 - abs(sa))

            if sa < -0.22:
                # SÀN ĐI LẠI: có độ nghiêng tự nhiên nhẹ, sỏi đá gợn lồi lõm ±0.09m và gờ đá mép chân vách
                floor_t = _clamp01((-sa - 0.22) / 0.78)
                px = cx + ca * rx * (1.08 - 0.06 * floor_t + alcove * 0.5)
                base_z = cz - 0.10 * floor_t + (sa + 0.22) * 0.14
                # Sàn nghiêng nhẹ theo vỉa đá nhưng vẫn rất dễ đi
                slope_cross = ca * 0.09 * overhang_side * (1.0 - floor_t * 0.5)
                wrinkle = noise.noise(Vector((px * 1.4, y * 1.2, 0.0)) + off) * 0.075
                crinkle = (_ridged(Vector((px * 3.2, y * 2.8, 1.0)) + off) - 0.5) * 0.055
                edge_lift = (1.0 - floor_t) * 0.24
                pz = base_z + slope_cross + wrinkle + crinkle + edge_lift
                col_pz = base_z + slope_cross * 0.5 + wrinkle * 0.3 + edge_lift * 0.5
            else:
                # VÁCH & VÒM BẤT ĐỐI XỨNG: một bên hàm ếch nhô cao vát góc cạnh, bên kia bậc đá sạt thấp
                px_raw = cx + ca * rx * asym * (1.0 + alcove)
                crown_skew_z = 0.34 * height * math.sin(ca * 1.5 + overhang_side * 0.85) if sa > 0.2 else 0.0
                # Bóp vách bên hàm ếch hơi chụm vào trên cao để tạo mái đá nhô ra
                overhang_pinch = -0.25 * overhang_side * _smoothstep(0.3, 0.9, sa) * _smoothstep(0.0, 0.8, ca * overhang_side)
                px_raw += overhang_pinch * (width * 0.35)

                pz_raw = cz + max(0.0, sa) * rz * side_bias * (0.92 + 0.18 * asym) + crown_skew_z
                p_vec = Vector((px_raw, y, pz_raw))
                disp = _rock_displacement(p_vec, off, strata_tilt)

                # Cắt chéo cửa hầm ở mặt trước (một bên sườn đá vươn ra phía trước xa hơn bên kia 1.4m!)
                asymmetric_lip = 0.0
                if y < 1.2:
                    lip_fade = 1.0 - _smoothstep(-2.6, 1.2, y)
                    asymmetric_lip = lip_fade * (0.85 if ca * overhang_side > 0 else 0.25) * _ridged(p_vec * 0.7 + off)

                dir_out = Vector((ca, 0.0, max(0.15, sa))).normalized()
                wall_flare = flare * (0.95 if abs(ca) > 0.42 else 0.42)
                p_disp = p_vec + dir_out * (disp * (0.78 + wall_flare * 0.45) + wall_flare * 0.58 + asymmetric_lip)
                px, pz = p_disp.x, p_disp.z
                col_pz = pz_raw

            if y < 0.0 and sa < -0.05 and abs(ca) > 0.55:
                pz = min(pz, 0.12)
                col_pz = min(col_pz, 0.10)

            ring.append(bm.verts.new((px, y, pz)))
            col_ring.append(bm_col.verts.new((px if sa < -0.22 else cx + ca * rx * asym, y, col_pz)))

        rings.append(ring)
        col_rings.append(col_ring)
        ring_meta.append((y, _clamp01(y / length)))

    kit.bridge_rings(bm, rings, flip=True)
    kit.bridge_rings(bm_col, col_rings, flip=True)

    # Lớp vỏ ngoài ôm dày tạo khối núi
    outer_rings = []
    for i, ring in enumerate(rings):
        y, t_depth = ring_meta[i]
        cx, cz = _tunnel_center_at(max(0.0, y), length, curve_x, curve_z)
        center = Vector((cx, y, cz + height * 0.38))
        o_ring = []
        for k, v in enumerate(ring):
            ang = -math.pi * 0.5 + (2.0 * math.pi * k / segs)
            sa = math.sin(ang)
            if sa < -0.22:
                o_ring.append(bm.verts.new((v.co.x * 1.20, v.co.y, v.co.z - 0.95)))
            else:
                out_dir = v.co - center
                out_dir.y = 0.0
                if out_dir.length < 0.1:
                    out_dir = Vector((0.0, 0.0, 1.0))
                out_dir.normalize()
                thickness = 2.0 + 1.5 * (1.0 - t_depth)
                nv = v.co + out_dir * thickness
                nv.z += 0.85 if sa > 0.25 else 0.0
                o_ring.append(bm.verts.new(nv))
        outer_rings.append(o_ring)

    kit.bridge_rings(bm, outer_rings, flip=False)

    for k in range(segs):
        k2 = (k + 1) % segs
        bm.faces.new([rings[0][k], rings[0][k2], outer_rings[0][k2], outer_rings[0][k]])
        bm.faces.new([outer_rings[-1][k], outer_rings[-1][k2], rings[-1][k2], rings[-1][k]])
    kit.cap_ring(bm, rings[-1], flip=True)

    # Thêm các khối gờ đá góc cạnh, thạch nhũ gãy ở mép cửa và đá tảng rìa sàn (bất đối xứng)
    _add_angular_rock_features(bm, kind, width, height, length, curve_x, curve_z, overhang_side, off, rng)

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    # Tách cạnh sắc tại các nếp gãy góc >= 24 độ để mặt đá nổi khối góc cạnh sắc nét, còn vùng thoải vẫn mượt!
    sharp_edges = [e for e in bm.edges if len(e.link_faces) == 2 and e.calc_face_angle(0.0) > math.radians(24.0)]
    if sharp_edges:
        bmesh.ops.split_edges(bm, edges=sharp_edges)

    mesh = bpy.data.meshes.new(f"MazeTunnel_{kind}")
    bm.to_mesh(mesh)
    bm.free()

    for p in mesh.polygons:
        p.use_smooth = True

    # Sinh UVMap đơn giản (triplanar-friendly) cho Unity/Blender
    uv_layer = mesh.uv_layers.new(name="UVMap")
    for poly in mesh.polygons:
        for li in poly.loop_indices:
            v = mesh.vertices[mesh.loops[li].vertex_index]
            uv_layer.data[li].uv = ((v.co.x + v.co.y) * 0.25, v.co.z * 0.25)

    mat_rock = _textured_rock_material(f"Rock_VC_{kind}")
    obj = bpy.data.objects.new(f"MazeTunnel_{kind}", mesh)
    kit.link(obj)
    mesh.materials.append(mat_rock)

    # Sơn Vertex Color đa tầng chi tiết cao (vân hạt đá, vỉa nghiêng, khe nứt đen, cạnh nổi sáng, rêu & địa y)
    def paint_tunnel(pos, nrm):
        depth_t = _smoothstep(-0.7, length * 0.85, pos.y)
        tilted_z = pos.z * math.cos(strata_tilt) + pos.x * math.sin(strata_tilt)
        strata_band = 0.5 + 0.5 * math.sin(tilted_z * 4.5 + noise.noise(pos * 0.9 + off) * 3.0)
        grain = 0.5 + 0.5 * noise.noise(pos * 6.5 + off)
        broad = 0.5 + 0.5 * noise.noise(pos * 1.3 + off)

        stone = _mix(STONE_DARK, _mix(STONE_MID, STONE_LIGHT, strata_band), broad)
        stone = _mix(stone, STONE_LIGHT, grain * 0.28)

        # Điểm thêm mảng địa y vàng đất trên các sống đá ngoài trời
        lichen_mask = (1.0 - _smoothstep(0.0, 2.5, pos.y)) * _smoothstep(0.35, 0.72, noise.noise(pos * 2.2 + off * 1.4))
        stone = _mix(stone, LICHEN_OCHRE, lichen_mask * 0.42)

        # Khe nứt & hốc gãy tối sâu
        crevice = _smoothstep(0.56, 0.90, _ridged(pos * 2.1 + off))
        stone = _mix(stone, STONE_SHADOW, crevice * 0.78)

        # Viền nổi nhẹ trên cạnh sắc đón ánh sáng
        if nrm.z > 0.15 and crevice < 0.35:
            ridge_edge = _smoothstep(0.65, 0.92, _ridged(pos * 1.1 + off * 0.5))
            stone = _mix(stone, STONE_LIGHT, ridge_edge * 0.25)

        # Sàn đất ẩm lẫn sỏi đá tự nhiên (không xanh nhựa)
        if nrm.z > 0.65 and pos.z < 0.42:
            pebble = _smoothstep(0.28, 0.78, noise.noise(pos * 5.2 + off))
            grit = _smoothstep(0.55, 0.85, noise.noise(pos * 11.0 + off))
            stone = _mix(SOIL_PATH, SOIL_PEBBLE, pebble * 0.75 + grit * 0.25)
            # Rêu loang bám ở 2 mép rìa sàn gần chân vách
            edge_moss = _smoothstep(width * 0.28, width * 0.48, abs(pos.x)) * (1.0 - depth_t)
            stone = _mix(stone, MOSS_DARK, edge_moss * 0.65)
        else:
            # Rêu loang bám bất đối xứng trên mặt đá hướng lên ở nửa ngoài cửa
            moss_zone = 1.0 - _smoothstep(0.3, 4.4, pos.y)
            up_facing = _smoothstep(0.22, 0.70, nrm.z + 0.35 * noise.noise(pos * 2.4 + off))
            moss_patch = _smoothstep(-0.25, 0.42, noise.noise(pos * 1.6 + off * 2.2))
            moss_col = _mix(MOSS_DARK, MOSS_BRIGHT, grain)
            stone = _mix(stone, moss_col, moss_zone * up_facing * moss_patch * 0.95)

        dark_fade = 1.0 - 0.80 * depth_t
        return (stone[0] * dark_fade, stone[1] * dark_fade, stone[2] * dark_fade)

    kit.paint(obj, paint_tunnel)
    col_obj = kit.collider_from_bm(bm_col, f"MazeTunnel_{kind}_COL")
    return obj, col_obj


def _add_angular_rock_features(bm, kind, width, height, length, curve_x, curve_z, overhang_side, off, rng):
    """Thêm tảng đá sạt ở mép chân ngoài cửa, thạch nhũ trần và đá tảng chân vách lệch bên (không treo cục rời trên cao)."""
    # 1. Tảng đá bậc sạt nằm sát mặt đất ở hai bên cửa (bên cao bên thấp phá đối xứng)
    _add_faceted_rock_feature(
        bm,
        Vector((-overhang_side * width * 0.62, -1.25, 0.05)),
        Vector((width * 0.24, 0.90, 0.38)),
        off + Vector((11, 2, 9)),
        (0.05, -overhang_side * 0.2, 0.3),
    )
    _add_faceted_rock_feature(
        bm,
        Vector((overhang_side * width * 0.72, -1.65, -0.05)),
        Vector((width * 0.20, 0.75, 0.28)),
        off + Vector((5, 9, 3)),
        (-0.05, overhang_side * 0.15, -0.4),
    )

    # 2. Thạch nhũ đá nhọn rủ lệch trên vòm hầm (3-5 cái ở các độ sâu khác nhau)
    stal_n = 5 if kind == "Large" else 3
    for s in range(stal_n):
        yy = 0.4 + s * (length * 0.16)
        cx, cz = _tunnel_center_at(yy, length, curve_x, curve_z)
        sx = cx + rng.uniform(-0.35, 0.35) * width + overhang_side * 0.25
        sz = cz + height * rng.uniform(0.86, 0.98)
        slen = rng.uniform(0.32, 0.65) * (height / 3.0)
        res = bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=6, radius1=0.02, radius2=0.16, depth=slen)
        rot = Euler((rng.uniform(-0.15, 0.15), rng.uniform(-0.15, 0.15), rng.uniform(0, 6.28)), 'XYZ').to_matrix().to_4x4()
        mat = Matrix.Translation(Vector((sx, yy, sz - slen * 0.32))) @ rot
        for v in res["verts"]:
            p = v.co.copy()
            p.x *= 1.0 + 0.28 * noise.noise(p * 4.0 + off)
            p.y *= 1.0 + 0.28 * noise.noise(p * 4.0 + off)
            v.co = mat @ p

    # 3. Đá tảng góc cạnh nằm rải sát chân vách (lệch bên, không chặn giữa đường đi)
    for r_i in range(4):
        yy = 1.2 + r_i * (length * 0.18)
        cx, cz = _tunnel_center_at(yy, length, curve_x, curve_z)
        side = overhang_side if (r_i % 3 != 0) else -overhang_side
        rx = cx + side * width * rng.uniform(0.40, 0.46)
        _add_faceted_rock_feature(
            bm,
            Vector((rx, yy, cz + 0.12)),
            Vector((0.36, 0.45, 0.30)),
            off + Vector((r_i * 7, 13, 17)),
            (rng.uniform(-0.2, 0.2), rng.uniform(-0.2, 0.2), rng.uniform(0, 3.14)),
        )


# ---------------------------------------------------------------- Dấu hiệu sinh thái bám vách bất đối xứng

def _make_glow_material(name, color, energy):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    col = nodes.new("ShaderNodeVertexColor")
    col.layer_name = "Col"
    links.new(col.outputs["Color"], bsdf.inputs["Base Color"])
    if "Emission Color" in bsdf.inputs:
        links.new(col.outputs["Color"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = energy
    bsdf.inputs["Roughness"].default_value = 0.35
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def _wall_ray_hit(shell_obj, origin, direction, max_dist=10.0):
    hit, loc, norm, _ = shell_obj.ray_cast(origin, direction.normalized(), distance=max_dist)
    if hit:
        return loc, norm
    return None, None


def _build_branching_roots(shell_obj, kind, width, height, length, seed, curve_x, curve_z, overhang_side):
    """Rễ cây bò chéo bất đối xứng (từ vách bên overhang vắt lên trần sang bên kia khi vào sâu)."""
    rng = random.Random(seed + 101)
    bm = bmesh.new()
    anchor_spots = []

    root_count = 19 if kind == "Small" else 10
    for i in range(root_count):
        t_start = (i / max(1, root_count - 1)) ** 0.72
        y0 = 0.05 + t_start * (length * 0.65)
        y1 = min(length - 0.8, y0 + rng.uniform(1.8, 3.6))
        # Bất đối xứng: phần lớn cụm rễ mọc từ vách bên có hàm ếch vắt chéo qua vòm trần
        side = overhang_side if (i % 3 != 0) else -overhang_side

        if i < 3:
            # 3 sợi rễ buông cong mềm ôm bên vách cửa có hàm ếch (lệch một bên cửa, không chắn giữa)
            y_m = rng.uniform(-0.5, 0.25)
            cx, cz = _tunnel_center_at(max(0.0, y_m), length, curve_x, curve_z)
            origin = Vector((cx, y_m, cz + height * 0.42))
            dir_v = Vector((overhang_side * rng.uniform(0.45, 0.85), 0.0, rng.uniform(0.55, 0.90)))
            hit_loc, _ = _wall_ray_hit(shell_obj, origin, dir_v)
            if hit_loc is None:
                continue
            top = hit_loc - dir_v.normalized() * 0.04 + Vector((0, 0, 0.14))
            drop_len = rng.uniform(0.70, 1.35)
            raw_pts = [
                top,
                top + Vector((-overhang_side * 0.12, 0.10, -drop_len * 0.5)),
                top + Vector((-overhang_side * 0.26, 0.20, -drop_len)),
            ]
            r0 = 0.036
        else:
            ang_base = rng.uniform(0.22, 1.25)
            raw_pts = []
            for s in range(6):
                fs = s / 5.0
                yy = y0 + fs * (y1 - y0)
                cx, cz = _tunnel_center_at(max(0.0, yy), length, curve_x, curve_z)
                origin = Vector((cx, yy, cz + height * 0.42))
                ang = ang_base + fs * rng.uniform(0.15, 0.55) + math.sin(fs * 2.5 + i) * 0.18
                dir_v = Vector((side * math.cos(ang), 0.0, math.sin(ang)))
                hit_loc, hit_n = _wall_ray_hit(shell_obj, origin, dir_v)
                if hit_loc is not None:
                    push = -0.05 if (s == 0 or s == 5) else 0.02
                    raw_pts.append(hit_loc + hit_n * push)
            if len(raw_pts) < 3:
                continue
            r0 = 0.038 + 0.075 * t_start

        pts = _smooth_path(raw_pts, sub=3)
        radii = [max(0.008, r0 * (1.0 - 0.82 * (k / max(1, len(pts) - 1)))) for k in range(len(pts))]
        rings = kit.tube_rings(bm, pts, radii, 7)
        kit.bridge_rings(bm, rings)
        kit.cap_ring(bm, rings[0], flip=True)
        kit.cap_ring(bm, rings[-1], flip=False)

        if i >= 3:
            for k in (len(pts) // 2, len(pts) - 2):
                anchor_spots.append((pts[k], t_start))

    name = f"MazeTunnel_{kind}_Roots"
    obj = kit.finish_object(bm, name, kit.vertex_color_material(name + "_Mat", roughness=0.88))
    kit.paint(obj, lambda p, n: _mix(BARK_DARK, BARK_LIGHT, _clamp01(0.5 + 0.5 * noise.noise(p * 2.5))))
    return obj, anchor_spots


def _add_bell_bud(bm, pos, drop_dir, size_m):
    res = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=size_m)
    quat = drop_dir.normalized().to_track_quat('Z', 'Y')
    mat = Matrix.Translation(pos) @ quat.to_matrix().to_4x4()
    for v in res["verts"]:
        p = v.co.copy()
        p.z *= 1.55
        if p.z < 0:
            p.x *= 0.65
            p.y *= 0.65
        v.co = mat @ p


def _add_mushroom_parts(bm_stalk, bm_cap, pos, up_dir, stalk_h, cap_r):
    quat = up_dir.normalized().to_track_quat('Z', 'Y')
    trans = Matrix.Translation(pos) @ quat.to_matrix().to_4x4()

    res_s = bmesh.ops.create_cone(bm_stalk, cap_ends=True, cap_tris=False, segments=10, radius1=cap_r * 0.25, radius2=cap_r * 0.14, depth=stalk_h + 0.08)
    for v in res_s["verts"]:
        v.co = trans @ (v.co + Vector((0, 0, stalk_h * 0.5 - 0.04)))

    res_c = bmesh.ops.create_cone(bm_cap, cap_ends=True, cap_tris=False, segments=14, radius1=cap_r, radius2=cap_r * 0.18, depth=cap_r * 0.58)
    for v in res_c["verts"]:
        v.co = trans @ (v.co + Vector((0, 0, stalk_h + cap_r * 0.12)))


def _add_crystal_shard(bm, pos, direction, length_m, radius_m):
    res = bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=6, radius1=radius_m, radius2=radius_m * 0.06, depth=length_m)
    quat = direction.normalized().to_track_quat('Z', 'Y')
    mat = Matrix.Translation(pos) @ quat.to_matrix().to_4x4()
    for v in res["verts"]:
        v.co = mat @ (v.co + Vector((0, 0, length_m * 0.30)))


def build_progressive_clues(shell_obj, kind, width, height, length, seed):
    bpy.context.view_layer.update()
    rng = random.Random(seed + 55)
    curve_x = 0.95 if kind == "Large" else (0.75 if kind == "Medium" else -0.60)
    curve_z = 0.18 if kind == "Large" else 0.10
    overhang_side = 1.0 if kind != "Medium" else -1.0

    objects = []
    roots, root_spots = _build_branching_roots(shell_obj, kind, width, height, length, seed, curve_x, curve_z, overhang_side)
    objects.append(roots)

    bm_glow = bmesh.new()
    bm_stalk = bmesh.new()

    if kind == "Small":
        glow_col_a = (0.35, 0.92, 0.35)
        glow_col_b = (0.85, 0.98, 0.28)
        energy = 1.6
        for pos, t in root_spots:
            bud_sz = 0.045 + 0.085 * t
            _add_bell_bud(bm_glow, pos + Vector((0, 0, -bud_sz * 0.7)), Vector((0, 0, -1)), bud_sz)
        for i in range(14):
            t = (i / 13.0) ** 0.65
            y = 1.0 + t * (length - 2.0)
            cx, cz = _tunnel_center_at(y, length, curve_x, curve_z)
            side = overhang_side if (i % 3 != 0) else -overhang_side
            ang = rng.uniform(0.35, 1.15)
            hit_loc, hit_n = _wall_ray_hit(shell_obj, Vector((cx, y, cz + height * 0.42)), Vector((side * math.cos(ang), 0, math.sin(ang))))
            if hit_loc is not None:
                bud_sz = 0.05 + 0.10 * t
                _add_bell_bud(bm_glow, hit_loc + hit_n * 0.02, Vector((-side * 0.4, 0, -0.9)), bud_sz)

    elif kind == "Medium":
        glow_col_a = (0.12, 0.62, 0.88)
        glow_col_b = (0.22, 0.90, 0.74)
        energy = 1.5
        clusters = 22
        for i in range(clusters):
            t = (i / (clusters - 1)) ** 0.72
            y = -0.2 + t * (length - 1.2)
            cx, cz = _tunnel_center_at(max(0.0, y), length, curve_x, curve_z)
            # Nửa ngoài mọc tập trung trong hốc ẩm một bên; vào sâu lan rộng sang cả hai bên vách
            side = -overhang_side if (t < 0.4 or i % 3 != 0) else overhang_side
            ang = rng.uniform(-0.12, 0.42)
            hit_loc, hit_n = _wall_ray_hit(shell_obj, Vector((cx, y, cz + height * 0.42)), Vector((side * math.cos(ang), 0, math.sin(ang))))
            if hit_loc is None:
                continue
            up_dir = (hit_n * 0.55 + Vector((0, 0, 0.85))).normalized()
            sub_n = 2 + int(t * 4)
            for _ in range(sub_n):
                sh = (0.09 + 0.46 * t) * rng.uniform(0.75, 1.25)
                cr = (0.055 + 0.26 * t) * rng.uniform(0.8, 1.2)
                off_p = hit_loc - hit_n * 0.04 + Vector((rng.uniform(-0.16, 0.16), rng.uniform(-0.20, 0.20), rng.uniform(-0.04, 0.06)))
                dir_m = (up_dir + Vector((rng.uniform(-0.22, 0.22), rng.uniform(-0.22, 0.22), 0))).normalized()
                _add_mushroom_parts(bm_stalk, bm_glow, off_p, dir_m, sh, cr)

        stalk_obj = kit.finish_object(bm_stalk, f"MazeTunnel_{kind}_Stalks", kit.vertex_color_material(f"{kind}_StalkMat", roughness=0.85))
        kit.paint(stalk_obj, lambda p, n: _mix((0.45, 0.42, 0.36), STALK_COL, _clamp01(0.5 + 0.5 * noise.noise(p * 3.0))))
        objects.append(stalk_obj)

    else:
        glow_col_a = (0.25, 0.52, 0.95)
        glow_col_b = (0.72, 0.30, 0.98)
        energy = 1.65
        clusters = 24
        for i in range(clusters):
            t = (i / (clusters - 1)) ** 0.70
            y = -0.3 + t * (length - 1.0)
            cx, cz = _tunnel_center_at(max(0.0, y), length, curve_x, curve_z)
            # Cụm tinh thể mọc dày ở các hốc đá chéo bên nhau
            side = overhang_side if (math.sin(t * 5.0) >= 0) else -overhang_side
            ang = rng.uniform(0.05, 1.15)
            hit_loc, hit_n = _wall_ray_hit(shell_obj, Vector((cx, y, cz + height * 0.42)), Vector((side * math.cos(ang), 0, math.sin(ang))))
            if hit_loc is None:
                continue
            base_dir = (hit_n * 0.8 + Vector((0, rng.uniform(-0.2, 0.2), rng.uniform(0.0, 0.35)))).normalized()
            shards = 2 + int(t * 4)
            for _ in range(shards):
                slen = (0.22 + 1.15 * t) * rng.uniform(0.75, 1.25)
                srad = 0.045 + 0.14 * t
                d = (base_dir + Vector((rng.uniform(-0.32, 0.32), rng.uniform(-0.32, 0.32), rng.uniform(-0.32, 0.32)))).normalized()
                _add_crystal_shard(bm_glow, hit_loc - hit_n * 0.08, d, slen, srad)

    if kind != "Medium":
        bm_stalk.free()

    glow_mat = _make_glow_material(f"MazeTunnel_{kind}_GlowMat", glow_col_a, energy)
    glow_obj = kit.finish_object(bm_glow, f"MazeTunnel_{kind}_Glow", glow_mat)
    for p in glow_obj.data.polygons:
        p.use_smooth = (kind != "Large")
    kit.paint(glow_obj, lambda p, n: _mix(glow_col_a, glow_col_b, _smoothstep(0.0, length, p.y)))
    objects.append(glow_obj)

    return objects


def render_tunnel_previews(objects, kind, out_dir, width, height, length):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 36
    scene.render.resolution_x, scene.render.resolution_y = 960, 640
    scene.render.image_settings.file_format = "PNG"

    world = bpy.data.worlds.new("TunnelWorld")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.04, 0.06, 0.05, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.65
    scene.world = world

    for o in list(scene.collection.objects):
        if o.type in {"CAMERA", "LIGHT"} or o.name.startswith("_preview"):
            bpy.data.objects.remove(o, do_unlink=True)

    # Mặt đất preview có sỏi đất gồ ghề & bump
    ground = bpy.data.meshes.new("_preview_ground")
    gobj = bpy.data.objects.new("_preview_ground", ground)
    kit.link(gobj)
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=32, y_segments=32, size=14.0)
    for v in bm.verts:
        v.co.y -= 7.5
        v.co.z = -0.06 + 0.08 * math.sin(v.co.x * 0.7) * math.cos(v.co.y * 0.7) + 0.04 * noise.noise(v.co * 1.4)
    bm.to_mesh(ground)
    bm.free()
    gmat = bpy.data.materials.new("_preview_ground_mat")
    gmat.use_nodes = True
    gnodes, glinks = gmat.node_tree.nodes, gmat.node_tree.links
    bsdf = gnodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (0.075, 0.115, 0.055, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.94
    gnoise = gnodes.new("ShaderNodeTexNoise")
    gnoise.inputs["Scale"].default_value = 12.0
    gnoise.inputs["Detail"].default_value = 6.0
    gbump = gnodes.new("ShaderNodeBump")
    gbump.inputs["Strength"].default_value = 0.45
    glinks.new(gnoise.outputs["Fac"], gbump.inputs["Height"])
    glinks.new(gbump.outputs["Normal"], bsdf.inputs["Normal"])
    gobj.data.materials.append(gmat)

    sun = bpy.data.objects.new("_preview_sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy = 2.8
    sun.rotation_euler = (math.radians(46), math.radians(18), math.radians(34))
    kit.link(sun)

    l_mouth = bpy.data.objects.new("_preview_mouth_light", bpy.data.lights.new("ml", "POINT"))
    l_mouth.data.energy = 110.0
    l_mouth.data.color = (0.95, 0.94, 0.84)
    l_mouth.location = (-0.8, -3.2, height * 1.05)
    kit.link(l_mouth)

    l_deep = bpy.data.objects.new("_preview_deep_light", bpy.data.lights.new("dl", "POINT"))
    l_deep.data.energy = 150.0
    l_deep.data.color = (0.4, 0.9, 0.65) if kind == "Small" else ((0.25, 0.85, 1.0) if kind == "Medium" else (0.65, 0.45, 1.0))
    l_deep.location = (0.4, length * 0.68, height * 0.48)
    kit.link(l_deep)

    for o in objects:
        o.hide_render = o.name.endswith("_COL")

    cam = bpy.data.objects.new("_preview_cam", bpy.data.cameras.new("cam"))
    cam.data.lens = 25.0
    kit.link(cam)
    scene.camera = cam

    views = {
        "front": (Vector((-width * 0.42, -width * 1.45 - 2.6, height * 0.78)), Vector((0.15, 2.2, height * 0.45))),
        "inside": (Vector((-0.15, 0.15, height * 0.48)), Vector((0.55, length * 0.75, height * 0.44))),
    }
    for vname, (cpos, target) in views.items():
        cam.location = cpos
        cam.rotation_euler = (target - cpos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(out_dir, f"maze_tunnel_{kind.lower()}_{vname}.png")
        bpy.ops.render.render(write_still=True)


SPECS = [
    ("Small", 2.8, 2.3, 8.2, 101),
    ("Medium", 4.4, 3.5, 9.0, 202),
    ("Large", 6.4, 4.9, 9.8, 303),
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=None)
    ap.add_argument("--previews", default=None)
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    args = ap.parse_args(argv)

    out_dir = os.path.abspath(args.out) if args.out else None
    preview_dir = os.path.abspath(args.previews) if args.previews else None
    if preview_dir:
        os.makedirs(preview_dir, exist_ok=True)

    for kind, width, height, length, seed in SPECS:
        kit.new_scene()
        shell, col = build_tunnel_shell(kind, width, height, length, seed)
        clues = build_progressive_clues(shell, kind, width, height, length, seed)
        all_objs = [shell, col] + clues
        if out_dir:
            kit.export_fbx(all_objs, os.path.join(out_dir, f"MazeTunnel_{kind}.fbx"))
        if preview_dir:
            render_tunnel_previews(all_objs, kind, preview_dir, width, height, length)
        print(f"Built MazeTunnel_{kind}: main_tris={kit.tri_count(shell)}, col_tris={kit.tri_count(col)}")


if __name__ == "__main__":
    main()
