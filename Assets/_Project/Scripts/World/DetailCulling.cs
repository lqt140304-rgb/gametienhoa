using System.Collections.Generic;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Keeps the cost of the lake's decorations small: only what is near the camera is drawn and lit.
    /// Put on the object that holds the decoration chunks (the Dress Lake tools do). Every chunk renderer, light and
    /// particle system below it is switched off while the camera is farther than ShowDistance from it (with a little
    /// hysteresis so nothing flickers on the border) and on again when it comes back. Only while playing.
    /// </summary>
    public class DetailCulling : MonoBehaviour
    {
        [Tooltip("Things closer than this to the camera are drawn")]
        public float ShowDistance = 150f;
        [Tooltip("Extra distance before something that was drawn is hidden again")]
        public float Hysteresis = 25f;
        [Tooltip("Seconds between checks")]
        public float Interval = 0.3f;

        struct Entry
        {
            public Renderer Renderer;
            public Light Light;
            public Bounds Bounds;
            public bool Visible;
        }

        readonly List<Entry> entries = new List<Entry>();
        float nextCheck;

        void Start()
        {
            if (!Application.isPlaying) return;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                entries.Add(new Entry { Renderer = r, Bounds = r.bounds, Visible = r.enabled });
            foreach (var l in GetComponentsInChildren<Light>(true))
                entries.Add(new Entry { Light = l, Bounds = new Bounds(l.transform.position, Vector3.one), Visible = l.enabled });
        }

        void Update()
        {
            if (!Application.isPlaying || Time.time < nextCheck) return;
            nextCheck = Time.time + Interval;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 eye = cam.transform.position;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.Renderer == null && e.Light == null) continue;
                // Distance on the ground plane (the roof and the floor are far apart, height should not hide things).
                Vector3 p = e.Bounds.ClosestPoint(new Vector3(eye.x, e.Bounds.center.y, eye.z));
                float distance = Vector2.Distance(new Vector2(eye.x, eye.z), new Vector2(p.x, p.z));
                bool show = e.Visible ? distance <= ShowDistance + Hysteresis : distance <= ShowDistance;
                if (show == e.Visible) continue;
                e.Visible = show;
                entries[i] = e;
                if (e.Renderer != null) e.Renderer.enabled = show;
                if (e.Light != null) e.Light.enabled = show;
            }
        }
    }
}
