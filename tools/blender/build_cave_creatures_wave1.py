"""Dựng 12 Sinh Vật 3D Đợt 1 (3 Hang Khởi Đầu: Hang Khởi Sinh, Vườn Quặng Cầu Vồng, Vực Nứt Tím).

Chạy headless:
  "D:/blender/blender.exe" -b -P tools/blender/build_cave_creatures_wave1.py
Tuỳ chọn lọc 1 loài để test nhanh:
  "D:/blender/blender.exe" -b -P tools/blender/build_cave_creatures_wave1.py -- --only bo_canh_cung
"""
import argparse
import math
import os
import random
import sys
import time

import bpy  # must come before bmesh/mathutils
import bmesh
from mathutils import Euler, Matrix, Vector, kdtree, noise

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_common as kc  # noqa: E402


DEFAULT_OUT_DIR = r"D:\gametienhoa\Assets\_Project\Models\Wildlife"
DEFAULT_PREV_DIR = r"D:\gametienhoa\docs\art\previews"


# ------------------------------------------------------------------ Vật liệu Vertex Color & Tiện ích Màu sắc

def _clamp01(x):
    return max(0.0, min(1.0, x))


def _lerp3(a, b, t):
    t = _clamp01(t)
    return (
        a[0] + (b[0] - a[0]) * t,
        a[1] + (b[1] - a[1]) * t,
        a[2] + (b[2] - a[2]) * t,
    )


def _rainbow_color(t):
    """Dải cầu vồng khoáng sản (Đỏ Ruby -> Vàng Cam -> Lục Bảo -> Lam Ngọc -> Tím Thạch Anh)."""
    stops = [
        (0.86, 0.14, 0.20),
        (0.92, 0.62, 0.10),
        (0.14, 0.78, 0.36),
        (0.12, 0.68, 0.90),
        (0.58, 0.22, 0.88),
        (0.86, 0.14, 0.20),
    ]
    t = t % 1.0
    seg = t * (len(stops) - 1)
    idx = int(math.floor(seg))
    frac = seg - idx
    return _lerp3(stops[idx], stops[idx + 1], frac)


def _make_creature_materials():
    """Trả về [Wildlife_Organic (nhám hữu cơ), Wildlife_Chitin (vỏ cứng bán bóng), Wildlife_Crystal_Glow (tinh thể phát sáng nhẹ)]."""
    # 0: Da/mai đá/lá hữu cơ nhám (0% bóng nhựa)
    m_org = bpy.data.materials.new("Wildlife_Organic")
    m_org.use_nodes = True
    nt = m_org.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    vcol = nt.nodes.new("ShaderNodeVertexColor")
    vcol.layer_name = "Col"
    tc = nt.nodes.new("ShaderNodeTexCoord")
    ns = nt.nodes.new("ShaderNodeTexNoise")
    ns.inputs["Scale"].default_value = 24.0
    ns.inputs["Detail"].default_value = 8.0
    ns.inputs["Roughness"].default_value = 0.65
    bmp = nt.nodes.new("ShaderNodeBump")
    bmp.inputs["Strength"].default_value = 0.26
    bmp.inputs["Distance"].default_value = 0.018
    mr = nt.nodes.new("ShaderNodeMapRange")
    mr.inputs["To Min"].default_value = 0.56
    mr.inputs["To Max"].default_value = 0.88
    nt.links.new(tc.outputs["Object"], ns.inputs["Vector"])
    nt.links.new(ns.outputs["Fac"], bmp.inputs["Height"])
    nt.links.new(ns.outputs["Fac"], mr.inputs["Value"])
    nt.links.new(vcol.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(mr.outputs["Result"], bsdf.inputs["Roughness"])
    nt.links.new(bmp.outputs["Normal"], bsdf.inputs["Normal"])
    if "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = 0.32
    if "Subsurface Weight" in bsdf.inputs:
        bsdf.inputs["Subsurface Weight"].default_value = 0.16
        bsdf.inputs["Subsurface Scale"].default_value = 0.04
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])

    # 1: Kitin / Đá Hắc Diện / Vảy Bóng nhẹ
    m_chi = bpy.data.materials.new("Wildlife_Chitin")
    m_chi.use_nodes = True
    nt = m_chi.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    vcol = nt.nodes.new("ShaderNodeVertexColor")
    vcol.layer_name = "Col"
    tc = nt.nodes.new("ShaderNodeTexCoord")
    ns = nt.nodes.new("ShaderNodeTexNoise")
    ns.inputs["Scale"].default_value = 18.0
    ns.inputs["Detail"].default_value = 6.0
    bmp = nt.nodes.new("ShaderNodeBump")
    bmp.inputs["Strength"].default_value = 0.16
    bmp.inputs["Distance"].default_value = 0.012
    mr = nt.nodes.new("ShaderNodeMapRange")
    mr.inputs["To Min"].default_value = 0.24
    mr.inputs["To Max"].default_value = 0.44
    nt.links.new(tc.outputs["Object"], ns.inputs["Vector"])
    nt.links.new(ns.outputs["Fac"], bmp.inputs["Height"])
    nt.links.new(ns.outputs["Fac"], mr.inputs["Value"])
    nt.links.new(vcol.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(mr.outputs["Result"], bsdf.inputs["Roughness"])
    nt.links.new(bmp.outputs["Normal"], bsdf.inputs["Normal"])
    if "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = 0.56
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])

    # 2: Tinh thể Quặng / Mắt / Đèn Đom Đóm (phát sáng dịu giữ rõ cạnh vát)
    m_cry = bpy.data.materials.new("Wildlife_Crystal_Glow")
    m_cry.use_nodes = True
    nt = m_cry.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    vcol = nt.nodes.new("ShaderNodeVertexColor")
    vcol.layer_name = "Col"
    nt.links.new(vcol.outputs["Color"], bsdf.inputs["Base Color"])
    if "Emission Color" in bsdf.inputs:
        nt.links.new(vcol.outputs["Color"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = 0.48
    bsdf.inputs["Roughness"].default_value = 0.14
    if "Metallic" in bsdf.inputs:
        bsdf.inputs["Metallic"].default_value = 0.14
    if "IOR" in bsdf.inputs:
        bsdf.inputs["IOR"].default_value = 1.66
    if "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = 0.90
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])

    m_cry2 = m_cry.copy()
    m_cry2.name = "Wildlife_Glow_Emissive"
    m_wing = m_org.copy()
    m_wing.name = "Wildlife_Wing_Membrane"
    m_detail = m_org.copy()
    m_detail.name = "Wildlife_Detail_Separate"
    return [m_org, m_chi, m_cry, m_cry2, m_wing, m_detail]


# ------------------------------------------------------------------ Thư viện Hình Khối Giải Phẫu 3D (Mọi chi tiết có bề dày 3D thật)

def _new_bm():
    bm = bmesh.new()
    col = bm.loops.layers.color.new("Col")
    return bm, col, set()


def _sculpt_remesh_into(dst_bm, dst_col, src_bm, voxel_size=0.016, smooth_iter=6,
                        decimate_ratio=0.74, mat_index=0, color_fn=None):
    """Hòa trộn toàn bộ khối chồng lấn trong src_bm bằng Voxel Remesh + Smooth để tạo bề mặt điêu khắc hữu cơ liền mạch,
    sau đó tô Vertex Color theo vị trí/pháp tuyến và gộp vào dst_bm."""
    tmp_mesh = bpy.data.meshes.new("_tmp_sculpt")
    src_bm.to_mesh(tmp_mesh)
    src_bm.free()

    tmp_obj = bpy.data.objects.new("_tmp_sculpt_obj", tmp_mesh)
    bpy.context.collection.objects.link(tmp_obj)
    bpy.context.view_layer.objects.active = tmp_obj

    rem = tmp_obj.modifiers.new("Remesh", 'REMESH')
    rem.mode = 'VOXEL'
    rem.voxel_size = voxel_size
    rem.use_smooth_shade = True

    sm = tmp_obj.modifiers.new("Smooth", 'SMOOTH')
    sm.factor = 0.55
    sm.iterations = smooth_iter

    if 0.1 < decimate_ratio < 0.99:
        dec = tmp_obj.modifiers.new("Decimate", 'DECIMATE')
        dec.ratio = decimate_ratio

    deps = bpy.context.evaluated_depsgraph_get()
    eval_obj = tmp_obj.evaluated_get(deps)
    eval_mesh = bpy.data.meshes.new_from_object(eval_obj)

    res_bm = bmesh.new()
    res_bm.from_mesh(eval_mesh)
    res_bm.normal_update()

    vert_map = {}
    for v in res_bm.verts:
        vert_map[v.index] = dst_bm.verts.new(v.co)
    for f in res_bm.faces:
        try:
            nf = dst_bm.faces.new([vert_map[v.index] for v in f.verts])
            nf.material_index = mat_index
            nf.smooth = True
            nrm = f.normal
            for loop in nf.loops:
                rgb = color_fn(loop.vert.co, nrm) if color_fn else (0.5, 0.5, 0.5)
                loop[dst_col] = (_clamp01(rgb[0]), _clamp01(rgb[1]), _clamp01(rgb[2]), 1.0)
        except ValueError:
            pass

    res_bm.free()
    bpy.data.meshes.remove(eval_mesh)
    bpy.data.objects.remove(tmp_obj, do_unlink=True)
    bpy.data.meshes.remove(tmp_mesh)


def _paint_faces(faces, col_layer, color_fn, flat_faces=None, is_flat=False):
    for f in faces:
        n = f.normal
        if is_flat and flat_faces is not None:
            flat_faces.add(f)
        for loop in f.loops:
            c = color_fn(loop.vert.co, n)
            loop[col_layer] = (_clamp01(c[0]), _clamp01(c[1]), _clamp01(c[2]), 1.0)


def _add_ellipsoid(bm, col_layer, center, radii, segs=16, rings=10, rot=(0.0, 0.0, 0.0),
                   warp_fn=None, mat_index=0, color_fn=None, flat_faces=None, is_flat=False):
    ret = bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=1.0)
    verts = ret["verts"]
    rx, ry, rz = radii
    mat_r = Euler(rot, "XYZ").to_matrix().to_4x4()
    c = Vector(center)
    for v in verts:
        p = v.co.copy()
        if warp_fn is not None:
            p = Vector(warp_fn(p))
        p = Vector((p.x * rx, p.y * ry, p.z * rz))
        v.co = c + (mat_r @ p)
    faces = list({f for v in verts for f in v.link_faces})
    for f in faces:
        f.material_index = mat_index
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    if color_fn is not None:
        _paint_faces(faces, col_layer, color_fn, flat_faces, is_flat)
    return faces


def _spline_pts(pts, sub=1):
    cur = [Vector(p) for p in pts]
    for _ in range(sub):
        nxt = [cur[0]]
        for i in range(len(cur) - 1):
            a, b = cur[i], cur[i + 1]
            nxt.append(a.lerp(b, 0.25))
            nxt.append(a.lerp(b, 0.75))
        nxt.append(cur[-1])
        cur = nxt
    return cur


def _spline_vals(vals, sub=1):
    cur = list(vals)
    for _ in range(sub):
        nxt = [cur[0]]
        for i in range(len(cur) - 1):
            a, b = cur[i], cur[i + 1]
            nxt.append(a * 0.75 + b * 0.25)
            nxt.append(a * 0.25 + b * 0.75)
        nxt.append(cur[-1])
        cur = nxt
    return cur


def _add_tube(bm, col_layer, pts, radii, sides=12, subdiv=1, shape_fn=None, twist=0.0,
              cap_ends=True, mat_index=0, color_fn=None, flat_faces=None, is_flat=False):
    s_pts = _spline_pts(pts, subdiv)
    if isinstance(radii[0], (tuple, list)):
        r_pairs = [(r[0], r[1]) for r in radii]
        rx_l = _spline_vals([r[0] for r in r_pairs], subdiv)
        rz_l = _spline_vals([r[1] for r in r_pairs], subdiv)
    else:
        r_s = _spline_vals(radii, subdiv)
        rx_l = r_s
        rz_l = r_s

    n = len(s_pts)
    rings = []
    prev_up = Vector((0.0, 0.0, 1.0))
    for i, p in enumerate(s_pts):
        if i == 0:
            t = (s_pts[1] - p).normalized()
        elif i == n - 1:
            t = (p - s_pts[-2]).normalized()
        else:
            t = (s_pts[i + 1] - s_pts[i - 1]).normalized()
        if t.length < 1e-6:
            t = Vector((0.0, -1.0, 0.0))
        ref = Vector((1.0, 0.0, 0.0)) if abs(t.dot(prev_up)) > 0.92 else prev_up
        side = t.cross(ref).normalized()
        up = side.cross(t).normalized()
        prev_up = up

        tw = twist * (i / max(1, n - 1))
        ring = []
        for s in range(sides):
            ang = 2.0 * math.pi * s / sides + tw
            ca, sa = math.cos(ang), math.sin(ang)
            m = shape_fn(ang, i / max(1, n - 1)) if shape_fn else 1.0
            off = (side * (ca * rx_l[i]) + up * (sa * rz_l[i])) * m
            ring.append(bm.verts.new(p + off))
        rings.append(ring)

    faces = []
    for i in range(n - 1):
        r0, r1 = rings[i], rings[i + 1]
        for s in range(sides):
            sn = (s + 1) % sides
            f = bm.faces.new((r0[s], r0[sn], r1[sn], r1[s]))
            f.material_index = mat_index
            faces.append(f)
    if cap_ends and len(rings) >= 2:
        f0 = bm.faces.new(list(reversed(rings[0])))
        f1 = bm.faces.new(rings[-1])
        f0.material_index = mat_index
        f1.material_index = mat_index
        faces.extend([f0, f1])

    bmesh.ops.recalc_face_normals(bm, faces=faces)
    if color_fn is not None:
        _paint_faces(faces, col_layer, color_fn, flat_faces, is_flat)
    return faces


def _add_crystal_prism(bm, col_layer, base, direction, length, radius_base, radius_mid=None,
                       sides=6, rot_twist=0.0, mat_index=2, color_fn=None, flat_faces=None):
    """Trụ tinh thể lục giác/ngũ giác vát chóp nhọn sắc nét (flat-shaded)."""
    d = Vector(direction).normalized()
    ref = Vector((1.0, 0.0, 0.0)) if abs(d.dot(Vector((0.0, 0.0, 1.0)))) > 0.9 else Vector((0.0, 0.0, 1.0))
    u = d.cross(ref).normalized()
    v = d.cross(u).normalized()
    b = Vector(base)
    r_mid = radius_mid if radius_mid is not None else radius_base * 0.78

    r0, r1 = [], []
    for k in range(sides):
        ang = 2.0 * math.pi * k / sides + rot_twist
        dir_r = u * math.cos(ang) + v * math.sin(ang)
        r0.append(bm.verts.new(b + dir_r * radius_base))
        r1.append(bm.verts.new(b + d * (length * 0.68) + dir_r * r_mid))
    apex = bm.verts.new(b + d * length)

    faces = []
    faces.append(bm.faces.new(list(reversed(r0))))
    for k in range(sides):
        kn = (k + 1) % sides
        faces.append(bm.faces.new((r0[k], r0[kn], r1[kn], r1[k])))
        faces.append(bm.faces.new((r1[k], r1[kn], apex)))
    for f in faces:
        f.material_index = mat_index
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    if color_fn is not None:
        _paint_faces(faces, col_layer, color_fn, flat_faces, is_flat=True)
    return faces


def _add_faceted_chunk(bm, col_layer, center, scale_v, seed_v, rot_euler=(0.0, 0.0, 0.0),
                       mat_index=1, color_fn=None, flat_faces=None, subdiv=1):
    res = bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=1.0)
    mat_r = Euler(rot_euler, "XYZ").to_matrix().to_4x4()
    c = Vector(center)
    sv = Vector(scale_v)
    verts = res["verts"]
    for v in verts:
        p = v.co.copy()
        n = 1.0 - abs(noise.noise(p * 2.2 + Vector((seed_v, seed_v * 0.8, seed_v * 1.4))))
        p.x *= sv.x * (0.72 + 0.45 * n)
        p.y *= sv.y * (0.72 + 0.45 * n)
        p.z *= sv.z * (0.75 + 0.48 * n)
        v.co = c + (mat_r @ p)
    faces = list({f for v in verts for f in v.link_faces})
    for f in faces:
        f.material_index = mat_index
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    if color_fn is not None:
        _paint_faces(faces, col_layer, color_fn, flat_faces, is_flat=True)
    return faces


def _add_wing_panel_3d(bm, col_layer, p_anchor, rim_pts, thickness=0.012, mat_index=0, color_fn=None):
    """Màng cánh dơi 3D có bề dày hai mặt khép kín nối từ khớp vai/khủy tới dãy ngón cánh."""
    a = Vector(p_anchor)
    top_v, bot_v = [], []
    off = Vector((0.0, 0.0, thickness * 0.5))
    a_t = bm.verts.new(a + off)
    a_b = bm.verts.new(a - off)
    for rp in rim_pts:
        r = Vector(rp)
        top_v.append(bm.verts.new(r + off * 0.65))
        bot_v.append(bm.verts.new(r - off * 0.65))
    faces = []
    for i in range(len(rim_pts) - 1):
        # Váy màng lõm nhẹ tạo độ căng giữa các ngón cánh
        faces.append(bm.faces.new((a_t, top_v[i], top_v[i + 1])))
        faces.append(bm.faces.new((a_b, bot_v[i + 1], bot_v[i])))
        # Dải viền mép ngoài nối đỉnh & đáy (khép kín tuyệt đối)
        faces.append(bm.faces.new((top_v[i], bot_v[i], bot_v[i + 1], top_v[i + 1])))
    # Hai cạnh biên đầu & cuối
    faces.append(bm.faces.new((a_t, a_b, bot_v[0], top_v[0])))
    faces.append(bm.faces.new((a_t, top_v[-1], bot_v[-1], a_b)))
    for f in faces:
        f.material_index = mat_index
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    if color_fn is not None:
        _paint_faces(faces, col_layer, color_fn)
    return faces


def _add_eye_bulb(bm, col_layer, pos, radius, yaw=0.0, brow_col=None, eye_col=(0.95, 0.72, 0.14), mat_eye=2):
    p = Vector(pos)
    if brow_col is not None:
        _add_ellipsoid(bm, col_layer, p, (radius * 1.25, radius * 1.15, radius * 1.18), segs=12, rings=8, mat_index=0, color_fn=brow_col)
    fwd = Vector((math.sin(yaw) * radius * 0.34, -math.cos(yaw) * radius * 0.34, 0.0))
    _add_ellipsoid(bm, col_layer, p + fwd, (radius * 0.86, radius * 0.86, radius * 0.86), segs=12, rings=8, mat_index=mat_eye, color_fn=lambda _p, _n: eye_col)


def _add_3d_leaf_blade(bm, col_layer, origin, forward_dir, up_dir, length, width, thickness=0.022,
                       curl=0.14, vein_mat=0, color_fn=None):
    """Lá thực vật 3D có bề dày hai mặt & sống gân nổi (an toàn Cull Back)."""
    o = Vector(origin)
    fwd = Vector(forward_dir).normalized()
    up = Vector(up_dir).normalized()
    side = fwd.cross(up).normalized()
    up = side.cross(fwd).normalized()

    steps = 8
    rows = []
    for i in range(steps):
        t = i / (steps - 1)
        w = width * math.sin(math.pi * (0.12 + 0.86 * (t ** 0.75))) * (1.0 - 0.18 * t)
        if i == steps - 1:
            w = width * 0.08
        th = thickness * (1.0 - 0.65 * t)
        bend = curl * (t * t) * length
        center = o + fwd * (t * length) + up * bend
        # 4 điểm quanh tiết diện hình thoi dẹt: Trái, Đỉnh sống lá, Phải, Đáy sống lá
        ring = [
            bm.verts.new(center - side * w),
            bm.verts.new(center + up * th),
            bm.verts.new(center + side * w),
            bm.verts.new(center - up * (th * 0.85)),
        ]
        rows.append(ring)

    faces = []
    for i in range(steps - 1):
        r0, r1 = rows[i], rows[i + 1]
        for k in range(4):
            kn = (k + 1) % 4
            f = bm.faces.new((r0[k], r0[kn], r1[kn], r1[k]))
            f.material_index = vein_mat
            faces.append(f)
    faces.append(bm.faces.new(list(reversed(rows[0]))))
    faces.append(bm.faces.new(rows[-1]))
    for f in faces[-2:]:
        f.material_index = vein_mat

    bmesh.ops.recalc_face_normals(bm, faces=faces)
    if color_fn is not None:
        _paint_faces(faces, col_layer, color_fn)
    return faces


def _add_amphibian_limb(bm, col_layer, pts, radii, toe_count=4, toe_len=0.10, webbed=True,
                        mat_index=0, color_fn=None):
    _add_tube(bm, col_layer, pts, radii, sides=12, subdiv=1, mat_index=mat_index, color_fn=color_fn)
    foot = Vector(pts[-1])
    prev = Vector(pts[-2])
    fwd = Vector((foot.x - prev.x, foot.y - prev.y, 0.0))
    if fwd.length < 1e-4:
        fwd = Vector((0.0, -1.0, 0.0))
    fwd.normalize()
    side = Vector((-fwd.y, fwd.x, 0.0))
    rf = radii[-1]
    _add_ellipsoid(
        bm, col_layer, foot + Vector((0, 0, rf * 0.25)),
        (rf * 1.15, rf * 1.25, rf * 0.55), segs=10, rings=6, mat_index=mat_index, color_fn=color_fn
    )
    toe_tips = []
    for i in range(toe_count):
        ang = (i - (toe_count - 1) * 0.5) * 0.45
        td = (fwd * math.cos(ang) + side * math.sin(ang)).normalized()
        t0 = foot + td * (rf * 0.65) + Vector((0, 0, rf * 0.20))
        t1 = t0 + td * (toe_len * 0.58) + Vector((0, 0, rf * 0.08))
        t2 = t0 + td * toe_len + Vector((0, 0, -rf * 0.14))
        _add_tube(bm, col_layer, [t0, t1, t2], [rf * 0.28, rf * 0.19, 0.008], sides=8, subdiv=0, mat_index=mat_index, color_fn=color_fn)
        toe_tips.append((t0, t1))
    if webbed and toe_count >= 2:
        for i in range(toe_count - 1):
            a0, a1 = toe_tips[i]
            b0, b1 = toe_tips[i + 1]
            mid0 = (a0 + b0) * 0.5
            mid1 = a0.lerp(a1, 0.72).lerp(b0.lerp(b1, 0.72), 0.5)
            _add_3d_leaf_blade(bm, col_layer, mid0, (mid1 - mid0), Vector((0, 0, 1)), (mid1 - mid0).length, (a1 - b1).length * 0.48, thickness=0.012, curl=0.0, vein_mat=mat_index, color_fn=color_fn)


