using Evolve.Organs;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// Timed states a skill puts on the slime's own body: less damage taken (Thu Mình, Lách Né), a shield that eats whole hits
    /// (Khiên Quặng), rooted in place (Thu Mình), a light (Phát Sáng), climbing walls (Bám Vách). Also applies the passive
    /// damage reduction of Vỏ Cứng (OrganHost.DamageReduction) through SurvivalState.IncomingDamage.
    /// </summary>
    [RequireComponent(typeof(SurvivalStats))]
    public class BodyState : MonoBehaviour
    {
        const float ClimbSpeed = 3f, WallReach = 0.9f;

        float damageUntil, rootUntil, shieldUntil, lightUntil, climbUntil;
        float damageMultiplier = 1f;
        int shieldCharges;
        Light glow;
        Vector3 lastMoveDir;
        SurvivalStats stats;
        OrganHost host;
        Rigidbody rb;

        public bool Shielded => Time.time < shieldUntil && shieldCharges > 0;
        public bool Climbing => Time.time < climbUntil;

        public static BodyState On(GameObject user) =>
            user.TryGetComponent(out BodyState s) ? s : user.AddComponent<BodyState>();

        void Awake()
        {
            stats = GetComponent<SurvivalStats>();
            host = GetComponent<OrganHost>();
            rb = GetComponent<Rigidbody>();
        }

        void OnEnable()
        {
            if (stats == null) return;
            stats.State.IncomingDamage = Filter;
            stats.State.Died += ClearAll;
        }

        void OnDisable()
        {
            if (stats == null) return;
            if (stats.State.IncomingDamage == Filter) stats.State.IncomingDamage = null;
            stats.State.Died -= ClearAll;
        }

        float Filter(float amount)
        {
            int charges = Time.time < shieldUntil ? shieldCharges : 0;
            float state = Time.time < damageUntil ? damageMultiplier : 1f;
            float left = Evolve.Swallow.CaveSkillMath.Incoming(amount, host != null ? host.DamageReduction : 0f, state, ref charges);
            if (Time.time < shieldUntil) shieldCharges = charges;
            return left;
        }

        public void TakeLessDamage(float multiplier, float seconds) { damageMultiplier = multiplier; damageUntil = Time.time + seconds; }
        public void Root(float seconds) => rootUntil = Time.time + seconds;
        public void Shield(int hits, float seconds) { shieldCharges = hits; shieldUntil = Time.time + seconds; }
        public void Climb(float seconds) => climbUntil = Time.time + seconds;

        public void Glow(float range, float seconds)
        {
            if (glow == null)
            {
                var go = new GameObject("SkillGlow");
                go.transform.SetParent(transform, false);
                glow = go.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.color = new Color(1f, 0.95f, 0.75f);
                glow.shadows = LightShadows.None;
            }
            glow.range = range;
            glow.intensity = 2f;
            glow.enabled = true;
            lightUntil = Time.time + seconds;
        }

        void Update()
        {
            if (glow != null && glow.enabled && Time.time >= lightUntil) glow.enabled = false;
        }

        void FixedUpdate()
        {
            if (rb == null) return;
            Vector3 v = rb.linearVelocity;
            Vector3 flat = new Vector3(v.x, 0f, v.z);
            if (flat.sqrMagnitude > 0.04f) lastMoveDir = flat.normalized;

            if (Time.time < rootUntil) rb.linearVelocity = new Vector3(0f, Mathf.Min(0f, v.y), 0f);

            // Bám Vách: pushing against a wall while the skill runs walks the body up it.
            if (Climbing && lastMoveDir != Vector3.zero
                && Physics.Raycast(rb.position, lastMoveDir, WallReach, ~0, QueryTriggerInteraction.Ignore))
                rb.linearVelocity = new Vector3(v.x, ClimbSpeed, v.z);
        }

        /// <summary>Death and respawn end every state.</summary>
        public void ClearAll()
        {
            damageUntil = rootUntil = shieldUntil = lightUntil = climbUntil = 0f;
            shieldCharges = 0;
            if (glow != null) glow.enabled = false;
        }
    }
}
