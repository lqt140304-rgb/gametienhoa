"""Hạ tầng dựng 3D dùng chung cho 3 phòng trùm (Golem, Chim Hang Mẹ, Bọ Cạp) trong Blender headless.

Quy ước trục (Blender Z-up, đơn vị mét):
  +X = Đông, -X = Tây, +Y = Bắc (Boss_Wait), -Y = Nam (Door_Mouth / Player_Entry), +Z = Lên.
Khi đưa vào Unity với localRotation = (0, 180, 0), tọa độ (X, Y, Z)_blender -> (X, Z, Y)_unity.
"""
import math
import os
import sys

import bpy  # phải đứng trước bmesh/mathutils
import bmesh
from mathutils import Vector, Matrix, Euler, noise

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_common as kit  # noqa: E402


# Thông số kích thước chuẩn theo docs/BOSS_ROOMS_BRIEF.md
ROOM_RADIUS = 32.0
DOOR_Y = -31.5
DOOR_WIDTH = 4.8
DOOR_HEIGHT = 7.2
VESTIBULE_DEPTH = 7.0
PLAYER_ENTRY_POS = (0.0, -26.0, 0.0)


def clamp01(x):
    return max(0.0, min(1.0, x))


def smoothstep(a, b, x):
    t = clamp01((x - a) / max(1e-5, b - a))
    return t * t * (3.0 - 2.0 * t)


def mix(a, b, t):
    t = clamp01(t)
    return (
        a[0] + (b[0] - a[0]) * t,
        a[1] + (b[1] - a[1]) * t,
        a[2] + (b[2] - a[2]) * t,
    )


def ridged(p):
    return 1.0 - abs(noise.noise(p))


def stepped(val, steps=5.0, sharp=0.75):
    s = val * steps
    fl = math.floor(s)
    frac = s - fl
    crisp = smoothstep(0.5 - sharp * 0.44, 0.5 + sharp * 0.44, frac)
    return (fl + crisp) / steps


def parallel_tube_rings(bm, path, radii, segs, wobble=None):
    """Dựng các vòng đỉnh dọc đường cong bằng Parallel Transport (không xoắn khi dựng đứng)."""
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
            r = max(0.02, radii[i] * (1.0 + w))
            ring.append(bm.verts.new(p + (norm * math.cos(ang) + binorm * math.sin(ang)) * r))
        rings.append(ring)
    return rings


