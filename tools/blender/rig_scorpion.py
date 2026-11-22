"""Rig the Meshy scorpion boss (Bọ Cạp Thạch Anh) and bake every clip into one FBX.

    python -I tools/blender/rig_scorpion.py [in.fbx] [out.fbx] [--previews dir]

Defaults: reads and rewrites Assets/_Project/Models/Wildlife/Meshy/Creature_BoCapThachAnh_Meshy.fbx in place (an old armature in the file is dropped
first, so it can be run again). The mesh must be the Meshy one: faces -Y in Blender, feet on Z=0, 1.9 m wide, tail arching over the back.

Bones (all measured from the mesh, see GEOMETRY): Root (the body, every pose is added to it), Sting1..Sting5 + StingTip (the crystal stinger, StingTip is where the
glow gathers), Pede{L,R}{1..4}_A/_B (eight legs, front to back, upper and lower), Claw{L,R}_A/_B/_Finger (shoulder, forearm, the moving pincer).
Every clip is computed frame by frame (no sparse keys): the legs are solved with a small numeric IK to hit absolute foot targets, so a foot that is planted stays
planted while the body crouches, rears or lunges. Weights: the nearest three bones, falling off with distance (a bone's radius counts as inside it).

Clips (30 fps): Idle, Walk_F/B/L/R (the four directions, loops, each 20 frames), Hit, Death, Flick (the tail throws a crystal), Exposed and Stuck (loops: lying open, claws jammed in a wall), and for each move three clips named after the move's Anim:
Tell_<Anim> (windup), Strike_<Anim> (cast) and Recover_<Anim> (after-cast), Anim in Sting, TailSlam, Volley, Burrow, Pinch, Burst.
"""
import math
import os
import sys

import bpy  # must come before mathutils
import mathutils
import numpy as np

V = mathutils.Vector
M = mathutils.Matrix
FPS = 30
DEFAULT_FBX = "Assets/_Project/Models/Wildlife/Meshy/Creature_BoCapThachAnh_Meshy.fbx"

# ---------------------------------------------------------------------------------------------------------------------------------------------------------
# GEOMETRY: measured on the Meshy mesh (world axes, Z up, the model faces -Y). Feet are where the leg tips touch the ground; the tail is the arc seen from the side.
# ---------------------------------------------------------------------------------------------------------------------------------------------------------
FOOT_Z = 0.07
LEGS = [  # index (front to back), foot (|x|, y), knee height, hip (|x|, z). The knee is where the leg tube bends from going out to going down (measured on the mesh: z about 0.58);
    # an earlier guess of 0.9 put the thigh bones through the spikes on the back, which then walked with the legs
    (1, 0.85, 0.12, 0.60, 0.26, 0.55),
    (2, 0.92, 0.37, 0.60, 0.30, 0.55),
    (3, 0.78, 0.67, 0.56, 0.30, 0.50),
    (4, 0.58, 0.93, 0.52, 0.26, 0.48),
]
REACH = {"Pede": 0.17, "Sting": 0.32, "Claw": 0.55}   # a bone moves only the vertices this close to it (metres): the spikes on the back belong to the body (Root) alone
TAIL = [(0.0, 0.62, 0.50), (0.0, 0.88, 0.66), (0.0, 0.84, 0.92), (0.0, 0.66, 1.16), (0.0, 0.44, 1.26), (0.0, 0.26, 1.14), (0.0, 0.10, 0.98)]   # base .. StingTip end
CLAW = dict(shoulder=(0.26, -0.38, 0.55), elbow=(0.50, -0.58, 0.60), wrist=(0.46, -0.82, 0.45), tip=(0.24, -0.98, 0.33))
ROOT = ((0.0, 0.55, 0.52), (0.0, -0.30, 0.52))

STRIDE = 0.42      # metres a foot travels while planted in one step; the clip's base speed is STRIDE / (half the cycle time)
CYCLE = 20         # frames of a walk loop
CLIP_SPEED = STRIDE / (0.5 * CYCLE / FPS)   # = 1.26 m/s: ScorpionRules.ClipSpeed in C# must say the same


def bone_specs():
    """name -> (head, tail, parent, radius, side-axis for roll): the whole skeleton."""
    b = {}
    b["Root"] = (V(ROOT[0]), V(ROOT[1]), None, 0.5, V((0, 0, -1)))
    for i in range(6):
        h, t = V(TAIL[i]), V(TAIL[i + 1])
        name = "StingTip" if i == 5 else "Sting%d" % (i + 1)
        b[name] = (h, t, "Root" if i == 0 else ("Sting%d" % i), 0.07, V((1, 0, 0)).cross(t - h))
    for side, sx in (("L", 1), ("R", -1)):
        for n, fx, fy, kz, hx, hz in LEGS:
            hip, foot = V((sx * hx, fy, hz)), V((sx * fx, fy, FOOT_Z))
            knee = V((sx * fx * 0.9, fy, kz))
            tang = (knee - hip).cross(V((0, 0, 1)))
            b["Pede%s%d_A" % (side, n)] = (hip, knee, "Root", 0.05, V((0, 0, 1)) if tang.length < 1e-4 else tang.cross(knee - hip))
            b["Pede%s%d_B" % (side, n)] = (knee, foot, "Pede%s%d_A" % (side, n), 0.04, tang.cross(foot - knee))
        c = {k: V((sx * v[0], v[1], v[2])) for k, v in CLAW.items()}
        b["Claw%s_A" % side] = (c["shoulder"], c["elbow"], "Root", 0.09, V((0, 0, 1)))
        b["Claw%s_B" % side] = (c["elbow"], c["wrist"], "Claw%s_A" % side, 0.10, V((0, 0, 1)))
        b["Claw%s_Finger" % side] = (c["wrist"], c["tip"], "Claw%s_B" % side, 0.10, V((0, 0, 1)))
    return b


