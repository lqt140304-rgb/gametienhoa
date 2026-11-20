using System.Collections.Generic;
using Evolve.Creatures;
using Evolve.Survival;
using Evolve.Swallow;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// A patch of crystal spikes (Cắm Gai). The crystals push out of the ground one after another, each creature that steps in is hurt once
    /// (and stunned at ★3), and the patch melts away (dissolves) at the end of its time.
    /// </summary>
    public class SpikeTrap : MonoBehaviour
    {
        const float GrowSeconds = 0.22f, Stagger = 0.04f, MeltSeconds = 0.8f;

        float radius, damage, stun, born, until;
        SurvivalStats owner;
        Color tint;
        Material mat;
        Transform[] crystals;
        Vector3[] fullSize;
        readonly HashSet<Component> hurt = new HashSet<Component>();

        public static SpikeTrap Spawn(Vector3 at, float radius, float damage, float seconds, float stun, SurvivalStats owner, Color tint)
        {
            var go = new GameObject("SpikeTrap");
            go.transform.position = at;
            var trap = go.AddComponent<SpikeTrap>();
            trap.radius = radius; trap.damage = damage; trap.stun = stun; trap.owner = owner; trap.tint = tint;
            trap.born = Time.time; trap.until = Time.time + seconds;
            trap.mat = SkillFx.Gel(tint, 0.3f, 1.4f, 1.8f);

            // a big crystal in the middle and six round it, leaning out, each a different height
            const int n = 7;
            trap.crystals = new Transform[n];
            trap.fullSize = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                var part = new GameObject(i == 0 ? "Crystal Core" : "Crystal") { layer = 2 };
                part.transform.SetParent(go.transform, false);
                part.AddComponent<MeshFilter>().sharedMesh = SkillFxMeshes.Crystal();
                var r = part.AddComponent<MeshRenderer>();
                r.sharedMaterial = trap.mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                if (i == 0) { part.transform.localRotation = Quaternion.identity; trap.fullSize[i] = new Vector3(0.32f, 1.05f, 0.32f); }
                else
                {
                    float a = (i - 1) / 6f * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                    var radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    part.transform.localPosition = radial * radius * Random.Range(0.45f, 0.7f);
                    part.transform.localRotation = Quaternion.FromToRotation(Vector3.up, Vector3.up + radial * Random.Range(0.15f, 0.4f));
                    float h = Random.Range(0.45f, 0.8f);
                    trap.fullSize[i] = new Vector3(0.22f, h, 0.22f);
                }
                trap.crystals[i] = part.transform;
                part.transform.localScale = Vector3.zero;
            }
            SkillFx.Burst(at + Vector3.up * 0.1f, tint, 14, 1.8f, 0.14f, 0.7f, 0.5f, radius * 0.5f);   // rock chips as they push out
            SkillFx.Ring(at, radius * 1.4f, tint, 0.4f, 0.2f);
            return trap;
        }

        void Update()
        {
            float now = Time.time;
            if (now >= until) { Destroy(gameObject); return; }

            // grow in one after another (with a little overshoot), melt away at the end
            float melt = Mathf.Clamp01((until - now) / MeltSeconds);
            for (int i = 0; i < crystals.Length; i++)
            {
                float g = SkillFxMath.EaseOutBack((now - born - i * Stagger) / GrowSeconds);
                crystals[i].localScale = fullSize[i] * g * Mathf.Lerp(0.6f, 1f, melt);
            }
            mat.SetFloat("_Fade", melt);

            Vector3 c = transform.position;
            foreach (var p in Predator.Active) Check(p, c);
            foreach (var p in Prey.Active) Check(p, c);
        }

        void OnDestroy() { if (mat != null) Destroy(mat); }

        void Check(Component creature, Vector3 c)
        {
            if (creature == null || hurt.Contains(creature)) return;
            Vector3 d = creature.transform.position - c; d.y = 0f;
            if (d.sqrMagnitude > radius * radius) return;
            hurt.Add(creature);
            SkillFx.Burst(creature.transform.position + Vector3.up * 0.2f, tint, 10, 2.5f, 0.12f, 0.5f, 0.6f, 0.2f);   // the spikes bite
            if (stun > 0f) CreatureStatus.Stun(creature, stun);
            if (creature is IDamageable target) target.TakeDamage(damage, owner);
        }
    }
}
