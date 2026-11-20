using System;
using Evolve.Creatures;
using Evolve.Ecology;
using Evolve.Player;
using Evolve.Survival;
using Evolve.Swallow;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Evolve.Combat
{
    /// <summary>
    /// One slime latched on to one monster (spec 2026-11-20): the slime rides the monster's back, the grip mini game
    /// (<see cref="LatchGame"/>) runs, digesting hurts the monster, and the fight ends when the monster dies (swallowed on the spot),
    /// shakes the slime off, vanishes, or the slime lets go (Space outside a heavy struggle) or dies.
    /// </summary>
    public class LatchSession : MonoBehaviour
    {
        const float TickSeconds = 0.25f, RememberSeconds = 120f, FleeSeconds = 3f;

        /// <summary>The local slime's session, or null.</summary>
        public static LatchSession Active { get; private set; }
        public LatchGame Game { get; private set; }
        public event Action<LatchState> Ended;

        SurvivalStats slime;
        Component monster;
        IDamageable target;
        Rigidbody rb;
        Collider slimeCol, monsterCol;
        SlimeController controller;
        Swallower swallower;
        Vector3 lastMonsterPos;
        float pendingDamage, nextTick;
        bool squeeze, release, finished;

        public static LatchSession Begin(SurvivalStats slimeStats, Component monster, StruggleProfile profile)
        {
            if (Active != null || slimeStats == null || monster == null || !(monster is IDamageable)) return null;
            var s = slimeStats.gameObject.AddComponent<LatchSession>();
            s.Init(slimeStats, monster, profile);
            return s;
        }

        void Init(SurvivalStats slimeStats, Component m, StruggleProfile profile)
        {
            slime = slimeStats; monster = m; target = (IDamageable)m;
            rb = GetComponent<Rigidbody>(); slimeCol = GetComponent<Collider>(); controller = GetComponent<SlimeController>(); swallower = GetComponent<Swallower>();
            monsterCol = m.GetComponentInChildren<Collider>();
            float slimeMass = TryGetComponent(out SlimeBody body) ? body.Mass : 10f;
            float monsterMass = m is Prey pr ? pr.BodyMass : m is Predator pd ? pd.BodyMass : 10f;
            Game = new LatchGame(profile, slimeMass, monsterMass, UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            SetLatched(true);
            if (monsterCol != null && slimeCol != null) Physics.IgnoreCollision(slimeCol, monsterCol, true);
            if (rb != null) rb.useGravity = false;
            lastMonsterPos = m.transform.position;
            Active = this;
        }

        void SetLatched(bool on)
        {
            if (monster is Prey p) p.Latched = on;
            else if (monster is Predator h) h.Latched = on;
        }

        float MonsterHealth01 => monster is Prey p ? p.HealthFraction : monster is Predator h ? h.HealthFraction : 0f;

        void Update()
        {
            var k = Keyboard.current;
            if (k != null && k.spaceKey.wasPressedThisFrame)
            {
                if (Game != null && Game.Telegraphing) squeeze = true;   // right on the warning: squeeze
                else release = true;                                      // otherwise: let go
            }
        }

        float ReadPull()
        {
            float v = 0f;
            var k = Keyboard.current;
            if (k != null) v = (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f);
            if (Mouse.current != null) v += Mathf.Clamp(Mouse.current.delta.ReadValue().x * 0.05f, -1f, 1f);
            return Mathf.Clamp(v, -1f, 1f);
        }

        void FixedUpdate()
        {
            if (finished) return;
            if (monster == null || slime == null || slime.State.IsDead) { Finish(LatchState.Escaped, false); return; }
            lastMonsterPos = monster.transform.position;
            if (release) { Finish(LatchState.Escaped, true); return; }

            // ride on the monster's back, leaning with the struggle
            if (rb != null && monsterCol != null)
            {
                var b = monsterCol.bounds;
                Vector3 right = Camera.main != null ? Camera.main.transform.right : Vector3.right;
                Vector3 top = b.center + Vector3.up * (b.extents.y + 0.35f) + right * ((Game.Dot - 0.5f) * b.extents.x * 2f);
                rb.linearVelocity = Vector3.zero;
                rb.MovePosition(Vector3.Lerp(rb.position, top, 0.35f));
            }
            if (controller != null) controller.FreezeFor(0.2f);

            var step = Game.Step(Time.fixedDeltaTime, ReadPull(), squeeze, MonsterHealth01, slime.State.Energy);
            squeeze = false;
            if (step.EnergySpent > 0f) slime.State.TrySpendEnergy(step.EnergySpent);
            pendingDamage += step.Damage;
            if (pendingDamage > 0f && Time.time >= nextTick)
            {
                target.TakeDamage(pendingDamage, slime);
                pendingDamage = 0f; nextTick = Time.time + TickSeconds;
            }
            if (MonsterHealth01 <= 0f || step.State == LatchState.MonsterDead) Finish(LatchState.MonsterDead, false);
            else if (step.State == LatchState.Escaped) Finish(LatchState.Escaped, true);
        }

        void Finish(LatchState state, bool monsterSurvives)
        {
            if (finished) return;
            finished = true;
            if (monster != null) SetLatched(false);
            if (monsterCol != null && slimeCol != null) Physics.IgnoreCollision(slimeCol, monsterCol, false);
            if (rb != null) rb.useGravity = true;

            if (state == LatchState.Escaped && monster != null)
            {
                Vector3 away = (transform.position - monster.transform.position); away.y = 0f;
                if (rb != null) rb.AddForce(away.normalized * 4f + Vector3.up * 3f, ForceMode.VelocityChange);
                if (controller != null) controller.FreezeFor(0.5f);
                if (monsterSurvives)   // it hurts and runs, and it remembers this slime
                {
                    if (monster is Prey p) { p.ForceFleeUntil = Time.time + FleeSeconds; p.RememberSlimeUntil = Time.time + RememberSeconds; }
                    else if (monster is Predator h) { h.LoseTarget(FleeSeconds); h.RememberSlimeUntil = Time.time + RememberSeconds; }
                }
            }
            else if (state == LatchState.MonsterDead && swallower != null)
            {
                Carcass best = null; float bestD = 2.5f * 2.5f;
                foreach (var c in Carcass.Active)
                {
                    if (c == null) continue;
                    float d = (c.transform.position - lastMonsterPos).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = c; }
                }
                swallower.SwallowCarcassNow(best);   // the freshest body there is: Giải Mã reads its best genes
            }
            Active = null;
            Ended?.Invoke(state);
            Destroy(this);
        }

        void OnDestroy()
        {
            if (!finished) { finished = true; if (monster != null) SetLatched(false); if (rb != null) rb.useGravity = true; }
            if (Active == this) Active = null;
        }
    }
}
