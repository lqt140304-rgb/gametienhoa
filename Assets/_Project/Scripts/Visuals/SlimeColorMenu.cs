using UnityEngine;
using Evolve.Survival;

namespace Evolve.Visuals
{
    /// <summary>F3: choose the colour of your slime, part by part (body, eyes, legs, back, skin film, core).</summary>
    public class SlimeColorMenu : MonoBehaviour
    {
        const int WindowId = 0xC10;
        public static bool IsOpen { get; private set; }

        static readonly string[] PartNames = { "Thân", "Mắt", "Chân", "Lưng", "Lớp phủ", "Lõi" };
        static readonly Color[] Swatches =
        {
            new Color(0.68f, 0.88f, 0.68f), new Color(0.60f, 0.80f, 0.95f), new Color(0.95f, 0.70f, 0.80f),
            new Color(0.78f, 0.70f, 0.95f), new Color(0.98f, 0.80f, 0.62f), new Color(0.95f, 0.93f, 0.60f),
            new Color(0.55f, 0.92f, 0.88f), new Color(0.95f, 0.55f, 0.55f), new Color(0.80f, 0.82f, 0.85f)
        };

        Rect window = new Rect(20, 120, 330, 380);
        int part;
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
            if (IsOpen) LoadHsv();
        }

        void OnDisable() => IsOpen = false;

        void LoadHsv() => Color.RGBToHSV(SlimeColor.GetRaw((SlimePart)part), out h, out s, out v);

        void OnGUI()
        {
            if (IsOpen) window = GUILayout.Window(WindowId, window, Draw, "Màu slime (F3 để đóng)");
        }

        void Draw(int id)
        {
            bool linked = GUILayout.Toggle(SlimeColor.Linked, " Mắt, chân, lưng cùng màu với thân");
            if (linked != SlimeColor.Linked) { SlimeColor.SetLinked(linked); LoadHsv(); }
            if (SlimeColor.IsUnlocked(ColorUnlocks.Rainbow))   // won in the Vườn Quặng Cầu Vồng
            {
                bool rainbow = GUILayout.Toggle(SlimeColor.RainbowOn, " Cầu Vồng (đã mở khóa): thân đổi sắc liên tục");
                if (rainbow != SlimeColor.RainbowOn) SlimeColor.SetRainbow(rainbow);
            }

            int picked = GUILayout.SelectionGrid(part, PartNames, 3);
            if (picked != part) { part = picked; LoadHsv(); }
            var slimePart = (SlimePart)part;

            if (SlimeColor.Linked && SlimeColor.Follows(slimePart))
            {
                GUILayout.Space(8);
                GUILayout.Label($"“{PartNames[part]}” đang cùng màu với thân. Bỏ chọn ô phía trên để chỉnh riêng, hoặc chỉnh màu Thân.", GUILayout.Width(300));
            }
            else
            {
                GUILayout.Space(4);
                GUILayout.Label($"Màu của “{PartNames[part]}”:");
                GUILayout.BeginHorizontal();
                for (int i = 0; i < Swatches.Length; i++)
                {
                    var old = GUI.backgroundColor;
                    GUI.backgroundColor = Swatches[i];
                    if (GUILayout.Button(" ", GUILayout.Width(26), GUILayout.Height(24)))
                    {
                        Color.RGBToHSV(Swatches[i], out h, out s, out v);
                        SlimeColor.Set(slimePart, Swatches[i]);
                    }
                    GUI.backgroundColor = old;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(6);
                float nh = Slider("Sắc", h), ns = Slider("Đậm", s), nv = Slider("Sáng", v);
                if (nh != h || ns != s || nv != v)
                {
                    h = nh; s = ns; v = nv;
                    SlimeColor.Set(slimePart, Color.HSVToRGB(h, s, v));
                }
            }

            GUILayout.Space(6);
            var old2 = GUI.backgroundColor;
            GUI.backgroundColor = SlimeColor.Get(slimePart);
            GUILayout.Box("Màu hiện tại", GUILayout.Height(26));
            GUI.backgroundColor = old2;
            if (GUILayout.Button("Về màu ban đầu")) { SlimeColor.Reset(); LoadHsv(); }
            GUILayout.Label("Lưu ý: chưa có mắt thì thế giới là đen trắng, màu chỉ hiện đầy đủ khi đã có mắt.", GUILayout.Width(300));
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
