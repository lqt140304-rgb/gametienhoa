using Evolve.Organs;
using UnityEngine;

namespace Evolve.Visuals
{
    /// <summary>Where each organ slot sits on the gel body (in the body's local space). One place for all builders.</summary>
    public static class SlimeSockets
    {
        public static Vector3 Position(OrganSlot slot)
        {
            switch (slot)
            {
                case OrganSlot.Eyes: return new Vector3(0f, 0.08f, 0.2f);
                case OrganSlot.Legs: return new Vector3(0f, -0.38f, 0f);
                case OrganSlot.Back: return new Vector3(0f, 0.17f, -0.1f);
                default: return Vector3.zero; // Skin
            }
        }
    }
}
