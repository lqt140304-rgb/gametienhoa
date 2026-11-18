using UnityEngine;

namespace Evolve.World
{
    /// <summary>What kind of floating particles a zone has. Falling: Dust, Leaves, Ash; Rising: Spores; Drifting: Droplets, Mist.</summary>
    public enum ParticleKind { Droplets, Dust, Leaves, Spores, Mist, Ash }

    /// <summary>How the air of one zone looks: fog colour and sight distance, and the few particles floating in it.</summary>
    public struct AtmosphereProfile
    {
        public Color FogColor;
        /// <summary>Fog end distance (m) for a character with eyes.</summary>
        public float FogEnd;
        /// <summary>Ambient light of the zone with full sight (dusk-dim; the darker the deeper under the roots).</summary>
        public Color Ambient;
        public ParticleKind Particles;
        public Color ParticleColor;
        /// <summary>Particles alive at once in the 30 m box around the camera = Density x 10 (5 = about 50).</summary>
        public float Density;
        /// <summary>Particle size in metres.</summary>
        public float Size;
        /// <summary>Drift or fall speed in m/s.</summary>
        public float Speed;
    }
}
