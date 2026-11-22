using Evolve.Abilities;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>Host-authoritative glow and sidestep for the Rainbow Ore Garden prey.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(70)]
    public sealed class HangTwoPrey : MonoBehaviour
    {
        Prey prey;
        Rigidbody body;
        Collider shape;
        CreatureLocomotion locomotion;
        CreatureGlow glow;
        bool firefly, dodging;
        float activatedAt = -100f, lastUse = -100f;
        Vector3 dodgeFrom, dodgeTo;
        TrailRenderer trail;
        Material trailMaterial;
        Transform model;
        Vector3 modelRestPosition;
        Quaternion modelRestRotation;
        bool posed;
        public bool IsDodging => dodging && HangTwoRules.Dodging(Time.time - activatedAt);
        public bool IsGlowing => firefly && HangTwoRules.GlowActive(Time.time - activatedAt);

        void Awake()
        {
            prey = GetComponent<Prey>();
            body = GetComponent<Rigidbody>();
            shape = GetComponent<Collider>();
            locomotion = GetComponent<CreatureLocomotion>();
            firefly = SpeciesNames.Key(name) == "domdomsacmau";
            if (firefly) glow = new CreatureGlow(gameObject);
            model = transform.Find("Model");
            if (model != null) { modelRestPosition = model.localPosition; modelRestRotation = model.localRotation; }
        }

        public bool TryEscape()
        {
            if (prey == null || prey.IsReplica || Prey.Frozen || body == null || CreatureStatus.Holds(gameObject)) return false;
            float cooldown = firefly ? HangTwoRules.GlowCooldown : HangTwoRules.DodgeCooldown;
            if (!HangTwoRules.Ready(Time.time, lastUse, cooldown)) return false;
            if (!firefly && !FindSidestep()) return false;
            activatedAt = lastUse = Time.time;
            if (firefly)
            {
                locomotion?.PlayAction("Action");
                // A short flash breaks nearby hunters' tracking. The body keeps glowing for ten seconds.
                foreach (var hunter in Predator.Active)
                    if (hunter != null && !hunter.IsReplica && Vector3.Distance(hunter.transform.position, transform.position) <= 4f)
                        hunter.LoseTarget(1f);
            }
            else
            {
                dodging = true;
                prey.RefreshStamina();
                CreatureStatus.Stun(prey, HangTwoRules.DodgeSeconds, false);
                locomotion?.PlayAction("Evade");
                BeginTrail();
            }
            return true;
        }

        bool FindSidestep()
        {
            Vector3 away = Vector3.zero;
            float nearest = float.MaxValue;
            foreach (var player in SurvivalStats.All)
            {
                if (player == null || player.State.IsDead) continue;
                Vector3 delta = body.position - player.transform.position;
                if (delta.sqrMagnitude >= nearest) continue;
                nearest = delta.sqrMagnitude;
                away = delta;
            }
            if (!Predator.Passive)
                foreach (var hunter in Predator.Active)
                {
                    if (hunter == null || hunter.IsReplica) continue;
                    Vector3 delta = body.position - hunter.transform.position;
                    if (delta.sqrMagnitude >= nearest) continue;
                    nearest = delta.sqrMagnitude;
                    away = delta;
                }
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = -transform.forward;
            Vector3 side = Vector3.Cross(away.normalized, Vector3.up);
            float offset = Evolve.World.GroundHeight.OffsetOf(body.position);
            float radius = shape != null ? Mathf.Max(shape.bounds.extents.x, shape.bounds.extents.z) : 0.25f;
            Vector3 centerOffset = shape != null ? shape.bounds.center - body.position : Vector3.up * radius;
            centerOffset.y = Mathf.Max(centerOffset.y, radius + 0.04f);
            // Try both lateral directions. An obstructed dodge spends no cooldown.
            for (int i = 0; i < 2; i++)
            {
                Vector3 destination = Evolve.World.GroundHeight.Follow(body.position + side * (i == 0 ? 1f : -1f) * HangTwoRules.DodgeDistance, offset);
                if (Mathf.Abs(destination.y - body.position.y) > 0.5f ||
                    Evolve.World.WaterField.DepthAt(destination.x, destination.z) > 0.05f) continue;
                if (!HangOneGround.Clear(body.position + centerOffset, destination + centerOffset, radius)) continue;
                dodgeFrom = body.position;
                dodgeTo = destination;
                return true;
            }
            return false;
        }

        void FixedUpdate()
        {
            if (!dodging) return;
            if (prey.IsReplica || Prey.Frozen || prey.Health <= 0f) { EndDodge(); return; }
            float elapsed = Time.time - activatedAt;
            Vector3 next = Vector3.Lerp(dodgeFrom, dodgeTo, HangTwoRules.DodgeProgress(elapsed));
            float radius = shape != null ? Mathf.Max(shape.bounds.extents.x, shape.bounds.extents.z) : 0.25f;
            Vector3 offset = shape != null ? shape.bounds.center - body.position : Vector3.up * radius;
            offset.y = Mathf.Max(radius + 0.04f, offset.y);
            if (!HangOneGround.Clear(body.position + offset, next + offset, radius)) { EndDodge(); return; }
            body.MovePosition(next);
            if (!HangTwoRules.Dodging(elapsed)) EndDodge();
        }

        void LateUpdate()
        {
            if (firefly)
            {
                float elapsed = Time.time - activatedAt;
                float fade = IsGlowing ? Mathf.Min(Mathf.Clamp01(elapsed / 0.18f), Mathf.Clamp01((HangTwoRules.GlowSeconds - elapsed) / 0.5f)) : 0f;
                glow?.Set(fade * (0.75f + 0.10f * Mathf.Sin(Time.time * 8f)));
            }
            if (model == null || (!dodging && !posed)) return;
            float progress = HangTwoRules.DodgeProgress(Time.time - activatedAt);
            float bend = dodging ? Mathf.Sin(progress * Mathf.PI) : 0f;
            model.localPosition = modelRestPosition + Vector3.up * (0.2f * bend);
            model.localRotation = modelRestRotation * Quaternion.Euler(0f, 0f, 24f * bend);
            posed = dodging;
        }

        void BeginTrail()
        {
            if (trail == null)
            {
                var go = new GameObject("Leaf dodge trail");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.up * 0.16f;
                trail = go.AddComponent<TrailRenderer>();
                trailMaterial = SkillFx.Gel(new Color(0.53f, 0.65f, 0.27f), 0.55f, 0f, 0.75f);
                trail.sharedMaterial = trailMaterial;
                trail.time = 0.14f;
                trail.startWidth = 0.11f;
                trail.endWidth = 0f;
                trail.minVertexDistance = 0.1f;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;
            }
            trail.Clear();
            trail.emitting = true;
        }

        void EndDodge()
        {
            dodging = false;
            if (trail != null) trail.emitting = false;
        }

        void OnDisable()
        {
            EndDodge();
            activatedAt = -100f;
            if (model != null && posed) { model.localPosition = modelRestPosition; model.localRotation = modelRestRotation; }
            posed = false;
            glow?.Set(0f);
        }

        void OnDestroy() { if (trailMaterial != null) Destroy(trailMaterial); }
    }
}
