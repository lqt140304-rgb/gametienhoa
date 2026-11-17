using System.Collections.Generic;
using Evolve.Creatures;
using Evolve.Player;
using UnityEngine;

namespace Evolve.Survival
{
    /// <summary>
    /// Hit feedback drawn over the screen: a health bar above every damaged predator, floating damage numbers
    /// (the spikes of one volley are added up into one number) and a small × at the crosshair in aim mode.
    /// It reacts at once to the local player's hits, so a client does not have to wait for the host to confirm.
    /// Added to the scene automatically when entering Play mode.
    /// </summary>
    public class CombatFeedback : MonoBehaviour
    {
        const float NumberLife = 0.9f;
        const float MergeWindow = 0.25f;

        class Number
        {
            public Predator Target;
            public Vector3 Position;
            public float Amount;
            public float Born;
        }

        readonly List<Number> numbers = new List<Number>();
        float hitMarkerUntil;
        GUIStyle numberStyle, markerStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindAnyObjectByType<CombatFeedback>() != null) return;
            var go = new GameObject("Combat Feedback");
            DontDestroyOnLoad(go);
            go.AddComponent<CombatFeedback>();
        }

        void OnEnable() => Predator.AnyDamaged += OnDamaged;
        void OnDisable() => Predator.AnyDamaged -= OnDamaged;

        void OnDamaged(Predator predator, float amount)
        {
            if (predator == null) return;
            float now = Time.unscaledTime;
            hitMarkerUntil = now + 0.2f;
            foreach (var n in numbers)
            {
                if (n.Target != predator || now - n.Born >= MergeWindow) continue;
                n.Amount += amount;
                return;
            }
            numbers.Add(new Number { Target = predator, Position = predator.transform.position, Amount = amount, Born = now });
        }

        void Update()
        {
            float now = Time.unscaledTime;
            numbers.RemoveAll(n => now - n.Born > NumberLife);
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            var cam = Camera.main;
            if (cam == null) return;
            EnsureStyles();
            float now = Time.unscaledTime;

            // Health bars above damaged predators.
            foreach (var p in Predator.Active)
            {
                if (p == null) continue;
                float fraction = p.HealthFraction;
                if (fraction >= 0.999f) continue;
                Vector3 sp = cam.WorldToScreenPoint(p.transform.position + Vector3.up * 1.1f);
                if (sp.z <= 0f) continue;
                DrawBar(sp.x, Screen.height - sp.y, fraction);
            }

            // Damage numbers float up and fade.
            var prev = GUI.color;
            foreach (var n in numbers)
            {
                float age = now - n.Born;
                Vector3 world = (n.Target != null ? n.Target.transform.position : n.Position) + Vector3.up * (1.5f + age * 0.9f);
                Vector3 sp = cam.WorldToScreenPoint(world);
                if (sp.z <= 0f) continue;
                GUI.color = new Color(1f, 1f, 1f, 1f - age / NumberLife);
                string text = n.Amount >= 1000f ? "-999+" : "-" + Mathf.RoundToInt(n.Amount);
                GUI.Label(new Rect(sp.x - 40f, Screen.height - sp.y - 12f, 80f, 26f), text, numberStyle);
            }
            GUI.color = prev;

            // Hit marker at the crosshair.
            if (AimController.Aiming && now < hitMarkerUntil)
                GUI.Label(new Rect(Screen.width / 2f - 20f, Screen.height / 2f - 20f, 40f, 40f), "×", markerStyle);
        }

        static void DrawBar(float x, float y, float fraction)
        {
            var back = new Rect(x - 26f, y - 4f, 52f, 6f);
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(back, Texture2D.whiteTexture);
            GUI.color = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.95f, 0.8f, 0.3f), Mathf.Clamp01(fraction));
            GUI.DrawTexture(new Rect(back.x + 1f, back.y + 1f, (back.width - 2f) * Mathf.Clamp01(fraction), back.height - 2f), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        void EnsureStyles()
        {
            if (numberStyle != null) return;
            numberStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter, fontSize = 18, fontStyle = FontStyle.Bold
            };
            numberStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
            markerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter, fontSize = 30, fontStyle = FontStyle.Bold
            };
            markerStyle.normal.textColor = new Color(1f, 0.35f, 0.3f);
        }
    }
}
