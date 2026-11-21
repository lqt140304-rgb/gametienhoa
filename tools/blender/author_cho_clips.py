"""Hand-authored keyframe clips for Chó Xương (Walk, Pounce_Tell, Pounce_Strike, Pounce_Recover), written as code on the renamed rig.

Every pose is a list of rotations about WORLD axes per bone (X = the dog's left-right axis: + swings a hanging leg back and tips the head down,
Z = up), converted to each bone's own rest frame, so the numbers do not depend on how the bones happen to be rolled. The dog faces -Y.
Used by rig_from_metarig.py (pass --clips); the actions are exported as takes of the FBX.
"""
import math

import bpy
import mathutils

Q = mathutils.Quaternion
V = mathutils.Vector
X, Y, Z = V((1, 0, 0)), V((0, 1, 0)), V((0, 0, 1))
FPS = 24
LEGS = ("FL", "FR", "BL", "BR")
NECK = (("spine.009", 0.35), ("spine.010", 0.35), ("spine.011", 0.30))
BACK = ("spine.005", "spine.006", "spine.007", "spine.008")


def _spec_add(spec, bone, axis, deg):
    spec.setdefault(bone, []).append((axis, deg))


def apply_pose(arm, spec, root_lift=0.0, root_fwd=0.0):
    """Set the whole pose: spec = {bone: [(world_axis, degrees), ...]}; root_lift / root_fwd move the Root bone up / forward (metres)."""
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)
    for name, rots in spec.items():
        pb = arm.pose.bones.get(name)
        if pb is None:
            continue
        R = Q((1, 0, 0, 0))
        for axis, deg in rots:
            R = Q(axis, math.radians(deg)) @ R
        rest = pb.bone.matrix_local.to_quaternion()
        pb.rotation_quaternion = rest.inverted() @ R @ rest
    root = arm.pose.bones["Root"]
    rest = root.bone.matrix_local.to_quaternion()
    root.location = rest.inverted() @ V((0, -root_fwd, root_lift))


def key(arm, frame):
    for pb in arm.pose.bones:
        pb.keyframe_insert("rotation_quaternion", frame=frame)
        if pb.name == "Root":
            pb.keyframe_insert("location", frame=frame)


def new_action(arm, name):
    act = bpy.data.actions.new(name)
    arm.animation_data_create()
    arm.animation_data.action = act
    return act


def smooth(x):
    x = max(0.0, min(1.0, x))
    return x * x * (3 - 2 * x)


# ---------------------------------------------------------------- walk
def walk_pose(f, frames, front_a=22.0, hind_a=20.0, flex=42.0):
    spec = {}
    t = f / frames
    phase = {"FL": 0.0, "BR": 0.0, "FR": 0.5, "BL": 0.5}   # diagonal pairs move together
    for leg in LEGS:
        ph = (t + phase[leg]) % 1.0
        a = front_a if leg[0] == "F" else hind_a
        hip = -a * math.cos(2 * math.pi * ph)               # + back, - forward: foot strike forward at ph 0, lift-off at the back at 0.5
        swing = max(0.0, math.sin(2 * math.pi * (ph - 0.5))) if ph >= 0.5 else 0.0
        fl = flex * swing
        _spec_add(spec, "Leg_" + leg, X, hip)
        if leg[0] == "F":
            _spec_add(spec, "Shin_" + leg, X, fl)
            _spec_add(spec, "Foot_" + leg, X, -0.35 * fl)
        else:
            _spec_add(spec, "Shin_" + leg, X, -0.8 * fl)      # the hock folds the other way
            _spec_add(spec, "Foot_" + leg, X, 0.5 * fl)
    for i, b in enumerate(BACK):
        _spec_add(spec, b, Z, 2.2 * math.sin(2 * math.pi * t * 1.0 + i * 0.5))
    for i in range(1, 6):
        _spec_add(spec, "Tail%d" % i, Z, 7.0 * math.sin(2 * math.pi * t - i * 0.7))
    for bone, share in NECK:
        _spec_add(spec, bone, X, share * 3.0 * math.sin(4 * math.pi * t))
    lift = 0.012 * math.sin(4 * math.pi * t + 0.6)
    return spec, lift


# ---------------------------------------------------------------- pounce
def pose_from(front, front_shin, hind, hind_shin, neck, jaw, back=0.0, lift=0.0, fwd=0.0, tail=0.0):
    spec = {}
    for leg in LEGS:
        if leg[0] == "F":
            _spec_add(spec, "Leg_" + leg, X, front)
            _spec_add(spec, "Shin_" + leg, X, front_shin)
        else:
            _spec_add(spec, "Leg_" + leg, X, hind)
            _spec_add(spec, "Shin_" + leg, X, hind_shin)
    for bone, share in NECK:
        _spec_add(spec, bone, X, share * neck)
    _spec_add(spec, "jaw", X, jaw)
    for b in BACK:
        _spec_add(spec, b, X, back / len(BACK))
    for i in range(1, 6):
        _spec_add(spec, "Tail%d" % i, X, tail / 5.0)
    return spec, lift, fwd


