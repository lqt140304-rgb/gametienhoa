using System.Collections.Generic;
using Evolve.Abilities;
using Evolve.Creatures;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// A small copy of Chó Xương that climbs out of the ground when the lord summons (the move "Triệu Hồi"): the same model at half size, a fraction of
    /// its health and bite, only the bite and the claw. It rises through the floor in about a second, hunts for a while, then sinks back (also when the
    /// lord dies). At most <see cref="MaxAlive"/> at a time.
    /// </summary>
    public class HoundMinion : MonoBehaviour
    {
        const float RiseSeconds = 1.3f, SinkSeconds = 1f, Depth = 1.4f, MaxAlive = 6;
        static readonly List<HoundMinion> alive = new List<HoundMinion>();

        Predator lordPred, pred;
        Rigidbody rb;
        float born, lifeSeconds, groundY;

        public static void Summon(CreatureCombat lord, MoveDef m, SurvivalStats target)
        {
            if (lord == null || m.SummonCount <= 0) return;
            lord.TryGetComponent(out Predator lordPred);
            for (int i = 0; i < m.SummonCount && alive.Count < MaxAlive; i++)
            {
                float a = Random.value * Mathf.PI * 2f, r = m.Radius * Random.Range(0.5f, 1f);
                Vector3 at = lord.transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                at.y = Evolve.World.GroundHeight.At(at.x, at.z, lord.transform.position.y);
                var go = Instantiate(lord.gameObject, at, Quaternion.Euler(0f, Random.value * 360f, 0f));
                go.name = lord.gameObject.name.Replace("(Clone)", "");
                var minion = go.AddComponent<HoundMinion>();
                minion.Setup(lord, lordPred, m.SummonSeconds, at.y);
                SkillFx.Ring(at, 1.5f, new Color(0.55f, 0.4f, 0.25f), RiseSeconds, 0.2f);   // the ground breaks where it comes up
            }
            if (target != null) foreach (var mi in alive) if (mi != null && mi.pred != null) mi.pred.Provoke(target);
        }

        void Setup(CreatureCombat lord, Predator lordPred, float seconds, float ground)
        {
            this.lordPred = lordPred; lifeSeconds = seconds; groundY = ground; born = Time.time;
            rb = GetComponent<Rigidbody>();
            pred = GetComponent<Predator>();
            transform.localScale = lord.transform.localScale * 0.5f;
            if (TryGetComponent(out CreatureMotor motor)) motor.Resize(0.5f);
            if (TryGetComponent(out CreatureLocomotion loco)) loco.ScaleBy(0.5f);
            if (pred != null)
            {
                pred.SetStats(Mathf.Max(1f, pred.BodyMass * 0.25f), Mathf.Max(5f, pred.MaxHealth * 0.2f));
                pred.ScaleDanger(0.4f, 1f, 1.15f, 0.5f);
            }
            if (TryGetComponent(out CreatureCombat combat))
            {
                combat.LimitMoves(mv => mv.Shape == MoveShape.Lunge && !mv.Pins);   // the bite and the claw, no pounce, no summoning
                combat.DamageScale = lord.DamageScale * 0.4f;
            }
            alive.Add(this);
            if (rb != null) rb.position = new Vector3(transform.position.x, groundY - Depth, transform.position.z);
        }

        float leaveStart = -1f;

        void FixedUpdate()
        {
            if (rb == null) return;
            bool lordGone = lordPred == null || lordPred.Health <= 0f;
            if (leaveStart < 0f && (Time.time - born > lifeSeconds || lordGone)) leaveStart = Time.time;
            float depth;
            if (leaveStart >= 0f)
            {
                float t = (Time.time - leaveStart) / SinkSeconds;
                if (t >= 1f) { Destroy(gameObject); return; }
                depth = Depth * Mathf.SmoothStep(0f, 1f, t);
            }
            else depth = Time.time - born < RiseSeconds ? Depth * (1f - Mathf.SmoothStep(0f, 1f, (Time.time - born) / RiseSeconds)) : 0f;
            if (leaveStart >= 0f || depth > 0.001f)
            {
                CreatureStatus.Stun(this, 0.2f, false);   // it does not walk or bite while it is coming up or going down
                var p = rb.position;
                rb.MovePosition(new Vector3(p.x, groundY - depth, p.z));
            }
        }

        void OnDestroy() => alive.Remove(this);
    }
}