def _add_moss_tuft(bm, col_layer, pos, scale=0.05, rng=None, color_fn=None):
    """Cụm rêu 3D nhỏ bám trên mai/lưng sinh vật."""
    p = Vector(pos)
    fn = color_fn or (lambda _p, _n: (0.18, 0.42, 0.14))
    for k in range(3):
        ang = k * 2.1 + (rng.uniform(-0.3, 0.3) if rng else 0.0)
        ox = math.cos(ang) * scale * 0.45
        oy = math.sin(ang) * scale * 0.45
        _add_ellipsoid(
            bm, col_layer, p + Vector((ox, oy, scale * 0.25)),
            (scale * 0.75, scale * 0.65, scale * 0.55), segs=8, rings=5,
            rot=(0.15, -0.15, ang), mat_index=0, color_fn=fn
        )


def _add_luma_bud(bm, col_layer, pos, up_dir, size=0.032, hue=0.35):
    """Nụ hoa / giọt sương phát sáng Luma nhỏ gắn trên sinh vật Vườn Quặng."""
    u = Vector(up_dir).normalized()
    p = Vector(pos)
    _add_tube(bm, col_layer, [p, p + u * (size * 1.4)], [size * 0.22, size * 0.14], sides=6, subdiv=0, mat_index=0, color_fn=lambda _p, _n: (0.16, 0.34, 0.14))
    _add_ellipsoid(
        bm, col_layer, p + u * (size * 1.6),
        (size * 0.58, size * 0.58, size * 0.72), segs=10, rings=6, mat_index=2,
        color_fn=lambda _p, _n, h=hue: _rainbow_color(h)
    )


def _dist_point_to_segment(p, a, b):
    ab = b - a
    l2 = ab.length_squared
    if l2 < 1e-8:
        return (p - a).length
    t = max(0.0, min(1.0, (p - a).dot(ab) / l2))
    proj = a + ab * t
    return (p - proj).length


def _add_bone(arm_data, name, head, tail, parent=None):
    b = arm_data.edit_bones.new(name)
    b.head = Vector(head)
    b.tail = Vector(tail)
    if (b.tail - b.head).length < 1e-4:
        b.tail = b.head + Vector((0.0, -0.05, 0.0))
    if name.startswith("Wing"):
        b.align_roll(Vector((0.0, 0.0, 1.0)))
    if parent is not None:
        b.parent = parent
    return b


def _assign_segment_weights(mesh_obj, arm_obj, bone_specs, z_shift=0.0):
    vgroups = {b[0]: mesh_obj.vertex_groups.new(name=b[0]) for b in bone_specs}
    segs = [
        (b[0], Vector((b[1][0], b[1][1], b[1][2] - z_shift)), Vector((b[2][0], b[2][1], b[2][2] - z_shift)), b[4], b[5])
        for b in bone_specs
    ]
    for v in mesh_obj.data.vertices:
        p = v.co
        weights = {}
        for name, h, t, rad, mult in segs:
            d = _dist_point_to_segment(p, h, t)
            if d < rad:
                w = ((1.0 - d / rad) ** 2.0) * mult
                if w > 0.005:
                    weights[name] = w
        if not weights:
            best = min(segs, key=lambda s: _dist_point_to_segment(p, s[1], s[2]))[0]
            weights[best] = 1.0
        tot = sum(weights.values())
        for name, w in weights.items():
            vgroups[name].add([v.index], w / tot, "REPLACE")

    mesh_obj.parent = arm_obj
    mod = mesh_obj.modifiers.new("Armature", "ARMATURE")
    mod.object = arm_obj


def _key_pose(arm_obj, frame, pose_dict):
    bpy.context.scene.frame_set(frame)
    for bname, vals in pose_dict.items():
        pb = arm_obj.pose.bones.get(bname)
        if pb is None:
            continue
        pb.rotation_mode = "QUATERNION"
        rot = vals.get("rot", (0.0, 0.0, 0.0))
        if bname.startswith("Wing"):
            rx, ry, rz = rot
            rot = (ry * 1.15, rz * 0.25, rx)
        loc = vals.get("loc", (0.0, 0.0, 0.0))
        scl = vals.get("scale", (1.0, 1.0, 1.0))
        pb.rotation_quaternion = Euler(rot, "XYZ").to_quaternion()
        pb.location = Vector(loc)
        pb.scale = Vector(scl)
        pb.keyframe_insert(data_path="rotation_quaternion", frame=frame)
        pb.keyframe_insert(data_path="location", frame=frame)
        pb.keyframe_insert(data_path="scale", frame=frame)


def _push_nla(arm_obj, clip_name, start_f, end_f, poses):
    act = bpy.data.actions.new(name=clip_name)
    if not arm_obj.animation_data:
        arm_obj.animation_data_create()
    arm_obj.animation_data.action = act
    if hasattr(act, "slots") and len(act.slots) == 0:
        act.slots.new(id_type="OBJECT", name=arm_obj.name)
    if hasattr(act, "slots") and act.slots:
        arm_obj.animation_data.action_slot = act.slots[0]

    for pb in arm_obj.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = (1.0, 0.0, 0.0, 0.0)
        pb.location = (0.0, 0.0, 0.0)
        pb.scale = (1.0, 1.0, 1.0)
    for f, pdict in poses:
        _key_pose(arm_obj, f, pdict)

    track = arm_obj.animation_data.nla_tracks.new()
    track.name = clip_name
    strip = track.strips.new(clip_name, start_f, act)
    strip.action_frame_start = start_f
    strip.action_frame_end = end_f
    arm_obj.animation_data.action = None


def _build_rig_and_nla(mesh_obj, rig_name, bone_specs, clips_spec, z_shift=0.0):
    arm_data = bpy.data.armatures.new(rig_name)
    arm_obj = bpy.data.objects.new(rig_name, arm_data)
    kc.link(arm_obj)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode="EDIT")
    created = {}
    for bname, head, tail, parent_name, _, _ in bone_specs:
        parent = created.get(parent_name) if parent_name else None
        h = (head[0], head[1], head[2] - z_shift)
        t = (tail[0], tail[1], tail[2] - z_shift)
        created[bname] = _add_bone(arm_data, bname, h, t, parent)
    bpy.ops.object.mode_set(mode="OBJECT")

    _assign_segment_weights(mesh_obj, arm_obj, bone_specs, z_shift=z_shift)

    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode="POSE")
    for clip_name, (start_f, end_f, poses) in clips_spec.items():
        _push_nla(arm_obj, clip_name, start_f, end_f, poses)
    for pb in arm_obj.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = (1.0, 0.0, 0.0, 0.0)
        pb.location = (0.0, 0.0, 0.0)
        pb.scale = (1.0, 1.0, 1.0)
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm_obj


def _apply_organic_voxel_remesh(bm, flat_faces):
    """Tách riêng phần thân hữu cơ (mat_index == 0) của Wave 1, hòa trộn khớp bằng Voxel Remesh + Smooth,
    bảo toàn Vertex Color gốc qua KDTree rồi gộp lại với giáp/mắt/tinh thể."""
    col_layer = bm.loops.layers.color.get("Col")
    if col_layer is None:
        return
    org_faces = [f for f in bm.faces if f.material_index == 0 and f not in flat_faces]
    if len(org_faces) < 12:
        return

    vert_cols = {}
    for f in org_faces:
        for loop in f.loops:
            vi = loop.vert.index
            c = loop[col_layer]
            if vi not in vert_cols:
                vert_cols[vi] = [c[0], c[1], c[2], 1]
            else:
                vert_cols[vi][0] += c[0]
                vert_cols[vi][1] += c[1]
                vert_cols[vi][2] += c[2]
                vert_cols[vi][3] += 1

    org_verts = list({v for f in org_faces for v in f.verts})
    kd = kdtree.KDTree(len(org_verts))
    idx_to_col = {}
    min_co = Vector((1e9, 1e9, 1e9))
    max_co = Vector((-1e9, -1e9, -1e9))
    for k_i, v in enumerate(org_verts):
        kd.insert(v.co, k_i)
        s = vert_cols[v.index]
        idx_to_col[k_i] = (s[0] / s[3], s[1] / s[3], s[2] / s[3])
        for ax in range(3):
            min_co[ax] = min(min_co[ax], v.co[ax])
            max_co[ax] = max(max_co[ax], v.co[ax])
    kd.balance()

    max_dim = max(max_co.x - min_co.x, max_co.y - min_co.y, max_co.z - min_co.z)
    voxel_size = max(0.011, min(0.022, max_dim * 0.016))

    org_bm = bmesh.new()
    vmap = {}
    for v in org_verts:
        vmap[v.index] = org_bm.verts.new(v.co)
    for f in org_faces:
        try:
            nf = org_bm.faces.new([vmap[v.index] for v in f.verts])
            nf.smooth = True
        except ValueError:
            pass

    bmesh.ops.delete(bm, geom=org_faces, context='FACES')
    orphan_verts = [v for v in bm.verts if not v.link_faces]
    if orphan_verts:
        bmesh.ops.delete(bm, geom=orphan_verts, context='VERTS')

    def lookup_col(co, _n):
        _, idx, _ = kd.find(co)
        return idx_to_col.get(idx, (0.5, 0.5, 0.5))

    _sculpt_remesh_into(bm, col_layer, org_bm, voxel_size=voxel_size, smooth_iter=6,
                        decimate_ratio=0.74, mat_index=0, color_fn=lookup_col)


_WAVE1_AUTO_REMESH = False


# Cấu hình bổ sung xương & animation Mắt (nhắm/mở) - Miệng (há/ngậm) - Lưỡi (thò/thụt) - Đuôi (vẫy/lơ lửng)
# eye: (x_off, y, z) | jaw: (x, y, z) hoặc None | tongue: (len, col) hoặc None | tail_mode: "wag" | "float" | None
_SPECIES_FACIAL_SPEC = {
    "Creature_BoCanhCungNho": {"eye": (0.048, -0.25, 0.18), "jaw": (0.0, -0.27, 0.14), "tongue": None, "tail_mode": None},
    "Creature_OcHo": {"eye": (0.045, -0.35, 0.18), "jaw": (0.0, -0.30, 0.08), "tongue": (0.05, (0.82, 0.44, 0.48)), "tail_mode": "wag"},
    "Creature_ThanLanBoHo": {"eye": (0.078, -0.64, 0.32), "jaw": (0.06, -0.66, 0.22), "tongue": (0.12, (0.82, 0.16, 0.26)), "tail_mode": "wag"},
    "Creature_CocHoGia": {"eye": (0.14, -0.28, 0.42), "jaw": (0.0, -0.32, 0.26), "tongue": (0.14, (0.86, 0.24, 0.34)), "tail_mode": None},
    "Creature_DomDomSacMau": {"eye": (0.036, -0.24, 0.38), "jaw": (0.0, -0.25, 0.34), "tongue": None, "tail_mode": "float"},
    "Creature_ThoLa": {"eye": (0.082, -0.40, 0.415), "jaw": (0.04, -0.46, 0.34), "tongue": (0.06, (0.88, 0.38, 0.44)), "tail_mode": "wag"},
    "Creature_BoGiapQuang": {"eye": (0.055, -0.28, 0.22), "jaw": (0.0, -0.30, 0.16), "tongue": None, "tail_mode": None},
    "Creature_RuaPhaLe": {"eye": (0.065, -0.42, 0.28), "jaw": (0.0, -0.44, 0.22), "tongue": (0.08, (0.84, 0.28, 0.36)), "tail_mode": "wag"},
    "Creature_DoiAmThoa": {"eye": (0.068, -0.23, 0.56), "jaw": (0.02, -0.22, 0.48), "tongue": (0.05, (0.82, 0.22, 0.36)), "tail_mode": "float"},
    "Creature_BoGaiTim": {"eye": (0.052, -0.25, 0.20), "jaw": (0.0, -0.27, 0.15), "tongue": None, "tail_mode": None},
    "Creature_NhenTinhThe": {"eye": (0.045, -0.28, 0.24), "jaw": (0.0, -0.29, 0.19), "tongue": None, "tail_mode": "float"},
    "Creature_BoCapThachAnh": {"eye": (0.048, -0.32, 0.22), "jaw": (0.0, -0.34, 0.17), "tongue": None, "tail_mode": "float"},
    "Creature_SenReu": {"eye": (0.095, -0.54, 0.29), "jaw": (0.04, -0.44, 0.10), "tongue": (0.06, (0.76, 0.42, 0.48)), "tail_mode": "wag"},
    "Creature_MuoiReu": {"eye": (0.048, -0.28, 0.445), "jaw": (0.0, -0.29, 0.41), "tongue": None, "tail_mode": "float"},
    "Creature_RetReu": {"eye": (0.055, -0.62, 0.155), "jaw": (0.0, -0.64, 0.11), "tongue": (0.07, (0.72, 0.22, 0.54)), "tail_mode": "wag"},
    "Creature_ReuMe": {"eye": (0.09, -0.34, 0.33), "jaw": None, "tongue": None, "tail_mode": None},
    "Creature_ChimNon": {"eye": (0.085, -0.175, 0.365), "jaw": (0.0, -0.19, 0.322), "tongue": (0.05, (0.88, 0.36, 0.42)), "tail_mode": "float"},
    "Creature_RanTromTrung": {"eye": (0.052, -0.50, 0.368), "jaw": (0.0, -0.48, 0.31), "tongue": (0.11, (0.94, 0.18, 0.24)), "tail_mode": "wag"},
    "Creature_ChimHangMe": {"eye": (0.122, -0.40, 0.945), "jaw": (0.0, -0.45, 0.88), "tongue": (0.07, (0.86, 0.28, 0.34)), "tail_mode": "float"},
    "Creature_NamDiBo": {"eye": (0.048, -0.088, 0.30), "jaw": None, "tongue": None, "tail_mode": None},
    "Creature_BuomBaoTu": {"eye": (0.042, -0.23, 0.455), "jaw": (0.0, -0.23, 0.42), "tongue": None, "tail_mode": "float"},
    "Creature_EchDocDom": {"eye": (0.095, -0.24, 0.375), "jaw": (0.0, -0.25, 0.26), "tongue": (0.12, (0.86, 0.26, 0.62)), "tail_mode": None},
    "Creature_NamKhongLo": {"eye": (0.10, -0.20, 0.50), "jaw": None, "tongue": None, "tail_mode": None},
    "Creature_OcLuuHuynh": {"eye": (0.045, -0.25, 0.085), "jaw": (0.0, -0.25, 0.05), "tongue": (0.05, (0.88, 0.46, 0.32)), "tail_mode": "wag"},
    "Creature_CuaDaNong": {"eye": (0.075, -0.175, 0.245), "jaw": (0.0, -0.18, 0.16), "tongue": None, "tail_mode": None},
    "Creature_KyNhongLua": {"eye": (0.062, -0.46, 0.285), "jaw": (0.0, -0.48, 0.22), "tongue": (0.10, (0.96, 0.32, 0.18)), "tail_mode": "wag"},
    "Creature_RanDungNham": {"eye": (0.095, -0.72, 0.73), "jaw": (0.0, -0.68, 0.61), "tongue": (0.14, (1.0, 0.46, 0.12)), "tail_mode": "wag"},
    "Creature_DeGio": {"eye": (0.048, -0.22, 0.18), "jaw": (0.0, -0.22, 0.14), "tongue": None, "tail_mode": None},
    "Creature_ChuotGio": {"eye": (0.044, -0.18, 0.16), "jaw": (0.0, -0.20, 0.115), "tongue": (0.05, (0.92, 0.44, 0.52)), "tail_mode": "wag"},
    "Creature_ChonKhe": {"eye": (0.048, -0.46, 0.326), "jaw": (0.0, -0.46, 0.275), "tongue": (0.07, (0.90, 0.34, 0.42)), "tail_mode": "wag"},
    "Creature_CaoGio": {"eye": (0.078, -0.48, 0.610), "jaw": (0.0, -0.50, 0.54), "tongue": (0.08, (0.90, 0.36, 0.46)), "tail_mode": "float"},
}


def _merge_clip_poses(poses, extra_by_frame):
    """Gộp keyframe mới (extra_by_frame: {frame: {bone: dict}}) vào danh sách poses sẵn có."""
    by_f = {}
    for f, pdict in poses:
        by_f.setdefault(f, {})
        for bname, vals in pdict.items():
            by_f[f].setdefault(bname, {}).update(vals)
    for f, pdict in extra_by_frame.items():
        by_f.setdefault(f, {})
        for bname, vals in pdict.items():
            by_f[f].setdefault(bname, {}).update(vals)
    return sorted(by_f.items(), key=lambda kv: kv[0])