# ---------------------------------------------------------------------------------------------------------------------------------------------------------
# forward kinematics (Blender's rule: pose = parent_pose @ inv(parent_rest) @ rest @ basis), so the clips can be solved without touching the depsgraph
# ---------------------------------------------------------------------------------------------------------------------------------------------------------
class Skeleton:
    def __init__(self, arm):
        self.arm = arm
        self.rest = {b.name: b.matrix_local.copy() for b in arm.data.bones}
        self.parent = {b.name: (b.parent.name if b.parent else None) for b in arm.data.bones}
        self.length = {b.name: b.length for b in arm.data.bones}
        self.order = [b.name for b in arm.data.bones]   # parents come first

    def fk(self, basis):
        """basis: name -> 4x4 matrix (missing = identity). Returns name -> pose matrix in armature space."""
        pose = {}
        for n in self.order:
            p = self.parent[n]
            local = self.rest[n] if p is None else self.rest[p].inverted() @ self.rest[n]
            base = pose[p] @ local if p else local
            pose[n] = base @ basis.get(n, M.Identity(4))
        return pose

    def tail_pos(self, pose, n):
        return pose[n] @ V((0, self.length[n], 0))

    def basis_rot(self, n, euler_arm):
        """A rotation given in armature axes (degrees XYZ, applied in the parent's rest-posed frame) as the bone's basis matrix."""
        r = M.Identity(3)
        for axis, ang in zip("XYZ", euler_arm):
            r = M.Rotation(math.radians(ang), 3, axis) @ r
        full = self.rest[n].to_3x3()
        return (full.inverted() @ r @ full).to_4x4()


def foot_target_ik(sk, basis_root, leg, target, theta0):
    """Solve the three angles of a leg (hip lift, hip yaw, knee; radians) so that the foot (tail of _B) is at `target` (armature space)."""
    a, bn = leg + "_A", leg + "_B"

    def foot(th):   # hip: lift about armature Y (in the leg's plane) and yaw about armature Z (fore-aft swing); knee: a hinge about Y
        d = np.degrees(th)
        bas = {"Root": basis_root,
               a: sk.basis_rot(a, (0, d[0], d[1])),
               bn: sk.basis_rot(bn, (0, d[2], 0))}
        pose = sk.fk(bas)
        return np.array(pose[bn] @ V((0, sk.length[bn], 0))), bas
    th = np.array(theta0, dtype=float)
    for _ in range(8):
        p, _b = foot(th)
        e = np.array(target) - p
        if np.linalg.norm(e) < 1e-3:
            break
        J = np.zeros((3, 3))
        for k in range(3):
            d = th.copy(); d[k] += 0.02
            J[:, k] = (foot(d)[0] - p) / 0.02
        th = th + np.clip(J.T @ np.linalg.solve(J @ J.T + 0.002 * np.eye(3), e), -0.5, 0.5)
        th = np.clip(th, -1.3, 1.3)
    p, bas = foot(th)
    return th, bas, float(np.linalg.norm(np.array(target) - p))


# ---------------------------------------------------------------------------------------------------------------------------------------------------------
# poses: nested dicts of numbers, blended by key frames. body d*(m), pitch (+ = nose up), roll (+ = right side down), yaw; tail.wind (+ = tail pulled back and up,
# - = lashed forward and down) and tail.sway; claw L/R: lift (+ up), spread (+ out), open (+ finger opens); feet: leg -> (dx, dy, dz) offsets from the planted spot.
# ---------------------------------------------------------------------------------------------------------------------------------------------------------
REST = dict(plant=1.0, body=dict(dx=0, dy=0, dz=0, pitch=0, roll=0, yaw=0), tail=dict(wind=0, sway=0, curl=0),
            cl=dict(lift=0, spread=0, open=0), cr=dict(lift=0, spread=0, open=0), feet={})


def lerp_pose(a, b, t):
    out = {}
    for k in set(a) | set(b):
        va, vb = a.get(k), b.get(k)
        if isinstance(va, dict) or isinstance(vb, dict):
            out[k] = lerp_pose(va or {}, vb or {}, t)
        elif isinstance(va, tuple) or isinstance(vb, tuple):
            va = va or (0, 0, 0); vb = vb or (0, 0, 0)
            out[k] = tuple(x + (y - x) * t for x, y in zip(va, vb))
        else:
            va = 0.0 if va is None else va; vb = 0.0 if vb is None else vb
            out[k] = va + (vb - va) * t
    return out


def ease(t):
    return t * t * (3 - 2 * t)


def sample_keys(keys, frame):
    """keys: [(frame, pose-delta)] sorted; blends deltas with smoothstep, holds the first and last."""
    if frame <= keys[0][0]:
        return keys[0][1]
    for (f0, p0), (f1, p1) in zip(keys, keys[1:]):
        if frame <= f1:
            return lerp_pose(p0, p1, ease((frame - f0) / max(1e-6, f1 - f0)))
    return keys[-1][1]


def P(body=None, tail=None, cl=None, cr=None, claws=None, feet=None, plant=None):
    """A pose delta; claws sets both claws at once; plant 1 = the feet stay where they are while the body moves (default), 0 = they travel with the body (burrowing, dying)."""
    d = dict(body=body or {}, tail=tail or {}, cl=claws or cl or {}, cr=claws or cr or {}, feet=feet or {})
    if plant is not None:
        d["plant"] = plant
    return d


