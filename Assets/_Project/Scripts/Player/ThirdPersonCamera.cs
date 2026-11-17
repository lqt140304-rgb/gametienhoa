using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Evolve.Player
{
    /// <summary>
    /// Orbit camera behind the character. Mouse mode: hold right mouse to rotate, scroll to zoom.
    /// Aim mode (Alt): the mouse rotates directly and the camera slides over the right shoulder, so the crosshair
    /// is not on top of the character. First person (F5): camera at the character's eyes, body hidden.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 targetOffset = new Vector3(0, 0.5f, 0);
        [SerializeField] float distance = 6f;
        [SerializeField] float minDistance = 2f, maxDistance = 12f;
        [SerializeField] float sensitivity = 0.2f;
        [SerializeField] float minPitch = -10f, maxPitch = 70f;
        [SerializeField] float followSmooth = 10f;
        [SerializeField] LayerMask collisionMask = ~0;

        [Header("Aim mode (over the shoulder)")]
        [Tooltip("How far the camera slides: x = to the right, y = up, so the character sits lower left of the crosshair")]
        [SerializeField] Vector2 shoulderOffset = new Vector2(1.0f, 0.7f);
        [Tooltip("Camera distance while aiming (closer than normal)")]
        [SerializeField] float aimDistance = 4.5f;

        [Header("First person")]
        [Tooltip("Height of the eyes above the character's centre")]
        [SerializeField] float eyeHeight = 0.45f;
        [SerializeField] float firstPersonPitch = 80f;

        float yaw, pitch = 25f;
        float aimBlend;
        Vector3 focus;
        readonly List<Renderer> hiddenBody = new List<Renderer>();
        float nextBodyScan;

        void Start()
        {
            if (target) focus = target.position + targetOffset;
            yaw = transform.eulerAngles.y;
        }

        /// <summary>Spectator mode: look at this instead of our own body (set by the co-op rescue code).</summary>
        public Transform Override;

        void LateUpdate()
        {
            Transform target = Override != null ? Override : this.target;
            if (!target) return;
            Vector2 look = Vector2.zero; float zoom = 0;
            if (!Evolve.Survival.SurvivalHUD.BlocksInput(target.gameObject)) ReadInput(out look, out zoom);

            bool firstPerson = AimController.FirstPerson;
            float minP = firstPerson ? -firstPersonPitch : minPitch;
            float maxP = firstPerson ? firstPersonPitch : maxPitch;
            yaw += look.x * sensitivity;
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, minP, maxP);
            distance = Mathf.Clamp(distance - zoom, minDistance, maxDistance);

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);

            if (firstPerson)
            {
                HideBody(true);
                transform.SetPositionAndRotation(Shaken(target.position + Vector3.up * eyeHeight), rot);
                focus = target.position + targetOffset; // so leaving first person does not jump
                return;
            }
            HideBody(false);

            // Slide over the shoulder while aiming (smoothly).
            aimBlend = Mathf.MoveTowards(aimBlend, AimController.Aiming ? 1f : 0f, Time.deltaTime * 6f);

            focus = Vector3.Lerp(focus, target.position + targetOffset, 1f - Mathf.Exp(-followSmooth * Time.deltaTime));
            Vector3 shoulder = (rot * Vector3.right * shoulderOffset.x + Vector3.up * shoulderOffset.y) * aimBlend;
            Vector3 anchor = focus + shoulder;
            float dist = Mathf.Lerp(distance, Mathf.Min(distance, aimDistance), aimBlend);
            Vector3 dir = rot * Vector3.back;

            float d = dist;
            if (Physics.SphereCast(anchor, 0.2f, dir, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(target))
                d = hit.distance;

            transform.SetPositionAndRotation(Shaken(anchor + dir * d), rot);
        }

        Evolve.Survival.SurvivalStats shakeStats;
        Transform shakeStatsFor;

        /// <summary>Stress shake: a final offset on the finished position (never fed back into focus), local slime only.</summary>
        Vector3 Shaken(Vector3 pos)
        {
            if (Override != null || target == null) return pos; // spectating: not our body
            if (shakeStatsFor != target) { shakeStatsFor = target; shakeStats = target.GetComponent<Evolve.Survival.SurvivalStats>(); }
            if (shakeStats == null || shakeStats.Config == null || shakeStats.State.IsDead) return pos;
            float amp = Evolve.Survival.StressEffects.CameraShake(shakeStats.State.Stress, shakeStats.Config);
            return amp > 0f ? pos + Random.insideUnitSphere * amp : pos;
        }

        /// <summary>In first person the body (and organs, which can grow while we play) must not block the view.</summary>
        void HideBody(bool hide)
        {
            if (!hide)
            {
                if (hiddenBody.Count == 0) return;
                foreach (var r in hiddenBody) if (r != null) r.enabled = true;
                hiddenBody.Clear();
                return;
            }
            if (Time.time < nextBodyScan) return;
            nextBodyScan = Time.time + 0.25f;
            foreach (var r in target.GetComponentsInChildren<Renderer>())
                if (r.enabled) { r.enabled = false; hiddenBody.Add(r); }
        }

        static void ReadInput(out Vector2 look, out float zoom)
        {
#if ENABLE_INPUT_SYSTEM
            look = Vector2.zero; zoom = 0f;
            var m = Mouse.current;
            if (m != null)
            {
                if (AimController.Aiming || m.rightButton.isPressed) look = m.delta.ReadValue();
                zoom = m.scroll.ReadValue().y * 0.01f;
            }
            if (Gamepad.current != null) look += Gamepad.current.rightStick.ReadValue() * 10f;
#else
            look = (AimController.Aiming || Input.GetMouseButton(1)) ? new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f : Vector2.zero;
            zoom = Input.mouseScrollDelta.y;
#endif
        }
    }
}