def _augment_facial_and_tail_rig(bm, col_layer, mesh_name, bone_specs, clips_spec):
    """Tự động bổ sung xương Mắt (nhắm/mở), Miệng/Hàm (há/ngậm), Lưỡi (thò/thụt) và chuyển động Đuôi (vẫy/lơ lửng)."""
    spec = _SPECIES_FACIAL_SPEC.get(mesh_name)
    if not spec or not bone_specs or not clips_spec:
        return bone_specs, clips_spec

    bones = list(bone_specs)
    existing = {b[0] for b in bones}
    head_parent = "Head" if "Head" in existing else ("Cap" if "Cap" in existing else "Root")

    # 1. Xương Mắt (nhắm/mở độc lập)
    ex, ey, ez = spec["eye"]
    if "Eye_L" not in existing:
        bones.append(("Eye_L", (-ex, ey + 0.015, ez), (-ex, ey - 0.025, ez), head_parent, 0.048, 5.5))
    if "Eye_R" not in existing:
        bones.append(("Eye_R", (ex, ey + 0.015, ez), (ex, ey - 0.025, ez), head_parent, 0.048, 5.5))

    # 2. Xương Miệng / Hàm dưới (há/ngậm)
    jaw = spec.get("jaw")
    has_jaw = jaw is not None
    if has_jaw and "Jaw" not in existing:
        jx, jy, jz = jaw
        bones.append(("Jaw", (jx, jy + 0.03, jz), (jx, jy - 0.07, jz - 0.01), head_parent, 0.085, 4.5))

    # 3. Xương Lưỡi + Hình học Lưỡi 3D nếu chưa có
    tongue = spec.get("tongue")
    has_tongue = tongue is not None
    if has_tongue and jaw is not None:
        tlen, tcol = tongue
        jx, jy, jz = jaw
        if "Tongue" not in existing:
            _add_tube(
                bm, col_layer,
                [(jx, jy + 0.01, jz + 0.015), (jx, jy - tlen * 0.55, jz + 0.012), (jx, jy - tlen, jz + 0.005)],
                [0.016, 0.013, 0.008], sides=8, subdiv=1, mat_index=5,
                color_fn=lambda _p, _n: tcol,
            )
            bones.append(("Tongue", (jx, jy + 0.01, jz + 0.015), (jx, jy - tlen, jz + 0.005), head_parent, 0.055, 6.0))

    # 4. Tìm xương Đuôi sẵn có
    tail_bones = [b[0] for b in bones if b[0] in ("Tail", "Tail1", "Tail2", "Tail_C", "Tail_L", "Tail_R")]
    tail_mode = spec.get("tail_mode")

    # 5. Chèn keyframes vào từng clip (Idle, Walk, Attack/Action, Hit)
    new_clips = {}
    for cname, (sf, ef, poses) in clips_spec.items():
        mid = max(sf + 1, (sf + ef) // 2)
        blink_s = max(sf + 1, int(sf + (ef - sf) * 0.42))
        blink_m = min(ef - 1, blink_s + 2)
        blink_e = min(ef - 1, blink_m + 2)

        extra = {}
        if cname == "Idle":
            extra[sf] = {"Eye_L": {"scale": (1, 1, 1)}, "Eye_R": {"scale": (1, 1, 1)}}
            extra[blink_s] = {"Eye_L": {"scale": (1, 1, 1)}, "Eye_R": {"scale": (1, 1, 1)}}
            extra[blink_m] = {"Eye_L": {"scale": (1, 1, 0.08)}, "Eye_R": {"scale": (1, 1, 0.08)}}
            extra[blink_e] = {"Eye_L": {"scale": (1, 1, 1)}, "Eye_R": {"scale": (1, 1, 1)}}
            extra[ef] = {"Eye_L": {"scale": (1, 1, 1)}, "Eye_R": {"scale": (1, 1, 1)}}
            if has_jaw:
                extra[sf]["Jaw"] = {"rot": (0.04, 0, 0)}
                extra.setdefault(mid, {})["Jaw"] = {"rot": (0.24, 0, 0)}
                extra[ef]["Jaw"] = {"rot": (0.04, 0, 0)}
            if has_tongue:
                extra[sf]["Tongue"] = {"scale": (1, 0.22, 1)}
                extra.setdefault(mid, {})["Tongue"] = {"scale": (1, 0.92, 1)}
                extra[ef]["Tongue"] = {"scale": (1, 0.22, 1)}
        elif cname == "Walk":
            if has_jaw:
                extra[sf] = {"Jaw": {"rot": (0.08, 0, 0)}}
                extra[mid] = {"Jaw": {"rot": (0.20, 0, 0)}}
                extra[ef] = {"Jaw": {"rot": (0.08, 0, 0)}}
            if has_tongue:
                extra.setdefault(sf, {})["Tongue"] = {"scale": (1, 0.25, 1)}
                extra.setdefault(mid, {})["Tongue"] = {"scale": (1, 0.65, 1)}
                extra.setdefault(ef, {})["Tongue"] = {"scale": (1, 0.25, 1)}
        elif cname in ("Attack", "Action"):
            peak = max(sf + 1, int(sf + (ef - sf) * 0.45))
            extra[sf] = {"Eye_L": {"scale": (1, 1, 1)}, "Eye_R": {"scale": (1, 1, 1)}}
            extra[peak] = {"Eye_L": {"scale": (1.12, 1.12, 1.12)}, "Eye_R": {"scale": (1.12, 1.12, 1.12)}}
            extra[ef] = {"Eye_L": {"scale": (1, 1, 1)}, "Eye_R": {"scale": (1, 1, 1)}}
            if has_jaw:
                extra[sf]["Jaw"] = {"rot": (0.05, 0, 0)}
                extra[peak]["Jaw"] = {"rot": (0.48, 0, 0)}
                extra[ef]["Jaw"] = {"rot": (0.05, 0, 0)}
            if has_tongue:
                extra[sf]["Tongue"] = {"scale": (1, 0.20, 1)}
                extra[peak]["Tongue"] = {"scale": (1, 1.38, 1)}
                extra[ef]["Tongue"] = {"scale": (1, 0.20, 1)}
        elif cname == "Hit":
            hit_f = max(sf + 1, int(sf + (ef - sf) * 0.33))
            extra[sf] = {"Eye_L": {"scale": (1, 1, 1)}, "Eye_R": {"scale": (1, 1, 1)}}
            extra[hit_f] = {"Eye_L": {"scale": (1, 1, 0.10)}, "Eye_R": {"scale": (1, 1, 0.10)}}
            extra[ef] = {"Eye_L": {"scale": (1, 1, 1)}, "Eye_R": {"scale": (1, 1, 1)}}
            if has_jaw:
                extra[sf]["Jaw"] = {"rot": (0.04, 0, 0)}
                extra[hit_f]["Jaw"] = {"rot": (0.42, 0, 0)}
                extra[ef]["Jaw"] = {"rot": (0.04, 0, 0)}

        # Đuôi vẫy hoặc lơ lửng tùy loài trong Idle/Walk
        if tail_bones and cname in ("Idle", "Walk"):
            amp = 0.24 if cname == "Walk" else 0.14
            for tb in tail_bones:
                if tail_mode == "wag":
                    extra.setdefault(sf, {}).setdefault(tb, {"rot": (0.0, 0.0, amp)})
                    extra.setdefault(mid, {}).setdefault(tb, {"rot": (0.0, 0.0, -amp)})
                    extra.setdefault(ef, {}).setdefault(tb, {"rot": (0.0, 0.0, amp)})
                elif tail_mode == "float":
                    extra.setdefault(sf, {}).setdefault(tb, {"rot": (amp * 0.7, 0.0, 0.06)})
                    extra.setdefault(mid, {}).setdefault(tb, {"rot": (-amp * 0.7, 0.0, -0.06)})
                    extra.setdefault(ef, {}).setdefault(tb, {"rot": (amp * 0.7, 0.0, 0.06)})

        new_clips[cname] = (sf, ef, _merge_clip_poses(poses, extra))

    return bones, new_clips


def _finalize_and_export(bm, flat_faces, mats, mesh_name, fbx_path, preview_path,
                         bone_specs=None, clips_spec=None, remesh_organic=False):
    if remesh_organic or _WAVE1_AUTO_REMESH:
        _apply_organic_voxel_remesh(bm, flat_faces)
    col_layer = bm.loops.layers.color.get("Col")
    if col_layer is not None and bone_specs and clips_spec:
        bone_specs, clips_spec = _augment_facial_and_tail_rig(bm, col_layer, mesh_name, bone_specs, clips_spec)
    bm.normal_update()
    bm.faces.index_update()
    flat_indices = {f.index for f in flat_faces if f.is_valid}

    mesh = bpy.data.meshes.new(mesh_name)
    bm.to_mesh(mesh)
    bm.free()

    for poly in mesh.polygons:
        poly.use_smooth = (poly.index not in flat_indices)

    obj = bpy.data.objects.new(mesh_name, mesh)
    kc.link(obj)
    for m in mats:
        mesh.materials.append(m)

    # Đặt đáy chạm sát z = 0
    min_z = min(v.co.z for v in mesh.vertices)
    z_shift = min_z if abs(min_z) > 1e-4 else 0.0
    if z_shift != 0.0:
        for v in mesh.vertices:
            v.co.z -= z_shift
        mesh.update()

    if preview_path:
        os.makedirs(os.path.dirname(preview_path), exist_ok=True)
        _render_studio_preview(obj, preview_path)

    arm_obj = None
    if bone_specs and clips_spec:
        arm_obj = _build_rig_and_nla(obj, mesh_name + "_Rig", bone_specs, clips_spec, z_shift=z_shift)

    if fbx_path:
        os.makedirs(os.path.dirname(fbx_path), exist_ok=True)
        if arm_obj is not None:
            bpy.ops.object.select_all(action="DESELECT")
            arm_obj.select_set(True)
            obj.select_set(True)
            bpy.context.view_layer.objects.active = arm_obj
            bpy.ops.export_scene.fbx(
                filepath=fbx_path,
                use_selection=True,
                apply_scale_options="FBX_SCALE_ALL",
                axis_forward="-Z",
                axis_up="Y",
                bake_space_transform=True,
                use_mesh_modifiers=True,
                add_leaf_bones=False,
                bake_anim=True,
                bake_anim_use_all_bones=True,
                bake_anim_use_nla_strips=True,
                bake_anim_use_all_actions=False,
                bake_anim_force_startend_keying=True,
                mesh_smooth_type="FACE",
            )
            print(f"Exported rigged {mesh_name} ({kc.tri_count(obj)} tris, {len(bone_specs)} bones, {len(clips_spec)} clips) -> {fbx_path}", flush=True)
        else:
            kc.export_fbx([obj], fbx_path)
            print(f"Exported {mesh_name} ({kc.tri_count(obj)} tris) -> {fbx_path}", flush=True)


def _render_studio_preview(obj, out_path):
    """Chụp Cycles studio 3/4 nhìn từ trước-phải với 3 đèn điểm & bệ đá ngầm trung tính."""
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 22
    scene.cycles.max_bounces = 4
    scene.cycles.diffuse_bounces = 2
    if hasattr(scene.cycles, "use_denoising"):
        scene.cycles.use_denoising = True
    scene.render.resolution_x, scene.render.resolution_y = 800, 600
    scene.render.filepath = out_path
    scene.render.image_settings.file_format = "PNG"

    world = bpy.data.worlds.new("StudioWorld")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.045, 0.055, 0.072, 1.0)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.75
    scene.world = world

    for o in list(scene.collection.objects):
        if o.type in {"CAMERA", "LIGHT"} or o.name.startswith("_prev"):
            bpy.data.objects.remove(o, do_unlink=True)

    pts = [v.co for v in obj.data.vertices]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    centre = (lo + hi) * 0.5
    size_v = hi - lo
    size = max(size_v.x, size_v.y, size_v.z, 0.35)

    # Đế đá studio tròn êm ở z = 0
    gmesh = bpy.data.meshes.new("_prev_ground")
    gobj = bpy.data.objects.new("_prev_ground", gmesh)
    kc.link(gobj)
    gbm = bmesh.new()
    bmesh.ops.create_circle(gbm, cap_ends=True, radius=size * 1.45, segments=36)
    gbm.to_mesh(gmesh)
    gbm.free()
    gmat = bpy.data.materials.new("_prev_ground_mat")
    gmat.use_nodes = True
    gbsdf = gmat.node_tree.nodes["Principled BSDF"]
    gbsdf.inputs["Base Color"].default_value = (0.08, 0.09, 0.11, 1.0)
    gbsdf.inputs["Roughness"].default_value = 0.85
    gmesh.materials.append(gmat)
    gobj.location = Vector((centre.x, centre.y, lo.z - 0.005))

    # Ánh sáng Studio 3 điểm + Đèn trần nhẹ để rõ chi tiết tối: Key ấm + Fill xanh + Rim tách viền
    lights = [
        ("key", Vector((centre.x - size * 1.4, centre.y - size * 1.7, centre.z + size * 1.5)), (1.0, 0.96, 0.90), 38.0 * size * size),
        ("fill", Vector((centre.x + size * 1.7, centre.y - size * 1.0, centre.z + size * 0.8)), (0.76, 0.88, 1.0), 16.0 * size * size),
        ("rim", Vector((centre.x + size * 0.4, centre.y + size * 1.8, centre.z + size * 1.3)), (0.88, 0.80, 1.0), 22.0 * size * size),
        ("top", Vector((centre.x, centre.y - size * 0.2, centre.z + size * 2.2)), (0.92, 0.94, 1.0), 14.0 * size * size),
    ]
    for lname, lpos, lcol, lpow in lights:
        ldata = bpy.data.lights.new("_prev_" + lname, "AREA")
        ldata.energy = lpow
        ldata.color = lcol
        ldata.size = size * 1.2
        lobj = bpy.data.objects.new("_prev_" + lname, ldata)
        lobj.location = lpos
        lobj.rotation_euler = (centre - lpos).to_track_quat("-Z", "Y").to_euler()
        kc.link(lobj)

    cam_data = bpy.data.cameras.new("_prev_cam")
    cam_data.lens = 42.0
    cam = bpy.data.objects.new("_prev_cam", cam_data)
    kc.link(cam)
    dist = size * 2.15
    cam_pos = centre + Vector((dist * 0.64, -dist * 0.74, size * 0.38))
    cam.location = cam_pos
    cam.rotation_euler = (centre - cam_pos).to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam

    bpy.ops.render.render(write_still=True)


# ================================================================== HANG 1: HANG KHỞI SINH (4 LOÀI)

def build_01_bo_canh_cung_nho(out_dir, prev_dir):
    """[M] Bọ Cánh Cứng Nhỏ: bọ tròn vỏ đồng rêu ẩm bò ven hồ."""
    kc.new_scene()
    mats = _make_creature_materials()
    bm, col, flat = _new_bm()

    def shell_col(p, n):
        sheen = 0.5 + 0.5 * math.sin(p.x * 16.0 + p.y * 10.0)
        base = _lerp3((0.08, 0.16, 0.07), (0.36, 0.44, 0.14), sheen * 0.55 + max(0.0, n.z) * 0.45)
        if abs(p.x) > 0.13:
            base = _lerp3(base, (0.64, 0.48, 0.14), 0.45)
        if abs(p.x) < 0.025 and p.y > -0.05:
            base = _lerp3(base, (0.04, 0.05, 0.03), 0.75)
        return base

    def chitin_dark(p, n):
        return _lerp3((0.08, 0.05, 0.03), (0.24, 0.15, 0.07), max(0.0, n.z) * 0.6 + 0.25)

    # Bụng & Ngực trước + rêu nhỏ bám trên mai
    _add_ellipsoid(bm, col, (0, 0.06, 0.13), (0.20, 0.25, 0.10), segs=18, rings=12, mat_index=1, color_fn=chitin_dark)
    _add_ellipsoid(bm, col, (0, -0.18, 0.16), (0.17, 0.11, 0.10), segs=18, rings=12, rot=(0.05, 0.0, 0.12), mat_index=1, color_fn=shell_col)
    for sx in (-1.0, 1.0):
        _add_ellipsoid(
            bm, col, (sx * 0.068, 0.09, 0.21), (0.145, 0.24, 0.12),
            segs=18, rings=12, rot=(0.10, sx * 0.18, sx * -0.06), mat_index=1, color_fn=shell_col
        )
    _add_moss_tuft(bm, col, (-0.08, 0.14, 0.29), scale=0.036)
    # Đầu hơi nghiêng nhìn sang phải tự nhiên
    _add_ellipsoid(bm, col, (0.02, -0.28, 0.145), (0.11, 0.09, 0.075), segs=16, rings=10, rot=(0.06, 0.08, -0.18), mat_index=1, color_fn=chitin_dark)
    for sx in (-1.0, 1.0):
        _add_eye_bulb(bm, col, (sx * 0.082 + 0.02, -0.31, 0.17), 0.024, yaw=sx * 0.45 - 0.18, eye_col=(0.94, 0.56, 0.08))
        _add_tube(bm, col, [(sx * 0.045 + 0.02, -0.34, 0.125), (sx * 0.068 + 0.03, -0.41, 0.13), (sx * 0.025 + 0.03, -0.45, 0.135)], [0.020, 0.014, 0.004], sides=8, subdiv=1, mat_index=1, color_fn=chitin_dark)
        _add_tube(bm, col, [(sx * 0.06 + 0.02, -0.34, 0.175), (sx * 0.14 + 0.03, -0.42, 0.235), (sx * 0.21 + 0.03, -0.43, 0.27)], [0.010, 0.007, 0.004], sides=6, subdiv=1, mat_index=1, color_fn=chitin_dark)

    # 6 chân bước so le sinh động (chân trước phải nhấc nhẹ)
    legs = [(-1, -0.16, 0.22, 0.26, 0.006), (1, -0.12, 0.23, 0.27, 0.028), (-1, 0.02, 0.27, 0.31, 0.006), (1, 0.04, 0.27, 0.31, 0.006), (-1, 0.18, 0.25, 0.29, 0.006), (1, 0.16, 0.25, 0.29, 0.006)]
    for sx, py, spread, reach, fz in legs:
        _add_tube(
            bm, col,
            [(sx * 0.12, py, 0.11), (sx * spread, py - 0.02, 0.17 + fz * 0.5), (sx * (reach - 0.02), py + 0.03, 0.08 + fz), (sx * reach, py + 0.04, fz)],
            [0.024, 0.018, 0.013, 0.006], sides=8, subdiv=1, mat_index=1, color_fn=chitin_dark
        )

    bones_01 = [
        ("Root", (0, 0.06, 0.16), (0, -0.14, 0.16), None, 0.34, 1.0),
        ("Head", (0.01, -0.22, 0.15), (0.02, -0.34, 0.145), "Root", 0.14, 2.2),
        ("Ant_L", (-0.04, -0.34, 0.175), (-0.18, -0.43, 0.27), "Head", 0.08, 2.6),
        ("Ant_R", (0.08, -0.34, 0.175), (0.24, -0.43, 0.27), "Head", 0.08, 2.6),
        ("Elytra_L", (-0.02, -0.05, 0.22), (-0.09, 0.24, 0.20), "Root", 0.18, 1.8),
        ("Elytra_R", (0.02, -0.05, 0.22), (0.09, 0.24, 0.20), "Root", 0.18, 1.8),
        ("Leg_FL", (-0.12, -0.16, 0.11), (-0.26, -0.12, 0.01), "Root", 0.11, 2.5),
        ("Leg_FR", (0.12, -0.12, 0.11), (0.27, -0.08, 0.03), "Root", 0.11, 2.5),
        ("Leg_ML", (-0.12, 0.02, 0.11), (-0.31, 0.06, 0.01), "Root", 0.11, 2.5),
        ("Leg_MR", (0.12, 0.04, 0.11), (0.31, 0.08, 0.01), "Root", 0.11, 2.5),
        ("Leg_BL", (-0.12, 0.18, 0.11), (-0.29, 0.22, 0.01), "Root", 0.11, 2.5),
        ("Leg_BR", (0.12, 0.16, 0.11), (0.29, 0.20, 0.01), "Root", 0.11, 2.5),
    ]
    clips_01 = {
        "Idle": (1, 48, [
            (1, {"Head": {"rot": (0.04, 0, 0.08)}, "Ant_L": {"rot": (0.12, 0, 0.10)}, "Ant_R": {"rot": (-0.10, 0, -0.12)}, "Root": {"loc": (0, 0, 0)}}),
            (16, {"Head": {"rot": (-0.06, 0, -0.12)}, "Ant_L": {"rot": (-0.18, 0, -0.15)}, "Ant_R": {"rot": (0.16, 0, 0.15)}, "Root": {"loc": (0, 0, 0.012)}}),
            (32, {"Head": {"rot": (0.08, 0, 0.10)}, "Ant_L": {"rot": (0.15, 0, -0.08)}, "Ant_R": {"rot": (-0.14, 0, 0.12)}, "Root": {"loc": (0, 0, 0.006)}}),
            (48, {"Head": {"rot": (0.04, 0, 0.08)}, "Ant_L": {"rot": (0.12, 0, 0.10)}, "Ant_R": {"rot": (-0.10, 0, -0.12)}, "Root": {"loc": (0, 0, 0)}}),
        ]),
        "Walk": (1, 20, [
            (1, {"Root": {"rot": (0, 0.06, 0.05), "loc": (0, 0, 0.015)}, "Leg_FL": {"rot": (0.45, 0, 0.22)}, "Leg_MR": {"rot": (0.45, 0, -0.22)}, "Leg_BL": {"rot": (0.45, 0, 0.22)}, "Leg_FR": {"rot": (-0.38, 0, 0)}, "Leg_ML": {"rot": (-0.38, 0, 0)}, "Leg_BR": {"rot": (-0.38, 0, 0)}}),
            (10, {"Root": {"rot": (0, -0.06, -0.05), "loc": (0, 0, 0.015)}, "Leg_FL": {"rot": (-0.38, 0, 0)}, "Leg_MR": {"rot": (-0.38, 0, 0)}, "Leg_BL": {"rot": (-0.38, 0, 0)}, "Leg_FR": {"rot": (0.45, 0, -0.22)}, "Leg_ML": {"rot": (0.45, 0, 0.22)}, "Leg_BR": {"rot": (0.45, 0, -0.22)}}),
            (20, {"Root": {"rot": (0, 0.06, 0.05), "loc": (0, 0, 0.015)}, "Leg_FL": {"rot": (0.45, 0, 0.22)}, "Leg_MR": {"rot": (0.45, 0, -0.22)}, "Leg_BL": {"rot": (0.45, 0, 0.22)}, "Leg_FR": {"rot": (-0.38, 0, 0)}, "Leg_ML": {"rot": (-0.38, 0, 0)}, "Leg_BR": {"rot": (-0.38, 0, 0)}}),
        ]),
        "Hit": (1, 16, [
            (1, {"Root": {"rot": (0, 0, 0)}, "Elytra_L": {"rot": (0, 0, 0)}, "Elytra_R": {"rot": (0, 0, 0)}}),
            (5, {"Root": {"rot": (0.20, 0, 0.22)}, "Head": {"rot": (0.32, 0, 0)}, "Elytra_L": {"rot": (0.18, 0, -0.28)}, "Elytra_R": {"rot": (0.18, 0, 0.28)}}),
            (16, {"Root": {"rot": (0, 0, 0)}, "Head": {"rot": (0, 0, 0)}, "Elytra_L": {"rot": (0, 0, 0)}, "Elytra_R": {"rot": (0, 0, 0)}}),
        ]),
    }

    _finalize_and_export(
        bm, flat, mats, "Creature_BoCanhCungNho",
        os.path.join(out_dir, "Creature_BoCanhCungNho.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_01_bo_canh_cung_nho.png") if prev_dir else None,
        bone_specs=bones_01, clips_spec=clips_01,
    )


