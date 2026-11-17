using System.Collections.Generic;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Makes a creature leave vibration tracks that a VibrationSense can feel passively.
    /// Footsteps are emitted while it moves faster than 0.5 m/s; Pulse() adds an extra pulse (e.g. fleeing).
    /// Visual only, so it also runs on clients (replicas move through ReplicaMotion).
    /// </summary>
    public class VibrationEmitter : MonoBehaviour
    {
        static readonly List<VibrationEmitter> active = new List<VibrationEmitter>();
        public static IReadOnlyList<VibrationEmitter> Active => active;

        const float MinSpeed = 0.5f;

        [SerializeField] bool footsteps = true;
        public float strength = 1f;
        public float interval = 1.2f;
        public bool Footsteps { get => footsteps; set => footsteps = value; }

        public float LastPulseTime { get; private set; } = -999f;
        public float LastPulseStrength { get; private set; }
        /// <summary>Counts pulses so a listener can tell a new one from one it already showed.</summary>
        public int PulseCount { get; private set; }
        /// <summary>True when this emitter belongs to a Predator (listeners colour it differently).</summary>
        public bool IsPredator { get; private set; }

        Vector3 lastPos;
        float nextStep;

        void OnEnable()
        {
            active.Add(this);
            IsPredator = GetComponentInParent<Evolve.Creatures.Predator>() != null;
            lastPos = transform.position;
        }

        void OnDisable() => active.Remove(this);

        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 pos = transform.position;
            Vector3 d = pos - lastPos; d.y = 0f;
            lastPos = pos;
            if (dt <= 0f) return;
            if (footsteps && d.magnitude / dt > MinSpeed && Time.time >= nextStep)
            {
                nextStep = Time.time + interval;
                Pulse(strength);
            }
        }

        public void Pulse(float pulseStrength)
        {
            LastPulseTime = Time.time;
            LastPulseStrength = pulseStrength;
            PulseCount++;
        }
    }
}
