using System;
using System.Collections.Generic;
using Evolve.World;
using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>
    /// Điều khiển Animator theo tốc độ thực tế, nghiêng thân theo độ dốc/gập ghềnh vật lý
    /// và chỉnh từng bước chân (lift arc + raycast tiếp đất) trong LateUpdate cho toàn bộ 32 loài sinh vật.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class CreatureLocomotion : MonoBehaviour
    {
        struct LegInfo
        {
            public Transform Bone;
            public Quaternion RestLocalRot;
            public bool IsRight;
            public float GaitPhase;
            public float RestHeight;
            public float SmoothedGroundDelta;
        }

        struct WingInfo
        {
            public Transform Bone;
            public Quaternion RestLocalRot;
            public Vector3 LocalFlapAxis;
            public Vector3 LocalTwistAxis;
            public bool IsRight;
            public bool IsOuterSegment;
            public bool IsHindWing;
        }

        [Header("Terrain Adaptation")]
        [SerializeField] float footprintRadius = 0.45f;
        [SerializeField] float maxTiltAngle = 38f;
        [SerializeField] float tiltSmooth = 10f;
        [SerializeField] float heightFollowSmooth = 14f;

        [Header("Gait & Foot IK")]
        [SerializeField] float strideLength = 0.65f;
        [SerializeField] float stepSwingAngle = 28f;
        [SerializeField] float stepLiftAngle = 18f;
        [SerializeField] float bodyBobAmount = 0.035f;
        [SerializeField] float bodyRollAmount = 4.5f;
        [SerializeField] float referenceWalkSpeed = 0.8f;

        Transform modelRoot;
        Animator animator;
        Collider selfCollider;

        Transform rootBone;
        Transform headBone;
        readonly List<Transform> tailBones = new List<Transform>();
        readonly List<WingInfo> wings = new List<WingInfo>();
        readonly List<LegInfo> legs = new List<LegInfo>();

        Quaternion restRootLocalRot = Quaternion.identity;
        Quaternion restHeadLocalRot = Quaternion.identity;
        readonly Dictionary<Transform, Quaternion> restTailLocalRots = new Dictionary<Transform, Quaternion>();

        bool isCocHoGia;
        bool isDomDom;
        bool isBoCanhCung;
        bool isSwimming;

        float flightTimeBeforePerch;
        float perchTimer;
        bool isPerching;

        float forageFlightTimer;
        float forageFlightDuration;
        bool isForageFlying;

        Vector3 prevWorldPos;
        Vector3 prevForward;
        float smoothSpeed;
        float turnRate;
        float stridePhase;
        float idleTime;
        float locomotionLayerWeight = 1f;
        bool isFlier;
        bool initialized;

        Quaternion smoothedSurfaceTilt = Quaternion.identity;
        float smoothedYOffset;
        Vector3 baseModelLocalPos;
        Quaternion baseModelLocalRot = Quaternion.identity;

        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int HitHash = Animator.StringToHash("Hit");
        static readonly int GroundMask = ~0;

        public void Configure(float radius, float walkSpeed)
        {
            footprintRadius = Mathf.Clamp(radius * 1.15f, 0.28f, 1.35f);
            referenceWalkSpeed = Mathf.Max(0.25f, walkSpeed);
            strideLength = Mathf.Clamp(radius * 1.6f, 0.4f, 1.4f);
        }

        /// <summary>The creature was scaled by <paramref name="k"/> (see CreatureTuning): its feet and stride grow with it.</summary>
        public void ScaleBy(float k)
        {
            footprintRadius *= k;
            strideLength *= k;
        }

        public void PlayHit()
        {
            if (animator != null && HasTrigger("Hit"))
                animator.SetTrigger(HitHash);
        }

        public void PlayAction(string triggerName)
        {
            if (animator != null && !string.IsNullOrEmpty(triggerName) && HasTrigger(triggerName))
                animator.SetTrigger(triggerName);
        }

        void Awake()
        {
            InitializeIfNeeded();
        }

        void OnEnable()
        {
            InitializeIfNeeded();
            prevWorldPos = transform.position;
            prevForward = transform.forward;
            smoothSpeed = 0f;
            smoothedYOffset = 0f;
            smoothedSurfaceTilt = Quaternion.identity;
        }

        void InitializeIfNeeded()
        {
            if (initialized) return;
            initialized = true;

            selfCollider = GetComponent<Collider>();
            animator = GetComponentInChildren<Animator>(true);

            var modelChild = transform.Find("Model");
            modelRoot = modelChild != null ? modelChild : (animator != null ? animator.transform : transform);
            baseModelLocalPos = modelRoot.localPosition;
            baseModelLocalRot = modelRoot.localRotation;

            DiscoverBones();

            if (TryGetComponent(out CapsuleCollider cap))
            {
                footprintRadius = Mathf.Clamp(cap.radius * 1.15f, 0.28f, 1.35f);
                strideLength = Mathf.Clamp(cap.radius * 1.6f, 0.4f, 1.4f);
            }

            isFlier = wings.Count > 0;

            string objName = gameObject.name;
            isCocHoGia = objName.IndexOf("CocHoGia", StringComparison.OrdinalIgnoreCase) >= 0 || objName.IndexOf("coc_ho_gia", StringComparison.OrdinalIgnoreCase) >= 0;
            isDomDom = objName.IndexOf("DomDom", StringComparison.OrdinalIgnoreCase) >= 0 || objName.IndexOf("dom_dom", StringComparison.OrdinalIgnoreCase) >= 0;
            isBoCanhCung = objName.IndexOf("BoCanhCung", StringComparison.OrdinalIgnoreCase) >= 0 || objName.IndexOf("bo_canh_cung", StringComparison.OrdinalIgnoreCase) >= 0;
            flightTimeBeforePerch = UnityEngine.Random.Range(45f, 75f);
            forageFlightTimer = UnityEngine.Random.Range(12f, 20f);
        }

        void DiscoverBones()
        {
            legs.Clear();
            tailBones.Clear();
            wings.Clear();

            var all = modelRoot.GetComponentsInChildren<Transform>(true);
            var rawLegs = new List<(Transform bone, bool isRight, float zPos)>();

            for (int i = 0; i < all.Length; i++)
            {
                var tr = all[i];
                if (tr == modelRoot) continue;
                string n = tr.name;

                if (rootBone == null && (n.Equals("Root", StringComparison.OrdinalIgnoreCase) || n.Equals("Body", StringComparison.OrdinalIgnoreCase)))
                    rootBone = tr;
                else if (headBone == null && n.Equals("Head", StringComparison.OrdinalIgnoreCase))
                    headBone = tr;
                else if (n.StartsWith("Tail", StringComparison.OrdinalIgnoreCase))
                    tailBones.Add(tr);
                else if (n.StartsWith("Wing", StringComparison.OrdinalIgnoreCase))
                {
                    Vector3 localPos = transform.InverseTransformPoint(tr.position);
                    bool right = n.Contains("_R") || n.Contains("FR") || n.Contains("BR") ||
                                 (!n.Contains("_L") && !n.Contains("FL") && !n.Contains("BL") && localPos.x >= 0f);
                    bool outer = n.EndsWith("2", StringComparison.Ordinal);
                    bool hind = n.Contains("BL") || n.Contains("BR");
                    Vector3 flapAxis = tr.InverseTransformDirection(transform.forward);
                    if (flapAxis.sqrMagnitude < 1e-5f) flapAxis = Vector3.forward;
                    Vector3 twistAxis = tr.InverseTransformDirection(right ? transform.right : -transform.right);
                    if (twistAxis.sqrMagnitude < 1e-5f) twistAxis = Vector3.up;
                    wings.Add(new WingInfo
                    {
                        Bone = tr,
                        RestLocalRot = tr.localRotation,
                        LocalFlapAxis = flapAxis.normalized,
                        LocalTwistAxis = twistAxis.normalized,
                        IsRight = right,
                        IsOuterSegment = outer,
                        IsHindWing = hind
                    });
                }
                else if (n.StartsWith("Leg", StringComparison.OrdinalIgnoreCase) || n.StartsWith("Limb", StringComparison.OrdinalIgnoreCase))
                {
                    Vector3 localPos = transform.InverseTransformPoint(tr.position);
                    bool right = n.Contains("_R") || n.Contains("FR") || n.Contains("BR") || n.Contains("MR") ||
                                 (!n.Contains("_L") && !n.Contains("FL") && !n.Contains("BL") && !n.Contains("ML") && localPos.x >= 0f);
                    rawLegs.Add((tr, right, localPos.z));
                }
            }

            if (rootBone != null) restRootLocalRot = rootBone.localRotation;
            if (headBone != null) restHeadLocalRot = headBone.localRotation;
            restTailLocalRots.Clear();
            foreach (var tb in tailBones)
                if (tb != null) restTailLocalRots[tb] = tb.localRotation;

            // Sort front-to-back so diagonal/tripod/wave gait phases alternate naturally
            rawLegs.Sort((a, b) => b.zPos.CompareTo(a.zPos));
            int leftIdx = 0, rightIdx = 0;
            for (int i = 0; i < rawLegs.Count; i++)
            {
                var (bone, isRight, _) = rawLegs[i];
                int sideIndex = isRight ? rightIdx++ : leftIdx++;
                float phase;
                if (rawLegs.Count <= 4)
                {
                    // Diagonal tetrapod gait: FL+BR vs FR+BL
                    phase = ((sideIndex % 2 == 0) ^ isRight) ? 0f : Mathf.PI;
                }
                else
                {
                    // Alternating tripod / wave gait for 6+ legged insects & spiders
                    phase = ((sideIndex % 2 == 0) ^ isRight ? 0f : Mathf.PI) + (sideIndex * 0.45f);
                }

                float restH = Mathf.Max(0.08f, transform.InverseTransformPoint(bone.position).y);
                legs.Add(new LegInfo
                {
                    Bone = bone,
                    RestLocalRot = bone.localRotation,
                    IsRight = isRight,
                    GaitPhase = phase,
                    RestHeight = restH,
                    SmoothedGroundDelta = 0f
                });
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0.0001f) return;

            Vector3 pos = transform.position;
            Vector3 disp = pos - prevWorldPos;
            disp.y = 0f;
            float instantSpeed = disp.magnitude / dt;
            smoothSpeed = Mathf.Lerp(smoothSpeed, instantSpeed, 1f - Mathf.Exp(-12f * dt));

            Vector3 fwd = transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.001f)
            {
                fwd.Normalize();
                float signedTurn = Vector3.SignedAngle(prevForward, fwd, Vector3.up) / dt;
                turnRate = Mathf.Lerp(turnRate, Mathf.Clamp(signedTurn, -180f, 180f), 1f - Mathf.Exp(-10f * dt));
                prevForward = fwd;
            }
            prevWorldPos = pos;

            if (smoothSpeed > 0.04f)
                stridePhase += (smoothSpeed * dt / Mathf.Max(0.25f, strideLength)) * Mathf.PI * 2f;
            idleTime += dt;

            // Cóc Hồ Già: bơi khi xuống nước
            if (isCocHoGia)
            {
                float waterDepth = World.WaterField.DepthAt(pos.x, pos.z);
                bool inWater = waterDepth > 0.15f;
                if (inWater != isSwimming)
                {
                    isSwimming = inWater;
                    if (animator != null && HasParameter("Swimming", AnimatorControllerParameterType.Bool))
                        animator.SetBool("Swimming", inWater);
                    if (inWater && HasTrigger("Swim"))
                        animator.SetTrigger("Swim");
                }
            }

            // Đom Đóm Sắc Màu: chu kỳ bay tầm trung thấp & đáp xuống nghỉ (Perch)
            if (isDomDom)
            {
                if (!isPerching)
                {
                    flightTimeBeforePerch -= dt;
                    if (flightTimeBeforePerch <= 0f && smoothSpeed < 0.35f)
                    {
                        isPerching = true;
                        perchTimer = UnityEngine.Random.Range(8f, 15f);
                        if (animator != null && HasParameter("Perching", AnimatorControllerParameterType.Bool))
                            animator.SetBool("Perching", true);
                        if (HasTrigger("Perch")) animator.SetTrigger("Perch");
                    }
                }
                else
                {
                    perchTimer -= dt;
                    Transform threat = NearestThreat(pos, 5.5f);
                    if (perchTimer <= 0f || threat != null)
                    {
                        isPerching = false;
                        flightTimeBeforePerch = UnityEngine.Random.Range(50f, 90f);
                        if (animator != null && HasParameter("Perching", AnimatorControllerParameterType.Bool))
                            animator.SetBool("Perching", false);
                    }
                }
            }

            // Bọ Cánh Cứng Nhỏ: thỉnh thoảng bay nhẹ tìm mồi
            if (isBoCanhCung)
            {
                if (!isForageFlying)
                {
                    if (smoothSpeed > 0.05f) forageFlightTimer -= dt;
                    if (forageFlightTimer <= 0f)
                    {
                        isForageFlying = true;
                        forageFlightDuration = UnityEngine.Random.Range(3.0f, 4.8f);
                        if (animator != null && HasParameter("Flying", AnimatorControllerParameterType.Bool))
                            animator.SetBool("Flying", true);
                        if (HasTrigger("Fly")) animator.SetTrigger("Fly");
                    }
                }
                else
                {
                    forageFlightDuration -= dt;
                    if (forageFlightDuration <= 0f)
                    {
                        isForageFlying = false;
                        forageFlightTimer = UnityEngine.Random.Range(14f, 24f);
                        if (animator != null && HasParameter("Flying", AnimatorControllerParameterType.Bool))
                            animator.SetBool("Flying", false);
                    }
                }
            }

            bool inCombatAnim = IsPlayingCombatState();
            locomotionLayerWeight = Mathf.MoveTowards(locomotionLayerWeight, inCombatAnim ? 0f : 1f, 8f * dt);

            if (animator != null)
            {
                animator.SetFloat(SpeedHash, smoothSpeed);
                if (!inCombatAnim && smoothSpeed > 0.08f)
                    animator.speed = Mathf.Clamp(smoothSpeed / Mathf.Max(0.25f, referenceWalkSpeed), 0.65f, 2.2f);
                else
                    animator.speed = 1f;
            }
        }

        bool IsPlayingCombatState()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return false;
            var cur = animator.GetCurrentAnimatorStateInfo(0);
            if (!cur.IsName("Idle") && !cur.IsName("Walk")) return true;
            if (animator.IsInTransition(0))
            {
                var next = animator.GetNextAnimatorStateInfo(0);
                if (!next.IsName("Idle") && !next.IsName("Walk")) return true;
            }
            return false;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0.0001f) return;

            UpdateTerrainTiltAndHeight(dt);
            ApplyProceduralGaitAndFootGrounding(dt);
        }

        void UpdateTerrainTiltAndHeight(float dt)
        {
            if (modelRoot == null || modelRoot == transform) return;

            Vector3 pos = transform.position;
            Vector3 fwd = transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

            if (isSwimming)
            {
                // Cóc bơi dưới nước: thân nghiêng xuôi dòng, nổi gần mặt nước, nhấp nhô theo nhịp bơi
                float swimPitch = Mathf.Clamp(smoothSpeed * 10f, 6f, 18f);
                float swimRoll = Mathf.Sin(stridePhase) * 5f;
                Quaternion targetTilt = Quaternion.Euler(swimPitch, 0f, swimRoll);
                smoothedSurfaceTilt = Quaternion.Slerp(smoothedSurfaceTilt, targetTilt, 1f - Mathf.Exp(-8f * dt));
                modelRoot.localRotation = baseModelLocalRot * smoothedSurfaceTilt;

                float swimBob = Mathf.Sin(stridePhase * 1.5f + idleTime * 3f) * 0.04f;
                float waterY = World.WaterField.Level - 0.22f;
                smoothedYOffset = Mathf.Lerp(smoothedYOffset, waterY - pos.y, 1f - Mathf.Exp(-12f * dt));
                modelRoot.localPosition = baseModelLocalPos + new Vector3(0f, smoothedYOffset + swimBob, 0f);
                return;
            }

            if (isFlier)
            {
                // Fliers bank smoothly into turns and pitch gently with speed
                float bank = Mathf.Clamp(-turnRate * 0.18f, -24f, 24f);
                float pitch = Mathf.Clamp(smoothSpeed * 4f, 0f, 12f);
                Quaternion targetTilt = Quaternion.Euler(pitch, 0f, bank);
                smoothedSurfaceTilt = Quaternion.Slerp(smoothedSurfaceTilt, targetTilt, 1f - Mathf.Exp(-8f * dt));
                modelRoot.localRotation = baseModelLocalRot * smoothedSurfaceTilt;

                if (isDomDom)
                {
                    // Đom Đóm: bay tầm trung thấp (1.4m) hoặc đậu nghỉ sát đất/đá (0.04m)
                    float targetAlt = isPerching ? 0.04f : 1.45f;
                    float h = SamplePhysicalGroundY(pos, out _, out _);
                    float targetY = (h + targetAlt) - pos.y;
                    smoothedYOffset = Mathf.Lerp(smoothedYOffset, targetY, 1f - Mathf.Exp(-7f * dt));
                    float hoverBob = isPerching ? 0f : (Mathf.Sin(idleTime * 5f + stridePhase * 0.4f) * 0.06f);
                    modelRoot.localPosition = baseModelLocalPos + new Vector3(0f, smoothedYOffset + hoverBob, 0f);
                    return;
                }

                float hoverBob2 = Mathf.Sin(idleTime * 5.5f + stridePhase * 0.4f) * 0.04f;
                modelRoot.localPosition = baseModelLocalPos + new Vector3(0f, hoverBob2, 0f);
                return;
            }

            // Sample 4 physical ground points around the creature footprint + center
            float r = footprintRadius;
            float hCenter = SamplePhysicalGroundY(pos, out Vector3 nCenter, out bool hitCenter);
            float hFront = SamplePhysicalGroundY(pos + fwd * r, out Vector3 nFront, out bool hitF);
            float hBack = SamplePhysicalGroundY(pos - fwd * r, out Vector3 nBack, out bool hitB);
            float hRight = SamplePhysicalGroundY(pos + right * r, out Vector3 nRight, out bool hitR);
            float hLeft = SamplePhysicalGroundY(pos - right * r, out Vector3 nLeft, out bool hitL);

            Vector3 surfaceNormal = Vector3.up;
            if (hitF && hitB && hitR && hitL)
            {
                Vector3 tanFwd = new Vector3(0f, hFront - hBack, 2f * r);
                Vector3 tanRight = new Vector3(2f * r, hRight - hLeft, 0f);
                // Convert local slope tangents into world-space normal
                Vector3 worldFwdSlope = fwd * (2f * r) + Vector3.up * (hFront - hBack);
                Vector3 worldRightSlope = right * (2f * r) + Vector3.up * (hRight - hLeft);
                Vector3 geoNormal = Vector3.Cross(worldFwdSlope, worldRightSlope).normalized;
                if (geoNormal.y < 0f) geoNormal = -geoNormal;
                surfaceNormal = Vector3.Slerp(geoNormal, (nCenter + nFront + nBack + nRight + nLeft).normalized, 0.35f).normalized;
            }
            else if (hitCenter)
            {
                surfaceNormal = nCenter;
            }

            // Clamp tilt angle so steep cave walls don't flip the creature upside down
            float angle = Vector3.Angle(Vector3.up, surfaceNormal);
            if (angle > maxTiltAngle)
                surfaceNormal = Vector3.Slerp(Vector3.up, surfaceNormal, maxTiltAngle / angle);

            Quaternion worldAlign = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
            Quaternion localTilt = Quaternion.Inverse(transform.rotation) * worldAlign * transform.rotation;
            smoothedSurfaceTilt = Quaternion.Slerp(smoothedSurfaceTilt, localTilt, 1f - Mathf.Exp(-tiltSmooth * dt));
            modelRoot.localRotation = baseModelLocalRot * smoothedSurfaceTilt;

            // Hug actual physical floor collider/terrain height cleanly
            float targetYOffset = hitCenter ? Mathf.Clamp(hCenter - pos.y, -0.6f, 0.6f) : 0f;
            smoothedYOffset = Mathf.Lerp(smoothedYOffset, targetYOffset, 1f - Mathf.Exp(-heightFollowSmooth * dt));

            if (isBoCanhCung && isForageFlying)
            {
                // Bọ cánh cứng bay nhẹ: nâng thân cao 0.5m so với mặt đất kèm nhịp rung cánh
                float flightLift = 0.48f + Mathf.Sin(idleTime * 14f) * 0.04f;
                modelRoot.localPosition = baseModelLocalPos + new Vector3(0f, smoothedYOffset + flightLift, 0f);
            }
            else
            {
                float moveBlend = Mathf.Clamp01(smoothSpeed / 0.25f);
                float bob = moveBlend * Mathf.Abs(Mathf.Sin(stridePhase)) * bodyBobAmount;
                modelRoot.localPosition = baseModelLocalPos + new Vector3(0f, smoothedYOffset + bob, 0f);
            }
        }

        void ApplyProceduralGaitAndFootGrounding(float dt)
        {
            if (locomotionLayerWeight <= 0.005f) return;

            float moveBlend = Mathf.Clamp01(smoothSpeed / 0.2f) * locomotionLayerWeight;

            if (wings.Count > 0 && !isPerching)
            {
                ApplyWingFlapping(moveBlend, locomotionLayerWeight);
                if (isFlier) return;
            }

            if (legs.Count == 0 && !isFlier)
            {
                // Legless crawler (snails, snakes): evaluate from restRootLocalRot so it never accumulates/spins
                if (rootBone != null && moveBlend > 0.02f)
                {
                    float sway = Mathf.Sin(stridePhase) * 6f * moveBlend;
                    rootBone.localRotation = restRootLocalRot * Quaternion.Euler(0f, sway, 0f);
                }
                for (int i = 0; i < tailBones.Count; i++)
                {
                    var tb = tailBones[i];
                    if (tb != null && restTailLocalRots.TryGetValue(tb, out var rRot))
                    {
                        float wave = Mathf.Sin(stridePhase - i * 0.9f) * 14f * moveBlend;
                        tb.localRotation = rRot * Quaternion.Euler(0f, wave, 0f);
                    }
                }
                return;
            }

            if (isFlier) return;

            // Body roll & counter-head/tail motion synced to steps (evaluated from rest rotations)
            if (rootBone != null && moveBlend > 0.02f)
            {
                float roll = Mathf.Sin(stridePhase) * bodyRollAmount * moveBlend;
                rootBone.localRotation = restRootLocalRot * Quaternion.Euler(0f, 0f, roll);
            }
            if (headBone != null && moveBlend > 0.02f)
            {
                float headCounter = -Mathf.Sin(stridePhase) * (bodyRollAmount * 0.65f) * moveBlend;
                headBone.localRotation = restHeadLocalRot * Quaternion.Euler(0f, headCounter, 0f);
            }
            for (int i = 0; i < tailBones.Count; i++)
            {
                var tb = tailBones[i];
                if (tb != null && restTailLocalRots.TryGetValue(tb, out var rRot))
                {
                    float tailSway = Mathf.Sin(stridePhase + Mathf.PI + i * 0.6f) * 8f * moveBlend;
                    tb.localRotation = rRot * Quaternion.Euler(0f, tailSway, 0f);
                }
            }

            // Per-leg crisp stepping arc + physical ground raycast contact
            for (int i = 0; i < legs.Count; i++)
            {
                var leg = legs[i];
                if (leg.Bone == null) continue;

                float phase = stridePhase + leg.GaitPhase;
                float swingSin = Mathf.Sin(phase);
                float strideCos = Mathf.Cos(phase);

                // During stance (swingSin <= 0), raycast under this exact foot to plant on uneven terrain
                float targetGroundDelta = 0f;
                if (swingSin <= 0.15f || moveBlend < 0.05f)
                {
                    Vector3 footProbe = leg.Bone.position + transform.forward * (strideCos * footprintRadius * 0.35f * moveBlend);
                    float footGroundY = SamplePhysicalGroundY(footProbe, out _, out bool hit);
                    if (hit)
                    {
                        float expectedFootY = transform.position.y;
                        targetGroundDelta = Mathf.Clamp(footGroundY - expectedFootY, -leg.RestHeight * 0.6f, leg.RestHeight * 0.6f);
                    }
                }

                leg.SmoothedGroundDelta = Mathf.Lerp(leg.SmoothedGroundDelta, targetGroundDelta, 1f - Mathf.Exp(-16f * dt));
                legs[i] = leg;

                float swingAngle = strideCos * stepSwingAngle * moveBlend;
                float liftAngle = Mathf.Max(0f, swingSin) * stepLiftAngle * moveBlend;
                float ikAdaptAngle = (leg.SmoothedGroundDelta / Mathf.Max(0.12f, leg.RestHeight)) * 24f;

                float sideSign = leg.IsRight ? 1f : -1f;
                leg.Bone.localRotation = leg.RestLocalRot * Quaternion.Euler(swingAngle, 0f, sideSign * (liftAngle + ikAdaptAngle));
            }
        }

        void ApplyWingFlapping(float moveBlend, float weight)
        {
            float flapFreq = Mathf.Lerp(9.5f, 16.5f, moveBlend);
            float basePhase = idleTime * flapFreq + stridePhase * 0.5f;
            float baseAmp = Mathf.Lerp(22f, 48f, moveBlend);

            for (int i = 0; i < wings.Count; i++)
            {
                var w = wings[i];
                if (w.Bone == null) continue;

                float phaseOffset = w.IsOuterSegment ? -0.58f : (w.IsHindWing ? -0.35f : 0f);
                float amp = w.IsOuterSegment ? baseAmp * 0.62f : (w.IsHindWing ? baseAmp * 0.78f : baseAmp);
                float wave = Mathf.Sin(basePhase + phaseOffset);

                // Right wing (+X): +angle around forward (+Z) lifts UP; Left wing (-X): -angle lifts UP
                float sideSign = w.IsRight ? 1f : -1f;
                float flapDeg = sideSign * wave * amp;
                float featherDeg = Mathf.Cos(basePhase + phaseOffset) * 10f * (0.4f + 0.6f * moveBlend);

                Quaternion targetRot = w.RestLocalRot
                    * Quaternion.AngleAxis(flapDeg, w.LocalFlapAxis)
                    * Quaternion.AngleAxis(featherDeg, w.LocalTwistAxis);
                w.Bone.localRotation = Quaternion.Slerp(w.Bone.localRotation, targetRot, weight);
            }
        }

        float SamplePhysicalGroundY(Vector3 worldPos, out Vector3 normal, out bool hitPhysical)
        {
            Vector3 origin = new Vector3(worldPos.x, worldPos.y + 1.8f, worldPos.z);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 4.5f, GroundMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != selfCollider && !hit.transform.IsChildOf(transform))
                {
                    normal = hit.normal;
                    hitPhysical = true;
                    return hit.point.y;
                }
            }

            // Fallback to mathematical terrain height if raycast missed or hit self
            normal = Vector3.up;
            hitPhysical = false;
            return GroundHeight.At(worldPos.x, worldPos.z, worldPos.y);
        }

        bool HasTrigger(string name)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return false;
            foreach (var p in animator.parameters)
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == name)
                    return true;
            return false;
        }

        bool HasParameter(string name, AnimatorControllerParameterType type)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return false;
            foreach (var p in animator.parameters)
                if (p.type == type && p.name == name)
                    return true;
            return false;
        }

        Transform NearestThreat(Vector3 fromPos, float radius)
        {
            float r2 = radius * radius;
            foreach (var s in Evolve.Survival.SurvivalStats.All)
            {
                if (s == null || s.State.IsDead) continue;
                Vector3 d = s.transform.position - fromPos; d.y = 0f;
                if (d.sqrMagnitude < r2) return s.transform;
            }
            return null;
        }
    }
}
