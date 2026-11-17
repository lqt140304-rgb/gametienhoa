using Evolve.Organs;
using Evolve.Survival;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Evolve.Player
{
    /// <summary>Body size curve: grows with equipped organs, capped.</summary>
    public static class BodySize
    {
        public static float ScaleFor(int organCount) => Mathf.Min(1.5f, 1f + 0.06f * organCount);
    }

    /// <summary>
    /// Primordial slime: slow crawl (free) and hop (costs energy).
    /// Movement is camera-relative. Requires a Rigidbody (freeze rotation).
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SurvivalStats))]
    public class SlimeController : MonoBehaviour
    {
        [SerializeField] float crawlSpeed = 1f;
        [Tooltip("Speed multiplier while holding Shift (burns extra food)")]
        [SerializeField] float sprintMultiplier = 1.5f;
        [SerializeField] float acceleration = 12f;
        [SerializeField] float hopForce = 5f;
        [SerializeField] float hopForward = 3f;
        [SerializeField] float groundCheckDistance = 0.15f;
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField] Transform cameraTransform;
        [Tooltip("Squash & stretch target (child mesh)")]
        [SerializeField] Transform visual;

        Rigidbody rb;

        public bool IsSprinting { get; private set; }
        SurvivalStats stats;
        OrganHost organs;
        Vector3 baseScale = Vector3.one;
        float squash;
        Vector3 spawnPoint;
        Collider col;
        SphereCollider sphere;
        CapsuleCollider capsule;
        float baseRadius, baseHeight;

        /// <summary>Current body scale (smoothed toward BodySize.ScaleFor(organ count)). Applied to collider and visual.</summary>
        public float BodyScale { get; private set; } = 1f;
        /// <summary>Collider diameter in world units at BodyScale 1 (cached in Awake).</summary>
        public float BaseDiameter { get; private set; } = 1f;
        const float BodyScaleRate = 0.5f;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            stats = GetComponent<SurvivalStats>();
            organs = GetComponent<OrganHost>();
            col = GetComponent<Collider>();
            sphere = col as SphereCollider;
            capsule = col as CapsuleCollider;
            if (sphere != null) { baseRadius = sphere.radius; baseHeight = 0f; BaseDiameter = baseRadius * 2f * MaxAbs(transform.lossyScale); }
            else if (capsule != null) { baseRadius = capsule.radius; baseHeight = capsule.height; BaseDiameter = baseRadius * 2f * MaxAbs(transform.lossyScale); }
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            if (visual != null) baseScale = visual.localScale;
            spawnPoint = transform.position;
        }

        void Update()
        {
            if (stats.State.IsDead)
            {
                if (stats.State.CanRespawn && KeyPressed.R()) Respawn();
                // Single player only: out of lives = start a fresh run. In co-op teammates rescue (RescueController).
                else if (stats.State.IsCorpse && !NetWorld.CoopActive && KeyPressed.Y()) NewRun();
                return;
            }
            if (ReadHop() && IsGrounded() && stats.State.TrySpendEnergy(stats.Config.HopEnergyCost))
            {
                Vector3 dir = GetMoveDirection();
                rb.AddForce(Vector3.up * hopForce + dir * hopForward, ForceMode.VelocityChange);
                squash = 1f;
            }
            AnimateVisual();
        }

        /// <summary>Return to the spawn point with full stats. Organs are kept (placeholder rule).</summary>
        public void Respawn()
        {
            rb.linearVelocity = Vector3.zero;
            rb.position = spawnPoint;
            transform.position = spawnPoint;
            stats.State.Ambient = stats.Config.DefaultAmbient;
            stats.State.Reset();
        }

        /// <summary>Move instantly (debug teleport).</summary>
        public void Teleport(Vector3 position)
        {
            rb.linearVelocity = Vector3.zero;
            rb.position = position;
            transform.position = position;
        }

        public Vector3 SpawnPoint => spawnPoint;

        /// <summary>Prototype only: restart with all lives. Real design: only a teammate can revive a corpse.</summary>
        void NewRun()
        {
            // The corpse is lost for good: its organs fall off where it lay.
            var interactor = GetComponent<OrganInteractor>();
            if (interactor != null) interactor.DropAllOnDeath();
            stats.State.ResetLives();
            Respawn();
        }

        void FixedUpdate()
        {
            if (stats.State.IsDead) return;
            UpdateBodyScale();
            Vector3 dir = GetMoveDirection();
            stats.IsMoving = dir.sqrMagnitude > 0.01f;

            // Sprinting burns food (see SurvivalState.Tick); impossible when starving.
            IsSprinting = stats.IsMoving && ReadSprint() && stats.State.Hunger > 0f;
            stats.IsSprinting = IsSprinting;

            Vector3 v = rb.linearVelocity;
            float organSpeed = organs != null ? organs.SpeedMultiplier : 1f;
            Vector3 target = dir * crawlSpeed * organSpeed * (IsSprinting ? sprintMultiplier : 1f);
            Vector3 horizontal = Vector3.MoveTowards(new Vector3(v.x, 0, v.z), target, acceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(horizontal.x, v.y, horizontal.z);
        }

        static float MaxAbs(Vector3 v) => Mathf.Max(Mathf.Abs(v.x), Mathf.Max(Mathf.Abs(v.y), Mathf.Abs(v.z)));

        /// <summary>Smooth BodyScale toward the organ-count target, resize the collider from cached base values
        /// and lift the body by the radius growth so it does not sink into the ground. No-op when settled.</summary>
        void UpdateBodyScale()
        {
            float target = organs != null ? BodySize.ScaleFor(organs.Count) : 1f;
            if (Mathf.Approximately(BodyScale, target)) return;
            float old = BodyScale;
            BodyScale = Mathf.MoveTowards(old, target, BodyScaleRate * Time.fixedDeltaTime);
            float growth = 0f;
            if (sphere != null)
            {
                sphere.radius = baseRadius * BodyScale;
                growth = baseRadius * (BodyScale - old);
            }
            else if (capsule != null)
            {
                capsule.radius = baseRadius * BodyScale;
                capsule.height = baseHeight * BodyScale;
                growth = baseHeight * 0.5f * (BodyScale - old);
            }
            if (growth > 0f) rb.position += Vector3.up * (growth * transform.lossyScale.y);
        }

        Vector3 GetMoveDirection()
        {
            Vector2 input = ReadMove();
            if (input.sqrMagnitude < 0.01f) return Vector3.zero;
            Vector3 fwd = cameraTransform ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform ? cameraTransform.right : Vector3.right;
            fwd.y = 0; right.y = 0;
            return Vector3.ClampMagnitude(fwd.normalized * input.y + right.normalized * input.x, 1f);
        }

        bool IsGrounded()
        {
            float extent = col ? col.bounds.extents.y : 0.5f;
            return Physics.Raycast(transform.position, Vector3.down, extent + groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
        }

        void AnimateVisual()
        {
            if (visual == null) return;
            squash = Mathf.MoveTowards(squash, 0f, Time.deltaTime * 3f);
            float wobble = stats.IsMoving ? Mathf.Sin(Time.time * 10f) * 0.05f : 0f;
            float bs = BodyScale;
            float y = 1f + squash * 0.35f + wobble;
            float xz = 1f / Mathf.Sqrt(y); // preserve volume
            visual.localScale = Vector3.Scale(baseScale * bs, new Vector3(xz, y, xz));

            // Face the direction of travel so front-mounted organs (eyes) lead.
            Vector3 hv = rb.linearVelocity; hv.y = 0f;
            if (hv.sqrMagnitude > 0.05f)
                visual.rotation = Quaternion.Slerp(visual.rotation, Quaternion.LookRotation(hv), Time.deltaTime * 10f);
        }

        static Vector2 ReadMove()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            Vector2 v = Vector2.zero;
            if (k != null)
            {
                if (k.wKey.isPressed) v.y += 1; if (k.sKey.isPressed) v.y -= 1;
                if (k.dKey.isPressed) v.x += 1; if (k.aKey.isPressed) v.x -= 1;
            }
            if (Gamepad.current != null) v += Gamepad.current.leftStick.ReadValue();
            return Vector2.ClampMagnitude(v, 1f);
#else
            return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
#endif
        }

        static bool ReadSprint()
        {
#if ENABLE_INPUT_SYSTEM
            return (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
                || (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed);
#else
            return Input.GetKey(KeyCode.LeftShift);
#endif
        }

        static bool ReadHop()
        {
#if ENABLE_INPUT_SYSTEM
            return (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }
    }
}