class Rig:
    def __init__(self, sk):
        self.sk = sk
        self.theta = {}
        self.signs = self._probe_signs()
        self.rest_feet = {}
        pose0 = sk.fk({})
        for side in "LR":
            for n, *_ in LEGS:
                leg = "Pede%s%d" % (side, n)
                self.rest_feet[leg] = np.array(pose0[leg + "_B"] @ V((0, sk.length[leg + "_B"], 0)))
        self.max_err = 0.0

    def _probe_signs(self):
        """Which way is positive for pitch, tail wind, claw lift/spread/open: measured on the skeleton, so no axis is assumed."""
        sk = self.sk
        s = {}
        pose0 = sk.fk({})
        tip0 = np.array(pose0["StingTip"] @ V((0, sk.length["StingTip"], 0)))
        for sg in (1, -1):   # a tail rotating about armature X: wind>0 must carry the tip backwards (+Y)
            bas = {"Sting1": sk.basis_rot("Sting1", (20 * sg, 0, 0))}
            tip = np.array(sk.fk(bas)["StingTip"] @ V((0, sk.length["StingTip"], 0)))
            if tip[1] > tip0[1] + 0.02:
                s["tail"] = sg
        for sg in (1, -1):   # body pitch: nose up (front claw tip rises)
            bas = {"Root": sk.basis_rot("Root", (10 * sg, 0, 0))}
            p = sk.fk(bas)["ClawL_Finger"] @ V((0, sk.length["ClawL_Finger"], 0))
            p0 = pose0["ClawL_Finger"] @ V((0, sk.length["ClawL_Finger"], 0))
            if p[2] > p0[2] + 0.02:
                s["pitch"] = sg
        for sg in (1, -1):   # body roll: right side (x<0) goes down for + roll
            bas = {"Root": sk.basis_rot("Root", (0, 10 * sg, 0))}
            p = sk.fk(bas)["PedeR2_B"] @ V((0, sk.length["PedeR2_B"], 0))
            p0 = pose0["PedeR2_B"] @ V((0, sk.length["PedeR2_B"], 0))
            if p[2] < p0[2] - 0.02:
                s["roll"] = sg
        for side, out in (("L", 1), ("R", -1)):
            a = "Claw%s_A" % side
            p0 = pose0["Claw%s_B" % side] @ V((0, sk.length["Claw%s_B" % side], 0))
            for sg in (1, -1):   # lift: wrist rises; spread: wrist moves outward; open: finger tip moves outward
                z = sk.fk({a: sk.basis_rot(a, (20 * sg, 0, 0))})["Claw%s_B" % side] @ V((0, sk.length["Claw%s_B" % side], 0))
                if z[2] > p0[2] + 0.02:
                    s["lift" + side] = sg
                z = sk.fk({a: sk.basis_rot(a, (0, 0, 20 * sg))})["Claw%s_B" % side] @ V((0, sk.length["Claw%s_B" % side], 0))
                if (z[0] - p0[0]) * out > 0.02:
                    s["spread" + side] = sg
            f = "Claw%s_Finger" % side
            q0 = pose0[f] @ V((0, sk.length[f], 0))
            for sg in (1, -1):
                z = sk.fk({f: sk.basis_rot(f, (0, 0, 20 * sg))})[f] @ V((0, sk.length[f], 0))
                if (z[0] - q0[0]) * out > 0.02:
                    s["open" + side] = sg
        need = {"tail", "pitch", "roll", "liftL", "liftR", "spreadL", "spreadR", "openL", "openR"}
        if need - set(s):
            raise SystemExit("could not probe the signs of: %s" % sorted(need - set(s)))
        return s

    def solve(self, pose, prev_theta=None):
        """pose (full, REST-merged) -> basis dict for every bone. Also tracks the worst foot error."""
        sk, sg = self.sk, self.signs
        b = pose["body"]
        bas = {}
        rot = sk.basis_rot("Root", (sg["pitch"] * b["pitch"], sg["roll"] * b["roll"], b["yaw"]))
        rest3 = sk.rest["Root"].to_3x3()
        # the root's basis translation is in its own rest frame: bring the armature-space offset into it
        off = rest3.inverted() @ V((b["dx"], b["dy"], b["dz"]))
        root_basis = M.Translation(off) @ rot
        bas["Root"] = root_basis
        tl = pose["tail"]
        names = ["Sting1", "Sting2", "Sting3", "Sting4", "Sting5", "StingTip"]
        w = np.array([0.34, 0.26, 0.22, 0.18, 0.12, 0.06])   # how the wind is shared along the tail (the base moves most)
        for i, n in enumerate(names):
            bas[n] = sk.basis_rot(n, (sg["tail"] * (tl["wind"] * w[i] + tl["curl"] * (1 if i else 0)), 0, tl["sway"] * w[i]))
        for side, key in (("L", "cl"), ("R", "cr")):
            c = pose[key]
            a, bb, f = "Claw%s_A" % side, "Claw%s_B" % side, "Claw%s_Finger" % side
            bas[a] = sk.basis_rot(a, (sg["lift" + side] * c["lift"] * 0.6, 0, sg["spread" + side] * c["spread"] * 0.6))
            bas[bb] = sk.basis_rot(bb, (sg["lift" + side] * c["lift"] * 0.4, 0, sg["spread" + side] * c["spread"] * 0.4))
            bas[f] = sk.basis_rot(f, (0, 0, sg["open" + side] * c["open"]))
        theta = {}
        carried = {}   # where each rest foot would be if it were glued to the body (for burrowing and dying, when the feet are not planted)
        root_move = sk.fk({"Root": root_basis})["Root"] @ sk.rest["Root"].inverted()
        for leg, f in self.rest_feet.items():
            carried[leg] = np.array(root_move @ V(f))
        for side in "LR":
            for n, *_ in LEGS:
                leg = "Pede%s%d" % (side, n)
                off = pose["feet"].get(leg, (0, 0, 0))
                target = self.rest_feet[leg] + np.array(off) + (1.0 - pose["plant"]) * (carried[leg] - self.rest_feet[leg])
                th, lb, err = foot_target_ik(sk, root_basis, leg, target, (prev_theta or self.theta).get(leg, (0, 0, 0)))
                self.max_err = max(self.max_err, err)
                bas[leg + "_A"], bas[leg + "_B"] = lb[leg + "_A"], lb[leg + "_B"]
                theta[leg] = tuple(th)
        self.theta = theta
        return bas


