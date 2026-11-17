using UnityEngine;
using Evolve.Survival;

namespace Evolve.Visuals
{
    /// <summary>F3: choose the colour of your slime (swatches plus hue / saturation / brightness sliders).</summary>
    public class SlimeColorMenu : MonoBehaviour
    {
        const int WindowId = 0xC10;
        public static bool IsOpen { get; private set; }

        static readonly Color[] Swatches =
        {
            new Color(0.68f, 0.88f, 0.68f), new Color(0.60f, 0.80f, 0.95f), new Color(0.95f, 0.70f, 0.80f),
            new Color(0.78f, 0.70f, 0.95f), new Color(0.98f, 0.80f, 0.62f), new Color(0.95f, 0.93f, 0.60f),
            new Color(0.55f, 0.92f, 0.88f), new Color(0.95f, 0.55f, 0.55f), new Color(0.80f, 0.82f, 0.85f)
        };

        Rect window = new Rect(20, 120, 280, 230);
        float h, s, v;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            IsOpen = false;
            if (FindAnyObjectByType<SlimeColorMenu>() != null) return;
            var go = new GameObject("Slime Color (F3)");
            DontDestroyOnLoad(go);
            go.AddComponent<SlimeColorMenu>();
        }

        void Update()
        {
            if (!KeyPressed.F3()) return;
            IsOpen = !IsOpen;
            if (IsOpen) Color.RGBToHSV(SlimeColor.Current, out h, out s, out v);
        }

        void OnDisable() => IsOpen = false;

        void OnGUI()
        {
            if (IsOpen) window = GUILayout.Window(WindowId, window, Draw, "Màu slime (F3 để đóng)");
        }

        void Draw(int id)
        {
            GUILayout.Label("Chọn màu có sẵn:");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < Swatches.Length; i++)
            {
                var old = GUI.backgroundColor;
                GUI.backgroundColor = Swatches[i];
                if (GUILayout.Button(" ", GUILayout.Width(24), GUILayout.Height(24)))
                {
                    Color.RGBToHSV(Swatches[i], out h, out s, out v);
                    SlimeColor.Set(Swatches[i]);
                }
                GUI.backgroundColor = old;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("Hoặc tự chỉnh:");
            float nh = Slider("Sắc", h), ns = Slider("Đậm", s), nv = Slider("Sáng", v);
            if (nh != h || ns != s || nv != v)
            {
                h = nh; s = ns; v = nv;
                SlimeColor.Set(Color.HSVToRGB(h, s, v));
            }
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        static float Slider(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(40));
            value = GUILayout.HorizontalSlider(value, 0f, 1f);
            GUILayout.EndHorizontal();
            return value;
        }
    }
}
