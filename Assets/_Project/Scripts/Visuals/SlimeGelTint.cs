using System.Collections.Generic;
using Evolve.Organs;
using UnityEngine;

namespace Evolve.Visuals
{
    /// <summary>
    /// Colours the gel of the body and of every organ, per part, using per-renderer material instances:
    /// the body, and the organs by their slot (eyes, legs, back, skin film). Only renderers whose material is gel
    /// ("SlimeGel...") or the skin film ("GelHeatLayer") are touched, so eye beads stay black.
    /// The local player uses the chosen colours (SlimeColor); remote players get theirs from the network.
    /// Sits on the slime's "Visual" object (added by SlimeGelCore).
    /// </summary>
    public class SlimeGelTint : MonoBehaviour
    {
        public bool UseLocalColor = true;
        Color[] custom;

        readonly List<Renderer> scratch = new List<Renderer>();
        readonly List<OrganSocket> sockets = new List<OrganSocket>();
        float nextScan;

        void OnEnable() => SlimeColor.Changed += Refresh;
        void OnDisable() => SlimeColor.Changed -= Refresh;

        void Refresh() => nextScan = 0f;

        /// <summary>The colour of the core (for SlimeGelCore).</summary>
        public Color CoreColor => UseLocalColor || custom == null ? SlimeColor.Get(SlimePart.Core) : custom[(int)SlimePart.Core]; // no allocation: read every frame

        /// <summary>Use these colours (one per SlimePart) instead of the local player's choice (remote players).</summary>
        public void SetColors(Color[] colors)
        {
            if (colors == null || colors.Length != SlimeColor.PartCount) return;
            if (!UseLocalColor && custom != null)
            {
                bool same = true;
                for (int i = 0; i < colors.Length && same; i++) same = custom[i] == colors[i];
                if (same) return;
            }
            UseLocalColor = false;
            custom = (Color[])colors.Clone();
            nextScan = 0f;
        }

        Color[] Colors() => UseLocalColor || custom == null ? SlimeColor.Resolved() : custom;

        void LateUpdate()
        {
            if (Time.time < nextScan) return;
            nextScan = Time.time + 0.4f; // also catches organs that were just attached
            Apply();
        }

        void Apply()
        {
            var colors = Colors();
            Tint(GetComponent<Renderer>(), colors[(int)SlimePart.Body]);
            GetComponentsInChildren(true, sockets);
            foreach (var socket in sockets)
            {
                Color c = colors[(int)PartOf(socket.Slot)];
                socket.GetComponentsInChildren(true, scratch);
                foreach (var r in scratch) Tint(r, c);
            }
        }

        static SlimePart PartOf(OrganSlot slot)
        {
            switch (slot)
            {
                case OrganSlot.Eyes: return SlimePart.Eyes;
                case OrganSlot.Legs: return SlimePart.Legs;
                case OrganSlot.Back: return SlimePart.Back;
                default: return SlimePart.Skin;
            }
        }

        static void Tint(Renderer r, Color c)
        {
            if (r == null || r.sharedMaterial == null) return;
            string name = r.sharedMaterial.name;
            if (name.StartsWith("SlimeGel")) c.a = SlimeColor.Alpha;
            else if (name.StartsWith("GelHeatLayer")) c.a = SlimeColor.SkinAlpha;
            else return;
            // A per-renderer material instance (created once, then reused): the most reliable way to colour one slime.
            var mat = r.material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c); else mat.color = c;
        }
    }
}
