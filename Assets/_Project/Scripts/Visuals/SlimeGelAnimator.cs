using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Visuals
{
    /// <summary>Motions the gel body can play on top of its constant breathing and crawling.</summary>
    public enum GelMotion { Absorb, Attack, Release, Dash }

    /// <summary>
    /// Soft-body animation of the slime, done by bending the body mesh in code (no skeleton):
    /// breathing, a crawling wave while moving, and short motions for absorbing, attacking and using skills.
    /// Sits on the slime's "Visual" object (added by SlimeGelCore). Works for remote players too: movement is
    /// read from how the object actually moves. Other scripts call <see cref="Play(GameObject, GelMotion)"/>.
    /// All amplitudes are small on purpose, so the organs sitting on the body never poke through.
    /// </summary>
    public class SlimeGelAnimator : MonoBehaviour
    {
        const float AbsorbTime = 0.9f, AttackTime = 0.45f, ReleaseTime = 0.8f, DashTime = 0.6f;

        MeshFilter filter;
        Mesh original, mesh;
        Vector3[] baseVertices, work;
        SlimeGelCore core;
        readonly List<MeshFilter> scratch = new List<MeshFilter>();

        /// <summary>An organ mesh that bends with the body: its own copy of the mesh plus the original vertices.</summary>
        sealed class Follower
        {
            public MeshFilter Filter;
            public Mesh Mesh;
            public Vector3[] Base, Work;
            public bool Legs;
        }
        readonly Dictionary<MeshFilter, Follower> followers = new Dictionary<MeshFilter, Follower>();
        readonly List<MeshFilter> dead_ = new List<MeshFilter>();

        Vector3 lastPosition;
        float speed;
        Vector3 moveDir;           // in the body's local space (xz)

        GelMotion active;
        float activeTime = -1f;    // seconds since the motion started, <0 = none

        float nextFollowerScan;
        float gaitPhase;
        bool hasLegs;
        float dead;                // 0..1 how collapsed the body is (stats are looked up lazily)
        Survival.SurvivalStats stats;

        /// <summary>Play a motion on the slime that owns this object (a player or a remote avatar).</summary>
        public static void Play(GameObject slime, GelMotion motion)
        {
            if (slime == null) return;
            var animator = slime.GetComponentInChildren<SlimeGelAnimator>();
            if (animator != null) animator.Play(motion);
        }

        public void Play(GelMotion motion)
        {
            // The same motion is not restarted until it is mostly done (contact damage fires every frame).
            if (activeTime >= 0f && active == motion && activeTime < Duration(motion) * 0.6f) return;
            active = motion;
            activeTime = 0f;
        }

        static float Duration(GelMotion m) => m == GelMotion.Absorb ? AbsorbTime : m == GelMotion.Attack ? AttackTime
            : m == GelMotion.Release ? ReleaseTime : DashTime;

        void Awake()
        {
            filter = GetComponent<MeshFilter>();
            core = GetComponent<SlimeGelCore>();
            if (filter == null || filter.sharedMesh == null) { enabled = false; return; }
            original = filter.sharedMesh;
            mesh = Instantiate(original);
            mesh.name = original.name + " (animated)";
            mesh.MarkDynamic();
            filter.sharedMesh = mesh;
            baseVertices = mesh.vertices;
            work = new Vector3[baseVertices.Length];
            lastPosition = transform.position;
        }

        void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            foreach (var f in followers.Values) if (f.Mesh != null) Destroy(f.Mesh);
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);

            // Movement as it really happens in the world (flat), expressed in the body's own directions.
            Vector3 delta = transform.position - lastPosition;
            lastPosition = transform.position;
            delta.y = 0f;
            Vector3 velocity = delta / dt;
            speed = Mathf.Lerp(speed, Mathf.Min(velocity.magnitude, 12f), 1f - Mathf.Exp(-10f * dt));
            if (velocity.sqrMagnitude > 0.04f)
            {
                Vector3 local = transform.InverseTransformDirection(velocity.normalized);
                local.y = 0f;
                if (local.sqrMagnitude > 1e-4f) moveDir = Vector3.Slerp(moveDir.sqrMagnitude < 1e-4f ? local.normalized : moveDir, local.normalized, 1f - Mathf.Exp(-12f * dt));
            }

            if (speed > 0.3f) gaitPhase += dt * (5f + speed * 1.6f);

            if (activeTime >= 0f)
            {
                activeTime += dt;
                if (activeTime >= Duration(active)) activeTime = -1f;
            }

            if (stats == null) stats = GetComponentInParent<Survival.SurvivalStats>();
            float deadTarget = stats != null && stats.State.IsDead ? 1f : 0f;
            dead = Mathf.MoveTowards(dead, deadTarget, dt * 2.5f);

            if (Time.time >= nextFollowerScan) { nextFollowerScan = Time.time + 0.4f; ScanFollowers(); UpdateHasLegs(); }

            Deform();
            if (core != null) core.Boost = CoreBoost();
        }

        // Every mesh on an organ (they all sit under an OrganSocket) bends with the body, so nothing stays stiff.
        void ScanFollowers()
        {
            dead_.Clear();
            foreach (var kv in followers)
                if (kv.Key == null) dead_.Add(kv.Key);
            foreach (var key in dead_)
            {
                if (followers[key].Mesh != null) Destroy(followers[key].Mesh);
                followers.Remove(key);
            }

            foreach (var socket in GetComponentsInChildren<Evolve.Organs.OrganSocket>(true))
            {
                socket.GetComponentsInChildren(true, scratch);
                foreach (var f in scratch)
                {
                    if (f == null || f.sharedMesh == null || followers.ContainsKey(f)) continue;
                    var copy = Instantiate(f.sharedMesh);
                    copy.name = f.sharedMesh.name + " (animated)";
                    copy.MarkDynamic();
                    f.sharedMesh = copy;
                    var verts = copy.vertices;
                    followers[f] = new Follower { Filter = f, Mesh = copy, Base = verts, Work = new Vector3[verts.Length], Legs = socket.Slot == Evolve.Organs.OrganSlot.Legs };
                }
            }
        }

        void UpdateHasLegs()
        {
            hasLegs = false;
            foreach (var f in followers.Values) if (f.Legs && f.Filter != null) { hasLegs = true; break; }
        }

        float CoreBoost()
        {
            if (activeTime < 0f) return 0f;
            float p = activeTime / Duration(active);
            switch (active)
            {
                case GelMotion.Absorb: return Mathf.Sin(p * Mathf.PI);
                case GelMotion.Release: return p < 0.45f ? p / 0.45f : Mathf.Max(0f, 1f - (p - 0.45f) / 0.4f);
                case GelMotion.Attack: return 0.4f * Mathf.Sin(p * Mathf.PI);
                default: return 0.6f * Mathf.Sin(p * Mathf.PI);
            }
        }

        // per-frame motion parameters shared by the body and the organs
        float fTime, fMove, fAbsorb, fAbsorbPulse, fKick, fStretch, fCharge, fBurst;
        Vector3 fDir;

        void PrepareFrame()
        {
            fTime = Time.time;
            fMove = Mathf.Clamp01(speed / 4f);
            fDir = moveDir;
            fAbsorb = fKick = fStretch = fCharge = fBurst = 0f;
            float p = activeTime >= 0f ? activeTime / Duration(active) : -1f;
            if (p >= 0f)
            {
                switch (active)
                {
                    case GelMotion.Absorb: fAbsorb = p; break;
                    case GelMotion.Attack: Lunge(p, 1f, out fKick, out fStretch); break;
                    case GelMotion.Dash: Lunge(p, 1.6f, out fKick, out fStretch); break;
                    case GelMotion.Release:
                        fCharge = p < 0.45f ? Mathf.SmoothStep(0f, 1f, p / 0.45f) : 0f;
                        fBurst = p < 0.45f ? 0f : Mathf.Sin((p - 0.45f) / 0.55f * Mathf.PI) * (1f - (p - 0.45f) / 0.55f * 0.3f);
                        break;
                }
            }
            fAbsorbPulse = fAbsorb > 0f ? Mathf.Sin(fAbsorb * Mathf.PI) : 0f;
        }

        void Deform()
        {
            PrepareFrame();

            for (int i = 0; i < baseVertices.Length; i++) work[i] = baseVertices[i] + Offset(baseVertices[i]);
            mesh.vertices = work;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Organs: the same displacement field, evaluated where each organ vertex sits relative to the body.
            Matrix4x4 worldToBody = transform.worldToLocalMatrix;
            foreach (var f in followers.Values)
            {
                if (f.Filter == null) continue;
                Matrix4x4 toBody = worldToBody * f.Filter.transform.localToWorldMatrix;
                Matrix4x4 toLocal = toBody.inverse;
                for (int i = 0; i < f.Base.Length; i++)
                {
                    Vector3 bodyPoint = toBody.MultiplyPoint3x4(f.Base[i]);
                    Vector3 off = Offset(bodyPoint);
                    if (f.Legs) off += Gait(bodyPoint);
                    f.Work[i] = f.Base[i] + toLocal.MultiplyVector(off);
                }
                f.Mesh.vertices = f.Work;
                f.Mesh.RecalculateNormals();
                f.Mesh.RecalculateBounds();
            }
        }

        /// <summary>How far a point of the body (or of an organ, in the body's local space) is pushed by the current motion.</summary>
        Vector3 Offset(Vector3 b)
        {
            float t = fTime;
            float h = Mathf.Clamp01((b.y - SlimeMeshBuilder.Bottom) / (SlimeMeshBuilder.Top - SlimeMeshBuilder.Bottom));
            float rx = Mathf.Sqrt(b.x * b.x + b.z * b.z);
            float rn = Mathf.Clamp01(rx / 0.65f);
            Vector3 outward = rx > 1e-4f ? new Vector3(b.x / rx, 0f, b.z / rx) : Vector3.zero;
            float low = 1f - h;
            Vector3 o = Vector3.zero;

            // Breathing: the hump rises and sinks slightly.
            o.y += 0.02f * Mathf.Sin(t * 2.1f) * h;
            o += outward * 0.01f * Mathf.Sin(t * 2.1f + 1f) * low;

            // Crawling: a wave runs along the body, the hump leans where it is going.
            if (fMove > 0.01f && fDir.sqrMagnitude > 1e-4f)
            {
                float along = b.x * fDir.x + b.z * fDir.z;
                float crawl = hasLegs ? 0.25f : 1f;     // with legs the legs do the moving, the body only sways
                float fast = 1f + speed * 0.15f;        // faster = bigger, quicker ripples
                float wave = Mathf.Sin(along * 11f - t * (9f + speed * 1.5f));
                o.y += fMove * 0.07f * fast * crawl * wave * (0.3f + 0.7f * h);
                o += outward * fMove * 0.06f * fast * crawl * wave * low;
                o += fDir * (fMove * 0.16f * fast * h * h);
                if (hasLegs) o.y += fMove * 0.035f * Mathf.Sin(gaitPhase * 2f) * h;   // body bobs with each step
            }

            // Absorbing: a ring travels from the rim toward the core while the body gulps inward.
            if (fAbsorb > 0f)
            {
                float ring = Mathf.Exp(-Mathf.Pow(rn - (1f - fAbsorb), 2f) / 0.06f) * fAbsorbPulse;
                o.y -= 0.11f * ring;
                o -= outward * 0.11f * ring;
                o -= outward * 0.07f * fAbsorbPulse * rn;
                o.y += 0.07f * fAbsorbPulse * h;
            }

            // Attacking / dashing: pull back, then the front lunges forward.
            if (fKick != 0f || fStretch != 0f)
            {
                float front = 0.5f + 0.5f * Mathf.Clamp(b.z / 0.55f, -1f, 1f);
                o.z += fKick * (0.35f + 0.65f * h) * 0.5f;
                o.z += fStretch * front * 0.3f;
                o.y -= Mathf.Abs(fKick) * 0.2f * h;
            }

            // Skill: tighten and tremble while charging, then a bristling burst outward.
            if (fCharge > 0f)
            {
                o -= outward * 0.1f * fCharge * rn;
                o.y += 0.12f * fCharge * h;
                o += new Vector3(Mathf.Sin(t * 70f + b.z * 20f), 0f, Mathf.Cos(t * 63f + b.x * 20f)) * 0.008f * fCharge;
            }
            if (fBurst > 0f)
            {
                o += outward * 0.17f * fBurst * (0.4f + 0.6f * low);
                o.y -= 0.1f * fBurst * h;
            }

            // Dead: the body slumps into a flat puddle.
            if (dead > 0f)
            {
                o.y -= (b.y - SlimeMeshBuilder.Bottom) * 0.6f * dead;
                o += outward * 0.07f * dead * rn;
            }
            return o;
        }

        /// <summary>
        /// Walking with legs: diagonal pairs of legs alternate. The foot (far from the body centre) swings along the
        /// direction of travel and lifts while it swings; near the shoulder almost nothing moves.
        /// </summary>
        Vector3 Gait(Vector3 b)
        {
            if (fMove < 0.01f) return Vector3.zero;
            float sx = b.x >= 0f ? 1f : -1f, sz = b.z >= 0f ? 1f : -1f;
            float phase = gaitPhase + (sx * sz > 0f ? 0f : Mathf.PI);
            float reach = Mathf.Clamp01((Mathf.Sqrt(b.x * b.x + b.z * b.z) - 0.1f) / 0.22f);
            float lift = Mathf.Max(0f, Mathf.Cos(phase));
            Vector3 o = fDir * (Mathf.Sin(phase) * 0.16f * fMove * reach);
            o.y += lift * 0.13f * fMove * reach;
            return o;
        }

        /// <summary>Kick curve of a lunge: pulls back first (negative), then shoots forward, then settles.</summary>
        static void Lunge(float p, float power, out float kick, out float stretch)
        {
            if (p < 0.3f) { kick = -0.35f * Mathf.Sin(p / 0.3f * Mathf.PI * 0.5f); stretch = 0f; }
            else if (p < 0.55f)
            {
                float q = (p - 0.3f) / 0.25f;
                kick = Mathf.Lerp(-0.35f, 0.9f * power, Mathf.SmoothStep(0f, 1f, q));
                stretch = Mathf.SmoothStep(0f, 1f, q) * power;
            }
            else
            {
                float q = (p - 0.55f) / 0.45f;
                kick = Mathf.Lerp(0.9f * power, 0f, Mathf.SmoothStep(0f, 1f, q));
                stretch = Mathf.Lerp(power, 0f, Mathf.SmoothStep(0f, 1f, q));
            }
        }
    }
}
