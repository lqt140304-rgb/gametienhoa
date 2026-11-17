using Evolve.Player;
using Evolve.Survival;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Evolve.DevTools
{
    /// <summary>
    /// For checking the map: fly through everything and widen the view. Added automatically when entering Play mode
    /// (Editor and development builds only).
    ///   F6  ghost mode on / off: the slime flies freely (no gravity, no collisions) wherever the camera looks.
    ///       W A S D move, Space up, Left Ctrl down, Left Shift fast.
    ///   F7  small panel: ghost on/off, fly speed, field of view.
    ///   Page Up / Page Down  widen / narrow the field of view (any time), Home = back to normal.
    /// The slime keeps living (hunger, creatures) while it is a ghost; in co-op the others see it flying.
    /// </summary>
    public class GhostMode : MonoBehaviour
    {
        const int WindowId = 0x6057;
        const float MinFov = 30f, MaxFov = 120f;

        /// <summary>True while the F7 panel is open (it frees the cursor and blocks game input).</summary>
        public static bool PanelOpen { get; private set; }
        /// <summary>True while the slime is a ghost.</summary>
        public static bool Active { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            PanelOpen = false;
            Active = false;
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            if (FindAnyObjectByType<GhostMode>() != null) return;
            var go = new GameObject("Ghost Mode (F6, F7)");
            DontDestroyOnLoad(go);
            go.AddComponent<GhostMode>();
        }

        SlimeController controller;
        Rigidbody body;
        Collider bodyCollider;
        Rect window = new Rect(20, 120, 280, 170);
        float speed = 14f;
        float defaultFov = -1f;

        void Update()
        {
            if (KeyPressed.F6()) SetGhost(!Active);
            if (KeyPressed.F7()) PanelOpen = !PanelOpen;

            HandleFov();
            if (Active) Fly();
        }

        void OnDisable()
        {
            if (Active) SetGhost(false);
            PanelOpen = false;
        }

        // ---------------------------------------------------------------- field of view

        Camera View => Camera.main;

        void RememberDefault()
        {
            if (defaultFov < 0f && View != null) defaultFov = View.fieldOfView;
        }

        void HandleFov()
        {
            var cam = View;
            if (cam == null) return;
            RememberDefault();
            float change = 0f;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.pageUpKey.isPressed) change += 1f;
                if (k.pageDownKey.isPressed) change -= 1f;
                if (k.homeKey.wasPressedThisFrame) cam.fieldOfView = defaultFov;
            }
#else
            if (Input.GetKey(KeyCode.PageUp)) change += 1f;
            if (Input.GetKey(KeyCode.PageDown)) change -= 1f;
            if (Input.GetKeyDown(KeyCode.Home)) cam.fieldOfView = defaultFov;
#endif
            if (change != 0f) cam.fieldOfView = Mathf.Clamp(cam.fieldOfView + change * 35f * Time.unscaledDeltaTime, MinFov, MaxFov);
        }

        // ---------------------------------------------------------------- ghost

        void SetGhost(bool on)
        {
            if (on == Active) return;
            if (on)
            {
                controller = FindAnyObjectByType<SlimeController>();
                if (controller == null) return;
                body = controller.GetComponent<Rigidbody>();
                bodyCollider = controller.GetComponent<Collider>();
                controller.enabled = false;
                if (body != null) { body.linearVelocity = Vector3.zero; body.isKinematic = true; }
                if (bodyCollider != null) bodyCollider.enabled = false;
                Active = true;
            }
            else
            {
                Active = false;
                if (controller != null) controller.enabled = true;
                if (bodyCollider != null) bodyCollider.enabled = true;
                if (body != null) { body.isKinematic = false; body.linearVelocity = Vector3.zero; }
            }
        }

        void Fly()
        {
            if (controller == null) { Active = false; return; }
            var cam = View;
            if (cam == null || SurvivalHUD.BlocksInput(controller.gameObject)) return;

            Vector3 move = Vector3.zero;
            bool fast = false, up = false, down = false;
            float forward = 0f, side = 0f;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return;
            if (k.wKey.isPressed) forward += 1f;
            if (k.sKey.isPressed) forward -= 1f;
            if (k.dKey.isPressed) side += 1f;
            if (k.aKey.isPressed) side -= 1f;
            up = k.spaceKey.isPressed;
            down = k.leftCtrlKey.isPressed;
            fast = k.leftShiftKey.isPressed;
#else
            if (Input.GetKey(KeyCode.W)) forward += 1f;
            if (Input.GetKey(KeyCode.S)) forward -= 1f;
            if (Input.GetKey(KeyCode.D)) side += 1f;
            if (Input.GetKey(KeyCode.A)) side -= 1f;
            up = Input.GetKey(KeyCode.Space);
            down = Input.GetKey(KeyCode.LeftControl);
            fast = Input.GetKey(KeyCode.LeftShift);
#endif
            move += cam.transform.forward * forward + cam.transform.right * side;
            if (up) move += Vector3.up;
            if (down) move += Vector3.down;
            if (move.sqrMagnitude > 1f) move.Normalize();

            float v = speed * (fast ? 3.5f : 1f);
            var t = controller.transform;
            t.position += move * v * Time.unscaledDeltaTime;
        }

        // ---------------------------------------------------------------- UI

        void OnGUI()
        {
            if (Active)
            {
                var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, fontSize = 12, richText = true };
                GUI.Label(new Rect(12, 12, 700, 40), // top-left: the bottom is taken by the HUD
                    "<b>GHOST</b>  WASD bay · Space lên · Ctrl xuống · Shift nhanh · PgUp/PgDn góc nhìn (Home: về lại) · F6 thoát · F7 bảng", style);
            }
            if (PanelOpen) window = GUILayout.Window(WindowId, window, DrawPanel, "Xem map (F7 để đóng)");
        }

        void DrawPanel(int id)
        {
            bool ghost = GUILayout.Toggle(Active, " Chế độ ghost (bay xuyên mọi thứ) · F6");
            if (ghost != Active) SetGhost(ghost);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Tốc độ bay", GUILayout.Width(80));
            speed = GUILayout.HorizontalSlider(speed, 4f, 80f);
            GUILayout.Label($"{speed:0} m/s", GUILayout.Width(55));
            GUILayout.EndHorizontal();

            var cam = View;
            if (cam != null)
            {
                RememberDefault();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Góc nhìn", GUILayout.Width(80));
                cam.fieldOfView = GUILayout.HorizontalSlider(cam.fieldOfView, MinFov, MaxFov);
                GUILayout.Label($"{cam.fieldOfView:0}°", GUILayout.Width(55));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Rộng hơn (+10°)")) cam.fieldOfView = Mathf.Min(MaxFov, cam.fieldOfView + 10f);
                if (GUILayout.Button("Hẹp hơn (-10°)")) cam.fieldOfView = Mathf.Max(MinFov, cam.fieldOfView - 10f);
                if (GUILayout.Button("Mặc định")) cam.fieldOfView = defaultFov;
                GUILayout.EndHorizontal();
            }
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
    }
}
