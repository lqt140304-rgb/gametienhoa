using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Evolve.Player
{
    /// <summary>Simple orbit camera: hold right mouse to rotate, scroll to zoom.</summary>
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

        float yaw, pitch = 25f;
        Vector3 focus;

        void Start()
        {
            if (target) focus = target.position + targetOffset;
            yaw = transform.eulerAngles.y;
        }

        void LateUpdate()
        {
            if (!target) return;
            Vector2 look = Vector2.zero; float zoom = 0;
            if (!Evolve.Survival.SurvivalHUD.BlocksInput(target.gameObject)) ReadInput(out look, out zoom);
            yaw += look.x * sensitivity;
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, minPitch, maxPitch);
            distance = Mathf.Clamp(distance - zoom, minDistance, maxDistance);

            focus = Vector3.Lerp(focus, target.position + targetOffset, 1f - Mathf.Exp(-followSmooth * Time.deltaTime));
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
            Vector3 dir = rot * Vector3.back;

            float d = distance;
            if (Physics.SphereCast(focus, 0.2f, dir, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(target))
                d = hit.distance;

            transform.SetPositionAndRotation(focus + dir * d, rot);
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
