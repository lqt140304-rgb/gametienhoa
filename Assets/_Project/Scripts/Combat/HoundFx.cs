using Evolve.Abilities;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// Sound and visual effects for Chó Xương's moves, fired from CreatureCombat at the start of each phase: a growl as the pounce winds up, a heavy
    /// whoosh and a dust kick as it launches, a thud and a dust ring where it lands; the jaw snap of the bite, the swipe of the claw, the rolling roar
    /// with a pale shock ring, the howl and the ground burst of the summoning, the head-back growl and the shard of the bone spit.
    /// </summary>
    public static class HoundFx
    {
        static readonly Color Dust = new Color(0.62f, 0.56f, 0.46f), Wave = new Color(0.85f, 0.88f, 0.9f), Bone = new Color(0.9f, 0.86f, 0.74f);

        public static void OnPhase(CreatureCombat dog, MovePhase phase, MoveDef m)
        {
            if (dog == null || m == null) return;
            Vector3 p = dog.transform.position;
            float scale = Mathf.Max(0.5f, dog.transform.lossyScale.y);
            var K = ProceduralSfx.Kind.HoundGrowlShort;
            switch (m.Anim)
            {
                case "Pounce":
                    if (phase == MovePhase.Windup) ProceduralSfx.Play(K, p, 0.8f);
                    else if (phase == MovePhase.Active) { ProceduralSfx.Play(ProceduralSfx.Kind.HoundLunge, p, 1f); SkillFx.Dust(p, 1.2f * scale, Dust, 18); }
                    else if (phase == MovePhase.Recover) Land(p, scale);
                    break;
                case "Bite":
                    if (phase == MovePhase.Windup) ProceduralSfx.Play(K, p, 0.5f);
                    else if (phase == MovePhase.Active) ProceduralSfx.Play(ProceduralSfx.Kind.HoundSnap, p, 1f);
                    break;
                case "Claw":
                    if (phase == MovePhase.Active) ProceduralSfx.Play(ProceduralSfx.Kind.HoundSwipe, p, 0.9f);
                    break;
                case "Roar":
                    if (phase == MovePhase.Windup) ProceduralSfx.Play(K, p, 0.6f);
                    else if (phase == MovePhase.Active)
                    {
                        ProceduralSfx.Play(ProceduralSfx.Kind.HoundRoar, p, 1f);
                        SkillFx.Ring(p, m.Radius, Wave, 0.7f, 0.15f);
                        SkillFx.Dust(p, 2.5f * scale, Dust, 26);
                    }
                    break;
                case "Howl":
                    if (phase == MovePhase.Windup) ProceduralSfx.Play(ProceduralSfx.Kind.HoundHowl, p, 1f);
                    else if (phase == MovePhase.Active) { ProceduralSfx.Play(ProceduralSfx.Kind.HoundThud, p, 0.9f); SkillFx.Dust(p, m.Radius, Dust, 40); }
                    break;
                case "Spit":
                    if (phase == MovePhase.Windup) ProceduralSfx.Play(K, p, 0.7f);
                    else if (phase == MovePhase.Active) { ProceduralSfx.Play(ProceduralSfx.Kind.HoundSpit, p, 1f); SkillFx.Burst(p + Vector3.up * 0.6f * scale, Bone, 10, 3f, 0.08f, 0.5f); }
                    break;
            }
        }

        /// <summary>It lands (also after a miss): a thud and a ring of dust.</summary>
        public static void Land(Vector3 p, float scale)
        {
            ProceduralSfx.Play(ProceduralSfx.Kind.HoundThud, p, 1f);
            SkillFx.Dust(p, 1.8f * scale, Dust, 30);
            SkillFx.Ring(p, 2.2f * scale, Dust, 0.5f, 0.2f);
        }

        /// <summary>It is hit: a short angry rasp (not more than one every 0.4 s).</summary>
        static float lastHurt = -10f;
        public static void Hurt(Vector3 p)
        {
            if (Time.time - lastHurt < 0.4f) return;
            lastHurt = Time.time;
            ProceduralSfx.Play(ProceduralSfx.Kind.HoundHurt, p, 0.9f);
        }

        /// <summary>A bone shard lands or hits.</summary>
        public static void ShardImpact(Vector3 p)
        {
            ProceduralSfx.Play(ProceduralSfx.Kind.HoundShatter, p, 0.9f);
            SkillFx.Burst(p, Bone, 14, 4f, 0.09f, 0.6f);
            SkillFx.Ring(p, 0.9f, Bone, 0.35f, 0.2f);
        }
    }
}
