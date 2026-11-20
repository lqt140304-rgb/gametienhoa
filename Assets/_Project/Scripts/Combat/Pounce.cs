using System.Collections.Generic;
using Evolve.Creatures;
using Evolve.Player;
using Evolve.Survival;
using Evolve.Swallow;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Evolve.Combat
{
    /// <summary>
    /// Vồ (pounce), the basic slime action from the start (spec 2026-11-20): left mouse button lunges at the nearest creature in front
    /// (about 4 m). A creature the slime can swallow whole is caught at once; a bigger one is latched on to (grip mini game); one over
    /// four times the slime's mass bounces it off. Nothing in reach: nothing happens and no energy is spent.
    /// </summary>
    public class Pounce : MonoBehaviour
    {
        const float Reach = 4f, Lunge = 8f;

        SurvivalStats stats;
        Rigidbody rb;
        SlimeController controller;
        SlimeBody body;
        readonly List<Candidate> candidates = new List<Candidate>();
        readonly List<Component> creatures = new List<Component>();

        void Awake()
        {
            stats = GetComponent<SurvivalStats>();
            rb = GetComponent<Rigidbody>();
            controller = GetComponent<SlimeController>();
            body = GetComponent<SlimeBody>();
        }

        void Update()
        {
            if (stats == null || stats.IsRemote || stats.State.IsDead || stats.Frozen || LatchSession.Active != null) return;
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            if (AimController.MouseUiOpen || SurvivalHUD.BlocksInput(gameObject)) return;
            TryPounce();
        }

        void TryPounce()
        {
            candidates.Clear(); creatures.Clear();
            foreach (var p in Prey.Active) Add(p, p.IsReplica, p.Latched);
            foreach (var h in Predator.Active) Add(h, h.IsReplica, h.Latched);

            Vector3 face = Camera.main != null ? Camera.main.transform.forward : transform.forward;
            Vector3 pos = transform.position;
            int i = PounceRules.PickTarget(candidates, pos.x, pos.z, face.x, face.z, Reach);
            if (i < 0 || !stats.State.TrySpendEnergy(stats.Config.HopEnergyCost)) return;   // nothing in reach: no cost

            var target = creatures[i];
            float slimeMass = body != null ? body.Mass : 10f;
            float monsterMass = target is Prey pr ? pr.BodyMass : ((Predator)target).BodyMass;
            Vector3 to = target.transform.position - pos; to.y = 0f;
            var outcome = PounceRules.Decide(slimeMass, monsterMass);

            if (outcome == PounceOutcome.Bounce)
            {
                rb.AddForce(-to.normalized * 5f + Vector3.up * 3f, ForceMode.VelocityChange);
                if (controller != null) controller.FreezeFor(0.4f);
                Echo.Say("«Quá to để vồ.»");
                return;
            }
            rb.AddForce(to.normalized * Lunge + Vector3.up * 2f, ForceMode.VelocityChange);
            if (outcome == PounceOutcome.SwallowWhole && target is Prey prey) { prey.PounceCatch(stats); return; }
            // a hunter small enough to swallow is still held and digested (hunters leave a carcass to learn from)
            LatchSession.Begin(stats, target, StruggleSeed.For(target.name));
        }

        void Add(Component c, bool replica, bool latched)
        {
            if (replica) return;
            candidates.Add(new Candidate(c.transform.position.x, c.transform.position.z, latched));
            creatures.Add(c);
        }
    }
}
