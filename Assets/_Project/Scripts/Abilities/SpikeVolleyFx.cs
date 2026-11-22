using System.Collections.Generic;
using Evolve.Player;
using Evolve.Survival;
using Evolve.Swallow;
using Evolve.Visuals;
using UnityEngine;

namespace Evolve.Abilities
{
    sealed class SpikeVolleyFx
    {
        public static readonly Color Hardened = new Color(0.68f, 0.43f, 1f);
        static Material shardMaterial, flashMaterial;
        readonly List<(Vector3 position, float time)> flashes = new List<(Vector3, float)>();
        readonly SlimeGelAnimator animator;
        readonly SlimeGelAnimator.Limb[] tips;
        readonly Vector3[] anchors;
        FxRun windup;

        public Color BodyColor { get; }

        public SpikeVolleyFx(GameObject user, int count, float delay)
        {
            var tint = user.GetComponentInChildren<SlimeGelTint>();
            BodyColor = tint != null ? tint.CoreColor : new Color(0.35f, 0.85f, 0.65f);
            animator = user.GetComponentInChildren<SlimeGelAnimator>();
            if (animator == null || delay <= 0f) return;

            tips = new SlimeGelAnimator.Limb[count];
            anchors = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                var tip = SlimeGelAnimator.AddLimb(user);
                tip.Local = true;
                tip.Width = 18f;
                tip.Sharp = 5f;
                tips[i] = tip;
            }

            var stats = user.GetComponent<SurvivalStats>();
            windup = FxRun.Begin("Spike windup", delay + 0.15f, elapsed =>
            {
                if (animator == null || !user.activeInHierarchy || (stats != null && stats.State.IsDead)) return false;
                Vector3 aim = AimController.AimDirection(user, user.transform.position, out _);
                UpdateTips(aim, SkillFxMath.EaseOutCubic(elapsed / delay));
                return true;
            }, () =>
            {
                foreach (var tip in tips) tip.Remove();
                windup = null;
            });
            UpdateTips(user.transform.forward, 0f);
        }

        void UpdateTips(Vector3 aim, float reach)
        {
            var direction = Vector3.ProjectOnPlane(aim, Vector3.up);
            if (direction.sqrMagnitude < 0.0001f) direction = animator.transform.forward;
            for (int i = 0; i < tips.Length; i++)
            {
                float fan = tips.Length == 1 ? 0f : Mathf.Lerp(-60f, 60f, i / (float)(tips.Length - 1));
                Vector3 upper = (Quaternion.AngleAxis(fan, Vector3.up) * direction.normalized * 0.75f + Vector3.up).normalized;
                Vector3 local = animator.transform.InverseTransformDirection(upper);
                Vector3 root = SlimeGelAnimator.LimbCentre + local * 0.42f;
                anchors[i] = root;
                tips[i].Target = SlimeGelAnimator.LimbCentre + local * 0.83f;
                tips[i].Reach = reach;
            }
        }

        public void Release(Vector3 aim)
        {
            if (windup == null || animator == null) return;
            UpdateTips(aim, 1f);
            Cancel();
        }

        public void Cancel() => windup?.Finish();

        public bool TryGetAnchor(int index, out Transform body, out Vector3 anchor)
        {
            body = animator != null ? animator.transform : null;
            anchor = anchors != null ? anchors[index] : Vector3.zero;
            return body != null && anchors != null;
        }

        public void Impact(Vector3 point, Vector3 normal, Vector3 direction, bool creature)
        {
            var now = Time.time;
            bool flash = creature;
            if (flash)
            {
                foreach (var previous in flashes)
                    if (SkillFxMath.SameSpikeFlash(now - previous.time, (point - previous.position).sqrMagnitude))
                    {
                        flash = false;
                        break;
                    }
            }

            if (flash)
            {
                flashes.Add((point, now));
                SkillFx.Burst(point + normal * 0.05f, new Color(0.93f, 0.81f, 1f), 5, 0.95f, 0.32f, 0.14f, 0f, 0.025f);
                Pulse(point + normal * 0.12f, normal);
            }
            SkillFx.Burst(point, Hardened, creature ? 5 : 3, 2.8f, 0.075f, 0.23f, 0.2f, 0.035f);
            Shatter(point, normal, direction, creature ? 5 : 3);
        }

        static void Pulse(Vector3 point, Vector3 normal)
        {
            const float seconds = 0.105f;
            if (flashMaterial == null)
            {
                flashMaterial = SkillFx.Gel(new Color(0.94f, 0.8f, 1f), 0.88f, 1.2f, 2.5f);
                flashMaterial.SetFloat("_SightMask", 0f);
            }
            Light light = null;
            Transform core = null;
            var flash = FxRun.Begin("Spike hit flash", seconds, t =>
            {
                float strength = 1f - t / seconds;
                light.intensity = 5.5f * strength;
                core.localScale = new Vector3(0.34f, 0.30f, 0.34f) * strength;
                return true;
            });
            flash.transform.position = point;
            core = SkillFx.Part("Spike flash core", SkillFxMeshes.Crystal(), flashMaterial);
            core.SetParent(flash.transform, false);
            core.localRotation = Quaternion.FromToRotation(Vector3.up, normal);
            core.localScale = new Vector3(0.34f, 0.30f, 0.34f);
            light = flash.gameObject.AddComponent<Light>();
            light.color = new Color(0.85f, 0.65f, 1f);
            light.intensity = 5.5f;
            light.range = 2.6f;
            light.shadows = LightShadows.None;
        }

        static void Shatter(Vector3 point, Vector3 normal, Vector3 direction, int count)
        {
            if (shardMaterial == null)
                shardMaterial = SkillFx.Gel(Hardened, 0.46f, 0.85f, 1.2f);

            const float seconds = 0.24f;
            var shards = new Transform[count];
            var velocities = new Vector3[count];
            var sizes = new Vector3[count];
            var run = FxRun.Begin("Spike shards", seconds, t =>
            {
                float k = 1f - t / seconds;
                for (int i = 0; i < count; i++)
                {
                    shards[i].position = point + velocities[i] * t + Vector3.down * (3f * t * t);
                    shards[i].localScale = sizes[i] * k;
                }
                return true;
            });

            Vector3 tangent = Vector3.ProjectOnPlane(direction, normal).normalized;
            if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.Cross(normal, Vector3.up).normalized;
            if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.right;
            Vector3 side = Vector3.Cross(normal, tangent);
            for (int i = 0; i < count; i++)
            {
                float sign = i % 2 == 0 ? 1f : -1f;
                Vector3 away = side * (sign * Random.Range(0.45f, 1f)) + tangent * Random.Range(-0.3f, 0.8f)
                    + normal * Random.Range(0.15f, 0.45f);
                velocities[i] = away.normalized * Random.Range(1.4f, 3.2f);
                sizes[i] = new Vector3(0.12f, Random.Range(0.1f, 0.22f), 0.12f);
                shards[i] = SkillFx.Part("Spike chip", SkillFxMeshes.Crystal(), shardMaterial);
                shards[i].SetParent(run.transform, false);
                shards[i].SetPositionAndRotation(point, Quaternion.FromToRotation(Vector3.up, away));
                shards[i].localScale = sizes[i];
            }
        }
    }
}
