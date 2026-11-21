using Evolve.Abilities;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// Life and trinkets for the Meshy Cáo Gió (the prefab named "CaoGio (Meshy)"), driven from its rig bones
    /// (Root, Head, Tail1/2, Leg_*, Jaw + JawTip, Eye_L/R): a livelier tail wag that flares up when a move is cast, a jaw that opens
    /// during moves and now and then yawns, eyelids that blink, and gold and sky-blue trinkets: a brow gem, a wind halo behind the head,
    /// anklets and a tail ring. Hinge directions are measured at start, so no bone axis is assumed. Runs after the other animation (LateUpdate).
    /// </summary>
    public class FoxRigFx : MonoBehaviour
    {
        public static bool Has(string name) => !string.IsNullOrEmpty(name) && name.Contains("(Meshy)");

        public static FoxRigFx Attach(GameObject creature) =>
            creature == null || creature.GetComponent<FoxRigFx>() != null ? null : creature.AddComponent<FoxRigFx>();

        static readonly Color Sky = new Color(0.58f, 0.8f, 1f), Gold = new Color(1f, 0.82f, 0.3f);

        Transform jaw, jawTip, tail1, tail2, halo;
        Quaternion jawRest, t1Rest, t2Rest;
        Vector3 jawAxis, yawAxis1, yawAxis2, pitchAxis1;
        float jawSign = 1f, pitchSign = 1f, jawOpen, cast;
        float nextYawn, yawnT = -1f, nextBlink, blinkT = -1f;
        Transform lidL, lidR;
        CreatureCombat combat;
        Vector3 lastPos;
        float moving;

        void Start()
        {
            combat = GetComponent<CreatureCombat>();
            Transform root = null, head = null, eyeL = null, eyeR = null;
            var legs = new System.Collections.Generic.List<Transform>();
            foreach (var t in GetComponentsInChildren<Transform>(true))
                switch (t.name)
                {
                    case "Root": root = t; break;
                    case "Head": head = t; break;
                    case "Jaw": jaw = t; break;
                    case "JawTip": jawTip = t; break;
                    case "Tail1": tail1 = t; break;
                    case "Tail2": tail2 = t; break;
                    case "Eye_L": eyeL = t; break;
                    case "Eye_R": eyeR = t; break;
                    default: if (t.name.StartsWith("Leg_")) legs.Add(t); break;
                }
            if (head == null) return;   // not the rigged model: nothing to drive
            lastPos = transform.position;

            var rb = GetComponentsInChildren<Renderer>();
            float H = 1f;
            if (rb.Length > 0) { var b = rb[0].bounds; foreach (var r in rb) b.Encapsulate(r.bounds); H = Mathf.Max(0.3f, b.size.y); }

            Vector3 up = Vector3.up, right = transform.right;
            if (jaw != null && jawTip != null) { jawRest = jaw.localRotation; jawAxis = jaw.InverseTransformDirection(right); jawSign = SignThatLowers(jaw, jawRest, jawAxis, jawTip); }
            if (tail1 != null) { t1Rest = tail1.localRotation; yawAxis1 = tail1.InverseTransformDirection(up); pitchAxis1 = tail1.InverseTransformDirection(right); }
            if (tail2 != null) { t2Rest = tail2.localRotation; yawAxis2 = tail2.InverseTransformDirection(up); }
            if (tail1 != null && tail2 != null)
            {
                float before = tail2.position.y;
                tail1.localRotation = t1Rest * Quaternion.AngleAxis(15f, pitchAxis1);
                pitchSign = tail2.position.y > before ? 1f : -1f;   // which way lifts the tail
                tail1.localRotation = t1Rest;
            }

            var skyMat = SkillFx.Gel(Sky, 1f, 0.8f, 2.4f);
            var goldMat = SkillFx.Gel(Gold, 1f, 0.3f, 1.4f);
            Vector3 fwd = root != null ? Vector3.ProjectOnPlane(head.position - root.position, up) : transform.forward;
            if (fwd.sqrMagnitude < 1e-4f) fwd = transform.forward;
            fwd.Normalize();
            Vector3 brow = eyeL != null && eyeR != null ? (eyeL.position + eyeR.position) * 0.5f : head.position + fwd * 0.2f * H + up * 0.2f * H;
            Quaternion faceRing = Quaternion.FromToRotation(Vector3.up, fwd);   // Torus lies round Y: turn its axis to face forward

            // brow gem in a gold ring
            Place(Sphere(skyMat), "Ngọc Trán", head, brow + up * 0.05f * H + fwd * 0.01f * H, Quaternion.identity, Vector3.one * 0.05f * H);
            Place(Ring(goldMat, 0.032f * H, 0.006f * H), "Khung Ngọc", head, brow + up * 0.05f * H + fwd * 0.01f * H, faceRing, Vector3.one);
            // wind halo standing behind the head, with three gold beads turning with it
            Vector3 hc = brow - fwd * 0.2f * H + up * 0.1f * H;
            halo = Place(Ring(skyMat, 0.3f * H, 0.012f * H), "Vòng Gió", head, hc, faceRing, Vector3.one);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.0944f;
                var bead = Place(Sphere(goldMat), "Hạt Vòng", halo, hc + (up * Mathf.Sin(a) + Vector3.Cross(up, fwd).normalized * Mathf.Cos(a)) * 0.3f * H, Quaternion.identity, Vector3.one * 0.03f * H);
            }
            // anklets
            float ground = transform.position.y;
            foreach (var leg in legs)
                Place(Ring(goldMat, 0.04f * H, 0.01f * H), "Vòng Chân", leg, new Vector3(leg.position.x, ground + 0.12f * H, leg.position.z), Quaternion.identity, Vector3.one);
            // tail ring
            if (tail2 != null) Place(Ring(goldMat, 0.08f * H, 0.014f * H), "Vòng Đuôi", tail2, tail2.position, faceRing, Vector3.one);

            // eyelids: fur-coloured blobs shown for a moment to blink
            var lidMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = new Color(0.62f, 0.66f, 0.72f) };
            if (eyeL != null) lidL = Lid(eyeL, lidMat, 0.034f * H);
            if (eyeR != null) lidR = Lid(eyeR, lidMat, 0.034f * H);
            nextBlink = Time.time + Random.Range(1.5f, 4f);
            nextYawn = Time.time + Random.Range(8f, 14f);
        }

        float SignThatLowers(Transform bone, Quaternion rest, Vector3 axis, Transform tip)
        {
            float y = tip.position.y;
            bone.localRotation = rest * Quaternion.AngleAxis(10f, axis);
            float s = tip.position.y < y ? 1f : -1f;
            bone.localRotation = rest;
            return s;
        }

        static GameObject Sphere(Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        static GameObject Ring(Material m, float major, float minor)
        {
            var go = new GameObject();
            go.AddComponent<MeshFilter>().sharedMesh = LordRegalia.ToMesh(RegaliaMeshes.Torus(major, minor, 24, 8), false);
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        /// <summary>Put <paramref name="go"/> at a world pose and hang it on a bone so it follows the animation.</summary>
        static Transform Place(GameObject go, string name, Transform bone, Vector3 worldPos, Quaternion worldRot, Vector3 worldScale)
        {
            go.name = name;
            go.transform.SetPositionAndRotation(worldPos, worldRot);
            go.transform.localScale = worldScale;
            go.transform.SetParent(bone, true);
            return go.transform;
        }

        Transform Lid(Transform eye, Material m, float radius)
        {
            var go = Sphere(m);
            go.name = "Mi Mắt";
            go.transform.SetParent(eye, false);
            go.transform.localPosition = Vector3.zero;
            float s = eye.lossyScale.x > 1e-5f ? eye.lossyScale.x : 1f;
            go.transform.localScale = Vector3.one * (radius * 2f / s);
            go.SetActive(false);
            return go.transform;
        }

        void LateUpdate()
        {
            if (tail1 == null && jaw == null) return;
            float dt = Mathf.Max(Time.deltaTime, 1e-4f), t = Time.time;
            float speed = (transform.position - lastPos).magnitude / dt; lastPos = transform.position;
            moving = Mathf.MoveTowards(moving, Mathf.Clamp01(speed / 1.5f), dt * 4f);
            bool busy = combat != null && combat.Busy;
            cast = Mathf.MoveTowards(cast, busy ? 1f : 0f, dt * 5f);

            if (tail1 != null)
            {
                float f = 1.4f + 1.8f * moving + 1.2f * cast;
                float amp = 16f + 14f * moving + 10f * cast;
                float w = 6.2832f * f * t;
                tail1.localRotation = t1Rest * Quaternion.AngleAxis(Mathf.Sin(w) * amp, yawAxis1) * Quaternion.AngleAxis(pitchSign * 28f * cast, pitchAxis1);
                if (tail2 != null) tail2.localRotation = t2Rest * Quaternion.AngleAxis(Mathf.Sin(w - 0.9f) * amp * 1.5f, yawAxis2);
            }

            if (jaw != null)
            {
                if (yawnT < 0f && !busy && moving < 0.2f && t > nextYawn) { yawnT = 0f; nextYawn = t + Random.Range(9f, 16f); }
                float yawn = 0f;
                if (yawnT >= 0f) { yawnT += dt; yawn = Mathf.Sin(Mathf.Clamp01(yawnT / 1.1f) * Mathf.PI); if (yawnT > 1.1f) yawnT = -1f; }
                float target = Mathf.Max(busy ? 1f : 0f, yawn * 0.8f);
                jawOpen = Mathf.MoveTowards(jawOpen, target, dt * 7f);
                jaw.localRotation = jawRest * Quaternion.AngleAxis(jawSign * 26f * jawOpen, jawAxis);
            }

            if (lidL != null || lidR != null)
            {
                if (blinkT < 0f && t > nextBlink) { blinkT = 0f; nextBlink = t + Random.Range(2.5f, 5.5f); }
                bool shut = false;
                if (blinkT >= 0f) { blinkT += dt; shut = blinkT < 0.13f; if (blinkT > 0.13f) blinkT = -1f; }
                if (lidL != null) lidL.gameObject.SetActive(shut);
                if (lidR != null) lidR.gameObject.SetActive(shut);
            }

            if (halo != null) halo.Rotate(0f, 40f * dt, 0f, Space.Self);   // Torus axis is local Y
        }
    }
}