# ---------------------------------------------------------------------------------------------------------------------------------------------------------
# the clips
# ---------------------------------------------------------------------------------------------------------------------------------------------------------
GROUP = {"L1": 0, "R1": 1, "L2": 1, "R2": 0, "L3": 0, "R3": 1, "L4": 1, "R4": 0}   # two tetrapods that step in turn


def walk_pose(frame, d, rate=1.0):
    """One frame of a gait going along unit vector d (armature x right, y back; forward is (0, -1)): the planted feet slide back at body speed."""
    p = (frame / CYCLE) % 1.0
    feet = {}
    for side in "LR":
        for n, *_ in LEGS:
            q = (p + 0.5 * GROUP[side + str(n)]) % 1.0
            if q < 0.5:
                f, lift = 0.5 - 2 * q, 0.0
            else:
                u = (q - 0.5) * 2
                f, lift = -0.5 + ease(u), math.sin(math.pi * u) * 0.14
            feet["Pede%s%d" % (side, n)] = (d[0] * STRIDE * f, d[1] * STRIDE * f, lift)
    bob = 0.012 * math.cos(4 * math.pi * p)
    sway = 2.5 * math.sin(2 * math.pi * p)
    fwd = -d[1]
    return P(body=dict(dz=bob - 0.02, roll=sway * (1 - abs(d[1])) + 0.8 * sway * 0.2, pitch=-2.0 * fwd * rate, dy=0, dx=0),
             tail=dict(wind=6 + 3 * math.sin(4 * math.pi * p), sway=7 * math.sin(2 * math.pi * p + 1.0)),
             claws=dict(lift=6 + 2 * math.sin(4 * math.pi * p), spread=4, open=3), feet=feet)


def idle_pose(frame, n=60):
    t = frame / n * 2 * math.pi
    return P(body=dict(dz=0.012 * math.sin(t), pitch=0.8 * math.sin(t), roll=0.8 * math.sin(t * 0.5)),
             tail=dict(wind=4 + 5 * math.sin(t * 0.5), sway=9 * math.sin(t * 0.5 + 0.7)),
             cl=dict(lift=5 + 3 * math.sin(t), spread=4, open=6 + 6 * max(0.0, math.sin(t * 2 + 1.5))),
             cr=dict(lift=5 + 3 * math.sin(t + 0.6), spread=4, open=6 + 6 * max(0.0, math.sin(t * 2 + 3.0))))


def shake(frame, amp, rate=1.0):
    return math.sin(frame * 1.9 * rate) * amp


