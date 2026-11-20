using System.Collections.Generic;
using Evolve.Swallow;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.Abilities
{
    /// <summary>A small timed effect: ticks every frame until its time is up (or Tick says stop), then cleans up after itself.</summary>
    sealed class FxRun : MonoBehaviour
    {
        public float Duration;
        float start;
        System.Func<float, bool> tick;
        System.Action end;
        bool done;

        public float Elapsed => Time.time - start;

        public static FxRun Begin(string name, float duration, System.Func<float, bool> tick, System.Action end = null)
        {
            var run = new GameObject("Fx " + name).AddComponent<FxRun>();
            run.Duration = duration; run.start = Time.time; run.tick = tick; run.end = end;
            return run;
        }

        void Update()
        {
            float t = Elapsed;
            if (t >= Duration || (tick != null && !tick(t))) Finish();
        }

        public void Finish()
        {
            if (done) return;
            done = true;
            end?.Invoke();
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// The look of the cave skills, all built from code: one shared particle system for sparks and chips (GlowParticle shader), procedural
    /// meshes (SkillFxMeshes) and the SkillGel shader (glassy rim, dissolving fade). Sizes come from colliders, so new creature models fit by themselves.
    /// shortcut: effects show only on the machine that used the skill; a co-op partner sees nothing yet. Upgrade: send a small event through NetWorld.
    /// </summary>
    public static class SkillFx
    {
        static Material gelBase, glowMaterial;
        static ParticleSystem sparks;
        static readonly Dictionary<Component, FxRun> starRuns = new Dictionary<Component, FxRun>();
        static readonly Dictionary<Transform, FxRun> shieldRuns = new Dictionary<Transform, FxRun>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { gelBase = glowMaterial = null; sparks = null; starRuns.Clear(); shieldRuns.Clear(); }

        // ---------- measuring ----------

        static Collider ColliderOf(Component c) => c != null ? c.GetComponentInChildren<Collider>() : null;
        /// <summary>Horizontal radius of a body.</summary>
        public static float Radius(Transform t)
        {
            var col = ColliderOf(t);
            if (col == null) return 0.5f;
            var e = col.bounds.extents;
            return Mathf.Max(0.2f, Mathf.Max(e.x, e.z));
        }
        public static Vector3 Center(Transform t) { var col = ColliderOf(t); return col != null ? col.bounds.center : t.position + Vector3.up * 0.3f; }
        /// <summary>Height of the top of a body (where a stunned creature's stars go).</summary>
        public static float Top(Component c) { var col = ColliderOf(c); return col != null ? col.bounds.max.y : c.transform.position.y + 0.8f; }

        // ---------- materials and parts ----------

        static Shader FindShader(string name)
        {
            var s = Shader.Find(name);
            return s != null ? s : Shader.Find("Sprites/Default");   // missing shader: flat sprites, the game still runs
        }

        /// <summary>A material for one effect (destroy it with the effect). fill = body glow, rim = edge glow (0 for flat shapes).</summary>
        public static Material Gel(Color c, float fill = 0.12f, float rim = 1.6f, float intensity = 1.5f)
        {
            if (gelBase == null) gelBase = Resources.Load<Material>("CaveSkills/SkillGel");   // made by Evolve → Create Cave Skills; being in Resources puts the shader in a build
            if (gelBase == null) gelBase = new Material(FindShader("Evolve/SkillGel")) { hideFlags = HideFlags.HideAndDontSave };
            var m = new Material(gelBase);
            m.SetColor("_Color", c);
            m.SetFloat("_Fill", fill); m.SetFloat("_RimGain", rim); m.SetFloat("_Intensity", intensity); m.SetFloat("_Fade", 1f);
            return m;
        }

        static Transform Part(string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name) { layer = 2 };   // Ignore Raycast: aiming rays pass through
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return go.transform;
        }

        static void Fade(Material m, float v) => m.SetFloat("_Fade", v);

        // ---------- particles ----------

        static ParticleSystem Sparks()
        {
            if (sparks != null) return sparks;
            var go = new GameObject("SkillFx Particles") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(go);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false; main.loop = true; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 600; main.gravityModifier = 0.35f; main.startLifetime = 1f; main.startSpeed = 0f; main.startSize = 0.1f;
            var emission = ps.emission; emission.enabled = false;   // only Emit() makes particles
            var shape = ps.shape; shape.enabled = false;
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0f, 1f) });
            fade.color = g;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.25f)));
            if (glowMaterial == null) glowMaterial = Resources.Load<Material>("CaveSkills/SkillGlow");
            if (glowMaterial == null) glowMaterial = new Material(FindShader("Evolve/GlowParticle")) { hideFlags = HideFlags.HideAndDontSave };
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = glowMaterial; r.renderMode = ParticleSystemRenderMode.Billboard;
            ps.Play();
            sparks = ps;
            return ps;
        }

        static void EmitOne(Vector3 pos, Vector3 velocity, Color c, float size, float life)
        {
            var p = new ParticleSystem.EmitParams
            {
                position = pos, velocity = velocity, startSize = size, startLifetime = life,
                startColor = new Color(c.r, c.g, c.b, Random.Range(0.7f, 1f)),
            };
            Sparks().Emit(p, 1);
        }

        /// <summary>A puff of glowing motes thrown out of a point (impacts, shield shards, rock chips).</summary>
        public static void Burst(Vector3 pos, Color c, int count, float speed, float size = 0.12f, float life = 0.7f, float up = 0.4f, float spread = 0.15f)
        {
            for (int i = 0; i < count; i++)
                EmitOne(pos + Random.insideUnitSphere * spread, (Random.onUnitSphere + Vector3.up * up) * speed * Random.Range(0.5f, 1f),
                        c, size * Random.Range(0.7f, 1.3f), life * Random.Range(0.7f, 1.2f));
        }

        /// <summary>Dust kicked out along the ground from a point (a shockwave).</summary>
        public static void Dust(Vector3 center, float radius, Color c, int count = 26)
        {
            center.y = Evolve.World.GroundHeight.At(center.x, center.z, center.y - 0.4f) + 0.1f;
            for (int i = 0; i < count; i++)
            {
                float a = Random.value * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                EmitOne(center + dir * radius * 0.2f, dir * radius * Random.Range(1.2f, 2.2f) + Vector3.up * Random.Range(0.3f, 1f),
                        c, Random.Range(0.12f, 0.24f), Random.Range(0.5f, 0.9f));
            }
        }

        // ---------- shapes ----------

        /// <summary>An expanding wave on the ground.</summary>
        public static void Ring(Vector3 center, float radius, Color c, float seconds = 0.55f, float startScale = 0.15f)
        {
            center.y = Evolve.World.GroundHeight.At(center.x, center.z, center.y - 0.4f) + 0.06f;
            var mat = Gel(c, 1f, 0f, 2.2f);
            var t = Part("Ring", SkillFxMeshes.Ring(), mat);
            t.position = center;
            FxRun.Begin("Ring", seconds, e =>
            {
                float k = e / seconds;
                t.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, SkillFxMath.EaseOutCubic(k)) * radius;
                Fade(mat, 1f - k * k);
                return true;
            }, () => { Object.Destroy(t.gameObject); Object.Destroy(mat); });
        }

        /// <summary>A translucent bubble round a body that swells in, breathes and thins out (Thu Mình, Phát Sáng, Bám Vách, a hit shrugged off).</summary>
        public static void Shell(Transform body, Color c, float seconds, float fill = 0.14f, float rim = 2.2f, float scale = 1.25f)
        {
            if (body == null) return;
            var mat = Gel(c, fill, rim);
            var t = Part("Shell", SkillFxMeshes.Sphere(), mat);
            float radius = Radius(body);
            FxRun.Begin("Shell", seconds, e =>
            {
                if (body == null) return false;
                float k = SkillFxMath.Envelope(e, seconds, Mathf.Min(0.15f, seconds * 0.4f), Mathf.Min(0.35f, seconds * 0.5f));
                t.position = Center(body);
                t.localScale = Vector3.one * radius * 2f * scale * (1f + 0.03f * Mathf.Sin(e * 6f)) * Mathf.Lerp(0.85f, 1f, k);
                Fade(mat, k);
                return true;
            }, () => { Object.Destroy(t.gameObject); Object.Destroy(mat); });
        }

        /// <summary>Crystal shards circling a body (Khiên Quặng). One shield per body: a new one replaces the old.</summary>
        public static void Orbit(Transform body, Color c, int count, float seconds)
        {
            if (body == null) return;
            if (shieldRuns.TryGetValue(body, out var old) && old != null) old.Finish();
            var mat = Gel(c, 0.35f, 1.2f, 1.8f);
            var shards = new Transform[count];
            for (int i = 0; i < count; i++) shards[i] = Part("Shard", SkillFxMeshes.Crystal(), mat);
            float radius = Radius(body);
            var run = FxRun.Begin("Orbit", seconds, e =>
            {
                if (body == null) return false;
                float k = SkillFxMath.Envelope(e, seconds, 0.2f, 0.5f);
                Vector3 centre = Center(body);
                for (int i = 0; i < count; i++)
                {
                    float a = e * 2.4f + i * Mathf.PI * 2f / count;
                    Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    Vector3 pos = centre + radial * (radius * 1.4f + 0.15f) * Mathf.Lerp(0.6f, 1f, k) + Vector3.up * 0.15f * Mathf.Sin(e * 3f + i);
                    shards[i].position = pos;
                    shards[i].rotation = Quaternion.FromToRotation(Vector3.up, radial + Vector3.up * 0.35f);
                    shards[i].localScale = new Vector3(0.2f, 0.42f, 0.2f) * k * Mathf.Max(0.7f, radius);
                }
                Fade(mat, k);
                return true;
            }, () =>
            {
                shieldRuns.Remove(body);
                foreach (var s in shards) if (s != null) Object.Destroy(s.gameObject);
                Object.Destroy(mat);
            });
            shieldRuns[body] = run;
        }

        /// <summary>The shield took its last hit: it bursts into shards.</summary>
        public static void ShieldBreak(Transform body, Color c)
        {
            if (body == null) return;
            if (shieldRuns.TryGetValue(body, out var run) && run != null) run.Finish();
            Burst(Center(body), c, 24, 3.4f, 0.16f, 0.8f, 0.4f, Radius(body));
            Ring(body.position, Radius(body) * 2.6f, c, 0.4f, 0.4f);
        }

        /// <summary>Warm motes drifting round a body (Phát Sáng).</summary>
        public static void Fireflies(Transform body, Color c, float seconds)
        {
            if (body == null) return;
            float radius = Radius(body), next = 0f;
            FxRun.Begin("Fireflies", seconds, e =>
            {
                if (body == null) return false;
                if (e < next) return true;
                next = e + 0.07f;
                EmitOne(Center(body) + Random.onUnitSphere * radius * Random.Range(1.1f, 2.2f), Random.insideUnitSphere * 0.25f + Vector3.up * 0.15f,
                        c, 0.14f, Random.Range(1f, 1.8f));
                return true;
            });
        }

        /// <summary>A wobbling tongue shoots from one body to another and stays stuck to it while it is dragged in (Lưỡi Dài).</summary>
        public static void Tongue(Transform user, Transform prey, Color c)
        {
            if (user == null || prey == null) return;
            const int points = 14;
            const float shoot = 0.1f, life = 0.5f;
            var go = new GameObject("Tongue") { layer = 2 };
            var lr = go.AddComponent<LineRenderer>();
            var mat = Gel(c, 1f, 0f, 1.6f);
            lr.sharedMaterial = mat; lr.positionCount = points; lr.useWorldSpace = true; lr.numCapVertices = 6;
            lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false;
            lr.widthCurve = new AnimationCurve(new Keyframe(0f, 0.16f), new Keyframe(0.7f, 0.1f), new Keyframe(1f, 0.05f));
            lr.widthMultiplier = Mathf.Clamp(Radius(user) * 2f, 0.6f, 2f);
            bool hit = false;
            FxRun.Begin("Tongue", life, e =>
            {
                if (user == null || prey == null) return false;
                Vector3 a = Center(user), b = Center(prey);
                float reach = Mathf.Clamp01(e / shoot);
                Vector3 tip = Vector3.Lerp(a, b, reach);
                Vector3 side = Vector3.Cross(Vector3.up, b - a).normalized;
                for (int i = 0; i < points; i++)
                {
                    float f = i / (points - 1f);
                    float wob = Mathf.Sin(f * Mathf.PI) * Mathf.Sin(e * 30f + f * 8f) * 0.08f * (1f - reach * 0.7f);
                    lr.SetPosition(i, Vector3.Lerp(a, tip, f) + side * wob);
                }
                Fade(mat, 1f - Mathf.Clamp01((e - 0.38f) / 0.12f));
                if (!hit && reach >= 1f) { hit = true; Burst(tip, c, 10, 1.6f, 0.12f, 0.5f, 0.3f, 0.15f); }
                return true;
            }, () => { Object.Destroy(go); Object.Destroy(mat); });
        }

        /// <summary>A crystal sting jabbed from one point to another (Châm Choáng).</summary>
        public static void Sting(Vector3 from, Vector3 to, Color c)
        {
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len < 0.05f) return;
            var mat = Gel(c, 0.4f, 1.3f, 2.2f);
            var t = Part("Sting", SkillFxMeshes.Crystal(), mat);
            var rot = Quaternion.FromToRotation(Vector3.up, d / len);
            bool hit = false;
            FxRun.Begin("Sting", 0.4f, e =>
            {
                float k = Mathf.Clamp01(e / 0.08f);
                t.SetPositionAndRotation(from, rot);
                t.localScale = new Vector3(0.22f, len * k, 0.22f);
                Fade(mat, 1f - Mathf.Clamp01((e - 0.15f) / 0.25f));
                if (!hit && k >= 1f) { hit = true; Burst(to, c, 12, 2.4f, 0.12f, 0.5f, 0.4f, 0.1f); Ring(to, 0.8f, c, 0.35f, 0.1f); }
                return true;
            }, () => { Object.Destroy(t.gameObject); Object.Destroy(mat); });
        }

        /// <summary>Stars circling above a stunned creature's head. Stunning it again while they are out only extends them.</summary>
        public static void Stars(Component creature, float seconds)
        {
            if (creature == null || seconds <= 0f) return;
            if (starRuns.TryGetValue(creature, out var old) && old != null) { old.Duration = Mathf.Max(old.Duration, old.Elapsed + seconds); return; }
            const int n = 3;
            var mat = Gel(new Color(1f, 0.92f, 0.55f), 0.9f, 0f, 2.2f);
            var parts = new Transform[n];
            for (int i = 0; i < n; i++) parts[i] = Part("Star", SkillFxMeshes.Crystal(), mat);
            float radius = Radius(creature.transform) * 0.6f + 0.2f;
            FxRun run = null;
            run = FxRun.Begin("Stars", seconds, e =>
            {
                if (creature == null) return false;
                float k = SkillFxMath.Envelope(e, run.Duration, 0.15f, 0.25f);
                Vector3 top = new Vector3(creature.transform.position.x, Top(creature) + 0.3f, creature.transform.position.z);
                for (int i = 0; i < n; i++)
                {
                    float a = e * 4f + i * Mathf.PI * 2f / n;
                    parts[i].position = top + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                    parts[i].rotation = Quaternion.Euler(0f, e * 200f, 20f);
                    parts[i].localScale = new Vector3(0.12f, 0.24f, 0.12f) * k;
                }
                Fade(mat, k);
                return true;
            }, () =>
            {
                starRuns.Remove(creature);
                foreach (var p in parts) if (p != null) Object.Destroy(p.gameObject);
                Object.Destroy(mat);
            });
            starRuns[creature] = run;
        }

        /// <summary>A fading ghost trail behind a body that dashes (Lách Né), with a few streaks of wind.</summary>
        public static void Afterimage(GameObject user, Color c, Vector3 dir)
        {
            var go = new GameObject("Afterimage");
            go.transform.SetParent(user.transform, false);
            var mat = Gel(c, 1f, 0f, 1.4f);
            var tr = go.AddComponent<TrailRenderer>();
            float radius = Radius(user.transform);
            tr.time = 0.35f; tr.minVertexDistance = 0.05f; tr.sharedMaterial = mat;
            tr.widthCurve = new AnimationCurve(new Keyframe(0f, radius * 1.6f), new Keyframe(1f, 0f));
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = g;
            tr.shadowCastingMode = ShadowCastingMode.Off; tr.receiveShadows = false;
            Vector3 centre = Center(user.transform);
            for (int i = 0; i < 8; i++)
                EmitOne(centre + Random.insideUnitSphere * radius, -dir * Random.Range(2f, 4f) + Vector3.up * 0.2f, Color.Lerp(c, Color.white, 0.5f), 0.1f, 0.4f);
            FxRun.Begin("Afterimage", 0.8f, e => true, () => { Object.Destroy(go); Object.Destroy(mat); });
        }
    }
}
