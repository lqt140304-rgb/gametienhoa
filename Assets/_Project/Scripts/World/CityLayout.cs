using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Where everything of the treefolk's city stands (spec 2026-11-18-treefolk-city-design), in world metres, from the
    /// Great Hall outward. Pure data and maths, so the building tool and the tests share the same numbers.
    /// The hall is the cave of ZoneLayout (CaveRadii x CaveHeight) centred on the Root Foot area; local x points east
    /// (the door, toward the lake) and local z north (the slit).
    /// </summary>
    public static class CityLayout
    {
        public static Vector2 HallCenter => ZoneLayout.RootFoot.Center;

        /// <summary>Local offsets of the eight pillars: two rows of four, 12 m apart, either side of the central aisle.</summary>
        static readonly Vector2[] PillarLocal =
        {
            new Vector2(-18f, -9f), new Vector2(-6f, -9f), new Vector2(6f, -9f), new Vector2(18f, -9f),
            new Vector2(-18f, 9f), new Vector2(-6f, 9f), new Vector2(6f, 9f), new Vector2(18f, 9f),
        };
        public const float PillarRadius = 1.4f;

        public static Vector2[] Pillars
        {
            get
            {
                var p = new Vector2[PillarLocal.Length];
                for (int i = 0; i < p.Length; i++) p[i] = HallCenter + PillarLocal[i];
                return p;
            }
        }

        /// <summary>The carved dais in the middle of the hall: three steps, each 0.25 m high.</summary>
        public static Vector2 DaisCenter => HallCenter;
        public const float DaisRadius = 4f, StepHeight = 0.25f;
        public const int DaisSteps = 3;
        public static float DaisTop => DaisSteps * StepHeight;

        /// <summary>The chieftain's throne: on the dais, toward its west side, facing the door.</summary>
        public static Vector2 ThroneAt => HallCenter + new Vector2(-1.4f, 0f);

        /// <summary>The amber furnace at the far (west) end of the hall.</summary>
        public static Vector2 FurnaceAt => HallCenter + new Vector2(-CaveRadiusX + 6f, 0f);

        /// <summary>The wide door on the east wall and the slit in the north wall.</summary>
        public static Vector2 DoorAt => HallCenter + new Vector2(CaveRadiusX, 0f);
        public static Vector2 SlitAt => HallCenter + new Vector2(0f, CaveRadiusZ);

        static float CaveRadiusX => ZoneLayout.CaveRadii.x;
        static float CaveRadiusZ => ZoneLayout.CaveRadii.y;

        /// <summary>True inside the hall's floor plan, keeping <paramref name="margin"/> metres from the wall.</summary>
        public static bool InsideHall(Vector2 p, float margin) => ZoneLayout.InsideCave(p.x, p.y, -margin);

        /// <summary>Height of the hall's roof (m above the floor) over a point of its floor, or 0 outside it.</summary>
        public static float RoofAbove(Vector2 p, float sink)
        {
            float nx = (p.x - HallCenter.x) / CaveRadiusX, nz = (p.y - HallCenter.y) / CaveRadiusZ;
            float q = 1f - nx * nx - nz * nz;
            if (q <= 0f) return 0f;
            return (ZoneLayout.CaveHeight + sink) * Mathf.Sqrt(q) - sink;
        }
    }
}
