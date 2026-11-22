using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// Heavy footsteps: reads the Animator's Walk and Run states and, when a foot lands in the cycle (the contact phases of the clip), plays a footfall sound,
    /// kicks up a puff of dust and thumps the camera a little (more for a bigger body). The clips are Meshy's: the walk lands its feet at 0.04 and 0.54 of the cycle,
    /// the run (an estimate) at 0 and 0.5. Nothing happens in Idle or while the Animator is blending between states.
    /// </summary>
    public class FootstepFx : MonoBehaviour
    {
        static readonly float[] WalkPhases = { 0.04f, 0.54f }, RunPhases = { 0f, 0.5f };

        Animator animator;
        string sfx; ProceduralSfx.Kind fallback; Color dust;
        int lastState; float lastTime = -1f;
        bool external;   // the owner calls StepNow itself (a gait written as code, no Animator states to read)

        public static FootstepFx Attach(GameObject creature, string sfxPath, ProceduralSfx.Kind fallbackSound, Color dustColor, bool externallyDriven = false)
        {
            if (creature == null || creature.GetComponent<FootstepFx>() != null) return null;
            var f = creature.AddComponent<FootstepFx>();
            f.sfx = sfxPath; f.fallback = fallbackSound; f.dust = dustColor; f.external = externallyDriven;
            return f;
        }

        void Start() { animator = GetComponentInChildren<Animator>(true); }

        void Update()
        {
            if (external) return;
            if (animator == null || animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled || animator.IsInTransition(0)) { lastTime = -1f; return; }
            var info = animator.GetCurrentAnimatorStateInfo(0);
            bool walk = info.IsName("Walk"), run = info.IsName("Run");
            if (!walk && !run) { lastTime = -1f; return; }
            if (info.shortNameHash != lastState || lastTime < 0f) { lastState = info.shortNameHash; lastTime = info.normalizedTime; return; }
            float now = info.normalizedTime;
            if (FootstepRules.Contacts(lastTime, now, walk ? WalkPhases : RunPhases) > 0) Step();
            lastTime = now;
        }

        public void StepNow() => Step();

        void Step()
        {
            float scale = Mathf.Max(0.5f, transform.lossyScale.y);
            Vector3 p = transform.position;
            ProceduralSfx.PlayFile(sfx, p, Mathf.Clamp(0.55f + 0.12f * scale, 0.5f, 1f), fallback);
            Evolve.Abilities.SkillFx.Dust(p, 0.5f * scale, dust, 6);
            ImpactFx.Thump(p, ShakeRules.StepAmplitude(scale), 0.25f, 22f);
        }
    }
}
