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
            public bool IsRight;
            public float GaitPhase;
            public float RestHeight;
            public float SmoothedGroundDelta;
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
        readonly List<Transform> wingBones = new List<Transform>();
        readonly List<LegInfo> legs = new List<LegInfo>();

        Vector3 prevWorldPos;
        Vector3 prevForward;
        float smoothSpeed;
        float turnRate;
        float stridePhase;
        float idleTime;
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

            isFlier = wingBones.Count > 0 && legs.Count <= 2;
        }

        void DiscoverBones()
        {
            legs.Clear();
            tailBones.Clear();
            wingBones.Clear();

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
                    wingBones.Add(tr);
                else if (n.StartsWith("Leg", StringComparison.OrdinalIgnoreCase) || n.StartsWith("Limb", StringComparison.OrdinalIgnoreCase))
                {
                    Vector3 localPos = transform.InverseTransformPoint(tr.position);
                    bool right = n.Contains("_R") || n.Contains("FR") || n.Contains("BR") || n.Contains("MR") ||
                                 (!n.Contains("_L") && !n.Contains("FL") && !n.Contains("BL") && !n.Contains("ML") && localPos.x >= 0f);
                    rawLegs.Add((tr, right, localPos.z));
                }
            }

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

            if (animator != null)
            {
                animator.SetFloat(SpeedHash, smoothSpeed);
                if (smoothSpeed > 0.08f)
                    animator.speed = Mathf.Clamp(smoothSpeed / Mathf.Max(0.25f, referenceWalkSpeed), 0.65f, 2.2f);
                else
                    animator.speed = 1f;
            }
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

            if (isFlier)
            {
                // Fliers bank smoothly into turns and pitch gently with speed
                float bank = Mathf.Clamp(-turnRate * 0.18f, -24f, 24f);
                float pitch = Mathf.Clamp(smoothSpeed * 4f, 0f, 12f);
                Quaternion targetTilt = Quaternion.Euler(pitch, 0f, bank);
                smoothedSurfaceTilt = Quaternion.Slerp(smoothedSurfaceTilt, targetTilt, 1f - Mathf.Exp(-8f * dt));
                modelRoot.localRotation = baseModelLocalRot * smoothedSurfaceTilt;
                float hoverBob = Mathf.Sin(idleTime * 5.5f + stridePhase * 0.4f) * 0.04f;
                modelRoot.localPosition = baseModelLocalPos + new Vector3(0f, hoverBob, 0f);
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

            float moveBlend = Mathf.Clamp01(smoothSpeed / 0.25f);
            float bob = moveBlend * Mathf.Abs(Mathf.Sin(stridePhase)) * bodyBobAmount;
            modelRoot.localPosition = baseModelLocalPos + new Vector3(0f, smoothedYOffset + bob, 0f);
        }

        void ApplyProceduralGaitAndFootGrounding(float dt)
        {
            float moveBlend = Mathf.Clamp01(smoothSpeed / 0.2f);

            if (legs.Count == 0 && !isFlier)
            {
                // Legless crawler (snails, snakes): subtle lateral wave on root/tail while moving
                if (rootBone != null && moveBlend > 0.02f)
                {
                    float sway = Mathf.Sin(stridePhase) * 6f * moveBlend;
                    rootBone.localRotation *= Quaternion.Euler(0f, sway, 0f);
                }
                for (int i = 0; i < tailBones.Count; i++)
                {
                    float wave = Mathf.Sin(stridePhase - i * 0.9f) * 14f * moveBlend;
                    tailBones[i].localRotation *= Quaternion.Euler(0f, wave, 0f);
                }
                return;
            }

            if (isFlier) return;

            // Body roll & counter-head/tail motion synced to steps
            if (rootBone != null && moveBlend > 0.02f)
            {
                float roll = Mathf.Sin(stridePhase) * bodyRollAmount * moveBlend;
                rootBone.localRotation *= Quaternion.Euler(0f, 0f, roll);
            }
            if (headBone != null && moveBlend > 0.02f)
            {
                float headCounter = -Mathf.Sin(stridePhase) * (bodyRollAmount * 0.65f) * moveBlend;
                headBone.localRotation *= Quaternion.Euler(0f, headCounter, 0f);
            }
            for (int i = 0; i < tailBones.Count; i++)
            {
                float tailSway = Mathf.Sin(stridePhase + Mathf.PI + i * 0.6f) * 8f * moveBlend;
                tailBones[i].localRotation *= Quaternion.Euler(0f, tailSway, 0f);
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
                leg.Bone.localRotation *= Quaternion.Euler(swingAngle, 0f, sideSign * (liftAngle + ikAdaptAngle));
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
    }
}
