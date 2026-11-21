using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// Wings and beak for the Meshy Chim Hang Mẹ (prefab "ChimHangMe (Meshy)"), driven from its rig bones:
    /// Pinion_L1/L2 and Pinion_R1/R2 (the two segments of each wing; called Pinion, not Wing, so CreatureLocomotion keeps stepping on the legs),
    /// Beak (the lower beak, modelled open) with a tiny BeakTip child. Wings hold, flutter when it walks and beat hard while it fights;
    /// the beak shuts at rest and opens for a trembling screech now and then and during moves.
    /// Which way each bone must turn is measured at start, so no bone axis is assumed. Runs after the other animation (LateUpdate).
    /// </summary>
    public class BirdRigFx : MonoBehaviour
    {
        public static bool Has(string name) => !string.IsNullOrEmpty(name) && name.Contains("(Meshy)") && Creatures.SpeciesNames.Key(name) == "chimhangme";

        public static BirdRigFx Attach(GameObject creature) =>
            creature == null || creature.GetComponent<BirdRigFx>() != null ? null : creature.AddComponent<BirdRigFx>();

        class Wing { public Transform Inner, Outer; public Quaternion InnerRest, OuterRest; public Vector3 AxisInner, AxisOuter; public float Sign = 1f; }

        const float BeakClosedDeg = 26f;
        Wing[] wings;
        Transform beak, beakTip;
        Quaternion beakRest;
        Vector3 beakAxis;
        float beakSign = 1f, open, beat, nextScreech, screechT = -1f;
        CreatureCombat combat;
        Vector3 lastPos;
        float moving;

        void Start()
        {
            combat = GetComponent<CreatureCombat>();
            Transform l1 = null, l2 = null, r1 = null, r2 = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
                switch (t.name)
                {
                    case "Pinion_L1": l1 = t; break;
                    case "Pinion_L2": l2 = t; break;
                    case "Pinion_R1": r1 = t; break;
                    case "Pinion_R2": r2 = t; break;
                    case "Beak": beak = t; break;
                    case "BeakTip": beakTip = t; break;
                }
            lastPos = transform.position;
            wings = new[] { MakeWing(l1, l2), MakeWing(r1, r2) };
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

        Wing MakeWing(Transform inner, Transform outer)
        {
            if (inner == null) return null;
            var w = new Wing { Inner = inner, Outer = outer, InnerRest = inner.localRotation, AxisInner = inner.InverseTransformDirection(transform.forward) };
            if (outer != null) { w.OuterRest = outer.localRotation; w.AxisOuter = outer.InverseTransformDirection(transform.forward); }
            if (outer != null)
            {
                float y = outer.position.y;
                inner.localRotation = w.InnerRest * Quaternion.AngleAxis(10f, w.AxisInner);
                w.Sign = outer.position.y > y ? 1f : -1f;   // which way lifts the wing tip
                inner.localRotation = w.InnerRest;
            }
            return w;
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f), t = Time.time;
            float speed = (transform.position - lastPos).magnitude / dt; lastPos = transform.position;
            moving = Mathf.MoveTowards(moving, Mathf.Clamp01(speed / 1.5f), dt * 4f);
            bool busy = combat != null && combat.Busy;
            beat = Mathf.MoveTowards(beat, busy ? 1f : 0f, dt * 4f);

            // wings: a slow breathing flutter at rest, bigger when walking, hard beats in a fight
            float amp = 3f + 6f * moving + 32f * beat;
            float f = 0.6f + 0.8f * moving + 2.4f * beat;
            float w = 6.2832f * f * t;
            if (wings != null)
                foreach (var wing in wings)
                {
                    if (wing == null) continue;
                    wing.Inner.localRotation = wing.InnerRest * Quaternion.AngleAxis(wing.Sign * Mathf.Sin(w) * amp, wing.AxisInner);
                    if (wing.Outer != null)
                        wing.Outer.localRotation = wing.OuterRest * Quaternion.AngleAxis(wing.Sign * Mathf.Sin(w - 0.7f) * amp * 1.4f, wing.AxisOuter);
                }

            // beak: shut at rest, open and trembling while screeching or fighting
            if (beak != null && beakTip != null)
            {
                if (screechT < 0f && !busy && t > nextScreech) { screechT = 0f; nextScreech = t + Random.Range(7f, 12f); }
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