def build_02_oc_ho(out_dir, prev_dir):
    """[M] Ốc Hồ: vỏ xoắn ốc có vân vôi rêu, thân mềm và cặp râu mắt vươn cao."""
    kc.new_scene()
    mats = _make_creature_materials()
    bm, col, flat = _new_bm()

    def body_col(p, n):
        mottle = 0.5 + 0.5 * math.sin(p.y * 22.0 + p.x * 16.0)
        base = _lerp3((0.18, 0.30, 0.32), (0.46, 0.56, 0.50), 0.35 + 0.55 * mottle)
        if p.z < 0.04:
            base = _lerp3(base, (0.62, 0.64, 0.54), 0.5)
        return base

    def shell_col(p, n):
        band = 0.5 + 0.5 * math.sin(p.z * 32.0 + p.y * 18.0)
        stripe = 0.5 + 0.5 * math.cos(p.x * 22.0 + p.z * 20.0)
        base = _lerp3((0.40, 0.28, 0.14), (0.70, 0.58, 0.36), band * 0.65 + stripe * 0.35)
        if n.z > 0.42:
            moss = _clamp01((n.z - 0.42) * 1.5 * (0.4 + 0.6 * math.cos(p.x * 12.0 - p.y * 10.0)))
            base = _lerp3(base, (0.16, 0.36, 0.12), moss * 0.7)
        return base

    # Thân ốc uốn lượn mềm mại bất đối xứng & râu mắt cong sinh động
    _add_tube(
        bm, col,
        [(-0.03, -0.34, 0.045), (-0.01, -0.18, 0.068), (0.0, 0.04, 0.075), (0.02, 0.24, 0.055), (-0.02, 0.39, 0.028)],
        [(0.10, 0.045), (0.135, 0.070), (0.145, 0.080), (0.10, 0.055), (0.04, 0.024)],
        sides=18, subdiv=2, mat_index=0, color_fn=body_col
    )
    _add_tube(bm, col, [(-0.01, -0.22, 0.09), (-0.02, -0.30, 0.14), (-0.03, -0.35, 0.15)], [0.085, 0.075, 0.062], sides=16, subdiv=2, mat_index=0, color_fn=body_col)
    for sx, sway in [(-1.0, -0.02), (1.0, 0.03)]:
        st_tip = (sx * 0.095 + sway - 0.03, -0.45, 0.32 if sx > 0 else 0.29)
        _add_tube(bm, col, [(sx * 0.04 - 0.02, -0.33, 0.17), (sx * 0.08 - 0.02, -0.40, 0.26), st_tip], [0.020, 0.014, 0.013], sides=8, subdiv=2, mat_index=0, color_fn=body_col)
        _add_eye_bulb(bm, col, st_tip, 0.022, yaw=sx * 0.25, eye_col=(0.12, 0.08, 0.05), mat_eye=1)
        _add_tube(bm, col, [(sx * 0.035 - 0.02, -0.35, 0.11), (sx * 0.075 - 0.03, -0.43, 0.07)], [0.014, 0.006], sides=6, subdiv=1, mat_index=0, color_fn=body_col)

    # Vỏ xoắn + cụm rêu hồ bám trên đỉnh vỏ
    steps = 64
    pts, rads = [], []
    for i in range(steps):
        t = i / (steps - 1)
        ang = t * 2.65 * 2.0 * math.pi
        r_tube = 0.088 * ((1.0 - t) ** 0.92) + 0.008
        r_orbit = 0.125 * ((1.0 - t) ** 0.88) + 0.004
        px = 0.02 + 0.16 * (t ** 1.15) + math.cos(ang) * r_orbit * 0.22
        py = 0.04 - math.cos(ang) * r_orbit * 0.92
        pz = 0.24 + math.sin(ang) * r_orbit * 0.92 + 0.04 * t
        rib = 1.0 + 0.065 * math.sin(i * 2.1)
        pts.append((px, py, pz))
        rads.append(r_tube * rib)
    _add_tube(bm, col, pts, rads, sides=18, subdiv=0, mat_index=0, color_fn=shell_col)
    _add_ellipsoid(bm, col, (0.01, -0.07, 0.20), (0.115, 0.105, 0.110), segs=16, rings=10, rot=(0.20, 0.0, 0.0), mat_index=0, color_fn=shell_col)
    _add_ellipsoid(bm, col, (0.18, 0.04, 0.28), (0.042, 0.042, 0.042), segs=10, rings=6, mat_index=0, color_fn=shell_col)
    _add_moss_tuft(bm, col, (0.02, 0.04, 0.42), scale=0.042)

    bones_02 = [
        ("Root", (0.0, 0.04, 0.06), (0.0, -0.18, 0.06), None, 0.30, 1.0),
        ("Shell", (0.02, 0.04, 0.22), (0.14, 0.04, 0.36), "Root", 0.28, 2.0),
        ("Head", (-0.01, -0.22, 0.09), (-0.03, -0.35, 0.15), "Root", 0.14, 2.2),
        ("EyeStalk_L", (-0.04, -0.33, 0.17), (-0.11, -0.45, 0.29), "Head", 0.09, 2.8),
        ("EyeStalk_R", (0.04, -0.33, 0.17), (0.10, -0.45, 0.32), "Head", 0.09, 2.8),
        ("Tail", (0.0, 0.18, 0.05), (-0.02, 0.39, 0.03), "Root", 0.14, 2.0),
    ]
    clips_02 = {
        "Idle": (1, 56, [
            (1, {"Head": {"rot": (0.04, 0, 0.06)}, "EyeStalk_L": {"rot": (0.10, 0, 0.12)}, "EyeStalk_R": {"rot": (-0.08, 0, -0.10)}, "Shell": {"rot": (0.03, 0, 0)}}),
            (20, {"Head": {"rot": (-0.08, 0, -0.12)}, "EyeStalk_L": {"rot": (-0.16, 0, -0.18)}, "EyeStalk_R": {"rot": (0.14, 0, 0.16)}, "Shell": {"rot": (-0.04, 0.04, 0)}}),
            (40, {"Head": {"rot": (0.02, 0, 0.10)}, "EyeStalk_L": {"rot": (0.14, 0, 0.08)}, "EyeStalk_R": {"rot": (-0.14, 0, -0.16)}, "Shell": {"rot": (0.02, -0.04, 0)}}),
            (56, {"Head": {"rot": (0.04, 0, 0.06)}, "EyeStalk_L": {"rot": (0.10, 0, 0.12)}, "EyeStalk_R": {"rot": (-0.08, 0, -0.10)}, "Shell": {"rot": (0.03, 0, 0)}}),
        ]),
        "Walk": (1, 40, [
            (1, {"Root": {"loc": (0, -0.02, 0)}, "Head": {"loc": (0, -0.03, 0)}, "Shell": {"rot": (0.08, 0.05, 0)}, "Tail": {"loc": (0, 0.02, 0)}}),
            (20, {"Root": {"loc": (0, 0.02, 0.012)}, "Head": {"loc": (0, 0.01, 0)}, "Shell": {"rot": (-0.08, -0.05, 0)}, "Tail": {"loc": (0, -0.02, 0)}}),
            (40, {"Root": {"loc": (0, -0.02, 0)}, "Head": {"loc": (0, -0.03, 0)}, "Shell": {"rot": (0.08, 0.05, 0)}, "Tail": {"loc": (0, 0.02, 0)}}),
        ]),
        "Retract": (1, 32, [
            (1, {"Head": {"loc": (0, 0, 0)}, "EyeStalk_L": {"scale": (1, 1, 1)}, "EyeStalk_R": {"scale": (1, 1, 1)}, "Shell": {"loc": (0, 0, 0)}}),
            (10, {"Head": {"loc": (0, 0.18, -0.02), "scale": (0.65, 0.65, 0.65)}, "EyeStalk_L": {"scale": (0.35, 0.35, 0.35)}, "EyeStalk_R": {"scale": (0.35, 0.35, 0.35)}, "Shell": {"loc": (0, 0, -0.06), "rot": (-0.16, 0, 0)}}),
            (32, {"Head": {"loc": (0, 0.18, -0.02), "scale": (0.65, 0.65, 0.65)}, "EyeStalk_L": {"scale": (0.35, 0.35, 0.35)}, "EyeStalk_R": {"scale": (0.35, 0.35, 0.35)}, "Shell": {"loc": (0, 0, -0.06), "rot": (-0.16, 0, 0)}}),
        ]),
        "Hit": (1, 18, [
            (1, {"Shell": {"rot": (0, 0, 0)}}),
            (6, {"Shell": {"rot": (-0.22, 0, 0.18), "loc": (0, 0, -0.04)}, "EyeStalk_L": {"scale": (0.45, 0.45, 0.45)}, "EyeStalk_R": {"scale": (0.45, 0.45, 0.45)}}),
            (18, {"Shell": {"rot": (0, 0, 0)}, "EyeStalk_L": {"scale": (1, 1, 1)}, "EyeStalk_R": {"scale": (1, 1, 1)}}),
        ]),
    }

    _finalize_and_export(
        bm, flat, mats, "Creature_OcHo",
        os.path.join(out_dir, "Creature_OcHo.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_02_oc_ho.png") if prev_dir else None,
        bone_specs=bones_02, clips_spec=clips_02,
    )


def build_03_than_lan_bo_ho(out_dir, prev_dir):
    """[S] Thằn Lằn Bờ Hồ: bò sát săn mồi dáng rình, mào gai lưng và lưỡi dài phóng ra."""
    kc.new_scene()
    mats = _make_creature_materials()
    bm, col, flat = _new_bm()

    def lizard_skin(p, n):
        band = 0.5 + 0.5 * math.sin(p.y * 14.0 + p.x * 8.0)
        spot = math.sin(p.x * 34.0) * math.cos(p.y * 28.0)
        dark = (0.09, 0.17, 0.09)
        olive = (0.24, 0.42, 0.16)
        belly = (0.64, 0.58, 0.36)
        c = _lerp3(dark, olive, band * 0.7 + 0.2)
        if spot > 0.65 and n.z > 0.05:
            c = _lerp3(c, (0.22, 0.68, 0.54), 0.55)
        if n.z < -0.10:
            c = _lerp3(c, belly, _clamp01((-n.z - 0.10) * 1.5))
        elif n.z > 0.68:
            c = _lerp3(c, (0.44, 0.58, 0.18), 0.50)
        return c

    # Xương sống chữ S mềm mại (subdiv=2), đầu ngoái nhìn rình mồi
    spine_pts = [
        (0.08, -0.78, 0.27), (0.05, -0.62, 0.28), (0.01, -0.45, 0.25),
        (-0.04, -0.22, 0.23), (-0.05, 0.02, 0.23), (0.0, 0.26, 0.21),
        (0.10, 0.48, 0.16), (0.24, 0.66, 0.11), (0.16, 0.84, 0.07), (0.02, 0.94, 0.05),
    ]
    spine_rads = [
        (0.055, 0.042), (0.105, 0.082), (0.092, 0.078),
        (0.135, 0.105), (0.148, 0.112), (0.115, 0.095),
        (0.075, 0.068), (0.048, 0.045), (0.028, 0.028), (0.010, 0.010),
    ]
    _add_tube(bm, col, spine_pts, spine_rads, sides=18, subdiv=2, mat_index=0, color_fn=lizard_skin)
    _add_ellipsoid(bm, col, (0.06, -0.68, 0.20), (0.078, 0.12, 0.032), segs=14, rings=6, rot=(0.14, 0, -0.18), mat_index=0, color_fn=lizard_skin)
    _add_tube(
        bm, col,
        [(0.07, -0.71, 0.22), (0.11, -0.85, 0.21), (0.14, -0.97, 0.18), (0.10, -1.05, 0.16)],
        [0.020, 0.015, 0.011, 0.016], sides=10, subdiv=2, mat_index=5,
        color_fn=lambda _p, _n: (0.82, 0.16, 0.26),
    )

    for k in range(1, len(spine_pts) - 2):
        bp = Vector(spine_pts[k])
        rz = spine_rads[k][1]
        h_fin = 0.075 * (1.0 - k / len(spine_pts) * 0.55)
        _add_crystal_prism(
            bm, col, bp + Vector((0, 0, rz * 0.62)),
            Vector((0, 0.35, 0.94)), h_fin, 0.022, 0.010, sides=4, mat_index=5,
            color_fn=lambda _p, _n: (0.36, 0.52, 0.16), flat_faces=flat
        )

    for sx in (-1.0, 1.0):
        _add_eye_bulb(bm, col, (0.05 + sx * 0.078, -0.64, 0.32), 0.028, yaw=sx * 0.48 - 0.18, brow_col=lizard_skin, eye_col=(0.98, 0.72, 0.10))

    # 4 chân bước rình mồi (chân trước phải nhấc lên rình)
    limb_specs = [
        (-1.0, -0.34, 0.23, -0.44, 0.070, 0.022), (1.0, -0.30, 0.23, -0.32, 0.070, 0.075),
        (-1.0, 0.20, 0.21, 0.14, 0.075, 0.022), (1.0, 0.22, 0.21, 0.16, 0.075, 0.022),
    ]
    for sx, sy, sz, fy, r0, fz in limb_specs:
        _add_amphibian_limb(
            bm, col,
            [(sx * 0.10, sy, sz), (sx * 0.25, sy - 0.04, sz + fz * 0.2), (sx * 0.29, fy - 0.08, fz)],
            [r0, r0 * 0.72, r0 * 0.58], toe_count=4, toe_len=0.075, webbed=True, mat_index=0, color_fn=lizard_skin
        )

    bones_03 = [
        ("Root", (-0.04, 0.02, 0.23), (-0.02, -0.24, 0.23), None, 0.42, 1.0),
        ("Neck", (0.01, -0.40, 0.24), (0.04, -0.58, 0.27), "Root", 0.20, 1.8),
        ("Head", (0.04, -0.58, 0.27), (0.08, -0.78, 0.27), "Neck", 0.20, 2.3),
        ("Tongue", (0.07, -0.71, 0.22), (0.10, -1.05, 0.16), "Head", 0.10, 3.0),
        ("Tail1", (0.0, 0.26, 0.21), (0.20, 0.62, 0.12), "Root", 0.24, 1.8),
        ("Tail2", (0.20, 0.62, 0.12), (0.02, 0.94, 0.05), "Tail1", 0.22, 2.2),
        ("Leg_FL", (-0.10, -0.34, 0.23), (-0.29, -0.52, 0.02), "Root", 0.18, 2.4),
        ("Leg_FR", (0.10, -0.30, 0.23), (0.29, -0.40, 0.07), "Root", 0.18, 2.4),
        ("Leg_BL", (-0.10, 0.20, 0.21), (-0.29, 0.06, 0.02), "Root", 0.18, 2.4),
        ("Leg_BR", (0.10, 0.22, 0.21), (0.29, 0.08, 0.02), "Root", 0.18, 2.4),
    ]
    clips_03 = {
        "Idle": (1, 56, [
            (1, {"Neck": {"rot": (0.06, 0, 0.10)}, "Head": {"rot": (-0.05, 0, 0.14)}, "Tongue": {"scale": (1, 0.3, 1)}, "Tail1": {"rot": (0, 0, 0.16)}, "Tail2": {"rot": (0, 0, -0.22)}}),
            (28, {"Neck": {"rot": (-0.06, 0, -0.14)}, "Head": {"rot": (0.06, 0, -0.18)}, "Tongue": {"scale": (1, 1.15, 1)}, "Tail1": {"rot": (0, 0, -0.16)}, "Tail2": {"rot": (0, 0, 0.24)}}),
            (56, {"Neck": {"rot": (0.06, 0, 0.10)}, "Head": {"rot": (-0.05, 0, 0.14)}, "Tongue": {"scale": (1, 0.3, 1)}, "Tail1": {"rot": (0, 0, 0.16)}, "Tail2": {"rot": (0, 0, -0.22)}}),
        ]),
        "Walk": (1, 24, [
            (1, {"Neck": {"rot": (0, 0, 0.26)}, "Head": {"rot": (0, 0, -0.20)}, "Leg_FL": {"rot": (0.55, 0, 0)}, "Leg_BR": {"rot": (0.55, 0, 0)}, "Leg_FR": {"rot": (-0.45, 0, 0)}, "Leg_BL": {"rot": (-0.45, 0, 0)}, "Tail1": {"rot": (0, 0, -0.35)}, "Tail2": {"rot": (0, 0, 0.45)}}),
            (12, {"Neck": {"rot": (0, 0, -0.26)}, "Head": {"rot": (0, 0, 0.20)}, "Leg_FL": {"rot": (-0.45, 0, 0)}, "Leg_BR": {"rot": (-0.45, 0, 0)}, "Leg_FR": {"rot": (0.55, 0, 0)}, "Leg_BL": {"rot": (0.55, 0, 0)}, "Tail1": {"rot": (0, 0, 0.35)}, "Tail2": {"rot": (0, 0, -0.45)}}),
            (24, {"Neck": {"rot": (0, 0, 0.26)}, "Head": {"rot": (0, 0, -0.20)}, "Leg_FL": {"rot": (0.55, 0, 0)}, "Leg_BR": {"rot": (0.55, 0, 0)}, "Leg_FR": {"rot": (-0.45, 0, 0)}, "Leg_BL": {"rot": (-0.45, 0, 0)}, "Tail1": {"rot": (0, 0, -0.35)}, "Tail2": {"rot": (0, 0, 0.45)}}),
        ]),
        "Attack": (1, 24, [
            (1, {"Neck": {"rot": (0.14, 0, 0)}, "Head": {"rot": (-0.10, 0, 0)}, "Tongue": {"scale": (1, 0.25, 1)}}),
            (6, {"Root": {"loc": (0, 0.14, 0.05)}, "Neck": {"rot": (-0.22, 0, 0)}, "Head": {"rot": (-0.18, 0, 0)}, "Tongue": {"scale": (1, 0.25, 1)}}),
            (11, {"Root": {"loc": (0, -0.26, 0.02)}, "Neck": {"rot": (0.18, 0, 0)}, "Head": {"rot": (0.22, 0, 0)}, "Tongue": {"scale": (1.2, 2.8, 1.2)}}),
            (18, {"Root": {"loc": (0, -0.10, 0)}, "Tongue": {"scale": (1, 0.6, 1)}}),
            (24, {"Root": {"loc": (0, 0, 0)}, "Neck": {"rot": (0, 0, 0)}, "Head": {"rot": (0, 0, 0)}, "Tongue": {"scale": (1, 0.3, 1)}}),
        ]),
        "Hit": (1, 18, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (6, {"Root": {"rot": (0, 0, 0.35)}, "Head": {"rot": (-0.30, 0, 0)}, "Tail1": {"rot": (0, 0, -0.32)}}),
            (18, {"Root": {"rot": (0, 0, 0)}, "Head": {"rot": (0, 0, 0)}, "Tail1": {"rot": (0, 0, 0)}}),
        ]),
    }

    _finalize_and_export(
        bm, flat, mats, "Creature_ThanLanBoHo",
        os.path.join(out_dir, "Creature_ThanLanBoHo.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_03_than_lan_bo_ho.png") if prev_dir else None,
        bone_specs=bones_03, clips_spec=clips_03,
    )


def build_04_coc_ho_gia(out_dir, prev_dir):
    """[C] Cóc Hồ Già (Chúa Hang): cóc cổ đại khổng lồ, u sần, lưng mọc rêu đá, bầu cổ phồng lớn."""
    kc.new_scene()
    mats = _make_creature_materials()
    bm, col, flat = _new_bm()
    rng = random.Random(104)

    def toad_skin(p, n):
        wart = 0.5 + 0.5 * math.sin(p.x * 16.0 + p.y * 14.0) * math.cos(p.z * 18.0)
        dark = (0.12, 0.13, 0.08)
        mid = (0.24, 0.28, 0.14)
        belly = (0.44, 0.40, 0.26)
        c = _lerp3(dark, mid, 0.35 + 0.65 * wart)
        if n.z < -0.1:
            c = _lerp3(c, belly, _clamp01((-n.z - 0.1) * 1.4))
        elif n.z > 0.58:
            moss = 0.4 + 0.5 * math.sin(p.x * 5.0 - p.y * 4.5)
            c = _lerp3(c, (0.15, 0.34, 0.12), _clamp01(moss))
        return c

    # Thân cóc đồ sộ ngồi xổm + bầu cổ họng phồng to đặc trưng "Ợ Chấn"
    _add_ellipsoid(
        bm, col, (0, 0.08, 0.52), (0.68, 0.72, 0.44), segs=22, rings=14,
        rot=(0.30, 0.0, 0.0), mat_index=0, color_fn=toad_skin,
        warp_fn=lambda p: (p.x * (1.0 + 0.04 * math.sin(p.z * 9.0)), p.y, p.z * (0.95 - 0.15 * max(0.0, -p.z)))
    )
    # Đầu bẹt rộng & bầu cổ phồng
    _add_ellipsoid(bm, col, (0, -0.52, 0.66), (0.46, 0.34, 0.25), segs=18, rings=10, mat_index=0, color_fn=toad_skin)
    _add_ellipsoid(bm, col, (0, -0.58, 0.38), (0.40, 0.32, 0.24), segs=16, rings=10, mat_index=0,
                   color_fn=lambda p, n: _lerp3((0.46, 0.38, 0.22), (0.58, 0.48, 0.28), max(0.0, -n.y)))

    # Gờ mày cao & đôi mắt vàng hổ phách lồi rõ về phía trước
    for sx in (-1.0, 1.0):
        _add_tube(bm, col, [(sx * 0.14, -0.44, 0.86), (sx * 0.28, -0.56, 0.91), (sx * 0.34, -0.66, 0.84)], [0.09, 0.09, 0.065], sides=10, subdiv=1, mat_index=0, color_fn=toad_skin)
        _add_eye_bulb(bm, col, (sx * 0.30, -0.64, 0.85), 0.095, yaw=sx * 0.55, brow_col=toad_skin, eye_col=(0.98, 0.68, 0.08))
        # Tuyến nọc lớn sau tai
        _add_ellipsoid(bm, col, (sx * 0.36, -0.26, 0.76), (0.14, 0.22, 0.10), segs=12, rings=8, rot=(0.2, sx * -0.25, sx * 0.25), mat_index=0, color_fn=toad_skin)

    # Tảng đá rêu & nấm phát sáng nhỏ mọc trên lưng cóc già
    for rx, ry, rz, sx_v, sy_v, sz_v in [(0.0, 0.12, 0.92, 0.32, 0.36, 0.14), (-0.28, 0.22, 0.84, 0.22, 0.24, 0.12), (0.28, 0.18, 0.85, 0.20, 0.22, 0.11)]:
        _add_faceted_chunk(
            bm, col, (rx, ry, rz), (sx_v, sy_v, sz_v), rx * 5.0 + ry, (0.1, 0.1, rng.uniform(0, 3.0)),
            mat_index=0, color_fn=lambda p, n: _lerp3((0.28, 0.30, 0.25), (0.14, 0.32, 0.11), max(0.0, n.z)), flat_faces=flat
        )
    _add_moss_tuft(bm, col, (-0.16, 0.18, 0.96), scale=0.085, rng=rng)
    _add_moss_tuft(bm, col, (0.18, 0.26, 0.92), scale=0.075, rng=rng)
    _add_luma_bud(bm, col, (-0.14, 0.14, 1.02), Vector((-0.2, 0.1, 1.0)), size=0.048, hue=0.32)
    _add_luma_bud(bm, col, (0.20, 0.24, 0.97), Vector((0.25, 0.15, 1.0)), size=0.036, hue=0.28)
    for _ in range(18):
        ang = rng.uniform(-2.2, 2.2)
        wx = math.sin(ang) * rng.uniform(0.18, 0.54)
        wy = rng.uniform(-0.20, 0.56)
        wz = 0.78 - 0.18 * (abs(wx) ** 1.3)
        _add_ellipsoid(bm, col, (wx, wy, wz), (0.065, 0.065, 0.045), segs=8, rings=5, mat_index=0, color_fn=toad_skin)

    # 2 chân trước chống vững + 2 đùi sau cơ bắp gập lại
    for sx in (-1.0, 1.0):
        _add_amphibian_limb(
            bm, col,
            [(sx * 0.44, -0.34, 0.44), (sx * 0.54, -0.44, 0.24), (sx * 0.46, -0.52, 0.045)],
            [0.16, 0.13, 0.11], toe_count=4, toe_len=0.14, webbed=False, mat_index=0, color_fn=toad_skin
        )
        _add_ellipsoid(bm, col, (sx * 0.54, 0.36, 0.32), (0.24, 0.34, 0.22), segs=14, rings=8, rot=(-0.25, 0, sx * 0.35), mat_index=0, color_fn=toad_skin)
        _add_amphibian_limb(
            bm, col,
            [(sx * 0.62, 0.22, 0.26), (sx * 0.72, 0.48, 0.14), (sx * 0.66, 0.36, 0.04)],
            [0.14, 0.11, 0.10], toe_count=4, toe_len=0.20, webbed=True, mat_index=0, color_fn=toad_skin
        )

    bones_04 = [
        ("Root", (0, 0.08, 0.52), (0, -0.24, 0.54), None, 0.90, 1.0),
        ("Head", (0, -0.44, 0.64), (0, -0.78, 0.68), "Root", 0.45, 1.8),
        ("ThroatSac", (0, -0.50, 0.40), (0, -0.70, 0.34), "Head", 0.38, 2.4),
        ("Leg_FL", (-0.44, -0.34, 0.44), (-0.46, -0.52, 0.04), "Root", 0.32, 2.4),
        ("Leg_FR", (0.44, -0.34, 0.44), (0.46, -0.52, 0.04), "Root", 0.32, 2.4),
        ("Leg_BL", (-0.60, 0.22, 0.28), (-0.66, 0.38, 0.04), "Root", 0.36, 2.4),
        ("Leg_BR", (0.60, 0.22, 0.28), (0.66, 0.38, 0.04), "Root", 0.36, 2.4),
    ]
    clips_04 = {
        "Idle": (1, 60, [
            (1, {"Root": {"loc": (0, 0, 0)}, "ThroatSac": {"scale": (1, 1, 1)}, "Head": {"rot": (0.04, 0, 0)}}),
            (25, {"Root": {"loc": (0, 0, 0.03)}, "ThroatSac": {"scale": (1.22, 1.24, 1.22)}, "Head": {"rot": (-0.06, 0, 0.08)}}),
            (45, {"Root": {"loc": (0, 0, 0.01)}, "ThroatSac": {"scale": (0.94, 0.94, 0.94)}, "Head": {"rot": (0.02, 0, -0.06)}}),
            (60, {"Root": {"loc": (0, 0, 0)}, "ThroatSac": {"scale": (1, 1, 1)}, "Head": {"rot": (0.04, 0, 0)}}),
        ]),
        "Walk": (1, 32, [
            (1, {"Root": {"loc": (0, 0, 0), "rot": (0, 0.06, 0)}, "Leg_FL": {"rot": (0.45, 0, 0)}, "Leg_BR": {"rot": (0.45, 0, 0)}, "Leg_FR": {"rot": (-0.35, 0, 0)}, "Leg_BL": {"rot": (-0.35, 0, 0)}}),
            (8, {"Root": {"loc": (0, -0.10, 0.14), "rot": (-0.12, 0, 0)}, "Leg_FL": {"rot": (-0.35, 0, 0)}, "Leg_FR": {"rot": (-0.35, 0, 0)}, "Leg_BL": {"rot": (0.45, 0, 0)}, "Leg_BR": {"rot": (0.45, 0, 0)}}),
            (16, {"Root": {"loc": (0, 0, 0), "rot": (0.06, -0.06, 0)}, "Leg_FL": {"rot": (-0.35, 0, 0)}, "Leg_BR": {"rot": (-0.35, 0, 0)}, "Leg_FR": {"rot": (0.45, 0, 0)}, "Leg_BL": {"rot": (0.45, 0, 0)}}),
            (32, {"Root": {"loc": (0, 0, 0), "rot": (0, 0.06, 0)}, "Leg_FL": {"rot": (0.45, 0, 0)}, "Leg_BR": {"rot": (0.45, 0, 0)}, "Leg_FR": {"rot": (-0.35, 0, 0)}, "Leg_BL": {"rot": (-0.35, 0, 0)}}),
        ]),
        "Warn": (1, 28, [
            (1, {"ThroatSac": {"scale": (1, 1, 1)}, "Head": {"rot": (0, 0, 0)}}),
            (12, {"Root": {"loc": (0, 0, 0.08), "rot": (-0.16, 0, 0)}, "ThroatSac": {"scale": (1.42, 1.42, 1.38)}, "Head": {"rot": (-0.18, 0, 0)}}),
            (28, {"ThroatSac": {"scale": (1, 1, 1)}, "Head": {"rot": (0, 0, 0)}, "Root": {"loc": (0, 0, 0), "rot": (0, 0, 0)}}),
        ]),
        "Attack": (1, 26, [
            (1, {"Root": {"loc": (0, 0, 0)}, "ThroatSac": {"scale": (1, 1, 1)}}),
            (8, {"Root": {"loc": (0, 0.14, -0.06), "rot": (0.14, 0, 0)}, "ThroatSac": {"scale": (1.52, 1.50, 1.48)}, "Head": {"rot": (-0.22, 0, 0)}}),
            (14, {"Root": {"loc": (0, -0.24, 0.10), "rot": (-0.18, 0, 0)}, "ThroatSac": {"scale": (0.85, 1.25, 0.85)}, "Head": {"rot": (0.28, 0, 0)}}),
            (26, {"Root": {"loc": (0, 0, 0), "rot": (0, 0, 0)}, "ThroatSac": {"scale": (1, 1, 1)}, "Head": {"rot": (0, 0, 0)}}),
        ]),
        "Hit": (1, 18, [
            (1, {"Root": {"loc": (0, 0, 0)}}),
            (6, {"Root": {"loc": (0, 0.08, -0.12), "rot": (0.18, 0, 0.15)}, "Head": {"rot": (0.24, 0, 0)}}),
            (18, {"Root": {"loc": (0, 0, 0), "rot": (0, 0, 0)}, "Head": {"rot": (0, 0, 0)}}),
        ]),
    }

    _finalize_and_export(
        bm, flat, mats, "Creature_CocHoGia",
        os.path.join(out_dir, "Creature_CocHoGia.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_04_coc_ho_gia.png") if prev_dir else None,
        bone_specs=bones_04, clips_spec=clips_04,
    )


