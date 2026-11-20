using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>A flat circle where hunters leave players alone (the start cavern's spawn area, so a new slime can learn the controls in peace).</summary>
    public class SafeZone : MonoBehaviour
    {
        public float Radius = 25f;
        static readonly List<SafeZone> all = new List<SafeZone>();

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => all.Clear();

        public static bool Contains(Vector3 point)
        {
            for (int i = 0; i < all.Count; i++)
            {
                var z = all[i];
                if (z != null && CaveTier.InZone(point.x, point.z, z.transform.position.x, z.transform.position.z, z.Radius)) return true;
            }
            return false;
        }
    }
}
