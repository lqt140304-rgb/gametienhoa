using System.IO;
using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Paints the pale branching veins that show through the gel (code-made texture, saved as a PNG).
    /// Veins are brighter than the base, so the body colour tints them. Planar UVs: seen from above.
    /// </summary>
    static class SlimeVeinTexture
    {
        const int Size = 256;
        const string Path = "Assets/_Project/Models/SlimeVeins.png";

        public static Texture2D GetOrCreate()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(Path);
            if (existing != null) return existing;

            var value = new float[Size * Size];
            for (int i = 0; i < value.Length; i++) value[i] = 0.72f;

            var rnd = new System.Random(5);
            for (int k = 0; k < 9; k++)
            {
                float x = 0.5f + ((float)rnd.NextDouble() - 0.5f) * 0.3f, y = 0.5f + ((float)rnd.NextDouble() - 0.5f) * 0.3f;
                Branch(value, rnd, x, y, (float)rnd.NextDouble() * Mathf.PI * 2f, 0.22f, 1.6f, 0);
            }
            Blur(value);
            Blur(value);

            var pixels = new Color32[value.Length];
            for (int i = 0; i < value.Length; i++)
            {
                byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(value[i] * 255f), 0, 255);
                pixels[i] = new Color32(b, b, b, 255);
            }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            File.WriteAllBytes(Path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(Path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(Path);
        }

        static void Branch(float[] value, System.Random rnd, float x, float y, float angle, float length, float width, int depth)
        {
            int steps = Mathf.Max(4, Mathf.RoundToInt(length * Size / 2f));
            for (int s = 0; s < steps; s++)
            {
                angle += ((float)rnd.NextDouble() - 0.5f) * 0.5f;
                x += Mathf.Cos(angle) * 2f / Size;
                y += Mathf.Sin(angle) * 2f / Size;
                if (x < 0.02f || x > 0.98f || y < 0.02f || y > 0.98f) return;
                float w = width * (1f - s / (float)steps * 0.6f);
                Dot(value, x, y, w);
                if (depth < 3 && rnd.NextDouble() < 0.05)
                    Branch(value, rnd, x, y, angle + ((float)rnd.NextDouble() < 0.5f ? 0.7f : -0.7f), length * 0.6f, width * 0.7f, depth + 1);
            }
        }

        static void Dot(float[] value, float x, float y, float radius)
        {
            int cx = Mathf.RoundToInt(x * Size), cy = Mathf.RoundToInt(y * Size), r = Mathf.CeilToInt(radius);
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    int px = cx + dx, py = cy + dy;
                    if (px < 0 || py < 0 || px >= Size || py >= Size) continue;
                    if (dx * dx + dy * dy > radius * radius) continue;
                    value[py * Size + px] = 1f;
                }
        }

        static void Blur(float[] value)
        {
            var copy = (float[])value.Clone();
            for (int y = 1; y < Size - 1; y++)
                for (int x = 1; x < Size - 1; x++)
                {
                    float sum = 0f;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++) sum += copy[(y + dy) * Size + x + dx];
                    value[y * Size + x] = sum / 9f;
                }
        }
    }
}
