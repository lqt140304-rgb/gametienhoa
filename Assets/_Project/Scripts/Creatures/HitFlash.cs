using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>A brief pale flash and a small pop on a creature that was just hit. Added on first use.</summary>
    public class HitFlash : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        Renderer[] renderers;
        MaterialPropertyBlock block;
        Vector3 baseScale;
        float until;
        bool active;

        public static void Play(GameObject target)
        {
            if (target == null) return;
            var flash = target.GetComponent<HitFlash>();
            if (flash == null) flash = target.AddComponent<HitFlash>();
            flash.Trigger();
        }

        void Trigger()
        {
            if (renderers == null)
            {
                renderers = GetComponentsInChildren<Renderer>();
                block = new MaterialPropertyBlock();
            }
            if (!active) baseScale = transform.localScale;
            active = true;
            until = Time.time + 0.12f;

            var pale = new Color(1f, 0.95f, 0.85f);
            block.SetColor(BaseColorId, pale);
            block.SetColor(ColorId, pale);
            foreach (var r in renderers)
                if (r != null) r.SetPropertyBlock(block);
            transform.localScale = baseScale * 1.12f;
        }

        void Update()
        {
            if (!active || Time.time < until) return;
            active = false;
            foreach (var r in renderers)
                if (r != null) r.SetPropertyBlock(null);
            transform.localScale = baseScale;
        }
    }
}