def make_clips():
    """name -> (frames, loop, function frame -> pose delta). Phase clips: Tell 24, Strike 12, Recover 24 frames (the game scales the speed to the phase's seconds)."""
    c = {}
    c["Idle"] = (60, True, lambda f: idle_pose(f))
    c["Walk_F"] = (CYCLE, True, lambda f: walk_pose(f, (0, -1)))
    c["Walk_B"] = (CYCLE, True, lambda f: walk_pose(f, (0, 1)))
    c["Walk_L"] = (CYCLE, True, lambda f: walk_pose(f, (1, 0)))    # the scorpion faces -Y, so its left side is +X (the L bones are on +X): strafing left goes to +X
    c["Walk_R"] = (CYCLE, True, lambda f: walk_pose(f, (-1, 0)))
    c["Hit"] = (12, False, lambda f: P(body=dict(dy=0.14 * math.sin(math.pi * min(1, f / 11)), pitch=-6 * math.sin(math.pi * min(1, f / 11)), dz=-0.03 * math.sin(math.pi * min(1, f / 11))),
                                         tail=dict(wind=18 * math.sin(math.pi * min(1, f / 11))), claws=dict(lift=14 * math.sin(math.pi * min(1, f / 11)), spread=10, open=18 * math.sin(math.pi * min(1, f / 11)))))

    def death(f):
        t = ease(min(1, f / 30))
        feet = {}
        for side, sx in (("L", 1), ("R", -1)):
            for n, fx, fy, *_ in LEGS:
                feet["Pede%s%d" % (side, n)] = (-sx * 0.10 * t, 0.0, 0.0)
        return P(body=dict(dz=-0.13 * t, roll=14 * t, pitch=-6 * t), tail=dict(wind=-30 * t, sway=14 * t), claws=dict(lift=-18 * t, spread=14 * t, open=30 * t), feet=feet)
    c["Death"] = (40, False, death)

    def phases(anim, tell, strike, recover, tell_len=24, strike_len=12, recover_len=24):
        for key, frames, ln in (("Tell_", tell, tell_len), ("Strike_", strike, strike_len), ("Recover_", recover, recover_len)):
            c[key + anim] = (ln, False, (lambda ks: (lambda f: sample_keys(ks, f)))(frames))

    # Sting (Châm Choáng): crouch, tail cocked back with the pincers open; stab forward; the stinger stuck in the ground, a heave, back up
    phases("Sting",
           [(0, P()), (24, P(body=dict(dz=-0.10, dy=0.12, pitch=6), tail=dict(wind=42, sway=0, curl=-6), claws=dict(lift=26, spread=18, open=38)))],
           [(0, P(body=dict(dz=-0.10, dy=0.12, pitch=6), tail=dict(wind=42, curl=-6), claws=dict(lift=26, spread=18, open=38))),
            (5, P(body=dict(dz=-0.12, dy=-0.30, pitch=-10), tail=dict(wind=-52, curl=-4), claws=dict(lift=10, spread=8, open=-6))),
            (12, P(body=dict(dz=-0.12, dy=-0.30, pitch=-10), tail=dict(wind=-52, curl=-4), claws=dict(lift=10, spread=8, open=-6)))],
           [(0, P(body=dict(dz=-0.12, dy=-0.30, pitch=-10), tail=dict(wind=-52, curl=-4), claws=dict(lift=10, spread=8, open=-6))),
            (10, P(body=dict(dz=-0.12, dy=-0.30, pitch=-10, roll=2), tail=dict(wind=-52, curl=-4, sway=3), claws=dict(lift=10, spread=8, open=-6))),
            (24, P(body=dict(dz=0, dy=0, pitch=0), tail=dict(wind=6), claws=dict(lift=6, spread=4, open=3)))])

    # TailSlam (Giáng Đuôi): rear up, tail straight up and trembling; slam; the tail stuck in the ground, pulled out slowly, a heavy breath
    up = P(body=dict(dz=0.06, dy=0.10, pitch=14), tail=dict(wind=70, curl=-10), claws=dict(lift=34, spread=22, open=20))
    low = P(body=dict(dz=-0.10, dy=-0.18, pitch=-9), tail=dict(wind=-70, curl=6), claws=dict(lift=-4, spread=14, open=10))
    phases("TailSlam",
           [(0, P()), (16, up), (24, P(body=dict(dz=0.06, dy=0.10, pitch=14, roll=1.5), tail=dict(wind=76, curl=-10, sway=2), claws=dict(lift=34, spread=22, open=20)))],
           [(0, P(body=dict(dz=0.06, dy=0.10, pitch=14), tail=dict(wind=76, curl=-10), claws=dict(lift=34, spread=22, open=20))), (7, low), (12, low)],
           [(0, low), (14, P(body=dict(dz=-0.10, dy=-0.18, pitch=-9, roll=3), tail=dict(wind=-58, curl=6, sway=4), claws=dict(lift=-4, spread=14, open=10))),
            (24, P(body=dict(dz=0, dy=0, pitch=0), tail=dict(wind=6), claws=dict(lift=6, spread=4, open=3)))])

    # Volley (Phóng Gai): lean back, tail arched and glowing; three jerks that each throw a spike; the tail goes slack
    arch = P(body=dict(dz=-0.04, dy=0.10, pitch=10), tail=dict(wind=58, curl=-14, sway=0), claws=dict(lift=22, spread=26, open=16))
    kick = lambda k: P(body=dict(dz=-0.04, dy=0.0 + 0.05 * k, pitch=2 - 3 * k), tail=dict(wind=58 - 52 * (1 if k else 0), curl=-14, sway=0), claws=dict(lift=22, spread=26, open=16))
    phases("Volley",
           [(0, P()), (24, arch)],
           [(0, arch), (2, P(body=dict(dz=-0.04, dy=-0.06, pitch=-2), tail=dict(wind=8, curl=-14), claws=dict(lift=22, spread=26, open=16))), (4, arch),
            (6, P(body=dict(dz=-0.04, dy=-0.06, pitch=-2), tail=dict(wind=8, curl=-14), claws=dict(lift=22, spread=26, open=16))), (8, arch),
            (10, P(body=dict(dz=-0.04, dy=-0.08, pitch=-3), tail=dict(wind=4, curl=-14), claws=dict(lift=22, spread=26, open=16))), (12, P(body=dict(dz=-0.04, dy=-0.04, pitch=-1), tail=dict(wind=14, curl=-14), claws=dict(lift=22, spread=26, open=16)))],
           [(0, P(body=dict(dz=-0.04, dy=-0.04, pitch=-1), tail=dict(wind=14, curl=-14), claws=dict(lift=22, spread=26, open=16))),
            (8, P(body=dict(dz=-0.05, dy=0, pitch=0), tail=dict(wind=-10, curl=8, sway=0), claws=dict(lift=10, spread=10, open=8))),
            (24, P(body=dict(dz=0), tail=dict(wind=6), claws=dict(lift=6, spread=4, open=3)))])

    # Burrow (Đào Chui): scrape the ground with the front legs and sink; underground (the model goes below the floor); burst up with the claws wide; shake the dirt off
    def scrape(f):
        t = ease(min(1, f / 23))
        feet = {}
        for side in "LR":
            ph = math.sin(f * 0.9 + (0 if side == "L" else 2.2))
            feet["Pede%s1" % side] = (0, -0.10 + 0.14 * ph, 0.10 * max(0, ph))
            feet["Pede%s2" % side] = (0, 0.05 * ph, 0.04 * max(0, ph))
        return P(body=dict(dz=-0.80 * t * t, pitch=-22 * t, roll=shake(f, 3.0 * t)), tail=dict(wind=30 * t, sway=shake(f, 5, 0.7)), claws=dict(lift=-8 * t, spread=-14 * t, open=-10 * t), feet=feet, plant=1 - t)
    c["Tell_Burrow"] = (24, False, scrape)

    def under(f):   # 36 frames: down below the floor until frame 30 (5/6 of the clip = where the game's Emerge stage starts), then the burst
        u = max(0.0, (f - 30) / 6.0)
        t = ease(min(1, u))
        return P(body=dict(dz=-0.80 + 0.90 * t, pitch=-22 + 40 * t, roll=shake(f, 2.0 * (1 - t))), tail=dict(wind=30 + 40 * t, sway=0), claws=dict(lift=-8 + 48 * t, spread=-14 + 44 * t, open=-10 + 52 * t), plant=0.0 if t < 0.3 else 0.0 + (t - 0.3) / 0.7)
    c["Strike_Burrow"] = (36, False, under)
    c["Recover_Burrow"] = (24, False, lambda f: sample_keys([(0, P(body=dict(dz=0.10, pitch=18), tail=dict(wind=70), claws=dict(lift=40, spread=30, open=42))),
                                                            (8, P(body=dict(dz=-0.02, pitch=-2, roll=5), tail=dict(wind=20, sway=6), claws=dict(lift=18, spread=12, open=16))),
                                                            (14, P(body=dict(dz=0, pitch=0, roll=-5), tail=dict(wind=8, sway=-6), claws=dict(lift=8, spread=6, open=6))),
                                                            (24, P(body=dict(dz=0), tail=dict(wind=6), claws=dict(lift=6, spread=4, open=3)))], f))

    # Pinch (Lao Kẹp): claws wide, hunch low; run flat out with the claws open, snap them shut; slide to a stop
    wide = P(body=dict(dz=-0.12, dy=0.14, pitch=-4), tail=dict(wind=50, curl=-6), claws=dict(lift=14, spread=40, open=44))
    c["Tell_Pinch"] = (24, False, lambda f: sample_keys([(0, P()), (16, wide), (24, P(body=dict(dz=-0.12, dy=0.14, pitch=-4, roll=1.2), tail=dict(wind=50, curl=-6), claws=dict(lift=14, spread=40, open=44)))], f))

    def charge(f):
        base = walk_pose(f * 2, (0, -1), 1.0)
        snap = ease(max(0.0, (f - 9) / 3.0))
        base["body"].update(dz=-0.10, pitch=-8, dy=-0.04)
        base["tail"].update(wind=24, sway=3 * math.sin(f))
        base["cl"] = base["cr"] = dict(lift=10 - 6 * snap, spread=34 - 30 * snap, open=44 - 56 * snap)
        return base
    c["Strike_Pinch"] = (12, False, charge)

    def slide(f):
        t = ease(f / 24)
        feet = {}
        for side in "LR":
            for n, *_ in LEGS:
                feet["Pede%s%d" % (side, n)] = (0, -0.20 * (1 - t) * (1 if n <= 2 else -0.4), 0)
        return P(body=dict(dz=-0.10 * (1 - t), pitch=-8 * (1 - t) + 3 * math.sin(f * 0.5) * (1 - t), dy=-0.04 * (1 - t)), tail=dict(wind=24 * (1 - t) + 6 * t), claws=dict(lift=4 + 2 * t, spread=4, open=-12 * (1 - t) + 3 * t), feet=feet)
    c["Recover_Pinch"] = (24, False, slide)

    # Burst (Bùng Pha Lê, the ultimate): rise on stiff legs, claws up, tail straight up and trembling harder and harder; slam the tail down; the shell cracks, a long gasp
    def rise(f):
        t = ease(min(1, f / 40))
        sh = shake(f, 0.9 * t * t * 3, 1.3)
        return P(body=dict(dz=0.20 * t, pitch=10 * t, roll=sh * 1.5, yaw=sh * 0.8), tail=dict(wind=78 * t, curl=-12 * t, sway=sh * 3), claws=dict(lift=58 * t, spread=40 * t, open=40 * t))
    c["Tell_Burst"] = (54, False, rise)
    top = P(body=dict(dz=0.20, pitch=10), tail=dict(wind=78, curl=-12), claws=dict(lift=58, spread=40, open=40))
    slam = P(body=dict(dz=-0.16, pitch=-12, dy=-0.10), tail=dict(wind=-78, curl=8), claws=dict(lift=-10, spread=30, open=26))
    c["Strike_Burst"] = (12, False, lambda f: sample_keys([(0, top), (5, slam), (12, slam)], f))
    gasp = [(0, slam), (12, P(body=dict(dz=-0.22, pitch=-8, dy=-0.06), tail=dict(wind=-30, sway=5), claws=dict(lift=-14, spread=18, open=34))),
            (24, P(body=dict(dz=-0.12, pitch=-3), tail=dict(wind=-6, sway=-4), claws=dict(lift=2, spread=10, open=18)))]
    c["Recover_Burst"] = (36, False, lambda f: _with_breath(sample_keys(gasp, min(f, 24)), f))

    # Flick (Bùng Pha Lê, after the rings): the stinger swings forward and down to spear a crystal, a beat, then whips up and back and throws it (21 frames = 0.7 s)
    ready = P(body=dict(dz=0.05, pitch=6), tail=dict(wind=40, curl=-8), claws=dict(lift=30, spread=26, open=22))
    stab = P(body=dict(dz=-0.10, dy=-0.14, pitch=-8), tail=dict(wind=-60, curl=6), claws=dict(lift=14, spread=18, open=10))
    whip = P(body=dict(dz=0.04, dy=0.10, pitch=10), tail=dict(wind=62, curl=-12), claws=dict(lift=36, spread=30, open=26))
    c["Flick"] = (21, False, lambda f: sample_keys([(0, ready), (5, stab), (9, stab), (12, whip), (16, whip), (21, ready)], f))

    # Exposed (the lie-open after a big move): low, claws drooping and slack, the tail trailing; slow sway, no panting (48 frames, loops)
    def exposed(f):
        t = f / 48 * 2 * math.pi
        return P(body=dict(dz=-0.12, pitch=-4, roll=1.5 * math.sin(t), dy=-0.04), tail=dict(wind=-22 + 3 * math.sin(t), sway=8 * math.sin(t)),
                 claws=dict(lift=-18 + 2 * math.sin(t), spread=14, open=24 + 5 * math.sin(t + 1)))
    c["Exposed"] = (48, True, exposed)

    # Stuck (Lao Kẹp into a wall or a column): both claws jammed ahead, the body strains and jerks, the tail quivers up (36 frames, loops)
    def stuck(f):
        t = f / 36 * 2 * math.pi
        jerk = max(0.0, math.sin(t * 3)) ** 3
        return P(body=dict(dy=-0.16 + 0.03 * jerk, dz=-0.06, pitch=-9 + 2 * jerk, roll=shake(f, 2.2, 1.1), yaw=shake(f, 1.5, 0.8)), tail=dict(wind=34 + 6 * jerk, sway=shake(f, 6, 1.3)),
                 claws=dict(lift=6, spread=4 + 3 * jerk, open=-8))
    c["Stuck"] = (36, True, stuck)
    return c


