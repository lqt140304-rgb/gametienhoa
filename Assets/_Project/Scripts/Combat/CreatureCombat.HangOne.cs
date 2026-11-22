using Evolve.Abilities;
using Evolve.Creatures;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Combat
{
    public partial class CreatureCombat
    {
        public const float TongueRange = 6f, TongueTell = 0.8f, TongueCooldown = 8f;
        const float TongueContact = 0.12f, TonguePull = 0.3f, TongueRecover = 0.16f;
        const float CamouflageDelay = 3f;
        static readonly int tintId = Shader.PropertyToID("_Tint");

        bool shoreLizard, tongueFired, tongueHit, camouflage;
        float tongueStarted, tongueReadyAt, stationarySince;
        Component tongueVictim;
        Vector3 tongueAim, stationaryAnchor;
        Transform rigTongue;
        Vector3 tongueScaleBeforeHide;
        bool rigTongueHidden;
        float tongueReach;
        Vector3 tongueTip;
        LineRenderer tongueLine;
        Material tongueMaterial;
        MaterialPropertyBlock camouflageBlock;
        Renderer[] camouflageSkin;
        Vector3 Mouth => col != null ? col.bounds.center + transform.forward * (Radius * 0.55f) : transform.position + Vector3.up * 0.3f;
        public bool IsCamouflaged => camouflage;

        void InitializeShoreLizard()
        {
            stationaryAnchor = rb.position;
            stationarySince = Time.time;
            if (model == null) return;
            foreach (var bone in model.GetComponentsInChildren<Transform>(true))
                if (bone.name == "Tongue") { rigTongue = bone; break; }
        }

        public static bool CanPull(float hunterMass, float preyMass) => preyMass > 0f && preyMass <= hunterMass * 0.5f;
        public static bool TongueReady(float now, float readyAt, float distance) => now >= readyAt && distance <= TongueRange;

        internal void BreakCamouflage()
        {
            if (shoreLizard) UpdateCamouflage(true);
        }

        void UpdateCamouflage(bool attacking)
        {
            Vector3 pos = rb.position;
            if (attacking || (pos - stationaryAnchor).sqrMagnitude > 0.0016f || !HangOneGround.Moss(pos))
            {
                stationaryAnchor = pos;
                stationarySince = Time.time;
                SetCamouflage(false);
                return;
            }
            if (Time.time - stationarySince >= CamouflageDelay) SetCamouflage(true);
        }

        void SetCamouflage(bool value)
        {
            if (value == camouflage) return;
            camouflage = value;
            camouflageSkin ??= GetComponentsInChildren<SkinnedMeshRenderer>();
            camouflageBlock ??= new MaterialPropertyBlock();
            foreach (var skin in camouflageSkin)
            {
                if (skin == null) continue;
                skin.GetPropertyBlock(camouflageBlock);
                camouflageBlock.SetColor(tintId, value ? new Color(0.48f, 0.56f, 0.44f) : Color.white);
                skin.SetPropertyBlock(camouflageBlock);
            }
        }

        bool TickTongue()
        {
            if (tongueVictim == null)
            {
                if (tongueStarted > 0f) EndTongue();
                if (scheduler.Busy || !TongueReady(Time.time, tongueReadyAt, 0f) || Predator.Passive || pred.Calm) return false;
                Component victim = target;
                if (!TryTongueAim(victim, out tongueAim))
                {
                    if (pred.Intent.Mode != IntentMode.Chase || !pred.Intent.Meal || pred.Intent.Target == null) return false;
                    victim = pred.Intent.Target.GetComponent<Prey>();
                    if (!TryTongueAim(victim, out tongueAim)) return false;
                }
                tongueVictim = victim;
                tongueStarted = Time.time;
                tongueReadyAt = Time.time + TongueCooldown;
                tongueFired = tongueHit = false;
                UpdateCamouflage(true);
            }

            float elapsed = Time.time - tongueStarted;
            if (elapsed < TongueTell)
            {
                if (tongueVictim != null) tongueAim = SkillFx.Center(tongueVictim.transform);
                Vector3 dir = Flat(tongueAim - transform.position);
                if (dir.sqrMagnitude > 0.001f) rb.MoveRotation(Quaternion.LookRotation(dir));
                return true;
            }

            if (!tongueFired) { tongueFired = true; loco?.PlayAction("Attack"); }
            float strikeTime = elapsed - TongueTell;
            if (strikeTime >= TongueContact + TonguePull + TongueRecover) { EndTongue(); return true; }
            Vector3 mouth = Mouth;
            if (TryTongueAim(tongueVictim, out Vector3 validAim)) tongueAim = validAim;
            Vector3 tip = mouth + Vector3.ClampMagnitude(tongueAim - mouth, TongueRange);
            if (!HangOneGround.Clear(mouth, tip)) tip = mouth;
            if (!tongueHit && strikeTime >= TongueContact)
            {
                tongueHit = true;
                if (TryTongueAim(tongueVictim, out Vector3 impact))
                {
                    PullVictim(tongueVictim);
                    SkillFx.Burst(impact, new Color(0.61f, 0.32f, 0.31f), 4, 0.55f, 0.05f, 0.18f, 0.25f, 0.07f);
                }
            }
            tongueReach = strikeTime < TongueContact ? strikeTime / TongueContact :
                1f - Mathf.Clamp01((strikeTime - TongueContact - TonguePull) / TongueRecover);
            tongueTip = tip;
            return true;
        }

        bool TryTongueAim(Component victim, out Vector3 aim)
        {
            aim = Mouth;
            if (victim == null || !victim.gameObject.activeInHierarchy) return false;
            float mass;
            if (victim is Prey animal && !animal.IsReplica && animal.Health > 0f) mass = animal.BodyMass;
            else if (victim is SurvivalStats slime && !slime.IsRemote && !slime.State.IsDead &&
                slime.TryGetComponent(out Evolve.Swallow.SlimeBody body)) mass = body.Mass;
            else return false;
            aim = SkillFx.Center(victim.transform);
            return CanPull(pred.BodyMass, mass) && TongueReady(Time.time, 0f, Vector3.Distance(transform.position, victim.transform.position))
                && HangOneGround.Clear(Mouth, aim);
        }

        void PullVictim(Component victim)
        {
            Vector3 toward = Flat(transform.position - victim.transform.position);
            float distance = Mathf.Max(0f, toward.magnitude - Radius - 0.55f);
            if (distance <= 0f) return;
            Vector3 velocity = toward.normalized * Mathf.Min(distance, 3f) / TonguePull;
            if (victim is Prey preyVictim)
            {
                if (CreatureStatus.ResistsDisplacement(preyVictim)) return;
                CreatureStatus.Knock(preyVictim, velocity, TonguePull);
                CreatureStatus.Stun(preyVictim, TonguePull, false);
            }
            else if (victim is SurvivalStats slime && !(slime.TryGetComponent(out BodyState state) && (state.Rooted || state.Climbing)))
                WindPush.Apply(slime, velocity, TonguePull);
        }

        void UpdateTongueVisual()
        {
            if (!tongueFired) return;
            Vector3 from = rigTongue != null ? rigTongue.position : Mouth;
            Vector3 to = Vector3.Lerp(from, tongueTip, tongueReach);
            SetRigTongueHidden(true);
            DrawTongue(from, to);
        }

        void SetRigTongueHidden(bool hide)
        {
            if (rigTongue == null) return;
            if (hide)
            {
                // Animator may have already sampled another tongue pose this frame.
                if (!rigTongueHidden || rigTongue.localScale != Vector3.zero)
                    tongueScaleBeforeHide = rigTongue.localScale;
                rigTongue.localScale = Vector3.zero;
            }
            else if (rigTongueHidden && rigTongue.localScale == Vector3.zero)
                rigTongue.localScale = tongueScaleBeforeHide;
            rigTongueHidden = hide;
        }

        void DrawTongue(Vector3 from, Vector3 to)
        {
            if (tongueLine == null)
            {
                var strand = new GameObject("Lizard tongue");
                strand.transform.SetParent(transform, false);
                tongueLine = strand.AddComponent<LineRenderer>();
                tongueMaterial = SkillFx.Gel(new Color(0.68f, 0.30f, 0.30f), 0.75f, 0.1f, 0.65f);
                tongueLine.sharedMaterial = tongueMaterial;
                tongueLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tongueLine.receiveShadows = false;
                tongueLine.widthMultiplier = 0.085f;
                tongueLine.positionCount = 5;
                tongueLine.numCapVertices = 3;
                tongueLine.widthCurve = new AnimationCurve(
                    new Keyframe(0f, 1f), new Keyframe(0.7f, 0.64f), new Keyframe(1f, 0.32f));
            }
            tongueLine.enabled = (to - from).sqrMagnitude > 0.0001f;
            float sag = Mathf.Min(0.055f, (to - from).magnitude * 0.025f);
            for (int i = 0; i < tongueLine.positionCount; i++)
            {
                float t = i / (float)(tongueLine.positionCount - 1);
                tongueLine.SetPosition(i, Vector3.Lerp(from, to, t) - Vector3.up * (4f * t * (1f - t) * sag));
            }
        }

        void EndTongue()
        {
            tongueVictim = null;
            tongueStarted = tongueReach = 0f;
            tongueFired = false;
            SetRigTongueHidden(false);
            if (tongueLine != null) tongueLine.enabled = false;
        }

        void OnDestroy()
        {
            if (tongueMaterial != null) Destroy(tongueMaterial);
        }
    }
}