def make_stone_material(name, roughness=0.85, bump_strength=0.72):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")

    vcol = nodes.new("ShaderNodeVertexColor")
    vcol.layer_name = "Col"
    links.new(vcol.outputs["Color"], bsdf.inputs["Base Color"])

    tc = nodes.new("ShaderNodeTexCoord")
    n1 = nodes.new("ShaderNodeTexNoise")
    n1.inputs["Scale"].default_value = 0.55
    n1.inputs["Detail"].default_value = 9.0
    n1.inputs["Roughness"].default_value = 0.78
    links.new(tc.outputs["Object"], n1.inputs["Vector"])

    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = bump_strength
    bump.inputs["Distance"].default_value = 0.22
    links.new(n1.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    bsdf.inputs["Roughness"].default_value = roughness
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def make_glow_material(name, energy=0.90):
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
    bsdf.inputs["Roughness"].default_value = 0.14
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def make_steel_material(name):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")

    vcol = nodes.new("ShaderNodeVertexColor")
    vcol.layer_name = "Col"
    links.new(vcol.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Metallic"].default_value = 0.72
    bsdf.inputs["Roughness"].default_value = 0.54

    tc = nodes.new("ShaderNodeTexCoord")
    n1 = nodes.new("ShaderNodeTexNoise")
    n1.inputs["Scale"].default_value = 2.8
    n1.inputs["Detail"].default_value = 7.0
    links.new(tc.outputs["Object"], n1.inputs["Vector"])

    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.40
    bump.inputs["Distance"].default_value = 0.05
    links.new(n1.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def add_hex_crystal(bm, base, direction, length, radius, seed=0):
    """Thêm một trụ tinh thể lục giác sắc cạnh."""
    d = direction.normalized()
    ref = Vector((1.0, 0.0, 0.0)) if abs(d.z) > 0.9 else Vector((0.0, 0.0, 1.0))
    u = d.cross(ref).normalized()
    v = d.cross(u).normalized()

    segs = 6
    rings = []
    for t_s, r_mul in ((0.0, 1.0), (0.74, 0.82)):
        ring = []
        p = base + d * (length * t_s)
        for k in range(segs):
            ang = 2.0 * math.pi * k / segs + seed * 0.13
            w = 1.0 + 0.12 * math.sin(ang * 2.0 + seed)
            ring.append(bm.verts.new(p + (u * math.cos(ang) + v * math.sin(ang)) * (radius * r_mul * w)))
        rings.append(ring)

    kit.bridge_rings(bm, rings)
    kit.cap_ring(bm, rings[0], flip=True)
    apex = bm.verts.new(base + d * length)
    top = rings[-1]
    for k in range(segs):
        bm.faces.new([top[k], top[(k + 1) % segs], apex])


def add_angular_stone_block(bm, center, size_xyz, rot_euler, rock_seed=0, chip=0.14):
    """Thêm khối đá góc cạnh gãy bậc (không dùng khối cầu tròn)."""
    mat_rot = Euler(rot_euler, "XYZ").to_matrix().to_4x4()
    bm_tmp = bmesh.new()
    bmesh.ops.create_cube(bm_tmp, size=1.0)
    bmesh.ops.subdivide_edges(bm_tmp, edges=bm_tmp.edges[:], cuts=2, use_grid_fill=True)
    for vert in bm_tmp.verts:
        p = vert.co.copy()
        bevel_x = 0.88 if abs(p.y) > 0.42 and abs(p.z) > 0.42 else 1.0
        n = ridged(p * 2.1 + Vector((rock_seed * 0.7, rock_seed * 1.3, rock_seed * 0.5))) - 0.45
        p.x = p.x * size_xyz[0] * bevel_x * (1.0 + chip * n)
        p.y = p.y * size_xyz[1] * (1.0 + chip * n)
        p.z = p.z * size_xyz[2] * (1.0 + chip * n)
        vert.co = Vector(center) + (mat_rot @ p)
    tmp_m = bpy.data.meshes.new("_tmp_stone")
    bm_tmp.to_mesh(tmp_m)
    bm_tmp.free()
    bm.from_mesh(tmp_m)
    bpy.data.meshes.remove(tmp_m)


def add_rock_cluster(bm, center, scale_vec, seed=0, count=3):
    """Tạo cụm tảng đá đa diện tự nhiên."""
    cx, cy, cz = center
    for ri in range(count):
        ang = ri * 2.2 + seed * 0.7
        dist = 0.0 if ri == 0 else scale_vec[0] * 0.52
        ox = cx + math.cos(ang) * dist
        oy = cy + math.sin(ang) * dist
        oz = cz - (0.0 if ri == 0 else scale_vec[2] * 0.16)
        s_mul = 1.0 if ri == 0 else (0.62 - 0.08 * (ri % 2))
        bm_r = bmesh.new()
        bmesh.ops.create_icosphere(bm_r, subdivisions=1, radius=1.0)
        mat_r = Euler((0.08 * ri, -0.07 * ri, ang), "XYZ").to_matrix().to_4x4()
        for v in bm_r.verts:
            p = v.co.copy()
            rn = ridged(p * 1.8 + Vector((seed * 1.1 + ri, ri * 2.1, seed * 0.7)))
            p.x *= scale_vec[0] * s_mul * (0.74 + 0.40 * rn)
            p.y *= scale_vec[1] * s_mul * (0.74 + 0.40 * rn)
            p.z *= scale_vec[2] * s_mul * (0.76 + 0.44 * rn)
            v.co = Vector((ox, oy, oz)) + (mat_r @ p)
        tmp_m = bpy.data.meshes.new("_tmp_rock")
        bm_r.to_mesh(tmp_m)
        bm_r.free()
        bm.from_mesh(tmp_m)
        bpy.data.meshes.remove(tmp_m)


def add_curved_bone(bm, pts, radii, segs=8, wobble_amp=0.08):
    """Thêm xương cong (xương sườn lớn, cành xương ổ kền kền)."""
    rgs = parallel_tube_rings(
        bm,
        [Vector(p) for p in pts],
        radii,
        segs,
        wobble=lambda p, a, i: wobble_amp * math.sin(a * 2.0 + i * 0.4),
    )
    kit.bridge_rings(bm, rgs)
    kit.cap_ring(bm, rgs[0], flip=True)
    kit.cap_ring(bm, rgs[-1])


def build_watertight_shell(
    name_prefix,
    stone_mat,
    floor_fn,
    wall_disp_fn,
    paint_fn,
    room_radius=ROOM_RADIUS,
    wall_top_z=25.5,
    dome_peak_z=34.5,
    door_y=DOOR_Y,
    door_width=DOOR_WIDTH,
    door_height=DOOR_HEIGHT,
    vestibule_depth=VESTIBULE_DEPTH,
    col_tris=6500,
):
    """Dựng vỏ phòng kín 360° liền mạch: sàn bán kính >= 32m, vách không khe hở, trần kín không lỗ, một lối vào Nam."""
    bm_shell = bmesh.new()

    door_half_w = door_width * 0.5
    door_half_ang = math.asin(door_half_w / room_radius)
    south_ang = -math.pi * 0.5
    n_door_cols = 4
    n_wall_cols = 92

    ring_angles = []
    for i in range(n_door_cols + 1):
        t = i / n_door_cols
        ang = south_ang - door_half_ang + t * (2.0 * door_half_ang)
        ring_angles.append((ang, True, i))
    span = 2.0 * math.pi - 2.0 * door_half_ang
    for i in range(1, n_wall_cols):
        t = i / n_wall_cols
        ang = south_ang + door_half_ang + t * span
        ring_angles.append((ang, False, -1))
    n_cols = len(ring_angles)

    def _perimeter_xy(ang, is_door, door_idx):
        if is_door:
            t = door_idx / n_door_cols
            return -door_half_w + t * door_width, door_y
        warp = 1.05 * math.sin(ang * 5.0 + 0.4) + 0.75 * math.cos(ang * 9.0 - 1.1)
        d_s = abs((ang - south_ang + math.pi) % (2.0 * math.pi) - math.pi)
        edge_blend = smoothstep(door_half_ang, door_half_ang + 0.28, d_s)
        r = room_radius + max(0.0, warp) * edge_blend
        return math.cos(ang) * r, math.sin(ang) * r

    # 1. Sàn hình đĩa (20 vòng đồng tâm)
    n_floor_rings = 20
    floor_rings = []
    center_v = bm_shell.verts.new(Vector((0.0, 0.0, floor_fn(0.0, 0.0))))
    for ri in range(1, n_floor_rings + 1):
        frac = ri / n_floor_rings
        ring = []
        for ang, is_door, didx in ring_angles:
            px, py = _perimeter_xy(ang, is_door, didx)
            x = px * frac
            y = py * frac
            z = 0.0 if (ri == n_floor_rings and is_door) else floor_fn(x, y)
            ring.append(bm_shell.verts.new(Vector((x, y, z))))
        floor_rings.append(ring)

    first_r = floor_rings[0]
    for k in range(n_cols):
        bm_shell.faces.new([center_v, first_r[k], first_r[(k + 1) % n_cols]])
    for ri in range(len(floor_rings) - 1):
        a, b = floor_rings[ri], floor_rings[ri + 1]
        for k in range(n_cols):
            bm_shell.faces.new([a[k], b[k], b[(k + 1) % n_cols], a[(k + 1) % n_cols]])

    # 2. Vách đá & Vòm trần kín (tỉ lệ theo wall_top_z & dome_peak_z)
    dome_span = dome_peak_z - wall_top_z
    wall_levels = [
        (1.000, 0.0),
        (1.005, 1.8),
        (1.010, 3.6),
        (1.015, 5.4),
        (1.020, door_height),
        (1.025, door_height + (wall_top_z - door_height) * 0.20),
        (1.020, door_height + (wall_top_z - door_height) * 0.42),
        (1.010, door_height + (wall_top_z - door_height) * 0.64),
        (0.990, door_height + (wall_top_z - door_height) * 0.84),
        (0.955, wall_top_z),
        (0.870, wall_top_z + dome_span * 0.30),
        (0.750, wall_top_z + dome_span * 0.56),
        (0.610, wall_top_z + dome_span * 0.76),
        (0.450, wall_top_z + dome_span * 0.89),
        (0.280, wall_top_z + dome_span * 0.96),
        (0.130, dome_peak_z - 0.05),
    ]

    wall_rings = [floor_rings[-1]]
    for li in range(1, len(wall_levels)):
        r_scale, z_base = wall_levels[li]
        ring = []
        for ang, is_door, didx in ring_angles:
            px, py = _perimeter_xy(ang, is_door, didx)
            if li <= 4 and is_door:
                x, y = px, door_y
                if li == 4:
                    t_arc = (didx / n_door_cols) * 2.0 - 1.0
                    z = door_height - 0.55 * (t_arc * t_arc)
                else:
                    z = z_base
            else:
                d_s = abs((ang - south_ang + math.pi) % (2.0 * math.pi) - math.pi)
                door_mask = smoothstep(door_half_ang, door_half_ang + 0.34, d_s) if li <= 6 else 1.0
                r_disp, z_disp = wall_disp_fn(px, py, z_base, r_scale, li)
                r_disp *= door_mask
                if is_door and li > 4:
                    blend_up = smoothstep(4.0, 7.0, float(li))
                    x_circ = math.cos(ang) * room_radius
                    y_circ = math.sin(ang) * room_radius
                    bx = px * (1.0 - blend_up) + x_circ * blend_up
                    by = py * (1.0 - blend_up) + y_circ * blend_up
                else:
                    bx, by = px, py
                len_b = max(1e-4, math.hypot(bx, by))
                rad_cur = len_b * r_scale + r_disp
                x = (bx / len_b) * rad_cur
                y = (by / len_b) * rad_cur
                z = z_base + z_disp
            ring.append(bm_shell.verts.new(Vector((x, y, z))))
        wall_rings.append(ring)

    for li in range(len(wall_rings) - 1):
        a, b = wall_rings[li], wall_rings[li + 1]
        for k in range(n_cols):
            kn = (k + 1) % n_cols
            if (li < 4) and (k < n_door_cols):
                continue
            bm_shell.faces.new([a[k], a[kn], b[kn], b[k]])

    peak_v = bm_shell.verts.new(Vector((0.0, 0.0, dome_peak_z)))
    top_r = wall_rings[-1]
    for k in range(n_cols):
        bm_shell.faces.new([top_r[k], top_r[(k + 1) % n_cols], peak_v])

    # 3. Sảnh cửa Nam kín 100%
    opening_loop = []
    for i in range(n_door_cols + 1):
        opening_loop.append(wall_rings[0][i])
    for li in range(1, 5):
        opening_loop.append(wall_rings[li][n_door_cols])
    for i in range(n_door_cols - 1, -1, -1):
        opening_loop.append(wall_rings[4][i])
    for li in range(3, 0, -1):
        opening_loop.append(wall_rings[li][0])

    vest_steps = [0.0, -2.2, -4.6, -vestibule_depth]
    vest_rings = [opening_loop]
    for si in range(1, len(vest_steps)):
        dy = vest_steps[si]
        widen = 1.0 + 0.08 * (si / (len(vest_steps) - 1))
        ring = []
        for ov in opening_loop:
            nx = ov.co.x * widen
            ny = door_y + dy
            nz = ov.co.z
            if nz > 0.2:
                nz += 0.12 * noise.noise(Vector((nx * 0.5, ny * 0.4, nz * 0.3)))
            ring.append(bm_shell.verts.new(Vector((nx, ny, nz))))
        vest_rings.append(ring)

    for si in range(len(vest_rings) - 1):
        a, b = vest_rings[si], vest_rings[si + 1]
        m = len(a)
        for k in range(m):
            kn = (k + 1) % m
            bm_shell.faces.new([a[k], b[k], b[kn], a[kn]])

    back_ring = vest_rings[-1]
    back_center = bm_shell.verts.new(Vector((0.0, door_y - vestibule_depth, door_height * 0.45)))
    m_back = len(back_ring)
    for k in range(m_back):
        bm_shell.faces.new([back_ring[k], back_center, back_ring[(k + 1) % m_back]])

    bmesh.ops.remove_doubles(bm_shell, verts=bm_shell.verts[:], dist=0.01)

    shell_obj = kit.finish_object(bm_shell, f"{name_prefix}_Shell", stone_mat, recalc_normals=False)
    mesh = shell_obj.data
    bm_fix = bmesh.new()
    bm_fix.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm_fix, faces=bm_fix.faces[:])
    floor_faces = [f for f in bm_fix.faces if f.calc_center_median().z < 0.35 and f.calc_center_median().length < 20.0]
    if floor_faces and sum(f.normal.z for f in floor_faces) < 0.0:
        bmesh.ops.reverse_faces(bm_fix, faces=bm_fix.faces[:])
    bm_fix.to_mesh(mesh)
    bm_fix.free()
    for p in mesh.polygons:
        p.use_smooth = False

    kit.paint(shell_obj, paint_fn)
    col_obj = kit.make_collider(shell_obj, col_tris)
    col_obj.name = f"{name_prefix}_Shell_COL"
    return shell_obj, col_obj


def build_steel_door(name="BossRoom_SteelDoor"):
    """Cánh cửa thép gỉ sét dùng chung cho cả 3 phòng trùm: tâm (0,0,0), 5.4x7.2x0.7 m + bản lề to + 2 dây xích."""
    bm = bmesh.new()
    steel_mat = make_steel_material(f"{name}_Mat")

    # Tấm thép lõi
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Diagonal((5.2, 0.44, 7.1, 1.0)))
    frames = [
        ((0.0, 0.0, 3.35), (5.4, 0.72, 0.50)),
        ((0.0, 0.0, -3.35), (5.4, 0.72, 0.50)),
        ((0.0, 0.0, 0.0), (5.3, 0.64, 0.42)),
        ((-2.45, 0.0, 0.0), (0.50, 0.72, 7.20)),
        ((2.45, 0.0, 0.0), (0.50, 0.72, 7.20)),
    ]
    for pos, sz in frames:
        bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(pos) @ Matrix.Diagonal((sz[0], sz[1], sz[2], 1.0)))

    # Thanh giằng chữ X
    for z_mid in (-1.65, 1.65):
        for sign in (-1.0, 1.0):
            m = (
                Matrix.Translation((0.0, 0.0, z_mid))
                @ Euler((0.0, sign * math.radians(32.0), 0.0), "XYZ").to_matrix().to_4x4()
                @ Matrix.Diagonal((5.3, 0.58, 0.28, 1.0))
            )
            bmesh.ops.create_cube(bm, size=1.0, matrix=m)

    # Bản lề & khớp thép lớn hai bên mép cửa (chi tiết theo lore: bản lề to)
    for sx in (-2.35, 2.35):
        for hz in (-2.2, 0.0, 2.2):
            m = Matrix.Translation((sx, 0.0, hz)) @ Matrix.Diagonal((0.46, 0.86, 0.65, 1.0))
            bmesh.ops.create_cube(bm, size=1.0, matrix=m)

    # Đinh tán nổi
    for y_side in (-0.36, 0.36):
        for ix in (-2.45, -1.2, 0.0, 1.2, 2.45):
            for iz in (-3.35, 0.0, 3.35):
                m = Matrix.Translation((ix, y_side, iz)) @ Matrix.Diagonal((0.14, 0.12, 0.14, 1.0))
                bmesh.ops.create_icosphere(bm, subdivisions=1, radius=0.5, matrix=m)

    # Hai dây xích sắt nặng kéo lên khe trần
    for cx in (-1.55, 1.55):
        for li in range(19):
            lz = 3.75 + li * 0.42
            rot_z = math.pi * 0.5 if (li % 2 == 1) else 0.0
            for dx, dz, sx, sz in (
                (-0.14, 0.0, 0.09, 0.52),
                (0.14, 0.0, 0.09, 0.52),
                (0.0, -0.21, 0.36, 0.09),
                (0.0, 0.21, 0.36, 0.09),
            ):
                m = (
                    Matrix.Translation((cx, 0.0, lz))
                    @ Euler((0.0, 0.0, rot_z), "XYZ").to_matrix().to_4x4()
                    @ Matrix.Translation((dx, 0.0, dz))
                    @ Matrix.Diagonal((sx, 0.11, sz, 1.0))
                )
                bmesh.ops.create_cube(bm, size=1.0, matrix=m)

    door_obj = kit.finish_object(bm, name, steel_mat)
    for p in door_obj.data.polygons:
        p.use_smooth = False

    steel_dark = (0.04, 0.04, 0.045)
    steel_mid = (0.09, 0.095, 0.105)
    rust_dark = (0.14, 0.045, 0.016)
    rust_orange = (0.26, 0.08, 0.025)

    def _paint_door(pos, _n):
        rn = ridged(Vector((pos.x * 0.85, pos.y * 1.2, pos.z * 0.75)))
        rust_t = clamp01(rn * 0.8 + (1.0 - clamp01((pos.z + 3.6) / 4.5)) * 0.45)
        if abs(pos.y) < 0.26 and abs(pos.x) < 2.20:
            return mix(mix(steel_dark, steel_mid, 0.45), rust_dark, rust_t * 0.75)
        return mix(steel_mid, rust_orange, rust_t * 0.65)

    kit.paint(door_obj, _paint_door)
    return door_obj


