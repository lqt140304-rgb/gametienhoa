using Evolve.Organs;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Player
{
    /// <summary>
    /// Switches between mouse mode and aim mode with Alt.
    /// Mouse mode: free cursor (needed for the body menu and the God menu), right-drag turns the camera.
    /// Aim mode: a crosshair in the centre of the screen, the cursor is locked and the mouse turns the camera
    /// directly; skills fire toward the crosshair. Menus that need the mouse (body menu, God menu) and death
    /// switch to the cursor temporarily; Esc or losing focus drops back to mouse mode.
    /// Added to the scene automatically when entering Play mode, so no scene change is needed.
    /// </summary>
    public class AimController : MonoBehaviour
    {
        /// <summary>True while the crosshair is shown and the cursor is locked.</summary>
        public static bool Aiming { get; private set; }

        /// <summary>Set by IMGUI tools (God menu) that need the free cursor while they are open.</summary>
        public static bool MouseUiOpen;

        static bool wantAim;

        SurvivalStats stats;
        OrganInteractor interactor;
        int lockFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            wantAim = false;
            Aiming = false;
            MouseUiOpen = false;
            if (FindAnyObjectByType<AimController>() != null) return;
            var go = new GameObject("Aim Controller (Alt)");
            DontDestroyOnLoad(go);
            go.AddComponent<AimController>();
        }

        void OnDisable() => Release();
        void OnDestroy() => Release();

        static void Release()
        {
            Aiming = false;
            wantAim = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            if (stats == null) FindPlayer();
            if (KeyPressed.Alt()) wantAim = !wantAim;

            bool menuOpen = MouseUiOpen || (interactor != null && interactor.BodyMenuOpen)
                || (stats != null && SurvivalHUD.BlocksInput(stats.gameObject));
            bool dead = stats != null && stats.State.IsDead;
            bool should = wantAim && !menuOpen && !dead;

            if (should != Aiming)
            {
                Aiming = should;
                Cursor.lockState = Aiming ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !Aiming;
                lockFrame = Time.frameCount;
            }

            // The system released the lock (Esc in the Editor, focus lost): go back to mouse mode.
            if (Aiming && Time.frameCount > lockFrame + 3 && Cursor.lockState != CursorLockMode.Locked)
                wantAim = false;
        }

        void FindPlayer()
        {
            var controller = FindAnyObjectByType<SlimeController>();
            if (controller == null) return;
            stats = controller.GetComponent<SurvivalStats>();
            interactor = controller.GetComponent<OrganInteractor>();
        }

        /// <summary>
        /// Direction a skill should fire from <paramref name="origin"/>. In aim mode: toward whatever the crosshair
        /// points at (the first thing the camera ray hits, ignoring the user's own body), or far ahead if nothing is hit.
        /// In mouse mode there is no crosshair, so it is the way the camera faces along the ground.
        /// </summary>
        public static Vector3 AimDirection(GameObject user, Vector3 origin, out Vector3 aimPoint)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Vector3 fwd = user.transform.forward;
                aimPoint = origin + fwd * 20f;
                return fwd;
            }

            if (!Aiming)
            {
                Vector3 flat = cam.transform.forward;
                flat.y = 0f;
                flat = flat.sqrMagnitude > 0.001f ? flat.normalized : user.transform.forward;
                aimPoint = origin + flat * 20f;
                return flat;
            }

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            aimPoint = ray.GetPoint(60f);
            float best = float.MaxValue;
            foreach (var h in Physics.RaycastAll(ray, 60f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (h.transform.IsChildOf(user.transform)) continue;
                if (h.distance < best) { best = h.distance; aimPoint = h.point; }
            }

            Vector3 d = aimPoint - origin;
            if (d.sqrMagnitude < 0.25f) d = cam.transform.forward; // target right on top of us
            return d.normalized;
        }
    }
}
