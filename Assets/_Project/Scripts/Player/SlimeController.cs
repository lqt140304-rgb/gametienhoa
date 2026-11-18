using Evolve.Organs;
using Evolve.Survival;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Evolve.Player
{
    /// <summary>
    /// Primordial slime: slow crawl (free) and hop (costs energy).
    /// Movement is camera-relative. Requires a Rigidbody (freeze rotation).
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SurvivalStats))]
    public class SlimeController : MonoBehaviour
    {
        [SerializeField] float crawlSpeed = 1f;
        /// <summary>Speed multiplier set by other systems (1 = normal).</summary>
        [System.NonSerialized] public float SpeedScale = 1f;
        /// <summary>Speed factor from a core (Nhân Quen Đất): 1 = none. Written by PlayerTraits.</summary>
        [System.NonSerialized] public float TraitSpeed = 1f;
        float frozenUntil;
        Evolve.Traits.PlayerTraits traitsComp;
        Vector3 steer;
        const float SteerRate = 6f;   // how fast the movement direction can change when Luống Cuống slows it (full reversal in about a third of a second at normal)

        /// <summary>Run Rẩy: the body stops and cannot hop for this many seconds.</summary>
        public void FreezeFor(float seconds)
        {
            if (seconds > 0f) frozenUntil = Mathf.Max(frozenUntil, Time.time + seconds);
        }
        /// <summary>In water at least SwimLogic.SwimDepth deep: no gravity, Space rises, Ctrl dives.</summary>
        public bool IsSwimming { get; private set; }
        /// <summary>In shallower water: walks, but slower.</summary>
        public bool IsWading { get; private set; }
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
        float squash, squashTarget, wobbleAmp, airK, airTime, scaleK = 1f, fallSpeed;
        Evolve.Visuals.SlimeGelAnimator jellyAnimator;
        Vector3 spawnPoint;
        Collider col;
        SphereCollider sphere;
        CapsuleCollider capsule;
        float baseRadius, baseHeight;
        bool layoutWater;                       // the scene is built from World/ZoneLayout (it has its water depths)
        Evolve.World.WaterSensor waterSensor;

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
            traitsComp = GetComponent<Evolve.Traits.PlayerTraits>();
            if (traitsComp == null) traitsComp = gameObject.AddComponent<Evolve.Traits.PlayerTraits>();
            col = GetComponent<Collider>();
            sphere = col as SphereCollider;
            capsule = col as CapsuleCollider;
            if (sphere != null) { baseRadius = sphere.radius; baseHeight = 0f; BaseDiameter = baseRadius * 2f * MaxAbs(transform.lossyScale); }
            else if (capsule != null) { baseRadius = capsule.radius; baseHeight = capsule.height; BaseDiameter = baseRadius * 2f * MaxAbs(transform.lossyScale); }
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            if (visual != null) baseScale = visual.localScale;
            spawnPoint = transform.position;
            waterSensor = GetComponent<Evolve.World.WaterSensor>();
            layoutWater = GameObject.Find("Water Zones") != null; // made by the Underroot Lake builder
            if (layoutWater)
            {
                if (GetComponent<Evolve.World.SwimRipple>() == null) gameObject.AddComponent<Evolve.World.SwimRipple>();
                if (cameraTransform != null && cameraTransform.GetComponent<Evolve.World.UnderwaterTint>() == null)
                    cameraTransform.gameObject.AddComponent<Evolve.World.UnderwaterTint>();
                if (cameraTransform != null && cameraTransform.GetComponent<Evolve.World.ZoneAtmosphere>() == null)
                    cameraTransform.gameObject.AddComponent<Evolve.World.ZoneAtmosphere>(); // per-zone fog and a few particles
            }
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
            if (!IsSwimming && ReadHop() && IsGrounded() && stats.State.TrySpendEnergy(stats.Config.HopEnergyCost))
            {
                Vector3 dir = GetMoveDirection();
                float k = Mathf.Sqrt(Mathf.Clamp(transform.lossyScale.y, 0.3f, 1.5f));   // a smaller slime hops proportionally lower (height grows with speed squared)
                rb.AddForce(Vector3.up * hopForce * k + dir * hopForward * k, ForceMode.VelocityChange);
                squashTarget = 1f; // eased in (see AnimateVisual), never an instant jump
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
            if (Time.time < frozenUntil) dir = Vector3.zero;   // Run Rẩy
            var traits = traitsComp;
            float turn = traits != null ? Evolve.Traits.FlawRules.TurnRateMultiplier(stats.State.Health / Mathf.Max(1f, stats.State.MaxHealth), traits.FlawStrength(Evolve.Traits.FlawRules.LuongCuong)) : 1f;
            if (turn < 0.999f) { steer = Vector3.MoveTowards(steer, dir, SteerRate * turn * Time.fixedDeltaTime); dir = steer; }   // Luống Cuống: steering is sluggish
            else steer = dir;
            stats.IsMoving = dir.sqrMagnitude > 0.01f;

            // Sprinting burns food (see SurvivalState.Tick); impossible when starving.
            IsSprinting = stats.IsMoving && ReadSprint() && stats.State.Hunger > 0f;
            stats.IsSprinting = IsSprinting;

            Vector3 v = rb.linearVelocity;
            float organSpeed = organs != null ? organs.SpeedMultiplier : 1f;
            UpdateWater();
            float speed = crawlSpeed * organSpeed * SpeedScale * TraitSpeed * (IsSprinting ? sprintMultiplier : 1f)
                * (IsSwimming || IsWading ? SwimLogic.SpeedScale : 1f);
            if (IsSwimming)
            {
                float vertical = ReadVertical();
                Vector3 swim = SwimLogic.Velocity(dir, vertical, speed);
                // float low, only the back above the surface, bobbing gently; Space never lifts it higher than that
                float floatY = SwimLogic.TargetY(Evolve.World.ZoneLayout.WaterLevel) + 0.03f * Mathf.Sin(Time.time * 0.6f * Mathf.PI * 2f);
                if (vertical >= 0f && rb.position.y >= floatY - 0.02f) swim.y = Mathf.Clamp((floatY - rb.position.y) * 3f, -0.4f, 0.4f);
                else if (vertical == 0f) swim.y = Mathf.Min(swim.y, (floatY - rb.position.y) * 3f);
                rb.linearVelocity = Vector3.MoveTowards(v, swim, acceleration * Time.fixedDeltaTime);
                stats.State.TrySpendEnergy(SwimLogic.EnergyPerSecond * Time.fixedDeltaTime);
                return;
            }
            Vector3 target = dir * speed;
            Vector3 horizontal = Vector3.MoveTowards(new Vector3(v.x, 0, v.z), target, acceleration * Time.fixedDeltaTime);
            // Hug the ground while crawling: on a slope the body moves along the surface (up or down it) instead of
            // bumping into each rise or flying off each dip, which read as stutter. It never presses into the ground
            // (that would stick it in place) and never cancels a hop or a fall.
            if (dir.sqrMagnitude > 0.01f && v.y < 1f)
            {
                float extent = col ? col.bounds.extents.y : 0.5f;
                if (Physics.Raycast(rb.position, Vector3.down, out var hit, extent + 0.3f, groundMask, QueryTriggerInteraction.Ignore)
                    && hit.normal.y > 0.6f && hit.collider.attachedRigidbody != rb)
                {
                    Vector3 along = Vector3.ProjectOnPlane(horizontal, hit.normal);
                    if (along.sqrMagnitude > 1e-6f) along = along.normalized * horizontal.magnitude;
                    float y = hit.distance > extent + 0.06f ? Mathf.Min(v.y, along.y - 1f) : along.y;   // a small gap: settle down onto it
                    rb.linearVelocity = new Vector3(along.x, y, along.z);
                    return;
                }
            }
            rb.linearVelocity = new Vector3(horizontal.x, v.y, horizontal.z);
        }

        /// <summary>Swim in deep layout water, wade in shallow water (any scene: inside a WaterZone).</summary>
        void UpdateWater()
        {
            bool wasSwimming = IsSwimming;
            if (layoutWater)
            {
                Vector3 p = rb.position;
                float depth = Evolve.World.ZoneLayout.DepthAt(p.x, p.z);
                // swim only while below the surface (a hop out of the water lands normally)
                IsSwimming = SwimLogic.IsSwimming(depth) && p.y < Evolve.World.ZoneLayout.WaterLevel + 0.2f;
                IsWading = !IsSwimming && depth > 0.05f;
            }
            else
            {
                IsSwimming = false;
                IsWading = waterSensor != null && waterSensor.InWater;
            }
            if (IsSwimming != wasSwimming) rb.useGravity = !IsSwimming;
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

        bool wasAirborne;

        void AnimateVisual()
        {
            if (visual == null) return;
            float dt = Time.deltaTime;

            // "Grounded" with a short grace period, so a flickering ground check never makes the body pop.
            if (IsGrounded() || IsSwimming) airTime = 0f; else airTime += dt;
            bool grounded = airTime < 0.12f;

            // Landing: spread out flat (negative squash), then settle.
            if (!grounded) fallSpeed = Mathf.Min(fallSpeed, rb.linearVelocity.y);
            if (wasAirborne && grounded)
            {
                if (fallSpeed < -1.5f) squashTarget = -0.4f; // only a real drop spreads the body (a bump or a slope never does)
                var jelly = visual.GetComponent<Evolve.Visuals.SlimeGelAnimator>();
                // Only a real fall splats; the small lift of a dash or a step off a bump does not.
                if (jelly != null && fallSpeed < -2.5f) jelly.Land(Mathf.Clamp(-fallSpeed / 7f, 0.3f, 1.2f));
                fallSpeed = 0f;
            }
            wasAirborne = !grounded;

            // Every part of the body scale is eased: the target decays, the body follows the target.
            squashTarget = Mathf.Lerp(squashTarget, 0f, 1f - Mathf.Exp(-7f * dt));
            squash = Mathf.Lerp(squash, squashTarget, 1f - Mathf.Exp(-22f * dt));
            wobbleAmp = Mathf.Lerp(wobbleAmp, stats.IsMoving ? 1f : 0f, 1f - Mathf.Exp(-8f * dt));
            float wobble = Mathf.Sin(Time.time * 10f) * 0.02f * wobbleAmp;
            float airTarget = grounded ? 0f : Mathf.Clamp(Mathf.Abs(rb.linearVelocity.y) * 0.03f, 0f, 0.2f);
            airK = Mathf.Lerp(airK, airTarget, 1f - Mathf.Exp(-16f * dt));

            float bs = BodyScale;
            // One animation at a time: while a skill / attack / absorb plays, the hop squash and wobble step aside.
            if (jellyAnimator == null) jellyAnimator = visual.GetComponent<Evolve.Visuals.SlimeGelAnimator>();
            bool action = jellyAnimator != null && jellyAnimator.ActionPlaying;
            scaleK = Mathf.Lerp(scaleK, action ? 0f : 1f, 1f - Mathf.Exp(-14f * dt));
            float y = 1f + (squash * 0.35f + wobble + airK) * scaleK;
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

        /// <summary>+1 rise (Space / gamepad south), -1 dive (Ctrl / gamepad east), 0 neither.</summary>
        static float ReadVertical()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            var g = Gamepad.current;
            bool up = (k != null && k.spaceKey.isPressed) || (g != null && g.buttonSouth.isPressed);
            bool down = (k != null && k.leftCtrlKey.isPressed) || (g != null && g.buttonEast.isPressed);
#else
            bool up = Input.GetKey(KeyCode.Space);
            bool down = Input.GetKey(KeyCode.LeftControl);
#endif
            return (up ? 1f : 0f) - (down ? 1f : 0f);
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
