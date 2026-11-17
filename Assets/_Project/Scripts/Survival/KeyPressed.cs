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
#else
        public static bool H() => Input.GetKeyDown(KeyCode.H);
        public static bool V() => Input.GetKeyDown(KeyCode.V);
#endif
    }
}
