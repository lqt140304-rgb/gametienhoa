using System.Collections.Generic;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Keeps the lake's decorations cheap and honest. Put on the object that holds the decoration chunks (the Dress Lake
    /// tools do). Only while playing:
    ///  - Distance: every chunk renderer, light and particle system below it is switched off while the camera is farther
    ///    than ShowDistance (with a little hysteresis so nothing flickers) and on again when it comes back.
    ///  - Sight: a slime without eyes is blind (MonochromeVision.Sight = 0) and must not see glowing things, so lights,
    ///    glowing materials (mushroom spots, light shafts) and spore particles fade out with the slime's sight and are
    ///    gone entirely when it is blind.
    /// </summary>
    public class DetailCulling : MonoBehaviour
    {
        [Tooltip("Things closer than this to the camera are drawn")]
        public float ShowDistance = 150f;
        [Tooltip("Extra distance before something that was drawn is hidden again")]
        public float Hysteresis = 25f;
        [Tooltip("Seconds between checks")]
        public float Interval = 0.3f;

        enum Glow { None, Emissive, Shaft, Particles }

        struct Entry
        {
            public Renderer Renderer;
            public Light Light;
            public Bounds Bounds;
            public bool Visible;
            public Glow Glow;
            public float BaseIntensity;
            public Color BaseColor, BaseEmission;
        }

        readonly List<Entry> entries = new List<Entry>();
        MaterialPropertyBlock block;
        MonochromeVision vision;
        float nextCheck;
        float lastFade = -1f;

        void Start()
        {
            if (!Application.isPlaying) return;
            block = new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                var entry = new Entry { Renderer = r, Bounds = r.bounds, Visible = r.enabled };
                var mat = r.sharedMaterial;
                if (r is ParticleSystemRenderer) entry.Glow = Glow.Particles;
                else if (mat != null && mat.name.Contains("LightShaft"))
                {
                    entry.Glow = Glow.Shaft;
                    entry.BaseColor = mat.GetColor("_BaseColor");
                    entry.BaseEmission = mat.GetColor("_EmissionColor");
                }
                else if (mat != null && mat.IsKeywordEnabled("_EMISSION") && mat.HasProperty("_EmissionColor"))
                {
                    entry.Glow = Glow.Emissive;
                    entry.BaseEmission = mat.GetColor("_EmissionColor");
                }
                entries.Add(entry);
            }
            foreach (var l in GetComponentsInChildren<Light>(true))
                entries.Add(new Entry { Light = l, Bounds = new Bounds(l.transform.position, Vector3.one), Visible = l.enabled, BaseIntensity = l.intensity });
        }

        void Update()
        {
            if (!Application.isPlaying || Time.time < nextCheck) return;
            nextCheck = Time.time + Interval;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 eye = cam.transform.position;

            if (vision == null) vision = FindAnyObjectByType<MonochromeVision>();
            float fade = vision != null ? Mathf.Clamp01(vision.Sight) : 1f;
            bool fadeChanged = Mathf.Abs(fade - lastFade) > 0.01f;
            lastFade = fade;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.Renderer == null && e.Light == null) continue;
                // Distance on the ground plane (the roof and the floor are far apart, height should not hide things).
                Vector3 p = e.Bounds.ClosestPoint(new Vector3(eye.x, e.Bounds.center.y, eye.z));
                float distance = Vector2.Distance(new Vector2(eye.x, eye.z), new Vector2(p.x, p.z));
                bool show = e.Visible ? distance <= ShowDistance + Hysteresis : distance <= ShowDistance;
                bool shownChanged = show != e.Visible;
                if (!shownChanged && !fadeChanged) continue;
                e.Visible = show;
                entries[i] = e;
                Apply(e, show, fade);
            }
        }

        void Apply(Entry e, bool show, float fade)
        {
            bool seen = fade > 0.02f;
            if (e.Light != null)
            {
                e.Light.intensity = e.BaseIntensity * fade;
                e.Light.enabled = show && seen;
                return;
            }
            switch (e.Glow)
            {
                case Glow.Particles:
                    e.Renderer.enabled = show && seen;
                    break;
                case Glow.Shaft:
                    e.Renderer.enabled = show && seen;
                    block.Clear();
                    block.SetColor("_BaseColor", new Color(e.BaseColor.r, e.BaseColor.g, e.BaseColor.b, e.BaseColor.a * fade));
                    block.SetColor("_EmissionColor", e.BaseEmission * fade);
                    e.Renderer.SetPropertyBlock(block);
                    break;
                case Glow.Emissive:
                    e.Renderer.enabled = show;
                    block.Clear();
                    block.SetColor("_EmissionColor", e.BaseEmission * fade);
                    e.Renderer.SetPropertyBlock(block);
                    break;
                default:
                    e.Renderer.enabled = show;
                    break;
            }
        }
    }
}
