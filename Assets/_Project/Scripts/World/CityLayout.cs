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

        // ---------------------------------------------------------------- the village between the huts

        /// <summary>Offset (m) beyond the hall's wall of the paved ring that circles it.</summary>
        public const float RingOffset = 3.5f;

        /// <summary>A point on the paved ring round the hall at <paramref name="degrees"/> (0 = east, counter-clockwise).</summary>
        public static Vector2 RingPoint(float degrees)
        {
            float a = degrees * Mathf.Deg2Rad;
            return HallCenter + new Vector2(Mathf.Cos(a) * (CaveRadiusX + RingOffset), Mathf.Sin(a) * (CaveRadiusZ + RingOffset));
        }

        /// <summary>Angle (degrees, -180..180) on the ring of the point nearest to <paramref name="p"/>.</summary>
        public static float RingAngle(Vector2 p)
        {
            Vector2 d = p - HallCenter;
            return Mathf.Atan2(d.y / CaveRadiusZ, d.x / CaveRadiusX) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// The flagstone path from every house's doorstep in to the ring and round it to the door of the hall (east):
        /// one polyline per house, about one point per 1.5 m.
        /// </summary>
        public static System.Collections.Generic.List<Vector2[]> HousePaths()
        {
            var paths = new System.Collections.Generic.List<Vector2[]>();
            foreach (var h in Houses())
            {
                var pts = new System.Collections.Generic.List<Vector2> { DoorStep(h) };
                float from = RingAngle(h.Position);
                pts.Add(RingPoint(from));
                float step = from > 0f ? -3f : 3f;           // the short way round to the east
                for (float a = from + step; from > 0f ? a > 0f : a < 0f; a += step) pts.Add(RingPoint(a));
                pts.Add(RingPoint(0f));
                pts.Add(DoorAt + new Vector2(3f, 0f));
                paths.Add(pts.ToArray());
            }
            return paths;
        }

        /// <summary>Carved statues standing on the ring between the huts (yaw degrees faces them toward the hall).</summary>
        public static System.Collections.Generic.List<(Vector2 position, float yaw)> Statues()
        {
            var list = new System.Collections.Generic.List<(Vector2, float)>();
            var houses = Houses();
            for (float a = -150f; a <= 150f; a += 15f)
            {
                if (Mathf.Abs(a) < 25f) continue;
                Vector2 p = RingPoint(a) + (RingPoint(a) - HallCenter).normalized * 2.2f;   // just outside the paving
                bool clear = ZoneLayout.DepthAt(p.x, p.y) <= 0f && ZoneLayout.DistanceToPath(p.x, p.y) >= 3f;
                foreach (var h in houses) if (Vector2.Distance(p, h.Position) < h.Radius + 2.5f) clear = false;
                foreach (var (q, _) in list) if (Vector2.Distance(p, q) < 12f) clear = false;
                if (!clear) continue;
                Vector2 to = HallCenter - p;
                list.Add((p, -Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg));
            }
            return list;
        }

        // ---------------------------------------------------------------- the class houses

        /// <summary>The house of one of the eight treefolk classes (see <see cref="Evolve.Civilization.TreefolkClass"/>).</summary>
        public struct ClassHouse
        {
            public Evolve.Civilization.TreefolkClass Class;
            public Vector2 Position;
            public float Yaw;        // degrees about y turning the building's local +x (its door, its front) toward the hall
        }

        public const float ClassHouseSpacing = 14f;

        /// <summary>
        /// Where each class has its house. Not a ring and not evenly spaced: every class looks for the site that suits it
        /// (Dưỡng Thể by the water, Xạ Thể and Năng Thể on high ground, Đấu Thể in front of the hall's door, Quần Thể on open
        /// ground to the south, Cấu Thể to the north, Liệp Thể hidden away from the paths, Dã Thể on the far edge), among
        /// dry, level, off-trail sites outside the village, at least <see cref="ClassHouseSpacing"/> apart. A little seeded
        /// noise breaks ties, so the layout never looks mechanical. Deterministic.
        /// </summary>
        public static ClassHouse[] ClassHouses()
        {
            Vector2 c = HallCenter;
            var houses = Houses();
            var sites = new System.Collections.Generic.List<Vector2>();
            for (int x = -196; x <= -90; x += 4)
                for (int z = -90; z <= 62; z += 4)
                {
                    var p = new Vector2(x, z);
                    if (ZoneLayout.InsideCave(p.x, p.y, 9f)) continue;
                    if (ZoneLayout.DepthAt(p.x, p.y) > 0f || ZoneLayout.DistanceToWater(p.x, p.y) < 6f) continue;
                    if (ZoneLayout.DistanceToPath(p.x, p.y) < 5f || ZoneLayout.DistanceToLine(p, ZoneLayout.TunnelLine) < 8f) continue;
                    if (Mathf.Abs(p.x) > 192f || Mathf.Abs(p.y) > 190f) continue;
                    if (ZoneLayout.Influence(1, p.x, p.y) < 0.08f) continue;
                    bool free = true;
                    foreach (var h in houses) if (Vector2.Distance(p, h.Position) < h.Radius + 7f) { free = false; break; }
                    if (!free) continue;
                    float h0 = TerrainShape.HeightAt(p.x + 5f, p.y, TerrainSeed), h1 = TerrainShape.HeightAt(p.x - 5f, p.y, TerrainSeed);
                    float h2 = TerrainShape.HeightAt(p.x, p.y + 5f, TerrainSeed), h3 = TerrainShape.HeightAt(p.x, p.y - 5f, TerrainSeed);
                    if (Mathf.Max(Mathf.Max(h0, h1), Mathf.Max(h2, h3)) - Mathf.Min(Mathf.Min(h0, h1), Mathf.Min(h2, h3)) > 1.6f) continue;
                    sites.Add(p);
                }

            // each class: how much it likes a site (higher is better), in the order the classes pick
            var order = new (Evolve.Civilization.TreefolkClass cls, System.Func<Vector2, float> like)[]
            {
                (Evolve.Civilization.TreefolkClass.DuongThe, p => -ZoneLayout.DistanceToWater(p.x, p.y)),
                (Evolve.Civilization.TreefolkClass.XaThe, p => TerrainShape.HeightAt(p.x, p.y, TerrainSeed) + 0.02f * p.x),
                (Evolve.Civilization.TreefolkClass.NangThe, p => TerrainShape.HeightAt(p.x, p.y, TerrainSeed) - 0.01f * p.y),
                (Evolve.Civilization.TreefolkClass.DauThe, p => -Mathf.Abs(Mathf.Atan2(p.y - c.y, p.x - c.x) * Mathf.Rad2Deg) - 0.3f * Vector2.Distance(p, c)),
                (Evolve.Civilization.TreefolkClass.QuanThe, p => -p.y - 0.1f * Vector2.Distance(p, c)),
                (Evolve.Civilization.TreefolkClass.CauThe, p => p.y + 0.05f * Vector2.Distance(p, c)),
                (Evolve.Civilization.TreefolkClass.LiepThe, p => -p.x - 0.5f * Vector2.Distance(p, c) + 0.3f * ZoneLayout.DistanceToPath(p.x, p.y)),
                (Evolve.Civilization.TreefolkClass.DaThe, p => Vector2.Distance(p, c)),
            };
            var result = new System.Collections.Generic.List<ClassHouse>();
            foreach (var (cls, like) in order)
            {
                bool found = false; float best = float.MinValue; Vector2 at = default;
                foreach (var p in sites)
                {
                    bool near = false;
                    foreach (var r in result) if (Vector2.Distance(r.Position, p) < ClassHouseSpacing) { near = true; break; }
                    if (near) continue;
                    float v = like(p) + ZoneLayout.Noise(p.x * 0.2f + 11f, p.y * 0.2f + 7f) * 3f;
                    if (!found || v > best) { found = true; best = v; at = p; }
                }
                if (!found) continue;
                Vector2 to = c - at;
                result.Add(new ClassHouse { Class = cls, Position = at, Yaw = -Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg });
            }
            return result.ToArray();
        }
    }
}