# ================================================================== HANG 2: VƯỜN QUẶNG CẦU VỒNG (4 LOÀI)

def build_05_dom_dom_sac_mau(out_dir, prev_dir):
    """[M] Đom Đóm Sắc Màu: cánh gân mảnh & bụng lồng đèn pha lê chuyển sắc cầu vồng."""
    kc.new_scene()
    mats = _make_creature_materials()
    bm, col, flat = _new_bm()

    def body_dark(p, n):
        pollen = 0.5 + 0.5 * math.sin(p.x * 28.0 + p.z * 22.0)
        base = _lerp3((0.09, 0.08, 0.14), (0.24, 0.22, 0.34), max(0.0, n.z) * 0.65 + 0.25)
        if p.z < 0.52 and pollen > 0.72:
            base = _lerp3(base, (0.86, 0.72, 0.22), 0.45)
        return base

    # Tư thế bay nghiêng lượn (ngẩng đầu, bụng cong mềm) + phấn hoa Luma bám ngực
    _add_ellipsoid(bm, col, (0, -0.14, 0.53), (0.09, 0.11, 0.09), segs=16, rings=10, rot=(0.14, 0.10, -0.12), mat_index=1, color_fn=body_dark)
    _add_ellipsoid(bm, col, (0.01, -0.27, 0.56), (0.072, 0.070, 0.068), segs=14, rings=8, rot=(0.10, 0.08, -0.15), mat_index=1, color_fn=body_dark)
    for sx in (-1.0, 1.0):
        _add_eye_bulb(bm, col, (sx * 0.056, -0.30, 0.555), 0.030, yaw=sx * 0.4, eye_col=(0.24, 0.92, 0.88))
        _add_tube(bm, col, [(sx * 0.025, -0.32, 0.58), (sx * 0.10, -0.44, 0.68), (sx * 0.17, -0.49, 0.72)], [0.009, 0.006, 0.003], sides=6, subdiv=1, mat_index=1, color_fn=body_dark)

    # Đuôi Lồng Đèn Pha Lê 4 đốt phát sắc cầu vồng rực rỡ
    seg_centers = [(0, -0.01, 0.48), (0, 0.11, 0.43), (0, 0.22, 0.38), (0, 0.32, 0.33)]
    seg_radii = [(0.095, 0.085, 0.085), (0.115, 0.095, 0.095), (0.100, 0.088, 0.088), (0.068, 0.075, 0.068)]
    for idx, (sc, sr) in enumerate(zip(seg_centers, seg_radii)):
        hue_t = idx * 0.22
        _add_faceted_chunk(
            bm, col, sc, sr, idx * 3.2, (0.28, 0.0, idx * 0.4),
            mat_index=2, color_fn=lambda p, _n, ht=hue_t: _rainbow_color(ht + p.y * 0.6), flat_faces=flat
        )
        # Vòng đai kitin ôm giữa các đốt đèn
        _add_tube(
            bm, col,
            [(0, sc[1] - 0.03, sc[2] + 0.06), (0, sc[1], sc[2] + 0.09), (0, sc[1] + 0.03, sc[2] + 0.06)],
            [sr[0] * 0.85, sr[0] * 0.92, sr[0] * 0.80], sides=8, subdiv=0, mat_index=1, color_fn=body_dark
        )

    # Đôi cánh chính & cánh phụ tỏa rộng có độ dày 3D & viền phát sáng nhẹ
    for sx in (-1.0, 1.0):
        _add_3d_leaf_blade(
            bm, col, (sx * 0.06, -0.10, 0.59), Vector((sx * 0.92, 0.18, 0.34)), Vector((0.0, 0.0, 1.0)),
            0.46, 0.125, thickness=0.012, curl=0.06, vein_mat=2,
            color_fn=lambda p, _n, s=sx: _lerp3((0.28, 0.88, 0.96), (0.94, 0.44, 0.82), abs(p.x) * 2.0)
        )
        _add_3d_leaf_blade(
            bm, col, (sx * 0.05, -0.05, 0.56), Vector((sx * 0.82, 0.46, 0.14)), Vector((0.0, 0.0, 1.0)),
            0.34, 0.090, thickness=0.010, curl=0.04, vein_mat=2,
            color_fn=lambda p, _n: _lerp3((0.92, 0.78, 0.24), (0.34, 0.94, 0.56), abs(p.x) * 2.4)
        )

    # 6 chân mảnh khảnh buông xuống chạm đất z = 0
    for sx in (-1.0, 1.0):
        for py, back_y in [(-0.18, -0.14), (-0.12, -0.02), (-0.06, 0.10)]:
            _add_tube(
                bm, col,
                [(sx * 0.06, py, 0.45), (sx * 0.13, back_y, 0.24), (sx * 0.09, back_y + 0.05, 0.01)],
                [0.013, 0.009, 0.005], sides=6, subdiv=1, mat_index=1, color_fn=body_dark
            )

    bones_05 = [
        ("Root", (0, -0.06, 0.50), (0, -0.20, 0.54), None, 0.24, 1.0),
        ("Head", (0, -0.20, 0.54), (0, -0.36, 0.58), "Root", 0.14, 2.2),
        ("Lantern", (0, 0.02, 0.48), (0, 0.34, 0.32), "Root", 0.18, 2.2),
        ("Wing_FL", (-0.06, -0.10, 0.59), (-0.48, 0.0, 0.72), "Root", 0.16, 2.8),
        ("Wing_FR", (0.06, -0.10, 0.59), (0.48, 0.0, 0.72), "Root", 0.16, 2.8),
        ("Wing_BL", (-0.05, -0.05, 0.56), (-0.36, 0.16, 0.60), "Root", 0.14, 2.6),
        ("Wing_BR", (0.05, -0.05, 0.56), (0.36, 0.16, 0.60), "Root", 0.14, 2.6),
        ("Legs", (0, -0.12, 0.44), (0, 0.06, 0.04), "Root", 0.16, 2.4),
    ]
    clips_05 = {
        "Idle": (1, 32, [
            (1, {"Root": {"loc": (0, 0, 0)}, "Wing_FL": {"rot": (0, 0.25, 0)}, "Wing_FR": {"rot": (0, -0.25, 0)}, "Wing_BL": {"rot": (0, 0.18, 0)}, "Wing_BR": {"rot": (0, -0.18, 0)}, "Lantern": {"rot": (0.08, 0, 0)}}),
            (8, {"Root": {"loc": (0, 0, 0.04)}, "Wing_FL": {"rot": (0, -0.55, 0.12)}, "Wing_FR": {"rot": (0, 0.55, -0.12)}, "Wing_BL": {"rot": (0, -0.42, 0)}, "Wing_BR": {"rot": (0, 0.42, 0)}, "Lantern": {"rot": (-0.06, 0, 0.08)}}),
            (16, {"Root": {"loc": (0, 0, 0.06)}, "Wing_FL": {"rot": (0, 0.35, 0)}, "Wing_FR": {"rot": (0, -0.35, 0)}, "Wing_BL": {"rot": (0, 0.25, 0)}, "Wing_BR": {"rot": (0, -0.25, 0)}, "Lantern": {"rot": (0.10, 0, -0.08)}}),
            (24, {"Root": {"loc": (0, 0, 0.02)}, "Wing_FL": {"rot": (0, -0.55, 0.12)}, "Wing_FR": {"rot": (0, 0.55, -0.12)}, "Wing_BL": {"rot": (0, -0.42, 0)}, "Wing_BR": {"rot": (0, 0.42, 0)}, "Lantern": {"rot": (-0.06, 0, 0)}}),
            (32, {"Root": {"loc": (0, 0, 0)}, "Wing_FL": {"rot": (0, 0.25, 0)}, "Wing_FR": {"rot": (0, -0.25, 0)}, "Wing_BL": {"rot": (0, 0.18, 0)}, "Wing_BR": {"rot": (0, -0.18, 0)}, "Lantern": {"rot": (0.08, 0, 0)}}),
        ]),
        "Walk": (1, 16, [
            (1, {"Root": {"rot": (0.18, 0.12, 0), "loc": (0, 0, 0.03)}, "Wing_FL": {"rot": (0, -0.65, 0.18)}, "Wing_FR": {"rot": (0, 0.65, -0.18)}, "Legs": {"rot": (0.28, 0, 0)}, "Lantern": {"rot": (0.15, 0, 0)}}),
            (8, {"Root": {"rot": (0.22, -0.12, 0), "loc": (0, 0, 0.08)}, "Wing_FL": {"rot": (0, 0.45, -0.12)}, "Wing_FR": {"rot": (0, -0.45, 0.12)}, "Legs": {"rot": (0.18, 0, 0)}, "Lantern": {"rot": (-0.05, 0, 0)}}),
            (16, {"Root": {"rot": (0.18, 0.12, 0), "loc": (0, 0, 0.03)}, "Wing_FL": {"rot": (0, -0.65, 0.18)}, "Wing_FR": {"rot": (0, 0.65, -0.18)}, "Legs": {"rot": (0.28, 0, 0)}, "Lantern": {"rot": (0.15, 0, 0)}}),
        ]),
        "Action": (1, 28, [
            (1, {"Lantern": {"scale": (1, 1, 1), "rot": (0, 0, 0)}}),
            (12, {"Root": {"loc": (0, 0, 0.12)}, "Lantern": {"scale": (1.35, 1.25, 1.35), "rot": (-0.28, 0, 0)}, "Wing_FL": {"rot": (0, -0.20, -0.35)}, "Wing_FR": {"rot": (0, 0.20, 0.35)}}),
            (28, {"Root": {"loc": (0, 0, 0)}, "Lantern": {"scale": (1, 1, 1), "rot": (0, 0, 0)}, "Wing_FL": {"rot": (0, 0, 0)}, "Wing_FR": {"rot": (0, 0, 0)}}),
        ]),
        "Hit": (1, 16, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (5, {"Root": {"rot": (-0.35, 0.45, 0.35)}}),
            (16, {"Root": {"rot": (0, 0, 0)}}),
        ]),
    }

    _finalize_and_export(
        bm, flat, mats, "Creature_DomDomSacMau",
        os.path.join(out_dir, "Creature_DomDomSacMau.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_05_dom_dom_sac_mau.png") if prev_dir else None,
        bone_specs=bones_05, clips_spec=clips_05,
    )


def build_06_tho_la(out_dir, prev_dir):
    """[M] Thỏ Lá: bộ lông 3 tông (nâu quế - kem sáng - rêu lá), nụ hoa Luma trên tai lá & dáng nghe ngóng sinh động."""
    kc.new_scene()
    mats = _make_creature_materials()
    bm, col, flat = _new_bm()
    rng = random.Random(206)

    def leaf_fur(p, n):
        # Lông 3 tông tự nhiên: lưng nâu rêu ấm có đốm lá, hông nâu quế, bụng & ngực kem sáng mềm mại
        speck = 0.5 + 0.5 * math.sin(p.x * 26.0 + p.y * 18.0) * math.cos(p.z * 22.0)
        moss_back = (0.18, 0.34, 0.13)
        cinnamon_flank = (0.42, 0.28, 0.14)
        cream_belly = (0.76, 0.72, 0.54)
        c = _lerp3(moss_back, cinnamon_flank, 0.35 + 0.45 * _clamp01(abs(p.x) * 4.5 + speck * 0.25))
        if n.z < -0.08 or (p.y < -0.15 and p.z < 0.28 and abs(p.x) < 0.08):
            c = _lerp3(c, cream_belly, _clamp01((-n.z + 0.15) * 1.15))
        elif n.z > 0.55 and speck > 0.65:
            c = _lerp3(c, (0.34, 0.56, 0.20), 0.55)
        return c

    def leaf_ear_col(p, n):
        t = _clamp01((p.z - 0.50) * 1.75)
        midrib = abs(p.x - 0.05) < 0.025 or abs(p.x + 0.05) < 0.025
        vein = abs(math.sin((p.z - 0.48) * 24.0)) > 0.86
        base = _lerp3((0.16, 0.38, 0.12), (0.64, 0.78, 0.22), t)
        if midrib or vein:
            base = _lerp3(base, (0.82, 0.86, 0.38), 0.6)
        return base

    # Thân thỏ cuộn tròn mềm mại, đầu hơi xoay nghiêng ngóng nghe (+X/-Y)
    _add_tube(
        bm, col,
        [(0.03, -0.34, 0.37), (0.02, -0.22, 0.34), (0.0, -0.02, 0.30), (-0.01, 0.18, 0.27), (0.0, 0.32, 0.21)],
        [(0.092, 0.085), (0.105, 0.098), (0.138, 0.142), (0.152, 0.158), (0.090, 0.095)],
        sides=18, subdiv=2, mat_index=0, color_fn=leaf_fur
    )
    # Ngực xù lông kem + đầu hơi nghiêng + hai má bầu bĩnh + ria mép
    _add_ellipsoid(bm, col, (0.01, -0.23, 0.25), (0.095, 0.085, 0.085), segs=14, rings=10, mat_index=0, color_fn=lambda _p, _n: (0.76, 0.72, 0.54))
    _add_ellipsoid(bm, col, (0.03, -0.38, 0.39), (0.108, 0.135, 0.098), segs=18, rings=12, rot=(0.22, 0.10, -0.22), mat_index=0, color_fn=leaf_fur)
    for sx in (-1.0, 1.0):
        _add_ellipsoid(bm, col, (0.03 + sx * 0.068, -0.42, 0.365), (0.052, 0.052, 0.044), segs=12, rings=8, mat_index=0, color_fn=lambda _p, _n: (0.72, 0.66, 0.48))
        for wy in (0.0, 0.015):
            _add_tube(bm, col, [(0.03 + sx * 0.05, -0.47, 0.36 + wy), (0.03 + sx * 0.14, -0.49, 0.35 + wy * 1.4)], [0.004, 0.002], sides=5, subdiv=0, mat_index=5, color_fn=lambda _p, _n: (0.86, 0.84, 0.74))
    _add_ellipsoid(bm, col, (0.045, -0.51, 0.37), (0.025, 0.020, 0.018), segs=10, rings=6, mat_index=5, color_fn=lambda _p, _n: (0.82, 0.42, 0.42))

    # Mắt to long lanh + Đôi tai lá bất đối xứng (1 tai vểnh nghe, 1 tai hơi ngả mềm) + nụ hoa Luma nhỏ
    ear_dirs = [(-1.0, Vector((-0.34, 0.38, 0.86)), 0.39), (1.0, Vector((0.46, 0.24, 0.82)), 0.41)]
    for sx, edir, elen in ear_dirs:
        _add_eye_bulb(bm, col, (0.03 + sx * 0.082, -0.40, 0.415), 0.030, yaw=sx * 0.52 - 0.20, eye_col=(0.94, 0.78, 0.22))
        _add_3d_leaf_blade(
            bm, col, (0.03 + sx * 0.055, -0.31, 0.46),
            edir, Vector((0.0, -1.0, 0.3)),
            elen, 0.115, thickness=0.018, curl=0.14, vein_mat=5, color_fn=leaf_ear_col
        )
        _add_3d_leaf_blade(
            bm, col, (0.02 + sx * 0.075, -0.22, 0.31),
            Vector((sx * 0.38, 0.35, -0.68)), Vector((0.0, -1.0, 0.0)),
            0.14, 0.056, thickness=0.014, curl=0.05, vein_mat=5, color_fn=leaf_ear_col
        )
    # Nụ sương Luma phát sáng nhẹ đính bên gốc tai & chùm lá đuôi
    _add_luma_bud(bm, col, (0.08, -0.30, 0.47), Vector((0.4, -0.2, 0.9)), size=0.024, hue=0.16)
    _add_luma_bud(bm, col, (-0.01, 0.36, 0.26), Vector((0.0, 0.5, 0.85)), size=0.022, hue=0.45)

    for ang_s, pitch in [(0.0, 0.45), (-0.42, 0.35), (0.42, 0.35)]:
        _add_3d_leaf_blade(
            bm, col, (0.0, 0.34, 0.22),
            Vector((math.sin(ang_s) * 0.45, math.cos(ang_s) * 0.85, pitch)), Vector((0.0, 0.0, 1.0)),
            0.20, 0.068, thickness=0.014, curl=0.10, vein_mat=5, color_fn=leaf_ear_col
        )

    # 2 chân sau gập đàn hồi + chân trước phải hơi nhấc nhẹ sinh động
    for sx, paw_lift in [(-1.0, 0.0), (1.0, 0.035)]:
        _add_ellipsoid(bm, col, (sx * 0.135, 0.16, 0.21), (0.072, 0.125, 0.11), segs=14, rings=10, rot=(0.35, 0, sx * 0.15), mat_index=0, color_fn=leaf_fur)
        _add_tube(bm, col, [(sx * 0.14, 0.22, 0.14), (sx * 0.14, 0.26, 0.03), (sx * 0.13, 0.06, 0.022)], [0.048, 0.038, 0.030], sides=12, subdiv=2, mat_index=0, color_fn=leaf_fur)
        _add_tube(bm, col, [(sx * 0.09, -0.17, 0.26), (sx * 0.11, -0.13, 0.13 + paw_lift * 0.4), (sx * 0.10, -0.22, 0.04 + paw_lift), (sx * 0.095, -0.28, 0.024 + paw_lift)], [0.042, 0.034, 0.026, 0.022], sides=12, subdiv=2, mat_index=0, color_fn=leaf_fur)
        _add_ellipsoid(bm, col, (sx * 0.095, -0.29, 0.024 + paw_lift), (0.032, 0.044, 0.020), segs=10, rings=6, mat_index=0, color_fn=lambda _p, _n: (0.72, 0.66, 0.48))

    bones_06 = [
        ("Root", (0.0, 0.12, 0.28), (0.0, -0.14, 0.31), None, 0.36, 1.0),
        ("Head", (0.02, -0.26, 0.35), (0.04, -0.48, 0.39), "Root", 0.20, 2.2),
        ("Ear_L", (-0.03, -0.31, 0.46), (-0.16, -0.18, 0.80), "Head", 0.14, 2.8),
        ("Ear_R", (0.09, -0.31, 0.46), (0.26, -0.21, 0.78), "Head", 0.14, 2.8),
        ("Tail", (0.0, 0.32, 0.21), (0.0, 0.50, 0.28), "Root", 0.14, 2.2),
        ("Leg_FL", (-0.09, -0.17, 0.26), (-0.10, -0.29, 0.02), "Root", 0.13, 2.5),
        ("Leg_FR", (0.09, -0.17, 0.26), (0.10, -0.29, 0.06), "Root", 0.13, 2.5),
        ("Leg_BL", (-0.135, 0.16, 0.21), (-0.13, 0.06, 0.02), "Root", 0.16, 2.5),
        ("Leg_BR", (0.135, 0.16, 0.21), (0.13, 0.06, 0.02), "Root", 0.16, 2.5),
    ]
    clips_06 = {
        "Idle": (1, 56, [
            (1, {"Head": {"rot": (0.04, 0, 0.06)}, "Ear_L": {"rot": (0.12, 0, 0.08)}, "Ear_R": {"rot": (-0.08, 0, -0.12)}, "Tail": {"rot": (0.08, 0, 0)}}),
            (20, {"Head": {"rot": (-0.08, 0.08, -0.14)}, "Ear_L": {"rot": (-0.14, 0, -0.18)}, "Ear_R": {"rot": (0.16, 0, 0.16)}, "Tail": {"rot": (-0.12, 0, 0.14)}}),
            (38, {"Head": {"rot": (0.08, -0.06, 0.12)}, "Ear_L": {"rot": (0.18, 0, 0.12)}, "Ear_R": {"rot": (-0.12, 0, 0.08)}, "Tail": {"rot": (0.14, 0, -0.14)}}),
            (56, {"Head": {"rot": (0.04, 0, 0.06)}, "Ear_L": {"rot": (0.12, 0, 0.08)}, "Ear_R": {"rot": (-0.08, 0, -0.12)}, "Tail": {"rot": (0.08, 0, 0)}}),
        ]),
        "Walk": (1, 20, [
            (1, {"Root": {"loc": (0, 0, 0), "rot": (-0.16, 0, 0)}, "Leg_FL": {"rot": (-0.55, 0, 0)}, "Leg_FR": {"rot": (-0.55, 0, 0)}, "Leg_BL": {"rot": (0.48, 0, 0)}, "Leg_BR": {"rot": (0.48, 0, 0)}, "Ear_L": {"rot": (0.28, 0, 0)}, "Ear_R": {"rot": (0.28, 0, 0)}}),
            (10, {"Root": {"loc": (0, -0.08, 0.16), "rot": (0.20, 0, 0)}, "Leg_FL": {"rot": (0.42, 0, 0)}, "Leg_FR": {"rot": (0.42, 0, 0)}, "Leg_BL": {"rot": (-0.58, 0, 0)}, "Leg_BR": {"rot": (-0.58, 0, 0)}, "Ear_L": {"rot": (-0.25, 0, 0)}, "Ear_R": {"rot": (-0.25, 0, 0)}}),
            (20, {"Root": {"loc": (0, 0, 0), "rot": (-0.16, 0, 0)}, "Leg_FL": {"rot": (-0.55, 0, 0)}, "Leg_FR": {"rot": (-0.55, 0, 0)}, "Leg_BL": {"rot": (0.48, 0, 0)}, "Leg_BR": {"rot": (0.48, 0, 0)}, "Ear_L": {"rot": (0.28, 0, 0)}, "Ear_R": {"rot": (0.28, 0, 0)}}),
        ]),
        "Evade": (1, 18, [
            (1, {"Root": {"loc": (0, 0, 0), "rot": (0, 0, 0)}}),
            (6, {"Root": {"loc": (0.26, 0, 0.14), "rot": (0.12, 0.38, -0.32)}, "Ear_L": {"rot": (0.45, 0, 0)}, "Ear_R": {"rot": (0.45, 0, 0)}}),
            (12, {"Root": {"loc": (0.36, 0, 0.04), "rot": (0, 0.18, -0.15)}}),
            (18, {"Root": {"loc": (0, 0, 0), "rot": (0, 0, 0)}, "Ear_L": {"rot": (0, 0, 0)}, "Ear_R": {"rot": (0, 0, 0)}}),
        ]),
        "Hit": (1, 16, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (5, {"Root": {"loc": (0, 0.10, 0.10), "rot": (-0.28, 0, 0.22)}, "Ear_L": {"rot": (0.40, 0, 0)}, "Ear_R": {"rot": (0.40, 0, 0)}}),
            (16, {"Root": {"loc": (0, 0, 0), "rot": (0, 0, 0)}, "Ear_L": {"rot": (0, 0, 0)}, "Ear_R": {"rot": (0, 0, 0)}}),
        ]),
    }

    _finalize_and_export(
        bm, flat, mats, "Creature_ThoLa",
        os.path.join(out_dir, "Creature_ThoLa.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_06_tho_la.png") if prev_dir else None,
        bone_specs=bones_06, clips_spec=clips_06,
    )


