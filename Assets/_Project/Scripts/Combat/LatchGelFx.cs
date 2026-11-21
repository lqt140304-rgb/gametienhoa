using System.Collections.Generic;
using Evolve.Visuals;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.Combat
{
    /// <summary>Local visual coating. The copy retains the last creature pose when gameplay removes the target.</summary>
    sealed class LatchGelFx : MonoBehaviour
    {
        sealed class Surface
        {
            public Renderer Source;
            public MeshRenderer Copy;
            public Mesh Baked;
            public Vector3 Position;
            public Vector3 Scale;
        }

        static readonly Dictionary<Transform, LatchGelFx> Active = new Dictionary<Transform, LatchGelFx>();

        readonly List<Surface> surfaces = new List<Surface>();
        SlimeGelAnimator animator;
        SlimeGelAnimator.Limb strand;
        Transform slime;
        Transform target;
        Material material;
        Vector3 anchor, frozenAnchor;
        float radius, progress, coverage, elapsed;
        bool absorbing, releasing;

        public static LatchGelFx Begin(GameObject owner, Transform creature, bool onBack = true)
        {
            var shader = Resources.Load<Shader>("LatchGelCoating");
            if (shader == null || owner == null || creature == null) return null;
            var fx = new GameObject("Latch gel").AddComponent<LatchGelFx>();
            fx.slime = owner.transform;
            fx.target = creature;
            fx.animator = owner.GetComponentInChildren<SlimeGelAnimator>();
            var tint = owner.GetComponentInChildren<SlimeGelTint>();
            var color = tint != null ? tint.CoreColor : new Color(0.35f, 0.85f, 0.65f);
            fx.material = new Material(shader);
            fx.material.SetColor("_GelColor", color);
            var bounds = new Bounds(creature.position, Vector3.zero);
            bool hasBounds = false;
            foreach (var renderer in creature.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled) continue;
                Mesh mesh = null;
                Mesh baked = null;
                if (renderer is SkinnedMeshRenderer skin && skin.sharedMesh != null)
                    mesh = baked = new Mesh();
                else if (renderer is MeshRenderer && renderer.TryGetComponent(out MeshFilter filter))
                    mesh = filter.sharedMesh;
                if (mesh == null) continue;

                var copy = new GameObject("Gel surface");
                copy.transform.SetParent(fx.transform, false);
                copy.layer = renderer.gameObject.layer;
                copy.AddComponent<MeshFilter>().sharedMesh = mesh;
                var display = copy.AddComponent<MeshRenderer>();
                var materials = new Material[baked != null ? ((SkinnedMeshRenderer)renderer).sharedMesh.subMeshCount : mesh.subMeshCount];
                for (int i = 0; i < materials.Length; i++) materials[i] = fx.material;
                display.sharedMaterials = materials;
                display.shadowCastingMode = ShadowCastingMode.Off;
                display.receiveShadows = false;
                fx.surfaces.Add(new Surface { Source = renderer, Copy = display, Baked = baked });
                if (hasBounds) bounds.Encapsulate(renderer.bounds);
                else { bounds = renderer.bounds; hasBounds = true; }
            }
            if (!hasBounds) { Destroy(fx.gameObject); return null; }
            if (Active.TryGetValue(fx.slime, out var previous) && previous != null) previous.Stop();
            Active[fx.slime] = fx;
            if (onBack)
            {
                fx.strand = SlimeGelAnimator.AddLimb(owner);
                if (fx.strand != null) { fx.strand.Width = 44f; fx.strand.Sharp = 1.5f; }
            }
            var contact = onBack ? bounds.center + Vector3.up * bounds.extents.y : bounds.ClosestPoint(owner.transform.position);
            fx.anchor = creature.InverseTransformPoint(contact);
            var offset = contact - bounds.center;
            var reach = bounds.extents + new Vector3(Mathf.Abs(offset.x), Mathf.Abs(offset.y), Mathf.Abs(offset.z));
            fx.radius = Mathf.Max(0.15f, reach.magnitude);
            fx.RefreshPose();
            fx.Draw(0f, 0f, 0f, 1f);
            return fx;
        }

        public void SetGripProgress(float value) => progress = Mathf.Max(progress, value);

        public void Absorb()
        {
            if (releasing || absorbing) return;
            RefreshPose();
            foreach (var surface in surfaces) surface.Source = null;
            target = null;
            strand?.Remove();
            strand = null;
            absorbing = true;
            elapsed = 0f;
        }

        public void Release(bool stretch)
        {
            if (absorbing || releasing) return;
            releasing = true;
            elapsed = 0f;
            if (strand == null) return;
            if (stretch) { strand.Width = 24f; strand.Sharp = 2f; }
            else { strand.Remove(); strand = null; }
        }

        void RefreshPose()
        {
            if (target == null) return;
            frozenAnchor = target.TransformPoint(anchor);
            foreach (var surface in surfaces)
            {
                if (surface.Source == null) continue;
                var source = surface.Source.transform;
                surface.Position = source.position;
                surface.Scale = source.lossyScale;
                surface.Copy.transform.SetPositionAndRotation(surface.Position, source.rotation);
                surface.Copy.transform.localScale = surface.Scale;
                if (surface.Baked != null) ((SkinnedMeshRenderer)surface.Source).BakeMesh(surface.Baked);
            }
        }

        void Draw(float spread, float silhouette, float dissolve, float opacity)
        {
            material.SetVector("_Anchor", frozenAnchor);
            material.SetFloat("_Radius", radius);
            material.SetFloat("_Coverage", spread);
            material.SetFloat("_Silhouette", silhouette);
            material.SetFloat("_Dissolve", dissolve);
            material.SetFloat("_Opacity", opacity);
        }

        void LateUpdate()
        {
            if (slime == null || (!absorbing && !releasing && target == null)) { Stop(); return; }
            elapsed += Time.unscaledDeltaTime;
            if (absorbing)
            {
                float close = Mathf.SmoothStep(0f, 1f, LatchGelTiming.Clamp(elapsed / LatchGelTiming.CloseSeconds));
                float dissolve = LatchGelTiming.Dissolve(elapsed);
                float retract = Mathf.SmoothStep(0f, 1f, LatchGelTiming.Clamp((elapsed - 1.45f) / 0.55f));
                foreach (var surface in surfaces)
                {
                    surface.Copy.transform.position = Vector3.Lerp(surface.Position, slime.position, retract);
                    surface.Copy.transform.localScale = surface.Scale * Mathf.Max(0.01f, 1f - retract);
                }
                Draw(Mathf.Lerp(coverage, 1.1f, close) + retract, 1f, dissolve, 1f - retract);
                if (animator != null) animator.LatchFlatten = 0.95f * (1f - retract);
                if (elapsed >= LatchGelTiming.AbsorbSeconds) Stop();
                return;
            }

            RefreshPose();
            if (releasing)
            {
                float t = LatchGelTiming.Clamp(elapsed / 0.32f);
                Draw(coverage * (1f - t), 0f, 0f, 1f - t);
                if (strand != null) { strand.Target = frozenAnchor; strand.Reach = 1f - t; }
                if (animator != null) animator.LatchFlatten = 0.95f * (1f - t);
                if (t >= 1f) Stop();
                return;
            }

            coverage = Mathf.MoveTowards(coverage, LatchGelTiming.Coverage(progress), Time.unscaledDeltaTime * 1.3f);
            Draw(coverage, 0f, 0f, 1f);
            if (strand != null) { strand.Target = frozenAnchor; strand.Reach = Mathf.Clamp01(elapsed / 0.18f); }
            if (animator != null) animator.LatchFlatten = Mathf.SmoothStep(0f, 0.95f, LatchGelTiming.Clamp(elapsed / 0.18f));
        }

        public void Stop()
        {
            ResetSlimeVisual();
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        void ResetSlimeVisual()
        {
            if (!ReferenceEquals(slime, null) && Active.TryGetValue(slime, out var current) && current == this)
            {
                Active.Remove(slime);
                if (animator != null) animator.LatchFlatten = 0f;
            }
            strand?.Remove();
            strand = null;
        }

        void OnDestroy()
        {
            ResetSlimeVisual();
            foreach (var surface in surfaces)
                if (surface.Baked != null) Destroy(surface.Baked);
            if (material != null) Destroy(material);
        }
    }
}
