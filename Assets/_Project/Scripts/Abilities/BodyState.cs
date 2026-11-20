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

        float damageUntil, rootUntil, shieldUntil, lightUntil, climbUntil, nextFlash, nextChip;
        float damageMultiplier = 1f;
        int shieldCharges;
        Light glow;
        Vector3 lastMoveDir;
        SurvivalStats stats;
        OrganHost host;
        Rigidbody rb;

        public bool Shielded => Time.time < shieldUntil && shieldCharges > 0;
        public bool Climbing => Time.time < climbUntil;
        public bool Glowing => glow != null && glow.enabled;

        /// <summary>Hunters spot a glowing slime (Phát Sáng) from further away. Read by Predator.</summary>
        public static float DetectMultiplier(Component player) =>
            player.TryGetComponent(out BodyState s) && s.Glowing ? Evolve.Swallow.CaveSkillMath.GlowDetectMultiplier : 1f;

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
            int before = Time.time < shieldUntil ? shieldCharges : 0, charges = before;
            float state = Time.time < damageUntil ? damageMultiplier : 1f;
            float reduction = host != null ? host.DamageReduction : 0f;
            float left = Evolve.Swallow.CaveSkillMath.Incoming(amount, reduction, state, ref charges);
            if (Time.time < shieldUntil) shieldCharges = charges;

            var stone = Evolve.Swallow.SkillFxMath.FamilyColor(Evolve.Swallow.SkillFamily.Stone);
            if (charges < before)   // the shield ate a hit: it bursts on the last one, flashes on the others
            {
                if (charges == 0) SkillFx.ShieldBreak(transform, stone); else SkillFx.Shell(transform, stone, 0.3f, 0.3f, 2f, 1.15f);
            }
            else if (reduction > 0f && left > 0f && left < amount && Time.time >= nextFlash)   // Vỏ Cứng shrugged a hit off
            {
                nextFlash = Time.time + 0.4f;
                SkillFx.Shell(transform, stone, 0.3f, 0.25f, 2f, 1.1f);
            }
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
            // Bám Vách: rock chips fall off the wall while the body walks up it
            if (Climbing && rb != null && rb.linearVelocity.y > 0.5f && Time.time >= nextChip)
            {
                nextChip = Time.time + 0.09f;
                SkillFx.Burst(rb.position + lastMoveDir * 0.5f, Evolve.Swallow.SkillFxMath.FamilyColor(Evolve.Swallow.SkillFamily.Agility), 2, 0.5f, 0.1f, 0.6f, 0f, 0.1f);
            }
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
