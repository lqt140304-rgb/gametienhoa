using System.Collections.Generic;
using Evolve.Organs;
using UnityEngine;

namespace Evolve.Visuals
{
    /// <summary>
    /// Colours the gel of the body and of every organ with the slime's colour, per renderer (own material instances):
    /// only renderers whose material is the gel ("SlimeGel...") are touched, so eyes and the heat film keep their look.
    /// The local player uses the chosen colour (SlimeColor); remote players get theirs from the network.
    /// Sits on the slime's "Visual" object (added by SlimeGelCore).
    /// </summary>
    public class SlimeGelTint : MonoBehaviour
    {
        public bool UseLocalColor = true;
        public Color Custom = SlimeColor.Default;

        readonly List<Renderer> scratch = new List<Renderer>();
        float nextScan;

        void OnEnable() => SlimeColor.Changed += Refresh;
        void OnDisable() => SlimeColor.Changed -= Refresh;

        void Refresh() => nextScan = 0f;

        /// <summary>Use this colour instead of the local player's choice (remote players).</summary>
        public void SetColor(Color color)
        {
            if (!UseLocalColor && Custom == color) return;
            UseLocalColor = false;
            Custom = color;
            nextScan = 0f;
        }

        void LateUpdate()
        {
            if (Time.time < nextScan) return;
            nextScan = Time.time + 0.4f; // also catches organs that were just attached
            Apply();
        }

        void Apply()
        {
            Color c = UseLocalColor ? SlimeColor.Current : Custom;
            c.a = SlimeColor.Alpha;

            Tint(GetComponent<Renderer>(), c);
            foreach (var socket in GetComponentsInChildren<OrganSocket>(true))
            {
                socket.GetComponentsInChildren(true, scratch);
                foreach (var r in scratch) Tint(r, c);
            }
        }

        void Tint(Renderer r, Color c)
        {
            if (r == null || r.sharedMaterial == null || !r.sharedMaterial.name.StartsWith("SlimeGel")) return;
            // A per-renderer material instance (created once, then reused): the most reliable way to colour one slime.
            var mat = r.material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c); else mat.color = c;
        }
    }
}
