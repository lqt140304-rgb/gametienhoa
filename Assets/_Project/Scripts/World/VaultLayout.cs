using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>One great root of the vault: a polyline hugging the roof (x, roof-hung y, z), thick at its start and thinning to its end.</summary>
    public struct RibPath
    {
        public Vector3[] Points;
        public float RadiusStart, RadiusEnd;
        /// <summary>0 = main rib, 1 = branch, 2 = twig.</summary>
        public int Level;
    }

    /// <summary>
    /// Pure layout of the root vault (design 2026-11-18): the great ribs fan out from the Great Hall (the heart of the one world tree),
    /// fork into branches and twigs, a few more start from the lake, the vault itself is a roof of many bays (not flat) with holes and slits
    /// where the light falls. No scene objects: the Editor tool and the tests read the same numbers.
    /// </summary>
    public static class VaultLayout
    {
        public const float Rim = 243f;          // the ribs end here (the boundary walls stand at 249)
        public const float MinHeight = 14f;     // lowest the vault comes (slimes and the city stay clear)
        public const float BelowDome = 3f;      // the vault hangs this far under the Dome
        public const float Step = 8f;           // metres between the points of a rib
        public const float TwigMinDistance = 7f;

        public static Vector2 Hub => ZoneLayout.RootFoot.Center;

        // ------------------------------------------------------------------ the roof

        /// <summary>Roof height (m above the lake level) at (x, z): bays high over the lake and the spore garden, low over the root cave and the mulch bank, alcoves and drops.</summary>
        public static float VaultHeight(float x, float z, float ceilingY)
        {
            float top = ceilingY - BelowDome;
            float h = top - 7f;
            h += Bay(x, z, ZoneLayout.LakeCenter, 130f, 6f);                 // high bell over the lake
            h += Bay(x, z, ZoneLayout.Spore.Center, 65f, 5f);                // 04
            h += Bay(x, z, ZoneLayout.RootFoot.Center, 70f, -3f);            // 02 (the hall has its own dome underneath)
            h += Bay(x, z, ZoneLayout.Humus.Center, 60f, -4f);               // 03
            foreach (var a in Alcoves) h += Bay(x, z, a, 26f, 6f);           // deep alcoves
            foreach (var d in Drops) h += Bay(x, z, d, 16f, -4f);            // ridges hanging down
            h += 1.6f * (Noise(x / 45f, z / 45f) - 0.5f) * 2f;               // gentle undulation
            // over the Great Hall the vault stays well above the hall's own dome (floor level + dome height + 4 m)
            float hallMin = ZoneLayout.ZoneLevels[1] + ZoneLayout.CaveHeight + 4f;
            float hx = (x - ZoneLayout.RootFoot.Center.x) / ZoneLayout.CaveRadii.x, hz = (z - ZoneLayout.RootFoot.Center.y) / ZoneLayout.CaveRadii.y;
            float hd = Mathf.Sqrt(hx * hx + hz * hz);
            float hw = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((hd - 1f) / 0.9f));   // 1 over the hall, 0 beyond ~1.9 hall radii
            h = Mathf.Lerp(h, Mathf.Max(h, hallMin), hw);
            return Mathf.Clamp(h, MinHeight, Mathf.Max(MinHeight, top));
        }

        static readonly Vector2[] Alcoves = { new Vector2(-70f, 60f), new Vector2(110f, -90f), new Vector2(30f, 150f) };
        static readonly Vector2[] Drops = { new Vector2(60f, 20f), new Vector2(-40f, -110f), new Vector2(120f, 60f), new Vector2(-110f, 90f) };

        /// <summary>A smooth bump: <paramref name="amount"/> at the centre, 0 beyond the radius.</summary>
        static float Bay(float x, float z, Vector2 centre, float radius, float amount)
        {
            float d = Vector2.Distance(new Vector2(x, z), centre) / radius;
            if (d >= 1f) return 0f;
            float t = 1f - d;
            return amount * t * t * (3f - 2f * t);
        }

        static float Noise(float x, float z) => Mathf.PerlinNoise(x + 500f, z + 500f);

        // ------------------------------------------------------------------ the ribs

        /// <summary>Main ribs from the Great Hall, branches and twigs, and three ribs from the lake. Deterministic for a seed.</summary>
        public static RibPath[] Ribs(int seed, Func<float, float, float> ceilingY)
        {
            var rng = new System.Random(seed);
            var all = new List<RibPath>();
            var pointsXZ = new List<(Vector2 p, int rib)>();   // every rib point so far (with its rib), to keep ribs apart

            int mains = 10;
            float phase = (float)rng.NextDouble() * Mathf.PI * 2f;
            for (int i = 0; i < mains; i++)
            {
                float ang = phase + i * Mathf.PI * 2f / mains + ((float)rng.NextDouble() - 0.5f) * 0.18f;
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                Vector2 start = Hub + dir * 5f;
                while (ZoneLayout.InsideCave(start.x, start.y, 3f)) start += dir;   // begin just outside the hall
                AddRib(all, pointsXZ, rng, ceilingY, start, ang, 0, 3.4f, 1.5f, 600f, -1, Vector2.zero);
            }
            float lakePhase = (float)rng.NextDouble() * Mathf.PI * 2f;
            for (int i = 0; i < 3; i++)   // a few more from the lake keep the roof balanced over the east (spread evenly, never on top of each other)
            {
                float ang = lakePhase + i * Mathf.PI * 2f / 3f + ((float)rng.NextDouble() - 0.5f) * 0.2f;
                AddRib(all, pointsXZ, rng, ceilingY, ZoneLayout.LakeCenter + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 12f, ang, 0, 2.4f, 1.1f, 600f, -1, Vector2.zero);
            }
            int mainCount = all.Count;
            for (int m = 0; m < mainCount; m++)   // branches
            {
                var rib = all[m];
                if (rib.Points.Length < 7) continue;   // a rib cut short (it met the hall or another rib) is too short to fork
                for (int b = 0; b < 3; b++)
                {
                    int idx = Mathf.Clamp((int)(rib.Points.Length * (0.25f + 0.25f * b)) + 1, 2, rib.Points.Length - 3);
                    var at = rib.Points[idx];
                    float side = (b % 2 == 0 ? 1f : -1f) * Mathf.Deg2Rad * (35f + 15f * (float)rng.NextDouble());
                    float baseAng = Mathf.Atan2(rib.Points[idx + 1].z - rib.Points[idx - 1].z, rib.Points[idx + 1].x - rib.Points[idx - 1].x);
                    AddRib(all, pointsXZ, rng, ceilingY, new Vector2(at.x, at.z), baseAng + side, 1, 1.3f, 0.5f, 100f, m, new Vector2(at.x, at.z));
                }
            }
            int branchEnd = all.Count;
            for (int m = mainCount; m < branchEnd; m++)   // twigs
            {
                var rib = all[m];
                if (rib.Points.Length < 5) continue;
                int idx = rib.Points.Length / 2;
                var at = rib.Points[idx];
                float side = (rng.NextDouble() < 0.5 ? 1f : -1f) * Mathf.Deg2Rad * (28f + 12f * (float)rng.NextDouble());
                float baseAng = Mathf.Atan2(rib.Points[idx + 1].z - rib.Points[idx - 1].z, rib.Points[idx + 1].x - rib.Points[idx - 1].x);
                AddRib(all, pointsXZ, rng, ceilingY, new Vector2(at.x, at.z), baseAng + side, 2, 0.5f, 0.25f, 55f, m, new Vector2(at.x, at.z));
            }
            return all.ToArray();
        }

        static void AddRib(List<RibPath> all, List<(Vector2 p, int rib)> others, System.Random rng, Func<float, float, float> ceilingY,
                           Vector2 start, float angle, int level, float r0, float r1, float maxLength, int parent, Vector2 fork)
        {
            var pts = new List<Vector3>();
            var mine = new List<(Vector2 p, int rib)>();
            int myId = all.Count;
            Vector2 p = start;
            float ang = angle;
            float wander = (float)rng.NextDouble() * 6f;
            float length = 0f;
            while (length < maxLength)
            {
                float radius = Mathf.Lerp(r0, r1, Mathf.Clamp01(length / Mathf.Max(1f, Mathf.Min(maxLength, 420f))));
                float y = VaultHeight(p.x, p.y, ceilingY(p.x, p.y)) - radius * 0.9f;
                pts.Add(new Vector3(p.x, y, p.y));
                mine.Add((p, myId));
                if (Mathf.Abs(p.x) >= Rim || Mathf.Abs(p.y) >= Rim) break;
                if (pts.Count > 1 && ZoneLayout.InsideCave(p.x, p.y, 4f)) { pts.RemoveAt(pts.Count - 1); mine.RemoveAt(mine.Count - 1); break; }   // never into the Great Hall
                if (length > 0f && TooClose(p, others, parent, fork)) break;   // never cross another rib (it stops where it meets one)
                ang += 0.012f * Mathf.Sin(length * 0.03f + wander);
                p += new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Step;
                p = new Vector2(Mathf.Clamp(p.x, -Rim, Rim), Mathf.Clamp(p.y, -Rim, Rim));
                length += Step;
            }
            if (pts.Count < 2) return;
            all.Add(new RibPath { Points = pts.ToArray(), RadiusStart = r0, RadiusEnd = Mathf.Lerp(r0, r1, Mathf.Clamp01(length / Mathf.Max(1f, Mathf.Min(maxLength, 420f)))), Level = level });
            others.AddRange(mine);
        }

        static bool TooClose(Vector2 p, List<(Vector2 p, int rib)> others, int parent, Vector2 fork)
        {
            for (int i = 0; i < others.Count; i++)
            {
                if (others[i].rib == parent && parent >= 0 && Vector2.Distance(others[i].p, fork) < 14f) continue;   // only its own parent, near the fork
                if (Vector2.Distance(p, others[i].p) < TwigMinDistance) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ light

        /// <summary>About 40 narrow slits in the roof between the ribs (never over the hall, never near a rib).</summary>
        public static Vector2[] Slots(int seed, RibPath[] ribs)
        {
            var rng = new System.Random(seed + 77);
            var result = new List<Vector2>();
            var ribPoints = new List<Vector2>();
            foreach (var r in ribs) foreach (var q in r.Points) ribPoints.Add(new Vector2(q.x, q.z));
            for (int attempt = 0; attempt < 4000 && result.Count < 40; attempt++)
            {
                var c = new Vector2(((float)rng.NextDouble() - 0.5f) * 2f * 225f, ((float)rng.NextDouble() - 0.5f) * 2f * 225f);
                if (ZoneLayout.InsideCave(c.x, c.y, 4f)) continue;
                bool ok = true;
                foreach (var q in ribPoints) if (Vector2.Distance(c, q) < 5f) { ok = false; break; }
                if (ok) foreach (var o in result) if (Vector2.Distance(c, o) < 12f) { ok = false; break; }
                if (ok) result.Add(c);
            }
            return result.ToArray();
        }

        /// <summary>Half width of slit i, 0.5 to 2 m.</summary>
        public static float SlotHalfWidth(int i) => 0.5f + 1.5f * Mathf.Abs(Mathf.Sin(i * 12.9898f));

        // ------------------------------------------------------------------ the rim

        /// <summary>Where the main ribs reach the rim: the feet of the great roots that climb down the cliff.</summary>
        public static Vector2[] FootPoints(RibPath[] ribs)
        {
            var feet = new List<Vector2>();
            foreach (var r in ribs)
            {
                if (r.Level != 0) continue;
                var e = r.Points[r.Points.Length - 1];
                if (Mathf.Abs(e.x) >= Rim - 8f || Mathf.Abs(e.z) >= Rim - 8f)
                    if (Vector2.Distance(new Vector2(e.x, e.z), ZoneLayout.NorthEntry) > 24f) feet.Add(new Vector2(e.x, e.z));   // the hunter's way in stays open
            }
            return feet.ToArray();
        }
    }
}
