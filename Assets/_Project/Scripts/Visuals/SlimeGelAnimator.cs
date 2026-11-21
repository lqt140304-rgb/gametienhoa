using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Visuals
{
    /// <summary>Motions the gel body can play on top of its constant breathing and crawling.</summary>
    public enum GelMotion { Absorb, Gulp, Attack, Release, Dash }

    /// <summary>
    /// Soft-body animation of the slime, done by bending the body mesh in code (no skeleton):
    /// breathing, a crawling wave while moving, and short motions for absorbing, attacking and using skills.
    /// Sits on the slime's "Visual" object (added by SlimeGelCore). Works for remote players too: movement is
    /// read from how the object actually moves. Other scripts call <see cref="Play(GameObject, GelMotion)"/>.
    /// All amplitudes are small on purpose, so the organs sitting on the body never poke through.
    /// </summary>
    public class SlimeGelAnimator : MonoBehaviour
    {
        const float AbsorbTime = 1.3f, GulpTime = 0.5f, AttackTime = 0.5f, ReleaseTime = 0.65f, DashTime = 0.5f;
        const float ReleaseWind = 0.3f;   // seconds of charging before a skill is released
        const float DashWind = 0.15f;     // pull-back before a dash lunges (0.3 of DashTime)

        /// <summary>Seconds the body needs to wind up before a skill of this motion should leave.</summary>
        public static float WindUp(GelMotion motion) => motion == GelMotion.Release ? ReleaseWind : motion == GelMotion.Dash ? DashWind : 0f;

        /// <summary>Time spent bending meshes since the last read (for the F4 performance overlay).</summary>
        public static long CostTicks;

        /// <summary>Test switch (F4, third press): all body animation off, to see whether a stutter comes from it.</summary>
        public static bool Paused;
        bool wasPaused;

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
        Rigidbody body;
        Evolve.Organs.OrganHost host;

        void OnOrganEvent(Evolve.Organs.OrganSO organ) => nextFollowerScan = 0f;
        void OnOrganChanged() => nextFollowerScan = 0f;
        float speed;
        Vector3 moveDir;           // in the body's local space (xz)

        GelMotion active;
        float activeTime = -1f;    // seconds since the motion started, <0 = none

        float nextFollowerScan;
        float gaitPhase;
        // Inertia: the body lags behind when speeding up, stopping or turning (a damped spring, so it overshoots a little).
        Vector3 prevVelocity, accelSmooth, leanPos, leanVel;
        // Landing splat: -1 = none
        float landT = -1f, landStrength, fLand;
        float jig, jigV;       // damped wobble after landings and after a motion ends
        bool hasLegs;
        float dead;                // 0..1 how collapsed the body is (stats are looked up lazily)
        Survival.SurvivalStats stats;

        /// <summary>
        /// A limb the body pulls out of its own gel (tongue, stinger, foot): the part of the body that faces <see cref="Target"/> stretches toward it,
        /// so the limb is one smooth piece with the body, no seam. Reach 0..1 is how far it has gone; Width (degrees) how big a patch of the body moves;
        /// Sharp above 1 makes a pointed spike, near 1 a blunt nub; Squeeze thins the limb's neck.
        /// </summary>
        public sealed class Limb
        {
            public Vector3 Target;
            /// <summary>True: Target is in the body's own space (legs, back spikes that move with it); false: a point in the world.</summary>
            public bool Local;
            public float Reach, Width = 30f, Sharp = 5f, Squeeze = 0.6f;
            internal SlimeGelAnimator Owner;
            internal Vector3 Dir;
            internal float Length, CosWidth;
            public void Remove() { if (Owner != null) Owner.limbs.Remove(this); Owner = null; }
        }
        readonly List<Limb> limbs = new List<Limb>();
        /// <summary>Where limbs grow from, in the body's own space (about the middle of the dome).</summary>
        public static readonly Vector3 LimbCentre = new Vector3(0f, -0.1f, 0f);
        /// <summary>About how far the body's surface is from LimbCentre; a limb's length is counted from there.</summary>
        public const float SurfaceRadius = 0.5f;
        float tuckUntil, fTuck;
        internal float LatchFlatten { get; set; }

        /// <summary>Start a limb on this slime (null if it has no animated body). Call Remove when done.</summary>
        public static Limb AddLimb(GameObject slime)
        {
            var a = slime != null ? slime.GetComponentInChildren<SlimeGelAnimator>() : null;
            if (a == null) return null;
            var l = new Limb { Owner = a };
            a.limbs.Add(l);
            return l;
        }

        /// <summary>Thu Mình: the body draws itself in, smaller and tighter, for a while.</summary>
        public static void Tuck(GameObject slime, float seconds)
        {
            var a = slime != null ? slime.GetComponentInChildren<SlimeGelAnimator>() : null;
            if (a != null) a.tuckUntil = Time.time + seconds;
        }

        /// <summary>Play a motion on the slime that owns this object (a player or a remote avatar).</summary>
        public static void Play(GameObject slime, GelMotion motion)
        {
            if (slime == null) return;
            var animator = slime.GetComponentInChildren<SlimeGelAnimator>();
            if (animator != null) animator.Play(motion);
        }

        /// <summary>A jelly wobble: the body keeps bouncing a little and settles (call after landing, hits, ...).</summary>
        /// <summary>True while an action (skill, attack, absorb) plays; everyday movement animation steps aside then.</summary>
        public bool ActionPlaying => !Paused && ( (activeTime >= 0f && active != GelMotion.Gulp) || (landT >= 0f && landT < LandRest));

        /// <summary>After a real landing the body rests this long (landing splat plays alone) before moving animation resumes.</summary>
        const float LandRest = 0.5f;

        /// <summary>The body splats flat when it lands (strength about 0.3 soft .. 1.2 hard) and rebounds.</summary>
        public void Land(float strength)
        {
            landT = 0f;
            landStrength = Mathf.Clamp(strength, 0.2f, 1.3f);
        }

        public void Jiggle(float strength) => jigV += strength;

        public void Play(GelMotion motion)
        {
            // A motion is never restarted while it plays (contact damage and eating fire every frame), and a small
            // gulp never interrupts a bigger motion.
            if (activeTime >= 0f && (active == motion || motion == GelMotion.Gulp)) return;
            active = motion;
            activeTime = 0f;
        }

        static float Duration(GelMotion m) => m == GelMotion.Absorb ? AbsorbTime : m == GelMotion.Gulp ? GulpTime
            : m == GelMotion.Attack ? AttackTime : m == GelMotion.Release ? ReleaseTime : DashTime;

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
            body = GetComponentInParent<Rigidbody>();
            // A newly equipped organ must start bending (and be coloured) the very frame it appears,
            // not up to 0.4 s later: that delay was a visible pop.
            host = GetComponentInParent<Evolve.Organs.OrganHost>();
            if (host != null) { host.Attached += OnOrganEvent; host.Removed += OnOrganEvent; host.Changed += OnOrganChanged; }
        }

        // Back to the undeformed shapes (body and organs).
        void RestShapes()
        {
            if (mesh != null) { mesh.SetVertices(baseVertices); mesh.RecalculateNormals(); mesh.RecalculateBounds(); }
            foreach (var f in followers.Values) if (f.Mesh != null) { f.Mesh.SetVertices(f.Base); f.Mesh.RecalculateBounds(); }
        }

        void OnDestroy()
        {
            if (host != null) { host.Attached -= OnOrganEvent; host.Removed -= OnOrganEvent; host.Changed -= OnOrganChanged; }
            if (mesh != null) Destroy(mesh);
            foreach (var f in followers.Values) if (f.Mesh != null) Destroy(f.Mesh);
        }

        void LateUpdate()
        {
            if (Paused)
            {
                if (!wasPaused) { wasPaused = true; RestShapes(); }
                return;
            }
            wasPaused = false;
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);

            // Movement as it really happens in the world (flat), expressed in the body's own directions.
            // The local player's physics velocity is smooth; others (remote avatars) are measured from how they move.
            Vector3 velocity;
            if (body != null) velocity = body.linearVelocity;
            else velocity = (transform.position - lastPosition) / dt;
            lastPosition = transform.position;
            velocity.y = 0f;
            speed = Mathf.Lerp(speed, Mathf.Min(velocity.magnitude, 12f), 1f - Mathf.Exp(-10f * dt));
            if (velocity.sqrMagnitude > 0.04f)
            {
                Vector3 local = transform.InverseTransformDirection(velocity.normalized);
                local.y = 0f;
                if (local.sqrMagnitude > 1e-4f) moveDir = Vector3.Slerp(moveDir.sqrMagnitude < 1e-4f ? local.normalized : moveDir, local.normalized, 1f - Mathf.Exp(-12f * dt));
            }

            // Inertia from the acceleration (sprinting off, stopping, changing direction).
            Vector3 accel = (velocity - prevVelocity) / dt;
            prevVelocity = velocity;
            accelSmooth = Vector3.Lerp(accelSmooth, accel, 1f - Mathf.Exp(-12f * dt));
            Vector3 accelLocal = transform.InverseTransformDirection(accelSmooth);
            accelLocal.y = 0f;
            Vector3 leanTarget = Vector3.ClampMagnitude(-accelLocal * 0.005f, 0.05f);
            leanVel += ((leanTarget - leanPos) * 55f - leanVel * 11f) * dt;
            leanPos += leanVel * dt;

            if (landT >= 0f) { landT += dt; if (landT > 0.6f) landT = -1f; }

            if (speed > 0.3f) gaitPhase += dt * (5f + speed * 1.6f);

            if (activeTime >= 0f)
            {
                activeTime += dt;
                if (activeTime >= Duration(active)) { activeTime = -1f; if (active != GelMotion.Gulp) jigV += 1f; }
            }
            // Damped spring: the wobble swings back and forth and dies out.
            jigV += (-90f * jig - 7f * jigV) * dt;
            jig += jigV * dt;

            if (stats == null) stats = GetComponentInParent<Survival.SurvivalStats>();
            float deadTarget = stats != null && stats.State.IsDead ? 1f : 0f;
            dead = Mathf.MoveTowards(dead, deadTarget, dt * 2.5f);

            if (Time.time >= nextFollowerScan) { nextFollowerScan = Time.time + 0.4f; ScanFollowers(); UpdateHasLegs(); }

            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            Deform();
            CostTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
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

            GetComponentsInChildren(true, sockets);
            foreach (var socket in sockets)
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
                case GelMotion.Absorb: return 0.6f * Mathf.Sin(p * Mathf.PI);
                case GelMotion.Gulp: return 0.25f * Mathf.Sin(p * Mathf.PI);
                case GelMotion.Release: { float f = ReleaseWind / ReleaseTime; return p < f ? p / f : Mathf.Max(0f, 1f - (p - f) / 0.4f); }
                case GelMotion.Attack: return 0.4f * Mathf.Sin(p * Mathf.PI);
                default: return 0.6f * Mathf.Sin(p * Mathf.PI);
            }
        }

        // per-frame motion parameters shared by the body and the organs
        float fBreath, fBreath2, fWaveTime;
        int frameCount;
        readonly List<Evolve.Organs.OrganSocket> sockets = new List<Evolve.Organs.OrganSocket>();
        float fTime, fMove, fAbsorb, fAbsorbPulse, fKick, fStretch, fCharge, fBurst;
        Vector3 fDir;

        float locoK = 1f;  // 1 = everyday movement animation fully on, 0 = off (an action or a jump has the floor)
        float crawlK = 1f; // 1 = body crawls, 0.25 = legs do the walking (eased, so attaching legs does not pop)

        /// <summary>
        /// Works out the motion parameters for this frame. They are only TARGETS: every value is eased toward its
        /// target, so switching state (idle, moving, absorbing, attacking, charging...) never makes a sudden jump.
        /// </summary>
        void PrepareFrame()
        {
            float dt = Time.deltaTime;
            float soft = 1f - Mathf.Exp(-14f * dt);   // everyday easing (about 70 ms)
            float quick = 1f - Mathf.Exp(-32f * dt);  // for the sharp moments (release, lunge)

            fTime = Time.time;
            fBreath = Mathf.Sin(fTime * 2.1f);
            fBreath2 = Mathf.Sin(fTime * 2.1f + 1f);
            fWaveTime = fTime * (9f + speed * 1.5f);
            fLand = landT >= 0f
                ? Mathf.SmoothStep(0f, 1f, landT / 0.04f) * Mathf.Exp(-6f * landT) * Mathf.Cos(landT * 16f) * landStrength
                : 0f;
            fDir = Vector3.forward; // the body turns to face where it goes: waves and steps always run along its own axis (no flips on turns)
            fMove = Mathf.Lerp(fMove, Mathf.Clamp01(speed / 4f), 1f - Mathf.Exp(-9f * dt));
            // One animation at a time: while an action plays or the slime is in the air, the everyday movement
            // animation (breathing, crawl, steps) fades out, so animations never fight each other.
            bool airborne = body != null && Mathf.Abs(body.linearVelocity.y) > 1.6f; // small bumps (a dash's little lift) do not count
            bool locoOff = ActionPlaying || airborne;
            // Off quickly, back on gently (so the resume after an action or a landing is smooth, not a snap).
            locoK = Mathf.Lerp(locoK, locoOff ? 0f : 1f, 1f - Mathf.Exp(-(locoOff ? 14f : 5f) * dt));
            crawlK = Mathf.Lerp(crawlK, hasLegs ? 0.25f : 1f, 1f - Mathf.Exp(-6f * dt));

            float tPulse = 0f, tKick = 0f, tStretch = 0f, tCharge = 0f, tBurst = 0f;
            float p = activeTime >= 0f ? activeTime / Duration(active) : -1f;
            if (p >= 0f)
            {
                switch (active)
                {
                    case GelMotion.Absorb: fAbsorb = p; tPulse = Mathf.Sin(p * Mathf.PI); break;
                    case GelMotion.Gulp: fAbsorb = p; tPulse = Mathf.Sin(p * Mathf.PI) * 0.35f; break;
                    case GelMotion.Attack: Lunge(p, 1f, out tKick, out tStretch); break;
                    case GelMotion.Dash: Lunge(p, 1.6f, out tKick, out tStretch); break;
                    case GelMotion.Release:
                        float f = ReleaseWind / ReleaseTime;
                        tCharge = p < f ? Mathf.SmoothStep(0f, 1f, p / f) : 0f;
                        if (p >= f)
                        {
                            // The burst peaks right at the moment the skill leaves, then relaxes.
                            float q = (p - f) / (1f - f);
                            tBurst = Mathf.Min(1f, q / 0.12f) * (1f - q) * (1f - q);
                        }
                        break;
                }
            }
            fTuck = Mathf.Lerp(fTuck, Time.time < tuckUntil ? 1f : 0f, soft);
            foreach (var l in limbs)
            {
                Vector3 v = (l.Local ? l.Target : transform.InverseTransformPoint(l.Target)) - LimbCentre;
                float dist = v.magnitude;
                l.Dir = dist > 1e-4f ? v / dist : Vector3.forward;
                l.Length = Mathf.Max(0f, dist - SurfaceRadius) * Mathf.Clamp01(l.Reach);
                l.CosWidth = Mathf.Cos(l.Width * Mathf.Deg2Rad);
            }

            fAbsorbPulse = Mathf.Lerp(fAbsorbPulse, tPulse, soft);
            fKick = Mathf.Lerp(fKick, tKick, quick);
            fStretch = Mathf.Lerp(fStretch, tStretch, quick);
            fCharge = Mathf.Lerp(fCharge, tCharge, soft);
            fBurst = Mathf.Lerp(fBurst, tBurst, quick);
        }

        void Deform()
        {
            PrepareFrame();
            frameCount++;

            // Standing still (only breathing): every second frame is plenty.
            bool still = fMove * locoK < 0.02f && activeTime < 0f && Mathf.Abs(fLand) < 0.002f
                && Mathf.Abs(jig) < 0.002f && leanPos.sqrMagnitude < 4e-6f && dead < 0.01f && limbs.Count == 0 && fTuck < 0.001f && LatchFlatten < 0.001f;
            if (still && (frameCount & 1) == 0) return;

            for (int i = 0; i < baseVertices.Length; i++) work[i] = baseVertices[i] + Offset(baseVertices[i]);
            mesh.SetVertices(work);
            if ((frameCount & 1) == 0) mesh.RecalculateNormals(); // lighting a frame late is not visible
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
                f.Mesh.SetVertices(f.Work);
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
            o.y += 0.02f * locoK * fBreath * h;
            o += outward * 0.01f * locoK * fBreath2 * low;

            // Crawling: a wave runs along the body, the hump leans where it is going.
            if (fMove * locoK > 0.01f && fDir.sqrMagnitude > 1e-4f)
            {
                float along = b.x * fDir.x + b.z * fDir.z;
                float crawl = crawlK * locoK;                   // with legs the legs do the moving, the body only sways
                float fast = 1f + Mathf.Min(speed, 6f) * 0.1f;        // faster = bigger, quicker ripples
                float wave = Mathf.Sin(along * 11f - fWaveTime);
                o.y += fMove * 0.07f * fast * crawl * wave * (0.3f + 0.7f * h);
                o += outward * fMove * 0.06f * fast * crawl * wave * low;
                o += fDir * (fMove * locoK * 0.16f * fast * h * h);
                if (hasLegs) o.y += fMove * locoK * 0.035f * Mathf.Sin(gaitPhase * 2f) * h;   // body bobs with each step
            }

            // Absorbing: a ring travels from the rim toward the core while the body gulps inward.
            if (fAbsorbPulse > 0.001f)
            {
                float ring = Mathf.Exp(-Mathf.Pow(rn - (1f - fAbsorb), 2f) / 0.1f) * fAbsorbPulse;
                o.y -= 0.05f * ring;
                o -= outward * 0.045f * ring;
                o -= outward * 0.03f * fAbsorbPulse * rn;
                o.y += 0.03f * fAbsorbPulse * h;
            }

            // Attacking / dashing: pull back, then the front lunges forward.
            if (Mathf.Abs(fKick) > 0.001f || Mathf.Abs(fStretch) > 0.001f)
            {
                float front = 0.5f + 0.5f * Mathf.Clamp(b.z / 0.55f, -1f, 1f);
                o.z += fKick * (0.35f + 0.65f * h) * 0.5f;
                o.z += fStretch * front * 0.3f;
                o.y -= Mathf.Abs(fKick) * 0.2f * h;
            }

            // Skill: tighten and tremble while charging, then a bristling burst outward.
            if (fCharge > 0.001f)
            {
                o -= outward * 0.1f * fCharge * rn;
                o.y += 0.12f * fCharge * h;
                o += new Vector3(Mathf.Sin(t * 70f + b.z * 20f), 0f, Mathf.Cos(t * 63f + b.x * 20f)) * 0.008f * fCharge;
            }
            if (fBurst > 0.001f)
            {
                o += outward * 0.17f * fBurst * (0.4f + 0.6f * low);
                o.y -= 0.1f * fBurst * h;
            }

            // Jelly wobble: taller and thinner, then wider and flatter, and back.
            if (Mathf.Abs(jig) > 1e-4f)
            {
                o.y += jig * 0.06f * h;
                o -= outward * jig * 0.04f * rn;
            }

            // Landing: flat and wide on impact, then a rebound (taller and thinner) that dies out.
            if (Mathf.Abs(fLand) > 0.001f)
            {
                o.y -= (b.y - SlimeMeshBuilder.Bottom) * 0.3f * fLand;
                o += outward * 0.08f * fLand * (0.3f + low);
            }

            // Inertia lean: the upper body lags behind the base when speeding up, stopping or turning.
            if (locoK > 0.01f)
            {
                float lag = h * h * locoK;
                o.x += leanPos.x * lag;
                o.z += leanPos.z * lag;
                o.y -= new Vector2(leanPos.x, leanPos.z).magnitude * 0.25f * lag;
            }

            // Thu Mình: drawn in, a little smaller and flatter.
            if (fTuck > 0.001f)
            {
                o -= (b - LimbCentre) * 0.16f * fTuck;
                o.y -= 0.03f * fTuck;
            }

            // Limbs pulled out of the body: points facing the target stretch toward it, the neck narrows.
            foreach (var l in limbs)
            {
                if (l.Length <= 1e-4f) continue;
                Vector3 rel = b - LimbCentre;
                float rl = rel.magnitude;
                if (rl < 1e-4f) continue;
                float k = Evolve.Swallow.SkillFxMath.LimbWeight(Vector3.Dot(rel / rl, l.Dir), l.CosWidth, l.Sharp);
                if (k <= 0f) continue;
                Vector3 perp = rel - l.Dir * Vector3.Dot(rel, l.Dir);
                o += l.Dir * (l.Length * k) - perp * (l.Squeeze * k * Mathf.Min(1f, l.Length * 2f));
            }

            if (LatchFlatten > 0f)
            {
                o.y = Mathf.Lerp(o.y, SlimeMeshBuilder.Bottom - b.y, LatchFlatten * (1f - dead));
                o += outward * 0.18f * LatchFlatten * rn;
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
            if (fMove * locoK < 0.01f) return Vector3.zero;
            float sx = b.x >= 0f ? 1f : -1f, sz = b.z >= 0f ? 1f : -1f;
            float phase = gaitPhase + (sx * sz > 0f ? 0f : Mathf.PI);
            float reach = Mathf.Clamp01((Mathf.Sqrt(b.x * b.x + b.z * b.z) - 0.1f) / 0.22f);
            float lift = Mathf.Max(0f, Mathf.Cos(phase));
            Vector3 o = fDir * (Mathf.Sin(phase) * 0.16f * fMove * locoK * reach);
            o.y += lift * 0.13f * fMove * locoK * reach;
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
