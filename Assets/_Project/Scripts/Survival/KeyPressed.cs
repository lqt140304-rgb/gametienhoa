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
        public static bool H() => Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame;
        public static bool V() => Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame;
        public static bool E() => Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        public static bool X() => Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame;
        public static bool G() => Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame;
        public static bool R() => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        public static bool Y() => Keyboard.current != null && Keyboard.current.yKey.wasPressedThisFrame;
        public static bool F1() => Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame;
        /// <summary>Number key 1..9 on the top row.</summary>
        public static bool Digit(int n) => Keyboard.current != null && n >= 1 && n <= 9
            && Keyboard.current[Key.Digit1 + (n - 1)].wasPressedThisFrame;
#else
        public static bool H() => Input.GetKeyDown(KeyCode.H);
        public static bool V() => Input.GetKeyDown(KeyCode.V);
        public static bool E() => Input.GetKeyDown(KeyCode.E);
        public static bool X() => Input.GetKeyDown(KeyCode.X);
        public static bool G() => Input.GetKeyDown(KeyCode.G);
        public static bool R() => Input.GetKeyDown(KeyCode.R);
        public static bool Y() => Input.GetKeyDown(KeyCode.Y);
        public static bool F1() => Input.GetKeyDown(KeyCode.F1);
        public static bool Digit(int n) => n >= 1 && n <= 9 && Input.GetKeyDown(KeyCode.Alpha1 + (n - 1));
#endif
    }
}