def _with_breath(p, f):
    p = {k: (dict(v) if isinstance(v, dict) else v) for k, v in p.items()}
    p["body"]["dz"] = p["body"].get("dz", 0) + 0.010 * math.sin(f * 0.5)
    p["body"]["pitch"] = p["body"].get("pitch", 0) + 1.2 * math.sin(f * 0.5)
    return p


def full_pose(delta):
    return _merge(REST, delta)


def _merge(base, over):
    out = {}
    for k in set(base) | set(over):
        a, b = base.get(k), over.get(k)
        if isinstance(a, dict) or isinstance(b, dict):
            out[k] = _merge(a if isinstance(a, dict) else {}, b if isinstance(b, dict) else {})
        else:
            out[k] = b if k in over else a
    return out


# ---------------------------------------------------------------------------------------------------------------------------------------------------------
# build the armature, skin, animate, export
# ---------------------------------------------------------------------------------------------------------------------------------------------------------
def load_mesh(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path, use_anim=False)   # a file rigged before carries its clips: they must not be exported again (Unity saw every clip twice)
    for act in list(bpy.data.actions):
        bpy.data.actions.remove(act)
    for o in list(bpy.data.objects):
        if o.type == "ARMATURE":
            bpy.data.objects.remove(o)
    mesh = max((o for o in bpy.data.objects if o.type == "MESH"), key=lambda o: len(o.data.vertices))
    mesh.parent = None
    mesh.data.transform(mesh.matrix_world)   # to world coordinates: Z up, the scorpion faces -Y, feet on Z=0
    mesh.matrix_world = M.Identity(4)
    mesh.rotation_euler = (0, 0, 0); mesh.location = (0, 0, 0); mesh.scale = (1, 1, 1)
    mesh.vertex_groups.clear()
    for m in list(mesh.modifiers):
        mesh.modifiers.remove(m)
    mesh.data.polygons.foreach_set("use_smooth", [True] * len(mesh.data.polygons))
    return mesh


