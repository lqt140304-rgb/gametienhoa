using Evolve.Maze;
using Evolve.Player;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Thảm thực vật nhỏ có vật lý rẽ ngang sống động khi slime / sinh vật đi qua,
    /// đàn hồi phục hồi mượt mà và rung động nhẹ tự nhiên (idle animation) trong gió ngầm.
    /// Tích hợp lực cản mềm (SoftPlant) khi slime bơi qua thảm cây.
    /// </summary>
    [SelectionBase]
    public class InteractiveFlora : MonoBehaviour
    {
        [Header("Tương Tác Rẽ Ngang (Slime & Sinh Vật)")]
        [Tooltip("Bán kính cảm ứng lực đẩy khi slime tiếp xúc.")]
        [SerializeField] float pushRadius = InteractiveFloraMath.DefaultPushRadius;

        [Tooltip("Góc nghiêng tối đa khi bị đè qua tâm bụi cây (độ).")]
        [SerializeField] float maxBendAngle = InteractiveFloraMath.DefaultMaxBendAngle;

        [Tooltip("Thời gian đàn hồi về vị trí cũ sau khi slime rời đi (giây).")]
        [SerializeField] float recoverySmoothTime = 0.22f;

        [Header("Dao Động Tự Nhiên (Idle Vibration)")]
        [Tooltip("Tần số rung động nhẹ tự nhiên.")]
        [SerializeField] float idleFrequency = InteractiveFloraMath.DefaultIdleFrequency;

        [Tooltip("Biên độ góc rung động nhẹ (độ).")]
        [SerializeField] float idleAmplitude = InteractiveFloraMath.DefaultIdleAmplitude;

        [Header("Lực Cản Slime (Soft Plant)")]
        [Range(0f, 1f)]
        [SerializeField] float stiffness = 0.35f;

        [Header("Visual Root")]
        [Tooltip("Transform con được xoay nghiêng (nếu để trống tự lấy child 0 hoặc chính nó).")]
        [SerializeField] Transform visualTarget;

        Quaternion baseLocalRot = Quaternion.identity;
        Vector3 currentBendAxis = Vector3.forward;
        float currentAngle;
        float angleVelocity;
        float phaseOffset;
        Vector3 lastIntruderPos;
        float lastIntruderTime = -10f;
        bool hasIntruder;
        Camera mainCam;

        void Awake()
        {
            if (visualTarget == null)
            {
                visualTarget = transform.childCount > 0 ? transform.GetChild(0) : transform;
            }
            baseLocalRot = visualTarget.localRotation;
            phaseOffset = (transform.position.x * 2.3f + transform.position.z * 3.7f) % 100f;

            // Đảm bảo có Trigger Collider để đón slime/sinh vật
            var col = GetComponent<Collider>();
            if (col == null)
            {
                var sc = gameObject.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = pushRadius * 0.85f;
                sc.center = new Vector3(0f, 0.45f, 0f);
            }
            else
            {
                col.isTrigger = true;
            }
        }

        void OnTriggerStay(Collider other)
        {
            // Bắt SlimeController hoặc Rigidbody của sinh vật
            var slime = other.GetComponentInParent<SlimeController>();
            if (slime != null)
            {
                slime.NotePlantDrag(PlantDragMath.SpeedFor(stiffness));
                lastIntruderPos = slime.transform.position;
                lastIntruderTime = Time.time;
                hasIntruder = true;
                return;
            }

            if (!other.isTrigger)
            {
                lastIntruderPos = other.transform.position;
                lastIntruderTime = Time.time;
                hasIntruder = true;
            }
        }

        void Update()
        {
            // Culling tối ưu khoảng cách nếu quá xa camera (> 45m)
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam != null)
            {
                float sqrDist = (mainCam.transform.position - transform.position).sqrMagnitude;
                if (sqrDist > 2025f) // > 45m
                {
                    if (currentAngle > 0.05f)
                    {
                        currentAngle = Mathf.MoveTowards(currentAngle, 0f, Time.deltaTime * 60f);
                        ApplyRotation(Vector3.forward, currentAngle, Vector2.zero);
                    }
                    return;
                }
            }

            if (Time.time - lastIntruderTime > 0.08f)
            {
                hasIntruder = false;
            }

            float targetAngle = 0f;
            Vector3 targetAxis = currentBendAxis;

            if (hasIntruder)
            {
                if (InteractiveFloraMath.ComputeHorizontalDeflection(
                    transform.position,
                    lastIntruderPos,
                    pushRadius,
                    maxBendAngle,
                    out Vector3 axis,
                    out float angle))
                {
                    targetAngle = angle;
                    targetAxis = axis;
                    currentBendAxis = Vector3.Slerp(currentBendAxis, axis, Time.deltaTime * 18f);
                }
            }

            currentAngle = InteractiveFloraMath.StepAngle(
                currentAngle,
                targetAngle,
                ref angleVelocity,
                recoverySmoothTime,
                Time.deltaTime);

            Vector2 idle = InteractiveFloraMath.ComputeIdleVibration(
                Time.time,
                idleFrequency,
                idleAmplitude,
                phaseOffset);

            ApplyRotation(currentBendAxis, currentAngle, idle);
        }

        void ApplyRotation(Vector3 bendAxis, float bendAngle, Vector2 idleTilt)
        {
            if (visualTarget == null) return;

            Quaternion bendRot = bendAngle > 0.1f
                ? Quaternion.AngleAxis(bendAngle, bendAxis)
                : Quaternion.identity;

            Quaternion idleRot = Quaternion.Euler(idleTilt.x, 0f, idleTilt.y);

            visualTarget.localRotation = baseLocalRot * bendRot * idleRot;
        }

        void OnDisable()
        {
            if (visualTarget != null)
                visualTarget.localRotation = baseLocalRot;
            currentAngle = 0f;
            angleVelocity = 0f;
            hasIntruder = false;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.35f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.4f, pushRadius);
        }
    }
}