def setup_cycles_scene(world_bg, world_strength, lights):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 36
    scene.cycles.max_bounces = 4
    scene.cycles.diffuse_bounces = 2
    if hasattr(scene.cycles, "use_denoising"):
        scene.cycles.use_denoising = True
    scene.render.resolution_x, scene.render.resolution_y = 960, 640
    scene.render.image_settings.file_format = "PNG"

    world = bpy.data.worlds.new("BossRoomPreviewWorld")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (*world_bg, 1.0)
    world.node_tree.nodes["Background"].inputs[1].default_value = world_strength
    scene.world = world

    for o in list(scene.collection.objects):
        if o.type in {"CAMERA", "LIGHT"}:
            bpy.data.objects.remove(o, do_unlink=True)
        o.hide_render = o.name.endswith("_COL")

    for lname, lpos, lcol, lenergy, lrad in lights:
        lobj = bpy.data.objects.new("_prev_" + lname, bpy.data.lights.new(lname, "POINT"))
        lobj.location = Vector(lpos)
        lobj.data.color = lcol
        lobj.data.energy = lenergy
        lobj.data.shadow_soft_size = lrad
        kit.link(lobj)

    cam = bpy.data.objects.new("_prev_cam", bpy.data.cameras.new("cam"))
    kit.link(cam)
    scene.camera = cam
    return cam, scene


def render_view(cam, scene, cpos, target, lens, filepath):
    cam.location = Vector(cpos)
    cam.data.lens = lens
    cam.rotation_euler = (Vector(target) - Vector(cpos)).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = filepath
    bpy.ops.render.render(write_still=True)
