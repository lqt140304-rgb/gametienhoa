using Evolve.Abilities;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// A stone spike that rises out of the ground (Golem's Giáng Thạch): a four-sided stone pyramid that grows to its height in 0.3 s. The golem later pulls it out
    /// (<see cref="Grab"/>: it becomes the rock in its hand, small, pointing forward) and throws it; spikes not used sink back into the ground (<see cref="Sink"/>).
    /// </summary>
    public class RockSpike : MonoBehaviour
    {
        static Mesh mesh;
        static Material mat;
        float width, height, grow, sinkT = -1f, liftT = -1f, liftSeconds = 1f;
        Vector3 liftFrom;
        bool held;

        static Mesh Pyramid()
        {
            if (mesh != null) return mesh;
            Vector3 a = new Vector3(-0.5f, 0f, -0.5f), b = new Vector3(0.5f, 0f, -0.5f), c = new Vector3(0.5f, 0f, 0.5f), d = new Vector3(-0.5f, 0f, 0.5f), tip = new Vector3(0f, 1f, 0f);
            var v = new[] { a, b, tip, b, c, tip, c, d, tip, d, a, tip, a, d, c, a, c, b };   // four flat sides and a base
            var t = new int[v.Length]; for (int i = 0; i < t.Length; i++) t[i] = i;
            mesh = new Mesh { name = "RockSpike", vertices = v, triangles = t };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        public static RockSpike Spawn(Vector3 pos, float width, float height)
        {
            var go = new GameObject("Gai Đá");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0f, 360f), Random.Range(-8f, 8f));   // a little crooked, like rock
            go.AddComponent<MeshFilter>().sharedMesh = Pyramid();
            if (mat == null) mat = SkillFx.Gel(new Color(0.42f, 0.38f, 0.34f), 1f, 0.15f, 0.9f);
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
            var s = go.AddComponent<RockSpike>();
            s.width = width; s.height = height;
            go.transform.localScale = new Vector3(width, 0.01f, width);
            return s;
        }

        /// <summary>The golem pulls the spike out and holds it in <paramref name="hand"/>.</summary>
        public void Grab(Transform hand)
        {
            held = true;
            transform.SetParent(hand, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // the tip points out of the fist
            float k = 1f / Mathf.Max(0.0001f, hand.lossyScale.y);
            transform.localScale = new Vector3(0.5f * width, 0.55f * height, 0.5f * width) * k;
        }

        /// <summary>The golem takes hold and draws the column slowly out of the ground (it rises by most of its length over <paramref name="seconds"/>).</summary>
        public void BeginLift(float seconds) { liftT = 0f; liftSeconds = Mathf.Max(0.1f, seconds); liftFrom = transform.position; }

        public void Sink() { if (sinkT < 0f) sinkT = 0f; }

        void Update()
        {
            if (held) return;
            if (liftT >= 0f)
            {
                liftT = Mathf.Min(1f, liftT + Time.deltaTime / liftSeconds);
                transform.position = liftFrom + Vector3.up * (0.85f * height * Mathf.SmoothStep(0f, 1f, liftT));
                return;
            }
            if (sinkT >= 0f)
            {
                sinkT += Time.deltaTime / 0.5f;
                transform.localScale = new Vector3(width, height * Mathf.Max(0f, 1f - sinkT), width);
                if (sinkT >= 1f) Destroy(gameObject);
                return;
            }
            grow = Mathf.MoveTowards(grow, 1f, Time.deltaTime / 0.3f);
            transform.localScale = new Vector3(width, Mathf.Max(0.01f, height * Mathf.SmoothStep(0f, 1f, grow)), width);
        }
    }
}