def build_armature(specs):
    data = bpy.data.armatures.new("Armature")
    arm = bpy.data.objects.new("Armature", data)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    eb = {}
    for n, (h, t, par, rad, roll) in specs.items():
        e = data.edit_bones.new(n)
        e.head, e.tail = h, t
        if roll.length > 1e-6:
            e.align_roll(roll)
        eb[n] = e
    for n, (_, _, par, _, _) in specs.items():
        if par:
            eb[n].parent = eb[par]
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm


def skin(mesh, arm, specs):
    """Weights: the three nearest bones (distance to the bone segment minus its radius), falling off with distance^4."""
    verts = np.array([v.co[:] for v in mesh.data.vertices])
    names = list(specs)
    D = np.zeros((len(verts), len(names)))
    for j, n in enumerate(names):
        h, t, _, rad, _ = specs[n]
        a, b = np.array(h[:]), np.array(t[:])
        ab = b - a
        tt = np.clip(((verts - a) @ ab) / max(ab @ ab, 1e-9), 0, 1)
        d = np.linalg.norm(verts - (a + tt[:, None] * ab), axis=1)
        reach = next((v for k, v in REACH.items() if n.startswith(k)), None)
        D[:, j] = np.where(d > reach, 1e6, d - rad) if reach is not None else d - rad
    D = np.maximum(D, 0.01)
    idx = np.argsort(D, axis=1)[:, :3]
    groups = {n: mesh.vertex_groups.new(name=n) for n in names}
    for i in range(len(verts)):
        w = 1.0 / D[i, idx[i]] ** 4
        w /= w.sum()
        for k, j in enumerate(idx[i]):
            groups[names[j]].add([i], float(w[k]), "REPLACE")
    mesh.parent = arm
    mod = mesh.modifiers.new("Armature", "ARMATURE")
    mod.object = arm


def euler_of(m4):
    return m4.to_euler("XYZ")