# poses: (front hip, front shin, hind hip, hind shin, neck (+ down), jaw, back, root lift, root forward, tail). Heavy, slow monster: it rocks back, sinks deep, lunges
# with a long slow start, lands hard and settles its weight.
STAND = pose_from(0, 0, 0, 0, 0, 0)
RISE = pose_from(-6, -4, 8, -6, -8, 4, back=3, lift=0.03, tail=4)        # anticipation: weight shifts up and back
CROUCH = pose_from(24, 40, -38, 56, 30, 8, back=-9, lift=-0.20, tail=-12)
LAUNCH = pose_from(-52, 5, 50, -10, -14, 20, back=6, lift=0.08, fwd=0.04, tail=8)
PEAK = pose_from(-58, 0, 56, -6, -10, 22, back=8, lift=0.12, fwd=0.08, tail=10)
LAND = pose_from(-34, 34, 30, 30, 10, 4, back=-6, lift=-0.12, fwd=0.1, tail=-4)      # the impact: legs fold under the weight
SETTLE = pose_from(8, 14, -10, 18, 12, 2, back=-2, lift=-0.05, tail=0)


def lerp_pose(a, b, t):
    """Blend two (spec, lift, fwd) poses; both must come from pose_from (same bones and axes)."""
    spec = {}
    for bone in a[0]:
        spec[bone] = [(ax, (1 - t) * d0 + t * d1) for (ax, d0), (_, d1) in zip(a[0][bone], b[0][bone])]
    return spec, (1 - t) * a[1] + t * b[1], (1 - t) * a[2] + t * b[2]


def author_clips(arm):
    """Create the actions on the armature and return their names."""
    bpy.context.scene.render.fps = FPS
    names = []

    # Idle: 48 frames, a breathing loop (every bone is keyed, so the Animator always writes the whole skeleton)
    act = new_action(arm, "Idle")
    for f in range(0, 49):
        t = (f % 48) / 48
        spec = {}
        _spec_add(spec, "spine.007", X, 0.8 * math.sin(2 * math.pi * t))
        for bone, share in NECK:
            _spec_add(spec, bone, X, share * 1.6 * math.sin(2 * math.pi * t + 0.8))
        for i in range(1, 6):
            _spec_add(spec, "Tail%d" % i, Z, 4.0 * math.sin(2 * math.pi * t - i * 0.6))
        apply_pose(arm, spec, root_lift=0.004 * math.sin(2 * math.pi * t))
        key(arm, f + 1)
    act.use_fake_user = True
    names.append("Idle")

    # Walk: 24 frames, a loop (the last frame equals the first)
    act = new_action(arm, "Walk")
    for f in range(0, 25):
        spec, lift = walk_pose(f % 24, 24)
        apply_pose(arm, spec, root_lift=lift)
        key(arm, f + 1)
    act.use_fake_user = True
    names.append("Walk")

    # Pounce_Tell: 32 frames (1.33 s): rock back a little, then sink slowly into a deep crouch, and hold
    act = new_action(arm, "Pounce_Tell")
    for f in range(0, 33):
        t = f / 32
        if t < 0.25:
            spec, lift, fwd = lerp_pose(STAND, RISE, smooth(t / 0.25))
        else:
            spec, lift, fwd = lerp_pose(RISE, CROUCH, smooth((t - 0.25) / 0.75))
        apply_pose(arm, spec, lift, fwd)
        key(arm, f + 1)
    act.use_fake_user = True
    names.append("Pounce_Tell")

    # Pounce_Strike: 14 frames (0.58 s): a slow, heavy start out of the crouch, then it flies
    act = new_action(arm, "Pounce_Strike")
    for f in range(0, 15):
        t = f / 14
        spec, lift, fwd = lerp_pose(CROUCH, LAUNCH, smooth(t / 0.5) ** 1.6) if t < 0.5 else lerp_pose(LAUNCH, PEAK, smooth((t - 0.5) / 0.5))
        apply_pose(arm, spec, lift, fwd)
        key(arm, f + 1)
    act.use_fake_user = True
    names.append("Pounce_Strike")

    # Pounce_Recover: 43 frames (1.8 s): a hard landing, the weight sags and holds, then it straightens up slowly
    act = new_action(arm, "Pounce_Recover")
    for f in range(0, 44):
        t = f / 43
        if t < 0.15:
            spec, lift, fwd = lerp_pose(PEAK, LAND, smooth(t / 0.15))
        elif t < 0.40:
            spec, lift, fwd = lerp_pose(LAND, SETTLE, smooth((t - 0.15) / 0.25))
        else:
            spec, lift, fwd = lerp_pose(SETTLE, STAND, smooth((t - 0.40) / 0.60))
        apply_pose(arm, spec, lift, fwd * (1 - smooth(max(0.0, (t - 0.15) / 0.85))))
        key(arm, f + 1)
    act.use_fake_user = True
    names.append("Pounce_Recover")

    # leave the rig in its rest pose, with no action assigned
    apply_pose(arm, {})
    arm.animation_data.action = None
    return names
