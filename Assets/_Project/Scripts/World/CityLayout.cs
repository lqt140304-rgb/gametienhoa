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

        /// <summary>A treefolk house: a round root hut with its door toward the hall. <see cref="Shelter"/> huts also shelter a slime.</summary>
        public struct House
        {
            public Vector2 Position;
            public float Yaw;        // degrees about y: turns the hut's local +x (its door) toward the hall
            public float Radius;     // inside radius of the dome, metres
            public bool Shelter;
        }

        public const int MaxHouses = 14;
        public const int TerrainSeed = 1337;

        /// <summary>
        /// The huts standing round the hall, outside it, off the trails and the tunnel, on dry level ground, never on the
        /// road side (east), at least 7.5 m apart. Deterministic. Three of them (spread round the hall) are slime shelters.
        /// </summary>
        public static House[] Houses()
        {
            var list = new System.Collections.Generic.List<House>();
            Vector2 c = HallCenter;
            for (int a = 0; a < 360 && list.Count < MaxHouses; a += 4)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(a, 0f)) < 30f) continue;   // keep the road to the door open
                float ang = a * Mathf.Deg2Rad;
                float k = 9f + 7f * ZoneLayout.Noise(a * 0.13f + 400f, 5f);
                var p = c + new Vector2(Mathf.Cos(ang) * (CaveRadiusX + k), Mathf.Sin(ang) * (CaveRadiusZ + k));
                if (InsideHall(p, -4f)) continue;
                if (ZoneLayout.DepthAt(p.x, p.y) > 0f) continue;
                if (ZoneLayout.DistanceToPath(p.x, p.y) < 5f) continue;
                if (ZoneLayout.DistanceToLine(p, ZoneLayout.TunnelLine) < 6f) continue;
                if (Mathf.Abs(TerrainShape.HeightAt(p.x, p.y, TerrainSeed)) > 2.2f || Mathf.Abs(p.x) > 190f || Mathf.Abs(p.y) > 190f) continue;
                bool crowded = false;
                foreach (var h in list) if (Vector2.Distance(h.Position, p) < 7.5f) { crowded = true; break; }
                if (crowded) continue;
                Vector2 to = c - p;
                list.Add(new House
                {
                    Position = p,
                    Yaw = -Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg,
                    Radius = 1.9f + 0.5f * ZoneLayout.Noise(a * 0.37f + 90f, 3f),
                });
            }
            var houses = list.ToArray();
            if (houses.Length >= 3)
                foreach (int i in new[] { 0, houses.Length / 2, houses.Length - 1 }) houses[i].Shelter = true;
            return houses;
        }

        /// <summary>The spot just outside a house's door (for the people who live there).</summary>
        public static Vector2 DoorStep(House h)
        {
            float yaw = -h.Yaw * Mathf.Deg2Rad;
            return h.Position + new Vector2(Mathf.Cos(yaw), Mathf.Sin(yaw)) * (h.Radius + 0.8f);
        }
    }
}
