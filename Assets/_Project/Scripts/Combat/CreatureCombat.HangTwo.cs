using Evolve.Abilities;
using Evolve.Creatures;
using UnityEngine;

namespace Evolve.Combat
{
    public partial class CreatureCombat
    {
        LineRenderer oreChargeTell;
        Material oreTellMaterial;
        CreatureGlow oreGlow;
        bool oreBeetle, oreWasDizzy;
        bool oreFxInitialized;

        void UpdateOreBeetleFx()
        {
            if (!oreBeetle || scheduler == null) return;
            if (!oreFxInitialized)
            {
                oreFxInitialized = true;
                oreGlow = new CreatureGlow(gameObject);
                var go = new GameObject("Ore charge direction");
                go.transform.SetParent(transform, false);
                oreChargeTell = go.AddComponent<LineRenderer>();
                oreTellMaterial = SkillFx.Gel(new Color(0.75f, 0.52f, 0.22f), 0.55f, 0f, 0.6f);
                oreChargeTell.sharedMaterial = oreTellMaterial;
                oreChargeTell.positionCount = 3;
                oreChargeTell.startWidth = 0.07f;
                oreChargeTell.endWidth = 0.02f;
                oreChargeTell.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                oreChargeTell.receiveShadows = false;
            }

            bool live = pred != null && !pred.IsReplica && !Predator.Frozen && pred.Health > 0f;
            bool windup = live && scheduler.Phase == MovePhase.Windup;
            bool charging = live && scheduler.Phase == MovePhase.Active;
            float pulse = windup ? 0.15f + 0.35f * scheduler.Progress01 :
                charging ? 0.24f : 0.035f * (0.5f + 0.5f * Mathf.Sin(Time.time * 2f));
            if (!live) pulse = 0f;
            oreGlow.Set(pulse);
            oreChargeTell.enabled = windup;
            if (windup)
            {
                Vector3 from = transform.position + Vector3.up * 0.055f;
                Vector3 direction = target != null ? Flat(target.transform.position - transform.position).normalized : transform.forward;
                float distance = Mathf.Min(2.5f, scheduler.Current.Distance);
                // Keep the warning on this side of a wall.
                while (distance > Radius && !HangOneGround.Clear(from + Vector3.up * 0.3f, from + direction * distance + Vector3.up * 0.3f))
                    distance -= 0.25f;
                oreChargeTell.SetPosition(0, from + direction * Radius);
                oreChargeTell.SetPosition(1, from + direction * Mathf.Max(Radius, distance * 0.65f));
                oreChargeTell.SetPosition(2, from + direction * Mathf.Max(Radius, distance));
            }
            bool dizzy = live && Time.time < dizzyUntil;
            if (dizzy && !oreWasDizzy)
                SkillFx.Burst(transform.position + transform.forward * Radius + Vector3.up * 0.2f,
                    new Color(0.70f, 0.57f, 0.30f), 5, 0.7f, 0.06f, 0.24f, 0.4f, 0.1f);
            oreWasDizzy = dizzy;
        }

        void ClearOreBeetleFx()
        {
            if (oreChargeTell != null) oreChargeTell.enabled = false;
            oreGlow?.Set(0f);
        }
    }
}
