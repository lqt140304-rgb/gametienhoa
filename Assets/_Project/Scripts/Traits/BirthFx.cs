using UnityEngine;
using UnityEngine.UI;

namespace Evolve.Traits
{
    /// <summary>
    /// Screen-wide effects of the scene: tearing bands that flash across the picture while the connection is bad, a full-screen flash, and a shared
    /// particle system for sparks and bursts. All built from code.
    /// </summary>
    internal sealed class BirthFx
    {
        const int Bands = 7;

        readonly BirthStage stage;
        readonly Image[] bands = new Image[Bands];
        Image flash;
        float flashAlpha;
        Color flashColor = Color.white;
        ParticleSystem ps;

        public BirthFx(BirthStage stage)
        {
            this.stage = stage;
            var parent = stage.Canvas;
            for (int i = 0; i < Bands; i++)
            {
                bands[i] = BirthUiKit.Box(parent, "Tear" + i, Vector2.zero, new Vector2(stage.CanvasSize.x * 1.1f, 10f), new Color(1f, 1f, 1f, 0f));
                bands[i].gameObject.SetActive(false);
            }
            flash = BirthUiKit.Box(parent, "Flash", Vector2.zero, stage.CanvasSize * 1.2f, new Color(1f, 1f, 1f, 0f));
            flash.transform.SetAsLastSibling();
            MakeParticles();
        }

        void MakeParticles()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) return;   // no sparks, the rest of the scene still runs
            var go = new GameObject("Birth Particles");
            go.transform.SetParent(stage.Root.transform, false);
            ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false; main.loop = false; main.startSpeed = 0f; main.startLifetime = 1.2f; main.startSize = 0.15f;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 1200; main.gravityModifier = 0.15f;
            var em = ps.emission; em.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            var rend = go.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(shader) { mainTexture = BirthUiKit.Disc.texture };
            rend.material = mat;
            ps.Play();
        }

        /// <summary>Sparks flying out from a point on the canvas (canvas pixels from the centre).</summary>
        public void Burst(Vector2 canvasPos, Color color, int count, float speed)
        {
            if (ps == null) return;
            Vector3 world = stage.Canvas.TransformPoint(new Vector3(canvasPos.x, canvasPos.y, 0f));
            world.z -= 0.3f;   // in front of the canvas
            for (int i = 0; i < count; i++)
            {
                float a = Random.value * Mathf.PI * 2f, v = speed * (0.3f + Random.value);
                var ep = new ParticleSystem.EmitParams
                {
                    position = world,
                    velocity = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * v,
                    startColor = color,
                    startSize = Random.Range(0.06f, 0.22f),
                    startLifetime = Random.Range(0.7f, 1.6f)
                };
                ps.Emit(ep, 1);
            }
        }

        public void Flash(Color color, float alpha) { flashColor = color; flashAlpha = Mathf.Max(flashAlpha, alpha); }

        public void Tick(float time, float dt, float instability)
        {
            float k = Mathf.Clamp01(instability);
            int frame = Mathf.FloorToInt(time * 18f);
            for (int i = 0; i < Bands; i++)
            {
                bool on = k > 0.3f && BirthUiKit.Hash(frame * 13 + i * 71) < k * 0.45f;
                bands[i].gameObject.SetActive(on);
                if (!on) continue;
                float y = (BirthUiKit.Hash(frame * 7 + i * 19) - 0.5f) * stage.CanvasSize.y;
                float h = 4f + BirthUiKit.Hash(frame * 5 + i * 3) * 30f;
                bands[i].rectTransform.anchoredPosition = new Vector2((BirthUiKit.Hash(frame * 3 + i) - 0.5f) * 120f, y);
                bands[i].rectTransform.sizeDelta = new Vector2(stage.CanvasSize.x * 1.1f, h);
                bands[i].color = BirthUiKit.WithAlpha(i % 2 == 0 ? BirthUiKit.Cyan : Color.white, 0.07f + 0.1f * k);
            }
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, dt * 2.5f);
            flash.color = BirthUiKit.WithAlpha(flashColor, flashAlpha);
        }

        public void Clear() { if (ps != null) ps.Clear(); flashAlpha = 0f; }
    }
}