def build_07_bo_giap_quang(out_dir, prev_dir):
    """[S] Bọ Giáp Quặng: bọ tăng thiết giáp sừng húc, mai đá xếp tầng nạm đầy tinh thể quặng cầu vồng."""
    kc.new_scene()
    mats = _make_creature_materials()
    bm, col, flat = _new_bm()
    rng = random.Random(207)

    def armor_plate(p, n):
        edge = max(0.0, n.z)
        vein = 1.0 - abs(noise.noise(p * 7.5))
        base = _lerp3((0.12, 0.13, 0.17), (0.36, 0.38, 0.44), edge * 0.7 + 0.15 * math.sin(p.y * 12.0))
        if vein > 0.78:
            base = _lerp3(base, (0.48, 0.38, 0.18), 0.55)
        return base

    # Thân đá giáp cứng chia 3 đốt tấm giáp lưng chồng xếp + rêu/mảnh quặng nhỏ
    _add_ellipsoid(bm, col, (0, 0.05, 0.30), (0.42, 0.56, 0.22), segs=18, rings=10, mat_index=1, color_fn=armor_plate)
    for k, (py, pz, sy, sx_r) in enumerate([(-0.24, 0.42, 0.20, 0.40), (0.04, 0.45, 0.24, 0.45), (0.32, 0.39, 0.20, 0.38)]):
        _add_faceted_chunk(
            bm, col, (0, py, pz), (sx_r, sy, 0.14), k * 2.1, (-0.18, 0.0, 0.0),
            mat_index=1, color_fn=armor_plate, flat_faces=flat
        )
    _add_moss_tuft(bm, col, (-0.28, -0.05, 0.40), scale=0.055, rng=rng)
    _add_moss_tuft(bm, col, (0.24, 0.26, 0.37), scale=0.048, rng=rng)

    # Đầu hơi xoay + Sừng Tê Giác uốn cong mạnh vươn về trước + cặp kìm đá
    _add_faceted_chunk(bm, col, (0.02, -0.52, 0.31), (0.24, 0.20, 0.16), 8.2, (0.2, 0.06, -0.14), mat_index=1, color_fn=armor_plate, flat_faces=flat)
    _add_tube(bm, col, [(0, -0.56, 0.36), (0, -0.78, 0.42), (0, -0.94, 0.58)], [0.068, 0.044, 0.016], sides=6, subdiv=1, mat_index=1, color_fn=armor_plate, flat_faces=flat, is_flat=True)
    for sx in (-1.0, 1.0):
        _add_eye_bulb(bm, col, (sx * 0.18, -0.56, 0.33), 0.036, yaw=sx * 0.5, eye_col=(0.95, 0.65, 0.15))
        _add_tube(bm, col, [(sx * 0.14, -0.58, 0.24), (sx * 0.20, -0.74, 0.25), (sx * 0.08, -0.82, 0.27)], [0.040, 0.026, 0.010], sides=6, subdiv=0, mat_index=1, color_fn=armor_plate, flat_faces=flat, is_flat=True)

    # Nạm 14 trụ tinh thể quặng đa sắc cầu vồng mọc xuyên qua khe giáp lưng
    ore_specs = [
        (0.0, -0.04, 0.50, (0.0, -0.1, 1.0), 0.34, 0.08, 0.0),
        (-0.22, 0.08, 0.46, (-0.45, 0.1, 0.88), 0.28, 0.065, 0.18),
        (0.22, 0.08, 0.46, (0.45, 0.1, 0.88), 0.28, 0.065, 0.38),
        (-0.14, -0.22, 0.46, (-0.25, -0.25, 0.92), 0.24, 0.055, 0.55),
        (0.14, -0.22, 0.46, (0.25, -0.25, 0.92), 0.24, 0.055, 0.72),
        (-0.20, 0.30, 0.40, (-0.38, 0.35, 0.85), 0.25, 0.060, 0.86),
        (0.20, 0.30, 0.40, (0.38, 0.35, 0.85), 0.25, 0.060, 0.10),
        (0.0, 0.22, 0.44, (0.0, 0.25, 0.96), 0.29, 0.072, 0.64),
    ]
    for ox, oy, oz, d_v, l_v, r_v, hue in ore_specs:
        _add_crystal_prism(
            bm, col, (ox, oy, oz), Vector(d_v), l_v, r_v, r_v * 0.70, sides=6, rot_twist=rng.uniform(0, 3.0),
            mat_index=2, color_fn=lambda p, _n, h=hue: _rainbow_color(h + p.z * 0.35), flat_faces=flat
        )

    # 6 chân bọc giáp nặng nề
    for sx, py, spread in [(-1, -0.26, 0.48), (1, -0.26, 0.48), (-1, 0.05, 0.56), (1, 0.05, 0.56), (-1, 0.34, 0.50), (1, 0.34, 0.50)]:
        _add_tube(
            bm, col,
            [(sx * 0.26, py, 0.26), (sx * spread, py - 0.03, 0.30), (sx * (spread + 0.08), py + 0.02, 0.14), (sx * (spread + 0.04), py + 0.03, 0.01)],
            [0.065, 0.052, 0.040, 0.022], sides=8, subdiv=0, mat_index=1, color_fn=armor_plate, flat_faces=flat, is_flat=True
        )

    bones_07 = [
        ("Root", (0, 0.08, 0.34), (0, -0.25, 0.34), None, 0.65, 1.0),
        ("Head", (0.01, -0.40, 0.32), (0.02, -0.78, 0.42), "Root", 0.34, 2.2),
        ("Leg_FL", (-0.26, -0.26, 0.26), (-0.52, -0.23, 0.01), "Root", 0.22, 2.5),
        ("Leg_FR", (0.26, -0.26, 0.26), (0.52, -0.23, 0.01), "Root", 0.22, 2.5),
        ("Leg_ML", (-0.26, 0.05, 0.26), (-0.60, 0.08, 0.01), "Root", 0.22, 2.5),
        ("Leg_MR", (0.26, 0.05, 0.26), (0.60, 0.08, 0.01), "Root", 0.22, 2.5),
        ("Leg_BL", (-0.26, 0.34, 0.26), (-0.54, 0.37, 0.01), "Root", 0.22, 2.5),
        ("Leg_BR", (0.26, 0.34, 0.26), (0.54, 0.37, 0.01), "Root", 0.22, 2.5),
    ]
    clips_07 = {
        "Idle": (1, 56, [
            (1, {"Root": {"loc": (0, 0, 0)}, "Head": {"rot": (0.05, 0, 0.06)}}),
            (28, {"Root": {"loc": (0, 0, 0.025)}, "Head": {"rot": (-0.08, 0, -0.08)}}),
            (56, {"Root": {"loc": (0, 0, 0)}, "Head": {"rot": (0.05, 0, 0.06)}}),
        ]),
        "Walk": (1, 28, [
            (1, {"Root": {"rot": (0, 0.06, 0.04)}, "Leg_FL": {"rot": (0.42, 0, 0.20)}, "Leg_MR": {"rot": (0.42, 0, -0.20)}, "Leg_BL": {"rot": (0.42, 0, 0.20)}, "Leg_FR": {"rot": (-0.35, 0, 0)}, "Leg_ML": {"rot": (-0.35, 0, 0)}, "Leg_BR": {"rot": (-0.35, 0, 0)}}),
            (14, {"Root": {"rot": (0, -0.06, -0.04)}, "Leg_FL": {"rot": (-0.35, 0, 0)}, "Leg_MR": {"rot": (-0.35, 0, 0)}, "Leg_BL": {"rot": (-0.35, 0, 0)}, "Leg_FR": {"rot": (0.42, 0, -0.20)}, "Leg_ML": {"rot": (0.42, 0, 0.20)}, "Leg_BR": {"rot": (0.42, 0, -0.20)}}),
            (28, {"Root": {"rot": (0, 0.06, 0.04)}, "Leg_FL": {"rot": (0.42, 0, 0.20)}, "Leg_MR": {"rot": (0.42, 0, -0.20)}, "Leg_BL": {"rot": (0.42, 0, 0.20)}, "Leg_FR": {"rot": (-0.35, 0, 0)}, "Leg_ML": {"rot": (-0.35, 0, 0)}, "Leg_BR": {"rot": (-0.35, 0, 0)}}),
        ]),
        "Attack": (1, 24, [
            (1, {"Head": {"rot": (0, 0, 0)}}),
            (7, {"Root": {"loc": (0, 0.12, 0)}, "Head": {"rot": (0.28, 0, 0)}}),
            (13, {"Root": {"loc": (0, -0.28, 0.04)}, "Head": {"rot": (-0.42, 0, 0)}}),
            (24, {"Root": {"loc": (0, 0, 0)}, "Head": {"rot": (0, 0, 0)}}),
        ]),
        "Hit": (1, 18, [
            (1, {"Root": {"loc": (0, 0, 0)}}),
            (6, {"Root": {"loc": (0, 0, -0.06), "rot": (0.14, 0, 0.18)}}),
            (18, {"Root": {"loc": (0, 0, 0), "rot": (0, 0, 0)}}),
        ]),
    }

    _finalize_and_export(
        bm, flat, mats, "Creature_BoGiapQuang",
        os.path.join(out_dir, "Creature_BoGiapQuang.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_07_bo_giap_quang.png") if prev_dir else None,
        bone_specs=bones_07, clips_spec=clips_07,
    )


def build_08_rua_pha_le(out_dir, prev_dir):
    """[C] Rùa Pha Lê (Chúa Hang): rùa cổ đại vĩ đại, cả mai lưng là pháo đài cụm pha lê thạch anh cầu vồng."""
    kc.new_scene()
    mats = _make_creature_materials()
    bm, col, flat = _new_bm()
    rng = random.Random(208)

    def stone_hide(p, n):
        cr = 1.0 - abs(noise.noise(p * 3.5))
        base = _lerp3((0.20, 0.23, 0.30), (0.50, 0.52, 0.58), cr * 0.65 + max(0.0, n.z) * 0.35)
        if n.z < -0.1:
            base = _lerp3(base, (0.56, 0.52, 0.40), 0.55)
        elif n.z > 0.65 and cr < 0.35:
            base = _lerp3(base, (0.16, 0.36, 0.15), 0.6)
        return base

    # Yếm dưới & Vành Mai Đá Khổng Lồ (dài ~2.3m, rộng ~1.7m)
    _add_ellipsoid(bm, col, (0, 0.04, 0.36), (0.64, 0.90, 0.22), segs=22, rings=10, mat_index=0, color_fn=stone_hide)
    _add_faceted_chunk(bm, col, (0, 0.06, 0.52), (0.82, 1.04, 0.20), 1.7, (0, 0, 0), mat_index=1, color_fn=stone_hide, flat_faces=flat)
    _add_faceted_chunk(bm, col, (0, 0.06, 0.72), (0.68, 0.88, 0.28), 3.4, (0, 0, 0.1), mat_index=1, color_fn=stone_hide, flat_faces=flat)

    # 19 Trụ Tinh Thể Thạch Anh Cầu Vồng mọc tua tủa tạo thành vòm mai Pha Lê rực rỡ!
    spire_ring = [
        (0.0, 0.04, 0.78, (0.0, 0.0, 1.0), 0.76, 0.18, 0.76),  # Đỉnh thạch anh tím-xanh trung tâm
        (-0.24, -0.24, 0.74, (-0.30, -0.25, 0.92), 0.56, 0.13, 0.05),
        (0.24, -0.24, 0.74, (0.30, -0.25, 0.92), 0.56, 0.13, 0.18),
        (-0.28, 0.18, 0.74, (-0.36, 0.18, 0.90), 0.54, 0.13, 0.48),
        (0.28, 0.18, 0.74, (0.36, 0.18, 0.90), 0.54, 0.13, 0.62),
        (0.0, -0.44, 0.68, (0.0, -0.45, 0.88), 0.46, 0.11, 0.28),
        (0.0, 0.44, 0.68, (0.0, 0.45, 0.88), 0.48, 0.11, 0.88),
    ]
    for k in range(10):
        ang = 2.0 * math.pi * k / 10.0
        bx = math.cos(ang) * 0.48
        by = 0.06 + math.sin(ang) * 0.62
        d_v = Vector((math.cos(ang) * 0.62, math.sin(ang) * 0.62, 0.72)).normalized()
        spire_ring.append((bx, by, 0.62, tuple(d_v), rng.uniform(0.32, 0.48), rng.uniform(0.08, 0.11), k / 10.0))

    for sx_p, sy_p, sz_p, d_v, l_v, r_v, hue in spire_ring:
        _add_crystal_prism(
            bm, col, (sx_p, sy_p, sz_p), Vector(d_v), l_v, r_v, r_v * 0.68,
            sides=6, rot_twist=rng.uniform(0, 3.14), mat_index=2,
            color_fn=lambda p, _n, h=hue: _lerp3(_rainbow_color(h), (0.94, 0.96, 1.0), _clamp01((p.z - 0.85) * 0.75) * 0.35),
            flat_faces=flat
        )

    # Cổ rùa uốn nghiêng nhìn + Đầu rùa giáp + Mắt sáng + Mào pha lê + Rêu quanh mép mai
    _add_tube(bm, col, [(0, -0.68, 0.42), (0.04, -0.92, 0.47), (0.08, -1.16, 0.51)], [0.24, 0.19, 0.18], sides=16, subdiv=2, mat_index=0, color_fn=stone_hide)
    _add_moss_tuft(bm, col, (-0.36, -0.32, 0.72), scale=0.09, rng=rng)
    _add_moss_tuft(bm, col, (0.38, 0.30, 0.70), scale=0.085, rng=rng)
    _add_faceted_chunk(bm, col, (0.08, -1.24, 0.51), (0.20, 0.24, 0.16), 9.1, (0.1, 0.08, -0.18), mat_index=1, color_fn=stone_hide, flat_faces=flat)
    for sx in (-1.0, 1.0):
        _add_eye_bulb(bm, col, (0.08 + sx * 0.155, -1.25, 0.555), 0.042, yaw=sx * 0.52 - 0.15, eye_col=(0.24, 0.94, 0.98))
        _add_crystal_prism(bm, col, (0.08 + sx * 0.10, -1.15, 0.61), Vector((sx * 0.3, 0.3, 0.9)), 0.18, 0.038, 0.020, sides=5, mat_index=2, color_fn=lambda _p, _n: (0.28, 0.76, 0.98), flat_faces=flat)

    # 4 chân trụ đá to khỏe + đuôi đá
    for sx, sy in [(-1, -0.52), (1, -0.52), (-1, 0.60), (1, 0.60)]:
        _add_amphibian_limb(
            bm, col,
            [(sx * 0.50, sy, 0.38), (sx * 0.66, sy - 0.03, 0.22), (sx * 0.64, sy - 0.06, 0.05)],
            [0.18, 0.15, 0.14], toe_count=3, toe_len=0.11, webbed=False, mat_index=0, color_fn=stone_hide
        )
    _add_tube(bm, col, [(0, 0.86, 0.38), (0, 1.12, 0.28), (0, 1.32, 0.18)], [0.14, 0.09, 0.03], sides=10, subdiv=1, mat_index=0, color_fn=stone_hide)

    bones_08 = [
        ("Root", (0, 0.06, 0.44), (0, -0.20, 0.44), None, 0.95, 1.0),
        ("Shell", (0, 0.06, 0.62), (0, 0.06, 1.35), "Root", 0.92, 1.7),
        ("Neck", (0, -0.68, 0.42), (0.04, -0.98, 0.48), "Root", 0.34, 1.8),
        ("Head", (0.04, -0.98, 0.48), (0.08, -1.32, 0.52), "Neck", 0.34, 2.4),
        ("Tail", (0, 0.86, 0.38), (0, 1.32, 0.18), "Root", 0.32, 2.0),
        ("Leg_FL", (-0.50, -0.52, 0.38), (-0.64, -0.58, 0.05), "Root", 0.32, 2.4),
        ("Leg_FR", (0.50, -0.52, 0.38), (0.64, -0.58, 0.05), "Root", 0.32, 2.4),
        ("Leg_BL", (-0.50, 0.60, 0.38), (-0.64, 0.54, 0.05), "Root", 0.32, 2.4),
        ("Leg_BR", (0.50, 0.60, 0.38), (0.64, 0.54, 0.05), "Root", 0.32, 2.4),
    ]
    clips_08 = {
        "Idle": (1, 60, [
            (1, {"Neck": {"rot": (0.08, 0, 0)}, "Head": {"rot": (-0.06, 0, 0)}, "Shell": {"loc": (0, 0, 0)}, "Tail": {"rot": (0, 0, 0.18)}}),
            (20, {"Neck": {"rot": (-0.06, 0, 0.16)}, "Head": {"rot": (-0.12, 0.06, 0.22)}, "Shell": {"loc": (0, 0, 0.04)}, "Tail": {"rot": (0, 0, -0.18)}}),
            (40, {"Neck": {"rot": (-0.06, 0, -0.16)}, "Head": {"rot": (-0.10, -0.06, -0.22)}, "Shell": {"loc": (0, 0, 0.025)}, "Tail": {"rot": (0, 0, 0.14)}}),
            (60, {"Neck": {"rot": (0.08, 0, 0)}, "Head": {"rot": (-0.06, 0, 0)}, "Shell": {"loc": (0, 0, 0)}, "Tail": {"rot": (0, 0, 0.18)}}),
        ]),
        "Walk": (1, 36, [
            (1, {"Root": {"rot": (0, 0.08, 0.05)}, "Head": {"rot": (0.10, 0, -0.08)}, "Leg_FL": {"rot": (0.48, 0, 0)}, "Leg_BR": {"rot": (0.48, 0, 0)}, "Leg_FR": {"rot": (-0.42, 0, 0)}, "Leg_BL": {"rot": (-0.42, 0, 0)}}),
            (18, {"Root": {"rot": (0, -0.08, -0.05)}, "Head": {"rot": (0.10, 0, 0.08)}, "Leg_FL": {"rot": (-0.42, 0, 0)}, "Leg_BR": {"rot": (-0.42, 0, 0)}, "Leg_FR": {"rot": (0.48, 0, 0)}, "Leg_BL": {"rot": (0.48, 0, 0)}}),
            (36, {"Root": {"rot": (0, 0.08, 0.05)}, "Head": {"rot": (0.10, 0, -0.08)}, "Leg_FL": {"rot": (0.48, 0, 0)}, "Leg_BR": {"rot": (0.48, 0, 0)}, "Leg_FR": {"rot": (-0.42, 0, 0)}, "Leg_BL": {"rot": (-0.42, 0, 0)}}),
        ]),
        "Retract": (1, 30, [
            (1, {"Neck": {"loc": (0, 0, 0)}, "Shell": {"loc": (0, 0, 0)}}),
            (10, {"Neck": {"loc": (0, 0.42, -0.06), "rot": (0.24, 0, 0)}, "Head": {"rot": (0.30, 0, 0)}, "Shell": {"loc": (0, 0, -0.20)}, "Leg_FL": {"rot": (0.72, 0, 0)}, "Leg_FR": {"rot": (0.72, 0, 0)}, "Leg_BL": {"rot": (-0.65, 0, 0)}, "Leg_BR": {"rot": (-0.65, 0, 0)}}),
            (30, {"Neck": {"loc": (0, 0.42, -0.06), "rot": (0.24, 0, 0)}, "Head": {"rot": (0.30, 0, 0)}, "Shell": {"loc": (0, 0, -0.20)}, "Leg_FL": {"rot": (0.72, 0, 0)}, "Leg_FR": {"rot": (0.72, 0, 0)}, "Leg_BL": {"rot": (-0.65, 0, 0)}, "Leg_BR": {"rot": (-0.65, 0, 0)}}),
        ]),
        "Hit": (1, 18, [
            (1, {"Shell": {"rot": (0, 0, 0)}}),
            (6, {"Shell": {"rot": (-0.22, 0, 0.16)}, "Neck": {"rot": (0.32, 0, 0)}}),
            (18, {"Shell": {"rot": (0, 0, 0)}, "Neck": {"rot": (0, 0, 0)}}),
        ]),
    }

    _finalize_and_export(
        bm, flat, mats, "Creature_RuaPhaLe",
        os.path.join(out_dir, "Creature_RuaPhaLe.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_08_rua_pha_le.png") if prev_dir else None,
        bone_specs=bones_08, clips_spec=clips_08,
    )


