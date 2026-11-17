using Evolve.Organs;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Player
{
    /// <summary>
    /// Aiming and view mode.
    /// Mouse mode (default): free cursor, right-drag turns the camera; skills fire toward the mouse cursor.
    /// Aim mode (Alt): crosshair in the middle of the screen, cursor locked, mouse turns the camera directly,
    /// camera slides over the shoulder; skills fire toward the crosshair.
    /// First-person view (F5): camera at the character's eyes, always with the crosshair.
    /// Menus that need the mouse (organ panel, God menu, HUD menus) and death free the cursor temporarily;
    /// Esc or losing focus drops back to mouse mode.
    /// Added to the scene automatically when entering Play mode, so no scene change is needed.
    /// </summary>
    public class AimController : MonoBehaviour
    {
        /// <summary>True while the crosshair is shown and the cursor is locked (aim mode or first person).</summary>
        public static bool Aiming { get; private set; }

        /// <summary>True while the camera is at the character's eyes (F5).</summary>
        public static bool FirstPerson { get; private set; }

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
            FirstPerson = false;
            MouseUiOpen = false;
            if (FindAnyObjectByType<AimController>() != null) return;
            var go = new GameObject("Aim Controller (Alt, F5)");
            DontDestroyOnLoad(go);
            go.AddComponent<AimController>();
        }

        void OnDisable() => Release();
        void OnDestroy() => Release();

        static void Release()
        {
            Aiming = false;
            FirstPerson = false;
            wantAim = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            if (stats == null) FindPlayer();
            if (KeyPressed.Alt()) wantAim = !wantAim;
            if (KeyPressed.F5()) FirstPerson = !FirstPerson;

            bool menuOpen = MouseUiOpen || Evolve.Visuals.SlimeColorMenu.IsOpen || Evolve.DevTools.GhostMode.PanelOpen || (interactor != null && interactor.BodyMenuOpen)
                || (stats != null && SurvivalHUD.BlocksInput(stats.gameObject));
            bool dead = stats != null && stats.State.IsDead;
            bool should = (wantAim || FirstPerson) && !menuOpen && !dead;

            if (should != Aiming)
            {
                Aiming = should;
                Cursor.lockState = Aiming ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !Aiming;
                lockFrame = Time.frameCount;
            }

            // The system released the lock (Esc in the Editor, focus lost): go back to mouse mode, third person.
            if (Aiming && Time.frameCount > lockFrame + 3 && Cursor.lockState != CursorLockMode.Locked)
            {
                wantAim = false;
                FirstPerson = false;
            }
        }

        void FindPlayer()
        {
            var controller = FindAnyObjectByType<SlimeController>();
            if (controller == null) return;
            stats = controller.GetComponent<SurvivalStats>();
            interactor = controller.GetComponent<OrganInteractor>();
        }

        /// <summary>
        /// Direction a skill should fire from <paramref name="origin"/>.
        /// Aim mode and first person: toward what the crosshair points at. Mouse mode: toward what the cursor points at.
        /// The target is the first thing the camera ray hits (ignoring the user's own body); if that is the ground or
        /// nothing, the shot goes level along that bearing so it can hit anything standing along the line.
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

            Ray ray = Aiming
                ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : cam.ScreenPointToRay(KeyPressed.MousePosition());

            aimPoint = ray.GetPoint(60f);
            bool levelShot = true; // no hit or ground: shoot level
            float best = float.MaxValue;
            foreach (var h in Physics.RaycastAll(ray, 80f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (h.transform.IsChildOf(user.transform)) continue;
                if (h.distance < best)
                {
                    best = h.distance;
                    aimPoint = h.point;
                    levelShot = h.normal.y > 0.7f; // flat ground
                }
            }
            if (best == float.MaxValue)
            {
                // Nothing hit (for example the sand has no collider): use the level plane through the origin.
                var plane = new Plane(Vector3.up, new Vector3(0f, origin.y, 0f));
                if (plane.Raycast(ray, out float enter) && enter > 0f && enter < 80f) aimPoint = ray.GetPoint(enter);
            }

            Vector3 d = aimPoint - origin;
            if (levelShot) d.y = 0f;
            if (d.sqrMagnitude < 0.04f)
            {
                d = cam.transform.forward; // target right on top of us
                if (levelShot) d.y = 0f;
            }
            return d.sqrMagnitude < 0.0001f ? user.transform.forward : d.normalized;
        }
    }
}
