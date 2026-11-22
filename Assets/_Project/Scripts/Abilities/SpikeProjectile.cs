using Evolve.Creatures;
using Evolve.Survival;
using Evolve.Swallow;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>A straight projectile. Its render tip can briefly lead from the slime surface while the collision path starts low.</summary>
    public class SpikeProjectile : MonoBehaviour
    {
        const float Radius = 0.12f;
        const float CreatureRadius = 0.4f;

        Vector3 direction;
        float speed, maxDistance, damage, travelled, launchedAt;
        SurvivalStats owner;
        Transform ownerRoot;
        SpikeVolleyFx volley;

        Transform tip, strand, anchorBody;
        TrailRenderer trail;
        Light flightLight;
        Material hardened, softGel;
        Vector3 anchorLocal, launchOffset;
        float endedAt = -1f;
        bool struck;

        public static void Spawn(Vector3 position, Vector3 direction, float speed, float range, float damage, SurvivalStats owner) =>
            Spawn(position, direction, speed, range, damage, owner, null, 0, true);

        internal static void Spawn(Vector3 position, Vector3 direction, float speed, float range, float damage,
            SurvivalStats owner, SpikeVolleyFx volley, int index, bool lightFlight)
        {
            var go = new GameObject("Spike") { layer = 2 };
            go.transform.SetPositionAndRotation(position, Quaternion.FromToRotation(Vector3.up, direction));
            var p = go.AddComponent<SpikeProjectile>();
            p.direction = direction.normalized;
            p.speed = speed;
            p.maxDistance = range;
            p.damage = damage;
            p.owner = owner;
            p.ownerRoot = owner != null ? owner.transform : null;
            p.volley = volley;
            p.launchedAt = Time.time;
            p.CreateVisuals(index, lightFlight);
        }

        void CreateVisuals(int index, bool lightFlight)
        {
            hardened = SkillFx.Gel(SpikeVolleyFx.Hardened, 0.62f, 1.2f, 1.4f);
            hardened.SetFloat("_SightMask", 0f);
            tip = SkillFx.Part("Hardened gel tip", SkillFxMeshes.Crystal(), hardened);
            tip.SetParent(transform, false);
            tip.localScale = new Vector3(0.23f, 0.43f, 0.23f);
            // One light follows the centre spike to make the volley readable in dark scenes.
            if (lightFlight)
            {
                flightLight = tip.gameObject.AddComponent<Light>();
                flightLight.color = SpikeVolleyFx.Hardened;
                flightLight.range = 3f;
                flightLight.intensity = 2.4f;
                flightLight.shadows = LightShadows.None;
            }
            var trailRoot = new GameObject("Spike wake") { layer = 2 };
            trailRoot.transform.SetParent(tip, false);
            trail = trailRoot.AddComponent<TrailRenderer>();
            trail.sharedMaterial = hardened;
            trail.generateLightingData = true;
            trail.time = 0.065f;
            trail.minVertexDistance = 0.035f;
            trail.startWidth = 0.065f;
            trail.endWidth = 0.005f;
            trail.startColor = new Color(1f, 1f, 1f, 0.7f);
            trail.endColor = new Color(1f, 1f, 1f, 0f);

            if (volley != null && volley.TryGetAnchor(index, out anchorBody, out anchorLocal))
            {
                Vector3 root = anchorBody.TransformPoint(anchorLocal);
                launchOffset = root + direction * 0.06f - transform.position;
                softGel = SkillFx.Gel(volley.BodyColor, 0.42f, 0.55f, 1.05f);
                strand = SkillFx.Part("Stretching gel strand", SkillFxMeshes.Crystal(), softGel);
                strand.SetParent(transform, true);
                DrawSeparation(0f);
            }
        }

        void DrawSeparation(float age)
        {
            float progress = SkillFxMath.SpikeSeparation(age);
            tip.position = transform.position + launchOffset * (1f - progress);
            if (strand == null) return;
            if (anchorBody == null || age >= SkillFxMath.SpikeSeparationSeconds)
            {
                Destroy(strand.gameObject);
                strand = null;
                return;
            }
            Vector3 root = anchorBody.TransformPoint(anchorLocal);
            Vector3 delta = tip.position - root;
            strand.SetPositionAndRotation(root, Quaternion.FromToRotation(Vector3.up, delta.normalized));
            float width = Mathf.Lerp(0.095f, 0.012f, progress);
            strand.localScale = new Vector3(width, delta.magnitude, width);
        }

        void Update()
        {
            if (endedAt >= 0f)
            {
                float age = Time.time - endedAt;
                if (struck)
                {
                    float squash = Mathf.Clamp01(age / 0.045f);
                    tip.localScale = new Vector3(0.23f * (1f + squash * 0.7f), 0.43f * (1f - squash), 0.23f * (1f + squash * 0.7f));
                }
                else
                {
                    float scale = 1f - Mathf.Clamp01(age / 0.09f);
                    tip.localScale = new Vector3(0.23f, 0.43f, 0.23f) * scale;
                    hardened.SetFloat("_Fade", scale);
                    if (flightLight != null) flightLight.intensity = 2.4f * scale;
                }
                if (age >= 0.10f) Destroy(gameObject);
                return;
            }

            float step = speed * Time.deltaTime;
            foreach (var c in Physics.OverlapSphere(transform.position, CreatureRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (ownerRoot != null && c.transform.IsChildOf(ownerRoot)) continue;
                var target = c.GetComponentInParent<IDamageable>();
                if (target != null)
                {
                    Vector3 point = c.ClosestPoint(transform.position);
                    Vector3 normal = (transform.position - point).normalized;
                    if (normal.sqrMagnitude < 0.01f) normal = -direction;
                    target.TakeDamage(damage, owner);
                    Hit(point, normal, true);
                    return;
                }
            }

            if (Physics.SphereCast(transform.position, Radius, direction, out RaycastHit hit, step + 0.05f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && !(ownerRoot != null && hit.transform.IsChildOf(ownerRoot)))
            {
                var target = hit.collider.GetComponentInParent<IDamageable>();
                target?.TakeDamage(damage, owner);
                Hit(hit.point, hit.normal, target != null);
                return;
            }

            transform.position += direction * step;
            travelled += step;
            DrawSeparation(Time.time - launchedAt);
            if (travelled >= maxDistance) End();
        }

        void Hit(Vector3 point, Vector3 normal, bool creature)
        {
            transform.position = point + normal * 0.015f;
            tip.localPosition = new Vector3(0f, -0.43f, 0f);
            struck = true;
            if (flightLight != null) flightLight.enabled = false;
            if (volley != null) volley.Impact(point, normal, direction, creature);
            else SkillFx.Burst(point, SpikeVolleyFx.Hardened, 5, 2.8f, 0.13f, 0.22f);
            End();
        }

        void End()
        {
            endedAt = Time.time;
            trail.emitting = false;
            if (strand != null) Destroy(strand.gameObject);
        }

        void OnDestroy()
        {
            if (hardened != null) Destroy(hardened);
            if (softGel != null) Destroy(softGel);
        }
    }
}