# ================================================================== HANG 5: VỰC NỨT TÍM (4 LOÀI)

def build_09_doi_am_thoa(out_dir, prev_dir):
    """[M] Dơi Âm Thoa: dơi hắc diện mang sừng tinh thể hình chữ U (âm thoa) & màng cánh 3D gân tím."""
    kc.new_scene()
    mats = _make_creature_materials()
    bm, col, flat = _new_bm()

    def bat_fur(p, n):
        sheen = 0.5 + 0.5 * math.sin(p.y * 20.0 + p.z * 18.0)
        return _lerp3((0.06, 0.04, 0.10), (0.24, 0.12, 0.32), max(0.0, n.z) * 0.55 + 0.25 * sheen)

    def violet_glow(p, n):
        return _lerp3((0.46, 0.10, 0.78), (0.82, 0.30, 0.98), _clamp01((p.z - 0.3) * 1.4))

    # Thân dơi bay chao lượn nghiêng 12° sinh động
    _add_ellipsoid(bm, col, (0, 0.04, 0.46), (0.12, 0.22, 0.13), segs=18, rings=12, rot=(-0.22, 0.18, -0.10), mat_index=0, color_fn=bat_fur)
    _add_ellipsoid(bm, col, (0.02, -0.18, 0.54), (0.095, 0.10, 0.092), segs=16, rings=10, rot=(0.06, 0.14, -0.12), mat_index=0, color_fn=bat_fur)
    _add_ellipsoid(bm, col, (0.02, -0.16, 0.47), (0.075, 0.070, 0.055), segs=12, rings=8, mat_index=2, color_fn=violet_glow)

    # Đặc trưng: Cặp Sừng Âm Thoa Pha Lê Tím chữ U mọc từ trán + tai dơi lớn
    _add_tube(bm, col, [(0, -0.18, 0.62), (0, -0.18, 0.69)], [0.026, 0.022], sides=6, subdiv=0, mat_index=2, color_fn=violet_glow, flat_faces=flat, is_flat=True)
    for sx in (-1.0, 1.0):
        _add_tube(
            bm, col,
            [(0, -0.18, 0.68), (sx * 0.075, -0.18, 0.74), (sx * 0.088, -0.18, 0.90), (sx * 0.084, -0.18, 1.02)],
            [0.020, 0.019, 0.017, 0.010], sides=6, subdiv=1, mat_index=2, color_fn=violet_glow, flat_faces=flat, is_flat=True
        )
        # Tai dơi vểnh thu âm
        _add_3d_leaf_blade(bm, col, (sx * 0.075, -0.14, 0.59), Vector((sx * 0.42, 0.18, 0.88)), Vector((0.0, -1.0, 0.0)), 0.16, 0.045, thickness=0.012, curl=0.08, vein_mat=5, color_fn=bat_fur)
        _add_eye_bulb(bm, col, (sx * 0.068, -0.23, 0.56), 0.022, yaw=sx * 0.45, eye_col=(0.94, 0.18, 0.52))

    # Đôi cánh sải ~1.18m có xương ngón tay + màng cánh 3D căng liền giữa các đốt xương
    for sx in (-1.0, 1.0):
        shoulder = Vector((sx * 0.10, -0.06, 0.50))
        elbow = Vector((sx * 0.28, 0.02, 0.44))
        wrist = Vector((sx * 0.44, -0.14, 0.62))
        tip1 = Vector((sx * 0.68, -0.02, 0.64))
        tip2 = Vector((sx * 0.58, 0.22, 0.42))
        tip3 = Vector((sx * 0.34, 0.26, 0.34))
        hip = Vector((sx * 0.09, 0.18, 0.40))
        for p_a, p_b in [(shoulder, elbow), (elbow, wrist), (wrist, tip1), (wrist, tip2), (wrist, tip3)]:
            _add_tube(bm, col, [p_a, p_b], [0.020, 0.011], sides=8, subdiv=0, mat_index=1, color_fn=bat_fur)
        _add_wing_panel_3d(bm, col, wrist, [elbow, shoulder, hip, tip3, tip2, tip1], thickness=0.012, mat_index=4, color_fn=lambda p, _n: _lerp3((0.16, 0.06, 0.26), (0.38, 0.10, 0.56), _clamp01(abs(p.x) * 1.4)))
        # Móng chân sau bám nhẹ dưới sàn
        _add_tube(bm, col, [(sx * 0.06, 0.20, 0.38), (sx * 0.08, 0.26, 0.18), (sx * 0.07, 0.22, 0.01)], [0.022, 0.015, 0.008], sides=8, subdiv=1, mat_index=0, color_fn=bat_fur)

    bones_09 = [
        ("Root", (0, 0.06, 0.46), (0, -0.10, 0.50), None, 0.28, 1.0),
        ("Head", (0.01, -0.12, 0.52), (0.02, -0.24, 0.56), "Root", 0.16, 2.2),
        ("ForkHorns", (0.02, -0.18, 0.65), (0.02, -0.18, 1.02), "Head", 0.16, 2.8),
        ("Throat", (0.02, -0.16, 0.47), (0.02, -0.22, 0.45), "Head", 0.12, 2.5),
        ("Wing_L1", (-0.10, -0.06, 0.50), (-0.44, -0.14, 0.62), "Root", 0.20, 2.5),
        ("Wing_L2", (-0.44, -0.14, 0.62), (-0.68, 0.04, 0.56), "Wing_L1", 0.24, 2.8),
        ("Wing_R1", (0.10, -0.06, 0.50), (0.44, -0.14, 0.62), "Root", 0.20, 2.5),
        ("Wing_R2", (0.44, -0.14, 0.62), (0.68, 0.04, 0.56), "Wing_R1", 0.24, 2.8),
        ("Feet", (0, 0.20, 0.38), (0, 0.22, 0.02), "Root", 0.16, 2.4),
    ]
    clips_09 = {
        "Idle": (1, 32, [
            (1, {"Root": {"loc": (0, 0, 0)}, "Wing_L1": {"rot": (0, 0.28, 0)}, "Wing_R1": {"rot": (0, -0.28, 0)}, "Wing_L2": {"rot": (0, 0.22, 0)}, "Wing_R2": {"rot": (0, -0.22, 0)}, "Head": {"rot": (0.06, 0, 0.10)}}),
            (16, {"Root": {"loc": (0, 0, 0.05)}, "Wing_L1": {"rot": (0, -0.52, 0.10)}, "Wing_R1": {"rot": (0, 0.52, -0.10)}, "Wing_L2": {"rot": (0, -0.40, 0)}, "Wing_R2": {"rot": (0, 0.40, 0)}, "Head": {"rot": (-0.08, 0, -0.12)}}),
            (32, {"Root": {"loc": (0, 0, 0)}, "Wing_L1": {"rot": (0, 0.28, 0)}, "Wing_R1": {"rot": (0, -0.28, 0)}, "Wing_L2": {"rot": (0, 0.22, 0)}, "Wing_R2": {"rot": (0, -0.22, 0)}, "Head": {"rot": (0.06, 0, 0.10)}}),
        ]),
        "Walk": (1, 20, [
            (1, {"Root": {"rot": (0.20, 0.14, 0), "loc": (0, 0, 0.04)}, "Wing_L1": {"rot": (0, -0.68, 0.15)}, "Wing_R1": {"rot": (0, 0.68, -0.15)}, "Wing_L2": {"rot": (0, -0.45, 0)}, "Wing_R2": {"rot": (0, 0.45, 0)}, "Feet": {"rot": (0.35, 0, 0)}}),
            (10, {"Root": {"rot": (0.24, -0.14, 0), "loc": (0, 0, 0.10)}, "Wing_L1": {"rot": (0, 0.55, -0.15)}, "Wing_R1": {"rot": (0, -0.55, 0.15)}, "Wing_L2": {"rot": (0, 0.40, 0)}, "Wing_R2": {"rot": (0, -0.40, 0)}, "Feet": {"rot": (0.25, 0, 0)}}),
            (20, {"Root": {"rot": (0.20, 0.14, 0), "loc": (0, 0, 0.04)}, "Wing_L1": {"rot": (0, -0.68, 0.15)}, "Wing_R1": {"rot": (0, 0.68, -0.15)}, "Wing_L2": {"rot": (0, -0.45, 0)}, "Wing_R2": {"rot": (0, 0.45, 0)}, "Feet": {"rot": (0.35, 0, 0)}}),
        ]),
        "Action": (1, 24, [
            (1, {"Head": {"rot": (0, 0, 0)}, "Throat": {"scale": (1, 1, 1)}, "ForkHorns": {"scale": (1, 1, 1)}}),
            (8, {"Head": {"rot": (-0.24, 0, 0)}, "Throat": {"scale": (1.42, 1.42, 1.42)}, "ForkHorns": {"scale": (1.18, 1.18, 1.12)}, "Wing_L1": {"rot": (0, 0.45, -0.25)}, "Wing_R1": {"rot": (0, -0.45, 0.25)}}),
            (24, {"Head": {"rot": (0, 0, 0)}, "Throat": {"scale": (1, 1, 1)}, "ForkHorns": {"scale": (1, 1, 1)}, "Wing_L1": {"rot": (0, 0, 0)}, "Wing_R1": {"rot": (0, 0, 0)}}),
        ]),
        "Hit": (1, 16, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (5, {"Root": {"rot": (-0.30, 0.40, 0.30)}, "Wing_L1": {"rot": (0, 0.60, 0)}, "Wing_R1": {"rot": (0, -0.60, 0)}}),
            (16, {"Root": {"rot": (0, 0, 0)}, "Wing_L1": {"rot": (0, 0, 0)}, "Wing_R1": {"rot": (0, 0, 0)}}),
        ]),
    }

    _finalize_and_export(
        bm, flat, mats, "Creature_DoiAmThoa",
        os.path.join(out_dir, "Creature_DoiAmThoa.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_09_doi_am_thoa.png") if prev_dir else None,
        bone_specs=bones_09, clips_spec=clips_09,
    )


def build_10_bo_gai_tim(out_dir, prev_dir):
    """[M] Bọ Gai Tím: bọ giáp đá hắc diện tủa gai tinh thể thạch anh tím sắc nhọn."""
    kc.new_scene()
    mats = _make_creature_materials()
    bm, col, flat = _new_bm()
    rng = random.Random(510)

    def obsidian_shell(p, n):
        crack = 1.0 - abs(noise.noise(p * 8.0))
        base = _lerp3((0.05, 0.04, 0.09), (0.24, 0.12, 0.32), max(0.0, n.z) * 0.65 + 0.15)
        if crack > 0.80:
            base = _lerp3(base, (0.64, 0.10, 0.28), 0.7)
        return base

    def amethyst_spine(p, n):
        return _lerp3((0.36, 0.06, 0.66), (0.82, 0.26, 0.94), _clamp01((p.z - 0.18) * 1.8))

    # Thân dẹt vát góc đá hắc diện
    _add_faceted_chunk(bm, col, (0, 0.04, 0.20), (0.26, 0.35, 0.14), 4.4, (0.05, 0, 0), mat_index=1, color_fn=obsidian_shell, flat_faces=flat)
    _add_faceted_chunk(bm, col, (0, -0.32, 0.18), (0.14, 0.13, 0.10), 2.3, (0.15, 0, 0), mat_index=1, color_fn=obsidian_shell, flat_faces=flat)

    # 15 Gai Tinh Thể Thạch Anh Tím nhọn hoắt tua tủa trên lưng ("Cắm Gai")
    spines = [
        (0.0, -0.08, 0.28, (0.0, 0.18, 0.98), 0.34, 0.055),
        (0.0, 0.12, 0.28, (0.0, 0.36, 0.93), 0.36, 0.058),
        (0.0, 0.28, 0.24, (0.0, 0.58, 0.81), 0.28, 0.046),
    ]
    for sx in (-1.0, 1.0):
        spines.extend([
            (sx * 0.14, -0.12, 0.25, (sx * 0.42, 0.12, 0.89), 0.26, 0.042),
            (sx * 0.18, 0.04, 0.25, (sx * 0.55, 0.22, 0.80), 0.29, 0.044),
            (sx * 0.16, 0.22, 0.23, (sx * 0.52, 0.48, 0.70), 0.25, 0.040),
        ])
        _add_eye_bulb(bm, col, (sx * 0.095, -0.38, 0.21), 0.024, yaw=sx * 0.5, eye_col=(0.94, 0.16, 0.40))
        _add_tube(bm, col, [(sx * 0.06, -0.40, 0.15), (sx * 0.10, -0.50, 0.15), (sx * 0.03, -0.55, 0.17)], [0.020, 0.013, 0.004], sides=6, subdiv=0, mat_index=2, color_fn=amethyst_spine, flat_faces=flat, is_flat=True)

    for px, py, pz, d_v, l_v, r_v in spines:
        _add_crystal_prism(bm, col, (px, py, pz), Vector(d_v), l_v, r_v, r_v * 0.55, sides=5, rot_twist=rng.uniform(0, 3.0), mat_index=2, color_fn=amethyst_spine, flat_faces=flat)

    # 6 chân góc cạnh bấm nền
    for sx, py, spread in [(-1, -0.18, 0.30), (1, -0.18, 0.30), (-1, 0.02, 0.36), (1, 0.02, 0.36), (-1, 0.22, 0.33), (1, 0.22, 0.33)]:
        _add_tube(
            bm, col,
            [(sx * 0.18, py, 0.17), (sx * spread, py - 0.02, 0.24), (sx * (spread + 0.06), py + 0.03, 0.09), (sx * (spread + 0.03), py + 0.04, 0.008)],
            [0.032, 0.022, 0.016, 0.007], sides=6, subdiv=0, mat_index=1, color_fn=obsidian_shell, flat_faces=flat, is_flat=True
        )

    bones_10 = [
        ("Root", (0, 0.04, 0.20), (0, -0.18, 0.20), None, 0.42, 1.0),
        ("Head", (0, -0.26, 0.19), (0, -0.44, 0.18), "Root", 0.22, 2.2),
        ("Spines", (0, 0.04, 0.28), (0, 0.18, 0.56), "Root", 0.34, 1.8),
        ("Leg_FL", (-0.18, -0.18, 0.17), (-0.33, -0.14, 0.01), "Root", 0.16, 2.5),
        ("Leg_FR", (0.18, -0.18, 0.17), (0.33, -0.14, 0.01), "Root", 0.16, 2.5),
        ("Leg_ML", (-0.18, 0.02, 0.17), (-0.39, 0.06, 0.01), "Root", 0.16, 2.5),
        ("Leg_MR", (0.18, 0.02, 0.17), (0.39, 0.06, 0.01), "Root", 0.16, 2.5),
        ("Leg_BL", (-0.18, 0.22, 0.17), (-0.36, 0.26, 0.01), "Root", 0.16, 2.5),
        ("Leg_BR", (0.18, 0.22, 0.17), (0.36, 0.26, 0.01), "Root", 0.16, 2.5),
    ]
    clips_10 = {
        "Idle": (1, 48, [
            (1, {"Root": {"loc": (0, 0, 0)}, "Head": {"rot": (0.04, 0, 0.08)}, "Spines": {"scale": (1, 1, 1)}}),
            (24, {"Root": {"loc": (0, 0, 0.015)}, "Head": {"rot": (-0.06, 0, -0.10)}, "Spines": {"scale": (1.04, 1.04, 1.08)}}),
            (48, {"Root": {"loc": (0, 0, 0)}, "Head": {"rot": (0.04, 0, 0.08)}, "Spines": {"scale": (1, 1, 1)}}),
        ]),
        "Walk": (1, 20, [
            (1, {"Root": {"rot": (0, 0.08, 0.05)}, "Leg_FL": {"rot": (0.48, 0, 0.20)}, "Leg_MR": {"rot": (0.48, 0, -0.20)}, "Leg_BL": {"rot": (0.48, 0, 0.20)}, "Leg_FR": {"rot": (-0.38, 0, 0)}, "Leg_ML": {"rot": (-0.38, 0, 0)}, "Leg_BR": {"rot": (-0.38, 0, 0)}}),
            (10, {"Root": {"rot": (0, -0.08, -0.05)}, "Leg_FL": {"rot": (-0.38, 0, 0)}, "Leg_MR": {"rot": (-0.38, 0, 0)}, "Leg_BL": {"rot": (-0.38, 0, 0)}, "Leg_FR": {"rot": (0.48, 0, -0.20)}, "Leg_ML": {"rot": (0.48, 0, 0.20)}, "Leg_BR": {"rot": (0.48, 0, -0.20)}}),
            (20, {"Root": {"rot": (0, 0.08, 0.05)}, "Leg_FL": {"rot": (0.48, 0, 0.20)}, "Leg_MR": {"rot": (0.48, 0, -0.20)}, "Leg_BL": {"rot": (0.48, 0, 0.20)}, "Leg_FR": {"rot": (-0.38, 0, 0)}, "Leg_ML": {"rot": (-0.38, 0, 0)}, "Leg_BR": {"rot": (-0.38, 0, 0)}}),
        ]),
        "Attack": (1, 22, [
            (1, {"Root": {"loc": (0, 0, 0)}, "Spines": {"scale": (1, 1, 1)}}),
            (7, {"Root": {"loc": (0, 0, 0.08), "rot": (-0.22, 0, 0)}, "Spines": {"scale": (1.12, 1.12, 1.18)}}),
            (12, {"Root": {"loc": (0, 0, -0.04), "rot": (0.14, 0, 0)}, "Spines": {"scale": (1.15, 1.15, 1.25)}}),
            (22, {"Root": {"loc": (0, 0, 0), "rot": (0, 0, 0)}, "Spines": {"scale": (1, 1, 1)}}),
        ]),
        "Hit": (1, 16, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (5, {"Root": {"rot": (0.18, 0, 0.24)}}),
            (16, {"Root": {"rot": (0, 0, 0)}}),
        ]),
    }

    _finalize_and_export(
        bm, flat, mats, "Creature_BoGaiTim",
        os.path.join(out_dir, "Creature_BoGaiTim.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_10_bo_gai_tim.png") if prev_dir else None,
        bone_specs=bones_10, clips_spec=clips_10,
    )


