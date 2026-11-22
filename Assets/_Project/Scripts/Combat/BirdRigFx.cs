using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// Wings and beak for the Meshy Chim Hang Mẹ (prefab "ChimHangMe (Meshy)"), driven from its rig bones: Pinion_L1/L2/L3 and Pinion_R1/R2/R3
    /// (three segments per wing, shoulder to wing tip; called Pinion, not Wing, so CreatureLocomotion keeps stepping on the legs), Root, Beak (the lower
    /// beak, modelled open) with a tiny BeakTip child. The wings fold in against the body at rest, open when it moves and spread wide and beat hard in
    /// a fight (a fast downstroke, an elbow that bends on the upstroke, a small bob of the body); the beak shuts at rest and opens for a trembling screech.
    /// Which way each bone must turn is measured at start, so no bone axis is assumed. Runs after the other animation (LateUpdate).
    /// </summary>
    [DefaultExecutionOrder(70)]   // after CreatureLocomotion (50) and CreatureCombat (60), which restore Model's transform each LateUpdate
    public class BirdRigFx : MonoBehaviour
    {
        public static bool Has(string name) => !string.IsNullOrEmpty(name) && name.Contains("(Meshy)") && Creatures.SpeciesNames.Key(name) == "chimhangme";

        public static BirdRigFx Attach(GameObject creature) =>
            creature == null || creature.GetComponent<BirdRigFx>() != null ? null : creature.AddComponent<BirdRigFx>();

        class Seg { public Transform T; public Quaternion Rest; public Vector3 Lift, Swing; }
        class Wing { public Seg[] S = new Seg[3]; public float LiftSign = 1f, SwingSign = 1f; }

        const float BeakClosedDeg = 26f;
        Wing[] wings;
        Transform beak, beakTip, root;
        Vector3 rootRest, rootUp;
        Quaternion beakRest;
        Vector3 beakAxis;
        float beakSign = 1f, open, spread, beat, nextScreech, screechT = -1f, phase;
        CreatureCombat combat;
        Vector3 lastPos;
        float moving, lastHealth = -1, lastCycle;
        Evolve.Creatures.Predator pred;

        void Start()
        {
            combat = GetComponent<CreatureCombat>();
            pred = GetComponent<Evolve.Creatures.Predator>();
            Transform[,] w = new Transform[2, 3];
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                if (n.Length == 9 && n.StartsWith("Pinion_") && (n[7] == 'L' || n[7] == 'R') && n[8] >= '1' && n[8] <= '3') w[n[7] == 'L' ? 0 : 1, n[8] - '1'] = t;
                else if (n == "Beak") beak = t;
                else if (n == "BeakTip") beakTip = t;
                else if (n == "Root") root = t;
            }
            lastPos = transform.position;
            wings = new Wing[2];
            for (int i = 0; i < 2; i++) wings[i] = MakeWing(w[i, 0], w[i, 1], w[i, 2]);
            if (root != null) { rootRest = root.localPosition; rootUp = root.parent != null ? root.parent.InverseTransformDirection(transform.up) : transform.up; }
            if (beak != null && beakTip != null)
            {
                beakRest = beak.localRotation;
                beakAxis = beak.InverseTransformDirection(transform.right);
                float y = beakTip.position.y;
                beak.localRotation = beakRest * Quaternion.AngleAxis(10f, beakAxis);
                beakSign = beakTip.position.y > y ? 1f : -1f;   // which way raises the lower beak, i.e. shuts it
                beak.localRotation = beakRest;
            }
            nextScreech = Time.time + Random.Range(4f, 8f);
        }

        Wing MakeWing(Transform a, Transform b, Transform c)
        {
            if (a == null || b == null) return null;
            var wing = new Wing();
            var ts = new[] { a, b, c };
            for (int i = 0; i < 3; i++)
                if (ts[i] != null)
                    wing.S[i] = new Seg { T = ts[i], Rest = ts[i].localRotation, Lift = ts[i].InverseTransformDirection(transform.forward), Swing = ts[i].InverseTransformDirection(transform.up) };
            // lift: turning about the creature's forward axis raises or lowers the tip; swing: turning about up sweeps the tip toward the tail or the head
            float y = b.position.y;
            a.localRotation = wing.S[0].Rest * Quaternion.AngleAxis(10f, wing.S[0].Lift);
            wing.LiftSign = b.position.y > y ? 1f : -1f;
            a.localRotation = wing.S[0].Rest;
            float f = Vector3.Dot(b.position, transform.forward);
            a.localRotation = wing.S[0].Rest * Quaternion.AngleAxis(10f, wing.S[0].Swing);
            wing.SwingSign = Vector3.Dot(b.position, transform.forward) < f ? 1f : -1f;   // + sweeps the wing back
            a.localRotation = wing.S[0].Rest;
            return wing;
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f), t = Time.time;
            float speed = (transform.position - lastPos).magnitude / dt; lastPos = transform.position;
            moving = Mathf.MoveTowards(moving, Mathf.Clamp01(speed / 1.5f), dt * 4f);
            bool busy = combat != null && combat.Busy;
            beat = Mathf.MoveTowards(beat, busy ? 1f : 0f, dt * 4f);
            if (busy) BossMusic.Ping("bird_battle");                                                                  // a move in progress is a blow in the fight: the boss music
            if (pred != null)
            {
                if (lastHealth > 0f && pred.Health < lastHealth - 0.01f) { ProceduralSfx.PlayFile("Bird/bird_hurt", transform.position, 1f, ProceduralSfx.Kind.HoundHurt); BossMusic.Ping("bird_battle"); }
                lastHealth = pred.Health;
            }
            // folded at rest (0), half open walking, wide open and beating in a fight (1)
            spread = Mathf.MoveTowards(spread, Mathf.Max(beat, 0.55f * moving), dt * 3f);

            float freq = 0.5f + 0.9f * moving + 2.6f * beat;
            phase += 6.2832f * freq * dt;
            float s = Mathf.Sin(phase);
            float cycle = Mathf.Floor(phase / 6.2832f);
            if (cycle != lastCycle) { lastCycle = cycle; if (beat > 0.4f) ProceduralSfx.PlayFile("Bird/bird_flap", transform.position, 0.7f, ProceduralSfx.Kind.HoundSwipe); }   // a wingbeat in a fight is heard
            float stroke = s > 0f ? s : s * 1.7f;   // the up stroke is slower than the power stroke down
            float amp = 6f + 10f * moving + 48f * beat;
            float fold = 1f - spread;

            if (wings != null)
                foreach (var wing in wings)
                {
                    if (wing == null) continue;
                    for (int i = 0; i < 3; i++)
                    {
                        var seg = wing.S[i];
                        if (seg == null) continue;
                        float lag = Mathf.Sin(phase - 0.6f * i);
                        float flap = (i == 0 ? stroke : (lag > 0f ? lag : lag * 1.7f) * 1.25f) * amp;   // outer segments lag and swing wider
                        float sweep = fold * (i == 0 ? 55f : i == 1 ? 80f : 60f) - beat * (i == 2 ? 12f * Mathf.Max(0f, -s) : 0f);
                        seg.T.localRotation = seg.Rest * Quaternion.AngleAxis(wing.SwingSign * sweep, seg.Swing) * Quaternion.AngleAxis(wing.LiftSign * flap, seg.Lift);
                    }
                }

            // the body dips and lifts with the beats
            if (root != null && beat > 0.01f)
                root.localPosition = rootRest + rootUp * (0.05f * beat * Mathf.Max(0f, s) * (1f / Mathf.Max(0.0001f, root.parent != null ? root.parent.lossyScale.y : 1f)));
            else if (root != null) root.localPosition = rootRest;

            if (beak != null && beakTip != null)
            {
                if (screechT < 0f && !busy && t > nextScreech) { screechT = 0f; nextScreech = t + Random.Range(7f, 12f); ProceduralSfx.PlayFile("Bird/bird_screech", transform.position, 0.9f, ProceduralSfx.Kind.HoundHowl); }
                float screech = 0f;
                if (screechT >= 0f) { screechT += dt; screech = Mathf.Sin(Mathf.Clamp01(screechT / 1.1f) * Mathf.PI); if (screechT > 1.1f) screechT = -1f; }
                float target = Mathf.Max(busy ? 1f : 0f, Mathf.Clamp01(screech * 1.6f));
                open = Mathf.MoveTowards(open, target, dt * 8f);
                float quiver = open > 0.5f ? Mathf.Sin(t * 110f) * 2.5f * open : 0f;
                beak.localRotation = beakRest * Quaternion.AngleAxis(beakSign * (BeakClosedDeg * (1f - open) + quiver), beakAxis);
            }
        }
    }
}
