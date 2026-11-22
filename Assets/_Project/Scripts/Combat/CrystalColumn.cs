using System.Collections.Generic;
using Evolve.Abilities;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// A purple crystal column the scorpion leaves behind: planted where a spike of Phóng Gai Tinh Thể lands (it blocks later spikes and the claw charge, and the player can hide behind it), or risen in front of the scorpion
    /// for the tail to flick in Bùng Pha Lê. It grows out of the ground, stands <see cref="ScorpionRules.ColumnLife"/> seconds and sinks, or shatters at once when the scorpion's claws jam into it or the tail throws it.
    /// Built on <see cref="RockSpike"/> (the same pyramid and growth), tinted and given a collider.
    /// </summary>
    public class CrystalColumn : MonoBehaviour
    {
        static readonly List<CrystalColumn> all = new List<CrystalColumn>();
        static readonly Color Crystal = new Color(0.66f, 0.38f, 0.98f), Shards = new Color(0.82f, 0.62f, 1f);
        static Material mat;
        RockSpike spike;
        Collider box;
        float height, dieAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { all.Clear(); mat = null; }

        /// <summary>The top of the column (where a flicked crystal leaves from).</summary>
        public Vector3 Top => transform.position + Vector3.up * height;

        public static CrystalColumn Spawn(Vector3 pos, float width, float height, float life)
        {
            var rs = RockSpike.Spawn(pos, width, height);
            var go = rs.gameObject;
            go.name = "Cột Tinh Thể";
            if (mat == null) mat = SkillFx.Gel(Crystal, 1f, 0.9f, 2.6f);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            var b = go.AddComponent<BoxCollider>();   // the pyramid's unit box: it scales with the column as it grows
            b.center = new Vector3(0f, 0.5f, 0f); b.size = Vector3.one;
            var c = go.AddComponent<CrystalColumn>();
            c.spike = rs; c.box = b; c.height = height; c.dieAt = Time.time + life;
            all.Add(c);
            SkillFx.Burst(pos + Vector3.up * 0.2f, Shards, 10, 3f, 0.08f, 0.5f, 0.5f);
            return c;
        }

        void Update()
        {
            if (Time.time < dieAt) return;
            if (box != null) box.enabled = false;
            if (spike != null) spike.Sink();
            all.Remove(this);
            enabled = false;
        }

        void OnDestroy() { all.Remove(this); }

        /// <summary>Breaks the column in a burst of shards.</summary>
        public void Shatter()
        {
            SkillFx.Burst(transform.position + Vector3.up * height * 0.5f, Shards, 22, 5f, 0.1f, 0.7f, 0.6f);
            SkillFx.Dust(transform.position, 0.9f, new Color(0.5f, 0.42f, 0.34f), 10);
            all.Remove(this);
            Destroy(gameObject);
        }

        /// <summary>Breaks the columns within <paramref name="radius"/> metres (flat) of <paramref name="point"/>; true if there was one.</summary>
        public static bool ShatterNear(Vector3 point, float radius)
        {
            bool any = false;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                var c = all[i];
                if (c == null) { all.RemoveAt(i); continue; }
                Vector3 d = c.transform.position - point; d.y = 0f;
                if (d.sqrMagnitude > radius * radius) continue;
                c.Shatter(); any = true;
            }
            return any;
        }
    }
}
