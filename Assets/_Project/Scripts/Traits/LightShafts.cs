using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Traits
{
    /// <summary>
    /// Where light falls (Nhân Quang Hợp reads it). A cave declares its shafts with <see cref="Register"/> (a vertical column of light: centre on
    /// the ground, radius, top height) or <see cref="RegisterArea"/> (any test). The sun beam through the roof of Hang Khởi Sinh is declared
    /// here, only while the scene Underroot_Lake is open.
    /// </summary>
    public static class LightShafts
    {
        static readonly List<Func<Vector3, bool>> areas = new List<Func<Vector3, bool>>();

        /// <summary>A vertical shaft of light. Returns a handle for <see cref="Unregister"/>.</summary>
        public static Func<Vector3, bool> Register(Vector3 center, float radius, float topY)
        {
            float r2 = radius * radius;
            Func<Vector3, bool> test = p => p.y <= topY && (new Vector2(p.x - center.x, p.z - center.z)).sqrMagnitude <= r2;
            areas.Add(test);
            return test;
        }

        public static Func<Vector3, bool> RegisterArea(Func<Vector3, bool> test)
        {
            if (test != null) areas.Add(test);
            return test;
        }

        public static void Unregister(Func<Vector3, bool> handle) => areas.Remove(handle);

        public static bool InLight(Vector3 p)
        {
            if (CaveBeam(p)) return true;
            foreach (var a in areas) if (a(p)) return true;
            return false;
        }

        // Hang Khởi Sinh: follow the sunlight back up from the point; lit if it leaves through the hole in the roof.
        static bool CaveBeam(Vector3 p)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Underroot_Lake") return false;
            var d = Evolve.World.CavernLayout.SunDirection;   // points down
            if (d.y > -0.05f) return false;
            float rise = Evolve.World.CavernLayout.CeilingY(p.x, p.z) - p.y;
            if (rise <= 0f) return false;
            var q = p - d * (rise / -d.y);
            return Evolve.World.CavernLayout.InHole(q.x, q.z);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => areas.Clear();
    }
}
