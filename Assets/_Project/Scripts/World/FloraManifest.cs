using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>One scanned asset to install (tools/assets/flora_manifest.json).</summary>
    [Serializable]
    public class FloraAsset
    {
        public string id, source, kind, resolution, role, tier, species;
        public bool IsFoliage => role == "foliage";
    }

    /// <summary>The list of scanned assets (Poly Haven, CC0) the installer downloads on the owner's machine.</summary>
    [Serializable]
    public class FloraManifest
    {
        public FloraAsset[] assets = new FloraAsset[0];

        /// <summary>Never throws: empty or broken JSON gives an empty manifest.</summary>
        public static FloraManifest Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new FloraManifest();
            try
            {
                var m = JsonUtility.FromJson<FloraManifest>(json);
                if (m == null || m.assets == null) return new FloraManifest();
                return m;
            }
            catch (Exception) { return new FloraManifest(); }
        }

        public IEnumerable<FloraAsset> Tier(string tier)
        {
            foreach (var a in assets) if (a != null && a.tier == tier) yield return a;
        }
    }
}
