using Evolve.Survival;
using UnityEngine;

namespace Evolve.Player
{
    /// <summary>Shows needs on the slime body: dry grey tint (thirst), pale tint (hunger), trembling (stress). Visual only.</summary>
    [DefaultExecutionOrder(100)]
    public class BodyFeedback : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly Color DryColor = new Color(.5f, .45f, .36f);
        static readonly Color PaleColor = new Color(.88f, .92f, .86f);
        const float TrembleStress = 40f;
        const float TrembleMax = 0.06f;

        [SerializeField] Transform visual;
        SurvivalStats stats;
        Renderer bodyRenderer;
        MaterialPropertyBlock block;
        Color original = Color.white;
        Vector3 basePosition;
        bool ready, trembling;

        /// <summary>Pure: how far toward dry (0..1) and pale (0..1) the body tint goes.</summary>
        public static void TintAmounts(float thirst, float maxThirst, float hunger, float maxHunger, bool thirstEnabled,
            out float dry, out float pale)
        {
            dry = thirstEnabled && maxThirst > 0 ? Mathf.Clamp01((0.5f - thirst / maxThirst) / 0.5f) : 0f;
            pale = maxHunger > 0 ? Mathf.Clamp01((0.5f - hunger / maxHunger) / 0.5f) : 0f;
        }

        void Start()
        {
            stats = GetComponent<SurvivalStats>();
            if (visual == null)
            {
                var child = transform.Find("Visual");
                if (child != null) visual = child;
            }
            if (stats == null || visual == null) return;
            // Only the body renderer on the visual itself, not organ sockets/children.
            bodyRenderer = visual.GetComponent<Renderer>();
            if (bodyRenderer == null) return;
            var mat = bodyRenderer.sharedMaterial;
            if (mat != null)
            {
                if (mat.HasProperty(BaseColorId)) original = mat.GetColor(BaseColorId);
                else if (mat.HasProperty(ColorId)) original = mat.GetColor(ColorId);
            }
            block = new MaterialPropertyBlock();
            basePosition = visual.localPosition;
            ready = true;
        }

        void LateUpdate()
        {
            if (!ready || stats.IsRemote) return;
            var s = stats.State;
            if (s == null) return;
            TintAmounts(s.Thirst, s.MaxThirst, s.Hunger, s.MaxHunger, s.ThirstEnabled, out float dry, out float pale);
            Color c = Color.Lerp(original, DryColor, dry * 0.7f);
            c = Color.Lerp(c, PaleColor, pale * 0.6f);
            bodyRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            block.SetColor(ColorId, c);
            bodyRenderer.SetPropertyBlock(block);

            if (s.Stress > TrembleStress && !s.IsDead)
            {
                float k = Mathf.Clamp01((s.Stress - TrembleStress) / (SurvivalState.MaxStress - TrembleStress));
                float amp = TrembleMax * (0.4f + 0.6f * k);
                visual.localPosition = basePosition + new Vector3(Random.Range(-amp, amp), Random.Range(-amp, amp) * 0.5f, Random.Range(-amp, amp));
                trembling = true;
            }
            else if (trembling)
            {
                visual.localPosition = basePosition;
                trembling = false;
            }
        }

        void OnDisable()
        {
            if (ready && trembling && visual != null) visual.localPosition = basePosition;
            trembling = false;
        }
    }
}
