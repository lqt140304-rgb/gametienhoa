using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Evolve.Survival
{
    /// <summary>Tiny helper for debug/UI hotkeys that works with either input backend.</summary>
    public static class KeyPressed
    {
#if ENABLE_INPUT_SYSTEM
        public static bool K() => Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame;
        public static bool Escape() => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        public static bool H() => Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame;
        public static bool V() => Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame;
        public static bool E() => Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        public static bool X() => Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame;
        public static bool G() => Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame;
        public static bool R() => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        public static bool Y() => Keyboard.current != null && Keyboard.current.yKey.wasPressedThisFrame;
        public static bool F1() => Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame;
        public static bool F() => Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
        public static bool C() => Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame;
        public static bool F2() => Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame;
        /// <summary>T is being held (rescue a teammate).</summary>
        public static bool THeld() => Keyboard.current != null && Keyboard.current.tKey.isPressed;
        public static bool Tab() => Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
        public static bool TabHeld() => Keyboard.current != null && Keyboard.current.tabKey.isPressed;
        public static bool F3() => Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame;
        public static bool F4() => Keyboard.current != null && Keyboard.current.f4Key.wasPressedThisFrame;
        public static bool F6() => Keyboard.current != null && Keyboard.current.f6Key.wasPressedThisFrame;
        public static bool F7() => Keyboard.current != null && Keyboard.current.f7Key.wasPressedThisFrame;
        public static bool F5() => Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame;
        public static Vector2 MousePosition() => Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)new Vector2(Screen.width / 2f, Screen.height / 2f);
        public static bool Alt() => Keyboard.current != null
            && (Keyboard.current.leftAltKey.wasPressedThisFrame || Keyboard.current.rightAltKey.wasPressedThisFrame);
        /// <summary>Number key 1..9 on the top row.</summary>
        public static bool Digit(int n) => Keyboard.current != null && n >= 1 && n <= 9
            && Keyboard.current[Key.Digit1 + (n - 1)].wasPressedThisFrame;
#else
        public static bool K() => Input.GetKeyDown(KeyCode.K);
        public static bool Escape() => Input.GetKeyDown(KeyCode.Escape);
        public static bool H() => Input.GetKeyDown(KeyCode.H);
        public static bool V() => Input.GetKeyDown(KeyCode.V);
        public static bool E() => Input.GetKeyDown(KeyCode.E);
        public static bool X() => Input.GetKeyDown(KeyCode.X);
        public static bool G() => Input.GetKeyDown(KeyCode.G);
        public static bool R() => Input.GetKeyDown(KeyCode.R);
        public static bool Y() => Input.GetKeyDown(KeyCode.Y);
        public static bool F1() => Input.GetKeyDown(KeyCode.F1);
        public static bool F() => Input.GetKeyDown(KeyCode.F);
        public static bool C() => Input.GetKeyDown(KeyCode.C);
        public static bool F2() => Input.GetKeyDown(KeyCode.F2);
        public static bool THeld() => Input.GetKey(KeyCode.T);
        public static bool Tab() => Input.GetKeyDown(KeyCode.Tab);
        public static bool TabHeld() => Input.GetKey(KeyCode.Tab);
        public static bool F3() => Input.GetKeyDown(KeyCode.F3);
        public static bool F4() => Input.GetKeyDown(KeyCode.F4);
        public static bool F6() => Input.GetKeyDown(KeyCode.F6);
        public static bool F7() => Input.GetKeyDown(KeyCode.F7);
        public static bool F5() => Input.GetKeyDown(KeyCode.F5);
        public static Vector2 MousePosition() => Input.mousePosition;
        public static bool Alt() => Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt);
        public static bool Digit(int n) => n >= 1 && n <= 9 && Input.GetKeyDown(KeyCode.Alpha1 + (n - 1));
#endif
    }
}