def animate(arm, sk, rig, clips, report):
    arm.animation_data_create()
    for n in sk.order:
        arm.pose.bones[n].rotation_mode = "XYZ"
    for name, (frames, loop, fn) in clips.items():
        act = bpy.data.actions.new(name)
        act.use_fake_user = True
        arm.animation_data.action = act
        rig.theta = {}
        n_total = frames + 1 if not loop else frames
        prev_eul = {}
        feet_z, err0 = [], rig.max_err
        rig.max_err = 0.0
        for f in range(n_total):
            pose = full_pose(fn(f))
            bas = rig.solve(pose)
            for n in sk.order:
                m = bas.get(n, M.Identity(4))
                pb = arm.pose.bones[n]
                pb.location = m.to_translation()
                e = m.to_euler("XYZ", prev_eul.get(n))
                prev_eul[n] = e
                pb.rotation_euler = e
                pb.keyframe_insert("location", frame=f + 1, group=n)
                pb.keyframe_insert("rotation_euler", frame=f + 1, group=n)
            pose_m = sk.fk(bas)
            feet_z.append(min(float((pose_m["Pede%s%d_B" % (s, k[0])] @ V((0, sk.length["Pede%s%d_B" % (s, k[0])], 0)))[2]) for s in "LR" for k in LEGS))
        act.frame_range = (1, n_total)
        act.use_frame_range = True
        report[name] = dict(frames=n_total, min_foot_z=min(feet_z), foot_err=rig.max_err)
        rig.max_err = max(rig.max_err, err0)
        rig.worst = max(getattr(rig, "worst", 0.0), report[name]["foot_err"])
    arm.animation_data.action = clips_action(arm, "Idle")


def clips_action(arm, name):
    return bpy.data.actions[name]


def export(path, arm, mesh):
    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in (arm, mesh):
        o.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE", add_leaf_bones=False, path_mode="STRIP",
        bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0, bake_anim_step=1.0)


def verify(rig, sk, report):
    bad = []
    if rig.max_err > 0.06:
        bad.append("a foot missed its target by %.3f m (reach)" % rig.max_err)
    for name, r in report.items():
        if r["min_foot_z"] < FOOT_Z - 0.06 and not name.startswith(("Tell_Burrow", "Strike_Burrow")):
            bad.append("%s: a foot goes %.3f below the ground" % (name, FOOT_Z - r["min_foot_z"]))
    return bad


def render_previews(arm, mesh, out_dir, clips, sk, rig):
    """A few frames rendered with Cycles (CPU) for a look: Idle, a walk, and each Strike's peak."""
    os.makedirs(out_dir, exist_ok=True)
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"; sc.cycles.device = "CPU"; sc.cycles.samples = 16
    sc.render.resolution_x, sc.render.resolution_y = 640, 480
    w = bpy.data.worlds.new("w"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs[1].default_value = 1.4; sc.world = w
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sun.rotation_euler = (0.8, 0.2, 0.6); sun.data.energy = 3; sc.collection.objects.link(sun)
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
    c0 = V((0, 0, 0.7))
    shots = [("idle", "Idle", 0), ("walkF", "Walk_F", 5), ("walkL", "Walk_L", 5), ("sting_tell", "Tell_Sting", 24), ("sting_hit", "Strike_Sting", 6),
             ("slam_tell", "Tell_TailSlam", 24), ("slam_hit", "Strike_TailSlam", 8), ("volley", "Tell_Volley", 24), ("burrow", "Tell_Burrow", 18),
             ("pinch", "Tell_Pinch", 24), ("burst", "Tell_Burst", 54), ("death", "Death", 40)]
    for tag, act, fr in shots:
        arm.animation_data.action = bpy.data.actions[act]
        sc.frame_set(fr + 1)
        az, el, d = math.radians(60), math.radians(14), 4.6
        cam.location = c0 + V((d * math.sin(az) * math.cos(el), -d * math.cos(az) * math.cos(el), d * math.sin(el)))
        cam.rotation_euler = (c0 - cam.location).to_track_quat("-Z", "Y").to_euler()
        sc.render.filepath = os.path.join(out_dir, "scorpion_%s.png" % tag)
        bpy.ops.render.render(write_still=True)


def main(argv):
    previews = None
    if "--previews" in argv:
        i = argv.index("--previews"); previews = argv[i + 1]; argv = argv[:i] + argv[i + 2:]
    src = argv[0] if len(argv) > 0 else DEFAULT_FBX
    dst = argv[1] if len(argv) > 1 else src
    mesh = load_mesh(src)
    specs = bone_specs()
    arm = build_armature(specs)
    sk = Skeleton(arm)
    rig = Rig(sk)
    clips = make_clips()
    report = {}
    animate(arm, sk, rig, clips, report)   # armature only (no mesh bound yet): cheap
    bad = verify(rig, sk, report)
    for name, r in report.items():
        print("clip %-18s frames %3d  lowest foot %.3f  foot error %.3f" % (name, r["frames"], r["min_foot_z"], r["foot_err"]))
    print("worst foot error %.4f m; signs %s" % (rig.max_err, rig.signs))
    skin(mesh, arm, specs)
    if previews:
        render_previews(arm, mesh, previews, clips, sk, rig)
    export(dst, arm, mesh)
    print("OK", dst, "bones:", len(specs), "clips:", len(clips), "clip base speed %.3f m/s" % CLIP_SPEED)
    if bad:
        print("PROBLEMS:\n  " + "\n  ".join(bad))
        sys.exit(1)


if __name__ == "__main__":
    main([a for a in sys.argv[sys.argv.index("--") + 1:]] if "--" in sys.argv else sys.argv[1:])
