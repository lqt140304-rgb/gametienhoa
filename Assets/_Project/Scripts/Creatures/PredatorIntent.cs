using UnityEngine;

namespace Evolve.Creatures
{
    public enum IntentMode { None, Chase, GoTo, Hold, Avoid }

    /// <summary>
    /// What CreatureBrain wants a Predator to do this moment: chase and bite a creature, walk to a point, hold still, or run from something.
    /// None = the old behaviour (patrol, and hunt the players).
    /// </summary>
    public struct PredatorIntent
    {
        public IntentMode Mode;
        public Transform Target;
        public Vector3 Point;
        public float Radius;
        /// <summary>Chase only: killing the target is a meal (false for a fight with a rival).</summary>
        public bool Meal;

        public static PredatorIntent ChaseOf(Transform t, bool meal) => new PredatorIntent { Mode = IntentMode.Chase, Target = t, Meal = meal };
        public static PredatorIntent GoToPoint(Vector3 p, float radius) => new PredatorIntent { Mode = IntentMode.GoTo, Point = p, Radius = radius };
        public static PredatorIntent AvoidOf(Transform t) => new PredatorIntent { Mode = IntentMode.Avoid, Target = t };
        public static PredatorIntent Hold() => new PredatorIntent { Mode = IntentMode.Hold };
    }
}
