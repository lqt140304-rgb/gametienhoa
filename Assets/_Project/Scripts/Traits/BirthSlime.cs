using UnityEngine;

namespace Evolve.Traits
{
    /// <summary>
    /// The primal slime shown at the end of the birth scene, alone on the stage like a magazine cover. It is built from a few soft sprites with a fake
    /// 3D turn: the body is a shaded disc (a sphere looks the same from every side), and the features inside it, the two eyes, the glowing nucleus
    /// and some floating specks, go round it as the "camera" orbits. Each feature is sorted in front of or behind the body by which side of the
    /// sphere it is on, so the turn reads as real. The eyes open from closed. No shader or mesh is needed (only sprites), so nothing can render black.
    /// </summary>
    internal sealed class BirthSlime
    {
        struct Feature { public Vector3 Pos; public SpriteRenderer Sr; public float Size; public Color Color; public bool IsEye; }

        const float SpriteUnits = 0.64f;   // BirthUiKit.Disc is 64 px at 100 px per unit

        GameObject root;
        Transform body;
        SpriteRenderer bodyDisc, shade, hilite;
        readonly System.Collections.Generic.List<Feature> features = new System.Collections.Generic.List<Feature>();
        Transform[] pupils = new Transform[2];
        float radius;

        public static BirthSlime Create(Transform parent, Vector3 localPos, float radius, Color bodyColor)
        {
            var s = new BirthSlime { radius = radius };
            s.root = new GameObject("Birth Slime");
            s.root.transform.SetParent(parent, false);
            s.root.transform.localPosition = localPos;
            s.body = s.root.transform;
            var gel = new Color(bodyColor.r, bodyColor.g, bodyColor.b, 1f);
            s.bodyDisc = s.Disc("Body", Vector3.zero, radius * 2f, Color.Lerp(gel, Color.white, 0.1f), 0);
            s.shade = s.Disc("Shade", new Vector3(0.12f, -0.18f, 0f) * radius, radius * 1.7f, new Color(gel.r * 0.35f, gel.g * 0.4f, gel.b * 0.5f, 0.55f), 1);
            s.hilite = s.Disc("Hilite", new Vector3(-0.38f, 0.42f, 0f) * radius, radius * 0.5f, new Color(1f, 1f, 1f, 0.55f), 40);
            // the nucleus and the specks (object space, unit sphere)
            s.AddFeature(new Vector3(0f, -0.05f, 0.1f), 0.42f, new Color(1f, 0.95f, 0.7f, 0.85f), false);
            for (int i = 0; i < 7; i++)
            {
                float a = i * 2.4f, y = Mathf.Lerp(-0.45f, 0.5f, BirthUiKit.Hash(i * 3 + 1));
                float r = Mathf.Sqrt(Mathf.Max(0f, 0.55f - y * y));
                s.AddFeature(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r) * 0.9f, 0.1f + 0.08f * BirthUiKit.Hash(i * 7), new Color(1f, 1f, 1f, 0.45f), false);
            }
            // eyes on the front of the sphere (z < 0 is toward the camera)
            s.AddFeature(new Vector3(-0.34f, 0.18f, -0.92f).normalized * 0.96f, 0.3f, Color.white, true);
            s.AddFeature(new Vector3(0.34f, 0.18f, -0.92f).normalized * 0.96f, 0.3f, Color.white, true);
            return s;
        }

        SpriteRenderer Disc(string name, Vector3 local, float diameter, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = local;
            go.transform.localScale = Vector3.one * (diameter / SpriteUnits);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BirthUiKit.Disc;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        void AddFeature(Vector3 pos, float size, Color color, bool eye)
        {
            var sr = Disc(eye ? "Eye" : "Feature", Vector3.zero, size * radius * 2f, color, 5);
            if (eye)
            {
                int k = features.FindAll(f => f.IsEye).Count;
                var p = new GameObject("Pupil");
                p.transform.SetParent(sr.transform, false);
                p.transform.localScale = Vector3.one * 0.5f;
                var psr = p.AddComponent<SpriteRenderer>();
                psr.sprite = BirthUiKit.Disc; psr.color = new Color(0.04f, 0.03f, 0.08f, 1f); psr.sortingOrder = 6;
                pupils[Mathf.Min(k, 1)] = p.transform;
            }
            features.Add(new Feature { Pos = pos, Sr = sr, Size = size, Color = color, IsEye = eye });
        }

        /// <param name="angle">How far the view has turned round the slime, in degrees.</param>
        /// <param name="eyeOpen">0 = closed, 1 = wide open.</param>
        public void Tick(float time, float angle, float eyeOpen, float appear)
        {
            if (root == null) return;
            float rad = angle * Mathf.Deg2Rad, c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            float wob = 1f + 0.035f * Mathf.Sin(time * 2.2f);
            float pop = Mathf.Max(0.001f, BirthUiKit.Smooth(appear));
            body.localScale = new Vector3(pop * (2f - wob), pop * wob * 0.94f, pop);
            for (int i = 0; i < features.Count; i++)
            {
                var f = features[i];
                if (f.Sr == null) continue;
                var p = f.Pos;
                float x = p.x * c + p.z * s, z = -p.x * s + p.z * c;      // turn about the vertical axis
                bool front = z < 0f;
                // the eyes belong to the surface: they vanish on the far side; the specks show through the gel on either side, dimmer behind
                bool visible = !f.IsEye || front;
                f.Sr.enabled = visible;
                f.Sr.transform.localPosition = new Vector3(x, p.y, 0f) * radius;
                float depth = Mathf.Clamp01(0.5f - z * 0.5f);              // 1 = nearest
                float size = f.Size * radius * 2f / SpriteUnits * (0.75f + 0.25f * depth);
                if (f.IsEye)
                {
                    float foreshorten = Mathf.Clamp(-z / 0.96f, 0.12f, 1f);
                    f.Sr.transform.localScale = new Vector3(size * foreshorten, size * Mathf.Max(0.04f, eyeOpen), 1f);
                    f.Sr.sortingOrder = 5;
                    int pi = p.x < 0f ? 0 : 1;
                    if (pupils[pi] != null)
                    {
                        pupils[pi].localPosition = new Vector3(Mathf.Sin(time * 0.9f + pi) * 0.12f, -0.04f, 0f);
                        pupils[pi].localScale = Vector3.one * (0.5f * Mathf.Clamp01(eyeOpen * 1.4f));
                    }
                }
                else
                {
                    f.Sr.transform.localScale = Vector3.one * size;
                    f.Sr.sortingOrder = front ? 4 : -1;                     // behind the body (-1) the specks are hidden by the gel
                    f.Sr.color = new Color(f.Color.r, f.Color.g, f.Color.b, f.Color.a * (front ? 1f : 0.0f) * (0.6f + 0.4f * Mathf.Sin(time * 3f + i)));
                }
            }
        }

        public void Destroy() { if (root != null) Object.Destroy(root); root = null; }
    }
}