def build_11_nhen_tinh_the(out_dir, prev_dir):
    """[S] Nhện Tinh Thể: nhện 8 chân khớp pha lê, bụng geode thạch anh tím & 6 mắt đỏ máu."""
    kc.new_scene()
    mats = _make_creature_materials()
    bm, col, flat = _new_bm()

    def obsidian_col(p, n):
        crack = 1.0 - abs(noise.noise(p * 7.0))
        base = _lerp3((0.05, 0.04, 0.085), (0.22, 0.12, 0.30), max(0.0, n.z) * 0.65 + 0.18)
        if crack > 0.82:
            base = _lerp3(base, (0.62, 0.08, 0.24), 0.65)
        return base

    def violet_core(p, n):
        return _lerp3((0.32, 0.05, 0.60), (0.86, 0.26, 0.96), _clamp01((p.z - 0.20) * 1.4))

    # Đầu ngực hắc diện + Bụng khối Geode Thạch Anh Tím góc cạnh lớn (subdiv=2) phía sau
    _add_faceted_chunk(bm, col, (0, -0.22, 0.36), (0.20, 0.24, 0.15), 2.2, (0.05, 0, 0), mat_index=1, color_fn=obsidian_col, flat_faces=flat)
    _add_faceted_chunk(bm, col, (0, 0.24, 0.44), (0.29, 0.36, 0.26), 6.5, (-0.18, 0, 0), mat_index=2, color_fn=violet_core, flat_faces=flat, subdiv=2)
    # Các gai tinh thể vểnh sau bụng nhện
    for sx, ang_y in [(-0.14, 0.35), (0.0, 0.55), (0.14, 0.35)]:
        _add_crystal_prism(bm, col, (sx, 0.38, 0.58), Vector((sx * 1.6, 0.55, 0.78)), 0.30, 0.058, 0.032, sides=6, mat_index=2, color_fn=violet_core, flat_faces=flat)

    # Cụm 6 mắt đỏ máu & cặp nanh pha lê tím cong xuống
    for ex, ey, ez, er in [(-0.055, -0.43, 0.40, 0.028), (0.055, -0.43, 0.40, 0.028), (-0.115, -0.39, 0.39, 0.020), (0.115, -0.39, 0.39, 0.020), (-0.03, -0.44, 0.44, 0.016), (0.03, -0.44, 0.44, 0.016)]:
        _add_eye_bulb(bm, col, (ex, ey, ez), er, yaw=ex * 2.0, eye_col=(0.98, 0.12, 0.22))
    for sx in (-1.0, 1.0):
        _add_crystal_prism(bm, col, (sx * 0.065, -0.42, 0.32), Vector((sx * 0.10, -0.45, -0.88)), 0.17, 0.030, 0.016, sides=5, mat_index=2, color_fn=violet_core, flat_faces=flat)
        _add_tube(bm, col, [(sx * 0.13, -0.38, 0.32), (sx * 0.24, -0.52, 0.35), (sx * 0.16, -0.60, 0.22)], [0.030, 0.022, 0.012], sides=6, subdiv=0, mat_index=1, color_fn=obsidian_col, flat_faces=flat, is_flat=True)

    # 8 chân nhện dài khớp tinh thể tím (sải ~1.55m)
    leg_angles = [-0.85, -0.35, 0.22, 0.78]
    for sx in (-1.0, 1.0):
        for idx, la in enumerate(leg_angles):
            hip = Vector((sx * 0.14, -0.22 + idx * 0.07, 0.33))
            dx, dy = sx * math.cos(la), math.sin(la)
            knee = Vector((hip.x + dx * 0.34, hip.y + dy * 0.30, 0.64 + (idx % 2) * 0.06))
            ankle = Vector((hip.x + dx * 0.62, hip.y + dy * 0.56, 0.28))
            foot = Vector((hip.x + dx * 0.70, hip.y + dy * 0.64, 0.01))
            _add_tube(bm, col, [hip, knee], [0.036, 0.028], sides=6, subdiv=0, mat_index=1, color_fn=obsidian_col, flat_faces=flat, is_flat=True)
            _add_crystal_prism(bm, col, knee - Vector((0, 0, 0.02)), Vector((dx * 0.3, dy * 0.3, 0.9)), 0.12, 0.034, 0.018, sides=5, mat_index=2, color_fn=violet_core, flat_faces=flat)
            _add_tube(bm, col, [knee, ankle, foot], [0.028, 0.020, 0.008], sides=6, subdiv=0, mat_index=1, color_fn=obsidian_col, flat_faces=flat, is_flat=True)

    bones_11 = [
        ("Root", (0, -0.14, 0.36), (0, -0.34, 0.36), None, 0.34, 1.0),
        ("Abdomen", (0, 0.06, 0.42), (0, 0.44, 0.50), "Root", 0.42, 2.2),
        ("Fangs", (0, -0.40, 0.32), (0, -0.52, 0.20), "Root", 0.16, 2.6),
    ]
    for sx_n, sx_s in [("L", -1.0), ("R", 1.0)]:
        for idx, la in enumerate([-0.85, -0.35, 0.22, 0.78]):
            hip = (sx_s * 0.14, -0.22 + idx * 0.07, 0.33)
            dx, dy = sx_s * math.cos(la), math.sin(la)
            foot = (hip[0] + dx * 0.70, hip[1] + dy * 0.64, 0.01)
            bones_11.append((f"Leg_{sx_n}{idx + 1}", hip, foot, "Root", 0.24, 2.6))

    clips_11 = {
        "Idle": (1, 48, [
            (1, {"Root": {"loc": (0, 0, 0)}, "Abdomen": {"rot": (0.06, 0, 0)}, "Fangs": {"rot": (0.10, 0, 0)}, "Leg_L1": {"rot": (-0.12, 0, 0)}, "Leg_R1": {"rot": (-0.08, 0, 0)}}),
            (24, {"Root": {"loc": (0, 0, 0.03)}, "Abdomen": {"rot": (-0.10, 0, 0.06)}, "Fangs": {"rot": (-0.18, 0, 0)}, "Leg_L1": {"rot": (0.12, 0, 0)}, "Leg_R1": {"rot": (0.16, 0, 0)}}),
            (48, {"Root": {"loc": (0, 0, 0)}, "Abdomen": {"rot": (0.06, 0, 0)}, "Fangs": {"rot": (0.10, 0, 0)}, "Leg_L1": {"rot": (-0.12, 0, 0)}, "Leg_R1": {"rot": (-0.08, 0, 0)}}),
        ]),
        "Walk": (1, 16, [
            (1, {"Root": {"loc": (0, 0, 0.02), "rot": (0, 0.06, 0)}, "Leg_L1": {"rot": (0.45, 0, 0.2)}, "Leg_R2": {"rot": (0.45, 0, -0.2)}, "Leg_L3": {"rot": (0.45, 0, 0.2)}, "Leg_R4": {"rot": (0.45, 0, -0.2)}, "Leg_R1": {"rot": (-0.38, 0, 0)}, "Leg_L2": {"rot": (-0.38, 0, 0)}, "Leg_R3": {"rot": (-0.38, 0, 0)}, "Leg_L4": {"rot": (-0.38, 0, 0)}}),
            (8, {"Root": {"loc": (0, 0, 0.02), "rot": (0, -0.06, 0)}, "Leg_L1": {"rot": (-0.38, 0, 0)}, "Leg_R2": {"rot": (-0.38, 0, 0)}, "Leg_L3": {"rot": (-0.38, 0, 0)}, "Leg_R4": {"rot": (-0.38, 0, 0)}, "Leg_R1": {"rot": (0.45, 0, -0.2)}, "Leg_L2": {"rot": (0.45, 0, 0.2)}, "Leg_R3": {"rot": (0.45, 0, -0.2)}, "Leg_L4": {"rot": (0.45, 0, 0.2)}}),
            (16, {"Root": {"loc": (0, 0, 0.02), "rot": (0, 0.06, 0)}, "Leg_L1": {"rot": (0.45, 0, 0.2)}, "Leg_R2": {"rot": (0.45, 0, -0.2)}, "Leg_L3": {"rot": (0.45, 0, 0.2)}, "Leg_R4": {"rot": (0.45, 0, -0.2)}, "Leg_R1": {"rot": (-0.38, 0, 0)}, "Leg_L2": {"rot": (-0.38, 0, 0)}, "Leg_R3": {"rot": (-0.38, 0, 0)}, "Leg_L4": {"rot": (-0.38, 0, 0)}}),
        ]),
        "Attack": (1, 22, [
            (1, {"Root": {"loc": (0, 0, 0), "rot": (0, 0, 0)}}),
            (7, {"Root": {"loc": (0, 0.14, 0.14), "rot": (-0.34, 0, 0)}, "Leg_L1": {"rot": (-0.75, 0, 0)}, "Leg_R1": {"rot": (-0.75, 0, 0)}, "Fangs": {"rot": (-0.35, 0, 0)}}),
            (13, {"Root": {"loc": (0, -0.32, 0.02), "rot": (0.22, 0, 0)}, "Leg_L1": {"rot": (0.40, 0, 0)}, "Leg_R1": {"rot": (0.40, 0, 0)}, "Fangs": {"rot": (0.35, 0, 0)}}),
            (22, {"Root": {"loc": (0, 0, 0), "rot": (0, 0, 0)}, "Leg_L1": {"rot": (0, 0, 0)}, "Leg_R1": {"rot": (0, 0, 0)}, "Fangs": {"rot": (0, 0, 0)}}),
        ]),
        "Hit": (1, 16, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (5, {"Root": {"loc": (0, 0.10, -0.06), "rot": (0.20, 0, 0.24)}, "Abdomen": {"rot": (0.28, 0, 0)}}),
            (16, {"Root": {"loc": (0, 0, 0), "rot": (0, 0, 0)}, "Abdomen": {"rot": (0, 0, 0)}}),
        ]),
    }

    _finalize_and_export(
        bm, flat, mats, "Creature_NhenTinhThe",
        os.path.join(out_dir, "Creature_NhenTinhThe.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_11_nhen_tinh_the.png") if prev_dir else None,
        bone_specs=bones_11, clips_spec=clips_11,
    )


def build_12_bo_cap_thach_anh(out_dir, prev_dir):
    """[C] Bọ Cạp Thạch Anh (Chúa Hang): giáp hắc diện phân đốt, càng khổng lồ & đuôi vút ngòi độc pha lê."""
    kc.new_scene()
    mats = _make_creature_materials()
    bm, col, flat = _new_bm()

    def scorp_obsidian(p, n):
        crack = 1.0 - abs(noise.noise(p * 6.5))
        base = _lerp3((0.05, 0.04, 0.09), (0.28, 0.14, 0.38), max(0.0, n.z) * 0.68 + 0.16)
        if crack > 0.82:
            base = _lerp3(base, (0.68, 0.10, 0.30), 0.65)
        return base

    def scorp_quartz(p, n):
        return _lerp3((0.40, 0.06, 0.68), (0.94, 0.26, 0.88), _clamp01((p.z - 0.35) * 0.8))

    # Đầu giáp & 6 đốt thân giáp phân tầng
    _add_faceted_chunk(bm, col, (0, -0.52, 0.36), (0.34, 0.32, 0.18), 1.4, (0.08, 0, 0), mat_index=1, color_fn=scorp_obsidian, flat_faces=flat)
    for k in range(6):
        py = -0.22 + k * 0.16
        w = 0.38 - k * 0.028
        _add_faceted_chunk(bm, col, (0, py, 0.37 - k * 0.008), (w, 0.14, 0.16), k * 2.5, (-0.12, 0, 0), mat_index=1, color_fn=scorp_obsidian, flat_faces=flat)
        if k in (1, 3, 5):
            for sx in (-1.0, 1.0):
                _add_crystal_prism(bm, col, (sx * (w * 0.72), py, 0.44), Vector((sx * 0.45, 0.2, 0.86)), 0.18, 0.042, 0.022, sides=5, mat_index=2, color_fn=scorp_quartz, flat_faces=flat)

    for sx in (-1.0, 1.0):
        _add_eye_bulb(bm, col, (sx * 0.10, -0.74, 0.44), 0.036, yaw=sx * 0.35, eye_col=(0.98, 0.16, 0.32))
        _add_eye_bulb(bm, col, (sx * 0.22, -0.68, 0.40), 0.022, yaw=sx * 0.55, eye_col=(0.98, 0.16, 0.32))

    # Cặp Càng Kẹp Pha Lê Hắc Diện Khổng Lồ vươn ra trước
    for sx in (-1.0, 1.0):
        sh = Vector((sx * 0.32, -0.58, 0.34))
        el = Vector((sx * 0.68, -0.76, 0.36))
        wr = Vector((sx * 0.64, -1.04, 0.40))
        _add_tube(bm, col, [sh, el, wr], [0.095, 0.088, 0.085], sides=6, subdiv=0, mat_index=1, color_fn=scorp_obsidian, flat_faces=flat, is_flat=True)
        # Bàn kẹp đa diện to khỏe + hai ngàm kẹp nhọn pha lê
        _add_faceted_chunk(bm, col, wr + Vector((0, -0.15, 0.02)), (0.17, 0.24, 0.14), sx * 3.1, (0, 0, sx * -0.2), mat_index=1, color_fn=scorp_obsidian, flat_faces=flat)
        _add_crystal_prism(bm, col, wr + Vector((sx * 0.06, -0.28, 0.03)), Vector((-sx * 0.30, -0.94, 0.08)), 0.36, 0.068, 0.034, sides=5, mat_index=2, color_fn=scorp_quartz, flat_faces=flat)
        _add_crystal_prism(bm, col, wr + Vector((-sx * 0.07, -0.28, 0.01)), Vector((sx * 0.16, -0.98, 0.04)), 0.32, 0.056, 0.028, sides=5, mat_index=2, color_fn=scorp_quartz, flat_faces=flat)

    # 8 chân bọ cạp giáp dày giữ thân vững
    for sx in (-1.0, 1.0):
        for idx, py in enumerate([-0.38, -0.18, 0.02, 0.22]):
            _add_tube(
                bm, col,
                [(sx * 0.28, py, 0.30), (sx * 0.56, py - 0.04, 0.42), (sx * 0.68, py + 0.02, 0.20), (sx * 0.66, py + 0.05, 0.01)],
                [0.062, 0.048, 0.036, 0.018], sides=6, subdiv=0, mat_index=1, color_fn=scorp_obsidian, flat_faces=flat, is_flat=True
            )

    # Đuôi 6 đốt cong vút lên cao qua đỉnh đầu + Telson & Ngòi Độc Thạch Anh Tím ("Châm Choáng")
    tail_pts = [
        (0, 0.68, 0.36), (0, 0.94, 0.54), (0, 1.02, 0.86),
        (0, 0.84, 1.18), (0, 0.46, 1.42), (0, 0.02, 1.52), (0, -0.34, 1.44),
    ]
    for i in range(len(tail_pts) - 1):
        p0 = Vector(tail_pts[i])
        p1 = Vector(tail_pts[i + 1])
        r0 = 0.14 - i * 0.014
        r1 = 0.13 - i * 0.014
        _add_tube(bm, col, [p0, p1], [r0, r1], sides=6, subdiv=0, mat_index=1, color_fn=scorp_obsidian, flat_faces=flat, is_flat=True)
        _add_crystal_prism(bm, col, p0.lerp(p1, 0.5), (p1 - p0).cross(Vector((1, 0, 0))).normalized() * -1.0, 0.14, 0.032, 0.018, sides=5, mat_index=2, color_fn=scorp_quartz, flat_faces=flat)

    # Telson bầu độc pha lê tím & mũi kim độc chĩa tới
    telson_p = Vector((0, -0.44, 1.38))
    _add_faceted_chunk(bm, col, telson_p, (0.12, 0.16, 0.12), 7.7, (-0.35, 0, 0), mat_index=2, color_fn=scorp_quartz, flat_faces=flat)
    _add_crystal_prism(bm, col, telson_p + Vector((0, -0.10, -0.02)), Vector((0.0, -0.88, -0.46)), 0.32, 0.048, 0.022, sides=6, mat_index=2, color_fn=lambda _p, _n: (0.98, 0.32, 0.92), flat_faces=flat)

    bones_12 = [
        ("Root", (0, 0.08, 0.36), (0, -0.42, 0.36), None, 0.65, 1.0),
        ("Head", (0, -0.44, 0.36), (0, -0.76, 0.40), "Root", 0.34, 2.0),
        ("Arm_L", (-0.32, -0.58, 0.34), (-0.64, -1.04, 0.40), "Root", 0.28, 2.5),
        ("Claw_L", (-0.64, -1.04, 0.40), (-0.64, -1.42, 0.44), "Arm_L", 0.28, 2.8),
        ("Arm_R", (0.32, -0.58, 0.34), (0.64, -1.04, 0.40), "Root", 0.28, 2.5),
        ("Claw_R", (0.64, -1.04, 0.40), (0.64, -1.42, 0.44), "Arm_R", 0.28, 2.8),
        ("Tail1", (0, 0.68, 0.36), (0, 1.02, 0.86), "Root", 0.28, 2.2),
        ("Tail2", (0, 1.02, 0.86), (0, 0.46, 1.42), "Tail1", 0.26, 2.4),
        ("Tail3", (0, 0.46, 1.42), (0, -0.34, 1.44), "Tail2", 0.24, 2.6),
        ("Stinger", (0, -0.34, 1.44), (0, -0.68, 1.24), "Tail3", 0.22, 3.0),
        ("Legs_L", (-0.38, -0.10, 0.24), (-0.66, -0.04, 0.01), "Root", 0.42, 2.2),
        ("Legs_R", (0.38, -0.10, 0.24), (0.66, -0.04, 0.01), "Root", 0.42, 2.2),
    ]
    clips_12 = {
        "Idle": (1, 56, [
            (1, {"Arm_L": {"rot": (0.04, 0, 0.06)}, "Arm_R": {"rot": (-0.04, 0, -0.06)}, "Tail1": {"rot": (0.06, 0, 0)}, "Tail2": {"rot": (0.08, 0, 0)}, "Stinger": {"rot": (0.10, 0, 0)}}),
            (28, {"Arm_L": {"rot": (-0.08, 0, -0.10)}, "Arm_R": {"rot": (0.08, 0, 0.10)}, "Tail1": {"rot": (-0.08, 0, 0.06)}, "Tail2": {"rot": (-0.12, 0, 0)}, "Stinger": {"rot": (-0.14, 0, 0)}}),
            (56, {"Arm_L": {"rot": (0.04, 0, 0.06)}, "Arm_R": {"rot": (-0.04, 0, -0.06)}, "Tail1": {"rot": (0.06, 0, 0)}, "Tail2": {"rot": (0.08, 0, 0)}, "Stinger": {"rot": (0.10, 0, 0)}}),
        ]),
        "Walk": (1, 24, [
            (1, {"Root": {"rot": (0, 0.06, 0.04)}, "Legs_L": {"rot": (0.38, 0, 0.18)}, "Legs_R": {"rot": (-0.32, 0, 0)}, "Arm_L": {"rot": (0, 0, 0.16)}, "Arm_R": {"rot": (0, 0, -0.10)}, "Tail2": {"rot": (0, 0, -0.10)}}),
            (12, {"Root": {"rot": (0, -0.06, -0.04)}, "Legs_L": {"rot": (-0.32, 0, 0)}, "Legs_R": {"rot": (0.38, 0, -0.18)}, "Arm_L": {"rot": (0, 0, -0.10)}, "Arm_R": {"rot": (0, 0, 0.16)}, "Tail2": {"rot": (0, 0, 0.10)}}),
            (24, {"Root": {"rot": (0, 0.06, 0.04)}, "Legs_L": {"rot": (0.38, 0, 0.18)}, "Legs_R": {"rot": (-0.32, 0, 0)}, "Arm_L": {"rot": (0, 0, 0.16)}, "Arm_R": {"rot": (0, 0, -0.10)}, "Tail2": {"rot": (0, 0, -0.10)}}),
        ]),
        "Warn": (1, 28, [
            (1, {"Arm_L": {"rot": (0, 0, 0)}, "Arm_R": {"rot": (0, 0, 0)}, "Tail1": {"rot": (0, 0, 0)}}),
            (12, {"Root": {"loc": (0, 0.10, 0.06), "rot": (-0.16, 0, 0)}, "Arm_L": {"rot": (-0.45, 0, 0.25)}, "Arm_R": {"rot": (-0.45, 0, -0.25)}, "Tail1": {"rot": (-0.22, 0, 0)}, "Tail2": {"rot": (-0.20, 0, 0)}}),
            (28, {"Root": {"loc": (0, 0, 0), "rot": (0, 0, 0)}, "Arm_L": {"rot": (0, 0, 0)}, "Arm_R": {"rot": (0, 0, 0)}, "Tail1": {"rot": (0, 0, 0)}, "Tail2": {"rot": (0, 0, 0)}}),
        ]),
        "Attack": (1, 24, [
            (1, {"Tail1": {"rot": (0, 0, 0)}, "Tail2": {"rot": (0, 0, 0)}, "Tail3": {"rot": (0, 0, 0)}, "Stinger": {"rot": (0, 0, 0)}}),
            (7, {"Root": {"loc": (0, 0.14, 0.04), "rot": (-0.14, 0, 0)}, "Tail1": {"rot": (0.28, 0, 0)}, "Tail2": {"rot": (0.35, 0, 0)}, "Arm_L": {"rot": (-0.35, 0, 0.20)}, "Arm_R": {"rot": (-0.35, 0, -0.20)}}),
            (13, {"Root": {"loc": (0, -0.34, 0.02), "rot": (0.18, 0, 0)}, "Tail1": {"rot": (-0.38, 0, 0)}, "Tail2": {"rot": (-0.48, 0, 0)}, "Tail3": {"rot": (-0.42, 0, 0)}, "Stinger": {"rot": (-0.35, 0, 0)}, "Arm_L": {"rot": (0.22, 0, -0.28)}, "Arm_R": {"rot": (0.22, 0, 0.28)}}),
            (24, {"Root": {"loc": (0, 0, 0), "rot": (0, 0, 0)}, "Tail1": {"rot": (0, 0, 0)}, "Tail2": {"rot": (0, 0, 0)}, "Tail3": {"rot": (0, 0, 0)}, "Stinger": {"rot": (0, 0, 0)}, "Arm_L": {"rot": (0, 0, 0)}, "Arm_R": {"rot": (0, 0, 0)}}),
        ]),
        "Hit": (1, 18, [
            (1, {"Root": {"rot": (0, 0, 0)}}),
            (6, {"Root": {"loc": (0, 0.14, 0), "rot": (-0.22, 0, 0.20)}, "Arm_L": {"rot": (-0.25, 0, 0)}, "Arm_R": {"rot": (-0.25, 0, 0)}}),
            (18, {"Root": {"loc": (0, 0, 0), "rot": (0, 0, 0)}, "Arm_L": {"rot": (0, 0, 0)}, "Arm_R": {"rot": (0, 0, 0)}}),
        ]),
    }

    _finalize_and_export(
        bm, flat, mats, "Creature_BoCapThachAnh",
        os.path.join(out_dir, "Creature_BoCapThachAnh.fbx") if out_dir else None,
        os.path.join(prev_dir, "creature_12_bo_cap_thach_anh.png") if prev_dir else None,
        bone_specs=bones_12, clips_spec=clips_12,
    )


BUILDERS = [
    ("bo_canh_cung", build_01_bo_canh_cung_nho),
    ("oc_ho", build_02_oc_ho),
    ("than_lan", build_03_than_lan_bo_ho),
    ("coc_ho_gia", build_04_coc_ho_gia),
    ("dom_dom", build_05_dom_dom_sac_mau),
    ("tho_la", build_06_tho_la),
    ("bo_giap_quang", build_07_bo_giap_quang),
    ("rua_pha_le", build_08_rua_pha_le),
    ("doi_am_thoa", build_09_doi_am_thoa),
    ("bo_gai_tim", build_10_bo_gai_tim),
    ("nhen_tinh_the", build_11_nhen_tinh_the),
    ("bo_cap_thach_anh", build_12_bo_cap_thach_anh),
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=DEFAULT_OUT_DIR)
    ap.add_argument("--previews", default=DEFAULT_PREV_DIR)
    ap.add_argument("--no-previews", action="store_true")
    ap.add_argument("--only", default=None)
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    args = ap.parse_args(argv)

    t0 = time.time()
    out_dir = os.path.abspath(args.out) if args.out else None
    prev_dir = None if args.no_previews else (os.path.abspath(args.previews) if args.previews else None)

    global _WAVE1_AUTO_REMESH
    _WAVE1_AUTO_REMESH = True
    for key, fn in BUILDERS:
        if args.only and args.only != key:
            continue
        fn(out_dir, prev_dir)

    print(f"Hoàn tất dựng sinh vật Wave 1 trong {time.time() - t0:.1f}s", flush=True)


if __name__ == "__main__":
    main()
